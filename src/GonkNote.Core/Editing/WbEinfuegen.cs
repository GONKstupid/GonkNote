using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Editing;

/// <summary>
/// Wo ein eingefügtes Bild landet und wie groß es dabei wird.
///
/// <para>
/// <b>Warum das hier steht und nicht im Kopf.</b> Es ist reine Rechnung — eine Zielkante, ein
/// Mittelpunkt, die Maße der Seite — und sie muss in beiden Köpfen dieselbe sein: ein Sticker,
/// der unter Linux anders groß ankommt als unter Windows, wäre ein Unterschied <b>in der
/// Datei</b> und nicht bloß auf dem Schirm. Bis Phase 4.5 lag sie privat in
/// <c>WhiteboardView.Stickers.cs</c> des WPF-Kopfs. Dasselbe Muster wie
/// <see cref="WbHandles"/> (§4.51): was ohne Steuerelement auskommt, gehört nach Core.
/// </para>
/// <para>
/// <b>⚠ Ein Unterschied, der bewusst stehen bleibt.</b> Der WPF-Kopf hat für dasselbe
/// Bedürfnis eine <em>zweite</em> Rechnung: <c>PlaceImages</c> (Bilder aus Datei,
/// Zwischenablage, OCR) passt in <b>60 % des Sichtbereichs</b> bzw. <b>70 % der Seite</b> ein
/// und versetzt mehrere Bilder kaskadenförmig um 24. Ein Sticker ist etwas anderes: er soll
/// klein und immer gleich groß kommen, weil man ihn als Marke danebensetzt und nicht als Bild
/// einfügt. <b>Die beiden zusammenzuziehen wäre eine Verhaltensänderung ohne Auftrag</b> —
/// hier steht nur die Sticker-Rechnung, und dass es die andere gibt, steht dabei.
/// </para>
/// </summary>
public static class WbEinfuegen
{
    /// <summary>Die lange Kante eines eingefügten Stickers, in Zeichenflächen-Einheiten.</summary>
    public const float StickerKante = 160f;

    /// <summary>
    /// Rechnet Lage und Größe für einen Sticker aus: er wird auf <see cref="StickerKante"/>
    /// heruntergerechnet (<b>nie hinaufgerechnet</b> — ein kleines Bild groß zu ziehen macht
    /// es nur unscharf), um <paramref name="mitte"/> zentriert und auf einer endlichen Seite
    /// so verschoben, dass er ganz darauf liegt.
    /// </summary>
    /// <param name="quellBreite">Pixelbreite des vorbereiteten Bildes.</param>
    /// <param name="quellHoehe">Pixelhöhe des vorbereiteten Bildes.</param>
    /// <param name="mitte">Wohin der Sticker zentriert werden soll (Zeichenflächen-Einheiten).</param>
    /// <param name="seite">Die Seite — sie sagt, ob geklemmt wird und in welchen Grenzen.</param>
    public static SKRect FuerSticker(float quellBreite, float quellHoehe, SKPoint mitte, WbPage seite)
    {
        // Ein Bild ohne Maße gäbe eine Division durch null und danach ein Element, das
        // niemand mehr anfassen kann, weil es keine Fläche hat.
        float qb = Math.Max(1f, quellBreite);
        float qh = Math.Max(1f, quellHoehe);

        float faktor = Math.Min(1f, StickerKante / Math.Max(qb, qh));
        float breite = qb * faktor;
        float hoehe = qh * faktor;

        float x = mitte.X - breite / 2f;
        float y = mitte.Y - hoehe / 2f;

        if (!seite.IsInfinite)
        {
            // Math.Max(0, …) fängt den Fall, dass das Bild größer ist als die Seite: dann
            // sitzt es in der Ecke und ragt hinaus, statt dass die Klammer die Grenzen
            // vertauscht und Clamp wirft.
            x = Math.Clamp(x, 0, Math.Max(0, seite.Width - breite));
            y = Math.Clamp(y, 0, Math.Max(0, seite.Height - hoehe));
        }

        return SKRect.Create(x, y, breite, hoehe);
    }
}
