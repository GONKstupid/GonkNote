using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using GonkNote.Core.Theming;

namespace GonkNote.Core.Tests;

/// <summary>
/// Wächter über die Theme-Schlüssel des Linux-Kopfs — das Gegenstück zu
/// <c>ThemeschluesselTests</c> im WPF-Testprojekt, das dieselbe Frage für den Windows-Kopf
/// stellt.
///
/// <para>
/// <b>Die Gefahr, gegen die das steht:</b> Beide Köpfe bauen ihr Ressourcen-Wörterbuch im
/// Code aus <see cref="Themes"/>, in einer Schleife über die zwanzig Farben. Ein
/// <c>{DynamicResource Brush.Boarder}</c> in der XAML wirkt deshalb <b>still gar nicht</b> —
/// die Fläche bleibt in der Vorgabe des Rahmenwerks stehen, und niemand bekommt einen
/// Fehler. Genau diese Sorte Fund hat den Linux-Kopf sechs Runden lang begleitet (§4.86,
/// §4.94, §4.95: „jede Fläche, die noch niemand angefasst hat, trägt Fluents Vorgabe").
/// </para>
/// <para>
/// Gelesen wird der Quelltext, nicht der Oberflächenbaum: für die Frage „steht dieser Name
/// in der Tabelle?" braucht es kein laufendes Avalonia, und der Test läuft damit in jeder CI.
/// </para>
/// </summary>
public class ThemeschluesselTests
{
    private static readonly Regex InXaml =
        new(@"\{\s*(?<art>Dynamic|Static)Resource\s+(?<praefix>Brush|Color)\.(?<name>\w+)\s*\}",
            RegexOptions.Compiled);

    private static readonly Regex InCode =
        new("\"(?<praefix>Brush|Color)\\.(?<name>\\w+)\"", RegexOptions.Compiled);

    private static HashSet<string> Bekannt =>
        Enum.GetValues<ThemeColor>().Select(c => c.ToString()).ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void Jeder_Theme_Schluessel_in_der_XAML_steht_in_der_Farbtabelle()
    {
        var bekannt = Bekannt;

        foreach (string datei in Quelldateien("*.axaml"))
        foreach (Match treffer in InXaml.Matches(File.ReadAllText(datei)))
        {
            string name = treffer.Groups["name"].Value;
            Assert.True(bekannt.Contains(name),
                $"„{treffer.Value}\" in {Kurz(datei)} zeigt auf keine Farbe der Tabelle. " +
                $"AvaloniaThemeHost legt „Brush.X\" und „Color.X\" für genau die zwanzig " +
                $"Namen aus ThemeColor an — alles andere wirkt still gar nicht.");
        }
    }

    [Fact]
    public void Jeder_Theme_Schluessel_im_Code_steht_in_der_Farbtabelle()
    {
        var bekannt = Bekannt;

        foreach (string datei in Quelldateien("*.cs"))
        {
            // Der Setzer selbst baut die Schlüssel zusammen ($"Brush.{name}") und nennt
            // daneben Fluent-Schlüssel, die nicht aus der Tabelle stammen.
            if (Path.GetFileName(datei) == "AvaloniaThemeHost.cs") continue;

            foreach (Match treffer in InCode.Matches(File.ReadAllText(datei)))
            {
                string name = treffer.Groups["name"].Value;
                Assert.True(bekannt.Contains(name),
                    $"„{treffer.Value}\" in {Kurz(datei)} zeigt auf keine Farbe der Tabelle.");
            }
        }
    }

    /// <summary>
    /// <b>Kein <c>StaticResource</c> auf einen Theme-Schlüssel.</b> Stelle [0] der
    /// <c>MergedDictionaries</c> ist beim Auswerten der XAML leer — sie wird im Start gefüllt
    /// und bei jedem Designwechsel <b>ersetzt</b>. Ein <c>StaticResource</c> fragt genau
    /// einmal; er behielte, was er beim ersten Mal fand, und bliebe beim Designwechsel stehen.
    /// </summary>
    [Fact]
    public void Theme_Farben_werden_nie_als_StaticResource_geholt()
    {
        foreach (string datei in Quelldateien("*.axaml"))
        foreach (Match treffer in InXaml.Matches(File.ReadAllText(datei)))
        {
            Assert.True(treffer.Groups["art"].Value == "Dynamic",
                $"„{treffer.Value}\" in {Kurz(datei)} ist ein StaticResource auf eine " +
                $"Theme-Farbe — DynamicResource nehmen, sonst bleibt die Fläche beim " +
                $"Designwechsel stehen.");
        }
    }

    private static IEnumerable<string> Quelldateien(string muster) =>
        Directory.EnumerateFiles(Kopfordner, muster, SearchOption.AllDirectories)
                 .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                          && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static string Kurz(string pfad) => Path.GetRelativePath(Kopfordner, pfad);

    private static string Kopfordner =>
        Path.GetFullPath(Path.Combine(Projektordner, "..", "..", "src", "GonkNote.Avalonia"));

    private static string Projektordner =>
        typeof(ThemeschluesselTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjektOrdner")?.Value
        ?? throw new InvalidOperationException(
            "Assembly-Metadatum „ProjektOrdner\" fehlt — siehe GonkNote.Core.Tests.csproj.");
}
