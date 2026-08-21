using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using GonkNote.Core.Text;

namespace GonkNote.Services;

/// <summary>
/// Der Rückweg zu <see cref="FlowZuTd"/>: aus einem <see cref="TdDocument"/> wird ein
/// <see cref="FlowDocument"/>.
///
/// <para>
/// <b>Wofür er gebraucht wird — und wofür ausdrücklich noch nicht.</b> Der DOCX-Import läuft
/// seit dem Umverdrahten über <see cref="TdDocx"/> und liefert damit ein Modell. Der Editor
/// liest aber weiter aus <c>TextDoc.Rtf</c> (HANDOFF §4.22: <c>Rtf</c> bleibt das führende
/// Feld, bis Export **und** Anzeige aus dem Modell laufen). Ohne diesen Weg käme ein
/// importiertes Dokument mit gefülltem Modell und leerem Editor an — für den Nutzer
/// ununterscheidbar von „die Datei war leer".
/// </para>
/// <para>
/// <b>Er ist nicht der Zeichner.</b> Die Anzeige aus dem Modell heraus ist die nächste Runde
/// (<c>TdLayout</c> → SkiaSharp); hier wird nur in das Format übersetzt, das der heutige
/// Editor bereits anzeigen kann.
/// </para>
///
/// <para>
/// <b>Was dabei nicht ankommt, steht hier und nicht in einer stillen Lücke</b> (§7):
/// </para>
/// <list type="bullet">
///   <item>
///     <b>Ein Diagramm.</b> Das <c>FlowDocument</c> kann nur Bilder, und aus einem
///     <see cref="TdChart"/> ein Bild zu machen ist Sache des Zeichners („gezeichnet wird noch
///     nichts", §4.21). Es überlebt im Modell bis zum nächsten Speichern — danach führt wieder
///     <c>Rtf</c>. <b>Vorher ging es schon beim Import verloren</b>, der alte
///     <c>DocxImporter</c> kannte <c>c:chart</c> gar nicht.
///   </item>
///   <item>
///     <b>Ein Feld</b> wird zu seinem Platzhaltertext (<c>{SEITE}</c> …) bzw. — beim
///     Inhaltsverzeichnis — zu den gerechneten Einträgen, genau in der Form, in der der Editor
///     sie selbst erzeugt. Der Editor kennt keine Felder; ein Platzhalter ist das, was er
///     stattdessen zeigt.
///   </item>
/// </list>
/// </summary>
public static class TdZuFlow
{
    /// <summary>
    /// <b>Den Inhalt eines frisch gebauten Dokuments in ein bestehendes übernehmen — ohne den
    /// Umweg über ein <c>XamlPackage</c></b> (HANDOFF §4.47). Das Gegenstück zu
    /// <see cref="Umwandeln"/>: <i>so kommt das Ergebnis in den Editor.</i>
    ///
    /// <para>
    /// <b>Der Umweg stand bis zum 2026-08-21 in <c>TextEditorView.AusModell</c>, und seine
    /// Begründung war richtig:</b> Ein ausgetauschtes <c>Document</c> nähme dem
    /// <c>RichTextBox</c> seine Stile und alle Ereignisverdrahtungen mit. <b>Nur folgt daraus
    /// nicht das Paket, sondern dieses Umhängen:</b> Das <c>Document</c> bleibt dasselbe
    /// Objekt, ausgetauscht wird sein <b>Inhalt</b>.
    /// </para>
    /// <para>
    /// <b>Warum das mehr ist als eine gesparte Umwandlung.</b> Ein <c>XamlPackage</c> speichert
    /// nur die Eigenschaften, die es kennt — <b>ein Träger am Element (<c>Tag</c> wie
    /// <c>ToolTip</c>) kommt als <c>null</c> zurück</b> (§4.45, gemessen). Es ist damit der
    /// Weg, auf dem alles verlorengeht, was ein <c>FlowDocument</c> nicht selbst kennt: heute
    /// das <b>Diagramm</b> und das <b>Feld</b> (§4.45, die zwei verbliebenen Lücken).
    /// <b>Ohne Paket überlebt ein Träger, weil ihn niemand serialisiert.</b>
    /// </para>
    /// <para>
    /// <b>Ein Block hat genau einen Elternteil</b> — deshalb wird er erst aus der Quelle
    /// genommen und dann angehängt, nicht umgekehrt. Die Quelle ist ein Wegwerf-Dokument aus
    /// <see cref="Umwandeln"/> und darf leer zurückbleiben; <b>bei einem Dokument, das der
    /// Nutzer gerade sieht, wäre genau dieser Griff der Fehler aus §4.22.</b>
    /// </para>
    /// </summary>
    public static void InhaltUebernehmen(FlowDocument quelle, FlowDocument ziel)
    {
        // Die Grundschrift steht am Dokument und nicht an den Absätzen (§4.14): Sie muss
        // mitkommen, sonst erbt jeder Absatz die Vorgabe des Steuerelements statt der des
        // Dokuments — und der Umbruch sähe anders aus als der Export.
        ziel.FontFamily = quelle.FontFamily;
        ziel.FontSize = quelle.FontSize;
        ziel.Foreground = quelle.Foreground;

        ziel.Blocks.Clear();
        while (quelle.Blocks.FirstBlock is { } block)
        {
            quelle.Blocks.Remove(block);
            ziel.Blocks.Add(block);
        }
    }

