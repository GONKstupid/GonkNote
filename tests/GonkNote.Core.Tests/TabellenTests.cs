using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// Tabellen — Phase 4, Schritt 4, erster Teil (Modell und DOCX).
///
/// <para>
/// <b>Warum das der gefährlichste Schritt bisher ist:</b> Eine Tabelle hat drei Dinge, die
/// jedes für sich still schiefgehen können — das Raster (welche Spalte ist wie breit), die
/// Verbindungen (welche Zelle reicht über welche) und die Rahmen. Alle drei sind in DOCX
/// anders abgelegt, als man es erwartet, und ein Fehler darin sieht nicht nach einem Fehler
/// aus, sondern nach einer Tabelle, die „irgendwie verrutscht" ist.
/// </para>
/// </summary>
public sealed class TabellenTests
{
    private static TdDocument Dok(params TdBlock[] bloecke) =>
        new() { Sections = { new TdSection(bloecke) } };

    private static TdDocument Zurueck(TdDocument doc)
    {
        using var strom = new MemoryStream();
        TdDocx.Schreiben(doc, strom);
        strom.Position = 0;
        return TdDocx.Lesen(strom);
    }

    private static TdTable ErsteTabelle(TdDocument doc) =>
        doc.Blocks().OfType<TdTable>().First();

    // ==================== Modell ====================

    /// <summary>
    /// Die Spaltenzahl ergibt sich aus dem Raster **und** aus dem, was die Zeilen belegen —
    /// eine Zelle über zwei Spalten zählt zwei. Wer nur die Zellen zählt, bekommt bei jeder
    /// verbundenen Zelle eine Spalte zu wenig.
    /// </summary>
    [Fact]
    public void Eine_verbundene_Zelle_zaehlt_so_viele_Spalten_wie_sie_belegt()
    {
        var tabelle = new TdTable(
            new TdTableRow(new TdTableCell(new TdParagraph("über alles")) { ColumnSpan = 3 }),
            TdTableRow.Text("a", "b", "c"));

        Assert.Equal(3, tabelle.Rows[0].GridSpaltenzahl());
        Assert.Equal(3, tabelle.Spaltenzahl());
    }

    /// <summary>Ohne Angabe wird die Breite gleichmäßig geteilt.</summary>
    [Fact]
    public void Ohne_Angabe_werden_die_Spalten_gleich_breit()
    {
        var tabelle = new TdTable(TdTableRow.Text("a", "b", "c", "d"));

        var breiten = tabelle.Spaltenbreiten(verfuegbarCm: 16);

        Assert.Equal(4, breiten.Length);
        Assert.All(breiten, b => Assert.Equal(4.0, b, 3));
    }

    /// <summary>Angegebene Spalten bleiben, der Rest teilt sich das Übrige.</summary>
    [Fact]
    public void Angegebene_Spalten_bleiben_und_der_Rest_teilt_sich_den_Platz()
    {
        var tabelle = new TdTable(TdTableRow.Text("a", "b", "c"))
        {
            ColumnWidthsCm = { 6, 0, 0 },
        };

        var breiten = tabelle.Spaltenbreiten(verfuegbarCm: 16);

        Assert.Equal(6.0, breiten[0], 3);
        Assert.Equal(5.0, breiten[1], 3);
        Assert.Equal(5.0, breiten[2], 3);
    }

    /// <summary>
    /// <b>Eine Spaltenbreite darf nie null oder negativ werden.</b> Eine Tabelle aus fremder
    /// Hand, deren angegebene Spalten schon breiter sind als die Seite, bekäme sonst Spalten
    /// von −3 cm — und der Zeilenumbruch darin liefe gegen die Wand.
    /// </summary>
    [Fact]
    public void Eine_zu_breite_Tabelle_ergibt_keine_negativen_Spalten()
    {
        var tabelle = new TdTable(TdTableRow.Text("a", "b"))
        {
            ColumnWidthsCm = { 30, 0 },
        };

        var breiten = tabelle.Spaltenbreiten(verfuegbarCm: 16);

        Assert.All(breiten, b => Assert.True(b > 0, $"Spaltenbreite {b} cm."));
    }

