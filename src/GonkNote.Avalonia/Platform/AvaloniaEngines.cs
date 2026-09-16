using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using GonkNote.Core.Platform;
using GonkNote.Core.Text;
using GonkNote.Core.Theming;

namespace GonkNote.Platform;

/// <summary>Der Griff nach draußen.</summary>
public sealed class AvaloniaShell : IShell
{
    /// <summary>
    /// Öffnet eine Datei mit dem hinterlegten Standardprogramm. Unter Windows über das
    /// Shell-Verb (<c>UseShellExecute</c>), unter Linux über <c>xdg-open</c> — dort tut
    /// <c>UseShellExecute</c> nichts dergleichen, sondern versucht, die Datei
    /// <b>auszuführen</b>. Bei einer frisch exportierten PDF wäre das nicht nur nutzlos,
    /// sondern die falsche Art von Nutzlosigkeit.
    /// </summary>
    public void OpenExternal(string path)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start(new ProcessStartInfo("xdg-open", [path]) { UseShellExecute = false });
            else
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // Kein Standardprogramm hinterlegt, oder xdg-open fehlt. Die Datei ist
            // geschrieben — das ist die Hauptsache.
        }
    }

    /// <summary>
    /// Über das Hauptfenster schließen, nicht über <c>Shutdown</c>: nur so laufen die
    /// Aufräumarbeiten am Fenster (Speichern, Fenstermaße merken) noch durch. Dieselbe
    /// Überlegung wie in <c>WpfShell</c>.
    /// </summary>
    public void Quit() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow?.Close();
}

/// <summary>Wiederholung über einen <see cref="DispatcherTimer"/> — läuft auf dem Oberflächen-Faden.</summary>
public sealed class AvaloniaUiScheduler : IUiScheduler
{
    public IDisposable Repeat(TimeSpan interval, Action tick)
    {
        // **`Background` ausgeschrieben, obwohl es die Vorgabe ist** — die Vorgabe ist eine
        // Falle (sie ist die *unterste* Stufe und kommt unter einem aufliegenden Stift nicht
        // mehr dran, siehe `ZeitgeberTests`), und hier ist sie ausnahmsweise richtig: der
        // einzige Nutzer ist das Sichern alle dreißig Sekunden. Das darf warten, bis die
        // Hand vom Schirm ist.
        var uhr = new DispatcherTimer(DispatcherPriority.Background) { Interval = interval };
        uhr.Tick += (_, _) => tick();
        uhr.Start();
        return new Abmeldung(uhr);
    }

    private sealed class Abmeldung(DispatcherTimer uhr) : IDisposable
    {
        public void Dispose() => uhr.Stop();
    }
}

/// <summary>
/// Das Schriftschema — <b>dasselbe wie im WPF-Kopf</b> (§4.26).
///
/// <para>
/// <b>Hier stand die Plattform-Weiche</b>: „Segoe UI" unter Windows, „Inter" unter Linux. Sie
/// ist weg, und das war der Sinn der Übung. Der Fehler dahinter war nicht die Weiche selbst,
/// sondern dass sie **nur für Avalonia stimmte**: Inter kam aus <c>WithInterFont()</c> und ist
/// damit in Avalonia eingebettet — <c>SKTypeface.FromFamilyName("Inter")</c> geht dagegen über
/// fontconfig. Auf einem Linux-Rechner ohne systemweit installiertes Inter zeichnete das Chrome
/// in Inter und die Zeichenfläche daneben in irgendeiner Ersatzschrift, still.
/// </para>
/// <para>
/// Seit §4.26 liegen die Schriften bei der App, und <see cref="Rendering.WbFonts"/> lädt sie
/// selbst — Chrome und Leinwand bekommen dieselbe Datei.
/// </para>
/// </summary>
/// <summary>
/// Die Sprachfrage, beantwortet aus den mitgelieferten Wörterbüchern (Phase 5.1).
/// <para>
/// <b>Hier stand bis dahin <c>AlwaysSupportedSpellChecker</c></b> — er sagte auf jede Sprache
/// Ja, weil unter Linux niemand prüfte und ein Nein nur blockiert hätte. Jetzt prüft jemand,
/// und damit ist Ja auf alles eine Unwahrheit: Für Französisch liegt kein Wörterbuch neben
/// dem Programm, und das soll man sehen können (§4.64).
/// </para>
/// </summary>
public sealed class AvaloniaSpellChecker : ISpellChecker
{
    public bool IsSupported(string bcp47) => TdRechtschreibung.Verfuegbar(bcp47);
}

public sealed class AvaloniaFontProvider : IFontProvider
{
    public FontScheme Scheme => Fonts.Standard;
}
