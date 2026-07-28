using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using SkiaSharp;

namespace GonkNote.Core.Services;

/// <summary>
/// Rendert PDF-Seiten über PDFium (Docnet.Core) zu JPEG-Bildern — für den Import als
/// Notizbuch-Seiten bzw. Whiteboard-Bilder.
/// <para>
/// **Nichts wird am Stück geladen.** PDFium öffnet die Datei über ihren Pfad statt über ein
/// <c>byte[]</c>, und die Seiten kommen einzeln heraus. Vorher lag erst die ganze Datei und
/// dann jede gerenderte Seite gleichzeitig im Speicher — bei einem PDF im Gigabyte-Bereich
/// war damit Schluss, lange bevor die Datenbank an ihre Grenze kam.
/// </para>
/// </summary>
public static class PdfImporter
{
    /// <summary>Ein gerendertes PDF-Blatt; Width/Height sind die Pixelmaße des Bildes.</summary>
    public sealed record PdfPageImage(byte[] Data, int Width, int Height);

    /// <summary>Auflösung der Vorschaubilder in der Seitenauswahl (lange Kante).</summary>
    public const int ThumbnailLongSide = 260;

    public static int PageCount(string path)
    {
        using var doc = DocLib.Instance.GetDocReader(path, new PageDimensions(1, 1));
        return doc.GetPageCount();
    }

    /// <summary>
    /// Rendert Seiten einzeln und gibt sie sofort weiter – es liegt nie mehr als eine Seite
    /// im Speicher. <paramref name="only"/> schränkt auf bestimmte Seitennummern ein
    /// (null = alle); genau das spart bei großen Dateien die meiste Zeit, denn aus tausend
    /// Seiten fünf auszuwählen rendert dann auch nur fünf.
    /// <para>CPU-intensiv – gehört auf einen Hintergrund-Thread.</para>
    /// </summary>
    public static IEnumerable<PdfPageImage> StreamPages(
        string path, int targetLongSide,
        IReadOnlyCollection<int>? only = null,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default)
    {
        using var doc = DocLib.Instance.GetDocReader(path, new PageDimensions(targetLongSide, targetLongSide));
        int count = doc.GetPageCount();
        int total = only?.Count ?? count;
        progress?.Report((0, total));

        int done = 0;
        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (only != null && !only.Contains(i)) continue;

            var image = RenderOne(doc, i);
            progress?.Report((++done, total));
            if (image != null) yield return image;
        }
    }

    /// <summary>
    /// Alle Seiten als Liste. Nur für kleine Auflösungen gedacht (Vorschaubilder) – bei
    /// voller Auflösung <see cref="StreamPages"/> verwenden, sonst liegt alles im Speicher.
    /// </summary>
    public static List<PdfPageImage> RenderPages(
        string path, int targetLongSide,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default) =>
        StreamPages(path, targetLongSide, null, progress, ct).ToList();

    private static PdfPageImage? RenderOne(IDocReader doc, int index)
    {
        using var page = doc.GetPageReader(index);
        int w = page.GetPageWidth();
        int h = page.GetPageHeight();
        byte[] bgra = page.GetImage();
        if (w <= 0 || h <= 0 || bgra.Length < w * h * 4) return null;

        using var bmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul));
        Marshal.Copy(bgra, 0, bmp.GetPixels(), w * h * 4);

        // PDFium liefert transparente Flächen → auf Weiß zusammenführen
        using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawBitmap(bmp, 0, 0);

        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Jpeg, 90);
        return new PdfPageImage(data.ToArray(), w, h);
    }
}
