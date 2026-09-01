using System.Globalization;

namespace GonkNote.Core.Text;

// ==================== Die Stelle ====================

/// <summary>
/// Eine Stelle im Dokument — **eine Lücke zwischen zwei Zeichen**, nicht ein Zeichen.
/// Das ist Phase 4, Schritt 1 des Schreibens (HANDOFF §6).
///
/// <para>
/// <b>Die Stelle steht im Modell und nicht im Umbruch.</b> Das ist die Entscheidung, die
/// alles Weitere prägt, und sie fällt so herum, weil der Umbruch ein **Ergebnis** ist: Er
/// ändert sich bei jeder Eingabe, bei jeder Zoomstufe und bei jedem Wechsel der
/// Seiteneinrichtung. Eine Stelle, die „Zeile 7, Zeichen 3" hieße, wäre nach dem nächsten
/// Tastendruck woanders, ohne dass sich der Text bewegt hätte. Umgerechnet wird erst zum
/// Zeichnen, über <see cref="TdLayoutResult"/> — das ist Schritt 4.
/// </para>
///
/// <para>
/// <b>Drei Zahlen: Absatz, Stück, Zeichen.</b>
/// <list type="bullet">
///   <item>
///     <see cref="Paragraph"/> zählt über <see cref="TdDocument.Paragraphs"/> — **denselben
///     Durchlauf**, den die Listennummerierung schon benutzt (§4.17). Er steigt in
///     Tabellenzellen ab, eine Zelle braucht also keine eigene Koordinate.
///   </item>
///   <item>
///     <see cref="Inline"/> zählt über die **flache** Sicht auf die Stücke
///     (<see cref="TdCursor.Stuecke"/>): Ein Verweis erscheint darin nicht selbst, sondern
///     seine Stücke — genau wie in <see cref="TdParagraph.FlacheStuecke"/> und genau wie im
///     gesetzten Ergebnis. Der Cursor sitzt *im Linktext*, nicht *im Verweis*.
///   </item>
///   <item>
///     <see cref="Offset"/> ist der Index in UTF-16-Einheiten innerhalb dieses Stücks —
///     dieselbe Zählung, die <c>string</c> benutzt, damit ein Einfügen ohne Umrechnung
///     auskommt. **Bewegt wird trotzdem in ganzen Zeichen** (<see cref="TdCursor.Rechts"/>),
///     sonst zerfiele ein Emoji in zwei halbe.
///   </item>
/// </list>
/// </para>
///
/// <para>
/// <b>Dieselbe Lücke hat zwei Schreibweisen, und nur eine davon gilt.</b> Das Ende von Stück 2
/// und der Anfang von Stück 3 sind derselbe Ort auf dem Papier. **Kanonisch ist die linke
/// Form** — das Ende des früheren Stücks —, und das ist keine Geschmacksfrage: In Schritt 2
/// erbt ein eingefügtes Zeichen das Format **links davon** (wie in Word). Wer die rechte Form
/// als kanonisch wählte, müsste diese Erbfolge an jeder Einfügestelle noch einmal von Hand
/// zurückdrehen. <see cref="TdCursor.Normalisieren"/> stellt die linke Form her; **vergleichen
/// darf man nur normalisierte Stellen** (siehe <see cref="CompareTo"/>).
/// </para>
///
/// <para>
/// <b>Absatzende und Absatzanfang sind dagegen *nicht* dieselbe Stelle</b> und werden nicht
/// zusammengezogen: Zwischen ihnen steht die Absatzmarke, und der Cursor steht sichtbar in
/// verschiedenen Zeilen.
/// </para>
/// </summary>
/// <param name="Paragraph">Der wievielte Absatz — Zählung wie <see cref="TdDocument.Paragraphs"/>.</param>
/// <param name="Inline">Das wievielte Stück des Absatzes — flache Sicht, siehe <see cref="TdCursor.Stuecke"/>.</param>
/// <param name="Offset">Der Index innerhalb des Stücks, in UTF-16-Einheiten.</param>
public readonly record struct TdPosition(int Paragraph, int Inline, int Offset)
    : IComparable<TdPosition>
{
    /// <summary>Der Anfang des ersten Absatzes — die Stelle, an der ein leeres Dokument steht.</summary>
    public static TdPosition Null => new(0, 0, 0);

    /// <summary>
    /// Reihenfolge im Dokument: erst der Absatz, dann das Stück, dann das Zeichen.
    ///
    /// <para>
    /// <b>Der Vergleich setzt normalisierte Stellen voraus.</b> „Stück 1, Zeichen 3" und
    /// „Stück 2, Zeichen 0" können dieselbe Lücke meinen; unverglichen kämen sie hier als
    /// verschieden heraus, und eine Auswahl hätte plötzlich eine Länge, die sie nicht hat.
    /// Wer aus einer Eingabe kommt, ruft vorher <see cref="TdCursor.Normalisieren"/> —
    /// <see cref="TdSelection"/> tut das für seine beiden Enden nicht selbst, weil es sein
    /// Dokument nicht kennt.
    /// </para>
    /// </summary>
    public int CompareTo(TdPosition other)
    {
        int p = Paragraph.CompareTo(other.Paragraph);
        if (p != 0) return p;

        int i = Inline.CompareTo(other.Inline);
        return i != 0 ? i : Offset.CompareTo(other.Offset);
    }

    public static bool operator <(TdPosition a, TdPosition b) => a.CompareTo(b) < 0;
    public static bool operator >(TdPosition a, TdPosition b) => a.CompareTo(b) > 0;
    public static bool operator <=(TdPosition a, TdPosition b) => a.CompareTo(b) <= 0;
    public static bool operator >=(TdPosition a, TdPosition b) => a.CompareTo(b) >= 0;

    public override string ToString() => $"A{Paragraph}/S{Inline}/Z{Offset}";
}

