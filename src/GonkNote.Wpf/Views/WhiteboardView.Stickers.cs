using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using GonkNote.Services;
using Microsoft.Win32;

namespace GonkNote.Views;

/// <summary>
/// Sticker-Werkzeug: eine Sammlung von Bild-Aufklebern (Basis-Sticker liegen neben
/// der Exe, eigene Sticker unter %APPDATA%\GonkNote\Stickers). Ein Klick auf eine
/// Kachel fügt den Sticker mittig aufs Blatt ein; platziert/skaliert wird danach mit
/// dem Verschieben-Werkzeug. Neue Sticker lassen sich der Sammlung hinzufügen.
/// </summary>
public partial class WhiteboardView
{
    private bool _stickersLoaded;

    private static readonly string[] StickerExts = { ".png", ".jpg", ".jpeg", ".webp" };

    /// <summary>Ordner der Basis-Sticker (neben der Exe).</summary>
    private static string BaseStickerDir =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Stickers");

    /// <summary>Ordner der eigenen Sticker des Nutzers (persistiert).</summary>
    private static string UserStickerDir
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GonkNote", "Stickers");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private void EnsureStickersLoaded()
    {
        if (_stickersLoaded) return;
        _stickersLoaded = true;
        ReloadStickers();
    }

    private void ReloadStickers()
    {
        StickerGrid.Children.Clear();

        var files = new List<string>();
        foreach (var dir in new[] { BaseStickerDir, UserStickerDir })
        {
            if (!Directory.Exists(dir)) continue;
            files.AddRange(Directory.EnumerateFiles(dir)
                .Where(f => StickerExts.Contains(Path.GetExtension(f).ToLowerInvariant())));
        }

        if (files.Count == 0)
        {
            StickerGrid.Children.Add(new TextBlock
            {
                Text = "Noch keine Sticker. Über „Sticker hinzufügen“ eigene Bilder ergänzen.",
                Foreground = (Brush)FindResource("Brush.TextMuted"),
                TextWrapping = TextWrapping.Wrap,
                Width = 230,
            });
            return;
        }

        foreach (var file in files)
            StickerGrid.Children.Add(MakeStickerThumb(file));
    }

    private Button MakeStickerThumb(string file)
    {
        BitmapImage? thumb = null;
        try
        {
            thumb = new BitmapImage();
            thumb.BeginInit();
            thumb.CacheOption = BitmapCacheOption.OnLoad;
            thumb.DecodePixelWidth = 96;
            thumb.UriSource = new Uri(file);
            thumb.EndInit();
            thumb.Freeze();
        }
        catch
        {
            thumb = null;
        }

        var btn = new Button
        {
            Style = (Style)FindResource("FlatButton"),
            Width = 64,
            Height = 64,
            Margin = new Thickness(3),
            Padding = new Thickness(3),
            ToolTip = Path.GetFileNameWithoutExtension(file),
            Content = thumb != null
                ? new Image { Source = thumb, Stretch = Stretch.Uniform }
                : (object)"?",
        };
        btn.Click += (_, _) => InsertSticker(file);
        return btn;
    }

    private void InsertSticker(string file)
    {
        if (_page == null || _vm == null) return;
        try
        {
            var raw = File.ReadAllBytes(file);
            var prepared = Path.GetExtension(file).Equals(".svg", StringComparison.OrdinalIgnoreCase)
                ? RasterizeSvg(raw)
                : PrepareRaster(raw);
            if (prepared is not { } img) return;

            // Sticker in vernünftiger Größe (lange Kante ~160 px) mittig einfügen
            float target = 160f;
            float scale = Math.Min(1f, target / Math.Max(img.W, img.H));
            float dw = img.W * scale, dh = img.H * scale;
            var at = ViewCenter();
            float x = at.X - dw / 2f, y = at.Y - dh / 2f;
            if (!_page.IsInfinite)
            {
                x = Math.Clamp(x, 0, Math.Max(0, _page.Width - dw));
                y = Math.Clamp(y, 0, Math.Max(0, _page.Height - dh));
            }

            var el = new ImageElement { X = x, Y = y, Width = dw, Height = dh, Data = img.Data };
            _page.Elements.Add(el);
            _vm.Undo.Push(_page, new AddElementsAction(new WbElement[] { el }));
            MarkDirty();

            // Zum sofortigen Platzieren gleich auswählen (Verschieben-Werkzeug)
            _suppressToolEvents = true;
            foreach (var b in ToolButtons) b.IsChecked = b == BtnMove;
            _suppressToolEvents = false;
            SetTool(ToolType.Move);
            _selection.Clear();
            _selection.Add(el);
            ComputeSelectionBounds();
            Skia.InvalidateVisual();
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), Loc.T("Msg.StickerFailed", ex.Message),
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ==================== Diagramm-Werkzeug ====================

    /// <summary>Öffnet den Diagramm-Dialog (wie im Text-Editor) und fügt das Ergebnis als Bild ein.</summary>
    private void InsertChart_Click(object sender, RoutedEventArgs e)
    {
        // Ist ein ToggleButton in der Werkzeugleiste → nicht „gedrückt“ stehen lassen
        if (sender is System.Windows.Controls.Primitives.ToggleButton tb) tb.IsChecked = false;
        if (_page == null || _vm == null) return;

        var dlg = new ChartDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true || dlg.ResultImage == null) return;

        byte[] data;
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(dlg.ResultImage));
        using (var ms = new MemoryStream()) { enc.Save(ms); data = ms.ToArray(); }

        PlaceImages(
            new List<(byte[] Data, float W, float H)>
            {
                (data, dlg.ResultImage.PixelWidth, dlg.ResultImage.PixelHeight),
            },
            ViewCenter());
    }

    private void AddSticker_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Sticker zur Sammlung hinzufügen",
            Filter = "Bilder (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Alle Dateien (*.*)|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        int added = 0;
        foreach (var src in dlg.FileNames)
        {
            try
            {
                string dest = Path.Combine(UserStickerDir, Path.GetFileName(src));
                // Namenskollision vermeiden
                int n = 1;
                while (File.Exists(dest))
                {
                    string stem = Path.GetFileNameWithoutExtension(src);
                    dest = Path.Combine(UserStickerDir, $"{stem}_{n++}{Path.GetExtension(src)}");
                }
                File.Copy(src, dest);
                added++;
            }
            catch
            {
                // einzelnen Fehlversuch überspringen
            }
        }

        if (added > 0) ReloadStickers();
    }
}
