using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Die Schnellaktionen: eine schwebende Leiste mit Ausschneiden, Kopieren, Duplizieren,
/// Einfügen, Löschen und Alles-Wählen.
///
/// <para>
/// <b>Ihr Zweck ist die Bedienung ohne Tastatur.</b> Mit der Maus gibt es Strg+C und Strg+V;
/// mit dem Stift in der Hand gibt es sie nicht. Deshalb öffnet sie auf <b>Rechtsklick</b> und
/// auf <b>langes Drücken</b> — und langes Drücken nur bei den Werkzeugen, die ohnehin nicht
/// zeichnen (Lasso, Verschieben, Hand): ein Stift ruht beim Schreiben oft kurz, und eine
/// Leiste, die dabei aufklappt, wäre eine Plage.
/// </para>
///
/// <para>
/// <b>Was hier nicht steht, und das ist Absicht:</b> das Aufklappen nach einer frischen
/// Auswahl, das der WPF-Kopf hat. Es legt die Leiste über die Oberkante der Auswahl, und
/// genau dort hängt der Dreh-Griff (§4.51; <c>SchnellaktionenTests</c> rechnet die
/// Überschneidung nach).
/// </para>
///
/// <para>
/// <b>Der Texterkennungs-Knopf ist seit Stück 6 da</b> (§4.64). Hier stand bis dahin, er
/// fehle — und dass <see cref="WbSchnellaktionen.Zustand"/> ihn ohnehin verbergen würde,
/// „solange keine Texterkennung da ist". Der zweite Halbsatz gilt weiter und ist jetzt der
/// eigentliche Punkt: <see cref="AvaloniaPlatformServices"/> liefert seit Stück 6 eine echte
/// Erkennung, aber ob sie <i>trägt</i>, entscheidet erst das System unter der App.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>Die Stelle auf der Fläche, an der die Leiste geöffnet wurde — dorthin wird eingefügt.</summary>
    private SKPoint _saStelle;

    private DispatcherTimer? _druckUhr;
    private Point _druckStart;
    private bool _druckVomFinger;

    /// <summary>Das Loslassen nach einem Langdruck nicht mehr als Ende einer Eingabe werten.</summary>
    private bool _druckEndeSchlucken;

    /// <summary>So lange muss gedrückt bleiben, bis die Leiste aufgeht.</summary>
    private static readonly TimeSpan Druckdauer = TimeSpan.FromMilliseconds(600);

    /// <summary>Ab dieser Bewegung ist es ein Zug und kein Druck.</summary>
    private const double DruckSpielraum = 10;

    /// <summary>Bei diesen Werkzeugen öffnet langes Drücken die Leiste — sie zeichnen nicht.</summary>
    private bool DruckWerkzeugAktiv => _tool is ToolType.Lasso or ToolType.Move or ToolType.Pan;

    // ==================== Aufmachen und zumachen ====================

    /// <summary>Darf die Leiste überhaupt aufgehen?</summary>
    private bool SchnellaktionenMoeglich =>
        _vm != null && _page != null && !EditFeld.IsVisible && !WarteSperre.IsVisible;

    /// <summary>
    /// Öffnet die Leiste an einer Zeigerstelle.
    /// <para>
    /// Ist nichts ausgewählt, wird das Element unter dem Zeiger angewählt — sonst stünde eine
    /// Leiste da, an der fast alles grau ist, und der Nutzer müsste erst zielen und dann
    /// noch einmal aufmachen.
    /// </para>
    /// </summary>
    private void SchnellaktionenZeigen(Point schirm)
    {
        if (!SchnellaktionenMoeglich) return;

        _saStelle = ToCanvas(schirm);

        // Derselbe Fangradius wie beim Anklicken (Input.cs): 5 Bildschirmpixel, durch den
        // Zoom geteilt — ein Strich soll bei jeder Vergrößerung gleich leicht zu treffen sein.
        if (_selection.Count == 0 &&
            WbHit.Topmost(_page!.Elements, _saStelle, 5f / Zoom) is { } treffer)
        {
            _selection.Add(treffer);
            ComputeSelectionBounds();
        }

        ZustandSpiegeln();
        Schnellaktionen.IsVisible = true;

        // **Erst messen, dann setzen.** Vor dem ersten Durchlauf ist Bounds leer, und die
        // Leiste säße mit halber Breite daneben.
        Schnellaktionen.Measure(Size.Infinity);
        var mass = new SKSize((float)Schnellaktionen.DesiredSize.Width,
                              (float)Schnellaktionen.DesiredSize.Height);

        var ecke = WbSchnellaktionen.ImBlick(
            WbSchnellaktionen.AmZeiger(new SKPoint((float)schirm.X, (float)schirm.Y), mass),
            mass,
            new SKSize((float)Skia.Bounds.Width, (float)Skia.Bounds.Height));

        Schnellaktionen.Margin = new Thickness(ecke.X, ecke.Y, 0, 0);
        Neuzeichnen();
    }

    private void SchnellaktionenVerbergen()
    {
        if (Schnellaktionen.IsVisible) Schnellaktionen.IsVisible = false;
    }

    /// <summary>
    /// Welcher Knopf gerade etwas tun kann — gerechnet in Core (<see cref="WbSchnellaktionen"/>),
    /// damit beide Köpfe dieselbe Regel haben.
    /// </summary>
    private void ZustandSpiegeln()
    {
        var z = WbSchnellaktionen.Rechnen(
            _selection, _page, _ablage.Count,
            App.Platform.Clipboard.HasImage,
            App.Platform.Ocr.IsAvailable);

        Sa_Ausschneiden.IsEnabled = z.Ausschneiden;
        Sa_Kopieren.IsEnabled = z.Kopieren;
        Sa_Duplizieren.IsEnabled = z.Duplizieren;
        Sa_Einfuegen.IsEnabled = z.Einfuegen;
        Sa_Loeschen.IsEnabled = z.Loeschen;
        Sa_AllesWaehlen.IsEnabled = z.AllesWaehlen;

        // Ausblenden statt ausgrauen, wenn es auf diesem System gar keine Erkennung gibt —
        // und der Trenner davor geht mit, sonst stünden zwei nebeneinander.
        Sa_Texterkennung.IsVisible = z.TexterkennungSichtbar;
        Sa_TrennerOcr.IsVisible = z.TexterkennungSichtbar;
        Sa_Texterkennung.IsEnabled = z.Texterkennung;
    }

    // ==================== Langes Drücken ====================

    private void DruckBeginnt(Point schirm, bool vomFinger)
    {
        DruckAbbrechen();
        if (!DruckWerkzeugAktiv || !SchnellaktionenMoeglich) return;

        _druckStart = schirm;
        _druckVomFinger = vomFinger;
        _druckUhr = new DispatcherTimer { Interval = Druckdauer };
        _druckUhr.Tick += DruckAbgelaufen;
        _druckUhr.Start();
    }

    private void DruckBewegt(Point schirm)
    {
        if (_druckUhr != null &&
            (Math.Abs(schirm.X - _druckStart.X) > DruckSpielraum ||
             Math.Abs(schirm.Y - _druckStart.Y) > DruckSpielraum))
            DruckAbbrechen();
    }

    private void DruckAbbrechen()
    {
        if (_druckUhr == null) return;
        _druckUhr.Stop();
        _druckUhr.Tick -= DruckAbgelaufen;
        _druckUhr = null;
    }

    private void DruckAbgelaufen(object? sender, EventArgs e)
    {
        DruckAbbrechen();
        if (!DruckWerkzeugAktiv) return;

        // Die angefangene Handlung verwerfen — der Zeiger lag still, es geht nichts verloren.
        _lassoPts = null;
        _movingSelection = false;
        _scalingSelection = false;
        _rotatingEl = null;
        if (_panning) EndPan();

        if (_druckVomFinger) _finger.Clear();      // der Finger soll nicht weiterschieben
        else _druckEndeSchlucken = true;           // das Loslassen ist kein Eingabe-Ende

        SchnellaktionenZeigen(_druckStart);
    }

    // ==================== Die Knöpfe ====================

    // Jeder schließt zuerst die Leiste: was er tut, ändert die Auswahl, und eine Leiste, die
    // danach mit falschem Zustand stehen bliebe, sähe aus wie ein Fehler.
    private void Sa_Ausschneiden_Click(object? s, RoutedEventArgs e) { SchnellaktionenVerbergen(); Ausschneiden(); Skia.Focus(); }
    private void Sa_Kopieren_Click(object? s, RoutedEventArgs e) { SchnellaktionenVerbergen(); Kopieren(); Skia.Focus(); }
    private void Sa_Duplizieren_Click(object? s, RoutedEventArgs e) { SchnellaktionenVerbergen(); Duplizieren(); Skia.Focus(); }
    private void Sa_Loeschen_Click(object? s, RoutedEventArgs e) { SchnellaktionenVerbergen(); DeleteSelection(); Skia.Focus(); }
    private void Sa_AllesWaehlen_Click(object? s, RoutedEventArgs e) { SchnellaktionenVerbergen(); SelectAll(); Skia.Focus(); }

    /// <summary>
    /// Einfügen — <b>an der Stelle, an der die Leiste aufgemacht wurde</b>, und nicht schräg
    /// versetzt wie bei Strg+V. Der Nutzer hat dort hingezeigt; das ist die Ansage.
    /// </summary>
    private void Sa_Einfuegen_Click(object? s, RoutedEventArgs e)
    {
        SchnellaktionenVerbergen();
        Einfuegen(_saStelle);
        Skia.Focus();
    }

    /// <summary>
    /// Texterkennung. <b>Kein <c>Skia.Focus()</c> hier</b> — anders als bei allen anderen
    /// Knöpfen dieser Leiste: <see cref="TexterkennungLaufen"/> öffnet ein modales Fenster
    /// und setzt den Fokus danach selbst, je nachdem, ob ein Zettel entstanden ist.
    /// </summary>
    private void Sa_Texterkennung_Click(object? s, RoutedEventArgs e)
    {
        SchnellaktionenVerbergen();
        _ = TexterkennungLaufen();
    }
}
