using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using System.Collections.Generic;
using GonkNote.Editing;
using GonkNote.Models;
using GonkNote.Rendering;
using GonkNote.Services;
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

    /// <summary>Aktuelle Stiftfarbe (Hex).</summary>
    public static readonly StyledProperty<string> InkColorProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, string>(nameof(InkColor), "#111827");

    /// <summary>Aktuelle Strichstärke.</summary>
    public static readonly StyledProperty<double> InkWidthProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, double>(nameof(InkWidth), 3.0);

    public string InkColor
    {
        get => GetValue(InkColorProperty);
        set => SetValue(InkColorProperty, value);
    }

    public double InkWidth
    {
        get => GetValue(InkWidthProperty);
        set => SetValue(InkWidthProperty, value);
    }

    /// <summary>Aktives Werkzeug (Stift/Pencil/Textmarker/Radierer/Hand).</summary>
    public static readonly StyledProperty<ToolType> ToolProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, ToolType>(nameof(Tool), ToolType.Pen);

    public ToolType Tool
    {
        get => GetValue(ToolProperty);
        set => SetValue(ToolProperty, value);
    }

    /// <summary>Verschiebung der Leinwand (Hand-Werkzeug / mittlere Maustaste).</summary>
    public static readonly StyledProperty<double> PanXProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, double>(nameof(PanX));

    public static readonly StyledProperty<double> PanYProperty =
        AvaloniaProperty.Register<WhiteboardCanvas, double>(nameof(PanY));

    public double PanX { get => GetValue(PanXProperty); set => SetValue(PanXProperty, value); }
    public double PanY { get => GetValue(PanYProperty); set => SetValue(PanYProperty, value); }

    /// <summary>Feuert mit dem fertigen Strich (Speichern + Undo-Eintrag).</summary>
    public event EventHandler<StrokeElement>? StrokeCompleted;

    /// <summary>Feuert mit den Radier-Schritten eines Zuges (ein Undo-Schritt).</summary>
    public event EventHandler<List<EraseStep>>? ElementsErased;

    /// <summary>Der gerade gezeichnete Strich (noch nicht Teil der Seite).</summary>
    private StrokeElement? _active;

    /// <summary>Radier-Schritte des laufenden Zuges (für einen einzigen Undo-Schritt).</summary>
    private List<EraseStep>? _eraseSteps;

    private bool _panning;
    private Point _panStart;
    private double _panOriginX, _panOriginY;

    static WhiteboardCanvas()
    {
        // Neu zeichnen, wenn sich Seite, Zoom oder Verschiebung ändern.
        AffectsRender<WhiteboardCanvas>(PageProperty, ZoomProperty, PanXProperty, PanYProperty);
    }

    public WhiteboardCanvas()
    {
        // Zeigereignisse kommen über die HitTest-Methode der Draw-Operation (füllt Bounds).
        Focusable = true;
    }

    public override void Render(DrawingContext context)
    {
        // Transparente Füllung macht das Control zuverlässig hit-testbar (sonst kommen
        // je nach Avalonia-Version keine Pointer-Events an).
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

        if (Page is not { } page) return;
        context.Custom(new PageDrawOperation(new Rect(Bounds.Size), page, Zoom, _active, PanX, PanY));
    }

    // ---- Zeigereingabe (Stift / Maus / Finger) ------------------------------------------

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Page is null) return;

        var pt = e.GetCurrentPoint(this);

        // Pan: Hand-Werkzeug oder mittlere Maustaste
        if (Tool == ToolType.Pan || pt.Properties.IsMiddleButtonPressed)
        {
            _panning = true;
            _panStart = pt.Position;
            _panOriginX = PanX;
            _panOriginY = PanY;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!pt.Properties.IsLeftButtonPressed) return;   // Stiftspitze bzw. linke Maustaste

        if (Tool == ToolType.Eraser)
        {
            _eraseSteps = new List<EraseStep>();
            EraseAt(ToPage(pt.Position));
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        _active = new StrokeElement
        {
            Color = InkColor,
            Width = Tool == ToolType.Highlighter ? Math.Max((float)InkWidth * 5f, 10f) : (float)InkWidth,
            Kind = Tool switch
            {
                ToolType.Pencil => StrokeKind.Pencil,
                ToolType.Highlighter => StrokeKind.Highlighter,
                _ => StrokeKind.Pen,
            },
        };
        AddPoint(pt);
        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);

        if (_panning)
        {
            PanX = _panOriginX + (pt.Position.X - _panStart.X);
            PanY = _panOriginY + (pt.Position.Y - _panStart.Y);
            e.Handled = true;
            return;
        }

        if (_eraseSteps is not null)
        {
            EraseAt(ToPage(pt.Position));
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (_active is null) return;
        AddPoint(pt);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_panning)
        {
            _panning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (_eraseSteps is { } steps)
        {
            _eraseSteps = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            if (steps.Count > 0) ElementsErased?.Invoke(this, steps);
            InvalidateVisual();
            return;
        }

        if (_active is null) return;

        AddPoint(e.GetCurrentPoint(this));
        var finished = _active;
        _active = null;
        e.Pointer.Capture(null);
        e.Handled = true;

        // Sehr kurze „Tupfer" trotzdem übernehmen (ergibt einen Punkt, s. WbRenderer).
        if (finished.Points.Count > 0 && Page is { } page)
        {
            if (finished.Points.Count == 1) finished.Points.Add(finished.Points[0]);
            page.Elements.Add(finished);
            StrokeCompleted?.Invoke(this, finished);
        }
        InvalidateVisual();
    }

    /// <summary>Mausrad zoomt (0,2×–8×).</summary>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double z = Zoom * (e.Delta.Y > 0 ? 1.1 : 1 / 1.1);
        Zoom = Math.Clamp(z, 0.2, 8.0);
        e.Handled = true;
    }

    /// <summary>Bildschirm- in Seitenkoordinaten (Pan + Zoom herausrechnen).</summary>
    private SKPoint ToPage(Point p) =>
        new((float)((p.X - PanX) / Zoom), (float)((p.Y - PanY) / Zoom));

    /// <summary>
    /// **Punktgenaues** Radieren wie in der WPF-App: Striche werden an der berührten Stelle
    /// aufgetrennt, die Reststücke bleiben stehen. Formen/Text/Zettel gehen als Ganzes,
    /// Bilder sind nicht radierbar (per Lasso löschen).
    /// </summary>
    private void EraseAt(SKPoint c)
    {
        if (Page is not { } page || _eraseSteps is null) return;

        float r = 14f / (float)Zoom;    // Radiergummi-Radius in Seiten-Koordinaten

        for (int i = page.Elements.Count - 1; i >= 0; i--)
        {
            var el = page.Elements[i];
            switch (el)
            {
                case StrokeElement s:
                {
                    if (!WbErase.HitsStroke(s, c, r)) break;
                    var parts = WbErase.SplitStroke(s, c, r + s.Width / 2f);
                    page.Elements.RemoveAt(i);
                    page.Elements.InsertRange(i, parts);
                    _eraseSteps.Add(new EraseStep(s, i, parts));
                    break;
                }

                case ShapeElement or GonkNote.Models.TextElement or StickyNoteElement:
                    if (!WbErase.HitsOther(el, c, r)) break;
                    page.Elements.RemoveAt(i);
                    _eraseSteps.Add(new EraseStep(el, i, new List<WbElement>()));
                    break;

                // Bilder/PDF-Seiten bleiben stehen (wie in der WPF-App)
            }
        }
    }

    /// <summary>Übernimmt einen Zeigerpunkt inkl. Druckstärke (Maus/Finger ohne Druck ⇒ 0,5).</summary>
    private void AddPoint(PointerPoint pt)
    {
        if (_active is null) return;
        float pressure = pt.Properties.Pressure;
        if (pressure <= 0f || float.IsNaN(pressure)) pressure = 0.5f;   // kein Drucksensor
        var p = ToPage(pt.Position);
        _active.Points.Add(new WbPoint { X = p.X, Y = p.Y, P = pressure });
    }

    /// <summary>Custom-Draw-Operation: leiht sich Avalonias SKCanvas und nutzt den Core-Renderer.</summary>
    private sealed class PageDrawOperation : ICustomDrawOperation
    {
        private readonly WbPage _page;
        private readonly double _zoom;
        private readonly StrokeElement? _active;
        private readonly double _panX, _panY;

        public PageDrawOperation(Rect bounds, WbPage page, double zoom, StrokeElement? active,
                                 double panX, double panY)
        {
            Bounds = bounds;
            _page = page;
            _zoom = zoom;
            _active = active;
            _panX = panX;
            _panY = panY;
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
            canvas.Translate((float)_panX, (float)_panY);
            canvas.Scale((float)_zoom);
            DrawPage(canvas);
            canvas.Restore();
        }

        private void DrawPage(SKCanvas canvas)
        {
            bool dark = _page.Shade == PageShade.Dark;
            var paper = dark ? new SKColor(0x24, 0x28, 0x30) : SKColors.White;

            // Sichtbarer Bereich in Seiten-Koordinaten (Pan + Zoom herausgerechnet)
            var view = SKRect.Create(
                (float)(-_panX / _zoom), (float)(-_panY / _zoom),
                (float)(Bounds.Width / _zoom), (float)(Bounds.Height / _zoom));

            // Seitenfläche (unendliche Whiteboards haben keine Seitengrenzen)
            SKRect pageRect;
            if (_page.IsInfinite)
            {
                pageRect = view;
                using var bgp = new SKPaint { Color = paper };
                canvas.DrawRect(pageRect, bgp);
            }
            else
            {
                using var desk = new SKPaint
                {
                    Color = dark ? new SKColor(0x18, 0x1A, 0x20) : new SKColor(0xEE, 0xF1, 0xF6),
                };
                canvas.DrawRect(view, desk);

                pageRect = SKRect.Create(0, 0, _page.Width, _page.Height);
                using var bg = new SKPaint { IsAntialias = true, Color = paper };
                canvas.DrawRect(pageRect, bg);
            }

            DrawPattern(canvas, pageRect, dark);

            foreach (var el in _page.Elements)
                WbRenderer.DrawElement(canvas, el);   // <- gemeinsame Routinen aus GonkNote.Core

            // Laufender Strich (noch nicht Teil der Seite)
            if (_active is { Points.Count: > 0 })
                WbRenderer.DrawStroke(canvas, _active);
        }

        /// <summary>Linien-/Raster-/Punktmuster der Seite (vereinfacht ggü. WPF, gleiche Optik).</summary>
        private void DrawPattern(SKCanvas canvas, SKRect r, bool dark)
        {
            if (_page.IsCover || _page.Background == PageBackground.Blank) return;

            var lineColor = dark ? new SKColor(0x35, 0x48, 0x6E) : new SKColor(0xBB, 0xD2, 0xF0);
            var dotColor = dark ? new SKColor(0x3A, 0x4A, 0x6B) : new SKColor(0xB8, 0xC6, 0xDC);
            const float step = 28f;

            // Am Seitenraster ausrichten (nicht am Viewport) — sonst wandert das Muster beim Pan.
            float x0 = MathF.Ceiling(r.Left / step) * step;
            float y0 = MathF.Ceiling(r.Top / step) * step;

            switch (_page.Background)
            {
                case PageBackground.Lines:
                {
                    using var p = new SKPaint { IsAntialias = true, Color = lineColor, StrokeWidth = 1 };
                    for (float y = y0; y < r.Bottom; y += step)
                        canvas.DrawLine(r.Left, y, r.Right, y, p);
                    break;
                }
                case PageBackground.Grid:
                {
                    using var p = new SKPaint { IsAntialias = true, Color = lineColor, StrokeWidth = 1 };
                    for (float y = y0; y < r.Bottom; y += step)
                        canvas.DrawLine(r.Left, y, r.Right, y, p);
                    for (float x = x0; x < r.Right; x += step)
                        canvas.DrawLine(x, r.Top, x, r.Bottom, p);
                    break;
                }
                case PageBackground.Dots:
                {
                    using var p = new SKPaint { IsAntialias = true, Color = dotColor };
                    for (float y = y0; y < r.Bottom; y += step)
                        for (float x = x0; x < r.Right; x += step)
                            canvas.DrawCircle(x, y, 1.6f, p);
                    break;
                }
            }
        }
    }
}
