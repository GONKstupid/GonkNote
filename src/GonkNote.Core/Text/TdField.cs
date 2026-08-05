using System.Text;
using System.Text.Json.Serialization;

namespace GonkNote.Core.Text;

/// <summary>
/// Die Art eines Feldes.
///
/// <para>
/// <b>Die Zahlenwerte sind Datenformat</b> — Enums gehen wie überall im Modell als Zahl in die
/// Datei (so hält es auch <c>TdAlign</c> und <c>TdListMarker</c>). Neue Arten gehören deshalb
/// **hinten** angehängt; wer dazwischenschiebt, verschiebt alle folgenden und macht damit aus
/// jedem gespeicherten Datumsfeld einen Titel.
/// </para>
/// </summary>
public enum TdFieldKind
{
    /// <summary>Die Nummer der Seite, auf der das Feld steht (Word: <c>PAGE</c>).</summary>
    PageNumber,

    /// <summary>Die Gesamtzahl der Seiten (Word: <c>NUMPAGES</c>).</summary>
    PageCount,

    /// <summary>Das Datum (Word: <c>DATE</c>).</summary>
    Date,

    /// <summary>Der Titel des Dokuments (Word: <c>TITLE</c>).</summary>
    Title,

    /// <summary>Das Inhaltsverzeichnis (Word: <c>TOC</c>).</summary>
    TableOfContents,
}

/// <summary>
/// Ein Feld: eine Stelle im Text, deren Inhalt **gerechnet** und nicht geschrieben wird.
///
/// <para>
/// <b>Das ist dasselbe Muster wie bei der Listennummer</b> (§4.17): Die Seitenzahl hängt nicht
/// vom Feld ab, sondern davon, wo es nach dem Umbruch landet; die Seitenanzahl hängt vom
/// ganzen Dokument ab; das Inhaltsverzeichnis hängt von jeder Überschrift darin ab.
/// Gespeichert wäre jeder dieser Werte bei der nächsten Änderung falsch — und zwar still, denn
/// eine falsche Seitenzahl sieht wie eine Seitenzahl aus. Deshalb steht hier nur, **welche
/// Art** von Feld es ist; den Wert liefert <see cref="TdFieldValues"/> beim Umbruch.
/// </para>
///
/// <para>
/// <b>Ein Feld hat deshalb keinen Klartext.</b> <see cref="PlainText"/> gibt „" zurück: Der
/// Klartext ist die Sicht auf das, was **im Dokument steht**, und im Dokument steht ein Feld
/// und keine Zahl. Wer den gerechneten Wert braucht, hat ihn im gesetzten Ergebnis
/// (<see cref="TdLaidOutRun.Field"/>).
/// </para>
/// </summary>
public sealed class TdField : TdInline
{
    public TdFieldKind Kind { get; set; }

    /// <summary>
    /// Die eine Zusatzangabe des Feldes; <c>null</c> = die Vorgabe seiner Art.
    ///
    /// <list type="bullet">
    ///   <item><see cref="TdFieldKind.Date"/> — das Muster, z. B. <c>"dd.MM.yyyy"</c>.</item>
    ///   <item><see cref="TdFieldKind.TableOfContents"/> — die Ebenen, z. B. <c>"1-3"</c>.</item>
    ///   <item>alle übrigen Arten haben keine.</item>
    /// </list>
    ///
    /// <para>
    /// <b>Ein Feld statt eines je Art:</b> In DOCX ist das ebenfalls **eine** Angabe je Feld
    /// (<c>DATE \@ "dd.MM.yyyy"</c>, <c>TOC \o "1-3"</c>), und sie geht so unverändert hin und
    /// zurück, statt bei jedem Export nachgebaut zu werden — dieselbe Überlegung wie bei
    /// <see cref="TdListLevel.Text"/>.
    /// </para>
    /// </summary>
    public string? Argument { get; set; }

    public TdField() { }

    public TdField(TdFieldKind kind, string? argument = null)
    {
        Kind = kind;
        Argument = argument;
    }

    /// <inheritdoc/>
    public override string PlainText() => "";

    // ---------------------------------------------------------------- Platzhalter

