using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdVorschau"/> — der unfertige Text einer Eingabemethode, <b>Schritt 6b des
/// Schreibens</b> (HANDOFF §4.43, §5 „Noch offen" 10a und 11).
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> <see cref="TdVorschau.Aus"/> ist eine Grenzstelle: Text
/// und Abstand kommen aus <b>fremdem</b> Code — bei IBus aus der Eingabemethode über D-Bus,
/// über Avalonias <c>OnUpdatePreedit</c>. Nichts davon ist geprüft, wenn es hier ankommt, und
/// der Kopf schneidet mit der Zahl unmittelbar in die Zeichenkette
/// (<c>Text.AsSpan(0, Marke)</c>), um die Marke zu setzen. <b>Ein Abstand, der außerhalb liegt
/// oder mitten in einem Ersatzpaar sitzt, ist dort kein Schönheitsfehler, sondern eine
/// Ausnahme beim Zeichnen — mitten im Tippen.</b>
/// </para>
/// <para>
/// <b>Was hier ausdrücklich <i>nicht</i> geprüft wird:</b> ob der unfertige Text auf dem Schirm
/// richtig aussieht, und ob die toten Tasten damit wieder ankommen. Das erste sieht nur ein
/// Auge, das zweite nur der Laptop — <c>SupportsPreedit</c> wirkt gegen IBus und nicht gegen
/// eine Rechnung (§4.42). <b>Diese Wächter halten die Rechnung, nicht den Zweck.</b>
/// </para>
/// </summary>
public class EingabeVorschauTests
{
    // ==================== Nichts im Gange ====================

    [Fact]
    public void Leer_ist_leer()
    {
        Assert.True(TdVorschau.Leer.IstLeer);
        Assert.Equal("", TdVorschau.Leer.Text);
        Assert.Equal(0, TdVorschau.Leer.Marke);
    }

    /// <summary>
    /// <c>null</c> und die leere Zeichenkette heißen beide „nichts im Gange" — <b>der Kopf soll
    /// dafür nicht zwei Fragen stellen müssen</b>. Eine Eingabemethode meldet mal das eine, mal
    /// das andere, wenn sie ein Zusammensetzen abbricht.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Nichts_wird_zu_Leer(string? text)
    {
        var v = TdVorschau.Aus(text, 7);

        Assert.True(v.IstLeer);
        Assert.Equal(TdVorschau.Leer, v);
        // **Auch die Marke:** Sonst stünde eine 7 neben einem leeren Text, und der Kopf
        // schnitte beim nächsten Zeichnen daneben.
        Assert.Equal(0, v.Marke);
    }

    // ==================== Der Abstand wird geklemmt ====================

    /// <summary>
    /// <b>Fehlt die Marke, steht sie am Ende</b> — dort steht sie beim Zusammensetzen fast
    /// immer, und der Anfang wäre die unwahrscheinlichere Vermutung.
    /// </summary>
    [Fact]
    public void Ohne_Marke_steht_sie_am_Ende()
    {
        var v = TdVorschau.Aus("nihon", null);

        Assert.Equal("nihon", v.Text);
        Assert.Equal(5, v.Marke);
    }

