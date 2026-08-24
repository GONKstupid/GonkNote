using GonkNote.Core.Editing;
using GonkNote.Core.Rendering;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using GonkNote.Core.Theming;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="Bildsammlung"/> und <see cref="StickerLibrary"/> — neu in Phase 4.5, weil der
/// Linux-Kopf Sticker bekommt. Beides lag privat im WPF-Kopf, und der Nutzerordner war dort
/// **von Hand** aus <c>Environment.SpecialFolder.ApplicationData</c> gebaut: eine
/// Windows-Festlegung mitten in einer Regel, die für alle Köpfe gelten soll.
/// </summary>
public sealed class SammlungTests
{
    // ---------- Welche Dateien zählen ----------

    [Theory]
    [InlineData("bild.png")]
    [InlineData("bild.jpg")]
    [InlineData("bild.jpeg")]
    [InlineData("bild.webp")]
    [InlineData("BILD.PNG")]        // Groß-/Kleinschreibung egal
    [InlineData("a.b.c.png")]       // mehrere Punkte
    public void Bilder_werden_angenommen(string name) =>
        Assert.True(Bildsammlung.IstBild(name));

    /// <summary>
    /// <b>SVG zählt bewusst nicht.</b> Eine Vektordatei muss vor dem Einfügen gerastert
    /// werden, und das gehört an die Stelle, die die Zielgröße kennt — nicht an die, die den
    /// Ordner liest. Käme sie hier durch, landete sie ungerastert im Bildpfad.
    /// </summary>
    [Theory]
    [InlineData("zeichnung.svg")]
    [InlineData("text.txt")]
    [InlineData("ohneendung")]
    [InlineData("bild.png.bak")]
    public void Alles_andere_nicht(string name) =>
        Assert.False(Bildsammlung.IstBild(name));

    // ---------- Ordner lesen ----------

    /// <summary>
    /// Ein fehlender Ordner ist der <b>Normalfall</b>, solange der Nutzer nichts hineingelegt
    /// hat — er darf keine Ausnahme werfen, sonst bliebe die Sammlung beim ersten Start leer
    /// *und* laut.
    /// </summary>
    [Fact]
    public void Ein_fehlender_Ordner_ergibt_eine_leere_Liste_und_wirft_nicht()
    {
        var weg = Path.Combine(Path.GetTempPath(), "gonk-gibt-es-nicht-" + Guid.NewGuid());
        Assert.Empty(Bildsammlung.Dateien(weg));
    }

    /// <summary>
    /// <b>Sortiert, weil das Dateisystem keine Reihenfolge verspricht.</b> Ohne das wechselte
    /// eine Sammlung zwischen zwei Starts ihre Anordnung, und niemand fände wieder, was er
    /// gestern an dritter Stelle gesehen hat.
    /// </summary>
    [Fact]
    public void Der_Ordnerinhalt_kommt_sortiert_und_gefiltert_zurueck()
    {
        using var tmp = new TempWorkspace("sammlung");
        foreach (var n in new[] { "zebra.png", "alpha.PNG", "mitte.jpg", "notiz.txt", "vektor.svg" })
            File.WriteAllBytes(Path.Combine(tmp.Root, n), [1]);

        var ist = Bildsammlung.Dateien(tmp.Root).Select(Path.GetFileName).ToList();

        Assert.Equal(["alpha.PNG", "mitte.jpg", "zebra.png"], ist);
    }

    // ---------- Sticker ----------

    /// <summary>
    /// <b>Der Wächter gegen den Rückfall.</b> Der Nutzerordner muss aus dem Datenordner
    /// kommen — unter Linux ist das <c>~/.config/GonkNote</c>, unter Windows
    /// <c>%APPDATA%\GonkNote</c>. Wer ihn wieder von Hand zusammensetzt, baut eine
    /// Windows-Festlegung in eine gemeinsame Regel.
    /// </summary>
    [Fact]
    public void Der_eigene_Stickerordner_liegt_im_Datenordner()
    {
        using var tmp = new TempWorkspace("sammlung");
        var vorher = Platform.AppPaths.Current;
        try
        {
            Platform.AppPaths.Current = new TestPfade(tmp.Root);
            Assert.Equal(Path.Combine(tmp.Root, "Stickers"), StickerLibrary.UserFolder);
            Assert.True(Directory.Exists(StickerLibrary.UserFolder), "Der Ordner wird angelegt.");
        }
        finally { Platform.AppPaths.Current = vorher; }
    }