    // WPF rechnet in geräteunabhängigen Pixeln: 96 auf ein Zoll.
    private const double PixelProCm = 96.0 / 2.54;
    private const double PixelProPunkt = 96.0 / 72.0;

    private static double Px(double cm) => cm * PixelProCm;
    private static double PxAusPt(double pt) => pt * PixelProPunkt;

    /// <summary>
    /// Wandelt Inhalt **und** Seiteneinrichtung um.
    /// </summary>
    /// <param name="ziel">
    /// Bekommt die Seiteneinrichtung des **ersten** Abschnitts — das Altformat kennt nur eine
    /// (§4.15). <c>null</c> = die Seiteneinrichtung interessiert nicht.
    /// </param>
    public static FlowDocument Umwandeln(TdDocument doc, BlobStore blobs, TextDoc? ziel = null)
    {
        var grund = doc.DefaultCharFormat.Aufgeloest();

        var flow = new FlowDocument
        {
            FontFamily = new FontFamily(grund.FontFamily!),
            FontSize = PxAusPt(grund.FontSize!.Value),
            Foreground = new SolidColorBrush(TextStyles.InkLight),
        };

        var zustand = new Zustand(doc, blobs, TdListNumbering.Marken(doc));
        BloeckeUmwandeln(doc.Blocks().ToList(), flow.Blocks, zustand);

        // Ein leeres Dokument ist nicht leer, sondern hat einen Absatz — sonst hätte der
        // Cursor beim ersten Tastendruck keinen Ort (§4.14, hier andersherum).
        if (flow.Blocks.Count == 0) flow.Blocks.Add(new Paragraph());

        if (ziel is not null && doc.Sections.Count > 0) SeiteUmwandeln(doc.Sections[0].Page, blobs, ziel);
        return flow;
    }

    /// <summary>Was über den ganzen Durchlauf gilt.</summary>
    private sealed record Zustand(
        TdDocument Doc, BlobStore Blobs, Dictionary<TdParagraph, string> Marken);

    // ==================== Seiteneinrichtung ====================

    /// <summary>
    /// Die Seiteneinrichtung zurück ans <see cref="TextDoc"/>.
    /// <para>
    /// <b>Der Formatname wird zurückerkannt und nicht geraten</b> — <c>TdPageSetup.Name</c>
    /// tut genau das (§4.15). Eine Größe ohne Namen ist kein Fehler; das Altformat kann sie
    /// nur nicht ablegen und bekommt das nächstliegende Blatt.
    /// </para>
    /// </summary>
    private static void SeiteUmwandeln(TdPageSetup seite, BlobStore blobs, TextDoc ziel)
    {
        ziel.PageFormat = seite.Name ?? NaechstesFormat(seite);
        ziel.Landscape = seite.IstQuerformat;

        ziel.MarginLeftCm = Math.Round(seite.MarginLeftCm, 2);
        ziel.MarginTopCm = Math.Round(seite.MarginTopCm, 2);
        ziel.MarginRightCm = Math.Round(seite.MarginRightCm, 2);
        ziel.MarginBottomCm = Math.Round(seite.MarginBottomCm, 2);

        ziel.HeaderText = seite.HeaderText;
        ziel.FooterText = seite.FooterText;
        ziel.SuppressHeaderOnFirstPage = seite.SuppressOnFirstPage;

        // Das Wasserzeichen liegt im Modell als Verweis, im Altformat als Bytes am Dokument
        // (§4.15) — hier läuft §4.22 rückwärts.
        if (seite.Watermark is { } zeichen && blobs.Read(zeichen.BlobId) is { } bytes)
        {
            ziel.WatermarkImage = bytes;
            ziel.WatermarkOpacity = seite.WatermarkOpacity;
        }
    }

