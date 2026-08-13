using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdHit"/> — Schritt 4 des Schreibens (HANDOFF §6): vom Papier ins Modell und
/// zurück.
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Hier treffen zwei Rechnungen aufeinander, die
/// auseinanderlaufen können: „welche Stelle gehört zu diesem Klick" und „wo steht diese
/// Stelle". Laufen sie auseinander, landet der Cursor beim Klicken woanders, als er danach
/// blinkt — und je weiter im Dokument, desto weiter daneben. Der teuerste Fall ist dabei nicht
/// der grobe Fehler, sondern der **um ein Zeichen**: Er sieht wie eine Ungenauigkeit der Maus
/// aus, bis jemand damit etwas löscht.
/// </para>
/// <para>
/// <b>Gemessen wird mit festen Maßen</b> — ein Zeichen 1 cm, eine Zeile 1 cm, wie in
/// <see cref="UmbruchTests"/>. Damit ist „der Cursor steht bei 7 cm" eine Behauptung, die
/// stimmt oder nicht, und der Test läuft unter Windows wie im Linux-Lauf der CI gleich.
/// </para>
/// </summary>
public sealed class SchreibmarkeTests
{
    // ==================== Hilfsmittel ====================

    /// <inheritdoc cref="UmbruchTests"/>
    private sealed class FesteMessung : ITdTextMeasure
    {
        public double WidthCm(string text, TdCharFormat format) => text.Length;

        public TdFontMetrics Metrics(TdCharFormat format) => new(0.8, 0.2, 1.0);
    }

    private static readonly FesteMessung Messung = new();

    /// <summary>Ein Blatt mit 1 cm Rand, auf das genau zehn Zeichen und zehn Zeilen passen.</summary>
    private static TdPageSetup Blatt(double breite = 10, double hoehe = 10) => new()
    {
        WidthCm = breite + 2,
        HeightCm = hoehe + 2,
        MarginLeftCm = 1,
        MarginRightCm = 1,
        MarginTopCm = 1,
        MarginBottomCm = 1,
    };

