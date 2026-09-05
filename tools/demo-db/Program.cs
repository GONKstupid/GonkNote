// Legt eine Demo-Datenbank an: Ordner, ein Notizbuch mit einer beschriebenen Seite, ein
// Whiteboard und ein Textdokument. Aus ihr entstehen die Bildschirmfotos für README,
// GitHub Pages und die AppStream-Beschreibung (Phase 5, Schritt ⑤).
//
// Aufruf:  dotnet run --project tools/demo-db -- <zielpfad.sqlite> [de|en]
//
// ⛔ DER INHALT IST ERFUNDEN UND MUSS ES BLEIBEN. Er geht als Bild in ein öffentliches
//    Repo. Nichts hier stammt aus der echten Datenbank -- die enthält Schulunterlagen
//    (HANDOFF, Kopfzeile und Dauerregel 4).
//
// ⚠ Die Zieldatei wird VORHER GELÖSCHT, samt Blob-Ordner. Deshalb nimmt das Werkzeug nur
//   einen Pfad entgegen, den man ihm ausdrücklich nennt, und hat keine Vorgabe: eine
//   Vorgabe wäre irgendwann der echte Datenordner gewesen.

using GonkNote.Core.Models;
using GonkNote.Core.Services;
using GonkNote.Core.Text;

if (args.Length < 1)
{
    Console.Error.WriteLine("Aufruf: demo-db <zielpfad.sqlite> [de|en]");
    return 1;
}

string ziel = Path.GetFullPath(args[0]);
string sprache = args.Length > 1 ? args[1] : "de";
bool en = sprache == "en";

// Aufräumen: die Datei und ihr Blob-Ordner. Der Ordner leitet seinen Namen von der
// Datenbankdatei ab -- wer nur die Datei löscht, erbt die Bilder des letzten Laufs.
Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
foreach (var pfad in new[] { ziel, ziel + "-log" })
    if (File.Exists(pfad)) File.Delete(pfad);
foreach (var anhang in new[] { ".blobs", ".papierkorb" })
{
    var d = Path.ChangeExtension(ziel, null) + anhang;
    if (Directory.Exists(d)) Directory.Delete(d, true);
}

using var db = new DatabaseService(ziel);
db.SetSetting("language", en ? "en" : "de");
db.SetSetting("theme", "light");

// ---------------------------------------------------------------- Ordner und Einträge

NoteItem Neu(string name, ItemKind art, Guid? eltern = null, string? farbe = null,
             bool angepinnt = false, bool favorit = false)
{
    var item = new NoteItem
    {
        Name = name,
        Kind = art,
        ParentId = eltern,
        IconColor = farbe,
        IsPinned = angepinnt,
        IsFavorite = favorit,
    };
    db.UpsertItem(item);
    return item;
}

var studium  = Neu(en ? "University" : "Studium",  ItemKind.Folder, null, "#3B82F6", angepinnt: true);
var projekte = Neu(en ? "Projects"   : "Projekte", ItemKind.Folder, null, "#10B981", angepinnt: true);
_            = Neu(en ? "Private"    : "Privat",   ItemKind.Folder, null, "#F59E0B");

var vorlesung = Neu(en ? "Lecture notes"  : "Vorlesung",      ItemKind.Notebook,     studium.Id, favorit: true);
_             = Neu(en ? "Exercises"      : "Übungen",        ItemKind.Notebook,     studium.Id);
var tafel     = Neu(en ? "Brainstorming"  : "Brainstorming",  ItemKind.Whiteboard,   projekte.Id);
var text      = Neu(en ? "Project report" : "Projektbericht", ItemKind.TextDocument, projekte.Id);
// Ein zweites Notizbuch in "Projekte", damit die Galerie dort ALLE DREI Kachelarten
// nebeneinander zeigt -- Notizbuch-Cover, Whiteboard-Karte, Textdokument-Karte. Genau das
// ist das Bild, das ins README geht; ein Ordner mit zwei gleichen Kacheln zeigt es nicht.
var konzept   = Neu(en ? "Concept"        : "Konzept",        ItemKind.Notebook,     projekte.Id);

