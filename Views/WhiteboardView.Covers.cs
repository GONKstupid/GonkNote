using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GonkNote.Views;

/// <summary>
/// Cover-Vorlagen fürs Notizbuch: mitgelieferte Motive liegen als JPEG neben der
/// Exe (Assets\Covers\&lt;Gruppe&gt;\*.jpg, 1600 px lange Kante), eigene Vorlagen in
/// %APPDATA%\GonkNote\Covers. Ein Klick auf eine Kachel setzt das Bild als Cover –
/// gleicher Weg wie „Bild wählen…": die Bytes landen im CoverStyle → DB.
/// Die Galerie wird erst beim ersten Aufklappen der Cover-Sektion befüllt.
/// </summary>
public partial class WhiteboardView
{
    private bool _coverPresetsLoaded;

    /// <summary>Mitgelieferte Vorlagen (neben der Exe).</summary>
    private static string BaseCoverDir => Path.Combine(AppContext.BaseDirectory, "Assets", "Covers");

    /// <summary>Eigene Vorlagen des Nutzers (persistiert).</summary>
    private static string UserCoverDir
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GonkNote", "Covers");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private void CoverSection_Expanded(object sender, RoutedEventArgs e)
    {
        if (_coverPresetsLoaded) return;
        _coverPresetsLoaded = true;
        ReloadCoverPresets();
    }

    private void ReloadCoverPresets()
    {
        CoverPresetHost.Children.Clear();

        // Gruppen = Unterordner (mitgeliefert: Basic, Muster); Dateien, die direkt
        // im Ordner liegen, bilden eine eigene Gruppe (Nutzer-Ablage ohne Zwang
        // zu Unterordnern)
        var groups = new List<(string Name, List<string> Files)>();
        foreach (var root in new[] { BaseCoverDir, UserCoverDir })
        {
            if (!Directory.Exists(root)) continue;
            var loose = CoverFiles(root);
            if (loose.Count > 0) groups.Add((root == BaseCoverDir ? "Weitere" : "Eigene", loose));
            foreach (var dir in Directory.EnumerateDirectories(root).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var files = CoverFiles(dir);
                if (files.Count > 0) groups.Add((Path.GetFileName(dir), files));
            }
        }

        if (groups.Count == 0)
        {
            CoverPresetHost.Children.Add(new TextBlock
            {
                Text = "Keine Vorlagen gefunden (Assets\\Covers fehlt).",
                Foreground = (Brush)FindResource("Brush.TextMuted"),
                TextWrapping = TextWrapping.Wrap,
            });
            return;
        }

        foreach (var (name, files) in groups)
        {
            // Jede Gruppe als zuklappbarer Bereich (Stil wie die Sektionen);
            // die Kacheln entstehen erst beim ersten Aufklappen der Gruppe
            var grid = new WrapPanel();
            var exp = new Expander
            {
                Header = $"{name} ({files.Count})",
                IsExpanded = false,
                Content = grid,
            };
            bool filled = false;
            exp.Expanded += (_, _) =>
            {
                if (filled) return;
                filled = true;
                foreach (var file in files) grid.Children.Add(MakeCoverThumb(file));
            };
            CoverPresetHost.Children.Add(exp);
        }
    }

    private static List<string> CoverFiles(string dir) =>
        Directory.EnumerateFiles(dir)
            .Where(f => StickerExts.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private Button MakeCoverThumb(string file)
    {
        BitmapImage? thumb = null;
        try
        {
            thumb = new BitmapImage();
            thumb.BeginInit();
            thumb.CacheOption = BitmapCacheOption.OnLoad;
            thumb.DecodePixelWidth = 72;
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
            Width = 46,
            Height = 62,   // Hochformat wie die Notizbuch-Seite
            Margin = new Thickness(2),
            Padding = new Thickness(2),
            ToolTip = Path.GetFileNameWithoutExtension(file),
            Content = thumb != null
                ? new Image { Source = thumb, Stretch = Stretch.UniformToFill }
                : (object)"?",
        };
        btn.Click += (_, _) => ApplyCoverPreset(file);
        return btn;
    }

    private void ApplyCoverPreset(string file)
    {
        if (_vm == null) return;
        try
        {
            if (PrepareRaster(File.ReadAllBytes(file)) is not { } img) return;
            var cs = EnsureCoverStyle();
            cs.Image = img.Data;
            cs.ImageId = Guid.NewGuid();
            BtnCoverImageRemove.IsEnabled = true;
            CoverChanged();
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this),
                $"Vorlage konnte nicht geladen werden:\n{ex.Message}",
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
