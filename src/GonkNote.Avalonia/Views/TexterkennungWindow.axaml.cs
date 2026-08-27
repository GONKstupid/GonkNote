using Avalonia.Controls;
using Avalonia.Interactivity;
using GonkNote.Platform;

namespace GonkNote.Views;

/// <summary>
/// Zeigt den erkannten Text zum Bearbeiten, Kopieren oder Ablegen als Notizzettel — das
/// Gegenstück zu <c>OcrResultDialog</c> im WPF-Kopf (Phase 4.5, Stück 6).
///
/// <para>
/// <b>Der Rückgabeweg ist <see cref="AlsZettel"/> und nicht <c>DialogResult</c></b>: das
/// gibt es in Avalonia so nicht. Der Aufrufer fragt nach dem Schließen beide Eigenschaften
/// ab — ob eingefügt werden soll, und was dann eingefügt wird.
/// </para>
/// </summary>
public partial class TexterkennungWindow : Window
{
    // InitializeComponent und nicht AvaloniaXamlLoader.Load — sonst bleiben die
    // x:Name-Felder null (HANDOFF §7).
    public TexterkennungWindow() => InitializeComponent();

    /// <summary>Hat der Nutzer „Als Notizzettel" gewählt?</summary>
    public bool AlsZettel { get; private set; }

    /// <summary>Der Text, so wie er im Feld steht — also gegebenenfalls nachgebessert.</summary>
    public string Ergebnis => ErgebnisFeld.Text ?? string.Empty;

    /// <summary>Zeigt das Fenster und wartet, bis es zu ist.</summary>
    public static TexterkennungWindow Zeige(Window besitzer, string text)
    {
        var fenster = new TexterkennungWindow();
        fenster.ErgebnisFeld.Text = text;

        // Die Marke ans Ende und den Fokus ins Feld: der Nutzer will hier fast immer etwas
        // nachbessern, und ein Fenster, in dem er erst hineinklicken muss, kostet einen
        // Handgriff, den es nicht braucht.
        fenster.Opened += (_, _) =>
        {
            fenster.ErgebnisFeld.Focus();
            fenster.ErgebnisFeld.CaretIndex = fenster.ErgebnisFeld.Text?.Length ?? 0;
        };

        Modal.Warte(fenster.ShowDialog(besitzer));
        return fenster;
    }

    private void Kopieren_Click(object? s, RoutedEventArgs e) =>
        // Über den Kopf-eigenen Dienst und nicht über TopLevel.Clipboard: er kapselt, dass
        // die X11-Zwischenablage am Besitzerfenster hängt (§4.62).
        App.Platform.Clipboard.SetText(Ergebnis);

    private void AlsZettel_Click(object? s, RoutedEventArgs e)
    {
        AlsZettel = true;
        Close();
    }

    private void Schliessen_Click(object? s, RoutedEventArgs e) => Close();
}
