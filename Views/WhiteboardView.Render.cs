using System.Windows;
using System.Windows.Media;
using GonkNote.Models;
using GonkNote.Rendering;
using GonkNote.Services;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace GonkNote.Views;

/// <summary>
/// Zeichnen der Flaeche: Seitenhintergrund, Cover, Elemente und Overlays.
/// </summary>
public partial class WhiteboardView
{
    // ==================== Rendering ====================

    private static SKColor ResColor(string key)
    {
        if (Application.Current.Resources[key] is Color c)
            return new SKColor(c.R, c.G, c.B, c.A);
        return SKColors.Magenta;
    }

    private void Skia_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var clearColor = ResColor("Color.CanvasBg");
        if (_page is { IsInfinite: true } p && p.Shade != PageShade.Auto)
            clearColor = EffectiveShade(p) == PageShade.Dark
                ? SKColor.Parse("#12161F")
                : SKColor.Parse("#EEF2F8");
        canvas.Clear(clearColor);
        if (_page == null || _vm == null) return;

        if (!_vm.ViewInitialized && CanvasHost.ActualWidth > 0)
        {
            CenterView();
            _vm.ViewInitialized = true;
        }

        float pixelScale = (float)(e.Info.Width / Math.Max(1.0, Skia.ActualWidth));
        canvas.Scale(pixelScale);
        canvas.Translate(PanX, PanY);
        canvas.Scale(Zoom);

        RefreshAutoSwatch();
        DrawPageBackground(canvas);

        // Viewport-Culling: nur sichtbare Elemente zeichnen. Das verhindert, dass
        // bei vielen (hochauflösenden) Bildern jedes Frame alle dekodiert werden.
        var visible = VisibleCanvasRect();
        foreach (var el in _page.Elements)
        {
            if (el == _editingText) continue;
            // Zettel in Bearbeitung: nur die Karte zeichnen, den Text übernimmt die EditBox
            if (el == _editingSticky) { DrawStickyCard(canvas, (StickyNoteElement)el); continue; }
            var b = ElementBounds(el);
            if (!b.IsEmpty && !visible.IntersectsWith(b)) continue;
            DrawElement(canvas, el);
        }

