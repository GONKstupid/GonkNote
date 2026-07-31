using GonkNote.Core.Platform;
using GonkNote.Services;

namespace GonkNote.Platform;

/// <summary>Texterkennung über Tesseract (<see cref="OcrService"/>).</summary>
public sealed class WpfOcrEngine : IOcrEngine
{
    public bool IsAvailable => OcrService.IsAvailable;

    public string Recognize(byte[] imageData, CancellationToken ct = default) =>
        OcrService.Recognize(imageData, ct);
}

/// <summary>Sprachprüfung über die Windows-Rechtschreib-API (<see cref="SpellCheckSupport"/>).</summary>
public sealed class WpfSpellChecker : ISpellChecker
{
    public bool IsSupported(string bcp47) => SpellCheckSupport.IsSupported(bcp47);
}

/// <summary>Die Oberflächenschrift von Windows 11.</summary>
public sealed class WpfFontProvider : IFontProvider
{
    public string UiFamily => "Segoe UI";
}
