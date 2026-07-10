using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.ViewModels;
using Microsoft.Win32;

namespace GonkNote.Views;

/// <summary>
/// Rich-Text-Editor für Textdokumente. Speichert als XamlPackage
/// (erhält Bilder/Tabellen), lädt ältere RTF-Dokumente weiterhin.
/// </summary>
public partial class TextEditorView : UserControl
{
    private static readonly double[] FontSizes =
        { 8, 9, 10, 11, 12, 14, 15, 16, 18, 20, 24, 28, 32, 36, 48, 72 };

    private TextTabViewModel? _vm;
    private bool _loading;
    private bool _syncing;
    private string _textColorHex = "#EC4899";
    private Color? _highlightColor = Color.FromRgb(0xFD, 0xE0, 0x47);

    public TextEditorView()
    {
        InitializeComponent();

        FontCombo.ItemsSource = System.Windows.Media.Fonts.SystemFontFamilies
            .Select(f => f.Source).OrderBy(s => s).ToList();
        SizeCombo.ItemsSource = FontSizes;

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.FlushRequested -= FlushToModel;

        _vm = DataContext as TextTabViewModel;
        if (_vm == null) return;

        _vm.FlushRequested += FlushToModel;
        LoadFromModel();
    }

    // ==================== Laden / Speichern ====================

    private void LoadFromModel()
    {
        if (_vm == null) return;
        _loading = true;
        try
        {
            var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);
            var bytes = _vm.Doc.Rtf;
            if (bytes.Length > 2)
            {
                using var ms = new MemoryStream(bytes);
                // XamlPackage ist ein ZIP ("PK"), alles andere ist historisches RTF
                bool isPackage = bytes[0] == 0x50 && bytes[1] == 0x4B;
                range.Load(ms, isPackage ? DataFormats.XamlPackage : DataFormats.Rtf);
            }
            else
            {
                range.Text = "";
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private void FlushToModel()
    {
        if (_vm == null) return;
        var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);
        using var ms = new MemoryStream();
        range.Save(ms, DataFormats.XamlPackage);
        _vm.Doc.Rtf = ms.ToArray();
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || _vm == null) return;
        _vm.IsDirty = true;
    }

    // ==================== Toolbar-Zustand mit Auswahl synchronisieren ====================

