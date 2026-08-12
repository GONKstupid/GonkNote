using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Der Wächter gegen die Rückkehr von <c>Segoe Fluent Icons</c> (§4.31).
///
/// <para>
/// <b>Warum ein Textvergleich und kein Verhaltenstest.</b> Eine Glyphe aus einer
/// Icon-Schrift ist im XAML nur ein Zeichen — sie kompiliert, sie zeichnet, und unter
/// Windows sieht sie sogar richtig aus. <b>Falsch wird sie erst auf dem Laptop</b>, wo die
/// Schrift fehlt und ein leeres Kästchen steht. Genau diese Sorte Fehler kann kein Test
/// finden, der den Kopf ausführt: Er läuft auf Windows.
/// </para>
/// <para>
/// Dieselbe Bauart wie <see cref="FarbtabelleTests"/> — dort wird die Farbtabelle gegen die
/// Theme-Dateien gehalten, hier die XAML-Dateien gegen eine Regel. Beide lesen den
/// <b>Quelltext</b> und nicht die gebaute Ausgabe.
/// </para>
/// </summary>
public class IkonenimKopfTests
{
    /// <summary>
    /// Der private Bereich von Unicode (U+E000 – U+F8FF) — dort liegen die Zeichen jeder
    /// Icon-Schrift. Ein <c>&amp;#xE7A7;</c> im XAML ist genau so ein Zeichen.
    /// </summary>
    private static readonly Regex Glyphe =
        new(@"&#x(?<n>[EF][0-9A-Fa-f]{3});|[-]", RegexOptions.Compiled);

    [Fact]
    public void Kein_Zeichen_aus_einer_Icon_Schrift_steht_mehr_im_XAML()
    {
        var funde = new List<string>();

        foreach (string datei in XamlDateien())
        {
            string[] zeilen = File.ReadAllLines(datei);
            for (int i = 0; i < zeilen.Length; i++)
                if (Glyphe.IsMatch(zeilen[i]))
                    funde.Add($"{Path.GetFileName(datei)}:{i + 1}  {zeilen[i].Trim()}");
        }

        Assert.True(funde.Count == 0,
            "Im XAML steht wieder ein Zeichen aus einer Icon-Schrift. Symbole gehören in die " +
            "Tabelle in Core (AppIcon/AppIcons, §4.31), sonst fehlen sie unter Linux:\n" +
            string.Join("\n", funde));
    }

    /// <summary>
    /// Und auch die Schrift selbst ist nicht mehr angemeldet. <c>IconFont</c> war die
    /// Ressource, über die die 91 Fundstellen ihre Glyphen bekamen; solange sie dasteht,
    /// kostet der Rückfall eine Zeile.
    /// </summary>
    [Fact]
    public void Die_Icon_Schrift_ist_nirgends_mehr_angemeldet()
    {
        var funde = XamlDateien()
            .Where(d => File.ReadAllText(d).Contains("IconFont", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(funde.Count == 0,
            "„IconFont\" steht wieder im XAML: " + string.Join(", ", funde));
    }

    /// <summary>
    /// <b>Die eigenen Geometrien sind ebenfalls weg.</b> Sie waren die zweite Hälfte des
    /// Problems: <c>Icon.Lasso</c> und <c>Icon.Hand</c> gab es in <i>beiden</i> Köpfen, mit
    /// verschiedenen Formen — die Doppelung aus §4.13, nur eine Ebene tiefer.
    /// </summary>
    [Fact]
    public void Keine_eigene_Symbolgeometrie_steht_mehr_im_Kopf()
    {
        var funde = XamlDateien()
            .Where(d => Regex.IsMatch(File.ReadAllText(d), @"x:Key=""Icon\.\w+"""))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(funde.Count == 0,
            "Im Kopf steht wieder eine eigene Symbolform: " + string.Join(", ", funde));
    }

    private static IEnumerable<string> XamlDateien() =>
        Directory.EnumerateFiles(Kopfordner, "*.xaml", SearchOption.AllDirectories)
                 .Where(d => !d.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                          && !d.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    /// <inheritdoc cref="FarbtabelleTests"/>
    private static string Kopfordner =>
        Path.GetFullPath(Path.Combine(Projektordner, "..", "..", "src", "GonkNote.Wpf"));

    private static string Projektordner =>
        typeof(IkonenimKopfTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjektOrdner")?.Value
        ?? throw new InvalidOperationException(
            "Assembly-Metadatum „ProjektOrdner\" fehlt — siehe GonkNote.Wpf.Tests.csproj.");
}
