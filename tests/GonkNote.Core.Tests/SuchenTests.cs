using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// Suchen &amp; Ersetzen — <see cref="TdSuche"/> (Phase 5, Schritt ①c, §4.80).
///
/// <para>
/// <b>Der Weg entsteht hier neu und zieht nicht um</b>, und das ist der Unterschied zu
/// §4.77 und §4.78: Der WPF-Kopf hat Suchen &amp; Ersetzen seit jeher, aber vollständig auf
/// <c>TextPointer</c>/<c>TextRange</c> — eine echte Windows-Schranke, keine Verwechslung.
/// *Nicht jede Datei in einem Kopf liegt dort zu Unrecht.*
/// </para>
/// <para>
/// <b>Geprüft wird gegen das Modell und nicht gegen einen Kopf</b> — damit läuft dieser
/// Wächter auch unter Linux, wo es kein WPF gibt.
/// </para>
/// </summary>
public sealed class SuchenTests
{
    // ==================== Hilfsmittel ====================

    private static TdDocument Dok(params TdParagraph[] absaetze)
    {
        var doc = new TdDocument();
        var abschnitt = new TdSection();
        abschnitt.Blocks.AddRange(absaetze);
        doc.Sections.Add(abschnitt);
        return doc;
    }

    private static TdParagraph Abs(params TdInline[] stuecke) => new(stuecke);
    private static TdParagraph Text(string text) => new(text);
    private static TdRun Fett(string text) => new(text, new TdCharFormat { Bold = true });

    private static TdPosition Bei(TdDocument doc, int absatz, int linear) =>
        TdCursor.AusLinear(TdCursor.AbsatzAn(doc, absatz)!, absatz, linear);

    /// <summary>Der Klartext eines Dokuments, Absätze durch <c>|</c> getrennt.</summary>
    private static string Klartext(TdDocument doc) =>
        string.Join("|", TdCursor.Absaetze(doc).Select(a =>
            string.Concat(TdCursor.Stuecke(a).Select(s => s is TdRun r ? r.Text : ""))));

    /// <summary>Was an einer Auswahl steht — der Treffer selbst.</summary>
    private static string Treffertext(TdDocument doc, TdSelection auswahl) =>
        TdCursor.Text(doc, auswahl);

    // ==================== Finden ====================

    [Fact]
    public void Findet_den_ersten_Treffer_ab_dem_Anfang()
    {
        var doc = Dok(Text("Der Hund bellt."));

        var treffer = TdSuche.Naechster(doc, "Hund", TdCursor.Anfang(doc));

        Assert.NotNull(treffer);
        Assert.Equal("Hund", Treffertext(doc, treffer!.Value));
    }

