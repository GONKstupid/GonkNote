namespace GonkNote.Core.Text;

/// <summary>
/// <b>Formate setzen</b> — Schritt 6 des Schreibens (HANDOFF §6), die Rechnung dazu.
///
/// <para>
/// <b>Warum das nicht über <see cref="TdEdit.Ersetzen"/> läuft, obwohl es zunächst danach
/// aussieht.</b> Die Auswahl durch dieselben Stücke in neuem Format zu ersetzen wäre ein
/// Zweizeiler — und an zwei Stellen falsch. Erstens vergibt <c>Ersetzen</c> den
/// **Zwischen**absätzen einer mehrabsätzigen Auswahl das Absatzformat des ersten (die
/// Word-Regel fürs Verbinden und Teilen, §4.32); beim Fettmachen über drei Absätze hinweg
/// verlöre der mittlere still seine eigene Gestalt. Zweitens käme als Art
/// <see cref="TdEditArt.Tippen"/> heraus, und der Verlauf zöge das Fettmachen mit dem
/// getippten Wort davor zu **einem** Schritt zusammen (§4.33) — ein Strg+Z nähme dann beides
/// zurück. Formatieren ist <see cref="TdEditArt.Struktur"/>: es fasst nie zusammen.
/// </para>
/// <para>
/// <b>Was es mit <c>Ersetzen</c> teilt, teilt es wirklich</b> — <see cref="TdEdit.Bereich"/>,
/// <see cref="TdEdit.Teil"/> und <see cref="TdEdit.Aufraeumen"/> stehen dort und werden hier
/// gerufen. Eine zweite Antwort auf „welche Blöcke berührt diese Auswahl" wäre die erste, die
/// von der anderen abweicht.
/// </para>
/// <para>
/// <b>Und die Regel aus §4.32 gilt hier zum ersten Mal ernsthaft: Stücke werden nie verändert,
/// sondern ersetzt.</b> <see cref="TdChange"/> hält als Sicherung die Absätze, wie sie waren —
/// wer beim Fettmachen ein <see cref="TdCharFormat"/> an Ort und Stelle umstellte, änderte
/// damit auch jede schon gemerkte Änderung, und der Fehler käme als kaputter
/// Rückgängig-Stapel heraus. Deshalb bekommt jedes berührte Stück eine **Kopie**.
/// </para>
/// <para>
/// <b>Die benannte Lücke: eine leere Auswahl ändert nichts.</b> In Word merkt sich der Editor
/// dann ein Format für das *nächste* getippte Zeichen. Das ist ein Zustand der Oberfläche und
/// keiner des Dokuments — im Modell gibt es nichts zu ändern, also kommt <c>null</c> zurück.
/// Wer es bauen will, hält es im Kopf und gibt es <see cref="TdEdit.Tippen"/> mit.
/// </para>
/// </summary>
/// <summary>
/// Was ein Absatz außer seinen Stücken hat — <b>der Griff, den
/// <see cref="TdFormatEdit.Absatzweise"/> dem Aufrufer in die Hand gibt.</b>
///
/// <para>
/// <b>Die Stücke stehen absichtlich nicht darin.</b> Wer sie ändern will, nimmt
/// <see cref="TdFormatEdit.Zeichen"/> — dort werden sie kopiert statt umgestellt (§4.32). Ein
/// Träger, der sie mitreichte, wäre die Einladung, genau das zu brechen.
/// </para>
/// </summary>
public sealed class TdAbsatzStil
{
    /// <summary>Ausrichtung, Einzüge, Abstände — als Abweichung vom Dokumentstandard.</summary>
    public TdParaFormat Format { get; set; } = new();

    /// <summary>
    /// Das Zeichenformat des **ganzen** Absatzes. Hier steht, was eine Überschrift ausmacht:
    /// Größe und Fett einmal am Absatz statt an jedem Lauf — nur so überlebt ein fett gemachtes
    /// Wort darin eine spätere Änderung der Überschrift (<see cref="TdParagraph.CharFormat"/>).
    /// </summary>
    public TdCharFormat CharFormat { get; set; } = new();

    /// <summary>Zu welcher Liste und Ebene der Absatz gehört; <c>null</c> = kein Listenpunkt.</summary>
    public TdListRef? List { get; set; }
}