// ---------------------------------------------------------------- Das Notizbuch

// Ein Strich mit natürlicher Schwankung: ohne sie sieht jede Linie gezogen aus wie mit dem
// Lineal, und das ist nicht, was diese App tut. Der Druck schwankt mit -- er ist das
// Merkmal, an dem man einen Stiftstrich von einem Mausstrich unterscheidet (§4.10).
static StrokeElement Strich(string farbe, float breite, StrokeKind art,
                            params (float X, float Y)[] punkte)
{
    var zufall = new Random(punkte.Length * 7919 + (int)punkte[0].X);
    var s = new StrokeElement { Color = farbe, Width = breite, Kind = art };
    for (int i = 0; i < punkte.Length; i++)
    {
        // Druck: sanft an, voll in der Mitte, sanft ab -- so setzt eine Hand auf.
        float t = punkte.Length == 1 ? 0.5f : i / (float)(punkte.Length - 1);
        float p = 0.35f + 0.55f * MathF.Sin(t * MathF.PI);
        s.Points.Add(new WbPoint(
            punkte[i].X + (float)(zufall.NextDouble() - 0.5) * 1.6f,
            punkte[i].Y + (float)(zufall.NextDouble() - 0.5) * 1.6f,
            Math.Clamp(p + (float)(zufall.NextDouble() - 0.5) * 0.08f, 0.05f, 1f)));
    }
    return s;
}

// Eine Kurve als Punktfolge -- für den Graphen auf der Seite.
static (float, float)[] Kurve(float x0, float x1, float schritt, Func<float, float> f)
{
    var liste = new List<(float, float)>();
    for (float x = x0; x <= x1; x += schritt) liste.Add((x, f(x)));
    return liste.ToArray();
}

const string Tinte = "#FF1B2B4B";
const string Rot = "#FFDC2626";
const string Grau = "#FF64748B";
const string Gruen = "#FF16A34A";

var buch = WhiteboardDoc.NewNotebook(vorlesung.Id);
buch.Cover = new CoverStyle { GradientStart = "#1E3A8A", GradientEnd = "#7C3AED" };

var seite = buch.Pages[1];

