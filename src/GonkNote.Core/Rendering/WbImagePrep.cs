using SkiaSharp;

namespace GonkNote.Core.Rendering;

/// <summary>
/// Bilder für die Aufnahme aufbereiten: verkleinern, neu kodieren, für die Texterkennung
/// vergrößern.
/// <para>
/// Stand bis Phase 2 zweimal im WPF-Kopf — als private Methode in
/// <c>WhiteboardView.Import</c> und in <c>OcrService</c>. Beide Wege sind reines SkiaSharp
/// und hatten nur deshalb keinen Test, weil sie privat im Kopf lagen; genau die beiden
/// Lücken nennt HANDOFF §4.4. Hier sind sie öffentlich und plattformneutral.
/// </para>
/// </summary>
public static class WbImagePrep
{
    /// <summary>Längste Kante, die ein importiertes Bild behalten darf (RAM- und DB-Ziel).</summary>
    public const int MaxImportDim = 2048;

    /// <summary>
    /// Bis zu dieser Dateigröße bleibt ein ausreichend kleines Bild unverändert. Neu zu
    /// kodieren würde es nur schlechter machen, nicht kleiner.
    /// </summary>
    public const int KeepAsIsBytes = 2 * 1024 * 1024;

    /// <summary>
    /// Dekodiert, verkleinert große Bilder und liefert speicherbare Bytes samt Pixelmaßen.
    /// <c>null</c> heißt „kein erkennbares Bildformat" — der Aufrufer meldet die Datei dann
    /// als fehlgeschlagen.
    /// <para>
    /// Kleine Bilder kommen <b>unverändert</b> zurück (dieselbe Byte-Folge, nicht nur derselbe
    /// Inhalt): ein PNG-Original soll durch den Import nicht zu einem JPEG werden.
    /// </para>
    /// </summary>
    public static (byte[] Data, int Width, int Height)? ForImport(byte[] raw)
    {
        // Nicht SKBitmap.Decode(raw): das wirft seit SkiaSharp 3, wenn die Datei kein
        // erkennbares Bild ist, statt null zu liefern (WbImages.Decode).
        using var bmp = WbImages.Decode(raw);
        if (bmp == null) return null;

        // Klein genug: Originalbytes unverändert übernehmen
        if (bmp.Width <= MaxImportDim && bmp.Height <= MaxImportDim && raw.Length <= KeepAsIsBytes)
            return (raw, bmp.Width, bmp.Height);

        float scale = Math.Min(1f, MaxImportDim / (float)Math.Max(bmp.Width, bmp.Height));
        SKBitmap use = bmp;
        if (scale < 1f)
        {
            int nw = Math.Max(1, (int)(bmp.Width * scale));
            int nh = Math.Max(1, (int)(bmp.Height * scale));
            use = bmp.Resize(new SKImageInfo(nw, nh), WbRenderer.HighSampling) ?? bmp;
        }
        try
        {
            // Nur wo es Durchsichtigkeit gibt, lohnt PNG; sonst ist JPEG deutlich kleiner.
            var format = HasTransparency(use) ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
            using var img = SKImage.FromBitmap(use);
            using var data = img.Encode(format, 88);
            return (data.ToArray(), use.Width, use.Height);
        }
        finally
        {
            if (!ReferenceEquals(use, bmp)) use.Dispose();
        }
    }

    /// <summary>Auf diese lange Kante werden kleine Bilder für die Texterkennung hochgezogen.</summary>
    public const int OcrTargetLongSide = 1600;

