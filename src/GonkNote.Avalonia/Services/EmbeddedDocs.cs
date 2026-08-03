using System.IO;
using Avalonia.Platform;

namespace GonkNote.Services;

/// <summary>
/// Lädt die mitgelieferten Dokumente (README, Erste Schritte) in der gerade eingestellten
/// Sprache — <b>das Linux-Gegenstück zu <c>src/GonkNote.Wpf/Services/EmbeddedDocs.cs</c></b>,
/// mit derselben API und demselben Namensraum.
///
/// <para>
/// <b>Warum zweimal und nicht in Core:</b> beide Fassungen unterscheiden sich in genau einer
/// Zeile — wie man an eine eingebettete Datei kommt. Der WPF-Kopf liest sie über
/// <c>pack://application:,,,/README.md</c>, Avalonia über <c>avares://…</c>. Das ist
/// derselbe Grund, aus dem es <c>TExtension</c> zweimal gibt (HANDOFF §7): eine gemeinsame
/// Klasse in Core müsste den Zugriff über eine weitere Schnittstelle hereinreichen, und die
/// hätte genau zwei Umsetzungen von je einer Zeile.
/// </para>
/// <para>
/// Fehlt die englische Fassung, wird die deutsche gezeigt. Lieber ein Dokument in der
/// falschen Sprache als ein leerer Dialog.
/// </para>
/// </summary>
public static class EmbeddedDocs
{
    /// <summary>Dateiname des Verweisziels, unter dem das README auf die Anleitung zeigt.</summary>
    public const string GuideLinkDe = "ERSTE-SCHRITTE.md";
    public const string GuideLinkEn = "GETTING-STARTED.md";

    private const string Wurzel = "avares://GonkNote.Avalonia/";

    public static string Readme() => Load("README.en.md", "README.md");

    public static string Guide() => Load(GuideLinkEn, GuideLinkDe);

    /// <summary>Zeigt der Verweis <paramref name="target"/> auf die Erste-Schritte-Anleitung?</summary>
    public static bool IsGuideLink(string target) =>
        target.EndsWith(GuideLinkDe, StringComparison.OrdinalIgnoreCase) ||
        target.EndsWith(GuideLinkEn, StringComparison.OrdinalIgnoreCase);

    private static string Load(string english, string german)
    {
        if (Loc.Current == AppLanguage.English && TryRead(english, out var text)) return text;
        return TryRead(german, out var fallback) ? fallback : $"{german} konnte nicht geladen werden.";
    }

    private static bool TryRead(string name, out string text)
    {
        try
        {
            using var strom = AssetLoader.Open(new Uri(Wurzel + name));
            using var leser = new StreamReader(strom);
            text = leser.ReadToEnd();
            return true;
        }
        catch
        {
            // Resource fehlt oder ist unlesbar -> der Aufrufer nimmt die Ausweichfassung.
            text = string.Empty;
            return false;
        }
    }
}
