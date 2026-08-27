namespace GonkNote.Core.Platform;

/// <summary>
/// Was die Tesseract-Bindung unter Linux braucht, um überhaupt zu laden — als reine Regel,
/// ohne Dateisystem und ohne das Tesseract-Paket.
///
/// <para>
/// <b>Warum es das gibt.</b> Das NuGet-Paket <c>Tesseract 5.2.0</c> bringt für Linux
/// <b>nichts</b> mit: kein <c>runtimes/linux-*</c>, nur die Windows-DLLs unter <c>x64/</c>
/// und <c>x86/</c>. Sein Lader sucht deshalb auch unter Linux die <b>Windows-Dateinamen mit
/// <c>.so</c></b> — <see cref="TesseractZiel"/> und <see cref="LeptonicaZiel"/> — und zwar im
/// Unterordner <see cref="Unterordner"/> unter <c>TesseractEnviornment.CustomSearchPath</c>.
/// Auf dem Laptop gemessen (HANDOFF §4.63, V2-86): <b>im Wurzelverzeichnis daneben reicht
/// nicht</b>, und <c>LD_LIBRARY_PATH</c> hilft auch nicht — der Lader prüft die Datei am
/// Pfad, bevor er <c>dlopen</c> ruft.
/// </para>
///
/// <para>
/// <b>Die Regel steht hier und nicht im Kopf</b>, weil sie sich prüfen lässt, ohne dass eine
/// native Bibliothek in der Nähe ist: <see cref="SonameWaehlen"/> bekommt eine Liste von
/// Dateinamen und sagt, welcher davon der richtige Verweisgeber ist. Was im Kopf bleibt, ist
/// das Anlegen des Verweises selbst — und das ist reines Dateisystem.
/// </para>
///
/// <para>
/// <b>Nutzer-Entscheidung 2026-08-27 (§5 „Noch offen" 18): verwiesen wird, nicht
/// mitgeliefert.</b> Die App legt beim Start Verweise auf die System-Bibliotheken an, statt
/// eigene <c>.so</c> auszuliefern. Das Flatpak-Manifest muss <c>tesseract</c> und
/// <c>leptonica</c> ohnehin als Abhängigkeit nennen (§4.63); dort liegt die Version fest.
/// </para>
/// </summary>
public static class TesseractBindung
{
    /// <summary>Der Unterordner, in dem der Lader des Pakets sucht — unter Linux wie unter Windows.</summary>
    public const string Unterordner = "x64";

    /// <summary>Der Dateiname, unter dem der Lader Tesseract erwartet.</summary>
    public const string TesseractZiel = "libtesseract50.so";

    /// <summary>Der Dateiname, unter dem der Lader Leptonica erwartet.</summary>
    public const string LeptonicaZiel = "libleptonica-1.82.0.so";

    /// <summary>Der Stamm des Systemnamens von Tesseract (ohne <c>.so</c> und ohne Version).</summary>
    public const string TesseractStamm = "libtesseract";

    /// <summary>Der Stamm des Systemnamens von Leptonica.</summary>
    public const string LeptonicaStamm = "libleptonica";

    /// <summary>
    /// Die Hauptversion von Tesseract, die auf dem Laptop <b>gemessen</b> funktioniert hat:
    /// <c>libtesseract.so.5</c> (Tesseract 5.5.3, erkannt bei Zuversicht 0,930).
    /// </summary>
    public const int TesseractHauptversion = 5;

    /// <summary>
    /// Dasselbe für Leptonica: <c>libleptonica.so.6</c> (Arch liefert 1.87.0). Dass die
    /// Bindung dem Namen nach <c>1.82.0</c> will, ist <b>festverdrahtet und dauerhaft</b> —
    /// der Versatz hat in der Messung nicht gestört.
    /// </summary>
    public const int LeptonicaHauptversion = 6;

