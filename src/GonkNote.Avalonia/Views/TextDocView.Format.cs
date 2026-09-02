using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using GonkNote.Core.Text;

namespace GonkNote.Views;

/// <summary>
/// Die Formatknöpfe des Ribbons — <b>Schritt 6 des Schreibens</b> (HANDOFF §6).
///
/// <para>
/// <b>Sie rechnet nichts</b>, wie <c>TextDocView.Eingabe.cs</c>: Was eine Formatänderung am
/// Dokument tut, baut <see cref="TdFormatEdit"/>; was die Auswahl gerade zeigt, beantwortet
/// <see cref="TdFormatEdit.Gemeinsam"/>. Hier steht die Übersetzung von Klicks in diese Aufrufe
/// — und das eine, was nur hier zu tun ist: <b>die Knöpfe nachziehen</b>, sobald sich die
/// Auswahl bewegt.
/// </para>
/// <para>
/// <b>Umgeschaltet wird gegen das Modell und nicht gegen den Knopf.</b> Avalonias
/// <c>ToggleButton</c> hat seinen Zustand schon gewechselt, wenn <c>Click</c> ankommt, und bei
/// <c>IsThreeState</c> läuft er im Kreis unbestimmt → an → aus. Wer den Knopf fragte, machte
/// über einer gemischten Auswahl mal fett und mal mager, je nachdem, wo der Kreis gerade steht.
/// Gefragt wird deshalb <see cref="TdFormatEdit.Gemeinsam"/>, und danach setzt
/// <see cref="RibbonNachziehen"/> den Knopf ohnehin neu.
/// </para>
/// <para>
/// <b>Was noch fehlt und hier benannt steht, statt als halber Knopf dazustehen</b> (§4.28):
/// eine Auswahl der **Schriftart** — dafür bräuchte es den Bestand der verfügbaren Familien,
/// und <c>IFontProvider</c> liefert das Schema und nicht den Bestand (§4.26). Ebenso Text- und
/// Hervorhebungsfarbe (sie brauchen einen Farbwähler) und die Formatvorlagen. Und: **eine leere
/// Auswahl ändert am Zeichenformat nichts** — Word merkt sich dann ein Format für das nächste
/// getippte Zeichen; das ist ein Zustand dieser Ansicht und keiner des Dokuments, und er ist
/// nicht gebaut (siehe <see cref="TdFormatEdit"/>).
/// </para>
/// </summary>
public partial class TextDocView
{
    /// <summary>
    /// Die Schriftgrößen, die „größer" und „kleiner" durchlaufen — dieselbe Leiter wie im
    /// WPF-Editor. <b>Keine feste Schrittweite:</b> Von 8 auf 9 ist ein sichtbarer Sprung, von
    /// 48 auf 49 keiner.
    /// </summary>
    private static readonly double[] Groessen =
        [8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 28, 32, 36, 40, 48, 56, 64, 72];

    /// <summary>
    /// Wie weit ein Klick auf „Einzug vergrößern" rückt: 1,25 cm — Words Standardtabulator,
    /// und damit die Schrittweite, die jeder von dort kennt.
    /// </summary>
    private const double EinzugSchrittCm = 1.25;

    // ==================== Zeichenformat ====================

    // **Knopf und Tastenkürzel rufen dieselbe Methode**, und deshalb steht das Paar aus Lesen
    // und Setzen genau einmal da. Zwei Fassungen wären zwei, von denen später eine anders
    // umschaltet als die andere — die Falle aus §4.13.
    private void Fett() => Umschalten(f => f.Bold, (f, an) => f.Bold = an);
    private void Kursiv() => Umschalten(f => f.Italic, (f, an) => f.Italic = an);
    private void Unterstrichen() => Umschalten(f => f.Underline, (f, an) => f.Underline = an);
    private void Durchgestrichen() =>
        Umschalten(f => f.Strikethrough, (f, an) => f.Strikethrough = an);

