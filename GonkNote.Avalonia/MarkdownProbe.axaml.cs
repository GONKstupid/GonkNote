using Avalonia.Controls;

namespace GonkNote.Avalonia;

/// <summary>Isolierter Render-Test: nur ein MarkdownScrollViewer, formatiert Beispiel-Markdown.</summary>
public partial class MarkdownProbe : Window
{
    public MarkdownProbe()
    {
        InitializeComponent();
        Md.Markdown =
            "# Überschrift 1\n\n" +
            "Das ist **fett**, das ist *kursiv*, das ist `Code`.\n\n" +
            "## Überschrift 2\n\n" +
            "- Punkt eins\n- Punkt zwei\n- Punkt drei\n\n" +
            "1. Erstens\n2. Zweitens\n\n" +
            "> Ein Zitat-Block zur Ansicht.\n\n" +
            "| Format | Windows | Linux |\n" +
            "|--------|:-------:|:-----:|\n" +
            "| WPF | ja | nein |\n" +
            "| Avalonia | ja | ja |\n";
    }
}
