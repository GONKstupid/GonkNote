using Avalonia;

namespace GonkNote;

/// <summary>
/// Einstiegspunkt des Linux-Kopfs.
/// <para>
/// <b>Vor <see cref="BuildAvaloniaApp"/> nichts von Avalonia anfassen</b> — der Rahmen
/// steht zu dem Zeitpunkt noch nicht, und ein Zugriff darauf scheitert an einer Stelle, die
/// mit dem eigentlichen Fehler nichts zu tun hat. Alles, was beim Start passieren muss,
/// steht in <see cref="App.OnFrameworkInitializationCompleted"/>.
/// </para>
/// </summary>
internal static class Program
{
    /// <summary>
    /// Die Aufrufargumente. Avalonia reicht sie nicht bis zur <c>Application</c> durch, der
    /// Start braucht aber <c>--db</c> (HANDOFF §8) — also hier ablegen, bevor der Rahmen
    /// hochfährt.
    /// </summary>
    public static string[] Args { get; private set; } = [];

    [STAThread]
    public static void Main(string[] args)
    {
        Args = args;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>Auch vom Oberflächen-Entwurf benutzt — die Signatur nicht ändern.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
