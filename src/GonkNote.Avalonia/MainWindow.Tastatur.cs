using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GonkNote.Views;

namespace GonkNote;

/// <summary>
/// Die Bildschirmtastatur — <b>welche</b> benutzt wird und <b>wann</b> sie aufgeht.
///
/// <para>
/// <b>Der Anlass</b> (Nutzer, 2026-09-09): „Mein Wunsch wäre eigentlich, dass sie sich
/// automatisch öffnet." Warum das nicht von selbst geschieht, ist gemessen (§4.104):
/// <c>Avalonia.X11</c> hat keine <c>IInputPane</c>, <c>TopLevel.InputPane</c> ist
/// <c>null</c>, und der Kopf ist XWayland-Client — es gibt <b>keinen</b> Weg, über den eine
/// beliebige fremde Tastatur von selbst aufgeht.
/// </para>
///
/// <para>
/// ⛔ <b>Eine frühere Aussage dieser Runde war zu hart und ist am Gerät widerlegt worden.</b>
/// Gemessen war „Tasten über <c>zwp_virtual_keyboard_v1</c> kommen nicht an" — belegt mit
/// <c>wtype</c>. Mit der Tastatur, die der Nutzer wirklich benutzt
/// (<c>io.github.mtolhuys.onscreen-keyboard</c>, eigener nativer Helfer über <b>dasselbe</b>
/// Protokoll), <b>kommen sie sehr wohl an</b> — „hello" stand im Textfeld. Der Unterschied
/// liegt am Werkzeug und nicht am Protokoll: <c>wtype</c> baut die Tastatur auf, schickt und
/// verschwindet sofort; der Helfer wartet nach jedem Schritt auf den Rundlauf.
/// <i>Ein Ersatzwerkzeug beweist über den echten Fall nur so viel, wie es ihm gleicht.</i>
/// </para>
///
/// <para>
/// <b>Daraus folgen drei Zustände statt eines Hakens.</b> „Eingebaut" ist die Antwort, die
/// ohne fremde Software auskommt und auf allen drei Zielplattformen gleich ist.
/// „System-Tastatur" ist für den, der seine eigene behalten will — GonkNote ruft dann
/// <b>einen einstellbaren Befehl</b>, und damit ist auch dort das automatische Aufgehen da,
/// ohne dass der Kopf eine Liste fremder Tastaturen pflegen müsste. „Aus" heißt: GonkNote
/// fasst nichts an.
/// </para>
/// </summary>
public partial class MainWindow
{
    /// <summary>Wer die Tastatur stellt.</summary>
    private enum Tastaturmodus
    {
        /// <summary>GonkNote tut nichts.</summary>
        Aus,
        /// <summary>Die mitgelieferte Tastatur unten im Fenster.</summary>
        Eingebaut,
        /// <summary>Die des Systems, über <see cref="_tastaturBefehl"/>.</summary>
        System,
    }

    private Tastaturmodus _tastaturmodus = Tastaturmodus.Aus;

    /// <summary>Steht die System-Tastatur gerade offen? Nur im Modus <see cref="Tastaturmodus.System"/>.</summary>
    private bool _systemtastaturOffen;

    /// <summary>
    /// Der Befehl, der die System-Tastatur ein- und ausblendet — <b>ein Umschaltbefehl</b>.
    /// Er kommt aus den Einstellungen (Schlüssel <c>keyboard.command</c>) und wird beim
    /// ersten Bedarf gesucht, wenn dort nichts steht.
    /// </summary>
    private string? _tastaturBefehl;

