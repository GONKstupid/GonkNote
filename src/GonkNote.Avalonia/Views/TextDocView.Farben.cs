using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using GonkNote.Core.Text;
using GonkNote.Core.Theming;
using GonkNote.Platform;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// <b>Schriftart, Schriftfarbe und Hervorhebung</b> — Gruppe B aus §6 (HANDOFF §4.40).
///
/// <para>
/// <b>Alle drei sind Zeichenformate</b> und laufen deshalb über
/// <see cref="TdFormatEdit.Zeichen"/>, wie fett und kursiv seit §4.36. Hier steht nur, welche
/// Werte zur Wahl stehen — und die stehen ihrerseits in Core (<see cref="Fonts.Mitgeliefert"/>
/// und <see cref="TdTextfarben"/>), damit beide Köpfe dieselben anbieten.
/// </para>
/// <para>
/// <b>Die Farbkacheln entstehen im Code</b>, weil sie aus einer Tabelle kommen. Damit greift
/// <see cref="Loc.T"/> beim Aufbau zur richtigen Zeit — dieselbe Lösung wie beim Kontextmenü
/// (§4.38) und aus demselben Grund (§7: ein Menü, das der Code baut, hat kein <c>{loc:T …}</c>).
/// </para>
/// </summary>
public partial class TextDocView
{
    /// <summary>
    /// Die zuletzt gewählte Hervorhebungsfarbe. <b>Sie ist Zustand der Ansicht und nicht des
    /// Dokuments</b> — der Knopf soll beim zweiten Klick dasselbe Gelb nehmen, ohne dass jemand
    /// es noch einmal aussucht (so hält es auch der WPF-Kopf).
    /// </summary>
    private string? _letzteHervorhebung = "#FDE047";

    // ==================== Aufbau ====================

    /// <summary>
    /// Füllt Schriftliste und die beiden Farbfelder — <b>einmal beim Erzeugen</b>.
    /// Die Tabellen ändern sich zur Laufzeit nicht; nur die Beschriftungen tun es, und die
    /// stehen in den Kurzhinweisen, die Avalonia selbst nachzieht.
    /// </summary>
    private void FarbenAufbauen()
    {
        foreach (var familie in Fonts.Mitgeliefert)
            SchriftWahl.Items.Add(new ComboBoxItem
            {
                Content = familie.Family,
                Tag = familie.Family,
                // Jeder Eintrag in seiner eigenen Schrift — dieselbe Vorschau wie drüben.
                FontFamily = new FontFamily(familie.Family),
            });

        Kacheln(SchriftfarbenFeld, TdTextfarben.Schrift, Schriftfarbe);
        Kacheln(HervorhebungFeld, TdTextfarben.Hervorhebung, Hervorhebung);
    }

    /// <summary>
    /// Ein Feld aus Farbkacheln. <b>Die Kachel ohne Farbe bekommt ein Kreuz</b> — eine leere
    /// Fläche wäre von „Weiß" nicht zu unterscheiden, und der Unterschied ist der ganze Zweck
    /// des ersten Eintrags (§4.40, <see cref="TdTextfarben"/>).
    /// </summary>
    private static void Kacheln(
        WrapPanel feld, IReadOnlyList<TdFarbwahl> farben, Action<string?> waehlen)
    {
        foreach (var farbe in farben)
        {
            var kachel = new Button
            {
                Width = 26,
                Height = 26,
                Margin = new Avalonia.Thickness(1),
                Padding = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(5),
                BorderThickness = new Avalonia.Thickness(1),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };

            kachel.BorderBrush = Brushes.Transparent;
            ToolTip.SetTip(kachel, Loc.T(farbe.Key));

            if (farbe.Hex is { } hex)
            {
                kachel.Background = new SolidColorBrush(Color.Parse(hex));
            }
            else
            {
                kachel.Background = Brushes.Transparent;
                kachel.Content = new TextBlock
                {
                    Text = "✕",
                    FontSize = 13,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                };
            }

            string? wert = farbe.Hex;
            kachel.Click += (_, _) => waehlen(wert);

            feld.Children.Add(kachel);
        }
    }

    // ==================== Setzen ====================

