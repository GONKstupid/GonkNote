using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace GonkNote.Services;

/// <summary>
/// Konvertiert DOCX (OpenXML) in ein WPF-FlowDocument und serialisiert es als
/// XamlPackage – das Format, in dem Textdokumente gespeichert werden.
/// Abgedeckt: Absätze, Zeichenformate (fett/kursiv/unterstrichen/durchgestrichen,
/// Farbe, Marker, Größe, Schriftart, Hoch-/Tiefstellung), Ausrichtung, Überschriften,
/// Listen (Punkt/Nummer), Tabellen und eingebettete Bilder. Nicht Abgedecktes wird
/// bestmöglich als Fließtext übernommen.
/// </summary>
public static class DocxImporter
{
    public static byte[] ToXamlPackage(string path)
    {
        var flow = Convert(path);
        var range = new TextRange(flow.ContentStart, flow.ContentEnd);
        using var ms = new MemoryStream();
        range.Save(ms, DataFormats.XamlPackage, true);
        return ms.ToArray();
    }

    private static FlowDocument Convert(string path)
    {
        var flow = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 15,
        };

        using var doc = WordprocessingDocument.Open(path, false);
        var main = doc.MainDocumentPart;
        var body = main?.Document.Body;
        if (main == null || body == null) return flow;

        List? currentList = null;
        TextMarkerStyle currentMarker = TextMarkerStyle.Disc;

        void FlushList()
        {
            if (currentList != null) { flow.Blocks.Add(currentList); currentList = null; }
        }

        foreach (var element in body.ChildElements)
        {
            switch (element)
            {
                case W.Paragraph p:
                {
                    var numbering = p.ParagraphProperties?.NumberingProperties;
                    var para = ConvertParagraph(p, main);

                    if (numbering != null)
                    {
                        var marker = ResolveMarker(numbering, main);
                        if (currentList == null || marker != currentMarker)
                        {
                            FlushList();
                            currentMarker = marker;
                            currentList = new List
                            {
                                MarkerStyle = marker,
                                Margin = new Thickness(14, 4, 0, 4),
                            };
                        }
                        para.Margin = new Thickness(0, 1, 0, 1);
                        currentList.ListItems.Add(new ListItem(para));
                    }
                    else
                    {
                        FlushList();
                        flow.Blocks.Add(para);
                    }
                    break;
                }

                case W.Table t:
                    FlushList();
                    flow.Blocks.Add(ConvertTable(t, main));
                    break;
            }
        }
        FlushList();

