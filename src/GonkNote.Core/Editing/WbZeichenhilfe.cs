using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Editing;

/// <summary>Welche Zeichenhilfe gerade auf der Fläche liegt.</summary>
public enum Zeichenhilfe
{
    Keine,
    Lineal,
    Geodreieck,
}

/// <summary>
/// Die Geometrie von Lineal und Geodreieck: wo sie liegen, was man anfasst, und woran ein
/// Strich einrastet.
///
/// <para>
/// <b>Warum das hier steht und nicht im Kopf.</b> Bis Phase 4.5 lagen diese rund zweihundert
/// Zeilen privat in <c>WhiteboardView.Aids.cs</c> des WPF-Kopfs — und §6 hielt Lineal und
/// Geodreieck für „reine Bedienarbeit". <b>Das Messen hat das widerlegt:</b> gerechnet wird
/// mit Punkten, Winkeln und <c>Zoom</c>, und <c>Zoom</c> ist eine <b>Zahl</b>. Dieselbe Lage
/// wie bei <see cref="WbHandles"/> (§4.51) und <see cref="WbHit"/> (§4.13) — nach der
/// Faustregel aus HANDOFF §3 gehört sie damit hierher.
/// </para>
/// <para>
/// <b>Und der Anlass ist derselbe wie dort:</b> der Linux-Kopf bekommt beide Hilfen. Wer die
/// Formeln abschreibt, hat zwei Fassungen — und ein Lineal, an dem ein Strich je nach Kopf
/// ein paar Pixel anders einrastet, fällt niemandem auf, bis jemand dieselbe Zeichnung auf
/// beiden Rechnern anlegt.
/// </para>
/// </summary>
public static class WbZeichenhilfe
{
    // ==================== Maße ====================

    /// <summary>Länge des Lineals in Zeichenflächen-Einheiten (rund 18 cm).</summary>
    public const float LinealLaenge = 680f;

    /// <summary>Halbe Breite des Lineals — der Körper reicht von −26 bis +26 quer zur Achse.</summary>
    public const float LinealHalbBreite = 26f;

    /// <summary>
    /// Halbe Hypotenuse des Geodreiecks = 8 cm. <b>Sie ist zugleich seine Höhe</b>, denn es ist
    /// rechtwinklig und gleichschenklig — ein 16-cm-Geodreieck, wie die mitgelieferten SVGs.
    /// </summary>
    public static readonly float GeoHalbeHypotenuse = 8f * WbAidRenderer.PxPerCm;

    /// <summary>Bis zu diesem Abstand rastet ein beginnender Strich an einer Kante ein.</summary>
    public const float EinrastAbstand = 26f;

    /// <summary>Das Winkelraster beim Drehen (Grad).</summary>
    public const float WinkelSchritt = 15f;

    /// <summary>Wie nah an einem Rastwinkel gefangen wird; darüber dreht es frei.</summary>
    public const float WinkelFangbreite = 4f;

    // ==================== Lage und Form ====================

    /// <summary>Richtungsvektor (entlang) und Normale (quer) für einen Winkel in Grad.</summary>
    public static (SKPoint Entlang, SKPoint Quer) Achsen(float winkelGrad)
    {
        float a = winkelGrad * MathF.PI / 180f;
        var d = new SKPoint(MathF.Cos(a), MathF.Sin(a));
        return (d, new SKPoint(-d.Y, d.X));
    }

    /// <summary>
    /// Ein lokaler Punkt (<paramref name="u"/> entlang, <paramref name="v"/> quer) in
    /// Weltkoordinaten.
    /// </summary>
    public static SKPoint Punkt(SKPoint mitte, float winkelGrad, float u, float v)
    {
        var (d, n) = Achsen(winkelGrad);
        return new SKPoint(mitte.X + u * d.X + v * n.X, mitte.Y + u * d.Y + v * n.Y);
    }

