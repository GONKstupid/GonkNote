using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GonkNote.Core.Rendering;
using GonkNote.Core.Text;
using GonkNote.Core.Theming;
using GonkNote.Platform;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Das Diagramm-Werkzeug des Linux-Kopfs — das Gegenstück zu <c>ChartDialog</c> (§4.82).
///
/// <para>
/// <b>Es rechnet und zeichnet nichts selbst.</b> <see cref="TdChartEingabe"/> liest die
/// Felder, <see cref="TdChartLayout"/> rechnet, <see cref="TdRenderer"/> malt — alles in Core.
/// Der WPF-Kopf hatte dafür bis §4.82 <b>435 Zeilen eigene Zeichnung</b>; sie hier ein zweites
/// Mal zu schreiben wäre genau der Fehler gewesen, den §4.13 (Trefferprüfung), §4.25
/// (Diagramm-Rechnung) und §4.54 (Text und Zettel) dreimal vermieden haben.
/// </para>
/// <para>
/// <b>Und beide Köpfe liefern jetzt dasselbe ab:</b> ein <see cref="TdChart"/>, das seine
/// Zahlen behält. Wer ein Bild braucht — die Tafel, die kein Modell dafür hat —, sagt das
/// selbst und nimmt <see cref="TdRenderer.DiagrammPng"/>.
/// </para>
/// </summary>
public partial class DiagrammWindow : Window
{
    /// <summary>Das fertige Diagramm; <c>null</c> bei Abbruch oder leerer Eingabe.</summary>
    public TdChart? Ergebnis { get; private set; }

    /// <summary>Ob der Nutzer „Einfügen" gedrückt hat — nicht bloß, ob etwas rechenbar war.</summary>
    private bool _uebernommen;

    /// <summary>
    /// Die Farben dieses Diagramms. <b>Sie gehen mit ins Dokument</b>
    /// (<see cref="TdChart.Palette"/>) und liegen nicht statisch im Fenster wie im alten
    /// WPF-Dialog — dort galten sie für die Sitzung und waren beim nächsten Start weg (§4.21).
    /// </summary>
    private readonly List<string> _palette;

    /// <summary>
    /// Die sieben Arten in der Reihenfolge des WPF-Kopfs. <b>Beim Editor ist Windows die
    /// Vorlage</b> (§6) — auch in der Reihenfolge einer Klappliste.
    /// </summary>
    private static readonly (TdChartKind Art, string Schluessel)[] Arten =
    [
        (TdChartKind.Column, "Chart.Type.Column"),
        (TdChartKind.Bar, "Chart.Type.Bar"),
        (TdChartKind.Line, "Chart.Type.Line"),
        (TdChartKind.Scatter, "Chart.Type.Scatter"),
        (TdChartKind.ScatterLine, "Chart.Type.ScatterLine"),
        (TdChartKind.Pie, "Chart.Type.Pie"),
        (TdChartKind.Radar, "Chart.Type.Radar"),
    ];

    /// <summary>Nur für den XAML-Lader.</summary>
    public DiagrammWindow() : this(null) { }

    /// <param name="vorlage">
    /// Ein bestehendes Diagramm, dessen Werte in die Felder zurückgehen — <c>null</c> legt ein
    /// neues an. <b>Dass das geht, ist der Gewinn aus §4.82:</b> Vorher lag im Dokument eine
    /// Bitmap, aus der sich keine Zahl zurückholen ließ.
    /// </param>
    public DiagrammWindow(TdChart? vorlage)
    {
        InitializeComponent();

        _palette = vorlage is { Palette.Count: > 0 }
            ? [.. vorlage.Palette]
            : [.. TdChart.StandardPalette];

        // **Die Einträge stehen direkt in `Items` und nicht in `ItemsSource`** — bei gesetztem
        // `ItemsSource` ist `Items` nicht die Liste, gegen die man vergleicht, und ein
        // `SelectedItem` daraus bliebe leer (§4.81, dort hat es eine Messung gekostet).
        foreach (var (art, schluessel) in Arten)
            ArtWahl.Items.Add(new ComboBoxItem { Content = Loc.T(schluessel), Tag = art });

        ArtWahl.SelectedIndex = 0;

        // **Ein Fenster, zwei Aufgaben — und es sagt, welche gerade gilt** (§4.83). Bis
        // dahin stand über einer Änderung „Diagramm einfügen" und auf dem Knopf
        // „Einfügen": *am laufenden Programm gesehen, nicht im Bau.*
        if (vorlage is not null)
        {
            FelderFuellen(vorlage);
            Title = Loc.T("Chart.Title.Edit");
            OkKnopf.Content = Loc.T("Dlg.Apply");
        }

        FarbreiheBauen();
        Neuzeichnen();
    }

