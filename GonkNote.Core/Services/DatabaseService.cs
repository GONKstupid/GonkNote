using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using GonkNote.Core.Models;
using LiteDB;

namespace GonkNote.Core.Services;

/// <summary>
/// Löst die Typnamen polymorpher Felder auf – auch die aus älteren Programmständen.
/// <para>
/// LiteDB schreibt für jedes Whiteboard-Element ein Feld <c>_type</c> im Format
/// "Namensraum.Typ, Assembly". Beides hat sich beim Aufräumen geändert: erst zog die
/// Assembly um ("GonkNote" → "GonkNote.Core"), dann der Namensraum
/// ("GonkNote.Models" → "GonkNote.Core.Models"). Ohne Übersetzung ließe sich **kein**
/// bestehendes Whiteboard mehr öffnen – die App stürzte dabei ab.
/// </para>
/// Deshalb wird die Assembly beim Lesen ignoriert und der alte Namensraum auf den neuen
/// abgebildet. Neu geschriebene Dokumente tragen immer den aktuellen Namen.
/// </summary>
internal sealed class ModelTypeBinder : ITypeNameBinder
{
    public static readonly ModelTypeBinder Instance = new();

    private static readonly Assembly Models = typeof(WbElement).Assembly;

    /// <summary>Namensräume aus früheren Ständen → heutiger Namensraum.</summary>
    private static readonly (string Old, string New)[] Renamed =
    {
        ("GonkNote.Models.", "GonkNote.Core.Models."),
    };

    private readonly ConcurrentDictionary<string, Type?> _cache = new();

    public string GetName(Type type) => DefaultTypeNameBinder.Instance.GetName(type);

    public Type? GetType(string name) => _cache.GetOrAdd(name, Resolve);

    private static Type? Resolve(string name)
    {
        if (Type.GetType(name) is { } exact) return exact;

        string plain = WithoutAssembly(name);
        if (Models.GetType(plain) is { } byName) return byName;

        foreach (var (old, current) in Renamed)
            if (plain.StartsWith(old, StringComparison.Ordinal) &&
                Models.GetType(current + plain[old.Length..]) is { } moved)
                return moved;

        return null;   // LiteDB meldet den unbekannten Typ selbst
    }

    private static string WithoutAssembly(string name)
    {
        int comma = name.IndexOf(',');
        return comma < 0 ? name : name[..comma].TrimEnd();
    }
}

/// <summary>
/// Lokale Persistenz über LiteDB. Eine Datei unter %APPDATA%\GonkNote, keine Adminrechte nötig.
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private readonly LiteDatabase _db;

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GonkNote", "gonknote.db");

    public DatabaseService(string? path = null)
    {
        var mapper = new BsonMapper(null, ModelTypeBinder.Instance)
        {
            // LiteDB macht aus "" standardmäßig BSON-Null (EmptyStringToNull) – das hat
            // beim Laden null-Strings erzeugt (Crash beim Öffnen von Textdokumenten).
            EmptyStringToNull = false,
        };

        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _db = new LiteDatabase(new ConnectionString { Filename = path, Connection = ConnectionType.Shared }, mapper);
        Blobs = new BlobStore(path);

        Items.EnsureIndex(x => x.ParentId);
    }

    /// <summary>
    /// Große Daten (Bilder, importierte PDFs) liegen daneben im Blob-Ordner, nicht im
    /// Datensatz – siehe <see cref="BlobStore"/>.
    /// </summary>
    public BlobStore Blobs { get; }

    private ILiteCollection<NoteItem> Items => _db.GetCollection<NoteItem>("items");
    private ILiteCollection<WhiteboardDoc> Boards => _db.GetCollection<WhiteboardDoc>("boards");
    private ILiteCollection<TextDoc> Texts => _db.GetCollection<TextDoc>("texts");
    private ILiteCollection<BsonDocument> Settings => _db.GetCollection<BsonDocument>("settings");

    // ---- Ordnerbaum ----

    public List<NoteItem> GetAllItems() => Items.FindAll().ToList();

    public void UpsertItem(NoteItem item)
    {
        item.ModifiedUtc = DateTime.UtcNow;
        Items.Upsert(item);
    }

    /// <summary>Löscht einen Eintrag samt aller Kinder und Dokumentinhalte.</summary>
    public void DeleteItemRecursive(Guid id)
    {
        foreach (var child in Items.Find(x => x.ParentId == id).ToList())
            DeleteItemRecursive(child.Id);

        Items.Delete(id);
        Boards.Delete(id);
        Texts.Delete(id);
    }

    // ---- Dokumentinhalte ----

    public WhiteboardDoc GetBoard(NoteItem item)
    {
        var doc = Boards.FindById(item.Id);
        if (doc != null) return doc;
        return item.Kind == ItemKind.Notebook
            ? WhiteboardDoc.NewNotebook(item.Id)
            : WhiteboardDoc.NewWhiteboard(item.Id);
    }

    public void SaveBoard(WhiteboardDoc doc) => Boards.Upsert(doc);

    /// <summary>
    /// Lädt nur die Cover-Gestaltung eines Notizbuchs (ohne die Seiten) für die
    /// Galerie-Vorschau – projiziert das Cover-Feld, damit nicht das ganze Board mit
    /// allen (ggf. bildlastigen) Seiten deserialisiert werden muss.
    /// </summary>
    public CoverStyle? GetCover(Guid id)
    {
        try { return Boards.Query().Where(x => x.Id == id).Select(x => x.Cover).FirstOrDefault(); }
        catch { return Boards.FindById(id)?.Cover; }
    }

    public TextDoc GetText(Guid id) => Texts.FindById(id) ?? new TextDoc { Id = id };

    public void SaveText(TextDoc doc) => Texts.Upsert(doc);

    /// <summary>
    /// Räumt Bilder auf, auf die kein Dokument mehr zeigt (gelöschte Dokumente, verworfene
    /// Importe). Liefert den freigegebenen Platz in Bytes. Läuft absichtlich nur über Blobs,
    /// die schon eine Weile liegen – ein gerade laufender Import ist sonst in Gefahr.
    /// </summary>
    public long RemoveOrphanBlobs()
    {
        var used = Texts.FindAll().SelectMany(t => t.Images);
        return Blobs.RemoveOrphans(used, TimeSpan.FromHours(1));
    }

    // ---- Einstellungen ----

    public string? GetSetting(string key) =>
        Settings.FindById(key)?["value"].AsString;

    public void SetSetting(string key, string value) =>
        Settings.Upsert(new BsonDocument { ["_id"] = key, ["value"] = value });

    public void Dispose() => _db.Dispose();
}
