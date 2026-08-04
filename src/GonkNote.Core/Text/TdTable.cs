using System.Text.Json.Serialization;

namespace GonkNote.Core.Text;

/// <summary>Senkrechte Ausrichtung des Inhalts einer Zelle.</summary>
public enum TdVAlign
{
    Top,
    Center,
    Bottom,
}

/// <summary>
/// Die Rolle einer Zelle in einer senkrechten Verbindung.
/// <para>
/// <b>Das ist DOCX' Sicht, und sie ist die richtige:</b> eine verbundene Zelle ist keine
/// Zelle, die über mehrere Zeilen reicht, sondern eine <see cref="Restart"/>-Zelle, der in
/// den Folgezeilen <see cref="Continue"/>-Zellen folgen. Jede Zeile behält damit ihre volle
/// Zellenzahl — und genau daran hängt, dass Spaltenzählung und Rahmen stimmen. Wer statt
/// dessen ein „RowSpan = 3" speichert, muss beim Lesen jeder Zeile nachhalten, welche Zellen
/// von oben hineinragen, und liegt beim ersten unregelmäßigen Raster daneben.
/// </para>
/// </summary>
public enum TdVerticalMerge
{
    None,
    Restart,
    Continue,
}

/// <summary>Eine Rahmenlinie.</summary>
/// <param name="WidthPt">Stärke in Punkt. <c>0</c> = keine Linie.</param>
/// <param name="Color">„#RRGGBB".</param>
public readonly record struct TdBorder(double WidthPt, string Color)
{
    public static TdBorder Keine => new(0, "#000000");

    public bool Sichtbar => WidthPt > 0;
}

/// <summary>
/// Das Format einer Tabelle: Rahmen und Zellinnenabstände.
/// <para>
/// <c>InsideH</c> und <c>InsideV</c> sind die Linien **zwischen** den Zellen. DOCX führt sie
/// eigens, statt sie aus den Zellrahmen zu erschließen — sonst wäre jede Linie zweimal
/// beschrieben, einmal von der Zelle darüber und einmal von der darunter.
/// </para>
/// </summary>
public sealed class TdTableFormat
{
    public TdBorder Top { get; set; } = Standardlinie;
    public TdBorder Left { get; set; } = Standardlinie;
    public TdBorder Bottom { get; set; } = Standardlinie;
    public TdBorder Right { get; set; } = Standardlinie;
    public TdBorder InsideH { get; set; } = Standardlinie;
    public TdBorder InsideV { get; set; } = Standardlinie;

    /// <summary>Innenabstand der Zellen in Zentimetern.</summary>
    public double CellPaddingLeftCm { get; set; } = 0.19;
    public double CellPaddingRightCm { get; set; } = 0.19;
    public double CellPaddingTopCm { get; set; } = 0;
    public double CellPaddingBottomCm { get; set; } = 0;

    private static TdBorder Standardlinie => new(0.5, "#000000");

    public TdTableFormat Kopie() => (TdTableFormat)MemberwiseClone();
}

/// <summary>
/// Eine Tabellenzelle. Sie enthält **Blöcke**, nicht bloß Text: in einer Zelle darf ein
/// zweiter Absatz stehen, eine Liste, und später ein Bild.
/// </summary>
public sealed class TdTableCell
{
    public List<TdBlock> Blocks { get; set; } = new();

    /// <summary>Wie viele Rasterspalten diese Zelle einnimmt (waagerecht verbunden).</summary>
    public int ColumnSpan { get; set; } = 1;

    public TdVerticalMerge VerticalMerge { get; set; } = TdVerticalMerge.None;

    /// <summary>Hintergrund als „#RRGGBB"; <c>null</c> = keiner.</summary>
    public string? Shading { get; set; }

    public TdVAlign VerticalAlign { get; set; } = TdVAlign.Top;

    public TdTableCell() { }

    public TdTableCell(params TdBlock[] blocks) => Blocks.AddRange(blocks);

    /// <summary>Eine Zelle mit einem einzigen Absatz — der Normalfall.</summary>
    public static TdTableCell Text(string text) => new(new TdParagraph(text));

    /// <summary>
    /// Eine Zelle, die zu einer Verbindung von oben gehört, trägt selbst keinen Inhalt —
    /// er steht in der <see cref="TdVerticalMerge.Restart"/>-Zelle darüber.
    /// </summary>
    [JsonIgnore]
    public bool IstFortsetzung => VerticalMerge == TdVerticalMerge.Continue;

    public string PlainText() => string.Join("\n", Blocks.Select(b => b.PlainText()));
}

