using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdChartEingabe"/> — der Weg zwischen den Textfeldern eines Dialogs und einem
/// <see cref="TdChart"/>, in beide Richtungen (HANDOFF §4.82).
///
/// <para>
/// <b>Diese Wächter stehen vor der Oberfläche</b> (§5e). In §4.80 sind zwei von ihnen gefallen
/// und beide hatten recht; hier ist es der <b>Trenner</b>, an dem sich zeigt, dass der WPF-Kopf
/// eine deutsche Dezimalzahl seit jeher still in zwei Werte zerlegt.
/// </para>
/// </summary>
public sealed class DiagrammEingabeTests
{
    // ==================== Zahlen lesen ====================

    /// <summary>Der Normalfall: Kommas trennen, wie es die Oberfläche vorschlägt („4, 7, 3").</summary>
    [Fact]
    public void Das_Komma_trennt_die_Werte()
    {
        Assert.Equal([4, 7, 3], TdChartEingabe.Zahlen("4, 7, 3"));
        Assert.Equal([4, 7, 3], TdChartEingabe.Zahlen("4,7,3"));
    }

    /// <summary>Ohne Semikolon ist der Punkt das Dezimaltrennzeichen.</summary>
    [Fact]
    public void Ohne_Semikolon_ist_der_Punkt_das_Dezimaltrennzeichen()
    {
        Assert.Equal([3.5, 7.25], TdChartEingabe.Zahlen("3.5, 7.25"));
    }

    /// <summary>
    /// <b>Der Fund, um den es geht.</b> Steht ein Semikolon in der Zeile, trennt nur dieses —
    /// und das Komma ist ein <b>Dezimalkomma</b>. Der WPF-Kopf macht daraus vier Werte
    /// (3, 5, 7, 25), ohne etwas zu sagen: sein <c>ParseRow</c> trennt erst an <c>,</c> und
    /// ersetzt <b>danach</b> Komma durch Punkt, wo keines mehr stehen kann.
    /// </summary>
    [Fact]
    public void Mit_Semikolon_ist_das_Komma_ein_Dezimalkomma()
    {
        Assert.Equal([3.5, 7.25], TdChartEingabe.Zahlen("3,5; 7,25"));
    }

    /// <summary>Der Tabulator trennt genauso — so kommt eine Zeile aus einer Tabelle an.</summary>
    [Fact]
    public void Der_Tabulator_trennt_wie_das_Semikolon()
    {
        Assert.Equal([3.5, 7.25], TdChartEingabe.Zahlen("3,5\t7,25"));
    }

    /// <summary>
    /// <b>Was keine Zahl ist, fällt heraus statt alles mitzunehmen.</b> Ein Tippfehler in einer
    /// Zelle soll nicht die ganze Reihe kosten — und ein Diagramm, das gar nicht erscheint,
    /// erklärt sich schlechter als eines mit einem Wert weniger.
    /// </summary>
    [Fact]
    public void Was_keine_Zahl_ist_faellt_heraus()
    {
        Assert.Equal([4, 3], TdChartEingabe.Zahlen("4, x, 3"));
    }

    /// <summary>Leerzeichen um die Werte gehören nicht zur Zahl.</summary>
    [Fact]
    public void Leerraum_um_die_Werte_stoert_nicht()
    {
        Assert.Equal([4, 7], TdChartEingabe.Zahlen("   4  ,   7   "));
    }

    /// <summary>Negative Werte und die Null sind Zahlen wie alle anderen.</summary>
    [Fact]
    public void Negative_Werte_und_die_Null_kommen_durch()
    {
        Assert.Equal([-2.5, 0, 4], TdChartEingabe.Zahlen("-2.5, 0, 4"));
    }

    /// <summary>Nichts eingegeben heißt keine Zahlen — und keine Ausnahme.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Ohne_Eingabe_kommen_keine_Zahlen(string? eingabe) =>
        Assert.Empty(TdChartEingabe.Zahlen(eingabe));

    // ==================== Reihen ====================

    /// <summary>Je nicht-leere Zeile eine Reihe — leere Zeilen zählen nicht mit.</summary>
    [Fact]
    public void Jede_nicht_leere_Zeile_ist_eine_Reihe()
    {
        var reihen = TdChartEingabe.Reihen("4, 7, 3\n\n1, 2, 3\n   \n");

        Assert.Equal(2, reihen.Count);
        Assert.Equal([4, 7, 3], reihen[0].Values);
        Assert.Equal([1, 2, 3], reihen[1].Values);
    }

