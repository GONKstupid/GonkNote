using System.IO;
using System.Windows;
using System.Windows.Documents;
using GonkNote.Core.Text;
using GonkNote.Services;
using TextDoc = GonkNote.Core.Models.TextDoc;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Der Weg <b>Linux-Import → Windows-Anzeige</b> (HANDOFF §4.28).
///
/// <para>
/// <b>Warum es diese Wächter geben muss.</b> Seit §4.28 kann der Linux-Kopf DOCX importieren,
/// und er kann dabei <b>kein</b> <c>XamlPackage</c> bauen — das gibt es nur unter Windows
/// (§4.22). Ein dort importiertes Dokument hat deshalb nur <c>Model</c> und ein leeres
/// <c>Rtf</c>. Der WPF-Editor las bis dahin ausschließlich <c>Rtf</c> und hätte für so ein
/// Dokument ein <b>leeres Blatt</b> gezeigt.
/// </para>
/// <para>
/// <b>Das ist der teuerste Fehler dieser Art</b>, weil er nicht nach einer fehlenden Funktion
/// aussieht, sondern nach gelöschtem Inhalt — und weil er erst auf dem <i>anderen</i> Rechner
/// auffällt, also dort, wo niemand mehr an den Import denkt.
/// </para>
/// <para>
/// Geprüft wird die Umwandlung und nicht das Steuerelement: <c>TextEditorView.AusModell</c>
/// ist zehn Zeilen um <see cref="TdZuFlow"/> herum, und ein <c>RichTextBox</c> im Test wäre
/// eine Fensterinstanz für eine Frage, die keine ist.
/// </para>
/// </summary>
public sealed class ModellRueckfallTests
{
    /// <summary>
    /// Ein Dokument, wie <c>AvaloniaDocumentIo.Import</c> es hinterlässt: <c>Model</c> gefüllt,
    /// <c>Rtf</c> leer.
    /// </summary>
    private static TextDoc WieVomLinuxImport(TdDocument modell) => new()
    {
        Model = TdFormatIo.Schreiben(modell),
        Rtf = [],
    };

    private static TdDocument Modell() => new()
    {
        Sections =
        {
            new TdSection(
                new TdParagraph("Rhabarberkuchen"),
                new TdParagraph("Zweiter Absatz"))
            {
                Page = new TdPageSetup { WidthCm = 14.8, HeightCm = 21.0, MarginLeftCm = 3.0 },
            },
        },
    };

    // ==================== Der Kern der Sache ====================

    /// <summary>
    /// <b>Aus dem Modell allein wird wieder ein lesbares Dokument.</b> Fällt dieser Wächter
    /// um, zeigt der WPF-Editor für jedes unter Linux importierte Dokument ein leeres Blatt.
    /// </summary>
    [Fact]
    public void Ein_Dokument_ohne_Altformat_kommt_aus_dem_Modell() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rueckfall");
        var doc = WieVomLinuxImport(Modell());

        Assert.Empty(doc.Rtf);   // die Ausgangslage, sonst prüft der Test nichts

        var gelesen = TdFormatIo.Lesen(doc.Model);
        Assert.NotNull(gelesen);

        var flow = TdZuFlow.Umwandeln(gelesen, werkbank.Blobs, doc);
        string text = new TextRange(flow.ContentStart, flow.ContentEnd).Text;

        Assert.Contains("Rhabarberkuchen", text);
        Assert.Contains("Zweiter Absatz", text);
    });

    /// <summary>
    /// <b>Die Seiteneinrichtung kommt mit.</b> Sie steht bei einem Linux-Import nur im Modell
    /// (<c>TdSection.Page</c>, §4.15) — ohne diesen Schritt bekäme das Dokument drüben
    /// stillschweigend die Standardwerte, und der Umbruch sähe anders aus als beim Export.
    /// </summary>
    [Fact]
    public void Die_Seiteneinrichtung_kommt_aus_dem_Modell_mit() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rueckfall-seite");
        var doc = WieVomLinuxImport(Modell());

        TdZuFlow.Umwandeln(TdFormatIo.Lesen(doc.Model)!, werkbank.Blobs, doc);

        Assert.Equal("A5", doc.PageFormat);
        Assert.Equal(3.0, doc.MarginLeftCm, 3);
    });

    /// <summary>
    /// <b>Ein leeres Modell bleibt leer und wirft nicht.</b> Das ist der Normalfall für jedes
    /// neu angelegte Dokument: <c>Rtf</c> leer, <c>Model</c> leer. Der Rückfall darf daraus
    /// keinen Fehler machen, sondern muss „nichts zu holen" melden — sonst schlüge das
    /// Öffnen eines frischen Dokuments fehl.
    /// </summary>
    [Fact]
    public void Ohne_Modell_gibt_es_nichts_zu_holen()
    {
        var frisch = new TextDoc();

        Assert.Empty(frisch.Rtf);
        Assert.Null(TdFormatIo.Lesen(frisch.Model));
    }

    /// <summary>
    /// <b>Solange <c>Rtf</c> etwas enthält, führt <c>Rtf</c>.</b> Der Rückfall kehrt die
    /// Reihenfolge nicht um (§5) — er greift nur, wo es sonst gar nichts zu lesen gäbe. Ein
    /// Bestandsdokument trägt in <c>Model</c> den Stand der letzten Übernahme; würde daraus
    /// gelesen, verschwände jede Änderung seit dem letzten Speichern.
    /// </summary>
    [Fact]
    public void Das_Altfeld_fuehrt_weiter_wenn_es_belegt_ist() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rueckfall-vorrang");

        var doc = WieVomLinuxImport(Modell());
        doc.Rtf = AlsPaket("Aus dem Altformat");

        // Dieselbe Bedingung wie im Editor: mehr als zwei Bytes im Altfeld → Altformat.
        Assert.True(doc.Rtf.Length > 2);

        var flow = new FlowDocument();
        var ziel = new TextRange(flow.ContentStart, flow.ContentEnd);
        using (var strom = new MemoryStream(doc.Rtf))
            ziel.Load(strom, DataFormats.XamlPackage);

        Assert.Contains("Aus dem Altformat", ziel.Text);
        Assert.DoesNotContain("Rhabarberkuchen", ziel.Text);
    });

    private static byte[] AlsPaket(string text)
    {
        var flow = new FlowDocument(new Paragraph(new Run(text)));
        using var strom = new MemoryStream();
        new TextRange(flow.ContentStart, flow.ContentEnd).Save(strom, DataFormats.XamlPackage);
        return strom.ToArray();
    }
}
