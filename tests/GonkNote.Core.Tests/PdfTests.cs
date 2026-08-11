using System.IO;
using System.Text;
using Docnet.Core;
using Docnet.Core.Models;
using GonkNote.Core.Services;
using GonkNote.Core.Text;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdPdf"/> — das PDF eines Textdokuments, gegen das Modell statt gegen den
/// WPF-Paginator (HANDOFF §4.27).
///
/// <para>
/// <b>Geprüft wird über den Rückweg, nicht über Pixel.</b> Das erzeugte PDF wird mit PDFium
/// wieder eingelesen und ausgefragt — genau der Harness, den §7 für den Whiteboard-Export
/// schon vorgesehen hat. Ein Pixelhash wäre hier falsch: die Seiten enthalten Schrift, und
/// „Segoe UI" gibt es unter Linux nicht (§4.6). Was hier steht, gilt für jede Schrift.
/// </para>
/// <para>
/// <b>Der wichtigste Wächter ist <see cref="Der_Text_im_PDF_ist_Text_und_kein_Bild"/>.</b> Er
/// bewacht genau den Unterschied, um dessentwillen dieser Weg gebaut wurde: Der alte Weg legte
/// jede Seite als Rasterbild ins PDF — das sah gut aus und ließ sich nicht durchsuchen,
/// markieren oder vorlesen. Fällt der Test um, ist jemand versehentlich zum Bild zurück.
/// </para>
/// </summary>
public sealed class PdfTests
{
    // ==================== Vorlagen ====================

    /// <summary>A5 hochkant mit 2 cm Rand — klein genug, dass wenige Absätze umbrechen.</summary>
    private static TdPageSetup Blatt() => TdPageSetup.A5;

    private static TdDocument Dok(params TdBlock[] bloecke) =>
        Dok(Blatt(), bloecke);

    private static TdDocument Dok(TdPageSetup seite, params TdBlock[] bloecke) => new()
    {
        Sections = { new TdSection(bloecke) { Page = seite } },
    };

    /// <summary>So viele Absätze, dass es über eine Seite hinausgeht — mit echter Schrift.</summary>
    private static TdBlock[] VieleAbsaetze(int anzahl) =>
        Enumerable.Range(1, anzahl)
            .Select(i => (TdBlock)new TdParagraph($"Absatz Nummer {i} mit genug Text, dass er umbricht."))
            .ToArray();

    /// <summary>Der Text aller Seiten, wie PDFium ihn aus der Datei zurückliest.</summary>
    private static List<string> SeitenText(string pfad)
    {
        // Die Maße sind hier gleichgültig — gelesen wird Text, nicht gerendert.
        using var leser = DocLib.Instance.GetDocReader(pfad, new PageDimensions(1080, 1080));

        var seiten = new List<string>();
        for (int i = 0; i < leser.GetPageCount(); i++)
        {
            using var seite = leser.GetPageReader(i);
            seiten.Add(seite.GetText());
        }
        return seiten;
    }

    /// <summary>Eine einfarbige Fläche als PNG — erzeugt statt eingecheckt (§6).</summary>
    private static byte[] Png(int breite, int hoehe, SKColor farbe)
    {
        using var bmp = new SKBitmap(breite, hoehe);
        using (var leinwand = new SKCanvas(bmp))
            leinwand.Clear(farbe);
        using var abbild = SKImage.FromBitmap(bmp);
        using var daten = abbild.Encode(SKEncodedImageFormat.Png, 100);
        return daten.ToArray();
    }

    /// <summary>Eine Bildquelle aus dem Gedächtnis — der Blob-Speicher ist hier zu viel.</summary>
    private sealed class Bilder : ITdImages
    {
        private readonly Dictionary<Guid, byte[]> _daten = new();

        public Guid Legen(byte[] bytes)
        {
            var id = Guid.NewGuid();
            _daten[id] = bytes;
            return id;
        }

        public byte[]? Lesen(Guid id) => _daten.GetValueOrDefault(id);

        public Guid Ablegen(byte[] daten, string endung) => Legen(daten);
    }

    // ==================== Der Kern der Sache ====================

