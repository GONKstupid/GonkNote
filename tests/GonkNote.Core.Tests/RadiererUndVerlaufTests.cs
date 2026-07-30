using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Punktgenaues Radieren (<see cref="WbErase"/>) und der Verlauf (<see cref="UndoStack"/>).
/// <para>
/// Beide sind reine Kernlogik ohne UI und ziehen in Phase 2 mit nach Avalonia bzw. iOS.
/// Radieren ist dabei der Vorgang, der Nutzerarbeit **zerstört** — wenn dort etwas
/// verrutscht, ist ein Strich zu viel weg, und ohne funktionierendes Undo bleibt er weg.
/// </para>
/// </summary>
public sealed class RadiererUndVerlaufTests
{
    private static StrokeElement Waagerecht(int punkte = 11, float y = 100f) => new()
    {
        Color = "#FF1B2B4B",
        Width = 2.5f,
        Kind = StrokeKind.Pen,
        Points = [.. Enumerable.Range(0, punkte).Select(i => new WbPoint(i * 10f, y, 0.5f))],
    };

    [Fact]
    public void Radieren_in_der_Mitte_hinterlaesst_zwei_Stuecke()
    {
        var strich = Waagerecht();

        var teile = WbErase.SplitStroke(strich, new SKPoint(50, 100), rr: 12f);

        Assert.Equal(2, teile.Count);
        var links = Assert.IsType<StrokeElement>(teile[0]);
        var rechts = Assert.IsType<StrokeElement>(teile[1]);

        Assert.All(links.Points, p => Assert.True(p.X < 38f));
        Assert.All(rechts.Points, p => Assert.True(p.X > 62f));

        // Farbe, Breite und Art müssen mitkommen — sonst wechselt ein Strich beim Radieren
        // sein Aussehen.
        Assert.Equal(strich.Color, links.Color);
        Assert.Equal(strich.Width, links.Width);
        Assert.Equal(strich.Kind, links.Kind);
    }

    [Fact]
    public void Radieren_am_Ende_laesst_ein_Stueck_stehen()
    {
        var teile = WbErase.SplitStroke(Waagerecht(), new SKPoint(100, 100), rr: 15f);

        var rest = Assert.IsType<StrokeElement>(Assert.Single(teile));
        Assert.All(rest.Points, p => Assert.True(p.X < 86f));
    }

    /// <summary>Deckt der Radierer den ganzen Strich ab, verschwindet er — leere Liste.</summary>
    [Fact]
    public void Radieren_ueber_alles_entfernt_den_Strich()
    {
        Assert.Empty(WbErase.SplitStroke(Waagerecht(), new SKPoint(50, 100), rr: 500f));
    }

    [Fact]
    public void Weit_daneben_radiert_bleibt_der_Strich_ganz()
    {
        var strich = Waagerecht();

        var teile = WbErase.SplitStroke(strich, new SKPoint(50, 400), rr: 12f);

        var rest = Assert.IsType<StrokeElement>(Assert.Single(teile));
        Assert.Equal(strich.Points.Count, rest.Points.Count);
    }

    /// <summary>
    /// Der Fall, für den die Zusatzprüfung im Radierer da ist: ein **langes** Segment
    /// kreuzt den Radierkreis, ohne dass einer seiner Endpunkte darin liegt. Ohne die
    /// Prüfung bliebe der Strich am Berührpunkt einfach durchgezogen.
    /// </summary>
    [Fact]
    public void Langes_Segment_wird_auch_ohne_Punkt_im_Kreis_getrennt()
    {
        var strich = new StrokeElement
        {
            Points = { new WbPoint(0, 100, 0.5f), new WbPoint(400, 100, 0.5f), new WbPoint(400, 300, 0.5f) },
        };

        var teile = WbErase.SplitStroke(strich, new SKPoint(200, 100), rr: 10f);

        // Getrennt wird bei (200|100), mitten auf der langen Waagerechten. Das linke
        // Reststück besteht danach nur noch aus einem Punkt und fällt weg (aus einem Punkt
        // wird kein Strich) — übrig bleibt das rechte Stück ab der Ecke.
        var rest = Assert.IsType<StrokeElement>(Assert.Single(teile));
        Assert.DoesNotContain(rest.Points, p => p.X == 0f);
        Assert.Contains(rest.Points, p => p.Y == 300f);
    }

