using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using V = DocumentFormat.OpenXml.Vml;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace GonkNote.Core.Text;

/// <summary>
/// DOCX in beide Richtungen — gegen <see cref="TdDocument"/> statt gegen ein
/// <c>FlowDocument</c>.
///
/// <para>
/// <b>Warum das schon in Schritt 1 dasteht.</b> Die Roadmap verlangt es wörtlich: „Nach
/// jedem Schritt muss der DOCX-Roundtrip-Test grün sein." Der Grund steht in derselben
/// Zeile — Phase 4 ist die, an der Projekte sterben, und ein Modell ohne Gegenprobe wächst
/// so lange weiter, bis niemand mehr weiß, welcher Teil davon je funktioniert hat. DOCX ist
/// dafür die richtige Gegenprobe, weil es ein **fremdes** Format ist: es kennt die eigenen
/// Bequemlichkeiten nicht und deckt auf, was im Modell nur deshalb stimmt, weil es beim
/// Schreiben und Lesen denselben Fehler macht.
/// </para>
///
/// <para>
/// <b>Was hier absichtlich noch fehlt</b>, weil es der Reihenfolge aus Roadmap §5 folgt:
/// Bilder und Diagramme (Schritt 6). Alles davor ist da — Absätze und Zeichenformate
/// (Schritt 1), Abschnitte samt Kopf- und Fußzeile (Schritt 2), Listen (Schritt 3), Tabellen
/// (Schritt 4), Felder, Verweise und Inhaltsverzeichnis (Schritt 5). Das Wasserzeichen hängt
/// an <c>TextDoc</c> und ist ein Bild; es kommt mit Schritt 6.
/// </para>
///
/// <para>
/// <b>Seit dem Umverdrahten (HANDOFF §4.23) ist das der Weg, den die App nimmt</b> — für den
/// Export **und** für den Import, und seit §4.27 der **einzige**: <c>DocxExporter</c> und
/// <c>DocxImporter</c> sind beide gelöscht. Der letzte Aufrufer des alten Lesers war der
/// Whiteboard-Einfüge-Weg; er geht jetzt über <see cref="TdPdf.Seitenbilder"/> und braucht
/// dafür kein <c>FlowDocument</c> mehr.
/// <para>
/// Bis dahin standen beide nebeneinander, und das war Absicht: Sie **parallel zu pflegen**
/// wäre die Falle aus §4.10 gewesen — deshalb stand hier von Anfang an nur, was das Modell
/// wirklich trägt, und alles andere wirft, statt still zu verschwinden.
/// </para>
/// </para>
/// </summary>
public static partial class TdDocx
{
    // Ein Zoll sind 1440 Twips und 2,54 cm — die Umrechnung, an der jeder Einzug hängt.
    private const double TwipsProCm = 1440.0 / 2.54;

    private static int CmZuTwips(double cm) => (int)Math.Round(cm * TwipsProCm);
    private static double TwipsZuCm(double twips) => twips / TwipsProCm;

    // Schriftgrade stehen in DOCX als **halbe** Punkt, Abstände als Twips (1/20 pt).
    private static string PtZuHalbePunkt(double pt) => ((int)Math.Round(pt * 2)).ToString();
    private static double HalbePunktZuPt(double halbe) => halbe / 2.0;
    private static int PtZuTwips(double pt) => (int)Math.Round(pt * 20);
    private static double TwipsZuPt(double twips) => twips / 20.0;

    // Zeilenabstand: bei lineRule="auto" ist eine Zeile 240 Einheiten.
    private const double EinheitenProZeile = 240.0;

    // ==================== Schreiben ====================

    /// <summary>
    /// Was beim Schreiben für das **ganze** Dokument gilt.
    ///
    /// <para>
    /// Der <c>MainDocumentPart</c> steht hier, weil ein Verweis eine Beziehung braucht
    /// (<c>r:id</c>) und die am Dokumentteil hängt, nicht am Absatz. Und der Zähler steht hier,
    /// weil Textmarken **dokumentweit** eindeutig sein müssen: zwei Marken mit derselben
    /// Kennung sind kein Schemafehler, sondern ein Inhaltsverzeichnis, dessen Einträge alle an
    /// dieselbe Stelle springen.
    /// </para>
    /// </summary>
    private sealed class Kontext(MainDocumentPart main)
    {
        public MainDocumentPart Main { get; } = main;

