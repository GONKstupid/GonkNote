using System.Windows;

namespace GonkNote.Views;

public partial class TableSizeDialog : Window
{
    public int Rows { get; private set; } = 3;
    public int Cols { get; private set; } = 3;

    public TableSizeDialog()
    {
        InitializeComponent();
        RowsCombo.ItemsSource = Enumerable.Range(1, 12).ToList();
        ColsCombo.ItemsSource = Enumerable.Range(1, 8).ToList();
        RowsCombo.SelectedItem = 3;
        ColsCombo.SelectedItem = 3;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Rows = (int)(RowsCombo.SelectedItem ?? 3);
        Cols = (int)(ColsCombo.SelectedItem ?? 3);
        DialogResult = true;
    }
}
