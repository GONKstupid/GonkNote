using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GonkNote.Core.Services;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Welche Seiten eines PDF oder DOCX eingefügt werden — das Gegenstück zu
/// <c>FileInsertDialog</c> im WPF-Kopf, neu in Phase 4.5 (§4.58).
///
/// <para>
/// <b>Der Rückgabewert sind Nummern und keine Bilder.</b> Die Vorschau ist klein gerendert
/// (<see cref="PdfImporter.ThumbnailLongSide"/>); in voller Auflösung entstehen die Seiten
/// erst danach, und nur die gewählten. Wer hier die Bilder zurückgäbe, hätte die
/// Ersparnis wieder verschenkt.
/// </para>
/// <para>
/// <b>Ein Klick auf das Bild schaltet das Häkchen um</b>, nicht nur einer auf das Kästchen —
/// die Kachel ist das, wonach der Nutzer zielt.
/// </para>
/// </summary>
public partial class SeitenwahlWindow : Window
{
    private readonly List<CheckBox> _haken = [];

    /// <summary>Die gewählten Seiten (Nummern ab 0, aufsteigend) — leer heißt „abgebrochen".</summary>
    public List<int> Gewaehlt { get; } = [];

    /// <summary>Nur für den XAML-Lader; benutzt wird der Konstruktor mit den Seiten.</summary>
    public SeitenwahlWindow() => InitializeComponent();

    public SeitenwahlWindow(string dateiname, IReadOnlyList<PdfImporter.PdfPageImage> seiten)
    {
        InitializeComponent();

        Title = Loc.T("Dialog.ChoosePages", dateiname);
        InfoText.Text = Loc.T("Msg.PagesHint", seiten.Count);

        for (int i = 0; i < seiten.Count; i++)
        {
            var haken = new CheckBox
            {
                Content = Loc.T("Msg.PageN", i + 1),
                IsChecked = true,
                Margin = new Avalonia.Thickness(2, 4, 0, 0),
            };
            haken.IsCheckedChanged += (_, _) => OkKnopfNachfuehren();
            _haken.Add(haken);

            var kachel = new StackPanel { Margin = new Avalonia.Thickness(6) };
            kachel.Children.Add(Vorschau(seiten[i].Data, haken));
            kachel.Children.Add(haken);

            KachelFlaeche.Children.Add(kachel);
        }

        OkKnopfNachfuehren();
    }

    /// <summary>
    /// Das Vorschaubild in einem Rahmen. <b>Weiß hinterlegt</b>, weil eine gerenderte Seite
    /// durchsichtige Stellen haben kann — auf dunklem Grund sähe man dort das Fenster
    /// durchscheinen und hielte es für einen Fehler im Dokument.
    /// </summary>
    private Control Vorschau(byte[] daten, CheckBox haken)
    {
        Control inhalt;
        try
        {
            using var strom = new MemoryStream(daten);
            inhalt = new Image { Source = new Bitmap(strom), Stretch = Stretch.Uniform, Height = 168 };
        }
        catch
        {
            inhalt = new TextBlock { Text = "?", HorizontalAlignment = HorizontalAlignment.Center };
        }

        var rahmen = new Border
        {
            Background = Brushes.White,
            BorderBrush = this.FindResource("Brush.Border") as IBrush,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(3),
            Padding = new Avalonia.Thickness(3),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Child = inhalt,
        };
        rahmen.PointerReleased += (_, _) => haken.IsChecked = haken.IsChecked != true;
        return rahmen;
    }

    private int Angehakt() => _haken.Count(h => h.IsChecked == true);

    /// <summary>
    /// Der Knopf sagt, <b>wie viele</b> Seiten er einfügt — und ist gesperrt, wenn es keine
    /// gibt. Ein „Einfügen", das nichts einfügt, sieht aus wie ein Fehler.
    /// </summary>
    private void OkKnopfNachfuehren()
    {
        int n = Angehakt();
        OkKnopf.Content = n == 1 ? Loc.T("Msg.InsertOnePage") : Loc.T("Msg.InsertPages", n);
        OkKnopf.IsEnabled = n > 0;
    }

    private void AlleWaehlen_Click(object? s, RoutedEventArgs e) => _haken.ForEach(h => h.IsChecked = true);
    private void KeineWaehlen_Click(object? s, RoutedEventArgs e) => _haken.ForEach(h => h.IsChecked = false);

    private void Abbrechen_Click(object? s, RoutedEventArgs e)
    {
        // Gewaehlt bleibt leer — der Aufrufer unterscheidet daran „abgebrochen" von „nichts
        // gewählt", und Letzteres kann gar nicht vorkommen (der Knopf ist dann gesperrt).
        Close();
    }

    private void Ok_Click(object? s, RoutedEventArgs e)
    {
        Gewaehlt.Clear();
        for (int i = 0; i < _haken.Count; i++)
            if (_haken[i].IsChecked == true) Gewaehlt.Add(i);
        Close();
    }
}