    /// <summary>Ein bestehendes Diagramm zurück in die Felder — der Rückweg aus Core.</summary>
    private void FelderFuellen(TdChart d)
    {
        for (int i = 0; i < Arten.Length; i++)
            if (Arten[i].Art == d.Kind) ArtWahl.SelectedIndex = i;

        TitelFeld.Text = d.Title;
        KategorienFeld.Text = TdChartEingabe.KategorienText(d);
        NamenFeld.Text = TdChartEingabe.NamenText(d);
        WerteFeld.Text = TdChartEingabe.WerteText(d);
    }

    // ==================== Die Farbreihe ====================

    private void FarbreiheBauen()
    {
        Farbreihe.Children.Clear();

        for (int i = 0; i < _palette.Count; i++)
        {
            int stelle = i;
            var kachel = new Button
            {
                Width = 24, Height = 24, Margin = new Thickness(0, 0, 4, 4), Padding = default,
                Background = new SolidColorBrush(Farbe(_palette[i])),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xDE, 0xEA)),
                BorderThickness = new Thickness(1),
                Cursor = new Cursor(StandardCursorType.Hand),
                [ToolTip.TipProperty] = Loc.T("Chart.Color.Change", i + 1),
            };
            kachel.Click += (_, _) =>
            {
                var start = HexColor.Parse(_palette[stelle], HexColor.Black);
                if (ColorPickerWindow.Waehlen(this, start, mitDeckkraft: false) is { } gewaehlt)
                {
                    _palette[stelle] = gewaehlt.ToString();
                    kachel.Background = new SolidColorBrush(gewaehlt.ToAvalonia());
                    Neuzeichnen();
                }
            };

            // Rechtsklick: Farbe wieder löschen (mindestens eine bleibt übrig)
            var weg = new MenuItem { Header = Loc.T("Chart.Color.Remove"), IsEnabled = _palette.Count > 1 };
            weg.Click += (_, _) =>
            {
                if (_palette.Count <= 1) return;
                _palette.RemoveAt(stelle);
                FarbreiheBauen();   // Stellen der Kacheln neu vergeben
                Neuzeichnen();
            };
            kachel.ContextMenu = new ContextMenu { ItemsSource = new[] { weg } };

