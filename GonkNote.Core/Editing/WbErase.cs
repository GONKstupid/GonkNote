using System;
using System.Collections.Generic;
using GonkNote.Models;
using GonkNote.Rendering;
using SkiaSharp;

namespace GonkNote.Editing;

/// <summary>
/// Geometrie fürs **punktgenaue** Radieren: ein Strich wird an der berührten Stelle
/// aufgetrennt, die Reststücke bleiben stehen (wie in der WPF-App). Plattformneutral,
/// damit WPF und der Avalonia-Port dieselbe Logik nutzen (HANDOFF §9.3e).
/// </summary>
public static class WbErase
{
    /// <summary>Abstand eines Punktes zur Strecke a–b.</summary>
    public static float SegmentDistance(SKPoint a, SKPoint b, SKPoint p)
    {
        float abx = b.X - a.X, aby = b.Y - a.Y;
        float len2 = abx * abx + aby * aby;
        float t = len2 < 1e-6f ? 0 : Math.Clamp(((p.X - a.X) * abx + (p.Y - a.Y) * aby) / len2, 0, 1);
        float px = a.X + t * abx - p.X, py = a.Y + t * aby - p.Y;
        return MathF.Sqrt(px * px + py * py);
    }

    /// <summary>Berührt der Radierkreis (Mittelpunkt c, Radius r) den Strich?</summary>
    public static bool HitsStroke(StrokeElement s, SKPoint c, float r)
    {
        float rr = r + s.Width / 2f;
        var pts = s.Points;
        if (pts.Count == 1)
        {
            float dx0 = pts[0].X - c.X, dy0 = pts[0].Y - c.Y;
            return MathF.Sqrt(dx0 * dx0 + dy0 * dy0) <= rr;
        }
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            var a = new SKPoint(pts[i].X, pts[i].Y);
            var b = new SKPoint(pts[i + 1].X, pts[i + 1].Y);
            if (SegmentDistance(a, b, c) <= rr) return true;
        }
        return false;
    }

    /// <summary>Berührt der Radierkreis ein Nicht-Strich-Element (grob über seine Fläche)?</summary>
    public static bool HitsOther(WbElement el, SKPoint c, float r)
    {
        var b = WbRenderer.ElementBounds(el);
        b.Inflate(r, r);
        return b.Contains(c.X, c.Y);
    }

    /// <summary>
    /// Zerlegt einen Strich in die Teilstücke **außerhalb** des Radierkreises.
    /// Leere Liste = der Strich wird komplett entfernt.
    /// </summary>
    public static List<WbElement> SplitStroke(StrokeElement s, SKPoint c, float rr)
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
}
