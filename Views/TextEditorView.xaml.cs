using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using GonkNote.Services;
using GonkNote.ViewModels;

namespace GonkNote.Views;

/// <summary>
/// Rich-Text-Editor für Textdokumente im Ribbon-Layout (Design-Konzept nach
/// ONLYOFFICE-Vorbild, Farben ausschließlich aus Themes/Light|Dark.xaml).
/// Speichert als XamlPackage (erhält Bilder/Tabellen), lädt ältere RTF-Dokumente.
/// Kern: Laden/Speichern, Ribbon-Umschaltung, Zoom, Lineal, Statusleiste,
/// Navigator, Theme-/Ink-Handling. Werkzeuge in den partial-Dateien.
/// </summary>
public partial class TextEditorView : UserControl
{
    private static readonly double[] FontSizes =
        { 8, 9, 10, 11, 12, 14, 15, 16, 18, 20, 24, 28, 32, 36, 48, 72 };

    private TextTabViewModel? _vm;
    private bool _loading;
    private bool _syncing;        // Toolbar ← Auswahl wird gerade synchronisiert
    private bool _syncingLayout;  // Layout-Felder ← Modell wird gerade befüllt
    private string _textColorHex = "#EC4899";
    private Color? _highlightColor = Color.FromRgb(0xFD, 0xE0, 0x47);

    private readonly DispatcherTimer _statsTimer;

    public TextEditorView()
    {
        InitializeComponent();

        FontCombo.ItemsSource = System.Windows.Media.Fonts.SystemFontFamilies
            .Select(f => f.Source).OrderBy(s => s).ToList();
        SizeCombo.ItemsSource = FontSizes;

        BuildStyleGallery();
        BuildSymbolGrid();

        // Wortanzahl/Navigator nicht bei jedem Tastendruck neu berechnen
        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _statsTimer.Tick += (_, _) => { _statsTimer.Stop(); UpdateWordCount(); RefreshNavigator(); };

        Editor.AddHandler(Hyperlink.RequestNavigateEvent,
            new System.Windows.Navigation.RequestNavigateEventHandler(Hyperlink_RequestNavigate));

        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => ThemeService.ThemeChanged += OnThemeChanged;
        Unloaded += (_, _) => ThemeService.ThemeChanged -= OnThemeChanged;
    }

    private static Color CurrentInk() =>
        (Color)Application.Current.Resources["Color.DefaultInk"];

