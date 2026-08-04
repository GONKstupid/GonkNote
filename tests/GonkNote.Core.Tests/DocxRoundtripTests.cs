using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdDocx"/> — der DOCX-Roundtrip gegen das eigene Dokumentmodell.
///
/// <para>
/// <b>Das Tor, das die Roadmap nach jedem Schritt der Phase 4 verlangt</b> („Nach jedem
/// Schritt muss der DOCX-Roundtrip-Test grün sein"). Der Grund steht in derselben Zeile:
/// Phase 4 ist die, an der Projekte sterben, und ein Modell ohne Gegenprobe wächst so lange
/// weiter, bis niemand mehr weiß, welcher Teil davon je funktioniert hat.
/// </para>
///
/// <para>
/// <b>Warum DOCX und nicht der eigene Json-Roundtrip</b> (den prüft
/// <see cref="DokumentmodellTests"/>): DOCX ist ein **fremdes** Format. Es kennt die eigenen
/// Bequemlichkeiten nicht und deckt genau das auf, was beim eigenen Roundtrip nur deshalb
/// stimmt, weil Schreiben und Lesen denselben Fehler machen — vertauschte Einheiten,
/// verlorene Vorzeichen, „nicht gesetzt" gegen „auf Standard gesetzt".
/// </para>
/// </summary>
public sealed class DocxRoundtripTests
{
    /// <summary>Ein Wegwerf-Ordner je Test; nichts davon kommt ins Repo.</summary>
    private sealed class Werkbank : IDisposable
    {
        public string Ordner { get; }

        public Werkbank(string name)
        {
            Ordner = Path.Combine(Path.GetTempPath(), $"gonk-docx-{name}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Ordner);
        }

        public string Datei(string name) => Path.Combine(Ordner, name);

        public void Dispose()
        {
            try { Directory.Delete(Ordner, recursive: true); } catch { /* Wegwerf */ }
        }
    }

    // ==================== Das Tor ====================

    /// <summary>
    /// Alles, was Schritt 1 ausdrücken kann, geht durch DOCX und kommt gleich wieder heraus.
    /// <b>Wer das Modell erweitert, erweitert <see cref="Beispiel"/> mit</b> — sonst bewacht
    /// dieser Test das neue Feld nicht.
    /// </summary>
    [Fact]
    public void Ein_Dokument_uebersteht_den_DOCX_Roundtrip()
    {
        using var werkbank = new Werkbank("roundtrip");
        string pfad = werkbank.Datei("referenz.docx");
        var original = Beispiel();

        TdDocx.Schreiben(original, pfad);
        var zurueck = TdDocx.Lesen(pfad);

        GleichesDokument(original, zurueck);
    }

    /// <summary>
    /// <b>Ein Dokument, das Word nicht öffnet, ist kein Export.</b> Dieselbe Messlatte, die
    /// der heutige <c>DocxExporter</c> anlegt — und der Grund, warum die Reihenfolge der
    /// Kindelemente in <c>TdDocx</c> Schema ist und keine Geschmacksfrage.
    /// </summary>
    [Fact]
    public void Das_erzeugte_DOCX_haelt_das_Office_Schema_ein()
    {
        using var werkbank = new Werkbank("schema");
        string pfad = werkbank.Datei("referenz.docx");

        TdDocx.Schreiben(Beispiel(), pfad);

        Assert.Equal(0, TdDocx.Pruefen(pfad));
    }

    // ==================== Die Stellen, an denen es sonst schiefgeht ====================

