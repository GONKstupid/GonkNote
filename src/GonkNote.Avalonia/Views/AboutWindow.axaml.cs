using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

        // Der Verweis auf die Anleitung im README soll dorthin führen, wo sie steht — sonst
        // wäre er eine Sackgasse. Genau dafür kennt `EmbeddedDocs` beide Dateinamen.
        Liesmich.Content = MarkdownView.Bauen(EmbeddedDocs.Readme(), ZuDenErstenSchritten);
    }

    private void ZuDenErstenSchritten(string ziel)
    {
        if (!EmbeddedDocs.IsGuideLink(ziel)) return;
        new GuideWindow().ShowDialog(this);
    }

    private void Schliessen_Click(object? sender, RoutedEventArgs e) => Close();
}
