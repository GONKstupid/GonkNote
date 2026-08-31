namespace GonkNote.Core.Theming;

/// <summary>
/// Die Schriftliste, die ein Kopf im Editor zur Auswahl stellt: <b>mitgelieferte oben,
/// Systemschriften darunter</b>.
///
/// <para>
/// <b>Warum es das gibt</b> (HANDOFF §5 Nr. 14, gemessen in §4.71). Bis zum 2026-08-30
/// füllte jeder Kopf sein Schriftfeld aus einer <b>anderen</b> Quelle: der WPF-Kopf aus
/// <c>SystemFontFamilies</c> — also <b>nur</b> Systemschriften —, der Linux-Kopf aus
/// <see cref="Fonts.Mitgeliefert"/> — also <b>nur</b> den fünf mitgelieferten. Zwei
/// disjunkte Listen.
/// </para>
/// <para>
/// <b>Die Folge war am laufenden Programm zu sehen:</b> Ein neues Dokument steht in
/// „Source Sans 3" (die Vorgabe für <see cref="FontRole.Body"/>). Der Linux-Kopf zeigte den
/// Namen, der WPF-Kopf ein <b>leeres</b> Feld — denn eine mitgelieferte Schrift steht in
/// keiner Systemliste. <i>Kein falscher Name, sondern gar keiner.</i>
/// </para>
/// <para>
/// <b>Die Reihenfolge ist die Aussage</b>, dasselbe Muster wie bei den Cover-Vorlagen
/// (<c>Bildsammlung</c>): Was mitgeliefert wird, sieht auf allen drei Plattformen gleich aus
/// und steht deshalb oben. Was vom System kommt, ist eine Zugabe des jeweiligen Rechners —
/// und ein Dokument, das sie benutzt, sieht anderswo anders aus.
/// </para>
/// <para>
/// <b>Warum Core die Systemschriften nicht selbst holt:</b> Es gibt keinen
/// plattformneutralen Weg dorthin — WPF hat <c>SystemFontFamilies</c>, Avalonia
/// <c>FontManager.Current.SystemFonts</c>, und iPadOS wird einen dritten haben. Core
/// bestimmt die <b>Zusammensetzung</b>, der Kopf liefert die Zutat. Genau die Teilung, die
/// §4.26 für das Schriftschema getroffen hat.
/// </para>
/// </summary>
public static class Schriftliste
{
    /// <summary>
    /// Mitgelieferte Familien zuerst, danach die übergebenen Systemschriften.
    /// </summary>
    /// <param name="systemschriften">
    /// Was der Kopf beim System erfragt hat. Darf leer sein — dann bleiben die
    /// mitgelieferten übrig, und das ist eine brauchbare Liste und kein Fehler.
    /// </param>
    /// <returns>
    /// Die Namen in Anzeigereihenfolge. <b>Ohne Doppelte:</b> Ist eine mitgelieferte Schrift
    /// zusätzlich im System installiert — unter Linux ist „Inter" gut möglich —, stünde sie
    /// sonst zweimal da, und der Nutzer müsste raten, welche der beiden er nimmt.
    /// Verglichen wird ohne Rücksicht auf Groß- und Kleinschreibung, weil Schriftnamen aus
    /// dem System in beliebiger Schreibweise kommen.
    /// </returns>
    public static IReadOnlyList<string> Aufbauen(IEnumerable<string>? systemschriften)
    {
        var namen = new List<string>();
        var gesehen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var familie in Fonts.Mitgeliefert)
            if (gesehen.Add(familie.Family))
                namen.Add(familie.Family);

        // Die Systemschriften alphabetisch — sie kommen je nach Rechner in beliebiger
        // Reihenfolge, und eine Liste, die auf zwei Rechnern anders sortiert ist, ist
        // dieselbe Sorte Ärgernis wie zwei verschiedene Listen.
        foreach (var name in (systemschriften ?? []).OrderBy(n => n, StringComparer.CurrentCulture))
            if (!string.IsNullOrWhiteSpace(name) && gesehen.Add(name.Trim()))
                namen.Add(name.Trim());

        return namen;
    }

    /// <summary>
    /// Wie viele Einträge am Anfang der Liste mitgeliefert sind — die Stelle, an der ein Kopf
    /// eine Trennlinie zieht.
    ///
    /// <para>
    /// <b>Eine Zahl und kein Trenneintrag in der Liste selbst:</b> Ein Platzhalter wäre ein
    /// Name, den man auswählen kann, und stünde am Ende als Schriftart in einem Dokument.
    /// </para>
    /// </summary>
    public static int MitgelieferteZahl => Fonts.Mitgeliefert.Count;
}