    /// <summary>Das benannte Blatt, dessen Hochformat der Größe am nächsten kommt.</summary>
    private static string NaechstesFormat(TdPageSetup seite)
    {
        double kurz = Math.Min(seite.WidthCm, seite.HeightCm);
        double lang = Math.Max(seite.WidthCm, seite.HeightCm);

        string bestes = "A4";
        double bester = double.MaxValue;

        foreach (var (name, blatt) in new[]
                 {
                     ("A4", TdPageSetup.A4), ("A5", TdPageSetup.A5),
                     ("A3", TdPageSetup.A3), ("Letter", TdPageSetup.Letter),
                 })
        {
            double abstand = Math.Abs(kurz - blatt.WidthCm) + Math.Abs(lang - blatt.HeightCm);
            if (abstand < bester) { bester = abstand; bestes = name; }
        }
        return bestes;
    }

    // ==================== Blöcke ====================

    /// <summary>
    /// Blöcke umwandeln — und die aufeinanderfolgenden Listenpunkte dabei wieder zu
    /// <see cref="List"/>-Blöcken bündeln.
    /// <para>
    /// <b>Das ist die Umrechnung, die man beim ersten Anlauf falsch macht</b> — genau
    /// andersherum als in §4.22: Im Modell ist ein Listenpunkt ein Absatz mit einer Angabe
    /// (§4.17), im <c>FlowDocument</c> ist eine Liste eine Klammer um ihre Punkte. Wer je
    /// Absatz eine eigene Liste anlegt, bekommt zehn Listen mit je einem Punkt — und jede
    /// fängt wieder bei 1 an.
    /// </para>
    /// </summary>
    private static void BloeckeUmwandeln(List<TdBlock> bloecke, BlockCollection ziel, Zustand zustand)
    {
        var offen = new List<(int Ebene, List Liste)>();
        int laufendeListe = 0;

        foreach (var block in bloecke)
        {
            if (block is TdParagraph { List: { } verweis } punkt)
            {
                if (verweis.ListId != laufendeListe) { offen.Clear(); laufendeListe = verweis.ListId; }

                // Zurück auf die Ebene dieses Punkts: alles Tiefere ist abgeschlossen.
                while (offen.Count > 0 && offen[^1].Ebene > verweis.Level) offen.RemoveAt(offen.Count - 1);

                while (offen.Count == 0 || offen[^1].Ebene < verweis.Level)
                {
                    int ebene = offen.Count == 0 ? 0 : offen[^1].Ebene + 1;
                    var neu = new List
                    {
                        MarkerStyle = MarkenartFuer(zustand.Doc, verweis.ListId, ebene),
                        Margin = new Thickness(14, 4, 0, 4),
                    };

                    // Eine tiefere Ebene ist **dieselbe** Liste weiter innen (§4.17): sie hängt
                    // im letzten Punkt der Ebene darüber, nicht neben ihr.
                    if (offen.Count > 0 && offen[^1].Liste.ListItems.LastOrDefault() is { } eltern)
                        eltern.Blocks.Add(neu);
                    else
                        ziel.Add(neu);

                    offen.Add((ebene, neu));
                }

                // **Der Abstand des Punktes bleibt der seines Absatzes** (§4.45). Hier stand
                // bis dahin ein festes `Thickness(0, 1, 0, 1)` — es sah in der Liste enger aus
                // und schrieb dem Modell auf dem Rückweg still `0,75 pt` vor Abstand und
                // `0,75 pt` danach ein, wo der Nutzer nie etwas gesetzt hatte. Ein Listenpunkt
                // ist ein Absatz (§4.17); er hat den Abstand, den sein Format ergibt.
                var absatz = AbsatzUmwandeln(punkt, zustand);
                offen[^1].Liste.ListItems.Add(new ListItem(absatz));
                continue;
            }

            offen.Clear();
            laufendeListe = 0;

            switch (block)
            {
                case TdParagraph p:
                    foreach (var b in AbsatzOderVerzeichnis(p, zustand)) ziel.Add(b);
                    break;

                case TdTable t:
                    ziel.Add(TabelleUmwandeln(t, zustand));
                    break;

                // Das FlowDocument kennt keinen Umbruchblock, nur die Angabe „vor diesem
                // Absatz umbrechen". Ein leerer Absatz damit ist genau das.
                case TdPageBreak:
                    ziel.Add(new Paragraph { BreakPageBefore = true, Margin = new Thickness(0) });
                    break;
            }
        }
    }

