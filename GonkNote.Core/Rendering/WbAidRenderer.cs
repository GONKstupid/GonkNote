using System;
using SkiaSharp;

namespace GonkNote.Rendering;

/// <summary>
/// Zeichnet das Geodreieck-Overlay aus den SVG-Assets des Nutzers (je Theme eine Variante,
/// Unterschied ist die Bandfarbe).
///
/// Vermessene SVG-Geometrie (viewBox 2520×1680): Hypotenuse von (2,2|1468,9) bis
/// (2517,5|1468,9) = 2515,2 units, Spitze bei (1259,8|210) → 16-cm-Geodreieck →
/// 157,2 units/cm. Beim Zeichnen kommt der Hypotenusen-Mittelpunkt des SVG auf das
/// Interaktionszentrum, skaliert auf 1 Geodreieck-cm = 1 Seiten-cm. Dadurch deckt sich der
/// Aufdruck exakt mit dem Einrast-/Dreh-Polygon der View (halbe Hypotenuse = 8 cm).
/// </summary>
public static class WbAidRenderer
{
    /// <summary>Umrechnung Zentimeter → Canvas-Einheiten (96 dpi).</summary>
    public const float PxPerCm = 37.795f;

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

    /// <summary>
    /// Zeichnet das Geodreieck um <paramref name="center"/> mit der Drehung
    /// <paramref name="angleDeg"/> (Grad).
    /// </summary>
    public static void DrawSetSquare(SKCanvas canvas, SKPoint center, float angleDeg,
                                     float zoom, bool dark)
    {
        var picture = SetSquarePicture(dark);
        if (picture != null)
        {
            float s = PxPerCm / SvgUnitsPerCm;
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
            float x = u * PxPerCm, y = -v * PxPerCm;
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
}
