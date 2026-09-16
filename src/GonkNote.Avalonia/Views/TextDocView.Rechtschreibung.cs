using Avalonia.Controls;
using Avalonia.Interactivity;
using GonkNote.Core.Rendering;
using GonkNote.Core.Text;

namespace GonkNote.Views;

/// <summary>
/// Die Rechtschreibprüfung im Linux-Kopf — Phase 5.1, das eine benannte Loch aus M2
/// (HANDOFF §5 Nr. 22).
///
/// <para>
/// <b>Hier steht nur der Anschluss.</b> Geprüft wird in <see cref="TdRechtschreibung"/>,
/// gezeichnet von <see cref="TdRenderer"/> — dieselbe Aufteilung wie beim Umbruch (§4.16).
/// Was dieser Teil beiträgt, sind zwei Angaben: welche Sprache gilt und ob überhaupt geprüft
/// wird. Beide landen als <b>eine</b> Zeichenfolge im <see cref="TdRenderContext"/>;
/// <c>null</c> heißt „nicht prüfen".
/// </para>
/// <para>
/// <b>Die Sprache gehört der Ansicht und nicht dem Dokument</b> — noch nicht. Drüben trägt
/// sie das <c>FlowDocument</c> (<c>Editor.Document.Language</c>) und geht damit in die
/// gespeicherte Datei ein; hier steht sie beim Öffnen wieder auf Deutsch. Das ist eine
/// bewusste Auslassung dieses Schritts und kein Versehen: Die Sprache im Datenformat
/// (<c>TdDocument</c>) zu verankern ist eine Änderung am Format und gehört zusammen mit dem
/// DOCX-Weg gemacht, nicht nebenbei.
/// </para>
/// </summary>
public partial class TextDocView
{
    /// <summary>
    /// Die Sprache, gegen die geprüft wird. Der Anfangswert ist derselbe wie der vorgewählte
    /// Eintrag in der Statusleiste (<c>SprachWahl</c>, erster Eintrag).
    /// </summary>
    private string _pruefsprache = "de-DE";

    private bool _rechtschreibungAn = true;

    /// <summary>
    /// Was der Zeichner bekommt: die Sprache — oder <c>null</c>, wenn abgeschaltet ist oder
    /// es für sie kein Wörterbuch gibt.
    /// <para>
    /// <b>Das fehlende Wörterbuch wird hier abgefangen und nicht im Zeichner.</b> Er zeichnet
    /// dann zwar ohnehin nichts (<see cref="TdRechtschreibung.Fehler"/> gibt eine leere Liste
    /// zurück), aber der Schalter oben soll grau sein und nicht so aussehen, als täte er
    /// etwas — dieselbe Ehrlichkeit wie beim ausgeblendeten Texterkennungsknopf (§4.64).
    /// </para>
    /// </summary>
    private string? Pruefsprache =>
        _rechtschreibungAn && TdRechtschreibung.Verfuegbar(_pruefsprache) ? _pruefsprache : null;

    /// <summary>
    /// Stellt den Schalter auf das, was die Wörterbücher hergeben — <b>einmal beim Aufbau</b>.
    /// Der Bestand ändert sich zur Laufzeit nicht.
    /// </summary>
    private void RechtschreibungAufbauen()
    {
        if (RechtschreibSchalter is null) return;

        // Kein einziges Wörterbuch heißt: der Ausgabeordner ist unvollständig. Dann ist der
        // Schalter tot, und das darf man sehen.
        bool moeglich = TdRechtschreibung.Sprachen.Any(TdRechtschreibung.Verfuegbar);
        RechtschreibSchalter.IsEnabled = moeglich;
        SprachWahl.IsEnabled = moeglich;
        if (!moeglich) RechtschreibSchalter.IsChecked = false;

        _rechtschreibungAn = RechtschreibSchalter.IsChecked == true;
    }

    private void Sprache_Gewechselt(object? sender, SelectionChangedEventArgs e)
    {
        if (SprachWahl?.SelectedItem is not ComboBoxItem eintrag) return;
        if (eintrag.Tag is not string bcp47) return;

        _pruefsprache = bcp47;
        PruefungNachziehen();
    }

    private void Rechtschreibung_Click(object? sender, RoutedEventArgs e)
    {
        _rechtschreibungAn = RechtschreibSchalter.IsChecked == true;
        PruefungNachziehen();
    }

