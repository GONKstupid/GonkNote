namespace GonkNote.Core.Services;

/// <summary>
/// Welche Bilddateien die App aus einem Ordner annimmt — für Sticker <b>und</b> für
/// Cover-Vorlagen.
///
/// <para>
/// <b>Warum das eine eigene Stelle ist.</b> Bis Phase 4.5 stand die Endungsliste privat in
/// <c>WhiteboardView.Stickers.cs</c> — und <c>WhiteboardView.Covers.cs</c> benutzte sie mit,
/// unter dem Namen <c>StickerExts</c>. Die Liste gehört also weder den Stickern noch den
/// Covern, sondern beiden: sie sagt, <em>was die App als Bildvorlage aus einem Ordner
/// liest</em>. Aufgefallen ist das erst beim Verschieben — der Übersetzer hat die zweite
/// Verwendung gemeldet.
/// </para>
/// <para>
/// <b>Kein SVG.</b> Eine Vektordatei muss vor dem Einfügen gerastert werden, und das gehört
/// an die Stelle, die die Zielgröße kennt — nicht an die, die den Ordner liest.
/// </para>
/// </summary>
public static class Bildsammlung
{
    /// <summary>Was als Bildvorlage zählt.</summary>
    public static readonly string[] Endungen = [".png", ".jpg", ".jpeg", ".webp"];

    /// <summary>
    /// Was sich als Bild <b>einfügen</b> lässt — mehr als <see cref="Endungen"/>.
    ///
    /// <para>
    /// <b>Warum zwei Listen, und warum das kein Versehen ist.</b> <see cref="Endungen"/>
    /// beantwortet „was liest die App aus einem <em>Sammlungsordner</em>" — dort sollen keine
    /// Vektordateien liegen, weil beim Lesen des Ordners niemand die Zielgröße kennt. Diese
    /// Liste beantwortet „was darf der Nutzer <em>auswählen</em>": da ist SVG willkommen (sie
    /// wird beim Einfügen gerastert, <see cref="Rendering.WbImagePrep.ForSvg"/>), und
    /// <c>bmp</c>/<c>gif</c> ebenso — sie kommen aus Fremdprogrammen und aus dem Netz.
    /// </para>
    /// <para>
    /// Bis Phase 4.5 lag sie als <c>ImageExtensions</c> privat im WPF-Kopf. Der Linux-Kopf
    /// braucht dieselbe, sonst nimmt er beim Ziehen und Einfügen andere Dateien an als
    /// Windows — und das merkt niemand, bis jemand eine <c>.bmp</c> nicht loswird.
    /// </para>
    /// </summary>
    public static readonly string[] ImportEndungen =
        [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".svg"];

    /// <summary>Lässt sich diese Datei als Bild einfügen? Groß-/Kleinschreibung egal.</summary>
    public static bool IstEinfuegbar(string pfad) =>
        ImportEndungen.Contains(Path.GetExtension(pfad), StringComparer.OrdinalIgnoreCase);

    /// <summary>Hat die Datei eine angenommene Endung? Groß-/Kleinschreibung egal.</summary>
    public static bool IstBild(string pfad) =>
        Endungen.Contains(Path.GetExtension(pfad), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Die Bilddateien eines Ordners, <b>nach Namen sortiert</b>. Ein fehlender Ordner
    /// ergibt eine leere Liste und keine Ausnahme — er ist der Normalfall, solange der
    /// Nutzer nichts hineingelegt hat.
    /// <para>
    /// Sortiert wird, weil das Dateisystem keine Reihenfolge verspricht: ohne das wechselte
    /// eine Sammlung zwischen zwei Starts ihre Anordnung, und niemand fände wieder, was er
    /// gestern an dritter Stelle gesehen hat.
    /// </para>
    /// </summary>
    public static List<string> Dateien(string ordner) =>
        Directory.Exists(ordner)
            ? [.. Directory.EnumerateFiles(ordner)
                  .Where(IstBild)
                  .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)]
            : [];
}
