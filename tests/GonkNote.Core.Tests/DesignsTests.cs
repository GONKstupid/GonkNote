using System.IO;
using GonkNote.Core.Platform;
using GonkNote.Core.Theming;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die eigenen Designs — <see cref="ThemeFile"/> und <see cref="ThemeLibrary"/>
/// (Nutzerwunsch vom 2026-08-02, gebaut am 2026-09-10, HANDOFF §6).
///
/// <para>
/// <b>Was hier wirklich geprüft wird, ist eine Entscheidung und keine Rechnung:</b> eine
/// unvollständige Datei wird <b>still ergänzt</b> (Nutzer, 2026-09-10) — eine Datei mit drei
/// Farben ist ein gültiges Design. Das ist die Sorte Verhalten, die niemandem auffällt,
/// wenn sie eines Tages umkippt: Ein Design, dem plötzlich siebzehn Angaben fehlen, sähe aus
/// wie ein Design, das jemand so gemeint hat.
/// </para>
/// <para>
/// Gemessen wird gegen echte Dateien in einem Wegwerf-Ordner, nicht gegen eine Attrappe —
/// dieselbe Begründung wie bei <see cref="CoverSammlungTests"/>: was hier zählt, ist genau
/// das Zusammenspiel aus Dateisystem, Namensvergabe und Rückfall.
/// </para>
/// </summary>
public sealed class DesignsTests : IDisposable
{
    private readonly TempWorkspace _arbeit = new("designs");
    private readonly IAppPaths _vorher;

    private sealed record Wegwerfpfade(string AppFolder, string DataFolder) : IAppPaths;

    /// <summary><b>Nie in den echten Ordnern testen</b> (§7).</summary>
    public DesignsTests()
    {
        _vorher = AppPaths.Current;
        AppPaths.Current = new Wegwerfpfade(
            Path.Combine(_arbeit.Root, "app"), Path.Combine(_arbeit.Root, "daten"));
    }

    public void Dispose()
    {
        AppPaths.Current = _vorher;
        _arbeit.Dispose();
    }

    // ---------- Lesen ----------

    [Fact]
    public void Eine_unvollstaendige_Datei_wird_still_ergaenzt()
    {
        const string json = """
            { "name": "Dreifarbig", "variant": "dark", "colors": { "Accent": "#FF8800" } }
            """;

        Assert.True(ThemeFile.TryParse(json, "ersatz", out var theme, out string? fehler));
        Assert.Null(fehler);
        Assert.NotNull(theme);

        Assert.Equal("Dreifarbig", theme!.Name);
        Assert.Equal(AppTheme.Dark, theme.Variant);

        // Die eine genannte Farbe gilt …
        Assert.Equal(new HexColor(0xFF, 0xFF, 0x88, 0x00), theme[ThemeColor.Accent]);
        // … und die neunzehn ungenannten kommen aus Dunkel, nicht aus Schwarz.
        Assert.Equal(Themes.Dark[ThemeColor.WindowBg], theme[ThemeColor.WindowBg]);
        Assert.Equal(Themes.Dark[ThemeColor.PageBg], theme[ThemeColor.PageBg]);
    }

    [Fact]
    public void Die_Variante_entscheidet_woraus_ergaenzt_wird()
    {
        // Dieselbe Datei, nur die Auskunft hell/dunkel getauscht: derselbe Akzent, aber ein
        // ganz anderes Papier. Das ist der Grund, warum die Variante Pflicht ist und nicht
        // aus der Helligkeit von PageBg geraten wird (HANDOFF §6, Punkt 4).
        const string muster = """{ "variant": "%V%", "colors": { "Accent": "#FF8800" } }""";

        Assert.True(ThemeFile.TryParse(muster.Replace("%V%", "light"), "a", out var hell, out _));
        Assert.True(ThemeFile.TryParse(muster.Replace("%V%", "dark"), "a", out var dunkel, out _));

        Assert.Equal(Themes.Light[ThemeColor.PageBg], hell![ThemeColor.PageBg]);
        Assert.Equal(Themes.Dark[ThemeColor.PageBg], dunkel![ThemeColor.PageBg]);
        Assert.Equal(hell[ThemeColor.Accent], dunkel[ThemeColor.Accent]);
    }

    [Fact]
    public void Ohne_Namen_tritt_der_Dateiname_ein()
    {
        Assert.True(ThemeFile.TryParse("""{ "variant": "light" }""", "mitternacht", out var theme, out _));
        Assert.Equal("mitternacht", theme!.Name);
    }

