namespace GonkNote.Core.Text;

/// <summary>
/// <b>Die Zeichen, die kein Tastenfeld hat</b> — der Vorrat hinter „Symbol einfügen" (§4.88).
///
/// <para>
/// <b>Warum das in Core liegt.</b> Bis §4.88 stand die Liste als <c>Symbols</c> im WPF-Kopf,
/// und der Linux-Kopf hätte sie ein zweites Mal gebraucht. Das ist zum vierten Mal derselbe
/// Fall (§4.77, §4.78, §4.82): <b>eine Tabelle, die beide Köpfe zeigen, gehört an eine
/// Stelle</b>. Sie rechnet nichts — aber ein Kopf, dem später ein Zeichen fehlt, ist genauso
/// falsch wie einer, der falsch rechnet.
/// </para>
/// <para>
/// <b>Gruppiert und nicht flach.</b> Der WPF-Kopf zeigte 57 Zeichen in einem einzigen Raster;
/// wer darin das Ungleich-Zeichen sucht, liest alle 57. Die Gruppen kosten nichts und stehen
/// beiden Köpfen frei — wer will, hängt sie einfach hintereinander.
/// </para>
/// <para>
/// <b>⛔ Ein Eintrag ist beim Umzug herausgefallen, und das ist kein Versehen:</b> die alte
/// Liste endete auf den Text <c>"None"</c>, den die Schleife danach ausdrücklich übersprang
/// (<c>if (sym == "None") continue;</c>). Ein Wert, der nur da ist, um übergangen zu werden,
/// ist ein Rest und kein Zeichen.
/// </para>
/// </summary>
public static class TdSonderzeichen
{
    /// <summary>Eine benannte Gruppe des Vorrats. Der Name ist ein Loc-Schlüssel, kein Text.</summary>
    public sealed record Gruppe(string Schluessel, IReadOnlyList<string> Zeichen);

    /// <summary>Striche, Anführungszeichen und was sonst zum Satz gehört.</summary>
    public static readonly IReadOnlyList<string> Satz =
        ["–", "—", "…", "„", "“", "‚", "‘", "»", "«", "§", "¶", "•"];

    /// <summary>Pfeile.</summary>
    public static readonly IReadOnlyList<string> Pfeile =
        ["→", "←", "↔", "⇒", "⇔", "↑", "↓"];

    /// <summary>Rechenzeichen und Vergleiche.</summary>
    public static readonly IReadOnlyList<string> Mathematik =
        ["±", "×", "÷", "≈", "≠", "≤", "≥", "∞", "√", "∑", "∫", "°", "‰",
         "½", "⅓", "¼", "¾", "²", "³"];

    /// <summary>Griechische Buchstaben, wie sie in Formeln vorkommen.</summary>
    public static readonly IReadOnlyList<string> Griechisch =
        ["π", "Δ", "Ω", "µ", "α", "β", "γ", "δ", "λ", "φ"];

    /// <summary>Zeichen, die weder Buchstabe noch Rechenzeichen sind.</summary>
    public static readonly IReadOnlyList<string> Weiteres =
        ["€", "©", "®", "™", "✓", "✗", "★", "☆", "♦"];

    /// <summary>Alle Gruppen in Anzeigereihenfolge.</summary>
    public static readonly IReadOnlyList<Gruppe> Gruppen =
    [
        new("Ed.Symbol.Group.Text", Satz),
        new("Ed.Symbol.Group.Arrows", Pfeile),
        new("Ed.Symbol.Group.Math", Mathematik),
        new("Ed.Symbol.Group.Greek", Griechisch),
        new("Ed.Symbol.Group.Misc", Weiteres),
    ];

    /// <summary>Alle Zeichen hintereinander — für einen Kopf, der sie ohne Gruppen zeigt.</summary>
    public static IEnumerable<string> Alle => Gruppen.SelectMany(g => g.Zeichen);
}
