using System.Globalization;

namespace GonkNote.Core.Text;

/// <summary>Was eine Tabellenformel ausrechnet.</summary>
public enum TdFormelArt
{
    Summe,
    Mittelwert,
    Anzahl,
    Kleinstwert,
    Groesstwert,
    Produkt,
}

/// <summary>Aus welcher Richtung sie ihre Zahlen nimmt.</summary>
public enum TdFormelRichtung
{
    Oben,
    Unten,
    Links,
    Rechts,
}

/// <summary>
/// <b>Die Rechnung hinter „Formel" in einer Tabelle</b> (§4.91) — Words <c>=SUMME(ABOVE)</c>.
///
/// <para>
/// <b>Sie steht getrennt von <see cref="TdTableEdit"/>, weil sie nichts am Dokument ändert.</b>
/// Zahlen aus Zellen lesen, addieren und das Ergebnis formatieren ist eine Rechnung ohne
/// Nebenwirkung — und damit das, was sich am billigsten prüfen lässt. <c>TdTableEdit.Formel</c>
/// ruft sie und schreibt, was herauskommt.
/// </para>
/// <para>
/// <b>⚠ Das Ergebnis geht als Text in die Zelle und nicht als Feld</b>, und das ist ein
/// benanntes Zugeständnis. §4.20 sagt: „ein Feld speichert seine Art, nicht seinen Wert" — für
/// eine Formel bräuchte es also eine neue <see cref="TdFieldKind"/>, einen Auswertungsschritt
/// im Umbruch und einen Weg durch DOCX. <b>Der WPF-Kopf schreibt seit jeher ebenfalls nur das
/// Ergebnis</b>, und beim Editor ist Windows die Vorlage (§6). Wer die Zahlen ändert, drückt
/// den Knopf noch einmal.
/// </para>
/// </summary>
public static class TdTabellenformel
{
    /// <summary>
    /// Rechnet — oder <c>null</c>, wenn in der Richtung keine einzige Zahl steht.
    ///
    /// <para>
    /// <b><c>null</c> ist kein Fehler, sondern eine Auskunft:</b> Wer „Summe der Spalte
    /// darüber" auf die erste Zeile anwendet, hat nichts zum Addieren. Eine 0 hineinzuschreiben
    /// sähe aus wie ein Ergebnis.
    /// </para>
    /// </summary>
    public static double? Rechnen(
        TdTable tabelle, int zeile, int spalte, TdFormelArt art, TdFormelRichtung richtung)
    {
        var zahlen = Zahlen(tabelle, zeile, spalte, richtung).ToList();
        if (zahlen.Count == 0) return null;

        return art switch
        {
            TdFormelArt.Summe => zahlen.Sum(),
            TdFormelArt.Mittelwert => zahlen.Average(),
            TdFormelArt.Anzahl => zahlen.Count,
            TdFormelArt.Kleinstwert => zahlen.Min(),
            TdFormelArt.Groesstwert => zahlen.Max(),
            TdFormelArt.Produkt => zahlen.Aggregate(1.0, (a, b) => a * b),
            _ => null,
        };
    }

    /// <summary>
    /// Die Zahlen in der gefragten Richtung, <b>ohne die Zelle selbst</b>.
    ///
    /// <para>
    /// <b>Zellen ohne Zahl werden übersprungen und beenden die Reihe nicht.</b> Word hört bei
    /// der ersten leeren Zelle auf; das ist eine Regel, die man kennen muss, um sie
    /// vorherzusagen — und wer eine Zwischenüberschrift in der Spalte hat, bekommt dort eine
    /// halbe Summe, ohne dass etwas danach aussieht.
    /// </para>
    /// </summary>
    public static IEnumerable<double> Zahlen(
        TdTable tabelle, int zeile, int spalte, TdFormelRichtung richtung)
    {
        for (int z = 0; z < tabelle.Rows.Count; z++)
            for (int s = 0; s < tabelle.Rows[z].Cells.Count; s++)
            {
                if (z == zeile && s == spalte) continue;

                bool passt = richtung switch
                {
                    TdFormelRichtung.Oben => s == spalte && z < zeile,
                    TdFormelRichtung.Unten => s == spalte && z > zeile,
                    TdFormelRichtung.Links => z == zeile && s < spalte,
                    _ => z == zeile && s > spalte,
                };

                if (passt && AlsZahl(Text(tabelle.Rows[z].Cells[s])) is { } wert)
                    yield return wert;
            }
    }

    /// <summary>Der Klartext einer Zelle, Absätze durch Leerzeichen getrennt.</summary>
    public static string Text(TdTableCell zelle) =>
        string.Join(" ", zelle.Blocks.OfType<TdParagraph>().Select(a => a.PlainText())).Trim();

    /// <summary>
    /// Liest eine Zahl aus einem Zelltext — oder <c>null</c>.
    ///
    /// <para>
    /// <b>Beide Schreibweisen werden versucht</b>, deutsche („1.234,56") und englische
    /// („1,234.56"). Ein Dokument kann aus beiden Welten kommen, und eine Tabelle, deren Summe
    /// von der Systemsprache abhängt, rechnet auf dem nächsten Rechner anders.
    /// </para>
    /// <para>
    /// <b>Währungszeichen und Prozent stören nicht</b> — was ein Mensch als Zahl liest, soll
    /// mitzählen. Alles, was keine Ziffer, kein Trennzeichen und kein Vorzeichen ist, fällt
    /// vorher weg.
    /// </para>
    /// </summary>
    public static double? AlsZahl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var roh = new string(text.Where(
            c => char.IsDigit(c) || c is '.' or ',' or '-' or '+' or ' ').ToArray()).Trim();

        if (roh.Length == 0 || !roh.Any(char.IsDigit)) return null;

        foreach (var kultur in new[] { Deutsch, Englisch })
            if (double.TryParse(roh, NumberStyles.Number, kultur, out double wert))
                return wert;

        return null;
    }

    private static readonly CultureInfo Deutsch = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Englisch = CultureInfo.InvariantCulture;

    /// <summary>
    /// Das Ergebnis als Text. <b>Ganze Zahlen ohne Nachkommastellen</b> — eine Anzahl von
    /// „3,00" sieht aus, als wäre gemessen worden, wo gezählt wurde.
    /// </summary>
    public static string Formatiert(double wert) =>
        Math.Abs(wert - Math.Round(wert)) < 1e-9
            ? Math.Round(wert).ToString("0", Deutsch)
            : wert.ToString("0.##", Deutsch);
}
