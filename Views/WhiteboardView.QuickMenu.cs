using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GonkNote.Core.Models;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Schnellaktionen: floatende Icon-Leiste statt Rechtsklick-Menue.
/// </summary>
public partial class WhiteboardView
{
    // ============ Schnellaktionen (Quick-Options statt Rechtsklick-Menü) ============
    // Floatende Icon-Leiste im Toolbar-Look. Auslösung: Maus-Rechtsklick, zweite
    // Stift-Taste (Barrel-Button → RightTap-Geste) oder automatisch nach einer
    // Auswahl mit Lasso (L)/Verschieben (V). Keyboard-freie Nutzung.

    private SKPoint _contextCanvasPos;
    private int _quickShownTick;

    private void OnCanvasRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Deckt Maus-Rechtsklick UND Stifte ab, deren Barrel-Button auf „Rechtsklick"
        // gemappt ist (kommt als rechte Maustaste mit gesetztem StylusDevice).
        ShowQuickMenuAt(e.GetPosition(CanvasHost), autoPick: true);
        e.Handled = true;
    }

    private void OnCanvasStylusSystemGesture(object sender, StylusSystemGestureEventArgs e)
    {
        // Zweite Stift-Taste = RightTap-Geste (Fallback zu StylusButtonDown)
        if (e.SystemGesture != SystemGesture.RightTap) return;
        ShowQuickMenuAt(e.GetPosition(CanvasHost), autoPick: true);
        e.Handled = true;
    }

    private void OnCanvasStylusButtonDown(object sender, StylusButtonEventArgs e)
    {
        // Direkter Druck der zweiten Stift-Taste (Barrel-Button) – zuverlässiger als
        // die RightTap-Geste, auch wenn die Spitze die Fläche nicht berührt.
        if (e.StylusButton.Guid != StylusPointProperties.BarrelButton.Id) return;
        ShowQuickMenuAt(e.GetPosition(CanvasHost), autoPick: true);
        e.Handled = true;
    }

    // ---------- Langes Drücken (Stift oder Finger) ----------
    // Öffnet die Schnellaktionen, wenn der Zeiger ~600 ms an derselben Stelle
    // gedrückt bleibt – nur bei Lasso (L)/Verschieben (V)/Hand (H), damit die
    // Zeichenwerkzeuge (Stift ruht beim Schreiben oft kurz) ungestört bleiben.

    private System.Windows.Threading.DispatcherTimer? _holdTimer;
    private Point _holdStart;
    private bool _holdFromTouch;
    /// <summary>Das Loslassen nach einem Langdruck nicht mehr als Eingabe-Ende verarbeiten.</summary>
    private bool _suppressNextEndInput;

    private bool HoldToolActive => _tool is ToolType.Lasso or ToolType.Move or ToolType.Pan;

    private void StartHoldDetect(Point screen, bool fromTouch)
    {
        CancelHoldDetect();
        if (!HoldToolActive || _page == null || _vm == null) return;
        _holdStart = screen;
        _holdFromTouch = fromTouch;
        _holdTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600),
        };
        _holdTimer.Tick += HoldTimer_Tick;
        _holdTimer.Start();
    }

    private void MoveHoldDetect(Point screen)
    {
        if (_holdTimer != null &&
            (Math.Abs(screen.X - _holdStart.X) > 10 || Math.Abs(screen.Y - _holdStart.Y) > 10))
            CancelHoldDetect();
    }

    private void CancelHoldDetect()
    {
        if (_holdTimer == null) return;
        _holdTimer.Stop();
        _holdTimer.Tick -= HoldTimer_Tick;
        _holdTimer = null;
    }

    private void HoldTimer_Tick(object? sender, EventArgs e)
    {
        CancelHoldDetect();
        if (!HoldToolActive) return;

        // Die angefangene Interaktion verwerfen – der Zeiger ruhte, es geht nichts verloren
        _lassoPts = null;
        _movingSelection = false;
        _scalingSelection = false;
        _rotatingEl = null;
        if (_panning) EndPan();
        if (_holdFromTouch) _touches.Clear();          // Finger soll nicht weiter pannen
        else _suppressNextEndInput = true;             // Stift-Up nicht als Eingabe-Ende werten

        ShowQuickMenuAt(_holdStart, autoPick: true);
        Skia.InvalidateVisual();
    }

    /// <summary>Öffnet die Schnellaktionen an einer Zeigerposition (Rechtsklick/Stifttaste).</summary>
    private void ShowQuickMenuAt(Point screen, bool autoPick)
    {
        if (!CanShowQuickMenu()) return;

        // RightTap + synthetische rechte Maustaste beim Stift entprellen
        int now = Environment.TickCount;
        if (QuickMenu.Visibility == Visibility.Visible && now - _quickShownTick < 250) return;

        _contextCanvasPos = ToCanvas(screen);

        // Nichts ausgewählt? Objekt unter dem Zeiger anwählen, damit die Aktion darauf wirkt
        if (autoPick && _selection.Count == 0)
        {
            var pick = HitTestElement(_contextCanvasPos);
            if (pick != null)
            {
                _selection.Add(pick);
                ComputeSelectionBounds();
                Skia.InvalidateVisual();
            }
        }

        PrepareQuickMenu();
        QuickMenu.Visibility = Visibility.Visible;
        QuickMenu.UpdateLayout();
        PlaceQuickMenu(screen.X - QuickMenu.ActualWidth / 2, screen.Y + 12);
    }

    /// <summary>Öffnet die Schnellaktionen mittig über der aktuellen Auswahl.</summary>
    private void ShowQuickMenuForSelection()
    {
        if (!CanShowQuickMenu() || _selection.Count == 0) return;

        _contextCanvasPos = new SKPoint(_selectionBounds.MidX, _selectionBounds.Top);
        PrepareQuickMenu();
        QuickMenu.Visibility = Visibility.Visible;
        QuickMenu.UpdateLayout();
        double w = QuickMenu.ActualWidth, h = QuickMenu.ActualHeight;

        var tl = ToScreen(new SKPoint(_selectionBounds.Left, _selectionBounds.Top));
        var br = ToScreen(new SKPoint(_selectionBounds.Right, _selectionBounds.Bottom));
        double midX = (tl.X + br.X) / 2;
        double top = tl.Y - h - 10;
        if (top < 4) top = br.Y + 10;   // kein Platz oben → unter die Auswahl
        PlaceQuickMenu(midX - w / 2, top);
    }

    private bool CanShowQuickMenu() =>
        _vm != null && _page != null && _editingText == null && _editingSticky == null
        && BusyOverlay.Visibility != Visibility.Visible;

    /// <summary>Setzt Aktiv-/Sichtbarkeit der Schnellaktionen je nach Auswahl/Zwischenablage.</summary>
    private void PrepareQuickMenu()
    {
        bool hasSel = _selection.Count > 0;
        Qm_Cut.IsEnabled = hasSel;
        Qm_Copy.IsEnabled = hasSel;
        Qm_Duplicate.IsEnabled = hasSel;
        Qm_Delete.IsEnabled = hasSel;
        Qm_Paste.IsEnabled = _clipboard.Count > 0 || ClipboardHasImage();
        Qm_SelectAll.IsEnabled = _page is { Elements.Count: > 0 };

        // OCR nur zeigen, wenn verfügbar; aktiv bei ausgewähltem Bild oder (ohne
        // Auswahl) einer Seite mit importiertem Hintergrund (PDF-Seite).
        bool ocrOk = OcrService.IsAvailable;
        bool ocrSource = _selection.OfType<ImageElement>().Any()
            || (_selection.Count == 0 && _page?.BackgroundImage is { Length: > 0 });
        Qm_Ocr.Visibility = ocrOk ? Visibility.Visible : Visibility.Collapsed;
        Qm_SepOcr.Visibility = ocrOk ? Visibility.Visible : Visibility.Collapsed;
        Qm_Ocr.IsEnabled = ocrOk && ocrSource;
    }

    /// <summary>Positioniert die Leiste (in Leinwand-Koordinaten) und hält sie im sichtbaren Bereich.</summary>
    private void PlaceQuickMenu(double left, double top)
    {
        double maxLeft = Math.Max(0, CanvasHost.ActualWidth - QuickMenu.ActualWidth);
        double maxTop = Math.Max(0, CanvasHost.ActualHeight - QuickMenu.ActualHeight);
        left = Math.Clamp(left, 0, maxLeft);
        top = Math.Clamp(top, 0, maxTop);
        QuickMenu.Margin = new Thickness(left, top, 0, 0);
        _quickShownTick = Environment.TickCount;
    }

    private void HideQuickMenu()
    {
        if (QuickMenu.Visibility != Visibility.Collapsed) QuickMenu.Visibility = Visibility.Collapsed;
    }

    /// <summary>Stammt das Ereignis von der Schnellaktions-Leiste (dann nicht als Zeichnen behandeln)?</summary>
    private bool IsOnQuickMenu(object? src)
    {
        if (QuickMenu.Visibility != Visibility.Visible) return false;
        var d = src as DependencyObject;
        while (d != null)
        {
            if (ReferenceEquals(d, QuickMenu)) return true;
            d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }
        return false;
    }

    private static bool ClipboardHasImage()
    {
        try { return System.Windows.Clipboard.ContainsImage(); }
        catch { return false; }
    }

    private void Cm_Cut_Click(object s, RoutedEventArgs e) { HideQuickMenu(); CutSelection(); }
    private void Cm_Copy_Click(object s, RoutedEventArgs e) { HideQuickMenu(); CopySelection(); }
    private void Cm_Duplicate_Click(object s, RoutedEventArgs e) { HideQuickMenu(); DuplicateSelection(); }
    private void Cm_Delete_Click(object s, RoutedEventArgs e) { HideQuickMenu(); DeleteSelection(); }
    private void Cm_SelectAll_Click(object s, RoutedEventArgs e) { HideQuickMenu(); SelectAll(); }

    private void Cm_Paste_Click(object s, RoutedEventArgs e)
    {
        HideQuickMenu();
        if (_clipboard.Count > 0) PasteClipboard(_contextCanvasPos);
        else PasteImageFromClipboard();
    }

    private static WbElement CloneElement(WbElement el) => el switch
    {
        StrokeElement s => new StrokeElement
        {
            Points = s.Points.Select(p => new WbPoint(p.X, p.Y, p.P)).ToList(),
            Color = s.Color, Width = s.Width, Kind = s.Kind,
        },
        ShapeElement sh => new ShapeElement
        {
            Shape = sh.Shape, X1 = sh.X1, Y1 = sh.Y1, X2 = sh.X2, Y2 = sh.Y2,
            Color = sh.Color, StrokeWidth = sh.StrokeWidth, Fill = sh.Fill,
        },
        TextElement t => new TextElement
        {
            X = t.X, Y = t.Y, Text = t.Text, Color = t.Color, FontSize = t.FontSize,
            Background = t.Background, FontFamily = t.FontFamily,
        },
        // Data wird bewusst geteilt (unveränderlich nach Import) – spart RAM und DB-Größe
        ImageElement im => new ImageElement
        {
            X = im.X, Y = im.Y, Width = im.Width, Height = im.Height, Data = im.Data,
        },
        StickyNoteElement sn => new StickyNoteElement
        {
            X = sn.X, Y = sn.Y, Width = sn.Width, Height = sn.Height, Text = sn.Text,
            Color = sn.Color, TextColor = sn.TextColor, FontSize = sn.FontSize, FontFamily = sn.FontFamily,
        },
        _ => throw new NotSupportedException(),
    };
}
