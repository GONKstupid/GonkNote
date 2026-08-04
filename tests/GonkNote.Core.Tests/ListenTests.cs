using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// Listen — Phase 4, Schritt 3.
///
/// <para>
/// <b>Ein Listenpunkt ist ein Absatz mit einer Angabe und kein eigener Blocktyp.</b> Das ist
/// die Entscheidung dieses Schritts, und sie folgt DOCX: der Absatz trägt ein <c>w:numPr</c>,
/// die Liste selbst ist nur eine Definition anderswo. Der Gewinn ist die Bearbeitung —
/// Eingabe drücken, Ebene wechseln, einen Punkt herausnehmen bleibt eine Absatzänderung.
/// </para>
///
/// <para>
/// <b>Die Nummer steht nicht im Absatz</b>, sondern wird gerechnet: sie hängt davon ab, was
/// vor ihm kommt. Gespeichert wäre sie bei jeder Einfügung im ganzen Dokument nachzuziehen,
/// und jede vergessene Stelle wäre eine Liste, die bei 3 weiterzählt, nachdem die 2 gelöscht
/// wurde. Hier liegt entsprechend der Schwerpunkt der Wächter.
/// </para>
/// </summary>
public sealed class ListenTests
{
    private sealed class FesteMessung : ITdTextMeasure
    {
        public double WidthCm(string text, TdCharFormat format) => text.Length;
        public TdFontMetrics Metrics(TdCharFormat format) => new(0.8, 0.2, 1.0);
    }

    private static TdParagraph Punkt(string text, int liste, int ebene) =>
        new(text) { List = new TdListRef(liste, ebene) };

