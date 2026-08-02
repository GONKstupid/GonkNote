using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GonkNote.Core.Platform;
using GonkNote.Core.Theming;
using GonkNote.Platform;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Der Ersatz für <c>MessageBox.Show</c>. Bewusst klein: eine Zeile Text, ein farbiger
/// Punkt für die Schwere, ein oder zwei Knöpfe.
/// </summary>
public partial class MessageWindow : Window
{
    private bool _antwort;

    // InitializeComponent und nicht AvaloniaXamlLoader.Load — sonst bleiben die
    // x:Name-Felder null (HANDOFF §7).
    public MessageWindow() => InitializeComponent();

    /// <summary>
    /// Zeigt die Meldung und wartet auf die Antwort. <paramref name="frage"/> entscheidet
    /// über zwei Knöpfe (Ja/Nein) oder einen (OK); der Rückgabewert ist bei einer
    /// Mitteilung immer <c>true</c>.
    /// </summary>
    public static bool Zeige(Window? besitzer, string nachricht, DialogSeverity schwere, bool frage)
    {
        var fenster = new MessageWindow();
        fenster.Nachricht.Text = nachricht;
        fenster.SchwereZeichen.Fill = Farbe(schwere).ToBrush();

        fenster.JaKnopf.Content = frage ? Loc.T("Dlg.Yes") : Loc.T("Dlg.Ok");
        fenster.NeinKnopf.Content = Loc.T("Dlg.No");
        fenster.NeinKnopf.IsVisible = frage;
        // Ohne zweiten Knopf muss Escape trotzdem schließen — sonst hängt eine Mitteilung
        // an der Maus fest.
        fenster.JaKnopf.IsCancel = !frage;

        if (besitzer == null)
        {
            // Beim Start und beim Beenden gibt es kurzzeitig kein Fenster. Dann eben
            // freistehend — eine Meldung, die niemand sieht, ist schlimmer als eine, die
            // an der falschen Stelle steht.
            fenster.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            fenster.ShowInTaskbar = true;
            fenster.Show();
            Modal.Warte(WartenAufSchliessen(fenster));
        }
        else
        {
            Modal.Warte(fenster.ShowDialog(besitzer));
        }

        return fenster._antwort;
    }

    private static Task WartenAufSchliessen(Window fenster)
    {
        var fertig = new TaskCompletionSource();
        fenster.Closed += (_, _) => fertig.TrySetResult();
        return fertig.Task;
    }

    private static HexColor Farbe(DialogSeverity schwere) => AvaloniaThemeHost.Current[schwere switch
    {
        DialogSeverity.Warning => ThemeColor.Pink,
        DialogSeverity.Question => ThemeColor.Turquoise,
        _ => ThemeColor.Accent,
    }];

    private void Ja_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _antwort = true;
        Close();
    }

    private void Nein_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _antwort = false;
        Close();
    }
}