/// <summary>Eine Tabellenzeile.</summary>
public sealed class TdTableRow
{
    public List<TdTableCell> Cells { get; set; } = new();

    /// <summary>
    /// Kopfzeile: wird auf jeder Seite wiederholt, über die die Tabelle läuft. Nur die
    /// **ersten** Zeilen einer Tabelle dürfen das — Word ignoriert es sonst, und das ist
    /// keine Eigenheit, sondern die einzige Lesart, die sich umbrechen lässt.
    /// </summary>
    public bool IsHeader { get; set; }

    /// <summary>Mindesthöhe in Zentimetern; <c>null</c> = so hoch wie der Inhalt.</summary>
    public double? MinHeightCm { get; set; }

    public TdTableRow() { }

    public TdTableRow(params TdTableCell[] cells) => Cells.AddRange(cells);

    /// <summary>Eine Zeile aus lauter einfachen Textzellen.</summary>
    public static TdTableRow Text(params string[] zellen) =>
        new([.. zellen.Select(TdTableCell.Text)]);

    /// <summary>
    /// Die Zahl der Rasterspalten, die diese Zeile belegt — <b>nicht</b> die Zahl ihrer
    /// Zellen. Eine Zelle über zwei Spalten zählt zwei.
    /// </summary>
    public int GridSpaltenzahl() => Cells.Sum(z => Math.Max(1, z.ColumnSpan));
}

/// <summary>
/// Eine Tabelle — Phase 4, Schritt 4.
///
/// <para>
/// <b>Das Raster steht in <see cref="ColumnWidthsCm"/> und nicht an den Zellen.</b> Eine
/// Spaltenbreite gilt für die ganze Spalte; sie an jeder Zelle zu führen hieße, denselben
/// Wert je Zeile ein weiteres Mal zu speichern — und ein Dokument, in dem Zeile 3 eine andere
/// Vorstellung von Spalte 2 hat als Zeile 1, ist nicht darstellbar. DOCX hält es mit
/// <c>w:tblGrid</c> genauso.
/// </para>
/// </summary>
public sealed class TdTable : TdBlock
{
    public List<TdTableRow> Rows { get; set; } = new();

    /// <summary>
    /// Die Breiten der Rasterspalten in Zentimetern. Sind es weniger als die Zeilen belegen,
    /// werden die fehlenden beim Umbruch gleichmäßig ergänzt — eine Tabelle aus fremder Hand
    /// soll nicht daran scheitern.
    /// </summary>
    public List<double> ColumnWidthsCm { get; set; } = new();

    public TdTableFormat Format { get; set; } = new();

    public TdTable() { }

    public TdTable(params TdTableRow[] rows) => Rows.AddRange(rows);

    /// <summary>Die Zahl der Rasterspalten: das Größte aus Raster und Zeilenbelegung.</summary>
    public int Spaltenzahl()
    {
        int aus_zeilen = Rows.Count == 0 ? 0 : Rows.Max(z => z.GridSpaltenzahl());
        return Math.Max(ColumnWidthsCm.Count, aus_zeilen);
    }

    /// <summary>
    /// Die Spaltenbreiten, auf <see cref="Spaltenzahl"/> aufgefüllt. Fehlende bekommen den
    /// Rest der verfügbaren Breite gleichmäßig zugeteilt; ist gar nichts angegeben, wird
    /// gleichmäßig geteilt.
    /// </summary>
    public double[] Spaltenbreiten(double verfuegbarCm)
    {
        int spalten = Spaltenzahl();
        if (spalten == 0) return [];

        var breiten = new double[spalten];
        double vergeben = 0;
        int offen = 0;

        for (int i = 0; i < spalten; i++)
        {
            if (i < ColumnWidthsCm.Count && ColumnWidthsCm[i] > 0)
            {
                breiten[i] = ColumnWidthsCm[i];
                vergeben += breiten[i];
            }
            else offen++;
        }

        if (offen > 0)
        {
            // **Nie null oder negativ**: eine Tabelle, deren angegebene Spalten schon breiter
            // sind als die Seite, bekäme sonst Spalten der Breite −3 cm, und der
            // Zeilenumbruch darin liefe gegen die Wand.
            double rest = Math.Max(0, verfuegbarCm - vergeben);
            double jede = Math.Max(0.5, rest / offen);
            for (int i = 0; i < spalten; i++)
                if (breiten[i] <= 0) breiten[i] = jede;
        }

        return breiten;
    }

    public override string PlainText() =>
        string.Join("\n", Rows.Select(z =>
            string.Join("\t", z.Cells.Where(c => !c.IstFortsetzung).Select(c => c.PlainText()))));
}
