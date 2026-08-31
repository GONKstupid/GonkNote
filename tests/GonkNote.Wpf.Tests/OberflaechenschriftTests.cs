using System.Windows.Media;
using GonkNote.Core.Theming;
// Wie in AppFonts: `Fonts` gibt es zweimal, gemeint ist die Tabelle aus Core.
using Schriften = GonkNote.Core.Theming.Fonts;
using GonkNote.Services;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Wächter darüber, dass der WPF-Kopf seine Oberfläche wirklich in den <b>mitgelieferten</b>
/// Schriften zeichnet — und nicht in der Systemschrift dahinter.
///
/// <para>
/// <b>Warum es diesen Wächter gibt</b> (HANDOFF §4.71). Von §4.26 bis zum 2026-08-29 stand in
/// <c>Themes/Styles.xaml</c> <c>./Fonts/Inter/#Inter, Segoe UI</c>. Ein <i>relativer</i>
/// Schrift-URI wird in WPF gegen die Basis des XAML aufgelöst, und die ist bei einem
/// eingebundenen Wörterbuch <c>pack://application:,,,/GonkNote;component/Themes/</c> — also
/// <b>in</b> der Assembly, wo keine Schrift liegt. <b>Die App zeichnete die ganze Zeit in
/// Segoe UI.</b>
/// </para>
/// <para>
/// <b>Kein Test konnte das sehen, und das ist der Punkt.</b> <c>SchriftkonzeptTests</c> in
/// Core prüft, dass jede Rolle auf eine mitgelieferte Familie zeigt und dass jede Datei
/// danebenliegt — beides stimmte. Geprüft hat niemand, ob der <b>Kopf</b> sie auch findet.
/// Gefunden wurde es erst im Bild-neben-Bild-Vergleich mit dem Linux-Kopf.
/// </para>
/// <para>
/// <b>Und es ist genau die Sorte Fehler, die ein Rückfall am Leben hält:</b> Weil hinter der
/// Datei „Segoe UI" steht, sah nichts kaputt aus. Ein Rückfall, der immer greift, ist kein
/// Rückfall mehr, sondern die Einstellung.
/// </para>
/// <para>
/// <b>Auf einem STA-Thread</b>, wie alles hier: <see cref="FontFamily"/> löst beim Zugriff auf
/// <see cref="FontFamily.FamilyNames"/> die Datei auf und braucht dafür WPFs Umfeld.
/// </para>
/// </summary>
public sealed class OberflaechenschriftTests
{
    /// <summary>
    /// <b>Die eine Frage, die elf Monate lang niemand gestellt hat:</b> Kommt bei
    /// <see cref="AppFonts.Family"/> die mitgelieferte Familie heraus — oder der Rückfall?
    ///
    /// <para>
    /// <see cref="FontFamily.FamilyNames"/> nennt die Familie, die WPF <i>tatsächlich</i>
    /// aufgelöst hat. Steht dort der Name aus dem Schema, ist die Datei geladen; steht dort
    /// die Systemschrift, greift der Rückfall — und der Test schlägt an.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData(FontRole.Ui, "Segoe UI")]
    [InlineData(FontRole.Mono, "Consolas")]
    [InlineData(FontRole.Display, "Segoe UI")]
    public void Die_Oberflaeche_bekommt_die_mitgelieferte_Schrift(FontRole rolle, string rueckfall)
        => Sta.Run(() =>
        {
            string erwartet = Schriften.Standard.Family(rolle);
            var familie = AppFonts.Family(rolle, rueckfall);

            string aufgeloest = string.Join(", ", familie.FamilyNames.Values);

            Assert.True(
                AppFonts.Mitgeliefert(rolle, rueckfall),
                $"Die Rolle {rolle} sollte in „{erwartet}“ gezeichnet werden, "
                + $"aufgelöst wurde aber „{aufgeloest}“. Steht dort der "
                + $"Rückfall „{rueckfall}“, findet der Kopf die mitgelieferte "
                + "Datei nicht — genau der Fehler aus HANDOFF §4.71.");
        });

    /// <summary>
    /// <b>Der Rückfall bleibt trotzdem stehen.</b> Er ist richtig für einen unvollständigen
    /// Ausgabeordner (§4.26) — nur eben nicht als Normalfall. Der Test hält fest, dass er
    /// weiterhin Teil der Angabe ist und nicht beim Beheben des Fehlers herausgeflogen ist.
    /// </summary>
    [Fact]
    public void Hinter_der_mitgelieferten_Schrift_steht_weiterhin_eine_Systemschrift()
        => Sta.Run(() =>
        {
            var familie = AppFonts.Family(FontRole.Ui, "Segoe UI");
            Assert.Contains("Segoe UI", familie.Source);
        });

    /// <summary>
    /// <b>Die Basis ist absolut.</b> Eine relative Basis war die Ursache; dass sie es nicht
    /// wieder wird, steht hier und nicht nur im Kommentar.
    /// </summary>
    [Fact]
    public void Die_Schriftbasis_zeigt_auf_den_Programmordner()
    {
        Assert.True(AppFonts.Basis.IsAbsoluteUri);
        Assert.True(AppFonts.Basis.IsFile);
    }
}
