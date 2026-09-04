using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdMarkdown.Lesen(string, ITdImages)"/> — der Markdown-Import, seit Phase 5,
/// Schritt ④ in Core.
///
/// <para>
/// <b>Vorher lag er als <c>MarkdownImporter</c> im WPF-Kopf, mit eigener Grammatik und
/// ohne einen einzigen Wächter</b> — 394 Zeilen, die nur das laufende Programm je geprüft
/// hat. Das ist derselbe Befund wie beim Formen-Stift (§4.78): Was in einem Kopf liegt,
/// bekommt keine Tests, weil dort niemand welche schreibt.
/// </para>
///
/// <para>
/// <b>Geprüft wird, was ein falsch gelesenes Dokument wäre</b>, nicht wie es aussieht: eine
/// Überschrift ohne Gliederungsebene (dann fehlt sie im Inhaltsverzeichnis und im Navigator),
/// eine Unterliste, die zur eigenen Liste wird, ein Verweis ohne Ziel, eine innere
/// Auszeichnung, die beim Flachmachen verloren geht — und ein Bild, das stillschweigend
/// verschwindet.
/// </para>
/// </summary>
public sealed class MarkdownImportTests
{
    /// <summary>
    /// Ein Bildspeicher, der nur mitschreibt. <b>Er merkt sich die Reihenfolge</b>, damit ein
    /// Wächter sagen kann, dass die Bytes wirklich <i>diese</i> Datei waren.
    /// </summary>
    private sealed class Merkspeicher : ITdImages
    {
        private readonly Dictionary<Guid, byte[]> _daten = new();

        public List<string> Endungen { get; } = new();

        public byte[]? Lesen(Guid id) => _daten.TryGetValue(id, out var d) ? d : null;

        public Guid Ablegen(byte[] daten, string endung)
        {
            var id = Guid.NewGuid();
            _daten[id] = daten;
            Endungen.Add(endung);
            return id;
        }
    }

    private static TdDocument Lesen(string markdown, string basisOrdner = "") =>
        TdMarkdown.Lesen(markdown, basisOrdner, new Merkspeicher());

    private static List<TdParagraph> Absaetze(TdDocument doc) => [.. doc.Paragraphs()];

    // ==================== Blöcke ====================

    /// <summary>
    /// <b>Eine Überschrift bekommt ihre Gliederungsebene</b> — und daran hängt mehr als das
    /// Aussehen: Inhaltsverzeichnis (§4.20) und Navigator (§4.85) lesen
    /// <see cref="TdParaFormat.OutlineLevel"/>. Ohne sie sähe die Zeile aus wie eine
    /// Überschrift und stünde in beidem nicht.
    /// </summary>
    [Theory]
    [InlineData("# eins", 1)]
    [InlineData("## zwei", 2)]
    [InlineData("### drei", 3)]
    [InlineData("#### vier", 4)]
    public void Eine_Ueberschrift_traegt_ihre_Ebene(string markdown, int ebene)
    {
        var absatz = Assert.Single(Absaetze(Lesen(markdown)));

        Assert.Equal(ebene, absatz.Format.OutlineLevel);
        Assert.Equal(TdStil.ZurEbene(ebene)!.Value.SizePt, absatz.CharFormat.FontSize);
    }

    /// <summary>
    /// <b>Eine fünfte Ebene gibt es nicht, und dann bleibt es ein Absatz.</b>
    /// <see cref="TdStil"/> kennt vier — eine erfundene fünfte wäre eine Überschrift, die
    /// keine Vorlage hat und die niemand wieder zurücksetzen kann.
    /// </summary>
    [Fact]
    public void Eine_fuenfte_Ebene_bleibt_ein_gewoehnlicher_Absatz()
    {
        var absatz = Assert.Single(Absaetze(Lesen("##### tief")));

        Assert.Null(absatz.Format.OutlineLevel);
        Assert.Equal("tief", absatz.PlainText());
    }

    /// <summary>
    /// <b>Die Unterliste wird ein Geschwister mit höherer Ebene und keine eigene Liste.</b>
    /// Im Zerleger ist sie ein Kind des Punktes, im Modell hängt die Verschachtelung an
    /// <see cref="TdListRef.Level"/> (§4.17) — wer das eins zu eins übernähme, bekäme eine
    /// zweite Aufzählung, die wieder bei „1." anfängt.
    /// </summary>
    [Fact]
    public void Eine_Unterliste_wird_zur_tieferen_Ebene_derselben_Liste()
    {
        var absaetze = Absaetze(Lesen("- eins\n  - innen\n- zwei"));

        Assert.Equal(3, absaetze.Count);
        Assert.Equal(0, absaetze[0].List!.Level);
        Assert.Equal(1, absaetze[1].List!.Level);
        Assert.Equal(0, absaetze[2].List!.Level);

        // Alle drei zeigen auf dieselbe Definition.
        Assert.Single(absaetze.Select(a => a.List!.ListId).Distinct());
    }