            Farbreihe.Children.Add(kachel);
        }

        // „+": Farbwähler öffnen und die gewählte Farbe als neue Kachel anhängen –
        // beliebig oft wiederholbar (Nutzer-Wunsch: mehr als 6 Farben)
        var mehr = new Button
        {
            Width = 24, Height = 24, Margin = new Thickness(0, 0, 4, 4), Padding = default,
            Content = "+", FontSize = 14,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Foreground = this.FindResource("Brush.Text") as IBrush,
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xDE, 0xEA)),
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(StandardCursorType.Hand),
            [ToolTip.TipProperty] = Loc.T("Chart.Color.Add"),
        };
        mehr.Click += (_, _) =>
        {
            var start = HexColor.Parse(_palette[^1], HexColor.Black);
            if (ColorPickerWindow.Waehlen(this, start, mitDeckkraft: false) is { } gewaehlt)
            {
                _palette.Add(gewaehlt.ToString());
                FarbreiheBauen();   // Reihe inkl. „+" neu aufbauen
                Neuzeichnen();
            }
        };
        Farbreihe.Children.Add(mehr);
    }

    private static Color Farbe(string hex)
    {
        var c = HexColor.Parse(hex, HexColor.Black);
        return Color.FromRgb(c.R, c.G, c.B);
    }

    // ==================== Vorschau ====================

    private void Eingabe_Geaendert(object? sender, RoutedEventArgs e) => Neuzeichnen();

    private void Eingabe_Geaendert(object? sender, SelectionChangedEventArgs e) => Neuzeichnen();

    private TdChartKind Art =>
        ArtWahl.SelectedItem is ComboBoxItem { Tag: TdChartKind art } ? art : TdChartKind.Column;

    /// <summary>
    /// Die Maße in Zentimetern — <b>dieselben wie im WPF-Kopf</b>, damit dasselbe Diagramm in
    /// beiden Köpfen gleich groß im Dokument steht. 14 × 8 cm passt in A4 mit gewöhnlichen
    /// Rändern.
    /// </summary>
    private const double BreiteCm = 14, HoeheCm = 8;

    private void Neuzeichnen()
    {
        if (Vorschau is null) return;

        Ergebnis = TdChartEingabe.Lesen(
            Art, TitelFeld.Text, KategorienFeld.Text, NamenFeld.Text, WerteFeld.Text,
            BreiteCm, HoeheCm, _palette);

        if (Ergebnis is null)
        {
            FehlerText.Text = Loc.T("Chart.Error.NoValues");
            Vorschau.Source = null;
            return;
        }

        // **Ein Hinweis und keine Sperre:** Core zeichnet unter drei Ecken den
        // Platzhalterkasten statt einer Strecke, die wie ein Zeichenfehler aussieht (§4.24).
        // Der Kasten sagt „hier fehlt etwas"; *was* fehlt, sagt nur dieses Fenster.
        FehlerText.Text = Ergebnis.Kind == TdChartKind.Radar && Ergebnis.Punktzahl() < 3
            ? Loc.T("Chart.Error.RadarNeedsThree")
            : "";

        // **Weiß, und der erste Anlauf war hier falsch** (§4.82): „durchsichtig, der Kasten
        // bringt seinen Grund mit" klang richtig und war am laufenden Programm unlesbar — der
        // Zeichner malt Titel und Achsen in **dunkler Tinte**, weil ein Diagramm für helles
        // Papier gedacht ist. Auf dem dunklen Vorschaukasten verschwand die Überschrift fast
        // ganz. Die Vorschau zeigt damit auch das, was später auf der Seite steht.
        using var strom = new MemoryStream(TdRenderer.DiagrammPng(Ergebnis, SKColors.White));
        Vorschau.Source = new Bitmap(strom);
    }

    // ==================== Schluss ====================

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Neuzeichnen();
        if (Ergebnis is null) { FehlerText.Text = Loc.T("Chart.Error.NoValues"); return; }

        _uebernommen = true;
        Close();
    }

    private void Abbrechen_Click(object? sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Zeigt das Fenster modal an; <c>null</c> bei Abbruch — <b>synchron wie im WPF-Kopf</b>,
    /// dieselbe Wartemechanik wie beim Farbwähler (<see cref="Modal"/>).
    /// </summary>
    public static TdChart? Waehlen(Window? besitzer, TdChart? vorlage = null)
    {
        if (besitzer is null) return null;

        var fenster = new DiagrammWindow(vorlage);
        Modal.Warte(fenster.ShowDialog(besitzer));

        // **Nicht `Ergebnis is not null` allein:** Die Vorschau rechnet bei jedem Tastendruck
        // und füllt `Ergebnis` dabei. Wer abbricht, hätte sonst trotzdem ein Diagramm im Blatt.
        return fenster._uebernommen ? fenster.Ergebnis : null;
    }
}
