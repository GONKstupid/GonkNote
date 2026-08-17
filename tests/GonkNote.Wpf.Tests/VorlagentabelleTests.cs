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
/// Abstände davor und danach, Gliederungsebene. **Nicht** verglichen werden die linken und
/// rechten Einzüge des Zitats: WPF misst sie in geräteunabhängigen Pixeln und Core in
/// Zentimetern, und eine Umrechnung im Wächter wäre eine dritte Wahrheit.
/// </para>
/// </summary>
public class VorlagentabelleTests
{
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
            Assert.Equal(wpf.Size, core.SizePt);
            Assert.Equal(wpf.Weight == FontWeights.Bold, core.Bold);
            Assert.Equal(wpf.Style == FontStyles.Italic, core.Italic);
            Assert.Equal(wpf.ColorHex, core.ColorHex);
            Assert.Equal(wpf.HeadingLevel, core.Heading);

            // Abstände: WPF führt sie als Rand in geräteunabhängigen Pixeln, Core in Punkt —
            // bei 96 dpi ist beides dieselbe Zahl, und genau deshalb stehen sie hier gleich.
            Assert.Equal(wpf.Margin.Top, core.BeforePt);
            Assert.Equal(wpf.Margin.Bottom, core.AfterPt);
        }
    }

    /// <summary>
    /// Die Größe des Fließtextes steht in beiden Tabellen — und sie ist die Zahl, gegen die
    /// <see cref="TdStil.Passt"/> eine Vorlage wiedererkennt.
    /// </summary>
    [Fact]
    public void Die_Koerpergroesse_stimmt_ueberein()
    {
        Assert.Equal(TextStyles.BodySize, TdStil.KoerperPt);
    }
}
