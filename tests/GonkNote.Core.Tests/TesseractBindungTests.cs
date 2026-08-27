using GonkNote.Core.Platform;

namespace GonkNote.Core.Tests;

/// <summary>
/// Wächter für <see cref="TesseractBindung"/> — die Regel, welche System-Bibliothek der
/// Linux-Kopf verweisen soll (Phase 4.5, Stück 6; gemessen in HANDOFF §4.63).
///
/// <para>
/// <b>Warum das prüfbar ist und der Rest nicht.</b> Ob <c>dlopen</c> am Ende trägt, sagt nur
/// ein Linux mit installiertem Tesseract — dafür gibt es den Laptop. Die <i>Auswahl</i> des
/// Namens ist dagegen reines Rechnen auf einer Liste von Dateinamen, und genau deshalb steht
/// sie in Core und nicht im Kopf.
/// </para>
/// </summary>
public class TesseractBindungTests
{
    // ==================== Die gemessene Fassung hat Vorrang ====================

    [Fact]
    public void Die_gemessene_Hauptversion_gewinnt_auch_wenn_eine_hoehere_daliegt()
    {
        // Genau der Fall, den §5 Nr. 18 als Risiko benannt hat — nur andersherum: liegt die
        // gemessene Fassung da, wird nicht auf die neuere gewechselt.
        string? treffer = TesseractBindung.SonameWaehlen(
            TesseractBindung.TesseractStamm, TesseractBindung.TesseractHauptversion,
            ["libtesseract.so", "libtesseract.so.5", "libtesseract.so.6"]);

        Assert.Equal("libtesseract.so.5", treffer);
    }

    [Fact]
    public void Das_ist_die_Lage_auf_dem_Laptop()
    {
        // Was `ls /usr/lib` dort am 2026-08-27 gezeigt hat (§4.63, Frage 2 und 3).
        Assert.Equal("libtesseract.so.5", TesseractBindung.SonameWaehlen(
            TesseractBindung.TesseractStamm, TesseractBindung.TesseractHauptversion,
            ["libtesseract.so", "libtesseract.so.5", "libtesseract.so.5.0.5"]));

        Assert.Equal("libleptonica.so.6", TesseractBindung.SonameWaehlen(
            TesseractBindung.LeptonicaStamm, TesseractBindung.LeptonicaHauptversion,
            ["libleptonica.so", "libleptonica.so.6", "libleptonica.so.6.0.0"]));
    }

    // ==================== Und wenn sie nicht da ist ====================

    [Fact]
    public void Steigt_das_System_auf_die_naechste_Hauptversion_bricht_der_Verweis_nicht()
    {
        // Der Einwand gegen den Verweis-Weg (§5 Nr. 18): „steigt Arch auf libleptonica.so.7,
        // bricht der Verweis". Tut er nicht — es wird die höchste vorhandene genommen.
        string? treffer = TesseractBindung.SonameWaehlen(
            TesseractBindung.LeptonicaStamm, TesseractBindung.LeptonicaHauptversion,
            ["libleptonica.so.7"]);

        Assert.Equal("libleptonica.so.7", treffer);
    }

    [Fact]
    public void Von_mehreren_Hauptversionen_gewinnt_die_hoechste()
    {
        Assert.Equal("libleptonica.so.11", TesseractBindung.SonameWaehlen(
            TesseractBindung.LeptonicaStamm, 99,
            ["libleptonica.so.7", "libleptonica.so.11", "libleptonica.so.9"]));
    }

    [Fact]
    public void Zahlenvergleich_und_kein_Zeichenkettenvergleich()
    {
        // Der Fehler, der hier nahe liegt: „so.9" ist als Zeichenkette größer als „so.11".
        Assert.Equal("libtesseract.so.11", TesseractBindung.SonameWaehlen(
            TesseractBindung.TesseractStamm, 99, ["libtesseract.so.9", "libtesseract.so.11"]));
    }

    [Fact]
    public void Der_unversionierte_Name_kommt_zuletzt()
    {
        Assert.Equal("libtesseract.so.4", TesseractBindung.SonameWaehlen(
            TesseractBindung.TesseractStamm, TesseractBindung.TesseractHauptversion,
            ["libtesseract.so", "libtesseract.so.4"]));
    }

    [Fact]
    public void Der_unversionierte_Name_taugt_wenn_es_sonst_nichts_gibt()
    {
        // Er gehört zum Entwicklungspaket und zeigt selbst auf die versionierte Datei. Der
        // Befund aus §4.63 („hilft nicht") galt dem gesuchten *Namen*, nicht dieser Datei.
        Assert.Equal("libtesseract.so", TesseractBindung.SonameWaehlen(
            TesseractBindung.TesseractStamm, TesseractBindung.TesseractHauptversion,
            ["libtesseract.so"]));
    }

    // ==================== Was nicht gewählt werden darf ====================

    [Fact]
    public void Die_volle_Versionsdatei_wird_nicht_verwiesen()
    {
        // `libtesseract.so.5.0.5` ist die echte Datei — sie zu verweisen wäre nicht falsch,
        // bände uns aber an eine Nebenversion, die jedes Update ändert.
        Assert.Null(TesseractBindung.SonameWaehlen(
            TesseractBindung.TesseractStamm, TesseractBindung.TesseractHauptversion,
            ["libtesseract.so.5.0.5"]));
    }

    [Fact]
    public void Ein_leerer_Ordner_liefert_nichts()
    {
        Assert.Null(TesseractBindung.SonameWaehlen(
            TesseractBindung.TesseractStamm, TesseractBindung.TesseractHauptversion, []));
    }

    [Fact]
    public void Fremde_Dateien_mit_aehnlichem_Namen_zaehlen_nicht()
    {
        // `Directory.GetFiles(ordner, "libtesseract.so*")` bringt so etwas mit.
        Assert.Null(TesseractBindung.SonameWaehlen(
            TesseractBindung.TesseractStamm, TesseractBindung.TesseractHauptversion,
            ["libtesseract.so.alt", "libtesseract.solid", "libtesseract.so.x"]));
    }

    [Fact]
    public void Der_Stamm_muss_genau_stimmen()
    {
        // `libleptonica` darf nicht auf `libtesseract` antworten.
        Assert.Null(TesseractBindung.SonameWaehlen(
            TesseractBindung.LeptonicaStamm, TesseractBindung.LeptonicaHauptversion,
            ["libtesseract.so.5"]));
    }

    // ==================== Die Namen, an denen alles hängt ====================

    [Fact]
    public void Die_gesuchten_Namen_sind_die_Windows_Namen_mit_so()
    {
        // Sie stehen im Paket und sind **nicht** frei wählbar (§4.63). Wer sie hier ändert,
        // hat die Erkennung still abgeschaltet — deshalb ein Wächter darauf.
        Assert.Equal("libtesseract50.so", TesseractBindung.TesseractZiel);
        Assert.Equal("libleptonica-1.82.0.so", TesseractBindung.LeptonicaZiel);
        Assert.Equal("x64", TesseractBindung.Unterordner);
    }

    [Fact]
    public void Im_Flatpak_gewinnt_die_Fassung_aus_dem_Manifest()
    {
        // /app/lib steht vor /usr/lib: im Flatpak legt das Manifest die Version fest, und die
        // soll gelten, falls daneben noch eine des Wirtssystems sichtbar ist (§4.63).
        Assert.Equal("/app/lib", TesseractBindung.Suchpfade[0]);
        Assert.Contains("/usr/lib", TesseractBindung.Suchpfade);
    }
}
