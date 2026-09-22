using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Threading;
using GonkNote.Core.Platform;
using GonkNote.Core.Rendering;
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

/// <summary>
/// Die mitgelieferten Schriften, <b>noch einmal für Avalonias Schriftverwaltung</b>.
///
/// <para>
/// <b>Warum das zweimal passieren muss.</b> <see cref="WbFonts"/> lädt die TTF-Dateien aus
/// <c>Fonts/</c> selbst und gibt <c>SKTypeface</c> heraus — damit zeichnet die <b>Leinwand</b>
/// (§4.26). Alles, was Avalonia selbst zeichnet, geht dagegen über <c>FontManager</c>, und der
/// kennt unter Linux nur <b>fontconfig</b> plus das eingebettete Inter. „Geist", „Space
/// Grotesk", „Source Sans 3" und „JetBrains Mono" lagen für ihn nie vor.
/// </para>
/// <para>
/// ⛔ <b>Sichtbar war das an zwei Stellen</b> (Nutzer, 2026-09-21): Die Schriftvorschau der
/// Wähler (V2-134, <c>FontFamily="{Binding}"</c>) zeigte jeden Eintrag in derselben
/// Ersatzschrift — die Vorschau war da, sie zeigte nur nichts —, und das Eingabefeld über
/// einem Textfeld blieb beim Umschalten unverändert, weil es ebenfalls Avalonia zeichnet.
/// <b>Die Leinwand darunter war die ganze Zeit richtig</b>; genau das machte es so schwer zu
/// glauben, dass überhaupt etwas ankommt.
/// </para>
/// <para>
/// <b>Die Dateien werden nicht zusätzlich eingebettet.</b> Avalonias
/// <c>TryAddFontSource</c> nimmt auch <c>file:</c>-Quellen, also denselben Ordner, den
/// <see cref="WbFonts.FontOrdner"/> ohnehin nennt — ein Satz Schriften auf der Platte, zwei
/// Leser. Ein zweiter Satz als <c>AvaloniaResource</c> wäre ein Megabyte, das bei der ersten
/// ausgetauschten Datei auseinanderliefe.
/// </para>
/// </summary>
public static class AvaloniaSchriften
{
    /// <summary>
    /// Der Schlüssel der Sammlung. <b>Muss dem <c>fonts:</c>-Schema folgen</b> — darauf
    /// besteht <c>AddFontCollection</c>, und die Zuordnungen zeigen mit
    /// <c>fonts:GonkNote#&lt;Familie&gt;</c> darauf.
    /// </summary>
    private const string Schluessel = "fonts:GonkNote";

    /// <summary>
    /// Meldet die mitgelieferten Schriften bei Avalonia an — aus <c>Program.BuildAvaloniaApp</c>
    /// über <c>ConfigureFonts</c>, genau dort, wo auch <c>WithInterFont</c> hängt.
    /// <para>
    /// <b>Je Familie eine Quelle:</b> Avalonia liest ein <c>file:</c>-Verzeichnis flach, und
    /// unsere Schriften liegen je Familie in einem eigenen Unterordner.
    /// </para>
    /// </summary>
    public static void Anmelden(FontManager verwaltung)
    {
        var sammlung = new EmbeddedFontCollection(
            new Uri(Schluessel, UriKind.Absolute), new Uri(WbFonts.FontOrdner));

        foreach (var familie in Fonts.Mitgeliefert)
        {
            string ordner = Path.Combine(WbFonts.FontOrdner, familie.Ordner);
            // Fehlt ein Ordner, fehlt eine Familie — nicht die App. Dieselbe Regel, nach der
            // WbFonts.Registratur eine fehlende Datei überspringt.
            if (Directory.Exists(ordner)) sammlung.TryAddFontSource(new Uri(ordner));
        }

        verwaltung.AddFontCollection(sammlung);
    }

    /// <summary>
    /// Die Zuordnung <c>„Geist"</c> → <c>fonts:GonkNote#Geist</c>, für alle mitgelieferten
    /// Familien.
    /// <para>
    /// <b>Ohne sie nützt die Sammlung nichts:</b> Ein bloßer Name ohne Quelle sucht bei
    /// Avalonia <b>nur</b> in den Systemschriften. Im Dokument steht aber der bloße Name
    /// (<c>TdCharFormat.FontFamily</c>, <c>TextElement.FontFamily</c>) und soll dort auch
    /// stehen bleiben — die Umleitung gehört an den Schriftmanager und nicht ins Datenformat.
    /// </para>
    /// <para>
    /// <b>Mitgeliefert schlägt System</b>, auch hier: Ist „Inter" zusätzlich im System
    /// installiert, gewinnt unsere Datei — dieselbe Reihenfolge, die
    /// <c>WbFonts.Aufloesen</c> für die Leinwand hält. Zwei verschiedene Inter auf einem
    /// Bildschirm wären genau der stille Unterschied, den §4.26 abgestellt hat.
    /// </para>
    /// </summary>
    public static FontManagerOptions Zuordnungen() => new()
    {
        FontFamilyMappings = Fonts.Mitgeliefert.ToDictionary(
            f => f.Family,
            f => new FontFamily($"{Schluessel}#{f.Family}")),
    };
}
