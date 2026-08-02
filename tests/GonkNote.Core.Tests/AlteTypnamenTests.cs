using System.IO;
using System.Security.Cryptography;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using GonkNote.Legacy;
using LiteDB;
using Microsoft.Data.Sqlite;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die Übertragung einer Altdatenbank nach SQLite — und darin die einzige Stelle, an der
/// alte Typnamen auf heutige abgebildet werden.
/// <para>
/// Warum das ein eigener Test ist: LiteDB schrieb je Whiteboard-Element ein Feld
/// <c>_type</c> im Format „Namensraum.Typ, Assembly". Beides hat sich seit V1 zweimal
/// geändert. Passt die Abbildung nicht, lässt sich **kein einziges** Bestandsdokument mehr
/// öffnen. Der Roundtrip-Test bemerkt das nicht: der schreibt und liest immer mit dem
/// heutigen Namen.
/// </para>
/// Die Dokumente hier werden deshalb **von Hand** mit den historischen Namen geschrieben,
/// direkt über LiteDB und ohne den <see cref="DatabaseService"/>. Genau so liegen sie in
/// einer alten Datenbank.
/// <para>
/// <b>LiteDB ist seit Phase 2, Schritt 3 kein Produktivpfad mehr</b> — es steht nur noch in
/// <c>GonkNote.Legacy</c> und hier. Hier ist es der <em>Fixture-Erzeuger</em>: der Test legt
/// eine echte Altdatei an und weist nach, dass der SQLite-Weg sie einliest. Aus dem
/// Roundtrip-Wächter ist der <b>Migrations</b>-Wächter geworden.
/// </para>
/// </summary>
public sealed class AlteTypnamenTests
{
    /// <summary>Wie die Namen in einer Datenbank je Programmstand aussehen.</summary>
    [Theory]
    // V1, vor dem Umbenennen der Assembly: alter Namensraum, alte Assembly
    [InlineData("GonkNote.Models.StrokeElement, GonkNote")]
    // Zwischenstand: Assembly schon umbenannt, Namensraum noch nicht
    [InlineData("GonkNote.Models.StrokeElement, GonkNote.Core")]
    // Heute: beides neu
    [InlineData("GonkNote.Core.Models.StrokeElement, GonkNote.Core")]
    // Ohne Assemblyangabe — die wird beim Lesen ohnehin ignoriert
    [InlineData("GonkNote.Core.Models.StrokeElement")]
    public void Bestandsdokument_laesst_sich_oeffnen(string typname)
    {
        using var ws = new TempWorkspace("legacy-type");
        SchreibeRoh(ws.DatabasePath, typname);

        using var db = new DatabaseService(ws.DatabasePath, new LiteDbReader());
        var doc = db.GetBoard(new NoteItem { Id = Beispieldokument.DokumentId, Kind = ItemKind.Notebook });

        var page = Assert.Single(doc.Pages);
        var stroke = Assert.IsType<StrokeElement>(Assert.Single(page.Elements));
        Assert.Equal(2, stroke.Points.Count);
        Assert.Equal(0.75f, stroke.Points[1].P);
        Assert.Equal("#FF1B2B4B", stroke.Color);
    }

    /// <summary>
    /// Gelesen wird alt, geschrieben immer neu: ein Bestandsdokument zieht bei der
    /// Übertragung ein für alle Mal um. Sonst müsste die Abbildungstabelle in
    /// <c>LiteDbReader</c> für alle Zeiten wachsen.
    /// <para>
    /// Der erwartete Wert ist **wörtlich** die Zeichenkette aus dem
    /// <c>[JsonDerivedType]</c> an <see cref="WbElement"/>. Weicht einer der beiden ab,
    /// öffnet sich kein Bestandsdokument mehr — und der Fehler sieht aus wie ein leeres
    /// Whiteboard, nicht wie ein Absturz.
    /// </para>
    /// </summary>
    [Fact]
    public void Migration_schreibt_den_heutigen_Typnamen()
    {
        using var ws = new TempWorkspace("legacy-upgrade");
        SchreibeRoh(ws.DatabasePath, "GonkNote.Models.StrokeElement, GonkNote");

        using (new DatabaseService(ws.DatabasePath, new LiteDbReader())) { }

        Assert.Equal("GonkNote.Core.Models.StrokeElement, GonkNote.Core", TypnameAusSqlite(ws));
    }

    /// <summary>
    /// Die Zusage, an der der ganze Schritt hängt: die Übertragung ist **additiv**. Die
    /// Altdatei wird nur gelesen — kein Byte darin ändert sich, auch kein Zeitstempel im
    /// Kopf, den LiteDB sonst beim Öffnen fortschreibt. Solange der Nutzer sie behält, ist
    /// sie der Rückweg.
    /// </summary>
    [Fact]
    public void Migration_laesst_die_Altdatei_unveraendert()
    {
        using var ws = new TempWorkspace("legacy-untouched");
        SchreibeRoh(ws.DatabasePath, "GonkNote.Models.StrokeElement, GonkNote");

        byte[] vorher = Pruefsumme(ws.DatabasePath);

        using (var db = new DatabaseService(ws.DatabasePath, new LiteDbReader()))
        {
            // Nicht nur öffnen: auch schreiben. Das darf ausschließlich die neue Datei treffen.
            var doc = db.GetBoard(new NoteItem { Id = Beispieldokument.DokumentId, Kind = ItemKind.Notebook });
            db.SaveBoard(doc);
            db.SetSetting("theme", "dark");
        }

        Assert.True(File.Exists(ws.DatabasePath), "Die Altdatei wurde entfernt oder umbenannt.");
        Assert.Equal(vorher, Pruefsumme(ws.DatabasePath));

        // Und die neue Datei liegt daneben, nicht an ihrer Stelle.
        Assert.True(File.Exists(Path.Combine(ws.Root, "wegwerf.sqlite")));
    }