    /// <summary>Mitgelieferte zuerst, eigene danach — die Reihenfolge ist die Aussage.</summary>
    [Fact]
    public void Eigene_Sticker_stehen_hinter_den_mitgelieferten()
    {
        using var tmp = new TempWorkspace("sammlung");
        var vorher = Platform.AppPaths.Current;
        try
        {
            Platform.AppPaths.Current = new TestPfade(tmp.Root);
            File.WriteAllBytes(Path.Combine(StickerLibrary.UserFolder, "eigener.png"), [1]);

            var ist = StickerLibrary.Alle().Select(Path.GetFileName).ToList();

            // Mitgelieferte gibt es in der Testumgebung nicht — geprüft wird, dass der
            // eigene gefunden wird und die Liste nicht wirft.
            Assert.Contains("eigener.png", ist);
        }
        finally { Platform.AppPaths.Current = vorher; }
    }

    /// <summary>Wegwerf-Pfade: der Datenordner zeigt in den Temp-Ordner des Tests.</summary>
    private sealed class TestPfade(string wurzel) : Platform.IAppPaths
    {
        public string DataFolder { get; } = wurzel;
        public string AppFolder { get; } = wurzel;
    }

    // ---------- Lesbare Schrift auf dem Notizzettel ----------

    /// <summary>
    /// Die Textfarbe wandert mit dem Zettel in die Datei. Rechneten beide Köpfe sie getrennt,
    /// bekäme derselbe gelbe Zettel je nach Kopf eine andere Schrift — und man sähe es erst,
    /// wenn jemand die Datei auf dem anderen Rechner öffnet.
    /// </summary>
    [Theory]
    [InlineData("#FFFDE68A", "#1F2937")]   // helles Gelb  → dunkler Text
    [InlineData("#FFFFFFFF", "#1F2937")]   // Weiß         → dunkler Text
    [InlineData("#FF1E3A8A", "#F9FAFB")]   // dunkles Blau → heller Text
    [InlineData("#FF000000", "#F9FAFB")]   // Schwarz      → heller Text
    public void Die_Schriftfarbe_folgt_der_Helligkeit(string grund, string erwartet)
    {
        var schrift = HexColor.Parse(grund, HexColor.Black).LesbareSchrift();
        Assert.Equal(erwartet, schrift.ToString());
    }

    // ---------- Gewählte Schriftfarbe: nur überstimmen, wenn sie unlesbar wäre ----------

    /// <summary>
    /// Ein Textfeld erbt die Tintenfarbe des Nutzers. <b>Die soll er behalten</b>, solange man
    /// sie sieht — <c>MitGenugKontrast</c> bestimmt keine Farbe, es überstimmt nur eine
    /// unlesbare.
    /// </summary>
    [Fact]
    public void Eine_gut_lesbare_Farbe_bleibt_stehen()
    {
        var rot = HexColor.Parse("#FFE11D48", HexColor.Black);
        var weiss = HexColor.Parse("#FFFFFFFF", HexColor.Black);
        Assert.Equal(rot, rot.MitGenugKontrast(weiss));
    }

    [Theory]
    [InlineData("#FFEEEEEE", "#FFFFFFFF", "#000000")]   // fast weiß auf weiß → Schwarz
    [InlineData("#FF222222", "#FF000000", "#FFFFFF")]   // fast schwarz auf schwarz → Weiß
    public void Eine_unlesbare_Farbe_wird_ueberstimmt(string text, string grund, string erwartet)
    {
        var ist = HexColor.Parse(text, HexColor.Black)
                          .MitGenugKontrast(HexColor.Parse(grund, HexColor.Black));
        Assert.Equal(erwartet, ist.ToString());
    }

