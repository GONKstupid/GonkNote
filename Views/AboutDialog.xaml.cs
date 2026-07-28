using System.Reflection;
using System.Windows;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Über-Dialog: App-Infos plus das eingebettete README — gesetzt als formatierter Text,
/// nicht als roher Markdown-Quelltext (<see cref="MarkdownFlow"/>).
/// </summary>
public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.ToString(3) ?? "?"} – Phase 3";

        ReadmeView.Document = MarkdownFlow.ToFlowDocument(EmbeddedDocs.Readme(), OpenDocument);
    }

    /// <summary>
    /// Verweise im README auf andere Dokumente. „Erste Schritte" öffnet den zugehörigen
    /// Dialog, statt ins Leere zu zeigen — die Datei liegt im Repo, nicht neben der Exe.
    /// </summary>
    private void OpenDocument(string target)
    {
        if (!EmbeddedDocs.IsGuideLink(target)) return;
        new GuideDialog { Owner = this }.ShowDialog();
    }
}
