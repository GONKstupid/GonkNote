using GonkNote.Core.Editing;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="WbZeichenhilfe"/> — Lineal und Geodreieck. Neu in Phase 4.5 (§4.59), aus
/// demselben Grund wie <see cref="WbHandles"/> in §4.51: der Linux-Kopf bekommt beide Hilfen,
/// und zwei Abschriften derselben Formel driften auseinander, ohne dass es auffällt.
///
/// <para>
/// <b>Geprüft wird, was ein Bedienfehler wäre</b>: eine Kante, an der ein Strich nicht klebt;
/// ein Dreh-Griff, den man nicht trifft; ein Winkel, der sich nicht frei einstellen lässt.
/// </para>
/// </summary>
public sealed class ZeichenhilfeTests
{
    private static readonly SKPoint Mitte = new(400, 300);

    // ==================== Umriss und Lage ====================

    /// <summary>
    /// Das Lineal ist ein Rechteck um die Mitte — <b>und es liegt waagerecht bei 0°</b>, sonst
    /// stimmte die Skala nicht mit dem Aufdruck überein.
    /// </summary>
    [Fact]
    public void Das_Lineal_liegt_bei_null_Grad_waagerecht()
    {
        var eck = WbZeichenhilfe.UmrissWelt(Zeichenhilfe.Lineal, Mitte, 0f);

        Assert.Equal(4, eck.Length);
        Assert.Equal(Mitte.X - WbZeichenhilfe.LinealLaenge / 2f, eck[0].X, 3);
        Assert.Equal(Mitte.X + WbZeichenhilfe.LinealLaenge / 2f, eck[1].X, 3);
        Assert.Equal(eck[0].Y, eck[1].Y, 3);
    }

    /// <summary>
    /// Das Geodreieck ist rechtwinklig <b>und gleichschenklig</b>: die Höhe ist so lang wie die
    /// halbe Hypotenuse. Daran hängt, dass sich der SVG-Aufdruck mit dem Einrast-Polygon deckt.
    /// </summary>
    [Fact]
    public void Das_Geodreieck_ist_rechtwinklig_und_gleichschenklig()
    {
        var eck = WbZeichenhilfe.UmrissWelt(Zeichenhilfe.Geodreieck, Mitte, 0f);

        Assert.Equal(3, eck.Length);
        float halbeHyp = WbZeichenhilfe.GeoHalbeHypotenuse;

        Assert.Equal(2 * halbeHyp, eck[1].X - eck[0].X, 2);          // Hypotenuse
        Assert.Equal(halbeHyp, eck[0].Y - eck[2].Y, 2);              // Höhe (Spitze liegt oben)
        Assert.Equal(Mitte.X, eck[2].X, 3);                          // Spitze über der Mitte
    }

    /// <summary>16 cm — dieselbe Größe wie die mitgelieferten SVGs, sonst passt der Aufdruck nicht.</summary>
    [Fact]
    public void Das_Geodreieck_ist_sechzehn_Zentimeter_breit()
    {
        float breiteCm = 2 * WbZeichenhilfe.GeoHalbeHypotenuse / WbAidRenderer.PxPerCm;
        Assert.Equal(16f, breiteCm, 2);
    }

    /// <summary>Gedreht wird um die Mitte, nicht um eine Ecke — die Größe bleibt dabei gleich.</summary>
    [Fact]
    public void Drehen_laesst_die_Masse_unveraendert()
    {
        var gerade = WbZeichenhilfe.UmrissWelt(Zeichenhilfe.Lineal, Mitte, 0f);
        var schraeg = WbZeichenhilfe.UmrissWelt(Zeichenhilfe.Lineal, Mitte, 37f);

        static float Kante(SKPoint a, SKPoint b) =>
            MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        Assert.Equal(Kante(gerade[0], gerade[1]), Kante(schraeg[0], schraeg[1]), 2);
        Assert.Equal(Kante(gerade[1], gerade[2]), Kante(schraeg[1], schraeg[2]), 2);
    }

    // ==================== Was der Zeiger trifft ====================

    [Fact]
    public void Die_Mitte_liegt_im_Lineal_und_weit_daneben_nicht()
    {
        Assert.True(WbZeichenhilfe.TrifftKoerper(Zeichenhilfe.Lineal, Mitte, 0f, Mitte));
        Assert.False(WbZeichenhilfe.TrifftKoerper(Zeichenhilfe.Lineal, Mitte, 0f, new SKPoint(400, 900)));
    }

