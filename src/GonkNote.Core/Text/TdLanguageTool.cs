using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace GonkNote.Core.Text;

/// <summary>
/// Die Brücke zu einem <b>örtlich laufenden</b> LanguageTool-Server — Phase 5.2, der Aufsatz
/// über <see cref="TdGrammatik"/>.
///
/// <para>
/// <b>Warum überhaupt ein fremder Dienst.</b> Echte Grammatikprüfung braucht Wortarten,
/// Kongruenzregeln und einen Wortschatz mit Flexion; das ist kein Regelsatz, das ist ein
/// Projekt. LanguageTool ist genau dieses Projekt, und Deutsch ist seine stärkste Sprache. In
/// .NET gibt es dazu nichts Gleichwertiges — auf NuGet stehen nur Cloud-APIs und Clients für
/// genau diesen Server.
/// </para>
/// <para>
/// <b>Warum er nicht mitgeliefert wird</b> (Nutzerfrage, 2026-09-16 — nachgemessen, nicht
/// geschätzt): LanguageTool 6.6 sind <b>386 MB</b> installiert, dazu eine JRE mit <b>125 MB</b>;
/// das AppImage wüchse von 88 MB auf rund 600 MB, getrimmt auf de+en und mit <c>jlink</c>
/// immer noch auf etwa 300 MB.
/// <para>
/// <b>Das Gewicht ist dabei der harmloseste der vier Gründe.</b> Der eigentliche ist, dass die
/// App damit eine <b>zweite Laufzeitumgebung besäße</b>: einen JVM-Kindprozess starten, einen
/// freien Port suchen, sein Hochfahren abwarten, ihn zuverlässig abräumen und seine Abstürze
/// überleben. Dazu ein halbes bis ein Gigabyte Arbeitsspeicher, eine deutlich größere
/// Prüffläche bei der Flathub-Einreichung — und ein Weg, den iPadOS (Phase 6) ohnehin nicht
/// mitgehen kann, weil dort keine JVM läuft. Die Naht „Server, wenn einer da ist" muss es
/// also sowieso geben.
/// </para>
/// <para>
/// Deshalb dieselbe Staffelung wie bei der Texterkennung (§4.64): <b>Ist einer da, wird er
/// benutzt; ist keiner da, wird das gesagt</b> — im Menü, an der Stelle, an der es jemanden
/// interessiert — und die festen Regeln tragen weiter.
/// </para>
/// <para>
/// <b>⛔ NUR DER EIGENE RECHNER.</b> Geprüft wird ausschließlich gegen einen Server auf dem
/// Loopback-Gerät — <see cref="Erlaubt"/> lässt nichts anderes durch, auch nicht über die
/// Umgebungsvariable. Das ist keine Vorsichtsmaßnahme, sondern die Zusage des Programms:
/// „Deine Daten liegen nur auf diesem Rechner." Ein Grammatikdienst bekommt den <b>Text des
/// Dokuments</b> zu sehen; wohin der geht, ist deshalb keine Einstellung, sondern eine
/// Festlegung. Die öffentliche API von languagetool.org wird bewusst <b>nicht</b> unterstützt.
/// </para>
/// <para>
/// <b>Gefragt wird nie im Zeichenweg.</b> <see cref="Befunde"/> antwortet sofort aus dem
/// Zwischenspeicher und stößt sonst eine Abfrage im Hintergrund an; kommt sie zurück, meldet
/// sich <see cref="Fertig"/>, und der Kopf zeichnet neu. Ein HTTP-Aufruf mitten im Malen einer
/// Seite wäre ein stehendes Fenster bei jedem Tastendruck.
/// </para>
/// </summary>
public static class TdLanguageTool
{
    /// <summary>
    /// Der Server. Vorgabe ist der Standardport von <c>languagetool --http</c>; über
    /// <c>GONKNOTE_LANGUAGETOOL</c> lässt sich ein anderer <b>Port</b> setzen — der Rechner
    /// nicht, siehe <see cref="Erlaubt"/>.
    /// </summary>
    public static Uri Adresse { get; set; } =
        new(Environment.GetEnvironmentVariable("GONKNOTE_LANGUAGETOOL") is { Length: > 0 } eigen
            ? eigen
            : "http://localhost:8081");

    /// <summary>
    /// Ist das eine Adresse auf diesem Rechner? <b>Alles andere wird abgelehnt</b>, und zwar
    /// still und endgültig: Wer die Umgebungsvariable auf einen fremden Rechner stellt,
    /// bekommt keine Grammatikprüfung und nicht etwa eine, die sein Dokument verschickt.
    /// </summary>
    public static bool Erlaubt(Uri adresse) =>
        adresse.IsLoopback &&
        adresse.Scheme is "http" or "https";