        /// <summary>
        /// Woher die Bytes eines Bildes kommen. <c>null</c> ist erlaubt, solange das Dokument
        /// keine Bilder hat — wer eines schreibt und die Naht nicht mitgibt, bekommt eine
        /// Ausnahme und kein stillschweigend leeres Dokument (§4.21).
        /// </summary>
        public ITdImages? Bilder { get; init; }

        private uint _zeichnung;

        /// <summary>
        /// Die Kennung der nächsten Zeichnung. **Dokumentweit eindeutig und größer als null** —
        /// Word verwirft eine Zeichnung mit der Id 0 kommentarlos.
        /// </summary>
        public uint NaechsteZeichnung() => ++_zeichnung;

        /// <summary>
        /// Werden Sprungziele für Überschriften geschrieben?
        /// <para>
        /// <b>Nur, wenn es ein Inhaltsverzeichnis gibt.</b> Eine Textmarke ist ein Ziel; ohne
        /// Verzeichnis zeigt niemand darauf, und ein Dokument voller unbenutzter Marken ist
        /// beim Nachsehen im XML schwerer zu lesen als eines ohne.
        /// </para>
        /// </summary>
        public bool Textmarken { get; init; }

        private int _naechste;

        /// <summary>
        /// Der Name der nächsten Textmarke. <c>_Toc</c> ist Words eigene Schreibweise für
        /// Verzeichnis-Sprungziele — wer einen eigenen Namen erfindet, bekommt ein
        /// Verzeichnis, das Word beim Aktualisieren neu aufbaut und dabei anders benennt.
        /// </summary>
        public (string Name, int Id) NaechsteTextmarke()
        {
            _naechste++;
            return ($"_Toc{_naechste:D8}", _naechste);
        }
    }

    private static void Fuellen(
        TdDocument doc, WordprocessingDocument docx, ITdImages? bilder, string? titel = null)
    {
        if (titel is { Length: > 0 }) docx.PackageProperties.Title = titel;

        var main = docx.AddMainDocumentPart();
        main.Document = new W.Document(new W.Body());
        var body = main.Document.Body!;

        StandardformateSchreiben(doc, main);
        ListenSchreiben(doc, main);

        var k = new Kontext(main) { Textmarken = TdToc.Enthaelt(doc), Bilder = bilder };

        // Felder (PAGE/NUMPAGES) beim Öffnen aktualisieren lassen — sonst zeigt Word die
        // beim Schreiben eingesetzte 1 statt der echten Seitenzahl.
        main.AddNewPart<DocumentSettingsPart>().Settings =
            new W.Settings(new W.UpdateFieldsOnOpen { Val = true });

        for (int i = 0; i < doc.Sections.Count; i++)
        {
            var abschnitt = doc.Sections[i];
            bool letzter = i == doc.Sections.Count - 1;

            // Absätze und Tabellen in Dokumentreihenfolge. Die sectPr eines nicht-letzten
            // Abschnitts hängt am letzten **Absatz** — deshalb wird der eigens gemerkt.
            var teile = new List<OpenXmlElement>();
            W.Paragraph? letzterAbsatz = null;
            bool vorherTabelle = false;

            foreach (var block in abschnitt.Blocks)
            {
                switch (block)
                {
                    case TdParagraph p:
                        letzterAbsatz = AbsatzSchreiben(p, k);
                        teile.Add(letzterAbsatz);
                        vorherTabelle = false;
                        break;

                    case TdPageBreak:
                        letzterAbsatz = SeitenumbruchSchreiben();
                        teile.Add(letzterAbsatz);
                        vorherTabelle = false;
                        break;

                    case TdTable t:
                        // **Zwei Tabellen direkt hintereinander verschmelzen in Word zu
                        // einer.** Das ist keine Eigenheit unseres Schreibers, sondern die
                        // Art, wie Word ein Dokument einliest — dazwischen gehört ein
                        // Absatz. Der Leser nimmt ihn an derselben Stelle wieder heraus.
                        if (vorherTabelle) teile.Add(TrennabsatzSchreiben());
                        teile.Add(TabelleSchreiben(t, k));
                        vorherTabelle = true;
                        break;

                    // Ein neuer Blocktyp ohne Zweig würde hier still verschwinden — und ein
                    // verlorener Block fällt erst dem Leser auf, nicht dem Diff.
                    default:
                        throw new NotSupportedException(
                            $"{block.GetType().Name} kann noch nicht nach DOCX — siehe die Reihenfolge in Roadmap §5.");
                }
            }

            // **Eine Tabelle am Ende des Körpers braucht einen Absatz dahinter**, sonst hat
            // Word keine Stelle, an der der Cursor unter der Tabelle stehen kann — und der
            // letzte Abschnitt hätte nichts, woran seine sectPr hängen könnte.
            if (teile.Count == 0 || letzterAbsatz is null || vorherTabelle)
            {
                letzterAbsatz = TrennabsatzSchreiben();
                teile.Add(letzterAbsatz);
            }

            foreach (var teil in teile) body.AppendChild(teil);

            var sectPr = SeiteSchreiben(abschnitt.Page, main, k);

            // **Die Stelle, an der DOCX unsymmetrisch ist:** die Einrichtung des *letzten*
            // Abschnitts steht am Ende des Körpers, die aller anderen im Absatzformat ihres
            // jeweils letzten Absatzes. Wer sie überall ans Körperende hängt, bekommt ein
            // Dokument mit genau einer Seiteneinrichtung — und merkt es erst am Ausdruck.
            if (letzter)
            {
                body.AppendChild(sectPr);
            }
            else
            {
                var pPr = letzterAbsatz.ParagraphProperties;
                if (pPr is null)
                {
                    pPr = new W.ParagraphProperties();
                    letzterAbsatz.InsertAt(pPr, 0);
                }
                // Schema: sectPr steht in pPr ganz hinten (nur pPrChange folgt noch).
                pPr.AppendChild(sectPr);
            }
        }

        main.Document.Save();
    }

