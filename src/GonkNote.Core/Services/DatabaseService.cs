using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using Microsoft.Data.Sqlite;

namespace GonkNote.Core.Services;

/// <summary>
/// Lokale Persistenz über <b>SQLite</b> (<c>Microsoft.Data.Sqlite</c>). Eine Datei im
/// Datenordner, keine Adminrechte nötig.
/// <para>
/// <b>Was hier bis Phase 2 stand — und warum es weg ist:</b> LiteDB baute seine
/// Zuordnung zwischen Objekt und Datensatz zur Laufzeit über
/// <c>System.Reflection.Emit</c>. Unter NativeAOT gibt es keinen Just-in-time-Übersetzer,
/// also stürzt das beim ersten Zugriff ab — und NativeAOT ist für den App-Store-Weg auf
/// dem iPad Pflicht (HANDOFF §1). SQLite ist eine C-Bibliothek ohne diese Eigenschaft,
/// und die Objekte werden über einen <see cref="GonkJson">Json-Source-Generator</see>
/// gelesen und geschrieben, der seinen Code zur Übersetzungszeit erzeugt.
/// </para>
/// <b>Die öffentliche API dieser Klasse ist dabei unverändert geblieben.</b> Das war die
/// Bedingung: <c>DatenbankRoundtripTests</c>, <c>AlteTypnamenTests</c> und
/// <c>BlobSpeicherTests</c> prüfen genau sie, und wenn sie nach dem Umbau nicht
/// unverändert grün sind, sind Daten betroffen und nicht Code.
/// <para>
/// <b>Ablage:</b> je Sammlung eine Tabelle mit einer Id-Spalte und einer <c>json</c>-Spalte.
/// Kein relationales Schema für Seiten, Elemente und Punkte — die App fragt nie über
/// einzelne Felder ab (die einzige Ausnahme, das Cover für die Galerie, geht über
/// <c>json_extract</c>), und jede Modelländerung wäre sonst eine Schemamigration.
/// </para>
/// </summary>
public sealed class DatabaseService : IDisposable
{
    /// <summary>
    /// Fassung des Tabellenaufbaus. Steht in <c>meta</c> und ist die Stelle, an der eine
    /// spätere Schemaänderung ansetzt — ohne sie müsste man raten, was in der Datei liegt.
    /// </summary>
    private const int SchemaVersion = 1;

    /// <summary>
    /// Kopfkennung jeder SQLite-Datei — die sechzehn Bytes "SQLite format 3" samt
    /// abschliessender Null.
    /// </summary>
    /// <remarks>
    /// Erkannt wird daran und **nicht** an der Dateiendung: eine Endung ist eine
    /// Behauptung, die ersten sechzehn Bytes sind eine Tatsache. Das Null-Byte steht
    /// bewusst als Zahl und nicht in der Zeichenkette — in einer Quelldatei hat es
    /// nichts verloren.
    /// </remarks>
    private static readonly byte[] SqliteKennung = [.. "SQLite format 3"u8, 0];

    private readonly SqliteConnection _db;

    /// <summary>
    /// Wo die Datenbank liegt, wenn kein <c>--db</c> mitgegeben wurde. Der Ordner kommt
    /// von <see cref="AppPaths"/>, damit Linux- und iOS-Kopf ihn setzen können,
    /// ohne dass hier eine Plattform hartkodiert steht.
    /// </summary>
    public static string DefaultPath => AppPaths.DatabaseFile;

    /// <summary>Die tatsächlich geöffnete Datei — nach einer Migration die neue.</summary>
    public string FilePath { get; }

