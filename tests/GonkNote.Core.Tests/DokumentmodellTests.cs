using System.Text;
using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdDocument"/> — das eigene Dokumentmodell aus Phase 4, Schritt 1
/// (Absätze + Zeichenformate).
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Das Modell ersetzt <c>FlowDocument</c> als
/// Speicherform von Textdokumenten. Ein Fehler darin ist kein Anzeigefehler, sondern
/// Datenverlust: ein weggelassenes Feld, ein umbenannter Diskriminator oder eine falsch
/// gerechnete Formatkaskade fällt beim Speichern nicht auf und beim Öffnen erst, wenn die
/// Formatierung weg ist. Genau die Sorte Fehler, für die HANDOFF §7 die Regel „Models nie
/// umziehen, ohne an <c>_type</c> zu denken" aufgestellt hat.
/// </para>
/// </summary>
public sealed class DokumentmodellTests
{
    // ==================== Formatkaskade ====================

    /// <summary>
    /// <c>null</c> heißt „nicht gesetzt", nicht „Standardwert". Das ist die Eigenschaft, an
    /// der ein Texteditor sonst scheitert: wer in einer Überschrift ein Wort fett macht,
    /// setzt genau ein Feld — Schrift und Größe müssen weiter von der Überschrift kommen.
    /// </summary>
    [Fact]
    public void Ein_gesetztes_Feld_ueberschreibt_nur_sich_selbst()
    {
        var ueberschrift = new TdCharFormat { FontFamily = "Georgia", FontSize = 20 };
        var fett = new TdCharFormat { Bold = true };

        var ergebnis = fett.Over(ueberschrift);

        Assert.True(ergebnis.Bold);
        Assert.Equal("Georgia", ergebnis.FontFamily);
        Assert.Equal(20, ergebnis.FontSize);
    }

    /// <summary>
    /// <c>false</c> ist ein Wert und kein „nicht gesetzt" — sonst ließe sich Fett innerhalb
    /// einer fetten Überschrift nie wieder abschalten.
    /// </summary>
    [Fact]
    public void Ausdrueckliches_Falsch_schlaegt_ein_geerbtes_Wahr()
    {
        var fetteUeberschrift = new TdCharFormat { Bold = true };
        var normalesWort = new TdCharFormat { Bold = false };

        Assert.False(normalesWort.Over(fetteUeberschrift).Bold);
    }

    /// <summary>Nach dem Auflösen ist kein Feld mehr offen — Layout und Export brauchen Werte.</summary>
    [Fact]
    public void Aufgeloest_laesst_kein_Feld_offen()
    {
        var c = new TdCharFormat().Aufgeloest();
        Assert.NotNull(c.FontFamily);
        Assert.NotNull(c.FontSize);
        Assert.NotNull(c.Bold);
        Assert.NotNull(c.Italic);
        Assert.NotNull(c.Underline);
        Assert.NotNull(c.Strikethrough);
        Assert.NotNull(c.Color);
        Assert.NotNull(c.Highlight);
        Assert.NotNull(c.VerticalAlign);

        var p = new TdParaFormat().Aufgeloest();
        Assert.NotNull(p.Alignment);
        Assert.NotNull(p.LeftIndentCm);
        Assert.NotNull(p.RightIndentCm);
        Assert.NotNull(p.FirstLineIndentCm);
        Assert.NotNull(p.SpaceBeforePt);
        Assert.NotNull(p.SpaceAfterPt);
        Assert.NotNull(p.LineSpacing);
        Assert.NotNull(p.OutlineLevel);
        Assert.NotNull(p.KeepWithNext);
        Assert.NotNull(p.PageBreakBefore);
    }

    /// <summary>
    /// Die volle Kette: Stück → Absatz → Dokument → Standard. Jede Schicht steuert genau ein
    /// Feld bei, und alle vier müssen im Ergebnis stehen — eine übersprungene Schicht fiele
    /// mit nur zweien nicht auf.
    /// </summary>
    [Fact]
    public void Die_Kaskade_geht_ueber_alle_vier_Schichten()
    {
        var doc = new TdDocument { DefaultCharFormat = { Color = "#112233" } };
        var absatz = new TdParagraph { CharFormat = { FontSize = 20 } };
        var lauf = new TdRun("Wort", new TdCharFormat { Bold = true });
        absatz.Inlines.Add(lauf);
        doc.Sections.Add(new TdSection(absatz));

        var f = doc.FormatVon(absatz, lauf);

        Assert.True(f.Bold);                              // vom Stück
        Assert.Equal(20, f.FontSize);                     // vom Absatz
        Assert.Equal("#112233", f.Color);                 // vom Dokument
        Assert.Equal("Segoe UI", f.FontFamily);           // vom Standard
    }