    // ==================== Felder ====================

    /// <summary>
    /// Die Anweisung eines Feldes, so wie Word sie schreibt.
    /// <para>
    /// Die Zusatzangabe steht in ihrer eigenen Schreibweise (<c>\@</c> für das Datumsmuster,
    /// <c>\o</c> für die Ebenen des Verzeichnisses) — beim Lesen wird genau sie wieder
    /// herausgeholt, damit die Angabe unverändert hin und zurück geht.
    /// </para>
    /// </summary>
    private static string Anweisung(TdField feld) => feld.Kind switch
    {
        TdFieldKind.PageNumber => " PAGE ",
        TdFieldKind.PageCount => " NUMPAGES ",
        TdFieldKind.Date =>
            $" DATE \\@ \"{feld.Argument ?? TdFieldValues.DatumsmusterStandard}\" ",
        TdFieldKind.Title => " TITLE ",
        _ => " TOC \\o \"" + Ebenenangabe(feld) + "\" \\h \\z \\u ",
    };

    private static string Ebenenangabe(TdField feld)
    {
        var (von, bis) = TdToc.Ebenen(feld.Argument);
        return TdToc.Ebenenangabe(von, bis);
    }

    // ==================== Bilder und Diagramme ====================

    // Ein Zentimeter sind 360 000 EMU („English Metric Units"), die Einheit jeder Zeichnung
    // in OOXML. Sie geht in Zoll **und** in Zentimetern auf — genau dafür ist sie erfunden.
    private const double EmuProCm = 360000.0;

    private static long CmZuEmu(double cm) => (long)Math.Round(Math.Max(0, cm) * EmuProCm);
    private static double EmuZuCm(double emu) => emu / EmuProCm;

    private const string UriBild = "http://schemas.openxmlformats.org/drawingml/2006/picture";
    private const string UriDiagramm = "http://schemas.openxmlformats.org/drawingml/2006/chart";

