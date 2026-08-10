using GonkNote.Core.Theming;

namespace GonkNote.Core.Platform;

/// <summary>
/// Welches Schriftschema die App benutzt.
///
/// <para>
/// <b>Bis §4.26 stand hier ein einziger Name</b> (<c>UiFamily</c>), und die Köpfe beantworteten
/// ihn je Plattform verschieden — Windows „Segoe UI", Linux „Inter". Das Ergebnis war
/// **dasselbe Dokument in drei Schriftbildern**, und im Avalonia-Kopf zeichnete das Chrome in
/// einer anderen Schrift als die Zeichenfläche: Avalonias eingebettetes Inter kennt Skia nicht.
/// </para>
/// <para>
/// <b>Jetzt liefert die Naht das ganze Schema</b> (fünf Rollen, <see cref="FontScheme"/>), und
/// alle drei Köpfe liefern dasselbe — die mitgelieferten Schriften machen die Frage
/// plattformunabhängig. <b>Die Naht bleibt trotzdem stehen:</b> Ein Kopf, der eines Tages der
/// Systemeinstellung folgen soll (Barrierefreiheit, iPadOS-Dynamic-Type), setzt hier an, ohne
/// dass Core davon weiß.
/// </para>
/// </summary>
public interface IFontProvider
{
    /// <summary>Das Schriftschema — Rolle → Familie, samt Rückfallkette.</summary>
    FontScheme Scheme { get; }

    /// <summary>
    /// Familienname der Oberflächenschrift. Bleibt als Abkürzung stehen, weil die Köpfe ihn für
    /// ihr eigenes Chrome brauchen und dort keine Rolle kennen.
    /// </summary>
    string UiFamily => Scheme.Family(FontRole.Ui);
}

/// <summary>Die Vorgabe: das mitgelieferte Schema (§4.26).</summary>
public sealed class DefaultFontProvider : IFontProvider
{
    public FontScheme Scheme => Fonts.Standard;
}
