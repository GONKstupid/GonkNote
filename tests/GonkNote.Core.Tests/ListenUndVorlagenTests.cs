using GonkNote.Core.Text;

namespace GonkNote.Core.Tests;

/// <summary>
/// <see cref="TdListEdit"/> und <see cref="TdStil"/> — Gruppe A aus §6 (HANDOFF §4.39):
/// Aufzählung, Nummerierung und Absatzvorlagen.
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Beides sind Absatzänderungen, und ihre Fehler sind
/// leise: Eine Nummerierung, die je Absatz eine eigene Definition anlegt, sieht aus wie drei
/// Listen mit je einer 1 — und der Fehler steckt nicht im Zeichner, sondern in der Zuweisung.
/// Eine Vorlage, die die Größe an die **Stücke** statt an den Absatz schreibt, sieht sofort
/// richtig aus und verliert ihr Fett beim nächsten Tippen. Und eine Überschrift ohne
/// <see cref="TdParaFormat.OutlineLevel"/> sieht wie eine aus, steht aber nicht im
/// Inhaltsverzeichnis (§4.20).
/// </para>
/// </summary>
public sealed class ListenUndVorlagenTests
{
    // ==================== Hilfsmittel ====================

    private static TdDocument Dok(params TdBlock[] bloecke)
    {
        var doc = new TdDocument();
        var abschnitt = new TdSection();
        abschnitt.Blocks.AddRange(bloecke);
        doc.Sections.Add(abschnitt);
        return doc;
    }

    private static TdParagraph Text(string text) => new(text);

    private static TdSelection Bei(TdDocument doc, int absatz, int linear)
    {
        var a = TdCursor.AbsatzAn(doc, absatz)!;
        return new TdSelection(TdCursor.AusLinear(a, absatz, linear));
    }

    private static TdSelection Von(TdDocument doc, int absatzA, int a, int absatzB, int b) =>
        new(TdCursor.AusLinear(TdCursor.AbsatzAn(doc, absatzA)!, absatzA, a),
            TdCursor.AusLinear(TdCursor.AbsatzAn(doc, absatzB)!, absatzB, b));

    private static TdParagraph Absatz(TdDocument doc, int i) => TdCursor.AbsatzAn(doc, i)!;

    // ==================== Listen ====================

    /// <summary>Der einfachste Fall: aus drei Absätzen wird eine Aufzählung.</summary>
    [Fact]
    public void Aufzaehlung_setzt_alle_beruehrten_Absaetze()
    {
        var doc = Dok(Text("eins"), Text("zwei"), Text("drei"));

        TdListEdit.Umschalten(doc, Von(doc, 0, 0, 2, 4), nummeriert: false)!.Anwenden();

        for (int i = 0; i < 3; i++)
            Assert.True(TdListEdit.IstArt(doc, Absatz(doc, i), nummeriert: false));
    }

    /// <summary>
    /// <b>Alle in *eine* Liste.</b> Wer drei Absätze markiert und nummeriert, will 1, 2, 3 —
    /// nicht dreimal die 1. Drei Definitionen wären drei Zählungen.
    /// </summary>
    [Fact]
    public void Nummerierung_steckt_alle_in_dieselbe_Liste()
    {
        var doc = Dok(Text("eins"), Text("zwei"), Text("drei"));

        TdListEdit.Umschalten(doc, Von(doc, 0, 0, 2, 4), nummeriert: true)!.Anwenden();

        Assert.Single(doc.Lists);

        int id = Absatz(doc, 0).List!.ListId;
        Assert.Equal(id, Absatz(doc, 1).List!.ListId);
        Assert.Equal(id, Absatz(doc, 2).List!.ListId);

        // Und die Probe darauf, dass es wirklich eine Zählung ist:
        var marken = TdListNumbering.Marken(doc);
        Assert.Equal("1.", marken[Absatz(doc, 0)]);
        Assert.Equal("2.", marken[Absatz(doc, 1)]);
        Assert.Equal("3.", marken[Absatz(doc, 2)]);
    }

    /// <summary>Nochmals derselbe Knopf hebt die Liste auf.</summary>
    [Fact]
    public void Nochmals_derselbe_Knopf_hebt_die_Liste_auf()
    {
        var doc = Dok(Text("eins"), Text("zwei"));

        TdListEdit.Umschalten(doc, Von(doc, 0, 0, 1, 4), nummeriert: false)!.Anwenden();
        TdListEdit.Umschalten(doc, Von(doc, 0, 0, 1, 4), nummeriert: false)!.Anwenden();

        Assert.Null(Absatz(doc, 0).List);
        Assert.Null(Absatz(doc, 1).List);
    }