    [Fact]
    public void Abstand_zur_Strecke_rechnet_auch_an_den_Enden_richtig()
    {
        var a = new SKPoint(0, 0);
        var b = new SKPoint(100, 0);

        Assert.Equal(0f, WbErase.SegmentDistance(a, b, new SKPoint(50, 0)), 3);
        Assert.Equal(10f, WbErase.SegmentDistance(a, b, new SKPoint(50, 10)), 3);
        // Hinter dem Ende zählt der Abstand zum Endpunkt, nicht zur verlängerten Geraden.
        Assert.Equal(50f, WbErase.SegmentDistance(a, b, new SKPoint(150, 0)), 3);
        // Entartete Strecke (Punkt) darf nicht durch Null teilen.
        Assert.Equal(5f, WbErase.SegmentDistance(a, a, new SKPoint(3, 4)), 3);
    }

    // ---- Verlauf --------------------------------------------------------------------------

    [Fact]
    public void Hinzufuegen_laesst_sich_zuruecknehmen_und_wiederholen()
    {
        var seite = new WbPage();
        var stack = new UndoStack();
        var strich = Waagerecht();

        seite.Elements.Add(strich);
        stack.Push(seite, new AddElementsAction([strich]));

        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);

        Assert.Same(seite, stack.Undo());
        Assert.Empty(seite.Elements);
        Assert.True(stack.CanRedo);

        Assert.Same(seite, stack.Redo());
        Assert.Same(strich, Assert.Single(seite.Elements));
    }

    /// <summary>
    /// Zurückgenommene Elemente kommen an ihren **alten Platz** zurück, nicht nach oben. Die
    /// Reihenfolge ist die Zeichenreihenfolge: ein zurückgeholter Strich, der plötzlich über
    /// einem Bild liegt, ist ein sichtbarer Fehler.
    /// </summary>
    [Fact]
    public void Zuruecknehmen_stellt_die_Zeichenreihenfolge_wieder_her()
    {
        var a = Waagerecht(y: 10);
        var b = Waagerecht(y: 20);
        var c = Waagerecht(y: 30);
        var seite = new WbPage { Elements = { a, b, c } };
        var stack = new UndoStack();

        var aktion = new RemoveElementsAction(seite, [b]);
        aktion.Redo(seite);
        stack.Push(seite, aktion);

        Assert.Equal([a, c], seite.Elements);

        stack.Undo();
        Assert.Equal([a, b, c], seite.Elements);
    }

    [Fact]
    public void Ein_neuer_Schritt_verwirft_den_Wiederholen_Zweig()
    {
        var seite = new WbPage();
        var stack = new UndoStack();
        var erst = Waagerecht(y: 10);
        var dann = Waagerecht(y: 20);

        seite.Elements.Add(erst);
        stack.Push(seite, new AddElementsAction([erst]));
        stack.Undo();
        Assert.True(stack.CanRedo);

        seite.Elements.Add(dann);
        stack.Push(seite, new AddElementsAction([dann]));

        Assert.False(stack.CanRedo);
    }

    /// <summary>
    /// Der Verlauf ist begrenzt, weil er gelöschte Elemente **am Leben hält** — ohne Grenze
    /// wüchse der Speicherbedarf einer langen Sitzung immer weiter (RAM-Ziel 800 MB).
    /// </summary>
    [Fact]
    public void Verlauf_wird_nicht_unbegrenzt_lang()
    {
        var seite = new WbPage();
        var stack = new UndoStack();

        for (int i = 0; i < 250; i++)
        {
            var strich = Waagerecht(y: i);
            seite.Elements.Add(strich);
            stack.Push(seite, new AddElementsAction([strich]));
        }

        int zurueck = 0;
        while (stack.Undo() != null) zurueck++;

        Assert.Equal(200, zurueck);
    }

    [Fact]
    public void Leerer_Verlauf_liefert_null_statt_zu_werfen()
    {
        var stack = new UndoStack();

        Assert.Null(stack.Undo());
        Assert.Null(stack.Redo());
        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    /// <summary>
    /// Das Zusammenspiel, um das es beim Radieren wirklich geht: Strich auftrennen, Schritt
    /// verbuchen, zurücknehmen — danach muss **ein** ganzer Strich stehen, nicht zwei Stücke.
    /// </summary>
    [Fact]
    public void Radieren_und_Zuruecknehmen_ergibt_wieder_einen_ganzen_Strich()
    {
        var strich = Waagerecht();
        var seite = new WbPage { Elements = { strich } };
        var stack = new UndoStack();

        var teile = WbErase.SplitStroke(strich, new SKPoint(50, 100), rr: 12f);
        int index = seite.Elements.IndexOf(strich);
        var schritt = new PartialEraseAction([new EraseStep(strich, index, teile)]);
        schritt.Redo(seite);
        stack.Push(seite, schritt);

        Assert.Equal(2, seite.Elements.Count);

        stack.Undo();
        Assert.Same(strich, Assert.Single(seite.Elements));

        stack.Redo();
        Assert.Equal(2, seite.Elements.Count);
    }
}
