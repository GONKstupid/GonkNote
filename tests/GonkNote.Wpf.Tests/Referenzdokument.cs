using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.Core.Services;
using GonkNote.Services;
using SkiaSharp;
using TextDoc = GonkNote.Core.Models.TextDoc;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Das Referenzdokument der Export-Fixtures: **ein** Textdokument, das alles enthält, was die
/// Roadmap für Phase 1 nennt — Überschriften, Zeichenformate, Tabelle, Bild, Diagramm,
/// Inhaltsverzeichnis, Listen, Kopf-/Fußzeile, Wasserzeichen.
/// <para>
/// Diese Vorlage und die daraus erzeugten Golden-Files sind später der **einzige Beweis**,
/// dass die eigene Dokument-Engine aus Phase 4 nichts verschlechtert hat. Wer hier etwas
/// wegnimmt, nimmt genau das aus der Beweislage heraus.
/// </para>
/// Alles ist bewusst festgelegt und ohne Uhrzeit: kein <c>{DATUM}</c> in der Fußzeile, keine
/// Zufallsfarben, keine <c>DateTime.Now</c>. Ein Golden-File, das sich morgen von selbst
/// ändert, ist kein Golden-File.
/// </summary>
internal static class Referenzdokument
{
    public const string Titel = "Gonk Note — Referenzdokument";

    /// <summary>Einstellungen der Seite. Absichtlich **nicht** die Standardwerte.</summary>
    public static TextDoc Einstellungen() => new()
    {
        PageFormat = "A4",
        Landscape = false,
        MarginLeftCm = 2.5,
        MarginTopCm = 2.0,
        MarginRightCm = 2.0,
        MarginBottomCm = 2.0,
        HeaderText = "{TITEL}",
        FooterText = "Seite {SEITE} von {SEITEN}",
        SuppressHeaderOnFirstPage = true,
        WatermarkImage = Bild(200, 280, SKColors.WhiteSmoke, SKColors.LightSteelBlue),
        WatermarkOpacity = 0.25,
    };

