using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GonkNote.Core.Models;
using GonkNote.Core.Theming;
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
        // **Vor InitializeComponent.** Die KeyBinding in der XAML bindet beim Aufbau
        // auf diese Eigenschaft; steht sie danach, ist sie beim Binden noch null und
        // die Taste bleibt still.
        SeitenleisteBefehl = new RelayCommand(SeitenleisteUmschalten);

        // **InitializeComponent, nicht AvaloniaXamlLoader.Load.** Beide bauen den
        // Oberflächenbaum auf, aber nur die erzeugte Methode weist danach die
        // `x:Name`-Felder zu. Mit dem Lader direkt bleibt jedes davon `null`, und der
        // erste Zugriff wirft eine NullReferenceException an einer Stelle, die mit der
        // Ursache nichts zu tun hat (HANDOFF §7).
        InitializeComponent();

        _vm = new MainViewModel(App.Db, App.Platform);
        DataContext = _vm;

        // Den Stand der Seitenleiste wiederherstellen — derselbe Schlüssel wie drüben, damit
        // beide Köpfe auf derselben Datenbank dasselbe meinen. **Nur die 0 klappt zu:** ein
        // fehlender Wert heißt „noch nie zugeklappt" und nicht „zu".
        if (App.Db.GetSetting("sidebar") == "0") SeitenleisteSetzen(false);

        SpracheHaken();
        Loc.LanguageChanged += SpracheHaken;

        ZiehenEinhaengen();
        TitelleisteEinhaengen();

        // Beim Schließen den Stand sichern — die Autospeicherung läuft nur alle 30 Sekunden.
        Closing += (_, _) =>
        {
            _vm.CommitPendingRename();
            _vm.SaveAll();
            App.Db.SetSetting("language", Loc.Code);
        };
    }

    // ---------- Menüleiste ----------

    /// <summary>
    /// Strg+B blendet die Seitenleiste um — <b>bis zum 2026-08-30 konnte das nur der
    /// WPF-Kopf</b> (§4.71), obwohl beide Menüs denselben Eintrag zeigen.
    ///
    /// <para>
    /// <b>Ein Befehl und kein <c>KeyDown</c>-Zweig:</b> Die drei anderen Kürzel des
    /// Fensters hängen an <c>Window.KeyBindings</c>, und eine vierte Taste an einer
    /// anderen Stelle zu behandeln hieße, bei der nächsten Änderung zwei Orte zu
    /// kennen. <c>Seitenleiste_Click</c> bleibt daneben stehen, weil Menü und Knopf
    /// ein <c>RoutedEventArgs</c> liefern und kein Befehlsziel.
    /// </para>
    /// </summary>
    public System.Windows.Input.ICommand SeitenleisteBefehl { get; }

    private void Seitenleiste_Click(object? sender, RoutedEventArgs e) =>
        SeitenleisteUmschalten();

    private void SeitenleisteUmschalten() => SeitenleisteSetzen(!_seitenleisteOffen);

    /// <summary>Ist die Seitenleiste aufgeklappt? Startwert wie drüben: ja.</summary>
    private bool _seitenleisteOffen = true;

    /// <summary>
    /// Die Breite, mit der die Leiste wieder aufgeht. <b>Sie wird beim Einklappen gemerkt</b>,
    /// damit eine von Hand gezogene Breite das Zuklappen übersteht — sonst stünde sie beim
    /// nächsten Aufklappen wieder auf den 260 Punkten aus dem XAML.
    /// </summary>
    private GridLength _seitenleisteBreite = new(260);

    /// <summary>
    /// Die Spalte der Seitenleiste. <b>Über das Raster geholt und nicht über <c>x:Name</c></b> —
    /// der Grund steht im XAML über den Spaltendefinitionen: Avalonia erzeugt für eine
    /// <c>ColumnDefinition</c> kein Feld, der WPF-Kopf bekommt dort eines.
    /// </summary>
    private ColumnDefinition SeitenleisteSpalte => Hauptraster.ColumnDefinitions[0];

    /// <summary>
    /// Klappt die Seitenleiste ein und aus.
    ///
    /// <para>
    /// ⛔ <b>Bis zum 2026-09-09 stand hier eine Zeile:</b> <c>Seitenleiste.IsVisible =
    /// !Seitenleiste.IsVisible</c>. Das blendet den <b>Inhalt</b> aus und sonst nichts — die
    /// Rasterspalte ist 260 Punkte breit geblieben, der Trenner daneben sichtbar, und der
    /// Arbeitsbereich damit genauso schmal wie vorher. Am laufenden Programm sah es aus, als
    /// hätte sich die Leiste geleert statt geschlossen. <b>Der WPF-Kopf hat es von Anfang an
    /// richtig gemacht</b> (<c>SetSidebarVisible</c>), und der Vergleich der beiden Flächen in
    /// §4.71 hat es trotzdem nicht gefunden: gemessen wurde, was zu sehen ist, nicht was
    /// passiert, wenn man darauf drückt.
    /// </para>
    /// <para>
    /// <b>Drei Dinge gehören zusammen</b>, und jedes einzeln weggelassen ergibt ein halb
    /// geschlossenes Bild: der Inhalt verschwindet, die <b>Spalte</b> geht auf 0 (samt
    /// <c>MinWidth</c> — eine Mindestbreite von 180 hält die Spalte sonst offen), und der
    /// <b>Trenner</b> geht mit.
    /// </para>
    /// <para>
    /// <b>Der Stand wird gesichert</b>, genau wie drüben (<c>App.Db</c>, Schlüssel
    /// <c>sidebar</c>) und mit demselben Schlüssel — wer die Leiste zuklappt, will sie beim
    /// nächsten Start zu haben, und zwei Köpfe auf derselben Datenbank dürfen sich darüber
    /// nicht widersprechen.
    /// </para>
    /// </summary>
    private void SeitenleisteSetzen(bool offen)
    {
        if (offen == _seitenleisteOffen) return;
        _seitenleisteOffen = offen;

        if (offen)
        {
            Seitenleiste.IsVisible = true;
            SeitenleisteTrenner.IsVisible = true;
            SeitenleisteSpalte.MinWidth = 180;
            SeitenleisteSpalte.Width = _seitenleisteBreite;
        }
        else
        {
            _seitenleisteBreite = SeitenleisteSpalte.Width;
            Seitenleiste.IsVisible = false;
            SeitenleisteTrenner.IsVisible = false;
            // **Erst die Mindestbreite, dann die Breite.** Umgekehrt klemmt das Raster die 0
            // sofort wieder auf 180 hoch, und die Leiste bliebe als leerer Streifen stehen.
            SeitenleisteSpalte.MinWidth = 0;
            SeitenleisteSpalte.Width = new GridLength(0);
        }

        App.Db.SetSetting("sidebar", offen ? "1" : "0");
    }

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

    private void ErsteSchritte_Click(object? sender, RoutedEventArgs e) =>
        new GuideWindow().ShowDialog(this);

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

    /// <summary>
    /// Eine freie Symbolfarbe über den Farbwähler — <b>der Eintrag fehlte hier ganz</b>
    /// (§4.71), obwohl der Wähler seit §4.52 da ist und der WPF-Kopf denselben Weg
    /// anbietet.
    ///
    /// <para>
    /// <b>Ohne Deckkraft</b>, wie drüben: Eine halb durchsichtige Ordnerfarbe im Baum
    /// wäre auf hellem und dunklem Grund verschieden hell — eine Einstellung, die je
    /// nach Theme etwas anderes bedeutet, ist keine.
    /// </para>
    /// </summary>
    private void SymbolfarbeEigene_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedTreeItem is not { } eintrag) return;

        // Tuerkis als Ausgangspunkt, wenn noch keine Farbe gesetzt ist — dieselbe
        // Vorgabe wie im WPF-Kopf (dort `Colors.Teal`).
        var start = HexColor.Parse(eintrag.Item.IconColor, new HexColor(0xFF, 0x14, 0xB8, 0xA6));

        if (ColorPickerWindow.Waehlen(this, start, mitDeckkraft: false) is { } gewaehlt)
            _vm.SetIconColor(eintrag, gewaehlt.ToString());
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
