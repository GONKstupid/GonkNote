using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace GonkNote.Services;

/// <summary>
/// Exportiert ein WPF-FlowDocument als DOCX (OpenXML) – die Gegenrichtung zum
/// <see cref="DocxImporter"/>. Erbbare Zeichenformate (fett/kursiv/Größe/Farbe/
/// Schriftart) werden als effektive Werte am Run gelesen, nicht-erbbare
/// (Unterstrichen/Durchgestrichen/Marker/Hoch-Tief) über die Elternkette akkumuliert.
/// </summary>
public static class DocxExporter
{
    private const int BulletNumId = 1;
    private const int DecimalNumId = 2;

    /// <summary>Nicht vererbte Zeichenformate, die von Eltern-Spans mitgetragen werden.</summary>
    private readonly record struct Acc(bool Underline, bool Strike, string? Vert, string? Highlight)
    {
        public static readonly Acc None = new();
    }

    public static void Export(FlowDocument flow, string path)
    {
        using var docx = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = docx.AddMainDocumentPart();
        main.Document = new W.Document(new W.Body());
        var body = main.Document.Body!;

        AddNumberingDefinitions(main);

        foreach (var block in flow.Blocks)
            WriteBlock(block, body, main, null);

        if (!body.HasChildren) body.AppendChild(new W.Paragraph());
        main.Document.Save();
    }

    // ==================== Blöcke ====================

    private static void WriteBlock(Block block, OpenXmlElement target, MainDocumentPart main, int? numId)
    {
        switch (block)
        {
            case Paragraph p:
                target.AppendChild(BuildParagraph(p, main, numId));
                break;

            case List list:
            {
                int id = IsOrdered(list.MarkerStyle) ? DecimalNumId : BulletNumId;
                foreach (var li in list.ListItems)
                    foreach (var b in li.Blocks)
                        WriteBlock(b, target, main, id);
                break;
            }

            case Table table:
                target.AppendChild(BuildTable(table, main));
                break;

            case BlockUIContainer or Section:
                // Trennlinien u. Ä.: als leerer Absatz mit Unterstrich
                target.AppendChild(new W.Paragraph(new W.ParagraphProperties(
                    new W.ParagraphBorders(new W.BottomBorder
                    {
                        Val = W.BorderValues.Single, Size = 6, Color = "AAAAAA",
                    }))));
                break;
        }
    }

    private static W.Paragraph BuildParagraph(Paragraph p, MainDocumentPart main, int? numId)
    {
        var para = new W.Paragraph();
        var props = new W.ParagraphProperties
        {
            Justification = new W.Justification
            {
                Val = p.TextAlignment switch
                {
                    TextAlignment.Center => W.JustificationValues.Center,
                    TextAlignment.Right => W.JustificationValues.Right,
                    TextAlignment.Justify => W.JustificationValues.Both,
                    _ => W.JustificationValues.Left,
                },
            },
        };
        if (numId is int id)
            props.NumberingProperties = new W.NumberingProperties(
                new W.NumberingLevelReference { Val = 0 },
                new W.NumberingId { Val = id });
        para.AppendChild(props);

        foreach (var inline in p.Inlines)
            WriteInline(inline, para, main, Acc.None);
        return para;
    }

    // ==================== Inlines ====================

    private static void WriteInline(Inline inline, OpenXmlElement target, MainDocumentPart main, Acc acc)
    {
        switch (inline)
        {
            case Run run:
                if (run.Text.Length > 0)
                    target.AppendChild(BuildRun(run, run.Text, Combine(acc, run)));
                break;

            case Span span:  // Bold/Italic/Underline/Hyperlink sind Spans
            {
                var a = Combine(acc, span);
                foreach (var child in span.Inlines)
                    WriteInline(child, target, main, a);
                break;
            }

            case LineBreak:
                target.AppendChild(new W.Run(new W.Break()));
                break;

            case InlineUIContainer { Child: Image img }:
                var drawing = BuildImage(img, main);
                if (drawing != null) target.AppendChild(new W.Run(drawing));
                break;
        }
    }

    /// <summary>Akkumuliert die nicht vererbten Formate dieses Elements.</summary>
    private static Acc Combine(Acc acc, Inline el)
    {
        bool underline = acc.Underline, strike = acc.Strike;
        if (el.GetValue(Inline.TextDecorationsProperty) is TextDecorationCollection deco)
        {
            if (deco.Any(d => d.Location == TextDecorationLocation.Underline)) underline = true;
            if (deco.Any(d => d.Location == TextDecorationLocation.Strikethrough)) strike = true;
        }

        string? vert = acc.Vert;
        if (el.GetValue(Inline.BaselineAlignmentProperty) is BaselineAlignment ba)
            vert = ba switch
            {
                BaselineAlignment.Superscript => "superscript",
                BaselineAlignment.Subscript => "subscript",
                _ => vert,
            };

        string? highlight = acc.Highlight;
        if (el.GetValue(TextElement.BackgroundProperty) is SolidColorBrush bb)
            highlight = Hex(bb.Color);

        return new Acc(underline, strike, vert, highlight);
    }

