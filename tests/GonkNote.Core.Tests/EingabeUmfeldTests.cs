using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdEingabe"/> — die Eingabe-Naht, <b>Schritt 6a des Schreibens</b> (HANDOFF §6,
/// §5 „Noch offen" 10).
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Was hier gerechnet wird, geht an eine
/// <b>fremde</b> Seite: an Avalonias <c>TextInputMethodClient</c>, an GNOMEs
/// Bildschirmtastatur, später an iPadOS. Die rechnet mit den beiden Zahlen weiter, die sie von
/// hier bekommt — „lösche zwei Zeichen vor der Marke", „ersetze den Bereich 3 bis 7" —, und ein
/// Abstand, der um eins danebenliegt, kommt nicht als Ausnahme heraus, sondern als
/// <b>Zeichen an der falschen Stelle im Dokument des Nutzers</b>. Das ist eine Sorte Fehler,
/// die man am laufenden Programm erst bemerkt, wenn schon etwas kaputt ist.
/// </para>
/// <para>
/// <b>Der eine Satz, der alles trägt:</b> In <see cref="TdEingabe.Text"/> ist jeder
/// Cursorschritt genau ein Zeichen breit — <c>Text(absatz).Length == TdCursor.Laenge(absatz)</c>.
/// <see cref="TdCursor.Text"/> gilt das ausdrücklich <b>nicht</b> (dort steht es als Absatz in
/// der Doku), und beide Zählweisen stehen im selben Namensraum nebeneinander. Genau deshalb
/// hält ein Wächter die Gleichung fest, statt sich auf das Lesen zu verlassen.
/// </para>
/// <para>
/// <b>Kein Kopf, kein Umbruch, keine Schrift.</b> Diese Rechnung liest das Modell und sonst
/// nichts; sie läuft auf jedem Rechner gleich — und schlägt an, bevor jemand einen der beiden
/// Köpfe startet.
/// </para>
/// </summary>
public sealed class EingabeUmfeldTests
{
    // ==================== Hilfsmittel ====================

    /// <summary>Ein Dokument aus fertigen Absätzen.</summary>
    private static TdDocument Dok(params TdParagraph[] absaetze)
    {
        var doc = new TdDocument();
        var abschnitt = new TdSection();
        abschnitt.Blocks.AddRange(absaetze);
        doc.Sections.Add(abschnitt);
        return doc;
    }

    private static TdParagraph Abs(params TdInline[] stuecke) => new(stuecke);

    /// <summary>
    /// Ein Absatz mit allem, was ein Stück sein kann: Text, Zeilenumbruch, Feld, Bild,
    /// Diagramm und ein Verweis. <b>Der Fall, an dem die beiden Zählweisen auseinanderlaufen</b>
    /// — vier der sechs Stücke steuern keinen Klartext bei.
    /// </summary>
    private static TdParagraph Allerlei() => Abs(
        new TdRun("ab"),
        new TdLineBreak(),
        new TdField(TdFieldKind.PageNumber),
        new TdImage { BlobId = Guid.NewGuid() },
        new TdChart(),
        TdHyperlink.Text("https://example.org", "cd"));

    // ==================== Ein Schritt, ein Zeichen ====================

    /// <summary>
    /// <b>Die Gleichung, um die es geht.</b> Ist sie verletzt, zeigt jeder Abstand, den eine
    /// Eingabemethode zurückreicht, hinter dem ersten Feld daneben — und zwar leise.
    /// </summary>
    [Fact]
    public void Der_Text_ist_so_lang_wie_der_Absatz_Schritte_breit_ist()
    {
        var absatz = Allerlei();

        Assert.Equal(TdCursor.Laenge(absatz), TdEingabe.Text(absatz).Length);
    }

    /// <summary>
    /// Dieselbe Gleichung für die einfachen Fälle, die jeden Tag vorkommen — ein leerer Absatz
    /// und einer aus schlichtem Text.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("Hallo Welt")]
    public void Ein_schlichter_Absatz_kommt_unveraendert_heraus(string text)
    {
        var absatz = new TdParagraph(text);

        Assert.Equal(text, TdEingabe.Text(absatz));
        Assert.Equal(TdCursor.Laenge(absatz), TdEingabe.Text(absatz).Length);
    }

    /// <summary>
    /// <b>Der Unterschied zu <see cref="TdCursor.Text"/>, ausdrücklich festgehalten.</b> Der
    /// Klartext ist kürzer, und das ist dort richtig: Im Dokument steht ein Feld und keine
    /// Zahl. Wer die beiden je zusammenlegt, muss an dieser Zeile vorbei.
    /// </summary>
    [Fact]
    public void Der_Klartext_ist_kuerzer_und_das_ist_gewollt()
    {
        var doc = Dok(Allerlei());
        var absatz = TdCursor.Absaetze(doc)[0];

        string klartext = TdCursor.Text(doc, TdSelection.Alles(doc));

        Assert.True(klartext.Length < TdEingabe.Text(absatz).Length);
    }