// ==================== Die Auswahl ====================

/// <summary>
/// Eine Auswahl: **zwei** Stellen. Ist sie leer, ist sie der Cursor — es gibt keinen zweiten
/// Zustand und keine zweite Klasse dafür.
///
/// <para>
/// <b>Anker und Spitze statt Anfang und Ende</b>, und das ist der eigentliche Inhalt dieses
/// Typs: Beim Ziehen mit der Maus und bei Umschalt+Pfeil bleibt der <see cref="Anchor"/>
/// stehen und die <see cref="Focus"/> wandert — auch **hinter** den Anker. Wer nur Anfang und
/// Ende speicherte, könnte eine Auswahl nicht mehr nach links verkleinern, ohne zu raten,
/// welches Ende der Nutzer gerade in der Hand hat. <see cref="Start"/> und <see cref="End"/>
/// gibt es trotzdem, sortiert — sie sind das, wonach jeder fragt, der den Text dazwischen
/// will.
/// </para>
/// <para>
/// <b>Die Schreibmarke steht auf der Spitze</b>, nicht am Anfang: Sie ist die Stelle, an der
/// der Nutzer zuletzt war.
/// </para>
/// </summary>
/// <param name="Anchor">Wo die Auswahl festgemacht wurde — sie bewegt sich beim Erweitern nicht.</param>
/// <param name="Focus">Wo der Cursor steht — dieses Ende wandert.</param>
public readonly record struct TdSelection(TdPosition Anchor, TdPosition Focus)
{
    /// <summary>Eine leere Auswahl an dieser Stelle — der gewöhnliche Cursor.</summary>
    public TdSelection(TdPosition bei) : this(bei, bei) { }

    /// <summary>Die frühere der beiden Stellen.</summary>
    public TdPosition Start => Anchor <= Focus ? Anchor : Focus;

    /// <summary>Die spätere der beiden Stellen.</summary>
    public TdPosition End => Anchor <= Focus ? Focus : Anchor;

    /// <summary>Nichts ausgewählt — die Auswahl ist ein Cursor.</summary>
    public bool IsEmpty => Anchor == Focus;

    /// <summary>Zeigt die Spitze hinter den Anker? Für die Anzeige der Ziehrichtung.</summary>
    public bool IsForward => Anchor <= Focus;

    /// <summary>Dieselbe Auswahl mit einer neuen Spitze — Umschalt+Pfeil, Ziehen mit der Maus.</summary>
    public TdSelection Bis(TdPosition spitze) => new(Anchor, spitze);

    // **Hier stand bis Phase 5 ein `Zusammenfallen()`** — „die Auswahl auf ihre Spitze
    // zusammenfallen lassen, ein Klick ohne Umschalt". Gerufen hat es nie jemand: Wer einen
    // Klick ohne Umschalt verarbeitet, kennt die neue Stelle schon und legt direkt eine
    // Auswahl daraus an, statt erst eine alte mitzuschleppen und dann einzuklappen.

    /// <summary>Das ganze Dokument, Anker am Anfang.</summary>
    public static TdSelection Alles(TdDocument doc) =>
        new(TdCursor.Anfang(doc), TdCursor.Ende(doc));

    public override string ToString() =>
        IsEmpty ? $"Cursor {Focus}" : $"Auswahl {Start}–{End}";
}

