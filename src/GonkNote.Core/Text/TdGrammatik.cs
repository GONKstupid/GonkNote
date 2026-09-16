namespace GonkNote.Core.Text;

/// <summary>
/// Die Grammatikprüfung — Phase 5.2, der Nachsatz zu 5.1 (Nutzer, 2026-09-16: „Grammatik noch
/// überhaupt nicht").
///
/// <para>
/// <b>⚠ Was das hier ist und was es nicht ist — und das gehört an den Anfang, nicht in eine
/// Fußnote.</b> Dies ist <b>keine</b> Grammatikprüfung im Sinne von LanguageTool. Es ist ein
/// Satz fester Regeln über Zeichen und Wortfolgen. Was eine Wortarterkennung bräuchte, prüft
/// er <b>nicht</b>: Kongruenz („ein Haus" / „eine Haus"), Fälle, Zeiten, Satzbau. Eine Regel,
/// die so etwas ohne Wortarten behauptet, wäre geraten — und geratene Anstreichungen sind
/// schlimmer als keine, weil sie die richtigen mit entwerten.
/// </para>
/// <para>
/// <b>Was er dafür sicher kann</b>, sind die Flüchtigkeitsfehler, die beim Schreiben am
/// laufenden Band entstehen und die man beim Lesen überfliegt: dasselbe Wort zweimal,
/// Leerzeichen vor dem Komma, ein Satz, der klein anfängt.
/// </para>
/// <para>
/// <b>Er ist der Boden, nicht die Decke.</b> Darüber liegt <see cref="TdLanguageTool"/>: Läuft
/// auf dem Rechner ein LanguageTool-Server, kommen dessen Befunde dazu und die echte Grammatik
/// mit ihnen. Läuft keiner, bleibt es bei diesen Regeln — und die laufen immer, ohne
/// Vorbedingung, ohne Netz. Dieselbe Staffelung wie bei der Texterkennung (§4.64): Was da ist,
/// wird benutzt; was fehlt, wird gesagt und nicht verschwiegen.
/// </para>
/// <para>
/// <b>Die Regeln stehen in Core und nicht im Kopf</b>, aus demselben Grund wie
/// <see cref="TdRechtschreibung"/>: Angestrichen wird vom Zeichner, und der steht hier.
/// </para>
/// </summary>
public static class TdGrammatik
{
    /// <summary>
    /// Die Sprachen, für die es Regeln gibt. <b>Die Satzzeichen-Regeln gälten für jede
    /// Sprache</b> — angeboten werden sie trotzdem nur für die beiden, die auch ein
    /// Wörterbuch haben: Eine Prüfung, die für Französisch die halbe Wahrheit sagt, sagt
    /// schlechter Bescheid als eine, die gar nicht erst antritt.
    /// </summary>
    public static IReadOnlyList<string> Sprachen { get; } = ["de-DE", "en-US"];

