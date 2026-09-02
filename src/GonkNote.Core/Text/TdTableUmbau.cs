namespace GonkNote.Core.Text;

/// <summary>
/// <b>Der Tabellenentwurf, zweite Hälfte</b> — teilen, sortieren, rechnen und die Umwandlung
/// zwischen Tabelle und Text (§4.91).
///
/// <para>
/// <b>Das ist das, was Core erst rechnen lernen musste</b> — anders als §4.90, wo das Modell
/// alles schon konnte und nur die Handgriffe fehlten.
/// </para>
/// </summary>
public static partial class TdTableEdit
{
    /// <summary>
    /// Das Trennzeichen zwischen zwei Zellen beim Umwandeln in Text und zurück.
    /// <b>Der Tabulator ist die Vorgabe</b> — er kommt in Fließtext praktisch nie vor, und
    /// jede Tabellenkalkulation nimmt ihn beim Einfügen ebenfalls.
    /// </summary>
    public const char Trennzeichen = '\t';

    // ---------------------------------------------------------------- Teilen

    /// <summary>
    /// <b>Teilt die Tabelle oberhalb der Zeile, in der die Marke steht</b> — daraus werden zwei
    /// Tabellen mit einem leeren Absatz dazwischen.
    ///
    /// <para>
    /// <b>Der leere Absatz gehört dazu und ist kein Rest.</b> Zwei Tabellen unmittelbar
    /// hintereinander sind in DOCX <b>eine</b>; Word setzt deshalb ebenfalls einen Absatz
    /// dazwischen. Ohne ihn wäre das Teilen beim nächsten Öffnen wieder rückgängig.
    /// </para>
    /// <para>
    /// <b>Die erste Zeile lässt sich nicht abteilen</b> — darüber bliebe eine Tabelle ohne
    /// Zeilen, und die ist nichts, was man noch anklicken könnte (dieselbe Regel wie bei
    /// <see cref="ZeileLoeschen"/>).
    /// </para>
    /// </summary>
    public static TdChange? TabelleTeilen(TdDocument doc, TdSelection auswahl)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, zeile, _, liste, index, gezogen) = lage;

        if (zeile <= 0 || zeile >= tabelle.Rows.Count) return null;

        var oben = Kopie(tabelle);
        var unten = Kopie(tabelle);

        oben.Rows.RemoveRange(zeile, oben.Rows.Count - zeile);
        unten.Rows.RemoveRange(0, zeile);

        // **Die Kopfzeilen-Angabe wandert nicht mit.** Die untere Tabelle fängt mit einer
        // Datenzeile an; sie zur Kopfzeile zu erklären hieße, sie auf jeder Folgeseite zu
        // wiederholen (§4.19) — eine Wirkung, die niemand bestellt hat.
        if (unten.Rows.Count > 0) unten.Rows[0].IsHeader = false;

        return new TdChange(
            liste, index, [tabelle], [oben, new TdParagraph(), unten],
            gezogen, new TdSelection(new TdPosition(ErsterAbsatzIndex(doc, tabelle), 0, 0)),
            TdEditArt.Struktur, schliesstGruppe: true);
    }

    // ---------------------------------------------------------------- Sortieren

    /// <summary>
    /// Sortiert die Zeilen nach der Spalte, in der die Marke steht.
    ///
    /// <para>
    /// <b>Eine Kopfzeile bleibt oben stehen.</b> Sie ist keine Datenzeile (§4.19), und wer sie
    /// mitsortiert, findet sie beim nächsten Mal in der Mitte.
    /// </para>
    /// <para>
    /// <b>Zahlen werden als Zahlen verglichen, alles andere als Text.</b> Eine Spalte mit
    /// „2", „10", „9" gehört sonst in dieser Reihenfolge sortiert — der klassische Fehler, den
    /// jeder erkennt und niemand erwartet. <b>Gemischt entscheidet die Mehrheit:</b> Sind alle
    /// befüllten Zellen Zahlen, wird numerisch verglichen, sonst alphabetisch. Eine
    /// Zeile-für-Zeile-Entscheidung ergäbe eine Ordnung, die nicht durchgängig ist.
    /// </para>
    /// <para>
    /// <b>Die Sortierung ist stabil</b> (<see cref="Enumerable.OrderBy{T, TKey}(IEnumerable{T},
    /// Func{T, TKey})"/>): Zeilen mit gleichem Schlüssel behalten ihre Reihenfolge, und zweimal
    /// Sortieren ergibt dasselbe wie einmal.
    /// </para>
    /// </summary>
    public static TdChange? Sortieren(TdDocument doc, TdSelection auswahl, bool aufsteigend)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, _, spalte, liste, index, gezogen) = lage;

        var neu = Kopie(tabelle);

        int erste = neu.Rows.Count > 0 && neu.Rows[0].IsHeader ? 1 : 0;
        if (neu.Rows.Count - erste < 2) return null;

        var daten = neu.Rows.Skip(erste).ToList();
        var texte = daten.Select(z => Zelltext(z, spalte)).ToList();

        var befuellt = texte.Where(t => t.Length > 0).ToList();

        // **Datum vor Zahl**, und das ist keine Geschmacksfrage: „01.03.2026“ liest sich als
        // Zahl 1.032.026 und „15.02.2026“ als 15.022.026 — die Reihenfolge wäre umgekehrt und
        // sähe trotzdem plausibel aus. Wer zuerst nach Zahlen fragt, sortiert Daten falsch.
        bool alsDatum = befuellt.Count > 0 && befuellt.All(t => AlsDatum(t) is not null);
        bool numerisch = !alsDatum && befuellt.Count > 0
            && befuellt.All(t => TdTabellenformel.AlsZahl(t) is not null);

        var sortiert =
            alsDatum
                ? daten.OrderBy(z => AlsDatum(Zelltext(z, spalte)) ?? DateTime.MinValue).ToList()
            : numerisch
                ? daten.OrderBy(z => TdTabellenformel.AlsZahl(Zelltext(z, spalte)) ?? double.MinValue)
                       .ToList()
                : daten.OrderBy(z => Zelltext(z, spalte), StringComparer.CurrentCultureIgnoreCase)
                       .ToList();

        if (!aufsteigend) sortiert.Reverse();

        neu.Rows.RemoveRange(erste, neu.Rows.Count - erste);
        neu.Rows.AddRange(sortiert);

        // **Die Marke geht an den Tabellenanfang.** Die Zeile, in der sie stand, ist nach dem
        // Sortieren woanders; sie an ihrer Nummer stehen zu lassen hieße, sie auf eine fremde
        // Zeile zu setzen.
        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: true);
    }

    /// <summary>
    /// Ein Datum aus einem Zelltext — oder <c>null</c>.
    ///
    /// <para>
    /// <b>Nur volle Datumsangaben</b> (<c>DateTimeStyles.NoCurrentDateDefault</c>): Ohne diese
    /// Vorgabe läse <c>DateTime.TryParse</c> auch „5“ als den Fünften des laufenden Monats —
    /// und eine Spalte aus Zahlen wäre plötzlich eine Spalte aus Daten.
    /// </para>
    /// </summary>
    private static DateTime? AlsDatum(string? text) =>
        !string.IsNullOrWhiteSpace(text) &&
        DateTime.TryParse(text, System.Globalization.CultureInfo.CurrentCulture,
                          System.Globalization.DateTimeStyles.NoCurrentDateDefault, out var wert) &&
        wert.Date != DateTime.MinValue.Date
            ? wert
            : null;

    private static string Zelltext(TdTableRow zeile, int spalte) =>
        spalte >= 0 && spalte < zeile.Cells.Count
            ? TdTabellenformel.Text(zeile.Cells[spalte])
            : "";

    // ---------------------------------------------------------------- Formel

    /// <summary>
    /// Schreibt das Ergebnis einer Formel in die Zelle unter der Marke.
    ///
    /// <para>
    /// <b>Als Text und nicht als Feld</b> — die Begründung steht bei
    /// <see cref="TdTabellenformel"/>, und der WPF-Kopf tut seit jeher dasselbe.
    /// </para>
    /// <para>
    /// <b>Steht in der Richtung keine Zahl, passiert nichts.</b> Eine 0 hineinzuschreiben sähe
    /// aus wie ein Ergebnis.
    /// </para>
    /// </summary>
    public static TdChange? Formel(
        TdDocument doc, TdSelection auswahl, TdFormelArt art, TdFormelRichtung richtung)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, zeile, spalte, liste, index, gezogen) = lage;

        if (TdTabellenformel.Rechnen(tabelle, zeile, spalte, art, richtung) is not { } wert)
            return null;

        var neu = Kopie(tabelle);
        if (zeile >= neu.Rows.Count || spalte >= neu.Rows[zeile].Cells.Count) return null;

        var zelle = neu.Rows[zeile].Cells[spalte];
        zelle.Blocks.Clear();
        zelle.Blocks.Add(new TdParagraph(TdTabellenformel.Formatiert(wert)));

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: true);
    }

    // ---------------------------------------------------------------- Tabelle ↔ Text

    /// <summary>
    /// <b>Wandelt die Tabelle in Absätze um</b> — eine Zeile wird ein Absatz, die Zellen
    /// darin durch <see cref="Trennzeichen"/> getrennt.
    ///
    /// <para>
    /// <b>Was nicht Text ist, geht verloren, und das ist unvermeidlich</b> — ein Bild in einer
    /// Zelle hat in einem Absatz aus Zelltexten keinen Ort. Wer das nicht will, nimmt Strg+Z;
    /// deshalb ist es <b>ein</b> Schritt im Verlauf und kein halber.
    /// </para>
    /// </summary>
    public static TdChange? InText(TdDocument doc, TdSelection auswahl)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, _, _, liste, index, gezogen) = lage;

        var absaetze = tabelle.Rows
            .Select(z => new TdParagraph(string.Join(
                Trennzeichen, z.Cells.Select(TdTabellenformel.Text))))
            .Cast<TdBlock>()
            .ToList();

        if (absaetze.Count == 0) return null;

        return new TdChange(
            liste, index, [tabelle], absaetze,
            gezogen, new TdSelection(new TdPosition(ErsterAbsatzIndex(doc, tabelle), 0, 0)),
            TdEditArt.Struktur, schliesstGruppe: true);
    }
}
