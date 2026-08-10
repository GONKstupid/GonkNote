using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Views;
using SkiaSharp;

namespace GonkNote.Services;

/// <summary>
/// PDF-Export. Whiteboards/Notizbücher werden über SkiaSharp gerendert (dieselben
/// Zeichenroutinen wie im Editor), Textdokumente seitenweise über den WPF-Paginator
/// als scharfe Rasterseiten – so bleibt die Formatierung 1:1 erhalten.
/// </summary>
public static class PdfExporter
{
    // 96 DPI (Canvas) → 72 pt (PDF)
    private const float PtPerUnit = 72f / 96f;

    // ==================== Whiteboard / Notizbuch ====================

    public static void ExportWhiteboard(WhiteboardDoc doc, string title, string path)
    {
        using var stream = File.Create(path);
        using var pdf = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata
        {
            Title = title,
            Producer = "Gonk Note",
            // 100 = verlustfrei: Skia bettet Bilder unverändert ein, statt sie
            // erneut (stark) als JPEG zu komprimieren → importierte PDF-Seiten
            // bleiben scharf. Zusammen mit SKImage.FromEncodedData unten werden
            // die Original-Bytes direkt durchgereicht.
            EncodingQuality = 100,
        });

        foreach (var page in doc.Pages)
            RenderPage(pdf, page, doc, title);

