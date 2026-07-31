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

    /// <summary>
    /// Wo die Datenbank liegt, wenn kein <c>--db</c> mitgegeben wurde. Der Ordner kommt
    /// von <see cref="Platform.AppPaths"/>, damit Linux- und iOS-Kopf ihn setzen können,
    /// ohne dass hier eine Plattform hartkodiert steht.
    /// </summary>
    public static string DefaultPath => Platform.AppPaths.DatabaseFile;

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
        BlobStore.Current = Blobs;

        // Ohne diese Zeile findet der Bild-Cache die ausgelagerten Bilder nicht: SaveBoard
        // leert die Byte-Felder im Dokument, und ImageCache.Bytes fällt dann auf Source
        // zurück. War Source null, lieferte jedes gespeicherte Bild null – graue Platzhalter
        // in Anzeige und Export, Cover zurück auf den Farbverlauf, während die Dateien
        // unversehrt daneben lagen.
        ImageCache.Source = Blobs;

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

    /// <summary>
    /// Speichert ein Whiteboard/Notizbuch. Bilder wandern dabei **aus** dem Datensatz in den
    /// Blob-Speicher: ein Notizbuch mit importierten PDF-Seiten sprengte sonst die 16-MB-Grenze
    /// von LiteDB und ließ sich überhaupt nicht mehr speichern.
    /// <para>
    /// Das leert die Byte-Felder am übergebenen Dokument – gewollt: ab dann liefert der
    /// <see cref="ImageCache"/> die Daten aus dem Blob-Speicher. Bestandsdokumente ziehen so
    /// beim ersten Speichern von selbst um.
    /// </para>
    /// </summary>
    public void SaveBoard(WhiteboardDoc doc)
    {
        MoveImagesToBlobs(doc);
        Boards.Upsert(doc);
    }

    private void MoveImagesToBlobs(WhiteboardDoc doc)
    {
        Move(doc.Cover?.ImageId, doc.Cover?.Image, () => doc.Cover!.Image = null);

        foreach (var page in doc.Pages)
        {
            Move(page.BackgroundImageId, page.BackgroundImage, () => page.BackgroundImage = null);
            foreach (var image in page.Elements.OfType<ImageElement>())
                Move(image.Id, image.Data, () => image.Data = Array.Empty<byte>());
        }

        // Geleert wird erst, wenn das Blob nachweislich liegt. Sonst wäre ein fehlgeschlagener
        // Schreibvorgang gleichbedeutend mit Datenverlust: die Bytes im Datensatz sind nach
        // dem Leeren die einzige Kopie gewesen.
        void Move(Guid? id, byte[]? data, Action clear)
        {
            if (id is not { } key || data is not { Length: > 0 }) return;

            // Guid.Empty wäre für alle Bilder derselbe Schlüssel – sie würden einander
            // überschreiben. Lieber die Bytes im Datensatz lassen als sie zusammenwerfen.
            if (key == Guid.Empty) return;

            Blobs.Put(key, data);
            if (Blobs.SizeOf(key) == data.Length) clear();
        }
    }

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
        // Erst vollständig einlesen, dann aufräumen. Vorher war das eine verzögerte Abfrage,
        // die *während* des Aufräumens Stück für Stück aus der Datenbank nachlas – auf einem
        // Hintergrund-Thread, parallel zur laufenden Bearbeitung. Was dabei nicht rechtzeitig
        // ankam, galt als „wird nicht mehr gebraucht".
        var used = new HashSet<Guid>();
        foreach (var text in Texts.FindAll())
            foreach (var id in text.Images) used.Add(id);
        foreach (var board in Boards.FindAll())
            foreach (var id in UsedBlobs(board)) used.Add(id);

        used.Remove(Guid.Empty);

        long moved = Blobs.RemoveOrphans(used, TimeSpan.FromHours(1));
        Blobs.PurgeRecycled(TimeSpan.FromDays(30));
        return moved;
    }

    private static IEnumerable<Guid> UsedBlobs(WhiteboardDoc doc)
    {
        if (doc.Cover != null) yield return doc.Cover.ImageId;
        foreach (var page in doc.Pages)
        {
            yield return page.BackgroundImageId;
            foreach (var image in page.Elements.OfType<ImageElement>())
                yield return image.Id;
        }
    }

    // ---- Einstellungen ----

    public string? GetSetting(string key) =>
        Settings.FindById(key)?["value"].AsString;

    public void SetSetting(string key, string value) =>
        Settings.Upsert(new BsonDocument { ["_id"] = key, ["value"] = value });

    public void Dispose() => _db.Dispose();
}
