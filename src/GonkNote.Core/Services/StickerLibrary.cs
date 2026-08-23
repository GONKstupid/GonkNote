using GonkNote.Core.Platform;

namespace GonkNote.Core.Services;

/// <summary>
/// Wo die Sticker liegen und welche Dateien als Sticker zählen.
///
/// <para>
/// <b>Warum das hier steht und nicht im Kopf.</b> Bis Phase 4.5 lag die Regel privat in
/// <c>WhiteboardView.Stickers.cs</c> des WPF-Kopfs — und sie baute den Nutzerordner **von
/// Hand** aus <c>Environment.SpecialFolder.ApplicationData</c>. Das ist eine
/// Windows-Festlegung mitten in einer Regel, die für alle Köpfe gelten soll: unter Linux
/// gehört sie nach <c>~/.config/GonkNote</c>, und <see cref="AppPaths"/> weiß das seit
/// Phase 2. <b>Der Kopf soll nicht wissen, wo der Datenordner liegt</b> — er soll fragen.
/// </para>
/// <para>
/// <b>Die Regel „die Datei des Nutzers gewinnt"</b> ist dieselbe wie bei den eigenen
/// Geodreiecken (<see cref="Rendering.WbAidRenderer.UserAssetFolder"/>) und den
/// Cover-Vorlagen. Sie steht schon dort im Kommentar — <em>„dasselbe Muster wie bei Stickern
/// und Cover-Vorlagen"</em> —, nur hielten die Sticker sich nicht daran.
/// </para>
/// </summary>
public static class StickerLibrary
{
    /// <summary>Die mitgelieferten Sticker neben der Exe (<c>Assets/Stickers</c>).</summary>
    public static string AppFolder => Path.Combine(AppPaths.AppSubfolder("Assets"), "Stickers");

    /// <summary>
    /// Die eigenen Sticker des Nutzers, im Datenordner. <b>Wird angelegt, wenn er fehlt</b> —
    /// ein Ordner, den man erst suchen muss, wird nicht benutzt.
    /// </summary>
    public static string UserFolder
    {
        get
        {
            string dir = AppPaths.DataSubfolder("Stickers");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>
    /// Alle Sticker: <b>mitgelieferte zuerst, eigene danach.</b> Die Reihenfolge ist die
    /// Aussage — was der Nutzer selbst hinzugefügt hat, steht am Ende und damit dort, wo er
    /// es zuletzt gesehen hat. Welche Dateien zählen und wie sie je Ordner sortiert werden,
    /// steht in <see cref="Bildsammlung"/>.
    /// </summary>
    public static List<string> Alle() => [.. Bildsammlung.Dateien(AppFolder),
                                          .. Bildsammlung.Dateien(UserFolder)];
}
