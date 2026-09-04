using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using GonkNote.Core.Text;

namespace GonkNote.Services;

/// <summary>
/// Malt das, was <see cref="Markdown"/> zerlegt hat — als <see cref="FlowDocument"/>.
/// <para>
/// <b>Die Grammatik steht seit Phase 3 in <c>Core/Text/Markdown.cs</c></b> und nicht mehr
/// hier. Bis dahin waren Zerlegen und Darstellen in dieser Datei dasselbe, weil das Ergebnis
/// unmittelbar ein <c>FlowDocument</c> ist; der Linux-Kopf hat keines (HANDOFF §4.1) und
/// hätte die Grammatik ein zweites Mal abschreiben müssen. Zwei Fassungen derselben Formel
/// driften auseinander, ohne dass es auffällt — deshalb zerlegt Core, und jeder Kopf malt
/// nur noch. Das Gegenstück im Linux-Kopf ist <c>Views/MarkdownView.cs</c>.
/// </para>
/// <para>
/// <b>Mitgekommen ist eine Endlosschleife:</b> eine Tabellenzeile ohne Trennzeile darunter
/// ist keine Tabelle, landete im Absatz-Zweig und wurde dort selbst wieder abgewiesen — der
/// Absatz blieb leer, der Zeilenzähler stand still. In den mitgelieferten Dokumenten steht
/// heute keine solche Zeile, deshalb hat es hier nie zugeschlagen. Wächter:
/// <c>Eine_Tabelle_braucht_ihre_Trennzeile</c> (HANDOFF §4.12).
/// </para>
/// Farben kommen über <c>SetResourceReference</c> aus dem Theme, damit der Dialog beim
/// Umschalten zwischen Hell und Dunkel mitzieht.
/// </summary>
public static class MarkdownFlow
{
    // Die Namen stehen im Schriftschema in Core und nicht hier (HANDOFF §4.26) — sonst gäbe
    // es eine zweite Liste, und eine davon veraltet.
    private static readonly string FontUi =
        GonkNote.Core.Theming.Fonts.Standard.Family(GonkNote.Core.Theming.FontRole.Ui);

    private static readonly string FontMono =
        GonkNote.Core.Theming.Fonts.Standard.Family(GonkNote.Core.Theming.FontRole.Mono);