    /// <summary>
    /// <b>Ein fast durchsichtiger Grund zählt nicht.</b> Dann bestimmt die Seite darunter den
    /// Kontrast, und über die weiß diese Rechnung nichts — lieber die Wahl des Nutzers stehen
    /// lassen als gegen einen Grund korrigieren, der gar nicht deckt.
    /// </summary>
    [Fact]
    public void Ein_fast_durchsichtiger_Grund_ueberstimmt_nichts()
    {
        var hell = HexColor.Parse("#FFEEEEEE", HexColor.Black);
        var kaumWeiss = HexColor.Parse("#40FFFFFF", HexColor.Black);   // Alpha 0x40 < 96
        Assert.Equal(hell, hell.MitGenugKontrast(kaumWeiss));
    }

    /// <summary>Ohne Hintergrund gibt es nichts zu vergleichen.</summary>
    [Fact]
    public void Ohne_Grund_bleibt_die_Farbe_unveraendert()
    {
        var farbe = HexColor.Parse("#FFEEEEEE", HexColor.Black);
        Assert.Equal(farbe, farbe.MitGenugKontrast(null));
    }

    /// <summary>Grün wiegt am schwersten, Blau am leichtesten — so sieht das Auge, nicht der Rechner.</summary>
    [Fact]
    public void Die_Helligkeit_wiegt_die_Kanaele_verschieden()
    {
        double gruen = HexColor.Parse("#00FF00", HexColor.Black).Luminanz;
        double rot   = HexColor.Parse("#FF0000", HexColor.Black).Luminanz;
        double blau  = HexColor.Parse("#0000FF", HexColor.Black).Luminanz;

        Assert.True(gruen > rot, "Grün muss heller wiegen als Rot.");
        Assert.True(rot > blau, "Rot muss heller wiegen als Blau.");

        // Reines Grün ist hell genug für dunkle Schrift, reines Blau nicht.
        Assert.Equal("#1F2937", HexColor.Parse("#00FF00", HexColor.Black).LesbareSchrift().ToString());
        Assert.Equal("#F9FAFB", HexColor.Parse("#0000FF", HexColor.Black).LesbareSchrift().ToString());
    }
    // ==================== Wo ein Sticker landet ====================
    //
    // WbEinfuegen.FuerSticker lag bis Phase 4.5 privat im WPF-Kopf. Sie steht jetzt in Core,
    // weil ihr Ergebnis IN DIE DATEI wandert: ein Sticker, der unter Linux anders groß
    // ankommt als unter Windows, ist kein Anzeigeunterschied, sondern ein Datenunterschied.

    private static WbPage Blatt(float breite = 800, float hoehe = 600) =>
        new() { Width = breite, Height = hoehe };

    /// <summary>
    /// Die unendliche Fläche wird nicht gesetzt, sondern <b>gerechnet</b>: <c>IsInfinite</c>
    /// ist „keine Breite oder keine Höhe". Ein Blatt ohne Maße <em>ist</em> die endlose Fläche.
    /// </summary>
    private static WbPage Endlos() => new() { Width = 0, Height = 0 };

    /// <summary>Ein großes Bild wird auf die lange Kante heruntergerechnet, das Seitenverhältnis bleibt.</summary>
    [Fact]
    public void Grosse_Sticker_werden_auf_die_lange_Kante_gebracht()
    {
        var k = WbEinfuegen.FuerSticker(800, 400, new SKPoint(400, 300), Blatt());

        Assert.Equal(160f, k.Width, 3);
        Assert.Equal(80f, k.Height, 3);
    }

