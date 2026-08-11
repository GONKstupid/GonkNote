using System.IO;
using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Views;
using SkiaSharp;

namespace GonkNote.Services;

/// <summary>
/// PDF- und PNG-Export von <b>Whiteboards und Notizbüchern</b> — über SkiaSharp und dieselben
/// Zeichenroutinen wie im Editor.
///
/// <para>
/// <b>Textdokumente stehen seit §4.27 nicht mehr hier.</b> Sie gehen über
/// <see cref="GonkNote.Core.Text.TdPdf"/> in Core, gegen das Dokumentmodell statt gegen ein
/// <c>FlowDocument</c> — damit läuft dieser Weg auch unter Linux und iPadOS, und der Text im
/// PDF ist Text statt Rasterbild.
/// </para>
/// <para>
/// <b>Der Whiteboard-Weg bleibt hier</b>, und das ist kein Versehen: Er zeichnet über
/// <c>WhiteboardView</c>, also über den Kopf. Ihn nach Core zu ziehen ist die Aufgabe des
/// Linux-Whiteboards (Phase 4.5) und nicht die des Umverdrahtens.
/// </para>
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

    // ==================== Textdokument: gelöscht (§4.27) ====================
    //
    // Hier standen `ExportFlowDocument`, `ExportFlowDocumentPng`, `RenderFlowDocumentPages`
    // und der Paginator-Weg dahinter. Sie sind weg, nicht auskommentiert — dieselbe
    // Entscheidung wie bei `DocxExporter` und `MarkdownExporter` in §4.23, und aus demselben
    // Grund: **zwei Wege parallel zu pflegen ist die Falle aus §4.10.** Der Ersatz steht in
    // Core (`TdPdf`), geht gegen das Modell und läuft auf jedem Kopf.
    //
    // Was dabei besser wurde, war nicht nur der Ort: Der alte Weg rasterte jede Seite zu
    // einem Bild — 300 dpi, aber ohne auswählbaren Text, ohne Suche, ohne anklickbaren
    // Verweis. `TdPdf` zeichnet direkt auf die PDF-Leinwand; der Text im PDF ist Text.

}
