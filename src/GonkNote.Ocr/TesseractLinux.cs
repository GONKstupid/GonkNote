using System.Reflection;
using System.Runtime.InteropServices;
using GonkNote.Core.Platform;

namespace GonkNote.Ocr;

/// <summary>
/// Was unter Linux nötig ist, damit die Tesseract-Bindung überhaupt lädt. <b>Auf dem
/// CachyOS-Laptop gemessen, nicht hergeleitet</b> (HANDOFF §4.63, V2-86).
///
/// <para>
/// <b>Es sind drei Namen, und sie sind nicht gleichartig.</b> Das ist der Grund, warum hier
/// zwei sehr verschiedene Handgriffe nebeneinander stehen:
/// </para>
/// <list type="table">
///   <item><term><c>libdl.so</c></term>
///     <description>Ein echter <c>DllImport</c> im Unix-Lader des Pakets. Seit glibc 2.44
///     gibt es die Datei nicht mehr, nur noch <c>libdl.so.2</c>. <b>Das ist die einzige der
///     drei Nähte, die ein <see cref="NativeLibrary.SetDllImportResolver"/> überhaupt
///     erreicht</b> — siehe <see cref="ResolverSetzen"/>.</description></item>
///   <item><term><c>libtesseract50.so</c></term>
///     <description>Wird vom Paket selbst per <c>dlopen</c> geöffnet
///     (<c>InteropDotNet.LibraryLoader</c>). <b>Für den Resolver unsichtbar</b> — hier hilft
///     nur, dass die Datei unter dem gesuchten Namen am gesuchten Ort
///     liegt.</description></item>
///   <item><term><c>libleptonica-1.82.0.so</c></term><description>Dasselbe.</description></item>
/// </list>
///
/// <para>
/// <b>Zwei Wege, die gegengeprüft wurden und nicht funktionieren</b> (§4.63): weder
/// <c>LD_LIBRARY_PATH</c> noch die Dateien im Wurzelverzeichnis neben dem Programm. Der Lader
/// prüft die Datei am Pfad <c>&lt;CustomSearchPath&gt;/x64/&lt;Name&gt;</c>, bevor er
/// <c>dlopen</c> ruft — der Unterordner ist Pflicht.
/// </para>
///
/// <para>
/// <b>Verwiesen wird, nicht mitgeliefert</b> — Nutzer-Entscheidung vom 2026-08-27
/// (§5 „Noch offen" 18). Die Begründung steht bei <see cref="TesseractBindung"/>.
/// </para>
/// </summary>
public static class TesseractLinux
{
    /// <summary>
    /// Richtet beides ein und liefert den Ordner, der als
    /// <c>TesseractEnviornment.CustomSearchPath</c> zu setzen ist.
    ///
    /// <para>
    /// <b>Der Ordner liegt in den Nutzerdaten und nicht neben dem Programm.</b> Hier wird
    /// geschrieben, und <see cref="IAppPaths.AppFolder"/> ist unter Linux und iOS
    /// schreibgeschützt — im Flatpak ist <c>/app</c> nur lesbar. Der Ordner ist reiner
    /// Zwischenspeicher: wer ihn löscht, bekommt ihn beim nächsten Start wieder.
    /// </para>
    ///
    /// <para>
    /// <b>Wirft nie.</b> Findet sich keine System-Bibliothek, entsteht ein leerer Ordner, der
    /// Lader scheitert später an genau derselben Stelle wie ohne diesen Handgriff, und die
    /// Texterkennung meldet ehrlich „nicht verfügbar". Das ist die richtige Antwort auf ein
    /// System, auf dem Tesseract schlicht nicht installiert ist.
    /// </para>
    /// </summary>
    public static string Einrichten()
    {
        ResolverSetzen();

        string wurzel = AppPaths.DataSubfolder("tesseract");
        string x64 = Path.Combine(wurzel, TesseractBindung.Unterordner);
        Directory.CreateDirectory(x64);

        VerweisAnlegen(x64, TesseractBindung.TesseractZiel,
            TesseractBindung.TesseractStamm, TesseractBindung.TesseractHauptversion);
        VerweisAnlegen(x64, TesseractBindung.LeptonicaZiel,
            TesseractBindung.LeptonicaStamm, TesseractBindung.LeptonicaHauptversion);

        return wurzel;
    }

    // ==================== ① libdl ====================

