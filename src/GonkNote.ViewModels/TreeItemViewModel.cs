using System.Collections.ObjectModel;
using GonkNote.Core.Models;
using GonkNote.Services;

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

    // Hier stand bis zum 2026-08-12 "IconGlyph" -- vier Zeichencodes aus "Segoe Fluent
    // Icons" (§4.31). Eine Windows-Systemschrift, mitten in der Assembly, die laut §4.2
    // gerade **kein** WPF kennen soll. Der Compiler konnte es nicht sehen: Es waren
    // Zeichenketten und kein Verweis.
    //
    // Die Zuordnung steht jetzt als AppIcons.ForKind in Core -- beide Köpfe fragen dieselbe
    // Stelle, und der Ordnerbaum bindet an "Kind" statt an eine schon fertige Antwort.

    /// <summary>
    /// Vom übergeordneten Ordner geerbte Farbe (vom MainViewModel gesetzt), greift nur,
    /// wenn keine eigene (händisch gesetzte) <see cref="NoteItem.IconColor"/> vorliegt.
    /// </summary>
    public string? InheritedColorHex { get; set; }

    /// <summary>
    /// Symbolfarbe als Hex-Text: eigene Farbe, sonst geerbte Ordnerfarbe, sonst
    /// <c>null</c> für „nimm die Theme-Farbe".
    /// <para>
    /// Bis Phase 2 stand hier ein WPF-<c>Brush</c>, geholt aus
    /// <c>Application.Current.Resources</c>. Eine Farbe ist plattformneutral, ein Pinsel
    /// nicht — den baut jetzt <c>HexToBrushConverter</c> im Kopf.
    /// </para>
    /// </summary>
    public string? IconColorHex => Item.IconColor ?? InheritedColorHex;

    public void RefreshIcon() => OnPropertyChanged(nameof(IconColorHex));

    public bool IsPinned => Item.IsPinned;
    public bool IsFavorite => Item.IsFavorite;
    public string PinMenuHeader => Loc.T(Item.IsPinned ? "Tree.Unpin" : "Tree.Pin");
    public string FavoriteMenuHeader => Loc.T(Item.IsFavorite ? "Tree.UnmarkFavorite" : "Tree.MarkFavorite");

    public void RefreshPinFavorite()
    {
        OnPropertyChanged(nameof(IsPinned));
        OnPropertyChanged(nameof(IsFavorite));
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
