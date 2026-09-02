using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdTableEdit"/>, zweite Hälfte — <b>der Tabellenentwurf</b> (HANDOFF §4.90):
/// Rahmen, Füllung, Kopfzeile, Zellabstand, Spaltenbreite, verbinden und teilen.
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Jeder dieser Handgriffe baut die Tabelle **neu** und
/// hängt den Tausch in den Verlauf (§4.32). Zwei Fehler liegen dabei nahe und sind beide
/// unsichtbar, bis jemand Strg+Z drückt: die alte Tabelle an Ort und Stelle umzustellen (dann
/// ändert sich die Sicherung mit), und beim Verbinden den Inhalt der Nachbarzelle
/// wegzuwerfen — <b>stiller Datenverlust, den kein Bau und keine Anzeige meldet</b>.
/// </para>
/// <para>
/// <b>Kein Umbruch und keine Schrift</b>, wie überall in diesen Wächtern: Die Rechnung läuft
/// auf jedem Rechner gleich.
/// </para>
/// </summary>
public sealed class TabellenEntwurfTests
{
    // ==================== Hilfsmittel ====================

    private static TdDocument Mit(TdTable tabelle) =>
        new() { Sections = { new TdSection(new TdParagraph("davor"), tabelle) } };

    private static TdTable Zwei() =>
        new(TdTableRow.Text("a", "b"), TdTableRow.Text("c", "d"));

    /// <summary>Eine Auswahl in der Zelle (<paramref name="zeile"/>, <paramref name="spalte"/>).</summary>
    private static TdSelection In(TdDocument doc, int zeile, int spalte)
    {
        // Absatz 0 ist „davor"; danach zählen die Zellabsätze der Reihe nach.
        var tabelle = doc.Blocks().OfType<TdTable>().First();

        int nummer = 1;
        for (int z = 0; z < tabelle.Rows.Count; z++)
            for (int s = 0; s < tabelle.Rows[z].Cells.Count; s++)
            {
                if (z == zeile && s == spalte)
                    return new TdSelection(new TdPosition(nummer, 0, 0));

                nummer += tabelle.Rows[z].Cells[s].Blocks.OfType<TdParagraph>().Count();
            }

        throw new ArgumentOutOfRangeException(nameof(zeile), "Zelle gibt es nicht.");
    }

    private static TdTable Danach(TdDocument doc) => doc.Blocks().OfType<TdTable>().First();

    // ==================== Rahmen ====================

    /// <summary>
    /// <b>„Nur außen" lässt innen stehen, was dort steht.</b> Sonst wäre jeder Rahmenbefehl
    /// heimlich ein „alle", und wer die Innenlinien absichtlich weggenommen hat, bekäme sie
    /// beim nächsten Klick auf den Außenrahmen zurück.
    /// </summary>
    [Fact]
    public void Aussen_laesst_die_Innenlinien_in_Ruhe()
    {
        var tabelle = Zwei();
        tabelle.Format.InsideH = TdBorder.Keine;
        tabelle.Format.InsideV = TdBorder.Keine;
        var doc = Mit(tabelle);

        TdTableEdit.Rahmen(doc, In(doc, 0, 0), TdTableEdit.Rahmenwahl.Aussen, 2, "#FF0000")!
            .Anwenden();

        var neu = Danach(doc);
        Assert.Equal(2, neu.Format.Top.WidthPt);
        Assert.Equal("#FF0000", neu.Format.Top.Color);
        Assert.Equal(TdBorder.Keine, neu.Format.InsideH);
        Assert.Equal(TdBorder.Keine, neu.Format.InsideV);
    }

    /// <summary>„Innen" fasst den Außenrahmen nicht an — die Gegenprobe.</summary>
    [Fact]
    public void Innen_laesst_den_Aussenrahmen_in_Ruhe()
    {
        var doc = Mit(Zwei());
        var vorher = Danach(doc).Format.Top;

        TdTableEdit.Rahmen(doc, In(doc, 0, 0), TdTableEdit.Rahmenwahl.Innen, 3, "#00FF00")!
            .Anwenden();

        var neu = Danach(doc);
        Assert.Equal(vorher, neu.Format.Top);
        Assert.Equal(3, neu.Format.InsideV.WidthPt);
    }

