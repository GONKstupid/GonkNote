using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using TextDoc = GonkNote.Core.Models.TextDoc;  // Models.TextElement würde mit WPF kollidieren

namespace GonkNote.Services;

/// <summary>
/// Zentrale Definition der Formatvorlagen des Text-Editors (Standard, Kein Abstand,
/// Überschrift 1–4), der Seitenformate und der Standard-Schreibfarbe ("Ink").
/// Wichtig: XamlPackage speichert keine Style-IDs, deshalb werden Überschriften
/// über ihre feste Größe+Gewicht-Kombination wiedererkannt (TOC, DOCX-Export,
/// Navigator). Alle Stellen nutzen dieselben Werte aus dieser Klasse.
/// </summary>
public static class TextStyles
{
    public const double PxPerCm = 96.0 / 2.54;

    /// <summary>Grundschriftgröße des Fließtexts (DIP).</summary>
    public const double BodySize = 15;

    /// <summary>Standard-Schreibfarben je Theme (entsprechen Color.DefaultInk).</summary>
    public static readonly Color InkLight = Color.FromRgb(0x1B, 0x2B, 0x4B);
    public static readonly Color InkDark = Color.FromRgb(0xE6, 0xEC, 0xF7);

    /// <summary>Titel des generierten Inhaltsverzeichnisses (dient auch als Marker).</summary>
    public const string TocTitle = "Inhaltsverzeichnis";

    /// <summary>Schriftgröße der TOC-Einträge – bewusst „krumm“, um sie wiederzuerkennen.</summary>
    public const double TocEntrySize = 13;

    // ==================== Formatvorlagen ====================

    /// <summary>
    /// Eine Absatz-Formatvorlage. <paramref name="Name"/> ist die **Kennung** (stabil, wird
    /// intern verglichen und bleibt deutsch); <paramref name="Key"/> zeigt auf den
    /// übersetzten Anzeigenamen.
    /// </summary>
    public sealed record ParaStyle(
        string Name, string Key, double Size, FontWeight Weight, FontStyle Style,
        string? ColorHex, Thickness Margin, int HeadingLevel)
    {
        /// <summary>Beschriftung in der aktuellen Sprache.</summary>
        public string Display => Loc.T(Key);
    }

    /// <summary>
    /// Überschrift-Farben folgen dem Design-Konzept: Blau/Türkis/Lila/Pink zeigen
    /// die Hierarchie (feste Light-Theme-Werte, damit Export auf weißem Papier stimmt).
    /// </summary>
    public static readonly ParaStyle[] All =
    {
        new("Standard", "Style.Normal", BodySize, FontWeights.Normal, FontStyles.Normal, null, new Thickness(0, 2, 0, 6), 0),
        new("Kein Abstand", "Style.NoSpacing", BodySize, FontWeights.Normal, FontStyles.Normal, null, new Thickness(0), 0),
        new("Überschrift 1", "Style.Heading1", 28, FontWeights.Bold, FontStyles.Normal, "#2563EB", new Thickness(0, 16, 0, 8), 1),
        new("Überschrift 2", "Style.Heading2", 22, FontWeights.Bold, FontStyles.Normal, "#14B8A6", new Thickness(0, 12, 0, 6), 2),
        new("Überschrift 3", "Style.Heading3", 18, FontWeights.Bold, FontStyles.Normal, "#8B5CF6", new Thickness(0, 10, 0, 4), 3),
        new("Überschrift 4", "Style.Heading4", 16, FontWeights.Bold, FontStyles.Italic, "#EC4899", new Thickness(0, 8, 0, 4), 4),
        // Weitere Presets (keine Überschriften → nicht im Inhaltsverzeichnis)
        new("Titel", "Style.Title", 34, FontWeights.Bold, FontStyles.Normal, null, new Thickness(0, 6, 0, 12), 0),
        new("Zitat", "Style.Quote", BodySize, FontWeights.Normal, FontStyles.Italic, "#6B7A99", new Thickness(28, 8, 28, 8), 0),
        new("Kopfzeile", "Style.Header", 12, FontWeights.Normal, FontStyles.Normal, "#6B7A99", new Thickness(0, 2, 0, 2), 0),
        new("Fußzeile", "Style.Footer", 12, FontWeights.Normal, FontStyles.Normal, "#6B7A99", new Thickness(0, 2, 0, 2), 0),
    };

