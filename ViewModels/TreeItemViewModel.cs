using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
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

    /// <summary>
    /// Vom übergeordneten Ordner geerbte Farbe (vom MainViewModel gesetzt), greift nur,
    /// wenn keine eigene (händisch gesetzte) <see cref="NoteItem.IconColor"/> vorliegt.
    /// </summary>
    public string? InheritedColorHex { get; set; }

    /// <summary>Pinselfarbe des Symbols: eigene Farbe, sonst geerbte Ordnerfarbe, sonst Theme-Türkis.</summary>
    public Brush IconBrush
    {
        get
        {
            string? hex = Item.IconColor ?? InheritedColorHex;
            if (hex is { })
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
                catch { /* ungültiger Wert → Standard */ }
            }
            return (Brush)Application.Current.Resources["Brush.Turquoise"];
        }
    }

    public void RefreshIcon() => OnPropertyChanged(nameof(IconBrush));

    public bool IsPinned => Item.IsPinned;
    public bool IsFavorite => Item.IsFavorite;
    public Visibility FavoriteVisibility => Item.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
    public string PinMenuHeader => Item.IsPinned ? "Nicht mehr anpinnen" : "Anpinnen";
    public string FavoriteMenuHeader => Item.IsFavorite ? "Favorit entfernen" : "Als Favorit";

    public void RefreshPinFavorite()
    {
        OnPropertyChanged(nameof(IsPinned));
        OnPropertyChanged(nameof(IsFavorite));
        OnPropertyChanged(nameof(FavoriteVisibility));
        OnPropertyChanged(nameof(PinMenuHeader));
        OnPropertyChanged(nameof(FavoriteMenuHeader));
    }

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