    /// <summary>Der Klartext einer Tabelle nimmt Fortsetzungszellen nicht doppelt mit.</summary>
    [Fact]
    public void Der_Klartext_zaehlt_eine_verbundene_Zelle_einmal()
    {
        var tabelle = new TdTable(
            new TdTableRow(
                new TdTableCell(new TdParagraph("oben")) { VerticalMerge = TdVerticalMerge.Restart },
                TdTableCell.Text("rechts")),
            new TdTableRow(
                new TdTableCell { VerticalMerge = TdVerticalMerge.Continue },
                TdTableCell.Text("darunter")));

        Assert.Equal("oben\trechts\ndarunter", tabelle.PlainText());
    }

    // ==================== Json ====================

    [Fact]
    public void Eine_Tabelle_uebersteht_das_eigene_Speicherformat()
    {
        var original = Dok(Beispieltabelle());

        var zurueck = TdFormatIo.Lesen(TdFormatIo.Schreiben(original));

        Assert.NotNull(zurueck);
        GleicheTabelle(ErsteTabelle(original), ErsteTabelle(zurueck));
    }

    /// <summary>Der Diskriminator einer Tabelle ist Datenformat und steht wörtlich fest.</summary>
    [Fact]
    public void Der_Diskriminator_einer_Tabelle_steht_fest()
    {
        var json = System.Text.Encoding.UTF8.GetString(
            TdFormatIo.Schreiben(Dok(new TdTable(TdTableRow.Text("x")))));

        Assert.Contains("\"t\":\"table\"", json);
    }

    // ==================== DOCX ====================

    [Fact]
    public void Eine_Tabelle_uebersteht_den_DOCX_Roundtrip()
    {
        var original = Dok(Beispieltabelle());

        GleicheTabelle(ErsteTabelle(original), ErsteTabelle(Zurueck(original)));
    }

    [Fact]
    public void Ein_DOCX_mit_Tabelle_haelt_das_Office_Schema_ein()
    {
        string ordner = Path.Combine(Path.GetTempPath(), $"gonk-tabelle-{Guid.NewGuid():N}");
        Directory.CreateDirectory(ordner);
        try
        {
            string pfad = Path.Combine(ordner, "tabelle.docx");
            TdDocx.Schreiben(Dok(Beispieltabelle()), pfad);

            Assert.Equal(0, TdDocx.Pruefen(pfad));
        }
        finally
        {
            try { Directory.Delete(ordner, recursive: true); } catch { /* Wegwerf */ }
        }
    }

    /// <summary>
    /// <b>Eine Fortsetzung ist ein <c>vMerge</c> ohne Wert</b> — nicht eines mit „continue".
    /// Word schreibt es so, und beim Lesen gilt dieselbe Regel wie beim <c>&lt;w:b/&gt;</c>:
    /// kein Wert heißt nicht „aus".
    /// </summary>
    [Fact]
    public void Eine_senkrechte_Verbindung_ueberlebt_in_beide_Richtungen()
    {
        var doc = Dok(new TdTable(
            new TdTableRow(
                new TdTableCell(new TdParagraph("oben")) { VerticalMerge = TdVerticalMerge.Restart },
                TdTableCell.Text("rechts")),
            new TdTableRow(
                new TdTableCell { VerticalMerge = TdVerticalMerge.Continue },
                TdTableCell.Text("darunter"))));

        var t = ErsteTabelle(Zurueck(doc));

        Assert.Equal(TdVerticalMerge.Restart, t.Rows[0].Cells[0].VerticalMerge);
        Assert.Equal(TdVerticalMerge.Continue, t.Rows[1].Cells[0].VerticalMerge);
        Assert.Equal(TdVerticalMerge.None, t.Rows[0].Cells[1].VerticalMerge);
        Assert.Equal("oben", t.Rows[0].Cells[0].PlainText());
    }

    /// <summary>
    /// Eine Fortsetzungszelle trägt keinen Inhalt. DOCX verlangt trotzdem einen Absatz darin
    /// — der darf beim Lesen **nicht** als Inhalt zurückkommen, sonst wüchse der Roundtrip
    /// mit jedem Durchgang um einen leeren Absatz je verbundener Zelle.
    /// </summary>
    [Fact]
    public void Der_Pflichtabsatz_einer_leeren_Zelle_kommt_nicht_als_Inhalt_zurueck()
    {
        var doc = Dok(new TdTable(new TdTableRow(new TdTableCell(), TdTableCell.Text("x"))));

        var einmal = Zurueck(doc);
        var zweimal = Zurueck(einmal);

        Assert.Empty(ErsteTabelle(einmal).Rows[0].Cells[0].Blocks);
        Assert.Empty(ErsteTabelle(zweimal).Rows[0].Cells[0].Blocks);
    }

