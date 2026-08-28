using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdLayout"/> — Zeilen- und Seitenumbruch, die zweite Hälfte von Phase 4,
/// Schritt 2.
///
/// <para>
/// <b>Gemessen wird mit festen Maßen und nicht mit echten Schriften.</b> „Segoe UI" gibt es
/// unter Linux nicht, und schon ein Schriftartenupdate verschiebt jede Breite — ein Test
/// gegen echte Maße hätte auf Windows und im Linux-Lauf der CI verschiedene Ergebnisse und
/// wäre nach dem ersten falschen Alarm abgeschaltet. Dieselbe Überlegung, aus der HANDOFF
/// §4.6 die Schrift aus den Renderer-Snapshots heraushält.
/// </para>
/// <para>
/// <see cref="FesteMessung"/> macht daraus exakte Zahlen: **jedes Zeichen ist 1 cm breit,
/// jede Zeile 1 cm hoch.** Damit ist „nach zehn Zeichen umbrechen" eine Behauptung, die
/// stimmt oder nicht — und kein Ungefähr.
/// </para>
/// </summary>
public sealed class UmbruchTests
{
    /// <summary>
    /// Ein Zeichen = 1 cm, eine Zeile = 1 cm. Die Schriftgröße geht bewusst **nicht** ein:
    /// was hier geprüft wird, ist die Rechnung, nicht die Schrift.
    /// </summary>
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

    private static TdLayoutResult Umbrechen(TdDocument doc) =>
        TdLayout.Umbrechen(doc, new FesteMessung());

