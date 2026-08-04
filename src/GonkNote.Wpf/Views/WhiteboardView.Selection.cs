using GonkNote.Core.Models;
using GonkNote.Core.Editing;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Auswahl: Treffer-Erkennung, Lasso, Griffe, Verschieben/Skalieren/Drehen
/// sowie Ausschneiden, Kopieren, Einfuegen und Loeschen.
/// <para>
/// <b>Die Geometrie steht seit Phase 3 in <see cref="WbHit"/>.</b> Bis dahin lag sie hier
/// privat und ein zweites Mal im Linux-Kopf — zwei Fassungen derselben Formel driften
/// auseinander, ohne dass es auffällt (HANDOFF §4.10). Was hier bleibt, hängt am
/// Steuerelement: <c>Zoom</c> für die Toleranzen, <c>_page</c> und <c>_selection</c> für
/// den Zustand, die Griffe für die Darstellung. Gerechnet wird in Core.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>Skalier-Griff unten rechts an der (aufgeblähten) Auswahl-Umrandung.</summary>
    private bool HitSelectionScaleHandle(SKPoint c)
    {
        var b = InflatedSelectionBounds();
        float r = 12f / Zoom;
        float dx = c.X - b.Right, dy = c.Y - b.Bottom;
        return dx * dx + dy * dy <= r * r;
    }

    // ---------- Griffe für Einzelauswahl (mitgedreht) ----------

    /// <summary>Dreh-Griff (oben) und Skalier-Griff (unten rechts) eines einzelnen Elements, mitgedreht.</summary>
    private (SKPoint Rotate, SKPoint Scale, SKPoint TL, SKPoint TR, SKPoint BR, SKPoint BL) SingleHandles(WbElement el)
    {
        var b = ElementBounds(el);
        var ctr = new SKPoint(b.MidX, b.MidY);
        float pad = 10f / Zoom;
        var tl = new SKPoint(b.Left - pad, b.Top - pad);
        var tr = new SKPoint(b.Right + pad, b.Top - pad);
        var br = new SKPoint(b.Right + pad, b.Bottom + pad);
        var bl = new SKPoint(b.Left - pad, b.Bottom + pad);
        var rot = new SKPoint(b.MidX, b.Top - pad - 28f / Zoom);
        float d = el.Rotation;
        return (WbHit.Rotate(rot, ctr, d), WbHit.Rotate(br, ctr, d),
                WbHit.Rotate(tl, ctr, d), WbHit.Rotate(tr, ctr, d),
                WbHit.Rotate(br, ctr, d), WbHit.Rotate(bl, ctr, d));
    }

    private bool NearHandle(SKPoint c, SKPoint handle)
    {
        float r = 13f / Zoom;
        float dx = c.X - handle.X, dy = c.Y - handle.Y;
        return dx * dx + dy * dy <= r * r;
    }

    private static float AngleDeg(SKPoint from, SKPoint to) =>
        MathF.Atan2(to.Y - from.Y, to.X - from.X) * 180f / MathF.PI;

    /// <summary>Ist der Zeiger „innerhalb" der (ggf. gedrehten) Auswahl → zum Verschieben?</summary>
    private bool SelectionContains(SKPoint c)
    {
        if (_selection.Count == 1)
        {
            var el = _selection.First();
            var b = ElementBounds(el);
            var ctr = new SKPoint(b.MidX, b.MidY);
            var local = WbHit.Rotate(c, ctr, -el.Rotation);   // Zeiger in den ungedrehten Raum bringen
            b.Inflate(10f / Zoom, 10f / Zoom);
            return b.Contains(local);
        }
        return InflatedSelectionBounds().Contains(c);
    }

    /// <summary>Verschieben-Werkzeug: Objekt direkt greifen; Lasso: neue Umkreisung beginnen.</summary>
    private void BeginSelectOrLasso(SKPoint c)
    {
        if (EffectiveTool == ToolType.Move)
        {
            var pick = HitTestElement(c);
            ClearSelection();
            if (pick != null)
            {
                _selection.Add(pick);
                ComputeSelectionBounds();
                _movingSelection = true;
                _moveLast = c;
                _movedX = _movedY = 0;
            }
        }
        else
        {
            ClearSelection();
            _lassoPts = new List<SKPoint> { c };
        }
    }

    /// <summary>
    /// Liegt der Zeiger (Radius <paramref name="r"/>) auf dem Element? Gerechnet wird in
    /// <see cref="WbHit.Hit"/> — hier steht nur noch der Name, unter dem der Radierer und
    /// die Direktauswahl ihn kennen.
    /// </summary>
    private static bool HitElement(WbElement el, SKPoint c, float r) => WbHit.Hit(el, c, r);

    /// <summary>
    /// Oberstes Objekt unter dem Zeiger für die Direktauswahl (Verschieben-Werkzeug).
    /// Vordergrund-Objekte (Striche/Formen/Text/Zettel) haben Vorrang vor Bildern/PDF-
    /// Seiten, damit man einen Strich über einem Hintergrundbild greifen kann — die
    /// Reihenfolge steckt in <see cref="WbHit.Topmost"/>.
    /// </summary>
    private WbElement? HitTestElement(SKPoint c) =>
        _page == null ? null : WbHit.Topmost(_page.Elements, c, 5f / Zoom);

    private static float SegmentDistance(SKPoint a, SKPoint b, SKPoint p) =>
        WbErase.SegmentDistance(a, b, p);

    // ==================== Lasso / Auswahl ====================

    private void ClearSelection()
    {
        HideQuickMenu();
        _selection.Clear();
        _movingSelection = false;
        _scalingSelection = false;
        _rotatingEl = null;
        Skia.InvalidateVisual();
    }

    private SKRect InflatedSelectionBounds()
    {
        var b = _selectionBounds;
        b.Inflate(12f / Zoom, 12f / Zoom);
        return b;
    }

    /// <summary>
    /// Was das Lasso eingefangen hat. Die Regel „nur ~vollständig (≥ 95 %) Umschlossenes
    /// zählt" (Nutzer-Wunsch aus V1) steht in <see cref="WbHit.InsideLasso"/>; hier bleibt
    /// nur, was mit dem Ergebnis geschieht.
    /// </summary>
    private void SelectByLasso(List<SKPoint> lasso)
    {
        if (_page == null) return;

        _selection.Clear();
        foreach (var el in WbHit.InsideLasso(_page.Elements, lasso)) _selection.Add(el);

        if (_selection.Count > 0) ComputeSelectionBounds();
        Skia.InvalidateVisual();
    }

    private void ComputeSelectionBounds() => _selectionBounds = WbHit.Bounds(_selection);

    internal static SKRect ElementBounds(WbElement el) => WbRenderer.ElementBounds(el);

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

    // Interne Zwischenablage (Whiteboard-Elemente); überlebt keinen App-Neustart
    private readonly List<WbElement> _clipboard = new();

    private void CopySelection()
    {
        if (_selection.Count == 0) return;
        _clipboard.Clear();
        _clipboard.AddRange(_selection.Select(CloneElement));
    }

    private void CutSelection()
    {
        if (_selection.Count == 0) return;
        CopySelection();
        DeleteSelection();
    }

    /// <summary>Fügt die interne Zwischenablage ein (an <paramref name="at"/> zentriert, sonst leicht versetzt).</summary>
    private void PasteClipboard(SKPoint? at)
    {
        if (_page == null || _vm == null || _clipboard.Count == 0) return;
        var clones = _clipboard.Select(CloneElement).ToList();

        if (at is { } target)
        {
            // Mittelpunkt der kopierten Gruppe auf den Zielpunkt legen
            bool first = true;
            SKRect b = SKRect.Empty;
            foreach (var cl in clones)
            {
                var eb = ElementBounds(cl);
                if (first) { b = eb; first = false; } else b = SKRect.Union(b, eb);
            }
            float dx = target.X - b.MidX, dy = target.Y - b.MidY;
            foreach (var cl in clones) cl.Translate(dx, dy);
        }
        else
        {
            foreach (var cl in clones) cl.Translate(18, 18);
        }

        _page.Elements.AddRange(clones);
        _vm.Undo.Push(_page, new AddElementsAction(clones));

        // Eingefügtes gleich auswählen (Verschieben-Werkzeug)
        _suppressToolEvents = true;
        foreach (var bt in ToolButtons) bt.IsChecked = bt == BtnMove;
        _suppressToolEvents = false;
        SetTool(ToolType.Move);
        _selection.Clear();
        foreach (var cl in clones) _selection.Add(cl);
        ComputeSelectionBounds();
        MarkDirty();
        Skia.InvalidateVisual();
    }

    private void SelectAll()
    {
        if (_page == null || _page.Elements.Count == 0) return;
        _selection.Clear();
        foreach (var el in _page.Elements) _selection.Add(el);
        ComputeSelectionBounds();
        // Auswahl-Werkzeug aktivieren, damit Griffe erscheinen
        if (_tool != ToolType.Move && _tool != ToolType.Lasso)
        {
            _suppressToolEvents = true;
            foreach (var bt in ToolButtons) bt.IsChecked = bt == BtnMove;
            _suppressToolEvents = false;
            SetTool(ToolType.Move);
        }
        Skia.InvalidateVisual();
    }
}