    /// <summary>Gibt es für diese Sprache Regeln?</summary>
    public static bool Verfuegbar(string? bcp47) =>
        bcp47 is { Length: > 0 } &&
        Sprachen.Any(s => s.Split('-')[0].Equals(bcp47.Split('-')[0], StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Die Befunde in einem Absatztext — <b>nur die aus den festen Regeln</b>. Was
    /// LanguageTool zusätzlich findet, holt <see cref="TdLanguageTool"/>; zusammengelegt wird
    /// beides in <see cref="TdPruefung"/>.
    /// </summary>
    public static IReadOnlyList<TdFehlstelle> Fehler(string? text, string? bcp47)
    {
        if (string.IsNullOrEmpty(text) || !Verfuegbar(bcp47)) return [];

        var funde = new List<TdFehlstelle>();
        Wortdopplung(text, funde);
        Satzzeichen(text, funde);
        Satzanfang(text, funde);

        // Nach Ort sortiert, damit das Menü sie in der Reihenfolge anbietet, in der sie im
        // Satz stehen — und nicht in der, in der die Regeln zufällig laufen.
        funde.Sort((a, b) => a.Start.CompareTo(b.Start));
        return funde;
    }

    /// <inheritdoc cref="Fehler(string?, string?)"/>
    public static IReadOnlyList<TdFehlstelle> Fehler(TdParagraph? absatz, string? bcp47) =>
        absatz is null ? [] : Fehler(TdCursor.AbsatzText(absatz), bcp47);

    // ==================== Die Regeln ====================

    /// <summary>
    /// <b>Dasselbe Wort zweimal hintereinander.</b> Der häufigste Tippfehler, den kein
    /// Wörterbuch findet: Beide Wörter sind richtig geschrieben.
    ///
    /// <para>
    /// <b>Ein Komma davor hebt die Regel auf</b>, und das ist keine Feinheit: „der Mann, der
    /// der Frau half" ist richtig, und ohne diese Ausnahme stünde jeder Relativsatz dieser
    /// Bauart angestrichen da. Dasselbe gilt für „das, das" und „die, die".
    /// </para>
    /// </summary>
    private static void Wortdopplung(string text, List<TdFehlstelle> funde)
    {
        var woerter = Woerter(text);

        for (int i = 1; i < woerter.Count; i++)
        {
            var (vorStart, vorLaenge) = woerter[i - 1];
            var (start, laenge) = woerter[i];

            if (laenge != vorLaenge) continue;
            if (!text.AsSpan(vorStart, vorLaenge).Equals(text.AsSpan(start, laenge), StringComparison.CurrentCultureIgnoreCase))
                continue;

            // Dazwischen darf **nur** Weißraum stehen. Steht dort ein Satzzeichen, sind es
            // zwei Sätze und keine Dopplung: „Das ist das. Das war es."
            if (!text.AsSpan(vorStart + vorLaenge, start - (vorStart + vorLaenge)).IsWhiteSpace()) continue;

            // Der Relativsatz-Ausweg: ein Komma unmittelbar vor dem ersten der beiden Wörter.
            if (KommaDavor(text, vorStart)) continue;

            funde.Add(new TdFehlstelle(
                vorStart, start + laenge - vorStart, TdBefundArt.Grammatik,
                "Gram.Doppeltes.Wort",
                [text.Substring(start, laenge)]));
        }
    }

    private static bool KommaDavor(string text, int wortStart)
    {
        for (int i = wortStart - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(text[i])) continue;
            return text[i] is ',';
        }
        return false;
    }

    /// <summary>
    /// <b>Leerzeichen vor dem Satzzeichen, fehlendes dahinter, und das doppelte Leerzeichen.</b>
    ///
    /// <para>
    /// <b>Die Abkürzung ist die Falle dieser Regel</b>, und sie wird namentlich umgangen: In
    /// „z.B." folgt auf einen Punkt ein Großbuchstabe, und genau das wäre sonst der Befund
    /// „hier fehlt ein Leerzeichen". Steht vor dem Punkt ein <b>einzelner</b> Buchstabe oder
    /// eine Ziffer, hält die Regel still — das deckt „z.B.", „u.a.", „1.000" und jede
    /// Gliederungsnummer ab.
    /// </para>
    /// <para>
    /// <b>Die Auslassungspunkte bleiben ebenfalls stehen:</b> „…" und „..." sind kein
    /// doppeltes Satzzeichen.
    /// </para>
    /// </summary>
    private static void Satzzeichen(string text, List<TdFehlstelle> funde)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            // ---- Leerzeichen vor dem Satzzeichen ----
            // **Die Auslassung gilt auch hier**, und das ist an einem gefallenen Wächter
            // gemessen: „dann ... kam" hat ein Leerzeichen vor dem ersten der drei Punkte,
            // und ohne diese Ausnahme stand es angestrichen da.
            if (c is ',' or '.' or ';' or ':' or '!' or '?' && i > 0 && text[i - 1] == ' ' &&
                !Auslassung(text, i))
            {
                // Der ganze Weißraum davor gehört zum Befund, nicht nur das letzte Zeichen.
                int von = i - 1;
                while (von > 0 && text[von - 1] == ' ') von--;

                // Nur, wenn davor überhaupt etwas steht — am Absatzanfang ist es Einzug.
                if (von > 0)
                    funde.Add(new TdFehlstelle(
                        von, i - von + 1, TdBefundArt.Grammatik,
                        "Gram.Leerzeichen.Vor", [c.ToString()]));
            }

            // ---- Fehlendes Leerzeichen dahinter ----
            if (c is ',' or '.' or ';' or ':' or '!' or '?' &&
                i > 0 && i + 1 < text.Length &&
                char.IsLetter(text[i - 1]) && char.IsLetter(text[i + 1]) &&
                !Abkuerzung(text, i) && !Auslassung(text, i))
            {
                funde.Add(new TdFehlstelle(
                    i, 2, TdBefundArt.Grammatik,
                    "Gram.Leerzeichen.Fehlt", [c + " " + text[i + 1]]));
            }

            // ---- Doppeltes Leerzeichen ----
            if (c == ' ' && i + 1 < text.Length && text[i + 1] == ' ')
            {
                int bis = i + 1;
                while (bis + 1 < text.Length && text[bis + 1] == ' ') bis++;

                funde.Add(new TdFehlstelle(
                    i, bis - i + 1, TdBefundArt.Grammatik,
                    "Gram.Leerzeichen.Doppelt", [" "]));
                i = bis;
            }
        }
    }

    /// <summary>
    /// <b>Ein Satz, der klein anfängt</b> — nach Punkt, Ausrufe- oder Fragezeichen.
    ///
    /// <para>
    /// Dieselbe Abkürzungs-Ausnahme wie oben, und eine zweite dazu: <b>nach einer Ziffer
    /// hält die Regel still</b>. „die 1. runde" wäre sonst ein Befund, obwohl dort eine
    /// Ordnungszahl steht und kein Satzende.
    /// </para>
    /// </summary>
    private static void Satzanfang(string text, List<TdFehlstelle> funde)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] is not ('.' or '!' or '?')) continue;
            if (Abkuerzung(text, i) || Auslassung(text, i)) continue;

            int j = i + 1;
            while (j < text.Length && char.IsWhiteSpace(text[j])) j++;
            if (j == i + 1 || j >= text.Length) continue;

            if (!char.IsLower(text[j])) continue;

            int laenge = 0;
            while (j + laenge < text.Length && char.IsLetter(text[j + laenge])) laenge++;

            funde.Add(new TdFehlstelle(
                j, laenge, TdBefundArt.Grammatik,
                "Gram.Satzanfang",
                [char.ToUpper(text[j]) + text.Substring(j + 1, laenge - 1)]));
        }
    }

    // ==================== Ausnahmen und Hilfsmittel ====================

    /// <summary>
    /// Steht vor diesem Punkt ein einzelner Buchstabe oder eine Ziffer? Dann ist es eine
    /// Abkürzung oder eine Gliederungsnummer und kein Satzende.
    /// </summary>
    private static bool Abkuerzung(string text, int punkt)
    {
        if (punkt == 0) return false;
        if (char.IsDigit(text[punkt - 1])) return true;
        if (!char.IsLetter(text[punkt - 1])) return false;

        // Wie viele Buchstaben stehen unmittelbar davor? Genau einer heißt „z.", „u.", „B.".
        int laenge = 0;
        int i = punkt - 1;
        while (i >= 0 && char.IsLetter(text[i])) { laenge++; i--; }
        return laenge == 1;
    }

    /// <summary>Auslassungspunkte — „..." und „…" sind kein Satzende und keine Häufung.</summary>
    private static bool Auslassung(string text, int i) =>
        text[i] == '…' ||
        (text[i] == '.' && ((i + 1 < text.Length && text[i + 1] == '.') || (i > 0 && text[i - 1] == '.')));

    /// <summary>
    /// Wortgrenzen — <b>dieselbe Regel wie in <see cref="TdRechtschreibung"/></b>: Buchstaben,
    /// innen mit Apostroph. Sie steht hier ein zweites Mal, weil sie dort privat ist und ein
    /// öffentlicher Zerleger eine Zusage wäre, die niemand braucht; sie ist fünf Zeilen lang
    /// und ändert sich nicht.
    /// </summary>
    private static List<(int Start, int Laenge)> Woerter(string text)
    {
        var funde = new List<(int, int)>();
        int i = 0;

        while (i < text.Length)
        {
            if (!char.IsLetter(text[i])) { i++; continue; }

            int start = i;
            while (i < text.Length)
            {
                char c = text[i];
                if (char.IsLetter(c)) { i++; continue; }
                if ((c is '\'' or '’') && i + 1 < text.Length && char.IsLetter(text[i + 1])) { i++; continue; }
                break;
            }
            funde.Add((start, i - start));
        }
        return funde;
    }
}
