using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die Neigung des Stifts (<see cref="WbPoint.TX"/>/<see cref="WbPoint.TY"/>), neu in
/// Phase 3.
///
/// <para>
/// <b>Der wichtigste Test hier ist der erste</b>, und er prüft, dass sich <i>nichts</i>
/// ändert. Neigung ist eine Erweiterung des gespeicherten Formats und greift in den
/// Renderer ein, den sich Linux-Kopf, Windows-Kopf und PDF-Export teilen. Die ganze
/// Änderung steht und fällt damit, dass ein Dokument <b>ohne</b> Neigungsangabe exakt so
/// aussieht wie vorher — sonst wären zwanzig Golden-Files aus Phase 1 neu zu setzen, und
/// der WPF-Kopf zeichnete Bestandsdokumente anders, ohne dass es hier jemand sähe.
/// </para>
/// </summary>
public sealed class NeigungTests
{
    private static StrokeElement Bleistift(float tiltX, float tiltY) => new()
    {
        Kind = StrokeKind.Pencil,
        Color = "#FF000000",
        Width = 4f,
        Points =
        {
            new WbPoint(20, 100, 0.6f, tiltX, tiltY),
            new WbPoint(100, 100, 0.6f, tiltX, tiltY),
            new WbPoint(180, 100, 0.6f, tiltX, tiltY),
        },
    };

    // ==================== Der Rückweg: ohne Neigung ändert sich nichts ====================

    /// <summary>
    /// <b>Ohne Neigung ist der Faktor exakt 1.</b> Nicht „ungefähr 1" — exakt, denn jede
    /// Abweichung wanderte in die Strichbreite und damit in jeden Pixelhash aus Phase 1.
    /// </summary>
    [Fact]
    public void Ohne_Neigung_ist_der_Breitenfaktor_genau_eins()
    {
        Assert.Equal(1f, WbRenderer.TiltWidthFactor(Bleistift(0f, 0f)));
    }

    [Fact]
    public void Ein_Strich_ohne_Punkte_hat_den_Faktor_eins()
    {
        Assert.Equal(1f, WbRenderer.TiltWidthFactor(new StrokeElement()));
    }

    /// <summary>
    /// Ein Strich aus der Zeit vor dieser Änderung — angelegt über den alten Konstruktor,
    /// also genau so, wie ihn ein eingelesenes Bestandsdokument liefert — zeichnet
    /// **pixelgleich** zu einem, der ausdrücklich Neigung 0 trägt.
    /// </summary>
    [Fact]
    public void Ein_alter_Strich_zeichnet_wie_einer_mit_Neigung_null()
    {
        var alt = new StrokeElement
        {
            Kind = StrokeKind.Pencil,
            Color = "#FF000000",
            Width = 4f,
            Points =
            {
                new WbPoint(20, 100, 0.6f),
                new WbPoint(100, 100, 0.6f),
                new WbPoint(180, 100, 0.6f),
            },
        };

        var a = Farbfleck.Von(200, 200, c => WbRenderer.DrawStroke(c, alt));
        var b = Farbfleck.Von(200, 200, c => WbRenderer.DrawStroke(c, Bleistift(0f, 0f)));

        Assert.Equal(a.Pixel, b.Pixel);
        Assert.Equal(a.Umschliessung, b.Umschliessung);
    }

    // ==================== Die Wirkung ====================

    [Fact]
    public void Mehr_Neigung_verbreitert_den_Strich()
    {
        float senkrecht = WbRenderer.TiltWidthFactor(Bleistift(0f, 0f));
        float schraeg = WbRenderer.TiltWidthFactor(Bleistift(30f, 0f));
        float flach = WbRenderer.TiltWidthFactor(Bleistift(60f, 0f));

        Assert.True(senkrecht < schraeg, $"{senkrecht} < {schraeg}");
        Assert.True(schraeg < flach, $"{schraeg} < {flach}");
    }

    /// <summary>
    /// Jenseits der vollen Neigung wächst nichts mehr. Ohne diese Grenze zöge ein Gerät,
    /// das 90° meldet, einen Balken statt eines Strichs.
    /// </summary>
    [Fact]
    public void Jenseits_der_vollen_Neigung_waechst_der_Strich_nicht_weiter()
    {
        Assert.Equal(WbRenderer.TiltWidthFactor(Bleistift(60f, 0f)),
                     WbRenderer.TiltWidthFactor(Bleistift(90f, 0f)));
    }

