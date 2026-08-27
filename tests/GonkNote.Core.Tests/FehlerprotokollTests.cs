using GonkNote.Core.Services;

namespace GonkNote.Core.Tests;

/// <summary>
/// Wächter für <see cref="Fehlerprotokoll"/> — die Obergrenze, die es bis V2-87 nicht gab
/// (§4.66).
///
/// <para>
/// <b>Der Anlass war kein Testfall, sondern eine Datei:</b> 272 MB im Datenordner des
/// Nutzers, 48.962 Einträge, alle vom selben Nachmittag. Der Fehler dahinter war da längst
/// behoben — <i>was fehlte, war die Grenze.</i>
/// </para>
/// </summary>
public class FehlerprotokollTests : IDisposable
{
    private readonly string _ordner = Path.Combine(
        Path.GetTempPath(), "gonk-protokoll-" + Guid.NewGuid().ToString("N"));

    private string Pfad => Path.Combine(_ordner, "fehler.log");

    public void Dispose()
    {
        try { Directory.Delete(_ordner, recursive: true); } catch { /* Wegwerfordner */ }
    }

    // ==================== Die Grenze ====================

    [Fact]
    public void Unter_der_Grenze_wird_nicht_umgebrochen()
    {
        Assert.False(Fehlerprotokoll.MussUmbrechen(0));
        Assert.False(Fehlerprotokoll.MussUmbrechen(Fehlerprotokoll.Hoechstgroesse - 1));
    }

    [Fact]
    public void Auf_der_Grenze_wird_umgebrochen()
    {
        // Vorher geprüft und nicht nachher: die Datei soll die Grenze nie überschreiten.
        Assert.True(Fehlerprotokoll.MussUmbrechen(Fehlerprotokoll.Hoechstgroesse));
        Assert.True(Fehlerprotokoll.MussUmbrechen(Fehlerprotokoll.Hoechstgroesse + 1));
    }

    // ==================== Das Format ====================

    [Fact]
    public void Ein_Eintrag_traegt_Zeitstempel_und_Ausnahme()
    {
        // Das Format ist absichtlich unverändert gegenüber dem, was die Köpfe vorher
        // schrieben — ein bestehendes Protokoll bleibt lesbar.
        string text = Fehlerprotokoll.Eintrag(
            new DateTime(2026, 8, 12, 18, 39, 57), new InvalidOperationException("Kaputt"));

        Assert.StartsWith("--- 2026-08-12 18:39:57 ---", text);
        Assert.Contains("Kaputt", text);
        Assert.Contains("InvalidOperationException", text);
    }

    // ==================== Das Schreiben ====================

    [Fact]
    public void Der_Ordner_entsteht_von_selbst()
    {
        Fehlerprotokoll.Schreiben(new Exception("erster"), Pfad);

        Assert.True(File.Exists(Pfad));
        Assert.Contains("erster", File.ReadAllText(Pfad));
    }

    [Fact]
    public void Ohne_Ausnahme_entsteht_keine_Datei()
    {
        Fehlerprotokoll.Schreiben(null, Pfad);
        Assert.False(File.Exists(Pfad));
    }

    [Fact]
    public void Mehrere_Eintraege_haengen_aneinander()
    {
        Fehlerprotokoll.Schreiben(new Exception("erster"), Pfad);
        Fehlerprotokoll.Schreiben(new Exception("zweiter"), Pfad);

        string alles = File.ReadAllText(Pfad);
        Assert.Contains("erster", alles);
        Assert.Contains("zweiter", alles);
    }

    [Fact]
    public void Eine_volle_Datei_wird_zurueckgelegt_und_nicht_weggeworfen()
    {
        // Der interessante Fehler ist oft der erste und nicht der letzte — deshalb wandert
        // die volle Fassung nach `.alt` statt in den Papierkorb.
        Directory.CreateDirectory(_ordner);
        File.WriteAllText(Pfad, new string('x', (int)Fehlerprotokoll.Hoechstgroesse));

        Fehlerprotokoll.Schreiben(new Exception("der neue"), Pfad);

        string alt = Pfad + Fehlerprotokoll.AltEndung;
        Assert.True(File.Exists(alt));
        Assert.Equal(Fehlerprotokoll.Hoechstgroesse, new FileInfo(alt).Length);

        string neu = File.ReadAllText(Pfad);
        Assert.Contains("der neue", neu);
        Assert.DoesNotContain("xxx", neu);        // frisch angefangen
    }

    [Fact]
    public void Beim_zweiten_Umbruch_wird_die_alte_Ruecklage_ersetzt()
    {
        // Sonst wäre der Verbrauch wieder offen. Gedeckelt ist er auf das Doppelte.
        Directory.CreateDirectory(_ordner);
        File.WriteAllText(Pfad + Fehlerprotokoll.AltEndung, "uralt");
        File.WriteAllText(Pfad, new string('x', (int)Fehlerprotokoll.Hoechstgroesse));

        Fehlerprotokoll.Schreiben(new Exception("der neue"), Pfad);

        Assert.DoesNotContain("uralt", File.ReadAllText(Pfad + Fehlerprotokoll.AltEndung));
    }

    [Fact]
    public void Der_Verbrauch_bleibt_unter_dem_Doppelten_der_Grenze()
    {
        // Das ist die eigentliche Zusage dieser Klasse — und genau die, die am 2026-08-12
        // gefehlt hat.
        Directory.CreateDirectory(_ordner);
        File.WriteAllText(Pfad, new string('x', (int)Fehlerprotokoll.Hoechstgroesse));

        for (int i = 0; i < 5; i++)
            Fehlerprotokoll.Schreiben(new Exception($"Fehler {i}"), Pfad);

        long gesamt = Directory.GetFiles(_ordner).Sum(f => new FileInfo(f).Length);
        Assert.True(gesamt < 2 * Fehlerprotokoll.Hoechstgroesse,
            $"Protokoll und Ruecklage zusammen {gesamt} Bytes");
    }

    [Fact]
    public void Ein_unbrauchbarer_Pfad_wirft_nicht()
    {
        // Protokollieren darf selbst nie zum Problem werden — ein verlorener Eintrag ist
        // das kleinere Uebel als eine Ausnahme aus dem Fehlerbehandler.
        Fehlerprotokoll.Schreiben(new Exception("egal"), Path.Combine(_ordner, "\0", "x.log"));
    }
}
