using GonkNote.Core.Editing;
using GonkNote.Core.Models;

namespace GonkNote.Core.Tests;

/// <summary>
/// Der Formen-Stift — <see cref="WbFormen"/> (Phase 5, Schritt ①c).
///
/// <para>
/// <b>Diese Wächter gab es nie, und das ist der eigentliche Befund.</b> Die Erkennung lag als
/// <c>WhiteboardView.Shapes.cs</c> im WPF-Kopf und war von dort aus nicht zu rufen, ohne ein
/// WPF-Fenster zu bauen — **255 Zeilen Geometrie, in fünf Phasen von keinem einzigen Test
/// berührt.** Sie ist in §4.78 nach Core gezogen, ohne eine Zeile Logikänderung; was hier
/// steht, ist die erste Prüfung, die sie je gesehen hat.
/// </para>
/// <para>
/// <b>Geprüft wird die Zusage, nicht die Rechnung.</b> Ein Kreis wird eine Ellipse, ein
/// Rechteck ein Rechteck, ein Gekritzel gar nichts — und **was nicht erkannt wird, geht
/// nicht verloren**, sondern kommt geglättet zurück. Die Schwellwerte selbst sind bewusst
/// nicht festgeschrieben: sie sind auf Gefühl eingestellt und dürfen sich ändern, ohne dass
/// ein Wächter fällt.
/// </para>
/// </summary>
public sealed class FormenstiftTests
{
    private const string Tinte = "#FF1B2B4B";
    private const float Breite = 2.5f;

    private static WbElement? Erkennen(IEnumerable<(float X, float Y)> punkte) =>
        WbFormen.Erkennen([.. punkte.Select(p => new WbPoint(p.X, p.Y, 0.5f))], Tinte, Breite);

    /// <summary>Ein Kreis aus <paramref name="n"/> Punkten, mit optionalem Zittern.</summary>
    private static List<(float X, float Y)> Kreis(float cx, float cy, float r, int n = 48,
                                                  float zittern = 0f)
    {
        var punkte = new List<(float, float)>();
        for (int i = 0; i < n; i++)
        {
            double w = 2 * Math.PI * i / n;
            // Das Zittern ist deterministisch aus dem Index gerechnet — kein Zufall, sonst
            // fiele der Wächter irgendwann ohne Änderung am Code.
            float d = zittern * MathF.Sin(i * 2.7f);
            punkte.Add((cx + (r + d) * (float)Math.Cos(w), cy + (r + d) * (float)Math.Sin(w)));
        }
        punkte.Add(punkte[0]);   // geschlossen
        return punkte;
    }

    /// <summary>Eine Strecke von a nach b, in <paramref name="n"/> Schritten.</summary>
    private static List<(float X, float Y)> Strecke(float x1, float y1, float x2, float y2,
                                                    int n = 20, float zittern = 0f)
    {
        var punkte = new List<(float, float)>();
        for (int i = 0; i <= n; i++)
        {
            float t = (float)i / n;
            float d = zittern * MathF.Sin(i * 1.9f);
            punkte.Add((x1 + t * (x2 - x1), y1 + t * (y2 - y1) + d));
        }
        return punkte;
    }

    private static List<(float X, float Y)> Rechteck(float x1, float y1, float x2, float y2,
                                                     float zittern = 0f)
    {
        var punkte = new List<(float, float)>();
        punkte.AddRange(Strecke(x1, y1, x2, y1, 14, zittern));
        punkte.AddRange(Strecke(x2, y1, x2, y2, 14, zittern));
        punkte.AddRange(Strecke(x2, y2, x1, y2, 14, zittern));
        punkte.AddRange(Strecke(x1, y2, x1, y1, 14, zittern));
        return punkte;
    }

    // ==================== Was nicht erkannt werden darf ====================

    /// <summary>
    /// <b>Zu wenige Punkte und zu kurze Züge ergeben nichts.</b> Ohne diese Schranke würde
    /// jedes Antippen zu einer Form — und der Formen-Stift wäre als Stift unbenutzbar.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Ein_zu_kurzer_Zug_ergibt_keine_Form(int anzahl)
    {
        var punkte = Enumerable.Range(0, anzahl).Select(i => ((float)i, (float)i));
        Assert.Null(Erkennen(punkte));
    }

