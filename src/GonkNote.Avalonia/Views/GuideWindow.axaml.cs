using GonkNote.Core.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// „Hilfe → Erste Schritte": zeigt die mitgelieferte Schritt-für-Schritt-Anleitung als
/// formatierten Text. Quelle ist die eingebettete Markdown-Datei in der aktuellen Sprache.
/// </summary>
public partial class GuideWindow : Window
{
    public GuideWindow()
    {
        // InitializeComponent und nicht AvaloniaXamlLoader.Load — sonst bleiben die
        // x:Name-Felder null (HANDOFF §7).
        InitializeComponent();

        // ⛔ Hier stand bis Phase 5, Schritt ④: „Kein Handler für Dokumentverweise: das hier
        // *ist* die Anleitung. Ein Verweis von ihr auf sich selbst würde nur dasselbe Fenster
        // ein zweites Mal öffnen." **Der Satz war falsch, und zwar nachmessbar:** Die
        // Anleitung verweist DREIMAL auf das README und KEIN EINZIGES MAL auf sich selbst —
        // in beiden Sprachen. Ein Grund, der eine Prüfung erspart, wird selten nachgeprüft.
        Anleitung.Content = MarkdownView.Bauen(
            EmbeddedDocs.Guide(),
            new Dokumentverweise(EmbeddedDocs.IsReadmeLink, ZumLiesmich));
    }

    /// <summary>
    /// <b>Kommt die Anleitung aus dem Über-Fenster, wird sie geschlossen statt ein zweites zu
    /// öffnen</b> — sonst stapelten sich zwei Fenster, die einander aufrufen, und der Nutzer
    /// müsste sich aus einer Kette klicken, die er als Hin und Zurück gemeint hat.
    /// </summary>
    private void ZumLiesmich(string ziel)
    {
        if (Owner is AboutWindow) { Close(); return; }

        new AboutWindow().ShowDialog(this);
    }

    private void Schliessen_Click(object? sender, RoutedEventArgs e) => Close();
}