    /// <summary>„Keine" räumt alle sechs Kanten ab.</summary>
    [Fact]
    public void Keine_nimmt_alle_sechs_Kanten()
    {
        var doc = Mit(Zwei());

        TdTableEdit.Rahmen(doc, In(doc, 0, 0), TdTableEdit.Rahmenwahl.Keine, 1, "#000000")!
            .Anwenden();

        var f = Danach(doc).Format;
        Assert.All(
            new[] { f.Top, f.Left, f.Bottom, f.Right, f.InsideH, f.InsideV },
            kante => Assert.Equal(TdBorder.Keine, kante));
    }

    /// <summary>Ein Rahmenbefehl außerhalb einer Tabelle tut nichts.</summary>
    [Fact]
    public void Ausserhalb_einer_Tabelle_passiert_nichts()
    {
        var doc = Mit(Zwei());
        var draussen = new TdSelection(new TdPosition(0, 0, 0));

        Assert.Null(TdTableEdit.Rahmen(
            doc, draussen, TdTableEdit.Rahmenwahl.Alle, 1, "#000000"));
    }

    // ==================== Füllung ====================

    [Fact]
    public void Die_Fuellung_trifft_die_Zelle_unter_der_Marke()
    {
        var doc = Mit(Zwei());

        TdTableEdit.Fuellung(doc, In(doc, 1, 0), "#DBEAFE")!.Anwenden();

        var neu = Danach(doc);
        Assert.Equal("#DBEAFE", neu.Rows[1].Cells[0].Shading);
        Assert.Null(neu.Rows[0].Cells[0].Shading);
        Assert.Null(neu.Rows[1].Cells[1].Shading);
    }

    /// <summary><c>null</c> nimmt die Füllung wieder weg.</summary>
    [Fact]
    public void Ohne_Farbe_faellt_die_Fuellung_weg()
    {
        var doc = Mit(Zwei());
        TdTableEdit.Fuellung(doc, In(doc, 0, 1), "#DBEAFE")!.Anwenden();

        TdTableEdit.Fuellung(doc, In(doc, 0, 1), null)!.Anwenden();

        Assert.Null(Danach(doc).Rows[0].Cells[1].Shading);
    }

    /// <summary>Dieselbe Farbe zweimal ist keine Änderung — und kommt nicht in den Verlauf.</summary>
    [Fact]
    public void Dieselbe_Fuellung_ist_keine_Aenderung()
    {
        var doc = Mit(Zwei());
        TdTableEdit.Fuellung(doc, In(doc, 0, 0), "#DBEAFE")!.Anwenden();

        Assert.Null(TdTableEdit.Fuellung(doc, In(doc, 0, 0), "#DBEAFE"));
    }

    // ==================== Kopfzeile ====================

    /// <summary>
    /// <b>Die Kopfzeile ist eine Auskunft und keine Formatierung</b> — sie wiederholt sich auf
    /// jeder Folgeseite (§4.19) und geht als <c>w:tblHeader</c> nach DOCX.
    /// </summary>
    [Fact]
    public void Die_Kopfzeile_sitzt_auf_der_ersten_Zeile()
    {
        var doc = Mit(Zwei());

        TdTableEdit.Kopfzeile(doc, In(doc, 1, 1), an: true)!.Anwenden();

        Assert.True(Danach(doc).Rows[0].IsHeader);
        Assert.False(Danach(doc).Rows[1].IsHeader);
    }

    [Fact]
    public void Die_Kopfzeile_laesst_sich_wieder_abschalten()
    {
        var doc = Mit(Zwei());
        TdTableEdit.Kopfzeile(doc, In(doc, 0, 0), an: true)!.Anwenden();

        TdTableEdit.Kopfzeile(doc, In(doc, 0, 0), an: false)!.Anwenden();

        Assert.False(Danach(doc).Rows[0].IsHeader);
    }

    [Fact]
    public void Zweimal_dieselbe_Kopfzeile_ist_keine_Aenderung()
    {
        var doc = Mit(Zwei());

        Assert.Null(TdTableEdit.Kopfzeile(doc, In(doc, 0, 0), an: false));
    }

