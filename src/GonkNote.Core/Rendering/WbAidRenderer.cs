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

    // ==================== Das Lineal und die Griffe (Phase 4.5, §4.59) ====================
    //
    // Bis dahin zeichnete der WPF-Kopf beides selbst (WhiteboardView.Aids.cs). Es ist reines
    // SkiaSharp, und der Linux-Kopf braucht dasselbe Bild — zwei Fassungen hießen ein Lineal,
    // dessen cm-Striche je Kopf woanders sitzen. Die Geometrie dazu steht in
    // Editing/WbZeichenhilfe.cs.

    /// <summary>
    /// Zeichnet die aktive Zeichenhilfe vollständig: Körper, Skala, Dreh-Griff — und beim
    /// Drehen die Gradzahl.
    ///
    /// <para>
    /// <b>Das Geodreieck zeichnet sich selbst</b> (aus seiner SVG, samt Aufdruck); für das
    /// Lineal entsteht das Bild hier. Der Dreh-Griff ist bei beiden derselbe.
    /// </para>
    /// </summary>
    /// <param name="akzent">Die Akzentfarbe des Kopfs — sie kommt von außen, weil Core keine Themes kennt.</param>
    /// <param name="dreht">Ob gerade gedreht wird; nur dann steht die Gradzahl daneben.</param>
    public static void DrawAid(SKCanvas canvas, Editing.Zeichenhilfe art, SKPoint mitte,
                               float winkelGrad, float zoom, bool dark, SKColor akzent, bool dreht)
    {
        if (art == Editing.Zeichenhilfe.Keine) return;

        if (art == Editing.Zeichenhilfe.Geodreieck)
            DrawSetSquare(canvas, mitte, winkelGrad, zoom, dark);
        else
            DrawRuler(canvas, mitte, winkelGrad, zoom, akzent);

        DrawAidHandle(canvas, art, mitte, winkelGrad, zoom, akzent);
        if (dreht) DrawAidAngle(canvas, art, mitte, winkelGrad, zoom);
    }

    /// <summary>Der Linealkörper mit cm-Skala an der oberen Längskante.</summary>
    public static void DrawRuler(SKCanvas canvas, SKPoint mitte, float winkelGrad, float zoom, SKColor akzent)
    {
        var eck = Editing.WbZeichenhilfe.UmrissWelt(Editing.Zeichenhilfe.Lineal, mitte, winkelGrad);

        using (var pfad = new SKPath())
        {
            pfad.MoveTo(eck[0]);
            for (int i = 1; i < eck.Length; i++) pfad.LineTo(eck[i]);
            pfad.Close();

            // Halbdurchsichtig: man muss sehen, was unter dem Lineal liegt — sonst legt man
            // es blind über den Strich, den man treffen will.
            using var fuellung = new SKPaint { Color = new SKColor(30, 41, 59, 40), IsAntialias = true };
            canvas.DrawPath(pfad, fuellung);
            using var kante = new SKPaint
            {
                Color = akzent, Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f / zoom, IsAntialias = true,
            };
            canvas.DrawPath(pfad, kante);
        }

        DrawCmScale(canvas, mitte, winkelGrad, zoom, akzent,
                    Editing.WbZeichenhilfe.LinealHalbBreite,
                    Editing.WbZeichenhilfe.LinealLaenge / 2f);
    }

    /// <summary>
    /// Die cm-Skala entlang einer Kante, Striche nach innen. <b>Jeder fünfte ist länger</b> —
    /// ohne diese Markierung zählt niemand über zehn Zentimeter hinweg richtig.
    /// </summary>
    public static void DrawCmScale(SKCanvas canvas, SKPoint mitte, float winkelGrad, float zoom,
                                   SKColor akzent, float kanteV, float halbeLaenge)
    {
        using var strich = new SKPaint
        {
            Color = akzent.WithAlpha(210), Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f / zoom, IsAntialias = true,
        };

        int k = 0;
        for (float u = 0; u <= halbeLaenge; u += PxPerCm, k++)
        {
            float laenge = k % 5 == 0 ? 9f : 5f;
            canvas.DrawLine(Editing.WbZeichenhilfe.Punkt(mitte, winkelGrad, u, kanteV),
                            Editing.WbZeichenhilfe.Punkt(mitte, winkelGrad, u, kanteV - laenge), strich);
            if (u > 0)
                canvas.DrawLine(Editing.WbZeichenhilfe.Punkt(mitte, winkelGrad, -u, kanteV),
                                Editing.WbZeichenhilfe.Punkt(mitte, winkelGrad, -u, kanteV - laenge), strich);
        }
    }

    /// <summary>Der Dreh-Griff: gefüllter Punkt mit weißem Ring, damit er auf jedem Grund sichtbar ist.</summary>
    public static void DrawAidHandle(SKCanvas canvas, Editing.Zeichenhilfe art, SKPoint mitte,
                                     float winkelGrad, float zoom, SKColor akzent)
    {
        var g = Editing.WbZeichenhilfe.Griffmitte(art, mitte, winkelGrad, zoom);

        using (var fuellung = new SKPaint { Color = akzent, IsAntialias = true })
            canvas.DrawCircle(g, 6f / zoom, fuellung);
        using var ring = new SKPaint
        {
            Color = SKColors.White, Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.4f / zoom, IsAntialias = true,
        };
        canvas.DrawCircle(g, 6f / zoom, ring);
    }

    /// <summary>
    /// Die Gradzahl beim Drehen — auf der <b>anderen</b> Seite der Achse als der Griff, damit
    /// die Hand sie nicht verdeckt.
    /// </summary>
    public static void DrawAidAngle(SKCanvas canvas, Editing.Zeichenhilfe art, SKPoint mitte,
                                    float winkelGrad, float zoom)
    {
        var (_, quer) = Editing.WbZeichenhilfe.Achsen(winkelGrad);
        string text = $"{Editing.WbZeichenhilfe.Anzeigewinkel(winkelGrad):0}°";

        float schrift = 15f / zoom;
        float abstand = (art == Editing.Zeichenhilfe.Geodreieck
                            ? Editing.WbZeichenhilfe.GeoHalbeHypotenuse * 0.5f
                            : Editing.WbZeichenhilfe.LinealHalbBreite) + 30f / zoom;
        var pos = new SKPoint(mitte.X - quer.X * abstand, mitte.Y - quer.Y * abstand);

        using var farbe = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var satz = new SKFont(WbFonts.Bold, schrift);
        float breite = satz.MeasureText(text);
        float randX = 9f / zoom, randY = 6f / zoom;

        var kasten = new SKRect(pos.X - breite / 2f - randX, pos.Y - schrift / 2f - randY,
                                pos.X + breite / 2f + randX, pos.Y + schrift / 2f + randY);
        using (var grund = new SKPaint { Color = new SKColor(23, 32, 51, 235), IsAntialias = true })
            canvas.DrawRoundRect(kasten, 6f / zoom, 6f / zoom, grund);

        canvas.DrawText(text, pos.X, pos.Y + schrift * 0.35f, SKTextAlign.Center, satz, farbe);
    }
}
