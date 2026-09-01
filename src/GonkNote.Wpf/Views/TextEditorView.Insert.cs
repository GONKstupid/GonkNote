using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.Services;
using Microsoft.Win32;

using GonkNote.Core.Platform;
using GonkNote.Core.Text;

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
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.ImageLoadFailed", ex.Message),
                DialogSeverity.Warning, frage: false);
        }
        Editor.Focus();
    }

    // ==================== Tabelle einfügen ====================

    /// <summary>Baut eine leere Tabelle und fügt sie an der Cursorposition ein (Raster/Dialog/Schnelltabellen).</summary>
    private Table InsertEmptyTable(int rows, int cols)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 8, 0, 8) };
        for (int c = 0; c < cols; c++)
            table.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        var borderBrush = (Brush)Application.Current.Resources["Brush.Border"];
        for (int r = 0; r < rows; r++)
        {
            var row = new TableRow();
            for (int c = 0; c < cols; c++)
                row.Cells.Add(NewCell(borderBrush));
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);

        InsertBlockAtCaret(table);
        // Cursor in die erste Zelle: dort erwartet man die Eingabe, und der
        // Selection-Sync blendet damit sofort den Kontext-Tab „Tabelle" ein
        if (group.Rows.Count > 0 && group.Rows[0].Cells.Count > 0)
            Editor.CaretPosition = group.Rows[0].Cells[0].ContentStart;
        MarkDirty();
        Editor.Focus();
        return table;
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

        if (PromptDialog.Show(Window.GetWindow(this), Loc.T("Ed.Link.Insert"),
                Loc.T("Msg.UrlPrompt"), initial) is not { } url || url.Trim().Length == 0)
            return;
        url = url.Trim();
        if (!url.Contains("://") && !url.StartsWith("mailto:")) url = "https://" + url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.InvalidUrl"),
                DialogSeverity.Warning, frage: false);
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

    // Der Vorrat steht seit §4.88 als `TdSonderzeichen` in Core und nicht mehr hier: Der
    // Linux-Kopf hätte ihn sonst ein zweites Mal gebraucht, und das ist der Fall, der schon
    // dreimal eingetreten ist (§4.77, §4.78, §4.82). **Dabei ist ein Rest herausgefallen:**
    // die alte Liste endete auf den Text "None", den die Schleife unten ausdrücklich
    // übersprang -- ein Wert, der nur da ist, um übergangen zu werden.

    private void BuildSymbolGrid()
    {
        foreach (var sym in TdSonderzeichen.Alle)
        {
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

        // Objekt-Menü (Größe / Hintergrund) nur bei Bild/Form/Diagramm am Cursor
        var obj = CurrentObjectHost();
        bool hasObj = obj != null;
        MenuObjSep.Visibility = hasObj ? Visibility.Visible : Visibility.Collapsed;
        MenuSize.Visibility = hasObj ? Visibility.Visible : Visibility.Collapsed;
        bool isBehind = obj is { Host: Figure };
        MenuToBack.Visibility = hasObj && !isBehind ? Visibility.Visible : Visibility.Collapsed;
        MenuToFront.Visibility = isBehind ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---------- Tabellen-Formatierung in der Seitenleiste ----------

    /// <summary>Hält den Kontext-Tab „Tabelle" synchron zum Cursor (Details in TextEditorView.Table.cs).</summary>
    private void UpdateTableSection() => UpdateTableRibbon();

    private void TableBorders_Click(object s, RoutedEventArgs e) =>
        ApplyTableBordersToSelection((string)((Button)s).Tag);

    /// <summary>
    /// Wendet Randdicke und -variante (alle/außen/innen/keine) nur auf die
    /// AUSGEWÄHLTEN Zellen an. „außen/innen" beziehen sich auf das Rechteck der Auswahl.
    /// </summary>
    private void ApplyTableBordersToSelection(string variant)
    {
        if (Editor == null || CurrentTableContext() is not { } ctx) return;
        if (TableBorderWidth.SelectedItem is not ComboBoxItem wi || !TryParseNum((string)wi.Tag, out double w))
            return;

        var selected = SelectedCells();
        if (selected.Count == 0) return;

        // Gitter-Position jeder Zelle (berücksichtigt Spalten- und Zeilenverbünde)
        var pos = GridPositions(ctx.Table);

        int minRow = selected.Min(c => pos[c].Row);
        int maxRow = selected.Max(c => pos[c].Row + pos[c].RowSpan - 1);
        int minCol = selected.Min(c => pos[c].Col);
        int maxCol = selected.Max(c => pos[c].Col + pos[c].ColSpan - 1);

        foreach (var cell in selected)
        {
            var (r, cs, span, rspan) = pos[cell];
            int ce = cs + span - 1;
            int re = r + rspan - 1;
            bool top, left, right, bottom;
            switch (variant)
            {
                case "none": top = left = right = bottom = false; break;
                case "outer": top = r == minRow; bottom = re == maxRow; left = cs == minCol; right = ce == maxCol; break;
                case "inner": top = r > minRow; left = cs > minCol; right = false; bottom = false; break;
                default: top = left = right = bottom = true; break;   // all
            }
            cell.BorderThickness = new Thickness(left ? w : 0, top ? w : 0, right ? w : 0, bottom ? w : 0);
        }
        MarkDirty();
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

    /// <summary>
    /// Gitter-Position jeder Zelle (Zeile, Spaltenstart, Spannen) unter
    /// Berücksichtigung von Spalten- UND Zeilenverbünden (Belegungsraster).
    /// </summary>
    private static Dictionary<TableCell, (int Row, int Col, int ColSpan, int RowSpan)> GridPositions(Table table)
    {
        var result = new Dictionary<TableCell, (int, int, int, int)>();
        var occupied = new HashSet<(int Row, int Col)>();   // von Zeilenverbünden belegte Plätze
        var rows = table.RowGroups.SelectMany(g => g.Rows).ToList();
        for (int r = 0; r < rows.Count; r++)
        {
            int col = 0;
            foreach (var cell in rows[r].Cells)
            {
                while (occupied.Contains((r, col))) col++;   // Platz unter einem Zeilenverbund überspringen
                result[cell] = (r, col, cell.ColumnSpan, cell.RowSpan);
                for (int rr = r; rr < r + cell.RowSpan; rr++)
                    for (int cc = col; cc < col + cell.ColumnSpan; cc++)
                        occupied.Add((rr, cc));
                col += cell.ColumnSpan;
            }
        }
        return result;
    }

    private void TableMergeCells_Click(object s, RoutedEventArgs e)
    {
        // Verbindet die Zellen im Rechteck zwischen Auswahlanfang und -ende –
        // waagerecht, senkrecht oder beides (ColumnSpan/RowSpan wie in Word).
        var startCell = CellOf(Editor.Selection.Start);
        var endCell = CellOf(Editor.Selection.End);
        if (startCell == null || endCell == null || startCell == endCell) return;
        if (startCell.Parent is not TableRow { Parent: TableRowGroup group } ||
            endCell.Parent is not TableRow { Parent: TableRowGroup endGroup } ||
            group != endGroup || group.Parent is not Table table)
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.MergeSameTable"),
                DialogSeverity.Information, frage: false);
            return;
        }

        var pos = GridPositions(table);

        // Rechteck aus Start- und Endzelle; solange erweitern, bis jede geschnittene
        // Zelle (inkl. bestehender Verbünde) vollständig darin liegt
        int minRow = Math.Min(pos[startCell].Row, pos[endCell].Row);
        int maxRow = Math.Max(pos[startCell].Row + pos[startCell].RowSpan - 1,
                              pos[endCell].Row + pos[endCell].RowSpan - 1);
        int minCol = Math.Min(pos[startCell].Col, pos[endCell].Col);
        int maxCol = Math.Max(pos[startCell].Col + pos[startCell].ColSpan - 1,
                              pos[endCell].Col + pos[endCell].ColSpan - 1);
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (var (cell, p) in pos)
            {
                int r2 = p.Row + p.RowSpan - 1, c2 = p.Col + p.ColSpan - 1;
                bool intersects = p.Row <= maxRow && r2 >= minRow && p.Col <= maxCol && c2 >= minCol;
                if (!intersects) continue;
                if (p.Row < minRow) { minRow = p.Row; grew = true; }
                if (r2 > maxRow) { maxRow = r2; grew = true; }
                if (p.Col < minCol) { minCol = p.Col; grew = true; }
                if (c2 > maxCol) { maxCol = c2; grew = true; }
            }
        }

        var inside = pos.Where(kv =>
                kv.Value.Row >= minRow && kv.Value.Row + kv.Value.RowSpan - 1 <= maxRow &&
                kv.Value.Col >= minCol && kv.Value.Col + kv.Value.ColSpan - 1 <= maxCol)
            .Select(kv => kv.Key).ToList();
        var target = inside.FirstOrDefault(c => pos[c].Row == minRow && pos[c].Col == minCol);
        if (target == null || inside.Count < 2) return;

        foreach (var victim in inside)
        {
            if (victim == target) continue;
            foreach (var b in victim.Blocks.ToList())
            {
                victim.Blocks.Remove(b);
                target.Blocks.Add(b);
            }
            ((TableRow)victim.Parent).Cells.Remove(victim);
        }
        target.ColumnSpan = maxCol - minCol + 1;
        target.RowSpan = maxRow - minRow + 1;
        MarkDirty();
    }

    private void TableUnmergeCell_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        var cell = ctx.Cell;
        if (cell.ColumnSpan <= 1 && cell.RowSpan <= 1) return;

        var pos = GridPositions(ctx.Table);
        var (row, col, colSpan, rowSpan) = pos[cell];
        var rows = ctx.Table.RowGroups.SelectMany(g => g.Rows).ToList();

        TableCell Clone()
        {
            var c = NewCell(cell.BorderBrush ?? Brushes.Gray);
            c.BorderThickness = cell.BorderThickness;
            c.Padding = cell.Padding;
            return c;
        }

        cell.ColumnSpan = 1;
        cell.RowSpan = 1;

        // Eigene Zeile: die restlichen Spalten des Verbunds auffüllen
        int idx = ctx.Row.Cells.IndexOf(cell);
        for (int k = 1; k < colSpan; k++)
            ctx.Row.Cells.Insert(idx + k, Clone());

        // Zeilen darunter: alle Spalten des Verbunds an der richtigen Stelle einfügen
        // (Einfüge-Index = vor der ersten Zelle, deren Spaltenstart rechts vom Verbund lag)
        for (int r = row + 1; r < row + rowSpan && r < rows.Count; r++)
        {
            var tr = rows[r];
            int insertAt = tr.Cells.Count;
            for (int i = 0; i < tr.Cells.Count; i++)
                if (pos[tr.Cells[i]].Col > col) { insertAt = i; break; }
            for (int k = 0; k < colSpan; k++)
                tr.Cells.Insert(insertAt + k, Clone());
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
        // Wirkt nur auf die ausgewählten Zellen (Erweiterte Einstellungen = Auswahl)
        var cells = SelectedCells();
        if (cells.Count == 0) return;
        var initial = (cells[0].BorderBrush as SolidColorBrush)?.Color ?? Colors.Gray;
        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial, allowAlpha: false) is not { } c) return;
        var brush = new SolidColorBrush(c);
        foreach (var cell in cells) cell.BorderBrush = brush;
        MarkDirty();
    }

    private void TableColumnWidth_Click(object s, RoutedEventArgs e)
    {
        // Wirkt auf alle Spalten, die von den ausgewählten Zellen abgedeckt werden
        if (CurrentTableContext() is not { } ctx) return;
        EnsureColumns(ctx.Table);
        var pos = GridPositions(ctx.Table);
        var cols = new SortedSet<int>();
        foreach (var cell in SelectedCells())
            for (int i = 0; i < pos[cell].ColSpan; i++)
                cols.Add(pos[cell].Col + i);
        cols.RemoveWhere(i => i >= ctx.Table.Columns.Count);
        if (cols.Count == 0) return;

        var first = ctx.Table.Columns[cols.First()];
        string initial = first.Width.IsAuto ? "" : FormatNum(first.Width.Value / TextStyles.PxPerCm);
        if (PromptDialog.Show(Window.GetWindow(this), "Spaltenbreite",
                "Breite in cm (leer = automatisch):", initial) is not { } input)
            return;

        GridLength width;
        if (input.Trim().Length == 0)
            width = GridLength.Auto;
        else if (TryParseNum(input, out double cm) && cm is > 0.2 and < 30)
            width = new GridLength(cm * TextStyles.PxPerCm);
        else
            return;
        foreach (int i in cols) ctx.Table.Columns[i].Width = width;
        MarkDirty();
    }
}
