using System.Text;
using System.Text.Json.Serialization;

namespace GonkNote.Core.Text;

// ==================== Textstücke ====================

/// <summary>
/// Ein Textstück innerhalb eines Absatzes.
///
/// <para>
/// <b>Die Zeichenketten in den <c>JsonDerivedType</c>-Angaben sind Datenformat, kein
/// Codedetail</b> — dieselbe Regel wie bei <c>WbElement</c> (HANDOFF §7). Sie stehen wörtlich
/// in jedem gespeicherten Dokument; wer eine ändert, macht die betroffenen Stücke unlesbar,
/// und das Fehlerbild ist ein leerer Absatz und kein Absturz.
/// </para>
///
/// <para>
/// <b>Sie sind hier bewusst kurz</b> („run", „break") und nicht in der Form
/// „Namensraum.Typ, Assembly" wie bei <c>WbElement</c>. Dort ist die lange Form ein Erbe:
/// LiteDB hat sie so geschrieben, und Bestandsdaten hängen daran. Dieses Format ist neu und
/// hat kein solches Erbe — es gibt keinen Grund, einen Assemblynamen in jede Datei zu
/// schreiben, und schon gar keinen, ihn dadurch unveränderlich zu machen.
/// </para>
///
/// <para>
/// <b>Alle für Phase 4 vorgemerkten Namen sind vergeben:</b> „hyperlink" und „field" mit
/// Schritt 5, „image" und „chart" mit Schritt 6. Ergänzen ist unbedenklich — eine Datei, die
/// einen Namen nicht enthält, stört sich nicht daran. Umbenennen ist es nicht.
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(TdRun), "run")]
[JsonDerivedType(typeof(TdLineBreak), "break")]
[JsonDerivedType(typeof(TdField), "field")]
[JsonDerivedType(typeof(TdHyperlink), "hyperlink")]
[JsonDerivedType(typeof(TdImage), "image")]
[JsonDerivedType(typeof(TdChart), "chart")]
public abstract class TdInline
{
    /// <summary>Zeichenformat dieses Stücks, als **Abweichung** vom Absatz. Nie <c>null</c>.</summary>
    public TdCharFormat Format { get; set; } = new();

    /// <summary>
    /// Die Zeichen, die dieses Stück zum Klartext beisteuert — für Wortzähler, Suche und den
    /// Markdown-Export. **Eine Methode und keine Eigenschaft**, damit kein Serialisierer auf
    /// den Gedanken kommt, sie sei zu speichern: ein abgeleiteter Wert in der Datei sähe
    /// später wie gespeicherte Wahrheit aus (HANDOFF §7).
    /// </summary>
    public abstract string PlainText();

    /// <summary>
    /// Dasselbe Stück mit einem anderen Zeichenformat — <b>als Kopie</b>.
    ///
    /// <para>
    /// <b>Das ist der Grund, warum es diese Methode gibt und nicht einfach
    /// <c>stueck.Format = neu</c> heißt</b> (§4.32, „Absätze und Stücke werden nie verändert,
    /// sondern ersetzt"): <see cref="TdChange"/> hält als Sicherung die Absätze, wie sie waren —
    /// samt ihrer Stücke. Ein an Ort und Stelle umgestelltes Format änderte die Sicherung mit,
    /// und ein Strg+Z holte den fetten Text als fetten Text zurück. Der Fehler sähe nicht nach
    /// Formatieren aus, sondern nach kaputtem Rückgängig.
    /// </para>
    /// <para>
    /// <b>Flach kopiert.</b> Was ein Stück sonst noch hält — die Kennung eines Bildes, die
    /// Werte eines Diagramms —, wird dabei geteilt und nicht verdoppelt. Das trägt, solange
    /// diese Dinge nach demselben Grundsatz behandelt werden: geändert wird, indem ersetzt
    /// wird. <see cref="TdHyperlink"/> hat eine veränderliche Stückliste und wird deshalb
    /// nicht über diesen Weg umformatiert, sondern in <see cref="TdFormatEdit"/> eigens
    /// nachgebaut.
    /// </para>
    /// </summary>
    public TdInline MitFormat(TdCharFormat format)
    {
        var kopie = (TdInline)MemberwiseClone();
        kopie.Format = format;
        return kopie;
    }
}

