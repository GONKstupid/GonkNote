using System.Collections.Generic;
using System.Linq;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using SkiaSharp;

namespace GonkNote.Core.Rendering;

/// <summary>
/// Der <b>einzige</b> Ort, an dem aus einem Schriftnamen ein <see cref="SKTypeface"/> wird —
/// für das Whiteboard, den Umbruch (<c>TdSkiaMeasure</c>) und den Dokumentzeichner
/// (<c>TdRenderer</c>) gleichermaßen (§4.26).
///
/// <para>
/// <b>Warum das einer sein muss.</b> Bis §4.26 fragten drei Stellen unabhängig voneinander
/// <c>SKTypeface.FromFamilyName</c> — und das sieht **nur Systemschriften**. Eine mitgelieferte
/// Schrift hätte den Dokumentzeichner damit nie erreicht, und die Zeile, die der Umbruch misst,
/// wäre nicht die Zeile, die der Zeichner malt. Dasselbe Muster wie in §4.13: **wegkommen soll
/// die zweite Rechnung, nicht die zweite Bezeichnung.**
/// </para>
/// <para>
/// <b>Mitgeliefert schlägt System.</b> Erst die Registratur aus <c>Assets/Fonts/</c>, dann die
/// Schriften des Rechners, dann die Rückfallkette des Schemas. Nur so sieht dasselbe Dokument
/// unter Windows, Linux und iPadOS gleich aus — und nur so zeichnet der Avalonia-Kopf sein
/// Chrome in derselben Schrift wie seine Zeichenfläche.
/// </para>
/// </summary>
public static class WbFonts
{
    /// <summary>
    /// Das Schriftschema. Der Kopf setzt es beim Start über
    /// <see cref="GonkNote.Core.Platform.IFontProvider"/>; ohne Zutun gilt
    /// <see cref="Theming.Fonts.Standard"/>.
    /// <para>
    /// <b>Ein Wechsel wirft den Zwischenspeicher weg</b> — anders als bis §4.26, wo ein
    /// später gesetzter Wert wirkungslos blieb, statt sichtbar zu scheitern.
    /// </para>
    /// </summary>
    public static Theming.FontScheme Schema
    {
        get => _schema;
        set
        {
            if (ReferenceEquals(_schema, value)) return;
            _schema = value;
            _aufgeloest.Clear();
        }
    }

    private static Theming.FontScheme _schema = Theming.Fonts.Standard;

    /// <summary>Die Oberflächenschrift — die Familie der Rolle <see cref="Theming.FontRole.Ui"/>.</summary>
    public static string UiFamily => _schema.Family(Theming.FontRole.Ui);

    /// <summary>Die Familie einer Rolle.</summary>
    public static string FamilyOf(Theming.FontRole rolle) => _schema.Family(rolle);

    public static SKTypeface Regular => Family(UiFamily);

    public static SKTypeface Bold => Family(UiFamily, bold: true);

    // ==================== Die Registratur der mitgelieferten Schriften ====================

    private static readonly Lock _tor = new();
    private static Dictionary<(string Fam, bool Bold, bool Italic), SKTypeface>? _mitgeliefert;
    private static readonly Dictionary<(string Fam, bool Bold, bool Italic), SKTypeface> _aufgeloest = new();

    /// <summary>
    /// Wo die mitgelieferten Schriften liegen: <c>Fonts/</c> neben dem Programm. Über
    /// <see cref="AppContext.BaseDirectory"/> und nicht über das Arbeitsverzeichnis — das
    /// steht beim Start über eine Verknüpfung woanders.
    /// </summary>
    public static string FontOrdner { get; set; } =
        Path.Combine(AppContext.BaseDirectory, Theming.Fonts.Ordner);

    /// <summary>
    /// Welche mitgelieferten Familien tatsächlich geladen werden konnten. <b>Für den Wächter</b>
    /// — und für die Frage, warum ein Kopf anders aussieht als der andere.
    /// </summary>
    public static IReadOnlyCollection<string> GeladeneFamilien =>
        [.. Registratur().Keys.Select(k => k.Fam).Distinct()];

