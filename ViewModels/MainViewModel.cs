using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using GonkNote.Models;
using GonkNote.Services;

namespace GonkNote.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly DispatcherTimer _autosave;
    private DocumentTabViewModel? _selectedTab;
    private TreeItemViewModel? _selectedTreeItem;

    public ObservableCollection<TreeItemViewModel> RootItems { get; } = new();
    public ObservableCollection<DocumentTabViewModel> OpenTabs { get; } = new();

    /// <summary>Angepinnte Ordner für den Schnellzugriff-Bereich der Seitenleiste.</summary>
    public ObservableCollection<TreeItemViewModel> PinnedFolders { get; } = new();

    public MainViewModel(DatabaseService db)
    {
        _db = db;
        BuildTree();

        NewFolderCommand = new RelayCommand(p => CreateItem(ItemKind.Folder, p as TreeItemViewModel));
        NewNotebookCommand = new RelayCommand(p => CreateItem(ItemKind.Notebook, p as TreeItemViewModel));
        NewWhiteboardCommand = new RelayCommand(p => CreateItem(ItemKind.Whiteboard, p as TreeItemViewModel));
        NewTextDocCommand = new RelayCommand(p => CreateItem(ItemKind.TextDocument, p as TreeItemViewModel));
        RenameCommand = new RelayCommand(p => { if (p is TreeItemViewModel t) t.IsRenaming = true; });
        DeleteCommand = new RelayCommand(p => DeleteItem(p as TreeItemViewModel));
        OpenItemCommand = new RelayCommand(p => { if (p is TreeItemViewModel t) OpenItem(t); });
        TogglePinCommand = new RelayCommand(p => TogglePinned(p as TreeItemViewModel));
        ToggleFavoriteCommand = new RelayCommand(p => ToggleFavorite(p as TreeItemViewModel));
        OpenPinnedCommand = new RelayCommand(p => { if (p is TreeItemViewModel t) RevealItem(t); });
        CloseTabCommand = new RelayCommand(p => { if (p is DocumentTabViewModel t) CloseTab(t); });
        SaveCommand = new RelayCommand(() => SelectedTab?.Save());
        SaveAllCommand = new RelayCommand(SaveAll);
        ToggleThemeCommand = new RelayCommand(ThemeService.Toggle);
        ExitCommand = new RelayCommand(() => Application.Current.MainWindow?.Close());

        _autosave = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _autosave.Tick += (_, _) => SaveAll();
        _autosave.Start();

        // Standard-Symbolfarbe hängt am Theme
        ThemeService.ThemeChanged += RefreshAllIcons;
    }

    private void RefreshAllIcons()
    {
        void Walk(IEnumerable<TreeItemViewModel> items)
        {
            foreach (var it in items) { it.RefreshIcon(); Walk(it.Children); }
        }
        Walk(RootItems);
    }

    /// <summary>Setzt die Symbolfarbe (null = Standard) und persistiert sie.</summary>
    public void SetIconColor(TreeItemViewModel? vm, string? hex)
    {
        if (vm == null) return;
        vm.Item.IconColor = hex;
        _db.UpsertItem(vm.Item);
        vm.RefreshIcon();
    }

    public RelayCommand NewFolderCommand { get; }
    public RelayCommand NewNotebookCommand { get; }
    public RelayCommand NewWhiteboardCommand { get; }
    public RelayCommand NewTextDocCommand { get; }
    public RelayCommand RenameCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand OpenItemCommand { get; }
    public RelayCommand TogglePinCommand { get; }
    public RelayCommand ToggleFavoriteCommand { get; }
    public RelayCommand OpenPinnedCommand { get; }
    public RelayCommand CloseTabCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SaveAllCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }
    public RelayCommand ExitCommand { get; }

    public DocumentTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => Set(ref _selectedTab, value);
    }

    public TreeItemViewModel? SelectedTreeItem
    {
        get => _selectedTreeItem;
        set => Set(ref _selectedTreeItem, value);
    }

    // ---------- Baum ----------

    private void BuildTree()
    {
        RootItems.Clear();
        var all = _db.GetAllItems();
        var byId = all.ToDictionary(i => i.Id, i => new TreeItemViewModel(i));

        foreach (var vm in byId.Values)
        {
            if (vm.Item.ParentId is Guid pid && byId.TryGetValue(pid, out var parent))
                parent.Children.Add(vm);
            else
                RootItems.Add(vm);
        }

        foreach (var vm in byId.Values) vm.SortChildren();
        SortRoot();
        RefreshPinned();
    }

    // ---------- Anpinnen / Favoriten ----------

    /// <summary>Pin-Status eines Ordners umschalten (Schnellzugriff-Bereich).</summary>
    public void TogglePinned(TreeItemViewModel? vm)
    {
        if (vm is not { IsFolder: true }) return;
        vm.Item.IsPinned = !vm.Item.IsPinned;
        _db.UpsertItem(vm.Item);
        vm.RefreshPinFavorite();
        RefreshPinned();
    }

    /// <summary>Favoriten-Status eines Ordners umschalten (wird im Ordner zuerst angezeigt).</summary>
    public void ToggleFavorite(TreeItemViewModel? vm)
    {
        if (vm is not { IsFolder: true }) return;
        vm.Item.IsFavorite = !vm.Item.IsFavorite;
        _db.UpsertItem(vm.Item);
        vm.RefreshPinFavorite();
        FindParent(vm)?.SortChildren();
        if (vm.Item.ParentId == null) SortRoot();
    }

    /// <summary>Baut die Schnellzugriff-Liste neu auf (alle angepinnten Ordner, alphabetisch).</summary>
    private void RefreshPinned()
    {
        var pinned = new List<TreeItemViewModel>();
        void Walk(IEnumerable<TreeItemViewModel> items)
        {
            foreach (var it in items)
            {
                if (it is { IsFolder: true, IsPinned: true }) pinned.Add(it);
                Walk(it.Children);
            }
        }
        Walk(RootItems);

        PinnedFolders.Clear();
        foreach (var p in pinned.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
            PinnedFolders.Add(p);
    }

    /// <summary>Springt im Baum zu einem Eintrag: Vorfahren aufklappen und auswählen.</summary>
    public void RevealItem(TreeItemViewModel vm)
    {
        for (var p = FindParent(vm); p != null; p = FindParent(p))
            p.IsExpanded = true;
        vm.IsExpanded = true;
        vm.IsSelected = true;
    }

    private void SortRoot()
    {
        var sorted = RootItems
            .OrderByDescending(c => c.IsFolder)
            .ThenByDescending(c => c.IsFavorite)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            int cur = RootItems.IndexOf(sorted[i]);
            if (cur != i) RootItems.Move(cur, i);
        }
    }

    private static string DefaultName(ItemKind kind) => kind switch
    {
        ItemKind.Folder => "Neuer Ordner",
        ItemKind.Notebook => "Neues Notizbuch",
        ItemKind.Whiteboard => "Neues Whiteboard",
        ItemKind.TextDocument => "Neues Textdokument",
        _ => "Neu",
    };

    /// <summary>Beendet eine laufende Umbenennung, bevor eine andere Aktion Fokus stiehlt.</summary>
    public void CommitPendingRename()
    {
        var renaming = FindRenaming(RootItems);
        if (renaming != null) CommitRename(renaming);

        static TreeItemViewModel? FindRenaming(IEnumerable<TreeItemViewModel> items)
        {
            foreach (var it in items)
            {
                if (it.IsRenaming) return it;
                if (FindRenaming(it.Children) is { } found) return found;
            }
            return null;
        }
    }

    private void CreateItem(ItemKind kind, TreeItemViewModel? context)
    {
        CommitPendingRename();
        context ??= SelectedTreeItem;
        // Ziel: der gewählte Ordner, sonst der Ordner des gewählten Dokuments, sonst Wurzel
        var parent = context == null ? null
            : context.IsFolder ? context
            : FindParent(context);

        var item = new NoteItem
        {
            Kind = kind,
            Name = DefaultName(kind),
            ParentId = parent?.Id,
        };
        _db.UpsertItem(item);

        var vm = new TreeItemViewModel(item);
        var target = parent?.Children ?? RootItems;
        target.Add(vm);
        if (parent != null) { parent.SortChildren(); parent.IsExpanded = true; }
        else SortRoot();

        vm.IsSelected = true;
        vm.IsRenaming = true;
        // Erst nach dem Benennen öffnen, sonst stiehlt der neue Tab der Namensbox den Fokus
        _pendingOpen = item.IsFolder ? null : vm;
    }

    private TreeItemViewModel? _pendingOpen;

    public TreeItemViewModel? FindParent(TreeItemViewModel child)
    {
        if (child.Item.ParentId is not Guid pid) return null;
        return FindById(pid);
    }

    public TreeItemViewModel? FindById(Guid id)
    {
        TreeItemViewModel? Walk(IEnumerable<TreeItemViewModel> items)
        {
            foreach (var it in items)
            {
                if (it.Id == id) return it;
                if (Walk(it.Children) is { } found) return found;
            }
            return null;
        }
        return Walk(RootItems);
    }

    private void DeleteItem(TreeItemViewModel? vm)
    {
        vm ??= SelectedTreeItem;
        if (vm == null) return;

        var msg = vm.IsFolder
            ? $"Ordner „{vm.Name}“ und den gesamten Inhalt löschen?"
            : $"„{vm.Name}“ löschen?";
        if (MessageBox.Show(msg, "Gonk Note", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        CloseTabsRecursive(vm);
        _db.DeleteItemRecursive(vm.Id);

        var parent = FindParent(vm);
        (parent?.Children ?? RootItems).Remove(vm);
        RefreshPinned();
    }

    private void CloseTabsRecursive(TreeItemViewModel vm)
    {
        var tab = OpenTabs.FirstOrDefault(t => t.Id == vm.Id);
        if (tab != null) OpenTabs.Remove(tab);
        foreach (var child in vm.Children) CloseTabsRecursive(child);
    }

    /// <summary>Verschiebt (oder kopiert) einen Eintrag per Drag &amp; Drop.</summary>
    public void MoveItem(TreeItemViewModel source, TreeItemViewModel? targetFolder, bool copy)
    {
        if (targetFolder != null && !targetFolder.IsFolder) return;
        // Kein Verschieben in sich selbst oder eigene Unterordner
        for (var p = targetFolder; p != null; p = FindParent(p))
            if (p == source) return;

        if (copy)
        {
            CopyRecursive(source, targetFolder);
        }
        else
        {
            if (source.Item.ParentId == targetFolder?.Id) return;
            var oldParent = FindParent(source);
            (oldParent?.Children ?? RootItems).Remove(source);

            source.Item.ParentId = targetFolder?.Id;
            _db.UpsertItem(source.Item);

            (targetFolder?.Children ?? RootItems).Add(source);
            if (targetFolder != null) { targetFolder.SortChildren(); targetFolder.IsExpanded = true; }
            else SortRoot();
        }
    }

    private void CopyRecursive(TreeItemViewModel source, TreeItemViewModel? targetFolder)
    {
        var clone = new NoteItem
        {
            Kind = source.Kind,
            Name = source.Item.ParentId == targetFolder?.Id ? source.Name + " (Kopie)" : source.Name,
            ParentId = targetFolder?.Id,
        };
        _db.UpsertItem(clone);

        // Inhalt mitkopieren
        switch (source.Kind)
        {
            case ItemKind.Notebook:
            case ItemKind.Whiteboard:
                var board = _db.GetBoard(source.Item);
                board.Id = clone.Id;
                _db.SaveBoard(board);
                break;
            case ItemKind.TextDocument:
                var text = _db.GetText(source.Id);
                text.Id = clone.Id;
                _db.SaveText(text);
                break;
        }

        var cloneVm = new TreeItemViewModel(clone);
        (targetFolder?.Children ?? RootItems).Add(cloneVm);
        if (targetFolder != null) { targetFolder.SortChildren(); targetFolder.IsExpanded = true; }
        else SortRoot();

        foreach (var child in source.Children)
            CopyRecursive(child, cloneVm);
    }

    public void CommitRename(TreeItemViewModel vm)
    {
        vm.IsRenaming = false;
        if (string.IsNullOrWhiteSpace(vm.Name)) vm.Name = DefaultName(vm.Kind);
        _db.UpsertItem(vm.Item);
        FindParent(vm)?.SortChildren();
        if (vm.Item.ParentId == null) SortRoot();

        OpenTabs.FirstOrDefault(t => t.Id == vm.Id)?.NotifyRenamed();
        if (vm.IsPinned) RefreshPinned();

        if (_pendingOpen == vm)
        {
            _pendingOpen = null;
            OpenItem(vm);
        }
    }

    // ---------- Tabs ----------

    public void OpenItem(TreeItemViewModel vm)
    {
        if (vm.IsFolder) { vm.IsExpanded = !vm.IsExpanded; return; }

        var existing = OpenTabs.FirstOrDefault(t => t.Id == vm.Id);
        if (existing != null) { SelectedTab = existing; return; }

        DocumentTabViewModel tab = vm.Kind == ItemKind.TextDocument
            ? new TextTabViewModel(vm.Item, _db)
            : new WhiteboardTabViewModel(vm.Item, _db);

        OpenTabs.Add(tab);
        SelectedTab = tab;
    }

    public void CloseTab(DocumentTabViewModel tab)
    {
        tab.Save();
        int idx = OpenTabs.IndexOf(tab);
        OpenTabs.Remove(tab);
        if (SelectedTab == null && OpenTabs.Count > 0)
            SelectedTab = OpenTabs[Math.Min(idx, OpenTabs.Count - 1)];
    }

    public void SaveAll()
    {
        foreach (var tab in OpenTabs) tab.Save();
    }
}
