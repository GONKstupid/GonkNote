using System.IO;
using System.Windows;
using System.Windows.Threading;
using GonkNote.Services;
using GonkNote.Core.Services;

namespace GonkNote;

public partial class App : Application
{
    public static DatabaseService Db { get; private set; } = null!;

    /// <summary>Protokoll unerwarteter Fehler, neben der Datenbank.</summary>
    private static string LogPath =>
        Path.Combine(Path.GetDirectoryName(DatabaseService.DefaultPath)!, "fehler.log");

    private static bool _errorShown;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherError;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log(args.ExceptionObject as Exception);

        // "--db <pfad>" erlaubt eine alternative Datenbank (z. B. für UI-Tests)
        string? dbPath = null;
        for (int i = 0; i < e.Args.Length - 1; i++)
            if (e.Args[i] == "--db") dbPath = e.Args[i + 1];

        Db = new DatabaseService(dbPath);

        Loc.Apply(Loc.FromCode(Db.GetSetting("language")));

        var theme = Db.GetSetting("theme") == "dark" ? AppTheme.Dark : AppTheme.Light;
        ThemeService.Apply(theme);
        ThemeService.ThemeChanged += () =>
            Db.SetSetting("theme", ThemeService.Current == AppTheme.Dark ? "dark" : "light");

        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Db.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Ein unerwarteter Fehler soll nicht die ganze Sitzung samt ungespeicherter Arbeit
    /// wegwerfen. Deshalb wird er protokolliert und einmal gemeldet, statt die App zu beenden
    /// (der Absturz beim Öffnen von Bestands-Whiteboards hatte sie kommentarlos geschlossen).
    /// </summary>
    private void OnDispatcherError(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Log(e.Exception);

        if (_errorShown) return;   // Folgefehler nur noch protokollieren, nicht zumüllen
        _errorShown = true;
        MessageBox.Show(
            $"Es ist ein unerwarteter Fehler aufgetreten:\n\n{e.Exception.Message}\n\n" +
            $"Die App läuft weiter. Einzelheiten stehen in:\n{LogPath}",
            "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static void Log(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath,
                $"--- {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---{Environment.NewLine}" +
                $"{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Protokollieren darf selbst nie zum Problem werden
        }
    }
}