    private static Dictionary<(string Fam, bool Bold, bool Italic), SKTypeface> Registratur()
    {
        if (_mitgeliefert is { } fertig) return fertig;

        lock (_tor)
        {
            if (_mitgeliefert is { } zweiterBlick) return zweiterBlick;

            var tabelle = new Dictionary<(string Fam, bool Bold, bool Italic), SKTypeface>();
            foreach (var familie in Theming.Fonts.Mitgeliefert)
                foreach (var schnitt in familie.Cuts)
                {
                    // **Fehlt eine Datei, fehlt ein Schnitt — nicht die App.** Ein
                    // unvollständiger Ausgabeordner ist dasselbe Bild wie ein fehlender Blob
                    // (§4.21): ärgerlich, aber kein Programmierfehler.
                    string pfad = Path.Combine(FontOrdner, familie.Ordner, schnitt.Datei);
                    if (!File.Exists(pfad)) continue;

                    var tf = SKTypeface.FromFile(pfad);
                    if (tf is null) continue;

                    // Der erste gewinnt: „Regular" steht in der Liste vor „SemiBold", und für
                    // (nicht fett, nicht kursiv) ist Regular der richtige Schnitt.
                    var schluessel = (familie.Family, schnitt.Bold, schnitt.Italic);
                    if (!tabelle.ContainsKey(schluessel)) tabelle[schluessel] = tf;
                }

            _mitgeliefert = tabelle;
            return tabelle;
        }
    }

    /// <summary>
    /// Typeface zu Familie, Stärke und Neigung. <b>Gecacht</b> — ein <c>SKTypeface</c> zu
    /// öffnen kostet einen Dateizugriff, und der Umbruch fragt je Wort danach.
    /// </summary>
    public static SKTypeface Family(string? name, bool bold = false, bool italic = false)
    {
        string familie = string.IsNullOrWhiteSpace(name) ? UiFamily : name!;
        var schluessel = (familie, bold, italic);

        lock (_tor)
        {
            if (_aufgeloest.TryGetValue(schluessel, out var da)) return da;
            var tf = Aufloesen(familie, bold, italic);
            _aufgeloest[schluessel] = tf;
            return tf;
        }
    }

    /// <summary>
    /// Die Reihenfolge, in der gesucht wird — und sie ist die ganze Entscheidung:
    /// <b>mitgeliefert, dann System, dann Rückfallkette, dann Skias Vorgabe.</b>
    /// <para>
    /// <b>Kein Wurf, wenn nichts passt.</b> Ein Dokument soll in einer Ersatzschrift stehen und
    /// nicht gar nicht — dieselbe Regel wie bisher in <c>TdSkiaMeasure</c>.
    /// </para>
    /// </summary>
    private static SKTypeface Aufloesen(string familie, bool bold, bool italic)
    {
        var tabelle = Registratur();

        // 1. Mitgeliefert, mit Rückfall innerhalb der Familie: genau der Schnitt, sonst der
        //    fette, sonst der kursive, sonst der gewöhnliche. Eine Familie ohne Kursiv (Space
        //    Grotesk) liefert damit den aufrechten Schnitt statt gar nichts.
        foreach (var (b, i) in new[] { (bold, italic), (bold, false), (false, italic), (false, false) })
            if (tabelle.TryGetValue((familie, b, i), out var eigen)) return eigen;

        var stil = new SKFontStyle(
            bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        // 2. Die Schriften des Rechners — ein Bestandsdokument mit „Segoe UI" bekommt sie
        //    unter Windows weiterhin (§4.14: der gespeicherte Wert gewinnt).
        if (SKTypeface.FromFamilyName(familie, stil) is { } system &&
            string.Equals(system.FamilyName, familie, StringComparison.OrdinalIgnoreCase))
            return system;

        // 3. Die Rückfallkette des Schemas, wieder mitgeliefert vor System.
        foreach (var ersatz in _schema.Rueckfall)
        {
            foreach (var (b, i) in new[] { (bold, italic), (false, false) })
                if (tabelle.TryGetValue((ersatz, b, i), out var eigen)) return eigen;

            if (SKTypeface.FromFamilyName(ersatz, stil) is { } kette &&
                string.Equals(kette.FamilyName, ersatz, StringComparison.OrdinalIgnoreCase))
                return kette;
        }

        return SKTypeface.FromFamilyName(null, stil) ?? SKTypeface.Default;
    }

    /// <summary>
    /// Schrift für die Textausgabe. Seit SkiaSharp 3 liegen Größe und Schriftart in
    /// <see cref="SKFont"/> statt in <see cref="SKPaint"/> — das Paint trägt nur noch
    /// Farbe, Kantenglättung und Effekte. Der Aufrufer gibt das Ergebnis frei.
    /// </summary>
    public static SKFont Font(string? name, float size) => new(Family(name), size);

    /// <summary>Dasselbe mit Stärke und Neigung — für Umbruch und Dokumentzeichner.</summary>
    public static SKFont Font(string? name, float size, bool bold, bool italic) =>
        new(Family(name, bold, italic), size);
}

/// <summary>
/// Plattformneutrales Zeichnen der Whiteboard-/Notizbuch-Elemente auf einen
/// <see cref="SKCanvas"/>. Aus <c>Views/WhiteboardView.xaml.cs</c> herausgelöst, damit WPF,
/// der PDF-Export **und** der Avalonia-Port dieselbe Darstellung verwenden (HANDOFF §9.3e).
/// Enthält bewusst KEINE Eingabe-/Werkzeuglogik — nur Rendering.
/// </summary>
public static class WbRenderer
{
    /// <summary>Innenabstand des Zettels zwischen Kartenrand und Text (Canvas-Einheiten).</summary>
    public const float StickyPad = 14f;

