using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using GonkNote.Services;

using GonkNote.Core.Platform;

namespace GonkNote.Views;

/// <summary>
/// Tabellenfunktion nach Word-Vorbild (Neubau lt. Nutzer-Vorgaben):
/// Raster-Einfügen, Text↔Tabelle, Schnelltabellen, Zellen teilen, Tabelle teilen,
/// AutoAnpassen, Zellenränder, Sortieren, Formeln sowie ein Kontext-Ribbon-Tab
/// „Tabelle" mit Formatvorlagen (Kopf-/Ergebniszeile, Zeilen-/Spaltenbänder).
/// Grundwerkzeuge (Zeilen/Spalten, Verbinden, Rahmen, Füllung, Spaltenbreite)
/// liegen weiter in TextEditorView.Insert.cs und werden vom Tab mitbenutzt.
/// Technische Grenzen (WPF-FlowDocument, bewusst ausgelassen): nur durchgezogene
/// Zellränder, keine senkrechte Zellen-Ausrichtung/Zeilenhöhe, keine Excel-
/// Einbettung, keine Kopfzeilen-Wiederholung beim Umbruch, kein Textumfluss.
/// </summary>
public partial class TextEditorView
{
    // ==================== Kontext-Tab „Tabelle" ====================

    private bool _tableRibbonSyncing;

    /// <summary>Zeigt/versteckt den Tab je nach Cursor und synchronisiert Stil-Combo + Toggles.</summary>
    private void UpdateTableRibbon()
    {
        if (TabTable == null) return;
        bool inTable = CurrentCell() != null;

        if (!inTable)
        {
            if (TabTable.Visibility != Visibility.Collapsed)
            {
                TabTable.Visibility = Visibility.Collapsed;
                if (TabTable.IsChecked == true) TabStart.IsChecked = true;
            }
            // Tabellen-Sektion der Seitenleiste schließen, wenn die Tabelle verlassen wird
            if (_activeSection == SecTable) CloseSettings_Click(this, new RoutedEventArgs());
            return;
        }

        TabTable.Visibility = Visibility.Visible;
        EnsureTableStyleCombo();

        // Stil/Optionen der aktuellen Tabelle in die Bedienelemente spiegeln
        var state = ReadTableStyleState(CurrentTableContext()?.Table);
        _tableRibbonSyncing = true;
        TableStyleCombo.SelectedIndex = Math.Clamp(state.StyleIndex, 0, TableStyles.Length - 1);
        BtnHeaderRow.IsChecked = state.Header;
        BtnTotalRow.IsChecked = state.Total;
        BtnBandRows.IsChecked = state.BandRows;
        BtnBandCols.IsChecked = state.BandCols;
        _tableRibbonSyncing = false;
    }

    private void OpenTableTab_Click(object s, RoutedEventArgs e)
    {
        UpdateTableRibbon();
        if (TabTable.Visibility == Visibility.Visible) TabTable.IsChecked = true;
    }

    /// <summary>„Design & Rahmen…" im Tab öffnet die Tabellen-Sektion der Seitenleiste.</summary>
    private void OpenTableDesign_Click(object s, RoutedEventArgs e)
    {
        if (CurrentCell() != null) OpenSettings(SecTable);
    }

    // Sammel-Dropdowns im Tab (öffnen ihr ContextMenu unter dem Button)
    private void RowMenu_Click(object s, RoutedEventArgs e) => OpenButtonMenu(s);
    private void ColMenu_Click(object s, RoutedEventArgs e) => OpenButtonMenu(s);
    private void SplitMenu_Click(object s, RoutedEventArgs e) => OpenButtonMenu(s);

