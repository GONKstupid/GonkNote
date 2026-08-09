using System.Text.Json.Serialization;

namespace GonkNote.Core.Text;

/// <summary>Waagerechte Ausrichtung eines Absatzes.</summary>
public enum TdAlign
{
    Left,
    Center,
    Right,
    Justify,
}

/// <summary>Hoch- und tiefgestellt.</summary>
public enum TdVerticalAlign
{
    Normal,
    Superscript,
    Subscript,
}

/// <summary>
/// Zeichenformat — Schrift, Größe, Auszeichnung, Farbe.
///
/// <para>
/// <b>Jede Eigenschaft ist absichtlich nullbar, und <c>null</c> heißt „nicht gesetzt", nicht
/// „Standardwert".</b> Das ist der Unterschied, an dem ein Texteditor sonst scheitert: wer
/// in einer Überschrift ein Wort fett macht, setzt genau ein Feld — Schrift und Größe müssen
/// weiter von der Überschrift kommen. Wären die Felder nicht nullbar, trüge jeder Lauf eine
/// vollständige Kopie des Formats mit sich, und eine Änderung an der Überschrift ginge an
/// allen bereits geschriebenen Läufen vorbei.
/// </para>
///
/// <para>
/// Zusammengerechnet wird über <see cref="Over"/> — dasselbe Muster wie
/// <c>ThemeDefinition.Over</c> in <c>Core/Theming</c>: der eigene Wert gewinnt, wo er
/// gesetzt ist, sonst zählt die Unterlage. Am Ende der Kette steht <see cref="Standard"/>,
/// das als einziges überall einen Wert hat — <see cref="Aufgeloest"/> kann deshalb nie
/// <c>null</c> zurückgeben.
/// </para>
/// </summary>
public sealed class TdCharFormat
{
    /// <summary>Schriftfamilie, z. B. „Segoe UI". <c>null</c> = nicht gesetzt.</summary>
    public string? FontFamily { get; set; }

    /// <summary>Schriftgrad in **Punkt** (nicht Pixel) — die Einheit, die DOCX benutzt.</summary>
    public double? FontSize { get; set; }

    public bool? Bold { get; set; }
    public bool? Italic { get; set; }
    public bool? Underline { get; set; }
    public bool? Strikethrough { get; set; }

    /// <summary>Schriftfarbe als „#RRGGBB".</summary>
    public string? Color { get; set; }

    /// <summary>Texthervorhebung als „#RRGGBB"; <c>null</c> = nicht gesetzt, "" = keine.</summary>
    public string? Highlight { get; set; }

    public TdVerticalAlign? VerticalAlign { get; set; }

    /// <summary>
    /// Das Format, an dem die Kaskade endet. **Als einziges überall belegt** — es ist die
    /// Unterlage aller anderen, nicht selbst eine Schicht darüber.
    /// <para>
    /// Die Werte entsprechen dem, was der WPF-Editor heute als Grundschrift benutzt
    /// (<c>TextStyles</c>). Die Farbe steht bewusst als <c>null</c>-freies Schwarz da und
    /// nicht als Theme-Farbe: **ein Dokument ist Papier** (HANDOFF §1, „Dark/Light bei hellem
    /// Papier"), und was hier steht, geht später so in den Export.
    /// </para>
    /// </summary>
    public static TdCharFormat Standard => new()
    {
        FontFamily = "Segoe UI",
        FontSize = 11,
        Bold = false,
        Italic = false,
        Underline = false,
        Strikethrough = false,
        Color = "#000000",
        Highlight = "",
        VerticalAlign = TdVerticalAlign.Normal,
    };

    /// <summary>
    /// Legt dieses Format über <paramref name="unterlage"/>: eigene gesetzte Werte gewinnen,
    /// alles Übrige kommt von unten. Beide Seiten bleiben unverändert.
    /// </summary>
    public TdCharFormat Over(TdCharFormat unterlage) => new()
    {
        FontFamily = FontFamily ?? unterlage.FontFamily,
        FontSize = FontSize ?? unterlage.FontSize,
        Bold = Bold ?? unterlage.Bold,
        Italic = Italic ?? unterlage.Italic,
        Underline = Underline ?? unterlage.Underline,
        Strikethrough = Strikethrough ?? unterlage.Strikethrough,
        Color = Color ?? unterlage.Color,
        Highlight = Highlight ?? unterlage.Highlight,
        VerticalAlign = VerticalAlign ?? unterlage.VerticalAlign,
    };

    /// <summary>
    /// Dieses Format über <see cref="Standard"/> — das Ergebnis ist lückenlos und damit das,
    /// was Layout und Export tatsächlich brauchen.
    /// </summary>
    public TdCharFormat Aufgeloest() => Over(Standard);

