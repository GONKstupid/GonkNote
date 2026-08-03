using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="WbHit"/> — Trefferprüfung und Lasso. Neu in Phase 3, weil der Linux-Kopf
/// dieselbe Geometrie braucht wie der WPF-Kopf und zwei Abschriften derselben Formel
/// auseinanderdriften, ohne dass es auffällt: die Auswahl säße dann je Kopf ein paar Pixel
/// anders, und niemand hätte einen Anhaltspunkt, welche der beiden richtig liegt.
///
/// <para>
/// Getestet wird, was ein Bedienfehler wäre und kein Zeichenfehler: ein Strich, der sich
/// nicht anfassen lässt, ein Radierer, der die halbe Seite mitnimmt, ein Lasso, das die
/// PDF-Seite hinter dem Strich einsammelt.
/// </para>
/// </summary>
public sealed class TrefferTests
{
    private static StrokeElement Strich(params (float X, float Y)[] punkte) => new()
    {
        Points = [.. punkte.Select(p => new WbPoint(p.X, p.Y, 0.5f))],
        Width = 2f,
    };

    // ==================== Striche ====================

    [Fact]
    public void Ein_Punkt_auf_dem_Strich_trifft()
    {
        var s = Strich((0, 0), (100, 0));
        Assert.True(WbHit.Hit(s, new SKPoint(50, 0), 1f));
    }

    [Fact]
    public void Ein_Punkt_neben_dem_Strich_trifft_nicht()
    {
        var s = Strich((0, 0), (100, 0));
        Assert.False(WbHit.Hit(s, new SKPoint(50, 40), 1f));
    }

    /// <summary>
    /// Die Strichstärke zählt zur Toleranz. Ein dicker Textmarker muss sich am sichtbaren
    /// Rand anfassen lassen und nicht nur auf seiner Mittellinie.
    /// </summary>
    [Fact]
    public void Die_Strichstaerke_zaehlt_zur_Toleranz()
    {
        var duenn = Strich((0, 0), (100, 0));
        var dick = Strich((0, 0), (100, 0));
        dick.Width = 40f;

        var knapp_daneben = new SKPoint(50, 15);
        Assert.False(WbHit.Hit(duenn, knapp_daneben, 1f));
        Assert.True(WbHit.Hit(dick, knapp_daneben, 1f));
    }

    /// <summary>
    /// Der Zwischenraum zweier weit auseinanderliegender Punkte gehört zum Strich — geprüft
    /// wird gegen die **Strecke**, nicht gegen die Punkte. Sonst hätte ein schnell gezogener
    /// Strich Löcher, an denen weder Radierer noch Auswahl greifen.
    /// </summary>
    [Fact]
    public void Zwischen_zwei_Punkten_ist_der_Strich_durchgehend()
    {
        var s = Strich((0, 0), (500, 0));
        Assert.True(WbHit.Hit(s, new SKPoint(250, 0), 1f));
    }

    // ==================== Drehung ====================

    /// <summary>
    /// Bei einem gedrehten Element wird der Zeiger zurückgedreht, nicht die Geometrie.
    /// Ohne das ließe sich ein gedrehtes Bild nur dort anfassen, wo es **vor** der Drehung
    /// lag — und das ist genau der Fehler, der aussieht, als reagiere die App nicht.
    /// </summary>
    [Fact]
    public void Ein_gedrehtes_Bild_wird_dort_getroffen_wo_es_zu_sehen_ist()
    {
        var bild = new ImageElement { X = 0, Y = 0, Width = 200, Height = 20, Rotation = 90f };

        // Mittelpunkt (100,10); um 90° gedreht liegt der lange Balken senkrecht.
        Assert.True(WbHit.Hit(bild, new SKPoint(100, 90), 1f));
        Assert.False(WbHit.Hit(bild, new SKPoint(190, 10), 1f));
    }

    // ==================== Formen ====================

    /// <summary>
    /// Eine Form zählt nur auf ihrer **Kontur**, nicht auf ihrer Fläche. Sonst nähme ein
    /// Radierstrich quer über ein großes Rechteck das ganze Rechteck mit.
    /// </summary>
    [Fact]
    public void Ein_Rechteck_trifft_auf_der_Kante_und_nicht_in_der_Mitte()
    {
        var r = new ShapeElement
        {
            Shape = ShapeKind.Rectangle, X1 = 0, Y1 = 0, X2 = 200, Y2 = 200, StrokeWidth = 2f,
        };

        Assert.True(WbHit.Hit(r, new SKPoint(100, 0), 2f));     // obere Kante
        Assert.False(WbHit.Hit(r, new SKPoint(100, 100), 2f));  // Mitte
    }

