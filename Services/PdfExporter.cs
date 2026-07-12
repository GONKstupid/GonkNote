using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.Models;
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
        switch (el)
        {
            case StrokeElement s: WhiteboardView.DrawStroke(canvas, s); break;
            case ShapeElement sh: WhiteboardView.DrawShape(canvas, sh, sh.Color, sh.StrokeWidth); break;
            case GonkNote.Models.TextElement t: WhiteboardView.DrawText(canvas, t); break;
            case ImageElement im: DrawImage(canvas, SKRect.Create(im.X, im.Y, im.Width, im.Height), im.Data); break;
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
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        canvas.DrawImage(img, rect, paint);
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
        if (page.BackgroundImage is { Length: > 0 } bgData)
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

        if (cover?.Image is { Length: > 0 } img)
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

        var typeface = SKTypeface.FromFamilyName(cover?.FontFamily ?? "Segoe UI", SKFontStyle.Bold) ?? Views.Fonts.Bold;
        using var titlePaint = new SKPaint
        {
            Color = SKColors.White, IsAntialias = true, TextSize = 46,
            Typeface = typeface, TextAlign = SKTextAlign.Center,
        };
        while (titlePaint.TextSize > 18 && titlePaint.MeasureText(title) > page.Width * 0.8f)
            titlePaint.TextSize -= 2;
        canvas.DrawText(title, page.Width / 2f, page.Height * 0.4f, titlePaint);

        using (var accent = new SKPaint
        {
            Color = SKColor.Parse("#2DD4BF"), StrokeWidth = 4, IsAntialias = true, StrokeCap = SKStrokeCap.Round,
        })
            canvas.DrawLine(page.Width * 0.3f, page.Height * 0.445f, page.Width * 0.7f, page.Height * 0.445f, accent);

        using var subPaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(170), IsAntialias = true, TextSize = 15,
            Typeface = Views.Fonts.Regular, TextAlign = SKTextAlign.Center,
        };
        canvas.DrawText("N O T I Z B U C H", page.Width / 2f, page.Height * 0.49f, subPaint);
    }

    // ==================== Textdokument ====================

    /// <summary>
    /// Rendert ein FlowDocument seitenweise (A4) über den WPF-Paginator und legt jede
    /// Seite als hochauflösendes Bild ins PDF. Muss auf dem UI-Thread laufen.
    /// </summary>
    public static void ExportFlowDocument(FlowDocument flow, string path)
    {
        using var stream = File.Create(path);
        using var pdf = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata { Producer = "Gonk Note" });
        foreach (var (skImage, pw, ph) in RenderTextPages(flow, 3f))
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

    /// <summary>Textdokument als einzelne PNG-Seiten (A4). Multi-Page → base-1.png, base-2.png …</summary>
    public static List<string> ExportFlowDocumentPng(FlowDocument flow, string path)
    {
        var pages = RenderTextPages(flow, 3f).ToList();
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
    /// Rendert das FlowDocument seitenweise (A4) direkt in hochauflösende Bitmaps.
    /// Direkt-Render (kein VisualBrush) → scharfer Text; PagePadding trägt den Rand.
    /// </summary>
    private static IEnumerable<(SKImage Image, double W, double H)> RenderTextPages(FlowDocument flow, float scale)
    {
        const double pw = WhiteboardDoc.A4Width;   // 794 (96 DPI)
        const double ph = WhiteboardDoc.A4Height;  // 1123
        const double margin = 56;

        var doc = CloneForPrint(flow, pw, margin);
        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        paginator.PageSize = new Size(pw, ph);
        paginator.ComputePageCount();

        int count = Math.Max(1, paginator.PageCount);
        for (int i = 0; i < count; i++)
        {
            using var docPage = paginator.GetPage(i);
            if (docPage == DocumentPage.Missing) yield break;

            var rtb = new RenderTargetBitmap(
                (int)Math.Round(pw * scale), (int)Math.Round(ph * scale),
                96 * scale, 96 * scale, PixelFormats.Pbgra32);

            // Weiß hinterlegen, dann die Seite direkt darüber rendern (scharf)
            var bg = new DrawingVisual();
            using (var dc = bg.RenderOpen())
                dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, pw, ph));
            rtb.Render(bg);
            rtb.Render(docPage.Visual);
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

    /// <summary>Kopie des FlowDocuments mit einer Spalte voller Breite und Seitenrand (Print-Layout).</summary>
    private static FlowDocument CloneForPrint(FlowDocument source, double pageWidth, double margin)
    {
        using var ms = new MemoryStream();
        new TextRange(source.ContentStart, source.ContentEnd).Save(ms, DataFormats.XamlPackage);
        ms.Position = 0;

        var clone = new FlowDocument
        {
            ColumnWidth = pageWidth,                  // exakt eine Spalte
            PagePadding = new Thickness(margin),      // Rand ist Teil der Seite → scharf
            FontFamily = source.FontFamily,
            FontSize = source.FontSize,
            TextAlignment = TextAlignment.Left,
        };
        new TextRange(clone.ContentStart, clone.ContentEnd).Load(ms, DataFormats.XamlPackage);
        return clone;
    }
}
