using System.Reflection;
using System.Text.RegularExpressions;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die beiden Sprachtabellen — <b>Wächter gegen doppelte Schlüssel und fehlende
/// Übersetzungen</b> (HANDOFF §4.40).
///
/// <para>
/// <b>Der Anlass ist ein Fehler, der beinahe durchgegangen wäre.</b> Beim Anlegen der
/// Textfarben sind Schlüssel entstanden, die es schon gab — <c>Color.Auto</c>, <c>Color.Red</c>
/// und fünf weitere, vergeben an die Ordnerfarben und die Tinte der Zeichenfläche. **Ein
/// Wörterbuch-Initialisierer mit Indexer-Schreibweise wirft dabei nicht**, er überschreibt: Der
/// Kurzhinweis der Zeichenfläche wäre still von „Standard (Schwarz auf hellen, Weiß auf dunklen
/// Seiten)" zu „Automatisch" geworden. Kein Compilerfehler, kein roter Test, und aufgefallen
/// wäre es erst jemandem, der die Zeichenfläche benutzt.
/// </para>
/// <para>
/// <b>Deshalb liest dieser Wächter den Quelltext</b> und nicht das fertige Wörterbuch — im
/// Wörterbuch ist die Doppelung ja bereits aufgelöst. Dasselbe Muster wie die drei
/// Ikonen-Wächter im WPF-Testprojekt (§4.31) und aus demselben Grund: Was der Compiler nicht
/// sieht und die Laufzeit stillschweigend erledigt, kann nur der Text verraten.
/// </para>
/// </summary>
public sealed class SprachtabellenTests
{
    /// <summary>Findet jede Zeile der Form <c>["Schlüssel"] =</c>.</summary>
    private static readonly Regex Schluesselzeile =
        new(@"^\s*\[""(?<key>[^""]+)""\]\s*=", RegexOptions.Multiline);

    [Theory]
    [InlineData("LocGerman.cs")]
    [InlineData("LocEnglish.cs")]
    public void Kein_Schluessel_steht_zweimal_in_derselben_Tabelle(string datei)
    {
        var doppelte = Schluessel(datei)
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} ({g.Count()}×)")
            .ToList();

        Assert.True(
            doppelte.Count == 0,
            $"In {datei} steht ein Schlüssel mehrfach — der spätere überschreibt den früheren " +
            $"still: {string.Join(", ", doppelte)}");
    }

    /// <summary>
    /// <b>Was auf Deutsch steht, muss auch auf Englisch stehen.</b>
    ///
    /// <para>
    /// <c>Loc</c> fällt bei einem fehlenden Schlüssel auf Deutsch zurück (Dauerregel 1) — das
    /// ist gut gemeint und macht die Lücke unsichtbar: Im englischen Programm steht dann ein
    /// deutscher Satz, und niemand merkt es, der die App auf Deutsch benutzt. **Genau davor
    /// warnt Dauerregel 1**, und genau das prüft dieser Wächter.
    /// </para>
    /// </summary>
    [Fact]
    public void Jeder_deutsche_Schluessel_hat_ein_englisches_Gegenstueck()
    {
        var deutsch = Schluessel("LocGerman.cs").ToHashSet();
        var englisch = Schluessel("LocEnglish.cs").ToHashSet();

        var fehlend = deutsch.Except(englisch).OrderBy(k => k).ToList();

        Assert.True(
            fehlend.Count == 0,
            "Diese Schlüssel fehlen in LocEnglish.cs — im englischen Programm erschiene dafür " +
            $"der deutsche Text (Dauerregel 1): {string.Join(", ", fehlend)}");
    }

    /// <summary>
    /// Und andersherum: Ein Schlüssel, den es nur auf Englisch gibt, ist einer, den niemand
    /// abruft — die deutsche Tabelle ist die Vorlage.
    /// </summary>
    [Fact]
    public void Kein_englischer_Schluessel_steht_allein()
    {
        var deutsch = Schluessel("LocGerman.cs").ToHashSet();
        var englisch = Schluessel("LocEnglish.cs").ToHashSet();

        var ueberzaehlig = englisch.Except(deutsch).OrderBy(k => k).ToList();

        Assert.True(
            ueberzaehlig.Count == 0,
            "Diese Schlüssel stehen nur in LocEnglish.cs und werden nie abgerufen — die " +
            $"deutsche Tabelle ist die Vorlage: {string.Join(", ", ueberzaehlig)}");
    }

    // ==================== Quelltext lesen ====================

    private static List<string> Schluessel(string datei)
    {
        string pfad = Path.Combine(Quellordner, datei);
        Assert.True(File.Exists(pfad), $"Nicht gefunden: {pfad}");

        return Schluesselzeile.Matches(File.ReadAllText(pfad))
            .Select(m => m.Groups["key"].Value)
            .ToList();
    }

    /// <summary>
    /// Der Quellordner der Sprachtabellen. <b>Sie liegen als Quelltext und nicht im
    /// Ausgabeordner</b> — sie sind einkompiliert, nicht kopiert.
    /// </summary>
    private static string Quellordner =>
        Path.GetFullPath(Path.Combine(
            Projektordner, "..", "..", "src", "GonkNote.Core", "Localization"));

    private static string Projektordner =>
        typeof(SprachtabellenTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjektOrdner")?.Value
        ?? throw new InvalidOperationException(
            "Assembly-Metadatum „ProjektOrdner\" fehlt — siehe GonkNote.Core.Tests.csproj.");
}
