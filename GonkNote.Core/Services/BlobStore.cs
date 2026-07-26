using System.IO;

namespace GonkNote.Core.Services;

/// <summary>
/// Ablage für große Daten – Bilder, importierte PDFs – **neben** der Datenbank statt darin.
/// <para>
/// Grund: LiteDB speichert einen Datensatz nur bis rund 16 MB, und ein Whiteboard oder
/// Textdokument war bisher **ein** Datensatz samt aller Bildbytes. Drei Fotos in einem
/// Word-Import haben gereicht, um die Grenze zu reißen – das Dokument ließ sich danach nicht
/// mehr speichern. Außerdem taugt eine Datenbankdatei schlecht als Gigabyte-Speicher:
/// gelöschter Platz kommt erst nach einem Rebuild zurück, und ein Defekt träfe alles.
/// </para>
/// Hier liegt je Blob eine Datei. Gelöschter Platz ist sofort frei, Lesen geht als Strom
/// (nichts muss am Stück in den Speicher), und ein beschädigtes Blob kostet ein Bild statt
/// der ganzen Bibliothek. Gesichert wird ab jetzt Datenbankdatei **plus** dieser Ordner.
/// </summary>
public sealed class BlobStore
{
    private readonly string _root;

    /// <summary>Legt den Speicher neben die Datenbank: <c>gonknote.db</c> → <c>gonknote.blobs\</c>.</summary>
    public BlobStore(string databasePath)
    {
        string dir = Path.GetDirectoryName(Path.GetFullPath(databasePath))!;
        _root = Path.Combine(dir, Path.GetFileNameWithoutExtension(databasePath) + ".blobs");
        Directory.CreateDirectory(_root);
    }

    /// <summary>
    /// Pfad eines Blobs. Die ersten beiden Zeichen der Id bilden einen Unterordner, damit auch
    /// bei zehntausenden Bildern kein Verzeichnis mit zehntausenden Einträgen entsteht.
    /// </summary>
    private string PathOf(Guid id)
    {
        string name = id.ToString("N");
        return Path.Combine(_root, name[..2], name + ".bin");
    }

    public bool Exists(Guid id) => File.Exists(PathOf(id));

    /// <summary>Legt Daten ab und liefert ihre Id.</summary>
    public Guid Put(byte[] data)
    {
        var id = Guid.NewGuid();
        Write(id, data);
        return id;
    }

    /// <summary>Legt Daten unter einer vorgegebenen Id ab (für Migration und Kopien).</summary>
    public void Put(Guid id, byte[] data) => Write(id, data);

    private void Write(Guid id, byte[] data)
    {
        string path = PathOf(id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Erst daneben schreiben, dann umbenennen: ein Abbruch hinterlässt nie ein halbes Blob.
        string tmp = path + ".neu";
        File.WriteAllBytes(tmp, data);
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>Liest ein Blob vollständig; null, wenn es fehlt.</summary>
    public byte[]? Read(Guid id)
    {
        string path = PathOf(id);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    /// <summary>
    /// Öffnet ein Blob als Strom. Für große Daten der richtige Weg – so muss ein 1-GB-PDF
    /// nie am Stück in den Speicher.
    /// </summary>
    public Stream? OpenRead(Guid id)
    {
        string path = PathOf(id);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public long SizeOf(Guid id)
    {
        var info = new FileInfo(PathOf(id));
        return info.Exists ? info.Length : 0;
    }

    public void Delete(Guid id)
    {
        try { File.Delete(PathOf(id)); }
        catch (IOException) { /* gesperrt: bleibt liegen, der Aufräumlauf holt es später */ }
    }

    /// <summary>Belegter Platz in Bytes.</summary>
    public long TotalBytes() =>
        Directory.EnumerateFiles(_root, "*.bin", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);

    public IEnumerable<Guid> All()
    {
        foreach (string file in Directory.EnumerateFiles(_root, "*.bin", SearchOption.AllDirectories))
            if (Guid.TryParseExact(Path.GetFileNameWithoutExtension(file), "N", out var id))
                yield return id;
    }

    /// <summary>
    /// Löscht Blobs, auf die kein Dokument mehr zeigt, und liefert den freigegebenen Platz.
    /// Nötig, weil Datenbank und Ordner zwei getrennte Dinge sind: löscht jemand ein Notizbuch,
    /// bleiben dessen Bilder sonst liegen.
    /// <para>
    /// <paramref name="minimumAge"/> schützt einen laufenden Import: dessen Blobs sind schon
    /// geschrieben, aber noch in keinem gespeicherten Dokument eingetragen. Frische Blobs
    /// bleiben deshalb grundsätzlich liegen.
    /// </para>
    /// </summary>
    public long RemoveOrphans(IEnumerable<Guid> stillUsed, TimeSpan minimumAge)
    {
        var used = stillUsed.ToHashSet();
        var cutoff = DateTime.UtcNow - minimumAge;
        long freed = 0;

        foreach (var id in All().ToList())
        {
            if (used.Contains(id)) continue;
            var file = new FileInfo(PathOf(id));
            if (!file.Exists || file.LastWriteTimeUtc > cutoff) continue;

            freed += file.Length;
            Delete(id);
        }
        return freed;
    }
}