    /// <summary>
    /// Einzüge stehen im Modell in Zentimetern und in DOCX in Twips. Ein vertauschter Faktor
    /// fällt bei „0" nicht auf und bei „2 cm" erst am Lineal — deshalb hier mit einem Wert,
    /// dessen Umrechnung nicht glatt aufgeht.
    /// <para>
    /// Verglichen wird auf zwei Stellen, weil ein Twip 0,0018 cm ist — die Begründung steht
    /// bei <see cref="GleicheZahlCm"/>.
    /// </para>
    /// </summary>
    [Fact]
    public void Einzuege_ueberstehen_die_Umrechnung_in_Twips()
    {
        var doc = MitAbsatzformat(new TdParaFormat { LeftIndentCm = 1.5, RightIndentCm = 0.75 });

        var f = Zurueck(doc).Paragraphs().First().Format;

        Assert.Equal(1.5, f.LeftIndentCm!.Value, 2);
        Assert.Equal(0.75, f.RightIndentCm!.Value, 2);
    }

    /// <summary>
    /// Der hängende Einzug ist der Fall, an dem ein Vorzeichen verlorengeht: DOCX kennt dafür
    /// **zwei** Felder (<c>firstLine</c> und <c>hanging</c>), beide positiv. Wer nur eines
    /// schreibt, bekommt aus −0,5 cm ein +0,5 cm — und aus einer Aufzählung einen Einzug in
    /// die falsche Richtung.
    /// </summary>
    [Fact]
    public void Ein_haengender_Einzug_behaelt_sein_Vorzeichen()
    {
        var einziehend = Zurueck(MitAbsatzformat(new TdParaFormat { FirstLineIndentCm = 1.0 }));
        var haengend = Zurueck(MitAbsatzformat(new TdParaFormat { FirstLineIndentCm = -0.5 }));

        Assert.Equal(1.0, einziehend.Paragraphs().First().Format.FirstLineIndentCm!.Value, 2);
        Assert.Equal(-0.5, haengend.Paragraphs().First().Format.FirstLineIndentCm!.Value, 2);
    }

    /// <summary>Schriftgrade stehen in DOCX als **halbe** Punkt — auch die ungeraden.</summary>
    [Theory]
    [InlineData(11.0)]
    [InlineData(10.5)]
    [InlineData(20.0)]
    public void Schriftgrade_ueberstehen_die_halben_Punkt(double pt)
    {
        var doc = MitZeichenformat(new TdCharFormat { FontSize = pt });

        var f = ErstesStueck(Zurueck(doc));

        Assert.Equal(pt, f.FontSize!.Value, 3);
    }

    /// <summary>
    /// <b>Ein <c>&lt;w:b/&gt;</c> ohne <c>val</c> heißt „an".</b> Das ist die Stelle, an der
    /// ein naives <c>Val?.Value ?? false</c> jede fette Stelle eines fremden Dokuments still
    /// normal machen würde — und zwar nur bei Dateien aus Word, nie bei den eigenen.
    /// </summary>
    [Fact]
    public void Eine_Auszeichnung_ohne_Wert_gilt_als_gesetzt()
    {
        using var werkbank = new Werkbank("bold-ohne-val");
        string pfad = werkbank.Datei("fremd.docx");

        // So schreibt Word es: das Element steht da, ein Wert nicht.
        TdDocx.Schreiben(MitZeichenformat(new TdCharFormat { Bold = true }), pfad);
        WertAmElementEntfernen(pfad, "b");

        Assert.True(ErstesStueck(TdDocx.Lesen(pfad)).Bold);
    }

    /// <summary>
    /// „Nicht gesetzt" und „ausdrücklich aus" sind zweierlei, und DOCX kann beides: kein
    /// Element gegen <c>&lt;w:b w:val="0"/&gt;</c>. Ginge das verloren, ließe sich Fett
    /// innerhalb einer fetten Überschrift nie wieder abschalten.
    /// </summary>
    [Fact]
    public void Nicht_gesetzt_und_ausdruecklich_aus_bleiben_unterscheidbar()
    {
        Assert.Null(ErstesStueck(Zurueck(MitZeichenformat(new TdCharFormat()))).Bold);
        Assert.False(ErstesStueck(Zurueck(MitZeichenformat(new TdCharFormat { Bold = false }))).Bold);
        Assert.True(ErstesStueck(Zurueck(MitZeichenformat(new TdCharFormat { Bold = true }))).Bold);
    }

