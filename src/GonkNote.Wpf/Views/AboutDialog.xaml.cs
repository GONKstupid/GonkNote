using GonkNote.Core.Text;
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

        ReadmeView.Document = MarkdownFlow.ToFlowDocument(
            EmbeddedDocs.Readme(), new Dokumentverweise(EmbeddedDocs.IsGuideLink, OpenDocument));
    }

    /// <summary>
    /// Verweise im README auf andere Dokumente. „Erste Schritte" öffnet den zugehörigen
    /// Dialog, statt ins Leere zu zeigen — die Datei liegt im Repo, nicht neben der Exe.
    ///
    /// <para>
    /// <b>Was hier nicht steht, wird seit Phase 5, Schritt ④ auch nicht mehr wie ein Verweis
    /// gezeichnet</b> (<see cref="Dokumentverweise"/>): Das README zeigt zweimal auf
    /// <c>THIRD-PARTY-NOTICES.md</c>, und <b>diese Datei ist in keinem Kopf eingebettet</b> —
    /// die zwei Verweise konnten nie funktionieren und sahen trotzdem aus wie welche.
    /// </para>
    /// </summary>
    private void OpenDocument(string target) => new GuideDialog { Owner = this }.ShowDialog();
}
