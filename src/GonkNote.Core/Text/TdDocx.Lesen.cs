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
/// <see cref="TdDocx"/> — die lesende Hälfte. Aus der 2448-Zeilen-Fassung
/// herausgelöst; der Code ist unverändert, nur die Datei ist geteilt — dasselbe
/// Muster wie TdEdit/TdFormatEdit oder WhiteboardView.*.cs.
/// </summary>
public static partial class TdDocx
{
    private static TdListMarker AusDocx(W.NumberFormatValues wert)
    {
        if (wert == W.NumberFormatValues.Decimal) return TdListMarker.Decimal;
        if (wert == W.NumberFormatValues.LowerLetter) return TdListMarker.LowerLetter;
        if (wert == W.NumberFormatValues.UpperLetter) return TdListMarker.UpperLetter;
        if (wert == W.NumberFormatValues.LowerRoman) return TdListMarker.LowerRoman;
        if (wert == W.NumberFormatValues.UpperRoman) return TdListMarker.UpperRoman;
        return TdListMarker.Bullet;
    }

    private static void ListenLesen(TdDocument doc, MainDocumentPart main)
    {
        if (main.NumberingDefinitionsPart?.Numbering is not { } nummerierung) return;

        // Erst die Vorlagen einsammeln, dann die Instanzen darauf abbilden — ein `num` kann
        // auf ein `abstractNum` zeigen, das im XML **danach** steht.
        var vorlagen = new Dictionary<int, List<TdListLevel>>();

        foreach (var vorlage in nummerierung.Elements<W.AbstractNum>())
        {
            if (vorlage.AbstractNumberId?.Value is not { } id) continue;

            var ebenen = new List<TdListLevel>();
            foreach (var lvl in vorlage.Elements<W.Level>())
            {
                var ebene = new TdListLevel
                {
                    Start = lvl.StartNumberingValue?.Val?.Value ?? 1,
                    Text = lvl.LevelText?.Val?.Value ?? "",
                };
                if (lvl.NumberingFormat?.Val?.Value is { } art) ebene.Marker = AusDocx(art);

                if (lvl.PreviousParagraphProperties?.GetFirstChild<W.Indentation>() is { } einzug)
                {
                    if (einzug.Left?.Value is { } l && double.TryParse(l, out double lv)) ebene.IndentCm = TwipsZuCm(lv);
                    if (einzug.Hanging?.Value is { } h && double.TryParse(h, out double hv)) ebene.HangingCm = TwipsZuCm(hv);
                }
                ebenen.Add(ebene);
            }
            vorlagen[id] = ebenen;
        }

        foreach (var instanz in nummerierung.Elements<W.NumberingInstance>())
        {
            if (instanz.NumberID?.Value is not { } id) continue;
            if (instanz.AbstractNumId?.Val?.Value is not { } vorlagenId) continue;
            if (!vorlagen.TryGetValue(vorlagenId, out var ebenen)) continue;

            doc.Lists.Add(new TdListDefinition { Id = id, Levels = ebenen });
        }
    }

    /// <summary>
    /// Die Feldart und ihre Zusatzangabe aus einer Anweisung — oder <c>null</c>, wenn wir das
    /// Feld nicht kennen.
    /// <para>
    /// Word hängt an fast jedes Feld noch Schalter an (<c>\* MERGEFORMAT</c>); ausgewertet
    /// wird deshalb nur das **erste Wort** und die eine Angabe, die uns gehört.
    /// </para>
    /// </summary>
    private static TdField? FeldAusAnweisung(string anweisung)
    {
        string text = anweisung.Trim();
        if (text.Length == 0) return null;

        int ende = text.IndexOfAny([' ', '\t']);
        string name = (ende < 0 ? text : text[..ende]).ToUpperInvariant();

        return name switch
        {
            "PAGE" => new TdField(TdFieldKind.PageNumber),
            "NUMPAGES" => new TdField(TdFieldKind.PageCount),
            "DATE" => new TdField(TdFieldKind.Date, Schalterwert(text, "\\@")),
            "TITLE" => new TdField(TdFieldKind.Title),
            "TOC" => new TdField(TdFieldKind.TableOfContents, Schalterwert(text, "\\o")),
            _ => null,
        };
    }

    /// <summary>Der Wert eines Schalters: <c>\@ "dd.MM.yyyy"</c> ergibt <c>dd.MM.yyyy</c>.</summary>
    private static string? Schalterwert(string anweisung, string schalter)
    {
        int start = anweisung.IndexOf(schalter, StringComparison.Ordinal);
        if (start < 0) return null;

        int auf = anweisung.IndexOf('"', start + schalter.Length);
        if (auf < 0) return null;

        int zu = anweisung.IndexOf('"', auf + 1);
        return zu < 0 ? null : anweisung[(auf + 1)..zu];
    }

