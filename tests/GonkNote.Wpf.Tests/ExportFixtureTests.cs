using System.IO;
using System.Text;
using System.Windows.Documents;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Die Export-Fixtures aus Roadmap Phase 1: **je ein Referenzdokument** durch DOCX, Markdown
/// und PDF, verglichen gegen eingecheckte Golden-Files.
/// <para>
/// Wofür: Phase 4 ersetzt das <c>FlowDocument</c> durch eine eigene Dokument-Engine
/// (8–12 Wochen, „die Phase, an der Projekte sterben"). Die Exporter behalten dabei ihre
/// Aufgabe und tauschen nur ihre Gegenseite. Diese Golden-Files sind danach der einzige
/// Beweis, dass Tabelle, Bild, Diagramm, Inhaltsverzeichnis, Listen und Seiteneinrichtung
/// noch dasselbe ergeben.
/// </para>
/// Alles läuft über <see cref="Sta"/> auf einem eigenen STA-Thread — WPF verlangt das.
/// </summary>
public sealed class ExportFixtureTests
{
    // ==================== Markdown ====================

    /// <summary>
    /// Markdown ist der einzige Weg, dessen Ausgabe man direkt einchecken kann: reiner Text.
    /// Das Golden-File <c>referenz.md</c> ist damit gleichzeitig das lesbarste der drei.
    /// </summary>
    [Fact]
    public void Markdown_Export_bleibt_gleich() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("md-export");
        var doc = Referenzdokument.Bauen(werkbank.Blobs);
        string pfad = werkbank.Datei("referenz.md");

        MarkdownExporter.Export(doc, pfad);