    /// <summary>Die Eckpunkte in lokalen Koordinaten — beim Geodreieck der rechte Winkel oben.</summary>
    public static SKPoint[] Umriss(Zeichenhilfe art) => art switch
    {
        Zeichenhilfe.Lineal =>
        [
            new(-LinealLaenge / 2f, -LinealHalbBreite), new(LinealLaenge / 2f, -LinealHalbBreite),
            new(LinealLaenge / 2f, LinealHalbBreite), new(-LinealLaenge / 2f, LinealHalbBreite),
        ],
        Zeichenhilfe.Geodreieck =>
        [
            new(-GeoHalbeHypotenuse, 0f), new(GeoHalbeHypotenuse, 0f), new(0f, -GeoHalbeHypotenuse),
        ],
        _ => [],
    };

    /// <summary>
    /// Welche Kanten zum Einrasten taugen (Indizes in <see cref="Umriss"/>).
    /// <para>
    /// <b>Beim Lineal nur die zwei Längskanten</b> — an den kurzen Stirnseiten zu zeichnen
    /// ergibt keinen Strich, den jemand haben will. <b>Beim Geodreieck alle drei</b>:
    /// Hypotenuse und beide Katheten, denn genau dafür nimmt man es.
    /// </para>
    /// </summary>
    public static (int A, int B)[] Kanten(Zeichenhilfe art) => art switch
    {
        Zeichenhilfe.Lineal => [(0, 1), (3, 2)],
        Zeichenhilfe.Geodreieck => [(0, 1), (1, 2), (2, 0)],
        _ => [],
    };

    /// <summary>Der Umriss in Weltkoordinaten.</summary>
    public static SKPoint[] UmrissWelt(Zeichenhilfe art, SKPoint mitte, float winkelGrad)
    {
        var lokal = Umriss(art);
        var welt = new SKPoint[lokal.Length];
        for (int i = 0; i < lokal.Length; i++)
            welt[i] = Punkt(mitte, winkelGrad, lokal[i].X, lokal[i].Y);
        return welt;
    }

    /// <summary>Lokale x-Lage des rechten Endes — dort sitzt der Dreh-Griff.</summary>
    public static float RechtesEnde(Zeichenhilfe art) =>
        art == Zeichenhilfe.Geodreieck ? GeoHalbeHypotenuse : LinealLaenge / 2f;

    /// <summary>
    /// Mitte des Dreh-Griffs. Er sitzt <b>außerhalb</b> des Körpers, um 16 Bildschirmpixel
    /// versetzt — läge er darauf, wäre jeder Griff danach auch ein Verschieben.
    /// </summary>
    public static SKPoint Griffmitte(Zeichenhilfe art, SKPoint mitte, float winkelGrad, float zoom)
    {
        var (d, _) = Achsen(winkelGrad);
        var ende = Punkt(mitte, winkelGrad, RechtesEnde(art), 0f);
        float ab = 16f / zoom;
        return new SKPoint(ende.X + d.X * ab, ende.Y + d.Y * ab);
    }

    // ==================== Was der Zeiger trifft ====================

    /// <summary>Liegt <paramref name="p"/> im Vieleck? Strahlverfahren, ungerade Zahl = drinnen.</summary>
    public static bool ImVieleck(SKPoint[] eck, SKPoint p)
    {
        bool drin = false;
        for (int i = 0, j = eck.Length - 1; i < eck.Length; j = i++)
        {
            if (eck[i].Y > p.Y != eck[j].Y > p.Y &&
                p.X < (eck[j].X - eck[i].X) * (p.Y - eck[i].Y) / (eck[j].Y - eck[i].Y) + eck[i].X)
                drin = !drin;
        }
        return drin;
    }

    /// <summary>Trifft der Zeiger den Dreh-Griff? Der Fangkreis ist etwas größer als der gezeichnete Punkt.</summary>
    public static bool TrifftGriff(Zeichenhilfe art, SKPoint mitte, float winkelGrad, float zoom, SKPoint p)
    {
        var g = Griffmitte(art, mitte, winkelGrad, zoom);
        float r = 13f / zoom;
        float dx = p.X - g.X, dy = p.Y - g.Y;
        return dx * dx + dy * dy <= r * r;
    }