    /// <summary>
    /// Bildtyp aus der Endung des Originals. Unbekanntes geht als PNG hinaus — dieselbe
    /// Zuordnung, die schon der frühere <c>DocxExporter</c> benutzt hat.
    /// </summary>
    private static PartTypeInfo BildTeilTyp(string endung) => endung switch
    {
        "jpg" or "jpeg" => ImagePartType.Jpeg,
        "gif" => ImagePartType.Gif,
        "bmp" => ImagePartType.Bmp,
        "tif" or "tiff" => ImagePartType.Tiff,
        _ => ImagePartType.Png,
    };

    private static string BildEndung(ImagePart teil) => teil.ContentType switch
    {
        "image/jpeg" => "jpg",
        "image/png" => "png",
        "image/gif" => "gif",
        "image/bmp" => "bmp",
        "image/tiff" => "tif",
        _ => "png",
    };

    // Zwei Achsen, zwei Kennungen. Sie müssen nur innerhalb **eines** Diagramms eindeutig
    // sein; jedes bekommt seinen eigenen Teil.
    private const uint AchseKategorie = 111111111u;
    private const uint AchseWerte = 222222222u;

    private static C.ChartSpace DiagrammraumBauen(TdChart d)
    {
        var diagramm = new C.Chart();

        // Schema-Reihenfolge in CT_Chart: title, autoTitleDeleted, plotArea, legend,
        // plotVisOnly.
        if (d.Title.Length > 0)
        {
            diagramm.AppendChild(new C.Title(
                new C.ChartText(new C.RichText(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(new A.Run(new A.Text(d.Title))))),
                new C.Overlay { Val = false }));
            diagramm.AppendChild(new C.AutoTitleDeleted { Val = false });
        }
        else
        {
            // Ohne diese Angabe erfindet Word einen Titel aus dem Namen der ersten Reihe.
            diagramm.AppendChild(new C.AutoTitleDeleted { Val = true });
        }

        diagramm.AppendChild(FlaecheBauen(d));

        if (d.ShowLegend)
            diagramm.AppendChild(new C.Legend(
                new C.LegendPosition { Val = C.LegendPositionValues.Right },
                new C.Overlay { Val = false }));

        diagramm.AppendChild(new C.PlotVisibleOnly { Val = true });

        return new C.ChartSpace(diagramm);
    }

    private static C.PlotArea FlaecheBauen(TdChart d)
    {
        var flaeche = new C.PlotArea(new C.Layout());
        flaeche.AppendChild(GruppeBauen(d));

        // **Ein Kuchen hat keine Achsen** — und ein Balkendiagramm hat sie vertauscht: die
        // Kategorien stehen links, die Werte unten.
        if (d.Kind == TdChartKind.Pie) return flaeche;

        bool waagerecht = d.Kind == TdChartKind.Bar;

        flaeche.AppendChild(new C.CategoryAxis(
            new C.AxisId { Val = AchseKategorie },
            new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
            new C.Delete { Val = false },
            new C.AxisPosition { Val = waagerecht ? C.AxisPositionValues.Left : C.AxisPositionValues.Bottom },
            new C.CrossingAxis { Val = AchseWerte }));

        flaeche.AppendChild(new C.ValueAxis(
            new C.AxisId { Val = AchseWerte },
            new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
            new C.Delete { Val = false },
            new C.AxisPosition { Val = waagerecht ? C.AxisPositionValues.Bottom : C.AxisPositionValues.Left },
            new C.MajorGridlines(),
            new C.CrossingAxis { Val = AchseKategorie }));

        return flaeche;
    }

