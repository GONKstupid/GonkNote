using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>Wächter für <see cref="WbSchnellaktionen"/> und <see cref="WbLeiste"/>.</summary>
public class SchnellaktionenTests
{
    private static readonly SKSize Leiste = new(240, 44);
    private static readonly SKSize Flaeche = new(1200, 800);

    // ==================== Was gerade etwas tun kann ====================

    [Fact]
    public void Ohne_Auswahl_ist_nur_Alles_Waehlen_zu_haben()
    {
        var seite = MitElementen(3);

        var z = WbSchnellaktionen.Rechnen([], seite, eigeneAblage: 0,
                                          systemablageHatBild: false, texterkennungVerfuegbar: false);

        Assert.False(z.Ausschneiden);
        Assert.False(z.Kopieren);
        Assert.False(z.Duplizieren);
        Assert.False(z.Loeschen);
        Assert.False(z.Einfuegen);
        Assert.True(z.AllesWaehlen);
    }

    [Fact]
    public void Auf_leerer_Seite_geht_auch_Alles_Waehlen_nicht()
    {
        var z = WbSchnellaktionen.Rechnen([], MitElementen(0), 0, false, false);
        Assert.False(z.AllesWaehlen);
    }

    [Fact]
    public void Ohne_Seite_geht_gar_nichts()
    {
        var z = WbSchnellaktionen.Rechnen([], null, 0, false, false);
        Assert.False(z.AllesWaehlen);
    }

    [Fact]
    public void Einfuegen_geht_aus_der_eigenen_Ablage_und_aus_der_des_Systems()
    {
        Assert.True(WbSchnellaktionen.Rechnen([], MitElementen(1), 2, false, false).Einfuegen);
        Assert.True(WbSchnellaktionen.Rechnen([], MitElementen(1), 0, true, false).Einfuegen);
        Assert.False(WbSchnellaktionen.Rechnen([], MitElementen(1), 0, false, false).Einfuegen);
    }

    // ==================== Texterkennung ====================

    [Fact]
    public void Ohne_Texterkennung_verschwindet_der_Knopf_statt_grau_zu_werden()
    {
        var z = WbSchnellaktionen.Rechnen([Bild()], MitElementen(1), 0, false,
                                          texterkennungVerfuegbar: false);
        Assert.False(z.TexterkennungSichtbar);
        Assert.False(z.Texterkennung);
    }

    [Fact]
    public void Ein_ausgewaehltes_Bild_ist_eine_Quelle_fuer_die_Texterkennung()
    {
        var z = WbSchnellaktionen.Rechnen([Bild()], MitElementen(1), 0, false, true);
        Assert.True(z.TexterkennungSichtbar);
        Assert.True(z.Texterkennung);
    }

    [Fact]
    public void Ein_ausgewaehlter_Strich_ist_keine()
    {
        var strich = new StrokeElement { Points = { new WbPoint(0, 0, 0.5f) } };
        var z = WbSchnellaktionen.Rechnen([strich], MitElementen(1), 0, false, true);
        Assert.True(z.TexterkennungSichtbar);
        Assert.False(z.Texterkennung);
    }

    [Fact]
    public void Ohne_Auswahl_zaehlt_der_eingefuegte_Seitenhintergrund()
    {
        // Der Fall „PDF-Seite importiert und gleich erkennen lassen", ohne sie erst
        // anklicken zu müssen.
        var seite = MitElementen(1);
        seite.BackgroundImageId = Guid.NewGuid();

        Assert.True(WbSchnellaktionen.Rechnen([], seite, 0, false, true).Texterkennung);
    }

    [Fact]
    public void Mit_Auswahl_zaehlt_der_Seitenhintergrund_nicht_mehr()
    {
        var seite = MitElementen(1);
        seite.BackgroundImageId = Guid.NewGuid();
        var strich = new StrokeElement { Points = { new WbPoint(0, 0, 0.5f) } };

        Assert.False(WbSchnellaktionen.Rechnen([strich], seite, 0, false, true).Texterkennung);
    }

    // ==================== Wo die Leiste hinkommt ====================

    [Fact]
    public void Am_Zeiger_haengt_sie_mittig_darunter()
    {
        var ecke = WbSchnellaktionen.AmZeiger(new SKPoint(600, 300), Leiste);

        Assert.Equal(600 - Leiste.Width / 2, ecke.X, 3);
        Assert.Equal(300 + WbSchnellaktionen.AbstandZumZeiger, ecke.Y, 3);
    }

    [Fact]
    public void Ueber_der_Auswahl_steht_sie_mittig_darueber()
    {
        var auswahl = new SKRect(400, 300, 700, 500);

        var ecke = WbSchnellaktionen.UeberDerAuswahl(auswahl, Leiste);

        Assert.Equal(auswahl.MidX - Leiste.Width / 2, ecke.X, 3);
        Assert.Equal(300 - Leiste.Height - WbSchnellaktionen.AbstandZurAuswahl, ecke.Y, 3);
    }

    [Fact]
    public void Ist_oben_kein_Platz_rutscht_sie_unter_die_Auswahl()
    {
        var auswahl = new SKRect(400, 2, 700, 200);

        var ecke = WbSchnellaktionen.UeberDerAuswahl(auswahl, Leiste);

        Assert.Equal(200 + WbSchnellaktionen.AbstandZurAuswahl, ecke.Y, 3);
    }