public static class TdFormatEdit
{
    // ---------------------------------------------------------------- Zeichenformat

    /// <summary>
    /// Setzt das Zeichenformat der Auswahl — fett, kursiv, Schrift, Größe, Farbe.
    ///
    /// <para>
    /// <b><paramref name="aendern"/> bekommt zwei Formate und darf nur das erste anfassen.</b>
    /// Das erste ist die **Abweichung** des Stücks — dorthin wird geschrieben (§4.14), denn nur
    /// so bleibt „dieses Wort ist fett" von „dieses Wort sieht gerade zufällig so aus"
    /// unterschieden, und eine spätere Änderung an der Überschrift geht nicht daran vorbei. Das
    /// zweite ist dasselbe Stück **aufgelöst**, zum Nachsehen: „eine Stufe größer" braucht die
    /// Größe, die gerade gilt, und die steht in der Abweichung gerade dann nicht, wenn sie vom
    /// Absatz kommt.
    /// </para>
    /// <para>
    /// <b>Umschalten entscheidet der Aufrufer</b>, nicht diese Rechnung. Ob ein Klick auf „F"
    /// fett macht oder aufhebt, hängt davon ab, was die Auswahl gerade zeigt — die Antwort
    /// darauf steht in <see cref="Gemeinsam"/>, und sie gehört dorthin, wo der Knopf steht.
    /// </para>
    /// </summary>
    public static TdChange? Zeichen(
        TdDocument doc, TdSelection auswahl, Action<TdCharFormat, TdCharFormat> aendern) =>
        Zeichen(doc, auswahl, (abweichung, aufgeloest, _) => aendern(abweichung, aufgeloest));

    /// <summary>
    /// <inheritdoc cref="Zeichen(TdDocument, TdSelection, Action{TdCharFormat, TdCharFormat})" path="/summary/node()[1]"/>
    ///
    /// <para>
    /// Wie oben, nur bekommt <paramref name="aendern"/> ein <b>drittes</b> Format: die
    /// <b>Unterlage</b> des Stücks, also das, was dort ohne jede Stück-Abweichung gälte
    /// (Absatz → Dokument → Standard). <b>Nur wer sie kennt, kann etwas übertragen, ohne es
    /// einzubrennen</b> — siehe <see cref="Uebertragen"/>. Für alles andere genügt die Fassung
    /// mit zwei Formaten.
    /// </para>
    /// </summary>
    private static TdChange? Zeichen(
        TdDocument doc, TdSelection auswahl,
        Action<TdCharFormat, TdCharFormat, TdCharFormat> aendern)
    {
        var gezogen = TdCursor.Normalisieren(doc, auswahl);
        var start = gezogen.Start;
        var ende = gezogen.End;

        // Leere Auswahl: im Dokument ändert sich nichts (siehe die benannte Lücke oben).
        if (start == ende) return null;

        if (TdEdit.Bereich(doc, start, ende) is not { } bereich) return null;
        var (container, iA, iB, absatzA, absatzB) = bereich;

        int vonA = TdCursor.Linear(absatzA, start);
        int bisB = TdCursor.Linear(absatzB, ende);

        var alt = container.GetRange(iA, iB - iA + 1);
        var neu = new List<TdBlock>(alt.Count);

        foreach (var block in alt)
        {
            // Ein Seitenumbruch zwischen zwei ausgewählten Absätzen hat kein Zeichenformat —
            // er bleibt **dasselbe Objekt**, damit die Rücknahme ihn wiedererkennt.
            if (block is not TdParagraph absatz) { neu.Add(block); continue; }

            int von = ReferenceEquals(absatz, absatzA) ? vonA : 0;
            int bis = ReferenceEquals(absatz, absatzB) ? bisB : TdCursor.Laenge(absatz);

            neu.Add(NeuFormatiert(doc, absatz, von, bis, aendern));
        }

        // **Die Stelle wird neu gerechnet und nicht übernommen.** Formatieren ändert kein
        // Zeichen, wohl aber den Schnitt der Stücke: Wer die Mitte eines Stücks fett macht,
        // bekommt drei daraus, und die Stücknummer in der alten Stelle zeigte danach woanders
        // hin. Der Abstand vom Absatzanfang ist davon unberührt — genau dafür gibt es ihn
        // (§4.30).
        var neuStart = TdCursor.AusLinear((TdParagraph)neu[0], start.Paragraph, vonA);
        var neuEnde = TdCursor.AusLinear((TdParagraph)neu[^1], ende.Paragraph, bisB);

        return new TdChange(
            container, iA, alt, neu, gezogen, Wie(gezogen, neuStart, neuEnde),
            TdEditArt.Struktur, schliesstGruppe: true);
    }

