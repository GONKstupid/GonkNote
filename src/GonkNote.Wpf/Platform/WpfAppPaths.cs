using System.IO;
using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>
/// Windows: Nutzerdaten unter <c>%APPDATA%\GonkNote</c>, Beigaben neben der Exe.
/// Inhaltlich dasselbe wie <see cref="DefaultAppPaths"/> — die Klasse steht trotzdem hier,
/// damit der Kopf seine Pfade besitzt und nicht Core für ihn rät.
/// </summary>
public sealed class WpfAppPaths : IAppPaths
{
    public string DataFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GonkNote");

    public string AppFolder { get; } = AppContext.BaseDirectory;
}
