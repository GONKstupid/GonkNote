using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GonkNote.Views;

/// <summary>
/// Diagramm-Werkzeug im Text-Editor: aus Werten gerenderte Zeichnung, als Bild
/// eingefügt (druck-/exportfähig). Formen wurden bewusst entfernt – ein voll
/// interaktives Zeichnen gehört ins Whiteboard (SkiaSharp-Canvas), nicht in den
/// Textfluss der RichTextBox (siehe HANDOFF).
/// </summary>
public partial class TextEditorView
{
    // ==================== Diagramm ====================

    private void OpenChart_Click(object s, RoutedEventArgs e)
    {
        var dlg = new ChartDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true || dlg.ResultImage == null) return;

        var img = new Image
        {
            Source = dlg.ResultImage,
            Width = dlg.ResultImage.Width,
            Height = dlg.ResultImage.Height,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(2, 6, 2, 6),
        };
        // Diagramm als eigener Absatz (zentriert), damit es nicht im Fließtext klemmt
        var para = new Paragraph { TextAlignment = TextAlignment.Center };
        para.Inlines.Add(new InlineUIContainer(img));
        InsertBlockAtCaret(para);
        MarkDirty();
        Editor.Focus();
    }
}
