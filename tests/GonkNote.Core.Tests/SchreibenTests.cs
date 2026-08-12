using System.Text;
using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdEdit"/>, <see cref="TdChange"/> und <see cref="TdFragment"/> — Schritt 2 des
/// Schreibens (HANDOFF §6): was sich ändert, wenn man tippt.
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Hier wird zum ersten Mal am Dokument des Nutzers
/// <em>geschrieben</em>. Ein Fehler in Schritt 1 setzte den Cursor an die falsche Stelle; ein
/// Fehler hier löscht etwas, das nicht wiederkommt. Der teuerste Fall ist dabei nicht die
/// falsche Änderung, sondern die **Gegenbewegung, die nicht ganz zurückführt** — denn die
/// merkt niemand beim Tippen, sondern erst, wenn er zweimal Strg+Z gedrückt hat und der Text
/// anders dasteht als vorher.
/// </para>
/// <para>
/// <b>Kein Umbruch und keine Schrift</b>, wie in Schritt 1: Diese Rechnung läuft auf jedem
/// Rechner gleich.
/// </para>
/// </summary>
public sealed class SchreibenTests
{
    // ==================== Hilfsmittel ====================

    private static TdDocument Dok(params TdParagraph[] absaetze)
    {
        var doc = new TdDocument();
        var abschnitt = new TdSection();
        abschnitt.Blocks.AddRange(absaetze);
        doc.Sections.Add(abschnitt);
        return doc;
    }

    private static TdParagraph Abs(params TdInline[] stuecke) => new(stuecke);

    private static TdParagraph Text(string text) => new(text);

    private static TdRun Fett(string text) => new(text, new TdCharFormat { Bold = true });

    /// <summary>Der Cursor an einer Stelle des ersten Absatzes.</summary>
    private static TdSelection Bei(TdDocument doc, int absatz, int linear)
    {
        var a = TdCursor.AbsatzAn(doc, absatz)!;
        return new TdSelection(TdCursor.AusLinear(a, absatz, linear));
    }

    private static TdSelection Von(TdDocument doc, int absatzA, int a, int absatzB, int b) =>
        new(TdCursor.AusLinear(TdCursor.AbsatzAn(doc, absatzA)!, absatzA, a),
            TdCursor.AusLinear(TdCursor.AbsatzAn(doc, absatzB)!, absatzB, b));

