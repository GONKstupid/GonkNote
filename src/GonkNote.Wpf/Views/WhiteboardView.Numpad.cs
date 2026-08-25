using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using GonkNote.Core.Editing;

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
        _sizeHoldTimer = new DispatcherTimer { Interval = WbZahlenblock.Haltedauer };
        _sizeHoldTimer.Tick += (_, _) => { CancelSizeHold(); OpenSizeNumpad(); };
        _sizeHoldTimer.Start();
    }

    private void MoveSizeHold(Point p)
    {
        // Wird der Schieber gezogen (Bewegung), ist es kein Langdruck → normales Verhalten
        if (_sizeHoldTimer != null &&
            WbZahlenblock.IstZiehen(_sizeHoldStart.X, _sizeHoldStart.Y, p.X, p.Y))
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

        // Welche Taste was mit der Eingabe macht, entscheidet Core (§4.61); null heißt
        // abgelehnt (zweites Komma, zweite Nachkommastelle, über dem Höchstwert).
        if (WbZahlenblock.Taste(_numpadEntry, key, WidthSlider.Maximum) is not { } next) return;

        _numpadEntry = next;
        ApplyNumpad();
    }

    private void NumpadBack_Click(object sender, RoutedEventArgs e)
    {
        _numpadEntry = WbZahlenblock.Rueckschritt(_numpadEntry);
        ApplyNumpad();
    }

    private void ApplyNumpad()
    {
        NumpadDisplay.Text = WbZahlenblock.Anzeige(_numpadEntry);
        if (WbZahlenblock.Schieberwert(_numpadEntry, WidthSlider.Minimum) is { } v)
            WidthSlider.Value = v;   // setzt über ValueChanged die Größe
    }
}
