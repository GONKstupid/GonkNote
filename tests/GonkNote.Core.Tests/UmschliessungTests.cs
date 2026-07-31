using System.Runtime.CompilerServices;
using GonkNote.Core.Models;
using GonkNote.Core.Rendering;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="WbRenderer.ElementBounds"/> — die Grundlage für Auswählen, Verschieben,
/// Drehen, Radieren und den Seitenzuschnitt beim Export. Eine falsche Umschließung ist kein
/// Zeichenfehler, sondern ein Bedienfehler: das Element lässt sich nicht mehr anfassen.
/// </summary>
public sealed class UmschliessungTests
{
    /// <summary>
    /// Der Absturz aus dem SkiaSharp-3-Umstieg lag im **statischen** Konstruktor von
    /// <see cref="WbRenderer"/> (Bleistift-Körnung, <c>SKColorFilter.CreateTable</c> mit
    /// <c>null</c>). Er riss damit jeden Zeichenweg mit, nicht nur den Bleistift —
    /// <c>TypeInitializationException</c> beim ersten Zugriff auf die Klasse.
    /// <para>
    /// Dieser Test ist der billigste mögliche Wächter dafür: er berührt nur den Aufbau.
    /// </para>
    /// </summary>
    [Fact]
    public void Statischer_Aufbau_des_Renderers_wirft_nicht()
    {
        RuntimeHelpers.RunClassConstructor(typeof(WbRenderer).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(WbAidRenderer).TypeHandle);

        // WbFonts löst seine Schriften seit Phase 2 erst beim ersten Zugriff auf, damit der
        // Kopf UiFamily vorher setzen kann. Der Klassenkonstruktor allein prüft seitdem
        // nichts mehr — die beiden Zugriffe hier sind der eigentliche Wächter.
        Assert.NotNull(WbFonts.Regular);
        Assert.NotNull(WbFonts.Bold);
    }

    /// <summary>Die Strichbreite gehört zur Umschließung — sonst greift das Anklicken daneben.</summary>
    [Fact]
    public void Strich_umschliesst_auch_die_halbe_Strichbreite()
    {
        var box = WbRenderer.ElementBounds(new StrokeElement
        {
            Width = 10f,
            Points = { new WbPoint(100, 200, 1f), new WbPoint(300, 260, 1f) },
        });

        Assert.Equal(95f, box.Left);
        Assert.Equal(195f, box.Top);
        Assert.Equal(305f, box.Right);
        Assert.Equal(265f, box.Bottom);
    }

    /// <summary>Auch rückwärts gezogene Formen (X2 &lt; X1) haben eine positive Umschließung.</summary>
    [Fact]
    public void Rueckwaerts_gezogene_Form_hat_positive_Umschliessung()
    {
        var box = WbRenderer.ElementBounds(new ShapeElement
        {
            Shape = ShapeKind.Rectangle,
            X1 = 300, Y1 = 260, X2 = 100, Y2 = 200,
            StrokeWidth = 4f,
        });

        Assert.Equal(98f, box.Left);
        Assert.Equal(198f, box.Top);
        Assert.Equal(302f, box.Right);
        Assert.Equal(262f, box.Bottom);
        Assert.True(box.Width > 0 && box.Height > 0);
    }

    [Fact]
    public void Bild_und_Zettel_umschliessen_genau_ihr_Rechteck()
    {
        var bild = WbRenderer.ElementBounds(new ImageElement { X = 10, Y = 20, Width = 100, Height = 50 });
        Assert.Equal(10f, bild.Left);
        Assert.Equal(20f, bild.Top);
        Assert.Equal(110f, bild.Right);
        Assert.Equal(70f, bild.Bottom);

        var zettel = WbRenderer.ElementBounds(new StickyNoteElement { X = 5, Y = 6, Width = 200, Height = 150 });
        Assert.Equal(5f, zettel.Left);
        Assert.Equal(6f, zettel.Top);
        Assert.Equal(205f, zettel.Right);
        Assert.Equal(156f, zettel.Bottom);
    }

    /// <summary>
    /// Ein Dreieck steht auf der Grundlinie, die Spitze sitzt mittig oben — die Werte, gegen
    /// die auch das Einrasten des Formen-Stifts rechnet.
    /// </summary>
    [Fact]
    public void Dreieck_hat_die_Spitze_mittig_oben()
    {
        var (a, b, c) = WbRenderer.TrianglePoints(new ShapeElement
        {
            Shape = ShapeKind.Triangle,
            X1 = 100, Y1 = 50, X2 = 300, Y2 = 250,
        });

        Assert.Equal(200f, a.X);
        Assert.Equal(50f, a.Y);
        Assert.Equal(300f, b.X);
        Assert.Equal(250f, b.Y);
        Assert.Equal(100f, c.X);
        Assert.Equal(250f, c.Y);
    }

    [Fact]
    public void Kaputte_Farbangabe_wird_grau_und_wirft_nicht()
    {
        Assert.Equal(SkiaSharp.SKColors.Gray, WbRenderer.ParseColor("kein Farbwert"));
        Assert.Equal(SkiaSharp.SKColors.Gray, WbRenderer.ParseColor(""));
        Assert.Equal(SkiaSharp.SKColor.Parse("#FF1B2B4B"), WbRenderer.ParseColor("#FF1B2B4B"));
    }

    /// <summary>
    /// Verschieben und Skalieren müssen sich in der Umschließung wiederfinden — beides läuft
    /// über die abstrakten Methoden am Modell und ist damit der Weg, den Undo/Redo nimmt.
    /// </summary>
    [Fact]
    public void Verschieben_und_Skalieren_wirken_auf_jede_Elementklasse()
    {
        foreach (var el in Beispieldokument.AlleElemente())
        {
            var vorher = WbRenderer.ElementBounds(el);

            el.Translate(50, -20);
            var verschoben = WbRenderer.ElementBounds(el);
            Assert.Equal(vorher.Left + 50, verschoben.Left, 2);
            Assert.Equal(vorher.Top - 20, verschoben.Top, 2);
            Assert.Equal(vorher.Width, verschoben.Width, 2);
            Assert.Equal(vorher.Height, verschoben.Height, 2);

            el.Scale(2f, 0f, 0f);
            var skaliert = WbRenderer.ElementBounds(el);
            Assert.True(skaliert.Width > verschoben.Width,
                $"{el.GetType().Name} wurde nicht breiter beim Skalieren.");
            Assert.True(skaliert.Height > verschoben.Height,
                $"{el.GetType().Name} wurde nicht höher beim Skalieren.");
        }
    }

    /// <summary>
    /// Zettel und Text haben Untergrenzen beim Verkleinern — sonst schrumpft ein Zettel auf
    /// null und ist weg.
    /// </summary>
    [Fact]
    public void Zettel_und_Text_schrumpfen_nicht_auf_null()
    {
        var zettel = new StickyNoteElement { X = 0, Y = 0, Width = 200, Height = 200, FontSize = 16f };
        zettel.Scale(0.001f, 0, 0);
        Assert.True(zettel.Width >= 60f);
        Assert.True(zettel.Height >= 60f);
        Assert.True(zettel.FontSize >= 6f);

        var text = new GonkNote.Core.Models.TextElement { FontSize = 18f };
        text.Scale(0.001f, 0, 0);
        Assert.True(text.FontSize >= 4f);
    }
}
