using System.Text;
using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdBlockEdit"/>, <see cref="TdFormatEdit.Verweis"/> und
/// <see cref="TdTableEdit"/> — Schritt 6, zweite Hälfte (HANDOFF §6): einfügen, verweisen,
/// Tabellen bearbeiten.
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Alle drei Handgriffe bauen **Blöcke** um, und ein Fehler
/// darin sieht anders aus als ein Fehler beim Tippen: Er löscht keinen Buchstaben, sondern
/// verschiebt eine Zelle, verliert ein Verweisziel oder lässt die Schreibmarke auf einem Absatz
/// stehen, den es nicht mehr gibt. **Das Letzte ist das gefährlichste** — die nächste Eingabe
/// landet dann irgendwo, und niemand bringt es mit dem Löschen davor in Verbindung.
/// </para>
/// <para>
/// <b>Bei Tabellen wird durchweg in Rasterspalten gerechnet und nicht in Zellindizes</b>
/// (<see cref="TdTableEdit"/>). Zwei Wächter halten genau das fest, denn bei einer
/// gleichmäßigen Tabelle sind beide Zahlen gleich — der Fehler zeigt sich erst mit einer
/// verbundenen Zelle.
/// </para>
/// </summary>
public sealed class EinfuegenTests
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

    private static TdParagraph Text(string text) => new(text);

    private static TdSelection Bei(TdDocument doc, int absatz, int linear)
    {
        var a = TdCursor.AbsatzAn(doc, absatz)!;
        return new TdSelection(TdCursor.AusLinear(a, absatz, linear));
    }

    private static TdSelection Von(TdDocument doc, int absatzA, int a, int absatzB, int b) =>
        new(TdCursor.AusLinear(TdCursor.AbsatzAn(doc, absatzA)!, absatzA, a),
            TdCursor.AusLinear(TdCursor.AbsatzAn(doc, absatzB)!, absatzB, b));

    /// <summary>Die Blöcke eines Abschnitts als lesbare Zeile — Art und Umriss, nicht Inhalt.</summary>
    private static string Umriss(TdDocument doc)
    {
        var sb = new StringBuilder();

        foreach (var block in doc.Sections[0].Blocks)
        {
            switch (block)
            {
                case TdParagraph absatz:
                    sb.Append($"¶({absatz.PlainText()})");
                    break;
                case TdPageBreak:
                    sb.Append("[umbruch]");
                    break;
                case TdTable tabelle:
                    sb.Append($"[tab {tabelle.Rows.Count}x{tabelle.Spaltenzahl()}]");
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>Das Raster einer Tabelle: je Zeile die Breiten ihrer Zellen.</summary>
    private static string Raster(TdTable tabelle) =>
        string.Join("|", tabelle.Rows.Select(z =>
            string.Join(",", z.Cells.Select(c => Math.Max(1, c.ColumnSpan)))));

    private static TdTable ErsteTabelle(TdDocument doc) =>
        doc.Sections[0].Blocks.OfType<TdTable>().First();

    private static TdTable Gitter(int zeilen, int spalten)
    {
        var tabelle = new TdTable();
        for (int z = 0; z < zeilen; z++)
        {
            var zeile = new TdTableRow();
            for (int s = 0; s < spalten; s++)
                zeile.Cells.Add(new TdTableCell(new TdParagraph($"z{z}s{s}")));
            tabelle.Rows.Add(zeile);
        }
        return tabelle;
    }

    // ==================== Blöcke einfügen ====================

    /// <summary>
    /// Ein Seitenumbruch mitten im Absatz teilt ihn — und **beide Hälften bleiben stehen**,
    /// samt ihrem Text.
    /// </summary>
    [Fact]
    public void Seitenumbruch_teilt_den_Absatz()
    {
        var doc = Dok(Text("abcdef"));

        TdBlockEdit.Seitenumbruch(doc, Bei(doc, 0, 3))!.Anwenden();

        Assert.Equal("¶(abc)[umbruch]¶(def)", Umriss(doc));
    }

    /// <summary>
    /// <b>Am Absatzanfang darf der Absatz nicht verschluckt werden.</b> Eine leere obere Hälfte
    /// ist richtig — ohne sie stünde der Umbruch als erster Block, und davor käme man nicht mehr.
    /// </summary>
    [Fact]
    public void Seitenumbruch_am_Anfang_laesst_die_leere_Haelfte_stehen()
    {
        var doc = Dok(Text("abc"));

        TdBlockEdit.Seitenumbruch(doc, Bei(doc, 0, 0))!.Anwenden();

        Assert.Equal("¶()[umbruch]¶(abc)", Umriss(doc));
    }

    /// <summary>Was ausgewählt war, verschwindet — wie bei jedem anderen Einfügen auch.</summary>
    [Fact]
    public void Einfuegen_ersetzt_die_Auswahl()
    {
        var doc = Dok(Text("abcdef"));

        TdBlockEdit.Seitenumbruch(doc, Von(doc, 0, 2, 0, 4))!.Anwenden();

        Assert.Equal("¶(ab)[umbruch]¶(ef)", Umriss(doc));
    }

    /// <summary>
    /// <b>Die Marke landet in der ersten Zelle</b>, wie in Word — und das ist die eine Regel
    /// „erster Absatz im Eingefügten", nicht ein Sonderfall für Tabellen.
    /// </summary>
    [Fact]
    public void Nach_dem_Einfuegen_einer_Tabelle_steht_die_Marke_in_der_ersten_Zelle()
    {
        var doc = Dok(Text("davor"), Text("dahinter"));

        var danach = TdBlockEdit.Tabelle(doc, Bei(doc, 0, 5), 2, 2)!.Anwenden();

        Assert.Equal("¶(davor)[tab 2x2]¶()¶(dahinter)", Umriss(doc));

        // Absatz 0 ist „davor", Absatz 1 die erste Zelle.
        Assert.Equal(1, danach.Focus.Paragraph);
        Assert.Same(
            ErsteTabelle(doc).Rows[0].Cells[0].Blocks[0],
            TdCursor.AbsatzAn(doc, 1));
    }

    /// <summary>
    /// <b>Eine Tabelle in eine Tabelle geht nicht — und der Knopf tut deshalb nichts</b>
    /// (Phase 5, Schritt ④).
    ///
    /// <para>
    /// <b>Der Anlass war ein Fund und keine Vorsichtsmaßnahme.</b> Die benannte Lücke aus
    /// §4.19 („der Umbruch setzt eine Tabelle *in* einer Zelle nicht") las sich wie eine
    /// Grenze des Modells — <b>sie war aber von der Oberfläche aus erreichbar</b>: Cursor in
    /// eine Zelle, „Tabelle einfügen", und die neue Tabelle stand in <c>zelle.Blocks</c>, wo
    /// der Umbruch sie wegließ. Der Nutzer legte Inhalt an, den niemand je zu sehen bekam.
    /// </para>
    /// <para>
    /// <b>Dieser Wächter hält die Sperre fest, nicht die Lücke</b> — die hat ihren eigenen in
    /// <c>TabellenUmbruchTests</c>. Wer die Lücke schließt, macht <i>beide</i> rot und muss
    /// sich zu beiden äußern: erst dann darf der Knopf wieder etwas tun.
    /// </para>
    /// </summary>
    [Fact]
    public void Eine_Tabelle_in_einer_Zelle_laesst_sich_nicht_einfuegen()
    {
        var doc = Dok(Text("davor"));
        TdBlockEdit.Tabelle(doc, Bei(doc, 0, 5), 2, 2)!.Anwenden();

        // Absatz 1 ist die erste Zelle der eben eingefügten Tabelle.
        string vorher = Umriss(doc);

        Assert.Null(TdBlockEdit.Tabelle(doc, Bei(doc, 1, 0), 2, 2));
        Assert.Equal(vorher, Umriss(doc));
    }

    /// <summary>
    /// <b>Die Infobox ist eine Tabelle mit einer Zelle</b> (§4.89) und fällt deshalb unter
    /// dieselbe Sperre — <b>ohne dass sie eigens genannt werden müsste</b>.
    ///
    /// <para>
    /// Der Wächter steht trotzdem da: Er beweist, dass die Regel an der Stelle sitzt, die
    /// einfügt, und nicht in <see cref="TdBlockEdit.Tabelle"/> allein. Wer sie dorthin
    /// zurückschöbe, machte genau diesen Test rot.
    /// </para>
    /// </summary>
    [Fact]
    public void Auch_eine_Infobox_geht_nicht_in_eine_Zelle()
    {
        var doc = Dok(Text("davor"));
        TdBlockEdit.Tabelle(doc, Bei(doc, 0, 5), 2, 2)!.Anwenden();

        Assert.Null(TdBlockEdit.Infobox(doc, Bei(doc, 1, 0), "#EEF2F8", "#D4DEEA"));
    }

    /// <summary>
    /// <b>Die Trennlinie ist von der Sperre nicht betroffen</b> — sie ist ein Absatz mit
    /// Rahmen und keine Tabelle (§4.40).
    ///
    /// <para>
    /// <b>Eine Sperre, die zu viel sperrt, fällt niemandem auf</b>: Der Knopf tut dann still
    /// nichts, und das sieht aus wie ein Knopf, den man falsch bedient hat. Dieser Wächter
    /// zieht die Grenze von der anderen Seite.
    /// </para>
    /// </summary>
    [Fact]
    public void Eine_Trennlinie_geht_sehr_wohl_in_eine_Zelle()
    {
        var doc = Dok(Text("davor"));
        TdBlockEdit.Tabelle(doc, Bei(doc, 0, 5), 2, 2)!.Anwenden();

        Assert.NotNull(TdBlockEdit.Trennlinie(doc, Bei(doc, 1, 0)));
    }

    /// <summary>
    /// Ein Seitenumbruch bringt keinen Absatz mit — dann steht die Marke am Anfang der unteren
    /// Hälfte, und zwar an deren **richtiger Nummer**.
    /// </summary>
    [Fact]
    public void Nach_einem_Seitenumbruch_steht_die_Marke_in_der_unteren_Haelfte()
    {
        var doc = Dok(Text("abcdef"));

        var danach = TdBlockEdit.Seitenumbruch(doc, Bei(doc, 0, 3))!.Anwenden();

        Assert.Equal(1, danach.Focus.Paragraph);
        Assert.Equal("def", TdCursor.AbsatzAn(doc, 1)!.PlainText());
    }

    /// <summary>
    /// <b>Ein eingefügter Absatzblock ist der Weg des Inhaltsverzeichnisses</b> (der Reiter
    /// „Verweise"): Es soll für sich stehen und nicht zwischen zwei Wörtern. Der Absatz wird
    /// geteilt, das Verzeichnis kommt dazwischen — und die Marke landet **darin**, weil es der
    /// erste Absatz im Eingefügten ist.
    /// </summary>
    [Fact]
    public void Ein_eingefuegter_Absatz_steht_zwischen_den_Haelften()
    {
        var doc = Dok(Text("abcdef"));

        var danach = TdBlockEdit.Einfuegen(
            doc, Bei(doc, 0, 3),
            new TdParagraph([new TdField(TdFieldKind.TableOfContents)]))!.Anwenden();

        Assert.Equal("¶(abc)¶()¶(def)", Umriss(doc));
        Assert.Equal(1, danach.Focus.Paragraph);

        var mitte = TdCursor.AbsatzAn(doc, 1)!;
        Assert.Equal(TdFieldKind.TableOfContents, mitte.Inlines.OfType<TdField>().Single().Kind);
    }

    /// <summary>
    /// <b>Die Trennlinie ist ein Absatz mit Unterstrich und kein eigener Blocktyp</b> (§4.40).
    /// Geprüft wird beides: dass sie als Absatz entsteht **und** dass die Linie wirklich daran
    /// hängt — ein Absatz ohne Rahmen wäre eine unsichtbare Leerzeile, und das sähe aus wie
    /// „der Knopf tut nichts".
    /// </summary>
    [Fact]
    public void Eine_Trennlinie_ist_ein_Absatz_mit_Unterstrich()
    {
        var doc = Dok(Text("abcdef"));

        TdBlockEdit.Trennlinie(doc, Bei(doc, 0, 3))!.Anwenden();

        Assert.Equal("¶(abc)¶()¶(def)", Umriss(doc));

        var linie = TdCursor.AbsatzAn(doc, 1)!;
        Assert.NotNull(linie.Format.BottomBorder);
        Assert.True(linie.Format.BottomBorder!.Value.Sichtbar);
    }

    /// <summary>Jede Zelle bekommt einen Absatz — sonst hätte der Cursor darin keinen Ort.</summary>
    [Fact]
    public void Jede_neue_Zelle_hat_einen_Absatz()
    {
        var doc = Dok(Text(""));

        TdBlockEdit.Tabelle(doc, Bei(doc, 0, 0), 2, 3)!.Anwenden();

        foreach (var zeile in ErsteTabelle(doc).Rows)
            foreach (var zelle in zeile.Cells)
                Assert.Single(zelle.Blocks.OfType<TdParagraph>());
    }

    /// <summary>Eine Tabelle ohne Zeilen oder Spalten gibt es nicht.</summary>
    [Fact]
    public void Eine_Tabelle_ohne_Zeilen_wird_abgelehnt()
    {
        var doc = Dok(Text("abc"));

        Assert.Null(TdBlockEdit.Tabelle(doc, Bei(doc, 0, 0), 0, 3));
        Assert.Null(TdBlockEdit.Tabelle(doc, Bei(doc, 0, 0), 3, 0));
        Assert.Null(TdBlockEdit.Einfuegen(doc, Bei(doc, 0, 0)));
    }

    /// <summary>Und die Probe: die Rücknahme führt vollständig zurück.</summary>
    [Fact]
    public void Ruecknahme_des_Einfuegens_fuehrt_zurueck()
    {
        var doc = Dok(Text("abcdef"), Text("zweiter"));
        string vorher = Umriss(doc);

        var aenderung = TdBlockEdit.Tabelle(doc, Bei(doc, 0, 3), 2, 2)!;
        aenderung.Anwenden();
        Assert.NotEqual(vorher, Umriss(doc));

        aenderung.Zuruecknehmen();
        Assert.Equal(vorher, Umriss(doc));
    }

    // ==================== Verweise ====================

    /// <summary>Der einfachste Fall: ein Wort wird zum Verweis.</summary>
    [Fact]
    public void Ein_Verweis_legt_sich_um_die_Auswahl()
    {
        var doc = Dok(Text("siehe hier bitte"));

        TdFormatEdit.Verweis(doc, Von(doc, 0, 6, 0, 10), "https://x")!.Anwenden();

        var absatz = (TdParagraph)doc.Sections[0].Blocks[0];
        var verweis = absatz.Inlines.OfType<TdHyperlink>().Single();

        Assert.Equal("https://x", verweis.Target);
        Assert.Equal("hier", verweis.PlainText());
        Assert.Equal("siehe hier bitte", doc.PlainText());
    }

    /// <summary>Leeres Ziel nimmt ihn wieder heraus — und der Text bleibt.</summary>
    [Fact]
    public void Ein_leeres_Ziel_nimmt_den_Verweis_heraus()
    {
        var doc = Dok(new TdParagraph([new TdHyperlink("https://x", new TdRun("hier"))]));

        TdFormatEdit.Verweis(doc, Von(doc, 0, 0, 0, 4), null)!.Anwenden();

        var absatz = (TdParagraph)doc.Sections[0].Blocks[0];
        Assert.Empty(absatz.Inlines.OfType<TdHyperlink>());
        Assert.Equal("hier", doc.PlainText());
    }

    /// <summary>
    /// <b>Erst auswickeln, dann einwickeln.</b> Ein Verweis im Verweis kennt weder DOCX noch
    /// die flache Sicht — beim Export käme das äußere Ziel für den ganzen Text heraus.
    /// </summary>
    [Fact]
    public void Ein_neuer_Verweis_ueber_einem_alten_verschachtelt_nicht()
    {
        var doc = Dok(new TdParagraph([
            new TdRun("a"),
            new TdHyperlink("https://alt", new TdRun("bc")),
            new TdRun("d"),
        ]));

        TdFormatEdit.Verweis(doc, Von(doc, 0, 0, 0, 4), "https://neu")!.Anwenden();

        var absatz = (TdParagraph)doc.Sections[0].Blocks[0];
        var verweis = absatz.Inlines.OfType<TdHyperlink>().Single();

        Assert.Equal("https://neu", verweis.Target);
        Assert.Empty(verweis.Inlines.OfType<TdHyperlink>());
        Assert.Equal("abcd", doc.PlainText());
    }

    /// <summary>Ein Verweis der Länge null wäre keiner — er zeigte nichts an (§4.30).</summary>
    [Fact]
    public void Eine_leere_Auswahl_bekommt_keinen_Verweis()
    {
        var doc = Dok(Text("abc"));

        Assert.Null(TdFormatEdit.Verweis(doc, Bei(doc, 0, 1), "https://x"));
    }

    /// <summary>Auch hier muss die Rücknahme vollständig zurückführen — samt Ziel.</summary>
    [Fact]
    public void Ruecknahme_des_Verweises_fuehrt_zurueck()
    {
        var doc = Dok(new TdParagraph([new TdHyperlink("https://alt", new TdRun("hier"))]));

        var aenderung = TdFormatEdit.Verweis(doc, Von(doc, 0, 0, 0, 4), "https://neu")!;
        aenderung.Anwenden();
        aenderung.Zuruecknehmen();

        var absatz = (TdParagraph)doc.Sections[0].Blocks[0];
        Assert.Equal("https://alt", absatz.Inlines.OfType<TdHyperlink>().Single().Target);
    }

    /// <summary>
    /// Die Auskunft für das Ribbon: liegt die Marke in einem Verweis, und in welchem?
    /// <b>Bei leerer Auswahl zählt das Stück links</b> — dieselbe Zugehörigkeit wie beim Erben
    /// eines Formats (§4.30), damit ein Klick hinter einen Verweis ihn noch meint.
    /// </summary>
    [Fact]
    public void VerweisZiel_meldet_den_Verweis_unter_der_Marke()
    {
        var doc = Dok(new TdParagraph([
            new TdRun("a"),
            new TdHyperlink("https://x", new TdRun("bc")),
            new TdRun("d"),
        ]));

        Assert.Null(TdFormatEdit.VerweisZiel(doc, Bei(doc, 0, 1)));
        Assert.Equal("https://x", TdFormatEdit.VerweisZiel(doc, Bei(doc, 0, 2)));
        Assert.Equal("https://x", TdFormatEdit.VerweisZiel(doc, Bei(doc, 0, 3)));
        Assert.Null(TdFormatEdit.VerweisZiel(doc, Bei(doc, 0, 4)));
    }

    /// <summary>Reicht die Auswahl über einen Verweis hinaus, gibt es kein *eines* Ziel.</summary>
    [Fact]
    public void VerweisZiel_meldet_nichts_wenn_die_Auswahl_hinausreicht()
    {
        var doc = Dok(new TdParagraph([
            new TdHyperlink("https://x", new TdRun("bc")),
            new TdRun("d"),
        ]));

        Assert.Equal("https://x", TdFormatEdit.VerweisZiel(doc, Von(doc, 0, 0, 0, 2)));
        Assert.Null(TdFormatEdit.VerweisZiel(doc, Von(doc, 0, 0, 0, 3)));
    }

    // ==================== Tabellen ====================

    /// <summary>Wo steht die Marke — die Auskunft, an der der Reiter „Tabelle" hängt.</summary>
    [Fact]
    public void Ort_meldet_Zeile_und_Spalte()
    {
        var doc = Dok(Text("davor"), Gitter(2, 3));

        // Absätze: 0 = „davor", dann zeilenweise die sechs Zellen.
        Assert.Null(TdTableEdit.Ort(doc, Bei(doc, 0, 0).Focus));

        var ort = TdTableEdit.Ort(doc, Bei(doc, 5, 0).Focus);
        Assert.NotNull(ort);
        Assert.Equal(1, ort!.Value.Zeile);
        Assert.Equal(1, ort.Value.Spalte);
    }

    /// <summary>Eine neue Zeile darunter, mit so vielen Zellen wie die Vorlage Spalten belegt.</summary>
    [Fact]
    public void Zeile_einfuegen_darunter()
    {
        var doc = Dok(Gitter(2, 3));

        TdTableEdit.ZeileEinfuegen(doc, Bei(doc, 0, 0), darunter: true)!.Anwenden();

        var tabelle = ErsteTabelle(doc);
        Assert.Equal(3, tabelle.Rows.Count);
        Assert.Equal("z0s0", tabelle.Rows[0].Cells[0].PlainText());
        Assert.Equal("", tabelle.Rows[1].Cells[0].PlainText());
        Assert.Equal("z1s0", tabelle.Rows[2].Cells[0].PlainText());
    }

    /// <summary>
    /// <b>Die neue Zeile zählt Rasterspalten und nicht Zellen.</b> Bei einer Zeile mit einer
    /// verbundenen Zelle hätte sie sonst zu wenige — und alles rechts davon rutschte.
    /// </summary>
    [Fact]
    public void Eine_neue_Zeile_folgt_dem_Raster_und_nicht_der_Zellenzahl()
    {
        var tabelle = Gitter(1, 3);
        tabelle.Rows[0].Cells.RemoveAt(2);
        tabelle.Rows[0].Cells[1].ColumnSpan = 2;   // zwei Zellen, drei Spalten
        var doc = Dok(tabelle);

        TdTableEdit.ZeileEinfuegen(doc, Bei(doc, 0, 0), darunter: true)!.Anwenden();

        Assert.Equal("1,2|1,1,1", Raster(ErsteTabelle(doc)));
    }

    /// <summary>Eine Spalte rechts — in **jeder** Zeile, nicht nur in der mit der Marke.</summary>
    [Fact]
    public void Spalte_einfuegen_trifft_jede_Zeile()
    {
        var doc = Dok(Gitter(2, 2));

        TdTableEdit.SpalteEinfuegen(doc, Bei(doc, 0, 0), rechts: true)!.Anwenden();

        var tabelle = ErsteTabelle(doc);
        Assert.Equal(3, tabelle.Spaltenzahl());
        Assert.Equal("z0s0", tabelle.Rows[0].Cells[0].PlainText());
        Assert.Equal("", tabelle.Rows[0].Cells[1].PlainText());
        Assert.Equal("z0s1", tabelle.Rows[0].Cells[2].PlainText());
        Assert.Equal("", tabelle.Rows[1].Cells[1].PlainText());
    }

    /// <summary>
    /// <b>Mitten in einer verbundenen Zelle wird sie breiter, nicht zerschnitten.</b>
    /// Zerschneiden hieße entscheiden, welche Hälfte den Inhalt behält.
    /// </summary>
    [Fact]
    public void Eine_verbundene_Zelle_wird_breiter_statt_zerschnitten()
    {
        var tabelle = Gitter(2, 3);
        tabelle.Rows[0].Cells.RemoveRange(1, 2);
        tabelle.Rows[0].Cells[0].ColumnSpan = 3;
        var doc = Dok(tabelle);

        // Die Marke steht in der zweiten Zeile, mittlere Spalte (Rasterspalte 1).
        TdTableEdit.SpalteEinfuegen(doc, Bei(doc, 2, 0), rechts: false)!.Anwenden();

        Assert.Equal("4|1,1,1,1", Raster(ErsteTabelle(doc)));
    }

    /// <summary>Zeile löschen — und die Marke muss die gelöschte Zelle verlassen haben.</summary>
    [Fact]
    public void Zeile_loeschen_setzt_die_Marke_in_die_Tabelle_zurueck()
    {
        var doc = Dok(Text("davor"), Gitter(2, 2));

        // Absatz 3 ist Zeile 1, Spalte 0.
        var danach = TdTableEdit.ZeileLoeschen(doc, Bei(doc, 3, 0))!.Anwenden();

        var tabelle = ErsteTabelle(doc);
        Assert.Single(tabelle.Rows);
        Assert.Equal("z0s0", tabelle.Rows[0].Cells[0].PlainText());

        // Absatz 1 ist die erste Zelle der Tabelle — sie gibt es noch.
        Assert.Equal(1, danach.Focus.Paragraph);
        Assert.Same(tabelle.Rows[0].Cells[0].Blocks[0], TdCursor.AbsatzAn(doc, 1));
    }

    /// <summary>Spalte löschen, dasselbe in der anderen Richtung.</summary>
    [Fact]
    public void Spalte_loeschen_nimmt_sie_aus_jeder_Zeile()
    {
        var doc = Dok(Gitter(2, 3));

        // Absatz 1 ist Zeile 0, Spalte 1.
        TdTableEdit.SpalteLoeschen(doc, Bei(doc, 1, 0))!.Anwenden();

        var tabelle = ErsteTabelle(doc);
        Assert.Equal(2, tabelle.Spaltenzahl());
        Assert.Equal("z0s0", tabelle.Rows[0].Cells[0].PlainText());
        Assert.Equal("z0s2", tabelle.Rows[0].Cells[1].PlainText());
        Assert.Equal("z1s2", tabelle.Rows[1].Cells[1].PlainText());
    }

    /// <summary>
    /// Die letzte Zeile und die letzte Spalte werden nicht gelöscht — eine Tabelle ohne beides
    /// wäre nichts, was man noch anklicken könnte. Wer sie loswerden will, löscht die Tabelle.
    /// </summary>
    [Fact]
    public void Die_letzte_Zeile_und_Spalte_bleiben_stehen()
    {
        var doc = Dok(Gitter(1, 1));

        Assert.Null(TdTableEdit.ZeileLoeschen(doc, Bei(doc, 0, 0)));
        Assert.Null(TdTableEdit.SpalteLoeschen(doc, Bei(doc, 0, 0)));
    }

    /// <summary>
    /// Die ganze Tabelle löschen — <b>nicht ersatzlos</b>: An ihre Stelle tritt ein leerer
    /// Absatz, sonst bliebe der Abschnitt ohne Ort für den Cursor.
    /// </summary>
    [Fact]
    public void Tabelle_loeschen_hinterlaesst_einen_leeren_Absatz()
    {
        var doc = Dok(Text("davor"), Gitter(2, 2), Text("dahinter"));

        var danach = TdTableEdit.TabelleLoeschen(doc, Bei(doc, 2, 0))!.Anwenden();

        Assert.Equal("¶(davor)¶()¶(dahinter)", Umriss(doc));
        Assert.Equal(1, danach.Focus.Paragraph);
    }

    /// <summary>Außerhalb einer Tabelle tut keiner dieser Handgriffe etwas.</summary>
    [Fact]
    public void Ausserhalb_einer_Tabelle_geschieht_nichts()
    {
        var doc = Dok(Text("abc"));
        var marke = Bei(doc, 0, 1);

        Assert.Null(TdTableEdit.ZeileEinfuegen(doc, marke, darunter: true));
        Assert.Null(TdTableEdit.SpalteEinfuegen(doc, marke, rechts: true));
        Assert.Null(TdTableEdit.ZeileLoeschen(doc, marke));
        Assert.Null(TdTableEdit.SpalteLoeschen(doc, marke));
        Assert.Null(TdTableEdit.TabelleLoeschen(doc, marke));
    }

    /// <summary>
    /// <b>Die alte Tabelle bleibt unangetastet</b> — sie ist die Sicherung des Verlaufs
    /// (§4.32). Am Ergebnis ist das nicht zu sehen, deshalb wird sie selbst befragt.
    ///
    /// <para>
    /// <b>Und zwar bis auf die Zellen, nicht nur bis auf die Zeilen.</b> Die erste Fassung
    /// dieses Wächters prüfte nur <c>Rows.Count</c> nach einem <c>ZeileEinfuegen</c> — und
    /// **blieb grün**, als <c>Kopie</c> versuchsweise dieselben Zeilenobjekte weiterreichte:
    /// Eine neue Zeilenliste mit alten Zeilen darin fällt beim Zählen der Zeilen nicht auf. Erst
    /// ein Handgriff, der **in** eine Zeile greift (Spalte einfügen), macht den Unterschied
    /// sichtbar. Dieselbe Lehre wie in §4.30, §4.32 und §4.33: ein Wächter, der beim
    /// absichtlichen Kaputtmachen grün bleibt, prüft nichts.
    /// </para>
    /// </summary>
    [Fact]
    public void Die_alte_Tabelle_bleibt_unveraendert()
    {
        var alt = Gitter(2, 2);
        string vorher = Raster(alt);
        var doc = Dok(alt);

        TdTableEdit.ZeileEinfuegen(doc, Bei(doc, 0, 0), darunter: true)!.Anwenden();
        Assert.Equal(2, alt.Rows.Count);

        TdTableEdit.SpalteEinfuegen(doc, Bei(doc, 0, 0), rechts: true)!.Anwenden();
        Assert.Equal(vorher, Raster(alt));
        Assert.Equal(2, alt.Rows[0].Cells.Count);

        Assert.NotSame(alt, ErsteTabelle(doc));
        Assert.NotSame(alt.Rows[0], ErsteTabelle(doc).Rows[0]);
    }

    /// <summary>
    /// Die Absätze in den Zellen werden **weitergereicht und nicht verdoppelt**: An ihnen
    /// ändert sich nichts, und eine Kopie machte jede Prüfung auf Objektgleichheit blind.
    /// </summary>
    [Fact]
    public void Die_Absaetze_in_den_Zellen_bleiben_dieselben_Objekte()
    {
        var alt = Gitter(1, 2);
        var zelleninhalt = alt.Rows[0].Cells[0].Blocks[0];
        var doc = Dok(alt);

        TdTableEdit.SpalteEinfuegen(doc, Bei(doc, 0, 0), rechts: true)!.Anwenden();

        Assert.Same(zelleninhalt, ErsteTabelle(doc).Rows[0].Cells[0].Blocks[0]);
    }

    /// <summary>Und die Probe für alle vier Handgriffe: die Rücknahme führt zurück.</summary>
    [Fact]
    public void Ruecknahme_der_Tabellenaenderungen_fuehrt_zurueck()
    {
        var doc = Dok(Gitter(2, 3));
        string vorher = Raster(ErsteTabelle(doc));

        foreach (var bauen in new Func<TdChange?>[]
        {
            () => TdTableEdit.ZeileEinfuegen(doc, Bei(doc, 0, 0), darunter: true),
            () => TdTableEdit.SpalteEinfuegen(doc, Bei(doc, 0, 0), rechts: false),
            () => TdTableEdit.ZeileLoeschen(doc, Bei(doc, 0, 0)),
            () => TdTableEdit.SpalteLoeschen(doc, Bei(doc, 0, 0)),
        })
        {
            var aenderung = bauen()!;
            aenderung.Anwenden();
            aenderung.Zuruecknehmen();
            Assert.Equal(vorher, Raster(ErsteTabelle(doc)));
        }
    }

    /// <summary>Tabellenänderungen fassen im Verlauf nie zusammen (§4.33).</summary>
    [Fact]
    public void Tabellenaenderungen_sind_Strukturaenderungen()
    {
        var doc = Dok(Gitter(2, 2));

        Assert.Equal(
            TdEditArt.Struktur,
            TdTableEdit.ZeileEinfuegen(doc, Bei(doc, 0, 0), darunter: true)!.Art);
        Assert.Equal(
            TdEditArt.Struktur,
            TdBlockEdit.Seitenumbruch(doc, Bei(doc, 0, 0))!.Art);
    }
}
