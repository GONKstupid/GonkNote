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
    /// <summary>
    /// Der Speicher der geöffneten Datenbank. Einmal beim Start gesetzt; alles, was Bilder
    /// liest oder schreibt, greift hierauf zu – damit gibt es genau einen Weg zu den Daten
    /// und nicht mehrere, die auseinanderlaufen können.
    /// </summary>
    public static BlobStore? Current { get; internal set; }

    private readonly string _root;
    private readonly string _bin;

    /// <summary>Legt den Speicher neben die Datenbank: <c>gonknote.db</c> → <c>gonknote.blobs\</c>.</summary>
    public BlobStore(string databasePath)
    {
        string dir = Path.GetDirectoryName(Path.GetFullPath(databasePath))!;
        string stem = Path.Combine(dir, Path.GetFileNameWithoutExtension(databasePath));
        _root = stem + ".blobs";

        // Bewusst **neben** dem Speicher, nicht darin: All() sucht rekursiv nach *.bin und
        // würde einen Unterordner sonst gleich wieder als regulären Blob einsammeln.
        _bin = stem + ".papierkorb";
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

    public bool Exists(Guid id) => File.Exists(PathOf(id)) || TryRestore(id);

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
        if (!File.Exists(path) && !TryRestore(id)) return null;
        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// Öffnet ein Blob als Strom. Für große Daten der richtige Weg – so muss ein 1-GB-PDF
    /// nie am Stück in den Speicher.
    /// </summary>
    public Stream? OpenRead(Guid id)
    {
        string path = PathOf(id);
        if (!File.Exists(path) && !TryRestore(id)) return null;
        return File.OpenRead(path);
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
        var all = All().ToList();

        // Notbremse. Die Referenzliste kommt aus der Datenbank; kann die gerade nicht
        // vollständig gelesen werden, sieht plötzlich *jedes* Bild wie Müll aus. Genau so
        // geht eine Bildersammlung in einem Rutsch verloren. Ein Speicher, in dem angeblich
        // nichts mehr gebraucht wird, ist deshalb kein Aufräumfall, sondern ein Verdachtsfall.
        if (used.Count == 0 && all.Count > 0) return 0;

        var cutoff = DateTime.UtcNow - minimumAge;
        long freed = 0;

        foreach (var id in all)
        {
            if (used.Contains(id)) continue;
            var file = new FileInfo(PathOf(id));
            if (!file.Exists || file.LastWriteTimeUtc > cutoff) continue;

            freed += file.Length;
            Recycle(id);
        }
        return freed;
    }

    /// <summary>
    /// Schiebt ein Blob in den Papierkorb statt es zu löschen. Der Aufräumlauf entscheidet
    /// über <em>Nutzerdaten</em> – Fotos, eingescannte Seiten, Cover –, und er entscheidet
    /// allein anhand einer Referenzrechnung. Ist die einmal falsch, war ein <c>File.Delete</c>
    /// endgültig: kein Windows-Papierkorb, keine Vorgängerversion, nichts.
    /// </summary>
    private void Recycle(Guid id)
    {
        string from = PathOf(id);
        string to = Path.Combine(_bin, id.ToString("N") + ".bin");
        try
        {
            Directory.CreateDirectory(_bin);
            File.Move(from, to, overwrite: true);
        }
        catch (IOException) { /* gesperrt: bleibt liegen, der nächste Lauf holt es */ }
    }

    /// <summary>
    /// Holt ein versehentlich aussortiertes Blob zurück. Wird beim Lesen versucht, bevor
    /// „fehlt" gemeldet wird – so heilt sich ein Fehlgriff des Aufräumlaufs von selbst,
    /// sobald das Bild wieder gebraucht wird.
    /// </summary>
    private bool TryRestore(Guid id)
    {
        string from = Path.Combine(_bin, id.ToString("N") + ".bin");
        if (!File.Exists(from)) return false;

        string to = PathOf(id);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Move(from, to, overwrite: true);
            return true;
        }
        catch (IOException) { return false; }
    }

    /// <summary>
    /// Leert den Papierkorb von allem, was länger als <paramref name="keepFor"/> darin liegt,
    /// und liefert den freigegebenen Platz. Bis dahin ist jeder Fehlgriff umkehrbar.
    /// </summary>
    public long PurgeRecycled(TimeSpan keepFor)
    {
        if (!Directory.Exists(_bin)) return 0;

        var cutoff = DateTime.UtcNow - keepFor;
        long freed = 0;

        foreach (string file in Directory.EnumerateFiles(_bin, "*.bin"))
        {
            var info = new FileInfo(file);
            if (!info.Exists || info.LastWriteTimeUtc > cutoff) continue;

            long size = info.Length;
            try { info.Delete(); freed += size; }
            catch (IOException) { /* nächster Lauf */ }
        }
        return freed;
    }
}