    // ==================== Zellabstand ====================

    [Fact]
    public void Der_Zellabstand_gilt_links_und_rechts()
    {
        var doc = Mit(Zwei());

        TdTableEdit.Zellabstand(doc, In(doc, 0, 0), 0.5)!.Anwenden();

        var f = Danach(doc).Format;
        Assert.Equal(0.5, f.CellPaddingLeftCm, 3);
        Assert.Equal(0.5, f.CellPaddingRightCm, 3);
    }

    /// <summary>Ein unsinniger Wert wird begrenzt und nicht durchgereicht.</summary>
    [Fact]
    public void Der_Zellabstand_bleibt_in_seinen_Grenzen()
    {
        var doc = Mit(Zwei());

        TdTableEdit.Zellabstand(doc, In(doc, 0, 0), 99)!.Anwenden();

        Assert.True(Danach(doc).Format.CellPaddingLeftCm <= 2);
    }

    // ==================== Spaltenbreite ====================

    /// <summary>
    /// Beim Setzen werden <b>alle</b> Breiten aufgefüllt. Eine Liste mit einer Lücke wäre eine
    /// Tabelle, in der die dritte Spalte die Breite der zweiten trägt.
    /// </summary>
    [Fact]
    public void Eine_gesetzte_Breite_fuellt_die_ganze_Liste()
    {
        var doc = Mit(Zwei());

        TdTableEdit.Spaltenbreite(doc, In(doc, 0, 1), 5)!.Anwenden();

        var breiten = Danach(doc).ColumnWidthsCm;
        Assert.Equal(2, breiten.Count);
        Assert.Equal(5, breiten[1], 3);
        Assert.True(breiten[0] > 0);
    }

    /// <summary><c>null</c> gibt alles wieder frei — das ist das AutoAnpassen.</summary>
    [Fact]
    public void Ohne_Angabe_teilt_der_Umbruch_wieder_selbst()
    {
        var doc = Mit(Zwei());
        TdTableEdit.Spaltenbreite(doc, In(doc, 0, 0), 4)!.Anwenden();

        TdTableEdit.Spaltenbreite(doc, In(doc, 0, 0), null)!.Anwenden();

        Assert.Empty(Danach(doc).ColumnWidthsCm);
    }

    // ==================== Verbinden und teilen ====================

    [Fact]
    public void Verbinden_nimmt_die_rechte_Nachbarin()
    {
        var doc = Mit(Zwei());

        TdTableEdit.ZellenVerbinden(doc, In(doc, 0, 0))!.Anwenden();

        var neu = Danach(doc);
        Assert.Single(neu.Rows[0].Cells);
        Assert.Equal(2, neu.Rows[0].Cells[0].ColumnSpan);
        Assert.Equal(2, neu.Rows[1].Cells.Count);   // die zweite Zeile bleibt, wie sie war
    }

    /// <summary>
    /// <b>Der Inhalt der Nachbarzelle geht nicht verloren.</b> Das ist der Fehler, den kein
    /// Bau und keine Anzeige meldet: Die Tabelle sieht danach richtig aus, und der Text ist weg.
    /// </summary>
    [Fact]
    public void Verbinden_rettet_den_Inhalt_der_Nachbarzelle()
    {
        var doc = Mit(Zwei());

        TdTableEdit.ZellenVerbinden(doc, In(doc, 0, 0))!.Anwenden();

        var texte = Danach(doc).Rows[0].Cells[0].Blocks
            .OfType<TdParagraph>().Select(p => p.PlainText()).ToList();

        Assert.Contains("a", texte);
        Assert.Contains("b", texte);
    }

    /// <summary>
    /// <b>Ein leerer Absatz wandert nicht mit</b> — sonst stünde nach jedem Verbinden eine
    /// Leerzeile in der Zelle, die niemand eingegeben hat.
    /// </summary>
    [Fact]
    public void Verbinden_schleppt_keine_leere_Zelle_mit()
    {
        var doc = Mit(new TdTable(new TdTableRow(
            new TdTableCell(new TdParagraph("a")),
            new TdTableCell(new TdParagraph()))));

        TdTableEdit.ZellenVerbinden(doc, In(doc, 0, 0))!.Anwenden();

        Assert.Single(Danach(doc).Rows[0].Cells[0].Blocks);
    }