    /// <summary>
    /// Die Diagrammgruppe. **Punkt und Punkt+Linie sind in DrawingML ein Liniendiagramm** —
    /// <c>c:scatterChart</c> verlangt Zahlen auf **beiden** Achsen, und unsere Kategorien sind
    /// Text. Beim Punktdiagramm wird die Linie unsichtbar gemacht, statt sie wegzulassen: ein
    /// Liniendiagramm ohne <c>a:ln</c> zeichnet Word mit Linie.
    /// </summary>
    private static OpenXmlElement GruppeBauen(TdChart d)
    {
        int punkte = d.Punktzahl();

        switch (d.Kind)
        {
            case TdChartKind.Pie:
            {
                var gruppe = new C.PieChart(new C.VaryColors { Val = true });
                if (d.Series.Count > 0)
                {
                    var reihe = new C.PieChartSeries(
                        new C.Index { Val = 0U }, new C.Order { Val = 0U });
                    if (NameBauen(d.Series[0]) is { } name) reihe.AppendChild(name);
                    foreach (var punkt in FarbpunkteBauen(d, punkte)) reihe.AppendChild(punkt);
                    reihe.AppendChild(KategorienBauen(d, punkte));
                    reihe.AppendChild(WerteBauen(d.Series[0]));
                    gruppe.AppendChild(reihe);
                }
                gruppe.AppendChild(new C.FirstSliceAngle { Val = 0 });
                return gruppe;
            }

            case TdChartKind.Radar:
            {
                var gruppe = new C.RadarChart(
                    new C.RadarStyle { Val = C.RadarStyleValues.Marker },
                    new C.VaryColors { Val = false });

                for (int i = 0; i < d.Series.Count; i++)
                {
                    var reihe = new C.RadarChartSeries(
                        new C.Index { Val = (uint)i }, new C.Order { Val = (uint)i });
                    if (NameBauen(d.Series[i]) is { } name) reihe.AppendChild(name);
                    reihe.AppendChild(LinieBauen(d.Farbe(i), sichtbar: true));
                    reihe.AppendChild(MarkeBauen(d.Farbe(i), sichtbar: true));
                    reihe.AppendChild(KategorienBauen(d, punkte));
                    reihe.AppendChild(WerteBauen(d.Series[i]));
                    gruppe.AppendChild(reihe);
                }
                gruppe.AppendChild(new C.AxisId { Val = AchseKategorie });
                gruppe.AppendChild(new C.AxisId { Val = AchseWerte });
                return gruppe;
            }

            case TdChartKind.Line or TdChartKind.Scatter or TdChartKind.ScatterLine:
            {
                bool linie = d.Kind != TdChartKind.Scatter;
                bool marke = d.Kind != TdChartKind.Line;

                var gruppe = new C.LineChart(
                    new C.Grouping { Val = C.GroupingValues.Standard },
                    new C.VaryColors { Val = false });

                for (int i = 0; i < d.Series.Count; i++)
                {
                    var reihe = new C.LineChartSeries(
                        new C.Index { Val = (uint)i }, new C.Order { Val = (uint)i });
                    if (NameBauen(d.Series[i]) is { } name) reihe.AppendChild(name);
                    reihe.AppendChild(LinieBauen(d.Farbe(i), linie));
                    reihe.AppendChild(MarkeBauen(d.Farbe(i), marke));
                    reihe.AppendChild(KategorienBauen(d, punkte));
                    reihe.AppendChild(WerteBauen(d.Series[i]));
                    gruppe.AppendChild(reihe);
                }
                gruppe.AppendChild(new C.AxisId { Val = AchseKategorie });
                gruppe.AppendChild(new C.AxisId { Val = AchseWerte });
                return gruppe;
            }

            default:
            {
                var gruppe = new C.BarChart(
                    new C.BarDirection
                    {
                        Val = d.Kind == TdChartKind.Bar
                            ? C.BarDirectionValues.Bar
                            : C.BarDirectionValues.Column,
                    },
                    new C.BarGrouping { Val = C.BarGroupingValues.Clustered },
                    new C.VaryColors { Val = d.FarbeJeElement });

                for (int i = 0; i < d.Series.Count; i++)
                {
                    var reihe = new C.BarChartSeries(
                        new C.Index { Val = (uint)i }, new C.Order { Val = (uint)i });
                    if (NameBauen(d.Series[i]) is { } name) reihe.AppendChild(name);
                    if (!d.FarbeJeElement) reihe.AppendChild(FuellungBauen(d.Farbe(i)));
                    if (d.FarbeJeElement)
                        foreach (var punkt in FarbpunkteBauen(d, punkte)) reihe.AppendChild(punkt);
                    reihe.AppendChild(KategorienBauen(d, punkte));
                    reihe.AppendChild(WerteBauen(d.Series[i]));
                    gruppe.AppendChild(reihe);
                }
                gruppe.AppendChild(new C.GapWidth { Val = 150 });
                gruppe.AppendChild(new C.AxisId { Val = AchseKategorie });
                gruppe.AppendChild(new C.AxisId { Val = AchseWerte });
                return gruppe;
            }
        }
    }

