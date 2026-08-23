using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using GonkNote.Core.Theming;
using GonkNote.Platform;

namespace GonkNote.Views;

/// <summary>
/// HSV-Farbwähler mit Hex-Eingabe und Deckkraft — das Gegenstück zu
/// <c>ColorPickerDialog</c> im WPF-Kopf, neu in Phase 4.5.
///
/// <para>
/// <b>Gerechnet wird in <see cref="HexColor"/> (Core)</b>: Zerlegen, Zusammensetzen, Lesen
/// und Schreiben der Hex-Schreibweise. Hier steht nur die Oberfläche. Beide Köpfe rechnen
/// damit dieselben Farben aus denselben Zeigerpositionen — sonst hätte derselbe Griff an
/// derselben Stelle je Kopf eine andere Farbe ergeben.
/// </para>
/// <para>
/// <b>H, S und V sind eigener Zustand und werden nicht aus der Farbe zurückgerechnet.</b>
/// Bei Grau ist der Farbton nicht bestimmt (<see cref="HexColor.ToHsv"/> meldet 0) — wer
/// bei jeder Bewegung neu zerlegt, dessen Farbtonzeiger springt auf Rot, sobald der Nutzer
/// die Sättigung auf null zieht.
/// </para>
/// </summary>
public partial class ColorPickerWindow : Window
{
    private double _h;         // 0..360
    private double _s = 1;     // 0..1
    private double _v = 1;     // 0..1
    private byte _a = 0xFF;
    private bool _ziehtSv, _ziehtFarbton, _ziehtDeckkraft;
    private bool _schreibtHex;

    /// <summary>Die gewählte Farbe — erst nach „Übernehmen" gesetzt.</summary>
    public HexColor Gewaehlt { get; private set; }

    private bool _uebernommen;

    public ColorPickerWindow() : this(HexColor.Black, true) { }

    public ColorPickerWindow(HexColor start, bool mitDeckkraft)
    {
        // Nicht AvaloniaXamlLoader.Load(this) — nur das erzeugte InitializeComponent() weist
        // die x:Name-Felder zu (HANDOFF §7).
        InitializeComponent();

        if (!mitDeckkraft) DeckkraftZeile.IsVisible = false;

        (_h, _s, _v) = start.ToHsv();
        _a = mitDeckkraft ? start.A : (byte)0xFF;
        Gewaehlt = start;

        // Erst wenn die Flächen eine Größe haben, lassen sich die Griffe setzen.
        Opened += (_, _) => Nachfuehren();
    }

    /// <summary>
    /// Zeigt den Wähler modal an; <c>null</c> bei Abbruch. Synchron wie im WPF-Kopf — die
    /// Wartemechanik steht in <see cref="Modal"/>.
    /// </summary>
    public static HexColor? Waehlen(Window? besitzer, HexColor start, bool mitDeckkraft = true)
    {
        var fenster = new ColorPickerWindow(start, mitDeckkraft);
        if (besitzer == null) return null;

        Modal.Warte(fenster.ShowDialog(besitzer));
        return fenster._uebernommen ? fenster.Gewaehlt : null;
    }

    private HexColor Aktuell() => HexColor.FromHsv(_h, _s, _v, _a);

    // ==================== Anzeige ====================