/// <summary>Ein Stück Text mit einheitlichem Zeichenformat.</summary>
public sealed class TdRun : TdInline
{
    private string _text = "";

    /// <summary>
    /// Der Text. **Nie <c>null</c>** — dieselbe Vorsorge wie bei den String-Feldern von
    /// <c>TextDoc</c>: der Wächter dafür ist ein Test und kein gutes Gedächtnis.
    /// <para>
    /// Der Json-Name ist kurz („s"), weil dieses Feld das mit Abstand häufigste des Formats
    /// ist — dieselbe Überlegung, aus der <c>WbPoint</c> seine Felder kurz hält.
    /// </para>
    /// </summary>
    [JsonPropertyName("s")]
    public string Text
    {
        get => _text;
        set => _text = value ?? "";
    }

    public TdRun() { }

    public TdRun(string text) => Text = text;

    public TdRun(string text, TdCharFormat format)
    {
        Text = text;
        Format = format;
    }

    public override string PlainText() => Text;
}

/// <summary>
/// Ein Zeilenumbruch **innerhalb** eines Absatzes (Umschalt+Eingabe) — nicht zu verwechseln
/// mit dem Absatzende. Der Unterschied ist im Export sichtbar: Word schreibt dafür
/// <c>&lt;w:br/&gt;</c> und keinen neuen Absatz, und die Absatzabstände gelten weiter.
/// </summary>
public sealed class TdLineBreak : TdInline
{
    public override string PlainText() => "\n";
}

// ==================== Blöcke ====================

/// <summary>
/// Ein Block auf oberster Ebene des Dokuments.
/// <inheritdoc cref="TdInline" path="/para[1]"/>
///
/// <para>
/// <b>Drei reservierte Namen sind frei geblieben, und zwar mit Absicht:</b> „list", weil ein
/// Listenpunkt ein Absatz mit einer Angabe ist und kein eigener Blocktyp (§4.17); „image" und
/// „chart", weil eine Zeichnung in DOCX immer in einem Lauf steht und ein bildbreites Foto
/// deshalb ein Absatz ist, der nichts als dieses Bild enthält (§4.21). Sie stehen auf
/// <see cref="TdInline"/>.
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(TdParagraph), "p")]
[JsonDerivedType(typeof(TdPageBreak), "pagebreak")]
[JsonDerivedType(typeof(TdTable), "table")]
public abstract class TdBlock
{
    /// <inheritdoc cref="TdInline.PlainText"/>
    public abstract string PlainText();
}

/// <summary>
/// Ein Absatz: eine Folge von Textstücken mit einem gemeinsamen Absatzformat.
/// <para>
/// <b>Der Absatz ist die Einheit, an der das Layout umbricht</b>, und die einzige, die es in
/// Schritt 1 gibt. Alles andere (Listen, Tabellen, Bilder) kommt in der Reihenfolge aus
/// Roadmap §5 dazu.
/// </para>
/// </summary>
public sealed class TdParagraph : TdBlock
{
    public List<TdInline> Inlines { get; set; } = new();

    /// <summary>Absatzformat als **Abweichung** vom Dokumentstandard. Nie <c>null</c>.</summary>
    public TdParaFormat Format { get; set; } = new();

    /// <summary>
    /// Zeichenformat, das für den **ganzen** Absatz gilt und unter dem der einzelnen Stücke
    /// liegt. Hier steht, was eine Überschrift ausmacht: Schrift und Größe einmal am Absatz
    /// statt an jedem Lauf — nur so überlebt ein fett gemachtes Wort darin eine spätere
    /// Änderung der Überschrift.
    /// </summary>
    public TdCharFormat CharFormat { get; set; } = new();

