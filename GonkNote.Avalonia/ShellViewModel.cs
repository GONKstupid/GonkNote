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

    private WhiteboardDoc? _currentDoc;

    /// <summary>Lädt die erste Seite des gewählten Boards (bzw. leert die Ansicht).</summary>
    private void LoadBoardPage()
    {
        if (_selected is { Kind: ItemKind.Whiteboard or ItemKind.Notebook } item)
        {
            _currentDoc = _db.GetBoard(item.Item);
            if (_currentDoc.Pages.Count == 0) _currentDoc.Pages.Add(new WbPage());
            _pageIndex = 0;
            CurrentPage = _currentDoc.Pages[0];
            Undo = new UndoStack();          // Undo-Verlauf gilt je Seite
            RaisePageState();
        }
        else
        {
            _currentDoc = null;
            CurrentPage = null;
        }
    }

    /// <summary>Speichert das aktuell geöffnete Board (nach jedem fertigen Strich).</summary>
    public void SaveCurrentBoard()
    {
        if (_currentDoc is { } doc) _db.SaveBoard(doc);
    }

    /// <summary>Undo/Redo des aktuell geöffneten Boards (Stack aus GonkNote.Core).</summary>
    public UndoStack Undo { get; private set; } = new();

    public bool CanUndo => Undo.CanUndo;
    public bool CanRedo => Undo.CanRedo;

    // ---- Seiten (Mehrseitigkeit) --------------------------------------------------------

    private int _pageIndex;

    public int PageIndex => _pageIndex;
    public int PageCount => _currentDoc?.Pages.Count ?? 0;
    public string PageLabel => PageCount == 0 ? "" : $"Seite {_pageIndex + 1} / {PageCount}";

    public bool CanPrevPage => _pageIndex > 0;
    public bool CanNextPage => _currentDoc is { } d && _pageIndex + 1 < d.Pages.Count;

    public void GoToPage(int index)
    {
        if (_currentDoc is not { } doc || index < 0 || index >= doc.Pages.Count) return;
        _pageIndex = index;
        CurrentPage = doc.Pages[index];
        Undo = new UndoStack();          // Undo gilt je Seite
        RaisePageState();
    }

    public void PrevPage() => GoToPage(_pageIndex - 1);
    public void NextPage() => GoToPage(_pageIndex + 1);

    /// <summary>Hängt eine neue Seite an (übernimmt Format/Muster der aktuellen) und springt hin.</summary>
    public void AddPage()
    {
        if (_currentDoc is not { } doc) return;
        var cur = CurrentPage;
        doc.Pages.Add(new WbPage
        {
            Width = cur?.Width ?? 0,
            Height = cur?.Height ?? 0,
            Background = cur?.Background ?? PageBackground.Blank,
            Shade = cur?.Shade ?? PageShade.Light,
        });
        SaveCurrentBoard();
        GoToPage(doc.Pages.Count - 1);
    }

    private void RaisePageState()
    {
        OnPropertyChanged(nameof(PageIndex));
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(PageLabel));
        OnPropertyChanged(nameof(CanPrevPage));
        OnPropertyChanged(nameof(CanNextPage));
        RaiseUndoState();
    }

    /// <summary>Registriert ein neu eingefügtes Element (Form/Zettel/Text) als Undo-Schritt.</summary>
    public void OnElementAdded(WbElement el)
    {
        if (_currentPage is not { } page) return;
        Undo.Push(page, new AddElementsAction(new[] { el }));
        SaveCurrentBoard();
        RaiseUndoState();
    }

    /// <summary>Registriert das Verschieben einer Auswahl als einen Undo-Schritt.</summary>
    public void OnSelectionMoved(List<WbElement> els, float dx, float dy)
    {
        if (_currentPage is not { } page) return;
        Undo.Push(page, new MoveElementsAction(els, dx, dy));
        SaveCurrentBoard();
        RaiseUndoState();
    }

    /// <summary>Registriert das Skalieren einer Auswahl als einen Undo-Schritt.</summary>
    public void OnSelectionScaled(List<WbElement> els, float factor, float px, float py)
    {
        if (_currentPage is not { } page) return;
        Undo.Push(page, new ScaleElementsAction(els, factor, px, py));
        SaveCurrentBoard();
        RaiseUndoState();
    }

    /// <summary>Registriert eingefügte Elemente (Einfügen/Duplizieren) als einen Undo-Schritt.</summary>
    public void OnElementsAdded(List<WbElement> els)
    {
        if (_currentPage is not { } page || els.Count == 0) return;
        Undo.Push(page, new AddElementsAction(els));
        SaveCurrentBoard();
        RaiseUndoState();
    }

    /// <summary>Registriert das Löschen einer Auswahl als einen Undo-Schritt.</summary>
    public void OnSelectionDeleted(List<(WbElement El, int Index)> removed)
    {
        if (_currentPage is not { } page || removed.Count == 0) return;
        Undo.Push(page, new RemoveElementsAction(removed));
        SaveCurrentBoard();
        RaiseUndoState();
    }

    /// <summary>Registriert einen neu gezeichneten Strich als Undo-Schritt und speichert.</summary>
    public void OnStrokeDrawn(StrokeElement stroke)
    {
        if (_currentPage is not { } page) return;
        Undo.Push(page, new AddElementsAction(new[] { stroke }));
        SaveCurrentBoard();
        RaiseUndoState();
    }

    /// <summary>Registriert einen Radier-Zug (punktgenau) als einen Undo-Schritt und speichert.</summary>
    public void OnElementsErased(List<EraseStep> steps)
    {
        if (_currentPage is not { } page) return;
        Undo.Push(page, new PartialEraseAction(steps));
        SaveCurrentBoard();
        RaiseUndoState();
    }

    /// <summary>Macht den letzten Schritt rückgängig; liefert true, wenn sich etwas geändert hat.</summary>
    public bool UndoLast()
    {
        if (Undo.Undo() is null) return false;
        SaveCurrentBoard();
        RaiseUndoState();
        return true;
    }

    public bool RedoLast()
    {
        if (Undo.Redo() is null) return false;
        SaveCurrentBoard();
        RaiseUndoState();
        return true;
    }

    private void RaiseUndoState()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
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