    /// <summary>Breite des weichen Schattenrands (Canvas-Einheiten).</summary>
    private const int ShadowFringe = 18;

    /// <summary>
    /// Abtastung beim Skalieren von Bildern: bilinear mit Mipmaps. Entspricht dem, was bis
    /// SkiaSharp 2 <c>SKFilterQuality.Medium</c> war — seit 3.x wird die Qualität nicht mehr
    /// am Paint eingestellt, sondern beim Zeichnen mitgegeben. Eine Wahrheit für Ansicht,
    /// PDF-Export und OCR-Vorverarbeitung.
    /// </summary>
    public static readonly SKSamplingOptions MediumSampling =
        new(SKFilterMode.Linear, SKMipmapMode.Linear);

    /// <summary>
    /// Abtastung für Export und Verkleinerung großer Vorlagen: bikubisch (Mitchell) —
    /// der Nachfolger von <c>SKFilterQuality.High</c>.
    /// </summary>
    public static readonly SKSamplingOptions HighSampling = new(SKCubicResampler.Mitchell);

    private static SKImage? _stickyShadowImg;   // Notizzettel (Radius 6, Alpha 45)

    // ---- Farben / Pfade -----------------------------------------------------------------

    public static SKColor ParseColor(string hex)
    {
        try { return SKColor.Parse(hex); }
        catch { return SKColors.Gray; }
    }

    private static SKPath BuildSmoothPath(List<WbPoint> pts)
    {
        var path = new SKPath();
        if (pts.Count == 0) return path;
        path.MoveTo(pts[0].X, pts[0].Y);
        for (int i = 1; i < pts.Count - 1; i++)
        {
            float mx = (pts[i].X + pts[i + 1].X) / 2f;
            float my = (pts[i].Y + pts[i + 1].Y) / 2f;
            path.QuadTo(pts[i].X, pts[i].Y, mx, my);
        }
        if (pts.Count > 1)
            path.LineTo(pts[^1].X, pts[^1].Y);
        return path;
    }

    public static (SKPoint A, SKPoint B, SKPoint C) TrianglePoints(ShapeElement sh)
    {
        float minX = Math.Min(sh.X1, sh.X2), maxX = Math.Max(sh.X1, sh.X2);
        float minY = Math.Min(sh.Y1, sh.Y2), maxY = Math.Max(sh.Y1, sh.Y2);
        return (new SKPoint((minX + maxX) / 2f, minY),
                new SKPoint(maxX, maxY),
                new SKPoint(minX, maxY));
    }

    // ---- Maße ---------------------------------------------------------------------------

