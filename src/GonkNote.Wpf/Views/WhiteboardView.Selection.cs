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
/// auseinander, ohne dass es auffällt (HANDOFF §4.10).
/// </para>
/// <para>
/// <b>Seit Phase 4.5 gilt das auch für die Griffe</b> (<see cref="WbHandles"/>). Hier stand
/// bis dahin, sie blieben im Kopf, weil sie „am Steuerelement hängen" — <b>das traf nicht
/// zu</b>: gerechnet wurde mit <c>ElementBounds</c>, <c>WbHit.Rotate</c> und <c>Rotation</c>,
/// alles aus Core, plus <c>Zoom</c>. Und <c>Zoom</c> ist eine Zahl. Aufgefallen ist es, als
/// der Linux-Kopf in Phase 4.5 Drehen und Skalieren bekommen sollte — er hätte die Formeln
/// sonst abgeschrieben.
/// </para>
/// <para>
/// <b>Was hier wirklich bleibt:</b> der Zustand (<c>_page</c>, <c>_selection</c>, was gerade
/// gezogen wird), die Zeigerereignisse und die Undo-Aktionen. Gerechnet wird in Core,
/// gezeichnet in <see cref="WbSelectionRenderer"/>.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>
    /// Das einzelne ausgewählte Element — oder <c>null</c>, wenn es keines oder mehrere sind.
    /// So fragen Weiche und Zeichner in Core danach (<see cref="WbHandles.Probe"/>).
    /// </summary>
    private WbElement? SingleSelected => _selection.Count == 1 ? _selection.First() : null;

    /// <summary>Was der Zeiger an der Auswahl anfasst. Gerechnet wird in <see cref="WbHandles"/>.</summary>
    private WbHandles.Grab ProbeHandles(SKPoint c) =>
        WbHandles.Probe(SingleSelected, _selectionBounds, _selection.Count, c, Zoom);

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

    private SKRect InflatedSelectionBounds() => WbHandles.InflatedBounds(_selectionBounds, Zoom);

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
