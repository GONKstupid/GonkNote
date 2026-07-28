using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace GonkNote.Services;

/// <summary>
/// Setzt Markdown in ein <see cref="FlowDocument"/> um — gerade so viel, wie das README
/// von Gonk Note braucht: Überschriften, Absätze, Aufzählungen (auch verschachtelt),
/// Tabellen, Zitate, Code-Blöcke, Trennlinien sowie <c>**fett**</c>, <c>*kursiv*</c>,
/// <c>`Code`</c> und Links.
/// <para>
/// Bewusst kein Markdown-Paket: der Umfang ist klein und überschaubar, und das Projekt
/// bleibt ohne zusätzliche Abhängigkeit samt Lizenzvermerk. Was hier nicht erkannt wird,
/// landet als normaler Text im Dokument — der Dialog kann also nie leer bleiben.
/// </para>
/// Farben kommen über <c>SetResourceReference</c> aus dem Theme, damit der Dialog beim
/// Umschalten zwischen Hell und Dunkel mitzieht.
/// </summary>
public static class MarkdownFlow
{
    private const string FontUi = "Segoe UI";
    private const string FontMono = "Consolas";

    /// <summary>
    /// Behandelt Verweise auf andere Dokumente im Projekt (z. B. <c>ERSTE-SCHRITTE.md</c>).
    /// Wird für die Dauer eines <see cref="ToFlowDocument"/>-Laufs gesetzt, damit die
    /// rekursiven Hilfsmethoden ihn nicht als Parameter durchreichen müssen; der vorige
    /// Wert wird wiederhergestellt, weil Zitatblöcke sich selbst erneut aufrufen.
    /// </summary>
    [System.ThreadStatic] private static Action<string>? _docLink;

    /// <param name="onDocumentLink">
    /// Wird mit dem Linkziel aufgerufen, wenn im Text ein Verweis auf eine andere
    /// <c>.md</c>-Datei angeklickt wird. Ohne Handler bleiben solche Verweise Text.
    /// </param>
    public static FlowDocument ToFlowDocument(string markdown, Action<string>? onDocumentLink = null)
    {
        var previous = _docLink;
        if (onDocumentLink != null) _docLink = onDocumentLink;
        try { return Build(markdown); }
        finally { _docLink = previous; }
    }

