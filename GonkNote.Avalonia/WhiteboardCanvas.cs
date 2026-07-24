using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using GonkNote.Models;
using GonkNote.Rendering;
using SkiaSharp;

namespace GonkNote.Avalonia;

/// <summary>
/// Zeichnet eine <see cref="WbPage"/> über SkiaSharp — dieselben Routinen wie die WPF-App
/// (<see cref="WbRenderer"/> in GonkNote.Core). Avalonia rendert selbst mit Skia, daher lässt
/// sich der SKCanvas direkt ausleihen (kein Zwischenbild). HANDOFF §9.3e.
/// **Stand: Anzeige (read-only).** Werkzeuge/Stift-Eingabe folgen in Schritt 4b.
/// </summary>
public class WhiteboardCanvas : Control
{
    public static readonly StyledProperty<WbPage?> PageProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, WbPage?>(nameof(Page));

    /// <summary>Zoomfaktor (1 = 100 %).</summary>
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, double>(nameof(Zoom), 1.0);

    public WbPage? Page
    {
        get => GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    static WhiteboardCanvas()
    {
        // Neu zeichnen, wenn sich Seite oder Zoom ändern.
        AffectsRender<WhiteboardCanvas>(PageProperty, ZoomProperty);
    }

    public override void Render(DrawingContext context)
    {
        if (Page is not { } page) return;
        context.Custom(new PageDrawOperation(new Rect(Bounds.Size), page, Zoom));
    }

    /// <summary>Custom-Draw-Operation: leiht sich Avalonias SKCanvas und nutzt den Core-Renderer.</summary>
    private sealed class PageDrawOperation : ICustomDrawOperation
    {
        private readonly WbPage _page;
        private readonly double _zoom;

        public PageDrawOperation(Rect bounds, WbPage page, double zoom)
        {
            Bounds = bounds;
            _page = page;
            _zoom = zoom;
        }

        public Rect Bounds { get; }
        public bool HitTest(Point p) => Bounds.Contains(p);
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            // Avalonia 11.0.x: TryGetFeature ist nicht generisch und liefert object.
            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature lease)
                return;                                     // kein Skia-Backend → nichts zeichnen
            using var api = lease.Lease();
            var canvas = api.SkCanvas;

            canvas.Save();
            // WICHTIG: auf die Control-Fläche begrenzen. canvas.Clear() würde die *gesamte*
            // Fensteroberfläche löschen (überdeckte anfangs Baum und Kopfleiste).
            canvas.ClipRect(SKRect.Create(0, 0, (float)Bounds.Width, (float)Bounds.Height));
            canvas.Scale((float)_zoom);
            DrawPage(canvas);
            canvas.Restore();
        }

        private void DrawPage(SKCanvas canvas)
        {
            bool dark = _page.Shade == PageShade.Dark;
            var paper = dark ? new SKColor(0x24, 0x28, 0x30) : SKColors.White;

            // Sichtbarer Bereich in Seiten-Koordinaten (nach dem Zoom-Scale)
            float vw = (float)(Bounds.Width / _zoom);
            float vh = (float)(Bounds.Height / _zoom);

            // Seitenfläche (unendliche Whiteboards haben keine Seitengrenzen)
            SKRect pageRect;
            if (_page.IsInfinite)
            {
                pageRect = SKRect.Create(0, 0, vw, vh);
                using var bgp = new SKPaint { Color = paper };
                canvas.DrawRect(pageRect, bgp);
            }
            else
            {
                using var desk = new SKPaint
                {
                    Color = dark ? new SKColor(0x18, 0x1A, 0x20) : new SKColor(0xEE, 0xF1, 0xF6),
                };
                canvas.DrawRect(SKRect.Create(0, 0, vw, vh), desk);

                pageRect = SKRect.Create(0, 0, _page.Width, _page.Height);
                using var bg = new SKPaint { IsAntialias = true, Color = paper };
                canvas.DrawRect(pageRect, bg);
            }

            DrawPattern(canvas, pageRect, dark);

            foreach (var el in _page.Elements)
                WbRenderer.DrawElement(canvas, el);   // <- gemeinsame Routinen aus GonkNote.Core
        }

        /// <summary>Linien-/Raster-/Punktmuster der Seite (vereinfacht ggü. WPF, gleiche Optik).</summary>
        private void DrawPattern(SKCanvas canvas, SKRect r, bool dark)
        {
            if (_page.IsCover || _page.Background == PageBackground.Blank) return;

            var lineColor = dark ? new SKColor(0x35, 0x48, 0x6E) : new SKColor(0xBB, 0xD2, 0xF0);
            var dotColor = dark ? new SKColor(0x3A, 0x4A, 0x6B) : new SKColor(0xB8, 0xC6, 0xDC);
            const float step = 28f;

            switch (_page.Background)
            {
                case PageBackground.Lines:
                {
                    using var p = new SKPaint { IsAntialias = true, Color = lineColor, StrokeWidth = 1 };
                    for (float y = r.Top + step; y < r.Bottom; y += step)
                        canvas.DrawLine(r.Left, y, r.Right, y, p);
                    break;
                }
                case PageBackground.Grid:
                {
                    using var p = new SKPaint { IsAntialias = true, Color = lineColor, StrokeWidth = 1 };
                    for (float y = r.Top + step; y < r.Bottom; y += step)
                        canvas.DrawLine(r.Left, y, r.Right, y, p);
                    for (float x = r.Left + step; x < r.Right; x += step)
                        canvas.DrawLine(x, r.Top, x, r.Bottom, p);
                    break;
                }
                case PageBackground.Dots:
                {
                    using var p = new SKPaint { IsAntialias = true, Color = dotColor };
                    for (float y = r.Top + step; y < r.Bottom; y += step)
                        for (float x = r.Left + step; x < r.Right; x += step)
                            canvas.DrawCircle(x, y, 1.6f, p);
                    break;
                }
            }
        }
    }
}
