using System.IO;
using System.Text;
using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="WbExport"/> — der Tafel-Export beider Köpfe (Phase 5, Schritt ①c).
///
/// <para>
/// <b>Warum diese Wächter hier stehen und nicht drüben:</b> Zwei von ihnen gab es schon, als
/// <c>ExportFixtureTests</c> im <b>WPF</b>-Testprojekt — und genau das war das Problem. Der
/// Linux-Kopf fährt nur <c>GonkNote.Core.Tests</c>; ein Weg, der ausschließlich im
/// Windows-Testprojekt bewacht wird, ist auf dem anderen System <b>ungeprüft</b>. Dass er dort
/// zusätzlich gar nicht erst <i>angeboten</i> wurde, ist über Runden hinweg niemandem
/// aufgefallen.
/// </para>
/// <para>
/// <b>Bewacht wird die Weiche und die Zusage, nicht das Aussehen.</b> Wie eine Seite aussieht,
/// steht in <see cref="RendererSnapshotTests"/>; hier steht, dass die Endung entscheidet, dass
/// eine Seite eine Seite bleibt, dass PNG je Seite eine Datei schreibt — und dass beide Köpfe
/// <b>dieselbe</b> Formatliste sehen, weil es nur noch eine gibt.
/// </para>
/// <para>
/// <b>Und was hier bewusst nicht steht:</b> ein Byte-Vergleich gegen eine eingecheckte Datei.
/// Der Export zeichnet Schrift, und die Schrift ist auf zwei Systemen nicht dieselbe (§4.26,
/// derselbe Grund wie in <see cref="Farbfleck"/>). Geprüft wird über den <b>Rückweg</b> —
/// PDFium liest das erzeugte PDF wieder ein.
/// </para>
/// </summary>
public sealed class TafelexportTests
{
    // ==================== Vorlagen ====================

    private static WbPage Blatt(float breite = WhiteboardDoc.A4Width,
                                float hoehe = WhiteboardDoc.A4Height) =>
        new()
        {
            Width = breite,
            Height = hoehe,
            Background = PageBackground.Lines,
            Elements =
            {
                new StrokeElement
                {
                    Color = "#FF1B2B4B",
                    Width = 6f,
                    Points = { new WbPoint { X = 120, Y = 200 }, new WbPoint { X = 620, Y = 260 } },
                },
            },
        };

    private static WhiteboardDoc Buch(int seiten = 1)
    {
        var doc = new WhiteboardDoc { Id = Guid.NewGuid() };
        for (int i = 0; i < seiten; i++) doc.Pages.Add(Blatt());
        return doc;
    }

    /// <summary>
    /// Die ersten vier Bytes als Zeichen. <b>Latin-1 und nicht ASCII:</b> die PNG-Kennung
    /// beginnt mit <c>0x89</c>, und ASCII macht daraus ein Fragezeichen — der Wächter
    /// verglichen hätte dann zwei Fragezeichen miteinander.
    /// </summary>
    private static string Kennung(string pfad)
    {
        using var s = File.OpenRead(pfad);
        Span<byte> kopf = stackalloc byte[4];
        s.ReadExactly(kopf);
        return Encoding.Latin1.GetString(kopf);
    }

    // ==================== Die Weiche ====================

    /// <summary>
    /// <b>Die Endung entscheidet, und sie entscheidet an einer Stelle.</b> Vor dem Umzug stand
    /// diese Verzweigung im <c>WpfDocumentIo</c>; hätte der Linux-Kopf sie später ein zweites
    /// Mal bekommen, wären es zwei Fassungen gewesen, die dieselben Tests bestehen — die Falle
    /// aus §4.13.
    /// </summary>
    [Theory]
    [InlineData(".pdf")]
    [InlineData(".PDF")]     // die Endung wird kleingeschrieben verglichen
    [InlineData(".sonstwas")] // alles Unbekannte wird ein PDF, keine Ausnahme
    public void Alles_ausser_png_wird_ein_PDF(string endung)
    {
        using var arbeit = new TempWorkspace("tafel-weiche");
        string pfad = arbeit.File("buch" + endung);

        var ergebnis = WbExport.Exportieren(Buch(), "Probe", pfad);

        Assert.Equal([pfad], ergebnis.Written);
        Assert.Equal("%PDF", Kennung(pfad));
    }

