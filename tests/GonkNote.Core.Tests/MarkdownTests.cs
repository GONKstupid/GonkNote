using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="Markdown"/> — der Zerleger hinter „Hilfe → Erste Schritte" und dem gerenderten
/// README. Neu in Phase 3, weil der Linux-Kopf kein <c>FlowDocument</c> hat und die
/// Grammatik deshalb aus <c>MarkdownFlow</c> heraus nach Core musste (HANDOFF §4.1, §4.12).
///
/// <para>
/// Getestet wird, was ein <b>falsch gelesenes Dokument</b> wäre und nicht, wie es aussieht:
/// eine Tabelle, die als Absatz durchgeht; ein Verweis, dessen Ziel verschwindet; eine
/// Unterliste, die zur eigenen Liste wird. Die Darstellung selbst prüft niemand
/// automatisiert — die steht im Kopf und wird am laufenden Programm angesehen.
/// </para>
///
/// <para>
/// <b>Der Verweis-Fall ist kein beliebiger.</b> Genau daran ist der Markdown-Export in
/// Phase 1 gescheitert (HANDOFF §7, „Markdown-Export"): der allgemeinere Fall griff zuerst,
/// der Linktext kam durch und das Ziel fiel weg. Hier ist der Wächter für die Leserichtung.
/// </para>
/// </summary>
public sealed class MarkdownTests
{
    private static string Text(IReadOnlyList<MdInline> inlines) =>
        string.Concat(inlines.Select(i => i switch
        {
            MdText t => t.Text,
            MdCodeSpan c => c.Text,
            MdBold b => Text(b.Inner),
            MdItalic k => Text(k.Inner),
            MdStrike d => Text(d.Inner),
            MdLink l => l.Text,
            _ => "",
        }));

    // ==================== Die vier Formen aus Phase 5, Schritt ④ ====================
    //
    // Sie sind mit dem Umzug des Markdown-Imports nach Core dazugekommen (TdMarkdown.Lesen):
    // Der Importer des WPF-Kopfs kannte sie, der Zerleger hier nicht.

    /// <summary>
    /// <c>~~durchgestrichen~~</c> — und der Wächter ist nicht nur wegen des Imports da:
    /// <b><see cref="TdMarkdown.Schreiben"/> schreibt seit jeher <c>~~</c></b>, und bis Schritt
    /// ④ las der Zerleger es als gewöhnlichen Text zurück. <b>Der eigene Export war also
    /// keine Rundreise durch den eigenen Leser.</b>
    /// </summary>
    [Fact]
    public void Durchgestrichenes_wird_erkannt()
    {
        var p = Assert.IsType<MdParagraph>(Markdown.Parse("a ~~weg~~ b").Single());

        Assert.Equal("a weg b", Text(p.Inlines));
        Assert.Contains(p.Inlines, i => i is MdStrike);
    }

    /// <summary>
    /// <c>***fett und kursiv***</c> — <b>als Fett um Kursiv und nicht als dritte Form.</b>
    /// Das Modell kennt zwei Schalter; ein eigener Knoten müsste durch jede Stelle hindurch,
    /// die über Stücke läuft.
    /// </summary>
    [Fact]
    public void Fett_und_kursiv_zugleich_wird_geschachtelt()
    {
        var p = Assert.IsType<MdParagraph>(Markdown.Parse("***beides***").Single());

        var fett = Assert.IsType<MdBold>(Assert.Single(p.Inlines));
        var kursiv = Assert.IsType<MdItalic>(Assert.Single(fett.Inner));

        Assert.Equal("beides", Text(kursiv.Inner));
    }

    /// <summary>Word und viele Editoren schreiben Fett als <c>__…__</c> statt <c>**…**</c>.</summary>
    [Fact]
    public void Unterstriche_gelten_als_fett()
    {
        var p = Assert.IsType<MdParagraph>(Markdown.Parse("__fett__").Single());

        Assert.Equal("fett", Text(Assert.IsType<MdBold>(Assert.Single(p.Inlines)).Inner));
    }

    /// <summary>
    /// <b><c>![alt](x)</c> ist ein Bild und kein Verweis mit Ausrufezeichen davor</b> — die
    /// Reihenfolge im Muster entscheidet das, und sie ist die einzige Stelle, an der ein
    /// falscher Vorrang <i>zwei</i> Formen zugleich verdirbt (§7, dieselbe Erbfolge wie beim
    /// Verweis).
    /// </summary>
    [Fact]
    public void Ein_Bild_ist_kein_Verweis()
    {
        var p = Assert.IsType<MdParagraph>(Markdown.Parse("![Foto](bild.png)").Single());

        var bild = Assert.IsType<MdImage>(Assert.Single(p.Inlines));
        Assert.Equal("Foto", bild.Alt);
        Assert.Equal("bild.png", bild.Source);
    }