    /// <summary>
    /// <b>Zwei Aufzählungsarten, zwei Definitionen — aber nur zwei</b>, auch bei vier Listen
    /// im Dokument. Markdown unterscheidet nicht mehr, und eine Definition je Liste wären
    /// mehrere Wege, dasselbe zu sagen.
    /// </summary>
    [Fact]
    public void Es_gibt_je_Aufzaehlungsart_genau_eine_Definition()
    {
        var doc = Lesen("- a\n\ntext\n\n1. b\n\ntext\n\n- c\n\ntext\n\n2. d");

        Assert.Equal(2, doc.Lists.Count);
    }

    /// <summary>Eine Pipe-Tabelle wird eine Tabelle, mit Kopfzeile und vollem Raster.</summary>
    [Fact]
    public void Eine_Pipe_Tabelle_wird_eine_Tabelle_mit_Raster()
    {
        var doc = Lesen("| a | b |\n| --- | --- |\n| 1 | 2 |");

        var tabelle = Assert.IsType<TdTable>(Assert.Single(doc.Sections[0].Blocks));

        Assert.Equal(2, tabelle.Rows.Count);
        Assert.True(tabelle.Rows[0].IsHeader);
        Assert.False(tabelle.Rows[1].IsHeader);

        // **Ohne Raster setzt der Umbruch die Tabelle nicht** (§4.19) — Markdown sagt über
        // Breiten nichts, also müssen sie hier entstehen.
        Assert.Equal(2, tabelle.ColumnWidthsCm.Count);
        Assert.All(tabelle.ColumnWidthsCm, b => Assert.True(b > 0));
    }

    /// <summary>
    /// <b>Eine Zeile mit zu wenig Zellen wird aufgefüllt und nicht abgeschnitten.</b> Zu wenig
    /// Pipes ist in Markdown erlaubt; eine fehlende Zelle ließe das Raster verrutschen, und
    /// der Inhalt stünde danach in der falschen Spalte.
    /// </summary>
    [Fact]
    public void Eine_kurze_Zeile_wird_auf_die_Spaltenzahl_aufgefuellt()
    {
        var doc = Lesen("| a | b | c |\n| --- | --- | --- |\n| 1 |");

        var tabelle = Assert.IsType<TdTable>(Assert.Single(doc.Sections[0].Blocks));

        Assert.Equal(3, tabelle.Rows[1].Cells.Count);
        Assert.Equal("1", tabelle.Rows[1].Cells[0].Blocks[0].PlainText());
        Assert.Equal("", tabelle.Rows[1].Cells[2].Blocks[0].PlainText());
    }

    /// <summary>
    /// Ein Code-Block wird <b>ein</b> Absatz mit Umbrüchen darin — sonst bekäme jede Zeile den
    /// Absatzabstand, und aus zehn Zeilen Code würde eine Leiter mit Lücken.
    /// </summary>
    [Fact]
    public void Ein_Codeblock_bleibt_ein_Absatz()
    {
        var absatz = Assert.Single(Absaetze(Lesen("```\neins\nzwei\n```")));

        Assert.Single(absatz.Inlines.OfType<TdLineBreak>());
        Assert.Contains("eins", absatz.PlainText());
        Assert.Contains("zwei", absatz.PlainText());
    }

    /// <summary>
    /// Die Trennlinie ist <b>ein Absatz mit Unterrahmen</b> und kein eigener Blocktyp — genau
    /// das, was <c>TdBlockEdit.Trennlinie</c> baut (§4.40). Zwei Wege dürfen nicht zwei
    /// verschiedene Trennlinien ergeben.
    /// </summary>
    [Fact]
    public void Eine_Trennlinie_ist_ein_Absatz_mit_Unterrahmen()
    {
        var absatz = Assert.Single(Absaetze(Lesen("---")));

        Assert.NotNull(absatz.Format.BottomBorder);
        Assert.Equal("", absatz.PlainText());
    }

    /// <summary>Ein Zitat bekommt die Vorlage „Zitat" — den Einzug, den das Modell dafür hat.</summary>
    [Fact]
    public void Ein_Zitat_bekommt_die_Vorlage_Zitat()
    {
        var absatz = Assert.Single(Absaetze(Lesen("> geliehen")));

        Assert.Equal("geliehen", absatz.PlainText());
        Assert.Equal(TdStil.MitNamen("Zitat")!.Value.LeftCm, absatz.Format.LeftIndentCm);
    }