        DrawActiveOverlays(canvas);
    }

    // ==================== Schatten (gecacht statt Live-Blur) ====================
    // Der frühere SKImageFilter.CreateBlur pro Frame skalierte mit der sichtbaren
    // Pixelfläche (im Notizbuch = ganzes Fenster, bei hohem Zoom zusätzlich mit
    // riesiger Blur-Sigma) und machte alle Eingaben träge. Stattdessen wird der
    // weiche Schatten einmal klein gerendert und als Nine-Patch gedehnt –
    // konstante Kosten pro Frame, identische Optik.

    private static SKImage? _pageShadowImg;     // Seite (eckig, Alpha 60)

    /// <summary>Zeichnet den weichen Schatten unter <paramref name="rect"/> (3 px nach unten versetzt).</summary>
    private static void DrawCachedShadow(SKCanvas canvas, SKRect rect, float cornerRadius, byte alpha, ref SKImage? cache) =>
        WbRenderer.DrawCachedShadow(canvas, rect, cornerRadius, alpha, ref cache);

    private SKColor PageLineColor()
    {
        if (_page == null || _page.Shade == PageShade.Auto) return ResColor("Color.PageLine");
        return EffectiveShade(_page) == PageShade.Dark ? SKColor.Parse("#35486E") : SKColor.Parse("#BBD2F0");
    }

    private SKColor PageDotColor()
    {
        if (_page == null || _page.Shade == PageShade.Auto) return ResColor("Color.PageGridDot");
        return EffectiveShade(_page) == PageShade.Dark ? SKColor.Parse("#3A4A6B") : SKColor.Parse("#B8C6DC");
    }

    private void DrawPageBackground(SKCanvas canvas)
    {
        if (_page == null) return;

        if (_page.IsInfinite)
        {
            DrawInfinitePattern(canvas);
            return;
        }

        var pageRect = SKRect.Create(0, 0, _page.Width, _page.Height);
        DrawCachedShadow(canvas, pageRect, 0, 60, ref _pageShadowImg);

        if (_page.IsCover)
        {
            DrawCover(canvas);
            return;
        }

        var bgColor = _page.Shade == PageShade.Auto ? ResColor("Color.PageBg")
            : EffectiveShade(_page) == PageShade.Dark ? SKColor.Parse("#1E2638") : SKColors.White;
        using (var bg = new SKPaint { Color = bgColor })
            canvas.DrawRect(pageRect, bg);

        // Hintergrundbild (importierte PDF-Seite): seitenfüllend, ersetzt das Muster
        if (_page.BackgroundImage is { Length: > 0 } bgData &&
            ImageCache.Get(_page.BackgroundImageId, bgData) is { } bgImg)
        {
            using var ip = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };
            canvas.DrawImage(bgImg, pageRect, ip);
            return;
        }

        using var line = new SKPaint
        {
            Color = PageLineColor(),
            StrokeWidth = 1f,
            IsAntialias = false,
        };

        const float spacingLines = 30f;
        switch (_page.Background)
        {
            case PageBackground.Lines:
                for (float y = 84; y < _page.Height - 30; y += spacingLines)
                    canvas.DrawLine(30, y, _page.Width - 30, y, line);
                break;

            case PageBackground.Grid:
                for (float y = 0; y <= _page.Height; y += spacingLines)
                    canvas.DrawLine(0, y, _page.Width, y, line);
                for (float x = 0; x <= _page.Width; x += spacingLines)
                    canvas.DrawLine(x, 0, x, _page.Height, line);
                break;

            case PageBackground.Dots:
                using (var dot = new SKPaint { Color = PageDotColor(), IsAntialias = true })
                {
                    for (float x = 24; x < _page.Width; x += 24)
                        for (float y = 24; y < _page.Height; y += 24)
                            canvas.DrawCircle(x, y, 1.1f, dot);
                }
                break;
        }
    }

    /// <summary>Muster für die unendliche Fläche, nur über den sichtbaren Bereich.</summary>
    private void DrawInfinitePattern(SKCanvas canvas)
    {
        if (_page == null || _page.Background == PageBackground.Blank) return;

        var tl = ToCanvas(new Point(0, 0));
        var br = ToCanvas(new Point(CanvasHost.ActualWidth, CanvasHost.ActualHeight));
        float spacing = _page.Background == PageBackground.Dots ? 28f : 30f;
        while (spacing * Zoom < 14f) spacing *= 2f;

        float x0 = MathF.Floor(tl.X / spacing) * spacing;
        float y0 = MathF.Floor(tl.Y / spacing) * spacing;

        switch (_page.Background)
        {
            case PageBackground.Dots:
                using (var dot = new SKPaint { Color = PageDotColor(), IsAntialias = true })
                {
                    float r = 1.1f / Zoom;
                    for (float x = x0; x <= br.X; x += spacing)
                        for (float y = y0; y <= br.Y; y += spacing)
                            canvas.DrawCircle(x, y, r, dot);
                }
                break;

            case PageBackground.Grid:
            case PageBackground.Lines:
                using (var line = new SKPaint { Color = PageLineColor(), StrokeWidth = 1f / Zoom })
                {
                    for (float y = y0; y <= br.Y; y += spacing)
                        canvas.DrawLine(tl.X, y, br.X, y, line);
                    if (_page.Background == PageBackground.Grid)
                        for (float x = x0; x <= br.X; x += spacing)
                            canvas.DrawLine(x, tl.Y, x, br.Y, line);
                }
                break;
        }
    }

    /// <summary>Cover-Seite: Bild oder Farbverlauf, Akzentlinie und Dokumenttitel (Stil anpassbar).</summary>
    private void DrawCover(SKCanvas canvas)
    {
        if (_page == null) return;
        var rect = SKRect.Create(0, 0, _page.Width, _page.Height);
        var cs = _vm?.Doc.Cover;

        // Bild-Cover: füllt die Seite formatfüllend (mittig beschnitten)
        if (cs?.Image is { Length: > 0 } imgData &&
            ImageCache.Get(cs.ImageId, imgData) is { } coverImg)
        {
            float scale = Math.Max(rect.Width / coverImg.Width, rect.Height / coverImg.Height);
            float w = coverImg.Width * scale, h = coverImg.Height * scale;
            var dst = SKRect.Create(rect.MidX - w / 2f, rect.MidY - h / 2f, w, h);
            canvas.Save();
            canvas.ClipRect(rect);
            using var ip = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };
            canvas.DrawImage(coverImg, dst, ip);
            canvas.Restore();
            return;
        }

        using (var grad = new SKPaint { IsAntialias = true })
        {
            grad.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(_page.Width, _page.Height),
                new[] { ParseColor(cs?.GradientStart ?? "#1E3A8A"), ParseColor(cs?.GradientEnd ?? "#7C3AED") },
                null, SKShaderTileMode.Clamp);
            canvas.DrawRect(rect, grad);
        }

        var coverBold = cs == null ? WbFonts.Bold
            : SKTypeface.FromFamilyName(cs.FontFamily, SKFontStyle.Bold) ?? WbFonts.Bold;

        string title = _vm?.Item.Name ?? "";
        using var titlePaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = 46,
            Typeface = coverBold,
            TextAlign = SKTextAlign.Center,
        };
        while (titlePaint.TextSize > 18 && titlePaint.MeasureText(title) > _page.Width * 0.8f)
            titlePaint.TextSize -= 2;
        canvas.DrawText(title, _page.Width / 2f, _page.Height * 0.4f, titlePaint);

        using (var accent = new SKPaint
        {
            Color = SKColor.Parse("#2DD4BF"),
            StrokeWidth = 4,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
        })
            canvas.DrawLine(_page.Width * 0.3f, _page.Height * 0.445f,
                            _page.Width * 0.7f, _page.Height * 0.445f, accent);

        using var subPaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(170),
            IsAntialias = true,
            TextSize = 15,
            Typeface = cs == null ? WbFonts.Regular : SKTypeface.FromFamilyName(cs.FontFamily) ?? WbFonts.Regular,
            TextAlign = SKTextAlign.Center,
        };
        canvas.DrawText("N O T I Z B U C H", _page.Width / 2f, _page.Height * 0.49f, subPaint);
    }

    private void DrawElement(SKCanvas canvas, WbElement el)
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

    // Die eigentlichen Zeichenroutinen liegen in GonkNote.Core (WbRenderer) — WPF, PDF-Export
    // und der Avalonia-Port teilen sich damit exakt dieselbe Darstellung (HANDOFF §9.3e).
    private static void DrawElementCore(SKCanvas canvas, WbElement el) =>
        WbRenderer.DrawElementCore(canvas, el);

    internal static void DrawImage(SKCanvas canvas, ImageElement im) =>
        WbRenderer.DrawImage(canvas, im);

    internal static void DrawStroke(SKCanvas canvas, StrokeElement s) =>
        WbRenderer.DrawStroke(canvas, s);

    internal static void DrawShape(SKCanvas canvas, ShapeElement sh, string colorHex, float strokeWidth) =>
        WbRenderer.DrawShape(canvas, sh, colorHex, strokeWidth);

    internal static void DrawText(SKCanvas canvas, TextElement t) =>
        WbRenderer.DrawText(canvas, t);

    /// <summary>Zeichnet nur die Zettelkarte (Schatten, Fläche, dezenter Rand) – ohne Text.</summary>
    internal static void DrawStickyCard(SKCanvas canvas, StickyNoteElement sn) =>
        WbRenderer.DrawStickyCard(canvas, sn);

    internal static void DrawSticky(SKCanvas canvas, StickyNoteElement sn) =>
        WbRenderer.DrawSticky(canvas, sn);

    /// <summary>Bricht Text an Wortgrenzen auf die verfügbare Breite um (respektiert \n).</summary>
    private static IEnumerable<string> WrapText(string text, SKPaint paint, float maxWidth) =>
        WbRenderer.WrapText(text, paint, maxWidth);

    /// <summary>Alles, was nur während einer Aktion zu sehen ist – liegt über den Elementen.</summary>
    private void DrawActiveOverlays(SKCanvas canvas)
    {
        DrawActiveStroke(canvas);
        DrawShapePreview(canvas);

        var accent = ResColorFromBrush("Brush.Accent");
        DrawLassoPath(canvas, accent);
        DrawSelectionFrame(canvas, accent);
        DrawEraserCursor(canvas);

        // Zeichenhilfe zuletzt (liegt über allem)
        if (_aid != DrawAid.None) DrawActiveAid(canvas);
    }

    /// <summary>Der Strich, der gerade gezogen wird (noch nicht im Dokument).</summary>
    private void DrawActiveStroke(SKCanvas canvas)
    {
        if (!_drawing || _activePoints is not { Count: > 0 }) return;

        var kind = _tool switch
        {
            ToolType.Pencil => StrokeKind.Pencil,
            ToolType.Highlighter => StrokeKind.Highlighter,
            _ => StrokeKind.Pen,
        };
        DrawStroke(canvas, new StrokeElement
        {
            Points = _activePoints,
            Color = CurrentInkHex(),
            Width = kind == StrokeKind.Highlighter ? Math.Max(_width * 5f, 10f) : _width,
            Kind = kind,
        });
    }

    /// <summary>Vorschau der Form, die gerade aufgezogen wird.</summary>
    private void DrawShapePreview(SKCanvas canvas)
    {
        if (!_shapeActive) return;

        DrawShape(canvas, new ShapeElement
        {
            Shape = _shape,
            X1 = _shapeStart.X, Y1 = _shapeStart.Y,
            X2 = _shapeCur.X, Y2 = _shapeCur.Y,
            Color = CurrentInkHex(),
            StrokeWidth = _width,
            Fill = CurrentFill(),
        }, CurrentInkHex(), _width);
    }

    private void DrawLassoPath(SKCanvas canvas, SKColor accent)
    {
        if (_lassoPts is not { Count: > 1 }) return;

        using var path = new SKPath();
        path.MoveTo(_lassoPts[0]);
        for (int i = 1; i < _lassoPts.Count; i++) path.LineTo(_lassoPts[i]);

        using var fill = new SKPaint { Color = accent.WithAlpha(25), IsAntialias = true };
        canvas.DrawPath(path, fill);

        using var stroke = new SKPaint
        {
            Color = accent,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.4f / Zoom,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new[] { 6f / Zoom, 4f / Zoom }, 0),
        };
        canvas.DrawPath(path, stroke);
    }

    /// <summary>
    /// Auswahlrahmen mit Griffen: bei einem einzelnen Element dreht der Rahmen mit und trägt
    /// Dreh- und Skalier-Griff, bei mehreren gibt es einen achsenparallelen Kasten.
    /// </summary>
    private void DrawSelectionFrame(SKCanvas canvas, SKColor accent)
    {
        if (_selection.Count == 0) return;

        using var fill = new SKPaint { Color = accent.WithAlpha(18) };
        using var stroke = new SKPaint
        {
            Color = accent, Style = SKPaintStyle.Stroke, StrokeWidth = 1.4f / Zoom, IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new[] { 6f / Zoom, 4f / Zoom }, 0),
        };
        using var handleFill = new SKPaint { Color = accent, IsAntialias = true };
        using var handleRing = new SKPaint
        {
            Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 1.4f / Zoom, IsAntialias = true,
        };
        float hs = 6f / Zoom;

        void Handle(SKPoint p, bool circle)
        {
            if (circle) { canvas.DrawCircle(p, hs, handleFill); canvas.DrawCircle(p, hs, handleRing); }
            else
            {
                var r = SKRect.Create(p.X - hs, p.Y - hs, hs * 2, hs * 2);
                canvas.DrawRect(r, handleFill); canvas.DrawRect(r, handleRing);
            }
        }

        if (_selection.Count > 1)
        {
            var b = InflatedSelectionBounds();
            canvas.DrawRect(b, fill);
            canvas.DrawRect(b, stroke);
            Handle(new SKPoint(b.Right, b.Bottom), circle: false);
            return;
        }

        var h = SingleHandles(_selection.First());
        using (var box = new SKPath())
        {
            box.MoveTo(h.TL); box.LineTo(h.TR); box.LineTo(h.BR); box.LineTo(h.BL); box.Close();
            canvas.DrawPath(box, fill);
            canvas.DrawPath(box, stroke);
        }

        // Linie zum Dreh-Griff
        var topMid = new SKPoint((h.TL.X + h.TR.X) / 2f, (h.TL.Y + h.TR.Y) / 2f);
        using (var line = new SKPaint
               {
                   Color = accent, Style = SKPaintStyle.Stroke, StrokeWidth = 1.4f / Zoom, IsAntialias = true,
               })
            canvas.DrawLine(topMid, h.Rotate, line);

        Handle(h.Rotate, circle: true);   // Drehen = Kreis
        Handle(h.Scale, circle: false);   // Skalieren = Quadrat
    }

    /// <summary>Radierer statt Mauszeiger: ein Ring in der eingestellten Größe.</summary>
    private void DrawEraserCursor(SKCanvas canvas)
    {
        if (!_eraserVisible || EffectiveTool != ToolType.Eraser) return;

        using var ring = new SKPaint
        {
            Color = ResColorFromBrush("Brush.Text").WithAlpha(160),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.2f / Zoom,
            IsAntialias = true,
        };
        canvas.DrawCircle(_eraserPos, _eraserRadius / Zoom, ring);
    }

    private static SKColor ResColorFromBrush(string key)
    {
        if (Application.Current.Resources[key] is SolidColorBrush b)
            return new SKColor(b.Color.R, b.Color.G, b.Color.B, b.Color.A);
        return SKColors.DodgerBlue;
    }

    private static SKColor ParseColor(string hex) => WbRenderer.ParseColor(hex);
}
