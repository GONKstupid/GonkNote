using Avalonia.Controls;
using Avalonia.Interactivity;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// <b>Aufzählung, Nummerierung, Absatzvorlagen und die Schriftgrößenliste</b> — Gruppe A aus §6
/// (HANDOFF §4.39).
///
/// <para>
/// <b>Sie rechnet nichts</b>, wie die Geschwisterdateien: <see cref="TdListEdit"/> weiß, was
/// eine Liste oder eine Vorlage am Absatz ändert, und <see cref="TdStil.Alle"/> ist die
/// Tabelle. Hier steht die Übersetzung von Klicks in diese Aufrufe — und das eine, was nur hier
/// geht: die Listen **aus der Tabelle** aufbauen, statt sie in der XAML zu wiederholen.
/// </para>
/// <para>
/// <b>Beide Listen entstehen im Code und tragen ihre Beschriftung aus <see cref="Loc.T"/>.</b>
/// Bei den Vorlagen ist das nötig, weil die Tabelle in Core steht; bei den Größen wäre es nicht
/// nötig, aber neunzehn Einträge von Hand in die XAML zu schreiben hieße, die Leiter aus
/// <c>TextDocView.Format.cs</c> ein zweites Mal hinzuschreiben (§4.13). **Ein Sprachwechsel
/// zieht die Vorlagennamen deshalb über <c>OnLanguageChanged</c> nach** — anders als bei
/// <c>{loc:T …}</c> tut Avalonia das hier nicht von selbst (§7).
/// </para>
/// </summary>
public partial class TextDocView
{
    /// <summary>
    /// Sperrt das Zurückschreiben, während die Listen gefüllt werden — <b>ohne sie setzte jedes
    /// Nachziehen die Vorlage, die es gerade anzeigt</b> (dieselbe Vorsorge wie in
    /// <c>TextDocView.Seite.cs</c>).
    /// </summary>
    private bool _fuelltListen;

    /// <summary>
    /// Baut die beiden Auswahllisten auf. <b>Gerufen aus dem Konstruktor und bei jedem
    /// Sprachwechsel</b> — die Vorlagennamen kommen aus <see cref="Loc.T"/>, und Avalonia zieht
    /// nur nach, was als <c>{loc:T …}</c> in der XAML steht.
    /// </summary>
    private void ListenAufbauen()
    {
        _fuelltListen = true;
        try
        {
            VorlageWahl.ItemsSource = TdStil.Alle
                .Select(s => new ComboBoxItem { Content = Loc.T(s.Key), Tag = s.Name })
                .ToList();

            GroesseWahl.ItemsSource = Groessen
                .Select(g => new ComboBoxItem { Content = $"{g:0.#}", Tag = g })
                .ToList();
        }
        finally
        {
            _fuelltListen = false;
        }
    }

    // ==================== Listen ====================

    private void Aufzaehlung_Click(object? s, RoutedEventArgs e) => Liste(nummeriert: false);
    private void Nummerierung_Click(object? s, RoutedEventArgs e) => Liste(nummeriert: true);

    private void Liste(bool nummeriert)
    {
        if (!Schreibbar) return;

        Aendern(TdListEdit.Umschalten(_modell!, _auswahl, nummeriert));
        RibbonNachziehen();
    }

    // ==================== Vorlagen ====================

    private void Vorlage_Gewechselt(object? sender, SelectionChangedEventArgs e)
    {
        if (_fuelltListen || !Schreibbar) return;
        if ((sender as ComboBox)?.SelectedItem is not ComboBoxItem { Tag: string name }) return;
        if (TdStil.MitNamen(name) is not { } stil) return;

        Aendern(TdListEdit.Vorlage(_modell!, _auswahl, stil));
        RibbonNachziehen();
    }

    // ==================== Schriftgröße ====================

    private void Groesse_Gewechselt(object? sender, SelectionChangedEventArgs e)
    {
        if (_fuelltListen || !Schreibbar) return;
        if ((sender as ComboBox)?.SelectedItem is not ComboBoxItem { Tag: double punkte }) return;

        Aendern(TdFormatEdit.Zeichen(_modell!, _auswahl,
            (abweichung, _) => abweichung.FontSize = punkte));

        RibbonNachziehen();
    }

    // ==================== Nachziehen ====================

    /// <summary>
    /// Stellt Vorlage, Größe und die zwei Listenschalter auf das, was die Auswahl zeigt —
    /// gerufen aus <see cref="RibbonNachziehen"/>, also aus demselben einen Trichter.
    ///
    /// <para>
    /// <b>Uneinig heißt „nichts gewählt" und nicht „das erste"</b> (§4.36): Eine Liste, die über
    /// einer gemischten Auswahl eine Vorlage nennt, behauptet etwas Falsches.
    /// </para>
    /// </summary>
    private void ListenNachziehen()
    {
        if (VorlageWahl is null) return;

        _fuelltListen = true;
        try
        {
            bool an = Schreibbar;
            VorlageWahl.IsEnabled = an;
            GroesseWahl.IsEnabled = an;
            SchalterPunkte.IsEnabled = an;
            SchalterNummern.IsEnabled = an;

            if (!an)
            {
                VorlageWahl.SelectedItem = null;
                GroesseWahl.SelectedItem = null;
                SchalterPunkte.IsChecked = false;
                SchalterNummern.IsChecked = false;
                return;
            }

            var vorlage = TdListEdit.GemeinsameVorlage(_modell!, _auswahl);
            VorlageWahl.SelectedItem = vorlage is { } gefunden
                ? Eintrag(VorlageWahl, i => (string?)i.Tag == gefunden.Name)
                : null;

            double? groesse = TdFormatEdit.Gemeinsam(_modell!, _auswahl).FontSize;
            GroesseWahl.SelectedItem = groesse is { } pt
                ? Eintrag(GroesseWahl, i => i.Tag is double g && Math.Abs(g - pt) < 0.01)
                : null;

            SchalterPunkte.IsChecked = TdListEdit.Gemeinsam(_modell!, _auswahl, nummeriert: false);
            SchalterNummern.IsChecked = TdListEdit.Gemeinsam(_modell!, _auswahl, nummeriert: true);
        }
        finally
        {
            _fuelltListen = false;
        }
    }

    private static ComboBoxItem? Eintrag(ComboBox liste, Func<ComboBoxItem, bool> passt) =>
        (liste.ItemsSource as IEnumerable<ComboBoxItem>)?.FirstOrDefault(passt);
}