    private static void OpenButtonMenu(object s)
    {
        if (s is not Button btn || btn.ContextMenu is not { } menu) return;
        menu.PlacementTarget = btn;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    // ==================== Einfügen: Raster, Dialog, Schnelltabellen ====================

    private bool _tableGridBuilt;

    private void TableMenu_Click(object s, RoutedEventArgs e)
    {
        BuildTableGrid();
        TableGridPopup.IsOpen = true;
    }

    /// <summary>8×10-Hover-Raster wie in Word: Überfahren zeigt „3×4", Klick fügt ein.</summary>
    private void BuildTableGrid()
    {
        if (_tableGridBuilt) return;
        _tableGridBuilt = true;

        for (int r = 1; r <= 8; r++)
            for (int c = 1; c <= 10; c++)
            {
                int rows = r, cols = c;
                var cell = new Border
                {
                    Width = 17, Height = 17, Margin = new Thickness(1),
                    Background = Brushes.Transparent,
                    BorderBrush = (Brush)FindResource("Brush.Border"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                };
                cell.MouseEnter += (_, _) =>
                {
                    TableGridLabel.Text = Loc.T("Msg.TableSize", cols, rows);
                    foreach (Border b in TableGrid.Children)
                    {
                        var (br, bc) = ((int, int))b.Tag;
                        b.Background = br <= rows && bc <= cols
                            ? (Brush)FindResource("Brush.AccentSoft")
                            : Brushes.Transparent;
                    }
                };
                cell.MouseLeftButtonUp += (_, _) =>
                {
                    TableGridPopup.IsOpen = false;
                    InsertEmptyTable(rows, cols);
                };
                cell.Tag = (rows, cols);
                TableGrid.Children.Add(cell);
            }
    }

    private void InsertTableDialog_Click(object s, RoutedEventArgs e)
    {
        TableGridPopup.IsOpen = false;
        var dlg = new TableSizeDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;
        InsertEmptyTable(dlg.Rows, dlg.Cols);
    }

    /// <summary>Markierten Text (Zeilen; Trennung Tab &gt; Semikolon &gt; Komma) in eine Tabelle umwandeln.</summary>
    private void TextToTable_Click(object s, RoutedEventArgs e)
    {
        TableGridPopup.IsOpen = false;
        string text = Editor.Selection.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.SelectTextFirst"),
                DialogSeverity.Information, frage: false);
            return;
        }

        var lines = text.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.TrimEnd('\r')).Where(l => l.Trim().Length > 0).ToList();
        if (lines.Count == 0) return;

        // Trennzeichen erkennen: Tab vor Semikolon vor Komma
        char sep = lines.Any(l => l.Contains('\t')) ? '\t'
            : lines.Any(l => l.Contains(';')) ? ';' : ',';
        var rows = lines.Select(l => l.Split(sep).Select(p => p.Trim()).ToArray()).ToList();
        int cols = Math.Max(1, rows.Max(r => r.Length));

