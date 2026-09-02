using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using GonkNote.Core.Text;
using GonkNote.Core.Theming;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// <b>Der Tabellenentwurf</b> — Rahmen, Füllung, Kopfzeile, Zellabstand, Spaltenbreite,
/// verbinden und teilen (HANDOFF §4.90).
///
/// <para>
/// <b>Sie rechnet nichts.</b> Was ein Handgriff an der Tabelle ändert, weiß
/// <see cref="TdTableEdit"/>; hier steht die Übersetzung von Klicks in diese Aufrufe und das
/// Nachziehen der Bedienung.
/// </para>
/// <para>
/// <b>Die Zellfarben stehen in Core</b> (<see cref="TdTextfarben.Hervorhebung"/>) und nicht
/// hier — dieselbe Tabelle, aus der die Texthervorhebung ihre Kacheln nimmt (§4.40). Eine
/// zweite Farbliste wäre die erste, die von der anderen abweicht.
/// </para>
/// </summary>
public partial class TextDocView
{
    /// <summary>
    /// Sperrt das Zurückschreiben, während die Entwurfsfelder gefüllt werden — <b>ohne sie
    /// setzte jedes Nachziehen den Wert, den es gerade anzeigt</b> (dieselbe Vorsorge wie in
    /// <c>TextDocView.Seite.cs</c>).
    /// </summary>
    private bool _fuelltEntwurf;

    // ==================== Verbinden und teilen ====================

