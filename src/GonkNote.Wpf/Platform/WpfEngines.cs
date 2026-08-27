using GonkNote.Core.Platform;
using GonkNote.Core.Theming;
using GonkNote.Services;

namespace GonkNote.Platform;

// **Hier stand `WpfOcrEngine`, und davor `Services\OcrService.cs` mit 93 Zeilen.** Beides ist
// mit Phase 4.5, Stück 6 weggefallen: der Rumpf war plattformfrei und liegt jetzt einmal
// statt zweimal in GonkNote.Ocr (`TesseractOcrEngine`), den auch der Linux-Kopf benutzt.
// `WpfPlatformServices` setzt ihn direkt ein — eine Hülle, die nur weiterreicht, wäre eine
// Datei ohne Aufgabe.

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