    /// <summary>
    /// <b>Gemischt zählt als „aus".</b> Ist einer der berührten Absätze kein Listenpunkt, macht
    /// der erste Klick alle zu einem — dieselbe Regel wie beim Fettmachen (§4.36).
    /// </summary>
    [Fact]
    public void Gemischte_Auswahl_wird_beim_ersten_Klick_einheitlich()
    {
        var doc = Dok(Text("eins"), Text("zwei"));

        TdListEdit.Umschalten(doc, Bei(doc, 0, 0), nummeriert: false)!.Anwenden();
        TdListEdit.Umschalten(doc, Von(doc, 0, 0, 1, 4), nummeriert: false)!.Anwenden();

        Assert.NotNull(Absatz(doc, 0).List);
        Assert.NotNull(Absatz(doc, 1).List);
    }

    /// <summary>
    /// Von Aufzählung auf Nummerierung: die Art wechselt, und es entsteht **eine** zweite
    /// Definition — nicht je Absatz eine.
    /// </summary>
    [Fact]
    public void Wechsel_der_Art_legt_genau_eine_zweite_Definition_an()
    {
        var doc = Dok(Text("eins"), Text("zwei"));
        var alles = Von(doc, 0, 0, 1, 4);

        TdListEdit.Umschalten(doc, alles, nummeriert: false)!.Anwenden();
        TdListEdit.Umschalten(doc, Von(doc, 0, 0, 1, 4), nummeriert: true)!.Anwenden();

        Assert.Equal(2, doc.Lists.Count);
        Assert.True(TdListEdit.IstArt(doc, Absatz(doc, 0), nummeriert: true));
        Assert.False(TdListEdit.IstArt(doc, Absatz(doc, 0), nummeriert: false));
    }

    /// <summary>
    /// <b>Wiederverwenden statt neu anlegen:</b> Zweimal dieselbe Art ergibt **eine**
    /// Definition. Sonst bekämen zwei getrennt nummerierte Absätze zweimal die 1.
    /// </summary>
    [Fact]
    public void Dieselbe_Art_zweimal_ergibt_eine_Definition()
    {
        var doc = Dok(Text("eins"), Text("zwei"));

        TdListEdit.Umschalten(doc, Bei(doc, 0, 0), nummeriert: true)!.Anwenden();
        TdListEdit.Umschalten(doc, Bei(doc, 1, 0), nummeriert: true)!.Anwenden();

        Assert.Single(doc.Lists);
    }

    /// <summary>Ausschalten legt keine Definition an — sie würde niemandem gehören.</summary>
    [Fact]
    public void Ausschalten_legt_keine_Definition_an()
    {
        var doc = Dok(Text("eins"));

        TdListEdit.Umschalten(doc, Bei(doc, 0, 0), nummeriert: false)!.Anwenden();
        int vorher = doc.Lists.Count;

        TdListEdit.Umschalten(doc, Bei(doc, 0, 0), nummeriert: false)!.Anwenden();

        Assert.Equal(vorher, doc.Lists.Count);
    }

    /// <summary>Die Ebene rückt ein und wieder aus — und nicht über Words neun hinaus.</summary>
    [Fact]
    public void Die_Ebene_bleibt_zwischen_null_und_acht()
    {
        var doc = Dok(Text("eins"));
        TdListEdit.Umschalten(doc, Bei(doc, 0, 0), nummeriert: false)!.Anwenden();

        TdListEdit.Ebene(doc, Bei(doc, 0, 0), +1)!.Anwenden();
        Assert.Equal(1, Absatz(doc, 0).List!.Level);

        for (int i = 0; i < 20; i++) TdListEdit.Ebene(doc, Bei(doc, 0, 0), +1)!.Anwenden();
        Assert.Equal(8, Absatz(doc, 0).List!.Level);

        for (int i = 0; i < 20; i++) TdListEdit.Ebene(doc, Bei(doc, 0, 0), -1)!.Anwenden();
        Assert.Equal(0, Absatz(doc, 0).List!.Level);
    }

    /// <summary>Wer in keiner Liste steht, hat keine Ebene — der Handgriff tut dort nichts.</summary>
    [Fact]
    public void Ohne_Liste_gibt_es_keine_Ebene()
    {
        var doc = Dok(Text("eins"));

        Assert.Null(TdListEdit.Ebene(doc, Bei(doc, 0, 0), +1));
    }