    private void ZellenVerbinden_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        Aendern(TdTableEdit.ZellenVerbinden(_modell!, _auswahl));
        ReiterNachziehen();
        Skia.Focus();
    }

    private void ZelleTeilen_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        Aendern(TdTableEdit.ZelleTeilen(_modell!, _auswahl));
        ReiterNachziehen();
        Skia.Focus();
    }

    // ==================== Rahmen ====================

    private void Rahmen_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar || s is not Control knopf) return;

        var wahl = (string?)knopf.Tag switch
        {
            "Aussen" => TdTableEdit.Rahmenwahl.Aussen,
            "Innen" => TdTableEdit.Rahmenwahl.Innen,
            "Keine" => TdTableEdit.Rahmenwahl.Keine,
            _ => TdTableEdit.Rahmenwahl.Alle,
        };

        Aendern(TdTableEdit.Rahmen(
            _modell!, _auswahl, wahl, RahmenstaerkePt(),

            // **Aus der Papier-Variante der Tabelle und nicht aus dem laufenden Theme**
            // (§4.89): Die Rahmenfarbe geht ins Dokument und damit in PDF und DOCX. Ein
            // Dokument ist Papier (§1) — im dunklen Modus stünde sonst ein heller Rahmen in
            // der Datei, den im Export niemand sieht.
            Themes.Light[ThemeColor.Text].ToString()));

        ReiterNachziehen();
        Skia.Focus();
    }

    private double RahmenstaerkePt() =>
        RahmenStaerke.SelectedItem is ComboBoxItem eintrag &&
        double.TryParse((string?)eintrag.Tag, System.Globalization.CultureInfo.InvariantCulture,
                        out double pt)
            ? pt
            : 0.5;

    // ==================== Füllung ====================

    /// <summary>
    /// Baut die Kacheln für die Zellfüllung — <b>aus derselben Tabelle wie die
    /// Texthervorhebung</b> (§4.40). Gerufen aus <c>ListenAufbauen</c>, also auch bei jedem
    /// Sprachwechsel: Die Namen der Farben kommen aus <see cref="Loc.T"/>.
    /// </summary>
    private void ZellfarbenAufbauen()
    {
        ZellfarbenFeld.Children.Clear();

        foreach (var farbe in TdTextfarben.Hervorhebung)
        {
            var kachel = new Button
            {
                Width = 26,
                Height = 26,
                Margin = new Avalonia.Thickness(1),
                Padding = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(5),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Background = farbe.Hex is { } hex
                    ? new SolidColorBrush(Color.Parse(hex))
                    : Brushes.Transparent,
            };

            ToolTip.SetTip(kachel, Loc.T(farbe.Key));

            // Die Kachel ohne Farbe bekommt ein Kreuz — eine leere Fläche wäre von „Weiß"
            // nicht zu unterscheiden, und der Unterschied ist ihr ganzer Zweck (§4.40).
            if (farbe.Hex is null)
            {
                kachel.Content = new TextBlock
                {
                    Text = "✕",
                    FontSize = 13,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                };
            }

            var wert = farbe.Hex;
            kachel.Click += (_, _) => Zellfarbe(wert);

            ZellfarbenFeld.Children.Add(kachel);
        }
    }

    private void Zellfarbe(string? hex)
    {
        if (!Schreibbar) return;

        Aendern(TdTableEdit.Fuellung(_modell!, _auswahl, hex));
        Skia.Focus();
    }

    // ==================== Kopfzeile, Abstand, Breite ====================

    private void Kopfzeile_Geaendert(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar || _fuelltEntwurf) return;

        Aendern(TdTableEdit.Kopfzeile(
            _modell!, _auswahl, SchalterKopfzeile.IsChecked == true));
    }

    private void Zellabstand_Geaendert(object? s, NumericUpDownValueChangedEventArgs e)
    {
        if (!Schreibbar || _fuelltEntwurf || e.NewValue is not { } wert) return;

        Aendern(TdTableEdit.Zellabstand(_modell!, _auswahl, (double)wert));
    }

    private void Spaltenbreite_Geaendert(object? s, NumericUpDownValueChangedEventArgs e)
    {
        if (!Schreibbar || _fuelltEntwurf || e.NewValue is not { } wert) return;

        Aendern(TdTableEdit.Spaltenbreite(_modell!, _auswahl, (double)wert));
    }

    /// <summary>
    /// <b>AutoAnpassen ist das Weglassen einer Zahl</b> und kein eigener Rechenweg (§4.90):
    /// Ohne Angabe teilt <see cref="TdTable.Spaltenbreiten"/> den Platz an der Breite auf, die
    /// beim Umbruch wirklich zur Verfügung steht.
    /// </summary>
    private void AutoAnpassen_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        Aendern(TdTableEdit.Spaltenbreite(_modell!, _auswahl, null));
        ReiterNachziehen();
        Skia.Focus();
    }

    // ==================== Nachziehen ====================

    /// <summary>
    /// Stellt die Entwurfsbedienung auf die Tabelle unter der Marke. <b>Gerufen aus
    /// <c>ReiterNachziehen</c></b>, also nach jedem Klick und jeder Pfeiltaste.
    ///
    /// <para>
    /// <b>„Teilen" ist aus, wo nichts zu teilen ist</b>, und „Verbinden" dort, wo es keine
    /// rechte Nachbarin gibt — ein Knopf, der aussieht, als täte er etwas, und nichts tut, ist
    /// der Fehler aus §4.78.
    /// </para>
    /// </summary>
    private void EntwurfNachziehen(TdTable? tabelle, int zeile, int spalte)
    {
        if (SchalterKopfzeile is null) return;

        _fuelltEntwurf = true;
        try
        {
            bool an = tabelle is not null;
            KnopfEntwurf.IsEnabled = an;
            KnopfVerbinden.IsEnabled = an && tabelle!.Rows.Count > zeile &&
                                       spalte + 1 < tabelle.Rows[zeile].Cells.Count;
            KnopfTeilen.IsEnabled = an && tabelle!.Rows.Count > zeile &&
                                    spalte < tabelle.Rows[zeile].Cells.Count &&
                                    tabelle.Rows[zeile].Cells[spalte].ColumnSpan > 1;

            if (tabelle is null) return;

            SchalterKopfzeile.IsChecked = tabelle.Rows.Count > 0 && tabelle.Rows[0].IsHeader;
            Zellabstand.Value = (decimal)Math.Round(tabelle.Format.CellPaddingLeftCm, 2);

            // **Nur eine wirklich gesetzte Breite wird angezeigt.** Stünde hier der geteilte
            // Wert, sähe „automatisch" aus wie „festgenagelt", und der nächste Klick machte es
            // dazu.
            Spaltenbreite.Value = spalte >= 0 && spalte < tabelle.ColumnWidthsCm.Count
                ? (decimal)Math.Round(tabelle.ColumnWidthsCm[spalte], 2)
                : null;
        }
        finally
        {
            _fuelltEntwurf = false;
        }
    }
}
