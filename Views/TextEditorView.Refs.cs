using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Verweise: Inhaltsverzeichnis aus den Überschriften 1–4 erzeugen und aktualisieren.
/// Das Verzeichnis wird über den Titel-Absatz („Inhaltsverzeichnis“) und das feste
/// Eintrags-Format wiedererkannt (XamlPackage speichert keine Marker/Tags).
/// Beim DOCX-Export wird es durch ein echtes Word-TOC-Feld ersetzt.
/// </summary>
public partial class TextEditorView
{
    private void InsertToc_Click(object s, RoutedEventArgs e)
    {
        if (FindTocTitle() is { } existing)
        {
            // Es gibt schon eins → nur aktualisieren
            RebuildTocAfter(existing);
        }
        else
        {
            var title = MakeTocTitle();
            InsertBlockAtCaret(title);
            RebuildTocAfter(title);
        }
        MarkDirty();
        Editor.Focus();
    }

    private void UpdateToc_Click(object s, RoutedEventArgs e)
    {
        if (FindTocTitle() is not { } title)
        {
            MessageBox.Show(Window.GetWindow(this),
                "Kein Inhaltsverzeichnis gefunden. Bitte zuerst über „Inhaltsverzeichnis einfügen“ anlegen.",
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        RebuildTocAfter(title);
        MarkDirty();
    }

    /// <summary>Titel-Absatz des Verzeichnisses (Text = Marker, groß + fett formatiert).</summary>
    private Paragraph? FindTocTitle() =>
        TextStyles.AllParagraphs(Editor.Document.Blocks).FirstOrDefault(p =>
            new TextRange(p.ContentStart, p.ContentEnd).Text.Trim() == TextStyles.TocTitle &&
            p.FontSize >= 20);

    private static Paragraph MakeTocTitle()
    {
        var h1 = TextStyles.ForHeading(1);
        return new Paragraph(new Run(TextStyles.TocTitle))
        {
            FontSize = h1.Size,
            FontWeight = h1.Weight,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(h1.ColorHex!)),
            Margin = h1.Margin,
        };
    }

    /// <summary>Entfernt alte Einträge hinter dem Titel und baut sie aus den Überschriften neu.</summary>
    private void RebuildTocAfter(Paragraph title)
    {
        var blocks = title.Parent switch
        {
            FlowDocument doc => doc.Blocks,
            Section sec => sec.Blocks,
            TableCell cell => cell.Blocks,
            ListItem li => li.Blocks,
            _ => Editor.Document.Blocks,
        };

        // Alte Einträge (direkt folgende Absätze im Eintrags-Format) entfernen
        while (title.NextBlock is Paragraph next && TextStyles.IsTocEntry(next))
            blocks.Remove(next);

        var headings = TextStyles.CollectHeadings(Editor.Document);
        Block anchor = title;

        if (headings.Count == 0)
        {
            var empty = MakeTocEntry(1, "(Keine Überschriften gefunden – Formatvorlagen Überschrift 1–4 verwenden)");
            blocks.InsertAfter(anchor, empty);
            return;
        }

        foreach (var (level, text, _) in headings)
        {
            var entry = MakeTocEntry(level, text);
            blocks.InsertAfter(anchor, entry);
            anchor = entry;
        }
    }

    private static Paragraph MakeTocEntry(int level, string text) => new(new Run(text))
    {
        FontSize = TextStyles.TocEntrySize,
        FontWeight = level == 1 ? FontWeights.SemiBold : FontWeights.Normal,
        Margin = new Thickness((level - 1) * 18, 0, 0, 2),
    };
}
