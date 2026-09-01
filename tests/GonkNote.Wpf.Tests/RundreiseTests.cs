using System.Windows.Documents;
using GonkNote.Core.Text;
using GonkNote.Services;
using TextDoc = GonkNote.Core.Models.TextDoc;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// <b>Die Rundreise <c>Modell → FlowDocument → Modell</c></b> — der Weg, den der WPF-Editor mit
/// <b>Schritt 7</b> bei <i>jedem</i> Speichern gehen wird (HANDOFF §6, §4.45).
///
/// <para>
/// <b>Warum es diese Wächter geben muss, und warum ausgerechnet jetzt.</b> Bis heute führt
/// <c>TextDoc.Rtf</c>: Das Modell wird bei jedem Speichern aus dem Altformat neu gebaut
/// (§4.23), und was die Rundreise dabei verändert, sieht niemand — die Anzeige kommt ja aus
/// <c>Rtf</c>. <b>Mit Schritt 7 dreht sich das um</b>, und ab dann ist jede Ungenauigkeit
/// dieses Weges kein Schönheitsfehler mehr, sondern eine Änderung am gespeicherten Dokument,
/// die bei jedem Speichern erneut zuschlägt.
/// </para>
/// <para>
/// <b>Gemessen statt vermutet</b> (§4.45): Ein Modell, wie der Linux-Kopf es anlegt, hat vor
/// der Reparatur auf einer einzigen Rundreise verloren, was hier Stück für Stück steht — allen
/// voran den <b>Absatzabstand</b>, denn <c>TdParaFormat.Standard.SpaceAfterPt</c> ist
/// <b>8</b> und nicht 0, und der Weg nach WPF hat die Kaskade nicht aufgelöst, sondern durch
/// Null ersetzt.
/// </para>
/// <para>
/// <b>Die drei benannten Lücken stehen am Ende dieser Datei</b> — als Wächter und nicht als
/// Fußnote, damit sie nicht still verschwinden (§4.19). <b>Alle drei sind inzwischen zu:</b>
/// die Gliederungsebene nebenbei (§4.46), das Feld in §4.49, das Diagramm in §4.50. <b>Die
/// Wächter sind umgedreht und nicht gelöscht</b> — sie sind jetzt die Stelle, an der es
/// auffiele, wenn eine Lücke zurückkäme.
/// </para>
/// </summary>
public sealed class RundreiseTests
{
    /// <summary>
    /// Ein Modell, wie es der Linux-Kopf hinterlässt: <b>nichts davon ist je durch WPF
    /// gelaufen</b>, und nichts trägt einen Wert, den der Nutzer nicht gesetzt hat.
    /// </summary>
    private static TdDocument Eingeboren()
    {
        var frei = new TdParagraph("Ein Absatz, an dem nichts eingestellt ist.");

        var punkt = new TdParagraph("Erster Listenpunkt") { List = new TdListRef(1, 0) };

        var tabelle = new TdTable(
            TdTableRow.Text("Fach", "Note"),
            TdTableRow.Text("Mathe", "2"));
        tabelle.ColumnWidthsCm.AddRange([4.0, 2.0]);

        var doc = new TdDocument();
        doc.Lists.Add(new TdListDefinition { Id = 1 });
        doc.Sections.Add(new TdSection(frei, punkt, tabelle));
        return doc;
    }

    /// <summary>Einmal hin und zurück — genau der Weg, den Schritt 7 bei jedem Speichern nimmt.</summary>
    private static TdDocument Rundreise(TdDocument quelle, Referenzdokument.Werkbank werkbank)
    {
        var traeger = new TextDoc();
        var flow = TdZuFlow.Umwandeln(quelle, werkbank.Blobs, traeger);
        return FlowZuTd.Umwandeln(traeger, flow, werkbank.Blobs);
    }

    // ==================== Der Kern: die Kaskade überlebt ====================

    /// <summary>
    /// <b>Was nicht gesetzt war, ist danach immer noch nicht gesetzt.</b>
    ///
    /// <para>
    /// <b>Der teuerste der gefundenen Fehler steckt in diesem einen Wächter.</b> Vorher kam ein
    /// unberührter Absatz mit <c>SpaceAfterPt = 0</c>, <c>SpaceBeforePt = 0</c>,
    /// <c>LeftIndentCm = 0</c>, <c>RightIndentCm = 0</c> und <c>Alignment = Justify</c>
    /// zurück — fünf Werte, die niemand gesetzt hatte, festgeschrieben im Dokument. Der
    /// Absatzabstand war damit dauerhaft <b>weg</b> (der Standard ist 8 pt), und die
    /// Ausrichtung stand auf Blocksatz, weil ein <c>FlowDocument</c> von Haus aus so steht
    /// (§4.37, Fund 2).
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_unberuehrter_Absatz_kommt_unberuehrt_zurueck() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-kaskade");