    /// <summary>
    /// Dasselbe für die Hervorhebung: <c>null</c> = nicht gesetzt, <c>""</c> = ausdrücklich
    /// keine. In DOCX ist Letzteres die Füllung „auto" und nicht das Fehlen des Elements.
    /// </summary>
    [Fact]
    public void Eine_abgeschaltete_Hervorhebung_ist_nicht_dasselbe_wie_keine_Angabe()
    {
        Assert.Null(ErstesStueck(Zurueck(MitZeichenformat(new TdCharFormat()))).Highlight);
        Assert.Equal("", ErstesStueck(Zurueck(MitZeichenformat(new TdCharFormat { Highlight = "" }))).Highlight);
        Assert.Equal("#FFFF00", ErstesStueck(Zurueck(MitZeichenformat(new TdCharFormat { Highlight = "#FFFF00" }))).Highlight);
    }

    /// <summary>
    /// Führende und mehrfache Leerzeichen überleben nur mit <c>xml:space="preserve"</c>.
    /// Ohne das säße der Text nach dem Roundtrip zusammengeschoben da — und zwar erst nach
    /// dem Speichern, nicht beim Tippen.
    /// </summary>
    [Fact]
    public void Leerzeichen_am_Rand_bleiben_stehen()
    {
        var doc = new TdDocument { Blocks = { new TdParagraph("  zwei  Leerzeichen  ") } };

        Assert.Equal("  zwei  Leerzeichen  ", Zurueck(doc).PlainText());
    }

    /// <summary>
    /// Ein Seitenumbruch ist in DOCX ein Absatz mit einem Umbruch-Lauf. Ein Absatz mit
    /// Umbruch **und** Text ist keiner — wer ihn dafür hält, verliert seinen Text.
    /// </summary>
    [Fact]
    public void Ein_Seitenumbruch_bleibt_ein_Seitenumbruch_und_frisst_keinen_Text()
    {
        var doc = new TdDocument
        {
            Blocks = { new TdParagraph("davor"), new TdPageBreak(), new TdParagraph("danach") },
        };

        var zurueck = Zurueck(doc);

        Assert.Collection(zurueck.Blocks,
            b => Assert.Equal("davor", Assert.IsType<TdParagraph>(b).PlainText()),
            b => Assert.IsType<TdPageBreak>(b),
            b => Assert.Equal("danach", Assert.IsType<TdParagraph>(b).PlainText()));
    }

    /// <summary>
    /// Der Zeilenumbruch **innerhalb** eines Absatzes (Umschalt+Eingabe) darf nicht zu einem
    /// zweiten Absatz werden: die Absatzabstände gelten weiter, und ein Aufzählungspunkt
    /// bekäme sonst eine zweite Nummer.
    /// </summary>
    [Fact]
    public void Ein_Zeilenumbruch_wird_nicht_zu_einem_zweiten_Absatz()
    {
        var doc = new TdDocument
        {
            Blocks = { new TdParagraph([new TdRun("oben"), new TdLineBreak(), new TdRun("unten")]) },
        };

        var zurueck = Zurueck(doc);

        var absatz = Assert.IsType<TdParagraph>(Assert.Single(zurueck.Blocks));
        Assert.Collection(absatz.Inlines,
            i => Assert.IsType<TdRun>(i),
            i => Assert.IsType<TdLineBreak>(i),
            i => Assert.IsType<TdRun>(i));
    }

