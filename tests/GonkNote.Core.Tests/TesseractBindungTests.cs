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

    // ==================== Die mitgelieferte Fassung (§5 Nr. 29) ====================

    [Fact]
    public void Die_mitgelieferte_Fassung_gewinnt_gegen_JEDEN_Systempfad()
    {
        // Nutzer-Entscheidung 2026-09-04: das AppImage bringt seine eigene Texterkennung mit,
        // weil nicht jede Verteilung eine hat. Wer eine Fassung mitliefert, hat sie gegen
        // genau diese App gebaut -- sie muss auch dann gewinnen, wenn der Wirt etwas hat.
        var pfade = TesseractBindung.SuchpfadeMit("/tmp/.mount_Gonk1234/usr/bin/lib");

        Assert.Equal("/tmp/.mount_Gonk1234/usr/bin/lib", pfade[0]);

        // ... und zwar auch gegen /app/lib, das sonst die erste Stelle hat.
        Assert.True(pfade.ToList().IndexOf("/app/lib") > 0);
    }

    [Fact]
    public void Ohne_eigenen_Ordner_bleibt_die_Liste_genau_die_alte()
    {
        // Der Normalfall: Flatpak und der Lauf aus dem Quellordner. §5 Nr. 18 gilt dort
        // unveraendert weiter -- verwiesen wird, nicht mitgeliefert.
        Assert.Same(TesseractBindung.Suchpfade, TesseractBindung.SuchpfadeMit(null));
        Assert.Same(TesseractBindung.Suchpfade, TesseractBindung.SuchpfadeMit(""));
        Assert.Same(TesseractBindung.Suchpfade, TesseractBindung.SuchpfadeMit("   "));
    }

    [Fact]
    public void Der_eigene_Ordner_haengt_die_Systempfade_an_und_ersetzt_sie_nicht()
    {
        // Damit ein AppImage, dessen lib-Ordner LEER oder unpassend bestueckt ist, noch auf
        // das Wirtssystem faellt statt gar nichts zu finden.
        //
        // ⚠ WAS DIESER WAECHTER NICHT ZUSICHERT, und die erste Fassung dieses Kommentars hat
        // genau das behauptet: Er deckt NICHT den Fall ab, dass die mitgelieferte Datei DA
        // IST und trotzdem nicht LAEDT. TesseractLinux.QuelleSuchen nimmt den ERSTEN Ordner
        // mit einem Treffer; steht dort etwas Unbrauchbares, wird das System nie gefragt.
        // Eine Rangfolge kann nur nach Namen entscheiden, nicht nach Ladbarkeit -- die
        // benannte Grenze steht bei TesseractLinux.QuelleSuchen.
        var pfade = TesseractBindung.SuchpfadeMit("/irgendwo/lib");

        Assert.Equal(TesseractBindung.Suchpfade.Count + 1, pfade.Count);
        Assert.Equal(TesseractBindung.Suchpfade, pfade.Skip(1));
    }

    [Fact]
    public void Der_Name_des_mitgelieferten_Ordners_steht_fest()
    {
        // Er steht an DREI Stellen: hier, im AppRun (LD_LIBRARY_PATH) und in bauen.sh des
        // AppImage. Wer ihn hier aendert, ohne die zwei anderen mitzuziehen, schaltet die
        // Texterkennung im AppImage STILL ab -- der Bau bliebe gruen und der Knopf
        // verschwaende nur (§4.64). Deshalb ein Waechter darauf.
        Assert.Equal("lib", TesseractBindung.EigenerUnterordner);
    }
}
