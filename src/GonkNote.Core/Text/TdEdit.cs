namespace GonkNote.Core.Text;

// ==================== Was für eine Änderung das war ====================

/// <summary>
/// Die Art einer Änderung — **nur für den Verlauf** (<see cref="TdUndo"/>), damit er
/// zusammenfassen kann, was der Nutzer als **einen** Handgriff erlebt hat.
///
/// <para>
/// <b>Sie wird abgeleitet und nicht mitgegeben</b> (<see cref="TdFragment"/>): Ein Inhalt aus
/// lauter Textstücken ist Tippen, ein leerer ist Löschen, alles andere — mehrere Absätze, ein
/// Zeilenumbruch, ein Bild — ist Struktur. Ein zusätzlicher Parameter an jedem Handgriff wäre
/// eine zweite Wahrheit über dieselbe Sache, und die erste, die jemand falsch setzt.
/// </para>
/// </summary>
public enum TdEditArt
{
    /// <summary>Umbrechen, teilen, verbinden, einfügen von allem, was kein Text ist — **fasst nie zusammen**.</summary>
    Struktur,

    /// <summary>Zeichen eingefügt.</summary>
    Tippen,

    /// <summary>Zeichen entfernt.</summary>
    Loeschen,
}

// ==================== Was an die Stelle einer Auswahl tritt ====================

/// <summary>
/// Der Inhalt, der bei einer Änderung an die Stelle der Auswahl tritt: **ein oder mehrere
/// Absätze voller Stücke**.
///
/// <para>
/// <b>Warum überhaupt mehrere Absätze.</b> Sonst bräuchte „Absatz teilen" einen eigenen
/// Handgriff neben „einfügen", und beide müssten getrennt umkehrbar sein. Mit einem Inhalt
/// aus zwei leeren Absätzen ist das Teilen dasselbe wie jedes andere Einfügen — und die
/// Gegenbewegung fällt dieselbe heraus. Dasselbe gilt für einen eingefügten Text mit
/// Absatzmarken darin.
/// </para>
/// <para>
/// <b>Die Formate der Absätze stehen hier nicht.</b> Sie kommen aus dem Dokument: der erste
/// neue Absatz behält das Format des Absatzes, in dem die Auswahl beginnt, der letzte das des
/// Absatzes, in dem sie endet — genau wie in Word, wo Verbinden das obere und Teilen das
/// eigene Format weiterträgt. Ein Fragment beschreibt **Inhalt**, keine Absatzgestalt.
/// </para>
/// </summary>
public sealed class TdFragment
{
    private TdFragment(IReadOnlyList<IReadOnlyList<TdInline>> absaetze) => Absaetze = absaetze;

    /// <summary>Die Absätze des Inhalts — **mindestens einer**, auch wenn er leer ist.</summary>
    public IReadOnlyList<IReadOnlyList<TdInline>> Absaetze { get; }

    /// <summary>Gar nichts — der Inhalt einer reinen Löschung.</summary>
    public static TdFragment Nichts { get; } = new([[]]);

    /// <summary>
    /// Eine Absatzmarke: zwei leere Absätze. Eingefügt heißt das **teilen** — was links der
    /// Stelle stand, bleibt im ersten, was rechts stand, wandert in den zweiten.
    /// </summary>
    public static TdFragment Absatzmarke { get; } = new([[], []]);

    /// <summary>Stücke innerhalb **eines** Absatzes.</summary>
    public static TdFragment Stuecke(params TdInline[] stuecke) => new([stuecke]);

    /// <summary>
    /// Text — **an Absatzmarken geteilt**.
    ///
    /// <para>
    /// <b>Ein <c>\n</c> darf nicht in einem Textstück landen.</b> Es käme von der
    /// Bildschirmtastatur, aus der Zwischenablage oder von einer Eingabemethode mit herein und
    /// stünde dann als Zeichen im Dokument: der Umbruch setzt es nicht, der Export schreibt es
    /// wörtlich, und im DOCX steht ein Steuerzeichen mitten im Absatz. Hier wird daraus, was
    /// der Nutzer gemeint hat.
    /// </para>
    /// </summary>
    public static TdFragment Text(string text, TdCharFormat? format = null)
    {
        string[] teile = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var absaetze = new List<IReadOnlyList<TdInline>>(teile.Length);
        foreach (string teil in teile)
        {
            absaetze.Add(teil.Length == 0
                ? []
                : [new TdRun(teil, format?.Kopie() ?? new TdCharFormat())]);
        }
        return new TdFragment(absaetze);
    }

