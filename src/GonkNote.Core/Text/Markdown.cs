using System.Text.RegularExpressions;

namespace GonkNote.Core.Text;

// ==================== Das Ergebnis ====================

/// <summary>Ein Block im Dokument — eine Überschrift, ein Absatz, eine Liste, …</summary>
public abstract record MdBlock;

public sealed record MdHeading(int Level, IReadOnlyList<MdInline> Inlines) : MdBlock;

public sealed record MdParagraph(IReadOnlyList<MdInline> Inlines) : MdBlock;

/// <summary>Ein eingerückter Code-Block (```). Der Inhalt bleibt wörtlich, Zeilenumbrüche inbegriffen.</summary>
public sealed record MdCodeBlock(string Text) : MdBlock;

/// <summary>Eine Trennlinie (<c>---</c>).</summary>
public sealed record MdRule : MdBlock;

/// <summary>Ein Zitatblock (<c>&gt;</c>). Sein Inhalt ist wieder ein ganzes Dokument.</summary>
public sealed record MdQuote(IReadOnlyList<MdBlock> Blocks) : MdBlock;

public sealed record MdList(bool Ordered, IReadOnlyList<MdListItem> Items) : MdBlock;

/// <param name="Sub">Eine eingerückte Unterliste, oder <c>null</c>.</param>
public sealed record MdListItem(IReadOnlyList<MdInline> Inlines, MdList? Sub);

public sealed record MdTable(
    IReadOnlyList<IReadOnlyList<MdInline>> Header,
    IReadOnlyList<IReadOnlyList<IReadOnlyList<MdInline>>> Rows,
    int Columns) : MdBlock;

/// <summary>Ein Textstück innerhalb eines Blocks.</summary>
public abstract record MdInline;

public sealed record MdText(string Text) : MdInline;

/// <summary>`Code` — im Fließtext, anders als <see cref="MdCodeBlock"/>.</summary>
public sealed record MdCodeSpan(string Text) : MdInline;

public sealed record MdBold(IReadOnlyList<MdInline> Inner) : MdInline;

public sealed record MdItalic(IReadOnlyList<MdInline> Inner) : MdInline;

/// <param name="Target">Das Ziel, wörtlich wie im Text — ein relativer Verweis bleibt relativ.</param>
public sealed record MdLink(string Text, string Target) : MdInline;

// ==================== Der Zerleger ====================

