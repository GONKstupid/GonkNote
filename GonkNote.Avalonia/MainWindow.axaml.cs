using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GonkNote.Models;

namespace GonkNote.Avalonia;

public partial class MainWindow : Window
{
    /// <summary>Breite des linken Baum-Panels (siehe MainWindow.axaml, Border Dock=Left).</summary>
    private const double SidebarWidth = 300;

    private ShellViewModel Vm => (ShellViewModel)DataContext!;

    /// <summary>Hatte die Umbenennen-TextBox schon echten Fokus? (Schutz gegen Vorzeitig-Commit.)</summary>
    private bool _renameHadFocus;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel(ShellViewModel.DefaultDbPath);

        // Umbenennen: Doppelklick im Baum startet die Inline-Bearbeitung (wie in der WPF-App).
        // Muss im TUNNEL laufen: sonst hat der TreeViewItem den Doppelklick schon verarbeitet
        // und klappt Ordner auf/zu, statt sie umzubenennen.
        // Enter/Escape/Fokusverlust werden an der TextBox selbst behandelt (siehe XAML).
        Tree.AddHandler(PointerPressedEvent, Tree_PointerPressed, RoutingStrategies.Tunnel);

        ThemeToggle.Click += (_, _) =>
            RequestedThemeVariant = RequestedThemeVariant == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;

        OpenEditor.Click += (_, _) => new EditorPrototypeWindow().Show(this);
        OpenProbe.Click += (_, _) => new MarkdownProbe().Show(this);

        // Jeder fertige Strich / Radier-Zug: Undo-Eintrag + sofort in die LiteDB.
        Board.StrokeCompleted += (_, stroke) => Vm.OnStrokeDrawn(stroke);
        Board.ElementsErased += (_, removed) => Vm.OnElementsErased(removed);
        Board.ElementAdded += (_, el) => Vm.OnElementAdded(el);
        Board.SelectionMoved += (_, m) => Vm.OnSelectionMoved(m.Els, m.Dx, m.Dy);
        Board.SelectionScaled += (_, s) => Vm.OnSelectionScaled(s.Els, s.Factor, s.Px, s.Py);
        Board.ElementRotated += (_, r) => Vm.OnElementRotated(r.El, r.OldDeg, r.NewDeg);
        Board.UndoRequested += (_, _) => Undo_Click(this, new RoutedEventArgs());
        Board.TextRequested += (_, txt) => OnTextRequested(txt);

        // Tastenkürzel des Whiteboards (Entf, Strg+C/V/D, Strg+Z/Y).
        AddHandler(KeyDownEvent, Board_KeyDown, RoutingStrategies.Bubble);

        // Bilder per Drag&Drop einfügen.
        AddHandler(DragDrop.DragOverEvent, (_, e) =>
            e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None);
        AddHandler(DragDrop.DropEvent, Board_Drop);

