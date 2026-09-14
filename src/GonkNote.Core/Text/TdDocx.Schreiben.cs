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
/// <see cref="TdDocx"/> — die schreibende Hälfte. Aus der 2448-Zeilen-Fassung
/// herausgelöst; der Code ist unverändert, nur die Datei ist geteilt — dasselbe
/// Muster wie TdEdit/TdFormatEdit oder WhiteboardView.*.cs.
/// </summary>
public static partial class TdDocx
{
    /// <summary>Schreibt das Dokument als DOCX an <paramref name="pfad"/>.</summary>
    /// <param name="bilder">
    /// Woher die Bildbytes kommen (§4.21). Wird nur gebraucht, wenn das Dokument Bilder
    /// enthält — ein Diagramm braucht sie nicht, denn es geht als **Diagramm** hinaus und
    /// nicht als Bild.
    /// </param>
    /// <param name="titel">
    /// Der Titel des Dokuments. <b>Er ist nicht Schmuck, sondern der Wert eines Feldes:</b>
    /// Ein <see cref="TdFieldKind.Title"/>-Feld wird als <c>TITLE</c> geschrieben, und Word
    /// füllt das aus den Kerneigenschaften der Datei (<c>dc:title</c>). Ohne ihn bliebe eine
    /// Kopfzeile mit <c>{TITEL}</c> beim Öffnen leer.
    /// </param>
    public static void Schreiben(TdDocument doc, string pfad, ITdImages? bilder = null, string? titel = null)
    {
        using var docx = WordprocessingDocument.Create(pfad, WordprocessingDocumentType.Document);
        Fuellen(doc, docx, bilder, titel);
    }

    /// <inheritdoc cref="Schreiben(TdDocument, string, ITdImages?, string?)"/>
    public static void Schreiben(TdDocument doc, Stream ziel, ITdImages? bilder = null, string? titel = null)
    {
        using var docx = WordprocessingDocument.Create(ziel, WordprocessingDocumentType.Document);
        Fuellen(doc, docx, bilder, titel);
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

        UeberschriftenvorlagenSchreiben(doc, teil.Styles);
    }

