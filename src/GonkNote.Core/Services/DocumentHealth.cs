using GonkNote.Core.Models;

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
}