        // Workaround gegen den Fill-Panel-Measure-Quirk (§9.5): dem Inhaltsbereich eine
        // explizite Breite geben (= Fensterbreite − Seitenleiste), damit Umbruch/Zentrierung
        // greifen. Die Arrange-Breite stimmt ohnehin; nur der Measure braucht die feste Breite.
        SizeChanged += (_, _) => UpdateContentWidth();
        Loaded += (_, _) => UpdateContentWidth();
    }

    private void UpdateContentWidth() =>
        ContentHost.Width = Math.Max(0, ClientSize.Width - SidebarWidth - 1); // -1 = Trennlinie

    // ---- Inline-Umbenennen im Ordnerbaum ------------------------------------------------

    private void Tree_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2) return;

        // Doppelklick auf den Aufklapp-Pfeil soll weiterhin auf-/zuklappen.
        for (var v = e.Source as Visual; v is not null; v = v.GetVisualParent())
        {
            if (v is ToggleButton) return;
            if (v is TreeViewItem) break;
        }

        if (ItemFrom(e.Source) is not { } vm) return;
        StartRename(vm);
        e.Handled = true;   // verhindert das Auf-/Zuklappen durch den TreeViewItem
    }

    /// <summary>Startet die Inline-Umbenennung und setzt den Fokus in die eingeblendete TextBox.</summary>
    private void StartRename(TreeItemVM vm)
    {
        _renameHadFocus = false;
        vm.BeginRename();

        // Fokus erst setzen, wenn die TreeView ihre eigene Fokuslogik abgeschlossen hat —
        // sonst holt sich der TreeViewItem den Fokus sofort zurück und die Bearbeitung endet.
        Dispatcher.UIThread.Post(() =>
        {
            if (FindRenameBox() is not { } box) return;
            box.Focus();
            box.SelectAll();
        }, DispatcherPriority.Background);
    }

    private void RenameBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: TreeItemVM vm }) return;
        if (e.Key == Key.Enter) { Vm.CommitRename(vm); e.Handled = true; }
        else if (e.Key == Key.Escape) { Vm.CancelRename(vm); e.Handled = true; }
    }

    private void RenameBox_GotFocus(object? sender, GotFocusEventArgs e) => _renameHadFocus = true;

    private void RenameBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        // Erst übernehmen, wenn die Box zuvor wirklich den Fokus hatte — sonst würde das
        // anfängliche Fokus-Geplänkel der TreeView die Bearbeitung sofort wieder beenden.
        if (!_renameHadFocus) return;
        _renameHadFocus = false;
        if (sender is TextBox { DataContext: TreeItemVM vm } && vm.IsRenaming)
            Vm.CommitRename(vm);
    }

    // ---- Whiteboard (Schritt 4b) ---------------------------------------------------------

    private void Tool_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag } btn) return;
        if (!Enum.TryParse<ToolType>(tag, out var tool)) return;

        Board.Tool = tool;

        // Aktives Werkzeug hervorheben (alle Geschwister zurücksetzen).
        if (btn.Parent is Panel row)
            foreach (var child in row.Children)
                if (child is Button b && b.Tag is string t && Enum.TryParse<ToolType>(t, out _))
                    b.Classes.Set("active", ReferenceEquals(b, btn));
    }

    private void Ink_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hex }) Board.InkColor = hex;
    }

    private void Width_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } && double.TryParse(tag, out double w))
            Board.InkWidth = w;
    }

    private void Undo_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm.UndoLast()) Board.InvalidateVisual();
    }

    private void Redo_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm.RedoLast()) Board.InvalidateVisual();
    }

    private void ZoomReset_Click(object? sender, RoutedEventArgs e)
    {
        Board.Zoom = 1.0;
        Board.PanX = 0;
        Board.PanY = 0;
    }

    private void ShapePicker_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (Board is null) return;
        Board.Shape = ShapePicker.SelectedIndex switch
        {
            1 => ShapeKind.Ellipse,
            2 => ShapeKind.Line,
            3 => ShapeKind.Arrow,
            4 => ShapeKind.Triangle,
            _ => ShapeKind.Rectangle,
        };
    }

    private void DeleteSel_Click(object? sender, RoutedEventArgs e) => DeleteSelection();

    private void DeleteSelection()
    {
        var removed = Board.DeleteSelection();
        if (removed.Count > 0) Vm.OnSelectionDeleted(removed);
    }

    private void PrevPage_Click(object? sender, RoutedEventArgs e) { Vm.PrevPage(); Board.InvalidateVisual(); }
    private void NextPage_Click(object? sender, RoutedEventArgs e) { Vm.NextPage(); Board.InvalidateVisual(); }
    private void AddPage_Click(object? sender, RoutedEventArgs e) { Vm.AddPage(); Board.InvalidateVisual(); }

    /// <summary>Tastenkürzel im Whiteboard: Entf, Strg+C/V/D.</summary>
    private void Board_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!Board.IsVisible) return;
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (e.Key == Key.Delete && Board.HasSelection)
        {
            DeleteSelection();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.C)
        {
            Board.CopySelection();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.X)
        {
            CutSelection();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.V)
        {
            var added = Board.Paste();
            if (added.Count > 0) Vm.OnElementsAdded(added);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.D)
        {
            var added = Board.DuplicateSelection();
            if (added.Count > 0) Vm.OnElementsAdded(added);
            e.Handled = true;
        }
    }

    /// <summary>Bilder per Drag&amp;Drop auf das Whiteboard legen.</summary>
    private void Board_Drop(object? sender, DragEventArgs e)
    {
        if (!Board.IsVisible || !e.Data.Contains(DataFormats.Files)) return;

        foreach (var item in e.Data.GetFiles() ?? Enumerable.Empty<IStorageItem>())
        {
            if (item is not IStorageFile file) continue;
            string path = file.Path.LocalPath;
            if (!IsImagePath(path)) continue;
            InsertImageFile(path);
        }
        e.Handled = true;
    }

    private static bool IsImagePath(string path)
    {
        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp";
    }

    private void InsertImageFile(string path)
    {
        try
        {
            var data = System.IO.File.ReadAllBytes(path);
            var img = Board.InsertImage(data, Board.ViewCenter());
            if (img is null) return;

            Vm.OnElementAdded(img);
            Board.Tool = ToolType.Move;   // direkt verschieb-/skalierbar
            Board.Focus();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bild-Import fehlgeschlagen: {ex.Message}");
        }
    }

    /// <summary>Bild über den Dateidialog einfügen.</summary>
    private async void InsertImage_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Bild einfügen",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Bilder")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" },
                },
            },
        });

        foreach (var f in files) InsertImageFile(f.Path.LocalPath);
    }

    private void Copy_Click(object? sender, RoutedEventArgs e) => Board.CopySelection();

    private void Cut_Click(object? sender, RoutedEventArgs e) => CutSelection();

    private void CutSelection()
    {
        var removed = Board.CutSelection();
        if (removed.Count > 0) Vm.OnSelectionDeleted(removed);
    }

    private void Duplicate_Click(object? sender, RoutedEventArgs e)
    {
        var added = Board.DuplicateSelection();
        if (added.Count > 0) Vm.OnElementsAdded(added);
    }

    private void Paste_Click(object? sender, RoutedEventArgs e)
    {
        var added = Board.Paste();
        if (added.Count > 0) Vm.OnElementsAdded(added);
    }

    /// <summary>Fragt den Text ab und fügt das Text-Element ein.</summary>
    private async void OnTextRequested(GonkNote.Models.TextElement txt)
    {
        string? text = await TextPrompt.ShowAsync(this, "Text einfügen", "");
        if (string.IsNullOrWhiteSpace(text)) return;
        if (Vm.CurrentPage is not { } page) return;

        txt.Text = text;
        page.Elements.Add(txt);
        Vm.OnElementAdded(txt);
        Board.InvalidateVisual();
    }

    // ---- Anlegen / Kontextmenü ----------------------------------------------------------

    private void New_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string tag }) return;
        if (!Enum.TryParse<ItemKind>(tag, out var kind)) return;

        var vm = Vm.CreateItem(kind);
        Vm.Selected = vm;                       // neues Element gleich anzeigen
        StartRename(vm);                        // Standardname gleich ersetzbar
    }

    /// <summary>Das <see cref="TreeItemVM"/> hinter einem Kontextmenü-Eintrag.</summary>
    private static TreeItemVM? CtxItem(object? sender) =>
        (sender as MenuItem)?.DataContext as TreeItemVM;

    private void Ctx_Rename_Click(object? sender, RoutedEventArgs e)
    {
        if (CtxItem(sender) is { } vm) StartRename(vm);
    }

    private void Ctx_Favorite_Click(object? sender, RoutedEventArgs e)
    {
        if (CtxItem(sender) is { } vm) Vm.ToggleFavorite(vm);
    }

    private void Ctx_Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (CtxItem(sender) is { } vm) Vm.DeleteItem(vm);
    }

    /// <summary>Findet das <see cref="TreeItemVM"/> zu einem angeklickten Visual.</summary>
    private static TreeItemVM? ItemFrom(object? source)
    {
        for (var v = source as Visual; v is not null; v = v.GetVisualParent())
            if (v is TreeViewItem { DataContext: TreeItemVM vm }) return vm;
        return null;
    }

    /// <summary>Die aktuell eingeblendete Umbenennen-TextBox (nur eine gleichzeitig sichtbar).</summary>
    private TextBox? FindRenameBox()
    {
        foreach (var tb in Tree.GetVisualDescendants().OfType<TextBox>())
            if (tb.Name == "RenameBox" && tb.IsVisible) return tb;
        return null;
    }
}
