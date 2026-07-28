using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace GonkNote.Views;

/// <summary>
/// Zahleneingabe für die Werkzeuggröße (Vorbild Adobe Fresco): Langes Drücken auf den
/// Größen-Schieber, die Wertanzeige oder das Icon öffnet einen kleinen Zahlenblock im
/// App-Stil. Er bleibt offen, bis klar daneben geklickt wird. Die Eingabe wird direkt auf
/// den Schieber angewandt und auf dessen Bereich begrenzt; gesteuert wird damit die
/// Strichstärke bzw. – beim Radierer – dessen Größe.
/// </summary>
public partial class WhiteboardView
{
    /// <summary>So lange muss gedrückt werden, bis der Zahlenblock aufgeht.</summary>
    private static readonly TimeSpan HoldToOpen = TimeSpan.FromMilliseconds(500);

    /// <summary>Ab dieser Bewegung ist es ein Ziehen am Schieber und kein Langdruck.</summary>
    private const double HoldSlack = 8;

    private DispatcherTimer? _sizeHoldTimer;
    private Point _sizeHoldStart;
    private string _numpadEntry = "";

    // ---------- Langdruck auf Schieber, Wertanzeige oder Icon ----------

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
        _sizeHoldTimer = new DispatcherTimer { Interval = HoldToOpen };
        _sizeHoldTimer.Tick += (_, _) => { CancelSizeHold(); OpenSizeNumpad(); };
        _sizeHoldTimer.Start();
    }

    private void MoveSizeHold(Point p)
    {
        // Wird der Schieber gezogen (Bewegung), ist es kein Langdruck → normales Verhalten
        if (_sizeHoldTimer != null &&
            (Math.Abs(p.X - _sizeHoldStart.X) > HoldSlack || Math.Abs(p.Y - _sizeHoldStart.Y) > HoldSlack))
            CancelSizeHold();
    }

    private void CancelSizeHold()
    {
        _sizeHoldTimer?.Stop();
        _sizeHoldTimer = null;
    }

    // ---------- Zahlenblock ----------

    private void OpenSizeNumpad()
    {
        _numpadEntry = "";
        NumpadDisplay.Text = ActiveSize.ToString("0.#");
        SizeNumpad.IsOpen = true;

        // StaysOpen=True: WPF schließt von sich aus nie. Wann geschlossen wird, entscheidet
        // allein SizeNumpad_OutsideDown – sonst schnappt der Block schon beim Zielen zu.
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
        if (!BelongsToNumpad(e.OriginalSource)) CloseSizeNumpad();
    }

    // sender ist nullable, weil PreviewTouchDown ein EventHandler<TouchEventArgs> ist
    private void SizeNumpad_OutsideTouch(object? sender, TouchEventArgs e)
    {
        if (!BelongsToNumpad(e.OriginalSource)) CloseSizeNumpad();
    }

    /// <summary>
    /// Gehört das angetippte Element zum Zahlenblock selbst oder zu einem seiner Auslöser
    /// (Schieber, Wertanzeige, Icon)? Nur ein Klick klar daneben schließt ihn.
    /// <para>
    /// Der Popup-Inhalt lebt in einem **eigenen Fenster** mit eigenem Visual-Baum; ein
    /// Hochlaufen vom Klickziel aus erreicht die Werkzeugleiste also nie. Genau das war der
    /// Fehler: jeder Ziffernklick galt als „außerhalb" und schloss den Block, noch bevor die
    /// Zahl ankam. Deshalb wird zuerst gegen den Popup-Inhalt selbst geprüft.
    /// </para>
    /// </summary>
    private bool BelongsToNumpad(object? src)
    {
        if (src is not DependencyObject start) return false;

        if (SizeNumpad.Child is Visual content && start is Visual clicked &&
            (ReferenceEquals(content, clicked) || content.IsAncestorOf(clicked)))
            return true;

        for (var d = start; d != null; d = ParentOf(d))
            if (ReferenceEquals(d, WidthSlider) || ReferenceEquals(d, WidthLabel) ||
                ReferenceEquals(d, WidthIcon))
                return true;

        return false;
    }

    /// <summary>Nächster Vorfahr – im Visual-Baum, sonst im logischen Baum.</summary>
    private static DependencyObject? ParentOf(DependencyObject d) =>
        (d is Visual or Visual3D ? VisualTreeHelper.GetParent(d) : null) ?? LogicalTreeHelper.GetParent(d);

    private void NumpadKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Content is not string key) return;

        string next;
        if (key == ",")
        {
            if (_numpadEntry.Contains(',')) return;
            next = (_numpadEntry.Length == 0 ? "0" : _numpadEntry) + ",";
        }
        else
        {
            int comma = _numpadEntry.IndexOf(',');
            if (comma >= 0 && _numpadEntry.Length - comma > 1) return;   // eine Nachkommastelle
            next = _numpadEntry + key;
        }

        // Eine Eingabe über dem Höchstwert wird gar nicht erst angenommen. Sonst stünde im
        // Display etwas anderes als die tatsächlich eingestellte (geklemmte) Größe.
        if (Parse(next) > WidthSlider.Maximum) return;

        _numpadEntry = next;
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
        if (Parse(_numpadEntry) is { } v)
            WidthSlider.Value = Math.Max(v, WidthSlider.Minimum);   // setzt über ValueChanged die Größe
    }

    /// <summary>
    /// Zahlenwert der Eingabe; null, solange sie (noch) keine Zahl ergibt (leer oder "0,").
    /// Das Komma der Tastatur wird für die Umwandlung zum Punkt.
    /// </summary>
    private static double? Parse(string entry) =>
        double.TryParse(entry.TrimEnd(',').Replace(',', '.'), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double v)
            ? v
            : null;
}