    /// <summary>
    /// <c>null</c> = noch nicht nachgesehen, <c>true</c>/<c>false</c> = nachgesehen. Die
    /// Unterscheidung ist wichtig für den Kopf: „noch nicht gefragt" ist kein „nein".
    /// </summary>
    public static bool? Verfuegbar { get; private set; }

    /// <summary>
    /// Meldet sich, wenn neue Befunde da sind — oder wenn feststeht, dass es keinen Server
    /// gibt. <b>Läuft auf einem fremden Faden</b>; wer die Oberfläche anfasst, geht über
    /// seinen Planer (<c>IUiScheduler</c>).
    /// </summary>
    public static event Action? Fertig;

    /// <summary>
    /// Wie lange ein „kein Server da" gilt, bevor erneut nachgesehen wird.
    ///
    /// <para>
    /// <b>Ohne diese Wiederholung wäre der Hinweis im Menü eine Lüge:</b> Er sagt „starte
    /// LanguageTool" — und ohne sie passierte danach nichts bis zum nächsten Programmstart,
    /// weil das erste Nein für immer gegolten hätte.
    /// </para>
    /// <para>
    /// <b>Gemessen wird verstrichene Zeit und nicht die Uhrzeit</b> (<see cref="Stopwatch"/>),
    /// und das ist die Antwort auf §4.20 „Core fragt die Uhr nicht selbst": Diese Zahl geht in
    /// kein Dokument, in keinen Export und in kein Bild — sie entscheidet nur, ob ein Rechner
    /// noch einmal gefragt wird. Eine Wanduhr, die zurückspringt, dürfte das nicht
    /// durcheinanderbringen.
    /// </para>
    /// </summary>
    public static TimeSpan Wiederholung { get; set; } = TimeSpan.FromSeconds(30);

    private static long _letzterFehlschlag;

    private static readonly Lock _tor = new();
    private static readonly Dictionary<(string Text, string Sprache), IReadOnlyList<TdFehlstelle>> _befunde = new();
    private static readonly HashSet<(string Text, string Sprache)> _unterwegs = [];

    // Eine einzige Verbindung für das ganze Programm. Ein HttpClient je Abfrage lässt unter
    // Last Sockets in TIME_WAIT zurück — der klassische Fehler mit dieser Klasse.
    private static readonly HttpClient _draht = new() { Timeout = TimeSpan.FromSeconds(8) };

    /// <summary>
    /// Die Befunde zu einem Absatztext, <b>soweit sie schon bekannt sind</b>. Sind sie es
    /// nicht, kommt eine leere Liste zurück und die Abfrage läuft an; das Ergebnis meldet
    /// <see cref="Fertig"/>.
    /// </summary>
    public static IReadOnlyList<TdFehlstelle> Befunde(string? text, string? bcp47)
    {
        if (string.IsNullOrWhiteSpace(text) || bcp47 is not { Length: > 0 }) return [];
        // Die fremde Adresse bleibt endgültig nein — daran ändert kein Abwarten etwas.
        if (!Erlaubt(Adresse)) { Verfuegbar = false; return []; }

        if (Verfuegbar is false)
        {
            if (Stopwatch.GetElapsedTime(_letzterFehlschlag) < Wiederholung) return [];
            Verfuegbar = null;
        }

        var schluessel = (text, bcp47);

        lock (_tor)
        {
            if (_befunde.TryGetValue(schluessel, out var da)) return da;
            if (!_unterwegs.Add(schluessel)) return [];
        }

        _ = Holen(schluessel);
        return [];
    }

    /// <summary>Wirft alles Gemerkte weg — für den Test und nach einem Serverwechsel.</summary>
    public static void Vergessen()
    {
        lock (_tor)
        {
            _befunde.Clear();
            _unterwegs.Clear();
        }
        Verfuegbar = null;
        _letzterFehlschlag = 0;
    }

    // ==================== Der Hintergrund ====================

