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
                LoadBoardPage();                       // vor ShowDocument/ShowWhiteboard-Meldung
                OnPropertyChanged(nameof(ShowWhiteboard));
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

    /// <summary>Dokument-Kontext (Platzhalter) für Typen ohne eigene Ansicht (z. B. Text).</summary>
    public bool ShowDocument => _selected is { IsFolder: false } && !ShowWhiteboard;

    /// <summary>Whiteboard-/Notizbuch-Ansicht zeigen (Schritt 4).</summary>
    public bool ShowWhiteboard =>
        _selected is { Kind: ItemKind.Whiteboard or ItemKind.Notebook } && _currentPage is not null;

    private WbPage? _currentPage;

    /// <summary>Aktuell dargestellte Seite des gewählten Whiteboards/Notizbuchs.</summary>
    public WbPage? CurrentPage
    {
        get => _currentPage;
        private set { if (Set(ref _currentPage, value)) OnPropertyChanged(nameof(ShowWhiteboard)); }
    }

    /// <summary>Lädt die erste Seite des gewählten Boards (bzw. leert die Ansicht).</summary>
    private void LoadBoardPage()
    {
        if (_selected is { Kind: ItemKind.Whiteboard or ItemKind.Notebook } item)
        {
            var doc = _db.GetBoard(item.Item);
            CurrentPage = doc.Pages.Count > 0 ? doc.Pages[0] : null;
        }
        else CurrentPage = null;
    }

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
        var skizzen = new NoteItem { Kind = ItemKind.Whiteboard, Name = "Skizzen", ParentId = projekte.Id };
        _db.UpsertItem(skizzen);
        _db.UpsertItem(new NoteItem { Kind = ItemKind.Notebook, Name = "Ideen", ParentId = projekte.Id, IsFavorite = true });

        SeedDemoBoard(skizzen);
    }

    /// <summary>Legt ein Beispiel-Whiteboard an, damit die Skia-Ansicht etwas zu zeigen hat.</summary>
    private void SeedDemoBoard(NoteItem item)
    {
        var doc = _db.GetBoard(item);
        if (doc.Pages.Count == 0) doc.Pages.Add(new WbPage());
        var page = doc.Pages[0];
        page.Background = PageBackground.Dots;

        // Freihand-Strich (mit Druckverlauf)
        var stroke = new StrokeElement { Color = "#2563EB", Width = 4f, Kind = StrokeKind.Pen };
        for (int i = 0; i <= 40; i++)
        {
            float t = i / 40f;
            stroke.Points.Add(new WbPoint
            {
                X = 80 + t * 320,
                Y = 150 + MathF.Sin(t * MathF.PI * 2) * 60,
                P = 0.35f + 0.65f * MathF.Sin(t * MathF.PI),
            });
        }
        page.Elements.Add(stroke);

        page.Elements.Add(new ShapeElement
        {
            Shape = ShapeKind.Rectangle, X1 = 90, Y1 = 250, X2 = 260, Y2 = 350,
            Color = "#DC2626", StrokeWidth = 3f,
        });
        page.Elements.Add(new ShapeElement
        {
            Shape = ShapeKind.Arrow, X1 = 280, Y1 = 300, X2 = 400, Y2 = 260,
            Color = "#16A34A", StrokeWidth = 3f,
        });
        page.Elements.Add(new GonkNote.Models.TextElement
        {
            X = 90, Y = 80, Text = "Gemeinsamer Skia-Renderer", Color = "#111827", FontSize = 22f,
        });
        page.Elements.Add(new StickyNoteElement
        {
            X = 430, Y = 150, Width = 190, Height = 130, Color = "#FDE68A",
            Text = "Dieselben Zeichenroutinen wie in der WPF-App — aus GonkNote.Core.",
            TextColor = "#3F3F46", FontSize = 14f,
        });

        _db.SaveBoard(doc);
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