    private static TextMarkerStyle MarkenartFuer(TdDocument doc, int listId, int ebene)
    {
        var stufe = doc.Lists.FirstOrDefault(l => l.Id == listId)?.Level(ebene);
        return stufe?.Marker switch
        {
            TdListMarker.Decimal => TextMarkerStyle.Decimal,
            TdListMarker.LowerLetter => TextMarkerStyle.LowerLatin,
            TdListMarker.UpperLetter => TextMarkerStyle.UpperLatin,
            TdListMarker.LowerRoman => TextMarkerStyle.LowerRoman,
            TdListMarker.UpperRoman => TextMarkerStyle.UpperRoman,
            _ => TextMarkerStyle.Disc,
        };
    }

    /// <summary>
    /// Ein Absatz — oder, wenn er ein Inhaltsverzeichnis-Feld trägt, dessen gerechnete
    /// Einträge. Der Editor erzeugt sein Verzeichnis als gewöhnliche Absätze
    /// (<c>TextStyles.IsTocEntry</c>: Größe 13, Einzug in 18er-Schritten), und genau die Form
    /// bekommt es hier — sonst stünde nach dem Import eine leere Zeile, wo ein Verzeichnis
    /// hingehört.
    /// </summary>
    private static IEnumerable<Block> AbsatzOderVerzeichnis(TdParagraph p, Zustand zustand)
    {
        if (TdToc.Feld(p) is not { } feld)
        {
            yield return AbsatzUmwandeln(p, zustand);
            yield break;
        }

        foreach (var eintrag in TdToc.Eintraege(zustand.Doc, null, feld.Argument))
        {
            yield return new Paragraph(new Run(eintrag.Text))
            {
                FontSize = TextStyles.TocEntrySize,
                FontWeight = eintrag.Level == 1 ? FontWeights.SemiBold : FontWeights.Normal,
                Margin = new Thickness((eintrag.Level - 1) * 18, 0, 0, 2),
            };
        }
    }

    private static Paragraph AbsatzUmwandeln(TdParagraph p, Zustand zustand)
    {
        var absatz = new Paragraph();

        AbsatzformatSetzen(absatz, p.Format.Over(zustand.Doc.DefaultParaFormat));
        ZeichenformatSetzen(absatz, p.CharFormat);

        foreach (var inline in p.Inlines) StueckUmwandeln(inline, absatz.Inlines, zustand);
        return absatz;
    }