    /// <summary>
    /// Liest die dreiteilige Feldform: zwischen <c>begin</c> und <c>end</c> steht die
    /// Anweisung, nach einem <c>separate</c> das zwischengespeicherte Ergebnis.
    ///
    /// <para>
    /// <b>Ein Feldergebnis ist kein Text.</b> Ein Dokument aus Word bringt es mit — ein
    /// Inhaltsverzeichnis kommt dort als dreißig Absätze samt Seitenzahlen daher. Wer das als
    /// Inhalt liest, hat das Verzeichnis zweimal im Dokument: einmal als Feld und einmal als
    /// Text, der beim nächsten Aktualisieren nicht mitwandert.
    /// </para>
    /// <para>
    /// <b>Ein Feld, das wir nicht kennen, verliert seine Rechenvorschrift — aber nicht seinen
    /// Text.</b> Dann wird das Ergebnis doch übernommen: eine <c>REF</c>-Angabe wieder
    /// ausrechnen zu können ist schön, aber ihren Text zu verlieren ist Datenverlust.
    /// </para>
    /// </summary>
    private sealed class Feldleser
    {
        private int _tiefe;
        private bool _imErgebnis;
        private TdCharFormat _format = new();
        private readonly System.Text.StringBuilder _anweisung = new();
        private readonly List<TdInline> _ergebnis = new();

        /// <summary>Steht der Leser gerade in einem Feld?</summary>
        public bool Aktiv => _tiefe > 0;

        /// <param name="format">
        /// Das Zeichenformat des Laufs, der das Feld eröffnet. Word legt es dort ab, und ohne
        /// diese Übernahme verlöre ein kursiv gesetztes Datum seine Auszeichnung.
        /// </param>
        public void Beginn(TdCharFormat format)
        {
            _tiefe++;
            if (_tiefe != 1) return;

            _imErgebnis = false;
            _format = format;
            _anweisung.Clear();
            _ergebnis.Clear();
        }

        public void Trenner()
        {
            if (_tiefe == 1) _imErgebnis = true;
        }

        public void Anweisung(string teil)
        {
            if (_tiefe == 1 && !_imErgebnis) _anweisung.Append(teil);
        }

        /// <summary>Ein Stück aus dem Ergebnisteil — es wird nur gebraucht, wenn wir das Feld nicht kennen.</summary>
        public void Ergebnis(TdInline stueck)
        {
            if (_tiefe == 1 && _imErgebnis) _ergebnis.Add(stueck);
        }

        /// <summary>Beendet das Feld und hängt an, was davon ins Dokument gehört.</summary>
        public void Ende(List<TdInline> ziel)
        {
            if (_tiefe == 0) return;      // ein `end` ohne `begin` — fremde Datei, kein Absturz
            _tiefe--;
            if (_tiefe != 0) return;

            if (FeldAusAnweisung(_anweisung.ToString()) is { } feld)
            {
                feld.Format = _format;
                ziel.Add(feld);
            }
            else ziel.AddRange(_ergebnis);

            _anweisung.Clear();
            _ergebnis.Clear();
            _imErgebnis = false;
        }
    }

    // -------------------------------------------------------------- Lesen

    private static TdGraphic? ZeichnungLesen(W.Drawing zeichnung, Lesestand stand)
    {
        double breite = 0, hoehe = 0;
        if (zeichnung.Descendants<DW.Extent>().FirstOrDefault() is { } ausdehnung)
        {
            breite = EmuZuCm(ausdehnung.Cx?.Value ?? 0);
            hoehe = EmuZuCm(ausdehnung.Cy?.Value ?? 0);
        }

        string? alt = zeichnung.Descendants<DW.DocProperties>().FirstOrDefault()?.Description?.Value;
        if (alt is { Length: 0 }) alt = null;

        if (zeichnung.Descendants<C.ChartReference>().FirstOrDefault()?.Id?.Value is { } diagrammId
            && stand.Teil.GetPartById(diagrammId) is ChartPart { ChartSpace: { } raum }
            && raum.GetFirstChild<C.Chart>() is { } inhalt)
        {
            var d = DiagrammLesen(inhalt);
            d.WidthCm = breite;
            d.HeightCm = hoehe;
            d.AltText = alt;
            return d;
        }

        if (zeichnung.Descendants<A.Blip>().FirstOrDefault()?.Embed?.Value is { } bildId
            && stand.Teil.GetPartById(bildId) is ImagePart bildteil)
        {
            if (stand.Bilder is null)
                throw new NotSupportedException(
                    "Das Dokument enthält ein Bild, aber es wurde kein Bildspeicher mitgegeben — " +
                    "TdDocx.Lesen(quelle, bilder) benutzen (HANDOFF §4.21).");

            using var strom = bildteil.GetStream();
            using var speicher = new MemoryStream();
            strom.CopyTo(speicher);
            byte[] daten = speicher.ToArray();

            string endung = BildEndung(bildteil);
            return new TdImage(stand.Bilder.Ablegen(daten, endung), endung, breite, hoehe)
            {
                AltText = alt,
            };
        }

        // Eine Zeichnung, die weder Bild noch Diagramm ist (eine Form, ein SmartArt): Sie
        // verschwindet, und das ist ein benannter Verlust — das Modell hat dafür keinen Ort,
        // und ein leerer Kasten wäre eine Behauptung über etwas, das wir nicht kennen.
        return null;
    }

