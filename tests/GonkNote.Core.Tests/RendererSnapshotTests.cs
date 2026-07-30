using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Pixelgenaue Snapshots der Zeichenwege in <see cref="WbRenderer"/> — das Netz für
/// SkiaSharp (Roadmap Phase 1, HANDOFF §4.4/§7).
/// <para>
/// **Was hier bewusst NICHT gehasht wird: alles mit Schrift.** <c>WbFonts</c> fragt „Segoe UI"
/// ab und fällt auf <c>SKTypeface.Default</c> zurück, wenn die Schrift fehlt. Unter Linux
/// fehlt sie. Ein Hash über gezeichneten Text würde deshalb nicht den Renderer prüfen,
/// sondern die Schriftausstattung des Rechners — und wäre in der CI auf dem Ubuntu-Läufer
/// dauerhaft rot. Text hat stattdessen eigene, schriftunabhängige Tests weiter unten:
/// <see cref="SchriftTests"/>.
/// </para>
/// Die Snapshots hier decken genau die Wege ab, die der SkiaSharp-3-Umstieg angefasst hat:
/// Druckabhängige Strichbreite, die Bleistift-Körnung (der Absturz), Textmarker-Alpha,
/// Bildabtastung (<c>SKSamplingOptions</c> statt <c>FilterQuality</c>), das
/// Nine-Patch-Schatten-Bild und die Drehung.
/// </summary>
public sealed class RendererSnapshotTests
{
    private const int Breite = 320;
    private const int Hoehe = 240;

    /// <summary>Eigene Bild-Ids, damit kein anderer Test über den statischen ImageCache hineinredet.</summary>
    private static readonly Guid SnapshotBildId = new("77777777-0000-0000-0000-000000000001");

    [Fact]
    public void Stift_Druck_steuert_die_Strichbreite() =>
        Snapshot.Assert("stift-druck", Breite, Hoehe, canvas =>
            WbRenderer.DrawStroke(canvas, new StrokeElement
            {
                Kind = StrokeKind.Pen,
                Color = "#FF1B2B4B",
                Width = 12f,
                Points =
                {
                    new WbPoint(20, 200, 0.02f),
                    new WbPoint(90, 60, 0.35f),
                    new WbPoint(170, 190, 0.70f),
                    new WbPoint(250, 50, 1.00f),
                    new WbPoint(300, 160, 0.10f),
                },
            }));

    /// <summary>
    /// Ein einzelner Antippen-Punkt (zwei fast gleiche Punkte) wird als Kreis gezeichnet, nicht
    /// als Nulllinie — sonst hinterlässt ein Tippen mit dem Stift nichts.
    /// </summary>
    [Fact]
    public void Stift_Punkt_wird_ein_Kreis() =>
        Snapshot.Assert("stift-punkt", Breite, Hoehe, canvas =>
            WbRenderer.DrawStroke(canvas, new StrokeElement
            {
                Kind = StrokeKind.Pen,
                Color = "#FFDC2626",
                Width = 20f,
                Points = { new WbPoint(160, 120, 1f), new WbPoint(160.2f, 120.1f, 1f) },
            }));

    /// <summary>
    /// **Der wichtigste Snapshot der Klasse.** Die Bleistift-Körnung ist die Stelle, an der
    /// SkiaSharp 3 die App zum Absturz gebracht hat: <c>SKColorFilter.CreateTable(a, null,
    /// null, null)</c> hieß bis 2.88 „diese Kanäle bleiben unverändert" und wirft seit 3.x.
    /// Der Aufruf steckt im **statischen** Konstruktor von <see cref="WbRenderer"/> — er reißt
    /// also nicht nur den Bleistift mit, sondern jeden Zeichenweg.
    /// </summary>
    [Fact]
    public void Bleistift_Koernung() =>
        Snapshot.Assert("bleistift-koernung", Breite, Hoehe, canvas =>
            WbRenderer.DrawStroke(canvas, new StrokeElement
            {
                Kind = StrokeKind.Pencil,
                Color = "#FF334155",
                Width = 9f,
                Points =
                {
                    new WbPoint(20, 120, 0.3f),
                    new WbPoint(110, 60, 0.6f),
                    new WbPoint(210, 180, 0.9f),
                    new WbPoint(300, 100, 0.5f),
                },
            }));

    /// <summary>Textmarker: halbdurchsichtig, stumpfe Enden, gleichmäßige Breite.</summary>
    [Fact]
    public void Textmarker_deckt_nur_halb() =>
        Snapshot.Assert("textmarker", Breite, Hoehe, canvas =>
        {
            // Erst ein Strich darunter: bei einem Marker muss man ihn durchsehen können.
            WbRenderer.DrawStroke(canvas, new StrokeElement
            {
                Kind = StrokeKind.Pen,
                Color = "#FF0F172A",
                Width = 3f,
                Points = { new WbPoint(20, 120, 1f), new WbPoint(300, 120, 1f) },
            });
            WbRenderer.DrawStroke(canvas, new StrokeElement
            {
                Kind = StrokeKind.Highlighter,
                Color = "#FFFACC15",
                Width = 34f,
                Points = { new WbPoint(30, 118, 1f), new WbPoint(160, 126, 1f), new WbPoint(290, 114, 1f) },
            });
        });

