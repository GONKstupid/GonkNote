using System.Windows;
using System.Windows.Input;
using GonkNote.Core.Models;
using GonkNote.Core.Editing;
using GonkNote.Core.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Eingabe: Stift, Maus, Finger-Gesten und Radierer.
/// Hier laeuft jeder Strich an - vom Aufsetzen bis zum Absetzen.
/// </summary>
public partial class WhiteboardView
{
    // ==================== Eingabe (Stylus + Maus) ====================

    /// <summary>
    /// Läuft gerade eine Aktion, die jede Zeigerbewegung braucht (zeichnen, radieren,
    /// Lasso ziehen, verschieben, skalieren, drehen, Form aufziehen, Zeichenhilfe schieben)?
    /// Wenn nicht, ist die Bewegung bloßes Schweben über der Fläche.
    /// </summary>
    private bool InputInProgress =>
        _drawing || _eraseSteps != null || _lassoPts != null || _movingSelection
        || _scalingSelection || _rotatingEl != null || _shapeActive || _rulerDrag != RulerDrag.None;

    private void OnStylusDown(object sender, StylusDownEventArgs e)
    {
        Focus();
        if (_vm == null || _page == null) return;
        if (IsOnQuickMenu(e.OriginalSource)) return;

        // Finger laufen über die Touch-Events (Gesten: Pan, Pinch-Zoom, Tipp-Gesten)
        if (e.StylusDevice.TabletDevice?.Type == TabletDeviceType.Touch) return;

        _stylusInverted = e.Inverted;
        var pts = e.GetStylusPoints(CanvasHost);
        if (pts.Count == 0) return;
        var p = pts[^1];

        // Zweite Stift-Taste (Barrel) gehalten → Schnellaktionen öffnen statt zeichnen
        if (IsBarrelPressed(p))
        {
            ShowQuickMenuAt(new Point(p.X, p.Y), autoPick: true);
            e.Handled = true;
            return;
        }

        BeginInput(ToCanvas(new Point(p.X, p.Y)), p.PressureFactor);
        CanvasHost.CaptureStylus();
        StartHoldDetect(new Point(p.X, p.Y), fromTouch: false);
        e.Handled = true;
    }

    private static bool IsBarrelPressed(StylusPoint p) =>
        p.HasProperty(StylusPointProperties.BarrelButton)
        && p.GetPropertyValue(StylusPointProperties.BarrelButton) != 0;

    private void OnStylusMove(object sender, StylusEventArgs e)
    {
        if (e.StylusDevice?.TabletDevice?.Type == TabletDeviceType.Touch) return;
        if (IsOnQuickMenu(e.OriginalSource)) return;   // Klicks auf die Quick-Leiste durchlassen
        MoveHoldDetect(e.GetPosition(CanvasHost));
        if (_panning)
        {
            MovePan(e.GetPosition(CanvasHost));
            e.Handled = true;
            return;
        }
        if (!InputInProgress)
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
        CancelHoldDetect();
        // Stift-Events auf der Quick-Leiste NICHT als „Handled" markieren – sonst
        // wird der Stift-Tipp nicht zum Maus-Klick promotet und der Button-Klick
        // (z. B. Kopieren) löst nie aus.
        if (IsOnQuickMenu(e.OriginalSource)) return;
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
        if (IsOnQuickMenu(e.OriginalSource)) return;

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

        if (e.LeftButton == MouseButtonState.Pressed && InputInProgress)
            MoveInput(ToCanvas(screen), 0.5f);
        else
            HoverInput(ToCanvas(screen));
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
        if (IsOnQuickMenu(e.OriginalSource)) return;   // Tipp auf die Quick-Leiste durchlassen
        CanvasHost.CaptureTouch(e.TouchDevice);
        var p = e.GetTouchPoint(CanvasHost).Position;
        _touches[e.TouchDevice.Id] = p;

        if (_touches.Count == 1)
        {
            _gestureStart = DateTime.UtcNow;
            _gestureMoved = false;
            _gestureMaxFingers = 1;
            StartHoldDetect(p, fromTouch: true);   // Langdruck = Schnellaktionen (L/V/H)
        }
        else
        {
            CancelHoldDetect();   // zweiter Finger = Pinch, kein Langdruck
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
        MoveHoldDetect(p);
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
        if (IsOnQuickMenu(e.OriginalSource)) return;
        CancelHoldDetect();
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
        HideQuickMenu();
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
        HideQuickMenu();
        CommitActiveEdit();

        // Zeichenhilfe bewegen/drehen hat Vorrang (außer beim Verschieben-Werkzeug)
        if (_tool != ToolType.Pan && TryBeginAid(c)) return;

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.SmoothPen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                BeginStroke(c, pressure);
                break;

            case ToolType.Eraser:
                _eraseSteps = new List<EraseStep>();
                _eraserPos = c;
                _eraserVisible = true;
                EraseAt(c);
                break;

            case ToolType.Lasso:
            case ToolType.Move:
                BeginSelectionDrag(c);
                break;

            case ToolType.Text:
                BeginTextInput(c);
                break;

            case ToolType.Shape:
                _shapeActive = true;
                _shapeStart = _shapeCur = c;
                break;

            case ToolType.Sticky:
                BeginStickyInput(c);
                break;

            case ToolType.Pan:
                // wird über BeginPan behandelt
                break;
        }
        Skia.InvalidateVisual();
    }

