using System.Runtime.ExceptionServices;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Führt einen Testkörper auf einem eigenen **STA**-Thread aus.
/// <para>
/// Nötig, weil alles hier an WPF hängt: <c>FlowDocument</c>, der Paginator und
/// <c>RenderTargetBitmap</c> verlangen einen Thread im Single-Threaded-Apartment. xunit
/// startet Tests auf Threadpool-Threads (MTA) — ohne diesen Umweg wirft schon das Anlegen
/// eines Dokuments.
/// </para>
/// Je Aufruf ein frischer Thread, und zwar mit Absicht: WPF bindet erzeugte Objekte an den
/// Thread, auf dem sie entstanden sind. Ein geteilter Thread würde Dokumente aus einem
/// früheren Test am Leben halten und Fehlschläge von der Testreihenfolge abhängig machen.
/// <para>
/// Eine Dispatcher-Nachrichtenschleife wird nicht gestartet: Layout und Rendern laufen in
/// den benutzten Pfaden synchron ab. Braucht ein späterer Test doch eine, gehört sie hierhin
/// und nicht in den Test.
/// </para>
/// </summary>
internal static class Sta
{
    public static void Run(Action test)
    {
        ExceptionDispatchInfo? fehler = null;

        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception ex) { fehler = ExceptionDispatchInfo.Capture(ex); }
        }, maxStackSize: 16 * 1024 * 1024);

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        // Weiterwerfen mit dem ursprünglichen Stapel — sonst zeigt jeder Fehlschlag nur
        // hierher und nicht auf die Zeile, die ihn ausgelöst hat.
        fehler?.Throw();
    }
}
