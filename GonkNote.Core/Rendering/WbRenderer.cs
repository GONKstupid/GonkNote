using System.Collections.Generic;
using System.Linq;
using GonkNote.Models;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Rendering;

/// <summary>Gemeinsame Schriften für Canvas-Text (SkiaSharp, plattformneutral).</summary>
public static class WbFonts
{
    public static readonly SKTypeface Regular =
        SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;

    public static readonly SKTypeface Bold =
        SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold) ?? SKTypeface.Default;

    private static readonly Dictionary<string, SKTypeface> _families = new();

    /// <summary>Typeface je Familienname, gecacht.</summary>
    public static SKTypeface Family(string? name)
    {
        if (string.IsNullOrEmpty(name) || name == "Segoe UI") return Regular;
        if (!_families.TryGetValue(name, out var tf))
        {
            tf = SKTypeface.FromFamilyName(name) ?? Regular;
            _families[name] = tf;
        }
        return tf;
    }
}

/// <summary>
/// Plattformneutrales Zeichnen der Whiteboard-/Notizbuch-Elemente auf einen
/// <see cref="SKCanvas"/>. Aus <c>Views/WhiteboardView.xaml.cs</c> herausgelöst, damit WPF,
/// der PDF-Export **und** der Avalonia-Port dieselbe Darstellung verwenden (HANDOFF §9.3e).
/// Enthält bewusst KEINE Eingabe-/Werkzeuglogik — nur Rendering.
/// </summary>
public static class WbRenderer
{
    /// <summary>Innenabstand des Zettels zwischen Kartenrand und Text (Canvas-Einheiten).</summary>
    public const float StickyPad = 14f;

    /// <summary>Breite des weichen Schattenrands (Canvas-Einheiten).</summary>
    private const int ShadowFringe = 18;

    private static SKImage? _stickyShadowImg;   // Notizzettel (Radius 6, Alpha 45)

    // ---- Farben / Pfade -----------------------------------------------------------------

    public static SKColor ParseColor(string hex)
    {
        try { return SKColor.Parse(hex); }
        catch { return SKColors.Gray; }
    }

    private static SKPath BuildSmoothPath(List<WbPoint> pts)
    {
        var path = new SKPath();
        if (pts.Count == 0) return path;
        path.MoveTo(pts[0].X, pts[0].Y);
        for (int i = 1; i < pts.Count - 1; i++)
        {
            float mx = (pts[i].X + pts[i + 1].X) / 2f;
            float my = (pts[i].Y + pts[i + 1].Y) / 2f;
            path.QuadTo(pts[i].X, pts[i].Y, mx, my);
        }
        if (pts.Count > 1)
            path.LineTo(pts[^1].X, pts[^1].Y);
        return path;
    }

    public static (SKPoint A, SKPoint B, SKPoint C) TrianglePoints(ShapeElement sh)
    {
        float minX = Math.Min(sh.X1, sh.X2), maxX = Math.Max(sh.X1, sh.X2);
        float minY = Math.Min(sh.Y1, sh.Y2), maxY = Math.Max(sh.Y1, sh.Y2);
        return (new SKPoint((minX + maxX) / 2f, minY),
                new SKPoint(maxX, maxY),
                new SKPoint(minX, maxY));
    }

    // ---- Maße ---------------------------------------------------------------------------

    public static SKRect TextBounds(TextElement t)
    {
        using var paint = new SKPaint
        {
            TextSize = t.FontSize,
            Typeface = WbFonts.Family(t.FontFamily),
        };
        var lines = t.Text.Length == 0 ? new[] { " " } : t.Text.Split('\n');
        float w = 10;
        foreach (var line in lines)
            w = Math.Max(w, paint.MeasureText(line.Length == 0 ? " " : line));
        float h = lines.Length * t.FontSize * 1.35f;
        return SKRect.Create(t.X, t.Y, w, h);
    }

