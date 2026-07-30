using System.IO;
using GonkNote.Core.Models;
using GonkNote.Core.Services;

namespace GonkNote.Core.Tests;

/// <summary>
/// Der Blob-Speicher und der Aufräumlauf — der Bereich, in dem ein Fehler **Nutzerdaten**
/// kostet (Fotos, eingescannte Seiten, Cover) und nicht nur Code.
/// <para>
/// Zwei Dinge werden hier festgenagelt, weil beide schon einmal Bilder gekostet haben:
/// erstens dass <see cref="DatabaseService.SaveBoard"/> die Bytes erst aus dem Datensatz
/// entfernt, **nachdem** das Blob nachweislich liegt; zweitens die Notbremse im
/// Aufräumlauf — eine leere Referenzliste ist kein Aufräumfall, sondern ein Verdachtsfall.
/// </para>
/// </summary>
public sealed class BlobSpeicherTests
{
    [Fact]
    public void Blob_Ordner_liegt_neben_der_Datenbank()
    {
        using var ws = new TempWorkspace("blob-layout");

        var store = new BlobStore(ws.DatabasePath);
        var id = store.Put([1, 2, 3, 4]);

        // Der Name leitet sich vom Datenbanknamen ab — davon hängt ab, was gesichert werden
        // muss: Datenbankdatei **plus** dieser Ordner (HANDOFF §7).
        string root = Path.Combine(ws.Root, "wegwerf.blobs");
        Assert.True(Directory.Exists(root));

        // Zwei Zeichen Unterordner, damit kein Verzeichnis mit zehntausenden Einträgen entsteht.
        string erwartet = Path.Combine(root, id.ToString("N")[..2], id.ToString("N") + ".bin");
        Assert.True(File.Exists(erwartet), $"Blob nicht unter {erwartet}");

        Assert.Equal(4, store.SizeOf(id));
        Assert.Equal([1, 2, 3, 4], store.Read(id));
        Assert.True(store.Exists(id));
        Assert.Equal([id], store.All());
    }

    [Fact]
    public void Fehlendes_Blob_ist_null_und_kein_Absturz()
    {
        using var ws = new TempWorkspace("blob-missing");
        var store = new BlobStore(ws.DatabasePath);

        Assert.Null(store.Read(Guid.NewGuid()));
        Assert.Null(store.OpenRead(Guid.NewGuid()));
        Assert.False(store.Exists(Guid.NewGuid()));
        Assert.Equal(0, store.SizeOf(Guid.NewGuid()));
    }

    /// <summary>
    /// Speichern lagert Bilder aus dem Datensatz aus (sonst reißt ein Notizbuch mit
    /// PDF-Seiten die 16-MB-Grenze von LiteDB) — die Bytes im Dokument sind danach leer,
    /// die Daten müssen aber über den Blob-Speicher weiterhin ankommen.
    /// </summary>
    [Fact]
    public void Speichern_verlagert_Bilder_in_den_Blob_Speicher()
    {
        using var ws = new TempWorkspace("blob-offload");

        var doc = Beispieldokument.Notizbuch();
        var coverBytes = doc.Cover!.Image!;
        var seite = doc.Pages[1];
        var seiteBytes = seite.BackgroundImage!;
        var bild = seite.Elements.OfType<ImageElement>().Single();
        var bildBytes = bild.Data;

        using var db = new DatabaseService(ws.DatabasePath);
        db.SaveBoard(doc);

        // Datensatz ist leer …
        Assert.Null(doc.Cover.Image);
        Assert.Null(seite.BackgroundImage);
        Assert.Empty(bild.Data);

        // … die Daten sind es nicht.
        Assert.Equal(coverBytes, db.Blobs.Read(doc.Cover.ImageId));
        Assert.Equal(seiteBytes, db.Blobs.Read(seite.BackgroundImageId));
        Assert.Equal(bildBytes, db.Blobs.Read(bild.Id));

        // Und die Seite weiß weiterhin, dass sie ein Hintergrundbild hat — ablesbar an der
        // Id, nicht an den Bytes.
        Assert.True(seite.HasBackgroundImage);
    }

    /// <summary>
    /// Guid.Empty wäre für alle Bilder derselbe Schlüssel — sie würden einander
    /// überschreiben. In dem Fall bleiben die Bytes lieber im Datensatz.
    /// </summary>
    [Fact]
    public void Bild_ohne_Id_bleibt_im_Datensatz()
    {
        using var ws = new TempWorkspace("blob-emptyguid");

        var bild = new ImageElement { Id = Guid.Empty, Width = 10, Height = 10, Data = [7, 7, 7] };
        var doc = new WhiteboardDoc
        {
            Id = Beispieldokument.DokumentId,
            Pages = { new WbPage { Elements = { bild } } },
        };

        using var db = new DatabaseService(ws.DatabasePath);
        db.SaveBoard(doc);

        Assert.Equal([7, 7, 7], bild.Data);
        Assert.Empty(db.Blobs.All());
    }