    /// <summary>Setzt einen Strich an – ggf. auf die Kante der Zeichenhilfe eingerastet.</summary>
    private void BeginStroke(SKPoint c, float pressure)
    {
        _drawing = true;
        TryActivateAidSnap(c);
        var start = ApplyAidSnap(c);
        _activePoints = new List<WbPoint> { new(start.X, start.Y, Math.Clamp(pressure, 0.05f, 1f)) };
    }

    /// <summary>
    /// Auswahl-Werkzeuge: Was unter dem Zeiger liegt, entscheidet – Dreh-Griff, Skalier-Griff,
    /// die Auswahl selbst (verschieben) oder freie Fläche (neue Auswahl beginnen).
    /// </summary>
    private void BeginSelectionDrag(SKPoint c)
    {
        var single = SingleSelected;

        // Der Drehpunkt hängt davon ab, was ausgewählt ist: bei einem Element sein
        // Mittelpunkt (es dreht sich um sich selbst), bei mehreren die obere linke Ecke des
        // Kastens (die Gruppe wächst nach unten rechts).
        switch (ProbeHandles(c))
        {
            case WbHandles.Grab.Rotate when single != null:
                BeginRotate(single, WbHandles.Center(single), c);
                return;
            case WbHandles.Grab.Scale:
                BeginScale(single != null
                    ? WbHandles.Center(single)
                    : new SKPoint(_selectionBounds.Left, _selectionBounds.Top), c);
                return;
            case WbHandles.Grab.Move:
                BeginMove(c);
                return;
            default:
                BeginSelectOrLasso(c);
                return;
        }
    }

    private void BeginRotate(WbElement el, SKPoint center, SKPoint c)
    {
        _rotatingEl = el;
        _rotStartDeg = el.Rotation;
        _rotStartPointer = WbHandles.AngleDeg(center, c);
    }

    private void BeginScale(SKPoint pivot, SKPoint c)
    {
        _scalingSelection = true;
        _scalePivot = pivot;
        _scaleStartDist = Math.Max(1f, SKPoint.Distance(pivot, c));
        _scaleAccum = 1f;
    }

    private void BeginMove(SKPoint c)
    {
        _movingSelection = true;
        _moveLast = c;
        _movedX = _movedY = 0;
    }

    /// <summary>Textfeld unter dem Zeiger bearbeiten – sonst ein neues anlegen.</summary>
    private void BeginTextInput(SKPoint c)
    {
        var hit = _page!.Elements.OfType<TextElement>().LastOrDefault(t => TextBounds(t).Contains(c));
        if (hit != null)
        {
            StartTextEdit(hit, isNew: false);
            return;
        }

        StartTextEdit(new TextElement
        {
            X = c.X, Y = c.Y,
            Color = EnsureReadableTextColor(CurrentInkHex(), _textBgHex),
            FontSize = 18f,
            Background = _textBgHex,
            FontFamily = _textFont,
        }, isNew: true);
    }

    /// <summary>Notizzettel unter dem Zeiger bearbeiten – sonst einen neuen anlegen.</summary>
    private void BeginStickyInput(SKPoint c)
    {
        var hit = _page!.Elements.OfType<StickyNoteElement>()
            .LastOrDefault(s => SKRect.Create(s.X, s.Y, s.Width, s.Height).Contains(c));
        if (hit != null)
        {
            StartStickyEdit(hit, isNew: false);
            return;
        }

        // Neuen Zettel mittig unter dem Zeiger anlegen und gleich beschriften
        StartStickyEdit(new StickyNoteElement
        {
            X = c.X - 100f, Y = c.Y - 100f,
            Color = _stickyColorHex,
            TextColor = ReadableStickyTextColor(_stickyColorHex),
        }, isNew: true);
    }