    /// <summary>Die Namen werden der Reihe nach vergeben.</summary>
    [Fact]
    public void Die_Namen_gehen_der_Reihe_nach_an_die_Reihen()
    {
        var reihen = TdChartEingabe.Reihen("1\n2", "Soll, Ist");

        Assert.Equal("Soll", reihen[0].Name);
        Assert.Equal("Ist", reihen[1].Name);
    }

    /// <summary>
    /// <b>Ein fehlender Name bleibt leer und wird nicht zu „Reihe 2".</b> Was niemand
    /// eingegeben hat, gehört nicht ins Dokument — sonst stünde in der Datei ein deutsches
    /// Wort, das davon abhängt, in welcher Sprache die App beim Einfügen lief. Die Legende
    /// darf trotzdem „Reihe 2" anzeigen; das ist Anzeige und keine Sicherung.
    /// </summary>
    [Fact]
    public void Ein_fehlender_Reihenname_bleibt_leer()
    {
        var reihen = TdChartEingabe.Reihen("1\n2", "Soll");

        Assert.Equal("Soll", reihen[0].Name);
        Assert.Equal("", reihen[1].Name);
    }

    /// <summary>Mehr Namen als Reihen ist kein Fehler — die überzähligen fallen weg.</summary>
    [Fact]
    public void Ueberzaehlige_Namen_fallen_weg()
    {
        var reihen = TdChartEingabe.Reihen("1", "Soll, Ist, Rest");

        Assert.Single(reihen);
        Assert.Equal("Soll", reihen[0].Name);
    }

    // ==================== Das ganze Diagramm ====================

    /// <summary>
    /// <b>Ohne eine einzige Reihe kommt kein Diagramm heraus.</b> Ein leerer Kasten im
    /// Dokument wäre von einem Fehler nicht zu unterscheiden — dieselbe Regel wie bei
    /// „leer ist nicht kaputt" (§7).
    /// </summary>
    [Fact]
    public void Ohne_Werte_entsteht_kein_Diagramm() =>
        Assert.Null(TdChartEingabe.Lesen(
            TdChartKind.Column, "Titel", "A, B", "", "", breiteCm: 12, hoeheCm: 8));

    /// <summary>Titel, Kategorien, Reihen und Maße kommen vollständig an.</summary>
    [Fact]
    public void Aus_den_Feldern_wird_ein_vollstaendiges_Diagramm()
    {
        var d = TdChartEingabe.Lesen(
            TdChartKind.Bar, "  Umsatz  ", "Jan, Feb", "Soll, Ist", "4, 7\n3, 5",
            breiteCm: 12, hoeheCm: 8);

        Assert.NotNull(d);
        Assert.Equal(TdChartKind.Bar, d.Kind);
        Assert.Equal("Umsatz", d.Title);
        Assert.Equal(["Jan", "Feb"], d.Categories);
        Assert.Equal(2, d.Series.Count);
        Assert.Equal([4, 7], d.Series[0].Values);
        Assert.Equal(12, d.WidthCm, 6);
        Assert.Equal(8, d.HeightCm, 6);
    }

    /// <summary>
    /// <b>Fehlende Kategorien werden nicht aufgefüllt.</b> Gespeichert wird nur, was jemand
    /// eingegeben hat; die laufende Nummer setzt <see cref="TdChart.Kategorie"/> beim
    /// Zeichnen ein.
    /// </summary>
    [Fact]
    public void Fehlende_Kategorien_werden_nicht_aufgefuellt()
    {
        var d = TdChartEingabe.Lesen(
            TdChartKind.Column, "", "Jan", "", "4, 7, 3", breiteCm: 12, hoeheCm: 8)!;

        Assert.Equal(["Jan"], d.Categories);
        Assert.Equal("Jan", d.Kategorie(0));
        Assert.Equal("2", d.Kategorie(1));   // aufgefüllt, aber nur zum Zeichnen
    }

    /// <summary>
    /// <b>Die Standardpalette wird nicht mitgeschrieben.</b> <see cref="TdChart.Farbe"/> fällt
    /// von selbst auf sie zurück; eine Kopie in jedem Diagramm hieße, dass eine spätere
    /// Änderung der Vorgabe an allen Bestandsdiagrammen vorbeiginge.
    /// </summary>
    [Fact]
    public void Die_Standardpalette_wird_nicht_mitgeschrieben()
    {
        var d = TdChartEingabe.Lesen(
            TdChartKind.Column, "", "", "", "1, 2", breiteCm: 12, hoeheCm: 8,
            palette: TdChart.StandardPalette)!;

        Assert.Empty(d.Palette);
        Assert.Equal(TdChart.StandardPalette[0], d.Farbe(0));
    }

