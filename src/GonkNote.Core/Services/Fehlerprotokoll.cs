using GonkNote.Core.Platform;

namespace GonkNote.Core.Services;

/// <summary>
/// Das Fehlerprotokoll neben der Datenbank — <b>mit einer Obergrenze</b>.
///
/// <para>
/// <b>Warum es diese Klasse gibt.</b> Beide Köpfe hatten je eine eigene, Zeile für Zeile
/// gleiche <c>Log</c>-Methode: Ordner anlegen, Zeitstempel und Ausnahme anhängen, jeden
/// Fehler dabei schlucken. **Was keiner von beiden hatte, war eine Grenze** — und am
/// 2026-08-12 hat genau das zugeschlagen: ein WPF-Stil, der auf jedes erzeugte Symbol warf
/// (<c>TargetType "Path"</c> an einem <c>Symbol</c>, behoben mit §4.31), schrieb an
/// <b>einem Nachmittag 48.962 Einträge und 272 MB</b> in den Datenordner des Nutzers.
/// Gefunden wurde das erst am 2026-08-27, weil ein Protokoll niemand liest, solange nichts
/// weh tut (§4.66).
/// </para>
///
/// <para>
/// <b>Die Lehre ist nicht „den Stil reparieren"</b> — der war längst repariert. Sie ist:
/// <i>eine Datei, die im Fehlerfall wächst, wächst am schnellsten genau dann, wenn niemand
/// hinsieht.</i> Deshalb steht die Grenze hier und nicht in einem der Köpfe.
/// </para>
/// </summary>
public static class Fehlerprotokoll
{
    /// <summary>
    /// Ab dieser Größe wird umgebrochen. <b>5 MB sind großzügig für den Zweck</b> — ein
    /// gewöhnlicher Eintrag mit Aufrufliste wiegt wenige Kilobyte, das sind also weit über
    /// tausend Fehler Vorgeschichte.
    /// </summary>
    public const long Hoechstgroesse = 5 * 1024 * 1024;

    /// <summary>Die Endung der zurückgelegten Fassung.</summary>
    public const string AltEndung = ".alt";

    /// <summary>
    /// Muss vor dem nächsten Eintrag umgebrochen werden?
    /// <para>
    /// <b>Vorher geprüft und nicht nachher</b>, damit die Datei die Grenze nie überschreitet
    /// statt sie einmal zu reißen und dann aufzuräumen.
    /// </para>
    /// </summary>
    public static bool MussUmbrechen(long groesse) => groesse >= Hoechstgroesse;

    /// <summary>
    /// Ein Eintrag, so wie er in der Datei steht: eine Kopfzeile mit Zeitstempel, darunter
    /// die Ausnahme mit allem, was sie mitbringt.
    /// <para>
    /// <b>Das Format ist unverändert gegenüber dem, was beide Köpfe vorher schrieben</b> —
    /// ein bestehendes Protokoll bleibt damit lesbar, und die Kopfzeile ist weiterhin die
    /// Marke, an der man Einträge zählt (<c>--- JJJJ-MM-TT HH:MM:SS ---</c>).
    /// </para>
    /// </summary>
    public static string Eintrag(DateTime wann, Exception ex) =>
        $"--- {wann:yyyy-MM-dd HH:mm:ss} ---{Environment.NewLine}" +
        $"{ex}{Environment.NewLine}{Environment.NewLine}";

    /// <summary>Schreibt in das Protokoll neben der Datenbank (<see cref="AppPaths.LogFile"/>).</summary>
    public static void Schreiben(Exception? ex) => Schreiben(ex, AppPaths.LogFile);

    /// <summary>
    /// Schreibt in ein bestimmtes Protokoll. <b>Wirft nie</b> — Protokollieren darf selbst
    /// nie zum Problem werden; das galt in beiden Köpfen schon vorher und gilt weiter.
    /// </summary>
    public static void Schreiben(Exception? ex, string pfad)
    {
        if (ex == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);

            var datei = new FileInfo(pfad);
            if (datei.Exists && MussUmbrechen(datei.Length))
            {
                // Die volle Fassung zurücklegen statt sie wegzuwerfen: der interessante
                // Fehler ist oft der **erste**, nicht der letzte. Eine ältere `.alt` wird
                // dabei überschrieben — damit ist der Gesamtverbrauch auf das Doppelte der
                // Grenze gedeckelt und nicht mehr offen.
                string alt = pfad + AltEndung;
                File.Delete(alt);
                File.Move(pfad, alt);
            }

            File.AppendAllText(pfad, Eintrag(DateTime.Now, ex));
        }
        catch
        {
            // Kein Schreibrecht, Platte voll, die Datei gerade von jemand anderem offen —
            // in allen Fällen ist ein verlorener Protokolleintrag das kleinere Übel.
        }
    }
}
