namespace GonkNote.Core.Platform;

/// <summary>
/// Wiederkehrende Arbeit auf dem Oberflächen-Faden (heute: die 30-Sekunden-Autospeicherung).
/// <para>
/// Der Faden ist der Punkt, weshalb hier kein <c>System.Threading.Timer</c> steht: das
/// Speichern liest den Zustand der geöffneten Registerkarten, und der gehört der Oberfläche.
/// </para>
/// </summary>
public interface IUiScheduler
{
    /// <summary>
    /// Ruft <paramref name="tick"/> alle <paramref name="interval"/> auf dem
    /// Oberflächen-Faden auf. Freigeben beendet die Wiederholung.
    /// </summary>
    IDisposable Repeat(TimeSpan interval, Action tick);
}
