using System;
using SkiaSharp;

namespace GonkNote.Editing;

/// <summary>Welche Zeichenhilfe gerade aktiv ist.</summary>
public enum DrawAidKind
{
    None,
    Ruler,
    SetSquare,
}

/// <summary>
/// Zeichenhilfen (Lineal &amp; Geodreieck) — Geometrie und Einrasten, plattformneutral.
/// Aus der WPF-App gehoben (HANDOFF §9.3e), damit WPF und Avalonia dieselbe Logik nutzen.
/// Die Hilfen sind **transient**: sie werden nicht in der Datenbank gespeichert.
/// </summary>
public sealed class WbDrawAid
{
    /// <summary>Umrechnung Zentimeter → Canvas-Einheiten (96 dpi).</summary>
    public const float PxPerCm = 37.795f;

    public const float RulerLength = 680f;
    public const float RulerHalfWidth = 26f;

    /// <summary>Abstand, ab dem ein Strichstart auf eine Kante einrastet.</summary>
    public const float SnapDistance = 26f;

    /// <summary>Halbe Hypotenuse = Höhe = 8 cm (16-cm-Geodreieck, wie die SVG-Assets).</summary>
    public static readonly float SetSquareHalfHyp = 8f * PxPerCm;

    public DrawAidKind Kind { get; private set; } = DrawAidKind.None;
    public SKPoint Center { get; set; }
    public float AngleDeg { get; set; }

    /// <summary>Wurde die Hilfe schon einmal platziert? (Sonst mittig in den Blick setzen.)</summary>
    public bool Placed { get; private set; }

    public bool IsActive => Kind != DrawAidKind.None;

    /// <summary>Schaltet eine Hilfe ein bzw. bei erneutem Aufruf aus (beide schließen sich aus).</summary>
    public void Toggle(DrawAidKind kind, SKPoint viewCenter)
    {
        Kind = Kind == kind ? DrawAidKind.None : kind;
        if (Kind != DrawAidKind.None && !Placed)
        {
            Center = viewCenter;
            AngleDeg = 0f;
            Placed = true;
        }
    }

    // ---- Achsen und Polygon ---------------------------------------------------------------

    /// <summary>Richtungs- (entlang) und Normalenvektor (quer) der Ausrichtung.</summary>
    public (SKPoint Dir, SKPoint Nrm) Axes()
    {
        float a = AngleDeg * MathF.PI / 180f;
        var d = new SKPoint(MathF.Cos(a), MathF.Sin(a));
        return (d, new SKPoint(-d.Y, d.X));
    }

    /// <summary>Lokalen Punkt (u entlang, v quer) in Weltkoordinaten wandeln.</summary>
    public SKPoint LocalToWorld(float u, float v)
    {
        var (d, n) = Axes();
        return new SKPoint(Center.X + u * d.X + v * n.X, Center.Y + u * d.Y + v * n.Y);
    }

    /// <summary>Eckpunkte der aktiven Hilfe in lokalen Koordinaten.</summary>
    public SKPoint[] LocalPolygon() => Kind switch
    {
        DrawAidKind.Ruler => new[]
        {
            new SKPoint(-RulerLength / 2f, -RulerHalfWidth), new SKPoint(RulerLength / 2f, -RulerHalfWidth),
            new SKPoint(RulerLength / 2f, RulerHalfWidth), new SKPoint(-RulerLength / 2f, RulerHalfWidth),
        },
        // Rechtwinklig-gleichschenklig: Hypotenuse unten, rechter Winkel oben
        DrawAidKind.SetSquare => new[]
        {
            new SKPoint(-SetSquareHalfHyp, 0f), new SKPoint(SetSquareHalfHyp, 0f),
            new SKPoint(0f, -SetSquareHalfHyp),
        },
        _ => Array.Empty<SKPoint>(),
    };

    public SKPoint[] WorldPolygon()
    {
        var lp = LocalPolygon();
        var wp = new SKPoint[lp.Length];
        for (int i = 0; i < lp.Length; i++) wp[i] = LocalToWorld(lp[i].X, lp[i].Y);
        return wp;
    }

