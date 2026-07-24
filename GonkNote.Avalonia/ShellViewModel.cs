using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using GonkNote.Models;
using GonkNote.Services;

namespace GonkNote.Avalonia;

/// <summary>
/// ViewModel der Avalonia-Shell: lädt den Ordnerbaum über den echten <see cref="DatabaseService"/>
/// (dieselbe LiteDB wie die WPF-App), baut die Farbvererbung nach und hält die aktuelle Auswahl.
/// Bewusst schlank gehalten — der große WPF-<c>MainViewModel</c> (Tabs/Commands/Dialoge) wird
/// NICHT 1:1 portiert (WPF-gekoppelt); Doku-Ansichten folgen in Schritt 4/5 (HANDOFF §9.4).
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
    private readonly DatabaseService _db;

    public ObservableCollection<TreeItemVM> Roots { get; } = new();

    /// <summary>Kacheln der „Big-Picture"-Galerie: Inhalt des gewählten Ordners bzw. die Wurzeln.</summary>
    public ObservableCollection<TreeItemVM> GalleryItems { get; } = new();

    /// <summary>Öffnet ein Element (Kachel- oder Baumklick): Ordner rein-navigieren, Dokument = Kontext.</summary>
    public ICommand OpenItem { get; }

    /// <summary>Pfadleiste über der Galerie („Alle Dokumente › Ordner › …"), Segmente sind klickbar.</summary>
    public ObservableCollection<BreadcrumbEntry> Breadcrumb { get; } = new();

    /// <summary>Springt zu einem Breadcrumb-Segment (Ziel <c>null</c> = Wurzelansicht).</summary>
    public ICommand NavigateCrumb { get; }

    private TreeItemVM? _selected;
    public TreeItemVM? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(HasNoSelection));
                OnPropertyChanged(nameof(SelectedTitle));
                OnPropertyChanged(nameof(SelectedKindText));
                OnPropertyChanged(nameof(ShowGallery));
                OnPropertyChanged(nameof(ShowDocument));
                OnPropertyChanged(nameof(GalleryTitle));
                RebuildGallery();
                RebuildBreadcrumb();
            }
        }
    }

    public bool HasSelection => _selected is not null;
    public bool HasNoSelection => _selected is null;
    public string SelectedTitle => _selected?.Name ?? "";
    public string SelectedKindText => _selected is null ? "" : KindText(_selected.Kind);

    /// <summary>Galerie zeigen, wenn nichts oder ein Ordner gewählt ist.</summary>
    public bool ShowGallery => _selected is null || _selected.IsFolder;

    /// <summary>Dokument-Kontext zeigen, wenn ein Nicht-Ordner gewählt ist.</summary>
    public bool ShowDocument => _selected is { IsFolder: false };

    public string GalleryTitle => _selected is null ? "Alle Dokumente" : _selected.Name;

    public bool GalleryEmpty => GalleryItems.Count == 0;

    public ShellViewModel(string dbPath)
    {
        _db = new DatabaseService(dbPath);
        OpenItem = new RelayCommand(p =>
        {
            if (p is not TreeItemVM vm) return;
            if (vm.IsFolder) vm.IsExpanded = true; // im Baum aufklappen
            Selected = vm;
        });
        NavigateCrumb = new RelayCommand(p =>
        {
            if (p is not BreadcrumbEntry crumb) return;
            if (crumb.Target is { } t) t.IsExpanded = true;
            Selected = crumb.Target; // null = Wurzelansicht
        });
        SeedIfEmpty();
        LoadTree();
        RebuildGallery();
        RebuildBreadcrumb();
    }

    /// <summary>
    /// Baut die Pfadleiste zur aktuellen Galerie-Position: „Alle Dokumente" + Ordnerkette.
    /// Bei einem gewählten Dokument zählt dessen Elternordner als Position.
    /// </summary>
    private void RebuildBreadcrumb()
    {
        Breadcrumb.Clear();
        Breadcrumb.Add(new BreadcrumbEntry("Alle Dokumente", null));

        var folder = _selected is { IsFolder: true } ? _selected : _selected?.Parent;
        var chain = new List<TreeItemVM>();
        for (var n = folder; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();
        foreach (var n in chain) Breadcrumb.Add(new BreadcrumbEntry(n.Name, n));
    }

    /// <summary>Übernimmt eine Inline-Umbenennung (leer/unverändert = verwerfen) und speichert.</summary>
    public void CommitRename(TreeItemVM vm)
    {
        if (!vm.IsRenaming) return;
        string neu = (vm.EditName ?? "").Trim();
        vm.IsRenaming = false;
        if (neu.Length == 0 || neu == vm.Name) return;

        vm.Name = neu;
        _db.UpsertItem(vm.Item);                 // Persistenz in dieselbe LiteDB wie die WPF-App
        vm.Parent?.SortChildren();
        if (vm.Parent is null) SortRoots();
        RebuildGallery();
        RebuildBreadcrumb();
        OnPropertyChanged(nameof(SelectedTitle));
        OnPropertyChanged(nameof(GalleryTitle));
    }

    public void CancelRename(TreeItemVM vm) => vm.IsRenaming = false;

    // ---- Anlegen / Löschen / Favorit ---------------------------------------------------

    /// <summary>Legt ein neues Element im aktuellen Ordner an (Wurzel, wenn keiner gewählt).</summary>
    public TreeItemVM CreateItem(ItemKind kind)
    {
        var parent = _selected is { IsFolder: true } ? _selected : _selected?.Parent;

        var item = new NoteItem { Kind = kind, Name = DefaultName(kind), ParentId = parent?.Id };
        _db.UpsertItem(item);

        var vm = new TreeItemVM(item)
        {
            Parent = parent,
            InheritedColorHex = parent?.Item.IconColor ?? parent?.InheritedColorHex,
        };
        vm.RefreshIcon();

        if (parent is null) { Roots.Add(vm); SortRoots(); }
        else { parent.Children.Add(vm); parent.SortChildren(); parent.IsExpanded = true; }

        RebuildGallery();
        return vm;
    }

    /// <summary>Löscht ein Element samt Unterbaum (DB + Ansicht).</summary>
    public void DeleteItem(TreeItemVM vm)
    {
        _db.DeleteItemRecursive(vm.Id);

        var parent = vm.Parent;
        if (parent is null) Roots.Remove(vm); else parent.Children.Remove(vm);

        // War das Gelöschte (oder ein Vorfahre davon) ausgewählt? → auf den Elternordner zurück.
        if (_selected is not null && (_selected == vm || IsDescendantOf(_selected, vm)))
            Selected = parent;                      // löst Galerie/Breadcrumb-Neuaufbau aus
        else { RebuildGallery(); RebuildBreadcrumb(); }
    }

    /// <summary>Schaltet den Favoriten-Status um (wirkt auf Sortierung) und speichert.</summary>
    public void ToggleFavorite(TreeItemVM vm)
    {
        vm.Item.IsFavorite = !vm.Item.IsFavorite;
        _db.UpsertItem(vm.Item);
        vm.RefreshFavorite();

        if (vm.Parent is { } p) p.SortChildren(); else SortRoots();
        RebuildGallery();
    }

    private static bool IsDescendantOf(TreeItemVM node, TreeItemVM ancestor)
    {
        for (var n = node.Parent; n is not null; n = n.Parent)
            if (n == ancestor) return true;
        return false;
    }

    private static string DefaultName(ItemKind kind) => kind switch
    {
        ItemKind.Folder => "Neuer Ordner",
        ItemKind.Notebook => "Neues Notizbuch",
        ItemKind.Whiteboard => "Neues Whiteboard",
        ItemKind.TextDocument => "Neues Textdokument",
        _ => "Neu",
    };

    /// <summary>Füllt die Galerie mit den Kindern des gewählten Ordners (bzw. den Wurzeln).</summary>
    private void RebuildGallery()
    {
        GalleryItems.Clear();
        var source = _selected is { IsFolder: true } ? _selected.Children : Roots;
        foreach (var vm in source) GalleryItems.Add(vm);
        OnPropertyChanged(nameof(GalleryEmpty));
    }

    private void SeedIfEmpty()
    {
        if (_db.GetAllItems().Count != 0) return;
        var schule = new NoteItem { Kind = ItemKind.Folder, Name = "Schule", IconColor = "#E23D57" };
        _db.UpsertItem(schule);
        _db.UpsertItem(new NoteItem { Kind = ItemKind.Notebook, Name = "Biologie", ParentId = schule.Id });
        _db.UpsertItem(new NoteItem { Kind = ItemKind.TextDocument, Name = "Notizen", ParentId = schule.Id });
        var projekte = new NoteItem { Kind = ItemKind.Folder, Name = "Projekte", IconColor = "#3D82E2" };
        _db.UpsertItem(projekte);
        _db.UpsertItem(new NoteItem { Kind = ItemKind.Whiteboard, Name = "Skizzen", ParentId = projekte.Id });
        _db.UpsertItem(new NoteItem { Kind = ItemKind.Notebook, Name = "Ideen", ParentId = projekte.Id, IsFavorite = true });
    }

    private void LoadTree()
    {
        Roots.Clear();
        var all = _db.GetAllItems();
        var byId = all.ToDictionary(i => i.Id, i => new TreeItemVM(i));

        foreach (var vm in byId.Values)
        {
            if (vm.Item.ParentId is Guid pid && byId.TryGetValue(pid, out var parent))
            {
                vm.Parent = parent;          // für Breadcrumb/Aufwärtsnavigation
                parent.Children.Add(vm);
            }
            else
            {
                vm.Parent = null;
                Roots.Add(vm);
            }
        }

        // Farbvererbung: Kinder ohne eigene Farbe erben die (eigene oder geerbte) Ordnerfarbe.
        foreach (var root in Roots) ApplyInheritedColors(root, null);

        // Sortierung (Ordner → Favoriten → Name) rekursiv.
        SortRecursive(Roots);
        foreach (var root in Roots) root.IsExpanded = true;
    }

    private static void ApplyInheritedColors(TreeItemVM node, string? inherited)
    {
        node.InheritedColorHex = inherited;
        node.RefreshIcon();
        string? passDown = node.Item.IconColor ?? inherited;
        foreach (var child in node.Children) ApplyInheritedColors(child, passDown);
    }

    private static void SortRecursive(IList<TreeItemVM> level)
    {
        foreach (var n in level) SortRecursive(n.Children);
        if (level is ObservableCollection<TreeItemVM> roots) SortCollection(roots);
        foreach (var n in level) n.SortChildren();
    }

    /// <summary>Sortiert eine Ebene in situ: Ordner → Favoriten → Name.</summary>
    private static void SortCollection(ObservableCollection<TreeItemVM> level)
    {
        var sorted = level
            .OrderByDescending(c => c.IsFolder)
            .ThenByDescending(c => c.IsFavorite)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            int cur = level.IndexOf(sorted[i]);
            if (cur != i) level.Move(cur, i);
        }
    }

    private void SortRoots() => SortCollection(Roots);

    private static string KindText(ItemKind k) => k switch
    {
        ItemKind.Folder => "Ordner",
        ItemKind.Notebook => "Notizbuch",
        ItemKind.Whiteboard => "Whiteboard",
        ItemKind.TextDocument => "Textdokument",
        _ => "",
    };

    public static string DefaultDbPath =>
        Path.Combine(Path.GetTempPath(), "gonk-avalonia-shell.db");
}

/// <summary>Ein Segment der Pfadleiste. <see cref="Target"/> <c>null</c> = Wurzelansicht.</summary>
public sealed class BreadcrumbEntry
{
    public string Label { get; }
    public TreeItemVM? Target { get; }

    public BreadcrumbEntry(string label, TreeItemVM? target)
    {
        Label = label;
        Target = target;
    }
}