    /// <summary>Bleibt alles in einem Absatz?</summary>
    internal bool EinAbsatz => Absaetze.Count == 1;

    /// <summary>Ist gar nichts einzufügen? Dann ist die Änderung eine reine Löschung.</summary>
    internal bool IstLeer => EinAbsatz && Absaetze[0].Count == 0;

    /// <summary>
    /// Wie viele Cursorschritte der **letzte** Absatz breit ist — dort steht die Schreibmarke,
    /// wenn die Änderung durch ist.
    /// </summary>
    internal int LetzteBreite
    {
        get
        {
            int summe = 0;
            foreach (var stueck in Absaetze[^1]) summe += TdCursor.Laenge(stueck);
            return summe;
        }
    }

    /// <inheritdoc cref="TdEditArt"/>
    internal TdEditArt Art
    {
        get
        {
            if (!EinAbsatz) return TdEditArt.Struktur;
            if (Absaetze[0].Count == 0) return TdEditArt.Loeschen;

            foreach (var stueck in Absaetze[0])
                if (stueck is not TdRun) return TdEditArt.Struktur;

            return TdEditArt.Tippen;
        }
    }

    /// <summary>
    /// Endet dieser Inhalt auf einem Zwischenraum? Dann ist ein Wort fertig — und der Verlauf
    /// macht dort einen Schnitt (<see cref="TdUndo"/>).
    /// </summary>
    internal bool SchliesstGruppe =>
        EinAbsatz && Absaetze[0].Count > 0 &&
        Absaetze[0][^1] is TdRun run && run.Text.Length > 0 &&
        char.IsWhiteSpace(run.Text[^1]);
}

// ==================== Eine Änderung samt ihrer Gegenbewegung ====================

/// <summary>
/// Eine Änderung am Dokument — **gebaut, aber noch nicht ausgeführt**. Ausgeführt wird sie mit
/// <see cref="Anwenden"/>, zurückgenommen mit <see cref="Zuruecknehmen"/>, und beides beliebig
/// oft im Wechsel.
///
/// <para>
/// <b>Sie merkt sich nicht, *was* sich geändert hat, sondern die Blöcke davor und danach.</b>
/// Das ist die eine Entscheidung dieser Klasse. Ein Vermerk der Art „an Stelle X wurden 3
/// Zeichen eingefügt" muss beim Zurücknehmen dieselbe Rechnung noch einmal rückwärts machen —
/// mit denselben Sonderfällen (Stückgrenzen, Verweise, aufgeräumte leere Stücke), nur diesmal
/// spiegelverkehrt und ohne Wächter. Genau daran scheitern Rückgängig-Funktionen: nicht am
/// Speichern, sondern an der zweiten Rechnung. Hier gibt es keine zweite Rechnung, sondern
/// zwei Listen und einen Tausch.
/// </para>
/// <para>
/// <b>Deshalb gilt im ganzen Schreibweg: Absätze und Stücke werden nie verändert, sondern
/// ersetzt.</b> Wer ein Zeichen tippt, bekommt einen neuen Absatz mit einem neuen Stück; der
/// alte bleibt unversehrt und ist damit die Sicherung. Wer das später bricht — etwa indem er
/// beim Fettmachen ein <see cref="TdCharFormat"/> an Ort und Stelle umstellt —, macht jede
/// bereits gemerkte Änderung still falsch, und man sucht den Fehler im Rückgängig-Stapel.
/// </para>
/// <para>
/// <b>Sie hängt an ihrem Dokument</b> und lässt sich nicht auf ein anderes anwenden: Sie hält
/// die Blockliste, in der sie steht (den Abschnitt oder die Tabellenzelle). Passt der Inhalt
/// dort nicht mehr zu dem, was sie erwartet, **wirft sie**, statt zu raten — eine Änderung, die
/// an der falschen Stelle einsetzt, wäre Datenverlust, und der fiele erst Wochen später auf.
/// </para>
/// </summary>
public sealed class TdChange
{
    private readonly List<TdBlock> _container;
    private readonly int _index;
    private readonly IReadOnlyList<TdBlock> _alt;
    private readonly IReadOnlyList<TdBlock> _neu;

    internal TdChange(
        List<TdBlock> container, int index,
        IReadOnlyList<TdBlock> alt, IReadOnlyList<TdBlock> neu,
        TdSelection vorher, TdSelection nachher,
        TdEditArt art, bool schliesstGruppe)
    {
        _container = container;
        _index = index;
        _alt = alt;
        _neu = neu;
        Vorher = vorher;
        Nachher = nachher;
        Art = art;
        SchliesstGruppe = schliesstGruppe;
    }

