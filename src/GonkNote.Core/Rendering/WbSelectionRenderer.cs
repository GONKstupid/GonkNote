using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Rendering;

/// <summary>
/// Der Auswahlrahmen samt Griffen.
///
/// <para>
/// <b>Warum das hier steht.</b> Bis Phase 4.5 hatte jeder Kopf sein eigenes
/// <c>DrawSelectionFrame</c> — und die beiden Fassungen waren für den Kasten der
/// Mehrfachauswahl bereits <b>Zeile für Zeile dieselbe</b>: Füllung mit Alpha 18,
/// Strichstärke <c>1.4/Zoom</c>, Strichelung <c>6/4</c>. Der WPF-Kopf hatte zusätzlich die
/// Griffe, der Linux-Kopf nicht („für M1 ein achsenparalleler Kasten"). Wer die Griffe dort
/// nachträgt, schreibt die dritte Fassung derselben Zahlen ab.
/// </para>
/// <para>
/// <b>Die Geometrie steht in <see cref="WbHandles"/></b> — hier wird nur gezeichnet. Die
/// Trennung ist dieselbe wie zwischen <see cref="WbHit"/> und <see cref="WbRenderer"/>.
/// </para>
/// <para>
/// <b>Was der Kopf mitbringt:</b> die Akzentfarbe aus seinem Farbschema und den Zoom. Beides
/// sind Werte, keine Steuerelemente.
/// </para>
/// </summary>
public static class WbSelectionRenderer
{
    /// <summary>Deckkraft der Füllfläche unter der Auswahl.</summary>
    private const byte FillAlpha = 18;

    /// <summary>Strichstärke von Rahmen, Arm und Griffring, in Bildschirmpixeln.</summary>
    private const float StrokePx = 1.4f;

    /// <summary>
    /// Zeichnet den Auswahlrahmen. Bei <b>einem</b> Element dreht der Rahmen mit und trägt
    /// Dreh- und Skalier-Griff; bei <b>mehreren</b> gibt es einen achsenparallelen Kasten mit
    /// nur einem Skalier-Griff. Bei leerer Auswahl wird nichts gezeichnet.
    /// </summary>
    /// <param name="single">Das Element bei Einzelauswahl, sonst <c>null</c>.</param>
    /// <param name="bounds">Das Kästchen der Auswahl (bei Mehrfachauswahl gebraucht).</param>
    /// <param name="count">Anzahl der ausgewählten Elemente.</param>
    public static void Draw(
        SKCanvas canvas, WbElement? single, SKRect bounds, int count, SKColor accent, float zoom)
    {
        if (count <= 0) return;

        using var fill = new SKPaint { Color = accent.WithAlpha(FillAlpha) };
        using var stroke = new SKPaint
        {
            Color = accent,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokePx / zoom,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash([6f / zoom, 4f / zoom], 0),
        };
        using var handleFill = new SKPaint { Color = accent, IsAntialias = true };
        using var handleRing = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokePx / zoom,
            IsAntialias = true,
        };
        float hs = WbHandles.HandleSizePx / zoom;

        void Handle(SKPoint p, bool circle)
        {
            if (circle)
            {
                canvas.DrawCircle(p, hs, handleFill);
                canvas.DrawCircle(p, hs, handleRing);
            }
            else
            {
                var r = SKRect.Create(p.X - hs, p.Y - hs, hs * 2, hs * 2);
                canvas.DrawRect(r, handleFill);
                canvas.DrawRect(r, handleRing);
            }
        }

        if (single == null)
        {
            var b = WbHandles.InflatedBounds(bounds, zoom);
            canvas.DrawRect(b, fill);
            canvas.DrawRect(b, stroke);
            Handle(new SKPoint(b.Right, b.Bottom), circle: false);
            return;
        }

        var h = WbHandles.Single(single, zoom);
        using (var box = new SKPath())
        {
            box.MoveTo(h.TL);
            box.LineTo(h.TR);
            box.LineTo(h.BR);
            box.LineTo(h.BL);
            box.Close();
            canvas.DrawPath(box, fill);
            canvas.DrawPath(box, stroke);
        }

        // Der Arm zum Dreh-Griff — durchgezogen, nicht gestrichelt.
        var topMid = new SKPoint((h.TL.X + h.TR.X) / 2f, (h.TL.Y + h.TR.Y) / 2f);
        using (var line = new SKPaint
               {
                   Color = accent,
                   Style = SKPaintStyle.Stroke,
                   StrokeWidth = StrokePx / zoom,
                   IsAntialias = true,
               })
            canvas.DrawLine(topMid, h.Rotate, line);

        Handle(h.Rotate, circle: true);    // Drehen = Kreis
        Handle(h.Scale, circle: false);    // Skalieren = Quadrat
    }
}
