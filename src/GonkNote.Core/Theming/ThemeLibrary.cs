using GonkNote.Core.Platform;
using GonkNote.Services;

namespace GonkNote.Core.Theming;

/// <summary>
/// Ein Eintrag der Design-Liste. <see cref="Theme"/> ist <c>null</c>, wenn die Datei sich
/// nicht lesen ließ — dann steht in <see cref="Error"/>, woran es lag.
/// <para>
/// <b>Warum eine kaputte Datei nicht einfach übersprungen wird:</b> Wer eine Datei in den
/// Ordner legt und sie im Menü nicht wiederfindet, sucht den Fehler bei sich und hat keinen
/// Anhaltspunkt. „Hier fehlt etwas" ist besser als „hier war nie etwas" (HANDOFF §7) —
/// dieselbe Regel wie beim Platzhalterkasten des Diagramms (§4.24).
/// </para>
/// </summary>
public sealed record ThemeEntry(string File, string Name, ThemeDefinition? Theme, string? Error)
{
    public bool IsUsable => Theme != null;
}

/// <summary>
/// Wo die eigenen Designs liegen und wie sie hineinkommen.
///
/// <para>
/// <b>Dieselbe Stelle und dieselbe Regel wie bei Stickern, Cover-Vorlagen und den eigenen
/// Geodreieck-SVGs</b>: der Ordner gehört dem Nutzer, liegt unter
/// <see cref="IAppPaths.DataFolder"/> und wird angelegt, wenn er fehlt — ein Ordner, den man
/// erst suchen muss, wird nicht benutzt (<see cref="Services.StickerLibrary"/>).
/// </para>
/// <para>
/// <b>Und dieselbe Naht:</b> der Kopf soll nicht wissen, wo der Datenordner liegt, er soll
/// fragen. Unter Windows ist das <c>%APPDATA%\GonkNote\Themes</c>, unter Linux
/// <c>~/.config/GonkNote/Themes</c>, in der Flatpak-Sandbox wieder etwas anderes — und
/// genau dafür steht <see cref="AppPaths"/> seit Phase 2.
/// </para>
/// </summary>
public static class ThemeLibrary
{
    /// <summary>Der Ordner mit den eigenen Designs. <b>Wird angelegt, wenn er fehlt.</b></summary>
    public static string UserFolder
    {
        get
        {
            string dir = AppPaths.DataSubfolder("Themes");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>
    /// Alle eigenen Designs, nach Anzeigename sortiert — <b>kaputte inbegriffen</b>, damit
    /// das Menü sie benennen kann. Die zwei mitgelieferten Tabellen stehen nicht darin: sie
    /// sind keine Dateien (<see cref="Themes.Light"/>, <see cref="Themes.Dark"/>).
    /// </summary>
    public static IReadOnlyList<ThemeEntry> All()
    {
        string ordner;
        try { ordner = UserFolder; }
        catch (IOException) { return []; }
        catch (UnauthorizedAccessException) { return []; }

        var liste = new List<ThemeEntry>();
        foreach (string pfad in Dateien(ordner))
            liste.Add(Read(pfad));

        // Nach dem, was im Menü steht — nicht nach dem Dateinamen. Wer sein Theme
        // „Mitternacht" nennt und die Datei `theme2.json`, sucht es unter M.
        liste.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return liste;
    }

    /// <summary>Eine einzelne Datei aus dem Design-Ordner, am Dateinamen (mit Endung).</summary>
    public static ThemeEntry? Find(string? file)
    {
        if (string.IsNullOrWhiteSpace(file)) return null;
        string pfad = Path.Combine(UserFolder, Path.GetFileName(file));
        return File.Exists(pfad) ? Read(pfad) : null;
    }

    /// <summary>
    /// Eine Datei irgendwo auf der Platte lesen — ohne sie zu übernehmen. Das ist der erste
    /// Schritt von „Eigenes laden…": <b>erst prüfen, dann kopieren</b>. Eine unlesbare Datei
    /// soll gar nicht erst im Ordner landen.
    /// </summary>
    public static ThemeEntry Read(string pfad)
    {
        string datei = Path.GetFileName(pfad);
        string ersatzname = Path.GetFileNameWithoutExtension(pfad);
        try
        {
            string text = File.ReadAllText(pfad);
            if (ThemeFile.TryParse(text, ersatzname, out var theme, out string? fehler) && theme != null)
                return new ThemeEntry(datei, theme.Name, theme.WithFile(datei), null);

            return new ThemeEntry(datei, ersatzname, null, fehler);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ThemeEntry(datei, ersatzname, null, ex.Message);
        }
    }

    /// <summary>
    /// Eine geprüfte Datei in den Design-Ordner <b>kopieren</b> (nicht verschieben, nicht
    /// verweisen) und den neuen Dateinamen zurückgeben.
    /// <para>
    /// Kopieren ist dieselbe Entscheidung wie bei den eigenen Cover-Vorlagen: Wer sein Theme
    /// aus dem Download-Ordner lädt und dort aufräumt, soll es nicht verlieren. Ein
    /// vorhandener Name wird <b>nicht</b> überschrieben — es wird durchnummeriert.
    /// </para>
    /// </summary>
    public static string Insert(string quellpfad)
    {
        string ordner = UserFolder;
        string stamm = Sanitize(Path.GetFileNameWithoutExtension(quellpfad));
        string ziel = Path.Combine(ordner, stamm + ThemeFile.Extension);

        for (int n = 2; File.Exists(ziel); n++)
            ziel = Path.Combine(ordner, $"{stamm}-{n}{ThemeFile.Extension}");

        File.Copy(quellpfad, ziel);
        return Path.GetFileName(ziel);
    }

    /// <summary>
    /// Schreibt <paramref name="vorlage"/> als vollständige Datei in den Design-Ordner und
    /// gibt den <b>ganzen Pfad</b> zurück — den braucht der Kopf, um ihn zu nennen oder zu
    /// öffnen. Ein vorhandener Name wird auch hier durchnummeriert und nie überschrieben.
    /// </summary>
    public static string WriteTemplate(ThemeDefinition vorlage)
    {
        string ordner = UserFolder;
        string stamm = Sanitize(vorlage.Name);
        if (stamm.Length == 0) stamm = "design";

        string ziel = Path.Combine(ordner, stamm + ThemeFile.Extension);
        for (int n = 2; File.Exists(ziel); n++)
            ziel = Path.Combine(ordner, $"{stamm}-{n}{ThemeFile.Extension}");

        // Der Name in der Datei ist der des Designs, nicht der der Vorlage: „Hell" bliebe im
        // Menü neben dem mitgelieferten „Hell" stehen, und zwei gleich benannte Einträge sind
        // eine Zumutung. Der Zusatz ist übersetzt, denn er ist Oberfläche und kein Datenfeld.
        string name = Loc.T("Theme.TemplateName", vorlage.Name);
        File.WriteAllText(ziel, ThemeFile.ToJson(vorlage.Over(name, vorlage.Variant, [])));
        return ziel;
    }

    /// <summary>
    /// Was beim Start gilt: die gespeicherte Datei, falls es sie noch gibt und sie sich
    /// lesen lässt — sonst die mitgelieferte Tabelle der gespeicherten Variante.
    /// <para>
    /// <b>Der Rückfall ist der eigentliche Inhalt dieser Methode.</b> Eine gelöschte oder
    /// zwischenzeitlich zerschriebene Theme-Datei darf den Start nicht kosten; sie darf ihn
    /// nicht einmal aufhalten. Was der Nutzer dann sieht, ist sein zuletzt gewähltes Hell
    /// oder Dunkel — und im Menü fehlt der Eintrag, was die Erklärung gleich mitliefert.
    /// </para>
    /// </summary>
    public static ThemeDefinition AtStartup(string? variante, string? datei)
    {
        var fallback = Themes.ForVariant(variante == "dark" ? AppTheme.Dark : AppTheme.Light);
        try
        {
            return Find(datei) is { Theme: { } theme } ? theme : fallback;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return fallback;
        }
    }

    /// <summary>Die Dateien des Ordners, sortiert — dieselbe Auswahlregel wie <see cref="Services.Bildsammlung"/>, nur für JSON.</summary>
    private static IEnumerable<string> Dateien(string ordner)
    {
        string[] dateien;
        try { dateien = Directory.GetFiles(ordner, "*" + ThemeFile.Extension); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return []; }

        Array.Sort(dateien, (a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
        return dateien;
    }

    /// <summary>
    /// Aus einem Anzeigenamen einen brauchbaren Dateinamen. <b>Nicht Kosmetik:</b> der Name
    /// kommt aus einer Datei, die jemand geschrieben hat, und <c>..\..\autostart</c> ist ein
    /// gültiger JSON-String. Übrig bleiben Buchstaben, Ziffern, Strich und Unterstrich.
    /// </summary>
    private static string Sanitize(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length);
        foreach (char c in name.Trim())
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_') sb.Append(c);
            else if (c is ' ' or '.') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}