    /// <summary>
    /// <b>Der Text im PDF ist Text.</b> PDFium liest ihn aus der Datei zurück — das kann es nur,
    /// wenn Skia Textbefehle und eingebettete Schriften geschrieben hat und kein Bild.
    /// <para>
    /// Beim alten Weg über den WPF-Paginator wäre dieser Test rot: dort stand auf jeder Seite
    /// ein <c>DrawImage</c>, und aus einem Bild liest niemand Text heraus. Suche, Markieren,
    /// Kopieren und Vorlesen hingen alle an dieser einen Eigenschaft.
    /// </para>
    /// </summary>
    [Fact]
    public void Der_Text_im_PDF_ist_Text_und_kein_Bild()
    {
        using var werkbank = new TempWorkspace("pdf-text");
        string pfad = werkbank.File("text.pdf");

        TdPdf.Schreiben(Dok(new TdParagraph("Rhabarberkuchen")), pfad);

        string text = string.Concat(SeitenText(pfad));
        Assert.Contains("Rhabarberkuchen", text);
    }

    /// <summary>
    /// Auch fett, kursiv und farbig bleiben Text. <b>Nicht selbstverständlich:</b> jedes
    /// Zeichenformat ist eine eigene Schrift, und eine, die Skia nicht einbetten kann, fiele
    /// entweder aus oder käme als Kurvenzug heraus — im PDF sichtbar, aber nicht mehr lesbar.
    /// </summary>
    [Fact]
    public void Zeichenformate_bleiben_lesbarer_Text()
    {
        using var werkbank = new TempWorkspace("pdf-format");
        string pfad = werkbank.File("format.pdf");

        TdPdf.Schreiben(Dok(new TdParagraph(new TdInline[]
        {
            new TdRun("Fettgedrucktes", new TdCharFormat { Bold = true }),
            new TdRun(" "),
            new TdRun("Schraeggestelltes", new TdCharFormat { Italic = true }),
            new TdRun(" "),
            new TdRun("Buntes", new TdCharFormat { Color = "#B03060" }),
        })), pfad);

        string text = string.Concat(SeitenText(pfad));
        Assert.Contains("Fettgedrucktes", text);
        Assert.Contains("Schraeggestelltes", text);
        Assert.Contains("Buntes", text);
    }

    /// <summary>
    /// Ein Verweis wird im PDF anklickbar. <b>Das ist kein Pixel, sondern ein Vermerk</b> —
    /// er entsteht in <c>TdPdf</c> und nicht im Zeichner, weil eine Bildschirm-Leinwand ihn gar
    /// nicht kennt. Geprüft wird am Rohbestand der Datei: das Ziel muss darin vorkommen.
    /// </summary>
    [Fact]
    public void Ein_Verweis_wird_im_PDF_vermerkt()
    {
        using var werkbank = new TempWorkspace("pdf-verweis");
        string pfad = werkbank.File("verweis.pdf");

        var verweis = new TdHyperlink { Target = "https://example.invalid/gonk" };
        verweis.Inlines.Add(new TdRun("Hierhin"));

        TdPdf.Schreiben(Dok(new TdParagraph(new TdInline[] { verweis })), pfad);

        string roh = Encoding.Latin1.GetString(File.ReadAllBytes(pfad));
        Assert.Contains("https://example.invalid/gonk", roh);
    }

    // ==================== Seiten ====================

    /// <summary>
    /// Jede Seite des Umbruchs wird ein Blatt im PDF — und mehr als eine, wenn der Text nicht
    /// auf eine passt. <b>„Alles auf einer Seite" ist das Fehlerbild eines Layout-Umbaus</b>,
    /// und es fällt sonst niemandem auf, weil die erste Seite richtig aussieht.
    /// </summary>
    [Fact]
    public void Jede_Seite_des_Umbruchs_wird_ein_Blatt()
    {
        using var werkbank = new TempWorkspace("pdf-seiten");
        string pfad = werkbank.File("seiten.pdf");

        var doc = Dok(VieleAbsaetze(60));

        using var messung = new TdSkiaMeasure();
        int erwartet = TdLayout.Umbrechen(doc, messung).PageCount;
        Assert.True(erwartet >= 2, $"Die Vorlage bricht nicht um ({erwartet} Seite).");

        TdPdf.Schreiben(doc, pfad);

        Assert.Equal(erwartet, PdfImporter.PageCount(pfad));
    }

    /// <summary>
    /// Keine Seite bleibt leer. Der zweite Teil desselben Fehlerbilds: ein Umbruch, der eine
    /// Seite zu viel anlegt, fällt am Seitenzahlvergleich nicht auf — hier schon.
    /// </summary>
    [Fact]
    public void Keine_Seite_bleibt_leer()
    {
        using var werkbank = new TempWorkspace("pdf-leer");
        string pfad = werkbank.File("leer.pdf");

        TdPdf.Schreiben(Dok(VieleAbsaetze(60)), pfad);

        var seiten = SeitenText(pfad);
        Assert.All(seiten, text => Assert.False(
            string.IsNullOrWhiteSpace(text), $"Seite {seiten.IndexOf(text) + 1} ist leer."));
    }