// ==================== Rechnen mit Stellen ====================

/// <summary>
/// Alles, was man mit einer <see cref="TdPosition"/> tun kann, ohne das Dokument zu ändern:
/// prüfen, geradeziehen, bewegen, den ausgewählten Text lesen.
///
/// <para>
/// <b>Das Ändern steht bewusst nicht hier</b> — es ist Schritt 2, und es braucht das
/// Gegenstück jeder Änderung für den <c>UndoStack</c> (Schritt 3). Was hier steht, ist die
/// Sprache, in der Schritt 2 dann redet.
/// </para>
///
/// <para>
/// <b>Alles rechnet über eine Zwischengröße: den Abstand vom Absatzanfang</b>
/// (<see cref="Linear"/>). Der Weg über die drei Zahlen direkt ist voller Sonderfälle — leere
/// Stücke, Stückgrenzen, unteilbare Felder —, und jeder davon ist eine Stelle, an der ein
/// Cursor hängenbleibt oder ein Zeichen überspringt. Über eine einzige Zahl gerechnet, gibt es
/// die Sonderfälle nur einmal: beim Umrechnen.
/// </para>
/// </summary>
public static class TdCursor
{
    // ---------------------------------------------------------------- Stücke und Längen

    /// <summary>
    /// Die Stücke eines Absatzes in der **flachen** Sicht — so, wie der Cursor sie sieht und
    /// wie sie gesetzt werden: der Verweis erscheint nicht, seine Stücke schon.
    ///
    /// <para>
    /// <b>Ein leerer Verweis verschwindet dabei ganz</b>, und in ihm kann der Cursor deshalb
    /// nicht stehen. Das ist hinnehmbar, weil ein Verweis ohne Text nichts anzeigt — er ist
    /// nichts, worauf man zielen könnte.
    /// </para>
    /// </summary>
    public static IReadOnlyList<TdInline> Stuecke(TdParagraph absatz)
    {
        var flach = new List<TdInline>(absatz.Inlines.Count);
        foreach (var (stueck, _) in absatz.FlacheStuecke()) flach.Add(stueck);
        return flach;
    }

    /// <summary>
    /// Wie viele **Cursorschritte** dieses Stück breit ist.
    ///
    /// <para>
    /// <b>Das ist absichtlich nicht die Länge von <see cref="TdInline.PlainText"/>.</b> Ein
    /// Feld und ein Bild liefern dort „" (§4.20, §4.21) — im Klartext stehen sie zu Recht
    /// nicht, denn im Dokument steht ein Feld und keine Zahl. Für den Cursor sind sie
    /// trotzdem **ein** Schritt breit: Man kann davor und dahinter stehen, und dazwischen
    /// nicht. Genau das meint „ein Feld ist unteilbar" (<see cref="TdLaidOutRun.Field"/>) —
    /// es fällt hier von selbst heraus, statt an jeder Bewegung noch einmal abgefragt zu
    /// werden.
    /// </para>
    /// </summary>
    public static int Laenge(TdInline stueck) => stueck switch
    {
        TdRun run => run.Text.Length,

        // Ein Verweis kommt in der flachen Sicht nicht vor; wer ihn hier hineinreicht, hat
        // Inlines statt Stuecke genommen. Die Summe ist die einzige Antwort, die dann nicht
        // zusätzlich lügt.
        TdHyperlink verweis => verweis.Inlines.Sum(Laenge),

        // Zeilenumbruch, Feld, Bild, Diagramm: unteilbar, ein Schritt.
        _ => 1,
    };

    /// <summary>Die Gesamtbreite eines Absatzes in Cursorschritten.</summary>
    public static int Laenge(TdParagraph absatz)
    {
        int summe = 0;
        foreach (var stueck in Stuecke(absatz)) summe += Laenge(stueck);
        return summe;
    }

    // ---------------------------------------------------------------- Absätze finden