    private static bool _resolverSteht;

    /// <summary>
    /// Lenkt <c>libdl</c> auf <c>libdl.so.2</c> um — auf der <b>Tesseract-Assembly</b>, denn
    /// ein Resolver gilt immer nur für die Assembly, die den <c>DllImport</c> erklärt.
    ///
    /// <para>
    /// <b>Warum das nötig ist:</b> glibc 2.44 hat <c>libdl</c> in <c>libc</c> aufgehen lassen;
    /// unversioniert gibt es nur noch <c>libdl.a</c>, und die ist für <c>dlopen</c> nutzlos.
    /// Das Paket ruft aber <c>DllImport("libdl")</c>, und .NET probiert von sich aus zwar
    /// <c>libdl.so</c>, nicht aber <c>libdl.so.2</c>.
    /// </para>
    ///
    /// <para>
    /// <b>Nur eintragen, wenn wirklich nötig.</b> Der Resolver läuft sonst bei jedem einzelnen
    /// nativen Aufruf der Assembly mit; ein Rückgabewert von <see cref="IntPtr.Zero"/> heißt
    /// „mach weiter wie sonst", also fällt alles andere unverändert auf den Normalweg zurück.
    /// </para>
    /// </summary>
    private static void ResolverSetzen()
    {
        if (_resolverSteht) return;
        _resolverSteht = true;
        try
        {
            NativeLibrary.SetDllImportResolver(
                typeof(Tesseract.TesseractEngine).Assembly,
                (name, assembly, pfade) =>
                    name == "libdl" && NativeLibrary.TryLoad("libdl.so.2", assembly, pfade, out var griff)
                        ? griff
                        : IntPtr.Zero);
        }
        catch (InvalidOperationException)
        {
            // Für diese Assembly steht schon einer. Beide Köpfe laufen einzeln, das kann
            // eigentlich nicht vorkommen — aber ein Absturz beim Öffnen eines Menüs wäre der
            // falsche Preis dafür, dass wir uns hier irren.
        }
    }

    // ==================== ② Die zwei Namen unter x64/ ====================

    /// <summary>
    /// Legt <paramref name="zielname"/> in <paramref name="x64"/> als Verweis auf die
    /// System-Bibliothek an. Welche das ist, entscheidet
    /// <see cref="TesseractBindung.SonameWaehlen"/>.
    ///
    /// <para>
    /// <b>Ein bestehender Verweis wird jedes Mal neu gesetzt.</b> Er ist billig, und ein
    /// stehengebliebener Verweis auf eine Bibliothek, die ein Systemupdate weggeräumt hat,
    /// wäre sonst ein Fehler, den kein Neustart heilt.
    /// </para>
    /// </summary>
    private static void VerweisAnlegen(string x64, string zielname, string stamm, int hauptversion)
    {
        try
        {
            string? quelle = QuelleSuchen(stamm, hauptversion);
            string ziel = Path.Combine(x64, zielname);

            if (quelle == null)
            {
                // Nichts gefunden. Einen alten Verweis stehen zu lassen wäre schlimmer als
                // keinen: er zeigte auf eine Datei, die es nicht mehr gibt.
                File.Delete(ziel);
                return;
            }

            File.Delete(ziel);
            File.CreateSymbolicLink(ziel, quelle);
        }
        catch
        {
            // Kein Schreibrecht, ein Dateisystem ohne Verweise, ein Wettlauf mit einem
            // zweiten Kopf — in allen Fällen bleibt es bei „Texterkennung nicht verfügbar".
        }
    }

    /// <summary>
    /// Wo eine <b>mitgelieferte</b> Fassung läge: <c>&lt;AppFolder&gt;/lib</c>, also neben
    /// dem Programm. Gibt es den Ordner nicht, kommt <c>null</c> zurück und gesucht wird nur
    /// im System.
    ///
    /// <para>
    /// <b>Ermittelt wird der Pfad hier und nicht in Core</b> (§5 Nr. 29):
    /// <see cref="TesseractBindung"/> ist bewusst ohne Dateisystem prüfbar. Dort steht die
    /// <i>Rangfolge</i>, hier steht das Nachsehen.
    /// </para>
    ///
    /// <para>
    /// ⚠ <b>Das ist nicht der Ordner, in den die Verweise gelegt werden.</b> Der liegt in den
    /// Nutzerdaten, weil hier geschrieben wird und <see cref="IAppPaths.AppFolder"/>
    /// schreibgeschützt sein kann — im AppImage ist er es immer (SquashFS, nur lesbar).
    /// </para>
    /// </summary>
    private static string? EigenerLibOrdner()
    {
        try
        {
            string ordner = AppPaths.AppSubfolder(TesseractBindung.EigenerUnterordner);
            return Directory.Exists(ordner) ? ordner : null;
        }
        catch
        {
            return null;   // Kein Lesezugriff neben dem Programm — dann eben nur das System.
        }
    }

