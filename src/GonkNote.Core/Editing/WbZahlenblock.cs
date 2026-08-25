namespace GonkNote.Core.Editing;

/// <summary>
/// Der Zahlenblock an der Werkzeuggröße (Vorbild Adobe Fresco): langes Drücken auf Schieber,
/// Wertanzeige oder Symbol öffnet ein kleines Ziffernfeld, und was dort getippt wird, geht
/// direkt auf die Strichstärke bzw. — beim Radierer — auf dessen Größe.
///
/// <para>
/// <b>Warum das hier steht und nicht im Kopf.</b> Bis Phase 4.5 lag es privat in
/// <c>WhiteboardView.Numpad.cs</c> des WPF-Kopfs. Der Aufklappteil ist Oberfläche und bleibt
/// dort; <b>was hier steht, ist die Rechnung dahinter</b> — welche Taste die Eingabe wie
/// verändert, wann sie abgelehnt wird, und ab wann ein Drücken ein Ziehen ist. Beides sind
/// Regeln über Zeichenketten und Zahlen, und beide Köpfe brauchen dieselben.
/// </para>
///
/// <para>
/// <b>Das Komma ist das Anzeigezeichen und nicht das Rechenzeichen.</b> Die Tasten des Blocks
/// zeigen ein Komma, weil deutsche Nutzer eines erwarten; gerechnet wird über
/// <see cref="System.Globalization.CultureInfo.InvariantCulture"/> mit Punkt. Wer das
/// vermischt, bekommt auf einem englischen System eine Zahl, die zehnmal zu groß ist.
/// </para>
/// </summary>
public static class WbZahlenblock
{
    /// <summary>So lange muss gedrückt bleiben, bis der Block aufgeht.</summary>
    public static readonly TimeSpan Haltedauer = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Ab dieser Bewegung ist es ein Ziehen am Schieber und kein Langdruck. <b>Ohne diese
    /// Schwelle wäre der Schieber unbenutzbar:</b> jedes langsame Ziehen klappte den Block auf.
    /// </summary>
    public const double Spielraum = 8;

    /// <summary>Das Zeichen, das die Tasten des Blocks für den Dezimaltrenner zeigen.</summary>
    public const char Komma = ',';

    /// <summary>
    /// Hat sich der Zeiger seit dem Aufsetzen weit genug bewegt, dass es ein Ziehen ist?
    /// </summary>
    public static bool IstZiehen(double startX, double startY, double x, double y) =>
        Math.Abs(x - startX) > Spielraum || Math.Abs(y - startY) > Spielraum;

    /// <summary>
    /// Eine Zifferntaste (oder das Komma) auf die bisherige Eingabe anwenden.
    /// <para>
    /// Gibt die neue Eingabe zurück — oder <c>null</c>, wenn die Taste <b>abgelehnt</b> wird.
    /// Abgelehnt wird sie in drei Fällen: ein zweites Komma, eine zweite Nachkommastelle, und
    /// jede Eingabe über <paramref name="hoechstwert"/>. <b>Der dritte Fall ist der wichtige:</b>
    /// eine zu große Zahl wird gar nicht erst angenommen, sonst stünde in der Anzeige etwas
    /// anderes als die tatsächlich eingestellte (geklemmte) Größe.
    /// </para>
    /// </summary>
    public static string? Taste(string eingabe, string taste, double hoechstwert)
    {
        string naechste;

        if (taste.Length == 1 && taste[0] == Komma)
        {
            if (eingabe.Contains(Komma)) return null;
            naechste = (eingabe.Length == 0 ? "0" : eingabe) + Komma;
        }
        else
        {
            int komma = eingabe.IndexOf(Komma);
            if (komma >= 0 && eingabe.Length - komma > 1) return null;   // eine Nachkommastelle
            naechste = eingabe + taste;
        }

        return Wert(naechste) > hoechstwert ? null : naechste;
    }

    /// <summary>Ein Zeichen zurücknehmen; auf leerer Eingabe passiert nichts.</summary>
    public static string Rueckschritt(string eingabe) =>
        eingabe.Length > 0 ? eingabe[..^1] : eingabe;

    /// <summary>
    /// Was die Anzeige über dem Block zeigt. Eine leere Eingabe zeigt „0" und nicht nichts —
    /// ein leeres Feld sieht aus wie ein Fehler.
    /// </summary>
    public static string Anzeige(string eingabe) => eingabe.Length > 0 ? eingabe : "0";

    /// <summary>
    /// Der Zahlenwert der Eingabe; <c>null</c>, solange sie keine ergibt.
    /// <para>
    /// <b>Ein angefangenes „0," ergibt 0 und nicht <c>null</c></b> — das Komma wird
    /// abgeschnitten, und „0" ist eine Zahl. Der WPF-Kopf schrieb bis V2-83 das Gegenteil in
    /// seinen Kommentar; gemessen hat es dort nie jemand, weil es folgenlos blieb:
    /// <see cref="Schieberwert"/> klemmt die 0 ohnehin auf den Mindestwert hoch. Hier steht,
    /// was der Code tut. Wächter: <c>ZahlenblockTests</c>.
    /// </para>
    /// </summary>
    public static double? Wert(string eingabe) =>
        double.TryParse(eingabe.TrimEnd(Komma).Replace(Komma, '.'),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double v)
            ? v
            : null;

    /// <summary>
    /// Der Wert, der auf den Schieber gesetzt wird — nach unten geklemmt, nach oben nicht:
    /// über den Höchstwert lässt <see cref="Taste"/> es gar nicht erst kommen.
    /// <c>null</c>, solange die Eingabe keine Zahl ergibt.
    /// </summary>
    public static double? Schieberwert(string eingabe, double mindestwert) =>
        Wert(eingabe) is { } v ? Math.Max(v, mindestwert) : null;
}