        Editor.Selection.Text = "";   // markierten Text ersetzen
        var table = InsertEmptyTable(rows.Count, cols);
        var trows = table.RowGroups[0].Rows;
        for (int r = 0; r < rows.Count; r++)
            for (int c = 0; c < rows[r].Length && c < trows[r].Cells.Count; c++)
                if (trows[r].Cells[c].Blocks.FirstBlock is Paragraph p)
                    p.Inlines.Add(new Run(rows[r][c]));
        MarkDirty();
    }

    /// <summary>Tabelle in Text umwandeln (Trennzeichen wählbar, Standard Tab).</summary>
    private void TableToText_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        if (PromptDialog.Show(Window.GetWindow(this), "In Text umwandeln",
                "Trennzeichen (Standard: Tabulator):", "\\t") is not { } sepInput)
            return;
        string sep = sepInput.Trim() switch { "\\t" or "" => "\t", var x => x };

        var sb = new System.Text.StringBuilder();
        foreach (var row in ctx.Table.RowGroups.SelectMany(g => g.Rows))
            sb.AppendLine(string.Join(sep, row.Cells.Select(CellText)));

        var para = new Paragraph(new Run(sb.ToString().TrimEnd('\r', '\n')));
        var blocks = ParentBlocksOf(ctx.Table);
        blocks.InsertBefore(ctx.Table, para);
        blocks.Remove(ctx.Table);
        MarkDirty();
    }

    private void QuickTableCalendar_Click(object s, RoutedEventArgs e)
    {
        TableGridPopup.IsOpen = false;
        var now = DateTime.Now;
        var first = new DateTime(now.Year, now.Month, 1);
        int lead = ((int)first.DayOfWeek + 6) % 7;   // Montag = 0
        int days = DateTime.DaysInMonth(now.Year, now.Month);
        int weeks = (lead + days + 6) / 7;

        var table = InsertEmptyTable(weeks + 1, 7);
        var rows = table.RowGroups[0].Rows;
        string[] names = { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };
        for (int c = 0; c < 7; c++)
            if (rows[0].Cells[c].Blocks.FirstBlock is Paragraph p)
            {
                p.Inlines.Add(new Run(names[c]) { FontWeight = FontWeights.SemiBold });
                p.TextAlignment = TextAlignment.Center;
            }
        for (int d = 1; d <= days; d++)
        {
            int idx = lead + d - 1;
            if (rows[1 + idx / 7].Cells[idx % 7].Blocks.FirstBlock is Paragraph p)
            {
                p.Inlines.Add(new Run(d.ToString()));
                p.TextAlignment = TextAlignment.Right;
            }
        }
        ApplyTableStyle(table, 0, header: true, total: false, bandRows: false, bandCols: false);
        MarkDirty();
    }

    private void QuickTableList_Click(object s, RoutedEventArgs e)
    {
        TableGridPopup.IsOpen = false;
        var table = InsertEmptyTable(4, 3);
        var rows = table.RowGroups[0].Rows;
        string[] head = { "Punkt", "Beschreibung", "Status" };
        for (int c = 0; c < 3; c++)
            if (rows[0].Cells[c].Blocks.FirstBlock is Paragraph p)
                p.Inlines.Add(new Run(head[c]) { FontWeight = FontWeights.SemiBold });
        ApplyTableStyle(table, 0, header: true, total: false, bandRows: true, bandCols: false);
        MarkDirty();
    }

    // ==================== Zelle teilen / Tabelle teilen ====================

    /// <summary>
    /// Teilt die aktuelle Zelle in N Spalten (Raster wird dafür verfeinert). Ein
    /// bestehender Zeilenverbund wird zusätzlich wieder in Zeilen aufgeteilt.
    /// </summary>
    private void TableSplitCell_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        if (PromptDialog.Show(Window.GetWindow(this), "Zelle teilen",
                "Anzahl Spalten (2–10):", "2") is not { } input)
            return;
        if (!int.TryParse(input.Trim(), out int n) || n is < 1 or > 10) return;

        var cell = ctx.Cell;

        // Zeilenverbund zuerst auflösen (nutzt die bestehende Aufhebungs-Logik)
        if (cell.RowSpan > 1) TableUnmergeCell_Click(s, e);

        if (n <= 1) return;
        if (cell.ColumnSpan >= n && cell.ColumnSpan % n == 0)
        {
            // Verbundene Zelle: Span gleichmäßig auf n neue Zellen verteilen
            int each = cell.ColumnSpan / n;
            cell.ColumnSpan = each;
            int idx = ctx.Row.Cells.IndexOf(cell);
            for (int k = 1; k < n; k++)
            {
                var extra = NewCell(cell.BorderBrush ?? Brushes.Gray);
                extra.BorderThickness = cell.BorderThickness;
                extra.Padding = cell.Padding;
                extra.ColumnSpan = each;
                ctx.Row.Cells.Insert(idx + k, extra);
            }
        }
        else
        {
            // Raster verfeinern: alle anderen Zellen spannen n-fach, Ziel wird n Zellen
            foreach (var c in ctx.Table.RowGroups.SelectMany(g => g.Rows).SelectMany(r => r.Cells))
                if (c != cell) c.ColumnSpan *= n;

            int oldSpan = cell.ColumnSpan;
            cell.ColumnSpan = oldSpan;   // Ziel behält eine Einheit × alter Span
            int idx = ctx.Row.Cells.IndexOf(cell);
            for (int k = 1; k < n; k++)
            {
                var extra = NewCell(cell.BorderBrush ?? Brushes.Gray);
                extra.BorderThickness = cell.BorderThickness;
                extra.Padding = cell.Padding;
                extra.ColumnSpan = oldSpan;
                ctx.Row.Cells.Insert(idx + k, extra);
            }

            // Spaltenliste ans neue Raster anpassen (Breiten zurück auf Auto)
            ctx.Table.Columns.Clear();
            EnsureColumns(ctx.Table);
        }
        MarkDirty();
    }

    /// <summary>Trennt die Tabelle oberhalb der aktuellen Zeile in zwei Tabellen.</summary>
    private void TableSplitTable_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        int idx = ctx.Group.Rows.IndexOf(ctx.Row);
        if (idx <= 0)
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.SplitNeedsSecondRow"),
                DialogSeverity.Information, frage: false);
            return;
        }
        var pos = GridPositions(ctx.Table);
        if (pos.Any(kv => kv.Value.Row < idx && kv.Value.Row + kv.Value.RowSpan - 1 >= idx))
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.SplitAcrossMerge"),
                DialogSeverity.Information, frage: false);
            return;
        }

        var second = new Table { CellSpacing = 0, Margin = ctx.Table.Margin };
        foreach (var col in ctx.Table.Columns)
            second.Columns.Add(new TableColumn { Width = col.Width });
        var group2 = new TableRowGroup();
        second.RowGroups.Add(group2);
        while (ctx.Group.Rows.Count > idx)
        {
            var row = ctx.Group.Rows[idx];
            ctx.Group.Rows.RemoveAt(idx);
            group2.Rows.Add(row);
        }

        ParentBlocksOf(ctx.Table).InsertAfter(ctx.Table, second);
        MarkDirty();
    }

    // ==================== AutoAnpassen & Zellenränder ====================

    private void AutoFitContent_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        EnsureColumns(ctx.Table);
        foreach (var col in ctx.Table.Columns) col.Width = GridLength.Auto;
        MarkDirty();
    }

    private void AutoFitWindow_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        EnsureColumns(ctx.Table);
        foreach (var col in ctx.Table.Columns) col.Width = new GridLength(1, GridUnitType.Star);
        MarkDirty();
    }

    private void AutoFitFixed_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        if (PromptDialog.Show(Window.GetWindow(this), "Feste Spaltenbreite",
                "Breite je Spalte in cm:", "3") is not { } input)
            return;
        if (!TryParseNum(input, out double cm) || cm is < 0.2 or > 30) return;
        EnsureColumns(ctx.Table);
        foreach (var col in ctx.Table.Columns) col.Width = new GridLength(cm * TextStyles.PxPerCm);
        MarkDirty();
    }

    /// <summary>Innenabstand (Padding) der ausgewählten Zellen.</summary>
    private void TableCellPadding_Click(object s, RoutedEventArgs e)
    {
        var cells = SelectedCells();
        if (cells.Count == 0) return;
        string initial = FormatNum(cells[0].Padding.Left / TextStyles.PxPerCm * 10);   // mm
        if (PromptDialog.Show(Window.GetWindow(this), "Zellenränder",
                "Innenabstand in mm (z. B. 1,6):", initial) is not { } input)
            return;
        if (!TryParseNum(input, out double mm) || mm is < 0 or > 30) return;
        double px = mm / 10 * TextStyles.PxPerCm;
        foreach (var cell in cells) cell.Padding = new Thickness(px, px * 0.6, px, px * 0.6);
        MarkDirty();
    }

    // ==================== Sortieren ====================

    private void TableSort_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        var rows = ctx.Table.RowGroups.SelectMany(g => g.Rows).ToList();
        if (rows.Count < 2) return;
        if (rows.SelectMany(r => r.Cells).Any(c => c.RowSpan > 1))
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.SortWithMerge"),
                DialogSeverity.Information, frage: false);
            return;
        }

        int cols = rows.Max(r => r.Cells.Count);
        var names = Enumerable.Range(0, cols).Select(c =>
        {
            string head = c < rows[0].Cells.Count ? CellText(rows[0].Cells[c]).Trim() : "";
            return head.Length > 0 ? $"Spalte {c + 1} ({Shorten(head, 18)})" : $"Spalte {c + 1}";
        });

        var dlg = new TableSortDialog(names) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        int colIdx = dlg.ColumnIndex;
        var body = dlg.HasHeader ? rows.Skip(1).ToList() : rows;
        object Key(TableRow r)
        {
            string t = colIdx < r.Cells.Count ? CellText(r.Cells[colIdx]).Trim() : "";
            return dlg.SortType switch
            {
                "Zahl" => TryParseNum(t, out double v) ? v : double.MaxValue,
                "Datum" => DateTime.TryParse(t, CultureInfo.CurrentCulture,
                    DateTimeStyles.None, out var d) ? d.Ticks : long.MaxValue,
                _ => t,
            };
        }

        var sorted = body.OrderBy(Key, Comparer<object>.Create((a, b) =>
        {
            int cmp = a switch
            {
                double da when b is double db => da.CompareTo(db),
                long la when b is long lb => la.CompareTo(lb),
                _ => string.Compare(a.ToString(), b.ToString(), StringComparison.CurrentCultureIgnoreCase),
            };
            return cmp;
        })).ToList();
        if (dlg.Descending) sorted.Reverse();

        var group = ctx.Group;
        int start = dlg.HasHeader ? 1 : 0;
        for (int i = group.Rows.Count - 1; i >= start; i--) group.Rows.RemoveAt(i);
        foreach (var row in sorted) group.Rows.Add(row);
        MarkDirty();
    }

    private static string Shorten(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    // ==================== Formeln ====================

    /// <summary>
    /// Einfache Berechnungen wie in Word: =SUMME(ABOVE), =MITTELWERT(LEFT), =MIN/MAX/
    /// ANZAHL/PRODUKT über ABOVE/BELOW/LEFT/RIGHT. Das Ergebnis wird als Text in die
    /// aktuelle Zelle geschrieben (einmalige Berechnung, keine Live-Formel).
    /// </summary>
    private void TableFormula_Click(object s, RoutedEventArgs e)
    {
        if (CurrentTableContext() is not { } ctx) return;
        if (PromptDialog.Show(Window.GetWindow(this), "Formel",
                "z. B. =SUMME(ABOVE), =MITTELWERT(LEFT), =MAX(RIGHT):", "=SUMME(ABOVE)") is not { } input)
            return;

        var m = System.Text.RegularExpressions.Regex.Match(input.Trim().ToUpperInvariant(),
            @"^=?\s*(SUMME|SUM|MITTELWERT|AVERAGE|MIN|MAX|ANZAHL|COUNT|PRODUKT|PRODUCT)\s*\(\s*(ABOVE|BELOW|LEFT|RIGHT)\s*\)$");
        if (!m.Success)
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.FormulaUnknown"),
                DialogSeverity.Information, frage: false);
            return;
        }

        var pos = GridPositions(ctx.Table);
        var (row, col, _, _) = pos[ctx.Cell];

        // Werte im gewählten Bereich einsammeln (bis zur ersten leeren/nicht-numerischen Zelle)
        var values = new List<double>();
        foreach (var (cell, p) in pos)
        {
            if (cell == ctx.Cell) continue;
            bool hit = m.Groups[2].Value switch
            {
                "ABOVE" => p.Col <= col && col < p.Col + p.ColSpan && p.Row < row,
                "BELOW" => p.Col <= col && col < p.Col + p.ColSpan && p.Row > row,
                "LEFT" => p.Row <= row && row < p.Row + p.RowSpan && p.Col < col,
                _ => p.Row <= row && row < p.Row + p.RowSpan && p.Col > col,
            };
            if (hit && TryParseNum(CellText(cell).Trim(), out double v)) values.Add(v);
        }

        if (values.Count == 0)
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.NoNumbers"),
                DialogSeverity.Information, frage: false);
            return;
        }

        double result = m.Groups[1].Value switch
        {
            "SUMME" or "SUM" => values.Sum(),
            "MITTELWERT" or "AVERAGE" => values.Average(),
            "MIN" => values.Min(),
            "MAX" => values.Max(),
            "ANZAHL" or "COUNT" => values.Count,
            _ => values.Aggregate(1.0, (a, v) => a * v),
        };

        if (ctx.Cell.Blocks.FirstBlock is Paragraph target)
            target.Inlines.Add(new Run(FormatNum(result)));
        else
            ctx.Cell.Blocks.Add(new Paragraph(new Run(FormatNum(result))));
        MarkDirty();
    }

    // ==================== Formatvorlagen (Entwurf) ====================

    /// <summary>Farbschema einer Tabellen-Formatvorlage (Key = Schlüssel des Anzeigenamens).</summary>
    private sealed record TableStyleDef(string Key, Color? HeaderBg, Color? HeaderFg,
        Color? Band, Color BorderColor, double BorderWidth);

    private static readonly TableStyleDef[] TableStyles =
    {
        new("TStyle.Plain", null, null, null, Color.FromRgb(0x9A, 0xA7, 0xBD), 1),
        new("TStyle.Blue", Color.FromRgb(0x25, 0x63, 0xEB), Colors.White, Color.FromRgb(0xDB, 0xEA, 0xFE), Color.FromRgb(0x93, 0xB8, 0xF5), 1),
        new("TStyle.Teal", Color.FromRgb(0x0F, 0x76, 0x6E), Colors.White, Color.FromRgb(0xCC, 0xFB, 0xF1), Color.FromRgb(0x5E, 0xEA, 0xD4), 1),
        new("TStyle.Purple", Color.FromRgb(0x7C, 0x3A, 0xED), Colors.White, Color.FromRgb(0xED, 0xE9, 0xFE), Color.FromRgb(0xC4, 0xB5, 0xFD), 1),
        new("TStyle.Gray", Color.FromRgb(0x47, 0x55, 0x69), Colors.White, Color.FromRgb(0xE2, 0xE8, 0xF0), Color.FromRgb(0x94, 0xA3, 0xB8), 1),
        new("TStyle.Warm", Color.FromRgb(0xB4, 0x53, 0x09), Colors.White, Color.FromRgb(0xFE, 0xF3, 0xC7), Color.FromRgb(0xFC, 0xD3, 0x4D), 1),
        new("TStyle.Borderless", null, null, Color.FromRgb(0xEE, 0xF2, 0xF8), Colors.Transparent, 0),
    };

    /// <summary>Füllt die Auswahl der Formatvorlagen (auch neu nach einem Sprachwechsel).</summary>
    private void EnsureTableStyleCombo()
    {
        if (TableStyleCombo.Items.Count == TableStyles.Length &&
            (string)TableStyleCombo.Items[0]! == Loc.T(TableStyles[0].Key))
            return;

        int selected = Math.Max(0, TableStyleCombo.SelectedIndex);
        TableStyleCombo.Items.Clear();
        foreach (var st in TableStyles) TableStyleCombo.Items.Add(Loc.T(st.Key));
        TableStyleCombo.SelectedIndex = selected;
    }

    private void TableStyle_Changed(object s, SelectionChangedEventArgs e) => ReapplyTableStyle();
    private void TableBanding_Click(object s, RoutedEventArgs e) => ReapplyTableStyle();

    private void ReapplyTableStyle()
    {
        if (_tableRibbonSyncing || CurrentTableContext() is not { } ctx) return;
        ApplyTableStyle(ctx.Table, Math.Max(0, TableStyleCombo.SelectedIndex),
            BtnHeaderRow.IsChecked == true, BtnTotalRow.IsChecked == true,
            BtnBandRows.IsChecked == true, BtnBandCols.IsChecked == true);
        MarkDirty();
    }

    /// <summary>
    /// Wendet Formatvorlage + Optionen auf die ganze Tabelle an (Füllungen/Rahmen/
    /// Kopfzeilen-Schrift) und merkt sie sich im Table.Tag (überlebt Speichern/Laden).
    /// </summary>
    private void ApplyTableStyle(Table table, int styleIndex, bool header, bool total, bool bandRows, bool bandCols)
    {
        var st = TableStyles[Math.Clamp(styleIndex, 0, TableStyles.Length - 1)];
        var pos = GridPositions(table);
        var rows = table.RowGroups.SelectMany(g => g.Rows).ToList();
        int lastRow = rows.Count - 1;

        var borderBrush = new SolidColorBrush(st.BorderColor);
        foreach (var (cell, p) in pos)
        {
            bool isHeader = header && p.Row == 0;
            bool isTotal = total && p.Row + p.RowSpan - 1 == lastRow && !isHeader;

            cell.BorderBrush = borderBrush;
            cell.BorderThickness = new Thickness(st.BorderWidth);

            if (isHeader && st.HeaderBg is { } hb)
            {
                cell.Background = new SolidColorBrush(hb);
                cell.Foreground = new SolidColorBrush(st.HeaderFg ?? Colors.White);
                cell.FontWeight = FontWeights.SemiBold;
            }
            else if (isHeader)
            {
                cell.Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xF8));
                cell.ClearValue(TextElement.ForegroundProperty);
                cell.FontWeight = FontWeights.SemiBold;
            }
            else if (isTotal)
            {
                cell.Background = new SolidColorBrush(st.Band ?? Color.FromRgb(0xEE, 0xF2, 0xF8));
                cell.ClearValue(TextElement.ForegroundProperty);
                cell.FontWeight = FontWeights.SemiBold;
                cell.BorderThickness = new Thickness(st.BorderWidth, Math.Max(1.5, st.BorderWidth * 2),
                    st.BorderWidth, st.BorderWidth);
            }
            else
            {
                bool banded =
                    (bandRows && st.Band != null && (p.Row - (header ? 1 : 0)) % 2 == 1) ||
                    (bandCols && st.Band != null && p.Col % 2 == 1);
                cell.Background = banded ? new SolidColorBrush(st.Band!.Value) : null;
                cell.ClearValue(TextElement.ForegroundProperty);
                cell.ClearValue(TextElement.FontWeightProperty);
            }
        }

        table.Tag = $"gonkstyle:{styleIndex}:{(header ? 1 : 0)}:{(total ? 1 : 0)}:{(bandRows ? 1 : 0)}:{(bandCols ? 1 : 0)}";
    }

    private static (int StyleIndex, bool Header, bool Total, bool BandRows, bool BandCols) ReadTableStyleState(Table? table)
    {
        if (table?.Tag is string tag && tag.StartsWith("gonkstyle:"))
        {
            var p = tag.Split(':');
            if (p.Length == 6 && int.TryParse(p[1], out int idx))
                return (idx, p[2] == "1", p[3] == "1", p[4] == "1", p[5] == "1");
        }
        return (0, false, false, false, false);
    }

    // ==================== Helfer ====================

    private static string CellText(TableCell cell) =>
        string.Join(" ", cell.Blocks.OfType<Paragraph>()
            .Select(p => new TextRange(p.ContentStart, p.ContentEnd).Text.Trim()));

    private BlockCollection ParentBlocksOf(Table table) => table.Parent switch
    {
        FlowDocument doc => doc.Blocks,
        TableCell cell => cell.Blocks,
        ListItem li => li.Blocks,
        Section sec => sec.Blocks,
        _ => Editor.Document.Blocks,
    };
}
