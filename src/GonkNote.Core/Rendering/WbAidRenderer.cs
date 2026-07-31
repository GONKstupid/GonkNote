using System;
using System.IO;
using SkiaSharp;

namespace GonkNote.Core.Rendering;

/// <summary>
/// Zeichnet das Geodreieck-Overlay. Genommen wird die erste Grafik, die sich findet:
/// die eigene des Nutzers aus <c>%APPDATA%\GonkNote</c>, sonst die mitgelieferte neben
/// der Exe (je Theme eine Variante). Fehlen beide, entsteht die Kontur im Code.
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

    /// <summary>
    /// Wo eigene Geodreieck-Grafiken liegen dürfen:
    /// <c>%APPDATA%\GonkNote\Geodreieck-Light.svg</c> bzw. <c>-Dark.svg</c>.
    /// <para>
    /// Eine Datei hier gewinnt gegen die mitgelieferte aus <see cref="AppAssetFolder"/> –
    /// dasselbe Muster wie bei Stickern und Cover-Vorlagen.
    /// </para>
    /// </summary>
    public static string UserAssetFolder
    {
        get => _userAssetFolder ??= Platform.AppPaths.Current.DataFolder;
        set => _userAssetFolder = value;
    }

    private static string? _userAssetFolder;

    /// <summary>
    /// Die mitgelieferten Geodreiecke neben der Exe (<c>Assets\Geodreieck-*.svg</c>).
    /// <para>
    /// Fehlen beide Quellen, zeichnet <see cref="DrawSetSquare"/> die Kontur selbst –
    /// dann ohne Skalen, aber maßgleich.
    /// </para>
    /// </summary>
    public static string AppAssetFolder
    {
        get => _appAssetFolder ??= Platform.AppPaths.AppSubfolder("Assets");
        set => _appAssetFolder = value;
    }

    private static string? _appAssetFolder;

    /// <summary>
    /// SVG des jeweiligen Themes, beim ersten Zugriff geladen; null, wenn keine zu finden
    /// ist. Zuerst zählt die eigene Datei des Nutzers, danach die mitgelieferte. Die
    /// Erwartung an beide ist dieselbe Geometrie wie beim Eigenbau: ein 16-cm-Geodreieck,
    /// Hypotenusen-Mitte im Ursprung der viewBox-Vermessung.
    /// </summary>
    private static SKPicture? SetSquarePicture(bool dark)
    {
        var cached = dark ? _svgDark : _svgLight;
        if (cached == null)
        {
            try
            {
                string name = dark ? "Geodreieck-Dark.svg" : "Geodreieck-Light.svg";
                string file = Path.Combine(UserAssetFolder, name);
                if (!File.Exists(file)) file = Path.Combine(AppAssetFolder, name);
                if (!File.Exists(file)) return null;

                var svg = new Svg.Skia.SKSvg();
                using (var stream = File.OpenRead(file)) svg.Load(stream);
                if (dark) _svgDark = svg; else _svgLight = svg;
                cached = svg;
            }
            catch
            {
                return null;   // fehlende/kaputte Datei → Eigenbau unten
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

        // Eigenbau: schlichte Glas-Kontur ohne Skalen. Greift nur, wenn weder eine eigene
        // noch die mitgelieferte Grafik da ist – maßgleich, damit Einrasten und Drehen
        // auch dann stimmen.
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
