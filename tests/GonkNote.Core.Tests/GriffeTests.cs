using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="WbHandles"/> — die Griffe an der Auswahl. Neu in Phase 4.5, aus demselben Grund
/// wie <see cref="WbHit"/> in Phase 3: der Linux-Kopf bekommt Drehen und Skalieren, und zwei
/// Abschriften derselben Formel driften auseinander, ohne dass es auffällt — die Griffe säßen
/// je Kopf ein paar Pixel anders, und niemand hätte einen Anhaltspunkt, welcher richtig liegt.
///
/// <para>
/// Getestet wird, was ein <b>Bedienfehler</b> wäre: ein Griff, der sich nicht treffen lässt,
/// ein Dreh-Griff, der beim Verschieben verschluckt wird, ein Klick auf leere Fläche, der als
/// „verschieben" gedeutet wird.
/// </para>
/// </summary>
public sealed class GriffeTests
{
    /// <summary>Ein Bild mit vorhersagbarem Kästchen: (100,100) bis (300,200).</summary>
    private static ImageElement Kasten(float drehung = 0f) => new()
    {
        X = 100, Y = 100, Width = 200, Height = 100, Rotation = drehung,
    };

    private static SKRect Kaestchen(WbElement el) => Core.Rendering.WbRenderer.ElementBounds(el);

    // ==================== Wo die Griffe sitzen ====================

    [Fact]
    public void Der_Skaliergriff_ist_die_untere_rechte_Ecke()
    {
        var g = WbHandles.Single(Kasten(), zoom: 1f);
        Assert.Equal(g.BR.X, g.Scale.X, 3);
        Assert.Equal(g.BR.Y, g.Scale.Y, 3);
    }

    [Fact]
    public void Der_Drehgriff_haengt_ueber_der_Oberkante()
    {
        var el = Kasten();
        var b = Kaestchen(el);
        var g = WbHandles.Single(el, zoom: 1f);

        Assert.Equal(b.MidX, g.Rotate.X, 3);
        // Oberkante minus Rahmenabstand minus Arm
        Assert.Equal(b.Top - WbHandles.PadPx - WbHandles.RotateArmPx, g.Rotate.Y, 3);
        Assert.True(g.Rotate.Y < g.TL.Y, "Der Drehgriff muss über dem Rahmen liegen.");
    }

    [Fact]
    public void Der_Rahmen_liegt_um_das_Element_herum()
    {
        var el = Kasten();
        var b = Kaestchen(el);
        var g = WbHandles.Single(el, zoom: 1f);

        Assert.Equal(b.Left - WbHandles.PadPx, g.TL.X, 3);
        Assert.Equal(b.Top - WbHandles.PadPx, g.TL.Y, 3);
        Assert.Equal(b.Right + WbHandles.PadPx, g.BR.X, 3);
        Assert.Equal(b.Bottom + WbHandles.PadPx, g.BR.Y, 3);
    }

    /// <summary>
    /// Bei doppelter Vergrößerung soll ein Griff auf dem Schirm <b>gleich groß</b> aussehen —
    /// in Weltkoordinaten heißt das: halb so weit weg.
    /// </summary>
    [Fact]
    public void Die_Griffe_haengen_am_Zoom_und_nicht_am_Element()
    {
        var el = Kasten();
        var b = Kaestchen(el);
        var einfach = WbHandles.Single(el, zoom: 1f);
        var doppelt = WbHandles.Single(el, zoom: 2f);

        Assert.Equal(b.Left - WbHandles.PadPx, einfach.TL.X, 3);
        Assert.Equal(b.Left - WbHandles.PadPx / 2f, doppelt.TL.X, 3);
    }

    // ==================== Drehung ====================

    [Fact]
    public void Ein_gedrehtes_Element_dreht_seine_Griffe_mit()
    {
        var gerade = WbHandles.Single(Kasten(), zoom: 1f);
        var schief = WbHandles.Single(Kasten(drehung: 90f), zoom: 1f);

        // Bei 90° wandert der Drehgriff von „über der Oberkante" nach rechts neben die Mitte.
        var b = Kaestchen(Kasten());
        Assert.Equal(b.MidY, schief.Rotate.Y, 2);
        Assert.True(schief.Rotate.X > b.MidX, "Bei 90° muss der Drehgriff rechts der Mitte liegen.");
        Assert.NotEqual(gerade.Rotate.X, schief.Rotate.X, 2);
    }

    /// <summary>
    /// Der Punkt liegt <b>innerhalb</b> des gedrehten Rahmens, aber außerhalb des ungedrehten.
    /// Wer die Drehung beim Treffen vergisst, greift ins Leere — und das ist der Fehler, der
    /// beim Zeichnen nicht auffällt, weil der Rahmen richtig aussieht.
    /// </summary>
    [Fact]
    public void Ein_gedrehtes_Element_wird_an_der_gedrehten_Stelle_getroffen()
    {
        var el = Kasten(drehung: 90f);
        var b = Kaestchen(Kasten());
        var mitte = new SKPoint(b.MidX, b.MidY);

        // Ein Punkt, der beim ungedrehten Kasten weit über der Oberkante läge — nach 90°
        // Drehung liegt er im Kasten, denn aus 200×100 wird 100×200.
        var p = new SKPoint(mitte.X, mitte.Y + 80);

        Assert.True(WbHandles.Contains(el, p, 1f));
        Assert.False(WbHandles.Contains(Kasten(), p, 1f));
    }