    /// <summary>
    /// Der Gegenweg zu <see cref="Over"/>: Was <paramref name="unterlage"/> ohnehin sagt, wird
    /// zu <c>null</c> — übrig bleibt nur, worin sich dieses Format von ihr unterscheidet.
    /// <para>
    /// <b>Gebraucht beim Lesen von DOCX</b> (§4.23): Dort trägt jeder Lauf das Format seines
    /// Absatzes mit, weil Word das <c>pPr/rPr</c> nur auf die Absatzmarke anwendet. Ohne diesen
    /// Schritt käme aus jeder Word-Datei ein Modell zurück, in dem jeder Lauf eine vollständige
    /// Formatkopie trägt — und damit wäre die Regel aus §4.14 (<c>null</c> heißt „nicht
    /// gesetzt") an der Tür wieder abgegeben: eine spätere Änderung am Absatz ginge an allen
    /// Läufen vorbei.
    /// </para>
    /// </summary>
    public TdCharFormat Ohne(TdCharFormat unterlage) => new()
    {
        FontFamily = FontFamily == unterlage.FontFamily ? null : FontFamily,
        FontSize = FontSize == unterlage.FontSize ? null : FontSize,
        Bold = Bold == unterlage.Bold ? null : Bold,
        Italic = Italic == unterlage.Italic ? null : Italic,
        Underline = Underline == unterlage.Underline ? null : Underline,
        Strikethrough = Strikethrough == unterlage.Strikethrough ? null : Strikethrough,
        Color = Color == unterlage.Color ? null : Color,
        Highlight = Highlight == unterlage.Highlight ? null : Highlight,
        VerticalAlign = VerticalAlign == unterlage.VerticalAlign ? null : VerticalAlign,
    };

    /// <summary>
    /// Ist überhaupt etwas gesetzt? Ein leeres Format wird beim Speichern weggelassen.
    /// <para>
    /// <b><c>JsonIgnore</c>, und das ist kein Schmuck:</b> Ohne es steht in **jedem** Format
    /// jeder Datei ein <c>"IstLeer":false</c> — ein abgeleiteter Wert, der später wie
    /// gespeicherte Wahrheit aussieht (§4.14). Gefunden am laufenden Programm, nicht im Test
    /// (§4.22).
    /// </para>
    /// </summary>
    [JsonIgnore]
    public bool IstLeer =>
        FontFamily is null && FontSize is null && Bold is null && Italic is null &&
        Underline is null && Strikethrough is null && Color is null && Highlight is null &&
        VerticalAlign is null;

    public TdCharFormat Kopie() => (TdCharFormat)MemberwiseClone();
}

/// <summary>
/// Absatzformat — Ausrichtung, Einzüge, Abstände, Gliederungsebene.
/// <para>
/// Dieselbe Regel wie bei <see cref="TdCharFormat"/>: <c>null</c> heißt „nicht gesetzt".
/// </para>
/// <para>
/// <b>Einzüge in Zentimetern, Abstände in Punkt.</b> Das ist keine Inkonsequenz, sondern
/// genau die Aufteilung, die Word und die Lineal-Oberfläche des Editors benutzen — wer sie
/// vereinheitlicht, muss an jedem Eingabefeld umrechnen und bekommt Rundungsreste dort, wo
/// der Nutzer „2 cm" eingetippt hat.
/// </para>
/// </summary>
public sealed class TdParaFormat
{
    public TdAlign? Alignment { get; set; }

    public double? LeftIndentCm { get; set; }
    public double? RightIndentCm { get; set; }

    /// <summary>Erstzeileneinzug; negativ = hängender Einzug.</summary>
    public double? FirstLineIndentCm { get; set; }

    public double? SpaceBeforePt { get; set; }
    public double? SpaceAfterPt { get; set; }

    /// <summary>Zeilenabstand als Vielfaches: 1 = einfach, 1,5 = anderthalb.</summary>
    public double? LineSpacing { get; set; }

    /// <summary>
    /// Gliederungsebene: 0 = Fließtext, 1–6 = Überschrift 1–6. **Das ist die Wahrheit über
    /// „ist das eine Überschrift?"** — nicht die Schriftgröße.
    /// <para>
    /// Der heutige Markdown-Exporter rät sie aus der Größe zurück (<c>HeadingPrefix</c>
    /// rechnet gegen <c>TextStyles</c>), weil das <c>FlowDocument</c> keinen Platz dafür hat.
    /// Wer eine Überschrift kleiner stellt, verliert dort ihre Ebene. Hier steht sie als
    /// eigener Wert, und das Inhaltsverzeichnis (Schritt 5) hat damit von Anfang an eine
    /// verlässliche Quelle.
    /// </para>
    /// </summary>
    public int? OutlineLevel { get; set; }