    [Fact]
    public void Die_Endung_png_ergibt_ein_PNG_und_kein_PDF()
    {
        using var arbeit = new TempWorkspace("tafel-weiche-png");
        string pfad = arbeit.File("buch.png");

        var ergebnis = WbExport.Exportieren(Buch(), "Probe", pfad);

        Assert.Equal([pfad], ergebnis.Written);
        Assert.Equal("PNG", Kennung(pfad));
    }

    // ==================== Was zurückgemeldet wird ====================

    /// <summary>
    /// <b>Gemeldet wird, was entstanden ist — nicht, wonach gefragt wurde.</b> Bei mehreren
    /// Seiten entstehen mehrere PNGs mit durchnummerierten Namen; der Kopf zeigt daraufhin
    /// „N Seiten geschrieben" statt eines Dateinamens, den es so gar nicht gibt.
    /// </summary>
    [Fact]
    public void PNG_schreibt_eine_Datei_je_Seite_und_meldet_sie_alle()
    {
        using var arbeit = new TempWorkspace("tafel-png-mehrseitig");
        string pfad = arbeit.File("buch.png");

        var ergebnis = WbExport.Exportieren(Buch(seiten: 3), "Probe", pfad);

        Assert.Equal(3, ergebnis.Written.Count);
        Assert.Equal(ergebnis.Written, ergebnis.Written.Distinct());
        Assert.All(ergebnis.Written, d => Assert.True(File.Exists(d), d));

        // Bei mehreren Seiten trägt jede ihre Nummer — und die angefragte Datei selbst
        // entsteht dann gerade NICHT.
        Assert.Equal(arbeit.File("buch-1.png"), ergebnis.Written[0]);
        Assert.Equal(arbeit.File("buch-3.png"), ergebnis.Written[2]);
        Assert.False(File.Exists(pfad));
    }

    /// <summary>Eine einzelne Seite behält den Namen, den der Nutzer gewählt hat.</summary>
    [Fact]
    public void Eine_einzelne_Seite_bekommt_keine_Nummer()
    {
        using var arbeit = new TempWorkspace("tafel-png-einseitig");
        string pfad = arbeit.File("buch.png");

        var ergebnis = WbExport.Exportieren(Buch(), "Probe", pfad);

        Assert.Equal([pfad], ergebnis.Written);
        Assert.False(File.Exists(arbeit.File("buch-1.png")));
    }

    [Fact]
    public void PNG_rastert_mit_doppelter_Aufloesung()
    {
        using var arbeit = new TempWorkspace("tafel-png-schaerfe");
        string pfad = arbeit.File("buch.png");

        WbExport.Exportieren(Buch(), "Probe", pfad);

        using var bild = WbImages.Decode(File.ReadAllBytes(pfad));
        Assert.NotNull(bild);
        Assert.Equal((int)Math.Round(WhiteboardDoc.A4Width * 2), bild!.Width);
        Assert.Equal((int)Math.Round(WhiteboardDoc.A4Height * 2), bild.Height);
    }

    // ==================== Der Rückweg ====================

    /// <summary>
    /// <b>Der Beweis, dass das PDF eines ist:</b> PDFium liest es wieder ein. Ein leeres oder
    /// halb geschriebenes Dokument fiele hier auf, ein vertauschtes Hoch-/Querformat auch.
    /// </summary>
    [Fact]
    public void Das_PDF_ueberlebt_den_Rueckimport_mit_einem_Blatt_je_Seite()
    {
        using var arbeit = new TempWorkspace("tafel-pdf-rueckweg");
        string pfad = arbeit.File("buch.pdf");
        var doc = Buch(seiten: 2);

        WbExport.Exportieren(doc, "Probe", pfad);

        Assert.Equal(doc.Pages.Count, PdfImporter.PageCount(pfad));

        var seiten = PdfImporter.RenderPages(pfad, targetLongSide: 500).ToList();
        Assert.Equal(doc.Pages.Count, seiten.Count);

        for (int i = 0; i < seiten.Count; i++)
        {
            double soll = doc.Pages[i].Width / doc.Pages[i].Height;
            double ist = (double)seiten[i].Width / seiten[i].Height;
            Assert.Equal(soll, ist, 0.02);
        }
    }