    /// <summary>Und die Probe: die Rücknahme führt vollständig zurück.</summary>
    [Fact]
    public void Ruecknahme_der_Liste_fuehrt_zurueck()
    {
        var doc = Dok(Text("eins"), Text("zwei"));

        var aenderung = TdListEdit.Umschalten(doc, Von(doc, 0, 0, 1, 4), nummeriert: true)!;
        aenderung.Anwenden();
        aenderung.Zuruecknehmen();

        Assert.Null(Absatz(doc, 0).List);
        Assert.Null(Absatz(doc, 1).List);
    }

    // ==================== Vorlagen ====================

    /// <summary>
    /// <b>Die Größe geht an den Absatz, nicht an seine Stücke.</b> Nur so überlebt ein einzeln
    /// fett gemachtes Wort eine spätere Änderung der Überschrift — und das ist der Grund, warum
    /// <c>TdParagraph.CharFormat</c> überhaupt existiert.
    /// </summary>
    [Fact]
    public void Eine_Vorlage_setzt_am_Absatz_und_nicht_am_Stueck()
    {
        var doc = Dok(Text("Kapitel"));

        TdListEdit.Vorlage(doc, Bei(doc, 0, 0), TdStil.ZurEbene(1)!.Value)!.Anwenden();

        var absatz = Absatz(doc, 0);
        Assert.Equal(28, absatz.CharFormat.FontSize);
        Assert.True(absatz.CharFormat.Bold);
        Assert.Null(((TdRun)absatz.Inlines[0]).Format.FontSize);
    }

    /// <summary>
    /// <b>Ohne Gliederungsebene steht die Überschrift nicht im Inhaltsverzeichnis</b> (§4.20) —
    /// sie sähe aus wie eine und wäre keine.
    /// </summary>
    [Fact]
    public void Eine_Ueberschrift_bekommt_ihre_Gliederungsebene()
    {
        var doc = Dok(Text("Kapitel"), Text("Unterkapitel"));

        TdListEdit.Vorlage(doc, Bei(doc, 0, 0), TdStil.ZurEbene(1)!.Value)!.Anwenden();
        TdListEdit.Vorlage(doc, Bei(doc, 1, 0), TdStil.ZurEbene(3)!.Value)!.Anwenden();

        Assert.Equal(1, Absatz(doc, 0).Format.OutlineLevel);
        Assert.Equal(3, Absatz(doc, 1).Format.OutlineLevel);
    }

    /// <summary>„Standard" nimmt die Gliederungsebene wieder heraus.</summary>
    [Fact]
    public void Standard_nimmt_die_Gliederungsebene_heraus()
    {
        var doc = Dok(Text("Kapitel"));

        TdListEdit.Vorlage(doc, Bei(doc, 0, 0), TdStil.ZurEbene(1)!.Value)!.Anwenden();
        TdListEdit.Vorlage(doc, Bei(doc, 0, 0), TdStil.Standard)!.Anwenden();

        Assert.Null(Absatz(doc, 0).Format.OutlineLevel);
        Assert.Equal(TdStil.KoerperPt, Absatz(doc, 0).CharFormat.FontSize);
    }

    /// <summary>
    /// Eine Überschrift ist kein Listenpunkt — <b>„Standard" dagegen hebt die Liste nicht auf</b>:
    /// Sie ist das, worauf man landet, wenn man eine Überschrift zurücknimmt, und dabei einen
    /// Aufzählungspunkt mit zu verlieren wäre eine Überraschung.
    /// </summary>
    [Fact]
    public void Eine_Ueberschrift_hebt_die_Liste_auf_Standard_nicht()
    {
        var doc = Dok(Text("eins"), Text("zwei"));

        TdListEdit.Umschalten(doc, Von(doc, 0, 0, 1, 4), nummeriert: false)!.Anwenden();

        TdListEdit.Vorlage(doc, Bei(doc, 0, 0), TdStil.ZurEbene(2)!.Value)!.Anwenden();
        Assert.Null(Absatz(doc, 0).List);

        TdListEdit.Vorlage(doc, Bei(doc, 1, 0), TdStil.Standard)!.Anwenden();
        Assert.NotNull(Absatz(doc, 1).List);
    }

