using GonkNote.Core.Text;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdImage"/>, <see cref="TdChart"/> und ihr Kasten im Umbruch — Phase 4,
/// Schritt 6.
///
/// <para>
/// <b>Der Befund, der diesen Schritt begründet:</b> Der heutige Editor rendert ein Diagramm
/// beim Einfügen zu einer Bitmap und wirft die Zahlen damit weg (<c>ChartDialog</c>). Was hier
/// bewacht wird, ist das Gegenteil — <b>die Zahlen stehen im Dokument, das Bild wird
/// gerechnet</b>, dasselbe Muster wie bei der Listennummer (§4.17) und beim Feld (§4.20).
/// </para>
/// <para>
/// Gemessen wird wie in <see cref="UmbruchTests"/> mit festen Maßen: jedes Zeichen 1 cm breit,
/// jede Zeile 1 cm hoch. <b>Eine Grafik wird ohnehin nicht gemessen</b> — sie hat ihr Maß bei
/// sich.
/// </para>
/// </summary>
public sealed class GrafikTests
{
    /// <inheritdoc cref="UmbruchTests"/>
    private sealed class FesteMessung : ITdTextMeasure
    {
        public double WidthCm(string text, TdCharFormat format) => text.Length;

        public TdFontMetrics Metrics(TdCharFormat format) => new(0.8, 0.2, 1.0);
    }

