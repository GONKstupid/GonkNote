using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GonkNote.Services;
using Microsoft.Win32;

namespace GonkNote.Views;

/// <summary>
/// Layout: Papierformat, Orientierung, Seitenränder, Wasserzeichen/Hintergrundbild
/// sowie die Seiten-Darstellung (Seitengröße, Kopf-/Fußzeilen-Vorschau) und das Lineal.
/// </summary>
public partial class TextEditorView
{
    // ==================== Modell → UI ====================

    /// <summary>Befüllt die Layout-Felder aus dem Dokument (ohne Änderungs-Events auszulösen).</summary>
    private void LoadSettingsToUi()
    {
        if (_vm == null) return;
        var doc = _vm.Doc;
        _syncingLayout = true;
        try
        {
            foreach (ComboBoxItem item in FormatCombo.Items)
                if ((string)item.Content == doc.PageFormat) { FormatCombo.SelectedItem = item; break; }

            BtnPortrait.IsChecked = !doc.Landscape;
            BtnLandscape.IsChecked = doc.Landscape;

            MarginL.Text = FormatNum(doc.MarginLeftCm);
            MarginT.Text = FormatNum(doc.MarginTopCm);
            MarginR.Text = FormatNum(doc.MarginRightCm);
            MarginB.Text = FormatNum(doc.MarginBottomCm);
            SyncMarginPreset();

            int opacityIdx = doc.WatermarkOpacity switch
            {
                <= 0.2 => 3,
                <= 0.45 => 2,
                <= 0.8 => 1,
                _ => 0,
            };
            WatermarkOpacityCombo.SelectedIndex = opacityIdx;

            BreaksCheck.IsChecked = doc.ShowPageBreaks;
        }
        finally
        {
            _syncingLayout = false;
        }
    }

    // ==================== Einstellungs-Seitenleiste („Erweiterte Einstellungen") ====================

    /// <summary>Die aktuell angezeigte Sektion; null = Leiste zu.</summary>
    private Expander? _activeSection;

    /// <summary>
    /// Öffnet die Leiste mit GENAU der angeforderten Sektion – die übrigen bleiben
    /// ausgeblendet (Nutzer-Wunsch: Ränder/Absätze/Hintergrundbild erscheinen nur
    /// über ihren jeweiligen Button im Layout-Tab, Tabelle übers Rechtsklick-Menü).
    /// </summary>
    private void OpenSettings(Expander section)
    {
        _activeSection = section;
        SettingsPanel.Visibility = Visibility.Visible;
        foreach (var sec in new[] { SecMargins, SecSpacing, SecWatermark, SecTable })
        {
            bool on = sec == section;
            sec.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
            sec.IsExpanded = on;
        }
        section.BringIntoView();
    }

    private void OpenMargins_Click(object s, RoutedEventArgs e) => OpenSettings(SecMargins);
    private void OpenSpacing_Click(object s, RoutedEventArgs e) => OpenSettings(SecSpacing);
    private void OpenWatermark_Click(object s, RoutedEventArgs e) => OpenSettings(SecWatermark);

    private void CloseSettings_Click(object s, RoutedEventArgs e)
    {
        _activeSection = null;
        SettingsPanel.Visibility = Visibility.Collapsed;
    }

    // ==================== Papierformat & Orientierung ====================

