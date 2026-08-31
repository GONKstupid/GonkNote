using System.IO;
using GonkNote.Core.Platform;
using GonkNote.Core.Services;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die Cover-Sammlung — <see cref="CoverLibrary"/> (Phase 5, Schritt ①c, §4.81).
///
/// <para>
/// <b>Warum das einen Wächter verdient:</b> Die Regel „welche Dateien zählen, wie sie
/// gruppiert und sortiert sind" lag bis §4.81 <b>privat im WPF-Kopf</b>. Ein zweiter Kopf
/// hätte sie ein zweites Mal bekommen — und die erste Abweichung wäre eine Sammlung, die auf
/// zwei Systemen verschieden aussieht, während <b>beide Fassungen jeden Test bestehen</b>
/// (§4.13, dieselbe Falle wie bei der Formatliste in §4.77).
/// </para>
/// <para>
/// <b>Gemessen wird gegen echte Ordner in einem Wegwerf-Verzeichnis</b>, nicht gegen eine
/// Attrappe: Was hier zählt, ist genau das Zusammenspiel aus Dateisystem, Sortierung und
/// Gruppierung — eine Attrappe prüfte davon nichts.
/// </para>
/// </summary>
public sealed class CoverSammlungTests : IDisposable
{
    private readonly TempWorkspace _arbeit = new("cover");
    private readonly IAppPaths _vorher;

    /// <summary>Zwei Wegwerf-Ordner statt der echten — mehr braucht es nicht.</summary>
    private sealed record Wegwerfpfade(string AppFolder, string DataFolder) : IAppPaths;

    /// <summary>
    /// <b>Nie in den echten Ordnern testen</b> (§7): <see cref="AppPaths.Current"/> zeigt für
    /// die Dauer des Tests in ein Wegwerf-Verzeichnis und wird danach zurückgestellt.
    /// </summary>
    public CoverSammlungTests()
    {
        _vorher = AppPaths.Current;
        AppPaths.Current = new Wegwerfpfade(
            Path.Combine(_arbeit.Root, "app"), Path.Combine(_arbeit.Root, "daten"));
    }

    public void Dispose()
    {
        AppPaths.Current = _vorher;
        _arbeit.Dispose();
    }

    private static void Bild(string ordner, string name)
    {
        Directory.CreateDirectory(ordner);
        // Der Inhalt ist gleichgültig — CoverLibrary entscheidet an der Endung, nicht am Bild.
        File.WriteAllBytes(Path.Combine(ordner, name), [0x89, 0x50, 0x4E, 0x47]);
    }

    private string Mitgeliefert(string gruppe) => Path.Combine(CoverLibrary.AppFolder, gruppe);

    // ==================== Die Gruppen ====================

    [Fact]
    public void Ohne_Ordner_gibt_es_keine_Gruppen()
    {
        Assert.Empty(CoverLibrary.Gruppen());
    }

    /// <summary>
    /// <b>Unterordner sind Gruppen, alphabetisch.</b> Die Reihenfolge ist die Aussage — eine
    /// Sammlung, die bei jedem Start anders sortiert ist, lässt sich nicht wiederfinden.
    /// </summary>
    [Fact]
    public void Unterordner_werden_zu_Gruppen_in_alphabetischer_Ordnung()
    {
        Bild(Mitgeliefert("Muster"), "a.jpg");
        Bild(Mitgeliefert("Basic"), "b.jpg");
        Bild(Mitgeliefert("Pixel Art"), "c.png");

        var namen = CoverLibrary.Gruppen().Select(g => g.Name).ToList();

        Assert.Equal(["Basic", "Muster", "Pixel Art"], namen);
    }

