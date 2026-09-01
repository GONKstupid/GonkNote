using GonkNote.Core.Rendering;
using GonkNote.Core.Text;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdRenderer"/> — aus einer gesetzten Seite werden Pixel (HANDOFF §4.24).
///
/// <para>
/// <b>Was hier bewusst NICHT gehasht wird: alles mit Schrift.</b> Dieselbe Regel wie bei
/// <see cref="RendererSnapshotTests"/> und aus demselben Grund (§4.6): „Segoe UI" gibt es unter
/// Linux nicht, der Zeichner fällt dann auf eine Ersatzschrift zurück, und ein Hash über
/// gezeichneten Text prüfte die Schriftausstattung des Rechners statt den Zeichner. Er wäre in
/// der CI auf dem Ubuntu-Läufer dauerhaft rot.
/// </para>
/// <para>
/// <b>Geprüft wird deshalb zweigeteilt:</b> die schriftfreien Teile (Papier, Tabellenrahmen,
/// Zellhintergrund, Absatzlinie, Platzhalterkasten, Wasserzeichen) pixelgenau als Snapshot —
/// und alles, was mit Text zu tun hat, über die **Rechnung**: mit einer festen Messung steht
/// vorher fest, wo etwas landen muss, und danach wird nachgesehen, ob dort Farbe ist.
/// </para>
/// </summary>
public sealed class ZeichnerTests
{
    /// <summary>Wie in <c>UmbruchTests</c>: ein Zeichen = 1 cm, eine Zeile = 1 cm.</summary>
    private sealed class FesteMessung : ITdTextMeasure
    {
        public double WidthCm(string text, TdCharFormat format) => text.Length;

        public TdFontMetrics Metrics(TdCharFormat format) => new(0.8, 0.2, 1.0);
    }

    /// <summary>Zehn Zeichen breit, zehn Zeilen hoch, 1 cm Rand ringsum.</summary>
    private static TdPageSetup Blatt(double breite = 10, double hoehe = 10) => new()
    {
        WidthCm = breite + 2,
        HeightCm = hoehe + 2,
        MarginLeftCm = 1,
        MarginRightCm = 1,
        MarginTopCm = 1,
        MarginBottomCm = 1,
    };

