namespace GonkNote.Core.Platform;

/// <summary>
/// Auskunft darüber, ob die Plattform für eine Sprache überhaupt ein Wörterbuch hat.
/// <para>
/// Heute prüft das nur — die Markierungen selbst zeichnet die WPF-<c>RichTextBox</c>.
/// Ab Phase 4 (eigene Dokument-Engine) muss die Schnittstelle auch die Fundstellen
/// liefern; die Sprachfrage bleibt dabei dieselbe und wird schon jetzt hier gestellt.
/// </para>
/// </summary>
public interface ISpellChecker
{
    /// <summary>Gibt es für <paramref name="bcp47"/> (z. B. „de-DE") ein Wörterbuch?</summary>
    bool IsSupported(string bcp47);
}

/// <summary>Für Plattformen ohne Prüfung: nicht blockieren, nicht warnen.</summary>
public sealed class AlwaysSupportedSpellChecker : ISpellChecker
{
    public bool IsSupported(string bcp47) => true;
}