        if (flow.Blocks.Count == 0)
            flow.Blocks.Add(new Paragraph());
        return flow;
    }

    // ==================== Absätze ====================

    private static Paragraph ConvertParagraph(W.Paragraph p, MainDocumentPart main)
    {
        var para = new Paragraph { Margin = new Thickness(0, 2, 0, 6) };
        var props = p.ParagraphProperties;

        // Ausrichtung
        var jc = props?.Justification?.Val;
        if (jc != null)
        {
            para.TextAlignment = jc.InnerText switch
            {
                "center" => TextAlignment.Center,
                "right" or "end" => TextAlignment.Right,
                "both" or "distribute" => TextAlignment.Justify,
                _ => TextAlignment.Left,
            };
        }

        // Überschriften-Styles (Heading1/berschrift1 …)
        int headingLevel = HeadingLevel(props?.ParagraphStyleId?.Val?.Value);
        if (headingLevel > 0)
        {
            para.FontSize = headingLevel switch { 1 => 26, 2 => 21, _ => 17 };
            para.FontWeight = FontWeights.SemiBold;
            para.Margin = new Thickness(0, headingLevel == 1 ? 12 : 8, 0, 6);
        }

        foreach (var child in p.ChildElements)
        {
            switch (child)
            {
                case W.Run run:
                    AppendRun(para.Inlines, run, main, hyperlink: false);
                    break;

                case W.Hyperlink link:
                    foreach (var linkRun in link.Elements<W.Run>())
                        AppendRun(para.Inlines, linkRun, main, hyperlink: true);
                    break;
            }
        }
        return para;
    }

    private static int HeadingLevel(string? styleId)
    {
        if (string.IsNullOrEmpty(styleId)) return 0;
        string s = styleId.ToLowerInvariant();
        foreach (var prefix in new[] { "heading", "berschrift" })
        {
            int idx = s.IndexOf(prefix, StringComparison.Ordinal);
            if (idx < 0) continue;
            string rest = s[(idx + prefix.Length)..];
            if (int.TryParse(rest, out int lvl)) return Math.Clamp(lvl, 1, 6);
        }
        return 0;
    }

    // ==================== Runs ====================

    private static void AppendRun(InlineCollection target, W.Run run, MainDocumentPart main, bool hyperlink)
    {
        var rPr = run.RunProperties;

        foreach (var child in run.ChildElements)
        {
            switch (child)
            {
                case W.Text t:
                {
                    var inline = new Run(t.Text);
                    ApplyRunProperties(inline, rPr, hyperlink);
                    target.Add(inline);
                    break;
                }

                case W.TabChar:
                    target.Add(new Run(" "));
                    break;

                case W.Break:
                    target.Add(new LineBreak());
                    break;

                case W.Drawing drawing:
                {
                    var img = ConvertImage(drawing, main);
                    if (img != null) target.Add(new InlineUIContainer(img));
                    break;
                }
            }
        }
    }

    private static void ApplyRunProperties(Run inline, W.RunProperties? rPr, bool hyperlink)
    {
        if (hyperlink)
        {
            inline.TextDecorations = TextDecorations.Underline;
            inline.Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
        }
        if (rPr == null) return;

        if (IsOn(rPr.Bold)) inline.FontWeight = FontWeights.Bold;
        if (IsOn(rPr.Italic)) inline.FontStyle = FontStyles.Italic;

        var decorations = new TextDecorationCollection(inline.TextDecorations ?? new TextDecorationCollection());
        if (rPr.Underline != null && rPr.Underline.Val?.InnerText != "none")
            decorations.Add(TextDecorations.Underline);
        if (IsOn(rPr.Strike))
            decorations.Add(TextDecorations.Strikethrough);
        if (decorations.Count > 0) inline.TextDecorations = decorations;

        // Halbe Punkte → Punkte → DIP (× 96/72)
        if (rPr.FontSize?.Val?.Value is { } sz && double.TryParse(sz, out double halfPoints))
            inline.FontSize = halfPoints / 2.0 * 96.0 / 72.0;

        if (rPr.RunFonts?.Ascii?.Value is { } font && !string.IsNullOrWhiteSpace(font))
            inline.FontFamily = new FontFamily(font);

        if (rPr.Color?.Val?.Value is { } colorHex && colorHex != "auto" && TryParseHex(colorHex, out var color))
            inline.Foreground = new SolidColorBrush(color);

        if (rPr.Highlight?.Val?.InnerText is { } hl && HighlightBrush(hl) is { } brush)
            inline.Background = brush;

        var vert = rPr.VerticalTextAlignment?.Val?.InnerText;
        if (vert == "superscript")
        {
            inline.BaselineAlignment = BaselineAlignment.Superscript;
            inline.FontSize = (inline.FontSize > 0 ? inline.FontSize : 15) * 0.65;
        }
        else if (vert == "subscript")
        {
            inline.BaselineAlignment = BaselineAlignment.Subscript;
            inline.FontSize = (inline.FontSize > 0 ? inline.FontSize : 15) * 0.65;
        }
    }

    private static bool IsOn(W.OnOffType? prop) =>
        prop != null && (prop.Val == null || prop.Val.Value);

    private static bool TryParseHex(string hex, out Color color)
    {
        color = Colors.Black;
        if (hex.Length != 6) return false;
        try
        {
            color = Color.FromRgb(
                System.Convert.ToByte(hex[..2], 16),
                System.Convert.ToByte(hex[2..4], 16),
                System.Convert.ToByte(hex[4..6], 16));
            return true;
        }
        catch { return false; }
    }

    private static Brush? HighlightBrush(string val) => val switch
    {
        "yellow" => Brushes.Yellow,
        "green" => Brushes.LightGreen,
        "cyan" => Brushes.Cyan,
        "magenta" => Brushes.Magenta,
        "red" => new SolidColorBrush(Color.FromRgb(0xFF, 0x9E, 0x9E)),
        "blue" => new SolidColorBrush(Color.FromRgb(0x9E, 0xC5, 0xFF)),
        "darkYellow" => new SolidColorBrush(Color.FromRgb(0xE3, 0xC8, 0x00)),
        "lightGray" => Brushes.LightGray,
        _ => null,
    };

    // ==================== Bilder ====================

    private static Image? ConvertImage(W.Drawing drawing, MainDocumentPart main)
    {
        try
        {
            var blip = drawing.Descendants<A.Blip>().FirstOrDefault();
            string? rId = blip?.Embed?.Value;
            if (rId == null || main.GetPartById(rId) is not ImagePart part) return null;

            using var stream = part.GetStream();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();

            // Größe aus dem Dokument (EMU → Pixel bei 96 DPI: ÷ 9525), sonst Bildgröße
            double width = bmp.PixelWidth, height = bmp.PixelHeight;
            var extent = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>().FirstOrDefault();
            if (extent is { Cx.Value: > 0, Cy.Value: > 0 })
            {
                width = extent.Cx.Value / 9525.0;
                height = extent.Cy.Value / 9525.0;
            }
            if (width > 800) { height *= 800 / width; width = 800; }

            return new Image { Source = bmp, Width = width, Height = height, Stretch = Stretch.Uniform };
        }
        catch
        {
            return null; // Bild überspringen statt Import abbrechen
        }
    }

    // ==================== Tabellen ====================

    private static Table ConvertTable(W.Table t, MainDocumentPart main)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 6, 0, 6),
        };
        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        foreach (var row in t.Elements<W.TableRow>())
        {
            var tableRow = new TableRow();
            foreach (var cell in row.Elements<W.TableCell>())
            {
                var tableCell = new TableCell
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(6, 3, 6, 3),
                };
                int span = cell.TableCellProperties?.GridSpan?.Val?.Value ?? 1;
                if (span > 1) tableCell.ColumnSpan = span;

                foreach (var p in cell.Elements<W.Paragraph>())
                    tableCell.Blocks.Add(ConvertParagraph(p, main));
                if (tableCell.Blocks.Count == 0)
                    tableCell.Blocks.Add(new Paragraph());

                tableRow.Cells.Add(tableCell);
            }
            group.Rows.Add(tableRow);
        }
        return table;
    }

    // ==================== Listen ====================

    private static TextMarkerStyle ResolveMarker(W.NumberingProperties np, MainDocumentPart main)
    {
        try
        {
            int numId = np.NumberingId?.Val?.Value ?? 0;
            var numbering = main.NumberingDefinitionsPart?.Numbering;
            var instance = numbering?.Elements<W.NumberingInstance>()
                .FirstOrDefault(n => n.NumberID?.Value == numId);
            int absId = instance?.AbstractNumId?.Val?.Value ?? -1;
            var abs = numbering?.Elements<W.AbstractNum>()
                .FirstOrDefault(a => a.AbstractNumberId?.Value == absId);
            int ilvl = np.NumberingLevelReference?.Val?.Value ?? 0;
            var level = abs?.Elements<W.Level>().FirstOrDefault(l => l.LevelIndex?.Value == ilvl);

            return level?.NumberingFormat?.Val?.InnerText switch
            {
                "decimal" => TextMarkerStyle.Decimal,
                "lowerLetter" => TextMarkerStyle.LowerLatin,
                "upperLetter" => TextMarkerStyle.UpperLatin,
                "lowerRoman" => TextMarkerStyle.LowerRoman,
                "upperRoman" => TextMarkerStyle.UpperRoman,
                _ => TextMarkerStyle.Disc,
            };
        }
        catch
        {
            return TextMarkerStyle.Disc;
        }
    }
}
