using System.Diagnostics;
using System.Windows;
using GonkNote.Core.Platform;

namespace GonkNote.Platform;

public sealed class WpfShell : IShell
{
    public void OpenExternal(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { /* kein Standardprogramm hinterlegt */ }
    }

    /// <summary>
    /// Über das Hauptfenster schließen, nicht über <c>Application.Shutdown</c>: nur so
    /// laufen die Aufräumarbeiten am Fenster (Speichern, Fenstermaße merken) noch durch.
    /// </summary>
    public void Quit() => Application.Current?.MainWindow?.Close();
}
