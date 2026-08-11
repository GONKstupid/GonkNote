using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using SkiaSharp;

namespace GonkNote.Core.Text;

/// <summary>
/// PDF und PNG eines Textdokuments — gegen <see cref="TdDocument"/> statt gegen den
/// WPF-Paginator. Der letzte Weg des Umverdrahtens (HANDOFF §4.23, Schritt 1 von zweien);
/// mit ihm ist §4.1 aufgelöst.
///
/// <para>
/// <b>Der Unterschied ist nicht nur, wer umbricht.</b> Der alte Weg rasterte jede Seite über
/// <c>RenderTargetBitmap</c> zu einem Bild und legte das Bild ins PDF — 300 dpi, aber eben ein
/// Foto: kein auswählbarer Text, keine Suche, kein anklickbarer Verweis, und eine Datei, die
/// mit der Seitenzahl linear wächst. Hier zeichnet <see cref="TdRenderer"/> **direkt auf die
/// PDF-Leinwand**. Skia schreibt daraus Textbefehle und bettet die Schriften ein: der Text im
/// PDF ist Text.
/// </para>
///
/// <para>
/// <b>Dieselben Zahlen wie auf dem Schirm.</b> Umbrochen wird mit <see cref="TdLayout"/> und
/// gemessen mit <see cref="TdSkiaMeasure"/> — genau das, was die Anzeige auch tut. Der einzige
/// Unterschied ist der Maßstab: hier <see cref="PunktProCm"/> statt
/// <see cref="TdRenderer.PixelProCm"/>. Weil der Umbruch in Zentimetern rechnet (§4.16), ändert
/// dieser Wechsel keine einzige Umbruchstelle — eine gedruckte Seite bricht dort um, wo sie es
/// auf dem Schirm auch tut.
/// </para>
///
/// <para>
/// <b>Er steht in Core und nicht im WPF-Kopf</b>, wie <see cref="TdDocx"/> und
/// <see cref="TdMarkdown"/> davor: der Linux- und der iPadOS-Kopf bekommen den PDF-Export damit
/// ohne Zutun. Der alte Weg konnte das nie — <c>FlowDocument</c> und
/// <c>DocumentPaginator</c> gibt es nur unter Windows.
/// </para>
/// </summary>
public static class TdPdf
{
    /// <summary>Ein Zoll sind 72 Punkt und 2,54 cm — die Einheit, in der ein PDF misst.</summary>
    public const double PunktProCm = 72.0 / 2.54;

    /// <summary>
    /// Wie fein eine PNG-Seite gerastert wird: das Vielfache der Bildschirmauflösung.
    /// Drei entspricht dem, was der alte Weg lieferte (96 dpi × 3 = 288 dpi).
    /// </summary>
    public const float PngVielfaches = 3f;

    /// <summary>
    /// Schreibt das Dokument als PDF.
    /// </summary>
    /// <param name="bilder">
    /// Woher die Bytes eines Bildes kommen. <c>null</c> ist erlaubt — dann bleibt jedes Bild
    /// ein Rahmen mit seinem Alternativtext, und das Dokument wird trotzdem geschrieben
    /// (Dauerregel 4).
    /// </param>
    /// <param name="titel">Der Wert für <c>{TITEL}</c> in Kopf- und Fußzeile.</param>
    /// <param name="felder">
    /// Datum und Titel für die Felder. <c>null</c> = <see cref="TdFieldContext.Ohne"/> plus
    /// <paramref name="titel"/> — <b>Core fragt die Uhr nicht selbst</b> (§4.20); ohne Angabe
    /// bleibt ein Datumsfeld leer, statt „heute" zu behaupten.
    /// </param>
    public static void Schreiben(
        TdDocument doc, string pfad, ITdImages? bilder = null, string titel = "",
        TdFieldContext? felder = null)
    {
        using var strom = File.Create(pfad);
        Schreiben(doc, strom, bilder, titel, felder);
    }