    /// <summary>
    /// Die Kaskade darf ihre Eingaben nicht verändern. Sonst wäre das erste Auflösen
    /// harmlos und jedes weitere falsch — ein Fehler, der erst beim zweiten Speichern
    /// auftritt.
    /// </summary>
    [Fact]
    public void Auflösen_veraendert_die_Unterlage_nicht()
    {
        var unterlage = new TdCharFormat { FontSize = 11 };
        var oben = new TdCharFormat { Bold = true };

        oben.Over(unterlage).Aufgeloest();

        Assert.Null(unterlage.Bold);
        Assert.Null(oben.FontSize);
    }

    // ==================== Klartext und Wortzahl ====================

    [Fact]
    public void Ein_Absatz_setzt_seine_Stuecke_zusammen()
    {
        var p = new TdParagraph([new TdRun("Hallo "), new TdRun("Welt")]);
        Assert.Equal("Hallo Welt", p.PlainText());
    }

    /// <summary>
    /// Zwei Absätze sind zwei Wörter, auch ohne Leerzeichen dazwischen. Wer die Absätze
    /// stumpf aneinanderhängt, bekommt „EndeAnfang" und zählt eines zu wenig.
    /// </summary>
    [Fact]
    public void Absatzgrenzen_trennen_Woerter()
    {
        var doc = new TdDocument
        {
            Sections = { new TdSection(new TdParagraph("Ende"), new TdParagraph("Anfang")) },
        };

        Assert.Equal("Ende\nAnfang", doc.PlainText());
        Assert.Equal(2, doc.WordCount());
    }

