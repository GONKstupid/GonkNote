using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using GonkNote.Core.Editing;
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

    // Wo die Sticker liegen und was als einer zählt, steht seit Phase 4.5 in
    // StickerLibrary (Core). Hier stand der Nutzerordner **von Hand** aus
    // Environment.SpecialFolder.ApplicationData zusammengesetzt — eine Windows-Festlegung
    // mitten in einer Regel, die für alle Köpfe gelten soll. AppPaths weiß seit Phase 2,
    // wo der Datenordner liegt; der Kopf muss es nicht wissen, er muss fragen.

    private void EnsureStickersLoaded()
    {
        if (_stickersLoaded) return;
        _stickersLoaded = true;
        ReloadStickers();
    }

    private void ReloadStickers()
    {
        StickerGrid.Children.Clear();

        var files = StickerLibrary.Alle();

        if (files.Count == 0)
        {
            // Der Text stand hier bis Phase 4.5 **fest auf Deutsch** — in einer App, die
            // zwei Sprachen führt (Dauerregel 1). Aufgefallen beim Portieren, weil der
            // Linux-Kopf denselben Hinweis braucht und ihn übersetzt haben wollte.
            StickerGrid.Children.Add(new TextBlock
            {
                Text = Loc.T("Settings.Sticker.Empty"),
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
            // **Kein SVG-Zweig mehr.** Er stand hier bis Phase 4.5 und konnte seit §4.54
            // nicht mehr laufen: was als Sticker zählt, sagt Bildsammlung.Endungen, und
            // dort steht SVG bewusst nicht — eine Vektordatei gehört vor dem Einfügen
            // gerastert, und das an der Stelle, die die Zielgröße kennt.
            if (PrepareRaster(File.ReadAllBytes(file)) is not { } img) return;

            // Wo der Sticker landet und wie groß er wird, rechnet seit Phase 4.5 Core
            // (WbEinfuegen.FuerSticker) — sonst käme derselbe Sticker im Linux-Kopf anders
            // groß an, und das steht dann so in der Datei.
            var kasten = WbEinfuegen.FuerSticker(img.W, img.H, ViewCenter(), _page);

            var el = new ImageElement
            {
                X = kasten.Left, Y = kasten.Top,
                Width = kasten.Width, Height = kasten.Height,
                Data = img.Data,
            };
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
        // Titel und Filter kamen bis Phase 4.5 **fest auf Deutsch** aus dem Code; die
        // Schlüssel dafür gab es längst bzw. sind mit dem Linux-Kopf dazugekommen.
        var dlg = new OpenFileDialog
        {
            Title = Loc.T("Settings.Sticker.Add"),
            Filter = $"{Loc.T("Filter.Images")}|*.png;*.jpg;*.jpeg;*.webp|{Loc.T("Filter.AllFiles")}|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        int added = 0;
        foreach (var src in dlg.FileNames)
        {
            try
            {
                string dest = Path.Combine(StickerLibrary.UserFolder, Path.GetFileName(src));
                // Namenskollision vermeiden
                int n = 1;
                while (File.Exists(dest))
                {
                    string stem = Path.GetFileNameWithoutExtension(src);
                    dest = Path.Combine(StickerLibrary.UserFolder, $"{stem}_{n++}{Path.GetExtension(src)}");
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