    private static TdChart DiagrammLesen(C.Chart inhalt)
    {
        var d = new TdChart();

        if (inhalt.Title?.ChartText?.RichText is { } text)
            d.Title = string.Concat(text.Descendants<A.Text>().Select(t => t.Text));

        var flaeche = inhalt.PlotArea;
        if (flaeche is null) return d;

        OpenXmlElement? gruppe = null;

        if (flaeche.GetFirstChild<C.BarChart>() is { } balken)
        {
            gruppe = balken;
            d.Kind = balken.BarDirection?.Val?.Value == C.BarDirectionValues.Bar
                ? TdChartKind.Bar
                : TdChartKind.Column;
        }
        else if (flaeche.GetFirstChild<C.PieChart>() is { } kuchen)
        {
            gruppe = kuchen;
            d.Kind = TdChartKind.Pie;
        }
        else if (flaeche.GetFirstChild<C.RadarChart>() is { } radar)
        {
            gruppe = radar;
            d.Kind = TdChartKind.Radar;
        }
        else if (flaeche.GetFirstChild<C.LineChart>() is { } linie)
        {
            gruppe = linie;

            // **Punkt, Punkt+Linie und Linie sind alle drei ein Liniendiagramm** — sie
            // unterscheiden sich darin, ob die Linie unsichtbar ist und ob es eine Marke gibt.
            var erste = linie.Elements<C.LineChartSeries>().FirstOrDefault();
            bool ohneLinie = erste?.ChartShapeProperties?.GetFirstChild<A.Outline>()
                ?.GetFirstChild<A.NoFill>() is not null;
            bool mitMarke = erste?.Marker?.Symbol?.Val?.Value is { } symbol
                            && symbol != C.MarkerStyleValues.None;

            d.Kind = ohneLinie ? TdChartKind.Scatter
                   : mitMarke ? TdChartKind.ScatterLine
                   : TdChartKind.Line;
        }

        if (gruppe is null) return d;

        var reihen = gruppe.ChildElements
            .Where(e => e is C.BarChartSeries or C.LineChartSeries or C.PieChartSeries or C.RadarChartSeries)
            .ToList();

        bool ersteReihe = true;
        foreach (var reihe in reihen)
        {
            var werte = new TdChartSeries
            {
                Name = reihe.GetFirstChild<C.SeriesText>()?.Descendants<C.NumericValue>()
                    .FirstOrDefault()?.Text ?? "",
            };

            if (reihe.GetFirstChild<C.Values>() is { } zahlen)
                foreach (var v in zahlen.Descendants<C.NumericValue>())
                    if (double.TryParse(v.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                        werte.Values.Add(z);

            // Die Kategorien stehen bei **jeder** Reihe gleich — einmal lesen genügt.
            if (ersteReihe && reihe.GetFirstChild<C.CategoryAxisData>() is { } kategorien)
                foreach (var v in kategorien.Descendants<C.NumericValue>())
                    d.Categories.Add(v.Text ?? "");

            d.Series.Add(werte);
            ersteReihe = false;
        }

        FarbenLesen(d, reihen);
        return d;
    }

    /// <summary>
    /// Die Palette zurücklesen: bei Farbe je Element aus den <c>c:dPt</c> der ersten Reihe,
    /// sonst aus je einer Reihe.
    /// <para>
    /// <b>Gelesen wird die erste Farbangabe je Element</b>, gleich ob sie an der Füllung oder
    /// an der Linie hängt — beim Punktdiagramm hat die Linie ausdrücklich **keine** Farbe, und
    /// die Marke trägt sie.
    /// </para>
    /// </summary>
    private static void FarbenLesen(TdChart d, List<OpenXmlElement> reihen)
    {
        if (reihen.Count == 0) return;

        var quellen = d.FarbeJeElement
            ? reihen[0].Elements<C.DataPoint>().Cast<OpenXmlElement>().ToList()
            : reihen;

        foreach (var quelle in quellen)
        {
            if (quelle.Descendants<A.RgbColorModelHex>().FirstOrDefault()?.Val?.Value is { } hex)
                d.Palette.Add("#" + hex.ToUpperInvariant());
        }
    }

    private static TdTable TabelleLesen(W.Table tabelle, Lesestand teil)
    {
        var t = new TdTable();

        if (tabelle.GetFirstChild<W.TableProperties>() is { } tblPr)
        {
            if (tblPr.TableBorders is { } rahmen) RahmenLesen(t.Format, rahmen);
            if (tblPr.TableCellMarginDefault is { } rand)
            {
                if (rand.TableCellLeftMargin?.Width?.Value is { } l) t.Format.CellPaddingLeftCm = TwipsZuCm(l);
                if (rand.TableCellRightMargin?.Width?.Value is { } r) t.Format.CellPaddingRightCm = TwipsZuCm(r);
                if (rand.TopMargin?.Width?.Value is { } o && double.TryParse(o, out double ov))
                    t.Format.CellPaddingTopCm = TwipsZuCm(ov);
                if (rand.BottomMargin?.Width?.Value is { } u && double.TryParse(u, out double uv))
                    t.Format.CellPaddingBottomCm = TwipsZuCm(uv);
            }
        }

        if (tabelle.GetFirstChild<W.TableGrid>() is { } raster)
            foreach (var spalte in raster.Elements<W.GridColumn>())
                if (spalte.Width?.Value is { } b && double.TryParse(b, out double bv))
                    t.ColumnWidthsCm.Add(TwipsZuCm(bv));

        foreach (var tr in tabelle.Elements<W.TableRow>()) t.Rows.Add(ZeileLesen(tr, teil));

        return t;
    }

    private static void RahmenLesen(TdTableFormat f, W.TableBorders rahmen)
    {
        if (rahmen.TopBorder is { } o) f.Top = LinieLesen(o);
        if (rahmen.LeftBorder is { } l) f.Left = LinieLesen(l);
        if (rahmen.BottomBorder is { } u) f.Bottom = LinieLesen(u);
        if (rahmen.RightBorder is { } r) f.Right = LinieLesen(r);
        if (rahmen.InsideHorizontalBorder is { } ih) f.InsideH = LinieLesen(ih);
        if (rahmen.InsideVerticalBorder is { } iv) f.InsideV = LinieLesen(iv);
    }

    private static TdBorder LinieLesen(W.BorderType b)
    {
        bool sichtbar = b.Val?.Value is { } art && art != W.BorderValues.None && art != W.BorderValues.Nil;
        double staerke = sichtbar ? (b.Size?.Value ?? 4) / 8.0 : 0;
        string farbe = b.Color?.Value is { } c && c != "auto" ? "#" + c : "#000000";
        return new TdBorder(staerke, farbe);
    }

    private static TdTableRow ZeileLesen(W.TableRow tr, Lesestand teil)
    {
        var zeile = new TdTableRow();

        if (tr.GetFirstChild<W.TableRowProperties>() is { } trPr)
        {
            zeile.IsHeader = trPr.GetFirstChild<W.TableHeader>() is not null;
            if (trPr.GetFirstChild<W.TableRowHeight>()?.Val?.Value is { } h)
                zeile.MinHeightCm = TwipsZuCm(h);
        }

        foreach (var tc in tr.Elements<W.TableCell>()) zeile.Cells.Add(ZelleLesen(tc, teil));
        return zeile;
    }

    private static TdTableCell ZelleLesen(W.TableCell tc, Lesestand teil)
    {
        var zelle = new TdTableCell();

        if (tc.TableCellProperties is { } tcPr)
        {
            zelle.ColumnSpan = tcPr.GridSpan?.Val?.Value ?? 1;

            if (tcPr.VerticalMerge is { } merge)
                // Ohne Wert heißt „Fortsetzung" — genau wie beim `<w:b/>` ohne val (§7).
                zelle.VerticalMerge = merge.Val?.Value == W.MergedCellValues.Restart
                    ? TdVerticalMerge.Restart
                    : TdVerticalMerge.Continue;

            if (tcPr.Shading?.Fill?.Value is { } fuellung && fuellung != "auto")
                zelle.Shading = "#" + fuellung;

            if (tcPr.TableCellVerticalAlignment?.Val?.Value is { } aus)
                zelle.VerticalAlign = aus == W.TableVerticalAlignmentValues.Center ? TdVAlign.Center
                                    : aus == W.TableVerticalAlignmentValues.Bottom ? TdVAlign.Bottom
                                    : TdVAlign.Top;
        }

        BloeckeLesen(tc, zelle.Blocks, teil);

        // Der Pflichtabsatz einer sonst leeren Zelle ist kein Inhalt — sonst bekäme jede
        // Fortsetzungszelle beim Lesen einen leeren Absatz dazu, und der Roundtrip wüchse
        // mit jedem Durchgang.
        if (zelle.Blocks is [TdParagraph { Inlines.Count: 0 } leer] && leer.Format.IstLeer)
            zelle.Blocks.Clear();

        return zelle;
    }

    /// <summary>
    /// Liest Absätze und Tabellen eines Behälters (hier: einer Zelle) in
    /// Dokumentreihenfolge. Der Körper geht einen eigenen Weg, weil dort zusätzlich die
    /// Abschnittsgrenzen abzulesen sind.
    /// </summary>
    private static void BloeckeLesen(OpenXmlElement behaelter, List<TdBlock> ziel, Lesestand teil)
    {
        var kinder = Inhaltskinder(behaelter);

        for (int i = 0; i < kinder.Count; i++)
        {
            if (kinder[i] is W.Table tabelle) { ziel.Add(TabelleLesen(tabelle, teil)); continue; }
            if (IstTrennabsatz(kinder, i)) continue;

            var absatz = (W.Paragraph)kinder[i];
            if (IstSeitenumbruch(absatz)) ziel.Add(new TdPageBreak());
            else ziel.Add(AbsatzLesen(absatz, teil));
        }
    }

    /// <summary>
    /// Liest das Wasserzeichen aus einem Kopfzeilenteil zurück. Die Größe steht in der
    /// VML-Stilangabe (<c>width:400pt;height:300pt</c>) — die einzige Stelle, an der dieses
    /// Format ein Maß führt.
    /// </summary>
    private static void WasserzeichenLesen(TdPageSetup seite, W.Header kopf, HeaderPart teil, Lesestand stand)
    {
        foreach (var form in kopf.Descendants<V.Shape>())
        {
            if (form.GetFirstChild<V.ImageData>() is not { } bilddaten) continue;
            if (bilddaten.RelationshipId?.Value is not { } id) continue;
            if (teil.GetPartById(id) is not ImagePart bildteil) continue;

            if (stand.Bilder is null)
                throw new NotSupportedException(
                    "Das Dokument hat ein Wasserzeichen, aber es wurde kein Bildspeicher mitgegeben — " +
                    "TdDocx.Lesen(quelle, bilder) benutzen (HANDOFF §4.21).");

            using var strom = bildteil.GetStream();
            using var speicher = new MemoryStream();
            strom.CopyTo(speicher);

            string endung = BildEndung(bildteil);
            seite.Watermark = new TdImage(
                stand.Bilder.Ablegen(speicher.ToArray(), endung), endung,
                StilmassCm(form.Style?.Value, "width"),
                StilmassCm(form.Style?.Value, "height"));
            seite.WatermarkOpacity = DeckkraftAusGain(bilddaten.Gain?.Value);
            return;
        }
    }

    private static TdPageSetup SeiteLesen(W.SectionProperties sectPr, MainDocumentPart main, Lesestand stand)
    {
        var seite = new TdPageSetup();

        if (sectPr.GetFirstChild<W.PageSize>() is { } groesse)
        {
            if (groesse.Width?.Value is { } b) seite.WidthCm = TwipsZuCm(b);
            if (groesse.Height?.Value is { } h) seite.HeightCm = TwipsZuCm(h);
        }

        if (sectPr.GetFirstChild<W.PageMargin>() is { } rand)
        {
            if (rand.Left?.Value is { } l) seite.MarginLeftCm = TwipsZuCm(l);
            if (rand.Right?.Value is { } r) seite.MarginRightCm = TwipsZuCm(r);
            if (rand.Top?.Value is { } o) seite.MarginTopCm = TwipsZuCm(o);
            if (rand.Bottom?.Value is { } u) seite.MarginBottomCm = TwipsZuCm(u);
        }

        seite.SuppressOnFirstPage = sectPr.GetFirstChild<W.TitlePage>() is not null;

        foreach (var verweis in sectPr.Elements<W.HeaderReference>())
        {
            if (verweis.Type?.Value != W.HeaderFooterValues.Default || verweis.Id?.Value is not { } id) continue;
            if (main.GetPartById(id) is HeaderPart { Header: { } kopf } kopfteil)
            {
                seite.HeaderText = KopfFussTextLesen(kopf, stand.Auf(kopfteil));
                WasserzeichenLesen(seite, kopf, kopfteil, stand);
            }
        }
        foreach (var verweis in sectPr.Elements<W.FooterReference>())
        {
            if (verweis.Type?.Value != W.HeaderFooterValues.Default || verweis.Id?.Value is not { } id) continue;
            if (main.GetPartById(id) is FooterPart { Footer: { } fuss } fussteil)
                seite.FooterText = KopfFussTextLesen(fuss, stand.Auf(fussteil));
        }

        return seite;
    }

    /// <summary>
    /// Der Weg zurück: aus den Feldern werden wieder Platzhalter. Ohne ihn käme aus einem
    /// Rückimport die beim Schreiben eingesetzte Zahl als gewöhnlicher Text — und die
    /// Kopfzeile zeigte auf jeder Seite Seite 1.
    ///
    /// <para>
    /// <b>Gelesen werden beide Feldformen.</b> Die eigene Datei hat die kurze; ein fremdes
    /// Dokument bringt die lange mit, und dann steht der Feldname in einem <c>instrText</c>
    /// mitten zwischen Läufen. Wer nur die kurze kennt, bekommt aus einer Word-Kopfzeile die
    /// Zeichenkette „PAGE" als Text.
    /// </para>
    /// </summary>
    private static string KopfFussTextLesen(OpenXmlElement kopfOderFuss, Lesestand dokumentteil)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var absatz in kopfOderFuss.Elements<W.Paragraph>())
        {
            var stuecke = new List<TdInline>();
            StueckeLesen(absatz, stuecke, dokumentteil, new Feldleser());

            foreach (var stueck in stuecke)
            {
                if (stueck is TdField feld) sb.Append(TdField.PlatzhalterVonArt(feld.Kind) ?? "");
                else sb.Append(stueck.PlainText());
            }
        }
        return sb.ToString();
    }

