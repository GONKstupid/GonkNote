using WeCantSpell.Hunspell;

namespace GonkNote.Core.Text;

/// <summary>Ein angestrichenes Wort: wo es anfängt und wie lang es ist, in Zeichen.</summary>
/// <remarks>
/// <b>Die Zählung ist die des Absatztextes</b> und damit dieselbe wie
/// <see cref="TdLaidOutRun.Linear"/> — nur so lässt sich eine Fundstelle ohne zweite
/// Rechnung auf ein gesetztes Stück abbilden.
/// </remarks>
public readonly record struct TdFehlstelle(int Start, int Laenge)
{
    public int Ende => Start + Laenge;
}

/// <summary>
/// Die Rechtschreibprüfung — Phase 5.1, das eine benannte Loch aus M2 (HANDOFF §5 Nr. 22).
///
/// <para>
/// <b>Sie steht in Core und nicht im Linux-Kopf.</b> Der Zeichner malt die Wellenlinien
/// (<see cref="Rendering.TdRenderer"/>), und der steht hier; ein Prüfer im Kopf hieße, die
/// Fundstellen über zwei Schichten nach unten zu reichen. Derselbe Grund wie beim Umbruch
/// (§4.16).
/// </para>
///
/// <para>
/// <b>Warum Hunspell in C# und nicht die Windows-Rechtschreib-API oder libenchant.</b>
/// <see cref="Platform.ISpellChecker"/> beantwortet bis heute nur die Sprachfrage, weil die
/// Markierungen drüben die WPF-<c>RichTextBox</c> zeichnet — unter Linux zeichnet sie
/// niemand. Eine native Bibliothek wäre der zweite Tesseract-Fall gewesen (Abhängigkeiten
/// einsammeln, SONAME richten, LD_LIBRARY_PATH setzen, §4.98). Verwaltetes Hunspell hat
/// davon nichts und trägt denselben Code unter Windows und später unter iPadOS.
/// </para>
///
/// <para>
/// <b>Fehlt ein Wörterbuch, fehlt die Prüfung — nicht die App.</b> Dieselbe Regel wie bei
/// der Texterkennung (§4.64): <see cref="Verfuegbar"/> sagt ehrlich nein, und der Kopf
/// blendet den Schalter aus, statt still nichts anzustreichen.
/// </para>
/// </summary>
public static class TdRechtschreibung
{
    /// <summary>Der Ordnername neben dem Programm — wie <c>Fonts/</c> bei den Schriften.</summary>
    public const string Ordner = "Dictionaries";

    /// <summary>
    /// Wo die mitgelieferten Wörterbücher liegen. Über <see cref="AppContext.BaseDirectory"/>
    /// und nicht über das Arbeitsverzeichnis — das steht beim Start über eine Verknüpfung
    /// woanders. Setzbar für den Test.
    /// </summary>
    public static string WoerterbuchOrdner { get; set; } =
        Path.Combine(AppContext.BaseDirectory, Ordner);

    /// <summary>
    /// Die Sprachen, die mitgeliefert werden — BCP-47, so wie die Statusleiste sie führt.
    /// <b>Die Liste steht hier und wird nicht aus dem Ordner gelesen:</b> Was ausgeliefert
    /// wird, ist eine Entscheidung und kein Fund. Fehlt eine Datei dazu, meldet
    /// <see cref="Verfuegbar"/> das — dann ist der Ausgabeordner unvollständig.
    /// </summary>
    public static IReadOnlyList<string> Sprachen { get; } = ["de-DE", "en-US"];

    // ==================== Die Registratur der geladenen Wörterbücher ====================

    private static readonly Lock _tor = new();
    private static readonly Dictionary<string, WordList?> _geladen = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gibt es für diese Sprache ein Wörterbuch? <b>Lädt es dabei</b> — die Frage ist genau
    /// dann mit Ja zu beantworten, wenn das Laden geklappt hat, und nicht schon dann, wenn
    /// eine Datei da liegt.
    /// </summary>
    public static bool Verfuegbar(string? bcp47) => Liste(bcp47) is not null;

    /// <summary>
    /// Die falsch geschriebenen Wörter in einem Absatztext.
    /// <para>
    /// Ohne Wörterbuch eine leere Liste — <b>nicht</b> „alles falsch". Ein fehlendes
    /// Wörterbuch ist kein Befund über den Text.
    /// </para>
    /// </summary>
    public static IReadOnlyList<TdFehlstelle> Fehler(string? text, string? bcp47)
    {
        if (string.IsNullOrEmpty(text)) return [];
        if (Liste(bcp47) is not { } woerter) return [];

        var schluessel = (text, Datei(bcp47) ?? "");
        lock (_tor)
        {
            if (_fundeCache.TryGetValue(schluessel, out var da)) return da;
        }

        List<TdFehlstelle>? funde = null;
        foreach (var (start, laenge) in Woerter(text))
        {
            string wort = text.Substring(start, laenge);
            if (woerter.Check(wort)) continue;
            (funde ??= []).Add(new TdFehlstelle(start, laenge));
        }

        IReadOnlyList<TdFehlstelle> ergebnis = funde ?? (IReadOnlyList<TdFehlstelle>)[];
        lock (_tor)
        {
            // ponytail: platt gedeckelter Zwischenspeicher statt LRU — beim Überlauf fliegt
            // alles raus. Ein Dokument hat Hunderte Absätze, nicht Hunderttausende; wird das
            // je knapp, ist eine echte LRU der Weg.
            if (_fundeCache.Count > 2048) _fundeCache.Clear();
            _fundeCache[schluessel] = ergebnis;
        }
        return ergebnis;
    }

