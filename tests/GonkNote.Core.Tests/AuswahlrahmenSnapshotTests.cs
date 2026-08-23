using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Pixelgenaue Snapshots des Auswahlrahmens (<see cref="WbSelectionRenderer"/>), neu in
/// Phase 4.5.
///
/// <para>
/// <b>Warum ausgerechnet hier Golden-Files.</b> Der Rahmen ist beim Zusammenlegen aus dem
/// WPF-Kopf nach Core gewandert, und die alt↔neu-Gleichheit wurde <b>einmal</b> gemessen
/// (byteweise gegen die abgeschriebene Altfassung, 0 Abweichungen). Diese Messung ist danach
/// weggeworfen worden — sie hätte die alte Fassung für immer mitgepflegt, und eine Altfassung
/// im Test ist genau die zweite Fassung, die die Zusammenlegung beseitigen sollte. Was bleibt,
/// muss die <b>Zukunft</b> halten: dass die Griffe nicht unbemerkt verrutschen, wenn jemand an
/// den Maßen in <see cref="Editing.WbHandles"/> dreht.
/// </para>
/// <para>
/// <b>Kein Text im Bild</b> — aus demselben Grund wie in <see cref="RendererSnapshotTests"/>:
/// ein Hash über gezeichnete Schrift prüfte die Schriftausstattung des Rechners und wäre auf
/// dem Linux-Läufer dauerhaft rot. Der Rahmen zeichnet ohnehin keine.
/// </para>
/// </summary>
public sealed class AuswahlrahmenSnapshotTests
{
    private const int Breite = 320;
    private const int Hoehe = 240;

    /// <summary>Ein Bild ohne Bilddaten — <see cref="WbRenderer.ElementBounds"/> genügt X/Y/Breite/Höhe.</summary>
    private static ImageElement Kasten(float x, float y, float b, float h, float drehung = 0f) =>
        new() { X = x, Y = y, Width = b, Height = h, Rotation = drehung };

    private static readonly SKColor Akzent = new(0x2E, 0x7D, 0xFF, 0xFF);

    private static void Rahmen(string name, WbElement? einzeln, SKRect kasten, int anzahl, float zoom) =>
        Snapshot.Assert(name, Breite, Hoehe, canvas =>
            WbSelectionRenderer.Draw(canvas, einzeln, kasten, anzahl, Akzent, zoom));

    [Fact]
    public void Einzelauswahl_ungedreht()
    {
        var el = Kasten(80, 90, 160, 70);
        Rahmen("auswahl-einzeln", el, WbRenderer.ElementBounds(el), 1, 1f);
    }

    /// <summary>
    /// Der Rahmen dreht mit, und der Dreh-Griff wandert mit ihm. Genau hier fiele auf, wenn
    /// jemand die Drehung beim Zeichnen vergisst — der Kasten sähe dann richtig aus, säße aber
    /// achsenparallel.
    /// </summary>
    [Fact]
    public void Einzelauswahl_gedreht()
    {
        var el = Kasten(80, 90, 160, 70, drehung: 35f);
        Rahmen("auswahl-einzeln-gedreht", el, WbRenderer.ElementBounds(el), 1, 1f);
    }

    /// <summary>
    /// Bei doppelter Vergrößerung müssen Griffe und Strichstärke <b>gleich groß</b> bleiben —
    /// alle Maße hängen am Zoom. Ein Snapshot, in dem die Griffe mitwachsen, wäre der Beleg,
    /// dass eine Division verlorengegangen ist.
    /// </summary>
    [Fact]
    public void Einzelauswahl_bei_doppelter_Vergroesserung()
    {
        var el = Kasten(80, 90, 160, 70);
        Rahmen("auswahl-einzeln-zoom2", el, WbRenderer.ElementBounds(el), 1, 2f);
    }

    /// <summary>Mehrere Elemente: achsenparalleler Kasten, nur ein Skalier-Griff, kein Dreh-Griff.</summary>
    [Fact]
    public void Mehrfachauswahl()
    {
        var kasten = new SKRect(60, 70, 250, 170);
        Rahmen("auswahl-mehrfach", null, kasten, 3, 1f);
    }

    /// <summary>Ohne Auswahl bleibt die Fläche leer — der Snapshot ist reines Weiß.</summary>
    [Fact]
    public void Ohne_Auswahl_bleibt_die_Flaeche_leer()
    {
        Rahmen("auswahl-leer", null, SKRect.Empty, 0, 1f);
    }
}
