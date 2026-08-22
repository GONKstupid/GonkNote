using System.Text.Json.Serialization;

namespace GonkNote.Core.Text;

/// <summary>
/// Etwas, das im Text einen **Kasten** einnimmt: ein Bild oder ein Diagramm.
///
/// <para>
/// <b>Ein Stück und kein Block</b> — genau wie in DOCX, wo eine Zeichnung immer in einem Lauf
/// steht (<c>w:drawing</c>). Ein bildbreites Foto ist dort ein Absatz, der nichts als dieses
/// Bild enthält; ein zweiter, block-eigener Weg dafür wäre eine zweite Wahrheit über dasselbe
/// (§4.10) und ein zweiter Pfad im Umbruch. **Die Diskriminatoren „image" und „chart" auf
/// <see cref="TdBlock"/> bleiben deshalb frei** — dasselbe Bild wie bei „list" in §4.17.
/// </para>
///
/// <para>
/// <b>Die Größe steht hier und nicht bei den Daten.</b> Ein Bild kann in zwei Dokumenten
/// verschieden groß stehen und ist trotzdem dasselbe Bild; ein Diagramm hat gar keine
/// natürliche Größe. Beides in Zentimetern, wie alles im Modell (§4.16).
/// </para>
/// </summary>
public abstract class TdGraphic : TdInline
{
    public double WidthCm { get; set; }
    public double HeightCm { get; set; }

    /// <summary>
    /// Der Alternativtext — was dasteht, wenn das Bild nicht dasteht. Word führt ihn ebenso
    /// (<c>wp:docPr/@descr</c>), der Markdown-Export braucht ihn (<c>![Text](…)</c>), und eine
    /// Vorlesehilfe hat sonst nichts.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// <b>Eine Grafik trägt keinen Klartext bei</b> — aus demselben Grund wie ein Feld
    /// (§4.20): Im Dokument steht ein Bild und kein Wort. Der Alternativtext ist ein Ersatz
    /// für die Anzeige und keine Stelle im Text; wer ihn hier zurückgäbe, zählte ihn im
    /// Wortzähler mit und fände ihn in der Suche.
    /// </summary>
    public override string PlainText() => "";
}

/// <summary>
/// Ein Bild im Text.
///
/// <para>
/// <b>Die Bytes stehen nicht im Dokument, sondern im Blob-Speicher</b> — hier steht nur der
/// Verweis. Das ist keine Sparsamkeit, sondern eine **gemessene** Entscheidung aus V1
/// (<c>DocumentImages</c>): Ein Word-Dokument mit drei Fotos (2 MB JPEG) wurde als
/// XamlPackage zu **16,8 MB** und riss damit die 16-MB-Grenze von LiteDB, weil WPF jedes Bild
/// als PNG neu kodierte. Seitdem liegt das **Original unangetastet** im Blob-Speicher, und
/// beim Export gehen genau diese Bytes wieder hinaus.
/// </para>
/// <para>
/// <see cref="Extension"/> gehört deshalb dazu: Sie sagt, als welcher Bildtyp die
/// unveränderten Bytes zurückgeschrieben werden. Ohne sie müsste der Export raten — und ein
/// JPEG, das als PNG hinausgeht, ist um ein Vielfaches größer.
/// </para>
/// </summary>
public sealed class TdImage : TdGraphic
{
    private string _extension = "png";

    /// <summary>Der Schlüssel im Blob-Speicher.</summary>
    public Guid BlobId { get; set; }

    /// <summary>Dateiendung des Originals („jpg", „png", …), ohne Punkt und klein.</summary>
    public string Extension
    {
        get => _extension;
        set => _extension = string.IsNullOrWhiteSpace(value)
            ? "png"
            : value.TrimStart('.').ToLowerInvariant();
    }

    public TdImage() { }

    public TdImage(Guid blobId, string extension, double widthCm, double heightCm)
    {
        BlobId = blobId;
        Extension = extension;
        WidthCm = widthCm;
        HeightCm = heightCm;
    }
}

/// <summary>Die Art eines Diagramms. <inheritdoc cref="TdFieldKind" path="/para[1]"/></summary>
public enum TdChartKind
{
    /// <summary>Säulen (senkrecht).</summary>
    Column,

    /// <summary>Balken (waagerecht).</summary>
    Bar,

    Line,

    /// <summary>Nur Punkte.</summary>
    Scatter,

    /// <summary>Punkte mit Linie.</summary>
    ScatterLine,

    /// <summary>Kuchen.</summary>
    Pie,

    Radar,
}