    // ==================== Lesen ====================

    /// <summary>
    /// Was beim Lesen gebraucht wird: der Dokumentteil, aus dem gerade gelesen wird — eine
    /// Kopfzeile führt ihre eigenen Beziehungen und Bilder —, und wohin Bilddaten gehen.
    /// </summary>
    private sealed class Lesestand(OpenXmlPart teil, ITdImages? bilder)
    {
        public OpenXmlPart Teil { get; } = teil;

        /// <inheritdoc cref="Kontext.Bilder"/>
        public ITdImages? Bilder { get; } = bilder;

        /// <summary>Derselbe Stand, aber auf einem anderen Teil (Kopf-/Fußzeile).</summary>
        public Lesestand Auf(OpenXmlPart anderer) => new(anderer, Bilder);
    }

    /// <summary>Liest ein DOCX in das eigene Modell.</summary>
    /// <param name="bilder">
    /// Wohin die Bytes eingebetteter Bilder gehen (§4.21). Ohne diese Naht wirft ein Dokument
    /// mit Bildern — ein stillschweigend übergangenes Bild wäre Datenverlust.
    /// </param>
    public static TdDocument Lesen(string pfad, ITdImages? bilder = null)
    {
        using var docx = WordprocessingDocument.Open(pfad, false);
        return Lesen(docx, bilder);
    }