    private static C.SeriesText? NameBauen(TdChartSeries reihe) =>
        reihe.Name.Length == 0 ? null : new C.SeriesText(new C.NumericValue(reihe.Name));

    private static C.CategoryAxisData KategorienBauen(TdChart d, int punkte)
    {
        var literal = new C.StringLiteral(new C.PointCount { Val = (uint)punkte });
        for (int i = 0; i < punkte; i++)
            literal.AppendChild(new C.StringPoint(new C.NumericValue(d.Kategorie(i))) { Index = (uint)i });
        return new C.CategoryAxisData(literal);
    }

    private static C.Values WerteBauen(TdChartSeries reihe)
    {
        var literal = new C.NumberLiteral(
            new C.FormatCode("General"),
            new C.PointCount { Val = (uint)reihe.Values.Count });

        for (int i = 0; i < reihe.Values.Count; i++)
            literal.AppendChild(new C.NumericPoint(
                new C.NumericValue(reihe.Values[i].ToString("R", CultureInfo.InvariantCulture)))
            { Index = (uint)i });

        return new C.Values(literal);
    }

    private static string HexOhneRaute(string farbe) => farbe.TrimStart('#').ToUpperInvariant();

    private static C.ChartShapeProperties FuellungBauen(string farbe) =>
        new(new A.SolidFill(new A.RgbColorModelHex { Val = HexOhneRaute(farbe) }));

    private static C.ChartShapeProperties LinieBauen(string farbe, bool sichtbar) =>
        new(sichtbar
            ? new A.Outline(new A.SolidFill(new A.RgbColorModelHex { Val = HexOhneRaute(farbe) })) { Width = 28575 }
            : new A.Outline(new A.NoFill()));

    private static C.Marker MarkeBauen(string farbe, bool sichtbar) =>
        sichtbar
            ? new C.Marker(
                new C.Symbol { Val = C.MarkerStyleValues.Circle },
                new C.Size { Val = 6 },
                FuellungBauen(farbe))
            : new C.Marker(new C.Symbol { Val = C.MarkerStyleValues.None });

    /// <summary>
    /// Farbe je **Element** statt je Reihe — beim Kuchen und bei einer einzelnen Reihe. Das
    /// ist die Regel des heutigen Editors, hier nur an einer Stelle statt in seiner
    /// Zeichenroutine.
    /// </summary>
    private static IEnumerable<C.DataPoint> FarbpunkteBauen(TdChart d, int punkte)
    {
        for (int i = 0; i < punkte; i++)
            yield return new C.DataPoint(
                new C.Index { Val = (uint)i },
                new C.Bubble3D { Val = false },
                FuellungBauen(d.Farbe(i)));
    }

    private static T Linie<T>(TdBorder b) where T : W.BorderType, new() => new()
    {
        // DOCX misst Rahmen in **Achtel-Punkt**. Eine 0,5-pt-Linie ist also die 4 — wer hier
        // Punkte einträgt, bekommt eine achtmal zu dicke Linie.
        Val = b.Sichtbar ? W.BorderValues.Single : W.BorderValues.None,
        Size = (uint)Math.Max(0, Math.Round(b.WidthPt * 8)),
        Color = b.Color.TrimStart('#'),
        Space = 0,
    };

    /// <summary>Absätze und Tabellen eines Behälters, in Dokumentreihenfolge.</summary>
    private static List<OpenXmlElement> Inhaltskinder(OpenXmlElement behaelter) =>
        [.. behaelter.ChildElements.Where(k => k is W.Paragraph or W.Table)];

