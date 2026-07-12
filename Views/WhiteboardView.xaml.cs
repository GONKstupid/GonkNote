using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.Models;
using GonkNote.Services;
using GonkNote.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace GonkNote.Views;

/// <summary>
/// SkiaSharp-Zeichenfläche für Whiteboards und Notizbücher.
/// Stylus-Eingabe mit Druckstärke, Maus als Fallback, Finger = Verschieben.
/// </summary>
public partial class WhiteboardView : UserControl
{
    private WhiteboardTabViewModel? _vm;
    private WbPage? _page;

    // Werkzeugzustand
    private ToolType _tool = ToolType.Pen;
    private ShapeKind _shape = ShapeKind.Rectangle;
    private string _colorTag = "auto";
    private float _width = 2.5f;
    private bool _suppressToolEvents;

    // Formfüllung
    private bool _shapeFillOn;
    private string _shapeFillRgb = "#14B8A6";
    private float _shapeFillOpacity = 0.4f;

    // Eingabezustand
    private bool _drawing;
    private List<WbPoint>? _activePoints;
    private SKPoint _shapeStart, _shapeCur;
    private bool _shapeActive;
    private bool _panning;
    private Point _panLast;
    private bool _spaceDown;
    private bool _stylusInverted;

    // Radierer (punktgenau: Striche werden aufgetrennt statt komplett gelöscht)
    private List<EraseStep>? _eraseSteps;
    private SKPoint _eraserPos;
    private bool _eraserVisible;

    // Lasso / Auswahl
    private List<SKPoint>? _lassoPts;
    private readonly HashSet<WbElement> _selection = new();
    private SKRect _selectionBounds;
    private bool _movingSelection;
    private SKPoint _moveLast;
    private float _movedX, _movedY;

    // Skalierung über den Eckgriff bei Einzelauswahl eines Box-Elements (Bild/Notizzettel)
    private IBoxElement? _resizingBox;
    private float _resizeW0, _resizeH0;

    // Texteingabe (Textfeld)
    private TextElement? _editingText;
    private bool _editingIsNew;
    private string _editingOldText = "";
    private bool _cancelEdit;

    // Notizzettel-Bearbeitung
    private StickyNoteElement? _editingSticky;
    private bool _editingStickyIsNew;
    private string _editingStickyOld = "";
    private string _stickyColorHex = "#FFFEF08A";

    // Lineal (transientes Zeichen-Hilfsmittel, wird nicht gespeichert)
    private enum RulerDrag { None, Move, Rotate }
    private bool _rulerOn;
    private bool _rulerPlaced;
    private SKPoint _rulerCenter;
    private float _rulerAngleDeg;
    private RulerDrag _rulerDrag;
    private SKPoint _rulerDragLast;
    // Einrasten eines Strichs auf eine Lineal-Kante
    private bool _rulerSnapActive;
    private SKPoint _rulerSnapE0, _rulerSnapDir;

    // Maße in Canvas-Einheiten (96 DPI): 1 cm ≈ 37,8 px
    private const float RulerLength = 680f;
    private const float RulerHalfWidth = 26f;
    private const float RulerSnapDist = 26f;
    private const float PxPerCm = 37.795f;

    private ToggleButton[] ToolButtons => new[] { BtnPen, BtnSmoothPen, BtnPencil, BtnHighlighter, BtnEraser, BtnLasso, BtnText, BtnShape, BtnSticky, BtnPan };
    private ToggleButton[] ShapeButtons => new[] { BtnShapeLine, BtnShapeArrow, BtnShapeRect, BtnShapeEllipse, BtnShapeTriangle };
    private ToggleButton[] PenButtons => new[] { BtnPen, BtnSmoothPen, BtnPencil, BtnHighlighter };

    // Stifte-Gruppe: eingeklappt ist nur der zuletzt benutzte Stift sichtbar
    private ToolType _lastPen = ToolType.Pen;
    private bool _penGroupExpanded;

    public WhiteboardView()
    {
        InitializeComponent();

        _suppressToolEvents = true;
        BtnPen.IsChecked = true;
        BtnShapeRect.IsChecked = true;
        _suppressToolEvents = false;
        SetPenGroupExpanded(false);

        foreach (var b in ToolButtons) b.Unchecked += Tool_Unchecked;

        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => { ThemeService.ThemeChanged += OnThemeChanged; Skia.InvalidateVisual(); };
        Unloaded += (_, _) => ThemeService.ThemeChanged -= OnThemeChanged;

        // Eingabe
        CanvasHost.MouseDown += OnMouseDown;
        CanvasHost.MouseMove += OnMouseMove;
        CanvasHost.MouseUp += OnMouseUp;
        CanvasHost.MouseWheel += OnMouseWheel;
        CanvasHost.MouseLeave += (_, _) => { _eraserVisible = false; Skia.InvalidateVisual(); };
        CanvasHost.StylusDown += OnStylusDown;
        CanvasHost.StylusMove += OnStylusMove;
        CanvasHost.StylusUp += OnStylusUp;
        CanvasHost.TouchDown += OnTouchDown;
        CanvasHost.TouchMove += OnTouchMove;
        CanvasHost.TouchUp += OnTouchUp;

        PreviewKeyDown += OnPreviewKeyDown;
        PreviewKeyUp += OnPreviewKeyUp;
    }

    private void OnThemeChanged() => Skia.InvalidateVisual();

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.Undo.Changed -= OnUndoChanged;

        _vm = DataContext as WhiteboardTabViewModel;
        if (_vm == null) return;

        _vm.Undo.Changed += OnUndoChanged;
        _vm.PageIndex = Math.Clamp(_vm.PageIndex, 0, _vm.Doc.Pages.Count - 1);
        _page = _vm.Doc.Pages[_vm.PageIndex];