    /// <summary>
    /// Die Absätze des Dokuments als Liste. <see cref="TdDocument.Paragraphs"/> ist ein
    /// Durchlauf; wer wie hier mehrfach an denselben Index muss, holt ihn einmal.
    /// </summary>
    public static IReadOnlyList<TdParagraph> Absaetze(TdDocument doc) =>
        doc.Paragraphs().ToList();

    /// <summary>Der Absatz an dieser Stelle — <c>null</c>, wenn der Index danebenliegt.</summary>
    public static TdParagraph? AbsatzAn(TdDocument doc, int index)
    {
        if (index < 0) return null;

        int i = 0;
        foreach (var absatz in doc.Paragraphs())
            if (i++ == index) return absatz;

        return null;
    }

    /// <summary>
    /// Der wievielte Absatz das ist — <c>-1</c>, wenn er nicht in diesem Dokument steht.
    /// Der Rückweg aus <see cref="TdLine.Source"/>, den Schritt 4 braucht.
    /// </summary>
    public static int IndexVon(TdDocument doc, TdParagraph absatz)
    {
        int i = 0;
        foreach (var kandidat in doc.Paragraphs())
        {
            if (ReferenceEquals(kandidat, absatz)) return i;
            i++;
        }
        return -1;
    }

    /// <summary>
    /// Das Stück, in dem eine Stelle steht — <c>null</c>, wenn dort keines ist (§4.83).
    ///
    /// <para>
    /// <b>Wozu:</b> Ein Doppelklick auf ein Diagramm soll es zum Ändern öffnen und nicht ein
    /// Wort auswählen. Dafür muss der Kopf fragen können, <i>worauf</i> geklickt wurde, und
    /// das ist eine Frage ans Modell und nicht an die Oberfläche — der zweite Kopf stellt sie
    /// sonst mit einer eigenen Schleife über <see cref="Stuecke"/>.
    /// </para>
    /// <para>
    /// <b>Gefragt wird die flache Sicht</b>, dieselbe, in der <see cref="TdPosition.Inline"/>
    /// zählt (§4.30): Ein Verweis erscheint darin nicht, seine Stücke schon. Wer hier
    /// <c>Inlines</c> nähme, bekäme bei jedem Dokument mit Verweis das falsche Stück.
    /// </para>
    /// </summary>
    public static TdInline? StueckAn(TdDocument doc, TdPosition stelle)
    {
        if (AbsatzAn(doc, stelle.Paragraph) is not { } absatz) return null;

        var stuecke = Stuecke(absatz);
        return stelle.Inline >= 0 && stelle.Inline < stuecke.Count ? stuecke[stelle.Inline] : null;
    }

    // ---------------------------------------------------------------- Umrechnen

    /// <summary>
    /// Der Abstand einer Stelle vom Absatzanfang, in Cursorschritten.
    /// **Unabhängig davon, ob die Stelle normalisiert ist** — genau das macht sie zur
    /// gemeinsamen Sprache beider Schreibweisen.
    /// </summary>
    public static int Linear(TdParagraph absatz, TdPosition stelle)
    {
        var stuecke = Stuecke(absatz);
        int summe = 0;

        for (int i = 0; i < stuecke.Count && i < stelle.Inline; i++)
            summe += Laenge(stuecke[i]);

        int inneres = stelle.Inline >= 0 && stelle.Inline < stuecke.Count
            ? Math.Clamp(stelle.Offset, 0, Laenge(stuecke[stelle.Inline]))
            : 0;

        return Math.Clamp(summe + inneres, 0, Laenge(absatz));
    }

    /// <summary>
    /// Der Rückweg: aus dem Abstand vom Absatzanfang die **kanonische** Stelle.
    ///
    /// <para>
    /// <b>Hier fällt die Entscheidung für die linke Form.</b> Ein Abstand, der genau auf eine
    /// Stückgrenze zeigt, wird dem **früheren** Stück zugeschlagen (Ende), nicht dem späteren
    /// (Anfang) — die Begründung steht bei <see cref="TdPosition"/>. Leere Stücke werden dabei
    /// übersprungen: Ein Stück ohne Zeichen ist kein Ort, an dem ein Cursor stehen kann, und
    /// es entsteht beim Bearbeiten laufend.
    /// </para>
    /// </summary>
    public static TdPosition AusLinear(TdParagraph absatz, int absatzIndex, int linear)
    {
        var stuecke = Stuecke(absatz);
        int ziel = Math.Clamp(linear, 0, Laenge(absatz));

        if (stuecke.Count == 0 || ziel == 0)
            return new TdPosition(absatzIndex, 0, 0);

        int summe = 0;
        for (int i = 0; i < stuecke.Count; i++)
        {
            int laenge = Laenge(stuecke[i]);
            if (laenge == 0) continue;

            if (summe + laenge >= ziel)
                return new TdPosition(absatzIndex, i, ziel - summe);

            summe += laenge;
        }

        // Unerreichbar, solange ziel geklemmt ist — aber ein Absatz aus lauter leeren Stücken
        // kommt hier heraus, und dann ist der Anfang die einzige richtige Antwort.
        return new TdPosition(absatzIndex, 0, 0);
    }