    [Fact]
    public void Ein_winziger_Zug_ergibt_keine_Form()
    {
        // Zehn Punkte, aber zusammen keine 24 Einheiten lang.
        Assert.Null(Erkennen(Strecke(0, 0, 8, 0, 9)));
    }

    /// <summary>
    /// ⛔ <b>Dieser Wächter hält einen Fehler fest, keine Zusage — und er ist der Grund,
    /// warum der Umzug nach Core sich gelohnt hat.</b>
    ///
    /// <para>
    /// Er war als „ein Gekritzel bleibt ein Gekritzel" geschrieben und ist <b>gefallen</b>.
    /// Gemessen (Wegwerf-Sonde, §4.78): ein Zug aus 120 Punkten, <b>2.886 Einheiten lang</b>
    /// und nur <b>297 weit</b>, gilt als <i>geschlossen</i> und wird zu einem
    /// <b>Streckenzug aus drei Punkten</b> eingedampft. Vom Gekritzel bleibt nichts übrig.
    /// </para>
    /// <para>
    /// <b>Die Ursache steht in einer Zeile:</b> als geschlossen gilt ein Zug, dessen Sehne
    /// kürzer ist als <c>Länge × 0,16</c> — <b>die Schwelle wächst also mit der Zuglänge.</b>
    /// Je länger und krakeliger ein Strich ist, desto eher hält ihn die Erkennung für
    /// geschlossen. Das ist genau verkehrt herum. Hier: Schwelle 461, Sehne 297.
    /// </para>
    /// <para>
    /// <b>Warum das trotzdem so stehen bleibt:</b> Der Fehler ist <b>seit Phase 3 im
    /// WPF-Kopf</b> und nicht in §4.78 entstanden — der Umzug hat keine Zeile Logik geändert,
    /// er hat den Fehler nur <i>sichtbar</i> gemacht. Ihn im selben Zug zu reparieren hieße,
    /// den Umzug und eine Verhaltensänderung zusammen zu prüfen; und es ändert ein Werkzeug,
    /// das der Nutzer täglich benutzt. <b>Der Zuschnitt gehört dem Nutzer</b> (§5e).
    /// </para>
    /// <para>
    /// <b>Dieser Wächter friert das falsche Verhalten also bewusst ein</b>, damit es nicht
    /// unbemerkt wieder verschwindet <i>oder</i> unbemerkt schlimmer wird. <b>Wer die
    /// Schwelle richtigstellt, lässt ihn fallen — und das ist dann die richtige Meldung,
    /// nicht die falsche.</b> Der Fund steht in §4.78 und als offener Punkt in §5e.
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_langes_Gekritzel_wird_faelschlich_zusammengefaltet()
    {
        var punkte = new List<(float, float)>();
        for (int i = 0; i < 120; i++)
            punkte.Add((i * 2.5f, 40f * MathF.Sin(i * 0.9f) + 12f * MathF.Cos(i * 2.3f)));

        var zug = Assert.IsType<StrokeElement>(Erkennen(punkte));

        Assert.True(zug.Points.Count <= 4,
            $"Der Fund aus §4.78 ist weg: das Gekritzel überlebt jetzt mit {zug.Points.Count} " +
            "Punkten. Wenn die Schwelle richtiggestellt wurde, gehört dieser Wächter ersetzt " +
            "durch: Assert.Null(Erkennen(punkte)).");
    }

    /// <summary>
    /// <b>Der Gegenwächter, und er hält:</b> Was auch beim langen Zug stimmen muss, ist, dass
    /// aus einem Gekritzel <b>keine saubere Form</b> wird. Ein Streckenzug ist ein Strich und
    /// bleibt anfassbar; ein Rechteck oder eine Ellipse wäre eine erfundene Form.
    /// </summary>
    [Fact]
    public void Ein_Gekritzel_wird_nie_zu_Rechteck_oder_Ellipse()
    {
        for (int variante = 0; variante < 6; variante++)
        {
            var punkte = new List<(float, float)>();
            for (int i = 0; i < 60 + variante * 20; i++)
                punkte.Add((i * (1.5f + variante * 0.4f),
                            35f * MathF.Sin(i * (0.7f + variante * 0.13f))
                          + 11f * MathF.Cos(i * 2.3f)));

            if (Erkennen(punkte) is ShapeElement form)
                Assert.Fail($"Variante {variante}: aus einem Gekritzel wurde {form.Shape}.");
        }
    }

