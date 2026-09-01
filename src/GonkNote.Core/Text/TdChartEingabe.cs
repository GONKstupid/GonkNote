using System.Globalization;

namespace GonkNote.Core.Text;

/// <summary>
/// Der Weg zwischen dem, was jemand in ein Textfeld tippt, und einem <see cref="TdChart"/> —
/// <b>in beide Richtungen</b> (HANDOFF §4.82).
///
/// <para>
/// <b>Warum das hier steht und nicht im Dialog.</b> Der WPF-Kopf zerlegt seine Eingabe in
/// <c>ChartDialog.ParseRow</c>, also mitten in einem Fenster: Niemand außer diesem Fenster
/// kann die Regel benutzen, und niemand kann sie prüfen. Der Linux-Kopf hätte sie ein zweites
/// Mal schreiben müssen — dasselbe Muster wie bei <see cref="TdChartLayout"/> (§4.25), der
/// Trefferprüfung (§4.13) und der Markdown-Grammatik: <b>die Rechnung nach Core, das Malen
/// und Tippen in den Kopf.</b>
/// </para>
///
/// <para>
/// <b>Und der Rückweg ist der eigentliche Gewinn.</b> §4.21 hält fest, was der heutige Editor
/// kostet: „Ein Diagramm lässt sich nie wieder ändern, nur löschen und neu bauen; ein
/// Tippfehler in einer Kategorie kostet die ganze Eingabe." Das lag daran, dass der Dialog
/// eine <b>Bitmap</b> ablieferte. Ein <see cref="TdChart"/> trägt seine Zahlen bei sich —
/// <see cref="WerteText"/> holt sie in die Felder zurück, und der Dialog kann ein bestehendes
/// Diagramm öffnen statt nur ein neues anzulegen.
/// </para>
/// </summary>
public static class TdChartEingabe
{
    /// <summary>
    /// <b>Die eine Trennregel, und sie gilt für Zahlen wie für Beschriftungen.</b>
    ///
    /// <para>
    /// Steht im Text ein <b>Semikolon oder ein Tabulator</b>, so trennen <b>nur</b> diese —
    /// das Komma ist dann ein <b>Dezimalkomma</b>. Sonst trennt das Komma, und der Punkt ist
    /// das Dezimaltrennzeichen. Das ist die Aufteilung, die eine Tabellenkalkulation im
    /// deutschen Gebietsschema schreibt, und sie liest beide Schreibweisen richtig:
    /// <c>4, 7, 3</c> ebenso wie <c>3,5; 7,25</c>.
    /// </para>
    /// <para>
    /// <b>Sie ist die Antwort auf einen gemessenen Fehler im WPF-Kopf.</b> Dort trennt
    /// <c>ParseRow</c> erst an <c>, ; \t</c> und ersetzt <b>danach</b> in jedem Stück ein
    /// Komma durch einen Punkt — nach dem Trennen kann dort aber keines mehr stehen. Der
    /// Handgriff ist tot, und <c>3,5</c> wird still zu <b>zwei</b> Werten, 3 und 5. Wer eine
    /// Dezimalzahl deutsch schreibt, bekommt ein anderes Diagramm als das, das er eingegeben
    /// hat, ohne Meldung.
    /// </para>
    /// <para>
    /// <b>Eine Regel für beide Sorten Feld und nicht zwei.</b> Wer seine Werte mit Semikolon
    /// schreibt, schreibt seine Kategorien in derselben Sitzung genauso — zwei Regeln
    /// nebeneinander teilten dieselbe Zeile in den zwei Feldern verschieden auf.
    /// </para>
    /// </summary>
    private static string[] Teilen(string text)
    {
        char[] trenner = text.Contains(';') || text.Contains('\t')
            ? [';', '\t']
            : [','];

        return text.Split(trenner, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Die Zahlen einer Zeile. <b>Was keine Zahl ist, fällt heraus</b> — der Zeichner soll
    /// zeichnen und nicht über einen Tippfehler stolpern; leer bleibt leer.
    /// </summary>
    public static List<double> Zahlen(string? zeile)
    {
        var werte = new List<double>();
        if (string.IsNullOrWhiteSpace(zeile)) return werte;

        foreach (var stueck in Teilen(zeile))
        {
            // Nach dem Trennen ist ein verbliebenes Komma ein Dezimalkomma und nichts sonst.
            var roh = stueck.Replace(',', '.');
            if (double.TryParse(roh, NumberStyles.Float, CultureInfo.InvariantCulture, out double wert)
                && !double.IsNaN(wert) && !double.IsInfinity(wert))
                werte.Add(wert);
        }

        return werte;
    }

    /// <summary>Die Beschriftungen einer Zeile — Kategorien oder Reihennamen.</summary>
    public static List<string> Beschriftungen(string? zeile) =>
        string.IsNullOrWhiteSpace(zeile) ? [] : [.. Teilen(zeile)];

    /// <summary>
    /// Die Reihen aus dem Werteblock: <b>je nicht-leere Zeile eine Reihe</b>.
    /// <para>
    /// Die Namen kommen aus einer eigenen Zeile und werden der Reihe nach vergeben. <b>Fehlt
    /// einer, bleibt er leer</b> und wird nicht mit „Reihe 2" aufgefüllt: Ein Name, den
    /// niemand eingegeben hat, gehört nicht ins Dokument — sonst stünde in der Datei ein
    /// deutsches Wort, das davon abhängt, in welcher Sprache die App beim Einfügen lief. Das
    /// steht so am Modell (<see cref="TdChartSeries.Name"/>), und die Anzeige darf trotzdem
    /// „Reihe 2" in die Legende schreiben.
    /// </para>
    /// </summary>
    public static List<TdChartSeries> Reihen(string? werte, string? namen = null)
    {
        var beschriftungen = Beschriftungen(namen);
        var reihen = new List<TdChartSeries>();
        if (string.IsNullOrWhiteSpace(werte)) return reihen;

        foreach (var zeile in werte.Split('\n'))
        {
            var zahlen = Zahlen(zeile);
            if (zahlen.Count == 0) continue;

            string name = reihen.Count < beschriftungen.Count ? beschriftungen[reihen.Count] : "";
            reihen.Add(new TdChartSeries(name) { Values = zahlen });
        }

        return reihen;
    }

    /// <summary>
    /// Ein ganzes Diagramm aus den Feldern eines Dialogs.
    /// <para>
    /// <b>Gibt <c>null</c> zurück, wenn keine einzige Reihe herauskommt.</b> Ein Diagramm ohne
    /// Zahlen ist kein leeres Diagramm, sondern eine Eingabe, die der Nutzer noch nicht
    /// gemacht hat — der Aufrufer sagt ihm das, statt einen leeren Kasten einzufügen.
    /// </para>
    /// <para>
    /// <b>Überzählige Kategorien werden nicht abgeschnitten und fehlende nicht aufgefüllt.</b>
    /// Aufgefüllt wird beim Zeichnen (<see cref="TdChart.Kategorie"/>), damit im Dokument nur
    /// steht, was jemand eingegeben hat — dieselbe Regel wie bei den Reihennamen.
    /// </para>
    /// </summary>
    public static TdChart? Lesen(
        TdChartKind art, string? titel, string? kategorien, string? reihennamen, string? werte,
        double breiteCm, double hoeheCm, IReadOnlyList<string>? palette = null)
    {
        var reihen = Reihen(werte, reihennamen);
        if (reihen.Count == 0) return null;

        var diagramm = new TdChart(art, breiteCm, hoeheCm)
        {
            Title = titel?.Trim() ?? "",
            Categories = Beschriftungen(kategorien),
            Series = reihen,
        };

        // Die Standardpalette wird **nicht** mitgeschrieben: `TdChart.Farbe` fällt von selbst
        // auf sie zurück, und eine Kopie in jedem Diagramm hieße, dass eine spätere Änderung
        // der Vorgabe an allen Bestandsdiagrammen vorbeiginge.
        if (palette is { Count: > 0 } && !palette.SequenceEqual(TdChart.StandardPalette))
            diagramm.Palette = [.. palette];

        return diagramm;
    }

    // ==================== Der Rückweg: aus den Zahlen wieder Text ====================

    /// <summary>
    /// Die Werte als Text, <b>je Reihe eine Zeile</b> — die Umkehrung von
    /// <see cref="Reihen"/>.
    /// <para>
    /// <b>Geschrieben wird mit Punkt und Komma-Trenner, unabhängig vom Gebietsschema.</b> Core
    /// fragt die Kultur so wenig wie die Uhr (§4.20): Sonst hinge der Text, den ein Wächter
    /// vergleicht, an der Spracheinstellung des Rechners, auf dem er läuft. Wer sein Diagramm
    /// deutsch eingegeben hat, sieht es beim Wiederöffnen also mit Punkt — <b>gelesen wird
    /// beides</b>, die Eingabe geht dadurch nicht verloren.
    /// </para>
    /// </summary>
    public static string WerteText(TdChart diagramm) =>
        string.Join("\n", diagramm.Series.Select(
            r => string.Join(", ", r.Values.Select(TdChartLayout.Zahl))));

    /// <summary>Die Kategorien als Text — die Umkehrung von <see cref="Beschriftungen"/>.</summary>
    public static string KategorienText(TdChart diagramm) =>
        string.Join(", ", diagramm.Categories);

    /// <summary>
    /// Die Reihennamen als Text.
    /// <para>
    /// <b>Leer, solange keine Reihe einen Namen trägt.</b> Sonst stünden im Feld nur Kommas,
    /// und die sähen wie eine Eingabe aus, die jemand gemacht hat.
    /// </para>
    /// </summary>
    public static string NamenText(TdChart diagramm) =>
        diagramm.Series.Any(r => r.Name.Length > 0)
            ? string.Join(", ", diagramm.Series.Select(r => r.Name))
            : "";
}
