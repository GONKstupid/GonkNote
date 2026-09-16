using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GonkNote.Core.Tests;

/// <summary>
/// Wächter über die Zeitgeber des Linux-Kopfs: <b>jeder <c>DispatcherTimer</c> nennt seine
/// Dringlichkeitsstufe.</b>
///
/// <para>
/// <b>Die Gefahr, gegen die das steht</b> (Nutzer, 2026-09-14: „hold für Rechtsklick ist
/// zickig und funktioniert nur manchmal"). <c>new DispatcherTimer()</c> nimmt in Avalonia
/// 12.1.1 <c>DispatcherPriority.Background</c> — die <b>unterste</b> Stufe, unter
/// <c>Input</c> und unter <c>Render</c>. Solange ein Stift oder ein Finger aufliegt, meldet
/// der Digitizer mit einigen hundert Hertz; die Warteschlange läuft nie leer, und ein
/// Auftrag der untersten Stufe kommt nicht mehr dran. Die beiden Langdruck-Gesten
/// (Schnellaktionen, Zahlenblock) hingen genau daran: <b>mit der Maus liefen sie
/// zuverlässig</b> — eine stillstehende Maus erzeugt keine Ereignisse — und mit dem
/// Werkzeug, für das sie gebaut sind, nur manchmal.
/// </para>
/// <para>
/// <b>Warum die Regel „nennen" heißt und nicht „nicht Background".</b> Für das Sichern alle
/// dreißig Sekunden und für die Schreibmarke ist <c>Background</c> die richtige Wahl. Falsch
/// ist nicht die Stufe, sondern sie <i>ungefragt</i> zu bekommen: Der Aufruf
/// <c>new DispatcherTimer()</c> sieht aus, als stünde da keine Entscheidung, und trifft doch
/// eine. Wer sie ausschreibt, hat sie getroffen.
/// </para>
/// <para>
/// Gelesen wird der Quelltext — dasselbe Vorgehen und derselbe Grund wie bei
/// <see cref="ThemeschluesselTests"/>: Diese Sorte Fehler ist am Bau nicht zu sehen (er war
/// grün), an keinem Wächter über das Verhalten und auch nicht beim Lesen der Zeile.
/// </para>
/// </summary>
public class ZeitgeberTests
{
    /// <summary>
    /// <c>new DispatcherTimer</c> bis zur öffnenden Klammer bzw. dem Objektinitialisierer.
    /// Was in den runden Klammern steht, landet in <c>args</c> — bei <c>new DispatcherTimer()</c>
    /// und bei <c>new DispatcherTimer {…}</c> ist das leer, und genau das ist der Fund.
    /// </summary>
    private static readonly Regex Erzeugung =
        new(@"new\s+DispatcherTimer\s*(\((?<args>[^)]*)\))?", RegexOptions.Compiled);

    /// <summary>
    /// Kommentare fallen vor dem Suchen heraus.
    ///
    /// <para>
    /// <b>Nicht der Ordnung halber, sondern weil dieser Kopf in Prosa erklärt.</b> Die
    /// Begründung zu dieser Regel steht als Fließtext neben den Zeitgebern und <b>nennt den
    /// falschen Aufruf beim Namen</b> — ohne das hier meldete der Wächter genau die Stelle,
    /// an der die Behebung erklärt wird. <i>Ein Wächter, der auf die Beschreibung des
    /// Fehlers anschlägt statt auf den Fehler, ist einer, den man abschaltet.</i>
    /// </para>
    /// </summary>
    private static readonly Regex Kommentar =
        new(@"/\*.*?\*/|//.*$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// <b>Die Zeilen bleiben stehen</b>, nur ihr Inhalt fällt weg — sonst zeigte die
    /// Fundmeldung auf eine Zeilennummer, die es in der Datei nicht gibt. (Blockkommentare
    /// über mehrere Zeilen kennt dieser Kopf nicht; der einzige steht auf einer Zeile.)
    /// </summary>
    private static string OhneKommentare(string quelle) => Kommentar.Replace(quelle, "");

    [Fact]
    public void Jeder_DispatcherTimer_nennt_seine_Dringlichkeit()
    {
        var fehlend = new List<string>();

        foreach (string datei in Quelldateien("*.cs"))
        {
            string quelle = OhneKommentare(File.ReadAllText(datei));
            foreach (Match treffer in Erzeugung.Matches(quelle))
            {
                if (treffer.Groups["args"].Value.Contains("DispatcherPriority")) continue;

                int zeile = quelle.Take(treffer.Index).Count(z => z == '\n') + 1;
                fehlend.Add($"{Kurz(datei)}:{zeile}");
            }
        }

        Assert.True(fehlend.Count == 0,
            "Diese Zeitgeber bekommen ihre Dringlichkeit ungefragt — und die Vorgabe ist " +
            "DispatcherPriority.Background, die unterste Stufe. Unter einem aufliegenden " +
            "Stift kommt sie nicht mehr dran (Langdruck, 2026-09-14). Stufe ausschreiben: " +
            "`Input` für alles, was auf eine Geste antwortet, `Background` für das, was " +
            "warten darf.\n  " + string.Join("\n  ", fehlend));
    }

    /// <summary>
    /// Die beiden Langdruck-Gesten namentlich: Sie sind der gemeldete Fehler, und für sie
    /// ist <c>Background</c> nicht nur ungenannt, sondern <b>falsch</b>. Die Regel darüber
    /// ließe ein ausgeschriebenes <c>Background</c> hier durchgehen.
    /// </summary>
    [Theory]
    [InlineData("Views/WhiteboardView.Schnellaktionen.cs")]
    [InlineData("Views/WhiteboardView.Zahlenblock.cs")]
    [InlineData("MainWindow.Tastatur.cs")]
    public void Langdruck_und_Fokusruhe_laufen_auf_Input(string datei)
    {
        string quelle = OhneKommentare(File.ReadAllText(Path.Combine(Kopfordner, datei)));

        foreach (Match treffer in Erzeugung.Matches(quelle))
            Assert.True(treffer.Groups["args"].Value.Contains("DispatcherPriority.Input"),
                $"„{treffer.Value}\" in {datei} antwortet auf eine Geste und braucht " +
                $"DispatcherPriority.Input. Auf Background wird der Auftrag von den " +
                $"Zeigerereignissen verdrängt, solange der Stift aufliegt; auf Normal " +
                $"drängelte er sich vor die Bewegungen, die ihn noch abbrechen könnten.");
    }

    private static IEnumerable<string> Quelldateien(string muster) =>
        Directory.EnumerateFiles(Kopfordner, muster, SearchOption.AllDirectories)
                 .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                          && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static string Kurz(string pfad) => Path.GetRelativePath(Kopfordner, pfad);

    private static string Kopfordner =>
        Path.GetFullPath(Path.Combine(Projektordner, "..", "..", "src", "GonkNote.Avalonia"));

    private static string Projektordner =>
        typeof(ZeitgeberTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjektOrdner")?.Value
        ?? throw new InvalidOperationException(
            "Assembly-Metadatum „ProjektOrdner\" fehlt — siehe GonkNote.Core.Tests.csproj.");
}
