using SkiaSharp;

namespace GonkNote.Core.Services;

/// <summary>
/// Cache dekodierter Bilder fürs Canvas-Rendering. Begrenzt über ein Byte-Budget
/// (LRU-Verdrängung), damit das RAM-Ziel der App auch bei vielen hochauflösenden
/// Bildern (z. B. importierten PDF-Seiten) hält.
/// </summary>
public static class ImageCache
{
    private const long MaxBytes = 96L * 1024 * 1024;

    /// <summary>
    /// Wo die Originale liegen. Whiteboard-Bilder und Seitenhintergründe stehen seit dem
    /// Umbau nicht mehr im Dokument, sondern als eigene Datei daneben – der Datensatz würde
    /// sonst die 16-MB-Grenze von LiteDB reißen. Bestandsdokumente bringen ihre Bytes noch
    /// mit; die haben Vorrang und wandern beim nächsten Speichern in den Blob-Speicher.
    /// </summary>
    public static BlobStore? Source { get; set; }

    /// <summary>Bilddaten zu einer Id: aus dem Dokument, sonst aus dem Blob-Speicher.</summary>
    public static byte[]? Bytes(Guid id, byte[]? inline) =>
        inline is { Length: > 0 } ? inline : Source?.Read(id);

    private static readonly Dictionary<Guid, (SKImage Img, long Bytes)> _cache = new();
    private static readonly LinkedList<Guid> _lru = new();
    private static long _bytes;

    /// <summary>
    /// Dekodiertes Bild, oder null wenn die Daten fehlen oder unbrauchbar sind. Null ist ein
    /// gültiges Ergebnis: der Aufrufer zeichnet dann einen Platzhalter. Ohne diese Prüfung warf
    /// <c>SKBitmap.Decode(null)</c> mitten im Zeichnen – ein einziges kaputtes Bild ließ damit
    /// die ganze Seite leer.
    /// </summary>
    public static SKImage? Get(Guid id, byte[]? data)
    {
        if (_cache.TryGetValue(id, out var entry))
        {
            _lru.Remove(id);
            _lru.AddFirst(id);
            return entry.Img;
        }

        byte[]? encoded = Bytes(id, data);
        if (encoded is not { Length: > 0 }) return null;

        using var bmp = SKBitmap.Decode(encoded);
        if (bmp == null) return null;
        var img = SKImage.FromBitmap(bmp);
        long bytes = (long)bmp.Width * bmp.Height * 4;

        _cache[id] = (img, bytes);
        _lru.AddFirst(id);
        _bytes += bytes;

        // Älteste Einträge verdrängen; der gerade geholte bleibt immer erhalten
        while (_bytes > MaxBytes && _lru.Count > 1)
        {
            var evict = _lru.Last!.Value;
            _lru.RemoveLast();
            if (_cache.Remove(evict, out var old))
            {
                _bytes -= old.Bytes;
                old.Img.Dispose();
            }
        }
        return img;
    }

    /// <summary>
    /// Vergisst ein Bild. Wird beim Schließen eines Dokuments für dessen Bilder aufgerufen –
    /// sonst blieben bis zu 96 MB dekodierter Seiten liegen, obwohl niemand sie mehr ansieht.
    /// </summary>
    public static void Forget(Guid id)
    {
        if (!_cache.Remove(id, out var entry)) return;
        _lru.Remove(id);
        _bytes -= entry.Bytes;
        entry.Img.Dispose();
    }

    public static void Forget(IEnumerable<Guid> ids)
    {
        foreach (var id in ids) Forget(id);
    }
}
