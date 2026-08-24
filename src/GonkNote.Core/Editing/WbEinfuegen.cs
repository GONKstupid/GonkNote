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

    // ==================== Bilder aus Dateien, Zwischenablage, Texterkennung ====================

    /// <summary>Abstand zwischen zwei kaskadierten Bildern, und zwischen PDF-Seiten.</summary>
    public const float Abstand = 28f;

    /// <summary>Der Versatz, um den jedes weitere Bild eines Stapels verschoben wird.</summary>
    private const float Kaskade = 24f;

    /// <summary>
    /// Rechnet Lage und Größe für einen <b>Stapel eingefügter Bilder</b>: jedes wird in den
    /// erlaubten Rahmen eingepasst (nie vergrößert), um <paramref name="mitte"/> zentriert und
    /// gegenüber dem vorigen um <see cref="Kaskade"/> versetzt.
    ///
    /// <para>
    /// <b>Der Versatz ist der Punkt.</b> Ohne ihn lägen fünf gleichzeitig eingefügte Bilder
    /// exakt übereinander, und der Nutzer sähe eines — er müsste vier davon blind
    /// wegziehen, um zu merken, dass sie da sind.
    /// </para>
    /// <para>
    /// <b>Das ist bewusst nicht <see cref="FuerSticker"/>.</b> Ein Sticker kommt klein und
    /// immer gleich groß, weil man ihn als Marke danebensetzt; ein eingefügtes Bild soll so
    /// groß sein, wie der Platz hergibt. Zwei Bedürfnisse, zwei Rechnungen.
    /// </para>
    /// </summary>
    /// <param name="masse">Pixelmaße der vorbereiteten Bilder, in Einfügereihenfolge.</param>
    /// <param name="mitte">Wohin zentriert wird (Zeichenflächen-Einheiten).</param>
    /// <param name="seite">Die Seite — endlich oder unendlich.</param>
    /// <param name="sichtBreite">Sichtbare Breite in Zeichenflächen-Einheiten; zählt nur auf der unendlichen Fläche.</param>
    /// <param name="sichtHoehe">Sichtbare Höhe, ebenso.</param>
    public static List<SKRect> FuerBilder(
        IReadOnlyList<(float Breite, float Hoehe)> masse,
        SKPoint mitte, WbPage seite, float sichtBreite, float sichtHoehe)
    {
        // Auf der unendlichen Fläche gibt es kein Blatt, an dem man sich messen könnte —
        // dort begrenzt der Sichtbereich, damit ein eingefügtes Bild nicht sofort über den
        // Rand hinausläuft. Die 64 fangen den Fall, dass das Fenster noch keine Größe hat.
        float maxB, maxH;
        if (seite.IsInfinite)
        {
            maxB = Math.Max(64f, sichtBreite * 0.6f);
            maxH = Math.Max(64f, sichtHoehe * 0.6f);
        }
        else
        {
            maxB = seite.Width * 0.7f;
            maxH = seite.Height * 0.7f;
        }

        var kaesten = new List<SKRect>(masse.Count);
        for (int i = 0; i < masse.Count; i++)
        {
            float qb = Math.Max(1f, masse[i].Breite);
            float qh = Math.Max(1f, masse[i].Hoehe);

            float faktor = Math.Min(1f, Math.Min(maxB / qb, maxH / qh));
            float breite = qb * faktor, hoehe = qh * faktor;

            float x = mitte.X - breite / 2f + i * Kaskade;
            float y = mitte.Y - hoehe / 2f + i * Kaskade;
            if (!seite.IsInfinite)
            {
                x = Math.Clamp(x, 0, Math.Max(0, seite.Width - breite));
                y = Math.Clamp(y, 0, Math.Max(0, seite.Height - hoehe));
            }

            kaesten.Add(SKRect.Create(x, y, breite, hoehe));
        }
        return kaesten;
    }

    // ==================== PDF- und DOCX-Seiten ====================

    /// <summary>
    /// Anzeigemaße einer gerenderten Seite: <b>die lange Kante wird zur A4-Höhe</b>, das
    /// Seitenverhältnis bleibt.
    ///
    /// <para>
    /// <b>Warum nicht die Pixelmaße.</b> Sie hängen an der Renderauflösung — dieselbe PDF-Seite
    /// käme je nach Einstellung unterschiedlich groß auf die Fläche, und das stünde dann so in
    /// der Datei. Eine A4-Seite soll wie eine A4-Seite daliegen, egal wie fein sie gerastert
    /// wurde. Querformat wird mit erkannt: dort ist die <em>Breite</em> die lange Kante.
    /// </para>
    /// </summary>
    public static (float Breite, float Hoehe) SeitenAnzeigegroesse(float pixelBreite, float pixelHoehe)
    {
        const float langeKante = WhiteboardDoc.A4Height;
        float pb = Math.Max(1f, pixelBreite), ph = Math.Max(1f, pixelHoehe);

        return ph >= pb
            ? (langeKante * pb / ph, langeKante)
            : (langeKante, langeKante * ph / pb);
    }

    /// <summary>
    /// Legt gerenderte Seiten <b>zweispaltig</b> auf die unendliche Fläche: Seite 1 und 2
    /// nebeneinander, 3 und 4 darunter, und so fort.
    ///
    /// <para>
    /// <b>Warum zwei Spalten und nicht eine Reihe.</b> Ein zwanzigseitiges PDF in einer Reihe
    /// wäre zwanzig Blatt breit — man müsste waagerecht durch die halbe Fläche fahren, um das
    /// Ende zu sehen. Zwei Spalten lesen sich wie ein aufgeschlagenes Buch.
    /// </para>
    /// <para>
    /// <b>Die Zeilenhöhe richtet sich nach der höheren der beiden Seiten</b> — sonst liefe eine
    /// Querformat-Seite in die Zeile darunter.
    /// </para>
    /// </summary>
    /// <param name="masse">Anzeigemaße der Seiten, in Reihenfolge (siehe <see cref="SeitenAnzeigegroesse"/>).</param>
    /// <param name="obenLinks">Bezugspunkt: die Mitte der Sicht, um die das Raster gelegt wird.</param>
    public static List<SKRect> SeitenRaster(IReadOnlyList<(float Breite, float Hoehe)> masse, SKPoint obenLinks)
    {
        var kaesten = new List<SKRect>(masse.Count);
        if (masse.Count == 0) return kaesten;

        float spaltenBreite = masse.Max(m => m.Breite);
        float linkeSpalte = obenLinks.X - spaltenBreite - Abstand / 2f;
        float y = obenLinks.Y;

        for (int i = 0; i < masse.Count; i += 2)
        {
            float zeilenHoehe = masse[i].Hoehe;
            if (i + 1 < masse.Count) zeilenHoehe = Math.Max(zeilenHoehe, masse[i + 1].Hoehe);

            kaesten.Add(SKRect.Create(linkeSpalte, y, masse[i].Breite, masse[i].Hoehe));
            if (i + 1 < masse.Count)
                kaesten.Add(SKRect.Create(
                    linkeSpalte + spaltenBreite + Abstand, y,
                    masse[i + 1].Breite, masse[i + 1].Hoehe));

            y += zeilenHoehe + Abstand;
        }
        return kaesten;
    }
}
