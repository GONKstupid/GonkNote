namespace GonkNote.Core.Text;

/// <summary>
/// Der eine Ort, an dem gefragt wird, was an einem Absatz auszusetzen ist — Phase 5.2.
///
/// <para>
/// <b>Warum es ihn gibt.</b> Drei Prüfer liefern Fundstellen: das Wörterbuch
/// (<see cref="TdRechtschreibung"/>), die festen Regeln (<see cref="TdGrammatik"/>) und, wenn
/// einer läuft, der LanguageTool-Server (<see cref="TdLanguageTool"/>). <b>Zeichner und Menü
/// dürfen von dieser Dreiteilung nichts wissen.</b> Sonst stünde die Reihenfolge — und damit
/// die Frage, welcher Befund gewinnt, wenn zwei sich überlappen — an zwei Stellen, und die
/// zweite wäre die, die jemand vergisst.
/// </para>
/// <para>
/// <b>Überlappungen werden aufgelöst und nicht übereinandergemalt.</b> Zwei Wellen unter
/// denselben Buchstaben sehen nicht nach doppelter Gründlichkeit aus, sondern nach einem
/// Fehler im Programm. <b>Die Rechtschreibung gewinnt</b>: Ein Wort, das es nicht gibt, ist
/// die handfestere Auskunft als ein Satzbau-Verdacht darüber.
/// </para>
/// </summary>
public static class TdPruefung
{
    /// <summary>
    /// Alles, was an diesem Absatz angestrichen gehört — nach Ort sortiert und
    /// überschneidungsfrei.
    /// </summary>
    /// <param name="grammatik">
    /// Ob auch auf Grammatik geprüft wird. <b>Getrennt schaltbar</b>, weil es zwei Fragen
    /// sind: Die Rechtschreibung ist eine Tatsachenauskunft, die Grammatik hier zum Teil eine
    /// Vermutung — wer sie nicht will, soll die Wellen unter falschen Wörtern behalten.
    /// </param>
    public static IReadOnlyList<TdFehlstelle> Fehler(TdParagraph? absatz, string? bcp47, bool grammatik = true)
    {
        if (absatz is null || bcp47 is not { Length: > 0 }) return [];
        return Fehler(TdCursor.AbsatzText(absatz), bcp47, grammatik);
    }

    /// <inheritdoc cref="Fehler(TdParagraph?, string?, bool)"/>
    public static IReadOnlyList<TdFehlstelle> Fehler(string? text, string? bcp47, bool grammatik = true)
    {
        if (string.IsNullOrEmpty(text)) return [];

        var rechtschreibung = TdRechtschreibung.Fehler(text, bcp47);
        if (!grammatik) return rechtschreibung;

        var alle = new List<TdFehlstelle>(rechtschreibung);
        alle.AddRange(TdGrammatik.Fehler(text, bcp47));
        alle.AddRange(TdLanguageTool.Befunde(text, bcp47));

        if (alle.Count == rechtschreibung.Count) return rechtschreibung;

        // Erst die Rechtschreibung, dann von links nach rechts — damit greift die Regel
        // „die Rechtschreibung gewinnt" unten von selbst, ohne Sonderfall.
        alle.Sort((a, b) =>
            a.Art != b.Art ? a.Art.CompareTo(b.Art)
            : a.Start != b.Start ? a.Start.CompareTo(b.Start)
            : b.Laenge.CompareTo(a.Laenge));

        var behalten = new List<TdFehlstelle>(alle.Count);
        foreach (var f in alle)
        {
            if (f.Laenge <= 0) continue;
            if (behalten.Any(g => g.Start < f.Ende && f.Start < g.Ende)) continue;
            behalten.Add(f);
        }

        behalten.Sort((a, b) => a.Start.CompareTo(b.Start));
        return behalten;
    }

    /// <summary>
    /// Die Vorschläge zu einer Fundstelle. <b>Die Grammatik bringt ihre mit</b> — sie stehen
    /// schon in der Fundstelle; die Rechtschreibung fragt erst jetzt das Wörterbuch, weil
    /// <c>Suggest</c> teuer ist und beim Zeichnen niemand danach fragt.
    /// </summary>
    public static IReadOnlyList<string> Vorschlaege(TdFehlstelle fund, string wort, string? bcp47) =>
        fund.Art is TdBefundArt.Grammatik
            ? fund.Vorschlaege ?? []
            : TdRechtschreibung.Vorschlaege(wort, bcp47);
}