    /// <summary>
    /// Bereitet ein Bild für die Texterkennung auf: kleine Bilder werden hochskaliert
    /// (Erkenner arbeiten mit größerer Schrift deutlich zuverlässiger), alles wird
    /// verlustfrei als PNG neu kodiert. Große Seiten (PDF-Import) bleiben unverändert.
    /// <para>
    /// Ist <paramref name="raw"/> kein erkennbares Bild, kommt es <b>unverändert</b> zurück —
    /// die Erkennungs-Bibliothek soll ihre eigene Dekodierung versuchen dürfen.
    /// </para>
    /// </summary>
    public static byte[] ForOcr(byte[] raw)
    {
        using var bmp = WbImages.Decode(raw);
        if (bmp == null) return raw;

        int longSide = Math.Max(bmp.Width, bmp.Height);

        SKBitmap work = bmp;
        SKBitmap? scaled = null;
        if (longSide > 0 && longSide < OcrTargetLongSide)
        {
            float f = (float)OcrTargetLongSide / longSide;
            var info = new SKImageInfo(
                (int)Math.Round(bmp.Width * f),
                (int)Math.Round(bmp.Height * f),
                SKColorType.Bgra8888, SKAlphaType.Premul);
            scaled = bmp.Resize(info, WbRenderer.HighSampling);
            if (scaled != null) work = scaled;
        }

        try
        {
            using var img = SKImage.FromBitmap(work);
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        finally
        {
            scaled?.Dispose();
        }
    }

    /// <summary>
    /// Kleinste Kantenlänge, die eine SVG haben muss, um als Bild angenommen zu werden.
    /// <b>Sie ist gemessen und nicht gewählt:</b> für eine SVG mit leerer <c>viewBox</c> gibt
    /// <c>Svg.Skia</c> eine Zeichnung von <b>1×1</b> Punkt zurück — nicht <c>null</c>, und die
    /// Prüfung <c>&lt; 1</c> greift dabei nicht. Alles darunter wäre auf der Fläche ohnehin
    /// unsichtbar und nicht anzufassen.
    /// </summary>
    public const float MindestSvgKante = 2f;

    /// <summary>
    /// Rastert eine SVG-Datei für die Aufnahme: Ergebnis sind PNG-Bytes, <b>die Maße sind
    /// aber die der SVG-Zeichnung</b> und nicht die des Rasters.
    ///
    /// <para>
    /// <b>Warum doppelt gerastert wird.</b> Ein Vektorbild wird auf der Fläche gezoomt; mit
    /// der Anzeigegröße gerastert wäre es beim ersten Hineinzoomen unscharf. Der Faktor ist
    /// auf <b>2</b> gedeckelt und zusätzlich durch <see cref="MaxImportDim"/> begrenzt —
    /// eine SVG mit 4000 Punkten Kantenlänge soll kein 8000er-Raster ergeben.
    /// </para>
    /// <para>
    /// <b>Warum das hier steht und nicht im Kopf.</b> Es ist reines SkiaSharp plus
    /// <c>Svg.Skia</c>, und <c>Svg.Skia</c> ist seit jeher ein <b>Core</b>-Paket — der
    /// WPF-Kopf hatte diese Methode trotzdem privat (<c>WhiteboardView.Import.cs</c>,
    /// <c>RasterizeSvg</c>). Beim Portieren des Imports in den Linux-Kopf wäre sie sonst ein
    /// zweites Mal entstanden; dasselbe Muster wie <see cref="ForImport"/> in Phase 2.
    /// </para>
    /// <para>
    /// <b>⚠ <c>null</c> heißt „keine brauchbare SVG" — und dass diese Zusage hält, kostet
    /// zwei Vorkehrungen, die beide gemessen sind:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <b>Eine Datei, die gar kein XML ist, lässt <c>Svg.Skia</c> eine
    /// <c>XmlException</c> werfen</b> — es liefert kein <c>null</c>. Der WPF-Kopf hat das nie
    /// gemerkt, weil sein Aufrufer ohnehin alles fängt; wer die Methode neu benutzt, erbte
    /// diese Pflicht, ohne dass es irgendwo stünde. <b>Sie wird hier gefangen.</b>
    /// </description></item>
    /// <item><description>
    /// <b>Eine SVG mit leerer <c>viewBox</c> ergibt eine Zeichnung von 1×1 Punkt</b>, und die
    /// Prüfung <c>&lt; 1</c> — die der WPF-Kopf seit jeher hatte — greift dabei nicht: die
    /// Datei landete als <b>unsichtbares Ein-Pixel-Element</b> auf der Fläche, nicht zu sehen,
    /// nicht anzuklicken, ohne jede Meldung. Der Nutzer sieht „es passiert nichts". Dagegen
    /// steht <see cref="MindestSvgKante"/>.
    /// </description></item>
    /// </list>
    /// </summary>
    public static (byte[] Data, float Width, float Height)? ForSvg(byte[] raw)
    {
        using var svg = new Svg.Skia.SKSvg();
        using var strom = new MemoryStream(raw);
        try
        {
            if (svg.Load(strom) == null || svg.Picture == null) return null;
        }
        catch
        {
            // Kein XML, kaputtes XML, fremdes Format — für den Aufrufer ist das alles
            // dasselbe: „diese Datei nicht". Eine Ausnahme brächte ihm keine Auskunft, die er
            // anders behandeln könnte.
            return null;
        }

        var masse = svg.Picture.CullRect;
        if (masse.Width < MindestSvgKante || masse.Height < MindestSvgKante) return null;

        float faktor = Math.Min(2f, MaxImportDim / Math.Max(masse.Width, masse.Height));
        int breite = Math.Max(1, (int)(masse.Width * faktor));
        int hoehe = Math.Max(1, (int)(masse.Height * faktor));

        using var flaeche = SKSurface.Create(new SKImageInfo(breite, hoehe, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (flaeche == null) return null;

        flaeche.Canvas.Clear(SKColors.Transparent);
        flaeche.Canvas.Scale(faktor);
        // Eine SVG muss nicht bei (0,0) anfangen; ohne die Verschiebung bliebe links und oben
        // ein Rand stehen und rechts würde abgeschnitten.
        flaeche.Canvas.Translate(-masse.Left, -masse.Top);
        flaeche.Canvas.DrawPicture(svg.Picture);

        using var bild = flaeche.Snapshot();
        using var daten = bild.Encode(SKEncodedImageFormat.Png, 100);
        return (daten.ToArray(), masse.Width, masse.Height);
    }

    /// <summary>
    /// Hat das Bild durchsichtige Stellen? Wird abgetastet statt vollständig geprüft — bei
    /// einem 4000er-Bild wären das 16 Millionen Einzelabfragen für eine Ja/Nein-Frage.
    /// </summary>
    public static bool HasTransparency(SKBitmap bmp)
    {
        if (bmp.AlphaType == SKAlphaType.Opaque) return false;
        int sx = Math.Max(1, bmp.Width / 256), sy = Math.Max(1, bmp.Height / 256);
        for (int y = 0; y < bmp.Height; y += sy)
            for (int x = 0; x < bmp.Width; x += sx)
                if (bmp.GetPixel(x, y).Alpha < 250) return true;
        return false;
    }
}
