using System.Collections.Concurrent;
using System.Reflection;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using LiteDB;

namespace GonkNote.Legacy;

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
/// abgebildet.
/// <para>
/// <b>Seit Phase 2, Schritt 3 wird hier nur noch gelesen.</b> Geschrieben wird in SQLite,
/// und dort trägt jedes Element den heutigen Namen — dieselbe Zeichenkette, die
/// <see cref="WbElement"/> als <c>[JsonDerivedType]</c> führt. Ein Bestandsdokument zieht
/// also bei der Migration ein für alle Mal um; diese Tabelle muss deshalb nie wachsen.
/// </para>
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
/// Liest eine LiteDB-Altdatenbank — den Stand bis einschließlich Version 0.2.0.
/// <para>
/// <b>Die Datei wird ausschließlich gelesen.</b> Die Verbindung steht auf
/// <c>ReadOnly</c>: LiteDB legt dann weder Log-Abschnitt noch Prüfpunkt an, die Bytes auf
/// der Platte bleiben Byte für Byte dieselben. Das ist keine Höflichkeit, sondern die
/// Zusage aus dem Arbeitsplan — die Migration ist rein additiv, und wenn sie scheitert,
/// muss der alte Stand unversehrt danebenliegen.
/// </para>
/// </summary>
public sealed class LiteDbReader : ILegacyDatabaseReader
{
    public LegacyContent Lies(string path)
    {
        // EmptyStringToNull = false wie im alten DatabaseService: LiteDB macht aus "" sonst
        // BSON-Null, und beim Lesen käme null in einem string-Feld an. Genau das hat in V1
        // das Öffnen von Textdokumenten zum Absturz gebracht (HANDOFF §7).
        var mapper = new BsonMapper(null, ModelTypeBinder.Instance) { EmptyStringToNull = false };

        using var db = new LiteDatabase(
            new ConnectionString
            {
                Filename = path,
                Connection = ConnectionType.Direct,
                ReadOnly = true,
            },
            mapper);

        var inhalt = new LegacyContent();

        inhalt.Items.AddRange(db.GetCollection<NoteItem>("items").FindAll());
        inhalt.Boards.AddRange(db.GetCollection<WhiteboardDoc>("boards").FindAll());
        inhalt.Texts.AddRange(db.GetCollection<TextDoc>("texts").FindAll());

        foreach (var doc in db.GetCollection<BsonDocument>("settings").FindAll())
        {
            string? key = doc["_id"].AsString;
            string? value = doc["value"].AsString;
            if (key != null && value != null)
                inhalt.Settings.Add(new KeyValuePair<string, string>(key, value));
        }

        return inhalt;
    }
}
