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
/// <b>Reserviert für spätere Schritte, damit niemand die Namen zweimal vergibt:</b>
/// „hyperlink" und „field" (Schritt 5, Felder/TOC), „image" (Bilder im Fließtext).
/// Ergänzen ist unbedenklich — eine Datei, die einen Namen nicht enthält, stört sich nicht
/// daran. Umbenennen ist es nicht.
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(TdRun), "run")]
[JsonDerivedType(typeof(TdLineBreak), "break")]
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
/// <b>Reserviert für spätere Schritte:</b> „list" (Schritt 3), „table" (Schritt 4),
/// „image" und „chart" (Schritt 6).
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(TdParagraph), "p")]
[JsonDerivedType(typeof(TdPageBreak), "pagebreak")]
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

    /// <summary>Alle Absätze der Reihe nach — der häufigste Durchlauf über ein Dokument.</summary>
    public IEnumerable<TdParagraph> Paragraphs()
    {
        foreach (var b in Blocks())
            if (b is TdParagraph p) yield return p;
    }
}