    /// <summary>
    /// <b>Die Kaskade wird hier aufgelöst und nicht durch Null ersetzt</b> — das ist die eine
    /// Regel, an der dieser Weg vorher gescheitert ist (HANDOFF §4.45).
    ///
    /// <para>
    /// <b>Der Fehler, den es behebt:</b> Ein Feld von <see cref="TdParaFormat"/> ist
    /// <c>null</c>, solange es <i>nicht gesetzt</i> ist — es gilt dann der Wert aus
    /// <see cref="TdParaFormat.Standard"/>, und der ist bei <c>SpaceAfterPt</c> eben
    /// <b>8</b> und nicht 0. Wer hier <c>f.SpaceAfterPt ?? 0</c> schreibt, zeigt jedes vom
    /// Linux-Kopf angelegte Dokument mit **aneinandergeklebten Absätzen** — und weil
    /// <see cref="FlowZuTd"/> die so gesetzten Werte als örtlich gesetzt zurückliest, steht
    /// die Null danach **fest im Modell**. Dasselbe bei der Ausrichtung: nicht gesetzt heißt
    /// linksbündig, ein <c>FlowDocument</c> steht aber von Haus aus auf <c>Justify</c>.
    /// </para>
    /// <para>
    /// <b>Der Preis ist beabsichtigt:</b> Nach dieser Auflösung steht an jedem Absatz ein
    /// örtlicher Wert. Den Weg zurück muss <see cref="FlowZuTd"/> deshalb genau umkehren — es
    /// schreibt nur, was vom aufgelösten Standard <i>abweicht</i>. Die beiden Stellen sind
    /// Umkehrungen voneinander; wer eine ändert, muss die andere mitziehen.
    /// </para>
    /// </summary>
    private static void AbsatzformatSetzen(Paragraph absatz, TdParaFormat f)
    {
        var auf = f.Aufgeloest();

        absatz.TextAlignment = auf.Alignment switch
        {
            TdAlign.Center => TextAlignment.Center,
            TdAlign.Right => TextAlignment.Right,
            TdAlign.Justify => TextAlignment.Justify,
            _ => TextAlignment.Left,
        };

        // **Einzüge und Abstände sitzen im Altformat alle im Margin** — die Einzüge in
        // Pixeln, die Abstände in Punkt umgerechnet. Genau diese Aufteilung liest
        // FlowZuTd zurück.
        absatz.Margin = new Thickness(
            Px(auf.LeftIndentCm!.Value), PxAusPt(auf.SpaceBeforePt!.Value),
            Px(auf.RightIndentCm!.Value), PxAusPt(auf.SpaceAfterPt!.Value));

        absatz.TextIndent = Px(auf.FirstLineIndentCm!.Value);
        absatz.KeepWithNext = auf.KeepWithNext!.Value;
        absatz.BreakPageBefore = auf.PageBreakBefore!.Value;

        // Der Zeilenabstand steht im Altformat als absolute Höhe. `FontSize` ist hier noch die
        // geerbte — deshalb wird er erst in ZeichenformatSetzen endgültig gerechnet.
        if (Math.Abs(auf.LineSpacing!.Value - 1) > 0.001)
            absatz.LineHeight = absatz.FontSize * auf.LineSpacing.Value;

        // Die Trennlinie: im Modell eine Angabe am Absatz, im FlowDocument ein Rahmen an
        // seinem unteren Ende. Der Blockbehälter des Editors bleibt lesbar (FlowZuTd kennt
        // beide Formen) — geschrieben wird die Form, die man auswählen und löschen kann.
        if (f.BottomBorder is { Sichtbar: true } linie)
        {
            absatz.BorderThickness = new Thickness(0, 0, 0, PxAusPt(linie.WidthPt));
            absatz.BorderBrush = new SolidColorBrush(Farbe(linie.Color) ?? Colors.Silver);
        }
    }

    /// <summary>
    /// Setzt ein Zeichenformat an ein Element — <b>nur seine gesetzten Werte</b>.
    /// <para>
    /// Das ist die Regel aus §4.14, hier andersherum gelesen: Was im Modell <c>null</c> ist,
    /// darf im <c>FlowDocument</c> nicht gesetzt werden, sonst trägt jeder Lauf eine
    /// vollständige Formatkopie und erbt nichts mehr von seinem Absatz.
    /// </para>
    /// </summary>
    private static void ZeichenformatSetzen(System.Windows.Documents.TextElement e, TdCharFormat f)
    {
        if (f.FontFamily is { Length: > 0 } schrift) e.FontFamily = new FontFamily(schrift);
        if (f.FontSize is { } groesse) e.FontSize = PxAusPt(groesse);
        if (f.Bold is { } fett) e.FontWeight = fett ? FontWeights.Bold : FontWeights.Normal;
        if (f.Italic is { } kursiv) e.FontStyle = kursiv ? FontStyles.Italic : FontStyles.Normal;
        if (Farbe(f.Color) is { } vorne) e.Foreground = new SolidColorBrush(vorne);

        // `""` heißt „ausdrücklich keine Hervorhebung", `null` heißt „nicht gesetzt" (§4.14).
        if (f.Highlight is { Length: > 0 } && Farbe(f.Highlight) is { } hinten)
            e.Background = new SolidColorBrush(hinten);

        if (f.Underline == true || f.Strikethrough == true)
        {
            var striche = new TextDecorationCollection();
            if (f.Underline == true) striche.Add(TextDecorations.Underline);
            if (f.Strikethrough == true) striche.Add(TextDecorations.Strikethrough);
            if (e is Inline stueck) stueck.TextDecorations = striche;
            else if (e is Paragraph absatz) absatz.TextDecorations = striche;
        }

        if (f.VerticalAlign is { } lage && lage != TdVerticalAlign.Normal && e is Inline il)
            il.BaselineAlignment = lage == TdVerticalAlign.Superscript
                ? BaselineAlignment.Superscript
                : BaselineAlignment.Subscript;
    }