    /// <summary>
    /// Sucht die Bibliothek in den üblichen Ordnern und liefert den vollen Pfad.
    /// <b>Der erste Ordner, der überhaupt etwas Passendes hat, gewinnt</b> — die Reihenfolge
    /// in <see cref="TesseractBindung.SuchpfadeMit"/> ist die Rangfolge, und ganz vorn steht
    /// die <b>mitgelieferte</b> Fassung (§5 Nr. 29).
    ///
    /// <para>
    /// ⛔ <b>Damit eine mitgelieferte Fassung wirklich lädt, reicht dieser Fund nicht.</b> Der
    /// Verweis zeigt auf <c>libtesseract.so.5</c>; deren <i>eigene</i> Abhängigkeiten
    /// (<c>libleptonica</c>, <c>libpng</c>, …) löst danach der Systemlader auf, und der sucht
    /// sie <b>nicht</b> neben dem Verweisziel. Dafür setzt das AppImage in seinem
    /// <c>AppRun</c> ein <c>LD_LIBRARY_PATH</c> auf denselben Ordner.
    /// <b>Das widerspricht §4.63 nicht:</b> dort half <c>LD_LIBRARY_PATH</c> nichts, weil der
    /// Lader des NuGet-Pakets die Datei <i>am Pfad prüft, bevor er <c>dlopen</c> ruft</i> —
    /// eine andere Stufe. <i>Zwei Stufen, zwei Regeln; wer die eine Messung auf die andere
    /// überträgt, sucht danach am falschen Ende.</i>
    /// </para>
    ///
    /// <para>
    /// ⚠ <b>Benannte Grenze: der erste Ordner mit einem Treffer gewinnt, und „Treffer" heißt
    /// hier „passender Dateiname" und nicht „lädt".</b> Liegt im mitgelieferten Ordner ein
    /// <c>libtesseract.so.5</c>, das auf dem fremden Rechner nicht lädt, wird das
    /// <b>Wirtssystem nicht mehr gefragt</b> — auf einem Rechner <i>mit</i> Tesseract wäre das
    /// ein Rückschritt gegenüber einem AppImage ohne Beipack.
    ///
    /// <b>Bewusst so gelassen</b> (§5 Nr. 29): Eine Rangfolge kann nach Namen entscheiden,
    /// nicht nach Ladbarkeit — dafür müsste hier probeweise <c>dlopen</c> gerufen und wieder
    /// aufgeräumt werden, und zwar in genau der Naht, an der §4.63 drei Anläufe gekostet hat.
    /// <b>Der Preis ist stattdessen an den Bau geknüpft:</b> <c>packaging/appimage/bauen.sh</c>
    /// sammelt die Abhängigkeiten mit <c>ldd</c> ein, statt die Datei allein zu kopieren, und
    /// der Auftrag in §5d prüft <b>beide</b> Fälle — mit und ohne System-Tesseract.
    /// <i>Eine Grenze, die man kennt und beim Bau schließt, ist billiger als eine Prüfung, die
    /// bei jedem Start läuft.</i>
    /// </para>
    /// </summary>
    private static string? QuelleSuchen(string stamm, int hauptversion)
    {
        foreach (string ordner in TesseractBindung.SuchpfadeMit(EigenerLibOrdner()))
        {
            if (!Directory.Exists(ordner)) continue;

            string[] namen;
            try
            {
                namen = Directory.GetFiles(ordner, stamm + ".so*")
                                 .Select(Path.GetFileName)
                                 .OfType<string>()
                                 .ToArray();
            }
            catch
            {
                continue;   // Ordner nicht lesbar — der nächste ist einen Versuch wert
            }

            if (TesseractBindung.SonameWaehlen(stamm, hauptversion, namen) is { } treffer)
                return Path.Combine(ordner, treffer);
        }
        return null;
    }
}