    /// <summary>
    /// Gehört dieser Absatz zu einer Liste, und zu welcher Ebene? <c>null</c> = kein
    /// Listenpunkt.
    ///
    /// <para>
    /// <b>Ein Listenpunkt ist ein Absatz mit einer Angabe — und kein eigener Blocktyp.</b>
    /// Das ist die Entscheidung von Schritt 3, und sie folgt DOCX: dort trägt der Absatz ein
    /// <c>w:numPr</c>, und die Liste selbst ist nur eine Definition anderswo. Der Gewinn ist
    /// die Bearbeitung: Eingabe drücken, Ebene wechseln, einen Punkt aus der Liste
    /// herausnehmen — alles bleibt eine Absatzänderung. Ein Listenblock mit Punkten darin
    /// (wie ihn <c>MdList</c> für Markdown hat, wo nur gelesen wird) müsste bei jedem dieser
    /// Handgriffe einen Baum umbauen.
    /// </para>
    /// <para>
    /// <b>Die Nummer steht hier bewusst nicht.</b> Sie hängt nicht vom Absatz ab, sondern
    /// davon, was vor ihm kommt — gerechnet wird sie in <see cref="TdListNumbering"/>.
    /// </para>
    /// </summary>
    public TdListRef? List { get; set; }

    [JsonConstructor]
    public TdParagraph() { }

    public TdParagraph(string text) => Inlines.Add(new TdRun(text));

    public TdParagraph(IEnumerable<TdInline> inlines) => Inlines.AddRange(inlines);

    public override string PlainText()
    {
        // Ein Absatz mit einem Stück ist der Normalfall — dafür lohnt kein StringBuilder.
        if (Inlines.Count == 0) return "";
        if (Inlines.Count == 1) return Inlines[0].PlainText();

        var sb = new StringBuilder();
        foreach (var i in Inlines) sb.Append(i.PlainText());
        return sb.ToString();
    }

    /// <summary>
    /// Das aufgelöste Zeichenformat eines Stücks: Stück über Absatz über
    /// <see cref="TdCharFormat.Standard"/>. **Ohne den Dokumentstandard** — den kennt nur
    /// <see cref="TdDocument.FormatVon(TdParagraph, TdInline)"/>, und wer ihn braucht,
    /// fragt dort.
    /// </summary>
    public TdCharFormat FormatVon(TdInline inline) =>
        inline.Format.Over(CharFormat).Aufgeloest();

    /// <summary>
    /// Die Stücke des Absatzes, **flach** — ein Verweis erscheint nicht selbst, sondern seine
    /// Stücke, jedes mit dem Verweis daneben.
    ///
    /// <para>
    /// <b>Das ist die Erbfolge aus §7, nur andersherum aufgelöst.</b> Dort war die Falle, den
    /// allgemeineren Fall (<c>Span</c>) vor dem besonderen (<c>Hyperlink</c>) zu behandeln und
    /// dabei das Ziel zu verlieren. Wer hier über <see cref="Inlines"/> läuft, statt über
    /// diesen Durchlauf, macht denselben Fehler mit umgekehrtem Vorzeichen: Er sieht den
    /// Verweis und **nicht seinen Text** — die Zeile bliebe leer, und niemand bekäme eine
    /// Fehlermeldung.
    /// </para>
    /// <para>
    /// Ein Verweis in einem Verweis kommt nicht vor und wird deshalb nicht gesucht; ein
    /// verschachtelter käme mit seinem äußeren Ziel heraus.
    /// </para>
    /// </summary>
    public IEnumerable<(TdInline Stueck, TdHyperlink? Verweis)> FlacheStuecke()
    {
        foreach (var inline in Inlines)
        {
            if (inline is TdHyperlink verweis)
            {
                foreach (var innen in verweis.Inlines)
                    yield return (innen, verweis);
            }
            else
            {
                yield return (inline, null);
            }
        }
    }
}

