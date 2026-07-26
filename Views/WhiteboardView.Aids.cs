using System.Windows;
using GonkNote.Core.Rendering;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Zeichenhilfen Lineal und Geodreieck: Platzieren, Drehen, Einrasten.
/// </summary>
public partial class WhiteboardView
{
    // ==================== Zeichenhilfen: Lineal & Geodreieck ====================

    // Halbe Hypotenuse = Höhe = 8 cm (16-cm-Geodreieck, wie die SVG-Assets;
    // cm-Skala der SVGs ist bis ±7 nummeriert)
    private static readonly float SsHalfHyp = 8f * PxPerCm;

    private void Ruler_Click(object sender, RoutedEventArgs e) => SetAid(DrawAid.Ruler);
    private void SetSquare_Click(object sender, RoutedEventArgs e) => SetAid(DrawAid.SetSquare);

    /// <summary>Schaltet eine Zeichenhilfe ein bzw. (bei erneutem Klick) aus. Beide schließen sich aus.</summary>
    private void SetAid(DrawAid kind)
    {
        _aid = _aid == kind ? DrawAid.None : kind;
        if (_aid != DrawAid.None) _lastAid = _aid;
        BtnRuler.IsChecked = _aid == DrawAid.Ruler;
        BtnSetSquare.IsChecked = _aid == DrawAid.SetSquare;
        // Gruppe aufklappen, solange eine Hilfe aktiv ist (zum Umschalten), sonst nur die zuletzt benutzte zeigen
        SetAidGroupExpanded(_aid != DrawAid.None);
        if (_aid != DrawAid.None && !_aidPlaced)
        {
            var v = VisibleCanvasRect();
            _aidCenter = new SKPoint(v.MidX, v.MidY);
            _aidAngleDeg = 0f;
            _aidPlaced = true;
        }
        Skia.InvalidateVisual();
    }

    private void SetAidGroupExpanded(bool expanded)
    {
        _aidGroupExpanded = expanded;
        var rep = _lastAid == DrawAid.SetSquare ? BtnSetSquare : BtnRuler;
        foreach (var b in AidButtons)
            b.Visibility = expanded || b == rep ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Richtungs- (entlang) und Normalenvektor (quer) der Ausrichtung.</summary>
    private (SKPoint Dir, SKPoint Nrm) AidAxes()
    {
        float a = _aidAngleDeg * MathF.PI / 180f;
        var d = new SKPoint(MathF.Cos(a), MathF.Sin(a));
        return (d, new SKPoint(-d.Y, d.X));
    }

    /// <summary>Lokalen Punkt (u entlang, v quer) in Weltkoordinaten wandeln.</summary>
    private SKPoint AidP(float u, float v)
    {
        var (d, n) = AidAxes();
        return new SKPoint(_aidCenter.X + u * d.X + v * n.X, _aidCenter.Y + u * d.Y + v * n.Y);
    }

    /// <summary>Eckpunkte der aktiven Hilfe in lokalen Koordinaten.</summary>
    private SKPoint[] AidLocalPolygon() => _aid switch
    {
        DrawAid.Ruler => new[]
        {
            new SKPoint(-RulerLength / 2f, -RulerHalfWidth), new SKPoint(RulerLength / 2f, -RulerHalfWidth),
            new SKPoint(RulerLength / 2f, RulerHalfWidth), new SKPoint(-RulerLength / 2f, RulerHalfWidth),
        },
        // Rechtwinklig-gleichschenkliges Geodreieck: Hypotenuse unten, rechter Winkel oben
        DrawAid.SetSquare => new[]
        {
            new SKPoint(-SsHalfHyp, 0f), new SKPoint(SsHalfHyp, 0f), new SKPoint(0f, -SsHalfHyp),
        },
        _ => Array.Empty<SKPoint>(),
    };

    /// <summary>Kantenpaare (Indizes in das Polygon) zum Einrasten.</summary>
    private (int A, int B)[] AidEdgePairs() => _aid switch
    {
        DrawAid.Ruler => new[] { (0, 1), (3, 2) },                 // beide Längskanten
        DrawAid.SetSquare => new[] { (0, 1), (1, 2), (2, 0) },     // Hypotenuse + zwei Katheten
        _ => Array.Empty<(int, int)>(),
    };

    private SKPoint[] AidWorldPolygon()
    {
        var lp = AidLocalPolygon();
        var wp = new SKPoint[lp.Length];
        for (int i = 0; i < lp.Length; i++) wp[i] = AidP(lp[i].X, lp[i].Y);
        return wp;
    }

    /// <summary>Lokale x-Position des „rechten Endes" (dort sitzt der Dreh-Griff).</summary>
    private float AidRightEndX => _aid == DrawAid.SetSquare ? SsHalfHyp : RulerLength / 2f;

    private SKPoint AidHandleCenter()
    {
        var (d, _) = AidAxes();
        var end = AidP(AidRightEndX, 0f);
        float ext = 16f / Zoom;
        return new SKPoint(end.X + d.X * ext, end.Y + d.Y * ext);
    }

    private bool AidHandleHit(SKPoint c)
    {
        var h = AidHandleCenter();
        float r = 13f / Zoom;
        float dx = c.X - h.X, dy = c.Y - h.Y;
        return dx * dx + dy * dy <= r * r;
    }

    private static bool PointInPolygon(SKPoint[] poly, SKPoint p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if (poly[i].Y > p.Y != poly[j].Y > p.Y &&
                p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)
                inside = !inside;
        }
        return inside;
    }