    /// <summary>
    /// Ein Dokument **ohne Absatzabstände**. Der Standard hat 8 pt nach jedem Absatz — was
    /// richtig ist, aber jede Umbruchrechnung im Kopf zunichte macht: aus „zehn Zeilen à 1 cm
    /// passen auf 10 cm" würden sieben. Wo der Abstand geprüft werden soll, wird er am Absatz
    /// ausdrücklich gesetzt (<see cref="Absatzabstaende_zaehlen_beim_Seitenumbruch_mit"/>).
    /// </summary>
    private static TdDocument Dok(TdPageSetup seite, params TdBlock[] bloecke) => new()
    {
        DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 0 },
        Sections = { new TdSection(bloecke) { Page = seite } },
    };

    // ==================== Zeilenumbruch ====================

    /// <summary>Was in eine Zeile passt, bleibt in einer Zeile.</summary>
    [Fact]
    public void Kurzer_Text_bleibt_eine_Zeile()
    {
        var ergebnis = Umbrechen(Dok(Blatt(), new TdParagraph("kurz")));

        var seite = Assert.Single(ergebnis.Pages);
        Assert.Single(seite.Lines);
        Assert.Equal("kurz", seite.Lines[0].PlainText());
    }

    /// <summary>
    /// Umgebrochen wird an Wortgrenzen, und der Leerraum am Zeilenanfang fällt weg — sonst
    /// rückte jede Folgezeile um ein Leerzeichen ein, und bei Blocksatz fiele das sofort auf.
    /// </summary>
    [Fact]
    public void Umbrochen_wird_am_Wort_und_der_Leerraum_faellt_weg()
    {
        // „aaaa bbbb cccc" — 14 Zeichen bei 10 cm Breite.
        var ergebnis = Umbrechen(Dok(Blatt(), new TdParagraph("aaaa bbbb cccc")));

        var zeilen = ergebnis.Pages[0].Lines;
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("aaaa bbbb", zeilen[0].PlainText());
        Assert.Equal("cccc", zeilen[1].PlainText());
        // Die zweite Zeile beginnt am linken Rand und nicht hinter einem Leerzeichen.
        Assert.Equal(0, zeilen[1].Runs[0].XCm, 3);
    }

    /// <summary>
    /// <b>Ein Wort, das breiter ist als die Zeile, darf nicht in eine Endlosschleife
    /// führen.</b> Es steht allein in seiner Zeile und ragt heraus — sichtbar falsch ist
    /// besser als ein Umbruch, der nicht zurückkommt. Dieselbe Lehre wie bei
    /// <c>Eine_Tabelle_braucht_ihre_Trennzeile</c> (HANDOFF §4.12).
    /// </summary>
    [Fact]
    public void Ein_ueberlanges_Wort_haengt_die_Rechnung_nicht_auf()
    {
        var ergebnis = Umbrechen(Dok(Blatt(breite: 5), new TdParagraph("kurz Donaudampfschifffahrt kurz")));

        var zeilen = ergebnis.Pages.SelectMany(s => s.Lines).ToList();
        Assert.Contains(zeilen, z => z.PlainText() == "Donaudampfschifffahrt");
        Assert.Equal("kurz Donaudampfschifffahrt kurz", string.Join(" ", zeilen.Select(z => z.PlainText())));
    }

    /// <summary>
    /// Ein erzwungener Zeilenumbruch (Umschalt+Eingabe) beendet die Zeile, aber nicht den
    /// Absatz — die Absatzabstände gelten weiter.
    /// </summary>
    [Fact]
    public void Ein_Zeilenumbruch_beendet_die_Zeile_und_nicht_den_Absatz()
    {
        var doc = Dok(Blatt(), new TdParagraph([new TdRun("ab"), new TdLineBreak(), new TdRun("cd")]));

        var zeilen = Umbrechen(doc).Pages[0].Lines;

        Assert.Equal(2, zeilen.Count);
        Assert.Equal("ab", zeilen[0].PlainText());
        Assert.Equal("cd", zeilen[1].PlainText());
        Assert.Same(zeilen[0].Source, zeilen[1].Source);
    }

    /// <summary>
    /// Ein leerer Absatz hat trotzdem eine Zeile mit Höhe — sonst hätte der Cursor keinen
    /// Ort und der Seitenumbruch nichts zu rechnen.
    /// </summary>
    [Fact]
    public void Ein_leerer_Absatz_hat_eine_Zeile_mit_Hoehe()
    {
        var zeilen = Umbrechen(Dok(Blatt(), new TdParagraph())).Pages[0].Lines;

        var zeile = Assert.Single(zeilen);
        Assert.Empty(zeile.Runs);
        Assert.True(zeile.HeightCm > 0);
    }

    // ==================== Einzüge und Ausrichtung ====================

    /// <summary>
    /// Der Erstzeileneinzug gilt nur für die erste Zeile — und verschmälert sie. Ein
    /// hängender Einzug (negativ) macht sie entsprechend breiter.
    /// </summary>
    [Fact]
    public void Der_Erstzeileneinzug_gilt_nur_fuer_die_erste_Zeile()
    {
        var doc = Dok(Blatt(), new TdParagraph("aaa bbb ccc") { Format = { FirstLineIndentCm = 4 } });

        var zeilen = Umbrechen(doc).Pages[0].Lines;

        // Erste Zeile: 10 − 4 = 6 cm → „aaa bbb" (7) passt nicht, „aaa" schon.
        Assert.Equal("aaa", zeilen[0].PlainText());
        Assert.Equal(4, zeilen[0].Runs[0].XCm, 3);
        // Zweite Zeile: volle 10 cm, beginnt am Rand.
        Assert.Equal("bbb ccc", zeilen[1].PlainText());
        Assert.Equal(0, zeilen[1].Runs[0].XCm, 3);
    }

    /// <summary>Die seitlichen Einzüge verschmälern jede Zeile des Absatzes.</summary>
    [Fact]
    public void Seitliche_Einzuege_verschmaelern_den_Absatz()
    {
        var doc = Dok(Blatt(), new TdParagraph("aaaa bbbb")
        {
            Format = { LeftIndentCm = 2, RightIndentCm = 2 },
        });

        // 10 − 2 − 2 = 6 cm; „aaaa bbbb" ist 9 Zeichen und passt nicht.
        Assert.Equal(2, Umbrechen(doc).Pages[0].Lines.Count);
    }

    /// <summary>Zentriert und rechtsbündig verschieben die ganze Zeile.</summary>
    [Theory]
    [InlineData(TdAlign.Left, 0.0)]
    [InlineData(TdAlign.Center, 3.0)]
    [InlineData(TdAlign.Right, 6.0)]
    public void Die_Ausrichtung_verschiebt_die_Zeile(TdAlign ausrichtung, double erwartetesX)
    {
        var doc = Dok(Blatt(), new TdParagraph("abcd") { Format = { Alignment = ausrichtung } });

        Assert.Equal(erwartetesX, Umbrechen(doc).Pages[0].Lines[0].Runs[0].XCm, 3);
    }

    /// <summary>
    /// <b>Blocksatz gilt nicht für die letzte Zeile eines Absatzes.</b> Sonst zöge ein
    /// Schlusswort über die ganze Breite auseinander — der auffälligste Fehler, den ein
    /// Textsatz machen kann.
    /// </summary>
    [Fact]
    public void Blocksatz_laesst_die_letzte_Zeile_in_Ruhe()
    {
        var doc = Dok(Blatt(), new TdParagraph("aaa bbb ccc") { Format = { Alignment = TdAlign.Justify } });

        var zeilen = Umbrechen(doc).Pages[0].Lines;
        Assert.Equal(2, zeilen.Count);

        // Erste Zeile („aaa bbb", 7 von 10 cm): der Rest wird auf die Lücke verteilt, das
        // zweite Stück rückt nach rechts.
        Assert.True(zeilen[0].Runs[^1].XCm > 4, "Der Blocksatz hat die erste Zeile nicht gestreckt.");

        // Letzte Zeile: unangetastet am linken Rand.
        Assert.Equal(0, zeilen[1].Runs[0].XCm, 3);
    }

    // ==================== Blocksatz an der Stückgrenze (§5 „Noch offen" 6) ====================

    /// <summary>
    /// Ein Absatz aus mehreren Zeichenformaten. <b>Der Formatwechsel selbst darf im Umbruch
    /// nichts kosten</b> — er teilt ein Stück, aber er fügt keinen Leerraum hinzu.
    /// </summary>
    private static TdParagraph Gemischt(params (string Text, bool Fett)[] teile)
    {
        var absatz = new TdParagraph(teile.Select(t => (TdInline)new TdRun(t.Text)
        {
            Format = new TdCharFormat { Bold = t.Fett },
        }));
        absatz.Format.Alignment = TdAlign.Justify;
        return absatz;
    }

    /// <summary>
    /// <b>Ein Formatwechsel mitten im Wort bekommt keinen Wortzwischenraum.</b> Auf dem Laptop
    /// gefunden (§4.28, 2026-08-11), aufgeklärt in §5 „Noch offen" 6: Der Blocksatz verteilte
    /// den Restplatz auf **jede** Stückgrenze, und ein Stück entsteht nicht nur am Wort,
    /// sondern auch dort, wo die Schrift umschlägt. „abcd" mit fettem „cd" stand deshalb als
    /// „ab cd" da.
    /// </summary>
    [Fact]
    public void Blocksatz_streckt_keinen_Formatwechsel_im_Wort()
    {
        // „abcd ef ghij" — „cd" fett. Zeile 1 fasst „abcd ef" (7 von 10 cm), „ ghij" nicht mehr.
        var doc = Dok(Blatt(), Gemischt(("ab", false), ("cd", true), (" ef ghij", false)));

        var zeile = Umbrechen(doc).Pages[0].Lines[0];

        Assert.Equal(["ab", "cd", " ef"], zeile.Runs.Select(r => r.Text));

        // „cd" schließt unmittelbar an „ab" an — dazwischen steht kein Zeichen, also auch
        // keine Lücke.
        Assert.Equal(2, zeile.Runs[1].XCm, 6);

        // Und die Zeile reicht trotzdem bis an den rechten Rand: die ganzen 3 cm Rest sind in
        // den **einen** Zwischenraum gegangen, den es wirklich gibt.
        Assert.Equal(10, zeile.Runs[^1].XCm + zeile.Runs[^1].WidthCm, 6);
    }

    /// <summary>
    /// <b>Ein Leerzeichen an einer Stückgrenze ist ein Zwischenraum und nicht zwei.</b> Fällt
    /// der Formatwechsel genau zwischen zwei Wörter, steht der Leerraum als eigenes Stück da —
    /// mit einer Grenze davor und einer dahinter. Die alte Rechnung streckte beide und machte
    /// aus einer Lücke die anderthalbfache: der Fund „Unterstrichenes␣␣und" aus §4.28.
    /// </summary>
    [Fact]
    public void Blocksatz_zaehlt_einen_Leerraum_an_der_Stueckgrenze_einmal()
    {
        // „ab cd ef gh" — „cd" fett, der Leerraum davor gehört zum normalen Stück.
        var doc = Dok(Blatt(), Gemischt(("ab ", false), ("cd", true), (" ef gh", false)));

        var zeile = Umbrechen(doc).Pages[0].Lines[0];

        Assert.Equal(["ab", " ", "cd", " ef"], zeile.Runs.Select(r => r.Text));

        // Zwei Wörter, zwei echte Zwischenräume, 2 cm Rest — also je 1 cm Streckung. Gemessen
        // wird von Wortende zu Wortanfang, denn nur das sieht man:
        //   „ab" endet bei 2, „cd" beginnt bei 4  → 2 cm, davon 1 cm Leerzeichen
        //   „cd" endet bei 6, „ef" beginnt bei 8  → 2 cm, davon 1 cm Leerzeichen
        Assert.Equal(4, zeile.Runs[2].XCm, 6);
        Assert.Equal(8, zeile.Runs[3].XCm + 1, 6);
        Assert.Equal(10, zeile.Runs[^1].XCm + zeile.Runs[^1].WidthCm, 6);
    }

    /// <summary>
    /// <b>Vor einem Schlusspunkt steht keine Lücke</b> — auch dann nicht, wenn das Wort davor
    /// eine eigene Farbe hat. Das ist die zweite Hälfte des Fundes aus §4.28: Der Punkt war
    /// das letzte Stück der Zeile und bekam deshalb die **volle** Streckung, obwohl vor ihm
    /// nichts steht, was sich strecken ließe.
    /// </summary>
    [Fact]
    public void Blocksatz_setzt_keine_Luecke_vor_den_Schlusspunkt()
    {
        // „ab cd. xyzw" — „cd" fett, der Punkt wieder normal.
        var doc = Dok(Blatt(), Gemischt(("ab ", false), ("cd", true), (". xyzw", false)));

        var zeile = Umbrechen(doc).Pages[0].Lines[0];

        Assert.Equal(["ab", " ", "cd", "."], zeile.Runs.Select(r => r.Text));

        // Der Punkt klebt am „d", wo er hingehört.
        Assert.Equal(zeile.Runs[2].XCm + zeile.Runs[2].WidthCm, zeile.Runs[3].XCm, 6);
        Assert.Equal(10, zeile.Runs[^1].XCm + zeile.Runs[^1].WidthCm, 6);
    }

    /// <summary>
    /// <b>Ein Zwischenraum am Zeilenende zieht nichts auseinander.</b> Er entsteht, wenn das
    /// nächste Wort nicht mehr passte. Wer ihn mitzählte, streckte eine Zeile, in der es nach
    /// dem letzten Wort gar nichts mehr zu verschieben gibt — das Wort stünde am linken Rand
    /// und der Leerraum am rechten.
    /// </summary>
    [Fact]
    public void Blocksatz_streckt_nicht_in_einen_Leerraum_am_Zeilenende()
    {
        // „ab cdefghijkl" — der Leerraum steht nur deshalb als **eigenes** Stück da, weil
        // dahinter die Schrift umschlägt; sonst hinge er am Wort. Das lange Wort passt nicht
        // mehr, die erste Zeile endet also auf dem Leerzeichen.
        var doc = Dok(Blatt(), Gemischt(("ab ", false), ("cdefghijkl", true)));

        var zeile = Umbrechen(doc).Pages[0].Lines[0];

        Assert.Equal(["ab", " "], zeile.Runs.Select(r => r.Text));
        Assert.Equal(0, zeile.Runs[0].XCm, 6);
        Assert.Equal(2, zeile.Runs[1].XCm, 6);
    }

    /// <summary>
    /// <b>Eine Grafik ist Inhalt, auch ohne Text.</b> Endet die Zeile auf einem Bild, soll sie
    /// bis an den Rand reichen — sonst bliebe ausgerechnet die Zeile kurz, in der das
    /// auffälligste Stück steht.
    /// </summary>
    [Fact]
    public void Blocksatz_zieht_die_Zeile_auch_bis_an_ein_Bild()
    {
        var absatz = new TdParagraph(new TdInline[]
        {
            new TdRun("ab cd "),
            new TdImage { WidthCm = 2, HeightCm = 1 },
            new TdRun(" efghij"),
        });
        absatz.Format.Alignment = TdAlign.Justify;

        var zeile = Umbrechen(Dok(Blatt(), absatz)).Pages[0].Lines[0];

        // „ab"(2) + " cd"(3) + " "(1) + Bild(2) = 8 cm, „ efghij" passt nicht mehr.
        Assert.Equal(10, zeile.Runs[^1].XCm + zeile.Runs[^1].WidthCm, 6);
        Assert.NotNull(zeile.Runs[^1].Graphic);
    }

    // ==================== Seitenumbruch ====================

    /// <summary>Passt der Text nicht mehr, beginnt eine neue Seite — und zählt weiter.</summary>
    [Fact]
    public void Was_nicht_mehr_passt_kommt_auf_die_naechste_Seite()
    {
        // Zwölf Absätze à 1 cm auf ein Blatt mit 10 cm Texthöhe.
        var bloecke = Enumerable.Range(1, 12).Select(i => (TdBlock)new TdParagraph($"Z{i}")).ToArray();

        var ergebnis = Umbrechen(Dok(Blatt(), bloecke));

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal(10, ergebnis.Pages[0].Lines.Count);
        Assert.Equal(2, ergebnis.Pages[1].Lines.Count);
        Assert.Equal(1, ergebnis.Pages[0].Number);
        Assert.Equal(2, ergebnis.Pages[1].Number);
        // Jede Seite fängt oben an.
        Assert.Equal(0, ergebnis.Pages[1].Lines[0].YCm, 3);
    }

    /// <summary>Ein erzwungener Seitenumbruch wirkt, auch wenn die Seite noch halb leer ist.</summary>
    [Fact]
    public void Ein_erzwungener_Seitenumbruch_wirkt_sofort()
    {
        var ergebnis = Umbrechen(Dok(Blatt(),
            new TdParagraph("oben"), new TdPageBreak(), new TdParagraph("unten")));

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal("oben", ergebnis.Pages[0].Lines[0].PlainText());
        Assert.Equal("unten", ergebnis.Pages[1].Lines[0].PlainText());
    }

    /// <summary>
    /// <c>PageBreakBefore</c> auf dem **ersten** Absatz darf keine leere Seite davor
    /// erzeugen — der häufigste Weg, wie ein Dokument ein weißes Deckblatt bekommt.
    /// </summary>
    [Fact]
    public void Ein_Umbruch_vor_dem_ersten_Absatz_erzeugt_keine_leere_Seite()
    {
        var doc = Dok(Blatt(), new TdParagraph("erster") { Format = { PageBreakBefore = true } });

        var ergebnis = Umbrechen(doc);

        Assert.Single(ergebnis.Pages);
        Assert.Equal("erster", ergebnis.Pages[0].Lines[0].PlainText());
    }

    /// <summary>
    /// <b>„Nicht vom nächsten Absatz trennen" bindet die Überschrift an ihren Text.</b> Ohne
    /// das stünde eine Überschrift allein unten auf der Seite — der klassische Satzfehler.
    /// </summary>
    [Fact]
    public void KeepWithNext_nimmt_die_Ueberschrift_mit_auf_die_naechste_Seite()
    {
        // Neun Füllzeilen, dann eine Überschrift mit KeepWithNext, dann zwei Zeilen Text.
        // Die Überschrift säße auf Zeile 10 — die letzte, die noch passt.
        var bloecke = new List<TdBlock>();
        for (int i = 1; i <= 9; i++) bloecke.Add(new TdParagraph($"F{i}"));
        bloecke.Add(new TdParagraph("Kapitel") { Format = { KeepWithNext = true } });
        bloecke.Add(new TdParagraph("Text A"));
        bloecke.Add(new TdParagraph("Text B"));

        var ergebnis = Umbrechen(Dok(Blatt(), [.. bloecke]));

        Assert.Equal(2, ergebnis.PageCount);
        // Die Überschrift ist mitgewandert und steht nicht mehr auf Seite 1.
        Assert.DoesNotContain(ergebnis.Pages[0].Lines, z => z.PlainText() == "Kapitel");
        Assert.Equal("Kapitel", ergebnis.Pages[1].Lines[0].PlainText());
        Assert.Equal("Text A", ergebnis.Pages[1].Lines[1].PlainText());
    }

    /// <summary>
    /// Eine Gruppe, die auf **keine** Seite passt, muss trotzdem gesetzt werden — sonst
    /// liefe der Umbruch endlos. Sie bricht dann innerhalb um.
    /// </summary>
    [Fact]
    public void Eine_zu_grosse_Gruppe_bricht_innerhalb_um_statt_haengenzubleiben()
    {
        var bloecke = new List<TdBlock>();
        // 15 Absätze, alle aneinandergebunden, auf ein Blatt mit 10 cm Texthöhe.
        for (int i = 1; i <= 15; i++)
            bloecke.Add(new TdParagraph($"Z{i}") { Format = { KeepWithNext = true } });
        bloecke.Add(new TdParagraph("Ende"));

        var ergebnis = Umbrechen(Dok(Blatt(), [.. bloecke]));

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal(16, ergebnis.Pages.Sum(s => s.Lines.Count));
    }

    /// <summary>Die Abstände vor und nach einem Absatz zählen zur Höhe und damit zum Umbruch.</summary>
    [Fact]
    public void Absatzabstaende_zaehlen_beim_Seitenumbruch_mit()
    {
        // Fünf Absätze à 1 cm Zeile + 1 cm Abstand danach = 10 cm; der sechste passt nicht.
        var bloecke = Enumerable.Range(1, 6)
            .Select(i => (TdBlock)new TdParagraph($"Z{i}") { Format = { SpaceAfterPt = 72 / 2.54 } })
            .ToArray();

        var ergebnis = Umbrechen(Dok(Blatt(), bloecke));

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal(5, ergebnis.Pages[0].Lines.Count);
    }

    /// <summary>
    /// Der Abstand **vor** einem Absatz schiebt den Text nach unten und nicht die Zeile
    /// hinein: die Grundlinie wandert mit. Ohne das säße der Text im Abstand.
    /// </summary>
    [Fact]
    public void Der_Abstand_davor_schiebt_die_Grundlinie_mit()
    {
        double einCm = 72 / 2.54;
        var ohne = Umbrechen(Dok(Blatt(), new TdParagraph("x"))).Pages[0].Lines[0];
        var mit = Umbrechen(Dok(Blatt(),
            new TdParagraph("x") { Format = { SpaceBeforePt = einCm } })).Pages[0].Lines[0];

        Assert.Equal(ohne.BaselineCm + 1.0, mit.BaselineCm, 3);
        Assert.Equal(ohne.HeightCm + 1.0, mit.HeightCm, 3);
    }

    // ==================== Abschnitte ====================

    /// <summary>
    /// <b>Jeder Abschnitt beginnt auf einer neuen Seite</b> — das ist DOCX' Vorgabe für eine
    /// <c>sectPr</c> ohne eigene Angabe, und der einzige Fall, den das Modell heute kennt.
    /// </summary>
    [Fact]
    public void Jeder_Abschnitt_beginnt_auf_einer_neuen_Seite()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(new TdParagraph("Deckblatt")) { Page = Blatt() },
                new TdSection(new TdParagraph("Inhalt")) { Page = Blatt() },
            },
        };

        var ergebnis = Umbrechen(doc);

        Assert.Equal(2, ergebnis.PageCount);
        Assert.Equal("Deckblatt", ergebnis.Pages[0].Lines[0].PlainText());
        Assert.Equal("Inhalt", ergebnis.Pages[1].Lines[0].PlainText());
    }

    /// <summary>
    /// Jede Seite kennt die Einrichtung ihres Abschnitts — sonst wüsste der Zeichner nicht,
    /// wie groß das Blatt ist, auf das er sie malt.
    /// </summary>
    [Fact]
    public void Jede_Seite_kennt_ihre_eigene_Einrichtung()
    {
        var doc = new TdDocument
        {
            Sections =
            {
                new TdSection(new TdParagraph("quer")) { Page = TdPageSetup.A4.Quer() },
                new TdSection(new TdParagraph("hoch")) { Page = TdPageSetup.A5 },
            },
        };

        var ergebnis = Umbrechen(doc);

        Assert.True(ergebnis.Pages[0].Setup.IstQuerformat);
        Assert.Equal("A5", ergebnis.Pages[1].Setup.Name);
    }

    /// <summary>
    /// Ein Dokument ohne jeden Absatz hat trotzdem eine Seite — sonst gäbe es nichts
    /// anzuzeigen und nichts zu drucken.
    /// </summary>
    [Fact]
    public void Ein_leeres_Dokument_hat_trotzdem_eine_Seite()
    {
        Assert.Equal(1, Umbrechen(new TdDocument()).PageCount);
        Assert.Equal(1, Umbrechen(TdDocument.Leer()).PageCount);
    }

    // ==================== Die echte Messung ====================

    /// <summary>
    /// <see cref="TdSkiaMeasure"/> selbst wird nur auf Plausibilität geprüft — an Zahlen
    /// festzuhalten, die von der installierten Schrift abhängen, wäre genau der Test, den
    /// das erste Schriftartenupdate rot macht (HANDOFF §4.6).
    /// <para>
    /// Geprüft wird, was auf **jedem** System gilt: breiterer Text misst mehr, eine größere
    /// Schrift misst mehr, und eine Schrift, die es nicht gibt, wirft nicht.
    /// </para>
    /// </summary>
    [Fact]
    public void Die_Skia_Messung_ist_plausibel_und_ueberlebt_eine_fehlende_Schrift()
    {
        using var messung = new TdSkiaMeasure();

        var klein = new TdCharFormat { FontFamily = "Gibt Es Nicht 12345", FontSize = 10 };
        var gross = new TdCharFormat { FontFamily = "Gibt Es Nicht 12345", FontSize = 20 };

        double kurz = messung.WidthCm("ab", klein);
        double lang = messung.WidthCm("abcdefgh", klein);

        Assert.True(kurz > 0, "Zwei Zeichen messen null.");
        Assert.True(lang > kurz, "Längerer Text misst nicht mehr.");
        Assert.True(messung.WidthCm("ab", gross) > kurz, "Größere Schrift misst nicht mehr.");
        Assert.Equal(0, messung.WidthCm("", klein));

        var m = messung.Metrics(klein);
        Assert.True(m.AscentCm > 0 && m.DescentCm > 0);
        Assert.True(m.LineHeightCm >= m.AscentCm + m.DescentCm);
    }
}
