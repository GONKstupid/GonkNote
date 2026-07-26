using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GonkNote.Views;

/// <summary>
/// Zahleneingabe für die Werkzeuggröße (Vorbild Adobe Fresco): Langes Drücken auf den
/// Größen-Slider – oder ein Klick auf die Wertanzeige daneben – öffnet einen kleinen
/// Zahlenblock im App-Stil. Die Eingabe wird direkt auf den Slider angewandt und auf dessen
/// Bereich begrenzt; gesteuert wird damit die Strichstärke bzw. – beim Radierer – seine Größe.
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
        NumpadDisplay.Text = ActiveSize.ToString("0.#");
        SizeNumpad.IsOpen = true;
        // Bleibt offen (StaysOpen=True); schließt nur bei einem Klick klar außerhalb –
        // nicht im Popup (eigenes Fenster) und nicht auf den Größen-Steuerelementen.
        PreviewMouseDown -= SizeNumpad_OutsideDown;
        PreviewMouseDown += SizeNumpad_OutsideDown;
        PreviewTouchDown -= SizeNumpad_OutsideTouch;
        PreviewTouchDown += SizeNumpad_OutsideTouch;
    }

    private void CloseSizeNumpad()
    {
        SizeNumpad.IsOpen = false;
        PreviewMouseDown -= SizeNumpad_OutsideDown;
        PreviewTouchDown -= SizeNumpad_OutsideTouch;
    }

    private void SizeNumpad_OutsideDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsSizeTrigger(e.OriginalSource)) CloseSizeNumpad();
    }

    // sender ist nullable, weil PreviewTouchDown ein EventHandler<TouchEventArgs> ist
    private void SizeNumpad_OutsideTouch(object? sender, TouchEventArgs e)
    {
        if (!IsSizeTrigger(e.OriginalSource)) CloseSizeNumpad();
    }

    /// <summary>Gehört das Element zu den Größen-Auslösern (Slider, Icon, Wertanzeige)?</summary>
    private bool IsSizeTrigger(object? src)
    {
        var d = src as DependencyObject;
        while (d != null)
        {
            if (ReferenceEquals(d, WidthSlider) || ReferenceEquals(d, WidthLabel) || ReferenceEquals(d, WidthIcon))
                return true;
            d = d is System.Windows.Media.Visual ? System.Windows.Media.VisualTreeHelper.GetParent(d)
                                                  : LogicalTreeHelper.GetParent(d);
        }
        return false;
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