    /// <summary>
    /// Ist das Kind an <paramref name="i"/> der leere Absatz, den der Schreiber **hinter**
    /// eine Tabelle setzt? Er steht dort nicht als Inhalt, sondern weil zwei Tabellen ohne
    /// ihn in Word zu einer verschmelzen und weil hinter der letzten Tabelle sonst keine
    /// Einfügemarke stünde.
    /// </summary>
    private static bool IstTrennabsatz(List<OpenXmlElement> kinder, int i)
    {
        if (kinder[i] is not W.Paragraph absatz || !IstLeererAbsatz(absatz)) return false;

        // Ohne eine Tabelle davor ist es ein gewöhnlicher leerer Absatz — und der ist Inhalt.
        if (i == 0 || kinder[i - 1] is not W.Table) return false;

        bool tabelleDanach = i + 1 < kinder.Count && kinder[i + 1] is W.Table;
        bool letzterImKoerper = i == kinder.Count - 1;

        // **Der dritte Fall, und der am wenigsten offensichtliche:** Endet ein *nicht
        // letzter* Abschnitt mit einer Tabelle, ist der Trennabsatz weder von einer weiteren
        // Tabelle gefolgt noch der letzte im Körper — er trägt aber die `sectPr`. Ohne diesen
        // Zweig käme er als leerer Absatz zurück, und das Dokument bekäme mit jedem Speichern
        // eine Leerzeile mehr.
        bool traegtAbschnitt = absatz.ParagraphProperties?.SectionProperties is not null;

        return tabelleDanach || letzterImKoerper || traegtAbschnitt;
    }

    /// <summary>
    /// Ein Absatz ohne Inhalt und ohne eigenes Format.
    /// <para>
    /// <b>Eine <c>sectPr</c> zählt dabei nicht als Format.</b> Endet ein Abschnitt mit einer
    /// Tabelle, trägt genau der Trennabsatz die Abschnittsangabe — würde er deswegen als
    /// „nicht leer" gelten, käme er beim Lesen als leerer Absatz zurück, und der Roundtrip
    /// wüchse mit jedem Durchgang um eine Zeile.
    /// </para>
    /// </summary>
    private static bool IstLeererAbsatz(W.Paragraph absatz)
    {
        // Ein Verweis und ein Feld in der kurzen Form sind **Geschwister** des Laufs, nicht
        // Teile von ihm. Wer nur nach Läufen sucht, hält einen Absatz, der nur einen Verweis
        // enthält, für leer — und wirft ihn hinter einer Tabelle weg.
        if (absatz.Elements<W.Run>().Any()) return false;
        if (absatz.Elements<W.Hyperlink>().Any()) return false;
        if (absatz.Elements<W.SimpleField>().Any()) return false;
        if (absatz.ParagraphProperties is not { } pPr) return true;

        return pPr.ChildElements.All(k => k is W.SectionProperties);
    }

    /// <summary>
    /// Der Name der Word-Vorlage zu einer Überschriftebene. <b>Für die beiden Absätze, die
    /// nicht ins Verzeichnis gehören, hat Word eigene eingebaute Vorlagen</b> — <c>Title</c>
    /// für den Dokumenttitel und <c>TOCHeading</c> für die Zeile darüber. Beide bauen auf
    /// <c>Heading 1</c> auf und stehen trotzdem nicht im Verzeichnis; wer stattdessen
    /// <c>Heading1</c> schriebe, bekäme in Word beim Aktualisieren beide wieder hinein.
    /// </summary>
    private static string Vorlagenname(int ebene, bool? ausgeschlossen) =>
        ausgeschlossen != true ? $"Heading{ebene}"
        : ebene == 1 ? TitelVorlage
        : $"Heading{ebene}";

    private const string TitelVorlage = "Title";

    // ==================== Wasserzeichen ====================

    // Ein Punkt ist 1/72 Zoll — die Einheit, in der VML seine Maße angibt.
    private const double PunktProCm = 72.0 / 2.54;

    private static string GainAusDeckkraft(double deckkraft) =>
        ((int)Math.Round(Math.Clamp(deckkraft, 0, 1) * 65536)).ToString(CultureInfo.InvariantCulture) + "f";

    private static double DeckkraftAusGain(string? gain)
    {
        if (gain is null) return 1;
        string zahl = gain.TrimEnd('f', 'F');
        return double.TryParse(zahl, NumberStyles.Float, CultureInfo.InvariantCulture, out double wert)
            ? Math.Clamp(wert / 65536.0, 0, 1)
            : 1;
    }

