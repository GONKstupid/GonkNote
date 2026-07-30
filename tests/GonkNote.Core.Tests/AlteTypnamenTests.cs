using GonkNote.Core.Models;
using GonkNote.Core.Services;
using LiteDB;

namespace GonkNote.Core.Tests;

/// <summary>
/// Der <c>ModelTypeBinder</c> — die einzige Stelle, an der alte Typnamen auf heutige
/// abgebildet werden.
/// <para>
/// Warum das ein eigener Test ist: LiteDB schreibt je Whiteboard-Element ein Feld
/// <c>_type</c> im Format „Namensraum.Typ, Assembly". Beides hat sich seit V1 zweimal
/// geändert. Passt die Abbildung nicht, lässt sich **kein einziges** Bestandsdokument mehr
/// öffnen — und zwar mit einem Absturz, nicht mit einer leeren Seite. Der Roundtrip-Test
/// bemerkt das nicht: der schreibt und liest immer mit dem heutigen Namen.
/// </para>
/// Die Dokumente hier werden deshalb **von Hand** mit den historischen Namen geschrieben,
/// direkt über LiteDB und ohne den <see cref="DatabaseService"/>. Genau so liegen sie in
/// einer alten Datenbank.
/// <para>
/// **Für Phase 2:** Beim Umbau auf SQLite/Json muss dieselbe Übersetzung mitkommen. Diese
/// Testklasse ist der Beweis, dass sie es tut.
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

        using var db = new DatabaseService(ws.DatabasePath);
        var doc = db.GetBoard(new NoteItem { Id = Beispieldokument.DokumentId, Kind = ItemKind.Notebook });

        var page = Assert.Single(doc.Pages);
        var stroke = Assert.IsType<StrokeElement>(Assert.Single(page.Elements));
        Assert.Equal(2, stroke.Points.Count);
        Assert.Equal(0.75f, stroke.Points[1].P);
        Assert.Equal("#FF1B2B4B", stroke.Color);
    }

    /// <summary>
    /// Gelesen wird alt, geschrieben immer neu: ein Bestandsdokument zieht beim ersten
    /// Speichern von selbst um. Sonst müsste die Abbildungstabelle für alle Zeiten wachsen.
    /// </summary>
    [Fact]
    public void Speichern_schreibt_den_heutigen_Typnamen()
    {
        using var ws = new TempWorkspace("legacy-upgrade");
        SchreibeRoh(ws.DatabasePath, "GonkNote.Models.StrokeElement, GonkNote");

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            var doc = db.GetBoard(new NoteItem { Id = Beispieldokument.DokumentId, Kind = ItemKind.Notebook });
            db.SaveBoard(doc);
        }

        Assert.Equal("GonkNote.Core.Models.StrokeElement, GonkNote.Core", LiesTypname(ws.DatabasePath));
    }

    /// <summary>
    /// Ein Typ, den es nicht mehr gibt, darf **nicht** stillschweigend verschwinden: eine
    /// Seite, die plötzlich ein Element weniger hat, sieht wie eine leere Seite aus, und der
    /// Nutzer speichert den Verlust beim nächsten Mal fest. Der Ladevorgang scheitert
    /// deshalb hörbar.
    /// </summary>
    [Fact]
    public void Unbekannter_Typ_verschwindet_nicht_stillschweigend()
    {
        using var ws = new TempWorkspace("legacy-unknown");
        SchreibeRoh(ws.DatabasePath, "GonkNote.Models.GibtEsNichtElement, GonkNote");

        using var db = new DatabaseService(ws.DatabasePath);
        Assert.ThrowsAny<Exception>(() =>
            db.GetBoard(new NoteItem { Id = Beispieldokument.DokumentId, Kind = ItemKind.Notebook }));
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

    private static string LiesTypname(string path)
    {
        using var db = new LiteDatabase(path);
        var board = db.GetCollection("boards").FindById(Beispieldokument.DokumentId);
        return board["Pages"].AsArray[0].AsDocument["Elements"].AsArray[0].AsDocument["_type"].AsString;
    }
}