    // ---------------------------------------------------------------- Geradeziehen

    /// <summary>
    /// Zieht eine Stelle gerade: Indizes in den gültigen Bereich, Zeichen in das Stück, und
    /// die Schreibweise in die kanonische linke Form.
    ///
    /// <para>
    /// <b>Jede Stelle, die von außen kommt, läuft hier durch</b> — aus einem Mausklick, aus
    /// einer alten Auswahl nach einer Änderung, aus einem gespeicherten Zustand. Ohne das
    /// vergleicht man später zwei Schreibweisen derselben Lücke und bekommt eine Auswahl mit
    /// einer Länge, die sie nicht hat.
    /// </para>
    /// </summary>
    public static TdPosition Normalisieren(TdDocument doc, TdPosition stelle)
    {
        var absaetze = Absaetze(doc);
        if (absaetze.Count == 0) return TdPosition.Null;

        int p = Math.Clamp(stelle.Paragraph, 0, absaetze.Count - 1);
        var absatz = absaetze[p];

        return AusLinear(absatz, p, Linear(absatz, stelle with { Paragraph = p }));
    }

    /// <summary>Steht diese Stelle schon kanonisch und im gültigen Bereich?</summary>
    public static bool Gueltig(TdDocument doc, TdPosition stelle) =>
        Normalisieren(doc, stelle) == stelle;

    /// <summary>Beide Enden einer Auswahl geradeziehen.</summary>
    public static TdSelection Normalisieren(TdDocument doc, TdSelection auswahl) =>
        new(Normalisieren(doc, auswahl.Anchor), Normalisieren(doc, auswahl.Focus));

    // ---------------------------------------------------------------- Ränder

    /// <summary>Der Anfang des Dokuments.</summary>
    public static TdPosition Anfang(TdDocument doc) => TdPosition.Null;

    /// <summary>Das Ende des Dokuments — hinter dem letzten Zeichen des letzten Absatzes.</summary>
    public static TdPosition Ende(TdDocument doc)
    {
        var absaetze = Absaetze(doc);
        if (absaetze.Count == 0) return TdPosition.Null;

        int letzter = absaetze.Count - 1;
        return AusLinear(absaetze[letzter], letzter, Laenge(absaetze[letzter]));
    }

    /// <summary>Der Anfang dieses Absatzes — <c>Pos1</c>, solange es keinen Zeilenumbruch gibt.</summary>
    public static TdPosition AbsatzAnfang(TdDocument doc, int absatzIndex)
    {
        var absaetze = Absaetze(doc);
        if (absaetze.Count == 0) return TdPosition.Null;

        return new TdPosition(Math.Clamp(absatzIndex, 0, absaetze.Count - 1), 0, 0);
    }

    /// <summary>Das Ende dieses Absatzes.</summary>
    public static TdPosition AbsatzEnde(TdDocument doc, int absatzIndex)
    {
        var absaetze = Absaetze(doc);
        if (absaetze.Count == 0) return TdPosition.Null;

        int p = Math.Clamp(absatzIndex, 0, absaetze.Count - 1);
        return AusLinear(absaetze[p], p, Laenge(absaetze[p]));
    }

    // ---------------------------------------------------------------- Bewegen