    /// <summary>
    /// <b>Nie hinaufgerechnet.</b> Ein kleines Bild auf 160 zu ziehen macht es nur unscharf —
    /// und der Nutzer sähe einen Sticker, den er so nie ausgewählt hat.
    /// </summary>
    [Fact]
    public void Kleine_Sticker_bleiben_klein()
    {
        var k = WbEinfuegen.FuerSticker(32, 16, new SKPoint(400, 300), Blatt());

        Assert.Equal(32f, k.Width, 3);
        Assert.Equal(16f, k.Height, 3);
    }

    /// <summary>Der Sticker sitzt um den übergebenen Punkt herum und nicht mit der Ecke darauf.</summary>
    [Fact]
    public void Der_Sticker_wird_um_den_Punkt_zentriert()
    {
        var k = WbEinfuegen.FuerSticker(100, 60, new SKPoint(400, 300), Blatt());

        Assert.Equal(400f, k.MidX, 3);
        Assert.Equal(300f, k.MidY, 3);
    }

    /// <summary>
    /// Am Rand einer endlichen Seite wird geschoben, nicht beschnitten: ein Sticker, der halb
    /// neben dem Blatt liegt, wäre auf jedem Ausdruck halb weg.
    /// </summary>
    [Fact]
    public void Am_Blattrand_rueckt_der_Sticker_auf_die_Seite()
    {
        var k = WbEinfuegen.FuerSticker(100, 100, new SKPoint(795, 595), Blatt());

        Assert.Equal(700f, k.Left, 3);
        Assert.Equal(500f, k.Top, 3);
        Assert.Equal(800f, k.Right, 3);
        Assert.Equal(600f, k.Bottom, 3);
    }

    /// <summary>Auf der unendlichen Fläche gibt es keinen Rand, an den man rücken könnte.</summary>
    [Fact]
    public void Auf_der_endlosen_Flaeche_wird_nicht_geschoben()
    {
        var k = WbEinfuegen.FuerSticker(100, 100, new SKPoint(-500, -500), Endlos());

        Assert.Equal(-550f, k.Left, 3);
        Assert.Equal(-550f, k.Top, 3);
    }

    /// <summary>
    /// Ein Bild, das größer ist als die Seite, sitzt in der Ecke und ragt hinaus — es wird
    /// <b>nicht</b> geklemmt. Ohne das <c>Math.Max(0, …)</c> in der Rechnung stünde die untere
    /// Grenze über der oberen, und <c>Math.Clamp</c> wirft dann.
    /// </summary>
    [Fact]
    public void Ein_Sticker_groesser_als_die_Seite_wirft_nicht()
    {
        var k = WbEinfuegen.FuerSticker(400, 400, new SKPoint(50, 50), Blatt(100, 100));

        Assert.Equal(0f, k.Left, 3);
        Assert.Equal(0f, k.Top, 3);
        Assert.Equal(160f, k.Width, 3);
    }

    /// <summary>
    /// Ein Bild ohne Maße gäbe eine Division durch null und danach ein Element ohne Fläche —
    /// unsichtbar, unanklickbar, und nur über Rückgängig wieder loszuwerden.
    /// </summary>
    [Fact]
    public void Ein_Bild_ohne_Masse_ergibt_trotzdem_eine_Flaeche()
    {
        var k = WbEinfuegen.FuerSticker(0, 0, new SKPoint(400, 300), Blatt());

        Assert.True(k.Width > 0, "Ein Sticker ohne Breite wäre nicht mehr anfassbar.");
        Assert.True(k.Height > 0, "Ein Sticker ohne Höhe wäre nicht mehr anfassbar.");
    }
    // ==================== Bilder aus Dateien, und PDF-Seiten ====================
    //
    // Alle drei Rechnungen lagen bis Phase 4.5 privat in WhiteboardView.Import.cs des
    // WPF-Kopfs. Sie stehen jetzt in Core, weil ihr Ergebnis IN DIE DATEI wandert: dasselbe
    // PDF darf im Linux-Kopf nicht anders auf der Fläche liegen als unter Windows.

    private static List<(float, float)> Masse(params (float, float)[] m) => [.. m];

