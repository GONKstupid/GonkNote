using GonkNote.Core.Models;
using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdFuehrung"/> — <b>seit Schritt 7 (§4.48) nur noch eine Frage: steht die
/// einmalige Übernahme aus?</b>
///
/// <para>
/// <b>Was hier gestanden hat und warum es weg ist.</b> Bis §4.48 trug diese Datei fünf weitere
/// Wächter über <c>AltformatFuehrt</c> — „wer voll ist, führt" (§4.22, §4.23) — und über die
/// Warnung, die im Linux-Kopf daran hing (§5 „Noch offen" 9). <b>Mit Schritt 7 führt das
/// Modell, in beiden Köpfen.</b> Der WPF-Editor liest und schreibt dasselbe Feld wie der
/// Linux-Kopf, also gibt es nichts mehr, wovor zu warnen wäre — und die Funktion ist gelöscht
/// statt auf <c>false</c> gesetzt: <b>Eine Funktion, die immer <c>false</c> zurückgibt, sähe
/// nach einer offenen Frage aus und würde eines Tages wieder geglaubt.</b>
/// </para>
/// <para>
/// <b>Die verbliebene Frage ist eine andere, und es gibt sie weiter.</b> Ein Dokument aus der
/// Windows-Zeit trägt seinen Inhalt nur im Altfeld, bis er <b>einmal</b> übernommen ist. Der
/// Linux-Kopf kann das nicht (RTF und XamlPackage liest nur WPF, §4.22) und muss es wissen,
/// bevor er ein leeres Blatt zeigt.
/// </para>
/// <para>
/// <b>Der einzige Weg, sie falsch zu bekommen, ist der naheliegende:</b> sie mit „das Altfeld
/// ist gefüllt" zu verwechseln. Nach der Übernahme ist es das weiterhin — <b>zu übernehmen
/// gibt es dann aber nichts mehr</b>, und wer es doch täte, überschriebe das Modell bei jedem
/// Öffnen mit dem Stand von damals. Genau dieser Unterschied hat den Linux-Kopf schon einmal
/// eine falsche Auskunft gekostet (§4.29, §5 „Noch offen" 8).
/// </para>
/// <para>
/// <b>Kein Kopf und keine Datenbank.</b> Die Regel liest zwei Feldlängen — sie läuft deshalb
/// auf jedem Rechner gleich, und dieser Wächter schlägt an, bevor jemand einen der beiden Köpfe
/// startet.
/// </para>
/// </summary>
public sealed class FuehrungTests
{
    private static readonly byte[] Etwas = [1, 2, 3];

    private static TextDoc Dok(byte[] rtf, byte[] model) =>
        new() { Id = Guid.NewGuid(), Rtf = rtf, Model = model };

    /// <summary>
    /// <b>Das Bestandsdokument beim ersten Öffnen</b> — der einzige Fall, der übernommen wird.
    /// </summary>
    [Fact]
    public void NurAltformat_UebernahmeStehtAus()
    {
        Assert.True(TdFuehrung.UebernahmeStehtAus(Dok(Etwas, [])));
    }

    /// <summary>
    /// <b>Beide Felder gefüllt heißt: schon übernommen.</b> Das Altfeld bleibt stehen — es wird
    /// nie überschrieben (§4.22) —, aber es ist ab hier eine Sicherung und keine Quelle.
    ///
    /// <para>
    /// <b>Der Wächter, an dem der teuerste Fehler dieser Stelle hängt.</b> Liefe die Übernahme
    /// hier noch einmal, schriebe sie den Stand von damals über alles, was seither getippt
    /// wurde — und zwar bei <b>jedem</b> Speichern, seit der Editor selbst ins Modell schreibt
    /// (§4.48).
    /// </para>
    /// </summary>
    [Fact]
    public void BeideGefuellt_UebernahmeStehtNichtMehrAus()
    {
        Assert.False(TdFuehrung.UebernahmeStehtAus(Dok(Etwas, Etwas)));
    }

    /// <summary>
    /// Ein neu angelegtes Dokument darf <b>nicht</b> in die Übernahme laufen — sonst zeigte der
    /// Linux-Kopf wieder „stammt aus der Windows-Fassung" für etwas, das aus dieser stammt
    /// (§4.29, §5 „Noch offen" 8).
    /// </summary>
    [Fact]
    public void NurModell_KeineUebernahme()
    {
        Assert.False(TdFuehrung.UebernahmeStehtAus(Dok([], Etwas)));
    }

    /// <summary>Ohne Altformat gibt es nichts zu übernehmen, auch nicht aus dem Nichts.</summary>
    [Fact]
    public void Leer_KeineUebernahme()
    {
        Assert.False(TdFuehrung.UebernahmeStehtAus(Dok([], [])));
    }
}
