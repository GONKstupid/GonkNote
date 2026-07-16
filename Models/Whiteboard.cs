namespace GonkNote.Models;

/// <summary>Aktives Zeichenwerkzeug im Whiteboard.</summary>
public enum ToolType
{
    Pen,
    SmoothPen,
    Pencil,
    Highlighter,
    Eraser,
    Lasso,
    Move,     // Verschieben (V): Objekte direkt anklicken/verschieben (wie Photoshop)
    Text,
    Shape,
    Sticky,
    Sticker,  // Bild-Aufkleber aus der Sammlung
    Pan,      // Hand (H): Leinwand verschieben
}

public enum StrokeKind
{
    Pen,
    Pencil,
    Highlighter,
}

public enum ShapeKind
{
    Line,
    Arrow,
    Rectangle,
    Ellipse,
    Triangle,
}

public enum PageBackground
{
    Blank,
    Lines,
    Grid,
    Dots,
}

/// <summary>Farbton der Seite, unabhängig vom App-Theme.</summary>
public enum PageShade
{
    Auto,
    Light,
    Dark,
}

/// <summary>Ein Punkt einer Stiftlinie inkl. Stylus-Druck (0..1).</summary>
public class WbPoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float P { get; set; } = 0.5f;

    public WbPoint() { }
    public WbPoint(float x, float y, float p) { X = x; Y = y; P = p; }
}

/// <summary>Basisklasse aller Whiteboard-Elemente. LiteDB speichert den konkreten Typ per _type-Feld.</summary>
public abstract class WbElement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Drehung im Uhrzeigersinn in Grad (um den Elementmittelpunkt). 0 = keine.</summary>
    public float Rotation { get; set; }

    public abstract void Translate(float dx, float dy);

    /// <summary>Skaliert die Geometrie um den Faktor <paramref name="f"/> herum um den Pivot (px,py).</summary>
    public abstract void Scale(float f, float px, float py);
}

/// <summary>
/// Elemente mit rechteckiger Position und Größe (Bild, Notizzettel). Ermöglicht
/// gemeinsame Behandlung von Auswahl-Griff und Skalierung.
/// </summary>
public interface IBoxElement
{
    float X { get; set; }
    float Y { get; set; }
    float Width { get; set; }
    float Height { get; set; }
}

public class StrokeElement : WbElement
{
    public List<WbPoint> Points { get; set; } = new();
    public string Color { get; set; } = "#FF1B2B4B";
    public float Width { get; set; } = 2.5f;
    public StrokeKind Kind { get; set; } = StrokeKind.Pen;

    public override void Translate(float dx, float dy)
    {
        foreach (var p in Points) { p.X += dx; p.Y += dy; }
    }

    public override void Scale(float f, float px, float py)
    {
        foreach (var p in Points) { p.X = px + (p.X - px) * f; p.Y = py + (p.Y - py) * f; }
        Width *= f;
    }
}

public class ShapeElement : WbElement
{
    public ShapeKind Shape { get; set; }
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
    public string Color { get; set; } = "#FF1B2B4B";
    public float StrokeWidth { get; set; } = 2.5f;
    /// <summary>Füllfarbe oder null für keine Füllung.</summary>
    public string? Fill { get; set; }

    public override void Translate(float dx, float dy)
    {
        X1 += dx; X2 += dx; Y1 += dy; Y2 += dy;
    }

    public override void Scale(float f, float px, float py)
    {
        X1 = px + (X1 - px) * f; Y1 = py + (Y1 - py) * f;
        X2 = px + (X2 - px) * f; Y2 = py + (Y2 - py) * f;
        StrokeWidth *= f;
    }
}

public class TextElement : WbElement
{
    public float X { get; set; }
    public float Y { get; set; }
    public string Text { get; set; } = "";
    public string Color { get; set; } = "#FF000000";
    public float FontSize { get; set; } = 18f;

    /// <summary>Hintergrundfarbe hinter dem Text; null = transparent.</summary>
    public string? Background { get; set; }

    public string FontFamily { get; set; } = "Segoe UI";

    public override void Translate(float dx, float dy)
    {
        X += dx; Y += dy;
    }