    private static async Task Holen((string Text, string Sprache) schluessel)
    {
        IReadOnlyList<TdFehlstelle> ergebnis = [];
        bool geklappt = true;

        try
        {
            var antwort = await _draht.PostAsync(
                new Uri(Adresse, "/v2/check"),
                new FormUrlEncodedContent(
                [
                    new("text", schluessel.Text),
                    new("language", schluessel.Sprache),
                    // Die Rechtschreibung macht Hunspell. Ohne diese Zeile stünde jedes
                    // unbekannte Wort unter zwei Wellen.
                    new("disabledCategories", "TYPOS"),
                ])).ConfigureAwait(false);

            if (antwort.IsSuccessStatusCode)
            {
                ergebnis = Lesen(await antwort.Content.ReadAsStringAsync().ConfigureAwait(false));
                Verfuegbar = true;
            }
            else
            {
                // Ein Server, der antwortet, aber nicht mit Ja: Er ist da, dieser eine Text
                // ging schief. Das ist kein Grund, die Prüfung für immer abzuschalten.
                Verfuegbar = true;
            }
        }
        catch (JsonException)
        {
            // Der Server ist da und hat geantwortet, nur nicht lesbar. Das ist eine Auskunft
            // über diesen einen Text, nicht über den Server.
            Verfuegbar = true;
        }
        catch (Exception)
        {
            // ⛔ **Jede andere Ausnahme ist ein Fehlschlag — nicht nur die zwei erwarteten.**
            // Bis 2026-09-17 standen hier `HttpRequestException` und
            // `TaskCanceledException`, und das war eine Annahme. Gemessen gegen einen Server,
            // der die Verbindung abreißt: `HttpClient` wirft dann **mal eine
            // `HttpRequestException`, mal eine nackte `SocketException`**. Die zweite lief an
            // beiden Fängen vorbei, die Aufgabe starb still — sie wird ja nicht abgewartet —,
            // und die Anfrage blieb für immer als „unterwegs" stehen. Dieser Absatz wäre bis
            // zum Neustart nie wieder geprüft worden, und `Fertig` hätte sich nie gemeldet.
            // Aufgefallen ist es am Wächter, nicht im Betrieb.
            Fehlgeschlagen();
            geklappt = false;
        }
        finally
        {
            Abschliessen(schluessel, ergebnis, geklappt);
        }
    }

    /// <summary>
    /// Trägt das Ergebnis ein und meldet es. <b>Steht im <c>finally</c></b>: Eine Abfrage, die
    /// hier nicht ankommt, bleibt als „unterwegs" liegen, und ihr Text wird nie wieder
    /// gefragt — deshalb darf kein Weg daran vorbeiführen.
    /// </summary>
    private static void Abschliessen(
        (string Text, string Sprache) schluessel, IReadOnlyList<TdFehlstelle> ergebnis, bool geklappt)
    {
        lock (_tor)
        {
            // ⚠ **Ein Fehlschlag wird NICHT gemerkt**, und das ist der Sinn von
            // <see cref="Wiederholung"/>: Läge hier nach einem gescheiterten Versuch eine
            // leere Liste im Zwischenspeicher, käme die Wiederholung nie bis zur Abfrage —
            // sie träfe vorher auf den Eintrag und gäbe ihn zurück. Genau so war es beim
            // ersten Wurf, und es hätte den Hinweis im Menü wirkungslos gemacht.
            if (geklappt)
            {
                // ponytail: platt gedeckelt wie in TdRechtschreibung — beim Überlauf fliegt
                // alles raus. Eine echte LRU erst, wenn ein Dokument das je erreicht.
                if (_befunde.Count > 2048) _befunde.Clear();
                _befunde[schluessel] = ergebnis;
            }

            _unterwegs.Remove(schluessel);
        }

        Fertig?.Invoke();
    }

    private static void Fehlgeschlagen()
    {
        Verfuegbar = false;
        _letzterFehlschlag = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Aus der Antwort werden Fundstellen. <b>Rechtschreibbefunde fliegen raus</b> — dafür ist
    /// Hunspell zuständig, und zwei Wellen unter demselben Wort sind keine doppelte Gründlichkeit,
    /// sondern ein Fehler.
    /// </summary>
    private static IReadOnlyList<TdFehlstelle> Lesen(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("matches", out var treffer)) return [];

        var funde = new List<TdFehlstelle>();

        foreach (var t in treffer.EnumerateArray())
        {
            if (!t.TryGetProperty("offset", out var o) || !t.TryGetProperty("length", out var l)) continue;

            if (t.TryGetProperty("rule", out var regel) &&
                regel.TryGetProperty("issueType", out var art) &&
                art.GetString() is "misspelling" or "whitespace")
                continue;

            var vorschlaege = new List<string>();
            if (t.TryGetProperty("replacements", out var ersatz))
                foreach (var e in ersatz.EnumerateArray())
                {
                    if (e.TryGetProperty("value", out var v) && v.GetString() is { Length: > 0 } wert)
                        vorschlaege.Add(wert);
                    if (vorschlaege.Count == 7) break;
                }

            funde.Add(new TdFehlstelle(
                o.GetInt32(), l.GetInt32(), TdBefundArt.Grammatik,
                // **Der fertige Satz und kein Loc-Schlüssel.** `Loc.T` gibt unbekannte
                // Schlüssel unverändert zurück, deshalb trägt dasselbe Feld beides: den
                // Schlüssel der eigenen Regeln und den Text des Servers, der ohnehin schon
                // in der geprüften Sprache steht.
                t.TryGetProperty("message", out var m) ? m.GetString() : null,
                vorschlaege));
        }

        return funde;
    }
}
