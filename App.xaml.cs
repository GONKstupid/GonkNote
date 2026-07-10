using System.Windows;
using GonkNote.Services;

namespace GonkNote;

public partial class App : Application
{
    public static DatabaseService Db { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // "--db <pfad>" erlaubt eine alternative Datenbank (z. B. für UI-Tests)
        string? dbPath = null;
        for (int i = 0; i < e.Args.Length - 1; i++)
            if (e.Args[i] == "--db") dbPath = e.Args[i + 1];

        Db = new DatabaseService(dbPath);

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
}
