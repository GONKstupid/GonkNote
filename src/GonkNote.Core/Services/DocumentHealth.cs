using GonkNote.Core.Models;
using GonkNote.Core.Text;

namespace GonkNote.Core.Services;

/// <summary>Auskunft über Schäden, die einem Dokument erst beim Export auffallen würden.</summary>
public static class DocumentHealth
{
    /// <summary>
    /// Zählt Bilder einer Tafel, zu denen keine Daten mehr auffindbar sind – die erscheinen
    /// im Export als graue Platzhalter bzw. leere Seiten. Der Nutzer soll das erfahren,
    /// statt sich über ein kaputtes PDF zu wundern.
    /// <para>
    /// Das Cover bleibt außen vor: <c>CoverStyle</c> merkt sich nicht, ob es je ein
    /// Bild-Cover war (<c>ImageId</c> ist immer belegt). Ein fehlendes Cover-Bild ließe
    /// sich von einem gewollten Farbverlauf nicht unterscheiden — hier lieber nichts melden
    /// als jedes Notizbuch fälschlich anmeckern.
    /// </para>
    /// </summary>
    public static int MissingImages(WhiteboardDoc doc)
    {
        int missing = 0;

        foreach (var page in doc.Pages)
        {
            if (page.HasBackgroundImage &&
                ImageCache.Bytes(page.BackgroundImageId, page.BackgroundImage) is not { Length: > 0 })
                missing++;

            foreach (var image in page.Elements.OfType<ImageElement>())
                if (ImageCache.Bytes(image.Id, image.Data) is not { Length: > 0 })
                    missing++;
        }
        return missing;
    }

    /// <summary>
    /// Dasselbe für ein Textdokument: Bilder, deren Blob nicht (mehr) zu lesen ist.
    ///
    /// <para>
    /// <b>Sie steht seit dem Umverdrahten des PDF-Wegs hier</b> (§4.27). Vorher zählte der
    /// WPF-Kopf beim Rastern mit (<c>DocumentImages.LastExportMissingOriginals</c>) — das ging
    /// nur, solange der Export durch ein <c>FlowDocument</c> lief, und war ein Zähler, der als
    /// Nebenwirkung eines <c>using</c> entstand. Gefragt wird jetzt das Modell, und die Antwort
    /// ist für jeden Kopf dieselbe.
    /// </para>
    /// <para>
    /// <b>Das Wasserzeichen zählt mit.</b> Es ist ein Bild wie jedes andere, und es fehlt am
    /// auffälligsten: eine Seite, die es tragen sollte, sieht ohne es einfach leer aus.
    /// </para>
    /// <para>
    /// <b>Ein Diagramm zählt nicht mit</b>, auch wenn es ein <c>TdGraphic</c> ist: seine Zahlen
    /// stehen im Dokument, nicht im Blob-Speicher — es kann gar nicht fehlen.
    /// </para>
    /// </summary>
    public static int MissingImages(TdDocument doc, ITdImages? images)
    {
        int missing = 0;

        foreach (var section in doc.Sections)
            if (section.Page.Watermark is { } watermark && !Vorhanden(watermark))
                missing++;

        foreach (var paragraph in doc.Paragraphs())
            foreach (var image in paragraph.Inlines.OfType<TdImage>())
                if (!Vorhanden(image))
                    missing++;

        return missing;

        // Ohne Bildquelle fehlt nicht *ein* Bild, sondern es gibt gar keine – das ist die
        // Entscheidung des Aufrufers und keine Beschädigung des Dokuments.
        bool Vorhanden(TdImage image) =>
            images is null || images.Lesen(image.BlobId) is { Length: > 0 };
    }
}