    /// <summary>
    /// <b>Der Formatpinsel</b>: überträgt ein aufgenommenes Zeichenformat auf die Auswahl —
    /// <b>sichtbar vollständig, im Dokument so sparsam wie möglich</b> (§4.87).
    ///
    /// <para>
    /// <b>Die Entscheidung, die hier steckt.</b> §4.14 trennt die <b>Abweichung</b> eines Stücks
    /// von seinem <b>wirksamen</b> Format, und ein Pinsel kann jede der beiden nehmen — beide
    /// falsch. Nimmt er die Abweichung, überträgt ein Wort, das nur wegen seiner Überschrift
    /// fett aussieht, <b>nichts</b>: der Nutzer klickt und sieht keine Wirkung. Nimmt er das
    /// wirksame Format und schreibt es hin, <b>brennt er neun Eigenschaften als Stück-Abweichung
    /// ein</b> — und eine spätere Änderung an der Überschrift ginge an genau diesen Wörtern
    /// vorbei. Das ist das, was `TdCharFormat` verhindern soll.
    /// </para>
    /// <para>
    /// <b>Deshalb die dritte Antwort: aufnehmen, was wirkt — hinschreiben, was fehlt.</b>
    /// <paramref name="quelle"/> ist das <b>aufgelöste</b> Format der Aufnahmestelle
    /// (<see cref="Gemeinsam"/> liefert genau das). Am Ziel wird jede Eigenschaft mit der
    /// <b>Unterlage</b> verglichen — dem, was dort ohne Stück-Abweichung ohnehin gälte — und nur
    /// in die Abweichung geschrieben, wo sich beide unterscheiden. <b>Das Ergebnis sieht aus wie
    /// „wirksames Format" und verhält sich wie „Abweichung".</b>
    /// </para>
    /// <para>
    /// Zwei Fälle, an denen man es ablesen kann:
    /// <list type="bullet">
    ///   <item>
    ///     Quelle steht in „Überschrift 1" (fett vom Absatz), Ziel im Fließtext: die Unterlage
    ///     des Ziels ist mager, also wird <c>Bold = true</c> geschrieben. <b>Der Nutzer sieht
    ///     die Wirkung</b>, die er erwartet.
    ///   </item>
    ///   <item>
    ///     Quelle <b>und</b> Ziel stehen in „Überschrift 1": es wird <b>nichts</b> geschrieben.
    ///     Ändert später jemand die Überschrift, ändern sich beide mit — <b>genau das
    ///     Versprechen aus §4.14</b>.
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Es wird immer alles zugewiesen, auch <c>null</c>.</b> Eine Eigenschaft, die am Ziel
    /// als Abweichung stand und die die Quelle nicht mitbringt, muss <b>fallen</b> — sonst
    /// überträge der Pinsel nur hinzu und nie weg, und zweimal Pinseln ergäbe etwas anderes als
    /// einmal.
    /// </para>
    /// </summary>
    public static TdChange? Uebertragen(TdDocument doc, TdSelection auswahl, TdCharFormat quelle)
    {
        var wirksam = quelle.Aufgeloest();

        return Zeichen(doc, auswahl, (abweichung, _, unterlage) =>
        {
            abweichung.FontFamily = NurText(wirksam.FontFamily, unterlage.FontFamily);
            abweichung.FontSize = Nur(wirksam.FontSize, unterlage.FontSize);
            abweichung.Bold = Nur(wirksam.Bold, unterlage.Bold);
            abweichung.Italic = Nur(wirksam.Italic, unterlage.Italic);
            abweichung.Underline = Nur(wirksam.Underline, unterlage.Underline);
            abweichung.Strikethrough = Nur(wirksam.Strikethrough, unterlage.Strikethrough);
            abweichung.Color = NurText(wirksam.Color, unterlage.Color);
            abweichung.Highlight = NurText(wirksam.Highlight, unterlage.Highlight);
            abweichung.VerticalAlign = Nur(wirksam.VerticalAlign, unterlage.VerticalAlign);
        });

        // Der Wert, wenn er von der Unterlage abweicht — sonst „nicht gesetzt".
        static T? Nur<T>(T? wert, T? unterlage) where T : struct =>
            EqualityComparer<T?>.Default.Equals(wert, unterlage) ? null : wert;

        static string? NurText(string? wert, string? unterlage) => wert == unterlage ? null : wert;
    }

