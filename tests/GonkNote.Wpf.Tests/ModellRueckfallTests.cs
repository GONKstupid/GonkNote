using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using GonkNote.Core.Text;
using GonkNote.Services;
using SkiaSharp;
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
/// <para>
/// <b>Seit §4.47 kommt der Ladeweg selbst dazu</b> — der Abschnitt „Der direkte Weg" am Ende.
/// <c>AusModell</c> hat den Umweg über ein <c>XamlPackage</c> verloren; was dabei zu prüfen
/// ist, sind zwei Dinge, die vorher nicht galten: dass der <c>RichTextBox</c> <b>sein
/// Dokument behält</b>, und dass ein <b>Träger überlebt</b>. Beides ist ohne Fenster zu
/// messen — es sind Fragen an zwei <c>FlowDocument</c>-Objekte.
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

    // ==================== Der direkte Weg (§4.47) ====================

    /// <summary>
    /// <b>Der Editor behält sein Dokument</b> — ausgetauscht wird nur der Inhalt.
    ///
    /// <para>
    /// <b>Das ist die Frage, an der der Umweg über das <c>XamlPackage</c> hing.</b> Die alte
    /// Begründung in <c>AusModell</c> war richtig: Ein ausgetauschtes <c>Document</c> nähme dem
    /// <c>RichTextBox</c> seine Stile und alle Ereignisverdrahtungen mit. <b>Nur folgt daraus
    /// nicht das Paket</b> — es genügt, die Blöcke umzuhängen. Dieser Wächter hält fest, dass
    /// das Zielobjekt dasselbe bleibt; fällt er, ist der Weg zurück zum Paket der richtige.
    /// </para>
    /// </summary>
    [Fact]
    public void Der_Editor_behaelt_sein_Dokument() => Sta.Run(() =>
    {
        var ziel = new FlowDocument(new Paragraph(new Run("alter Inhalt")));
        var vorher = ziel;

        var quelle = new FlowDocument(new Paragraph(new Run("neuer Inhalt")));
        TdZuFlow.InhaltUebernehmen(quelle, ziel);

        Assert.Same(vorher, ziel);
        Assert.Contains("neuer Inhalt", new TextRange(ziel.ContentStart, ziel.ContentEnd).Text);
        Assert.DoesNotContain("alter Inhalt", new TextRange(ziel.ContentStart, ziel.ContentEnd).Text);

        // Die Quelle darf leer zurückbleiben — sie ist ein Wegwerf-Dokument. Ein Block hat
        // genau einen Elternteil; ohne das Herausnehmen wirft das Anhängen.
        Assert.Empty(quelle.Blocks);
    });

    /// <summary>
    /// <b>Ein Träger überlebt den direkten Weg — und stirbt im Paket.</b>
    ///
    /// <para>
    /// <b>Dieser Wächter ist der ganze Grund für §4.47</b>, und er prüft beide Hälften in einem
    /// Lauf: Dasselbe Element geht einmal durch das <c>XamlPackage</c> und einmal über
    /// <see cref="TdZuFlow.InhaltUebernehmen"/>. <b>Nur der zweite Weg bringt den Träger
    /// mit.</b> Ohne den Vergleich wäre nicht belegt, dass der Umbau überhaupt etwas ändert.
    /// </para>
    /// <para>
    /// <b>Warum ein <see cref="InlineUIContainer"/> und nicht ein <c>Run</c>:</b> Am 2026-08-22
    /// gemessen (§4.47) — WPF <b>kopiert</b> das <c>Tag</c> beim Teilen eines Absatzes und beim
    /// Teilen eines Laufs auf <i>beide</i> Hälften. Ein Träger dort wäre nach einem einzigen
    /// Tastendruck doppelt vorhanden. <b>Ein <c>InlineUIContainer</c> ist unteilbar</b> und
    /// deshalb die einzige Stelle, an der ein Träger sicher sitzt.
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_Traeger_ueberlebt_den_direkten_Weg() => Sta.Run(() =>
    {
        static FlowDocument MitTraeger()
        {
            var absatz = new Paragraph();
            absatz.Inlines.Add(new InlineUIContainer(new Border { Width = 8, Height = 8 })
            {
                Tag = "TRAEGER",
            });
            return new FlowDocument(absatz);
        }

        // (a) durch das Paket — so lief es bis §4.47
        var ueberPaket = new FlowDocument();
        using (var strom = new MemoryStream())
        {
            var quelle = MitTraeger();
            new TextRange(quelle.ContentStart, quelle.ContentEnd).Save(strom, DataFormats.XamlPackage);
            strom.Position = 0;
            new TextRange(ueberPaket.ContentStart, ueberPaket.ContentEnd)
                .Load(strom, DataFormats.XamlPackage);
        }

        Assert.Null(Traeger(ueberPaket));   // ⛔ das Paket wirft ihn weg

        // (b) direkt — der Weg seit §4.47
        var direkt = new FlowDocument();
        TdZuFlow.InhaltUebernehmen(MitTraeger(), direkt);

        Assert.Equal("TRAEGER", Traeger(direkt));   // ✅ er kommt an

        static object? Traeger(FlowDocument doc) => doc.Blocks.OfType<Paragraph>()
            .SelectMany(p => p.Inlines).OfType<InlineUIContainer>().FirstOrDefault()?.Tag;
    });

    /// <summary>
    /// <b>Ein Bild behält seinen Blob-Verweis, ohne dass jemand ihn nachträgt.</b>
    ///
    /// <para>
    /// <c>DocumentImages.Attach</c> stand in <c>AusModell</c> und ist mit §4.47 entfallen — es
    /// übersetzt einen Verweis aus dem <c>ToolTip</c> in das <c>Tag</c>, und das braucht nur,
    /// wer aus einem Paket lädt (nur der ToolTip übersteht eines). <b>Dieser Wächter hält
    /// fest, dass das Entfallen richtig war</b>: <c>TdZuFlow</c> setzt das <c>Tag</c> selbst.
    /// Ohne ihn wäre die gestrichene Zeile eine Vermutung — und ein fehlender Verweis fällt
    /// erst beim Export auf, wo das Original ein zweites Mal abgelegt würde.
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_Bild_behaelt_seinen_Blob_Verweis() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("direkt-bild");

        var kennung = werkbank.Blobs.Put(
            Referenzdokument.Bild(40, 30, SKColors.White, SKColors.SteelBlue));

        var modell = new TdDocument();
        modell.Sections.Add(new TdSection(new TdParagraph(new TdInline[]
        {
            new TdImage(kennung, "png", 3, 2),
        })));

        var ziel = new FlowDocument();
        TdZuFlow.InhaltUebernehmen(
            TdZuFlow.Umwandeln(modell, werkbank.Blobs, new TextDoc()), ziel);

        var bild = ziel.Blocks.OfType<Paragraph>()
            .SelectMany(p => p.Inlines).OfType<InlineUIContainer>()
            .Select(u => u.Child).OfType<Image>().SingleOrDefault();

        Assert.NotNull(bild);
        Assert.Equal(kennung, Assert.IsType<BlobRef>(bild.Tag).Id);
    });

    /// <summary>
    /// <b>Die Grundschrift kommt mit.</b> Sie steht am Dokument und nicht an den Absätzen
    /// (§4.14) — bliebe sie zurück, erbte jeder Absatz die Vorgabe des Steuerelements statt
    /// der des Dokuments, und der Umbruch im Editor sähe anders aus als der Export.
    /// </summary>
    [Fact]
    public void Die_Grundschrift_kommt_mit() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("direkt-schrift");

        var quelle = TdZuFlow.Umwandeln(Modell(), werkbank.Blobs, new TextDoc());
        var ziel = new FlowDocument();

        TdZuFlow.InhaltUebernehmen(quelle, ziel);

        Assert.Equal(quelle.FontFamily.Source, ziel.FontFamily.Source);
        Assert.Equal(quelle.FontSize, ziel.FontSize, 6);
    });

    /// <summary>
    /// <b>Die Dokumentschrift wird nicht in jeden Absatz gebrannt</b> — und das ist der zweite
    /// Gewinn des direkten Wegs, der beim Bauen gar nicht gesucht wurde (§4.47).
    ///
    /// <para>
    /// <b>Gemessen am 2026-08-22:</b> <c>TextRange.Save/Load</c> über ein <c>XamlPackage</c>
    /// schiebt die Schrift des Dokuments als <b>örtlichen Wert</b> auf jeden Absatz hinunter.
    /// Sichtbar ist das nicht — die Schrift ist dieselbe —, <b>aber die Kaskade ist weg</b>:
    /// <c>FlowZuTd.ZeichenformatVon</c> liest örtliche Werte, und schriebe danach in
    /// <i>jedem</i> Absatz eine <c>FontFamily</c> ins Modell, wo vorher keine stand.
    /// </para>
    /// <para>
    /// <b>Es ist derselbe Fehler wie in §4.45</b> — „nicht gesetzt" wird zu „gesetzt" —, nur an
    /// einer anderen Stelle und ohne dass jemand ihn gesucht hätte. Mit Schritt 7, wenn die
    /// Rundreise bei jedem Speichern läuft, wäre er in jedes Dokument gewandert.
    /// </para>
    /// </summary>
    [Fact]
    public void Der_direkte_Weg_brennt_die_Dokumentschrift_nicht_in_die_Absaetze() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("direkt-kaskade");

        var quelle = TdZuFlow.Umwandeln(Modell(), werkbank.Blobs, new TextDoc());
        var ziel = new FlowDocument();
        TdZuFlow.InhaltUebernehmen(quelle, ziel);

        var absatz = ziel.Blocks.OfType<Paragraph>().First();

        Assert.Equal(DependencyProperty.UnsetValue,
            absatz.ReadLocalValue(TextElement.FontFamilyProperty));

        // Und trotzdem steht die richtige Schrift da — sie wird geerbt, wie es sein soll.
        Assert.Equal(ziel.FontFamily.Source, absatz.FontFamily.Source);
    });
}