    /// <summary>
    /// <b>Ohne aktive Hilfe trifft nichts.</b> Sonst läge ein unsichtbares Rechteck um (0,0),
    /// und ein Klick auf die linke obere Ecke einer frischen Seite würde als „Lineal
    /// verschieben" gedeutet — genau der Fehler, den §4.51 bei den Griffen gefunden hat.
    /// </summary>
    [Fact]
    public void Ohne_Hilfe_trifft_nichts()
    {
        Assert.False(WbZeichenhilfe.TrifftKoerper(Zeichenhilfe.Keine, Mitte, 0f, Mitte));
        Assert.Empty(WbZeichenhilfe.UmrissWelt(Zeichenhilfe.Keine, Mitte, 0f));
        Assert.Empty(WbZeichenhilfe.Kanten(Zeichenhilfe.Keine));
    }

    /// <summary>
    /// Der Dreh-Griff sitzt <b>außerhalb</b> des Körpers. Läge er darauf, wäre jeder Griff
    /// danach auch ein Verschieben — und man könnte nicht mehr drehen, ohne zu verrutschen.
    /// </summary>
    [Fact]
    public void Der_Drehgriff_liegt_ausserhalb_des_Koerpers()
    {
        var griff = WbZeichenhilfe.Griffmitte(Zeichenhilfe.Lineal, Mitte, 0f, zoom: 1f);

        Assert.True(griff.X > Mitte.X + WbZeichenhilfe.LinealLaenge / 2f,
            "Der Griff muss hinter dem rechten Ende liegen.");
        Assert.False(WbZeichenhilfe.TrifftKoerper(Zeichenhilfe.Lineal, Mitte, 0f, griff));
        Assert.True(WbZeichenhilfe.TrifftGriff(Zeichenhilfe.Lineal, Mitte, 0f, 1f, griff));
    }

    /// <summary>
    /// <b>Der Fangkreis wächst beim Herauszoomen mit</b> — er ist in Bildschirmpixeln gedacht.
    /// Ohne das wäre der Griff bei 25 % Zoom ein Ziel von wenigen Pixeln.
    /// </summary>
    [Fact]
    public void Der_Fangkreis_haengt_am_Zoom()
    {
        var griff = WbZeichenhilfe.Griffmitte(Zeichenhilfe.Lineal, Mitte, 0f, zoom: 0.25f);
        var knappDaneben = new SKPoint(griff.X + 40f, griff.Y);

        Assert.True(WbZeichenhilfe.TrifftGriff(Zeichenhilfe.Lineal, Mitte, 0f, 0.25f, knappDaneben));
        Assert.False(WbZeichenhilfe.TrifftGriff(Zeichenhilfe.Lineal, Mitte, 0f, 1f, knappDaneben));
    }

    // ==================== Einrasten ====================

    /// <summary>Ein Strich, der nahe an der Längskante beginnt, klebt an ihr.</summary>
    [Fact]
    public void Nahe_der_Kante_wird_eingerastet()
    {
        var nahe = new SKPoint(Mitte.X, Mitte.Y - WbZeichenhilfe.LinealHalbBreite - 5f);
        var kante = WbZeichenhilfe.Einrasten(Zeichenhilfe.Lineal, Mitte, 0f, nahe);

        Assert.NotNull(kante);

        // Auf der Kante liegen heißt: dieselbe Höhe wie die Kante, x unverändert.
        var gezogen = WbZeichenhilfe.AufKante(kante!.Value, nahe);
        Assert.Equal(Mitte.Y - WbZeichenhilfe.LinealHalbBreite, gezogen.Y, 2);
        Assert.Equal(nahe.X, gezogen.X, 2);
    }

    /// <summary>Weit weg wird nicht eingerastet — sonst könnte man neben dem Lineal nicht frei zeichnen.</summary>
    [Fact]
    public void Weit_weg_wird_nicht_eingerastet()
    {
        var weit = new SKPoint(Mitte.X, Mitte.Y - WbZeichenhilfe.LinealHalbBreite - 200f);
        Assert.Null(WbZeichenhilfe.Einrasten(Zeichenhilfe.Lineal, Mitte, 0f, weit));
    }

