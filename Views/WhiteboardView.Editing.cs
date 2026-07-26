using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Bearbeiten von Textfeldern und Notizzetteln direkt auf der Flaeche.
/// </summary>
public partial class WhiteboardView
{
    // ==================== Texteingabe ====================

    private void StartTextEdit(TextElement el, bool isNew)
    {
        CommitActiveEdit();
        _editingText = el;
        _editingIsNew = isNew;
        _editingOldText = el.Text;
        _cancelEdit = false;

        var screen = ToScreen(new SKPoint(el.X, el.Y));
        Canvas.SetLeft(EditBox, screen.X - 4);
        Canvas.SetTop(EditBox, screen.Y - 3);
        EditBox.FontSize = Math.Max(8, el.FontSize * Zoom);
        EditBox.FontFamily = new FontFamily(string.IsNullOrEmpty(el.FontFamily) ? "Segoe UI" : el.FontFamily);
        EditBox.Background = el.Background is { } bgHex
            ? BrushFromHex(bgHex)
            : new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
        EditBox.Text = el.Text;
        try
        {
            var c = SKColor.Parse(el.Color);
            EditBox.Foreground = new SolidColorBrush(Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
        }
        catch { /* Standardfarbe behalten */ }

        EditBox.Visibility = Visibility.Visible;
        EditBox.Focus();
        EditBox.CaretIndex = EditBox.Text.Length;
        Skia.InvalidateVisual();
    }

    private void EditBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _cancelEdit = true;
            Focus(); // löst LostFocus aus
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            Focus();
            e.Handled = true;
        }
    }

    private void EditBox_LostFocus(object sender, RoutedEventArgs e) => CommitActiveEdit();

    private void CommitTextEdit()
    {
        if (_editingText == null || _page == null || _vm == null) return;
        var el = _editingText;
        _editingText = null;

        EditBox.Visibility = Visibility.Collapsed;
        string newText = _cancelEdit ? _editingOldText : EditBox.Text;
        _cancelEdit = false;

        if (_editingIsNew)
        {
            if (!string.IsNullOrWhiteSpace(newText))
            {
                el.Text = newText;
                _page.Elements.Add(el);
                _vm.Undo.Push(_page, new AddElementsAction(new WbElement[] { el }));
                MarkDirty();
            }
        }
        else if (string.IsNullOrWhiteSpace(newText))
        {
            var action = new RemoveElementsAction(_page, new[] { el });
            action.Redo(_page);
            _vm.Undo.Push(_page, action);
            MarkDirty();
        }
        else if (newText != _editingOldText)
        {
            el.Text = newText;
            _vm.Undo.Push(_page, new TextChangeAction(el, _editingOldText, newText));
            MarkDirty();
        }

        Skia.InvalidateVisual();
    }

    private static SKRect TextBounds(TextElement t) => WbRenderer.TextBounds(t);

    // ==================== Notizzettel-Bearbeitung ====================

    /// <summary>Innenabstand des Zettels zwischen Kartenrand und Text (Canvas-Einheiten).</summary>
    private const float StickyPad = 14f;

    /// <summary>Schließt eine offene Text- oder Notizzettel-Bearbeitung.</summary>
    private void CommitActiveEdit()
    {
        CommitTextEdit();
        CommitStickyEdit();
    }

    private void StartStickyEdit(StickyNoteElement el, bool isNew)
    {
        CommitActiveEdit();
        _editingSticky = el;
        _editingStickyIsNew = isNew;
        _editingStickyOld = el.Text;
        _cancelEdit = false;

        var screen = ToScreen(new SKPoint(el.X + StickyPad, el.Y + StickyPad));
        Canvas.SetLeft(EditBox, screen.X);
        Canvas.SetTop(EditBox, screen.Y);
        EditBox.Width = Math.Max(24, (el.Width - StickyPad * 2) * Zoom);
        EditBox.Height = Math.Max(24, (el.Height - StickyPad * 2) * Zoom);
        EditBox.TextWrapping = TextWrapping.Wrap;
        EditBox.VerticalContentAlignment = VerticalAlignment.Top;
        EditBox.FontSize = Math.Max(8, el.FontSize * Zoom);
        EditBox.FontFamily = new FontFamily(string.IsNullOrEmpty(el.FontFamily) ? "Segoe UI" : el.FontFamily);
        EditBox.Background = BrushFromHex(el.Color);
        EditBox.Foreground = BrushFromHex(el.TextColor);
        EditBox.Text = el.Text;

        EditBox.Visibility = Visibility.Visible;
        EditBox.Focus();
        EditBox.CaretIndex = EditBox.Text.Length;
        Skia.InvalidateVisual();
    }

    private void CommitStickyEdit()
    {
        if (_editingSticky == null || _page == null || _vm == null) return;
        var el = _editingSticky;
        _editingSticky = null;

        EditBox.Visibility = Visibility.Collapsed;
        // Zettel-spezifische EditBox-Optik zurücksetzen (sonst erbt das Textfeld sie)
        EditBox.Width = double.NaN;
        EditBox.Height = double.NaN;
        EditBox.TextWrapping = TextWrapping.NoWrap;

        string newText = _cancelEdit ? _editingStickyOld : EditBox.Text;
        _cancelEdit = false;

        if (_editingStickyIsNew)
        {
            // Ein bewusst gesetzter Zettel bleibt bestehen, auch ohne Text
            el.Text = newText;
            _page.Elements.Add(el);
            _vm.Undo.Push(_page, new AddElementsAction(new WbElement[] { el }));
            MarkDirty();
        }
        else if (newText != _editingStickyOld)
        {
            el.Text = newText;
            _vm.Undo.Push(_page, new StickyTextChangeAction(el, _editingStickyOld, newText));
            MarkDirty();
        }

        Skia.InvalidateVisual();
    }

    /// <summary>Dunkler oder heller Text je nach Helligkeit der Zettelfarbe.</summary>
    private static string ReadableStickyTextColor(string bgHex)
    {
        var b = ParseColor(bgHex);
        double lum = 0.2126 * b.Red + 0.7152 * b.Green + 0.0722 * b.Blue;
        return lum > 140 ? "#FF1F2937" : "#FFF9FAFB";
    }
}
