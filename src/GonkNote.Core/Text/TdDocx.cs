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
/// Bilder und Diagramme (Schritt 6). Alles davor ist da — Absätze und Zeichenformate
/// (Schritt 1), Abschnitte samt Kopf- und Fußzeile (Schritt 2), Listen (Schritt 3), Tabellen
/// (Schritt 4), Felder, Verweise und Inhaltsverzeichnis (Schritt 5). Das Wasserzeichen hängt
/// an <c>TextDoc</c> und ist ein Bild; es kommt mit Schritt 6.
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

        var k = new Kontext(main) { Textmarken = TdToc.Enthaelt(doc) };

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

    private static W.Paragraph AbsatzSchreiben(TdParagraph p, Kontext k)
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

        // **Das Sprungziel einer Überschrift.** Es steht *im* Absatz und umschließt seinen
        // Inhalt — eine Textmarke ist eine Spanne, kein Punkt. Geschrieben wird sie nur, wenn
        // das Dokument ein Inhaltsverzeichnis hat (siehe Kontext.Textmarken).
        int? textmarke = null;
        if (k.Textmarken && p.Format.OutlineLevel is > 0)
        {
            var (name, id) = k.NaechsteTextmarke();
            absatz.AppendChild(new W.BookmarkStart { Id = id.ToString(), Name = name });
            textmarke = id;
        }

        foreach (var inline in p.Inlines) StueckSchreiben(absatz, inline, k);

        if (textmarke is { } ende) absatz.AppendChild(new W.BookmarkEnd { Id = ende.ToString() });

        return absatz;
    }

    /// <summary>
    /// Schreibt ein Textstück in seinen Absatz.
    /// <para>
    /// <b>Der Verweis steht vor dem Lauf</b> — nicht als Laune, sondern weil er einer ist, der
    /// Läufe enthält. Dieselbe Erbfolge wie im <c>FlowDocument</c>, wo <c>Hyperlink</c> von
    /// <c>Span</c> erbt und der allgemeinere Fall das Ziel verschluckt (§7).
    /// </para>
    /// </summary>
    private static void StueckSchreiben(OpenXmlElement absatz, TdInline inline, Kontext k)
    {
        switch (inline)
        {
            case TdHyperlink verweis:
            {
                var element = new W.Hyperlink();

                if (verweis.IstTextmarke)
                {
                    // Ein Verweis **in dasselbe Dokument** ist keine Beziehung auf eine Datei,
                    // sondern ein Anker. Wer ihn als Beziehung schreibt, bekommt einen Link,
                    // der Word ein zweites Fenster öffnen lässt.
                    element.Anchor = verweis.Target[1..];
                }
                else if (verweis.Target.Length > 0)
                {
                    // **`OriginalString` und nicht `AbsoluteUri`**: sonst wird aus dem relativen
                    // Ziel `kapitel-2.md` ein absoluter `file:///`-Pfad (§7).
                    var beziehung = k.Main.AddHyperlinkRelationship(
                        new Uri(verweis.Target, UriKind.RelativeOrAbsolute), isExternal: true);
                    element.Id = beziehung.Id;
                }

                foreach (var innen in verweis.Inlines) StueckSchreiben(element, innen, k);

                // Ein leerer Verweis ist schemawidrig und wäre ohnehin nicht anklickbar.
                if (element.HasChildren) absatz.AppendChild(element);
                break;
            }

            case TdField feld:
                FeldSchreiben(absatz, feld);
                break;

            case TdRun r:
            {
                var lauf = new W.Run();
                var rPr = ZeichenformatSchreiben(r.Format);
                if (rPr.HasChildren) lauf.AppendChild(rPr);

                // Space="preserve": ohne das fielen führende und mehrfache Leerzeichen weg,
                // und der Text säße nach dem Roundtrip zusammengeschoben da.
                lauf.AppendChild(new W.Text(r.Text) { Space = SpaceProcessingModeValues.Preserve });
                absatz.AppendChild(lauf);
                break;
            }

            case TdLineBreak b:
            {
                var lauf = new W.Run();
                var rPr = ZeichenformatSchreiben(b.Format);
                if (rPr.HasChildren) lauf.AppendChild(rPr);
                lauf.AppendChild(new W.Break());
                absatz.AppendChild(lauf);
                break;
            }

            default:
                throw new NotSupportedException(
                    $"{inline.GetType().Name} kann noch nicht nach DOCX — siehe die Reihenfolge in Roadmap §5.");
        }
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

    /// <summary>
    /// Ein Feld in der **dreiteiligen** Form: <c>fldChar begin</c>, <c>instrText</c>,
    /// <c>fldChar end</c>.
    ///
    /// <para>
    /// <b>Warum nicht überall <c>fldSimple</c>.</b> Für PAGE und NUMPAGES reicht die kurze
    /// Form, für ein Inhaltsverzeichnis nicht: dessen Ergebnis sind ganze Absätze mit eigenen
    /// Verweisen, und die haben in einem Attribut keinen Platz. Zwei Formen nebeneinander zu
    /// schreiben wäre die Doppelung aus §4.10 — deshalb schreibt der Körper **eine**, und zwar
    /// die, die alles kann.
    /// </para>
    ///
    /// <para>
    /// <b>Ohne zwischengespeichertes Ergebnis</b>, also ohne <c>separate</c>-Teil. Das ist die
    /// wichtigste Entscheidung an dieser Stelle: Ein mitgeschriebenes Verzeichnis käme beim
    /// Lesen als gewöhnliche Absätze zurück, und das Dokument wüchse **mit jedem Speichern um
    /// ein ganzes Inhaltsverzeichnis** — dieselbe Falle wie beim Trennabsatz zwischen zwei
    /// Tabellen (§4.18), nur mit dreißig Zeilen statt einer. Word füllt das Feld beim Öffnen,
    /// dafür steht <c>UpdateFieldsOnOpen</c> im Dokument.
    /// </para>
    /// </summary>
    private static void FeldSchreiben(OpenXmlElement ziel, TdField feld)
    {
        var format = ZeichenformatSchreiben(feld.Format);

        W.Run Lauf(OpenXmlElement inhalt)
        {
            var lauf = new W.Run();
            if (format.HasChildren)
                lauf.AppendChild(new W.RunProperties(format.ChildElements.Select(c => c.CloneNode(true))));
            lauf.AppendChild(inhalt);
            return lauf;
        }

        ziel.AppendChild(Lauf(new W.FieldChar { FieldCharType = W.FieldCharValues.Begin }));
        ziel.AppendChild(Lauf(new W.FieldCode(Anweisung(feld)) { Space = SpaceProcessingModeValues.Preserve }));
        ziel.AppendChild(Lauf(new W.FieldChar { FieldCharType = W.FieldCharValues.End }));
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

    private static W.Table TabelleSchreiben(TdTable t, Kontext k)
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

        foreach (var zeile in t.Rows) tabelle.AppendChild(ZeileSchreiben(zeile, t, k));

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

    private static W.TableRow ZeileSchreiben(TdTableRow zeile, TdTable t, Kontext k)
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
            tr.AppendChild(ZelleSchreiben(zelle, t, spalte, k));
            spalte += Math.Max(1, zelle.ColumnSpan);
        }
        return tr;
    }

    private static W.TableCell ZelleSchreiben(TdTableCell zelle, TdTable t, int abSpalte, Kontext k)
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
                case TdParagraph p: tc.AppendChild(AbsatzSchreiben(p, k)); hatAbsatz = true; break;
                case TdPageBreak: tc.AppendChild(SeitenumbruchSchreiben()); hatAbsatz = true; break;

                // **Eine Tabelle in einer Tabelle** ist erlaubt und braucht danach einen
                // Absatz — dieselbe Regel wie im Körper.
                case TdTable innen:
                    tc.AppendChild(TabelleSchreiben(innen, k));
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

    private static TdTable TabelleLesen(W.Table tabelle, OpenXmlPart teil)
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

    private static TdTableRow ZeileLesen(W.TableRow tr, OpenXmlPart teil)
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

    private static TdTableCell ZelleLesen(W.TableCell tc, OpenXmlPart teil)
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

    /// <summary>Absätze und Tabellen eines Behälters, in Dokumentreihenfolge.</summary>
    private static List<OpenXmlElement> Inhaltskinder(OpenXmlElement behaelter) =>
        [.. behaelter.ChildElements.Where(k => k is W.Paragraph or W.Table)];

    /// <summary>
    /// Liest Absätze und Tabellen eines Behälters (hier: einer Zelle) in
    /// Dokumentreihenfolge. Der Körper geht einen eigenen Weg, weil dort zusätzlich die
    /// Abschnittsgrenzen abzulesen sind.
    /// </summary>
    private static void BloeckeLesen(OpenXmlElement behaelter, List<TdBlock> ziel, OpenXmlPart teil)
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
    /// Eine Kopf- oder Fußzeile. **Alle vier Platzhalter werden zu echten Word-Feldern** —
    /// <c>{SEITE}</c>, <c>{SEITEN}</c>, <c>{DATUM}</c> und <c>{TITEL}</c>. Als bloßer Text
    /// stünde auf jeder Seite dieselbe Zahl, und ein Datum wäre auf ewig der Tag des Exports.
    ///
    /// <para>
    /// <b>Die Zuordnung kommt aus <see cref="TdField.Platzhalter"/></b> und steht nicht noch
    /// einmal hier: eine zweite Tabelle für dieselbe Sache driftet (§4.10). Bis Schritt 5
    /// standen <c>{DATUM}</c> und <c>{TITEL}</c> hier wörtlich im Text — sie hatten kein Feld,
    /// zu dem sie hätten werden können.
    /// </para>
    /// <para>
    /// <b>Hier reicht die kurze Form <c>fldSimple</c></b>, anders als im Körper: In einer
    /// Kopfzeile steht nie ein Inhaltsverzeichnis, und mehr als eine Zeile Ergebnis braucht
    /// keines dieser Felder. Der Leser kennt trotzdem beide Formen — ein fremdes Dokument
    /// schreibt hier gern die lange.
    /// </para>
    /// </summary>
    private static W.Paragraph KopfFussAbsatzSchreiben(string vorlage)
    {
        var absatz = new W.Paragraph();

        foreach (string teil in ZerlegtNachPlatzhaltern(vorlage))
        {
            if (teil.Length == 0) continue;

            if (TdField.ArtVonPlatzhalter(teil) is { } art)
            {
                absatz.AppendChild(new W.SimpleField { Instruction = Anweisung(new TdField(art)) });
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
            if (main.GetPartById(id) is HeaderPart { Header: { } kopf } kopfteil)
                seite.HeaderText = KopfFussTextLesen(kopf, kopfteil);
        }
        foreach (var verweis in sectPr.Elements<W.FooterReference>())
        {
            if (verweis.Type?.Value != W.HeaderFooterValues.Default || verweis.Id?.Value is not { } id) continue;
            if (main.GetPartById(id) is FooterPart { Footer: { } fuss } fussteil)
                seite.FooterText = KopfFussTextLesen(fuss, fussteil);
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
    private static string KopfFussTextLesen(OpenXmlElement kopfOderFuss, OpenXmlPart dokumentteil)
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
                laufend.Blocks.Add(TabelleLesen(tabelle, main));
                continue;
            }

            var absatz = (W.Paragraph)kinder[i];

            // Der Trennabsatz hinter einer Tabelle ist kein Inhalt — er ist nur da, damit
            // Word das Dokument richtig liest. Er wird übersprungen, **kann aber trotzdem
            // die sectPr tragen**, wenn ein Abschnitt mit einer Tabelle endet.
            if (!IstTrennabsatz(kinder, i))
            {
                if (IstSeitenumbruch(absatz)) laufend.Blocks.Add(new TdPageBreak());
                else laufend.Blocks.Add(AbsatzLesen(absatz, main));
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

    private static TdParagraph AbsatzLesen(W.Paragraph absatz, OpenXmlPart teil)
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
        OpenXmlElement behaelter, List<TdInline> ziel, OpenXmlPart teil, Feldleser leser)
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
                    LaufLesen(lauf, ziel, leser);
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

    private static void LaufLesen(W.Run lauf, List<TdInline> ziel, Feldleser leser)
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
    private static string VerweisZielLesen(W.Hyperlink verweis, OpenXmlPart teil)
    {
        if (verweis.Anchor?.Value is { Length: > 0 } anker) return "#" + anker;
        if (verweis.Id?.Value is not { } id) return "";

        foreach (var beziehung in teil.HyperlinkRelationships)
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