    /// <inheritdoc cref="Schreiben(TdDocument, string, ITdImages?, string, TdFieldContext?)"/>
    public static void Schreiben(
        TdDocument doc, Stream ziel, ITdImages? bilder = null, string titel = "",
        TdFieldContext? felder = null)
    {
        var (umbruch, kontext) = Setzen(doc, bilder, titel, felder);

        using var pdf = SKDocument.CreatePdf(ziel, new SKDocumentPdfMetadata
        {
            Title = titel,
            Producer = "Gonk Note",
            // 100 = verlustfrei. Ein eingebettetes Bild soll im PDF so scharf stehen wie im
            // Dokument; derselbe Wert und derselbe Grund wie beim Whiteboard-Export.
            EncodingQuality = 100,
        });

        foreach (var seite in umbruch.Pages)
        {
            var setup = seite.Setup;
            var leinwand = pdf.BeginPage(
                (float)(setup.WidthCm * PunktProCm), (float)(setup.HeightCm * PunktProCm));

            TdRenderer.Seite(leinwand, seite, PunktProCm, kontext);
            VerweiseVermerken(leinwand, seite);

            pdf.EndPage();
        }

        pdf.Close();
    }

    /// <summary>
    /// Dasselbe Dokument als PNG: eine Datei je Seite. Bei mehr als einer Seite werden sie
    /// durchnummeriert (<c>name-1.png</c>, <c>name-2.png</c> …), bei genau einer bleibt es beim
    /// angegebenen Pfad. Gibt zurück, was wirklich geschrieben wurde.
    /// </summary>
    public static List<string> Png(
        TdDocument doc, string pfad, ITdImages? bilder = null, string titel = "",
        TdFieldContext? felder = null, float vielfaches = PngVielfaches)
    {
        var (umbruch, kontext) = Setzen(doc, bilder, titel, felder);

        string ordner = Path.GetDirectoryName(pfad) ?? "";
        string stamm = Path.GetFileNameWithoutExtension(pfad);
        bool mehrere = umbruch.Pages.Count > 1;

        var geschrieben = new List<string>();
        for (int i = 0; i < umbruch.Pages.Count; i++)
        {
            using var abbild = Abbild(umbruch.Pages[i], kontext, vielfaches);
            using var daten = abbild.Encode(SKEncodedImageFormat.Png, 100);

            string ziel = mehrere ? Path.Combine(ordner, $"{stamm}-{i + 1}.png") : pfad;
            File.WriteAllBytes(ziel, daten.ToArray());
            geschrieben.Add(ziel);
        }
        return geschrieben;
    }

    /// <summary>
    /// Die Seiten als Bilder im Speicher — der Weg, auf dem ein Word-Dokument als Bildseiten
    /// ins Whiteboard oder Notizbuch kommt. Dieselbe Form wie beim PDF-Import, damit beide
    /// Einfüge-Wege danach eine einzige Fortsetzung haben.
    /// </summary>
    public static List<PdfImporter.PdfPageImage> Seitenbilder(
        TdDocument doc, ITdImages? bilder = null, string titel = "",
        TdFieldContext? felder = null, float vielfaches = 2f)
    {
        var (umbruch, kontext) = Setzen(doc, bilder, titel, felder);

        var ergebnis = new List<PdfImporter.PdfPageImage>();
        foreach (var seite in umbruch.Pages)
        {
            using var abbild = Abbild(seite, kontext, vielfaches);
            using var daten = abbild.Encode(SKEncodedImageFormat.Jpeg, 90);
            ergebnis.Add(new PdfImporter.PdfPageImage(daten.ToArray(), abbild.Width, abbild.Height));
        }
        return ergebnis;
    }

    // ==================== Das Gemeinsame ====================

    /// <summary>
    /// Umbrechen und den Zeichenkontext bauen — der eine Weg, den alle drei Ausgaben nehmen.
    /// <para>
    /// <b>Der Titel wandert in die Felder</b>, wenn der Aufrufer keinen eigenen Feldkontext
    /// mitgibt: sonst stünde in der Kopfzeile <c>{TITEL}</c> als leerer Platz, obwohl der Titel
    /// eine Zeile weiter oben im Aufruf steht.
    /// </para>
    /// </summary>
    private static (TdLayoutResult Umbruch, TdRenderContext Kontext) Setzen(
        TdDocument doc, ITdImages? bilder, string titel, TdFieldContext? felder)
    {
        var werte = felder ?? new TdFieldContext { Title = titel };

        // **Gemessen wird mit derselben Maschine, mit der gezeichnet wird** (§4.16): eine
        // Zeile, die anders gemessen als gezeichnet wird, bricht an einer Stelle um und steht
        // an einer anderen.
        using var messung = new TdSkiaMeasure();
        var umbruch = TdLayout.Umbrechen(doc, messung, werte);

        return (umbruch, new TdRenderContext(bilder, werte, umbruch.PageCount));
    }

