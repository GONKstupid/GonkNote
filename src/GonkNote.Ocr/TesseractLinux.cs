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
    /// Sucht die System-Bibliothek in den üblichen Ordnern und liefert den vollen Pfad.
    /// <b>Der erste Ordner, der überhaupt etwas Passendes hat, gewinnt</b> — die Reihenfolge
    /// in <see cref="TesseractBindung.Suchpfade"/> ist die Rangfolge.
    /// </summary>
    private static string? QuelleSuchen(string stamm, int hauptversion)
    {
        foreach (string ordner in TesseractBindung.Suchpfade)
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