    private void OnThemeChanged()
    {
        // Eingebrannte Standard-Schreibfarbe des alten Themes auf das neue umziehen
        TextStyles.NormalizeInk(Editor.Document, CurrentInk());
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

            TextStyles.NormalizeInk(Editor.Document, CurrentInk());
            LoadSettingsToUi();
            ApplyPageSetup();

            ZoomSlider.Value = Math.Clamp(_vm.Zoom * 100, ZoomSlider.Minimum, ZoomSlider.Maximum);
        }
        finally
        {
            _loading = false;
        }
        UpdateWordCount();
        RefreshNavigator();
        Editor_SelectionChanged(this, new RoutedEventArgs());  // Toolbar initial befüllen
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
        _statsTimer.Stop();
        _statsTimer.Start();
    }

    private void MarkDirty()
    {
        if (_vm != null) _vm.IsDirty = true;
    }

    // ==================== Ribbon ====================

    private void RibbonTab_Checked(object sender, RoutedEventArgs e)
    {
        if (PanelStart == null || PanelRefs == null) return;  // während InitializeComponent
        string tag = (string)((RadioButton)sender).Tag;
        PanelStart.Visibility = tag == "Start" ? Visibility.Visible : Visibility.Collapsed;
        PanelInsert.Visibility = tag == "Einfügen" ? Visibility.Visible : Visibility.Collapsed;
        PanelLayout.Visibility = tag == "Layout" ? Visibility.Visible : Visibility.Collapsed;
        PanelRefs.Visibility = tag == "Verweise" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Undo_Click(object s, RoutedEventArgs e) { Editor.Undo(); Editor.Focus(); }
    private void Redo_Click(object s, RoutedEventArgs e) { Editor.Redo(); Editor.Focus(); }

    // ==================== Toolbar-Zustand mit Auswahl synchronisieren ====================

    private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;

        if (TryApplyFormatPainter()) return;

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

            SyncParaSpacingFields();
            SyncStyleGallery();
        }
        finally
        {
            _syncing = false;
        }

        EnsureCaretVisible();
    }

    /// <summary>
    /// Der äußere ScrollViewer scrollt (die RichTextBox selbst hat keinen Viewport) –
    /// deshalb den Cursor beim Tippen/Navigieren manuell sichtbar halten.
    /// </summary>
    private void EnsureCaretVisible()
    {
        try
        {
            var rect = Editor.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
            if (rect.IsEmpty) return;

            var top = Editor.TransformToAncestor(PageHost).Transform(rect.TopLeft);
            var bottom = Editor.TransformToAncestor(PageHost).Transform(rect.BottomLeft);
            double y1 = top.Y + PageHost.Margin.Top;
            double y2 = bottom.Y + PageHost.Margin.Top;

            if (y1 < MainScroll.VerticalOffset + 8)
                MainScroll.ScrollToVerticalOffset(Math.Max(0, y1 - 40));
            else if (y2 > MainScroll.VerticalOffset + MainScroll.ViewportHeight - 8)
                MainScroll.ScrollToVerticalOffset(y2 - MainScroll.ViewportHeight + 40);
        }
        catch
        {
            // Layout noch nicht fertig – unkritisch
        }
    }

    // ==================== Zoom ====================

    private void Zoom_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomText == null || PageScale == null) return;  // während InitializeComponent
        double z = e.NewValue / 100.0;
        PageScale.ScaleX = z;
        PageScale.ScaleY = z;
        ZoomText.Text = $"{Math.Round(e.NewValue)} %";
        if (_vm != null) _vm.Zoom = z;
        DrawRuler();
    }

    private void ZoomIn_Click(object s, RoutedEventArgs e) => ZoomSlider.Value = Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + 10);
    private void ZoomOut_Click(object s, RoutedEventArgs e) => ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, ZoomSlider.Value - 10);

    private void MainScroll_PreviewMouseWheel(object s, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        ZoomSlider.Value = Math.Clamp(ZoomSlider.Value + (e.Delta > 0 ? 10 : -10),
            ZoomSlider.Minimum, ZoomSlider.Maximum);
        e.Handled = true;
    }

    private void MainScroll_ScrollChanged(object s, ScrollChangedEventArgs e)
    {
        // Lineal horizontal mit dem Arbeitsbereich synchron halten
        if (RulerHost.Width != MainScroll.ExtentWidth && MainScroll.ExtentWidth > 0)
            RulerHost.Width = MainScroll.ExtentWidth;
        RulerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
    }

    // ==================== Statusleiste ====================

    private void UpdateWordCount()
    {
        string text = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;
        int words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        int chars = text.Count(c => c != '\r' && c != '\n');
        WordCountText.Text = $"Wörter: {words} · Zeichen: {chars}";
    }

    private void Language_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editor == null || LanguageCombo.SelectedItem is not ComboBoxItem item) return;
        var lang = XmlLanguage.GetLanguage((string)item.Tag);
        Editor.Language = lang;
        Editor.Document.Language = lang;
    }

    private void Spell_Click(object s, RoutedEventArgs e) =>
        Editor.SpellCheck.IsEnabled = BtnSpell.IsChecked == true;

    // ==================== Überschriften-Navigator ====================

    private void ToggleNavigator_Click(object s, RoutedEventArgs e)
    {
        NavPanel.Visibility = BtnSideNav.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        RefreshNavigator();
    }

    private void RefreshNavigator()
    {
        if (NavPanel.Visibility != Visibility.Visible) return;

        NavList.Items.Clear();
        foreach (var (level, text, para) in TextStyles.CollectHeadings(Editor.Document))
        {
            NavList.Items.Add(new ListBoxItem
            {
                Content = new TextBlock
                {
                    Text = text,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontSize = level == 1 ? 13 : 12,
                    FontWeight = level == 1 ? FontWeights.SemiBold : FontWeights.Normal,
                },
                Margin = new Thickness((level - 1) * 12, 0, 0, 0),
                Padding = new Thickness(6, 3, 4, 3),
                Tag = para,
                ToolTip = text,
            });
        }
        if (NavList.Items.Count == 0)
            NavList.Items.Add(new ListBoxItem
            {
                Content = "Keine Überschriften",
                IsEnabled = false,
                Padding = new Thickness(6, 3, 4, 3),
            });
    }

    private void NavList_Selected(object s, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ListBoxItem { Tag: Paragraph para }) return;
        para.BringIntoView();
        Editor.CaretPosition = para.ContentStart;
        Editor.Focus();
        NavList.SelectedItem = null;
    }

    // ==================== Tastatur ====================

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
        else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
        {
            // Strg+Alt+1…4 = Überschrift 1…4, Strg+Alt+0 = Standard (wie Word)
            int? level = e.Key switch
            {
                Key.D1 or Key.NumPad1 => 1,
                Key.D2 or Key.NumPad2 => 2,
                Key.D3 or Key.NumPad3 => 3,
                Key.D4 or Key.NumPad4 => 4,
                Key.D0 or Key.NumPad0 => 0,
                _ => null,
            };
            if (level is { } lvl)
            {
                ApplyParaStyle(TextStyles.ForHeading(lvl));
                e.Handled = true;
            }
        }
    }
}