    public static SKRect ElementBounds(WbElement el)
    {
        switch (el)
        {
            case StrokeElement s:
            {
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (var p in s.Points)
                {
                    minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                    minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                }
                var r = new SKRect(minX, minY, maxX, maxY);
                r.Inflate(s.Width / 2f, s.Width / 2f);
                return r;
            }
            case ShapeElement sh:
            {
                var r = new SKRect(Math.Min(sh.X1, sh.X2), Math.Min(sh.Y1, sh.Y2),
                                   Math.Max(sh.X1, sh.X2), Math.Max(sh.Y1, sh.Y2));
                r.Inflate(sh.StrokeWidth / 2f, sh.StrokeWidth / 2f);
                return r;
            }
            case TextElement t:
                return TextBounds(t);
            case ImageElement im:
                return SKRect.Create(im.X, im.Y, im.Width, im.Height);
            case StickyNoteElement sn:
                return SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height);
            default:
                return SKRect.Empty;
        }
    }

    // ---- Schatten (gecachtes Nine-Patch, konstante Kosten) -------------------------------

    private static SKImage ShadowNinePatch(ref SKImage? cache, float cornerRadius, byte alpha)
    {
        if (cache != null) return cache;
        int inset = ShadowFringe + (int)MathF.Ceiling(cornerRadius) + 2;
        int size = inset * 2 + 32;   // 32 px dehnbarer Kern
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        var c = surface.Canvas;
        c.Clear(SKColors.Transparent);
        using var p = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black.WithAlpha(alpha),
            ImageFilter = SKImageFilter.CreateBlur(6, 6),
        };
        var r = SKRect.Create(ShadowFringe, ShadowFringe, size - 2 * ShadowFringe, size - 2 * ShadowFringe);
        if (cornerRadius > 0) c.DrawRoundRect(r, cornerRadius, cornerRadius, p);
        else c.DrawRect(r, p);
        cache = surface.Snapshot();
        return cache;
    }

    /// <summary>Zeichnet den weichen Schatten unter <paramref name="rect"/> (3 px nach unten versetzt).</summary>
    public static void DrawCachedShadow(SKCanvas canvas, SKRect rect, float cornerRadius, byte alpha, ref SKImage? cache)
    {
        var img = ShadowNinePatch(ref cache, cornerRadius, alpha);
        int inset = ShadowFringe + (int)MathF.Ceiling(cornerRadius) + 2;
        var center = new SKRectI(inset, inset, img.Width - inset, img.Height - inset);
        var dst = SKRect.Create(rect.Left - ShadowFringe, rect.Top - ShadowFringe + 3,
            rect.Width + 2 * ShadowFringe, rect.Height + 2 * ShadowFringe);
        canvas.DrawImageNinePatch(img, center, dst);
    }

    // ---- Elemente -----------------------------------------------------------------------

    /// <summary>Zeichnet ein Element inkl. seiner Drehung.</summary>
    public static void DrawElement(SKCanvas canvas, WbElement el)
    {
        if (el.Rotation != 0f)
        {
            var b = ElementBounds(el);
            canvas.Save();
            canvas.RotateDegrees(el.Rotation, b.MidX, b.MidY);
            DrawElementCore(canvas, el);
            canvas.Restore();
        }
        else
        {
            DrawElementCore(canvas, el);
        }
    }

    public static void DrawElementCore(SKCanvas canvas, WbElement el)
    {
        switch (el)
        {
            case StrokeElement s: DrawStroke(canvas, s); break;
            case ShapeElement sh: DrawShape(canvas, sh, sh.Color, sh.StrokeWidth); break;
            case TextElement t: DrawText(canvas, t); break;
            case ImageElement im: DrawImage(canvas, im); break;
            case StickyNoteElement sn: DrawSticky(canvas, sn); break;
        }
    }

    public static void DrawImage(SKCanvas canvas, ImageElement im)
    {
        var rect = SKRect.Create(im.X, im.Y, im.Width, im.Height);
        var img = ImageCache.Get(im.Id, im.Data);
        if (img == null)
        {
            // Nicht dekodierbar: Platzhalter, damit das Element auswählbar bleibt
            using var ph = new SKPaint { Color = SKColors.Gray.WithAlpha(60) };
            canvas.DrawRect(rect, ph);
            return;
        }
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };
        canvas.DrawImage(img, rect, paint);
    }

    public static void DrawStroke(SKCanvas canvas, StrokeElement s)
    {
        if (s.Points.Count == 0) return;
        var color = ParseColor(s.Color);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color,
        };

        switch (s.Kind)
        {
            case StrokeKind.Highlighter:
                paint.Color = color.WithAlpha(70);
                paint.StrokeCap = SKStrokeCap.Butt;
                paint.StrokeWidth = s.Width;
                using (var path = BuildSmoothPath(s.Points))
                    canvas.DrawPath(path, paint);
                break;

            case StrokeKind.Pencil:
                DrawPencil(canvas, s, color);
                break;

            default: // Stift: Druck steuert die Strichbreite je Segment
                if (s.Points.Count == 2 &&
                    Math.Abs(s.Points[0].X - s.Points[1].X) < 0.5f &&
                    Math.Abs(s.Points[0].Y - s.Points[1].Y) < 0.5f)
                {
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(s.Points[0].X, s.Points[0].Y, s.Width * 0.7f, paint);
                    return;
                }
                for (int i = 0; i < s.Points.Count - 1; i++)
                {
                    var a = s.Points[i];
                    var b = s.Points[i + 1];
                    float p = (a.P + b.P) / 2f;
                    paint.StrokeWidth = s.Width * (0.35f + 1.1f * p);
                    canvas.DrawLine(a.X, a.Y, b.X, b.Y, paint);
                }
                break;
        }
    }

    /// <summary>
    /// Bleistift-Anmutung: Graphit deckt nie voll und setzt sich körnig in die Papierstruktur.
    /// Umgesetzt in drei günstigen Skia-Durchgängen (statt einer zackigen Discrete-Linie):
    /// weiche Kernlinie, fein aufgerauter Rand und eine gestempelte Körnung entlang eines
    /// leicht verrauschten Pfades.
    /// </summary>
    private static void DrawPencil(SKCanvas canvas, StrokeElement s, SKColor color)
    {
        float w = Math.Max(s.Width, 0.6f);
        using var path = BuildSmoothPath(s.Points);

        // 1) Kernlinie – halbtransparent, damit der Untergrund durchscheint
        using (var core = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color.WithAlpha(80),
            StrokeWidth = w * 0.9f,
        })
            canvas.DrawPath(path, core);

        // 2) fein aufgerauter Rand (kleine Amplitude – kein Zacken-Look)
        using (var edgeFx = SKPathEffect.CreateDiscrete(1.4f, w * 0.20f))
        using (var edge = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color.WithAlpha(55),
            StrokeWidth = w * 0.72f,
            PathEffect = edgeFx,
        })
            canvas.DrawPath(path, edge);

        // 3) Körnung: kleine Punkte werden entlang eines verrauschten Pfades gestempelt
        using var dot = new SKPath();
        dot.AddCircle(0, 0, Math.Max(0.45f, w * 0.13f));
        using (var jitter = SKPathEffect.CreateDiscrete(1.8f, w * 0.38f))
        using (var stamp = SKPathEffect.Create1DPath(dot, Math.Max(1.1f, w * 0.42f), 0,
                                                     SKPath1DPathEffectStyle.Translate))
        using (var grainFx = SKPathEffect.CreateCompose(stamp, jitter))
        using (var grain = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = color.WithAlpha(140),
            StrokeWidth = w * 0.85f,
            PathEffect = grainFx,
        })
            canvas.DrawPath(path, grain);
    }

    public static void DrawShape(SKCanvas canvas, ShapeElement sh, string colorHex, float strokeWidth)
    {
        var color = ParseColor(colorHex);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color,
            StrokeWidth = strokeWidth,
        };

        var p1 = new SKPoint(sh.X1, sh.Y1);
        var p2 = new SKPoint(sh.X2, sh.Y2);

        switch (sh.Shape)
        {
            case ShapeKind.Line:
                canvas.DrawLine(p1, p2, paint);
                break;

            case ShapeKind.Arrow:
            {
                canvas.DrawLine(p1, p2, paint);
                float angle = MathF.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                float head = strokeWidth * 3f + 10f;
                const float spread = 0.46f;
                var h1 = new SKPoint(p2.X - head * MathF.Cos(angle - spread), p2.Y - head * MathF.Sin(angle - spread));
                var h2 = new SKPoint(p2.X - head * MathF.Cos(angle + spread), p2.Y - head * MathF.Sin(angle + spread));
                canvas.DrawLine(p2, h1, paint);
                canvas.DrawLine(p2, h2, paint);
                break;
            }

            case ShapeKind.Rectangle:
            {
                var r = SKRect.Create(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                      Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));
                if (sh.Fill != null)
                {
                    using var fill = new SKPaint { IsAntialias = true, Color = ParseColor(sh.Fill) };
                    canvas.DrawRect(r, fill);
                }
                canvas.DrawRect(r, paint);
                break;
            }

            case ShapeKind.Ellipse:
            {
                var r = SKRect.Create(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                      Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));
                if (sh.Fill != null)
                {
                    using var fill = new SKPaint { IsAntialias = true, Color = ParseColor(sh.Fill) };
                    canvas.DrawOval(r, fill);
                }
                canvas.DrawOval(r, paint);
                break;
            }

            case ShapeKind.Triangle:
            {
                var (a, b, c) = TrianglePoints(sh);
                using var path = new SKPath();
                path.MoveTo(a); path.LineTo(b); path.LineTo(c); path.Close();
                if (sh.Fill != null)
                {
                    using var fill = new SKPaint { IsAntialias = true, Color = ParseColor(sh.Fill) };
                    canvas.DrawPath(path, fill);
                }
                canvas.DrawPath(path, paint);
                break;
            }
        }
    }

    public static void DrawText(SKCanvas canvas, TextElement t)
    {
        if (t.Background != null)
        {
            var b = TextBounds(t);
            b.Inflate(5, 3);
            using var bg = new SKPaint { IsAntialias = true, Color = ParseColor(t.Background) };
            canvas.DrawRoundRect(b, 3, 3, bg);
        }

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(t.Color),
            TextSize = t.FontSize,
            Typeface = WbFonts.Family(t.FontFamily),
        };
        float lineHeight = t.FontSize * 1.35f;
        var lines = t.Text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            canvas.DrawText(lines[i], t.X, t.Y + t.FontSize + i * lineHeight, paint);
    }

    /// <summary>Zeichnet nur die Zettelkarte (Schatten, Fläche, dezenter Rand) – ohne Text.</summary>
    public static void DrawStickyCard(SKCanvas canvas, StickyNoteElement sn)
    {
        var rect = SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height);
        const float radius = 6f;

        DrawCachedShadow(canvas, rect, radius, 45, ref _stickyShadowImg);

        var fill = ParseColor(sn.Color);
        using (var bg = new SKPaint { IsAntialias = true, Color = fill })
            canvas.DrawRoundRect(rect, radius, radius, bg);

        // hauchzarter Rand, leicht dunkler als die Füllung
        using var border = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            Color = new SKColor(0, 0, 0, 28),
        };
        canvas.DrawRoundRect(rect, radius, radius, border);
    }

    public static void DrawSticky(SKCanvas canvas, StickyNoteElement sn)
    {
        DrawStickyCard(canvas, sn);
        if (string.IsNullOrEmpty(sn.Text)) return;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(sn.TextColor),
            TextSize = sn.FontSize,
            Typeface = WbFonts.Family(sn.FontFamily),
        };

        float lineHeight = sn.FontSize * 1.32f;
        float maxWidth = sn.Width - StickyPad * 2;
        float x = sn.X + StickyPad;
        float yBase = sn.Y + StickyPad + sn.FontSize;
        float maxY = sn.Y + sn.Height - StickyPad * 0.5f;

        canvas.Save();
        canvas.ClipRect(SKRect.Create(sn.X, sn.Y, sn.Width, sn.Height));
        float y = yBase;
        foreach (var line in WrapText(sn.Text, paint, maxWidth))
        {
            if (y > maxY) break; // Text, der nicht mehr passt, wird abgeschnitten
            canvas.DrawText(line, x, y, paint);
            y += lineHeight;
        }
        canvas.Restore();
    }

    // ---- Textumbruch ---------------------------------------------------------------------

    /// <summary>Bricht Text an Wortgrenzen auf die verfügbare Breite um (respektiert \n).</summary>
    public static IEnumerable<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        foreach (var para in text.Split('\n'))
        {
            if (para.Length == 0) { yield return ""; continue; }

            var words = para.Split(' ');
            var current = "";
            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (paint.MeasureText(candidate) <= maxWidth || current.Length == 0)
                {
                    // Einzelnes zu langes Wort hart umbrechen
                    if (current.Length == 0 && paint.MeasureText(word) > maxWidth)
                    {
                        foreach (var chunk in BreakLongWord(word, paint, maxWidth))
                        {
                            if (chunk.Last) { current = chunk.Text; }
                            else yield return chunk.Text;
                        }
                    }
                    else current = candidate;
                }
                else
                {
                    yield return current;
                    current = word;
                    if (paint.MeasureText(word) > maxWidth)
                    {
                        foreach (var chunk in BreakLongWord(word, paint, maxWidth))
                        {
                            if (chunk.Last) current = chunk.Text;
                            else yield return chunk.Text;
                        }
                    }
                }
            }
            yield return current;
        }
    }

    private static IEnumerable<(string Text, bool Last)> BreakLongWord(string word, SKPaint paint, float maxWidth)
    {
        var chunk = "";
        foreach (var ch in word)
        {
            if (chunk.Length > 0 && paint.MeasureText(chunk + ch) > maxWidth)
            {
                yield return (chunk, false);
                chunk = ch.ToString();
            }
            else chunk += ch;
        }
        yield return (chunk, true);
    }
}