    /// <inheritdoc cref="Lesen(string, ITdImages?)"/>
    public static TdDocument Lesen(Stream quelle, ITdImages? bilder = null)
    {
        using var docx = WordprocessingDocument.Open(quelle, false);
        return Lesen(docx, bilder);
    }

    private static TdDocument Lesen(WordprocessingDocument docx, ITdImages? bilder)
    {
        var doc = new TdDocument();
        var main = docx.MainDocumentPart;
        if (main?.Document?.Body is not { } body) return doc;

        StandardformateLesen(doc, main);
        ListenLesen(doc, main);

        var stand = new Lesestand(main, bilder);
        var laufend = new TdSection();
        var kinder = Inhaltskinder(body);

        for (int i = 0; i < kinder.Count; i++)
        {
            if (kinder[i] is W.Table tabelle)
            {
                laufend.Blocks.Add(TabelleLesen(tabelle, stand));
                continue;
            }

            var absatz = (W.Paragraph)kinder[i];

            // Der Trennabsatz hinter einer Tabelle ist kein Inhalt — er ist nur da, damit
            // Word das Dokument richtig liest. Er wird übersprungen, **kann aber trotzdem
            // die sectPr tragen**, wenn ein Abschnitt mit einer Tabelle endet.
            if (!IstTrennabsatz(kinder, i))
            {
                if (IstSeitenumbruch(absatz)) laufend.Blocks.Add(new TdPageBreak());
                else laufend.Blocks.Add(AbsatzLesen(absatz, stand));
            }

            // Eine sectPr **im** Absatzformat beendet den Abschnitt — sie gehört zu allem,
            // was bis hierher kam, und nicht zu dem, was folgt. Das ist die Gegenrichtung
            // zur Unsymmetrie beim Schreiben.
            if (absatz.ParagraphProperties?.SectionProperties is { } sectPr)
            {
                laufend.Page = SeiteLesen(sectPr, main, stand);
                doc.Sections.Add(laufend);
                laufend = new TdSection();
            }
        }

        // Der letzte Abschnitt trägt seine Einrichtung am Ende des Körpers.
        if (body.GetFirstChild<W.SectionProperties>() is { } letzte)
            laufend.Page = SeiteLesen(letzte, main, stand);

        // Ein DOCX endet immer mit einem Abschnitt, auch wenn er leer ist — nur ein Dokument
        // ohne jeden Absatz **und** ohne sectPr bekommt keinen.
        if (laufend.Blocks.Count > 0 || doc.Sections.Count == 0) doc.Sections.Add(laufend);

        return doc;
    }

