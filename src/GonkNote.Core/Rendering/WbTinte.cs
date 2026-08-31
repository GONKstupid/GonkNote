using GonkNote.Core.Text;

namespace GonkNote.Core.Rendering;

/// <summary>
/// <b>Die Tintenfarben der Zeichenfläche</b> — die Kacheln in der Werkzeugleiste.
///
/// <para>
/// <b>Warum es diese Tabelle gibt, und sie ist eine Richtigstellung</b> (HANDOFF §4.74).
/// <see cref="TdTextfarben"/> sagt seit §4.40 wörtlich: <i>„Die Tintenfarben sind dieselben
/// wie auf der Zeichenfläche — ein Nutzer, der auf dem Whiteboard rot schreibt, sucht im
/// Textdokument dasselbe Rot, und zwei Rottöne, die fast gleich sind, sind schlimmer als
/// einer."</i>
/// </para>
/// <para>
/// <b>Der Linux-Kopf hielt sich daran, der WPF-Kopf nicht.</b> Dort standen acht Kacheln mit
/// den Farben aus der <i>Theme</i>-Tabelle (<c>#EF4444</c>, <c>#EAB308</c>, dazu Türkis und
/// Pink), hier fünf mit den Tintenfarben (<c>#E11D48</c>, <c>#F59E0B</c>). <b>Zwei Paletten,
/// verschieden lang, verschieden sortiert, mit zwei fast gleichen Rottönen</b> — genau das,
/// wovor der Satz warnt. <i>Ein Kommentar, der eine Übereinstimmung behauptet, ersetzt sie
/// nicht.</i>
/// </para>
/// <para>
/// <b>Deshalb steht sie jetzt hier und nicht zweimal in XAML.</b> Dasselbe Muster wie bei den
/// Farben (§4.9), Schriften (§4.26), Symbolen (§4.31) und Vorlagen (§4.39) — nur wird es
/// diesmal <i>vor</i> dem Auseinanderlaufen angewandt und nicht danach.
/// </para>
/// </summary>
public static class WbTinte
{
    /// <summary>
    /// Die Kacheln in Anzeigereihenfolge. <b>Der erste Eintrag ist „automatisch"</b> und trägt
    /// <c>null</c>: Er folgt der Seite — schwarz auf hellen, weiß auf dunklen. Ein
    /// ausdrückliches Schwarz täte das nicht und wäre auf einer dunklen Seite unsichtbar.
    ///
    /// <para>
    /// <b>Grau fehlt hier mit Absicht</b>, obwohl <see cref="TdTextfarben.Schrift"/> es kennt:
    /// Auf Papier ist Grau eine lesbare Schriftfarbe, auf einer Tafel ist es ein blasser
    /// Strich. Der Linux-Kopf — die Vorlage — hatte es nie, und ein Werkzeug mehr, das
    /// niemand nimmt, macht die Leiste nur länger.
    /// </para>
    /// </summary>
    public static IReadOnlyList<TdFarbwahl> Palette { get; } =
    [
        new("Color.Auto", null),
        new("Color.Red", "#E11D48"),
        new("Color.Blue", "#2563EB"),
        new("Color.Green", "#16A34A"),
        // `Color.Orange` und nicht `Td.Color.Amber`: Die Leiste benutzt die kurzen
        // Farbnamen, die auch das Symbolfarben-Menue nimmt — ein zweiter Name fuer
        // denselben Ton waere ein zweiter Name.
        new("Color.Orange", "#F59E0B"),
        new("Color.Purple", "#7C3AED"),
    ];

    /// <summary>
    /// Der Wert, der ins Dokument geschrieben wird — <c>„#AARRGGBB"</c> mit voller Deckkraft,
    /// oder <c>„auto"</c> für die erste Kachel.
    ///
    /// <para>
    /// <b>Die Umrechnung steht hier und nicht in den Köpfen:</b> Beide schrieben sie bisher
    /// von Hand in ihre XAML (<c>Tag="#FF2563EB"</c> neben <c>Background="#2563EB"</c>) —
    /// zwei Schreibweisen derselben Farbe, nebeneinander, per Hand gepflegt.
    /// </para>
    /// </summary>
    public static string Marke(TdFarbwahl farbe) =>
        farbe.Hex is { } hex ? "#FF" + hex.TrimStart('#') : "auto";
}