    /// <summary>Auf einem Blatt begrenzt die Seite (70 %), nicht der Bildschirm.</summary>
    [Fact]
    public void Auf_dem_Blatt_passt_ein_Bild_in_siebzig_Prozent()
    {
        var k = WbEinfuegen.FuerBilder(Masse((4000, 4000)), new SKPoint(400, 300), Blatt(800, 600), 9999, 9999);

        // 70 % von 600 (Höhe) ist die engere Grenze
        Assert.Equal(420f, k[0].Width, 3);
        Assert.Equal(420f, k[0].Height, 3);
    }

    /// <summary>
    /// Auf der unendlichen Fläche gibt es kein Blatt — dort begrenzt der Sichtbereich, sonst
    /// läge ein eingefügtes Bild sofort zu großen Teilen außerhalb.
    /// </summary>
    [Fact]
    public void Auf_der_endlosen_Flaeche_begrenzt_die_Sicht()
    {
        var k = WbEinfuegen.FuerBilder(Masse((4000, 4000)), new SKPoint(0, 0), Endlos(), 1000, 800);

        // 60 % von 800 (Höhe) ist die engere Grenze
        Assert.Equal(480f, k[0].Width, 3);
        Assert.Equal(480f, k[0].Height, 3);
    }

    /// <summary>Ein kleines Bild wird nicht aufgeblasen, nur weil Platz da ist.</summary>
    [Fact]
    public void Kleine_Bilder_bleiben_klein()
    {
        var k = WbEinfuegen.FuerBilder(Masse((40, 20)), new SKPoint(400, 300), Blatt(), 9999, 9999);

        Assert.Equal(40f, k[0].Width, 3);
        Assert.Equal(20f, k[0].Height, 3);
    }

    /// <summary>
    /// <b>Der Versatz ist der Punkt.</b> Ohne ihn lägen fünf gleichzeitig eingefügte Bilder
    /// exakt übereinander — der Nutzer sähe eines und müsste vier blind wegziehen, um zu
    /// merken, dass sie da sind.
    /// </summary>
    [Fact]
    public void Mehrere_Bilder_werden_versetzt()
    {
        var k = WbEinfuegen.FuerBilder(Masse((100, 100), (100, 100), (100, 100)),
                                       new SKPoint(0, 0), Endlos(), 1000, 1000);

        Assert.Equal(3, k.Count);
        Assert.Equal(24f, k[1].Left - k[0].Left, 3);
        Assert.Equal(24f, k[1].Top - k[0].Top, 3);
        Assert.Equal(48f, k[2].Left - k[0].Left, 3);
    }

    /// <summary>Auch der Stapel bleibt auf dem Blatt — der Versatz darf nicht hinausschieben.</summary>
    [Fact]
    public void Der_Versatz_schiebt_nicht_vom_Blatt()
    {
        var k = WbEinfuegen.FuerBilder(Masse((100, 100), (100, 100)),
                                       new SKPoint(795, 595), Blatt(800, 600), 9999, 9999);

        Assert.All(k, r =>
        {
            Assert.True(r.Right <= 800.001f, "Ein Bild ragt rechts über das Blatt hinaus.");
            Assert.True(r.Bottom <= 600.001f, "Ein Bild ragt unten über das Blatt hinaus.");
        });
    }

    /// <summary>
    /// Die Anzeigegröße hängt an der <b>A4-Höhe</b> und nicht an der Renderauflösung: dieselbe
    /// PDF-Seite käme sonst je nach Einstellung unterschiedlich groß auf die Fläche — und das
    /// stünde dann so in der Datei.
    /// </summary>
    [Theory]
    [InlineData(1000, 1414)]   // grob A4 hochkant
    [InlineData(2000, 2828)]   // dieselbe Seite, doppelt so fein gerastert
    public void Die_Seitengroesse_haengt_nicht_an_der_Aufloesung(float pb, float ph)
    {
        var (b, h) = WbEinfuegen.SeitenAnzeigegroesse(pb, ph);

        Assert.Equal(WhiteboardDoc.A4Height, h, 3);
        Assert.Equal(WhiteboardDoc.A4Height * pb / ph, b, 2);
    }

