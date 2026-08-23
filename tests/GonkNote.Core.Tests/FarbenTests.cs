using GonkNote.Core.Platform;
using GonkNote.Core.Theming;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die Farbtabelle aus Phase 3 und ihr Parser. Beides läuft ohne Oberfläche und darum auch
/// unter Linux — der Avalonia-Kopf baut seine Pinsel daraus.
/// </summary>
public class FarbenTests
{
    // ---------- HexColor ----------

    [Theory]
    [InlineData("#2563EB", 0xFF, 0x25, 0x63, 0xEB)]
    [InlineData("2563eb", 0xFF, 0x25, 0x63, 0xEB)]     // ohne Doppelkreuz, klein geschrieben
    [InlineData("  #2563EB  ", 0xFF, 0x25, 0x63, 0xEB)] // Leerraum wie aus einer Datei
    [InlineData("#8022C55E", 0x80, 0x22, 0xC5, 0x5E)]   // mit Alpha-Anteil
    [InlineData("#F0A", 0xFF, 0xFF, 0x00, 0xAA)]        // Kurzform, jede Ziffer verdoppelt
    public void Farben_werden_in_allen_ueblichen_Schreibweisen_gelesen(
        string text, int a, int r, int g, int b)
    {
        Assert.True(HexColor.TryParse(text, out var farbe));
        Assert.Equal(new HexColor((byte)a, (byte)r, (byte)g, (byte)b), farbe);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rot")]
    [InlineData("#12345")]        // fünf Stellen gibt es nicht
    [InlineData("#GGHHII")]
    public void Unbrauchbares_ergibt_keine_Farbe_und_wirft_nicht(string? text)
    {
        // false statt einer Ausnahme: der häufigste Aufrufer ist ein Konverter an einer
        // Bindung. Eine unbrauchbare Farbe darf dort einen Rückfall auslösen, nicht das
        // Zeichnen der ganzen Seite abbrechen.
        Assert.False(HexColor.TryParse(text, out _));
        Assert.Equal(HexColor.Black, HexColor.Parse(text, HexColor.Black));
    }

    [Theory]
    [InlineData("#2563EB")]
    [InlineData("#8022C55E")]
    public void Aus_Text_und_zurueck_ergibt_denselben_Text(string text)
    {
        Assert.True(HexColor.TryParse(text, out var farbe));
        Assert.Equal(text, farbe.ToString());
    }

    // ---------- Die mitgelieferten Tabellen ----------

    [Fact]
    public void Beide_Tabellen_sind_vollstaendig()
    {
        foreach (var tabelle in new[] { Themes.Light, Themes.Dark })
        {
            Assert.Equal(ThemeDefinition.ColorCount, tabelle.Entries.Count());
            // Eine Tabelle mit einer nicht gesetzten Farbe hätte dort durchsichtiges
            // Schwarz — das sähe im Programm nach einem Zeichenfehler aus, nicht nach
            // einer fehlenden Angabe.
            Assert.All(tabelle.Entries, e => Assert.NotEqual(default, e.Value));
        }
    }

    [Fact]
    public void Hell_und_Dunkel_sind_wirklich_verschieden()
    {
        // Ohne diesen Test wäre ein versehentlich zweimal eingefügter Farbblock nicht zu
        // sehen, solange man nicht umschaltet.
        Assert.NotEqual(Themes.Light[ThemeColor.WindowBg], Themes.Dark[ThemeColor.WindowBg]);
        Assert.NotEqual(Themes.Light[ThemeColor.Text], Themes.Dark[ThemeColor.Text]);
        Assert.NotEqual(Themes.Light[ThemeColor.PageBg], Themes.Dark[ThemeColor.PageBg]);
    }

    [Fact]
    public void Die_Variante_sagt_hell_oder_dunkel()
    {
        Assert.Equal(AppTheme.Light, Themes.Light.Variant);
        Assert.Equal(AppTheme.Dark, Themes.Dark.Variant);
        Assert.Same(Themes.Dark, Themes.ForVariant(AppTheme.Dark));
        Assert.Same(Themes.Light, Themes.ForVariant(AppTheme.Light));
    }

    [Fact]
    public void Eine_unvollstaendige_Tabelle_wird_still_ergaenzt()
    {
        // Das ist der Weg, auf dem später eine Theme-Datei mit drei Farben genügt: was
        // dort steht, gewinnt — der Rest kommt aus Hell bzw. Dunkel. Eine Datei
        // abzulehnen, weil ihr siebzehn Angaben fehlen, wäre bei einer Datei, die Nutzer
        // von Hand schreiben, die falsche Strenge (HANDOFF §6).
        var eigen = Themes.Light.Over("Nur Akzent", AppTheme.Light,
            [(ThemeColor.Accent, HexColor.Parse("#FF00FF", HexColor.Black))]);

        Assert.Equal("Nur Akzent", eigen.Name);
        Assert.Equal(HexColor.Parse("#FF00FF", HexColor.Black), eigen[ThemeColor.Accent]);
        Assert.Equal(Themes.Light[ThemeColor.WindowBg], eigen[ThemeColor.WindowBg]);
        Assert.Equal(Themes.Light[ThemeColor.PageBg], eigen[ThemeColor.PageBg]);

        // Die Vorlage bleibt unberührt — sonst färbte ein geladenes Theme die
        // mitgelieferte Tabelle für den Rest der Sitzung um.
        Assert.NotEqual(eigen[ThemeColor.Accent], Themes.Light[ThemeColor.Accent]);
    }

    [Fact]
    public void Eine_Tabelle_mit_falscher_Laenge_wird_nicht_angenommen()
    {
        // Bei den **mitgelieferten** Tabellen ist ein fehlender Eintrag ein
        // Programmierfehler und soll beim ersten Zugriff auffallen — nicht still eine
        // schwarze Fläche ergeben.
        Assert.Throws<ArgumentException>(() =>
            ThemeDefinition.FromHex("Zu kurz", AppTheme.Light, "#FFFFFF", "#000000"));
    }

    [Fact]
    public void Eine_Tabelle_mit_einer_unlesbaren_Farbe_wird_nicht_angenommen()
    {
        var werte = Enumerable.Repeat("#FFFFFF", ThemeDefinition.ColorCount).ToArray();
        werte[(int)ThemeColor.Accent] = "türkis";

        var ex = Assert.Throws<ArgumentException>(() =>
            ThemeDefinition.FromHex("Kaputt", AppTheme.Light, werte));
        // Die Meldung muss sagen, **welche** Farbe — bei zwanzig gleich aussehenden
        // Zeichenketten ist das der Unterschied zwischen Suchen und Finden.
        Assert.Contains("Accent", ex.Message);
    }

    // ---------- Farbton, Sättigung, Helligkeit (Phase 4.5, für den Farbwähler) ----------

    [Theory]
    [InlineData("#FF0000", 0, 1, 1)]        // Rot
    [InlineData("#00FF00", 120, 1, 1)]      // Grün
    [InlineData("#0000FF", 240, 1, 1)]      // Blau
    [InlineData("#FFFF00", 60, 1, 1)]       // Gelb
    [InlineData("#00FFFF", 180, 1, 1)]      // Cyan
    [InlineData("#FF00FF", 300, 1, 1)]      // Magenta
    [InlineData("#FFFFFF", 0, 0, 1)]        // Weiß — Farbton unbestimmt, gemeldet als 0
    [InlineData("#000000", 0, 0, 0)]        // Schwarz
    [InlineData("#808080", 0, 0, 0.50196)]  // Grau
    public void Die_Grundfarben_zerfallen_richtig(string hex, double h, double s, double v)
    {
        var (ist_h, ist_s, ist_v) = HexColor.Parse(hex, HexColor.Black).ToHsv();
        Assert.Equal(h, ist_h, 3);
        Assert.Equal(s, ist_s, 3);
        Assert.Equal(v, ist_v, 3);
    }

    /// <summary>
    /// <b>Der Wächter, auf den es ankommt.</b> Ein Farbwähler zerlegt beim Öffnen und setzt
    /// beim Ziehen wieder zusammen — kommt dabei nicht dieselbe Farbe heraus, wandert sie bei
    /// jedem Öffnen ein Stück, und niemand sieht es an einer einzelnen Runde.
    /// <para>Geprüft über alle 4.096 Farben des 16er-Rasters, nicht an drei Beispielen.</para>
    /// </summary>
    [Fact]
    public void Zerlegen_und_zusammensetzen_ergibt_dieselbe_Farbe()
    {
        for (int r = 0; r < 256; r += 17)
            for (int g = 0; g < 256; g += 17)
                for (int b = 0; b < 256; b += 17)
                {
                    var vorher = new HexColor(0xFF, (byte)r, (byte)g, (byte)b);
                    var (h, s, v) = vorher.ToHsv();
                    Assert.Equal(vorher, HexColor.FromHsv(h, s, v));
                }
    }

    [Fact]
    public void Der_Alphaanteil_ueberlebt_die_Zerlegung_nicht_und_wird_mitgegeben()
    {
        var mit = new HexColor(0x80, 0x25, 0x63, 0xEB);
        var (h, s, v) = mit.ToHsv();

        // ToHsv sagt nichts über Alpha — FromHsv ohne Angabe liefert deckend.
        Assert.Equal(0xFF, HexColor.FromHsv(h, s, v).A);
        Assert.Equal(mit, HexColor.FromHsv(h, s, v, 0x80));
    }

    /// <summary>
    /// Der Aufrufer ist ein Mauszeiger auf einer Fläche, und der darf über den Rand hinaus.
    /// Zurechtgestutzt statt abgelehnt — eine Ausnahme mitten im Ziehen wäre unbrauchbar.
    /// </summary>
    [Theory]
    [InlineData(-90, 0.5, 0.5)]
    [InlineData(450, 0.5, 0.5)]
    [InlineData(0, -1, 0.5)]
    [InlineData(0, 2, 0.5)]
    [InlineData(0, 0.5, -1)]
    [InlineData(0, 0.5, 2)]
    public void Werte_ausserhalb_des_Bereichs_werden_zurechtgestutzt(double h, double s, double v)
    {
        var farbe = HexColor.FromHsv(h, s, v);   // wirft nicht
        Assert.Equal(0xFF, farbe.A);
    }

    [Fact]
    public void Ein_negativer_und_ein_ueberdrehter_Winkel_treffen_dieselbe_Farbe()
    {
        Assert.Equal(HexColor.FromHsv(30, 1, 1), HexColor.FromHsv(390, 1, 1));
        Assert.Equal(HexColor.FromHsv(30, 1, 1), HexColor.FromHsv(-330, 1, 1));
    }

    /// <summary>Bei 360° darf nicht Magenta herauskommen, sondern Rot — der Zweig muss umlaufen.</summary>
    [Fact]
    public void Der_volle_Kreis_endet_wieder_bei_Rot()
    {
        Assert.Equal(HexColor.FromHsv(0, 1, 1), HexColor.FromHsv(360, 1, 1));
        Assert.Equal(new HexColor(0xFF, 0xFF, 0, 0), HexColor.FromHsv(360, 1, 1));
    }

    [Fact]
    public void Der_Alphaanteil_laesst_sich_einzeln_wechseln()
    {
        var c = HexColor.Parse("#2563EB", HexColor.Black);
        Assert.Equal(new HexColor(0x80, 0x25, 0x63, 0xEB), c.WithAlpha(0x80));
        Assert.Equal("#802563EB", c.WithAlpha(0x80).ToString());
    }
}
