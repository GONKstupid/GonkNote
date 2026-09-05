using System.Globalization;
using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <b>Der Tabellenentwurf, zweite Hälfte</b> (HANDOFF §4.91): teilen, sortieren, rechnen und
/// die Umwandlung zwischen Tabelle und Text.
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Anders als in §4.90 rechnet hier etwas — und
/// Rechenfehler in einer Tabelle sehen richtig aus. Eine Spalte mit „2", „10", „9" alphabetisch
/// zu sortieren ergibt eine plausible Reihenfolge, die falsch ist; eine Summe, die bei der
/// ersten leeren Zelle aufhört, ergibt eine plausible Zahl, die falsch ist. <b>Beides meldet
/// kein Bau und keine Anzeige.</b>
/// </para>
/// </summary>
public sealed class TabellenUmbauTests
{
    // ==================== Hilfsmittel ====================

    private static TdDocument Mit(params TdBlock[] bloecke) =>
        new() { Sections = { new TdSection(bloecke) } };

    private static TdTable Tab(params string[][] zeilen)
    {
        var tabelle = new TdTable();
        foreach (var felder in zeilen)
            tabelle.Rows.Add(TdTableRow.Text(felder));
        return tabelle;
    }

    private static TdTable Danach(TdDocument doc) => doc.Blocks().OfType<TdTable>().First();

    /// <summary>Eine Auswahl in der Zelle (<paramref name="zeile"/>, <paramref name="spalte"/>).</summary>
    private static TdSelection In(TdDocument doc, int zeile, int spalte)
    {
        var tabelle = doc.Blocks().OfType<TdTable>().First();

        int nummer = 0;
        foreach (var block in doc.Sections[0].Blocks)
        {
            if (ReferenceEquals(block, tabelle)) break;
            nummer += block is TdParagraph ? 1 : 0;
        }

        for (int z = 0; z < tabelle.Rows.Count; z++)
            for (int s = 0; s < tabelle.Rows[z].Cells.Count; s++)
            {
                if (z == zeile && s == spalte)
                    return new TdSelection(new TdPosition(nummer, 0, 0));

                nummer += tabelle.Rows[z].Cells[s].Blocks.OfType<TdParagraph>().Count();
            }

        throw new ArgumentOutOfRangeException(nameof(zeile), "Zelle gibt es nicht.");
    }

    private static string Zelle(TdTable t, int z, int s) =>
        TdTabellenformel.Text(t.Rows[z].Cells[s]);

    // ==================== Zahlen lesen ====================