/// <summary>
/// Ein erzwungener Seitenumbruch.
/// <para>
/// Er steht schon in Schritt 1 im Modell, obwohl das Seitenlayout erst Schritt 2 ist: der
/// heutige Editor kann ihn einfügen, ein Bestandsdokument kann ihn also enthalten, und ein
/// Modell, das ihn nicht kennt, würde ihn bei der Übernahme still verschlucken. **Ein
/// verlorener Seitenumbruch fällt erst beim Drucken auf.**
/// </para>
/// </summary>
public sealed class TdPageBreak : TdBlock
{
    public override string PlainText() => "";
}

// ==================== Dokument ====================

/// <summary>
/// Der Inhalt eines Textdokuments — das eigene Modell aus Roadmap Phase 4, das an die Stelle
/// des <c>FlowDocument</c> tritt.
///
/// <para>
/// <b>Warum überhaupt ein eigenes Modell.</b> Der Inhalt liegt heute als <c>TextDoc.Rtf</c>
/// vor: RTF oder ein WPF-<c>XamlPackage</c>, beides erzeugt von
/// <c>System.Windows.Documents.TextRange</c>. Das ist kein Format, das der Linux- oder der
/// iPad-Kopf lesen kann — es ist Windows, in Bytes gegossen. Solange es das Speicherformat
/// ist, bleiben Textdokumente unter Linux ausgegraut, egal wie gut die Oberfläche wird.
/// </para>
///
/// <para>
/// <b>Das Modell zeichnet kein Pixel</b> und steht deshalb nach der Faustregel aus HANDOFF §3
/// in Core — genau wie <c>Markdown</c> nebenan.
/// </para>
/// </summary>
public sealed class TdDocument
{
    /// <summary>
    /// Die Version des Formats. Steht von Anfang an in der Datei, damit ein späterer
    /// Formatwechsel nicht raten muss — und weil ein Feld, das erst nachträglich eingeführt
    /// wird, für alle Bestandsdateien fehlt.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Die Abschnitte des Dokuments — mindestens einer. Jeder trägt seine eigene
    /// Seiteneinrichtung (Schritt 2, <see cref="TdSection"/>).
    /// </summary>
    public List<TdSection> Sections { get; set; } = new();

    /// <summary>
    /// Grundformate des Dokuments. Sie liegen unter allem, was Absätze und Stücke setzen,
    /// und über <see cref="TdCharFormat.Standard"/>.
    /// </summary>
    public TdCharFormat DefaultCharFormat { get; set; } = new();

    /// <inheritdoc cref="DefaultCharFormat"/>
    public TdParaFormat DefaultParaFormat { get; set; } = new();

    /// <summary>
    /// Die Listendefinitionen des Dokuments. Absätze verweisen über
    /// <see cref="TdParagraph.List"/> darauf — eine Liste ist keine Klammer um ihre Punkte,
    /// sondern eine Vorlage, auf die sie zeigen (Schritt 3, §4.17).
    /// </summary>
    public List<TdListDefinition> Lists { get; set; } = new();

    /// <summary>
    /// Die nächste freie Listenkennung. **Zwei Listen dürfen sich keine Kennung teilen** —
    /// sonst zählte die zweite dort weiter, wo die erste aufgehört hat.
    /// </summary>
    public int NextListId() => Lists.Count == 0 ? 1 : Lists.Max(l => l.Id) + 1;

    /// <summary>
    /// Ein leeres Dokument ist **nicht** leer, sondern hat einen Abschnitt mit einem leeren
    /// Absatz — sonst hätte der Cursor beim ersten Tastendruck keinen Ort.
    /// </summary>
    public static TdDocument Leer() =>
        new() { Sections = { new TdSection(new TdParagraph()) } };

    /// <summary>
    /// Alle Blöcke über alle Abschnitte hinweg, der Reihe nach. Für alles, was die
    /// Seiteneinrichtung nicht interessiert — Wortzähler, Suche, Markdown-Export.
    /// </summary>
    public IEnumerable<TdBlock> Blocks()
    {
        foreach (var abschnitt in Sections)
            foreach (var block in abschnitt.Blocks)
                yield return block;
    }