    /// <summary>
    /// Wo Systembibliotheken üblicherweise liegen, in der Reihenfolge, in der gesucht wird.
    /// <para>
    /// <c>/app/lib</c> steht vorn: <b>im Flatpak liegt dort die Fassung, die das Manifest
    /// festgelegt hat</b>, und die soll gewinnen, falls daneben noch eine des Wirtssystems
    /// sichtbar ist. Danach kommen Arch (<c>/usr/lib</c>), Debian/Ubuntu
    /// (<c>/usr/lib/x86_64-linux-gnu</c>) und Fedora (<c>/usr/lib64</c>).
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Suchpfade { get; } =
    [
        "/app/lib",
        "/usr/lib",
        "/usr/lib/x86_64-linux-gnu",
        "/usr/lib64",
        "/lib/x86_64-linux-gnu",
        "/usr/local/lib",
    ];

    /// <summary>
    /// Welcher der vorhandenen Dateinamen der richtige Verweisgeber ist.
    ///
    /// <para>Die Reihenfolge, und jede Stufe hat einen Grund:</para>
    /// <list type="number">
    ///   <item><description><b>Die gemessene Hauptversion zuerst.</b> <c>libtesseract.so.5</c>
    ///     ist die Fassung, gegen die auf dem Laptop zeichengenau erkannt wurde (§4.63). Wenn
    ///     sie da ist, wird nicht geraten.</description></item>
    ///   <item><description><b>Sonst die höchste vorhandene Hauptversion.</b> Steigt Arch auf
    ///     <c>libleptonica.so.7</c>, bricht der Verweis dadurch <b>nicht</b> — genau das war
    ///     der Einwand gegen den Verweis-Weg in §5 Nr. 18. Ob die neue Version zur Bindung
    ///     passt, weiß hier niemand; scheitert sie, meldet die Texterkennung sauber „nicht
    ///     verfügbar" statt zu stürzen (<see cref="IOcrEngine.IsAvailable"/>).</description></item>
    ///   <item><description><b>Der unversionierte Name zuletzt.</b> <c>libtesseract.so</c>
    ///     gehört zum Entwicklungspaket und fehlt auf einem normalen System oft. Als
    ///     <i>Verweisziel</i> taugt er trotzdem — er zeigt selbst auf die versionierte Datei.
    ///     <b>Nicht zu verwechseln mit dem Befund aus §4.63</b>: dort half er nicht, weil der
    ///     Lader einen <i>anderen Namen</i> sucht, nicht weil die Datei untauglich
    ///     wäre.</description></item>
    /// </list>
    /// </summary>
    /// <param name="stamm">Etwa <see cref="TesseractStamm"/>.</param>
    /// <param name="bevorzugteHauptversion">Etwa <see cref="TesseractHauptversion"/>.</param>
    /// <param name="dateinamen">Die Dateinamen (ohne Pfad), die in einem Ordner liegen.</param>
    /// <returns>Der zu verweisende Dateiname, oder <c>null</c>, wenn keiner passt.</returns>
    public static string? SonameWaehlen(
        string stamm, int bevorzugteHauptversion, IEnumerable<string> dateinamen)
    {
        string ohneVersion = stamm + ".so";
        string bevorzugt = $"{ohneVersion}.{bevorzugteHauptversion}";

        bool unversioniertDa = false;
        int besteVersion = -1;
        string? bester = null;

        foreach (string name in dateinamen)
        {
            if (name == bevorzugt) return name;      // Stufe 1 — nichts schlägt das Gemessene

            if (name == ohneVersion) { unversioniertDa = true; continue; }

            if (!name.StartsWith(ohneVersion + ".", StringComparison.Ordinal)) continue;

            // Nur die reine Hauptversion zählt. `libtesseract.so.5.0.5` ist die echte Datei,
            // auf die `libtesseract.so.5` zeigt — sie zu verweisen wäre nicht falsch, aber sie
            // bindet uns an eine Nebenversion, die sich bei jedem Update ändert.
            string rest = name[(ohneVersion.Length + 1)..];
            if (!int.TryParse(rest, out int version)) continue;

            if (version > besteVersion) { besteVersion = version; bester = name; }
        }

        if (bester != null) return bester;           // Stufe 2
        return unversioniertDa ? ohneVersion : null; // Stufe 3
    }
}
