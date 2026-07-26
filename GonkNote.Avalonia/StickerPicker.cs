using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace GonkNote.Avalonia;

/// <summary>
/// Auswahl eines Stickers aus der Sammlung. Quellen wie in der WPF-App: mitgelieferte Motive
/// neben der Exe (<c>Assets/Stickers</c>) und eigene unter
/// <c>%APPDATA%\GonkNote\Stickers</c> (unter Linux <c>~/.config/GonkNote/Stickers</c>).
/// Bewusst in Code statt XAML (kleiner Dialog, feste Breiten → umgeht den Quirk aus §9.5).
/// </summary>
public static class StickerPicker
{
    private static readonly string[] Exts = { ".png", ".jpg", ".jpeg", ".webp" };

    private static string BuiltInDir =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Stickers");

    /// <summary>
    /// Ordner für eigene Sticker. Wird bei Bedarf **angelegt** — sonst zeigt der Dialog nur
    /// einen Pfad, den es gar nicht gibt (Nutzer-Rückmeldung 2026-07-24).
    /// </summary>
    public static string UserDir
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GonkNote", "Stickers");
            try { Directory.CreateDirectory(dir); } catch { /* schreibgeschützt: egal */ }
            return dir;
        }
    }

    /// <summary>Alle verfügbaren Sticker-Dateien (mitgeliefert + eigene).</summary>
    public static List<string> FindStickers()
    {
        var files = new List<string>();
        foreach (var dir in new[] { BuiltInDir, UserDir })
        {
            if (!Directory.Exists(dir)) continue;
            files.AddRange(Directory
                .EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                .Where(f => Exts.Contains(Path.GetExtension(f).ToLowerInvariant())));
        }
        return files;
    }

    /// <summary>
    /// Kopiert Bilddateien in den Sticker-Ordner des Nutzers (kollisionssicher wie in der
    /// WPF-App: bei Namensgleichheit wird „ (2)", „ (3)" … angehängt).
    /// </summary>
    public static int ImportStickers(IEnumerable<string> sourcePaths)
    {
        string target = UserDir;
        int added = 0;
        foreach (var src in sourcePaths)
        {
            if (!Exts.Contains(Path.GetExtension(src).ToLowerInvariant())) continue;
            try
            {
                string name = Path.GetFileNameWithoutExtension(src);
                string ext = Path.GetExtension(src);
                string dest = Path.Combine(target, name + ext);
                for (int i = 2; File.Exists(dest); i++)
                    dest = Path.Combine(target, $"{name} ({i}){ext}");
                File.Copy(src, dest);
                added++;
            }
            catch { /* einzelne Datei überspringen */ }
        }
        return added;
    }

    /// <summary>Zeigt die Sammlung; liefert den Pfad des gewählten Stickers (null = Abbruch).</summary>
    public static async Task<string?> ShowAsync(Window owner)
    {
        var files = FindStickers();

        var panel = new WrapPanel { Width = 560 };
        string? chosen = null;

        var dlg = new Window
        {
            Title = "Sticker wählen",
            Width = 600,
            Height = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        // Kopfleiste mit Import-Button (der Ordner wird beim Zugriff automatisch angelegt).
        var addBtn = new Button { Content = "＋ Sticker hinzufügen…", Padding = new global::Avalonia.Thickness(10, 5) };
        var hint = new TextBlock
        {
            Text = "Eigene Sticker: " + UserDir,
            FontSize = 11,
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new global::Avalonia.Thickness(10, 0, 0, 0),
            TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis,
        };
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new global::Avalonia.Thickness(12, 10, 12, 4),
        };
        header.Children.Add(addBtn);
        header.Children.Add(hint);

        addBtn.Click += async (_, _) =>
        {
            var picked = await dlg.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Sticker hinzufügen",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Bilder") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" } },
                },
            });
            if (picked.Count == 0) return;

            ImportStickers(picked.Select(f => f.Path.LocalPath));
            dlg.Close();                       // neu geladen wird beim nächsten Öffnen
        };

        if (files.Count == 0)
        {
            var empty = new StackPanel();
            empty.Children.Add(header);
            empty.Children.Add(new TextBlock
            {
                Text = "Noch keine Sticker vorhanden.\n\nFüge welche über den Knopf oben hinzu — " +
                       "sie landen in deinem Sticker-Ordner:\n" + UserDir +
                       "\n\nMitgelieferte Motive werden zusätzlich hier gesucht:\n" + BuiltInDir,
                Margin = new global::Avalonia.Thickness(14, 16, 14, 0),
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                Opacity = 0.8,
            });
            dlg.Content = empty;
        }
        else
        {
            foreach (var file in files)
            {
                Image thumb;
                try
                {
                    thumb = new Image
                    {
                        Source = new Bitmap(file),
                        Width = 88,
                        Height = 88,
                        Stretch = global::Avalonia.Media.Stretch.Uniform,
                    };
                }
                catch { continue; }   // defekte Datei überspringen

                string path = file;
                var btn = new Button
                {
                    Content = thumb,
                    Margin = new global::Avalonia.Thickness(6),
                    Padding = new global::Avalonia.Thickness(4),
                    Background = global::Avalonia.Media.Brushes.Transparent,
                    BorderThickness = new global::Avalonia.Thickness(0),
                    Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand),
                };
                btn.Click += (_, _) => { chosen = path; dlg.Close(); };
                panel.Children.Add(btn);
            }

            var root = new DockPanel();
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);
            root.Children.Add(new ScrollViewer
            {
                Content = panel,
                Margin = new global::Avalonia.Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
            });
            dlg.Content = root;
        }

        await dlg.ShowDialog(owner);
        return chosen;
    }
}
