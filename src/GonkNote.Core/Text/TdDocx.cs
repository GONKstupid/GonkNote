using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
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
/// Listen (Schritt 3), Tabellen (Schritt 4), Felder und Inhaltsverzeichnis (Schritt 5),
/// Bilder und Diagramme (Schritt 6). Seiteneinrichtung, Kopf-/Fußzeile und Wasserzeichen
/// kommen mit dem Seitenumbruch (Schritt 2) dazu — sie hängen an <c>TextDoc</c> und nicht
/// am Inhalt.
/// </para>
///
/// <para>
/// <b>Das ist nicht der Ersatz für <c>DocxExporter</c>/<c>DocxImporter</c></b> im WPF-Kopf.
/// Die laufen weiter und bedienen die App; sie werden abgelöst, wenn dieses Modell alles
/// kann, was sie können — „danach umverdrahten" in Roadmap §5. Bis dahin wäre es die Falle
/// aus HANDOFF §4.10, sie parallel zu pflegen, und genau deshalb steht hier nur, was das
/// Modell heute wirklich trägt.
/// </para>
/// </summary>
public static class TdDocx
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

    /// <summary>Schreibt das Dokument als DOCX an <paramref name="pfad"/>.</summary>
    public static void Schreiben(TdDocument doc, string pfad)
    {
        using var docx = WordprocessingDocument.Create(pfad, WordprocessingDocumentType.Document);
        Fuellen(doc, docx);
    }

    /// <summary>Schreibt das Dokument als DOCX in einen Strom.</summary>
    public static void Schreiben(TdDocument doc, Stream ziel)
    {
        using var docx = WordprocessingDocument.Create(ziel, WordprocessingDocumentType.Document);
        Fuellen(doc, docx);
    }

    private static void Fuellen(TdDocument doc, WordprocessingDocument docx)
    {
        var main = docx.AddMainDocumentPart();
        main.Document = new W.Document(new W.Body());
        var body = main.Document.Body!;

        StandardformateSchreiben(doc, main);
        ListenSchreiben(doc, main);

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
                        letzterAbsatz = AbsatzSchreiben(p);
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
                        teile.Add(TabelleSchreiben(t));
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

            var sectPr = SeiteSchreiben(abschnitt.Page, main);

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

    /// <summary>
    /// Die Grundformate des Dokuments landen in <c>docDefaults</c> — dort, wo Word sie auch
    /// erwartet. Damit bleibt die Kaskade im DOCX erhalten und wird nicht in jeden Absatz
    /// hineinkopiert.
    /// </summary>
    private static void StandardformateSchreiben(TdDocument doc, MainDocumentPart main)
    {
        var teil = main.AddNewPart<StyleDefinitionsPart>();

        var rDefault = new W.RunPropertiesDefault();
        var rPr = ZeichenformatSchreiben(doc.DefaultCharFormat);
        if (rPr.HasChildren) rDefault.AppendChild(new W.RunPropertiesBaseStyle(rPr.ChildElements.Select(c => c.CloneNode(true))));

        var pDefault = new W.ParagraphPropertiesDefault();
        var pPr = AbsatzformatSchreiben(doc.DefaultParaFormat);
        if (pPr.HasChildren) pDefault.AppendChild(new W.ParagraphPropertiesBaseStyle(pPr.ChildElements.Select(c => c.CloneNode(true))));

        // Reihenfolge im Schema: rPrDefault vor pPrDefault.
        teil.Styles = new W.Styles(new W.DocDefaults(rDefault, pDefault));
        teil.Styles.AppendChild(new W.Style(new W.StyleName { Val = "Normal" })
        {
            Type = W.StyleValues.Paragraph,
            StyleId = "Normal",
            Default = true,
        });
    }

    // ==================== Listen ====================

    /// <summary>
    /// Die Listendefinitionen als <c>numbering.xml</c>.
    /// <para>
    /// DOCX trennt hier zwei Dinge, die man leicht verwechselt: ein <c>abstractNum</c> ist
    /// die **Vorlage** (wie sehen die neun Ebenen aus), ein <c>num</c> ist eine **Instanz**
    /// davon — und nur die hat eine Kennung, auf die ein Absatz zeigt. Zwei Listen, die
    /// gleich aussehen, aber getrennt zählen, sind zwei <c>num</c> auf dasselbe
    /// <c>abstractNum</c>. Hier bekommt jede Definition beides, weil jede Liste im Modell
    /// ohnehin ihre eigene ist.
    /// </para>
    /// </summary>
    private static void ListenSchreiben(TdDocument doc, MainDocumentPart main)
    {
        if (doc.Lists.Count == 0) return;

        var nummerierung = new W.Numbering();

        // Schema-Reihenfolge: **erst alle abstractNum, dann alle num.** Verschachtelt
        // geschrieben ergibt das eine Datei, die Word nicht öffnet.
        foreach (var liste in doc.Lists)
        {
            var vorlage = new W.AbstractNum { AbstractNumberId = liste.Id };
            vorlage.AppendChild(new W.MultiLevelType { Val = W.MultiLevelValues.HybridMultilevel });

            for (int i = 0; i < liste.Levels.Count; i++)
            {
                var ebene = liste.Levels[i];
                var lvl = new W.Level { LevelIndex = i };

                // Schema-Reihenfolge in w:lvl: start, numFmt, lvlText, lvlJc, pPr.
                lvl.AppendChild(new W.StartNumberingValue { Val = ebene.Start });
                lvl.AppendChild(new W.NumberingFormat { Val = NachDocx(ebene.Marker) });
                lvl.AppendChild(new W.LevelText { Val = ebene.Text });
                lvl.AppendChild(new W.LevelJustification { Val = W.LevelJustificationValues.Left });
                lvl.AppendChild(new W.PreviousParagraphProperties(new W.Indentation
                {
                    Left = CmZuTwips(ebene.IndentCm).ToString(),
                    Hanging = CmZuTwips(ebene.HangingCm).ToString(),
                }));

                vorlage.AppendChild(lvl);
            }
            nummerierung.AppendChild(vorlage);
        }

        foreach (var liste in doc.Lists)
        {
            nummerierung.AppendChild(new W.NumberingInstance(
                new W.AbstractNumId { Val = liste.Id })
            {
                NumberID = liste.Id,
            });
        }

        main.AddNewPart<NumberingDefinitionsPart>().Numbering = nummerierung;
    }

    private static W.NumberFormatValues NachDocx(TdListMarker marke) => marke switch
    {
        TdListMarker.Decimal => W.NumberFormatValues.Decimal,
        TdListMarker.LowerLetter => W.NumberFormatValues.LowerLetter,
        TdListMarker.UpperLetter => W.NumberFormatValues.UpperLetter,
        TdListMarker.LowerRoman => W.NumberFormatValues.LowerRoman,
        TdListMarker.UpperRoman => W.NumberFormatValues.UpperRoman,
        _ => W.NumberFormatValues.Bullet,
    };

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

    private static W.Paragraph AbsatzSchreiben(TdParagraph p)
    {
        var absatz = new W.Paragraph();

        var pPr = AbsatzformatSchreiben(p.Format);

        // Schema-Reihenfolge in CT_PPr: numPr steht **nach** pageBreakBefore und **vor**
        // spacing. AbsatzformatSchreiben hat beides schon gesetzt, also wird hier
        // eingefügt statt angehängt.
        if (p.List is { } verweis)
        {
            var numPr = new W.NumberingProperties(
                new W.NumberingLevelReference { Val = verweis.Level },
                new W.NumberingId { Val = verweis.ListId });

            OpenXmlElement? davor =
                pPr.GetFirstChild<W.SpacingBetweenLines>() as OpenXmlElement
                ?? pPr.GetFirstChild<W.Indentation>() as OpenXmlElement
                ?? pPr.GetFirstChild<W.Justification>() as OpenXmlElement
                ?? pPr.GetFirstChild<W.OutlineLevel>();

            if (davor is null) pPr.AppendChild(numPr);
            else pPr.InsertBefore(numPr, davor);
        }
        // Das Zeichenformat des **ganzen** Absatzes steht in DOCX im pPr/rPr — nicht an
        // jedem Lauf. Genau so bleibt eine Überschrift änderbar, ohne jeden Lauf anzufassen.
        var absatzZeichen = ZeichenformatSchreiben(p.CharFormat);
        if (absatzZeichen.HasChildren)
            pPr.AppendChild(new W.ParagraphMarkRunProperties(absatzZeichen.ChildElements.Select(c => c.CloneNode(true))));
        if (pPr.HasChildren) absatz.AppendChild(pPr);

        foreach (var inline in p.Inlines)
        {
            var lauf = new W.Run();
            var rPr = ZeichenformatSchreiben(inline.Format);
            if (rPr.HasChildren) lauf.AppendChild(rPr);

            switch (inline)
            {
                case TdRun r:
                    // Space="preserve": ohne das fielen führende und mehrfache Leerzeichen weg,
                    // und der Text säße nach dem Roundtrip zusammengeschoben da.
                    lauf.AppendChild(new W.Text(r.Text) { Space = SpaceProcessingModeValues.Preserve });
                    break;

                case TdLineBreak:
                    lauf.AppendChild(new W.Break());
                    break;

                default:
                    throw new NotSupportedException(
                        $"{inline.GetType().Name} kann noch nicht nach DOCX — siehe die Reihenfolge in Roadmap §5.");
            }

            absatz.AppendChild(lauf);
        }
        return absatz;
    }

    /// <summary>
    /// Ein erzwungener Seitenumbruch ist in DOCX ein Absatz, dessen einziger Lauf einen
    /// Umbruch vom Typ „page" enthält — es gibt dafür keinen eigenen Blocktyp.
    /// </summary>
    private static W.Paragraph SeitenumbruchSchreiben() =>
        new(new W.Run(new W.Break { Type = W.BreakValues.Page }));

    /// <summary>
    /// Ein leerer Absatz, der nur da ist, damit Word das Dokument richtig liest — zwischen
    /// zwei Tabellen und hinter der letzten. Er trägt eine Kennung, damit der Leser ihn
    /// wieder herausnehmen kann und der Roundtrip nicht mit jedem Durchgang wächst.
    /// </summary>
    private static W.Paragraph TrennabsatzSchreiben() => new();

    // ==================== Tabellen ====================

    private static W.Table TabelleSchreiben(TdTable t)
    {
        var tabelle = new W.Table();

        // Schema-Reihenfolge in CT_TblPr: tblW, tblBorders, tblCellMar.
        var tblPr = new W.TableProperties();
        tblPr.AppendChild(new W.TableWidth { Width = "0", Type = W.TableWidthUnitValues.Auto });
        tblPr.AppendChild(RahmenSchreiben(t.Format));
        tblPr.AppendChild(new W.TableCellMarginDefault(
            new W.TopMargin { Width = CmZuTwips(t.Format.CellPaddingTopCm).ToString(), Type = W.TableWidthUnitValues.Dxa },
            new W.TableCellLeftMargin { Width = (short)CmZuTwips(t.Format.CellPaddingLeftCm), Type = W.TableWidthValues.Dxa },
            new W.BottomMargin { Width = CmZuTwips(t.Format.CellPaddingBottomCm).ToString(), Type = W.TableWidthUnitValues.Dxa },
            new W.TableCellRightMargin { Width = (short)CmZuTwips(t.Format.CellPaddingRightCm), Type = W.TableWidthValues.Dxa }));
        tabelle.AppendChild(tblPr);

        // Das Raster. **Es steht einmal für die ganze Tabelle** und nicht je Zeile.
        var raster = new W.TableGrid();
        int spalten = t.Spaltenzahl();
        for (int i = 0; i < spalten; i++)
        {
            double breite = i < t.ColumnWidthsCm.Count ? t.ColumnWidthsCm[i] : 0;
            raster.AppendChild(new W.GridColumn { Width = CmZuTwips(breite).ToString() });
        }
        tabelle.AppendChild(raster);

        foreach (var zeile in t.Rows) tabelle.AppendChild(ZeileSchreiben(zeile, t));

        return tabelle;
    }

    private static W.TableBorders RahmenSchreiben(TdTableFormat f)
    {
        // Schema-Reihenfolge: top, left, bottom, right, insideH, insideV.
        var rahmen = new W.TableBorders();
        rahmen.AppendChild(Linie<W.TopBorder>(f.Top));
        rahmen.AppendChild(Linie<W.LeftBorder>(f.Left));
        rahmen.AppendChild(Linie<W.BottomBorder>(f.Bottom));
        rahmen.AppendChild(Linie<W.RightBorder>(f.Right));
        rahmen.AppendChild(Linie<W.InsideHorizontalBorder>(f.InsideH));
        rahmen.AppendChild(Linie<W.InsideVerticalBorder>(f.InsideV));
        return rahmen;
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

    private static W.TableRow ZeileSchreiben(TdTableRow zeile, TdTable t)
    {
        var tr = new W.TableRow();

        if (zeile.IsHeader || zeile.MinHeightCm is not null)
        {
            // Schema-Reihenfolge in CT_TrPr: trHeight vor tblHeader.
            var trPr = new W.TableRowProperties();
            if (zeile.MinHeightCm is { } hoehe)
                trPr.AppendChild(new W.TableRowHeight
                {
                    Val = (uint)CmZuTwips(hoehe),
                    HeightType = W.HeightRuleValues.AtLeast,
                });
            if (zeile.IsHeader) trPr.AppendChild(new W.TableHeader());
            tr.AppendChild(trPr);
        }

        int spalte = 0;
        foreach (var zelle in zeile.Cells)
        {
            tr.AppendChild(ZelleSchreiben(zelle, t, spalte));
            spalte += Math.Max(1, zelle.ColumnSpan);
        }
        return tr;
    }

    private static W.TableCell ZelleSchreiben(TdTableCell zelle, TdTable t, int abSpalte)
    {
        var tc = new W.TableCell();

        // Schema-Reihenfolge in CT_TcPr: tcW, gridSpan, vMerge, shd, vAlign.
        var tcPr = new W.TableCellProperties();

        double breite = 0;
        for (int i = 0; i < Math.Max(1, zelle.ColumnSpan); i++)
            if (abSpalte + i < t.ColumnWidthsCm.Count) breite += t.ColumnWidthsCm[abSpalte + i];

        tcPr.AppendChild(new W.TableCellWidth
        {
            Width = CmZuTwips(breite).ToString(),
            Type = breite > 0 ? W.TableWidthUnitValues.Dxa : W.TableWidthUnitValues.Auto,
        });

        if (zelle.ColumnSpan > 1) tcPr.AppendChild(new W.GridSpan { Val = zelle.ColumnSpan });

        if (zelle.VerticalMerge != TdVerticalMerge.None)
        {
            // **Eine Fortsetzung ist ein `vMerge` ohne Wert** — nicht eines mit „continue".
            // Word schreibt es so, und ein Wert, den das Schema nicht kennt, macht die Datei
            // unlesbar.
            tcPr.AppendChild(zelle.VerticalMerge == TdVerticalMerge.Restart
                ? new W.VerticalMerge { Val = W.MergedCellValues.Restart }
                : new W.VerticalMerge());
        }

        if (zelle.Shading is { } farbe)
            tcPr.AppendChild(new W.Shading
            {
                Val = W.ShadingPatternValues.Clear,
                Color = "auto",
                Fill = farbe.Length == 0 ? "auto" : farbe.TrimStart('#'),
            });

        if (zelle.VerticalAlign != TdVAlign.Top)
            tcPr.AppendChild(new W.TableCellVerticalAlignment
            {
                Val = zelle.VerticalAlign == TdVAlign.Center
                    ? W.TableVerticalAlignmentValues.Center
                    : W.TableVerticalAlignmentValues.Bottom,
            });

        tc.AppendChild(tcPr);

        bool hatAbsatz = false;
        foreach (var block in zelle.Blocks)
        {
            switch (block)
            {
                case TdParagraph p: tc.AppendChild(AbsatzSchreiben(p)); hatAbsatz = true; break;
                case TdPageBreak: tc.AppendChild(SeitenumbruchSchreiben()); hatAbsatz = true; break;

                // **Eine Tabelle in einer Tabelle** ist erlaubt und braucht danach einen
                // Absatz — dieselbe Regel wie im Körper.
                case TdTable innen:
                    tc.AppendChild(TabelleSchreiben(innen));
                    tc.AppendChild(TrennabsatzSchreiben());
                    hatAbsatz = true;
                    break;

                default:
                    throw new NotSupportedException(
                        $"{block.GetType().Name} kann noch nicht in eine Tabellenzelle — siehe Roadmap §5.");
            }
        }

        // **Eine Zelle ohne Absatz ist schemawidrig.** Sie kommt zwangsläufig vor: eine
        // Fortsetzungszelle trägt keinen Inhalt.
        if (!hatAbsatz) tc.AppendChild(new W.Paragraph());

        return tc;
    }

    private static TdTable TabelleLesen(W.Table tabelle)
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

        foreach (var tr in tabelle.Elements<W.TableRow>()) t.Rows.Add(ZeileLesen(tr));

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

    private static TdTableRow ZeileLesen(W.TableRow tr)
    {
        var zeile = new TdTableRow();

        if (tr.GetFirstChild<W.TableRowProperties>() is { } trPr)
        {
            zeile.IsHeader = trPr.GetFirstChild<W.TableHeader>() is not null;
            if (trPr.GetFirstChild<W.TableRowHeight>()?.Val?.Value is { } h)
                zeile.MinHeightCm = TwipsZuCm(h);
        }

        foreach (var tc in tr.Elements<W.TableCell>()) zeile.Cells.Add(ZelleLesen(tc));
        return zeile;
    }

    private static TdTableCell ZelleLesen(W.TableCell tc)
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

        BloeckeLesen(tc, zelle.Blocks);

        // Der Pflichtabsatz einer sonst leeren Zelle ist kein Inhalt — sonst bekäme jede
        // Fortsetzungszelle beim Lesen einen leeren Absatz dazu, und der Roundtrip wüchse
        // mit jedem Durchgang.
        if (zelle.Blocks is [TdParagraph { Inlines.Count: 0 } leer] && leer.Format.IstLeer)
            zelle.Blocks.Clear();

        return zelle;
    }

    /// <summary>Absätze und Tabellen eines Behälters, in Dokumentreihenfolge.</summary>
    private static List<OpenXmlElement> Inhaltskinder(OpenXmlElement behaelter) =>
        [.. behaelter.ChildElements.Where(k => k is W.Paragraph or W.Table)];

    /// <summary>
    /// Liest Absätze und Tabellen eines Behälters (hier: einer Zelle) in
    /// Dokumentreihenfolge. Der Körper geht einen eigenen Weg, weil dort zusätzlich die
    /// Abschnittsgrenzen abzulesen sind.
    /// </summary>
    private static void BloeckeLesen(OpenXmlElement behaelter, List<TdBlock> ziel)
    {
        var kinder = Inhaltskinder(behaelter);

        for (int i = 0; i < kinder.Count; i++)
        {
            if (kinder[i] is W.Table tabelle) { ziel.Add(TabelleLesen(tabelle)); continue; }
            if (IstTrennabsatz(kinder, i)) continue;

            var absatz = (W.Paragraph)kinder[i];
            if (IstSeitenumbruch(absatz)) ziel.Add(new TdPageBreak());
            else ziel.Add(AbsatzLesen(absatz));
        }
    }

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
        if (absatz.Elements<W.Run>().Any()) return false;
        if (absatz.ParagraphProperties is not { } pPr) return true;

        return pPr.ChildElements.All(k => k is W.SectionProperties);
    }

    private static W.RunProperties ZeichenformatSchreiben(TdCharFormat f)
    {
        // **Die Reihenfolge ist Schema und keine Geschmacksfrage** (CT_RPr): rFonts, b, i,
        // strike, color, sz, u, shd, vertAlign. Wer sie vertauscht, bekommt kein kaputtes
        // Bild, sondern eine Datei, die Word nicht öffnet — deshalb prüft der Wächter
        // zusätzlich mit dem OpenXmlValidator.
        var rPr = new W.RunProperties();

        if (f.FontFamily is { } schrift)
            rPr.AppendChild(new W.RunFonts { Ascii = schrift, HighAnsi = schrift });
        if (f.Bold is { } fett) rPr.AppendChild(new W.Bold { Val = fett });
        if (f.Italic is { } kursiv) rPr.AppendChild(new W.Italic { Val = kursiv });
        if (f.Strikethrough is { } durch) rPr.AppendChild(new W.Strike { Val = durch });
        if (f.Color is { } farbe) rPr.AppendChild(new W.Color { Val = farbe.TrimStart('#') });
        if (f.FontSize is { } groesse) rPr.AppendChild(new W.FontSize { Val = PtZuHalbePunkt(groesse) });
        if (f.Underline is { } unter)
            rPr.AppendChild(new W.Underline { Val = unter ? W.UnderlineValues.Single : W.UnderlineValues.None });
        if (f.Highlight is { } hervor)
            rPr.AppendChild(new W.Shading
            {
                Val = W.ShadingPatternValues.Clear,
                Color = "auto",
                // Leerer Text heißt „ausdrücklich keine Hervorhebung" und ist etwas anderes
                // als „nicht gesetzt" — in DOCX ist das die Füllung „auto".
                Fill = hervor.Length == 0 ? "auto" : hervor.TrimStart('#'),
            });
        if (f.VerticalAlign is { } hoch)
            rPr.AppendChild(new W.VerticalTextAlignment { Val = hoch switch
            {
                TdVerticalAlign.Superscript => W.VerticalPositionValues.Superscript,
                TdVerticalAlign.Subscript => W.VerticalPositionValues.Subscript,
                _ => W.VerticalPositionValues.Baseline,
            } });

        return rPr;
    }

    private static W.ParagraphProperties AbsatzformatSchreiben(TdParaFormat f)
    {
        // Schema-Reihenfolge (CT_PPr): keepNext, pageBreakBefore, spacing, ind, jc, outlineLvl.
        var pPr = new W.ParagraphProperties();

        if (f.KeepWithNext is { } halten) pPr.AppendChild(new W.KeepNext { Val = halten });
        if (f.PageBreakBefore is { } umbruch) pPr.AppendChild(new W.PageBreakBefore { Val = umbruch });

        if (f.SpaceBeforePt is not null || f.SpaceAfterPt is not null || f.LineSpacing is not null)
        {
            var abstand = new W.SpacingBetweenLines();
            if (f.SpaceBeforePt is { } vor) abstand.Before = PtZuTwips(vor).ToString();
            if (f.SpaceAfterPt is { } nach) abstand.After = PtZuTwips(nach).ToString();
            if (f.LineSpacing is { } zeile)
            {
                abstand.Line = ((int)Math.Round(zeile * EinheitenProZeile)).ToString();
                abstand.LineRule = W.LineSpacingRuleValues.Auto;
            }
            pPr.AppendChild(abstand);
        }

        if (f.LeftIndentCm is not null || f.RightIndentCm is not null || f.FirstLineIndentCm is not null)
        {
            var einzug = new W.Indentation();
            if (f.LeftIndentCm is { } links) einzug.Left = CmZuTwips(links).ToString();
            if (f.RightIndentCm is { } rechts) einzug.Right = CmZuTwips(rechts).ToString();
            if (f.FirstLineIndentCm is { } erste)
            {
                // DOCX kennt zwei Felder statt eines Vorzeichens: firstLine zieht ein,
                // hanging zieht heraus. Beide sind positiv.
                if (erste >= 0) einzug.FirstLine = CmZuTwips(erste).ToString();
                else einzug.Hanging = CmZuTwips(-erste).ToString();
            }
            pPr.AppendChild(einzug);
        }

        if (f.Alignment is { } ausrichtung)
            pPr.AppendChild(new W.Justification { Val = ausrichtung switch
            {
                TdAlign.Center => W.JustificationValues.Center,
                TdAlign.Right => W.JustificationValues.Right,
                TdAlign.Justify => W.JustificationValues.Both,
                _ => W.JustificationValues.Left,
            } });

        if (f.OutlineLevel is { } ebene)
            // Word zählt ab 0 und benutzt 9 für Fließtext — die eigene 0 ist genau das.
            pPr.AppendChild(new W.OutlineLevel { Val = ebene == 0 ? 9 : ebene - 1 });

        return pPr;
    }

    // ==================== Seiteneinrichtung ====================

    private static W.SectionProperties SeiteSchreiben(TdPageSetup seite, MainDocumentPart main)
    {
        var sectPr = new W.SectionProperties();

        // Schema-Reihenfolge (CT_SectPr): die Verweise auf Kopf-/Fußzeile stehen **vor**
        // pgSz und pgMar.
        if (seite.HeaderText.Length > 0)
        {
            var teil = main.AddNewPart<HeaderPart>();
            teil.Header = new W.Header(KopfFussAbsatzSchreiben(seite.HeaderText));
            sectPr.AppendChild(new W.HeaderReference
            {
                Type = W.HeaderFooterValues.Default,
                Id = main.GetIdOfPart(teil),
            });
        }
        if (seite.FooterText.Length > 0)
        {
            var teil = main.AddNewPart<FooterPart>();
            teil.Footer = new W.Footer(KopfFussAbsatzSchreiben(seite.FooterText));
            sectPr.AppendChild(new W.FooterReference
            {
                Type = W.HeaderFooterValues.Default,
                Id = main.GetIdOfPart(teil),
            });
        }

        sectPr.AppendChild(new W.PageSize
        {
            Width = (uint)CmZuTwips(seite.WidthCm),
            Height = (uint)CmZuTwips(seite.HeightCm),
            // Word leitet die Ausrichtung **nicht** aus den Maßen ab: ohne orient dreht es
            // ein quer eingetragenes Blatt beim Drucken wieder hoch.
            Orient = seite.IstQuerformat ? W.PageOrientationValues.Landscape : W.PageOrientationValues.Portrait,
        });

        sectPr.AppendChild(new W.PageMargin
        {
            Left = (uint)CmZuTwips(seite.MarginLeftCm),
            Right = (uint)CmZuTwips(seite.MarginRightCm),
            Top = CmZuTwips(seite.MarginTopCm),
            Bottom = CmZuTwips(seite.MarginBottomCm),
            Header = 0,
            Footer = 0,
            Gutter = 0,
        });

        if (seite.SuppressOnFirstPage) sectPr.AppendChild(new W.TitlePage());

        return sectPr;
    }

    /// <summary>
    /// Eine Kopf- oder Fußzeile. <c>{SEITE}</c> und <c>{SEITEN}</c> werden zu **echten**
    /// Word-Feldern (PAGE, NUMPAGES) — als bloßer Text stünde in einem exportierten Dokument
    /// auf jeder Seite dieselbe Zahl. <c>{DATUM}</c> und <c>{TITEL}</c> bleiben vorerst
    /// wörtlich stehen; sie werden mit den Feldern in Schritt 5 nachgezogen.
    /// </summary>
    private static W.Paragraph KopfFussAbsatzSchreiben(string vorlage)
    {
        var absatz = new W.Paragraph();

        foreach (string teil in ZerlegtNachPlatzhaltern(vorlage))
        {
            if (teil.Length == 0) continue;

            if (teil is "{SEITE}" or "{SEITEN}")
            {
                absatz.AppendChild(new W.SimpleField(new W.Run(new W.Text("1")))
                {
                    Instruction = teil == "{SEITE}" ? " PAGE " : " NUMPAGES ",
                });
            }
            else
            {
                absatz.AppendChild(new W.Run(
                    new W.Text(teil) { Space = SpaceProcessingModeValues.Preserve }));
            }
        }
        return absatz;
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

    private static TdPageSetup SeiteLesen(W.SectionProperties sectPr, MainDocumentPart main)
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
            if (main.GetPartById(id) is HeaderPart { Header: { } kopf })
                seite.HeaderText = KopfFussTextLesen(kopf);
        }
        foreach (var verweis in sectPr.Elements<W.FooterReference>())
        {
            if (verweis.Type?.Value != W.HeaderFooterValues.Default || verweis.Id?.Value is not { } id) continue;
            if (main.GetPartById(id) is FooterPart { Footer: { } fuss })
                seite.FooterText = KopfFussTextLesen(fuss);
        }

        return seite;
    }

    /// <summary>
    /// Der Weg zurück: PAGE- und NUMPAGES-Felder werden wieder zu <c>{SEITE}</c> und
    /// <c>{SEITEN}</c>. Ohne das käme aus einem Rückimport die beim Schreiben eingesetzte
    /// „1" als gewöhnlicher Text — und die Kopfzeile zeigte auf jeder Seite Seite 1.
    /// </summary>
    private static string KopfFussTextLesen(OpenXmlElement kopfOderFuss)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var absatz in kopfOderFuss.Elements<W.Paragraph>())
        {
            foreach (var teil in absatz.ChildElements)
            {
                switch (teil)
                {
                    case W.SimpleField feld:
                        string anweisung = feld.Instruction?.Value ?? "";
                        sb.Append(anweisung.Contains("NUMPAGES", StringComparison.OrdinalIgnoreCase) ? "{SEITEN}"
                                : anweisung.Contains("PAGE", StringComparison.OrdinalIgnoreCase) ? "{SEITE}"
                                : "");
                        break;

                    case W.Run lauf:
                        foreach (var t in lauf.Elements<W.Text>()) sb.Append(t.Text);
                        break;
                }
            }
        }
        return sb.ToString();
    }

    // ==================== Lesen ====================

    /// <summary>Liest ein DOCX in das eigene Modell.</summary>
    public static TdDocument Lesen(string pfad)
    {
        using var docx = WordprocessingDocument.Open(pfad, false);
        return Lesen(docx);
    }

    /// <summary>Liest ein DOCX aus einem Strom.</summary>
    public static TdDocument Lesen(Stream quelle)
    {
        using var docx = WordprocessingDocument.Open(quelle, false);
        return Lesen(docx);
    }

    private static TdDocument Lesen(WordprocessingDocument docx)
    {
        var doc = new TdDocument();
        var main = docx.MainDocumentPart;
        if (main?.Document?.Body is not { } body) return doc;

        StandardformateLesen(doc, main);
        ListenLesen(doc, main);

        var laufend = new TdSection();
        var kinder = Inhaltskinder(body);

        for (int i = 0; i < kinder.Count; i++)
        {
            if (kinder[i] is W.Table tabelle)
            {
                laufend.Blocks.Add(TabelleLesen(tabelle));
                continue;
            }

            var absatz = (W.Paragraph)kinder[i];

            // Der Trennabsatz hinter einer Tabelle ist kein Inhalt — er ist nur da, damit
            // Word das Dokument richtig liest. Er wird übersprungen, **kann aber trotzdem
            // die sectPr tragen**, wenn ein Abschnitt mit einer Tabelle endet.
            if (!IstTrennabsatz(kinder, i))
            {
                if (IstSeitenumbruch(absatz)) laufend.Blocks.Add(new TdPageBreak());
                else laufend.Blocks.Add(AbsatzLesen(absatz));
            }

            // Eine sectPr **im** Absatzformat beendet den Abschnitt — sie gehört zu allem,
            // was bis hierher kam, und nicht zu dem, was folgt. Das ist die Gegenrichtung
            // zur Unsymmetrie beim Schreiben.
            if (absatz.ParagraphProperties?.SectionProperties is { } sectPr)
            {
                laufend.Page = SeiteLesen(sectPr, main);
                doc.Sections.Add(laufend);
                laufend = new TdSection();
            }
        }

        // Der letzte Abschnitt trägt seine Einrichtung am Ende des Körpers.
        if (body.GetFirstChild<W.SectionProperties>() is { } letzte)
            laufend.Page = SeiteLesen(letzte, main);

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

    private static TdParagraph AbsatzLesen(W.Paragraph absatz)
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

        foreach (var lauf in absatz.Elements<W.Run>())
        {
            var format = lauf.RunProperties is { } rPr ? ZeichenformatLesen(rPr) : new TdCharFormat();

            foreach (var teil in lauf.ChildElements)
            {
                switch (teil)
                {
                    case W.Text t:
                        p.Inlines.Add(new TdRun(t.Text, format.Kopie()));
                        break;

                    // Ein Umbruch ohne Typ ist der Zeilenumbruch innerhalb des Absatzes.
                    // Ein Seitenumbruch mitten im Absatz wird hier bewusst zum Zeilenumbruch:
                    // das Modell kennt ihn erst ab Schritt 2 als Blockeigenschaft, und ein
                    // stillschweigend verschluckter Umbruch wäre schlechter als ein sichtbarer.
                    case W.Break:
                        p.Inlines.Add(new TdLineBreak { Format = format.Kopie() });
                        break;
                }
            }
        }
        return p;
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

        return f;
    }

    // ==================== Gegenprobe ====================

    /// <summary>
    /// Zählt die Verstöße gegen das Office-2019-Schema. **Ein Dokument, das Word nicht
    /// öffnet, ist kein Export** — dieselbe Messlatte, die der heutige <c>DocxExporter</c>
    /// anlegt.
    /// </summary>
    public static int Pruefen(string pfad)
    {
        using var docx = WordprocessingDocument.Open(pfad, false);
        return new OpenXmlValidator(FileFormatVersions.Office2019).Validate(docx).Count();
    }
}