    /// <summary>
    /// Ein vollständiges Abbild des Dokuments — Absatzformate, Stückgrenzen, Zeichenformate
    /// und Verweisziele.
    ///
    /// <para>
    /// <b>Der Klartext allein reicht als Vergleich nicht.</b> Eine Rücknahme, die den Text
    /// wiederherstellt, aber die Stücke anders schneidet, das Fett verliert oder aus einem
    /// Verweis zwei macht, wäre damit grün. Genau diese Fehler sind hier die teuren.
    /// </para>
    /// </summary>
    private static string Abbild(TdDocument doc)
    {
        var sb = new StringBuilder();

        foreach (var absatz in doc.Paragraphs())
        {
            sb.Append('¶');
            if (absatz.List is { } liste) sb.Append($"[L{liste.ListId}.{liste.Level}]");
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
                case TdLineBreak:
                    sb.Append("<umbruch>");
                    break;
                case TdField feld:
                    sb.Append($"<feld:{feld.Kind}>");
                    break;
                case TdImage bild:
                    sb.Append($"<bild:{bild.BlobId:N}>");
                    break;
                case TdHyperlink verweis:
                    sb.Append($"<verweis:{verweis.Target}:");
                    Stuecke(sb, verweis.Inlines);
                    sb.Append('>');
                    break;
                default:
                    sb.Append("<?>");
                    break;
            }
        }
    }

    private static string Marke(TdCharFormat format) =>
        (format.Bold == true ? "f" : "") + (format.Italic == true ? "k" : "");

    private static string Klartext(TdDocument doc) =>
        TdCursor.Text(doc, TdSelection.Alles(doc));

    // ==================== Einfügen ====================

    /// <summary>Der einfachste Fall — und der häufigste.</summary>
    [Fact]
    public void Ein_getipptes_Zeichen_steht_danach_im_Text()
    {
        var doc = Dok(Text("ab"));

        var aenderung = TdEdit.Tippen(doc, Bei(doc, 0, 1), "X")!;
        var danach = aenderung.Anwenden();

        Assert.Equal("aXb", Klartext(doc));
        Assert.Equal(2, TdCursor.Linear(TdCursor.AbsatzAn(doc, 0)!, danach.Focus));
        Assert.True(danach.IsEmpty);
    }

    /// <summary>
    /// **Die Word-Regel:** ein eingefügtes Zeichen erbt das Format links davon. Daran hing in
    /// §4.30 die Entscheidung für die linke kanonische Schreibweise — hier wird sie eingelöst.
    /// </summary>
    [Fact]
    public void Ein_getipptes_Zeichen_erbt_das_Format_links_davon()
    {
        var doc = Dok(Abs(Fett("fett"), new TdRun("mager")));

        // Genau auf der Stückgrenze: links fett, rechts mager.
        TdEdit.Tippen(doc, Bei(doc, 0, 4), "X")!.Anwenden();

        Assert.Equal("¶<f:fettX><:mager>\n", Abbild(doc));
    }

    /// <summary>Und rechts davon erbt es nicht — sonst gäbe es kein Ende einer Auszeichnung.</summary>
    [Fact]
    public void Ein_Zeichen_hinter_der_Grenze_erbt_das_rechte_Format_nicht()
    {
        var doc = Dok(Abs(Fett("fett"), new TdRun("mager")));

        TdEdit.Tippen(doc, Bei(doc, 0, 5), "X")!.Anwenden();

        Assert.Equal("¶<f:fett><:mXager>\n", Abbild(doc));
    }

    /// <summary>
    /// Am Absatzanfang gibt es keinen linken Nachbarn — dort gilt das Format des ersten
    /// Stücks. Alles andere ergäbe ein Zeichen ohne Auszeichnung vor einer fetten Überschrift.
    /// </summary>
    [Fact]
    public void Am_Absatzanfang_erbt_das_Zeichen_nach_rechts()
    {
        var doc = Dok(Abs(Fett("fett")));

        TdEdit.Tippen(doc, Bei(doc, 0, 0), "X")!.Anwenden();

        Assert.Equal("¶<f:Xfett>\n", Abbild(doc));
    }

    /// <summary>
    /// In einem leeren Absatz erbt es **nichts** — und das ist wichtig: Eine Abweichung, die
    /// nur das wiederholt, was der Absatz ohnehin sagt, überlebt jede spätere Änderung an der
    /// Überschrift (§4.14).
    /// </summary>
    [Fact]
    public void In_einem_leeren_Absatz_traegt_das_Zeichen_keine_eigene_Abweichung()
    {
        var doc = Dok(new TdParagraph { CharFormat = new TdCharFormat { Bold = true } });

        TdEdit.Tippen(doc, Bei(doc, 0, 0), "X")!.Anwenden();

        var run = (TdRun)TdCursor.AbsatzAn(doc, 0)!.Inlines[0];
        Assert.True(run.Format.IstLeer);
    }

    /// <summary>Tippen mit ausgewähltem Text ersetzt ihn — wie überall.</summary>
    [Fact]
    public void Tippen_ersetzt_eine_Auswahl()
    {
        var doc = Dok(Text("abcdef"));

        TdEdit.Tippen(doc, Von(doc, 0, 1, 0, 4), "X")!.Anwenden();

        Assert.Equal("aXef", Klartext(doc));
    }

    /// <summary>
    /// Ein <c>\n</c> im eingegebenen Text wird zur **Absatzmarke** und nicht zu einem Zeichen
    /// im Lauf. Es kommt aus der Zwischenablage und von Eingabemethoden herein.
    /// </summary>
    [Fact]
    public void Zeilenumbrueche_im_eingefuegten_Text_werden_zu_Absaetzen()
    {
        var doc = Dok(Text("ab"));

        var danach = TdEdit.Tippen(doc, Bei(doc, 0, 1), "X\r\nY")!.Anwenden();

        Assert.Equal("aX\nYb", Klartext(doc));
        Assert.Equal(2, TdCursor.Absaetze(doc).Count);
        Assert.Equal(1, danach.Focus.Paragraph);
    }

    /// <summary>Ein leerer Text ist keine Änderung — und darf keinen Rückgängig-Schritt kosten.</summary>
    [Fact]
    public void Ein_leerer_Text_ergibt_keine_Aenderung()
    {
        var doc = Dok(Text("ab"));

        Assert.Null(TdEdit.Tippen(doc, Bei(doc, 0, 1), ""));
        Assert.Null(TdEdit.Loeschen(doc, Bei(doc, 0, 1)));
    }

    // ==================== Löschen ====================

    /// <summary>Rücktaste nimmt das Zeichen links.</summary>
    [Fact]
    public void Die_Ruecktaste_loescht_links()
    {
        var doc = Dok(Text("abc"));

        TdEdit.Rueckwaerts(doc, Bei(doc, 0, 2))!.Anwenden();

        Assert.Equal("ac", Klartext(doc));
    }

    /// <summary>Entf nimmt das Zeichen rechts.</summary>
    [Fact]
    public void Entfernen_loescht_rechts()
    {
        var doc = Dok(Text("abc"));

        TdEdit.Vorwaerts(doc, Bei(doc, 0, 1))!.Anwenden();

        Assert.Equal("ac", Klartext(doc));
    }

    /// <summary>An den Rändern des Dokuments gibt es nichts zu löschen — und keine Ausnahme.</summary>
    [Fact]
    public void An_den_Raendern_loescht_niemand_ins_Leere()
    {
        var doc = Dok(Text("ab"));

        Assert.Null(TdEdit.Rueckwaerts(doc, new TdSelection(TdCursor.Anfang(doc))));
        Assert.Null(TdEdit.Vorwaerts(doc, new TdSelection(TdCursor.Ende(doc))));
    }

    /// <summary>
    /// **Absätze verbinden, ohne dass es dafür einen Handgriff gäbe.** Die Rücktaste am
    /// Absatzanfang wählt über die Absatzmarke hinweg aus, und eine Ersetzung über zwei
    /// Absätze *ist* das Verbinden.
    /// </summary>
    [Fact]
    public void Die_Ruecktaste_am_Absatzanfang_verbindet_die_Absaetze()
    {
        var doc = Dok(Text("ab"), Text("cd"));

        TdEdit.Rueckwaerts(doc, Bei(doc, 1, 0))!.Anwenden();

        Assert.Single(TdCursor.Absaetze(doc));
        Assert.Equal("abcd", Klartext(doc));
    }

    /// <summary>
    /// Beim Verbinden führt der **obere** Absatz — wie in Word. Sonst zöge ein gelöschter
    /// Zeilenumbruch die Gestalt des nächsten Absatzes nach oben.
    /// </summary>
    [Fact]
    public void Beim_Verbinden_behaelt_der_obere_Absatz_sein_Format()
    {
        var doc = Dok(
            new TdParagraph("ab") { Format = new TdParaFormat { Alignment = TdAlign.Center } },
            new TdParagraph("cd") { Format = new TdParaFormat { Alignment = TdAlign.Right } });

        TdEdit.Rueckwaerts(doc, Bei(doc, 1, 0))!.Anwenden();

        Assert.Equal(TdAlign.Center, TdCursor.AbsatzAn(doc, 0)!.Format.Alignment);
    }

    /// <summary>Eine Auswahl über mehrere Absätze löschen lässt einen Absatz übrig.</summary>
    [Fact]
    public void Eine_Auswahl_ueber_Absaetze_hinweg_verschmilzt_sie()
    {
        var doc = Dok(Text("abc"), Text("def"), Text("ghi"));

        TdEdit.Loeschen(doc, Von(doc, 0, 2, 2, 1))!.Anwenden();

        Assert.Single(TdCursor.Absaetze(doc));
        Assert.Equal("abhi", Klartext(doc));
    }

    /// <summary>
    /// Ein Emoji verschwindet in **einem** Schritt. Wer halbe UTF-16-Einheiten löscht, lässt
    /// eine kaputte Ersatzzeichenkette stehen (§4.30).
    /// </summary>
    [Fact]
    public void Ein_Emoji_wird_in_einem_Schritt_geloescht()
    {
        var doc = Dok(Text("a\U0001F600b"));

        TdEdit.Rueckwaerts(doc, Bei(doc, 0, 3))!.Anwenden();

        Assert.Equal("ab", Klartext(doc));
    }

    /// <summary>Dasselbe für ein zusammengesetztes „ä" — Nummer statt Buchstabe, siehe §4.30.</summary>
    [Fact]
    public void Ein_zusammengesetztes_Zeichen_wird_in_einem_Schritt_geloescht()
    {
        var doc = Dok(Text("a" + (char)0x0308 + "b"));

        TdEdit.Rueckwaerts(doc, Bei(doc, 0, 2))!.Anwenden();

        Assert.Equal("b", Klartext(doc));
    }

    /// <summary>Ein Feld ist unteilbar — es geht ganz oder gar nicht.</summary>
    [Fact]
    public void Ein_Feld_wird_in_einem_Schritt_geloescht()
    {
        var doc = Dok(Abs(new TdRun("a"), new TdField(TdFieldKind.PageNumber), new TdRun("b")));

        TdEdit.Rueckwaerts(doc, Bei(doc, 0, 2))!.Anwenden();

        Assert.Equal("¶<:ab>\n", Abbild(doc));
    }

    // ==================== Absatz teilen und umbrechen ====================

    /// <summary>Eingabe teilt den Absatz; die Schreibmarke steht am Anfang der zweiten Hälfte.</summary>
    [Fact]
    public void Der_Absatz_wird_an_der_Stelle_geteilt()
    {
        var doc = Dok(Text("abcd"));

        var danach = TdEdit.AbsatzTeilen(doc, Bei(doc, 0, 2))!.Anwenden();

        Assert.Equal(2, TdCursor.Absaetze(doc).Count);
        Assert.Equal("ab\ncd", Klartext(doc));
        Assert.Equal(new TdPosition(1, 0, 0), danach.Focus);
    }

    /// <summary>
    /// Beide Hälften erben Absatzformat und Listenzugehörigkeit — sonst fiele der zweite
    /// Listenpunkt bei jeder Eingabetaste aus der Liste heraus.
    /// </summary>
    [Fact]
    public void Beide_Haelften_erben_Format_und_Listenzugehoerigkeit()
    {
        var doc = Dok(new TdParagraph("abcd")
        {
            Format = new TdParaFormat { Alignment = TdAlign.Center },
            List = new TdListRef(1, 0),
        });

        TdEdit.AbsatzTeilen(doc, Bei(doc, 0, 2))!.Anwenden();

        Assert.Equal("¶[L1.0][Center]<:ab>\n¶[L1.0][Center]<:cd>\n", Abbild(doc));
    }

    /// <summary>
    /// Umschalt+Eingabe bleibt **im** Absatz — der Unterschied ist im DOCX sichtbar
    /// (<c>w:br</c> statt eines neuen Absatzes) und an den Absatzabständen.
    /// </summary>
    [Fact]
    public void Ein_Zeilenumbruch_bleibt_im_Absatz()
    {
        var doc = Dok(Text("abcd"));

        TdEdit.Zeilenumbruch(doc, Bei(doc, 0, 2))!.Anwenden();

        Assert.Single(TdCursor.Absaetze(doc));
        Assert.Equal("¶<:ab><umbruch><:cd>\n", Abbild(doc));
    }

    // ==================== Verweise ====================

    /// <summary>
    /// **Wer in einem Verweis tippt, bleibt darin** — und der Verweis bleibt einer. Ohne das
    /// stünden nach dem ersten Tastendruck zwei Verweise mit demselben Ziel da, und der Text
    /// dazwischen gehörte zu keinem.
    /// </summary>
    [Fact]
    public void Wer_in_einem_Verweis_tippt_bleibt_darin()
    {
        var doc = Dok(Abs(new TdRun("a"), new TdHyperlink("#z", new TdRun("bc"))));

        TdEdit.Tippen(doc, Bei(doc, 0, 2), "X")!.Anwenden();

        Assert.Equal("¶<:a><verweis:#z:<:bXc>>\n", Abbild(doc));
    }

    /// <summary>
    /// Vor einem Verweis entsteht keiner: dort ist die Naht kein Schnitt, und der neue Text
    /// gehört zum Stück links davon.
    /// </summary>
    [Fact]
    public void Vor_einem_Verweis_bleibt_der_Text_draussen()
    {
        var doc = Dok(Abs(new TdRun("a"), new TdHyperlink("#z", new TdRun("bc"))));

        TdEdit.Tippen(doc, Bei(doc, 0, 1), "X")!.Anwenden();

        Assert.Equal("¶<:aX><verweis:#z:<:bc>>\n", Abbild(doc));
    }

    /// <summary>
    /// Und hinter einem Verweis auch nicht — **ein Verweis wächst, wenn man in ihm schreibt,
    /// nicht, wenn man dahinter weiterschreibt.**
    /// </summary>
    [Fact]
    public void Hinter_einem_Verweis_waechst_er_nicht_mit()
    {
        var doc = Dok(Abs(new TdHyperlink("#z", new TdRun("bc"))));

        TdEdit.Tippen(doc, Bei(doc, 0, 2), "X")!.Anwenden();

        Assert.Equal("¶<verweis:#z:<:bc>><:X>\n", Abbild(doc));
    }

    /// <summary>Löschen mitten im Verweis lässt einen Verweis übrig, nicht zwei.</summary>
    [Fact]
    public void Loeschen_im_Verweis_laesst_einen_Verweis_uebrig()
    {
        var doc = Dok(Abs(new TdHyperlink("#z", new TdRun("abcd"))));

        TdEdit.Loeschen(doc, Von(doc, 0, 1, 0, 3))!.Anwenden();

        Assert.Equal("¶<verweis:#z:<:ad>>\n", Abbild(doc));
    }

    /// <summary>Wird sein ganzer Text gelöscht, verschwindet der Verweis mit.</summary>
    [Fact]
    public void Ein_Verweis_ohne_Text_bleibt_nicht_stehen()
    {
        var doc = Dok(Abs(new TdRun("a"), new TdHyperlink("#z", new TdRun("bc"))));

        TdEdit.Loeschen(doc, Von(doc, 0, 1, 0, 3))!.Anwenden();

        Assert.Equal("¶<:a>\n", Abbild(doc));
    }

    // ==================== Aufräumen ====================

    /// <summary>
    /// **Ein Absatz zerfällt nicht mit jedem Tastendruck.** Jede Einfügung schneidet ein Stück
    /// auf; ohne Zusammenlegen stünde ein getippter Satz am Ende in dreihundert Stücken — und
    /// im DOCX ein Lauf je Zeichen.
    /// </summary>
    [Fact]
    public void Gleichformatige_Nachbarn_werden_zusammengelegt()
    {
        var doc = Dok(Text("ab"));

        var auswahl = Bei(doc, 0, 1);
        for (int i = 0; i < 5; i++)
            auswahl = TdEdit.Tippen(doc, auswahl, "X")!.Anwenden();

        Assert.Equal("¶<:aXXXXXb>\n", Abbild(doc));
        Assert.Single(TdCursor.AbsatzAn(doc, 0)!.Inlines);
    }

    /// <summary>Was verschieden aussieht, bleibt getrennt.</summary>
    [Fact]
    public void Verschiedene_Formate_werden_nicht_zusammengelegt()
    {
        var doc = Dok(Abs(Fett("ab"), new TdRun("cd")));

        TdEdit.Tippen(doc, Bei(doc, 0, 3), "X")!.Anwenden();

        Assert.Equal("¶<f:ab><:cXd>\n", Abbild(doc));
    }

    /// <summary>Ein leer gewordenes Stück bleibt nicht als unsichtbarer Rest stehen.</summary>
    [Fact]
    public void Ein_leer_geloeschtes_Stueck_verschwindet()
    {
        var doc = Dok(Abs(new TdRun("ab"), Fett("XY"), new TdRun("cd")));

        TdEdit.Loeschen(doc, Von(doc, 0, 2, 0, 4))!.Anwenden();

        Assert.Equal("¶<:abcd>\n", Abbild(doc));
        Assert.Single(TdCursor.AbsatzAn(doc, 0)!.Inlines);
    }

    // ==================== Die Gegenbewegung ====================

    /// <summary>
    /// **Der Wächter, der am meisten wert ist.** Jeder Handgriff einzeln: anwenden, zurück —
    /// und das Dokument muss *vollständig* dastehen wie vorher, samt Stückgrenzen, Formaten
    /// und Verweiszielen. Der Klartext allein würde die teuren Fehler durchlassen.
    /// </summary>
    [Theory]
    [InlineData("tippen")]
    [InlineData("tippen-mehrzeilig")]
    [InlineData("ruecktaste")]
    [InlineData("ruecktaste-verbinden")]
    [InlineData("entfernen")]
    [InlineData("auswahl-loeschen")]
    [InlineData("auswahl-ueberschreiben")]
    [InlineData("teilen")]
    [InlineData("umbruch")]
    [InlineData("verweis-tippen")]
    [InlineData("verweis-loeschen")]
    [InlineData("feld-loeschen")]
    [InlineData("leerer-absatz")]
    [InlineData("liste-teilen")]
    [InlineData("liste-verbinden")]
    [InlineData("alles-ersetzen")]
    public void Jede_Aenderung_fuehrt_vollstaendig_zurueck(string handgriff)
    {
        var doc = Beispiel();
        string vorher = Abbild(doc);

        var aenderung = Handgriff(doc, handgriff);
        Assert.NotNull(aenderung);

        aenderung.Anwenden();
        Assert.NotEqual(vorher, Abbild(doc));

        aenderung.Zuruecknehmen();
        Assert.Equal(vorher, Abbild(doc));
    }

    /// <summary>
    /// Und wieder vor: Rückgängig und Wiederherstellen müssen beliebig oft im Wechsel
    /// dasselbe ergeben — sonst driftet ein Dokument über eine lange Sitzung weg.
    /// </summary>
    [Theory]
    [InlineData("tippen")]
    [InlineData("ruecktaste-verbinden")]
    [InlineData("teilen")]
    [InlineData("verweis-tippen")]
    [InlineData("alles-ersetzen")]
    public void Zurueck_und_wieder_vor_ergibt_dasselbe(string handgriff)
    {
        var doc = Beispiel();

        var aenderung = Handgriff(doc, handgriff)!;
        aenderung.Anwenden();
        string danach = Abbild(doc);

        for (int i = 0; i < 3; i++)
        {
            aenderung.Zuruecknehmen();
            aenderung.Anwenden();
        }

        Assert.Equal(danach, Abbild(doc));
    }

    /// <summary>
    /// Die Schreibmarke kommt mit zurück. Ein Rückgängig, das den Text wiederherstellt und den
    /// Cursor woanders stehen lässt, zwingt den Nutzer, die Stelle wiederzufinden.
    /// </summary>
    [Fact]
    public void Die_Ruecknahme_bringt_die_Auswahl_von_vorher_zurueck()
    {
        var doc = Dok(Text("abcdef"));
        var auswahl = Von(doc, 0, 1, 0, 4);

        var aenderung = TdEdit.Tippen(doc, auswahl, "X")!;
        aenderung.Anwenden();

        Assert.Equal(auswahl, aenderung.Zuruecknehmen());
    }

    /// <summary>
    /// <see cref="TdChange.Gegenbewegung"/> ist dieselbe Änderung mit vertauschten Rollen —
    /// kein zweiter Weg, der abweichen könnte.
    /// </summary>
    [Fact]
    public void Die_Gegenbewegung_ist_die_Aenderung_andersherum()
    {
        var doc = Dok(Text("abc"));

        var aenderung = TdEdit.Tippen(doc, Bei(doc, 0, 1), "X")!;
        aenderung.Anwenden();
        string danach = Abbild(doc);

        var gegen = aenderung.Gegenbewegung;
        gegen.Anwenden();
        Assert.Equal("¶<:abc>\n", Abbild(doc));

        gegen.Zuruecknehmen();
        Assert.Equal(danach, Abbild(doc));
    }

    /// <summary>
    /// **Eine Änderung, die nicht mehr passt, wirft.** Sie an einer Stelle auszuführen, an der
    /// inzwischen etwas anderes steht, ersetzte fremde Absätze — und der Verlust fiele erst
    /// viel später auf. Laut ist hier besser als still (dieselbe Regel wie beim unbekannten
    /// Elementtyp, §7).
    /// </summary>
    [Fact]
    public void Eine_Aenderung_an_der_falschen_Stelle_wirft()
    {
        var doc = Dok(Text("abc"));

        var aenderung = TdEdit.Tippen(doc, Bei(doc, 0, 1), "X")!;
        aenderung.Anwenden();

        Assert.Throws<InvalidOperationException>(() => aenderung.Anwenden());
    }

    /// <summary>
    /// **Das Versprechen, auf dem die Rücknahme steht:** Eine Änderung fasst die vorhandenen
    /// Absätze nicht an, sondern baut neue. Wer das später bricht, macht jeden gemerkten
    /// Schritt still falsch.
    /// </summary>
    [Fact]
    public void Eine_Aenderung_veraendert_den_alten_Absatz_nicht()
    {
        var alt = Abs(Fett("ab"), new TdRun("cd"));
        var doc = Dok(alt);

        TdEdit.Tippen(doc, Bei(doc, 0, 3), "X")!.Anwenden();

        Assert.Equal("abcd", alt.PlainText());
        Assert.Equal(2, alt.Inlines.Count);
        Assert.NotSame(alt, TdCursor.AbsatzAn(doc, 0));
    }

    // ==================== Tabellen ====================

    /// <summary>In einer Tabellenzelle wird geschrieben wie überall — sie ist nur ein Ort.</summary>
    [Fact]
    public void In_einer_Tabellenzelle_laesst_sich_schreiben()
    {
        var doc = MitTabelle();

        TdEdit.Tippen(doc, Bei(doc, 1, 5), "X")!.Anwenden();

        Assert.Equal("davor\nZelleX\ndanach", Klartext(doc));
    }

    /// <summary>
    /// **Die benannte Lücke:** Eine Auswahl, die halb in einer Tabelle steht, wird abgelehnt
    /// statt geraten. Was aus der Tabelle würde, ist eine eigene Entscheidung — und ein
    /// geratenes Ergebnis wäre stiller Datenverlust.
    /// </summary>
    [Fact]
    public void Ueber_eine_Tabellengrenze_hinweg_wird_nicht_bearbeitet()
    {
        var doc = MitTabelle();

        Assert.Null(TdEdit.Loeschen(doc, Von(doc, 0, 2, 1, 2)));
        Assert.Null(TdEdit.Rueckwaerts(doc, Bei(doc, 1, 0)));
        Assert.Null(TdEdit.Tippen(doc, Von(doc, 1, 2, 2, 2), "X"));
    }

    /// <summary>
    /// Ein Seitenumbruch dazwischen darf mit: Wer am Anfang des Absatzes dahinter die
    /// Rücktaste drückt, meint ihn. Und die Rücknahme bringt ihn zurück.
    /// </summary>
    [Fact]
    public void Ein_Seitenumbruch_dazwischen_faellt_mit_und_kommt_zurueck()
    {
        var doc = new TdDocument();
        var abschnitt = new TdSection();
        abschnitt.Blocks.Add(Text("ab"));
        abschnitt.Blocks.Add(new TdPageBreak());
        abschnitt.Blocks.Add(Text("cd"));
        doc.Sections.Add(abschnitt);

        var aenderung = TdEdit.Rueckwaerts(doc, Bei(doc, 1, 0))!;
        aenderung.Anwenden();

        Assert.Equal("abcd", Klartext(doc));
        Assert.Empty(doc.Blocks().OfType<TdPageBreak>());

        aenderung.Zuruecknehmen();
        Assert.Single(doc.Blocks().OfType<TdPageBreak>());
    }

    // ==================== Ganze Sitzungen ====================

    /// <summary>
    /// Eine Folge von Handgriffen, hintereinander ausgeführt und in umgekehrter Reihenfolge
    /// zurückgenommen — **so, wie ein Rückgängig-Stapel es tun wird** (Schritt 3). Das ist der
    /// Fall, in dem sich ein kleiner Fehler in der Gegenbewegung aufsummiert.
    /// </summary>
    [Fact]
    public void Eine_ganze_Folge_von_Handgriffen_faehrt_vollstaendig_zurueck()
    {
        var doc = Beispiel();
        string vorher = Abbild(doc);

        var stapel = new List<TdChange>();
        var auswahl = new TdSelection(TdCursor.Anfang(doc));

        foreach (string name in new[]
                 { "tippen", "teilen", "tippen", "ruecktaste", "umbruch", "tippen" })
        {
            var aenderung = Handgriff(doc, name, auswahl);
            if (aenderung is null) continue;

            auswahl = aenderung.Anwenden();
            stapel.Add(aenderung);
        }

        Assert.NotEmpty(stapel);
        Assert.NotEqual(vorher, Abbild(doc));

        for (int i = stapel.Count - 1; i >= 0; i--) stapel[i].Zuruecknehmen();

        Assert.Equal(vorher, Abbild(doc));
    }

    /// <summary>
    /// Ein ganzes Dokument leer tippen und wieder zurück. **Mit Schrittgrenze** — dieselbe
    /// Vorsorge wie in §4.30: Ein Wächter, der nicht fertig wird, meldet nichts, er blockiert
    /// nur die Suite.
    /// </summary>
    [Fact]
    public void Alles_wegloeschen_endet_bei_einem_leeren_Absatz()
    {
        var doc = Beispiel();
        int grenze = TdCursor.Absaetze(doc).Sum(TdCursor.Laenge) + TdCursor.Absaetze(doc).Count + 5;

        var stapel = new List<TdChange>();
        for (int i = 0; i < grenze; i++)
        {
            var aenderung = TdEdit.Rueckwaerts(doc, new TdSelection(TdCursor.Ende(doc)));
            if (aenderung is null) break;

            aenderung.Anwenden();
            stapel.Add(aenderung);
        }

        Assert.True(stapel.Count < grenze, $"Nach {grenze} Rücktasten ist noch etwas übrig.");
        Assert.Equal("", Klartext(doc));
        Assert.Single(TdCursor.Absaetze(doc));
    }

    // ==================== Beispiele ====================

    /// <summary>
    /// Ein Dokument mit allem, was einer Änderung im Weg stehen kann: mehrere Stücke, ein
    /// Zeichenformat, ein Feld, ein Verweis, ein leerer Absatz und eine Liste.
    /// </summary>
    private static TdDocument Beispiel() => Dok(
        new TdParagraph([new TdRun("Hallo "), Fett("Welt"), new TdRun("!")])
        {
            Format = new TdParaFormat { Alignment = TdAlign.Center },
        },
        Abs(new TdRun("Seite "), new TdField(TdFieldKind.PageNumber)),
        Abs(new TdRun("siehe "), new TdHyperlink("#ziel", new TdRun("dort"))),
        Text(""),
        new TdParagraph("Punkt") { List = new TdListRef(1, 0) });

    private static TdDocument MitTabelle()
    {
        var zelle = new TdTableCell();
        zelle.Blocks.Add(Text("Zelle"));

        var zeile = new TdTableRow();
        zeile.Cells.Add(zelle);

        var tabelle = new TdTable();
        tabelle.Rows.Add(zeile);

        var abschnitt = new TdSection();
        abschnitt.Blocks.Add(Text("davor"));
        abschnitt.Blocks.Add(tabelle);
        abschnitt.Blocks.Add(Text("danach"));

        var doc = new TdDocument();
        doc.Sections.Add(abschnitt);
        return doc;
    }

    /// <summary>Die Handgriffe der beiden Theorien, an einer Stelle beschrieben.</summary>
    private static TdChange? Handgriff(TdDocument doc, string name, TdSelection? bei = null)
    {
        var stelle = bei ?? Bei(doc, 0, 6);

        return name switch
        {
            "tippen" => TdEdit.Tippen(doc, stelle, "X"),
            "tippen-mehrzeilig" => TdEdit.Tippen(doc, stelle, "X\nY\nZ"),
            "ruecktaste" => TdEdit.Rueckwaerts(doc, stelle),
            "ruecktaste-verbinden" => TdEdit.Rueckwaerts(doc, Bei(doc, 1, 0)),
            "entfernen" => TdEdit.Vorwaerts(doc, stelle),
            "auswahl-loeschen" => TdEdit.Loeschen(doc, Von(doc, 0, 3, 2, 4)),
            "auswahl-ueberschreiben" => TdEdit.Tippen(doc, Von(doc, 0, 3, 1, 2), "X"),
            "teilen" => TdEdit.AbsatzTeilen(doc, stelle),
            "umbruch" => TdEdit.Zeilenumbruch(doc, stelle),
            "verweis-tippen" => TdEdit.Tippen(doc, Bei(doc, 2, 8), "X"),
            "verweis-loeschen" => TdEdit.Loeschen(doc, Von(doc, 2, 7, 2, 9)),
            "feld-loeschen" => TdEdit.Rueckwaerts(doc, Bei(doc, 1, 7)),
            "leerer-absatz" => TdEdit.Tippen(doc, Bei(doc, 3, 0), "X"),
            "liste-teilen" => TdEdit.AbsatzTeilen(doc, Bei(doc, 4, 2)),
            "liste-verbinden" => TdEdit.Rueckwaerts(doc, Bei(doc, 4, 0)),
            "alles-ersetzen" => TdEdit.Tippen(doc, TdSelection.Alles(doc), "X"),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unbekannter Handgriff"),
        };
    }
}