    [Fact]
    public void Eine_Ellipse_trifft_auf_dem_Rand()
    {
        var el = new ShapeElement
        {
            Shape = ShapeKind.Ellipse, X1 = 0, Y1 = 0, X2 = 200, Y2 = 100, StrokeWidth = 2f,
        };

        Assert.True(WbHit.Hit(el, new SKPoint(200, 50), 3f));   // rechter Scheitel
        Assert.False(WbHit.Hit(el, new SKPoint(100, 50), 3f));  // Mittelpunkt
    }

    // ==================== Reihenfolge ====================

    /// <summary>
    /// <b>Vordergrund vor Bildern.</b> Ein Strich auf einer importierten PDF-Seite muss sich
    /// greifen lassen, ohne die Seite mitzunehmen — sonst verschiebt der erste Griff das
    /// ganze Blatt statt der Notiz darauf. Das Bild liegt hier bewusst **hinter** dem
    /// Strich in der Liste und würde ohne die Sonderbehandlung trotzdem gewinnen, weil von
    /// hinten gesucht wird.
    /// </summary>
    [Fact]
    public void Ein_Strich_ueber_einem_Bild_wird_zuerst_gegriffen()
    {
        var bild = new ImageElement { X = 0, Y = 0, Width = 400, Height = 400 };
        var strich = Strich((100, 100), (300, 100));

        // Reihenfolge wie beim Import: erst der Strich, dann das Bild darüber gelegt.
        WbElement[] elemente = [strich, bild];

        Assert.Same(strich, WbHit.Topmost(elemente, new SKPoint(200, 100), 5f));
        // Abseits des Strichs bleibt das Bild der Treffer.
        Assert.Same(bild, WbHit.Topmost(elemente, new SKPoint(200, 300), 5f));
    }

    [Fact]
    public void Auf_leerer_Flaeche_wird_nichts_gegriffen()
    {
        WbElement[] elemente = [Strich((0, 0), (10, 10))];
        Assert.Null(WbHit.Topmost(elemente, new SKPoint(500, 500), 5f));
    }

    // ==================== Lasso ====================

    private static SKPoint[] Kasten(float l, float o, float r, float u) =>
        [new(l, o), new(r, o), new(r, u), new(l, u)];

    [Fact]
    public void Das_Lasso_faengt_was_es_umschliesst()
    {
        var drin = Strich((20, 20), (60, 60));
        var draussen = Strich((300, 300), (340, 340));

        var treffer = WbHit.InsideLasso([drin, draussen], Kasten(0, 0, 100, 100));

        Assert.Single(treffer);
        Assert.Same(drin, treffer[0]);
    }

    /// <summary>
    /// <b>Nur ~vollständig Umschlossenes zählt</b> (≥ 95 % der Punkte) — Nutzer-Wunsch aus
    /// V1. Ein Strich, der halb aus dem Lasso herausragt, war nicht gemeint; ihn trotzdem
    /// mitzunehmen ist die Art Fehler, die man erst nach dem Verschieben bemerkt.
    /// </summary>
    [Fact]
    public void Ein_halb_umschlossener_Strich_wird_nicht_gefangen()
    {
        var haelfte = Strich((20, 20), (40, 20), (60, 20), (300, 20), (400, 20));
        Assert.Empty(WbHit.InsideLasso([haelfte], Kasten(0, 0, 100, 100)));
    }

    /// <summary>
    /// Ein Bild wird nur mitgenommen, wenn **alle** Ecken drin liegen. Sonst fischt ein
    /// Lasso um eine Notiz herum die ganze PDF-Seite darunter mit heraus.
    /// </summary>
    [Fact]
    public void Ein_teilweise_umschlossenes_Bild_wird_nicht_gefangen()
    {
        var bild = new ImageElement { X = 50, Y = 50, Width = 400, Height = 400 };
        Assert.Empty(WbHit.InsideLasso([bild], Kasten(0, 0, 100, 100)));
    }

    [Fact]
    public void Ein_Lasso_aus_zwei_Punkten_faengt_nichts()
    {
        var s = Strich((20, 20), (60, 60));
        Assert.Empty(WbHit.InsideLasso([s], [new SKPoint(0, 0), new SKPoint(100, 100)]));
    }

    // ==================== Umschließung einer Auswahl ====================

    [Fact]
    public void Die_Auswahl_umschliesst_alle_ihre_Elemente()
    {
        var a = Strich((0, 0), (10, 10));
        var b = Strich((100, 100), (200, 150));

        var r = WbHit.Bounds([a, b]);

        Assert.True(r.Left <= 0 && r.Top <= 0);
        Assert.True(r.Right >= 200 && r.Bottom >= 150);
    }

    [Fact]
    public void Eine_leere_Auswahl_hat_eine_leere_Umschliessung()
    {
        Assert.True(WbHit.Bounds([]).IsEmpty);
    }
}
