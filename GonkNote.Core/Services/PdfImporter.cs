using System.IO;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using SkiaSharp;

namespace GonkNote.Services;

/// <summary>
/// Rendert PDF-Seiten über PDFium (Docnet.Core) zu hochauflösenden JPEG-Bildern —
/// für den Import als Notizbuch-Seiten bzw. Whiteboard-Bilder.
/// </summary>
public static class PdfImporter
{
    /// <summary>Ein gerendertes PDF-Blatt; Width/Height sind die Pixelmaße des Bildes.</summary>
    public sealed record PdfPageImage(byte[] Data, int Width, int Height);

    /// <summary>
    /// Rendert alle Seiten. targetLongSide steuert die Auflösung der langen Kante
    /// in Pixeln (z. B. 2246 ≈ 200 % einer A4-Seite bei 96 DPI). Der Aufruf ist
    /// CPU-intensiv und gehört auf einen Hintergrund-Thread; progress meldet
    /// (fertige, gesamt) und ct erlaubt Abbruch.
    /// </summary>
    public static List<PdfPageImage> RenderPages(
        string path, int targetLongSide,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default)
    {
        var result = new List<PdfPageImage>();
        byte[] file = File.ReadAllBytes(path);

        using var doc = DocLib.Instance.GetDocReader(file, new PageDimensions(targetLongSide, targetLongSide));
        int count = doc.GetPageCount();
        progress?.Report((0, count));

        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            using var page = doc.GetPageReader(i);
            int w = page.GetPageWidth();
            int h = page.GetPageHeight();
            byte[] bgra = page.GetImage();
            if (w <= 0 || h <= 0 || bgra.Length < w * h * 4) continue;

            using var bmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul));
            Marshal.Copy(bgra, 0, bmp.GetPixels(), w * h * 4);

            // PDFium liefert transparente Flächen → auf Weiß zusammenführen
            using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
            surface.Canvas.Clear(SKColors.White);
            surface.Canvas.DrawBitmap(bmp, 0, 0);

            using var img = surface.Snapshot();
            using var data = img.Encode(SKEncodedImageFormat.Jpeg, 90);
            result.Add(new PdfPageImage(data.ToArray(), w, h));
            progress?.Report((i + 1, count));
        }
        return result;
    }
}