    private static void StandardformateLesen(TdDocument doc, MainDocumentPart main)
    {
        var vorgaben = main.StyleDefinitionsPart?.Styles?.DocDefaults;
        if (vorgaben is null) return;

        if (vorgaben.RunPropertiesDefault?.RunPropertiesBaseStyle is { } rPr)
            doc.DefaultCharFormat = ZeichenformatLesen(rPr);
        if (vorgaben.ParagraphPropertiesDefault?.ParagraphPropertiesBaseStyle is { } pPr)
            doc.DefaultParaFormat = AbsatzformatLesen(pPr);
    }

    private static TdParagraph AbsatzLesen(W.Paragraph absatz, Lesestand teil)
    {
        var p = new TdParagraph();
        var pPr = absatz.ParagraphProperties;

        if (pPr is not null)
        {
            p.Format = AbsatzformatLesen(pPr);
            if (pPr.ParagraphMarkRunProperties is { } marke)
                p.CharFormat = ZeichenformatLesen(marke);

            if (pPr.NumberingProperties is { } numPr && numPr.NumberingId?.Val?.Value is { } id)
            {
                // Eine fehlende Ebene heißt 0 — Word lässt `w:ilvl` bei der obersten weg.
                p.List = new TdListRef(id, numPr.NumberingLevelReference?.Val?.Value ?? 0);
            }
        }

        StueckeLesen(absatz, p.Inlines, teil, new Feldleser());

        // **Der Gegenzug zur `unterlage` beim Schreiben** (§4.23): Jeder Lauf in DOCX trägt das
        // Format seines Absatzes mit, denn Word wendet das `pPr/rPr` nur auf die Absatzmarke
        // an. Bliebe das so stehen, käme aus jeder Word-Datei ein Modell zurück, in dem jeder
        // Lauf eine vollständige Formatkopie trägt — genau das, was §4.14 verhindert.
        EntdoppeltGegen(p.Inlines, p.CharFormat);
        return p;
    }

