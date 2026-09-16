using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die Grammatikprüfung (Phase 5.2, <see cref="TdGrammatik"/>).
///
/// <para>
/// <b>Die Hälfte dieser Fälle prüft, dass NICHTS angestrichen wird</b>, und das ist Absicht.
/// Bei einer Rechtschreibprüfung ist ein übersehener Fehler ärgerlich; bei festen
/// Grammatikregeln ist die <b>falsche</b> Anstreichung der teure Fall. Wer unter richtigen
/// Sätzen Wellen sieht, schaltet die Prüfung ab — und verliert die richtigen Befunde gleich
/// mit. Jede Ausnahme in <see cref="TdGrammatik"/> hat hier deshalb ihren Wächter.
/// </para>
/// </summary>
public sealed class GrammatikTests
{
    private const string De = "de-DE";

    private static string[] Stellen(string text) =>
        [.. TdGrammatik.Fehler(text, De).Select(f => text.Substring(f.Start, f.Laenge))];

    private static string[] Vorschlaege(string text) =>
        [.. TdGrammatik.Fehler(text, De).SelectMany(f => f.Vorschlaege ?? [])];

    // ==================== Wortdopplung ====================

    [Fact]
    public void Dasselbe_Wort_zweimal_wird_gefunden()
    {
        // Kein Wörterbuch findet das: beide Wörter sind richtig geschrieben.
        Assert.Equal(["ist ist"], Stellen("Das ist ist ein Haus."));
        Assert.Equal(["ist"], Vorschlaege("Das ist ist ein Haus."));
    }

    [Fact]
    public void Gross_und_klein_zaehlt_als_dieselbe_Dopplung()
    {
        Assert.Equal(["Der der"], Stellen("Der der Mann kam."));
    }

    [Fact]
    public void Ein_Komma_davor_hebt_die_Dopplung_auf()
    {
        // „der Mann, der der Frau half" ist richtig. Ohne diese Ausnahme stünde jeder
        // Relativsatz dieser Bauart angestrichen da — der teuerste Fehlalarm der Regel.
        Assert.Empty(Stellen("Der Mann, der der Frau half, ging."));
        Assert.Empty(Stellen("Das Haus, das das Dach verlor, steht noch."));
    }

    [Fact]
    public void Ueber_ein_Satzzeichen_hinweg_ist_es_keine_Dopplung()
    {
        // Zwei Sätze, nicht ein Tippfehler.
        Assert.Empty(Stellen("Das ist das. Das war es."));
    }

    // ==================== Satzzeichen ====================

    [Fact]
    public void Leerzeichen_vor_dem_Satzzeichen()
    {
        Assert.Equal([" ,"], Stellen("Ja , genau."));
        Assert.Equal([","], Vorschlaege("Ja , genau."));
    }

    [Fact]
    public void Fehlendes_Leerzeichen_nach_dem_Satzzeichen()
    {
        Assert.Equal([".N"], Stellen("Ende.Neuer Satz folgt."));
    }

    [Fact]
    public void Doppeltes_Leerzeichen()
    {
        Assert.Equal(["  "], Stellen("Hier  steht zu viel."));
    }

    [Fact]
    public void Abkuerzungen_bleiben_unangetastet()
    {
        // In „z.B." folgt auf einen Punkt ein Großbuchstabe — genau das Muster, das die Regel
        // sonst als fehlendes Leerzeichen meldet.
        Assert.Empty(Stellen("Das gilt z.B. für Hunde."));
        Assert.Empty(Stellen("Hunde, Katzen u.a. Tiere."));
    }

    [Fact]
    public void Gliederungsnummern_und_Zahlen_bleiben_unangetastet()
    {
        Assert.Empty(Stellen("Wir lesen 1.000 Seiten."));
        Assert.Empty(Stellen("Das war die 1. runde des Spiels."));
    }

