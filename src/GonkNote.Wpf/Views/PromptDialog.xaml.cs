using System.Windows;

namespace GonkNote.Views;

/// <summary>Kleiner generischer Eingabedialog (eine Textzeile).</summary>
public partial class PromptDialog : Window
{
    private PromptDialog()
    {
        InitializeComponent();
    }

    /// <summary>Zeigt den Dialog; null = abgebrochen.</summary>
    public static string? Show(Window? owner, string title, string prompt, string initial = "")
    {
        var dlg = new PromptDialog { Owner = owner, Title = title };
        dlg.PromptLabel.Text = prompt;
        dlg.InputBox.Text = initial;
        dlg.InputBox.Focus();
        dlg.InputBox.SelectAll();
        return dlg.ShowDialog() == true ? dlg.InputBox.Text : null;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
