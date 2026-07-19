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

    /// <summary>Expander der Gruppe „Individuell" (eigene Vorlagen), für Auto-Aufklappen nach dem Hinzufügen.</summary>
    private Expander? _coverIndividualExp;

    private void ReloadCoverPresets()
    {
        CoverPresetHost.Children.Clear();
        _coverIndividualExp = null;

        // Gruppen = Unterordner (mitgeliefert: Basic, Muster); Dateien, die direkt
        // in %APPDATA%\GonkNote\Covers liegen, bilden die Gruppe „Individuell" –
        // dort landen auch die über die „+"-Kachel hochgeladenen Vorlagen
        var groups = new List<(string Name, List<string> Files, bool Individual)>();
        foreach (var root in new[] { BaseCoverDir, UserCoverDir })
        {
            if (!Directory.Exists(root)) continue;
            var loose = CoverFiles(root);
            if (loose.Count > 0 || root == UserCoverDir)
                groups.Add((root == BaseCoverDir ? "Weitere" : "Individuell", loose, root == UserCoverDir));
            foreach (var dir in Directory.EnumerateDirectories(root).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var files = CoverFiles(dir);
                if (files.Count > 0) groups.Add((Path.GetFileName(dir), files, false));
            }
        }

        foreach (var (name, files, individual) in groups)
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
                if (individual) grid.Children.Add(MakeAddCoverTile());
            };
            if (individual) _coverIndividualExp = exp;
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

    /// <summary>„+"-Kachel am Ende der Gruppe „Individuell": eigene Vorlagen hochladen.</summary>
    private Button MakeAddCoverTile()
    {
        var btn = new Button
        {
            Style = (Style)FindResource("FlatButton"),
            Width = 46,
            Height = 62,
            Margin = new Thickness(2),
            Content = "+",
            FontSize = 20,
            ToolTip = "Eigene Cover-Vorlage hinzufügen",
        };
        System.Windows.Automation.AutomationProperties.SetName(btn, "Cover-Vorlage hinzufügen");
        btn.Click += (_, _) => AddCoverPresets();
        return btn;
    }

    /// <summary>Kopiert gewählte Bilder nach %APPDATA%\GonkNote\Covers („Individuell").</summary>
    private void AddCoverPresets()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Eigene Cover-Vorlagen hinzufügen",
            Filter = "Bilder (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Alle Dateien (*.*)|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        int added = 0;
        foreach (var src in dlg.FileNames)
        {
            try
            {
                string dest = Path.Combine(UserCoverDir, Path.GetFileName(src));
                // Namenskollision vermeiden (wie bei den Stickern)
                int n = 1;
                while (File.Exists(dest))
                {
                    string stem = Path.GetFileNameWithoutExtension(src);
                    dest = Path.Combine(UserCoverDir, $"{stem}_{n++}{Path.GetExtension(src)}");
                }
                File.Copy(src, dest);
                added++;
            }
            catch
            {
                // einzelnen Fehlversuch überspringen
            }
        }

        if (added > 0)
        {
            ReloadCoverPresets();
            // Gruppe „Individuell" gleich aufklappen, damit die neuen Kacheln sichtbar sind
            if (_coverIndividualExp != null) _coverIndividualExp.IsExpanded = true;
        }
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