    /// <summary>
    /// Die Überschriftvorlagen <c>Heading1</c> … <c>Heading9</c> — <b>eine je Ebene, die im
    /// Dokument wirklich vorkommt</b>.
    ///
    /// <para>
    /// <b>Warum es mit <c>w:outlineLvl</c> am Absatz allein nicht getan ist.</b> Ein
    /// Inhaltsverzeichnis-Feld findet seine Einträge zwar auch über die Gliederungsebene, aber
    /// Words Navigationsbereich, „Zu Überschrift springen" und der Katalog beim Aktualisieren
    /// eines Verzeichnisses gehen über die **Formatvorlage**. Eine Datei ohne sie öffnet sich
    /// tadellos und fühlt sich in Word trotzdem falsch an — die Gliederung ist da, aber
    /// unbenutzbar.
    /// </para>
    /// <para>
    /// <b>Die Vorlage trägt bewusst kein Aussehen</b>, nur den Namen und die Ebene. Wie eine
    /// Überschrift aussieht, steht im Modell am Absatz (<see cref="TdParagraph.CharFormat"/>)
    /// und wird dort auch geschrieben. Stünde es zusätzlich in der Vorlage, gäbe es zwei
    /// Quellen für dieselbe Aussage — und die, die gewinnt, wäre nicht die, die der Nutzer
    /// bearbeitet hat (§4.10).
    /// </para>
    /// </summary>
    private static void UeberschriftenvorlagenSchreiben(TdDocument doc, W.Styles styles)
    {
        var ebenen = new SortedSet<int>();
        bool titel = false;

        foreach (var absatz in doc.Paragraphs())
        {
            if (absatz.Format.OutlineLevel is not (> 0 and var ebene)) continue;

            if (absatz.Format.ExcludeFromToc == true && ebene == 1) titel = true;
            else ebenen.Add(ebene);
        }

        foreach (int ebene in ebenen)
        {
            styles.AppendChild(new W.Style(
                new W.StyleName { Val = $"heading {ebene}" },
                new W.BasedOn { Val = "Normal" },
                new W.StyleParagraphProperties(new W.OutlineLevel { Val = ebene - 1 }))
            {
                Type = W.StyleValues.Paragraph,
                StyleId = $"Heading{ebene}",
            });
        }

        // Die Titelvorlage trägt **keine** Gliederungsebene — das ist ihr ganzer Zweck.
        if (titel)
        {
            styles.AppendChild(new W.Style(
                new W.StyleName { Val = "Title" },
                new W.BasedOn { Val = "Normal" })
            {
                Type = W.StyleValues.Paragraph,
                StyleId = TitelVorlage,
            });
        }
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

    private static W.Paragraph AbsatzSchreiben(TdParagraph p, Kontext k)
    {
        var absatz = new W.Paragraph();

        var pPr = AbsatzformatSchreiben(p.Format);

        // Schema-Reihenfolge in CT_PPr: numPr steht **nach** pageBreakBefore und **vor**
        // pBdr. AbsatzformatSchreiben hat beides schon gesetzt, also wird hier
        // eingefügt statt angehängt.
        if (p.List is { } verweis)
        {
            var numPr = new W.NumberingProperties(
                new W.NumberingLevelReference { Val = verweis.Level },
                new W.NumberingId { Val = verweis.ListId });

            OpenXmlElement? davor =
                pPr.GetFirstChild<W.ParagraphBorders>() as OpenXmlElement
                ?? pPr.GetFirstChild<W.SpacingBetweenLines>() as OpenXmlElement
                ?? pPr.GetFirstChild<W.Indentation>() as OpenXmlElement
                ?? pPr.GetFirstChild<W.Justification>() as OpenXmlElement
                ?? pPr.GetFirstChild<W.OutlineLevel>();

            if (davor is null) pPr.AppendChild(numPr);
            else pPr.InsertBefore(numPr, davor);
        }
        // Das Zeichenformat des Absatzes kommt ins pPr/rPr — dort gilt es aber **nur für die
        // Absatzmarke**, nicht für den Text darin. Word erbt Laufformate aus der
        // Formatvorlage, nicht aus dem pPr. Deshalb wird es unten zusätzlich unter jeden Lauf
        // gelegt (siehe `unterlage`); stünde es nur hier, verlöre eine Überschrift beim Export
        // ihre Größe und Farbe und käme in Word als Fließtext an.
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

        foreach (var inline in p.Inlines) StueckSchreiben(absatz, inline, k, p.CharFormat);

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
    /// <param name="unterlage">
    /// Das Zeichenformat des Absatzes. Es wird **unter** jeden Lauf gelegt, weil Word das
    /// <c>pPr/rPr</c> nur auf die Absatzmarke anwendet — dieselbe Erbfolge, die
    /// <see cref="TdDocument.FormatVon(TdParagraph, TdInline)"/> rechnet, hier ausgeschrieben.
    /// </param>
    private static void StueckSchreiben(
        OpenXmlElement absatz, TdInline inline, Kontext k, TdCharFormat unterlage)
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

                foreach (var innen in verweis.Inlines)
                    StueckSchreiben(element, innen, k, verweis.Format.Over(unterlage));

                // Ein leerer Verweis ist schemawidrig und wäre ohnehin nicht anklickbar.
                if (element.HasChildren) absatz.AppendChild(element);
                break;
            }

            case TdField feld:
                FeldSchreiben(absatz, feld, unterlage);
                break;

            case TdGraphic grafik:
                if (ZeichnungSchreiben(grafik, k) is { } zeichnung)
                {
                    var lauf = new W.Run();
                    var rPr = ZeichenformatSchreiben(grafik.Format.Over(unterlage));
                    if (rPr.HasChildren) lauf.AppendChild(rPr);
                    lauf.AppendChild(zeichnung);
                    absatz.AppendChild(lauf);
                }
                break;

            case TdRun r:
            {
                var lauf = new W.Run();
                var rPr = ZeichenformatSchreiben(r.Format.Over(unterlage));
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
                var rPr = ZeichenformatSchreiben(b.Format.Over(unterlage));
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
    private static void FeldSchreiben(OpenXmlElement ziel, TdField feld, TdCharFormat unterlage)
    {
        var format = ZeichenformatSchreiben(feld.Format.Over(unterlage));

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

    private static W.Drawing? ZeichnungSchreiben(TdGraphic grafik, Kontext k) => grafik switch
    {
        TdImage bild => BildSchreiben(bild, k),
        TdChart diagramm => DiagrammSchreiben(diagramm, k),
        _ => throw new NotSupportedException(
            $"{grafik.GetType().Name} kann noch nicht nach DOCX — siehe die Reihenfolge in Roadmap §5."),
    };

    /// <summary>
    /// Der Rahmen, den jede Zeichnung braucht: Maß, Kennung, Alternativtext — und darin das,
    /// was sie ausmacht. Bild und Diagramm unterscheiden sich in OOXML **nur** im Inhalt der
    /// <c>a:graphicData</c> und in deren <c>uri</c>.
    /// </summary>
    private static W.Drawing ZeichnungsrahmenSchreiben(
        TdGraphic grafik, uint id, string name, OpenXmlElement inhalt, string uri)
    {
        var eigenschaften = new DW.DocProperties { Id = id, Name = name };
        if (grafik.AltText is { Length: > 0 } alt) eigenschaften.Description = alt;

        return new W.Drawing(new DW.Inline(
            new DW.Extent { Cx = CmZuEmu(grafik.WidthCm), Cy = CmZuEmu(grafik.HeightCm) },
            eigenschaften,
            new A.Graphic(new A.GraphicData(inhalt) { Uri = uri })));
    }

    /// <summary>
    /// Ein Bild. **Die Originalbytes gehen unverändert hinaus** — neu kodiert würde aus einem
    /// 2-MB-Foto ein Vielfaches (§4.21).
    /// </summary>
    private static W.Drawing? BildSchreiben(TdImage bild, Kontext k)
    {
        if (k.Bilder is null)
            throw new NotSupportedException(
                "Das Dokument enthält ein Bild, aber es wurde kein Bildspeicher mitgegeben — " +
                "TdDocx.Schreiben(doc, ziel, bilder) benutzen (HANDOFF §4.21).");

        // **Ein fehlender Blob ist kein Programmierfehler**, sondern eine unvollständige
        // Sicherung (Dauerregel 4: der Blob-Ordner wird gern vergessen). Das eine Bild fällt
        // weg, der Export läuft weiter — so hielt es der frühere DocxExporter auch.
        if (k.Bilder.Lesen(bild.BlobId) is not { } daten) return null;

        var teil = k.Main.AddImagePart(BildTeilTyp(bild.Extension));
        using (var strom = new MemoryStream(daten)) teil.FeedData(strom);

        uint id = k.NaechsteZeichnung();
        long cx = CmZuEmu(bild.WidthCm), cy = CmZuEmu(bild.HeightCm);

        return ZeichnungsrahmenSchreiben(bild, id, $"Bild {id}", new PIC.Picture(
            new PIC.NonVisualPictureProperties(
                new PIC.NonVisualDrawingProperties { Id = 0U, Name = $"Bild {id}" },
                new PIC.NonVisualPictureDrawingProperties()),
            new PIC.BlipFill(
                new A.Blip { Embed = k.Main.GetIdOfPart(teil) },
                new A.Stretch(new A.FillRectangle())),
            new PIC.ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = 0L, Y = 0L },
                    new A.Extents { Cx = cx, Cy = cy }),
                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })),
            UriBild);
    }

    /// <summary>
    /// Ein Diagramm — als **echtes** Diagramm und nicht als Bild.
    ///
    /// <para>
    /// <b>Das ist der Unterschied zum heutigen Editor</b>, der ein Diagramm beim Einfügen zu
    /// einer Bitmap rendert und die Zahlen damit wegwirft (§4.21). Hier gehen die Zahlen
    /// hinaus: Word zeigt ein Diagramm, das es selbst zeichnet, und beim Rückimport sind sie
    /// wieder da.
    /// </para>
    /// <para>
    /// <b>Mit literalen Daten (<c>c:strLit</c>/<c>c:numLit</c>) und ohne eingebettete
    /// Arbeitsmappe.</b> Word legt seine Diagramme sonst zusätzlich als XLSX in die Datei —
    /// **dieselben Zahlen ein zweites Mal**, und genau davor warnt §4.10. Der Preis steht in
    /// §4.21: Words Knopf „Daten bearbeiten" findet keine Mappe und bietet an, eine anzulegen.
    /// Angezeigt und gedruckt wird das Diagramm einwandfrei.
    /// </para>
    /// </summary>
    private static W.Drawing DiagrammSchreiben(TdChart d, Kontext k)
    {
        var teil = k.Main.AddNewPart<ChartPart>();
        teil.ChartSpace = DiagrammraumBauen(d);

        uint id = k.NaechsteZeichnung();
        return ZeichnungsrahmenSchreiben(d, id, $"Diagramm {id}",
            new C.ChartReference { Id = k.Main.GetIdOfPart(teil) }, UriDiagramm);
    }

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
        // Schema-Reihenfolge (CT_PPr): pStyle, keepNext, pageBreakBefore, numPr, pBdr, spacing,
        // ind, jc, outlineLvl. `numPr` trägt AbsatzSchreiben nach — es kennt den Listenverweis.
        var pPr = new W.ParagraphProperties();

        // **Die Überschriftvorlage — zusätzlich zur Gliederungsebene weiter unten, nicht
        // statt ihrer.** Word braucht die Vorlage für Navigationsbereich und Verzeichnis-Katalog
        // (siehe UeberschriftenvorlagenSchreiben), die Ebene dagegen ist das, was beim Lesen
        // zurück ins Modell geht. Beide sagen dasselbe, und beide werden gebraucht.
        if (f.OutlineLevel is > 0 and var ueberschrift)
            pPr.AppendChild(new W.ParagraphStyleId { Val = Vorlagenname(ueberschrift, f.ExcludeFromToc) });

        if (f.KeepWithNext is { } halten) pPr.AppendChild(new W.KeepNext { Val = halten });
        if (f.PageBreakBefore is { } umbruch) pPr.AppendChild(new W.PageBreakBefore { Val = umbruch });

        // Die Trennlinie. Eine Linie ohne Stärke wird **ausdrücklich** als `none`
        // geschrieben und nicht weggelassen: „nicht gesetzt" und „keine Linie" sind im
        // Modell zweierlei, und nur so kommt der Unterschied zurück.
        if (f.BottomBorder is { } linie)
            pPr.AppendChild(new W.ParagraphBorders(Linie<W.BottomBorder>(linie)));

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
        {
            // Word zählt ab 0 und benutzt 9 für Fließtext — die eigene 0 ist genau das.
            // **Und ein ausgeschlossener Absatz bekommt ebenfalls die 9**: Genau daran erkennt
            // Words Verzeichnisfeld, dass es ihn übergehen soll. Sein Rang steht in der
            // Vorlage und geht dadurch nicht verloren (§4.23).
            bool ausgeschlossen = f.ExcludeFromToc == true;
            pPr.AppendChild(new W.OutlineLevel { Val = ebene == 0 || ausgeschlossen ? 9 : ebene - 1 });
        }

        return pPr;
    }

    // ==================== Seiteneinrichtung ====================

    private static W.SectionProperties SeiteSchreiben(TdPageSetup seite, MainDocumentPart main, Kontext k)
    {
        var sectPr = new W.SectionProperties();

        // Schema-Reihenfolge (CT_SectPr): die Verweise auf Kopf-/Fußzeile stehen **vor**
        // pgSz und pgMar.
        //
        // **Das Wasserzeichen erzwingt eine Kopfzeile, auch ohne Kopfzeilentext** — in DOCX
        // hängt es dort und nirgends sonst (§4.21).
        if (seite.HeaderText.Length > 0 || seite.Watermark is not null)
        {
            var teil = main.AddNewPart<HeaderPart>();
            var kopf = new W.Header();

            if (seite.Watermark is { } zeichen &&
                WasserzeichenSchreiben(zeichen, seite.WatermarkOpacity, teil, k) is { } absatz)
                kopf.AppendChild(absatz);

            // Ein Kopfzeilenteil ohne Absatz ist schemawidrig — dieselbe Regel wie bei der
            // Tabellenzelle (§4.18).
            if (seite.HeaderText.Length > 0 || !kopf.HasChildren)
                kopf.AppendChild(KopfFussAbsatzSchreiben(seite.HeaderText));

            teil.Header = kopf;
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

    /// <summary>
    /// Das Wasserzeichen als Absatz für die Kopfzeile.
    ///
    /// <para>
    /// <b>Es ist eine VML-Zeichnung und keine DrawingML.</b> Das ist kein Rückschritt, sondern
    /// die Form, in der Word ein Wasserzeichen schreibt und erwartet — ein hinter dem Text
    /// liegendes, auf der Seite zentriertes Bild gibt es als eingebundene Zeichnung
    /// (<c>wp:inline</c>) gar nicht.
    /// </para>
    /// <para>
    /// <b>Das Bild hängt am Kopfzeilenteil</b>, nicht am Hauptteil: Beziehungen gehören zu dem
    /// Teil, der sie benutzt. Wer die Kennung am Hauptteil holt, bekommt eine Datei, in der
    /// Word das Wasserzeichen nicht findet.
    /// </para>
    /// </summary>
    private static W.Paragraph? WasserzeichenSchreiben(
        TdImage zeichen, double deckkraft, HeaderPart teil, Kontext k)
    {
        if (k.Bilder is null)
            throw new NotSupportedException(
                "Der Abschnitt hat ein Wasserzeichen, aber es wurde kein Bildspeicher mitgegeben — " +
                "TdDocx.Schreiben(doc, ziel, bilder) benutzen (HANDOFF §4.21).");

        if (k.Bilder.Lesen(zeichen.BlobId) is not { } daten) return null;

        var bildteil = teil.AddImagePart(BildTeilTyp(zeichen.Extension));
        using (var strom = new MemoryStream(daten)) bildteil.FeedData(strom);

        double breite = zeichen.WidthCm * PunktProCm;
        double hoehe = zeichen.HeightCm * PunktProCm;

        string stil = string.Create(CultureInfo.InvariantCulture,
            $"position:absolute;margin-left:0;margin-top:0;width:{breite:0.##}pt;height:{hoehe:0.##}pt;" +
            $"z-index:-251658752;mso-position-horizontal:center;mso-position-horizontal-relative:margin;" +
            $"mso-position-vertical:center;mso-position-vertical-relative:margin");

        var bilddaten = new V.ImageData
        {
            RelationshipId = teil.GetIdOfPart(bildteil),
            Title = "Wasserzeichen",
            // **Deckkraft gibt es hier nicht** — Word blasst über Helligkeit auf. `gain` ist
            // eine Festkommazahl mit 16 Nachkommastellen und dem Suffix „f".
            Gain = GainAusDeckkraft(deckkraft),
        };

        return new W.Paragraph(new W.Run(new W.Picture(
            new V.Shape(bilddaten)
            {
                Id = "Wasserzeichen",
                Style = stil,
            })));
    }
}