    private static TdDocument Dok(params TdBlock[] bloecke) => new()
    {
        DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 0 },
        Sections = { new TdSection(bloecke) { Page = Blatt() } },
    };

    private static TdLayoutResult Umbrechen(TdDocument doc) => TdLayout.Umbrechen(doc, Messung);

    /// <summary>Ein Klick auf Seite 0, in Zentimetern ab der oberen linken Ecke des Papiers.</summary>
    private static TdPosition Klick(TdDocument doc, double xCm, double yCm) =>
        TdHit.StelleAn(Umbrechen(doc), doc, Messung, 0, xCm, yCm);

    private static TdCaret Marke(TdDocument doc, int linear)
    {
        var absatz = TdCursor.AbsatzAn(doc, 0)!;
        var stelle = TdCursor.AusLinear(absatz, 0, linear);

        return TdHit.Schreibmarke(Umbrechen(doc), doc, Messung, stelle)
               ?? throw new InvalidOperationException($"Keine Schreibmarke für {stelle}.");
    }

    // ==================== Der Klick ====================

    /// <summary>
    /// **Auf die linke Hälfte eines Zeichens klicken heißt davor, auf die rechte dahinter.**
    /// Ohne diese Regel läge der Cursor bei jedem Klick um ein Zeichen zu weit links, und
    /// hinter das letzte Zeichen einer Zeile käme man gar nicht.
    /// </summary>
    [Theory]
    [InlineData(1.4, 0)]      // linke Hälfte des ersten Zeichens
    [InlineData(1.6, 1)]      // rechte Hälfte des ersten Zeichens
    [InlineData(3.4, 2)]
    [InlineData(3.6, 3)]
    public void Ein_Klick_trifft_die_naechste_Zeichengrenze(double xCm, int erwartet)
    {
        var doc = Dok(new TdParagraph("Hallo"));

        Assert.Equal(new TdPosition(0, 0, erwartet), Klick(doc, xCm, 1.5));
    }

    /// <summary>Rechts neben dem Text endet die Zeile — nicht der Cursor.</summary>
    [Fact]
    public void Ein_Klick_rechts_neben_den_Text_geht_ans_Zeilenende()
    {
        var doc = Dok(new TdParagraph("Hallo"));

        Assert.Equal(new TdPosition(0, 0, 5), Klick(doc, 9.5, 1.5));
    }

    /// <summary>Und links davor an den Anfang.</summary>
    [Fact]
    public void Ein_Klick_links_neben_den_Text_geht_an_den_Zeilenanfang()
    {
        var doc = Dok(new TdParagraph("Hallo"));

        Assert.Equal(TdPosition.Null, Klick(doc, 0.2, 1.5));
    }

    /// <summary>
    /// **Ein Klick ins Leere unter dem Text bleibt nicht ohne Antwort.** Der Nutzer hat ins
    /// Dokument geklickt; ein Cursor, der dabei stehen bleibt, sieht aus wie ein hängendes
    /// Programm.
    /// </summary>
    [Fact]
    public void Ein_Klick_unter_die_letzte_Zeile_findet_die_letzte_Zeile()
    {
        var doc = Dok(new TdParagraph("eins"), new TdParagraph("zwei"));

        Assert.Equal(new TdPosition(1, 0, 4), Klick(doc, 9.5, 9.0));
    }

    /// <summary>Die Seitenränder zählen mit — sonst träfe jeder Klick um sie daneben.</summary>
    [Fact]
    public void Die_Seitenraender_zaehlen_mit()
    {
        var doc = Dok(new TdParagraph("Hallo"));

        // Ohne den Rand läge 0,4 cm im ersten Zeichen; mit ihm liegt es links daneben.
        Assert.Equal(TdPosition.Null, Klick(doc, 0.4, 1.5));
        Assert.Equal(new TdPosition(0, 0, 1), Klick(doc, 1.6, 1.5));
    }

    /// <summary>Eine Seite, die es nicht gibt, liefert den Nullpunkt und wirft nicht.</summary>
    [Fact]
    public void Eine_Seite_die_es_nicht_gibt_liefert_den_Nullpunkt()
    {
        var doc = Dok(new TdParagraph("Hallo"));
        var umbruch = Umbrechen(doc);

        Assert.Equal(TdPosition.Null, TdHit.StelleAn(umbruch, doc, Messung, 7, 2, 2));
        Assert.Equal(TdPosition.Null, TdHit.StelleAn(umbruch, doc, Messung, -1, 2, 2));
    }

    // ==================== Die Stelle im Absatz ====================

    /// <summary>
    /// **Der eigentliche Fund dieses Schritts:** Der Umbruch wirft den Leerraum am Zeilenende
    /// weg — und die Stelle rückt mit. Wer die Textlängen der Läufe addierte, wäre ab der
    /// zweiten Zeile um jeden weggefallenen Zwischenraum verschoben.
    /// </summary>
    [Fact]
    public void Nach_einem_Umbruch_stimmt_die_Stelle_trotz_weggefallenem_Leerraum()
    {
        var doc = Dok(new TdParagraph("aaa bbb ccc"));
        var umbruch = Umbrechen(doc);

        var seite = Assert.Single(umbruch.Pages);
        Assert.Equal(2, seite.Lines.Count);

        // „ccc" fängt im Modell beim neunten Zeichen an — der Zwischenraum davor ist auf dem
        // Papier weg, im Absatz aber nicht.
        Assert.Equal(8, seite.Lines[1].Linear);
        Assert.Equal(8, seite.Lines[1].Runs[0].Linear);
        Assert.Equal("ccc", doc.PlainText()[8..]);

        Assert.Equal(new TdPosition(0, 0, 8), Klick(doc, 1.2, 2.5));
    }

    /// <summary>
    /// Ein Feld ist **einen** Schritt breit, auch wenn auf dem Papier mehr steht: links davon
    /// steht der Cursor davor, rechts dahinter — nie darin (§4.30).
    /// </summary>
    [Fact]
    public void Ein_Feld_wird_nicht_geteilt()
    {
        var doc = Dok(new TdParagraph([
            new TdRun("a"), new TdField(TdFieldKind.PageNumber), new TdRun("b")]));

        Assert.Equal(new TdPosition(0, 0, 1), Klick(doc, 2.2, 1.5));   // linke Hälfte des Feldes
        Assert.Equal(new TdPosition(0, 1, 1), Klick(doc, 2.8, 1.5));   // rechte Hälfte
        Assert.Equal("ab", doc.PlainText());
    }

    /// <summary>
    /// Ein gerechneter Inhaltsverzeichnis-Eintrag steht **nicht** im Dokument (§4.20) — ein
    /// Klick darauf darf nicht in ihm landen, sondern in der nächsten echten Zeile.
    /// </summary>
    [Fact]
    public void Ein_Inhaltsverzeichnis_Eintrag_wird_nicht_getroffen()
    {
        var doc = Dok(
            new TdParagraph([new TdField(TdFieldKind.TableOfContents)]),
            new TdParagraph("Kapitel") { Format = new TdParaFormat { OutlineLevel = 1 } });

        var umbruch = Umbrechen(doc);
        var seite = Assert.Single(umbruch.Pages);

        Assert.Contains(seite.Lines, z => z.IsTocEntry);

        // Jede getroffene Stelle gehört zu einem Absatz, den es wirklich gibt.
        for (double y = 1.1; y < 6; y += 0.5)
        {
            var stelle = TdHit.StelleAn(umbruch, doc, Messung, 0, 3, y);
            Assert.InRange(stelle.Paragraph, 0, TdCursor.Absaetze(doc).Count - 1);
        }
    }

    // ==================== Die Schreibmarke ====================

    /// <summary>Sie steht auf der Zeichengrenze, in Seitenkoordinaten samt Rand.</summary>
    [Fact]
    public void Die_Schreibmarke_steht_auf_der_Zeichengrenze()
    {
        var doc = Dok(new TdParagraph("Hallo"));

        Assert.Equal(1.0, Marke(doc, 0).XCm, 3);
        Assert.Equal(4.0, Marke(doc, 3).XCm, 3);
        Assert.Equal(6.0, Marke(doc, 5).XCm, 3);
        Assert.Equal(1.0, Marke(doc, 0).YCm, 3);
    }

    /// <summary>
    /// **In einem leeren Absatz steht sie am Einzug und nicht am Textrand.** Dort steht sie
    /// beim Schreiben besonders oft — ein neues Dokument besteht aus nichts anderem.
    /// </summary>
    [Fact]
    public void In_einem_leeren_Absatz_steht_sie_am_Einzug()
    {
        var doc = Dok(new TdParagraph
        {
            Format = new TdParaFormat { LeftIndentCm = 2 },
        });

        Assert.Equal(3.0, Marke(doc, 0).XCm, 3);   // 1 cm Rand + 2 cm Einzug
    }

    /// <summary>
    /// Am Ende einer umgebrochenen Zeile bleibt sie dort — und nicht am Anfang der nächsten.
    /// Es muss eine der beiden gelten, sonst blinkt sie mal hier und mal dort (§4.30).
    /// </summary>
    [Fact]
    public void Am_Zeilenende_bleibt_sie_am_Zeilenende()
    {
        var doc = Dok(new TdParagraph("aaa bbb ccc"));

        var amEnde = Marke(doc, 7);       // hinter „bbb", auf dem weggefallenen Zwischenraum
        var amAnfang = Marke(doc, 8);     // vor „ccc"

        Assert.Equal(8.0, amEnde.XCm, 3);
        Assert.Equal(1.0, amAnfang.XCm, 3);
        Assert.True(amAnfang.YCm > amEnde.YCm);
    }

    /// <summary>Steht die Stelle auf keiner Seite, gibt es keine Marke — und keine Ausnahme.</summary>
    [Fact]
    public void Ohne_gesetzte_Zeile_gibt_es_keine_Marke()
    {
        var doc = Dok(new TdParagraph("Hallo"));
        var leer = new TdLayoutResult();

        Assert.Null(TdHit.Schreibmarke(leer, doc, Messung, TdPosition.Null));
    }

    // ==================== Hin und zurück ====================

    /// <summary>
    /// **Der Wächter, der am meisten wert ist.** Jede Stelle des Dokuments wird zur Marke
    /// gerechnet, und aus dieser Marke muss derselbe Klick wieder dieselbe Stelle ergeben.
    /// Er fängt beide Rechnungen in einem — und jede Verschiebung um ein Zeichen.
    ///
    /// <para>
    /// <b>Mit Schrittgrenze</b>, wie in §4.30: Ein Wächter, der nicht fertig wird, meldet
    /// nichts, sondern blockiert die ganze Suite.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData("schlicht")]
    [InlineData("umgebrochen")]
    [InlineData("mehrere-absaetze")]
    [InlineData("mit-feld")]
    [InlineData("mit-verweis")]
    [InlineData("leere-absaetze")]
    [InlineData("in-einer-tabelle")]
    public void Jede_Stelle_findet_ueber_das_Papier_zu_sich_selbst(string fall)
    {
        var doc = Beispiel(fall);
        var umbruch = Umbrechen(doc);

        int grenze = TdCursor.Absaetze(doc).Sum(TdCursor.Laenge) + TdCursor.Absaetze(doc).Count + 5;

        var stelle = TdCursor.Anfang(doc);
        var ende = TdCursor.Ende(doc);

        for (int i = 0; ; i++)
        {
            Assert.True(i <= grenze, $"Nach {i} Schritten ist {ende} nicht erreicht — bei {stelle}.");

            var marke = TdHit.Schreibmarke(umbruch, doc, Messung, stelle);
            Assert.True(marke is not null, $"Keine Schreibmarke für {stelle}.");

            var zurueck = TdHit.StelleAn(
                umbruch, doc, Messung,
                marke!.Value.Seite, marke.Value.XCm, marke.Value.YCm + marke.Value.HoeheCm / 2);

            Assert.Equal(stelle, zurueck);

            if (stelle == ende) break;
            stelle = TdCursor.Rechts(doc, stelle);
        }
    }

    // ==================== Die Auswahl ====================

    /// <summary>Innerhalb einer Zeile ist es ein Kasten, und er sitzt auf den Zeichengrenzen.</summary>
    [Fact]
    public void Eine_Auswahl_in_einer_Zeile_ergibt_einen_Kasten()
    {
        var doc = Dok(new TdParagraph("Hallo Welt"));

        var markierung = TdHit.Markieren(
            Umbrechen(doc), doc, Messung,
            new TdSelection(new TdPosition(0, 0, 2), new TdPosition(0, 0, 6)));

        var kasten = Assert.Single(markierung.Auswahl);
        Assert.Equal(2.0, kasten.Value.VonCm, 3);
        Assert.Equal(6.0, kasten.Value.BisCm, 3);
    }

    /// <summary>Über einen Zeilenumbruch hinweg werden es zwei — je Zeile einer.</summary>
    [Fact]
    public void Eine_Auswahl_ueber_zwei_Zeilen_ergibt_zwei_Kaesten()
    {
        var doc = Dok(new TdParagraph("aaa bbb ccc"));

        var markierung = TdHit.Markieren(
            Umbrechen(doc), doc, Messung, TdSelection.Alles(doc));

        Assert.Equal(2, markierung.Auswahl.Count);
    }

    /// <summary>Und über Absätze hinweg genauso.</summary>
    [Fact]
    public void Eine_Auswahl_ueber_Absaetze_markiert_jeden()
    {
        var doc = Dok(new TdParagraph("eins"), new TdParagraph("zwei"), new TdParagraph("drei"));

        var markierung = TdHit.Markieren(
            Umbrechen(doc), doc, Messung,
            new TdSelection(new TdPosition(0, 0, 2), new TdPosition(2, 0, 2)));

        Assert.Equal(3, markierung.Auswahl.Count);
    }

    /// <summary>
    /// **Ein leerer Absatz mitten in der Auswahl bekommt trotzdem einen Kasten.** Ohne ihn
    /// sähe er aus, als wäre er nicht mit ausgewählt — und wer dann löscht, ist überrascht.
    /// </summary>
    [Fact]
    public void Ein_leerer_Absatz_in_der_Auswahl_bekommt_einen_Kasten()
    {
        var doc = Dok(new TdParagraph("eins"), new TdParagraph(), new TdParagraph("drei"));

        var markierung = TdHit.Markieren(
            Umbrechen(doc), doc, Messung,
            new TdSelection(new TdPosition(0, 0, 1), new TdPosition(2, 0, 3)));

        Assert.Equal(3, markierung.Auswahl.Count);
        Assert.All(markierung.Auswahl, k => Assert.True(k.Value.BreiteCm > 0));
    }

    /// <summary>Eine leere Auswahl ist der Cursor — und markiert nichts.</summary>
    [Fact]
    public void Eine_leere_Auswahl_markiert_nichts()
    {
        var doc = Dok(new TdParagraph("Hallo"));

        var markierung = TdHit.Markieren(
            Umbrechen(doc), doc, Messung, new TdSelection(new TdPosition(0, 0, 2)));

        Assert.Empty(markierung.Auswahl);
        Assert.NotNull(markierung.MarkeZeile);
        Assert.Equal(2.0, markierung.MarkeXCm, 3);
    }

    /// <summary>
    /// Die Marke steht auf der **Spitze** und nicht am Anfang der Auswahl (§4.30) — auch wenn
    /// rückwärts gezogen wurde.
    /// </summary>
    [Fact]
    public void Die_Marke_steht_auf_der_Spitze_der_Auswahl()
    {
        var doc = Dok(new TdParagraph("Hallo Welt"));

        var rueckwaerts = new TdSelection(new TdPosition(0, 0, 8), new TdPosition(0, 0, 2));
        var markierung = TdHit.Markieren(Umbrechen(doc), doc, Messung, rueckwaerts);

        Assert.Equal(2.0, markierung.MarkeXCm, 3);
    }

    /// <summary>Was außerhalb der Auswahl liegt, bekommt keinen Kasten.</summary>
    [Fact]
    public void Was_nicht_ausgewaehlt_ist_bekommt_keinen_Kasten()
    {
        var doc = Dok(new TdParagraph("eins"), new TdParagraph("zwei"), new TdParagraph("drei"));

        var markierung = TdHit.Markieren(
            Umbrechen(doc), doc, Messung,
            new TdSelection(new TdPosition(0, 0, 1), new TdPosition(0, 0, 3)));

        Assert.Single(markierung.Auswahl);
    }

    // ==================== Tabellen ====================

    /// <summary>
    /// In einer Zelle zählt der Versatz der Zelle mit — **Spalte und Innenabstand** (§4.19).
    ///
    /// <para>
    /// <b>Geprüft wird die Zahl und nicht nur der Rückweg</b>, und das ist hier der Punkt:
    /// Lässt man den Spaltenversatz in beiden Richtungen weg, findet jede Stelle weiterhin zu
    /// sich selbst — beide Rechnungen sind dann **gleich falsch**, und der Cursor stünde in der
    /// zweiten Spalte um deren ganze Breite daneben. Beim Erproben ist genau das passiert: Der
    /// Rückweg-Wächter blieb grün.
    /// </para>
    /// </summary>
    [Fact]
    public void In_einer_Tabellenzelle_zaehlt_der_Spaltenversatz_mit()
    {
        var doc = MitTabelle(spalten: 2);
        var umbruch = Umbrechen(doc);

        // Der Absatz in der **zweiten** Zelle — die erste steht bei 0 und verriete nichts.
        var stelle = TdCursor.AusLinear(TdCursor.AbsatzAn(doc, 2)!, 2, 2);
        var marke = TdHit.Schreibmarke(umbruch, doc, Messung, stelle);

        Assert.NotNull(marke);

        var reihe = Assert.Single(umbruch.Pages[0].TableRows);
        var zweite = reihe.Cells[1];
        var format = reihe.Table!.Format;

        // 1 cm Seitenrand + Spalte + Innenabstand + zwei Zeichen.
        Assert.Equal(1 + zweite.XCm + format.CellPaddingLeftCm + 2, marke!.Value.XCm, 3);
        Assert.True(zweite.XCm > 0, "Die zweite Spalte steht nicht rechts von der ersten.");

        var zurueck = TdHit.StelleAn(
            umbruch, doc, Messung, marke.Value.Seite,
            marke.Value.XCm, marke.Value.YCm + marke.Value.HoeheCm / 2);

        Assert.Equal(stelle, zurueck);
    }

    // ==================== Beispiele ====================

    private static TdDocument Beispiel(string fall) => fall switch
    {
        "schlicht" => Dok(new TdParagraph("Hallo Welt")),
        "umgebrochen" => Dok(new TdParagraph("aaa bbb ccc ddd eee")),
        "mehrere-absaetze" => Dok(
            new TdParagraph("eins"), new TdParagraph("zwei"), new TdParagraph("drei")),
        "mit-feld" => Dok(new TdParagraph([
            new TdRun("a"), new TdField(TdFieldKind.PageNumber), new TdRun("b")])),
        "mit-verweis" => Dok(new TdParagraph([
            new TdRun("vor "), new TdHyperlink("#ziel", new TdRun("Text"))])),
        "leere-absaetze" => Dok(
            new TdParagraph(), new TdParagraph("mitte"), new TdParagraph()),
        "in-einer-tabelle" => MitTabelle(spalten: 2),
        _ => throw new ArgumentOutOfRangeException(nameof(fall), fall, "Unbekanntes Beispiel"),
    };

    private static TdDocument MitTabelle(int spalten = 1)
    {
        var zeile = new TdTableRow();
        for (int i = 0; i < spalten; i++)
        {
            var zelle = new TdTableCell();
            zelle.Blocks.Add(new TdParagraph($"Z{i}"));
            zeile.Cells.Add(zelle);
        }

        var tabelle = new TdTable();
        tabelle.Rows.Add(zeile);

        return Dok(new TdParagraph("davor"), tabelle, new TdParagraph("danach"));
    }
}