    /// <summary>
    /// <b>Eine leere Datei gibt ein leeres Dokument und keinen Wurf</b> — und das Dokument hat
    /// trotzdem einen Absatz: Ohne ihn hätte die Schreibmarke keinen Ort (§4.19, dieselbe
    /// Überlegung wie bei der leeren Zelle).
    /// </summary>
    [Fact]
    public void Eine_leere_Datei_gibt_ein_Dokument_mit_einem_leeren_Absatz()
    {
        var doc = Lesen("");

        Assert.Equal("", Assert.Single(Absaetze(doc)).PlainText());
    }

    // ==================== Stücke ====================

    /// <summary>
    /// <b>Die innere Auszeichnung überlebt das Flachmachen</b> — das ist der eine Fall, den
    /// ein Importer falsch macht, der erst flach macht und dann auszeichnet (§7, Erbfolge).
    /// Aus <c>**fett mit *kursiv* darin**</c> müssen drei Läufe werden, und der mittlere ist
    /// beides.
    /// </summary>
    [Fact]
    public void Fett_um_Kursiv_ergibt_ein_Stueck_das_beides_ist()
    {
        var absatz = Assert.Single(Absaetze(Lesen("**fett mit *kursiv* darin**")));

        var stuecke = absatz.Inlines.OfType<TdRun>().ToList();

        Assert.All(stuecke, s => Assert.True(s.Format.Bold));
        Assert.Single(stuecke, s => s.Format.Italic == true && s.Text == "kursiv");
    }

    /// <summary>Durchgestrichenes kommt als <see cref="TdCharFormat.Strikethrough"/> an.</summary>
    [Fact]
    public void Durchgestrichenes_wird_ein_Format_und_keine_Tilde()
    {
        var absatz = Assert.Single(Absaetze(Lesen("a ~~weg~~ b")));

        Assert.Equal("a weg b", absatz.PlainText());
        Assert.Single(absatz.Inlines.OfType<TdRun>(), s => s.Format.Strikethrough == true);
    }

    /// <summary>
    /// <b>Ein Verweis ist eine Klammer und kein Lauf mit Zusatzfeld</b> (§4.20) — und sein
    /// Ziel bleibt <b>wörtlich</b> stehen: Ein relativer Verweis, der zum absoluten
    /// <c>file:///</c>-Pfad wird, ist genau der Fehler, an dem der Export einmal gescheitert
    /// ist (§7, „Markdown-Export").
    /// </summary>
    [Fact]
    public void Ein_Verweis_behaelt_sein_Ziel_woertlich()
    {
        var absatz = Assert.Single(Absaetze(Lesen("siehe [Kapitel 2](kapitel-2.md)")));

        var verweis = Assert.Single(absatz.Inlines.OfType<TdHyperlink>());

        Assert.Equal("kapitel-2.md", verweis.Target);
        Assert.Equal("Kapitel 2", verweis.Inlines[0].PlainText());
    }

    /// <summary>Inline-Code bekommt die feste Schrift und bleibt sonst Text.</summary>
    [Fact]
    public void Inline_Code_bekommt_die_feste_Schrift()
    {
        var absatz = Assert.Single(Absaetze(Lesen("ruf `Lesen()` auf")));

        var code = Assert.Single(absatz.Inlines.OfType<TdRun>(), s => s.Text == "Lesen()");

        Assert.Equal(
            GonkNote.Core.Theming.Fonts.Standard.Family(GonkNote.Core.Theming.FontRole.Mono),
            code.Format.FontFamily);
    }

    // ==================== Bilder ====================

    /// <summary>
    /// <b>Ein lokales Bild kommt mit seinen Originalbytes in den Blob-Speicher</b> — und zwar
    /// unverändert (§4.21: neu kodiert wurde in V1 aus 2 MB Vorlage ein 16,8-MB-Export).
    /// </summary>
    [Fact]
    public void Ein_lokales_Bild_landet_unveraendert_im_Blob_Speicher()
    {
        using var ordner = new Wegwerfordner();
        byte[] bytes = [1, 2, 3, 4, 5];
        ordner.Schreiben("foto.png", bytes);

        var speicher = new Merkspeicher();
        var doc = TdMarkdown.Lesen("![Der Aufbau](foto.png)", ordner.Pfad, speicher);

        var bild = Assert.Single(Absaetze(doc)[0].Inlines.OfType<TdImage>());

        Assert.Equal(bytes, speicher.Lesen(bild.BlobId));
        Assert.Equal("png", bild.Extension);
        Assert.Equal("Der Aufbau", bild.AltText);
    }