    /// <summary>Und die Gegenprobe: ein gewöhnlicher Verweis bleibt einer.</summary>
    [Fact]
    public void Ein_Verweis_neben_einem_Bild_bleibt_ein_Verweis()
    {
        var p = Assert.IsType<MdParagraph>(
            Markdown.Parse("![a](x.png) und [b](y.md)").Single());

        Assert.Contains(p.Inlines, i => i is MdImage);
        Assert.Equal("y.md", Assert.IsType<MdLink>(p.Inlines[^1]).Target);
    }

    /// <summary>
    /// <b>Der eigene Export geht jetzt durch den eigenen Leser</b> — die Rundreise, die es
    /// vor Schritt ④ nicht gab. Geprüft wird an dem, was <see cref="TdMarkdown.Schreiben"/>
    /// wirklich schreibt, und nicht an einem von Hand getippten Text.
    /// </summary>
    [Fact]
    public void Was_der_eigene_Export_schreibt_liest_der_eigene_Zerleger()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(new TdParagraph(new TdInline[]
                {
                    new TdRun("normal "),
                    new TdRun("weg") { Format = { Strikethrough = true } },
                    new TdRun(" und "),
                    new TdRun("beides") { Format = { Bold = true, Italic = true } },
                })),
            },
        };

        var gelesen = Markdown.Parse(TdMarkdown.Schreiben(doc));
        var p = Assert.IsType<MdParagraph>(gelesen.Single());

        Assert.Equal("normal weg und beides", Text(p.Inlines));
        Assert.Contains(p.Inlines, i => i is MdStrike);
        Assert.Contains(p.Inlines, i => i is MdBold);
    }

    // ==================== Blöcke ====================

    [Fact]
    public void Leerer_Text_ergibt_keine_Bloecke()
    {
        Assert.Empty(Markdown.Parse(""));
    }

    [Fact]
    public void Ueberschriften_kennen_ihre_Ebene()
    {
        var b = Markdown.Parse("# Eins\n\n### Drei");
        Assert.Equal(1, Assert.IsType<MdHeading>(b[0]).Level);
        Assert.Equal(3, Assert.IsType<MdHeading>(b[1]).Level);
        Assert.Equal("Drei", Text(((MdHeading)b[1]).Inlines));
    }

    [Fact]
    public void Ein_Absatz_zieht_seine_Zeilen_zusammen()
    {
        var p = Assert.IsType<MdParagraph>(Markdown.Parse("Erste Zeile\nzweite Zeile")[0]);
        Assert.Equal("Erste Zeile zweite Zeile", Text(p.Inlines));
    }

    [Fact]
    public void Ein_Codeblock_behaelt_seine_Zeilenumbrueche()
    {
        var c = Assert.IsType<MdCodeBlock>(Markdown.Parse("```bash\neins\nzwei\n```")[0]);
        Assert.Equal("eins\nzwei", c.Text);
    }

    [Fact]
    public void Ein_Codeblock_verschluckt_kein_Markdown()
    {
        // Was im Codeblock steht, ist Text — sonst würde eine Anleitung, die Markdown zeigt,
        // von sich selbst formatiert.
        var b = Markdown.Parse("```\n# keine Ueberschrift\n```");
        Assert.Single(b);
        Assert.Equal("# keine Ueberschrift", Assert.IsType<MdCodeBlock>(b[0]).Text);
    }

    [Fact]
    public void Eine_Trennlinie_ist_ein_eigener_Block()
    {
        Assert.IsType<MdRule>(Markdown.Parse("---")[0]);
    }

    [Fact]
    public void Ein_Zitat_enthaelt_wieder_ganze_Bloecke()
    {
        var q = Assert.IsType<MdQuote>(Markdown.Parse("> ## Achtung\n> Zwei Zeilen.")[0]);
        Assert.Equal(2, Assert.IsType<MdHeading>(q.Blocks[0]).Level);
        Assert.Equal("Zwei Zeilen.", Text(Assert.IsType<MdParagraph>(q.Blocks[1]).Inlines));
    }

    // ==================== Listen ====================

    [Fact]
    public void Eine_Aufzaehlung_sammelt_ihre_Punkte()
    {
        var l = Assert.IsType<MdList>(Markdown.Parse("- eins\n- zwei\n- drei")[0]);
        Assert.False(l.Ordered);
        Assert.Equal(3, l.Items.Count);
        Assert.Equal("zwei", Text(l.Items[1].Inlines));
    }

    [Fact]
    public void Eine_nummerierte_Liste_wird_als_solche_erkannt()
    {
        Assert.True(Assert.IsType<MdList>(Markdown.Parse("1. eins\n2. zwei")[0]).Ordered);
    }

    [Fact]
    public void Eine_eingerueckte_Liste_haengt_am_Punkt_darueber()
    {
        // Nicht als zweite Liste daneben: sonst stünde die Einrückung im Dokument und wäre
        // in der Darstellung weg.
        var b = Markdown.Parse("- oben\n  - drunter\n- daneben");
        Assert.Single(b);

        var l = Assert.IsType<MdList>(b[0]);
        Assert.Equal(2, l.Items.Count);
        Assert.Equal("drunter", Text(l.Items[0].Sub!.Items[0].Inlines));
        Assert.Null(l.Items[1].Sub);
    }

    [Fact]
    public void Eine_Fortsetzungszeile_gehoert_zum_Punkt_darueber()
    {
        var l = Assert.IsType<MdList>(Markdown.Parse("- ein Punkt,\n  der weitergeht")[0]);
        Assert.Single(l.Items);
        Assert.Equal("ein Punkt, der weitergeht", Text(l.Items[0].Inlines));
    }

    // ==================== Tabellen ====================

    [Fact]
    public void Eine_Tabelle_braucht_ihre_Trennzeile()
    {
        // Ohne Trennzeile ist es keine Tabelle, sondern ein Absatz — sonst würde jede Zeile,
        // die mit einem Strich beginnt, zur Tabelle.
        Assert.IsType<MdParagraph>(Markdown.Parse("| a | b |\n| c | d |")[0]);
    }

    [Fact]
    public void Eine_Tabelle_kennt_Kopf_und_Zeilen()
    {
        var t = Assert.IsType<MdTable>(Markdown.Parse("| a | b |\n|---|---|\n| c | d |\n| e | f |")[0]);
        Assert.Equal(2, t.Columns);
        Assert.Equal("b", Text(t.Header[1]));
        Assert.Equal(2, t.Rows.Count);
        Assert.Equal("e", Text(t.Rows[1][0]));
    }

    [Fact]
    public void Die_Spaltenzahl_richtet_sich_nach_der_laengsten_Zeile()
    {
        // Eine zu kurze Kopfzeile darf keine Zelle abschneiden — Daten verschwinden sonst
        // still.
        var t = Assert.IsType<MdTable>(Markdown.Parse("| a |\n|---|\n| b | c |")[0]);
        Assert.Equal(2, t.Columns);
    }

    // ==================== Textstücke ====================

    [Fact]
    public void Fett_und_kursiv_werden_unterschieden()
    {
        var s = Markdown.Inline("**fett** und *kursiv*");
        Assert.IsType<MdBold>(s[0]);
        Assert.IsType<MdItalic>(s[2]);
    }

    [Fact]
    public void Fett_darf_Kursives_enthalten()
    {
        // Der Grund für die Reihenfolge im Muster: fett vor kursiv, sonst reißt ** in zwei *
        // auseinander.
        var b = Assert.IsType<MdBold>(Markdown.Inline("**fett mit *kursiv* darin**")[0]);
        Assert.Contains(b.Inner, i => i is MdItalic);
    }

    [Fact]
    public void Sternchen_im_Code_bleiben_woertlich()
    {
        var c = Assert.IsType<MdCodeSpan>(Markdown.Inline("`a*b*c`")[0]);
        Assert.Equal("a*b*c", c.Text);
    }

    [Fact]
    public void Ein_Verweis_behaelt_sein_Ziel()
    {
        var l = Assert.IsType<MdLink>(Markdown.Inline("[Erste Schritte](ERSTE-SCHRITTE.md)")[0]);
        Assert.Equal("Erste Schritte", l.Text);
        Assert.Equal("ERSTE-SCHRITTE.md", l.Target);
    }

    [Fact]
    public void Ein_relatives_Ziel_bleibt_relativ()
    {
        // Beim Export ist genau das schon einmal schiefgegangen: aus `kapitel-2.md` wurde
        // ein absoluter file:///-Pfad (HANDOFF §7, „Markdown-Export").
        Assert.Equal("kapitel-2.md", Assert.IsType<MdLink>(Markdown.Inline("[K2](kapitel-2.md)")[0]).Target);
    }

    [Fact]
    public void Ein_maskiertes_Sternchen_bleibt_ein_Sternchen()
    {
        Assert.Equal("2 * 3", Text(Markdown.Inline(@"2 \* 3")));
    }

    // ==================== Die echten Dokumente ====================

    [Fact]
    public void Unbekanntes_wird_zu_Text_und_nicht_verworfen()
    {
        // Die Zusage der Klasse: ein Dialog kann nie leer bleiben.
        var p = Assert.IsType<MdParagraph>(Markdown.Parse("<div class=\"x\">roh</div>")[0]);
        Assert.Contains("roh", Text(p.Inlines));
    }
}
