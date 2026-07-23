using System;
using Avalonia;

namespace GonkNote.Avalonia;

internal static class Program
{
    // Avalonia-Einstieg. Plattformerkennung wählt automatisch Win32/X11/macOS.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
