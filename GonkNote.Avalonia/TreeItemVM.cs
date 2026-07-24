using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using GonkNote.Models;

namespace GonkNote.Avalonia;

/// <summary>
/// Ein Knoten im Ordnerbaum der Avalonia-Shell. Avalonia-native Portierung von
/// <c>GonkNote.ViewModels.TreeItemViewModel</c> (nutzt Avalonia-Brushes statt WPF-Media).
/// </summary>
public sealed class TreeItemVM : ObservableObject
{
    /// <summary>Standard-Symbolfarbe (Türkis), wenn weder eigene noch geerbte Farbe vorliegt.</summary>
    public static readonly IBrush DefaultIconBrush = new SolidColorBrush(Color.Parse("#17A2A2"));

    public NoteItem Item { get; }
    public ObservableCollection<TreeItemVM> Children { get; } = new();

    /// <summary>Elternknoten (in <c>LoadTree</c> gesetzt) — für Breadcrumb/Aufwärtsnavigation.</summary>
    public TreeItemVM? Parent { get; set; }

    private bool _isExpanded;
    private bool _isSelected;
    private bool _isRenaming;
    private string _editName = "";

    public TreeItemVM(NoteItem item) => Item = item;

    public Guid Id => Item.Id;
    public bool IsFolder => Item.IsFolder;
    public ItemKind Kind => Item.Kind;

    public string Name
    {
        get => Item.Name;
        set { if (Item.Name != value) { Item.Name = value; OnPropertyChanged(); OnPropertyChanged(nameof(Label)); } }
    }

    public bool IsExpanded { get => _isExpanded; set => Set(ref _isExpanded, value); }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }

    /// <summary>Inline-Umbenennung aktiv (Baum zeigt dann eine TextBox statt des Namens).</summary>
    public bool IsRenaming
    {
        get => _isRenaming;
        set { if (Set(ref _isRenaming, value)) OnPropertyChanged(nameof(IsNotRenaming)); }
    }

    public bool IsNotRenaming => !_isRenaming;

    /// <summary>Puffer der Inline-Bearbeitung (Escape verwirft, Enter übernimmt).</summary>
    public string EditName { get => _editName; set => Set(ref _editName, value); }

    /// <summary>Startet die Inline-Umbenennung mit dem aktuellen Namen als Vorgabe.</summary>
    public void BeginRename()
    {
        EditName = Name;
        IsRenaming = true;
    }

    /// <summary>Cross-platform-Glyph (Emoji) je Typ — Segoe-Fluent-Font gibt es unter Linux nicht.</summary>
    public string Glyph => Kind switch
    {
        ItemKind.Folder => "📁",
        ItemKind.Notebook => "📓",
        ItemKind.Whiteboard => "🖊",
        ItemKind.TextDocument => "📄",
        _ => "•",
    };

    public string Label => $"{Glyph}  {Name}";

    /// <summary>Typ-Beschriftung für Galerie-Kacheln.</summary>
    public string KindLabel => Kind switch
    {
        ItemKind.Folder => "Ordner",
        ItemKind.Notebook => "Notizbuch",
        ItemKind.Whiteboard => "Whiteboard",
        ItemKind.TextDocument => "Textdokument",
        _ => "",
    };

    /// <summary>Vom Elternordner geerbte Farbe (greift nur ohne eigene <see cref="NoteItem.IconColor"/>).</summary>
    public string? InheritedColorHex { get; set; }

    /// <summary>Symbolfarbe: eigene Farbe, sonst geerbte Ordnerfarbe, sonst Standard-Türkis.</summary>
    public IBrush IconBrush
    {
        get
        {
            string? hex = Item.IconColor ?? InheritedColorHex;
            if (!string.IsNullOrWhiteSpace(hex))
            {
                try { return new SolidColorBrush(Color.Parse(hex)); }
                catch { /* ungültiger Wert → Standard */ }
            }
            return DefaultIconBrush;
        }
    }

    public void RefreshIcon() => OnPropertyChanged(nameof(IconBrush));

    public bool IsFavorite => Item.IsFavorite;

    /// <summary>Ordner zuerst, dann Favoriten, dann alphabetisch — wie in der WPF-App.</summary>
    public void SortChildren()
    {
        var sorted = Children
            .OrderByDescending(c => c.IsFolder)
            .ThenByDescending(c => c.IsFavorite)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            int cur = Children.IndexOf(sorted[i]);
            if (cur != i) Children.Move(cur, i);
        }
    }
}