    public override void Scale(float f, float px, float py)
    {
        X = px + (X - px) * f; Y = py + (Y - py) * f;
        FontSize = Math.Max(4f, FontSize * f);
    }
}

/// <summary>
/// Eingebettetes Rasterbild. Data enthält PNG- oder JPEG-Bytes; große Bilder
/// werden beim Import auf max. 2048 px Kantenlänge verkleinert (RAM-/DB-Größe).
/// SVG wird beim Import gerastert.
/// </summary>
public class ImageElement : WbElement, IBoxElement
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public override void Translate(float dx, float dy)
    {
        X += dx; Y += dy;
    }

    public override void Scale(float f, float px, float py)
    {
        X = px + (X - px) * f; Y = py + (Y - py) * f;
        Width *= f; Height *= f;
    }
}

/// <summary>
/// Notizzettel (Klebezettel): farbige Karte mit umbrochenem Text, frei verschieb-
/// und skalierbar. Größe wird gespeichert; der Text wird beim Zeichnen umbrochen.
/// </summary>
public class StickyNoteElement : WbElement, IBoxElement
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 200f;
    public float Height { get; set; } = 200f;
    public string Text { get; set; } = "";
    /// <summary>Zettelfarbe (Kartenhintergrund).</summary>
    public string Color { get; set; } = "#FFFEF08A";
    /// <summary>Textfarbe.</summary>
    public string TextColor { get; set; } = "#FF1F2937";
    public float FontSize { get; set; } = 16f;
    public string FontFamily { get; set; } = "Segoe UI";

    public override void Translate(float dx, float dy)
    {
        X += dx; Y += dy;
    }

    public override void Scale(float f, float px, float py)
    {
        X = px + (X - px) * f; Y = py + (Y - py) * f;
        Width = Math.Max(60f, Width * f);
        Height = Math.Max(60f, Height * f);
        FontSize = Math.Max(6f, FontSize * f);
    }
}

/// <summary>Eine Seite. Width/Height = 0 bedeutet unendliche Fläche (Whiteboard-Modus).</summary>
public class WbPage
{
    public List<WbElement> Elements { get; set; } = new();
    public PageBackground Background { get; set; } = PageBackground.Blank;
    /// <summary>Standard ist Hell – Seiten sollen unabhängig vom App-Theme hell wirken.</summary>
    public PageShade Shade { get; set; } = PageShade.Light;
    public float Width { get; set; }
    public float Height { get; set; }
    /// <summary>Cover-Seite eines Notizbuchs (ohne Muster, mit Titel).</summary>
    public bool IsCover { get; set; }

    /// <summary>
    /// Hintergrundbild der Seite (z. B. importierte PDF-Seite, JPEG/PNG-Bytes).
    /// Wird seitenfüllend hinter den Elementen gezeichnet und ist weder
    /// verschieb- noch radierbar; ersetzt das Muster.
    /// </summary>
    public byte[]? BackgroundImage { get; set; }

    /// <summary>Cache-Schlüssel fürs Rendering des Hintergrundbilds.</summary>
    public Guid BackgroundImageId { get; set; }

    public bool IsInfinite => Width <= 0 || Height <= 0;
}

/// <summary>Gestaltung der Cover-Seite eines Notizbuchs.</summary>
public class CoverStyle
{
    public string GradientStart { get; set; } = "#1E3A8A";
    public string GradientEnd { get; set; } = "#7C3AED";
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>Optionales Bild als Cover (PNG/JPEG-Bytes); ersetzt den Farbverlauf.</summary>
    public byte[]? Image { get; set; }

    /// <summary>Cache-Schlüssel fürs Rendering; wird bei jedem Bildwechsel neu vergeben.</summary>
    public Guid ImageId { get; set; } = Guid.NewGuid();
}

/// <summary>Vorlage für neue Notizbuch-Seiten.</summary>
public class PageTemplate
{
    public float Width { get; set; } = WhiteboardDoc.A4Width;
    public float Height { get; set; } = WhiteboardDoc.A4Height;
    public PageBackground Background { get; set; } = PageBackground.Lines;
    public PageShade Shade { get; set; } = PageShade.Light;
}

