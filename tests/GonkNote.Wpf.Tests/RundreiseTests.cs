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
/// Fußnote, damit sie nicht still verschwinden (§4.19).
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

    // ==================== Die drei benannten Lücken ====================

    /// <summary>
    /// <b>⚠ Benannte Lücke 1: ein Diagramm überlebt die Rundreise nicht.</b>
    ///
    /// <para>
    /// Das <c>FlowDocument</c> kennt nur Bilder, und der Träger, an dem ein Diagramm hängen
    /// könnte, überlebt das <c>XamlPackage</c> nicht (gemessen, §4.45: <c>Tag</c> und
    /// <c>ToolTip</c> an <c>Run</c> und <c>Paragraph</c> kommen als <c>null</c> zurück; nur ein
    /// <c>ToolTip</c> an einem <c>Image</c> übersteht es — genau deshalb trägt
    /// <c>DocumentImages</c> ihn dort und nirgends sonst).
    /// </para>
    /// <para>
    /// <b>Dieser Wächter hält den Verlust fest, damit er nicht still verschwindet</b> (§4.19).
    /// <b>Wer die Lücke schließt, dreht ihn um</b> — er ist die Stelle, an der es auffällt.
    /// </para>
    /// </summary>
    [Fact]
    public void Noch_offen_Ein_Diagramm_geht_auf_der_Rundreise_verloren() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-diagramm");

        var quelle = Eingeboren();
        quelle.Sections[0].Blocks.Add(new TdParagraph(new TdInline[]
        {
            new TdChart { Title = "Noten", WidthCm = 8, HeightCm = 5 },
        }));

        Assert.Empty(Rundreise(quelle, werkbank).Blocks()
            .OfType<TdParagraph>().SelectMany(p => p.Inlines).OfType<TdChart>());
    });

    /// <summary>
    /// <b>⚠ Benannte Lücke 2: ein Feld wird zu seinem Platzhaltertext.</b> Der Editor kennt
    /// keine Felder, also steht dort <c>{SEITE}</c> als gewöhnlicher Text — und genau als
    /// solcher kommt es zurück. <b>Aus einer Seitenzahl, die sich rechnet, wird Text, der
    /// stehenbleibt.</b>
    /// <inheritdoc cref="Noch_offen_Ein_Diagramm_geht_auf_der_Rundreise_verloren" path="/summary/para[2]"/>
    /// </summary>
    [Fact]
    public void Noch_offen_Ein_Feld_wird_zu_Text() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("rundreise-feld");

        var quelle = Eingeboren();
        quelle.Sections[0].Blocks.Add(new TdParagraph(new TdInline[]
        {
            new TdField { Kind = TdFieldKind.PageNumber },
        }));

        var stuecke = Rundreise(quelle, werkbank).Blocks()
            .OfType<TdParagraph>().SelectMany(p => p.Inlines).ToList();

        Assert.Empty(stuecke.OfType<TdField>());
        Assert.Contains(stuecke.OfType<TdRun>(), r => r.Text.Contains("{SEITE}"));
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
