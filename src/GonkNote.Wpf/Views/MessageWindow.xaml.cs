using System.Windows;
using System.Windows.Media;
using GonkNote.Core.Platform;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Der Ersatz für <see cref="MessageBox"/> — das Gegenstück zu
/// <c>src/GonkNote.Avalonia/Views/MessageWindow.axaml.cs</c>.
///
/// <para>
/// <b>Warum dieser Kopf die eigene MessageBox aufgibt</b> (HANDOFF §4.75). Er zeigte seine
/// Meldungen an <b>28 Stellen</b> über <c>MessageBox.Show</c>. Die trägt Systemfarben,
/// Systemschrift und Systemsymbol — und sah damit an keiner Stelle aus wie die App, während
/// der Linux-Kopf ein eigenes, gestaltetes Fenster hat. <b>Jede Fehlermeldung und jede
/// Rückfrage war ein sichtbarer Unterschied.</b>
/// </para>
/// <para>
/// <b>Was dabei nicht verloren geht:</b> Escape schließt weiterhin, die Eingabetaste
/// bestätigt, und ohne Besitzer erscheint das Fenster mittig auf dem Bildschirm statt hinter
/// der App. Das sind die drei Dinge, die eine <c>MessageBox</c> von sich aus richtig macht
/// und die man beim Nachbauen als Erstes verliert.
/// </para>
/// </summary>
public partial class MessageWindow : Window
{
    private bool _antwort;

    public MessageWindow() => InitializeComponent();

    /// <summary>
    /// Zeigt die Meldung und wartet auf die Antwort. <paramref name="frage"/> entscheidet
    /// über zwei Knöpfe (Ja/Nein) oder einen (OK); bei einer Mitteilung ist der Rückgabewert
    /// immer <c>true</c>.
    /// </summary>
    public static bool Zeige(Window? besitzer, string nachricht, DialogSeverity schwere, bool frage)
    {
        var fenster = new MessageWindow
        {
            Owner = besitzer,
            WindowStartupLocation = besitzer == null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
            // Ohne Besitzer verschwände es sonst hinter der App — beim Start und beim
            // Beenden gibt es kurzzeitig keinen.
            ShowInTaskbar = besitzer == null,
        };

        fenster.Nachricht.Text = nachricht;
        fenster.SchwereZeichen.Fill = Farbe(schwere);

        fenster.JaKnopf.Content = frage ? Loc.T("Dlg.Yes") : Loc.T("Dlg.Ok");
        fenster.NeinKnopf.Content = Loc.T("Dlg.No");
        fenster.NeinKnopf.Visibility = frage ? Visibility.Visible : Visibility.Collapsed;
        // Ohne zweiten Knopf muss Escape trotzdem schließen — sonst hängt eine Mitteilung
        // an der Maus fest.
        fenster.JaKnopf.IsCancel = !frage;

        fenster.ShowDialog();
        return fenster._antwort;
    }

    /// <summary>
    /// Dieselbe Zuordnung wie drüben: blau für die Mitteilung, pink für die Warnung, türkis
    /// für die Frage. <b>Die Farben kommen aus dem Theme</b> und nicht als feste Werte —
    /// sonst folgten sie einem späteren eigenen Farbschema nicht (§5 Nr. 27).
    /// </summary>
    private static Brush Farbe(DialogSeverity schwere) =>
        (Brush)Application.Current.FindResource(schwere switch
        {
            DialogSeverity.Warning => "Brush.Pink",
            DialogSeverity.Question => "Brush.Turquoise",
            _ => "Brush.Accent",
        });

    private void Ja_Click(object sender, RoutedEventArgs e)
    {
        _antwort = true;
        DialogResult = true;
    }

    private void Nein_Click(object sender, RoutedEventArgs e)
    {
        _antwort = false;
        DialogResult = false;
    }
}