    /// <summary>
    /// <b>Nichts, was von außen kommt, wird geglaubt</b> — dieselbe Regel wie beim Rückweg der
    /// Auswahl (§4.41). Beide Richtungen, denn eine Eingabemethode, die den Anschluss verloren
    /// hat, meldet auch negative Zahlen.
    /// </summary>
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(-99, 0)]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(6, 5)]
    [InlineData(int.MaxValue, 5)]
    public void Die_Marke_wird_in_den_Text_geklemmt(int gemeldet, int erwartet)
    {
        var v = TdVorschau.Aus("nihon", gemeldet);

        Assert.Equal(erwartet, v.Marke);
        // Der Kopf schneidet unmittelbar damit — das muss immer gehen.
        _ = v.Text.AsSpan(0, v.Marke).ToString();
    }

    /// <summary>
    /// Die Marke <b>am Ende</b> ist gültig und wird nicht abgeschnitten: Dort steht sie, solange
    /// noch nichts zurückgenommen wurde — also fast die ganze Zeit.
    /// </summary>
    [Fact]
    public void Die_Marke_darf_hinter_dem_letzten_Zeichen_stehen()
    {
        var v = TdVorschau.Aus("ab", 2);

        Assert.Equal(2, v.Marke);
        Assert.Equal("ab", v.Text.AsSpan(0, v.Marke).ToString());
    }

    // ==================== Das Ersatzpaar ====================

    /// <summary>
    /// <b>Der eigentliche Grund, warum das kein bloßes <c>Math.Clamp</c> ist.</b> Ein Emoji und
    /// ein seltenes CJK-Zeichen stehen in .NET als <i>zwei</i> UTF-16-Stellen. Eine Marke
    /// dazwischen ist keine Stelle, sondern ein halbes Zeichen — und genau diese Zeichen sind
    /// die, für die eine Eingabemethode überhaupt gebraucht wird.
    ///
    /// <para>
    /// <b>Sie rückt zurück und nicht vor:</b> Der Anfang des Zeichens ist die Stelle, die der
    /// Nutzer meint; vorzurücken schöbe die Marke über ein Zeichen, das er noch gar nicht
    /// getippt hat.
    /// </para>
    /// </summary>
    [Fact]
    public void Die_Marke_zerschneidet_kein_Ersatzpaar()
    {
        // U+1F600 — zwei UTF-16-Stellen. Davor ein „a", damit die Prüfung nicht zufällig
        // dadurch aufgeht, dass 0 sowieso richtig wäre.
        const string text = "a\U0001F600b";
        Assert.Equal(4, text.Length);

        var v = TdVorschau.Aus(text, 2);   // mitten im Paar

        Assert.Equal(1, v.Marke);          // auf den Anfang des Zeichens zurück
        Assert.Equal("a", v.Text.AsSpan(0, v.Marke).ToString());
    }

    /// <summary>
    /// Und die Stellen <b>daneben</b> bleiben, wo sie sind — sonst wäre die Rückkehr aus dem
    /// Ersatzpaar eine Verschiebung, die auch gültige Marken trifft.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    public void Gueltige_Stellen_neben_dem_Ersatzpaar_bleiben_stehen(int gemeldet, int erwartet)
    {
        var v = TdVorschau.Aus("a\U0001F600b", gemeldet);

        Assert.Equal(erwartet, v.Marke);
    }

    /// <summary>
    /// <b>Die Gleichung, die der Kopf braucht</b>, über alle Meldungen hinweg: Der Schnitt an
    /// der Marke geht immer auf, und was dabei herauskommt, ist gültiger Text — kein halbes
    /// Ersatzpaar, das Skia als leeren Kasten malt.
    /// </summary>
    [Fact]
    public void Der_Schnitt_an_der_Marke_geht_immer_auf()
    {
        string[] texte = ["", "a", "nihon", "a\U0001F600b", "\U0001F600", "\U0001F600\U0001F600"];
        int[] gemeldet = [int.MinValue, -1, 0, 1, 2, 3, 4, 5, int.MaxValue];

        foreach (string text in texte)
            foreach (int m in gemeldet)
            {
                var v = TdVorschau.Aus(text, m);

                Assert.InRange(v.Marke, 0, v.Text.Length);

                string vorne = v.Text.AsSpan(0, v.Marke).ToString();
                string hinten = v.Text.AsSpan(v.Marke).ToString();

                // Zusammen wieder das Ganze — und keine der beiden Hälften endet oder beginnt
                // mit einem halben Zeichen.
                Assert.Equal(v.Text, vorne + hinten);
                Assert.False(vorne.Length > 0 && char.IsHighSurrogate(vorne[^1]));
                Assert.False(hinten.Length > 0 && char.IsLowSurrogate(hinten[0]));
            }
    }

    // ==================== Der Text selbst wird nicht angefasst ====================

    /// <summary>
    /// <b>Der unfertige Text kommt durch, wie er gemeldet wurde.</b> Er ist kein Inhalt, der ins
    /// Dokument geht (§4.43) — er wird nur gemalt. Ihn hier zu säubern hieße, die Abstände der
    /// Eingabemethode gegen einen Text zu halten, den sie nie geschickt hat: <b>genau das
    /// Auseinanderlaufen von Zeichen- und Schrittzählung, vor dem §4.41 warnt.</b>
    /// </summary>
    [Fact]
    public void Der_Text_wird_nicht_veraendert()
    {
        const string roh = "  ni  hon  ";

        Assert.Equal(roh, TdVorschau.Aus(roh, 0).Text);
    }

    /// <summary>
    /// Gleiche Meldung, gleicher Wert — der Kopf vergleicht damit, um nicht bei jeder
    /// Wiederholung ein Bild anzustoßen (IBus meldet denselben Stand mehrfach).
    /// </summary>
    [Fact]
    public void Gleiche_Meldung_ergibt_denselben_Wert()
    {
        Assert.Equal(TdVorschau.Aus("nihon", 2), TdVorschau.Aus("nihon", 2));
        Assert.NotEqual(TdVorschau.Aus("nihon", 2), TdVorschau.Aus("nihon", 3));
        Assert.NotEqual(TdVorschau.Aus("nihon", 2), TdVorschau.Aus("nihom", 2));
    }
}