    private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _syncing = true;
        try
        {
            var sel = Editor.Selection;

            FontCombo.SelectedItem = sel.GetPropertyValue(TextElement.FontFamilyProperty) is FontFamily ff
                ? ff.Source : null;
            SizeCombo.SelectedItem = sel.GetPropertyValue(TextElement.FontSizeProperty) is double fs
                ? FontSizes.FirstOrDefault(s => Math.Abs(s - fs) < 0.1) : null;

            BtnBold.IsChecked = sel.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight w &&
                                w >= FontWeights.Bold;
            BtnItalic.IsChecked = sel.GetPropertyValue(TextElement.FontStyleProperty) is FontStyle st &&
                                  st == FontStyles.Italic;

            var deco = sel.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
            BtnUnderline.IsChecked = deco?.Any(d => d.Location == TextDecorationLocation.Underline) == true;
            BtnStrike.IsChecked = deco?.Any(d => d.Location == TextDecorationLocation.Strikethrough) == true;

            var baseline = sel.GetPropertyValue(Inline.BaselineAlignmentProperty);
            BtnSub.IsChecked = baseline is BaselineAlignment.Subscript;
            BtnSuper.IsChecked = baseline is BaselineAlignment.Superscript;

            var align = sel.Start.Paragraph?.TextAlignment;
            BtnAlignLeft.IsChecked = align == TextAlignment.Left;
            BtnAlignCenter.IsChecked = align == TextAlignment.Center;
            BtnAlignRight.IsChecked = align == TextAlignment.Right;
            BtnAlignJustify.IsChecked = align == TextAlignment.Justify;
        }
        finally
        {
            _syncing = false;
        }
    }

    // ==================== Zeichenformate ====================

    private void Undo_Click(object s, RoutedEventArgs e) { Editor.Undo(); Editor.Focus(); }
    private void Redo_Click(object s, RoutedEventArgs e) { Editor.Redo(); Editor.Focus(); }

    private void Bold_Click(object s, RoutedEventArgs e) { EditingCommands.ToggleBold.Execute(null, Editor); Editor.Focus(); }
    private void Italic_Click(object s, RoutedEventArgs e) { EditingCommands.ToggleItalic.Execute(null, Editor); Editor.Focus(); }
    private void Underline_Click(object s, RoutedEventArgs e) { EditingCommands.ToggleUnderline.Execute(null, Editor); Editor.Focus(); }

    private void Strike_Click(object s, RoutedEventArgs e)
    {
        var sel = Editor.Selection;
        var deco = sel.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
        bool has = deco?.Any(d => d.Location == TextDecorationLocation.Strikethrough) == true;

        var result = new TextDecorationCollection(
            (deco ?? new TextDecorationCollection()).Where(d => d.Location != TextDecorationLocation.Strikethrough));
        if (!has) result.Add(TextDecorations.Strikethrough[0]);
        sel.ApplyPropertyValue(Inline.TextDecorationsProperty, result);
        Editor.Focus();
    }

    private void Sub_Click(object s, RoutedEventArgs e) => ToggleBaseline(BaselineAlignment.Subscript);
    private void Super_Click(object s, RoutedEventArgs e) => ToggleBaseline(BaselineAlignment.Superscript);

    private void ToggleBaseline(BaselineAlignment target)
    {
        var sel = Editor.Selection;
        var current = sel.GetPropertyValue(Inline.BaselineAlignmentProperty);
        bool active = current is BaselineAlignment b && b == target;
        sel.ApplyPropertyValue(Inline.BaselineAlignmentProperty,
            active ? BaselineAlignment.Baseline : target);
        Editor.Focus();
    }

    private void ClearFormat_Click(object s, RoutedEventArgs e)
    {
        Editor.Selection.ClearAllProperties();
        Editor.Focus();
    }

    // ==================== Schrift / Styles ====================

    private void FontCombo_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editor == null || _syncing || FontCombo.SelectedItem is not string name) return;
        Editor.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily(name));
        Editor.Focus();
    }

    private void SizeCombo_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editor == null || _syncing || SizeCombo.SelectedItem is not double size) return;
        Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
        Editor.Focus();
    }

    private void StyleCombo_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editor == null || _syncing || StyleCombo.SelectedIndex < 0) return;

        (double size, FontWeight weight) = StyleCombo.SelectedIndex switch
        {
            1 => (26.0, FontWeights.Bold),
            2 => (20.0, FontWeights.Bold),
            3 => (16.0, FontWeights.Bold),
            _ => (15.0, FontWeights.Normal),
        };

        var start = Editor.Selection.Start.Paragraph?.ContentStart ?? Editor.Selection.Start;
        var end = Editor.Selection.End.Paragraph?.ContentEnd ?? Editor.Selection.End;
        var range = new TextRange(start, end);
        range.ApplyPropertyValue(TextElement.FontSizeProperty, size);
        range.ApplyPropertyValue(TextElement.FontWeightProperty, weight);
        Editor.Focus();
    }

    // ==================== Farben ====================

    private void ApplyTextColor_Click(object s, RoutedEventArgs e)
    {
        var c = (Color)ColorConverter.ConvertFromString(_textColorHex);
        Editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(c));
        Editor.Focus();
    }

    private void PickTextColor_Click(object s, RoutedEventArgs e)
    {
        var initial = (Color)ColorConverter.ConvertFromString(_textColorHex);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial, allowAlpha: false) is not { } c) return;
        _textColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        TextColorBar.Fill = new SolidColorBrush(c);
        ApplyTextColor_Click(s, e);
    }

    private void ApplyHighlight_Click(object s, RoutedEventArgs e)
    {
        Editor.Selection.ApplyPropertyValue(TextElement.BackgroundProperty,
            _highlightColor is { } c ? new SolidColorBrush(c) : null);
        Editor.Focus();
    }

    private void PickHighlight_Click(object s, RoutedEventArgs e)
    {
        var initial = _highlightColor ?? Color.FromArgb(0, 255, 255, 255);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial) is not { } c) return;

        // Deckkraft 0 = Markierung entfernen
        _highlightColor = c.A == 0 ? null : Color.FromRgb(c.R, c.G, c.B);
        HighlightBar.Fill = _highlightColor is { } hc
            ? new SolidColorBrush(hc)
            : (Brush)Application.Current.Resources["Brush.Border"];
        ApplyHighlight_Click(s, e);
    }

    // ==================== Absatz ====================

    private void Align_Click(object s, RoutedEventArgs e)
    {
        var tag = (string)((ToggleButton)s).Tag;
        var cmd = tag switch
        {
            "Center" => EditingCommands.AlignCenter,
            "Right" => EditingCommands.AlignRight,
            "Justify" => EditingCommands.AlignJustify,
            _ => EditingCommands.AlignLeft,
        };
        cmd.Execute(null, Editor);
        Editor.Focus();
        Editor_SelectionChanged(s, e);
    }

    private void Spacing_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editor == null || _syncing || SpacingCombo.SelectedItem is not ComboBoxItem item) return;
        double factor = double.Parse((string)item.Tag, System.Globalization.CultureInfo.InvariantCulture);

        var startPara = Editor.Selection.Start.Paragraph;
        var endPara = Editor.Selection.End.Paragraph;
        if (startPara == null) return;

        var block = (Block)startPara;
        while (block != null)
        {
            if (block is Paragraph p)
                p.LineHeight = factor <= 1.001 ? double.NaN : p.FontSize * factor;
            if (block == endPara) break;
            block = block.NextBlock;
        }
        if (_vm != null) _vm.IsDirty = true;
        Editor.Focus();
    }

    // ==================== Einfügen ====================

    private void InsertImage_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Bild einfügen",
            Filter = "Bilder|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Alle Dateien|*.*",
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(dlg.FileName);
            bmp.EndInit();
            bmp.Freeze();

            var img = new Image
            {
                Source = bmp,
                MaxWidth = 640,
                Stretch = Stretch.Uniform,
            };
            if (bmp.PixelWidth < 640) img.Width = bmp.PixelWidth;

            Editor.CaretPosition = Editor.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
            _ = new InlineUIContainer(img, Editor.CaretPosition);
            if (_vm != null) _vm.IsDirty = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), $"Bild konnte nicht geladen werden:\n{ex.Message}",
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        Editor.Focus();
    }

    private void InsertTable_Click(object s, RoutedEventArgs e)
    {
        var dlg = new TableSizeDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 8, 0, 8) };
        for (int c = 0; c < dlg.Cols; c++)
            table.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        var borderBrush = (Brush)Application.Current.Resources["Brush.Border"];
        for (int r = 0; r < dlg.Rows; r++)
        {
            var row = new TableRow();
            for (int c = 0; c < dlg.Cols; c++)
            {
                var cell = new TableCell(new Paragraph())
                {
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6, 3, 6, 3),
                };
                row.Cells.Add(cell);
            }
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);

        var insertAfter = Editor.CaretPosition.Paragraph as Block ?? Editor.Document.Blocks.LastBlock;
        if (insertAfter != null) Editor.Document.Blocks.InsertAfter(insertAfter, table);
        else Editor.Document.Blocks.Add(table);

        if (_vm != null) _vm.IsDirty = true;
        Editor.Focus();
    }

    private void InsertRule_Click(object s, RoutedEventArgs e)
    {
        var rule = new BlockUIContainer(new Border
        {
            Height = 2,
            Background = (Brush)Application.Current.Resources["Brush.Border"],
            Margin = new Thickness(0, 6, 0, 6),
        });

        var insertAfter = Editor.CaretPosition.Paragraph as Block ?? Editor.Document.Blocks.LastBlock;
        if (insertAfter != null) Editor.Document.Blocks.InsertAfter(insertAfter, rule);
        else Editor.Document.Blocks.Add(rule);

        if (_vm != null) _vm.IsDirty = true;
        Editor.Focus();
    }

    // ==================== Suchen & Ersetzen ====================

    private void Editor_PreviewKeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ShowFind();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && FindPanel.Visibility == Visibility.Visible)
        {
            CloseFind_Click(s, e);
            e.Handled = true;
        }
    }

    private void ToggleFind_Click(object s, RoutedEventArgs e)
    {
        if (FindPanel.Visibility == Visibility.Visible) CloseFind_Click(s, e);
        else ShowFind();
    }

    private void ShowFind()
    {
        FindPanel.Visibility = Visibility.Visible;
        FindStatus.Text = "";
        if (!Editor.Selection.IsEmpty && Editor.Selection.Text.Length < 60)
            FindBox.Text = Editor.Selection.Text;
        FindBox.Focus();
        FindBox.SelectAll();
    }

    private void CloseFind_Click(object s, RoutedEventArgs e)
    {
        FindPanel.Visibility = Visibility.Collapsed;
        Editor.Focus();
    }

    private void FindBox_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { FindNext_Click(s, e); e.Handled = true; }
        else if (e.Key == Key.Escape) { CloseFind_Click(s, e); e.Handled = true; }
    }

    private void FindNext_Click(object s, RoutedEventArgs e)
    {
        string needle = FindBox.Text;
        if (needle.Length == 0) return;

        var hit = FindFrom(Editor.Selection.End, needle) ?? FindFrom(Editor.Document.ContentStart, needle);
        if (hit == null)
        {
            FindStatus.Text = "Nicht gefunden";
            return;
        }
        FindStatus.Text = "";
        Editor.Selection.Select(hit.Start, hit.End);
        Editor.Focus();
    }

    private void ReplaceOne_Click(object s, RoutedEventArgs e)
    {
        string needle = FindBox.Text;
        if (needle.Length == 0) return;

        if (string.Equals(Editor.Selection.Text, needle, StringComparison.CurrentCultureIgnoreCase))
        {
            Editor.Selection.Text = ReplaceBox.Text;
            if (_vm != null) _vm.IsDirty = true;
        }
        FindNext_Click(s, e);
    }

    private void ReplaceAll_Click(object s, RoutedEventArgs e)
    {
        string needle = FindBox.Text;
        if (needle.Length == 0) return;

        int count = 0;
        var hit = FindFrom(Editor.Document.ContentStart, needle);
        while (hit != null && count < 10000)
        {
            hit.Text = ReplaceBox.Text;
            count++;
            hit = FindFrom(hit.End, needle);
        }
        FindStatus.Text = $"{count} ersetzt";
        if (count > 0 && _vm != null) _vm.IsDirty = true;
    }

    /// <summary>Sucht (ohne Groß-/Kleinschreibung) ab einer Position innerhalb einzelner Text-Runs.</summary>
    private static TextRange? FindFrom(TextPointer start, string needle)
    {
        var pos = start;
        while (pos != null)
        {
            if (pos.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                string run = pos.GetTextInRun(LogicalDirection.Forward);
                int idx = run.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase);
                if (idx >= 0)
                {
                    var s = pos.GetPositionAtOffset(idx);
                    var e = pos.GetPositionAtOffset(idx + needle.Length);
                    if (s != null && e != null) return new TextRange(s, e);
                }
            }
            pos = pos.GetNextContextPosition(LogicalDirection.Forward);
        }
        return null;
    }
}
