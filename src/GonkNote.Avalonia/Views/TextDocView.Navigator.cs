using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Der Überschriften-Navigator — das Gegenstück zu <c>NavPanel</c> im WPF-Kopf (HANDOFF §4.85).
///
/// <para>
/// <b>Die Liste kommt aus Core und wird hier nicht gesammelt.</b>
/// <see cref="TdToc.Eintraege"/> rechnet sie für das Inhaltsverzeichnis ohnehin — Text, Ebene,
/// Seite und der Absatz, aus dem sie stammt. Eine zweite Sammelschleife im Kopf wäre eine
/// zweite Antwort auf „was ist hier eine Überschrift", und die beiden gingen irgendwann
/// auseinander. <b>Der WPF-Kopf hat genau diese zweite Antwort</b>
/// (<c>TextStyles.CollectHeadings</c> über das <c>FlowDocument</c>) — er kann nicht anders,
/// weil sein Editor kein Modell kennt.
/// </para>
/// <para>
/// <b>Und die Ebene kommt aus <c>OutlineLevel</c> und nicht aus der Schriftgröße</b> (§4.20):
/// Ein groß gesetzter Absatz ist keine Überschrift, und eine Überschrift, die jemand klein
/// gestellt hat, bleibt eine.
/// </para>
/// </summary>
public partial class TextDocView
{
    private void NavigatorUmschalten(object? sender, RoutedEventArgs e)
    {
        if (Navigator is null) return;

        Navigator.IsVisible = KnopfNavigator.IsChecked == true;
        if (Navigator.IsVisible) NavigatorNachziehen();
    }

    /// <summary>
    /// Baut die Liste neu — <b>nur wenn sie überhaupt zu sehen ist</b>.
    /// <para>
    /// Sie hängt am Umbruch und nicht am Tastendruck: Wer tippt, verschiebt Seitenzahlen, und
    /// eine Liste, die bei jedem Zeichen neu entsteht, kostet genau dort Zeit, wo es auffällt.
    /// </para>
    /// </summary>
    private void NavigatorNachziehen()
    {
        if (Navigator is not { IsVisible: true } || _modell is null) return;

        NavigatorListe.Children.Clear();

        var eintraege = TdToc.Eintraege(_modell, _umbruch);

        if (eintraege.Count == 0)
        {
            // **Ein Satz und keine leere Fläche.** „Leer" ist von „kaputt" nicht zu
            // unterscheiden — dieselbe Regel wie beim nicht übernommenen Dokument (§4.22).
            NavigatorListe.Children.Add(new TextBlock
            {
                Text = Loc.T("Ed.Navigator.Empty"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(6, 4, 6, 4),
                FontSize = 12,
                Foreground = this.FindResource("Brush.TextMuted") as IBrush,
            });
            return;
        }

        foreach (var eintrag in eintraege)
        {
            var absatz = eintrag.Source;

            var knopf = new Button
            {
                Content = new TextBlock
                {
                    Text = eintrag.Text,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontSize = eintrag.Level == 1 ? 13 : 12,
                    FontWeight = eintrag.Level == 1 ? FontWeight.SemiBold : FontWeight.Normal,
                },
                // Die Einrückung zeigt die Ebene — dasselbe Maß wie drüben (12 je Stufe).
                Margin = new Thickness((eintrag.Level - 1) * 12, 0, 0, 0),
                Padding = new Thickness(6, 3, 4, 3),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                BorderThickness = default,
                Cursor = new Cursor(StandardCursorType.Hand),
                [ToolTip.TipProperty] = eintrag.Text,
            };

            knopf.Click += (_, _) => Springen(absatz);
            NavigatorListe.Children.Add(knopf);
        }
    }

    /// <summary>
    /// Zum Anfang einer Überschrift springen.
    /// <para>
    /// <b>Gesetzt wird die Marke, gerollt wird von selbst:</b> <c>MarkeNachziehen</c> tut nach
    /// einem Klick genau dasselbe wie nach einem Umbruch (§4.34) — wer hier ein eigenes
    /// Scrollen schriebe, hätte zwei Listen von Handgriffen, und eine davon bekäme irgendwann
    /// einen Punkt weniger.
    /// </para>
    /// <para>
    /// <b>Und der Fokus geht auf die Leinwand</b>, nicht auf den Knopf: Wer eine Überschrift
    /// anspringt, will dort weiterschreiben — bliebe der Fokus in der Liste, ginge der nächste
    /// Tastendruck ins Leere. Das ist der Fund aus §4.79 an anderer Stelle.
    /// </para>
    /// </summary>
    private void Springen(TdParagraph absatz)
    {
        if (_modell is null) return;

        int index = TdCursor.IndexVon(_modell, absatz);
        if (index < 0) return;

        _auswahl = new TdSelection(new TdPosition(index, 0, 0));
        Skia.Focus();
        MarkeVersetzt();
    }
}
