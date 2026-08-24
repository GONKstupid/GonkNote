using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using GonkNote.Core.Services;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Datei-Einfüge-Tool: zeigt die gerenderten Seiten einer PDF-/DOCX-Datei als
/// Thumbnails und lässt einzelne Seiten an-/abwählen, bevor sie ins Whiteboard
/// bzw. Notizbuch eingefügt werden.
/// </summary>
public partial class FileInsertDialog : Window
{
    private readonly List<CheckBox> _checks = new();

    /// <summary>Indizes der gewählten Seiten (aufsteigend), nach OK.</summary>
    public List<int> SelectedPages { get; } = new();

    public FileInsertDialog(string fileName, IReadOnlyList<PdfImporter.PdfPageImage> pages)
    {
        InitializeComponent();
        Title = Loc.T("Dialog.ChoosePages", fileName);
        InfoText.Text = Loc.T("Msg.PagesHint", pages.Count);

        for (int i = 0; i < pages.Count; i++)
        {
            var check = new CheckBox
            {
                Content = Loc.T("Msg.PageN", i + 1),
                IsChecked = true,
                Margin = new Thickness(2, 4, 0, 0),
                Foreground = (Brush)FindResource("Brush.Text"),
            };
            check.Checked += (_, _) => UpdateOkButton();
            check.Unchecked += (_, _) => UpdateOkButton();
            _checks.Add(check);

            var thumb = new Image
            {
                Source = DecodeThumb(pages[i].Data),
                Height = 168,
                Stretch = Stretch.Uniform,
            };
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = (Brush)FindResource("Brush.Border"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(3),
                Child = thumb,
            };

            var cell = new StackPanel { Margin = new Thickness(6), Cursor = System.Windows.Input.Cursors.Hand };
            cell.Children.Add(border);
            cell.Children.Add(check);
            var cb = check;  // Klick aufs Bild toggelt die Checkbox
            border.MouseLeftButtonUp += (_, _) => cb.IsChecked = cb.IsChecked != true;

            ThumbPanel.Children.Add(cell);
        }
        UpdateOkButton();
    }

    private static BitmapImage DecodeThumb(byte[] data)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.StreamSource = new MemoryStream(data);
        bmp.DecodePixelHeight = 220;   // Thumbnails klein dekodieren (RAM)
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private int CheckedCount() => _checks.Count(c => c.IsChecked == true);

    private void UpdateOkButton()
    {
        int n = CheckedCount();
        OkButton.Content = n == 1 ? Loc.T("Msg.InsertOnePage") : Loc.T("Msg.InsertPages", n);
        OkButton.IsEnabled = n > 0;
    }

    private void SelectAll_Click(object s, RoutedEventArgs e) => _checks.ForEach(c => c.IsChecked = true);
    private void SelectNone_Click(object s, RoutedEventArgs e) => _checks.ForEach(c => c.IsChecked = false);

    private void Ok_Click(object s, RoutedEventArgs e)
    {
        SelectedPages.Clear();
        for (int i = 0; i < _checks.Count; i++)
            if (_checks[i].IsChecked == true)
                SelectedPages.Add(i);
        DialogResult = true;
    }
}
