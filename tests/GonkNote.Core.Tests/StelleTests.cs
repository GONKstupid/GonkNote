using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdPosition"/>, <see cref="TdSelection"/> und <see cref="TdCursor"/> — Schritt 1
/// des Schreibens (HANDOFF §6).
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Eine Stelle im Dokument ist kein Anzeigedetail: Auf
/// ihr steht jede Einfügung, jede Löschung und jede Auswahl, die noch kommt. Ein Cursor, der
/// an einer Stückgrenze hängenbleibt, ein Vergleich, der zwei Schreibweisen derselben Lücke
/// für verschieden hält, oder ein Schritt, der ein Emoji halbiert — das sind Fehler, die
/// später als Datenverlust herauskommen und dann nicht mehr hier gesucht werden.
/// </para>
/// <para>
/// <b>Kein Umbruch und keine Schrift.</b> Diese Rechnung kennt beides nicht; sie läuft
/// deshalb auf jedem Rechner gleich — das ist der Grund, warum die Schritte 1 bis 3 in Core
/// liegen (§6).
/// </para>
/// </summary>
public sealed class StelleTests
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

    /// <summary>Ein Absatz aus Stücken.</summary>
    private static TdParagraph Abs(params TdInline[] stuecke) => new(stuecke);

    /// <summary>Ein Absatz mit schlichtem Text.</summary>
    private static TdParagraph Text(string text) => new(text);

    // ==================== Die kanonische Form ====================

    /// <summary>
    /// Der Anfang von Stück 2 und das Ende von Stück 1 sind dieselbe Lücke — und nach dem
    /// Geradeziehen auch dieselbe **Zahl**. Ohne das hätte eine leere Auswahl plötzlich eine
    /// Länge.
    /// </summary>
    [Fact]
    public void Die_Stueckgrenze_hat_nur_eine_gueltige_Schreibweise()
    {
        var doc = Dok(Abs(new TdRun("ab"), new TdRun("cd")));

        var linksHerum = TdCursor.Normalisieren(doc, new TdPosition(0, 0, 2));
        var rechtsHerum = TdCursor.Normalisieren(doc, new TdPosition(0, 1, 0));

        Assert.Equal(linksHerum, rechtsHerum);
    }

    /// <summary>
    /// Und zwar die **linke**: das Ende des früheren Stücks. Daran hängt in Schritt 2 die
    /// Formaterbung — ein eingefügtes Zeichen erbt das Format links davon.
    /// </summary>
    [Fact]
    public void Kanonisch_ist_das_Ende_des_frueheren_Stuecks()
    {
        var doc = Dok(Abs(new TdRun("ab"), new TdRun("cd")));

        Assert.Equal(
            new TdPosition(0, 0, 2),
            TdCursor.Normalisieren(doc, new TdPosition(0, 1, 0)));
    }

    /// <summary>
    /// Absatzende und Absatzanfang werden **nicht** zusammengezogen. Zwischen ihnen steht die
    /// Absatzmarke, und der Cursor steht sichtbar in verschiedenen Zeilen.
    /// </summary>
    [Fact]
    public void Absatzende_und_naechster_Absatzanfang_bleiben_zwei_Stellen()
    {
        var doc = Dok(Text("ab"), Text("cd"));

        var ende = TdCursor.AbsatzEnde(doc, 0);
        var anfang = TdCursor.AbsatzAnfang(doc, 1);

        Assert.NotEqual(ende, anfang);
        Assert.True(ende < anfang);
    }

    /// <summary>
    /// Ein leeres Stück ist kein Ort — es entsteht beim Bearbeiten laufend, und der Cursor
    /// darf darin nicht steckenbleiben.
    /// </summary>
    [Fact]
    public void Ein_leeres_Stueck_traegt_keine_Stelle()
    {
        var doc = Dok(Abs(new TdRun("ab"), new TdRun(""), new TdRun("cd")));

        Assert.Equal(
            new TdPosition(0, 0, 2),
            TdCursor.Normalisieren(doc, new TdPosition(0, 1, 0)));

        Assert.Equal(
            new TdPosition(0, 0, 2),
            TdCursor.Normalisieren(doc, new TdPosition(0, 2, 0)));
    }

    /// <summary>Ein Index daneben wird geklemmt und nicht zur Ausnahme.</summary>
    [Fact]
    public void Eine_Stelle_ausserhalb_wird_hereingezogen()
    {
        var doc = Dok(Text("abc"));

        Assert.Equal(TdCursor.Ende(doc), TdCursor.Normalisieren(doc, new TdPosition(9, 9, 9)));
        Assert.Equal(TdPosition.Null, TdCursor.Normalisieren(doc, new TdPosition(-3, -3, -3)));
    }

    /// <summary>Ein Dokument ohne einen einzigen Absatz hat trotzdem eine Stelle.</summary>
    [Fact]
    public void Ein_Dokument_ohne_Absatz_liefert_den_Nullpunkt()
    {
        var doc = new TdDocument { Sections = { new TdSection() } };

        Assert.Equal(TdPosition.Null, TdCursor.Anfang(doc));
        Assert.Equal(TdPosition.Null, TdCursor.Ende(doc));
        Assert.Equal(TdPosition.Null, TdCursor.Rechts(doc, TdPosition.Null));
    }

    // ==================== Länge in Schritten ====================

    /// <summary>
    /// Ein Feld ist **einen** Schritt breit, obwohl sein Klartext leer ist (§4.20). Der
    /// Cursor steht davor oder dahinter — nie darin.
    /// </summary>
    [Fact]
    public void Ein_Feld_ist_einen_Schritt_breit_und_hat_keinen_Klartext()
    {
        var feld = new TdField(TdFieldKind.PageNumber);

        Assert.Equal("", feld.PlainText());
        Assert.Equal(1, TdCursor.Laenge(feld));
    }

    /// <summary>Dasselbe gilt für ein Bild — ein Schritt, kein Zeichen.</summary>
    [Fact]
    public void Ein_Bild_ist_einen_Schritt_breit()
    {
        Assert.Equal(1, TdCursor.Laenge(new TdImage { BlobId = Guid.NewGuid() }));
    }

    /// <summary>
    /// Der Cursor läuft **durch** ein Feld hindurch, ohne darin haltzumachen: davor, dahinter,
    /// fertig.
    /// </summary>
    [Fact]
    public void Ueber_ein_Feld_geht_es_in_einem_Schritt()
    {
        var doc = Dok(Abs(new TdRun("a"), new TdField(TdFieldKind.PageNumber), new TdRun("b")));

        var stelle = new TdPosition(0, 0, 1);                 // hinter dem „a", vor dem Feld
        var hinterDemFeld = TdCursor.Rechts(doc, stelle);      // ein Schritt: hinter dem Feld

        Assert.Equal(2, TdCursor.Linear(TdCursor.AbsatzAn(doc, 0)!, hinterDemFeld));
        Assert.Equal(stelle, TdCursor.Links(doc, hinterDemFeld));
    }

    // ==================== Der Verweis ====================

    /// <summary>
    /// Der Cursor sitzt **im Linktext** und nicht **im Verweis** — die flache Sicht ist
    /// dieselbe, die auch das Layout benutzt (§4.20).
    /// </summary>
    [Fact]
    public void Ein_Verweis_erscheint_nicht_selbst_sondern_mit_seinen_Stuecken()
    {
        var doc = Dok(Abs(
            new TdRun("vor "),
            new TdHyperlink("https://example.org", new TdRun("Text"))));

        var absatz = TdCursor.AbsatzAn(doc, 0)!;

        Assert.Equal(2, TdCursor.Stuecke(absatz).Count);
        Assert.All(TdCursor.Stuecke(absatz), s => Assert.IsNotType<TdHyperlink>(s));
        Assert.Equal(8, TdCursor.Laenge(absatz));       // „vor " + „Text"
    }

    /// <summary>Der Cursor läuft Zeichen für Zeichen in den Linktext hinein.</summary>
    [Fact]
    public void In_einen_Verweis_hinein_geht_es_zeichenweise()
    {
        var doc = Dok(Abs(
            new TdRun("a"),
            new TdHyperlink("https://example.org", new TdRun("bc"))));

        var stelle = TdCursor.Anfang(doc);
        for (int i = 0; i < 3; i++) stelle = TdCursor.Rechts(doc, stelle);

        Assert.Equal(TdCursor.Ende(doc), stelle);
    }

    // ==================== Bewegen ====================

    /// <summary>
    /// **Ein Zeichen heißt ein sichtbares Zeichen.** Ein Emoji steht als Paar in der
    /// Zeichenkette; wer um eine UTF-16-Einheit weiterrückt, setzt den Cursor hinein, und die
    /// nächste Eingabe zerlegt es.
    /// </summary>
    [Fact]
    public void Ein_Emoji_wird_in_einem_Schritt_uebersprungen()
    {
        var doc = Dok(Text("a\U0001F600b"));    // a 😀 b — das Emoji sind zwei char

        var nachDemA = TdCursor.Rechts(doc, TdCursor.Anfang(doc));
        var nachDemEmoji = TdCursor.Rechts(doc, nachDemA);

        Assert.Equal(1, nachDemA.Offset);
        Assert.Equal(3, nachDemEmoji.Offset);
    }

    /// <summary>Und zurück genauso — sonst zerfiele es beim Rückwärtslaufen.</summary>
    [Fact]
    public void Ein_Emoji_wird_auch_rueckwaerts_in_einem_Schritt_uebersprungen()
    {
        var doc = Dok(Text("a\U0001F600b"));

        var ende = TdCursor.Ende(doc);
        var vorDemB = TdCursor.Links(doc, ende);
        var vorDemEmoji = TdCursor.Links(doc, vorDemB);

        Assert.Equal(3, vorDemB.Offset);
        Assert.Equal(1, vorDemEmoji.Offset);
    }

    /// <summary>
    /// Ein zusammengesetztes „ä" (a + kombinierendes Trema) ist ebenfalls **ein** Zeichen.
    ///
    /// <para>
    /// <b>Die Zeichennummer steht hier mit Absicht statt eines getippten „ä".</b> Das
    /// zerlegte und das zusammengesetzte „ä" sehen im Quelltext gleich aus; ein Werkzeug, das
    /// die Datei normalisiert, machte aus dem zerlegten still das zusammengesetzte — der
    /// Wächter wäre dann grün, ohne noch etwas zu prüfen. **Genau das ist beim Schreiben
    /// dieser Datei passiert**, deshalb die Nummer und kein Buchstabe.
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_zusammengesetztes_Zeichen_bleibt_zusammen()
    {
        var doc = Dok(Text("a" + (char)0x0308 + "b"));

        var nachDemAe = TdCursor.Rechts(doc, TdCursor.Anfang(doc));

        Assert.Equal(2, nachDemAe.Offset);
        Assert.Equal(new TdPosition(0, 0, 2), TdCursor.Links(doc, TdCursor.Ende(doc)));
    }

    /// <summary>Am Absatzende geht es in den nächsten Absatz — und nicht ins Leere.</summary>
    [Fact]
    public void Rechts_am_Absatzende_springt_in_den_naechsten_Absatz()
    {
        var doc = Dok(Text("ab"), Text("cd"));

        var weiter = TdCursor.Rechts(doc, TdCursor.AbsatzEnde(doc, 0));

        Assert.Equal(TdCursor.AbsatzAnfang(doc, 1), weiter);
    }

    /// <summary>Und am Absatzanfang zurück ans Ende des vorigen.</summary>
    [Fact]
    public void Links_am_Absatzanfang_springt_ans_Ende_des_vorigen()
    {
        var doc = Dok(Text("ab"), Text("cd"));

        var zurueck = TdCursor.Links(doc, TdCursor.AbsatzAnfang(doc, 1));

        Assert.Equal(TdCursor.AbsatzEnde(doc, 0), zurueck);
    }

    /// <summary>
    /// An den Rändern bleibt die Stelle stehen. **Kein Umlauf**, keine Ausnahme — beides
    /// hätte der Nutzer nicht erwartet.
    /// </summary>
    [Fact]
    public void An_den_Raendern_bleibt_die_Stelle_stehen()
    {
        var doc = Dok(Text("ab"));

        Assert.Equal(TdCursor.Anfang(doc), TdCursor.Links(doc, TdCursor.Anfang(doc)));
        Assert.Equal(TdCursor.Ende(doc), TdCursor.Rechts(doc, TdCursor.Ende(doc)));
    }

    /// <summary>
    /// Alle Stellen eines Dokuments der Reihe nach — mit **Schrittgrenze**.
    ///
    /// <para>
    /// <b>Die Grenze ist der eigentliche Wächter und keine Vorsicht.</b> Beim Erproben dieser
    /// Klasse wurde die kanonische Form absichtlich falsch gestellt (<c>&gt;=</c> zu
    /// <c>&gt;</c> in <c>AusLinear</c>); daraufhin war das Dokumentende nicht mehr
    /// erreichbar, und der Testlauf **hing**, statt rot zu werden. Ein Wächter, der nicht
    /// fertig wird, meldet nichts — er blockiert nur die ganze Suite. Deshalb bricht der
    /// Durchlauf hier nach mehr Schritten ab, als das Dokument breit ist, und sagt, woran es
    /// lag.
    /// </para>
    /// </summary>
    private static List<TdPosition> AlleStellenVorwaerts(TdDocument doc)
    {
        int grenze = TdCursor.Absaetze(doc).Sum(TdCursor.Laenge) + TdCursor.Absaetze(doc).Count + 2;

        var stellen = new List<TdPosition>();
        var stelle = TdCursor.Anfang(doc);
        var ende = TdCursor.Ende(doc);
        stellen.Add(stelle);

        while (stelle != ende)
        {
            Assert.True(stellen.Count <= grenze,
                $"Nach {stellen.Count} Schritten ist {ende} immer noch nicht erreicht — " +
                $"zuletzt bei {stelle}.");

            var naechste = TdCursor.Rechts(doc, stelle);
            Assert.True(naechste > stelle, $"Rechts ist bei {stelle} stehengeblieben.");

            stelle = naechste;
            stellen.Add(stelle);
        }

        return stellen;
    }

    /// <summary>
    /// Einmal durch das ganze Dokument und wieder zurück — jede Zwischenstelle muss dabei
    /// dieselbe sein. Das ist der Wächter, der Hängenbleiben und Überspringen zugleich fängt.
    /// </summary>
    [Fact]
    public void Hin_und_zurueck_fuehrt_ueber_dieselben_Stellen()
    {
        var doc = Dok(
            Abs(new TdRun("ab"), new TdRun(""), new TdField(TdFieldKind.PageNumber),
                new TdHyperlink("#ziel", new TdRun("cd"))),
            Text(""),
            Text("ef"));

        var hin = AlleStellenVorwaerts(doc);

        var stelle = hin[^1];
        var zurueck = new List<TdPosition> { stelle };
        for (int i = 0; i < hin.Count && stelle != TdCursor.Anfang(doc); i++)
        {
            stelle = TdCursor.Links(doc, stelle);
            zurueck.Add(stelle);
        }
        zurueck.Reverse();

        Assert.Equal(hin, zurueck);
    }

    /// <summary>Jede Stelle, die beim Laufen entsteht, ist schon kanonisch.</summary>
    [Fact]
    public void Bewegen_liefert_immer_kanonische_Stellen()
    {
        var doc = Dok(Abs(new TdRun("ab"), new TdRun(""), new TdRun("cd")), Text("ef"));

        foreach (var stelle in AlleStellenVorwaerts(doc))
            Assert.True(TdCursor.Gueltig(doc, stelle), $"{stelle} steht nicht kanonisch.");
    }

    // ==================== Auswahl ====================

    /// <summary>
    /// Anker und Spitze, nicht Anfang und Ende: Die Spitze darf **hinter** den Anker wandern,
    /// sonst ließe sich eine Auswahl nicht nach links verkleinern.
    /// </summary>
    [Fact]
    public void Die_Spitze_darf_vor_dem_Anker_stehen()
    {
        var doc = Dok(Text("abcdef"));
        var anker = new TdPosition(0, 0, 4);

        var rueckwaerts = new TdSelection(anker).Bis(new TdPosition(0, 0, 1));

        Assert.False(rueckwaerts.IsForward);
        Assert.Equal(new TdPosition(0, 0, 1), rueckwaerts.Start);
        Assert.Equal(anker, rueckwaerts.End);
        Assert.Equal("bcd", TdCursor.Text(doc, rueckwaerts));
    }

    /// <summary>Eine Auswahl ohne Ausdehnung ist der Cursor — es gibt keinen zweiten Zustand.</summary>
    [Fact]
    public void Eine_leere_Auswahl_ist_der_Cursor()
    {
        var stelle = new TdPosition(0, 0, 2);
        var auswahl = new TdSelection(stelle);

        Assert.True(auswahl.IsEmpty);
        Assert.Equal(stelle, auswahl.Start);
        Assert.Equal(stelle, auswahl.End);
    }

    /// <summary>Der Text über mehrere Absätze bekommt die Zeilenumbrüche wie im Klartext.</summary>
    [Fact]
    public void Ausgewaehlter_Text_trennt_Absaetze_mit_Zeilenumbruch()
    {
        var doc = Dok(Text("abc"), Text("def"), Text("ghi"));

        var auswahl = new TdSelection(new TdPosition(0, 0, 2), new TdPosition(2, 0, 1));

        Assert.Equal("c\ndef\ng", TdCursor.Text(doc, auswahl));
    }

    /// <summary>Der Text läuft über Stückgrenzen hinweg, ohne etwas zu verlieren.</summary>
    [Fact]
    public void Ausgewaehlter_Text_laeuft_ueber_Stueckgrenzen()
    {
        var doc = Dok(Abs(new TdRun("ab"), new TdRun("cd"), new TdRun("ef")));

        var auswahl = new TdSelection(new TdPosition(0, 0, 1), new TdPosition(0, 2, 1));

        Assert.Equal("bcde", TdCursor.Text(doc, auswahl));
    }

    /// <summary>
    /// **Der ausgewählte Text ist kürzer als die Auswahl breit ist**, wenn ein Feld darin
    /// steht — ein Schritt ohne Zeichen. Das steht hier als Wächter, weil es beim Zählen von
    /// Zeichen sonst als Fehler erscheint.
    /// </summary>
    [Fact]
    public void Ein_Feld_zaehlt_als_Schritt_aber_nicht_als_Zeichen()
    {
        var doc = Dok(Abs(new TdRun("a"), new TdField(TdFieldKind.PageNumber), new TdRun("b")));

        var alles = TdSelection.Alles(doc);
        var absatz = TdCursor.AbsatzAn(doc, 0)!;

        Assert.Equal(3, TdCursor.Laenge(absatz));        // a · Feld · b
        Assert.Equal("ab", TdCursor.Text(doc, alles));   // aber nur zwei Zeichen
    }

    /// <summary>Alles auswählen reicht vom ersten bis zum letzten Zeichen.</summary>
    [Fact]
    public void Alles_auswaehlen_umfasst_das_ganze_Dokument()
    {
        var doc = Dok(Text("abc"), Text("def"));

        Assert.Equal("abc\ndef", TdCursor.Text(doc, TdSelection.Alles(doc)));
    }

    // ==================== Tabellen ====================

    /// <summary>
    /// Ein Absatz in einer Tabellenzelle braucht **keine eigene Koordinate**: Der Durchlauf
    /// aus <see cref="TdDocument.Paragraphs"/> steigt hinein (§4.19), und damit hat die Zelle
    /// von selbst ihren Platz in der Absatzzählung.
    /// </summary>
    [Fact]
    public void Der_Cursor_findet_auch_Absaetze_in_Tabellenzellen()
    {
        var tabelle = new TdTable();
        var zeile = new TdTableRow();
        var zelle = new TdTableCell();
        zelle.Blocks.Add(Text("Zelle"));
        zeile.Cells.Add(zelle);
        tabelle.Rows.Add(zeile);

        var doc = new TdDocument();
        var abschnitt = new TdSection();
        abschnitt.Blocks.Add(Text("davor"));
        abschnitt.Blocks.Add(tabelle);
        doc.Sections.Add(abschnitt);

        Assert.Equal(2, TdCursor.Absaetze(doc).Count);
        Assert.Equal("Zelle", TdCursor.AbsatzAn(doc, 1)!.PlainText());
        Assert.Equal("davor\nZelle", TdCursor.Text(doc, TdSelection.Alles(doc)));
    }

    /// <summary>
    /// Der Rückweg vom Absatz zu seiner Nummer — den braucht Schritt 4, um aus
    /// <see cref="TdLine.Source"/> eine Stelle zu machen.
    /// </summary>
    [Fact]
    public void Zu_jedem_Absatz_gibt_es_seine_Nummer_zurueck()
    {
        var zweiter = Text("zwei");
        var doc = Dok(Text("eins"), zweiter, Text("drei"));

        Assert.Equal(1, TdCursor.IndexVon(doc, zweiter));
        Assert.Equal(-1, TdCursor.IndexVon(doc, Text("fremd")));
    }

    // ==================== Vergleichen ====================

    /// <summary>Die Reihenfolge ist die Leserichtung: erst Absatz, dann Stück, dann Zeichen.</summary>
    [Fact]
    public void Stellen_sind_in_Leserichtung_geordnet()
    {
        Assert.True(new TdPosition(0, 9, 9) < new TdPosition(1, 0, 0));
        Assert.True(new TdPosition(1, 0, 9) < new TdPosition(1, 1, 0));
        Assert.True(new TdPosition(1, 1, 0) < new TdPosition(1, 1, 1));
    }
}
