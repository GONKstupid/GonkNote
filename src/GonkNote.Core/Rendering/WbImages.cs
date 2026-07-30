using SkiaSharp;

namespace GonkNote.Core.Rendering;

/// <summary>
/// Bilddaten dekodieren — die eine Stelle, an der aus Bytes ein Bitmap wird.
/// </summary>
public static class WbImages
{
    /// <summary>
    /// Dekodiert Bildbytes; <c>null</c>, wenn die Daten kein erkennbares Bildformat sind.
    /// <para>
    /// **Warum das nicht einfach <c>SKBitmap.Decode(bytes)</c> ist:** Bis SkiaSharp 2.88 gab
    /// dieser Aufruf bei unbrauchbaren Daten <c>null</c> zurück. Seit 3.x legt er intern
    /// einen <see cref="SKCodec"/> an und reicht ihn an <c>Decode(SKCodec)</c> weiter — ist
    /// das Format unbekannt, ist der Codec <c>null</c> und der Aufruf wirft
    /// <see cref="ArgumentNullException"/>. Die übliche Prüfung <c>if (bmp == null)</c>
    /// dahinter wird also nie erreicht.
    /// </para>
    /// Das ist dieselbe Falle wie bei <c>SKColorFilter.CreateTable(a, null, null, null)</c>
    /// (HANDOFF §7): ein Aufruf, der früher „kann nichts" mit <c>null</c> beantwortete und
    /// heute wirft. Beide bauen fehlerfrei. Gefunden hat es der Snapshot-Test
    /// <c>Kaputtes_Bild_bekommt_einen_Platzhalter</c>.
    /// <para>
    /// **Ein kaputtes Bild darf nie das Zeichnen abbrechen.** Sonst kostet ein einziges
    /// halb geschriebenes Blob die ganze Seite — und das Muster „null heißt: zeichne einen
    /// Platzhalter" ist an allen Aufrufstellen schon vorhanden.
    /// </para>
    /// </summary>
    public static SKBitmap? Decode(byte[]? encoded)
    {
        if (encoded is not { Length: > 0 }) return null;

        // CreateCopy statt Create(IntPtr): die Bytes gehören dem Aufrufer, und der
        // SKCodec würde sie sonst über seine Lebensdauer hinaus festhalten.
        using var data = SKData.CreateCopy(encoded);
        using var codec = SKCodec.Create(data);
        return codec == null ? null : SKBitmap.Decode(codec);
    }
}