    [Fact]
    public void Eine_waagerechte_Verbindung_ueberlebt()
    {
        var doc = Dok(new TdTable(
            new TdTableRow(new TdTableCell(new TdParagraph("Überschrift")) { ColumnSpan = 2 }),
            TdTableRow.Text("a", "b")));

        var t = ErsteTabelle(Zurueck(doc));

        Assert.Equal(2, t.Rows[0].Cells[0].ColumnSpan);
        Assert.Equal(1, t.Rows[1].Cells[0].ColumnSpan);
        Assert.Equal(2, t.Spaltenzahl());
    }

    /// <summary>
    /// <b>DOCX misst Rahmen in Achtel-Punkt.</b> Wer Punkte einträgt, bekommt eine achtmal zu
    /// dicke Linie — und das sieht nicht nach einem Umrechnungsfehler aus, sondern nach einer
    /// Tabelle mit unabsichtlich fetten Rändern.
    /// </summary>
    [Fact]
    public void Rahmenstaerken_ueberstehen_die_Achtel_Punkt()
    {
        var tabelle = new TdTable(TdTableRow.Text("x"))
        {
            Format =
            {
                Top = new TdBorder(1.5, "#FF0000"),
                InsideH = new TdBorder(0.25, "#00FF00"),
                Bottom = TdBorder.Keine,
            },
        };

        var f = ErsteTabelle(Zurueck(Dok(tabelle))).Format;

        Assert.Equal(1.5, f.Top.WidthPt, 3);
        Assert.Equal("#FF0000", f.Top.Color);
        Assert.Equal(0.25, f.InsideH.WidthPt, 3);
        Assert.False(f.Bottom.Sichtbar);
    }

    [Fact]
    public void Zellhintergrund_und_senkrechte_Ausrichtung_ueberleben()
    {
        var doc = Dok(new TdTable(new TdTableRow(
            new TdTableCell(new TdParagraph("a")) { Shading = "#FFFF00", VerticalAlign = TdVAlign.Center },
            new TdTableCell(new TdParagraph("b")) { VerticalAlign = TdVAlign.Bottom },
            TdTableCell.Text("c"))));

        var zeile = ErsteTabelle(Zurueck(doc)).Rows[0];

        Assert.Equal("#FFFF00", zeile.Cells[0].Shading);
        Assert.Equal(TdVAlign.Center, zeile.Cells[0].VerticalAlign);
        Assert.Equal(TdVAlign.Bottom, zeile.Cells[1].VerticalAlign);
        Assert.Null(zeile.Cells[2].Shading);
        Assert.Equal(TdVAlign.Top, zeile.Cells[2].VerticalAlign);
    }

    [Fact]
    public void Kopfzeile_und_Mindesthoehe_ueberleben()
    {
        var doc = Dok(new TdTable(
            new TdTableRow(TdTableCell.Text("Kopf")) { IsHeader = true, MinHeightCm = 1.2 },
            new TdTableRow(TdTableCell.Text("Inhalt"))));

        var t = ErsteTabelle(Zurueck(doc));

        Assert.True(t.Rows[0].IsHeader);
        Assert.Equal(1.2, t.Rows[0].MinHeightCm!.Value, 2);
        Assert.False(t.Rows[1].IsHeader);
        Assert.Null(t.Rows[1].MinHeightCm);
    }

    [Fact]
    public void Die_Spaltenbreiten_ueberstehen_das_Raster()
    {
        var doc = Dok(new TdTable(TdTableRow.Text("a", "b", "c"))
        {
            ColumnWidthsCm = { 3.5, 6.0, 2.25 },
        });

        var breiten = ErsteTabelle(Zurueck(doc)).ColumnWidthsCm;

        Assert.Equal(3, breiten.Count);
        Assert.Equal(3.5, breiten[0], 2);
        Assert.Equal(6.0, breiten[1], 2);
        Assert.Equal(2.25, breiten[2], 2);
    }

