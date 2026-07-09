using System.Collections.ObjectModel;
using GonkNote.Models;

namespace GonkNote.ViewModels;

/// <summary>Ein Knoten im Ordnerbaum.</summary>
public sealed class TreeItemViewModel : ObservableObject
{
    public NoteItem Item { get; }
    public ObservableCollection<TreeItemViewModel> Children { get; } = new();

    private bool _isExpanded;
    private bool _isSelected;
    private bool _isRenaming;

    public TreeItemViewModel(NoteItem item) => Item = item;

    public Guid Id => Item.Id;
    public bool IsFolder => Item.IsFolder;
    public ItemKind Kind => Item.Kind;

    public string Name
    {
        get => Item.Name;
        set
        {
            if (Item.Name == value) return;
            Item.Name = value;
            OnPropertyChanged();
        }
    }

    public bool IsExpanded { get => _isExpanded; set => Set(ref _isExpanded, value); }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }

    public bool IsRenaming
    {
        get => _isRenaming;
        set
        {
            if (value && !_isRenaming) NameBeforeRename = Name;
            Set(ref _isRenaming, value);
        }
    }

    /// <summary>Name vor Beginn der Umbenennung (für Escape = verwerfen).</summary>
    public string? NameBeforeRename { get; private set; }

    /// <summary>Icon-Glyph (Segoe Fluent Icons) je nach Typ.</summary>
    public string IconGlyph => Kind switch
    {
        ItemKind.Folder => "",       // Ordner
        ItemKind.Notebook => "",     // Buch
        ItemKind.Whiteboard => "",   // Stift
        ItemKind.TextDocument => "", // Dokument
        _ => "",
    };

    public void SortChildren()
    {
        var sorted = Children
            .OrderByDescending(c => c.IsFolder)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            int cur = Children.IndexOf(sorted[i]);
            if (cur != i) Children.Move(cur, i);
        }
    }
}
