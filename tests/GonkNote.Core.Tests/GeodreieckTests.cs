using System.IO;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Das Geodreieck-Overlay — der einzige Weg im Kern, der über **Svg.Skia** läuft.
/// <para>
/// Warum eigens geprüft: Svg.Skia 5.1.1 ist die erste Fassung auf SkiaSharp ≥ 3.119.2, und
/// der Ladeweg ist dreistufig (eigene Grafik des Nutzers → mitgelieferte neben der Exe →
/// Kontur im Code). Jede Stufe fällt **still** auf die nächste zurück. Ein kaputter
/// SVG-Ladeweg sieht deshalb nicht wie ein Fehler aus, sondern wie ein Geodreieck ohne
/// Skalen — und das fällt erst dem Nutzer auf.
/// </para>
/// Beide Stufen stecken in **einem** Test, und zwar mit Absicht: <c>WbAidRenderer</c> hält die
/// geladene Grafik in einem statischen Feld. Wäre der Eigenbau ein eigener Test, entschiede
/// die Reihenfolge der Tests über sein Ergebnis — mal grün, mal rot, nie erklärlich.
/// </summary>
public sealed class GeodreieckTests
{
    [Fact]
    public void Beide_Ladestufen_zeichnen_ein_Geodreieck()
    {
        using var leer = new TempWorkspace("geodreieck");

        // Der Nutzerordner zeigt in beiden Fällen ins Leere. Sonst hinge der Test daran, ob
        // auf diesem Rechner eine eigene Grafik in %APPDATA%\GonkNote liegt.
        WbAidRenderer.UserAssetFolder = leer.Root;

        // --- Stufe 3: keine Grafik zu finden → Kontur im Code -----------------------------
        string mitgeliefert = WbAidRenderer.AppAssetFolder;
        WbAidRenderer.AppAssetFolder = leer.Root;

        var eigenbau = Farbfleck.Von(700, 500, c =>
            WbAidRenderer.DrawSetSquare(c, new SKPoint(350, 200), 0f, 1f, dark: false));

        Assert.False(eigenbau.Leer, "Ohne Grafik wurde gar nichts gezeichnet.");
        // Maßgleich muss der Eigenbau sein: 16 cm Hypotenuse bei 37,795 px/cm ≈ 605 px.
        int erwarteteBreite = (int)MathF.Round(16f * WbAidRenderer.PxPerCm);
        Assert.InRange(eigenbau.Umschliessung.Width, erwarteteBreite - 4, erwarteteBreite + 4);

        Snapshot.Assert("geodreieck-eigenbau", 700, 500, c =>
            WbAidRenderer.DrawSetSquare(c, new SKPoint(350, 200), 0f, 1f, dark: false));

        // --- Stufe 2: die mitgelieferte SVG-Grafik ----------------------------------------
        WbAidRenderer.AppAssetFolder = mitgeliefert;
        Assert.True(File.Exists(Path.Combine(mitgeliefert, "Geodreieck-Light.svg")),
            $"Die mitgelieferte Grafik fehlt neben der Test-Assembly ({mitgeliefert}) — " +
            "dann prüft dieser Test den Svg.Skia-Weg gar nicht. Siehe .csproj.");

        var ausSvg = Farbfleck.Von(700, 500, c =>
            WbAidRenderer.DrawSetSquare(c, new SKPoint(350, 200), 0f, 1f, dark: false));

        Assert.False(ausSvg.Leer, "Die SVG-Grafik wurde nicht gezeichnet.");

        // Die Grafik bringt Skalen und Beschriftung mit, der Eigenbau nicht: deutlich mehr
        // Farbe auf gleicher Fläche. Das ist der Unterschied, an dem man erkennt, dass
        // wirklich das SVG genommen wurde und nicht still die Kontur.
        Assert.True(ausSvg.Pixel > eigenbau.Pixel,
            $"Aus dem SVG kam nicht mehr heraus als aus dem Eigenbau " +
            $"({ausSvg.Pixel} vs. {eigenbau.Pixel}) — vermutlich ist der SVG-Ladeweg still " +
            "auf die Kontur zurückgefallen.");

        // Dieselbe Vermessung wie beim Eigenbau: 1 Geodreieck-cm = 1 Seiten-cm.
        Assert.InRange(ausSvg.Umschliessung.Width, erwarteteBreite - 8, erwarteteBreite + 8);

        Snapshot.Assert("geodreieck-svg", 700, 500, c =>
            WbAidRenderer.DrawSetSquare(c, new SKPoint(350, 200), 0f, 1f, dark: false));

        // --- Dunkle Fassung ----------------------------------------------------------------
        var dunkel = Farbfleck.Von(700, 500, c =>
            WbAidRenderer.DrawSetSquare(c, new SKPoint(350, 200), 0f, 1f, dark: true));
        Assert.False(dunkel.Leer, "Die dunkle Fassung wurde nicht gezeichnet.");

        // --- Drehung ----------------------------------------------------------------------
        var gedreht = Farbfleck.Von(700, 700, c =>
            WbAidRenderer.DrawSetSquare(c, new SKPoint(350, 350), 90f, 1f, dark: false));
        Assert.False(gedreht.Leer);
        // Um 90° gedreht ist die Umschließung hoch statt breit.
        Assert.True(gedreht.Umschliessung.Height > gedreht.Umschliessung.Width,
            $"Die Drehung um 90° hat nicht gewirkt: {gedreht.Umschliessung}");
    }
}