    /// <summary>
    /// Ein Zeilenumbruch wird sein <c>\n</c>, ein Feld, ein Bild und ein Diagramm je <b>ein</b>
    /// <see cref="TdEingabe.Platzhalter"/> — und die Stücke eines Verweises stehen darin, wie
    /// der Cursor sie sieht.
    /// </summary>
    [Fact]
    public void Jedes_unteilbare_Stueck_bekommt_genau_ein_Zeichen()
    {
        string p = TdEingabe.Platzhalter.ToString();

        Assert.Equal("ab\n" + p + p + p + "cd", TdEingabe.Text(Allerlei()));
    }

    /// <summary>
    /// <b>Und der Abstand vom Absatzanfang ist ohne Umrechnung schon der Abstand in dieser
    /// Zeichenkette</b> — das ist der ganze Zweck der Gleichung. Geprüft wird jede Stelle des
    /// Absatzes und nicht eine ausgesuchte.
    /// </summary>
    [Fact]
    public void Der_lineare_Abstand_ist_der_Abstand_im_Text()
    {
        var doc = Dok(Allerlei());
        var absatz = TdCursor.Absaetze(doc)[0];
        string text = TdEingabe.Text(absatz);

        for (int i = 0; i <= TdCursor.Laenge(absatz); i++)
        {
            var stelle = TdCursor.AusLinear(absatz, 0, i);
            var umfeld = TdEingabe.Umfeld(doc, new TdSelection(stelle));

            Assert.Equal(i, umfeld.Start);
            Assert.Equal(i, umfeld.Ende);
            Assert.Equal(text, umfeld.Text);
        }
    }

    // ==================== Das Umfeld ====================

    /// <summary>
    /// Der Absatz der <b>Marke</b> und nicht der erste des Dokuments — an ihr klappt die
    /// Tastatur auf. Wer hier <c>Absaetze[0]</c> nähme, zeigte einer Bildschirmtastatur beim
    /// Schreiben im dritten Absatz den Text des ersten.
    /// </summary>
    [Fact]
    public void Das_Umfeld_ist_der_Absatz_der_Marke()
    {
        var doc = Dok(new TdParagraph("eins"), new TdParagraph("zwei"), new TdParagraph("drei"));

        var umfeld = TdEingabe.Umfeld(doc, new TdSelection(new TdPosition(2, 0, 4)));

        Assert.Equal(2, umfeld.Absatz);
        Assert.Equal("drei", umfeld.Text);
        Assert.Equal(4, umfeld.Start);
        Assert.Equal(4, umfeld.Ende);
    }

    /// <summary>Eine Auswahl innerhalb eines Absatzes kommt als ihre beiden Abstände heraus.</summary>
    [Fact]
    public void Eine_Auswahl_wird_zu_zwei_Abstaenden()
    {
        var doc = Dok(new TdParagraph("Hallo Welt"));

        var umfeld = TdEingabe.Umfeld(
            doc, new TdSelection(new TdPosition(0, 0, 6), new TdPosition(0, 0, 10)));

        Assert.Equal(6, umfeld.Start);
        Assert.Equal(10, umfeld.Ende);
    }

    /// <summary>
    /// <b>Rückwärts gezogen kommt sie trotzdem sortiert heraus.</b> <see cref="TdSelection"/>
    /// hält Anker und Spitze auseinander, weil das Ziehen es verlangt; eine Eingabemethode
    /// kennt diesen Unterschied nicht und schneidet mit den beiden Zahlen in die Zeichenkette.
    /// Unsortiert wäre die erste Rückwärtsauswahl eine Ausnahme in fremdem Code.
    /// </summary>
    [Fact]
    public void Eine_rueckwaerts_gezogene_Auswahl_kommt_sortiert_heraus()
    {
        var doc = Dok(new TdParagraph("Hallo Welt"));

        var umfeld = TdEingabe.Umfeld(
            doc, new TdSelection(new TdPosition(0, 0, 10), new TdPosition(0, 0, 6)));

        Assert.Equal(6, umfeld.Start);
        Assert.Equal(10, umfeld.Ende);
    }

    /// <summary>
    /// Eine Auswahl über mehrere Absätze wird auf den der Marke beschnitten — liegt der Anker
    /// <b>davor</b>, beginnt sie bei 0. Ausgewählt bleibt trotzdem alles; die Eingabemethode
    /// erfährt nur den Teil, über den sie reden kann.
    /// </summary>
    [Fact]
    public void Ein_Anker_im_Absatz_davor_beginnt_bei_Null()
    {
        var doc = Dok(new TdParagraph("eins"), new TdParagraph("zwei"));

        var umfeld = TdEingabe.Umfeld(
            doc, new TdSelection(new TdPosition(0, 0, 2), new TdPosition(1, 0, 3)));

        Assert.Equal(1, umfeld.Absatz);
        Assert.Equal("zwei", umfeld.Text);
        Assert.Equal(0, umfeld.Start);
        Assert.Equal(3, umfeld.Ende);
    }