    /// <summary>
    /// Am Stück gesetzte Abweichungen überleben die Vorlage — wer in einer Überschrift ein Wort
    /// kursiv gemacht hat, will es kursiv behalten.
    /// </summary>
    [Fact]
    public void Abweichungen_am_Stueck_ueberleben_die_Vorlage()
    {
        var doc = Dok(new TdParagraph([
            new TdRun("Kapitel "),
            new TdRun("zwei", new TdCharFormat { Italic = true }),
        ]));

        TdListEdit.Vorlage(doc, Bei(doc, 0, 0), TdStil.ZurEbene(1)!.Value)!.Anwenden();

        Assert.True(((TdRun)Absatz(doc, 0).Inlines[1]).Format.Italic);
    }

    /// <summary>
    /// Welche Vorlage zeigt die Auswahl? — <b>und die dritte Antwort, wenn sie sich nicht einig
    /// ist</b> (§4.36).
    /// </summary>
    [Fact]
    public void GemeinsameVorlage_meldet_Uneinigkeit_als_null()
    {
        var doc = Dok(Text("eins"), Text("zwei"));

        TdListEdit.Vorlage(doc, Bei(doc, 0, 0), TdStil.ZurEbene(1)!.Value)!.Anwenden();
        Assert.Equal("Überschrift 1", TdListEdit.GemeinsameVorlage(doc, Bei(doc, 0, 0))!.Value.Name);

        TdListEdit.Vorlage(doc, Bei(doc, 1, 0), TdStil.ZurEbene(2)!.Value)!.Anwenden();
        Assert.Null(TdListEdit.GemeinsameVorlage(doc, Von(doc, 0, 0, 1, 4)));
    }

    /// <summary>Und die Probe: die Rücknahme führt vollständig zurück.</summary>
    [Fact]
    public void Ruecknahme_der_Vorlage_fuehrt_zurueck()
    {
        var doc = Dok(Text("Kapitel"));

        var aenderung = TdListEdit.Vorlage(doc, Bei(doc, 0, 0), TdStil.ZurEbene(1)!.Value)!;
        aenderung.Anwenden();
        aenderung.Zuruecknehmen();

        Assert.Null(Absatz(doc, 0).CharFormat.FontSize);
        Assert.Null(Absatz(doc, 0).Format.OutlineLevel);
    }

    // ==================== Die Tabelle selbst ====================

    /// <summary>
    /// <b>Vier Überschriftsebenen, und jede genau einmal.</b> Zwei Vorlagen mit derselben Ebene
    /// hießen: Das Inhaltsverzeichnis bekäme zwei verschiedene Aussehen für dieselbe Stufe.
    /// </summary>
    [Fact]
    public void Es_gibt_vier_Ueberschriftsebenen_und_jede_einmal()
    {
        for (int ebene = 1; ebene <= 4; ebene++)
        {
            int stufe = ebene;
            Assert.Single(TdStil.Alle, s => s.Heading == stufe);
        }

        Assert.Null(TdStil.ZurEbene(0));
        Assert.Null(TdStil.ZurEbene(5));
    }

    /// <summary>
    /// Die Überschriften werden nach oben hin **größer** — eine Stufe, die kleiner wäre als die
    /// darunter, wäre keine Hierarchie mehr.
    /// </summary>
    [Fact]
    public void Ueberschriften_werden_nach_oben_hin_groesser()
    {
        for (int ebene = 1; ebene < 4; ebene++)
            Assert.True(TdStil.ZurEbene(ebene)!.Value.SizePt > TdStil.ZurEbene(ebene + 1)!.Value.SizePt);

        Assert.True(TdStil.ZurEbene(4)!.Value.SizePt > TdStil.KoerperPt);
    }

    /// <summary>
    /// <see cref="TdStil.Passt"/> erkennt jede Vorlage an ihren eigenen Werten wieder —
    /// <b>sonst zeigte die Vorlagenliste nach dem Setzen etwas anderes an, als gesetzt wurde.</b>
    /// </summary>
    [Fact]
    public void Jede_Vorlage_erkennt_sich_selbst_wieder()
    {
        foreach (var stil in TdStil.Alle)
        {
            var aufgeloest = new TdCharFormat
            {
                FontSize = stil.SizePt,
                Bold = stil.Bold,
                Italic = stil.Italic,
            };

            Assert.True(stil.Passt(aufgeloest), stil.Name);
        }
    }

    /// <summary>Namen sind eindeutig — sie dienen dem Vergleich und nicht der Anzeige.</summary>
    [Fact]
    public void Die_Namen_sind_eindeutig()
    {
        Assert.Equal(TdStil.Alle.Count, TdStil.Alle.Select(s => s.Name).Distinct().Count());
        Assert.Equal(TdStil.Alle.Count, TdStil.Alle.Select(s => s.Key).Distinct().Count());
    }
}
