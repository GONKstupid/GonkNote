using System;
using System.Collections.Generic;
using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Editing;

/// <summary>
/// Geometrie fürs **punktgenaue** Radieren: ein Strich wird an der berührten Stelle
/// aufgetrennt, die Reststücke bleiben stehen. Liegt in Core, weil es Editier- und keine
/// View-Logik ist (Leitlinie §5: Kernlogik von den Views entkoppeln).
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

    /// <summary>
    /// Zerlegt einen Strich in die Teilstücke **außerhalb** des Radierkreises
    /// (Mittelpunkt <paramref name="c"/>, Radius <paramref name="rr"/>).
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