    /// <summary>
    /// Das Seitenmaß kommt in Punkt heraus, aus Zentimetern gerechnet. <b>Die Umrechnung ist
    /// die Stelle, an der ein PDF still falsch wird:</b> ein A5-Blatt, das als A4 im Papierkorb
    /// des Druckers landet, sieht am Bildschirm völlig richtig aus.
    /// </summary>
    [Theory]
    [InlineData(14.8, 21.0)]     // A5 hoch
    [InlineData(29.7, 21.0)]     // A4 quer
    public void Das_Seitenmass_steht_in_Punkt(double breiteCm, double hoeheCm)
    {
        using var werkbank = new TempWorkspace("pdf-mass");
        string pfad = werkbank.File("mass.pdf");

        var seite = new TdPageSetup { WidthCm = breiteCm, HeightCm = hoeheCm };
        TdPdf.Schreiben(Dok(seite, new TdParagraph("Papier")), pfad);

        // **Maßstab 1 und keine Zielgröße.** Docnet gibt die Maße der *gerenderten* Seite
        // zurück, nicht die des Papiers; mit einer Zielgröße stünde hier deren Zahl. Bei
        // Maßstab 1 rendert PDFium im Benutzerkoordinatensystem, und das ist Punkt.
        using var leser = DocLib.Instance.GetDocReader(pfad, new PageDimensions(1.0));
        using var blatt = leser.GetPageReader(0);

        Assert.Equal(breiteCm * TdPdf.PunktProCm, blatt.GetPageWidth(), 1.0);
        Assert.Equal(hoeheCm * TdPdf.PunktProCm, blatt.GetPageHeight(), 1.0);
    }

    /// <summary>
    /// Kopf- und Fußzeile stehen im PDF, und die Unterdrückung auf der ersten Seite gilt.
    /// <para>
    /// Der alte Weg prüfte das am Farbanteil im oberen Rand — mehr ging bei einem Rasterbild
    /// nicht. Jetzt lässt sich fragen, was dort <b>steht</b>, und das ist die schärfere Frage:
    /// eine Kopfzeile mit dem falschen Inhalt hätte denselben Farbanteil gehabt.
    /// </para>
    /// </summary>
    [Fact]
    public void Kopfzeile_fehlt_auf_der_ersten_Seite_und_steht_auf_der_zweiten()
    {
        using var werkbank = new TempWorkspace("pdf-kopf");
        string pfad = werkbank.File("kopf.pdf");

        var seite = TdPageSetup.A5;
        seite.HeaderText = "Kopfzeilenwort";
        seite.FooterText = "Fusszeilenwort";
        seite.SuppressOnFirstPage = true;

        TdPdf.Schreiben(Dok(seite, VieleAbsaetze(60)), pfad);

        var seiten = SeitenText(pfad);
        Assert.True(seiten.Count >= 2);

        Assert.DoesNotContain("Kopfzeilenwort", seiten[0]);
        Assert.DoesNotContain("Fusszeilenwort", seiten[0]);
        Assert.Contains("Kopfzeilenwort", seiten[1]);
        Assert.Contains("Fusszeilenwort", seiten[1]);
    }

    /// <summary>
    /// <c>{SEITE}</c> und <c>{SEITEN}</c> stehen mit ihren gerechneten Werten im PDF.
    /// <b>Die Gesamtzahl ist der heikle Teil:</b> sie steht erst fest, wenn alles gesetzt ist,
    /// und ein Exporter, der sie nicht durchreicht, schreibt einen leeren Platz — sichtbar
    /// erst in der fertigen Datei.
    /// </summary>
    [Fact]
    public void Seitenzahl_und_Seitenanzahl_stehen_im_PDF()
    {
        using var werkbank = new TempWorkspace("pdf-felder");
        string pfad = werkbank.File("felder.pdf");

        var seite = TdPageSetup.A5;
        seite.FooterText = "Blatt {SEITE} von {SEITEN}";

        TdPdf.Schreiben(Dok(seite, VieleAbsaetze(60)), pfad);

        var seiten = SeitenText(pfad);
        Assert.True(seiten.Count >= 2);

        for (int i = 0; i < seiten.Count; i++)
            Assert.Contains($"Blatt {i + 1} von {seiten.Count}", seiten[i]);
    }

