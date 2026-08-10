using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdChartLayout"/> — aus den Zahlen eines <see cref="TdChart"/> wird ein Bild
/// (HANDOFF §4.25).
///
/// <para>
/// <b>Hier steht kein einziges Pixel.</b> Der Plan ist reine Rechnung in Zentimetern — genau
/// deshalb liegt er in <c>Core/Text/</c> und nicht im Zeichner: Achsenteilung, Farbvergabe und
/// die Frage, ob eine Legende nötig ist, lassen sich als Zahl prüfen, im Zeichner ließen sie
/// sich nur noch über Pixel prüfen. Der heutige Editor rechnet sie mitten in seiner
/// Zeichenroutine (<c>ChartDialog</c>), und deshalb sieht dort niemand, wie er rundet.
/// </para>
/// <para>
/// Was daraus Pixel macht, bewacht <see cref="ZeichnerTests"/> — dort und nur dort wird auf
/// Farbe gesehen.
/// </para>
/// </summary>
public sealed class DiagrammTests
{
    /// <summary>Ein Diagramm mit einer Reihe je übergebenem Werte-Satz.</summary>
    private static TdChart Bauen(TdChartKind art, params double[][] reihen)
    {
        var d = new TdChart(art, widthCm: 12, heightCm: 8);
        foreach (var werte in reihen) d.Series.Add(new TdChartSeries("", werte));
        return d;
    }

    private static readonly string[] ZweiFarben = ["#111111", "#222222"];

    // ==================== Die Achse ====================

    /// <summary>
    /// <b>Die Teilung ist eine schöne Zahl</b> — 1, 2, 5 oder 10 mal einer Zehnerpotenz. Ohne
    /// das stünde an der Achse „1,75", und niemand liest das als Teilung.
    /// </summary>
    [Theory]
    [InlineData(1.75, 2)]
    [InlineData(0.75, 1)]
    [InlineData(3, 5)]
    [InlineData(6, 10)]
    [InlineData(120, 200)]
    [InlineData(0.03, 0.05)]
    public void Die_Achsenteilung_ist_eine_schoene_Zahl(double roh, double erwartet) =>
        Assert.Equal(erwartet, TdChartLayout.SchoenerSchritt(roh), 6);

    /// <summary>Werte 3, 4, 7 → Achse von 0 bis 8 in Zweierschritten.</summary>
    [Fact]
    public void Die_Achse_reicht_bis_zur_naechsten_Stufe_ueber_dem_groessten_Wert()
    {
        var achse = TdChartLayout.AchseFuer([4, 7, 3]);

        Assert.Equal(0, achse.Min, 6);
        Assert.Equal(8, achse.Max, 6);
        Assert.Equal(2, achse.Schritt, 6);
        Assert.Equal(4, achse.Stufen);
    }

    /// <summary>
    /// <b>Die Null ist immer dabei</b>, auch wenn kein Wert in ihre Nähe kommt. Eine Säule, die
    /// bei 98 anfängt und bei 100 aufhört, sieht doppelt so hoch aus wie eine bis 99 — die
    /// bekannteste Art, mit einem richtigen Diagramm etwas Falsches zu behaupten.
    /// </summary>
    [Fact]
    public void Die_Achse_faengt_bei_null_an_auch_wenn_die_Werte_weit_darueber_liegen()
    {
        var achse = TdChartLayout.AchseFuer([98, 100]);

        Assert.Equal(0, achse.Min, 6);
        Assert.True(achse.Max >= 100);
    }

    /// <summary>
    /// <b>Bei negativen Werten liegt die Null auf einer Stufe</b> — aus ihr wachsen die Säulen.
    /// Läge sie zwischen zwei Stufen, stünde die Grundlinie aller Säulen dort, wo keine Linie
    /// ist.
    /// </summary>
    [Fact]
    public void Negative_Werte_bekommen_eine_Nulllinie_auf_einer_Stufe()
    {
        var achse = TdChartLayout.AchseFuer([-3, 5]);

        Assert.True(achse.Min < 0, "Die Achse muss unter null reichen.");
        Assert.True(achse.Max >= 5);
        Assert.Contains(Enumerable.Range(0, achse.Stufen + 1), i => Math.Abs(achse.Wert(i)) < 1e-9);
    }

