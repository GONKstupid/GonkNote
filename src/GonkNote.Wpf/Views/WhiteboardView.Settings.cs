using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using GonkNote.Core.Theming;
using GonkNote.Services;
using SkiaSharp;

using GonkNote.Core.Platform;

namespace GonkNote.Views;

/// <summary>
/// Einstellungs-Seitenleiste rechts: Seite, Formen, Text, Notizzettel.
/// </summary>
public partial class WhiteboardView
{
    // ==================== Einstellungen (rechte Seitenleiste) ====================

    private bool _suppressSettingsEvents;

    private static readonly string[] CoverFonts =
    {
        "Segoe UI", "Segoe Print", "Segoe Script", "Arial", "Calibri", "Cambria",
        "Comic Sans MS", "Consolas", "Georgia", "Impact", "Palatino Linotype",
        "Times New Roman", "Trebuchet MS", "Verdana",
    };

    private void PageSetup_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsPanel.Visibility == Visibility.Visible)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            return;
        }
        RefreshSettingsPanel();
        SettingsPanel.Visibility = Visibility.Visible;
    }

    /// <summary>Spiegelt die aktuelle Seite (und ggf. das Cover) in die Panel-Controls.</summary>
    private void RefreshSettingsPanel()
    {
        if (_vm == null || _page == null) return;
        _suppressSettingsEvents = true;

        (_page.Background switch
        {
            PageBackground.Lines => SetBgLines,
            PageBackground.Grid => SetBgGrid,
            PageBackground.Dots => SetBgDots,
            _ => SetBgBlank,
        }).IsChecked = true;

        (_page.Shade switch
        {
            PageShade.Light => SetShadeLight,
            PageShade.Dark => SetShadeDark,
            _ => SetShadeAuto,
        }).IsChecked = true;

        // Formen-Sektion nur bei aktivem Formen-Werkzeug
        ShapeSection.Visibility = _tool == ToolType.Shape ? Visibility.Visible : Visibility.Collapsed;
        // Notizzettel-Sektion nur bei aktivem Notizzettel-Werkzeug
        StickySection.Visibility = _tool == ToolType.Sticky ? Visibility.Visible : Visibility.Collapsed;
        StickerSection.Visibility = _tool == ToolType.Sticker ? Visibility.Visible : Visibility.Collapsed;

        bool paged = !_page.IsInfinite;
        SetSizeSection.Visibility = paged ? Visibility.Visible : Visibility.Collapsed;
        if (paged)
        {
            float longSide = Math.Max(_page.Width, _page.Height);
            (longSide > WhiteboardDoc.A4Height + 1 ? SetSizeA3 : SetSizeA4).IsChecked = true;
            (_page.Width > _page.Height ? SetOrientLandscape : SetOrientPortrait).IsChecked = true;
        }

        // Text-Werkzeug
        if (TextFontBox.ItemsSource == null) TextFontBox.ItemsSource = CoverFonts;
        TextFontBox.SelectedItem = CoverFonts.Contains(_textFont) ? _textFont : CoverFonts[0];
        TextColorSwatch.Background = BrushFromHex(CurrentInkHex());
        TextBgSwatch.Background = _textBgHex is { } textBg ? BrushFromHex(textBg) : Brushes.Transparent;

        bool hasCover = _vm.Doc.Pages.Any(p => p.IsCover);
        CoverSection.Visibility = hasCover ? Visibility.Visible : Visibility.Collapsed;
        if (hasCover)
        {
            var cs = _vm.Doc.Cover;
            CoverStartSwatch.Background = BrushFromHex(cs?.GradientStart ?? "#1E3A8A");
            CoverEndSwatch.Background = BrushFromHex(cs?.GradientEnd ?? "#7C3AED");

            string font = cs?.FontFamily ?? "Segoe UI";
            CoverFontBox.ItemsSource = CoverFonts.Contains(font)
                ? CoverFonts
                : CoverFonts.Append(font).OrderBy(f => f).ToArray();
            CoverFontBox.SelectedItem = font;

            // Nicht auf die Bytes im Datensatz sehen: nach dem ersten Speichern liegt das
            // Cover im Blob-Speicher, der Knopf wäre sonst fälschlich ausgegraut.
            BtnCoverImageRemove.IsEnabled =
                cs != null && ImageCache.Bytes(cs.ImageId, cs.Image) is { Length: > 0 };
        }

        _suppressSettingsEvents = false;
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        var c = ParseColor(hex);
        return new SolidColorBrush(Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
    }

    /// <summary>Änderungen im Panel wirken sofort auf die aktuelle Seite (kein OK-Knopf).</summary>
    private void PageSetting_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingsEvents || _vm == null || _page == null) return;

        _page.Background = SetBgLines.IsChecked == true ? PageBackground.Lines
            : SetBgGrid.IsChecked == true ? PageBackground.Grid
            : SetBgDots.IsChecked == true ? PageBackground.Dots
            : PageBackground.Blank;

        _page.Shade = SetShadeLight.IsChecked == true ? PageShade.Light
            : SetShadeDark.IsChecked == true ? PageShade.Dark
            : PageShade.Auto;

        if (!_page.IsInfinite)
        {
            float w = SetSizeA3.IsChecked == true ? WhiteboardDoc.A3Width : WhiteboardDoc.A4Width;
            float h = SetSizeA3.IsChecked == true ? WhiteboardDoc.A3Height : WhiteboardDoc.A4Height;
            bool landscape = SetOrientLandscape.IsChecked == true;
            float nw = landscape ? h : w, nh = landscape ? w : h;

            bool sizeChanged = Math.Abs(_page.Width - nw) > 0.5f || Math.Abs(_page.Height - nh) > 0.5f;
            _page.Width = nw;
            _page.Height = nh;
            if (sizeChanged) CenterView();

            if (SetAsDefault.IsChecked == true)
            {
                _vm.Doc.NewPageTemplate = new PageTemplate
                {
                    Width = nw,
                    Height = nh,
                    Background = _page.Background,
                    Shade = _page.Shade,
                };
            }
        }

        MarkDirty();
        Skia.InvalidateVisual();
    }

    // ---- Text-Werkzeug (Sidebar-Sektion) ----

    /// <summary>Standard-Hintergrund für neue Textfelder; null = transparent.</summary>
    private string? _textBgHex;
    private string _textFont = "Segoe UI";

    /// <summary>
    /// Sorgt für lesbaren Text: bei zu geringem Helligkeitskontrast zum Hintergrund kippt die
    /// Textfarbe auf Schwarz bzw. Weiß.
    /// <para>
    /// <b>Gerechnet wird seit Phase 4.5 in <see cref="HexColor.MitGenugKontrast"/></b> —
    /// dieselbe Luminanz-Formel wie bei der Zettelschrift, und der Linux-Kopf braucht sie
    /// genauso.
    /// </para>
    /// </summary>
    private static string EnsureReadableTextColor(string textHex, string? bgHex) =>
        HexColor.Parse(textHex, HexColor.Black)
                .MitGenugKontrast(bgHex is null ? null : HexColor.Parse(bgHex, HexColor.Black))
                .ToString();

    /// <summary>Wendet eine Stiländerung auf das gerade bearbeitete bzw. einzeln ausgewählte Textfeld an.</summary>
    private void ApplyToActiveText(Action<TextElement> apply)
    {
        TextElement? target = _editingText
            ?? (_selection.Count == 1 ? _selection.First() as TextElement : null);
        if (target == null) return;

        apply(target);
        MarkDirty();
        if (_editingText == target) StartTextEditRefresh(target);
        Skia.InvalidateVisual();
    }

    /// <summary>EditBox-Optik nach Stiländerung auffrischen, ohne die Eingabe zu unterbrechen.</summary>
    private void StartTextEditRefresh(TextElement el)
    {
        EditBox.FontFamily = new FontFamily(string.IsNullOrEmpty(el.FontFamily) ? "Segoe UI" : el.FontFamily);
        EditBox.Background = el.Background is { } bgHex
            ? BrushFromHex(bgHex)
            : new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
        try
        {
            var c = SKColor.Parse(el.Color);
            EditBox.Foreground = new SolidColorBrush(Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
        }
        catch { /* Farbe behalten */ }
    }

    // ---- Notizzettel-Werkzeug (Sidebar-Sektion) ----

    /// <summary>Wendet eine Änderung auf den gerade bearbeiteten bzw. einzeln ausgewählten Zettel an.</summary>
    private void ApplyToActiveSticky(Action<StickyNoteElement> apply)
    {
        StickyNoteElement? target = _editingSticky
            ?? (_selection.Count == 1 ? _selection.First() as StickyNoteElement : null);
        if (target == null) return;

        apply(target);
        MarkDirty();
        if (_editingSticky == target)
        {
            EditBox.Background = BrushFromHex(target.Color);
            EditBox.Foreground = BrushFromHex(target.TextColor);
        }
        Skia.InvalidateVisual();
    }

    private void StickyColor_Checked(object sender, RoutedEventArgs e)
    {
        // Tag kann während des XAML-Ladens noch fehlen → Standard behalten
        if (((RadioButton)sender).Tag is not string tag || tag.Length == 0) return;
        _stickyColorHex = tag;
        ApplyToActiveSticky(sn =>
        {
            sn.Color = _stickyColorHex;
            sn.TextColor = ReadableStickyTextColor(_stickyColorHex);
        });
    }

    private void StickyColorPick_Click(object sender, RoutedEventArgs e)
    {
        var cur = ParseColor(_stickyColorHex);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), Color.FromRgb(cur.Red, cur.Green, cur.Blue), allowAlpha: false) is not { } c)
            return;

        string hex = $"#FF{c.R:X2}{c.G:X2}{c.B:X2}";
        StickyCustomSwatch.Background = new SolidColorBrush(c);
        StickyCustomSwatch.Tag = hex;
        StickyCustomSwatch.Visibility = Visibility.Visible;
        _stickyColorHex = hex;
        StickyCustomSwatch.IsChecked = true; // löst StickyColor_Checked aus → wendet an
    }

    private void TextColor_Click(object sender, RoutedEventArgs e)
    {
        var cur = ParseColor(CurrentInkHex());
        if (ColorPickerDialog.Pick(Window.GetWindow(this), Color.FromArgb(cur.Alpha, cur.Red, cur.Green, cur.Blue)) is not { } c)
            return;

        // Textfarbe = Tintenfarbe: setzt die eigene Palette-Kachel und gilt für alle Werkzeuge
        string hex = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        CustomSwatch.Background = new SolidColorBrush(c);
        CustomSwatch.Tag = hex;
        CustomSwatch.Visibility = Visibility.Visible;
        _colorTag = hex;
        CustomSwatch.IsChecked = true;

        TextColorSwatch.Background = new SolidColorBrush(c);
        ApplyToActiveText(t => t.Color = hex);
    }

    private void TextBg_Click(object sender, RoutedEventArgs e)
    {
        var initial = _textBgHex is { } cur ? ParseColor(cur) : new SKColor(255, 249, 196);
        if (ColorPickerDialog.Pick(Window.GetWindow(this),
                Color.FromRgb(initial.Red, initial.Green, initial.Blue),
                allowAlpha: false) is not { } c)
            return;

        _textBgHex = $"#FF{c.R:X2}{c.G:X2}{c.B:X2}";
        TextBgSwatch.Background = new SolidColorBrush(c);
        ApplyToActiveText(t =>
        {
            t.Background = _textBgHex;
            t.Color = EnsureReadableTextColor(t.Color, _textBgHex);
        });
    }

    private void TextBgClear_Click(object sender, RoutedEventArgs e)
    {
        _textBgHex = null;
        TextBgSwatch.Background = Brushes.Transparent;
        ApplyToActiveText(t => t.Background = null);
    }

    private void TextFont_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSettingsEvents) return;
        if (TextFontBox.SelectedItem is not string font) return;
        _textFont = font;
        ApplyToActiveText(t => t.FontFamily = font);
    }

    private CoverStyle EnsureCoverStyle()
    {
        _vm!.Doc.Cover ??= new CoverStyle();
        return _vm.Doc.Cover;
    }

    /// <summary>Zum Cover springen, damit Änderungen sofort sichtbar sind.</summary>
    private void CoverChanged()
    {
        if (_vm == null) return;
        MarkDirty();
        int idx = _vm.Doc.Pages.FindIndex(p => p.IsCover);
        if (idx >= 0 && idx != _vm.PageIndex) GoToPage(idx);
        Skia.InvalidateVisual();
    }

    private void CoverStart_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var cs = EnsureCoverStyle();
        var cur = ParseColor(cs.GradientStart);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), Color.FromRgb(cur.Red, cur.Green, cur.Blue), allowAlpha: false) is not { } c)
            return;
        cs.GradientStart = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        CoverStartSwatch.Background = new SolidColorBrush(c);
        CoverChanged();
    }

    private void CoverEnd_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var cs = EnsureCoverStyle();
        var cur = ParseColor(cs.GradientEnd);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), Color.FromRgb(cur.Red, cur.Green, cur.Blue), allowAlpha: false) is not { } c)
            return;
        cs.GradientEnd = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        CoverEndSwatch.Background = new SolidColorBrush(c);
        CoverChanged();
    }

    private void CoverFont_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSettingsEvents || _vm == null) return;
        if (CoverFontBox.SelectedItem is not string font) return;
        EnsureCoverStyle().FontFamily = font;
        CoverChanged();
    }

    private void CoverImage_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Bild als Cover wählen",
            Filter = "Bilder (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg|Alle Dateien (*.*)|*.*",
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var img = Path.GetExtension(dlg.FileName).Equals(".svg", StringComparison.OrdinalIgnoreCase)
                ? RasterizeSvg(File.ReadAllBytes(dlg.FileName))
                : PrepareRaster(File.ReadAllBytes(dlg.FileName));
            if (img == null)
            {
                MessageWindow.Zeige(
                    Window.GetWindow(this),
                    Loc.T("Msg.ImageLoadSimple"),
                    DialogSeverity.Warning, frage: false);
                return;
            }
            var cs = EnsureCoverStyle();
            cs.Image = img.Value.Data;
            cs.ImageId = Guid.NewGuid();
            BtnCoverImageRemove.IsEnabled = true;
            CoverChanged();
        }
        catch
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.ImageLoadSimple"),
                DialogSeverity.Warning, frage: false);
        }
    }

    private void CoverImageRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_vm?.Doc.Cover is not { } cs) return;
        cs.Image = null;
        cs.ImageId = Guid.NewGuid();
        BtnCoverImageRemove.IsEnabled = false;
        CoverChanged();
    }

    // ---- Export aus der Seitenleiste ----
    // Derselbe Weg wie „Datei → Exportieren“, nur mit vorgewähltem Format: der Export gilt
    // immer dem aktiven Tab, und das ist genau das Dokument, dessen Seitenleiste offen ist.

    private void ExportPdf_Click(object sender, RoutedEventArgs e) => ExportAs(".pdf");

    private void ExportPng_Click(object sender, RoutedEventArgs e) => ExportAs(".png");

    private void ExportAs(string ext) =>
        (Application.Current.MainWindow?.DataContext as ViewModels.MainViewModel)?.ExportActiveTab(ext);
}