    /// <summary>
    /// <b>Ein leerer Ordner ist keine Gruppe.</b> Eine Überschrift ohne Inhalt sieht aus wie
    /// ein Fehler — und wer die Sammlung aufklappt, sucht dann nach der Ursache.
    /// </summary>
    [Fact]
    public void Ein_leerer_Ordner_wird_uebersprungen()
    {
        Directory.CreateDirectory(Mitgeliefert("Leer"));
        Bild(Mitgeliefert("Voll"), "a.jpg");

        var namen = CoverLibrary.Gruppen().Select(g => g.Name).ToList();

        Assert.Equal(["Voll"], namen);
    }

    /// <summary>
    /// <b>Was keine Bilddatei ist, zählt nicht.</b> Eine Textdatei im Cover-Ordner ergäbe
    /// sonst eine Kachel mit einem Fragezeichen — und die sieht aus wie ein kaputtes Cover.
    /// </summary>
    [Fact]
    public void Fremde_Dateien_zaehlen_nicht()
    {
        string ordner = Mitgeliefert("Basic");
        Bild(ordner, "echt.jpg");
        File.WriteAllText(Path.Combine(ordner, "liesmich.txt"), "kein Bild");

        var gruppe = Assert.Single(CoverLibrary.Gruppen());

        Assert.Equal(["echt.jpg"], gruppe.Dateien.Select(Path.GetFileName));
    }

    // ==================== Die eigene Gruppe ====================

    /// <summary>
    /// <b>Die eigenen Vorlagen stehen zuletzt</b> — dieselbe Regel wie bei den Stickern
    /// (§4.54): was der Nutzer selbst hinzugefügt hat, steht dort, wo er es zuletzt gesehen
    /// hat.
    /// </summary>
    [Fact]
    public void Eigene_Vorlagen_stehen_am_Ende_und_sind_gekennzeichnet()
    {
        Bild(Mitgeliefert("Muster"), "a.jpg");
        Bild(Mitgeliefert("Basic"), "b.jpg");
        Bild(CoverLibrary.UserFolder, "meins.png");

        var gruppen = CoverLibrary.Gruppen();

        Assert.Equal(["Basic", "Muster", CoverLibrary.IndividuellName],
                     gruppen.Select(g => g.Name));
        Assert.False(gruppen[0].Eigene);
        Assert.False(gruppen[1].Eigene);
        Assert.True(gruppen[^1].Eigene, "Die eigene Gruppe ist nicht als solche gekennzeichnet.");
    }

    [Fact]
    public void Ohne_eigene_Vorlagen_gibt_es_die_Gruppe_nicht()
    {
        Bild(Mitgeliefert("Basic"), "a.jpg");

        Assert.DoesNotContain(CoverLibrary.Gruppen(), g => g.Eigene);
    }

    /// <summary>
    /// <b>Der Nutzerordner wird angelegt, wenn er fehlt</b> — ein Ordner, den man erst suchen
    /// muss, wird nicht benutzt. Dieselbe Zusage wie <see cref="StickerLibrary.UserFolder"/>.
    /// </summary>
    [Fact]
    public void Der_eigene_Ordner_entsteht_beim_ersten_Zugriff()
    {
        string ordner = CoverLibrary.UserFolder;

        Assert.True(Directory.Exists(ordner), $"{ordner} wurde nicht angelegt.");
    }

    /// <summary>
    /// <b>Eigene Vorlagen liegen flach, nicht in Unterordnern.</b> Dorthin kopiert die
    /// „+"-Kachel; ein von Hand angelegter Unterordner darunter wäre eine zweite,
    /// unsichtbare Gruppierung.
    /// </summary>
    [Fact]
    public void Unterordner_beim_Nutzer_bilden_keine_eigenen_Gruppen()
    {
        Bild(CoverLibrary.UserFolder, "flach.png");
        Bild(Path.Combine(CoverLibrary.UserFolder, "tiefer"), "versteckt.png");

        var gruppe = Assert.Single(CoverLibrary.Gruppen());

        Assert.True(gruppe.Eigene);
        Assert.Equal(["flach.png"], gruppe.Dateien.Select(Path.GetFileName));
    }
}
