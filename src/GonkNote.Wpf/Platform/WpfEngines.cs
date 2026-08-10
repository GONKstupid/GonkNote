using GonkNote.Core.Platform;
using GonkNote.Core.Theming;
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

/// <summary>
/// Das Schriftschema. <b>Dasselbe wie im Linux-Kopf</b> — seit §4.26 liefert die App ihre
/// Schriften mit, und damit gibt es keine Windows-Antwort und keine Linux-Antwort mehr,
/// sondern eine. Früher stand hier fest „Segoe UI".
/// </summary>
public sealed class WpfFontProvider : IFontProvider
{
    public FontScheme Scheme => Fonts.Standard;
}
