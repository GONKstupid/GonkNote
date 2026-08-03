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

        // Kein Handler für Dokumentverweise: das hier *ist* die Anleitung. Ein Verweis von
        // ihr auf sich selbst würde nur dasselbe Fenster ein zweites Mal öffnen.
        Anleitung.Content = MarkdownView.Bauen(EmbeddedDocs.Guide());
    }

    private void Schliessen_Click(object? sender, RoutedEventArgs e) => Close();
}
