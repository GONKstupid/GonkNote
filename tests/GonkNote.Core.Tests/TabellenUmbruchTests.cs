using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// Das Tabellen-Layout — Phase 4, Schritt 4, zweiter Teil.
///
/// <para>
/// Gemessen wird wie in <see cref="UmbruchTests"/> mit **festen Maßen** (jedes Zeichen 1 cm,
/// jede Zeile 1 cm): eine Tabelle hat genug bewegliche Teile, ohne dass auch noch die
/// installierte Schrift mitredet.
/// </para>
/// </summary>
public sealed class TabellenUmbruchTests
{
    private sealed class FesteMessung : ITdTextMeasure
    {
        public double WidthCm(string text, TdCharFormat format) => text.Length;
        public TdFontMetrics Metrics(TdCharFormat format) => new(0.8, 0.2, 1.0);
    }

    /// <summary>Ein Blatt mit <paramref name="breite"/> × <paramref name="hoehe"/> cm Textbereich.</summary>
    private static TdPageSetup Blatt(double breite = 12, double hoehe = 10) => new()
    {
        WidthCm = breite + 2,
        HeightCm = hoehe + 2,
        MarginLeftCm = 1,
        MarginRightCm = 1,
        MarginTopCm = 1,
        MarginBottomCm = 1,
    };