    public static SKRect TextBounds(TextElement t)
    {
        using var font = WbFonts.Font(t.FontFamily, t.FontSize);
        var lines = t.Text.Length == 0 ? new[] { " " } : t.Text.Split('\n');
        float w = 10;
        foreach (var line in lines)
            w = Math.Max(w, font.MeasureText(line.Length == 0 ? " " : line));
        float h = lines.Length * t.FontSize * 1.35f;
        return SKRect.Create(t.X, t.Y, w, h);
    }

    public static SKRect ElementBounds(WbElement el)
    {
        switch (el)
        {
            case StrokeElement s:
            {
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (var p in s.Points)
                {
                    minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                    minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                }
                var r = new SKRect(minX, minY, maxX, maxY);
                r.Inflate(s.Width / 2f, s.Width / 2f);
                return r;
            }
            case ShapeElement sh:
            {
                var r = new SKRect(Math.Min(sh.X1, sh.X2), Math.Min(sh.Y1, sh.Y2),
                                   Math.Max(sh.X1, sh.X2), Math.Max(sh.Y1, sh.Y2));
                r.Inflate(sh.StrokeWidth / 2f, sh.StrokeWidth / 2f);
                return r;
            }
            case TextElement t:
                return TextBounds(t);
            case ImageElement im:
                return SKRect.Create(im.X, im.Y, im.Width, im.Height);
            case StickyNoteElement sn:
                return SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height);
            default:
                return SKRect.Empty;
        }
    }

    // ---- Schatten (gecachtes Nine-Patch, konstante Kosten) -------------------------------

