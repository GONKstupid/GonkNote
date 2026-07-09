using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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

    // Eingabezustand
    private bool _drawing;
    private List<WbPoint>? _activePoints;
    private SKPoint _shapeStart, _shapeCur;
    private bool _shapeActive;
    private bool _panning;
    private Point _panLast;
    private bool _spaceDown;
    private bool _stylusInverted;

    // Radierer
    private List<(WbElement El, int Index)>? _erased;
    private SKPoint _eraserPos;
    private bool _eraserVisible;

    // Lasso / Auswahl
    private List<SKPoint>? _lassoPts;
    private readonly HashSet<WbElement> _selection = new();
    private SKRect _selectionBounds;
    private bool _movingSelection;
    private SKPoint _moveLast;
    private float _movedX, _movedY;

    // Texteingabe
    private TextElement? _editingText;
    private bool _editingIsNew;
    private string _editingOldText = "";
    private bool _cancelEdit;

    private ToggleButton[] ToolButtons => new[] { BtnPen, BtnPencil, BtnHighlighter, BtnEraser, BtnLasso, BtnText, BtnShape, BtnPan };
    private ToggleButton[] ShapeButtons => new[] { BtnShapeLine, BtnShapeArrow, BtnShapeRect, BtnShapeEllipse, BtnShapeTriangle };

    public WhiteboardView()
    {
        InitializeComponent();

        _suppressToolEvents = true;
        BtnPen.IsChecked = true;
        BtnShapeRect.IsChecked = true;
        _suppressToolEvents = false;

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

    private void SetTool(ToolType tool)
    {
        CommitTextEdit();
        if (tool != ToolType.Lasso) ClearSelection();

        _tool = tool;
        ShapePicker.Visibility = tool == ToolType.Shape ? Visibility.Visible : Visibility.Collapsed;
        _eraserVisible = false;

        CanvasHost.Cursor = tool switch
        {
            ToolType.Pen or ToolType.Pencil or ToolType.Highlighter => Cursors.Pen,
            ToolType.Eraser => Cursors.None,
            ToolType.Text => Cursors.IBeam,
            ToolType.Shape => Cursors.Cross,
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
        _colorTag = (string)((RadioButton)sender).Tag;
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
        CommitTextEdit();
        ClearSelection();
        var page = _vm.Undo.Undo();
        if (page != null) { NavigateToPage(page); MarkDirty(); Skia.InvalidateVisual(); }
    }

    private void DoRedo()
    {
        if (_vm == null) return;
        CommitTextEdit();
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

        // Finger = Verschieben (wie GoodNotes), Stift-Rückseite = Radierer
        if (e.StylusDevice.TabletDevice?.Type == TabletDeviceType.Touch)
        {
            BeginPan(e.GetPosition(CanvasHost));
            CanvasHost.CaptureStylus();
            e.Handled = true;
            return;
        }

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
        if (_panning)
        {
            MovePan(e.GetPosition(CanvasHost));
            e.Handled = true;
            return;
        }
        if (!_drawing && _erased == null && _lassoPts == null && !_movingSelection && !_shapeActive)
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
            (_drawing || _erased != null || _lassoPts != null || _movingSelection || _shapeActive))
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
        CommitTextEdit();

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                _drawing = true;
                _activePoints = new List<WbPoint> { new(c.X, c.Y, Math.Clamp(pressure, 0.05f, 1f)) };
                break;

            case ToolType.Eraser:
                _erased = new List<(WbElement, int)>();
                _eraserPos = c;
                _eraserVisible = true;
                EraseAt(c);
                break;

            case ToolType.Lasso:
                if (_selection.Count > 0 && InflatedSelectionBounds().Contains(c))
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
                    Color = CurrentInkHex(),
                    FontSize = 18f,
                }, isNew: true);
                break;

            case ToolType.Shape:
                _shapeActive = true;
                _shapeStart = _shapeCur = c;
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

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                if (!_drawing || _activePoints == null) return;
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
                if (_erased == null) return;
                _eraserPos = c;
                _eraserVisible = true;
                EraseAt(c);
                break;

            case ToolType.Lasso:
                if (_movingSelection)
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

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                if (!_drawing || _activePoints == null) break;
                CommitStroke();
                break;

            case ToolType.Eraser:
                if (_erased is { Count: > 0 })
                {
                    _vm.Undo.Push(_page, new RemoveElementsAction(_erased));
                    MarkDirty();
                }
                _erased = null;
                break;

            case ToolType.Lasso:
                if (_movingSelection)
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

        var stroke = new StrokeElement
        {
            Points = _activePoints,
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
        };
        _page.Elements.Add(shape);
        _vm.Undo.Push(_page, new AddElementsAction(new WbElement[] { shape }));
        MarkDirty();
    }

    private string CurrentInkHex()
    {
        if (_colorTag != "auto") return _colorTag;
        var c = ResColor("Color.DefaultInk");
        return $"#{c.Alpha:X2}{c.Red:X2}{c.Green:X2}{c.Blue:X2}";
    }

    // ==================== Radierer ====================

    private void EraseAt(SKPoint c)
    {
        if (_page == null || _erased == null) return;
        float r = 14f / Zoom;

        for (int i = _page.Elements.Count - 1; i >= 0; i--)
        {
            var el = _page.Elements[i];
            if (!HitElement(el, c, r)) continue;
            _erased.Add((el, i));
            _page.Elements.RemoveAt(i);
            MarkDirty();
        }
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

    private static SKRect ElementBounds(WbElement el)
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
        },
        _ => throw new NotSupportedException(),
    };

    // ==================== Texteingabe ====================

    private void StartTextEdit(TextElement el, bool isNew)
    {
        CommitTextEdit();
        _editingText = el;
        _editingIsNew = isNew;
        _editingOldText = el.Text;
        _cancelEdit = false;

        var screen = ToScreen(new SKPoint(el.X, el.Y));
        Canvas.SetLeft(EditBox, screen.X - 4);
        Canvas.SetTop(EditBox, screen.Y - 3);
        EditBox.FontSize = Math.Max(8, el.FontSize * Zoom);
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

    private void EditBox_LostFocus(object sender, RoutedEventArgs e) => CommitTextEdit();

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
            Typeface = Fonts.Regular,
        };
        var lines = t.Text.Length == 0 ? new[] { " " } : t.Text.Split('\n');
        float w = 10;
        foreach (var line in lines)
            w = Math.Max(w, paint.MeasureText(line.Length == 0 ? " " : line));
        float h = lines.Length * t.FontSize * 1.35f;
        return SKRect.Create(t.X, t.Y, w, h);
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
        ToggleButton? btn = e.Key switch
        {
            Key.S => BtnPen,
            Key.B => BtnPencil,
            Key.M => BtnHighlighter,
            Key.E => BtnEraser,
            Key.L => BtnLasso,
            Key.T => BtnText,
            Key.F => BtnShape,
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
        PageLabel.Text = $"Seite {_vm.PageIndex + 1} / {_vm.Doc.Pages.Count}";
    }

    private void GoToPage(int idx)
    {
        if (_vm == null) return;
        idx = Math.Clamp(idx, 0, _vm.Doc.Pages.Count - 1);
        if (idx == _vm.PageIndex) return;
        CommitTextEdit();
        ClearSelection();
        _vm.PageIndex = idx;
        _page = _vm.Doc.Pages[idx];
        UpdatePageLabel();
        Skia.InvalidateVisual();
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e) => GoToPage((_vm?.PageIndex ?? 0) - 1);
    private void NextPage_Click(object sender, RoutedEventArgs e) => GoToPage((_vm?.PageIndex ?? 0) + 1);

    private void AddPage_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var page = WhiteboardDoc.NewNotebookPage();
        page.Background = _page?.Background ?? PageBackground.Lines;
        _vm.Doc.Pages.Insert(_vm.PageIndex + 1, page);
        MarkDirty();
        GoToPage(_vm.PageIndex + 1);
        UpdatePageLabel();
    }

    private void DeletePage_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null || _vm.Doc.Pages.Count <= 1 || _page == null) return;
        if (_page.Elements.Count > 0 &&
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
        canvas.Clear(ResColor("Color.CanvasBg"));
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

        foreach (var el in _page.Elements)
        {
            if (el == _editingText) continue;
            DrawElement(canvas, el);
        }

        DrawActiveOverlays(canvas);
    }

    private void DrawPageBackground(SKCanvas canvas)
    {
        if (_page == null) return;

        if (_page.IsInfinite)
        {
            // Punktraster über den sichtbaren Bereich
            var tl = ToCanvas(new Point(0, 0));
            var br = ToCanvas(new Point(CanvasHost.ActualWidth, CanvasHost.ActualHeight));
            float spacing = 28f;
            while (spacing * Zoom < 14f) spacing *= 2f;

            using var dot = new SKPaint { Color = ResColor("Color.PageGridDot"), IsAntialias = true };
            float r = 1.1f / Zoom;
            for (float x = MathF.Floor(tl.X / spacing) * spacing; x <= br.X; x += spacing)
                for (float y = MathF.Floor(tl.Y / spacing) * spacing; y <= br.Y; y += spacing)
                    canvas.DrawCircle(x, y, r, dot);
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

        using (var bg = new SKPaint { Color = ResColor("Color.PageBg") })
            canvas.DrawRect(pageRect, bg);

        using var line = new SKPaint
        {
            Color = ResColor("Color.PageLine"),
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
                using (var dot = new SKPaint { Color = ResColor("Color.PageGridDot"), IsAntialias = true })
                {
                    for (float x = 24; x < _page.Width; x += 24)
                        for (float y = 24; y < _page.Height; y += 24)
                            canvas.DrawCircle(x, y, 1.1f, dot);
                }
                break;
        }
    }

    private void DrawElement(SKCanvas canvas, WbElement el)
    {
        switch (el)
        {
            case StrokeElement s: DrawStroke(canvas, s); break;
            case ShapeElement sh: DrawShape(canvas, sh, sh.Color, sh.StrokeWidth); break;
            case TextElement t: DrawText(canvas, t); break;
        }
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

    private static void DrawStroke(SKCanvas canvas, StrokeElement s)
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

    private static void DrawShape(SKCanvas canvas, ShapeElement sh, string colorHex, float strokeWidth)
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

    private static void DrawText(SKCanvas canvas, TextElement t)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(t.Color),
            TextSize = t.FontSize,
            Typeface = Fonts.Regular,
        };
        float lineHeight = t.FontSize * 1.35f;
        var lines = t.Text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            canvas.DrawText(lines[i], t.X, t.Y + t.FontSize + i * lineHeight, paint);
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

/// <summary>Gemeinsame Schrift für Canvas-Text.</summary>
internal static class Fonts
{
    public static readonly SKTypeface Regular =
        SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;
}