    /// <summary>Bei einer Querformat-Seite ist die BREITE die lange Kante.</summary>
    [Fact]
    public void Querformat_wird_erkannt()
    {
        var (b, h) = WbEinfuegen.SeitenAnzeigegroesse(1414, 1000);

        Assert.Equal(WhiteboardDoc.A4Height, b, 3);
        Assert.True(h < b, "Eine Querformat-Seite darf nicht höher als breit werden.");
    }

    /// <summary>Ein Bild ohne Maße gäbe eine Division durch null.</summary>
    [Fact]
    public void Eine_Seite_ohne_Masse_wirft_nicht()
    {
        var (b, h) = WbEinfuegen.SeitenAnzeigegroesse(0, 0);

        Assert.True(b > 0 && h > 0, "Auch eine Seite ohne Maße braucht eine Fläche.");
    }

    /// <summary>
    /// Zweispaltig heißt: 1 und 2 nebeneinander, 3 und 4 darunter. Ein zwanzigseitiges PDF in
    /// einer Reihe wäre zwanzig Blatt breit.
    /// </summary>
    [Fact]
    public void Seiten_liegen_zweispaltig()
    {
        var k = WbEinfuegen.SeitenRaster(Masse((200, 300), (200, 300), (200, 300), (200, 300)),
                                        new SKPoint(0, 0));

        Assert.Equal(4, k.Count);
        Assert.Equal(k[0].Top, k[1].Top, 3);                 // 1 und 2 auf einer Höhe
        Assert.True(k[1].Left > k[0].Left, "Seite 2 gehört rechts neben Seite 1.");
        Assert.True(k[2].Top > k[0].Top, "Seite 3 gehört unter Seite 1.");
        Assert.Equal(k[0].Left, k[2].Left, 3);               // 1 und 3 in derselben Spalte
        Assert.Equal(WbEinfuegen.Abstand, k[1].Left - k[0].Right, 3);
    }

    /// <summary>
    /// Die Zeilenhöhe richtet sich nach der <b>höheren</b> der beiden Seiten — sonst liefe eine
    /// Querformat-Seite in die Zeile darunter.
    /// </summary>
    [Fact]
    public void Die_hoehere_Seite_bestimmt_die_Zeilenhoehe()
    {
        var k = WbEinfuegen.SeitenRaster(Masse((200, 100), (200, 400), (200, 100)), new SKPoint(0, 0));

        Assert.True(k[2].Top >= k[1].Bottom, "Seite 3 überlappt die höhere Seite 2.");
    }

    /// <summary>Eine ungerade Seitenzahl lässt den rechten Platz der letzten Zeile leer.</summary>
    [Fact]
    public void Eine_ungerade_Seitenzahl_geht_auf()
    {
        var k = WbEinfuegen.SeitenRaster(Masse((200, 300), (200, 300), (200, 300)), new SKPoint(0, 0));

        Assert.Equal(3, k.Count);
        Assert.Equal(k[0].Left, k[2].Left, 3);
    }

    /// <summary>Keine Seiten, kein Raster — und kein Griff ins Leere beim Suchen der breitesten.</summary>
    [Fact]
    public void Ohne_Seiten_kommt_ein_leeres_Raster()
    {
        Assert.Empty(WbEinfuegen.SeitenRaster(Masse(), new SKPoint(0, 0)));
    }
    // ==================== SVG rastern ====================
    //
    // WbImagePrep.ForSvg lag bis Phase 4.5 privat im WPF-Kopf (RasterizeSvg) -- obwohl
    // Svg.Skia seit jeher ein CORE-Paket ist. Beim Portieren des Imports waere sie ein
    // zweites Mal entstanden.

