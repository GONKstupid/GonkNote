namespace GonkNote.Core.Platform;

/// <summary>
/// Ein Eintrag in der Formatauswahl eines Datei-Dialogs.
/// <paramref name="Extensions"/> stehen <b>mit</b> Punkt und in Kleinschreibung
/// (<c>".pdf"</c>) — so, wie <see cref="Path.GetExtension(string)"/> sie zurückgibt,
/// damit Vergleiche ohne Umformung auskommen.
/// </summary>
public sealed record FileFilter(string Label, params string[] Extensions)
{
    /// <summary>Die erste Endung — das Format, das dieser Eintrag in erster Linie meint.</summary>
    public string PrimaryExtension => Extensions[0];
}

/// <summary>
/// Datei öffnen und speichern. Bewusst nur Pfade, keine Streams: der Import liest
/// große Dateien seitenweise über den Pfad (siehe <c>PdfImporter</c>), ein Stream
/// würde diesen Weg verbauen.
/// </summary>
public interface IFileDialog
{
    /// <summary>
    /// Dateien zum Öffnen wählen. Leere Liste = abgebrochen (nie <c>null</c>: ein
    /// Abbruch ist kein Sonderfall, sondern schlicht „keine Datei").
    /// </summary>
    IReadOnlyList<string> Open(string title, IReadOnlyList<FileFilter> filters, bool multiple = false);

    /// <summary>
    /// Zielpfad zum Speichern wählen; <c>null</c> = abgebrochen.
    /// <paramref name="preferred"/> wählt einen Filter vor (Endung mit Punkt); ist die
    /// Endung nicht dabei, bleibt der erste Filter oben.
    /// </summary>
    string? Save(string title, string suggestedName, IReadOnlyList<FileFilter> filters, string? preferred = null);
}
