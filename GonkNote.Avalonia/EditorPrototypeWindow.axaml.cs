using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GonkNote.Avalonia;

/// <summary>
/// Prototyp für den Text-Editor unter Avalonia (HANDOFF.md §9.4 Punkt 5, Ansatz B = Markdown).
/// Beweist einen schlanken, plattformneutralen Editor: reine Textbearbeitung + Live-Vorschau
/// (Markdown.Avalonia) + Formatier-Werkzeugleiste, ganz ohne WPF-FlowDocument oder WebView.
/// Persistenz-Roundtrip über eine Markdown-Datei (später: als UTF-8 in TextDoc statt XamlPackage).
/// </summary>
public partial class EditorPrototypeWindow : Window
{
    private static readonly string SamplePath =
        Path.Combine(Path.GetTempPath(), "gonk-avalonia-editor.md");

    public EditorPrototypeWindow()
    {
        InitializeComponent();

        BtnBold.Click   += (_, _) => Wrap("**", "**");
        BtnItalic.Click += (_, _) => Wrap("*", "*");
        BtnCode.Click   += (_, _) => Wrap("`", "`");

        BtnH1.Click += (_, _) => Heading(1);
        BtnH2.Click += (_, _) => Heading(2);
        BtnH3.Click += (_, _) => Heading(3);

        BtnBullet.Click += (_, _) => LinePrefix("- ");
        BtnNumber.Click += (_, _) => LinePrefix("1. ");
        BtnQuote.Click  += (_, _) => LinePrefix("> ");
        BtnTable.Click  += (_, _) => InsertAtCaret(TableSnippet);

        BtnSave.Click += OnSaveRoundtrip;

        Source.TextChanged += (_, _) => UpdatePreview();

        Source.Text = File.Exists(SamplePath) ? File.ReadAllText(SamplePath) : SampleMarkdown;
        UpdatePreview();
    }

    // ---- Live-Vorschau + Statuszeile -------------------------------------------------

    private void UpdatePreview()
    {
        string text = Source.Text ?? "";
        Preview.Markdown = text;
        int words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        StatusText.Text = $"{words} Wörter · {text.Length} Zeichen · Live-Vorschau: Markdown.Avalonia";
    }

    // ---- Formatier-Helfer ------------------------------------------------------------

    /// <summary>Umschließt die Auswahl (oder fügt einen Platzhalter ein) mit Markern.</summary>
    private void Wrap(string left, string right)
    {
        string text = Source.Text ?? "";
        int a = Math.Min(Source.SelectionStart, Source.SelectionEnd);
        int b = Math.Max(Source.SelectionStart, Source.SelectionEnd);
        a = Math.Clamp(a, 0, text.Length);
        b = Math.Clamp(b, 0, text.Length);

        string sel = text.Substring(a, b - a);
        string body = sel.Length == 0 ? "Text" : sel;
        Source.Text = text[..a] + left + body + right + text[b..];
        Source.SelectionStart = a + left.Length;
        Source.SelectionEnd = a + left.Length + body.Length;
        Source.Focus();
    }

    /// <summary>Setzt die aktuelle Zeile auf Überschriftsebene <paramref name="level"/> (ersetzt vorhandene #).</summary>
    private void Heading(int level)
    {
        string text = Source.Text ?? "";
        int caret = Math.Clamp(Source.SelectionStart, 0, text.Length);
        int ls = caret == 0 ? 0 : text.LastIndexOf('\n', caret - 1) + 1; // 0 falls kein \n davor
        int le = text.IndexOf('\n', ls);
        if (le < 0) le = text.Length;

        string line = text[ls..le].TrimStart();
        while (line.StartsWith('#')) line = line[1..];
        line = line.TrimStart();

        string prefix = new string('#', level) + " ";
        Source.Text = text[..ls] + prefix + line + text[le..];
        Source.CaretIndex = ls + prefix.Length + line.Length;
        Source.Focus();
    }

    /// <summary>Stellt der aktuellen Zeile ein Präfix voran (Liste/Zitat).</summary>
    private void LinePrefix(string prefix)
    {
        string text = Source.Text ?? "";
        int caret = Math.Clamp(Source.SelectionStart, 0, text.Length);
        int ls = caret == 0 ? 0 : text.LastIndexOf('\n', caret - 1) + 1;
        Source.Text = text[..ls] + prefix + text[ls..];
        Source.CaretIndex = caret + prefix.Length;
        Source.Focus();
    }

    /// <summary>Fügt einen Textblock an der Einfügemarke ein.</summary>
    private void InsertAtCaret(string snippet)
    {
        string text = Source.Text ?? "";
        int caret = Math.Clamp(Source.CaretIndex, 0, text.Length);
        Source.Text = text[..caret] + snippet + text[caret..];
        Source.CaretIndex = caret + snippet.Length;
        Source.Focus();
    }

    // ---- Persistenz-Roundtrip (beweist Speichern/Laden) ------------------------------

    private void OnSaveRoundtrip(object? sender, RoutedEventArgs e)
    {
        try
        {
            File.WriteAllText(SamplePath, Source.Text ?? "");
            Source.Text = File.ReadAllText(SamplePath); // neu laden → Roundtrip
            StatusText.Text = $"Gespeichert & neu geladen: {SamplePath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Fehler beim Speichern: " + ex.Message;
        }
    }

    // ---- Beispieltext ----------------------------------------------------------------

    private const string TableSnippet =
        "\n\n| Spalte A | Spalte B |\n|----------|----------|\n| Zelle 1  | Zelle 2  |\n| Zelle 3  | Zelle 4  |\n\n";

    private const string SampleMarkdown =
        "# Gonk Note — Editor-Prototyp\n\n" +
        "Das ist **Ansatz B**: ein *schlanker* Markdown-Editor, der unter " +
        "Windows **und** Linux läuft — ganz ohne `FlowDocument`.\n\n" +
        "## Was schon geht\n\n" +
        "- Überschriften, Fett/Kursiv, `Code`\n" +
        "- Aufzählungen und nummerierte Listen\n" +
        "- Zitate und Tabellen\n\n" +
        "> Zitat: Live-Vorschau rechts, Bearbeitung links.\n\n" +
        "### Tabelle\n\n" +
        "| Format | Windows | Linux |\n" +
        "|--------|---------|-------|\n" +
        "| WPF    | ✔       | –       |\n" +
        "| Avalonia | ✔     | ✔     |\n\n" +
        "1. Kernlogik liegt schon in `GonkNote.Core`\n" +
        "2. Dieser Editor braucht kein WPF\n" +
        "3. Export kann über Markdown laufen\n";
}
