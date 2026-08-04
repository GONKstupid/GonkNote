using System;
using System.Collections.Generic;
using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Editing;

/// <summary>
/// Trefferprüfung: Was liegt unter dem Zeiger, und was hat das Lasso eingefangen?
/// <para>
/// <b>Warum das hier steht und nicht im Kopf:</b> es ist reine Geometrie — kein Pixel wird
/// gezeichnet, keine Eingabe entgegengenommen. Nach der Faustregel aus HANDOFF §3 gehört
/// damit alles davon nach Core. Bis Phase 3 lag es privat in
/// <c>WhiteboardView.Selection.cs</c> des WPF-Kopfs; der Linux-Kopf hätte es sonst Zeile für
/// Zeile abschreiben müssen, und zwei Fassungen derselben Geometrie driften auseinander,
/// ohne dass es jemandem auffällt — die Auswahl säße dann je Kopf ein paar Pixel anders.
/// </para>
/// <para>
/// <b>Beide Köpfe rechnen hiermit</b> — der Avalonia-Kopf seit Phase 3, der WPF-Kopf seit
/// dem 2026-08-04 (HANDOFF §4.13). Dazwischen lag die private Fassung bewusst noch in
/// <c>WhiteboardView.Selection.cs</c>: sie ließ sich auf dem Linux-Laptop nicht bauen, und
/// ein Umbau ohne Gegenprobe am laufenden Programm wäre genau die Art Änderung, vor der §7
/// warnt. Die Gegenprobe ist nachgeholt und hat <b>pixelgleiche</b> Aufnahmen ergeben.
/// Wächter: <c>TrefferTests</c> — sie halten damit heute beide Köpfe.
/// </para>
/// <para>
/// <b>Was im Kopf bleibt, hängt am Steuerelement:</b> die Toleranzen (<c>5f/Zoom</c>,
/// <c>12f/Zoom</c>), der Zustand und die Griffe. Gerechnet wird hier.
/// </para>
/// </summary>
public static class WbHit
{
    /// <summary>
    /// Dreht einen Punkt um <paramref name="pivot"/>. Wird gebraucht, um den Zeiger in den
    /// **ungedrehten** Raum eines Elements zurückzuholen: die Geometrie eines Elements liegt
    /// immer achsenparallel, gedreht wird erst beim Zeichnen.
    /// </summary>
    public static SKPoint Rotate(SKPoint p, SKPoint pivot, float degrees)
    {
        float r = degrees * MathF.PI / 180f, cos = MathF.Cos(r), sin = MathF.Sin(r);
        float dx = p.X - pivot.X, dy = p.Y - pivot.Y;
        return new SKPoint(pivot.X + dx * cos - dy * sin, pivot.Y + dx * sin + dy * cos);
    }