    private static W.Run BuildRun(Inline el, string text, Acc acc)
    {
        var rpr = new W.RunProperties();

        if (el.GetValue(TextElement.FontWeightProperty) is FontWeight w && w.ToOpenTypeWeight() >= 600)
            rpr.Bold = new W.Bold();
        if (el.GetValue(TextElement.FontStyleProperty) is FontStyle st && st != FontStyles.Normal)
            rpr.Italic = new W.Italic();
        if (acc.Underline) rpr.Underline = new W.Underline { Val = W.UnderlineValues.Single };
        if (acc.Strike) rpr.Strike = new W.Strike();

        if (el.GetValue(TextElement.ForegroundProperty) is SolidColorBrush fb)
        {
            string hex = Hex(fb.Color);
            if (hex != "000000") rpr.Color = new W.Color { Val = hex };
        }
        if (acc.Highlight is { } hl)
            rpr.Shading = new W.Shading { Val = W.ShadingPatternValues.Clear, Fill = hl, Color = "auto" };
        if (el.GetValue(TextElement.FontFamilyProperty) is FontFamily ff)
            rpr.RunFonts = new W.RunFonts { Ascii = ff.Source, HighAnsi = ff.Source };
        if (el.GetValue(TextElement.FontSizeProperty) is double px)
        {
            int halfPoints = Math.Max(2, (int)Math.Round(px * 1.5));  // px→pt(×0.75)→halbe Punkte(×2)
            rpr.FontSize = new W.FontSize { Val = halfPoints.ToString() };
        }
        if (acc.Vert is { } v)
            rpr.VerticalTextAlignment = new W.VerticalTextAlignment
            {
                Val = v == "superscript" ? W.VerticalPositionValues.Superscript : W.VerticalPositionValues.Subscript,
            };

        var run = new W.Run();
        if (rpr.HasChildren) run.AppendChild(rpr);
        run.AppendChild(new W.Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return run;
    }

    // ==================== Tabellen ====================

    private static W.Table BuildTable(Table table, MainDocumentPart main)
    {
        var t = new W.Table(new W.TableProperties(new W.TableBorders(
            new W.TopBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.BottomBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.LeftBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.RightBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" })));

        foreach (var group in table.RowGroups)
            foreach (var row in group.Rows)
            {
                var tr = new W.TableRow();
                foreach (var cell in row.Cells)
                {
                    var tc = new W.TableCell();
                    if (cell.ColumnSpan > 1)
                        tc.AppendChild(new W.TableCellProperties(new W.GridSpan { Val = cell.ColumnSpan }));

                    foreach (var b in cell.Blocks)
                        WriteBlock(b, tc, main, null);
                    if (!tc.Elements<W.Paragraph>().Any()) tc.AppendChild(new W.Paragraph());
                    tr.AppendChild(tc);
                }
                t.AppendChild(tr);
            }
        return t;
    }

    // ==================== Bilder ====================

    private static W.Drawing? BuildImage(Image img, MainDocumentPart main)
    {
        if (img.Source is not BitmapSource src) return null;
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(src));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            var part = main.AddImagePart(ImagePartType.Png);
            part.FeedData(ms);
            string rId = main.GetIdOfPart(part);

            double wPx = double.IsNaN(img.Width) ? src.PixelWidth : img.Width;
            double hPx = double.IsNaN(img.Height)
                ? (double.IsNaN(img.Width) ? src.PixelHeight : img.Width * src.PixelHeight / src.PixelWidth)
                : img.Height;
            long cx = (long)(wPx * 9525);
            long cy = (long)(hPx * 9525);

            return new W.Drawing(new DW.Inline(
                new DW.Extent { Cx = cx, Cy = cy },
                new DW.DocProperties { Id = 1U, Name = "Bild" },
                new A.Graphic(new A.GraphicData(
                    new PIC.Picture(
                        new PIC.NonVisualPictureProperties(
                            new PIC.NonVisualDrawingProperties { Id = 0U, Name = "Bild" },
                            new PIC.NonVisualPictureDrawingProperties()),
                        new PIC.BlipFill(
                            new A.Blip { Embed = rId },
                            new A.Stretch(new A.FillRectangle())),
                        new PIC.ShapeProperties(
                            new A.Transform2D(
                                new A.Offset { X = 0L, Y = 0L },
                                new A.Extents { Cx = cx, Cy = cy }),
                            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })));
        }
        catch
        {
            return null;  // Bild überspringen statt Export abbrechen
        }
    }

    // ==================== Nummerierung ====================

    private static bool IsOrdered(TextMarkerStyle m) => m is
        TextMarkerStyle.Decimal or TextMarkerStyle.LowerLatin or TextMarkerStyle.UpperLatin
        or TextMarkerStyle.LowerRoman or TextMarkerStyle.UpperRoman;

    private static void AddNumberingDefinitions(MainDocumentPart main)
    {
        var part = main.AddNewPart<NumberingDefinitionsPart>();
        part.Numbering = new W.Numbering(
            new W.AbstractNum(
                new W.Level(
                    new W.NumberingFormat { Val = W.NumberFormatValues.Bullet },
                    new W.LevelText { Val = "•" },
                    new W.LevelJustification { Val = W.LevelJustificationValues.Left }) { LevelIndex = 0 })
            { AbstractNumberId = 1 },
            new W.AbstractNum(
                new W.Level(
                    new W.NumberingFormat { Val = W.NumberFormatValues.Decimal },
                    new W.LevelText { Val = "%1." },
                    new W.LevelJustification { Val = W.LevelJustificationValues.Left }) { LevelIndex = 0 })
            { AbstractNumberId = 2 },
            new W.NumberingInstance(new W.AbstractNumId { Val = 1 }) { NumberID = BulletNumId },
            new W.NumberingInstance(new W.AbstractNumId { Val = 2 }) { NumberID = DecimalNumId });
    }

    private static string Hex(Color c) => $"{c.R:X2}{c.G:X2}{c.B:X2}";
}
