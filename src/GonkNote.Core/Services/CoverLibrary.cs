using GonkNote.Core.Platform;

namespace GonkNote.Core.Services;

/// <summary>
/// Wo die Cover-Vorlagen liegen und wie sie gruppiert sind — <b>neu in Phase 5, Schritt ①c</b>
/// (§4.81).
///
/// <para>
/// <b>Warum das hier steht und nicht im Kopf:</b> genau derselbe Grund wie bei
/// <see cref="StickerLibrary"/> (§4.54). Die Regel lag privat in
/// <c>WhiteboardView.Covers.cs</c> des WPF-Kopfs; ein zweiter Kopf hätte sie ein zweites Mal
/// bekommen, und die erste Abweichung wäre eine Sammlung, die auf zwei Systemen verschieden
/// aussieht. <b>Das ist §4.13</b>, und der Sticker-Fall ist die Vorlage für die Antwort.
/// </para>
/// <para>
/// <b>Gruppen sind Unterordner.</b> Mitgeliefert werden <c>Basic</c>, <c>Muster</c> und
/// <c>Pixel Art</c>; was der Nutzer selbst hinzufügt, liegt <b>flach</b> im Datenordner und
/// bildet die Gruppe „Individuell". Diese Aufteilung ist keine Kosmetik — sie entscheidet,
/// wohin die „+"-Kachel kopiert und welche Gruppe nach dem Hinzufügen aufklappt.
/// </para>
/// </summary>
public static class CoverLibrary
{
    /// <summary>Die mitgelieferten Vorlagen neben der Exe (<c>Assets/Covers</c>).</summary>
    public static string AppFolder => Path.Combine(AppPaths.AppSubfolder("Assets"), "Covers");

    /// <summary>
    /// Die eigenen Vorlagen des Nutzers, im Datenordner. <b>Wird angelegt, wenn er fehlt</b> —
    /// ein Ordner, den man erst suchen muss, wird nicht benutzt.
    /// </summary>
    public static string UserFolder
    {
        get
        {
            string dir = AppPaths.DataSubfolder("Covers");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>Eine Gruppe der Sammlung: ein Name und die Dateien darin.</summary>
    /// <param name="Name">Der Ordnername, bzw. „Individuell" für die eigenen.</param>
    /// <param name="Dateien">Die Bilddateien, sortiert wie <see cref="Bildsammlung.Dateien"/>.</param>
    /// <param name="Eigene">Ob das die Gruppe des Nutzers ist — sie klappt nach dem Hinzufügen auf.</param>
    public sealed record Gruppe(string Name, IReadOnlyList<string> Dateien, bool Eigene);

    /// <summary>
    /// Alle Gruppen: <b>mitgelieferte zuerst (alphabetisch), die eigenen zuletzt.</b> Die
    /// Reihenfolge ist die Aussage — dieselbe wie bei den Stickern: was der Nutzer selbst
    /// hinzugefügt hat, steht am Ende und damit dort, wo er es zuletzt gesehen hat.
    ///
    /// <para>
    /// <b>Leere Gruppen kommen nicht vor.</b> Ein Ordner ohne Bilder ist kein Abschnitt,
    /// sondern eine leere Überschrift — und die sieht aus wie ein Fehler.
    /// </para>
    /// </summary>
    public static List<Gruppe> Gruppen()
    {
        var gruppen = new List<Gruppe>();

        if (Directory.Exists(AppFolder))
        {
            foreach (string ordner in Directory.GetDirectories(AppFolder)
                                               .OrderBy(o => Path.GetFileName(o), StringComparer.CurrentCulture))
            {
                var dateien = Bildsammlung.Dateien(ordner);
                if (dateien.Count > 0)
                    gruppen.Add(new Gruppe(Path.GetFileName(ordner), dateien, Eigene: false));
            }
        }

        // Die eigenen liegen flach im Datenordner — **nicht** in Unterordnern: dorthin
        // kopiert die „+"-Kachel, und ein Nutzer, der von Hand einen Unterordner anlegt,
        // hat sich etwas anderes vorgestellt, als diese Sammlung anbietet.
        var eigene = Bildsammlung.Dateien(UserFolder);
        if (eigene.Count > 0)
            gruppen.Add(new Gruppe(IndividuellName, eigene, Eigene: true));

        return gruppen;
    }

    /// <summary>
    /// Der Name der Nutzergruppe. <b>Er steht hier und nicht in einer Sprachtabelle</b>, und
    /// das ist Absicht: Die anderen Gruppennamen sind <i>Ordnernamen</i> und werden nicht
    /// übersetzt (sonst fände sie niemand auf der Platte wieder). Eine Gruppe zu übersetzen
    /// und die anderen nicht, wäre schlimmer als keine zu übersetzen.
    /// </summary>
    public const string IndividuellName = "Individuell";
}
