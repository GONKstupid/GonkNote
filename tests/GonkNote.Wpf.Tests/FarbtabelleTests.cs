using System.IO;
using System.Reflection;
using System.Xml.Linq;
using GonkNote.Core.Platform;
using GonkNote.Core.Theming;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Wächter über die zwei Fassungen derselben Farben.
/// <para>
/// Seit Phase 3 hält <c>GonkNote.Core.Theming.Themes</c> die Farbtabelle, aus der sich der
/// Avalonia-Kopf bedient. Der WPF-Kopf liest weiterhin seine beiden
/// <c>ResourceDictionary</c>-Dateien — bewusst, denn ihn umzustellen wäre ein Umbau ohne
/// Gegenwert und stünde der Regel „der WPF-Kopf verhält sich unverändert" entgegen.
/// </para>
/// <para>
/// Damit stehen dieselben zwanzig Farben an zwei Stellen, und <b>zwei Wahrheiten laufen
/// auseinander, sobald es niemand nachhält</b>. Genau das tun diese Tests: sie lesen die
/// XAML-Dateien als reines XML und vergleichen sie Zeile für Zeile mit der Tabelle. Wer eine
/// Farbe ändert und die andere Seite vergisst, bekommt einen roten Lauf statt zweier Köpfe,
/// die unterschiedlich aussehen.
/// </para>
/// <para>
/// Absichtlich <b>ohne</b> WPF geladen: ein <c>ResourceDictionary</c> bräuchte einen
/// STA-Faden und eine <c>Application</c>. Für die Frage „steht in der Datei dieselbe Farbe?"
/// genügt ein XML-Leser.
/// </para>
/// </summary>
public class FarbtabelleTests
{
    [Fact]
    public void Helles_Theme_stimmt_mit_Light_xaml_ueberein() =>
        Vergleiche(Themes.Light, "Light.xaml");

    [Fact]
    public void Dunkles_Theme_stimmt_mit_Dark_xaml_ueberein() =>
        Vergleiche(Themes.Dark, "Dark.xaml");

    /// <summary>
    /// Die Gegenrichtung: steht in der XAML-Datei ein Farbschlüssel, den die Tabelle nicht
    /// kennt? Ohne diesen Test fiele eine **einundzwanzigste** Farbe im WPF-Kopf niemandem
    /// auf — sie wäre schlicht nicht Teil eines Themes, und der Avalonia-Kopf hätte sie nie.
    /// </summary>
    [Theory]
    [InlineData("Light.xaml")]
    [InlineData("Dark.xaml")]
    public void Die_XAML_Datei_kennt_keine_Farbe_ausserhalb_der_Tabelle(string datei)
    {
        var bekannt = Enum.GetValues<ThemeColor>().Select(c => c.ToString()).ToHashSet();

        foreach (var (schluessel, _) in Lies(datei))
        {
            // "Brush.WindowBg" bzw. "Color.PageBg" → "WindowBg" / "PageBg"
            int punkt = schluessel.IndexOf('.');
            string name = punkt < 0 ? schluessel : schluessel[(punkt + 1)..];

            Assert.True(bekannt.Contains(name),
                $"„{schluessel}\" steht in {datei}, aber nicht in ThemeColor. Entweder gehört " +
                $"die Farbe in die Tabelle (dann hinten anhängen — die Reihenfolge ist Teil " +
                $"des Formats), oder sie ist keine Theme-Farbe und der Schlüssel ist irreführend.");
        }
    }

    private static void Vergleiche(ThemeDefinition tabelle, string datei)
    {
        var ausDatei = Lies(datei);

        foreach (var (farbe, wert) in tabelle.Entries)
        {
            // Die 15 Oberflächenfarben stehen als SolidColorBrush unter "Brush.X", die
            // 5 Farben des gezeichneten Blattes als rohe Color unter "Color.X" — der
            // Renderer braucht ein SKColor und keinen Pinsel.
            string schluessel = farbe < ThemeColor.CanvasBg ? $"Brush.{farbe}" : $"Color.{farbe}";

            Assert.True(ausDatei.TryGetValue(schluessel, out string? text),
                $"„{schluessel}\" fehlt in {datei}. Die Tabelle in Core kennt {farbe}.");

            Assert.True(HexColor.TryParse(text, out var ausXaml),
                $"„{text}\" in {datei} ({schluessel}) ist keine lesbare Farbe.");

            Assert.True(ausXaml == wert,
                $"{farbe} weicht ab: {datei} sagt {ausXaml}, Themes.{tabelle.Variant} sagt {wert}. " +
                $"Beide Stellen nachziehen — src/GonkNote.Wpf/Themes/{datei} und " +
                $"src/GonkNote.Core/Theming/Themes.cs.");
        }
    }

    /// <summary>Alle <c>x:Key</c>/Farbwert-Paare einer Theme-Datei, als reines XML gelesen.</summary>
    private static Dictionary<string, string> Lies(string datei)
    {
        string pfad = Path.Combine(Kopfordner, "Themes", datei);
        Assert.True(File.Exists(pfad), $"{pfad} gibt es nicht.");

        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var wurzel = XDocument.Load(pfad).Root!;

        var treffer = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var e in wurzel.Elements())
        {
            if (e.Attribute(x + "Key")?.Value is not { } schluessel) continue;

            // <SolidColorBrush Color="#RRGGBB"/> oder <Color>#RRGGBB</Color>
            string? wert = e.Attribute("Color")?.Value ?? e.Value;
            if (!string.IsNullOrWhiteSpace(wert)) treffer[schluessel] = wert.Trim();
        }
        return treffer;
    }

    /// <summary>
    /// Der Quellordner des WPF-Kopfs, aus dem Projektordner des Testprojekts abgeleitet.
    /// Die Theme-Dateien liegen dort als Quelltext und nicht im Ausgabeordner — sie sind
    /// im Kopf als <c>Resource</c> eingebettet, nicht als Datei kopiert.
    /// </summary>
    private static string Kopfordner =>
        Path.GetFullPath(Path.Combine(Projektordner, "..", "..", "src", "GonkNote.Wpf"));

    private static string Projektordner =>
        typeof(FarbtabelleTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjektOrdner")?.Value
        ?? throw new InvalidOperationException(
            "Assembly-Metadatum „ProjektOrdner\" fehlt — siehe GonkNote.Wpf.Tests.csproj.");
}