    private bool AidBodyContains(SKPoint c) => _aid != DrawAid.None && PointInPolygon(AidWorldPolygon(), c);

    /// <summary>Prüft, ob ein Strichstart nahe einer Kante liegt, und aktiviert das Einrasten auf diese Kante.</summary>
    private bool TryActivateAidSnap(SKPoint c)
    {
        _rulerSnapActive = false;
        if (_aid == DrawAid.None) return false;

        var poly = AidWorldPolygon();
        float best = float.MaxValue;
        SKPoint bestE0 = default, bestDir = default;

        foreach (var (ia, ib) in AidEdgePairs())
        {
            var a = poly[ia]; var b = poly[ib];
            float ex = b.X - a.X, ey = b.Y - a.Y;
            float len = MathF.Sqrt(ex * ex + ey * ey);
            if (len < 1f) continue;
            var dir = new SKPoint(ex / len, ey / len);
            float t = (c.X - a.X) * dir.X + (c.Y - a.Y) * dir.Y;
            if (t < -80f || t > len + 80f) continue;
            var proj = new SKPoint(a.X + dir.X * t, a.Y + dir.Y * t);
            float pd = MathF.Sqrt((c.X - proj.X) * (c.X - proj.X) + (c.Y - proj.Y) * (c.Y - proj.Y));
            if (pd <= RulerSnapDist && pd < best) { best = pd; bestE0 = a; bestDir = dir; }
        }

        if (best == float.MaxValue) return false;
        _rulerSnapE0 = bestE0;
        _rulerSnapDir = bestDir;
        _rulerSnapActive = true;
        return true;
    }

    /// <summary>Projiziert einen Punkt auf die eingerastete Kantenlinie (sonst unverändert).</summary>
    private SKPoint ApplyAidSnap(SKPoint p)
    {
        if (!_rulerSnapActive) return p;
        float t = (p.X - _rulerSnapE0.X) * _rulerSnapDir.X + (p.Y - _rulerSnapE0.Y) * _rulerSnapDir.Y;
        return new SKPoint(_rulerSnapE0.X + _rulerSnapDir.X * t, _rulerSnapE0.Y + _rulerSnapDir.Y * t);
    }

    /// <summary>Startet Bewegen/Drehen, wenn Körper bzw. Dreh-Griff getroffen wird.</summary>
    private bool TryBeginAid(SKPoint c)
    {
        if (_aid == DrawAid.None) return false;
        if (AidHandleHit(c)) { _rulerDrag = RulerDrag.Rotate; Skia.InvalidateVisual(); return true; }
        if (AidBodyContains(c)) { _rulerDrag = RulerDrag.Move; _rulerDragLast = c; Skia.InvalidateVisual(); return true; }
        return false;
    }

    private void UpdateAidDrag(SKPoint c)
    {
        if (_rulerDrag == RulerDrag.Move)
        {
            _aidCenter = new SKPoint(_aidCenter.X + (c.X - _rulerDragLast.X), _aidCenter.Y + (c.Y - _rulerDragLast.Y));
            _rulerDragLast = c;
        }
        else if (_rulerDrag == RulerDrag.Rotate)
        {
            float raw = MathF.Atan2(c.Y - _aidCenter.Y, c.X - _aidCenter.X) * 180f / MathF.PI;
            _aidAngleDeg = SnapAngle(raw);
        }
        Skia.InvalidateVisual();
    }

    /// <summary>Magnetisches Einrasten an 15°-Vielfachen (0/15/30/45…), sonst frei.</summary>
    private static float SnapAngle(float deg)
    {
        float nearest = MathF.Round(deg / RulerAngleStep) * RulerAngleStep;
        return MathF.Abs(deg - nearest) <= RulerAngleSnapTol ? nearest : deg;
    }