    /// <summary>
    /// Öffnet die Datenbank und überträgt eine Altdatenbank, falls eine danebenliegt.
    /// </summary>
    /// <param name="path">
    /// Die zu öffnende Datei; <c>null</c> = <see cref="DefaultPath"/>. Zeigt der Pfad auf
    /// eine **Altdatei** (LiteDB — erkennbar daran, dass die SQLite-Kopfkennung fehlt),
    /// wird daneben die gleichnamige <c>.sqlite</c> angelegt und diese geöffnet. Damit
    /// bleibt <c>--db …\gonknote.db</c> aus HANDOFF §8 wörtlich gültig.
    /// </param>
    /// <param name="legacy">
    /// Der Leser für Altdatenbanken (<c>GonkNote.Legacy.LiteDbReader</c>). Fehlt er und
    /// liegt eine Altdatei vor, wird **geworfen** statt still eine leere Datenbank neben
    /// den vollen Bestandsdaten anzulegen — das wäre für den Nutzer nicht von Datenverlust
    /// zu unterscheiden.
    /// </param>
    public DatabaseService(string? path = null, ILegacyDatabaseReader? legacy = null)
    {
        bool eigenerPfad = path != null;
        string ziel = path ?? DefaultPath;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ziel))!);

        string? alt = AltdateiZu(ref ziel);
        FilePath = ziel;

        // Der Blob-Ordner kommt beim Vorgabepfad vom Kopf (IAppPaths, Phase 2 Schritt 4),
        // bei einem ausdrücklich angegebenen Pfad weiterhin aus dessen Dateinamen: sonst
        // zöge eine Testinstanz mit --db die Bilder der echten Ablage an sich.
        Blobs = eigenerPfad ? new BlobStore(ziel) : BlobStore.InFolder(AppPaths.BlobFolder);
        BlobStore.Current = Blobs;

        // Ohne diese Zeile findet der Bild-Cache die ausgelagerten Bilder nicht: SaveBoard
        // leert die Byte-Felder im Dokument, und ImageCache.Bytes fällt dann auf Source
        // zurück. War Source null, lieferte jedes gespeicherte Bild null – graue Platzhalter
        // in Anzeige und Export, Cover zurück auf den Farbverlauf, während die Dateien
        // unversehrt daneben lagen.
        ImageCache.Source = Blobs;

        if (alt != null)
        {
            if (legacy == null)
                throw new InvalidOperationException(
                    $"Neben \"{ziel}\" liegt die Altdatenbank \"{alt}\", aber kein Leser dafür. " +
                    "Der Kopf muss GonkNote.Legacy.LiteDbReader übergeben — sonst startete die " +
                    "App mit einer leeren Datenbank, während die Bestandsdaten danebenliegen.");

            Uebertragen(alt, ziel, legacy, Blobs);
        }

        _db = Oeffnen(ziel);
    }

    /// <summary>
    /// Große Daten (Bilder, importierte PDFs) liegen daneben im Blob-Ordner, nicht im
    /// Datensatz – siehe <see cref="BlobStore"/>.
    /// </summary>
    public BlobStore Blobs { get; }

    // ---- Ordnerbaum ----

    public List<NoteItem> GetAllItems()
    {
        var alle = new List<NoteItem>();
        using var cmd = Befehl("SELECT json FROM items");
        using var leser = cmd.ExecuteReader();
        while (leser.Read())
            if (Lies(leser.GetString(0), GonkJson.Default.NoteItem) is { } item)
                alle.Add(item);
        return alle;
    }

    public void UpsertItem(NoteItem item)
    {
        item.ModifiedUtc = DateTime.UtcNow;
        SchreibeItem(_db, null, item);
    }

    /// <summary>Löscht einen Eintrag samt aller Kinder und Dokumentinhalte.</summary>
    public void DeleteItemRecursive(Guid id)
    {
        foreach (var child in Kinder(id))
            DeleteItemRecursive(child);

        Loeschen("items", id);
        Loeschen("boards", id);
        Loeschen("texts", id);
    }

    private List<Guid> Kinder(Guid id)
    {
        var kinder = new List<Guid>();
        using var cmd = Befehl("SELECT id FROM items WHERE parent_id = $p");
        cmd.Parameters.AddWithValue("$p", Schluessel(id));
        using var leser = cmd.ExecuteReader();
        while (leser.Read()) kinder.Add(Guid.Parse(leser.GetString(0)));
        return kinder;
    }

    private void Loeschen(string tabelle, Guid id)
    {
        using var cmd = Befehl($"DELETE FROM {tabelle} WHERE id = $id");
        cmd.Parameters.AddWithValue("$id", Schluessel(id));
        cmd.ExecuteNonQuery();
    }

    // ---- Dokumentinhalte ----

    public WhiteboardDoc GetBoard(NoteItem item)
    {
        var doc = Lies(EinJson("boards", item.Id), GonkJson.Default.WhiteboardDoc);
        if (doc != null) return doc;
        return item.Kind == ItemKind.Notebook
            ? WhiteboardDoc.NewNotebook(item.Id)
            : WhiteboardDoc.NewWhiteboard(item.Id);
    }

    /// <summary>
    /// Speichert ein Whiteboard/Notizbuch. Bilder wandern dabei **aus** dem Datensatz in den
    /// Blob-Speicher: ein Notizbuch mit importierten PDF-Seiten sprengte sonst jede
    /// vernünftige Datensatzgröße und ließ sich unter LiteDB überhaupt nicht mehr speichern.
    /// <para>
    /// Das leert die Byte-Felder am übergebenen Dokument – gewollt: ab dann liefert der
    /// <see cref="ImageCache"/> die Daten aus dem Blob-Speicher. Bestandsdokumente ziehen so
    /// beim ersten Speichern von selbst um.
    /// </para>
    /// </summary>
    public void SaveBoard(WhiteboardDoc doc)
    {
        MoveImagesToBlobs(doc, Blobs);
        SchreibeBoard(_db, null, doc);
    }

    private static void MoveImagesToBlobs(WhiteboardDoc doc, BlobStore blobs)
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

            blobs.Put(key, data);
            if (blobs.SizeOf(key) == data.Length) clear();
        }
    }

    /// <summary>
    /// Lädt nur die Cover-Gestaltung eines Notizbuchs (ohne die Seiten) für die
    /// Galerie-Vorschau. <c>json_extract</c> schneidet das Cover schon in SQLite heraus,
    /// damit nicht das ganze Board mit allen (ggf. bildlastigen) Seiten durch den
    /// Deserialisierer muss.
    /// </summary>
    public CoverStyle? GetCover(Guid id)
    {
        try
        {
            using var cmd = Befehl("SELECT json_extract(json, '$.Cover') FROM boards WHERE id = $id");
            cmd.Parameters.AddWithValue("$id", Schluessel(id));
            return Lies(cmd.ExecuteScalar() as string, GonkJson.Default.CoverStyle);
        }
        catch (SqliteException)
        {
            // Fällt json_extract einmal aus (sehr alte native Bibliothek), soll die Galerie
            // langsamer werden und nicht leer bleiben.
            return Lies(EinJson("boards", id), GonkJson.Default.WhiteboardDoc)?.Cover;
        }
    }

    public TextDoc GetText(Guid id) =>
        Lies(EinJson("texts", id), GonkJson.Default.TextDoc) ?? new TextDoc { Id = id };

    public void SaveText(TextDoc doc) => SchreibeText(_db, null, doc);

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
        foreach (var text in Alle("texts", GonkJson.Default.TextDoc))
            foreach (var id in text.Images) used.Add(id);
        foreach (var board in Alle("boards", GonkJson.Default.WhiteboardDoc))
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

    public string? GetSetting(string key)
    {
        using var cmd = Befehl("SELECT value FROM settings WHERE id = $id");
        cmd.Parameters.AddWithValue("$id", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetSetting(string key, string value) => SchreibeSetting(_db, null, key, value);

    public void Dispose() => _db.Dispose();

    // ---- Migration ------------------------------------------------------------------------

    /// <summary>
    /// Bestimmt, ob zu <paramref name="ziel"/> eine Altdatenbank gehört, und korrigiert
    /// <paramref name="ziel"/> notfalls auf die daneben liegende SQLite-Datei.
    /// <para>
    /// Zwei Fälle, beide führen auf denselben Stamm und damit auf denselben Blob-Ordner:
    /// </para>
    /// <list type="bullet">
    /// <item>Der Pfad zeigt selbst auf eine Altdatei (<c>--db …\gonknote.db</c>) → Ziel wird
    ///       <c>…\gonknote.sqlite</c>.</item>
    /// <item>Der Pfad zeigt auf eine noch nicht vorhandene SQLite-Datei und daneben liegt
    ///       die gleichnamige <c>.db</c> → das ist der Bestand.</item>
    /// </list>
    /// Erkannt wird an der Kopfkennung, nicht an der Endung: eine Endung ist eine
    /// Behauptung, die ersten sechzehn Bytes sind eine Tatsache.
    /// </summary>
    private static string? AltdateiZu(ref string ziel)
    {
        if (File.Exists(ziel))
        {
            if (IstSqlite(ziel)) return null;               // schon die neue Datei

            string alt = ziel;
            ziel = Path.ChangeExtension(ziel, ".sqlite");
            return File.Exists(ziel) ? null : alt;          // schon einmal übertragen?
        }

        string daneben = Path.ChangeExtension(ziel, ".db");
        if (string.Equals(daneben, ziel, StringComparison.OrdinalIgnoreCase)) return null;
        return File.Exists(daneben) && !IstSqlite(daneben) ? daneben : null;
    }

    private static bool IstSqlite(string path)
    {
        try
        {
            using var s = File.OpenRead(path);
            Span<byte> kopf = stackalloc byte[16];
            return s.ReadAtLeast(kopf, 16, throwOnEndOfStream: false) == 16
                   && kopf.SequenceEqual(SqliteKennung);
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    /// <summary>
    /// Überträgt eine Altdatenbank nach SQLite — <b>rein additiv</b>.
    /// <para>
    /// Geschrieben wird in <c>…​.sqlite.neu</c> und erst nach vollständigem Commit
    /// umbenannt. Scheitert irgendetwas, verschwindet die halbe Datei und der Fehler kommt
    /// nach oben: die App startet dann nicht mit halben Daten, sondern gar nicht. Die
    /// Altdatei wird dabei nur gelesen (siehe <c>LiteDbReader</c>) und bleibt unverändert
    /// liegen — sie ist der Rückweg, solange der Nutzer sie behält.
    /// </para>
    /// Bilder wandern dabei in den Blob-Speicher, wie beim gewöhnlichen Speichern auch.
    /// Auch das ist additiv: <see cref="BlobStore.Put(Guid, byte[])"/> legt Dateien an und
    /// löscht keine, und die Altdatei behält ihre eingebetteten Bytes.
    /// </summary>
    private static void Uebertragen(string alt, string ziel, ILegacyDatabaseReader leser, BlobStore blobs)
    {
        var inhalt = leser.Lies(alt);

        string tmp = ziel + ".neu";
        if (File.Exists(tmp)) File.Delete(tmp);

        try
        {
            using (var neu = Oeffnen(tmp))
            using (var tx = neu.BeginTransaction())
            {
                // Bewusst nicht über UpsertItem: das setzt ModifiedUtc auf jetzt und
                // datierte damit jeden Eintrag des Nutzers auf den Tag der Migration um.
                foreach (var item in inhalt.Items) SchreibeItem(neu, tx, item);

                foreach (var board in inhalt.Boards)
                {
                    MoveImagesToBlobs(board, blobs);
                    SchreibeBoard(neu, tx, board);
                }

                foreach (var text in inhalt.Texts) SchreibeText(neu, tx, text);
                foreach (var (key, value) in inhalt.Settings) SchreibeSetting(neu, tx, key, value);

                SchreibeMeta(neu, tx, "migriert-aus", Path.GetFileName(alt));
                tx.Commit();
            }

            // Erst hier entsteht die neue Datenbank. Bis zu dieser Zeile hat ein Abbruch
            // nichts hinterlassen als eine Datei mit der Endung .neu.
            File.Move(tmp, ziel);
        }
        catch
        {
            try { File.Delete(tmp); } catch (IOException) { }
            throw;
        }
    }

    // ---- SQLite-Handwerk --------------------------------------------------------------------

    private static SqliteConnection Oeffnen(string path)
    {
        var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            // **Kein Verbindungspool.** Sonst hält Microsoft.Data.Sqlite die Datei nach
            // Dispose weiter offen: der Wegwerf-Ordner eines Tests ließe sich nicht löschen,
            // und File.Move am Ende der Migration schlüge fehl.
            Pooling = false,
        }.ToString());

        verbindung.Open();

        using (var pragma = verbindung.CreateCommand())
        {
            // Wartet, statt sofort "database is locked" zu melden — der Ersatz für
            // ConnectionType.Shared von LiteDB (zwei Programminstanzen auf einer Datei).
            pragma.CommandText = "PRAGMA busy_timeout = 5000";
            pragma.ExecuteNonQuery();
        }

        // **Bewusst kein WAL.** Der Write-Ahead-Log legt zwei Nebendateien an (-wal, -shm).
        // Wer die Datenbank dann kopiert, ohne sie mitzunehmen, verliert die zuletzt
        // geschriebenen Änderungen — und genau das tut HANDOFF §8 beim Gegenprüfen mit
        // echten Daten. Eine Datei bleibt eine Datei.

        using (var schema = verbindung.CreateCommand())
        {
            schema.CommandText = """
                CREATE TABLE IF NOT EXISTS meta     (id TEXT PRIMARY KEY, value TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS items    (id TEXT PRIMARY KEY, parent_id TEXT NULL, json TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS boards   (id TEXT PRIMARY KEY, json TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS texts    (id TEXT PRIMARY KEY, json TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS settings (id TEXT PRIMARY KEY, value TEXT NOT NULL);
                CREATE INDEX IF NOT EXISTS ix_items_parent ON items(parent_id);
                """;
            schema.ExecuteNonQuery();
        }

        SchreibeMeta(verbindung, null, "schema", SchemaVersion.ToString());
        return verbindung;
    }

    private SqliteCommand Befehl(string sql)
    {
        var cmd = _db.CreateCommand();
        cmd.CommandText = sql;
        return cmd;
    }

    private string? EinJson(string tabelle, Guid id)
    {
        using var cmd = Befehl($"SELECT json FROM {tabelle} WHERE id = $id");
        cmd.Parameters.AddWithValue("$id", Schluessel(id));
        return cmd.ExecuteScalar() as string;
    }

    private List<T> Alle<T>(string tabelle, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typ)
    {
        var alle = new List<T>();
        using var cmd = Befehl($"SELECT json FROM {tabelle}");
        using var leser = cmd.ExecuteReader();
        while (leser.Read())
            if (Lies(leser.GetString(0), typ) is { } wert)
                alle.Add(wert);
        return alle;
    }

    private static T? Lies<T>(string? json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typ) =>
        json == null ? default : JsonSerializer.Deserialize(json, typ);

    /// <summary>Guids immer in derselben Schreibweise, sonst findet kein PRIMARY KEY etwas.</summary>
    private static string Schluessel(Guid id) => id.ToString("D");

    private static void SchreibeItem(SqliteConnection db, SqliteTransaction? tx, NoteItem item) =>
        Schreiben(db, tx,
            """
            INSERT INTO items (id, parent_id, json) VALUES ($id, $p, $j)
            ON CONFLICT(id) DO UPDATE SET parent_id = excluded.parent_id, json = excluded.json
            """,
            cmd =>
            {
                cmd.Parameters.AddWithValue("$id", Schluessel(item.Id));
                cmd.Parameters.AddWithValue("$p", item.ParentId is { } p ? Schluessel(p) : DBNull.Value);
                cmd.Parameters.AddWithValue("$j", JsonSerializer.Serialize(item, GonkJson.Default.NoteItem));
            });

    private static void SchreibeBoard(SqliteConnection db, SqliteTransaction? tx, WhiteboardDoc doc) =>
        SchreibeJson(db, tx, "boards", doc.Id, JsonSerializer.Serialize(doc, GonkJson.Default.WhiteboardDoc));

    private static void SchreibeText(SqliteConnection db, SqliteTransaction? tx, TextDoc doc) =>
        SchreibeJson(db, tx, "texts", doc.Id, JsonSerializer.Serialize(doc, GonkJson.Default.TextDoc));

    private static void SchreibeJson(SqliteConnection db, SqliteTransaction? tx, string tabelle, Guid id, string json) =>
        Schreiben(db, tx,
            $"""
             INSERT INTO {tabelle} (id, json) VALUES ($id, $j)
             ON CONFLICT(id) DO UPDATE SET json = excluded.json
             """,
            cmd =>
            {
                cmd.Parameters.AddWithValue("$id", Schluessel(id));
                cmd.Parameters.AddWithValue("$j", json);
            });

    private static void SchreibeSetting(SqliteConnection db, SqliteTransaction? tx, string key, string value) =>
        Schreiben(db, tx,
            """
            INSERT INTO settings (id, value) VALUES ($id, $v)
            ON CONFLICT(id) DO UPDATE SET value = excluded.value
            """,
            cmd =>
            {
                cmd.Parameters.AddWithValue("$id", key);
                cmd.Parameters.AddWithValue("$v", value);
            });

    private static void SchreibeMeta(SqliteConnection db, SqliteTransaction? tx, string key, string value) =>
        Schreiben(db, tx,
            """
            INSERT INTO meta (id, value) VALUES ($id, $v)
            ON CONFLICT(id) DO UPDATE SET value = excluded.value
            """,
            cmd =>
            {
                cmd.Parameters.AddWithValue("$id", key);
                cmd.Parameters.AddWithValue("$v", value);
            });

    private static void Schreiben(SqliteConnection db, SqliteTransaction? tx, string sql, Action<SqliteCommand> parameter)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        parameter(cmd);
        cmd.ExecuteNonQuery();
    }
}
