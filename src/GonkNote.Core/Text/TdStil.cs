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
        new("Standard", "Style.Normal", KoerperPt, false, false, null, 0, 0, 2, 6, 0),
        new("Kein Abstand", "Style.NoSpacing", KoerperPt, false, false, null, 0, 0, 0, 0, 0),
        new("Überschrift 1", "Style.Heading1", 28, true, false, "#2563EB", 0, 0, 16, 8, 1),
        new("Überschrift 2", "Style.Heading2", 22, true, false, "#14B8A6", 0, 0, 12, 6, 2),
        new("Überschrift 3", "Style.Heading3", 18, true, false, "#8B5CF6", 0, 0, 10, 4, 3),
        new("Überschrift 4", "Style.Heading4", 16, true, true, "#EC4899", 0, 0, 8, 4, 4),
        new("Titel", "Style.Title", 34, true, false, null, 0, 0, 6, 12, 0),
        new("Zitat", "Style.Quote", KoerperPt, false, true, "#6B7A99", 1, 1, 8, 8, 0),
        new("Kopfzeile", "Style.Header", 12, false, false, "#6B7A99", 0, 0, 2, 2, 0),
        new("Fußzeile", "Style.Footer", 12, false, false, "#6B7A99", 0, 0, 2, 2, 0),
    ];

    /// <summary>Die Größe des Fließtextes in Punkt — dieselbe Zahl wie <c>TextStyles.BodySize</c>.</summary>
    public const double KoerperPt = 15;

    /// <summary>Die Vorlage „Standard" — die, auf die „Formatierung zurücksetzen" führt.</summary>
    public static TdStil Standard => Alle[0];

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
