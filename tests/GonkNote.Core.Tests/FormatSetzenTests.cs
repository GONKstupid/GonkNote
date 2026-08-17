using System.Text;
using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdFormatEdit"/> — Schritt 6 des Schreibens (HANDOFF §6): Formate setzen.
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Formatieren sieht harmloser aus als Tippen — es
/// verändert keinen Buchstaben. Genau deshalb sind seine Fehler die stillen: Ein Fettmachen,
/// das die Sicherung des Verlaufs mit umstellt, macht Rückgängig kaputt, ohne dass beim
/// Formatieren selbst etwas falsch aussieht (§4.32). Ein Fettmachen über drei Absätze, das dem
/// mittleren nebenbei das Absatzformat des ersten gibt, fällt beim nächsten Öffnen auf und
/// nicht beim Klicken. Und eine Auswahl, deren Stellen nach dem Umformatieren nicht
/// nachgerechnet werden, springt beim nächsten Umschalt+Pfeil an eine fremde Stelle.
/// </para>
/// <para>
/// <b>Kein Umbruch und keine Schrift</b>, wie in den Schritten 1 bis 3: Diese Rechnung läuft
/// auf jedem Rechner gleich.
/// </para>
/// </summary>
public sealed class FormatSetzenTests
{
    // ==================== Hilfsmittel ====================

    private static TdDocument Dok(params TdBlock[] bloecke)
    {
        var doc = new TdDocument();
        var abschnitt = new TdSection();
        abschnitt.Blocks.AddRange(bloecke);
        doc.Sections.Add(abschnitt);
        return doc;
    }

    private static TdParagraph Abs(params TdInline[] stuecke) => new(stuecke);

    private static TdParagraph Text(string text) => new(text);

    private static TdRun Fett(string text) => new(text, new TdCharFormat { Bold = true });

    private static TdSelection Von(TdDocument doc, int absatzA, int a, int absatzB, int b) =>
        new(TdCursor.AusLinear(TdCursor.AbsatzAn(doc, absatzA)!, absatzA, a),
            TdCursor.AusLinear(TdCursor.AbsatzAn(doc, absatzB)!, absatzB, b));

    private static TdSelection Bei(TdDocument doc, int absatz, int linear)
    {
        var a = TdCursor.AbsatzAn(doc, absatz)!;
        return new TdSelection(TdCursor.AusLinear(a, absatz, linear));
    }

    /// <summary>
    /// Fett machen — der Handgriff, an dem alles hängt. Das zweite Format ist das aufgelöste
    /// zum Nachsehen; hier braucht es niemand.
    /// </summary>
    private static void FettAn(TdCharFormat f, TdCharFormat _) => f.Bold = true;