    /// <inheritdoc cref="TdEditArt"/>
    public TdEditArt Art { get; }

    /// <summary>
    /// Ist mit dieser Änderung ein Wort fertig geworden? Dann setzt der Verlauf dahinter einen
    /// Schnitt (<see cref="TdUndo"/>).
    /// </summary>
    public bool SchliesstGruppe { get; }

    /// <summary>Die Auswahl, wie sie vor der Änderung stand — geradegezogen (§4.30).</summary>
    public TdSelection Vorher { get; }

    /// <summary>Wo die Schreibmarke steht, wenn die Änderung ausgeführt ist.</summary>
    public TdSelection Nachher { get; }

    /// <summary>
    /// Dieselbe Änderung andersherum. **Sie ist kein Nachbau, sondern dieselben zwei Listen mit
    /// vertauschten Rollen** — deshalb kann sie nicht von der Vorwärtsrichtung abweichen.
    /// </summary>
    public TdChange Gegenbewegung =>
        new(_container, _index, _neu, _alt, Nachher, Vorher, Art, SchliesstGruppe);

    /// <summary>
    /// Diese Änderung und die unmittelbar danach ausgeführte <paramref name="folgende"/> als
    /// **eine** — oder <c>null</c>, wenn die beiden nicht lückenlos aufeinanderfolgen.
    ///
    /// <para>
    /// <b>Das ist der zweite Ertrag der Blocksicherung.</b> Weil eine Änderung die Blöcke davor
    /// und danach hält, ist die Verschmelzung zweier aufeinanderfolgender kein Zusammenrechnen,
    /// sondern ein **Weglassen der Mitte**: das Davor der ersten, das Danach der zweiten. Ein
    /// Verlauf, der Handgriffe statt Zustände merkte, müsste hier zwei Einfügungen zu einer
    /// verrechnen — mit denselben Sonderfällen wie beim Einfügen selbst.
    /// </para>
    /// <para>
    /// <b>Geprüft wird auf Lückenlosigkeit, und zwar an den Objekten:</b> derselbe Container,
    /// dieselbe Stelle, und was die zweite Änderung ersetzt hat, ist **genau das**, was die
    /// erste hingelegt hat. Damit kann zwischen den beiden nichts anderes geschehen sein — eine
    /// Prüfung auf gleichen *Inhalt* könnte das nicht ausschließen.
    /// </para>
    /// </summary>
    public TdChange? Verschmelzen(TdChange folgende)
    {
        if (!ReferenceEquals(_container, folgende._container) || _index != folgende._index)
            return null;

        if (folgende._alt.Count != _neu.Count) return null;

        for (int i = 0; i < _neu.Count; i++)
            if (!ReferenceEquals(_neu[i], folgende._alt[i])) return null;

        return new TdChange(
            _container, _index, _alt, folgende._neu, Vorher, folgende.Nachher,
            folgende.Art, folgende.SchliesstGruppe);
    }

    /// <summary>Ausführen (und nach einem <see cref="Zuruecknehmen"/>: wiederherstellen).</summary>
    public TdSelection Anwenden()
    {
        Tauschen(_alt, _neu);
        return Nachher;
    }

    /// <summary>Zurücknehmen.</summary>
    public TdSelection Zuruecknehmen()
    {
        Tauschen(_neu, _alt);
        return Vorher;
    }

    private void Tauschen(IReadOnlyList<TdBlock> weg, IReadOnlyList<TdBlock> hin)
    {
        Pruefen(weg);
        _container.RemoveRange(_index, weg.Count);
        _container.InsertRange(_index, hin);
    }

    /// <summary>
    /// Steht dort noch das, was diese Änderung erwartet? Verglichen wird auf **dasselbe
    /// Objekt** und nicht auf gleichen Inhalt: Zwei Absätze mit demselben Text sind zwei
    /// Absätze, und an den falschen zu geraten hieße, den anderen zu verlieren.
    /// </summary>
    private void Pruefen(IReadOnlyList<TdBlock> erwartet)
    {
        bool passt = _index >= 0 && _index + erwartet.Count <= _container.Count;

        for (int i = 0; passt && i < erwartet.Count; i++)
            passt = ReferenceEquals(_container[_index + i], erwartet[i]);

        if (!passt)
            throw new InvalidOperationException(
                "Diese Änderung passt nicht mehr an ihre Stelle im Dokument. Zwischen ihr und " +
                "diesem Aufruf ist dort etwas anderes geschehen — sie jetzt auszuführen würde " +
                "fremde Absätze ersetzen.");
    }
}

