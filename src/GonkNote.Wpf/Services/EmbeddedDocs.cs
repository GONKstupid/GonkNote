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
        ZeigtAuf(target, GuideLinkDe) || ZeigtAuf(target, GuideLinkEn);

    /// <summary>
    /// Zeigt <paramref name="target"/> auf die Datei <paramref name="datei"/>?
    ///
    /// <para>
    /// <b>Die Sprungmarke fällt vorher weg</b>, und das ist kein Feinschliff: In
    /// <c>ERSTE-SCHRITTE.md</c> steht <c>README.md#zwei-ausgaben-eine-app</c>, und ein
    /// schlichtes <c>EndsWith</c> sagt dazu <b>nein</b> — der Verweis zeigt aber sehr wohl
    /// auf das README. <b>Gefunden hat es der Wächter</b>, der drei Stellen erwartete und
    /// zwei bekam; das Nachzählen von Hand hatte drei ergeben.
    /// </para>
    /// <para>
    /// Die Marke selbst wird <b>nicht ausgewertet</b> — beide Dokumente gehen in einem Stück
    /// auf, und an eine Stelle darin zu springen wäre eine eigene Fähigkeit. <b>Ein Verweis,
    /// der das richtige Dokument öffnet, hält mehr als einer, der nichts tut.</b>
    /// </para>
    /// </summary>
    private static bool ZeigtAuf(string target, string datei)
    {
        int marke = target.IndexOf('#');
        var ohne = marke < 0 ? target.AsSpan() : target.AsSpan(0, marke);

        return ohne.EndsWith(datei, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Dateiname des Verweisziels, unter dem die Anleitung auf das README zeigt.</summary>
    public const string ReadmeLinkDe = "README.md";
    public const string ReadmeLinkEn = "README.en.md";

    /// <summary>
    /// Zeigt der Verweis <paramref name="target"/> auf das README?
    ///
    /// <para>
    /// <b>Nachgereicht in Phase 5, Schritt ④, und der Anlass war das laufende Programm:</b> Das
    /// Gegenstück <see cref="IsGuideLink"/> gibt es seit jeher, weil das README auf die
    /// Anleitung zeigt — <b>die Anleitung zeigt aber auch auf das README</b>, und dieser
    /// Verweis ging ins Leere. Er sah trotzdem aus wie einer: Ein <c>.md</c>-Ziel ohne
    /// Behandler wird in <see cref="MarkdownFlow"/> ein <b>eingefärbter Text</b> — dieselbe
    /// Farbe, kein Klick. <b>Ein Verweis, der aussieht wie einer und keiner ist</b> (§4.83).
    /// </para>
    /// <para>
    /// <b><c>README.en.md</c> steht vor <c>README.md</c> in keiner Reihenfolge</b>, weil
    /// <c>EndsWith</c> beide trifft: <c>README.en.md</c> endet nicht auf <c>README.md</c>.
    /// </para>
    /// </summary>
    public static bool IsReadmeLink(string target) =>
        ZeigtAuf(target, ReadmeLinkDe) || ZeigtAuf(target, ReadmeLinkEn);

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
