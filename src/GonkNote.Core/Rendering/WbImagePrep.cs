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