    /// <summary>
    /// Ein Zeichen nach rechts. Am Absatzende geht es in den nächsten Absatz, am Dokumentende
    /// bleibt die Stelle stehen.
    ///
    /// <para>
    /// <b>„Ein Zeichen" heißt ein *sichtbares* Zeichen und keine UTF-16-Einheit.</b> Ein Emoji
    /// steht als Paar in der Zeichenkette, ein „ä" kann aus zwei Zeichen bestehen; wer um eins
    /// weiterrückt, setzt den Cursor mitten hinein, und die nächste Eingabe zerlegt das
    /// Zeichen. Gemessen wird deshalb mit <see cref="StringInfo"/> in ganzen Textelementen.
    /// </para>
    /// </summary>
    public static TdPosition Rechts(TdDocument doc, TdPosition stelle)
    {
        var absaetze = Absaetze(doc);
        if (absaetze.Count == 0) return TdPosition.Null;

        var jetzt = Normalisieren(doc, stelle);
        var absatz = absaetze[jetzt.Paragraph];
        int linear = Linear(absatz, jetzt);

        if (linear >= Laenge(absatz))
        {
            // Absatzende: in den nächsten Absatz, sonst stehen bleiben.
            return jetzt.Paragraph + 1 < absaetze.Count
                ? new TdPosition(jetzt.Paragraph + 1, 0, 0)
                : jetzt;
        }

        return AusLinear(absatz, jetzt.Paragraph, linear + SchrittRechts(absatz, linear));
    }

    /// <summary>
    /// Ein Zeichen nach links. Am Absatzanfang geht es ans Ende des vorigen Absatzes, am
    /// Dokumentanfang bleibt die Stelle stehen.
    /// <inheritdoc cref="Rechts" path="/para[1]"/>
    /// </summary>
    public static TdPosition Links(TdDocument doc, TdPosition stelle)
    {
        var absaetze = Absaetze(doc);
        if (absaetze.Count == 0) return TdPosition.Null;

        var jetzt = Normalisieren(doc, stelle);
        var absatz = absaetze[jetzt.Paragraph];
        int linear = Linear(absatz, jetzt);

        if (linear <= 0)
        {
            if (jetzt.Paragraph == 0) return jetzt;

            int vorher = jetzt.Paragraph - 1;
            return AusLinear(absaetze[vorher], vorher, Laenge(absaetze[vorher]));
        }

        return AusLinear(absatz, jetzt.Paragraph, linear - SchrittLinks(absatz, linear));
    }

    /// <summary>
    /// Wie viele UTF-16-Einheiten das Zeichen **rechts** von <paramref name="linear"/> breit
    /// ist. Für alles außer einem Textstück ist die Antwort 1 — ein Feld ist unteilbar.
    /// </summary>
    private static int SchrittRechts(TdParagraph absatz, int linear)
    {
        var (stueck, innen) = StueckRechtsVon(absatz, linear);

        return stueck is TdRun run && innen < run.Text.Length
            ? StringInfo.GetNextTextElementLength(run.Text.AsSpan(innen))
            : 1;
    }

    /// <summary>
    /// Wie viele UTF-16-Einheiten das Zeichen **links** von <paramref name="linear"/> breit
    /// ist. <see cref="StringInfo"/> kann nur vorwärts; die letzte Grenze vor
    /// <paramref name="linear"/> wird deshalb vorwärts gesucht — bei Absatzlängen kostet das
    /// nichts, und es ist die einzige Zählung, die mit <see cref="SchrittRechts"/>
    /// zusammenpasst.
    /// </summary>
    private static int SchrittLinks(TdParagraph absatz, int linear)
    {
        var (stueck, innen) = StueckLinksVon(absatz, linear);
        if (stueck is not TdRun run || innen <= 0) return 1;

        int grenze = 0;
        while (grenze < innen)
        {
            int breite = StringInfo.GetNextTextElementLength(run.Text.AsSpan(grenze));
            if (grenze + breite >= innen) return innen - grenze;
            grenze += breite;
        }
        return 1;
    }

    /// <summary>Das Stück, in dem das Zeichen rechts der Stelle steht, samt Index darin.</summary>
    private static (TdInline? Stueck, int Innen) StueckRechtsVon(TdParagraph absatz, int linear)
    {
        int summe = 0;
        foreach (var stueck in Stuecke(absatz))
        {
            int laenge = Laenge(stueck);
            if (summe + laenge > linear) return (stueck, linear - summe);
            summe += laenge;
        }
        return (null, 0);
    }

