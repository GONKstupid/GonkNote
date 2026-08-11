using System.IO;
using System.Text;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdExport"/> — der eine Exportweg beider Köpfe (HANDOFF §4.28).
///
/// <para>
/// <b>Was hier bewacht wird, ist nicht das Schreiben.</b> Dass ein PDF ein PDF ist und ein
/// DOCX ein DOCX, steht in <see cref="PdfTests"/> und <c>DocxRoundtripTests</c>. Bewacht wird
/// die <b>Weiche</b>: dass die Endung entscheidet, dass eine unbekannte Endung ein PDF wird
/// statt einer Ausnahme, und dass beide Köpfe dieselbe Formatliste sehen.
/// </para>
/// <para>
/// <b>Warum das eigene Wächter verdient:</b> Die Weiche stand bis §4.28 im WPF-Kopf. Wäre sie
/// im Linux-Kopf ein zweites Mal geschrieben worden, hätten beide Fassungen dieselben Tests
/// bestanden und wären trotzdem auseinandergedriftet — die Falle aus §4.13. Jetzt gibt es
/// eine Fassung, und dieser Wächter hält fest, was sie zusagt.
/// </para>
/// </summary>
public sealed class ExportwegTests
{
    private static TdDocument Dok(string text = "Rhabarberkuchen") => new()
    {
        Sections = { new TdSection(new TdParagraph(text)) { Page = TdPageSetup.A5 } },
    };

    private static TdFieldContext Felder() =>
        new() { Date = new DateTime(2026, 8, 11), Title = "Probe" };

    // ==================== Die Weiche ====================

    [Theory]
    [InlineData(".pdf", "%PDF")]
    [InlineData(".docx", "PK")]      // ein DOCX ist ein ZIP
    public void Die_Endung_entscheidet_das_Format(string endung, string kennung)
    {
        using var werkbank = new TempWorkspace("export-weiche");
        string pfad = werkbank.File("probe" + endung);

        var ergebnis = TdExport.Schreiben(Dok(), pfad, null, "Probe", Felder());

        Assert.Equal([pfad], ergebnis.Written);
        Assert.StartsWith(kennung, Kopf(pfad));
    }

    [Fact]
    public void Markdown_kommt_als_Text_heraus()
    {
        using var werkbank = new TempWorkspace("export-md");
        string pfad = werkbank.File("probe.md");

        TdExport.Schreiben(Dok(), pfad, null, "Probe", Felder());

        Assert.Contains("Rhabarberkuchen", File.ReadAllText(pfad));
    }

    /// <summary>
    /// <b>Eine unbekannte Endung wird ein PDF und keine Ausnahme.</b> Der Dateidialog lässt
    /// einen eigenen Namen zu, und wer „Bericht" ohne Punkt eingibt, hat nichts falsch
    /// gemacht — er bekommt das Format, das im Dialog obenan stand.
    /// </summary>
    [Fact]
    public void Was_keine_bekannte_Endung_hat_wird_ein_PDF()
    {
        using var werkbank = new TempWorkspace("export-unbekannt");
        string pfad = werkbank.File("bericht.eigenartig");

        TdExport.Schreiben(Dok(), pfad, null, "Probe", Felder());

        Assert.StartsWith("%PDF", Kopf(pfad));
    }

    /// <summary>
    /// PNG legt <b>eine Datei je Seite</b> ab und meldet, welche es wirklich geschrieben hat —
    /// die Meldung an den Nutzer nennt sonst einen Pfad, den es nicht gibt.
    /// </summary>
    [Fact]
    public void PNG_meldet_jede_geschriebene_Seite()
    {
        using var werkbank = new TempWorkspace("export-png");
        string pfad = werkbank.File("probe.png");

        var viele = Enumerable.Range(1, 60)
            .Select(i => (TdBlock)new TdParagraph($"Absatz {i} mit genug Text, dass er umbricht."))
            .ToArray();
        var doc = new TdDocument { Sections = { new TdSection(viele) { Page = TdPageSetup.A5 } } };

        var ergebnis = TdExport.Schreiben(doc, pfad, null, "Probe", Felder());

        Assert.True(ergebnis.Written.Count > 1, "mehrere Seiten erwartet");
        Assert.All(ergebnis.Written, p => Assert.True(File.Exists(p), p));
    }

    // ==================== Die Formatliste ====================

    /// <summary>
    /// <b>Vier Formate, PDF zuerst.</b> Die Reihenfolge ist keine Kosmetik: Avalonia kennt
    /// kein „FilterIndex", der erste Eintrag <i>ist</i> die Vorauswahl
    /// (<c>AvaloniaFileDialog.Save</c>), und unter Windows steht er ebenso oben.
    /// </summary>
    [Fact]
    public void Die_Formatliste_nennt_vier_Formate_und_PDF_zuerst()
    {
        var formate = TdExport.Formate;

        Assert.Equal(4, formate.Count);
        Assert.Equal(".pdf", formate[0].PrimaryExtension);
        Assert.Equal([".pdf", ".docx", ".md", ".png"],
            formate.Select(f => f.PrimaryExtension).ToArray());
    }

    /// <summary>
    /// <b>Jede Endung der Liste wird auch wirklich bedient.</b> Ein Eintrag im Dateidialog,
    /// hinter dem nichts steht, ist eine Sackgasse mit Aussicht — genau deshalb war die Liste
    /// im Linux-Kopf bis §4.28 leer und nicht halb gefüllt.
    /// </summary>
    [Fact]
    public void Zu_jedem_Eintrag_der_Liste_entsteht_eine_Datei()
    {
        using var werkbank = new TempWorkspace("export-liste");

        foreach (var format in TdExport.Formate)
        {
            string pfad = werkbank.File("probe" + format.PrimaryExtension);
            var ergebnis = TdExport.Schreiben(Dok(), pfad, null, "Probe", Felder());

            Assert.NotEmpty(ergebnis.Written);
            Assert.All(ergebnis.Written, p => Assert.True(File.Exists(p), p));
        }
    }

    /// <summary>
    /// Die Liste wird bei <b>jedem</b> Zugriff neu gebaut und nicht einmal beim Start.
    ///
    /// <para>
    /// <b>Daran hängt der Sprachwechsel:</b> die Beschriftungen kommen aus
    /// <see cref="Loc"/>, und ein <c>static readonly</c> hielte für den Rest der Sitzung die
    /// Sprache fest, die beim Start galt (§7, „Texte, die der Code setzt"). Geprüft wird die
    /// Frischheit und nicht der Sprachwechsel selbst: <c>Loc.Apply</c> ist global, und die
    /// Tests laufen nebenläufig — ein Wächter, der die Sprache umstellt, macht andere rot.
    /// </para>
    /// </summary>
    [Fact]
    public void Die_Formatliste_entsteht_bei_jedem_Zugriff_neu()
    {
        Assert.NotSame(TdExport.Formate, TdExport.Formate);
        Assert.Equal(Loc.T("Filter.Pdf"), TdExport.Formate[0].Label);
    }

    private static string Kopf(string pfad)
    {
        using var strom = File.OpenRead(pfad);
        var puffer = new byte[4];
        int gelesen = strom.Read(puffer, 0, puffer.Length);
        return Encoding.ASCII.GetString(puffer, 0, gelesen);
    }
}
