using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GonkNote.ViewModels;

namespace GonkNote;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private Point _dragStart;
    private TreeItemViewModel? _dragCandidate;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel(App.Db);
        DataContext = _vm;
        Closing += (_, _) => _vm.SaveAll();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "Gonk Note 0.1 – Phase 1\n\n" +
            "Offline-Notizen, Whiteboards und Textdokumente.\n" +
            "Daten liegen lokal unter %APPDATA%\\GonkNote.",
            "Über Gonk Note", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ==================== Baum: Auswahl & Öffnen ====================

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _vm.SelectedTreeItem = e.NewValue as TreeItemViewModel;
    }

    private void Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindTreeItem(e.OriginalSource) is { } vm)
            _vm.OpenItem(vm);
    }

    private void Tree_KeyDown(object sender, KeyEventArgs e)
    {
        if (_vm.SelectedTreeItem is not { } vm) return;
        // Nicht während des Umbenennens reagieren
        if (e.OriginalSource is TextBox) return;

        switch (e.Key)
        {
            case Key.F2:
                vm.IsRenaming = true;
                e.Handled = true;
                break;
            case Key.Delete:
                _vm.DeleteCommand.Execute(vm);
                e.Handled = true;
                break;
            case Key.Enter:
                _vm.OpenItem(vm);
                e.Handled = true;
                break;
        }
    }

    private void Tree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Rechtsklick wählt den Eintrag unter dem Zeiger aus
        if (FindContainer(e.OriginalSource) is { } container)
        {
            container.IsSelected = true;
        }
        else
        {
            _vm.SelectedTreeItem = null;
            if (Tree.SelectedItem is TreeItemViewModel sel)
            {
                var c = (TreeViewItem?)Tree.ItemContainerGenerator.ContainerFromItem(sel);
                if (c != null) c.IsSelected = false;
            }
        }
    }

    // ==================== Baum: Drag & Drop ====================

    private void Tree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(Tree);
        _dragCandidate = e.OriginalSource is TextBox ? null : FindTreeItem(e.OriginalSource);
    }

    private void Tree_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragCandidate == null || e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(Tree);
        if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var item = _dragCandidate;
        _dragCandidate = null;
        DragDrop.DoDragDrop(Tree, new DataObject(typeof(TreeItemViewModel), item),
            DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void Tree_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(TreeItemViewModel)))
        {
            e.Effects = DragDropEffects.None;
        }
        else
        {
            e.Effects = e.KeyStates.HasFlag(DragDropKeyStates.ControlKey)
                ? DragDropEffects.Copy
                : DragDropEffects.Move;
        }
        e.Handled = true;
    }

    private void Tree_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(TreeItemViewModel)) is not TreeItemViewModel source) return;

        var target = FindTreeItem(e.OriginalSource);
        // Ziel ist der Ordner selbst oder der Ordner des getroffenen Dokuments; leere Fläche = Wurzel
        var targetFolder = target == null ? null
            : target.IsFolder ? target
            : _vm.FindParent(target);

        bool copy = e.KeyStates.HasFlag(DragDropKeyStates.ControlKey);
        _vm.MoveItem(source, targetFolder, copy);
        e.Handled = true;
    }

    // ==================== Umbenennen ====================

    private void RenameBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox box && box.IsVisible)
        {
            box.Dispatcher.BeginInvoke(() =>
            {
                box.Focus();
                box.SelectAll();
            });
        }
    }

    private void RenameBox_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (((TextBox)sender).DataContext is TreeItemViewModel vm && vm.IsRenaming)
            _vm.CommitRename(vm);
    }

    private void RenameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Escape)) return;
        if (((TextBox)sender).DataContext is TreeItemViewModel vm)
        {
            if (e.Key == Key.Escape && vm.NameBeforeRename != null)
                vm.Name = vm.NameBeforeRename;
            _vm.CommitRename(vm);
        }
        Tree.Focus();
        e.Handled = true;
    }

    // ==================== Hilfen ====================

    private static TreeItemViewModel? FindTreeItem(object? source) =>
        (source as DependencyObject).FindDataContext<TreeItemViewModel>();

    private TreeViewItem? FindContainer(object? source)
    {
        var d = source as DependencyObject;
        while (d != null && d is not TreeViewItem)
            d = VisualTreeHelper.GetParent(d);
        return d as TreeViewItem;
    }
}

internal static class VisualTreeExtensions
{
    /// <summary>Sucht aufwärts im Visual Tree nach einem Element mit passendem DataContext.</summary>
    public static T? FindDataContext<T>(this DependencyObject? source) where T : class
    {
        while (source != null)
        {
            if (source is FrameworkElement { DataContext: T ctx }) return ctx;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }
}
