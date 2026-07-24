using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GonkNote.Avalonia;

public partial class MainWindow : Window
{
    /// <summary>Breite des linken Baum-Panels (siehe MainWindow.axaml, Border Dock=Left).</summary>
    private const double SidebarWidth = 300;

    private ShellViewModel Vm => (ShellViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel(ShellViewModel.DefaultDbPath);

        // Umbenennen: Doppelklick im Baum startet die Inline-Bearbeitung (wie in der WPF-App).
        Tree.DoubleTapped += Tree_DoubleTapped;
        // Enter übernimmt, Escape verwirft; Fokusverlust übernimmt ebenfalls.
        Tree.AddHandler(KeyDownEvent, Tree_KeyDown, RoutingStrategies.Tunnel);
        Tree.AddHandler(LostFocusEvent, Tree_LostFocus, RoutingStrategies.Bubble);

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
        vm.BeginRename();
        // Fokus in die eingeblendete TextBox legen, damit sofort getippt werden kann.
        Dispatcher.UIThread.Post(() => FindRenameBox()?.Focus());
        e.Handled = true;
    }

    private void Tree_KeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm.Selected is not { IsRenaming: true } vm) return;
        if (e.Key == Key.Enter) { Vm.CommitRename(vm); e.Handled = true; }
        else if (e.Key == Key.Escape) { Vm.CancelRename(vm); e.Handled = true; }
    }

    private void Tree_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (Vm.Selected is { IsRenaming: true } vm) Vm.CommitRename(vm);
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