    private void Nachfuehren()
    {
        // Grundton der Sättigungsfläche: voller Farbton, darüber liegen die zwei Verläufe
        // aus der XAML (weiß nach rechts durchsichtig, durchsichtig nach schwarz).
        SvFlaeche.Background = new SolidColorBrush(HexColor.FromHsv(_h, 1, 1).ToAvalonia());

        Setze(SvGriff, _s * SvFlaeche.Bounds.Width - 7, (1 - _v) * SvFlaeche.Bounds.Height - 7);
        Setze(SvGriffAussen, _s * SvFlaeche.Bounds.Width - 8, (1 - _v) * SvFlaeche.Bounds.Height - 8);
        Canvas.SetTop(FarbtonGriff, _h / 360.0 * FarbtonFlaeche.Bounds.Height - 3);
        Canvas.SetLeft(DeckkraftGriff, _a / 255.0 * DeckkraftFlaeche.Bounds.Width - 3);

        var c = Aktuell();

        // Der Deckkraftverlauf zeigt dieselbe Farbe von durchsichtig nach deckend.
        DeckkraftVerlauf.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(c.WithAlpha(0).ToAvalonia(), 0),
                new GradientStop(c.WithAlpha(0xFF).ToAvalonia(), 1),
            },
        };

        DeckkraftWert.Text = $"{Math.Round(_a / 255.0 * 100)} %";
        Vorschau.Background = c.ToBrush();

        // HexColor.ToString() liefert #RRGGBB, und #AARRGGBB nur bei nicht deckenden Farben.
        if (!_schreibtHex) HexFeld.Text = c.ToString();
    }

    private static void Setze(Control c, double x, double y)
    {
        Canvas.SetLeft(c, x);
        Canvas.SetTop(c, y);
    }

    // ==================== Die drei Flächen ====================
    //
    // Jede fängt den Zeiger beim Drücken ein (Pointer.Capture), sonst reißt das Ziehen ab,
    // sobald der Zeiger die Fläche verlässt — und ein Farbwähler, bei dem man nicht über
    // den Rand hinausziehen darf, fühlt sich kaputt an.

    private void SvFlaeche_Gedrueckt(object? s, PointerPressedEventArgs e)
    {
        _ziehtSv = true;
        e.Pointer.Capture(SvFlaeche);
        SvAnwenden(e.GetPosition(SvFlaeche));
    }

    private void SvFlaeche_Bewegt(object? s, PointerEventArgs e)
    {
        if (_ziehtSv) SvAnwenden(e.GetPosition(SvFlaeche));
    }

    private void Farbton_Gedrueckt(object? s, PointerPressedEventArgs e)
    {
        _ziehtFarbton = true;
        e.Pointer.Capture(FarbtonFlaeche);
        FarbtonAnwenden(e.GetPosition(FarbtonFlaeche));
    }

    private void Farbton_Bewegt(object? s, PointerEventArgs e)
    {
        if (_ziehtFarbton) FarbtonAnwenden(e.GetPosition(FarbtonFlaeche));
    }

    private void Deckkraft_Gedrueckt(object? s, PointerPressedEventArgs e)
    {
        _ziehtDeckkraft = true;
        e.Pointer.Capture(DeckkraftFlaeche);
        DeckkraftAnwenden(e.GetPosition(DeckkraftFlaeche));
    }

    private void Deckkraft_Bewegt(object? s, PointerEventArgs e)
    {
        if (_ziehtDeckkraft) DeckkraftAnwenden(e.GetPosition(DeckkraftFlaeche));
    }

    private void Flaeche_Losgelassen(object? s, PointerReleasedEventArgs e)
    {
        _ziehtSv = _ziehtFarbton = _ziehtDeckkraft = false;
        e.Pointer.Capture(null);
    }

    private void SvAnwenden(Point p)
    {
        _s = Math.Clamp(p.X / SvFlaeche.Bounds.Width, 0, 1);
        _v = 1 - Math.Clamp(p.Y / SvFlaeche.Bounds.Height, 0, 1);
        Nachfuehren();
    }

    private void FarbtonAnwenden(Point p)
    {
        _h = Math.Clamp(p.Y / FarbtonFlaeche.Bounds.Height, 0, 1) * 360.0;
        Nachfuehren();
    }

    private void DeckkraftAnwenden(Point p)
    {
        _a = (byte)Math.Round(Math.Clamp(p.X / DeckkraftFlaeche.Bounds.Width, 0, 1) * 255);
        Nachfuehren();
    }

    // ==================== Hex ====================

    private void HexFeld_Taste(object? s, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        HexUebernehmen();
        e.Handled = true;
    }

    private void HexFeld_Verlassen(object? s, RoutedEventArgs e) => HexUebernehmen();

    /// <summary>
    /// Übernimmt die getippte Farbe. Bei Unsinn wird stillschweigend zurückgesetzt statt
    /// gemeckert — der Nutzer sieht sofort, dass sich nichts bewegt hat.
    /// </summary>
    private void HexUebernehmen()
    {
        if (!HexColor.TryParse(HexFeld.Text, out var c))
        {
            Nachfuehren();   // ungültig → zurücksetzen
            return;
        }

        (_h, _s, _v) = c.ToHsv();
        if (DeckkraftZeile.IsVisible) _a = c.A;
        _schreibtHex = true;
        Nachfuehren();
        _schreibtHex = false;
    }

    // ==================== Knöpfe ====================

    private void Uebernehmen_Click(object? s, RoutedEventArgs e)
    {
        Gewaehlt = Aktuell();
        _uebernommen = true;
        Close();
    }

    private void Abbrechen_Click(object? s, RoutedEventArgs e) => Close();
}