    /// <summary>
    /// Liest die Textstücke eines Absatzes — oder eines Verweises darin.
    /// <para>
    /// Der Durchlauf geht über **alle** Kinder und nicht nur über die Läufe: ein Verweis, ein
    /// Feld in der kurzen Form und eine Textmarke sind Geschwister des Laufs, keine Teile von
    /// ihm. Wer nur <c>w:r</c> einsammelt, verliert jeden Verweistext, ohne dass ein Test
    /// darüber stolpert — er hat ja Text bekommen, nur weniger.
    /// </para>
    /// </summary>
    private static void StueckeLesen(
        OpenXmlElement behaelter, List<TdInline> ziel, Lesestand teil, Feldleser leser)
    {
        foreach (var kind in behaelter.ChildElements)
        {
            switch (kind)
            {
                case W.Hyperlink verweis:
                {
                    var link = new TdHyperlink { Target = VerweisZielLesen(verweis, teil) };
                    StueckeLesen(verweis, link.Inlines, teil, leser);
                    if (link.Inlines.Count > 0) ziel.Add(link);
                    break;
                }

                // Die kurze Feldform — Word schreibt sie für einfache Felder, wir für Kopf-
                // und Fußzeilen.
                case W.SimpleField einfach:
                {
                    if (FeldAusAnweisung(einfach.Instruction?.Value ?? "") is { } feld) ziel.Add(feld);
                    else StueckeLesen(einfach, ziel, teil, leser);   // unbekannt: der Text bleibt
                    break;
                }

                case W.Run lauf:
                    LaufLesen(lauf, ziel, teil, leser);
                    break;

                // Textmarken sind Sprungziele und kein Inhalt. Sie werden beim Schreiben aus
                // den Gliederungsebenen neu erzeugt (§4.20) — gespeichert wären sie ein
                // zweiter Name für dieselbe Überschrift, und der erste driftet.
                case W.BookmarkStart:
                case W.BookmarkEnd:
                    break;
            }
        }
    }

    private static void LaufLesen(W.Run lauf, List<TdInline> ziel, Lesestand stand, Feldleser leser)
    {
        var format = lauf.RunProperties is { } rPr ? ZeichenformatLesen(rPr) : new TdCharFormat();

        foreach (var teil in lauf.ChildElements)
        {
            switch (teil)
            {
                case W.FieldChar marke when marke.FieldCharType?.Value is { } art:
                    if (art == W.FieldCharValues.Begin) leser.Beginn(format.Kopie());
                    else if (art == W.FieldCharValues.Separate) leser.Trenner();
                    else if (art == W.FieldCharValues.End) leser.Ende(ziel);
                    break;

                case W.FieldCode anweisung:
                    leser.Anweisung(anweisung.Text);
                    break;

                case W.Text t:
                    Anhaengen(new TdRun(t.Text, format.Kopie()));
                    break;

                // Ein Umbruch ohne Typ ist der Zeilenumbruch innerhalb des Absatzes.
                // Ein Seitenumbruch mitten im Absatz wird hier bewusst zum Zeilenumbruch:
                // das Modell kennt ihn erst ab Schritt 2 als Blockeigenschaft, und ein
                // stillschweigend verschluckter Umbruch wäre schlechter als ein sichtbarer.
                case W.Break:
                    Anhaengen(new TdLineBreak { Format = format.Kopie() });
                    break;

                case W.Drawing zeichnung when ZeichnungLesen(zeichnung, stand) is { } grafik:
                    grafik.Format = format.Kopie();
                    Anhaengen(grafik);
                    break;
            }
        }

        void Anhaengen(TdInline stueck)
        {
            if (leser.Aktiv) leser.Ergebnis(stueck);
            else ziel.Add(stueck);
        }
    }

    /// <summary>
    /// Das Ziel eines Verweises. Ein Anker zeigt ins eigene Dokument und bekommt das
    /// <c>#</c> zurück; alles andere kommt aus der Beziehung — und zwar als
    /// <c>OriginalString</c>, damit ein relatives Ziel relativ bleibt (§7).
    /// </summary>
    private static string VerweisZielLesen(W.Hyperlink verweis, Lesestand teil)
    {
        if (verweis.Anchor?.Value is { Length: > 0 } anker) return "#" + anker;
        if (verweis.Id?.Value is not { } id) return "";

        foreach (var beziehung in teil.Teil.HyperlinkRelationships)
            if (beziehung.Id == id) return beziehung.Uri.OriginalString;

        // Eine Beziehung, die es nicht gibt, kommt aus einer beschädigten Datei. Der Linktext
        // bleibt trotzdem stehen — ein Verweis ohne Ziel ist besser als ein verlorener Satz.
        return "";
    }

