using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GonkNote.Services;
using GonkNote.ViewModels;

namespace GonkNote;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private Point _dragStart;
    private TreeItemViewModel? _dragCandidate;
    private TreeItemViewModel? _clickOpenCandidate;

    private bool _sidebarVisible = true;
    private GridLength _sidebarWidth = new(260);

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel(App.Db);
        DataContext = _vm;
        Closing += (_, _) => _vm.SaveAll();
        Closing += (_, _) => ThemeService.ThemeChanged -= ApplyTitleBarTheme;
        PreviewKeyDown += Window_PreviewKeyDown;
        StateChanged += (_, _) => ApplyMaximizedChrome();
        ThemeService.ThemeChanged += ApplyTitleBarTheme;

        if (App.Db.GetSetting("sidebar") == "0") SetSidebarVisible(false);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Handle existiert jetzt: MinMax-Hook (korrekte Maximier-Größe ohne Titelleiste)
        // einhängen und Titelleiste passend zum Theme/Fensterzustand setzen.
        System.Windows.Interop.HwndSource.FromHwnd(
            new System.Windows.Interop.WindowInteropHelper(this).Handle)?.AddHook(WndProc);
        ApplyTitleBarTheme();
        ApplyMaximizedChrome();
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WindowBounds.WmGetMinMaxInfo)
        {
            WindowBounds.AdjustMaximizedBounds(hwnd, lParam);
            // Als erledigt markieren, sonst überschreibt WPFs Standardbehandlung die Werte
            // wieder mit dem Vollmonitor + ~7-px-Überstand (randlose Fenster).
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>Native Titelleiste dunkel/hell entsprechend dem aktuellen App-Theme.</summary>
    private void ApplyTitleBarTheme() =>
        TitleBarTheme.Apply(this, ThemeService.Current == AppTheme.Dark);

    /// <summary>
    /// Blendet die native Titelleiste aus, sobald das Fenster maximiert („in Groß") ist,
    /// und bringt sie im Normalfenster zurück. Die Maximier-Größe ist per
    /// <see cref="WindowBounds"/> auf den Arbeitsbereich begrenzt – Taskleiste bleibt
    /// sichtbar, nichts ragt über den Rand. Zurück ins Fenster: Doppelklick auf die
    /// Menüleiste (siehe <see cref="TopBar_MouseLeftButtonDown"/>).
    /// </summary>
    private void ApplyMaximizedChrome()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowStyle = WindowStyle.None;
        }
        else if (WindowState == WindowState.Normal)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ApplyTitleBarTheme(); // Farbe nach dem Stilwechsel neu setzen
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SetSidebarVisible(!_sidebarVisible);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Doppelklick auf die Menüleiste maximiert bzw. verkleinert das Fenster (wie eine
    /// Titelleiste). So lässt sich das maximierte, titelleistenlose Fenster wieder
    /// verkleinern. Klicks auf Menüs/Buttons werden dabei ausgenommen.
    /// </summary>
    private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2) return;
        var src = e.OriginalSource as DependencyObject;
        if (src.FindAncestor<MenuItem>() != null ||
            src.FindAncestor<System.Windows.Controls.Primitives.ButtonBase>() != null)
            return;
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        e.Handled = true;
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e) =>
        SetSidebarVisible(!_sidebarVisible);

    private void SetSidebarVisible(bool visible)
    {
        if (visible == _sidebarVisible) return;
        _sidebarVisible = visible;

        if (visible)
        {
            Sidebar.Visibility = Visibility.Visible;
            Splitter.Visibility = Visibility.Visible;
            SidebarCol.MinWidth = 180;
            SidebarCol.Width = _sidebarWidth;
        }
        else
        {
            _sidebarWidth = SidebarCol.Width;
            Sidebar.Visibility = Visibility.Collapsed;
            Splitter.Visibility = Visibility.Collapsed;
            SidebarCol.MinWidth = 0;
            SidebarCol.Width = new GridLength(0);
        }
        App.Db.SetSetting("sidebar", visible ? "1" : "0");
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new Views.AboutDialog { Owner = this }.ShowDialog();
    }

    // ==================== Baum: Auswahl & Öffnen ====================

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _vm.SelectedTreeItem = e.NewValue as TreeItemViewModel;
    }

    private void Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Doppelklick benennt um (Einfachklick öffnet bereits)
        if (e.OriginalSource is TextBox) return;
        if (FindTreeItem(e.OriginalSource) is { } vm)
        {
            _clickOpenCandidate = null;
            vm.IsRenaming = true;
            e.Handled = true;
        }
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
        var hit = e.OriginalSource is TextBox ? null : FindTreeItem(e.OriginalSource);
        _dragCandidate = hit;
        // Einfachklick öffnet (beim Loslassen, wenn nicht gezogen und kein Doppelklick)
        _clickOpenCandidate = e.ClickCount == 1 ? hit : null;
    }

    private void Tree_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Einfachklick öffnet Dokumente; Ordner werden nur ausgewählt (Aufklappen per Pfeil).
        // Ziehen/Doppelklick haben den Kandidaten bereits geleert.
        var vm = _clickOpenCandidate;
        _clickOpenCandidate = null;
        if (vm != null && !vm.IsRenaming && !vm.IsFolder && e.OriginalSource is not TextBox)
            _vm.OpenItem(vm);
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
        _clickOpenCandidate = null; // Ziehen ist kein Klick
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

    // ==================== Symbolfarbe ====================

    private void IconColor_Click(object sender, RoutedEventArgs e)
    {
        var tag = (string)((MenuItem)sender).Tag;
        _vm.SetIconColor(_vm.SelectedTreeItem, string.IsNullOrEmpty(tag) ? null : tag);
    }

    private void IconColorCustom_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedTreeItem is not { } item) return;

        var initial = Colors.Teal;
        if (item.Item.IconColor is { } hex)
        {
            try { initial = (Color)ColorConverter.ConvertFromString(hex); } catch { }
        }

        if (Views.ColorPickerDialog.Pick(this, initial, allowAlpha: false) is { } c)
            _vm.SetIconColor(item, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
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

    /// <summary>Sucht aufwärts im Visual Tree nach dem nächsten Vorfahren vom Typ T.</summary>
    public static T? FindAncestor<T>(this DependencyObject? source) where T : DependencyObject
    {
        while (source != null && source is not T)
            source = VisualTreeHelper.GetParent(source);
        return source as T;
    }
}
