using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.Core.Rendering;
using GonkNote.Core.Text;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Diagramm-Werkzeug: Kategorien + eine oder mehrere Werte-Reihen (je Zeile eine Kurve), Typ
/// wählbar (Säulen/Balken/Linie/Punkt/Punkt+Linie/Kuchen/Netz), anpassbare Farben,
/// Live-Vorschau.
///
/// <para>
/// <b>Das Ergebnis ist ein <see cref="TdChart"/> und keine Bitmap mehr</b> (HANDOFF §4.82).
/// Bis dahin rechnete und zeichnete dieses Fenster die sieben Diagrammarten selbst, mit
/// <c>DrawingVisual</c> — <b>ein zweites Mal</b>, denn Core kann sie seit §4.25
/// (<see cref="TdChartLayout"/> rechnet, <see cref="TdRenderer"/> malt). Und es lieferte
/// Pixel ab: <b>die Zahlen waren im selben Augenblick verloren</b>, ein Diagramm ließ sich
/// nie wieder ändern, und beim Export ging ein Bild hinaus, wo Word ein Diagramm erwartet
/// (§4.21).
/// </para>
/// <para>
/// <b>Wer ein Bild braucht, sagt das jetzt selbst.</b> Der Editor legt das
/// <see cref="TdChart"/> ins Modell und lässt es bei jedem Laden neu zeichnen; die Tafel
/// kennt kein Modell dafür und nimmt <see cref="TdRenderer.DiagrammPng"/>. Das ist dort kein
/// Verlust derselben Sorte, sondern die Grenze ihres Modells.
/// </para>
/// </summary>
public partial class ChartDialog : Window
{
    /// <summary>
    /// Das fertige Diagramm — <c>null</c>, solange die Eingabe keine Reihe hergibt.
    /// </summary>
    public TdChart? Result { get; private set; }

    /// <summary>
    /// Die Farben dieses Diagramms.
    /// <para>
    /// <b>Nicht mehr statisch</b>, und das ist die zweite Hälfte des Befunds aus §4.21: Die
    /// Palette lag im Fenster und galt für die Sitzung — beim nächsten Start war sie weg, und
    /// zwei Diagramme in derselben Datei konnten verschieden aussehen, ohne dass die Datei den
    /// Unterschied kannte. Jetzt geht sie mit dem Diagramm ins Dokument
    /// (<see cref="TdChart.Palette"/>), und ein geöffnetes Diagramm bringt seine eigene mit.
    /// </para>
    /// </summary>
    private readonly List<string> _palette;

    /// <summary>Ein neues Diagramm — oder eines, das schon im Dokument steht.</summary>
    /// <param name="vorlage">
    /// Ein bestehendes Diagramm, dessen Werte in die Felder zurückgehen. <c>null</c> legt ein
    /// neues an. <b>Dass das überhaupt geht, ist der Gewinn aus §4.82:</b> Bis dahin lag im
    /// Text eine Bitmap, aus der sich keine Zahl zurückholen ließ — ein Tippfehler in einer
    /// Kategorie kostete die ganze Eingabe.
    /// </param>
    public ChartDialog(TdChart? vorlage = null)
    {
        InitializeComponent();

        _palette = vorlage is { Palette.Count: > 0 }
            ? [.. vorlage.Palette]
            : [.. TdChart.StandardPalette];

        // **Ein Fenster, zwei Aufgaben — und es sagt, welche gerade gilt** (§4.83). Bis
        // dahin stand ueber einer Aenderung "Diagramm einfuegen" und auf dem Knopf
        // "Einfuegen": *am laufenden Programm gesehen, nicht im Bau.*
        if (vorlage is not null)
        {
            FelderFuellen(vorlage);
            Title = Loc.T("Chart.Title.Edit");
            OkKnopf.Content = Loc.T("Dlg.Apply");
        }

        FarbreiheBauen();
        Loaded += (_, _) => Neuzeichnen();
    }

    /// <summary>Ein bestehendes Diagramm zurück in die Felder — der Rückweg aus Core.</summary>
    private void FelderFuellen(TdChart d)
    {
        foreach (ComboBoxItem eintrag in TypeCombo.Items)
            eintrag.IsSelected = ArtAus((string)eintrag.Tag) == d.Kind;

        TitleBox.Text = d.Title;
        LabelsBox.Text = TdChartEingabe.KategorienText(d);
        SeriesBox.Text = TdChartEingabe.NamenText(d);
        ValuesBox.Text = TdChartEingabe.WerteText(d);
    }

    // ==================== Die Farbreihe ====================

