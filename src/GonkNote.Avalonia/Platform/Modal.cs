using Avalonia.Threading;

namespace GonkNote.Platform;

/// <summary>
/// Die Brücke zwischen einer synchronen Schnittstelle und einem asynchronen Toolkit.
/// <para>
/// <b>Das ist die größte Nahtstelle des Avalonia-Kopfs</b>, und sie ist es nicht aus
/// Nachlässigkeit: <c>Core/Platform/</c> ist durchgehend synchron, weil es gegen WPF
/// entstanden ist — <c>MessageBox.Show</c> blockiert, <c>OpenFileDialog.ShowDialog</c>
/// blockiert. Avalonia hat für beides nur <c>Task</c>-Fassungen.
/// </para>
/// <para>
/// Die Schnittstelle deshalb auf <c>async</c> umzustellen wäre ein Eingriff in Core, in die
/// ViewModels <b>und</b> in den WPF-Kopf — genau das, was Phase 3 nicht tun soll. Der Preis
/// ist stattdessen diese Datei: ein <b>verschachtelter Nachrichtenlauf</b>, wie ihn WPF für
/// modale Dialoge von sich aus fährt (<c>DispatcherFrame</c> heißt dort genauso).
/// </para>
/// <para>
/// <b>Nur vom Oberflächen-Faden aufrufen</b> und nur für etwas, auf das der Nutzer ohnehin
/// wartet — einen Dialog. Für Arbeit im Hintergrund ist das der falsche Weg: der Lauf
/// verarbeitet währenddessen weiter Eingaben, und wer ihn um eine lange Rechnung legt,
/// bekommt Wiedereintritt an einer Stelle, an der niemand damit rechnet.
/// </para>
/// </summary>
internal static class Modal
{
    /// <summary>Wartet auf <paramref name="aufgabe"/>, ohne den Oberflächen-Faden anzuhalten.</summary>
    public static T Warte<T>(Task<T> aufgabe)
    {
        if (!aufgabe.IsCompleted)
        {
            var rahmen = new DispatcherFrame();
            // Post statt ContinueWith auf dem aktuellen Kontext: der Abschluss kann auf
            // einem beliebigen Faden kommen, das Beenden des Laufs gehört aber auf den
            // Oberflächen-Faden.
            aufgabe.ContinueWith(_ => Dispatcher.UIThread.Post(() => rahmen.Continue = false),
                TaskScheduler.Default);
            Dispatcher.UIThread.PushFrame(rahmen);
        }
        // GetResult und nicht .Result: eine Ausnahme kommt so unverpackt heraus und nicht
        // in einer AggregateException, die der Aufrufer erst auspacken müsste.
        return aufgabe.GetAwaiter().GetResult();
    }

    /// <summary>Dasselbe für eine <see cref="Task"/> ohne Ergebnis.</summary>
    public static void Warte(Task aufgabe) => Warte(Mit(aufgabe));

    private static async Task<bool> Mit(Task aufgabe)
    {
        await aufgabe.ConfigureAwait(true);
        return true;
    }
}