    /// <summary>Der ganze Text, Absätze durch Zeilenumbruch getrennt.</summary>
    public string PlainText()
    {
        var sb = new StringBuilder();
        foreach (var b in Blocks())
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(b.PlainText());
        }
        return sb.ToString();
    }

    /// <summary>
    /// Wortzahl. **Zählt über Absatzgrenzen hinweg nicht zusammen** — „Ende" und „Anfang" in
    /// zwei Absätzen sind zwei Wörter, auch ohne Leerzeichen dazwischen. Genau deshalb steht
    /// hier <see cref="PlainText"/> mit seinen eingefügten Zeilenumbrüchen und nicht ein
    /// aneinandergehängter Lauftext.
    /// </summary>
    public int WordCount()
    {
        int n = 0;
        bool imWort = false;
        foreach (char c in PlainText())
        {
            if (char.IsWhiteSpace(c)) imWort = false;
            else if (!imWort) { imWort = true; n++; }
        }
        return n;
    }

    /// <summary>
    /// Das aufgelöste Zeichenformat eines Stücks, mit dem Dokumentstandard dazwischen:
    /// Stück → Absatz → Dokument → <see cref="TdCharFormat.Standard"/>.
    /// </summary>
    public TdCharFormat FormatVon(TdParagraph absatz, TdInline inline) =>
        inline.Format.Over(absatz.CharFormat).Over(DefaultCharFormat).Aufgeloest();

    /// <summary>Das aufgelöste Absatzformat: Absatz → Dokument → Standard.</summary>
    public TdParaFormat FormatVon(TdParagraph absatz) =>
        absatz.Format.Over(DefaultParaFormat).Aufgeloest();

    /// <summary>
    /// Alle Absätze der Reihe nach — der häufigste Durchlauf über ein Dokument.
    ///
    /// <para>
    /// <b>Er steigt in Tabellenzellen ab</b>, und das ist keine Bequemlichkeit: Ein
    /// Listenpunkt in einer Zelle ist ein Listenpunkt, und die Nummerierung
    /// (<see cref="TdListNumbering"/>) läuft über genau diesen Durchlauf. Ohne den Abstieg
    /// bekäme er keine Nummer — und das Fehlerbild wäre ein Aufzählungspunkt ohne Marke,
    /// nicht ein Absturz.
    /// </para>
    /// </summary>
    public IEnumerable<TdParagraph> Paragraphs()
    {
        foreach (var b in Blocks())
            foreach (var p in AbsaetzeIn(b))
                yield return p;
    }

    /// <summary>
    /// Die Kennungen aller Bilder, die dieses Dokument benutzt — für den Aufräumlauf
    /// verwaister Blobs.
    /// <para>
    /// <b>Er muss vollständig sein, und zwar in die falsche Richtung:</b> Eine Kennung zu
    /// viel kostet Platz, eine zu wenig löscht ein Bild, das noch gebraucht wird. Deshalb
    /// läuft er über <see cref="Paragraphs"/> — der steigt in Tabellenzellen ab (§4.19) — und
    /// über <see cref="TdParagraph.FlacheStuecke"/>, der in Verweise hineingeht (§4.20).
    /// </para>
    /// </summary>
    public IEnumerable<Guid> UsedImages()
    {
        foreach (var absatz in Paragraphs())
            foreach (var (stueck, _) in absatz.FlacheStuecke())
                if (stueck is TdImage bild) yield return bild.BlobId;
    }

    /// <summary>Die Absätze eines Blocks, Tabellen eingeschlossen — auch verschachtelte.</summary>
    private static IEnumerable<TdParagraph> AbsaetzeIn(TdBlock block)
    {
        switch (block)
        {
            case TdParagraph p:
                yield return p;
                break;

            case TdTable t:
                foreach (var zeile in t.Rows)
                    foreach (var zelle in zeile.Cells)
                        foreach (var innen in zelle.Blocks)
                            foreach (var p in AbsaetzeIn(innen))
                                yield return p;
                break;
        }
    }
}