    /// <summary>
    /// Lauter Nullen sind kein Fehler, sondern ein Diagramm ohne Ausschlag. <b>Eine Achse muss
    /// es trotzdem geben</b> — sonst teilte die Umrechnung durch null, und das ist die Sorte
    /// Fehler, die erst beim ersten leeren Datensatz auffällt.
    /// </summary>
    [Fact]
    public void Lauter_Nullen_ergeben_trotzdem_eine_brauchbare_Achse()
    {
        var achse = TdChartLayout.AchseFuer([0, 0, 0]);

        Assert.True(achse.Max > achse.Min);
        Assert.InRange(achse.Anteil(0), 0, 1);
    }

    /// <summary>
    /// Ein Wert außerhalb der Achse wird beschnitten. Er kann nur aus einer Reihe kommen, die
    /// nach dem Rechnen der Achse gewachsen ist — ein Balken, der aus dem Diagramm herausragt,
    /// sähe aus wie ein Zeichenfehler.
    /// </summary>
    [Fact]
    public void Ein_Wert_ausserhalb_der_Achse_wird_beschnitten()
    {
        var achse = TdChartLayout.AchseFuer([4]);

        Assert.Equal(1, achse.Anteil(999), 6);
        Assert.Equal(0, achse.Anteil(-999), 6);
    }

