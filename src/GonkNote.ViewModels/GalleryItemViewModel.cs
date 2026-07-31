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

    public bool IsFavorite => Tree.IsFavorite;
    public string? IconColorHex => Tree.IconColorHex;
    public string IconGlyph => Tree.IconGlyph;

    /// <summary>Zuletzt geändert, wie in GoodNotes unter der Kachel.</summary>
    public string DateText => Tree.Item.ModifiedUtc.ToLocalTime().ToString(Loc.T("Gallery.DateFormat"), Loc.Culture);

    // ---------- Notizbuch-Cover (lazy) ----------
    //
    // Seit Phase 2 liefert die Kachel nur noch Daten: die Bild-Bytes und die beiden
    // Verlaufsfarben als Hex-Text. Bitmap und Pinsel baut der Kopf daraus
    // (BytesToImageConverter, GradientBrushConverter) — beides gibt es unter Avalonia
    // nicht, wohl aber Bytes und Farben.

    private bool _coverLoaded;
    private byte[]? _coverImage;
    private string? _gradientStart, _gradientEnd;

    public bool HasCoverImage { get { EnsureCover(); return _coverImage != null; } }

    /// <summary>Cover-Bild als kodierte Bytes; <c>null</c>, wenn es keines gibt.</summary>
    public byte[]? CoverImageData { get { EnsureCover(); return _coverImage; } }

    /// <summary>
    /// Farbverlauf-Cover (falls kein Bild gesetzt ist), als Hex-Text. <c>null</c> heißt
    /// „nimm den Standard" — Blau→Lila wie beim echten Cover.
    /// </summary>
    public string? CoverGradientStart { get { EnsureCover(); return _gradientStart; } }
    public string? CoverGradientEnd { get { EnsureCover(); return _gradientEnd; } }

    private void EnsureCover()
    {
        if (_coverLoaded) return;
        _coverLoaded = true;

        var cover = IsNotebook ? _db.GetCover(Tree.Id) : null;
        if (cover == null) return;

        _gradientStart = cover.GradientStart;
        _gradientEnd = cover.GradientEnd;

        // Nicht direkt aus dem Datensatz: nach dem ersten Speichern liegt das Cover im
        // Blob-Speicher und das Feld ist leer – die Kachel bliebe sonst dauerhaft ohne Bild.
        if (ImageCache.Bytes(cover.ImageId, cover.Image) is { Length: > 0 } bytes)
            _coverImage = bytes;
    }
}