    [Fact]
    public void Das_Drehen_rastet_auf_fuenfzehn_Grad_ein()
    {
        var mitte = new SKPoint(0, 0);
        // Zeiger bei 1°: Startwinkel 0, Zeigerstart 0 → 1° liegt innerhalb der Toleranz von 3°.
        var fast = new SKPoint(MathF.Cos(1f * MathF.PI / 180f), MathF.Sin(1f * MathF.PI / 180f));
        Assert.Equal(0f, WbHandles.RotationFromDrag(mitte, fast, 0f, 0f), 3);
    }

    [Fact]
    public void Ausserhalb_der_Toleranz_wird_nicht_eingerastet()
    {
        var mitte = new SKPoint(0, 0);
        // Zeiger bei 7° — zu weit von 0° und von 15° entfernt, bleibt also stehen.
        var weit = new SKPoint(MathF.Cos(7f * MathF.PI / 180f), MathF.Sin(7f * MathF.PI / 180f));
        Assert.Equal(7f, WbHandles.RotationFromDrag(mitte, weit, 0f, 0f), 2);
    }

    // ==================== Die Weiche ====================

    /// <summary>
    /// <b>Der Wächter für den Fund beim Bauen.</b> Bei leerer Auswahl ist das Kästchen ein
    /// leeres Rechteck am Ursprung. Ohne die Abfrage auf die Anzahl läge um den Punkt (0,0)
    /// ein unsichtbarer Kasten von 12 Pixeln, und ein Klick dorthin — also auf die linke obere
    /// Ecke einer frischen Seite — würde als „verschieben" gedeutet. Es gäbe nichts zu
    /// verschieben, aber auch keine neue Auswahl und kein Lasso: der Klick liefe ins Leere.
    /// </summary>
    [Fact]
    public void Ohne_Auswahl_greift_kein_Griff_auch_nicht_am_Ursprung()
    {
        Assert.Equal(WbHandles.Grab.None,
            WbHandles.Probe(null, SKRect.Empty, count: 0, new SKPoint(0, 0), 1f));
        Assert.Equal(WbHandles.Grab.None,
            WbHandles.Probe(null, SKRect.Empty, count: 0, new SKPoint(5, 5), 1f));
    }

    /// <summary>
    /// Die Reihenfolge ist der Punkt: der Drehgriff hängt <b>außerhalb</b> des Rahmens, aber
    /// der Skaliergriff sitzt auf der Ecke und ragt in ihn hinein. Wer erst auf „innerhalb"
    /// prüft, verschluckt ihn.
    /// </summary>
    [Fact]
    public void Der_Skaliergriff_geht_dem_Verschieben_vor()
    {
        var el = Kasten();
        var g = WbHandles.Single(el, zoom: 1f);

        Assert.Equal(WbHandles.Grab.Scale,
            WbHandles.Probe(el, Kaestchen(el), count: 1, g.Scale, 1f));
    }

    [Fact]
    public void Der_Drehgriff_wird_getroffen()
    {
        var el = Kasten();
        var g = WbHandles.Single(el, zoom: 1f);

        Assert.Equal(WbHandles.Grab.Rotate,
            WbHandles.Probe(el, Kaestchen(el), count: 1, g.Rotate, 1f));
    }

    [Fact]
    public void Mitten_im_Element_wird_verschoben()
    {
        var el = Kasten();
        var b = Kaestchen(el);

        Assert.Equal(WbHandles.Grab.Move,
            WbHandles.Probe(el, b, count: 1, new SKPoint(b.MidX, b.MidY), 1f));
    }

    [Fact]
    public void Weit_daneben_faengt_eine_neue_Auswahl_an()
    {
        var el = Kasten();

        Assert.Equal(WbHandles.Grab.None,
            WbHandles.Probe(el, Kaestchen(el), count: 1, new SKPoint(900, 900), 1f));
    }

    /// <summary>
    /// Bei mehreren Elementen gibt es keinen Drehgriff — der Kasten ist achsenparallel. Ein
    /// Klick dorthin, wo bei Einzelauswahl der Drehgriff säße, darf deshalb <b>nicht</b>
    /// drehen.
    /// </summary>
    [Fact]
    public void Mehrfachauswahl_hat_keinen_Drehgriff()
    {
        var b = new SKRect(100, 100, 300, 200);
        var wo_der_drehgriff_waere = new SKPoint(b.MidX, b.Top - WbHandles.PadPx - WbHandles.RotateArmPx);

        Assert.NotEqual(WbHandles.Grab.Rotate,
            WbHandles.Probe(null, b, count: 2, wo_der_drehgriff_waere, 1f));
    }

    [Fact]
    public void Mehrfachauswahl_hat_einen_Skaliergriff_unten_rechts()
    {
        var b = new SKRect(100, 100, 300, 200);
        var ecke = new SKPoint(b.Right + WbHandles.BoxPadPx, b.Bottom + WbHandles.BoxPadPx);

        Assert.Equal(WbHandles.Grab.Scale, WbHandles.Probe(null, b, count: 2, ecke, 1f));
    }
}