/// <summary>Eine Werte-Reihe eines Diagramms.</summary>
public sealed class TdChartSeries
{
    private string _name = "";

    /// <summary>
    /// Der Name der Reihe; leer, wenn keiner eingegeben wurde.
    /// <para>
    /// <b>Ein Name, den niemand eingegeben hat, gehört nicht ins Dokument.</b> Die Oberfläche
    /// darf in der Legende „Reihe 2" anzeigen — geschrieben wird das nicht, sonst stünde in
    /// der Datei ein deutsches Wort, das von der Sprache beim Einfügen abhängt.
    /// </para>
    /// </summary>
    public string Name { get => _name; set => _name = value ?? ""; }

    public List<double> Values { get; set; } = new();

    public TdChartSeries() { }

    public TdChartSeries(string name, params double[] werte)
    {
        Name = name;
        Values.AddRange(werte);
    }

    /// <summary>Eine eigenständige Reihe mit denselben Werten — <b>eine eigene Liste</b>.</summary>
    public TdChartSeries Kopie() => new(Name) { Values = [.. Values] };
}

/// <summary>
/// Ein Diagramm — Phase 4, Schritt 6, der letzte der sechs.
///
/// <para>
/// <b>Der Befund, der diesen Schritt begründet.</b> Der heutige Editor rendert ein Diagramm
/// beim Einfügen zu einer **Bitmap** und legt diese in den Text (<c>ChartDialog</c>:
/// „Das Ergebnis wird als Bitmap gerendert und in den Text eingefügt … keine
/// Live-Datenbindung"). **Die Zahlen sind damit im selben Augenblick verloren.** Ein Diagramm
/// lässt sich nie wieder ändern, nur löschen und neu bauen; ein Tippfehler in einer
/// Kategorie kostet die ganze Eingabe. Und beim Export geht ein Pixelbild hinaus, wo Word ein
/// Diagramm erwartet.
/// </para>
/// <para>
/// <b>Hier stehen deshalb die Daten</b>, und das Bild wird gerechnet — dasselbe Muster wie bei
/// der Listennummer (§4.17) und beim Feld (§4.20), zum dritten Mal und aus demselben Grund:
/// Was sich ableiten lässt, wird nicht gespeichert, sonst gibt es zwei Wahrheiten und eine
/// davon veraltet.
/// </para>
/// </summary>
public sealed class TdChart : TdGraphic
{
    private string _title = "";

    public TdChartKind Kind { get; set; } = TdChartKind.Column;

    /// <summary>Überschrift des Diagramms; leer = keine.</summary>
    public string Title { get => _title; set => _title = value ?? ""; }

    /// <summary>Die Beschriftungen der Achse — je Wert einer Reihe eine.</summary>
    public List<string> Categories { get; set; } = new();

    public List<TdChartSeries> Series { get; set; } = new();

    /// <summary>
    /// Die Farben, in der Reihenfolge, in der sie vergeben werden.
    /// <para>
    /// <b>Sie stehen am Diagramm und nicht in der Oberfläche.</b> Der heutige Editor hält die
    /// Palette in einem **statischen** Feld des Dialogs — sie gilt für die Sitzung und ist beim
    /// nächsten Start weg; zwei Diagramme in derselben Datei können deshalb verschieden
    /// aussehen, ohne dass die Datei den Unterschied kennt. Ein Dokument muss sich selbst
    /// erklären.
    /// </para>
    /// </summary>
    public List<string> Palette { get; set; } = new();

    /// <summary>Die sechs Farben, mit denen der heutige Editor beginnt.</summary>
    public static IReadOnlyList<string> StandardPalette { get; } =
        ["#2563EB", "#14B8A6", "#EC4899", "#8B5CF6", "#22C55E", "#EAB308"];

    public TdChart() { }

    public TdChart(TdChartKind kind, double widthCm, double heightCm)
    {
        Kind = kind;
        WidthCm = widthCm;
        HeightCm = heightCm;
    }

    /// <summary>
    /// Steht neben dem Diagramm eine Legende?
    /// <para>
    /// <b>Gerechnet und nicht gespeichert:</b> Sie hat nur dann etwas zu sagen, wenn es mehr
    /// als eine Reihe gibt — beim Kuchen stehen die Namen ohnehin an den Stücken. Dieselbe
    /// Regel wendet der heutige Editor an, nur eben in seiner Zeichenroutine, wo sie außer ihm
    /// niemand sieht.
    /// </para>
    /// </summary>
    [JsonIgnore]
    public bool ShowLegend => Kind != TdChartKind.Pie && Series.Count > 1;

