using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GonkNote.Core.Text;

namespace GonkNote.Views;

/// <summary>
/// Die Reiter <b>Einfügen</b>, <b>Verweise</b> und <b>Tabelle</b> — Schritt 6, zweite Hälfte
/// (HANDOFF §6, §4.37).
///
/// <para>
/// <b>Sie rechnet nichts</b>, wie die beiden Geschwisterdateien: Was eingefügt wird, baut
/// <see cref="TdBlockEdit"/>; was ein Verweis ist, <see cref="TdFormatEdit.Verweis"/>; was eine
/// Tabellenänderung tut, <see cref="TdTableEdit"/>. Alles läuft über <c>Aendern</c> — denselben
/// Weg wie jeder Tastendruck, und damit liegt jeder dieser Handgriffe im Verlauf, ohne dass hier
/// ein Wort davon steht.
/// </para>
/// <para>
/// <b>Warum die Reiter erst jetzt da sind</b>, und nicht mit §4.28: Sie hätten leer dagestanden.
/// Jeder Knopf, der jetzt darin steht, ändert wirklich etwas — und was noch fehlt (Bilder,
/// Zellen verbinden, Beschriftungen), steht **nicht** als ausgegrauter Knopf da, sondern im
/// HANDOFF (§4.28, „ein halbes Feature ist schlechter als ein fehlendes").
/// </para>
/// </summary>
public partial class TextDocView
{
    // ==================== Einfügen ====================