    /// <summary>
    /// Dieselbe Frage für einen Absatz. <b>Gezählt wird in Cursorschritten</b>
    /// (<see cref="TdCursor.AbsatzText"/>) — nur so passen die Fundstellen ohne zweite
    /// Rechnung zu <see cref="TdLaidOutRun.Linear"/>, und ein Bild im Absatz verschiebt die
    /// Wellenlinie nicht.
    /// </summary>
    public static IReadOnlyList<TdFehlstelle> Fehler(TdParagraph? absatz, string? bcp47) =>
        absatz is null ? [] : Fehler(TdCursor.AbsatzText(absatz), bcp47);

    /// <summary>Vorschläge für ein falsch geschriebenes Wort — höchstens <paramref name="hoechstens"/>.</summary>
    public static IReadOnlyList<string> Vorschlaege(string? wort, string? bcp47, int hoechstens = 7)
    {
        if (string.IsNullOrWhiteSpace(wort)) return [];
        if (Liste(bcp47) is not { } woerter) return [];
        return [.. woerter.Suggest(wort).Take(hoechstens)];
    }

    /// <summary>
    /// Wirft alles Geladene weg. Für den Test und für einen Wechsel des
    /// <see cref="WoerterbuchOrdner"/> — ohne das bliebe ein später gesetzter Pfad
    /// wirkungslos, statt sichtbar zu scheitern (dieselbe Falle wie bei
    /// <c>WbFonts.Schema</c>, §4.26).
    /// </summary>
    public static void Vergessen()
    {
        lock (_tor)
        {
            _geladen.Clear();
            _fundeCache.Clear();
        }
    }

    private static readonly Dictionary<(string Text, string Sprache), IReadOnlyList<TdFehlstelle>> _fundeCache = new();

    // ==================== Laden ====================

    private static WordList? Liste(string? bcp47)
    {
        if (Datei(bcp47) is not { } stamm) return null;

        lock (_tor)
        {
            if (_geladen.TryGetValue(stamm, out var da)) return da;

            WordList? liste = null;
            string dic = Path.Combine(WoerterbuchOrdner, stamm + ".dic");
            string aff = Path.Combine(WoerterbuchOrdner, stamm + ".aff");

            // **Ein unvollständiger Ausgabeordner ist kein Programmierfehler** (§4.21) — hier
            // gilt dasselbe wie bei einem fehlenden Schriftschnitt: nicht werfen, nichts
            // anstreichen, und `Verfuegbar` sagt nein.
            if (File.Exists(dic) && File.Exists(aff))
            {
                try { liste = WordList.CreateFromFiles(dic, aff); }
                catch (IOException) { }
                catch (InvalidDataException) { }
            }

            // Auch das Nein wird gemerkt: sonst wird bei jedem Tastendruck erneut im
            // Dateisystem gesucht.
            _geladen[stamm] = liste;
            return liste;
        }
    }

    /// <summary>
    /// BCP-47 → Dateistamm. <c>de-DE</c> wird zu <c>de_DE</c>; was es nicht genau gibt, fällt
    /// auf die erste mitgelieferte Sprache **derselben Sprachfamilie** zurück — <c>de-AT</c>
    /// bekommt so <c>de_DE</c>, und das ist besser als gar keine Prüfung.
    /// </summary>
    private static string? Datei(string? bcp47)
    {
        if (string.IsNullOrWhiteSpace(bcp47)) return null;

        string wunsch = bcp47.Replace('-', '_');
        foreach (var s in Sprachen)
            if (string.Equals(s.Replace('-', '_'), wunsch, StringComparison.OrdinalIgnoreCase))
                return s.Replace('-', '_');

        string familie = wunsch.Split('_')[0];
        foreach (var s in Sprachen)
            if (s.StartsWith(familie + "-", StringComparison.OrdinalIgnoreCase))
                return s.Replace('-', '_');

        return null;
    }

    // ==================== Wortgrenzen ====================

    /// <summary>
    /// Zerlegt einen Text in prüfbare Wörter.
    ///
    /// <para>
    /// <b>Ein Wort ist eine Folge von Buchstaben</b>, innen mit Apostroph
    /// (<c>don't</c>, <c>Schiller's</c>). Der Bindestrich trennt: Hunspell prüft
    /// <c>Donau-Dampfschiff</c> nicht als Ganzes, seine beiden Hälften aber schon.
    /// </para>
    /// <para>
    /// <b>Was Ziffern enthält, wird übersprungen</b> — <c>A4</c>, <c>x2</c>, <c>COVID19</c>
    /// sind keine Rechtschreibfrage, und sie alle anzustreichen wäre die schnellste Art, die
    /// Prüfung unbrauchbar zu machen.
    /// </para>
    /// </summary>
    private static IEnumerable<(int Start, int Laenge)> Woerter(string text)
    {
        int i = 0;
        while (i < text.Length)
        {
            if (!char.IsLetter(text[i])) { i++; continue; }

            int start = i;
            bool ziffer = false;
            while (i < text.Length)
            {
                char c = text[i];
                if (char.IsLetter(c)) { i++; continue; }
                if (char.IsDigit(c)) { ziffer = true; i++; continue; }
                // Ein Apostroph zählt nur **zwischen** Buchstaben mit.
                if ((c is '\'' or '’') && i + 1 < text.Length && char.IsLetter(text[i + 1])) { i++; continue; }
                break;
            }

            if (!ziffer) yield return (start, i - start);
        }
    }
}
