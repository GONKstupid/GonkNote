using GonkNote.Core.Editing;
using GonkNote.Services;

namespace GonkNote.Core.Tests;

/// <summary>
/// Wächter für <see cref="Bildschirmtastatur"/> — die eingebaute Tastatur (Nutzerwunsch
/// 2026-09-09).
///
/// <para>
/// <b>Was hier geprüft wird, ist nicht das Aussehen, sondern die eine Zusage, die zählt:
/// was draufsteht, kommt heraus.</b> Eine Tastatur, deren Beschriftung und deren Zeichen
/// auseinanderlaufen, sieht auf jedem Bildschirmfoto richtig aus und schreibt trotzdem
/// falsch — das merkt erst der Nutzer, und zwar an einer Stelle, an der er es nicht mehr
/// zuordnen kann.
/// </para>
/// </summary>
public class BildschirmtastaturTests
{
    private static IReadOnlyList<Bildschirmtastatur.Taste> AlleTasten(AppLanguage sprache) =>
        Bildschirmtastatur.Reihen(sprache).SelectMany(r => r).ToList();

    // ==================== Was draufsteht, kommt heraus ====================

    [Fact]
    public void Jede_Zeichentaste_schreibt_genau_das_was_auf_ihr_steht()
    {
        foreach (var sprache in new[] { AppLanguage.German, AppLanguage.English })
            foreach (var stand in new[]
                     {
                         Bildschirmtastatur.Stand.Grund,
                         new Bildschirmtastatur.Stand(Umschalt: true, AltGr: false),
                         new Bildschirmtastatur.Stand(Umschalt: false, AltGr: true),
                     })
                foreach (var taste in AlleTasten(sprache))
                {
                    if (taste.Art != Bildschirmtastatur.Art.Zeichen) continue;
                    Assert.Equal(Bildschirmtastatur.Beschriftung(taste, stand),
                                 Bildschirmtastatur.Zeichen(taste, stand));
                }
    }

    /// <summary>
    /// <b>Keine Zeichentaste darf leer sein</b> — auch nicht in der AltGr-Ebene. Eine leere
    /// Taste sieht kaputt aus und schreibt nichts; wer sie drückt, weiß nicht, ob die
    /// Tastatur klemmt oder die Taste unbelegt ist.
    /// </summary>
    [Fact]
    public void Keine_Taste_bleibt_in_irgendeiner_Ebene_leer()
    {
        foreach (var sprache in new[] { AppLanguage.German, AppLanguage.English })
            foreach (var taste in AlleTasten(sprache))
            {
                if (taste.Art is Bildschirmtastatur.Art.Leer) continue;
                foreach (var stand in new[]
                         {
                             Bildschirmtastatur.Stand.Grund,
                             new Bildschirmtastatur.Stand(true, false),
                             new Bildschirmtastatur.Stand(false, true),
                         })
                    Assert.NotEqual("", Bildschirmtastatur.Beschriftung(taste, stand));
            }
    }

    // ==================== Die Umschalter ====================

    [Fact]
    public void Umschalt_gilt_fuer_genau_eine_Taste()
    {
        var umschalt = AlleTasten(AppLanguage.German)
            .First(t => t.Art == Bildschirmtastatur.Art.Umschalt);
        var a = AlleTasten(AppLanguage.German).First(t => t.Normal == "a");

        var nachUmschalt = Bildschirmtastatur.NaechsterStand(umschalt, Bildschirmtastatur.Stand.Grund);
        Assert.True(nachUmschalt.Umschalt);
        Assert.Equal("A", Bildschirmtastatur.Zeichen(a, nachUmschalt));

        // ...und danach ist er wieder weg.
        Assert.False(Bildschirmtastatur.NaechsterStand(a, nachUmschalt).Umschalt);
    }

    [Fact]
    public void Ein_zweiter_Druck_hebt_den_Umschalter_wieder_auf()
    {
        var umschalt = AlleTasten(AppLanguage.German)
            .First(t => t.Art == Bildschirmtastatur.Art.Umschalt);

        var eins = Bildschirmtastatur.NaechsterStand(umschalt, Bildschirmtastatur.Stand.Grund);
        var zwei = Bildschirmtastatur.NaechsterStand(umschalt, eins);

        Assert.False(zwei.Umschalt);
    }

    /// <summary>
    /// <b>Die Rücktaste verbraucht den Umschalter nicht.</b> Wer <c>⇧</c> drückt und sich
    /// vertippt, will nach der Korrektur immer noch groß schreiben — sonst muss er den
    /// Umschalter nach jedem Verschreiber neu drücken.
    /// </summary>
    [Fact]
    public void Ruecktaste_und_Pfeile_verbrauchen_den_Umschalter_nicht()
    {
        var gehalten = new Bildschirmtastatur.Stand(Umschalt: true, AltGr: false);

        foreach (var art in new[]
                 {
                     Bildschirmtastatur.Art.Rueck, Bildschirmtastatur.Art.Links,
                     Bildschirmtastatur.Art.Rechts, Bildschirmtastatur.Art.Tab,
                 })
        {
            var taste = new Bildschirmtastatur.Taste(art, "x");
            Assert.True(Bildschirmtastatur.NaechsterStand(taste, gehalten).Umschalt);
        }
    }

