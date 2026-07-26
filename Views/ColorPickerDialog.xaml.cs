using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GonkNote.Views;

/// <summary>HSV-Farbwähler mit Hex-Eingabe und optionaler Deckkraft.</summary>
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

        (_h, _s, _v) = RgbToHsv(initial.R, initial.G, initial.B);
        _a = allowAlpha ? initial.A : (byte)255;
        Loaded += (_, _) => UpdateUi();
    }

    /// <summary>Zeigt den Dialog an; null bei Abbruch.</summary>
    public static Color? Pick(Window? owner, Color initial, bool allowAlpha = true)
    {
        var dlg = new ColorPickerDialog(initial, allowAlpha) { Owner = owner };
        return dlg.ShowDialog() == true ? dlg.SelectedColor : null;
    }

    private Color CurrentColor()
    {
        var (r, g, b) = HsvToRgb(_h, _s, _v);
        return Color.FromArgb(_a, r, g, b);
    }

    private void UpdateUi()
    {
        var (hr, hg, hb) = HsvToRgb(_h, 1, 1);
        HueBrush.Color = Color.FromRgb(hr, hg, hb);

        Canvas.SetLeft(SvThumb, _s * SvArea.ActualWidth - 7);
        Canvas.SetTop(SvThumb, (1 - _v) * SvArea.ActualHeight - 7);
        Canvas.SetTop(HueThumb, _h / 360.0 * HueArea.ActualHeight - 3);
        Canvas.SetLeft(AlphaThumb, _a / 255.0 * AlphaArea.ActualWidth - 3);

        var c = CurrentColor();
        AlphaStop0.Color = Color.FromArgb(0, c.R, c.G, c.B);
        AlphaStop1.Color = Color.FromArgb(255, c.R, c.G, c.B);
        AlphaLabel.Text = $"{Math.Round(_a / 255.0 * 100)} %";
        Preview.Background = new SolidColorBrush(c);

        if (!_updatingHex)
            HexBox.Text = _a == 255 ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
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

    private void TryParseHex()
    {
        var text = HexBox.Text.Trim();
        if (!text.StartsWith('#')) text = "#" + text;
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(text);
            (_h, _s, _v) = RgbToHsv(c.R, c.G, c.B);
            if (AlphaRow.Visibility == Visibility.Visible) _a = c.A;
            _updatingHex = true;
            UpdateUi();
            _updatingHex = false;
        }
        catch
        {
            UpdateUi(); // ungültig → zurücksetzen
        }
    }

    private void Ok_Click(object s, RoutedEventArgs e)
    {
        SelectedColor = CurrentColor();
        DialogResult = true;
    }

    // ---- HSV-Mathematik ----

    private static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
    {
        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        double max = Math.Max(rf, Math.Max(gf, bf)), min = Math.Min(rf, Math.Min(gf, bf));
        double d = max - min;
        double h = 0;
        if (d > 0)
        {
            if (max == rf) h = 60 * (((gf - bf) / d) % 6);
            else if (max == gf) h = 60 * ((bf - rf) / d + 2);
            else h = 60 * ((rf - gf) / d + 4);
        }
        if (h < 0) h += 360;
        return (h, max == 0 ? 0 : d / max, max);
    }

    private static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
    {
        double c = v * s;
        double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        double m = v - c;
        var (rf, gf, bf) = ((int)(h / 60) % 6) switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return ((byte)Math.Round((rf + m) * 255), (byte)Math.Round((gf + m) * 255), (byte)Math.Round((bf + m) * 255));
    }
}