    /// <summary>
    /// Zweimal starten darf nicht zweimal übertragen: beim zweiten Mal liegt die neue Datei
    /// schon da, und alles, was seitdem darin steht, muss erhalten bleiben. Würde erneut
    /// übertragen, wäre jede Arbeit seit dem Umstieg still weg.
    /// </summary>
    [Fact]
    public void Zweiter_Start_uebertraegt_nicht_noch_einmal()
    {
        using var ws = new TempWorkspace("legacy-einmalig");
        SchreibeRoh(ws.DatabasePath, "GonkNote.Models.StrokeElement, GonkNote");

        using (var db = new DatabaseService(ws.DatabasePath, new LiteDbReader()))
            db.UpsertItem(new NoteItem { Name = "Nach dem Umstieg angelegt", Kind = ItemKind.Folder });

        using (var db = new DatabaseService(ws.DatabasePath, new LiteDbReader()))
        {
            Assert.Equal("Nach dem Umstieg angelegt", Assert.Single(db.GetAllItems()).Name);

            // Der Bestand aus der Altdatei ist trotzdem noch da.
            var doc = db.GetBoard(new NoteItem { Id = Beispieldokument.DokumentId, Kind = ItemKind.Notebook });
            Assert.IsType<StrokeElement>(Assert.Single(Assert.Single(doc.Pages).Elements));
        }
    }

    /// <summary>
    /// Ein Typ, den es nicht mehr gibt, darf **nicht** stillschweigend verschwinden: eine
    /// Seite, die plötzlich ein Element weniger hat, sieht wie eine leere Seite aus, und der
    /// Nutzer speichert den Verlust beim nächsten Mal fest. Die Übertragung scheitert
    /// deshalb hörbar — und lässt dabei keine halbe neue Datenbank zurück.
    /// </summary>
    [Fact]
    public void Unbekannter_Typ_verschwindet_nicht_stillschweigend()
    {
        using var ws = new TempWorkspace("legacy-unknown");
        SchreibeRoh(ws.DatabasePath, "GonkNote.Models.GibtEsNichtElement, GonkNote");

        Assert.ThrowsAny<Exception>(() => new DatabaseService(ws.DatabasePath, new LiteDbReader()));

        // Wichtiger als der Fehler selbst: es liegt keine angefangene Datenbank herum, die
        // beim nächsten Start als „schon übertragen" durchginge.
        Assert.False(File.Exists(Path.Combine(ws.Root, "wegwerf.sqlite")));
        Assert.False(File.Exists(Path.Combine(ws.Root, "wegwerf.sqlite.neu")));
    }

    /// <summary>
    /// Ohne Leser darf nicht einfach eine leere Datenbank neben den vollen Bestandsdaten
    /// entstehen — für den Nutzer wäre das von Datenverlust nicht zu unterscheiden. Der
    /// Fall trifft jeden Kopf, der beim Bauen vergisst, den Leser mitzugeben.
    /// </summary>
    [Fact]
    public void Ohne_Leser_wird_nicht_stillschweigend_neu_angefangen()
    {
        using var ws = new TempWorkspace("legacy-ohne-leser");
        SchreibeRoh(ws.DatabasePath, "GonkNote.Core.Models.StrokeElement, GonkNote.Core");

        Assert.Throws<InvalidOperationException>(() => new DatabaseService(ws.DatabasePath));
        Assert.False(File.Exists(Path.Combine(ws.Root, "wegwerf.sqlite")));
    }

    // ---- Rohzugriff, absichtlich ohne DatabaseService -------------------------------------

    /// <summary>
    /// Schreibt ein Board-Dokument mit vorgegebenem <c>_type</c>-Wert direkt in die Datei —
    /// so, wie ein älterer Programmstand es hinterlassen hätte.
    /// </summary>
    private static void SchreibeRoh(string path, string typname)
    {
        using var db = new LiteDatabase(path);
        db.GetCollection("boards").Insert(new BsonDocument
        {
            ["_id"] = Beispieldokument.DokumentId,
            ["Pages"] = new BsonArray
            {
                new BsonDocument
                {
                    ["Elements"] = new BsonArray
                    {
                        new BsonDocument
                        {
                            ["_type"] = typname,
                            ["Id"] = Guid.NewGuid(),
                            ["Color"] = "#FF1B2B4B",
                            ["Width"] = 2.5,
                            ["Kind"] = "Pen",
                            ["Points"] = new BsonArray
                            {
                                new BsonDocument { ["X"] = 10.0, ["Y"] = 20.0, ["P"] = 0.5 },
                                new BsonDocument { ["X"] = 60.0, ["Y"] = 80.0, ["P"] = 0.75 },
                            },
                        },
                    },
                    ["Background"] = "Lines",
                    ["Shade"] = "Light",
                    ["Width"] = (double)WhiteboardDoc.A4Width,
                    ["Height"] = (double)WhiteboardDoc.A4Height,
                },
            },
        });
    }

    /// <summary>
    /// Liest den <c>_type</c> aus der **neuen** Datei — bewusst über SQL und json_extract
    /// statt über den DatabaseService: geprüft werden soll das Format auf der Platte, nicht
    /// die Fähigkeit des Programms, das eigene Geschriebene wieder zu lesen.
    /// </summary>
    private static string TypnameAusSqlite(TempWorkspace ws)
    {
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(ws.Root, "wegwerf.sqlite"),
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString());
        db.Open();

        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT json_extract(json, '$.Pages[0].Elements[0]._type') FROM boards";
        return (string)cmd.ExecuteScalar()!;
    }

    private static byte[] Pruefsumme(string path) => SHA256.HashData(File.ReadAllBytes(path));
}
