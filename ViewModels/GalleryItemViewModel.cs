using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.Core.Models;
using GonkNote.Services;
using GonkNote.Core.Services;

namespace GonkNote.ViewModels;

/// <summary>Ein Eintrag in der Breadcrumb-Leiste des Galeriemodus (null = Wurzel „Dokumente").</summary>
public sealed record BreadcrumbEntry(string Label, TreeItemViewModel? Folder);

/// <summary>
/// Eine Kachel im „Big Picture"-Galeriemodus (an GoodNotes angelehnt): großer Ordner
/// bzw. Vorschau für Notizbuch (Cover), Whiteboard und Textdokument. Kapselt einen
/// Baum-Knoten und lädt die Notizbuch-Cover-Vorschau bei Bedarf nach.
/// </summary>
public sealed class GalleryItemViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    public TreeItemViewModel Tree { get; }

    public GalleryItemViewModel(TreeItemViewModel tree, DatabaseService db)
    {
        Tree = tree;
        _db = db;
    }

    public string Name => Tree.Name;
    public ItemKind Kind => Tree.Kind;
    public bool IsFolder => Tree.IsFolder;
    public bool IsNotebook => Kind == ItemKind.Notebook;
    public bool IsWhiteboard => Kind == ItemKind.Whiteboard;
    public bool IsText => Kind == ItemKind.TextDocument;

    public Visibility StarVisibility => Tree.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
    public Brush IconBrush => Tree.IconBrush;
    public string IconGlyph => Tree.IconGlyph;

    /// <summary>Zuletzt geändert, wie in GoodNotes unter der Kachel.</summary>
    public string DateText => Tree.Item.ModifiedUtc.ToLocalTime().ToString(Loc.T("Gallery.DateFormat"), Loc.Culture);

    // ---------- Notizbuch-Cover (lazy) ----------
    private bool _coverLoaded;
    private ImageSource? _coverImage;
    private Brush? _coverBrush;

    public bool HasCoverImage { get { EnsureCover(); return _coverImage != null; } }
    public ImageSource? CoverImage { get { EnsureCover(); return _coverImage; } }

    /// <summary>Farbverlauf-Cover (falls kein Bild gesetzt ist); Standard Blau→Lila wie beim echten Cover.</summary>
    public Brush CoverBrush { get { EnsureCover(); return _coverBrush!; } }

    private void EnsureCover()
    {
        if (_coverLoaded) return;
        _coverLoaded = true;

        var cover = IsNotebook ? _db.GetCover(Tree.Id) : null;

        var start = ParseColor(cover?.GradientStart, Color.FromRgb(0x1E, 0x3A, 0x8A));
        var end = ParseColor(cover?.GradientEnd, Color.FromRgb(0x7C, 0x3A, 0xED));
        var brush = new LinearGradientBrush(start, end, new Point(0, 0), new Point(1, 1));
        brush.Freeze();
        _coverBrush = brush;

        if (cover?.Image is { Length: > 0 } bytes)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 240;   // Vorschaugröße genügt – spart RAM
                bmp.StreamSource = new System.IO.MemoryStream(bytes);
                bmp.EndInit();
                bmp.Freeze();
                _coverImage = bmp;
            }
            catch { _coverImage = null; }
        }
    }

    private static Color ParseColor(string? hex, Color fallback)
    {
        if (hex is { })
            try { return (Color)ColorConverter.ConvertFromString(hex); } catch { /* Standard */ }
        return fallback;
    }
}