    /// <summary>
    /// Bekommt jedes **Element** eine eigene Farbe statt jede Reihe?
    /// <para>
    /// Beim Kuchen immer — dort ist jedes Stück eine Kategorie. Bei Säulen und Balken mit
    /// **einer** Reihe ebenfalls: ein einfarbiges Säulenbild wäre richtig, sieht aber aus wie
    /// ein Fehler. Bei Linien und Radar dagegen nicht, auch nicht bei einer einzigen Reihe —
    /// eine Kurve, die alle zwei Punkte die Farbe wechselt, ist keine Kurve mehr. Genau diese
    /// Unterscheidung trifft der heutige Editor in seinen zwei Zeichenroutinen.
    /// </para>
    /// </summary>
    [JsonIgnore]
    public bool FarbeJeElement =>
        Kind == TdChartKind.Pie ||
        (Series.Count <= 1 && Kind is TdChartKind.Column or TdChartKind.Bar);

    /// <summary>
    /// Wie viele Farben dieses Diagramm überhaupt vergibt.
    /// <para>
    /// <b>Das ist zugleich die Zahl, die durch DOCX passt.</b> Eine Farbe, die kein Element
    /// benutzt, hat in der Datei keinen Ort — sie überlebt das eigene Format, aber nicht den
    /// Weg durch DOCX (§4.21).
    /// </para>
    /// </summary>
    [JsonIgnore]
    public int FarbenGebraucht =>
        Series.Count == 0 ? 0 : FarbeJeElement ? Punktzahl() : Series.Count;

    /// <summary>
    /// Die Farbe an einer Stelle. **Läuft um**, wenn die Palette kürzer ist als die Zahl der
    /// Elemente — ein siebter Balken bekommt wieder die erste Farbe, statt keine zu haben.
    /// </summary>
    public string Farbe(int index)
    {
        var palette = Palette.Count > 0 ? Palette : (IReadOnlyList<string>)StandardPalette;
        return palette[((index % palette.Count) + palette.Count) % palette.Count];
    }

    /// <summary>
    /// Die Beschriftung an einer Stelle — oder die laufende Nummer, wenn es keine gibt.
    /// <para>
    /// Der heutige Editor füllt fehlende Kategorien beim Zeichnen mit „1", „2", … auf. Das
    /// bleibt eine Sache der Anzeige: **Gespeichert wird nur, was jemand eingegeben hat.**
    /// </para>
    /// </summary>
    public string Kategorie(int index) =>
        index < Categories.Count && Categories[index].Length > 0
            ? Categories[index]
            : (index + 1).ToString();

    /// <summary>Die längste Reihe — so viele Kategorien braucht das Diagramm.</summary>
    public int Punktzahl() => Series.Count == 0 ? 0 : Series.Max(r => r.Values.Count);

    /// <summary>
    /// <b>Ein eigenständiges Diagramm mit demselben Inhalt</b> (HANDOFF §4.50).
    ///
    /// <para>
    /// <b>Wozu:</b> Ein Diagramm reist durch den WPF-Editor als Auflage an seinem Träger
    /// (§4.49). Auf dem Rückweg darf <b>nicht dasselbe Objekt</b> ins neue Modell wandern —
    /// es läge sonst im alten und im neuen zugleich, und §4.32 verlangt, dass Stücke
    /// <b>ersetzt</b> und nicht verändert werden: Ein späterer Griff änderte sonst die
    /// Sicherung des Verlaufs mit.
    /// </para>
    /// <para>
    /// <b>Die Listen werden mitkopiert und nicht geteilt.</b> Eine geteilte
    /// <see cref="Series"/>-Liste wäre genau derselbe Fehler eine Ebene tiefer, und er fiele
    /// erst auf, wenn jemand rückgängig macht.
    /// </para>
    /// <para>
    /// <b>Diese Methode ist die Stelle, an der eine neue Eigenschaft vergessen werden kann</b>
    /// — deshalb hält der Wächter <c>Die_Kopie_eines_Diagramms_ist_gleich</c> sie gegen die
    /// gespeicherte Fassung und nicht gegen eine Aufzählung von Hand.
    /// </para>
    /// </summary>
    public TdChart Kopie() => new(Kind, WidthCm, HeightCm)
    {
        Title = Title,
        AltText = AltText,
        Format = Format.Kopie(),
        Categories = [.. Categories],
        Palette = [.. Palette],
        Series = [.. Series.Select(reihe => reihe.Kopie())],
    };
}
