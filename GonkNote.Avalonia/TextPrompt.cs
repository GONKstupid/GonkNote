using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace GonkNote.Avalonia;

/// <summary>
/// Kleiner modaler Eingabedialog (eine Zeile). Bewusst in Code statt XAML: der Dialog ist
/// winzig, und feste Breiten umgehen den Fill-Measure-Quirk aus HANDOFF §9.5.
/// </summary>
public static class TextPrompt
{
    /// <summary>Zeigt den Dialog und liefert die Eingabe (null = abgebrochen).</summary>
    public static async Task<string?> ShowAsync(Window owner, string title, string initial)
    {
        var box = new TextBox
        {
            Text = initial,
            Width = 320,
            Margin = new global::Avalonia.Thickness(0, 0, 0, 12),
        };

        var ok = new Button { Content = "OK", Width = 90, IsDefault = true };
        var cancel = new Button { Content = "Abbrechen", Width = 110, IsCancel = true };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);

        var root = new StackPanel { Margin = new global::Avalonia.Thickness(18) };
        root.Children.Add(box);
        root.Children.Add(buttons);

        var dlg = new Window
        {
            Title = title,
            Width = 380,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = root,
        };

        string? result = null;
        ok.Click += (_, _) => { result = box.Text; dlg.Close(); };
        cancel.Click += (_, _) => dlg.Close();
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { result = box.Text; dlg.Close(); }
            else if (e.Key == Key.Escape) dlg.Close();
        };
        dlg.Opened += (_, _) => { box.Focus(); box.SelectAll(); };

        await dlg.ShowDialog(owner);
        return result;
    }
}
