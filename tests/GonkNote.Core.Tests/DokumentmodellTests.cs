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
        doc.Blocks.Add(absatz);

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
            Blocks = { new TdParagraph("Ende"), new TdParagraph("Anfang") },
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
            Blocks =
            {
                new TdParagraph([new TdRun("zwei   Wörter"), new TdLineBreak(), new TdRun("  drei")]),
                new TdParagraph("   "),
            },
        };

        Assert.Equal(3, doc.WordCount());
    }

    /// <summary>Ein leeres Dokument hat einen Absatz — sonst hätte der Cursor keinen Ort.</summary>
    [Fact]
    public void Ein_leeres_Dokument_hat_einen_leeren_Absatz()
    {
        var doc = TdDocument.Leer();
        Assert.Single(doc.Blocks);
        Assert.IsType<TdParagraph>(doc.Blocks[0]);
        Assert.Equal("", doc.PlainText());
        Assert.Equal(0, doc.WordCount());
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
        var doc = new TdDocument { Blocks = { new TdParagraph("schlicht") } };

        string json = Text(TdFormatIo.Schreiben(doc));

        Assert.DoesNotContain("null", json);
        Assert.DoesNotContain("FontFamily", json);
        Assert.Contains("schlicht", json);
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
            Blocks =
            {
                new TdParagraph([new TdRun("x"), new TdLineBreak()]),
                new TdPageBreak(),
            },
        };

        string json = Text(TdFormatIo.Schreiben(doc));

        Assert.Contains("\"t\":\"p\"", json);
        Assert.Contains("\"t\":\"pagebreak\"", json);
        Assert.Contains("\"t\":\"run\"", json);
        Assert.Contains("\"t\":\"break\"", json);
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
        Assert.Equal(a.Version, b.Version);
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
                        var ia = pa.Inlines[k];
                        var ib = pb.Inlines[k];
                        Assert.Equal(ia.GetType(), ib.GetType());
                        Assert.Equal(ia.PlainText(), ib.PlainText());
                        GleichesZeichenformat(ia.Format, ib.Format);
                    }
                    break;
                }

                case TdPageBreak:
                    Assert.IsType<TdPageBreak>(b.Blocks[i]);
                    break;

                // Wer einen Blocktyp ergänzt und diesen Zweig vergisst, bekommt hier einen
                // roten Lauf statt eines stillen Lochs im Wächter — dasselbe Muster wie in
                // DatenbankRoundtripTests.GleichesElement.
                default:
                    Assert.Fail($"Kein Vergleich für {a.Blocks[i].GetType().Name} — bitte ergänzen.");
                    break;
            }
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
        Assert.Equal(a.KeepWithNext, b.KeepWithNext);
        Assert.Equal(a.PageBreakBefore, b.PageBreakBefore);
    }
}