        Golden.Assert("referenz.md", File.ReadAllText(pfad, Encoding.UTF8));
    });

    /// <summary>
    /// Ein Rohrzeichen in einer Zelle würde die Markdown-Tabelle sprengen — es muss maskiert
    /// werden. Steht schon im Golden-File, hier trotzdem eigens geprüft: an dieser Stelle
    /// fällt ein Fehler sonst erst dem Leser auf, nicht dem Diff.
    /// </summary>
    [Fact]
    public void Markdown_maskiert_Rohrzeichen_in_Zellen() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("md-pipe");
        var doc = Referenzdokument.Bauen(werkbank.Blobs);
        string pfad = werkbank.Datei("referenz.md");

        MarkdownExporter.Export(doc, pfad);
        string md = File.ReadAllText(pfad, Encoding.UTF8);

        Assert.Contains(@"Physik \| Chemie", md);

        // Jede Tabellenzeile muss gleich viele **trennende** Rohrzeichen haben, sonst ist es
        // keine Tabelle mehr. Maskierte (\|) zählen nicht mit — genau darum geht es hier.
        var tabellenzeilen = md.Split('\n').Where(z => z.StartsWith("| ", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(tabellenzeilen);

        int Trenner(string zeile) =>
            System.Text.RegularExpressions.Regex.Matches(zeile, @"(?<!\\)\|").Count;

        int spalten = Trenner(tabellenzeilen[0]);
        Assert.All(tabellenzeilen, z => Assert.Equal(spalten, Trenner(z)));
    });

    /// <summary>
    /// Das **Ziel** eines Verweises muss den Export überleben, nicht nur sein Text.
    /// <para>
    /// Der Fall war offen: <c>Hyperlink</c> erbt von <c>Span</c>, und der Span-Fall stand im
    /// Exporter zuerst — der Text kam durch, die URL fiel weg. Ein Markdown-Import mit Links
    /// war nach dem Rückexport also nur noch Fließtext.
    /// </para>
    /// Geprüft werden zusätzlich die drei Stellen, an denen die Markdown-Klammerung bricht:
    /// eckige Klammern im Text, Leerzeichen bzw. runde Klammern im Ziel, und ein Verweis
    /// ohne Ziel (den gibt es in den mitgelieferten Hilfe-Dokumenten).
    /// </summary>
    [Fact]
    public void Markdown_behaelt_das_Ziel_eines_Verweises() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("md-link");

        // Grundschriftgröße wie im Editor. Ohne sie steht ein FlowDocument auf der
        // System-Schriftgröße, und der Exporter hält jeden Absatz für eine Überschrift
        // (HeadingPrefix rechnet gegen die Größen aus TextStyles).
        var doc = new FlowDocument { FontSize = TextStyles.BodySize };

        doc.Blocks.Add(Absatz(new Hyperlink(new Run("Gonk Note"))
        {
            NavigateUri = new Uri("https://github.com/GONKstupid/GonkNote"),
        }));
        // Relatives Ziel, wie es aus einem Markdown-Import kommt: muss unverändert
        // zurückkommen und nicht zu file:///… werden.
        doc.Blocks.Add(Absatz(new Hyperlink(new Run("Kapitel 2"))
        {
            NavigateUri = new Uri("kapitel-2.md", UriKind.Relative),
        }));
        // Klammern und Leerzeichen im Ziel → spitze Klammern, sonst endet der Link zu früh.
        doc.Blocks.Add(Absatz(new Hyperlink(new Run("Wikipedia"))
        {
            NavigateUri = new Uri("https://de.wikipedia.org/wiki/Skia_(Bibliothek)"),
        }));
        // Eckige Klammern im Linktext → maskieren.
        doc.Blocks.Add(Absatz(new Hyperlink(new Run("siehe [dort]"))
        {
            NavigateUri = new Uri("https://example.org/"),
        }));
        // Ohne Ziel: bleibt Text, wird kein leerer Link.
        doc.Blocks.Add(Absatz(new Hyperlink(new Run("Erste Schritte"))));

        string pfad = werkbank.Datei("links.md");
        MarkdownExporter.Export(doc, pfad);
        var zeilen = File.ReadAllLines(pfad, Encoding.UTF8)
            .Where(z => z.Trim().Length > 0)
            .ToList();

        Assert.Equal(
        [
            "[Gonk Note](https://github.com/GONKstupid/GonkNote)",
            "[Kapitel 2](kapitel-2.md)",
            "[Wikipedia](<https://de.wikipedia.org/wiki/Skia_(Bibliothek)>)",
            @"[siehe \[dort\]](https://example.org/)",
            "Erste Schritte",
        ], zeilen);
    });

    private static Paragraph Absatz(Inline inhalt) => new(inhalt);

    // ==================== DOCX ====================

    [Fact]
    public void Docx_Export_bleibt_gleich() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("docx-export");
        var doc = Referenzdokument.Bauen(werkbank.Blobs);
        var einstellungen = Referenzdokument.Einstellungen();
        string pfad = werkbank.Datei("referenz.docx");

        int fehler = DocxExporter.Export(doc, einstellungen, Referenzdokument.Titel, pfad);

        // Der Exporter validiert selbst gegen das Office-2019-Schema. Ein Dokument, das Word
        // nicht öffnet, ist kein Export.
        Assert.Equal(0, fehler);
        Golden.Assert("referenz-docx.txt", DocxAufriss.Von(pfad));
    });

    /// <summary>
    /// Das Original geht unverändert hinaus — nicht die Anzeige-Ableitung und nicht ein von
    /// WPF neu kodiertes PNG. Gemessen war der Unterschied 2 MB Vorlage → 16,8 MB Export
    /// (<see cref="DocumentImages"/>); das ist der Grund für den ganzen Blob-Umweg.
    /// </summary>
    [Fact]
    public void Docx_Export_schickt_die_Originalbytes_hinaus() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("docx-original");
        var doc = Referenzdokument.Bauen(werkbank.Blobs);
        string pfad = werkbank.Datei("referenz.docx");

        // Die Originale, wie sie beim Bauen im Blob-Speicher gelandet sind.
        var originale = DocumentImages.UsedBlobs(doc)
            .Select(id => werkbank.Blobs.Read(id)!)
            .ToList();
        Assert.Equal(2, originale.Count);

        DocxExporter.Export(doc, Referenzdokument.Einstellungen(), Referenzdokument.Titel, pfad);

        var eingebettet = DocxBildteile(pfad);
        Assert.Equal(originale.Count, eingebettet.Count);
        // Byteweise gleich: keine Neukodierung, keine Verkleinerung.
        Assert.Equal(originale.Select(Kurzfassung), eingebettet.Select(Kurzfassung));

        // Und weil „byteweise gleich" auch für zwei leere Felder gilt: die Bilder müssen
        // dekodierbar sein und ihre Maße behalten.
        Assert.Equal(0, DocumentImages.LastExportMissingOriginals);
        foreach (var bytes in eingebettet)
        {
            using var bmp = GonkNote.Core.Rendering.WbImages.Decode(bytes);
            Assert.NotNull(bmp);
            Assert.True(bmp.Width > 0 && bmp.Height > 0);
        }
    });

    // ==================== PDF: Textdokument ====================

    /// <summary>
    /// Beim PDF eines Textdokuments wird **nicht** die Pixelfläche gehasht: die Seiten
    /// entstehen aus dem WPF-Paginator mit den Schriften des Systems, und schon ein
    /// Schriftartenupdate verschiebt jeden Pixel. Ein Hash wäre nach dem ersten falschen
    /// Alarm abgeschaltet.
    /// <para>
    /// Geprüft wird stattdessen, was bei einem Umbau wirklich kaputtgeht und was sich mit
    /// jeder Schrift gleich verhält: Seitenzahl, Seitenmaß und dass **keine** Seite leer ist.
    /// „Alles auf einer Seite" und „Seite 2 ist weiß" sind die beiden Fehlerbilder, die ein
    /// Layout-Umbau produziert.
    /// </para>
    /// </summary>
    [Fact]
    public void Pdf_Export_eines_Textdokuments_hat_gefuellte_Seiten() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("pdf-text");
        var doc = Referenzdokument.Bauen(werkbank.Blobs);
        var einstellungen = Referenzdokument.Einstellungen();
        string pfad = werkbank.Datei("referenz.pdf");

        PdfExporter.ExportFlowDocument(doc, einstellungen, Referenzdokument.Titel, pfad);

        Assert.True(File.Exists(pfad));
        int seiten = GonkNote.Core.Services.PdfImporter.PageCount(pfad);

        // Die Fixture ist mit Füllabsatz gebaut, damit sie über eine Seite hinausgeht.
        Assert.True(seiten >= 2, $"Nur {seiten} Seite(n) — der Seitenumbruch greift nicht.");

        var gelesen = GonkNote.Core.Services.PdfImporter
            .RenderPages(pfad, targetLongSide: 800)
            .ToList();
        Assert.Equal(seiten, gelesen.Count);

        for (int i = 0; i < gelesen.Count; i++)
        {
            var seite = gelesen[i];
            // A4 hochkant: die lange Kante ist die Höhe.
            Assert.True(seite.Height > seite.Width, $"Seite {i + 1} ist nicht hochkant.");

            double anteil = Farbanteil(seite.Data);
            // Untergrenze: das Wasserzeichen allein deckt schon einen guten Teil, Text kommt
            // dazu. Obergrenze: eine komplett zugelaufene Seite wäre auch ein Fehler.
            Assert.InRange(anteil, 0.02, 0.98);
        }
    });

    /// <summary>
    /// Kopf- und Fußzeile: die Kopfzeile ist auf der ersten Seite unterdrückt, auf der zweiten
    /// nicht. Das ist am Farbanteil im oberen Rand ablesbar — schriftunabhängig, weil nur
    /// „irgendetwas steht da" geprüft wird.
    /// </summary>
    [Fact]
    public void Kopfzeile_fehlt_auf_der_ersten_Seite_und_steht_auf_der_zweiten() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("pdf-kopfzeile");
        var doc = Referenzdokument.Bauen(werkbank.Blobs);

        // Ohne Wasserzeichen, sonst ist im Randbereich immer Farbe.
        var einstellungen = Referenzdokument.Einstellungen();
        einstellungen.WatermarkImage = null;

        var seiten = PdfExporter.RenderFlowDocumentPages(doc, einstellungen, Referenzdokument.Titel, scale: 2f);
        Assert.True(seiten.Count >= 2);

        // Oberer Rand = MarginTopCm; dort steht die Kopfzeile.
        double randAnteil = einstellungen.MarginTopCm * TextStyles.PxPerCm / TextStyles.PageSize(einstellungen).H;

        double erste = FarbanteilOben(seiten[0].Data, randAnteil);
        double zweite = FarbanteilOben(seiten[1].Data, randAnteil);

        Assert.Equal(0.0, erste, 4);
        Assert.True(zweite > 0.0005,
            $"Auf Seite 2 steht keine Kopfzeile (Farbanteil im oberen Rand {zweite:P3}).");
    });

    // ==================== PDF: Whiteboard ====================

    /// <summary>
    /// Der Whiteboard-/Notizbuch-PDF-Export geht über SkiaSharp — dieselben Zeichenroutinen
    /// wie im Editor. Geprüft wird über den Rückweg: das erzeugte PDF wieder durch
    /// <c>PdfImporter</c> (PDFium) lesen und die Seiten vermessen. Genau der Harness, den
    /// HANDOFF §7 für Phase 1 vorgesehen hat — nur als Test statt als Konsolenprogramm.
    /// </summary>
    [Fact]
    public void Whiteboard_PDF_hat_je_Seite_ein_Blatt_und_ueberlebt_den_Rueckimport() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("pdf-board");
        var doc = Referenzbuch.Bauen(werkbank.Blobs);
        string pfad = werkbank.Datei("referenz-buch.pdf");

        PdfExporter.ExportWhiteboard(doc, "Referenzbuch", pfad);

        Assert.Equal(doc.Pages.Count, GonkNote.Core.Services.PdfImporter.PageCount(pfad));

        var seiten = GonkNote.Core.Services.PdfImporter.RenderPages(pfad, targetLongSide: 700).ToList();
        Assert.Equal(doc.Pages.Count, seiten.Count);

        for (int i = 0; i < seiten.Count; i++)
        {
            // Seitenverhältnis muss zur Vorlage passen: A4 hochkant bzw. der Zuschnitt der
            // unendlichen Fläche. Ein vertauschtes Hoch-/Querformat fiele hier auf.
            var vorlage = doc.Pages[i];
            if (!vorlage.IsInfinite)
            {
                double sollVerhaeltnis = vorlage.Width / vorlage.Height;
                double istVerhaeltnis = (double)seiten[i].Width / seiten[i].Height;
                Assert.Equal(sollVerhaeltnis, istVerhaeltnis, 0.02);
            }

            Assert.True(Farbanteil(seiten[i].Data) > 0.001,
                $"Seite {i + 1} des Notizbuchs ist leer im PDF.");
        }
    });

    /// <summary>
    /// Der PNG-Weg desselben Dokuments: eine Datei je Seite, durchnummeriert. Der Export wird
    /// aus dem Ordnerbaum heraus angeboten und ist der einzige Weg, der ohne PDF-Betrachter
    /// prüfbar ist.
    /// </summary>
    [Fact]
    public void Whiteboard_PNG_Export_schreibt_eine_Datei_je_Seite() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("png-board");
        var doc = Referenzbuch.Bauen(werkbank.Blobs);

        var dateien = PdfExporter.ExportWhiteboardPng(doc, "Referenzbuch", werkbank.Datei("buch.png"));

        Assert.Equal(doc.Pages.Count, dateien.Count);
        Assert.Equal(dateien, dateien.Distinct());
        foreach (string datei in dateien)
        {
            Assert.True(File.Exists(datei), datei);
            using var bmp = GonkNote.Core.Rendering.WbImages.Decode(File.ReadAllBytes(datei));
            Assert.NotNull(bmp);
            // 2× Auflösung laut Exporter.
            Assert.True(bmp.Width >= 1000, $"{datei} ist nur {bmp.Width} px breit.");
        }
    });

    // ==================== Hilfsmittel ====================

    private static List<byte[]> DocxBildteile(string pfad)
    {
        using var docx = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(pfad, false);
        var teile = new List<byte[]>();
        foreach (var teil in docx.MainDocumentPart!.ImageParts)
        {
            using var strom = teil.GetStream();
            using var speicher = new MemoryStream();
            strom.CopyTo(speicher);
            teile.Add(speicher.ToArray());
        }
        return teile;
    }

    /// <summary>Länge + Hash statt der Bytes selbst — sonst ist die Fehlermeldung unlesbar.</summary>
    private static string Kurzfassung(byte[] bytes) =>
        $"{bytes.Length} Bytes, {Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes))[..16]}";

    /// <summary>Anteil der Pixel, die nicht (fast) weiß sind.</summary>
    private static double Farbanteil(byte[] bild) => FarbanteilOben(bild, 1.0);

    /// <summary>
    /// Anteil nicht-weißer Pixel im oberen <paramref name="anteilHoehe"/> des Bildes.
    /// Für die Kopfzeile: „steht im oberen Rand überhaupt etwas?"
    /// </summary>
    private static double FarbanteilOben(byte[] bild, double anteilHoehe)
    {
        using var bmp = GonkNote.Core.Rendering.WbImages.Decode(bild);
        Assert.NotNull(bmp);

        int bisZeile = Math.Max(1, (int)(bmp.Height * anteilHoehe));
        long farbig = 0, gesamt = 0;

        for (int y = 0; y < bisZeile; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                var p = bmp.GetPixel(x, y);
                gesamt++;
                if (p.Red <= 245 || p.Green <= 245 || p.Blue <= 245) farbig++;
            }

        return gesamt == 0 ? 0 : (double)farbig / gesamt;
    }
}