// ==================== Die Handgriffe ====================

/// <summary>
/// Was sich ändert, wenn man tippt — Schritt 2 des Schreibens (HANDOFF §6).
///
/// <para>
/// <b>Alles läuft über einen einzigen Handgriff: <see cref="Ersetzen"/>.</b> Einfügen ist eine
/// Ersetzung einer leeren Auswahl, Löschen eine Ersetzung durch nichts, die Rücktaste am
/// Absatzanfang eine Ersetzung, deren Auswahl über die Absatzmarke reicht — und damit fällt
/// **Absätze verbinden von selbst heraus**, statt als eigener Fall noch einmal geschrieben zu
/// werden. Absatz teilen ist eine Ersetzung durch <see cref="TdFragment.Absatzmarke"/>. Es gibt
/// deshalb genau eine Stelle, an der Stücke zerschnitten und wieder zusammengesetzt werden, und
/// genau eine, die eine Gegenbewegung bauen muss.
/// </para>
/// <para>
/// <b>Dasselbe Muster wie in §4.17, §4.20, §4.21, §4.25 und §4.30</b>, hier zum sechsten Mal:
/// erst die eine Rechnung, dann das, was sie benutzt.
/// </para>
/// <para>
/// <b>Nichts wird ausgeführt.</b> Jeder Handgriff **baut** eine <see cref="TdChange"/> und gibt
/// sie zurück; wer sie ausführen will, ruft <see cref="TdChange.Anwenden"/>. So kommt Schritt 3
/// (Rückgängig) an dieselbe Änderung, die die Oberfläche ausführt, statt an eine
/// nachträgliche Beschreibung davon.
/// </para>
/// <para>
/// <b><c>null</c> heißt „keine Änderung".</b> Drei Gründe, und die Oberfläche behandelt sie
/// gleich (nichts tun):
/// <list type="bullet">
///   <item>Es gibt nichts zu tun — Rücktaste am Dokumentanfang, ein leerer Text.</item>
///   <item>Die Stelle liegt nicht im Dokument.</item>
///   <item>Die Auswahl reicht über eine <b>Tabellengrenze</b> — die benannte Lücke, siehe
///         <see cref="Ersetzen"/>.</item>
/// </list>
/// </para>
/// </summary>
public static class TdEdit
{
    // ---------------------------------------------------------------- Der eine Handgriff

    /// <summary>
    /// Ersetzt die Auswahl durch <paramref name="inhalt"/> — der Grundhandgriff, auf dem alle
    /// anderen stehen.
    ///
    /// <para>
    /// <b>Was dabei mit den Absatzformaten geschieht, ist die Word-Regel:</b> Bleibt alles in
    /// einem Absatz, behält er sein Format. Entstehen mehrere, trägt der erste das Format des
    /// Absatzes, in dem die Auswahl begann, und der letzte das des Absatzes, in dem sie endete;
    /// Absätze dazwischen bekommen das des ersten. So behält beim Verbinden der obere Absatz
    /// die Führung und beim Teilen erben beide Hälften — samt Listenzugehörigkeit.
    /// </para>
    /// <para>
    /// <b>Die benannte Lücke: über eine Tabellengrenze hinweg wird nicht bearbeitet.</b> Beide
    /// Enden der Auswahl müssen in derselben Blockliste liegen (demselben Abschnitt oder
    /// derselben Zelle), und dazwischen dürfen nur Absätze und Seitenumbrüche stehen. Eine
    /// Auswahl, die halb in einer Tabelle steht, müsste beim Löschen entscheiden, was aus der
    /// Tabelle wird — Word baut dafür eine eigene Mechanik, und ein geratenes Ergebnis wäre
    /// hier stiller Datenverlust. **Abgelehnt heißt <c>null</c>**, und der Nutzer sieht, dass
    /// nichts geschieht; das ist die Sorte Lücke, die man später schließt, ohne etwas
    /// zurückzudrehen.
    /// </para>
    /// </summary>
    public static TdChange? Ersetzen(TdDocument doc, TdSelection auswahl, TdFragment inhalt)
    {
        // Erst geradeziehen, dann sortieren: Anfang und Ende einer Auswahl vergleichen sich
        // nur zwischen kanonischen Stellen verlässlich (§4.30).
        var gezogen = TdCursor.Normalisieren(doc, auswahl);
        var start = gezogen.Start;
        var ende = gezogen.End;

        if (start == ende && inhalt.IstLeer) return null;

        if (Bereich(doc, start, ende) is not { } bereich) return null;
        var (container, iA, iB, absatzA, absatzB) = bereich;

        int von = TdCursor.Linear(absatzA, start);
        int bis = TdCursor.Linear(absatzB, ende);

        var kopf = Teil(absatzA.Inlines, 0, von);
        var schwanz = Teil(absatzB.Inlines, bis, TdCursor.Laenge(absatzB));

        var neu = new List<TdBlock>(inhalt.Absaetze.Count);
        if (inhalt.EinAbsatz)
        {
            neu.Add(Absatz(absatzA, Naht(
                kopf, inhalt.Absaetze[0], schwanz, ReferenceEquals(absatzA, absatzB))));
        }
        else
        {
            neu.Add(Absatz(absatzA, [.. kopf, .. inhalt.Absaetze[0]]));

            for (int i = 1; i < inhalt.Absaetze.Count - 1; i++)
                neu.Add(Absatz(absatzA, [.. inhalt.Absaetze[i]]));

            neu.Add(Absatz(absatzB, [.. inhalt.Absaetze[^1], .. schwanz]));
        }

        var alt = container.GetRange(iA, iB - iA + 1);

        // Die Schreibmarke steht hinter dem Eingefügten. Gerechnet wird am **neuen** Absatz:
        // das Aufräumen hat die Stücke inzwischen anders geschnitten, der Abstand vom
        // Absatzanfang ist davon aber unberührt — genau dafür gibt es ihn (§4.30).
        var letzter = (TdParagraph)neu[^1];
        int danachLinear = inhalt.EinAbsatz ? von + inhalt.LetzteBreite : inhalt.LetzteBreite;
        var danach = TdCursor.AusLinear(letzter, start.Paragraph + neu.Count - 1, danachLinear);

        return new TdChange(
            container, iA, alt, neu, gezogen, new TdSelection(danach),
            inhalt.Art, inhalt.SchliesstGruppe);
    }

