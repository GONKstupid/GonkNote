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
}
