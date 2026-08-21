using System.Windows;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Wächter über die zwei Fassungen derselben Absatzvorlagen.
///
/// <para>
/// Seit §4.39 hält <see cref="TdStil.Alle"/> in Core die Vorlagentabelle, aus der sich der
/// Avalonia-Kopf bedient. Der WPF-Kopf liest weiterhin sein eigenes <c>TextStyles.All</c> —
/// bewusst, denn es arbeitet mit WPF-Typen (<c>FontWeight</c>, <c>Thickness</c>) auf einem
/// <c>FlowDocument</c>, und ein Umbau im laufenden Kopf wäre einer ohne Gegenwert.
/// </para>
/// <para>
/// Damit stehen dieselben zehn Vorlagen an zwei Stellen, und <b>zwei Wahrheiten laufen
/// auseinander, sobald es niemand nachhält</b> — das Ergebnis wäre **dasselbe Dokument in zwei
/// Schriftbildern**, je nachdem, welcher Kopf die Überschrift gesetzt hat. Genau das verhindert
/// dieser Wächter: <b>dieselbe Lösung wie bei der Farbtabelle</b> (§4.9,
/// <see cref="FarbtabelleTests"/>), zum zweiten Mal.
/// </para>
/// <para>
/// <b>Verglichen wird, was auf dem Papier sichtbar ist</b> — Größe, Fett, Kursiv, Farbe,
/// Abstände davor und danach, Einzüge, Gliederungsebene.
/// </para>
/// <para>
/// <b>⚠ Und zwar seit §4.46 in derselben Einheit — davor hat genau das gefehlt.</b> Der
/// Wächter verglich die <i>Zahlen</i> und nicht die <i>Größen</i>: <c>TextStyles</c> misst in
/// geräteunabhängigen <b>Pixeln</b>, <c>TdStil</c> in <b>Punkt</b>, und bei 96 dpi ist ein
/// Punkt <b>1,333</b> Pixel. Über den Abständen stand hier sogar wörtlich „bei 96 dpi ist
/// beides dieselbe Zahl" — das ist falsch, und es war die Stelle, an der die Verwechslung
/// festgeschrieben wurde. <b>Grün war er trotzdem</b>, jahrelang, während „Überschrift 1" im
/// Linux-Kopf 28 pt und im WPF-Kopf 21 pt groß war — <b>also genau der Zustand, den dieser
/// Wächter laut seinem eigenen Kommentar verhindern sollte.</b>
/// </para>
/// <para>
/// <b>Die Lehre steht hier und nicht nur im HANDOFF:</b> Ein Wächter, der zwei Zahlen
/// vergleicht, prüft nichts über die Welt, solange nicht auch ihre <b>Einheit</b> geprüft ist.
/// Deshalb rechnet er jetzt um, und deshalb sind die Einzüge nicht mehr ausgenommen —
/// „eine Umrechnung wäre eine dritte Wahrheit" hieß in Wahrheit: hier wird nicht hingesehen.
/// </para>
/// </summary>
public class VorlagentabelleTests
{
    /// <summary>WPF rechnet in geräteunabhängigen Pixeln: 96 auf ein Zoll, 72 Punkt auf ein Zoll.</summary>
    private const double PunktProPixel = 72.0 / 96.0;

    /// <inheritdoc cref="PunktProPixel"/>
    private const double CmProPixel = 2.54 / 96.0;

    [Fact]
    public void Beide_Tabellen_haben_dieselben_Vorlagen_in_derselben_Reihenfolge()
    {
        Assert.Equal(
            TextStyles.All.Select(s => s.Name).ToArray(),
            TdStil.Alle.Select(s => s.Name).ToArray());
    }

    [Fact]
    public void Jede_Vorlage_hat_beidseits_dieselben_Werte()
    {
        foreach (var (wpf, core) in TextStyles.All.Zip(TdStil.Alle))
        {
            Assert.Equal(wpf.Key, core.Key);
            Assert.Equal(wpf.Weight == FontWeights.Bold, core.Bold);
            Assert.Equal(wpf.Style == FontStyles.Italic, core.Italic);
            Assert.Equal(wpf.ColorHex, core.ColorHex);
            Assert.Equal(wpf.HeadingLevel, core.Heading);

            // **Umgerechnet und nicht verglichen.** WPF misst in geräteunabhängigen Pixeln,
            // Core in Punkt bzw. Zentimetern. Die drei Zeilen darunter sind der ganze Inhalt
            // dieses Wächters — vor §4.46 fehlte an jeder der Faktor, und er war grün.
            Assert.Equal(wpf.Size * PunktProPixel, core.SizePt, 6);

            Assert.Equal(wpf.Margin.Top * PunktProPixel, core.BeforePt, 6);
            Assert.Equal(wpf.Margin.Bottom * PunktProPixel, core.AfterPt, 6);

            Assert.Equal(wpf.Margin.Left * CmProPixel, core.LeftCm, 6);
            Assert.Equal(wpf.Margin.Right * CmProPixel, core.RightCm, 6);
        }
    }

    /// <summary>
    /// <b>Der Wächter über den Wächter: die beiden Tabellen stehen wirklich in verschiedenen
    /// Einheiten.</b>
    ///
    /// <para>
    /// Ohne ihn ließe sich der Vergleich oben jederzeit wieder „vereinfachen", indem jemand
    /// die Faktoren herauskürzt — und er wäre erneut grün und erneut blind. Diese Zeile hält
    /// fest, <b>warum</b> dort ein Faktor steht: Eine Überschrift 1 ist im WPF-Kopf
    /// <b>28 Pixel</b> und im Modell <b>21 Punkt</b>, und das ist dieselbe Größe.
    /// </para>
    /// </summary>
    [Fact]
    public void Die_beiden_Tabellen_messen_in_verschiedenen_Einheiten()
    {
        var wpf = TextStyles.ForHeading(1);
        var core = TdStil.ZurEbene(1)!.Value;

        Assert.Equal(28, wpf.Size);        // geräteunabhängige Pixel
        Assert.Equal(21, core.SizePt);     // Punkt — dieselbe Größe auf dem Papier
    }

    /// <summary>
    /// Die Größe des Fließtextes steht in beiden Tabellen — und sie ist die Zahl, gegen die
    /// <see cref="TdStil.Passt"/> eine Vorlage wiedererkennt. <b>15 Pixel sind 11,25 Punkt.</b>
    /// </summary>
    [Fact]
    public void Die_Koerpergroesse_stimmt_ueberein()
    {
        Assert.Equal(TextStyles.BodySize * PunktProPixel, TdStil.KoerperPt, 6);
    }

    /// <summary>
    /// <b>Ein unberührter Absatz wird als „Standard" wiedererkannt.</b>
    ///
    /// <para>
    /// <b>Das ist der Wächter, an dem der Fehler aus §4.46 im eigenen Kopf sichtbar gewesen
    /// wäre</b>, ganz ohne Vergleich mit dem anderen: Ein Absatz ohne eigene Größe wird über
    /// <see cref="TdCharFormat.Standard"/> mit <b>11 pt</b> gesetzt. Solange
    /// <see cref="TdStil.KoerperPt"/> auf 15 stand, passte er auf <b>keine</b> Vorlage — und
    /// „Standard" anzuwenden vergrößerte den Text um ein Drittel, obwohl es die Vorlage ist,
    /// die nichts ändern sollte.
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_unberuehrter_Absatz_ist_die_Vorlage_Standard()
    {
        var unberuehrt = new TdCharFormat().Aufgeloest();

        Assert.True(TdStil.Standard.Passt(unberuehrt));
    }
}