        var f = Rundreise(Eingeboren(), werkbank).Paragraphs().First().Format;

        Assert.Null(f.Alignment);
        Assert.Null(f.SpaceBeforePt);
        Assert.Null(f.SpaceAfterPt);
        Assert.Null(f.LeftIndentCm);
        Assert.Null(f.RightIndentCm);
        Assert.Null(f.FirstLineIndentCm);
        Assert.Null(f.LineSpacing);
    });

    /// <summary>
    /// <b>Der Absatzabstand steht im Editor so da, wie das Modell ihn meint.</b> Die andere
    /// Hälfte des Wächters darüber: Es genügt nicht, dass nichts <i>zurück</i>kommt — auf dem
    /// Hinweg muss der aufgelöste Wert auch wirklich am Absatz stehen, sonst zeigt der
    /// WPF-Editor jedes Linux-Dokument mit aneinandergeklebten Absätzen.
    /// </summary>
    [Fact]
    public void Der_Standardabstand_kommt_im_Editor_an() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-abstand");

        var flow = TdZuFlow.Umwandeln(Eingeboren(), werkbank.Blobs, new TextDoc());
        var absatz = flow.Blocks.OfType<System.Windows.Documents.Paragraph>().First();

        // 8 pt sind bei 96 dpi 10,666… geräteunabhängige Pixel.
        Assert.Equal(8 * 96.0 / 72.0, absatz.Margin.Bottom, 3);
        Assert.Equal(System.Windows.TextAlignment.Left, absatz.TextAlignment);
    });

    /// <summary>
    /// <b>Ein Listenpunkt bekommt keinen Abstand angedichtet.</b> Der Weg nach WPF hat ihm
    /// vorher ein festes <c>Thickness(0, 1, 0, 1)</c> aufgedrückt, damit die Liste enger
    /// aussieht — und schrieb dem Modell damit <c>0,75 pt</c> davor und danach ein. Ein
    /// Listenpunkt ist ein Absatz (§4.17) und hat den Abstand seines Formats.
    /// </summary>
    [Fact]
    public void Ein_Listenpunkt_behaelt_seinen_eigenen_Abstand() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-liste");

        var punkt = Rundreise(Eingeboren(), werkbank).Paragraphs().First(p => p.List is not null);

        Assert.Null(punkt.Format.SpaceBeforePt);
        Assert.Null(punkt.Format.SpaceAfterPt);
    });

    // ==================== Tabellen ====================

    /// <summary>
    /// <b>Rahmen und Innenabstand einer Tabelle kommen aus dem Modell.</b> Der Weg nach WPF
    /// schrieb dort fest <c>Grau</c> und <c>0,5 px</c> hin, und der Rückweg las genau das
    /// wieder ab: Aus einer schwarzen 0,5-<b>pt</b>-Linie wurde still eine graue
    /// 0,375-pt-Linie, aus 0,19 cm Innenabstand 0,159 cm — bei jedem Speichern aufs Neue.
    /// </summary>
    [Fact]
    public void Tabellenrahmen_und_Innenabstand_ueberstehen_die_Rundreise() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-tabelle");

        var vorher = new TdTable().Format;
        var nachher = Rundreise(Eingeboren(), werkbank).Blocks().OfType<TdTable>().Single().Format;

        Assert.Equal(vorher.Top.WidthPt, nachher.Top.WidthPt, 3);
        Assert.Equal(vorher.Top.Color, nachher.Top.Color);
        Assert.Equal(vorher.CellPaddingLeftCm, nachher.CellPaddingLeftCm, 3);
        Assert.Equal(vorher.CellPaddingTopCm, nachher.CellPaddingTopCm, 3);
    });

    /// <summary>
    /// <b>Eine Spaltenbreite wandert nicht.</b> Zentimeter → Pixel → Zentimeter trifft sich
    /// nicht selbst (aus 4 wird 3,9999999999999996). Solange <c>Rtf</c> führte, war das
    /// folgenlos; mit Schritt 7 wanderte die Breite bei jedem Speichern ein Stückchen weiter.
    /// </summary>
    [Fact]
    public void Spaltenbreiten_wandern_nicht() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-spalten");

        var t = Rundreise(Eingeboren(), werkbank).Blocks().OfType<TdTable>().Single();

        Assert.Equal(4.0, t.ColumnWidthsCm[0]);
        Assert.Equal(2.0, t.ColumnWidthsCm[1]);
    });

    // ==================== Die Probe aufs Ganze ====================

    /// <summary>
    /// <b>Die zweite Rundreise ändert nichts mehr</b> — Byte für Byte dasselbe Dokument.
    ///
    /// <para>
    /// <b>Das ist der Wächter, auf den es bei Schritt 7 wirklich ankommt.</b> Die anderen
    /// prüfen einzelne Werte; dieser prüft die Eigenschaft, die den Editor gefahrlos macht:
    /// <b>Speichern, ohne etwas zu ändern, darf das Dokument nicht ändern.</b> Ein Dokument,
    /// das nur geöffnet und wieder gespeichert wird, muss danach dasselbe sein — sonst wandert
    /// es über Wochen davon, ohne dass jemand es angefasst hat.
    /// </para>
    /// </summary>
    [Fact]
    public void Die_zweite_Rundreise_aendert_nichts_mehr() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-fest");

        var eins = Rundreise(Eingeboren(), werkbank);
        var zwei = Rundreise(eins, werkbank);

        Assert.Equal(TdFormatIo.Schreiben(eins), TdFormatIo.Schreiben(zwei));
    });

    // ==================== Die drei benannten Lücken — alle drei zu ====================

    /// <summary>
    /// <b>✅ Ein Diagramm überlebt die Rundreise — seit §4.50.</b>
    ///
    /// <para>
    /// <b>Hier stand die letzte der drei benannten Lücken</b>, und sie war die stillste:
    /// <see cref="TdZuFlow"/> hatte für ein Diagramm <b>gar keinen Zweig</b>. Es fiel beim
    /// Umwandeln heraus und war nach dem ersten Speichern im WPF-Editor weg — <b>samt seiner
    /// Zahlen</b>, und das ist der Unterschied zum Feld: Ein Feld hinterließ wenigstens seinen
    /// Platzhaltertext, ein Diagramm hinterließ nichts.
    /// </para>
    /// <para>
    /// <b>Warum sie erst jetzt zu schließen war:</b> Der Träger, an dem ein Diagramm hängen
    /// kann, überlebte das <c>XamlPackage</c> nicht (§4.45, gemessen). Seit §4.47 lädt der
    /// Editor ohne Paket und seit §4.48 schreibt er auch ohne — <b>erst damit übersteht eine
    /// Auflage im Arbeitsspeicher eine volle Runde aus Speichern und Öffnen.</b> Der Träger
    /// ist derselbe wie beim Feld (§4.47: <c>Tag</c> an Absatz und Lauf wird beim Teilen
    /// <b>kopiert</b>, ein <see cref="System.Windows.Documents.InlineUIContainer"/> ist
    /// unteilbar).
    /// </para>
    /// <para>
    /// <b>Geprüft wird der Inhalt und nicht die Anwesenheit.</b> Ein Diagramm, das als leerer
    /// Kasten zurückkommt, wäre hier sonst grün — und genau das war der alte Zustand des
    /// Editors: Er rasterte beim Einfügen zu einer Bitmap und warf die Zahlen weg (§4.21).
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_Diagramm_ueberlebt_die_Rundreise() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-diagramm");

        var quelle = Eingeboren();
        var diagramm = new TdChart(TdChartKind.Column, 8, 5)
        {
            Title = "Noten",
            Categories = { "Mathe", "Deutsch" },
            Palette = { "#112233", "#445566" },
            AltText = "Notenverteilung",
        };
        diagramm.Series.Add(new TdChartSeries("Halbjahr", 2, 3));
        quelle.Sections[0].Blocks.Add(new TdParagraph(new TdInline[] { diagramm }));

        var zurueck = Assert.Single(Rundreise(quelle, werkbank).Blocks()
            .OfType<TdParagraph>().SelectMany(p => p.Inlines).OfType<TdChart>());

        Assert.Equal(TdChartKind.Column, zurueck.Kind);
        Assert.Equal("Noten", zurueck.Title);
        Assert.Equal("Notenverteilung", zurueck.AltText);
        Assert.Equal(8, zurueck.WidthCm);
        Assert.Equal(5, zurueck.HeightCm);
        Assert.Equal(["Mathe", "Deutsch"], zurueck.Categories);
        Assert.Equal(["#112233", "#445566"], zurueck.Palette);
        Assert.Equal("Halbjahr", Assert.Single(zurueck.Series).Name);
        Assert.Equal([2, 3], Assert.Single(zurueck.Series).Values);
    });

    /// <summary>
    /// <b>Aus dem Diagramm wird kein Bild — und kein Blob</b> (§4.50).
    ///
    /// <para>
    /// <b>Das ist der Wächter für den einen Griff, an dem diese Runde scheitern konnte.</b>
    /// Unter der Auflage liegt ein <c>Image</c> mit der gezeichneten Anzeige. Fragt
    /// <see cref="FlowZuTd"/> erst den <i>Inhalt</i> des Behälters und dann die Auflage, so
    /// findet es dieses Bild — und <c>DocumentImages.Adopt</c> legt die Pixel als <b>neuen
    /// Blob</b> ab. Aus den Zahlen würden Pixel, und bei jedem Speichern läge eine Kopie mehr
    /// im Speicher.
    /// </para>
    /// <para>
    /// <b>Es ist derselbe Fehler, den §4.21 am alten Editor benannt hat</b>, nur an neuer
    /// Stelle — und er sähe auf dem Schirm völlig richtig aus. Deshalb prüft dieser Wächter
    /// nicht das Bild, sondern <b>den Blob-Speicher</b>.
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_Diagramm_wird_nicht_zu_einem_Bild_und_legt_keinen_Blob_an() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-diagramm-blob");

        var quelle = Eingeboren();
        var diagramm = new TdChart(TdChartKind.Column, 8, 5) { Title = "Noten" };
        diagramm.Series.Add(new TdChartSeries("Halbjahr", 2, 3));
        quelle.Sections[0].Blocks.Add(new TdParagraph(new TdInline[] { diagramm }));

        int vorher = werkbank.Blobs.All().Count();
        var zurueck = Rundreise(quelle, werkbank);

        Assert.Empty(zurueck.Blocks().OfType<TdParagraph>()
            .SelectMany(p => p.Inlines).OfType<TdImage>());
        Assert.Equal(vorher, werkbank.Blobs.All().Count());
    });

    /// <summary>
    /// <b>Was das Werkzeug einfügt, ist danach ein Diagramm — und nicht erst, was der Ladeweg
    /// baut</b> (§4.82).
    ///
    /// <para>
    /// <b>Das ist die Naht, an der es bis §4.82 auseinanderging, und kein Wächter sah es.</b>
    /// Die Rundreise oben geht vom <i>Modell</i> aus und war immer grün. Das
    /// <i>Diagramm-Werkzeug</i> nahm aber einen anderen Weg: Es legte ein gewöhnliches
    /// <c>Image</c> in den Text, <b>ohne Auflage</b> — und genau an der erkennt
    /// <see cref="FlowZuTd"/> ein Diagramm. Ein frisch eingefügtes Diagramm war deshalb beim
    /// ersten Speichern keines mehr, sondern ein Bild samt neuem Blob, während der Wächter für
    /// dieselbe Datenstruktur grün blieb.
    /// </para>
    /// <para>
    /// <b>Deshalb prüft dieser Wächter den Behälter, den das Werkzeug wirklich benutzt</b>
    /// (<see cref="TdZuFlow.DiagrammBehaelter"/>), und nicht einen nachgebauten. <i>Zwei Wege,
    /// die dasselbe bauen, weichen voneinander ab — und zwar an der Stelle, die niemand
    /// nachsieht.</i>
    /// </para>
    /// </summary>
    [Fact]
    public void Was_das_Werkzeug_einfuegt_ueberlebt_als_Diagramm() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("werkzeug-diagramm");

        // Genau der Griff, den der Dialog tut: Text in den Feldern → Diagramm.
        var diagramm = TdChartEingabe.Lesen(
            TdChartKind.Bar, "Noten", "Mathe, Deutsch", "Halbjahr", "2, 3",
            breiteCm: 14, hoeheCm: 8)!;

        var flow = new FlowDocument();
        var absatz = new Paragraph();
        absatz.Inlines.Add(TdZuFlow.DiagrammBehaelter(diagramm));
        flow.Blocks.Add(absatz);

        int vorher = werkbank.Blobs.All().Count();
        var zurueck = FlowZuTd.Umwandeln(new TextDoc(), flow, werkbank.Blobs);

        var wieder = Assert.Single(zurueck.Blocks()
            .OfType<TdParagraph>().SelectMany(p => p.Inlines).OfType<TdChart>());

        Assert.Equal(TdChartKind.Bar, wieder.Kind);
        Assert.Equal("Noten", wieder.Title);
        Assert.Equal(["Mathe", "Deutsch"], wieder.Categories);
        Assert.Equal([2, 3], Assert.Single(wieder.Series).Values);

        // Und kein Bild, kein Blob — sonst wäre aus den Zahlen beim Speichern ein Pixelbild
        // geworden, so wie es §4.21 am alten Editor gemessen hat.
        Assert.Empty(zurueck.Blocks().OfType<TdParagraph>()
            .SelectMany(p => p.Inlines).OfType<TdImage>());
        Assert.Equal(vorher, werkbank.Blobs.All().Count());
    });

    /// <summary>
    /// <b>Die Auflage wird kopiert und nicht durchgereicht</b> (§4.32, wie beim Feld in §4.49).
    ///
    /// <para>
    /// Läge dasselbe <see cref="TdChart"/> im alten <b>und</b> im neuen Modell, änderte ein
    /// späterer Griff — eine neue Reihe, eine andere Farbe — die Sicherung des Verlaufs mit.
    /// <b>Der Fehler fiele erst beim Rückgängigmachen auf</b>, und dann als „das Rückgängig
    /// funktioniert nicht" und nicht als das, was er ist.
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_Diagramm_kommt_als_eigenes_Stueck_zurueck() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-diagramm-kopie");

        var quelle = Eingeboren();
        var diagramm = new TdChart(TdChartKind.Column, 8, 5) { Title = "Noten" };
        diagramm.Series.Add(new TdChartSeries("Halbjahr", 2, 3));
        quelle.Sections[0].Blocks.Add(new TdParagraph(new TdInline[] { diagramm }));

        var zurueck = Assert.Single(Rundreise(quelle, werkbank).Blocks()
            .OfType<TdParagraph>().SelectMany(p => p.Inlines).OfType<TdChart>());

        Assert.NotSame(diagramm, zurueck);
        Assert.NotSame(diagramm.Series, zurueck.Series);
        Assert.NotSame(diagramm.Series[0].Values, zurueck.Series[0].Values);
    });

    /// <summary>
    /// <b>✅ Jede Feldart überlebt die Rundreise — seit §4.49.</b>
    ///
    /// <para>
    /// <b>Hier stand die zweite benannte Lücke</b>, und sie war die einzige, die ein Nutzer
    /// heute wirklich auslösen konnte: Der Linux-Kopf kann alle fünf Feldarten einfügen. Ein
    /// Feld wurde beim Weg durch den WPF-Editor zu einem gewöhnlichen <c>Run</c> mit seinem
    /// Platzhaltertext — <b>aus einer Seitenzahl, die sich rechnet, wurde Text, der
    /// stehenbleibt</b>, still und dauerhaft.
    /// </para>
    /// <para>
    /// <b>Alle fünf Arten, und nicht nur eine.</b> Der frühere Wächter prüfte
    /// <c>PageNumber</c>; gemessen wurde erst beim Bauen, dass <c>TableOfContents</c> einen
    /// **anderen** Weg nimmt (Blockebene statt Stück) und deshalb auch einen anderen Träger
    /// braucht. Ein Wächter über eine Art hätte die vier anderen nicht gedeckt.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData(TdFieldKind.PageNumber)]
    [InlineData(TdFieldKind.PageCount)]
    [InlineData(TdFieldKind.Date)]
    [InlineData(TdFieldKind.Title)]
    [InlineData(TdFieldKind.TableOfContents)]
    public void Ein_Feld_ueberlebt_die_Rundreise(TdFieldKind art) => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank($"rundreise-feld-{art}");

        var quelle = Eingeboren();

        // Eine Überschrift, damit ein Inhaltsverzeichnis überhaupt Einträge hätte — sonst
        // prüfte der Fall nur den leeren Behälter.
        var h1 = new TdParagraph("Kapitel eins");
        h1.Format.OutlineLevel = 1;
        h1.CharFormat.Bold = true;
        h1.CharFormat.FontSize = TdStil.ZurEbene(1)!.Value.SizePt;
        quelle.Sections[0].Blocks.Insert(0, h1);

        quelle.Sections[0].Blocks.Add(new TdParagraph(new TdInline[]
        {
            new TdField { Kind = art },
        }));

        var felder = Rundreise(quelle, werkbank).Blocks()
            .OfType<TdParagraph>().SelectMany(p => p.Inlines).OfType<TdField>().ToList();

        var feld = Assert.Single(felder);
        Assert.Equal(art, feld.Kind);
    });

    /// <summary>
    /// <b>Und der Platzhaltertext bleibt nicht als Text zurück.</b> Die andere Hälfte des
    /// Wächters darüber: Käme das Feld <i>und</i> sein <c>{SEITE}</c> zurück, stünde die
    /// Seitenzahl zweimal da — einmal gerechnet und einmal eingefroren.
    /// </summary>
    [Fact]
    public void Der_Platzhalter_bleibt_nicht_als_Text_stehen() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-feld-text");

        var quelle = Eingeboren();
        quelle.Sections[0].Blocks.Add(new TdParagraph(new TdInline[]
        {
            new TdField { Kind = TdFieldKind.PageNumber },
        }));

        string text = TdMarkdown.Schreiben(Rundreise(quelle, werkbank));

        Assert.DoesNotContain("{SEITE}", text);
    });

    /// <summary>
    /// <b>✅ Die Gliederungsebene überlebt die Rundreise — seit §4.46, und ohne einen einzigen
    /// Träger.</b>
    ///
    /// <para>
    /// <b>Hier stand bis zum 2026-08-21 die dritte benannte Lücke</b>, und sie hat sich
    /// geschlossen, ohne dass jemand sie angefasst hätte. <c>FlowZuTd</c> erkennt die Ebene an
    /// der <b>Schriftgröße</b> zurück (§4.22, „die eine Stelle, an der Raten richtig ist") —
    /// und das ging fehl, weil <c>TdStil</c> seine Größen in <b>Punkt</b> führte, die aus
    /// <c>TextStyles</c> abgeschriebenen Zahlen aber <b>Pixel</b> waren: Eine Überschrift 1 aus
    /// dem Linux-Kopf kam mit 28 pt = 37,33 px an, und <c>TextStyles.HeadingLevel</c> hielt sie
    /// gegen seine eigene 28. Sie passte nie.
    /// </para>
    /// <para>
    /// <b>Seit die Einheit stimmt, sind es 21 pt = 28 px, und sie passt genau.</b> Das ist der
    /// Beleg für den Satz aus §4.46: <b>Die Lücke war keine Eigenschaft des
    /// <c>FlowDocument</c>, sondern ein Zahlendreher</b> — sie sah nur so aus wie die beiden
    /// anderen.
    /// </para>
    /// </summary>
    [Fact]
    public void Die_Gliederungsebene_ueberlebt_die_Rundreise() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-ebene");

        var quelle = Eingeboren();
        var h1 = TdStil.ZurEbene(1)!.Value;
        var ueberschrift = new TdParagraph("Kapitel eins");
        ueberschrift.Format.OutlineLevel = 1;
        ueberschrift.CharFormat.Bold = h1.Bold;
        ueberschrift.CharFormat.FontSize = h1.SizePt;
        quelle.Sections[0].Blocks.Insert(0, ueberschrift);

        var zurueck = Rundreise(quelle, werkbank).Paragraphs().First();

        Assert.Equal("Kapitel eins", zurueck.PlainText());
        Assert.Equal(1, zurueck.Format.OutlineLevel);
    });

    /// <summary>
    /// <b>Und die Größe kommt dabei unverändert zurück</b> — die andere Hälfte des Wächters
    /// darüber.
    ///
    /// <para>
    /// Ohne ihn bliebe offen, ob die Ebene nur deshalb wiedererkannt wird, weil die Größe auf
    /// dem Weg zufällig auf einen Vorlagenwert gerutscht ist. <b>21 pt gehen hinein und
    /// 21 pt kommen heraus</b>, über 28 geräteunabhängige Pixel in der Mitte.
    /// </para>
    /// </summary>
    [Fact]
    public void Eine_Ueberschrift_behaelt_ihre_Groesse() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-groesse");

        var quelle = Eingeboren();
        var h1 = TdStil.ZurEbene(1)!.Value;
        var ueberschrift = new TdParagraph("Kapitel eins");
        ueberschrift.Format.OutlineLevel = 1;
        ueberschrift.CharFormat.Bold = h1.Bold;
        ueberschrift.CharFormat.FontSize = h1.SizePt;
        quelle.Sections[0].Blocks.Insert(0, ueberschrift);

        var zurueck = Rundreise(quelle, werkbank).Paragraphs().First();

        Assert.Equal(h1.SizePt, zurueck.CharFormat.FontSize!.Value, 6);
    });
}
