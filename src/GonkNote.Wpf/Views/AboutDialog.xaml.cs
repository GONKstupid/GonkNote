using System.Reflection;
using System.Windows;
using GonkNote.Core.Platform;
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

        // Über Loc, nicht fest verdrahtet: die Zeile stand bis 0.2.0 als
        // "Version x.y.z – Phase 3" im Code und erschien damit auch im englischen Dialog
        // deutsch. „Phase 3" war zusätzlich die Entwicklungsphase von V1 und wurde neben der
        // laufenden Portierung zweideutig. Der Dialog wird bei jedem Öffnen neu erzeugt, die
        // Sprache stimmt deshalb ohne Loc.LanguageChanged.
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = Loc.T("About.Version", version?.ToString(3) ?? "?");

        // Wo die Daten liegen, sagt seit HANDOFF §4.12 nicht mehr About.Subtitle — der Pfad
        // unterscheidet sich zwischen den Plattformen. Der Linux-Kopf zeigt ihn schon;
        // ohne diese Zeile fehlte die Angabe unter Windows ganz.
        DataFolderText.Text = AppPaths.Current.DataFolder;

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
