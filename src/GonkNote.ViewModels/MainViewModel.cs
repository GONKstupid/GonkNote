using System.Runtime;
using System.Collections.ObjectModel;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Services;
using GonkNote.Core.Services;

namespace GonkNote.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly IPlatformServices _platform;
    private readonly IDisposable _autosave;
    private DocumentTabViewModel? _selectedTab;
    private TreeItemViewModel? _selectedTreeItem;
    private TreeItemViewModel? _galleryFolder;

    public ObservableCollection<TreeItemViewModel> RootItems { get; } = new();
    public ObservableCollection<DocumentTabViewModel> OpenTabs { get; } = new();

    /// <summary>Angepinnte Ordner für den Schnellzugriff-Bereich der Seitenleiste.</summary>
    public ObservableCollection<TreeItemViewModel> PinnedFolders { get; } = new();

    /// <summary>Kacheln des „Big Picture"-Galeriemodus (Inhalt des aktuellen Ordners).</summary>
    public ObservableCollection<GalleryItemViewModel> GalleryItems { get; } = new();

    /// <summary>Breadcrumb-Pfad im Galeriemodus (Wurzel „Dokumente" + Vorfahren).</summary>
    public ObservableCollection<BreadcrumbEntry> Breadcrumb { get; } = new();

    public MainViewModel(DatabaseService db, IPlatformServices platform)
    {
        _db = db;
        _platform = platform;
        BuildTree();
        RebuildGallery();
        RebuildBreadcrumb();

        NewFolderCommand = new RelayCommand(p => CreateItem(ItemKind.Folder, p as TreeItemViewModel));
        NewNotebookCommand = new RelayCommand(p => CreateItem(ItemKind.Notebook, p as TreeItemViewModel));
        NewWhiteboardCommand = new RelayCommand(p => CreateItem(ItemKind.Whiteboard, p as TreeItemViewModel));
        NewTextDocCommand = new RelayCommand(p => CreateItem(ItemKind.TextDocument, p as TreeItemViewModel));
        RenameCommand = new RelayCommand(p => { if (p is TreeItemViewModel t) t.IsRenaming = true; });
        DeleteCommand = new RelayCommand(p => DeleteItem(p as TreeItemViewModel));
        OpenItemCommand = new RelayCommand(p => { if (p is TreeItemViewModel t) OpenItem(t); });
        ImportDocxCommand = new RelayCommand(ImportDocx);
        ExportCommand = new RelayCommand(() => ExportActiveTab());
        TogglePinCommand = new RelayCommand(p => TogglePinned(p as TreeItemViewModel));
        ToggleFavoriteCommand = new RelayCommand(p => ToggleFavorite(p as TreeItemViewModel));
        OpenPinnedCommand = new RelayCommand(p => { if (p is TreeItemViewModel t) { NavigateGallery(t); RevealItem(t); } });
        GalleryOpenCommand = new RelayCommand(p => GalleryOpen(p as GalleryItemViewModel));
        GalleryBackCommand = new RelayCommand(_ => NavigateGallery(_galleryFolder == null ? null : FindParent(_galleryFolder)));
        GalleryNavigateCommand = new RelayCommand(p => NavigateGallery(p as TreeItemViewModel));
        CloseTabCommand = new RelayCommand(p => { if (p is DocumentTabViewModel t) CloseTab(t); });
        SaveCommand = new RelayCommand(() => SelectedTab?.Save());
        SaveAllCommand = new RelayCommand(SaveAll);
        ToggleThemeCommand = new RelayCommand(_platform.Theme.Toggle);
        ExitCommand = new RelayCommand(_platform.Shell.Quit);

        _autosave = _platform.Scheduler.Repeat(TimeSpan.FromSeconds(30), SaveAll);

        // Standard-Symbolfarbe hängt am Theme
        _platform.Theme.ThemeChanged += RefreshAllIcons;

        // Texte, die der Code erzeugt (Galerietitel, Pfadleiste, Datumsangaben), tragen die
        // Sprache nicht über eine Bindung – sie werden beim Wechsel neu aufgebaut.
        Loc.LanguageChanged += RefreshLanguage;
    }

    private void RefreshLanguage()
    {
        RebuildBreadcrumb();
        RebuildGallery();
        OnPropertyChanged(nameof(GalleryTitle));

        // Die Kontextmenü-Beschriftungen „Anpinnen"/„Als Favorit" wechseln mit dem Zustand
        // UND mit der Sprache. Ohne diese Zeile meldeten sie nur den Zustandswechsel, und
        // der Eintrag bliebe nach einem Sprachwechsel in der alten Sprache stehen — genau
        // das war beim Umstellen auf Loc in Phase 2 am laufenden Programm zu sehen.
        Walk(RootItems, it => it.RefreshPinFavorite());
    }

    private void RefreshAllIcons() => Walk(RootItems, it => it.RefreshIcon());

    /// <summary>Führt <paramref name="tun"/> auf jedem Knoten des Baums aus, Kinder inbegriffen.</summary>
    private static void Walk(IEnumerable<TreeItemViewModel> items, Action<TreeItemViewModel> tun)
    {
        foreach (var it in items) { tun(it); Walk(it.Children, tun); }
    }

    /// <summary>
    /// Setzt die Symbolfarbe händisch (null = automatisch: erbt die Ordnerfarbe des
    /// übergeordneten Ordners) und persistiert sie. Färbt anschließend alle Nachkommen
    /// ohne eigene Farbe neu.
    /// </summary>
    public void SetIconColor(TreeItemViewModel? vm, string? hex)
    {
        if (vm == null) return;
        vm.Item.IconColor = hex;
        _db.UpsertItem(vm.Item);
        ApplyInheritedColors();
        RebuildGallery();   // Ordnerfarbe in der Galerie mitziehen
    }

    public RelayCommand NewFolderCommand { get; }
    public RelayCommand NewNotebookCommand { get; }
    public RelayCommand NewWhiteboardCommand { get; }
    public RelayCommand NewTextDocCommand { get; }
    public RelayCommand RenameCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand OpenItemCommand { get; }
    public RelayCommand ImportDocxCommand { get; }
    public RelayCommand ExportCommand { get; }
    public RelayCommand TogglePinCommand { get; }
    public RelayCommand ToggleFavoriteCommand { get; }
    public RelayCommand OpenPinnedCommand { get; }
    public RelayCommand GalleryOpenCommand { get; }
    public RelayCommand GalleryBackCommand { get; }
    public RelayCommand GalleryNavigateCommand { get; }
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
        set
        {
            if (!Set(ref _selectedTreeItem, value)) return;
            // Ordnerauswahl im Baum zieht den Galeriemodus mit (zeigt den Ordnerinhalt).
            // Dokumente lassen die Galerie unverändert (sie wird beim Öffnen ohnehin ausgeblendet).
            if (!_suppressGallerySync && value is { IsFolder: true }) GalleryFolder = value;
        }
    }

    /// <summary>Unterdrückt vorübergehend die Baum→Galerie-Synchronisierung (z. B. beim Umbenennen).</summary>
    private bool _suppressGallerySync;

    /// <summary>Startet die Umbenennung eines Eintrags aus der Galerie, ohne die Galerie in den Ordner zu navigieren.</summary>
    public void BeginRename(TreeItemViewModel t)
    {
        _suppressGallerySync = true;
        RevealItem(t);            // im Baum sichtbar machen + auswählen (ohne Galerie-Navigation)
        _suppressGallerySync = false;
        t.IsRenaming = true;
    }

    // ---------- Big-Picture-Galeriemodus ----------

    /// <summary>Der Ordner, dessen Inhalt die Galerie zeigt; null = Wurzelebene („Dokumente").</summary>
    public TreeItemViewModel? GalleryFolder
    {
        get => _galleryFolder;
        private set
        {
            if (!Set(ref _galleryFolder, value)) return;
            OnPropertyChanged(nameof(GalleryTitle));
            OnPropertyChanged(nameof(CanGoBack));
            RebuildGallery();
            RebuildBreadcrumb();
        }
    }

    public string GalleryTitle => _galleryFolder?.Name ?? Loc.T("Gallery.Root");
    public bool CanGoBack => _galleryFolder != null;
    public bool GalleryIsEmpty => GalleryItems.Count == 0;
    public bool ShowBreadcrumb => _galleryFolder != null;

    /// <summary>Wechselt den Galerie-Ordner und zieht die Auswahl im Baum mit.</summary>
    public void NavigateGallery(TreeItemViewModel? folder)
    {
        GalleryFolder = folder;
        if (folder != null) RevealItem(folder);
    }

    private void GalleryOpen(GalleryItemViewModel? item)
    {
        if (item == null) return;
        if (item.IsFolder) NavigateGallery(item.Tree);
        else OpenItem(item.Tree);
    }

    /// <summary>Baut die Galerie-Kacheln aus dem aktuellen Ordner (bzw. der Wurzel) neu auf.</summary>
    public void RebuildGallery()
    {
        GalleryItems.Clear();
        var children = _galleryFolder?.Children ?? RootItems;
        foreach (var c in children)
            GalleryItems.Add(new GalleryItemViewModel(c, _db));
        OnPropertyChanged(nameof(GalleryIsEmpty));
    }

    private void RebuildBreadcrumb()
    {
        Breadcrumb.Clear();
        Breadcrumb.Add(new BreadcrumbEntry(Loc.T("Gallery.Root"), null));
        // Vorfahren des aktuellen Ordners (ohne ihn selbst – der ist der große Titel)
        var chain = new List<TreeItemViewModel>();
        for (var p = _galleryFolder == null ? null : FindParent(_galleryFolder); p != null; p = FindParent(p))
            chain.Insert(0, p);
        foreach (var f in chain) Breadcrumb.Add(new BreadcrumbEntry(f.Name, f));
        OnPropertyChanged(nameof(ShowBreadcrumb));
    }

    /// <summary>Zeigt den angegebenen Ordner in der Galerie (baut immer neu, auch wenn gleich).</summary>
    private void ShowFolderInGallery(TreeItemViewModel? folder)
    {
        if (_galleryFolder == folder) RebuildGallery();
        else GalleryFolder = folder;
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
        ApplyInheritedColors();
        RefreshPinned();
    }

    /// <summary>
    /// Vererbt die Symbolfarbe eines Ordners an alle Nachkommen ohne eigene (händisch
    /// gesetzte) Farbe. Zur Laufzeit berechnet – nicht in der DB gespeichert, damit ein
    /// späterer Ordner-Farbwechsel automatisch durchschlägt.
    /// </summary>
    public void ApplyInheritedColors()
    {
        void Walk(IEnumerable<TreeItemViewModel> items, string? inherited)
        {
            foreach (var it in items)
            {
                it.InheritedColorHex = it.Item.IconColor == null ? inherited : null;
                it.RefreshIcon();
                // Effektive Farbe für die Kinder: eigene Farbe hat Vorrang, sonst geerbte
                Walk(it.Children, it.Item.IconColor ?? inherited);
            }
        }
        Walk(RootItems, null);
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
        RebuildGallery();   // Stern + Sortierung in der Galerie mitziehen
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

    /// <summary>
    /// Name eines frisch angelegten Eintrags – in der Sprache, die beim Anlegen aktiv ist.
    /// Bestehende Namen bleiben unberührt: sie gehören dem Nutzer, nicht der Oberfläche.
    /// </summary>
    private static string DefaultName(ItemKind kind) => Loc.T(kind switch
    {
        ItemKind.Folder => "New.Folder",
        ItemKind.Notebook => "New.Notebook",
        ItemKind.Whiteboard => "New.Whiteboard",
        ItemKind.TextDocument => "New.Text",
        _ => "Gallery.New",
    });

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
        ApplyInheritedColors();   // neues Element erbt die Ordnerfarbe

        vm.IsSelected = true;
        vm.IsRenaming = true;
        // Erst nach dem Benennen öffnen, sonst stiehlt der neue Tab der Namensbox den Fokus
        _pendingOpen = item.IsFolder ? null : vm;

        // Galerie zeigt den Zielordner mit der neuen Kachel (nicht in den neuen, leeren Ordner
        // hineinnavigieren – die Auswahl im Baum steht fürs Umbenennen auf dem neuen Element).
        ShowFolderInGallery(parent);
    }

    private TreeItemViewModel? _pendingOpen;

    // ---------- Import ----------

    /// <summary>DOCX-/Markdown-Dateien als neue Textdokumente importieren (Formatierung bestmöglich).</summary>
    private void ImportDocx()
    {
        CommitPendingRename();
        var files = _platform.Files.Open(
            Loc.T("Dialog.ImportTitle"), _platform.Documents.ImportFormats, multiple: true);
        if (files.Count == 0) return;

        var context = SelectedTreeItem;
        var parent = context == null ? null
            : context.IsFolder ? context
            : FindParent(context);

        var failed = new List<string>();
        TreeItemViewModel? lastImported = null;

        foreach (var file in files)
        {
            try
            {
                var textDoc = new TextDoc();
                byte[] bytes = _platform.Documents.Import(file, textDoc);

                var item = new NoteItem
                {
                    Kind = ItemKind.TextDocument,
                    Name = System.IO.Path.GetFileNameWithoutExtension(file),
                    ParentId = parent?.Id,
                };
                _db.UpsertItem(item);
                textDoc.Id = item.Id;
                textDoc.Rtf = bytes;
                _db.SaveText(textDoc);

                var vm = new TreeItemViewModel(item);
                (parent?.Children ?? RootItems).Add(vm);
                lastImported = vm;
            }
            catch (Exception ex)
            {
                failed.Add($"{System.IO.Path.GetFileName(file)} – {ex.Message}");
            }
        }

        if (lastImported != null)
        {
            if (parent != null) { parent.SortChildren(); parent.IsExpanded = true; }
            else SortRoot();
            ApplyInheritedColors();   // importierte Dokumente erben die Ordnerfarbe
            lastImported.IsSelected = true;
            OpenItem(lastImported);
        }

        if (failed.Count > 0)
            _platform.Dialogs.Inform(Loc.T("Msg.ImportFailed") + "\n" + string.Join("\n", failed),
                DialogSeverity.Warning);
    }

    // ---------- Export ----------

    /// <summary>Exportiert den aktiven Tab: Textdokument → PDF/DOCX/Markdown, Whiteboard/Notizbuch → PDF.</summary>
    /// <summary>
    /// Export des aktiven Tabs. <paramref name="preferred"/> wählt das Format im Dialog vor
    /// (".pdf"/".png"/…) – dafür gibt es die Knöpfe in der Einstellungs-Seitenleiste von
    /// Whiteboard und Notizbuch; ohne Angabe steht wie bisher PDF oben.
    /// </summary>
    public void ExportActiveTab(string? preferred = null)
    {
        var tab = SelectedTab;
        if (tab == null)
        {
            _platform.Dialogs.Inform(Loc.T("Msg.OpenDocumentFirst"));
            return;
        }

        tab.Save(); // aktuellen Stand ins Modell schreiben

        var formats = tab is TextTabViewModel
            ? _platform.Documents.TextExportFormats
            : _platform.Documents.BoardExportFormats;

        string? path = _platform.Files.Save(
            Loc.T("Dialog.ExportTitle"), SafeFileName(tab.Title), formats, preferred);
        if (path == null) return;

        ExportResult result;
        try
        {
            result = tab switch
            {
                TextTabViewModel text => _platform.Documents.ExportText(text.Doc, text.Title, path),
                WhiteboardTabViewModel wb => _platform.Documents.ExportBoard(wb.Doc, wb.Title, path),
                _ => new ExportResult([path]),
            };
        }
        catch (Exception ex)
        {
            _platform.Dialogs.Inform(Loc.T("Msg.ExportFailed", ex.Message), DialogSeverity.Warning);
            return;
        }

        var written = result.Written;
        string openTarget = written.Count > 0 ? written[0] : path;
        string info = written.Count > 1
            ? Loc.T("Msg.ExportedPages", written.Count, System.IO.Path.GetDirectoryName(openTarget))
            : Loc.T("Msg.ExportedFile", openTarget);

        if (result.ValidationIssues > 0)
            info += Loc.T("Msg.ValidationHint", result.ValidationIssues);

        // Lieber einmal zu viel sagen als stillschweigend schlechter exportieren: fehlt zu
        // einem Bild die Vorlage, landet nur die kleinere Anzeige-Fassung im PDF – auf
        // Seitenbreite hochgezogen sieht das verpixelt aus, ohne dass der Grund erkennbar wäre.
        if (result.MissingImages > 0)
            info += Loc.T("Msg.ExportImagesMissing", result.MissingImages);

        if (_platform.Dialogs.Confirm(info, DialogSeverity.Information))
            _platform.Shell.OpenExternal(openTarget);
    }

    private static string SafeFileName(string name)
    {
        foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(ch, '_');
        return string.IsNullOrWhiteSpace(name) ? "Dokument" : name;
    }

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
            ? Loc.T("Msg.DeleteFolder", vm.Name)
            : Loc.T("Msg.DeleteItem", vm.Name);
        if (!_platform.Dialogs.Confirm(msg, DialogSeverity.Warning)) return;

        CloseTabsRecursive(vm);
        _db.DeleteItemRecursive(vm.Id);

        var parent = FindParent(vm);
        (parent?.Children ?? RootItems).Remove(vm);
        RefreshPinned();

        // Wurde der gezeigte Galerie-Ordner (oder ein Vorfahre) gelöscht, zum Elternordner hoch
        if (_galleryFolder != null && FindById(_galleryFolder.Id) == null)
            GalleryFolder = parent;
        else
            RebuildGallery();
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
            ApplyInheritedColors();   // Kopien erben die Ordnerfarbe des Ziels
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
            ApplyInheritedColors();   // verschobenes Element erbt die neue Ordnerfarbe
        }

        RebuildGallery();   // Galerie ggf. neu (Element hat den aktuellen Ordner verlassen/betreten)
    }

    private void CopyRecursive(TreeItemViewModel source, TreeItemViewModel? targetFolder)
    {
        var clone = new NoteItem
        {
            Kind = source.Kind,
            Name = source.Item.ParentId == targetFolder?.Id ? source.Name + Loc.T("Item.CopySuffix") : source.Name,
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

        // Galerie/Breadcrumb spiegeln den neuen Namen/Sortierung
        OnPropertyChanged(nameof(GalleryTitle));
        RebuildGallery();
        RebuildBreadcrumb();

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

        if (tab is TextTabViewModel text) Uebernehmen(text);

        OpenTabs.Add(tab);
        SelectedTab = tab;
    }

    /// <summary>
    /// Übernimmt ein Bestandsdokument einmalig ins eigene Modell — beim Öffnen, **still**.
    ///
    /// <para>
    /// <b>Nutzer-Entscheidung 2026-08-05: still übernehmen, Fehler aber zeigen.</b> Was gelingt,
    /// gelingt wortlos — wie die Datenbankübernahme in §4.8, und aus demselben Grund: Ein
    /// Hinweis, den man bei jedem Dokument wegklicken muss, wird nach dem dritten Mal nicht
    /// mehr gelesen. Was **misslingt**, wird benannt: hier kann etwas verlorengehen, denn RTF
    /// und XamlPackage können Dinge tragen, die kein Modell kennt.
    /// </para>
    /// <para>
    /// <b>Das Altfeld bleibt in beiden Fällen unangetastet</b> (§4.22). Eine misslungene
    /// Übernahme ist deshalb kein Datenverlust, sondern ein Versuch, der beim nächsten Öffnen
    /// wiederholt wird — genau das ist gewollt, denn dazwischen kann ein Fehler behoben worden
    /// sein.
    /// </para>
    /// </summary>
    private void Uebernehmen(TextTabViewModel tab)
    {
        var doc = tab.Doc;

        // Schon übernommen, oder es gibt nichts zu übernehmen.
        if (doc.Model.Length > 0 || doc.Rtf.Length == 0) return;

        // Der Linux-Kopf kann es nicht und darf es nicht versuchen — er würde ein leeres
        // Dokument erzeugen und den Inhalt damit scheinbar löschen.
        if (!_platform.Documents.CanMigrate) return;

        var ergebnis = _platform.Documents.Migrate(doc);
        _db.SaveText(doc);

        if (!ergebnis.Migrated)
            _platform.Dialogs.Inform(
                Loc.T("Msg.MigrationFailed", tab.Title, ergebnis.Issue ?? ""),
                DialogSeverity.Warning);
    }

    public void CloseTab(DocumentTabViewModel tab)
    {
        tab.Save();
        int idx = OpenTabs.IndexOf(tab);
        OpenTabs.Remove(tab);
        if (SelectedTab == null && OpenTabs.Count > 0)
            SelectedTab = OpenTabs[Math.Min(idx, OpenTabs.Count - 1)];

        ReleaseMemory(tab);
    }

    /// <summary>
    /// Gibt nach dem Schließen frei, was nur für dieses Dokument im Speicher lag: die
    /// dekodierten Bilder aus dem Cache (bis zu 96 MB) und die Seiten samt Bildbytes am
    /// Dokument selbst. Anschließend ein verdichtender Sammellauf, damit der Speicher auch
    /// beim Betriebssystem ankommt und nicht nur im Heap frei wird.
    /// </summary>
    private static void ReleaseMemory(DocumentTabViewModel tab)
    {
        if (tab is WhiteboardTabViewModel wb)
        {
            ImageCache.Forget(wb.Doc.Pages.Select(p => p.BackgroundImageId));
            ImageCache.Forget(wb.Doc.Pages
                .SelectMany(p => p.Elements)
                .OfType<ImageElement>()
                .Select(im => im.Id));
        }

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    public void SaveAll()
    {
        foreach (var tab in OpenTabs) tab.Save();
    }
}