    // ---------------------------------------------------------------- Tippen

    /// <summary>
    /// Text an der Auswahl einfügen; steht dort etwas ausgewähltes, tritt der Text an seine
    /// Stelle.
    ///
    /// <para>
    /// <b>Das eingefügte Zeichen erbt das Zeichenformat *links* davon</b> — die Erwartung aus
    /// Word, und der Grund, aus dem in §4.30 die linke Schreibweise einer Stückgrenze die
    /// kanonische geworden ist. Wer hinter ein fettes Wort tippt, schreibt fett weiter; wer
    /// davor tippt, nicht. Am Absatzanfang gibt es keinen linken Nachbarn — dort gilt das
    /// Format des ersten Stücks, und in einem leeren Absatz das des Absatzes selbst.
    /// </para>
    /// </summary>
    public static TdChange? Tippen(TdDocument doc, TdSelection auswahl, string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        var start = TdCursor.Normalisieren(doc, auswahl).Start;
        var absatz = TdCursor.AbsatzAn(doc, start.Paragraph);
        if (absatz is null) return null;

        return Ersetzen(doc, auswahl, TdFragment.Text(text, FormatBei(absatz, start)));
    }

    /// <summary>
    /// Das Zeichenformat, das ein hier eingefügtes Zeichen erbt: das des Stücks **links** der
    /// Stelle.
    ///
    /// <para>
    /// Zurück kommt eine **Abweichung** und kein aufgelöstes Format (§4.14) — sonst trüge jedes
    /// getippte Zeichen eine vollständige Formatkopie mit sich, und eine spätere Änderung am
    /// Absatz ginge daran vorbei. Und eine **Kopie**, weil der Aufrufer sie behalten darf,
    /// ohne dem Nachbarstück ins Format zu greifen.
    /// </para>
    /// </summary>
    public static TdCharFormat FormatBei(TdParagraph absatz, TdPosition stelle)
    {
        int linear = TdCursor.Linear(absatz, stelle);

        int summe = 0;
        TdInline? erstes = null;

        foreach (var stueck in TdCursor.Stuecke(absatz))
        {
            int laenge = TdCursor.Laenge(stueck);
            if (laenge > 0) erstes ??= stueck;

            // Dieselbe Zugehörigkeit wie die kanonische Form: die Stelle gehört dem Stück, das
            // **vor** ihr endet. Genau daraus fällt „erbt links" heraus.
            if (linear > summe && linear <= summe + laenge) return stueck.Format.Kopie();

            summe += laenge;
        }

        // Absatzanfang: kein linker Nachbar. Ein leerer Absatz hat auch keinen rechten — dann
        // ist die leere Abweichung richtig, denn dann gilt das Zeichenformat des Absatzes.
        return erstes is null ? new TdCharFormat() : erstes.Format.Kopie();
    }

