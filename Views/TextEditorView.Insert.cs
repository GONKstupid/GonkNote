using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.Services;
using Microsoft.Win32;

namespace GonkNote.Views;

/// <summary>
/// Einfügen: Bilder, Tabellen (inkl. Tabellen-Werkzeuge im Kontextmenü), Infoboxen,
/// Trennlinien, Hyperlinks, Sonderzeichen, Beschriftungen, Kopf-/Fußzeile.
/// </summary>
public partial class TextEditorView
{
    // ==================== Bild ====================

    private void InsertImage_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Bild einfügen",
            Filter = "Bilder|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Alle Dateien|*.*",
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(dlg.FileName);
            bmp.EndInit();
            bmp.Freeze();

            var img = new Image
            {
                Source = bmp,
                MaxWidth = 640,
                Stretch = Stretch.Uniform,
            };
            if (bmp.PixelWidth < 640) img.Width = bmp.PixelWidth;

            Editor.CaretPosition = Editor.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
            _ = new InlineUIContainer(img, Editor.CaretPosition);
            MarkDirty();
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), $"Bild konnte nicht geladen werden:\n{ex.Message}",
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        Editor.Focus();
    }

    // ==================== Tabelle einfügen ====================

    private void InsertTable_Click(object s, RoutedEventArgs e)
    {
        var dlg = new TableSizeDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 8, 0, 8) };
        for (int c = 0; c < dlg.Cols; c++)
            table.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        var borderBrush = (Brush)Application.Current.Resources["Brush.Border"];
        for (int r = 0; r < dlg.Rows; r++)
        {
            var row = new TableRow();
            for (int c = 0; c < dlg.Cols; c++)
                row.Cells.Add(NewCell(borderBrush));
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);

        InsertBlockAtCaret(table);
        MarkDirty();
        Editor.Focus();
    }

    private static TableCell NewCell(Brush borderBrush, Brush? background = null) => new(new Paragraph())
    {
        BorderBrush = borderBrush,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(6, 3, 6, 3),
        Background = background,
    };

    /// <summary>Infobox = 1×1-Tabelle mit Füllfarbe und Rahmen (wie die Boxen des Lernblatt-Skills).</summary>
    private void InsertInfoBox_Click(object s, RoutedEventArgs e)
    {
        var initial = Color.FromRgb(0xDB, 0xEA, 0xFE);  // AccentSoft (Light) als Vorschlag
        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial, allowAlpha: false) is not { } fill) return;

        // Rahmen: etwas dunklere Variante der Füllfarbe
        var border = Color.FromRgb(
            (byte)(fill.R * 0.65), (byte)(fill.G * 0.65), (byte)(fill.B * 0.65));

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 8, 0, 8) };
        table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        var row = new TableRow();
        var cell = NewCell(new SolidColorBrush(border), new SolidColorBrush(fill));
        cell.Padding = new Thickness(10, 8, 10, 8);
        row.Cells.Add(cell);
        group.Rows.Add(row);
        table.RowGroups.Add(group);

        InsertBlockAtCaret(table);
        Editor.CaretPosition = cell.ContentStart;
        MarkDirty();
        Editor.Focus();
    }

    private void InsertRule_Click(object s, RoutedEventArgs e)
    {
        var rule = new BlockUIContainer(new Border
        {
            Height = 2,
            Background = (Brush)Application.Current.Resources["Brush.Border"],
            Margin = new Thickness(0, 6, 0, 6),
        });
        InsertBlockAtCaret(rule);
        MarkDirty();
        Editor.Focus();
    }

    /// <summary>Fügt einen Block nach dem Absatz an der Einfügemarke ein (auch in Zellen/Listen).</summary>
    private void InsertBlockAtCaret(Block block)
    {
        var anchor = Editor.CaretPosition.Paragraph as Block ?? Editor.Document.Blocks.LastBlock;
        var blocks = anchor?.Parent switch
        {
            FlowDocument doc => doc.Blocks,
            TableCell cell => cell.Blocks,
            ListItem li => li.Blocks,
            Section sec => sec.Blocks,
            _ => Editor.Document.Blocks,
        };
        if (anchor != null && blocks.Contains(anchor)) blocks.InsertAfter(anchor, block);
        else blocks.Add(block);
    }

    // ==================== Hyperlinks ====================

    private void Hyperlink_RequestNavigate(object s, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        }
        catch
        {
            // kein Handler für das URI-Schema hinterlegt
        }
        e.Handled = true;
    }

    private Hyperlink? CurrentHyperlink()
    {
        for (object? el = Editor.CaretPosition.Parent; el is TextElement te; el = te.Parent)
            if (te is Hyperlink link) return link;
        return null;
    }

    private void InsertLink_Click(object s, RoutedEventArgs e)
    {
        var existing = CurrentHyperlink();
        string initial = existing?.NavigateUri?.ToString() ?? "https://";

        if (PromptDialog.Show(Window.GetWindow(this), "Link einfügen",
                "Adresse (URL):", initial) is not { } url || url.Trim().Length == 0)
            return;
        url = url.Trim();
        if (!url.Contains("://") && !url.StartsWith("mailto:")) url = "https://" + url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            MessageBox.Show(Window.GetWindow(this), "Die Adresse ist keine gültige URL.",
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (existing != null)
        {
            existing.NavigateUri = uri;
        }
        else if (!Editor.Selection.IsEmpty)
        {
            var link = new Hyperlink(Editor.Selection.Start, Editor.Selection.End)
            {
                NavigateUri = uri,
                ToolTip = $"{uri} (Strg+Klick zum Öffnen)",
            };
            StyleLink(link);
        }
        else
        {
            var link = new Hyperlink(new Run(url), Editor.CaretPosition)
            {
                NavigateUri = uri,
                ToolTip = $"{uri} (Strg+Klick zum Öffnen)",
            };
            StyleLink(link);
        }
        MarkDirty();
        Editor.Focus();
    }

    private static void StyleLink(Hyperlink link)
    {
        link.Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
        link.TextDecorations = TextDecorations.Underline;
    }

    private void RemoveLink_Click(object s, RoutedEventArgs e)
    {
        var link = CurrentHyperlink();
        if (link == null) return;

        var parentInlines = link.Parent switch
        {
            Paragraph p => p.Inlines,
            Span span => span.Inlines,
            _ => null,
        };
        if (parentInlines == null) return;

        // Kinder vor dem Link wieder einhängen, dann Link entfernen
        while (link.Inlines.FirstInline is { } child)
        {
            link.Inlines.Remove(child);
            child.ClearValue(TextElement.ForegroundProperty);
            child.ClearValue(Inline.TextDecorationsProperty);
            parentInlines.InsertBefore(link, child);
        }
        parentInlines.Remove(link);
        MarkDirty();
        Editor.Focus();
    }

    // ==================== Sonderzeichen ====================

    private static readonly string[] Symbols =
    {
        "–", "—", "…", "„", "“", "‚", "‘", "»", "«", "§", "¶", "•",
        "→", "←", "↔", "⇒", "⇔", "↑", "↓", "±", "×", "÷", "≈", "≠",
        "≤", "≥", "∞", "√", "∑", "∫", "π", "Δ", "Ω", "µ", "α", "β",
        "γ", "δ", "λ", "φ", "°", "‰", "½", "⅓", "¼", "¾", "²", "³",
        "€", "©", "®", "™", "✓", "✗", "★", "☆", "♦", "None",
    };

    private void BuildSymbolGrid()
    {
        foreach (var sym in Symbols)
        {
            if (sym == "None") continue;
            var btn = new Button
            {
                Style = (Style)FindResource("FlatButton"),
                Content = sym,
                Width = 30,
                Height = 30,
                Padding = new Thickness(0),
                FontSize = 15,
                FontFamily = new FontFamily("Segoe UI"),
            };
            btn.Click += (_, _) => InsertSymbol(sym);
            SymbolGrid.Children.Add(btn);
        }
    }

    private void InsertSymbol(string sym)
    {
        Editor.Selection.Text = sym;
        Editor.CaretPosition = Editor.Selection.End;
        MarkDirty();
        SymbolPopup.IsOpen = false;
        Editor.Focus();
    }

    private void ToggleSymbols_Click(object s, RoutedEventArgs e) =>
        SymbolPopup.IsOpen = BtnSymbols.IsChecked == true;

    private void SymbolPopup_Closed(object s, EventArgs e) => BtnSymbols.IsChecked = false;

    // ==================== Beschriftung ====================

    private void InsertCaption_Click(object s, RoutedEventArgs e)
    {
        int number = 1 + TextStyles.AllParagraphs(Editor.Document.Blocks)
            .Count(p => new TextRange(p.ContentStart, p.ContentEnd).Text.TrimStart()
                .StartsWith("Abbildung ", StringComparison.Ordinal));

        var caption = new Paragraph(new Run($"Abbildung {number}: "))
        {
            FontSize = 12.5,
            FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x7A, 0x99)),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 10),
        };
        InsertBlockAtCaret(caption);
        Editor.CaretPosition = caption.ContentEnd;
        MarkDirty();
        Editor.Focus();
    }

    // ==================== Kopf-/Fußzeile & Seitenzahlen ====================

    private void EditHeaderFooter_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var dlg = new HeaderFooterDialog(_vm.Doc) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;
        ApplyPageSetup();
        MarkDirty();
    }

    private void InsertPageNumbers_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        if (!_vm.Doc.FooterText.Contains("{SEITE}"))
        {
            _vm.Doc.FooterText = _vm.Doc.FooterText.Length == 0
                ? "Seite {SEITE} von {SEITEN}"
                : _vm.Doc.FooterText + " · Seite {SEITE} von {SEITEN}";
        }
        ApplyPageSetup();
        MarkDirty();
    }

    // ==================== Kontextmenü (inkl. Tabellen-Werkzeuge) ====================

    private readonly List<object> _spellItems = new();

    private void Editor_ContextMenuOpening(object s, ContextMenuEventArgs e)
    {
        // Zuvor eingefügte Rechtschreib-Einträge entfernen
        foreach (var it in _spellItems) EditorMenu.Items.Remove(it);
        _spellItems.Clear();

        // Rechtschreib-Vorschläge zuoberst, falls das Wort unter dem Cursor falsch ist
        var error = Editor.GetSpellingError(Editor.CaretPosition);
        if (error != null)
        {
            int idx = 0;
            var suggestions = error.Suggestions.ToList();
            if (suggestions.Count == 0)
            {
                var none = new MenuItem { Header = "Keine Vorschläge", IsEnabled = false };
                EditorMenu.Items.Insert(idx++, none); _spellItems.Add(none);
            }
            else
            {
                foreach (var sug in suggestions.Take(6))
                {
                    string replacement = sug;
                    var mi = new MenuItem { Header = replacement, FontWeight = FontWeights.SemiBold };
                    mi.Click += (_, _) => { error.Correct(replacement); MarkDirty(); };
                    EditorMenu.Items.Insert(idx++, mi); _spellItems.Add(mi);
                }
            }
            var ignore = new MenuItem { Header = "Alle ignorieren" };
            ignore.Click += (_, _) => error.IgnoreAll();
            EditorMenu.Items.Insert(idx++, ignore); _spellItems.Add(ignore);
            var sep = new Separator();
            EditorMenu.Items.Insert(idx++, sep); _spellItems.Add(sep);
        }

        bool inTable = CurrentCell() != null;
        MenuTable.Visibility = inTable ? Visibility.Visible : Visibility.Collapsed;
        MenuTableSep.Visibility = MenuTable.Visibility;

        bool inLink = CurrentHyperlink() != null;
        MenuLink.Header = inLink ? "Link bearbeiten…" : "Link einfügen…";
        MenuUnlink.Visibility = inLink ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---------- Tabellen-Navigation ----------

    private TableCell? CurrentCell() => CellOf(Editor.CaretPosition);

    private static TableCell? CellOf(TextPointer pos)
    {
        for (object? el = pos.Parent; el is TextElement te; el = te.Parent)
            if (te is TableCell cell) return cell;
        return null;
    }

    private (Table Table, TableRowGroup Group, TableRow Row, TableCell Cell)? CurrentTableContext()
    {
        var cell = CurrentCell();
        if (cell?.Parent is not TableRow row) return null;
        if (row.Parent is not TableRowGroup group) return null;
        if (group.Parent is not Table table) return null;
        return (table, group, row, cell);
    }

    /// <summary>Sorgt dafür, dass Columns zur Zellenzahl passt (für Spaltenbreiten).</summary>
    private static void EnsureColumns(Table table)
    {
        int needed = table.RowGroups.SelectMany(g => g.Rows)
            .Max(r => r.Cells.Sum(c => c.ColumnSpan));
        while (table.Columns.Count < needed) table.Columns.Add(new TableColumn());
    }

    private void TableRowAbove_Click(object s, RoutedEventArgs e) => InsertRow(before: true);
    private void TableRowBelow_Click(object s, RoutedEventArgs e) => InsertRow(before: false);

    private void InsertRow(bool before)
    {
        if (CurrentTableContext() is not { } ctx) return;
        var newRow = new TableRow();
        foreach (var c in ctx.Row.Cells)
        {
            var cell = NewCell(c.BorderBrush ?? Brushes.Gray);
            cell.BorderThickness = c.BorderThickness;
            cell.ColumnSpan = c.ColumnSpan;
            cell.Padding = c.Padding;
            newRow.Cells.Add(cell);
        }
        int idx = ctx.Group.Rows.IndexOf(ctx.Row);
        ctx.Group.Rows.Insert(before ? idx : idx + 1, newRow);
        MarkDirty();
    }

    private void TableColLeft_Click(object s, RoutedEventArgs e) => InsertColumn(before: true);
    private void TableColRight_Click(object s, RoutedEventArgs e) => InsertColumn(before: false);

    private void InsertColumn(bool before)
    {
        if (CurrentTableContext() is not { } ctx) return;
        int cellIdx = ctx.Row.Cells.IndexOf(ctx.Cell);

        foreach (var row in ctx.Table.RowGroups.SelectMany(g => g.Rows))
        {
            int idx = Math.Min(cellIdx, row.Cells.Count);
            var reference = row.Cells.Count > 0 ? row.Cells[Math.Min(idx, row.Cells.Count - 1)] : null;
            var cell = NewCell(reference?.BorderBrush ?? Brushes.Gray);
            if (reference != null)
            {
                cell.BorderThickness = reference.BorderThickness;
                cell.Padding = reference.Padding;
            }
            row.Cells.Insert(before ? idx : Math.Min(idx + 1, row.Cells.Count), cell);
        }
        if (ctx.Table.Columns.Count > 0) ctx.Table.Columns.Add(new TableColumn());
        MarkDirty();
    }

    private void TableDeleteRow_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        ctx.Group.Rows.Remove(ctx.Row);
        if (ctx.Group.Rows.Count == 0) RemoveTable(ctx.Table);
        MarkDirty();
    }

    private void TableDeleteCol_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        int cellIdx = ctx.Row.Cells.IndexOf(ctx.Cell);

        foreach (var row in ctx.Table.RowGroups.SelectMany(g => g.Rows).ToList())
        {
            if (cellIdx < row.Cells.Count) row.Cells.RemoveAt(cellIdx);
            if (row.Cells.Count == 0) ((TableRowGroup)row.Parent).Rows.Remove(row);
        }
        if (cellIdx < ctx.Table.Columns.Count) ctx.Table.Columns.RemoveAt(cellIdx);
        if (!ctx.Table.RowGroups.SelectMany(g => g.Rows).Any()) RemoveTable(ctx.Table);
        MarkDirty();
    }

    private void TableDelete_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        RemoveTable(ctx.Table);
        MarkDirty();
    }

    private void RemoveTable(Table table)
    {
        var blocks = table.Parent switch
        {
            FlowDocument doc => doc.Blocks,
            TableCell cell => cell.Blocks,
            ListItem li => li.Blocks,
            Section sec => sec.Blocks,
            _ => Editor.Document.Blocks,
        };
        blocks.Remove(table);
    }

    private void TableMergeCells_Click(object s, RoutedEventArgs e)
    {
        // Verbindet die Zellen zwischen Auswahlanfang und -ende innerhalb einer Zeile
        var startCell = CellOf(Editor.Selection.Start);
        var endCell = CellOf(Editor.Selection.End);
        if (startCell == null || endCell == null || startCell == endCell) return;
        if (startCell.Parent is not TableRow row || endCell.Parent != row)
        {
            MessageBox.Show(Window.GetWindow(this),
                "Zum Verbinden bitte Zellen innerhalb einer Zeile markieren.",
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        int i = row.Cells.IndexOf(startCell);
        int j = row.Cells.IndexOf(endCell);
        if (i > j) (i, j) = (j, i);

        var target = row.Cells[i];
        int span = 0;
        for (int k = i; k <= j; k++) span += row.Cells[k].ColumnSpan;

        for (int k = j; k > i; k--)
        {
            var victim = row.Cells[k];
            foreach (var b in victim.Blocks.ToList())
            {
                victim.Blocks.Remove(b);
                target.Blocks.Add(b);
            }
            row.Cells.RemoveAt(k);
        }
        target.ColumnSpan = span;
        MarkDirty();
    }

    private void TableUnmergeCell_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx || ctx.Cell.ColumnSpan <= 1) return;
        int extra = ctx.Cell.ColumnSpan - 1;
        ctx.Cell.ColumnSpan = 1;
        int idx = ctx.Row.Cells.IndexOf(ctx.Cell);
        for (int k = 0; k < extra; k++)
        {
            var cell = NewCell(ctx.Cell.BorderBrush ?? Brushes.Gray);
            cell.BorderThickness = ctx.Cell.BorderThickness;
            cell.Padding = ctx.Cell.Padding;
            ctx.Row.Cells.Insert(idx + 1 + k, cell);
        }
        MarkDirty();
    }

    /// <summary>Zellen der Auswahl (mind. die aktuelle Zelle).</summary>
    private List<TableCell> SelectedCells()
    {
        var result = new List<TableCell>();
        var startCell = CellOf(Editor.Selection.Start);
        var endCell = CellOf(Editor.Selection.End);
        if (startCell == null) return result;
        if (endCell == null || endCell == startCell) { result.Add(startCell); return result; }

        // Alle Zellen der Tabelle zwischen Start und Ende (dokumentreihenfolge)
        if (startCell.Parent is TableRow { Parent: TableRowGroup { Parent: Table table } })
        {
            bool active = false;
            foreach (var cell in table.RowGroups.SelectMany(g => g.Rows).SelectMany(r => r.Cells))
            {
                if (cell == startCell) active = true;
                if (active) result.Add(cell);
                if (cell == endCell) break;
            }
        }
        if (result.Count == 0) result.Add(startCell);
        return result;
    }

    private void TableCellColor_Click(object s, RoutedEventArgs e)
    {
        var cells = SelectedCells();
        if (cells.Count == 0) return;
        var initial = (cells[0].Background as SolidColorBrush)?.Color ?? Color.FromRgb(0xDB, 0xEA, 0xFE);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial, allowAlpha: false) is not { } c) return;
        foreach (var cell in cells) cell.Background = new SolidColorBrush(c);
        MarkDirty();
    }

    private void TableCellColorClear_Click(object s, RoutedEventArgs e)
    {
        foreach (var cell in SelectedCells()) cell.Background = null;
        MarkDirty();
    }

    private void TableBorderColor_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        var initial = (ctx.Cell.BorderBrush as SolidColorBrush)?.Color ?? Colors.Gray;
        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial, allowAlpha: false) is not { } c) return;
        var brush = new SolidColorBrush(c);
        foreach (var cell in ctx.Table.RowGroups.SelectMany(g => g.Rows).SelectMany(r => r.Cells))
            cell.BorderBrush = brush;
        MarkDirty();
    }

    private void TableToggleBorders_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        bool visible = ctx.Cell.BorderThickness.Left > 0;
        var thickness = new Thickness(visible ? 0 : 1);
        foreach (var cell in ctx.Table.RowGroups.SelectMany(g => g.Rows).SelectMany(r => r.Cells))
            cell.BorderThickness = thickness;
        MarkDirty();
    }

    private void TableColumnWidth_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        EnsureColumns(ctx.Table);
        int colIdx = 0;
        foreach (var c in ctx.Row.Cells)
        {
            if (c == ctx.Cell) break;
            colIdx += c.ColumnSpan;
        }
        if (colIdx >= ctx.Table.Columns.Count) return;

        var col = ctx.Table.Columns[colIdx];
        string initial = col.Width.IsAuto ? "" : FormatNum(col.Width.Value / TextStyles.PxPerCm);
        if (PromptDialog.Show(Window.GetWindow(this), "Spaltenbreite",
                "Breite in cm (leer = automatisch):", initial) is not { } input)
            return;

        if (input.Trim().Length == 0)
            col.Width = GridLength.Auto;
        else if (TryParseNum(input, out double cm) && cm is > 0.2 and < 30)
            col.Width = new GridLength(cm * TextStyles.PxPerCm);
        MarkDirty();
    }
}
