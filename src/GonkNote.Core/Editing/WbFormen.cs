using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Editing;

/// <summary>
/// Der <b>Formen-Stift</b>: aus einem freihändigen Zug die gemeinte Form erkennen (Gerade,
/// Streckenzug, Rechteck, Ellipse) — und, wenn keine erkennbar ist, die Kurve glätten.
///
/// <para>
/// <b>Sie stand bis Phase 5, Schritt ①c als <c>WhiteboardView.Shapes.cs</c> im WPF-Kopf</b>,
/// und das war dieselbe Verwechslung wie beim Tafel-Export (§4.77): die Datei lag in einem
/// Kopf, weil sie dort <i>entstanden</i> ist, nicht weil sie dorthin gehört. **Von den 305
/// Zeilen war keine an WPF gebunden** — es sind Punktlisten hinein, Modellobjekte hinaus.
/// Was wirklich am Kopf hing, waren zwei Werte: die aktuelle Tinte und die Strichbreite.
/// <b>Sie sind jetzt Parameter</b>, und damit ist der Rest hier richtig aufgehoben.
/// </para>
/// <para>
/// <b>Was der Umzug zusätzlich einbringt, ist kein Nebeneffekt, sondern der Punkt:</b> Die
/// Formerkennung war bis heute <b>durch keinen einzigen Wächter gedeckt</b> — sie ließ sich
/// nicht rufen, ohne ein WPF-Fenster zu bauen. Jetzt geht das, und
/// <c>Core.Tests/FormenstiftTests.cs</c> tut es.
/// </para>
/// <para>
/// <b>Das Vorbild ist GoodNotes:</b> ein fast gerader Zug wird eine perfekte Gerade (mit
/// Einrasten auf 45°-Schritte), ein geschlossener runder ein Kreis oder eine Ellipse, ein
/// geschlossener eckiger ein Rechteck oder Polygon. Wird nichts erkannt, kommt
/// <see cref="Glaetten"/> zum Zug — der Strich bleibt, er wird nur ruhiger.
/// </para>
/// </summary>
public static class WbFormen
{
    /// <summary>
    /// Erkennt aus <paramref name="punkte"/> die gemeinte Grundform. <b>Liefert
    /// <c>null</c>, wenn nichts erkannt wurde</b> — dann gehört der Zug durch
    /// <see cref="Glaetten"/> und wird als Strich abgelegt.
    ///
    /// <para>
    /// <paramref name="farbe"/> und <paramref name="breite"/> sind das, was vorher am Kopf
    /// hing (<c>CurrentInkHex()</c> und <c>_width</c>). <b>Sie stehen hier als Parameter und
    /// werden nicht selbst ermittelt:</b> welche Tinte gerade gilt, hängt an der Seite, am
    /// Theme und an der Farbkachel — das ist Sache des Kopfes, und der beantwortet sie in
    /// beiden Fassungen schon.
    /// </para>
    /// </summary>
    public static WbElement? Erkennen(List<WbPoint> punkte, string farbe, float breite)
    {
        if (punkte.Count < 4) return null;

        float len = 0;
        for (int i = 1; i < punkte.Count; i++)
            len += Abstand(punkte[i - 1], punkte[i]);
        if (len < 24) return null;

        var a = punkte[0];
        var b = punkte[^1];
        float sehne = Abstand(a, b);

        // Gerade Linie: Punkte weichen kaum von der Sehne ab
        if (sehne > len * 0.8f && MaxSehnenabstand(punkte) <= Math.Max(4f, len * 0.05f))
            return GeradeEinrasten(a, b, farbe, breite);

        bool geschlossen = sehne <= Math.Max(18f, len * 0.16f);
        if (!geschlossen)
        {
            // Offener Zug mit wenigen klaren Ecken → perfekter Streckenzug
            var offen = DouglasPeucker(punkte, Math.Max(6f, len * 0.03f));
            if (offen.Count == 2) return GeradeEinrasten(offen[0], offen[^1], farbe, breite);
            if (offen.Count <= 6) return Streckenzug(offen, geschlossen: false, farbe, breite);
            return null;
        }

        // Geschlossener Zug: wenige Ecken → Polygon, sonst Ellipse prüfen
        var polygon = GeschlosseneEcken(punkte, Math.Max(7f, len * 0.025f));

        if (polygon.Count == 4 && RechteckEinrasten(polygon, farbe, breite) is { } rechteck)
            return rechteck;
        if (polygon.Count is 3 or 4) return Streckenzug(polygon, geschlossen: true, farbe, breite);

        if (EllipsenFehler(punkte) <= 0.14f) return EllipseEinrasten(punkte, farbe, breite);
        if (polygon.Count <= 6) return Streckenzug(polygon, geschlossen: true, farbe, breite);
        return null;
    }

