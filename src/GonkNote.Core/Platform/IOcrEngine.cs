namespace GonkNote.Core.Platform;

/// <summary>
/// Texterkennung auf einem Rasterbild. Windows nutzt Tesseract, iOS bekommt Vision
/// (Roadmap Phase 5) — beide sagen dasselbe: „hier ist ein Bild, gib mir den Text".
/// </summary>
public interface IOcrEngine
{
    /// <summary>
    /// Steht die Erkennung bereit (Sprachdaten und native Bibliothek erreichbar)?
    /// Entscheidet über den ausgegrauten Menüeintrag — darf deshalb nicht werfen.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Erkennt Text in einem kodierten Bild (PNG/JPEG/BMP-Bytes). Liefert den getrimmten
    /// Text, leer wenn nichts erkannt wurde. Wirft, wenn <see cref="IsAvailable"/> falsch ist.
    /// <para>CPU-intensiv — gehört auf einen Hintergrund-Faden.</para>
    /// </summary>
    string Recognize(byte[] imageData, CancellationToken ct = default);
}

/// <summary>
/// Wenn eine Plattform (noch) keine Erkennung mitbringt. Meldet ehrlich „nicht verfügbar",
/// statt still einen leeren Text zu liefern — der wäre von „nichts erkannt" nicht zu
/// unterscheiden.
/// </summary>
public sealed class NoOcrEngine : IOcrEngine
{
    public bool IsAvailable => false;

    public string Recognize(byte[] imageData, CancellationToken ct = default) =>
        throw new InvalidOperationException("Auf dieser Plattform gibt es keine Texterkennung.");
}