    [Fact]
    public void Aufraeumen_behaelt_benutzte_und_sortiert_verwaiste_aus()
    {
        using var ws = new TempWorkspace("blob-orphans");

        using var db = new DatabaseService(ws.DatabasePath);

        var doc = Beispieldokument.Notizbuch();
        db.SaveBoard(doc);
        var benutzt = doc.Cover!.ImageId;

        var verwaist = db.Blobs.Put([9, 9, 9, 9, 9, 9]);
        var frisch = db.Blobs.Put([8, 8, 8]);

        // Der Aufräumlauf lässt frische Blobs grundsätzlich liegen: sie könnten zu einem
        // gerade laufenden Import gehören, der noch in keinem gespeicherten Dokument steht.
        // Nur das eine Blob wird künstlich gealtert.
        Altern(ws, verwaist, TimeSpan.FromHours(2));

        long frei = db.RemoveOrphanBlobs();

        Assert.Equal(6, frei);
        Assert.True(db.Blobs.Exists(benutzt), "Ein benutztes Bild wurde weggeräumt.");
        Assert.True(db.Blobs.Exists(frisch), "Ein frisches Blob wurde weggeräumt.");
        Assert.DoesNotContain(verwaist, db.Blobs.All());
    }

    /// <summary>
    /// Aussortiert heißt nicht gelöscht: das Blob liegt im Papierkorb und kommt beim
    /// nächsten Lesen von selbst zurück. So heilt sich ein Fehlgriff des Aufräumlaufs,
    /// sobald das Bild wieder gebraucht wird.
    /// </summary>
    [Fact]
    public void Aussortiertes_Blob_kommt_beim_Lesen_zurueck()
    {
        using var ws = new TempWorkspace("blob-restore");
        var store = new BlobStore(ws.DatabasePath);

        var behalten = store.Put([1]);
        var verwaist = store.Put([2, 2, 2]);
        Altern(ws, verwaist, TimeSpan.FromHours(2));

        Assert.Equal(3, store.RemoveOrphans([behalten], TimeSpan.FromHours(1)));
        Assert.DoesNotContain(verwaist, store.All());

        // Lesen holt es aus dem Papierkorb zurück.
        Assert.Equal([2, 2, 2], store.Read(verwaist));
        Assert.Contains(verwaist, store.All());
    }

    /// <summary>
    /// Die Notbremse. Die Referenzliste kommt aus der Datenbank; kann die gerade nicht
    /// gelesen werden, sieht plötzlich jedes Bild wie Müll aus — genau so geht eine
    /// Bildersammlung in einem Rutsch verloren.
    /// </summary>
    [Fact]
    public void Leere_Referenzliste_raeumt_nichts_weg()
    {
        using var ws = new TempWorkspace("blob-notbremse");
        var store = new BlobStore(ws.DatabasePath);

        var a = store.Put([1, 1, 1]);
        var b = store.Put([2, 2, 2]);
        Altern(ws, a, TimeSpan.FromDays(1));
        Altern(ws, b, TimeSpan.FromDays(1));

        Assert.Equal(0, store.RemoveOrphans([], TimeSpan.FromHours(1)));
        Assert.Equal(2, store.All().Count());
    }

    [Fact]
    public void Papierkorb_wird_erst_nach_der_Aufbewahrungszeit_geleert()
    {
        using var ws = new TempWorkspace("blob-purge");
        var store = new BlobStore(ws.DatabasePath);

        var behalten = store.Put([1]);
        var verwaist = store.Put([2, 2, 2, 2]);
        Altern(ws, verwaist, TimeSpan.FromHours(2));
        store.RemoveOrphans([behalten], TimeSpan.FromHours(1));

        // Noch innerhalb der Aufbewahrungszeit: unverändert umkehrbar.
        Assert.Equal(0, store.PurgeRecycled(TimeSpan.FromDays(30)));
        Assert.Equal([2, 2, 2, 2], store.Read(verwaist));

        // Erneut aussortieren und über die Aufbewahrungszeit hinaus altern lassen.
        Altern(ws, verwaist, TimeSpan.FromHours(2));
        store.RemoveOrphans([behalten], TimeSpan.FromHours(1));
        AlternImPapierkorb(ws, verwaist, TimeSpan.FromDays(40));

        Assert.Equal(4, store.PurgeRecycled(TimeSpan.FromDays(30)));
        Assert.Null(store.Read(verwaist));
    }

    // ---- Hilfsmittel ---------------------------------------------------------------------

    /// <summary>
    /// Datiert ein Blob zurück. Der Aufräumlauf entscheidet über das Alter der **Datei**,
    /// nicht über eine Uhr im Code — also muss der Test die Datei anfassen. Die Alternative
    /// wäre, den Test eine Stunde warten zu lassen.
    /// </summary>
    private static void Altern(TempWorkspace ws, Guid id, TimeSpan alter) =>
        Zurueckdatieren(Path.Combine(ws.Root, "wegwerf.blobs", id.ToString("N")[..2], id.ToString("N") + ".bin"), alter);

    private static void AlternImPapierkorb(TempWorkspace ws, Guid id, TimeSpan alter) =>
        Zurueckdatieren(Path.Combine(ws.Root, "wegwerf.papierkorb", id.ToString("N") + ".bin"), alter);

    private static void Zurueckdatieren(string path, TimeSpan alter)
    {
        Assert.True(File.Exists(path), $"Erwartete Datei fehlt: {path}");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow - alter);
    }
}