    /// <summary>Trifft der Zeiger den Körper?</summary>
    public static bool TrifftKoerper(Zeichenhilfe art, SKPoint mitte, float winkelGrad, SKPoint p) =>
        art != Zeichenhilfe.Keine && ImVieleck(UmrissWelt(art, mitte, winkelGrad), p);

    // ==================== Einrasten ====================

    /// <summary>Eine Kante, an der ein Strich klebt: ein Punkt darauf und ihre Richtung.</summary>
    public readonly record struct Einrastkante(SKPoint Anfang, SKPoint Richtung);

    /// <summary>
    /// Sucht die Kante, an der ein bei <paramref name="p"/> beginnender Strich einrasten soll —
    /// <c>null</c>, wenn keine nah genug ist.
    ///
    /// <para>
    /// <b>Die Kante gilt 80 Einheiten über ihre Enden hinaus.</b> Ohne diesen Überstand würde
    /// ein Strich, der knapp neben dem Linealende beginnt, nicht einrasten — und der Nutzer
    /// zöge eine krumme Linie, obwohl er am Lineal entlangfährt.
    /// </para>
    /// </summary>
    public static Einrastkante? Einrasten(Zeichenhilfe art, SKPoint mitte, float winkelGrad, SKPoint p)
    {
        if (art == Zeichenhilfe.Keine) return null;

        var eck = UmrissWelt(art, mitte, winkelGrad);
        float beste = float.MaxValue;
        Einrastkante? treffer = null;

        foreach (var (ia, ib) in Kanten(art))
        {
            var a = eck[ia];
            var b = eck[ib];
            float ex = b.X - a.X, ey = b.Y - a.Y;
            float laenge = MathF.Sqrt(ex * ex + ey * ey);
            if (laenge < 1f) continue;

            var richtung = new SKPoint(ex / laenge, ey / laenge);
            float t = (p.X - a.X) * richtung.X + (p.Y - a.Y) * richtung.Y;
            if (t < -80f || t > laenge + 80f) continue;

            var lot = new SKPoint(a.X + richtung.X * t, a.Y + richtung.Y * t);
            float abstand = MathF.Sqrt((p.X - lot.X) * (p.X - lot.X) + (p.Y - lot.Y) * (p.Y - lot.Y));
            if (abstand <= EinrastAbstand && abstand < beste)
            {
                beste = abstand;
                treffer = new Einrastkante(a, richtung);
            }
        }
        return treffer;
    }

    /// <summary>Zieht einen Punkt auf die eingerastete Kantenlinie (senkrechtes Lot).</summary>
    public static SKPoint AufKante(Einrastkante kante, SKPoint p)
    {
        float t = (p.X - kante.Anfang.X) * kante.Richtung.X + (p.Y - kante.Anfang.Y) * kante.Richtung.Y;
        return new SKPoint(kante.Anfang.X + kante.Richtung.X * t, kante.Anfang.Y + kante.Richtung.Y * t);
    }

    /// <summary>
    /// Fängt einen Drehwinkel an Vielfachen von <see cref="WinkelSchritt"/> — <b>aber nur
    /// innerhalb der Fangbreite</b>. Außerhalb dreht es frei, sonst ließe sich kein 37°-Winkel
    /// einstellen.
    /// </summary>
    public static float WinkelFangen(float grad)
    {
        float naechster = MathF.Round(grad / WinkelSchritt) * WinkelSchritt;
        return MathF.Abs(grad - naechster) <= WinkelFangbreite ? naechster : grad;
    }

    /// <summary>
    /// Der Winkel, den die Anzeige nennt: <b>0 bis 179°</b>, gegen die Waagerechte. Ein Lineal
    /// hat keine Vorder- und Rückseite — 190° und 10° sind dieselbe Lage, und zwei Zahlen für
    /// dieselbe Lage wären eine zu viel.
    /// </summary>
    public static float Anzeigewinkel(float grad) => ((grad % 180f) + 180f) % 180f;
}