/// <summary>Inhalt eines Whiteboards oder Notizbuchs (Id = NoteItem.Id).</summary>
public class WhiteboardDoc
{
    // Seitenformate bei 96 DPI
    public const float A4Width = 794f;
    public const float A4Height = 1123f;
    public const float A3Width = 1123f;
    public const float A3Height = 1587f;

    public Guid Id { get; set; }
    public List<WbPage> Pages { get; set; } = new();

    /// <summary>Vorlage für neue Seiten; null = A4 liniert.</summary>
    public PageTemplate? NewPageTemplate { get; set; }

    /// <summary>Gestaltung des Covers; null = Standard (Blau-Lila-Verlauf, Segoe UI).</summary>
    public CoverStyle? Cover { get; set; }

    public static WhiteboardDoc NewWhiteboard(Guid id) => new()
    {
        Id = id,
        Pages = { new WbPage { Background = PageBackground.Dots } },
    };

    public static WhiteboardDoc NewNotebook(Guid id) => new()
    {
        Id = id,
        Pages =
        {
            new WbPage { IsCover = true, Width = A4Width, Height = A4Height },
            NewNotebookPage(),
        },
    };

    public static WbPage NewNotebookPage() => new()
    {
        Background = PageBackground.Lines,
        Width = A4Width,
        Height = A4Height,
    };

    public WbPage PageFromTemplate()
    {
        var t = NewPageTemplate;
        if (t == null) return NewNotebookPage();
        return new WbPage
        {
            Width = t.Width,
            Height = t.Height,
            Background = t.Background,
            Shade = t.Shade,
        };
    }
}

/// <summary>
/// Inhalt eines Textdokuments (Id = NoteItem.Id).
/// Feld heißt historisch "Rtf", enthält aber je nach Alter RTF oder XamlPackage
/// (ZIP, erkennbar am "PK"-Header) – Letzteres erhält auch eingebettete Bilder.
/// </summary>
public class TextDoc
{
    public Guid Id { get; set; }
    public byte[] Rtf { get; set; } = Array.Empty<byte>();

    // ---------- Seiteneinrichtung (Neu: Word-Grundfunktionen) ----------
    // String-Felder haben null-sichere Setter: LiteDB kann leere Strings als
    // BSON-Null speichern (EmptyStringToNull) – null darf hier nie ankommen.

    private string _pageFormat = "A4";
    private string _headerText = "";
    private string _footerText = "";

    /// <summary>Papierformat: "A4", "A5", "A3" oder "Letter".</summary>
    public string PageFormat
    {
        get => _pageFormat;
        set => _pageFormat = string.IsNullOrEmpty(value) ? "A4" : value;
    }

    /// <summary>Querformat statt Hochformat.</summary>
    public bool Landscape { get; set; }

    // Seitenränder in Zentimetern (Word-Standard: 2,5/2,5/2,5/2 – hier 2 rundum)
    public double MarginLeftCm { get; set; } = 2;
    public double MarginTopCm { get; set; } = 2;
    public double MarginRightCm { get; set; } = 2;
    public double MarginBottomCm { get; set; } = 2;

    /// <summary>
    /// Kopf-/Fußzeilentext. Platzhalter: {SEITE}, {SEITEN}, {DATUM}, {TITEL}.
    /// Leer = keine Kopf-/Fußzeile.
    /// </summary>
    public string HeaderText { get => _headerText; set => _headerText = value ?? ""; }
    public string FooterText { get => _footerText; set => _footerText = value ?? ""; }

    /// <summary>Kopf-/Fußzeile auf der ersten Seite unterdrücken (Deckblatt).</summary>
    public bool SuppressHeaderOnFirstPage { get; set; }

    /// <summary>Seitenfüllendes Hintergrundbild/Wasserzeichen (hinter dem Text), null = keins.</summary>
    public byte[]? WatermarkImage { get; set; }

    /// <summary>Deckkraft des Wasserzeichens (0–1).</summary>
    public double WatermarkOpacity { get; set; } = 1.0;

    /// <summary>Seitenumbruch-Markierungen im Editor anzeigen (Näherung, s. Layout-Tab).</summary>
    public bool ShowPageBreaks { get; set; }
}