    /// <summary>
    /// Der Absatz wird <b>als Überschrift gesetzt, gehört aber nicht ins
    /// Inhaltsverzeichnis</b> (§4.23).
    ///
    /// <para>
    /// <b>Es gibt genau zwei davon, und beide braucht man:</b> der <b>Titel</b> eines Dokuments
    /// und die Zeile <b>„Inhaltsverzeichnis"</b> über dem Verzeichnis selbst. Beide sehen aus
    /// wie eine Überschrift und sollen es auch sein — ein Titel muss im Markdown ein <c>#</c>
    /// bekommen, sonst hat eine exportierte Datei keine oberste Überschrift. Im Verzeichnis
    /// stehen dürfen sie trotzdem nicht: ein Verzeichnis, dessen erster Eintrag der
    /// Dokumenttitel ist, und ein zweiter namens „Inhaltsverzeichnis", der auf sich selbst
    /// zeigt.
    /// </para>
    /// <para>
    /// <b>Word trennt das genauso</b> — die eingebauten Vorlagen <c>Title</c> und
    /// <c>TOC Heading</c> bauen auf <c>Heading 1</c> auf und setzen <c>w:outlineLvl</c> auf
    /// Fließtext. Genau diese Form schreibt <see cref="TdDocx"/>, und daraus liest sie den Wert
    /// auch zurück: die Vorlage sagt den Rang, die Gliederungsebene das Verzeichnis.
    /// </para>
    /// </summary>
    public bool? ExcludeFromToc { get; set; }

    /// <summary>Nicht vom nächsten Absatz trennen (Seitenumbruch, Schritt 2).</summary>
    public bool? KeepWithNext { get; set; }

    /// <summary>Seitenumbruch vor diesem Absatz erzwingen (Schritt 2).</summary>
    public bool? PageBreakBefore { get; set; }

    /// <summary>
    /// Die Linie **unter** dem Absatz. <c>null</c> = nicht gesetzt,
    /// <see cref="TdBorder.Keine"/> = ausdrücklich keine — dieselbe Unterscheidung wie bei
    /// <see cref="TdCharFormat.Highlight"/>.
    ///
    /// <para>
    /// <b>Das ist die Trennlinie des Editors</b> („Einfügen → Trennlinie"). Im
    /// <c>FlowDocument</c> ist sie ein <c>BlockUIContainer</c> mit einem 2 px hohen Rahmen —
    /// also ein <b>Bedienelement</b> im Text, und dafür hat kein Dokumentformat einen Ort.
    /// DOCX schreibt sie seit jeher als leeren Absatz mit einer Unterlinie
    /// (<c>w:pBdr/w:bottom</c>), und genau das steht hier: <b>eine Linie ist eine Angabe am
    /// Absatz und kein Block für sich.</b>
    /// </para>
    /// <para>
    /// <b>Warum sie überhaupt ins Modell musste:</b> Ohne sie verschluckt die Übernahme jede
    /// Trennlinie eines Bestandsdokuments — still, denn ein fehlender Strich sieht nicht nach
    /// einem Fehler aus, sondern nach einem Dokument ohne Strich (§7,
    /// „Was noch nicht geht, verschwindet nicht still").
    /// </para>
    /// </summary>
    public TdBorder? BottomBorder { get; set; }

    /// <inheritdoc cref="TdCharFormat.Standard"/>
    public static TdParaFormat Standard => new()
    {
        Alignment = TdAlign.Left,
        LeftIndentCm = 0,
        RightIndentCm = 0,
        FirstLineIndentCm = 0,
        SpaceBeforePt = 0,
        SpaceAfterPt = 8,
        LineSpacing = 1,
        OutlineLevel = 0,
        ExcludeFromToc = false,
        KeepWithNext = false,
        PageBreakBefore = false,
        BottomBorder = TdBorder.Keine,
    };

    /// <inheritdoc cref="TdCharFormat.Over"/>
    public TdParaFormat Over(TdParaFormat unterlage) => new()
    {
        Alignment = Alignment ?? unterlage.Alignment,
        LeftIndentCm = LeftIndentCm ?? unterlage.LeftIndentCm,
        RightIndentCm = RightIndentCm ?? unterlage.RightIndentCm,
        FirstLineIndentCm = FirstLineIndentCm ?? unterlage.FirstLineIndentCm,
        SpaceBeforePt = SpaceBeforePt ?? unterlage.SpaceBeforePt,
        SpaceAfterPt = SpaceAfterPt ?? unterlage.SpaceAfterPt,
        LineSpacing = LineSpacing ?? unterlage.LineSpacing,
        OutlineLevel = OutlineLevel ?? unterlage.OutlineLevel,
        ExcludeFromToc = ExcludeFromToc ?? unterlage.ExcludeFromToc,
        KeepWithNext = KeepWithNext ?? unterlage.KeepWithNext,
        PageBreakBefore = PageBreakBefore ?? unterlage.PageBreakBefore,
        BottomBorder = BottomBorder ?? unterlage.BottomBorder,
    };

    /// <inheritdoc cref="TdCharFormat.Aufgeloest"/>
    public TdParaFormat Aufgeloest() => Over(Standard);

    /// <inheritdoc cref="TdCharFormat.IstLeer"/>
    [JsonIgnore]
    public bool IstLeer =>
        Alignment is null && LeftIndentCm is null && RightIndentCm is null &&
        FirstLineIndentCm is null && SpaceBeforePt is null && SpaceAfterPt is null &&
        LineSpacing is null && OutlineLevel is null && ExcludeFromToc is null &&
        KeepWithNext is null && PageBreakBefore is null && BottomBorder is null;

    public TdParaFormat Kopie() => (TdParaFormat)MemberwiseClone();
}