    /// <summary>
    /// <b>Die Zahl an der Achse steht fest und nicht in der Kultur des Rechners</b> — dieselbe
    /// Entscheidung wie beim Datumsmuster (§4.20). Ein Dokument, dessen Diagramm hier „1,5" und
    /// dort „1.5" zeigt, ist nicht mehr dasselbe Dokument.
    /// </summary>
    [Fact]
    public void Die_Achsenzahl_haengt_nicht_an_der_Kultur_des_Rechners()
    {
        var vorher = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal("1.5", TdChartLayout.Zahl(1.5));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = vorher;
        }
    }

    // ==================== Wann es kein Bild gibt ====================

    /// <summary>
    /// <b>Ohne Reihen gibt es kein Diagramm</b> — und dann bleibt der Platzhalterkasten stehen
    /// (§4.24). Ein leeres Achsenkreuz sagte „hier ist alles in Ordnung".
    /// </summary>
    [Fact]
    public void Ohne_Zahlen_bleibt_der_Plan_leer() =>
        Assert.True(TdChartLayout.Rechnen(new TdChart(TdChartKind.Column, 6, 4)).IstLeer);

    /// <summary>Ein Kuchen aus lauter Nullen hat keine Anteile — auch das ist kein Bild.</summary>
    [Fact]
    public void Ein_Kuchen_aus_lauter_Nullen_bleibt_leer() =>
        Assert.True(TdChartLayout.Rechnen(Bauen(TdChartKind.Pie, [0, 0])).IstLeer);

    /// <summary>
    /// <b>Unter drei Ecken gibt es kein Netz.</b> Zwei Kategorien ergäben eine Strecke, die wie
    /// ein Zeichenfehler aussieht.
    /// </summary>
    [Fact]
    public void Das_Netz_braucht_drei_Ecken()
    {
        Assert.True(TdChartLayout.Rechnen(Bauen(TdChartKind.Radar, [4, 6])).IstLeer);
        Assert.False(TdChartLayout.Rechnen(Bauen(TdChartKind.Radar, [4, 6, 5])).IstLeer);
    }

    /// <summary>Ein Kasten ohne Maß kann nichts enthalten — und darf nicht durch null teilen.</summary>
    [Fact]
    public void Ein_Diagramm_ohne_Maß_bleibt_leer()
    {
        var d = Bauen(TdChartKind.Column, [1, 2]);
        d.WidthCm = 0;

        Assert.True(TdChartLayout.Rechnen(d).IstLeer);
    }

    // ==================== Säulen und Balken ====================

    /// <summary>
    /// Drei Werte, drei Säulen — nebeneinander, und die Höhe folgt dem Wert. <b>Geprüft wird
    /// das Verhältnis</b>, nicht die absolute Höhe: die hängt an den Rändern, das Verhältnis
    /// an der Achse.
    /// </summary>
    [Fact]
    public void Die_Saeulen_stehen_nebeneinander_und_ihre_Hoehe_folgt_dem_Wert()
    {
        var plan = TdChartLayout.Rechnen(Bauen(TdChartKind.Column, [1, 2, 3]));

        Assert.Equal(3, plan.Flaechen.Count);
        Assert.True(plan.Flaechen[0].Kasten.XCm < plan.Flaechen[1].Kasten.XCm);
        Assert.True(plan.Flaechen[1].Kasten.XCm < plan.Flaechen[2].Kasten.XCm);

        Assert.Equal(2.0, plan.Flaechen[1].Kasten.HeightCm / plan.Flaechen[0].Kasten.HeightCm, 3);
        Assert.Equal(3.0, plan.Flaechen[2].Kasten.HeightCm / plan.Flaechen[0].Kasten.HeightCm, 3);
    }

    /// <summary>
    /// <b>Alle Säulen stehen auf derselben Linie</b> — der Nulllinie. Sie ist die einzige
    /// gemeinsame Bezugslinie; ohne sie ließen sich zwei Säulen nicht vergleichen.
    /// </summary>
    [Fact]
    public void Alle_Saeulen_stehen_auf_der_Nulllinie()
    {
        var plan = TdChartLayout.Rechnen(Bauen(TdChartKind.Column, [1, 2, 3]));

        double linie = plan.Flaechen[0].Kasten.UntenCm;
        Assert.All(plan.Flaechen, f => Assert.Equal(linie, f.Kasten.UntenCm, 6));
    }

    /// <summary>
    /// <b>Ein negativer Wert hängt unter der Nulllinie</b>, statt am Boden abgeschnitten zu
    /// werden. Abgeschnitten sähe er aus wie eine Null — eine Zahl, die niemand eingegeben hat.
    /// </summary>
    [Fact]
    public void Eine_negative_Saeule_haengt_unter_der_Nulllinie()
    {
        var plan = TdChartLayout.Rechnen(Bauen(TdChartKind.Column, [4, -2]));
        var achse = Assert.NotNull(plan.Achse);

        double nulllinie = plan.Flaeche.UntenCm - plan.Flaeche.HeightCm * achse.Anteil(0);

        Assert.Equal(nulllinie, plan.Flaechen[0].Kasten.UntenCm, 6);   // 4 wächst nach oben
        Assert.Equal(nulllinie, plan.Flaechen[1].Kasten.YCm, 6);       // −2 hängt nach unten
    }

    /// <summary>
    /// <b>Ein Balkendiagramm ist kein gedrehtes Säulendiagramm.</b> Die Balken fangen alle an
    /// derselben Stelle an und werden nach rechts länger; die Kategorien stehen links.
    /// </summary>
    [Fact]
    public void Ein_Balken_faengt_links_an_und_waechst_nach_rechts()
    {
        var plan = TdChartLayout.Rechnen(Bauen(TdChartKind.Bar, [1, 2, 3]));

        Assert.Equal(3, plan.Flaechen.Count);
        double links = plan.Flaechen[0].Kasten.XCm;
        Assert.All(plan.Flaechen, f => Assert.Equal(links, f.Kasten.XCm, 6));

        Assert.Equal(3.0, plan.Flaechen[2].Kasten.WidthCm / plan.Flaechen[0].Kasten.WidthCm, 3);
        Assert.True(plan.Flaechen[0].Kasten.YCm < plan.Flaechen[2].Kasten.YCm);
    }

    /// <summary>
    /// <b>Eine kürzere Reihe hat an dieser Kategorie keine Säule</b> — und nicht eine der Höhe
    /// null. Eine Säule ohne Höhe behauptete, dort stehe der Wert 0; dort steht aber gar nichts.
    /// </summary>
    [Fact]
    public void Eine_kuerzere_Reihe_bekommt_dort_keine_Saeule()
    {
        var d = Bauen(TdChartKind.Column, [1, 2, 3], [1]);
        var plan = TdChartLayout.Rechnen(d);

        // Vier Säulen (3 + 1) und zwei Legendenkästchen.
        Assert.Equal(4 + 2, plan.Flaechen.Count);
    }

    // ==================== Farben ====================

    /// <summary>
    /// <b>Eine Reihe: Farbe je Kategorie. Zwei Reihen: Farbe je Reihe.</b> Die Unterscheidung
    /// steht am Diagramm (<see cref="TdChart.FarbeJeElement"/>, §4.21) und gilt damit auch für
    /// DOCX — zwei Antworten auf dieselbe Frage wären zwei Diagramme.
    /// </summary>
    [Fact]
    public void Eine_Reihe_faerbt_je_Kategorie_zwei_Reihen_je_Reihe()
    {
        var eine = Bauen(TdChartKind.Column, [1, 2]);
        eine.Palette.AddRange(ZweiFarben);
        var planEine = TdChartLayout.Rechnen(eine);

        Assert.Equal("#111111", planEine.Flaechen[0].Farbe);
        Assert.Equal("#222222", planEine.Flaechen[1].Farbe);

        var zwei = Bauen(TdChartKind.Column, [1, 2], [3, 4]);
        zwei.Palette.AddRange(ZweiFarben);
        var planZwei = TdChartLayout.Rechnen(zwei);

        // Die Reihenfolge ist Kategorie außen, Reihe innen: (K1/R1), (K1/R2), (K2/R1), (K2/R2).
        Assert.Equal("#111111", planZwei.Flaechen[0].Farbe);
        Assert.Equal("#222222", planZwei.Flaechen[1].Farbe);
        Assert.Equal("#111111", planZwei.Flaechen[2].Farbe);
        Assert.Equal("#222222", planZwei.Flaechen[3].Farbe);
    }

    /// <summary>
    /// <b>Eine Kurve wechselt nicht alle zwei Punkte die Farbe</b> — dann wäre sie keine Kurve
    /// mehr. Auch bei einer einzigen Reihe bleibt es bei einer Farbe (§4.21).
    /// </summary>
    [Fact]
    public void Eine_Kurve_hat_eine_Farbe_und_nicht_eine_je_Punkt()
    {
        var d = Bauen(TdChartKind.Line, [1, 2, 3]);
        d.Palette.AddRange(ZweiFarben);

        var zug = Assert.Single(TdChartLayout.Rechnen(d).Zuege);
        Assert.Equal("#111111", zug.Farbe);
    }

    /// <summary>
    /// <b>Die Palette steht am Diagramm und nicht in der Oberfläche</b> (§4.21) — der heutige
    /// Editor hält sie in einem statischen Feld des Dialogs, und beim nächsten Start ist sie weg.
    /// </summary>
    [Fact]
    public void Die_Palette_kommt_vom_Diagramm()
    {
        var d = Bauen(TdChartKind.Pie, [1, 1, 1]);
        d.Palette.Add("#ABCDEF");

        // Eine Farbe, drei Stücke: sie läuft um, statt dass zwei Stücke keine Farbe bekommen.
        Assert.All(TdChartLayout.Rechnen(d).Stuecke, s => Assert.Equal("#ABCDEF", s.Farbe));
    }

    // ==================== Linie, Punkt, Punkt+Linie ====================

    /// <summary>
    /// <b>Die drei unterscheiden sich nur darin, ob eine Linie und ob Marken dastehen</b> —
    /// genau wie in DOCX, wo alle drei ein <c>c:lineChart</c> sind (§4.21). Genau daran erkennt
    /// der Leser sie zurück.
    /// </summary>
    [Theory]
    [InlineData(TdChartKind.Line, true, false)]
    [InlineData(TdChartKind.Scatter, false, true)]
    [InlineData(TdChartKind.ScatterLine, true, true)]
    public void Linie_Punkt_und_Punkt_mit_Linie_unterscheiden_sich_nur_darin(
        TdChartKind art, bool linie, bool marken)
    {
        var zug = Assert.Single(TdChartLayout.Rechnen(Bauen(art, [1, 2, 3])).Zuege);

        Assert.Equal(linie, zug.Linie);
        Assert.Equal(marken, zug.Marken);
        Assert.Equal(3, zug.Punkte.Count);
        Assert.False(zug.Geschlossen);
    }

    /// <summary>Die Punkte einer Kurve stehen in der Mitte ihres Fachs, von links nach rechts.</summary>
    [Fact]
    public void Die_Punkte_einer_Kurve_laufen_von_links_nach_rechts()
    {
        var plan = TdChartLayout.Rechnen(Bauen(TdChartKind.Line, [1, 2, 3]));
        var zug = Assert.Single(plan.Zuege);

        double fach = plan.Flaeche.WidthCm / 3;
        for (int i = 0; i < 3; i++)
            Assert.Equal(plan.Flaeche.XCm + fach * (i + 0.5), zug.Punkte[i].XCm, 6);

        // Größerer Wert heißt weiter oben — und „oben" ist ein *kleineres* Y.
        Assert.True(zug.Punkte[2].YCm < zug.Punkte[0].YCm);
    }

    // ==================== Kuchen ====================

    /// <summary>
    /// Je Wert ein Stück, zusammen ein voller Kreis, und das erste fängt oben an. Alles andere
    /// ließe den Kuchen schief aussehen, ohne dass man sagen könnte, warum.
    /// </summary>
    [Fact]
    public void Der_Kuchen_ist_zusammen_ein_voller_Kreis_und_faengt_oben_an()
    {
        var plan = TdChartLayout.Rechnen(Bauen(TdChartKind.Pie, [1, 1, 2]));

        Assert.Equal(3, plan.Stuecke.Count);
        Assert.Equal(-90, plan.Stuecke[0].StartGrad, 6);
        Assert.Equal(360, plan.Stuecke.Sum(s => s.SpanGrad), 6);

        // Der doppelte Wert ist das doppelte Stück.
        Assert.Equal(2.0, plan.Stuecke[2].SpanGrad / plan.Stuecke[0].SpanGrad, 6);
    }

    /// <summary>
    /// <b>Ein Kuchen zeigt nur die erste Reihe.</b> Er zeigt Anteile an einem Ganzen; zwei
    /// Ganze nebeneinander wären zwei Kuchen. Word liest ihn genauso (§4.21).
    /// </summary>
    [Fact]
    public void Der_Kuchen_nimmt_nur_die_erste_Reihe()
    {
        var plan = TdChartLayout.Rechnen(Bauen(TdChartKind.Pie, [1, 1], [5, 5, 5]));

        Assert.Equal(2, plan.Stuecke.Count);
    }

    /// <summary>
    /// Der Kuchen führt seine Legende immer mit — <b>die Anteile stehen darin</b>, und an den
    /// Stücken ist kein Platz dafür. Deshalb sagt <see cref="TdChart.ShowLegend"/> beim Kuchen
    /// „nein": die Reihen-Legende ist gemeint, nicht diese.
    /// </summary>
    [Fact]
    public void Der_Kuchen_beschriftet_seine_Stuecke_mit_Anteilen()
    {
        var d = Bauen(TdChartKind.Pie, [1, 3]);
        d.Categories.AddRange(["Apfel", "Birne"]);

        var plan = TdChartLayout.Rechnen(d);

        Assert.False(d.ShowLegend);
        Assert.Contains(plan.Schriften, s => s.Text == "Apfel · 25%");
        Assert.Contains(plan.Schriften, s => s.Text == "Birne · 75%");
    }

    // ==================== Netz ====================

    /// <summary>
    /// Das Netz bekommt eine Speiche je Kategorie, Ringe je Achsenstufe und je Reihe ein
    /// **geschlossenes** Polygon. Offen wäre es eine Kurve, die nicht dorthin zurückkommt, wo
    /// sie angefangen hat.
    /// </summary>
    [Fact]
    public void Das_Netz_hat_Speichen_Ringe_und_geschlossene_Polygone()
    {
        var d = Bauen(TdChartKind.Radar, [4, 6, 5, 3], [2, 2, 2, 2]);
        var plan = TdChartLayout.Rechnen(d);
        var achse = Assert.NotNull(plan.Achse);

        Assert.Equal(4, plan.Striche.Count);                       // vier Speichen
        Assert.Equal(achse.Stufen + 2, plan.Zuege.Count);          // Ringe + zwei Reihen
        Assert.All(plan.Zuege, z => Assert.True(z.Geschlossen));
        Assert.All(plan.Zuege, z => Assert.Equal(4, z.Punkte.Count));
    }

    /// <summary>
    /// <b>Eine fehlende Ecke wird zur Achsenuntergrenze und nicht ausgelassen.</b> Ein Polygon
    /// mit weniger Ecken als das Netz wäre ein anderes Vieleck und stünde an ganz anderer Stelle.
    /// </summary>
    [Fact]
    public void Eine_fehlende_Ecke_verschiebt_das_Netzpolygon_nicht()
    {
        var d = Bauen(TdChartKind.Radar, [4, 6, 5], [2, 2]);
        var plan = TdChartLayout.Rechnen(d);

        Assert.All(plan.Zuege, z => Assert.Equal(3, z.Punkte.Count));
    }

    // ==================== Legende und Beschriftung ====================

    /// <summary>
    /// <b>Eine Legende hat nur etwas zu sagen, wenn es mehr als eine Reihe gibt</b> (§4.21).
    /// Bei einer Reihe erklärte sie eine Farbe, die ohnehin die einzige ist.
    /// </summary>
    [Fact]
    public void Eine_Legende_steht_nur_bei_mehreren_Reihen()
    {
        var eine = new TdChart(TdChartKind.Column, 12, 8);
        eine.Series.Add(new TdChartSeries("Alpha", 1, 2));
        Assert.DoesNotContain(TdChartLayout.Rechnen(eine).Schriften, s => s.Text == "Alpha");

        var zwei = new TdChart(TdChartKind.Column, 12, 8);
        zwei.Series.Add(new TdChartSeries("Alpha", 1, 2));
        zwei.Series.Add(new TdChartSeries("Beta", 3, 4));

        var plan = TdChartLayout.Rechnen(zwei);
        Assert.Contains(plan.Schriften, s => s.Text == "Alpha");
        Assert.Contains(plan.Schriften, s => s.Text == "Beta");
    }

    /// <summary>
    /// <b>Ein Name, den niemand eingegeben hat, wird nicht erfunden.</b> Der heutige Editor
    /// schreibt „Reihe 2" — ein deutsches Wort, das an der Sprache des Rechners hinge und beim
    /// nächsten Öffnen auf Englisch stünde. Dieselbe Antwort wie bei
    /// <see cref="TdChart.Kategorie"/>: die laufende Nummer.
    /// </summary>
    [Fact]
    public void Eine_Reihe_ohne_Namen_bekommt_ihre_Nummer_und_kein_Wort()
    {
        var d = Bauen(TdChartKind.Column, [1], [2]);

        Assert.Equal("1", TdChartLayout.Reihenname(d, 0));
        Assert.Equal("2", TdChartLayout.Reihenname(d, 1));
    }

    /// <summary>
    /// Jede Kategorie wird beschriftet — mit ihrem Namen oder mit ihrer Nummer
    /// (<see cref="TdChart.Kategorie"/>). <b>Gespeichert ist dabei nur, was jemand eingegeben
    /// hat</b> (§4.21).
    /// </summary>
    [Fact]
    public void Jede_Kategorie_wird_beschriftet()
    {
        var d = Bauen(TdChartKind.Column, [1, 2, 3]);
        d.Categories.Add("Jan");

        var plan = TdChartLayout.Rechnen(d);

        Assert.Contains(plan.Schriften, s => s.Text == "Jan");
        Assert.Contains(plan.Schriften, s => s.Text == "2");
        Assert.Contains(plan.Schriften, s => s.Text == "3");
        Assert.Single(d.Categories);   // im Modell steht weiterhin nur die eine
    }

    /// <summary>Ein Titel macht oben Platz — die Zeichenfläche rückt hinunter.</summary>
    [Fact]
    public void Ein_Titel_macht_oben_Platz()
    {
        var ohne = TdChartLayout.Rechnen(Bauen(TdChartKind.Column, [1, 2]));

        var mit = Bauen(TdChartKind.Column, [1, 2]);
        mit.Title = "Umsatz";
        var planMit = TdChartLayout.Rechnen(mit);

        Assert.True(planMit.Flaeche.YCm > ohne.Flaeche.YCm);
        Assert.Contains(planMit.Schriften, s => s.Text == "Umsatz" && s.Fett);
    }

    // ==================== Der Kasten ====================

    /// <summary>
    /// <b>Nichts ragt aus dem Diagramm heraus.</b> Der Umbruch hat für den Kasten genau so viel
    /// Platz reserviert, wie am Modell steht (§4.21) — was darüber hinausliefe, stünde im Text
    /// daneben, und zwar ohne dass der Umbruch davon weiß.
    /// </summary>
    [Theory]
    [InlineData(TdChartKind.Column)]
    [InlineData(TdChartKind.Bar)]
    [InlineData(TdChartKind.Line)]
    [InlineData(TdChartKind.Scatter)]
    [InlineData(TdChartKind.ScatterLine)]
    [InlineData(TdChartKind.Pie)]
    [InlineData(TdChartKind.Radar)]
    public void Alles_bleibt_im_Kasten(TdChartKind art)
    {
        var d = Bauen(art, [4, 7, 3, 6], [2, 5, 1, 3]);
        d.Title = "Ein Titel, der Platz braucht";
        d.Categories.AddRange(["Januar", "Februar", "März", "April"]);

        var plan = TdChartLayout.Rechnen(d);
        Assert.False(plan.IstLeer);

        void Drin(double x, double y, string was)
        {
            Assert.True(x >= -0.01 && x <= d.WidthCm + 0.01, $"{was}: X {x} liegt außerhalb.");
            Assert.True(y >= -0.01 && y <= d.HeightCm + 0.01, $"{was}: Y {y} liegt außerhalb.");
        }

        foreach (var f in plan.Flaechen)
        {
            Drin(f.Kasten.XCm, f.Kasten.YCm, "Fläche");
            Drin(f.Kasten.RechtsCm, f.Kasten.UntenCm, "Fläche");
        }
        foreach (var s in plan.Striche)
        {
            Drin(s.Von.XCm, s.Von.YCm, "Strich");
            Drin(s.Bis.XCm, s.Bis.YCm, "Strich");
        }
        foreach (var z in plan.Zuege)
            foreach (var p in z.Punkte) Drin(p.XCm, p.YCm, "Zug");

        foreach (var k in plan.Stuecke)
        {
            Drin(k.Mitte.XCm - k.RadiusCm, k.Mitte.YCm - k.RadiusCm, "Kuchen");
            Drin(k.Mitte.XCm + k.RadiusCm, k.Mitte.YCm + k.RadiusCm, "Kuchen");
        }
    }

    /// <summary>
    /// <b>Ein kleines Diagramm ist kein Sonderfall.</b> Wer die Ränder von der Größe abzieht,
    /// ohne sie zu begrenzen, bekommt bei 3 × 2 cm eine Zeichenfläche mit negativer Breite —
    /// und danach Balken, die nach links wachsen.
    /// </summary>
    [Theory]
    [InlineData(3, 2)]
    [InlineData(1.5, 1)]
    [InlineData(20, 14)]
    public void Auch_ein_winziges_Diagramm_bekommt_eine_brauchbare_Flaeche(double breite, double hoehe)
    {
        var d = Bauen(TdChartKind.Column, [1, 2, 3]);
        d.WidthCm = breite;
        d.HeightCm = hoehe;

        var plan = TdChartLayout.Rechnen(d);

        Assert.True(plan.Flaeche.WidthCm > 0, "Die Zeichenfläche hat keine Breite.");
        Assert.True(plan.Flaeche.HeightCm > 0, "Die Zeichenfläche hat keine Höhe.");
        Assert.All(plan.Flaechen, f => Assert.True(f.Kasten.WidthCm > 0 && f.Kasten.HeightCm >= 0));
    }
}