    /// <summary>
    /// Der Titel geht in die Felder, auch wenn der Aufrufer keinen eigenen Feldkontext mitgibt.
    /// Sonst stünde in der Kopfzeile ein leerer Platz, obwohl der Titel im selben Aufruf steht.
    /// </summary>
    [Fact]
    public void Der_Titel_kommt_in_der_Kopfzeile_an()
    {
        using var werkbank = new TempWorkspace("pdf-titel");
        string pfad = werkbank.File("titel.pdf");

        var seite = TdPageSetup.A5;
        seite.HeaderText = "{TITEL}";

        TdPdf.Schreiben(Dok(seite, new TdParagraph("Inhalt")), pfad, titel: "Quittenmus");

        Assert.Contains("Quittenmus", string.Concat(SeitenText(pfad)));
    }

    // ==================== Bilder ====================

    /// <summary>
    /// <b>Fehlt der Blob, fehlt das Bild und nicht die Seite</b> (Dauerregel 4). Ein Dokument
    /// mit einem Bild, dessen Daten weg sind, muss trotzdem ein vollständiges PDF ergeben —
    /// mit dem Alternativtext im Kasten, damit man sieht, was fehlt.
    /// </summary>
    [Fact]
    public void Ein_fehlender_Blob_kostet_das_Bild_und_nicht_die_Seite()
    {
        using var werkbank = new TempWorkspace("pdf-blob");
        string pfad = werkbank.File("blob.pdf");

        var bild = new TdImage(Guid.NewGuid(), "png", 4, 3) { AltText = "Ersatzwort" };
        var doc = Dok(new TdParagraph(new TdInline[] { bild }), new TdParagraph("Danach"));

        TdPdf.Schreiben(doc, pfad, new Bilder());   // kennt diese Kennung nicht

        Assert.Equal(1, PdfImporter.PageCount(pfad));
        string text = string.Concat(SeitenText(pfad));
        Assert.Contains("Ersatzwort", text);
        Assert.Contains("Danach", text);
    }

    /// <summary>
    /// Ohne Bildquelle vermisst niemand etwas, mit Bildquelle wird gezählt. Das ist die
    /// Auskunft, die der Kopf dem Nutzer nach dem Export gibt — sie hing bis §4.27 am
    /// WPF-Rasterweg und gilt jetzt für jeden Kopf.
    /// </summary>
    [Fact]
    public void Fehlende_Bilder_werden_gezaehlt()
    {
        var bilder = new Bilder();
        var vorhanden = new TdImage(bilder.Legen(Png(8, 8, SKColors.SeaGreen)), "png", 2, 2);
        var verloren = new TdImage(Guid.NewGuid(), "png", 2, 2);

        var doc = Dok(new TdParagraph(new TdInline[] { vorhanden, verloren }));

        Assert.Equal(1, DocumentHealth.MissingImages(doc, bilder));

        // Ohne Bildquelle fehlt nicht ein Bild, sondern es gibt gar keine — das ist die
        // Entscheidung des Aufrufers und keine Beschädigung des Dokuments.
        Assert.Equal(0, DocumentHealth.MissingImages(doc, null));
    }

    /// <summary>Das Wasserzeichen ist ein Bild wie jedes andere und zählt mit, wenn es fehlt.</summary>
    [Fact]
    public void Ein_fehlendes_Wasserzeichen_zaehlt_mit()
    {
        var seite = TdPageSetup.A5;
        seite.Watermark = new TdImage(Guid.NewGuid(), "png", 10, 10);

        Assert.Equal(1, DocumentHealth.MissingImages(Dok(seite, new TdParagraph("x")), new Bilder()));
    }

    // ==================== PNG ====================

    /// <summary>
    /// Der PNG-Weg: eine Datei je Seite, durchnummeriert. Bei genau einer Seite bleibt es beim
    /// angegebenen Pfad — alles andere wäre für den Nutzer eine Datei, die er nicht wiederfindet.
    /// </summary>
    [Fact]
    public void Png_schreibt_eine_Datei_je_Seite()
    {
        using var werkbank = new TempWorkspace("png-seiten");

        var dateien = TdPdf.Png(Dok(VieleAbsaetze(60)), werkbank.File("blatt.png"));

        Assert.True(dateien.Count >= 2);
        Assert.Equal(dateien, dateien.Distinct());
        Assert.All(dateien, datei => Assert.True(File.Exists(datei), datei));
        Assert.EndsWith("blatt-1.png", dateien[0]);
    }

