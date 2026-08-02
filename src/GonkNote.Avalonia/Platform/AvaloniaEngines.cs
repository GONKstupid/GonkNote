using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>Der Griff nach draußen.</summary>
public sealed class AvaloniaShell : IShell
{
    /// <summary>
    /// Öffnet eine Datei mit dem hinterlegten Standardprogramm. Unter Windows über das
    /// Shell-Verb (<c>UseShellExecute</c>), unter Linux über <c>xdg-open</c> — dort tut
    /// <c>UseShellExecute</c> nichts dergleichen, sondern versucht, die Datei
    /// <b>auszuführen</b>. Bei einer frisch exportierten PDF wäre das nicht nur nutzlos,
    /// sondern die falsche Art von Nutzlosigkeit.
    /// </summary>
    public void OpenExternal(string path)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start(new ProcessStartInfo("xdg-open", [path]) { UseShellExecute = false });
            else
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // Kein Standardprogramm hinterlegt, oder xdg-open fehlt. Die Datei ist
            // geschrieben — das ist die Hauptsache.
        }
    }

    /// <summary>
    /// Über das Hauptfenster schließen, nicht über <c>Shutdown</c>: nur so laufen die
    /// Aufräumarbeiten am Fenster (Speichern, Fenstermaße merken) noch durch. Dieselbe
    /// Überlegung wie in <c>WpfShell</c>.
    /// </summary>
    public void Quit() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow?.Close();
}

/// <summary>Wiederholung über einen <see cref="DispatcherTimer"/> — läuft auf dem Oberflächen-Faden.</summary>
public sealed class AvaloniaUiScheduler : IUiScheduler
{
    public IDisposable Repeat(TimeSpan interval, Action tick)
    {
        var uhr = new DispatcherTimer { Interval = interval };
        uhr.Tick += (_, _) => tick();
        uhr.Start();
        return new Abmeldung(uhr);
    }

    private sealed class Abmeldung(DispatcherTimer uhr) : IDisposable
    {
        public void Dispose() => uhr.Stop();
    }
}

/// <summary>
/// Die Oberflächenschrift.
/// <para>
/// Unter Windows gibt es „Segoe UI" wie beim WPF-Kopf. Unter Linux ist keine Schrift
/// garantiert — <c>Inter</c> liegt aber als Paket im Kopf und wird von Avalonia geladen
/// (<c>WithInterFont</c>), steht also immer zur Verfügung.
/// </para>
/// <para>
/// <b>Das gilt nur für Avalonia selbst.</b> Was der <c>WbRenderer</c> zeichnet, geht über
/// <c>SKTypeface</c> und damit über fontconfig; findet Skia den Namen nicht, nimmt es seine
/// eigene Rückfallschrift. Genau deshalb prüft kein Pixelhash gezeichneten Text
/// (HANDOFF §4.6).
/// </para>
/// </summary>
public sealed class AvaloniaFontProvider : IFontProvider
{
    public string UiFamily { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Segoe UI" : "Inter";
}
