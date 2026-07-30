using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace GonkNote.Services;

/// <summary>
/// Best-effort-Export eines FlowDocuments nach Markdown: Überschriften (nach
/// Schriftgröße), fett/kursiv/durchgestrichen, Listen, Tabellen und Trennlinien.
/// Bilder werden als Platzhalter vermerkt.
/// </summary>
public static class MarkdownExporter
{
    public static void Export(FlowDocument flow, string path)
    {
        var sb = new StringBuilder();
        foreach (var block in flow.Blocks)
            WriteBlock(block, sb, indent: 0);
        File.WriteAllText(path, sb.ToString().TrimEnd() + "\n", new UTF8Encoding(false));
    }

    private static void WriteBlock(Block block, StringBuilder sb, int indent)
    {
        switch (block)
        {
            case Paragraph p:
            {
                string prefix = HeadingPrefix(EffectiveFontSize(p));
                // Überschriften brauchen keine zusätzliche Fett-/Kursiv-Auszeichnung
                string text = (prefix.Length > 0 ? PlainText(p.Inlines) : InlineText(p.Inlines)).Trim();
                if (text.Length == 0) { sb.AppendLine(); return; }

                sb.Append(new string(' ', indent * 2)).Append(prefix).AppendLine(text);
                sb.AppendLine();
                break;
            }

            case List list:
            {
                bool ordered = IsOrdered(list.MarkerStyle);
                int n = 1;
                foreach (var li in list.ListItems)
                {
                    string marker = ordered ? $"{n++}. " : "- ";
                    bool firstBlock = true;
                    foreach (var b in li.Blocks)
                    {
                        if (b is Paragraph lp)
                        {
                            string text = InlineText(lp.Inlines).Trim();
                            sb.Append(new string(' ', indent * 2))
                              .Append(firstBlock ? marker : "  ")
                              .AppendLine(text);
                            firstBlock = false;
                        }
                        else if (b is List nested)
                        {
                            WriteBlock(nested, sb, indent + 1);
                        }
                    }
                }
                sb.AppendLine();
                break;
            }

            case Table table:
                WriteTable(table, sb);
                break;

            case BlockUIContainer:
                sb.AppendLine("---").AppendLine();
                break;
        }
    }

    private static void WriteTable(Table table, StringBuilder sb)
    {
        var rows = table.RowGroups.SelectMany(g => g.Rows).ToList();
        if (rows.Count == 0) return;

        int cols = rows.Max(r => r.Cells.Count);
        bool headerDone = false;

        foreach (var row in rows)
        {
            sb.Append("| ");
            for (int c = 0; c < cols; c++)
            {
                string cellText = c < row.Cells.Count
                    ? string.Join(" ", row.Cells[c].Blocks.OfType<Paragraph>()
                        .Select(p => InlineText(p.Inlines).Trim())).Replace("|", "\\|")
                    : "";
                sb.Append(cellText).Append(" | ");
            }
            sb.AppendLine();

            if (!headerDone)
            {
                sb.Append("| ");
                for (int c = 0; c < cols; c++) sb.Append("--- | ");
                sb.AppendLine();
                headerDone = true;
            }
        }
        sb.AppendLine();
    }

    // ==================== Inlines ====================

    private static string InlineText(InlineCollection inlines)
    {
        var sb = new StringBuilder();
        foreach (var inline in inlines) AppendInline(inline, sb);
        return sb.ToString();
    }

    /// <summary>Reiner Text ohne Markdown-Auszeichnung (für Überschriften).</summary>
    private static string PlainText(InlineCollection inlines)
    {
        var sb = new StringBuilder();
        void Walk(Inline inline)
        {
            switch (inline)
            {
                case Run r: sb.Append(r.Text); break;
                case Span s: foreach (var c in s.Inlines) Walk(c); break;
                case LineBreak: sb.Append(' '); break;
            }
        }
        foreach (var i in inlines) Walk(i);
        return sb.ToString();
    }