    /// <summary>
    /// <b>Beide Schreibweisen</b>: Ein Dokument kann aus der deutschen oder der englischen Welt
    /// kommen, und eine Tabelle, deren Summe von der Systemsprache abhängt, rechnet auf dem
    /// nächsten Rechner anders.
    /// </summary>
    [Theory]
    [InlineData("42", 42)]
    [InlineData("3,5", 3.5)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("-7", -7)]
    [InlineData("12 €", 12)]
    [InlineData("50%", 50)]
    public void Zahlen_werden_in_beiden_Schreibweisen_gelesen(string text, double erwartet)
    {
        Assert.Equal(erwartet, TdTabellenformel.AlsZahl(text)!.Value, 3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Summe")]
    [InlineData("—")]
    public void Was_keine_Zahl_ist_gibt_null(string text)
    {
        Assert.Null(TdTabellenformel.AlsZahl(text));
    }

    /// <summary>Eine Anzahl von „3,00" sähe aus, als wäre gemessen worden, wo gezählt wurde.</summary>
    [Fact]
    public void Ganze_Zahlen_stehen_ohne_Nachkommastellen()
    {
        Assert.Equal("3", TdTabellenformel.Formatiert(3.0));
        Assert.Equal("3,5", TdTabellenformel.Formatiert(3.5));
    }

    // ==================== Formel ====================

    [Fact]
    public void Die_Summe_nimmt_die_Spalte_darueber()
    {
        var doc = Mit(Tab(["10"], ["5"], ["7"], [""]));

        TdTableEdit.Formel(doc, In(doc, 3, 0), TdFormelArt.Summe, TdFormelRichtung.Oben)!
            .Anwenden();

        Assert.Equal("22", Zelle(Danach(doc), 3, 0));
    }

    [Fact]
    public void Der_Mittelwert_nimmt_die_Zeile_links()
    {
        var doc = Mit(Tab(["4", "6", "8", ""]));

        TdTableEdit.Formel(doc, In(doc, 0, 3), TdFormelArt.Mittelwert, TdFormelRichtung.Links)!
            .Anwenden();

        Assert.Equal("6", Zelle(Danach(doc), 0, 3));
    }

    /// <summary>
    /// <b>Eine leere Zelle beendet die Reihe nicht.</b> Word hört dort auf — eine Regel, die
    /// man kennen muss, um sie vorherzusagen, und die bei einer Zwischenüberschrift in der
    /// Spalte eine halbe Summe ergibt, ohne dass etwas danach aussieht.
    /// </summary>
    [Fact]
    public void Eine_Luecke_beendet_die_Reihe_nicht()
    {
        var doc = Mit(Tab(["10"], [""], ["5"], [""]));

        TdTableEdit.Formel(doc, In(doc, 3, 0), TdFormelArt.Summe, TdFormelRichtung.Oben)!
            .Anwenden();

        Assert.Equal("15", Zelle(Danach(doc), 3, 0));
    }

    /// <summary>Ohne eine einzige Zahl passiert nichts — eine 0 sähe aus wie ein Ergebnis.</summary>
    [Fact]
    public void Ohne_Zahlen_passiert_nichts()
    {
        var doc = Mit(Tab(["a"], ["b"], [""]));

        Assert.Null(TdTableEdit.Formel(
            doc, In(doc, 2, 0), TdFormelArt.Summe, TdFormelRichtung.Oben));
    }

    [Theory]
    [InlineData(TdFormelArt.Anzahl, "3")]
    [InlineData(TdFormelArt.Kleinstwert, "2")]
    [InlineData(TdFormelArt.Groesstwert, "9")]
    [InlineData(TdFormelArt.Produkt, "72")]
    public void Die_uebrigen_Arten_rechnen_auch(TdFormelArt art, string erwartet)
    {
        var doc = Mit(Tab(["4"], ["9"], ["2"], [""]));

        TdTableEdit.Formel(doc, In(doc, 3, 0), art, TdFormelRichtung.Oben)!.Anwenden();

        Assert.Equal(erwartet, Zelle(Danach(doc), 3, 0));
    }

    // ==================== Sortieren ====================

    /// <summary>
    /// <b>Der klassische Fehler, den jeder erkennt und niemand erwartet:</b> „2", „10", „9"
    /// alphabetisch sortiert ergibt „10", „2", „9" — eine plausible Reihenfolge, die falsch ist.
    /// </summary>
    [Fact]
    public void Zahlen_werden_als_Zahlen_sortiert()
    {
        var doc = Mit(Tab(["2"], ["10"], ["9"]));

        TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true)!.Anwenden();

        var t = Danach(doc);
        Assert.Equal(["2", "9", "10"], new[] { Zelle(t, 0, 0), Zelle(t, 1, 0), Zelle(t, 2, 0) });
    }

    [Fact]
    public void Text_wird_alphabetisch_sortiert()
    {
        var doc = Mit(Tab(["Birne"], ["Apfel"], ["Citrone"]));

        TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true)!.Anwenden();

        var t = Danach(doc);
        Assert.Equal("Apfel", Zelle(t, 0, 0));
        Assert.Equal("Citrone", Zelle(t, 2, 0));
    }

    [Fact]
    public void Absteigend_dreht_die_Reihenfolge_um()
    {
        var doc = Mit(Tab(["2"], ["10"], ["9"]));

        TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: false)!.Anwenden();

