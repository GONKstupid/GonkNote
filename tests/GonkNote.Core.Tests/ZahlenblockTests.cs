using System.Globalization;
using GonkNote.Core.Editing;

namespace GonkNote.Core.Tests;

/// <summary>Wächter für <see cref="WbZahlenblock"/> — die Zifferneingabe an der Werkzeuggröße.</summary>
public class ZahlenblockTests
{
    private const double Hoechst = 20;

    // ==================== Ziffern ====================

    [Fact]
    public void Ziffern_haengen_sich_an()
    {
        string e = "";
        e = WbZahlenblock.Taste(e, "1", Hoechst)!;
        e = WbZahlenblock.Taste(e, "2", Hoechst)!;
        Assert.Equal("12", e);
        Assert.Equal(12d, WbZahlenblock.Wert(e));
    }

    [Fact]
    public void Ueber_dem_Hoechstwert_wird_die_Taste_gar_nicht_erst_angenommen()
    {
        // Sonst stünde in der Anzeige etwas anderes als die eingestellte (geklemmte) Größe.
        Assert.Null(WbZahlenblock.Taste("2", "5", Hoechst));    // 25 > 20
        Assert.Equal("20", WbZahlenblock.Taste("2", "0", Hoechst));
    }

    // ==================== Das Komma ====================

    [Fact]
    public void Ein_Komma_am_Anfang_bekommt_seine_Null()
    {
        Assert.Equal("0,", WbZahlenblock.Taste("", ",", Hoechst));
    }

    [Fact]
    public void Ein_zweites_Komma_wird_abgelehnt()
    {
        Assert.Null(WbZahlenblock.Taste("2,5", ",", Hoechst));
    }

    [Fact]
    public void Nach_dem_Komma_ist_nach_einer_Stelle_Schluss()
    {
        Assert.Equal("2,5", WbZahlenblock.Taste("2,", "5", Hoechst));
        Assert.Null(WbZahlenblock.Taste("2,5", "7", Hoechst));
    }

    [Fact]
    public void Nur_die_leere_Eingabe_ergibt_keine_Zahl()
    {
        // Ein angefangenes „0," ergibt 0 und nicht nichts: das Komma wird abgeschnitten.
        // Der WPF-Kopf behauptete in seinem Kommentar das Gegenteil (V2-83) — folgenlos,
        // weil Schieberwert die 0 ohnehin auf den Mindestwert hochklemmt.
        Assert.Null(WbZahlenblock.Wert(""));
        Assert.Equal(0d, WbZahlenblock.Wert("0,"));
    }

    [Fact]
    public void Das_Komma_wird_unabhaengig_von_der_Systemsprache_gerechnet()
    {
        // Der Fallstrick: auf einem System mit Punkt als Trenner würde "2,5" sonst als 25
        // gelesen — zehnmal zu dick, und niemand sieht dem Code an, warum.
        var vorher = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            Assert.Equal(2.5d, WbZahlenblock.Wert("2,5"));
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal(2.5d, WbZahlenblock.Wert("2,5"));
        }
        finally { CultureInfo.CurrentCulture = vorher; }
    }

    // ==================== Rückschritt und Anzeige ====================

    [Fact]
    public void Der_Rueckschritt_nimmt_ein_Zeichen_weg()
    {
        Assert.Equal("2,", WbZahlenblock.Rueckschritt("2,5"));
        Assert.Equal("", WbZahlenblock.Rueckschritt("2"));
    }

    [Fact]
    public void Auf_leerer_Eingabe_tut_der_Rueckschritt_nichts()
    {
        Assert.Equal("", WbZahlenblock.Rueckschritt(""));
    }

    [Fact]
    public void Die_leere_Eingabe_zeigt_eine_Null()
    {
        Assert.Equal("0", WbZahlenblock.Anzeige(""));
        Assert.Equal("2,5", WbZahlenblock.Anzeige("2,5"));
    }

    // ==================== Der Weg auf den Schieber ====================

    [Fact]
    public void Unter_dem_Mindestwert_wird_geklemmt()
    {
        Assert.Equal(1d, WbZahlenblock.Schieberwert("0,5", mindestwert: 1));
        Assert.Equal(4d, WbZahlenblock.Schieberwert("4", mindestwert: 1));
    }

    [Fact]
    public void Ohne_Zahl_geht_nichts_auf_den_Schieber()
    {
        Assert.Null(WbZahlenblock.Schieberwert("", 1));
    }

    [Fact]
    public void Eine_getippte_Null_landet_auf_dem_Mindestwert()
    {
        Assert.Equal(1d, WbZahlenblock.Schieberwert("0", mindestwert: 1));
        Assert.Equal(1d, WbZahlenblock.Schieberwert("0,", mindestwert: 1));
    }

    // ==================== Langdruck ====================

    [Fact]
    public void Kleine_Bewegungen_sind_noch_ein_Druck()
    {
        Assert.False(WbZahlenblock.IstZiehen(100, 100, 105, 103));
    }

    [Fact]
    public void Ab_der_Schwelle_ist_es_ein_Ziehen()
    {
        Assert.True(WbZahlenblock.IstZiehen(100, 100, 100 + WbZahlenblock.Spielraum + 1, 100));
        Assert.True(WbZahlenblock.IstZiehen(100, 100, 100, 100 - WbZahlenblock.Spielraum - 1));
    }
}
