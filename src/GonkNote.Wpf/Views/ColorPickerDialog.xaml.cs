using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GonkNote.Core.Theming;

namespace GonkNote.Views;

/// <summary>
/// HSV-Farbwähler mit Hex-Eingabe und optionaler Deckkraft.
/// <para>
/// <b>Gerechnet wird seit Phase 4.5 in <see cref="HexColor"/></b> — Zerlegen, Zusammensetzen,
/// Lesen und Schreiben der Hex-Schreibweise. Hier bleibt die Oberfläche: die Flächen, das
/// Ziehen, die Verläufe. Der Grund ist derselbe wie bei den Griffen in §4.51: der Linux-Kopf
/// bekommt denselben Wähler, und zwei Fassungen derselben Arithmetik driften auseinander.
/// </para>
/// <para>
/// <b>Eine benannte Änderung dabei:</b> das Hex-Feld nahm über <c>ColorConverter</c> auch
/// Farb<b>namen</b> an („Red", „CornflowerBlue"). Das tut es nicht mehr — es ist ein
/// Hex-Feld, es zeigt immer <c>#RRGGBB</c>, und ein Extra, das nur einer der beiden Köpfe
/// kann, ist gegen M2.
/// </para>
/// </summary>
public partial class ColorPickerDialog : Window
{
    private double _h;      // 0..360
    private double _s = 1;  // 0..1
    private double _v = 1;  // 0..1
    private byte _a = 255;
    private bool _dragSv, _dragHue, _dragAlpha;
    private bool _updatingHex;

    public Color SelectedColor { get; private set; }

    public ColorPickerDialog(Color initial, bool allowAlpha = true)
    {
        InitializeComponent();
        if (!allowAlpha) AlphaRow.Visibility = Visibility.Collapsed;

        (_h, _s, _v) = Hex(initial).ToHsv();
        _a = allowAlpha ? initial.A : (byte)255;
        Loaded += (_, _) => UpdateUi();
    }

    // ---- Übersetzung zwischen Core und WPF ----

    private static HexColor Hex(Color c) => new(c.A, c.R, c.G, c.B);

    private static Color Wpf(HexColor c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    /// <summary>Zeigt den Dialog an; null bei Abbruch.</summary>
    public static Color? Pick(Window? owner, Color initial, bool allowAlpha = true)
    {
        var dlg = new ColorPickerDialog(initial, allowAlpha) { Owner = owner };
        return dlg.ShowDialog() == true ? dlg.SelectedColor : null;
    }

    private Color CurrentColor() => Wpf(HexColor.FromHsv(_h, _s, _v, _a));

    private void UpdateUi()
    {
        HueBrush.Color = Wpf(HexColor.FromHsv(_h, 1, 1));

        Canvas.SetLeft(SvThumb, _s * SvArea.ActualWidth - 7);
        Canvas.SetTop(SvThumb, (1 - _v) * SvArea.ActualHeight - 7);
        Canvas.SetTop(HueThumb, _h / 360.0 * HueArea.ActualHeight - 3);
        Canvas.SetLeft(AlphaThumb, _a / 255.0 * AlphaArea.ActualWidth - 3);

        var c = CurrentColor();
        AlphaStop0.Color = Color.FromArgb(0, c.R, c.G, c.B);
        AlphaStop1.Color = Color.FromArgb(255, c.R, c.G, c.B);
        AlphaLabel.Text = $"{Math.Round(_a / 255.0 * 100)} %";
        Preview.Background = new SolidColorBrush(c);

        // HexColor.ToString() liefert genau diese zwei Formen: #RRGGBB, und #AARRGGBB nur
        // dann, wenn die Farbe nicht deckend ist.
        if (!_updatingHex) HexBox.Text = Hex(c).ToString();
    }

    // ---- Eingabeflächen ----

    private void SvArea_MouseDown(object s, MouseButtonEventArgs e)
    {
        _dragSv = true;
        SvArea.CaptureMouse();
        ApplySv(e.GetPosition(SvArea));
    }

    private void SvArea_MouseMove(object s, MouseEventArgs e)
    {
        if (_dragSv) ApplySv(e.GetPosition(SvArea));
    }

    private void HueArea_MouseDown(object s, MouseButtonEventArgs e)
    {
        _dragHue = true;
        HueArea.CaptureMouse();
        ApplyHue(e.GetPosition(HueArea));
    }

    private void HueArea_MouseMove(object s, MouseEventArgs e)
    {
        if (_dragHue) ApplyHue(e.GetPosition(HueArea));
    }

    private void AlphaArea_MouseDown(object s, MouseButtonEventArgs e)
    {
        _dragAlpha = true;
        AlphaArea.CaptureMouse();
        ApplyAlpha(e.GetPosition(AlphaArea));
    }

    private void AlphaArea_MouseMove(object s, MouseEventArgs e)
    {
        if (_dragAlpha) ApplyAlpha(e.GetPosition(AlphaArea));
    }

    private void Area_MouseUp(object s, MouseButtonEventArgs e)
    {
        _dragSv = _dragHue = _dragAlpha = false;
        ((UIElement)s).ReleaseMouseCapture();
    }

    private void ApplySv(Point p)
    {
        _s = Math.Clamp(p.X / SvArea.ActualWidth, 0, 1);
        _v = 1 - Math.Clamp(p.Y / SvArea.ActualHeight, 0, 1);
        UpdateUi();
    }

    private void ApplyHue(Point p)
    {
        _h = Math.Clamp(p.Y / HueArea.ActualHeight, 0, 1) * 360.0;
        UpdateUi();
    }

    private void ApplyAlpha(Point p)
    {
        _a = (byte)Math.Round(Math.Clamp(p.X / AlphaArea.ActualWidth, 0, 1) * 255);
        UpdateUi();
    }

    // ---- Hex ----

    private void HexBox_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { TryParseHex(); e.Handled = true; }
    }

    private void HexBox_LostFocus(object s, KeyboardFocusChangedEventArgs e) => TryParseHex();

    /// <summary>
    /// Übernimmt die getippte Farbe. Bei Unsinn wird stillschweigend zurückgesetzt statt
    /// gemeckert — der Nutzer sieht sofort, dass sich nichts bewegt hat.
    /// </summary>
    private void TryParseHex()
    {
        if (!HexColor.TryParse(HexBox.Text, out var c))
        {
            UpdateUi();   // ungültig → zurücksetzen
            return;
        }

        (_h, _s, _v) = c.ToHsv();
        if (AlphaRow.Visibility == Visibility.Visible) _a = c.A;
        _updatingHex = true;
        UpdateUi();
        _updatingHex = false;
    }

    private void Ok_Click(object s, RoutedEventArgs e)
    {
        SelectedColor = CurrentColor();
        DialogResult = true;
    }

    // Die HSV-Mathematik stand bis Phase 4.5 hier. Sie steht jetzt in HexColor (Core) —
    // siehe den Kopfkommentar.
}