    /// <summary>
    /// <b>Zwei Tabellen direkt hintereinander verschmelzen in Word zu einer.</b> Dazwischen
    /// gehört ein Absatz — und der darf beim Lesen nicht als Inhalt zurückkommen, sonst
    /// stünde nach jedem Speichern eine Leerzeile mehr im Dokument.
    /// </summary>
    [Fact]
    public void Zwei_Tabellen_hintereinander_bleiben_zwei_und_erben_keine_Leerzeile()
    {
        var doc = Dok(
            new TdTable(TdTableRow.Text("erste")),
            new TdTable(TdTableRow.Text("zweite")));

        var einmal = Zurueck(doc);
        var zweimal = Zurueck(einmal);

        foreach (var stand in new[] { einmal, zweimal })
        {
            var bloecke = stand.Blocks().ToList();
            Assert.Equal(2, bloecke.Count);
            Assert.All(bloecke, b => Assert.IsType<TdTable>(b));
        }
    }

    /// <summary>
    /// Ein Absatz, den der Nutzer selbst hinter eine Tabelle gesetzt hat, ist Inhalt und
    /// bleibt — der Trennabsatz wird nur dort weggenommen, wo er hingehört. Geprüft mit
    /// einem Absatz, der etwas enthält, denn genau daran unterscheiden sich die beiden.
    /// </summary>
    [Fact]
    public void Ein_eigener_Absatz_hinter_der_Tabelle_bleibt()
    {
        var doc = Dok(
            new TdTable(TdTableRow.Text("Tabelle")),
            new TdParagraph("Danach"));

        var bloecke = Zurueck(doc).Blocks().ToList();

        Assert.Equal(2, bloecke.Count);
        Assert.IsType<TdTable>(bloecke[0]);
        Assert.Equal("Danach", bloecke[1].PlainText());
    }

