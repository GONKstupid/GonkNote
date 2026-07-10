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
    Text,
    Shape,
    Pan,
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

    public abstract void Translate(float dx, float dy);
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
}

public class TextElement : WbElement
{
    public float X { get; set; }
    public float Y { get; set; }
    public string Text { get; set; } = "";
    public string Color { get; set; } = "#FF1B2B4B";
    public float FontSize { get; set; } = 18f;

    public override void Translate(float dx, float dy)
    {
        X += dx; Y += dy;
    }
}

/// <summary>Eine Seite. Width/Height = 0 bedeutet unendliche Fläche (Whiteboard-Modus).</summary>
public class WbPage
{
    public List<WbElement> Elements { get; set; } = new();
    public PageBackground Background { get; set; } = PageBackground.Blank;
    public PageShade Shade { get; set; } = PageShade.Auto;
    public float Width { get; set; }
    public float Height { get; set; }
    /// <summary>Cover-Seite eines Notizbuchs (ohne Muster, mit Titel).</summary>
    public bool IsCover { get; set; }

    public bool IsInfinite => Width <= 0 || Height <= 0;
}

/// <summary>Vorlage für neue Notizbuch-Seiten.</summary>
public class PageTemplate
{
    public float Width { get; set; } = WhiteboardDoc.A4Width;
    public float Height { get; set; } = WhiteboardDoc.A4Height;
    public PageBackground Background { get; set; } = PageBackground.Lines;
    public PageShade Shade { get; set; } = PageShade.Auto;
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
}
