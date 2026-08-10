using System.IO;
using System.Windows;
using System.Windows.Threading;
using GonkNote.Core.Platform;
using GonkNote.Platform;
using GonkNote.Services;
using GonkNote.Core.Services;

namespace GonkNote;

public partial class App : Application
{
    public static DatabaseService Db { get; private set; } = null!;

    /// <summary>
    /// Der Windows-Kopf: alles, was Core und ViewModels von der Plattform brauchen.
    /// Wird als Erstes im Start gesetzt, noch vor der Datenbank — die fragt bereits nach
    /// den Pfaden.
    /// </summary>
    public static IPlatformServices Platform { get; private set; } = null!;

    /// <summary>Protokoll unerwarteter Fehler, neben der Datenbank.</summary>
    private static string LogPath => AppPaths.LogFile;

    private static bool _errorShown;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherError;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log(args.ExceptionObject as Exception);

        Platform = new WpfPlatformServices();
        AppPaths.Current = Platform.Paths;
        Core.Rendering.WbFonts.Schema = Platform.Fonts.Scheme;       // vor dem ersten Zeichnen

        // "--db <pfad>" erlaubt eine alternative Datenbank (z. B. für UI-Tests)
        string? dbPath = null;
        for (int i = 0; i < e.Args.Length - 1; i++)
            if (e.Args[i] == "--db") dbPath = e.Args[i + 1];

        // Der Leser für Altdatenbanken wird mitgegeben, nicht global gesetzt: liegt neben
        // der SQLite-Datei noch eine LiteDB-Datei aus einem früheren Stand, überträgt
        // DatabaseService sie beim ersten Start einmalig. Die Altdatei wird dabei nur
        // gelesen und bleibt danach unverändert liegen (HANDOFF §4.8).
        try
        {
            Db = new DatabaseService(dbPath, new Legacy.LiteDbReader());
        }
        catch (Exception ex)
        {
            // Scheitert die Übertragung, ist der Bestand nicht verloren — er liegt
            // unversehrt in der Altdatei. Was fehlt, ist eine brauchbare neue Datenbank,
            // und mit der arbeitet die App. Deshalb: melden und beenden, statt mit einem
            // halben Zustand weiterzulaufen.
            //
            // Der Text erscheint zwangsläufig in der Standardsprache: welche Sprache der
            // Nutzer gewählt hat, steht in genau der Datenbank, die sich nicht öffnen lässt.
            Log(ex);
            MessageBox.Show(
                Loc.T("Db.OpenFailed", ex.Message, LogPath),
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        Loc.Apply(Loc.FromCode(Db.GetSetting("language")));

        var theme = Db.GetSetting("theme") == "dark" ? AppTheme.Dark : AppTheme.Light;
        Platform.Theme.Apply(theme);
        Platform.Theme.ThemeChanged += () =>
            Db.SetSetting("theme", Platform.Theme.Current == AppTheme.Dark ? "dark" : "light");

        MainWindow = new MainWindow();
        MainWindow.Show();

        CleanUpBlobsInBackground();
    }

    /// <summary>
    /// Räumt im Hintergrund Bilder weg, auf die kein Dokument mehr zeigt – Reste gelöschter
    /// Dokumente und abgebrochener Importe. Bewusst nach dem Fensterstart und ohne Eile:
    /// der Start soll davon nichts merken.
    /// <para>
    /// Aussortiert wird in einen Papierkorb mit 30-Tage-Frist, nicht gelöscht. Der Lauf
    /// entscheidet über Bilder, die es sonst nirgends mehr gibt; eine falsche Entscheidung
    /// muss umkehrbar bleiben. Abschaltbar über „Ansicht → Einstellungen" (Schlüssel
    /// <c>blob-cleanup</c>).
    /// </para>
    /// </summary>
    private static void CleanUpBlobsInBackground() => Task.Run(async () =>
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        try
        {
            if (Db.GetSetting("blob-cleanup") == "aus") return;
            Db.RemoveOrphanBlobs();
        }
        catch (Exception ex) { Log(ex); }
    });

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
