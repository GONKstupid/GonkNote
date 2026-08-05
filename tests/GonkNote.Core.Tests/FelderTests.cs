using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdField"/>, <see cref="TdHyperlink"/> und <see cref="TdToc"/> — Phase 4,
/// Schritt 5.
///
/// <para>
/// <b>Der Kern dieses Schritts ist eine Rechnung und keine Speicherung.</b> Eine Seitenzahl,
/// eine Seitenanzahl und ein Inhaltsverzeichnis hängen nicht von der Stelle ab, an der sie
/// stehen, sondern von allem darum herum — dasselbe Muster wie bei der Listennummer (§4.17).
/// Was hier schiefgehen kann, sieht deshalb nie nach einem Fehler aus: eine Seitenzahl, die um
/// eins danebenliegt, ist immer noch eine Seitenzahl.
/// </para>
/// <para>
/// Gemessen wird wie in <see cref="UmbruchTests"/> mit **festen Maßen** und nicht mit echten
/// Schriften: jedes Zeichen 1 cm breit, jede Zeile 1 cm hoch (§4.16).
/// </para>
/// </summary>
public sealed class FelderTests
{
    /// <inheritdoc cref="UmbruchTests"/>
    private sealed class FesteMessung : ITdTextMeasure
    {
        public double WidthCm(string text, TdCharFormat format) => text.Length;

        public TdFontMetrics Metrics(TdCharFormat format) => new(0.8, 0.2, 1.0);
    }

    /// <summary>Ein Blatt, auf das genau zehn Zeichen und zehn Zeilen passen.</summary>
    private static TdPageSetup Blatt(double breite = 10, double hoehe = 10) => new()
    {
        WidthCm = breite + 2,
        HeightCm = hoehe + 2,
        MarginLeftCm = 1,
        MarginRightCm = 1,
        MarginTopCm = 1,
        MarginBottomCm = 1,
    };

