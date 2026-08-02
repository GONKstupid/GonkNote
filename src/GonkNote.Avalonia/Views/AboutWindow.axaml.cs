using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GonkNote.Core.Platform;
using GonkNote.Services;

namespace GonkNote.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        // InitializeComponent und nicht AvaloniaXamlLoader.Load — sonst bleiben die
        // x:Name-Felder null (HANDOFF §7).
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionsZeile.Text = Loc.T("About.Version", version?.ToString(3) ?? "?");

        // Wo die Daten liegen, unterscheidet sich zwischen den Plattformen — unter Linux
        // ~/.config/GonkNote, unter Windows %APPDATA%\GonkNote. Deshalb den tatsächlichen
        // Pfad zeigen und nicht den aus About.Subtitle.
        Datenordner.Text = AppPaths.Current.DataFolder;
    }

    private void Schliessen_Click(object? sender, RoutedEventArgs e) => Close();
}