/// <summary>
/// Zerlegt Markdown in Blöcke und Textstücke — gerade so viel, wie die vier mitgelieferten
/// Dokumente brauchen: Überschriften, Absätze, Aufzählungen (auch verschachtelt), Tabellen,
/// Zitate, Code-Blöcke, Trennlinien sowie <c>**fett**</c>, <c>*kursiv*</c>, <c>`Code`</c>
/// und Verweise.
///
/// <para>
/// <b>Bewusst kein Markdown-Paket:</b> der Umfang ist klein und überschaubar, und das
/// Projekt bleibt ohne zusätzliche Abhängigkeit samt Lizenzvermerk. Was hier nicht erkannt
/// wird, landet als gewöhnlicher Text — ein Dialog kann also nie leer bleiben.
/// </para>
///
/// <para>
/// <b>Warum das in Core steht und nicht im Kopf.</b> Zerlegen zeichnet kein Pixel und nimmt
/// keine Eingabe entgegen; nach der Faustregel aus HANDOFF §3 gehört es damit hierher. Bis
/// Phase 3 waren Zerlegen und Darstellen im WPF-Kopf dasselbe
/// (<c>Services/MarkdownFlow.cs</c>), weil dessen Ergebnis unmittelbar ein
/// <c>FlowDocument</c> ist. Der Linux-Kopf kann das nicht übernehmen: Avalonia hat kein
/// <c>FlowDocument</c> (HANDOFF §4.1). Statt die Grammatik ein zweites Mal abzuschreiben,
/// steht sie einmal hier und jeder Kopf malt nur noch.
/// </para>
///
/// <para>
/// <b>Beide Köpfe zerlegen hiermit</b> — <c>Views/MarkdownView.cs</c> (Avalonia) seit
/// Phase 3, <c>Services/MarkdownFlow.cs</c> (WPF) seit dem 2026-08-04 (HANDOFF §4.13).
/// Die Endlosschleife unten stand deshalb eine Zeit lang auch dort; sie ist mit dem
/// Umstellen verschwunden, ohne dass jemand sie eigens beheben musste.
/// </para>
/// </summary>
public static partial class Markdown
{
    /// <summary>Zerlegt <paramref name="markdown"/>. Leerer Text ergibt eine leere Liste, keine Ausnahme.</summary>
    public static IReadOnlyList<MdBlock> Parse(string markdown)
    {
        var blocks = new List<MdBlock>();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');

        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i];

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                blocks.Add(CodeBlock(lines, ref i));
                continue;
            }
            if (IstTrennlinie(line)) { blocks.Add(new MdRule()); i++; continue; }

            var kopf = HeadingRegex().Match(line);
            if (kopf.Success)
            {
                blocks.Add(new MdHeading(kopf.Groups[1].Value.Length, Inline(kopf.Groups[2].Value)));
                i++;
                continue;
            }

            // Eine Tabelle ist erst eine, wenn unter der Kopfzeile die Trennzeile steht —
            // sonst wäre jede Zeile mit einem Strich am Anfang eine.
            if (IstTabellenZeile(line) && i + 1 < lines.Length && IstTabellenTrenner(lines[i + 1]))
            {
                blocks.Add(Tabelle(lines, ref i));
                continue;
            }

            if (line.StartsWith('>')) { blocks.Add(Zitat(lines, ref i)); continue; }
            if (ListenTreffer(line).Success) { blocks.Add(Liste(lines, ref i)); continue; }

            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

            blocks.Add(Absatz(lines, ref i));
        }
        return blocks;
    }

    // ---------------------------------------------------------------- Blöcke

    private static bool IstTrennlinie(string s) => RuleRegex().IsMatch(s.Trim());

    private static bool IstTabellenZeile(string s) => s.TrimStart().StartsWith('|');

    private static bool IstTabellenTrenner(string s) => TableSeparatorRegex().IsMatch(s.Trim());

    private static Match ListenTreffer(string s) => ListRegex().Match(s);

    private static MdBlock CodeBlock(string[] lines, ref int i)
    {
        i++;                                     // die öffnende ```
        var body = new List<string>();
        while (i < lines.Length && !lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
            body.Add(lines[i++]);
        if (i < lines.Length) i++;               // die schließende ```

        return new MdCodeBlock(string.Join("\n", body));
    }

    private static MdBlock Zitat(string[] lines, ref int i)
    {
        var body = new List<string>();
        while (i < lines.Length && lines[i].StartsWith('>'))
            body.Add(QuoteMarkRegex().Replace(lines[i++], ""));

        // Der Inhalt eines Zitats ist wieder ein ganzes Dokument — Überschriften und Listen
        // darin sollen auch dort welche sein.
        return new MdQuote(Parse(string.Join("\n", body)));
    }

    private static MdBlock Liste(string[] lines, ref int i)
    {
        int grundEinzug = ListenTreffer(lines[i]).Groups[1].Value.Length;
        bool nummeriert = OrderedRegex().IsMatch(lines[i].TrimStart());
        var punkte = new List<MdListItem>();

        while (i < lines.Length)
        {
            var m = ListenTreffer(lines[i]);
            if (!m.Success) break;

            int einzug = m.Groups[1].Value.Length;
            if (einzug < grundEinzug) break;

            if (einzug > grundEinzug)
            {
                // Untereintrag: an den letzten Punkt hängen, statt einen neuen zu beginnen.
                var unter = Liste(lines, ref i);
                if (punkte.Count > 0 && unter is MdList ul)
                    punkte[^1] = punkte[^1] with { Sub = ul };
                continue;
            }

            string text = m.Groups[3].Value;
            i++;
            // Fortsetzungszeilen eines Punktes: eingerückt, aber kein neuer Punkt.
            while (i < lines.Length && !ListenTreffer(lines[i]).Success &&
                   !string.IsNullOrWhiteSpace(lines[i]) && lines[i].StartsWith(' '))
                text += " " + lines[i++].Trim();

            punkte.Add(new MdListItem(Inline(text), null));
        }
        return new MdList(nummeriert, punkte);
    }

    private static MdBlock Tabelle(string[] lines, ref int i)
    {
        var kopf = Zellen(lines[i]);
        i += 2;                                  // Kopfzeile + Trennzeile

        var zeilen = new List<string[]>();
        while (i < lines.Length && IstTabellenZeile(lines[i])) zeilen.Add(Zellen(lines[i++]));

        int spalten = kopf.Length;
        foreach (var z in zeilen) spalten = Math.Max(spalten, z.Length);

        return new MdTable(
            kopf.Select(Inline).ToArray(),
            zeilen.Select(z => (IReadOnlyList<IReadOnlyList<MdInline>>)z.Select(Inline).ToArray()).ToArray(),
            spalten);
    }

    private static string[] Zellen(string line)
    {
        string s = line.Trim();
        if (s.StartsWith('|')) s = s[1..];
        if (s.EndsWith('|')) s = s[..^1];
        var teile = s.Split('|');
        for (int i = 0; i < teile.Length; i++) teile[i] = teile[i].Trim();
        return teile;
    }

    private static MdBlock Absatz(string[] lines, ref int i)
    {
        // **Die erste Zeile wird bedingungslos genommen.** <c>Parse</c> hat für sie schon
        // entschieden, dass sie ein Absatz ist; würde die Schleife unten sie erneut prüfen
        // und ablehnen, käme ein leerer Absatz heraus, <c>i</c> stünde still und
        // <c>Parse</c> liefe endlos.
        //
        // Das ist kein gedachter Fall: eine Tabellenzeile **ohne** Trennzeile darunter ist
        // laut Parse keine Tabelle und landet hier — und genau die weist die Schleife ab
        // (`!IstTabellenZeile`). Gefunden hat es der Wächter
        // `Eine_Tabelle_braucht_ihre_Trennzeile`, und zwar daran, dass der Testlauf nicht
        // mehr zurückkam.
        var body = new List<string> { lines[i++].Trim() };

        while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]) &&
               !lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal) &&
               !lines[i].StartsWith('>') &&
               !IstTrennlinie(lines[i]) && !IstTabellenZeile(lines[i]) &&
               !HeadingRegex().IsMatch(lines[i]) && !ListenTreffer(lines[i]).Success)
            body.Add(lines[i++].Trim());

        return new MdParagraph(Inline(string.Join(" ", body)));
    }

    // ---------------------------------------------------------------- Textstücke

    /// <summary>Zerlegt eine Zeile in Text, Code, Fett, Kursiv und Verweise.</summary>
    public static IReadOnlyList<MdInline> Inline(string text)
    {
        var stuecke = new List<MdInline>();
        int pos = 0;

        foreach (Match m in InlineRegex().Matches(text))
        {
            if (m.Index > pos) stuecke.Add(new MdText(Entschaerft(text[pos..m.Index])));

            if (m.Groups["code"].Success)
                stuecke.Add(new MdCodeSpan(m.Groups["codeIn"].Value));
            else if (m.Groups["link"].Success)
                stuecke.Add(new MdLink(m.Groups["linkText"].Value, m.Groups["linkUrl"].Value));
            else if (m.Groups["bold"].Success)
                stuecke.Add(new MdBold(Inline(m.Groups["boldIn"].Value)));
            else
                stuecke.Add(new MdItalic(Inline(m.Groups["emIn"].Value)));

            pos = m.Index + m.Length;
        }
        if (pos < text.Length) stuecke.Add(new MdText(Entschaerft(text[pos..])));
        return stuecke;
    }

    private static string Entschaerft(string s) => s.Replace("\\*", "*").Replace("\\_", "_");

    // ---------------------------------------------------------------- Muster
    //
    // **`[GeneratedRegex]` und nicht `RegexOptions.Compiled`.** Das eine erzeugt seinen Code
    // zur Übersetzungszeit, das andere zur Laufzeit über `Reflection.Emit` — und den gibt es
    // unter NativeAOT nicht (HANDOFF §1, derselbe Grund, aus dem LiteDB weichen musste).
    // Seit §4.13 gibt es keine Laufzeitfassung mehr: `MarkdownFlow` hat mit der Grammatik
    // auch seinen `RegexOptions.Compiled` verloren.

    [GeneratedRegex(@"^(#{1,6})\s+(.*)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^(-{3,}|\*{3,}|_{3,})$")]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"^\|(\s*:?-{2,}:?\s*\|)+$")]
    private static partial Regex TableSeparatorRegex();

    [GeneratedRegex(@"^(\s*)([-*+]|\d+\.)\s+(.*)$")]
    private static partial Regex ListRegex();

    [GeneratedRegex(@"^\d+\.")]
    private static partial Regex OrderedRegex();

    [GeneratedRegex(@"^>\s?")]
    private static partial Regex QuoteMarkRegex();

    // Die Reihenfolge zählt: `Code` zuerst, damit Sternchen darin wörtlich bleiben; fett vor
    // kursiv, sonst reißt ** in zwei * auseinander. Fett darf Sternchen enthalten
    // („**fett mit *kursiv* darin**") — es läuft bis zum nächsten **, nicht bis zum
    // nächsten Sternchen.
    [GeneratedRegex(
        @"(?<code>`(?<codeIn>[^`]+)`)" +
        @"|(?<link>\[(?<linkText>[^\]]+)\]\((?<linkUrl>[^)]+)\))" +
        @"|(?<bold>\*\*(?<boldIn>(?:(?!\*\*).)+?)\*\*)" +
        @"|(?<em>(?<!\*)\*(?!\*)(?<emIn>[^*]+)\*(?!\*))")]
    private static partial Regex InlineRegex();
}