    private void MoveInput(SKPoint c, float pressure)
    {
        if (_page == null) return;
        if (_rulerDrag != RulerDrag.None) { UpdateAidDrag(c); return; }

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.SmoothPen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                if (!_drawing || _activePoints == null) return;
                c = ApplyAidSnap(c);
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
            case ToolType.Move:
                if (_rotatingEl != null)
                {
                    // Einrasten auf 15°-Schritte inbegriffen — die Rechnung steht in Core.
                    _rotatingEl.Rotation = WbHandles.RotationFromDrag(
                        WbHandles.Center(_rotatingEl), c, _rotStartDeg, _rotStartPointer);
                }
                else if (_scalingSelection)
                {
                    float dist = SKPoint.Distance(_scalePivot, c);
                    float target = Math.Max(0.05f, dist / _scaleStartDist);   // Gesamtfaktor seit Anfassen
                    float step = target / _scaleAccum;                         // relativer Schritt
                    if (step > 0.001f && MathF.Abs(step - 1f) > 0.0001f)
                    {
                        foreach (var el in _selection) el.Scale(step, _scalePivot.X, _scalePivot.Y);
                        _scaleAccum = target;
                        ComputeSelectionBounds();
                    }
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

        // Loslassen nach einem Langdruck (Schnellaktionen sind bereits offen)
        if (_suppressNextEndInput) { _suppressNextEndInput = false; return; }

        if (_rulerDrag != RulerDrag.None) { _rulerDrag = RulerDrag.None; Skia.InvalidateVisual(); return; }

        // Ob diese Eingabe zu einer frischen Auswahl (Tipp/Lasso, ohne Verschieben)
        // geführt hat → danach die Schnellaktionen an der Auswahl einblenden.
        bool freshSelect = false;

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
            case ToolType.Move:
                freshSelect = EndSelectionDrag();
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

        if (freshSelect) ShowQuickMenuForSelection();
    }

    /// <summary>
    /// Schließt Drehen, Skalieren, Verschieben oder Lasso ab und legt den Undo-Schritt an.
    /// Rückgabe: true, wenn dabei eine frische Auswahl entstanden ist – dann blendet der
    /// Aufrufer die Schnellaktionen ein.
    /// </summary>
    private bool EndSelectionDrag()
    {
        if (_rotatingEl != null)
        {
            var el = _rotatingEl;
            _rotatingEl = null;
            if (Math.Abs(el.Rotation - _rotStartDeg) > 0.01f)
            {
                _vm!.Undo.Push(_page!, new RotateElementAction(el, _rotStartDeg, el.Rotation));
                MarkDirty();
            }
            return false;
        }

        if (_scalingSelection)
        {
            _scalingSelection = false;
            if (Math.Abs(_scaleAccum - 1f) > 0.001f)
            {
                _vm!.Undo.Push(_page!,
                    new ScaleElementsAction(_selection, _scaleAccum, _scalePivot.X, _scalePivot.Y));
                MarkDirty();
            }
            ComputeSelectionBounds();
            return false;
        }

        if (_movingSelection)
        {
            _movingSelection = false;
            if (Math.Abs(_movedX) <= 0.01f && Math.Abs(_movedY) <= 0.01f)
                return _selection.Count > 0;   // reiner Tipp aufs Element = Auswahl

            _vm!.Undo.Push(_page!, new MoveElementsAction(_selection, _movedX, _movedY));
            MarkDirty();
            return false;
        }

        if (_lassoPts is { Count: > 2 })
        {
            SelectByLasso(_lassoPts);
            _lassoPts = null;
            return _selection.Count > 0;
        }

        _lassoPts = null;
        return false;
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

    // ==================== Radierer ====================

    private void EraseAt(SKPoint c)
    {
        if (_page == null || _eraseSteps == null) return;
        float r = _eraserRadius / Zoom;

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
    private static List<WbElement> SplitStroke(StrokeElement s, SKPoint c, float rr) =>
        WbErase.SplitStroke(s, c, rr);
}