    /// <summary>
    /// Die Gliederungsebene ist die Wahrheit über „ist das eine Überschrift?" — nicht die
    /// Schriftgröße. Word zählt ab 0 und benutzt 9 für Fließtext; die eigene 0 ist genau das,
    /// und beide Richtungen müssen sich decken.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void Die_Gliederungsebene_ueberlebt_Words_Zaehlweise(int ebene)
    {
        var doc = MitAbsatzformat(new TdParaFormat { OutlineLevel = ebene });

        Assert.Equal(ebene, Zurueck(doc).Paragraphs().First().Format.OutlineLevel);
    }

    /// <summary>
    /// Die Grundformate des Dokuments gehören nach <c>docDefaults</c> und nicht in jeden
    /// Absatz hineinkopiert — sonst wäre die Kaskade nach dem ersten Export verloren, und
    /// eine Änderung an der Grundschrift ginge an allen Absätzen vorbei.
    /// </summary>
    [Fact]
    public void Die_Grundformate_bleiben_Grundformate()
    {
        var doc = new TdDocument
        {
            DefaultCharFormat = { FontFamily = "Calibri", FontSize = 11 },
            DefaultParaFormat = { SpaceAfterPt = 6, LineSpacing = 1.15 },
            Blocks = { new TdParagraph("schlicht") },
        };

        var zurueck = Zurueck(doc);

        Assert.Equal("Calibri", zurueck.DefaultCharFormat.FontFamily);
        Assert.Equal(11, zurueck.DefaultCharFormat.FontSize!.Value, 3);
        Assert.Equal(6, zurueck.DefaultParaFormat.SpaceAfterPt!.Value, 3);
        Assert.Equal(1.15, zurueck.DefaultParaFormat.LineSpacing!.Value, 3);

        // Und der Absatz hat davon nichts abbekommen.
        var absatz = zurueck.Paragraphs().First();
        Assert.Null(absatz.CharFormat.FontFamily);
        Assert.Null(absatz.Format.SpaceAfterPt);
    }

    /// <summary>
    /// Ein Blocktyp, den <c>TdDocx</c> noch nicht kann, muss **werfen** und nicht still
    /// verschwinden. Ein verlorener Block fällt sonst erst dem Leser auf und nicht dem Diff —
    /// dasselbe Prinzip, aus dem ein unbekannter Whiteboard-Elementtyp beim Speichern wirft
    /// (HANDOFF §7).
    /// </summary>
    [Fact]
    public void Was_noch_nicht_geht_verschwindet_nicht_still()
    {
        using var werkbank = new Werkbank("unbekannt");
        var doc = new TdDocument { Blocks = { new NochNichtBlock() } };

        Assert.Throws<NotSupportedException>(
            () => TdDocx.Schreiben(doc, werkbank.Datei("x.docx")));
    }

    /// <summary>Steht für jeden Blocktyp, den ein späterer Schritt ergänzt.</summary>
    private sealed class NochNichtBlock : TdBlock
    {
        public override string PlainText() => "";
    }

    // ==================== Hilfsmittel ====================

    private static TdDocument Zurueck(TdDocument doc)
    {
        using var strom = new MemoryStream();
        TdDocx.Schreiben(doc, strom);
        strom.Position = 0;
        return TdDocx.Lesen(strom);
    }

    private static TdDocument MitZeichenformat(TdCharFormat f) =>
        new() { Blocks = { new TdParagraph([new TdRun("Wort", f)]) } };

    private static TdDocument MitAbsatzformat(TdParaFormat f) =>
        new() { Blocks = { new TdParagraph("Wort") { Format = f } } };

    private static TdCharFormat ErstesStueck(TdDocument doc) =>
        doc.Paragraphs().First().Inlines[0].Format;