    /// <summary>
    /// <b>Ein Bild, das es nicht gibt, wird sein Ersatztext — und verschwindet nicht.</b>
    ///
    /// <para>
    /// Das ist der Fall, der ohne Wächter still bliebe: Ein weggelassenes Bild sieht aus, als
    /// hätte im Dokument nie eines gestanden. Der Ersatztext sagt, dass dort etwas war.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData("![Foto](gibtsnicht.png)", "[Foto]")]
    [InlineData("![](gibtsnicht.png)", "[Bild]")]
    [InlineData("![Netz](https://example.com/a.png)", "[Netz]")]
    public void Ein_nicht_ladbares_Bild_wird_sein_Ersatztext(string markdown, string erwartet)
    {
        var absatz = Assert.Single(Absaetze(Lesen(markdown)));

        Assert.Empty(absatz.Inlines.OfType<TdGraphic>());
        Assert.Equal(erwartet, absatz.PlainText());
    }

    /// <summary>
    /// <b>Ohne Basisordner wird gar nicht erst gesucht</b> — der Fall, in dem der Text nicht
    /// aus einer Datei kommt. Ein relativer Pfad hätte dann nichts, wogegen er gilt, und ein
    /// Versuch gegen das Arbeitsverzeichnis läse eine fremde Datei.
    /// </summary>
    [Fact]
    public void Ohne_Basisordner_bleibt_jedes_Bild_sein_Ersatztext()
    {
        using var ordner = new Wegwerfordner();
        ordner.Schreiben("foto.png", [1, 2, 3]);

        var absatz = Assert.Single(Absaetze(Lesen("![x](foto.png)")));

        Assert.Empty(absatz.Inlines.OfType<TdGraphic>());
    }

    // ==================== Die Rundreise ====================

    /// <summary>
    /// <b>Export und Import sind Gegenrichtungen, und das ist hier gemessen und nicht
    /// behauptet.</b> Geprüft wird an dem, was <see cref="TdMarkdown.Schreiben"/> wirklich
    /// schreibt — Überschriftsebenen, Listenebenen, Auszeichnungen und das Verweisziel.
    ///
    /// <para>
    /// <b>Was Markdown nicht kann, steht bewusst nicht darin</b> (Seiten, Kopfzeilen, Farben,
    /// Ränder): Die Rundreise misst, was das Format trägt, nicht was das Modell hat.
    /// </para>
    /// </summary>
    [Fact]
    public void Was_geschrieben_wurde_kommt_wieder_herein()
    {
        var doc = new TdDocument();
        doc.Lists.Add(TdListDefinition.Punkte(1));

        var abschnitt = new TdSection();
        var ueberschrift = new TdParagraph("Kapitel");
        TdStil.ZurEbene(2)!.Value.AufAbsatz(ueberschrift);

        abschnitt.Blocks.Add(ueberschrift);
        abschnitt.Blocks.Add(new TdParagraph(new TdInline[]
        {
            new TdRun("mit "),
            new TdRun("Nachdruck") { Format = { Bold = true } },
            new TdRun(" und "),
            TdHyperlink.Text("ziel.md", "einem Verweis"),
        }));
        abschnitt.Blocks.Add(new TdParagraph("Punkt") { List = new TdListRef(1, 0) });
        doc.Sections.Add(abschnitt);

        var zurueck = Absaetze(Lesen(TdMarkdown.Schreiben(doc)));

        Assert.Equal(2, zurueck[0].Format.OutlineLevel);
        Assert.Equal("Kapitel", zurueck[0].PlainText());

        Assert.Equal("mit Nachdruck und einem Verweis", zurueck[1].PlainText());
        Assert.Single(zurueck[1].Inlines.OfType<TdRun>(), s => s.Format.Bold == true);
        Assert.Equal("ziel.md", Assert.Single(zurueck[1].Inlines.OfType<TdHyperlink>()).Target);

        Assert.Equal("Punkt", zurueck[2].PlainText());
        Assert.NotNull(zurueck[2].List);
    }

    // ==================== Hilfsmittel ====================

    private sealed class Wegwerfordner : IDisposable
    {
        public string Pfad { get; } = Directory.CreateTempSubdirectory("gonk-md-").FullName;

        public void Schreiben(string name, byte[] daten) =>
            File.WriteAllBytes(Path.Combine(Pfad, name), daten);

        public void Dispose()
        {
            try { Directory.Delete(Pfad, true); } catch { /* aufräumen darf scheitern */ }
        }
    }
}