    /// <summary>
    /// Ein Dokument **ohne Absatzabstände** — der Standard hat 8 pt nach jedem Absatz, und
    /// damit ginge jede Rechnung „zehn Zeilen à 1 cm" daneben (§4.16).
    /// </summary>
    private static TdDocument Dok(params TdBlock[] bloecke) => new()
    {
        DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 0 },
        Sections = { new TdSection(bloecke) { Page = Blatt() } },
    };

    /// <summary>Fester Zeitpunkt statt <c>DateTime.Now</c> — sonst hinge der Wächter an der Uhr.</summary>
    private static readonly TdFieldContext Kontext = new()
    {
        Date = new DateTime(2026, 8, 5, 9, 30, 0, DateTimeKind.Utc),
        Title = "Mein Dokument",
    };

    private static TdLayoutResult Umbrechen(TdDocument doc, TdFieldContext? kontext = null) =>
        TdLayout.Umbrechen(doc, new FesteMessung(), kontext ?? Kontext);

    private static TdParagraph Ueberschrift(string text, int ebene) =>
        new(text) { Format = { OutlineLevel = ebene } };

    // ==================== Das Modell ====================

    /// <summary>
    /// <b>Ein Feld hat keinen gespeicherten Text.</b> Im Dokument steht ein Feld und keine
    /// Zahl — wer den zuletzt gerechneten Wert mitspeicherte, hätte nach der ersten Änderung
    /// eine Seitenzahl in der Datei, die niemand mehr nachrechnet.
    /// </summary>
    [Fact]
    public void Ein_Feld_traegt_keinen_Text_mit_sich()
    {
        var feld = new TdField(TdFieldKind.PageNumber);

        Assert.Equal("", feld.PlainText());
        Assert.Equal("", new TdParagraph([feld]).PlainText());
    }

    /// <summary>
    /// Der Text eines Verweises ist Text — er zählt im Wortzähler mit und steht im Klartext.
    /// Ein Verweis ist eine Klammer um Stücke und nicht selbst eines.
    /// </summary>
    [Fact]
    public void Ein_Verweis_gibt_seinen_Text_heraus()
    {
        var doc = Dok(new TdParagraph([
            new TdRun("Siehe "),
            new TdHyperlink("kapitel-2.md", new TdRun("Kapitel "), new TdRun("zwei")),
        ]));

        Assert.Equal("Siehe Kapitel zwei", doc.PlainText());
        Assert.Equal(3, doc.WordCount());
    }

    /// <summary>
    /// <b>Das Ziel bleibt wörtlich stehen</b> — es ist eine Zeichenkette und kein <c>Uri</c>.
    /// Ein <c>Uri</c> vereinheitlicht, und aus <c>kapitel-2.md</c> würde beim nächsten
    /// Schreiben ein absoluter <c>file:///</c>-Pfad (§7, „Markdown-Export").
    /// </summary>
    [Theory]
    [InlineData("kapitel-2.md")]
    [InlineData("../oben/datei.md")]
    [InlineData("https://example.org/pfad?a=1&b=2")]
    [InlineData("#ueberschrift")]
    public void Ein_Verweisziel_bleibt_woertlich_stehen(string ziel)
    {
        Assert.Equal(ziel, TdHyperlink.Text(ziel, "Text").Target);
    }

    /// <summary>Ein Ziel mit <c>#</c> zeigt ins eigene Dokument und ist kein Dateiverweis.</summary>
    [Fact]
    public void Eine_Textmarke_ist_kein_Dateiverweis()
    {
        Assert.True(TdHyperlink.Text("#kapitel-1", "x").IstTextmarke);
        Assert.False(TdHyperlink.Text("kapitel-1.md", "x").IstTextmarke);
    }

    /// <summary>
    /// Der flache Durchlauf liefert die Stücke **innerhalb** eines Verweises, nicht den
    /// Verweis selbst. Wer über <c>Inlines</c> liefe, sähe die Klammer und nicht ihren Inhalt
    /// — und die Zeile bliebe leer, ohne dass jemand einen Fehler bekäme.
    /// </summary>
    [Fact]
    public void Der_flache_Durchlauf_steigt_in_den_Verweis_hinein()
    {
        var absatz = new TdParagraph([
            new TdRun("a"),
            new TdHyperlink("ziel", new TdRun("b"), new TdRun("c")),
            new TdRun("d"),
        ]);

        var stuecke = absatz.FlacheStuecke().ToList();

        Assert.Equal(4, stuecke.Count);
        Assert.Equal(["a", "b", "c", "d"], stuecke.Select(s => s.Stueck.PlainText()));
        Assert.Null(stuecke[0].Verweis);
        Assert.Equal("ziel", stuecke[1].Verweis!.Target);
        Assert.Equal("ziel", stuecke[2].Verweis!.Target);
        Assert.Null(stuecke[3].Verweis);
    }

    /// <summary>
    /// Die Platzhalter der Kopf- und Fußzeile sind Datenformat: sie stehen so in
    /// Bestandsdokumenten. Der Weg hin und zurück muss sich decken, sonst wird aus einem
    /// Datumsfeld beim nächsten Speichern der Text „{DATUM}".
    /// </summary>
    [Theory]
    [InlineData("{SEITE}", TdFieldKind.PageNumber)]
    [InlineData("{SEITEN}", TdFieldKind.PageCount)]
    [InlineData("{DATUM}", TdFieldKind.Date)]
    [InlineData("{TITEL}", TdFieldKind.Title)]
    public void Platzhalter_und_Feldart_gehoeren_umkehrbar_zusammen(string platzhalter, TdFieldKind art)
    {
        Assert.Equal(art, TdField.ArtVonPlatzhalter(platzhalter));
        Assert.Equal(platzhalter, TdField.PlatzhalterVonArt(art));
    }

    /// <summary>
    /// Das Inhaltsverzeichnis hat keinen Platzhalter — es passt nicht in eine Kopfzeile, und
    /// ein erfundener Platzhalter dafür wäre ein Versprechen ohne Gegenstück.
    /// </summary>
    [Fact]
    public void Das_Inhaltsverzeichnis_hat_keinen_Platzhalter()
    {
        Assert.Null(TdField.PlatzhalterVonArt(TdFieldKind.TableOfContents));
        Assert.Null(TdField.ArtVonPlatzhalter("{INHALT}"));
    }

    // ==================== Die Feldwerte ====================

    /// <summary>
    /// <b>Das Datum kommt von außen und nicht aus der Uhr.</b> Core fragt sie nicht — sonst
    /// hinge jeder Wächter davon ab, wann er läuft (§7).
    /// </summary>
    [Fact]
    public void Das_Datum_kommt_von_aussen()
    {
        var feld = new TdField(TdFieldKind.Date);

        Assert.Equal("05.08.2026", TdFieldValues.Text(feld, Kontext, 1, 1));
        Assert.Equal("", TdFieldValues.Text(feld, TdFieldContext.Ohne, 1, 1));
    }

    /// <summary>
    /// Das Muster steht am Feld und wird nicht aus der Kultur des Rechners geholt: dasselbe
    /// Dokument muss auf jedem System dasselbe Datum zeigen.
    /// </summary>
    [Fact]
    public void Das_Datumsmuster_steht_am_Feld()
    {
        Assert.Equal("2026-08-05",
            TdFieldValues.Text(new TdField(TdFieldKind.Date, "yyyy-MM-dd"), Kontext, 1, 1));
    }

    /// <summary>
    /// Solange die Seitenanzahl nicht feststeht, steht dort **nichts** und keine erfundene
    /// Zahl. Im ersten Durchgang des Umbruchs ist das der Normalfall.
    /// </summary>
    [Fact]
    public void Eine_unbekannte_Seitenanzahl_bleibt_leer()
    {
        var feld = new TdField(TdFieldKind.PageCount);

        Assert.Equal("", TdFieldValues.Text(feld, Kontext, 1, null));
        Assert.Equal("7", TdFieldValues.Text(feld, Kontext, 1, 7));
    }

    // ==================== Felder im Umbruch ====================

    /// <summary>Ein Feld wird als sein Wert gesetzt — und der Lauf weiß, dass er ein Feld ist.</summary>
    [Fact]
    public void Ein_Feld_wird_als_sein_Wert_gesetzt()
    {
        var doc = Dok(new TdParagraph([new TdField(TdFieldKind.Title)]));

        var zeile = Umbrechen(doc).Pages[0].Lines[0];

        var lauf = Assert.Single(zeile.Runs);
        Assert.Equal("Mein Dokument", lauf.Text);
        Assert.NotNull(lauf.Field);
        Assert.Equal(TdFieldKind.Title, lauf.Field!.Kind);
    }

    /// <summary>
    /// <b>Ein Feld bricht nicht in sich um.</b> Sein Wert ist ein Stück und kein Text — eine
    /// Seitenzahl, deren zweite Ziffer in der nächsten Zeile steht, wäre keine mehr. Der Titel
    /// hier ist breiter als das Blatt und bleibt trotzdem zusammen.
    /// </summary>
    [Fact]
    public void Ein_Feld_bricht_nicht_in_sich_um()
    {
        var doc = Dok(new TdParagraph([new TdField(TdFieldKind.Title)]));

        var zeilen = Umbrechen(doc, new TdFieldContext { Title = "Ein sehr langer Titel" }).Pages[0].Lines;

        var lauf = Assert.Single(Assert.Single(zeilen).Runs);
        Assert.Equal("Ein sehr langer Titel", lauf.Text);
    }

    /// <summary>Ein Feld ohne Wert erzeugt kein leeres Stück, das der Cursor betreten könnte.</summary>
    [Fact]
    public void Ein_Feld_ohne_Wert_erzeugt_kein_Stueck()
    {
        var doc = Dok(new TdParagraph([new TdField(TdFieldKind.Date)]));

        var zeile = Umbrechen(doc, TdFieldContext.Ohne).Pages[0].Lines[0];

        Assert.Empty(zeile.Runs);
    }

    /// <summary>Die Seitenzahl ist die der Seite, auf der das Feld steht.</summary>
    [Fact]
    public void Die_Seitenzahl_ist_die_der_eigenen_Seite()
    {
        var doc = Dok(
            new TdParagraph([new TdField(TdFieldKind.PageNumber)]),
            new TdPageBreak(),
            new TdParagraph([new TdField(TdFieldKind.PageNumber)]));

        var ergebnis = Umbrechen(doc);

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal("1", ergebnis.Pages[0].Lines[0].Runs[0].Text);
        Assert.Equal("2", ergebnis.Pages[1].Lines[0].Runs[0].Text);
    }

    /// <summary>
    /// <b>Der Fall, für den die Seitenzahl nachträglich gesetzt wird.</b> Eine Zeile wird
    /// umbrochen, bevor feststeht, wohin sie kommt: eine zusammengehaltene Gruppe kann noch
    /// auf die nächste Seite rutschen. Ein Feld, das beim Umbrechen die damals aktuelle Nummer
    /// bekäme, stünde dann um eins daneben — und eine Seitenzahl, die um eins danebenliegt,
    /// sieht aus wie eine Seitenzahl.
    /// </summary>
    [Fact]
    public void Eine_verschobene_Gruppe_nimmt_die_richtige_Seitenzahl_mit()
    {
        var bloecke = new List<TdBlock>();
        for (int i = 0; i < 9; i++) bloecke.Add(new TdParagraph("x"));

        // Diese beiden gehören zusammen und passen als Paar nicht mehr auf Seite 1.
        bloecke.Add(new TdParagraph([new TdField(TdFieldKind.PageNumber)]) { Format = { KeepWithNext = true } });
        bloecke.Add(new TdParagraph("y"));

        var ergebnis = Umbrechen(Dok([.. bloecke]));

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal(9, ergebnis.Pages[0].Lines.Count);
        Assert.Equal("2", ergebnis.Pages[1].Lines[0].Runs[0].Text);
    }

    /// <summary>
    /// <b>Die Seitenanzahl braucht einen zweiten Durchgang</b>, denn sie steht erst fest, wenn
    /// alles gesetzt ist. Im ersten bleibt sie leer, im zweiten stimmt sie.
    /// </summary>
    [Fact]
    public void Die_Seitenanzahl_steht_nach_dem_zweiten_Durchgang()
    {
        var doc = Dok(
            new TdParagraph([new TdField(TdFieldKind.PageCount)]),
            new TdPageBreak(),
            new TdParagraph("zwei"),
            new TdPageBreak(),
            new TdParagraph("drei"));

        var ergebnis = Umbrechen(doc);

        Assert.Equal(3, ergebnis.PageCount);
        Assert.Equal("3", ergebnis.Pages[0].Lines[0].Runs[0].Text);
    }

    /// <summary>Der gesetzte Lauf eines Verweises trägt sein Ziel — der Zeichner sieht nur ihn.</summary>
    [Fact]
    public void Ein_gesetzter_Verweis_traegt_sein_Ziel()
    {
        var doc = Dok(new TdParagraph([
            new TdRun("a"),
            TdHyperlink.Text("kapitel-2.md", "hier"),
        ]));

        var zeile = Umbrechen(doc).Pages[0].Lines[0];

        Assert.Null(zeile.Runs[0].Link);
        Assert.Equal("hier", zeile.Runs[1].Text);
        Assert.Equal("kapitel-2.md", zeile.Runs[1].Link!.Target);
    }

    // ==================== Das Inhaltsverzeichnis ====================

    /// <summary>Die Ebenenangabe eines Verzeichnisses, und was bei Unsinn daraus wird.</summary>
    [Theory]
    [InlineData("1-3", 1, 3)]
    [InlineData("2-4", 2, 4)]
    [InlineData("1-99", 1, 9)]     // mehr als neun Ebenen gibt es nicht
    [InlineData(null, 1, 3)]
    [InlineData("", 1, 3)]
    [InlineData("Unsinn", 1, 3)]
    [InlineData("3-1", 1, 3)]      // verdreht: die Vorgabe ist besser als kein Verzeichnis
    public void Die_Ebenenangabe_wird_gelesen_und_notfalls_ersetzt(string? angabe, int von, int bis)
    {
        Assert.Equal((von, bis), TdToc.Ebenen(angabe));
    }

    /// <summary>
    /// <b>Das Verzeichnis liest die Gliederungsebene und nicht die Schriftgröße.</b> Das ist
    /// der Grund für den ganzen Schritt: die Ebene steht seit Schritt 1 als eigener Wert da,
    /// während der heutige Markdown-Exporter sie aus der Größe zurückrechnet — wer eine
    /// Überschrift kleiner stellt, verliert dort ihre Ebene.
    /// </summary>
    [Fact]
    public void Das_Verzeichnis_nimmt_die_Gliederungsebene_und_nicht_die_Groesse()
    {
        var doc = Dok(
            // Groß, aber keine Überschrift — das darf nicht ins Verzeichnis.
            new TdParagraph("Riesig") { CharFormat = { FontSize = 40 } },
            Ueberschrift("Klein", 1));

        var eintraege = TdToc.Eintraege(doc, umbruch: null);

        var eintrag = Assert.Single(eintraege);
        Assert.Equal("Klein", eintrag.Text);
        Assert.Equal(1, eintrag.Level);
    }

    /// <summary>Nur die Ebenen aus der Angabe kommen hinein.</summary>
    [Fact]
    public void Das_Verzeichnis_haelt_sich_an_seine_Ebenen()
    {
        var doc = Dok(
            Ueberschrift("Eins", 1),
            Ueberschrift("Zwei", 2),
            Ueberschrift("Drei", 3),
            Ueberschrift("Vier", 4),
            new TdParagraph("Fließtext"));

        Assert.Equal(["Eins", "Zwei", "Drei"],
            TdToc.Eintraege(doc, null, "1-3").Select(e => e.Text));

        Assert.Equal(["Zwei"],
            TdToc.Eintraege(doc, null, "2-2").Select(e => e.Text));
    }

    /// <summary>
    /// Das Verzeichnis nennt die Seite, auf der die Überschrift steht. **Das ist die Zahl, um
    /// derentwillen es überhaupt gerechnet wird** — und sie braucht einen zweiten Durchgang,
    /// weil der erste noch keine Seiten kennt.
    /// </summary>
    [Fact]
    public void Das_Verzeichnis_nennt_die_richtige_Seite()
    {
        var doc = Dok(
            new TdParagraph([new TdField(TdFieldKind.TableOfContents)]),
            Ueberschrift("Eins", 1),
            new TdPageBreak(),
            Ueberschrift("Zwei", 1));

        var ergebnis = Umbrechen(doc);
        var zeilen = ergebnis.Pages[0].Lines;

        // Zwei Einträge, dann die erste Überschrift selbst.
        Assert.Equal("Eins", zeilen[0].Runs[0].Text);
        Assert.Equal("1", zeilen[0].Runs[^1].Text);
        Assert.Equal("Zwei", zeilen[1].Runs[0].Text);
        Assert.Equal("2", zeilen[1].Runs[^1].Text);
    }

    /// <summary>
    /// Die Seitenzahl steht **rechtsbündig** am Zeilenende. Einen Tabulator mit Füllzeichen,
    /// wie Word ihn dafür benutzt, kennt das Modell nicht — für ein Verzeichnis genügt der
    /// rechte Rand, und den kennt der Umbruch ohnehin.
    /// </summary>
    [Fact]
    public void Die_Seitenzahl_im_Verzeichnis_steht_rechts()
    {
        var doc = Dok(
            new TdParagraph([new TdField(TdFieldKind.TableOfContents)]),
            Ueberschrift("Eins", 1));

        var zeile = Umbrechen(doc).Pages[0].Lines[0];

        // Blatt ist 10 cm breit, die Zahl „1" ist 1 cm breit.
        Assert.Equal(9.0, zeile.Runs[^1].XCm, 3);
    }

    /// <summary>
    /// <b>Eine Verzeichniszeile steht so nicht im Dokument</b> — ihr Absatz ist gerechnet.
    /// Sie zeigt deshalb auf den Absatz mit dem Feld und ist als Verzeichniszeile erkennbar;
    /// wer das übersieht, setzt den Cursor in einen Absatz, den es nicht gibt (dieselbe
    /// Vorsicht wie bei der wiederholten Kopfzeile, §4.19).
    /// </summary>
    [Fact]
    public void Eine_Verzeichniszeile_ist_als_solche_erkennbar()
    {
        var verzeichnis = new TdParagraph([new TdField(TdFieldKind.TableOfContents)]);
        var doc = Dok(verzeichnis, Ueberschrift("Eins", 1));

        var zeilen = Umbrechen(doc).Pages[0].Lines;

        Assert.True(zeilen[0].IsTocEntry);
        Assert.Same(verzeichnis, zeilen[0].Source);
        Assert.False(zeilen[1].IsTocEntry);   // die Überschrift selbst
    }

    /// <summary>
    /// Tiefere Ebenen rücken ein — sonst sähe ein Verzeichnis wie eine Liste gleichrangiger
    /// Zeilen aus.
    /// </summary>
    [Fact]
    public void Tiefere_Ebenen_ruecken_im_Verzeichnis_ein()
    {
        var doc = Dok(
            new TdParagraph([new TdField(TdFieldKind.TableOfContents)]),
            Ueberschrift("A", 1),
            Ueberschrift("B", 2));

        var zeilen = Umbrechen(doc).Pages[0].Lines;

        Assert.Equal(0.0, zeilen[0].Runs[0].XCm, 3);
        Assert.Equal(0.5, zeilen[1].Runs[0].XCm, 3);
    }

    /// <summary>
    /// <b>Ein Verzeichnis ohne Überschriften bleibt eine Zeile</b> und verschwindet nicht.
    /// Sonst hätte der Cursor an der Stelle des Verzeichnisses keinen Ort — dieselbe
    /// Begründung wie beim leeren Absatz (§4.16).
    /// </summary>
    [Fact]
    public void Ein_leeres_Verzeichnis_behaelt_seine_Zeile()
    {
        var doc = Dok(
            new TdParagraph([new TdField(TdFieldKind.TableOfContents)]),
            // Kurz genug für eine Zeile — zehn Zeichen passen aufs Blatt (§4.16).
            new TdParagraph("Fliesstext"));

        var seite = Umbrechen(doc).Pages[0];

        Assert.Equal(2, seite.Lines.Count);
        Assert.Equal("", seite.Lines[0].PlainText());
    }

    /// <summary>
    /// Das Verzeichnis steht nicht in sich selbst. Wer den Verzeichnisabsatz als Überschrift
    /// mitzählte, bekäme mit jedem Durchgang eine Zeile mehr.
    /// </summary>
    [Fact]
    public void Das_Verzeichnis_nimmt_sich_selbst_nicht_auf()
    {
        var doc = Dok(
            new TdParagraph([new TdField(TdFieldKind.TableOfContents)]) { Format = { OutlineLevel = 1 } },
            Ueberschrift("Eins", 1));

        Assert.Equal(["Eins"], TdToc.Eintraege(doc, null).Select(e => e.Text));
    }

    /// <summary>
    /// Eine Überschrift **in einer Tabellenzelle** ist eine Überschrift. Der Durchlauf steigt
    /// in Zellen ab — dieselbe Lehre, die die Listennummerierung in §4.19 gekostet hat.
    /// </summary>
    [Fact]
    public void Eine_Ueberschrift_in_einer_Zelle_kommt_ins_Verzeichnis()
    {
        var doc = Dok(new TdTable(new TdTableRow(new TdTableCell(Ueberschrift("In der Zelle", 1)))));

        Assert.Equal(["In der Zelle"], TdToc.Eintraege(doc, null).Select(e => e.Text));
    }

    /// <summary>
    /// Ein Dokument ohne Verzeichnis braucht keine Sprungziele — und der Umbruch keinen
    /// zweiten Durchgang. Der Wächter hält fest, dass beides erkannt wird.
    /// </summary>
    [Fact]
    public void Ein_Verzeichnis_wird_im_Dokument_gefunden()
    {
        Assert.False(TdToc.Enthaelt(Dok(new TdParagraph("nichts"))));
        Assert.True(TdToc.Enthaelt(Dok(new TdParagraph([new TdField(TdFieldKind.TableOfContents)]))));
    }
}
