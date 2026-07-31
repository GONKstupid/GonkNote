using System.IO;
using GonkNote.Core.Rendering;
using SkiaSharp;
using Tesseract;

namespace GonkNote.Services;

/// <summary>
/// Texterkennung (OCR) auf Rasterbildern per Tesseract. Angewandt auf die ohnehin
/// als Bitmaps vorliegenden importierten Bild-/PDF-Seiten (Whiteboard-Bilder bzw.
/// Notizbuch-Seitenhintergründe).
///
/// Auslieferung: Die Sprachdaten (tessdata: deu/eng, tessdata_fast) liegen als
/// lose Begleitdatei neben der Exe – genau wie die Basis-Sticker. Die nativen
/// Tesseract-DLLs kommen über das NuGet-Paket in den x64-/x86-Unterordner.
/// Für den Single-File-Publish wird der native Suchpfad explizit auf das
/// Exe-Verzeichnis gesetzt, weil dort die Assembly-Location leer ist und der
/// Loader den x64-Ordner sonst nicht findet.
/// </summary>
public static class OcrService
{
    private static readonly object _gate = new();
    private static bool _initDone;
    private static string? _tessdata;
    private static string _languages = "eng";

    /// <summary>Steht OCR bereit (native Lib erreichbar + tessdata gefunden)?</summary>
    public static bool IsAvailable
    {
        get { EnsureInit(); return _tessdata != null; }
    }

    private static void EnsureInit()
    {
        if (_initDone) return;
        lock (_gate)
        {
            if (_initDone) return;
            _initDone = true;
            try
            {
                // Native DLLs (x64\tesseract50.dll …) neben der Exe finden lassen –
                // im Single-File-Publish ist Assembly.Location leer, sonst schlägt
                // das native Laden fehl.
                TesseractEnviornment.CustomSearchPath = AppContext.BaseDirectory;

                string dir = Path.Combine(AppContext.BaseDirectory, "tessdata");
                bool deu = File.Exists(Path.Combine(dir, "deu.traineddata"));
                bool eng = File.Exists(Path.Combine(dir, "eng.traineddata"));
                if (deu || eng)
                {
                    _tessdata = dir;
                    _languages = (deu, eng) switch
                    {
                        (true, true) => "deu+eng",
                        (true, false) => "deu",
                        _ => "eng",
                    };
                }
            }
            catch
            {
                _tessdata = null;
            }
        }
    }

    /// <summary>
    /// Erkennt Text in einem Rasterbild (JPEG/PNG/BMP-Bytes). CPU-intensiv →
    /// gehört auf einen Hintergrund-Thread. Liefert den erkannten Text (getrimmt;
    /// leer, wenn nichts erkannt wurde). Wirft, wenn OCR nicht verfügbar ist.
    /// </summary>
    public static string Recognize(byte[] imageData, CancellationToken ct = default)
    {
        EnsureInit();
        if (_tessdata == null)
            throw new InvalidOperationException(
                "OCR-Sprachdaten (tessdata) wurden nicht gefunden.");

        ct.ThrowIfCancellationRequested();
        byte[] prepared = WbImagePrep.ForOcr(imageData);

        ct.ThrowIfCancellationRequested();
        using var engine = new TesseractEngine(_tessdata, _languages, EngineMode.Default);
        using var pix = Pix.LoadFromMemory(prepared);
        using var page = engine.Process(pix);
        return (page.GetText() ?? string.Empty).Trim();
    }

    // Die Bildaufbereitung liegt seit Phase 2 in GonkNote.Core.Rendering.WbImagePrep: sie ist
    // reines SkiaSharp und hatte nur deshalb keinen Test, weil sie hier privat im Kopf stand
    // (HANDOFF §4.4). Wächter sind jetzt die BildaufbereitungTests in GonkNote.Core.Tests.
}