    // ---------------------------------------------------------------- Absatzformat

    /// <summary>
    /// Setzt das Absatzformat aller berührten Absätze — Ausrichtung, Einzug, Abstände.
    ///
    /// <para>
    /// <b>Berührt genügt, ganz drin sein muss keiner.</b> Wer eine Auswahl über das Ende des
    /// einen und den Anfang des nächsten Absatzes zieht und dann zentriert, meint beide — das
    /// ist die Erwartung aus jedem Textprogramm. Deshalb reicht hier auch eine **leere**
    /// Auswahl: Sie berührt den Absatz, in dem sie steht, und der Cursor irgendwo im Absatz ist
    /// die übliche Art, ihn auszuwählen. Das ist der Unterschied zu
    /// <see cref="Zeichen"/> — dort hat eine leere Auswahl keinen Text, an dem etwas zu ändern
    /// wäre, hier hat sie einen Absatz.
    /// </para>
    /// <para>
    /// Die Stücke bleiben <b>dieselben Objekte</b>: An ihnen ändert sich nichts, und sie doppelt
    /// im Speicher zu halten hätte keinen Zweck. Der Absatz selbst wird ersetzt — er ist es, was
    /// sich ändert, und die Rücknahme braucht ihn unversehrt.
    /// </para>
    /// </summary>
    public static TdChange? Absatz(TdDocument doc, TdSelection auswahl, Action<TdParaFormat> aendern) =>
        Absatzweise(doc, auswahl, stil => aendern(stil.Format));

    /// <summary>
    /// <b>Der allgemeine Fall:</b> alles ändern, was ein Absatz außer seinen Stücken hat —
    /// Absatzformat, Zeichenformat des ganzen Absatzes und Listenzugehörigkeit.
    ///
    /// <para>
    /// <b>Warum es die drei zusammen gibt und nicht drei Handgriffe:</b> Eine Vorlage
    /// (<see cref="TdStil"/>) setzt alle drei auf einmal — Größe und Fett am
    /// <see cref="TdParagraph.CharFormat"/>, Abstände am <see cref="TdParagraph.Format"/>, und
    /// eine Überschrift ist kein Listenpunkt mehr. Drei Änderungen daraus zu machen hieße: drei
    /// Schritte im Verlauf für einen Handgriff (§4.33), und ein Strg+Z ließe die halbe Vorlage
    /// stehen.
    /// </para>
    /// <inheritdoc cref="Absatz" path="/summary/para[1]"/>
    /// <inheritdoc cref="Absatz" path="/summary/para[2]"/>
    /// </summary>
    public static TdChange? Absatzweise(
        TdDocument doc, TdSelection auswahl, Action<TdAbsatzStil> aendern)
    {
        var gezogen = TdCursor.Normalisieren(doc, auswahl);

        if (TdEdit.Bereich(doc, gezogen.Start, gezogen.End) is not { } bereich) return null;
        var (container, iA, iB, _, _) = bereich;

        var alt = container.GetRange(iA, iB - iA + 1);
        var neu = new List<TdBlock>(alt.Count);

        bool etwas = false;
        foreach (var block in alt)
        {
            if (block is not TdParagraph absatz) { neu.Add(block); continue; }

            var stil = new TdAbsatzStil
            {
                Format = absatz.Format.Kopie(),
                CharFormat = absatz.CharFormat.Kopie(),
                List = absatz.List,
            };
            aendern(stil);

            neu.Add(new TdParagraph(absatz.Inlines)
            {
                Format = stil.Format,
                CharFormat = stil.CharFormat,
                List = stil.List,
            });
            etwas = true;
        }

        // Eine Auswahl ohne einen einzigen Absatz gibt es heute nicht — aber eine Änderung, die
        // nur Seitenumbrüche „ersetzt", wäre ein Verlaufsschritt, der nichts tut.
        if (!etwas) return null;

        // Die Stelle bleibt, wo sie war: kein Zeichen und kein Stück hat sich bewegt.
        return new TdChange(
            container, iA, alt, neu, gezogen, gezogen,
            TdEditArt.Struktur, schliesstGruppe: true);
    }

