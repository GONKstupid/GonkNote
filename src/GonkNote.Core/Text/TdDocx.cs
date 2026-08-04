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

        foreach (var block in doc.Blocks)
        {
            switch (block)
            {
                case TdParagraph p: body.AppendChild(AbsatzSchreiben(p)); break;
                case TdPageBreak: body.AppendChild(SeitenumbruchSchreiben()); break;

                // Ein neuer Blocktyp ohne Zweig würde hier still verschwinden — und ein
                // verlorener Block fällt erst dem Leser auf, nicht dem Diff.
                default:
                    throw new NotSupportedException(
                        $"{block.GetType().Name} kann noch nicht nach DOCX — siehe die Reihenfolge in Roadmap §5.");
            }
        }

        // Ein Körper ohne Absatz ist zwar schemakonform, aber Word zeigt dafür ein Dokument
        // ohne Einfügemarke. Dieselbe Vorsorge trifft der heutige DocxExporter.
        if (!body.HasChildren) body.AppendChild(new W.Paragraph());

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

    private static W.Paragraph AbsatzSchreiben(TdParagraph p)
    {
        var absatz = new W.Paragraph();

        var pPr = AbsatzformatSchreiben(p.Format);
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

        foreach (var absatz in body.Elements<W.Paragraph>())
        {
            if (IstSeitenumbruch(absatz)) { doc.Blocks.Add(new TdPageBreak()); continue; }
            doc.Blocks.Add(AbsatzLesen(absatz));
        }
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