    /// <summary>Kantenpaare (Indizes ins Polygon), auf die eingerastet wird.</summary>
    public (int A, int B)[] EdgePairs() => Kind switch
    {
        DrawAidKind.Ruler => new[] { (0, 1), (3, 2) },              // beide Längskanten
        DrawAidKind.SetSquare => new[] { (0, 1), (1, 2), (2, 0) },  // Hypotenuse + zwei Katheten
        _ => Array.Empty<(int, int)>(),
    };

    /// <summary>Lokale x-Position des rechten Endes (dort sitzt der Dreh-Griff).</summary>
    public float RightEndX =>
        Kind == DrawAidKind.SetSquare ? SetSquareHalfHyp : RulerLength / 2f;

    /// <summary>Mittelpunkt des Dreh-Griffs; <paramref name="zoom"/> hält ihn bildschirmgroß.</summary>
    public SKPoint HandleCenter(double zoom)
    {
        var (d, _) = Axes();
        var end = LocalToWorld(RightEndX, 0f);
        float ext = 16f / (float)zoom;
        return new SKPoint(end.X + d.X * ext, end.Y + d.Y * ext);
    }

    public bool HandleHit(SKPoint c, double zoom)
    {
        var h = HandleCenter(zoom);
        float r = 13f / (float)zoom;
        float dx = c.X - h.X, dy = c.Y - h.Y;
        return dx * dx + dy * dy <= r * r;
    }

    public bool BodyContains(SKPoint c) =>
        IsActive && PointInPolygon(WorldPolygon(), c);

    public static bool PointInPolygon(SKPoint[] poly, SKPoint p)
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

    // ---- Einrasten -------------------------------------------------------------------------

    private bool _snapActive;
    private SKPoint _snapOrigin;
    private SKPoint _snapDir;

    public bool SnapActive => _snapActive;

    /// <summary>
    /// Prüft, ob ein Strichstart nahe einer Kante liegt, und rastet dann auf diese Kante ein.
    /// </summary>
    public bool TryActivateSnap(SKPoint c)
    {
        _snapActive = false;
        if (!IsActive) return false;

        var poly = WorldPolygon();
        float best = float.MaxValue;
        SKPoint bestOrigin = default, bestDir = default;

        foreach (var (ia, ib) in EdgePairs())
        {
            var a = poly[ia];
            var b = poly[ib];
            float ex = b.X - a.X, ey = b.Y - a.Y;
            float len = MathF.Sqrt(ex * ex + ey * ey);
            if (len < 1f) continue;

            var dir = new SKPoint(ex / len, ey / len);
            float t = (c.X - a.X) * dir.X + (c.Y - a.Y) * dir.Y;
            if (t < -80f || t > len + 80f) continue;      // etwas Überstand an den Enden

            var proj = new SKPoint(a.X + dir.X * t, a.Y + dir.Y * t);
            float pd = MathF.Sqrt((c.X - proj.X) * (c.X - proj.X) + (c.Y - proj.Y) * (c.Y - proj.Y));
            if (pd <= SnapDistance && pd < best) { best = pd; bestOrigin = a; bestDir = dir; }
        }

        if (best == float.MaxValue) return false;
        _snapOrigin = bestOrigin;
        _snapDir = bestDir;
        _snapActive = true;
        return true;
    }

    /// <summary>Projiziert einen Punkt auf die eingerastete Kantenlinie (sonst unverändert).</summary>
    public SKPoint ApplySnap(SKPoint p)
    {
        if (!_snapActive) return p;
        float t = (p.X - _snapOrigin.X) * _snapDir.X + (p.Y - _snapOrigin.Y) * _snapDir.Y;
        return new SKPoint(_snapOrigin.X + _snapDir.X * t, _snapOrigin.Y + _snapDir.Y * t);
    }

    public void ClearSnap() => _snapActive = false;

    /// <summary>Rastet den Winkel auf 15°-Schritte, wenn er nah genug dran liegt.</summary>
    public static float SnapAngle(float deg)
    {
        float snapped = MathF.Round(deg / 15f) * 15f;
        return MathF.Abs(deg - snapped) <= 3f ? snapped : deg;
    }
}