        Assert.Equal("10", Zelle(Danach(doc), 0, 0));
    }

    /// <summary>
    /// <b>Eine Kopfzeile bleibt oben.</b> Sie ist keine Datenzeile (§4.19) — wer sie
    /// mitsortiert, findet sie beim nächsten Mal in der Mitte.
    /// </summary>
    [Fact]
    public void Die_Kopfzeile_wird_nicht_mitsortiert()
    {
        var tabelle = Tab(["Zahl"], ["9"], ["2"]);
        tabelle.Rows[0].IsHeader = true;
        var doc = Mit(tabelle);

        TdTableEdit.Sortieren(doc, In(doc, 1, 0), aufsteigend: true)!.Anwenden();

        var t = Danach(doc);
        Assert.Equal("Zahl", Zelle(t, 0, 0));
        Assert.Equal("2", Zelle(t, 1, 0));
        Assert.Equal("9", Zelle(t, 2, 0));
    }

    /// <summary>
    /// <b>Gemischt entscheidet die Mehrheit.</b> Eine Zeile-für-Zeile-Entscheidung ergäbe eine
    /// Ordnung, die nicht durchgängig ist — a &lt; b und b &lt; c, aber c &lt; a.
    /// </summary>
    [Fact]
    public void Gemischte_Spalten_werden_alphabetisch_sortiert()
    {
        var doc = Mit(Tab(["10"], ["Apfel"], ["2"]));

        TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true)!.Anwenden();

        var t = Danach(doc);
        Assert.Equal(["10", "2", "Apfel"], new[] { Zelle(t, 0, 0), Zelle(t, 1, 0), Zelle(t, 2, 0) });
    }

    /// <summary>
    /// <b>Datum wird vor Zahl geprüft</b> — sonst liest sich „01.03.2026" als 1.032.026 und
    /// „15.02.2026" als 15.022.026, und die Reihenfolge kehrt sich um, ohne dass etwas danach
    /// aussieht.
    /// </summary>
    [Fact]
    public void Datumsangaben_werden_als_Datum_sortiert()
    {
        var doc = Mit(Tab(["15.02.2026"], ["01.03.2026"], ["20.01.2026"]));

        TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true)!.Anwenden();

        var t = Danach(doc);
        Assert.Equal(
            ["20.01.2026", "15.02.2026", "01.03.2026"],
            new[] { Zelle(t, 0, 0), Zelle(t, 1, 0), Zelle(t, 2, 0) });
    }

    /// <summary>
    /// <b>Und dasselbe auf einem englischen Rechner</b> — der Wächter, der vier Tage gefehlt
    /// hat (§4.101).
    ///
    /// <para>
    /// <b>Der Fehler, den er festhält, war ein echter und kein Testfehler:</b>
    /// <c>AlsDatum</c> las mit <c>CultureInfo.CurrentCulture</c>. Auf <c>de-DE</c> ging
    /// „15.02.2026" durch, auf <c>en-US</c> nicht (Monat 15) — die Spalte galt dann als
    /// <b>Zahlen</b>spalte, und daraus wurde 15.022.026. <b>Genau die Umkehrung, vor der der
    /// Kommentar über der Sortierung warnt</b>, nur dass sie niemand sah: Das Ergebnis sieht
    /// sortiert aus.
    /// </para>
    /// <para>
    /// <b>Gefunden hat es die CI und nicht dieser Wächter</b> — ihre Runner laufen auf
    /// <c>en-US</c>, der Entwicklungsrechner auf <c>de-DE</c>, und der Test darüber erbt die
    /// Kultur des Rechners. <i>Ein Wächter, der die Kultur des Rechners erbt, prüft den
    /// Rechner und nicht das Programm.</i>
    /// </para>
    /// </summary>
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    public void Datumsangaben_werden_unabhaengig_von_der_Systemsprache_sortiert(string kultur)
    {
        var vorher = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(kultur);

            var doc = Mit(Tab(["15.02.2026"], ["01.03.2026"], ["20.01.2026"]));
            TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true)!.Anwenden();

            var t = Danach(doc);
            Assert.Equal(
                ["20.01.2026", "15.02.2026", "01.03.2026"],
                new[] { Zelle(t, 0, 0), Zelle(t, 1, 0), Zelle(t, 2, 0) });
        }
        finally { CultureInfo.CurrentCulture = vorher; }
    }

    /// <summary>
    /// <b>Auch die andere Schreibweise wird erkannt</b>, und zwar auf jedem Rechner: Die
    /// invariante Kultur steht als zweite in <c>Datumskulturen</c> und fängt <c>2026-01-20</c>
    /// ab, wenn Deutsch die Spalte nicht vollständig lesen konnte.
    /// </summary>
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void ISO_Datumsangaben_werden_als_Datum_sortiert(string kultur)
    {
        var vorher = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(kultur);

            var doc = Mit(Tab(["2026-02-15"], ["2026-03-01"], ["2026-01-20"]));
            TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true)!.Anwenden();

            var t = Danach(doc);
            Assert.Equal(
                ["2026-01-20", "2026-02-15", "2026-03-01"],
                new[] { Zelle(t, 0, 0), Zelle(t, 1, 0), Zelle(t, 2, 0) });
        }
        finally { CultureInfo.CurrentCulture = vorher; }
    }

    /// <summary>
    /// <b>Eine Spalte aus Zahlen bleibt eine Spalte aus Zahlen.</b> Ohne
    /// <c>NoCurrentDateDefault</c> läse <c>DateTime.TryParse</c> auch „5" als den Fünften des
    /// laufenden Monats — und die Zahlen wären plötzlich Daten.
    /// </summary>
    [Fact]
    public void Blosse_Zahlen_werden_nicht_zu_Datumsangaben()
    {
        var doc = Mit(Tab(["2"], ["10"], ["9"]));

        TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true)!.Anwenden();

        var t = Danach(doc);
        Assert.Equal(["2", "9", "10"], new[] { Zelle(t, 0, 0), Zelle(t, 1, 0), Zelle(t, 2, 0) });
    }

    /// <summary>Zweimal Sortieren ergibt dasselbe wie einmal — die Sortierung ist stabil.</summary>
    [Fact]
    public void Sortieren_ist_wiederholbar()
    {
        var doc = Mit(Tab(["b", "1"], ["a", "2"], ["b", "3"]));

        TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true)!.Anwenden();
        var reihen = Danach(doc).Rows
            .Select(z => TdTabellenformel.Text(z.Cells[1])).ToList();

        TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true)!.Anwenden();

        Assert.Equal(reihen, Danach(doc).Rows
            .Select(z => TdTabellenformel.Text(z.Cells[1])).ToList());
    }

    /// <summary>Eine Tabelle mit einer Datenzeile lässt sich nicht sortieren.</summary>
    [Fact]
    public void Eine_einzige_Zeile_wird_nicht_sortiert()
    {
        var doc = Mit(Tab(["a"]));

        Assert.Null(TdTableEdit.Sortieren(doc, In(doc, 0, 0), aufsteigend: true));
    }

    // ==================== Teilen ====================

    /// <summary>
    /// <b>Der leere Absatz zwischen den Hälften gehört dazu.</b> Zwei Tabellen unmittelbar
    /// hintereinander sind in DOCX <b>eine</b> — ohne ihn wäre das Teilen beim nächsten Öffnen
    /// wieder rückgängig.
    /// </summary>
    [Fact]
    public void Teilen_setzt_einen_Absatz_dazwischen()
    {
        var doc = Mit(Tab(["a"], ["b"], ["c"]));

        TdTableEdit.TabelleTeilen(doc, In(doc, 1, 0))!.Anwenden();

        var bloecke = doc.Sections[0].Blocks;
        Assert.Equal(3, bloecke.Count);
        Assert.IsType<TdTable>(bloecke[0]);
        Assert.IsType<TdParagraph>(bloecke[1]);
        Assert.IsType<TdTable>(bloecke[2]);

        Assert.Single(((TdTable)bloecke[0]).Rows);
        Assert.Equal(2, ((TdTable)bloecke[2]).Rows.Count);
    }

    /// <summary>Über der ersten Zeile bliebe eine Tabelle ohne Zeilen.</summary>
    [Fact]
    public void Die_erste_Zeile_laesst_sich_nicht_abteilen()
    {
        var doc = Mit(Tab(["a"], ["b"]));

        Assert.Null(TdTableEdit.TabelleTeilen(doc, In(doc, 0, 0)));
    }

    /// <summary>
    /// <b>Die Kopfzeilen-Angabe wandert nicht mit.</b> Die untere Tabelle fängt mit einer
    /// Datenzeile an; sie zur Kopfzeile zu erklären hieße, sie auf jeder Folgeseite zu
    /// wiederholen — eine Wirkung, die niemand bestellt hat.
    /// </summary>
    [Fact]
    public void Die_untere_Haelfte_bekommt_keine_Kopfzeile()
    {
        var tabelle = Tab(["Kopf"], ["a"], ["b"]);
        tabelle.Rows[0].IsHeader = true;
        var doc = Mit(tabelle);

        TdTableEdit.TabelleTeilen(doc, In(doc, 1, 0))!.Anwenden();

        var unten = (TdTable)doc.Sections[0].Blocks[2];
        Assert.False(unten.Rows[0].IsHeader);
    }

    // ==================== Tabelle ↔ Text ====================

    [Fact]
    public void In_Text_macht_aus_jeder_Zeile_einen_Absatz()
    {
        var doc = Mit(Tab(["a", "b"], ["c", "d"]));

        TdTableEdit.InText(doc, In(doc, 0, 0))!.Anwenden();

        var texte = doc.Sections[0].Blocks.OfType<TdParagraph>()
            .Select(a => a.PlainText()).ToList();

        Assert.Equal(["a\tb", "c\td"], texte);
        Assert.Empty(doc.Blocks().OfType<TdTable>());
    }

    [Fact]
    public void Aus_Text_macht_aus_jedem_Absatz_eine_Zeile()
    {
        var doc = Mit(new TdParagraph("a\tb"), new TdParagraph("c\td"));

        TdBlockEdit.AusText(
            doc,
            new TdSelection(new TdPosition(0, 0, 0), new TdPosition(1, 0, 3)),
            TdTableEdit.Trennzeichen)!.Anwenden();

        var t = Danach(doc);
        Assert.Equal(2, t.Rows.Count);
        Assert.Equal("b", Zelle(t, 0, 1));
        Assert.Equal("c", Zelle(t, 1, 0));
    }

    /// <summary>
    /// <b>Kürzere Zeilen werden aufgefüllt.</b> Nach der kürzesten zu gehen hieße, Text
    /// wegzuwerfen.
    /// </summary>
    [Fact]
    public void Kuerzere_Zeilen_bekommen_leere_Zellen()
    {
        var doc = Mit(new TdParagraph("a"), new TdParagraph("b\tc\td"));

        TdBlockEdit.AusText(
            doc,
            new TdSelection(new TdPosition(0, 0, 0), new TdPosition(1, 0, 5)),
            TdTableEdit.Trennzeichen)!.Anwenden();

        var t = Danach(doc);
        Assert.Equal(3, t.Rows[0].Cells.Count);
        Assert.Equal("", Zelle(t, 0, 1));
    }

    /// <summary>
    /// Ein einzelner Absatz ohne Trennzeichen ergibt keine Tabelle — eine Tabelle mit einer
    /// Zelle ist das, was „Infobox" tut, und wer sie hier bekäme, hätte sie nicht bestellt.
    /// </summary>
    [Fact]
    public void Ein_Absatz_ohne_Trennzeichen_ergibt_keine_Tabelle()
    {
        var doc = Mit(new TdParagraph("nur Text"));

        Assert.Null(TdBlockEdit.AusText(
            doc, new TdSelection(new TdPosition(0, 0, 0)), TdTableEdit.Trennzeichen));
    }

    /// <summary>Hin und zurück ergibt dieselbe Tabelle.</summary>
    [Fact]
    public void Hin_und_zurueck_ergibt_dasselbe()
    {
        var doc = Mit(Tab(["a", "b"], ["c", "d"]));

        TdTableEdit.InText(doc, In(doc, 0, 0))!.Anwenden();
        TdBlockEdit.AusText(
            doc,
            new TdSelection(new TdPosition(0, 0, 0), new TdPosition(1, 0, 3)),
            TdTableEdit.Trennzeichen)!.Anwenden();

        var t = Danach(doc);
        Assert.Equal(2, t.Rows.Count);
        Assert.Equal(["a", "b", "c", "d"],
            new[] { Zelle(t, 0, 0), Zelle(t, 0, 1), Zelle(t, 1, 0), Zelle(t, 1, 1) });
    }
}
