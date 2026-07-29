using System;
using System.Linq;

namespace GonkNote.StylusProbe;

/// <summary>
/// Ermittelt, welches Fenster-Backend Avalonia tatsaechlich gewaehlt hat.
/// Genau das ist die Frage aus Schritt 4: laeuft es nativ auf Wayland oder
/// ueber XWayland - und verhaelt sich der Druck dabei unterschiedlich?
/// </summary>
internal static class Umgebung
{
    public static string SitzungsTyp =>
        Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "(nicht gesetzt)";

    public static string WaylandAnzeige =>
        Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") ?? "(nicht gesetzt)";

    public static string X11Anzeige =>
        Environment.GetEnvironmentVariable("DISPLAY") ?? "(nicht gesetzt)";

    /// <summary>
    /// Das geladene Plattform-Assembly verraet das Backend zuverlaessiger als
    /// die Umgebungsvariablen - unter Wayland kann Avalonia trotzdem X11
    /// (also XWayland) benutzen.
    /// </summary>
    public static string BackendName
    {
        get
        {
            var geladen = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .Where(n => n is not null)
                .ToHashSet()!;

            if (geladen.Contains("Avalonia.Wayland")) return "Wayland (nativ)";
            if (geladen.Contains("Avalonia.X11"))
                return WaylandAnzeige != "(nicht gesetzt)" ? "X11 / XWayland" : "X11 (nativ)";
            if (geladen.Contains("Avalonia.Native")) return "Avalonia.Native (macOS)";
            if (geladen.Contains("Avalonia.Win32")) return "Win32";
            return "unbekannt";
        }
    }
}