    /// <summary>
    /// Endet ein **nicht letzter** Abschnitt mit einer Tabelle, trägt der Trennabsatz die
    /// Abschnittsangabe. Er darf dann trotzdem nicht als leerer Absatz zurückkommen.
    /// </summary>
    [Fact]
    public void Ein_Abschnitt_darf_mit_einer_Tabelle_enden()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(new TdTable(TdTableRow.Text("erster"))) { Page = TdPageSetup.A4.Quer() },
                new TdSection(new TdParagraph("zweiter")) { Page = TdPageSetup.A5 },
            },
        };

        var zurueck = Zurueck(doc);

        Assert.Equal(2, zurueck.Sections.Count);
        Assert.IsType<TdTable>(Assert.Single(zurueck.Sections[0].Blocks));
        Assert.True(zurueck.Sections[0].Page.IstQuerformat);
        Assert.Equal("A5", zurueck.Sections[1].Page.Name);
    }

    /// <summary>Eine Zelle darf mehr als einen Absatz und sogar eine Liste enthalten.</summary>
    [Fact]
    public void Eine_Zelle_traegt_mehrere_Bloecke()
    {
        var doc = new TdDocument
        {
            Lists = { TdListDefinition.Nummern(1) },
            Sections =
            {
                new TdSection(new TdTable(new TdTableRow(new TdTableCell(
                    new TdParagraph("erster"),
                    new TdParagraph("Punkt") { List = new TdListRef(1, 0) })))),
            },
        };

        var zelle = ErsteTabelle(Zurueck(doc)).Rows[0].Cells[0];

        Assert.Equal(2, zelle.Blocks.Count);
        Assert.Equal("erster", zelle.Blocks[0].PlainText());
        Assert.NotNull(((TdParagraph)zelle.Blocks[1]).List);
    }

    /// <summary>Eine Tabelle in einer Tabelle — DOCX kann das, und das Modell darf es nicht verlieren.</summary>
    [Fact]
    public void Eine_Tabelle_darf_in_einer_Zelle_stehen()
    {
        var doc = Dok(new TdTable(new TdTableRow(new TdTableCell(
            new TdTable(TdTableRow.Text("innen"))))));

        var aussen = ErsteTabelle(Zurueck(doc));
        var innen = Assert.IsType<TdTable>(aussen.Rows[0].Cells[0].Blocks[0]);

        Assert.Equal("innen", innen.PlainText());
    }

    // ==================== Hilfsmittel ====================

    /// <summary>
    /// Eine Tabelle, die **alles** benutzt, was Schritt 4 kann. Wer das Modell erweitert,
    /// erweitert sie mit — sonst bewacht der Roundtrip das neue Feld nicht.
    /// </summary>
    private static TdTable Beispieltabelle() => new(
        new TdTableRow(
            new TdTableCell(new TdParagraph("Überschrift über zwei")) { ColumnSpan = 2, Shading = "#DDEEFF" },
            TdTableCell.Text("Rest"))
        { IsHeader = true, MinHeightCm = 1.0 },

        new TdTableRow(
            new TdTableCell(new TdParagraph("verbunden")) { VerticalMerge = TdVerticalMerge.Restart, VerticalAlign = TdVAlign.Center },
            TdTableCell.Text("b"),
            TdTableCell.Text("c")),

        new TdTableRow(
            new TdTableCell { VerticalMerge = TdVerticalMerge.Continue },
            new TdTableCell(new TdParagraph("zwei"), new TdParagraph("Absätze")),
            new TdTableCell(new TdParagraph("f")) { VerticalAlign = TdVAlign.Bottom }))
    {
        ColumnWidthsCm = { 4.0, 5.5, 3.25 },
        Format =
        {
            Top = new TdBorder(1.5, "#112233"),
            Left = new TdBorder(0.5, "#000000"),
            Bottom = new TdBorder(1.5, "#112233"),
            Right = new TdBorder(0.5, "#000000"),
            InsideH = new TdBorder(0.25, "#888888"),
            InsideV = TdBorder.Keine,
            CellPaddingLeftCm = 0.25,
            CellPaddingRightCm = 0.25,
            CellPaddingTopCm = 0.1,
            CellPaddingBottomCm = 0.1,
        },
    };

    private static void GleicheTabelle(TdTable a, TdTable b)
    {
        Assert.Equal(a.ColumnWidthsCm.Count, b.ColumnWidthsCm.Count);
        for (int i = 0; i < a.ColumnWidthsCm.Count; i++)
            Assert.Equal(a.ColumnWidthsCm[i], b.ColumnWidthsCm[i], 2);

        GleicheLinie(a.Format.Top, b.Format.Top);
        GleicheLinie(a.Format.Left, b.Format.Left);
        GleicheLinie(a.Format.Bottom, b.Format.Bottom);
        GleicheLinie(a.Format.Right, b.Format.Right);
        GleicheLinie(a.Format.InsideH, b.Format.InsideH);
        GleicheLinie(a.Format.InsideV, b.Format.InsideV);

        Assert.Equal(a.Format.CellPaddingLeftCm, b.Format.CellPaddingLeftCm, 2);
        Assert.Equal(a.Format.CellPaddingRightCm, b.Format.CellPaddingRightCm, 2);
        Assert.Equal(a.Format.CellPaddingTopCm, b.Format.CellPaddingTopCm, 2);
        Assert.Equal(a.Format.CellPaddingBottomCm, b.Format.CellPaddingBottomCm, 2);

        Assert.Equal(a.Rows.Count, b.Rows.Count);
        for (int z = 0; z < a.Rows.Count; z++)
        {
            Assert.Equal(a.Rows[z].IsHeader, b.Rows[z].IsHeader);
            if (a.Rows[z].MinHeightCm is { } h) Assert.Equal(h, b.Rows[z].MinHeightCm!.Value, 2);
            else Assert.Null(b.Rows[z].MinHeightCm);

            Assert.Equal(a.Rows[z].Cells.Count, b.Rows[z].Cells.Count);
            for (int s = 0; s < a.Rows[z].Cells.Count; s++)
            {
                var az = a.Rows[z].Cells[s];
                var bz = b.Rows[z].Cells[s];
                Assert.Equal(az.ColumnSpan, bz.ColumnSpan);
                Assert.Equal(az.VerticalMerge, bz.VerticalMerge);
                Assert.Equal(az.Shading, bz.Shading);
                Assert.Equal(az.VerticalAlign, bz.VerticalAlign);
                Assert.Equal(az.PlainText(), bz.PlainText());
                Assert.Equal(az.Blocks.Count, bz.Blocks.Count);
            }
        }
    }

    private static void GleicheLinie(TdBorder a, TdBorder b)
    {
        Assert.Equal(a.WidthPt, b.WidthPt, 3);
        if (a.Sichtbar) Assert.Equal(a.Color, b.Color);
    }
}