    /// <summary>Ein Maß aus einer VML-Stilangabe, in Zentimetern. Fehlt es, ist es 0.</summary>
    private static double StilmassCm(string? stil, string name)
    {
        if (stil is null) return 0;

        foreach (string teil in stil.Split(';'))
        {
            int doppelpunkt = teil.IndexOf(':');
            if (doppelpunkt < 0) continue;
            if (!teil[..doppelpunkt].Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) continue;

            string wert = teil[(doppelpunkt + 1)..].Trim().TrimEnd('p', 't', 'P', 'T');
            return double.TryParse(wert, NumberStyles.Float, CultureInfo.InvariantCulture, out double punkt)
                ? punkt / PunktProCm
                : 0;
        }
        return 0;
    }

    /// <summary>Zerlegt „Seite {SEITE} von {SEITEN}" in Text- und Platzhalterstücke.</summary>
    private static IEnumerable<string> ZerlegtNachPlatzhaltern(string vorlage)
    {
        int pos = 0;
        while (pos < vorlage.Length)
        {
            int auf = vorlage.IndexOf('{', pos);
            if (auf < 0) break;
            int zu = vorlage.IndexOf('}', auf);
            if (zu < 0) break;

            if (auf > pos) yield return vorlage[pos..auf];
            yield return vorlage[auf..(zu + 1)];
            pos = zu + 1;
        }
        if (pos < vorlage.Length) yield return vorlage[pos..];
    }

    /// <summary>
    /// Ein Absatz, dessen **einziger** Inhalt ein Seitenumbruch ist. Die Prüfung ist mit
    /// Absicht so eng: ein Absatz mit Umbruch **und** Text ist kein Seitenumbruchblock, und
    /// wer ihn dafür hält, verliert seinen Text.
    /// </summary>
    private static bool IstSeitenumbruch(W.Paragraph absatz)
    {
        var laeufe = absatz.Elements<W.Run>().ToList();
        if (laeufe.Count != 1) return false;

        var inhalt = laeufe[0].ChildElements.Where(c => c is not W.RunProperties).ToList();
        return inhalt.Count == 1
            && inhalt[0] is W.Break br
            && br.Type is not null
            && br.Type.Value == W.BreakValues.Page;
    }

    /// <summary>
    /// Nimmt aus jedem Textstück heraus, was <paramref name="unterlage"/> ohnehin sagt.
    /// <inheritdoc cref="TdCharFormat.Ohne" path="/summary/para"/>
    /// </summary>
    private static void EntdoppeltGegen(List<TdInline> stuecke, TdCharFormat unterlage)
    {
        foreach (var stueck in stuecke)
        {
            // Beim Verweis erst die Kinder — sie stehen unter *seinem* aufgelösten Format,
            // nicht unter dem des Absatzes.
            if (stueck is TdHyperlink verweis)
                EntdoppeltGegen(verweis.Inlines, verweis.Format.Over(unterlage));

            stueck.Format = stueck.Format.Ohne(unterlage);
        }
    }

    /// <summary>
    /// Die Überschriftebene hinter einem Vorlagennamen — <c>null</c>, wenn es keine Überschrift
    /// ist. <c>Title</c> und <c>TOCHeading</c> zählen als Ebene 1: so heißen Words eingebaute
    /// Vorlagen für genau die zwei Absätze, die oben stehen und trotzdem nicht ins Verzeichnis
    /// gehören.
    /// </summary>
    private static int? VorlagenEbene(string? name)
    {
        if (name is not { Length: > 0 }) return null;
        if (name is TitelVorlage or "TOCHeading") return 1;

        return name.StartsWith("Heading", StringComparison.Ordinal) &&
               int.TryParse(name.AsSpan("Heading".Length), out int ebene) &&
               ebene is >= 1 and <= 9
            ? ebene
            : null;
    }

    // ==================== Gegenprobe ====================

    /// <summary>
    /// Zählt die Verstöße gegen das Office-2019-Schema. **Ein Dokument, das Word nicht
    /// öffnet, ist kein Export** — dieselbe Messlatte, die schon der frühere <c>DocxExporter</c>
    /// angelegt hat.
    /// </summary>
    public static int Pruefen(string pfad)
    {
        using var docx = WordprocessingDocument.Open(pfad, false);
        return new OpenXmlValidator(FileFormatVersions.Office2019).Validate(docx).Count();
    }
}