    [Theory]
    [InlineData(ShapeKind.Line, null)]
    [InlineData(ShapeKind.Arrow, null)]
    [InlineData(ShapeKind.Rectangle, null)]
    [InlineData(ShapeKind.Rectangle, "#553B82F6")]
    [InlineData(ShapeKind.Ellipse, null)]
    [InlineData(ShapeKind.Ellipse, "#5510B981")]
    [InlineData(ShapeKind.Triangle, null)]
    [InlineData(ShapeKind.Triangle, "#55F59E0B")]
    public void Formen(ShapeKind form, string? fuellung) =>
        Snapshot.Assert($"form-{form.ToString().ToLowerInvariant()}{(fuellung == null ? "" : "-gefuellt")}",
            Breite, Hoehe, canvas =>
                WbRenderer.DrawShape(canvas, new ShapeElement
                {
                    Shape = form,
                    X1 = 40, Y1 = 40, X2 = 280, Y2 = 200,
                    Color = "#FF7C3AED",
                    StrokeWidth = 4f,
                    Fill = fuellung,
                }, "#FF7C3AED", 4f));

    /// <summary>
    /// Bilder werden mit <see cref="WbRenderer.MediumSampling"/> skaliert — der Nachfolger von
    /// <c>SKPaint.FilterQuality</c>. Das Bild wird hier absichtlich hochskaliert (70×50 → 200×140),
    /// damit die Abtastung überhaupt sichtbar wird.
    /// </summary>
    [Fact]
    public void Bild_wird_weich_skaliert() =>
        Snapshot.Assert("bild-skaliert", Breite, Hoehe, canvas =>
            WbRenderer.DrawImage(canvas, new ImageElement
            {
                Id = SnapshotBildId,
                X = 60, Y = 50, Width = 200, Height = 140,
                Data = Beispieldokument.Bild(70, 50, SKColors.MediumSeaGreen),
            }));

    /// <summary>
    /// Ein Bild, das sich nicht dekodieren lässt, bekommt einen grauen Platzhalter — es darf
    /// nicht spurlos verschwinden, sonst ist das Element nicht mehr anklickbar.
    /// </summary>
    [Fact]
    public void Kaputtes_Bild_bekommt_einen_Platzhalter() =>
        Snapshot.Assert("bild-platzhalter", Breite, Hoehe, canvas =>
            WbRenderer.DrawImage(canvas, new ImageElement
            {
                Id = new Guid("77777777-0000-0000-0000-000000000002"),
                X = 60, Y = 50, Width = 200, Height = 140,
                Data = [0x00, 0x01, 0x02, 0x03],   // kein Bildformat
            }));

    /// <summary>
    /// Die Zettelkarte ohne Text: Fläche, Rand und der gecachte Nine-Patch-Schatten. Der
    /// Aufruf <c>DrawImageNinePatch</c> braucht seit SkiaSharp 3 einen ausdrücklichen
    /// Filtermodus — ohne ihn wäre er zweideutig.
    /// </summary>
    [Fact]
    public void Zettel_Karte_mit_Schatten() =>
        Snapshot.Assert("zettel-karte", Breite, Hoehe, canvas =>
            WbRenderer.DrawStickyCard(canvas, new StickyNoteElement
            {
                X = 50, Y = 40, Width = 220, Height = 160,
                Color = "#FFFEF08A",
            }));

    /// <summary>
    /// Der Schatten kommt aus einem **gecachten** Bild, das beim ersten Aufruf entsteht.
    /// Zwei Karten hintereinander müssen deshalb gleich aussehen — täte der Cache das nicht,
    /// wäre die zweite Karte schattenlos oder anders gedehnt.
    /// </summary>
    [Fact]
    public void Zwei_Zettel_sehen_gleich_aus() =>
        Snapshot.Assert("zettel-zwei", Breite, Hoehe, canvas =>
        {
            WbRenderer.DrawStickyCard(canvas, new StickyNoteElement
            {
                X = 20, Y = 40, Width = 120, Height = 120, Color = "#FFFEF08A",
            });
            WbRenderer.DrawStickyCard(canvas, new StickyNoteElement
            {
                X = 180, Y = 40, Width = 120, Height = 120, Color = "#FFFEF08A",
            });
        });

    /// <summary>Drehung: <see cref="WbRenderer.DrawElement"/> dreht um den Mittelpunkt der Umschließung.</summary>
    [Fact]
    public void Drehung_um_den_Elementmittelpunkt() =>
        Snapshot.Assert("drehung", Breite, Hoehe, canvas =>
        {
            // Ungedreht als Bezug, gedreht darüber: die Mittelpunkte müssen zusammenfallen.
            WbRenderer.DrawElement(canvas, new ShapeElement
            {
                Shape = ShapeKind.Rectangle,
                X1 = 60, Y1 = 70, X2 = 260, Y2 = 170,
                Color = "#33000000", StrokeWidth = 2f,
            });
            WbRenderer.DrawElement(canvas, new ShapeElement
            {
                Shape = ShapeKind.Rectangle,
                X1 = 60, Y1 = 70, X2 = 260, Y2 = 170,
                Color = "#FF7C3AED", StrokeWidth = 4f,
                Rotation = 30f,
            });
        });

    /// <summary>Alle Elementklassen in einem Bild — der Rundumschlag über <c>DrawElement</c>.</summary>
    [Fact]
    public void Alle_Elemente_ohne_Schrift() =>
        Snapshot.Assert("alle-elemente", 520, 620, canvas =>
        {
            foreach (var el in Beispieldokument.AlleElemente())
            {
                // Text und Zettel-Text sind schriftabhängig und gehören nicht in einen Hash.
                if (el is TextElement) continue;
                if (el is StickyNoteElement zettel) { WbRenderer.DrawStickyCard(canvas, zettel); continue; }
                WbRenderer.DrawElement(canvas, el);
            }
        });
}
