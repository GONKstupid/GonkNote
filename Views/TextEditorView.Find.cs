using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace GonkNote.Views;

/// <summary>Suchen &amp; Ersetzen.</summary>
public partial class TextEditorView
{
    private void ToggleFind_Click(object s, RoutedEventArgs e)
    {
        if (FindPanel.Visibility == Visibility.Visible) CloseFind_Click(s, e);
        else ShowFind();
    }

    private void ShowFind()
    {
        FindPanel.Visibility = Visibility.Visible;
        BtnSideFind.IsChecked = true;
        FindStatus.Text = "";
        if (!Editor.Selection.IsEmpty && Editor.Selection.Text.Length < 60)
            FindBox.Text = Editor.Selection.Text;
        FindBox.Focus();
        FindBox.SelectAll();
    }

    private void CloseFind_Click(object s, RoutedEventArgs e)
    {
        FindPanel.Visibility = Visibility.Collapsed;
        BtnSideFind.IsChecked = false;
        Editor.Focus();
    }

    private void FindBox_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { FindNext_Click(s, e); e.Handled = true; }
        else if (e.Key == Key.Escape) { CloseFind_Click(s, e); e.Handled = true; }
    }

    private void FindNext_Click(object s, RoutedEventArgs e)
    {
        string needle = FindBox.Text;
        if (needle.Length == 0) return;

        var hit = FindFrom(Editor.Selection.End, needle) ?? FindFrom(Editor.Document.ContentStart, needle);
        if (hit == null)
        {
            FindStatus.Text = "Nicht gefunden";
            return;
        }
        FindStatus.Text = "";
        Editor.Selection.Select(hit.Start, hit.End);
        Editor.Focus();
    }

    private void ReplaceOne_Click(object s, RoutedEventArgs e)
    {
        string needle = FindBox.Text;
        if (needle.Length == 0) return;

        if (string.Equals(Editor.Selection.Text, needle, StringComparison.CurrentCultureIgnoreCase))
        {
            Editor.Selection.Text = ReplaceBox.Text;
            MarkDirty();
        }
        FindNext_Click(s, e);
    }

    private void ReplaceAll_Click(object s, RoutedEventArgs e)
    {
        string needle = FindBox.Text;
        if (needle.Length == 0) return;

        int count = 0;
        var hit = FindFrom(Editor.Document.ContentStart, needle);
        while (hit != null && count < 10000)
        {
            hit.Text = ReplaceBox.Text;
            count++;
            hit = FindFrom(hit.End, needle);
        }
        FindStatus.Text = $"{count} ersetzt";
        if (count > 0) MarkDirty();
    }

    /// <summary>Sucht (ohne Groß-/Kleinschreibung) ab einer Position innerhalb einzelner Text-Runs.</summary>
    private static TextRange? FindFrom(TextPointer start, string needle)
    {
        var pos = start;
        while (pos != null)
        {
            if (pos.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                string run = pos.GetTextInRun(LogicalDirection.Forward);
                int idx = run.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase);
                if (idx >= 0)
                {
                    var s = pos.GetPositionAtOffset(idx);
                    var e = pos.GetPositionAtOffset(idx + needle.Length);
                    if (s != null && e != null) return new TextRange(s, e);
                }
            }
            pos = pos.GetNextContextPosition(LogicalDirection.Forward);
        }
        return null;
    }
}
