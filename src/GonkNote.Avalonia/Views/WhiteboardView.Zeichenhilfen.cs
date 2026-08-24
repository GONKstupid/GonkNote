using Avalonia.Controls;
using Avalonia.Interactivity;
using GonkNote.Core.Editing;
using GonkNote.Core.Platform;
using GonkNote.Core.Theming;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Lineal und Geodreieck — das Gegenstück zu <c>WhiteboardView.Aids.cs</c> im WPF-Kopf,
/// neu in Phase 4.5 (§4.60).
///
/// <para>
/// <b>Hier steht nur die Bedienung.</b> Umriss, Treffer, Einrasten und Winkelraster liegen
/// seit §4.59 in <see cref="WbZeichenhilfe"/>, die Zeichnung in <see cref="WbAidRenderer"/> —
/// beides Core. Ein Lineal, an dem ein Strich je nach Kopf ein paar Pixel anders einrastet,
/// fiele niemandem auf, bis jemand dieselbe Zeichnung auf beiden Rechnern anlegt.
/// </para>
/// <para>
/// <b>Die Hilfen werden nicht gespeichert.</b> Sie liegen über der Seite wie ein echtes
/// Lineal auf dem Papier — was bleibt, ist der Strich, den man daran gezogen hat, nicht das
/// Werkzeug. Der WPF-Kopf hält es genauso.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    private Zeichenhilfe _hilfe = Zeichenhilfe.Keine;

    private bool _hilfeGesetzt;
    private SKPoint _hilfeMitte;
    private float _hilfeWinkel;

    /// <summary>Was gerade mit der Hilfe geschieht: nichts, verschieben oder drehen.</summary>
    private enum HilfeZug { Keiner, Verschieben, Drehen }

    private HilfeZug _hilfeZug;
    private SKPoint _hilfeZugLetzter;

    /// <summary>
    /// Die Kante, an der ein gerade gezogener Strich klebt — <c>null</c> heißt „frei".
    /// Wird beim Aufsetzen bestimmt und beim Loslassen wieder gelöscht.
    /// </summary>
    private WbZeichenhilfe.Einrastkante? _hilfeKante;

    // ==================== Ein- und ausschalten ====================
    //
    // **Beide Knöpfe stehen immer sichtbar da.** Der WPF-Kopf klappt sie zu einer Gruppe
    // zusammen und zeigt eingeklappt nur die zuletzt benutzte — das ist dort nötig, weil
    // seine Leiste voller ist. Hier wäre es eine Mechanik ohne Anlass.

    private void Lineal_Click(object? sender, RoutedEventArgs e) => HilfeSetzen(Zeichenhilfe.Lineal);
    private void Geodreieck_Click(object? sender, RoutedEventArgs e) => HilfeSetzen(Zeichenhilfe.Geodreieck);

    /// <summary>
    /// Schaltet eine Hilfe ein — <b>oder bei erneutem Klick wieder aus</b>. Beide schließen
    /// sich aus: zwei Lineale übereinander wären zwei Kanten, an denen ein Strich kleben
    /// könnte, und keine davon die gemeinte.
    /// </summary>
    private void HilfeSetzen(Zeichenhilfe art)
    {
        _hilfe = _hilfe == art ? Zeichenhilfe.Keine : art;

        _suppressToolEvents = true;
        BtnLineal.IsChecked = _hilfe == Zeichenhilfe.Lineal;
        BtnGeodreieck.IsChecked = _hilfe == Zeichenhilfe.Geodreieck;
        _suppressToolEvents = false;

        // Beim ersten Einschalten in die Mitte der Sicht legen — dort sucht sie niemand
        // vergeblich. Danach bleibt sie liegen, wo der Nutzer sie hingeschoben hat.
        if (_hilfe != Zeichenhilfe.Keine && !_hilfeGesetzt)
        {
            _hilfeMitte = Sichtmitte();
            _hilfeWinkel = 0f;
            _hilfeGesetzt = true;
        }

        // Der Fokus gehört zurück auf die Fläche, sonst kommt danach keine Taste mehr an
        // (§4.56, am laufenden Programm gemessen).
        Skia.Focus();
        Neuzeichnen();
    }

    // ==================== Der Zug über die Fläche ====================

    /// <summary>
    /// Fängt einen Zeigerdruck ab, wenn er den Dreh-Griff oder den Körper trifft.
    /// <b>Liefert <c>true</c>, dann ist der Zug vergeben</b> und das Werkzeug kommt nicht
    /// mehr zum Zug — sonst zeichnete jeder Griff ans Lineal auch einen Strich.
    /// </summary>
    private bool HilfeZugBeginnen(SKPoint c)
    {
        if (_hilfe == Zeichenhilfe.Keine) return false;

        // Erst der Griff, dann der Körper: der Griff hängt außen und ragt nicht hinein, aber
        // wer zuerst auf „im Körper" prüft, verschluckt ihn bei überlappenden Fangkreisen.
        if (WbZeichenhilfe.TrifftGriff(_hilfe, _hilfeMitte, _hilfeWinkel, Zoom, c))
        {
            _hilfeZug = HilfeZug.Drehen;
            Neuzeichnen();
            return true;
        }
        if (WbZeichenhilfe.TrifftKoerper(_hilfe, _hilfeMitte, _hilfeWinkel, c))
        {
            _hilfeZug = HilfeZug.Verschieben;
            _hilfeZugLetzter = c;
            Neuzeichnen();
            return true;
        }
        return false;
    }

    private void HilfeZugFortsetzen(SKPoint c)
    {
        if (_hilfeZug == HilfeZug.Verschieben)
        {
            _hilfeMitte = new SKPoint(_hilfeMitte.X + (c.X - _hilfeZugLetzter.X),
                                      _hilfeMitte.Y + (c.Y - _hilfeZugLetzter.Y));
            _hilfeZugLetzter = c;
        }
        else if (_hilfeZug == HilfeZug.Drehen)
        {
            float roh = MathF.Atan2(c.Y - _hilfeMitte.Y, c.X - _hilfeMitte.X) * 180f / MathF.PI;
            _hilfeWinkel = WbZeichenhilfe.WinkelFangen(roh);
        }
        Neuzeichnen();
    }

    /// <summary>Beendet einen Hilfe-Zug; <c>true</c>, wenn einer lief.</summary>
    private bool HilfeZugBeenden()
    {
        if (_hilfeZug == HilfeZug.Keiner) return false;
        _hilfeZug = HilfeZug.Keiner;
        Neuzeichnen();
        return true;
    }

    // ==================== Einrasten eines Strichs ====================

    /// <summary>
    /// Prüft beim Aufsetzen, ob der Strich an einer Kante kleben soll. <b>Nur hier</b> — wer
    /// bei jeder Bewegung neu suchte, dessen Strich spränge mitten im Zug auf eine andere
    /// Kante, sobald er ihr näher kommt.
    /// </summary>
    private void HilfeEinrastenPruefen(SKPoint c) =>
        _hilfeKante = WbZeichenhilfe.Einrasten(_hilfe, _hilfeMitte, _hilfeWinkel, c);

    /// <summary>Zieht einen Punkt auf die eingerastete Kante — oder lässt ihn, wo er ist.</summary>
    private SKPoint HilfeEinrasten(SKPoint p) =>
        _hilfeKante is { } kante ? WbZeichenhilfe.AufKante(kante, p) : p;

    // ==================== Zeichnen ====================

    /// <summary>
    /// Die aktive Hilfe, <b>zuletzt gezeichnet</b> — sie liegt über allem, wie ein echtes
    /// Lineal auf dem Blatt.
    /// </summary>
    private void HilfeZeichnen(SKCanvas leinwand)
    {
        if (_hilfe == Zeichenhilfe.Keine) return;

        WbAidRenderer.DrawAid(leinwand, _hilfe, _hilfeMitte, _hilfeWinkel, Zoom,
                              App.Platform.Theme.Current == AppTheme.Dark,
                              ThemeSk(ThemeColor.Accent),
                              _hilfeZug == HilfeZug.Drehen);
    }
}