    /// <summary>
    /// Entfernt das <c>w:val</c>-Attribut eines Elements im gespeicherten DOCX — so schreibt
    /// Word eine gesetzte Auszeichnung. Von Hand am XML, weil sich das über die
    /// OpenXml-Objekte nicht erzeugen lässt.
    /// </summary>
    private static void WertAmElementEntfernen(string pfad, string element)
    {
        using var docx = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(pfad, true);
        var teil = docx.MainDocumentPart!;

        string xml;
        using (var lesen = new StreamReader(teil.GetStream(FileMode.Open, FileAccess.Read)))
            xml = lesen.ReadToEnd();

        xml = System.Text.RegularExpressions.Regex.Replace(
            xml, $"<w:{element} w:val=\"[^\"]*\" ?/>", $"<w:{element}/>");

        using var schreiben = new StreamWriter(teil.GetStream(FileMode.Create, FileAccess.Write));
        schreiben.Write(xml);
    }

    /// <inheritdoc cref="DokumentmodellTests"/>
    private static TdDocument Beispiel() => new()
    {
        DefaultCharFormat = { FontFamily = "Calibri", FontSize = 11 },
        DefaultParaFormat = { SpaceAfterPt = 6, LineSpacing = 1.15 },
        Blocks =
        {
            new TdParagraph([new TdRun("Kapitel 1")])
            {
                Format = { OutlineLevel = 1, Alignment = TdAlign.Center, SpaceBeforePt = 12, KeepWithNext = true },
                CharFormat = { FontFamily = "Georgia", FontSize = 20, Bold = true, Color = "#1B2B4B" },
            },
            new TdParagraph(
            [
                new TdRun("Ein Absatz mit "),
                new TdRun("fett", new TdCharFormat { Bold = true }),
                new TdRun(", "),
                new TdRun("kursiv", new TdCharFormat { Italic = true }),
                new TdRun(", "),
                new TdRun("unterstrichen", new TdCharFormat { Underline = true }),
                new TdRun(", "),
                new TdRun("durchgestrichen", new TdCharFormat { Strikethrough = true }),
                new TdRun(", "),
                new TdRun("hervorgehoben", new TdCharFormat { Highlight = "#FFFF00" }),
                new TdRun(" und "),
                new TdRun("hoch", new TdCharFormat { VerticalAlign = TdVerticalAlign.Superscript }),
                new TdRun("/"),
                new TdRun("tief", new TdCharFormat { VerticalAlign = TdVerticalAlign.Subscript }),
                new TdRun("."),
                new TdLineBreak(),
                new TdRun("Zweite Zeile desselben Absatzes."),
            ])
            {
                Format =
                {
                    Alignment = TdAlign.Justify,
                    LeftIndentCm = 1.5,
                    RightIndentCm = 0.5,
                    FirstLineIndentCm = -0.5,
                    SpaceBeforePt = 3,
                    SpaceAfterPt = 9,
                    LineSpacing = 1.5,
                    OutlineLevel = 0,
                    KeepWithNext = false,
                    PageBreakBefore = false,
                },
            },
            new TdPageBreak(),
            new TdParagraph("Nach dem Umbruch.") { Format = { PageBreakBefore = true } },
        },
    };

    private static void GleichesDokument(TdDocument a, TdDocument b)
    {
        GleichesZeichenformat(a.DefaultCharFormat, b.DefaultCharFormat);
        GleichesAbsatzformat(a.DefaultParaFormat, b.DefaultParaFormat);

        Assert.Equal(a.Blocks.Count, b.Blocks.Count);
        for (int i = 0; i < a.Blocks.Count; i++)
        {
            switch (a.Blocks[i])
            {
                case TdParagraph pa:
                {
                    var pb = Assert.IsType<TdParagraph>(b.Blocks[i]);
                    GleichesZeichenformat(pa.CharFormat, pb.CharFormat);
                    GleichesAbsatzformat(pa.Format, pb.Format);

                    Assert.Equal(pa.Inlines.Count, pb.Inlines.Count);
                    for (int k = 0; k < pa.Inlines.Count; k++)
                    {
                        Assert.Equal(pa.Inlines[k].GetType(), pb.Inlines[k].GetType());
                        Assert.Equal(pa.Inlines[k].PlainText(), pb.Inlines[k].PlainText());
                        GleichesZeichenformat(pa.Inlines[k].Format, pb.Inlines[k].Format);
                    }
                    break;
                }

                case TdPageBreak:
                    Assert.IsType<TdPageBreak>(b.Blocks[i]);
                    break;

                default:
                    Assert.Fail($"Kein Vergleich für {a.Blocks[i].GetType().Name} — bitte ergänzen.");
                    break;
            }
        }
    }