    /// <summary>Die Richtung der Neigung zählt nicht, nur ihr Betrag — Kippen ist Kippen.</summary>
    [Fact]
    public void Die_Richtung_der_Neigung_aendert_die_Breite_nicht()
    {
        Assert.Equal(WbRenderer.TiltWidthFactor(Bleistift(40f, 0f)),
                     WbRenderer.TiltWidthFactor(Bleistift(-40f, 0f)));
        Assert.Equal(WbRenderer.TiltWidthFactor(Bleistift(0f, 40f)),
                     WbRenderer.TiltWidthFactor(Bleistift(0f, -40f)));
    }

    /// <summary>
    /// Und das Ganze auch auf dem Papier, nicht nur in der Formel: ein geneigt gezogener
    /// Bleistift deckt mehr Fläche und wird höher.
    /// </summary>
    [Fact]
    public void Ein_geneigter_Bleistift_zeichnet_breiter()
    {
        var senkrecht = Farbfleck.Von(240, 200, c => WbRenderer.DrawStroke(c, Bleistift(0f, 0f)));
        var geneigt = Farbfleck.Von(240, 200, c => WbRenderer.DrawStroke(c, Bleistift(55f, 0f)));

        Assert.False(senkrecht.Leer);
        Assert.True(geneigt.Pixel > senkrecht.Pixel,
            $"geneigt {geneigt.Pixel} sollte mehr sein als senkrecht {senkrecht.Pixel}");
        Assert.True(geneigt.Umschliessung.Height > senkrecht.Umschliessung.Height,
            $"geneigt {geneigt.Umschliessung.Height} px hoch, senkrecht {senkrecht.Umschliessung.Height} px");
    }

    /// <summary>
    /// <b>Nur der Bleistift reagiert.</b> Ein Fineliner hat eine feste Spitze; ihn durch
    /// Kippen breiter zu machen wäre erfunden. Der Test hält die Entscheidung fest, damit
    /// sie nicht später aus Versehen ausgeweitet wird.
    /// </summary>
    [Theory]
    [InlineData(StrokeKind.Pen)]
    [InlineData(StrokeKind.Highlighter)]
    public void Nur_der_Bleistift_reagiert_auf_Neigung(StrokeKind art)
    {
        StrokeElement Strich(float tilt) => new()
        {
            Kind = art,
            Color = "#FF000000",
            Width = 4f,
            Points =
            {
                new WbPoint(20, 100, 0.6f, tilt, 0f),
                new WbPoint(100, 100, 0.6f, tilt, 0f),
                new WbPoint(180, 100, 0.6f, tilt, 0f),
            },
        };

        var ohne = Farbfleck.Von(240, 200, c => WbRenderer.DrawStroke(c, Strich(0f)));
        var mit = Farbfleck.Von(240, 200, c => WbRenderer.DrawStroke(c, Strich(60f)));

        Assert.Equal(ohne.Pixel, mit.Pixel);
        Assert.Equal(ohne.Umschliessung, mit.Umschliessung);
    }

    // ==================== Der Mittelwert ====================

    /// <summary>
    /// Gerechnet wird mit der **mittleren** Neigung des Strichs. Ein Strich, der zur Hälfte
    /// senkrecht und zur Hälfte flach geführt wurde, liegt damit zwischen beiden — und
    /// nicht bei einem der Extreme.
    /// </summary>
    [Fact]
    public void Gerechnet_wird_mit_der_mittleren_Neigung()
    {
        var gemischt = new StrokeElement
        {
            Kind = StrokeKind.Pencil,
            Width = 4f,
            Points =
            {
                new WbPoint(20, 100, 0.6f, 0f, 0f),
                new WbPoint(180, 100, 0.6f, 60f, 0f),
            },
        };

        float f = WbRenderer.TiltWidthFactor(gemischt);

        Assert.True(f > WbRenderer.TiltWidthFactor(Bleistift(0f, 0f)));
        Assert.True(f < WbRenderer.TiltWidthFactor(Bleistift(60f, 0f)));
    }
}