// ⚠ ALLES MUSS ÜBER SEITEN-Y 690 LIEGEN. Die Seite ist 1123 hoch; bei 100 % zeigt das
//   Fenster davon nur die oberen zwei Drittel, und ein Bildschirmfoto zeigt genau das.
//   Der erste Anlauf hatte Notizzettel und Haken bei y=730..840 -- sie waren auf dem Bild
//   schlicht nicht da, und das fiel erst am laufenden Programm auf.
seite.Elements.AddRange(new WbElement[]
{
    // Überschrift und ihre Unterstreichung
    new TextElement
    {
        X = 90, Y = 84, FontSize = 34, Color = Tinte,
        Text = en ? "Signals and systems" : "Signale und Systeme",
    },
    Strich(Tinte, 3.2f, StrokeKind.Pen,
        (92, 140), (140, 138), (200, 141), (260, 139), (320, 142), (380, 140), (430, 139)),

    // Der Textmarker liegt UNTER dem Text, sonst überdeckt er ihn -- er ist halbdurchlässig,
    // aber nicht farblos.
    Strich("#66FACC15", 24f, StrokeKind.Highlighter,
        (95, 200), (170, 201), (250, 199), (330, 202), (410, 200), (490, 201), (556, 200)),
    new TextElement
    {
        X = 92, Y = 184, FontSize = 20, Color = Tinte,
        Text = en
            ? "Fourier: every signal is a sum of waves"
            : "Fourier: jedes Signal ist eine Summe von Wellen",
    },

    // Der Graph: Achsen als Formen, die Kurve mit dem Bleistift
    new ShapeElement { Shape = ShapeKind.Line, X1 = 110, Y1 = 400, X2 = 460, Y2 = 400, Color = Grau, StrokeWidth = 1.6f },
    new ShapeElement { Shape = ShapeKind.Line, X1 = 130, Y1 = 285, X2 = 130, Y2 = 495, Color = Grau, StrokeWidth = 1.6f },
    Strich(Tinte, 2.2f, StrokeKind.Pencil,
        Kurve(135, 452, 5f, x => 400 - 66 * MathF.Sin((x - 135) / 32f)
                                     - 24 * MathF.Sin((x - 135) / 10.5f))),
    new TextElement { X = 464, Y = 384, FontSize = 15, Color = Grau, Text = "t" },

    // Eine Randbemerkung in Rot, wie man sie beim Nachbereiten macht
    new TextElement
    {
        X = 546, Y = 288, FontSize = 17, Color = Rot,
        Text = en ? "exam relevant!" : "klausurrelevant!",
    },
    Strich(Rot, 2.4f, StrokeKind.Pen, (500, 300), (522, 300), (540, 300)),
    Strich(Rot, 2.4f, StrokeKind.Pen, (530, 292), (542, 300), (530, 308)),

    // Ein Notizzettel -- er zeigt, dass die Seite nicht nur Striche trägt
    new StickyNoteElement
    {
        X = 528, Y = 348, Width = 196, Height = 140, FontSize = 16,
        Color = "#FFFEF08A", TextColor = "#FF1F2937",
        Text = en
            ? "Bring the handout to the tutorial on Thursday."
            : "Handout zur Übung am Donnerstag mitbringen.",
    },

    // Zwei Haken in einer Zeile -- das, was man am Ende einer Seite macht
    Strich(Gruen, 3f, StrokeKind.Pen, (112, 546), (126, 562), (156, 522)),
    new TextElement { X = 170, Y = 532, FontSize = 18, Color = Tinte, Text = en ? "revised" : "nachbereitet" },
    Strich(Gruen, 3f, StrokeKind.Pen, (330, 546), (344, 562), (374, 522)),
    new TextElement { X = 388, Y = 532, FontSize = 18, Color = Tinte, Text = en ? "summarised" : "zusammengefasst" },

    // Das kleine Ablaufbild -- Formen-Stift
    new ShapeElement { Shape = ShapeKind.Rectangle, X1 = 110, Y1 = 600, X2 = 268, Y2 = 668, Color = Tinte, StrokeWidth = 2.4f },
    new ShapeElement { Shape = ShapeKind.Arrow, X1 = 276, Y1 = 634, X2 = 356, Y2 = 634, Color = Tinte, StrokeWidth = 2.4f },
    new ShapeElement { Shape = ShapeKind.Ellipse, X1 = 364, Y1 = 596, X2 = 522, Y2 = 672, Color = Tinte, StrokeWidth = 2.4f },
    new TextElement { X = 132, Y = 620, FontSize = 17, Color = Tinte, Text = en ? "input x(t)" : "Eingang x(t)" },
    new TextElement { X = 396, Y = 620, FontSize = 17, Color = Tinte, Text = en ? "output y(t)" : "Ausgang y(t)" },
});
db.SaveBoard(buch);

// Das zweite Notizbuch bleibt leer -- es steht nur für seine Kachel in der Galerie da.
// Ein eigener Farbverlauf, damit die zwei Cover im Bild nicht verwechselbar sind.
var konzeptbuch = WhiteboardDoc.NewNotebook(konzept.Id);
konzeptbuch.Cover = new CoverStyle { GradientStart = "#0F766E", GradientEnd = "#0891B2" };
db.SaveBoard(konzeptbuch);

// ---------------------------------------------------------------- Das Whiteboard