    /// <summary>Das Stück, in dem das Zeichen links der Stelle steht, samt Index dahinter.</summary>
    private static (TdInline? Stueck, int Innen) StueckLinksVon(TdParagraph absatz, int linear)
    {
        int summe = 0;
        foreach (var stueck in Stuecke(absatz))
        {
            int laenge = Laenge(stueck);
            if (laenge > 0 && summe + laenge >= linear) return (stueck, linear - summe);
            summe += laenge;
        }
        return (null, 0);
    }

    // ---------------------------------------------------------------- Wörter

    /// <summary>
    /// Wozu ein Zeichen gehört. <b>Ein Wort ist ein Lauf gleicher Art</b> — das ist die
    /// einfachste Regel, die sich verhält wie erwartet: Ein Doppelklick auf „Haus" nimmt das
    /// Wort, einer auf „, " den Zwischenraum, und einer auf ein Feld das Feld.
    /// </summary>
    private enum Zeichenart
    {
        /// <summary>Ein Feld, ein Bild, ein Zeilenumbruch — unteilbar und für sich allein.</summary>
        Unteilbar,
        Leerraum,
        Wortzeichen,

        /// <summary>Satzzeichen und alles andere.</summary>
        Sonstiges,
    }

    /// <summary>
    /// Die Art des Zeichens **rechts** von <paramref name="linear"/> — <c>null</c> am
    /// Absatzende, wo keines mehr steht.
    /// </summary>
    private static Zeichenart? ArtRechtsVon(TdParagraph absatz, int linear)
    {
        if (linear < 0 || linear >= Laenge(absatz)) return null;

        var (stueck, innen) = StueckRechtsVon(absatz, linear);
        if (stueck is not TdRun run || innen >= run.Text.Length) return Zeichenart.Unteilbar;

        char c = run.Text[innen];

        // Der Unterstrich zählt zum Wort, weil er in Bezeichnern und Dateinamen mitten darin
        // steht; der Bindestrich nicht, weil „Schwarz-Weiß" zwei Wörter sind — so hält es Word.
        if (char.IsWhiteSpace(c)) return Zeichenart.Leerraum;
        if (char.IsLetterOrDigit(c) || c == '_') return Zeichenart.Wortzeichen;

        return Zeichenart.Sonstiges;
    }

    /// <summary>
    /// Das Wort unter dieser Stelle — die Antwort auf einen Doppelklick.
    ///
    /// <para>
    /// <b>Genommen wird das Zeichen rechts der Stelle</b>, und am Absatzende das links davon:
    /// Ein Doppelklick hinter das letzte Wort einer Zeile soll dieses Wort nehmen und nicht ins
    /// Leere greifen. <b>Über die Absatzgrenze geht es nie</b> — ein Wort steht in einem Absatz.
    /// </para>
    /// </summary>
    public static TdSelection Wort(TdDocument doc, TdPosition stelle)
    {
        var jetzt = Normalisieren(doc, stelle);
        var absatz = AbsatzAn(doc, jetzt.Paragraph);
        if (absatz is null) return new TdSelection(jetzt);

        int linear = Linear(absatz, jetzt);

        // Am Absatzende gibt es rechts nichts mehr — dann gilt das Zeichen davor.
        var art = ArtRechtsVon(absatz, linear) ?? ArtRechtsVon(absatz, linear - 1);
        if (art is not { } gesucht) return new TdSelection(jetzt);

        // Unteilbares steht für sich: ein Feld ist ein „Wort", zwei Felder sind zwei.
        if (gesucht == Zeichenart.Unteilbar)
        {
            int einer = ArtRechtsVon(absatz, linear) is null ? linear - 1 : linear;
            return new TdSelection(
                AusLinear(absatz, jetzt.Paragraph, einer),
                AusLinear(absatz, jetzt.Paragraph, einer + 1));
        }

        int von = linear;
        while (von > 0 && ArtRechtsVon(absatz, von - 1) == gesucht) von--;

        int bis = linear;
        while (ArtRechtsVon(absatz, bis) == gesucht) bis++;

        return new TdSelection(
            AusLinear(absatz, jetzt.Paragraph, von),
            AusLinear(absatz, jetzt.Paragraph, bis));
    }