    /// <summary>
    /// <b>Die Kante gilt über ihre Enden hinaus.</b> Ohne diesen Überstand würde ein Strich,
    /// der knapp neben dem Linealende beginnt, nicht einrasten — und der Nutzer zöge eine
    /// krumme Linie, obwohl er am Lineal entlangfährt.
    /// </summary>
    [Fact]
    public void Knapp_hinter_dem_Ende_rastet_noch_ein()
    {
        var dahinter = new SKPoint(Mitte.X + WbZeichenhilfe.LinealLaenge / 2f + 50f,
                                   Mitte.Y - WbZeichenhilfe.LinealHalbBreite);
        Assert.NotNull(WbZeichenhilfe.Einrasten(Zeichenhilfe.Lineal, Mitte, 0f, dahinter));
    }

    /// <summary>Das Geodreieck rastet an <b>allen drei</b> Kanten ein — genau dafür nimmt man es.</summary>
    [Fact]
    public void Das_Geodreieck_hat_drei_Einrastkanten()
    {
        Assert.Equal(3, WbZeichenhilfe.Kanten(Zeichenhilfe.Geodreieck).Length);

        var eck = WbZeichenhilfe.UmrissWelt(Zeichenhilfe.Geodreieck, Mitte, 0f);
        foreach (var (a, b) in WbZeichenhilfe.Kanten(Zeichenhilfe.Geodreieck))
        {
            var kantenmitte = new SKPoint((eck[a].X + eck[b].X) / 2f, (eck[a].Y + eck[b].Y) / 2f);
            Assert.NotNull(WbZeichenhilfe.Einrasten(Zeichenhilfe.Geodreieck, Mitte, 0f, kantenmitte));
        }
    }

    /// <summary>
    /// <b>Das Lineal rastet nur an den Längskanten ein.</b> An den kurzen Stirnseiten ergäbe ein
    /// Strich nichts, was jemand haben will — und er läge quer zur Richtung, in die der Nutzer
    /// zieht.
    /// </summary>
    [Fact]
    public void Das_Lineal_hat_nur_zwei_Einrastkanten()
    {
        var kanten = WbZeichenhilfe.Kanten(Zeichenhilfe.Lineal);
        Assert.Equal(2, kanten.Length);

        var eck = WbZeichenhilfe.UmrissWelt(Zeichenhilfe.Lineal, Mitte, 0f);
        foreach (var (a, b) in kanten)
            Assert.Equal(eck[a].Y, eck[b].Y, 3);   // beide waagerecht bei 0° = Längskanten
    }

    // ==================== Winkel ====================

    [Theory]
    [InlineData(2f, 0f)]         // knapp neben 0 → gefangen
    [InlineData(13f, 15f)]       // knapp unter 15 → gefangen
    [InlineData(46f, 45f)]       // knapp über 45 → gefangen
    [InlineData(-31f, -30f)]     // auch negativ
    public void Nahe_Winkel_rasten_ein(float roh, float erwartet) =>
        Assert.Equal(erwartet, WbZeichenhilfe.WinkelFangen(roh), 3);

    /// <summary>
    /// <b>Außerhalb der Fangbreite dreht es frei.</b> Sonst ließe sich kein 37°-Winkel
    /// einstellen — und ein Werkzeug, das nur Vielfache von 15 kann, ist kein Geodreieck.
    /// </summary>
    [Theory]
    [InlineData(37f)]
    [InlineData(22f)]
    [InlineData(8f)]
    public void Ferne_Winkel_bleiben_frei(float roh) =>
        Assert.Equal(roh, WbZeichenhilfe.WinkelFangen(roh), 3);

    /// <summary>
    /// Die Anzeige nennt 0 bis 179°. <b>Ein Lineal hat keine Vorder- und Rückseite</b> — 190°
    /// und 10° sind dieselbe Lage, und zwei Zahlen dafür wären eine zu viel.
    /// </summary>
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(10f, 10f)]
    [InlineData(190f, 10f)]
    [InlineData(-10f, 170f)]
    [InlineData(360f, 0f)]
    public void Der_Anzeigewinkel_bleibt_unter_hundertachtzig(float roh, float erwartet) =>
        Assert.Equal(erwartet, WbZeichenhilfe.Anzeigewinkel(roh), 3);
}
