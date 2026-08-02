using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GonkNote.Core.Models;
using GonkNote.Services;
using GonkNote.ViewModels;
using GonkNote.Views;

namespace GonkNote;

/// <summary>
/// Das Hauptfenster. Was hier steht, ist ausschließlich Oberflächenverhalten: Auswahl,
/// Umbenennen, Menüs. <b>Jede Entscheidung fällt im <see cref="MainViewModel"/></b> — dem
/// gleichen, das der WPF-Kopf benutzt.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        // **InitializeComponent, nicht AvaloniaXamlLoader.Load.** Beide bauen den
        // Oberflächenbaum auf, aber nur die erzeugte Methode weist danach die
        // `x:Name`-Felder zu. Mit dem Lader direkt bleibt jedes davon `null`, und der
        // erste Zugriff wirft eine NullReferenceException an einer Stelle, die mit der
        // Ursache nichts zu tun hat (HANDOFF §7).
        InitializeComponent();

        _vm = new MainViewModel(App.Db, App.Platform);
        DataContext = _vm;

        SpracheHaken();
        Loc.LanguageChanged += SpracheHaken;

        // Beim Schließen den Stand sichern — die Autospeicherung läuft nur alle 30 Sekunden.
        Closing += (_, _) =>
        {
            _vm.CommitPendingRename();
            _vm.SaveAll();
            App.Db.SetSetting("language", Loc.Code);
        };
    }

    // ---------- Menüleiste ----------

    private void Seitenleiste_Click(object? sender, RoutedEventArgs e) =>
        Seitenleiste.IsVisible = !Seitenleiste.IsVisible;

    private void Sprache_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string code }) return;
        Loc.Apply(Loc.FromCode(code));
        App.Db.SetSetting("language", code);
    }

    /// <summary>Setzt den Haken auf die aktive Sprache — auch, wenn sie woanders gewechselt wurde.</summary>
    private void SpracheHaken()
    {
        SpracheDeutsch.IsChecked = Loc.Current == AppLanguage.German;
        SpracheEnglisch.IsChecked = Loc.Current == AppLanguage.English;
    }

    private void Ueber_Click(object? sender, RoutedEventArgs e) => new AboutWindow().ShowDialog(this);

    // ---------- Baum ----------

    private void Baum_SelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        _vm.SelectedTreeItem = Baum.SelectedItem as TreeItemViewModel;

    private void Baum_DoubleTapped(object? sender, TappedEventArgs e)
    {
        // Nicht auf einem Umbenennen-Feld: dort ist ein Doppelklick Wortauswahl.
        if (e.Source is TextBox) return;
        if (_vm.SelectedTreeItem is { } t) _vm.OpenItem(t);
    }

    private void Baum_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Source is TextBox) return;   // die Umbenennung hat eigene Tasten
        if (_vm.SelectedTreeItem is not { } t) return;

        switch (e.Key)
        {
            case Key.Enter:
                _vm.OpenItem(t);
                e.Handled = true;
                break;
            case Key.F2:
                t.IsRenaming = true;
                e.Handled = true;
                break;
            case Key.Delete:
                _vm.DeleteCommand.Execute(t);
                e.Handled = true;
                break;
        }
    }

    private void Symbolfarbe_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string hex }) return;
        // Leerer Tag = „automatisch": die Farbe des übergeordneten Ordners erben.
        _vm.SetIconColor(_vm.SelectedTreeItem, hex.Length == 0 ? null : hex);
    }

    // ---------- Umbenennen ----------

    /// <summary>Das Feld erscheint — Fokus hinein und den Namen markieren.</summary>
    private void Umbenennen_Sichtbar(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox box || !box.IsVisible) return;
        // Nach dem Einhängen, nicht währenddessen: davor gibt es noch kein Fenster, das
        // den Fokus vergeben könnte.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            box.Focus();
            box.SelectAll();
        });
    }

    private void Umbenennen_Fokus(object? sender, RoutedEventArgs e)
    {
        if ((sender as TextBox)?.DataContext is TreeItemViewModel t && t.IsRenaming)
            _vm.CommitRename(t);
    }

    private void Umbenennen_Taste(object? sender, KeyEventArgs e)
    {
        if ((sender as TextBox)?.DataContext is not TreeItemViewModel t) return;

        if (e.Key == Key.Enter)
        {
            _vm.CommitRename(t);
            Baum.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Verwerfen heißt: den Namen von vor der Umbenennung zurückholen.
            if (t.NameBeforeRename is { } alt) t.Name = alt;
            t.IsRenaming = false;
            Baum.Focus();
            e.Handled = true;
        }
    }

    // ---------- Galerie ----------

    private void GalerieNeu_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string art }) return;

        // Ziel ist der Ordner, den die Galerie gerade zeigt — nicht die Auswahl im Baum.
        var ziel = _vm.GalleryFolder;
        switch (art)
        {
            case nameof(ItemKind.Folder): _vm.NewFolderCommand.Execute(ziel); break;
            case nameof(ItemKind.Notebook): _vm.NewNotebookCommand.Execute(ziel); break;
            case nameof(ItemKind.Whiteboard): _vm.NewWhiteboardCommand.Execute(ziel); break;
            case nameof(ItemKind.TextDocument): _vm.NewTextDocCommand.Execute(ziel); break;
        }
    }

    /// <summary>
    /// Der Chevron öffnet das Kachelmenü. Er sitzt <b>in</b> der Kachel, die selbst ein
    /// Knopf ist — ohne <c>Handled</c> öffnete derselbe Klick auch noch das Dokument.
    /// </summary>
    private void GalerieMenue_Click(object? sender, RoutedEventArgs e) => e.Handled = true;

    private void GalerieEintrag_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string was } eintrag) return;
        if (Kachel(eintrag) is not { } item) return;

        switch (was)
        {
            case "öffnen": _vm.GalleryOpenCommand.Execute(item); break;
            case "umbenennen": _vm.BeginRename(item.Tree); break;
            case "löschen": _vm.DeleteCommand.Execute(item.Tree); break;
        }
    }

    /// <summary>
    /// Zu welcher Kachel gehört ein Menüeintrag?
    /// <para>
    /// <b>Nicht über den DataContext des Eintrags:</b> ein Flyout hängt in einem eigenen
    /// Popup-Fenster und damit in einem anderen Visual Tree als die Kachel. Der Weg führt
    /// deshalb über den <b>logischen</b> Elternteil bis zum Knopf, der das Flyout besitzt —
    /// dessen DataContext ist die Kachel.
    /// </para>
    /// </summary>
    private static GalleryItemViewModel? Kachel(Control eintrag)
    {
        for (StyledElement? p = eintrag; p != null; p = p.Parent)
            if (p.DataContext is GalleryItemViewModel item) return item;
        return null;
    }
}
