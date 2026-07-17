using System.Text;
using System.Windows;
using GonkNote.Models;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Texterkennung (OCR) im Whiteboard/Notizbuch: erkennt Text in den ausgewählten
/// Bildern bzw. – ohne Auswahl – im importierten Seitenhintergrund (PDF-Seite).
/// Das Ergebnis erscheint in einem Dialog zum Kopieren/als Notizzettel einfügen.
/// </summary>
public partial class WhiteboardView
{
    private async void Cm_Ocr_Click(object sender, RoutedEventArgs e)
    {
        HideQuickMenu();
        await RunOcrAsync();
    }

    private async Task RunOcrAsync()
    {
        if (_page == null || _importing) return;

        // Quelle bestimmen: ausgewählte Bilder, sonst der Seitenhintergrund.
        var images = _selection.OfType<ImageElement>()
            .Select(im => im.Data)
            .Where(d => d.Length > 0)
            .ToList();
        if (images.Count == 0 && _page.BackgroundImage is { Length: > 0 } bg)
            images.Add(bg);
        if (images.Count == 0) return;

        _importing = true;
        ShowBusy(images.Count > 1 ? "Text wird erkannt…  (mehrere Bilder)" : "Text wird erkannt…");
        try
        {
            string text = await Task.Run(() =>
            {
                var sb = new StringBuilder();
                foreach (var data in images)
                {
                    string t = OcrService.Recognize(data);
                    if (t.Length == 0) continue;
                    if (sb.Length > 0) sb.Append("\n\n");
                    sb.Append(t);
                }
                return sb.ToString();
            });

            HideBusy();

            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(Window.GetWindow(this),
                    "Es wurde kein Text erkannt.", "Texterkennung",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new OcrResultDialog(text) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
                InsertTextAsNote(dlg.ResultText);
        }
        catch (Exception ex)
        {
            HideBusy();
            MessageBox.Show(Window.GetWindow(this),
                $"Texterkennung fehlgeschlagen:\n{ex.Message}", "Texterkennung",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _importing = false;
            HideBusy();
        }
    }

    /// <summary>Fügt den erkannten Text als Notizzettel mittig ein und wählt ihn aus.</summary>
    private void InsertTextAsNote(string text)
    {
        if (_page == null || _vm == null || string.IsNullOrWhiteSpace(text)) return;

        const float w = 280f, h = 220f;
        var at = ViewCenter();
        var note = new StickyNoteElement
        {
            X = at.X - w / 2f,
            Y = at.Y - h / 2f,
            Width = w,
            Height = h,
            Text = text.Trim(),
            Color = _stickyColorHex,
            TextColor = ReadableStickyTextColor(_stickyColorHex),
        };
        if (!_page.IsInfinite)
        {
            note.X = Math.Clamp(note.X, 0, Math.Max(0, _page.Width - w));
            note.Y = Math.Clamp(note.Y, 0, Math.Max(0, _page.Height - h));
        }

        _page.Elements.Add(note);
        _vm.Undo.Push(_page, new AddElementsAction(new WbElement[] { note }));
        MarkDirty();

        // Zum sofortigen Platzieren gleich auswählen (Verschieben-Werkzeug).
        _suppressToolEvents = true;
        foreach (var b in ToolButtons) b.IsChecked = b == BtnMove;
        _suppressToolEvents = false;
        SetTool(ToolType.Move);
        _selection.Clear();
        _selection.Add(note);
        ComputeSelectionBounds();
        Skia.InvalidateVisual();
    }
}