    // ---------------------------------------------------------------- Verweis

    /// <summary>
    /// Legt einen Verweis um die Auswahl — oder nimmt ihn heraus, wenn
    /// <paramref name="ziel"/> leer ist.
    ///
    /// <para>
    /// <b>Es steht hier und nicht in einer eigenen Datei</b>, weil es dieselbe Sache ist wie
    /// fett machen: Der Text bleibt Zeichen für Zeichen derselbe, und nur seine Auszeichnung
    /// ändert sich. Es teilt deshalb bis auf eine Zeile denselben Weg — Kopf und Schwanz bleiben
    /// dieselben Objekte, die Mitte wird neu gebaut, und die Stelle wird nachgerechnet.
    /// </para>
    /// <para>
    /// <b>Erst auswickeln, dann einwickeln.</b> Liegt in der Auswahl schon ein Verweis, wird er
    /// aufgelöst und der neue um **alles** gelegt. Ohne das entstünde ein Verweis im Verweis —
    /// den kennt weder DOCX noch <see cref="TdParagraph.FlacheStuecke"/>, und beim Export käme
    /// das äußere Ziel für den ganzen Text heraus (§7, die Erbfolge).
    /// </para>
    /// <para>
    /// <b>Je Absatz einer</b>, wenn die Auswahl über mehrere reicht: Ein Verweis steht in einem
    /// Absatz, denn eine Absatzmarke dazwischen wäre im DOCX ein zweites <c>w:hyperlink</c>
    /// ohnehin.
    /// </para>
    /// </summary>
    public static TdChange? Verweis(TdDocument doc, TdSelection auswahl, string? ziel)
    {
        var gezogen = TdCursor.Normalisieren(doc, auswahl);
        var start = gezogen.Start;
        var ende = gezogen.End;

        // Ein Verweis ohne Text zeigt nichts an und kann den Cursor nicht tragen (§4.30) —
        // eine leere Auswahl ist deshalb nicht „ein Verweis der Länge null", sondern nichts.
        if (start == ende) return null;

        if (TdEdit.Bereich(doc, start, ende) is not { } bereich) return null;
        var (container, iA, iB, absatzA, absatzB) = bereich;

        int vonA = TdCursor.Linear(absatzA, start);
        int bisB = TdCursor.Linear(absatzB, ende);

        var alt = container.GetRange(iA, iB - iA + 1);
        var neu = new List<TdBlock>(alt.Count);

        foreach (var block in alt)
        {
            if (block is not TdParagraph absatz) { neu.Add(block); continue; }

            int von = ReferenceEquals(absatz, absatzA) ? vonA : 0;
            int bis = ReferenceEquals(absatz, absatzB) ? bisB : TdCursor.Laenge(absatz);

            var kopf = TdEdit.Teil(absatz.Inlines, 0, von);
            var mitte = Ausgewickelt(TdEdit.Teil(absatz.Inlines, von, bis));
            var schwanz = TdEdit.Teil(absatz.Inlines, bis, TdCursor.Laenge(absatz));

            var stuecke = new List<TdInline>(kopf.Count + 1 + schwanz.Count);
            stuecke.AddRange(kopf);

            if (string.IsNullOrEmpty(ziel)) stuecke.AddRange(mitte);
            else if (mitte.Count > 0) stuecke.Add(new TdHyperlink(ziel) { Inlines = mitte });

            stuecke.AddRange(schwanz);

            neu.Add(new TdParagraph(TdEdit.Aufraeumen(stuecke))
            {
                Format = absatz.Format,
                CharFormat = absatz.CharFormat,
                List = absatz.List,
            });
        }

        var neuStart = TdCursor.AusLinear((TdParagraph)neu[0], start.Paragraph, vonA);
        var neuEnde = TdCursor.AusLinear((TdParagraph)neu[^1], ende.Paragraph, bisB);

        return new TdChange(
            container, iA, alt, neu, gezogen, Wie(gezogen, neuStart, neuEnde),
            TdEditArt.Struktur, schliesstGruppe: true);
    }