    [Fact]
    public void Auslassungspunkte_sind_kein_Befund()
    {
        Assert.Empty(Stellen("Und dann ... kam nichts mehr."));
        Assert.Empty(Stellen("Und dann … kam nichts mehr."));
    }

    // ==================== Satzanfang ====================

    [Fact]
    public void Ein_klein_beginnender_Satz_wird_gefunden()
    {
        Assert.Equal(["dann"], Stellen("Erst kam er. dann ging er."));
        Assert.Equal(["Dann"], Vorschlaege("Erst kam er. dann ging er."));
    }

    [Fact]
    public void Nach_einem_Fragezeichen_gilt_dasselbe()
    {
        Assert.Equal(["ja"], Stellen("Kommst du? ja bitte."));
    }

    // ==================== Was nicht angestrichen werden darf ====================

    [Fact]
    public void Ein_sauberer_Absatz_bleibt_ohne_jeden_Befund()
    {
        Assert.Empty(Stellen(
            "Das ist ein sauberer Absatz. Er hat zwei Sätze, ein Komma und keinen Fehler."));
    }

    [Fact]
    public void Leerer_Text_und_fremde_Sprache_werfen_nicht()
    {
        Assert.Empty(TdGrammatik.Fehler("", De));
        Assert.Empty(TdGrammatik.Fehler((string?)null, De));
        Assert.Empty(TdGrammatik.Fehler("Das ist ist falsch.", "fr-FR"));
        Assert.False(TdGrammatik.Verfuegbar("fr-FR"));
        Assert.True(TdGrammatik.Verfuegbar("de-AT"));
    }

    // ==================== Das Zusammenlegen ====================

    [Fact]
    public void Rechtschreibung_und_Grammatik_kommen_zusammen_heraus()
    {
        const string text = "Das ist ist ein Hausx.";
        var alle = TdPruefung.Fehler(text, De);

        Assert.Contains(alle, f => f.Art is TdBefundArt.Rechtschreibung);
        Assert.Contains(alle, f => f.Art is TdBefundArt.Grammatik);

        // Nach Ort sortiert — die Reihenfolge trägt das Menü.
        Assert.Equal([.. alle.Select(f => f.Start).Order()], [.. alle.Select(f => f.Start)]);
    }

    [Fact]
    public void Abgeschaltete_Grammatik_laesst_die_Rechtschreibung_stehen()
    {
        var nur = TdPruefung.Fehler("Das ist ist ein Hausx.", De, grammatik: false);
        Assert.All(nur, f => Assert.Equal(TdBefundArt.Rechtschreibung, f.Art));
        Assert.NotEmpty(nur);
    }

    [Fact]
    public void Zwei_Befunde_ueber_denselben_Buchstaben_werden_aufgeloest()
    {
        // **Nie zwei Wellen übereinander.** Sie sähen nicht nach doppelter Gründlichkeit aus,
        // sondern nach einem Fehler im Programm.
        var alle = TdPruefung.Fehler("Ein Satz. hausx steht hier.", De);

        foreach (var a in alle)
            Assert.DoesNotContain(alle, b => !b.Equals(a) && b.Start < a.Ende && a.Start < b.Ende);
    }

    [Fact]
    public void Bei_Ueberschneidung_gewinnt_die_Rechtschreibung()
    {
        // „hausx" ist beides: kein Wörterbuchwort UND ein klein beginnender Satz. Die
        // handfestere Auskunft gewinnt.
        var fund = Assert.Single(TdPruefung.Fehler("Ein Satz. hausx steht hier.", De));
        Assert.Equal(TdBefundArt.Rechtschreibung, fund.Art);
    }

    // ==================== LanguageTool, ohne Server ====================