        pdf.Close();
    }

    /// <summary>Whiteboard/Notizbuch als PNG: 1 Datei pro Seite, hohe Auflösung (2×).</summary>
    public static List<string> ExportWhiteboardPng(WhiteboardDoc doc, string title, string path)
    {
        const float scale = 2f;
        var written = new List<string>();
        string dir = Path.GetDirectoryName(path)!;
        string stem = Path.GetFileNameWithoutExtension(path);
        bool multi = doc.Pages.Count > 1;

        for (int i = 0; i < doc.Pages.Count; i++)
        {
            var page = doc.Pages[i];
            var (ox, oy, w, h) = PageGeometry(page);

            var info = new SKImageInfo((int)Math.Round(w * scale), (int)Math.Round(h * scale));
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Scale(scale);
            canvas.Translate(-ox, -oy);
            PaintPage(canvas, page, doc, title, ox, oy, w, h);

            using var img = surface.Snapshot();
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            string outPath = multi ? Path.Combine(dir, $"{stem}-{i + 1}.png") : path;
            File.WriteAllBytes(outPath, data.ToArray());
            written.Add(outPath);
        }
        return written;
    }

    /// <summary>Seitengröße/Ursprung: feste Seiten direkt, unendliche über den Inhalt (mit Rand).</summary>
    private static (float Ox, float Oy, float W, float H) PageGeometry(WbPage page)
    {
        if (!page.IsInfinite) return (0, 0, page.Width, page.Height);
        var b = ContentBounds(page);
        const float margin = 48f;
        return (b.Left - margin, b.Top - margin, b.Width + margin * 2, b.Height + margin * 2);
    }

    private static void RenderPage(SKDocument pdf, WbPage page, WhiteboardDoc doc, string title)
    {
        var (ox, oy, w, h) = PageGeometry(page);
        var canvas = pdf.BeginPage(w * PtPerUnit, h * PtPerUnit);
        canvas.Scale(PtPerUnit);
        canvas.Translate(-ox, -oy);
        PaintPage(canvas, page, doc, title, ox, oy, w, h);
        pdf.EndPage();
    }

    private static void PaintPage(SKCanvas canvas, WbPage page, WhiteboardDoc doc, string title,
        float ox, float oy, float w, float h)
    {
        DrawBackground(canvas, page, doc, title, ox, oy, w, h);
        foreach (var el in page.Elements)
            DrawElement(canvas, el);
    }

    private static void DrawElement(SKCanvas canvas, WbElement el)
    {
        if (el.Rotation != 0f)
        {
            var b = WhiteboardView.ElementBounds(el);
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

    private static void DrawElementCore(SKCanvas canvas, WbElement el)
    {
        switch (el)
        {
            case StrokeElement s: WhiteboardView.DrawStroke(canvas, s); break;
            case ShapeElement sh: WhiteboardView.DrawShape(canvas, sh, sh.Color, sh.StrokeWidth); break;
            case GonkNote.Core.Models.TextElement t: WhiteboardView.DrawText(canvas, t); break;
            case ImageElement im:
                DrawImage(canvas, SKRect.Create(im.X, im.Y, im.Width, im.Height),
                          ImageCache.Bytes(im.Id, im.Data));
                break;
            case StickyNoteElement sn: WhiteboardView.DrawSticky(canvas, sn); break;
        }
    }

    /// <summary>
    /// Zeichnet ein eingebettetes Bild aus seinen Original-Bytes (statt aus dem
    /// dekodierten <see cref="ImageCache"/> wie im Editor). Wichtig für den PDF-Export:
    /// Skia reicht so das unveränderte JPEG/PNG durch, statt es neu zu komprimieren –
    /// importierte PDF-Seiten und eingefügte Bilder bleiben dadurch scharf.
    /// </summary>
    private static void DrawImage(SKCanvas canvas, SKRect rect, byte[]? data)
    {
        using var img = data is { Length: > 0 } ? SKImage.FromEncodedData(data) : null;
        if (img == null)
        {
            using var ph = new SKPaint { Color = SKColors.Gray.WithAlpha(60) };
            canvas.DrawRect(rect, ph);
            return;
        }
        // Export rastert aus dem Original: hoechste Abtastqualitaet (frueher FilterQuality.High)
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawImage(img, rect, WbRenderer.HighSampling, paint);
    }

    private static SKRect ContentBounds(WbPage page)
    {
        if (page.Elements.Count == 0) return SKRect.Create(0, 0, WhiteboardDoc.A4Width, WhiteboardDoc.A4Height);
        bool first = true;
        SKRect r = SKRect.Empty;
        foreach (var el in page.Elements)
        {
            var b = WhiteboardView.ElementBounds(el);
            if (b.IsEmpty) continue;
            if (first) { r = b; first = false; }
            else r = SKRect.Union(r, b);
        }
        return first ? SKRect.Create(0, 0, WhiteboardDoc.A4Width, WhiteboardDoc.A4Height) : r;
    }

    private static void DrawBackground(SKCanvas canvas, WbPage page, WhiteboardDoc doc, string title,
        float ox, float oy, float w, float h)
    {
        bool dark = page.Shade == PageShade.Dark;
        var pageRect = SKRect.Create(ox, oy, w, h);

        using (var bg = new SKPaint { Color = dark ? SKColor.Parse("#1E2638") : SKColors.White })
            canvas.DrawRect(pageRect, bg);

        // Importierte PDF-Seite bzw. Cover haben Vorrang vor dem Muster.
        // Aus Original-Bytes zeichnen (nicht aus ImageCache) → verlustfreier Export.
        if (ImageCache.Bytes(page.BackgroundImageId, page.BackgroundImage) is { Length: > 0 } bgData)
        {
            DrawImage(canvas, SKRect.Create(0, 0, page.Width, page.Height), bgData);
            return;
        }
        if (page.IsCover)
        {
            DrawCover(canvas, page, doc, title);
            return;
        }

        var lineColor = dark ? SKColor.Parse("#35486E") : SKColor.Parse("#BBD2F0");
        var dotColor = dark ? SKColor.Parse("#3A4A6B") : SKColor.Parse("#B8C6DC");
        const float spacing = 30f;

        switch (page.Background)
        {
            case PageBackground.Lines:
                using (var line = new SKPaint { Color = lineColor, StrokeWidth = 1f })
                    for (float y = oy + 84; y < oy + h - 30; y += spacing)
                        canvas.DrawLine(ox + 30, y, ox + w - 30, y, line);
                break;

            case PageBackground.Grid:
                using (var line = new SKPaint { Color = lineColor, StrokeWidth = 1f })
                {
                    for (float y = oy; y <= oy + h; y += spacing) canvas.DrawLine(ox, y, ox + w, y, line);
                    for (float x = ox; x <= ox + w; x += spacing) canvas.DrawLine(x, oy, x, oy + h, line);
                }
                break;

            case PageBackground.Dots:
                using (var dot = new SKPaint { Color = dotColor, IsAntialias = true })
                    for (float x = ox + 24; x < ox + w; x += 24)
                        for (float y = oy + 24; y < oy + h; y += 24)
                            canvas.DrawCircle(x, y, 1.1f, dot);
                break;
        }
    }

    private static void DrawCover(SKCanvas canvas, WbPage page, WhiteboardDoc doc, string title)
    {
        var rect = SKRect.Create(0, 0, page.Width, page.Height);
        var cover = doc.Cover;

        // Über den Cache holen, nicht direkt aus dem Datensatz: dort stehen die Bytes nur
        // bis zum ersten Speichern, danach liegt das Cover im Blob-Speicher.
        if (cover != null && ImageCache.Bytes(cover.ImageId, cover.Image) is { Length: > 0 } img)
        {
            DrawImage(canvas, rect, img);
            return;
        }

        string startHex = cover?.GradientStart ?? "#1E3A8A";
        string endHex = cover?.GradientEnd ?? "#7C3AED";
        using (var grad = new SKPaint { IsAntialias = true })
        {
            grad.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(page.Width, page.Height),
                new[] { SKColor.Parse(startHex), SKColor.Parse(endHex) }, null, SKShaderTileMode.Clamp);
            canvas.DrawRect(rect, grad);
        }

        // **Über WbFonts und nicht über SKTypeface.FromFamilyName** (§4.26): nur so findet der
        // Cover-Titel die mitgelieferte Schrift. Ohne eigene Angabe die Rolle „Display".
        var typeface = WbFonts.Family(
            cover?.FontFamily ?? WbFonts.FamilyOf(GonkNote.Core.Theming.FontRole.Display), bold: true);
        using var titlePaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var titleFont = new SKFont(typeface, 46);
        while (titleFont.Size > 18 && titleFont.MeasureText(title) > page.Width * 0.8f)
            titleFont.Size -= 2;
        canvas.DrawText(title, page.Width / 2f, page.Height * 0.4f,
            SKTextAlign.Center, titleFont, titlePaint);

        using (var accent = new SKPaint
        {
            Color = SKColor.Parse("#2DD4BF"), StrokeWidth = 4, IsAntialias = true, StrokeCap = SKStrokeCap.Round,
        })
            canvas.DrawLine(page.Width * 0.3f, page.Height * 0.445f, page.Width * 0.7f, page.Height * 0.445f, accent);

        using var subPaint = new SKPaint { Color = SKColors.White.WithAlpha(170), IsAntialias = true };
        using var subFont = new SKFont(WbFonts.Regular, 15);
        canvas.DrawText("N O T I Z B U C H", page.Width / 2f, page.Height * 0.49f,
            SKTextAlign.Center, subFont, subPaint);
    }

    // ==================== Textdokument ====================

    /// <summary>
    /// Rendert ein FlowDocument seitenweise über den WPF-Paginator und legt jede
    /// Seite als hochauflösendes Bild ins PDF. Seitenformat, Ränder, Kopf-/Fußzeile
    /// und Wasserzeichen kommen aus den TextDoc-Einstellungen.
    /// Muss auf dem UI-Thread laufen.
    /// </summary>
    public static void ExportFlowDocument(FlowDocument flow, TextDoc settings, string title, string path)
    {
        using var stream = File.Create(path);
        using var pdf = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata
        {
            Title = title,
            Producer = "Gonk Note",
        });
        foreach (var (skImage, pw, ph) in RenderTextPages(flow, settings, title, 3f))
        {
            using (skImage)
            {
                var canvas = pdf.BeginPage((float)pw * PtPerUnit, (float)ph * PtPerUnit);
                canvas.DrawImage(skImage, SKRect.Create(0, 0, (float)pw * PtPerUnit, (float)ph * PtPerUnit));
                pdf.EndPage();
            }
        }
        pdf.Close();
    }

    /// <summary>Textdokument als einzelne PNG-Seiten. Multi-Page → base-1.png, base-2.png …</summary>
    public static List<string> ExportFlowDocumentPng(FlowDocument flow, TextDoc settings, string title, string path)
    {
        var pages = RenderTextPages(flow, settings, title, 3f).ToList();
        var written = new List<string>();
        string dir = Path.GetDirectoryName(path)!;
        string stem = Path.GetFileNameWithoutExtension(path);

        for (int i = 0; i < pages.Count; i++)
        {
            var (skImage, _, _) = pages[i];
            using (skImage)
            {
                string outPath = pages.Count == 1 ? path : Path.Combine(dir, $"{stem}-{i + 1}.png");
                using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(outPath, data.ToArray());
                written.Add(outPath);
            }
        }
        return written;
    }

    /// <summary>
    /// Rendert ein FlowDocument seitenweise zu JPEG-Bildern — für das
    /// Datei-Einfüge-Tool (DOCX → Bildseiten ins Whiteboard/Notizbuch).
    /// Muss auf dem UI-Thread laufen (WPF-Paginator).
    /// </summary>
    public static List<PdfImporter.PdfPageImage> RenderFlowDocumentPages(
        FlowDocument flow, TextDoc settings, string title, float scale = 2f)
    {
        var result = new List<PdfImporter.PdfPageImage>();
        foreach (var (skImage, _, _) in RenderTextPages(flow, settings, title, scale))
            using (skImage)
            {
                using var data = skImage.Encode(SKEncodedImageFormat.Jpeg, 90);
                result.Add(new PdfImporter.PdfPageImage(data.ToArray(), skImage.Width, skImage.Height));
            }
        return result;
    }

    /// <summary>
    /// Rendert das FlowDocument seitenweise direkt in hochauflösende Bitmaps.
    /// Direkt-Render (kein VisualBrush) → scharfer Text; PagePadding trägt den Rand.
    /// Wasserzeichen liegt hinter, Kopf-/Fußzeile in den Randbereichen über dem Inhalt.
    /// </summary>
    private static IEnumerable<(SKImage Image, double W, double H)> RenderTextPages(
        FlowDocument flow, TextDoc settings, string title, float scale)
    {
        var (pw, ph) = TextStyles.PageSize(settings);
        var margin = TextStyles.PageMarginPx(settings);

        // Für die Kopie die Originale einsetzen: gerastert wird aus dem Original, nicht aus
        // der Anzeige-Ableitung (siehe DocumentImages).
        FlowDocument doc;
        using (DocumentImages.WithFullResolution(flow, BlobStore.Current!))
            doc = CloneForPrint(flow, pw, margin);

        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        paginator.PageSize = new Size(pw, ph);
        paginator.ComputePageCount();

        // Wasserzeichen einmal dekodieren
        BitmapImage? watermark = null;
        if (settings.WatermarkImage is { Length: > 0 } wmBytes)
        {
            try
            {
                using var wms = new MemoryStream(wmBytes);
                watermark = new BitmapImage();
                watermark.BeginInit();
                watermark.CacheOption = BitmapCacheOption.OnLoad;
                watermark.StreamSource = wms;
                watermark.EndInit();
                watermark.Freeze();
            }
            catch
            {
                watermark = null;
            }
        }

        int count = Math.Max(1, paginator.PageCount);
        for (int i = 0; i < count; i++)
        {
            using var docPage = paginator.GetPage(i);
            if (docPage == DocumentPage.Missing) yield break;

            var rtb = new RenderTargetBitmap(
                (int)Math.Round(pw * scale), (int)Math.Round(ph * scale),
                96 * scale, 96 * scale, PixelFormats.Pbgra32);

            // Weiß + Wasserzeichen hinterlegen, dann die Seite direkt darüber rendern
            var bg = new DrawingVisual();
            using (var dc = bg.RenderOpen())
            {
                dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, pw, ph));
                if (watermark != null)
                {
                    if (settings.WatermarkOpacity < 0.999)
                        dc.PushOpacity(settings.WatermarkOpacity);
                    dc.DrawImage(watermark, FillRect(watermark, pw, ph));
                    if (settings.WatermarkOpacity < 0.999)
                        dc.Pop();
                }
            }
            rtb.Render(bg);
            rtb.Render(docPage.Visual);

            // Kopf-/Fußzeile in die Randbereiche
            bool suppress = settings.SuppressHeaderOnFirstPage && i == 0;
            if (!suppress && (settings.HeaderText.Length > 0 || settings.FooterText.Length > 0))
            {
                var hf = new DrawingVisual();
                using (var dc = hf.RenderOpen())
                {
                    if (settings.HeaderText.Length > 0)
                        DrawHeaderFooter(dc, settings.HeaderText, i + 1, count, title,
                            pw, margin, y: Math.Max(4, (margin.Top - 16) / 2));
                    if (settings.FooterText.Length > 0)
                        DrawHeaderFooter(dc, settings.FooterText, i + 1, count, title,
                            pw, margin, y: ph - margin.Bottom + Math.Max(2, (margin.Bottom - 16) / 2));
                }
                rtb.Render(hf);
            }
            rtb.Freeze();

            // Verlustfrei über PNG an SkiaSharp übergeben (formatunabhängig, scharf)
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;
            yield return (SKImage.FromEncodedData(ms), pw, ph);
        }
    }

    /// <summary>„Seitenfüllend“: Bild proportional so skalieren, dass es die Seite abdeckt (Mitte).</summary>
    private static Rect FillRect(BitmapSource img, double pw, double ph)
    {
        double s = Math.Max(pw / img.PixelWidth, ph / img.PixelHeight);
        double w = img.PixelWidth * s, h = img.PixelHeight * s;
        return new Rect((pw - w) / 2, (ph - h) / 2, w, h);
    }

    private static void DrawHeaderFooter(DrawingContext dc, string template, int page, int pages,
        string title, double pw, Thickness margin, double y)
    {
        string text = TextStyles.ResolveHeaderFooter(template, page, pages, title);
        var ft = new FormattedText(text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(GonkNote.Core.Theming.Fonts.Standard.Family(GonkNote.Core.Theming.FontRole.Ui)),
            12,
            new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6B, 0x7A, 0x99)),
            1.0)
        {
            TextAlignment = TextAlignment.Center,
            MaxTextWidth = Math.Max(40, pw - margin.Left - margin.Right),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
        };
        dc.DrawText(ft, new Point(margin.Left, y));
    }

    /// <summary>Kopie des FlowDocuments mit einer Spalte voller Breite und Seitenrand (Print-Layout).</summary>
    private static FlowDocument CloneForPrint(FlowDocument source, double pageWidth, Thickness margin)
    {
        using var ms = new MemoryStream();
        new TextRange(source.ContentStart, source.ContentEnd).Save(ms, DataFormats.XamlPackage);
        ms.Position = 0;

        var clone = new FlowDocument
        {
            ColumnWidth = pageWidth,                  // exakt eine Spalte
            PagePadding = margin,                     // Rand ist Teil der Seite → scharf
            FontFamily = source.FontFamily,
            FontSize = source.FontSize,
            TextAlignment = TextAlignment.Left,
        };
        new TextRange(clone.ContentStart, clone.ContentEnd).Load(ms, DataFormats.XamlPackage);
        return clone;
    }
}