    // ---------------------------------------------------------------- Umbrechen und teilen

    /// <summary>
    /// Ein Zeilenumbruch **innerhalb** des Absatzes (Umschalt+Eingabe) — kein neuer Absatz, und
    /// deshalb bleiben Absatzabstände und Listenmarke unangetastet (<see cref="TdLineBreak"/>).
    /// </summary>
    public static TdChange? Zeilenumbruch(TdDocument doc, TdSelection auswahl) =>
        Ersetzen(doc, auswahl, TdFragment.Stuecke(new TdLineBreak()));

    /// <summary>
    /// Den Absatz an der Auswahl teilen (Eingabe). Beide Hälften behalten Absatzformat,
    /// Zeichenformat und Listenzugehörigkeit; die Schreibmarke steht am Anfang der zweiten.
    /// </summary>
    public static TdChange? AbsatzTeilen(TdDocument doc, TdSelection auswahl) =>
        Ersetzen(doc, auswahl, TdFragment.Absatzmarke);

    // ---------------------------------------------------------------- Löschen

    /// <summary>Die Auswahl löschen. Bei leerer Auswahl gibt es nichts zu tun.</summary>
    public static TdChange? Loeschen(TdDocument doc, TdSelection auswahl) =>
        Ersetzen(doc, auswahl, TdFragment.Nichts);

    /// <summary>
    /// Rücktaste: die Auswahl löschen, oder — wenn nichts ausgewählt ist — das Zeichen links
    /// der Schreibmarke.
    ///
    /// <para>
    /// <b>Am Absatzanfang verbindet sie die Absätze</b>, ohne dass hier ein Wort davon steht:
    /// Die Stelle links davon ist das Ende des vorigen Absatzes (§4.30), die Auswahl reicht
    /// damit über die Absatzmarke, und eine Ersetzung über zwei Absätze hinweg **ist** das
    /// Verbinden. Ein eigener Zweig dafür wäre ein zweiter Weg zum selben Ergebnis — und der
    /// zweite Weg ist immer der, den niemand prüft.
    /// </para>
    /// <para>
    /// Gelöscht wird ein **ganzes** Zeichen: ein Emoji, ein zusammengesetztes „ä", ein Feld
    /// oder ein Bild verschwinden in einem Schritt, weil <see cref="TdCursor.Links"/> so zählt.
    /// </para>
    /// </summary>
    public static TdChange? Rueckwaerts(TdDocument doc, TdSelection auswahl)
    {
        if (!auswahl.IsEmpty) return Loeschen(doc, auswahl);

        var stelle = TdCursor.Normalisieren(doc, auswahl.Focus);
        var links = TdCursor.Links(doc, stelle);

        return links == stelle ? null : Loeschen(doc, new TdSelection(links, stelle));
    }

    /// <summary>
    /// Entf: die Auswahl löschen, oder das Zeichen rechts der Schreibmarke.
    /// <inheritdoc cref="Rueckwaerts" path="/para[1]"/>
    /// </summary>
    public static TdChange? Vorwaerts(TdDocument doc, TdSelection auswahl)
    {
        if (!auswahl.IsEmpty) return Loeschen(doc, auswahl);

        var stelle = TdCursor.Normalisieren(doc, auswahl.Focus);
        var rechts = TdCursor.Rechts(doc, stelle);

        return rechts == stelle ? null : Loeschen(doc, new TdSelection(stelle, rechts));
    }

    // ---------------------------------------------------------------- Wo gearbeitet wird

    /// <summary>
    /// Die Blockliste, in der beide Enden der Auswahl stehen, samt der Blöcke von A bis B.
    /// <c>null</c>, wenn dort nicht bearbeitet werden kann — siehe die benannte Lücke in
    /// <see cref="Ersetzen"/>.
    /// </summary>
    internal static (List<TdBlock> Container, int Von, int Bis, TdParagraph A, TdParagraph B)?
        Bereich(TdDocument doc, TdPosition start, TdPosition ende)
    {
        var absatzA = TdCursor.AbsatzAn(doc, start.Paragraph);
        var absatzB = TdCursor.AbsatzAn(doc, ende.Paragraph);
        if (absatzA is null || absatzB is null) return null;

        var (containerA, iA) = Ort(doc, absatzA);
        var (containerB, iB) = Ort(doc, absatzB);

        if (containerA is null || !ReferenceEquals(containerA, containerB) || iA > iB) return null;

        // Dazwischen darf nur stehen, was eine Ersetzung mitnehmen darf. Ein Seitenumbruch
        // darf es: Wer am Anfang des Absatzes dahinter die Rücktaste drückt, meint ihn. Eine
        // Tabelle darf es nicht — siehe Ersetzen.
        for (int i = iA; i <= iB; i++)
            if (containerA[i] is not (TdParagraph or TdPageBreak)) return null;

        return (containerA, iA, iB, absatzA, absatzB);
    }

