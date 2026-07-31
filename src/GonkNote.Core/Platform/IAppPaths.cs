namespace GonkNote.Core.Platform;

/// <summary>
/// Wo die App ihre Daten und ihre Beigaben findet. Die beiden Ordner sind
/// bewusst getrennt: der eine gehört dem Nutzer und wird beschrieben, der andere
/// gehört zur Installation und wird nur gelesen.
/// <para>
/// Unter Windows liegen sie in <c>%APPDATA%\GonkNote</c> und neben der Exe; unter
/// Linux und iOS gilt jeweils etwas anderes, und genau dafür steht diese Schnittstelle.
/// </para>
/// </summary>
public interface IAppPaths
{
    /// <summary>
    /// Veränderliche Nutzerdaten: Datenbank, Blob-Ordner, Fehlerprotokoll und die
    /// eigenen Beigaben des Nutzers (Sticker, Cover-Vorlagen, Geodreieck-SVGs).
    /// </summary>
    string DataFolder { get; }

    /// <summary>
    /// Mitgelieferte, unveränderliche Beigaben neben dem Programm — der Inhalt von
    /// <c>Assets\</c> und <c>tessdata\</c>. Nie hierhin schreiben: unter Linux und
    /// iOS ist das Verzeichnis schreibgeschützt.
    /// </summary>
    string AppFolder { get; }
}

/// <summary>
/// Die Vorgabe, solange kein Plattform-Kopf etwas anderes setzt: dasselbe Verhalten
/// wie vor Phase 2 — <c>ApplicationData\GonkNote</c> und das Programmverzeichnis.
/// <para>
/// <see cref="Current"/> hat deshalb <b>immer</b> einen brauchbaren Wert. Ein Kopf, der
/// vergisst, ihn zu setzen, bekommt das alte Verhalten und keinen Absturz — das ist
/// Absicht: ein statisches Feld ohne Vorgabe ist genau die stille Falle, die uns bei
/// <c>ImageCache.Source</c> schon einmal die Bilder gekostet hat (HANDOFF §7).
/// </para>
/// </summary>
public sealed class DefaultAppPaths : IAppPaths
{
    public string DataFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GonkNote");

    public string AppFolder { get; } = AppContext.BaseDirectory;
}

/// <summary>Zugriffspunkt auf die Pfade; der Plattform-Kopf setzt ihn beim Start.</summary>
public static class AppPaths
{
    public static IAppPaths Current { get; set; } = new DefaultAppPaths();

    /// <summary>Vorgabepfad der Datenbank — <c>gonknote.db</c> im Datenordner.</summary>
    public static string DatabaseFile => Path.Combine(Current.DataFolder, "gonknote.db");

    /// <summary>Protokoll unerwarteter Fehler, neben der Datenbank.</summary>
    public static string LogFile => Path.Combine(Current.DataFolder, "fehler.log");

    /// <summary>Ein Unterordner der mitgelieferten Beigaben, z. B. <c>Assets</c> oder <c>tessdata</c>.</summary>
    public static string AppSubfolder(string name) => Path.Combine(Current.AppFolder, name);

    /// <summary>Ein Unterordner der Nutzerdaten, z. B. <c>Stickers</c> oder <c>Covers</c>.</summary>
    public static string DataSubfolder(string name) => Path.Combine(Current.DataFolder, name);
}