    /// <summary>
    /// Wonach gesucht wird, wenn der Nutzer keinen Befehl eingetragen hat — in dieser
    /// Reihenfolge, und <b>jeder Eintrag prüft seine eigene Voraussetzung</b>.
    ///
    /// <para>
    /// ⛔ <b>Warum die Voraussetzung mit dazugehört</b> (Nutzer, 2026-09-10): Die erste
    /// Fassung dieser Liste hat nur gefragt, ob der <i>Befehl</i> im Pfad liegt. Auf diesem
    /// Rechner liegt <c>omarchy-toggle-osk</c> im Pfad — aber er startet <c>wvkbd-mobintl</c>,
    /// und <b>das ist gar nicht installiert.</b> GonkNote hat also brav einen Befehl
    /// gerufen, der jedes Mal mit „Command not found: wvkbd-mobintl" scheiterte, <b>während
    /// die Tastatur, die der Nutzer wirklich benutzt, danebenstand und funktioniert hätte.</b>
    /// <i>Ein vorhandener Startknopf ist kein Beleg dafür, dass hinter ihm etwas steht.</i>
    /// </para>
    /// <para>
    /// <b>Die Liste bleibt trotzdem kurz und wird nicht gepflegt.</b> Wer eine andere
    /// Tastatur benutzt, trägt seinen Befehl unter <c>keyboard.command</c> ein — eine lange
    /// Liste sähe aus wie eine Zusicherung, dass alles Aufgezählte funktioniert, und genau
    /// diese Zusicherung ist oben gebrochen.
    /// </para>
    /// </summary>
    private static readonly (string Befehl, Func<bool> Vorhanden)[] BekannteBefehle =
    [
        // Das Plugin, das der Nutzer benutzt. Erkannt am Plugin-Ordner und nicht am
        // Startknopf: `omarchy-shell` gibt es auf jedem Omarchy, das Plugin nicht.
        ("omarchy-shell onscreen-keyboard toggle",
            () => ImPfad("omarchy-shell") &&
                  Directory.Exists(Path.Combine(
                      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                      ".config", "omarchy", "plugins",
                      "io.github.mtolhuys.onscreen-keyboard"))),

        // Omarchys eigener Umschalter — **nur wenn wvkbd wirklich da ist**, siehe oben.
        ("omarchy-toggle-osk", () => ImPfad("omarchy-toggle-osk") && ImPfad("wvkbd-mobintl")),

        ("squeekboard-toggle", () => ImPfad("squeekboard-toggle")),
    ];

    // ==================== Menü ====================

