using SkiaSharp;

namespace GonkNote.Services;

/// <summary>
/// Cache dekodierter Bilder fürs Canvas-Rendering. Begrenzt auf wenige Einträge
/// (LRU), damit das RAM-Ziel der App trotz vieler Bilder hält.
/// </summary>
public static class ImageCache
{
    private const int MaxEntries = 24;

    private static readonly Dictionary<Guid, SKImage> _cache = new();
    private static readonly LinkedList<Guid> _lru = new();

    public static SKImage? Get(Guid id, byte[] data)
    {
        if (_cache.TryGetValue(id, out var img))
        {
            _lru.Remove(id);
            _lru.AddFirst(id);
            return img;
        }

        using var bmp = SKBitmap.Decode(data);
        if (bmp == null) return null;
        img = SKImage.FromBitmap(bmp);

        _cache[id] = img;
        _lru.AddFirst(id);
        if (_lru.Count > MaxEntries)
        {
            var evict = _lru.Last!.Value;
            _lru.RemoveLast();
            if (_cache.Remove(evict, out var old)) old.Dispose();
        }
        return img;
    }
}
