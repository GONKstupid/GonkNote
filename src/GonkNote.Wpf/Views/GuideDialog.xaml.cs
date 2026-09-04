using GonkNote.Core.Text;
using System.Windows;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// „Hilfe → Erste Schritte": zeigt die mitgelieferte Schritt-für-Schritt-Anleitung als
/// formatierten Text. Quelle ist die eingebettete Markdown-Datei in der aktuellen Sprache.
/// </summary>
public partial class GuideDialog : Window
{
    public GuideDialog()
    {
        InitializeComponent();
        GuideView.Document = MarkdownFlow.ToFlowDocument(
            EmbeddedDocs.Guide(), new Dokumentverweise(EmbeddedDocs.IsReadmeLink, OpenDocument));
    }

    /// <summary>
    /// Verweise in der Anleitung auf andere Dokumente — <b>die Gegenrichtung zu
    /// <see cref="AboutDialog"/>, und sie fehlte bis Phase 5, Schritt ④.</b>
    ///
    /// <para>
    /// <b>Gefunden am laufenden Programm, im Prüflauf von ④:</b> Der Absatz „lies die
    /// Feature-Übersicht im README" trug einen Verweis, der <b>aussah wie einer und nichts
    /// tat</b> — <c>ToFlowDocument</c> bekam hier keinen Behandler, und ein <c>.md</c>-Ziel
    /// ohne Behandler wird in <see cref="MarkdownFlow"/> ein eingefärbter Text ohne Klick.
    /// <b>Die Begründung stand dabei seit jeher drüben im Kommentar</b> („statt ins Leere zu
    /// zeigen — die Datei liegt im Repo, nicht neben der Exe"); sie galt nur nie für diese
    /// Richtung. <i>Ein Kommentar, der einen Grund nennt, sagt nicht, wo er sonst noch gilt.</i>
    /// </para>
    /// <para>
    /// <b>Kommt die Anleitung aus dem Über-Dialog, wird sie geschlossen statt einen zweiten
    /// zu öffnen.</b> Sonst stapelten sich zwei Fenster, die einander aufrufen — und der
    /// Nutzer müsste sich aus einer Kette klicken, die er als Hin und Zurück gemeint hat.
    /// </para>
    /// </summary>
    private void OpenDocument(string target)
    {
        if (Owner is AboutDialog) { Close(); return; }

        new AboutDialog { Owner = this }.ShowDialog();
    }
}