    // ==================== Tabellen ====================

    /// <summary>
    /// <b>Die Umrechnung aus §4.22, rückwärts:</b> Das Modell führt <c>Restart</c> +
    /// <c>Continue</c> je Zeile, WPF führt <c>RowSpan</c> an der Startzelle. Eine
    /// Fortsetzungszelle bekommt deshalb **keine** eigene Zelle — sie erhöht den Verbund
    /// darüber.
    /// </summary>
    private static Table TabelleUmwandeln(TdTable t, Zustand zustand)
    {
        // **Der Rahmen kommt aus dem Modell und nicht aus einer festen Vorgabe** (§4.45). Er
        // stand hier bis dahin auf Grau/0,5 px — und weil FlowZuTd ihn zurückliest, wurde aus
        // einer schwarzen 0,5-pt-Linie auf dem Rückweg still eine graue 0,375-pt-Linie. Der
        // Fehler war unsichtbar, solange `Rtf` führte, und wäre mit Schritt 7 in jedem
        // gespeicherten Dokument gelandet.
        var aussen = t.Format.Top;
        var tabelle = new Table
        {
            CellSpacing = 0,
            BorderBrush = new SolidColorBrush(Farbe(aussen.Color) ?? Colors.Black),
            BorderThickness = new Thickness(PxAusPt(aussen.WidthPt)),
            Margin = new Thickness(0, 6, 0, 6),
        };

        foreach (double breite in t.ColumnWidthsCm)
            tabelle.Columns.Add(new TableColumn
            {
                Width = breite > 0 ? new GridLength(Px(breite)) : GridLength.Auto,
            });

        var gruppe = new TableRowGroup();
        tabelle.RowGroups.Add(gruppe);

        var innen = t.Format.InsideH;

        // Je Rasterspalte die Startzelle eines noch offenen senkrechten Verbunds.
        var offen = new Dictionary<int, TableCell>();

        foreach (var zeile in t.Rows)
        {
            var tr = new TableRow();
            int spalte = 0;
            var beruehrt = new HashSet<int>();

            foreach (var zelle in zeile.Cells)
            {
                int spannweite = Math.Max(1, zelle.ColumnSpan);

                if (zelle.IstFortsetzung)
                {
                    if (offen.TryGetValue(spalte, out var start)) start.RowSpan++;
                    beruehrt.Add(spalte);
                    spalte += spannweite;
                    continue;
                }

                // Innenlinie und Zellabstand ebenso aus dem Modell. `FlowZuTd` liest beides an
                // der **ersten** Zelle ab (`RahmenUebernehmen`, `ZellabstandUebernehmen`) —
                // hier stehen sie deshalb an jeder, damit die erste kein Sonderfall ist.
                var tc = new TableCell
                {
                    BorderBrush = new SolidColorBrush(Farbe(innen.Color) ?? Colors.Black),
                    BorderThickness = new Thickness(PxAusPt(innen.WidthPt)),
                    Padding = new Thickness(
                        Px(t.Format.CellPaddingLeftCm), Px(t.Format.CellPaddingTopCm),
                        Px(t.Format.CellPaddingRightCm), Px(t.Format.CellPaddingBottomCm)),
                };
                if (spannweite > 1) tc.ColumnSpan = spannweite;
                if (Farbe(zelle.Shading) is { } fuellung) tc.Background = new SolidColorBrush(fuellung);

                if (zelle.VerticalMerge == TdVerticalMerge.Restart)
                {
                    offen[spalte] = tc;
                    beruehrt.Add(spalte);
                }

                BloeckeUmwandeln(zelle.Blocks, tc.Blocks, zustand);

                // **Eine Zelle ohne Absatz ist auch in WPF keine Zelle** — dieselbe Regel wie
                // im Schema von DOCX (§4.18).
                if (tc.Blocks.Count == 0) tc.Blocks.Add(new Paragraph());

                tr.Cells.Add(tc);
                spalte += spannweite;
            }

            foreach (int k in offen.Keys.Where(k => !beruehrt.Contains(k)).ToList()) offen.Remove(k);
            gruppe.Rows.Add(tr);
        }

        return tabelle;
    }

