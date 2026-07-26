using GonkNote.Core.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using TextDoc = GonkNote.Core.Models.TextDoc;  // Models.TextElement würde mit WPF kollidieren
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace GonkNote.Services;

/// <summary>
/// Exportiert ein WPF-FlowDocument als DOCX (OpenXML) – die Gegenrichtung zum
/// <see cref="DocxImporter"/>. Zusätzlich zur Zeichen-/Absatzformatierung werden
/// exportiert: Seitenformat/Ränder (sectPr), Kopf-/Fußzeile mit PAGE-Feldern,
/// echte Überschriften-Styles (Heading1–4 mit Gliederungsebene → Word-Navigation
/// und automatisches Inhaltsverzeichnis), Zellschattierung/Spaltenbreiten und
/// Hyperlinks. Ein von Gonk Note erzeugtes Inhaltsverzeichnis wird durch ein
/// echtes TOC-Feld ersetzt (Word aktualisiert es beim Öffnen).
/// </summary>
public static class DocxExporter
{
    private const int BulletNumId = 1;
    private const int DecimalNumId = 2;

    // px (96 dpi) → Twips (1/20 pt, 1440/Zoll)
    private const double PxToTwip = 15.0;

    /// <summary>Nicht vererbte Zeichenformate, die von Eltern-Spans mitgetragen werden.</summary>
    private readonly record struct Acc(bool Underline, bool Strike, string? Vert, string? Highlight)
    {
        public static readonly Acc None = new();
    }

    private sealed class Ctx
    {
        public uint ImageId = 1;
        public bool SkipTocEntries;
    }