    private void DrawActiveAid(SKCanvas canvas)
    {
        var accent = ResColorFromBrush("Brush.Accent");

        if (_aid == DrawAid.SetSquare)
        {
            // Das Geodreieck zeichnet sich komplett selbst (Körper, Kanten, Aufdruck)
            DrawSetSquare(canvas, _aidCenter, _aidAngleDeg, Zoom);
        }
        else
        {
            var poly = AidWorldPolygon();
            using (var path = new SKPath())
            {
                path.MoveTo(poly[0]);
                for (int i = 1; i < poly.Length; i++) path.LineTo(poly[i]);
                path.Close();
                using var fill = new SKPaint { Color = new SKColor(30, 41, 59, 40), IsAntialias = true };
                canvas.DrawPath(path, fill);
                using var edge = new SKPaint { Color = accent, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f / Zoom, IsAntialias = true };
                canvas.DrawPath(path, edge);
            }
            DrawCmScale(canvas, accent, RulerHalfWidth, RulerLength / 2f);
        }

        // Dreh-Griff
        var h = AidHandleCenter();
        using (var hf = new SKPaint { Color = accent, IsAntialias = true })
            canvas.DrawCircle(h, 6f / Zoom, hf);
        using (var hr = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 1.4f / Zoom, IsAntialias = true })
            canvas.DrawCircle(h, 6f / Zoom, hr);

        if (_rulerDrag == RulerDrag.Rotate)
            DrawAidAngle(canvas);
    }

    /// <summary>cm-Skala entlang einer Kante (lokal bei y=edgeY), Ticks nach innen (−y).</summary>
    private void DrawCmScale(SKCanvas canvas, SKColor accent, float edgeY, float halfLen)
    {
        using var tick = new SKPaint
        {
            Color = accent.WithAlpha(210), Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f / Zoom, IsAntialias = true,
        };
        int k = 0;
        for (float u = 0; u <= halfLen; u += PxPerCm, k++)
        {
            float len = (k % 5 == 0) ? 9f : 5f;
            canvas.DrawLine(AidP(u, edgeY), AidP(u, edgeY - len), tick);
            if (u > 0) canvas.DrawLine(AidP(-u, edgeY), AidP(-u, edgeY - len), tick);
        }
    }

    // ==================== Geodreieck (SVG-Assets des Nutzers, je Theme) ====================
    //
    // Das Geodreieck wird nicht mehr im Code gezeichnet, sondern aus den Nutzer-SVGs
    // Assets/Geodreieck-Light.svg bzw. -Dark.svg gerendert (eingebettete Ressourcen,
    // Unterschied: Bandfarbe Lila bzw. Pink). Vermessene SVG-Geometrie (viewBox 2520x1680):
    //   Hypotenuse von (2,2 | 1468,9) bis (2517,5 | 1468,9) = 2515,2 units,
    //   Spitze bei (1259,8 | 210) -> 16-cm-Geodreieck -> 157,2 units/cm.
    // Beim Zeichnen kommt der Hypotenusen-Mittelpunkt des SVG auf das Interaktions-
    // zentrum, skaliert auf 1 Geodreieck-cm = 1 Seiten-cm (PxPerCm). Dadurch deckt
    // sich der Aufdruck exakt mit dem Einrast-/Dreh-Polygon (SsHalfHyp = 8 cm).

    /// <summary>
    /// Zeichnet das Geodreieck-SVG um <paramref name="center"/> mit Drehung
    /// <paramref name="angleDeg"/>. Leitet auf <see cref="WbAidRenderer"/> in GonkNote.Core
    /// weiter, damit WPF und der Avalonia-Port dieselben Assets und dieselbe Geometrie
    /// nutzen (HANDOFF §9.3e). Statisch, damit der Render-Harness denselben Code aufruft.
    /// </summary>
    public static void DrawSetSquare(SKCanvas canvas, SKPoint center, float angleDeg, float zoom) =>
        WbAidRenderer.DrawSetSquare(canvas, center, angleDeg, zoom,
                                    ThemeService.Current == AppTheme.Dark);

    private void DrawAidAngle(SKCanvas canvas)
    {
        var (_, n) = AidAxes();
        float disp = ((_aidAngleDeg % 180f) + 180f) % 180f;   // Winkel der Kante gegen die Waagerechte, 0–179°
        string label = $"{disp:0}°";

        float ts = 15f / Zoom;
        float gap = (_aid == DrawAid.SetSquare ? SsHalfHyp * 0.5f : RulerHalfWidth) + 30f / Zoom;
        var pos = new SKPoint(_aidCenter.X - n.X * gap, _aidCenter.Y - n.Y * gap);

        using var tp = new SKPaint
        {
            Color = SKColors.White, IsAntialias = true, TextSize = ts,
            TextAlign = SKTextAlign.Center, Typeface = WbFonts.Bold,
        };
        float tw = tp.MeasureText(label);
        float padX = 9f / Zoom, padY = 6f / Zoom;
        var bg = new SKRect(pos.X - tw / 2f - padX, pos.Y - ts / 2f - padY,
                            pos.X + tw / 2f + padX, pos.Y + ts / 2f + padY);
        using (var bgp = new SKPaint { Color = new SKColor(23, 32, 51, 235), IsAntialias = true })
            canvas.DrawRoundRect(bg, 6f / Zoom, 6f / Zoom, bgp);
        canvas.DrawText(label, pos.X, pos.Y + ts * 0.35f, tp);
    }
}