    /// <summary>
    /// Ein Dokument ohne Absatzabstände und ohne Zellinnenabstand — sonst geht keine
    /// Rechnung im Kopf auf (die Lehre aus §4.16).
    /// </summary>
    private static TdDocument Dok(TdPageSetup seite, params TdBlock[] bloecke)
    {
        var doc = new TdDocument
        {
            DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 0 },
            Sections = { new TdSection(bloecke) { Page = seite } },
        };
        foreach (var t in doc.Blocks().OfType<TdTable>()) OhnePolster(t);
        return doc;
    }

    private static TdTable OhnePolster(TdTable t)
    {
        t.Format.CellPaddingLeftCm = 0;
        t.Format.CellPaddingRightCm = 0;
        t.Format.CellPaddingTopCm = 0;
        t.Format.CellPaddingBottomCm = 0;
        return t;
    }

    private static TdLayoutResult Umbrechen(TdDocument doc) =>
        TdLayout.Umbrechen(doc, new FesteMessung());

    // ==================== Raster ====================

    /// <summary>Die Zellen sitzen nebeneinander, jede an ihrer Spalte.</summary>
    [Fact]
    public void Die_Zellen_sitzen_an_ihren_Spalten()
    {
        var tabelle = new TdTable(TdTableRow.Text("a", "b", "c"))
        {
            ColumnWidthsCm = { 3, 5, 4 },
        };

        var zeile = Umbrechen(Dok(Blatt(), tabelle)).Pages[0].TableRows[0];

        Assert.Equal(3, zeile.Cells.Count);
        Assert.Equal(0, zeile.Cells[0].XCm, 3);
        Assert.Equal(3, zeile.Cells[0].WidthCm, 3);
        Assert.Equal(3, zeile.Cells[1].XCm, 3);
        Assert.Equal(5, zeile.Cells[1].WidthCm, 3);
        Assert.Equal(8, zeile.Cells[2].XCm, 3);
    }

    /// <summary>Eine waagerecht verbundene Zelle bekommt die Breite aller Spalten, die sie belegt.</summary>
    [Fact]
    public void Eine_waagerecht_verbundene_Zelle_ist_so_breit_wie_ihre_Spalten()
    {
        var tabelle = new TdTable(
            new TdTableRow(
                new TdTableCell(new TdParagraph("weit")) { ColumnSpan = 2 },
                TdTableCell.Text("c")))
        {
            ColumnWidthsCm = { 3, 5, 4 },
        };

        var zeile = Umbrechen(Dok(Blatt(), tabelle)).Pages[0].TableRows[0];

        Assert.Equal(8, zeile.Cells[0].WidthCm, 3);
        Assert.Equal(8, zeile.Cells[1].XCm, 3);
        Assert.Equal(4, zeile.Cells[1].WidthCm, 3);
    }

    /// <summary>
    /// <b>Der Zellinhalt bricht in der Innenbreite um, nicht in der Zellbreite.</b> Ein
    /// vergessener Innenabstand ist kein sichtbarer Fehler, sondern eine Tabelle, deren Text
    /// am Rand klebt und eine Zeile zu spät umbricht.
    /// </summary>
    [Fact]
    public void Der_Innenabstand_verschmaelert_den_Umbruch()
    {
        var eng = new TdTable(TdTableRow.Text("aaaa bbbb")) { ColumnWidthsCm = { 10 } };
        eng.Format.CellPaddingLeftCm = 1;
        eng.Format.CellPaddingRightCm = 1;

        var doc = new TdDocument
        {
            DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 0 },
            Sections = { new TdSection(eng) { Page = Blatt() } },
        };

        // Innen 10 − 2 = 8 cm; „aaaa bbbb" ist 9 Zeichen und passt nicht.
        Assert.Equal(2, Umbrechen(doc).Pages[0].TableRows[0].Cells[0].Lines.Count);
    }

    // ==================== Zeilenhöhe ====================

    /// <summary>Die Zeile ist so hoch wie ihre höchste Zelle.</summary>
    [Fact]
    public void Die_Zeile_richtet_sich_nach_der_hoechsten_Zelle()
    {
        var tabelle = new TdTable(new TdTableRow(
            TdTableCell.Text("kurz"),
            new TdTableCell(new TdParagraph("eins"), new TdParagraph("zwei"), new TdParagraph("drei"))))
        {
            ColumnWidthsCm = { 6, 6 },
        };

        var zeile = Umbrechen(Dok(Blatt(), tabelle)).Pages[0].TableRows[0];

        Assert.Equal(3.0, zeile.HeightCm, 3);
    }

    /// <summary>Eine Mindesthöhe gilt, wenn der Inhalt kleiner ist — und nicht, wenn er größer ist.</summary>
    [Fact]
    public void Die_Mindesthoehe_hebt_an_und_deckelt_nicht()
    {
        var klein = new TdTable(new TdTableRow(TdTableCell.Text("x")) { MinHeightCm = 3 })
        { ColumnWidthsCm = { 6 } };

        var gross = new TdTable(new TdTableRow(new TdTableCell(
            new TdParagraph("a"), new TdParagraph("b"), new TdParagraph("c"), new TdParagraph("d")))
        { MinHeightCm = 2 })
        { ColumnWidthsCm = { 6 } };

        Assert.Equal(3.0, Umbrechen(Dok(Blatt(), klein)).Pages[0].TableRows[0].HeightCm, 3);
        Assert.Equal(4.0, Umbrechen(Dok(Blatt(), gross)).Pages[0].TableRows[0].HeightCm, 3);
    }

    // ==================== Senkrechte Verbindung ====================

    /// <summary>
    /// Eine Fortsetzungszelle bekommt **keinen Ort und keinen Inhalt** — ihr Inhalt steht in
    /// der Zelle darüber. Sie schiebt aber die Spalte weiter: sonst säße alles dahinter eine
    /// Spalte zu weit links.
    /// </summary>
    [Fact]
    public void Eine_Fortsetzungszelle_kommt_nicht_vor_verschiebt_aber_die_Spalte()
    {
        var tabelle = new TdTable(
            new TdTableRow(
                new TdTableCell(new TdParagraph("oben")) { VerticalMerge = TdVerticalMerge.Restart },
                TdTableCell.Text("b")),
            new TdTableRow(
                new TdTableCell { VerticalMerge = TdVerticalMerge.Continue },
                TdTableCell.Text("d")))
        {
            ColumnWidthsCm = { 4, 8 },
        };

        var zeilen = Umbrechen(Dok(Blatt(), tabelle)).Pages[0].TableRows;

        Assert.Equal(2, zeilen[0].Cells.Count);
        // Zweite Zeile: nur die rechte Zelle, und die sitzt trotzdem bei 4 cm.
        Assert.Single(zeilen[1].Cells);
        Assert.Equal(4, zeilen[1].Cells[0].XCm, 3);
        Assert.Equal("d", zeilen[1].Cells[0].Lines[0].PlainText());
    }

    /// <summary>Die Restart-Zelle weiß, über wie viele Zeilen sie reicht.</summary>
    [Fact]
    public void Die_verbundene_Zelle_kennt_ihre_Zeilenzahl()
    {
        var tabelle = new TdTable(
            new TdTableRow(
                new TdTableCell(new TdParagraph("lang")) { VerticalMerge = TdVerticalMerge.Restart },
                TdTableCell.Text("b")),
            new TdTableRow(new TdTableCell { VerticalMerge = TdVerticalMerge.Continue }, TdTableCell.Text("d")),
            new TdTableRow(new TdTableCell { VerticalMerge = TdVerticalMerge.Continue }, TdTableCell.Text("f")),
            new TdTableRow(TdTableCell.Text("g"), TdTableCell.Text("h")))
        {
            ColumnWidthsCm = { 4, 8 },
        };

        var zeilen = Umbrechen(Dok(Blatt(), tabelle)).Pages[0].TableRows;

        Assert.Equal(3, zeilen[0].Cells[0].RowSpan);
        Assert.Equal(1, zeilen[3].Cells[0].RowSpan);
    }

    /// <summary>
    /// <b>Eine verbundene Zelle zieht ihre Zeile nicht allein hoch.</b> Ihre Höhe verteilt
    /// sich über die Zeilen, über die sie reicht; fehlt Platz, wächst die **letzte** von
    /// ihnen. Würde stattdessen die erste wachsen, säße jede unverbundene Nachbarzelle
    /// daneben in einer plötzlich viel zu hohen Zeile.
    /// </summary>
    [Fact]
    public void Eine_verbundene_Zelle_verteilt_ihre_Hoehe()
    {
        // Linke Zelle: vier Absätze = 4 cm, über zwei Zeilen verbunden.
        // Rechte Zellen: **kurze** Texte, damit sie in einer Zeile bleiben — bei 1 cm je
        // Zeichen bricht „rechts oben" auf 8 cm Spaltenbreite sonst selbst um, und dann
        // prüft der Test den Zellumbruch statt der Verteilung.
        var tabelle = new TdTable(
            new TdTableRow(
                new TdTableCell(new TdParagraph("a"), new TdParagraph("b"), new TdParagraph("c"), new TdParagraph("d"))
                { VerticalMerge = TdVerticalMerge.Restart },
                TdTableCell.Text("R1")),
            new TdTableRow(
                new TdTableCell { VerticalMerge = TdVerticalMerge.Continue },
                TdTableCell.Text("R2")))
        {
            ColumnWidthsCm = { 4, 8 },
        };

        var zeilen = Umbrechen(Dok(Blatt(), tabelle)).Pages[0].TableRows;

        Assert.Equal(1.0, zeilen[0].HeightCm, 3);   // von der rechten Zelle
        Assert.Equal(3.0, zeilen[1].HeightCm, 3);   // 1 + die fehlenden 2 cm
        Assert.Equal(1.0, zeilen[1].YCm, 3);
    }

    // ==================== Seitenumbruch ====================

    /// <summary>Passt eine Zeile nicht mehr, beginnt sie auf der nächsten Seite.</summary>
    [Fact]
    public void Eine_Tabelle_bricht_zwischen_ihren_Zeilen_um()
    {
        var zeilen = Enumerable.Range(1, 14)
            .Select(i => new TdTableRow(TdTableCell.Text($"Z{i}")))
            .ToArray();

        var ergebnis = Umbrechen(Dok(Blatt(), new TdTable(zeilen) { ColumnWidthsCm = { 12 } }));

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal(10, ergebnis.Pages[0].TableRows.Count);
        Assert.Equal(4, ergebnis.Pages[1].TableRows.Count);
        Assert.Equal(0, ergebnis.Pages[1].TableRows[0].YCm, 3);
    }

    /// <summary>
    /// <b>Die Kopfzeile wiederholt sich auf jeder Folgeseite.</b> Sie steht im Modell nur
    /// einmal — auf dem Papier so oft, wie die Tabelle Seiten braucht.
    /// </summary>
    [Fact]
    public void Die_Kopfzeile_wiederholt_sich_auf_der_naechsten_Seite()
    {
        var zeilen = new List<TdTableRow>
        {
            new(TdTableCell.Text("KOPF")) { IsHeader = true },
        };
        for (int i = 1; i <= 14; i++) zeilen.Add(new TdTableRow(TdTableCell.Text($"Z{i}")));

        var ergebnis = Umbrechen(Dok(Blatt(), new TdTable([.. zeilen]) { ColumnWidthsCm = { 12 } }));

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal("KOPF", ergebnis.Pages[0].TableRows[0].Cells[0].Lines[0].PlainText());
        Assert.Equal("KOPF", ergebnis.Pages[1].TableRows[0].Cells[0].Lines[0].PlainText());

        // Die wiederholte ist als solche erkennbar — wer sie beim Zurückrechnen auf den Text
        // mitzählt, findet den Cursor an der falschen Stelle.
        Assert.False(ergebnis.Pages[0].TableRows[0].IsRepeatedHeader);
        Assert.True(ergebnis.Pages[1].TableRows[0].IsRepeatedHeader);

        // Und im Modell steht sie weiterhin genau einmal.
        Assert.Single(
            ergebnis.Pages.SelectMany(s => s.TableRows),
            z => z is { IsRepeatedHeader: false, Source.IsHeader: true });
    }

    /// <summary>
    /// Eine Zeile, die höher ist als die ganze Seite, darf nicht in eine Endlosschleife
    /// führen — sie kommt auf die leere Seite und ragt heraus. Derselbe Ausweg wie beim
    /// überlangen Wort (§4.16).
    /// </summary>
    [Fact]
    public void Eine_zu_hohe_Zeile_haengt_die_Rechnung_nicht_auf()
    {
        var riesig = new TdTableCell([.. Enumerable.Range(1, 20).Select(i => (TdBlock)new TdParagraph($"Z{i}"))]);

        var ergebnis = Umbrechen(Dok(Blatt(),
            new TdTable(new TdTableRow(riesig), new TdTableRow(TdTableCell.Text("danach")))
            { ColumnWidthsCm = { 12 } }));

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal(20.0, ergebnis.Pages[0].TableRows[0].HeightCm, 3);
        Assert.Equal("danach", ergebnis.Pages[1].TableRows[0].Cells[0].Lines[0].PlainText());
    }

    /// <summary>Text vor und nach einer Tabelle sitzt an der richtigen Höhe.</summary>
    [Fact]
    public void Absaetze_und_Tabellen_folgen_aufeinander()
    {
        var ergebnis = Umbrechen(Dok(Blatt(),
            new TdParagraph("davor"),
            new TdTable(new TdTableRow(TdTableCell.Text("Zelle"))) { ColumnWidthsCm = { 12 } },
            new TdParagraph("danach")));

        var seite = ergebnis.Pages[0];

        Assert.Equal(2, seite.Lines.Count);
        Assert.Single(seite.TableRows);

        Assert.Equal(0, seite.Lines[0].YCm, 3);        // „davor"
        Assert.Equal(1, seite.TableRows[0].YCm, 3);    // die Tabelle
        Assert.Equal(2, seite.Lines[1].YCm, 3);        // „danach"
    }

    /// <summary>Eine Tabelle ohne Zeilen ist kein Fehler — sie belegt nur nichts.</summary>
    [Fact]
    public void Eine_leere_Tabelle_belegt_nichts()
    {
        var ergebnis = Umbrechen(Dok(Blatt(), new TdTable(), new TdParagraph("danach")));

        Assert.Empty(ergebnis.Pages[0].TableRows);
        Assert.Equal(0, ergebnis.Pages[0].Lines[0].YCm, 3);
    }

    /// <summary>
    /// <b>Benannte Lücke, festgehalten statt verschwiegen:</b> eine Tabelle **in** einer
    /// Zelle wird vom Umbruch noch nicht gesetzt. Das Modell trägt sie und DOCX schreibt und
    /// liest sie (§4.18) — nur eine gesetzte Zelle hat noch keine Tabellenzeilen.
    /// <para>
    /// Dieser Wächter hält den Zustand fest, damit er **absichtlich** verschwindet und nicht
    /// versehentlich: wer die Lücke schließt, macht diesen Test rot und muss ihn umschreiben.
    /// Der Absatz daneben beweist zugleich, dass die äußere Zelle nicht mitleidet.
    /// </para>
    /// </summary>
    [Fact]
    public void Eine_Tabelle_in_einer_Zelle_wird_noch_nicht_gesetzt()
    {
        var innen = OhnePolster(new TdTable(TdTableRow.Text("innen")) { ColumnWidthsCm = { 5 } });
        var aussen = OhnePolster(new TdTable(new TdTableRow(new TdTableCell(
            new TdParagraph("davor"), innen)))
        { ColumnWidthsCm = { 12 } });

        var zelle = Umbrechen(Dok(Blatt(), aussen)).Pages[0].TableRows[0].Cells[0];

        Assert.Equal("davor", Assert.Single(zelle.Lines).PlainText());
    }

    /// <summary>Ein Listenpunkt in einer Zelle bekommt seine Marke wie überall sonst.</summary>
    [Fact]
    public void Eine_Liste_in_einer_Zelle_wird_nummeriert()
    {
        var doc = new TdDocument
        {
            DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 0 },
            Lists = { TdListDefinition.Nummern(1) },
            Sections =
            {
                new TdSection(OhnePolster(new TdTable(new TdTableRow(new TdTableCell(
                    new TdParagraph("eins") { List = new TdListRef(1, 0) },
                    new TdParagraph("zwei") { List = new TdListRef(1, 0) })))
                { ColumnWidthsCm = { 12 } }))
                { Page = Blatt() },
            },
        };

        var zellzeilen = Umbrechen(doc).Pages[0].TableRows[0].Cells[0].Lines;

        Assert.Equal("1.", zellzeilen[0].Marker?.Text);
        Assert.Equal("2.", zellzeilen[1].Marker?.Text);
    }
}