    private void FarbreiheBauen()
    {
        ColorRow.Children.Clear();

        for (int i = 0; i < _palette.Count; i++)
        {
            int stelle = i;
            var kachel = new Button
            {
                Width = 24, Height = 24, Margin = new Thickness(0, 0, 4, 4),
                Background = new SolidColorBrush(Farbe(_palette[i])),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xDE, 0xEA)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                // **Der Text kam bis §4.82 fest auf Deutsch aus dem Code** — dieselbe Sorte
                // Fund wie in §4.74, §4.75, §4.80 und §4.81, jetzt zum fünften Mal in Folge.
                ToolTip = Loc.T("Chart.Color.Change", i + 1),
            };
            kachel.Click += (_, _) =>
            {
                if (ColorPickerDialog.Pick(this, Farbe(_palette[stelle]), allowAlpha: false) is { } gewaehlt)
                {
                    _palette[stelle] = Hex(gewaehlt);
                    ((SolidColorBrush)kachel.Background).Color = gewaehlt;
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
            var menue = new ContextMenu();
            menue.Items.Add(weg);
            kachel.ContextMenu = menue;

            ColorRow.Children.Add(kachel);
        }

        // „+": Farbwähler öffnen und die gewählte Farbe als neue Kachel anhängen –
        // beliebig oft wiederholbar (Nutzer-Wunsch: mehr als 6 Farben)
        var mehr = new Button
        {
            Width = 24, Height = 24, Margin = new Thickness(0, 0, 4, 4),
            Content = "+", FontSize = 14, Padding = new Thickness(0, -2, 0, 0),
            Foreground = (Brush)FindResource("Brush.Text"),
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xDE, 0xEA)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = Loc.T("Chart.Color.Add"),
        };
        mehr.Click += (_, _) =>
        {
            if (ColorPickerDialog.Pick(this, Farbe(_palette[^1]), allowAlpha: false) is { } gewaehlt)
            {
                _palette.Add(Hex(gewaehlt));
                FarbreiheBauen();   // Reihe inkl. „+" neu aufbauen
                Neuzeichnen();
            }
        };
        ColorRow.Children.Add(mehr);
    }

    private static Color Farbe(string hex) =>
        ColorConverter.ConvertFromString(hex) is Color c ? c : Colors.Black;

    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // ==================== Vorschau ====================

    private void Input_Changed(object sender, RoutedEventArgs e) => Neuzeichnen();

    /// <summary>Die gewählte Art. Die Kennungen im XAML sind die von <see cref="TdChartKind"/>.</summary>
    private static TdChartKind ArtAus(string tag) => tag switch
    {
        "bar" => TdChartKind.Bar,
        "line" => TdChartKind.Line,
        "scatter" => TdChartKind.Scatter,
        "scatterline" => TdChartKind.ScatterLine,
        "pie" => TdChartKind.Pie,
        "radar" => TdChartKind.Radar,
        _ => TdChartKind.Column,
    };

    private TdChartKind Art => ArtAus((string)((ComboBoxItem)TypeCombo.SelectedItem).Tag);

    /// <summary>
    /// Die Maße des eingefügten Diagramms in Zentimetern.
    /// <para>
    /// <b>Zentimeter und nicht Pixel</b> — das Modell rechnet so, und damit steht die Größe im
    /// Dokument fest, statt von der Auflösung des Rechners abzuhängen, auf dem es eingefügt
    /// wurde. 14 × 8 cm passt in A4 mit gewöhnlichen Rändern und hält das Seitenverhältnis der
    /// bisherigen Vorschau (560 × 320).
    /// </para>
    /// </summary>
    private const double BreiteCm = 14, HoeheCm = 8;

    private void Neuzeichnen()
    {
        if (Preview == null) return;

        Result = TdChartEingabe.Lesen(
            Art, TitleBox.Text, LabelsBox.Text, SeriesBox.Text, ValuesBox.Text,
            BreiteCm, HoeheCm, _palette);

        if (Result == null)
        {
            ErrorText.Text = Loc.T("Chart.Error.NoValues");
            Preview.Source = null;
            return;
        }

        // **Ein Hinweis und keine Sperre:** Core zeichnet unter drei Ecken den
        // Platzhalterkasten statt einer Strecke, die wie ein Zeichenfehler aussieht (§4.24).
        // Der Kasten sagt „hier fehlt etwas"; *was* fehlt, sagt nur dieses Fenster.
        ErrorText.Text = Result.Kind == TdChartKind.Radar && Result.Punktzahl() < 3
            ? Loc.T("Chart.Error.RadarNeedsThree")
            : "";

        // **Weiß, und der erste Anlauf war hier falsch** (§4.82): „durchsichtig, der Kasten
        // bringt seinen Grund mit" klang richtig und war am laufenden Programm unlesbar — der
        // Zeichner malt Titel und Achsen in **dunkler Tinte**, weil ein Diagramm für helles
        // Papier gedacht ist. Auf dem dunklen Vorschaukasten verschwand die Überschrift fast
        // ganz. Die Vorschau zeigt damit auch das, was später auf der Seite steht.
        Preview.Source = Abbild(TdRenderer.DiagrammPng(Result, SKColors.White));
    }

    private static BitmapImage Abbild(byte[] png)
    {
        var quelle = new BitmapImage();
        quelle.BeginInit();
        quelle.CacheOption = BitmapCacheOption.OnLoad;
        quelle.StreamSource = new MemoryStream(png);
        quelle.EndInit();
        quelle.Freeze();
        return quelle;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Neuzeichnen();
        if (Result == null) { ErrorText.Text = Loc.T("Chart.Error.NoValues"); return; }
        DialogResult = true;
    }
}
