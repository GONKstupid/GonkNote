using Avalonia;
using Avalonia.Input;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Eingabe: Stift, Maus und Finger. Hier läuft jeder Strich an — vom Aufsetzen bis zum
/// Absetzen.
///
/// <para>
/// <b>Der Eingabepfad ist gegen die Fähigkeiten des Geräts geschrieben, nicht gegen das
/// Gerät auf diesem Laptop.</b> Das ist eine Anforderung und keine Nebensache (HANDOFF §1):
/// Die App soll mit jedem Stylus laufen. Gemessen ist bisher genau eine Geräteklasse — ein
/// Wacom-AES-Digitizer (§5a) —, und MPP und EMR sind weiterhin ungetestet. Deshalb fragt
/// keine Stelle hier „ist das ein Wacom", sondern nur: kommt Druck an, kommt Neigung an,
/// meldet sich der Zeiger als Stift oder als Finger.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>
    /// Läuft gerade eine Aktion, die jede Zeigerbewegung braucht? Wenn nicht, ist die
    /// Bewegung bloßes Schweben über der Fläche.
    /// <para>
    /// <b>Wer hier einen Zug vergisst, bekommt keinen Fehler, sondern gar nichts.</b> Das
    /// Drehen und Skalieren aus Phase 4.5 stand zuerst nicht in dieser Liste: der Griff
    /// wurde erkannt, der Zustand gesetzt — und jede folgende Bewegung galt als Schweben,
    /// also passierte nichts. Es baute mit 0 Fehlern, die Wächter blieben grün, und am
    /// laufenden Programm bewegte sich das Element keinen Pixel.
    /// </para>
    /// </summary>
    private bool InputInProgress =>
        _drawing || _eraseSteps != null || _lassoPts != null || _movingSelection
        || _scalingSelection || _rotatingEl != null;

    /// <summary>Das Radiergummi-Ende des Stifts schlägt das gewählte Werkzeug.</summary>
    private ToolType EffectiveTool => _stylusInverted ? ToolType.Eraser : _tool;

    // ==================== Druck: erkennen statt annehmen ====================

    /// <summary>
    /// Ob das Gerät wirklich Druck liefert — und was daraus wird, wenn nicht.
    ///
    /// <para>
    /// <b>Warum das überhaupt nötig ist:</b> Avalonia gibt für einen Zeiger ohne
    /// Drucksensor nicht etwa „unbekannt" zurück, sondern glatt <c>0,5</c>. Ein Stift ohne
    /// Druck ist an der Zahl allein also nicht von einem Stift zu unterscheiden, der gerade
    /// mittelfest aufliegt. Wer die Zahl ungeprüft übernimmt, bekommt bei einem
    /// drucklosen Gerät zwar einen gleichmäßigen Strich — aber nur zufällig, und ohne dass
    /// irgendwo stünde, dass es der Rückfall ist.
    /// </para>
    /// <para>
    /// <b>Der Rückfall ist Pflicht, nicht Kür</b> (HANDOFF §1, §5a): Liefert ein Gerät
    /// keinen Druck, muss der Strich trotzdem sauber aussehen. Genau das tut die feste
    /// Mitte hier. Sobald ein Wert auftaucht, der von <c>0,5</c> abweicht, steht fest, dass
    /// das Gerät es kann — und ab da wird gemessen statt angenommen. Die Erkennung ist
    /// <b>statisch</b>, weil sie eine Eigenschaft der Hardware ist und nicht eine des
    /// gerade offenen Dokuments; sie über einen Registerkartenwechsel zu verlieren, hieße
    /// den ersten Strich danach wieder flach zu zeichnen.
    /// </para>
    /// </summary>
    private static bool _geraetLiefertDruck;

    /// <summary>Die Mitte — der Wert, den ein Gerät ohne Drucksensor ohnehin meldet.</summary>
    private const float DruckMitte = 0.5f;

    /// <summary>Unter diesem Druck wird kein Strich unsichtbar dünn.</summary>
    private const float DruckMinimum = 0.05f;

    /// <summary>Was der Stift an dieser Stelle liefert — alles, was in einen Punkt eingeht.</summary>
    private readonly record struct Stiftlage(float Druck, float TiltX, float TiltY)
    {
        /// <summary>Der Rückfall: mittlerer Druck, senkrecht gehalten.</summary>
        public static readonly Stiftlage Rueckfall = new(DruckMitte, 0f, 0f);
    }

    private static Stiftlage Lage(PointerPointProperties eigenschaften, PointerType art)
    {
        // Maus und Finger haben weder Druck noch Neigung, die einen Strich tragen würden.
        if (art != PointerType.Pen) return Stiftlage.Rueckfall;

        float roh = eigenschaften.Pressure;
        if (MathF.Abs(roh - DruckMitte) > 0.001f) _geraetLiefertDruck = true;

        return new Stiftlage(
            _geraetLiefertDruck ? Math.Clamp(roh, DruckMinimum, 1f) : DruckMitte,
            eigenschaften.XTilt,
            eigenschaften.YTilt);
    }

    // ==================== Neigung ====================

    /// <summary>
    /// Zuletzt gemessene Neigung in Grad — sie geht in jeden Punkt des Strichs ein
    /// (<see cref="WbPoint.TX"/>/<see cref="WbPoint.TY"/>) und verbreitert dort den
    /// Bleistift, so wie eine schräg gehaltene Mine es tut.
    ///
    /// <para>
    /// <b>Ein Gerät ohne Neigungsachse meldet 0</b>, und 0 heißt „senkrecht" — der
    /// Renderer rechnet dann mit dem Faktor Eins und zeichnet wie bisher. Es braucht
    /// dafür also keine zweite Erkennung wie beim Druck: dort ist der Rückfallwert
    /// <c>0,5</c> und damit von einem echten Messwert nicht zu unterscheiden, hier ist er
    /// <c>0</c> und bedeutet dasselbe wie der häufigste echte Messwert.
    /// </para>
    /// </summary>
    private float _tiltX, _tiltY;
    private PointerType _letzteZeigerart = PointerType.Mouse;
    private float _letzterDruck = DruckMitte;

    /// <summary>Mit F9 einblendbar: was der Stift gerade wirklich liefert. Standardmäßig aus.</summary>
    private bool _stiftAnzeige;

    // ==================== Handballenabweisung ====================

    /// <summary>
    /// Liegt gerade ein Stift auf? Solange das gilt, wird <b>jede</b> Berührung verworfen.
    ///
    /// <para>
    /// Das ist die Handballenabweisung, und sie hat zwei Stufen. Die erste ist, dass der
    /// Finger ohnehin nie zeichnet — er verschiebt und zoomt, mehr nicht; ein aufliegender
    /// Handballen kann also gar keinen Strich erzeugen. Die zweite ist diese hier: er soll
    /// auch nicht die Ansicht verschieben, während geschrieben wird. Ohne sie rutscht das
    /// Blatt unter dem Stift weg, sobald die Hand aufsetzt — der Fehler, an dem
    /// Notiz-Apps üblicherweise scheitern.
    /// </para>
    /// <para>
    /// Möglich ist das nur, weil <c>Pointer.Type</c> Stift und Finger sauber
    /// auseinanderhält; §5a hat genau das als Voraussetzung geprüft.
    /// </para>
    /// </summary>
    private bool _stiftLiegtAuf;

    /// <summary>Aktive Berührungen für die Gesten (1 Finger = schieben, 2 = zoomen).</summary>
    private readonly Dictionary<int, Point> _finger = new();
    private Point _gestenMitte;
    private double _gestenAbstand;

    // ==================== Zeiger ====================

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_vm == null || _page == null) return;
        Skia.Focus();   // Tastenkürzel brauchen den Fokus auf der Fläche, nicht auf dem Rahmen

        var punkt = e.GetCurrentPoint(Skia);
        var art = e.Pointer.Type;
        _letzteZeigerart = art;

        if (art == PointerType.Touch)
        {
            BeruehrungBeginnt(e, punkt);
            return;
        }

        // ---- Stift und Maus ----
        e.Pointer.Capture(Skia);

        if (art == PointerType.Pen)
        {
            _stiftLiegtAuf = true;
            // Das Radiergummi-Ende meldet sich als eigener Zeiger — dafür braucht es keine
            // Einstellung und keinen Werkzeugwechsel.
            _stylusInverted = punkt.Properties.IsEraser;
            // Die zweite Stift-Taste radiert, solange sie gehalten wird. Im WPF-Kopf öffnet
            // sie die Schnellaktionen; die sind nicht M1, und die Taste ungenutzt zu lassen
            // wäre schlechter als sie sinnvoll zu belegen.
            if (punkt.Properties.IsBarrelButtonPressed) _stylusInverted = true;
        }

        // Verschieben: mittlere Maustaste, Leertaste oder das Hand-Werkzeug.
        if (punkt.Properties.IsMiddleButtonPressed || _spaceDown || _tool == ToolType.Pan)
        {
            BeginPan(punkt.Position);
            e.Handled = true;
            return;
        }

        // Nur die linke Maustaste zeichnet; die rechte ist für M1 unbelegt.
        if (art == PointerType.Mouse && !punkt.Properties.IsLeftButtonPressed) return;

        var lage = Lage(punkt.Properties, art);
        NeigungMerken(lage, art);
        BeginInput(ToCanvas(punkt.Position), lage);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_page == null) return;

        var art = e.Pointer.Type;

        if (art == PointerType.Touch)
        {
            BeruehrungBewegt(e);
            return;
        }

        var aktuell = e.GetCurrentPoint(Skia);

        if (_panning)
        {
            MovePan(aktuell.Position);
            e.Handled = true;
            return;
        }

        NeigungMerken(Lage(aktuell.Properties, art), art);

        if (!InputInProgress)
        {
            HoverInput(ToCanvas(aktuell.Position));
            return;
        }

        // **Die Zwischenpunkte sind der Grund, warum der Strich etwas taugt.** Der
        // Digitizer tastet mit einigen hundert Hertz ab, die Oberfläche zeichnet mit
        // sechzig — wer nur `GetCurrentPoint` nimmt, wirft den Großteil der 4096
        // Druckstufen weg und bekommt einen eckigen Strich mit Treppen in der Breite
        // (HANDOFF §5a, ausdrücklich für diesen Brocken notiert).
        foreach (var p in e.GetIntermediatePoints(Skia))
            MoveInput(ToCanvas(p.Position), Lage(p.Properties, art));

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Touch)
        {
            BeruehrungEndet(e);
            return;
        }

        e.Pointer.Capture(null);

        if (e.Pointer.Type == PointerType.Pen) _stiftLiegtAuf = false;

        if (_panning) { EndPan(); e.Handled = true; return; }

        EndInput();
        _stylusInverted = false;
        e.Handled = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (InputInProgress) return;   // der Zeiger ist gefangen, das ist nur ein Streifschuss
        _eraserVisible = false;
        Neuzeichnen();
    }

    private void NeigungMerken(Stiftlage lage, PointerType art)
    {
        if (art != PointerType.Pen) return;
        _tiltX = lage.TiltX;
        _tiltY = lage.TiltY;
        _letzterDruck = lage.Druck;
        if (_stiftAnzeige) Neuzeichnen();
    }

    // ==================== Finger-Gesten ====================
    // Der Finger zeichnet nie — er schiebt und zoomt. Das ist die Grundlage der
    // Handballenabweisung: was nicht zeichnen kann, kann auch nicht versehentlich malen.

    private void BeruehrungBeginnt(PointerPressedEventArgs e, PointerPoint punkt)
    {
        if (_stiftLiegtAuf) return;   // Handballen, während geschrieben wird

        e.Pointer.Capture(Skia);
        _finger[e.Pointer.Id] = punkt.Position;
        if (_finger.Count >= 2) PinchSetzen();
        e.Handled = true;
    }

    private void PinchSetzen()
    {
        var p = _finger.Values.Take(2).ToList();
        _gestenMitte = new Point((p[0].X + p[1].X) / 2, (p[0].Y + p[1].Y) / 2);
        _gestenAbstand = Math.Max(8, Abstand(p[0], p[1]));
    }

    private static double Abstand(Point a, Point b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void BeruehrungBewegt(PointerEventArgs e)
    {
        if (!_finger.TryGetValue(e.Pointer.Id, out var alt)) return;

        var neu = e.GetPosition(Skia);
        _finger[e.Pointer.Id] = neu;

        if (_finger.Count == 1)
        {
            PanX += (float)(neu.X - alt.X);
            PanY += (float)(neu.Y - alt.Y);
            Neuzeichnen();
        }
        else if (_finger.Count >= 2)
        {
            var p = _finger.Values.Take(2).ToList();
            var mitte = new Point((p[0].X + p[1].X) / 2, (p[0].Y + p[1].Y) / 2);
            double abstand = Math.Max(8, Abstand(p[0], p[1]));

            ZoomAt(mitte, (float)(abstand / _gestenAbstand));
            PanX += (float)(mitte.X - _gestenMitte.X);
            PanY += (float)(mitte.Y - _gestenMitte.Y);
            _gestenMitte = mitte;
            _gestenAbstand = abstand;
            Neuzeichnen();
        }
        e.Handled = true;
    }

    private void BeruehrungEndet(PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
        _finger.Remove(e.Pointer.Id);
        if (_finger.Count >= 2) PinchSetzen();
        e.Handled = true;
    }

    // ==================== Verschieben ====================

    private void BeginPan(Point punkt)
    {
        _panning = true;
        _panLast = punkt;
    }

    private void MovePan(Point punkt)
    {
        if (!_panning) return;
        PanX += (float)(punkt.X - _panLast.X);
        PanY += (float)(punkt.Y - _panLast.Y);
        _panLast = punkt;
        Neuzeichnen();
    }

    private void EndPan() => _panning = false;

    // ==================== Der Strich ====================

    private void BeginInput(SKPoint c, Stiftlage lage)
    {
        if (_page == null || _vm == null) return;

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                _drawing = true;
                _activePoints = [new WbPoint(c.X, c.Y, lage.Druck, lage.TiltX, lage.TiltY)];
                break;

            case ToolType.Eraser:
                _eraseSteps = [];
                _eraserPos = c;
                _eraserVisible = true;
                EraseAt(c);
                break;

            // Beide Auswahl-Werkzeuge müssen zuerst die Griffe fragen: wer eine Auswahl hat
            // und den Drehgriff anfasst, will drehen — auch mit dem Lasso in der Hand.
            case ToolType.Lasso:
                if (BeginHandleDrag(c)) break;
                ClearSelection();
                _lassoPts = [c];
                break;

            case ToolType.Move:
                if (BeginHandleDrag(c)) break;
                BeginMoveOrSelect(c);
                break;
        }
        Neuzeichnen();
    }

    /// <summary>
    /// Drehen und Skalieren, seit Phase 4.5. Liefert <c>true</c>, wenn ein Griff angefasst
    /// wurde — dann ist der Zug vergeben und der Aufrufer tut nichts weiter.
    /// <para>
    /// <b>Die Weiche steht in <see cref="WbHandles.Probe"/></b>, damit die Reihenfolge in
    /// beiden Köpfen dieselbe ist: der Drehgriff hängt außerhalb des Rahmens, der Skaliergriff
    /// sitzt auf der Ecke und ragt hinein — wer erst auf „innerhalb" prüft, verschluckt ihn.
    /// </para>
    /// <para>
    /// <b>Verschieben behandelt diese Methode nicht.</b> Der Linux-Kopf verschiebt seit
    /// Phase 3 in <see cref="BeginMoveOrSelect"/>, und der greift zusätzlich ein Element auf,
    /// das noch gar nicht ausgewählt ist. Diese Unterscheidung geht verloren, wenn man beides
    /// zusammenlegt.
    /// </para>
    /// </summary>
    private bool BeginHandleDrag(SKPoint c)
    {
        if (_selection.Count == 0) return false;

        var einzeln = SingleSelected;
        switch (ProbeHandles(c))
        {
            case WbHandles.Grab.Rotate when einzeln != null:
                _rotatingEl = einzeln;
                _rotStartDeg = einzeln.Rotation;
                _rotStartPointer = WbHandles.AngleDeg(WbHandles.Center(einzeln), c);
                return true;

            // Der Drehpunkt hängt davon ab, was ausgewählt ist: bei einem Element sein
            // Mittelpunkt (es dreht sich um sich selbst), bei mehreren die obere linke Ecke
            // des Kastens (die Gruppe wächst nach unten rechts). Genauso im WPF-Kopf.
            case WbHandles.Grab.Scale:
                _scalingSelection = true;
                _scalePivot = einzeln != null
                    ? WbHandles.Center(einzeln)
                    : new SKPoint(_selectionBounds.Left, _selectionBounds.Top);
                _scaleStartDist = Math.Max(1f, SKPoint.Distance(_scalePivot, c));
                _scaleAccum = 1f;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Verschieben-Werkzeug: liegt etwas unter dem Zeiger, wird es gegriffen; sonst wird
    /// die Auswahl aufgehoben.
    /// </summary>
    private void BeginMoveOrSelect(SKPoint c)
    {
        if (_page == null) return;

        if (_selection.Count > 0 && InflatedSelectionBounds().Contains(c))
        {
            _movingSelection = true;
            _moveLast = c;
            _movedX = _movedY = 0;
            return;
        }

        var treffer = WbHit.Topmost(_page.Elements, c, 5f / Zoom);
        _selection.Clear();
        if (treffer == null) return;

        _selection.Add(treffer);
        ComputeSelectionBounds();
        _movingSelection = true;
        _moveLast = c;
        _movedX = _movedY = 0;
    }

    private void MoveInput(SKPoint c, Stiftlage lage)
    {
        if (_page == null) return;

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
            {
                if (!_drawing || _activePoints == null) return;

                // Punkte, die dichter liegen als ein Bildschirmpunkt, tragen nichts bei —
                // sie kosten nur Speicher und Zeichenzeit. Der Abstand hängt am Zoom:
                // beim Hineinzoomen darf feiner abgetastet werden.
                var letzter = _activePoints[^1];
                float mindest = 1.2f / Zoom;
                float dx = c.X - letzter.X, dy = c.Y - letzter.Y;
                if (dx * dx + dy * dy < mindest * mindest) return;

                // Leichte Glättung. Der Digitizer zittert um eine Zehntelstufe, und ohne
                // das sieht eine langsam gezogene Linie aus wie mit zittriger Hand.
                float gx = 0.35f * letzter.X + 0.65f * c.X;
                float gy = 0.35f * letzter.Y + 0.65f * c.Y;
                // Die Neigung wird genauso geglättet wie der Druck: der Digitizer meldet
                // sie mit demselben Zittern, und ein Sprung darin ließe die Breite des
                // Bleistifts mitten im Strich springen.
                float gp = 0.4f * letzter.P + 0.6f * lage.Druck;
                float gtx = 0.4f * letzter.TX + 0.6f * lage.TiltX;
                float gty = 0.4f * letzter.TY + 0.6f * lage.TiltY;
                _activePoints.Add(new WbPoint(gx, gy, gp, gtx, gty));
                break;
            }

            case ToolType.Eraser:
                if (_eraseSteps == null) return;
                _eraserPos = c;
                _eraserVisible = true;
                EraseAt(c);
                break;

            case ToolType.Lasso:
                if (DragHandle(c)) break;
                _lassoPts?.Add(c);
                break;

            case ToolType.Move:
                if (DragHandle(c)) break;
                if (!_movingSelection) return;
                float mx = c.X - _moveLast.X, my = c.Y - _moveLast.Y;
                foreach (var el in _selection) el.Translate(mx, my);
                _selectionBounds.Offset(mx, my);
                _movedX += mx; _movedY += my;
                _moveLast = c;
                break;

            default:
                return;
        }
        Neuzeichnen();
    }

    /// <summary>
    /// Das Ziehen an einem Griff. Liefert <c>true</c>, wenn gerade gedreht oder skaliert
    /// wird — dann ist die Bewegung vergeben.
    /// </summary>
    private bool DragHandle(SKPoint c)
    {
        if (_rotatingEl != null)
        {
            // Einrasten auf 15°-Schritte inbegriffen — die Rechnung steht in Core.
            _rotatingEl.Rotation = WbHandles.RotationFromDrag(
                WbHandles.Center(_rotatingEl), c, _rotStartDeg, _rotStartPointer);
            return true;
        }

        if (!_scalingSelection) return false;

        float dist = SKPoint.Distance(_scalePivot, c);
        float target = Math.Max(0.05f, dist / _scaleStartDist);   // Gesamtfaktor seit Anfassen
        float step = target / _scaleAccum;                        // relativer Schritt
        if (step > 0.001f && MathF.Abs(step - 1f) > 0.0001f)
        {
            foreach (var el in _selection) el.Scale(step, _scalePivot.X, _scalePivot.Y);
            _scaleAccum = target;
            ComputeSelectionBounds();
        }
        return true;
    }

    private void HoverInput(SKPoint c)
    {
        if (EffectiveTool != ToolType.Eraser) return;
        _eraserPos = c;
        _eraserVisible = true;
        Neuzeichnen();
    }

    private void EndInput()
    {
        if (_page == null || _vm == null) return;

        // Zuerst die Griffe — sie greifen bei beiden Auswahl-Werkzeugen, und ein
        // abgeschlossenes Drehen darf nicht als Lasso-Ende gedeutet werden.
        if (EndHandleDrag())
        {
            _drawing = false;
            _activePoints = null;
            InhaltVerwerfen();
            Neuzeichnen();
            return;
        }

        switch (EffectiveTool)
        {
            case ToolType.Pen:
            case ToolType.Pencil:
            case ToolType.Highlighter:
                if (_drawing && _activePoints != null) CommitStroke();
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
                if (_lassoPts is { Count: > 2 })
                {
                    _selection.Clear();
                    foreach (var el in WbHit.InsideLasso(_page.Elements, _lassoPts))
                        _selection.Add(el);
                    if (_selection.Count > 0) ComputeSelectionBounds();
                }
                _lassoPts = null;
                break;

            case ToolType.Move:
                if (_movingSelection &&
                    (Math.Abs(_movedX) > 0.01f || Math.Abs(_movedY) > 0.01f))
                {
                    _vm.Undo.Push(_page, new MoveElementsAction(_selection, _movedX, _movedY));
                    MarkDirty();
                }
                _movingSelection = false;
                break;
        }

        _drawing = false;
        _activePoints = null;
        InhaltVerwerfen();
        Neuzeichnen();
    }

    /// <summary>
    /// Schließt ein Drehen oder Skalieren ab und legt es auf den Verlaufsstapel. Liefert
    /// <c>true</c>, wenn einer der beiden Züge lief.
    /// <para>
    /// <b>Die Schwellen sind kein Beiwerk:</b> ein Klick auf einen Griff ohne Bewegung darf
    /// keinen Verlaufseintrag erzeugen — sonst nimmt das erste Rückgängig scheinbar nichts
    /// zurück, und der Nutzer drückt es ein zweites Mal.
    /// </para>
    /// </summary>
    private bool EndHandleDrag()
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
            return true;
        }

        if (!_scalingSelection) return false;

        _scalingSelection = false;
        if (Math.Abs(_scaleAccum - 1f) > 0.0001f)
        {
            _vm!.Undo.Push(_page!,
                new ScaleElementsAction(_selection, _scaleAccum, _scalePivot.X, _scalePivot.Y));
            MarkDirty();
        }
        return true;
    }

    private void CommitStroke()
    {
        if (_page == null || _vm == null || _activePoints == null) return;

        // Ein Tippen ohne Ziehen soll einen Punkt setzen, nicht nichts. Ein Strich aus
        // einem einzigen Punkt hat keine Richtung und wird von Skia nicht gezeichnet.
        if (_activePoints.Count == 1)
        {
            var p = _activePoints[0];
            _activePoints.Add(new WbPoint(p.X + 0.1f, p.Y + 0.1f, p.P));
        }

        var strich = new StrokeElement
        {
            Points = _activePoints,
            Color = CurrentInkHex(),
            Width = AktiveStrichbreite(),
            Kind = AktiveStrichart(),
        };
        _page.Elements.Add(strich);
        _vm.Undo.Push(_page, new AddElementsAction([strich]));
        MarkDirty();
    }

    // ==================== Radieren ====================

    /// <summary>
    /// Punktgenau: ein berührter Strich wird aufgetrennt, die Reststücke bleiben stehen.
    /// Die Geometrie dafür steht in <see cref="WbErase"/> in Core — hier steht nur, was
    /// radierbar ist und was nicht.
    /// </summary>
    private void EraseAt(SKPoint c)
    {
        if (_page == null || _eraseSteps == null) return;
        float r = _eraserRadius / Zoom;

        for (int i = _page.Elements.Count - 1; i >= 0; i--)
        {
            var el = _page.Elements[i];
            switch (el)
            {
                case StrokeElement s:
                {
                    if (!WbHit.Hit(s, c, r)) break;
                    var teile = WbErase.SplitStroke(s, c, r + s.Width / 2f);
                    _page.Elements.RemoveAt(i);
                    _page.Elements.InsertRange(i, teile);
                    _eraseSteps.Add(new EraseStep(s, i, teile));
                    MarkDirty();
                    break;
                }

                // Formen und Text als Ganzes — aber nur bei Berührung der Kontur bzw. des
                // Rahmens, sonst radiert ein Streifen quer über die Seite alles darunter weg.
                case ShapeElement or TextElement:
                    if (!WbHit.Hit(el, c, r)) break;
                    _page.Elements.RemoveAt(i);
                    _eraseSteps.Add(new EraseStep(el, i, []));
                    MarkDirty();
                    break;

                // Bilder sind nicht radierbar — die gehen über Auswählen und Löschen.
            }
        }
    }
}