    // ==================== Die Gerade ====================

    [Fact]
    public void Ein_fast_gerader_Zug_wird_eine_Gerade()
    {
        var form = Assert.IsType<ShapeElement>(Erkennen(Strecke(20, 100, 320, 104, 30, zittern: 1.2f)));

        Assert.Equal(ShapeKind.Line, form.Shape);
        Assert.Equal(Tinte, form.Color);
        Assert.Equal(Breite, form.StrokeWidth);
        // Anfang bleibt, wo gezeichnet wurde.
        Assert.Equal(20f, form.X1, 0.01);
        Assert.Equal(100f, form.Y1, 0.01);
    }

    /// <summary>
    /// <b>Nahe an der Waagerechten rastet die Gerade exakt ein</b> — das ist der sichtbare
    /// Zweck des Werkzeugs. Bei 4° Schräglage muss hinten eine waagerechte Linie stehen.
    /// </summary>
    [Fact]
    public void Eine_fast_waagerechte_Gerade_rastet_auf_null_Grad_ein()
    {
        var form = Assert.IsType<ShapeElement>(Erkennen(Strecke(0, 0, 300, 21, 30)));

        Assert.Equal(ShapeKind.Line, form.Shape);
        Assert.Equal(form.Y1, form.Y2, 0.01);
    }

    /// <summary>
    /// <b>Und weit weg vom Raster rastet sie nicht ein.</b> Ohne diesen Gegenwächter wäre der
    /// erste nur die halbe Aussage: eine Bedingung, die immer zutrifft, prüft nichts.
    /// </summary>
    [Fact]
    public void Eine_deutlich_schraege_Gerade_bleibt_schraeg()
    {
        var form = Assert.IsType<ShapeElement>(Erkennen(Strecke(0, 0, 300, 120, 30)));

        Assert.Equal(ShapeKind.Line, form.Shape);
        Assert.NotEqual(form.Y1, form.Y2, 1.0);
        // 22° ist kein 45°-Schritt — der Endpunkt bleibt, wo er war.
        Assert.Equal(120f, form.Y2, 0.5);
    }

    // ==================== Der Kreis ====================

    [Fact]
    public void Ein_gezeichneter_Kreis_wird_eine_Ellipse()
    {
        var form = Assert.IsType<ShapeElement>(Erkennen(Kreis(200, 200, 80, zittern: 3f)));

        Assert.Equal(ShapeKind.Ellipse, form.Shape);
        float w = form.X2 - form.X1, h = form.Y2 - form.Y1;
        Assert.Equal(w, h, 0.01);                       // fast rund → exakt rund
        Assert.Equal(160f, w, 8f);
    }

    /// <summary>Eine deutlich ovale Form bleibt oval und wird nicht zum Kreis gerundet.</summary>
    [Fact]
    public void Ein_flaches_Oval_bleibt_oval()
    {
        var punkte = new List<(float, float)>();
        for (int i = 0; i < 48; i++)
        {
            double w = 2 * Math.PI * i / 48;
            punkte.Add((200 + 140 * (float)Math.Cos(w), 200 + 50 * (float)Math.Sin(w)));
        }
        punkte.Add(punkte[0]);

        var form = Assert.IsType<ShapeElement>(Erkennen(punkte));

        Assert.Equal(ShapeKind.Ellipse, form.Shape);
        Assert.True(form.X2 - form.X1 > 2f * (form.Y2 - form.Y1),
            "Das flache Oval ist zum Kreis gerundet worden.");
    }

    // ==================== Das Rechteck ====================