    private void PageFormat_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editor == null || _vm == null || _loading || _syncingLayout) return;
        if (FormatCombo.SelectedItem is not ComboBoxItem item) return;
        _vm.Doc.PageFormat = (string)item.Content;
        ApplyPageSetup();
        MarkDirty();
    }

    private void Orientation_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        bool landscape = s == BtnLandscape;
        _vm.Doc.Landscape = landscape;
        BtnPortrait.IsChecked = !landscape;
        BtnLandscape.IsChecked = landscape;
        ApplyPageSetup();
        MarkDirty();
    }

    // ==================== Seitenränder ====================

    private static readonly (string Name, double L, double T, double R, double B)[] MarginPresets =
    {
        ("Normal", 2, 2, 2, 2),
        ("Schmal", 1.27, 1.27, 1.27, 1.27),
        ("Breit", 3, 3, 3, 3),
        ("Lernblatt", 4, 2, 2, 2),   // 4 cm links für Randnotizen (Lernblatt-Skill)
    };

    private void MarginPreset_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editor == null || _vm == null || _loading || _syncingLayout) return;
        int idx = MarginPresetCombo.SelectedIndex;
        if (idx < 0 || idx >= MarginPresets.Length) return;  // "Benutzerdefiniert"

        var p = MarginPresets[idx];
        _syncingLayout = true;
        MarginL.Text = FormatNum(p.L);
        MarginT.Text = FormatNum(p.T);
        MarginR.Text = FormatNum(p.R);
        MarginB.Text = FormatNum(p.B);
        _syncingLayout = false;

        (_vm.Doc.MarginLeftCm, _vm.Doc.MarginTopCm, _vm.Doc.MarginRightCm, _vm.Doc.MarginBottomCm) =
            (p.L, p.T, p.R, p.B);
        ApplyPageSetup();
        MarkDirty();
    }

    private void Margin_Changed(object s, RoutedEventArgs e)
    {
        if (Editor == null || _vm == null || _loading || _syncingLayout) return;
        if (!TryParseNum(MarginL.Text, out double l) || !TryParseNum(MarginT.Text, out double t) ||
            !TryParseNum(MarginR.Text, out double r) || !TryParseNum(MarginB.Text, out double b))
            return;

        var doc = _vm.Doc;
        doc.MarginLeftCm = Math.Clamp(l, 0, 10);
        doc.MarginTopCm = Math.Clamp(t, 0, 10);
        doc.MarginRightCm = Math.Clamp(r, 0, 10);
        doc.MarginBottomCm = Math.Clamp(b, 0, 10);

        SyncMarginPreset();
        ApplyPageSetup();
        MarkDirty();
    }

    /// <summary>Wählt den passenden Preset-Eintrag (oder „Benutzerdefiniert“) ohne Events.</summary>
    private void SyncMarginPreset()
    {
        if (_vm == null) return;
        var doc = _vm.Doc;
        bool wasSyncing = _syncingLayout;
        _syncingLayout = true;
        int match = -1;
        for (int i = 0; i < MarginPresets.Length; i++)
        {
            var p = MarginPresets[i];
            if (Math.Abs(doc.MarginLeftCm - p.L) < 0.01 && Math.Abs(doc.MarginTopCm - p.T) < 0.01 &&
                Math.Abs(doc.MarginRightCm - p.R) < 0.01 && Math.Abs(doc.MarginBottomCm - p.B) < 0.01)
            {
                match = i;
                break;
            }
        }
        MarginPresetCombo.SelectedIndex = match >= 0 ? match : MarginPresets.Length;  // letzter = Benutzerdef.
        _syncingLayout = wasSyncing;
    }

    // ==================== Wasserzeichen / Hintergrundbild ====================

    private void PickWatermark_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var dlg = new OpenFileDialog
        {
            Title = "Hintergrundbild wählen",
            Filter = "Bilder|*.png;*.jpg;*.jpeg;*.bmp|Alle Dateien|*.*",
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            _vm.Doc.WatermarkImage = File.ReadAllBytes(dlg.FileName);
            ApplyPageSetup();
            MarkDirty();
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), $"Bild konnte nicht geladen werden:\n{ex.Message}",
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void WatermarkOpacity_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editor == null || _vm == null || _loading || _syncingLayout) return;
        if (WatermarkOpacityCombo.SelectedItem is not ComboBoxItem item) return;
        _vm.Doc.WatermarkOpacity = double.Parse((string)item.Tag,
            System.Globalization.CultureInfo.InvariantCulture);
        ApplyPageSetup();
        MarkDirty();
    }

    private void RemoveWatermark_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || _vm.Doc.WatermarkImage == null) return;
        _vm.Doc.WatermarkImage = null;
        ApplyPageSetup();
        MarkDirty();
    }

    // ==================== Seiten-Darstellung ====================

    /// <summary>
    /// Überträgt die Seiteneinstellungen auf die Darstellung: Seitengröße, Ränder
    /// (als Editor-Padding), Kopf-/Fußzeilen-Vorschau, Wasserzeichen, Lineal.
    /// Hinweis: Der Editor zeigt eine fortlaufende Seite (kein Live-Umbruch);
    /// der Seitenumbruch passiert beim PDF-Export.
    /// </summary>
    private void ApplyPageSetup()
    {
        if (_vm == null) return;
        var doc = _vm.Doc;
        var (w, h) = TextStyles.PageSize(doc);
        var margin = TextStyles.PageMarginPx(doc);

        Page.Width = w;
        Page.MinHeight = h;
        Editor.Padding = margin;
        Editor.MinHeight = h - 2;

        // Kopf-/Fußzeilen-Vorschau in den Randbereichen
        string title = _vm.Title;
        HeaderPreview.Text = TextStyles.ResolveHeaderFooter(doc.HeaderText, 1, 1, title);
        FooterPreview.Text = TextStyles.ResolveHeaderFooter(doc.FooterText, 1, 1, title);
        HeaderPreview.Visibility = doc.HeaderText.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        FooterPreview.Visibility = doc.FooterText.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        HeaderPreview.Margin = new Thickness(margin.Left, Math.Max(4, (margin.Top - 18) / 2), margin.Right, 0);
        FooterPreview.Margin = new Thickness(margin.Left, 0, margin.Right, Math.Max(4, (margin.Bottom - 18) / 2));

        // Wasserzeichen
        if (doc.WatermarkImage is { Length: > 0 } bytes)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                WatermarkView.Source = bmp;
                WatermarkView.Opacity = doc.WatermarkOpacity;
                WatermarkView.Visibility = Visibility.Visible;
            }
            catch
            {
                WatermarkView.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            WatermarkView.Source = null;
            WatermarkView.Visibility = Visibility.Collapsed;
        }

        DrawRuler();
        DrawPageBreaks();
    }

    // ==================== Seitenumbruch-Markierungen ====================

    private void PageBreaks_Toggled(object s, RoutedEventArgs e)
    {
        if (Editor == null || _vm == null || _loading || _syncingLayout) return;
        _vm.Doc.ShowPageBreaks = BreaksCheck.IsChecked == true;
        DrawPageBreaks();
        MarkDirty();
    }

    /// <summary>
    /// Zeichnet gestrichelte Markierungen dort, wo der PDF-Export voraussichtlich
    /// umbricht (Näherung: der Paginator bricht nur an Zeilengrenzen, die Marke
    /// exakt bei jedem vollen Inhaltsmaß pro Seite).
    /// </summary>
    private void DrawPageBreaks()
    {
        if (PageBreakLayer == null) return;
        PageBreakLayer.Children.Clear();
        if (_vm == null || !_vm.Doc.ShowPageBreaks) return;

        var doc = _vm.Doc;
        var (w, h) = TextStyles.PageSize(doc);
        var m = TextStyles.PageMarginPx(doc);
        double contentH = h - m.Top - m.Bottom;
        if (contentH < 60) return;

        double totalContent = Math.Max(0, Editor.ActualHeight - m.Top - m.Bottom);
        int pages = Math.Max(1, (int)Math.Ceiling((totalContent - 1) / contentH));

        var stroke = (Brush)FindResource("Brush.Accent");
        for (int k = 1; k < pages; k++)
        {
            double y = m.Top + k * contentH;

            var line = new Line
            {
                X1 = 6, X2 = w - 6, Y1 = y, Y2 = y,
                Stroke = stroke,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 5, 4 },
                Opacity = 0.55,
            };
            PageBreakLayer.Children.Add(line);

            var label = new TextBlock
            {
                Text = $"Seite {k + 1}",
                FontSize = 10,
                Foreground = stroke,
                Opacity = 0.75,
            };
            Canvas.SetLeft(label, w - m.Right - 44);
            Canvas.SetTop(label, y + 2);
            PageBreakLayer.Children.Add(label);
        }
    }

    // ==================== Lineal ====================

    /// <summary>Zeichnet das horizontale Lineal (cm-Skala, Randzonen, Randmarken).</summary>
    private void DrawRuler()
    {
        if (_vm == null || RulerCanvas == null) return;
        var doc = _vm.Doc;
        var (w, _) = TextStyles.PageSize(doc);
        double z = PageScale?.ScaleX ?? 1.0;
        double width = w * z;

        RulerCanvas.Children.Clear();
        RulerCanvas.Width = width;

        var hover = (Brush)FindResource("Brush.Hover");
        var muted = (Brush)FindResource("Brush.TextMuted");
        var accent = (Brush)FindResource("Brush.Accent");

        double mL = doc.MarginLeftCm * TextStyles.PxPerCm * z;
        double mR = doc.MarginRightCm * TextStyles.PxPerCm * z;

        // Randzonen abdunkeln
        AddRect(0, 0, mL, 22, hover);
        AddRect(width - mR, 0, mR, 22, hover);

        // cm-Skala (Nullpunkt = linker Rand, wie in Word)
        double cmPx = TextStyles.PxPerCm * z;
        int labelStep = cmPx < 22 ? 2 : 1;
        for (double cm = 0; ; cm += 0.5)
        {
            double x = mL + cm * cmPx;
            if (x > width + 0.5) break;
            bool whole = Math.Abs(cm - Math.Round(cm)) < 0.01;
            AddRect(x, whole ? 12 : 15, 1, whole ? 9 : 6, muted);

            if (whole && cm > 0 && (int)Math.Round(cm) % labelStep == 0)
            {
                var label = new TextBlock
                {
                    Text = ((int)Math.Round(cm)).ToString(),
                    FontSize = 8.5,
                    Foreground = muted,
                };
                Canvas.SetLeft(label, x + 2);
                Canvas.SetTop(label, 0);
                RulerCanvas.Children.Add(label);
            }
        }
        // Skala links vom Nullpunkt (Randbereich, rückwärts)
        for (double cm = 1; ; cm++)
        {
            double x = mL - cm * cmPx;
            if (x < -0.5) break;
            AddRect(x, 12, 1, 9, muted);
        }

        // Randmarken (Dreiecke) in Akzentfarbe
        AddMarker(mL, accent);
        AddMarker(width - mR, accent);

        void AddRect(double x, double y, double rw, double rh, Brush fill)
        {
            var rect = new Rectangle { Width = Math.Max(0.5, rw), Height = rh, Fill = fill };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            RulerCanvas.Children.Add(rect);
        }

        void AddMarker(double x, Brush fill)
        {
            var tri = new Polygon
            {
                Points = new PointCollection { new(0, 0), new(8, 0), new(4, 6) },
                Fill = fill,
            };
            Canvas.SetLeft(tri, x - 4);
            Canvas.SetTop(tri, 3);
            RulerCanvas.Children.Add(tri);
        }
    }
}