        bool paged = !_page.IsInfinite;
        PageBar.Visibility = paged ? Visibility.Visible : Visibility.Collapsed;
        UpdatePageLabel();
        OnUndoChanged();
        UpdateZoomLabel();
        if (SettingsPanel.Visibility == Visibility.Visible) RefreshSettingsPanel();
        Skia.InvalidateVisual();
    }

    private void OnUndoChanged()
    {
        if (_vm == null) return;
        BtnUndo.IsEnabled = _vm.Undo.CanUndo;
        BtnRedo.IsEnabled = _vm.Undo.CanRedo;
    }

    private void MarkDirty()
    {
        if (_vm != null) _vm.IsDirty = true;
    }

    // ==================== Werkzeuge / Toolbar ====================

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressToolEvents) return;
        var btn = (ToggleButton)sender;

        _suppressToolEvents = true;
        foreach (var b in ToolButtons)
            if (b != btn) b.IsChecked = false;
        _suppressToolEvents = false;

        SetTool(Enum.Parse<ToolType>((string)btn.Tag));

        // Ein Klick genügt: Stift auswählen klappt die Stifte-Gruppe aus,
        // ein anderes Werkzeug klappt sie wieder ein
        if (IsPenTool(_tool)) _lastPen = _tool;
        SetPenGroupExpanded(IsPenTool(_tool));
    }

    private void Tool_Unchecked(object? sender, RoutedEventArgs e)
    {
        if (_suppressToolEvents) return;
        // Aktives Werkzeug lässt sich nicht abwählen
        var btn = (ToggleButton)sender!;
        if (ToolButtons.All(b => b.IsChecked != true))
        {
            _suppressToolEvents = true;
            btn.IsChecked = true;
            _suppressToolEvents = false;
        }
    }

    private static bool IsPenTool(ToolType t) =>
        t is ToolType.Pen or ToolType.SmoothPen or ToolType.Pencil or ToolType.Highlighter;

    private ToggleButton PenButtonFor(ToolType t) => t switch
    {
        ToolType.SmoothPen => BtnSmoothPen,
        ToolType.Pencil => BtnPencil,
        ToolType.Highlighter => BtnHighlighter,
        _ => BtnPen,
    };

    private void SetPenGroupExpanded(bool expanded)
    {
        _penGroupExpanded = expanded;
        var rep = PenButtonFor(_lastPen);
        foreach (var b in PenButtons)
            b.Visibility = expanded || b == rep ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetTool(ToolType tool)
    {
        CommitActiveEdit();
        if (tool != ToolType.Lasso) ClearSelection();

        _tool = tool;
        _eraserVisible = false;

        // Formen-/Notizzettel-Werkzeug: Einstellungs-Panel mit passender Sektion öffnen
        if (tool == ToolType.Shape && SettingsPanel.Visibility != Visibility.Visible)
        {
            RefreshSettingsPanel();
            ShapeSection.IsExpanded = true;
            SettingsPanel.Visibility = Visibility.Visible;
        }
        else if (tool == ToolType.Sticky && SettingsPanel.Visibility != Visibility.Visible)
        {
            RefreshSettingsPanel();
            StickySection.IsExpanded = true;
            SettingsPanel.Visibility = Visibility.Visible;
        }
        else if (SettingsPanel.Visibility == Visibility.Visible)
        {
            // Sichtbarkeit der werkzeugspezifischen Sektionen ans neue Werkzeug anpassen
            RefreshSettingsPanel();
        }

        CanvasHost.Cursor = tool switch
        {
            ToolType.Pen or ToolType.SmoothPen or ToolType.Pencil or ToolType.Highlighter => Cursors.Pen,
            ToolType.Eraser => Cursors.None,
            ToolType.Text => Cursors.IBeam,
            ToolType.Shape => Cursors.Cross,
            ToolType.Sticky => Cursors.Cross,
            ToolType.Pan => Cursors.Hand,
            _ => Cursors.Arrow,
        };
        Skia.InvalidateVisual();
    }

    private void Shape_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressToolEvents) return;
        var btn = (ToggleButton)sender;

        _suppressToolEvents = true;
        foreach (var b in ShapeButtons)
            if (b != btn) b.IsChecked = false;
        btn.IsChecked = true;
        _suppressToolEvents = false;

        _shape = Enum.Parse<ShapeKind>((string)btn.Tag);
    }

    private void Color_Checked(object sender, RoutedEventArgs e)
    {
        // Tag kann während des XAML-Ladens noch fehlen → Standard behalten
        if (((RadioButton)sender).Tag is string tag && tag.Length > 0)
            _colorTag = tag;
    }

    /// <summary>Aktuelle Füllfarbe inkl. Deckkraft oder null, wenn Füllung aus ist.</summary>
    private string? CurrentFill()
    {
        if (!_shapeFillOn) return null;
        var c = ParseColor(_shapeFillRgb);
        byte a = (byte)Math.Round(_shapeFillOpacity * 255);
        return $"#{a:X2}{c.Red:X2}{c.Green:X2}{c.Blue:X2}";
    }

    private void UpdateFillPreview()
    {
        var c = ParseColor(_shapeFillRgb);
        byte a = (byte)Math.Round(_shapeFillOpacity * 255);
        FillPreviewRect.Fill = new SolidColorBrush(Color.FromArgb(a, c.Red, c.Green, c.Blue));
    }

    private void ShapeFill_Changed(object sender, RoutedEventArgs e)
    {
        _shapeFillOn = BtnShapeFill.IsChecked == true;
    }

    private void PickFillColor_Click(object sender, RoutedEventArgs e)
    {
        var cur = ParseColor(_shapeFillRgb);
        var initial = Color.FromRgb(cur.Red, cur.Green, cur.Blue);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial, allowAlpha: false) is not { } c) return;

        _shapeFillRgb = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        _shapeFillOn = true;
        BtnShapeFill.IsChecked = true;
        UpdateFillPreview();
    }

    private void FillOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _shapeFillOpacity = (float)(e.NewValue / 100.0);
        if (FillOpacityLabel != null) FillOpacityLabel.Text = $"{e.NewValue:0} %";
        if (FillPreviewRect != null) UpdateFillPreview();
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        // Startwert: aktuelle Farbe
        var cur = ParseColor(CurrentInkHex());
        var initial = Color.FromArgb(cur.Alpha, cur.Red, cur.Green, cur.Blue);

        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial) is not { } c) return;

        string hex = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        CustomSwatch.Background = new SolidColorBrush(c);
        CustomSwatch.Tag = hex;
        CustomSwatch.Visibility = Visibility.Visible;
        _colorTag = hex;
        CustomSwatch.IsChecked = true;
    }

    private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _width = (float)e.NewValue;
        if (WidthLabel != null) WidthLabel.Text = _width.ToString("0.#");
    }

    private void Undo_Click(object sender, RoutedEventArgs e) => DoUndo();
    private void Redo_Click(object sender, RoutedEventArgs e) => DoRedo();

    private void DoUndo()
    {
        if (_vm == null) return;
        CommitActiveEdit();
        ClearSelection();
        var page = _vm.Undo.Undo();
        if (page != null) { NavigateToPage(page); MarkDirty(); Skia.InvalidateVisual(); }
    }

    private void DoRedo()
    {
        if (_vm == null) return;
        CommitActiveEdit();
        ClearSelection();
        var page = _vm.Undo.Redo();
        if (page != null) { NavigateToPage(page); MarkDirty(); Skia.InvalidateVisual(); }
    }

    private void NavigateToPage(WbPage page)
    {
        if (_vm == null || page == _page) return;
        int idx = _vm.Doc.Pages.IndexOf(page);
        if (idx < 0) return;
        _vm.PageIndex = idx;
        _page = page;
        UpdatePageLabel();
    }

    // ==================== Zoom / Pan ====================

    private float Zoom { get => _vm?.Zoom ?? 1f; set { if (_vm != null) _vm.Zoom = value; } }
    private float PanX { get => _vm?.PanX ?? 0f; set { if (_vm != null) _vm.PanX = value; } }
    private float PanY { get => _vm?.PanY ?? 0f; set { if (_vm != null) _vm.PanY = value; } }

    private void UpdateZoomLabel() => ZoomLabel.Content = $"{Zoom * 100:0} %";

    private void ZoomAt(Point screenCenter, float factor)
    {
        float newZoom = Math.Clamp(Zoom * factor, 0.15f, 8f);
        factor = newZoom / Zoom;
        PanX = (float)(screenCenter.X - (screenCenter.X - PanX) * factor);
        PanY = (float)(screenCenter.Y - (screenCenter.Y - PanY) * factor);
        Zoom = newZoom;
        UpdateZoomLabel();
        Skia.InvalidateVisual();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) =>
        ZoomAt(new Point(CanvasHost.ActualWidth / 2, CanvasHost.ActualHeight / 2), 1.25f);

    private void ZoomOut_Click(object sender, RoutedEventArgs e) =>
        ZoomAt(new Point(CanvasHost.ActualWidth / 2, CanvasHost.ActualHeight / 2), 0.8f);

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        Zoom = 1f;
        CenterView();
        UpdateZoomLabel();
        Skia.InvalidateVisual();
    }

    private void CenterView()
    {
        if (_page == null) return;
        if (_page.IsInfinite)
        {
            PanX = 32; PanY = 32;
        }
        else
        {
            PanX = Math.Max(24f, (float)(CanvasHost.ActualWidth - _page.Width * Zoom) / 2f);
            PanY = 24;
        }
    }

    private SKPoint ToCanvas(Point p) => new((float)((p.X - PanX) / Zoom), (float)((p.Y - PanY) / Zoom));

    private Point ToScreen(SKPoint c) => new(c.X * Zoom + PanX, c.Y * Zoom + PanY);

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ZoomAt(e.GetPosition(CanvasHost), (float)Math.Pow(1.1, e.Delta / 120.0));
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            PanX += e.Delta * 0.5f;
            Skia.InvalidateVisual();
        }
        else
        {
            PanY += e.Delta * 0.5f;
            Skia.InvalidateVisual();
        }
        e.Handled = true;
    }

    // ==================== Eingabe (Stylus + Maus) ====================

    private void OnStylusDown(object sender, StylusDownEventArgs e)
    {
        Focus();
        if (_vm == null || _page == null) return;

        // Finger laufen über die Touch-Events (Gesten: Pan, Pinch-Zoom, Tipp-Gesten)
        if (e.StylusDevice.TabletDevice?.Type == TabletDeviceType.Touch) return;

        _stylusInverted = e.Inverted;
        var pts = e.GetStylusPoints(CanvasHost);
        if (pts.Count == 0) return;
        var p = pts[^1];
        BeginInput(ToCanvas(new Point(p.X, p.Y)), p.PressureFactor);
        CanvasHost.CaptureStylus();
        e.Handled = true;
    }

    private void OnStylusMove(object sender, StylusEventArgs e)
    {
        if (e.StylusDevice?.TabletDevice?.Type == TabletDeviceType.Touch) return;
        if (_panning)
        {
            MovePan(e.GetPosition(CanvasHost));
            e.Handled = true;
            return;
        }
        if (!_drawing && _eraseSteps == null && _lassoPts == null && !_movingSelection && !_shapeActive && _resizingBox == null && _rulerDrag == RulerDrag.None)
        {
            HoverInput(ToCanvas(e.GetPosition(CanvasHost)));
            return;
        }

        foreach (StylusPoint p in e.GetStylusPoints(CanvasHost))
            MoveInput(ToCanvas(new Point(p.X, p.Y)), p.PressureFactor);
        e.Handled = true;
    }

    private void OnStylusUp(object sender, StylusEventArgs e)
    {
        if (e.StylusDevice?.TabletDevice?.Type == TabletDeviceType.Touch) return;
        CanvasHost.ReleaseStylusCapture();
        if (_panning) { EndPan(); e.Handled = true; return; }
        EndInput(ToCanvas(e.GetPosition(CanvasHost)));
        _stylusInverted = false;
        e.Handled = true;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        if (e.StylusDevice != null) return; // bereits als Stylus behandelt
        if (_vm == null || _page == null) return;

        var screen = e.GetPosition(CanvasHost);
        if (e.ChangedButton == MouseButton.Middle || _spaceDown || _tool == ToolType.Pan)
        {
            BeginPan(screen);
            CanvasHost.CaptureMouse();
            return;
        }
        if (e.ChangedButton != MouseButton.Left) return;

        BeginInput(ToCanvas(screen), 0.5f);
        CanvasHost.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.StylusDevice != null) return;

        var screen = e.GetPosition(CanvasHost);
        if (_panning) { MovePan(screen); return; }

        if (e.LeftButton == MouseButtonState.Pressed &&
            (_drawing || _eraseSteps != null || _lassoPts != null || _movingSelection || _shapeActive || _resizingBox != null || _rulerDrag != RulerDrag.None))
        {
            MoveInput(ToCanvas(screen), 0.5f);
        }
        else
        {
            HoverInput(ToCanvas(screen));
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice != null) return;
        CanvasHost.ReleaseMouseCapture();
        if (_panning) { EndPan(); return; }
        EndInput(ToCanvas(e.GetPosition(CanvasHost)));
    }

    // ==================== Touch-Gesten ====================
    // 1 Finger = verschieben, 2 Finger = Pinch-Zoom + verschieben,
    // Drei-Finger-Doppeltipp = Rückgängig

    private readonly Dictionary<int, Point> _touches = new();
    private Point _gestureMid;
    private double _gestureDist;
    private int _gestureMaxFingers;
    private DateTime _gestureStart;
    private bool _gestureMoved;
    private DateTime _lastTripleTap = DateTime.MinValue;

    private void OnTouchDown(object? sender, TouchEventArgs e)
    {
        CanvasHost.CaptureTouch(e.TouchDevice);
        var p = e.GetTouchPoint(CanvasHost).Position;
        _touches[e.TouchDevice.Id] = p;

        if (_touches.Count == 1)
        {
            _gestureStart = DateTime.UtcNow;
            _gestureMoved = false;
            _gestureMaxFingers = 1;
        }
        _gestureMaxFingers = Math.Max(_gestureMaxFingers, _touches.Count);

        if (_touches.Count >= 2) InitPinch();
        e.Handled = true;
    }

    private void InitPinch()
    {
        var pts = _touches.Values.Take(2).ToList();
        _gestureMid = new Point((pts[0].X + pts[1].X) / 2, (pts[0].Y + pts[1].Y) / 2);
        _gestureDist = Math.Max(8, (pts[0] - pts[1]).Length);
    }

    private void OnTouchMove(object? sender, TouchEventArgs e)
    {
        if (!_touches.TryGetValue(e.TouchDevice.Id, out var old)) return;
        var p = e.GetTouchPoint(CanvasHost).Position;
        if ((p - old).Length > 6) _gestureMoved = true;
        _touches[e.TouchDevice.Id] = p;

        if (_touches.Count == 1)
        {
            PanX += (float)(p.X - old.X);
            PanY += (float)(p.Y - old.Y);
            Skia.InvalidateVisual();
        }
        else if (_touches.Count >= 2)
        {
            var pts = _touches.Values.Take(2).ToList();
            var mid = new Point((pts[0].X + pts[1].X) / 2, (pts[0].Y + pts[1].Y) / 2);
            double dist = Math.Max(8, (pts[0] - pts[1]).Length);

            ZoomAt(mid, (float)(dist / _gestureDist));
            PanX += (float)(mid.X - _gestureMid.X);
            PanY += (float)(mid.Y - _gestureMid.Y);
            _gestureMid = mid;
            _gestureDist = dist;
            Skia.InvalidateVisual();
        }
        e.Handled = true;
    }

    private void OnTouchUp(object? sender, TouchEventArgs e)
    {
        CanvasHost.ReleaseTouchCapture(e.TouchDevice);
        _touches.Remove(e.TouchDevice.Id);

        if (_touches.Count >= 2) InitPinch();

        if (_touches.Count == 0)
        {
            bool tap = !_gestureMoved && (DateTime.UtcNow - _gestureStart).TotalMilliseconds < 400;
            if (tap && _gestureMaxFingers == 3)
            {
                if ((DateTime.UtcNow - _lastTripleTap).TotalMilliseconds < 600)
                {
                    DoUndo();
                    _lastTripleTap = DateTime.MinValue;
                }
                else
                {
                    _lastTripleTap = DateTime.UtcNow;
                }
            }
            _gestureMaxFingers = 0;
        }
        e.Handled = true;
    }

    private void BeginPan(Point screen)
    {
        _panning = true;
        _panLast = screen;
    }

    private void MovePan(Point screen)
    {
        if (!_panning) return;
        PanX += (float)(screen.X - _panLast.X);
        PanY += (float)(screen.Y - _panLast.Y);
        _panLast = screen;
        Skia.InvalidateVisual();
    }

    private void EndPan() => _panning = false;

    private ToolType EffectiveTool => _stylusInverted ? ToolType.Eraser : _tool;

    private void BeginInput(SKPoint c, float pressure)
    {
        if (_page == null || _vm == null) return;
        CommitActiveEdit();

        // Lineal bewegen/drehen hat Vorrang (außer beim Verschieben-Werkzeug)
        if (_tool != ToolType.Pan && TryBeginRuler(c)) return;

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.SmoothPen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                _drawing = true;
                TryActivateRulerSnap(c);
                var start = ApplyRulerSnap(c);
                _activePoints = new List<WbPoint> { new(start.X, start.Y, Math.Clamp(pressure, 0.05f, 1f)) };
                break;

            case ToolType.Eraser:
                _eraseSteps = new List<EraseStep>();
                _eraserPos = c;
                _eraserVisible = true;
                EraseAt(c);
                break;

            case ToolType.Lasso:
                if (_selection.Count == 1 && _selection.First() is IBoxElement rb && HitResizeHandle(rb, c))
                {
                    _resizingBox = rb;
                    _resizeW0 = rb.Width;
                    _resizeH0 = rb.Height;
                }
                else if (_selection.Count > 0 && InflatedSelectionBounds().Contains(c))
                {
                    _movingSelection = true;
                    _moveLast = c;
                    _movedX = _movedY = 0;
                }
                else
                {
                    ClearSelection();
                    _lassoPts = new List<SKPoint> { c };
                }
                break;

            case ToolType.Text:
                var hit = _page.Elements.OfType<TextElement>()
                    .LastOrDefault(t => TextBounds(t).Contains(c));
                if (hit != null) StartTextEdit(hit, isNew: false);
                else StartTextEdit(new TextElement
                {
                    X = c.X, Y = c.Y,
                    Color = EnsureReadableTextColor(CurrentInkHex(), _textBgHex),
                    FontSize = 18f,
                    Background = _textBgHex,
                    FontFamily = _textFont,
                }, isNew: true);
                break;

            case ToolType.Shape:
                _shapeActive = true;
                _shapeStart = _shapeCur = c;
                break;

            case ToolType.Sticky:
                var hitNote = _page.Elements.OfType<StickyNoteElement>()
                    .LastOrDefault(s => SKRect.Create(s.X, s.Y, s.Width, s.Height).Contains(c));
                if (hitNote != null)
                {
                    StartStickyEdit(hitNote, isNew: false);
                }
                else
                {
                    // Neuen Zettel mittig unter dem Zeiger anlegen und gleich beschriften
                    var note = new StickyNoteElement
                    {
                        X = c.X - 100f, Y = c.Y - 100f,
                        Color = _stickyColorHex,
                        TextColor = ReadableStickyTextColor(_stickyColorHex),
                    };
                    StartStickyEdit(note, isNew: true);
                }
                break;

            case ToolType.Pan:
                // wird über BeginPan behandelt
                break;
        }
        Skia.InvalidateVisual();
    }

    private void MoveInput(SKPoint c, float pressure)
    {
        if (_page == null) return;
        if (_rulerDrag != RulerDrag.None) { UpdateRulerDrag(c); return; }

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.SmoothPen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                if (!_drawing || _activePoints == null) return;
                c = ApplyRulerSnap(c);
                var last = _activePoints[^1];
                float minDist = 1.2f / Zoom;
                if ((c.X - last.X) * (c.X - last.X) + (c.Y - last.Y) * (c.Y - last.Y) < minDist * minDist)
                    return;
                // leichte Glättung der Eingabe
                float sx = 0.35f * last.X + 0.65f * c.X;
                float sy = 0.35f * last.Y + 0.65f * c.Y;
                float sp = 0.4f * last.P + 0.6f * Math.Clamp(pressure, 0.05f, 1f);
                _activePoints.Add(new WbPoint(sx, sy, sp));
                break;

            case ToolType.Eraser:
                if (_eraseSteps == null) return;
                _eraserPos = c;
                _eraserVisible = true;
                EraseAt(c);
                break;

            case ToolType.Lasso:
                if (_resizingBox is ImageElement rImg)
                {
                    // Bilder proportional über den Eckgriff unten rechts, Ankerpunkt oben links
                    float scale = Math.Max(
                        (c.X - rImg.X) / Math.Max(1f, _resizeW0),
                        (c.Y - rImg.Y) / Math.Max(1f, _resizeH0));
                    scale = Math.Max(scale, 16f / Math.Max(_resizeW0, _resizeH0));
                    rImg.Width = _resizeW0 * scale;
                    rImg.Height = _resizeH0 * scale;
                    ComputeSelectionBounds();
                }
                else if (_resizingBox is StickyNoteElement rNote)
                {
                    // Notizzettel frei skalieren (Text bricht neu um), Mindestgröße 60 px
                    rNote.Width = Math.Max(60f, c.X - rNote.X);
                    rNote.Height = Math.Max(60f, c.Y - rNote.Y);
                    ComputeSelectionBounds();
                }
                else if (_movingSelection)
                {
                    float dx = c.X - _moveLast.X, dy = c.Y - _moveLast.Y;
                    foreach (var el in _selection) el.Translate(dx, dy);
                    _selectionBounds.Offset(dx, dy);
                    _movedX += dx; _movedY += dy;
                    _moveLast = c;
                }
                else
                {
                    _lassoPts?.Add(c);
                }
                break;

            case ToolType.Shape:
                if (!_shapeActive) return;
                _shapeCur = Constrain(_shapeStart, c);
                break;

            default:
                return;
        }
        Skia.InvalidateVisual();
    }

    private void HoverInput(SKPoint c)
    {
        if (EffectiveTool == ToolType.Eraser)
        {
            _eraserPos = c;
            _eraserVisible = true;
            Skia.InvalidateVisual();
        }
    }

    private void EndInput(SKPoint c)
    {
        if (_page == null || _vm == null) return;

        if (_rulerDrag != RulerDrag.None) { _rulerDrag = RulerDrag.None; Skia.InvalidateVisual(); return; }

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.SmoothPen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                if (!_drawing || _activePoints == null) break;
                CommitStroke();
                break;

            case ToolType.Eraser:
                if (_eraseSteps is { Count: > 0 })
                {
                    _vm.Undo.Push(_page, new PartialEraseAction(_eraseSteps));
                    MarkDirty();
                }
                _eraseSteps = null;
                break;

            case ToolType.Lasso:
                if (_resizingBox != null)
                {
                    var box = _resizingBox;
                    _resizingBox = null;
                    if (Math.Abs(box.Width - _resizeW0) > 0.01f || Math.Abs(box.Height - _resizeH0) > 0.01f)
                    {
                        _vm.Undo.Push(_page, new ResizeBoxAction(box, _resizeW0, _resizeH0, box.Width, box.Height));
                        MarkDirty();
                    }
                    ComputeSelectionBounds();
                }
                else if (_movingSelection)
                {
                    _movingSelection = false;
                    if (Math.Abs(_movedX) > 0.01f || Math.Abs(_movedY) > 0.01f)
                    {
                        _vm.Undo.Push(_page, new MoveElementsAction(_selection, _movedX, _movedY));
                        MarkDirty();
                    }
                }
                else if (_lassoPts is { Count: > 2 })
                {
                    SelectByLasso(_lassoPts);
                    _lassoPts = null;
                }
                else
                {
                    _lassoPts = null;
                }
                break;

            case ToolType.Shape:
                if (!_shapeActive) break;
                _shapeActive = false;
                CommitShape();
                break;
        }

        _drawing = false;
        _activePoints = null;
        _rulerSnapActive = false;
        Skia.InvalidateVisual();
    }

    private static SKPoint Constrain(SKPoint start, SKPoint cur)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) return cur;
        float dx = cur.X - start.X, dy = cur.Y - start.Y;
        float m = Math.Max(Math.Abs(dx), Math.Abs(dy));
        return new SKPoint(start.X + Math.Sign(dx) * m, start.Y + Math.Sign(dy) * m);
    }

    private void CommitStroke()
    {
        if (_page == null || _vm == null || _activePoints == null) return;

        // Tippen ohne Ziehen erzeugt einen Punkt
        if (_activePoints.Count == 1)
        {
            var p = _activePoints[0];
            _activePoints.Add(new WbPoint(p.X + 0.1f, p.Y + 0.1f, p.P));
        }

        var kind = _tool switch
        {
            ToolType.Pencil => StrokeKind.Pencil,
            ToolType.Highlighter => StrokeKind.Highlighter,
            _ => StrokeKind.Pen,
        };

        // Formen-Stift: erst versuchen, eine Grundform zu erkennen (wie GoodNotes)
        if (_tool == ToolType.SmoothPen && RecognizeShape(_activePoints) is { } recognized)
        {
            _page.Elements.Add(recognized);
            _vm.Undo.Push(_page, new AddElementsAction(new[] { recognized }));
            MarkDirty();
            return;
        }

        var points = _tool == ToolType.SmoothPen ? SmoothPoints(_activePoints) : _activePoints;

        var stroke = new StrokeElement
        {
            Points = points,
            Color = CurrentInkHex(),
            Width = kind == StrokeKind.Highlighter ? Math.Max(_width * 5f, 10f) : _width,
            Kind = kind,
        };
        _page.Elements.Add(stroke);
        _vm.Undo.Push(_page, new AddElementsAction(new WbElement[] { stroke }));
        MarkDirty();
    }

    private void CommitShape()
    {
        if (_page == null || _vm == null) return;
        float dx = _shapeCur.X - _shapeStart.X, dy = _shapeCur.Y - _shapeStart.Y;
        if (dx * dx + dy * dy < 9f / (Zoom * Zoom)) return; // zu klein

        var shape = new ShapeElement
        {
            Shape = _shape,
            X1 = _shapeStart.X, Y1 = _shapeStart.Y,
            X2 = _shapeCur.X, Y2 = _shapeCur.Y,
            Color = CurrentInkHex(),
            StrokeWidth = _width,
            Fill = CurrentFill(),
        };
        _page.Elements.Add(shape);
        _vm.Undo.Push(_page, new AddElementsAction(new WbElement[] { shape }));
        MarkDirty();
    }

    // ==================== Formen-Stift: Formerkennung ====================

    /// <summary>
    /// Erkennt gezeichnete Grundformen (wie der Formen-Stift in GoodNotes): fast gerade
    /// Züge werden zu perfekten Geraden (mit Winkel-Einrasten auf 45°-Schritte), runde
    /// geschlossene Züge zu Kreisen/Ellipsen, eckige zu Rechtecken bzw. Streckenzügen.
    /// Liefert null, wenn nichts erkannt wurde – dann wird die Kurve geglättet übernommen.
    /// </summary>
    private WbElement? RecognizeShape(List<WbPoint> pts)
    {
        if (pts.Count < 4) return null;

        float len = 0;
        for (int i = 1; i < pts.Count; i++)
            len += Dist(pts[i - 1], pts[i]);
        if (len < 24) return null;

        var a = pts[0];
        var b = pts[^1];
        float chord = Dist(a, b);

        // Gerade Linie: Punkte weichen kaum von der Sehne ab
        if (chord > len * 0.8f && MaxChordDeviation(pts) <= Math.Max(4f, len * 0.05f))
            return SnappedLine(a, b);

        bool closed = chord <= Math.Max(18f, len * 0.16f);
        if (!closed)
        {
            // Offener Zug mit wenigen klaren Ecken → perfekter Streckenzug
            var open = DouglasPeucker(pts, Math.Max(6f, len * 0.03f));
            if (open.Count == 2) return SnappedLine(open[0], open[^1]);
            if (open.Count <= 6) return PolylineStroke(open, closed: false);
            return null;
        }

        // Geschlossener Zug: wenige Ecken → Polygon, sonst Ellipse prüfen
        var poly = ClosedCorners(pts, Math.Max(7f, len * 0.025f));

        if (poly.Count == 4 && TrySnapRectangle(poly) is { } rect) return rect;
        if (poly.Count is 3 or 4) return PolylineStroke(poly, closed: true);

        if (EllipseFitError(pts) <= 0.14f) return SnapEllipse(pts);
        if (poly.Count <= 6) return PolylineStroke(poly, closed: true);
        return null;
    }

    private static float Dist(WbPoint a, WbPoint b)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static float MaxChordDeviation(List<WbPoint> pts)
    {
        var a = new SKPoint(pts[0].X, pts[0].Y);
        var b = new SKPoint(pts[^1].X, pts[^1].Y);
        float max = 0;
        foreach (var p in pts)
            max = Math.Max(max, SegmentDistance(a, b, new SKPoint(p.X, p.Y)));
        return max;
    }

    /// <summary>Perfekte Gerade; rastet nahe 0°/45°/90° exakt ein.</summary>
    private ShapeElement SnappedLine(WbPoint a, WbPoint b)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y;
        float angle = MathF.Atan2(dy, dx);
        const float step = MathF.PI / 4f;
        float snapped = MathF.Round(angle / step) * step;
        if (Math.Abs(snapped - angle) <= 6f * MathF.PI / 180f)
        {
            float l = MathF.Sqrt(dx * dx + dy * dy);
            dx = l * MathF.Cos(snapped);
            dy = l * MathF.Sin(snapped);
        }
        return new ShapeElement
        {
            Shape = ShapeKind.Line,
            X1 = a.X, Y1 = a.Y, X2 = a.X + dx, Y2 = a.Y + dy,
            Color = CurrentInkHex(), StrokeWidth = _width,
        };
    }

    /// <summary>Streckenzug aus den erkannten Eckpunkten – Segmente sind perfekt gerade.</summary>
    private StrokeElement PolylineStroke(List<WbPoint> corners, bool closed)
    {
        var points = corners.Select(p => new WbPoint(p.X, p.Y, 0.5f)).ToList();
        if (closed) points.Add(new WbPoint(corners[0].X, corners[0].Y, 0.5f));
        return new StrokeElement { Points = points, Color = CurrentInkHex(), Width = _width, Kind = StrokeKind.Pen };
    }

    /// <summary>Achsenparalleles Viereck rastet zum perfekten Rechteck ein, sonst null.</summary>
    private ShapeElement? TrySnapRectangle(List<WbPoint> quad)
    {
        const float tol = 15f * MathF.PI / 180f;
        for (int i = 0; i < 4; i++)
        {
            var p = quad[i];
            var q = quad[(i + 1) % 4];
            float ang = MathF.Atan2(Math.Abs(q.Y - p.Y), Math.Abs(q.X - p.X));
            if (ang > tol && ang < MathF.PI / 2f - tol) return null;
        }
        return new ShapeElement
        {
            Shape = ShapeKind.Rectangle,
            X1 = quad.Min(p => p.X), Y1 = quad.Min(p => p.Y),
            X2 = quad.Max(p => p.X), Y2 = quad.Max(p => p.Y),
            Color = CurrentInkHex(), StrokeWidth = _width,
        };
    }

    /// <summary>Mittlere radiale Abweichung von der einbeschriebenen Ellipse (0 = perfekt).</summary>
    private static float EllipseFitError(List<WbPoint> pts)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in pts)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
        }
        float rx = Math.Max(1f, (maxX - minX) / 2f), ry = Math.Max(1f, (maxY - minY) / 2f);
        float cx = (minX + maxX) / 2f, cy = (minY + maxY) / 2f;

        float err = 0;
        foreach (var p in pts)
        {
            float nx = (p.X - cx) / rx, ny = (p.Y - cy) / ry;
            err += Math.Abs(MathF.Sqrt(nx * nx + ny * ny) - 1f);
        }
        return err / pts.Count;
    }

    /// <summary>Perfekte Ellipse über der Bounding-Box; fast runde werden zum Kreis.</summary>
    private ShapeElement SnapEllipse(List<WbPoint> pts)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in pts)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
        }
        float w = maxX - minX, h = maxY - minY;
        if (Math.Abs(w - h) <= 0.2f * Math.Max(w, h))
        {
            float cx = (minX + maxX) / 2f, cy = (minY + maxY) / 2f;
            float r = (w + h) / 4f;
            minX = cx - r; maxX = cx + r; minY = cy - r; maxY = cy + r;
        }
        return new ShapeElement
        {
            Shape = ShapeKind.Ellipse,
            X1 = minX, Y1 = minY, X2 = maxX, Y2 = maxY,
            Color = CurrentInkHex(), StrokeWidth = _width,
        };
    }

    /// <summary>Ecken eines geschlossenen Zugs: Start an der centroid-fernsten Stelle, dann Douglas-Peucker.</summary>
    private static List<WbPoint> ClosedCorners(List<WbPoint> pts, float epsilon)
    {
        float cx = 0, cy = 0;
        foreach (var p in pts) { cx += p.X; cy += p.Y; }
        cx /= pts.Count; cy /= pts.Count;

        int start = 0; float best = -1;
        for (int i = 0; i < pts.Count; i++)
        {
            float dx = pts[i].X - cx, dy = pts[i].Y - cy;
            float d = dx * dx + dy * dy;
            if (d > best) { best = d; start = i; }
        }

        var rotated = new List<WbPoint>(pts.Count + 1);
        for (int i = 0; i < pts.Count; i++) rotated.Add(pts[(start + i) % pts.Count]);
        rotated.Add(pts[start]);

        var corners = DouglasPeucker(rotated, epsilon);
        corners.RemoveAt(corners.Count - 1); // Ende == Anfang
        return corners;
    }

    private static List<WbPoint> DouglasPeucker(List<WbPoint> pts, float epsilon)
    {
        if (pts.Count < 3) return new List<WbPoint>(pts);
        var keep = new bool[pts.Count];
        keep[0] = keep[^1] = true;
        DpRecurse(pts, 0, pts.Count - 1, epsilon, keep);

        var result = new List<WbPoint>();
        for (int i = 0; i < pts.Count; i++)
            if (keep[i]) result.Add(pts[i]);
        return result;
    }

    private static void DpRecurse(List<WbPoint> pts, int lo, int hi, float eps, bool[] keep)
    {
        if (hi <= lo + 1) return;
        var a = new SKPoint(pts[lo].X, pts[lo].Y);
        var b = new SKPoint(pts[hi].X, pts[hi].Y);
        float maxD = -1; int maxI = -1;
        for (int i = lo + 1; i < hi; i++)
        {
            float d = SegmentDistance(a, b, new SKPoint(pts[i].X, pts[i].Y));
            if (d > maxD) { maxD = d; maxI = i; }
        }
        if (maxD <= eps) return;
        keep[maxI] = true;
        DpRecurse(pts, lo, maxI, eps, keep);
        DpRecurse(pts, maxI, hi, eps, keep);
    }

    /// <summary>Glättstift: Resampling auf gleichmäßige Abstände + mehrfacher gleitender Mittelwert.</summary>
    private static List<WbPoint> SmoothPoints(List<WbPoint> pts)
    {
        if (pts.Count < 3) return pts;

        var resampled = Resample(pts, 3f);
        for (int pass = 0; pass < 3 && resampled.Count >= 3; pass++)
        {
            var sm = new List<WbPoint>(resampled.Count) { resampled[0] };
            for (int i = 1; i < resampled.Count - 1; i++)
            {
                var a = resampled[i - 1];
                var b = resampled[i];
                var c = resampled[i + 1];
                sm.Add(new WbPoint(
                    (a.X + 2 * b.X + c.X) / 4f,
                    (a.Y + 2 * b.Y + c.Y) / 4f,
                    (a.P + 2 * b.P + c.P) / 4f));
            }
            sm.Add(resampled[^1]);
            resampled = sm;
        }
        return resampled;
    }

    private static List<WbPoint> Resample(List<WbPoint> pts, float spacing)
    {
        var result = new List<WbPoint> { pts[0] };
        float carried = 0;
        for (int i = 1; i < pts.Count; i++)
        {
            var a = pts[i - 1];
            var b = pts[i];
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float segLen = MathF.Sqrt(dx * dx + dy * dy);
            if (segLen < 1e-5f) continue;

            float pos = spacing - carried;
            while (pos <= segLen)
            {
                float t = pos / segLen;
                result.Add(new WbPoint(a.X + t * dx, a.Y + t * dy, a.P + t * (b.P - a.P)));
                pos += spacing;
            }
            carried = segLen - (pos - spacing);
        }
        if (result.Count < 2 ||
            Math.Abs(result[^1].X - pts[^1].X) > 0.01f || Math.Abs(result[^1].Y - pts[^1].Y) > 0.01f)
            result.Add(new WbPoint(pts[^1].X, pts[^1].Y, pts[^1].P));
        return result;
    }

    /// <summary>Effektiver Farbton der Seite (Auto folgt dem App-Theme).</summary>
    private static PageShade EffectiveShade(WbPage? page)
    {
        if (page != null && page.Shade != PageShade.Auto) return page.Shade;
        return ThemeService.Current == AppTheme.Dark ? PageShade.Dark : PageShade.Light;
    }

    private string CurrentInkHex()
    {
        if (!string.IsNullOrEmpty(_colorTag) && _colorTag != "auto") return _colorTag;
        // Standardtinte: Schwarz; auf dunklen Seiten helle Tinte
        return EffectiveShade(_page) == PageShade.Dark ? "#FFE6ECF7" : "#FF000000";
    }

    // ==================== Einstellungen (rechte Seitenleiste) ====================

    private bool _suppressSettingsEvents;

    private static readonly string[] CoverFonts =
    {
        "Segoe UI", "Segoe Print", "Segoe Script", "Arial", "Calibri", "Cambria",
        "Comic Sans MS", "Consolas", "Georgia", "Impact", "Palatino Linotype",
        "Times New Roman", "Trebuchet MS", "Verdana",
    };

    private void PageSetup_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsPanel.Visibility == Visibility.Visible)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            return;
        }
        RefreshSettingsPanel();
        SettingsPanel.Visibility = Visibility.Visible;
    }

    /// <summary>Spiegelt die aktuelle Seite (und ggf. das Cover) in die Panel-Controls.</summary>
    private void RefreshSettingsPanel()
    {
        if (_vm == null || _page == null) return;
        _suppressSettingsEvents = true;

        (_page.Background switch
        {
            PageBackground.Lines => SetBgLines,
            PageBackground.Grid => SetBgGrid,
            PageBackground.Dots => SetBgDots,
            _ => SetBgBlank,
        }).IsChecked = true;

        (_page.Shade switch
        {
            PageShade.Light => SetShadeLight,
            PageShade.Dark => SetShadeDark,
            _ => SetShadeAuto,
        }).IsChecked = true;

        // Formen-Sektion nur bei aktivem Formen-Werkzeug
        ShapeSection.Visibility = _tool == ToolType.Shape ? Visibility.Visible : Visibility.Collapsed;
        // Notizzettel-Sektion nur bei aktivem Notizzettel-Werkzeug
        StickySection.Visibility = _tool == ToolType.Sticky ? Visibility.Visible : Visibility.Collapsed;

        bool paged = !_page.IsInfinite;
        SetSizeSection.Visibility = paged ? Visibility.Visible : Visibility.Collapsed;
        if (paged)
        {
            float longSide = Math.Max(_page.Width, _page.Height);
            (longSide > WhiteboardDoc.A4Height + 1 ? SetSizeA3 : SetSizeA4).IsChecked = true;
            (_page.Width > _page.Height ? SetOrientLandscape : SetOrientPortrait).IsChecked = true;
        }

        // Text-Werkzeug
        if (TextFontBox.ItemsSource == null) TextFontBox.ItemsSource = CoverFonts;
        TextFontBox.SelectedItem = CoverFonts.Contains(_textFont) ? _textFont : CoverFonts[0];
        TextColorSwatch.Background = BrushFromHex(CurrentInkHex());
        TextBgSwatch.Background = _textBgHex is { } textBg ? BrushFromHex(textBg) : Brushes.Transparent;

        bool hasCover = _vm.Doc.Pages.Any(p => p.IsCover);
        CoverSection.Visibility = hasCover ? Visibility.Visible : Visibility.Collapsed;
        if (hasCover)
        {
            var cs = _vm.Doc.Cover;
            CoverStartSwatch.Background = BrushFromHex(cs?.GradientStart ?? "#1E3A8A");
            CoverEndSwatch.Background = BrushFromHex(cs?.GradientEnd ?? "#7C3AED");

            string font = cs?.FontFamily ?? "Segoe UI";
            CoverFontBox.ItemsSource = CoverFonts.Contains(font)
                ? CoverFonts
                : CoverFonts.Append(font).OrderBy(f => f).ToArray();
            CoverFontBox.SelectedItem = font;

            BtnCoverImageRemove.IsEnabled = cs?.Image is { Length: > 0 };
        }

        _suppressSettingsEvents = false;
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        var c = ParseColor(hex);
        return new SolidColorBrush(Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
    }

    /// <summary>Änderungen im Panel wirken sofort auf die aktuelle Seite (kein OK-Knopf).</summary>
    private void PageSetting_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingsEvents || _vm == null || _page == null) return;

        _page.Background = SetBgLines.IsChecked == true ? PageBackground.Lines
            : SetBgGrid.IsChecked == true ? PageBackground.Grid
            : SetBgDots.IsChecked == true ? PageBackground.Dots
            : PageBackground.Blank;

        _page.Shade = SetShadeLight.IsChecked == true ? PageShade.Light
            : SetShadeDark.IsChecked == true ? PageShade.Dark
            : PageShade.Auto;

        if (!_page.IsInfinite)
        {
            float w = SetSizeA3.IsChecked == true ? WhiteboardDoc.A3Width : WhiteboardDoc.A4Width;
            float h = SetSizeA3.IsChecked == true ? WhiteboardDoc.A3Height : WhiteboardDoc.A4Height;
            bool landscape = SetOrientLandscape.IsChecked == true;
            float nw = landscape ? h : w, nh = landscape ? w : h;

            bool sizeChanged = Math.Abs(_page.Width - nw) > 0.5f || Math.Abs(_page.Height - nh) > 0.5f;
            _page.Width = nw;
            _page.Height = nh;
            if (sizeChanged) CenterView();

            if (SetAsDefault.IsChecked == true)
            {
                _vm.Doc.NewPageTemplate = new PageTemplate
                {
                    Width = nw,
                    Height = nh,
                    Background = _page.Background,
                    Shade = _page.Shade,
                };
            }
        }

        MarkDirty();
        Skia.InvalidateVisual();
    }

    // ---- Text-Werkzeug (Sidebar-Sektion) ----

    /// <summary>Standard-Hintergrund für neue Textfelder; null = transparent.</summary>
    private string? _textBgHex;
    private string _textFont = "Segoe UI";

    /// <summary>
    /// Sorgt für lesbaren Text: bei zu geringem Helligkeitskontrast zum Hintergrund
    /// kippt die Textfarbe auf Schwarz bzw. Weiß.
    /// </summary>
    private static string EnsureReadableTextColor(string textHex, string? bgHex)
    {
        if (bgHex == null) return textHex;
        var t = ParseColor(textHex);
        var b = ParseColor(bgHex);
        if (b.Alpha < 96) return textHex; // fast transparent → Seite bestimmt den Kontrast

        static double Lum(SKColor c) => 0.2126 * c.Red + 0.7152 * c.Green + 0.0722 * c.Blue;
        if (Math.Abs(Lum(t) - Lum(b)) >= 80) return textHex;
        return Lum(b) > 127 ? "#FF000000" : "#FFFFFFFF";
    }

    /// <summary>Wendet eine Stiländerung auf das gerade bearbeitete bzw. einzeln ausgewählte Textfeld an.</summary>
    private void ApplyToActiveText(Action<TextElement> apply)
    {
        TextElement? target = _editingText
            ?? (_selection.Count == 1 ? _selection.First() as TextElement : null);
        if (target == null) return;

        apply(target);
        MarkDirty();
        if (_editingText == target) StartTextEditRefresh(target);
        Skia.InvalidateVisual();
    }

    /// <summary>EditBox-Optik nach Stiländerung auffrischen, ohne die Eingabe zu unterbrechen.</summary>
    private void StartTextEditRefresh(TextElement el)
    {
        EditBox.FontFamily = new FontFamily(string.IsNullOrEmpty(el.FontFamily) ? "Segoe UI" : el.FontFamily);
        EditBox.Background = el.Background is { } bgHex
            ? BrushFromHex(bgHex)
            : new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
        try
        {
            var c = SKColor.Parse(el.Color);
            EditBox.Foreground = new SolidColorBrush(Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
        }
        catch { /* Farbe behalten */ }
    }

    // ---- Notizzettel-Werkzeug (Sidebar-Sektion) ----

    /// <summary>Wendet eine Änderung auf den gerade bearbeiteten bzw. einzeln ausgewählten Zettel an.</summary>
    private void ApplyToActiveSticky(Action<StickyNoteElement> apply)
    {
        StickyNoteElement? target = _editingSticky
            ?? (_selection.Count == 1 ? _selection.First() as StickyNoteElement : null);
        if (target == null) return;

        apply(target);
        MarkDirty();
        if (_editingSticky == target)
        {
            EditBox.Background = BrushFromHex(target.Color);
            EditBox.Foreground = BrushFromHex(target.TextColor);
        }
        Skia.InvalidateVisual();
    }

    private void StickyColor_Checked(object sender, RoutedEventArgs e)
    {
        // Tag kann während des XAML-Ladens noch fehlen → Standard behalten
        if (((RadioButton)sender).Tag is not string tag || tag.Length == 0) return;
        _stickyColorHex = tag;
        ApplyToActiveSticky(sn =>
        {
            sn.Color = _stickyColorHex;
            sn.TextColor = ReadableStickyTextColor(_stickyColorHex);
        });
    }

    private void StickyColorPick_Click(object sender, RoutedEventArgs e)
    {
        var cur = ParseColor(_stickyColorHex);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), Color.FromRgb(cur.Red, cur.Green, cur.Blue), allowAlpha: false) is not { } c)
            return;

        string hex = $"#FF{c.R:X2}{c.G:X2}{c.B:X2}";
        StickyCustomSwatch.Background = new SolidColorBrush(c);
        StickyCustomSwatch.Tag = hex;
        StickyCustomSwatch.Visibility = Visibility.Visible;
        _stickyColorHex = hex;
        StickyCustomSwatch.IsChecked = true; // löst StickyColor_Checked aus → wendet an
    }

    private void TextColor_Click(object sender, RoutedEventArgs e)
    {
        var cur = ParseColor(CurrentInkHex());
        if (ColorPickerDialog.Pick(Window.GetWindow(this), Color.FromArgb(cur.Alpha, cur.Red, cur.Green, cur.Blue)) is not { } c)
            return;

        // Textfarbe = Tintenfarbe: setzt die eigene Palette-Kachel und gilt für alle Werkzeuge
        string hex = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        CustomSwatch.Background = new SolidColorBrush(c);
        CustomSwatch.Tag = hex;
        CustomSwatch.Visibility = Visibility.Visible;
        _colorTag = hex;
        CustomSwatch.IsChecked = true;

        TextColorSwatch.Background = new SolidColorBrush(c);
        ApplyToActiveText(t => t.Color = hex);
    }

    private void TextBg_Click(object sender, RoutedEventArgs e)
    {
        var initial = _textBgHex is { } cur ? ParseColor(cur) : new SKColor(255, 249, 196);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), Color.FromRgb(initial.Red, initial.Green, initial.Blue), allowAlpha: false) is not { } c)
            return;

        _textBgHex = $"#FF{c.R:X2}{c.G:X2}{c.B:X2}";
        TextBgSwatch.Background = new SolidColorBrush(c);
        ApplyToActiveText(t =>
        {
            t.Background = _textBgHex;
            t.Color = EnsureReadableTextColor(t.Color, _textBgHex);
        });
    }

    private void TextBgClear_Click(object sender, RoutedEventArgs e)
    {
        _textBgHex = null;
        TextBgSwatch.Background = Brushes.Transparent;
        ApplyToActiveText(t => t.Background = null);
    }

    private void TextFont_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSettingsEvents) return;
        if (TextFontBox.SelectedItem is not string font) return;
        _textFont = font;
        ApplyToActiveText(t => t.FontFamily = font);
    }

    private CoverStyle EnsureCoverStyle()
    {
        _vm!.Doc.Cover ??= new CoverStyle();
        return _vm.Doc.Cover;
    }

    /// <summary>Zum Cover springen, damit Änderungen sofort sichtbar sind.</summary>
    private void CoverChanged()
    {
        if (_vm == null) return;
        MarkDirty();
        int idx = _vm.Doc.Pages.FindIndex(p => p.IsCover);
        if (idx >= 0 && idx != _vm.PageIndex) GoToPage(idx);
        Skia.InvalidateVisual();
    }

    private void CoverStart_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var cs = EnsureCoverStyle();
        var cur = ParseColor(cs.GradientStart);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), Color.FromRgb(cur.Red, cur.Green, cur.Blue), allowAlpha: false) is not { } c)
            return;
        cs.GradientStart = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        CoverStartSwatch.Background = new SolidColorBrush(c);
        CoverChanged();
    }

    private void CoverEnd_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var cs = EnsureCoverStyle();
        var cur = ParseColor(cs.GradientEnd);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), Color.FromRgb(cur.Red, cur.Green, cur.Blue), allowAlpha: false) is not { } c)
            return;
        cs.GradientEnd = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        CoverEndSwatch.Background = new SolidColorBrush(c);
        CoverChanged();
    }

    private void CoverFont_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSettingsEvents || _vm == null) return;
        if (CoverFontBox.SelectedItem is not string font) return;
        EnsureCoverStyle().FontFamily = font;
        CoverChanged();
    }

    private void CoverImage_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Bild als Cover wählen",
            Filter = "Bilder (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg|Alle Dateien (*.*)|*.*",
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var img = Path.GetExtension(dlg.FileName).Equals(".svg", StringComparison.OrdinalIgnoreCase)
                ? RasterizeSvg(File.ReadAllBytes(dlg.FileName))
                : PrepareRaster(File.ReadAllBytes(dlg.FileName));
            if (img == null)
            {
                MessageBox.Show("Das Bild konnte nicht geladen werden.", "Gonk Note",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var cs = EnsureCoverStyle();
            cs.Image = img.Value.Data;
            cs.ImageId = Guid.NewGuid();
            BtnCoverImageRemove.IsEnabled = true;
            CoverChanged();
        }
        catch
        {
            MessageBox.Show("Das Bild konnte nicht geladen werden.", "Gonk Note",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CoverImageRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_vm?.Doc.Cover is not { } cs) return;
        cs.Image = null;
        cs.ImageId = Guid.NewGuid();
        BtnCoverImageRemove.IsEnabled = false;
        CoverChanged();
    }

    // ==================== Bilder einfügen ====================

    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".svg" };
    private const int MaxImportDim = 2048;

    private SKPoint ViewCenter() => ToCanvas(new Point(CanvasHost.ActualWidth / 2, CanvasHost.ActualHeight / 2));

    /// <summary>Aktuell sichtbarer Bereich in Canvas-Koordinaten (fürs Culling).</summary>
    private SKRect VisibleCanvasRect()
    {
        var tl = ToCanvas(new Point(0, 0));
        var br = ToCanvas(new Point(CanvasHost.ActualWidth, CanvasHost.ActualHeight));
        return new SKRect(
            Math.Min(tl.X, br.X), Math.Min(tl.Y, br.Y),
            Math.Max(tl.X, br.X), Math.Max(tl.Y, br.Y));
    }

    /// <summary>Ein Import-Button für alle Formate: Bilder direkt, PDFs seitenweise.</summary>
    private async void InsertFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Datei einfügen",
            Filter = "Bilder & PDF (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg;*.pdf)"
                   + "|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg;*.pdf"
                   + "|PDF-Dokumente (*.pdf)|*.pdf"
                   + "|Bilder (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg"
                   + "|Alle Dateien (*.*)|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        var images = dlg.FileNames
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
        var pdfs = dlg.FileNames
            .Where(f => Path.GetExtension(f).Equals(".pdf", StringComparison.OrdinalIgnoreCase)).ToList();

        if (images.Count > 0) InsertImageFiles(images, ViewCenter());
        foreach (var pdf in pdfs) await InsertPdfFileAsync(pdf);
    }

    private void InsertImageFiles(IEnumerable<string> paths, SKPoint at)
    {
        var imported = new List<(byte[] Data, float W, float H)>();
        var failed = new List<string>();
        foreach (var path in paths)
        {
            try
            {
                var img = Path.GetExtension(path).Equals(".svg", StringComparison.OrdinalIgnoreCase)
                    ? RasterizeSvg(File.ReadAllBytes(path))
                    : PrepareRaster(File.ReadAllBytes(path));
                if (img != null) imported.Add(img.Value);
                else failed.Add(Path.GetFileName(path));
            }
            catch
            {
                failed.Add(Path.GetFileName(path));
            }
        }

        if (imported.Count > 0) PlaceImages(imported, at);
        if (failed.Count > 0)
            MessageBox.Show("Konnte nicht geladen werden:\n" + string.Join("\n", failed),
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>Dekodiert, verkleinert große Bilder (RAM-/DB-Ziel) und liefert speicherbare Bytes + Pixelmaße.</summary>
    private static (byte[] Data, float W, float H)? PrepareRaster(byte[] raw)
    {
        using var bmp = SKBitmap.Decode(raw);
        if (bmp == null) return null;

        // Klein genug: Originalbytes unverändert übernehmen
        if (bmp.Width <= MaxImportDim && bmp.Height <= MaxImportDim && raw.Length <= 2 * 1024 * 1024)
            return (raw, bmp.Width, bmp.Height);

        float scale = Math.Min(1f, MaxImportDim / (float)Math.Max(bmp.Width, bmp.Height));
        SKBitmap use = bmp;
        if (scale < 1f)
        {
            int nw = Math.Max(1, (int)(bmp.Width * scale));
            int nh = Math.Max(1, (int)(bmp.Height * scale));
            use = bmp.Resize(new SKImageInfo(nw, nh), SKFilterQuality.High) ?? bmp;
        }
        try
        {
            var format = HasTransparency(use) ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
            using var img = SKImage.FromBitmap(use);
            using var data = img.Encode(format, 88);
            return (data.ToArray(), use.Width, use.Height);
        }
        finally
        {
            if (!ReferenceEquals(use, bmp)) use.Dispose();
        }
    }

    private static bool HasTransparency(SKBitmap bmp)
    {
        if (bmp.AlphaType == SKAlphaType.Opaque) return false;
        int sx = Math.Max(1, bmp.Width / 256), sy = Math.Max(1, bmp.Height / 256);
        for (int y = 0; y < bmp.Height; y += sy)
            for (int x = 0; x < bmp.Width; x += sx)
                if (bmp.GetPixel(x, y).Alpha < 250) return true;
        return false;
    }

    /// <summary>SVG wird beim Import gerastert (2x für scharfes Zoomen), Ergebnis ist PNG; Anzeigegröße bleibt die SVG-Größe.</summary>
    private static (byte[] Data, float W, float H)? RasterizeSvg(byte[] raw)
    {
        using var svg = new Svg.Skia.SKSvg();
        using var ms = new MemoryStream(raw);
        if (svg.Load(ms) == null || svg.Picture == null) return null;

        var bounds = svg.Picture.CullRect;
        if (bounds.Width < 1 || bounds.Height < 1) return null;
        float scale = Math.Min(2f, MaxImportDim / Math.Max(bounds.Width, bounds.Height));
        int w = Math.Max(1, (int)(bounds.Width * scale));
        int h = Math.Max(1, (int)(bounds.Height * scale));

        using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (surface == null) return null;
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Scale(scale);
        surface.Canvas.Translate(-bounds.Left, -bounds.Top);
        surface.Canvas.DrawPicture(svg.Picture);
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return (data.ToArray(), bounds.Width, bounds.Height);
    }

    private void PlaceImages(List<(byte[] Data, float W, float H)> images, SKPoint at)
    {
        if (_page == null || _vm == null) return;

        // Maximale Anzeigegröße: in Seite bzw. Sichtbereich einpassen
        float maxW, maxH;
        if (_page.IsInfinite)
        {
            maxW = Math.Max(64f, (float)CanvasHost.ActualWidth * 0.6f / Zoom);
            maxH = Math.Max(64f, (float)CanvasHost.ActualHeight * 0.6f / Zoom);
        }
        else
        {
            maxW = _page.Width * 0.7f;
            maxH = _page.Height * 0.7f;
        }

        var added = new List<WbElement>();
        int i = 0;
        foreach (var (data, w, h) in images)
        {
            float scale = Math.Min(1f, Math.Min(maxW / Math.Max(1f, w), maxH / Math.Max(1f, h)));
            float dw = w * scale, dh = h * scale;
            float x = at.X - dw / 2f + i * 24f;
            float y = at.Y - dh / 2f + i * 24f;
            if (!_page.IsInfinite)
            {
                x = Math.Clamp(x, 0, Math.Max(0, _page.Width - dw));
                y = Math.Clamp(y, 0, Math.Max(0, _page.Height - dh));
            }
            added.Add(new ImageElement { X = x, Y = y, Width = dw, Height = dh, Data = data });
            i++;
        }

        _page.Elements.AddRange(added);
        _vm.Undo.Push(_page, new AddElementsAction(added));
        MarkDirty();

        // Direkt auswählen, damit Verschieben/Skalieren sofort möglich ist
        BtnLasso.IsChecked = true;
        _selection.Clear();
        foreach (var el in added) _selection.Add(el);
        ComputeSelectionBounds();
        Skia.InvalidateVisual();
    }

    private bool PasteImageFromClipboard()
    {
        if (Clipboard.ContainsImage() && Clipboard.GetImage() is { } src)
        {
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(src));
            using var ms = new MemoryStream();
            enc.Save(ms);
            if (PrepareRaster(ms.ToArray()) is { } img)
            {
                PlaceImages(new List<(byte[], float, float)> { img }, ViewCenter());
                return true;
            }
            return false;
        }

        if (Clipboard.ContainsFileDropList())
        {
            var files = Clipboard.GetFileDropList().Cast<string>()
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();
            if (files.Count > 0)
            {
                InsertImageFiles(files, ViewCenter());
                return true;
            }
        }
        return false;
    }

    private void CanvasHost_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetDroppedImageFiles(e).Count > 0 || GetDroppedPdfFiles(e).Count > 0
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void CanvasHost_Drop(object sender, DragEventArgs e)
    {
        var files = GetDroppedImageFiles(e);
        if (files.Count > 0)
            InsertImageFiles(files, ToCanvas(e.GetPosition(CanvasHost)));

        var pdfs = GetDroppedPdfFiles(e);
        e.Handled = true;
        foreach (var pdf in pdfs)
            await InsertPdfFileAsync(pdf);
    }

    private static List<string> GetDroppedImageFiles(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return new List<string>();
        return files.Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
    }

    private static List<string> GetDroppedPdfFiles(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return new List<string>();
        return files.Where(f => Path.GetExtension(f).Equals(".pdf", StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // ==================== PDF einfügen ====================

    /// <summary>Renderauflösung der langen Kante (≈ 200 % einer A4-Seite bei 96 DPI).</summary>
    private const int PdfRenderLongSide = 2246;

    private bool _importing;

    /// <summary>
    /// Rendert ein PDF im Hintergrund (UI bleibt bedienbar, Fortschritt sichtbar)
    /// und fügt es je nach Dokumenttyp ein. Ziel-Seite/-Dokument werden vor dem
    /// Await festgehalten, damit ein Tabwechsel während des Imports nichts verfälscht.
    /// </summary>
    private async Task InsertPdfFileAsync(string path)
    {
        if (_vm == null || _page == null || _importing) return;
        var vm = _vm;
        var anchor = _page;

        _importing = true;
        ShowBusy("PDF wird importiert…");
        var progress = new Progress<(int Done, int Total)>(t =>
            BusyText.Text = t.Total > 0 ? $"PDF wird importiert…  {t.Done} / {t.Total}" : "PDF wird importiert…");

        try
        {
            var pages = await Task.Run(() => PdfImporter.RenderPages(path, PdfRenderLongSide, progress));
            if (pages.Count == 0)
            {
                MessageBox.Show("Das PDF enthält keine darstellbaren Seiten.",
                    "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (anchor.IsInfinite) InsertPdfIntoWhiteboard(pages, anchor, vm);
            else InsertPdfIntoNotebook(pages, anchor, vm);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF konnte nicht geladen werden:\n{ex.Message}",
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _importing = false;
            HideBusy();
        }
    }

    private void ShowBusy(string text)
    {
        BusyText.Text = text;
        BusyBar.IsIndeterminate = true;
        BusyOverlay.Visibility = Visibility.Visible;
    }

    private void HideBusy() => BusyOverlay.Visibility = Visibility.Collapsed;

    /// <summary>Anzeigemaße einer PDF-Seite: lange Kante = A4-Höhe, Seitenverhältnis bleibt.</summary>
    private static (float W, float H) PdfDisplaySize(PdfImporter.PdfPageImage pg)
    {
        const float longSide = WhiteboardDoc.A4Height;
        return pg.Height >= pg.Width
            ? (longSide * pg.Width / pg.Height, longSide)
            : (longSide, longSide * pg.Height / pg.Width);
    }

    /// <summary>Notizbuch: jede PDF-Seite wird eine neue Seite hinter der Ankerseite.</summary>
    private void InsertPdfIntoNotebook(List<PdfImporter.PdfPageImage> pages, WbPage anchor, WhiteboardTabViewModel vm)
    {
        int insertAt = vm.Doc.Pages.IndexOf(anchor) + 1;
        if (insertAt <= 0) insertAt = vm.Doc.Pages.Count;

        foreach (var pg in pages)
        {
            var (pw, ph) = PdfDisplaySize(pg);
            vm.Doc.Pages.Insert(insertAt++, new WbPage
            {
                Width = pw,
                Height = ph,
                Background = PageBackground.Blank,
                Shade = PageShade.Light,
                BackgroundImage = pg.Data,
                BackgroundImageId = Guid.NewGuid(),
            });
        }

        vm.IsDirty = true;
        // Nur wenn dieses Dokument noch angezeigt wird, zur ersten neuen Seite springen
        if (_vm == vm && _page == anchor)
        {
            GoToPage(vm.Doc.Pages.IndexOf(anchor) + 1);
            UpdatePageLabel();
        }
    }

    /// <summary>Whiteboard: PDF-Seiten zweispaltig (s1 s2 / s3 s4 …) als Bild-Elemente.</summary>
    private void InsertPdfIntoWhiteboard(List<PdfImporter.PdfPageImage> pages, WbPage anchor, WhiteboardTabViewModel vm)
    {
        const float gap = 28f;
        var sizes = pages.Select(PdfDisplaySize).ToList();
        float colW = sizes.Max(s => s.W);

        // Startpunkt: sichtbarer Mittelpunkt, wenn das Dokument noch angezeigt wird
        SKPoint at = _vm == vm && _page == anchor ? ViewCenter() : new SKPoint(0, 0);
        float leftX = at.X - colW - gap / 2f;

        var added = new List<WbElement>();
        float y = at.Y;
        for (int i = 0; i < pages.Count; i += 2)
        {
            float rowH = sizes[i].H;
            if (i + 1 < pages.Count) rowH = Math.Max(rowH, sizes[i + 1].H);

            added.Add(new ImageElement
            {
                X = leftX, Y = y, Width = sizes[i].W, Height = sizes[i].H, Data = pages[i].Data,
            });
            if (i + 1 < pages.Count)
                added.Add(new ImageElement
                {
                    X = leftX + colW + gap, Y = y,
                    Width = sizes[i + 1].W, Height = sizes[i + 1].H, Data = pages[i + 1].Data,
                });

            y += rowH + gap;
        }

        anchor.Elements.AddRange(added);
        vm.Undo.Push(anchor, new AddElementsAction(added));
        vm.IsDirty = true;

        if (_vm == vm && _page == anchor)
        {
            BtnLasso.IsChecked = true;
            _selection.Clear();
            foreach (var el in added) _selection.Add(el);
            ComputeSelectionBounds();
            Skia.InvalidateVisual();
        }
    }

    private bool HitResizeHandle(IBoxElement box, SKPoint c)
    {
        float r = 12f / Zoom;
        float dx = c.X - (box.X + box.Width), dy = c.Y - (box.Y + box.Height);
        return dx * dx + dy * dy <= r * r;
    }

    // ==================== Radierer ====================

    private void EraseAt(SKPoint c)
    {
        if (_page == null || _eraseSteps == null) return;
        float r = 14f / Zoom;

        for (int i = _page.Elements.Count - 1; i >= 0; i--)
        {
            var el = _page.Elements[i];
            switch (el)
            {
                // Striche: nur die berührte Stelle entfernen, Reststücke bleiben stehen
                case StrokeElement s:
                {
                    if (!HitElement(s, c, r)) break;
                    var parts = SplitStroke(s, c, r + s.Width / 2f);
                    _page.Elements.RemoveAt(i);
                    _page.Elements.InsertRange(i, parts);
                    _eraseSteps.Add(new EraseStep(s, i, parts));
                    MarkDirty();
                    break;
                }

                // Formen/Text: als Ganzes, aber nur bei Berührung der Kontur bzw. des Rahmens
                case ShapeElement or TextElement:
                    if (!HitElement(el, c, r)) break;
                    _page.Elements.RemoveAt(i);
                    _eraseSteps.Add(new EraseStep(el, i, new List<WbElement>()));
                    MarkDirty();
                    break;

                // Bilder sind nicht radierbar – über Lasso auswählen und löschen
            }
        }
    }

    /// <summary>Zerlegt einen Strich in die Teilstücke außerhalb des Radierkreises.</summary>
    private static List<WbElement> SplitStroke(StrokeElement s, SKPoint c, float rr)
    {
        var parts = new List<WbElement>();
        var run = new List<WbPoint>();

        void Flush()
        {
            if (run.Count >= 2)
                parts.Add(new StrokeElement { Points = run, Color = s.Color, Width = s.Width, Kind = s.Kind });
            run = new List<WbPoint>();
        }

        var pts = s.Points;
        float rr2 = rr * rr;
        for (int i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            float dx = p.X - c.X, dy = p.Y - c.Y;
            if (dx * dx + dy * dy <= rr2) { Flush(); continue; }

            run.Add(p);

            // Segment kreuzt den Radierkreis, ohne dass ein Endpunkt drinliegt → trotzdem trennen
            if (i + 1 < pts.Count)
            {
                var q = pts[i + 1];
                float qdx = q.X - c.X, qdy = q.Y - c.Y;
                if (qdx * qdx + qdy * qdy > rr2 &&
                    SegmentDistance(new SKPoint(p.X, p.Y), new SKPoint(q.X, q.Y), c) <= rr)
                    Flush();
            }
        }
        Flush();
        return parts;
    }

    private static bool HitElement(WbElement el, SKPoint c, float r)
    {
        switch (el)
        {
            case StrokeElement s:
                float rr = r + s.Width / 2f;
                for (int i = 0; i < s.Points.Count; i++)
                {
                    var p = s.Points[i];
                    if (SegOrPointDist(s.Points, i, c) <= rr) return true;
                }
                return false;

            case ShapeElement sh:
                return ShapeOutlineDist(sh, c) <= r + sh.StrokeWidth / 2f;

            case TextElement t:
                var b = TextBounds(t);
                b.Inflate(r, r);
                return b.Contains(c);

            default:
                return false;
        }
    }

    private static float SegOrPointDist(List<WbPoint> pts, int i, SKPoint c)
    {
        var a = new SKPoint(pts[i].X, pts[i].Y);
        if (i + 1 >= pts.Count) return SKPoint.Distance(a, c);
        var b = new SKPoint(pts[i + 1].X, pts[i + 1].Y);
        return SegmentDistance(a, b, c);
    }

    private static float SegmentDistance(SKPoint a, SKPoint b, SKPoint p)
    {
        float abx = b.X - a.X, aby = b.Y - a.Y;
        float len2 = abx * abx + aby * aby;
        float t = len2 < 1e-6f ? 0 : Math.Clamp(((p.X - a.X) * abx + (p.Y - a.Y) * aby) / len2, 0, 1);
        float px = a.X + t * abx - p.X, py = a.Y + t * aby - p.Y;
        return MathF.Sqrt(px * px + py * py);
    }

    private static float ShapeOutlineDist(ShapeElement sh, SKPoint c)
    {
        var p1 = new SKPoint(sh.X1, sh.Y1);
        var p2 = new SKPoint(sh.X2, sh.Y2);
        switch (sh.Shape)
        {
            case ShapeKind.Line:
            case ShapeKind.Arrow:
                return SegmentDistance(p1, p2, c);

            case ShapeKind.Rectangle:
            {
                var r = SKRect.Create(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                      Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));
                var tl = new SKPoint(r.Left, r.Top);
                var tr = new SKPoint(r.Right, r.Top);
                var br = new SKPoint(r.Right, r.Bottom);
                var bl = new SKPoint(r.Left, r.Bottom);
                return Math.Min(Math.Min(SegmentDistance(tl, tr, c), SegmentDistance(tr, br, c)),
                                Math.Min(SegmentDistance(br, bl, c), SegmentDistance(bl, tl, c)));
            }

            case ShapeKind.Ellipse:
            {
                float cx = (p1.X + p2.X) / 2f, cy = (p1.Y + p2.Y) / 2f;
                float rx = Math.Max(1f, Math.Abs(p2.X - p1.X) / 2f);
                float ry = Math.Max(1f, Math.Abs(p2.Y - p1.Y) / 2f);
                // Abstand grob über normalisierte Radialdistanz
                float nx = (c.X - cx) / rx, ny = (c.Y - cy) / ry;
                float d = MathF.Sqrt(nx * nx + ny * ny);
                return Math.Abs(d - 1f) * Math.Min(rx, ry);
            }

            case ShapeKind.Triangle:
            {
                var (a, b2, c2) = TrianglePoints(sh);
                return Math.Min(SegmentDistance(a, b2, c),
                       Math.Min(SegmentDistance(b2, c2, c), SegmentDistance(c2, a, c)));
            }

            default:
                return float.MaxValue;
        }
    }

    private static (SKPoint A, SKPoint B, SKPoint C) TrianglePoints(ShapeElement sh)
    {
        float minX = Math.Min(sh.X1, sh.X2), maxX = Math.Max(sh.X1, sh.X2);
        float minY = Math.Min(sh.Y1, sh.Y2), maxY = Math.Max(sh.Y1, sh.Y2);
        return (new SKPoint((minX + maxX) / 2f, minY),
                new SKPoint(maxX, maxY),
                new SKPoint(minX, maxY));
    }

    // ==================== Lasso / Auswahl ====================

    private void ClearSelection()
    {
        _selection.Clear();
        _movingSelection = false;
        Skia.InvalidateVisual();
    }

    private SKRect InflatedSelectionBounds()
    {
        var b = _selectionBounds;
        b.Inflate(12f / Zoom, 12f / Zoom);
        return b;
    }

    private void SelectByLasso(List<SKPoint> lasso)
    {
        if (_page == null) return;
        using var path = new SKPath();
        path.MoveTo(lasso[0]);
        for (int i = 1; i < lasso.Count; i++) path.LineTo(lasso[i]);
        path.Close();

        _selection.Clear();
        foreach (var el in _page.Elements)
        {
            bool inside = el switch
            {
                StrokeElement s => s.Points.Count > 0 &&
                    s.Points.Count(p => path.Contains(p.X, p.Y)) * 2 >= s.Points.Count,
                ShapeElement sh => path.Contains((sh.X1 + sh.X2) / 2f, (sh.Y1 + sh.Y2) / 2f),
                TextElement t => path.Contains(TextBounds(t).MidX, TextBounds(t).MidY),
                ImageElement im => path.Contains(im.X + im.Width / 2f, im.Y + im.Height / 2f),
                _ => false,
            };
            if (inside) _selection.Add(el);
        }

        if (_selection.Count > 0) ComputeSelectionBounds();
        Skia.InvalidateVisual();
    }

    private void ComputeSelectionBounds()
    {
        bool first = true;
        SKRect r = SKRect.Empty;
        foreach (var el in _selection)
        {
            var b = ElementBounds(el);
            if (first) { r = b; first = false; }
            else r = SKRect.Union(r, b);
        }
        _selectionBounds = r;
    }

    internal static SKRect ElementBounds(WbElement el)
    {
        switch (el)
        {
            case StrokeElement s:
            {
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (var p in s.Points)
                {
                    minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                    minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                }
                var r = new SKRect(minX, minY, maxX, maxY);
                r.Inflate(s.Width / 2f, s.Width / 2f);
                return r;
            }
            case ShapeElement sh:
            {
                var r = new SKRect(Math.Min(sh.X1, sh.X2), Math.Min(sh.Y1, sh.Y2),
                                   Math.Max(sh.X1, sh.X2), Math.Max(sh.Y1, sh.Y2));
                r.Inflate(sh.StrokeWidth / 2f, sh.StrokeWidth / 2f);
                return r;
            }
            case TextElement t:
                return TextBounds(t);
            case ImageElement im:
                return SKRect.Create(im.X, im.Y, im.Width, im.Height);
            case StickyNoteElement sn:
                return SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height);
            default:
                return SKRect.Empty;
        }
    }

    private void DeleteSelection()
    {
        if (_page == null || _vm == null || _selection.Count == 0) return;
        var action = new RemoveElementsAction(_page, _selection);
        action.Redo(_page);
        _vm.Undo.Push(_page, action);
        _selection.Clear();
        MarkDirty();
        Skia.InvalidateVisual();
    }

    private void DuplicateSelection()
    {
        if (_page == null || _vm == null || _selection.Count == 0) return;
        var clones = _selection.Select(CloneElement).ToList();
        foreach (var cl in clones) cl.Translate(18, 18);
        _page.Elements.AddRange(clones);
        _vm.Undo.Push(_page, new AddElementsAction(clones));

        _selection.Clear();
        foreach (var cl in clones) _selection.Add(cl);
        ComputeSelectionBounds();
        MarkDirty();
        Skia.InvalidateVisual();
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

    // ==================== Texteingabe ====================

    private void StartTextEdit(TextElement el, bool isNew)
    {
        CommitActiveEdit();
        _editingText = el;
        _editingIsNew = isNew;
        _editingOldText = el.Text;
        _cancelEdit = false;

        var screen = ToScreen(new SKPoint(el.X, el.Y));
        Canvas.SetLeft(EditBox, screen.X - 4);
        Canvas.SetTop(EditBox, screen.Y - 3);
        EditBox.FontSize = Math.Max(8, el.FontSize * Zoom);
        EditBox.FontFamily = new FontFamily(string.IsNullOrEmpty(el.FontFamily) ? "Segoe UI" : el.FontFamily);
        EditBox.Background = el.Background is { } bgHex
            ? BrushFromHex(bgHex)
            : new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
        EditBox.Text = el.Text;
        try
        {
            var c = SKColor.Parse(el.Color);
            EditBox.Foreground = new SolidColorBrush(Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
        }
        catch { /* Standardfarbe behalten */ }

        EditBox.Visibility = Visibility.Visible;
        EditBox.Focus();
        EditBox.CaretIndex = EditBox.Text.Length;
        Skia.InvalidateVisual();
    }

    private void EditBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _cancelEdit = true;
            Focus(); // löst LostFocus aus
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            Focus();
            e.Handled = true;
        }
    }

    private void EditBox_LostFocus(object sender, RoutedEventArgs e) => CommitActiveEdit();

    private void CommitTextEdit()
    {
        if (_editingText == null || _page == null || _vm == null) return;
        var el = _editingText;
        _editingText = null;

        EditBox.Visibility = Visibility.Collapsed;
        string newText = _cancelEdit ? _editingOldText : EditBox.Text;
        _cancelEdit = false;

        if (_editingIsNew)
        {
            if (!string.IsNullOrWhiteSpace(newText))
            {
                el.Text = newText;
                _page.Elements.Add(el);
                _vm.Undo.Push(_page, new AddElementsAction(new WbElement[] { el }));
                MarkDirty();
            }
        }
        else if (string.IsNullOrWhiteSpace(newText))
        {
            var action = new RemoveElementsAction(_page, new[] { el });
            action.Redo(_page);
            _vm.Undo.Push(_page, action);
            MarkDirty();
        }
        else if (newText != _editingOldText)
        {
            el.Text = newText;
            _vm.Undo.Push(_page, new TextChangeAction(el, _editingOldText, newText));
            MarkDirty();
        }

        Skia.InvalidateVisual();
    }

    private static SKRect TextBounds(TextElement t)
    {
        using var paint = new SKPaint
        {
            TextSize = t.FontSize,
            Typeface = Fonts.Family(t.FontFamily),
        };
        var lines = t.Text.Length == 0 ? new[] { " " } : t.Text.Split('\n');
        float w = 10;
        foreach (var line in lines)
            w = Math.Max(w, paint.MeasureText(line.Length == 0 ? " " : line));
        float h = lines.Length * t.FontSize * 1.35f;
        return SKRect.Create(t.X, t.Y, w, h);
    }

    // ==================== Notizzettel-Bearbeitung ====================

    /// <summary>Innenabstand des Zettels zwischen Kartenrand und Text (Canvas-Einheiten).</summary>
    private const float StickyPad = 14f;

    /// <summary>Schließt eine offene Text- oder Notizzettel-Bearbeitung.</summary>
    private void CommitActiveEdit()
    {
        CommitTextEdit();
        CommitStickyEdit();
    }

    private void StartStickyEdit(StickyNoteElement el, bool isNew)
    {
        CommitActiveEdit();
        _editingSticky = el;
        _editingStickyIsNew = isNew;
        _editingStickyOld = el.Text;
        _cancelEdit = false;

        var screen = ToScreen(new SKPoint(el.X + StickyPad, el.Y + StickyPad));
        Canvas.SetLeft(EditBox, screen.X);
        Canvas.SetTop(EditBox, screen.Y);
        EditBox.Width = Math.Max(24, (el.Width - StickyPad * 2) * Zoom);
        EditBox.Height = Math.Max(24, (el.Height - StickyPad * 2) * Zoom);
        EditBox.TextWrapping = TextWrapping.Wrap;
        EditBox.VerticalContentAlignment = VerticalAlignment.Top;
        EditBox.FontSize = Math.Max(8, el.FontSize * Zoom);
        EditBox.FontFamily = new FontFamily(string.IsNullOrEmpty(el.FontFamily) ? "Segoe UI" : el.FontFamily);
        EditBox.Background = BrushFromHex(el.Color);
        EditBox.Foreground = BrushFromHex(el.TextColor);
        EditBox.Text = el.Text;

        EditBox.Visibility = Visibility.Visible;
        EditBox.Focus();
        EditBox.CaretIndex = EditBox.Text.Length;
        Skia.InvalidateVisual();
    }

    private void CommitStickyEdit()
    {
        if (_editingSticky == null || _page == null || _vm == null) return;
        var el = _editingSticky;
        _editingSticky = null;

        EditBox.Visibility = Visibility.Collapsed;
        // Zettel-spezifische EditBox-Optik zurücksetzen (sonst erbt das Textfeld sie)
        EditBox.Width = double.NaN;
        EditBox.Height = double.NaN;
        EditBox.TextWrapping = TextWrapping.NoWrap;

        string newText = _cancelEdit ? _editingStickyOld : EditBox.Text;
        _cancelEdit = false;

        if (_editingStickyIsNew)
        {
            // Ein bewusst gesetzter Zettel bleibt bestehen, auch ohne Text
            el.Text = newText;
            _page.Elements.Add(el);
            _vm.Undo.Push(_page, new AddElementsAction(new WbElement[] { el }));
            MarkDirty();
        }
        else if (newText != _editingStickyOld)
        {
            el.Text = newText;
            _vm.Undo.Push(_page, new StickyTextChangeAction(el, _editingStickyOld, newText));
            MarkDirty();
        }

        Skia.InvalidateVisual();
    }

    /// <summary>Dunkler oder heller Text je nach Helligkeit der Zettelfarbe.</summary>
    private static string ReadableStickyTextColor(string bgHex)
    {
        var b = ParseColor(bgHex);
        double lum = 0.2126 * b.Red + 0.7152 * b.Green + 0.0722 * b.Blue;
        return lum > 140 ? "#FF1F2937" : "#FFF9FAFB";
    }

    // ==================== Lineal ====================

    private void Ruler_Click(object sender, RoutedEventArgs e) => SetRuler(BtnRuler.IsChecked == true);

    private void SetRuler(bool on)
    {
        _rulerOn = on;
        BtnRuler.IsChecked = on;
        if (on && !_rulerPlaced)
        {
            var v = VisibleCanvasRect();
            _rulerCenter = new SKPoint(v.MidX, v.MidY);
            _rulerAngleDeg = 0f;
            _rulerPlaced = true;
        }
        Skia.InvalidateVisual();
    }

    /// <summary>Richtungs- (entlang) und Normalenvektor (quer) der Lineal-Ausrichtung.</summary>
    private (SKPoint Dir, SKPoint Nrm) RulerAxes()
    {
        float a = _rulerAngleDeg * MathF.PI / 180f;
        var d = new SKPoint(MathF.Cos(a), MathF.Sin(a));
        return (d, new SKPoint(-d.Y, d.X));
    }

    private SKPoint RulerHandleCenter()
    {
        var (d, _) = RulerAxes();
        float ext = RulerLength / 2f + 16f / Zoom;
        return new SKPoint(_rulerCenter.X + d.X * ext, _rulerCenter.Y + d.Y * ext);
    }

    /// <summary>Punkt in Lineal-lokale Koordinaten: x entlang der Kante, y quer dazu.</summary>
    private (float Lx, float Ly) RulerLocal(SKPoint c)
    {
        var (d, n) = RulerAxes();
        float rx = c.X - _rulerCenter.X, ry = c.Y - _rulerCenter.Y;
        return (rx * d.X + ry * d.Y, rx * n.X + ry * n.Y);
    }

    private bool RulerHandleHit(SKPoint c)
    {
        var h = RulerHandleCenter();
        float r = 13f / Zoom;
        float dx = c.X - h.X, dy = c.Y - h.Y;
        return dx * dx + dy * dy <= r * r;
    }

    private bool RulerBodyContains(SKPoint c)
    {
        var (lx, ly) = RulerLocal(c);
        return Math.Abs(lx) <= RulerLength / 2f && Math.Abs(ly) <= RulerHalfWidth;
    }

    /// <summary>Prüft, ob ein Strichstart nahe einer Lineal-Kante liegt, und aktiviert das Einrasten.</summary>
    private bool TryActivateRulerSnap(SKPoint c)
    {
        _rulerSnapActive = false;
        if (!_rulerOn) return false;
        var (lx, ly) = RulerLocal(c);
        if (Math.Abs(lx) > RulerLength / 2f + 120f) return false;

        float distTop = Math.Abs(ly - RulerHalfWidth);
        float distBot = Math.Abs(ly + RulerHalfWidth);
        if (Math.Min(distTop, distBot) > RulerSnapDist) return false;

        float edgeOff = distTop <= distBot ? RulerHalfWidth : -RulerHalfWidth;
        var (d, n) = RulerAxes();
        _rulerSnapE0 = new SKPoint(_rulerCenter.X + n.X * edgeOff, _rulerCenter.Y + n.Y * edgeOff);
        _rulerSnapDir = d;
        _rulerSnapActive = true;
        return true;
    }

    /// <summary>Projiziert einen Punkt auf die eingerastete Kantenlinie (sonst unverändert).</summary>
    private SKPoint ApplyRulerSnap(SKPoint p)
    {
        if (!_rulerSnapActive) return p;
        float t = (p.X - _rulerSnapE0.X) * _rulerSnapDir.X + (p.Y - _rulerSnapE0.Y) * _rulerSnapDir.Y;
        return new SKPoint(_rulerSnapE0.X + _rulerSnapDir.X * t, _rulerSnapE0.Y + _rulerSnapDir.Y * t);
    }

    /// <summary>Startet Bewegen/Drehen des Lineals, wenn Körper bzw. Dreh-Griff getroffen wird.</summary>
    private bool TryBeginRuler(SKPoint c)
    {
        if (!_rulerOn) return false;
        if (RulerHandleHit(c)) { _rulerDrag = RulerDrag.Rotate; Skia.InvalidateVisual(); return true; }
        if (RulerBodyContains(c)) { _rulerDrag = RulerDrag.Move; _rulerDragLast = c; Skia.InvalidateVisual(); return true; }
        return false;
    }

    private void UpdateRulerDrag(SKPoint c)
    {
        if (_rulerDrag == RulerDrag.Move)
        {
            _rulerCenter = new SKPoint(_rulerCenter.X + (c.X - _rulerDragLast.X), _rulerCenter.Y + (c.Y - _rulerDragLast.Y));
            _rulerDragLast = c;
        }
        else if (_rulerDrag == RulerDrag.Rotate)
        {
            _rulerAngleDeg = MathF.Atan2(c.Y - _rulerCenter.Y, c.X - _rulerCenter.X) * 180f / MathF.PI;
        }
        Skia.InvalidateVisual();
    }

    private void DrawRuler(SKCanvas canvas)
    {
        var (d, n) = RulerAxes();
        float hl = RulerLength / 2f, hw = RulerHalfWidth;
        SKPoint P(float u, float v) => new(_rulerCenter.X + u * d.X + v * n.X, _rulerCenter.Y + u * d.Y + v * n.Y);

        var accent = ResColorFromBrush("Brush.Accent");

        using (var body = new SKPath())
        {
            body.MoveTo(P(-hl, -hw)); body.LineTo(P(hl, -hw)); body.LineTo(P(hl, hw)); body.LineTo(P(-hl, hw)); body.Close();
            using var fill = new SKPaint { Color = new SKColor(30, 41, 59, 40), IsAntialias = true };
            canvas.DrawPath(body, fill);
            using var edge = new SKPaint { Color = accent, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f / Zoom, IsAntialias = true };
            canvas.DrawPath(body, edge);
        }

        // cm-Skala entlang der unteren Kante (längerer Strich alle 5 cm)
        using (var tick = new SKPaint { Color = accent.WithAlpha(210), Style = SKPaintStyle.Stroke, StrokeWidth = 1f / Zoom, IsAntialias = true })
        {
            int k = 0;
            for (float u = 0; u <= hl; u += PxPerCm, k++)
            {
                float len = (k % 5 == 0) ? 9f : 5f;
                canvas.DrawLine(P(u, hw), P(u, hw - len), tick);
                if (u > 0) canvas.DrawLine(P(-u, hw), P(-u, hw - len), tick);
            }
        }

        // Dreh-Griff am Ende
        var h = RulerHandleCenter();
        using (var hf = new SKPaint { Color = accent, IsAntialias = true })
            canvas.DrawCircle(h, 6f / Zoom, hf);
        using (var hr = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 1.4f / Zoom, IsAntialias = true })
            canvas.DrawCircle(h, 6f / Zoom, hr);
    }

    // ==================== Tastatur ====================

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (EditBox.IsKeyboardFocused) return;
        if (_vm == null) return;

        bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

        if (ctrl && e.Key == Key.Z) { DoUndo(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.Y) { DoRedo(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.D) { DuplicateSelection(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.V) { if (PasteImageFromClipboard()) e.Handled = true; return; }

        switch (e.Key)
        {
            case Key.Delete:
            case Key.Back:
                DeleteSelection();
                e.Handled = true;
                return;
            case Key.Escape:
                ClearSelection();
                e.Handled = true;
                return;
            case Key.Space:
                _spaceDown = true;
                return;
        }

        if (ctrl) return;

        if (e.Key == Key.R) { SetRuler(!_rulerOn); e.Handled = true; return; }

        ToggleButton? btn = e.Key switch
        {
            Key.S => BtnPen,
            Key.G => BtnSmoothPen,
            Key.B => BtnPencil,
            Key.M => BtnHighlighter,
            Key.E => BtnEraser,
            Key.L => BtnLasso,
            Key.T => BtnText,
            Key.F => BtnShape,
            Key.N => BtnSticky,
            Key.H => BtnPan,
            _ => null,
        };
        if (btn != null)
        {
            btn.IsChecked = true;
            e.Handled = true;
        }
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space) _spaceDown = false;
    }

    // ==================== Seiten ====================

    private void UpdatePageLabel()
    {
        if (_vm == null) return;
        var pages = _vm.Doc.Pages;
        int covers = pages.Count(pg => pg.IsCover);
        int contentTotal = pages.Count - covers;

        if (pages[_vm.PageIndex].IsCover)
        {
            PageLabel.Text = "Cover";
        }
        else
        {
            int num = 0;
            for (int i = 0; i <= _vm.PageIndex; i++)
                if (!pages[i].IsCover) num++;
            PageLabel.Text = $"Seite {num} / {contentTotal}";
        }
    }

    private void GoToPage(int idx)
    {
        if (_vm == null) return;
        idx = Math.Clamp(idx, 0, _vm.Doc.Pages.Count - 1);
        if (idx == _vm.PageIndex) return;
        CommitActiveEdit();
        ClearSelection();
        _vm.PageIndex = idx;
        _page = _vm.Doc.Pages[idx];
        UpdatePageLabel();
        if (SettingsPanel.Visibility == Visibility.Visible) RefreshSettingsPanel();
        Skia.InvalidateVisual();
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e) => GoToPage((_vm?.PageIndex ?? 0) - 1);
    private void NextPage_Click(object sender, RoutedEventArgs e) => GoToPage((_vm?.PageIndex ?? 0) + 1);

    private void AddPage_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;

        WbPage page;
        if (_vm.Doc.NewPageTemplate != null)
        {
            page = _vm.Doc.PageFromTemplate();
        }
        else if (_page is { IsCover: false, IsInfinite: false })
        {
            // Ohne Vorlage: aktuelle Seite fortführen
            page = new WbPage
            {
                Width = _page.Width,
                Height = _page.Height,
                Background = _page.Background,
                Shade = _page.Shade,
            };
        }
        else
        {
            page = WhiteboardDoc.NewNotebookPage();
        }

        _vm.Doc.Pages.Insert(_vm.PageIndex + 1, page);
        MarkDirty();
        GoToPage(_vm.PageIndex + 1);
        UpdatePageLabel();
    }

    private void DeletePage_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null || _vm.Doc.Pages.Count <= 1 || _page == null) return;
        if ((_page.Elements.Count > 0 || _page.BackgroundImage != null) &&
            MessageBox.Show("Diese Seite und ihren Inhalt löschen?", "Gonk Note",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        int idx = _vm.PageIndex;
        _vm.Doc.Pages.RemoveAt(idx);
        _vm.PageIndex = Math.Min(idx, _vm.Doc.Pages.Count - 1);
        _page = _vm.Doc.Pages[_vm.PageIndex];
        MarkDirty();
        UpdatePageLabel();
        Skia.InvalidateVisual();
    }

    // ==================== Rendering ====================

    private static SKColor ResColor(string key)
    {
        if (Application.Current.Resources[key] is Color c)
            return new SKColor(c.R, c.G, c.B, c.A);
        return SKColors.Magenta;
    }

    private void Skia_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var clearColor = ResColor("Color.CanvasBg");
        if (_page is { IsInfinite: true } p && p.Shade != PageShade.Auto)
            clearColor = EffectiveShade(p) == PageShade.Dark
                ? SKColor.Parse("#12161F")
                : SKColor.Parse("#EEF2F8");
        canvas.Clear(clearColor);
        if (_page == null || _vm == null) return;

        if (!_vm.ViewInitialized && CanvasHost.ActualWidth > 0)
        {
            CenterView();
            _vm.ViewInitialized = true;
        }

        float pixelScale = (float)(e.Info.Width / Math.Max(1.0, Skia.ActualWidth));
        canvas.Scale(pixelScale);
        canvas.Translate(PanX, PanY);
        canvas.Scale(Zoom);

        DrawPageBackground(canvas);

        // Viewport-Culling: nur sichtbare Elemente zeichnen. Das verhindert, dass
        // bei vielen (hochauflösenden) Bildern jedes Frame alle dekodiert werden.
        var visible = VisibleCanvasRect();
        foreach (var el in _page.Elements)
        {
            if (el == _editingText) continue;
            // Zettel in Bearbeitung: nur die Karte zeichnen, den Text übernimmt die EditBox
            if (el == _editingSticky) { DrawStickyCard(canvas, (StickyNoteElement)el); continue; }
            var b = ElementBounds(el);
            if (!b.IsEmpty && !visible.IntersectsWith(b)) continue;
            DrawElement(canvas, el);
        }

        DrawActiveOverlays(canvas);
    }

    private SKColor PageLineColor()
    {
        if (_page == null || _page.Shade == PageShade.Auto) return ResColor("Color.PageLine");
        return EffectiveShade(_page) == PageShade.Dark ? SKColor.Parse("#35486E") : SKColor.Parse("#BBD2F0");
    }

    private SKColor PageDotColor()
    {
        if (_page == null || _page.Shade == PageShade.Auto) return ResColor("Color.PageGridDot");
        return EffectiveShade(_page) == PageShade.Dark ? SKColor.Parse("#3A4A6B") : SKColor.Parse("#B8C6DC");
    }

    private void DrawPageBackground(SKCanvas canvas)
    {
        if (_page == null) return;

        if (_page.IsInfinite)
        {
            DrawInfinitePattern(canvas);
            return;
        }

        var pageRect = SKRect.Create(0, 0, _page.Width, _page.Height);

        using (var shadow = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(60),
            ImageFilter = SKImageFilter.CreateBlur(6, 6),
        })
        {
            var sr = pageRect;
            sr.Offset(0, 3);
            canvas.DrawRect(sr, shadow);
        }

        if (_page.IsCover)
        {
            DrawCover(canvas);
            return;
        }

        var bgColor = _page.Shade == PageShade.Auto ? ResColor("Color.PageBg")
            : EffectiveShade(_page) == PageShade.Dark ? SKColor.Parse("#1E2638") : SKColors.White;
        using (var bg = new SKPaint { Color = bgColor })
            canvas.DrawRect(pageRect, bg);

        // Hintergrundbild (importierte PDF-Seite): seitenfüllend, ersetzt das Muster
        if (_page.BackgroundImage is { Length: > 0 } bgData &&
            ImageCache.Get(_page.BackgroundImageId, bgData) is { } bgImg)
        {
            using var ip = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };
            canvas.DrawImage(bgImg, pageRect, ip);
            return;
        }

        using var line = new SKPaint
        {
            Color = PageLineColor(),
            StrokeWidth = 1f,
            IsAntialias = false,
        };

        const float spacingLines = 30f;
        switch (_page.Background)
        {
            case PageBackground.Lines:
                for (float y = 84; y < _page.Height - 30; y += spacingLines)
                    canvas.DrawLine(30, y, _page.Width - 30, y, line);
                break;

            case PageBackground.Grid:
                for (float y = 0; y <= _page.Height; y += spacingLines)
                    canvas.DrawLine(0, y, _page.Width, y, line);
                for (float x = 0; x <= _page.Width; x += spacingLines)
                    canvas.DrawLine(x, 0, x, _page.Height, line);
                break;

            case PageBackground.Dots:
                using (var dot = new SKPaint { Color = PageDotColor(), IsAntialias = true })
                {
                    for (float x = 24; x < _page.Width; x += 24)
                        for (float y = 24; y < _page.Height; y += 24)
                            canvas.DrawCircle(x, y, 1.1f, dot);
                }
                break;
        }
    }

    /// <summary>Muster für die unendliche Fläche, nur über den sichtbaren Bereich.</summary>
    private void DrawInfinitePattern(SKCanvas canvas)
    {
        if (_page == null || _page.Background == PageBackground.Blank) return;

        var tl = ToCanvas(new Point(0, 0));
        var br = ToCanvas(new Point(CanvasHost.ActualWidth, CanvasHost.ActualHeight));
        float spacing = _page.Background == PageBackground.Dots ? 28f : 30f;
        while (spacing * Zoom < 14f) spacing *= 2f;

        float x0 = MathF.Floor(tl.X / spacing) * spacing;
        float y0 = MathF.Floor(tl.Y / spacing) * spacing;

        switch (_page.Background)
        {
            case PageBackground.Dots:
                using (var dot = new SKPaint { Color = PageDotColor(), IsAntialias = true })
                {
                    float r = 1.1f / Zoom;
                    for (float x = x0; x <= br.X; x += spacing)
                        for (float y = y0; y <= br.Y; y += spacing)
                            canvas.DrawCircle(x, y, r, dot);
                }
                break;

            case PageBackground.Grid:
            case PageBackground.Lines:
                using (var line = new SKPaint { Color = PageLineColor(), StrokeWidth = 1f / Zoom })
                {
                    for (float y = y0; y <= br.Y; y += spacing)
                        canvas.DrawLine(tl.X, y, br.X, y, line);
                    if (_page.Background == PageBackground.Grid)
                        for (float x = x0; x <= br.X; x += spacing)
                            canvas.DrawLine(x, tl.Y, x, br.Y, line);
                }
                break;
        }
    }

    /// <summary>Cover-Seite: Bild oder Farbverlauf, Akzentlinie und Dokumenttitel (Stil anpassbar).</summary>
    private void DrawCover(SKCanvas canvas)
    {
        if (_page == null) return;
        var rect = SKRect.Create(0, 0, _page.Width, _page.Height);
        var cs = _vm?.Doc.Cover;

        // Bild-Cover: füllt die Seite formatfüllend (mittig beschnitten)
        if (cs?.Image is { Length: > 0 } imgData &&
            ImageCache.Get(cs.ImageId, imgData) is { } coverImg)
        {
            float scale = Math.Max(rect.Width / coverImg.Width, rect.Height / coverImg.Height);
            float w = coverImg.Width * scale, h = coverImg.Height * scale;
            var dst = SKRect.Create(rect.MidX - w / 2f, rect.MidY - h / 2f, w, h);
            canvas.Save();
            canvas.ClipRect(rect);
            using var ip = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };
            canvas.DrawImage(coverImg, dst, ip);
            canvas.Restore();
            return;
        }

        using (var grad = new SKPaint { IsAntialias = true })
        {
            grad.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(_page.Width, _page.Height),
                new[] { ParseColor(cs?.GradientStart ?? "#1E3A8A"), ParseColor(cs?.GradientEnd ?? "#7C3AED") },
                null, SKShaderTileMode.Clamp);
            canvas.DrawRect(rect, grad);
        }

        var coverBold = cs == null ? Fonts.Bold
            : SKTypeface.FromFamilyName(cs.FontFamily, SKFontStyle.Bold) ?? Fonts.Bold;

        string title = _vm?.Item.Name ?? "";
        using var titlePaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = 46,
            Typeface = coverBold,
            TextAlign = SKTextAlign.Center,
        };
        while (titlePaint.TextSize > 18 && titlePaint.MeasureText(title) > _page.Width * 0.8f)
            titlePaint.TextSize -= 2;
        canvas.DrawText(title, _page.Width / 2f, _page.Height * 0.4f, titlePaint);

        using (var accent = new SKPaint
        {
            Color = SKColor.Parse("#2DD4BF"),
            StrokeWidth = 4,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
        })
            canvas.DrawLine(_page.Width * 0.3f, _page.Height * 0.445f,
                            _page.Width * 0.7f, _page.Height * 0.445f, accent);

        using var subPaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(170),
            IsAntialias = true,
            TextSize = 15,
            Typeface = cs == null ? Fonts.Regular : SKTypeface.FromFamilyName(cs.FontFamily) ?? Fonts.Regular,
            TextAlign = SKTextAlign.Center,
        };
        canvas.DrawText("N O T I Z B U C H", _page.Width / 2f, _page.Height * 0.49f, subPaint);
    }

    private void DrawElement(SKCanvas canvas, WbElement el)
    {
        switch (el)
        {
            case StrokeElement s: DrawStroke(canvas, s); break;
            case ShapeElement sh: DrawShape(canvas, sh, sh.Color, sh.StrokeWidth); break;
            case TextElement t: DrawText(canvas, t); break;
            case ImageElement im: DrawImage(canvas, im); break;
            case StickyNoteElement sn: DrawSticky(canvas, sn); break;
        }
    }

    internal static void DrawImage(SKCanvas canvas, ImageElement im)
    {
        var rect = SKRect.Create(im.X, im.Y, im.Width, im.Height);
        var img = ImageCache.Get(im.Id, im.Data);
        if (img == null)
        {
            // Nicht dekodierbar: Platzhalter, damit das Element auswählbar bleibt
            using var ph = new SKPaint { Color = SKColors.Gray.WithAlpha(60) };
            canvas.DrawRect(rect, ph);
            return;
        }
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };
        canvas.DrawImage(img, rect, paint);
    }

    private static SKPath BuildSmoothPath(List<WbPoint> pts)
    {
        var path = new SKPath();
        if (pts.Count == 0) return path;
        path.MoveTo(pts[0].X, pts[0].Y);
        for (int i = 1; i < pts.Count - 1; i++)
        {
            float mx = (pts[i].X + pts[i + 1].X) / 2f;
            float my = (pts[i].Y + pts[i + 1].Y) / 2f;
            path.QuadTo(pts[i].X, pts[i].Y, mx, my);
        }
        if (pts.Count > 1)
            path.LineTo(pts[^1].X, pts[^1].Y);
        return path;
    }

    internal static void DrawStroke(SKCanvas canvas, StrokeElement s)
    {
        if (s.Points.Count == 0) return;
        var color = ParseColor(s.Color);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color,
        };

        switch (s.Kind)
        {
            case StrokeKind.Highlighter:
                paint.Color = color.WithAlpha(70);
                paint.StrokeCap = SKStrokeCap.Butt;
                paint.StrokeWidth = s.Width;
                using (var path = BuildSmoothPath(s.Points))
                    canvas.DrawPath(path, paint);
                break;

            case StrokeKind.Pencil:
                paint.Color = color.WithAlpha(185);
                paint.StrokeWidth = s.Width;
                paint.PathEffect = SKPathEffect.CreateDiscrete(3f, 0.55f);
                using (var path = BuildSmoothPath(s.Points))
                    canvas.DrawPath(path, paint);
                break;

            default: // Stift: Druck steuert die Strichbreite je Segment
                if (s.Points.Count == 2 &&
                    Math.Abs(s.Points[0].X - s.Points[1].X) < 0.5f &&
                    Math.Abs(s.Points[0].Y - s.Points[1].Y) < 0.5f)
                {
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(s.Points[0].X, s.Points[0].Y, s.Width * 0.7f, paint);
                    return;
                }
                for (int i = 0; i < s.Points.Count - 1; i++)
                {
                    var a = s.Points[i];
                    var b = s.Points[i + 1];
                    float p = (a.P + b.P) / 2f;
                    paint.StrokeWidth = s.Width * (0.35f + 1.1f * p);
                    canvas.DrawLine(a.X, a.Y, b.X, b.Y, paint);
                }
                break;
        }
    }

    internal static void DrawShape(SKCanvas canvas, ShapeElement sh, string colorHex, float strokeWidth)
    {
        var color = ParseColor(colorHex);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color,
            StrokeWidth = strokeWidth,
        };

        var p1 = new SKPoint(sh.X1, sh.Y1);
        var p2 = new SKPoint(sh.X2, sh.Y2);

        switch (sh.Shape)
        {
            case ShapeKind.Line:
                canvas.DrawLine(p1, p2, paint);
                break;

            case ShapeKind.Arrow:
            {
                canvas.DrawLine(p1, p2, paint);
                float angle = MathF.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                float head = strokeWidth * 3f + 10f;
                const float spread = 0.46f;
                var h1 = new SKPoint(p2.X - head * MathF.Cos(angle - spread), p2.Y - head * MathF.Sin(angle - spread));
                var h2 = new SKPoint(p2.X - head * MathF.Cos(angle + spread), p2.Y - head * MathF.Sin(angle + spread));
                canvas.DrawLine(p2, h1, paint);
                canvas.DrawLine(p2, h2, paint);
                break;
            }

            case ShapeKind.Rectangle:
            {
                var r = SKRect.Create(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                      Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));
                if (sh.Fill != null)
                {
                    using var fill = new SKPaint { IsAntialias = true, Color = ParseColor(sh.Fill) };
                    canvas.DrawRect(r, fill);
                }
                canvas.DrawRect(r, paint);
                break;
            }

            case ShapeKind.Ellipse:
            {
                var r = SKRect.Create(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                      Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));
                if (sh.Fill != null)
                {
                    using var fill = new SKPaint { IsAntialias = true, Color = ParseColor(sh.Fill) };
                    canvas.DrawOval(r, fill);
                }
                canvas.DrawOval(r, paint);
                break;
            }

            case ShapeKind.Triangle:
            {
                var (a, b, c) = TrianglePoints(sh);
                using var path = new SKPath();
                path.MoveTo(a); path.LineTo(b); path.LineTo(c); path.Close();
                if (sh.Fill != null)
                {
                    using var fill = new SKPaint { IsAntialias = true, Color = ParseColor(sh.Fill) };
                    canvas.DrawPath(path, fill);
                }
                canvas.DrawPath(path, paint);
                break;
            }
        }
    }

    internal static void DrawText(SKCanvas canvas, TextElement t)
    {
        if (t.Background != null)
        {
            var b = TextBounds(t);
            b.Inflate(5, 3);
            using var bg = new SKPaint { IsAntialias = true, Color = ParseColor(t.Background) };
            canvas.DrawRoundRect(b, 3, 3, bg);
        }

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(t.Color),
            TextSize = t.FontSize,
            Typeface = Fonts.Family(t.FontFamily),
        };
        float lineHeight = t.FontSize * 1.35f;
        var lines = t.Text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            canvas.DrawText(lines[i], t.X, t.Y + t.FontSize + i * lineHeight, paint);
    }

    /// <summary>Zeichnet nur die Zettelkarte (Schatten, Fläche, dezenter Rand) – ohne Text.</summary>
    internal static void DrawStickyCard(SKCanvas canvas, StickyNoteElement sn)
    {
        var rect = SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height);
        const float radius = 6f;

        // weicher Schlagschatten für einen „aufgeklebten" Eindruck
        using (var shadow = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0, 0, 0, 45),
            ImageFilter = SKImageFilter.CreateBlur(6, 6),
        })
        {
            var sr = rect;
            sr.Offset(0, 3);
            canvas.DrawRoundRect(sr, radius, radius, shadow);
        }

        var fill = ParseColor(sn.Color);
        using (var bg = new SKPaint { IsAntialias = true, Color = fill })
            canvas.DrawRoundRect(rect, radius, radius, bg);

        // hauchzarter Rand, leicht dunkler als die Füllung
        using var border = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            Color = new SKColor(0, 0, 0, 28),
        };
        canvas.DrawRoundRect(rect, radius, radius, border);
    }

    internal static void DrawSticky(SKCanvas canvas, StickyNoteElement sn)
    {
        DrawStickyCard(canvas, sn);
        if (string.IsNullOrEmpty(sn.Text)) return;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(sn.TextColor),
            TextSize = sn.FontSize,
            Typeface = Fonts.Family(sn.FontFamily),
        };

        float lineHeight = sn.FontSize * 1.32f;
        float maxWidth = sn.Width - StickyPad * 2;
        float x = sn.X + StickyPad;
        float yBase = sn.Y + StickyPad + sn.FontSize;
        float maxY = sn.Y + sn.Height - StickyPad * 0.5f;

        canvas.Save();
        canvas.ClipRect(SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height));
        float y = yBase;
        foreach (var line in WrapText(sn.Text, paint, maxWidth))
        {
            if (y > maxY) break; // Text, der nicht mehr passt, wird abgeschnitten
            canvas.DrawText(line, x, y, paint);
            y += lineHeight;
        }
        canvas.Restore();
    }

    /// <summary>Bricht Text an Wortgrenzen auf die verfügbare Breite um (respektiert \n).</summary>
    private static IEnumerable<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        foreach (var para in text.Split('\n'))
        {
            if (para.Length == 0) { yield return ""; continue; }

            var words = para.Split(' ');
            var current = "";
            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (paint.MeasureText(candidate) <= maxWidth || current.Length == 0)
                {
                    // Einzelnes zu langes Wort hart umbrechen
                    if (current.Length == 0 && paint.MeasureText(word) > maxWidth)
                    {
                        foreach (var chunk in BreakLongWord(word, paint, maxWidth))
                        {
                            if (chunk.Last) { current = chunk.Text; }
                            else yield return chunk.Text;
                        }
                    }
                    else current = candidate;
                }
                else
                {
                    yield return current;
                    current = word;
                    if (paint.MeasureText(word) > maxWidth)
                    {
                        foreach (var chunk in BreakLongWord(word, paint, maxWidth))
                        {
                            if (chunk.Last) current = chunk.Text;
                            else yield return chunk.Text;
                        }
                    }
                }
            }
            yield return current;
        }
    }

    private static IEnumerable<(string Text, bool Last)> BreakLongWord(string word, SKPaint paint, float maxWidth)
    {
        var chunk = "";
        foreach (var ch in word)
        {
            if (chunk.Length > 0 && paint.MeasureText(chunk + ch) > maxWidth)
            {
                yield return (chunk, false);
                chunk = ch.ToString();
            }
            else chunk += ch;
        }
        yield return (chunk, true);
    }

    private void DrawActiveOverlays(SKCanvas canvas)
    {
        // Laufender Strich
        if (_drawing && _activePoints is { Count: > 0 })
        {
            var kind = _tool switch
            {
                ToolType.Pencil => StrokeKind.Pencil,
                ToolType.Highlighter => StrokeKind.Highlighter,
                _ => StrokeKind.Pen,
            };
            DrawStroke(canvas, new StrokeElement
            {
                Points = _activePoints,
                Color = CurrentInkHex(),
                Width = kind == StrokeKind.Highlighter ? Math.Max(_width * 5f, 10f) : _width,
                Kind = kind,
            });
        }

        // Formvorschau
        if (_shapeActive)
        {
            DrawShape(canvas, new ShapeElement
            {
                Shape = _shape,
                X1 = _shapeStart.X, Y1 = _shapeStart.Y,
                X2 = _shapeCur.X, Y2 = _shapeCur.Y,
                Color = CurrentInkHex(),
                StrokeWidth = _width,
                Fill = CurrentFill(),
            }, CurrentInkHex(), _width);
        }

        var accent = ResColorFromBrush("Brush.Accent");

        // Lasso-Pfad
        if (_lassoPts is { Count: > 1 })
        {
            using var path = new SKPath();
            path.MoveTo(_lassoPts[0]);
            for (int i = 1; i < _lassoPts.Count; i++) path.LineTo(_lassoPts[i]);

            using var fill = new SKPaint { Color = accent.WithAlpha(25), IsAntialias = true };
            canvas.DrawPath(path, fill);

            using var stroke = new SKPaint
            {
                Color = accent,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.4f / Zoom,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new[] { 6f / Zoom, 4f / Zoom }, 0),
            };
            canvas.DrawPath(path, stroke);
        }

        // Auswahlrahmen
        if (_selection.Count > 0)
        {
            var b = InflatedSelectionBounds();
            using var fill = new SKPaint { Color = accent.WithAlpha(18) };
            canvas.DrawRect(b, fill);
            using var stroke = new SKPaint
            {
                Color = accent,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.4f / Zoom,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new[] { 6f / Zoom, 4f / Zoom }, 0),
            };
            canvas.DrawRect(b, stroke);

            // Eckgriff zum Skalieren (bei einzelnem Box-Element: Bild oder Notizzettel)
            if (_selection.Count == 1 && _selection.First() is IBoxElement selBox)
            {
                float hs = 5f / Zoom;
                var hr = SKRect.Create(selBox.X + selBox.Width - hs, selBox.Y + selBox.Height - hs, hs * 2, hs * 2);
                using var hf = new SKPaint { Color = accent, IsAntialias = true };
                canvas.DrawRect(hr, hf);
                using var hw = new SKPaint
                {
                    Color = SKColors.White,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.2f / Zoom,
                    IsAntialias = true,
                };
                canvas.DrawRect(hr, hw);
            }
        }

        // Radierer-Cursor
        if (_eraserVisible && EffectiveTool == ToolType.Eraser)
        {
            using var ring = new SKPaint
            {
                Color = ResColorFromBrush("Brush.Text").WithAlpha(160),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.2f / Zoom,
                IsAntialias = true,
            };
            canvas.DrawCircle(_eraserPos, 14f / Zoom, ring);
        }

        // Lineal zuletzt (liegt über allem)
        if (_rulerOn) DrawRuler(canvas);
    }

    private static SKColor ResColorFromBrush(string key)
    {
        if (Application.Current.Resources[key] is SolidColorBrush b)
            return new SKColor(b.Color.R, b.Color.G, b.Color.B, b.Color.A);
        return SKColors.DodgerBlue;
    }

    private static SKColor ParseColor(string hex)
    {
        try { return SKColor.Parse(hex); }
        catch { return SKColors.Gray; }
    }
}

/// <summary>Gemeinsame Schriften für Canvas-Text.</summary>
internal static class Fonts
{
    public static readonly SKTypeface Regular =
        SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;

    public static readonly SKTypeface Bold =
        SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold) ?? SKTypeface.Default;

    private static readonly Dictionary<string, SKTypeface> _families = new();

    /// <summary>Typeface je Familienname, gecacht.</summary>
    public static SKTypeface Family(string? name)
    {
        if (string.IsNullOrEmpty(name) || name == "Segoe UI") return Regular;
        if (!_families.TryGetValue(name, out var tf))
        {
            tf = SKTypeface.FromFamilyName(name) ?? Regular;
            _families[name] = tf;
        }
        return tf;
    }
}