    [Fact]
    public void Ohne_Server_gibt_LanguageTool_nichts_zurueck_und_haelt_nicht_auf()
    {
        TdLanguageTool.Vergessen();

        // Ein Port, auf dem nichts lauscht. **Der Aufruf darf nicht blockieren** — er steht im
        // Zeichenweg, und ein HTTP-Aufruf mitten im Malen wäre ein stehendes Fenster.
        TdLanguageTool.Adresse = new Uri("http://localhost:1");

        var begonnen = System.Diagnostics.Stopwatch.StartNew();
        Assert.Empty(TdLanguageTool.Befunde("Das ist ist ein Satz.", De));
        Assert.True(begonnen.ElapsedMilliseconds < 500, $"blockiert: {begonnen.ElapsedMilliseconds} ms");

        TdLanguageTool.Vergessen();
    }

    [Fact]
    public async Task Ein_Fehlschlag_wird_nicht_gemerkt_sonst_greift_die_Wiederholung_nie()
    {
        // **Der Grund für diesen Wächter.** Der Hinweis im Menü sagt „starte LanguageTool".
        // Würde ein gescheiterter Versuch als leere Liste im Zwischenspeicher landen, träfe
        // jede spätere Abfrage auf diesen Eintrag und käme gar nicht mehr bis zum Server —
        // der Hinweis wäre eine Anweisung, die folgenlos bleibt, bis das Programm neu
        // startet. Genau so war der erste Wurf.
        TdLanguageTool.Vergessen();
        TdLanguageTool.Adresse = new Uri("http://localhost:1");
        TdLanguageTool.Wiederholung = TimeSpan.Zero;

        const string text = "Das ist ist ein Satz.";
        Assert.Empty(TdLanguageTool.Befunde(text, De));

        // Dem Hintergrund Zeit geben, den Fehlschlag festzustellen.
        for (int i = 0; i < 100 && TdLanguageTool.Verfuegbar is null; i++)
            await Task.Delay(20);

        Assert.False(TdLanguageTool.Verfuegbar);

        // Mit abgelaufener Sperre muss derselbe Text erneut angefragt werden — messbar daran,
        // dass die Auskunft „nicht nachgesehen" zurückkommt statt beim Nein zu bleiben.
        Assert.Empty(TdLanguageTool.Befunde(text, De));
        Assert.True(TdLanguageTool.Verfuegbar is null or false,
            "Der Fehlschlag wurde gemerkt — die Wiederholung kommt nie bis zur Abfrage.");

        TdLanguageTool.Wiederholung = TimeSpan.FromSeconds(30);
        TdLanguageTool.Vergessen();
        TdLanguageTool.Adresse = new Uri("http://localhost:8081");
    }

    [Fact]
    public void Nur_der_eigene_Rechner_ist_erlaubt()
    {
        // ⛔ Die Zusage des Programms: „Deine Daten liegen nur auf diesem Rechner." Ein
        // Grammatikdienst bekommt den Text des Dokuments zu sehen — wohin der geht, ist
        // deshalb keine Einstellung.
        Assert.True(TdLanguageTool.Erlaubt(new Uri("http://localhost:8081")));
        Assert.True(TdLanguageTool.Erlaubt(new Uri("http://127.0.0.1:8081")));
        Assert.True(TdLanguageTool.Erlaubt(new Uri("http://[::1]:8081")));

        Assert.False(TdLanguageTool.Erlaubt(new Uri("https://api.languagetool.org")));
        Assert.False(TdLanguageTool.Erlaubt(new Uri("http://192.168.1.5:8081")));
        Assert.False(TdLanguageTool.Erlaubt(new Uri("file:///etc/passwd")));
    }

    [Fact]
    public void Eine_fremde_Adresse_schaltet_die_Pruefung_ab_statt_zu_senden()
    {
        TdLanguageTool.Vergessen();
        TdLanguageTool.Adresse = new Uri("https://api.languagetool.org");

        Assert.Empty(TdLanguageTool.Befunde("Das ist ist ein Satz.", De));
        Assert.False(TdLanguageTool.Verfuegbar);

        TdLanguageTool.Vergessen();
        TdLanguageTool.Adresse = new Uri("http://localhost:8081");
    }
}
