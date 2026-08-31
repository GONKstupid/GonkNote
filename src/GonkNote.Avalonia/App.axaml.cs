using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GonkNote.Core.Platform;
using GonkNote.Core.Services;
using GonkNote.Platform;
using GonkNote.Services;
using GonkNote.Views;

namespace GonkNote;

/// <summary>
/// Der Start des Linux-Kopfs. <b>Reihenfolge und Inhalt sind absichtlich dieselben wie in
/// <c>App.OnStartup</c> des WPF-Kopfs</b> — was hier abweicht, weicht ab, weil Avalonia es
/// anders macht, und nicht, weil es hier anders gemeint wäre.
/// </summary>
public partial class App : Application
{
    public static DatabaseService Db { get; private set; } = null!;

    /// <summary>Alles, was Core und ViewModels von der Plattform brauchen.</summary>
    public static IPlatformServices Platform { get; private set; } = null!;

    private static string LogPath => AppPaths.LogFile;

    private static bool _errorShown;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        // Ein unerwarteter Fehler soll nicht die ganze Sitzung samt ungespeicherter Arbeit
        // wegwerfen — dieselbe Entscheidung wie im WPF-Kopf.
        Dispatcher.UIThread.UnhandledException += OnDispatcherError;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log(args.ExceptionObject as Exception);

        Platform = new AvaloniaPlatformServices();
        AppPaths.Current = Platform.Paths;
        Core.Rendering.WbFonts.Schema = Platform.Fonts.Scheme;       // vor dem ersten Zeichnen

        // "--db <pfad>" erlaubt eine alternative Datenbank. Unter Windows zeigt dieser Kopf
        // auf **denselben** Datenordner wie der WPF-Kopf (AvaloniaAppPaths) — zum Prüfen
        // also immer mit einer Kopie starten (HANDOFF Dauerregel 4, Befehle in §8).
        string? dbPath = null;
        var args = Program.Args;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--db") dbPath = args[i + 1];

        // **Der Leser für Altdatenbanken darf nicht fehlen.** Er ist ein *optionaler*
        // Konstruktor-Parameter — ein Kopf, der ihn vergisst, baut anstandslos und wirft
        // erst beim Start, sobald eine LiteDB-Datei danebenliegt. Das ist Absicht: eine
        // leere Datenbank neben vollen Bestandsdaten ist für den Nutzer von Datenverlust
        // nicht zu unterscheiden (HANDOFF §7, Wächter
        // `Ohne_Leser_wird_nicht_stillschweigend_neu_angefangen`).
        try
        {
            Db = new DatabaseService(dbPath, new Legacy.LiteDbReader());
        }
        catch (Exception ex)
        {
            // Scheitert die Übertragung, ist der Bestand nicht verloren — er liegt
            // unversehrt in der Altdatei. Was fehlt, ist eine brauchbare neue Datenbank.
            // Also melden und beenden, statt mit einem halben Zustand weiterzulaufen.
            //
            // Der Text erscheint zwangsläufig in der Standardsprache: welche Sprache der
            // Nutzer gewählt hat, steht in genau der Datenbank, die sich nicht öffnen lässt.
            Log(ex);
            MessageWindow.Zeige(null, Loc.T("Db.OpenFailed", ex.Message, LogPath),
                DialogSeverity.Warning, frage: false);
            desktop.Shutdown(1);
            return;
        }

        Loc.Apply(Loc.FromCode(Db.GetSetting("language")));

        var theme = Db.GetSetting("theme") == "dark" ? AppTheme.Dark : AppTheme.Light;
        Platform.Theme.Apply(theme);
        Platform.Theme.ThemeChanged += () =>
            Db.SetSetting("theme", Platform.Theme.Current == AppTheme.Dark ? "dark" : "light");

        desktop.MainWindow = new MainWindow();
        desktop.ShutdownRequested += (_, _) => Db.Dispose();

        base.OnFrameworkInitializationCompleted();

        CleanUpBlobsInBackground();
    }

    /// <summary>
    /// Räumt im Hintergrund Bilder weg, auf die kein Dokument mehr zeigt – Reste gelöschter
    /// Dokumente und abgebrochener Importe. Bewusst nach dem Fensterstart und ohne Eile:
    /// der Start soll davon nichts merken.
    /// <para>
    /// Aussortiert wird in einen Papierkorb mit 30-Tage-Frist, nicht gelöscht. Abschaltbar
    /// über den Schlüssel <c>blob-cleanup</c>.
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

    private void OnDispatcherError(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Log(e.Exception);

        if (_errorShown) return;   // Folgefehler nur noch protokollieren, nicht zumüllen
        _errorShown = true;
        MessageWindow.Zeige(AvaloniaDialogService.Besitzer(),
            Loc.T("Msg.Unexpected", e.Exception.Message, LogPath),
            DialogSeverity.Warning, frage: false);
    }

    private static void Log(Exception? ex)
    {
        // Gerechnet und geschrieben wird in Core (Fehlerprotokoll), damit beide Koepfe
        // dieselbe Obergrenze haben. **Hier stand bis V2-87 eine eigene Fassung ohne jede
        // Grenze** -- und der andere Kopf dieselbe, Zeile fuer Zeile. Am 2026-08-12 hat das
        // an einem Nachmittag 272 MB in den Datenordner geschrieben (§4.66).
        Fehlerprotokoll.Schreiben(ex, LogPath);
    }
}