    /// <summary>In welcher Liste und an welcher Stelle dieser Absatz steht.</summary>
    private static (List<TdBlock>? Liste, int Index) Ort(TdDocument doc, TdParagraph absatz)
    {
        foreach (var abschnitt in doc.Sections)
        {
            var treffer = Ort(abschnitt.Blocks, absatz);
            if (treffer.Liste is not null) return treffer;
        }
        return (null, -1);
    }

    /// <summary>
    /// <inheritdoc cref="Ort(TdDocument, TdParagraph)"/>
    /// <para>
    /// Steigt in Tabellenzellen ab, weil <see cref="TdDocument.Paragraphs"/> das auch tut
    /// (§4.19) — ein Absatz in einer Zelle hat eine Nummer und muss deshalb auch einen Ort
    /// haben.
    /// </para>
    /// </summary>
    private static (List<TdBlock>? Liste, int Index) Ort(List<TdBlock> bloecke, TdParagraph absatz)
    {
        for (int i = 0; i < bloecke.Count; i++)
        {
            if (ReferenceEquals(bloecke[i], absatz)) return (bloecke, i);

            if (bloecke[i] is not TdTable tabelle) continue;

            foreach (var zeile in tabelle.Rows)
                foreach (var zelle in zeile.Cells)
                {
                    var treffer = Ort(zelle.Blocks, absatz);
                    if (treffer.Liste is not null) return treffer;
                }
        }
        return (null, -1);
    }

    // ---------------------------------------------------------------- Stücke schneiden

    /// <summary>
    /// Ein neuer Absatz mit den Formaten von <paramref name="vorlage"/> und aufgeräumten
    /// Stücken. <b>Der Vorlage-Absatz bleibt unangetastet</b> — er ist die Sicherung, die
    /// <see cref="TdChange"/> festhält.
    /// </summary>
    private static TdParagraph Absatz(TdParagraph vorlage, List<TdInline> stuecke)
    {
        var absatz = new TdParagraph(Aufraeumen(stuecke))
        {
            Format = vorlage.Format,
            CharFormat = vorlage.CharFormat,
            List = vorlage.List,
        };
        return absatz;
    }

    /// <summary>
    /// Die Stücke zwischen zwei Abständen vom Absatzanfang, in Cursorschritten gemessen.
    ///
    /// <para>
    /// <b>Gerechnet wird über die verschachtelte Liste und nicht über die flache Sicht</b>, und
    /// das ist der Punkt, an dem die Erbfolge aus §7 zum vierten Mal zuschlägt: Wer hier
    /// <see cref="TdCursor.Stuecke"/> nähme, bekäme den Linktext ohne seinen Verweis wieder
    /// zusammengesetzt — **jeder Schnitt in der Nähe eines Verweises verlöre still sein Ziel**.
    /// Beide Zählungen kommen trotzdem auf dieselbe Zahl, weil ein Verweis so viele Schritte
    /// breit ist wie seine Stücke zusammen (<see cref="TdCursor.Laenge(TdInline)"/>).
    /// </para>
    /// </summary>
    internal static List<TdInline> Teil(IReadOnlyList<TdInline> stuecke, int von, int bis)
    {
        var ziel = new List<TdInline>();

        int summe = 0;
        foreach (var stueck in stuecke)
        {
            int laenge = TdCursor.Laenge(stueck);
            int a = Math.Max(von, summe);
            int b = Math.Min(bis, summe + laenge);

            if (b > a)
            {
                ziel.Add(a == summe && b == summe + laenge
                    ? stueck
                    : Ausschnitt(stueck, a - summe, b - summe));
            }

            summe += laenge;
            if (summe >= bis) break;
        }

        return ziel;
    }

