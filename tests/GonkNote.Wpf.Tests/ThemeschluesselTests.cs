using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using GonkNote.Core.Theming;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Wächter über die Theme-Schlüssel des WPF-Kopfs.
///
/// <para>
/// <b>Hier stand bis zum 2026-09-10 <c>FarbtabelleTests</c></b> — ein Vergleich der
/// Farbtabelle in Core mit <c>Themes/Light.xaml</c> und <c>Dark.xaml</c>, Zeile für Zeile,
/// <em>weil zwei Wahrheiten auseinanderlaufen, sobald es niemand nachhält</em> (§4.13). Mit
/// den eigenen Designs baut <c>WpfThemeHost</c> sein Wörterbuch aus derselben Tabelle wie
/// der Linux-Kopf; die zwei XAML-Dateien sind gelöscht, und damit ist die Frage, die dieser
/// Wächter stellte, <b>beantwortet und nicht abgeschafft</b>.
/// </para>
/// <para>
/// <b>Was an ihre Stelle tritt, ist die neue Gefahr.</b> Solange die Farben in einer
/// XAML-Datei standen, war ein vertippter Schlüssel ein Ladefehler beim Start. Jetzt
/// entsteht das Wörterbuch im Code, und ein <c>DynamicResource Brush.Boarder</c> wirkt
/// <b>still gar nicht</b>: die Fläche bleibt in ihrer WPF-Vorgabe stehen, niemand bekommt
/// einen Fehler. Diese Tests lesen deshalb den Quelltext des Kopfs und prüfen jeden
/// gefundenen Theme-Schlüssel gegen <see cref="ThemeColor"/>.
/// </para>
/// <para>
/// Absichtlich <b>ohne</b> WPF geladen — für die Frage „steht dieser Name in der Tabelle?"
/// genügt ein Textleser; ein <c>ResourceDictionary</c> bräuchte einen STA-Faden und eine
/// <c>Application</c>.
/// </para>
/// </summary>
public class ThemeschluesselTests
{
    /// <summary><c>{DynamicResource Brush.Text}</c> und <c>{StaticResource Color.PageBg}</c>.</summary>
    private static readonly Regex InXaml =
        new(@"\{\s*(?<art>Dynamic|Static)Resource\s+(?<praefix>Brush|Color)\.(?<name>\w+)\s*\}",
            RegexOptions.Compiled);

    /// <summary><c>FindResource("Brush.Accent")</c> und jede andere Zeichenkette dieser Form.</summary>
    private static readonly Regex InCode =
        new("\"(?<praefix>Brush|Color)\\.(?<name>\\w+)\"", RegexOptions.Compiled);

    [Fact]
    public void Jeder_Theme_Schluessel_in_der_XAML_steht_in_der_Farbtabelle()
    {
        var bekannt = Enum.GetValues<ThemeColor>().Select(c => c.ToString()).ToHashSet(StringComparer.Ordinal);

        foreach (string datei in Quelldateien("*.xaml"))
        foreach (Match treffer in InXaml.Matches(File.ReadAllText(datei)))
        {
            string name = treffer.Groups["name"].Value;
            Assert.True(bekannt.Contains(name),
                $"„{treffer.Value}\" in {Kurz(datei)} zeigt auf keine Farbe der Tabelle. " +
                $"Seit dem 2026-09-10 baut WpfThemeHost das Wörterbuch aus " +
                $"GonkNote.Core.Theming.Themes — ein Schlüssel, den es dort nicht gibt, " +
                $"wirkt still gar nicht.");
        }
    }

    [Fact]
    public void Jeder_Theme_Schluessel_im_Code_steht_in_der_Farbtabelle()
    {
        var bekannt = Enum.GetValues<ThemeColor>().Select(c => c.ToString()).ToHashSet(StringComparer.Ordinal);

        foreach (string datei in Quelldateien("*.cs"))
        foreach (Match treffer in InCode.Matches(File.ReadAllText(datei)))
        {
            string name = treffer.Groups["name"].Value;

            // Der Setzer selbst baut die Schlüssel zusammen ($"Brush.{name}") und nennt
            // keinen einzelnen — er kann hier nicht danebengreifen.
            if (Path.GetFileName(datei) == "WpfThemeHost.cs") continue;

            Assert.True(bekannt.Contains(name),
                $"„{treffer.Value}\" in {Kurz(datei)} zeigt auf keine Farbe der Tabelle.");
        }
    }

    /// <summary>
    /// <b>Kein <c>StaticResource</c> auf einen Theme-Schlüssel.</b> Das ist keine Stilfrage:
    /// Stelle [0] der <c>MergedDictionaries</c> ist beim Auswerten der XAML <b>leer</b> — sie
    /// wird erst im Start gefüllt (App.xaml.cs) und bei jedem Designwechsel ersetzt. Ein
    /// <c>StaticResource</c> fragt genau einmal und behielte, was er beim ersten Mal fand:
    /// nichts. Bis zum 2026-09-10 stand dort <c>Light.xaml</c>, und deshalb ging es gut.
    /// </summary>
    [Fact]
    public void Theme_Farben_werden_nie_als_StaticResource_geholt()
    {
        foreach (string datei in Quelldateien("*.xaml"))
        foreach (Match treffer in InXaml.Matches(File.ReadAllText(datei)))
        {
            Assert.True(treffer.Groups["art"].Value == "Dynamic",
                $"„{treffer.Value}\" in {Kurz(datei)} ist ein StaticResource auf eine " +
                $"Theme-Farbe. Die Farben stehen zur Auswertungszeit der XAML noch nicht im " +
                $"Wörterbuch und wechseln danach mit jedem Design — DynamicResource nehmen.");
        }
    }

    private static IEnumerable<string> Quelldateien(string muster) =>
        Directory.EnumerateFiles(Kopfordner, muster, SearchOption.AllDirectories)
                 .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                          && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static string Kurz(string pfad) => Path.GetRelativePath(Kopfordner, pfad);

    /// <summary>Der Quellordner des WPF-Kopfs, aus dem Projektordner des Testprojekts abgeleitet.</summary>
    private static string Kopfordner =>
        Path.GetFullPath(Path.Combine(Projektordner, "..", "..", "src", "GonkNote.Wpf"));

    private static string Projektordner =>
        typeof(ThemeschluesselTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjektOrdner")?.Value
        ?? throw new InvalidOperationException(
            "Assembly-Metadatum „ProjektOrdner\" fehlt — siehe GonkNote.Wpf.Tests.csproj.");
}
