using System.IO;
using System.Windows;

namespace GonkNote.Services;

/// <summary>
/// Lädt die mitgelieferten Dokumente (README, Erste Schritte) in der gerade eingestellten
/// Sprache. Sie liegen als eingebettete Resource in der Exe — Textänderungen an den
/// Markdown-Dateien erscheinen also automatisch im Programm, ohne Code anzufassen.
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

    public static string Readme() => Load("README.en.md", "README.md");

    public static string Guide() => Load(GuideLinkEn, GuideLinkDe);

    /// <summary>Zeigt der Verweis <paramref name="target"/> auf die Erste-Schritte-Anleitung?</summary>
    public static bool IsGuideLink(string target) =>
        target.EndsWith(GuideLinkDe, System.StringComparison.OrdinalIgnoreCase) ||
        target.EndsWith(GuideLinkEn, System.StringComparison.OrdinalIgnoreCase);

    private static string Load(string english, string german)
    {
        if (Loc.Current == AppLanguage.English && TryRead(english, out var text)) return text;
        return TryRead(german, out var fallback) ? fallback : $"{german} konnte nicht geladen werden.";
    }

    private static bool TryRead(string name, out string text)
    {
        try
        {
            var res = Application.GetResourceStream(new System.Uri($"pack://application:,,,/{name}"));
            if (res != null)
            {
                using var reader = new StreamReader(res.Stream);
                text = reader.ReadToEnd();
                return true;
            }
        }
        catch { /* Resource fehlt oder ist unlesbar -> Aufrufer nimmt die Ausweichfassung */ }

        text = string.Empty;
        return false;
    }
}