    private static void AppendInline(Inline inline, StringBuilder sb)
    {
        switch (inline)
        {
            case Run run:
            {
                string text = run.Text;
                if (text.Length == 0) return;
                bool bold = run.GetValue(TextElement.FontWeightProperty) is FontWeight w && w.ToOpenTypeWeight() >= 600;
                bool italic = run.GetValue(TextElement.FontStyleProperty) is FontStyle st && st != FontStyles.Normal;
                bool strike = run.GetValue(Inline.TextDecorationsProperty) is TextDecorationCollection d
                              && d.Any(x => x.Location == TextDecorationLocation.Strikethrough);
                sb.Append(Wrap(text, bold, italic, strike));
                break;
            }

            // **Muss vor `case Span` stehen:** Hyperlink erbt von Span. Stand dieser Fall
            // nicht hier, griff der allgemeinere — der Text kam durch, das **Ziel fiel weg**.
            // Damit war ein Markdown-Import mit Links nach dem Rückexport nur noch Text.
            case Hyperlink link:
            {
                // Fett/kursiv wird bewusst nicht auf Link-Ebene geprüft: die inneren Runs
                // bringen ihre Auszeichnung über InlineText schon mit, und GetValue würde hier
                // auch die vom Absatz **geerbte** Formatierung melden.
                string text = InlineText(link.Inlines);

                // Ein Link ohne Ziel ist keiner: den gibt es in den mitgelieferten
                // Hilfe-Dokumenten (Verweis auf ein anderes .md, per Click-Handler statt
                // NavigateUri). Der bleibt Text.
                if (link.NavigateUri is not { } uri)
                {
                    sb.Append(text);
                    break;
                }

                sb.Append('[').Append(LinkText(text)).Append("](").Append(LinkTarget(uri)).Append(')');
                break;
            }

            case Span span:
            {
                // Formatierung auf Span-Ebene (Bold/Italic-Container) auf die Kinder anwenden
                bool bold = span.GetValue(TextElement.FontWeightProperty) is FontWeight w && w.ToOpenTypeWeight() >= 600;
                bool italic = span.GetValue(TextElement.FontStyleProperty) is FontStyle st && st != FontStyles.Normal;
                bool strike = span.GetValue(Inline.TextDecorationsProperty) is TextDecorationCollection d
                              && d.Any(x => x.Location == TextDecorationLocation.Strikethrough);

                string inner = InlineText(span.Inlines);
                sb.Append((bold || italic || strike) ? Wrap(inner, bold, italic, strike) : inner);
                break;
            }

            case LineBreak:
                sb.Append("  \n");  // Markdown-Zeilenumbruch
                break;

            case InlineUIContainer:
                sb.Append("![Bild]()");
                break;
        }
    }

    /// <summary>
    /// Linktext für die eckigen Klammern. Eine unmaskierte Klammer darin beendet den Link
    /// vorzeitig — aus „[a]b](url)" wird sonst der Link „a" gefolgt von Text.
    /// </summary>
    private static string LinkText(string text) =>
        text.Replace(@"\", @"\\").Replace("[", @"\[").Replace("]", @"\]");

    /// <summary>
    /// Linkziel für die runden Klammern.
    /// <para>
    /// <c>OriginalString</c> und nicht <c>AbsoluteUri</c>: so kommt genau das zurück, was
    /// hineingeschrieben wurde — ein Markdown-Import mit relativem Ziel bleibt dadurch beim
    /// Rückexport unverändert, statt zu einem absoluten <c>file:///</c>-Pfad zu werden.
    /// </para>
    /// Enthält das Ziel Leerzeichen oder Klammern, muss es in spitze Klammern: sonst endet der
    /// Link an der ersten schließenden runden Klammer.
    /// </summary>
    private static string LinkTarget(Uri uri)
    {
        string url = uri.OriginalString;
        if (url.Length == 0) url = uri.ToString();
        return url.AsSpan().IndexOfAny(" ()<>") >= 0 ? $"<{url}>" : url;
    }

    private static string Wrap(string text, bool bold, bool italic, bool strike)
    {
        if (text.Trim().Length == 0) return text;  // Whitespace nicht dekorieren
        string lead = text[..(text.Length - text.TrimStart().Length)];
        string trail = text[text.TrimEnd().Length..];
        string core = text.Trim();

        if (strike) core = $"~~{core}~~";
        if (bold && italic) core = $"***{core}***";
        else if (bold) core = $"**{core}**";
        else if (italic) core = $"*{core}*";
        return lead + core + trail;
    }

    // ==================== Helfer ====================

    private static double EffectiveFontSize(Paragraph p)
    {
        double size = p.FontSize;
        var firstRun = p.Inlines.FirstOrDefault();
        if (firstRun?.GetValue(TextElement.FontSizeProperty) is double fs) size = fs;
        return size;
    }

    // Schwellen passend zu den Formatvorlagen (TextStyles: 28/22/18/16, Fließtext 15)
    private static string HeadingPrefix(double fontSize) => fontSize switch
    {
        >= 25 => "# ",
        >= 20 => "## ",
        >= 17 => "### ",
        >= 15.6 => "#### ",
        _ => "",
    };

    private static bool IsOrdered(TextMarkerStyle m) => m is
        TextMarkerStyle.Decimal or TextMarkerStyle.LowerLatin or TextMarkerStyle.UpperLatin
        or TextMarkerStyle.LowerRoman or TextMarkerStyle.UpperRoman;
}