    /// <summary>
    /// Das Ziel des Verweises, in dem die Auswahl liegt — <c>null</c>, wenn dort keiner liegt
    /// oder mehrere verschiedene. <b>Die Auskunft, aus der ein Ribbon „Verweis bearbeiten" von
    /// „Verweis einfügen" unterscheidet.</b>
    /// </summary>
    public static string? VerweisZiel(TdDocument doc, TdSelection auswahl)
    {
        var gezogen = TdCursor.Normalisieren(doc, auswahl);
        var absatz = TdCursor.AbsatzAn(doc, gezogen.Start.Paragraph);
        if (absatz is null) return null;

        int von = TdCursor.Linear(absatz, gezogen.Start);
        int bis = ReferenceEquals(absatz, TdCursor.AbsatzAn(doc, gezogen.End.Paragraph))
            ? TdCursor.Linear(absatz, gezogen.End)
            : TdCursor.Laenge(absatz);

        string? gefunden = null;
        int summe = 0;

        foreach (var (stueck, verweis) in absatz.FlacheStuecke())
        {
            int laenge = TdCursor.Laenge(stueck);

            // **Berührt genügt, und bei leerer Auswahl zählt das Stück links** — dieselbe
            // Zugehörigkeit wie beim Erben eines Formats (§4.30), damit ein Klick hinter einen
            // Verweis ihn noch meint.
            bool drin = von == bis
                ? von > summe && von <= summe + laenge
                : Math.Min(bis, summe + laenge) > Math.Max(von, summe);

            if (drin)
            {
                if (verweis is null) return null;
                if (gefunden is not null && gefunden != verweis.Target) return null;
                gefunden = verweis.Target;
            }

            summe += laenge;
        }

        return gefunden;
    }

    /// <summary>
    /// Die Stücke ohne die Verweise um sie herum — <inheritdoc cref="Verweis" path="/summary/para[2]"/>
    /// </summary>
    private static List<TdInline> Ausgewickelt(IReadOnlyList<TdInline> stuecke)
    {
        var ziel = new List<TdInline>(stuecke.Count);

        foreach (var stueck in stuecke)
        {
            if (stueck is TdHyperlink verweis) ziel.AddRange(Ausgewickelt(verweis.Inlines));
            else ziel.Add(stueck);
        }

        return ziel;
    }

    // ---------------------------------------------------------------- Was zeigt die Auswahl?

    /// <summary>
    /// Das Zeichenformat, das die ganze Auswahl gemeinsam hat — <b>die Auskunft, aus der ein
    /// Ribbon seine Knöpfe stellt</b>.
    ///
    /// <para>
    /// <b>Zurück kommt ein aufgelöstes Format, in dem <c>null</c> etwas anderes heißt als
    /// sonst:</b> nicht „keine Abweichung", sondern <b>„darüber ist die Auswahl sich nicht
    /// einig"</b>. Ein Ribbon braucht genau diese dritte Antwort — ein Knopf über einer Auswahl
    /// aus fettem und nicht fettem Text ist weder gedrückt noch nicht gedrückt, und ein
    /// Schriftfeld, das dann eine der beiden Schriften nennt, behauptet etwas Falsches.
    /// </para>
    /// <para>
    /// <b>Eine leere Auswahl antwortet mit dem Format, das das nächste getippte Zeichen bekäme</b>
    /// (<see cref="TdEdit.FormatBei"/>) — nicht mit „nichts". Wer hinter ein fettes Wort klickt,
    /// soll den Fett-Knopf gedrückt sehen, denn genau so würde er weiterschreiben.
    /// </para>
    /// </summary>
    public static TdCharFormat Gemeinsam(TdDocument doc, TdSelection auswahl)
    {
        var gezogen = TdCursor.Normalisieren(doc, auswahl);
        var start = gezogen.Start;
        var ende = gezogen.End;

        if (start == ende)
        {
            var hier = TdCursor.AbsatzAn(doc, start.Paragraph);
            if (hier is null) return new TdCharFormat();

            return TdEdit.FormatBei(hier, start)
                .Over(hier.CharFormat).Over(doc.DefaultCharFormat).Aufgeloest();
        }

        TdCharFormat? gemeinsam = null;

        if (TdEdit.Bereich(doc, start, ende) is not { } bereich) return new TdCharFormat();
        var (container, iA, iB, absatzA, absatzB) = bereich;

        for (int i = iA; i <= iB; i++)
        {
            if (container[i] is not TdParagraph absatz) continue;

            int von = ReferenceEquals(absatz, absatzA) ? TdCursor.Linear(absatzA, start) : 0;
            int bis = ReferenceEquals(absatz, absatzB) ? TdCursor.Linear(absatzB, ende)
                                                       : TdCursor.Laenge(absatz);

            foreach (var stueck in Ausgewaehlte(absatz, von, bis))
            {
                var format = doc.FormatVon(absatz, stueck);
                gemeinsam = gemeinsam is null ? format : NurGleiches(gemeinsam, format);
            }
        }

        // Eine Auswahl, die nur über leere Absätze reicht, hat kein Stück — dann zählt, was dort
        // getippt würde.
        return gemeinsam ?? Gemeinsam(doc, new TdSelection(start));
    }