    private void Schrift_Gewechselt(object? sender, SelectionChangedEventArgs e)
    {
        if (_fuellt || !Schreibbar) return;
        if ((sender as ComboBox)?.SelectedItem is not ComboBoxItem { Tag: string familie }) return;

        Aendern(TdFormatEdit.Zeichen(_modell!, _auswahl,
            (abweichung, _) => abweichung.FontFamily = familie));

        RibbonNachziehen();
    }

    /// <summary>
    /// Setzt die Schriftfarbe. <b><c>null</c> nimmt die Abweichung heraus</b>, statt Schwarz
    /// hineinzuschreiben — sonst überstünde die Farbe einen späteren Wechsel der Dokumentfarbe
    /// (§4.14).
    /// </summary>
    private void Schriftfarbe(string? hex)
    {
        if (!Schreibbar) return;

        Aendern(TdFormatEdit.Zeichen(_modell!, _auswahl,
            (abweichung, _) => abweichung.Color = hex));

        RibbonNachziehen();
    }

    /// <inheritdoc cref="Schriftfarbe"/>
    private void Hervorhebung(string? hex)
    {
        if (!Schreibbar) return;

        // Nur eine echte Farbe wird gemerkt: „keine" ist kein Marker, den man wiederholen will.
        if (hex is not null) _letzteHervorhebung = hex;

        Aendern(TdFormatEdit.Zeichen(_modell!, _auswahl,
            (abweichung, _) => abweichung.Highlight = hex));

        RibbonNachziehen();
    }

    // ==================== Nachziehen ====================

    /// <summary>
    /// Stellt Schriftliste und die beiden Farbbalken auf das, was die Auswahl zeigt — gerufen
    /// aus <see cref="RibbonNachziehen"/>.
    /// </summary>
    private void FarbenNachziehen(TdCharFormat zeichen)
    {
        if (SchriftWahl is null) return;

        bool an = Schreibbar;
        SchriftWahl.IsEnabled = an;
        KnopfSchriftfarbe.IsEnabled = an;
        KnopfHervorhebung.IsEnabled = an;

        // Uneinige Auswahl: keine Zeile gewählt, statt eine der beiden zu behaupten (§4.36).
        SchriftWahl.SelectedItem = zeichen.FontFamily is { } familie
            ? SchriftWahl.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => (string?)i.Tag == familie)
            : null;

        // Der Balken unter dem „A" zeigt, was ein Klick auf die Kachel gerade ergäbe.
        SchriftfarbeBalken.Background = Pinsel(zeichen.Color) ?? Vordergrund();

        HervorhebungBalken.Background =
            Pinsel(zeichen.Highlight) ?? Pinsel(_letzteHervorhebung) ?? Brushes.Transparent;
    }

    /// <summary>
    /// Ein Pinsel aus einer Farbangabe — <c>null</c>, wenn keine dasteht.
    ///
    /// <para>
    /// <b>„Keine Farbe" heißt im Modell zweierlei, und das hat hier einen Absturz gekostet</b>
    /// (§4.40): <c>null</c> ist „nichts dazu gesagt", der **leere String** ist „ausdrücklich
    /// keine" — <see cref="TdCharFormat.Standard"/> setzt <c>Highlight = ""</c>, und ein
    /// aufgelöstes Format trägt das durch. <c>Color.Parse("")</c> wirft. Wer nur gegen
    /// <c>null</c> prüft, hat den halben Fall geprüft.
    /// </para>
    /// </summary>
    private static IBrush? Pinsel(string? hex) =>
        string.IsNullOrWhiteSpace(hex) ? null : new SolidColorBrush(Color.Parse(hex));

    /// <summary>
    /// Die Schriftfarbe des Erscheinungsbilds — für den Balken unter dem „A", wenn die Auswahl
    /// gar keine Farbe gesetzt hat. <b>Aus der Farbtabelle in Core</b> (§4.9) und nicht fest:
    /// Sie ist die Farbe der *Oberfläche*, nicht die des Papiers, und wechselt mit Hell/Dunkel.
    /// </summary>
    private static IBrush Vordergrund()
    {
        var farbe = AvaloniaThemeHost.Current[ThemeColor.Text];
        return new SolidColorBrush(Color.FromArgb(farbe.A, farbe.R, farbe.G, farbe.B));
    }
}
