using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Suchen &amp; Ersetzen — <b>neu in Phase 5, Schritt ①c</b> (§4.80). Es fehlte in diesem
/// Kopf ganz.
///
/// <para>
/// <b>Hier steht nur die Bedienung.</b> Gesucht und ersetzt wird in Core
/// (<see cref="TdSuche"/>), gegen das Dokumentmodell — damit läuft es unter Linux, ist ohne
/// ein Fenster prüfbar, und es gibt die Logik nur einmal.
/// </para>
/// <para>
/// <b>⚠ Der WPF-Kopf teilt sie trotzdem nicht, und das ist kein Versehen:</b> Er bearbeitet
/// ein <c>FlowDocument</c> und sucht über <c>TextPointer</c>; sein Weg steht auf einer
/// Windows-Schranke und nicht im falschen Projekt. *Anders als beim Tafel-Export (§4.77) und
/// beim Formen-Stift (§4.78) — man muss nachsehen, statt es anzunehmen.*
/// </para>
/// </summary>
public partial class TextDocView
{
    /// <summary>
    /// Wo die nächste Suche anfängt. <b>Nicht die Auswahl selbst</b>, und das ist am
    /// laufenden Programm zu sehen: Nach „Weiter" steht der Treffer ausgewählt da, und die
    /// nächste Suche muss <b>hinter</b> ihm anfangen — sonst findet sie ihn wieder.
    /// </summary>
    private TdPosition? _suchAb;

    // ==================== Auf und zu ====================

    private void SuchleisteUmschalten(object? sender, RoutedEventArgs e)
    {
        if (KnopfSuchen.IsChecked == true) SuchleisteZeigen();
        else SuchleisteSchliessen();
    }

    /// <summary>
    /// <b>Der ausgewählte Text wandert ins Suchfeld</b> — wer ein Wort markiert und Strg+F
    /// drückt, will danach suchen. Die Schranke bei 60 Zeichen ist dieselbe wie drüben: eine
    /// halbe Seite Auswahl ist kein Suchbegriff.
    /// </summary>
    private void SuchleisteZeigen()
    {
        Suchleiste.IsVisible = true;
        KnopfSuchen.IsChecked = true;
        SuchStatus.Text = "";
        _suchAb = null;

        if (_modell is not null && !_auswahl.IsEmpty)
        {
            string markiert = TdCursor.Text(_modell, _auswahl);
            if (markiert.Length is > 0 and < 60 && !markiert.Contains('\n'))
                SuchFeld.Text = markiert;
        }

        SuchFeld.Focus();
        SuchFeld.SelectAll();
    }

    private void SuchleisteSchliessen_Click(object? sender, RoutedEventArgs e) =>
        SuchleisteSchliessen();

    private void SuchleisteSchliessen()
    {
        Suchleiste.IsVisible = false;
        KnopfSuchen.IsChecked = false;
        _suchAb = null;

        // **Der Fokus muss zurück auf die Leinwand**, sonst tippt der Nutzer nach dem
        // Schließen weiter ins Suchfeld und wundert sich, warum nichts im Dokument steht.
        Skia.Focus();
    }

    /// <summary>
    /// Eingabe sucht weiter, Escape schließt. <b>Beide Felder hängen daran</b> — wer im
    /// Ersetzen-Feld steht und Eingabe drückt, meint dasselbe.
    /// </summary>
    private void SuchFeld_Taste(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { Weitersuchen(); e.Handled = true; }
        else if (e.Key == Key.Escape) { SuchleisteSchliessen(); e.Handled = true; }
    }

    // ==================== Suchen ====================

    private void Weitersuchen_Click(object? sender, RoutedEventArgs e) => Weitersuchen();

    private void Weitersuchen()
    {
        if (_modell is null) return;

        string suche = SuchFeld.Text ?? "";
        if (suche.Length == 0) return;

        var ab = _suchAb ?? _auswahl.End;
        if (TdSuche.Naechster(_modell, suche, ab) is not { } treffer)
        {
            SuchStatus.Text = Loc.T("Ed.Find.NotFound");
            return;
        }

        SuchStatus.Text = "";
        _auswahl = treffer;

        // **Ab dem Trefferende weitersuchen, nicht ab der Auswahl.** Die Auswahl liegt jetzt
        // *auf* dem Treffer; wer von ihrem Anfang aus weitersucht, findet ihn erneut, und
        // „Weiter" stünde still.
        _suchAb = treffer.End;

        MarkeNachziehen();
    }

    // ==================== Ersetzen ====================

    /// <summary>
    /// <b>Ersetzt nur, was gerade wirklich dasteht</b>, und sucht danach weiter — dieselbe
    /// Reihenfolge wie drüben. Steht die Auswahl nicht auf dem Suchbegriff (etwa gleich nach
    /// dem Öffnen der Leiste), wird nichts ersetzt, sondern erst einmal gesucht.
    /// </summary>
    private void Ersetzen_Click(object? sender, RoutedEventArgs e)
    {
        if (_modell is null || _vm is null) return;

        string suche = SuchFeld.Text ?? "";
        if (suche.Length == 0) return;

        if (!_auswahl.IsEmpty &&
            string.Equals(TdCursor.Text(_modell, _auswahl), suche,
                          StringComparison.CurrentCultureIgnoreCase))
        {
            Aendern(TdEdit.Ersetzen(_modell, _auswahl, TdFragment.Text(
                ErsatzFeld.Text ?? "", TdEdit.FormatBei(TdCursor.AbsatzAn(_modell, _auswahl.End.Paragraph)!, _auswahl.End))));

            // Nach dem Ersetzen steht die Marke hinter dem neuen Text — von dort geht es
            // weiter, sonst fände „Weiter" den Ersatz selbst, wenn er den Suchbegriff enthält.
            _suchAb = _auswahl.End;
        }

        Weitersuchen();
    }

    /// <summary>
    /// <b>Alle ersetzen.</b> Die Arbeit macht <see cref="TdSuche.AlleErsetzen"/> in Core; hier
    /// steht nur, was danach zu tun ist: die Änderungen in den Verlauf schieben, das Dokument
    /// als geändert melden und neu umbrechen.
    ///
    /// <para>
    /// <b>⚠ Die Änderungen sind in Core schon angewandt</b> (dort steht, warum) — hier wird
    /// deshalb <c>Push</c> ohne <c>Anwenden</c> gerufen, und <b>nicht</b>
    /// <see cref="Aendern"/>, das beides täte.
    /// </para>
    /// </summary>
    private void AlleErsetzen_Click(object? sender, RoutedEventArgs e)
    {
        if (_modell is null || _vm is null) return;

        string suche = SuchFeld.Text ?? "";
        if (suche.Length == 0) return;

        var aenderungen = TdSuche.AlleErsetzen(_modell, suche, ErsatzFeld.Text ?? "");

        SuchStatus.Text = Loc.T("Ed.Find.Replaced", aenderungen.Count);
        if (aenderungen.Count == 0) return;

        foreach (var aenderung in aenderungen) _vm.Undo.Push(aenderung);
        _vm.Undo.Abschliessen();
        _vm.IsDirty = true;

        // Die Auswahl von vorher zeigt womöglich in Text, den es nicht mehr gibt.
        _auswahl = new TdSelection(TdCursor.Anfang(_modell));
        _suchAb = null;

        UmbruchAnstossen();
    }
}
