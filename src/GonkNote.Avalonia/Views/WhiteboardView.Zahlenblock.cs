using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GonkNote.Core.Editing;

namespace GonkNote.Views;

/// <summary>
/// Die Zahleneingabe an der Werkzeuggröße (Vorbild Adobe Fresco): langes Drücken auf Symbol,
/// Schieber oder Wertanzeige öffnet ein Ziffernfeld, und was dort steht, geht direkt auf die
/// Strichstärke bzw. — beim Radierer — auf dessen Größe.
///
/// <para>
/// <b>Warum es das überhaupt gibt.</b> Ein Schieber von 110 Pixeln Breite kann eine Größe
/// nicht genau treffen, und mit dem Stift trifft er sie noch schlechter. Wer 6,5 will, soll
/// 6,5 tippen können.
/// </para>
///
/// <para>
/// <b>Gerechnet wird in Core</b> (<see cref="WbZahlenblock"/>, §4.61): welche Taste die
/// Eingabe wie verändert, wann sie abgelehnt wird, was die Anzeige zeigt und was auf den
/// Schieber geht. Hier steht nur der Weg dorthin — Uhr, Popup und die drei Auslöser.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    private DispatcherTimer? _groesseUhr;
    private Point _groesseStart;
    private string _zahlenblockEingabe = "";

    // ==================== Langdruck auf einen der drei Auslöser ====================

    /// <summary>
    /// Hängt die drei Auslöser an — <b>am Tunnel und mit <c>handledEventsToo</c></b>.
    /// <para>
    /// <b>Am laufenden Programm gemessen (V2-84):</b> als gewöhnliches Bubble-Ereignis im
    /// XAML kam der Handler nie an. Der <c>Slider</c> behandelt den Druck selbst und
    /// markiert ihn als erledigt; der Langdruck fing gar nicht erst an, der Schieber sprang
    /// auf den angeklickten Wert, und sonst geschah nichts. <b>Der Bau war dabei grün.</b>
    /// </para>
    /// <para>
    /// Der Tunnel läuft <b>vor</b> dem Steuerelement, also vor dem Slider — und weil hier
    /// nichts als erledigt markiert wird, bedient der Schieber sich danach ganz normal.
    /// Genau das ist gewollt: kurz drücken zieht, lange drücken öffnet den Block.
    /// </para>
    /// </summary>
    private void ZahlenblockAnhaengen()
    {
        foreach (var ausloeser in new Control[] { WidthIcon, WidthSlider, WidthLabel })
        {
            ausloeser.AddHandler(PointerPressedEvent, Groesse_Gedrueckt,
                                 RoutingStrategies.Tunnel, handledEventsToo: true);
            ausloeser.AddHandler(PointerMovedEvent, Groesse_Bewegt,
                                 RoutingStrategies.Tunnel, handledEventsToo: true);
            ausloeser.AddHandler(PointerReleasedEvent, Groesse_Losgelassen,
                                 RoutingStrategies.Tunnel, handledEventsToo: true);
        }
    }

    private void Groesse_Gedrueckt(object? sender, PointerPressedEventArgs e) =>
        GroesseHaltenBeginnt(e.GetPosition(this));

    private void Groesse_Bewegt(object? sender, PointerEventArgs e)
    {
        // Nur solange gedrückt wird — sonst bräche jedes Vorbeifahren den Druck ab, den es
        // gar nicht gibt.
        if (_groesseUhr != null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            GroesseHaltenBewegt(e.GetPosition(this));
    }

    private void Groesse_Losgelassen(object? sender, PointerReleasedEventArgs e) =>
        GroesseHaltenAbbrechen();

    private void GroesseHaltenBeginnt(Point p)
    {
        GroesseHaltenAbbrechen();
        _groesseStart = p;
        _groesseUhr = new DispatcherTimer { Interval = WbZahlenblock.Haltedauer };
        _groesseUhr.Tick += GroesseHaltenAbgelaufen;
        _groesseUhr.Start();
    }

    private void GroesseHaltenBewegt(Point p)
    {
        // Wird am Schieber gezogen, ist es kein Langdruck — sonst wäre der Schieber
        // unbenutzbar, weil jedes langsame Ziehen den Block aufklappte.
        if (WbZahlenblock.IstZiehen(_groesseStart.X, _groesseStart.Y, p.X, p.Y))
            GroesseHaltenAbbrechen();
    }

    private void GroesseHaltenAbbrechen()
    {
        if (_groesseUhr == null) return;
        _groesseUhr.Stop();
        _groesseUhr.Tick -= GroesseHaltenAbgelaufen;
        _groesseUhr = null;
    }

    private void GroesseHaltenAbgelaufen(object? sender, EventArgs e)
    {
        GroesseHaltenAbbrechen();
        ZahlenblockOeffnen();
    }

    // ==================== Der Block ====================

    private void ZahlenblockOeffnen()
    {
        _zahlenblockEingabe = "";
        // Beim Aufmachen steht der **aktuelle** Wert da und nicht „0": der Nutzer soll sehen,
        // wovon er kommt. Die leere Eingabe darunter heißt, dass die erste Ziffer neu anfängt.
        ZahlenblockAnzeige.Text = ActiveSize.ToString("0.#");
        Zahlenblock.IsOpen = true;
    }

    private void ZahlenblockSchliessen() => Zahlenblock.IsOpen = false;

    private void Ziffer_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Content is not string taste) return;

        // null heißt abgelehnt: zweites Komma, zweite Nachkommastelle, über dem Höchstwert.
        if (WbZahlenblock.Taste(_zahlenblockEingabe, taste, WidthSlider.Maximum) is not { } naechste)
            return;

        _zahlenblockEingabe = naechste;
        ZahlenblockUebernehmen();
    }

    private void Rueckschritt_Click(object? sender, RoutedEventArgs e)
    {
        _zahlenblockEingabe = WbZahlenblock.Rueckschritt(_zahlenblockEingabe);
        ZahlenblockUebernehmen();
    }

    private void ZahlenblockUebernehmen()
    {
        ZahlenblockAnzeige.Text = WbZahlenblock.Anzeige(_zahlenblockEingabe);

        // Setzt über WidthSlider_Changed die Größe — dieselbe Naht, die auch das Ziehen
        // am Schieber benutzt. Wer hier direkt _width setzte, hätte zwei Wege zum selben Wert.
        if (WbZahlenblock.Schieberwert(_zahlenblockEingabe, WidthSlider.Minimum) is { } wert)
            WidthSlider.Value = wert;
    }
}