    [Fact]
    public void Ein_gezeichnetes_Rechteck_wird_ein_Rechteck()
    {
        var form = Assert.IsType<ShapeElement>(Erkennen(Rechteck(50, 60, 250, 180, zittern: 2f)));

        Assert.Equal(ShapeKind.Rectangle, form.Shape);
        Assert.Equal(50f, form.X1, 6f);
        Assert.Equal(60f, form.Y1, 6f);
        Assert.Equal(250f, form.X2, 6f);
        Assert.Equal(180f, form.Y2, 6f);
    }

    /// <summary>
    /// <b>Der Startpunkt darf das Ergebnis nicht ändern.</b> Genau dafür dreht
    /// <c>GeschlosseneEcken</c> den Zug an die centroid-fernste Stelle, bevor gerechnet wird —
    /// ohne das ergäbe dasselbe Rechteck je nach Anfangsecke ein anderes Polygon.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(29)]
    [InlineData(43)]
    public void Dasselbe_Rechteck_ergibt_dasselbe_Ergebnis_egal_wo_es_anfaengt(int versatz)
    {
        var roh = Rechteck(50, 60, 250, 180);
        var gedreht = roh.Skip(versatz).Concat(roh.Take(versatz));

        var form = Assert.IsType<ShapeElement>(Erkennen(gedreht));

        Assert.Equal(ShapeKind.Rectangle, form.Shape);
        Assert.Equal(50f, form.X1, 2f);
        Assert.Equal(60f, form.Y1, 2f);
        Assert.Equal(250f, form.X2, 2f);
        Assert.Equal(180f, form.Y2, 2f);
    }

    /// <summary>
    /// Ein <b>gekipptes</b> Viereck rastet nicht zum achsenparallelen Rechteck ein — es wird
    /// ein Streckenzug. *Ein Rechteck, das der Nutzer schräg gezeichnet hat, gerade zu
    /// drehen, wäre eine Korrektur und keine Erkennung.*
    /// </summary>
    [Fact]
    public void Ein_gekipptes_Viereck_wird_kein_achsenparalleles_Rechteck()
    {
        var punkte = new List<(float, float)>();
        (float X, float Y)[] ecken = [(120, 40), (260, 120), (180, 250), (40, 170)];
        for (int i = 0; i < 4; i++)
        {
            var a = ecken[i];
            var b = ecken[(i + 1) % 4];
            punkte.AddRange(Strecke(a.X, a.Y, b.X, b.Y, 14));
        }

        var ergebnis = Erkennen(punkte);

        Assert.IsNotType<ShapeElement>(ergebnis);
        var zug = Assert.IsType<StrokeElement>(ergebnis);
        Assert.Equal(zug.Points[0].X, zug.Points[^1].X, 0.01);   // geschlossen
        Assert.Equal(zug.Points[0].Y, zug.Points[^1].Y, 0.01);
    }

    // ==================== Farbe und Breite kommen von außen ====================

    /// <summary>
    /// <b>Der Grund, warum der Umzug nach Core überhaupt ging:</b> Tinte und Strichbreite
    /// hingen als einzige am Kopf. Sie sind jetzt Parameter — und was hineingeht, muss
    /// herauskommen, sonst hätte der Kopf sie zu Unrecht abgegeben.
    /// </summary>
    [Fact]
    public void Tinte_und_Breite_werden_durchgereicht()
    {
        var punkte = Kreis(100, 100, 60).Select(p => new WbPoint(p.X, p.Y, 0.5f)).ToList();

        var form = Assert.IsType<ShapeElement>(WbFormen.Erkennen(punkte, "#FFAA00FF", 7.5f));

        Assert.Equal("#FFAA00FF", form.Color);
        Assert.Equal(7.5f, form.StrokeWidth);
    }

    [Fact]
    public void Auch_der_Streckenzug_bekommt_Tinte_und_Breite()
    {
        var punkte = new List<WbPoint>();
        foreach (var (x, y) in Strecke(0, 0, 150, 0, 12).Concat(Strecke(150, 0, 150, 150, 12)))
            punkte.Add(new WbPoint(x, y, 0.5f));

        var zug = Assert.IsType<StrokeElement>(WbFormen.Erkennen(punkte, "#FFAA00FF", 7.5f));

        Assert.Equal("#FFAA00FF", zug.Color);
        Assert.Equal(7.5f, zug.Width);
        Assert.Equal(StrokeKind.Pen, zug.Kind);
    }