    [Fact]
    public void Ein_unbekannter_Farbname_wird_uebergangen()
    {
        // Eine Datei aus einer späteren Fassung, die eine einundzwanzigste Farbe kennt, soll
        // hier trotzdem laufen — was wir nicht kennen, geht uns nichts an.
        const string json = """
            { "variant": "light", "colors": { "Accent": "#FF8800", "Regenbogen": "#123456" } }
            """;

        Assert.True(ThemeFile.TryParse(json, "a", out var theme, out string? fehler));
        Assert.Null(fehler);
        Assert.Equal(new HexColor(0xFF, 0xFF, 0x88, 0x00), theme![ThemeColor.Accent]);
    }

    [Theory]
    [InlineData("""{ "variant": "light" """)]                       // abgeschnitten
    [InlineData("kein json")]
    public void Kaputtes_JSON_meldet_und_wirft_nicht(string json)
    {
        Assert.False(ThemeFile.TryParse(json, "a", out var theme, out string? fehler));
        Assert.Null(theme);
        Assert.False(string.IsNullOrWhiteSpace(fehler));
    }

    [Theory]
    [InlineData("""{ "colors": { "Accent": "#FF8800" } }""")]        // Variante fehlt ganz
    [InlineData("""{ "variant": "mitteldunkel" }""")]                // unbekannt
    public void Ohne_brauchbare_Variante_wird_nicht_geraten(string json)
    {
        Assert.False(ThemeFile.TryParse(json, "a", out var theme, out string? fehler));
        Assert.Null(theme);
        Assert.False(string.IsNullOrWhiteSpace(fehler));
    }

    [Fact]
    public void Ein_unlesbarer_Farbwert_wird_gemeldet_und_nicht_still_ersetzt()
    {
        // Der Unterschied zur fehlenden Farbe ist Absicht: „nicht dagewesen" ist eine
        // Aussage des Nutzers, „#GG00ZZ" ein Tippfehler. Still zurückzufallen hieße,
        // jemanden nach einer Farbe suchen zu lassen, die nie ankommt.
        const string json = """{ "variant": "light", "colors": { "Accent": "#GG00ZZ" } }""";

        Assert.False(ThemeFile.TryParse(json, "a", out _, out string? fehler));
        Assert.Contains("Accent", fehler);
    }

    // ---------- Schreiben ----------

    [Fact]
    public void Geschrieben_und_zurueckgelesen_ergibt_dieselbe_Tabelle()
    {
        string text = ThemeFile.ToJson(Themes.Dark);

        Assert.True(ThemeFile.TryParse(text, "ersatz", out var zurueck, out _));
        Assert.Equal(Themes.Dark.Name, zurueck!.Name);
        Assert.Equal(Themes.Dark.Variant, zurueck.Variant);
        foreach (var (farbe, wert) in Themes.Dark.Entries)
            Assert.Equal(wert, zurueck[farbe]);
    }

    [Fact]
    public void Die_geschriebene_Datei_nennt_alle_zwanzig_Farben()
    {
        // Sie ist die Vorlage, mit der ein eigenes Design anfängt. Eine Vorlage, die nur
        // die Hälfte zeigt, verschweigt genau das, wonach jemand sucht.
        string text = ThemeFile.ToJson(Themes.Light);
        foreach (var farbe in Enum.GetValues<ThemeColor>())
            Assert.Contains(farbe.ToString(), text);
    }

    // ---------- Der Ordner ----------

    [Fact]
    public void Der_Design_Ordner_wird_angelegt_und_liegt_im_Datenordner()
    {
        string ordner = ThemeLibrary.UserFolder;
        Assert.True(Directory.Exists(ordner));
        Assert.Equal(Path.Combine(AppPaths.Current.DataFolder, "Themes"), ordner);
    }

    [Fact]
    public void Eine_kaputte_Datei_steht_mit_in_der_Liste_und_nennt_ihren_Grund()
    {
        Ablegen("gut.json", """{ "name": "Gut", "variant": "dark" }""");
        Ablegen("kaputt.json", "kein json");

        var liste = ThemeLibrary.All();
        Assert.Equal(2, liste.Count);

        var gut = liste.Single(e => e.File == "gut.json");
        Assert.True(gut.IsUsable);
        Assert.Equal("Gut", gut.Name);

        var kaputt = liste.Single(e => e.File == "kaputt.json");
        Assert.False(kaputt.IsUsable);
        Assert.False(string.IsNullOrWhiteSpace(kaputt.Error));
    }