    public static ParaStyle ForHeading(int level) => All.First(s => s.HeadingLevel == level);

    /// <summary>Wendet eine Formatvorlage auf alle Absätze der Auswahl an.</summary>
    public static void Apply(ParaStyle style, TextPointer selStart, TextPointer selEnd, Color defaultInk)
    {
        var start = selStart.Paragraph?.ContentStart ?? selStart;
        var end = selEnd.Paragraph?.ContentEnd ?? selEnd;
        var range = new TextRange(start, end);

        range.ApplyPropertyValue(TextElement.FontSizeProperty, style.Size);
        range.ApplyPropertyValue(TextElement.FontWeightProperty, style.Weight);
        range.ApplyPropertyValue(TextElement.FontStyleProperty, style.Style);
        range.ApplyPropertyValue(TextElement.ForegroundProperty,
            new SolidColorBrush(style.ColorHex != null
                ? (Color)ColorConverter.ConvertFromString(style.ColorHex)
                : defaultInk));

        // Absatzabstände blockweise setzen (ApplyPropertyValue kennt Margin nicht)
        for (var block = selStart.Paragraph as Block; block != null; block = block.NextBlock)
        {
            if (block is Paragraph p) p.Margin = style.Margin;
            if (selEnd.Paragraph != null && block == selEnd.Paragraph) break;
            if (block.ContentStart.CompareTo(end) >= 0) break;
        }
    }

    /// <summary>
    /// Erkennt die Überschriften-Ebene eines Absatzes (0 = keine) anhand von
    /// Größe + Gewicht. Der TOC-Titel selbst zählt nicht als Überschrift.
    /// </summary>
    public static int HeadingLevel(Paragraph p)
    {
        var range = new TextRange(p.ContentStart, p.ContentEnd);
        string text = range.Text.Trim();
        if (text.Length == 0 || text == TocTitle) return 0;

        if (range.GetPropertyValue(TextElement.FontSizeProperty) is not double size) return 0;
        bool bold = range.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight w &&
                    w.ToOpenTypeWeight() >= 600;
        if (!bold) return 0;

        foreach (var s in All)
            if (s.HeadingLevel > 0 && Math.Abs(size - s.Size) < 0.6)
                return s.HeadingLevel;
        return 0;
    }

    /// <summary>Alle Überschriften des Dokuments in Lesereihenfolge (für TOC/Navigator).</summary>
    public static List<(int Level, string Text, Paragraph Para)> CollectHeadings(FlowDocument doc)
    {
        var result = new List<(int, string, Paragraph)>();
        foreach (var p in AllParagraphs(doc.Blocks))
        {
            int lvl = HeadingLevel(p);
            if (lvl > 0)
                result.Add((lvl, new TextRange(p.ContentStart, p.ContentEnd).Text.Trim(), p));
        }
        return result;
    }

