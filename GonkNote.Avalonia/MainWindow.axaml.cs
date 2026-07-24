using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
        // Enter/Escape/Fokusverlust werden an der TextBox selbst behandelt (siehe XAML) —
        // ein Handler am ganzen Baum würde bei fremden Fokuswechseln vorzeitig übernehmen.
        Tree.DoubleTapped += Tree_DoubleTapped;

        ThemeToggle.Click += (_, _) =>
            RequestedThemeVariant = RequestedThemeVariant == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;

        OpenEditor.Click += (_, _) => new EditorPrototypeWindow().Show(this);
        OpenProbe.Click += (_, _) => new MarkdownProbe().Show(this);

        // Workaround gegen den Fill-Panel-Measure-Quirk (§9.5): dem Inhaltsbereich eine
        // explizite Breite geben (= Fensterbreite − Seitenleiste), damit Umbruch/Zentrierung
        // greifen. Die Arrange-Breite stimmt ohnehin; nur der Measure braucht die feste Breite.
        SizeChanged += (_, _) => UpdateContentWidth();
        Loaded += (_, _) => UpdateContentWidth();
    }

    private void UpdateContentWidth() =>
        ContentHost.Width = Math.Max(0, ClientSize.Width - SidebarWidth - 1); // -1 = Trennlinie

    // ---- Inline-Umbenennen im Ordnerbaum ------------------------------------------------

    private void Tree_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ItemFrom(e.Source) is not { } vm) return;
        _renameHadFocus = false;
        vm.BeginRename();
        e.Handled = true;

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

    // ---- Anlegen / Kontextmenü ----------------------------------------------------------

    private void New_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string tag }) return;
        if (!Enum.TryParse<ItemKind>(tag, out var kind)) return;

        var vm = Vm.CreateItem(kind);
        Vm.Selected = vm;                       // neues Element gleich anzeigen
        // Direkt in die Umbenennung springen, damit der Standardname ersetzt werden kann.
        _renameHadFocus = false;
        vm.BeginRename();
        Dispatcher.UIThread.Post(() =>
        {
            if (FindRenameBox() is not { } box) return;
            box.Focus();
            box.SelectAll();
        }, DispatcherPriority.Background);
    }

    /// <summary>Das <see cref="TreeItemVM"/> hinter einem Kontextmenü-Eintrag.</summary>
    private static TreeItemVM? CtxItem(object? sender) =>
        (sender as MenuItem)?.DataContext as TreeItemVM;

    private void Ctx_Rename_Click(object? sender, RoutedEventArgs e)
    {
        if (CtxItem(sender) is not { } vm) return;
        _renameHadFocus = false;
        vm.BeginRename();
        Dispatcher.UIThread.Post(() =>
        {
            if (FindRenameBox() is not { } box) return;
            box.Focus();
            box.SelectAll();
        }, DispatcherPriority.Background);
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