    /// <summary>
    /// Das Absatzformat, das alle berührten Absätze gemeinsam haben —
    /// <inheritdoc cref="Gemeinsam(TdDocument, TdSelection)" path="/para[1]"/>
    /// </summary>
    public static TdParaFormat GemeinsamerAbsatz(TdDocument doc, TdSelection auswahl)
    {
        var gezogen = TdCursor.Normalisieren(doc, auswahl);

        if (TdEdit.Bereich(doc, gezogen.Start, gezogen.End) is not { } bereich)
            return new TdParaFormat();

        var (container, iA, iB, _, _) = bereich;

        TdParaFormat? gemeinsam = null;
        for (int i = iA; i <= iB; i++)
        {
            if (container[i] is not TdParagraph absatz) continue;

            var format = doc.FormatVon(absatz);
            gemeinsam = gemeinsam is null ? format : NurGleiches(gemeinsam, format);
        }

        return gemeinsam ?? new TdParaFormat();
    }

    // ---------------------------------------------------------------- Kleinteile

    /// <summary>
    /// Derselbe Absatz mit umformatiertem Ausschnitt. <b>Kopf und Schwanz bleiben dieselben
    /// Objekte</b> — angefasst wird nur, was in der Auswahl liegt.
    /// </summary>
    private static TdParagraph NeuFormatiert(
        TdDocument doc, TdParagraph absatz, int von, int bis,
        Action<TdCharFormat, TdCharFormat, TdCharFormat> aendern)
    {
        var kopf = TdEdit.Teil(absatz.Inlines, 0, von);
        var mitte = TdEdit.Teil(absatz.Inlines, von, bis);
        var schwanz = TdEdit.Teil(absatz.Inlines, bis, TdCursor.Laenge(absatz));

        var stuecke = new List<TdInline>(kopf.Count + mitte.Count + schwanz.Count);
        stuecke.AddRange(kopf);
        foreach (var stueck in mitte) stuecke.Add(Umformatiert(doc, absatz, stueck, aendern));
        stuecke.AddRange(schwanz);

        return new TdParagraph(TdEdit.Aufraeumen(stuecke))
        {
            Format = absatz.Format,
            CharFormat = absatz.CharFormat,
            List = absatz.List,
        };
    }

    /// <summary>
    /// Ein Stück mit geändertem Format — <b>als Kopie</b>, siehe die Regel oben.
    ///
    /// <para>
    /// <b>Ein Verweis bekommt es an seinen Text und nicht an sich selbst.</b> Sein eigenes
    /// Format ist die Unterlage seiner Stücke; wer es dort setzte, bekäme ein fettes Wort, das
    /// beim nächsten Tippen darin wieder mager würde — die Stücke überschreiben die Unterlage.
    /// Und er bekommt eine **eigene Stückliste**: Die des alten Verweises gehört der Sicherung.
    /// </para>
    /// </summary>
    private static TdInline Umformatiert(
        TdDocument doc, TdParagraph absatz, TdInline stueck,
        Action<TdCharFormat, TdCharFormat, TdCharFormat> aendern)
    {
        if (stueck is TdHyperlink verweis)
        {
            var neu = new TdHyperlink(verweis.Target) { Format = verweis.Format };
            foreach (var innen in verweis.Inlines)
                neu.Inlines.Add(Umformatiert(doc, absatz, innen, aendern));
            return neu;
        }

        var format = stueck.Format.Kopie();
        aendern(format,
                doc.FormatVon(absatz, stueck),
                absatz.CharFormat.Over(doc.DefaultCharFormat).Aufgeloest());
        return stueck.MitFormat(format);
    }