    [Fact]
    public void Die_Liste_ist_nach_dem_Anzeigenamen_sortiert()
    {
        Ablegen("z-datei.json", """{ "name": "Abend", "variant": "dark" }""");
        Ablegen("a-datei.json", """{ "name": "Morgen", "variant": "light" }""");

        Assert.Equal(["Abend", "Morgen"], ThemeLibrary.All().Select(e => e.Name));
    }

    [Fact]
    public void Ein_geladenes_Design_kennt_seine_Datei()
    {
        // Daran hängt, dass dasselbe Bild nach dem Neustart wieder da ist: der Kopf schreibt
        // den Dateinamen in die Einstellungen.
        Ablegen("meins.json", """{ "name": "Meins", "variant": "dark" }""");

        var eintrag = ThemeLibrary.All().Single();
        Assert.Equal("meins.json", eintrag.Theme!.File);
    }

    [Fact]
    public void Eingefuegt_wird_kopiert_und_nie_ueberschrieben()
    {
        string quelle = _arbeit.File("fremd.json");
        File.WriteAllText(quelle, """{ "name": "Fremd", "variant": "light" }""");

        string erste = ThemeLibrary.Insert(quelle);
        string zweite = ThemeLibrary.Insert(quelle);

        Assert.Equal("fremd.json", erste);
        Assert.Equal("fremd-2.json", zweite);
        Assert.True(File.Exists(quelle), "Die Quelldatei muss liegen bleiben — kopiert, nicht verschoben.");
        Assert.Equal(2, ThemeLibrary.All().Count);
    }

    [Fact]
    public void Ein_Name_mit_Pfadanteilen_wird_zu_einem_harmlosen_Dateinamen()
    {
        // Der Name kommt aus einer Datei, die jemand geschrieben hat — „..\\..\\autostart"
        // ist ein gültiger JSON-String und darf nicht zu einem Pfad werden.
        var boese = Themes.Light.Over("../../autostart", AppTheme.Light, []);

        string pfad = ThemeLibrary.WriteTemplate(boese);
        Assert.Equal(ThemeLibrary.UserFolder, Path.GetDirectoryName(pfad));
    }

    [Fact]
    public void Die_Vorlage_landet_im_Ordner_und_ist_sofort_ein_Eintrag()
    {
        string pfad = ThemeLibrary.WriteTemplate(Themes.Dark);

        Assert.True(File.Exists(pfad));
        var eintrag = ThemeLibrary.All().Single();
        Assert.True(eintrag.IsUsable);
        Assert.Equal(AppTheme.Dark, eintrag.Theme!.Variant);

        // Nicht wortgleich „Dunkel": zwei gleich benannte Einträge im selben Menü sind eine
        // Zumutung, und das mitgelieferte Dunkel steht dort ohnehin.
        Assert.NotEqual(Themes.Dark.Name, eintrag.Name);
    }

    // ---------- Der Start ----------

    [Fact]
    public void Beim_Start_gewinnt_die_gespeicherte_Datei()
    {
        Ablegen("meins.json", """{ "name": "Meins", "variant": "dark", "colors": { "Accent": "#FF8800" } }""");

        var theme = ThemeLibrary.AtStartup("light", "meins.json");
        Assert.Equal("Meins", theme.Name);
        Assert.Equal(new HexColor(0xFF, 0xFF, 0x88, 0x00), theme[ThemeColor.Accent]);
    }

    [Theory]
    [InlineData("weg.json")]      // gelöscht
    [InlineData("")]              // nie eines gewählt
    [InlineData(null)]
    public void Ohne_brauchbare_Datei_gilt_die_gespeicherte_Variante(string? datei)
    {
        // Eine gelöschte oder zerschriebene Datei darf den Start nicht kosten. Was der
        // Nutzer dann sieht, ist sein zuletzt gewähltes Hell oder Dunkel.
        Assert.Equal(Themes.Dark.Name, ThemeLibrary.AtStartup("dark", datei).Name);
        Assert.Equal(Themes.Light.Name, ThemeLibrary.AtStartup("light", datei).Name);
    }

    [Fact]
    public void Eine_kaputte_gespeicherte_Datei_kostet_den_Start_nicht()
    {
        Ablegen("kaputt.json", "kein json");
        Assert.Equal(Themes.Dark.Name, ThemeLibrary.AtStartup("dark", "kaputt.json").Name);
    }

    private static void Ablegen(string datei, string inhalt) =>
        File.WriteAllText(Path.Combine(ThemeLibrary.UserFolder, datei), inhalt);
}
