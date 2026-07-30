using System.IO;

namespace GonkNote.Core.Tests;

/// <summary>
/// Ein eigener Wegwerf-Ordner je Test, samt Pfad für eine Wegwerf-Datenbank.
/// <para>
/// **Nie in der echten Datenbank testen** (HANDOFF §7). Die Tests hier legen deshalb
/// grundsätzlich unter <c>%TEMP%\gonk-tests\…</c> an und räumen hinterher auf; auf
/// <see cref="Services.DatabaseService.DefaultPath"/> greift kein Test zu.
/// </para>
/// Der Ordnername enthält eine Guid, damit parallel laufende Testklassen (xunit
/// parallelisiert je Collection) sich nicht gegenseitig die Blobs wegräumen.
/// </summary>
public sealed class TempWorkspace : IDisposable
{
    public TempWorkspace(string label)
    {
        Root = Path.Combine(Path.GetTempPath(), "gonk-tests", $"{label}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    /// <summary>Pfad für die Wegwerf-Datenbank. Der Blob-Ordner leitet sich daraus ab.</summary>
    public string DatabasePath => Path.Combine(Root, "wegwerf.db");

    public string File(string name) => Path.Combine(Root, name);

    public void Dispose()
    {
        // Ein liegengebliebener Temp-Ordner ist kein Testfehler — er darf keinen roten
        // Lauf erzeugen, nur weil LiteDB die Datei noch einen Moment gesperrt hält.
        try { Directory.Delete(Root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