    /// <summary>Die letzte Zelle einer Zeile hat keine Nachbarin.</summary>
    [Fact]
    public void Die_letzte_Zelle_laesst_sich_nicht_verbinden()
    {
        var doc = Mit(Zwei());

        Assert.Null(TdTableEdit.ZellenVerbinden(doc, In(doc, 0, 1)));
    }

    /// <summary>Mehrmals gedrückt verbindet weiter — damit ist jede Breite erreichbar.</summary>
    [Fact]
    public void Mehrmals_verbinden_zieht_weiter()
    {
        var doc = Mit(new TdTable(TdTableRow.Text("a", "b", "c")));

        TdTableEdit.ZellenVerbinden(doc, In(doc, 0, 0))!.Anwenden();
        TdTableEdit.ZellenVerbinden(doc, In(doc, 0, 0))!.Anwenden();

        Assert.Single(Danach(doc).Rows[0].Cells);
        Assert.Equal(3, Danach(doc).Rows[0].Cells[0].ColumnSpan);
    }

    /// <summary>Teilen ist die Gegenbewegung — die Spaltenzahl steht danach wieder.</summary>
    [Fact]
    public void Teilen_macht_das_Verbinden_rueckgaengig()
    {
        var doc = Mit(Zwei());
        TdTableEdit.ZellenVerbinden(doc, In(doc, 0, 0))!.Anwenden();

        TdTableEdit.ZelleTeilen(doc, In(doc, 0, 0))!.Anwenden();

        var neu = Danach(doc);
        Assert.Equal(2, neu.Rows[0].Cells.Count);
        Assert.Equal(1, neu.Rows[0].Cells[0].ColumnSpan);
        Assert.Equal(2, neu.Spaltenzahl());
    }

    /// <summary>
    /// <b>Eine Zelle, die nichts überspannt, lässt sich nicht teilen.</b> Sonst baute ein Klick
    /// die ganze Tabelle um statt einer Zelle — dafür gibt es „Spalte einfügen".
    /// </summary>
    [Fact]
    public void Eine_gewoehnliche_Zelle_laesst_sich_nicht_teilen()
    {
        var doc = Mit(Zwei());

        Assert.Null(TdTableEdit.ZelleTeilen(doc, In(doc, 0, 0)));
    }

    // ==================== Der Verlauf ====================

    /// <summary>
    /// <b>Die Sicherung im Verlauf darf sich nicht mitändern</b> (§4.32) — der Fehler, der
    /// erst beim ersten Strg+Z auffällt. Ein Rücklauf muss die Tabelle so herstellen, wie sie
    /// war.
    /// </summary>
    [Fact]
    public void Ein_Ruecklauf_stellt_die_alte_Tabelle_her()
    {
        var doc = Mit(Zwei());

        var schritt = TdTableEdit.Rahmen(
            doc, In(doc, 0, 0), TdTableEdit.Rahmenwahl.Keine, 1, "#000000")!;
        schritt.Anwenden();
        Assert.Equal(TdBorder.Keine, Danach(doc).Format.Top);

        schritt.Gegenbewegung.Anwenden();

        Assert.NotEqual(TdBorder.Keine, Danach(doc).Format.Top);
    }

    /// <summary>Dasselbe für das Verbinden — hier hängt Inhalt daran.</summary>
    [Fact]
    public void Ein_Ruecklauf_bringt_die_verbundene_Zelle_zurueck()
    {
        var doc = Mit(Zwei());

        var schritt = TdTableEdit.ZellenVerbinden(doc, In(doc, 0, 0))!;
        schritt.Anwenden();

        schritt.Gegenbewegung.Anwenden();

        var neu = Danach(doc);
        Assert.Equal(2, neu.Rows[0].Cells.Count);
        Assert.Equal("a", neu.Rows[0].Cells[0].Blocks.OfType<TdParagraph>().First().PlainText());
        Assert.Equal("b", neu.Rows[0].Cells[1].Blocks.OfType<TdParagraph>().First().PlainText());
    }
}