    private static TdCharFormat ZeichenformatLesen(OpenXmlElement rPr)
    {
        var f = new TdCharFormat();

        if (rPr.GetFirstChild<W.RunFonts>()?.Ascii?.Value is { } schrift) f.FontFamily = schrift;
        // Ein <w:b/> **ohne** val bedeutet „an" — das ist die Stelle, an der ein naives
        // `Val?.Value ?? false` jede fette Stelle stillschweigend normal machen würde.
        if (rPr.GetFirstChild<W.Bold>() is { } b) f.Bold = b.Val?.Value ?? true;
        if (rPr.GetFirstChild<W.Italic>() is { } i) f.Italic = i.Val?.Value ?? true;
        if (rPr.GetFirstChild<W.Strike>() is { } s) f.Strikethrough = s.Val?.Value ?? true;
        if (rPr.GetFirstChild<W.Color>()?.Val?.Value is { } farbe) f.Color = "#" + farbe;
        if (rPr.GetFirstChild<W.FontSize>()?.Val?.Value is { } groesse && double.TryParse(groesse, out double halbe))
            f.FontSize = HalbePunktZuPt(halbe);
        if (rPr.GetFirstChild<W.Underline>() is { } u)
            f.Underline = u.Val is not null && u.Val.Value != W.UnderlineValues.None;
        if (rPr.GetFirstChild<W.Shading>()?.Fill?.Value is { } fuellung)
            f.Highlight = fuellung == "auto" ? "" : "#" + fuellung;
        if (rPr.GetFirstChild<W.VerticalTextAlignment>()?.Val?.Value is { } hoch)
            f.VerticalAlign = hoch == W.VerticalPositionValues.Superscript ? TdVerticalAlign.Superscript
                            : hoch == W.VerticalPositionValues.Subscript ? TdVerticalAlign.Subscript
                            : TdVerticalAlign.Normal;

        return f;
    }

    private static TdParaFormat AbsatzformatLesen(OpenXmlElement pPr)
    {
        var f = new TdParaFormat();

        if (pPr.GetFirstChild<W.KeepNext>() is { } k) f.KeepWithNext = k.Val?.Value ?? true;
        if (pPr.GetFirstChild<W.PageBreakBefore>() is { } pb) f.PageBreakBefore = pb.Val?.Value ?? true;

        if (pPr.GetFirstChild<W.ParagraphBorders>()?.GetFirstChild<W.BottomBorder>() is { } linie)
            f.BottomBorder = LinieLesen(linie);

        if (pPr.GetFirstChild<W.SpacingBetweenLines>() is { } abstand)
        {
            if (abstand.Before?.Value is { } vor && double.TryParse(vor, out double v)) f.SpaceBeforePt = TwipsZuPt(v);
            if (abstand.After?.Value is { } nach && double.TryParse(nach, out double n)) f.SpaceAfterPt = TwipsZuPt(n);
            if (abstand.Line?.Value is { } zeile && double.TryParse(zeile, out double z))
                f.LineSpacing = z / EinheitenProZeile;
        }

        if (pPr.GetFirstChild<W.Indentation>() is { } einzug)
        {
            if (einzug.Left?.Value is { } l && double.TryParse(l, out double lv)) f.LeftIndentCm = TwipsZuCm(lv);
            if (einzug.Right?.Value is { } r && double.TryParse(r, out double rv)) f.RightIndentCm = TwipsZuCm(rv);
            if (einzug.FirstLine?.Value is { } e && double.TryParse(e, out double ev)) f.FirstLineIndentCm = TwipsZuCm(ev);
            else if (einzug.Hanging?.Value is { } h && double.TryParse(h, out double hv)) f.FirstLineIndentCm = -TwipsZuCm(hv);
        }

        if (pPr.GetFirstChild<W.Justification>()?.Val?.Value is { } aus)
            f.Alignment = aus == W.JustificationValues.Center ? TdAlign.Center
                        : aus == W.JustificationValues.Right ? TdAlign.Right
                        : aus == W.JustificationValues.Both ? TdAlign.Justify
                        : TdAlign.Left;

        if (pPr.GetFirstChild<W.OutlineLevel>()?.Val?.Value is { } ebene)
            f.OutlineLevel = ebene == 9 ? 0 : ebene + 1;

        // **Der Rang steht in der Vorlage, das Verzeichnis in der Gliederungsebene** (§4.23).
        // Ein Absatz mit Überschriftvorlage, dessen Ebene auf Fließtext steht, ist genau der
        // Fall „sieht aus wie eine Überschrift, gehört aber nicht ins Verzeichnis" — Titel und
        // die Zeile „Inhaltsverzeichnis". Ohne diesen Zweig käme der Titel als Fließtext zurück
        // und verlöre im Markdown seine Raute.
        if (VorlagenEbene(pPr.GetFirstChild<W.ParagraphStyleId>()?.Val?.Value) is { } rang)
        {
            if (f.OutlineLevel is null or 0)
            {
                f.OutlineLevel = rang;
                f.ExcludeFromToc = true;
            }
        }

        return f;
    }
}
