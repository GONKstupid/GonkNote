using System.Windows.Media;
using GonkNote.Models;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Formen-Stift: erkennt aus einem freihaendigen Zug die gemeinte Form
/// (Gerade, Rechteck, Ellipse, Dreieck, Pfeil).
/// </summary>
public partial class WhiteboardView
{
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
        // Standardtinte: Schwarz auf hellen, Weiß auf dunklen Seiten
        return EffectiveShade(_page) == PageShade.Dark ? "#FFFFFFFF" : "#FF000000";
    }

    /// <summary>
    /// Hält die erste Farbkachel synchron zur Seite: Schwarz auf hellen, Weiß auf
    /// dunklen Seiten. Wird aus dem Paint-Pfad aufgerufen (deckt Seitenwechsel,
    /// Farbton- und Theme-Wechsel ab) und ist per Cache-Feld praktisch kostenlos.
    /// </summary>
    private bool? _autoSwatchDark;

    private void RefreshAutoSwatch()
    {
        bool dark = EffectiveShade(_page) == PageShade.Dark;
        if (_autoSwatchDark == dark) return;
        _autoSwatchDark = dark;
        AutoSwatch.Background = new SolidColorBrush(dark ? Colors.White : Colors.Black);
    }
}