    /// <summary>Eine gesetzte Seite als Rasterbild, in <paramref name="vielfaches"/>facher Auflösung.</summary>
    private static SKImage Abbild(TdPage seite, TdRenderContext kontext, float vielfaches)
    {
        double massstab = TdRenderer.PixelProCm * vielfaches;
        var setup = seite.Setup;

        var info = new SKImageInfo(
            Math.Max(1, (int)Math.Round(setup.WidthCm * massstab)),
            Math.Max(1, (int)Math.Round(setup.HeightCm * massstab)));

        using var flaeche = SKSurface.Create(info);

        // Papier ist weiß, und der Zeichner malt es selbst. Vorher trotzdem füllen: ein PNG
        // hat einen Alphakanal, und ein Rundungsrest am Rand wäre sonst durchsichtig statt
        // weiß — im Bildbetrachter schwarz.
        flaeche.Canvas.Clear(TdRenderer.Papier);
        TdRenderer.Seite(flaeche.Canvas, seite, massstab, kontext);

        return flaeche.Snapshot();
    }

    // ==================== Verweise ====================

    /// <summary>
    /// Legt über jedes Stück, das zu einem Verweis gehört, die Sprungmarke des PDF.
    ///
    /// <para>
    /// <b>Das geht nur hier und nicht im Zeichner:</b> Eine Sprungmarke ist kein Pixel, sondern
    /// ein Vermerk, den nur ein PDF kennt — <c>DrawUrlAnnotation</c> ist auf einer
    /// Bildschirm-Leinwand wirkungslos. Der Zeichner malt den Verweis blau und unterstrichen;
    /// anklickbar wird er an dieser Stelle.
    /// </para>
    /// <para>
    /// Die Maße kommen aus demselben Umbruch, aus dem der Zeichner sie nimmt — Ort des Stücks
    /// plus Rand der Seite, alles in Zentimetern. Gezeichnet wird nichts; wer hier rechnete,
    /// hätte zwei Rechnungen für denselben Ort.
    /// </para>
    /// </summary>
    private static void VerweiseVermerken(SKCanvas leinwand, TdPage seite)
    {
        var setup = seite.Setup;
        double randX = setup.MarginLeftCm;
        double randY = setup.MarginTopCm;

        foreach (var zeile in seite.Lines)
            ZeileVermerken(leinwand, zeile, randX, randY);

        // In einer Tabelle zählt das YCm der Zeile ab der **Oberkante der Zelle** und nicht ab
        // der Seite — genauso setzt der Zeichner beides zusammen.
        foreach (var tabellenzeile in seite.TableRows)
            foreach (var zelle in tabellenzeile.Cells)
                foreach (var zeile in zelle.Lines)
                    ZeileVermerken(leinwand, zeile, randX + zelle.XCm, randY + tabellenzeile.YCm);
    }

    private static void ZeileVermerken(SKCanvas leinwand, TdLine zeile, double randX, double randY)
    {
        foreach (var stueck in zeile.Runs)
        {
            if (stueck.Link is not { Target.Length: > 0 } verweis) continue;
            if (stueck.WidthCm <= 0) continue;

            var kasten = SKRect.Create(
                (float)((randX + stueck.XCm) * PunktProCm),
                (float)((randY + zeile.YCm) * PunktProCm),
                (float)(stueck.WidthCm * PunktProCm),
                (float)(zeile.HeightCm * PunktProCm));

            using var ziel = SKData.CreateCopy(
                System.Text.Encoding.UTF8.GetBytes(verweis.Target + "\0"));
            leinwand.DrawUrlAnnotation(kasten, ziel);
        }
    }
}
