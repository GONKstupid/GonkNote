using System.Windows;

namespace GonkNote.Views;

/// <summary>
/// Zeigt den per OCR erkannten Text zum Bearbeiten/Kopieren an. „Als Notizzettel“
/// schließt mit <see cref="Window.DialogResult"/> = true; der aktuelle Text steht
/// dann in <see cref="ResultText"/> zum Einfügen bereit.
/// </summary>
public partial class OcrResultDialog : Window
{
    public OcrResultDialog(string text)
    {
        InitializeComponent();
        ResultBox.Text = text;
        Loaded += (_, _) => { ResultBox.Focus(); ResultBox.CaretIndex = ResultBox.Text.Length; };
    }

    /// <summary>Der (ggf. bearbeitete) Text.</summary>
    public string ResultText => ResultBox.Text;

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(ResultBox.Text); }
        catch { /* Zwischenablage kurzzeitig gesperrt – ignorieren */ }
    }

    private void Insert_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