    [Fact]
    public void Was_nicht_dasteht_wird_nicht_gefunden()
    {
        var doc = Dok(Text("Der Hund bellt."));
        Assert.Null(TdSuche.Naechster(doc, "Katze", TdCursor.Anfang(doc)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Eine_leere_Suche_findet_nichts(string? suche)
    {
        var doc = Dok(Text("Der Hund bellt."));
        Assert.Null(TdSuche.Naechster(doc, suche!, TdCursor.Anfang(doc)));
    }

    [Fact]
    public void Ein_leeres_Dokument_bringt_die_Suche_nicht_aus_dem_Tritt()
    {
        var doc = Dok(Text(""));
        Assert.Null(TdSuche.Naechster(doc, "irgendwas", TdCursor.Anfang(doc)));
        Assert.Empty(TdSuche.AlleErsetzen(doc, "irgendwas", "x"));
    }

    /// <summary>Gesucht wird ohne Rücksicht auf Groß- und Kleinschreibung — wie drüben.</summary>
    [Theory]
    [InlineData("hund")]
    [InlineData("HUND")]
    [InlineData("hUnD")]
    public void Gross_und_Kleinschreibung_spielt_keine_Rolle(string suche)
    {
        var doc = Dok(Text("Der Hund bellt."));

        var treffer = TdSuche.Naechster(doc, suche, TdCursor.Anfang(doc));

        Assert.NotNull(treffer);
        Assert.Equal("Hund", Treffertext(doc, treffer!.Value));
    }

    [Fact]
    public void Findet_ueber_Absatzgrenzen_hinweg_den_naechsten()
    {
        var doc = Dok(Text("erste Zeile"), Text("zweite Zeile"), Text("dritte Zeile"));

        var treffer = TdSuche.Naechster(doc, "Zeile", Bei(doc, 0, 11));

        Assert.NotNull(treffer);
        Assert.Equal(1, treffer!.Value.Start.Paragraph);
    }

    /// <summary>
    /// <b>Der Treffer, den der WPF-Kopf nicht findet.</b> Dort wird *„innerhalb einzelner
    /// Text-Runs"* gesucht — wer „Hallo" schreibt und „llo" fett macht, findet „Hallo" nicht
    /// mehr. Hier ist die Formatgrenze für die Suche unsichtbar (§4.80).
    /// </summary>
    [Fact]
    public void Ein_Treffer_ueber_eine_Formatgrenze_wird_gefunden()
    {
        var doc = Dok(Abs(new TdRun("Ha"), Fett("llo"), new TdRun(" Welt")));

        var treffer = TdSuche.Naechster(doc, "Hallo", TdCursor.Anfang(doc));

        Assert.NotNull(treffer);
        Assert.Equal("Hallo", Treffertext(doc, treffer!.Value));
    }

    // ==================== Der Umlauf ====================

    /// <summary>
    /// <b>Wer am Ende ankommt, sucht vom Anfang weiter.</b> Ohne den Umlauf müsste man nach
    /// dem letzten Treffer von Hand zum Dokumentanfang springen — und das Werkzeug wäre nach
    /// einer Runde stumm.
    /// </summary>
    [Fact]
    public void Am_Ende_faengt_die_Suche_vorne_wieder_an()
    {
        var doc = Dok(Text("Hund"), Text("Katze"), Text("Hund"));

        var treffer = TdSuche.Naechster(doc, "Hund", TdCursor.Ende(doc));

        Assert.NotNull(treffer);
        Assert.Equal(0, treffer!.Value.Start.Paragraph);
    }

    /// <summary>
    /// <b>Der Umlauf darf denselben Treffer nicht zweimal liefern.</b> Steht der Cursor
    /// hinter dem einzigen Vorkommen, ist der Umlauf-Treffer wieder dasselbe Wort — das ist
    /// richtig. Steht er <i>davor</i>, muss der erste Durchgang ihn finden und nicht der
    /// zweite.
    /// </summary>
    [Fact]
    public void Beim_Umlauf_kommt_kein_Treffer_doppelt()
    {
        var doc = Dok(Text("Hund und Katze und Hund"));

        // Zwischen den beiden „Hund" — der nächste ist der hintere.
        var erster = TdSuche.Naechster(doc, "Hund", Bei(doc, 0, 10));
        Assert.NotNull(erster);
        Assert.Equal(19, TdCursor.Linear(TdCursor.AbsatzAn(doc, 0)!, erster!.Value.Start));

        // Vom hinteren aus geht es per Umlauf auf den vorderen.
        var zweiter = TdSuche.Naechster(doc, "Hund", erster.Value.End);
        Assert.NotNull(zweiter);
        Assert.Equal(0, TdCursor.Linear(TdCursor.AbsatzAn(doc, 0)!, zweiter!.Value.Start));
    }

    /// <summary>
    /// Das einzige Vorkommen wird auch gefunden, wenn der Cursor schon dahinter steht —
    /// **sonst meldete die Suche „nicht gefunden" für ein Wort, das dasteht.**
    /// </summary>
    [Fact]
    public void Das_einzige_Vorkommen_wird_auch_von_hinten_gefunden()
    {
        var doc = Dok(Text("nur einmal Hund hier"));

        var treffer = TdSuche.Naechster(doc, "Hund", TdCursor.Ende(doc));

        Assert.NotNull(treffer);
        Assert.Equal("Hund", Treffertext(doc, treffer!.Value));
    }

    // ==================== Ersetzen ====================

    [Fact]
    public void Alle_ersetzen_trifft_jedes_Vorkommen()
    {
        var doc = Dok(Text("Hund und Hund"), Text("noch ein Hund"));

        var aenderungen = TdSuche.AlleErsetzen(doc, "Hund", "Katze");

        Assert.Equal(3, aenderungen.Count);
        Assert.Equal("Katze und Katze|noch ein Katze", Klartext(doc));
    }

    /// <summary>
    /// <b>Auch dann, wenn der Ersatz die Suche enthält.</b> „Hund" durch „Hundehütte" zu
    /// ersetzen darf keine Endlosschleife ergeben — und genau das täte es, wenn nach jeder
    /// Ersetzung von vorn gesucht würde.
    /// </summary>
    [Fact]
    public void Ein_Ersatz_der_die_Suche_enthaelt_laeuft_nicht_endlos()
    {
        var doc = Dok(Text("Hund und Hund"));

        var aenderungen = TdSuche.AlleErsetzen(doc, "Hund", "Hundehütte");

        Assert.Equal(2, aenderungen.Count);
        Assert.Equal("Hundehütte und Hundehütte", Klartext(doc));
    }

    /// <summary>
    /// <b>Unterschiedlich lange Ersetzungen verschieben alles dahinter</b> — deshalb läuft
    /// <see cref="TdSuche.AlleErsetzen"/> rückwärts. Ein kürzerer und ein längerer Ersatz im
    /// selben Absatz sind der Fall, der eine Vorwärtsschleife auffliegen ließe.
    /// </summary>
    [Theory]
    [InlineData("x", "x und x und x")]
    [InlineData("Elefantenrüssel", "Elefantenrüssel und Elefantenrüssel und Elefantenrüssel")]
    public void Laengenaenderungen_verschieben_die_folgenden_Treffer_nicht(string ersatz, string erwartet)
    {
        var doc = Dok(Text("Hund und Hund und Hund"));

        TdSuche.AlleErsetzen(doc, "Hund", ersatz);

        Assert.Equal(erwartet, Klartext(doc));
    }

    /// <summary>
    /// <b>Der Ersatz erbt das Format der Stelle</b> — wer ein fettes Wort ersetzt, will ein
    /// fettes Wort zurück. Ohne diese Zeile käme der Text in der Grundschrift wieder und der
    /// Absatz sähe hinterher gerupft aus.
    /// </summary>
    [Fact]
    public void Der_Ersatz_erbt_das_Format_der_Stelle()
    {
        var doc = Dok(Abs(new TdRun("mager "), Fett("Hund")));

        TdSuche.AlleErsetzen(doc, "Hund", "Katze");

        var stuecke = TdCursor.Stuecke(TdCursor.AbsatzAn(doc, 0)!);
        var katze = stuecke.OfType<TdRun>().First(r => r.Text.Contains("Katze"));
        Assert.True(katze.Format?.Bold == true, "Der Ersatz hat das Fett der Stelle verloren.");
    }

    [Fact]
    public void Was_nicht_dasteht_wird_nicht_ersetzt()
    {
        var doc = Dok(Text("Der Hund bellt."));

        Assert.Empty(TdSuche.AlleErsetzen(doc, "Katze", "Maus"));
        Assert.Equal("Der Hund bellt.", Klartext(doc));
    }

    /// <summary>
    /// <b>Der Treffer über eine Formatgrenze wird auch ersetzt</b>, nicht nur gefunden — sonst
    /// stünde die Suche vor einem Wort, das sie zeigt und nicht anfassen kann.
    /// </summary>
    [Fact]
    public void Ein_Treffer_ueber_eine_Formatgrenze_wird_auch_ersetzt()
    {
        var doc = Dok(Abs(new TdRun("Ha"), Fett("llo"), new TdRun(" Welt")));

        TdSuche.AlleErsetzen(doc, "Hallo", "Servus");

        Assert.Equal("Servus Welt", Klartext(doc));
    }
}
