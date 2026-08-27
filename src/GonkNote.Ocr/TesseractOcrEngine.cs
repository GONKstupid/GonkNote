using GonkNote.Core.Platform;
using GonkNote.Core.Rendering;
using Tesseract;

namespace GonkNote.Ocr;

/// <summary>
/// Texterkennung auf Rasterbildern per Tesseract — <b>für beide Köpfe dieselbe Datei</b>.
///
/// <para>
/// Angewandt wird sie auf die ohnehin als Bitmap vorliegenden Bilder: die ausgewählten
/// Bild-Elemente einer Fläche oder, wenn nichts ausgewählt ist, der importierte
/// Seitenhintergrund (eine eingefügte PDF-Seite).
/// </para>
///
/// <para>
/// <b>Bis Phase 4.5, Stück 6 stand das als <c>OcrService</c> im WPF-Kopf</b> und der
/// Linux-Kopf hatte gar keine Erkennung (<see cref="NoOcrEngine"/>). Beim Zusammenlegen ist
/// aufgefallen, wie wenig davon plattformabhängig war: <b>nichts</b> außer der Frage, wie die
/// native Bibliothek gefunden wird — und die steht gekapselt in <see cref="TesseractLinux"/>.
/// </para>
///
/// <para>
/// <b>Auslieferung.</b> Die Sprachdaten (<c>tessdata</c>: deu/eng, <c>tessdata_fast</c>)
/// liegen als lose Begleitdatei neben dem Programm — genau wie die Geodreieck-SVGs. Unter
/// Windows kommen die nativen DLLs über das NuGet-Paket in den <c>x64</c>-Unterordner; unter
/// Linux bringt das Paket <b>nichts</b> mit, dort legt <see cref="TesseractLinux"/> beim
/// ersten Zugriff Verweise auf die System-Bibliotheken an (HANDOFF §4.63, §5 Nr. 18).
/// </para>
/// </summary>
public sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly object _riegel = new();
    private bool _startVorbei;
    private string? _tessdata;
    private string _sprachen = "eng";

    /// <inheritdoc/>
    public bool IsAvailable
    {
        get { Starten(); return _tessdata != null; }
    }

    /// <summary>
    /// Einmalig: den nativen Suchpfad setzen und die Sprachdaten suchen.
    ///
    /// <para>
    /// <b>Nichts hier darf werfen.</b> <see cref="IsAvailable"/> entscheidet über einen
    /// Menüeintrag und wird beim Öffnen jeder Schnellaktionen-Leiste gerufen — eine Ausnahme
    /// von dort käme mitten in der Bedienung heraus. Was schiefgeht, endet in
    /// <c>_tessdata == null</c>, und das heißt genau „diese Plattform kann es nicht".
    /// </para>
    /// </summary>
    private void Starten()
    {
        if (_startVorbei) return;
        lock (_riegel)
        {
            if (_startVorbei) return;
            _startVorbei = true;
            try
            {
                // Wo der Lader nach den nativen Dateien sucht. Unter Windows ist das das
                // Programmverzeichnis, in dem das NuGet-Paket seinen x64-Ordner abgelegt hat;
                // unter Linux ein beschreibbarer Ordner mit den Verweisen (dort ist das
                // Programmverzeichnis schreibgeschützt, siehe IAppPaths.AppFolder).
                //
                // Der Tippfehler im Namen `TesseractEnviornment` steckt im Paket, nicht hier.
                TesseractEnviornment.CustomSearchPath =
                    OperatingSystem.IsLinux() ? TesseractLinux.Einrichten() : AppPaths.Current.AppFolder;

                // Die Sprachdaten kommen aus dem Repo und nicht vom System: Arch liefert im
                // `tesseract`-Paket nur `osd` und `afr`, `deu` und `eng` sind eigene Pakete.
                // Auf dem Laptop gemessen (§4.63) — der mitgelieferte Weg ist der richtige.
                string ordner = AppPaths.AppSubfolder("tessdata");
                bool deu = File.Exists(Path.Combine(ordner, "deu.traineddata"));
                bool eng = File.Exists(Path.Combine(ordner, "eng.traineddata"));
                if (deu || eng)
                {
                    _tessdata = ordner;
                    _sprachen = (deu, eng) switch
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

    /// <inheritdoc/>
    public string Recognize(byte[] imageData, CancellationToken ct = default)
    {
        Starten();
        if (_tessdata == null)
            throw new InvalidOperationException(
                "OCR-Sprachdaten (tessdata) wurden nicht gefunden.");

        ct.ThrowIfCancellationRequested();
        byte[] aufbereitet = WbImagePrep.ForOcr(imageData);

        ct.ThrowIfCancellationRequested();
        using var maschine = new TesseractEngine(_tessdata, _sprachen, EngineMode.Default);
        using var bild = Pix.LoadFromMemory(aufbereitet);
        using var seite = maschine.Process(bild);
        return (seite.GetText() ?? string.Empty).Trim();
    }

    // Die Bildaufbereitung liegt seit Phase 2 in GonkNote.Core.Rendering.WbImagePrep: sie ist
    // reines SkiaSharp und hatte nur deshalb keinen Test, weil sie im Kopf privat stand
    // (HANDOFF §4.4). Wächter sind die BildaufbereitungTests in GonkNote.Core.Tests.
}
