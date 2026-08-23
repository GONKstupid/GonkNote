using Avalonia.Interactivity;
using GonkNote.Core.Models;

namespace GonkNote.Views;

/// <summary>
/// Die Einstellungen-Seitenleiste rechts — <b>nur die Seite</b>: Muster, Farbton, Format,
/// Ausrichtung.
///
/// <para>
/// Das Gegenstück im WPF-Kopf (<c>WhiteboardView.Settings.cs</c>, 398 Zeilen) bedient
/// zusätzlich Formen, Text, Notizzettel, Sticker und Cover. Diese Werkzeuge gibt es im
/// Linux-Kopf nicht (nicht M1, HANDOFF §6), und ihre Abschnitte stehen deshalb hier auch
/// nicht — nicht vergessen, sondern ausgelassen.
/// </para>
///
/// <para>
/// <b>Es gibt keinen OK-Knopf.</b> Jede Änderung wirkt sofort auf die aktuelle Seite; das
/// ist dieselbe Bedienung wie drüben und der Grund, warum die Umschalter beim Spiegeln
/// stummgeschaltet werden müssen (<see cref="_stummeEinstellungen"/>) — sonst löste das
/// Setzen der Haken die Änderung aus, die es nur abbilden soll.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    private bool _stummeEinstellungen;

    private void Einstellungen_Click(object? sender, RoutedEventArgs e)
    {
        if (EinstellungenLeiste.IsVisible)
        {
            EinstellungenLeiste.IsVisible = false;
            return;
        }

        // **Erst sichtbar machen, dann spiegeln.** Umgekehrt steigt das Spiegeln sofort
        // wieder aus (es tut nichts an einer verborgenen Leiste), und die Leiste ginge mit
        // lauter leeren Umschaltern auf — am laufenden Programm genau so gesehen.
        EinstellungenLeiste.IsVisible = true;
        EinstellungenSpiegeln();
    }

    /// <summary>
    /// Bildet die aktuelle Seite in den Umschaltern ab. Wird von allem gerufen, was
    /// <see cref="_page"/> wechselt — sonst zeigte die offene Leiste nach einem
    /// Seitenwechsel die Einstellungen der vorigen Seite an, und die nächste Änderung
    /// schriebe sie der neuen auf.
    /// </summary>
    private void EinstellungenSpiegeln()
    {
        if (_vm == null || _page == null || !EinstellungenLeiste.IsVisible) return;

        // Die Vorschauflächen der Werkzeug-Sektionen (Phase 4.5). Sie hängen nicht an der
        // Seite, sondern am gewählten Werkzeug — spiegeln muss man sie trotzdem hier, sonst
        // gehen sie leer auf. Genau das ist in §4.53 an der zweiten Aufklappstelle passiert.
        FuellvorschauNachfuehren();
        TextGrundVorschauNachfuehren();
        ZettelVorschauNachfuehren();

        _stummeEinstellungen = true;

        (_page.Background switch
        {
            PageBackground.Lines => SetzeMusterLinien,
            PageBackground.Grid => SetzeMusterKaro,
            PageBackground.Dots => SetzeMusterPunkte,
            _ => SetzeMusterBlanko,
        }).IsChecked = true;

        (_page.Shade switch
        {
            PageShade.Light => SetzeTonHell,
            PageShade.Dark => SetzeTonDunkel,
            _ => SetzeTonAuto,
        }).IsChecked = true;

        bool geheftet = !_page.IsInfinite;
        FormatAbschnitt.IsVisible = geheftet;
        if (geheftet)
        {
            // Erkannt an der langen Seite, nicht an Breite und Höhe einzeln — sonst fiele
            // ein Querformat-A4 durch, dessen Breite größer als die A4-Höhe ist.
            float lang = Math.Max(_page.Width, _page.Height);
            (lang > WhiteboardDoc.A4Height + 1 ? SetzeFormatA3 : SetzeFormatA4).IsChecked = true;
            (_page.Width > _page.Height ? SetzeQuerformat : SetzeHochformat).IsChecked = true;
        }

        CoverHinweis.IsVisible = _page.IsCover;

        _stummeEinstellungen = false;
    }

    /// <summary>
    /// Eine Änderung im Panel wirkt sofort auf die aktuelle Seite. Aufgebaut wie
    /// <c>PageSetting_Changed</c> im WPF-Kopf — bis auf die eine Zeile, die es dort nicht
    /// braucht und hier zwingend ist: siehe <see cref="RefreshAutoSwatch"/> unten.
    /// </summary>
    private void Seiteneinstellung_Geaendert(object? sender, RoutedEventArgs e)
    {
        if (_stummeEinstellungen || _vm == null || _page == null) return;

        _page.Background =
            SetzeMusterLinien.IsChecked == true ? PageBackground.Lines
            : SetzeMusterKaro.IsChecked == true ? PageBackground.Grid
            : SetzeMusterPunkte.IsChecked == true ? PageBackground.Dots
            : PageBackground.Blank;

        _page.Shade =
            SetzeTonHell.IsChecked == true ? PageShade.Light
            : SetzeTonDunkel.IsChecked == true ? PageShade.Dark
            : PageShade.Auto;

        if (!_page.IsInfinite)
        {
            bool a3 = SetzeFormatA3.IsChecked == true;
            float breit = a3 ? WhiteboardDoc.A3Width : WhiteboardDoc.A4Width;
            float hoch = a3 ? WhiteboardDoc.A3Height : WhiteboardDoc.A4Height;

            bool quer = SetzeQuerformat.IsChecked == true;
            float nb = quer ? hoch : breit, nh = quer ? breit : hoch;

            bool andereGroesse = Math.Abs(_page.Width - nb) > 0.5f || Math.Abs(_page.Height - nh) > 0.5f;
            _page.Width = nb;
            _page.Height = nh;
            // Ein anderes Blatt sitzt sonst außermittig oder halb außerhalb der Fläche.
            if (andereGroesse) CenterView();

            if (SetzeAlsStandard.IsChecked == true)
                _vm.Doc.NewPageTemplate = new PageTemplate
                {
                    Width = nb,
                    Height = nh,
                    Background = _page.Background,
                    Shade = _page.Shade,
                };
        }

        // **Der Farbton bestimmt die Vorgabetinte.** Wer den Farbton auf Dunkel stellt und
        // die Kachel stehen lässt, schriebe sonst mit der alten Tinte weiter — im
        // schlimmsten Fall dunkel auf dunkel. Der WPF-Kopf braucht die Zeile nicht, weil er
        // die Kachel im Zeichenpfad nachführt; hier ist das ausgeschlossen (HANDOFF §7,
        // „Render läuft im Renderdurchlauf").
        RefreshAutoSwatch();

        MarkDirty();
        Neuzeichnen();
    }
}
