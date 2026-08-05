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
        var doc = new TdDocument { Sections = { new TdSection(new TdParagraph("  zwei  Leerzeichen  ")) } };

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
            Sections = { new TdSection(new TdParagraph("davor"), new TdPageBreak(), new TdParagraph("danach")) },
        };

        var zurueck = Zurueck(doc);

        Assert.Collection(zurueck.Blocks(),
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
            Sections = { new TdSection(new TdParagraph([new TdRun("oben"), new TdLineBreak(), new TdRun("unten")])) },
        };

        var zurueck = Zurueck(doc);

        var absatz = Assert.IsType<TdParagraph>(Assert.Single(zurueck.Blocks()));
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
            Sections = { new TdSection(new TdParagraph("schlicht")) },
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
        var doc = new TdDocument { Sections = { new TdSection(new NochNichtBlock()) } };

        Assert.Throws<NotSupportedException>(
            () => TdDocx.Schreiben(doc, werkbank.Datei("x.docx")));
    }

    /// <summary>Steht für jeden Blocktyp, den ein späterer Schritt ergänzt.</summary>
    private sealed class NochNichtBlock : TdBlock
    {
        public override string PlainText() => "";
    }

    // ==================== Seiteneinrichtung (Schritt 2) ====================

    /// <summary>
    /// Blattgröße und Ränder gehen durch <c>sectPr</c>. Geprüft mit einem Format, dessen
    /// Umrechnung nicht glatt aufgeht (Letter), und mit Rändern, die sich voneinander
    /// unterscheiden — vier gleiche Ränder würden eine vertauschte Reihenfolge nicht zeigen.
    /// </summary>
    [Fact]
    public void Blattgroesse_und_Raender_ueberstehen_sectPr()
    {
        var seite = new TdPageSetup
        {
            WidthCm = 21.59,
            HeightCm = 27.94,
            MarginLeftCm = 3.0,
            MarginTopCm = 1.0,
            MarginRightCm = 2.0,
            MarginBottomCm = 1.5,
        };

        var zurueck = Zurueck(MitSeite(seite)).Sections[0].Page;

        Assert.Equal(21.59, zurueck.WidthCm, 2);
        Assert.Equal(27.94, zurueck.HeightCm, 2);
        Assert.Equal(3.0, zurueck.MarginLeftCm, 2);
        Assert.Equal(1.0, zurueck.MarginTopCm, 2);
        Assert.Equal(2.0, zurueck.MarginRightCm, 2);
        Assert.Equal(1.5, zurueck.MarginBottomCm, 2);
        Assert.Equal("Letter", zurueck.Name);
    }

    /// <summary>
    /// <b>Word leitet die Ausrichtung nicht aus den Maßen ab.</b> Ohne <c>w:orient</c> dreht
    /// es ein quer eingetragenes Blatt beim Drucken wieder hoch — die Datei sieht dabei
    /// richtig aus, nur der Ausdruck nicht.
    /// </summary>
    [Fact]
    public void Querformat_ueberlebt_und_traegt_seine_Ausrichtung()
    {
        var zurueck = Zurueck(MitSeite(TdPageSetup.A4.Quer())).Sections[0].Page;

        Assert.True(zurueck.IstQuerformat);
        Assert.Equal(29.7, zurueck.WidthCm, 2);
        Assert.Equal(21.0, zurueck.HeightCm, 2);
        Assert.Equal("A4", zurueck.Name);
    }

    /// <summary>
    /// <b>Kopf- und Fußzeile gehen durch echte Felder.</b> <c>{SEITE}</c> als bloßer Text
    /// stünde auf jeder Seite gleich da — deshalb wird daraus ein PAGE-Feld, und beim Lesen
    /// wieder der Platzhalter. Ohne den Rückweg käme aus einem Rückimport die beim Schreiben
    /// eingesetzte „1" als gewöhnlicher Text zurück.
    /// </summary>
    [Fact]
    public void Kopf_und_Fusszeile_gehen_durch_echte_Felder()
    {
        var seite = new TdPageSetup
        {
            HeaderText = "Gonk Note — {TITEL}",
            FooterText = "Seite {SEITE} von {SEITEN}",
            SuppressOnFirstPage = true,
        };

        var zurueck = Zurueck(MitSeite(seite)).Sections[0].Page;

        Assert.Equal("Gonk Note — {TITEL}", zurueck.HeaderText);
        Assert.Equal("Seite {SEITE} von {SEITEN}", zurueck.FooterText);
        Assert.True(zurueck.SuppressOnFirstPage);
    }

    /// <summary>Ohne Kopf-/Fußzeile entsteht auch kein leerer Teil im Dokument.</summary>
    [Fact]
    public void Ohne_Kopfzeile_bleibt_sie_leer()
    {
        var zurueck = Zurueck(MitSeite(TdPageSetup.A4)).Sections[0].Page;

        Assert.Equal("", zurueck.HeaderText);
        Assert.Equal("", zurueck.FooterText);
        Assert.False(zurueck.SuppressOnFirstPage);
    }

    /// <summary>
    /// <b>Die Stelle, an der DOCX unsymmetrisch ist.</b> Die Einrichtung des *letzten*
    /// Abschnitts steht am Ende des Körpers, die aller anderen im Absatzformat ihres jeweils
    /// letzten Absatzes. Wer alle ans Körperende hängt, bekommt ein Dokument mit genau einer
    /// Seiteneinrichtung — und merkt es erst am Ausdruck.
    /// </summary>
    [Fact]
    public void Zwei_Abschnitte_behalten_zwei_Seiteneinrichtungen()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(new TdParagraph("Deckblatt")) { Page = TdPageSetup.A4.Quer() },
                new TdSection(new TdParagraph("Inhalt")) { Page = TdPageSetup.A5 },
            },
        };

        var zurueck = Zurueck(doc);

        Assert.Equal(2, zurueck.Sections.Count);
        Assert.Equal("Deckblatt", zurueck.Sections[0].Blocks[0].PlainText());
        Assert.True(zurueck.Sections[0].Page.IstQuerformat);
        Assert.Equal("A4", zurueck.Sections[0].Page.Name);

        Assert.Equal("Inhalt", zurueck.Sections[1].Blocks[0].PlainText());
        Assert.False(zurueck.Sections[1].Page.IstQuerformat);
        Assert.Equal("A5", zurueck.Sections[1].Page.Name);
    }

    /// <summary>
    /// Ein Abschnittswechsel darf keinen Absatz kosten. Die <c>sectPr</c> hängt am **letzten**
    /// Absatz des Abschnitts, und der ist Inhalt — wer ihn beim Lesen als bloßen Träger
    /// abtut, verliert je Abschnitt eine Zeile.
    /// </summary>
    [Fact]
    public void Ein_Abschnittswechsel_kostet_keinen_Absatz()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(new TdParagraph("eins"), new TdParagraph("zwei")),
                new TdSection(new TdParagraph("drei")),
            },
        };

        var zurueck = Zurueck(doc);

        Assert.Equal(2, zurueck.Sections[0].Blocks.Count);
        Assert.Equal("eins\nzwei\ndrei", zurueck.PlainText());
    }

    /// <summary>
    /// Ein Abschnitt, dessen letzter Block ein Seitenumbruch ist, trägt seine
    /// <c>sectPr</c> auf dem Umbruch-Absatz — und muss beim Lesen trotzdem ein
    /// Seitenumbruch bleiben und kein leerer Absatz werden.
    /// </summary>
    [Fact]
    public void Ein_Abschnitt_darf_mit_einem_Seitenumbruch_enden()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(new TdParagraph("davor"), new TdPageBreak()),
                new TdSection(new TdParagraph("danach")),
            },
        };

        var zurueck = Zurueck(doc);

        Assert.Equal(2, zurueck.Sections.Count);
        Assert.IsType<TdPageBreak>(zurueck.Sections[0].Blocks[^1]);
        Assert.Equal("danach", zurueck.Sections[1].Blocks[0].PlainText());
    }

    // ==================== Felder und Verweise (Schritt 5) ====================

    /// <summary>
    /// <b>Ein relatives Ziel bleibt relativ.</b> Das ist die Entscheidung aus §7
    /// („Markdown-Export"): Wer beim Lesen <c>AbsoluteUri</c> nimmt statt
    /// <c>OriginalString</c>, macht aus <c>kapitel-2.md</c> einen <c>file:///</c>-Pfad, der
    /// auf jedem anderen Rechner ins Leere zeigt — und sichtbar wird das erst beim
    /// Anklicken.
    /// </summary>
    [Theory]
    [InlineData("kapitel-2.md")]
    [InlineData("../oben/datei.md")]
    [InlineData("https://example.org/pfad?a=1&b=2")]
    public void Ein_Verweisziel_uebersteht_DOCX_woertlich(string ziel)
    {
        var doc = MitStuecken(TdHyperlink.Text(ziel, "Text"));

        var verweis = Assert.IsType<TdHyperlink>(Zurueck(doc).Paragraphs().First().Inlines[0]);

        Assert.Equal(ziel, verweis.Target);
        Assert.Equal("Text", verweis.PlainText());
    }

    /// <summary>
    /// Ein Verweis **ins eigene Dokument** ist in DOCX kein Dateiverweis, sondern ein Anker.
    /// Als Beziehung geschrieben öffnete Word ein zweites Fenster auf dieselbe Datei.
    /// </summary>
    [Fact]
    public void Ein_Verweis_ins_eigene_Dokument_wird_ein_Anker()
    {
        var doc = MitStuecken(TdHyperlink.Text("#marke", "dorthin"));

        var verweis = Assert.IsType<TdHyperlink>(Zurueck(doc).Paragraphs().First().Inlines[0]);

        Assert.Equal("#marke", verweis.Target);
        Assert.True(verweis.IstTextmarke);
    }

    /// <summary>
    /// Ein Verweis ist eine Klammer um Läufe und kein Lauf mit einem Zusatzfeld — die
    /// Auszeichnung **innerhalb** des Linktextes muss deshalb erhalten bleiben.
    /// </summary>
    [Fact]
    public void Ein_Verweis_behaelt_die_Auszeichnung_seines_Textes()
    {
        var doc = MitStuecken(new TdHyperlink("ziel.md",
            new TdRun("normal "),
            new TdRun("fett", new TdCharFormat { Bold = true })));

        var verweis = Assert.IsType<TdHyperlink>(Zurueck(doc).Paragraphs().First().Inlines[0]);

        Assert.Equal(2, verweis.Inlines.Count);
        Assert.Null(verweis.Inlines[0].Format.Bold);
        Assert.True(verweis.Inlines[1].Format.Bold);
    }

    /// <summary>Jede Feldart geht als Feld hin und zurück — und nicht als Text.</summary>
    [Theory]
    [InlineData(TdFieldKind.PageNumber, null)]
    [InlineData(TdFieldKind.PageCount, null)]
    [InlineData(TdFieldKind.Date, "yyyy-MM-dd")]
    [InlineData(TdFieldKind.Title, null)]
    [InlineData(TdFieldKind.TableOfContents, "2-4")]
    public void Jede_Feldart_uebersteht_DOCX(TdFieldKind art, string? angabe)
    {
        var doc = MitStuecken(new TdField(art, angabe));

        var feld = Assert.IsType<TdField>(Zurueck(doc).Paragraphs().First().Inlines[0]);

        Assert.Equal(art, feld.Kind);
        Assert.Equal(angabe, feld.Argument);
    }

    /// <summary>
    /// <b>Ein Feld ohne eigene Angabe bekommt in DOCX die Vorgabe eingetragen</b> — das Format
    /// kennt kein „nicht gesetzt" für einen Schalter. Das ist der einzige Weg, auf dem ein
    /// Feld beim Roundtrip etwas dazubekommt, und er ist harmlos: eingetragen wird genau der
    /// Wert, mit dem gerechnet worden wäre. Der Wächter steht hier, damit es niemanden
    /// überrascht.
    /// </summary>
    [Fact]
    public void Ein_Feld_ohne_Angabe_bekommt_die_Vorgabe_eingetragen()
    {
        var datum = Assert.IsType<TdField>(
            Zurueck(MitStuecken(new TdField(TdFieldKind.Date))).Paragraphs().First().Inlines[0]);
        var verzeichnis = Assert.IsType<TdField>(
            Zurueck(MitStuecken(new TdField(TdFieldKind.TableOfContents))).Paragraphs().First().Inlines[0]);

        Assert.Equal(TdFieldValues.DatumsmusterStandard, datum.Argument);
        Assert.Equal("1-3", verzeichnis.Argument);
    }

    /// <summary>
    /// Das Feld steht in der **dreiteiligen** Form da — <c>begin</c>, <c>instrText</c>,
    /// <c>end</c>. Für PAGE reichte die kurze; ein Inhaltsverzeichnis, dessen Ergebnis ganze
    /// Absätze sind, passt dort nicht hinein, und zwei Formen nebeneinander wären die
    /// Doppelung aus §4.10.
    /// </summary>
    [Fact]
    public void Ein_Feld_im_Koerper_steht_in_der_dreiteiligen_Form()
    {
        using var werkbank = new Werkbank("dreiteilig");
        string pfad = werkbank.Datei("toc.docx");

        TdDocx.Schreiben(MitStuecken(new TdField(TdFieldKind.TableOfContents)), pfad);

        string xml = Hauptteil(pfad);
        Assert.Contains("w:fldCharType=\"begin\"", xml);
        Assert.Contains("w:instrText", xml);
        Assert.Contains("TOC", xml);
        Assert.Contains("w:fldCharType=\"end\"", xml);
        Assert.DoesNotContain("fldSimple", xml);
    }

    /// <summary>
    /// <b>Das Feld wird ohne zwischengespeichertes Ergebnis geschrieben</b>, also ohne
    /// <c>separate</c>-Teil. Käme das Verzeichnis mit, läse der Import es als gewöhnliche
    /// Absätze, und das Dokument wüchse **mit jedem Speichern um ein ganzes
    /// Inhaltsverzeichnis** — dieselbe Falle wie beim Trennabsatz zwischen zwei Tabellen
    /// (§4.18), nur mit dreißig Zeilen statt einer. Der Wächter läuft deshalb zweimal.
    /// </summary>
    [Fact]
    public void Ein_Dokument_mit_Verzeichnis_waechst_beim_Speichern_nicht()
    {
        var doc = Beispiel();

        var einmal = Zurueck(doc);
        var zweimal = Zurueck(einmal);

        GleichesDokument(einmal, zweimal);
    }

    /// <summary>
    /// <b>Ein Feldergebnis ist kein Text.</b> Ein Dokument aus Word bringt es mit — dort steht
    /// zwischen <c>separate</c> und <c>end</c>, was zuletzt gerechnet wurde. Wer es als Inhalt
    /// liest, hat die Seitenzahl zweimal: einmal als Feld und einmal als Zahl, die nie wieder
    /// nachgerechnet wird.
    /// </summary>
    [Fact]
    public void Ein_zwischengespeichertes_Ergebnis_wird_nicht_zu_Text()
    {
        using var werkbank = new Werkbank("ergebnis");
        string pfad = werkbank.Datei("fremd.docx");

        TdDocx.Schreiben(MitStuecken(new TdField(TdFieldKind.PageNumber)), pfad);
        ErgebnisEinsetzen(pfad, "42");

        var absatz = TdDocx.Lesen(pfad).Paragraphs().First();

        var feld = Assert.IsType<TdField>(Assert.Single(absatz.Inlines));
        Assert.Equal(TdFieldKind.PageNumber, feld.Kind);
        Assert.Equal("", absatz.PlainText());
    }

    /// <summary>
    /// <b>Ein Feld, das wir nicht kennen, verliert seine Rechenvorschrift — aber nicht seinen
    /// Text.</b> Eine <c>REF</c>-Angabe wieder ausrechnen zu können wäre schön; ihren Text zu
    /// verlieren ist Datenverlust.
    /// </summary>
    [Fact]
    public void Ein_unbekanntes_Feld_behaelt_seinen_Text()
    {
        using var werkbank = new Werkbank("unbekanntes-feld");
        string pfad = werkbank.Datei("fremd.docx");

        TdDocx.Schreiben(MitStuecken(new TdField(TdFieldKind.PageNumber)), pfad);
        ErgebnisEinsetzen(pfad, "Querverweis");
        AnweisungErsetzen(pfad, " REF _Ref12345 \\h ");

        Assert.Equal("Querverweis", TdDocx.Lesen(pfad).Paragraphs().First().PlainText());
    }

    /// <summary>
    /// Ein Inhaltsverzeichnis braucht **Sprungziele**, sonst führt jeder Eintrag nirgendwohin.
    /// Geschrieben werden sie an den Überschriften und in Words eigener Schreibweise
    /// (<c>_Toc…</c>) — und beim Lesen wieder übergangen: eine Textmarke ist ein Ziel und kein
    /// Inhalt.
    /// </summary>
    [Fact]
    public void Ueberschriften_bekommen_Sprungziele_wenn_es_ein_Verzeichnis_gibt()
    {
        using var werkbank = new Werkbank("textmarken");
        string mit = werkbank.Datei("mit.docx");
        string ohne = werkbank.Datei("ohne.docx");

        TdDocx.Schreiben(new TdDocument
        {
            Sections =
            {
                new TdSection(
                    new TdParagraph([new TdField(TdFieldKind.TableOfContents)]),
                    new TdParagraph("Kapitel") { Format = { OutlineLevel = 1 } }),
            },
        }, mit);

        TdDocx.Schreiben(new TdDocument
        {
            Sections = { new TdSection(new TdParagraph("Kapitel") { Format = { OutlineLevel = 1 } }) },
        }, ohne);

        Assert.Contains("_Toc00000001", Hauptteil(mit));

        // Ohne Verzeichnis zeigt niemand auf eine Marke — dann steht auch keine da.
        Assert.DoesNotContain("bookmarkStart", Hauptteil(ohne));

        // Und beim Lesen wird sie nicht zu Inhalt.
        Assert.Equal(2, TdDocx.Lesen(mit).Paragraphs().Count());
        Assert.Equal("Kapitel", TdDocx.Lesen(mit).PlainText());
    }

    /// <summary>
    /// <b>Alle vier Platzhalter der Kopf- und Fußzeile sind jetzt echte Felder.</b> Bis
    /// Schritt 5 standen <c>{DATUM}</c> und <c>{TITEL}</c> dort wörtlich im Text — sie hatten
    /// kein Feld, zu dem sie hätten werden können (§4.15). Als Text wäre das Datum auf ewig
    /// der Tag des Exports.
    /// </summary>
    [Fact]
    public void Alle_vier_Platzhalter_werden_zu_echten_Feldern()
    {
        using var werkbank = new Werkbank("kopfzeile");
        string pfad = werkbank.Datei("kopf.docx");

        var seite = new TdPageSetup
        {
            HeaderText = "{TITEL} — {DATUM}",
            FooterText = "Seite {SEITE} von {SEITEN}",
        };

        TdDocx.Schreiben(MitSeite(seite), pfad);

        string kopf = Kopfzeile(pfad);
        Assert.Contains("TITLE", kopf);
        Assert.Contains("DATE", kopf);
        Assert.DoesNotContain("{TITEL}", kopf);
        Assert.DoesNotContain("{DATUM}", kopf);

        // Und der Weg zurück macht wieder Platzhalter daraus.
        var zurueck = TdDocx.Lesen(pfad).Sections[0].Page;
        Assert.Equal("{TITEL} — {DATUM}", zurueck.HeaderText);
        Assert.Equal("Seite {SEITE} von {SEITEN}", zurueck.FooterText);
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
        new() { Sections = { new TdSection(new TdParagraph([new TdRun("Wort", f)])) } };

    private static TdDocument MitAbsatzformat(TdParaFormat f) =>
        new() { Sections = { new TdSection(new TdParagraph("Wort") { Format = f }) } };

    private static TdDocument MitSeite(TdPageSetup seite) =>
        new() { Sections = { new TdSection(new TdParagraph("Wort")) { Page = seite } } };

    private static TdDocument MitStuecken(params TdInline[] stuecke) =>
        new() { Sections = { new TdSection(new TdParagraph(stuecke)) } };

    private static TdCharFormat ErstesStueck(TdDocument doc) =>
        doc.Paragraphs().First().Inlines[0].Format;

    /// <summary>
    /// Schreibt das XML des Hauptteils um. **Von Hand**, weil sich manches, was Word schreibt,
    /// über die OpenXml-Objekte gar nicht erzeugen lässt — und genau daran scheitert ein
    /// Import, den nur der eigene Export geprüft hat.
    /// </summary>
    private static void XmlAendern(string pfad, Func<string, string> aendern)
    {
        using var docx = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(pfad, true);
        var teil = docx.MainDocumentPart!;

        string xml;
        using (var lesen = new StreamReader(teil.GetStream(FileMode.Open, FileAccess.Read)))
            xml = lesen.ReadToEnd();

        xml = aendern(xml);

        using var schreiben = new StreamWriter(teil.GetStream(FileMode.Create, FileAccess.Write));
        schreiben.Write(xml);
    }

    /// <summary>
    /// Entfernt das <c>w:val</c>-Attribut eines Elements im gespeicherten DOCX — so schreibt
    /// Word eine gesetzte Auszeichnung.
    /// </summary>
    private static void WertAmElementEntfernen(string pfad, string element) =>
        XmlAendern(pfad, xml => System.Text.RegularExpressions.Regex.Replace(
            xml, $"<w:{element} w:val=\"[^\"]*\" ?/>", $"<w:{element}/>"));

    /// <summary>
    /// Setzt ein zwischengespeichertes Feldergebnis ein — den <c>separate</c>-Teil samt Text,
    /// wie Word ihn schreibt und wie wir ihn bewusst **nicht** schreiben (§4.20).
    /// </summary>
    private static void ErgebnisEinsetzen(string pfad, string text) =>
        XmlAendern(pfad, xml => System.Text.RegularExpressions.Regex.Replace(
            xml,
            "<w:r><w:fldChar w:fldCharType=\"end\" ?/></w:r>",
            $"<w:r><w:fldChar w:fldCharType=\"separate\" /></w:r><w:r><w:t>{text}</w:t></w:r>$0"));

    /// <summary>Tauscht die Anweisung eines Feldes gegen eine andere.</summary>
    private static void AnweisungErsetzen(string pfad, string anweisung) =>
        XmlAendern(pfad, xml => System.Text.RegularExpressions.Regex.Replace(
            xml,
            "<w:instrText[^>]*>[^<]*</w:instrText>",
            $"<w:instrText xml:space=\"preserve\">{anweisung}</w:instrText>"));

    /// <summary>Das XML des Hauptteils — für Wächter, die auf die Datei selbst sehen müssen.</summary>
    private static string Hauptteil(string pfad)
    {
        using var docx = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(pfad, false);
        using var lesen = new StreamReader(docx.MainDocumentPart!.GetStream(FileMode.Open, FileAccess.Read));
        return lesen.ReadToEnd();
    }

    /// <inheritdoc cref="Hauptteil"/>
    private static string Kopfzeile(string pfad)
    {
        using var docx = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(pfad, false);
        var teil = docx.MainDocumentPart!.HeaderParts.First();
        using var lesen = new StreamReader(teil.GetStream(FileMode.Open, FileAccess.Read));
        return lesen.ReadToEnd();
    }

    /// <inheritdoc cref="DokumentmodellTests"/>
    private static TdDocument Beispiel() => new()
    {
        DefaultCharFormat = { FontFamily = "Calibri", FontSize = 11 },
        DefaultParaFormat = { SpaceAfterPt = 6, LineSpacing = 1.15 },
        Lists = { TdListDefinition.Nummern(1), TdListDefinition.Punkte(2) },
        Sections =
        {
            // Ein Deckblatt quer, der Rest hoch — der Fall, für den es Abschnitte gibt, und
            // gleichzeitig der Fall, an dem DOCX unsymmetrisch ist: die sectPr des ersten
            // Abschnitts steht im letzten Absatz, die des zweiten am Körperende.
            new TdSection(new TdParagraph("Deckblatt")) { Page = TdPageSetup.A4.Quer() },
            new TdSection(Inhalt())
            {
                Page = new TdPageSetup
                {
                    WidthCm = 21.0,
                    HeightCm = 29.7,
                    MarginLeftCm = 2.5,
                    MarginTopCm = 1.5,
                    MarginRightCm = 2.5,
                    MarginBottomCm = 2.0,
                    HeaderText = "Gonk Note — {TITEL}",
                    FooterText = "Seite {SEITE} von {SEITEN}",
                    SuppressOnFirstPage = true,
                },
            },
        },
    };

    private static TdBlock[] Inhalt() =>
        [
            // Das Inhaltsverzeichnis: ein Feld in der dreiteiligen Form, dazu die Textmarken
            // an den Überschriften, die es aufzählt (§4.20).
            new TdParagraph([new TdField(TdFieldKind.TableOfContents, "1-2")]),

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
            // Verweise und Felder: ein relatives Ziel, eine Textmarke im eigenen Dokument, und
            // vier Feldarten — zwei davon mit eigener Zusatzangabe.
            new TdParagraph([
                new TdRun("Siehe "),
                new TdHyperlink("kapitel-2.md",
                    new TdRun("Kapitel "),
                    new TdRun("zwei", new TdCharFormat { Bold = true })),
                new TdRun(", "),
                TdHyperlink.Text("#marke", "hier im Text"),
                new TdRun(" — Stand "),
                new TdField(TdFieldKind.Date, "yyyy-MM-dd"),
                new TdRun(", Seite "),
                new TdField(TdFieldKind.PageNumber),
                new TdRun(" von "),
                new TdField(TdFieldKind.PageCount),
                new TdRun(" in "),
                new TdField(TdFieldKind.Title) { Format = { Italic = true } },
                new TdRun("."),
            ]),

            // Zwei Listen, zwei Ebenen — Nummerierung und Aufzählung nebeneinander.
            new TdParagraph("Erster Punkt") { List = new TdListRef(1, 0) },
            new TdParagraph("Unterpunkt") { List = new TdListRef(1, 1) },
            new TdParagraph("Zweiter Punkt") { List = new TdListRef(1, 0) },
            new TdParagraph("Ein Strich") { List = new TdListRef(2, 0) },

            // Eine Tabelle mit beiden Verbindungsarten.
            new TdTable(
                new TdTableRow(
                    new TdTableCell(new TdParagraph("Kopf über zwei")) { ColumnSpan = 2, Shading = "#DDEEFF" },
                    TdTableCell.Text("Rest"))
                { IsHeader = true },
                new TdTableRow(
                    new TdTableCell(new TdParagraph("verbunden")) { VerticalMerge = TdVerticalMerge.Restart },
                    TdTableCell.Text("b"),
                    TdTableCell.Text("c")),
                new TdTableRow(
                    new TdTableCell { VerticalMerge = TdVerticalMerge.Continue },
                    TdTableCell.Text("e"),
                    TdTableCell.Text("f")))
            {
                ColumnWidthsCm = { 4.0, 5.5, 3.25 },
            },

            new TdPageBreak(),
            new TdParagraph("Nach dem Umbruch.") { Format = { PageBreakBefore = true } },
        ];

    private static void GleichesDokument(TdDocument a, TdDocument b)
    {
        GleichesZeichenformat(a.DefaultCharFormat, b.DefaultCharFormat);
        GleichesAbsatzformat(a.DefaultParaFormat, b.DefaultParaFormat);
        GleicheListen(a.Lists, b.Lists);

        Assert.Equal(a.Sections.Count, b.Sections.Count);
        for (int s = 0; s < a.Sections.Count; s++)
        {
            GleicheSeite(a.Sections[s].Page, b.Sections[s].Page);
            GleicheBloecke(a.Sections[s].Blocks, b.Sections[s].Blocks);
        }
    }

    private static void GleicheBloecke(List<TdBlock> a, List<TdBlock> b)
    {
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            switch (a[i])
            {
                case TdParagraph pa:
                {
                    var pb = Assert.IsType<TdParagraph>(b[i]);
                    GleichesZeichenformat(pa.CharFormat, pb.CharFormat);
                    GleichesAbsatzformat(pa.Format, pb.Format);
                    GleicherListenverweis(pa.List, pb.List);
                    GleicheStuecke(pa.Inlines, pb.Inlines);
                    break;
                }

                case TdPageBreak:
                    Assert.IsType<TdPageBreak>(b[i]);
                    break;

                case TdTable ta:
                {
                    var tb = Assert.IsType<TdTable>(b[i]);

                    Assert.Equal(ta.ColumnWidthsCm.Count, tb.ColumnWidthsCm.Count);
                    for (int s = 0; s < ta.ColumnWidthsCm.Count; s++)
                        GleicheZahlCm(ta.ColumnWidthsCm[s], tb.ColumnWidthsCm[s]);

                    Assert.Equal(ta.Rows.Count, tb.Rows.Count);
                    for (int z = 0; z < ta.Rows.Count; z++)
                    {
                        Assert.Equal(ta.Rows[z].IsHeader, tb.Rows[z].IsHeader);
                        Assert.Equal(ta.Rows[z].Cells.Count, tb.Rows[z].Cells.Count);

                        for (int s = 0; s < ta.Rows[z].Cells.Count; s++)
                        {
                            var za = ta.Rows[z].Cells[s];
                            var zb = tb.Rows[z].Cells[s];
                            Assert.Equal(za.ColumnSpan, zb.ColumnSpan);
                            Assert.Equal(za.VerticalMerge, zb.VerticalMerge);
                            Assert.Equal(za.Shading, zb.Shading);
                            Assert.Equal(za.VerticalAlign, zb.VerticalAlign);
                            GleicheBloecke(za.Blocks, zb.Blocks);
                        }
                    }
                    break;
                }

                default:
                    Assert.Fail($"Kein Vergleich für {a[i].GetType().Name} — bitte ergänzen.");
                    break;
            }
        }
    }

    /// <inheritdoc cref="DokumentmodellTests"/>
    private static void GleicheStuecke(List<TdInline> a, List<TdInline> b)
    {
        Assert.Equal(a.Count, b.Count);
        for (int k = 0; k < a.Count; k++)
        {
            Assert.Equal(a[k].GetType(), b[k].GetType());
            GleichesZeichenformat(a[k].Format, b[k].Format);

            switch (a[k])
            {
                case TdRun ra:
                    Assert.Equal(ra.Text, ((TdRun)b[k]).Text);
                    break;

                case TdLineBreak:
                    break;

                // **Ein Feld hat keinen Klartext** — wer nur den vergliche, bemerkte nicht,
                // dass aus einem Datumsfeld ein Titel geworden ist.
                case TdField fa:
                {
                    var fb = (TdField)b[k];
                    Assert.Equal(fa.Kind, fb.Kind);
                    Assert.Equal(fa.Argument, fb.Argument);
                    break;
                }

                case TdHyperlink ha:
                {
                    var hb = (TdHyperlink)b[k];
                    Assert.Equal(ha.Target, hb.Target);
                    GleicheStuecke(ha.Inlines, hb.Inlines);
                    break;
                }

                default:
                    Assert.Fail($"Kein Vergleich für {a[k].GetType().Name} — bitte ergänzen.");
                    break;
            }
        }
    }

    private static void GleicherListenverweis(TdListRef? a, TdListRef? b)
    {
        if (a is null || b is null) { Assert.Equal(a is null, b is null); return; }
        Assert.Equal(a.ListId, b.ListId);
        Assert.Equal(a.Level, b.Level);
    }

    private static void GleicheListen(List<TdListDefinition> a, List<TdListDefinition> b)
    {
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Id, b[i].Id);
            Assert.Equal(a[i].Levels.Count, b[i].Levels.Count);
            for (int k = 0; k < a[i].Levels.Count; k++)
            {
                Assert.Equal(a[i].Levels[k].Marker, b[i].Levels[k].Marker);
                Assert.Equal(a[i].Levels[k].Text, b[i].Levels[k].Text);
                Assert.Equal(a[i].Levels[k].Start, b[i].Levels[k].Start);
                GleicheZahlCm(a[i].Levels[k].IndentCm, b[i].Levels[k].IndentCm);
                GleicheZahlCm(a[i].Levels[k].HangingCm, b[i].Levels[k].HangingCm);
            }
        }
    }

    private static void GleicheSeite(TdPageSetup a, TdPageSetup b)
    {
        GleicheZahlCm(a.WidthCm, b.WidthCm);
        GleicheZahlCm(a.HeightCm, b.HeightCm);
        GleicheZahlCm(a.MarginLeftCm, b.MarginLeftCm);
        GleicheZahlCm(a.MarginTopCm, b.MarginTopCm);
        GleicheZahlCm(a.MarginRightCm, b.MarginRightCm);
        GleicheZahlCm(a.MarginBottomCm, b.MarginBottomCm);
        Assert.Equal(a.HeaderText, b.HeaderText);
        Assert.Equal(a.FooterText, b.FooterText);
        Assert.Equal(a.SuppressOnFirstPage, b.SuppressOnFirstPage);
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
