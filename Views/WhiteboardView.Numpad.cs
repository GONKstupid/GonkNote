using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GonkNote.Views;

/// <summary>
/// Zahleneingabe für die Strichstärke (Vorbild Adobe Fresco): Langes Drücken auf den
/// Größen-Slider – oder ein Klick auf die Wertanzeige daneben – öffnet einen kleinen
/// Zahlenblock im App-Stil. Die Eingabe wird direkt auf den Slider (und damit die
/// aktive Strichstärke) angewandt und auf den Slider-Bereich begrenzt.
/// </summary>
public partial class WhiteboardView
{
    private DispatcherTimer? _sizeHoldTimer;
    private Point _sizeHoldStart;
    private string _numpadEntry = "";

    // ---------- Langdruck auf den Slider ----------

    private void SizeInput_Down(object sender, MouseButtonEventArgs e) => StartSizeHold(e.GetPosition(this));

    private void SizeInput_Move(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) MoveSizeHold(e.GetPosition(this));
    }

    private void SizeInput_Up(object sender, MouseButtonEventArgs e) => CancelSizeHold();

    private void SizeInput_TouchDown(object sender, TouchEventArgs e) => StartSizeHold(e.GetTouchPoint(this).Position);
    private void SizeInput_TouchMove(object sender, TouchEventArgs e) => MoveSizeHold(e.GetTouchPoint(this).Position);
    private void SizeInput_TouchUp(object sender, TouchEventArgs e) => CancelSizeHold();

    private void StartSizeHold(Point p)
    {
        CancelSizeHold();
        _sizeHoldStart = p;
        _sizeHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _sizeHoldTimer.Tick += (_, _) => { CancelSizeHold(); OpenSizeNumpad(); };
        _sizeHoldTimer.Start();
    }

    private void MoveSizeHold(Point p)
    {
        // Wird der Slider gezogen (Bewegung), ist es kein Langdruck → normales Verhalten
        if (_sizeHoldTimer != null &&
            (Math.Abs(p.X - _sizeHoldStart.X) > 8 || Math.Abs(p.Y - _sizeHoldStart.Y) > 8))
            CancelSizeHold();
    }

    private void CancelSizeHold()
    {
        _sizeHoldTimer?.Stop();
        _sizeHoldTimer = null;
    }

    // ---------- Zahlenblock ----------

    private void OpenSizeNumpad_Click(object sender, RoutedEventArgs e) => OpenSizeNumpad();

    private void OpenSizeNumpad()
    {
        _numpadEntry = "";
        NumpadDisplay.Text = _width.ToString("0.#");
        SizeNumpad.IsOpen = true;
    }

    private void NumpadKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Content is not string key) return;

        if (key == ",")
        {
            if (_numpadEntry.Contains(',')) return;
            _numpadEntry = (_numpadEntry.Length == 0 ? "0" : _numpadEntry) + ",";
        }
        else
        {
            int comma = _numpadEntry.IndexOf(',');
            // max. eine Nachkommastelle, max. zwei Vorkommastellen (Slider bis 20)
            if (comma >= 0)
            {
                if (_numpadEntry.Length - comma > 1) return;
            }
            else if (_numpadEntry.Length >= 2)
            {
                return;
            }
            _numpadEntry += key;
        }
        ApplyNumpad();
    }

    private void NumpadBack_Click(object sender, RoutedEventArgs e)
    {
        if (_numpadEntry.Length > 0) _numpadEntry = _numpadEntry[..^1];
        ApplyNumpad();
    }

    private void ApplyNumpad()
    {
        NumpadDisplay.Text = _numpadEntry.Length > 0 ? _numpadEntry : "0";
        string parseable = _numpadEntry.TrimEnd(',').Replace(',', '.');
        if (double.TryParse(parseable, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
        {
            v = Math.Clamp(v, WidthSlider.Minimum, WidthSlider.Maximum);
            WidthSlider.Value = v;   // löst WidthSlider_ValueChanged aus (setzt _width + Anzeige)
        }
    }
}
