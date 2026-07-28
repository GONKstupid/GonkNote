using System.Windows;
using System.Windows.Controls;

namespace GonkNote.Views;

/// <summary>Sortier-Optionen für Tabellen: Spalte, Typ (Text/Zahl/Datum), Richtung, Kopfzeile.</summary>
public partial class TableSortDialog : Window
{
    public int ColumnIndex => ColumnCombo.SelectedIndex;
    public string SortType => ((ComboBoxItem)TypeCombo.SelectedItem).Content as string ?? "Text";
    public bool Descending => DirCombo.SelectedIndex == 1;
    public bool HasHeader => HeaderCheck.IsChecked == true;

    public TableSortDialog(IEnumerable<string> columnNames)
    {
        InitializeComponent();
        foreach (var name in columnNames) ColumnCombo.Items.Add(name);
        if (ColumnCombo.Items.Count > 0) ColumnCombo.SelectedIndex = 0;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = ColumnCombo.SelectedIndex >= 0;
}
