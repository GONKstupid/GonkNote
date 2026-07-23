using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
            }
        }
    }

    public bool HasSelection => _selected is not null;
    public bool HasNoSelection => _selected is null;
    public string SelectedTitle => _selected?.Name ?? "";
    public string SelectedKindText => _selected is null ? "" : KindText(_selected.Kind);

    public ShellViewModel(string dbPath)
    {
        _db = new DatabaseService(dbPath);
        SeedIfEmpty();
        LoadTree();
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
                parent.Children.Add(vm);
            else
                Roots.Add(vm);
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
        if (level is ObservableCollection<TreeItemVM> roots)
        {
            var sorted = roots
                .OrderByDescending(c => c.IsFolder)
                .ThenByDescending(c => c.IsFavorite)
                .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int cur = roots.IndexOf(sorted[i]);
                if (cur != i) roots.Move(cur, i);
            }
        }
        foreach (var n in level) n.SortChildren();
    }

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
