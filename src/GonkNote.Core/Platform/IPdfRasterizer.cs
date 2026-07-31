using GonkNote.Core.Services;

namespace GonkNote.Core.Platform;

/// <summary>
/// PDF-Seiten zu Bildern rastern — für den Import als Notizbuchseiten und
/// Whiteboard-Bilder.
/// <para>
/// Windows und Linux teilen sich die PDFium-Umsetzung (<see cref="PdfiumRasterizer"/>);
/// iOS bekommt in Phase 5 PDFKit, weil PDFium dort nicht mitgeliefert werden darf. Die
/// Rückgabe bleibt in beiden Fällen <see cref="PdfImporter.PdfPageImage"/> — kodierte
/// Bytes plus Pixelmaße, also genau das, was danach in den Blob-Speicher wandert.
/// </para>
/// </summary>
public interface IPdfRasterizer
{
    int PageCount(string path);

    /// <summary>
    /// Rendert seitenweise und gibt jede Seite sofort weiter — es liegt nie mehr als eine
    /// im Speicher. <paramref name="only"/> schränkt auf Seitennummern ein (null = alle).
    /// <para>CPU-intensiv — gehört auf einen Hintergrund-Faden.</para>
    /// </summary>
    IEnumerable<PdfImporter.PdfPageImage> StreamPages(
        string path, int targetLongSide,
        IReadOnlyCollection<int>? only = null,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default);
}

/// <summary>Die PDFium-Umsetzung — dieselbe unter Windows und Linux.</summary>
public sealed class PdfiumRasterizer : IPdfRasterizer
{
    public int PageCount(string path) => PdfImporter.PageCount(path);

    public IEnumerable<PdfImporter.PdfPageImage> StreamPages(
        string path, int targetLongSide,
        IReadOnlyCollection<int>? only = null,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default) =>
        PdfImporter.StreamPages(path, targetLongSide, only, progress, ct);
}