    /// <summary>
    /// Zahlen mit Nachkommastellen werden **gerundet** verglichen, und zwar je Einheit
    /// unterschiedlich genau — der Weg durch DOCX ist verlustbehaftet, und das ist eine
    /// Eigenschaft des Formats und kein Fehler. Siehe <see cref="GleicheZahlCm"/>.
    /// </summary>
    private static void GleichesZeichenformat(TdCharFormat a, TdCharFormat b)
    {
        Assert.Equal(a.FontFamily, b.FontFamily);
        GleicheZahl(a.FontSize, b.FontSize);
        Assert.Equal(a.Bold, b.Bold);
        Assert.Equal(a.Italic, b.Italic);
        Assert.Equal(a.Underline, b.Underline);
        Assert.Equal(a.Strikethrough, b.Strikethrough);
        Assert.Equal(a.Color, b.Color);
        Assert.Equal(a.Highlight, b.Highlight);
        Assert.Equal(a.VerticalAlign, b.VerticalAlign);
    }

    private static void GleichesAbsatzformat(TdParaFormat a, TdParaFormat b)
    {
        Assert.Equal(a.Alignment, b.Alignment);
        GleicheZahlCm(a.LeftIndentCm, b.LeftIndentCm);
        GleicheZahlCm(a.RightIndentCm, b.RightIndentCm);
        GleicheZahlCm(a.FirstLineIndentCm, b.FirstLineIndentCm);
        GleicheZahl(a.SpaceBeforePt, b.SpaceBeforePt);
        GleicheZahl(a.SpaceAfterPt, b.SpaceAfterPt);
        GleicheZahl(a.LineSpacing, b.LineSpacing);
        Assert.Equal(a.OutlineLevel, b.OutlineLevel);
        Assert.Equal(a.KeepWithNext, b.KeepWithNext);
        Assert.Equal(a.PageBreakBefore, b.PageBreakBefore);
    }

    /// <summary>
    /// Punkt-Werte: <c>pt·20</c> ergibt ganze Twips, und Schriftgrade gehen in halben Punkt
    /// auf. Drei Stellen sind hier exakt genug.
    /// </summary>
    private static void GleicheZahl(double? a, double? b)
    {
        if (a is null || b is null) { Assert.Equal(a, b); return; }
        Assert.Equal(a.Value, b.Value, 3);
    }

    /// <summary>
    /// Zentimeter: **zwei Stellen, und das ist kein Nachlassen.** Ein Twip ist 1/1440 Zoll
    /// = 0,0018 cm — feiner kann DOCX einen Einzug gar nicht ablegen. Aus 1,5 cm werden 850
    /// Twips und daraus wieder 1,4993 cm. Wer hier drei Stellen verlangt, prüft nicht den
    /// eigenen Code, sondern die Auflösung eines fremden Formats, und bekommt einen roten
    /// Lauf für 7 Mikrometer.
    /// <para>
    /// Zwei Stellen (0,005 cm) sind trotzdem dreimal feiner als ein Twip — jeder echte
    /// Fehler an dieser Stelle, ein vertauschter Faktor oder eine verwechselte Einheit,
    /// liegt um Größenordnungen darüber.
    /// </para>
    /// </summary>
    private static void GleicheZahlCm(double? a, double? b)
    {
        if (a is null || b is null) { Assert.Equal(a, b); return; }
        Assert.Equal(a.Value, b.Value, 2);
    }
}