    /// <summary>Ein Dokument mit einer Liste und den angegebenen Punkten.</summary>
    private static TdDocument MitListe(TdListDefinition definition, params TdBlock[] bloecke) => new()
    {
        DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 0 },
        Lists = { definition },
        Sections = { new TdSection(bloecke) },
    };

    private static string[] Marken(TdDocument doc)
    {
        var marken = TdListNumbering.Marken(doc);
        return [.. doc.Paragraphs().Where(marken.ContainsKey).Select(p => marken[p])];
    }

    // ==================== Nummerierung ====================

    [Fact]
    public void Eine_nummerierte_Liste_zaehlt_hoch()
    {
        var doc = MitListe(TdListDefinition.Nummern(1),
            Punkt("a", 1, 0), Punkt("b", 1, 0), Punkt("c", 1, 0));

        Assert.Equal(["1.", "2.", "3."], Marken(doc));
    }

    /// <summary>Aufzählungspunkte zählen nicht — ihre Marke ist immer dieselbe.</summary>
    [Fact]
    public void Aufzaehlungspunkte_bleiben_gleich()
    {
        var doc = MitListe(TdListDefinition.Punkte(1),
            Punkt("a", 1, 0), Punkt("b", 1, 0));

        Assert.Equal(["•", "•"], Marken(doc));
    }

    /// <summary>
    /// Ein Absatz **ohne** Listenangabe zwischen zwei Punkten unterbricht die Zählung nicht.
    /// Das ist Words Verhalten und das, was man beim Schreiben erwartet: ein erklärender
    /// Absatz zwischen Punkt 2 und 3 macht aus der 3 keine 1.
    /// </summary>
    [Fact]
    public void Ein_Absatz_dazwischen_unterbricht_die_Zaehlung_nicht()
    {
        var doc = MitListe(TdListDefinition.Nummern(1),
            Punkt("a", 1, 0),
            new TdParagraph("Dazwischen etwas Text."),
            Punkt("b", 1, 0));

        Assert.Equal(["1.", "2."], Marken(doc));
    }

    /// <summary>
    /// <b>Zwei Listen zählen getrennt.</b> Wer beiden dieselbe Kennung gäbe, bekäme eine
    /// zweite Liste, die bei 3 anfängt — der Fehler, den man erst beim zweiten Kapitel sieht.
    /// </summary>
    [Fact]
    public void Zwei_Listen_zaehlen_getrennt()
    {
        var doc = new TdDocument
        {
            Lists = { TdListDefinition.Nummern(1), TdListDefinition.Nummern(2) },
            Sections =
            {
                new TdSection(
                    Punkt("a", 1, 0), Punkt("b", 1, 0),
                    Punkt("x", 2, 0), Punkt("y", 2, 0)),
            },
        };

        Assert.Equal(["1.", "2.", "1.", "2."], Marken(doc));
    }

    [Fact]
    public void NextListId_vergibt_keine_Kennung_zweimal()
    {
        var doc = new TdDocument();
        Assert.Equal(1, doc.NextListId());

        doc.Lists.Add(TdListDefinition.Nummern(doc.NextListId()));
        Assert.Equal(2, doc.NextListId());

        doc.Lists.Add(TdListDefinition.Punkte(7));
        Assert.Equal(8, doc.NextListId());
    }

    // ==================== Ebenen ====================

    /// <summary>
    /// <b>Eine tiefere Ebene fängt bei jedem Einrücken neu an.</b> Ohne das Zurücksetzen
    /// zählte die zweite Unterliste dort weiter, wo die erste aufgehört hat — der Fehler, den
    /// man erst bei der dritten Ebene bemerkt.
    /// </summary>
    [Fact]
    public void Eine_Unterliste_faengt_bei_jedem_Einruecken_neu_an()
    {
        var doc = MitListe(TdListDefinition.Nummern(1),
            Punkt("1", 1, 0),
            Punkt("1.1", 1, 1),
            Punkt("1.2", 1, 1),
            Punkt("2", 1, 0),
            Punkt("2.1", 1, 1));

        Assert.Equal(["1.", "1.", "2.", "2.", "1."], Marken(doc));
    }

    /// <summary>
    /// Eine Gliederungsvorlage („%1.%2.") setzt die Zähler **mehrerer** Ebenen zusammen —
    /// jeden in der Schreibweise seiner eigenen Ebene.
    /// </summary>
    [Fact]
    public void Eine_Gliederungsvorlage_setzt_mehrere_Zaehler_zusammen()
    {
        var definition = new TdListDefinition
        {
            Id = 1,
            Levels =
            [
                new TdListLevel { Marker = TdListMarker.Decimal, Text = "%1." },
                new TdListLevel { Marker = TdListMarker.Decimal, Text = "%1.%2." },
                new TdListLevel { Marker = TdListMarker.Decimal, Text = "%1.%2.%3." },
            ],
        };

        var doc = MitListe(definition,
            Punkt("a", 1, 0),
            Punkt("b", 1, 1),
            Punkt("c", 1, 2),
            Punkt("d", 1, 1),
            Punkt("e", 1, 0));

        Assert.Equal(["1.", "1.1.", "1.1.1.", "1.2.", "2."], Marken(doc));
    }

    /// <summary>
    /// Eine Ebene, die es in der Definition nicht gibt, darf das Dokument nicht sprengen —
    /// sie kommt aus einer fremden Datei. Genommen wird die tiefste vorhandene.
    /// </summary>
    [Fact]
    public void Eine_zu_tiefe_Ebene_faellt_auf_die_letzte_zurueck()
    {
        var definition = new TdListDefinition
        {
            Id = 1,
            Levels = [new TdListLevel { Marker = TdListMarker.Decimal, Text = "%1." }],
        };

        var doc = MitListe(definition, Punkt("tief", 1, 5));

        Assert.Equal(["1."], Marken(doc));
    }

    /// <summary>Ein Verweis auf eine Liste, die es nicht gibt, ergibt keine Marke — und keinen Absturz.</summary>
    [Fact]
    public void Ein_Verweis_ins_Leere_ergibt_keine_Marke()
    {
        var doc = new TdDocument { Sections = { new TdSection(Punkt("verwaist", 99, 0)) } };

        Assert.Empty(Marken(doc));
        Assert.Equal("verwaist", doc.PlainText());
    }

    // ==================== Schreibweisen ====================

    [Theory]
    [InlineData(1, TdListMarker.LowerLetter, "a")]
    [InlineData(26, TdListMarker.LowerLetter, "z")]
    [InlineData(27, TdListMarker.LowerLetter, "aa")]
    [InlineData(28, TdListMarker.UpperLetter, "AB")]
    [InlineData(1, TdListMarker.LowerRoman, "i")]
    [InlineData(4, TdListMarker.LowerRoman, "iv")]
    [InlineData(9, TdListMarker.UpperRoman, "IX")]
    [InlineData(14, TdListMarker.UpperRoman, "XIV")]
    [InlineData(1994, TdListMarker.UpperRoman, "MCMXCIV")]
    [InlineData(42, TdListMarker.Decimal, "42")]
    public void Die_Schreibweisen_stimmen(int wert, TdListMarker art, string erwartet)
    {
        Assert.Equal(erwartet, TdListNumbering.Formatiert(wert, art));
    }

    /// <summary>
    /// Eine Null hat keine römische Schreibweise. Sie kommt aus einer fremden Datei mit
    /// <c>start="0"</c> — dann steht dort die Ziffer und nicht nichts.
    /// </summary>
    [Fact]
    public void Eine_Null_bleibt_lesbar()
    {
        Assert.Equal("0", TdListNumbering.Formatiert(0, TdListMarker.UpperRoman));
        Assert.Equal("", TdListNumbering.Formatiert(0, TdListMarker.LowerLetter));
    }

    // ==================== Umbruch ====================

    /// <summary>
    /// <b>Die Marke steht links vom Text, und der Text aller Zeilen fluchtet.</b> Genau das
    /// unterscheidet eine Liste von einem Absatz, dem man ein „• " vorangestellt hat: dort
    /// rückte die zweite Zeile unter die Marke.
    /// </summary>
    [Fact]
    public void Die_Marke_steht_links_und_der_Text_fluchtet()
    {
        var definition = TdListDefinition.Nummern(1);
        definition.Levels[0].IndentCm = 2;
        definition.Levels[0].HangingCm = 1;

        var doc = MitListe(definition, Punkt("aaaa bbbb cccc", 1, 0));
        var seite = new TdPageSetup { WidthCm = 12, HeightCm = 30, MarginLeftCm = 1, MarginRightCm = 1 };
        doc.Sections[0].Page = seite;

        var zeilen = TdLayout.Umbrechen(doc, new FesteMessung()).Pages[0].Lines;

        Assert.True(zeilen.Count >= 2, "Der Text sollte über zwei Zeilen gehen.");
        // Marke bei 2 − 1 = 1 cm, nur auf der ersten Zeile.
        Assert.NotNull(zeilen[0].Marker);
        Assert.Equal("1.", zeilen[0].Marker!.Text);
        Assert.Equal(1.0, zeilen[0].Marker!.XCm, 3);
        Assert.Null(zeilen[1].Marker);

        // Der Text beider Zeilen beginnt beim Einzug — nicht unter der Marke.
        Assert.Equal(2.0, zeilen[0].Runs[0].XCm, 3);
        Assert.Equal(2.0, zeilen[1].Runs[0].XCm, 3);
    }

    /// <summary>
    /// Die Marke gehört **nicht** zum Text: sie steht nicht in <c>Runs</c>, taucht im
    /// Klartext nicht auf und wird damit auch nicht mitkopiert.
    /// </summary>
    [Fact]
    public void Die_Marke_ist_kein_Text()
    {
        var doc = MitListe(TdListDefinition.Nummern(1), Punkt("Inhalt", 1, 0));

        var zeile = TdLayout.Umbrechen(doc, new FesteMessung()).Pages[0].Lines[0];

        Assert.Equal("Inhalt", zeile.PlainText());
        Assert.DoesNotContain(zeile.Runs, r => r.Text == "1.");
        Assert.Equal("Inhalt", doc.PlainText());
    }

    /// <summary>
    /// Der Einzug kommt aus der Ebene — **es sei denn, der Absatz sagt selbst etwas anderes**.
    /// So hält es DOCX: das <c>pPr</c> der Ebene ist die Vorlage, das des Absatzes schlägt sie.
    /// </summary>
    [Fact]
    public void Ein_eigener_Einzug_am_Absatz_schlaegt_die_Ebene()
    {
        var definition = TdListDefinition.Nummern(1);
        definition.Levels[0].IndentCm = 2;

        var eigen = Punkt("x", 1, 0);
        eigen.Format.LeftIndentCm = 5;

        var doc = MitListe(definition, eigen);
        doc.Sections[0].Page = new TdPageSetup { WidthCm = 12, HeightCm = 30, MarginLeftCm = 1, MarginRightCm = 1 };

        var zeile = TdLayout.Umbrechen(doc, new FesteMessung()).Pages[0].Lines[0];

        Assert.Equal(5.0, zeile.Runs[0].XCm, 3);
    }

    // ==================== DOCX ====================

    private static TdDocument Zurueck(TdDocument doc)
    {
        using var strom = new MemoryStream();
        TdDocx.Schreiben(doc, strom);
        strom.Position = 0;
        return TdDocx.Lesen(strom);
    }

    /// <summary>
    /// Das Tor aus Roadmap §5: Listen gehen durch DOCX und kommen gleich wieder heraus —
    /// Definition, Zugehörigkeit und Ebene.
    /// </summary>
    [Fact]
    public void Listen_ueberstehen_den_DOCX_Roundtrip()
    {
        var definition = TdListDefinition.Nummern(1);
        definition.Levels[1].Marker = TdListMarker.LowerLetter;
        definition.Levels[1].Text = "%2)";

        var doc = MitListe(definition,
            Punkt("eins", 1, 0),
            Punkt("eins-a", 1, 1),
            Punkt("eins-b", 1, 1),
            Punkt("zwei", 1, 0));

        var zurueck = Zurueck(doc);

        var liste = Assert.Single(zurueck.Lists);
        Assert.Equal(1, liste.Id);
        Assert.Equal(9, liste.Levels.Count);
        Assert.Equal(TdListMarker.Decimal, liste.Levels[0].Marker);
        Assert.Equal("%1.", liste.Levels[0].Text);
        Assert.Equal(TdListMarker.LowerLetter, liste.Levels[1].Marker);
        Assert.Equal("%2)", liste.Levels[1].Text);

        var absaetze = zurueck.Paragraphs().ToList();
        Assert.Equal(4, absaetze.Count);
        Assert.All(absaetze, a => Assert.NotNull(a.List));
        Assert.Equal([0, 1, 1, 0], absaetze.Select(a => a.List!.Level));
        Assert.All(absaetze, a => Assert.Equal(1, a.List!.ListId));

        // Und die Nummern rechnen sich danach genauso.
        Assert.Equal(["1.", "a)", "b)", "2."], Marken(zurueck));
    }

    /// <summary>Ein Dokument mit Listen muss Word öffnen können.</summary>
    [Fact]
    public void Ein_DOCX_mit_Listen_haelt_das_Office_Schema_ein()
    {
        string ordner = Path.Combine(Path.GetTempPath(), $"gonk-listen-{Guid.NewGuid():N}");
        Directory.CreateDirectory(ordner);
        try
        {
            string pfad = Path.Combine(ordner, "listen.docx");
            var doc = MitListe(TdListDefinition.Punkte(1),
                Punkt("a", 1, 0), Punkt("b", 1, 1));

            TdDocx.Schreiben(doc, pfad);

            Assert.Equal(0, TdDocx.Pruefen(pfad));
        }
        finally
        {
            try { Directory.Delete(ordner, recursive: true); } catch { /* Wegwerf */ }
        }
    }

    /// <summary>
    /// Ein Absatz **ohne** Liste darf durch den Roundtrip keine bekommen — sonst würde aus
    /// jedem Fließtext ein Aufzählungspunkt.
    /// </summary>
    [Fact]
    public void Ein_gewoehnlicher_Absatz_bekommt_keine_Liste()
    {
        var doc = MitListe(TdListDefinition.Nummern(1),
            Punkt("Punkt", 1, 0),
            new TdParagraph("Fließtext"));

        var absaetze = Zurueck(doc).Paragraphs().ToList();

        Assert.NotNull(absaetze[0].List);
        Assert.Null(absaetze[1].List);
    }

    /// <summary>
    /// Zwei Listen bleiben zwei Listen — mit getrennten Kennungen, damit sie getrennt zählen.
    /// </summary>
    [Fact]
    public void Zwei_Listen_bleiben_durch_DOCX_getrennt()
    {
        var doc = new TdDocument
        {
            Lists = { TdListDefinition.Nummern(1), TdListDefinition.Punkte(2) },
            Sections = { new TdSection(Punkt("a", 1, 0), Punkt("b", 2, 0), Punkt("c", 1, 0)) },
        };

        var zurueck = Zurueck(doc);

        Assert.Equal(2, zurueck.Lists.Count);
        Assert.Equal([1, 2], zurueck.Lists.Select(l => l.Id).Order());
        Assert.Equal(["1.", "•", "2."], Marken(zurueck));
    }

    /// <summary>
    /// Einzug und hängender Einzug der Ebene gehen durch die Nummerierungsdefinition — nicht
    /// über das Absatzformat. Ginge das verloren, säße jede Liste am linken Rand.
    /// </summary>
    [Fact]
    public void Die_Einzuege_der_Ebene_ueberstehen_DOCX()
    {
        var definition = TdListDefinition.Nummern(1);
        definition.Levels[0].IndentCm = 1.75;
        definition.Levels[0].HangingCm = 0.6;

        var zurueck = Zurueck(MitListe(definition, Punkt("x", 1, 0)));

        Assert.Equal(1.75, zurueck.Lists[0].Levels[0].IndentCm, 2);
        Assert.Equal(0.6, zurueck.Lists[0].Levels[0].HangingCm, 2);
    }

    /// <summary>Der Startwert einer Ebene übersteht den Roundtrip — eine Liste darf bei 5 anfangen.</summary>
    [Fact]
    public void Ein_eigener_Startwert_ueberlebt()
    {
        var definition = TdListDefinition.Nummern(1);
        definition.Levels[0].Start = 5;

        var zurueck = Zurueck(MitListe(definition, Punkt("a", 1, 0), Punkt("b", 1, 0)));

        Assert.Equal(5, zurueck.Lists[0].Levels[0].Start);
        Assert.Equal(["5.", "6."], Marken(zurueck));
    }
}
