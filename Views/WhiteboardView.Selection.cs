using GonkNote.Models;
using GonkNote.Editing;
using GonkNote.Rendering;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Auswahl: Treffer-Erkennung, Lasso, Griffe, Verschieben/Skalieren/Drehen
/// sowie Ausschneiden, Kopieren, Einfuegen und Loeschen.
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

    private static SKPoint RotatePt(SKPoint p, SKPoint pivot, float deg)
    {
        float r = deg * MathF.PI / 180f, cos = MathF.Cos(r), sin = MathF.Sin(r);
        float dx = p.X - pivot.X, dy = p.Y - pivot.Y;
        return new SKPoint(pivot.X + dx * cos - dy * sin, pivot.Y + dx * sin + dy * cos);
    }

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
        return (RotatePt(rot, ctr, d), RotatePt(br, ctr, d),
                RotatePt(tl, ctr, d), RotatePt(tr, ctr, d), RotatePt(br, ctr, d), RotatePt(bl, ctr, d));
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
            var local = RotatePt(c, ctr, -el.Rotation);   // Zeiger in den ungedrehten Raum bringen
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

    private static bool HitElement(WbElement el, SKPoint c, float r)
    {
        // Gedrehte Elemente: Klickpunkt in den ungedrehten Raum zurückdrehen
        if (el.Rotation != 0f)
        {
            var eb = ElementBounds(el);
            c = RotatePt(c, new SKPoint(eb.MidX, eb.MidY), -el.Rotation);
        }
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

            case ImageElement im:
                return SKRect.Create(im.X, im.Y, im.Width, im.Height).Contains(c);

            case StickyNoteElement sn:
                return SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height).Contains(c);

            default:
                return false;
        }
    }

    /// <summary>
    /// Oberstes Objekt unter dem Zeiger für die Direktauswahl (Verschieben-Werkzeug).
    /// Vordergrund-Objekte (Striche/Formen/Text/Zettel) haben Vorrang vor Bildern/PDF-
    /// Seiten, damit man einen Strich über einem Hintergrundbild greifen kann.
    /// </summary>
    private WbElement? HitTestElement(SKPoint c)
    {
        if (_page == null) return null;
        float tol = 5f / Zoom;
        for (int pass = 0; pass < 2; pass++)
            for (int i = _page.Elements.Count - 1; i >= 0; i--)
            {
                var el = _page.Elements[i];
                bool isImage = el is ImageElement;
                if (pass == 0 && isImage) continue;   // erst Vordergrund …
                if (pass == 1 && !isImage) continue;   // … dann Bilder
                if (HitElement(el, c, tol)) return el;
            }
        return null;
    }

    private static float SegOrPointDist(List<WbPoint> pts, int i, SKPoint c)
    {
        var a = new SKPoint(pts[i].X, pts[i].Y);
        if (i + 1 >= pts.Count) return SKPoint.Distance(a, c);
        var b = new SKPoint(pts[i + 1].X, pts[i + 1].Y);
        return SegmentDistance(a, b, c);
    }

    private static float SegmentDistance(SKPoint a, SKPoint b, SKPoint p) =>
        WbErase.SegmentDistance(a, b, p);

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

    private static (SKPoint A, SKPoint B, SKPoint C) TrianglePoints(ShapeElement sh) =>
        WbRenderer.TrianglePoints(sh);

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
            // Nutzer-Wunsch: nur Objekte auswählen, die ~vollständig (≥95 %) umschlossen
            // sind – so lässt sich ein Strich greifen, ohne die PDF-Seite/Form dahinter
            // mitzunehmen.
            bool inside = el switch
            {
                StrokeElement s => s.Points.Count > 0 &&
                    s.Points.Count(p => path.Contains(p.X, p.Y)) >= s.Points.Count * 0.95f,
                ShapeElement sh => AllCornersInside(path, ElementBounds(sh)),
                TextElement t => AllCornersInside(path, TextBounds(t)),
                ImageElement im => AllCornersInside(path, SKRect.Create(im.X, im.Y, im.Width, im.Height)),
                StickyNoteElement sn => AllCornersInside(path, SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height)),
                _ => false,
            };
            if (inside) _selection.Add(el);
        }

        if (_selection.Count > 0) ComputeSelectionBounds();
        Skia.InvalidateVisual();
    }

    /// <summary>Alle vier Ecken (plus Mitte) eines Rechtecks liegen im Lasso-Pfad.</summary>
    private static bool AllCornersInside(SKPath path, SKRect r) =>
        path.Contains(r.Left, r.Top) && path.Contains(r.Right, r.Top) &&
        path.Contains(r.Left, r.Bottom) && path.Contains(r.Right, r.Bottom) &&
        path.Contains(r.MidX, r.MidY);

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
