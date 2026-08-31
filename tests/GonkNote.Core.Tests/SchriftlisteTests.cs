using GonkNote.Core.Theming;

namespace GonkNote.Core.Tests;

/// <summary>
/// Wächter über die Schriftliste des Editors (§5 Nr. 14, HANDOFF §4.72).
///
/// <para>
/// <b>Warum sie geprüft wird und nicht nur da ist:</b> Bis zum 2026-08-30 füllte jeder Kopf
/// sein Schriftfeld aus einer <b>anderen</b> Quelle — der WPF-Kopf nur aus den
/// Systemschriften, der Linux-Kopf nur aus den mitgelieferten. Zwei disjunkte Listen, und
/// die Folge stand am laufenden Programm: ein neues Dokument steht in „Source Sans 3", und
/// das Feld des WPF-Kopfs blieb <b>leer</b>, weil eine mitgelieferte Schrift in keiner
/// Systemliste vorkommt (§4.71).
/// </para>
/// <para>
/// <b>Die Zusammensetzung ist der prüfbare Teil.</b> Welche Systemschriften ein Rechner hat,
/// ist keine Zusicherung und gehört nicht in einen Test — <i>in welcher Reihenfolge</i> sie
/// erscheinen und <i>dass die mitgelieferten dabei sind</i>, sehr wohl.
/// </para>
/// </summary>
public sealed class SchriftlisteTests
{
    private static readonly string[] System =
        ["Verdana", "Arial", "consolas", "Segoe UI"];

    /// <summary>
    /// <b>Die mitgelieferten stehen vorn, vollständig und in der Reihenfolge der Tabelle.</b>
    /// Das ist die eigentliche Zusage von §5 Nr. 14.
    /// </summary>
    [Fact]
    public void Die_mitgelieferten_Schriften_stehen_vorn()
    {
        var liste = Schriftliste.Aufbauen(System);

        Assert.Equal(
            Fonts.Mitgeliefert.Select(f => f.Family).ToList(),
            liste.Take(Schriftliste.MitgelieferteZahl).ToList());
    }

    /// <summary>Die Systemschriften folgen danach — alphabetisch, damit zwei Rechner dieselbe
    /// Liste zeigen und nicht dieselben Namen in anderer Ordnung.</summary>
    [Fact]
    public void Die_Systemschriften_folgen_alphabetisch()
    {
        var liste = Schriftliste.Aufbauen(System);
        var rest = liste.Skip(Schriftliste.MitgelieferteZahl).ToList();

        Assert.Equal(["Arial", "consolas", "Segoe UI", "Verdana"], rest);
    }

    /// <summary>
    /// <b>Keine Doppelten.</b> Unter Linux ist „Inter" gut möglich auch im System
    /// installiert; stünde sie zweimal da, müsste der Nutzer raten, welche der beiden er
    /// nimmt. Verglichen wird ohne Rücksicht auf die Schreibweise, weil Systemnamen in
    /// beliebiger Schreibung kommen.
    /// </summary>
    [Fact]
    public void Eine_Schrift_die_es_doppelt_gibt_steht_nur_einmal()
    {
        var liste = Schriftliste.Aufbauen(["INTER", "Arial", "inter"]);

        Assert.Single(liste, n => n.Equals("Inter", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(Schriftliste.MitgelieferteZahl + 1, liste.Count);
    }

    /// <summary>
    /// <b>Ohne Systemschriften bleibt eine brauchbare Liste übrig</b> und keine leere. Ein
    /// Kopf, dessen Systemabfrage nichts liefert, soll die mitgelieferten trotzdem anbieten —
    /// das ist genau der Fall, in dem ein leeres Feld wieder entstünde.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ohne_Systemschriften_bleiben_die_mitgelieferten(bool alsNull)
    {
        var liste = Schriftliste.Aufbauen(alsNull ? null : []);

        Assert.Equal(Schriftliste.MitgelieferteZahl, liste.Count);
        Assert.Contains(Fonts.Standard.Family(FontRole.Body), liste);
    }

    /// <summary>
    /// <b>Die Vorgabeschrift eines neuen Dokuments steht in der Liste.</b> Das ist der Fehler
    /// aus §4.71 als Test: Sie stand dort nicht, und das Feld war deshalb leer.
    /// </summary>
    [Fact]
    public void Die_Vorgabe_eines_neuen_Dokuments_laesst_sich_auswaehlen()
    {
        var liste = Schriftliste.Aufbauen(System);

        foreach (FontRole rolle in Enum.GetValues<FontRole>())
            Assert.Contains(Fonts.Standard.Family(rolle), liste);
    }

    /// <summary>Leere und weiße Einträge fliegen heraus — manche Systeme melden sie.</summary>
    [Fact]
    public void Leere_Namen_kommen_nicht_in_die_Liste()
    {
        var liste = Schriftliste.Aufbauen(["", "   ", "Arial", " Verdana "]);

        Assert.Equal(Schriftliste.MitgelieferteZahl + 2, liste.Count);
        Assert.Contains("Verdana", liste);
        Assert.DoesNotContain(liste, string.IsNullOrWhiteSpace);
    }
}