    /// <summary>Abstand des Zeigers zur Kontur einer Form.</summary>
    public static float ShapeOutlineDistance(ShapeElement sh, SKPoint c)
    {
        var p1 = new SKPoint(sh.X1, sh.Y1);
        var p2 = new SKPoint(sh.X2, sh.Y2);
        switch (sh.Shape)
        {
            case ShapeKind.Line:
            case ShapeKind.Arrow:
                return WbErase.SegmentDistance(p1, p2, c);

            case ShapeKind.Rectangle:
            {
                var r = SKRect.Create(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                      Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));
                var tl = new SKPoint(r.Left, r.Top);
                var tr = new SKPoint(r.Right, r.Top);
                var br = new SKPoint(r.Right, r.Bottom);
                var bl = new SKPoint(r.Left, r.Bottom);
                return Math.Min(Math.Min(WbErase.SegmentDistance(tl, tr, c), WbErase.SegmentDistance(tr, br, c)),
                                Math.Min(WbErase.SegmentDistance(br, bl, c), WbErase.SegmentDistance(bl, tl, c)));
            }

            case ShapeKind.Ellipse:
            {
                float cx = (p1.X + p2.X) / 2f, cy = (p1.Y + p2.Y) / 2f;
                float rx = Math.Max(1f, Math.Abs(p2.X - p1.X) / 2f);
                float ry = Math.Max(1f, Math.Abs(p2.Y - p1.Y) / 2f);
                // Abstand grob über die normalisierte Radialdistanz — genau genug für einen
                // Zeiger, und ohne die Newton-Iteration einer exakten Ellipsen-Projektion.
                float nx = (c.X - cx) / rx, ny = (c.Y - cy) / ry;
                float d = MathF.Sqrt(nx * nx + ny * ny);
                return Math.Abs(d - 1f) * Math.Min(rx, ry);
            }

            case ShapeKind.Triangle:
            {
                var (a, b, cc) = WbRenderer.TrianglePoints(sh);
                return Math.Min(WbErase.SegmentDistance(a, b, c),
                       Math.Min(WbErase.SegmentDistance(b, cc, c), WbErase.SegmentDistance(cc, a, c)));
            }

            default:
                return float.MaxValue;
        }
    }

    /// <summary>
    /// Liegt der Zeiger (Radius <paramref name="tolerance"/>) auf dem Element?
    /// <para>
    /// Striche und Formen zählen nur bei Berührung der **Linie**, nicht der Umschließung —
    /// sonst fängt ein weit ausholender Strich alles darunter mit ein. Bilder und Zettel
    /// zählen als Fläche, weil sie eine sind.
    /// </para>
    /// </summary>
    public static bool Hit(WbElement el, SKPoint c, float tolerance)
    {
        if (el.Rotation != 0f)
        {
            var eb = WbRenderer.ElementBounds(el);
            c = Rotate(c, new SKPoint(eb.MidX, eb.MidY), -el.Rotation);
        }

        switch (el)
        {
            case StrokeElement s:
            {
                float rr = tolerance + s.Width / 2f;
                for (int i = 0; i < s.Points.Count; i++)
                    if (SegmentOrPointDistance(s.Points, i, c) <= rr) return true;
                return false;
            }

            case ShapeElement sh:
                return ShapeOutlineDistance(sh, c) <= tolerance + sh.StrokeWidth / 2f;

            case TextElement t:
            {
                var b = WbRenderer.TextBounds(t);
                b.Inflate(tolerance, tolerance);
                return b.Contains(c);
            }

            case ImageElement im:
                return SKRect.Create(im.X, im.Y, im.Width, im.Height).Contains(c);

            case StickyNoteElement sn:
                return SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height).Contains(c);

            default:
                return false;
        }
    }

    /// <summary>Abstand zum Segment ab <paramref name="i"/>; beim letzten Punkt zum Punkt selbst.</summary>
    public static float SegmentOrPointDistance(List<WbPoint> pts, int i, SKPoint c)
    {
        var a = new SKPoint(pts[i].X, pts[i].Y);
        if (i + 1 >= pts.Count) return SKPoint.Distance(a, c);
        var b = new SKPoint(pts[i + 1].X, pts[i + 1].Y);
        return WbErase.SegmentDistance(a, b, c);
    }

    /// <summary>
    /// Oberstes Element unter dem Zeiger. **Vordergrund vor Bildern**: sonst greift man
    /// über einer importierten PDF-Seite immer die Seite und nie den Strich darauf.
    /// </summary>
    public static WbElement? Topmost(IReadOnlyList<WbElement> elements, SKPoint c, float tolerance)
    {
        for (int pass = 0; pass < 2; pass++)
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                var el = elements[i];
                bool isImage = el is ImageElement;
                if (pass == 0 && isImage) continue;    // erst der Vordergrund …
                if (pass == 1 && !isImage) continue;   // … dann die Bilder
                if (Hit(el, c, tolerance)) return el;
            }
        return null;
    }

    /// <summary>
    /// Was das Lasso eingefangen hat. **Nur ~vollständig (≥ 95 %) Umschlossenes zählt**
    /// (Nutzer-Wunsch aus V1): so lässt sich ein Strich greifen, ohne die PDF-Seite oder
    /// die Form dahinter mitzunehmen.
    /// </summary>
    public static List<WbElement> InsideLasso(IReadOnlyList<WbElement> elements, IReadOnlyList<SKPoint> lasso)
    {
        var treffer = new List<WbElement>();
        if (lasso.Count < 3) return treffer;

        using var path = new SKPath();
        path.MoveTo(lasso[0]);
        for (int i = 1; i < lasso.Count; i++) path.LineTo(lasso[i]);
        path.Close();

        foreach (var el in elements)
        {
            bool drin = el switch
            {
                StrokeElement s => s.Points.Count > 0 && AnteilDrin(path, s.Points) >= 0.95f,
                ShapeElement sh => AlleEckenDrin(path, WbRenderer.ElementBounds(sh)),
                TextElement t => AlleEckenDrin(path, WbRenderer.TextBounds(t)),
                ImageElement im => AlleEckenDrin(path, SKRect.Create(im.X, im.Y, im.Width, im.Height)),
                StickyNoteElement sn => AlleEckenDrin(path, SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height)),
                _ => false,
            };
            if (drin) treffer.Add(el);
        }
        return treffer;
    }

    private static float AnteilDrin(SKPath path, List<WbPoint> pts)
    {
        int n = 0;
        foreach (var p in pts) if (path.Contains(p.X, p.Y)) n++;
        return (float)n / pts.Count;
    }

    /// <summary>Alle vier Ecken **und** die Mitte eines Rechtecks liegen im Pfad.</summary>
    private static bool AlleEckenDrin(SKPath path, SKRect r) =>
        path.Contains(r.Left, r.Top) && path.Contains(r.Right, r.Top) &&
        path.Contains(r.Left, r.Bottom) && path.Contains(r.Right, r.Bottom) &&
        path.Contains(r.MidX, r.MidY);

    /// <summary>Umschließung einer Auswahl (Vereinigung aller Element-Umschließungen).</summary>
    public static SKRect Bounds(IEnumerable<WbElement> elements)
    {
        bool erste = true;
        SKRect r = SKRect.Empty;
        foreach (var el in elements)
        {
            var b = WbRenderer.ElementBounds(el);
            if (erste) { r = b; erste = false; }
            else r = SKRect.Union(r, b);
        }
        return r;
    }
}