    /// <summary>
    /// Die Platzhalter, die Kopf- und Fußzeile benutzen (<c>{SEITE}</c>, <c>{SEITEN}</c>,
    /// <c>{DATUM}</c>, <c>{TITEL}</c>).
    ///
    /// <para>
    /// <b>Diese Tabelle ist die einzige Wahrheit darüber, welcher Platzhalter welches Feld
    /// meint.</b> Sie stand bis Schritt 5 zweimal da — einmal im heutigen Editor und einmal im
    /// DOCX-Schreiber —, und zwei Tabellen für dieselbe Zuordnung driften auseinander (§4.10).
    /// Der Text ist dabei **deutsch und bleibt es**: er steht in Bestandsdokumenten in
    /// <c>TextDoc.HeaderText</c>, ist also Datenformat und keine Übersetzung.
    /// </para>
    /// </summary>
    public static IReadOnlyList<(string Platzhalter, TdFieldKind Art)> Platzhalter { get; } =
    [
        ("{SEITE}", TdFieldKind.PageNumber),
        ("{SEITEN}", TdFieldKind.PageCount),
        ("{DATUM}", TdFieldKind.Date),
        ("{TITEL}", TdFieldKind.Title),
    ];

    /// <summary>Die Feldart zu einem Platzhalter — oder <c>null</c>, wenn es keiner ist.</summary>
    public static TdFieldKind? ArtVonPlatzhalter(string text)
    {
        foreach (var (platzhalter, art) in Platzhalter)
            if (platzhalter == text) return art;
        return null;
    }

    /// <summary>Der Platzhalter zu einer Feldart — oder <c>null</c> (das Inhaltsverzeichnis hat keinen).</summary>
    public static string? PlatzhalterVonArt(TdFieldKind art)
    {
        foreach (var (platzhalter, kind) in Platzhalter)
            if (kind == art) return platzhalter;
        return null;
    }
}

/// <summary>
/// Ein Verweis: ein Stück Text mit einem Ziel.
///
/// <para>
/// <b>Er enthält Stücke und ist nicht selbst eines.</b> In einem Verweis darf ein Wort fett
/// sein, und in DOCX ist <c>w:hyperlink</c> ebenfalls eine Klammer um Läufe — dieselbe
/// Entscheidung wie bei der Tabellenzelle, die Blöcke enthält und nicht Text (§4.18). Wer den
/// Verweis zu einem Lauf mit einem Zusatzfeld machte, könnte einen zweiteilig ausgezeichneten
/// Linktext nicht ablegen.
/// </para>
///
/// <para>
/// <b>Und deshalb gilt hier die Erbfolge aus §7:</b> <c>Hyperlink</c> ist im
/// <c>FlowDocument</c> ein <c>Span</c>, und wer den allgemeineren Fall zuerst behandelt,
/// verliert das Ziel und merkt es nicht. Jede Stelle, die über Stücke läuft, muss den Verweis
/// **vor** dem Lauf abfragen.
/// </para>
/// </summary>
public sealed class TdHyperlink : TdInline
{
    private string _target = "";

    /// <summary>
    /// Das Ziel — **wörtlich so, wie es hereinkam**.
    ///
    /// <para>
    /// <b>Eine Zeichenkette und kein <c>Uri</c></b>, und das ist die Entscheidung, an der der
    /// Markdown-Export schon einmal gescheitert ist (§7 „Markdown-Export"): Ein <c>Uri</c>
    /// vereinheitlicht, und aus dem relativen Ziel <c>kapitel-2.md</c> wird beim
    /// nächsten Schreiben ein absoluter <c>file:///</c>-Pfad. Ein relativer Verweis bleibt
    /// relativ — auch dann, wenn ein fremdes Format ihn gern absolut hätte.
    /// </para>
    /// <para>
    /// Ein Ziel, das mit <c>#</c> beginnt, zeigt **in dasselbe Dokument** (eine Textmarke). In
    /// DOCX ist das kein Verweis auf eine Datei, sondern ein <c>w:anchor</c>.
    /// </para>
    /// </summary>
    public string Target { get => _target; set => _target = value ?? ""; }

    /// <summary>Der Linktext, in Stücken.</summary>
    public List<TdInline> Inlines { get; set; } = new();

    /// <summary>Zeigt der Verweis in dasselbe Dokument?</summary>
    [JsonIgnore]
    public bool IstTextmarke => Target.StartsWith('#');

    public TdHyperlink() { }

    public TdHyperlink(string target, params TdInline[] inlines)
    {
        Target = target;
        Inlines.AddRange(inlines);
    }

