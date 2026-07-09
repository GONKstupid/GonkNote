using System.Windows;
using GonkNote.Services;

namespace GonkNote;

public partial class App : Application
{
    public static DatabaseService Db { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Db = new DatabaseService();

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