    public static IEnumerable<Paragraph> AllParagraphs(BlockCollection blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case Paragraph p:
                    yield return p;
                    break;
                case Section s:
                    foreach (var inner in AllParagraphs(s.Blocks)) yield return inner;
                    break;
                case List list:
                    foreach (var li in list.ListItems)
                        foreach (var inner in AllParagraphs(li.Blocks)) yield return inner;
                    break;
                case Table t:
                    foreach (var g in t.RowGroups)
                        foreach (var r in g.Rows)
                            foreach (var c in r.Cells)
                                foreach (var inner in AllParagraphs(c.Blocks)) yield return inner;
                    break;
            }
        }
    }

    /// <summary>Ist der Absatz ein generierter TOC-Eintrag? (Marker: Größe + linker Einzug in 18er-Schritten)</summary>
    public static bool IsTocEntry(Paragraph p) =>
        Math.Abs(p.FontSize - TocEntrySize) < 0.3 &&
        p.Margin.Left >= 0 && p.Margin.Left <= 3 * 18 + 0.5 &&
        Math.Abs(p.Margin.Left % 18) < 0.5;

    // ==================== Ink-Normalisierung ====================

    /// <summary>
    /// Ersetzt die Standard-Schreibfarbe des jeweils anderen Themes durch die
    /// gewünschte Ink-Farbe. Nötig, weil TextRange.Save die geerbte Editor-Farbe
    /// „einbrennt“ – ohne Normalisierung bliebe heller Text nach einem Theme-Wechsel
    /// hell (bzw. würde hell auf weißes Exportpapier gedruckt).
    /// </summary>
    public static void NormalizeInk(FlowDocument doc, Color target)
    {
        void FixElement(TextElement el)
        {
            if (el.ReadLocalValue(TextElement.ForegroundProperty) is SolidColorBrush b &&
                (IsInk(b.Color)) && b.Color != target)
                el.Foreground = new SolidColorBrush(target);
        }

        void WalkInlines(InlineCollection inlines)
        {
            foreach (var il in inlines)
            {
                FixElement(il);
                if (il is Span span) WalkInlines(span.Inlines);
            }
        }

        void WalkBlocks(BlockCollection blocks)
        {
            foreach (var block in blocks)
            {
                FixElement(block);
                switch (block)
                {
                    case Paragraph p: WalkInlines(p.Inlines); break;
                    case Section s: WalkBlocks(s.Blocks); break;
                    case List list:
                        foreach (var li in list.ListItems) WalkBlocks(li.Blocks);
                        break;
                    case Table t:
                        foreach (var g in t.RowGroups)
                            foreach (var r in g.Rows)
                                foreach (var c in r.Cells)
                                    WalkBlocks(c.Blocks);
                        break;
                }
            }
        }

        if (doc.ReadLocalValue(FlowDocument.ForegroundProperty) is SolidColorBrush db && IsInk(db.Color))
            doc.Foreground = new SolidColorBrush(target);
        WalkBlocks(doc.Blocks);
    }

    private static bool IsInk(Color c) => c == InkLight || c == InkDark;

    // ==================== Seitenformate ====================

    /// <summary>Seitengröße in DIP (96 dpi) im Hochformat.</summary>
    public static (double W, double H) PageSize(string format) => format switch
    {
        "A3" => (1123, 1587),
        "A5" => (559, 794),
        "Letter" => (816, 1056),
        _ => (794, 1123),   // A4
    };

    /// <summary>Effektive Seitengröße inkl. Orientierung.</summary>
    public static (double W, double H) PageSize(TextDoc doc)
    {
        var (w, h) = PageSize(doc.PageFormat);
        return doc.Landscape ? (h, w) : (w, h);
    }

    public static Thickness PageMarginPx(TextDoc doc) => new(
        doc.MarginLeftCm * PxPerCm, doc.MarginTopCm * PxPerCm,
        doc.MarginRightCm * PxPerCm, doc.MarginBottomCm * PxPerCm);

    // ==================== Kopf-/Fußzeile ====================

    /// <summary>Ersetzt die Platzhalter {SEITE}, {SEITEN}, {DATUM}, {TITEL}.</summary>
    public static string ResolveHeaderFooter(string? template, int page, int pageCount, string? title) =>
        (template ?? "")
            .Replace("{SEITE}", page.ToString())
            .Replace("{SEITEN}", pageCount.ToString())
            .Replace("{DATUM}", DateTime.Now.ToString("dd.MM.yyyy"))
            .Replace("{TITEL}", title);
}