    /// <summary>
    /// Baut das Dokument. <paramref name="blobs"/> nimmt die Originalbytes der Bilder auf —
    /// nur mit Verweis gehen sie beim DOCX-Export unverändert wieder hinaus (statt von WPF
    /// als PNG neu kodiert zu werden, siehe <see cref="DocumentImages"/>).
    /// </summary>
    public static FlowDocument Bauen(BlobStore blobs)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = TextStyles.BodySize,
            Foreground = new SolidColorBrush(TextStyles.InkLight),
        };

        // Ausrichtung ausdrücklich setzen, wo sie geprüft werden soll. Ohne Angabe steht ein
        // FlowDocument auf Justify — der Export schreibt dann „both". Das ist richtig, sieht
        // im Aufriss aber nach einer Entscheidung aus, die niemand getroffen hat.
        var titel = Absatz("Titel", Titel);
        titel.TextAlignment = TextAlignment.Center;
        doc.Blocks.Add(titel);

        // --- Inhaltsverzeichnis: Titelabsatz jetzt, Einträge zum Schluss ------------------
        var tocTitel = Absatz("Überschrift 1", TextStyles.TocTitle);
        doc.Blocks.Add(tocTitel);

        // --- Fließtext mit Zeichenformaten ------------------------------------------------
        doc.Blocks.Add(Absatz("Überschrift 1", "Einleitung"));

        var mischung = new Paragraph { Margin = Standard.Margin };
        mischung.Inlines.Add(new Run("Dieser Absatz enthält "));
        mischung.Inlines.Add(new Run("fetten") { FontWeight = FontWeights.Bold });
        mischung.Inlines.Add(new Run(", "));
        mischung.Inlines.Add(new Run("kursiven") { FontStyle = FontStyles.Italic });
        mischung.Inlines.Add(new Run(" und "));
        mischung.Inlines.Add(new Run("durchgestrichenen")
        {
            TextDecorations = TextDecorations.Strikethrough,
        });
        mischung.Inlines.Add(new Run(" Text sowie einen "));
        mischung.Inlines.Add(new Hyperlink(new Run("Verweis"))
        {
            NavigateUri = new Uri("https://github.com/GONKstupid/GonkNote"),
        });
        mischung.Inlines.Add(new Run("."));
        doc.Blocks.Add(mischung);

        // --- Tabelle ----------------------------------------------------------------------
        doc.Blocks.Add(Absatz("Überschrift 2", "Tabelle"));
        doc.Blocks.Add(Tabelle());

        // --- Bild -------------------------------------------------------------------------
        doc.Blocks.Add(Absatz("Überschrift 2", "Bild"));
        doc.Blocks.Add(BildAbsatz(blobs, Bild(320, 200, SKColors.MediumSeaGreen, SKColors.DarkSlateGray),
            breite: 240, hoehe: 150));

        // --- Diagramm ---------------------------------------------------------------------
        // In der App entsteht ein Diagramm als RenderTargetBitmap aus dem ChartDialog und
        // landet als ganz normales Bild im Dokument. Für den Export ist es deshalb dasselbe
        // wie ein Foto — geprüft wird, dass **zwei** Bilder unterschiedlicher Größe getrennt
        // durchkommen und nicht eines das andere überschreibt.
        doc.Blocks.Add(Absatz("Überschrift 3", "Diagramm"));
        doc.Blocks.Add(BildAbsatz(blobs, Diagramm(), breite: 300, hoehe: 180));

        // --- Listen -----------------------------------------------------------------------
        doc.Blocks.Add(Absatz("Überschrift 2", "Listen"));
        doc.Blocks.Add(Liste(TextMarkerStyle.Disc, "Erster Punkt", "Zweiter Punkt", "Dritter Punkt"));
        doc.Blocks.Add(Liste(TextMarkerStyle.Decimal, "Erster Schritt", "Zweiter Schritt"));

        // --- Trennlinie (BlockUIContainer) -------------------------------------------------
        doc.Blocks.Add(new BlockUIContainer(new Border
        {
            Height = 1,
            Background = Brushes.Silver,
            Margin = new Thickness(0, 8, 0, 8),
        }));

        doc.Blocks.Add(Absatz("Überschrift 4", "Anhang"));

        var rechts = Absatz("Zitat", "Rechtsbündig ausgerichtetes Zitat.");
        rechts.TextAlignment = TextAlignment.Right;
        doc.Blocks.Add(rechts);

        doc.Blocks.Add(Absatz("Standard",
            "Ein längerer Schlussabsatz, damit das Dokument über eine Seite hinausgeht und der " +
            "Seitenumbruch, die Kopfzeile ab Seite zwei und die Fußzeilen-Zählung überhaupt " +
            "geprüft werden können. " + Fuelltext()));

        // --- Inhaltsverzeichnis nachtragen ------------------------------------------------
        // Genauso, wie es die App tut (TextEditorView.Refs): aus CollectHeadings, hinter dem
        // Titelabsatz, im Eintragsformat (Größe 13 + Einzug in 18er-Schritten). Damit prüft
        // die Fixture auch die Erkennung selbst — nicht nur handgeschriebene Absätze.
        Block anker = tocTitel;
        foreach (var (ebene, text, _) in TextStyles.CollectHeadings(doc))
        {
            var eintrag = new Paragraph(new Run(text))
            {
                FontSize = TextStyles.TocEntrySize,
                FontWeight = ebene == 1 ? FontWeights.SemiBold : FontWeights.Normal,
                Margin = new Thickness((ebene - 1) * 18, 0, 0, 2),
            };
            doc.Blocks.InsertAfter(anker, eintrag);
            anker = eintrag;
        }

        return doc;
    }

    // ---- Bausteine -----------------------------------------------------------------------

    private static TextStyles.ParaStyle Standard => TextStyles.All.First(s => s.Name == "Standard");

    private static Paragraph Absatz(string vorlage, string text)
    {
        var s = TextStyles.All.First(x => x.Name == vorlage);
        return new Paragraph(new Run(text))
        {
            FontSize = s.Size,
            FontWeight = s.Weight,
            FontStyle = s.Style,
            Margin = s.Margin,
            Foreground = new SolidColorBrush(s.ColorHex != null
                ? (Color)ColorConverter.ConvertFromString(s.ColorHex)
                : TextStyles.InkLight),
        };
    }

    private static Table Tabelle()
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 6, 0, 10) };
        // Feste Spaltenbreiten: der DOCX-Export schreibt sie als gridCol mit — ohne feste
        // Breiten wäre der Aufriss von der Fensterbreite abhängig.
        foreach (double breite in new[] { 160d, 120d, 100d })
            table.Columns.Add(new TableColumn { Width = new GridLength(breite) });

        var gruppe = new TableRowGroup();

        // Schattierung gehört an die **Zelle**, nicht an die Zeile: der DOCX-Export liest
        // cell.Background (shd), und auch die App setzt sie überall je Zelle
        // (TextEditorView.Table). Eine über TableRow.Background eingefärbte Zeile käme im
        // DOCX ungefärbt an.
        var kopf = new TableRow();
        foreach (string text in new[] { "Fach", "Note", "Datum" })
            kopf.Cells.Add(Zelle(text, fett: true, fuellung: Brushes.LightSteelBlue));
        gruppe.Rows.Add(kopf);

        foreach (var (fach, note, datum) in new[]
        {
            ("Mathematik", "2", "12.03.2026"),
            ("Deutsch", "1", "19.03.2026"),
            ("Physik | Chemie", "3", "26.03.2026"),   // Pipe: muss im Markdown maskiert werden
        })
        {
            var zeile = new TableRow();
            zeile.Cells.Add(Zelle(fach));
            zeile.Cells.Add(Zelle(note));
            zeile.Cells.Add(Zelle(datum));
            gruppe.Rows.Add(zeile);
        }

        table.RowGroups.Add(gruppe);
        return table;
    }

    private static TableCell Zelle(string text, bool fett = false, Brush? fuellung = null) =>
        new(new Paragraph(new Run(text) { FontWeight = fett ? FontWeights.Bold : FontWeights.Normal })
        {
            Margin = new Thickness(0),
        })
        {
            Background = fuellung,
            BorderBrush = Brushes.Gainsboro,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(4, 2, 4, 2),
        };

    private static Paragraph BildAbsatz(BlobStore blobs, byte[] bytes, double breite, double hoehe)
    {
        var bild = new Image
        {
            Source = DocumentImages.Proxy(bytes),
            Width = breite,
            Height = hoehe,
        };
        // Verweis auf das Original setzen — dieselbe Stelle, an der die App es beim Import tut.
        DocumentImages.Remember(bild, bytes, "png", blobs);

        return new Paragraph(new InlineUIContainer(bild)) { Margin = new Thickness(0, 4, 0, 10) };
    }

    private static List Liste(TextMarkerStyle marker, params string[] punkte)
    {
        var liste = new List { MarkerStyle = marker, Margin = new Thickness(24, 4, 0, 8) };
        foreach (string punkt in punkte)
            liste.ListItems.Add(new ListItem(new Paragraph(new Run(punkt)) { Margin = new Thickness(0, 0, 0, 2) }));
        return liste;
    }

    private static string Fuelltext() =>
        string.Join(' ', Enumerable.Repeat(
            "Der Text hier hat keinen Inhalt, nur Länge — er soll den Umbruch erzwingen.", 14));

    // ---- Bilder --------------------------------------------------------------------------

    /// <summary>
    /// Erzeugte Bilder statt eingecheckter: eine Binärdatei im Repo bräuchte eine geklärte
    /// Lizenz (HANDOFF §6), zwei Rechtecke und ein Kreuz brauchen keine.
    /// </summary>
    public static byte[] Bild(int width, int height, SKColor grund, SKColor strich)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var c = surface.Canvas;
        c.Clear(grund);
        using (var pen = new SKPaint { Color = strich, StrokeWidth = 3, IsAntialias = true })
        {
            // Diagonalen: ein rotations- oder achsensymmetrisches Bild würde eine vertauschte
            // Achse im Export nicht zeigen.
            c.DrawLine(0, 0, width, height, pen);
            c.DrawLine(0, height * 0.75f, width, height * 0.25f, pen);
        }
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Ein Balkendiagramm — dasselbe, was der ChartDialog als Bild ins Dokument legt.</summary>
    private static byte[] Diagramm()
    {
        const int w = 400, h = 240;
        using var surface = SKSurface.Create(new SKImageInfo(w, h));
        var c = surface.Canvas;
        c.Clear(SKColors.White);

        float[] werte = [0.35f, 0.72f, 0.5f, 0.9f, 0.18f];
        SKColor[] farben =
        [
            SKColor.Parse("#2563EB"), SKColor.Parse("#14B8A6"), SKColor.Parse("#8B5CF6"),
            SKColor.Parse("#EC4899"), SKColor.Parse("#F59E0B"),
        ];

        float rand = 24f, breite = (w - 2 * rand) / werte.Length;
        for (int i = 0; i < werte.Length; i++)
        {
            using var balken = new SKPaint { Color = farben[i], IsAntialias = true };
            float hoehe = (h - 2 * rand) * werte[i];
            c.DrawRect(SKRect.Create(rand + i * breite + 6, h - rand - hoehe, breite - 12, hoehe), balken);
        }
        using (var achse = new SKPaint { Color = SKColors.DimGray, StrokeWidth = 2 })
        {
            c.DrawLine(rand, h - rand, w - rand, h - rand, achse);
            c.DrawLine(rand, rand, rand, h - rand, achse);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// Eine Wegwerf-Datenbank samt Blob-Speicher. Nötig, weil die Exportwege über
    /// <see cref="BlobStore.Current"/> gehen (Originalbytes der Bilder, Wasserzeichen) — und
    /// weil in der echten Datenbank nie getestet wird (HANDOFF §7).
    /// </summary>
    public sealed class Werkbank : IDisposable
    {
        private readonly DatabaseService _db;

        public Werkbank(string label)
        {
            Ordner = Path.Combine(Path.GetTempPath(), "gonk-tests", $"{label}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Ordner);
            // Der Konstruktor setzt BlobStore.Current und ImageCache.Source — genau darauf
            // greifen PdfExporter und DocxExporter zu.
            _db = new DatabaseService(Path.Combine(Ordner, "wegwerf.db"));
        }

        public string Ordner { get; }
        public BlobStore Blobs => _db.Blobs;
        public string Datei(string name) => Path.Combine(Ordner, name);

        public void Dispose()
        {
            _db.Dispose();
            try { Directory.Delete(Ordner, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