    private static SKImage ShadowNinePatch(ref SKImage? cache, float cornerRadius, byte alpha)
    {
        if (cache != null) return cache;
        int inset = ShadowFringe + (int)MathF.Ceiling(cornerRadius) + 2;
        int size = inset * 2 + 32;   // 32 px dehnbarer Kern
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        var c = surface.Canvas;
        c.Clear(SKColors.Transparent);
        using var p = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black.WithAlpha(alpha),
            ImageFilter = SKImageFilter.CreateBlur(6, 6),
        };
        var r = SKRect.Create(ShadowFringe, ShadowFringe, size - 2 * ShadowFringe, size - 2 * ShadowFringe);
        if (cornerRadius > 0) c.DrawRoundRect(r, cornerRadius, cornerRadius, p);
        else c.DrawRect(r, p);
        cache = surface.Snapshot();
        return cache;
    }

    /// <summary>Zeichnet den weichen Schatten unter <paramref name="rect"/> (3 px nach unten versetzt).</summary>
    public static void DrawCachedShadow(SKCanvas canvas, SKRect rect, float cornerRadius, byte alpha, ref SKImage? cache)
    {
        var img = ShadowNinePatch(ref cache, cornerRadius, alpha);
        int inset = ShadowFringe + (int)MathF.Ceiling(cornerRadius) + 2;
        var center = new SKRectI(inset, inset, img.Width - inset, img.Height - inset);
        var dst = SKRect.Create(rect.Left - ShadowFringe, rect.Top - ShadowFringe + 3,
            rect.Width + 2 * ShadowFringe, rect.Height + 2 * ShadowFringe);
        // Filtermodus seit SkiaSharp 3 ausdrücklich: ohne ihn ist der Aufruf zweideutig.
        // Gedehnt wird nur die Mittelzeile/-spalte, die entlang der Dehnrichtung konstant
        // ist — das Bild ändert sich dadurch nicht.
        canvas.DrawImageNinePatch(img, center, dst, SKFilterMode.Linear, null);
    }

    // ---- Elemente -----------------------------------------------------------------------

    /// <summary>Zeichnet ein Element inkl. seiner Drehung.</summary>
    public static void DrawElement(SKCanvas canvas, WbElement el)
    {
        if (el.Rotation != 0f)
        {
            var b = ElementBounds(el);
            canvas.Save();
            canvas.RotateDegrees(el.Rotation, b.MidX, b.MidY);
            DrawElementCore(canvas, el);
            canvas.Restore();
        }
        else
        {
            DrawElementCore(canvas, el);
        }
    }

    public static void DrawElementCore(SKCanvas canvas, WbElement el)
    {
        switch (el)
        {
            case StrokeElement s: DrawStroke(canvas, s); break;
            case ShapeElement sh: DrawShape(canvas, sh, sh.Color, sh.StrokeWidth); break;
            case TextElement t: DrawText(canvas, t); break;
            case ImageElement im: DrawImage(canvas, im); break;
            case StickyNoteElement sn: DrawSticky(canvas, sn); break;
        }
    }

    public static void DrawImage(SKCanvas canvas, ImageElement im)
    {
        var rect = SKRect.Create(im.X, im.Y, im.Width, im.Height);
        var img = ImageCache.Get(im.Id, im.Data);
        if (img == null)
        {
            // Nicht dekodierbar: Platzhalter, damit das Element auswählbar bleibt
            using var ph = new SKPaint { Color = SKColors.Gray.WithAlpha(60) };
            canvas.DrawRect(rect, ph);
            return;
        }
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawImage(img, rect, MediumSampling, paint);
    }

    public static void DrawStroke(SKCanvas canvas, StrokeElement s)
    {
        if (s.Points.Count == 0) return;
        var color = ParseColor(s.Color);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color,
        };

        switch (s.Kind)
        {
            case StrokeKind.Highlighter:
                paint.Color = color.WithAlpha(70);
                paint.StrokeCap = SKStrokeCap.Butt;
                paint.StrokeWidth = s.Width;
                using (var path = BuildSmoothPath(s.Points))
                    canvas.DrawPath(path, paint);
                break;

            case StrokeKind.Pencil:
                DrawPencil(canvas, s, color);
                break;

            default: // Stift: Druck steuert die Strichbreite je Segment
                if (s.Points.Count == 2 &&
                    Math.Abs(s.Points[0].X - s.Points[1].X) < 0.5f &&
                    Math.Abs(s.Points[0].Y - s.Points[1].Y) < 0.5f)
                {
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(s.Points[0].X, s.Points[0].Y, s.Width * 0.7f, paint);
                    return;
                }
                for (int i = 0; i < s.Points.Count - 1; i++)
                {
                    var a = s.Points[i];
                    var b = s.Points[i + 1];
                    float p = (a.P + b.P) / 2f;
                    paint.StrokeWidth = s.Width * (0.35f + 1.1f * p);
                    canvas.DrawLine(a.X, a.Y, b.X, b.Y, paint);
                }
                break;
        }
    }

    /// <summary>
    /// Bleistift-Anmutung: Graphit deckt nie voll und setzt sich körnig in die Papierstruktur.
    /// Umgesetzt in drei günstigen Skia-Durchgängen (statt einer zackigen Discrete-Linie):
    /// weiche Kernlinie, fein aufgerauter Rand und eine gestempelte Körnung entlang eines
    /// leicht verrauschten Pfades.
    /// </summary>
    /// <summary>
    /// Wie stark der Strich durch die <b>Neigung</b> des Stifts verbreitert wird.
    /// <c>1</c> = senkrecht gehalten, also unverändert.
    ///
    /// <para>
    /// <b>Warum nur der Bleistift das benutzt:</b> eine schräg gehaltene Mine legt sich um
    /// und zieht eine breitere, weichere Spur — das ist der Grund, warum man beim
    /// Schraffieren den Stift kippt. Ein Fineliner hat eine feste Spitze und wird davon
    /// nicht breiter, ein Textmarker hat eine Keilspitze, deren Verhalten von der
    /// Drehung um die eigene Achse abhinge, und die liefert kein Digitizer. Beides
    /// nachzuahmen wäre erfunden, nicht beobachtet.
    /// </para>
    /// <para>
    /// <b>Und warum die Pixelhashes davon unberührt bleiben:</b> ohne Neigungsangabe ist
    /// <see cref="WbPoint.TX"/>/<see cref="WbPoint.TY"/> gleich 0, der Faktor damit exakt
    /// <c>1</c> und jede Rechnung darunter eine Multiplikation mit Eins. Bestandsdokumente,
    /// die Golden-Files aus Phase 1 und alles, was eine Maus zeichnet, sehen deshalb
    /// unverändert aus — auch im WPF-Kopf, der denselben Renderer benutzt und für diese
    /// Änderung nicht angefasst werden musste.
    /// </para>
    /// <para>
    /// Genommen wird die <b>mittlere</b> Neigung des Strichs, nicht die je Punkt: die
    /// Körnung des Bleistifts entsteht aus drei Durchgängen über einen gemeinsamen Pfad,
    /// und je Segment eine eigene Breite hieße, den Pfad je Segment neu zu bauen. Ein
    /// Strich wird ohnehin selten in der Mitte umgegriffen.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <c>NeigungVoll</c> ist der Winkel, ab dem die Verbreiterung ihr Maximum erreicht.
    /// 60° ist etwa die Grenze, jenseits derer ein Stift auf dem Blatt liegt statt geführt
    /// zu werden; darüber wird nicht weiter verbreitert.
    /// </remarks>
    public static float TiltWidthFactor(StrokeElement s)
    {
        const float NeigungVoll = 60f;
        const float MaxZuwachs = 0.9f;

        if (s.Points.Count == 0) return 1f;

        double summe = 0;
        foreach (var p in s.Points)
            summe += Math.Sqrt(p.TX * (double)p.TX + p.TY * (double)p.TY);

        float grad = (float)(summe / s.Points.Count);
        if (grad <= 0f) return 1f;   // der Normalfall: keine Neigung gemeldet

        return 1f + MaxZuwachs * Math.Min(1f, grad / NeigungVoll);
    }

    private static void DrawPencil(SKCanvas canvas, StrokeElement s, SKColor color)
    {
        float w = Math.Max(s.Width, 0.6f) * TiltWidthFactor(s);
        using var path = BuildSmoothPath(s.Points);

        // Graphit besteht aus einzelnen Körnern — es gibt KEINEN deckenden Kern. Deshalb wird
        // die Farbe von der Körnungs-Textur maskiert: wo die Textur dunkel ist, bleibt Papier
        // frei. Drei Durchgänge von breit+zart nach schmal+dicht erzeugen die nach außen
        // ausdünnende Dichte und die ausgefransten Ränder.

        // breit & zart: Streuung in der Randzone
        DrawGrainPass(canvas, path, color, 130, w * 1.30f, wobble: w * 0.22f);
        // mittlere Lage
        DrawGrainPass(canvas, path, color, 205, w * 0.92f, wobble: w * 0.13f);
        // schmal & dicht: Kern, wie stärkerer Andruck
        DrawGrainPass(canvas, path, color, 255, w * 0.48f, wobble: 0f);
    }

    /// <summary>
    /// Alpha-Kennlinie, die weiches Rauschen in **deutliche Körner** verwandelt: Werte unter der
    /// Schwelle werden transparent, darüber schnell deckend. Ohne das mittelt sich feines
    /// Rauschen zu einer glatten grauen Linie.
    /// </summary>
    private static readonly SKColorFilter GrainContrast = BuildGrainContrast();

    private static SKColorFilter BuildGrainContrast()
    {
        var table = new byte[256];
        var identity = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            float v = i / 255f;
            // Fractal-Noise liegt um 0,5 — diese Rampe spreizt den mittleren Bereich stark,
            // sodass klar getrennte Körner entstehen (statt eines weichen Grauverlaufs).
            float t = Math.Clamp((v - 0.32f) / 0.26f, 0f, 1f);
            table[i] = (byte)(t * 255f);
            identity[i] = (byte)i;
        }
        // Nur der Alphakanal wird verbogen, R/G/B bleiben unverändert. Bis SkiaSharp 2 stand
        // dafür `null`; seit 3.x wirft das eine ArgumentNullException — die unveränderte
        // Kennlinie muss ausgeschrieben werden.
        return SKColorFilter.CreateTable(table, identity, identity, identity);
    }

    /// <summary>
    /// Kachelbare Körnungs-Textur. Der Perlin-Shader wird **einmal** ausgewertet statt in jedem
    /// Bild neu: als Live-Shader kostete die Körnung pro Strich drei volle Rausch-Durchgänge —
    /// bei rund 50 Bleistift-Strichen waren das ~100 ms pro Bild (spürbarer Stift-Nachlauf).
    /// Als Textur ist es ein einfacher Speicherzugriff bei gleicher Optik. Die Kachel liegt in
    /// Canvas-Koordinaten, die Körnung klebt also wie bisher am Papier (nicht am Bildschirm).
    /// </summary>
    private static readonly SKShader GrainTexture = BuildGrainTexture();

    private static SKShader BuildGrainTexture()
    {
        // 512 px: bei der feinen Körnung (Periode < 1 px) ist die Wiederholung nicht sichtbar.
        const int size = 512;
        const float freq = 1.1f;   // hoch = feine Körnung

        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        using (var noise = SKShader.CreatePerlinNoiseFractalNoise(freq, freq, 3, 0f))
        using (var paint = new SKPaint { Shader = noise })
            surface.Canvas.DrawRect(SKRect.Create(0, 0, size, size), paint);

        using var image = surface.Snapshot();
        return SKShader.CreateImage(image, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
    }

    /// <summary>Ein Körnungs-Durchgang des Bleistifts (Farbe × Körnung als Alpha-Maske).</summary>
    private static void DrawGrainPass(SKCanvas canvas, SKPath path, SKColor color,
                                      byte alpha, float width, float wobble)
    {
        using var tint = SKShader.CreateColor(color.WithAlpha(alpha));
        using var grain = SKShader.CreateCompose(tint, GrainTexture, SKBlendMode.DstIn);
        using var fx = wobble > 0f ? SKPathEffect.CreateDiscrete(1.3f, wobble) : null;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeWidth = Math.Max(width, 0.4f),
            Shader = grain,
            PathEffect = fx,
            ColorFilter = GrainContrast,
        };
        canvas.DrawPath(path, paint);
    }

    public static void DrawShape(SKCanvas canvas, ShapeElement sh, string colorHex, float strokeWidth)
    {
        var color = ParseColor(colorHex);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color,
            StrokeWidth = strokeWidth,
        };

        var p1 = new SKPoint(sh.X1, sh.Y1);
        var p2 = new SKPoint(sh.X2, sh.Y2);

        switch (sh.Shape)
        {
            case ShapeKind.Line:
                canvas.DrawLine(p1, p2, paint);
                break;

            case ShapeKind.Arrow:
            {
                canvas.DrawLine(p1, p2, paint);
                float angle = MathF.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                float head = strokeWidth * 3f + 10f;
                const float spread = 0.46f;
                var h1 = new SKPoint(p2.X - head * MathF.Cos(angle - spread), p2.Y - head * MathF.Sin(angle - spread));
                var h2 = new SKPoint(p2.X - head * MathF.Cos(angle + spread), p2.Y - head * MathF.Sin(angle + spread));
                canvas.DrawLine(p2, h1, paint);
                canvas.DrawLine(p2, h2, paint);
                break;
            }

            case ShapeKind.Rectangle:
            {
                var r = SKRect.Create(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                      Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));
                if (sh.Fill != null)
                {
                    using var fill = new SKPaint { IsAntialias = true, Color = ParseColor(sh.Fill) };
                    canvas.DrawRect(r, fill);
                }
                canvas.DrawRect(r, paint);
                break;
            }

            case ShapeKind.Ellipse:
            {
                var r = SKRect.Create(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                      Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));
                if (sh.Fill != null)
                {
                    using var fill = new SKPaint { IsAntialias = true, Color = ParseColor(sh.Fill) };
                    canvas.DrawOval(r, fill);
                }
                canvas.DrawOval(r, paint);
                break;
            }

            case ShapeKind.Triangle:
            {
                var (a, b, c) = TrianglePoints(sh);
                using var path = new SKPath();
                path.MoveTo(a); path.LineTo(b); path.LineTo(c); path.Close();
                if (sh.Fill != null)
                {
                    using var fill = new SKPaint { IsAntialias = true, Color = ParseColor(sh.Fill) };
                    canvas.DrawPath(path, fill);
                }
                canvas.DrawPath(path, paint);
                break;
            }
        }
    }

    public static void DrawText(SKCanvas canvas, TextElement t)
    {
        if (t.Background != null)
        {
            var b = TextBounds(t);
            b.Inflate(5, 3);
            using var bg = new SKPaint { IsAntialias = true, Color = ParseColor(t.Background) };
            canvas.DrawRoundRect(b, 3, 3, bg);
        }

        using var paint = new SKPaint { IsAntialias = true, Color = ParseColor(t.Color) };
        using var font = WbFonts.Font(t.FontFamily, t.FontSize);
        float lineHeight = t.FontSize * 1.35f;
        var lines = t.Text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            canvas.DrawText(lines[i], t.X, t.Y + t.FontSize + i * lineHeight, font, paint);
    }

    /// <summary>Zeichnet nur die Zettelkarte (Schatten, Fläche, dezenter Rand) – ohne Text.</summary>
    public static void DrawStickyCard(SKCanvas canvas, StickyNoteElement sn)
    {
        var rect = SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height);
        const float radius = 6f;

        DrawCachedShadow(canvas, rect, radius, 45, ref _stickyShadowImg);

        var fill = ParseColor(sn.Color);
        using (var bg = new SKPaint { IsAntialias = true, Color = fill })
            canvas.DrawRoundRect(rect, radius, radius, bg);

        // hauchzarter Rand, leicht dunkler als die Füllung
        using var border = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            Color = new SKColor(0, 0, 0, 28),
        };
        canvas.DrawRoundRect(rect, radius, radius, border);
    }

    public static void DrawSticky(SKCanvas canvas, StickyNoteElement sn)
    {
        DrawStickyCard(canvas, sn);
        if (string.IsNullOrEmpty(sn.Text)) return;

        using var paint = new SKPaint { IsAntialias = true, Color = ParseColor(sn.TextColor) };
        using var font = WbFonts.Font(sn.FontFamily, sn.FontSize);

        float lineHeight = sn.FontSize * 1.32f;
        float maxWidth = sn.Width - StickyPad * 2;
        float x = sn.X + StickyPad;
        float yBase = sn.Y + StickyPad + sn.FontSize;
        float maxY = sn.Y + sn.Height - StickyPad * 0.5f;

        canvas.Save();
        canvas.ClipRect(SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height));
        float y = yBase;
        foreach (var line in WrapText(sn.Text, font, maxWidth))
        {
            if (y > maxY) break; // Text, der nicht mehr passt, wird abgeschnitten
            canvas.DrawText(line, x, y, font, paint);
            y += lineHeight;
        }
        canvas.Restore();
    }

    // ---- Textumbruch ---------------------------------------------------------------------

    /// <summary>Bricht Text an Wortgrenzen auf die verfügbare Breite um (respektiert \n).</summary>
    public static IEnumerable<string> WrapText(string text, SKFont font, float maxWidth)
    {
        foreach (var para in text.Split('\n'))
        {
            if (para.Length == 0) { yield return ""; continue; }

            var words = para.Split(' ');
            var current = "";
            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (font.MeasureText(candidate) <= maxWidth || current.Length == 0)
                {
                    // Einzelnes zu langes Wort hart umbrechen
                    if (current.Length == 0 && font.MeasureText(word) > maxWidth)
                    {
                        foreach (var chunk in BreakLongWord(word, font, maxWidth))
                        {
                            if (chunk.Last) { current = chunk.Text; }
                            else yield return chunk.Text;
                        }
                    }
                    else current = candidate;
                }
                else
                {
                    yield return current;
                    current = word;
                    if (font.MeasureText(word) > maxWidth)
                    {
                        foreach (var chunk in BreakLongWord(word, font, maxWidth))
                        {
                            if (chunk.Last) current = chunk.Text;
                            else yield return chunk.Text;
                        }
                    }
                }
            }
            yield return current;
        }
    }

    private static IEnumerable<(string Text, bool Last)> BreakLongWord(string word, SKFont font, float maxWidth)
    {
        var chunk = "";
        foreach (var ch in word)
        {
            if (chunk.Length > 0 && font.MeasureText(chunk + ch) > maxWidth)
            {
                yield return (chunk, false);
                chunk = ch.ToString();
            }
            else chunk += ch;
        }
        yield return (chunk, true);
    }
}