    /// <summary>Ein Verweis mit schlichtem Text — der Normalfall.</summary>
    public static TdHyperlink Text(string target, string text) =>
        new(target, new TdRun(text));

    public override string PlainText()
    {
        if (Inlines.Count == 0) return "";
        if (Inlines.Count == 1) return Inlines[0].PlainText();

        var sb = new StringBuilder();
        foreach (var i in Inlines) sb.Append(i.PlainText());
        return sb.ToString();
    }
}

/// <summary>
/// Was ein Feld braucht, das nicht im Dokument steht: Datum und Titel.
///
/// <para>
/// <b>Core fragt die Uhr nicht selbst</b> — aus demselben Grund, aus dem es die Schrift nicht
/// selbst misst (<see cref="ITdTextMeasure"/>, §4.16). Ein <c>DateTime.Now</c> mitten in der
/// Rechnung machte jeden Wächter davon abhängig, wann er läuft, und jeden Golden-File einen
/// Tag später falsch (§7, „Nie Zeit oder Zufall in einer Fixture"). Beides kommt deshalb von
/// außen herein, und wer nichts hereingibt, bekommt <see cref="Ohne"/>: leere Werte statt
/// erfundener.
/// </para>
/// </summary>
public sealed class TdFieldContext
{
    /// <summary>Das Datum für <see cref="TdFieldKind.Date"/>; <c>null</c> = keines bekannt.</summary>
    public DateTime? Date { get; init; }

    /// <summary>Der Titel für <see cref="TdFieldKind.Title"/>.</summary>
    public string Title { get; init; } = "";

    /// <summary>Kein Datum, kein Titel — die Vorgabe, wenn niemand etwas hereingibt.</summary>
    public static TdFieldContext Ohne { get; } = new();
}

/// <summary>
/// Rechnet aus, was in einem Feld steht.
///
/// <para>
/// <b>Warum das eine eigene Rechnung ist</b>, steht bei <see cref="TdField"/>: der Wert hängt
/// nicht vom Feld ab, sondern von seiner Umgebung. Hier kommt beides zusammen — die Werte von
/// außen (<see cref="TdFieldContext"/>) und die aus dem Umbruch (Seitenzahl und
/// Seitenanzahl).
/// </para>
/// </summary>
public static class TdFieldValues
{
    /// <summary>
    /// Das Muster, in dem ein Datumsfeld ohne eigene Angabe steht.
    ///
    /// <para>
    /// <b>Fest und nicht aus der Kultur des Rechners.</b> Ein Dokument muss auf jedem System
    /// dasselbe Datum zeigen — dieselbe Überlegung, aus der der Umbruch hinter einer
    /// Schriftmessung sitzt und nicht auf die installierten Schriften baut (§4.16). Wer ein
    /// anderes Muster will, schreibt es an das Feld; dann steht es **im Dokument** und wandert
    /// mit ihm.
    /// </para>
    /// </summary>
    public const string DatumsmusterStandard = "dd.MM.yyyy";

    /// <summary>
    /// Der Text eines Feldes.
    /// </summary>
    /// <param name="seite">Die Nummer der Seite, auf der das Feld steht.</param>
    /// <param name="seiten">
    /// Die Gesamtzahl der Seiten, oder <c>null</c>, solange sie noch nicht feststeht — im
    /// ersten Durchgang des Umbruchs ist das der Normalfall (§4.20).
    /// </param>
    public static string Text(TdField feld, TdFieldContext kontext, int seite, int? seiten) =>
        feld.Kind switch
        {
            TdFieldKind.PageNumber => seite.ToString(),
            TdFieldKind.PageCount => seiten?.ToString() ?? "",
            TdFieldKind.Date => kontext.Date is { } d
                ? d.ToString(feld.Argument ?? DatumsmusterStandard, System.Globalization.CultureInfo.InvariantCulture)
                : "",
            TdFieldKind.Title => kontext.Title,

            // **Das Inhaltsverzeichnis ist kein Textstück, sondern Absätze.** Es steht in der
            // Zeile nicht als Wort da, sondern ersetzt sie — der Umbruch setzt dafür die
            // Einträge aus TdToc. Ein Text hier wäre eine zweite, kürzere Wahrheit darüber,
            // wie ein Verzeichnis aussieht.
            _ => "",
        };
}