    /// <summary>
    /// Ein Abbild mit Stückgrenzen und Formatmarken — <b>der Klartext allein reicht hier
    /// überhaupt nicht</b>, denn am Klartext ändert Formatieren nichts.
    /// </summary>
    private static string Abbild(TdDocument doc)
    {
        var sb = new StringBuilder();

        foreach (var absatz in doc.Paragraphs())
        {
            sb.Append('¶');
            if (absatz.Format.Alignment is { } aus) sb.Append($"[{aus}]");
            Stuecke(sb, absatz.Inlines);
            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static void Stuecke(StringBuilder sb, IEnumerable<TdInline> stuecke)
    {
        foreach (var stueck in stuecke)
        {
            switch (stueck)
            {
                case TdRun run:
                    sb.Append($"<{Marke(run.Format)}:{run.Text}>");
                    break;
                case TdHyperlink verweis:
                    sb.Append($"<verweis:{verweis.Target}:");
                    Stuecke(sb, verweis.Inlines);
                    sb.Append('>');
                    break;
                case TdField feld:
                    sb.Append($"<feld:{Marke(feld.Format)}:{feld.Kind}>");
                    break;
                default:
                    sb.Append("<?>");
                    break;
            }
        }
    }

    private static string Marke(TdCharFormat format) =>
        (format.Bold == true ? "f" : "") + (format.Italic == true ? "k" : "");

    // ==================== Zeichenformat ====================

    /// <summary>Der einfachste Fall: ein Wort in der Mitte wird fett, und nur es.</summary>
    [Fact]
    public void Fett_wirkt_nur_auf_die_Auswahl()
    {
        var doc = Dok(Text("abcdef"));

        TdFormatEdit.Zeichen(doc, Von(doc, 0, 2, 0, 4), FettAn)!.Anwenden();

        Assert.Equal("¶<:ab><f:cd><:ef>\n", Abbild(doc));
    }

    /// <summary>
    /// <b>Der teuerste Fehler dieser Runde, wenn er nicht abgefangen wäre:</b> Formatieren
    /// verändert kein Zeichen — wer trotzdem etwas am Klartext ändert, hat einen Ausschnitt
    /// falsch gerechnet, und der Text ist weg.
    /// </summary>
    [Fact]
    public void Formatieren_aendert_kein_einziges_Zeichen()
    {
        var doc = Dok(Text("erste Zeile"), Text("zweite Zeile"), Text("dritte Zeile"));
        string vorher = doc.PlainText();

        TdFormatEdit.Zeichen(doc, Von(doc, 0, 3, 2, 6), FettAn)!.Anwenden();

        Assert.Equal(vorher, doc.PlainText());
    }

    /// <summary>
    /// <b>Die Regel aus §4.32, hier zum ersten Mal geprüft.</b> Wer das Format am alten Stück
    /// umstellt statt eine Kopie zu bauen, ändert damit die Sicherung des Verlaufs mit — und
    /// das Rückgängig führt nicht zurück. Am Abbild ist das nicht zu sehen; deshalb wird hier
    /// das alte Stück selbst befragt.
    /// </summary>
    [Fact]
    public void Das_alte_Stueck_bleibt_unveraendert()
    {
        var alt = new TdRun("abc");
        var doc = Dok(Abs(alt));

        TdFormatEdit.Zeichen(doc, Von(doc, 0, 0, 0, 3), FettAn)!.Anwenden();

        Assert.Null(alt.Bold());
        Assert.NotSame(alt, ((TdParagraph)doc.Sections[0].Blocks[0]).Inlines[0]);
    }

    /// <summary>Und die Probe darauf: zweimal hin und her muss dasselbe Dokument ergeben.</summary>
    [Fact]
    public void Ruecknahme_fuehrt_vollstaendig_zurueck()
    {
        var doc = Dok(Abs(new TdRun("ab"), Fett("cd"), new TdRun("ef")));
        string vorher = Abbild(doc);

        var aenderung = TdFormatEdit.Zeichen(doc, Von(doc, 0, 1, 0, 5), FettAn)!;
        aenderung.Anwenden();
        Assert.NotEqual(vorher, Abbild(doc));

        aenderung.Zuruecknehmen();
        Assert.Equal(vorher, Abbild(doc));

        aenderung.Anwenden();
        aenderung.Zuruecknehmen();
        Assert.Equal(vorher, Abbild(doc));
    }

    /// <summary>
    /// Benachbarte Stücke mit gleichem Format werden wieder zusammengelegt — sonst zerfiele der
    /// Absatz mit jedem Formatklick weiter (<c>TdEdit.Aufraeumen</c>).
    /// </summary>
    [Fact]
    public void Gleich_formatierte_Nachbarn_werden_zusammengelegt()
    {
        var doc = Dok(Abs(new TdRun("ab"), Fett("cd")));

        TdFormatEdit.Zeichen(doc, Von(doc, 0, 0, 0, 2), FettAn)!.Anwenden();

        Assert.Equal("¶<f:abcd>\n", Abbild(doc));
    }

    /// <summary>
    /// <b>Der Fall, für den diese Klasse nicht über <c>TdEdit.Ersetzen</c> läuft.</b> Eine
    /// Auswahl über drei Absätze darf dem mittleren nicht das Absatzformat des ersten geben —
    /// <c>Ersetzen</c> täte genau das (die Word-Regel fürs Verbinden), und hier wäre es
    /// stiller Verlust der Gestalt.
    /// </summary>
    [Fact]
    public void Der_mittlere_Absatz_behaelt_sein_eigenes_Absatzformat()
    {
        var links = Text("eins");
        var mitte = Text("zwei");
        var rechts = Text("drei");
        mitte.Format.Alignment = TdAlign.Center;

        var doc = Dok(links, mitte, rechts);

        TdFormatEdit.Zeichen(doc, Von(doc, 0, 1, 2, 3), FettAn)!.Anwenden();

        Assert.Equal(
            "¶<:e><f:ins>\n¶[Center]<f:zwei>\n¶<f:dre><:i>\n",
            Abbild(doc));
    }

    /// <summary>
    /// <b>Formatieren fasst im Verlauf nie zusammen.</b> Käme hier <c>Tippen</c> heraus, zöge
    /// <see cref="TdUndo"/> das Fettmachen mit dem Wort davor zu einem Schritt zusammen — ein
    /// Strg+Z nähme dann beides zurück (§4.33).
    /// </summary>
    [Fact]
    public void Formatieren_ist_eine_Strukturaenderung()
    {
        var doc = Dok(Text("abc"));

        var aenderung = TdFormatEdit.Zeichen(doc, Von(doc, 0, 0, 0, 3), FettAn)!;

        Assert.Equal(TdEditArt.Struktur, aenderung.Art);
    }

    /// <summary>
    /// Eine leere Auswahl hat keinen Text, an dem etwas zu ändern wäre — das ist die benannte
    /// Lücke, und sie heißt <c>null</c> und nicht „Änderung, die nichts tut".
    /// </summary>
    [Fact]
    public void Leere_Auswahl_aendert_am_Dokument_nichts()
    {
        var doc = Dok(Text("abc"));

        Assert.Null(TdFormatEdit.Zeichen(doc, Bei(doc, 0, 1), FettAn));
    }

    /// <summary>
    /// Ein Verweis bekommt das Format an seinen <b>Text</b> und nicht an sich selbst — sonst
    /// überschriebe das nächste getippte Zeichen darin die Unterlage wieder. Und sein Ziel
    /// überlebt: ein Verweis, der beim Fettmachen zerfällt, ist der Fehler aus §7.
    /// </summary>
    [Fact]
    public void Ein_Verweis_behaelt_sein_Ziel_und_bekommt_das_Format_an_den_Text()
    {
        var doc = Dok(Abs(new TdHyperlink("https://x", new TdRun("hier"))));

        TdFormatEdit.Zeichen(doc, Von(doc, 0, 0, 0, 4), FettAn)!.Anwenden();

        Assert.Equal("¶<verweis:https://x:<f:hier>>\n", Abbild(doc));
    }

    /// <summary>
    /// Die Stückliste des alten Verweises gehört der Sicherung — der neue braucht seine eigene,
    /// sonst ändert das Formatieren beide.
    /// </summary>
    [Fact]
    public void Der_alte_Verweis_behaelt_seine_Stuecke()
    {
        var alt = new TdHyperlink("https://x", new TdRun("hier"));
        var doc = Dok(Abs(alt));

        TdFormatEdit.Zeichen(doc, Von(doc, 0, 0, 0, 4), FettAn)!.Anwenden();

        Assert.Null(((TdRun)alt.Inlines[0]).Bold());
    }

    /// <summary>
    /// Ein Feld ist einen Schritt breit und unteilbar (§4.30) — formatieren lässt es sich
    /// trotzdem, und es bleibt ein Feld.
    /// </summary>
    [Fact]
    public void Ein_Feld_laesst_sich_formatieren_und_bleibt_ein_Feld()
    {
        var doc = Dok(Abs(new TdRun("S. "), new TdField(TdFieldKind.PageNumber)));

        TdFormatEdit.Zeichen(doc, Von(doc, 0, 3, 0, 4), FettAn)!.Anwenden();

        Assert.Equal("¶<:S. ><feld:f:PageNumber>\n", Abbild(doc));
    }

    /// <summary>
    /// <b>Die Stelle wird nachgerechnet und nicht übernommen.</b> Fett in der Mitte macht aus
    /// einem Stück drei; eine Auswahl, die ihre alten Stücknummern behielte, zeigte danach auf
    /// fremden Text. Geprüft wird deshalb am Abstand vom Absatzanfang.
    /// </summary>
    [Fact]
    public void Die_Auswahl_steht_danach_noch_auf_demselben_Text()
    {
        var doc = Dok(Text("abcdef"));

        var danach = TdFormatEdit.Zeichen(doc, Von(doc, 0, 2, 0, 4), FettAn)!.Anwenden();
        var absatz = TdCursor.AbsatzAn(doc, 0)!;

        Assert.Equal(2, TdCursor.Linear(absatz, danach.Start));
        Assert.Equal(4, TdCursor.Linear(absatz, danach.End));
        Assert.Equal("cd", TdCursor.Text(doc, danach));
    }

    /// <summary>
    /// Wer von rechts nach links gezogen hat, hält die Spitze links — sonst spränge sie beim
    /// nächsten Umschalt+Pfeil auf die andere Seite.
    /// </summary>
    [Fact]
    public void Die_Richtung_der_Auswahl_bleibt_erhalten()
    {
        var doc = Dok(Text("abcdef"));

        var rueckwaerts = Von(doc, 0, 4, 0, 2);
        var danach = TdFormatEdit.Zeichen(doc, rueckwaerts, FettAn)!.Anwenden();

        Assert.True(danach.Focus < danach.Anchor);
    }

    /// <summary>
    /// <b>Wofür das zweite Format da ist.</b> „Eine Stufe größer" muss wissen, wie groß es
    /// gerade ist — und das steht in der Abweichung gerade dann nicht, wenn die Größe vom Absatz
    /// kommt. Hier ist ein Stück 20 pt (am Stück gesetzt), das andere 14 pt (vom Absatz geerbt);
    /// nach dem Vergrößern müssen es 22 und 16 sein und nicht zweimal dieselbe Zahl.
    /// </summary>
    [Fact]
    public void Groesser_rechnet_je_Stueck_mit_dem_aufgeloesten_Format()
    {
        var absatz = Abs(
            new TdRun("gross", new TdCharFormat { FontSize = 20 }),
            new TdRun("erbt"));
        absatz.CharFormat.FontSize = 14;
        var doc = Dok(absatz);

        TdFormatEdit.Zeichen(doc, Von(doc, 0, 0, 0, 9),
            (abweichung, aufgeloest) => abweichung.FontSize = aufgeloest.FontSize + 2)!.Anwenden();

        var neu = (TdParagraph)doc.Sections[0].Blocks[0];
        Assert.Equal(22, ((TdRun)neu.Inlines[0]).Format.FontSize);
        Assert.Equal(16, ((TdRun)neu.Inlines[1]).Format.FontSize);
    }

    // ==================== Absatzformat ====================

    /// <summary>Eine leere Auswahl <b>reicht</b> hier — der Cursor im Absatz wählt ihn.</summary>
    [Fact]
    public void Absatzformat_wirkt_auch_bei_leerer_Auswahl()
    {
        var doc = Dok(Text("abc"));

        TdFormatEdit.Absatz(doc, Bei(doc, 0, 1), f => f.Alignment = TdAlign.Center)!.Anwenden();

        Assert.Equal("¶[Center]<:abc>\n", Abbild(doc));
    }

    /// <summary>Berührt genügt: eine Auswahl über die Absatzgrenze meint beide Absätze.</summary>
    [Fact]
    public void Eine_beruehrte_Absatzgrenze_zaehlt_beide()
    {
        var doc = Dok(Text("eins"), Text("zwei"), Text("drei"));

        TdFormatEdit.Absatz(doc, Von(doc, 0, 4, 1, 0), f => f.Alignment = TdAlign.Right)!
            .Anwenden();

        Assert.Equal("¶[Right]<:eins>\n¶[Right]<:zwei>\n¶<:drei>\n", Abbild(doc));
    }

    /// <summary>
    /// Die Stücke bleiben dieselben Objekte — am Text ändert sich nichts, und ihn zu verdoppeln
    /// hätte keinen Zweck. Der <b>Absatz</b> dagegen muss ein neuer sein, sonst hat die
    /// Rücknahme nichts, worauf sie zurückgreifen könnte.
    /// </summary>
    [Fact]
    public void Absatzformat_ersetzt_den_Absatz_und_nicht_seine_Stuecke()
    {
        var stueck = new TdRun("abc");
        var alt = Abs(stueck);
        var doc = Dok(alt);

        TdFormatEdit.Absatz(doc, Bei(doc, 0, 0), f => f.Alignment = TdAlign.Center)!.Anwenden();

        var neu = (TdParagraph)doc.Sections[0].Blocks[0];
        Assert.NotSame(alt, neu);
        Assert.Same(stueck, neu.Inlines[0]);
        Assert.Null(alt.Format.Alignment);
    }

    /// <summary>Auch hier muss die Rücknahme vollständig zurückführen.</summary>
    [Fact]
    public void Ruecknahme_des_Absatzformats_fuehrt_zurueck()
    {
        var doc = Dok(Text("eins"), Text("zwei"));
        string vorher = Abbild(doc);

        var aenderung = TdFormatEdit.Absatz(doc, Von(doc, 0, 0, 1, 4), f => f.SpaceAfterPt = 12)!;
        aenderung.Anwenden();
        aenderung.Zuruecknehmen();

        Assert.Equal(vorher, Abbild(doc));
        Assert.Null(((TdParagraph)doc.Sections[0].Blocks[0]).Format.SpaceAfterPt);
    }

    // ==================== Was zeigt die Auswahl? ====================

    /// <summary>Einheitlich fett: der Knopf ist gedrückt.</summary>
    [Fact]
    public void Gemeinsam_meldet_fett_wenn_alles_fett_ist()
    {
        var doc = Dok(Abs(Fett("abc"), Fett("def")));

        Assert.True(TdFormatEdit.Gemeinsam(doc, Von(doc, 0, 0, 0, 6)).Bold);
    }

    /// <summary>
    /// <b>Die dritte Antwort, für die es diese Auskunft gibt.</b> Über einer Auswahl aus fettem
    /// und magerem Text ist der Knopf weder gedrückt noch nicht gedrückt — <c>null</c> heißt
    /// hier „uneinig" und nicht „keine Abweichung".
    /// </summary>
    [Fact]
    public void Gemeinsam_meldet_Uneinigkeit_als_null()
    {
        var doc = Dok(Abs(Fett("abc"), new TdRun("def")));

        Assert.Null(TdFormatEdit.Gemeinsam(doc, Von(doc, 0, 0, 0, 6)).Bold);
    }

    /// <summary>
    /// Eine leere Auswahl antwortet mit dem, was das nächste getippte Zeichen bekäme — wer
    /// hinter ein fettes Wort klickt, schreibt fett weiter und soll das am Knopf sehen.
    /// </summary>
    [Fact]
    public void Gemeinsam_antwortet_bei_leerer_Auswahl_mit_dem_Erbe_links()
    {
        var doc = Dok(Abs(Fett("abc"), new TdRun("def")));

        Assert.True(TdFormatEdit.Gemeinsam(doc, Bei(doc, 0, 3)).Bold);
        Assert.False(TdFormatEdit.Gemeinsam(doc, Bei(doc, 0, 6)).Bold);
    }

    /// <summary>
    /// Was am Absatz steht, zählt für seine Stücke mit — sonst meldete eine Überschrift, deren
    /// Fett am Absatz hängt, „nicht fett" (<c>TdParagraph.CharFormat</c>).
    /// </summary>
    [Fact]
    public void Gemeinsam_loest_ueber_den_Absatz_hinweg_auf()
    {
        var absatz = Abs(new TdRun("abc"));
        absatz.CharFormat.Bold = true;
        var doc = Dok(absatz);

        Assert.True(TdFormatEdit.Gemeinsam(doc, Von(doc, 0, 0, 0, 3)).Bold);
    }

    /// <summary>Dasselbe für Absätze: zwei verschiedene Ausrichtungen ergeben keine.</summary>
    [Fact]
    public void GemeinsamerAbsatz_meldet_Uneinigkeit_als_null()
    {
        var links = Text("eins");
        var rechts = Text("zwei");
        rechts.Format.Alignment = TdAlign.Center;
        var doc = Dok(links, rechts);

        Assert.Null(TdFormatEdit.GemeinsamerAbsatz(doc, Von(doc, 0, 0, 1, 4)).Alignment);
        Assert.Equal(TdAlign.Center, TdFormatEdit.GemeinsamerAbsatz(doc, Bei(doc, 1, 0)).Alignment);
    }
}

/// <summary>Kleiner Lesehelfer für die Wächter oben.</summary>
internal static class FormatSetzenTestsHilfe
{
    public static bool? Bold(this TdRun run) => run.Format.Bold;
}