    /// <summary>Und liegt er <b>dahinter</b>, endet sie am Absatzende.</summary>
    [Fact]
    public void Ein_Anker_im_Absatz_danach_endet_am_Absatzende()
    {
        var doc = Dok(new TdParagraph("eins"), new TdParagraph("zwei"));

        var umfeld = TdEingabe.Umfeld(
            doc, new TdSelection(new TdPosition(1, 0, 3), new TdPosition(0, 0, 2)));

        Assert.Equal(0, umfeld.Absatz);
        Assert.Equal("eins", umfeld.Text);
        Assert.Equal(2, umfeld.Start);
        Assert.Equal(4, umfeld.Ende);
    }

    /// <summary>
    /// <b>Eine Stelle, die es nicht gibt, wird geradegezogen und nicht geglaubt.</b> Sie kommt
    /// aus einem Klick, aus einer Auswahl nach einer Änderung — und die Eingabemethode fragt
    /// auch dann, wenn gerade nichts stimmt.
    /// </summary>
    [Fact]
    public void Eine_Stelle_jenseits_des_Dokuments_wird_geradegezogen()
    {
        var doc = Dok(new TdParagraph("kurz"));

        var umfeld = TdEingabe.Umfeld(doc, new TdSelection(new TdPosition(9, 9, 99)));

        Assert.Equal(0, umfeld.Absatz);
        Assert.Equal("kurz", umfeld.Text);
        Assert.Equal(4, umfeld.Start);
        Assert.Equal(4, umfeld.Ende);
    }

    /// <summary>
    /// Ein Dokument ohne Absätze gibt es im Modell nicht — der Kopf fragt aber auch, während
    /// noch nichts geladen ist. <b>Dann ist die leere Auskunft die richtige und keine
    /// Ausnahme:</b> Ein Wurf an dieser Stelle käme aus Avalonias Eingabepfad heraus, also aus
    /// einem Faden, den niemand fängt.
    /// </summary>
    [Fact]
    public void Ein_Dokument_ohne_Absaetze_liefert_eine_leere_Auskunft()
    {
        var umfeld = TdEingabe.Umfeld(new TdDocument(), new TdSelection(TdPosition.Null));

        Assert.Equal(0, umfeld.Absatz);
        Assert.Equal("", umfeld.Text);
        Assert.Equal(0, umfeld.Start);
        Assert.Equal(0, umfeld.Ende);
    }

    // ==================== Der Rückweg ====================

    /// <summary>
    /// <b>Hin und zurück muss dasselbe herauskommen</b> — sonst wanderte die Auswahl bei jeder
    /// Wortvervollständigung ein Stück. Geprüft über <see cref="Allerlei"/>, also über einen
    /// Absatz mit Feldern und Bildern darin.
    /// </summary>
    [Fact]
    public void Der_Rueckweg_trifft_dieselben_Abstaende()
    {
        var doc = Dok(Allerlei());
        int laenge = TdCursor.Laenge(TdCursor.Absaetze(doc)[0]);

        for (int a = 0; a <= laenge; a++)
        {
            for (int b = a; b <= laenge; b++)
            {
                var umfeld = TdEingabe.Umfeld(doc, TdEingabe.Auswahl(doc, 0, a, b));

                Assert.Equal(a, umfeld.Start);
                Assert.Equal(b, umfeld.Ende);
            }
        }
    }

    /// <summary>
    /// Der Anker landet auf <c>start</c>, die Marke auf <c>ende</c> — so vergrößert ein
    /// anschließendes Umschalt+Rechts die Auswahl, statt sie umzuklappen.
    /// </summary>
    [Fact]
    public void Die_Marke_landet_auf_dem_Ende()
    {
        var doc = Dok(new TdParagraph("Hallo Welt"));

        var auswahl = TdEingabe.Auswahl(doc, 0, 2, 7);

        Assert.Equal(new TdPosition(0, 0, 2), auswahl.Anchor);
        Assert.Equal(new TdPosition(0, 0, 7), auswahl.Focus);
    }

    /// <summary>
    /// <b>Nichts, was von außen kommt, wird geglaubt.</b> Eine Eingabemethode, die über das
    /// Absatzende hinausgreift oder auf einen Absatz zeigt, den es nicht gibt, bekommt eine
    /// geklemmte Antwort — und keinen Wurf aus Avalonias Eingabepfad.
    /// </summary>
    [Fact]
    public void Abstaende_und_Absatz_werden_geklemmt()
    {
        var doc = Dok(new TdParagraph("kurz"));

        var auswahl = TdEingabe.Auswahl(doc, 42, -5, 999);

        Assert.Equal(new TdPosition(0, 0, 0), auswahl.Anchor);
        Assert.Equal(new TdPosition(0, 0, 4), auswahl.Focus);
    }
}
