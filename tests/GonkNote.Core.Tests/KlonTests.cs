using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Wächter für <see cref="WbKlon"/> — Kopieren, Duplizieren, Einfügen.
///
/// <para>
/// <b>Die ersten beiden Wächter halten gemessene Fehler fest, keine ausgedachten.</b> In
/// V2-83 wurde am laufenden WPF-Kopf eine gedrehte Linie dupliziert und kam gerade wieder
/// heraus; ein Bleistiftstrich mit Neigung kam schmaler und glatter heraus. Beides war in der
/// gespeicherten Datei nachzulesen: <c>Rotation 45 → 0</c> und <c>TX 30 → 0</c>.
/// </para>
/// </summary>
public class KlonTests
{
    // ==================== Die zwei gemessenen Fehler ====================

    [Fact]
    public void Klon_behaelt_die_Drehung()
    {
        var el = new ShapeElement
        {
            Shape = ShapeKind.Line, X1 = 120, Y1 = 120, X2 = 340, Y2 = 260,
            Rotation = 45f,
        };

        var klon = (ShapeElement)WbKlon.Klonen(el);

        Assert.Equal(45f, klon.Rotation);
    }

    [Fact]
    public void Klon_behaelt_die_Drehung_bei_jedem_Elementtyp()
    {
        foreach (var el in AlleTypen())
        {
            el.Rotation = 33.5f;
            Assert.Equal(33.5f, WbKlon.Klonen(el).Rotation);
        }
    }

    [Fact]
    public void Klon_eines_Strichs_behaelt_die_Neigung()
    {
        var el = new StrokeElement
        {
            Kind = StrokeKind.Pencil, Width = 8f,
            Points = { new WbPoint(120, 400, 0.8f, 30f, -20f) },
        };

        var klon = (StrokeElement)WbKlon.Klonen(el);

        Assert.Equal(30f, klon.Points[0].TX);
        Assert.Equal(-20f, klon.Points[0].TY);
        Assert.Equal(0.8f, klon.Points[0].P);
    }

    // ==================== Was ein Klon sonst mitbringt — und was nicht ====================

    [Fact]
    public void Klon_bekommt_eine_eigene_Id()
    {
        var el = new StrokeElement { Points = { new WbPoint(0, 0, 0.5f) } };
        Assert.NotEqual(el.Id, WbKlon.Klonen(el).Id);
    }

    [Fact]
    public void Die_Punkte_eines_Klons_sind_eigene_Punkte()
    {
        var el = new StrokeElement { Points = { new WbPoint(10, 10, 0.5f) } };
        var klon = (StrokeElement)WbKlon.Klonen(el);

        klon.Translate(100, 0);

        Assert.Equal(10f, el.Points[0].X);      // das Original bleibt, wo es war
        Assert.Equal(110f, klon.Points[0].X);
    }

    [Fact]
    public void Ein_Bild_teilt_seine_Bytes_mit_dem_Klon()
    {
        // Bewusst so: die Bytes ändern sich nach dem Import nie mehr, und eine PDF-Seite
        // zweimal im Speicher wäre ein spürbarer Posten.
        var el = new ImageElement { Data = [1, 2, 3], Width = 40, Height = 30 };
        Assert.Same(el.Data, ((ImageElement)WbKlon.Klonen(el)).Data);
    }

    [Fact]
    public void Jeder_Elementtyp_hat_einen_Klonweg()
    {
        foreach (var el in AlleTypen())
            Assert.IsType(el.GetType(), WbKlon.Klonen(el));
    }

    [Fact]
    public void Ein_unbekannter_Elementtyp_faellt_auf()
    {
        Assert.Throws<NotSupportedException>(() => WbKlon.Klonen(new FremdesElement()));
    }

    // ==================== Wo der Klon landet ====================

    [Fact]
    public void Ohne_Zielpunkt_rueckt_der_Klon_schraeg_weg()
    {
        var klone = WbKlon.Klonen(new[] { Kasten(0, 0, 100, 100) });

        WbKlon.Platzieren(klone, null);

        var b = WbKlon.Umschliessung(klone);
        Assert.Equal(WbKlon.Versatz, b.Left, 3);
        Assert.Equal(WbKlon.Versatz, b.Top, 3);
    }

    [Fact]
    public void Mit_Zielpunkt_liegt_die_Gruppenmitte_auf_dem_Ziel()
    {
        var klone = WbKlon.Klonen(new[] { Kasten(0, 0, 100, 100), Kasten(200, 200, 100, 100) });

        WbKlon.Platzieren(klone, new SKPoint(1000, 500));

        var b = WbKlon.Umschliessung(klone);
        Assert.Equal(1000f, b.MidX, 3);
        Assert.Equal(500f, b.MidY, 3);
    }

    [Fact]
    public void Die_Elemente_behalten_ihre_Lage_zueinander()
    {
        var a = Kasten(0, 0, 100, 100);
        var b = Kasten(300, 50, 100, 100);
        var klone = WbKlon.Klonen(new[] { a, b });

        WbKlon.Platzieren(klone, new SKPoint(-800, -800));

        float dx = ((ImageElement)klone[1]).X - ((ImageElement)klone[0]).X;
        float dy = ((ImageElement)klone[1]).Y - ((ImageElement)klone[0]).Y;
        Assert.Equal(300f, dx, 3);
        Assert.Equal(50f, dy, 3);
    }

    [Fact]
    public void Eine_Gruppe_weit_weg_vom_Ursprung_bekommt_ihre_eigene_Mitte()
    {
        // Der Fallstrick: SKRect.Empty als Startwert ist der Punkt (0,0). Wer damit
        // anfängt, zieht den Kasten bis zum Ursprung auf — und der Mittelpunkt sitzt falsch.
        var kasten = WbKlon.Umschliessung(new[] { Kasten(5000, 5000, 100, 100) });

        Assert.Equal(5050f, kasten.MidX, 3);
        Assert.Equal(5050f, kasten.MidY, 3);
    }

    [Fact]
    public void Eine_leere_Auswahl_platziert_nichts_und_wirft_nicht()
    {
        WbKlon.Platzieren([], new SKPoint(10, 10));
        Assert.Equal(SKRect.Empty, WbKlon.Umschliessung([]));
    }

    // ==================== Hilfen ====================

    private static ImageElement Kasten(float x, float y, float w, float h) =>
        new() { X = x, Y = y, Width = w, Height = h, Data = [0] };

    private static WbElement[] AlleTypen() =>
    [
        new StrokeElement { Points = { new WbPoint(1, 2, 0.5f) } },
        new ShapeElement { Shape = ShapeKind.Ellipse, X1 = 0, Y1 = 0, X2 = 10, Y2 = 10 },
        new TextElement { X = 1, Y = 2, Text = "hallo" },
        new ImageElement { X = 1, Y = 2, Width = 3, Height = 4, Data = [7] },
        new StickyNoteElement { X = 1, Y = 2, Text = "Zettel" },
    ];

    /// <summary>Ein Typ, den <see cref="WbKlon"/> nicht kennen kann.</summary>
    private sealed class FremdesElement : WbElement
    {
        public override void Translate(float dx, float dy) { }
        public override void Scale(float f, float px, float py) { }
    }
}
