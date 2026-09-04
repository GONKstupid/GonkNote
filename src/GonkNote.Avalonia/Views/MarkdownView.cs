using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using GonkNote.Core.Text;

namespace GonkNote.Views;

/// <summary>
/// Malt das, was <see cref="Markdown"/> zerlegt hat — die Antwort des Linux-Kopfs auf
/// <c>MarkdownFlow</c> im WPF-Kopf.
///
/// <para>
/// <b>Avalonia hat kein <c>FlowDocument</c></b>, und das ist genau die Naht aus HANDOFF
/// §4.1. Statt eines Dokumentmodells mit eigenem Umbruch entsteht hier ein ganz gewöhnlicher
/// Steuerelementbaum: <see cref="TextBlock"/> je Absatz, <see cref="Grid"/> je Tabelle,
/// <see cref="Border"/> je Zitat. Innerhalb eines Absatzes übernimmt Avalonias
/// <see cref="InlineCollection"/> die Auszeichnung — sie kann Fett, Kursiv und, über
/// <see cref="InlineUIContainer"/>, auch anklickbare Verweise.
/// </para>
///
/// <para>
/// <b>Die Farben stehen hier nicht.</b> Jede kommt als <c>DynamicResource</c> aus den
/// Ressourcen, die <c>AvaloniaThemeHost</c> aus der Farbtabelle in Core legt — deshalb
/// wechselt ein offener Dialog beim Umschalten hell/dunkel mit. Der WPF-Kopf macht dasselbe
/// mit <c>SetResourceReference</c>.
/// </para>
/// </summary>
public static class MarkdownView
{
    /// <summary>
    /// Schrift für Code. Avalonia löst die Liste von links nach rechts auf; <c>monospace</c>
    /// am Ende ist der fontconfig-Sammelname und trägt auf jedem Linux-System.
    /// </summary>
    private static readonly FontFamily Feste = new(
        GonkNote.Core.Theming.Fonts.Standard.Family(GonkNote.Core.Theming.FontRole.Mono)
        + ", Consolas, DejaVu Sans Mono, Liberation Mono, monospace");

    /// <param name="onDocumentLink">
    /// Wird mit dem Ziel aufgerufen, wenn ein Verweis auf eine andere <c>.md</c>-Datei
    /// angeklickt wird — so landet „Erste Schritte" im README beim passenden Fenster statt
    /// im Nichts. Ohne Handler bleiben solche Verweise Text.
    /// </param>
    public static Control Bauen(string markdown, Dokumentverweise? onDocumentLink = null)
    {
        var stapel = new StackPanel();
        foreach (var block in Markdown.Parse(markdown))
            stapel.Children.Add(Block(block, onDocumentLink));
        return stapel;
    }

    // ---------------------------------------------------------------- Blöcke

    private static Control Block(MdBlock block, Dokumentverweise? link) => block switch
    {
        MdHeading h => Ueberschrift(h, link),
        MdParagraph p => Absatz(p.Inlines, link, new Thickness(0, 0, 0, 8)),
        MdCodeBlock c => Codeblock(c),
        MdRule => Trennlinie(),
        MdQuote q => Zitat(q, link),
        MdList l => Liste(l, link),
        MdTable t => Tabelle(t, link),
        _ => new TextBlock(),
    };

    private static Control Ueberschrift(MdHeading h, Dokumentverweise? link)
    {
        var tb = Absatz(h.Inlines, link, new Thickness(0, h.Level == 1 ? 0 : 16, 0, 6));
        tb.FontSize = h.Level switch { 1 => 21, 2 => 17, 3 => 15, _ => 13.5 };
        tb.FontWeight = FontWeight.SemiBold;
        // Ab Ebene 3 in der Akzentfarbe — dieselbe Staffelung wie im WPF-Kopf.
        Farbe(tb, TextBlock.ForegroundProperty, h.Level >= 3 ? "Brush.Accent" : "Brush.Text");
        return tb;
    }

