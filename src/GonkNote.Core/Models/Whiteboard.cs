using System.Text.Json.Serialization;

namespace GonkNote.Core.Models;

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

/// <summary>Ein Punkt einer Stiftlinie inkl. Stylus-Druck (0..1) und Neigung.</summary>
public class WbPoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float P { get; set; } = 0.5f;

    /// <summary>
    /// Neigung des Stifts in Grad, −90…+90. <c>0</c> heißt senkrecht — <b>und ebenso
    /// „nicht bekannt"</b>: eine Maus, ein Finger und ein Digitizer ohne Neigungsachse
    /// liefern alle 0, und der Renderer behandelt sie deshalb gleich wie einen senkrecht
    /// gehaltenen Stift. Das ist kein Verlust: senkrecht ist der Normalfall.
    ///
    /// <para>
    /// <b>Warum die beiden Felder Bestandsdateien nicht anfassen:</b>
    /// <c>WhenWritingDefault</c> lässt sie beim Schreiben weg, solange sie 0 sind. Ein
    /// Dokument ohne Neigung wird also byteweise geschrieben wie bisher, und ein
    /// Dokument von vor dieser Änderung liest sich mit 0 ein. Das zählt hier mehr als
    /// anderswo: <see cref="WbPoint"/> ist der mit Abstand häufigste Datensatz der ganzen
    /// App — in der echten Datenbank standen 6308 Druckpunkte auf 160 Striche
    /// (HANDOFF §4.8). Zwei bedingungslos geschriebene Felder je Punkt wären ein knappes
    /// Drittel mehr Datei für einen Wert, den die meisten Geräte gar nicht liefern.
    /// </para>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float TX { get; set; }

    /// <inheritdoc cref="TX"/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float TY { get; set; }

    public WbPoint() { }
    public WbPoint(float x, float y, float p) { X = x; Y = y; P = p; }

    public WbPoint(float x, float y, float p, float tx, float ty)
    {
        X = x; Y = y; P = p; TX = tx; TY = ty;
    }
}

/// <summary>
/// Basisklasse aller Whiteboard-Elemente. Der konkrete Typ steht im Feld <c>_type</c>.
/// <para>
/// <b>Die Zeichenketten unten sind Datenformat, kein Codedetail — sie dürfen sich nie
/// ändern.</b> Sie stehen wörtlich so in jeder gespeicherten Datei, seit LiteDB sie als
/// "Namensraum.Typ, Assembly" geschrieben hat; seit dem Umbau auf SQLite/Json schreibt
/// <c>System.Text.Json</c> genau dieselben Werte. Weicht einer davon ab, lässt sich das
/// betroffene Element nicht mehr laden — und der Fehler sieht aus wie ein leeres
/// Whiteboard, nicht wie ein Absturz.
/// </para>
/// Wer einen Namensraum oder den Assemblynamen ändert, ändert deshalb **nicht** diese
/// Zeichenketten mit. Wer einen neuen Elementtyp ergänzt, trägt ihn hier nach demselben
/// Muster ein — sonst wirft das Speichern (<see cref="JsonUnknownDerivedTypeHandling"/>).
/// Wächter: <c>AlteTypnamenTests</c>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "_type")]
[JsonDerivedType(typeof(StrokeElement), "GonkNote.Core.Models.StrokeElement, GonkNote.Core")]
[JsonDerivedType(typeof(ShapeElement), "GonkNote.Core.Models.ShapeElement, GonkNote.Core")]
[JsonDerivedType(typeof(TextElement), "GonkNote.Core.Models.TextElement, GonkNote.Core")]
[JsonDerivedType(typeof(ImageElement), "GonkNote.Core.Models.ImageElement, GonkNote.Core")]
[JsonDerivedType(typeof(StickyNoteElement), "GonkNote.Core.Models.StickyNoteElement, GonkNote.Core")]
public abstract class WbElement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Drehung im Uhrzeigersinn in Grad (um den Elementmittelpunkt). 0 = keine.</summary>
    public float Rotation { get; set; }

    public abstract void Translate(float dx, float dy);

    /// <summary>Skaliert die Geometrie um den Faktor <paramref name="f"/> herum um den Pivot (px,py).</summary>
    public abstract void Scale(float f, float px, float py);
}

