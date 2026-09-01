using System.Windows;
using System.Windows.Input;
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

    /// <summary>
    /// <b>Ein Doppelklick auf ein Diagramm öffnet es zum Ändern</b> (§4.83) — der Punkt, den
    /// §4.82 offen gelassen hat.
    ///
    /// <para>
    /// <b>Vor §4.82 wäre das gar nicht möglich gewesen:</b> Im Text lag eine <b>Bitmap</b>,
    /// und aus Pixeln holt man keine Zahlen zurück (§4.21). Jetzt reist das
    /// <see cref="Core.Text.TdChart"/> als Auflage am Behälter mit, und
    /// <c>TdChartEingabe</c> legt seine Werte wieder in die Felder.
    /// </para>
    /// <para>
    /// <b>Gefragt wird <c>e.OriginalSource</c> und nicht die Schreibmarke:</b> Ein
    /// Doppelklick auf ein Bild setzt keine sinnvolle Textstelle, und ein
    /// <c>GetPositionFromPoint</c> daneben zeigte auf den Absatz statt auf das Diagramm.
    /// </para>
    /// </summary>
    private void Editor_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not FrameworkElement quelle) return;
        if (quelle.Parent is not InlineUIContainer behaelter) return;
        if (behaelter.Tag is not Core.Text.TdChart altes) return;

        // Angeklickt war es in jedem Fall — auch beim Abbrechen soll der Doppelklick nicht
        // noch als Wortauswahl im Bild landen.
        e.Handled = true;

        var dlg = new ChartDialog(altes) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true || dlg.Result is not { } neues) return;

        // **Ersetzt und nicht verändert** (§4.32): Wer das vorhandene `TdChart` von innen
        // umschriebe, änderte die Sicherung im Rückgängig-Stapel mit.
        if (behaelter.Parent is not Paragraph absatz) return;
        absatz.Inlines.InsertAfter(behaelter, TdZuFlow.DiagrammBehaelter(neues));
        absatz.Inlines.Remove(behaelter);

        MarkDirty();
        Editor.Focus();
    }
}
