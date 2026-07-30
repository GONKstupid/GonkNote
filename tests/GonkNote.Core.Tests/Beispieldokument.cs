using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Ein Notizbuch, das **jeden** Elementtyp und jedes Feld benutzt, das die Persistenz
/// betrifft — Vorlage für den Roundtrip-Test und für die Renderer-Snapshots.
/// <para>
/// Absicht: ein Feld, das hier fehlt, wird von keinem Test bewacht. Wer ein Feld am Modell
/// ergänzt, ergänzt es hier mit einem Wert, der **nicht** dem Standard entspricht — sonst
/// bemerkt der Roundtrip-Test nicht, wenn es überhaupt nicht gespeichert wird.
/// </para>
/// </summary>
internal static class Beispieldokument
{
    /// <summary>Fester Zeitstempel statt DateTime.UtcNow — Tests dürfen nicht von der Uhr abhängen.</summary>
    public static readonly DateTime Zeitpunkt = new(2026, 7, 29, 10, 15, 30, 123, DateTimeKind.Utc);

    /// <summary>Feste Ids statt Guid.NewGuid() — sonst wären die Renderer-Snapshots nicht vergleichbar.</summary>
    public static readonly Guid DokumentId = new("11111111-1111-1111-1111-111111111111");

    private static readonly Guid CoverBildId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SeitenBildId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ElementBildId = new("44444444-4444-4444-4444-444444444444");

    public static WhiteboardDoc Notizbuch() => new()
    {
        Id = DokumentId,
        NewPageTemplate = new PageTemplate
        {
            Width = WhiteboardDoc.A3Width,
            Height = WhiteboardDoc.A3Height,
            Background = PageBackground.Grid,
            Shade = PageShade.Dark,
        },
        Cover = new CoverStyle
        {
            GradientStart = "#0F766E",
            GradientEnd = "#BE185D",
            FontFamily = "Georgia",
            ImageId = CoverBildId,
            Image = Bild(64, 64, SKColors.Orange),
        },
        Pages =
        {
            new WbPage
            {
                IsCover = true,
                Width = WhiteboardDoc.A4Width,
                Height = WhiteboardDoc.A4Height,
            },
            new WbPage
            {
                Width = WhiteboardDoc.A4Width,
                Height = WhiteboardDoc.A4Height,
                Background = PageBackground.Lines,
                Shade = PageShade.Light,
                BackgroundImageId = SeitenBildId,
                BackgroundImage = Bild(120, 160, SKColors.LightGray),
                Elements = AlleElemente(),
            },
            new WbPage
            {
                // Unendliche Fläche (Whiteboard-Modus): Width/Height bleiben 0.
                Background = PageBackground.Dots,
                Shade = PageShade.Dark,
                Elements =
                {
                    new StrokeElement
                    {
                        Id = new Guid("55555555-5555-5555-5555-555555555555"),
                        Kind = StrokeKind.Highlighter,
                        Color = "#FFFACC15",
                        Width = 18f,
                        Points = { new WbPoint(20, 20, 1f), new WbPoint(160, 40, 1f), new WbPoint(300, 25, 1f) },
                    },
                },
            },
        },
    };

    /// <summary>Je ein Element jeder Klasse, alle mit von den Standardwerten abweichenden Feldern.</summary>
    public static List<WbElement> AlleElemente() =>
    [
        new StrokeElement
        {
            Id = new Guid("aaaaaaaa-0000-0000-0000-000000000001"),
            Kind = StrokeKind.Pen,
            Color = "#FF1B2B4B",
            Width = 3.5f,
            // Druckverlauf mit Zwischenwerten: prüft, dass P nicht auf 0,5 zurückfällt
            Points =
            {
                new WbPoint(40, 60, 0.05f),
                new WbPoint(90, 110, 0.42f),
                new WbPoint(150, 80, 0.88f),
                new WbPoint(210, 140, 1.00f),
            },
        },
        new StrokeElement
        {
            Id = new Guid("aaaaaaaa-0000-0000-0000-000000000002"),
            Kind = StrokeKind.Pencil,
            Color = "#FF334155",
            Width = 4.2f,
            Points =
            {
                new WbPoint(40, 200, 0.30f),
                new WbPoint(120, 240, 0.65f),
                new WbPoint(220, 205, 0.95f),
            },
        },
        new ShapeElement
        {
            Id = new Guid("bbbbbbbb-0000-0000-0000-000000000001"),
            Shape = ShapeKind.Rectangle,
            X1 = 300, Y1 = 60, X2 = 460, Y2 = 170,
            Color = "#FF7C3AED",
            StrokeWidth = 2.5f,
            Fill = "#33A78BFA",
            Rotation = 12f,
        },
        new ShapeElement
        {
            Id = new Guid("bbbbbbbb-0000-0000-0000-000000000002"),
            Shape = ShapeKind.Arrow,
            X1 = 300, Y1 = 220, X2 = 470, Y2 = 300,
            Color = "#FFDC2626",
            StrokeWidth = 3f,
        },
        new TextElement
        {
            Id = new Guid("cccccccc-0000-0000-0000-000000000001"),
            X = 40, Y = 320,
            Text = "Zeile eins\nZeile zwei",
            Color = "#FF0F172A",
            FontSize = 22f,
            Background = "#FFFEF3C7",
            FontFamily = "Segoe UI",
        },
        new ImageElement
        {
            Id = ElementBildId,
            X = 320, Y = 340, Width = 140, Height = 100,
            Data = Bild(70, 50, SKColors.MediumSeaGreen),
        },
        new StickyNoteElement
        {
            Id = new Guid("dddddddd-0000-0000-0000-000000000001"),
            X = 40, Y = 420, Width = 220, Height = 150,
            Text = "Ein Zettel mit einem Text, der umbrochen werden muss.",
            Color = "#FFFEF08A",
            TextColor = "#FF1F2937",
            FontSize = 15f,
            FontFamily = "Segoe UI",
            Rotation = -6f,
        },
    ];

    /// <summary>Textdokument mit gesetzten Einstellungen und **leeren** Strings (EmptyStringToNull).</summary>
    public static TextDoc Textdokument() => new()
    {
        Id = DokumentId,
        Rtf = "PK kein echtes ZIP, nur Bytes"u8.ToArray(),
        Images = [ElementBildId, SeitenBildId],
        PageFormat = "Letter",
        Landscape = true,
        MarginLeftCm = 3.5,
        MarginTopCm = 1.25,
        MarginRightCm = 3.5,
        MarginBottomCm = 1.75,
        HeaderText = "",              // leer, nicht null — genau der EmptyStringToNull-Fall
        FooterText = "Seite {SEITE} von {SEITEN}",
        SuppressHeaderOnFirstPage = true,
        WatermarkImage = Bild(32, 32, SKColors.SlateGray),
        WatermarkOpacity = 0.35,
        ShowPageBreaks = true,
    };

    /// <summary>
    /// Ein winziges, gültiges PNG. Erzeugt statt eingecheckt: eine Binärdatei im Repo
    /// bräuchte eine geklärte Lizenz (HANDOFF §6), ein Rechteck braucht keine.
    /// </summary>
    public static byte[] Bild(int width, int height, SKColor color)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.Clear(color);
        // Diagonale, damit das Bild nicht rotationssymmetrisch ist — sonst fiele eine
        // vertauschte Achse im Renderer-Snapshot nicht auf.
        using (var pen = new SKPaint { Color = SKColors.Black, StrokeWidth = 2 })
            surface.Canvas.DrawLine(0, 0, width, height, pen);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
