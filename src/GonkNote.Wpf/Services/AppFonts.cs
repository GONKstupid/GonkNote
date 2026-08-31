using System.IO;
using System.Windows;
using System.Windows.Media;
using GonkNote.Core.Theming;
// `Fonts` gibt es zweimal: hier gemeint ist die Schrifttabelle aus Core, nicht
// System.Windows.Media.Fonts (die Systemschriftliste). Der Alias macht das an jeder
// Fundstelle sichtbar, statt es von der using-Reihenfolge abhaengen zu lassen.
using Schriften = GonkNote.Core.Theming.Fonts;

namespace GonkNote.Services;

/// <summary>
/// Die mitgelieferten Schriften für die <b>Oberfläche</b> des WPF-Kopfs — als
/// <see cref="FontFamily"/> mit einer <b>absoluten</b> Basis.
///
/// <para>
/// <b>Warum das in C# steht und nicht in XAML</b> (HANDOFF §4.71). Bis zum 2026-08-29 hieß es
/// in <c>Themes/Styles.xaml</c> <c>./Fonts/Inter/#Inter, Segoe UI</c>, und der Kommentar
/// daneben sagte, das zeige „auf die losen Dateien neben der Exe". <b>Das hat nie
/// gestimmt.</b> Ein <i>relativer</i> Schrift-URI wird gegen die Basis des XAML aufgelöst,
/// und die ist bei einem eingebundenen Wörterbuch
/// <c>pack://application:,,,/GonkNote;component/Themes/</c> — also <b>in</b> der Assembly,
/// wo keine Schrift liegt. Die App fiel still auf Segoe UI zurück.
/// </para>
/// <para>
/// <b>Der Fehler war deshalb so langlebig, weil der Rückfall funktioniert hat</b> — nur eben
/// immer. Bau grün, Tests grün, und unter Windows sah alles vernünftig aus. Aufgefallen ist
/// es erst, als der Linux-Kopf danebenstand und dieselbe Fläche in Inter zeichnete
/// (§4.71, Schritt ①a). <i>Ein Kopf allein hat kein Maß für sich selbst.</i>
/// </para>
/// <para>
/// <b>Nebenbei fällt eine Doppelschreibung weg.</b> Der alte Kommentar in
/// <c>Styles.xaml</c> begründete die zweite Fassung der Familiennamen damit, dass „XAML keine
/// Konstante aus C# lesen kann". C# kann es — die Namen kommen jetzt aus
/// <see cref="Schriften.Standard"/> und <see cref="Schriften.Mitgeliefert"/>, also aus derselben
/// Tabelle wie im Linux-Kopf und in der Zeichenfläche.
/// </para>
/// </summary>
public static class AppFonts
{
    /// <summary>
    /// Der Ordner des Programms als absolute Basis. <b>Nicht der Ordner des XAML</b> und
    /// nicht <c>pack://siteoforigin:</c> — Letzteres ist ausprobiert worden und hat unter
    /// .NET denselben Rückfall ergeben (§4.71).
    /// </summary>
    public static Uri Basis { get; } = new(AppContext.BaseDirectory, UriKind.Absolute);

    /// <summary>
    /// Die Familie zu einer Rolle: erst die mitgelieferte Datei, dann die Systemschrift.
    /// </summary>
    /// <param name="rolle">Die Rolle aus dem Schema in Core.</param>
    /// <param name="systemRueckfall">
    /// Was gelten soll, wenn die Datei fehlt. <b>Absicht und kein Beiwerk:</b> Ein
    /// unvollständiger Ausgabeordner soll die App nicht in Times New Roman zeichnen. Genau
    /// dieser Rückfall hat den Fehler oben versteckt — er bleibt trotzdem richtig, und der
    /// Wächter <c>OberflaechenschriftTests</c> sorgt dafür, dass er nicht wieder zum
    /// Normalfall wird.
    /// </param>
    public static FontFamily Family(FontRole rolle, string systemRueckfall)
    {
        string name = Schriften.Standard.Family(rolle);
        string ordner = Schriften.Mitgeliefert.FirstOrDefault(f => f.Family == name)?.Ordner ?? name;

        return new FontFamily(Basis, $"./{Schriften.Ordner}/{ordner}/#{name}, {systemRueckfall}");
    }

    /// <summary>
    /// Trägt die drei Oberflächenschriften in ein Wörterbuch ein — gerufen aus
    /// <c>App.OnStartup</c>, <b>bevor</b> das erste Fenster entsteht.
    ///
    /// <para>
    /// Die Schlüssel gibt es in <c>Styles.xaml</c> weiterhin, dort aber nur noch mit der
    /// Systemschrift als Inhalt. <b>Alle Verwendungen sind darum <c>DynamicResource</c></b> —
    /// ein <c>StaticResource</c> hätte den alten Wert schon beim Laden des Wörterbuchs
    /// eingefroren, und diese Methode liefe ins Leere, ohne dass es auffiele.
    /// </para>
    /// </summary>
    public static void Apply(ResourceDictionary res)
    {
        res["Font.Ui"] = Family(FontRole.Ui, "Segoe UI");
        res["Font.Mono"] = Family(FontRole.Mono, "Consolas");
        res["Font.Display"] = Family(FontRole.Display, "Segoe UI");
    }

    /// <summary>
    /// Ob die mitgelieferte Datei zu einer Rolle wirklich gefunden wurde — <b>für den Wächter
    /// und nicht für den Betrieb</b>.
    ///
    /// <para>
    /// <see cref="FontFamily.FamilyNames"/> nennt die Familie, die WPF <i>tatsächlich</i>
    /// aufgelöst hat. Steht dort der Name aus dem Schema, ist die Datei geladen; steht dort
    /// die Systemschrift, greift der Rückfall. <b>Genau diese Unterscheidung hat elf Monate
    /// lang gefehlt.</b>
    /// </para>
    /// </summary>
    public static bool Mitgeliefert(FontRole rolle, string systemRueckfall) =>
        Family(rolle, systemRueckfall).FamilyNames.Values
            .Contains(Schriften.Standard.Family(rolle));
}
