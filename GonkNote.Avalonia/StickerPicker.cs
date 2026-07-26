using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;

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

    private static string UserDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "GonkNote", "Stickers");

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

        if (files.Count == 0)
        {
            dlg.Content = new TextBlock
            {
                Text = "Keine Sticker gefunden.\n\nLege PNG/JPG-Dateien ab unter:\n" +
                       BuiltInDir + "\noder\n" + UserDir,
                Margin = new global::Avalonia.Thickness(20),
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            };
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

            dlg.Content = new ScrollViewer
            {
                Content = panel,
                Margin = new global::Avalonia.Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
        }

        await dlg.ShowDialog(owner);
        return chosen;
    }
}
