using GonkNote.Core.Models;
using GonkNote.Core.Services;
using SkiaSharp;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Das Referenz-Notizbuch für den Whiteboard-PDF-/PNG-Export: Cover, feste Seiten mit
/// Muster und Hintergrundbild, eine unendliche Fläche, und auf den Seiten je ein Element
/// jeder Klasse.
/// <para>
/// Gegenstück zum Textdokument in <see cref="Referenzdokument"/>. Dieser Weg läuft
/// vollständig über SkiaSharp und ist damit der, der beim Umzug nach Avalonia (Phase 3)
/// unverändert weiterlaufen muss — der Export bekommt dort keinen neuen Code, sondern
/// denselben.
/// </para>
/// </summary>
internal static class Referenzbuch
{
    public static WhiteboardDoc Bauen(BlobStore blobs)
    {
        var coverBild = Referenzdokument.Bild(400, 560, SKColors.MidnightBlue, SKColors.Gold);
        var seitenBild = Referenzdokument.Bild(600, 850, SKColors.White, SKColors.SlateGray);
        var elementBild = Referenzdokument.Bild(200, 140, SKColors.MediumSeaGreen, SKColors.DarkSlateGray);

        var coverId = blobs.Put(coverBild);
        var seitenId = blobs.Put(seitenBild);
        var elementId = blobs.Put(elementBild);

        return new WhiteboardDoc
        {
            Id = new Guid("99999999-9999-9999-9999-999999999999"),
            Cover = new CoverStyle
            {
                GradientStart = "#1E3A8A",
                GradientEnd = "#7C3AED",
                FontFamily = "Segoe UI",
                ImageId = coverId,
            },
            Pages =
            {
                // 1: Cover mit Bild (das Bild hat Vorrang vor dem Farbverlauf)
                new WbPage { IsCover = true, Width = WhiteboardDoc.A4Width, Height = WhiteboardDoc.A4Height },

                // 2: linierte Seite mit allen Elementklassen
                new WbPage
                {
                    Width = WhiteboardDoc.A4Width,
                    Height = WhiteboardDoc.A4Height,
                    Background = PageBackground.Lines,
                    Shade = PageShade.Light,
                    Elements = Elemente(elementId),
                },

                // 3: importierte Seite (Hintergrundbild ersetzt das Muster)
                new WbPage
                {
                    Width = WhiteboardDoc.A4Width,
                    Height = WhiteboardDoc.A4Height,
                    BackgroundImageId = seitenId,
                },

                // 4: dunkle Gitterseite — Papierfarbe ist unabhängig vom App-Theme
                new WbPage
                {
                    Width = WhiteboardDoc.A4Width,
                    Height = WhiteboardDoc.A4Height,
                    Background = PageBackground.Grid,
                    Shade = PageShade.Dark,
                    Elements =
                    {
                        new StrokeElement
                        {
                            Kind = StrokeKind.Pen,
                            Color = "#FFE6ECF7",
                            Width = 3f,
                            Points = { new WbPoint(80, 200, 0.2f), new WbPoint(400, 400, 0.9f) },
                        },
                    },
                },

                // 5: unendliche Fläche — der Exporter muss sie über den Inhalt zuschneiden
                new WbPage
                {
                    Background = PageBackground.Dots,
                    Elements =
                    {
                        new ShapeElement
                        {
                            Shape = ShapeKind.Ellipse,
                            X1 = -200, Y1 = -100, X2 = 300, Y2 = 260,
                            Color = "#FF14B8A6",
                            StrokeWidth = 5f,
                            Fill = "#3314B8A6",
                        },
                    },
                },
            },
        };
    }

    private static List<WbElement> Elemente(Guid bildId) =>
    [
        new StrokeElement
        {
            Kind = StrokeKind.Pen,
            Color = "#FF1B2B4B",
            Width = 4f,
            Points =
            {
                new WbPoint(60, 120, 0.05f), new WbPoint(160, 200, 0.5f),
                new WbPoint(280, 130, 0.95f), new WbPoint(380, 210, 0.4f),
            },
        },
        new StrokeElement
        {
            Kind = StrokeKind.Pencil,
            Color = "#FF334155",
            Width = 5f,
            Points = { new WbPoint(60, 280, 0.4f), new WbPoint(240, 330, 0.8f), new WbPoint(400, 270, 0.6f) },
        },
        new StrokeElement
        {
            Kind = StrokeKind.Highlighter,
            Color = "#FFFACC15",
            Width = 24f,
            Points = { new WbPoint(60, 400, 1f), new WbPoint(420, 410, 1f) },
        },
        new ShapeElement
        {
            Shape = ShapeKind.Rectangle,
            X1 = 480, Y1 = 120, X2 = 700, Y2 = 260,
            Color = "#FF7C3AED",
            StrokeWidth = 3f,
            Fill = "#337C3AED",
            Rotation = 8f,
        },
        new ShapeElement
        {
            Shape = ShapeKind.Arrow,
            X1 = 480, Y1 = 320, X2 = 700, Y2 = 420,
            Color = "#FFDC2626",
            StrokeWidth = 3f,
        },
        new TextElement
        {
            X = 60, Y = 480,
            Text = "Ein Textkasten\nmit zwei Zeilen",
            Color = "#FF0F172A",
            FontSize = 20f,
            Background = "#FFFEF3C7",
        },
        // Das Bild liegt nur als Verweis vor (Data leer) — genau so steht es in einem
        // gespeicherten Dokument. Der Exporter muss es über den Blob-Speicher holen.
        new ImageElement
        {
            Id = bildId,
            X = 480, Y = 480, Width = 260, Height = 180,
        },
        new StickyNoteElement
        {
            X = 60, Y = 620, Width = 240, Height = 160,
            Text = "Ein Notizzettel mit Text, der umbrochen wird.",
            Color = "#FFFEF08A",
            TextColor = "#FF1F2937",
            FontSize = 15f,
            Rotation = -5f,
        },
    ];
}
