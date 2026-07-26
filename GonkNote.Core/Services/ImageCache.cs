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

        if (data is not { Length: > 0 }) return null;

        using var bmp = SKBitmap.Decode(data);
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
}