    [Fact]
    public void AltGr_schlaegt_Umschalt_nur_dort_wo_es_eine_dritte_Ebene_gibt()
    {
        var beide = new Bildschirmtastatur.Stand(Umschalt: true, AltGr: true);

        var mitDritter = AlleTasten(AppLanguage.German).First(t => t.AltGr is not null);
        Assert.Equal(mitDritter.AltGr, Bildschirmtastatur.Beschriftung(mitDritter, beide));

        // Ohne dritte Ebene bleibt es bei der Umschalt-Ebene und wird nicht leer.
        var ohneDritte = AlleTasten(AppLanguage.German)
            .First(t => t.Art == Bildschirmtastatur.Art.Zeichen && t.AltGr is null);
        Assert.Equal(ohneDritte.Umschaltet, Bildschirmtastatur.Beschriftung(ohneDritte, beide));
    }

    // ==================== Die zwei Belegungen ====================

    /// <summary>
    /// <b>Deutsch ist QWERTZ, Englisch ist QWERTY</b> — der Unterschied, an dem man sofort
    /// sieht, ob die richtige Belegung geladen ist. Ein vertauschtes Y und Z ist der
    /// klassische Fehler, und er fällt beim Tippen erst beim ersten „z" auf.
    /// </summary>
    [Fact]
    public void Deutsch_ist_QWERTZ_und_Englisch_QWERTY()
    {
        static string ZweiteReihe(AppLanguage s) =>
            string.Concat(Bildschirmtastatur.Reihen(s)[1]
                .Where(t => t.Art == Bildschirmtastatur.Art.Zeichen)
                .Take(6).Select(t => t.Normal));

        Assert.Equal("qwertz", ZweiteReihe(AppLanguage.German));
        Assert.Equal("qwerty", ZweiteReihe(AppLanguage.English));
    }

    [Fact]
    public void Die_deutsche_Belegung_hat_ihre_Umlaute_und_das_scharfe_s()
    {
        var zeichen = AlleTasten(AppLanguage.German)
            .Where(t => t.Art == Bildschirmtastatur.Art.Zeichen)
            .SelectMany(t => new[] { t.Normal, t.Umschaltet })
            .ToHashSet();

        foreach (var z in new[] { "ä", "ö", "ü", "Ä", "Ö", "Ü", "ß" })
            Assert.Contains(z, zeichen);
    }

    /// <summary>
    /// Jede Belegung muss die Tasten haben, ohne die man nicht schreiben kann. <b>Die Liste
    /// ist kurz und absichtlich hart:</b> fehlte eine davon, wäre die Tastatur auf einem
    /// Gerät ohne Hardware-Tastatur nicht benutzbar, und der Bau bliebe grün.
    /// </summary>
    [Fact]
    public void Jede_Belegung_hat_die_unverzichtbaren_Tasten()
    {
        foreach (var sprache in new[] { AppLanguage.German, AppLanguage.English })
        {
            var arten = AlleTasten(sprache).Select(t => t.Art).ToHashSet();
            foreach (var noetig in new[]
                     {
                         Bildschirmtastatur.Art.Rueck, Bildschirmtastatur.Art.Eingabe,
                         Bildschirmtastatur.Art.Leer, Bildschirmtastatur.Art.Umschalt,
                         Bildschirmtastatur.Art.Links, Bildschirmtastatur.Art.Rechts,
                         Bildschirmtastatur.Art.Zu,
                     })
                Assert.Contains(noetig, arten);
        }
    }

    /// <summary>
    /// Eine unbekannte Sprache bekommt die englische Belegung und keine leere Fläche.
    /// </summary>
    [Fact]
    public void Eine_Sprache_ohne_eigene_Belegung_bekommt_die_englische()
    {
        Assert.Same(Bildschirmtastatur.Reihen(AppLanguage.English),
                    Bildschirmtastatur.Reihen((AppLanguage)99));
    }

    // ==================== Die Aufteilung der Fläche ====================

    [Fact]
    public void Die_Grundbreiten_richten_sich_nach_der_breitesten_Reihe()
    {
        foreach (var sprache in new[] { AppLanguage.German, AppLanguage.English })
        {
            var reihen = Bildschirmtastatur.Reihen(sprache);
            double breiteste = Bildschirmtastatur.Grundbreiten(reihen);

            Assert.True(breiteste > 0);
            foreach (var reihe in reihen)
                Assert.True(reihe.Sum(t => t.Breite) <= breiteste + 0.001);
        }
    }
}
