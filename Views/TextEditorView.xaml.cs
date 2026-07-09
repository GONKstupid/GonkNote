using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GonkNote.ViewModels;

namespace GonkNote.Views;

/// <summary>RTF-basierter Texteditor für Textdokumente.</summary>
public partial class TextEditorView : UserControl
{
    private TextTabViewModel? _vm;
    private bool _loading;

    public TextEditorView()
    {
        InitializeComponent();
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

    private void LoadFromModel()
    {
        if (_vm == null) return;
        _loading = true;
        try
        {
            var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);
            if (_vm.Doc.Rtf.Length > 0)
            {
                using var ms = new MemoryStream(_vm.Doc.Rtf);
                range.Load(ms, DataFormats.Rtf);
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
        range.Save(ms, DataFormats.Rtf);
        _vm.Doc.Rtf = ms.ToArray();
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || _vm == null) return;
        _vm.IsDirty = true;
    }

    private void TextColor_Checked(object sender, RoutedEventArgs e)
    {
        if (Editor == null) return;
        var tag = (string)((System.Windows.Controls.Primitives.ToggleButton)sender).Tag;

        Brush brush = tag == "auto"
            ? (Brush)Application.Current.Resources["Brush.Text"]
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString(tag));

        Editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
        Editor.Focus();
    }
}