    /// <param name="onDocumentLink">
    /// Wird mit dem Linkziel aufgerufen, wenn im Text ein Verweis auf eine andere
    /// <c>.md</c>-Datei angeklickt wird. Ohne Handler bleiben solche Verweise Text.
    /// </param>
    public static FlowDocument ToFlowDocument(string markdown, Dokumentverweise? onDocumentLink = null)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily(FontUi),
            FontSize = 13,
            PagePadding = new Thickness(0),
            LineHeight = 19,
            // WPF setzt FlowDocuments sonst im Blocksatz — das reisst bei schmaler
            // Spalte haessliche Luecken zwischen die Woerter.
            TextAlignment = TextAlignment.Left,
        };
        doc.SetResourceReference(FlowDocument.ForegroundProperty, "Brush.Text");

        foreach (var block in Markdown.Parse(markdown))
            doc.Blocks.Add(Block(block, onDocumentLink));

        return doc;
    }

    // ---------------------------------------------------------------- Blöcke

    private static Block Block(MdBlock block, Dokumentverweise? link) => block switch
    {
        MdHeading h => Heading(h, link),
        MdParagraph p => new Paragraph(Inline(p.Inlines, link)) { Margin = new Thickness(0, 0, 0, 8) },
        MdCodeBlock c => CodeBlock(c),
        MdRule => Rule(),
        MdQuote q => Quote(q, link),
        MdList l => BuildList(l, link),
        MdTable t => BuildTable(t, link),
        _ => new Paragraph(),
    };

    private static Block Heading(MdHeading h, Dokumentverweise? link)
    {
        var p = new Paragraph(Inline(h.Inlines, link))
        {
            FontSize = h.Level switch { 1 => 21, 2 => 17, 3 => 15, _ => 13.5 },
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, h.Level == 1 ? 0 : 16, 0, 6),
        };
        if (h.Level >= 3) p.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent");
        return p;
    }

    private static Block Rule()
    {
        var b = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 10, 0, 10) };
        b.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        return new BlockUIContainer(b) { Margin = new Thickness(0) };
    }

    private static Block CodeBlock(MdCodeBlock c)
    {
        var p = new Paragraph(new Run(c.Text))
        {
            FontFamily = new FontFamily(FontMono),
            FontSize = 12,
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 6, 0, 10),
        };
        p.SetResourceReference(TextElement.BackgroundProperty, "Brush.WindowBg");
        return p;
    }

    /// <summary>
    /// Ein Zitat ist wieder ein ganzes Dokument — <see cref="Markdown"/> hat seinen Inhalt
    /// schon als Blockliste geliefert, hier wird sie nur eingerückt und eingefärbt.
    /// </summary>
    private static Block Quote(MdQuote q, Dokumentverweise? link)
    {
        var section = new Section { Padding = new Thickness(12, 2, 0, 2), Margin = new Thickness(0, 6, 0, 10) };
        section.SetResourceReference(TextElement.ForegroundProperty, "Brush.TextMuted");
        section.BorderThickness = new Thickness(3, 0, 0, 0);
        section.SetResourceReference(Section.BorderBrushProperty, "Brush.Accent");

        foreach (var b in q.Blocks) section.Blocks.Add(Block(b, link));
        return section;
    }

    private static Block BuildList(MdList liste, Dokumentverweise? link)
    {
        var list = new List
        {
            MarkerStyle = liste.Ordered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 4, 0, 8),
            // Breit genug für zweistellige Nummern — bei 20 schnitt WPF die führende
            // Ziffer ab, aus „10." wurde „0.".
            Padding = new Thickness(32, 0, 0, 0),
        };

        foreach (var punkt in liste.Items)
        {
            var item = new ListItem(new Paragraph(Inline(punkt.Inlines, link))
            {
                Margin = new Thickness(0, 1, 0, 1),
            });
            // Untereintrag: an den Punkt hängen, statt einen neuen zu beginnen.
            if (punkt.Sub is { } unter) item.Blocks.Add(BuildList(unter, link));
            list.ListItems.Add(item);
        }
        return list;
    }

    private static Block BuildTable(MdTable t, Dokumentverweise? link)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 6, 0, 10) };
        for (int c = 0; c < t.Columns; c++) table.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        group.Rows.Add(Row(t.Header, t.Columns, header: true, link));
        foreach (var r in t.Rows) group.Rows.Add(Row(r, t.Columns, header: false, link));
        return table;
    }

    private static TableRow Row(IReadOnlyList<IReadOnlyList<MdInline>> cells, int cols, bool header,
        Dokumentverweise? link)
    {
        var row = new TableRow();
        if (header) row.FontWeight = FontWeights.SemiBold;
        for (int c = 0; c < cols; c++)
        {
            var cell = new TableCell(new Paragraph(Inline(c < cells.Count ? cells[c] : [], link)))
            {
                Padding = new Thickness(8, 4, 8, 4),
                BorderThickness = new Thickness(0, 0, 0, 1),
            };
            cell.SetResourceReference(TableCell.BorderBrushProperty, "Brush.Border");
            row.Cells.Add(cell);
        }
        return row;
    }

    // ---------------------------------------------------------------- Textstücke

    private static Span Inline(IReadOnlyList<MdInline> stuecke, Dokumentverweise? link)
    {
        var span = new Span();
        foreach (var s in stuecke)
        {
            switch (s)
            {
                case MdText t:
                    span.Inlines.Add(new Run(t.Text));
                    break;

                case MdCodeSpan c:
                    var run = new Run(c.Text) { FontFamily = new FontFamily(FontMono), FontSize = 12 };
                    run.SetResourceReference(TextElement.BackgroundProperty, "Brush.WindowBg");
                    span.Inlines.Add(run);
                    break;

                case MdBold b:
                    span.Inlines.Add(new Bold(Inline(b.Inner, link)));
                    break;

                case MdItalic k:
                    span.Inlines.Add(new Italic(Inline(k.Inner, link)));
                    break;

                case MdStrike d:
                    var durch = new Span(Inline(d.Inner, link))
                    {
                        TextDecorations = TextDecorations.Strikethrough,
                    };
                    span.Inlines.Add(durch);
                    break;

                case MdImage bild:
                    span.Inlines.Add(Bildersatz(bild));
                    break;

                case MdLink l:
                    span.Inlines.Add(Link(l, link));
                    break;
            }
        }
        return span;
    }

    /// <summary>
    /// <b>Ein Bild wird hier nicht geladen, sondern durch seinen Ersatztext vertreten</b> —
    /// <c>[Bildschirmfoto]</c> in der gedämpften Farbe.
    ///
    /// <para>
    /// <b>Warum überhaupt etwas dasteht</b> (Phase 5, Schritt ④): Seit der Markdown-Import
    /// nach Core gezogen ist (<see cref="TdMarkdown"/>), kennt der Zerleger
    /// <see cref="MdImage"/> — und dieser Maler hätte es <b>stillschweigend weggelassen</b>.
    /// Das fällt heute niemandem auf, weil keines der vier mitgelieferten Dokumente ein Bild
    /// enthält; <b>Schritt ⑤ setzt aber Bildschirmfotos in beide READMEs</b>, und dann stünde
    /// im Hilfe-Fenster eine Lücke, wo ein Bild sein sollte.
    /// </para>
    /// <para>
    /// <b>Der Ersatztext und nicht das Bild</b>, und das ist eine Entscheidung: Dieses Fenster
    /// zeigt vier mitgelieferte Dokumente aus der eigenen Exe, kein fremdes Markdown. Ein
    /// Bildlader müsste den Pfad relativ zu einer Datei auflösen, die es als Datei gar nicht
    /// gibt (die Dokumente liegen als eingebettete Resource, §7). <b>Der Ersatztext sagt, dass
    /// dort etwas ist</b> — das Weglassen behauptete, dort sei nichts.
    /// </para>
    /// </summary>
    private static Inline Bildersatz(MdImage bild)
    {
        var run = new Run(bild.Alt.Length > 0 ? $"[{bild.Alt}]" : "[Bild]");
        run.SetResourceReference(TextElement.ForegroundProperty, "Brush.TextMuted");
        return run;
    }

    /// <summary>
    /// Web-Links öffnen den Browser. Verweise auf andere <c>.md</c>-Dateien gehen an den
    /// Handler aus <see cref="ToFlowDocument"/> — so landet „Erste Schritte" im README
    /// beim passenden Dialog statt im Nichts. Alles Übrige bleibt Text.
    /// </summary>
    private static Inline Link(MdLink l, Dokumentverweise? handler)
    {
        if (l.Target.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var web = new Hyperlink(new Run(l.Text)) { NavigateUri = new Uri(l.Target), ToolTip = l.Target };
            web.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent");
            web.RequestNavigate += OnNavigate;
            return web;
        }

        // **Erst fragen, dann zeichnen** (Phase 5, Schritt ④): `Kann` sagt, ob dieses Ziel
        // überhaupt jemand öffnet. Vorher wurde jedes `.md`-Ziel eingefärbt, auch wenn es
        // niemand annahm — fünf Verweise in den mitgelieferten Dokumenten sahen aus wie
        // Verweise und taten nichts (siehe `Dokumentverweise`).
        // ⛔ Hier stand zusätzlich `l.Target.EndsWith(".md")`, und das war beim ersten Anlauf
        // von ④ genau derselbe Fehler eine Etage höher: `README.md#zwei-ausgaben-eine-app`
        // endet **nicht** auf `.md`. Die Prüfung ist ersatzlos weg — `Kann` beantwortet die
        // Frage „ist das ein Dokumentverweis?" ohnehin genauer als eine Endung, und zwei
        // Stellen, die dasselbe entscheiden, entscheiden es verschieden.
        if (handler != null && handler.Kann(l.Target))
        {
            string target = l.Target;
            var doc = new Hyperlink(new Run(l.Text));
            doc.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent");
            doc.Click += (_, _) => handler.Oeffnen(target);
            return doc;
        }

        // **Text und keine Akzentfarbe.** Hier stand bis Schritt ④ ein eingefärbter Run —
        // und der war die eigentliche Ursache: Er ließ ein totes Ziel wie ein lebendes
        // aussehen, und niemand klickt zweimal, um sich zu vergewissern.
        return new Run(l.Text);
    }

    private static void OnNavigate(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch { /* kein Browser da: der Dialog soll deswegen nicht abstuerzen */ }
        e.Handled = true;
    }
}