    /// <summary>
    /// Die Stücke eines Absatzes zwischen zwei Abständen — <b>flach</b>, denn nach dem Format
    /// gefragt wird der Linktext und nicht der Verweis um ihn herum (§7, die Erbfolge).
    /// </summary>
    private static IEnumerable<TdInline> Ausgewaehlte(TdParagraph absatz, int von, int bis)
    {
        int summe = 0;
        foreach (var stueck in TdCursor.Stuecke(absatz))
        {
            int laenge = TdCursor.Laenge(stueck);

            // Ein Stück zählt mit, sobald es die Auswahl überlappt. Ein unteilbares Stück
            // (Feld, Bild) ist einen Schritt breit und liegt damit ganz drin oder ganz draußen.
            if (Math.Min(bis, summe + laenge) > Math.Max(von, summe)) yield return stueck;

            summe += laenge;
            if (summe >= bis) break;
        }
    }

    /// <summary>Wo zwei Formate übereinstimmen — sonst <c>null</c> (siehe <see cref="Gemeinsam"/>).</summary>
    private static TdCharFormat NurGleiches(TdCharFormat a, TdCharFormat b) => new()
    {
        FontFamily = a.FontFamily == b.FontFamily ? a.FontFamily : null,
        FontSize = a.FontSize == b.FontSize ? a.FontSize : null,
        Bold = a.Bold == b.Bold ? a.Bold : null,
        Italic = a.Italic == b.Italic ? a.Italic : null,
        Underline = a.Underline == b.Underline ? a.Underline : null,
        Strikethrough = a.Strikethrough == b.Strikethrough ? a.Strikethrough : null,
        Color = a.Color == b.Color ? a.Color : null,
        Highlight = a.Highlight == b.Highlight ? a.Highlight : null,
        VerticalAlign = a.VerticalAlign == b.VerticalAlign ? a.VerticalAlign : null,
    };

    /// <inheritdoc cref="NurGleiches(TdCharFormat, TdCharFormat)"/>
    private static TdParaFormat NurGleiches(TdParaFormat a, TdParaFormat b) => new()
    {
        Alignment = a.Alignment == b.Alignment ? a.Alignment : null,
        LeftIndentCm = a.LeftIndentCm == b.LeftIndentCm ? a.LeftIndentCm : null,
        RightIndentCm = a.RightIndentCm == b.RightIndentCm ? a.RightIndentCm : null,
        FirstLineIndentCm = a.FirstLineIndentCm == b.FirstLineIndentCm ? a.FirstLineIndentCm : null,
        SpaceBeforePt = a.SpaceBeforePt == b.SpaceBeforePt ? a.SpaceBeforePt : null,
        SpaceAfterPt = a.SpaceAfterPt == b.SpaceAfterPt ? a.SpaceAfterPt : null,
        LineSpacing = a.LineSpacing == b.LineSpacing ? a.LineSpacing : null,
        OutlineLevel = a.OutlineLevel == b.OutlineLevel ? a.OutlineLevel : null,
        ExcludeFromToc = a.ExcludeFromToc == b.ExcludeFromToc ? a.ExcludeFromToc : null,
        KeepWithNext = a.KeepWithNext == b.KeepWithNext ? a.KeepWithNext : null,
        PageBreakBefore = a.PageBreakBefore == b.PageBreakBefore ? a.PageBreakBefore : null,
        BottomBorder = a.BottomBorder == b.BottomBorder ? a.BottomBorder : null,
    };

    /// <summary>
    /// Die neue Auswahl mit der <b>Richtung</b> der alten. Wer von rechts nach links gezogen hat,
    /// hält die Spitze links — nähme man sie ihm hier weg, spränge sie beim nächsten
    /// Umschalt+Pfeil auf die andere Seite.
    /// </summary>
    private static TdSelection Wie(TdSelection vorlage, TdPosition start, TdPosition ende) =>
        vorlage.Focus < vorlage.Anchor
            ? new TdSelection(ende, start)
            : new TdSelection(start, ende);
}
