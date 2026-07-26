using System;
using System.Reflection;
using GonkNote.Editing;
using SkiaSharp;

namespace GonkNote.Rendering;

/// <summary>
/// Zeichnet die Zeichenhilfen (Lineal &amp; Geodreieck). Aus der WPF-App gehoben, damit WPF und
/// der Avalonia-Port dieselbe Darstellung nutzen (HANDOFF §9.3e).
///
/// Das Geodreieck kommt aus den SVG-Assets des Nutzers (je Theme eine Variante, Unterschied ist
/// die Bandfarbe). Vermessene SVG-Geometrie (viewBox 2520×1680): Hypotenuse 2515,2 units =
/// 16-cm-Geodreieck → 157,2 units/cm; der Hypotenusen-Mittelpunkt wird auf das Interaktions-
/// zentrum gelegt und auf 1 Geodreieck-cm = 1 Seiten-cm skaliert. Dadurch deckt sich der
/// Aufdruck exakt mit dem Einrast-/Dreh-Polygon (<see cref="WbDrawAid.SetSquareHalfHyp"/>).
/// </summary>
public static class WbAidRenderer
{
    private const float SvgUnitsPerCm = 157.2f;
    private static readonly SKPoint SvgMid = new(1259.85f, 1468.85f);

    private static Svg.Skia.SKSvg? _svgLight, _svgDark;

    /// <summary>SVG des jeweiligen Themes, beim ersten Zugriff aus der Ressource geladen.</summary>
    private static SKPicture? SetSquarePicture(bool dark)
    {
        var cached = dark ? _svgDark : _svgLight;
        if (cached == null)
        {
            try
            {
                string name = dark
                    ? "GonkNote.Core.Assets.Geodreieck-Dark.svg"
                    : "GonkNote.Core.Assets.Geodreieck-Light.svg";
                using var stream = typeof(WbAidRenderer).Assembly.GetManifestResourceStream(name);
                if (stream == null) return null;

                var svg = new Svg.Skia.SKSvg();
                svg.Load(stream);
                if (dark) _svgDark = svg; else _svgLight = svg;
                cached = svg;
            }
            catch
            {
                return null;   // fehlende/kaputte Ressource → Notnagel unten
            }
        }
        return cached?.Picture;
    }

    /// <summary>Zeichnet das Geodreieck um <paramref name="center"/> mit der Drehung in Grad.</summary>
    public static void DrawSetSquare(SKCanvas canvas, SKPoint center, float angleDeg,
                                     float zoom, bool dark)
    {
        var picture = SetSquarePicture(dark);
        if (picture != null)
        {
            float s = WbDrawAid.PxPerCm / SvgUnitsPerCm;
            canvas.Save();
            canvas.Translate(center.X, center.Y);
            canvas.RotateDegrees(angleDeg);
            canvas.Scale(s);
            canvas.Translate(-SvgMid.X, -SvgMid.Y);
            canvas.DrawPicture(picture);
            canvas.Restore();
            return;
        }

        // Notnagel: schlichte Glas-Kontur, falls die SVG-Ressource nicht ladbar ist
        float rad = angleDeg * MathF.PI / 180f;
        float cos = MathF.Cos(rad), sin = MathF.Sin(rad);
        SKPoint P(float u, float v)
        {
            float x = u * WbDrawAid.PxPerCm, y = -v * WbDrawAid.PxPerCm;
            return new SKPoint(center.X + x * cos - y * sin, center.Y + x * sin + y * cos);
        }

        using var path = new SKPath();
        path.MoveTo(P(-8f, 0)); path.LineTo(P(8f, 0)); path.LineTo(P(0, 8f)); path.Close();
        using (var fill = new SKPaint { Color = new SKColor(0xF2, 0xF3, 0xF8, 165), IsAntialias = true })
            canvas.DrawPath(path, fill);
        using var edge = new SKPaint
        {
            Color = new SKColor(46, 49, 82),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f / zoom,
            IsAntialias = true,
        };
        canvas.DrawPath(path, edge);
    }

    /// <summary>Zeichnet die aktive Hilfe samt Dreh-Griff.</summary>
    public static void DrawAid(SKCanvas canvas, WbDrawAid aid, double zoom, SKColor accent, bool dark)
    {
        if (!aid.IsActive) return;
        float z = (float)zoom;

        if (aid.Kind == DrawAidKind.SetSquare)
        {
            // Das Geodreieck zeichnet sich komplett selbst (Körper, Kanten, Aufdruck)
            DrawSetSquare(canvas, aid.Center, aid.AngleDeg, z, dark);
        }
        else
        {
            var poly = aid.WorldPolygon();
            using var path = new SKPath();
            path.MoveTo(poly[0]);
            for (int i = 1; i < poly.Length; i++) path.LineTo(poly[i]);
            path.Close();

            using (var fill = new SKPaint { Color = new SKColor(30, 41, 59, 40), IsAntialias = true })
                canvas.DrawPath(path, fill);
            using (var edge = new SKPaint
            {
                Color = accent, Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f / z, IsAntialias = true,
            })
                canvas.DrawPath(path, edge);

            DrawCmScale(canvas, aid, accent, WbDrawAid.RulerHalfWidth, WbDrawAid.RulerLength / 2f, z);
        }

        // Dreh-Griff
        var h = aid.HandleCenter(zoom);
        using (var hf = new SKPaint { Color = accent, IsAntialias = true })
            canvas.DrawCircle(h, 6f / z, hf);
        using (var hr = new SKPaint
        {
            Color = SKColors.White, Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.4f / z, IsAntialias = true,
        })
            canvas.DrawCircle(h, 6f / z, hr);
    }

    /// <summary>cm-Skala entlang einer Kante (lokal bei y=edgeY), Ticks nach innen (−y).</summary>
    private static void DrawCmScale(SKCanvas canvas, WbDrawAid aid, SKColor accent,
                                    float edgeY, float halfLen, float zoom)
    {
        using var tick = new SKPaint
        {
            Color = accent.WithAlpha(210),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f / zoom,
            IsAntialias = true,
        };

        int k = 0;
        for (float u = 0; u <= halfLen; u += WbDrawAid.PxPerCm, k++)
        {
            float len = (k % 5 == 0) ? 9f : 5f;
            canvas.DrawLine(aid.LocalToWorld(u, edgeY), aid.LocalToWorld(u, edgeY - len), tick);
            if (u > 0)
                canvas.DrawLine(aid.LocalToWorld(-u, edgeY), aid.LocalToWorld(-u, edgeY - len), tick);
        }
    }
}
