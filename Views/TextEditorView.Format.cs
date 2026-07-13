using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Zeichen- und Absatzformatierung: Fett/Kursiv/…, Farben, Ausrichtung,
/// Zeilen- und Absatzabstand, Formatvorlagen-Galerie, Format übertragen.
/// </summary>
public partial class TextEditorView
{
    // ==================== Zeichenformate ====================

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

    // ==================== Schrift ====================

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

    // ==================== Formatvorlagen-Galerie ====================

    private readonly List<Button> _styleCards = new();

    private void BuildStyleGallery()
    {
        foreach (var style in TextStyles.All)
        {
            var preview = new TextBlock
            {
                Text = style.Name,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = Math.Min(14, style.Size * 0.5 + 4),
                FontWeight = style.Weight,
                FontStyle = style.Style,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            if (style.ColorHex != null)
                preview.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(style.ColorHex));
            else
                preview.SetResourceReference(TextBlock.ForegroundProperty, "InkBrush");

            var card = new Button
            {
                Style = (Style)FindResource("StyleCard"),
                Content = preview,
                Tag = style,
                ToolTip = style.HeadingLevel > 0
                    ? $"{style.Name} (Strg+Alt+{style.HeadingLevel}) – erscheint im Inhaltsverzeichnis"
                    : style.Name,
            };
            System.Windows.Automation.AutomationProperties.SetName(card, $"Formatvorlage {style.Name}");
            card.Click += (_, _) => ApplyParaStyle((TextStyles.ParaStyle)card.Tag);
            StyleGallery.Children.Add(card);
            _styleCards.Add(card);
        }
    }

    /// <summary>Markiert die Karte der aktuellen Formatvorlage mit Akzentrahmen (Design-Konzept 7.4).</summary>
    private void SyncStyleGallery()
    {
        int level = Editor.Selection.Start.Paragraph is { } p ? TextStyles.HeadingLevel(p) : 0;
        var accent = (Brush)FindResource("Brush.Accent");
        var normal = (Brush)FindResource("Brush.Border");
        foreach (var card in _styleCards)
        {
            var style = (TextStyles.ParaStyle)card.Tag;
            bool active = style.HeadingLevel == level && level > 0;
            card.BorderBrush = active ? accent : normal;
        }
    }

    private void ApplyParaStyle(TextStyles.ParaStyle style)
    {
        TextStyles.Apply(style, Editor.Selection.Start, Editor.Selection.End, CurrentInk());
        MarkDirty();
        RefreshNavigator();
        Editor.Focus();
    }

    // ==================== Format übertragen ====================

    private sealed record PainterFormat(
        object Font, object Size, object Weight, object Style,
        object Foreground, object Background, object Decorations, object Baseline);

    private PainterFormat? _painterFormat;

    private void FormatPainter_Click(object s, RoutedEventArgs e)
    {
        if (BtnFormatPainter.IsChecked != true)
        {
            _painterFormat = null;
            return;
        }

        var sel = Editor.Selection;
        _painterFormat = new PainterFormat(
            sel.GetPropertyValue(TextElement.FontFamilyProperty),
            sel.GetPropertyValue(TextElement.FontSizeProperty),
            sel.GetPropertyValue(TextElement.FontWeightProperty),
            sel.GetPropertyValue(TextElement.FontStyleProperty),
            sel.GetPropertyValue(TextElement.ForegroundProperty),
            sel.GetPropertyValue(TextElement.BackgroundProperty),
            sel.GetPropertyValue(Inline.TextDecorationsProperty),
            sel.GetPropertyValue(Inline.BaselineAlignmentProperty));
        Editor.Focus();
    }

    /// <summary>Wendet das aufgenommene Format auf die nächste Auswahl an. True = verbraucht.</summary>
    private bool TryApplyFormatPainter()
    {
        if (_painterFormat is not { } fmt || BtnFormatPainter.IsChecked != true) return false;
        if (Editor.Selection.IsEmpty) return false;

        var sel = Editor.Selection;
        void Apply(DependencyProperty prop, object value)
        {
            if (value != DependencyProperty.UnsetValue)
                sel.ApplyPropertyValue(prop, value);
        }

        Apply(TextElement.FontFamilyProperty, fmt.Font);
        Apply(TextElement.FontSizeProperty, fmt.Size);
        Apply(TextElement.FontWeightProperty, fmt.Weight);
        Apply(TextElement.FontStyleProperty, fmt.Style);
        Apply(TextElement.ForegroundProperty, fmt.Foreground);
        Apply(TextElement.BackgroundProperty, fmt.Background);
        Apply(Inline.TextDecorationsProperty, fmt.Decorations);
        Apply(Inline.BaselineAlignmentProperty, fmt.Baseline);

        _painterFormat = null;
        BtnFormatPainter.IsChecked = false;
        MarkDirty();
        return true;
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

        foreach (var p in SelectedParagraphs())
            p.LineHeight = factor <= 1.001 ? double.NaN : p.FontSize * factor;
        MarkDirty();
        Editor.Focus();
    }

    /// <summary>Alle Absätze, die die aktuelle Auswahl berührt.</summary>
    private IEnumerable<Paragraph> SelectedParagraphs()
    {
        var startPara = Editor.Selection.Start.Paragraph;
        var endPara = Editor.Selection.End.Paragraph;
        if (startPara == null) yield break;

        for (Block? block = startPara; block != null; block = block.NextBlock)
        {
            if (block is Paragraph p) yield return p;
            if (block == endPara) break;
        }
    }

    // ---------- Abstand vor/nach (Layout-Tab, in pt wie Word) ----------

    private const double PtToDip = 96.0 / 72.0;

    private void ParaSpacing_Changed(object s, RoutedEventArgs e)
    {
        if (Editor == null || _syncing || _syncingLayout) return;
        if (!TryParseNum(SpaceBefore.Text, out double before) ||
            !TryParseNum(SpaceAfter.Text, out double after)) return;

        before = Math.Clamp(before, 0, 200);
        after = Math.Clamp(after, 0, 200);

        bool any = false;
        foreach (var p in SelectedParagraphs())
        {
            p.Margin = new Thickness(p.Margin.Left, before * PtToDip, p.Margin.Right, after * PtToDip);
            any = true;
        }
        if (any) MarkDirty();
    }

    private void SyncParaSpacingFields()
    {
        var p = Editor.Selection.Start.Paragraph;
        if (p == null) return;
        _syncingLayout = true;
        SpaceBefore.Text = FormatNum(p.Margin.Top / PtToDip);
        SpaceAfter.Text = FormatNum(p.Margin.Bottom / PtToDip);
        _syncingLayout = false;
    }

    // ---------- Zahlen-Eingabefelder ----------

    private static bool TryParseNum(string text, out double value) =>
        double.TryParse(text.Replace(',', '.').Trim(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value);

    private static string FormatNum(double value) =>
        Math.Round(value, 2).ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);

    private void NumBox_KeyDown(object s, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;
        // Enter übernimmt den Wert (LostFocus-Handler feuert durch den Fokuswechsel)
        Editor.Focus();
        e.Handled = true;
    }
}