    // ==================== Textstücke ====================

    private static void StueckUmwandeln(TdInline inline, InlineCollection ziel, Zustand zustand)
    {
        switch (inline)
        {
            // **Der Verweis steht vor allem anderen** — dieselbe Erbfolge wie überall (§4.20).
            case TdHyperlink verweis:
            {
                var link = new Hyperlink();
                ZeichenformatSetzen(link, verweis.Format);
                foreach (var stueck in verweis.Inlines) StueckUmwandeln(stueck, link.Inlines, zustand);

                // **Das Ziel bleibt wörtlich**: relativ bleibt relativ (§4.20). Ein Ziel, das
                // sich nicht als Uri lesen lässt, kostet den Verweis und nicht den Text.
                if (verweis.Target.Length > 0 &&
                    Uri.TryCreate(verweis.Target, UriKind.RelativeOrAbsolute, out var uri))
                {
                    link.NavigateUri = uri;
                }

                ziel.Add(link);
                break;
            }

            case TdRun lauf when lauf.Text.Length > 0:
            {
                var run = new Run(lauf.Text);
                ZeichenformatSetzen(run, lauf.Format);
                ziel.Add(run);
                break;
            }

            case TdLineBreak:
                ziel.Add(new LineBreak());
                break;

            case TdImage bild when BildUmwandeln(bild, zustand) is { } element:
                ziel.Add(new InlineUIContainer(element));
                break;

            // Ein Feld hat im Editor kein Gegenstück. Sein Platzhalter ist das, was der Editor
            // an dieser Stelle ohnehin zeigt — und was FlowZuTd nicht wieder zu einem Feld
            // macht: das kann erst der Editor, der Felder kennt.
            case TdField feld when TdField.PlatzhalterVonArt(feld.Kind) is { } platzhalter:
            {
                var run = new Run(platzhalter);
                ZeichenformatSetzen(run, feld.Format);
                ziel.Add(run);
                break;
            }
        }
    }

    /// <summary>
    /// Ein Bild: angezeigt wird eine verkleinerte Ableitung, im Blob-Speicher bleibt das
    /// Original — dieselbe Aufteilung wie beim Import (<see cref="DocumentImages"/>, §4.21).
    /// <b>Fehlt der Blob, fällt dieses eine Bild weg</b> und nicht das Dokument: das ist eine
    /// unvollständige Sicherung und kein Programmierfehler (Dauerregel 4).
    /// </summary>
    private static Image? BildUmwandeln(TdImage bild, Zustand zustand)
    {
        if (zustand.Blobs.Read(bild.BlobId) is not { } bytes) return null;
        if (DocumentImages.Proxy(bytes) is not { } ableitung) return null;

        var element = new Image
        {
            Source = ableitung,
            Width = Px(bild.WidthCm),
            Height = Px(bild.HeightCm),
            Stretch = Stretch.Uniform,
        };
        // **Kein ToolTip für den Alternativtext**: Dort steht beim Speichern der Blob-Verweis
        // (DocumentImages.Detach), und zwei Bedeutungen für dasselbe Feld heben einander auf.
        // Der Alternativtext überlebt im Modell.

        // Der Verweis zeigt auf den **vorhandenen** Blob; ein zweites Ablegen derselben Bytes
        // wäre dasselbe Bild ein zweites Mal im Speicher.
        element.Tag = new BlobRef(bild.BlobId, bild.Extension);
        return element;
    }

    /// <summary>„#RRGGBB" als Farbe — oder <c>null</c>, wenn dort nichts Brauchbares steht.</summary>
    private static Color? Farbe(string? hex)
    {
        if (hex is not { Length: > 0 }) return null;
        try
        {
            return ColorConverter.ConvertFromString(hex.StartsWith('#') ? hex : "#" + hex) is Color c
                ? c
                : null;
        }
        catch
        {
            return null;   // eine unlesbare Farbe kostet die Farbe, nicht den Text
        }
    }
}
