using System.IO;
using GonkNote.Models;
using LiteDB;

namespace GonkNote.Services;

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
        // LiteDB macht aus "" standardmäßig BSON-Null (EmptyStringToNull) – das hat
        // beim Laden null-Strings erzeugt (Crash beim Öffnen von Textdokumenten).
        BsonMapper.Global.EmptyStringToNull = false;

        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _db = new LiteDatabase(new ConnectionString { Filename = path, Connection = ConnectionType.Shared });

        Items.EnsureIndex(x => x.ParentId);
    }

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

    public TextDoc GetText(Guid id) => Texts.FindById(id) ?? new TextDoc { Id = id };

    public void SaveText(TextDoc doc) => Texts.Upsert(doc);

    // ---- Einstellungen ----

    public string? GetSetting(string key) =>
        Settings.FindById(key)?["value"].AsString;

    public void SetSetting(string key, string value) =>
        Settings.Upsert(new BsonDocument { ["_id"] = key, ["value"] = value });

    public void Dispose() => _db.Dispose();
}
