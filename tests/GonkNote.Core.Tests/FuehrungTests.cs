using GonkNote.Core.Models;
using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdFuehrung"/> — „wer voll ist, führt" (HANDOFF §4.22, §4.23) und die Warnung,
/// die daran hängt (§5 „Noch offen" 9).
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Die Regel ist ein Zweizeiler, aber sie entscheidet, ob
/// eine Warnung erscheint, die vor stillem Datenverlust schützt — und der einzige Weg, sie
/// falsch zu bekommen, ist der naheliegende: „warnen, wenn <c>Model</c> leer ist". Das wäre
/// genau verkehrt herum. Ein Dokument mit <i>beiden</i> Feldern gefüllt ist der gefährliche
/// Fall; ein Dokument mit leerem <c>Model</c> zeigt der Linux-Kopf gar nicht erst an.
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

    // ==================== Wer führt ====================

    /// <summary>
    /// <b>Der Fall, für den die Warnung gebaut wurde.</b> Ein übernommenes Bestandsdokument hat
    /// beide Felder gefüllt — und trotzdem führt das Altformat, denn der WPF-Editor liest und
    /// schreibt weiter daraus (§4.22). Wer hier <c>false</c> lieferte, ließe die Warnung
    /// ausgerechnet bei jedem echten Dokument schweigen.
    /// </summary>
    [Fact]
    public void BeideGefuellt_AltformatFuehrtTrotzdem()
    {
        Assert.True(TdFuehrung.AltformatFuehrt(Dok(Etwas, Etwas)));
    }

    /// <summary>Ein Bestandsdokument vor der Übernahme: das Altformat führt, unbestritten.</summary>
    [Fact]
    public void NurAltformat_Fuehrt()
    {
        Assert.True(TdFuehrung.AltformatFuehrt(Dok(Etwas, [])));
    }

    /// <summary>
    /// Ein Dokument, das in dieser Fassung entstanden ist (<c>DatabaseService.GetText</c>, §4.32):
    /// Es hat nie ein Altformat gehabt, also gibt es nichts zu führen — <b>und genau hier ist im
    /// Linux-Kopf gefahrlos zu schreiben.</b>
    /// </summary>
    [Fact]
    public void NurModell_NiemandFuehrt()
    {
        Assert.False(TdFuehrung.AltformatFuehrt(Dok([], Etwas)));
    }

    /// <summary>Ein leeres Dokument warnt nicht — es gibt nichts, was etwas überschreiben könnte.</summary>
    [Fact]
    public void Leer_KeineWarnung()
    {
        Assert.False(TdFuehrung.AltformatFuehrt(Dok([], [])));
    }

    // ==================== Was noch zu übernehmen ist ====================

    /// <summary>
    /// <b>Die zwei Fragen sind nicht dieselbe.</b> Nach der Übernahme führt das Altformat
    /// weiter — aber zu übernehmen gibt es nichts mehr. Fielen beide zusammen, liefe die
    /// Übernahme bei jedem Öffnen erneut und überschriebe das Modell mit dem Stand von damals.
    /// </summary>
    [Fact]
    public void BeideGefuellt_UebernahmeStehtNichtMehrAus()
    {
        var doc = Dok(Etwas, Etwas);

        Assert.True(TdFuehrung.AltformatFuehrt(doc));
        Assert.False(TdFuehrung.UebernahmeStehtAus(doc));
    }

    /// <summary>Das Bestandsdokument beim ersten Öffnen — der einzige Fall, der übernommen wird.</summary>
    [Fact]
    public void NurAltformat_UebernahmeStehtAus()
    {
        Assert.True(TdFuehrung.UebernahmeStehtAus(Dok(Etwas, [])));
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