    // ==================== Glätten ====================

    /// <summary>
    /// <b>Was nicht erkannt wird, geht nicht verloren</b> — es wird geglättet. Und beim
    /// Glätten müssen **Anfang und Ende stehen bleiben**: ein Strich, der von seinem
    /// Startpunkt wegrutscht, sieht aus, als hätte man danebengetroffen.
    /// </summary>
    [Fact]
    public void Glaetten_laesst_Anfang_und_Ende_stehen()
    {
        var roh = Strecke(10, 10, 210, 90, 40, zittern: 6f)
            .Select(p => new WbPoint(p.X, p.Y, 0.5f)).ToList();

        var glatt = WbFormen.Glaetten(roh);

        Assert.Equal(roh[0].X, glatt[0].X, 0.01);
        Assert.Equal(roh[0].Y, glatt[0].Y, 0.01);
        Assert.Equal(roh[^1].X, glatt[^1].X, 0.01);
        Assert.Equal(roh[^1].Y, glatt[^1].Y, 0.01);
    }

    /// <summary>Geglättet heißt: ruhiger. Gemessen an der Summe der Richtungswechsel.</summary>
    [Fact]
    public void Glaetten_macht_den_Zug_ruhiger()
    {
        var roh = Strecke(10, 10, 310, 10, 60, zittern: 9f)
            .Select(p => new WbPoint(p.X, p.Y, 0.5f)).ToList();

        double vorher = Unruhe(roh) / roh.Count;
        var glatt = WbFormen.Glaetten(roh);
        double nachher = Unruhe(glatt) / glatt.Count;

        Assert.True(nachher < vorher,
            $"Nach dem Glätten ist der Zug nicht ruhiger ({nachher:F3} statt < {vorher:F3}).");

        static double Unruhe(List<WbPoint> p)
        {
            double summe = 0;
            for (int i = 1; i < p.Count - 1; i++)
                summe += Math.Abs((p[i + 1].Y - p[i].Y) - (p[i].Y - p[i - 1].Y));
            return summe;
        }
    }

    [Fact]
    public void Ein_zu_kurzer_Zug_wird_unveraendert_zurueckgegeben()
    {
        List<WbPoint> zwei = [new(0, 0, 0.5f), new(10, 10, 0.5f)];
        Assert.Same(zwei, WbFormen.Glaetten(zwei));
    }

    // ==================== Die Leiste gegen Core ====================

    /// <summary>
    /// <b>Der Wächter, der den Fund gemacht hätte.</b> <c>WbLeiste.Stifte</c> führt in Core
    /// **vier** Stifte und <c>WbLeiste.Kuerzel</c> den Buchstaben <c>G</c> für den
    /// Formen-Stift — **der Linux-Kopf hatte drei Knöpfe und keinen davon.** „G" fand keinen
    /// Knopf und tat still gar nichts; im Bau war nichts zu sehen, und kein Test hat die
    /// beiden Listen je verglichen (§4.78).
    ///
    /// <para>
    /// <b>Was dieser Wächter kann und was nicht:</b> Er hält die Zusage in Core fest — vier
    /// Stifte, jeder mit Kürzel und Tooltip. **Ob ein Kopf sie auch zeigt, kann er nicht
    /// sehen**; das bleibt Sache des laufenden Programms. *Ein Wächter, der behauptet, mehr
    /// zu prüfen als er kann, ist schlimmer als keiner.*
    /// </para>
    /// </summary>
    [Fact]
    public void Core_fuehrt_vier_Stifte_und_jeder_hat_ein_Kuerzel()
    {
        Assert.Equal(
            [ToolType.Pen, ToolType.SmoothPen, ToolType.Pencil, ToolType.Highlighter],
            WbLeiste.Stifte);

        foreach (var stift in WbLeiste.Stifte)
        {
            Assert.Contains(stift, WbLeiste.Kuerzel.Values);
            Assert.True(WbLeiste.IstStift(stift), $"{stift} gilt nicht als Stift.");
        }

        Assert.Equal(ToolType.SmoothPen, WbLeiste.Kuerzel['G']);
    }
}