    /// <summary>Export inkl. Validierung. Rückgabe: Anzahl der Validierungsfehler (0 = sauber).</summary>
    public static int Export(FlowDocument flow, TextDoc settings, string title, string path)
    {
        using (var docx = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
        {
            var main = docx.AddMainDocumentPart();
            main.Document = new W.Document(new W.Body());
            var body = main.Document.Body!;

            AddNumberingDefinitions(main);
            AddStyleDefinitions(main);

            // Felder (TOC/PAGE) beim Öffnen aktualisieren lassen
            var settingsPart = main.AddNewPart<DocumentSettingsPart>();
            settingsPart.Settings = new W.Settings(new W.UpdateFieldsOnOpen { Val = true });

            var ctx = new Ctx();
            foreach (var block in flow.Blocks)
                WriteBlock(block, body, main, null, ctx);

            if (!body.HasChildren) body.AppendChild(new W.Paragraph());

            body.AppendChild(BuildSectionProperties(main, settings, title));
            main.Document.Save();
        }

        // Dokumentvalidierung (Wunschliste "lernblatt-generator")
        using var reopened = WordprocessingDocument.Open(path, false);
        return new OpenXmlValidator(FileFormatVersions.Office2019).Validate(reopened).Count();
    }

    // ==================== Styles ====================

    private static void AddStyleDefinitions(MainDocumentPart main)
    {
        var part = main.AddNewPart<StyleDefinitionsPart>();
        var styles = new W.Styles();

        styles.AppendChild(new W.Style(
            new W.StyleName { Val = "Normal" })
        {
            Type = W.StyleValues.Paragraph,
            StyleId = "Normal",
            Default = true,
        });

        foreach (var s in TextStyles.All.Where(s => s.HeadingLevel > 0))
        {
            int lvl = s.HeadingLevel;
            string colorHex = s.ColorHex!.TrimStart('#');
            // Schema-Reihenfolge beachten: b, i, color, sz
            var runProps = new W.StyleRunProperties();
            if (s.Weight >= FontWeights.Bold) runProps.AppendChild(new W.Bold());
            if (s.Style == FontStyles.Italic) runProps.AppendChild(new W.Italic());
            runProps.AppendChild(new W.Color { Val = colorHex });
            runProps.AppendChild(new W.FontSize { Val = ((int)Math.Round(s.Size * 1.5)).ToString() });

            styles.AppendChild(new W.Style(
                new W.StyleName { Val = $"heading {lvl}" },
                new W.BasedOn { Val = "Normal" },
                new W.NextParagraphStyle { Val = "Normal" },
                new W.PrimaryStyle(),
                new W.StyleParagraphProperties(
                    new W.KeepNext(),
                    new W.SpacingBetweenLines
                    {
                        Before = ((int)(s.Margin.Top * PxToTwip)).ToString(),
                        After = ((int)(s.Margin.Bottom * PxToTwip)).ToString(),
                    },
                    new W.OutlineLevel { Val = lvl - 1 }),
                runProps)
            {
                Type = W.StyleValues.Paragraph,
                StyleId = $"Heading{lvl}",
            });
        }

        part.Styles = styles;
    }

    // ==================== Seiteneinrichtung / Kopf- & Fußzeile ====================

    private static W.SectionProperties BuildSectionProperties(MainDocumentPart main, TextDoc settings, string title)
    {
        var (pw, ph) = TextStyles.PageSize(settings);
        var sect = new W.SectionProperties();

        if (settings.HeaderText.Length > 0)
        {
            var headerPart = main.AddNewPart<HeaderPart>();
            headerPart.Header = new W.Header(BuildHeaderFooterParagraph(settings.HeaderText, title));
            sect.AppendChild(new W.HeaderReference
            {
                Type = W.HeaderFooterValues.Default,
                Id = main.GetIdOfPart(headerPart),
            });
        }
        if (settings.FooterText.Length > 0)
        {
            var footerPart = main.AddNewPart<FooterPart>();
            footerPart.Footer = new W.Footer(BuildHeaderFooterParagraph(settings.FooterText, title));
            sect.AppendChild(new W.FooterReference
            {
                Type = W.HeaderFooterValues.Default,
                Id = main.GetIdOfPart(footerPart),
            });
        }

        var pgSz = new W.PageSize
        {
            Width = (uint)Math.Round(pw * PxToTwip),
            Height = (uint)Math.Round(ph * PxToTwip),
        };
        if (settings.Landscape) pgSz.Orient = W.PageOrientationValues.Landscape;
        sect.AppendChild(pgSz);

        const double cmToTwip = 566.929;
        sect.AppendChild(new W.PageMargin
        {
            Left = (uint)Math.Round(settings.MarginLeftCm * cmToTwip),
            Top = (int)Math.Round(settings.MarginTopCm * cmToTwip),
            Right = (uint)Math.Round(settings.MarginRightCm * cmToTwip),
            Bottom = (int)Math.Round(settings.MarginBottomCm * cmToTwip),
            Header = (uint)Math.Round(settings.MarginTopCm * cmToTwip / 2),
            Footer = (uint)Math.Round(settings.MarginBottomCm * cmToTwip / 2),
            Gutter = 0U,
        });

        // "Unterschiedliche erste Seite": ohne eigene First-Page-Parts bleibt Seite 1 leer
        if (settings.SuppressHeaderOnFirstPage)
            sect.AppendChild(new W.TitlePage());

        return sect;
    }

    /// <summary>Kopf-/Fußzeilentext mit {SEITE}/{SEITEN} als echte Felder, Rest als Text.</summary>
    private static W.Paragraph BuildHeaderFooterParagraph(string template, string title)
    {
        string text = template
            .Replace("{DATUM}", DateTime.Now.ToString("dd.MM.yyyy"))
            .Replace("{TITEL}", title);

        var para = new W.Paragraph(new W.ParagraphProperties(
            new W.Justification { Val = W.JustificationValues.Center }));

        static W.RunProperties SmallMuted() => new(
            new W.Color { Val = "6B7A99" },
            new W.FontSize { Val = "18" });  // 9 pt

        foreach (var part in System.Text.RegularExpressions.Regex.Split(text, @"(\{SEITEN?\})"))
        {
            switch (part)
            {
                case "":
                    break;
                case "{SEITE}" or "{SEITEN}":
                    para.AppendChild(new W.SimpleField(
                        new W.Run(SmallMuted(), new W.Text("1")))
                    {
                        Instruction = part == "{SEITE}" ? " PAGE " : " NUMPAGES ",
                    });
                    break;
                default:
                    para.AppendChild(new W.Run(SmallMuted(),
                        new W.Text(part) { Space = SpaceProcessingModeValues.Preserve }));
                    break;
            }
        }

        return para;
    }

    // ==================== Blöcke ====================

    private static void WriteBlock(Block block, OpenXmlElement target, MainDocumentPart main,
        int? numId, Ctx ctx)
    {
        switch (block)
        {
            case Paragraph p:
            {
                // Von Gonk Note generierte TOC-Einträge → durch echtes TOC-Feld ersetzt
                if (ctx.SkipTocEntries && TextStyles.IsTocEntry(p)) return;
                ctx.SkipTocEntries = false;

                target.AppendChild(BuildParagraph(p, main, numId, ctx));

                if (IsTocTitle(p))
                {
                    target.AppendChild(new W.Paragraph(new W.SimpleField(
                        new W.Run(new W.Text("(Inhaltsverzeichnis – wird beim Öffnen in Word aktualisiert)")))
                    {
                        Instruction = " TOC \\o \"1-4\" \\h \\z \\u ",
                    }));
                    ctx.SkipTocEntries = true;
                }
                break;
            }

            case List list:
            {
                int id = IsOrdered(list.MarkerStyle) ? DecimalNumId : BulletNumId;
                foreach (var li in list.ListItems)
                    foreach (var b in li.Blocks)
                        WriteBlock(b, target, main, id, ctx);
                break;
            }

            case Table table:
                ctx.SkipTocEntries = false;
                target.AppendChild(BuildTable(table, main, ctx));
                break;

            case BlockUIContainer or Section:
                // Trennlinien u. Ä.: als leerer Absatz mit Unterstrich
                ctx.SkipTocEntries = false;
                target.AppendChild(new W.Paragraph(new W.ParagraphProperties(
                    new W.ParagraphBorders(new W.BottomBorder
                    {
                        Val = W.BorderValues.Single, Size = 6, Color = "AAAAAA",
                    }))));
                break;
        }
    }

    private static bool IsTocTitle(Paragraph p) =>
        new TextRange(p.ContentStart, p.ContentEnd).Text.Trim() == TextStyles.TocTitle &&
        new TextRange(p.ContentStart, p.ContentEnd)
            .GetPropertyValue(TextElement.FontSizeProperty) is double size && size >= 20;

    private static W.Paragraph BuildParagraph(Paragraph p, MainDocumentPart main, int? numId, Ctx ctx)
    {
        var para = new W.Paragraph();
        var props = new W.ParagraphProperties();

        // Überschrift? → echter Word-Style (Gliederungsebene, Navigation, TOC)
        int heading = TextStyles.HeadingLevel(p);
        if (heading > 0)
            props.ParagraphStyleId = new W.ParagraphStyleId { Val = $"Heading{heading}" };

        props.Justification = new W.Justification
        {
            Val = p.TextAlignment switch
            {
                TextAlignment.Center => W.JustificationValues.Center,
                TextAlignment.Right => W.JustificationValues.Right,
                TextAlignment.Justify => W.JustificationValues.Both,
                _ => W.JustificationValues.Left,
            },
        };

        // Absatzabstände (Abstand vor/nach) + Einzug
        if (p.Margin.Top > 0 || p.Margin.Bottom > 0)
            props.SpacingBetweenLines = new W.SpacingBetweenLines
            {
                Before = ((int)Math.Round(p.Margin.Top * PxToTwip)).ToString(),
                After = ((int)Math.Round(p.Margin.Bottom * PxToTwip)).ToString(),
            };
        if (p.Margin.Left > 0)
            props.Indentation = new W.Indentation
            {
                Left = ((int)Math.Round(p.Margin.Left * PxToTwip)).ToString(),
            };

        if (numId is int id)
            props.NumberingProperties = new W.NumberingProperties(
                new W.NumberingLevelReference { Val = 0 },
                new W.NumberingId { Val = id });
        para.AppendChild(props);

        foreach (var inline in p.Inlines)
            WriteInline(inline, para, main, Acc.None, ctx);
        return para;
    }

    // ==================== Inlines ====================

    private static void WriteInline(Inline inline, OpenXmlElement target, MainDocumentPart main,
        Acc acc, Ctx ctx)
    {
        switch (inline)
        {
            case Run run:
                if (run.Text.Length > 0)
                    target.AppendChild(BuildRun(run, run.Text, Combine(acc, run)));
                break;

            case Hyperlink { NavigateUri: { } uri } link:
            {
                var rel = main.AddHyperlinkRelationship(uri, true);
                var wLink = new W.Hyperlink { Id = rel.Id };
                var a = Combine(acc, link);
                foreach (var child in link.Inlines)
                    WriteInline(child, wLink, main, a, ctx);
                target.AppendChild(wLink);
                break;
            }

            case Span span:  // Bold/Italic/Underline sind Spans
            {
                var a = Combine(acc, span);
                foreach (var child in span.Inlines)
                    WriteInline(child, target, main, a, ctx);
                break;
            }

            case LineBreak:
                target.AppendChild(new W.Run(new W.Break()));
                break;

            case InlineUIContainer { Child: Image img }:
                var drawing = BuildImage(img, main, ctx);
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

    /// <summary>
    /// Wo im Gitter jede Zelle sitzt. <paramref name="Continuations"/> hält je Zeile die
    /// Fortsetzungszellen der Zeilenverbünde (Spalte + Breite).
    /// </summary>
    private sealed record GridLayout(
        Dictionary<TableCell, int> Column,
        Dictionary<int, List<(int Col, int Span)>> Continuations);

    private static W.Table BuildTable(Table table, MainDocumentPart main, Ctx ctx)
    {
        var t = NewTableWithBorders();
        AppendColumnGrid(t, table);

        var rows = table.RowGroups.SelectMany(g => g.Rows).ToList();
        var grid = MapGridPositions(rows);

        for (int r = 0; r < rows.Count; r++)
            t.AppendChild(BuildRow(rows[r], r, grid, main, ctx));

        return t;
    }

    /// <summary>Leere Tabelle mit Standardrahmen (Schema-Reihenfolge: top, left, bottom, right, insideH, insideV).</summary>
    private static W.Table NewTableWithBorders() =>
        new(new W.TableProperties(new W.TableBorders(
            new W.TopBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.LeftBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.BottomBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.RightBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" },
            new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 4, Color = "999999" })));

    /// <summary>
    /// Spaltenraster (tblGrid) mit den im Editor gesetzten Breiten. OOXML verlangt das
    /// Raster in **jeder** Tabelle – fehlte es (Tabellen mit lauter Auto-Spalten, etwa aus
    /// dem Markdown-Import), meldete die Validierung beim Export einen Schemafehler.
    /// </summary>
    private static void AppendColumnGrid(W.Table t, Table table)
    {
        int columns = table.Columns.Count > 0
            ? table.Columns.Count
            : table.RowGroups.SelectMany(g => g.Rows).Max(r => r.Cells.Sum(c => c.ColumnSpan));

        var grid = new W.TableGrid();
        for (int i = 0; i < columns; i++)
        {
            var gc = new W.GridColumn();
            // Auto-Spalten: Breitenattribut weglassen (w="" wäre ungültig)
            if (i < table.Columns.Count && table.Columns[i].Width.IsAbsolute)
                gc.Width = ((int)Math.Round(table.Columns[i].Width.Value * PxToTwip)).ToString();
            grid.AppendChild(gc);
        }
        t.AppendChild(grid);
    }

    /// <summary>
    /// Ordnet jede Zelle ihrer Gitterspalte zu. Zellen mit RowSpan &gt; 1 brauchen in OOXML
    /// vMerge=Restart plus Fortsetzungszellen in den Folgezeilen – die werden hier mitgesammelt.
    /// </summary>
    private static GridLayout MapGridPositions(List<TableRow> rows)
    {
        var occupied = new HashSet<(int Row, int Col)>();
        var column = new Dictionary<TableCell, int>();
        var continuations = new Dictionary<int, List<(int Col, int Span)>>();

        for (int r = 0; r < rows.Count; r++)
        {
            int col = 0;
            foreach (var cell in rows[r].Cells)
            {
                while (occupied.Contains((r, col))) col++;
                column[cell] = col;

                for (int rr = r; rr < r + cell.RowSpan; rr++)
                    for (int cc = col; cc < col + cell.ColumnSpan; cc++)
                        occupied.Add((rr, cc));

                for (int rr = r + 1; rr < r + cell.RowSpan; rr++)
                {
                    if (!continuations.TryGetValue(rr, out var list)) continuations[rr] = list = new();
                    list.Add((col, cell.ColumnSpan));
                }
                col += cell.ColumnSpan;
            }
        }
        return new GridLayout(column, continuations);
    }

    private static W.TableRow BuildRow(TableRow row, int r, GridLayout grid, MainDocumentPart main, Ctx ctx)
    {
        var entries = new List<(int Col, W.TableCell Tc)>();
        foreach (var cell in row.Cells)
            entries.Add((grid.Column[cell], BuildCell(cell, main, ctx)));

        // Fortsetzungszellen der Zeilenverbünde (vMerge ohne Val = Continue)
        if (grid.Continuations.TryGetValue(r, out var merges))
            foreach (var (col, span) in merges)
            {
                var tcp = new W.TableCellProperties();
                if (span > 1) tcp.AppendChild(new W.GridSpan { Val = span });
                tcp.AppendChild(new W.VerticalMerge());
                entries.Add((col, new W.TableCell(tcp, new W.Paragraph())));
            }

        var tr = new W.TableRow();
        foreach (var (_, tc) in entries.OrderBy(x => x.Col)) tr.AppendChild(tc);
        return tr;
    }

    private static W.TableCell BuildCell(TableCell cell, MainDocumentPart main, Ctx ctx)
    {
        var tc = new W.TableCell();

        // Schema-Reihenfolge in tcPr: gridSpan → vMerge → tcBorders → shd
        var tcp = new W.TableCellProperties();
        if (cell.ColumnSpan > 1)
            tcp.AppendChild(new W.GridSpan { Val = cell.ColumnSpan });
        if (cell.RowSpan > 1)
            tcp.AppendChild(new W.VerticalMerge { Val = W.MergedCellValues.Restart });

        // Abweichende Rahmenfarbe/unsichtbare Rahmen je Zelle
        if (cell.BorderBrush is SolidColorBrush bb)
        {
            var val = cell.BorderThickness.Left <= 0 ? W.BorderValues.None : W.BorderValues.Single;
            string color = Hex(bb.Color);
            tcp.AppendChild(new W.TableCellBorders(
                new W.TopBorder { Val = val, Size = 4, Color = color },
                new W.LeftBorder { Val = val, Size = 4, Color = color },
                new W.BottomBorder { Val = val, Size = 4, Color = color },
                new W.RightBorder { Val = val, Size = 4, Color = color }));
        }

        // Zellschattierung (farbige Info-Boxen des Lernblatt-Skills)
        if (cell.Background is SolidColorBrush bg)
            tcp.AppendChild(new W.Shading
            {
                Val = W.ShadingPatternValues.Clear,
                Fill = Hex(bg.Color),
                Color = "auto",
            });

        if (tcp.HasChildren) tc.AppendChild(tcp);

        foreach (var b in cell.Blocks)
            WriteBlock(b, tc, main, null, ctx);
        if (!tc.Elements<W.Paragraph>().Any()) tc.AppendChild(new W.Paragraph());
        return tc;
    }

    // ==================== Bilder ====================

    /// <summary>
    /// Bildtyp aus der Endung des Originals. Unbekanntes geht als PNG raus – lieber ein
    /// größeres Bild als ein kaputtes.
    /// </summary>
    private static PartTypeInfo PartTypeOf(string extension) => extension.ToLowerInvariant() switch
    {
        "jpg" or "jpeg" => ImagePartType.Jpeg,
        "gif" => ImagePartType.Gif,
        "bmp" => ImagePartType.Bmp,
        "tif" or "tiff" => ImagePartType.Tiff,
        _ => ImagePartType.Png,
    };

    /// <summary>
    /// Bilddaten fürs DOCX. Liegt das **Original** im Blob-Speicher, geht es unverändert
    /// hinaus – sonst würde WPF jedes Foto als PNG neu kodieren und die Datei um ein
    /// Vielfaches aufblähen (gemessen: 2 MB Vorlage → 16,8 MB Export).
    /// </summary>
    private static (byte[] Data, PartTypeInfo Type)? ImageBytes(Image img)
    {
        if (img.Tag is BlobRef reference && BlobStore.Current!.Read(reference.Id) is { } original)
            return (original, PartTypeOf(reference.Extension));

        if (img.Source is not BitmapSource src) return null;

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(src));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return (ms.ToArray(), ImagePartType.Png);
    }

    private static W.Drawing? BuildImage(Image img, MainDocumentPart main, Ctx ctx)
    {
        if (img.Source is not BitmapSource src) return null;
        try
        {
            if (ImageBytes(img) is not { } bytes) return null;

            var part = main.AddImagePart(bytes.Type);
            using (var ms = new MemoryStream(bytes.Data)) part.FeedData(ms);
            string rId = main.GetIdOfPart(part);

            double wPx = double.IsNaN(img.Width) ? src.PixelWidth : img.Width;
            double hPx = double.IsNaN(img.Height)
                ? (double.IsNaN(img.Width) ? src.PixelHeight : img.Width * src.PixelHeight / src.PixelWidth)
                : img.Height;
            long cx = (long)(wPx * 9525);
            long cy = (long)(hPx * 9525);

            uint id = ctx.ImageId++;
            return new W.Drawing(new DW.Inline(
                new DW.Extent { Cx = cx, Cy = cy },
                new DW.DocProperties { Id = id, Name = $"Bild {id}" },
                new A.Graphic(new A.GraphicData(
                    new PIC.Picture(
                        new PIC.NonVisualPictureProperties(
                            new PIC.NonVisualDrawingProperties { Id = 0U, Name = $"Bild {id}" },
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
