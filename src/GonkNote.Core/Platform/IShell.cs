namespace GonkNote.Core.Platform;

/// <summary>Der Griff nach draußen — alles, was das Betriebssystem für uns übernimmt.</summary>
public interface IShell
{
    /// <summary>
    /// Öffnet eine Datei mit dem hinterlegten Standardprogramm (Windows: Shell-Verb,
    /// Linux: <c>xdg-open</c>, iOS: die Vorschau). Schlägt still fehl, wenn es keines
    /// gibt — die Datei ist geschrieben, das ist die Hauptsache.
    /// </summary>
    void OpenExternal(string path);

    /// <summary>Beendet die Anwendung (Menü „Datei → Beenden").</summary>
    void Quit();
}