    /// <summary>Eine abgewandelte Palette steht dagegen am Diagramm — sonst wäre sie weg.</summary>
    [Fact]
    public void Eine_eigene_Palette_steht_am_Diagramm()
    {
        var d = TdChartEingabe.Lesen(
            TdChartKind.Column, "", "", "", "1, 2", breiteCm: 12, hoeheCm: 8,
            palette: ["#112233", "#445566"])!;

        Assert.Equal(["#112233", "#445566"], d.Palette);
        Assert.Equal("#112233", d.Farbe(0));
    }

    // ==================== Der Rückweg ====================

    /// <summary>
    /// <b>Der Grund, warum es diesen Weg gibt</b> (§4.21): Ein Diagramm, das schon im Dokument
    /// steht, geht wieder in die Felder zurück — <b>der Dialog kann es ändern statt es nur neu
    /// anzulegen.</b> Der WPF-Kopf konnte das nie, weil dort eine Bitmap im Text lag.
    /// </summary>
    [Fact]
    public void Ein_Diagramm_geht_vollstaendig_in_die_Felder_zurueck()
    {
        var d = TdChartEingabe.Lesen(
            TdChartKind.Line, "Umsatz", "Jan, Feb", "Soll, Ist", "4, 7\n3.5, 5",
            breiteCm: 12, hoeheCm: 8)!;

        Assert.Equal("Jan, Feb", TdChartEingabe.KategorienText(d));
        Assert.Equal("Soll, Ist", TdChartEingabe.NamenText(d));
        Assert.Equal("4, 7\n3.5, 5", TdChartEingabe.WerteText(d));
    }

    /// <summary>
    /// Was zurückgeht, muss wieder hineingehen: Hin und Zurück ändert die Zahlen nicht. Ohne
    /// diesen Wächter wäre jedes Öffnen-und-Speichern ein stiller Rundungsfehler.
    /// </summary>
    [Fact]
    public void Hin_und_zurueck_aendert_die_Zahlen_nicht()
    {
        var d = TdChartEingabe.Lesen(
            TdChartKind.Column, "T", "A, B", "R1", "4, 7.25\n-3, 0",
            breiteCm: 12, hoeheCm: 8)!;

        var zurueck = TdChartEingabe.Reihen(TdChartEingabe.WerteText(d), TdChartEingabe.NamenText(d));

        Assert.Equal(d.Series.Count, zurueck.Count);
        Assert.Equal([4, 7.25], zurueck[0].Values);
        Assert.Equal([-3, 0], zurueck[1].Values);
        Assert.Equal("R1", zurueck[0].Name);
    }

    /// <summary>
    /// <b>Deutsch eingegeben, deutsch gelesen.</b> Die Ausgabe schreibt mit Punkt (Core fragt
    /// das Gebietsschema so wenig wie die Uhr, §4.20) — <b>gelesen wird beides</b>, die
    /// Eingabe geht also nicht verloren.
    /// </summary>
    [Fact]
    public void Deutsch_eingegebene_Werte_ueberstehen_den_Rundlauf()
    {
        var d = TdChartEingabe.Lesen(
            TdChartKind.Column, "", "", "", "3,5; 7,25", breiteCm: 12, hoeheCm: 8)!;

        Assert.Equal([3.5, 7.25], d.Series[0].Values);
        Assert.Equal("3.5, 7.25", TdChartEingabe.WerteText(d));
        Assert.Equal([3.5, 7.25], TdChartEingabe.Zahlen(TdChartEingabe.WerteText(d)));
    }

    /// <summary>
    /// <b>Ein Feld voller Kommas ist keine Eingabe.</b> Trüge keine Reihe einen Namen und
    /// schriebe der Rückweg trotzdem „, , ", sähe das im Dialog aus wie etwas, das jemand
    /// getippt hat — und beim nächsten Speichern stünden drei leere Namen im Dokument.
    /// </summary>
    [Fact]
    public void Ohne_einen_einzigen_Namen_bleibt_das_Namensfeld_leer()
    {
        var d = TdChartEingabe.Lesen(
            TdChartKind.Column, "", "", "", "1\n2\n3", breiteCm: 12, hoeheCm: 8)!;

        Assert.Equal("", TdChartEingabe.NamenText(d));
    }
}