    private static FlowDocument Build(string markdown)
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

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i];

            if (line.TrimStart().StartsWith("```")) { doc.Blocks.Add(CodeBlock(lines, ref i)); continue; }
            if (IsRule(line)) { doc.Blocks.Add(Rule()); i++; continue; }

            var head = Regex.Match(line, @"^(#{1,6})\s+(.*)$");
            if (head.Success) { doc.Blocks.Add(Heading(head.Groups[1].Value.Length, head.Groups[2].Value)); i++; continue; }

            if (IsTableRow(line) && i + 1 < lines.Length && IsTableSeparator(lines[i + 1]))
            { doc.Blocks.Add(BuildTable(lines, ref i)); continue; }

            if (line.StartsWith(">")) { doc.Blocks.Add(Quote(lines, ref i)); continue; }
            if (ListMatch(line).Success) { doc.Blocks.Add(BuildList(lines, ref i)); continue; }

            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

            doc.Blocks.Add(TextParagraph(lines, ref i));
        }
        return doc;
    }

    // ---------------------------------------------------------------- Blöcke

    private static bool IsRule(string s) => Regex.IsMatch(s.Trim(), @"^(-{3,}|\*{3,}|_{3,})$");

    private static bool IsTableRow(string s) => s.TrimStart().StartsWith("|");

    private static bool IsTableSeparator(string s) =>
        Regex.IsMatch(s.Trim(), @"^\|(\s*:?-{2,}:?\s*\|)+$");

    private static Match ListMatch(string s) => Regex.Match(s, @"^(\s*)([-*+]|\d+\.)\s+(.*)$");

    private static Block Heading(int level, string text)
    {
        var p = new Paragraph(Inline(text))
        {
            FontSize = level switch { 1 => 21, 2 => 17, 3 => 15, _ => 13.5 },
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, level == 1 ? 0 : 16, 0, 6),
        };
        if (level >= 3) p.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent");
        return p;
    }

    private static Block Rule()
    {
        var b = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 10, 0, 10) };
        b.SetResourceReference(Border.BorderBrushProperty, "Brush.Border");
        return new BlockUIContainer(b) { Margin = new Thickness(0) };
    }

    private static Block CodeBlock(string[] lines, ref int i)
    {
        i++;                                  // öffnende ```
        var body = new List<string>();
        while (i < lines.Length && !lines[i].TrimStart().StartsWith("```")) body.Add(lines[i++]);
        if (i < lines.Length) i++;            // schließende ```

        var p = new Paragraph(new Run(string.Join("\n", body)))
        {
            FontFamily = new FontFamily(FontMono),
            FontSize = 12,
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 6, 0, 10),
        };
        p.SetResourceReference(TextElement.BackgroundProperty, "Brush.WindowBg");
        return p;
    }

    private static Block Quote(string[] lines, ref int i)
    {
        var body = new List<string>();
        while (i < lines.Length && lines[i].StartsWith(">"))
            body.Add(Regex.Replace(lines[i++], @"^>\s?", ""));

        var inner = ToFlowDocument(string.Join("\n", body));
        var section = new Section { Padding = new Thickness(12, 2, 0, 2), Margin = new Thickness(0, 6, 0, 10) };
        section.SetResourceReference(TextElement.ForegroundProperty, "Brush.TextMuted");
        section.BorderThickness = new Thickness(3, 0, 0, 0);
        section.SetResourceReference(Section.BorderBrushProperty, "Brush.Accent");
        while (inner.Blocks.Count > 0) section.Blocks.Add(inner.Blocks.FirstBlock);
        return section;
    }

    private static Block BuildList(string[] lines, ref int i)
    {
        int baseIndent = ListMatch(lines[i]).Groups[1].Value.Length;
        var list = NewList(lines[i]);

        while (i < lines.Length)
        {
            var m = ListMatch(lines[i]);
            if (!m.Success) break;

            int indent = m.Groups[1].Value.Length;
            if (indent < baseIndent) break;

            if (indent > baseIndent)
            {
                // Untereintrag: an den letzten Punkt hängen statt einen neuen zu beginnen
                var sub = BuildList(lines, ref i);
                if (list.ListItems.LastListItem is { } last) last.Blocks.Add(sub);
                else list.ListItems.Add(new ListItem(new Paragraph()) { });
                continue;
            }

            string text = m.Groups[3].Value;
            i++;
            // Fortsetzungszeilen eines Punktes (eingerückt, aber kein neuer Punkt)
            while (i < lines.Length && !ListMatch(lines[i]).Success &&
                   !string.IsNullOrWhiteSpace(lines[i]) && lines[i].StartsWith(" "))
                text += " " + lines[i++].Trim();

            list.ListItems.Add(new ListItem(new Paragraph(Inline(text)) { Margin = new Thickness(0, 1, 0, 1) }));
        }
        return list;
    }

    private static List NewList(string firstLine) => new()
    {
        MarkerStyle = Regex.IsMatch(firstLine.TrimStart(), @"^\d+\.")
            ? TextMarkerStyle.Decimal
            : TextMarkerStyle.Disc,
        Margin = new Thickness(0, 4, 0, 8),
        // Breit genug für zweistellige Nummern — bei 20 schnitt WPF die führende
        // Ziffer ab, aus „10." wurde „0.".
        Padding = new Thickness(32, 0, 0, 0),
    };

    private static Block BuildTable(string[] lines, ref int i)
    {
        var head = SplitRow(lines[i]);
        i += 2;                                   // Kopfzeile + Trennzeile

        var rows = new List<string[]>();
        while (i < lines.Length && IsTableRow(lines[i])) rows.Add(SplitRow(lines[i++]));

        int cols = head.Length;
        foreach (var r in rows) cols = System.Math.Max(cols, r.Length);

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 6, 0, 10) };
        for (int c = 0; c < cols; c++) table.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        group.Rows.Add(Row(head, cols, header: true));
        foreach (var r in rows) group.Rows.Add(Row(r, cols, header: false));
        return table;
    }

    private static TableRow Row(string[] cells, int cols, bool header)
    {
        var row = new TableRow();
        if (header) row.FontWeight = FontWeights.SemiBold;
        for (int c = 0; c < cols; c++)
        {
            var cell = new TableCell(new Paragraph(Inline(c < cells.Length ? cells[c] : "")))
            {
                Padding = new Thickness(8, 4, 8, 4),
                BorderThickness = new Thickness(0, 0, 0, 1),
            };
            cell.SetResourceReference(TableCell.BorderBrushProperty, "Brush.Border");
            row.Cells.Add(cell);
        }
        return row;
    }

    private static string[] SplitRow(string line)
    {
        string s = line.Trim();
        if (s.StartsWith("|")) s = s.Substring(1);
        if (s.EndsWith("|")) s = s.Substring(0, s.Length - 1);
        var parts = s.Split('|');
        for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
        return parts;
    }

    private static Block TextParagraph(string[] lines, ref int i)
    {
        var body = new List<string>();
        while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]) &&
               !lines[i].TrimStart().StartsWith("```") && !lines[i].StartsWith(">") &&
               !IsRule(lines[i]) && !IsTableRow(lines[i]) &&
               !Regex.IsMatch(lines[i], @"^#{1,6}\s") && !ListMatch(lines[i]).Success)
            body.Add(lines[i++].Trim());

        return new Paragraph(Inline(string.Join(" ", body))) { Margin = new Thickness(0, 0, 0, 8) };
    }

    // ---------------------------------------------------------------- Inline

    // Reihenfolge zählt: `Code` zuerst, damit Sternchen darin wörtlich bleiben; fett vor
    // kursiv, sonst reisst ** in zwei * auseinander. Fett darf Sternchen enthalten
    // („**fett mit *kursiv* darin**") — es läuft bis zum nächsten **, nicht bis zum
    // nächsten Sternchen.
    private static readonly Regex InlinePattern = new(
        @"(?<code>`(?<codeIn>[^`]+)`)" +
        @"|(?<link>\[(?<linkText>[^\]]+)\]\((?<linkUrl>[^)]+)\))" +
        @"|(?<bold>\*\*(?<boldIn>(?:(?!\*\*).)+?)\*\*)" +
        @"|(?<em>(?<!\*)\*(?!\*)(?<emIn>[^*]+)\*(?!\*))",
        RegexOptions.Compiled);

    private static Span Inline(string text)
    {
        var span = new Span();
        int pos = 0;
        foreach (Match m in InlinePattern.Matches(text))
        {
            if (m.Index > pos) span.Inlines.Add(new Run(Unescape(text.Substring(pos, m.Index - pos))));

            if (m.Groups["code"].Success)
            {
                var run = new Run(m.Groups["codeIn"].Value)
                {
                    FontFamily = new FontFamily(FontMono),
                    FontSize = 12,
                };
                run.SetResourceReference(TextElement.BackgroundProperty, "Brush.WindowBg");
                span.Inlines.Add(run);
            }
            else if (m.Groups["link"].Success)
                span.Inlines.Add(Link(m.Groups["linkText"].Value, m.Groups["linkUrl"].Value));
            else if (m.Groups["bold"].Success)
                span.Inlines.Add(new Bold(Inline(m.Groups["boldIn"].Value)));
            else
                span.Inlines.Add(new Italic(Inline(m.Groups["emIn"].Value)));

            pos = m.Index + m.Length;
        }
        if (pos < text.Length) span.Inlines.Add(new Run(Unescape(text.Substring(pos))));
        return span;
    }

    /// <summary>
    /// Web-Links öffnen den Browser. Verweise auf andere <c>.md</c>-Dateien gehen an den
    /// Handler aus <see cref="ToFlowDocument"/> — so landet „Erste Schritte" im README
    /// beim passenden Dialog statt im Nichts. Alles Übrige bleibt Text.
    /// </summary>
    private static Inline Link(string text, string url)
    {
        if (url.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
        {
            var web = new Hyperlink(new Run(text)) { NavigateUri = new System.Uri(url), ToolTip = url };
            web.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent");
            web.RequestNavigate += OnNavigate;
            return web;
        }

        var handler = _docLink;
        if (handler != null && url.EndsWith(".md", System.StringComparison.OrdinalIgnoreCase))
        {
            string target = url;
            var doc = new Hyperlink(new Run(text));
            doc.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent");
            doc.Click += (_, _) => handler(target);
            return doc;
        }

        var plain = new Run(text);
        plain.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent");
        return plain;
    }

    private static void OnNavigate(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch { /* kein Browser da: der Dialog soll deswegen nicht abstuerzen */ }
        e.Handled = true;
    }

    private static string Unescape(string s) => s.Replace("\\*", "*").Replace("\\_", "_");
}