    private void Tastatur_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string wahl }) return;
        TastaturmodusSetzen(wahl switch
        {
            "eingebaut" => Tastaturmodus.Eingebaut,
            "system" => Tastaturmodus.System,
            _ => Tastaturmodus.Aus,
        });
        App.Db.SetSetting("keyboard", wahl);
    }

    /// <summary>Beim Start: gespeicherten Modus herstellen und die Haken im Menü setzen.</summary>
    private void TastaturHerstellen()
    {
        _tastaturBefehl = App.Db.GetSetting("keyboard.command");
        if (string.IsNullOrWhiteSpace(_tastaturBefehl)) _tastaturBefehl = BefehlSuchen();

        TastaturmodusSetzen(App.Db.GetSetting("keyboard") switch
        {
            "eingebaut" => Tastaturmodus.Eingebaut,
            "system" => Tastaturmodus.System,
            _ => Tastaturmodus.Aus,
        });

        // Die Tastatur kann sich selbst zuklappen — dann bleibt der Modus, aber die Leiste
        // geht erst beim nächsten Textfeld wieder auf.
        Tastatur.Zugeklappt += () => TastaturLeiste.IsVisible = false;

        // **Am Fenster und im Blasenlauf**, nicht am einzelnen Feld: Es gibt viele Stellen,
        // an denen Text eingegeben wird (Eingabefeld der Tafel, Umbenennen im Baum, Suchen,
        // der Textdokument-Editor) — sie einzeln anzuhängen hieße, die nächste zu vergessen.
        AddHandler(GotFocusEvent, Fokus_Gewandert, RoutingStrategies.Bubble);
    }

    private void TastaturmodusSetzen(Tastaturmodus modus)
    {
        _tastaturmodus = modus;

        TastaturEingebaut.IsChecked = modus == Tastaturmodus.Eingebaut;
        TastaturSystem.IsChecked = modus == Tastaturmodus.System;
        TastaturAus.IsChecked = modus == Tastaturmodus.Aus;

        if (modus != Tastaturmodus.Eingebaut) TastaturLeiste.IsVisible = false;
        if (modus != Tastaturmodus.System && _systemtastaturOffen) SystemtastaturUmschalten();
    }

    // ==================== Automatisch aufgehen ====================

    /// <summary>
    /// Will das Element, das gerade den Fokus bekommen hat, Text?
    ///
    /// <para>
    /// <b>Zwei Fälle, und der zweite ist der wichtige.</b> Ein <c>TextBox</c> ist eindeutig.
    /// Der Textdokument-Editor und das Beschriftungsfeld der Tafel zeichnen jedoch selbst;
    /// dort hat die <see cref="SkiaCanvas"/> den Fokus, und ob sie gerade Text erwartet,
    /// weiß nur sie. <b>Gefragt wird deshalb die Eingabemethode</b>
    /// (<c>TextInputMethodClientRequestedEvent</c> ist der Weg, den auch eine echte
    /// Tastatur nimmt): Wer sich als Eingabeziel anmeldet, will Text — wer nicht, nicht.
    /// Das ist dieselbe Auskunft, an der auch eine <c>IInputPane</c> hinge, wenn es sie
    /// gäbe (§4.41).
    /// </para>
    /// </summary>
    private static bool WillText(object? quelle) => quelle switch
    {
        TextBox => true,
        SkiaCanvas skia => skia.ErwartetText,
        _ => false,
    };

    /// <summary>
    /// ⚠ <b><c>RoutedEventArgs</c> und nicht <c>GotFocusEventArgs</c>:</b> Den Typ gibt es in
    /// Avalonia 12 nicht mehr (an 12.1.1 nachgesehen — die Assembly kennt <c>GotFocusEvent</c>,
    /// aber keinen gleichnamigen Argumenttyp). Gebraucht wird hier ohnehin nur
    /// <c>e.Source</c>.
    /// </summary>
    private void Fokus_Gewandert(object? sender, RoutedEventArgs e)
    {
        if (_tastaturmodus == Tastaturmodus.Aus) return;

        bool will = WillText(e.Source);

        if (_tastaturmodus == Tastaturmodus.Eingebaut)
        {
            // **Nur aufgehen, nicht zugehen.** Wer die Tastatur benutzt, klickt zwischendurch
            // auf ihre eigenen Tasten; die nehmen zwar keinen Fokus (TastaturView), aber ein
            // Werkzeugwechsel oder ein Klick auf die Fläche täte es. Die Tastatur bei jedem
            // Fokusverlust einzuklappen hieße, dass sie beim Schreiben ständig springt.
            // Zugeklappt wird über ihre eigene Taste oder über das Menü.
            if (will) TastaturLeiste.IsVisible = true;
            return;
        }

        // System-Tastatur: auf- und zumachen, denn sie liegt über dem Fenster und verdeckt
        // die Arbeit, solange sie steht.
        if (will != _systemtastaturOffen) SystemtastaturUmschalten();
    }

    // ==================== Die System-Tastatur ====================

    private static string? BefehlSuchen()
    {
        foreach (var (befehl, vorhanden) in BekannteBefehle)
            if (vorhanden()) return befehl;
        return null;
    }

    /// <summary>Liegt ein ausführbarer Name in <c>PATH</c>? Ohne ihn zu starten.</summary>
    private static bool ImPfad(string name) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(ordner =>
            {
                try { return File.Exists(Path.Combine(ordner, name)); }
                catch { return false; }
            });

    /// <summary>
    /// Ruft den Umschaltbefehl.
    ///
    /// <para>
    /// <b>Ein Umschaltbefehl und kein Paar aus „zeigen" und „verbergen"</b> — das ist keine
    /// Vorliebe, sondern der kleinste gemeinsame Nenner: <c>wvkbd</c> kennt ein Signal zum
    /// Umschalten, <c>omarchy-toggle-osk</c> ist selbst einer. Wer zwei Befehle bräuchte,
    /// bekäme zwei Einstellungen, und die zweite bliebe bei den meisten leer.
    /// </para>
    /// <para>
    /// <b>Scheitert er, bleibt es still.</b> Der Nutzer hat den Befehl selbst gesetzt oder
    /// gar keinen; eine Fehlermeldung bei jedem Antippen eines Textfelds wäre unerträglich.
    /// Was fehlt, sagt der Hinweis im Menü.
    /// </para>
    /// </summary>
    private void SystemtastaturUmschalten()
    {
        if (_tastaturBefehl is not { Length: > 0 } zeile) return;

        // **Der Befehl darf Argumente haben.** Der Umschalter des Plugins heißt
        // `omarchy-shell onscreen-keyboard toggle` — drei Wörter. Die erste Fassung hat die
        // ganze Zeile als Dateinamen genommen; damit war jeder Befehl mit Argumenten von
        // vornherein unbrauchbar, und in der Einstellung `keyboard.command` hätte niemand
        // einen eintragen können.
        //
        // **Getrennt wird an Leerzeichen und nicht über eine Shell.** Eine Shell dazwischen
        // wäre eine zweite Sprache in einer Einstellung, die der Nutzer von Hand füllt —
        // mit Anführungszeichen, Ersetzungen und allem, was daran hängt.
        var teile = zeile.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (teile.Length == 0) return;

        try
        {
            var start = new ProcessStartInfo
            {
                FileName = teile[0],
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var arg in teile.Skip(1)) start.ArgumentList.Add(arg);

            using var p = Process.Start(start);
            _systemtastaturOffen = !_systemtastaturOffen;
        }
        catch
        {
            // Siehe oben: still. Der Modus bleibt, damit der nächste Versuch nicht ausbleibt.
        }
    }
}
