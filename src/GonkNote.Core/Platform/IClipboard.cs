namespace GonkNote.Core.Platform;

/// <summary>
/// Die Zwischenablage. Bilder gehen als kodierte Bytes (PNG) über die Grenze, nicht als
/// Bitmap-Objekt der jeweiligen Oberfläche — dieselbe Form, in der sie ohnehin in der
/// Datenbank landen.
/// </summary>
public interface IClipboard
{
    void SetText(string text);
    string? GetText();

    /// <summary>Liegt ein Bild bereit? Absichtlich getrennt von <see cref="GetImage"/>: die
    /// Abfrage entscheidet über ausgegraute Menüeinträge und darf nichts kodieren.</summary>
    bool HasImage { get; }

    /// <summary>Das Bild der Zwischenablage als PNG-Bytes; <c>null</c>, wenn keines da ist.</summary>
    byte[]? GetImage();

    /// <summary>Kopierte Dateien (Explorer/Dateimanager); leer, wenn keine da sind.</summary>
    IReadOnlyList<string> GetFiles();
}