    [Fact]
    public void Die_Leiste_ueber_der_Auswahl_liegt_auf_dem_Drehgriff()
    {
        // **Der Grund, warum der Linux-Kopf das Aufklappen nach einer frischen Auswahl nicht
        // mitbekommt** (§4.51). Der Dreh-Griff hängt RotateArmPx über der Oberkante; die
        // Leiste belegt den Streifen von AbstandZurAuswahl bis Höhe + AbstandZurAuswahl
        // darüber. Bei jeder üblichen Leistenhöhe überschneidet sich das.
        var auswahl = new SKRect(400, 300, 700, 500);
        var ecke = WbSchnellaktionen.UeberDerAuswahl(auswahl, Leiste);

        float griffY = auswahl.Top - WbHandles.RotateArmPx;
        Assert.InRange(griffY, ecke.Y, ecke.Y + Leiste.Height);
    }

    [Fact]
    public void Am_Rand_bleibt_sie_im_Blick()
    {
        var ecke = WbSchnellaktionen.ImBlick(new SKPoint(1150, 790), Leiste, Flaeche);

        Assert.Equal(Flaeche.Width - Leiste.Width, ecke.X, 3);
        Assert.Equal(Flaeche.Height - Leiste.Height, ecke.Y, 3);
    }

    [Fact]
    public void Auf_einer_zu_schmalen_Flaeche_klebt_sie_am_Rand_statt_hinauszurutschen()
    {
        var eng = new SKSize(100, 30);

        var ecke = WbSchnellaktionen.ImBlick(new SKPoint(-50, -50), Leiste, eng);

        Assert.Equal(0f, ecke.X);
        Assert.Equal(0f, ecke.Y);
    }

    // ==================== Die Ordnung der Leiste ====================

    [Fact]
    public void Jedes_Werkzeug_steht_genau_einmal_in_der_Reihenfolge()
    {
        var stehen = WbLeiste.Reihenfolge.Where(t => t.HasValue).Select(t => t!.Value).ToList();

        Assert.Equal(stehen.Count, stehen.Distinct().Count());

        // Der Sticker ist ein Werkzeug, der Radierer auch — keines darf fehlen.
        foreach (var t in Enum.GetValues<ToolType>())
            Assert.Contains(t, stehen);
    }

    [Fact]
    public void Die_Gruppen_stehen_zusammen_und_in_der_Reihenfolge_der_Leiste()
    {
        var stehen = WbLeiste.Reihenfolge.Where(t => t.HasValue).Select(t => t!.Value).ToList();

        foreach (var gruppe in new[] { WbLeiste.Stifte, WbLeiste.Auswahlwerkzeuge })
        {
            var plaetze = gruppe.Select(t => stehen.IndexOf(t)).ToList();
            Assert.DoesNotContain(-1, plaetze);
            // lückenlos und in derselben Folge
            for (int i = 1; i < plaetze.Count; i++)
                Assert.Equal(plaetze[i - 1] + 1, plaetze[i]);
        }
    }

    [Fact]
    public void Eine_Gruppe_ist_genau_dann_auf_wenn_ihr_Werkzeug_aktiv_ist()
    {
        Assert.True(WbLeiste.IstAufgeklappt(WbLeiste.Gruppe.Stifte, ToolType.Highlighter));
        Assert.False(WbLeiste.IstAufgeklappt(WbLeiste.Gruppe.Stifte, ToolType.Lasso));
        Assert.True(WbLeiste.IstAufgeklappt(WbLeiste.Gruppe.Auswahl, ToolType.Move));
        Assert.True(WbLeiste.IstAufgeklappt(WbLeiste.Gruppe.Formen, ToolType.Shape));
    }

    [Fact]
    public void Ein_Werkzeug_ohne_Gruppe_klappt_nichts_auf()
    {
        Assert.Equal(WbLeiste.Gruppe.Keine, WbLeiste.GruppeVon(ToolType.Pan));
        Assert.False(WbLeiste.IstAufgeklappt(WbLeiste.Gruppe.Keine, ToolType.Pan));
    }

    [Fact]
    public void Eingeklappt_bleibt_der_zuletzt_benutzte_stehen()
    {
        Assert.True(WbLeiste.IstSichtbar(ToolType.Highlighter, ToolType.Highlighter, aufgeklappt: false));
        Assert.False(WbLeiste.IstSichtbar(ToolType.Pen, ToolType.Highlighter, aufgeklappt: false));
        Assert.True(WbLeiste.IstSichtbar(ToolType.Pen, ToolType.Highlighter, aufgeklappt: true));
    }

    [Fact]
    public void Die_Kuerzel_sind_eindeutig_und_treffen_die_Leiste()
    {
        Assert.Equal(WbLeiste.Kuerzel.Count, WbLeiste.Kuerzel.Values.Distinct().Count());

        // D und R gehören Geodreieck und Lineal — sie schalten kein Werkzeug um.
        Assert.False(WbLeiste.Kuerzel.ContainsKey('D'));
        Assert.False(WbLeiste.Kuerzel.ContainsKey('R'));

        var stehen = WbLeiste.Reihenfolge.Where(t => t.HasValue).Select(t => t!.Value).ToList();
        foreach (var t in WbLeiste.Kuerzel.Values) Assert.Contains(t, stehen);
    }

    // ==================== Hilfen ====================

    private static WbPage MitElementen(int anzahl)
    {
        var seite = new WbPage();
        for (int i = 0; i < anzahl; i++)
            seite.Elements.Add(new StrokeElement { Points = { new WbPoint(i, i, 0.5f) } });
        return seite;
    }

    private static ImageElement Bild() =>
        new() { X = 0, Y = 0, Width = 10, Height = 10, Data = [1] };
}
