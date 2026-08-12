using System.Text;
using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdUndo"/> — Schritt 3 des Schreibens (HANDOFF §6).
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Das Zurücknehmen selbst ist seit Schritt 2 geprüft
/// (<see cref="SchreibenTests"/>): Eine <see cref="TdChange"/> führt vollständig zurück. Was
/// hier geprüft wird, ist das, was der Verlauf dazutut — **Reihenfolge** und
/// **Zusammenfassen**. Und das Zusammenfassen ist die Stelle, an der ein Verlauf Schaden
/// anrichten kann: Wer zwei Schritte zusammenzieht, die nicht lückenlos aufeinanderfolgen,
/// lässt beim Zurücknehmen einen Absatz verschwinden, der dazwischen entstanden ist.
/// </para>
/// </summary>
public sealed class RueckgaengigTests
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

    private static TdParagraph Text(string text) => new(text);

    private static TdSelection Bei(TdDocument doc, int absatz, int linear)
    {
        var a = TdCursor.AbsatzAn(doc, absatz)!;
        return new TdSelection(TdCursor.AusLinear(a, absatz, linear));
    }

    private static string Klartext(TdDocument doc) =>
        TdCursor.Text(doc, TdSelection.Alles(doc));

    /// <summary><inheritdoc cref="SchreibenTests" path="/summary"/></summary>
    private static string Abbild(TdDocument doc)
    {
        var sb = new StringBuilder();
        foreach (var absatz in doc.Paragraphs())
        {
            sb.Append('¶');
            foreach (var stueck in absatz.Inlines)
                sb.Append(stueck is TdRun run ? $"<{run.Text}>" : "<?>");
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Tippen, ausführen, merken — die Kette, die die Oberfläche später fährt.</summary>
    private static TdSelection Tippen(TdDocument doc, TdUndo verlauf, TdSelection bei, string text)
    {
        var aenderung = TdEdit.Tippen(doc, bei, text)!;
        var danach = aenderung.Anwenden();
        verlauf.Push(aenderung);
        return danach;
    }

    private static TdSelection Ruecktaste(TdDocument doc, TdUndo verlauf, TdSelection bei)
    {
        var aenderung = TdEdit.Rueckwaerts(doc, bei)!;
        var danach = aenderung.Anwenden();
        verlauf.Push(aenderung);
        return danach;
    }

    // ==================== Zurück und wieder vor ====================

    /// <summary>Der einfachste Fall — und die Auswahl kommt mit zurück.</summary>
    [Fact]
    public void Zurueck_stellt_den_Text_und_die_Auswahl_wieder_her()
    {
        var doc = Dok(Text("ab"));
        var verlauf = new TdUndo();

        var stelle = Bei(doc, 0, 1);
        Tippen(doc, verlauf, stelle, "X");

        Assert.Equal("aXb", Klartext(doc));
        Assert.True(verlauf.CanUndo);

        Assert.Equal(stelle, verlauf.Undo());
        Assert.Equal("ab", Klartext(doc));
        Assert.False(verlauf.CanUndo);
    }

    /// <summary>Und wieder vor, mit der Auswahl von danach.</summary>
    [Fact]
    public void Vor_fuehrt_die_Aenderung_wieder_aus()
    {
        var doc = Dok(Text("ab"));
        var verlauf = new TdUndo();

        var danach = Tippen(doc, verlauf, Bei(doc, 0, 1), "X");
        verlauf.Undo();

        Assert.True(verlauf.CanRedo);
        Assert.Equal(danach, verlauf.Redo());
        Assert.Equal("aXb", Klartext(doc));
    }

    /// <summary>Ein leerer Verlauf tut nichts und wirft nicht.</summary>
    [Fact]
    public void Ein_leerer_Verlauf_liefert_nichts()
    {
        var verlauf = new TdUndo();

        Assert.False(verlauf.CanUndo);
        Assert.False(verlauf.CanRedo);
        Assert.Null(verlauf.Undo());
        Assert.Null(verlauf.Redo());
    }

    /// <summary>
    /// Mehrere Schritte kommen in umgekehrter Reihenfolge zurück — das ist der ganze Zweck
    /// einer Reihenfolge.
    /// </summary>
    [Fact]
    public void Mehrere_Schritte_kommen_rueckwaerts_zurueck()
    {
        var doc = Dok(Text(""));
        var verlauf = new TdUndo();

        var stelle = Bei(doc, 0, 0);
        foreach (string wort in new[] { "eins ", "zwei ", "drei" })
            stelle = Tippen(doc, verlauf, stelle, wort);

        Assert.Equal("eins zwei drei", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("eins zwei ", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("eins ", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("", Klartext(doc));
        Assert.False(verlauf.CanUndo);
    }

    /// <summary>
    /// **Ein neuer Schritt wirft die Wiederherstellung weg.** Wer nach einem Rückgängig
    /// weiterschreibt, hat sich für diesen Weg entschieden.
    /// </summary>
    [Fact]
    public void Ein_neuer_Schritt_wirft_die_Wiederherstellung_weg()
    {
        var doc = Dok(Text("ab"));
        var verlauf = new TdUndo();

        Tippen(doc, verlauf, Bei(doc, 0, 1), "X");
        verlauf.Undo();
        Assert.True(verlauf.CanRedo);

        Tippen(doc, verlauf, Bei(doc, 0, 2), "Y");

        Assert.False(verlauf.CanRedo);
        Assert.Equal("abY", Klartext(doc));
    }

    /// <summary>Hin und her, mehrfach — ein Dokument darf dabei nicht wegdriften.</summary>
    [Fact]
    public void Zurueck_und_vor_im_Wechsel_bleibt_stabil()
    {
        var doc = Dok(Text("ab"));
        var verlauf = new TdUndo();

        Tippen(doc, verlauf, Bei(doc, 0, 1), "X");
        string danach = Abbild(doc);

        for (int i = 0; i < 5; i++)
        {
            verlauf.Undo();
            Assert.Equal("¶<ab>\n", Abbild(doc));
            verlauf.Redo();
            Assert.Equal(danach, Abbild(doc));
        }
    }

    // ==================== Zusammenfassen ====================

    /// <summary>
    /// **Wer „Hallo" tippt, hat einen Handgriff gemacht und nicht fünf.** Ohne das nimmt
    /// Strg+Z ein Zeichen je Druck zurück — der Fehler, über den jeder als Erstes stolpert.
    /// </summary>
    [Fact]
    public void Getippte_Zeichen_werden_zu_einem_Schritt()
    {
        var doc = Dok(Text(""));
        var verlauf = new TdUndo();

        var stelle = Bei(doc, 0, 0);
        foreach (char c in "Hallo") stelle = Tippen(doc, verlauf, stelle, c.ToString());

        Assert.Equal("Hallo", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("", Klartext(doc));
        Assert.False(verlauf.CanUndo);
    }

    /// <summary>
    /// **Ein Zwischenraum schneidet.** Sonst nähme ein Strg+Z einen ganzen Absatz zurück, und
    /// niemand tippt ihn noch einmal.
    /// </summary>
    [Fact]
    public void Ein_Zwischenraum_beendet_den_Schritt()
    {
        var doc = Dok(Text(""));
        var verlauf = new TdUndo();

        var stelle = Bei(doc, 0, 0);
        foreach (char c in "Hallo Welt") stelle = Tippen(doc, verlauf, stelle, c.ToString());

        verlauf.Undo();
        Assert.Equal("Hallo ", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("", Klartext(doc));
    }

    /// <summary>
    /// Tippen und Löschen bleiben getrennt: Wer erst schreibt und dann wegnimmt, hat sich
    /// dazwischen entschieden.
    /// </summary>
    [Fact]
    public void Tippen_und_Loeschen_werden_nicht_zusammengefasst()
    {
        var doc = Dok(Text(""));
        var verlauf = new TdUndo();

        var stelle = Tippen(doc, verlauf, Bei(doc, 0, 0), "abc");
        Ruecktaste(doc, verlauf, stelle);

        Assert.Equal("ab", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("abc", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("", Klartext(doc));
    }

    /// <summary>Gehaltene Rücktaste ist ebenfalls ein Schritt.</summary>
    [Fact]
    public void Mehrere_Ruecktasten_werden_zu_einem_Schritt()
    {
        var doc = Dok(Text("abcdef"));
        var verlauf = new TdUndo();

        var stelle = new TdSelection(TdCursor.Ende(doc));
        for (int i = 0; i < 3; i++) stelle = Ruecktaste(doc, verlauf, stelle);

        Assert.Equal("abc", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("abcdef", Klartext(doc));
        Assert.False(verlauf.CanUndo);
    }

    /// <summary>
    /// Eine Strukturänderung fasst nie zusammen — die Eingabetaste ist von selbst eine Grenze,
    /// und ein Absatz, der beim Zurücknehmen mit verschwindet, wäre eine Überraschung.
    /// </summary>
    [Fact]
    public void Eine_Strukturaenderung_fasst_nie_zusammen()
    {
        var doc = Dok(Text("ab"));
        var verlauf = new TdUndo();

        var stelle = Tippen(doc, verlauf, Bei(doc, 0, 2), "c");

        var teilen = TdEdit.AbsatzTeilen(doc, stelle)!;
        stelle = teilen.Anwenden();
        verlauf.Push(teilen);

        Tippen(doc, verlauf, stelle, "d");

        Assert.Equal("abc\nd", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("abc\n", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("abc", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("ab", Klartext(doc));
    }

    /// <summary>
    /// **Was nicht lückenlos anschließt, wird nicht zusammengefasst** — auch wenn es dieselbe
    /// Art ist. Zwei Buchstaben in verschiedenen Absätzen sind zwei Schritte.
    /// </summary>
    [Fact]
    public void Zwei_Stellen_ergeben_zwei_Schritte()
    {
        var doc = Dok(Text("ab"), Text("cd"));
        var verlauf = new TdUndo();

        Tippen(doc, verlauf, Bei(doc, 0, 1), "X");
        Tippen(doc, verlauf, Bei(doc, 1, 1), "Y");

        Assert.Equal("aXb\ncYd", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("aXb\ncd", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("ab\ncd", Klartext(doc));
    }

    /// <summary>
    /// Der Schnitt, den nur die Oberfläche setzen kann: Wer die Schreibmarke versetzt, fängt
    /// einen neuen Handgriff an. **Der Verlauf sieht Änderungen und keine Klicks.**
    /// </summary>
    [Fact]
    public void Abschliessen_setzt_einen_Schnitt()
    {
        var doc = Dok(Text(""));
        var verlauf = new TdUndo();

        var stelle = Tippen(doc, verlauf, Bei(doc, 0, 0), "ab");
        verlauf.Abschliessen();
        Tippen(doc, verlauf, stelle, "cd");

        verlauf.Undo();
        Assert.Equal("ab", Klartext(doc));

        verlauf.Undo();
        Assert.Equal("", Klartext(doc));
    }

    /// <summary>Nach einem Rückgängig wird nicht an den zurückgenommenen Schritt angebaut.</summary>
    [Fact]
    public void Nach_einem_Rueckgaengig_faengt_ein_neuer_Schritt_an()
    {
        var doc = Dok(Text(""));
        var verlauf = new TdUndo();

        var stelle = Tippen(doc, verlauf, Bei(doc, 0, 0), "ab");
        verlauf.Undo();
        verlauf.Redo();

        Tippen(doc, verlauf, stelle, "cd");

        verlauf.Undo();
        Assert.Equal("ab", Klartext(doc));
    }

    /// <summary>
    /// **Zusammengefasst heißt nicht ungenau.** Nach fünf Tastendrücken in einem Schritt muss
    /// das Dokument vollständig dastehen wie vorher — Stückgrenzen eingeschlossen.
    /// </summary>
    [Fact]
    public void Zusammengefasst_fuehrt_trotzdem_vollstaendig_zurueck()
    {
        var doc = Dok(Text("Rand"), Text("ab"), Text("Rand"));
        var verlauf = new TdUndo();
        string vorher = Abbild(doc);

        var stelle = Bei(doc, 1, 1);
        foreach (char c in "XYZ") stelle = Tippen(doc, verlauf, stelle, c.ToString());

        Assert.NotEqual(vorher, Abbild(doc));

        verlauf.Undo();
        Assert.Equal(vorher, Abbild(doc));
    }

    // ==================== Verschmelzen von Hand ====================

    /// <summary>
    /// <see cref="TdChange.Verschmelzen"/> verlangt Lückenlosigkeit: Zwischen zwei Änderungen
    /// darf nichts geschehen sein.
    /// </summary>
    [Fact]
    public void Verschmelzen_lehnt_ab_was_nicht_lueckenlos_anschliesst()
    {
        var doc = Dok(Text("ab"));

        var erste = TdEdit.Tippen(doc, Bei(doc, 0, 1), "X")!;
        var stelle = erste.Anwenden();

        var zweite = TdEdit.Tippen(doc, stelle, "Y")!;
        Assert.NotNull(erste.Verschmelzen(zweite));

        // Dazwischen etwas anderes.
        TdEdit.Tippen(doc, stelle, "Q")!.Anwenden();
        var dritte = TdEdit.Tippen(doc, stelle, "Z")!;

        Assert.Null(erste.Verschmelzen(dritte));
    }

    /// <summary>
    /// **Und geprüft wird an den Objekten und nicht am Inhalt** — das ist der Unterschied, der
    /// hier zählt.
    ///
    /// <para>
    /// Dazwischen geschieht etwas, das den Text **wieder genauso** dastehen lässt: ein Zeichen
    /// getippt und wieder gelöscht. Der Absatz ist danach ein **anderer** (jede Änderung baut
    /// einen neuen, §4.32), trägt aber denselben Text. Ein Vergleich auf gleichen Inhalt
    /// verschmölze hier — und das Ergebnis wäre eine Änderung, deren Rücknahme die beiden
    /// Schritte dazwischen **überspringt**: Sie legt den Absatz von vor dem „X" zurück, und was
    /// seitdem geschah, ist weg.
    /// </para>
    /// </summary>
    [Fact]
    public void Verschmelzen_prueft_die_Objekte_und_nicht_den_Inhalt()
    {
        var doc = Dok(Text("ab"));

        var erste = TdEdit.Tippen(doc, Bei(doc, 0, 1), "X")!;
        var stelle = erste.Anwenden();
        Assert.Equal("aXb", Klartext(doc));

        // Hin und wieder zurück: derselbe Text, ein anderer Absatz.
        var dazwischen = TdEdit.Tippen(doc, stelle, "Q")!;
        var danach = dazwischen.Anwenden();
        TdEdit.Rueckwaerts(doc, danach)!.Anwenden();
        Assert.Equal("aXb", Klartext(doc));

        var spaeter = TdEdit.Tippen(doc, stelle, "Y")!;

        Assert.Null(erste.Verschmelzen(spaeter));
    }

    /// <summary>Eine Änderung in einem anderen Dokument verschmilzt nie.</summary>
    [Fact]
    public void Verschmelzen_lehnt_ein_fremdes_Dokument_ab()
    {
        var eins = Dok(Text("ab"));
        var zwei = Dok(Text("ab"));

        var hier = TdEdit.Tippen(eins, Bei(eins, 0, 1), "X")!;
        var dort = TdEdit.Tippen(zwei, Bei(zwei, 0, 1), "X")!;

        Assert.Null(hier.Verschmelzen(dort));
    }

    // ==================== Deckel, Leeren, Meldung ====================

    /// <summary>
    /// Der Verlauf hat eine Grenze — er **hält Absätze am Leben**, die sonst niemand mehr
    /// hält. Der älteste Schritt fällt vorne heraus.
    /// </summary>
    [Fact]
    public void Der_Verlauf_hat_eine_Grenze()
    {
        var doc = Dok(Text(""));
        var verlauf = new TdUndo();

        var stelle = Bei(doc, 0, 0);
        for (int i = 0; i < 250; i++)
        {
            stelle = Tippen(doc, verlauf, stelle, "x");
            verlauf.Abschliessen();          // jeder Druck ein eigener Schritt
        }

        int zurueck = 0;
        while (verlauf.Undo() is not null) zurueck++;

        Assert.Equal(200, zurueck);

        // Die 50 ältesten sind weg — der Text ist deshalb nicht leer, sondern 50 Zeichen lang.
        Assert.Equal(50, Klartext(doc).Length);
    }

    /// <summary>Leeren wirft beides weg — nötig, sobald der Inhalt an diesem Verlauf vorbei getauscht wird.</summary>
    [Fact]
    public void Leeren_wirft_den_ganzen_Verlauf_weg()
    {
        var doc = Dok(Text("ab"));
        var verlauf = new TdUndo();

        Tippen(doc, verlauf, Bei(doc, 0, 1), "X");
        verlauf.Undo();

        verlauf.Leeren();

        Assert.False(verlauf.CanUndo);
        Assert.False(verlauf.CanRedo);
        Assert.Null(verlauf.Undo());
    }

    /// <summary>Die Knöpfe im Ribbon hängen an dieser Meldung — wie bei <c>UndoStack</c>.</summary>
    [Fact]
    public void Jede_Bewegung_meldet_sich()
    {
        var doc = Dok(Text("ab"));
        var verlauf = new TdUndo();

        int meldungen = 0;
        verlauf.Changed += () => meldungen++;

        Tippen(doc, verlauf, Bei(doc, 0, 1), "X");
        verlauf.Undo();
        verlauf.Redo();
        verlauf.Leeren();

        Assert.Equal(4, meldungen);
    }
}