    /// <summary>
    /// <b>Formatierung löschen</b> (§4.84) — genauer: die <b>Abweichung</b> des Stücks fallen
    /// lassen.
    ///
    /// <para>
    /// <b>Was danach zu sehen ist, kommt vom Absatz und von der Grundschrift</b> (§4.14): Ein
    /// Text in einer Überschrift bleibt groß. Genau das erwartet, wer den Knopf drückt —
    /// <i>zurück auf das, was hier ohnehin gälte</i>, und nicht „alles klein und schwarz".
    /// </para>
    /// <para>
    /// <b>Die neun Felder stehen in Core</b> (<see cref="TdCharFormat.Zuruecksetzen"/>) und
    /// nicht hier: Sonst zählte sie der zweite Kopf ein zweites Mal auf, und beim nächsten
    /// neuen Feld veraltete eine der beiden Listen.
    /// </para>
    /// </summary>
    private void FormatLoeschen_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        Aendern(TdFormatEdit.Zeichen(_modell!, _auswahl,
            (abweichung, _) => abweichung.Zuruecksetzen()));

        RibbonNachziehen();
    }

    // ==================== Der Formatpinsel (§4.87) ====================

    /// <summary>
    /// Das aufgenommene Format, solange der Pinsel geladen ist — <c>null</c>, wenn nicht.
    /// <b>Es ist das aufgelöste</b> (siehe <see cref="TdFormatEdit.Uebertragen"/>); was davon
    /// im Dokument landet, entscheidet Core und nicht dieser Kopf.
    /// </summary>
    private TdCharFormat? _pinselFormat;

    /// <summary>
    /// Die Auswahl, aus der aufgenommen wurde. <b>Erst eine andere darf empfangen</b> — sonst
    /// träfe der Pinsel beim ersten Loslassen sich selbst, und der Nutzer sähe einen Knopf, der
    /// beim Drücken sofort wieder aufspringt.
    /// </summary>
    private TdSelection? _pinselQuelle;

    /// <summary>
    /// <b>Aufnehmen</b> — und mit dem zweiten Klick wieder ablegen, ohne etwas zu übertragen.
    ///
    /// <para>
    /// <b>Aufgenommen wird über <see cref="TdFormatEdit.Gemeinsam"/>, also das *wirksame*
    /// Format</b>, und das ist die Entscheidung vom 2026-09-01 (§5e Frage 1, §4.87). Ein Wort,
    /// das nur wegen seiner Überschrift fett aussieht, trägt keine eigene Abweichung — nähme
    /// der Pinsel sie, käme nichts mit, und der Nutzer klickte ins Leere.
    /// </para>
    /// <para>
    /// <b>Was daraus im Dokument landet, entscheidet dieser Kopf ausdrücklich nicht.</b>
    /// <see cref="TdFormatEdit.Uebertragen"/> schreibt nur, was von der Unterlage des Ziels
    /// abweicht — sonst brennte jeder Pinselstrich neun Eigenschaften ein und höbe die Trennung
    /// auf, auf der §4.14 besteht. Diese Rechnung gehört nach Core, damit der WPF-Kopf sie
    /// bekommt, sobald er sein <c>FlowDocument</c> los ist (§4.1).
    /// </para>
    /// </summary>
    private void Pinsel_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar || SchalterPinsel.IsChecked != true) { PinselAblegen(); return; }

        _pinselFormat = TdFormatEdit.Gemeinsam(_modell!, _auswahl);
        _pinselQuelle = _auswahl;

        // Ohne den Fokus zurück auf der Fläche zielt die nächste Auswahl ins Leere — dieselbe
        // Stelle, an der §4.86 den ganzen Tastenweg gefunden hat.
        Skia.Focus();
    }

    /// <summary>
    /// <b>Auftragen</b>, sobald der Nutzer eine <i>andere</i>, nicht leere Auswahl gezogen hat.
    /// Gerufen aus <c>Zeiger_Losgelassen</c> und damit erst, wenn das Ziehen fertig ist.
    ///
    /// <para>
    /// <b>Eine leere Auswahl trägt nichts auf</b> — wie drüben. Word pinselte auf einen
    /// einfachen Klick das ganze Wort; das wäre ein zweiter Handgriff mit eigener Regel, und
    /// er stünde dann nur in einem der beiden Köpfe.
    /// </para>
    /// </summary>
    private void PinselAnwenden()
    {
        if (_pinselFormat is not { } format || !Schreibbar) return;

        var gezogen = TdCursor.Normalisieren(_modell!, _auswahl);
        if (gezogen.Start == gezogen.End) return;

        if (_pinselQuelle is { } quelle)
        {
            var alt = TdCursor.Normalisieren(_modell!, quelle);
            if (alt.Start == gezogen.Start && alt.End == gezogen.End) return;
        }

        Aendern(TdFormatEdit.Uebertragen(_modell!, _auswahl, format));

        PinselAblegen();
        RibbonNachziehen();
    }

    /// <summary>Der Pinsel ist verbraucht oder abgewählt — ein Handgriff, zwei Aufrufer.</summary>
    private void PinselAblegen()
    {
        _pinselFormat = null;
        _pinselQuelle = null;
        SchalterPinsel.IsChecked = false;
    }

    private void Fett_Click(object? s, RoutedEventArgs e) => Fett();
    private void Kursiv_Click(object? s, RoutedEventArgs e) => Kursiv();
    private void Unterstrichen_Click(object? s, RoutedEventArgs e) => Unterstrichen();
    private void Durchgestrichen_Click(object? s, RoutedEventArgs e) => Durchgestrichen();

    private void Hoch_Click(object? s, RoutedEventArgs e) =>
        Stellung(TdVerticalAlign.Superscript);

    private void Tief_Click(object? s, RoutedEventArgs e) =>
        Stellung(TdVerticalAlign.Subscript);

    private void Groesser_Click(object? s, RoutedEventArgs e) => Stufe(+1);
    private void Kleiner_Click(object? s, RoutedEventArgs e) => Stufe(-1);

    /// <summary>
    /// Ein Ja/Nein-Format umschalten.
    ///
    /// <para>
    /// <b>Uneinig zählt als „aus".</b> Über einer Auswahl aus fettem und magerem Text macht der
    /// erste Klick alles fett — die Erwartung aus jedem Textprogramm, und die einzige, bei der
    /// zwei Klicks zu einem einheitlichen Zustand führen statt zurück ins Gemischte.
    /// </para>
    /// <para>
    /// <b>Ausgeschaltet wird zu <c>false</c> und nicht zu <c>null</c></b>, und das ist der
    /// Unterschied zwischen „nicht fett" und „nichts dazu gesagt" (§4.14). In einer Überschrift
    /// steht das Fett am **Absatz** (<c>TdParagraph.CharFormat</c>); eine bloß gelöschte
    /// Abweichung erbte es sofort wieder, und der Klick auf „F" täte sichtbar nichts. Word
    /// schreibt an dieser Stelle aus demselben Grund ein ausdrückliches <c>w:val="0"</c>.
    /// </para>
    /// </summary>
    private void Umschalten(Func<TdCharFormat, bool?> lesen, Action<TdCharFormat, bool> setzen)
    {
        if (!Schreibbar) return;

        bool an = lesen(TdFormatEdit.Gemeinsam(_modell!, _auswahl)) == true;

        Aendern(TdFormatEdit.Zeichen(_modell!, _auswahl,
            (abweichung, _) => setzen(abweichung, !an)));

        RibbonNachziehen();
    }

    /// <summary>
    /// Hoch- und Tiefstellung. <b>Sie sind keine zwei Schalter, sondern einer mit drei
    /// Stellungen</b> — ein Zeichen kann nicht gleichzeitig hoch und tief stehen. Nochmals
    /// derselbe Knopf hebt sie auf.
    /// </summary>
    private void Stellung(TdVerticalAlign stellung)
    {
        if (!Schreibbar) return;

        bool schon = TdFormatEdit.Gemeinsam(_modell!, _auswahl).VerticalAlign == stellung;

        Aendern(TdFormatEdit.Zeichen(_modell!, _auswahl, (abweichung, _) =>
            abweichung.VerticalAlign = schon ? null : stellung));

        RibbonNachziehen();
    }

    /// <summary>
    /// Eine Stufe größer oder kleiner. <b>Gerechnet wird je Stück am aufgelösten Format</b>:
    /// Eine Auswahl über eine Überschrift und den Absatz darunter muss beide um eine Stufe
    /// bewegen und nicht beide auf dieselbe Zahl setzen.
    /// </summary>
    private void Stufe(int richtung)
    {
        if (!Schreibbar) return;

        Aendern(TdFormatEdit.Zeichen(_modell!, _auswahl, (abweichung, aufgeloest) =>
            abweichung.FontSize = Nachbar(aufgeloest.FontSize, richtung)));

        RibbonNachziehen();
    }

    /// <summary>
    /// Die nächste Stufe der Leiter. Eine Größe, die nicht auf der Leiter steht (aus einem
    /// importierten Dokument), springt auf die nächste darüber oder darunter — und liegt danach
    /// darauf.
    /// </summary>
    private static double? Nachbar(double? groesse, int richtung)
    {
        if (groesse is not { } jetzt) return null;

        if (richtung > 0)
        {
            int i = Array.FindIndex(Groessen, g => g > jetzt + 0.01);
            return i < 0 ? Groessen[^1] : Groessen[i];
        }

        int j = Array.FindLastIndex(Groessen, g => g < jetzt - 0.01);
        return j < 0 ? Groessen[0] : Groessen[j];
    }

    // ==================== Absatzformat ====================

    /// <summary>
    /// Ausrichtung setzen. Nochmals dieselbe hebt sie auf — dann gilt wieder, was das Dokument
    /// vorgibt, und nicht „linksbündig, weil das die Vorgabe ist".
    /// </summary>
    private void Ausrichten_Click(object? sender, RoutedEventArgs e)
    {
        if (!Schreibbar || sender is not ToggleButton { Tag: string name }) return;
        if (!Enum.TryParse<TdAlign>(name, out var gewuenscht)) return;

        bool schon = TdFormatEdit.GemeinsamerAbsatz(_modell!, _auswahl).Alignment == gewuenscht;

        Aendern(TdFormatEdit.Absatz(_modell!, _auswahl,
            f => f.Alignment = schon ? null : gewuenscht));

        RibbonNachziehen();
    }

    private void EinzugRein_Click(object? s, RoutedEventArgs e) => Einzug(+EinzugSchrittCm);
    private void EinzugRaus_Click(object? s, RoutedEventArgs e) => Einzug(-EinzugSchrittCm);

    /// <summary>
    /// Den linken Einzug verschieben — <b>oder die Listenebene, wenn die Auswahl in einer Liste
    /// steht</b> (§4.39).
    ///
    /// <para>
    /// <b>Das ist Words Verhalten, und es ist das richtige:</b> In einer Liste ist „einrücken"
    /// keine Zentimeterfrage, sondern eine Ebene — die Marke wechselt mit, und der Einzug kommt
    /// aus der Listendefinition (<see cref="TdListLevel.IndentCm"/>). Wer hier stattdessen
    /// Zentimeter addierte, bekäme einen Aufzählungspunkt, der weiter rechts steht und trotzdem
    /// dieselbe Marke trägt.
    /// </para>
    /// <para>
    /// <b>Sonst: nicht unter null</b> — ein negativer Einzug schöbe den Text in den Seitenrand,
    /// und das ist nie gemeint, wenn jemand auf „verkleinern" drückt.
    /// </para>
    /// </summary>
    private void Einzug(double schrittCm)
    {
        if (!Schreibbar) return;

        if (TdListEdit.Gemeinsam(_modell!, _auswahl, nummeriert: false)
            || TdListEdit.Gemeinsam(_modell!, _auswahl, nummeriert: true))
        {
            Aendern(TdListEdit.Ebene(_modell!, _auswahl, schrittCm > 0 ? +1 : -1));
            RibbonNachziehen();
            return;
        }

        Aendern(TdFormatEdit.Absatz(_modell!, _auswahl, f =>
        {
            double neu = Math.Max(0, (f.LeftIndentCm ?? 0) + schrittCm);
            f.LeftIndentCm = neu == 0 ? null : neu;
        }));

        RibbonNachziehen();
    }

    // ==================== Die Knöpfe nachziehen ====================

    /// <summary>
    /// Stellt die Formatknöpfe auf das, was die Auswahl gerade zeigt.
    ///
    /// <para>
    /// <b>Gerufen aus <c>MarkeNachziehen</c></b> — also nach jedem Klick, jeder Pfeiltaste und
    /// jedem Umbruch, und damit an genau einer Stelle. Zwei Aufruflisten für „die Auswahl hat
    /// sich bewegt" wären zwei, von denen eine irgendwann einen Punkt weniger hat (§4.35).
    /// </para>
    /// <para>
    /// <b><c>null</c> heißt hier unbestimmt und nicht „aus"</b> (siehe
    /// <see cref="TdFormatEdit.Gemeinsam"/>): Ein Knopf über einer gemischten Auswahl darf sich
    /// nicht für eine der beiden Antworten entscheiden.
    /// </para>
    /// </summary>
    private void RibbonNachziehen()
    {
        // Vor InitializeComponent gibt es die Knöpfe noch nicht — dieselbe Vorsorge wie in
        // Reiter_Gewechselt, und aus demselben Grund nötig und nicht bloß vorsichtig.
        if (SchalterFett is null) return;

        bool an = Schreibbar;
        foreach (var schalter in Formatschalter()) schalter.IsEnabled = an;

        if (!an)
        {
            foreach (var schalter in Formatschalter()) schalter.IsChecked = false;

            // **Auch hier, und deshalb stehen die Aufrufe zweimal da:** Ein Dokument, das gerade
            // gar nicht angezeigt wird, darf nicht die Tabellenwerkzeuge des vorigen
            // stehenlassen.
            ListenNachziehen();
            FarbenNachziehen(new TdCharFormat());
            ReiterNachziehen();
            SeiteNachziehen();
            ObjekteNachziehen();
            return;
        }

        var zeichen = TdFormatEdit.Gemeinsam(_modell!, _auswahl);
        var absatz = TdFormatEdit.GemeinsamerAbsatz(_modell!, _auswahl);

        SchalterFett.IsChecked = zeichen.Bold;
        SchalterKursiv.IsChecked = zeichen.Italic;
        SchalterUnterstrichen.IsChecked = zeichen.Underline;
        SchalterDurchgestrichen.IsChecked = zeichen.Strikethrough;

        // Die Stellung ist einer aus drei Zuständen; ein Schalter, der nicht dafür steht, ist
        // aus und nicht unbestimmt.
        SchalterHoch.IsChecked = zeichen.VerticalAlign == TdVerticalAlign.Superscript;
        SchalterTief.IsChecked = zeichen.VerticalAlign == TdVerticalAlign.Subscript;

        SchalterLinks.IsChecked = absatz.Alignment == TdAlign.Left;
        SchalterMitte.IsChecked = absatz.Alignment == TdAlign.Center;
        SchalterRechts.IsChecked = absatz.Alignment == TdAlign.Right;
        SchalterBlock.IsChecked = absatz.Alignment == TdAlign.Justify;

        ListenNachziehen();
        FarbenNachziehen(zeichen);
        ReiterNachziehen();
        SeiteNachziehen();
        ObjekteNachziehen();
    }

    private IEnumerable<ToggleButton> Formatschalter()
    {
        yield return SchalterFett;
        yield return SchalterKursiv;
        yield return SchalterUnterstrichen;
        yield return SchalterDurchgestrichen;
        yield return SchalterHoch;
        yield return SchalterTief;
        yield return SchalterLinks;
        yield return SchalterMitte;
        yield return SchalterRechts;
        yield return SchalterBlock;
    }
}