    private static TextBlock Absatz(IReadOnlyList<MdInline> inlines, Dokumentverweise? link, Thickness rand)
    {
        var tb = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = rand, LineHeight = 19 };
        Farbe(tb, TextBlock.ForegroundProperty, "Brush.Text");
        Fuellen(tb.Inlines!, inlines, link);
        return tb;
    }

    private static Control Codeblock(MdCodeBlock c)
    {
        var rahmen = new Border
        {
            Padding = new Thickness(10, 8),
            Margin = new Thickness(0, 6, 0, 10),
            CornerRadius = new CornerRadius(4),
            Child = new TextBlock
            {
                Text = c.Text,
                FontFamily = Feste,
                FontSize = 12,
                TextWrapping = TextWrapping.NoWrap,
            },
        };
        Farbe(rahmen, Border.BackgroundProperty, "Brush.WindowBg");
        Farbe((Control)rahmen.Child, TextBlock.ForegroundProperty, "Brush.Text");

        // Lange Befehlszeilen laufen sonst aus dem Dialog heraus, statt rollbar zu sein.
        var roller = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = rahmen,
        };
        return roller;
    }

    private static Control Trennlinie()
    {
        var b = new Border { Height = 1, Margin = new Thickness(0, 10) };
        Farbe(b, Border.BackgroundProperty, "Brush.Border");
        return b;
    }

    private static Control Zitat(MdQuote q, Dokumentverweise? link)
    {
        var inhalt = new StackPanel();
        foreach (var b in q.Blocks) inhalt.Children.Add(Block(b, link));

        var rahmen = new Border
        {
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(12, 2, 0, 2),
            Margin = new Thickness(0, 6, 0, 10),
            Child = inhalt,
        };
        Farbe(rahmen, Border.BorderBrushProperty, "Brush.Accent");
        return rahmen;
    }

    private static Control Liste(MdList liste, Dokumentverweise? link)
    {
        var stapel = new StackPanel { Margin = new Thickness(0, 4, 0, 8) };

        for (int i = 0; i < liste.Items.Count; i++)
        {
            var punkt = liste.Items[i];

            // Zwei Spalten: Markierung und Text. Ein TextBlock mit vorangestelltem „• "
            // täte es nicht — die zweite Zeile eines Punktes rückte dann unter die
            // Markierung statt unter den Text.
            var zeile = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                Margin = new Thickness(0, 1),
            };

            var marke = new TextBlock
            {
                Text = liste.Ordered ? $"{i + 1}." : "•",
                Margin = new Thickness(4, 0, 8, 0),
                MinWidth = liste.Ordered ? 22 : 12,
                TextAlignment = liste.Ordered ? TextAlignment.Right : TextAlignment.Left,
                LineHeight = 19,
            };
            Farbe(marke, TextBlock.ForegroundProperty, "Brush.TextMuted");
            zeile.Children.Add(marke);

            var inhalt = new StackPanel();
            inhalt.Children.Add(Absatz(punkt.Inlines, link, default));
            if (punkt.Sub is { } unter) inhalt.Children.Add(Liste(unter, link));

            Grid.SetColumn(inhalt, 1);
            zeile.Children.Add(inhalt);

            stapel.Children.Add(zeile);
        }
        return stapel;
    }

    private static Control Tabelle(MdTable t, Dokumentverweise? link)
    {
        var gitter = new Grid { Margin = new Thickness(0, 6, 0, 10) };
        for (int c = 0; c < t.Columns; c++)
            gitter.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        // Die letzte Spalte bekommt den Rest — sonst steht eine schmale Tabelle links und
        // der lange Text daneben bricht früh um.
        if (t.Columns > 0) gitter.ColumnDefinitions[^1].Width = new GridLength(1, GridUnitType.Star);

        for (int r = 0; r <= t.Rows.Count; r++)
            gitter.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        Zeile(gitter, 0, t.Header, t.Columns, fett: true, link);
        for (int r = 0; r < t.Rows.Count; r++)
            Zeile(gitter, r + 1, t.Rows[r], t.Columns, fett: false, link);

        return gitter;
    }

    private static void Zeile(Grid gitter, int r, IReadOnlyList<IReadOnlyList<MdInline>> zellen,
        int spalten, bool fett, Dokumentverweise? link)
    {
        for (int c = 0; c < spalten; c++)
        {
            var text = Absatz(c < zellen.Count ? zellen[c] : [], link, default);
            if (fett) text.FontWeight = FontWeight.SemiBold;

            var zelle = new Border
            {
                Padding = new Thickness(8, 4),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = text,
            };
            Farbe(zelle, Border.BorderBrushProperty, "Brush.Border");

            Grid.SetRow(zelle, r);
            Grid.SetColumn(zelle, c);
            gitter.Children.Add(zelle);
        }
    }

    // ---------------------------------------------------------------- Textstücke

    private static void Fuellen(InlineCollection ziel, IReadOnlyList<MdInline> stuecke,
        Dokumentverweise? link)
    {
        foreach (var s in stuecke)
        {
            switch (s)
            {
                case MdText t:
                    ziel.Add(new Run(t.Text));
                    break;

                case MdCodeSpan c:
                    var code = new Run(c.Text) { FontFamily = Feste, FontSize = 12 };
                    Farbe(code, TextElement.BackgroundProperty, "Brush.WindowBg");
                    ziel.Add(code);
                    break;

                case MdBold b:
                    var fett = new Bold();
                    Fuellen(fett.Inlines, b.Inner, link);
                    ziel.Add(fett);
                    break;

                case MdItalic k:
                    var kursiv = new Italic();
                    Fuellen(kursiv.Inlines, k.Inner, link);
                    ziel.Add(kursiv);
                    break;

                case MdStrike d:
                    var durch = new Span { TextDecorations = TextDecorations.Strikethrough };
                    Fuellen(durch.Inlines, d.Inner, link);
                    ziel.Add(durch);
                    break;

                case MdImage bild:
                    ziel.Add(Bildersatz(bild));
                    break;

                case MdLink l:
                    ziel.Add(Verweis(l, link));
                    break;
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="GonkNote.Services.MarkdownFlow" path="/summary"/>
    /// <b>Ein Bild wird hier nicht geladen, sondern durch seinen Ersatztext vertreten</b> —
    /// dieselbe Entscheidung wie im WPF-Kopf, und die Begründung steht dort
    /// (<c>MarkdownFlow.Bildersatz</c>): Dieses Fenster zeigt vier mitgelieferte Dokumente aus
    /// der eigenen Assembly, deren Bildpfade sich gegen keine Datei auflösen lassen.
    ///
    /// <para>
    /// <b>Der Punkt ist, dass überhaupt etwas dasteht.</b> Seit Schritt ④ kennt der Zerleger
    /// <see cref="MdImage"/>, und dieser Maler hätte es sonst stillschweigend weggelassen —
    /// mit Schritt ⑤ und den Bildschirmfotos in den READMEs wäre daraus eine Lücke geworden,
    /// die aussieht, als fehlte im Dokument etwas.
    /// </para>
    /// </summary>
    private static Inline Bildersatz(MdImage bild)
    {
        var run = new Run(bild.Alt.Length > 0 ? $"[{bild.Alt}]" : "[Bild]");
        Farbe(run, TextElement.ForegroundProperty, "Brush.TextMuted");
        return run;
    }

    /// <summary>
    /// Web-Verweise öffnen den Browser. Verweise auf andere <c>.md</c>-Dateien gehen an den
    /// Handler. Alles Übrige bleibt Text — eingefärbt, aber ohne Versprechen.
    ///
    /// <para>
    /// <b>Ein anklickbarer Verweis ist in Avalonia ein Steuerelement</b>, kein
    /// ausgezeichneter Text: <c>Run</c> kennt kein Klickereignis, und ein <c>Hyperlink</c>
    /// wie in WPF existiert nicht. Der Weg führt deshalb über
    /// <see cref="InlineUIContainer"/> — ein <see cref="TextBlock"/> mitten im Fließtext.
    /// Der bricht in sich nicht um; bei Verweistexten von wenigen Wörtern fällt das nicht
    /// ins Gewicht.
    /// </para>
    /// </summary>
    private static Inline Verweis(MdLink l, Dokumentverweise? handler)
    {
        bool web = l.Target.StartsWith("http", StringComparison.OrdinalIgnoreCase);

        // **Erst fragen, dann zeichnen** (Phase 5, Schritt ④): `Kann` sagt, ob dieses Ziel
        // überhaupt jemand öffnet — siehe `Dokumentverweise` für den Fund dahinter.
        // ⛔ Hier stand zusätzlich `l.Target.EndsWith(".md")` — und
        // `README.md#zwei-ausgaben-eine-app` endet nicht auf `.md`. Weg damit: `Kann`
        // beantwortet die Frage genauer, und zwei Stellen, die dasselbe entscheiden,
        // entscheiden es verschieden.
        bool dok = handler != null && handler.Kann(l.Target);

        if (!web && !dok)
        {
            // **Schlichter Text, ohne Akzentfarbe** — ein Sprungziel im Dokument
            // (`#abschnitt`) oder ein Ziel, das niemand annimmt.
            //
            // ⛔ Hier stand bis Schritt ④ „eingefärbt, damit er als Verweis zu erkennen ist,
            // aber ohne Versprechen. Genau so hält es der WPF-Kopf." **Der zweite Satz stimmte,
            // und deshalb war der erste in beiden Köpfen falsch:** Ein Verweis, der wie einer
            // aussieht und keiner ist, ist kein Hinweis, sondern eine Zusage, die niemand
            // einlöst. Und dieser Kopf ging noch weiter als der andere — unten bekam dasselbe
            // tote Ziel Unterstreichung, Handzeiger und Tooltip.
            return new Run(l.Text);
        }

        var tb = new TextBlock
        {
            Text = l.Text,
            Cursor = new Cursor(StandardCursorType.Hand),
            TextDecorations = TextDecorations.Underline,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Farbe(tb, TextBlock.ForegroundProperty, "Brush.Accent");
        ToolTip.SetTip(tb, l.Target);

        string ziel = l.Target;
        tb.PointerPressed += (_, e) =>
        {
            if (web) App.Platform.Shell.OpenExternal(ziel);
            else handler!.Oeffnen(ziel);
            e.Handled = true;
        };

        return new InlineUIContainer(tb) { BaselineAlignment = BaselineAlignment.TextBottom };
    }

    /// <summary>
    /// Eine Farbe aus dem Theme anbinden statt sie auszurechnen — damit ein offener Dialog
    /// beim Umschalten hell/dunkel mitzieht.
    /// </summary>
    private static void Farbe(AvaloniaObject c, AvaloniaProperty p, string schluessel) =>
        c[!p] = new DynamicResourceExtension(schluessel);
}