    private static TdDocument Dok(TdPageSetup seite, params TdBlock[] bloecke) => new()
    {
        DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 0 },
        Sections = { new TdSection(bloecke) { Page = seite } },
    };

    private static TdPage Setzen(TdDocument doc) =>
        TdLayout.Umbrechen(doc, new FesteMessung()).Pages[0];

    /// <summary>Zeichnet auf eine weiße Fläche und gibt die Pixel zurück.</summary>
    private static SKBitmap Malen(TdPage seite, double massstab, TdRenderContext kontext = default)
    {
        int breite = (int)Math.Ceiling(seite.Setup.WidthCm * massstab);
        int hoehe = (int)Math.Ceiling(seite.Setup.HeightCm * massstab);

        var bmp = new SKBitmap(new SKImageInfo(breite, hoehe, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var leinwand = new SKCanvas(bmp))
        {
            leinwand.Clear(SKColors.Transparent);
            TdRenderer.Seite(leinwand, seite, massstab, kontext);
        }
        return bmp;
    }

    /// <summary>Ist an dieser Stelle etwas anderes als Papier?</summary>
    private static bool Farbig(SKBitmap bmp, int x, int y)
    {
        var p = bmp.GetPixel(x, y);
        return p.Alpha > 0 && (p.Red < 245 || p.Green < 245 || p.Blue < 245);
    }

    /// <summary>
    /// Ein einfarbiges PNG. **Erzeugt statt eingecheckt** — eine Binärdatei im Repo bräuchte
    /// eine geklärte Lizenz (§6), eine Fläche in einer Farbe braucht keine.
    /// </summary>
    private static byte[] Png(int breite, int hoehe, SKColor farbe)
    {
        using var flaeche = SKSurface.Create(new SKImageInfo(breite, hoehe));
        flaeche.Canvas.Clear(farbe);
        using var abbild = flaeche.Snapshot();
        using var daten = abbild.Encode(SKEncodedImageFormat.Png, 100);
        return daten.ToArray();
    }

    /// <summary>Wie viele Pixel im Rechteck sind nicht Papier?</summary>
    private static int FarbigeIm(SKBitmap bmp, int x, int y, int breite, int hoehe)
    {
        int treffer = 0;
        for (int yy = y; yy < Math.Min(y + hoehe, bmp.Height); yy++)
            for (int xx = x; xx < Math.Min(x + breite, bmp.Width); xx++)
                if (Farbig(bmp, xx, yy)) treffer++;
        return treffer;
    }

    /// <summary>
    /// Die Umschließung aller nicht-weißen Pixel — leer, wenn nichts gezeichnet wurde.
    /// <para>
    /// <b>Für alles mit Schrift belastbarer als die Pixelzahl.</b> Wie viele Pixel ein
    /// Buchstabe einfärbt, hängt an Kantenglättung und Hinting und wächst bei kleinen Graden
    /// nicht linear mit; wie groß er ist, schon.
    /// </para>
    /// </summary>
    private static SKRectI TinteKasten(SKBitmap bmp)
    {
        int links = int.MaxValue, oben = int.MaxValue, rechts = int.MinValue, unten = int.MinValue;

        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                if (!Farbig(bmp, x, y)) continue;
                if (x < links) links = x;
                if (x > rechts) rechts = x;
                if (y < oben) oben = y;
                if (y > unten) unten = y;
            }

        return rechts < links ? SKRectI.Empty : new SKRectI(links, oben, rechts + 1, unten + 1);
    }

    // ==================== Die Seite selbst ====================

    /// <summary>
    /// <b>Das Papier ist weiß und füllt die Seite</b> — auch im dunklen Thema (§1). Was sich
    /// verdunkelt, ist die Fläche um die Seite herum, und die gehört dem Kopf.
    /// </summary>
    [Fact]
    public void Das_Papier_ist_weiss_und_deckt_die_ganze_Seite()
    {
        using var bmp = Malen(Setzen(Dok(Blatt(), new TdParagraph())), massstab: 10);

        foreach (var (x, y) in new[] { (0, 0), (bmp.Width - 1, 0), (0, bmp.Height - 1), (bmp.Width - 1, bmp.Height - 1) })
        {
            var p = bmp.GetPixel(x, y);
            Assert.Equal(255, p.Alpha);
            Assert.Equal(SKColors.White, new SKColor(p.Red, p.Green, p.Blue));
        }
    }

    /// <summary>
    /// <b>Der Text beginnt am Seitenrand und nicht am Blattrand.</b> Der Umbruch legt seine
    /// Maße ab der Oberkante des **Textbereichs** ab; wer die Ränder beim Zeichnen vergisst,
    /// bekommt ein Dokument, das oben links klebt — und zwar auf jeder Seite gleich falsch,
    /// also ohne dass es nach einem Fehler aussieht.
    /// </summary>
    [Fact]
    public void Der_Text_sitzt_hinter_den_Seitenraendern()
    {
        var seite = Setzen(Dok(Blatt(), new TdParagraph("MMMM")));
        using var bmp = Malen(seite, massstab: 20);

        // 1 cm Rand bei 20 px/cm = die ersten 20 Pixel bleiben leer, in beiden Richtungen.
        Assert.Equal(0, FarbigeIm(bmp, 0, 0, bmp.Width, 19));
        Assert.Equal(0, FarbigeIm(bmp, 0, 0, 19, bmp.Height));

        // Und im Textbereich steht etwas.
        Assert.True(FarbigeIm(bmp, 20, 20, 100, 40) > 0, "Im Textbereich wurde nichts gezeichnet.");
    }

    /// <summary>
    /// <b>Der Maßstab skaliert die Schrift mit.</b> Die Größe steht im Modell in **Punkt**, die
    /// Leinwand rechnet in Pixeln — wer die Umrechnung vergisst, bekommt bei jeder Zoomstufe
    /// dieselbe winzige Schrift auf einer immer größeren Seite. Geprüft wird nicht die
    /// Buchstabenform (die hängt an der Schrift des Rechners), sondern dass die eingefärbte
    /// Fläche mit dem Quadrat des Maßstabs wächst.
    /// </summary>
    [Fact]
    public void Der_Massstab_skaliert_auch_die_Schrift()
    {
        var doc = Dok(Blatt(), new TdParagraph("MMMM"));

        using var klein = Malen(Setzen(doc), massstab: 20);
        using var gross = Malen(Setzen(doc), massstab: 40);

        var a = TinteKasten(klein);
        var b = TinteKasten(gross);

        Assert.False(a.IsEmpty, "Bei kleinem Maßstab wurde nichts gezeichnet.");

        // Doppelter Maßstab = doppelt so hohe und doppelt so breite Schrift. Die Spanne lässt
        // Kantenglättung und Schriftraster Platz — **eine Schrift, die gar nicht mitskaliert,
        // läge bei 1,0**, und genau das ist der Fehler, den dieser Wächter fängt.
        Assert.InRange((double)b.Height / a.Height, 1.7, 2.4);
        Assert.InRange((double)b.Width / a.Width, 1.7, 2.4);
    }

    // ==================== Tabellen ====================

    private static TdTable Tabelle(string fuellung)
    {
        var t = new TdTable();
        t.ColumnWidthsCm.Add(5);
        t.ColumnWidthsCm.Add(5);
        t.Format.Top = t.Format.Bottom = t.Format.Left = t.Format.Right = new TdBorder(1, "#FF0000");
        t.Format.InsideH = t.Format.InsideV = new TdBorder(1, "#FF0000");

        var zeile = new TdTableRow();
        zeile.Cells.Add(new TdTableCell { Shading = fuellung, Blocks = { new TdParagraph() } });
        zeile.Cells.Add(new TdTableCell { Blocks = { new TdParagraph() } });
        t.Rows.Add(zeile);
        return t;
    }

    /// <summary>
    /// <b>Rahmen und Zellhintergrund landen dort, wo die Zelle steht.</b> Beides kommt aus dem
    /// **Tabellen**format, nicht aus der Zelle (§4.18) — die Umrechnung ist die Stelle, an der
    /// eine Tabelle „irgendwie verrutscht" aussieht statt falsch.
    /// </summary>
    [Fact]
    public void Zellhintergrund_und_Rahmen_sitzen_an_der_Zelle()
    {
        var seite = Setzen(Dok(Blatt(), Tabelle("#00FF00")));
        using var bmp = Malen(seite, massstab: 20);

        var zeile = Assert.Single(seite.TableRows);
        Assert.Equal(2, zeile.Cells.Count);

        // Mitte der ersten Zelle: grün gefüllt. +20 px für den Seitenrand.
        int mitteX = 20 + (int)(zeile.Cells[0].XCm * 20 + zeile.Cells[0].WidthCm * 20 / 2);
        int mitteY = 20 + (int)(zeile.YCm * 20 + zeile.HeightCm * 20 / 2);
        var fuellung = bmp.GetPixel(mitteX, mitteY);
        Assert.Equal(new SKColor(0x00, 0xFF, 0x00), new SKColor(fuellung.Red, fuellung.Green, fuellung.Blue));

        // Die zweite Zelle hat keine Füllung — dort steht das Papier.
        int zweiteX = 20 + (int)(zeile.Cells[1].XCm * 20 + zeile.Cells[1].WidthCm * 20 / 2);
        var leer = bmp.GetPixel(zweiteX, mitteY);
        Assert.Equal(SKColors.White, new SKColor(leer.Red, leer.Green, leer.Blue));

        // Auf der Oberkante der Zeile liegt der rote Rahmen.
        Assert.True(Farbig(bmp, mitteX, 20 + (int)(zeile.YCm * 20)), "Kein Rahmen an der Oberkante.");
    }

    /// <summary>
    /// <b>Der Zellinhalt zählt ab der Innenkante der Zelle</b>, nicht ab dem Textbereich — der
    /// Umbruch setzt ihn als eigene kleine Seite ohne Ränder (§4.19). Wer den Versatz vergisst,
    /// bekommt eine Tabelle, deren Text links neben ihr steht, und zwar bei jeder Spalte weiter
    /// daneben. Geprüft an der **zweiten** Spalte, weil dort der Fehler am größten wäre.
    /// </summary>
    [Fact]
    public void Der_Text_einer_Zelle_steht_in_ihrer_Spalte()
    {
        var t = new TdTable();
        t.ColumnWidthsCm.Add(5);
        t.ColumnWidthsCm.Add(5);

        // **Ohne Rahmen** — sonst färbt die Linie der ersten Zelle die Spalte ein, und der
        // Wächter fände Farbe dort, wo er gerade beweisen will, dass keine ist.
        t.Format.Top = t.Format.Bottom = t.Format.Left = t.Format.Right = TdBorder.Keine;
        t.Format.InsideH = t.Format.InsideV = TdBorder.Keine;

        var zeile = new TdTableRow();
        zeile.Cells.Add(new TdTableCell { Blocks = { new TdParagraph() } });
        zeile.Cells.Add(new TdTableCell { Blocks = { new TdParagraph("MM") } });
        t.Rows.Add(zeile);

        var seite = Setzen(Dok(Blatt(), t));
        using var bmp = Malen(seite, massstab: 20);

        var gesetzt = Assert.Single(seite.TableRows);
        int zweiteLinks = 20 + (int)(gesetzt.Cells[1].XCm * 20);
        int obenY = 20 + (int)(gesetzt.YCm * 20);
        int hoehe = (int)(gesetzt.HeightCm * 20);

        // In der zweiten Spalte steht Text …
        Assert.True(FarbigeIm(bmp, zweiteLinks, obenY, (int)(gesetzt.Cells[1].WidthCm * 20), hoehe) > 0,
            "In der zweiten Spalte steht kein Text.");

        // … und in der ersten nicht. Genau andersherum sähe es aus, wenn der Versatz fehlte.
        Assert.Equal(0, FarbigeIm(bmp, 20, obenY, (int)(gesetzt.Cells[0].WidthCm * 20) - 1, hoehe));
    }

    // ==================== Absatzlinie ====================

    /// <summary>
    /// <b>Ein Absatz über drei Zeilen bekommt *eine* Trennlinie, nicht drei.</b> Wer sie je
    /// Zeile zöge, bekäme liniertes Papier — und weil das nach einer Gestaltungsabsicht
    /// aussieht, fiele es niemandem als Fehler auf.
    /// </summary>
    [Fact]
    public void Die_Absatzlinie_steht_einmal_unter_dem_Absatz()
    {
        var absatz = new TdParagraph("aaaa bbbb cccc")
        {
            Format = { BottomBorder = new TdBorder(1, "#FF0000") },
        };
        var seite = Setzen(Dok(Blatt(), absatz));
        using var bmp = Malen(seite, massstab: 20);

        Assert.True(seite.Lines.Count >= 2, "Der Absatz sollte über mehrere Zeilen laufen.");

        // Eine Linie erkennt man daran, dass eine ganze Pixelzeile über die Spaltenbreite rot
        // ist. **Gezählt werden zusammenhängende Bänder und nicht Pixelzeilen:** Eine 1 px
        // starke Linie auf einer halben Pixelgrenze wird von der Kantenglättung auf zwei
        // Zeilen verteilt — wer Zeilen zählt, findet dann zwei Linien, wo eine ist.
        int spanne = (int)(seite.Setup.TextBreiteCm * 20);
        int baender = 0;
        bool drin = false;

        for (int y = 0; y < bmp.Height; y++)
        {
            int rote = 0;
            for (int x = 21; x < 20 + spanne - 1; x++)
            {
                var p = bmp.GetPixel(x, y);
                // „Rot überwiegt" statt „rein rot": die Glättung mischt Weiß dazu.
                if (p.Red > p.Green + 40 && p.Red > p.Blue + 40) rote++;
            }

            bool volleZeile = rote > spanne * 0.9;
            if (volleZeile && !drin) baender++;
            drin = volleZeile;
        }

        Assert.Equal(1, baender);
    }

    // ==================== Grafiken ====================

    /// <summary>
    /// <b>Ein Diagramm ohne Zahlen wird als Kasten gezeichnet und verschwindet nicht still</b>
    /// (§7). Ein Kasten sagt „hier fehlt etwas", während eine Leerstelle sagte „hier war nie
    /// etwas" — und aus keiner Reihe gibt es kein Bild (§4.25).
    /// </summary>
    [Fact]
    public void Ein_Diagramm_ohne_Zahlen_wird_als_benannter_Kasten_gezeichnet()
    {
        var diagramm = new TdChart(TdChartKind.Column, widthCm: 6, heightCm: 4);
        Assert.True(TdChartLayout.Rechnen(diagramm).IstLeer);

        var seite = Setzen(Dok(Blatt(), new TdParagraph([diagramm])));
        using var bmp = Malen(seite, massstab: 20);

        // Der Kasten steht im Textbereich und hat einen Rand — irgendwo dort ist Farbe.
        Assert.True(FarbigeIm(bmp, 20, 20, (int)(6 * 20), (int)(4 * 20)) > 0,
            "Für das Diagramm wurde nichts gezeichnet.");
    }

    // ==================== Diagramme ====================

    /// <summary>
    /// Ein Diagramm mit Zahlen, allein auf einer Seite. Zurück kommen die Pixel <b>und</b> der
    /// Ort seines Kastens auf dem Papier — die Wächter rechnen daraus aus, wo etwas sein muss.
    /// </summary>
    private static (SKBitmap Bild, double LinksCm, double ObenCm) Diagrammseite(
        TdChart diagramm, double massstab = 20)
    {
        var seite = Setzen(Dok(Blatt(), new TdParagraph([diagramm])));
        var zeile = seite.Lines[0];
        var lauf = Assert.Single(zeile.Runs);

        // +1 cm Seitenrand; die Grafik sitzt **auf** der Grundlinie (§4.21).
        double linksCm = 1 + lauf.XCm;
        double obenCm = 1 + zeile.YCm + zeile.BaselineCm - diagramm.HeightCm;

        return (Malen(seite, massstab), linksCm, obenCm);
    }

    /// <summary>Die Farbe an einem Ort, der in Zentimetern des Diagramms angegeben ist.</summary>
    private static SKColor Am(SKBitmap bmp, double linksCm, double obenCm,
        double xCm, double yCm, double massstab)
    {
        var p = bmp.GetPixel((int)((linksCm + xCm) * massstab), (int)((obenCm + yCm) * massstab));
        return new SKColor(p.Red, p.Green, p.Blue);
    }

    private static TdChart MitZahlen(TdChartKind art, params double[] werte)
    {
        var d = new TdChart(art, widthCm: 8, heightCm: 6);
        d.Series.Add(new TdChartSeries("", werte));
        d.Palette.Add("#FF0000");
        return d;
    }

    /// <summary>
    /// <b>Die Säule steht dort, wo die Rechnung sie hingelegt hat</b> — und in der Farbe, die
    /// das Diagramm mitbringt. Geprüft wird nicht das Bild, sondern der Ort: mit
    /// <see cref="TdChartLayout"/> steht vorher fest, wo Farbe sein muss, und daneben, wo keine
    /// sein darf. <b>Kein Pixel-Hash</b>, denn an den Achsen steht Schrift (§4.6).
    /// </summary>
    [Fact]
    public void Eine_Saeule_steht_an_ihrem_gerechneten_Ort()
    {
        var diagramm = MitZahlen(TdChartKind.Column, 1, 2, 3);
        var plan = TdChartLayout.Rechnen(diagramm);
        Assert.Equal(3, plan.Flaechen.Count);

        var (bmp, links, oben) = Diagrammseite(diagramm);
        using var _ = bmp;

        foreach (var saeule in plan.Flaechen)
        {
            var farbe = Am(bmp, links, oben, saeule.Kasten.MitteXCm, saeule.Kasten.MitteYCm, 20);
            Assert.True(farbe.Red > 200 && farbe.Green < 80,
                $"In der Säule bei {saeule.Kasten.MitteXCm:0.00} cm steht {farbe} statt Rot.");
        }

        // **Über der höchsten Säule ist Papier.** Ohne diese Gegenprobe bestünde der Wächter
        // auch dann, wenn der Zeichner die ganze Zeichenfläche rot ausmalte.
        var hoechste = plan.Flaechen[2].Kasten;
        Assert.Equal(SKColors.White,
            Am(bmp, links, oben, hoechste.MitteXCm, hoechste.YCm - 0.3, 20));
    }

    /// <summary>
    /// <b>Der Balken wächst nach rechts, nicht nach oben</b> — Säule und Balken sind zwei
    /// Diagrammarten und nicht dieselbe gedreht. Der Fehler wäre auf einem Bildschirmfoto sofort
    /// zu sehen und in keinem Zahlentest.
    /// </summary>
    [Fact]
    public void Ein_Balken_faerbt_seine_Laenge_und_nicht_seine_Hoehe()
    {
        var diagramm = MitZahlen(TdChartKind.Bar, 1, 3);
        var plan = TdChartLayout.Rechnen(diagramm);

        var (bmp, links, oben) = Diagrammseite(diagramm);
        using var _ = bmp;

        var langer = plan.Flaechen[1].Kasten;
        var kurzer = plan.Flaechen[0].Kasten;

        // Am rechten Ende des langen Balkens ist Farbe …
        var farbe = Am(bmp, links, oben, langer.RechtsCm - 0.1, langer.MitteYCm, 20);
        Assert.True(farbe.Red > 200 && farbe.Green < 80, $"Am Balkenende steht {farbe}.");

        // … und auf derselben Höhe beim kurzen Balken ist keine.
        Assert.Equal(SKColors.White,
            Am(bmp, links, oben, langer.RechtsCm - 0.1, kurzer.MitteYCm, 20));
    }

    /// <summary>
    /// Der Kuchen färbt seine Stücke. Geprüft an zwei Punkten, deren Winkel bekannt ist: Bei
    /// den Werten 3 und 1 reicht das erste Stück von oben im Uhrzeigersinn über drei Viertel —
    /// rechts von der Mitte liegt es, links oben liegt das zweite.
    /// </summary>
    [Fact]
    public void Der_Kuchen_faerbt_seine_Stuecke()
    {
        var diagramm = new TdChart(TdChartKind.Pie, widthCm: 8, heightCm: 6);
        diagramm.Series.Add(new TdChartSeries("", 3, 1));
        diagramm.Palette.AddRange(["#FF0000", "#0000FF"]);

        var plan = TdChartLayout.Rechnen(diagramm);
        Assert.Equal(2, plan.Stuecke.Count);

        var (bmp, links, oben) = Diagrammseite(diagramm);
        using var _ = bmp;

        var mitte = plan.Stuecke[0].Mitte;
        double r = plan.Stuecke[0].RadiusCm * 0.5;

        // 0° zeigt nach rechts — dort liegt das erste (rote) Stück.
        var erstes = Am(bmp, links, oben, mitte.XCm + r, mitte.YCm, 20);
        Assert.True(erstes.Red > 200 && erstes.Blue < 80, $"Rechts der Mitte steht {erstes}.");

        // 225° zeigt nach links oben — dort liegt das zweite (blaue) Stück.
        var zweites = Am(bmp, links, oben,
            mitte.XCm - r * 0.707, mitte.YCm - r * 0.707, 20);
        Assert.True(zweites.Blue > 200 && zweites.Red < 80, $"Links oben steht {zweites}.");
    }

    /// <summary>
    /// <b>Ein Netz mit zwei Kategorien bleibt ein Platzhalter</b> — zwei Ecken ergäben eine
    /// Strecke, die wie ein Zeichenfehler aussieht. Der Kasten sagt stattdessen, dass hier
    /// etwas fehlt (§4.24).
    /// </summary>
    [Fact]
    public void Ein_Netz_mit_zwei_Ecken_bekommt_den_Platzhalter()
    {
        var diagramm = MitZahlen(TdChartKind.Radar, 4, 6);
        Assert.True(TdChartLayout.Rechnen(diagramm).IstLeer);

        var (bmp, links, oben) = Diagrammseite(diagramm);
        using var _ = bmp;

        // Der Platzhalter ist grau und gestrichelt — im Kasten steht Farbe, aber kein Rot.
        int rote = 0;
        for (double y = 0.1; y < diagramm.HeightCm; y += 0.1)
            for (double x = 0.1; x < diagramm.WidthCm; x += 0.1)
                if (Am(bmp, links, oben, x, y, 20) is { Red: > 200, Green: < 80 }) rote++;

        Assert.Equal(0, rote);
        Assert.True(FarbigeIm(bmp, 20, 20, (int)(8 * 20), (int)(6 * 20)) > 0,
            "Der Platzhalter ist ganz ausgeblieben.");
    }

    /// <summary>
    /// <b>Das Diagramm bleibt in seinem Kasten.</b> Der Umbruch hat genau so viel Platz
    /// reserviert, wie am Modell steht (§4.21); was darüber hinausliefe, stünde im Text daneben —
    /// und der Umbruch wüsste nichts davon.
    /// </summary>
    [Fact]
    public void Das_Diagramm_bleibt_in_seinem_Kasten()
    {
        var diagramm = MitZahlen(TdChartKind.Column, 4, 7, 3, 6);
        diagramm.Title = "Ein Titel, der über die ganze Breite läuft";

        var (bmp, links, _) = Diagrammseite(diagramm);
        using var _bmp = bmp;

        // Rechts neben dem Diagramm ist der Textbereich noch 2 cm breit, und dort steht nichts.
        int rechts = (int)((links + diagramm.WidthCm + 0.05) * 20);
        Assert.Equal(0, FarbigeIm(bmp, rechts, 0, bmp.Width - rechts, bmp.Height));
    }

    /// <summary>
    /// <b>Punkt ist nicht Pixel — auch im Diagramm</b> (§7). Schriftgrad, Linienstärke und
    /// Markenradius stehen in Zentimetern und müssen mit dem Maßstab wachsen; sonst ist die
    /// Achsenbeschriftung beim Druck mit 300 dpi ein Haar. Geprüft wird die Umschließung der
    /// Tinte: sie muss sich verdoppeln, nicht gleich bleiben.
    /// </summary>
    [Fact]
    public void Der_Massstab_skaliert_auch_das_Diagramm()
    {
        var diagramm = MitZahlen(TdChartKind.Column, 1, 2, 3);
        var seite = Setzen(Dok(Blatt(), new TdParagraph([diagramm])));

        using var klein = Malen(seite, massstab: 30);
        using var gross = Malen(seite, massstab: 60);

        var a = TinteKasten(klein);
        var b = TinteKasten(gross);

        Assert.False(a.IsEmpty, "Bei kleinem Maßstab wurde nichts gezeichnet.");
        Assert.InRange((double)b.Height / a.Height, 1.9, 2.1);
        Assert.InRange((double)b.Width / a.Width, 1.9, 2.1);
    }

    /// <summary>
    /// <b>Fehlt der Blob, fehlt das Bild und nicht die Seite</b> (Dauerregel 4). Eine
    /// unvollständige Sicherung ist kein Programmierfehler — aber sie darf auch nicht
    /// spurlos bleiben: der Platzhalterkasten bleibt stehen.
    /// </summary>
    [Fact]
    public void Ein_Bild_ohne_Blob_kostet_das_Bild_und_nicht_die_Seite()
    {
        var bild = new TdImage(Guid.NewGuid(), "png", widthCm: 4, heightCm: 3);
        var seite = Setzen(Dok(Blatt(), new TdParagraph([bild])));

        using var bmp = Malen(seite, massstab: 20, new TdRenderContext(new TdMemoryImages()));

        Assert.True(FarbigeIm(bmp, 20, 20, (int)(4 * 20), (int)(3 * 20)) > 0,
            "Ohne Blob blieb die Stelle vollständig leer.");
    }

    /// <summary>Ein vorhandenes Bild wird gezeichnet — und zwar in seinen Kasten.</summary>
    [Fact]
    public void Ein_Bild_mit_Blob_wird_gezeichnet()
    {
        var bilder = new TdMemoryImages();
        var id = bilder.Ablegen(Png(40, 30, SKColors.Blue), "png");

        var bild = new TdImage(id, "png", widthCm: 4, heightCm: 3);
        var seite = Setzen(Dok(Blatt(), new TdParagraph([bild])));

        using var bmp = Malen(seite, massstab: 20, new TdRenderContext(bilder));

        // Mitte des Bildkastens: blau. Der Kasten sitzt auf der Grundlinie der ersten Zeile.
        var lauf = Assert.Single(seite.Lines[0].Runs);
        double grundlinie = seite.Lines[0].YCm + seite.Lines[0].BaselineCm;
        int x = 20 + (int)((lauf.XCm + 2) * 20);
        int y = 20 + (int)((grundlinie - 1.5) * 20);

        var p = bmp.GetPixel(x, y);
        Assert.True(p.Blue > 200 && p.Red < 80, $"Erwartet Blau, gefunden {p}.");
    }

    // ==================== Kopf- und Fußzeile ====================

    /// <summary>
    /// <b>Die Platzhalter werden aufgelöst, und die Seitenzahl kennt nur der Zeichner.</b> Sie
    /// steht nicht im Umbruch: Kopf- und Fußzeile gehören zur Seite und nicht zum Textfluss
    /// (§4.15). Geprüft wird schriftfrei — dass im oberen und unteren Rand überhaupt etwas
    /// steht, und dass auf der ersten Seite nichts steht, wenn sie unterdrückt ist.
    /// </summary>
    [Fact]
    public void Kopfzeile_fehlt_auf_der_ersten_Seite_und_steht_auf_der_zweiten()
    {
        var blatt = Blatt();
        blatt.HeaderText = "Seite {SEITE} von {SEITEN}";
        blatt.SuppressOnFirstPage = true;

        // Zwei Seiten: zehn Zeilen passen aufs Blatt, elf nicht.
        var absaetze = Enumerable.Range(0, 12).Select(i => (TdBlock)new TdParagraph($"Z{i}")).ToArray();
        var umbruch = TdLayout.Umbrechen(Dok(blatt, absaetze), new FesteMessung());
        Assert.True(umbruch.PageCount >= 2);

        var kontext = new TdRenderContext(Seitenzahl: umbruch.PageCount);

        using var erste = Malen(umbruch.Pages[0], massstab: 20, kontext);
        using var zweite = Malen(umbruch.Pages[1], massstab: 20, kontext);

        // Oberer Rand = 1 cm = die ersten 20 Pixel.
        Assert.Equal(0, FarbigeIm(erste, 0, 0, erste.Width, 19));
        Assert.True(FarbigeIm(zweite, 0, 0, zweite.Width, 19) > 0,
            "Auf Seite 2 steht keine Kopfzeile.");
    }

    /// <summary>
    /// Das Wasserzeichen liegt **unter** dem Text. Andersherum stünde der Text darunter und
    /// wäre je nach Deckkraft nicht mehr zu lesen.
    /// </summary>
    [Fact]
    public void Das_Wasserzeichen_liegt_unter_dem_Text()
    {
        var bilder = new TdMemoryImages();
        var blatt = Blatt();
        blatt.Watermark = new TdImage(
            bilder.Ablegen(Png(60, 60, SKColors.Red), "png"), "png",
            blatt.WidthCm, blatt.HeightCm);
        blatt.WatermarkOpacity = 1.0;

        var seite = Setzen(Dok(blatt, new TdParagraph("MMMM")));
        using var bmp = Malen(seite, massstab: 20, new TdRenderContext(bilder));

        // Im Rand, wo kein Text steht, ist das Wasserzeichen zu sehen.
        var rand = bmp.GetPixel(bmp.Width / 2, 10);
        Assert.True(rand.Red > 150 && rand.Green < 120, $"Kein Wasserzeichen im Rand, gefunden {rand}.");
    }

    // ==================== Schreibmarke und Auswahl (Schritt 4) ====================

    /// <summary>
    /// <b>Ohne Markierung ändert sich am Bild nichts.</b> Das ist der wichtigere der beiden
    /// Fälle: Beim Export, beim Drucken und in jeder Vorschau gibt es keinen Cursor — ein
    /// Strich, der dort mitgedruckt wird, fällt erst auf dem Papier auf.
    /// </summary>
    [Fact]
    public void Ohne_Markierung_wird_nichts_zusaetzliches_gezeichnet()
    {
        var seite = Setzen(Dok(Blatt(), new TdParagraph("MMMM")));

        using var ohne = Malen(seite, massstab: 20);
        using var leer = Malen(seite, massstab: 20, new TdRenderContext(Markierung: new TdMarkierung()));

        Assert.Equal(TinteKasten(ohne), TinteKasten(leer));
    }

    /// <summary>
    /// Die Schreibmarke ist ein senkrechter Strich an der gerechneten Stelle — <b>so hoch wie
    /// die Schrift und um die Grundlinie herum</b>.
    ///
    /// <para>
    /// <b>Nicht über die Zeilenhöhe</b>, und das ist die Änderung aus §4.35: Die Zeilenhöhe
    /// trägt den Absatzabstand mit, und der Strich war damit zwischen zwei Absätzen doppelt so
    /// hoch wie die Buchstaben daneben. Aufgefallen ist es in der Sekunde, in der zum ersten
    /// Mal wirklich eine Marke auf dem Schirm stand — die Anzeige als Prüfmittel (§4.28).
    /// </para>
    /// <para>
    /// Geprüft wird deshalb die **Beziehung** und nicht die Zahl: Der Strich muss die
    /// Grundlinie kreuzen und niedriger sein als die Zeile. Eine feste Höhe stünde hier nicht,
    /// weil der Umbruch dieser Wächter mit fester Messung rechnet und der Zeichner mit der
    /// echten Schrift — im laufenden Programm ist es dieselbe (§7).
    /// </para>
    /// </summary>
    [Fact]
    public void Die_Schreibmarke_steht_als_Strich_um_die_Grundlinie()
    {
        var doc = Dok(Blatt(), new TdParagraph());          // leerer Absatz: kein Text im Weg
        var seite = Setzen(doc);

        var zeile = seite.Lines[0];
        var markierung = new TdMarkierung { MarkeZeile = zeile, MarkeXCm = 3 };
        using var bmp = Malen(seite, massstab: 20, new TdRenderContext(Markierung: markierung));

        var kasten = TinteKasten(bmp);

        // 1 cm Rand + 3 cm, bei 20 px/cm.
        Assert.InRange(kasten.Left, 79, 81);
        Assert.InRange(kasten.Width, 1, 3);

        double grundlinie = (1 + zeile.YCm + zeile.BaselineCm) * 20;
        Assert.True(
            kasten.Top < grundlinie && kasten.Bottom > grundlinie,
            $"Der Strich ({kasten.Top}–{kasten.Bottom}) kreuzt die Grundlinie {grundlinie} nicht.");

        Assert.True(
            kasten.Height < zeile.HeightCm * 20,
            $"Der Strich ist {kasten.Height} px hoch und damit nicht niedriger als die Zeile.");
    }

    /// <summary>
    /// <b>Der Fall, der die Regel erzwungen hat:</b> Ein Absatzabstand macht die Zeile höher,
    /// die Schrift aber nicht. Ein Strich über die ganze Zeilenhöhe reichte damit weit in den
    /// Zwischenraum hinein und sah aus wie ein Trennstrich zwischen zwei Absätzen.
    /// </summary>
    [Fact]
    public void Ein_Absatzabstand_macht_die_Schreibmarke_nicht_hoeher()
    {
        var doc = new TdDocument
        {
            DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 28 },   // ein knapper cm
            Sections = { new TdSection([new TdParagraph()]) { Page = Blatt() } },
        };

        var seite = Setzen(doc);
        var zeile = seite.Lines[0];

        Assert.True(zeile.HeightCm > 1.5, "Der Absatzabstand ist gar nicht in der Zeile gelandet.");

        var markierung = new TdMarkierung { MarkeZeile = zeile, MarkeXCm = 3 };
        using var bmp = Malen(seite, massstab: 20, new TdRenderContext(Markierung: markierung));

        // Der Strich hört über dem Zwischenraum auf — nicht am unteren Rand der Zeile.
        Assert.True(
            TinteKasten(bmp).Bottom < (1 + zeile.YCm + zeile.HeightCm) * 20 - 10,
            "Der Strich reicht bis in den Absatzabstand hinein.");
    }

    /// <summary>
    /// Der Auswahlkasten liegt in der Farbe des Editors über die **ganze Zeilenhöhe** — sonst
    /// stünden zwischen den Zeilen weiße Streifen, und die Auswahl sähe zerrissen aus.
    /// </summary>
    [Fact]
    public void Die_Auswahl_liegt_als_Kasten_ueber_der_Zeilenhoehe()
    {
        var doc = Dok(Blatt(), new TdParagraph());
        var seite = Setzen(doc);

        var markierung = new TdMarkierung();
        markierung.Auswahl[seite.Lines[0]] = new TdSpanne(2, 5);

        using var bmp = Malen(seite, massstab: 20, new TdRenderContext(Markierung: markierung));

        // Innerhalb: 1 cm Rand + 3 cm = 80 px, Mitte der ersten Zeile = 20 + 10 px.
        Assert.Equal(TdRenderer.Auswahlfarbe, Ohne_Alpha(bmp.GetPixel(80, 30)));

        // Oberkante und Unterkante der Zeile gehören dazu, links davon nicht.
        Assert.Equal(TdRenderer.Auswahlfarbe, Ohne_Alpha(bmp.GetPixel(80, 21)));
        Assert.Equal(TdRenderer.Auswahlfarbe, Ohne_Alpha(bmp.GetPixel(80, 38)));
        Assert.Equal(SKColors.White, Ohne_Alpha(bmp.GetPixel(50, 30)));
    }

    // ==================== Der öffentliche Einstieg für ein Diagramm (§4.50) ====================

    /// <summary>Zeichnet ein Diagramm für sich allein — genau der Weg, den der WPF-Kopf nimmt.</summary>
    private static SKBitmap MalenDiagramm(TdChart diagramm, double massstab)
    {
        int breite = (int)Math.Ceiling(diagramm.WidthCm * massstab);
        int hoehe = (int)Math.Ceiling(diagramm.HeightCm * massstab);

        var bmp = new SKBitmap(new SKImageInfo(breite, hoehe, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var leinwand = new SKCanvas(bmp))
        {
            leinwand.Clear(SKColors.White);
            TdRenderer.Diagramm(leinwand, diagramm, SKRect.Create(0, 0, breite, hoehe), massstab);
        }
        return bmp;
    }

    private static TdChart Saeulen(double breite = 8, double hoehe = 5)
    {
        var d = new TdChart(TdChartKind.Column, breite, hoehe) { Title = "Noten" };
        d.Series.Add(new TdChartSeries("Halbjahr", 2, 3, 1));
        return d;
    }

    /// <summary>
    /// <b>Der öffentliche Einstieg zeichnet, und zwar in den übergebenen Kasten</b>
    /// (HANDOFF §4.50).
    ///
    /// <para>
    /// <b>Wozu es ihn gibt:</b> Bis §4.50 war <c>DiagrammZeichnen</c> privat, und deshalb war
    /// die letzte Lücke aus §4.45 nicht zu schließen — der WPF-Editor kennt keine Diagramme
    /// und braucht ein <i>Bild</i>, um eines anzuzeigen und als Auflage durch die Rundreise zu
    /// bringen.
    /// </para>
    /// </summary>
    [Fact]
    public void Der_oeffentliche_Einstieg_zeichnet_ein_Diagramm()
    {
        using var bmp = MalenDiagramm(Saeulen(), massstab: 40);
        var tinte = TinteKasten(bmp);

        Assert.False(tinte.IsEmpty, "Es wurde nichts gezeichnet.");

        // Es füllt seinen Kasten aus und sitzt nicht in einer Ecke: Ein Diagramm, das sich auf
        // ein Viertel der Fläche zurückzieht, wäre hier sonst grün.
        Assert.True(tinte.Width > bmp.Width / 2, $"Zu schmal: {tinte.Width} von {bmp.Width}.");
        Assert.True(tinte.Height > bmp.Height / 2, $"Zu flach: {tinte.Height} von {bmp.Height}.");
    }

    /// <summary>
    /// <b>Ein Diagramm ohne Zahlen bleibt ein Kasten und wird nicht zu nichts</b> (§7).
    ///
    /// <para>
    /// <b>Deshalb hat der Einstieg keinen Rückgabewert</b> (§4.50): Müsste der Aufrufer den
    /// Platzhalter selbst nachbauen, gäbe es zwei davon und damit zwei Wahrheiten. <b>Ein
    /// Kasten sagt „hier fehlt etwas", eine Leerstelle sagt „hier war nie etwas"</b> — und im
    /// Editor wäre das der Unterschied zwischen „das Diagramm rechnet gerade nichts aus" und
    /// „das Diagramm ist weg".
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_Diagramm_ohne_Zahlen_bleibt_ein_Kasten()
    {
        var leer = new TdChart(TdChartKind.Column, 8, 5);   // keine Reihen
        using var bmp = MalenDiagramm(leer, massstab: 40);
        var tinte = TinteKasten(bmp);

        Assert.False(tinte.IsEmpty, "Der Platzhalter fehlt — das Diagramm wäre unauffindbar.");

        // Der gestrichelte Rahmen läuft um die ganze Fläche, nicht um einen Teil davon.
        Assert.True(tinte.Left <= 2 && tinte.Top <= 2, $"Rahmen sitzt nicht oben links: {tinte}.");
        Assert.True(
            tinte.Right >= bmp.Width - 3 && tinte.Bottom >= bmp.Height - 3,
            $"Rahmen reicht nicht bis unten rechts: {tinte} bei {bmp.Width}x{bmp.Height}.");
    }

    /// <summary>
    /// <b>Der Maßstab wirkt, und deshalb darf der Kopf feiner rastern.</b>
    ///
    /// <para>
    /// Der WPF-Kopf zeichnet mit dem Doppelten der Anzeigegröße (<c>TdZuFlow.Feinheit</c>),
    /// damit ein Diagramm beim Vergrößern des Editors nicht ausfranst. <b>Das setzt voraus,
    /// dass der Zeichner den Maßstab überall durchreicht</b> — Linienstärken und Beschriftung
    /// eingeschlossen. Ein Zeichner, der nur die Geometrie skaliert, lieferte bei doppelter
    /// Auflösung haarfeine Linien und eine winzige Schrift.
    /// </para>
    /// </summary>
    [Fact]
    public void Der_Massstab_wirkt_auf_das_ganze_Diagramm()
    {
        using var klein = MalenDiagramm(Saeulen(), massstab: 30);
        using var gross = MalenDiagramm(Saeulen(), massstab: 60);

        double anteilKlein = (double)FarbigeIm(klein, 0, 0, klein.Width, klein.Height)
            / (klein.Width * klein.Height);
        double anteilGross = (double)FarbigeIm(gross, 0, 0, gross.Width, gross.Height)
            / (gross.Width * gross.Height);

        // Der **Anteil** bleibt ungefähr gleich, wenn alles mitwächst. Skalierte nur die
        // Geometrie, fiele er bei doppelter Auflösung deutlich ab (Striche und Schrift blieben
        // gleich dick bzw. gleich groß in Pixeln).
        Assert.InRange(anteilGross, anteilKlein * 0.75, anteilKlein * 1.25);
    }

    // ==================== Das fertige PNG für die Tafel (§4.82) ====================

    /// <summary>
    /// <b>Ein Diagramm als PNG, für die Tafel</b> (§4.82). Sie kennt kein
    /// <see cref="TdChart"/>; dort <i>ist</i> ein Diagramm ein Bild. Bis §4.82 hat der WPF-Kopf
    /// dafür seine eigene Zeichnung gehabt (<c>ChartDialog</c> mit <c>DrawingVisual</c>) —
    /// also die sieben Diagrammarten ein zweites Mal.
    /// </summary>
    [Fact]
    public void Ein_Diagramm_wird_zu_einem_lesbaren_PNG()
    {
        var png = TdRenderer.DiagrammPng(Saeulen(), SKColors.White);

        using var bmp = SKBitmap.Decode(png);
        Assert.NotNull(bmp);

        var tinte = TinteKasten(bmp);
        Assert.False(tinte.IsEmpty, "Das PNG ist leer.");
        Assert.True(tinte.Width > bmp.Width / 2, $"Zu schmal: {tinte.Width} von {bmp.Width}.");
    }

    /// <summary>
    /// <b>Der Grund ist weiß und nicht durchsichtig.</b> Der Editor bekommt seinen Grund von
    /// der Seite, die Tafel hat keinen — ein PNG mit durchsichtigem Grund sähe auf einer
    /// dunklen Tafel aus wie ein Fehler, und zwar erst beim Nutzer.
    /// </summary>
    [Fact]
    public void Der_Grund_des_PNG_ist_weiss_und_nicht_durchsichtig()
    {
        var png = TdRenderer.DiagrammPng(Saeulen(), SKColors.White);

        using var bmp = SKBitmap.Decode(png);
        var ecke = bmp.GetPixel(1, 1);

        Assert.Equal(byte.MaxValue, ecke.Alpha);
        Assert.Equal(SKColors.White, Ohne_Alpha(ecke));
    }

    /// <summary>
    /// <b>Ohne Grundfarbe bleibt der Grund durchsichtig — der Fall des Editors.</b> Dort gibt
    /// die Seite den Grund; ein weißer Kasten stäche auf getöntem Papier und über einem
    /// Wasserzeichen heraus. Deshalb ist die Angabe Pflicht und keine Vorgabe: Sonst erbte
    /// einer der beiden Aufrufer sie stillschweigend, und zwar der, der später dazukommt.
    /// </summary>
    [Fact]
    public void Ohne_Grundfarbe_bleibt_der_Grund_durchsichtig()
    {
        var png = TdRenderer.DiagrammPng(Saeulen(), grund: null);

        using var bmp = SKBitmap.Decode(png);
        Assert.Equal(0, bmp.GetPixel(1, 1).Alpha);

        // Gezeichnet wurde trotzdem — durchsichtig heißt nicht leer.
        Assert.False(TinteKasten(bmp).IsEmpty, "Es wurde nichts gezeichnet.");
    }

    /// <summary>
    /// <b>Die Feinheit wirkt, und sie ist der Grund für die Größe.</b> Ein Diagramm ist
    /// Strichzeichnung mit Beschriftung; bei einfacher Auflösung franst es beim Vergrößern
    /// genau dann aus, wenn jemand hinsieht.
    ///
    /// <para>
    /// <b>Warum hier nicht auf das Pixel genau geprüft wird</b> — die erste Fassung tat es und
    /// ist gefallen (604 gegen 605): Gerundet wird <b>nach</b> der Multiplikation mit dem
    /// Maßstab und nicht davor, also ist <c>Round(x·2m)</c> nicht immer <c>2·Round(x·m)</c>.
    /// Das ist die richtige Reihenfolge — rundete der Zeichner zuerst auf ganze Pixel und
    /// vervielfachte danach, summierte sich der Fehler mit der Größe des Diagramms auf.
    /// </para>
    /// </summary>
    [Fact]
    public void Die_Feinheit_bestimmt_die_Groesse_des_PNG()
    {
        using var einfach = SKBitmap.Decode(TdRenderer.DiagrammPng(Saeulen(), SKColors.White, feinheit: 1.0));
        using var doppelt = SKBitmap.Decode(TdRenderer.DiagrammPng(Saeulen(), SKColors.White, feinheit: 2.0));

        Assert.InRange(doppelt.Width, einfach.Width * 2 - 1, einfach.Width * 2 + 1);
        Assert.InRange(doppelt.Height, einfach.Height * 2 - 1, einfach.Height * 2 + 1);
    }

    /// <summary>
    /// <b>Auch ein Diagramm ohne Zahlen liefert ein Bild</b> — den Platzhalterkasten. Käme
    /// hier nichts oder <c>null</c> heraus, verschwände auf der Tafel ein Element, das der
    /// Nutzer gerade eingefügt hat, ohne Meldung (§7).
    /// </summary>
    [Fact]
    public void Auch_ein_leeres_Diagramm_liefert_ein_Bild()
    {
        var png = TdRenderer.DiagrammPng(new TdChart(TdChartKind.Column, 8, 5), SKColors.White);

        using var bmp = SKBitmap.Decode(png);
        Assert.NotNull(bmp);
        Assert.False(TinteKasten(bmp).IsEmpty, "Der Platzhalterkasten fehlt.");
    }

    private static SKColor Ohne_Alpha(SKColor farbe) => new(farbe.Red, farbe.Green, farbe.Blue);
}
