using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using System.Collections.Generic;
using GonkNote.Editing;
using GonkNote.Models;
using GonkNote.Rendering;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Avalonia;

/// <summary>
/// Zeichnet eine <see cref="WbPage"/> über SkiaSharp — dieselben Routinen wie die WPF-App
/// (<see cref="WbRenderer"/> in GonkNote.Core). Avalonia rendert selbst mit Skia, daher lässt
/// sich der SKCanvas direkt ausleihen (kein Zwischenbild). HANDOFF §9.3e.
/// **Stand: Anzeige (read-only).** Werkzeuge/Stift-Eingabe folgen in Schritt 4b.
/// </summary>
public class WhiteboardCanvas : Control
{
    public static readonly StyledProperty<WbPage?> PageProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, WbPage?>(nameof(Page));

    /// <summary>Zoomfaktor (1 = 100 %).</summary>
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, double>(nameof(Zoom), 1.0);

    public WbPage? Page
    {
        get => GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    /// <summary>Aktuelle Stiftfarbe (Hex).</summary>
    public static readonly StyledProperty<string> InkColorProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, string>(nameof(InkColor), "#111827");

    /// <summary>Aktuelle Strichstärke.</summary>
    public static readonly StyledProperty<double> InkWidthProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, double>(nameof(InkWidth), 3.0);

    public string InkColor
    {
        get => GetValue(InkColorProperty);
        set => SetValue(InkColorProperty, value);
    }

    public double InkWidth
    {
        get => GetValue(InkWidthProperty);
        set => SetValue(InkWidthProperty, value);
    }

    /// <summary>Aktives Werkzeug (Stift/Pencil/Textmarker/Radierer/Hand).</summary>
    public static readonly StyledProperty<ToolType> ToolProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, ToolType>(nameof(Tool), ToolType.Pen);

    public ToolType Tool
    {
        get => GetValue(ToolProperty);
        set => SetValue(ToolProperty, value);
    }

    /// <summary>Verschiebung der Leinwand (Hand-Werkzeug / mittlere Maustaste).</summary>
    public static readonly StyledProperty<double> PanXProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, double>(nameof(PanX));

    public static readonly StyledProperty<double> PanYProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, double>(nameof(PanY));

    public double PanX { get => GetValue(PanXProperty); set => SetValue(PanXProperty, value); }
    public double PanY { get => GetValue(PanYProperty); set => SetValue(PanYProperty, value); }

    /// <summary>Feuert mit dem fertigen Strich (Speichern + Undo-Eintrag).</summary>
    public event EventHandler<StrokeElement>? StrokeCompleted;

    /// <summary>Feuert mit den Radier-Schritten eines Zuges (ein Undo-Schritt).</summary>
    public event EventHandler<List<EraseStep>>? ElementsErased;

    /// <summary>Der gerade gezeichnete Strich (noch nicht Teil der Seite).</summary>
    private StrokeElement? _active;

    /// <summary>Radier-Schritte des laufenden Zuges (für einen einzigen Undo-Schritt).</summary>
    private List<EraseStep>? _eraseSteps;

    private bool _panning;
    private Point _panStart;
    private double _panOriginX, _panOriginY;

    /// <summary>Aktuell ausgewählte Elemente (Lasso / Verschieben).</summary>
    private readonly List<WbElement> _selection = new();

    private bool _movingSelection;
    private SKPoint _moveLast;
    private float _moveDx, _moveDy;          // Summe für einen einzigen Undo-Schritt

    /// <summary>Laufendes Lasso-Polygon (Seiten-Koordinaten).</summary>
    private List<SKPoint>? _lasso;

    /// <summary>Gerade aufgezogene Form (Vorschau bis zum Loslassen).</summary>
    private ShapeElement? _shape;

    /// <summary>Zu zeichnende Form beim Formen-Werkzeug.</summary>
    public static readonly StyledProperty<ShapeKind> ShapeKindProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, ShapeKind>(nameof(Shape), ShapeKind.Rectangle);

    public ShapeKind Shape
    {
        get => GetValue(ShapeKindProperty);
        set => SetValue(ShapeKindProperty, value);
    }

    /// <summary>Feuert, wenn Elemente hinzugefügt wurden (Form/Zettel/Text) — für Undo+Speichern.</summary>
    public event EventHandler<WbElement>? ElementAdded;

    /// <summary>Feuert nach dem Verschieben einer Auswahl (dx, dy) — für Undo+Speichern.</summary>
    public event EventHandler<(List<WbElement> Els, float Dx, float Dy)>? SelectionMoved;

    /// <summary>Feuert nach dem Skalieren einer Auswahl (Faktor + Pivot) — für Undo+Speichern.</summary>
    public event EventHandler<(List<WbElement> Els, float Factor, float Px, float Py)>? SelectionScaled;

    /// <summary>Läuft gerade eine Skalierung über einen Eckgriff?</summary>
    private bool _scaling;
    private SKPoint _scalePivot;        // gegenüberliegende Ecke (bleibt fest)
    private float _scaleStartDist;      // Abstand Pivot→Zeiger bei Beginn
    private float _scaleTotal = 1f;     // aufsummierter Faktor für einen Undo-Schritt

    /// <summary>Kantenlänge der Skalier-Griffe (Bildschirm-Pixel).</summary>
    private const float HandleSize = 10f;

    /// <summary>Abstand des Dreh-Griffs über der Auswahl (Bildschirm-Pixel).</summary>
    private const float RotateHandleGap = 28f;

    /// <summary>Feuert nach dem Drehen eines Elements (alt/neu in Grad) — für Undo+Speichern.</summary>
    public event EventHandler<(WbElement El, float OldDeg, float NewDeg)>? ElementRotated;

    private bool _rotating;
    private WbElement? _rotateTarget;
    private SKPoint _rotateCenter;
    private float _rotateStartDeg;      // Winkel des Zeigers bei Beginn
    private float _rotateOrigDeg;       // ursprüngliche Rotation des Elements

    /// <summary>Feuert, wenn Text eingegeben werden soll (die Shell öffnet dafür einen Dialog).</summary>
    public event EventHandler<GonkNote.Models.TextElement>? TextRequested;

    static WhiteboardCanvas()
    {
        // Neu zeichnen, wenn sich Seite, Zoom oder Verschiebung ändern.
        AffectsRender<WhiteboardCanvas>(PageProperty, ZoomProperty, PanXProperty, PanYProperty);
    }

    public WhiteboardCanvas()
    {
        // Zeigereignisse kommen über die HitTest-Methode der Draw-Operation (füllt Bounds).
        Focusable = true;

        // Touch: Pinch zum Zoomen (zwei Finger).
        Gestures.AddPinchHandler(this, (_, e) =>
        {
            Zoom = Math.Clamp(_pinchStart * e.Scale, 0.2, 8.0);
            e.Handled = true;
        });
        Gestures.AddPinchEndedHandler(this, (_, _) => _pinchStart = Zoom);
    }

    /// <summary>Zoom zu Beginn einer Pinch-Geste (Skalierung ist relativ dazu).</summary>
    private double _pinchStart = 1.0;

    public override void Render(DrawingContext context)
    {
        // Transparente Füllung macht das Control zuverlässig hit-testbar (sonst kommen
        // je nach Avalonia-Version keine Pointer-Events an).
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

        if (Page is not { } page) return;
        context.Custom(new PageDrawOperation(new Rect(Bounds.Size), page, Zoom, _active, PanX, PanY,
                                             _shape, _lasso, SelectionBounds(),
                                             _selection.Count == 1 ? RotateHandlePos() : null));
    }

    // ---- Zeigereingabe (Stift / Maus / Finger) ------------------------------------------

    // ---- Finger-Gesten ------------------------------------------------------------------
    // Rohe Touch-Kontakte werden selbst verfolgt (wie in der WPF-App): 1 Finger schiebt die
    // Leinwand, 2 Finger zoomen+schieben, 3 Finger tippen macht rückgängig. Stift und Maus
    // laufen unverändert durch die Werkzeuglogik.

    private readonly Dictionary<int, Point> _touches = new();
    private int _maxTouches;            // höchste gleichzeitige Fingerzahl der Geste
    private double _gestureStartDist;
    private double _gestureStartZoom;
    private Point _gestureStartMid;
    private double _gestureOriginX, _gestureOriginY;

    /// <summary>Rückgängig per 3-Finger-Tipp (die Shell hängt sich hier ein).</summary>
    public event EventHandler? UndoRequested;

    private bool IsTouch(PointerEventArgs e) => e.Pointer.Type == PointerType.Touch;

    private void BeginGesture()
    {
        if (_touches.Count < 2) return;
        var pts = _touches.Values.ToList();
        _gestureStartDist = Math.Max(Distance(pts[0], pts[1]), 1);
        _gestureStartZoom = Zoom;
        _gestureStartMid = Mid(pts[0], pts[1]);
        _gestureOriginX = PanX;
        _gestureOriginY = PanY;
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Point Mid(Point a, Point b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Page is null) return;

        var pt = e.GetCurrentPoint(this);

        if (IsTouch(e))
        {
            _touches[e.Pointer.Id] = pt.Position;
            _maxTouches = Math.Max(_maxTouches, _touches.Count);

            // Ab dem zweiten Finger: laufende Zeichnung abbrechen, Geste übernimmt.
            if (_touches.Count >= 2)
            {
                _active = null;
                _eraseSteps = null;
                _lasso = null;
                _shape = null;
                _movingSelection = _scaling = _rotating = false;
                BeginGesture();
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return;
            }

            // Ein Finger schiebt die Leinwand (Zeichnen bleibt dem Stift vorbehalten).
            _panning = true;
            _panStart = pt.Position;
            _panOriginX = PanX;
            _panOriginY = PanY;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        // Pan: Hand-Werkzeug oder mittlere Maustaste
        if (Tool == ToolType.Pan || pt.Properties.IsMiddleButtonPressed)
        {
            _panning = true;
            _panStart = pt.Position;
            _panOriginX = PanX;
            _panOriginY = PanY;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!pt.Properties.IsLeftButtonPressed) return;   // Stiftspitze bzw. linke Maustaste

        if (Tool == ToolType.Eraser)
        {
            _eraseSteps = new List<EraseStep>();
            EraseAt(ToPage(pt.Position));
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        var page0 = Page!;
        var hit = ToPage(pt.Position);

        // ---- Dreh-Griff (nur bei Einzelauswahl) ----
        if ((Tool == ToolType.Move || Tool == ToolType.Lasso) &&
            _selection.Count == 1 && IsOnRotateHandle(hit))
        {
            _rotating = true;
            _rotateTarget = _selection[0];
            var rb = WbRenderer.ElementBounds(_rotateTarget);
            _rotateCenter = new SKPoint(rb.MidX, rb.MidY);
            _rotateOrigDeg = _rotateTarget.Rotation;
            _rotateStartDeg = AngleDeg(_rotateCenter, hit);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        // ---- Skalier-Griff der Auswahl (hat Vorrang vor allem anderen) ----
        if ((Tool == ToolType.Move || Tool == ToolType.Lasso) && HandleAt(hit) is { } pivot)
        {
            _scaling = true;
            _scalePivot = pivot;
            _scaleStartDist = Math.Max(Dist(pivot, hit), 1f);
            _scaleTotal = 1f;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        // ---- Auswahl / Verschieben ----
        if (Tool == ToolType.Move)
        {
            var el = TopElementAt(hit);
            if (el is null) _selection.Clear();
            else if (!_selection.Contains(el)) { _selection.Clear(); _selection.Add(el); }

            if (_selection.Count > 0)
            {
                _movingSelection = true;
                _moveLast = hit;
                _moveDx = _moveDy = 0f;
                e.Pointer.Capture(this);
            }
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        // ---- Lasso ----
        if (Tool == ToolType.Lasso)
        {
            // Klick in eine bestehende Auswahl ⇒ verschieben, sonst neues Lasso
            if (_selection.Count > 0 && SelectionBounds() is { } sb && sb.Contains(hit.X, hit.Y))
            {
                _movingSelection = true;
                _moveLast = hit;
                _moveDx = _moveDy = 0f;
            }
            else
            {
                _selection.Clear();
                _lasso = new List<SKPoint> { hit };
            }
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        // ---- Formen ----
        if (Tool == ToolType.Shape)
        {
            _shape = new ShapeElement
            {
                Shape = Shape,
                X1 = hit.X, Y1 = hit.Y, X2 = hit.X, Y2 = hit.Y,
                Color = InkColor,
                StrokeWidth = (float)InkWidth,
            };
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        // ---- Notizzettel ----
        if (Tool == ToolType.Sticky)
        {
            var note = new StickyNoteElement
            {
                X = hit.X, Y = hit.Y, Width = 200f, Height = 160f,
                Color = "#FDE68A", TextColor = "#3F3F46", FontSize = 15f,
                Text = "",
            };
            page0.Elements.Add(note);
            ElementAdded?.Invoke(this, note);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        // ---- Text ----
        if (Tool == ToolType.Text)
        {
            var txt = new GonkNote.Models.TextElement
            {
                X = hit.X, Y = hit.Y, Text = "", Color = InkColor, FontSize = 22f,
            };
            TextRequested?.Invoke(this, txt);   // Shell fragt den Text ab und fügt ihn ein
            e.Handled = true;
            return;
        }

        _active = new StrokeElement
        {
            Color = InkColor,
            Width = Tool == ToolType.Highlighter ? Math.Max((float)InkWidth * 5f, 10f) : (float)InkWidth,
            Kind = Tool switch
            {
                ToolType.Pencil => StrokeKind.Pencil,
                ToolType.Highlighter => StrokeKind.Highlighter,
                _ => StrokeKind.Pen,
            },
        };
        AddPoint(pt);
        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);

        if (IsTouch(e) && _touches.ContainsKey(e.Pointer.Id))
        {
            _touches[e.Pointer.Id] = pt.Position;

            if (_touches.Count >= 2)
            {
                var pts = _touches.Values.ToList();
                double dist = Math.Max(Distance(pts[0], pts[1]), 1);
                var mid = Mid(pts[0], pts[1]);

                // Zoom um die Fingermitte, zusätzlich der Versatz der Mitte = Zwei-Finger-Pan
                double newZoom = Math.Clamp(_gestureStartZoom * (dist / _gestureStartDist), 0.2, 8.0);
                double k = newZoom / _gestureStartZoom;
                Zoom = newZoom;
                PanX = mid.X - (_gestureStartMid.X - _gestureOriginX) * k;
                PanY = mid.Y - (_gestureStartMid.Y - _gestureOriginY) * k;

                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        if (_panning)
        {
            PanX = _panOriginX + (pt.Position.X - _panStart.X);
            PanY = _panOriginY + (pt.Position.Y - _panStart.Y);
            e.Handled = true;
            return;
        }

        if (_eraseSteps is not null)
        {
            EraseAt(ToPage(pt.Position));
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (_rotating && _rotateTarget is { } rt)
        {
            var now = ToPage(pt.Position);
            float delta = AngleDeg(_rotateCenter, now) - _rotateStartDeg;
            float deg = _rotateOrigDeg + delta;
            // Shift rastet auf 15°-Schritte (wie die Zeichenhilfen der WPF-App)
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                deg = MathF.Round(deg / 15f) * 15f;
            rt.Rotation = deg;
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (_scaling)
        {
            var now = ToPage(pt.Position);
            float f = Math.Max(Dist(_scalePivot, now), 1f) / _scaleStartDist;
            f = Math.Clamp(f, 0.05f, 20f);
            // relativ zum bereits angewandten Faktor skalieren
            float step = f / _scaleTotal;
            foreach (var el in _selection) el.Scale(step, _scalePivot.X, _scalePivot.Y);
            _scaleTotal = f;
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (_movingSelection)
        {
            var now = ToPage(pt.Position);
            float dx = now.X - _moveLast.X, dy = now.Y - _moveLast.Y;
            foreach (var el in _selection) el.Translate(dx, dy);
            _moveDx += dx; _moveDy += dy;
            _moveLast = now;
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (_lasso is not null)
        {
            _lasso.Add(ToPage(pt.Position));
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (_shape is not null)
        {
            var now = ToPage(pt.Position);
            _shape.X2 = now.X; _shape.Y2 = now.Y;
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (_active is null) return;
        AddPoint(pt);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (IsTouch(e) && _touches.Remove(e.Pointer.Id))
        {
            // Drei gleichzeitige Finger, kurz getippt ⇒ rückgängig (wie in der WPF-App).
            if (_maxTouches >= 3)
            {
                if (_touches.Count == 0)
                {
                    _maxTouches = 0;
                    UndoRequested?.Invoke(this, EventArgs.Empty);
                }
                _panning = false;
                e.Pointer.Capture(null);
                e.Handled = true;
                return;
            }

            if (_touches.Count == 1) BeginGesture();     // zurück auf einen Finger
            if (_touches.Count == 0)
            {
                _maxTouches = 0;
                _panning = false;
            }
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (_panning)
        {
            _panning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (_eraseSteps is { } steps)
        {
            _eraseSteps = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            if (steps.Count > 0) ElementsErased?.Invoke(this, steps);
            InvalidateVisual();
            return;
        }

        if (_rotating)
        {
            _rotating = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            if (_rotateTarget is { } rt && Math.Abs(rt.Rotation - _rotateOrigDeg) > 0.01f)
                ElementRotated?.Invoke(this, (rt, _rotateOrigDeg, rt.Rotation));
            _rotateTarget = null;
            InvalidateVisual();
            return;
        }

        if (_scaling)
        {
            _scaling = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            if (_selection.Count > 0 && Math.Abs(_scaleTotal - 1f) > 0.001f)
                SelectionScaled?.Invoke(this,
                    (new List<WbElement>(_selection), _scaleTotal, _scalePivot.X, _scalePivot.Y));
            InvalidateVisual();
            return;
        }

        if (_movingSelection)
        {
            _movingSelection = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            if (_selection.Count > 0 && (Math.Abs(_moveDx) > 0.01f || Math.Abs(_moveDy) > 0.01f))
                SelectionMoved?.Invoke(this, (new List<WbElement>(_selection), _moveDx, _moveDy));
            InvalidateVisual();
            return;
        }

        if (_lasso is { } poly)
        {
            _lasso = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            SelectInsideLasso(poly);
            InvalidateVisual();
            return;
        }

        if (_shape is { } sh)
        {
            _shape = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            // Winzige Formen (Fehlklick) verwerfen
            if (Page is { } pg && (Math.Abs(sh.X2 - sh.X1) > 2f || Math.Abs(sh.Y2 - sh.Y1) > 2f))
            {
                pg.Elements.Add(sh);
                ElementAdded?.Invoke(this, sh);
            }
            InvalidateVisual();
            return;
        }

        if (_active is null) return;

        AddPoint(e.GetCurrentPoint(this));
        var finished = _active;
        _active = null;
        e.Pointer.Capture(null);
        e.Handled = true;

        // Sehr kurze „Tupfer" trotzdem übernehmen (ergibt einen Punkt, s. WbRenderer).
        if (finished.Points.Count > 0 && Page is { } page)
        {
            if (finished.Points.Count == 1) finished.Points.Add(finished.Points[0]);
            page.Elements.Add(finished);
            StrokeCompleted?.Invoke(this, finished);
        }
        InvalidateVisual();
    }

    /// <summary>Mausrad zoomt (0,2×–8×).</summary>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double z = Zoom * (e.Delta.Y > 0 ? 1.1 : 1 / 1.1);
        Zoom = Math.Clamp(z, 0.2, 8.0);
        e.Handled = true;
    }

    /// <summary>Bildschirm- in Seitenkoordinaten (Pan + Zoom herausrechnen).</summary>
    private SKPoint ToPage(Point p) =>
        new((float)((p.X - PanX) / Zoom), (float)((p.Y - PanY) / Zoom));

    // ---- Auswahl -------------------------------------------------------------------------

    /// <summary>Oberstes Element unter dem Punkt (Vordergrund zuerst).</summary>
    private WbElement? TopElementAt(SKPoint c)
    {
        if (Page is not { } page) return null;
        float tol = 5f / (float)Zoom;
        for (int i = page.Elements.Count - 1; i >= 0; i--)
        {
            var el = page.Elements[i];
            bool hit = el is StrokeElement s
                ? WbErase.HitsStroke(s, c, tol)
                : WbErase.HitsOther(el, c, tol);
            if (hit) return el;
        }
        return null;
    }

    /// <summary>Wählt alle Elemente, deren Mittelpunkt im Lasso-Polygon liegt.</summary>
    private void SelectInsideLasso(List<SKPoint> poly)
    {
        _selection.Clear();
        if (Page is not { } page || poly.Count < 3) return;

        foreach (var el in page.Elements)
        {
            var b = WbRenderer.ElementBounds(el);
            if (PointInPolygon(poly, b.MidX, b.MidY)) _selection.Add(el);
        }
    }

    private static bool PointInPolygon(List<SKPoint> poly, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            if (poly[i].Y > y != poly[j].Y > y &&
                x < (poly[j].X - poly[i].X) * (y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>Umschließendes Rechteck der Auswahl (null, wenn nichts gewählt ist).</summary>
    private SKRect? SelectionBounds()
    {
        if (_selection.Count == 0) return null;
        var r = WbRenderer.ElementBounds(_selection[0]);
        for (int i = 1; i < _selection.Count; i++)
            r.Union(WbRenderer.ElementBounds(_selection[i]));
        return r;
    }

    /// <summary>Löscht die aktuelle Auswahl (Entf-Taste).</summary>
    public List<(WbElement El, int Index)> DeleteSelection()
    {
        var removed = new List<(WbElement, int)>();
        if (Page is not { } page || _selection.Count == 0) return removed;

        foreach (var el in _selection)
        {
            int idx = page.Elements.IndexOf(el);
            if (idx >= 0) { removed.Add((el, idx)); page.Elements.RemoveAt(idx); }
        }
        _selection.Clear();
        InvalidateVisual();
        return removed;
    }

    public bool HasSelection => _selection.Count > 0;

    /// <summary>Setzt die Auswahl programmatisch (z. B. nach dem Einfügen eines Bildes).</summary>
    public void SelectOnly(WbElement el)
    {
        _selection.Clear();
        _selection.Add(el);
        InvalidateVisual();
    }

    private static float Dist(SKPoint a, SKPoint b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Liegt der Punkt auf einem der vier Eckgriffe? Liefert dann die **gegenüberliegende**
    /// Ecke als Pivot (die beim Skalieren fest bleibt), sonst null.
    /// </summary>
    private SKPoint? HandleAt(SKPoint p)
    {
        if (SelectionBounds() is not { } b) return null;

        float pad = 4f / (float)Zoom;
        b.Inflate(pad, pad);
        float r = HandleSize / (float)Zoom;   // Griffe bleiben bildschirmgroß

        var corners = new (SKPoint Handle, SKPoint Pivot)[]
        {
            (new SKPoint(b.Left,  b.Top),    new SKPoint(b.Right, b.Bottom)),
            (new SKPoint(b.Right, b.Top),    new SKPoint(b.Left,  b.Bottom)),
            (new SKPoint(b.Left,  b.Bottom), new SKPoint(b.Right, b.Top)),
            (new SKPoint(b.Right, b.Bottom), new SKPoint(b.Left,  b.Top)),
        };
        foreach (var (handle, pivot) in corners)
            if (Dist(handle, p) <= r) return pivot;
        return null;
    }

    private static float AngleDeg(SKPoint center, SKPoint p) =>
        MathF.Atan2(p.Y - center.Y, p.X - center.X) * 180f / MathF.PI;

    /// <summary>Position des Dreh-Griffs (mittig über dem Auswahlrahmen), null ohne Auswahl.</summary>
    private SKPoint? RotateHandlePos()
    {
        if (SelectionBounds() is not { } b) return null;
        float pad = 4f / (float)Zoom;
        b.Inflate(pad, pad);
        return new SKPoint(b.MidX, b.Top - RotateHandleGap / (float)Zoom);
    }

    private bool IsOnRotateHandle(SKPoint p) =>
        RotateHandlePos() is { } h && Dist(h, p) <= HandleSize / (float)Zoom;

    // ---- Zwischenablage (intern, wie in der WPF-App) -------------------------------------

    private static readonly List<WbElement> Clipboard = new();

    /// <summary>Kopiert die Auswahl in die interne Zwischenablage.</summary>
    public void CopySelection()
    {
        if (_selection.Count == 0) return;
        Clipboard.Clear();
        foreach (var el in _selection) Clipboard.Add(CloneElement(el));
    }

    /// <summary>Fügt die Zwischenablage leicht versetzt ein und wählt das Eingefügte aus.</summary>
    public List<WbElement> Paste()
    {
        var added = new List<WbElement>();
        if (Page is not { } page || Clipboard.Count == 0) return added;

        const float offset = 18f;
        _selection.Clear();
        foreach (var el in Clipboard)
        {
            var copy = CloneElement(el);
            copy.Translate(offset, offset);
            page.Elements.Add(copy);
            _selection.Add(copy);
            added.Add(copy);
        }
        // Mehrfaches Einfügen versetzt weiter
        foreach (var el in Clipboard) el.Translate(offset, offset);
        InvalidateVisual();
        return added;
    }

    /// <summary>Kopiert die Auswahl und entfernt sie (Ausschneiden).</summary>
    public List<(WbElement El, int Index)> CutSelection()
    {
        CopySelection();
        return DeleteSelection();
    }

    /// <summary>Dupliziert die Auswahl direkt (Kopieren + Einfügen in einem Schritt).</summary>
    public List<WbElement> DuplicateSelection()
    {
        if (_selection.Count == 0) return new List<WbElement>();
        CopySelection();
        return Paste();
    }

    /// <summary>Tiefe Kopie eines Elements (neue Id, damit der Bild-Cache sauber bleibt).</summary>
    private static WbElement CloneElement(WbElement el) => el switch
    {
        StrokeElement s => new StrokeElement
        {
            Points = new List<WbPoint>(s.Points),
            Color = s.Color, Width = s.Width, Kind = s.Kind, Rotation = s.Rotation,
        },
        ShapeElement sh => new ShapeElement
        {
            Shape = sh.Shape, X1 = sh.X1, Y1 = sh.Y1, X2 = sh.X2, Y2 = sh.Y2,
            Color = sh.Color, StrokeWidth = sh.StrokeWidth, Fill = sh.Fill, Rotation = sh.Rotation,
        },
        GonkNote.Models.TextElement t => new GonkNote.Models.TextElement
        {
            X = t.X, Y = t.Y, Text = t.Text, Color = t.Color, FontSize = t.FontSize,
            Background = t.Background, FontFamily = t.FontFamily, Rotation = t.Rotation,
        },
        ImageElement im => new ImageElement
        {
            X = im.X, Y = im.Y, Width = im.Width, Height = im.Height,
            Data = im.Data, Rotation = im.Rotation,
        },
        StickyNoteElement sn => new StickyNoteElement
        {
            X = sn.X, Y = sn.Y, Width = sn.Width, Height = sn.Height,
            Text = sn.Text, Color = sn.Color, TextColor = sn.TextColor,
            FontSize = sn.FontSize, FontFamily = sn.FontFamily, Rotation = sn.Rotation,
        },
        _ => el,
    };

    /// <summary>Fügt ein Bild ein (skaliert auf eine sinnvolle Anzeigegröße) und wählt es aus.</summary>
    public ImageElement? InsertImage(byte[] data, SKPoint at)
    {
        if (Page is not { } page || data.Length == 0) return null;

        float w = 300f, h = 200f;
        using (var bmp = SKBitmap.Decode(data))
        {
            if (bmp is null) return null;
            float max = 340f;
            float scale = Math.Min(max / bmp.Width, max / bmp.Height);
            if (scale > 1f) scale = 1f;
            w = bmp.Width * scale;
            h = bmp.Height * scale;
        }

        var img = new ImageElement { X = at.X, Y = at.Y, Width = w, Height = h, Data = data };
        page.Elements.Add(img);
        _selection.Clear();
        _selection.Add(img);
        InvalidateVisual();
        return img;
    }

    /// <summary>Mitte des sichtbaren Bereichs in Seiten-Koordinaten (Standard-Einfügeort).</summary>
    public SKPoint ViewCenter() =>
        ToPage(new Point(Bounds.Width / 2, Bounds.Height / 2));

    /// <summary>
    /// **Punktgenaues** Radieren wie in der WPF-App: Striche werden an der berührten Stelle
    /// aufgetrennt, die Reststücke bleiben stehen. Formen/Text/Zettel gehen als Ganzes,
    /// Bilder sind nicht radierbar (per Lasso löschen).
    /// </summary>
    private void EraseAt(SKPoint c)
    {
        if (Page is not { } page || _eraseSteps is null) return;

        float r = 14f / (float)Zoom;    // Radiergummi-Radius in Seiten-Koordinaten

        for (int i = page.Elements.Count - 1; i >= 0; i--)
        {
            var el = page.Elements[i];
            switch (el)
            {
                case StrokeElement s:
                {
                    if (!WbErase.HitsStroke(s, c, r)) break;
                    var parts = WbErase.SplitStroke(s, c, r + s.Width / 2f);
                    page.Elements.RemoveAt(i);
                    page.Elements.InsertRange(i, parts);
                    _eraseSteps.Add(new EraseStep(s, i, parts));
                    break;
                }

                case ShapeElement or GonkNote.Models.TextElement or StickyNoteElement:
                    if (!WbErase.HitsOther(el, c, r)) break;
                    page.Elements.RemoveAt(i);
                    _eraseSteps.Add(new EraseStep(el, i, new List<WbElement>()));
                    break;

                // Bilder/PDF-Seiten bleiben stehen (wie in der WPF-App)
            }
        }
    }

    /// <summary>Übernimmt einen Zeigerpunkt inkl. Druckstärke (Maus/Finger ohne Druck ⇒ 0,5).</summary>
    private void AddPoint(PointerPoint pt)
    {
        if (_active is null) return;
        float pressure = pt.Properties.Pressure;
        if (pressure <= 0f || float.IsNaN(pressure)) pressure = 0.5f;   // kein Drucksensor
        var p = ToPage(pt.Position);
        _active.Points.Add(new WbPoint { X = p.X, Y = p.Y, P = pressure });
    }

    /// <summary>Custom-Draw-Operation: leiht sich Avalonias SKCanvas und nutzt den Core-Renderer.</summary>
    private sealed class PageDrawOperation : ICustomDrawOperation
    {
        private readonly WbPage _page;
        private readonly double _zoom;
        private readonly StrokeElement? _active;
        private readonly double _panX, _panY;
        private readonly ShapeElement? _shape;
        private readonly List<SKPoint>? _lasso;
        private readonly SKRect? _selBounds;
        private readonly SKPoint? _rotateHandle;

        public PageDrawOperation(Rect bounds, WbPage page, double zoom, StrokeElement? active,
                                 double panX, double panY, ShapeElement? shape,
                                 List<SKPoint>? lasso, SKRect? selBounds, SKPoint? rotateHandle)
        {
            _rotateHandle = rotateHandle;
            Bounds = bounds;
            _page = page;
            _zoom = zoom;
            _active = active;
            _panX = panX;
            _panY = panY;
            _shape = shape;
            _lasso = lasso;
            _selBounds = selBounds;
        }

        public Rect Bounds { get; }
        public bool HitTest(Point p) => Bounds.Contains(p);
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            // Avalonia 11.0.x: TryGetFeature ist nicht generisch und liefert object.
            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature lease)
                return;                                     // kein Skia-Backend → nichts zeichnen
            using var api = lease.Lease();
            var canvas = api.SkCanvas;

            canvas.Save();
            // WICHTIG: auf die Control-Fläche begrenzen. canvas.Clear() würde die *gesamte*
            // Fensteroberfläche löschen (überdeckte anfangs Baum und Kopfleiste).
            canvas.ClipRect(SKRect.Create(0, 0, (float)Bounds.Width, (float)Bounds.Height));
            canvas.Translate((float)_panX, (float)_panY);
            canvas.Scale((float)_zoom);
            DrawPage(canvas);
            canvas.Restore();
        }

        private void DrawPage(SKCanvas canvas)
        {
            bool dark = _page.Shade == PageShade.Dark;
            var paper = dark ? new SKColor(0x24, 0x28, 0x30) : SKColors.White;

            // Sichtbarer Bereich in Seiten-Koordinaten (Pan + Zoom herausgerechnet)
            var view = SKRect.Create(
                (float)(-_panX / _zoom), (float)(-_panY / _zoom),
                (float)(Bounds.Width / _zoom), (float)(Bounds.Height / _zoom));

            // Seitenfläche (unendliche Whiteboards haben keine Seitengrenzen)
            SKRect pageRect;
            if (_page.IsInfinite)
            {
                pageRect = view;
                using var bgp = new SKPaint { Color = paper };
                canvas.DrawRect(pageRect, bgp);
            }
            else
            {
                using var desk = new SKPaint
                {
                    Color = dark ? new SKColor(0x18, 0x1A, 0x20) : new SKColor(0xEE, 0xF1, 0xF6),
                };
                canvas.DrawRect(view, desk);

                pageRect = SKRect.Create(0, 0, _page.Width, _page.Height);
                using var bg = new SKPaint { IsAntialias = true, Color = paper };
                canvas.DrawRect(pageRect, bg);
            }

            DrawPattern(canvas, pageRect, dark);

            // Pro Element abgesichert: ein defektes Element darf nicht das ganze Bild kosten
            // (Custom-DrawOperations verschlucken Ausnahmen sonst stillschweigend).
            foreach (var el in _page.Elements)
            {
                try { WbRenderer.DrawElement(canvas, el); }   // gemeinsame Routinen aus GonkNote.Core
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Render {el.GetType().Name}: {ex.Message}");
                }
            }

            // Laufender Strich (noch nicht Teil der Seite)
            if (_active is { Points.Count: > 0 })
                WbRenderer.DrawStroke(canvas, _active);

            // Vorschau der gerade aufgezogenen Form
            if (_shape is { } sh)
                WbRenderer.DrawShape(canvas, sh, sh.Color, sh.StrokeWidth);

            DrawOverlays(canvas);
        }

        /// <summary>Lasso-Spur und Auswahlrahmen (transient, nicht Teil der Seite).</summary>
        private void DrawOverlays(SKCanvas canvas)
        {
            float px = 1f / (float)_zoom;   // konstante Strichbreite unabhängig vom Zoom

            if (_lasso is { Count: > 1 })
            {
                using var path = new SKPath();
                path.MoveTo(_lasso[0]);
                for (int i = 1; i < _lasso.Count; i++) path.LineTo(_lasso[i]);
                using var dash = SKPathEffect.CreateDash(new[] { 6f * px, 4f * px }, 0);
                using var p = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.5f * px,
                    Color = new SKColor(0x5B, 0x21, 0xB6),
                    PathEffect = dash,
                };
                canvas.DrawPath(path, p);
            }

            if (_selBounds is { } b)
            {
                var r = b;
                r.Inflate(4f * px, 4f * px);
                var accent = new SKColor(0x5B, 0x21, 0xB6);

                using (var dash = SKPathEffect.CreateDash(new[] { 5f * px, 4f * px }, 0))
                using (var p = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.5f * px,
                    Color = accent,
                    PathEffect = dash,
                })
                    canvas.DrawRect(r, p);

                // Eckgriffe zum Skalieren (bildschirmgroß, unabhängig vom Zoom)
                float hs = 5f * px;
                using var fill = new SKPaint { IsAntialias = true, Color = SKColors.White };
                using var edge = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.5f * px,
                    Color = accent,
                };
                foreach (var c in new[]
                {
                    new SKPoint(r.Left, r.Top), new SKPoint(r.Right, r.Top),
                    new SKPoint(r.Left, r.Bottom), new SKPoint(r.Right, r.Bottom),
                })
                {
                    var h = SKRect.Create(c.X - hs, c.Y - hs, hs * 2, hs * 2);
                    canvas.DrawRect(h, fill);
                    canvas.DrawRect(h, edge);
                }

                // Dreh-Griff: runder Knauf mittig über der Auswahl, mit Verbindungslinie
                if (_rotateHandle is { } rh)
                {
                    canvas.DrawLine(r.MidX, r.Top, rh.X, rh.Y, edge);
                    canvas.DrawCircle(rh.X, rh.Y, hs * 1.15f, fill);
                    canvas.DrawCircle(rh.X, rh.Y, hs * 1.15f, edge);
                }
            }
        }

        /// <summary>Linien-/Raster-/Punktmuster der Seite (vereinfacht ggü. WPF, gleiche Optik).</summary>
        private void DrawPattern(SKCanvas canvas, SKRect r, bool dark)
        {
            if (_page.IsCover || _page.Background == PageBackground.Blank) return;

            var lineColor = dark ? new SKColor(0x35, 0x48, 0x6E) : new SKColor(0xBB, 0xD2, 0xF0);
            var dotColor = dark ? new SKColor(0x3A, 0x4A, 0x6B) : new SKColor(0xB8, 0xC6, 0xDC);
            const float step = 28f;

            // Am Seitenraster ausrichten (nicht am Viewport) — sonst wandert das Muster beim Pan.
            float x0 = MathF.Ceiling(r.Left / step) * step;
            float y0 = MathF.Ceiling(r.Top / step) * step;

            switch (_page.Background)
            {
                case PageBackground.Lines:
                {
                    using var p = new SKPaint { IsAntialias = true, Color = lineColor, StrokeWidth = 1 };
                    for (float y = y0; y < r.Bottom; y += step)
                        canvas.DrawLine(r.Left, y, r.Right, y, p);
                    break;
                }
                case PageBackground.Grid:
                {
                    using var p = new SKPaint { IsAntialias = true, Color = lineColor, StrokeWidth = 1 };
                    for (float y = y0; y < r.Bottom; y += step)
                        canvas.DrawLine(r.Left, y, r.Right, y, p);
                    for (float x = x0; x < r.Right; x += step)
                        canvas.DrawLine(x, r.Top, x, r.Bottom, p);
                    break;
                }
                case PageBackground.Dots:
                {
                    using var p = new SKPaint { IsAntialias = true, Color = dotColor };
                    for (float y = y0; y < r.Bottom; y += step)
                        for (float x = x0; x < r.Right; x += step)
                            canvas.DrawCircle(x, y, 1.6f, p);
                    break;
                }
            }
        }
    }
}