    /// <summary>Eine einzelne Seite behält den Pfad, den der Nutzer im Dialog gewählt hat.</summary>
    [Fact]
    public void Eine_einzelne_Seite_behaelt_ihren_Pfad()
    {
        using var werkbank = new TempWorkspace("png-eine");
        string pfad = werkbank.File("blatt.png");

        var dateien = TdPdf.Png(Dok(new TdParagraph("Kurz")), pfad);

        Assert.Equal(new[] { pfad }, dateien);
    }

    /// <summary>
    /// Die Auflösung ist das Vielfache der Bildschirmauflösung — und das Papier ist weiß.
    /// <b>Der zweite Teil ist kein Beiwerk:</b> ein PNG hat einen Alphakanal, und eine Seite,
    /// die den Rand durchsichtig lässt, sieht in jedem Bildbetrachter mit dunklem Grund
    /// schwarz umrandet aus.
    /// </summary>
    [Fact]
    public void Png_hat_die_verlangte_Aufloesung_und_weisses_Papier()
    {
        using var werkbank = new TempWorkspace("png-mass");
        string pfad = werkbank.File("blatt.png");

        var seite = TdPageSetup.A5;
        TdPdf.Png(Dok(seite, new TdParagraph("Kurz")), pfad, vielfaches: 2f);

        using var bmp = Rendering.WbImages.Decode(File.ReadAllBytes(pfad));
        Assert.NotNull(bmp);

        double erwartetBreit = seite.WidthCm * Rendering.TdRenderer.PixelProCm * 2;
        Assert.Equal(erwartetBreit, bmp.Width, 1.0);

        // Die linke obere Ecke liegt im Rand: dort ist nur Papier.
        var ecke = bmp.GetPixel(2, 2);
        Assert.Equal(byte.MaxValue, ecke.Alpha);
        Assert.True(ecke.Red > 250 && ecke.Green > 250 && ecke.Blue > 250,
            $"Die Ecke ist nicht weiß, sondern {ecke}.");
    }

    /// <summary>
    /// Die Seitenbilder für den Einfüge-Weg: JPEG, im Speicher, in der Reihenfolge des
    /// Umbruchs. Sie kommen in derselben Form heraus wie die eines PDF-Imports — nur so haben
    /// beide Einfüge-Wege danach eine einzige Fortsetzung.
    /// </summary>
    [Fact]
    public void Seitenbilder_kommen_in_der_Form_des_PDF_Imports()
    {
        var bilder = TdPdf.Seitenbilder(Dok(VieleAbsaetze(60)));

        Assert.True(bilder.Count >= 2);
        Assert.All(bilder, bild =>
        {
            Assert.NotEmpty(bild.Data);
            using var bmp = Rendering.WbImages.Decode(bild.Data);
            Assert.NotNull(bmp);
            Assert.Equal(bild.Width, bmp.Width);
            Assert.Equal(bild.Height, bmp.Height);
        });
    }

    // ==================== Umbruch und Ausgabe stimmen überein ====================

    /// <summary>
    /// <b>Der Maßstab ändert den Umbruch nicht.</b> Ein PDF rechnet in Punkt, der Bildschirm in
    /// Pixeln — wenn diese Wahl eine Umbruchstelle verschöbe, stünde eine gedruckte Seite
    /// woanders um als die angezeigte. Genau dafür rechnet der Umbruch in Zentimetern (§4.16);
    /// dieser Test hält die Begründung fest.
    /// </summary>
    [Fact]
    public void Der_Massstab_verschiebt_keine_Umbruchstelle()
    {
        using var werkbank = new TempWorkspace("pdf-massstab");
        string pfad = werkbank.File("massstab.pdf");

        var doc = Dok(VieleAbsaetze(60));

        using var messung = new TdSkiaMeasure();
        var umbruch = TdLayout.Umbrechen(doc, messung);

        TdPdf.Schreiben(doc, pfad);
        var ausDerDatei = SeitenText(pfad);

        Assert.Equal(umbruch.PageCount, ausDerDatei.Count);

        // Das erste Wort jeder Seite muss dasselbe sein. Nicht der ganze Text: PDFium gibt
        // Leerraum und Zeilenenden anders zurück, als der Umbruch sie ablegt.
        for (int i = 0; i < umbruch.Pages.Count; i++)
        {
            string erwartet = ErstesWort(umbruch.Pages[i].Lines.FirstOrDefault()?.PlainText() ?? "");
            if (erwartet.Length == 0) continue;
            Assert.Contains(erwartet, ausDerDatei[i]);
        }

        static string ErstesWort(string zeile) =>
            zeile.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
    }
}