    // ==================== Die Korrektur ====================

    /// <summary>
    /// Die Vorschläge für das Wort unter der Marke — leer, wenn dort keines steht, das
    /// angestrichen ist.
    ///
    /// <para>
    /// <b>Gefragt wird an <c>_auswahl.Focus</c></b>, und das ist beim Rechtsklick genau die
    /// Stelle unter dem Zeiger: <c>Zeiger_Gedrueckt</c> setzt sie dorthin, bevor es das Menü
    /// ruft. Dieselbe Stelle also, die auch die rote Welle trägt.
    /// </para>
    /// <para>
    /// <b>Jeder Vorschlag holt seinen Absatz beim Anklicken neu.</b> Zwischen Aufklappen und
    /// Klicken kann sich das Dokument geändert haben — eine festgehaltene Absatz-Referenz
    /// zeigte dann auf einen Absatz, der nicht mehr im Dokument steht, und das Ersetzen liefe
    /// ins Leere, ohne zu klagen. Genau diese Falle steht in <c>TdSuche.AlleErsetzen</c>
    /// angeschrieben; hier ist sie dieselbe.
    /// </para>
    /// </summary>
    private IReadOnlyList<(string Wort, Action Ersetzen)> Verbesserungsvorschlaege()
    {
        if (Pruefsprache is not { } sprache || _modell is null) return [];

        var stelle = _auswahl.Focus;
        if (TdCursor.AbsatzAn(_modell, stelle.Paragraph) is not { } absatz) return [];

        int linear = TdCursor.Linear(absatz, stelle);

        // `<=` auf beiden Seiten: Wer hinter das letzte Zeichen eines Wortes klickt, meint
        // dieses Wort und nicht das nächste.
        var fehler = TdRechtschreibung.Fehler(absatz, sprache);
        if (fehler.FirstOrDefault(f => linear >= f.Start && linear <= f.Ende) is not { Laenge: > 0 } fund)
            return [];

        string wort = TdCursor.AbsatzText(absatz).Substring(fund.Start, fund.Laenge);

        int absatzIndex = stelle.Paragraph;
        return
        [
            .. TdRechtschreibung.Vorschlaege(wort, sprache)
                .Select(vorschlag => (vorschlag, (Action)(() => Ersetzen(absatzIndex, fund, wort, vorschlag)))),
        ];
    }

    /// <summary>
    /// Tauscht ein angestrichenes Wort gegen einen Vorschlag.
    /// <b>Nur, wenn dort noch dasselbe Wort steht</b> — sonst hätte ein Klick im
    /// aufgeklappten Menü nach einer zwischenzeitlichen Änderung irgendeine andere Stelle
    /// überschrieben.
    /// </summary>
    private void Ersetzen(int absatzIndex, TdFehlstelle fund, string wort, string vorschlag)
    {
        if (!Schreibbar || _modell is null) return;
        if (TdCursor.AbsatzAn(_modell, absatzIndex) is not { } absatz) return;

        string text = TdCursor.AbsatzText(absatz);
        if (fund.Ende > text.Length || text.Substring(fund.Start, fund.Laenge) != wort) return;

        var treffer = new TdSelection(
            TdCursor.AusLinear(absatz, absatzIndex, fund.Start),
            TdCursor.AusLinear(absatz, absatzIndex, fund.Ende));

        // ⚠ **Das Format kommt vom ENDE des Treffers.** `FormatBei` erbt nach links (§4.30);
        // am Trefferanfang wäre der linke Nachbar das Stück davor — ein fett geschriebenes
        // Wort käme mager zurück. An einem gefallenen Wächter in `TdSuche` gemessen.
        var format = TdEdit.FormatBei(absatz, treffer.End);

        Aendern(TdEdit.Ersetzen(_modell, treffer, TdFragment.Text(vorschlag, format)));
    }

    /// <summary>
    /// Trägt Sprache und Schalter in den Zeichenkontext und zeichnet neu.
    /// <b>Ohne neuen Umbruch</b> — eine Wellenlinie ändert keine Umbruchstelle, und ein
    /// Umbruch bei jedem Klick auf den Schalter wäre in einem dreißigseitigen Skript zu
    /// merken.
    /// </summary>
    private void PruefungNachziehen()
    {
        _kontext = _kontext with { Rechtschreibsprache = Pruefsprache };
        Skia.InvalidateVisual();
    }
}
