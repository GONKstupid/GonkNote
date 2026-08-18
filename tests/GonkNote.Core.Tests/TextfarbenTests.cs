using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdTextfarben"/> — die Farben für Schrift und Hervorhebung (HANDOFF §4.40).
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Eine Farbtabelle sieht aus, als könnte man an ihr nichts
/// falsch machen. Die zwei Dinge, die man doch falsch macht, sind: den ersten Eintrag mit einer
/// Farbe zu belegen (dann nimmt „automatisch" die Abweichung nicht mehr heraus, sondern setzt
/// Schwarz — und die überlebt jeden späteren Wechsel der Dokumentfarbe, §4.14), und eine dunkle
/// Markierungsfarbe aufzunehmen (dann liegt sie hinter dunklem Text und macht ihn unlesbar).
/// </para>
/// </summary>
public sealed class TextfarbenTests
{
    /// <summary>
    /// <b>Der erste Eintrag jeder Liste trägt keine Farbe</b> — er nimmt die Abweichung heraus.
    /// Alle anderen tragen eine.
    /// </summary>
    [Fact]
    public void Der_erste_Eintrag_nimmt_die_Farbe_heraus()
    {
        Assert.Null(TdTextfarben.Schrift[0].Hex);
        Assert.Null(TdTextfarben.Hervorhebung[0].Hex);

        Assert.All(TdTextfarben.Schrift.Skip(1), f => Assert.NotNull(f.Hex));
        Assert.All(TdTextfarben.Hervorhebung.Skip(1), f => Assert.NotNull(f.Hex));
    }

    /// <summary>
    /// <b>Jede Hervorhebung ist hell genug, dass dunkler Text darauf lesbar bleibt.</b>
    /// Gemessen an der wahrgenommenen Helligkeit (Rec. 601) — eine Markierung liegt **hinter**
    /// dem Text, und Word bietet deshalb andere Farben an als für die Schrift.
    /// </summary>
    [Fact]
    public void Jede_Hervorhebung_ist_hell()
    {
        foreach (var farbe in TdTextfarben.Hervorhebung.Skip(1))
            Assert.True(
                Helligkeit(farbe.Hex!) > 0.6,
                $"{farbe.Key} ({farbe.Hex}) ist zu dunkel für eine Hervorhebung: " +
                $"{Helligkeit(farbe.Hex!):0.00}");
    }

    /// <summary>
    /// Und umgekehrt: <b>Jede Schriftfarbe ist dunkel genug für weißes Papier.</b> Ein Dokument
    /// wird gedruckt — eine helle Schrift verschwände darauf (§4.26, derselbe Grund wie bei der
    /// Tinte).
    /// </summary>
    [Fact]
    public void Jede_Schriftfarbe_ist_dunkel()
    {
        foreach (var farbe in TdTextfarben.Schrift.Skip(1))
            Assert.True(
                Helligkeit(farbe.Hex!) < 0.75,
                $"{farbe.Key} ({farbe.Hex}) ist zu hell für weißes Papier: " +
                $"{Helligkeit(farbe.Hex!):0.00}");
    }

    /// <summary>Kein Schlüssel steht zweimal — sonst zeigten zwei Kacheln denselben Namen.</summary>
    [Fact]
    public void Kein_Name_kommt_zweimal_vor()
    {
        var alle = TdTextfarben.Schrift.Concat(TdTextfarben.Hervorhebung)
            .Select(f => f.Key).ToList();

        Assert.Equal(alle.Count, alle.Distinct().Count());
    }

    /// <summary>Wahrgenommene Helligkeit, 0 bis 1 (Rec. 601).</summary>
    private static double Helligkeit(string hex)
    {
        int r = Convert.ToInt32(hex.Substring(1, 2), 16);
        int g = Convert.ToInt32(hex.Substring(3, 2), 16);
        int b = Convert.ToInt32(hex.Substring(5, 2), 16);

        return (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
    }
}
