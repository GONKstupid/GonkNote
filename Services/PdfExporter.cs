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
        });

        foreach (var page in doc.Pages)
            RenderPage(pdf, page, doc, title);

        pdf.Close();
    }

    private static void RenderPage(SKDocument pdf, WbPage page, WhiteboardDoc doc, string title)
    {
        // Seitengröße bestimmen: feste Seiten direkt, unendliche über den Inhalt
        float ox = 0, oy = 0, w, h;
        if (page.IsInfinite)
        {
            var b = ContentBounds(page);
            const float margin = 48f;
            ox = b.Left - margin;
            oy = b.Top - margin;
            w = b.Width + margin * 2;
            h = b.Height + margin * 2;
        }
        else
        {
            w = page.Width;
            h = page.Height;
        }

        var canvas = pdf.BeginPage(w * PtPerUnit, h * PtPerUnit);
        canvas.Scale(PtPerUnit);
        canvas.Translate(-ox, -oy);

        DrawBackground(canvas, page, doc, title, ox, oy, w, h);

        foreach (var el in page.Elements)
            DrawElement(canvas, el);

        pdf.EndPage();
    }

    private static void DrawElement(SKCanvas canvas, WbElement el)
    {
        switch (el)
        {
            case StrokeElement s: WhiteboardView.DrawStroke(canvas, s); break;
            case ShapeElement sh: WhiteboardView.DrawShape(canvas, sh, sh.Color, sh.StrokeWidth); break;
            case GonkNote.Models.TextElement t: WhiteboardView.DrawText(canvas, t); break;
            case ImageElement im: WhiteboardView.DrawImage(canvas, im); break;
        }
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

        // Importierte PDF-Seite bzw. Cover haben Vorrang vor dem Muster
        if (page.BackgroundImage is { Length: > 0 } bgData &&
            ImageCache.Get(page.BackgroundImageId, bgData) is { } bgImg)
        {
            canvas.DrawImage(bgImg, SKRect.Create(0, 0, page.Width, page.Height));
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

        if (cover?.Image is { Length: > 0 } img && ImageCache.Get(cover.ImageId, img) is { } coverImg)
        {
            canvas.DrawImage(coverImg, rect);
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
        const double pw = WhiteboardDoc.A4Width;   // 794 (96 DPI)
        const double ph = WhiteboardDoc.A4Height;  // 1123
        const double margin = 48;
        const double contentW = pw - margin * 2;
        const double contentH = ph - margin * 2;
        const float scale = 2f;                    // 192 DPI fürs Rendern

        // Eigenes Print-Layout: eine Spalte in Content-Breite, sonst bräche der Text
        // auf Bildschirmbreite um. Paginierung erfolgt auf den Content-Bereich.
        var doc = CloneForPrint(flow, contentW);
        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        paginator.PageSize = new Size(contentW, contentH);
        paginator.ComputePageCount();

        using var stream = File.Create(path);
        using var pdf = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata { Producer = "Gonk Note" });

        int pages = Math.Max(1, paginator.PageCount);
        for (int i = 0; i < pages; i++)
        {
            using var docPage = paginator.GetPage(i);
            if (docPage == DocumentPage.Missing) break;

            var rtb = new RenderTargetBitmap(
                (int)(pw * scale), (int)(ph * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, pw, ph));
                // Seiteninhalt in Originalgröße mit Rand einsetzen
                dc.DrawRectangle(new VisualBrush(docPage.Visual) { Stretch = Stretch.None },
                    null, new Rect(margin, margin, contentW, contentH));
            }
            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            using var skImage = SKImage.FromEncodedData(ms);
            var canvas = pdf.BeginPage((float)pw * PtPerUnit, (float)ph * PtPerUnit);
            canvas.DrawImage(skImage, SKRect.Create(0, 0, (float)pw * PtPerUnit, (float)ph * PtPerUnit));
            pdf.EndPage();
        }
        pdf.Close();
    }

    /// <summary>Kopie des FlowDocuments mit einer Spalte fester Breite (verhindert Bildschirm-Umbruch).</summary>
    private static FlowDocument CloneForPrint(FlowDocument source, double contentWidth)
    {
        using var ms = new MemoryStream();
        new TextRange(source.ContentStart, source.ContentEnd).Save(ms, DataFormats.XamlPackage);
        ms.Position = 0;

        var clone = new FlowDocument
        {
            ColumnWidth = contentWidth,   // exakt eine Spalte
            FontFamily = source.FontFamily,
            FontSize = source.FontSize,
        };
        new TextRange(clone.ContentStart, clone.ContentEnd).Load(ms, DataFormats.XamlPackage);
        return clone;
    }
}
