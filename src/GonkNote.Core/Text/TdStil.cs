namespace GonkNote.Core.Text;

/// <summary>
/// Eine <b>Absatzvorlage</b> — „Standard", „Überschrift 1", „Zitat".
/// </summary>
/// <param name="Name">Der interne Name; **nicht übersetzt**, er dient dem Vergleich.</param>
/// <param name="Key">Der Übersetzungsschlüssel des Anzeigenamens.</param>
/// <param name="SizePt">Schriftgröße in Punkt.</param>
/// <param name="Bold">Fett?</param>
/// <param name="Italic">Kursiv?</param>
/// <param name="ColorHex">Schriftfarbe „#RRGGBB", <c>null</c> = die des Dokuments.</param>
/// <param name="LeftCm">Linker Einzug in Zentimetern.</param>
/// <param name="RightCm">Rechter Einzug in Zentimetern.</param>
/// <param name="BeforePt">Abstand davor in Punkt.</param>
/// <param name="AfterPt">Abstand danach in Punkt.</param>
/// <param name="Heading">Gliederungsebene 1–4, oder <c>0</c> — <b>daran hängt das
/// Inhaltsverzeichnis</b> (§4.20).</param>
public readonly record struct TdStil(
    string Name, string Key, double SizePt, bool Bold, bool Italic, string? ColorHex,
    double LeftCm, double RightCm, double BeforePt, double AfterPt, int Heading)
{
    /// <summary>
    /// Die zehn mitgelieferten Vorlagen — <b>der einzige Ort, an dem sie stehen</b>.
    ///
    /// <para>
    /// <b>Zum vierten Mal dieselbe Lage nach Farben (§4.9), Schriften (§4.26) und Symbolen
    /// (§4.31):</b> Der WPF-Kopf führte diese Tabelle in <c>TextStyles.All</c>, und der
    /// Linux-Kopf hatte sie gar nicht. Zwei Fassungen derselben Tabelle sind zwei, von denen
    /// später eine jemand ändert — und das Ergebnis wäre **dasselbe Dokument in zwei
    /// Schriftbildern**, je nachdem, welcher Kopf die Überschrift gesetzt hat.
    /// </para>
    /// <para>
    /// <b>Der WPF-Kopf ist trotzdem nicht umgebaut worden</b>, und das ist Absicht: Sein
    /// <c>TextStyles</c> arbeitet auf einem <c>FlowDocument</c> mit WPF-Typen
    /// (<c>FontWeight</c>, <c>Thickness</c>), und ein Umbau im laufenden Kopf hätte keinen
    /// Gegenwert. **Stattdessen hält ein Wächter im WPF-Testprojekt beide Tabellen Zeile für
    /// Zeile aneinander** — dieselbe Lösung wie bei der Farbtabelle (§4.9). Wer hier oder dort
    /// eine Zahl ändert, bekommt einen roten Lauf statt zweier Köpfe, die verschieden aussehen.
    /// </para>
    /// <para>
    /// <b>Die Farben folgen dem Design-Konzept</b> (Blau/Türkis/Lila/Pink zeigen die
    /// Hierarchie) und stehen **fest** und nicht aus der Farbtabelle: Ein Dokument wird
    /// gedruckt, und eine Überschrift, die im dunklen Erscheinungsbild hell wäre, verschwände
    /// auf weißem Papier (§4.26, derselbe Grund wie bei der Tinte).
    /// </para>
    /// </summary>
    public static IReadOnlyList<TdStil> Alle { get; } =
    [
        new("Standard", "Style.Normal", KoerperPt, false, false, null, 0, 0, 1.5, 4.5, 0),
        new("Kein Abstand", "Style.NoSpacing", KoerperPt, false, false, null, 0, 0, 0, 0, 0),
        new("Überschrift 1", "Style.Heading1", 21, true, false, "#2563EB", 0, 0, 12, 6, 1),
        new("Überschrift 2", "Style.Heading2", 16.5, true, false, "#14B8A6", 0, 0, 9, 4.5, 2),
        new("Überschrift 3", "Style.Heading3", 13.5, true, false, "#8B5CF6", 0, 0, 7.5, 3, 3),
        new("Überschrift 4", "Style.Heading4", 12, true, true, "#EC4899", 0, 0, 6, 3, 4),
        new("Titel", "Style.Title", 25.5, true, false, null, 0, 0, 4.5, 9, 0),
        new("Zitat", "Style.Quote", KoerperPt, false, true, "#6B7A99", ZitatEinzugCm, ZitatEinzugCm, 6, 6, 0),
        new("Kopfzeile", "Style.Header", 9, false, false, "#6B7A99", 0, 0, 1.5, 1.5, 0),
        new("Fußzeile", "Style.Footer", 9, false, false, "#6B7A99", 0, 0, 1.5, 1.5, 0),
    ];

    /// <summary>
    /// Der Einzug des Zitats: <b>28 geräteunabhängige Pixel</b>, in Zentimetern.
    ///
    /// <para>
    /// <b>Die krumme Zahl ist der Punkt und kein Versehen</b> (HANDOFF §4.46). Hier stand
    /// <c>1</c> — eine gerundete Fassung derselben 28, und damit ein Einzug, der sich um
    /// einen Viertelmillimeter von dem des WPF-Kopfs unterschied. Rund und falsch ist an
    /// dieser Stelle schlechter als krumm und gleich: Die beiden Köpfe sollen dasselbe
    /// Schriftbild zeigen, nicht ein ähnliches.
    /// </para>
    /// </summary>
    private const double ZitatEinzugCm = 28 * 2.54 / 96.0;

    /// <summary>
    /// Die Größe des Fließtextes <b>in Punkt</b> — <c>TextStyles.BodySize</c> sind 15
    /// geräteunabhängige Pixel, und das sind <b>11,25 pt</b>.
    ///
    /// <para>
    /// <b>Hier stand <c>15</c>, und das war der Fehler, aus dem alle anderen folgten</b>
    /// (HANDOFF §4.46). Die Zahl ist beim Anlegen dieser Tabelle (§4.39) aus
    /// <c>TextStyles.BodySize</c> abgeschrieben und dabei von <b>Pixeln</b> auf <b>Punkt</b>
    /// umetikettiert worden — bei 96 dpi ist ein Punkt aber <b>1,333</b> Pixel, nicht einer.
    /// Damit war jede Vorlage dieses Kopfes um ein Drittel größer als dieselbe Vorlage im
    /// WPF-Kopf.
    /// </para>
    /// <para>
    /// <b>Der Beweis stand im eigenen Kopf und nicht im Vergleich mit dem anderen:</b> Ein
    /// unberührter Absatz wird über <see cref="TdCharFormat.Standard"/> gesetzt und ist
    /// <b>11 pt</b> groß. Die Vorlage „Standard" machte ihn auf <b>15 pt</b> — <b>„Standard"
    /// anzuwenden vergrößerte den Text um ein Drittel</b>, obwohl es die Vorlage ist, die
    /// nichts ändern sollte.
    /// </para>
    /// <para>
    /// <b>Warum 11,25 und nicht 11</b> — also nicht die Vorgabe des Dateiformats: Der WPF-Kopf
    /// ist hier die Quelle, und 15 Pixel sind nun einmal 11,25 pt. <c>TdCharFormat.Standard</c>
    /// bleibt bei <b>11</b>, denn das ist die Vorgabe des <i>Formats</i> und keine Vorlage —
    /// sie zu verschieben änderte, wie jedes gespeicherte Dokument ohne eigene Größe gesetzt
    /// wird (§4.14). Der Rest von einem Viertelpunkt liegt weit innerhalb der Schranke von
    /// <see cref="Passt"/>: Ein unberührter Absatz wird als „Standard" wiedererkannt, und
    /// genau so soll es sein.
    /// </para>
    /// </summary>
    public const double KoerperPt = 11.25;

    /// <summary>Die Vorlage „Standard" — die, auf die „Formatierung zurücksetzen" führt.</summary>
    public static TdStil Standard => Alle[0];

    /// <summary>
    /// <b>Setzt diese Vorlage auf einen Absatz</b> — Größe, Fett, Kursiv, Farbe, die vier
    /// Abstände und die Gliederungsebene.
    ///
    /// <para>
    /// <b>Die Gliederungsebene geht mit</b> (<see cref="TdParaFormat.OutlineLevel"/>): Daran
    /// hängt das Inhaltsverzeichnis (§4.20). Ohne sie sähe eine Überschrift wie eine aus und
    /// stünde trotzdem nicht darin.
    /// </para>
    /// <para>
    /// <b>Eine Überschrift hebt die Listenzugehörigkeit auf</b>, „Standard" nicht: Sie ist
    /// das, worauf man landet, wenn man eine Überschrift zurücknimmt, und dabei einen
    /// Aufzählungspunkt zu verlieren wäre eine Überraschung.
    /// </para>
    /// <para>
    /// <b>Warum das hier steht und nicht in <c>TdListEdit.Vorlage</c></b>, wo es bis Phase 5,
    /// Schritt ④ allein stand: Der Markdown-Import (<see cref="TdMarkdown.Lesen(string,
    /// ITdImages)"/>) braucht dieselben zwölf Zuweisungen für einen Absatz, den es noch gar
    /// nicht gibt — er kann also nicht über eine <c>TdSelection</c> gehen. <b>Zwei Fassungen
    /// wären zwei, von denen später eine jemand ändert</b>, und das Fehlerbild wäre eine
    /// importierte Überschrift, die nicht im Inhaltsverzeichnis steht.
    /// </para>
    /// </summary>
    public void AufAbsatz(TdParagraph ziel) =>
        Setzen(ziel.Format, ziel.CharFormat, () => ziel.List = null);

    /// <summary>
    /// <inheritdoc cref="AufAbsatz(TdParagraph)" path="/summary/para[1]"/>
    /// <para>
    /// <b>Zwei Überladungen, weil es zwei Träger desselben Trios gibt:</b>
    /// <see cref="TdParagraph"/> ist der Absatz selbst, <see cref="TdAbsatzStil"/> die
    /// Abweichung, die <c>TdFormatEdit.Absatzweise</c> herumreicht. Beide führen
    /// <c>Format</c>, <c>CharFormat</c> und <c>List</c> — <b>die zwölf Zuweisungen stehen
    /// trotzdem nur einmal</b>, in <see cref="Setzen"/>.
    /// </para>
    /// </summary>
    public void AufAbsatz(TdAbsatzStil ziel) =>
        Setzen(ziel.Format, ziel.CharFormat, () => ziel.List = null);

    private void Setzen(TdParaFormat format, TdCharFormat zeichen, Action listeLoeschen)
    {
        zeichen.FontSize = SizePt;
        zeichen.Bold = Bold;
        zeichen.Italic = Italic;
        zeichen.Color = ColorHex;

        format.SpaceBeforePt = BeforePt == 0 ? null : BeforePt;
        format.SpaceAfterPt = AfterPt == 0 ? null : AfterPt;
        format.LeftIndentCm = LeftCm == 0 ? null : LeftCm;
        format.RightIndentCm = RightCm == 0 ? null : RightCm;
        format.OutlineLevel = Heading == 0 ? null : Heading;

        if (Heading > 0) listeLoeschen();
    }

    /// <summary>Die Vorlage zu einer Gliederungsebene; <c>null</c>, wenn es keine gibt.</summary>
    public static TdStil? ZurEbene(int ebene)
    {
        foreach (var stil in Alle)
            if (stil.Heading == ebene && ebene > 0) return stil;

        return null;
    }

    /// <summary>Die Vorlage mit diesem internen Namen; <c>null</c>, wenn es keine gibt.</summary>
    public static TdStil? MitNamen(string name)
    {
        foreach (var stil in Alle)
            if (stil.Name == name) return stil;

        return null;
    }

    /// <summary>
    /// Passt dieser Absatz zu dieser Vorlage? — <b>die Auskunft, aus der eine Vorlagenliste
    /// zeigt, welche gerade gilt.</b>
    ///
    /// <para>
    /// <b>Verglichen wird an Größe, Fett und Kursiv und nicht an einem gespeicherten Namen.</b>
    /// Ein Vorlagenname am Absatz wäre eine zweite Wahrheit: Wer die Größe von Hand ändert,
    /// hätte danach eine „Überschrift 1", die keine ist. Der WPF-Kopf entscheidet es seit jeher
    /// genauso (<c>TextStyles.HeadingLevel</c> misst die Größe) — und weil beide Köpfe dieselbe
    /// Tabelle benutzen, kommen sie auf dasselbe Ergebnis.
    /// </para>
    /// </summary>
    public bool Passt(TdCharFormat aufgeloest) =>
        Math.Abs((aufgeloest.FontSize ?? KoerperPt) - SizePt) < 0.6
        && (aufgeloest.Bold == true) == Bold
        && (aufgeloest.Italic == true) == Italic;
}
