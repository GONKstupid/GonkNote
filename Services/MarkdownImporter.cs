using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GonkNote.Services;

/// <summary>
/// Best-effort-Import von Markdown in ein FlowDocument (Gegenrichtung zum
/// <see cref="MarkdownExporter"/>): Überschriften (#–#### → TextStyles-Vorlagen,
/// damit TOC/Navigator/Export sie erkennen), fett/kursiv/durchgestrichen,
/// Inline-Code, Links, Listen (auch verschachtelt), Pipe-Tabellen, Zitate,
/// Code-Blöcke, Trennlinien und lokale Bilder.
/// </summary>
public static class MarkdownImporter
{
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x1B, 0x2B, 0x4B));
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(0x6B, 0x7A, 0x99));
    private static readonly Brush CodeBg = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xF8));
    private static readonly Brush RuleBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xDE, 0xEA));

    /// <summary>Markdown-Datei als XamlPackage-Bytes (Speicherformat der Textdokumente).</summary>
    public static byte[] ToXamlPackage(string path)
    {
        var flow = ToFlowDocument(path);
        var range = new TextRange(flow.ContentStart, flow.ContentEnd);
        using var ms = new MemoryStream();
        range.Save(ms, DataFormats.XamlPackage, true);
        return ms.ToArray();
    }

    public static FlowDocument ToFlowDocument(string path)
    {
        string[] lines = File.ReadAllLines(path);
        string baseDir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? "";

        var flow = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = TextStyles.BodySize,
            Foreground = Ink,
            PagePadding = new Thickness(0),
        };

        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i];
            string trimmed = line.TrimStart();

            if (trimmed.Length == 0) { i++; continue; }

            // Code-Block (``` … ```)
            if (trimmed.StartsWith("```"))
            {
                var sb = new StringBuilder();
                i++;
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                    sb.AppendLine(lines[i++]);
                i++;   // schließendes ```
                flow.Blocks.Add(new Paragraph(new Run(sb.ToString().TrimEnd('\r', '\n')))
                {
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    Background = CodeBg,
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 4, 0, 8),
                });
                continue;
            }

            // Trennlinie
            if (Regex.IsMatch(trimmed, @"^(-{3,}|\*{3,}|_{3,})\s*$"))
            {
                flow.Blocks.Add(new Paragraph
                {
                    Margin = new Thickness(0, 8, 0, 8),
                    BorderBrush = RuleBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1.5),
                    FontSize = 2,
                });
                i++;
                continue;
            }

            // Überschrift (# … ####; mehr Rauten → Ebene 4)
            var h = Regex.Match(trimmed, @"^(#{1,6})\s+(.*)$");
            if (h.Success)
            {
                int level = Math.Min(4, h.Groups[1].Value.Length);
                var style = TextStyles.ForHeading(level);
                var p = new Paragraph
                {
                    FontSize = style.Size,
                    FontWeight = style.Weight,
                    FontStyle = style.Style,
                    Margin = style.Margin,
                };
                if (style.ColorHex != null)
                    p.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(style.ColorHex));
                AddInlines(p.Inlines, h.Groups[2].Value.Trim(), baseDir);
                flow.Blocks.Add(p);
                i++;
                continue;
            }

            // Tabelle (| a | b | mit Trennzeile in der 2. Zeile)
            if (trimmed.StartsWith("|") && i + 1 < lines.Length &&
                Regex.IsMatch(lines[i + 1].Trim(), @"^\|?\s*:?-{2,}.*\|"))
            {
                var rows = new List<string[]>();
                rows.Add(SplitTableRow(trimmed));
                i += 2;   // Kopfzeile + Trennzeile
                while (i < lines.Length && lines[i].TrimStart().StartsWith("|"))
                    rows.Add(SplitTableRow(lines[i++].Trim()));
                flow.Blocks.Add(BuildTable(rows, baseDir));
                continue;
            }

            // Liste (-/*/+ bzw. 1. …), Verschachtelung über Einrückung (2 Leerzeichen/Ebene)
            if (IsListLine(line, out _, out _, out _))
            {
                flow.Blocks.Add(ParseList(lines, ref i, IndentOf(line), baseDir));
                continue;
            }

            // Zitat
            if (trimmed.StartsWith(">"))
            {
                var p = new Paragraph
                {
                    Foreground = Muted,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(12, 4, 0, 6),
                    Padding = new Thickness(10, 0, 0, 0),
                    BorderBrush = RuleBrush,
                    BorderThickness = new Thickness(3, 0, 0, 0),
                };
                var quote = new StringBuilder(trimmed.TrimStart('>', ' '));
                i++;
                while (i < lines.Length && lines[i].TrimStart().StartsWith(">"))
                    quote.Append(' ').Append(lines[i++].TrimStart().TrimStart('>', ' '));
                AddInlines(p.Inlines, quote.ToString(), baseDir);
                flow.Blocks.Add(p);
                continue;
            }

            // Normaler Absatz: Folgezeilen bis zur Leerzeile zusammenfassen;
            // "  " am Zeilenende = harter Umbruch
            var para = new Paragraph { Margin = new Thickness(0, 2, 0, 6) };
            bool first = true;
            while (i < lines.Length && lines[i].Trim().Length > 0 &&
                   !lines[i].TrimStart().StartsWith("#") && !lines[i].TrimStart().StartsWith(">") &&
                   !lines[i].TrimStart().StartsWith("|") && !lines[i].TrimStart().StartsWith("```") &&
                   !IsListLine(lines[i], out _, out _, out _) &&
                   !Regex.IsMatch(lines[i].Trim(), @"^(-{3,}|\*{3,}|_{3,})\s*$"))
            {
                string text = lines[i].TrimEnd('\r');
                bool hardBreak = text.EndsWith("  ");
                if (!first) para.Inlines.Add(new Run(" "));
                AddInlines(para.Inlines, text.Trim(), baseDir);
                if (hardBreak) { para.Inlines.Add(new LineBreak()); first = true; }
                else first = false;
                i++;
            }
            flow.Blocks.Add(para);
        }

        if (flow.Blocks.Count == 0) flow.Blocks.Add(new Paragraph());
        return flow;
    }

    // ==================== Listen ====================

    private static bool IsListLine(string line, out bool ordered, out string content, out int number)
    {
        ordered = false; content = ""; number = 1;
        var un = Regex.Match(line, @"^\s*[-*+]\s+(.*)$");
        if (un.Success) { content = un.Groups[1].Value; return true; }
        var or = Regex.Match(line, @"^\s*(\d+)[.)]\s+(.*)$");
        if (or.Success) { ordered = true; number = int.Parse(or.Groups[1].Value); content = or.Groups[2].Value; return true; }
        return false;
    }

    private static int IndentOf(string line) => line.Length - line.TrimStart().Length;

    private static List ParseList(string[] lines, ref int i, int baseIndent, string baseDir)
    {
        IsListLine(lines[i], out bool ordered, out _, out _);
        var list = new List
        {
            MarkerStyle = ordered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 2, 0, 6),
            Padding = new Thickness(22, 0, 0, 0),
        };

        while (i < lines.Length && IsListLine(lines[i], out _, out string content, out _))
        {
            int indent = IndentOf(lines[i]);
            if (indent < baseIndent) break;              // Ebene zu Ende
            if (indent > baseIndent + 1)
            {
                // Verschachtelte Liste in das letzte ListItem hängen
                var nested = ParseList(lines, ref i, indent, baseDir);
                if (list.ListItems.LastListItem is { } last) last.Blocks.Add(nested);
                else { var li0 = new ListItem(); li0.Blocks.Add(nested); list.ListItems.Add(li0); }
                continue;
            }

            var p = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
            AddInlines(p.Inlines, content.Trim(), baseDir);
            var li = new ListItem();
            li.Blocks.Add(p);
            list.ListItems.Add(li);
            i++;
        }
        return list;
    }

    // ==================== Tabellen ====================

    private static string[] SplitTableRow(string line)
    {
        line = line.Trim();
        if (line.StartsWith("|")) line = line[1..];
        if (line.EndsWith("|")) line = line[..^1];
        // \| als Literal-Pipe zulassen
        return Regex.Split(line, @"(?<!\\)\|")
            .Select(c => c.Trim().Replace("\\|", "|"))
            .ToArray();
    }

    private static Table BuildTable(List<string[]> rows, string baseDir)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 6, 0, 6),
        };
        int cols = rows.Max(r => r.Length);
        for (int c = 0; c < cols; c++) table.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        for (int r = 0; r < rows.Count; r++)
        {
            var tr = new TableRow();
            for (int c = 0; c < cols; c++)
            {
                var p = new Paragraph { Margin = new Thickness(0) };
                if (c < rows[r].Length) AddInlines(p.Inlines, rows[r][c], baseDir);
                if (r == 0) p.FontWeight = FontWeights.SemiBold;   // Kopfzeile
                var cell = new TableCell(p)
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(6, 3, 6, 3),
                };
                if (r == 0) cell.Background = new SolidColorBrush(Color.FromArgb(255, 0xEE, 0xF2, 0xF8));
                tr.Cells.Add(cell);
            }
            group.Rows.Add(tr);
        }
        return table;
    }

    // ==================== Inlines ====================

    // Reihenfolge wichtig: erst Bilder/Links/Code, dann ***, **, *, ~~
    private static readonly Regex InlineToken = new(
        @"(!\[(?<ialt>[^\]]*)\]\((?<isrc>[^)]*)\))" +
        @"|(\[(?<ltext>[^\]]+)\]\((?<lhref>[^)]+)\))" +
        @"|(`(?<code>[^`]+)`)" +
        @"|(\*\*\*(?<bi>[^*]+)\*\*\*)" +
        @"|(\*\*(?<b>[^*]+)\*\*)" +
        @"|(__(?<b2>[^_]+)__)" +
        @"|(\*(?<i>[^*]+)\*)" +
        @"|(~~(?<s>[^~]+)~~)",
        RegexOptions.Compiled);

    private static void AddInlines(InlineCollection target, string text, string baseDir)
    {
        int pos = 0;
        foreach (Match m in InlineToken.Matches(text))
        {
            if (m.Index > pos) target.Add(new Run(text[pos..m.Index]));
            pos = m.Index + m.Length;

            if (m.Groups["isrc"].Success)
            {
                if (TryLoadImage(m.Groups["isrc"].Value, baseDir) is { } img)
                    target.Add(new InlineUIContainer(img));
                else if (m.Groups["ialt"].Value.Length > 0)
                    target.Add(new Run($"[{m.Groups["ialt"].Value}]") { Foreground = Muted });
            }
            else if (m.Groups["ltext"].Success)
            {
                var link = new Hyperlink(new Run(m.Groups["ltext"].Value));
                try { link.NavigateUri = new Uri(m.Groups["lhref"].Value, UriKind.RelativeOrAbsolute); }
                catch { /* ungültige URI → reiner Linktext */ }
                target.Add(link);
            }
            else if (m.Groups["code"].Success)
            {
                target.Add(new Run(m.Groups["code"].Value)
                {
                    FontFamily = new FontFamily("Consolas"),
                    Background = CodeBg,
                });
            }
            else if (m.Groups["bi"].Success)
                target.Add(new Run(m.Groups["bi"].Value) { FontWeight = FontWeights.Bold, FontStyle = FontStyles.Italic });
            else if (m.Groups["b"].Success)
                target.Add(new Run(m.Groups["b"].Value) { FontWeight = FontWeights.Bold });
            else if (m.Groups["b2"].Success)
                target.Add(new Run(m.Groups["b2"].Value) { FontWeight = FontWeights.Bold });
            else if (m.Groups["i"].Success)
                target.Add(new Run(m.Groups["i"].Value) { FontStyle = FontStyles.Italic });
            else if (m.Groups["s"].Success)
                target.Add(new Run(m.Groups["s"].Value) { TextDecorations = TextDecorations.Strikethrough });
        }
        if (pos < text.Length) target.Add(new Run(text[pos..]));
    }

    /// <summary>Lädt ein lokales Bild (relativ zur .md) auf max. 600 px Breite; null bei Fehlschlag.</summary>
    private static System.Windows.Controls.Image? TryLoadImage(string src, string baseDir)
    {
        try
        {
            if (src.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return null;   // offline
            string full = Path.IsPathRooted(src) ? src : Path.Combine(baseDir, src);
            if (!File.Exists(full)) return null;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(full);
            bmp.EndInit();
            bmp.Freeze();

            double w = Math.Min(600, bmp.PixelWidth);
            return new System.Windows.Controls.Image
            {
                Source = bmp,
                Width = w,
                Height = w / Math.Max(1, bmp.PixelWidth) * bmp.PixelHeight,
                Stretch = Stretch.Uniform,
            };
        }
        catch
        {
            return null;
        }
    }
}