var board = WhiteboardDoc.NewWhiteboard(tafel.Id);
board.Pages[0].Elements.AddRange(new WbElement[]
{
    new TextElement { X = 120, Y = 96, FontSize = 30, Color = Tinte, Text = "Release 1.0" },
    new ShapeElement { Shape = ShapeKind.Ellipse, X1 = 110, Y1 = 220, X2 = 330, Y2 = 330, Color = "#FF2563EB", StrokeWidth = 2.6f, Fill = "#222563EB" },
    new TextElement { X = 168, Y = 262, FontSize = 18, Color = Tinte, Text = "Windows" },
    new ShapeElement { Shape = ShapeKind.Ellipse, X1 = 400, Y1 = 220, X2 = 620, Y2 = 330, Color = "#FF16A34A", StrokeWidth = 2.6f, Fill = "#2216A34A" },
    new TextElement { X = 478, Y = 262, FontSize = 18, Color = Tinte, Text = "Linux" },
    new ShapeElement { Shape = ShapeKind.Arrow, X1 = 340, Y1 = 275, X2 = 392, Y2 = 275, Color = Grau, StrokeWidth = 2.2f },
    new StickyNoteElement
    {
        X = 680, Y = 210, Width = 195, Height = 140, FontSize = 16,
        Color = "#FFBFDBFE", TextColor = "#FF1F2937",
        Text = en ? "One core, two front ends." : "Ein Kern, zwei Oberflächen.",
    },
    Strich(Tinte, 2.6f, StrokeKind.Pen,
        (120, 420), (200, 404), (280, 428), (360, 402), (440, 424), (520, 400), (600, 420)),
});
db.SaveBoard(board);

// ---------------------------------------------------------------- Das Textdokument

// Der Inhalt entsteht aus Markdown und geht durch denselben Leser wie ein Import
// (TdMarkdown.Lesen, §4.99) -- so ist er zwangsläufig ein gültiges Modell und nicht von
// Hand zusammengesteckt.
string md = en
    ? """
      # Project report

      ## 1. Purpose

      This document belongs to the demo database. It is here so the screenshots in the
      README show a *typeset* page and not an empty one.

      The editor lays out **real pages**: headings, character formats, lists, tables,
      images, charts, headers and footers. What is on screen is what the PDF export
      writes — same line breaking, same renderer.

      ## 2. What ships

      1. Notebooks with pen, pencil and highlighter
      2. Whiteboards with shapes, sticky notes and text recognition
      3. Text documents with sections, lists, tables and a table of contents

      * No cloud, no account, no installation
      * Windows and Linux read the same database
      * Export to PDF, DOCX, Markdown and PNG

      ## 3. The two editions

      | Edition | Toolkit | Spell checking |
      |---|---|---|
      | Windows | WPF | yes |
      | Linux | Avalonia | next item |

      > Everything stays on the machine, in your user folder.
      """
    : """
      # Projektbericht

      ## 1. Zweck

      Dieses Dokument gehört zur Demo-Datenbank. Es steht hier, damit die Bildschirmfotos
      im README eine *gesetzte* Seite zeigen und keine leere.

      Der Editor setzt **echte Seiten**: Überschriften, Zeichenformate, Listen, Tabellen,
      Bilder, Diagramme sowie Kopf- und Fußzeile. Was auf dem Schirm steht, schreibt auch
      der PDF-Export — derselbe Umbruch, derselbe Zeichner.

      ## 2. Was ausgeliefert wird

      1. Notizbücher mit Stift, Bleistift und Textmarker
      2. Whiteboards mit Formen, Notizzetteln und Texterkennung
      3. Textdokumente mit Abschnitten, Listen, Tabellen und Inhaltsverzeichnis

      * Keine Cloud, kein Konto, keine Installation
      * Windows und Linux lesen dieselbe Datenbank
      * Export nach PDF, DOCX, Markdown und PNG

      ## 3. Die zwei Ausgaben

      | Ausgabe | Baustein | Rechtschreibprüfung |
      |---|---|---|
      | Windows | WPF | ja |
      | Linux | Avalonia | nächster Punkt |

      > Alles bleibt auf dem Rechner, in deinem Benutzerordner.
      """;

var doc = TdMarkdown.Lesen(md, "", new TdBlobImages(db.Blobs));
db.SaveText(new TextDoc { Id = text.Id, Model = TdFormatIo.Schreiben(doc) });

Console.WriteLine($"Demo-Datenbank angelegt: {ziel} ({sprache})");
return 0;
