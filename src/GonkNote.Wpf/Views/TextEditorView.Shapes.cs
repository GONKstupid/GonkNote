using System.Windows;
using System.Windows.Documents;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Diagramm-Werkzeug im Text-Editor. Formen wurden bewusst entfernt – ein voll
/// interaktives Zeichnen gehört ins Whiteboard (SkiaSharp-Canvas), nicht in den
/// Textfluss der RichTextBox (siehe HANDOFF).
///
/// <para>
/// <b>Seit §4.82 geht das Diagramm als <see cref="Core.Text.TdChart"/> in den Text und nicht
/// mehr als Bild.</b> Vorher fügte dieses Werkzeug eine Bitmap ein — <b>die Zahlen waren im
/// selben Augenblick verloren</b> (§4.21). Und weil der Behälter keinen <c>Tag</c> trug, war
/// das eingefügte Diagramm für <see cref="FlowZuTd"/> gar keines: beim ersten Speichern wurde
/// es endgültig zu einem Bild.
/// </para>
/// </summary>
public partial class TextEditorView
{
    // ==================== Diagramm ====================

    private void OpenChart_Click(object s, RoutedEventArgs e)
    {
        var dlg = new ChartDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true || dlg.Result is not { } diagramm) return;

        // **Denselben Behälter wie der Ladeweg** (§4.82): Wer ihn hier von Hand nachbaut,
        // baut ihn irgendwann anders — und zwar an der Stelle, die niemand nachsieht.
        var absatz = new Paragraph { TextAlignment = TextAlignment.Center };
        absatz.Inlines.Add(TdZuFlow.DiagrammBehaelter(diagramm));

        // Diagramm als eigener Absatz (zentriert), damit es nicht im Fließtext klemmt
        InsertBlockAtCaret(absatz);
        MarkDirty();
        Editor.Focus();
    }
}