    /// <summary>Ein Blatt, auf das genau zehn Zeichen und zehn Zeilen passen.</summary>
    private static TdPageSetup Blatt() => new()
    {
        WidthCm = 12,
        HeightCm = 12,
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

    private static TdLayoutResult Umbrechen(TdDocument doc) =>
        TdLayout.Umbrechen(doc, new FesteMessung());

    private static readonly Guid BildId = new("77777777-7777-7777-7777-777777777777");

    private static TdImage Bild(double breite = 4, double hoehe = 3) =>
        new(BildId, "png", breite, hoehe);

    // ==================== Das Bild ====================

    /// <summary>
    /// <b>Die Bytes stehen nicht im Dokument.</b> Das ist keine Sparsamkeit, sondern eine
    /// gemessene Entscheidung aus V1: Als sie noch drinstanden, wurde ein Dokument mit drei
    /// Fotos (2 MB) zu 16,8 MB und riss die 16-MB-Grenze von LiteDB. Hier steht ein Verweis,
    /// und die Endung sagt, als was die unveränderten Bytes zurückgeschrieben werden.
    /// </summary>
    [Fact]
    public void Ein_Bild_traegt_einen_Verweis_und_keine_Bytes()
    {
        var bild = Bild();

        Assert.Equal(BildId, bild.BlobId);
        Assert.Equal("png", bild.Extension);
        Assert.Equal("", bild.PlainText());
    }

    /// <summary>
    /// Die Endung wird eingenordet: ohne Punkt, klein, und niemals leer. Sie entscheidet über
    /// den Bildtyp im Export — ein JPEG, das als PNG hinausgeht, ist um ein Vielfaches größer.
    /// </summary>
    [Theory]
    [InlineData(".JPG", "jpg")]
    [InlineData("JPEG", "jpeg")]
    [InlineData("png", "png")]
    [InlineData("", "png")]
    [InlineData(null, "png")]
    public void Die_Endung_wird_eingenordet(string? gesetzt, string erwartet)
    {
        Assert.Equal(erwartet, new TdImage { Extension = gesetzt! }.Extension);
    }

    /// <summary>
    /// <b>Eine Grafik trägt keinen Klartext bei</b> — aus demselben Grund wie ein Feld
    /// (§4.20). Der Alternativtext ist ein Ersatz für die Anzeige und keine Stelle im Text;
    /// wer ihn mitzählte, fände ihn im Wortzähler und in der Suche.
    /// </summary>
    [Fact]
    public void Der_Alternativtext_ist_kein_Text()
    {
        var doc = Dok(new TdParagraph([
            new TdRun("Hier: "),
            new TdImage(BildId, "png", 4, 3) { AltText = "Ein Foto vom Meer" },
        ]));

        Assert.Equal("Hier: ", doc.PlainText());
        Assert.Equal(1, doc.WordCount());
    }

    /// <summary>
    /// Der Durchlauf über die benutzten Bilder muss **vollständig** sein, und zwar in die
    /// gefährliche Richtung: Eine Kennung zu viel kostet Platz, eine zu wenig löscht ein Bild,
    /// das noch gebraucht wird. Er steigt deshalb in Tabellenzellen ab (§4.19) und in Verweise
    /// hinein (§4.20).
    /// </summary>
    [Fact]
    public void Benutzte_Bilder_werden_auch_in_Zelle_und_Verweis_gefunden()
    {
        var inZelle = new Guid("11111111-0000-0000-0000-000000000001");
        var imVerweis = new Guid("11111111-0000-0000-0000-000000000002");

        var doc = Dok(
            new TdParagraph([Bild()]),
            new TdTable(new TdTableRow(new TdTableCell(
                new TdParagraph([new TdImage(inZelle, "png", 1, 1)])))),
            new TdParagraph([new TdHyperlink("ziel.md", new TdImage(imVerweis, "png", 1, 1))]));

        Assert.Equal([BildId, inZelle, imVerweis], doc.UsedImages());
    }

    // ==================== Das Diagramm ====================

    /// <summary>
    /// <b>Ein Diagramm speichert seine Zahlen.</b> Der heutige Editor speichert ein Bild
    /// davon — danach lässt sich ein Tippfehler in einer Kategorie nur noch durch Neubauen
    /// beheben.
    /// </summary>
    [Fact]
    public void Ein_Diagramm_traegt_seine_Zahlen()
    {
        var d = Diagramm();

        Assert.Equal(["Mo", "Di", "Mi"], d.Categories);
        Assert.Equal([4.0, 7.0, 3.0], d.Series[0].Values);
        Assert.Equal("", d.PlainText());
    }

    /// <summary>
    /// Die Legende wird **gerechnet**: Sie hat nur etwas zu sagen, wenn es mehr als eine Reihe
    /// gibt — beim Kuchen stehen die Namen ohnehin an den Stücken. Dieselbe Regel wendet der
    /// heutige Editor an, nur in seiner Zeichenroutine, wo sie außer ihm niemand sieht.
    /// </summary>
    [Fact]
    public void Die_Legende_wird_gerechnet()
    {
        var eine = Diagramm();
        Assert.False(eine.ShowLegend);

        eine.Series.Add(new TdChartSeries("Zweite", 1, 2, 3));
        Assert.True(eine.ShowLegend);

        eine.Kind = TdChartKind.Pie;
        Assert.False(eine.ShowLegend);
    }

    /// <summary>
    /// Farbe je Element gibt es beim Kuchen und bei einer einzelnen Säulenreihe — **nicht**
    /// bei einer Linie: eine Kurve, die alle zwei Punkte die Farbe wechselt, ist keine Kurve
    /// mehr.
    /// </summary>
    [Theory]
    [InlineData(TdChartKind.Pie, 1, true)]
    [InlineData(TdChartKind.Pie, 2, true)]
    [InlineData(TdChartKind.Column, 1, true)]
    [InlineData(TdChartKind.Bar, 1, true)]
    [InlineData(TdChartKind.Column, 2, false)]
    [InlineData(TdChartKind.Line, 1, false)]
    [InlineData(TdChartKind.Radar, 1, false)]
    public void Farbe_je_Element_gilt_nur_dort_wo_sie_hingehoert(TdChartKind art, int reihen, bool erwartet)
    {
        var d = new TdChart(art, 8, 6);
        for (int i = 0; i < reihen; i++) d.Series.Add(new TdChartSeries($"R{i}", 1, 2, 3));

        Assert.Equal(erwartet, d.FarbeJeElement);
    }

    /// <summary>
    /// Die Palette läuft um: Ein siebter Balken bekommt wieder die erste Farbe, statt keine zu
    /// haben. Und ohne eigene Palette gilt die des heutigen Editors.
    /// </summary>
    [Fact]
    public void Die_Palette_laeuft_um()
    {
        var d = new TdChart(TdChartKind.Column, 8, 6) { Palette = { "#111111", "#222222" } };

        Assert.Equal("#111111", d.Farbe(0));
        Assert.Equal("#222222", d.Farbe(1));
        Assert.Equal("#111111", d.Farbe(2));

        Assert.Equal(TdChart.StandardPalette[0], new TdChart().Farbe(0));
    }

    /// <summary>
    /// Eine fehlende Kategorie wird beim **Anzeigen** zur laufenden Nummer — gespeichert wird
    /// nur, was jemand eingegeben hat. Ein erfundener Name in der Datei wäre eine Angabe, die
    /// niemand gemacht hat.
    /// </summary>
    [Fact]
    public void Fehlende_Kategorien_werden_gezaehlt_und_nicht_gespeichert()
    {
        var d = new TdChart(TdChartKind.Column, 8, 6) { Categories = { "Mo" } };
        d.Series.Add(new TdChartSeries("", 1, 2, 3));

        Assert.Equal("Mo", d.Kategorie(0));
        Assert.Equal("2", d.Kategorie(1));
        Assert.Equal("3", d.Kategorie(2));
        Assert.Single(d.Categories);
    }

    // ==================== Der Kasten im Umbruch ====================

    /// <summary>
    /// Eine Grafik wird **nicht gemessen** — sie hat ihr Maß dabei. Die Schriftmessung hat
    /// hier nichts zu suchen: ein Bild ist so breit, wie es im Dokument steht, und auf jedem
    /// System gleich.
    /// </summary>
    [Fact]
    public void Eine_Grafik_bekommt_ihren_Kasten()
    {
        var zeile = Umbrechen(Dok(new TdParagraph([Bild(breite: 4, hoehe: 3)]))).Pages[0].Lines[0];

        var lauf = Assert.Single(zeile.Runs);
        Assert.Equal("", lauf.Text);
        Assert.Equal(4.0, lauf.WidthCm, 3);
        Assert.NotNull(lauf.Graphic);
        Assert.Equal(3.0, lauf.Graphic!.HeightCm, 3);
    }

    /// <summary>
    /// <b>Die Zeile wird so hoch wie die Grafik.</b> Ohne das ragte ein Bild in die nächste
    /// Zeile hinein, und der Seitenumbruch rechnete mit einer Höhe, die es nicht gibt — das
    /// Fehlerbild wäre ein Bild, das über den Seitenrand läuft.
    /// </summary>
    [Fact]
    public void Die_Zeile_waechst_mit_der_Grafik()
    {
        var klein = Umbrechen(Dok(new TdParagraph([Bild(hoehe: 0.5)]))).Pages[0].Lines[0];
        var gross = Umbrechen(Dok(new TdParagraph([Bild(hoehe: 3)]))).Pages[0].Lines[0];

        // Eine Zeile ist bei fester Messung 1 cm hoch — die kleine Grafik ändert daran nichts.
        Assert.Equal(1.0, klein.HeightCm, 3);

        // Die große dagegen schon: 3 cm über der Grundlinie, und darunter bleibt die
        // Unterlänge der Absatzmarke (0,2 cm) stehen. **Die Absatzmarke ist immer da** — auch
        // in einem Absatz, der nichts als ein Bild enthält.
        Assert.Equal(3.2, gross.HeightCm, 3);
    }

    /// <summary>
    /// Eine Grafik steht **auf** der Grundlinie und nicht darin: Ihre Höhe schiebt die
    /// Grundlinie hinunter. Steht Text daneben, muss die Zeile beides fassen — Bildhöhe über
    /// der Grundlinie plus Unterlänge darunter.
    /// </summary>
    [Fact]
    public void Eine_Grafik_steht_auf_der_Grundlinie()
    {
        var zeile = Umbrechen(Dok(new TdParagraph([
            new TdRun("ab"),
            Bild(breite: 2, hoehe: 3),
        ]))).Pages[0].Lines[0];

        Assert.Equal(3.0, zeile.BaselineCm, 3);
        Assert.Equal(3.2, zeile.HeightCm, 3);   // 3 cm über, 0,2 cm Unterlänge darunter
    }

    /// <summary>
    /// <b>Eine Grafik, die breiter ist als die Zeile, hängt die Rechnung nicht auf.</b> Sie
    /// steht allein in ihrer Zeile und ragt heraus — derselbe Ausweg wie beim überlangen Wort
    /// (§4.16). Sichtbar falsch ist besser als ein Umbruch, der nicht zurückkommt.
    /// </summary>
    [Fact]
    public void Eine_zu_breite_Grafik_haengt_den_Umbruch_nicht_auf()
    {
        var seite = Umbrechen(Dok(new TdParagraph([
            new TdRun("davor"),
            Bild(breite: 30, hoehe: 1),
            new TdRun("danach"),
        ]))).Pages[0];

        var zeilen = seite.Lines;
        Assert.Equal(3, zeilen.Count);
        Assert.Equal("davor", zeilen[0].PlainText());
        Assert.Equal(30.0, Assert.Single(zeilen[1].Runs).WidthCm, 3);
        Assert.Equal("danach", zeilen[2].PlainText());
    }

    /// <summary>Ein Bild in einem Verweis ist anklickbar — der gesetzte Lauf trägt das Ziel.</summary>
    [Fact]
    public void Eine_Grafik_in_einem_Verweis_traegt_das_Ziel()
    {
        var doc = Dok(new TdParagraph([new TdHyperlink("kapitel-2.md", Bild())]));

        var lauf = Assert.Single(Umbrechen(doc).Pages[0].Lines[0].Runs);

        Assert.NotNull(lauf.Graphic);
        Assert.Equal("kapitel-2.md", lauf.Link!.Target);
    }

    /// <summary>
    /// Ein Diagramm ist für den Umbruch **derselbe Kasten** wie ein Bild — es weiß nur, was
    /// darin steht. Wer das trennte, bekäme zwei Umbruchpfade für dieselbe Sache.
    /// </summary>
    [Fact]
    public void Ein_Diagramm_ist_fuer_den_Umbruch_derselbe_Kasten()
    {
        var lauf = Assert.Single(
            Umbrechen(Dok(new TdParagraph([Diagramm()]))).Pages[0].Lines[0].Runs);

        Assert.IsType<TdChart>(lauf.Graphic);
        Assert.Equal(8.0, lauf.WidthCm, 3);
    }

    // ==================== Der Bildspeicher ====================

    /// <summary>
    /// Der Wegwerf-Bildspeicher bildet seine Kennung aus dem **Inhalt**. Zweimal dasselbe Bild
    /// abzulegen ergibt deshalb dieselbe Kennung — ein Wächter, der das täte, bekäme sonst ein
    /// Ergebnis, das sich von Lauf zu Lauf ändert (§7).
    /// </summary>
    [Fact]
    public void Derselbe_Inhalt_bekommt_dieselbe_Kennung()
    {
        var speicher = new TdMemoryImages();
        byte[] daten = Beispieldokument.Bild(8, 8, SKColors.Red);

        var erste = speicher.Ablegen(daten, "png");
        var zweite = speicher.Ablegen([.. daten], "png");

        Assert.Equal(erste, zweite);
        Assert.Equal(1, speicher.Anzahl);
        Assert.Equal(daten, speicher.Lesen(erste));
    }

    /// <summary>
    /// Ein fehlender Blob ist **kein Programmierfehler**: Er kommt aus einer unvollständigen
    /// Sicherung — der Blob-Ordner wird beim Kopieren gern vergessen (Dauerregel 4).
    /// </summary>
    [Fact]
    public void Ein_fehlender_Blob_gibt_null_und_wirft_nicht()
    {
        Assert.Null(new TdMemoryImages().Lesen(BildId));
    }

    // ==================== Grafiken anfassen (§4.89) ====================

    private static TdSelection Bei(TdDocument doc, int absatz, int linear)
    {
        var a = TdCursor.AbsatzAn(doc, absatz)!;
        return new TdSelection(TdCursor.AusLinear(a, absatz, linear));
    }

    /// <summary>
    /// <b>Gefunden wird auch von rechts.</b> Eine Grafik ist einen Schritt breit, und ein Klick
    /// darauf landet je nach Bildhälfte davor oder dahinter (§4.30). Suchte der Knopf nur an
    /// der Stelle selbst, täte er mal etwas und mal nicht.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Die_Grafik_wird_von_beiden_Seiten_gefunden(int linear)
    {
        var doc = Dok(new TdParagraph([Bild()]));

        Assert.NotNull(TdGrafikEdit.GrafikAn(doc, Bei(doc, 0, linear)));
    }

    /// <summary>Wo keine Grafik steht, gibt es auch keine.</summary>
    [Fact]
    public void Ohne_Grafik_kein_Fund()
    {
        var doc = Dok(new TdParagraph("nur Text"));

        Assert.Null(TdGrafikEdit.GrafikAn(doc, Bei(doc, 0, 3)));
    }

    /// <summary>„Größer" behält das Seitenverhältnis, „breiter" nicht.</summary>
    [Fact]
    public void Groesser_behaelt_das_Verhaeltnis()
    {
        var doc = Dok(new TdParagraph([Bild(8, 6)]));

        TdGrafikEdit.Groesse(doc, Bei(doc, 0, 1), 1.5, 1.5)!.Anwenden();

        var neu = (TdImage)TdCursor.Stuecke(TdCursor.AbsatzAn(doc, 0)!).First();
        Assert.Equal(12, neu.WidthCm, 3);
        Assert.Equal(9, neu.HeightCm, 3);
    }

    [Fact]
    public void Breiter_laesst_die_Hoehe_stehen()
    {
        var doc = Dok(new TdParagraph([Bild(8, 6)]));

        TdGrafikEdit.Groesse(doc, Bei(doc, 0, 1), 1.5, 1.0)!.Anwenden();

        var neu = (TdImage)TdCursor.Stuecke(TdCursor.AbsatzAn(doc, 0)!).First();
        Assert.Equal(12, neu.WidthCm, 3);
        Assert.Equal(6, neu.HeightCm, 3);
    }

    /// <summary>
    /// <b>Ein Bild darf nicht auf null schrumpfen.</b> Es wäre danach unsichtbar und nicht mehr
    /// anklickbar — also weg, ohne dass jemand es gelöscht hätte.
    /// </summary>
    [Fact]
    public void Kleiner_hoert_bei_der_Untergrenze_auf()
    {
        var doc = Dok(new TdParagraph([Bild(8, 6)]));

        for (int i = 0; i < 60; i++)
            TdGrafikEdit.Groesse(doc, Bei(doc, 0, 1), 0.5, 0.5)?.Anwenden();

        var neu = (TdImage)TdCursor.Stuecke(TdCursor.AbsatzAn(doc, 0)!).First();
        Assert.True(neu.WidthCm >= TdGrafikEdit.MindestCm);
        Assert.True(neu.HeightCm >= TdGrafikEdit.MindestCm);
    }

    /// <summary>Eine Größe, die sich nicht ändert, ist keine Änderung — und kommt nicht in den Verlauf.</summary>
    [Fact]
    public void Dieselbe_Groesse_ist_keine_Aenderung()
    {
        var doc = Dok(new TdParagraph([Bild(8, 6)]));

        Assert.Null(TdGrafikEdit.GroesseSetzen(doc, Bei(doc, 0, 1), 8, 6));
    }

    /// <summary>
    /// <b>Was am Bild hängt, überlebt die Größenänderung</b> — Kennung, Dateityp und
    /// Alternativtext. Ein Bild, das beim Vergrößern seinen Blob-Verweis verliert, ist weg.
    /// </summary>
    [Fact]
    public void Die_Groessenaenderung_laesst_alles_andere_stehen()
    {
        var bild = Bild(8, 6);
        bild.AltText = "Ein Hund";
        var doc = Dok(new TdParagraph([bild]));

        TdGrafikEdit.Groesse(doc, Bei(doc, 0, 1), 1.15, 1.15)!.Anwenden();

        var neu = (TdImage)TdCursor.Stuecke(TdCursor.AbsatzAn(doc, 0)!).First();
        Assert.Equal(BildId, neu.BlobId);
        Assert.Equal("png", neu.Extension);
        Assert.Equal("Ein Hund", neu.AltText);
    }

    /// <summary>Dasselbe für ein Diagramm — seine Zahlen dürfen beim Ziehen nicht verlorengehen.</summary>
    [Fact]
    public void Ein_Diagramm_behaelt_seine_Zahlen()
    {
        var doc = Dok(new TdParagraph([Diagramm()]));

        TdGrafikEdit.Groesse(doc, Bei(doc, 0, 1), 1.15, 1.15)!.Anwenden();

        var neu = (TdChart)TdCursor.Stuecke(TdCursor.AbsatzAn(doc, 0)!).First();
        Assert.Equal("Woche", neu.Title);
        Assert.Equal(["Mo", "Di", "Mi"], neu.Categories);
        Assert.Equal([4, 7, 3], neu.Series[0].Values);
        Assert.Equal(3, neu.Palette.Count);
    }

    /// <summary>Ein Bild kommt als eigener Absatz und nicht mitten in eine Textzeile.</summary>
    [Fact]
    public void Ein_eingefuegtes_Bild_bekommt_einen_eigenen_Absatz()
    {
        var doc = Dok(new TdParagraph("Text davor"));

        TdGrafikEdit.Einfuegen(doc, Bei(doc, 0, 10), Bild())!.Anwenden();

        var mitBild = doc.Paragraphs().Single(
            a => TdCursor.Stuecke(a).Any(x => x is TdImage));
        Assert.Equal("", mitBild.PlainText());
    }

    /// <summary>Die Beschriftung steht unter dem Absatz und zählt sich selbst hoch.</summary>
    [Fact]
    public void Beschriftungen_zaehlen_hoch()
    {
        var doc = Dok(new TdParagraph([Bild()]), new TdParagraph([Bild()]));

        TdGrafikEdit.Beschriftung(doc, Bei(doc, 0, 1), "Abbildung")!.Anwenden();
        TdGrafikEdit.Beschriftung(doc, Bei(doc, 2, 1), "Abbildung")!.Anwenden();

        var texte = doc.Paragraphs().Select(a => a.PlainText()).Where(t => t.Length > 0).ToList();
        Assert.Equal(["Abbildung 1: ", "Abbildung 2: "], texte);
    }

    /// <summary>
    /// <b>Der Vorsatz kommt von außen</b> — ein festes „Abbildung" stünde in der englischen
    /// Fassung genauso da.
    /// </summary>
    [Fact]
    public void Der_Beschriftungsvorsatz_ist_uebersetzbar()
    {
        var doc = Dok(new TdParagraph([Bild()]));

        TdGrafikEdit.Beschriftung(doc, Bei(doc, 0, 1), "Figure")!.Anwenden();

        Assert.Contains("Figure 1: ", doc.Paragraphs().Select(a => a.PlainText()));
    }

    /// <summary>Die Beschriftung lässt den Absatz darüber ganz.</summary>
    [Fact]
    public void Die_Beschriftung_teilt_den_Absatz_nicht()
    {
        var doc = Dok(new TdParagraph("Ein Satz mit Text"));

        TdGrafikEdit.Beschriftung(doc, Bei(doc, 0, 3), "Abbildung")!.Anwenden();

        Assert.Equal("Ein Satz mit Text", TdCursor.AbsatzAn(doc, 0)!.PlainText());
    }

    // ==================== Hilfsmittel ====================

    internal static TdChart Diagramm() => new(TdChartKind.Column, 8, 6)
    {
        Title = "Woche",
        Categories = { "Mo", "Di", "Mi" },
        Series = { new TdChartSeries("Umsatz", 4, 7, 3) },
        Palette = { "#2563EB", "#14B8A6", "#EC4899" },
    };
}