    /// <summary>
    /// Kopf, Inhalt und Schwanz zu einer Stückliste — und **einen dabei zerschnittenen Verweis
    /// wieder zusammen**.
    ///
    /// <para>
    /// <b>Ohne das zerfiele jeder Verweis, in dem jemand tippt.</b> Eine Änderung mitten in
    /// einem Verweis schneidet ihn in zwei Hälften; einfach aneinandergehängt stünden danach
    /// zwei Verweise da und der eingefügte Text zwischen ihnen — sichtbar wäre das erst im
    /// DOCX oder beim nächsten Anklicken. Deshalb gilt: **liegt die Naht in einem Verweis,
    /// liegt auch der neue Text darin.**
    /// </para>
    /// <para>
    /// <b>Am *Ende* eines Verweises wächst er nicht mit.</b> Dort ist die Naht kein Schnitt,
    /// der Verweis steht ganz im Kopf, und der neue Text kommt dahinter — ein Verweis wird
    /// länger, wenn man **in** ihm schreibt, und nicht, wenn man dahinter weiterschreibt. Word
    /// hängt dort an; das ist die Eigenheit, die Leute wieder herausnehmen.
    /// </para>
    /// </summary>
    private static List<TdInline> Naht(
        List<TdInline> kopf, IReadOnlyList<TdInline> inhalt, List<TdInline> schwanz,
        bool derselbeAbsatz)
    {
        // Nur innerhalb eines Absatzes kann ein Verweis zerschnitten worden sein — über eine
        // Absatzgrenze hinweg gibt es keinen, der zusammengehörte.
        if (derselbeAbsatz &&
            kopf.Count > 0 && schwanz.Count > 0 &&
            kopf[^1] is TdHyperlink links && schwanz[0] is TdHyperlink rechts &&
            links.Target == rechts.Target)
        {
            List<TdInline> innen = [.. links.Inlines, .. inhalt, .. rechts.Inlines];

            return
            [
                .. kopf[..^1],
                new TdHyperlink(links.Target) { Inlines = innen, Format = links.Format },
                .. schwanz[1..],
            ];
        }

        return [.. kopf, .. inhalt, .. schwanz];
    }

    /// <summary>
    /// Ein Teilstück. Nur ein Textstück und ein Verweis lassen sich teilen — ein Feld, ein Bild
    /// und ein Zeilenumbruch sind einen Schritt breit und kommen deshalb nur ganz oder gar
    /// nicht vor (§4.30).
    /// </summary>
    private static TdInline Ausschnitt(TdInline stueck, int von, int bis) => stueck switch
    {
        TdRun run => new TdRun(run.Text[von..bis], run.Format),

        TdHyperlink verweis => new TdHyperlink(verweis.Target)
        {
            Inlines = Teil(verweis.Inlines, von, bis),
            Format = verweis.Format,
        },

        _ => stueck,
    };

    /// <summary>
    /// Räumt die Stücke eines neu gebauten Absatzes auf: leere Textstücke fallen weg,
    /// benachbarte mit **gleichem** Format werden zusammengelegt, ein Verweis ohne Text
    /// verschwindet.
    ///
    /// <para>
    /// <b>Ohne das zerfiele der Absatz mit jedem Tastendruck weiter.</b> Jede Einfügung
    /// schneidet ein Stück auf und setzt drei daraus zusammen; nach hundert Zeichen stünde ein
    /// Satz in dreihundert Stücken. Das ist nicht nur unordentlich: Der Zeichner setzt den
    /// Wortzwischenraum an einer Stückgrenze heute falsch (§5 „Noch offen" 6), und im DOCX
    /// stünde je Zeichen ein eigener Lauf.
    /// </para>
    /// <para>
    /// <b>Es betrifft den ganzen berührten Absatz und nicht nur die Naht</b> — auch Stücke,
    /// die schon vorher nebeneinander standen, werden zusammengelegt. Das ist gedeckt: Die
    /// Rücknahme holt den **ganzen** Absatz zurück, wie er war.
    /// </para>
    /// </summary>
    internal static List<TdInline> Aufraeumen(IEnumerable<TdInline> stuecke)
    {
        var ziel = new List<TdInline>();

        foreach (var stueck in stuecke)
        {
            if (stueck is TdHyperlink verweis)
            {
                var innen = Aufraeumen(verweis.Inlines);

                // Ein Verweis ohne Text zeigt nichts an und kann den Cursor nicht tragen
                // (§4.30) — er bliebe als unsichtbarer Rest stehen.
                if (innen.Count == 0) continue;

                ziel.Add(new TdHyperlink(verweis.Target)
                {
                    Inlines = innen,
                    Format = verweis.Format,
                });
                continue;
            }

            if (stueck is TdRun run)
            {
                if (run.Text.Length == 0) continue;

                if (ziel.Count > 0 && ziel[^1] is TdRun vorher && vorher.Format.Gleicht(run.Format))
                {
                    ziel[^1] = new TdRun(vorher.Text + run.Text, vorher.Format);
                    continue;
                }
            }

            ziel.Add(stueck);
        }

        return ziel;
    }
}
