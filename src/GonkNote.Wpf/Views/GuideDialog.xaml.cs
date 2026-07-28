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
        GuideView.Document = MarkdownFlow.ToFlowDocument(EmbeddedDocs.Guide());
    }
}