    private void Seitenumbruch_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;
        Aendern(TdBlockEdit.Seitenumbruch(_modell!, _auswahl));
    }

    /// <summary>
    /// Derselbe Handgriff wie Umschalt+Eingabe — <b>und derselbe Aufruf</b>. Zwei Wege zum
    /// selben Ergebnis sind einer zu viel; der zweite ist immer der, den niemand prüft (§4.32).
    /// </summary>
    private void Zeilenumbruch_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;
        Aendern(TdEdit.Zeilenumbruch(_modell!, _auswahl));
    }

    private void TabelleEinfuegen_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        int zeilen = (int)(TabelleZeilen.Value ?? 2);
        int spalten = (int)(TabelleSpalten.Value ?? 2);

        Aendern(TdBlockEdit.Tabelle(_modell!, _auswahl, zeilen, spalten));
    }

    /// <summary>
    /// Ein Feld einfügen. <b>Es ist ein Stück und kein Block</b> (§4.20) — deshalb läuft es über
    /// denselben <see cref="TdEdit.Ersetzen"/> wie getippter Text und nicht über
    /// <see cref="TdBlockEdit"/>.
    /// </summary>
    private void Feld_Click(object? sender, RoutedEventArgs e)
    {
        if (!Schreibbar || sender is not MenuItem { Tag: string name }) return;
        if (!Enum.TryParse<TdFieldKind>(name, out var art)) return;

        Aendern(TdEdit.Ersetzen(_modell!, _auswahl, TdFragment.Stuecke(new TdField(art))));
    }

    // ==================== Verweise ====================

    private void VerweisSetzen_Click(object? s, RoutedEventArgs e) => VerweisSetzen();

    /// <summary>Eingabe im Zielfeld setzt den Verweis — sonst müsste man zum Knopf greifen.</summary>
    private void VerweisZiel_Taste(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return)) return;

        VerweisSetzen();
        e.Handled = true;
    }

    /// <summary>
    /// Setzt den Verweis auf das, was im Feld steht.
    ///
    /// <para>
    /// <b>Ein leeres Feld nimmt ihn heraus</b>, statt einen Verweis ins Nichts zu legen — das
    /// ist dieselbe Antwort wie „Link entfernen", und zwei Knöpfe, die dasselbe tun, sind
    /// keiner zu viel: Der eine ist der, den man sucht, der andere der, den man findet.
    /// </para>
    /// </summary>
    private void VerweisSetzen()
    {
        if (!Schreibbar) return;

        string ziel = VerweisZiel.Text?.Trim() ?? "";
        Aendern(TdFormatEdit.Verweis(_modell!, _auswahl, ziel.Length == 0 ? null : ziel));

        RibbonNachziehen();
    }

    private void VerweisEntfernen_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        Aendern(TdFormatEdit.Verweis(_modell!, _auswahl, null));
        RibbonNachziehen();
    }

    /// <summary>
    /// Das Inhaltsverzeichnis ist ein <b>Feld</b> und keine gerechnete Liste (§4.20): Es steht
    /// als eine Stelle im Text, und was darin steht, entsteht bei jedem Umbruch neu.
    ///
    /// <para>
    /// <b>Es bekommt trotzdem einen eigenen Absatz</b> und wird deshalb als Block eingefügt und
    /// nicht als Stück: Mitten in einem Satz wäre es zwar möglich, aber nie gemeint — und der
    /// Umbruch setzte dann ein ganzes Verzeichnis zwischen zwei Wörter. Über
    /// <see cref="TdBlockEdit"/> ist das **eine** Änderung und damit **ein** Schritt im Verlauf;
    /// erst teilen und dann einfügen wären zwei, und ein Strg+Z ließe den halben Handgriff
    /// stehen.
    /// </para>
    /// </summary>
    private void Inhaltsverzeichnis_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        Aendern(TdBlockEdit.Einfuegen(
            _modell!, _auswahl,
            new TdParagraph([new TdField(TdFieldKind.TableOfContents)])));
    }

    // ==================== Tabelle ====================

    private void ZeileDarueber_Click(object? s, RoutedEventArgs e) => Zeile(darunter: false);
    private void ZeileDarunter_Click(object? s, RoutedEventArgs e) => Zeile(darunter: true);
    private void SpalteLinks_Click(object? s, RoutedEventArgs e) => Spalte(rechts: false);
    private void SpalteRechts_Click(object? s, RoutedEventArgs e) => Spalte(rechts: true);

    private void Zeile(bool darunter)
    {
        if (!Schreibbar) return;
        Aendern(TdTableEdit.ZeileEinfuegen(_modell!, _auswahl, darunter));
        RibbonNachziehen();
    }

    private void Spalte(bool rechts)
    {
        if (!Schreibbar) return;
        Aendern(TdTableEdit.SpalteEinfuegen(_modell!, _auswahl, rechts));
        RibbonNachziehen();
    }

    private void ZeileWeg_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;
        Aendern(TdTableEdit.ZeileLoeschen(_modell!, _auswahl));
        RibbonNachziehen();
    }

    private void SpalteWeg_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;
        Aendern(TdTableEdit.SpalteLoeschen(_modell!, _auswahl));
        RibbonNachziehen();
    }

    private void TabelleWeg_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;
        Aendern(TdTableEdit.TabelleLoeschen(_modell!, _auswahl));
        RibbonNachziehen();
    }

    // ==================== Nachziehen ====================

    /// <summary>
    /// Was an den beiden Reitern hängt, die von der Stelle der Marke abhängen — gerufen aus
    /// <see cref="RibbonNachziehen"/> und damit aus demselben einen Trichter wie alles andere.
    /// </summary>
    private void ReiterNachziehen()
    {
        if (LeisteTabelle is null) return;

        bool schreibbar = Schreibbar;

        // ---- Verweise ----
        // **Das Feld wird nur nachgezogen, wenn der Nutzer nicht gerade darin schreibt.**
        // Sonst überschriebe jeder Tastendruck im Dokument die halb getippte Adresse — und beim
        // Tippen *im Feld* bewegt sich die Marke im Dokument ohnehin nicht.
        if (schreibbar && !VerweisZiel.IsFocused)
            VerweisZiel.Text = TdFormatEdit.VerweisZiel(_modell!, _auswahl) ?? "";

        KnopfVerweisSetzen.IsEnabled = schreibbar && !_auswahl.IsEmpty;
        KnopfVerweisWeg.IsEnabled = schreibbar && !_auswahl.IsEmpty;

        // ---- Tabelle ----
        var ort = schreibbar ? TdTableEdit.Ort(_modell!, _auswahl.Focus) : null;

        TabelleWerkzeuge.IsVisible = ort is not null;
        TabelleHinweis.IsVisible = ort is null;

        if (ort is { } drin)
        {
            // Die letzte Zeile und die letzte Spalte lassen sich nicht löschen (§4.37) — und
            // ein Knopf, der nichts tut, soll nicht so aussehen, als täte er etwas.
            KnopfZeileWeg.IsEnabled = drin.Tabelle.Rows.Count > 1;
            KnopfSpalteWeg.IsEnabled = drin.Tabelle.Spaltenzahl() > 1;
        }
    }
}