    /// <summary>Mehrfache Leerzeichen und Umbrüche erzeugen keine leeren Wörter.</summary>
    [Fact]
    public void Leerraum_erzeugt_keine_leeren_Woerter()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(
                    new TdParagraph([new TdRun("zwei   Wörter"), new TdLineBreak(), new TdRun("  drei")]),
                    new TdParagraph("   ")),
            },
        };

        Assert.Equal(3, doc.WordCount());
    }

    /// <summary>
    /// Der Klartext läuft über **alle** Abschnitte. Ein Abschnittswechsel ist eine Angabe
    /// über das Blatt und keine über den Text — wer nur den ersten liest, verliert ab dem
    /// Deckblatt alles.
    /// </summary>
    [Fact]
    public void Der_Klartext_laeuft_ueber_alle_Abschnitte()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(new TdParagraph("Deckblatt")),
                new TdSection(new TdParagraph("Inhalt")),
            },
        };

        Assert.Equal("Deckblatt\nInhalt", doc.PlainText());
        Assert.Equal(2, doc.WordCount());
        Assert.Equal(2, doc.Blocks().Count());
    }

    /// <summary>Ein leeres Dokument hat einen Abschnitt mit einem Absatz — sonst hätte der Cursor keinen Ort.</summary>
    [Fact]
    public void Ein_leeres_Dokument_hat_einen_leeren_Absatz()
    {
        var doc = TdDocument.Leer();
        var abschnitt = Assert.Single(doc.Sections);
        Assert.IsType<TdParagraph>(Assert.Single(abschnitt.Blocks));
        Assert.Equal("", doc.PlainText());
        Assert.Equal(0, doc.WordCount());
    }

    // ==================== Seiteneinrichtung ====================

    /// <summary>
    /// Querformat ist keine eigene Angabe, sondern „breiter als hoch". Ein zusätzliches
    /// <c>bool</c> daneben wäre eine zweite Wahrheit über dieselbe Sache — die Doppelung aus
    /// HANDOFF §4.10, nur im Kleinen.
    /// </summary>
    [Fact]
    public void Querformat_steht_in_den_Massen_und_nicht_in_einem_Schalter()
    {
        var seite = TdPageSetup.A4;
        Assert.False(seite.IstQuerformat);

        seite.Quer();
        Assert.True(seite.IstQuerformat);
        Assert.Equal(29.7, seite.WidthCm, 2);
        Assert.Equal(21.0, seite.HeightCm, 2);

        // Zweimal drehen ändert nichts mehr — die Methoden setzen einen Zustand, sie
        // schalten nicht um.
        seite.Quer();
        Assert.Equal(29.7, seite.WidthCm, 2);

        seite.Hoch();
        Assert.Equal(21.0, seite.WidthCm, 2);
    }

    /// <summary>
    /// Der Formatname wird aus der Größe **zurückerkannt** und nicht gespeichert — sonst
    /// könnten Name und Maße sich widersprechen. Ein quer liegendes A4 ist weiterhin A4.
    /// </summary>
    [Theory]
    [InlineData(21.0, 29.7, "A4")]
    [InlineData(29.7, 21.0, "A4")]
    [InlineData(14.8, 21.0, "A5")]
    [InlineData(29.7, 42.0, "A3")]
    [InlineData(21.59, 27.94, "Letter")]
    public void Der_Formatname_wird_aus_der_Groesse_erkannt(double b, double h, string name)
    {
        Assert.Equal(name, new TdPageSetup { WidthCm = b, HeightCm = h }.Name);
    }

    /// <summary>Eine eigene Größe ist kein Fehler — sie hat nur keinen Namen.</summary>
    [Fact]
    public void Eine_eigene_Groesse_hat_keinen_Namen_und_ist_trotzdem_gueltig()
    {
        Assert.Null(new TdPageSetup { WidthCm = 17.3, HeightCm = 24.1 }.Name);
    }

    /// <summary>
    /// Der bedruckbare Bereich ist Blatt minus Ränder — und nie negativ. Ränder, die breiter
    /// sind als das Blatt, kommen aus fremden Dateien; ein negativer Textbereich brächte den
    /// Umbruch in Schritt 2b zum Stillstand.
    /// </summary>
    [Fact]
    public void Der_bedruckbare_Bereich_wird_nicht_negativ()
    {
        var normal = TdPageSetup.A4;
        Assert.Equal(17.0, normal.TextBreiteCm, 2);
        Assert.Equal(25.7, normal.TextHoeheCm, 2);

        var unsinnig = new TdPageSetup { WidthCm = 5, MarginLeftCm = 4, MarginRightCm = 4 };
        Assert.Equal(0, unsinnig.TextBreiteCm);
    }

    // ==================== Speicherformat ====================

    /// <summary>
    /// Der Roundtrip: alles, was Schritt 1 ausdrücken kann, geht hinein und kommt gleich
    /// wieder heraus. **Das ist der Wächter, an dem jeder weitere Schritt hängt** — wer in
    /// Schritt 2 ein Feld ergänzt und hier nichts nachzieht, merkt den Verlust nicht.
    /// </summary>
    [Fact]
    public void Ein_Dokument_uebersteht_Schreiben_und_Lesen()
    {
        var original = Beispiel();

        var zurueck = TdFormatIo.Lesen(TdFormatIo.Schreiben(original));

        Assert.NotNull(zurueck);
        GleichesDokument(original, zurueck);
    }

    /// <summary>
    /// **Nicht gesetzte Felder stehen nicht in der Datei.** Das ist nicht Kosmetik: ein Format
    /// hat neun bzw. zehn Felder, und ein Dokument hat sehr viele Läufe. Stünde in jedem
    /// „null", wäre die Datei ein Vielfaches groß — und das Weglassen ist zugleich die
    /// einzige Art, „nicht gesetzt" überhaupt zu speichern.
    /// </summary>
    [Fact]
    public void Nicht_gesetzte_Formatfelder_landen_nicht_in_der_Datei()
    {
        var doc = new TdDocument { Sections = { new TdSection(new TdParagraph("schlicht")) } };

        string json = Text(TdFormatIo.Schreiben(doc));

        Assert.DoesNotContain("null", json);
        Assert.DoesNotContain("FontFamily", json);
        Assert.Contains("schlicht", json);
    }

    /// <summary>
    /// <b>Und schon gar keine abgeleiteten Werte.</b> Ein gerechneter Wert in der Datei sieht
    /// später wie gespeicherte Wahrheit aus (§4.14) — und veraltet beim ersten Öffnen mit
    /// einer neuen Fassung.
    /// <para>
    /// <b>Dieser Wächter fehlte fünf Schritte lang</b>, und gefunden wurde die Lücke nicht im
    /// Test, sondern am laufenden Programm: In jedem Format stand ein <c>"IstLeer":false</c>,
    /// in jeder Seiteneinrichtung ein <c>"IstQuerformat"</c> samt Textbereich (§4.22). Der
    /// eigene Roundtrip konnte es nicht sehen — er liest die Felder ja gar nicht zurück.
    /// </para>
    /// </summary>
    [Fact]
    public void Gerechnete_Werte_landen_nicht_in_der_Datei()
    {
        string json = Text(TdFormatIo.Schreiben(Beispiel()));

        foreach (string abgeleitet in
                 new[] { "IstLeer", "IstQuerformat", "TextBreiteCm", "TextHoeheCm",
                         "IstTextmarke", "IstFortsetzung", "ShowLegend", "FarbeJeElement",
                         "FarbenGebraucht" })
        {
            Assert.DoesNotContain($"\"{abgeleitet}\"", json);
        }

        // `TdPageSetup.Name` lässt sich nicht am Feldnamen prüfen — eine Reihe eines Diagramms
        // hat auch einen `Name`, und der ist echte Angabe. Geprüft wird deshalb sein **Wert**:
        // Das Beispiel steht auf A4, und „A4" darf nirgends in der Datei stehen.
        Assert.DoesNotContain("A4", json);
    }

    /// <summary>
    /// Die Diskriminatoren sind Datenformat. Sie stehen hier **wörtlich** — genau wie
    /// <c>AlteTypnamenTests</c> die alten <c>_type</c>-Namen wörtlich festhält. Wer einen
    /// Typ umbenennt, bekommt einen roten Lauf und nicht ein stilles Dokument ohne Inhalt.
    /// </summary>
    [Fact]
    public void Die_Diskriminatoren_stehen_fest()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(
                    new TdParagraph([
                        new TdRun("x"),
                        new TdLineBreak(),
                        new TdField(TdFieldKind.PageNumber),
                        TdHyperlink.Text("ziel.md", "y"),
                        new TdImage(Guid.NewGuid(), "png", 1, 1),
                        new TdChart(TdChartKind.Pie, 1, 1),
                    ]),
                    new TdPageBreak(),
                    new TdTable(TdTableRow.Text("z"))),
            },
        };

        string json = Text(TdFormatIo.Schreiben(doc));

        Assert.Contains("\"t\":\"p\"", json);
        Assert.Contains("\"t\":\"pagebreak\"", json);
        Assert.Contains("\"t\":\"table\"", json);
        Assert.Contains("\"t\":\"run\"", json);
        Assert.Contains("\"t\":\"break\"", json);
        Assert.Contains("\"t\":\"field\"", json);
        Assert.Contains("\"t\":\"hyperlink\"", json);
        Assert.Contains("\"t\":\"image\"", json);
        Assert.Contains("\"t\":\"chart\"", json);
        // Und der Text eines Laufs steht unter "s" — auch das ist Format.
        Assert.Contains("\"s\":\"x\"", json);
    }

    /// <summary>
    /// Die Kennung steht vorn und ist der einzige Weg, das eigene Format von dem zu
    /// unterscheiden, was heute im selben Feld liegt.
    /// </summary>
    [Fact]
    public void Das_eigene_Format_gibt_sich_zu_erkennen()
    {
        var bytes = TdFormatIo.Schreiben(TdDocument.Leer());

        Assert.Equal((byte)'G', bytes[0]);
        Assert.Equal((byte)'N', bytes[1]);
        Assert.Equal((byte)'T', bytes[2]);
        Assert.Equal((byte)'D', bytes[3]);
        Assert.True(TdFormatIo.IstEigenesFormat(bytes));
    }

    /// <summary>
    /// **Ein Altformat ist kein Fehler, sondern der Normalfall.** <c>Lesen</c> gibt dafür
    /// <c>null</c> zurück und wirft nicht — sonst flöge beim Öffnen jedes Bestandsdokuments
    /// eine Ausnahme, und zwar genau so lange, wie die Übernahme läuft.
    /// </summary>
    [Theory]
    [InlineData(@"{\rtf1\ansi Hallo}")]     // RTF
    [InlineData("PKirgendwas")] // XamlPackage (ZIP)
    [InlineData("")]                        // ein nie gespeichertes Dokument
    [InlineData("GNT")]                     // kürzer als die Kennung
    public void Fremde_Bytes_werden_als_fremd_erkannt(string inhalt)
    {
        var bytes = Encoding.UTF8.GetBytes(inhalt);

        Assert.False(TdFormatIo.IstEigenesFormat(bytes));
        Assert.Null(TdFormatIo.Lesen(bytes));
    }

    /// <summary>
    /// Ein <c>null</c>-Text darf nicht durchkommen — eine übernommene Altdatei könnte ihn
    /// mitbringen, und der Wächter dafür ist ein Test und kein gutes Gedächtnis (dieselbe
    /// Vorsorge wie bei <c>TextDoc</c>, HANDOFF §7).
    /// </summary>
    [Fact]
    public void Ein_Lauf_ohne_Text_ist_leer_und_nicht_null()
    {
        var lauf = new TdRun { Text = null! };
        Assert.Equal("", lauf.Text);
        Assert.Equal("", lauf.PlainText());
    }

    // ==================== Hilfsmittel ====================

    private static string Text(byte[] bytes) => Encoding.UTF8.GetString(bytes);

    /// <summary>
    /// Ein Dokument, das **alles** benutzt, was Schritt 1 kann. Wer das Modell erweitert,
    /// erweitert auch dieses Beispiel — sonst bewacht der Roundtrip das neue Feld nicht.
    /// Dieselbe Regel wie bei <c>Beispieldokument</c> für die Datenbank (HANDOFF §7).
    /// </summary>
    private static TdDocument Beispiel() => new()
    {
        DefaultCharFormat = { FontFamily = "Calibri", FontSize = 11 },
        DefaultParaFormat = { SpaceAfterPt = 6, LineSpacing = 1.15 },
        Lists = { TdListDefinition.Nummern(1), TdListDefinition.Punkte(2) },
        Sections =
        {
            // Ein Deckblatt quer, der Rest hoch — der Fall, für den es Abschnitte gibt.
            new TdSection(new TdParagraph("Deckblatt"))
            {
                Page = TdPageSetup.A4.Quer(),
            },
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

                    // Das Wasserzeichen gehört zur Seiteneinrichtung und nicht zum Inhalt —
                    // in DOCX hängt es in der Kopfzeile (§4.21).
                    Watermark = new TdImage(Wasserzeichenkennung, "png", 12.0, 8.0),
                    WatermarkOpacity = 0.4,
                },
            },
        },
    };

    private static readonly Guid Bildkennung = new("88888888-8888-8888-8888-888888888888");
    private static readonly Guid Wasserzeichenkennung = new("99999999-9999-9999-9999-999999999999");

    private static TdBlock[] Inhalt() =>
        [
            // Der Titel: Überschrift ja, Verzeichniseintrag nein (§4.23). Er steht hier, damit
            // der Roundtrip `ExcludeFromToc` bewacht — ohne ihn käme das Feld nie vor.
            new TdParagraph([new TdRun("Das Beispieldokument")])
            {
                Format = { OutlineLevel = 1, ExcludeFromToc = true, Alignment = TdAlign.Center },
                CharFormat = { FontSize = 26, Bold = true },
            },

            // Das Inhaltsverzeichnis steht vor den Überschriften, die es aufzählt — es ist ein
            // Feld und trägt seinen Text nicht bei sich (§4.20).
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
            // drei Feldarten mit eigener Zusatzangabe.
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
                new TdField(TdFieldKind.Title, null) { Format = { Italic = true } },
                new TdRun("."),
            ]),

            // Zwei Listen, zwei Ebenen — Nummerierung und Aufzählung nebeneinander.
            new TdParagraph("Erster Punkt") { List = new TdListRef(1, 0) },
            new TdParagraph("Unterpunkt") { List = new TdListRef(1, 1) },
            new TdParagraph("Ein Strich") { List = new TdListRef(2, 0) },

            // Eine Tabelle mit beiden Verbindungsarten.
            new TdTable(
                new TdTableRow(
                    new TdTableCell(new TdParagraph("Kopf über zwei")) { ColumnSpan = 2, Shading = "#DDEEFF" },
                    TdTableCell.Text("Rest"))
                { IsHeader = true, MinHeightCm = 1.0 },
                new TdTableRow(
                    new TdTableCell(new TdParagraph("verbunden")) { VerticalMerge = TdVerticalMerge.Restart },
                    TdTableCell.Text("b"),
                    new TdTableCell(new TdParagraph("c")) { VerticalAlign = TdVAlign.Bottom }),
                new TdTableRow(
                    new TdTableCell { VerticalMerge = TdVerticalMerge.Continue },
                    TdTableCell.Text("e"),
                    TdTableCell.Text("f")))
            {
                ColumnWidthsCm = { 4.0, 5.5, 3.25 },
                Format = { InsideV = TdBorder.Keine, CellPaddingTopCm = 0.1 },
            },

            // Ein Bild und ein Diagramm — beide sind Stücke und keine Blöcke (§4.21).
            new TdParagraph([new TdImage(Bildkennung, "jpg", 6.5, 4.25) { AltText = "Der Aufbau" }]),
            new TdParagraph([
                new TdChart(TdChartKind.Line, 12.0, 7.5)
                {
                    Title = "Woche",
                    Categories = { "Mo", "Di", "Mi" },
                    Series =
                    {
                        new TdChartSeries("Umsatz", 4, 7, 3),
                        new TdChartSeries("Kosten", 2, 3, 2.5),
                    },
                    Palette = { "#2563EB", "#14B8A6" },
                    AltText = "Umsatz und Kosten",
                },
            ]),

            new TdPageBreak(),
            new TdParagraph("Nach dem Umbruch.") { Format = { PageBreakBefore = true } },
        ];

    private static void GleichesDokument(TdDocument a, TdDocument b)
    {
        Assert.Equal(a.Version, b.Version);
        GleichesZeichenformat(a.DefaultCharFormat, b.DefaultCharFormat);
        GleichesAbsatzformat(a.DefaultParaFormat, b.DefaultParaFormat);

        Assert.Equal(a.Lists.Count, b.Lists.Count);
        for (int i = 0; i < a.Lists.Count; i++)
        {
            Assert.Equal(a.Lists[i].Id, b.Lists[i].Id);
            Assert.Equal(a.Lists[i].Levels.Count, b.Lists[i].Levels.Count);
            for (int k = 0; k < a.Lists[i].Levels.Count; k++)
            {
                Assert.Equal(a.Lists[i].Levels[k].Marker, b.Lists[i].Levels[k].Marker);
                Assert.Equal(a.Lists[i].Levels[k].Text, b.Lists[i].Levels[k].Text);
                Assert.Equal(a.Lists[i].Levels[k].Start, b.Lists[i].Levels[k].Start);
                Assert.Equal(a.Lists[i].Levels[k].IndentCm, b.Lists[i].Levels[k].IndentCm, 3);
                Assert.Equal(a.Lists[i].Levels[k].HangingCm, b.Lists[i].Levels[k].HangingCm, 3);
            }
        }

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

                    Assert.Equal(pa.List is null, pb.List is null);
                    if (pa.List is not null && pb.List is not null)
                    {
                        Assert.Equal(pa.List.ListId, pb.List.ListId);
                        Assert.Equal(pa.List.Level, pb.List.Level);
                    }

                    GleicheStuecke(pa.Inlines, pb.Inlines);
                    break;
                }

                case TdPageBreak:
                    Assert.IsType<TdPageBreak>(b[i]);
                    break;

                case TdTable ta:
                {
                    var tb = Assert.IsType<TdTable>(b[i]);

                    Assert.Equal(ta.ColumnWidthsCm, tb.ColumnWidthsCm);
                    Assert.Equal(ta.Format.InsideV, tb.Format.InsideV);
                    Assert.Equal(ta.Format.CellPaddingTopCm, tb.Format.CellPaddingTopCm, 3);

                    Assert.Equal(ta.Rows.Count, tb.Rows.Count);
                    for (int z = 0; z < ta.Rows.Count; z++)
                    {
                        Assert.Equal(ta.Rows[z].IsHeader, tb.Rows[z].IsHeader);
                        Assert.Equal(ta.Rows[z].MinHeightCm, tb.Rows[z].MinHeightCm);
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

                // Wer einen Blocktyp ergänzt und diesen Zweig vergisst, bekommt hier einen
                // roten Lauf statt eines stillen Lochs im Wächter — dasselbe Muster wie in
                // DatenbankRoundtripTests.GleichesElement.
                default:
                    Assert.Fail($"Kein Vergleich für {a[i].GetType().Name} — bitte ergänzen.");
                    break;
            }
        }
    }

    /// <summary>
    /// Der Vergleich der Textstücke. <b>Ein Feld hat keinen Klartext</b> — wer nur
    /// <c>PlainText</c> vergliche, bemerkte nicht, dass aus einem Datumsfeld ein Titel
    /// geworden ist. Der <c>default</c>-Zweig ist Absicht: ein neuer Stücktyp macht diesen
    /// Wächter rot, statt still an ihm vorbeizugehen.
    /// </summary>
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

                case TdImage ba:
                {
                    var bb = (TdImage)b[k];
                    Assert.Equal(ba.BlobId, bb.BlobId);
                    Assert.Equal(ba.Extension, bb.Extension);
                    GleicheGrafik(ba, bb);
                    break;
                }

                case TdChart da:
                {
                    var db = (TdChart)b[k];
                    GleichesDiagramm(da, db);
                    GleicheGrafik(da, db);
                    break;
                }

                default:
                    Assert.Fail($"Kein Vergleich für {a[k].GetType().Name} — bitte ergänzen.");
                    break;
            }
        }
    }

    private static void GleicheGrafik(TdGraphic a, TdGraphic b)
    {
        Assert.Equal(a.WidthCm, b.WidthCm, 2);
        Assert.Equal(a.HeightCm, b.HeightCm, 2);
        Assert.Equal(a.AltText, b.AltText);
    }

    /// <summary>
    /// <b>Ein Diagramm ist seine Daten.</b> Wer nur Art und Größe vergliche, bemerkte nicht,
    /// dass die Zahlen fehlen — und genau die sind der Grund, warum es das Diagramm im Modell
    /// überhaupt gibt (§4.21).
    /// </summary>
    private static void GleichesDiagramm(TdChart a, TdChart b)
    {
        Assert.Equal(a.Kind, b.Kind);
        Assert.Equal(a.Title, b.Title);
        Assert.Equal(a.Categories, b.Categories);
        Assert.Equal(a.Palette, b.Palette);

        Assert.Equal(a.Series.Count, b.Series.Count);
        for (int i = 0; i < a.Series.Count; i++)
        {
            Assert.Equal(a.Series[i].Name, b.Series[i].Name);
            Assert.Equal(a.Series[i].Values, b.Series[i].Values);
        }
    }

    private static void GleicheSeite(TdPageSetup a, TdPageSetup b)
    {
        Assert.Equal(a.WidthCm, b.WidthCm, 2);
        Assert.Equal(a.HeightCm, b.HeightCm, 2);
        Assert.Equal(a.MarginLeftCm, b.MarginLeftCm, 2);
        Assert.Equal(a.MarginTopCm, b.MarginTopCm, 2);
        Assert.Equal(a.MarginRightCm, b.MarginRightCm, 2);
        Assert.Equal(a.MarginBottomCm, b.MarginBottomCm, 2);
        Assert.Equal(a.HeaderText, b.HeaderText);
        Assert.Equal(a.FooterText, b.FooterText);
        Assert.Equal(a.SuppressOnFirstPage, b.SuppressOnFirstPage);

        Assert.Equal(a.Watermark is null, b.Watermark is null);
        if (a.Watermark is not null && b.Watermark is not null)
        {
            Assert.Equal(a.Watermark.BlobId, b.Watermark.BlobId);
            Assert.Equal(a.Watermark.Extension, b.Watermark.Extension);
            GleicheGrafik(a.Watermark, b.Watermark);
            Assert.Equal(a.WatermarkOpacity, b.WatermarkOpacity, 2);
        }
    }

    private static void GleichesZeichenformat(TdCharFormat a, TdCharFormat b)
    {
        Assert.Equal(a.FontFamily, b.FontFamily);
        Assert.Equal(a.FontSize, b.FontSize);
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
        Assert.Equal(a.LeftIndentCm, b.LeftIndentCm);
        Assert.Equal(a.RightIndentCm, b.RightIndentCm);
        Assert.Equal(a.FirstLineIndentCm, b.FirstLineIndentCm);
        Assert.Equal(a.SpaceBeforePt, b.SpaceBeforePt);
        Assert.Equal(a.SpaceAfterPt, b.SpaceAfterPt);
        Assert.Equal(a.LineSpacing, b.LineSpacing);
        Assert.Equal(a.OutlineLevel, b.OutlineLevel);
        Assert.Equal(a.ExcludeFromToc, b.ExcludeFromToc);
        Assert.Equal(a.KeepWithNext, b.KeepWithNext);
        Assert.Equal(a.PageBreakBefore, b.PageBreakBefore);
    }
}