    /// <summary>
    /// Querformat bleibt Querformat. <b>Das ist kein Selbstläufer:</b> die Seitengeometrie
    /// wird aus dem Modell gerechnet und nicht aus einer Konstante, und ein vertauschtes
    /// Paar sähe im Bau grün aus.
    /// </summary>
    [Fact]
    public void Querformat_bleibt_Querformat()
    {
        using var arbeit = new TempWorkspace("tafel-pdf-quer");
        string pfad = arbeit.File("quer.pdf");
        var doc = new WhiteboardDoc { Id = Guid.NewGuid() };
        doc.Pages.Add(Blatt(WhiteboardDoc.A4Height, WhiteboardDoc.A4Width));

        WbExport.Exportieren(doc, "Probe", pfad);

        var seite = Assert.Single(PdfImporter.RenderPages(pfad, targetLongSide: 500));
        Assert.True(seite.Width > seite.Height, "Die Querformat-Seite steht hochkant im PDF.");
    }

    /// <summary>
    /// <b>Die unendliche Fläche hat kein Blatt</b> (<c>Width</c>/<c>Height</c> sind 0). Sie
    /// bekommt den Zuschnitt ihres Inhalts samt Rand — ohne diese Rechnung entstünde eine
    /// Seite der Größe null, und <c>SKSurface.Create</c> gäbe dafür null zurück.
    /// </summary>
    [Fact]
    public void Die_unendliche_Flaeche_wird_auf_ihren_Inhalt_zugeschnitten()
    {
        using var arbeit = new TempWorkspace("tafel-unendlich");
        string pfad = arbeit.File("endlos.png");

        var doc = new WhiteboardDoc { Id = Guid.NewGuid() };
        doc.Pages.Add(new WbPage
        {
            Width = 0,
            Height = 0,
            Elements =
            {
                new StrokeElement
                {
                    Width = 4f,
                    Points = { new WbPoint { X = 100, Y = 100 }, new WbPoint { X = 300, Y = 200 } },
                },
            },
        });

        var ergebnis = WbExport.Exportieren(doc, "Probe", pfad);

        using var bild = WbImages.Decode(File.ReadAllBytes(Assert.Single(ergebnis.Written)));
        Assert.NotNull(bild);
        // Zug 200×100 plus 48 Rand ringsum, mal 2 für die Rasterung — und keinesfalls A4.
        Assert.InRange(bild!.Width, 560, 640);
        Assert.NotEqual((int)Math.Round(WhiteboardDoc.A4Width * 2), bild.Width);
    }

    // ==================== Die eine Formatliste ====================

    /// <summary>
    /// <b>Der Wächter, den es vorher nicht gab — und der den Fund gemacht hätte.</b> Die
    /// Formatliste stand zweimal da: gefüllt im WPF-Kopf, <i>leer</i> im Linux-Kopf. Beide
    /// Fassungen haben jeden Test bestanden, weil keiner sie verglichen hat. Jetzt gibt es
    /// eine, und dieser Wächter hält fest, was sie zusagt.
    /// </summary>
    [Fact]
    public void Es_gibt_genau_die_beiden_Tafelformate()
    {
        var endungen = WbExport.Formate.Select(f => f.PrimaryExtension).ToList();

        Assert.Equal([".pdf", ".png"], endungen);
        Assert.All(WbExport.Formate, f => Assert.False(string.IsNullOrWhiteSpace(f.Label)));
    }

    /// <summary>
    /// <b>Jedes angebotene Format muss auch geschrieben werden können.</b> Ein Eintrag in der
    /// Liste, hinter dem eine Ausnahme wartet, ist schlimmer als ein fehlender — genau das war
    /// im Linux-Kopf der Zustand, nur andersherum: kein Eintrag, und dahinter ein
    /// <c>throw</c>.
    /// </summary>
    [Fact]
    public void Jedes_angebotene_Format_laesst_sich_auch_schreiben()
    {
        using var arbeit = new TempWorkspace("tafel-alle-formate");

        foreach (var format in WbExport.Formate)
        {
            string pfad = arbeit.File("probe" + format.PrimaryExtension);
            var ergebnis = WbExport.Exportieren(Buch(), "Probe", pfad);

            Assert.NotEmpty(ergebnis.Written);
            Assert.All(ergebnis.Written, d => Assert.True(File.Exists(d), d));
        }
    }
}
