using System.Windows;
using GonkNote.Core.Models;

namespace GonkNote.Views;

/// <summary>Bearbeitet Kopf-/Fußzeilentext eines Textdokuments (mit Platzhaltern).</summary>
public partial class HeaderFooterDialog : Window
{
    private readonly TextDoc _doc;

    public HeaderFooterDialog(TextDoc doc)
    {
        InitializeComponent();
        _doc = doc;
        HeaderBox.Text = doc.HeaderText;
        FooterBox.Text = doc.FooterText;
        FirstPageBox.IsChecked = doc.SuppressHeaderOnFirstPage;
        HeaderBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _doc.HeaderText = HeaderBox.Text.Trim();
        _doc.FooterText = FooterBox.Text.Trim();
        _doc.SuppressHeaderOnFirstPage = FirstPageBox.IsChecked == true;
        DialogResult = true;
    }
}