    /// <summary>Ein Quadrat von 100x50 Punkten, das nicht bei (0,0) anfaengt.</summary>
    private const string SvgVersetzt =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="20 10 100 50"><rect x="20" y="10" width="100" height="50" fill="#E11D48"/></svg>""";

    private static byte[] Bytes(string s) => System.Text.Encoding.UTF8.GetBytes(s);

    /// <summary>
    /// <b>Die gemeldeten Maße sind die der Zeichnung, nicht die des Rasters.</b> Ein Vektorbild
    /// wird auf der Fläche gezoomt, also wird feiner gerastert als angezeigt — käme die
    /// Rastergröße zurück, läge die SVG doppelt so groß auf dem Blatt wie gewollt.
    /// </summary>
    [Fact]
    public void Eine_SVG_meldet_ihre_Zeichengroesse()
    {
        var p = WbImagePrep.ForSvg(Bytes(SvgVersetzt));

        Assert.NotNull(p);
        Assert.Equal(100f, p!.Value.Width, 2);
        Assert.Equal(50f, p.Value.Height, 2);
    }

    /// <summary>
    /// Das Ergebnis ist ein <b>PNG</b> — eine Vektordatei kann kein Kopf zeichnen, und
    /// Durchsichtigkeit muss überleben (ein Aufkleber mit weißem Kasten drumherum wäre keiner).
    /// </summary>
    [Fact]
    public void Das_Ergebnis_ist_ein_lesbares_PNG_mit_Alpha()
    {
        var p = WbImagePrep.ForSvg(Bytes(SvgVersetzt));
        Assert.NotNull(p);

        using var bmp = WbImages.Decode(p!.Value.Data);
        Assert.NotNull(bmp);

        // Feiner gerastert als die Zeichengröße (Faktor 2), damit Zoomen scharf bleibt.
        Assert.Equal(200, bmp!.Width);
        Assert.Equal(100, bmp.Height);

        // Und die Fläche ist wirklich gefüllt: ohne die Verschiebung um (-20,-10) stünde
        // links oben ein leerer Rand und rechts würde abgeschnitten.
        Assert.Equal((byte)0xE1, bmp.GetPixel(bmp.Width / 2, bmp.Height / 2).Red);
        Assert.Equal((byte)255, bmp.GetPixel(bmp.Width / 2, bmp.Height / 2).Alpha);
    }

    /// <summary>
    /// Eine kaputte oder ausdehnungslose Datei ergibt <c>null</c> und keine Ausnahme — der
    /// Aufrufer meldet sie dann wie jede andere unlesbare Datei, mit Namen.
    /// </summary>
    [Theory]
    [InlineData("das ist gar kein svg")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 0 0"></svg>""")]
    public void Unbrauchbare_SVG_ergibt_null(string inhalt) =>
        Assert.Null(WbImagePrep.ForSvg(Bytes(inhalt)));

    /// <summary>
    /// <b>Eine riesige SVG wird nicht ins Unermessliche gerastert.</b> Der Faktor 2 gilt nur,
    /// solange <see cref="WbImagePrep.MaxImportDim"/> nicht überschritten wird — sonst ergäbe
    /// eine Zeichnung mit 4000 Punkten Kantenlänge ein 8000er-Bild in der Datenbank.
    /// </summary>
    [Fact]
    public void Eine_riesige_SVG_bleibt_unter_der_Grenze()
    {
        string riesig =
            """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 4000 4000"><rect width="4000" height="4000" fill="#123456"/></svg>""";

        var p = WbImagePrep.ForSvg(Bytes(riesig));
        Assert.NotNull(p);

        using var bmp = WbImages.Decode(p!.Value.Data);
        Assert.NotNull(bmp);
        Assert.True(bmp!.Width <= WbImagePrep.MaxImportDim,
            $"Raster {bmp.Width} px überschreitet die Importgrenze.");

        // Die gemeldete Größe bleibt trotzdem die der Zeichnung.
        Assert.Equal(4000f, p.Value.Width, 2);
    }
}
