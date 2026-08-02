using System.IO;
using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>
/// Wo dieser Kopf seine Daten sucht.
/// <para>
/// <b>Unter Linux</b> ist <c>SpecialFolder.ApplicationData</c> das, was die
/// XDG-Vereinbarung vorsieht: <c>$XDG_CONFIG_HOME</c>, sonst <c>~/.config</c>. Es entsteht
/// also <c>~/.config/GonkNote</c> — der Ort, an dem ein Linux-Programm seine Daten
/// hinterlegen soll, ohne dass hier eine eigene Pfadlogik nötig wäre.
/// </para>
/// <para>
/// <b>Unter Windows</b> ergibt dieselbe Zeile <c>%APPDATA%\GonkNote</c> — also
/// <b>denselben Ordner wie der WPF-Kopf</b>. Das ist Absicht: es ist dieselbe App mit
/// denselben Daten, und nur so lassen sich beide Köpfe überhaupt vergleichen. Zum Prüfen
/// heißt das aber: <b>immer <c>--db</c> mit einer Kopie</b> (HANDOFF Dauerregel 4 und §8) —
/// ein Start ohne Argument greift auf den echten Bestand zu.
/// </para>
/// </summary>
public sealed class AvaloniaAppPaths : IAppPaths
{
    public string DataFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GonkNote");

    public string AppFolder { get; } = AppContext.BaseDirectory;
}