// **Hier stand bis Phase 5 ein `IBoxElement`** — X/Y/Breite/Höhe für Bild und Notizzettel,
// mit der Begründung „ermöglicht gemeinsame Behandlung von Auswahl-Griff und Skalierung".
// **Genau diese Begründung ist abgelaufen** (§7, Muster B): Griffe und Skalierung gehen seit
// §4.51 über `WbElement.Scale`, das jedes Element selbst beantwortet — ein Strich anders als
// ein Kasten. Gelesen hat die Schnittstelle zuletzt nur `ResizeBoxAction`, und der ist mit
// ihr zusammen weggefallen. **Das Dateiformat ist nicht betroffen:** `JsonDerivedType` steht
// an den konkreten Klassen, nicht an einer Schnittstelle.

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

    /// <inheritdoc cref="StickyNoteElement.FontFamily"/>
    public string FontFamily { get; set; } = Theming.Fonts.Standard.Family(Theming.FontRole.Handwriting);

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
public class ImageElement : WbElement
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
public class StickyNoteElement : WbElement
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

    /// <summary>
    /// Die Schrift dieses Elements. <b>Nur die Vorgabe hat sich mit §4.26 geändert</b> —
    /// bestehende Elemente tragen ihren gespeicherten Wert und behalten ihn, es gibt keinen
    /// Migrationsschritt. Wer „Segoe UI" gespeichert hat, bekommt „Segoe UI".
    /// </summary>
    public string FontFamily { get; set; } = Theming.Fonts.Standard.Family(Theming.FontRole.Handwriting);

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

    /// <summary>
    /// Ob die Seite ein importiertes Hintergrundbild hat. Bewusst **nicht** an
    /// <see cref="BackgroundImage"/> abzulesen: das Feld ist nach dem ersten Speichern leer,
    /// weil das Bild dann im Blob-Speicher liegt. Wer die Bytes prüft, hält jede gespeicherte
    /// PDF-Seite für leer.
    /// </summary>
    /// <remarks>
    /// <c>[JsonIgnore]</c>, weil System.Text.Json auch nur lesbare Eigenschaften schreibt:
    /// ohne das stünde ein abgeleiteter Wert mit in der Datei und sähe später wie
    /// gespeicherte Wahrheit aus.
    /// </remarks>
    [JsonIgnore]
    public bool HasBackgroundImage => BackgroundImageId != Guid.Empty || BackgroundImage is { Length: > 0 };

    [JsonIgnore]
    public bool IsInfinite => Width <= 0 || Height <= 0;
}

/// <summary>Gestaltung der Cover-Seite eines Notizbuchs.</summary>
public class CoverStyle
{
    public string GradientStart { get; set; } = "#1E3A8A";
    public string GradientEnd { get; set; } = "#7C3AED";

    /// <summary>
    /// Die Schrift des Cover-Titels — die Rolle „Display" (§4.26). <inheritdoc
    /// cref="StickyNoteElement.FontFamily" path="/summary/b"/>
    /// </summary>
    public string FontFamily { get; set; } = Theming.Fonts.Standard.Family(Theming.FontRole.Display);

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

    /// <summary>Gestaltung des Covers; null = Standard (Blau-Lila-Verlauf, Space Grotesk).</summary>
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

    /// <summary>
    /// Der Inhalt im **Altformat**: RTF oder ein WPF-<c>XamlPackage</c> (ZIP, erkennbar am
    /// „PK"). Das Feld heißt historisch <c>Rtf</c> und trägt beides.
    /// <para>
    /// <b>Es wird nie überschrieben</b>, auch nicht nach der Übernahme — dieselbe Regel, aus
    /// der <c>gonknote.db</c> neben <c>gonknote.sqlite</c> liegen bleibt (HANDOFF §4.8, §4.22).
    /// Solange es dasteht, ist eine misslungene Übernahme kein Datenverlust, sondern ein
    /// Versuch, der wiederholt werden kann.
    /// </para>
    /// </summary>
    public byte[] Rtf { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Derselbe Inhalt im **eigenen** Dokumentmodell (Kennung <c>GNTD</c>, siehe
    /// <c>TdFormatIo</c>). Leer = noch nicht übernommen.
    /// <para>
    /// <b>Ein neues Feld neben <see cref="Rtf"/> und nicht statt dessen</b> — additiv, wie die
    /// Datenbankübernahme in §4.8. Wer voll ist, führt: steht hier etwas, wird daraus gelesen;
    /// sonst aus <see cref="Rtf"/>.
    /// </para>
    /// </summary>
    public byte[] Model { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Was bei der letzten Übernahme schiefging; leer = nichts.
    /// <para>
    /// <b>Er steht im Dokument und nicht nur in einem Hinweisfenster</b>, das man weggeklickt
    /// hat: Ein Fehler, den niemand mehr nachlesen kann, ist bei der nächsten Frage „warum
    /// sieht das anders aus?" verschwunden. Die Übernahme läuft still — **ein Fehler dabei
    /// nicht** (Nutzer-Entscheidung 2026-08-05, §4.22).
    /// </para>
    /// </summary>
    public string MigrationIssue { get => _migrationIssue; set => _migrationIssue = value ?? ""; }

    /// <summary>
    /// Bilder dieses Dokuments im Blob-Speicher. Wird beim Speichern neu geschrieben und
    /// dient dem Aufräumlauf: Blobs, auf die kein Dokument mehr zeigt, sind Müll.
    /// </summary>
    public List<Guid> Images { get; set; } = new();

    // ---------- Seiteneinrichtung (Neu: Word-Grundfunktionen) ----------
    // String-Felder haben null-sichere Setter: LiteDB konnte leere Strings als
    // BSON-Null speichern (EmptyStringToNull) – null darf hier nie ankommen.
    // System.Text.Json macht aus "" nie null, der Fall ist damit von selbst erledigt;
    // die Setter bleiben trotzdem stehen, denn eine **migrierte** Altdatei kann null
    // mitbringen, und der Wächter dafür ist ein Test, kein gutes Gedächtnis.

    private string _pageFormat = "A4";
    private string _headerText = "";
    private string _footerText = "";
    private string _migrationIssue = "";

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
