using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GonkNote.Services;
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
    /// Ob auch auf Grammatik geprüft wird (Phase 5.2). Eigener Schalter, weil es eine eigene
    /// Frage ist — siehe die Begründung am Knopf in <c>TextDocView.axaml</c>.
    /// </summary>
    private bool _grammatikAn = true;

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

        GrammatikSchalter.IsEnabled = moeglich;
        if (!moeglich) GrammatikSchalter.IsChecked = false;
        _grammatikAn = GrammatikSchalter.IsChecked == true;

        // **Der Server wird nicht angepingt und nicht abgewartet.** Läuft einer, kommen seine
        // Befunde beim ersten geprüften Absatz von selbst dazu und melden sich hier zurück;
        // läuft keiner, merkt sich `TdLanguageTool` das nach dem ersten Versuch und fragt nie
        // wieder. Ein Startvorgang, der auf ein Netzwerk wartet, ist ein Startvorgang, der
        // manchmal hängt.
        TdLanguageTool.Fertig += LanguageToolGemeldet;
    }

    /// <summary>
    /// LanguageTool ist mit Befunden zurück. <b>Auf einem fremden Faden</b> — deshalb über den
    /// Planer der Plattform zurück in die Oberfläche, und nur neu zeichnen: Der Umbruch ändert
    /// sich durch eine Welle nicht.
    /// </summary>
    private void LanguageToolGemeldet() =>
        // **`Dispatcher.UIThread` und nicht `IUiScheduler`**: Der kann nur wiederholen
        // (`Repeat`), und ihn um ein einmaliges `Post` zu erweitern hieße, beide Köpfe
        // anzufassen — für eine Zeile, die ohnehin nur der Linux-Kopf braucht.
        Dispatcher.UIThread.Post(() =>
        {
            if (_grammatikAn && Pruefsprache is not null) Skia.InvalidateVisual();
        });

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

    private void Grammatik_Click(object? sender, RoutedEventArgs e)
    {
        _grammatikAn = GrammatikSchalter.IsChecked == true;
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
    /// <summary>
    /// Der Satz über der Vorschlagsliste — <b>nur bei Grammatik</b>. Bei der Rechtschreibung
    /// ist das angestrichene Wort selbst der Hinweis; eine Zeile „Dieses Wort steht nicht im
    /// Wörterbuch" darüber sagte nichts, was die rote Welle nicht schon gesagt hat.
    /// </summary>
    private string? Befundhinweis()
    {
        if (Pruefsprache is not { } sprache || _modell is null) return null;

        var stelle = _auswahl.Focus;
        if (TdCursor.AbsatzAn(_modell, stelle.Paragraph) is not { } absatz) return null;

        int linear = TdCursor.Linear(absatz, stelle);
        var fund = TdPruefung.Fehler(absatz, sprache, _grammatikAn)
            .FirstOrDefault(f => linear >= f.Start && linear <= f.Ende);

        // `Loc.T` gibt unbekannte Schlüssel unverändert zurück — deshalb geht hier sowohl der
        // Schlüssel der eigenen Regeln durch als auch der fertige Satz von LanguageTool.
        return fund is { Art: TdBefundArt.Grammatik, Hinweis: { Length: > 0 } schluessel }
            ? Loc.T(schluessel)
            : null;
    }

    private IReadOnlyList<(string Wort, Action Ersetzen)> Verbesserungsvorschlaege()
    {
        if (Pruefsprache is not { } sprache || _modell is null) return [];

        var stelle = _auswahl.Focus;
        if (TdCursor.AbsatzAn(_modell, stelle.Paragraph) is not { } absatz) return [];

        int linear = TdCursor.Linear(absatz, stelle);

        // `<=` auf beiden Seiten: Wer hinter das letzte Zeichen eines Wortes klickt, meint
        // dieses Wort und nicht das nächste.
        var fehler = TdPruefung.Fehler(absatz, sprache, _grammatikAn);
        if (fehler.FirstOrDefault(f => linear >= f.Start && linear <= f.Ende) is not { Laenge: > 0 } fund)
            return [];

        string wort = TdCursor.AbsatzText(absatz).Substring(fund.Start, fund.Laenge);

        int absatzIndex = stelle.Paragraph;
        return
        [
            .. TdPruefung.Vorschlaege(fund, wort, sprache)
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
        _kontext = _kontext with { Rechtschreibsprache = Pruefsprache, Grammatik = _grammatikAn };
        Skia.InvalidateVisual();
    }
}