    /// <summary>
    /// Glättstift: gleichmäßige Abstände (Resampling) und dreifacher gleitender Mittelwert.
    /// <b>Anfang und Ende bleiben stehen</b> — ein Strich, der beim Glätten von seinem
    /// Startpunkt wegrutscht, sieht aus, als hätte man danebengetroffen.
    /// </summary>
    public static List<WbPoint> Glaetten(List<WbPoint> punkte)
    {
        if (punkte.Count < 3) return punkte;

        var neu = Neuverteilen(punkte, 3f);
        for (int lauf = 0; lauf < 3 && neu.Count >= 3; lauf++)
        {
            var geglaettet = new List<WbPoint>(neu.Count) { neu[0] };
            for (int i = 1; i < neu.Count - 1; i++)
            {
                var a = neu[i - 1];
                var b = neu[i];
                var c = neu[i + 1];
                geglaettet.Add(new WbPoint(
                    (a.X + 2 * b.X + c.X) / 4f,
                    (a.Y + 2 * b.Y + c.Y) / 4f,
                    (a.P + 2 * b.P + c.P) / 4f));
            }
            geglaettet.Add(neu[^1]);
            neu = geglaettet;
        }
        return neu;
    }

    // ==================== Die einzelnen Formen ====================

    /// <summary>Perfekte Gerade; rastet nahe 0°/45°/90° exakt ein.</summary>
    private static ShapeElement GeradeEinrasten(WbPoint a, WbPoint b, string farbe, float breite)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y;
        float winkel = MathF.Atan2(dy, dx);
        const float schritt = MathF.PI / 4f;
        float gerastet = MathF.Round(winkel / schritt) * schritt;
        if (Math.Abs(gerastet - winkel) <= 6f * MathF.PI / 180f)
        {
            float l = MathF.Sqrt(dx * dx + dy * dy);
            dx = l * MathF.Cos(gerastet);
            dy = l * MathF.Sin(gerastet);
        }
        return new ShapeElement
        {
            Shape = ShapeKind.Line,
            X1 = a.X, Y1 = a.Y, X2 = a.X + dx, Y2 = a.Y + dy,
            Color = farbe, StrokeWidth = breite,
        };
    }

    /// <summary>Streckenzug aus den erkannten Eckpunkten – Segmente sind perfekt gerade.</summary>
    private static StrokeElement Streckenzug(List<WbPoint> ecken, bool geschlossen,
                                             string farbe, float breite)
    {
        var punkte = ecken.Select(p => new WbPoint(p.X, p.Y, 0.5f)).ToList();
        if (geschlossen) punkte.Add(new WbPoint(ecken[0].X, ecken[0].Y, 0.5f));
        return new StrokeElement
        {
            Points = punkte, Color = farbe, Width = breite, Kind = StrokeKind.Pen,
        };
    }

    /// <summary>Achsenparalleles Viereck rastet zum perfekten Rechteck ein, sonst null.</summary>
    private static ShapeElement? RechteckEinrasten(List<WbPoint> viereck, string farbe, float breite)
    {
        const float toleranz = 15f * MathF.PI / 180f;
        for (int i = 0; i < 4; i++)
        {
            var p = viereck[i];
            var q = viereck[(i + 1) % 4];
            float winkel = MathF.Atan2(Math.Abs(q.Y - p.Y), Math.Abs(q.X - p.X));
            if (winkel > toleranz && winkel < MathF.PI / 2f - toleranz) return null;
        }
        return new ShapeElement
        {
            Shape = ShapeKind.Rectangle,
            X1 = viereck.Min(p => p.X), Y1 = viereck.Min(p => p.Y),
            X2 = viereck.Max(p => p.X), Y2 = viereck.Max(p => p.Y),
            Color = farbe, StrokeWidth = breite,
        };
    }

    /// <summary>Perfekte Ellipse über der Bounding-Box; fast runde werden zum Kreis.</summary>
    private static ShapeElement EllipseEinrasten(List<WbPoint> punkte, string farbe, float breite)
    {
        var (minX, minY, maxX, maxY) = Umschliessung(punkte);
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
            Color = farbe, StrokeWidth = breite,
        };
    }

    // ==================== Die Geometrie dahinter ====================

    private static float Abstand(WbPoint a, WbPoint b)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static (float MinX, float MinY, float MaxX, float MaxY) Umschliessung(List<WbPoint> punkte)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in punkte)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
        }
        return (minX, minY, maxX, maxY);
    }

    private static float MaxSehnenabstand(List<WbPoint> punkte)
    {
        var a = new SKPoint(punkte[0].X, punkte[0].Y);
        var b = new SKPoint(punkte[^1].X, punkte[^1].Y);
        float max = 0;
        foreach (var p in punkte)
            max = Math.Max(max, WbErase.SegmentDistance(a, b, new SKPoint(p.X, p.Y)));
        return max;
    }

    /// <summary>Mittlere radiale Abweichung von der einbeschriebenen Ellipse (0 = perfekt).</summary>
    private static float EllipsenFehler(List<WbPoint> punkte)
    {
        var (minX, minY, maxX, maxY) = Umschliessung(punkte);
        float rx = Math.Max(1f, (maxX - minX) / 2f), ry = Math.Max(1f, (maxY - minY) / 2f);
        float cx = (minX + maxX) / 2f, cy = (minY + maxY) / 2f;

        float fehler = 0;
        foreach (var p in punkte)
        {
            float nx = (p.X - cx) / rx, ny = (p.Y - cy) / ry;
            fehler += Math.Abs(MathF.Sqrt(nx * nx + ny * ny) - 1f);
        }
        return fehler / punkte.Count;
    }

    /// <summary>
    /// Ecken eines geschlossenen Zugs. <b>Der Anfang wird an die centroid-fernste Stelle
    /// gedreht</b>, bevor Douglas-Peucker läuft — sonst hinge das Ergebnis davon ab, wo
    /// jemand zu zeichnen angefangen hat, und dasselbe Rechteck ergäbe je nach Startpunkt
    /// vier verschiedene Polygone.
    /// </summary>
    private static List<WbPoint> GeschlosseneEcken(List<WbPoint> punkte, float epsilon)
    {
        float cx = 0, cy = 0;
        foreach (var p in punkte) { cx += p.X; cy += p.Y; }
        cx /= punkte.Count; cy /= punkte.Count;

        int start = 0; float weitest = -1;
        for (int i = 0; i < punkte.Count; i++)
        {
            float dx = punkte[i].X - cx, dy = punkte[i].Y - cy;
            float d = dx * dx + dy * dy;
            if (d > weitest) { weitest = d; start = i; }
        }

        var gedreht = new List<WbPoint>(punkte.Count + 1);
        for (int i = 0; i < punkte.Count; i++) gedreht.Add(punkte[(start + i) % punkte.Count]);
        gedreht.Add(punkte[start]);

        var ecken = DouglasPeucker(gedreht, epsilon);
        ecken.RemoveAt(ecken.Count - 1); // Ende == Anfang
        return ecken;
    }

    private static List<WbPoint> DouglasPeucker(List<WbPoint> punkte, float epsilon)
    {
        if (punkte.Count < 3) return new List<WbPoint>(punkte);
        var behalten = new bool[punkte.Count];
        behalten[0] = behalten[^1] = true;
        DpRekursiv(punkte, 0, punkte.Count - 1, epsilon, behalten);

        var ergebnis = new List<WbPoint>();
        for (int i = 0; i < punkte.Count; i++)
            if (behalten[i]) ergebnis.Add(punkte[i]);
        return ergebnis;
    }

    private static void DpRekursiv(List<WbPoint> punkte, int lo, int hi, float eps, bool[] behalten)
    {
        if (hi <= lo + 1) return;
        var a = new SKPoint(punkte[lo].X, punkte[lo].Y);
        var b = new SKPoint(punkte[hi].X, punkte[hi].Y);
        float maxD = -1; int maxI = -1;
        for (int i = lo + 1; i < hi; i++)
        {
            float d = WbErase.SegmentDistance(a, b, new SKPoint(punkte[i].X, punkte[i].Y));
            if (d > maxD) { maxD = d; maxI = i; }
        }
        if (maxD <= eps) return;
        behalten[maxI] = true;
        DpRekursiv(punkte, lo, maxI, eps, behalten);
        DpRekursiv(punkte, maxI, hi, eps, behalten);
    }

    private static List<WbPoint> Neuverteilen(List<WbPoint> punkte, float abstand)
    {
        var ergebnis = new List<WbPoint> { punkte[0] };
        float uebertrag = 0;
        for (int i = 1; i < punkte.Count; i++)
        {
            var a = punkte[i - 1];
            var b = punkte[i];
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float segment = MathF.Sqrt(dx * dx + dy * dy);
            if (segment < 1e-5f) continue;

            float pos = abstand - uebertrag;
            while (pos <= segment)
            {
                float t = pos / segment;
                ergebnis.Add(new WbPoint(a.X + t * dx, a.Y + t * dy, a.P + t * (b.P - a.P)));
                pos += abstand;
            }
            uebertrag = segment - (pos - abstand);
        }
        if (ergebnis.Count < 2 ||
            Math.Abs(ergebnis[^1].X - punkte[^1].X) > 0.01f ||
            Math.Abs(ergebnis[^1].Y - punkte[^1].Y) > 0.01f)
            ergebnis.Add(new WbPoint(punkte[^1].X, punkte[^1].Y, punkte[^1].P));
        return ergebnis;
    }
}