    /// <summary>
    /// Ein Wort nach rechts (Strg+Pfeil): über den Rest des laufenden Wortes hinweg und über
    /// den Leerraum dahinter — <b>an den Anfang des nächsten Wortes</b>, wie in Word.
    ///
    /// <para>
    /// Am Absatzende geht es in den nächsten Absatz, und dort wird **nicht** weitergelaufen:
    /// Ein Absatzwechsel ist für sich genommen schon ein Sprung, und wer ihn überspränge,
    /// käme an einer Absatzmarke nie zum Stehen.
    /// </para>
    /// </summary>
    public static TdPosition WortRechts(TdDocument doc, TdPosition stelle)
    {
        var jetzt = Normalisieren(doc, stelle);
        var absatz = AbsatzAn(doc, jetzt.Paragraph);
        if (absatz is null) return jetzt;

        int linear = Linear(absatz, jetzt);
        if (linear >= Laenge(absatz)) return Rechts(doc, jetzt);

        // Erst den laufenden Lauf zu Ende, dann den Leerraum dahinter.
        if (ArtRechtsVon(absatz, linear) is { } art)
            while (ArtRechtsVon(absatz, linear) == art) linear++;

        while (ArtRechtsVon(absatz, linear) == Zeichenart.Leerraum) linear++;

        return AusLinear(absatz, jetzt.Paragraph, linear);
    }

    /// <summary>
    /// Ein Wort nach links: über den Leerraum davor hinweg an den **Anfang** des Wortes, in dem
    /// die Stelle steht oder das links von ihr liegt.
    /// <inheritdoc cref="WortRechts" path="/para[1]"/>
    /// </summary>
    public static TdPosition WortLinks(TdDocument doc, TdPosition stelle)
    {
        var jetzt = Normalisieren(doc, stelle);
        var absatz = AbsatzAn(doc, jetzt.Paragraph);
        if (absatz is null) return jetzt;

        int linear = Linear(absatz, jetzt);
        if (linear <= 0) return Links(doc, jetzt);

        while (linear > 0 && ArtRechtsVon(absatz, linear - 1) == Zeichenart.Leerraum) linear--;

        if (linear > 0 && ArtRechtsVon(absatz, linear - 1) is { } art)
            while (linear > 0 && ArtRechtsVon(absatz, linear - 1) == art) linear--;

        return AusLinear(absatz, jetzt.Paragraph, linear);
    }

    // ---------------------------------------------------------------- Lesen

    /// <summary>
    /// Der Klartext einer Auswahl — Absätze durch Zeilenumbruch getrennt, wie in
    /// <see cref="TdDocument.PlainText"/>.
    ///
    /// <para>
    /// <b>Er ist nicht so lang, wie die Auswahl Schritte breit ist</b>, und das ist kein
    /// Fehler: Ein Feld und ein Bild sind je einen Schritt breit und steuern kein Zeichen bei
    /// (<see cref="Laenge(TdInline)"/>). Wer Zeichen zählen will, zählt hier; wer Stellen
    /// zählen will, zählt dort.
    /// </para>
    /// </summary>
    public static string Text(TdDocument doc, TdSelection auswahl)
    {
        var absaetze = Absaetze(doc);
        if (absaetze.Count == 0 || auswahl.IsEmpty) return "";

        var von = Normalisieren(doc, auswahl.Start);
        var bis = Normalisieren(doc, auswahl.End);

        var sb = new System.Text.StringBuilder();

        for (int p = von.Paragraph; p <= bis.Paragraph && p < absaetze.Count; p++)
        {
            if (p > von.Paragraph) sb.Append('\n');

            var absatz = absaetze[p];
            int anfang = p == von.Paragraph ? Linear(absatz, von) : 0;
            int ende = p == bis.Paragraph ? Linear(absatz, bis) : Laenge(absatz);

            AbsatzTextAnhaengen(sb, absatz, anfang, ende);
        }

        return sb.ToString();
    }

    /// <summary>Der Klartext eines Absatzstücks zwischen zwei Abständen vom Absatzanfang.</summary>
    private static void AbsatzTextAnhaengen(
        System.Text.StringBuilder sb, TdParagraph absatz, int von, int bis)
    {
        int summe = 0;
        foreach (var stueck in Stuecke(absatz))
        {
            int laenge = Laenge(stueck);
            int anfang = Math.Max(von, summe);
            int ende = Math.Min(bis, summe + laenge);

            if (ende > anfang)
            {
                if (stueck is TdRun run)
                    sb.Append(run.Text.AsSpan(anfang - summe, ende - anfang));
                else
                    sb.Append(stueck.PlainText());
            }

            summe += laenge;
            if (summe >= bis) break;
        }
    }
}
