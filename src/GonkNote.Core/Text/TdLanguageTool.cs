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
/// <b>Warum er nicht mitgeliefert wird.</b> LanguageTool ist Java. Mitliefern hieße eine
/// Laufzeitumgebung ins AppImage zu legen und aus 88 MB rund 600 MB zu machen — für eine
/// Funktion, die ohne sie nicht ausfällt, sondern nur schmaler wird. Deshalb dieselbe
/// Staffelung wie bei der Texterkennung (§4.64): <b>Ist einer da, wird er benutzt; ist keiner
/// da, wird das gesagt</b> und die festen Regeln tragen weiter.
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
        if (Verfuegbar is false) return [];
        if (!Erlaubt(Adresse)) { Verfuegbar = false; return []; }

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
    }

    // ==================== Der Hintergrund ====================

    private static async Task Holen((string Text, string Sprache) schluessel)
    {
        IReadOnlyList<TdFehlstelle> ergebnis = [];

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
        catch (HttpRequestException) { Verfuegbar = false; }
        catch (TaskCanceledException) { Verfuegbar = false; }
        catch (JsonException) { Verfuegbar = true; }

        lock (_tor)
        {
            // ponytail: platt gedeckelt wie in TdRechtschreibung — beim Überlauf fliegt alles
            // raus. Eine echte LRU erst, wenn ein Dokument das je erreicht.
            if (_befunde.Count > 2048) _befunde.Clear();
            _befunde[schluessel] = ergebnis;
            _unterwegs.Remove(schluessel);
        }

        Fertig?.Invoke();
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
