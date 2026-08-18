namespace GonkNote.Core.Text;

/// <summary>
/// Eine wählbare Farbe mit ihrem Namen.
/// </summary>
/// <param name="Key">Der Übersetzungsschlüssel des Namens.</param>
/// <param name="Hex">„#RRGGBB" — oder <c>null</c> für „keine" bzw. „wie das Dokument".</param>
public readonly record struct TdFarbwahl(string Key, string? Hex);

/// <summary>
/// <b>Die Farben, die Text und Hervorhebung annehmen können</b> — Gruppe B aus §6
/// (HANDOFF §4.40).
///
/// <para>
/// <b>Sie stehen in Core, und das ist diesmal eine Vorsorge und keine Nachbesserung.</b> Bei
/// Farben (§4.9), Schriften (§4.26), Symbolen (§4.31) und Vorlagen (§4.39) ist dieselbe Tabelle
/// je einmal in zwei Köpfen entstanden und musste hinterher zusammengeführt werden — viermal.
/// Diese Tabelle gibt es im WPF-Kopf noch nicht als Liste (er öffnet einen Farbwähler); sie
/// steht deshalb **von Anfang an** an der Stelle, an der beide Köpfe sie finden.
/// </para>
/// <para>
/// <b>Die Tintenfarben sind dieselben wie auf der Zeichenfläche</b>
/// (<c>WhiteboardView.axaml</c>): Ein Nutzer, der auf dem Whiteboard rot schreibt, sucht im
/// Textdokument dasselbe Rot — und zwei Rottöne, die fast gleich sind, sind schlimmer als einer.
/// </para>
/// <para>
/// <b>Feste Werte und nicht aus der Farbtabelle</b> (<c>Themes</c>): Ein Dokument wird gedruckt.
/// Eine Schriftfarbe, die im dunklen Erscheinungsbild hell wäre, verschwände auf weißem Papier —
/// derselbe Grund, aus dem die Überschriftsfarben in <see cref="TdStil"/> festliegen (§4.26).
/// </para>
/// </summary>
public static class TdTextfarben
{
    /// <summary>
    /// Die Schriftfarben. <b>Der erste Eintrag ist „automatisch"</b> und trägt <c>null</c> —
    /// er nimmt die Abweichung wieder heraus, statt Schwarz hineinzuschreiben. Der Unterschied
    /// zählt: Ein ausdrückliches Schwarz überstünde einen späteren Wechsel der Dokumentfarbe
    /// (§4.14).
    /// </summary>
    public static IReadOnlyList<TdFarbwahl> Schrift { get; } =
    [
        new("Td.Color.Auto", null),
        new("Td.Color.Red", "#E11D48"),
        new("Td.Color.Blue", "#2563EB"),
        new("Td.Color.Green", "#16A34A"),
        new("Td.Color.Amber", "#F59E0B"),
        new("Td.Color.Purple", "#7C3AED"),
        new("Td.Color.Grey", "#6B7A99"),
    ];

    /// <summary>
    /// Die Hervorhebungsfarben. <b>Der erste Eintrag ist „keine"</b> — er nimmt die Hervorhebung
    /// heraus; ein Weiß täte das nicht, es legte einen weißen Kasten über den Text.
    ///
    /// <para>
    /// <b>Alle sind hell.</b> Eine Hervorhebung liegt **hinter** dem Text, und der ist dunkel;
    /// eine dunkle Markierung machte ihn unlesbar. Das ist keine Geschmacksfrage, sondern der
    /// Grund, warum Word hier andere Farben anbietet als für die Schrift.
    /// </para>
    /// </summary>
    public static IReadOnlyList<TdFarbwahl> Hervorhebung { get; } =
    [
        new("Td.Color.None", null),
        new("Td.Color.Yellow", "#FDE047"),
        new("Td.Color.Lime", "#BEF264"),
        new("Td.Color.Cyan", "#67E8F9"),
        new("Td.Color.Pink", "#FBCFE8"),
        new("Td.Color.Sky", "#BAE6FD"),
        new("Td.Color.Silver", "#E2E8F0"),
    ];
}
