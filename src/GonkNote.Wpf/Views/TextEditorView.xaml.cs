using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
// **Kein `using GonkNote.Core.Models`** — dort steht ein `TextElement`, und WPF hat auch
// eines (System.Windows.Documents). Beides zugleich sichtbar zu machen macht jeden der vier
// Verweise weiter unten mehrdeutig. Der eine Core-Typ, der hier gebraucht wird, steht im
// Kommentar voll ausgeschrieben.
using GonkNote.Core.Text;
using GonkNote.Services;
using GonkNote.ViewModels;

namespace GonkNote.Views;

/// <summary>
/// Rich-Text-Editor für Textdokumente im Ribbon-Layout (Design-Konzept nach
/// ONLYOFFICE-Vorbild, Farben ausschließlich aus Themes/Light|Dark.xaml).
/// Speichert als XamlPackage (erhält Bilder/Tabellen), lädt ältere RTF-Dokumente.
/// Kern: Laden/Speichern, Ribbon-Umschaltung, Zoom, Lineal, Statusleiste,
/// Navigator, Theme-/Ink-Handling. Werkzeuge in den partial-Dateien.
/// </summary>
public partial class TextEditorView : UserControl
{
    // Seit §4.93 aus Core: Es gab die Leiter zweimal, und keine der beiden enthielt alle
    // Werte der anderen (hier fehlten 22, 40, 56, 64; drueben fehlte 15). Das ist §5 Nr. 14
    // noch einmal, fuer Grade statt Familien.
    private static IReadOnlyList<double> FontSizes => Core.Theming.Schriftliste.Schriftgrade;

    private TextTabViewModel? _vm;
    private bool _loading;
    private bool _syncing;        // Toolbar ← Auswahl wird gerade synchronisiert
    private bool _syncingLayout;  // Layout-Felder ← Modell wird gerade befüllt
    private string _textColorHex = "#EC4899";
    private Color? _highlightColor = Color.FromRgb(0xFD, 0xE0, 0x47);

    private readonly DispatcherTimer _statsTimer;

    public TextEditorView()
    {
        InitializeComponent();

        // **Mitgelieferte oben, Systemschriften darunter** — die Zusammensetzung steht in
        // Core und gilt fuer beide Koepfe (§5 Nr. 14, Schriftliste.cs). Hier stand bis zum
        // 2026-08-30 nur `SystemFontFamilies`, also NUR Systemschriften: ein neues Dokument
        // steht in „Source Sans 3", und dieses Feld blieb deshalb LEER (§4.71).
        FontCombo.ItemsSource = GonkNote.Core.Theming.Schriftliste.Aufbauen(
            System.Windows.Media.Fonts.SystemFontFamilies.Select(f => f.Source));
        SizeCombo.ItemsSource = FontSizes;

        BuildStyleGallery();
        BuildSymbolGrid();

        // Wortanzahl/Navigator/Umbruch-Marken nicht bei jedem Tastendruck neu berechnen
        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _statsTimer.Tick += (_, _) =>
        {
            _statsTimer.Stop();
            UpdateWordCount();
            RefreshNavigator();
            DrawPageBreaks();
        };
        Editor.SizeChanged += (_, _) => DrawPageBreaks();

        Editor.AddHandler(Hyperlink.RequestNavigateEvent,
            new System.Windows.Navigation.RequestNavigateEventHandler(Hyperlink_RequestNavigate));

        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            App.Platform.Theme.ThemeChanged += OnThemeChanged;
            Loc.LanguageChanged += OnLanguageChanged;
        };
        Unloaded += (_, _) =>
        {
            App.Platform.Theme.ThemeChanged -= OnThemeChanged;
            Loc.LanguageChanged -= OnLanguageChanged;
        };
    }

    // Die Seite bleibt in beiden Themes weiß → Standard-Tinte ist immer die helle Variante.
    private static Color CurrentInk() => TextStyles.InkLight;

    private void OnThemeChanged()
    {
        // Repariert Dokumente, die (aus der dunklen-Seite-Phase) helle Tinte gespeichert haben
        TextStyles.NormalizeInk(Editor.Document, CurrentInk());
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.FlushRequested -= FlushToModel;

        _vm = DataContext as TextTabViewModel;
        if (_vm == null) return;

        _vm.FlushRequested += FlushToModel;
        LoadFromModel();
    }

    // ==================== Laden / Speichern ====================

    private void LoadFromModel()
    {
        if (_vm == null) return;
        _loading = true;
        try
        {
            // **Das Modell führt — seit Schritt 7** (§4.48). Bis dahin stand hier das
            // Altformat zuerst, und `AusModell` war der Rückfall für den einen Fall, den es
            // erst seit dem Linux-Import gab. **Jetzt ist es umgekehrt**, und das Altformat
            // ist der Rückfall: Es kommt nur noch zum Zug, wenn das Modell leer ist — also
            // wenn die einmalige Übernahme (§4.22) noch aussteht oder **fehlgeschlagen** ist.
            // Genau dann ist es richtig: Der Nutzer sieht sein Dokument, statt eines leeren
            // Blattes.
            if (!AusModell()) AusAltformat();

            TextStyles.NormalizeInk(Editor.Document, CurrentInk());
            LoadSettingsToUi();
            ApplyPageSetup();

            ZoomSlider.Value = Math.Clamp(_vm.Zoom * 100, ZoomSlider.Minimum, ZoomSlider.Maximum);
        }
        finally
        {
            _loading = false;
        }
        UpdateWordCount();
        RefreshNavigator();
        Editor_SelectionChanged(this, new RoutedEventArgs());  // Toolbar initial befüllen

        // Geladenes Dokument einheitlich in der gewählten Sprache prüfen (überschreibt
        // beim Import/aus früheren Sitzungen gespeicherte, teils gemischte Sprach-Tags).
        SetSpellLanguage(CurrentSpellLanguage());
    }

    /// <summary>
    /// Der Rückfall auf <see cref="Core.Models.TextDoc.Model"/>, wenn das Altfeld leer ist — <b>der Fall,
    /// den es erst seit dem Linux-Import gibt</b> (§4.28).
    ///
    /// <para>
    /// <b>Warum das sein muss:</b> <c>AvaloniaDocumentIo.Import</c> kann kein <c>XamlPackage</c>
    /// bauen, das gibt es nur unter Windows. Ein unter Linux importiertes Dokument hat deshalb
    /// nur <c>Model</c> und ein leeres <c>Rtf</c>. Ohne diesen Zweig zeigte der WPF-Editor dafür
    /// ein leeres Blatt — und das ist der teuerste Fehler dieser Art, weil er nicht nach einer
    /// fehlenden Funktion aussieht, sondern nach gelöschtem Inhalt.
    /// </para>
    /// <para>
    /// <b>Es kehrt die Reihenfolge nicht um.</b> <c>Rtf</c> führt weiter, solange dort etwas
    /// steht (§5); gelesen wird von hier nur, wenn es sonst gar nichts zu lesen gäbe. Genau so
    /// steht es seit jeher an <see cref="Core.Models.TextDoc.Model"/>: „wer voll ist, führt".
    /// </para>
    /// </summary>
    /// <returns><c>false</c>, wenn auch das Modell leer ist — dann bleibt es beim leeren Blatt.</returns>
    private bool AusModell()
    {
        if (_vm == null || TdFormatIo.Lesen(_vm.Doc.Model) is not { } modell) return false;

        var flow = TdZuFlow.Umwandeln(modell, App.Db.Blobs, _vm.Doc);   // samt Seiteneinrichtung
        TdZuFlow.InhaltUebernehmen(flow, Editor.Document);

        // **`DocumentImages.Attach` steht hier nicht mehr**, und das ist kein Vergessen:
        // Es übersetzt einen Blob-Verweis aus dem `ToolTip` in das `Tag` — nötig **nach dem
        // Laden eines Pakets**, denn nur der ToolTip übersteht ein XamlPackage. Auf diesem Weg
        // gibt es kein Paket, und `TdZuFlow.BildUmwandeln` setzt das `Tag` gleich selbst.
        // Wächter: `Ein_Bild_behaelt_seinen_Blob_Verweis`.
        return true;
    }

    /// <summary>
    /// <b>Der Rückfall auf das Altformat</b> — seit Schritt 7 die Ausnahme und nicht mehr der
    /// Normalfall (§4.48).
    ///
    /// <para>
    /// <b>Es gibt ihn noch, und zwar für genau einen Fall:</b> Das Modell ist leer, weil die
    /// einmalige Übernahme (§4.22) aussteht oder <b>fehlgeschlagen</b> ist. Dann steht der
    /// Inhalt nur im Altfeld, und ihn nicht zu zeigen hieße, ein volles Dokument als leeres
    /// Blatt anzuzeigen — <b>der teuerste Fehler dieser Art</b>, weil er nach gelöschtem
    /// Inhalt aussieht und nicht nach einer fehlenden Funktion (§4.28).
    /// </para>
    /// <para>
    /// <b>Geschrieben wird von hier aus trotzdem nichts zurück.</b> <c>Rtf</c> wird seit
    /// Schritt 7 nie mehr überschrieben (§4.22) — was der Nutzer hier tippt, geht beim
    /// Speichern ins <b>Modell</b>, und beim nächsten Öffnen führt es. Der Rückfall heilt sich
    /// also von selbst, sobald einmal gespeichert wurde.
    /// </para>
    /// </summary>
    private void AusAltformat()
    {
        var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);
        var bytes = _vm!.Doc.Rtf;

        if (bytes.Length <= 2)
        {
            range.Text = "";
            return;
        }

        using var ms = new MemoryStream(bytes);
        // XamlPackage ist ein ZIP ("PK"), alles andere ist historisches RTF
        bool istPaket = bytes[0] == 0x50 && bytes[1] == 0x4B;
        range.Load(ms, istPaket ? DataFormats.XamlPackage : DataFormats.Rtf);

        // Hier **schon**: Aus einem Paket kommt der Blob-Verweis im `ToolTip` an und muss ins
        // `Tag` übersetzt werden. Auf dem Weg über das Modell entfällt das (siehe `AusModell`).
        DocumentImages.Attach(Editor.Document, App.Db.Blobs);
    }

    /// <summary>
    /// <b>Schritt 7: Der Editor schreibt das Modell — und <c>Rtf</c> nie wieder</b> (§4.48).
    ///
    /// <para>
    /// <b>Das ist der eigentliche Zweck des ganzen Wegs</b> (§5). Vorher stand hier
    /// <c>_vm.Doc.Rtf = ms.ToArray()</c> — ein <c>XamlPackage</c> —, und
    /// <c>WpfDocumentIo.Migrate</c> baute daraus bei jedem Speichern das Modell neu. **Wer
    /// unter Linux geschrieben hatte, verlor seine Arbeit beim nächsten Speichern unter
    /// Windows, still** (§5 „Noch offen" 9). Jetzt schreibt dieser Kopf dasselbe Feld wie
    /// jener: <see cref="TextTabViewModel.Modell"/>.
    /// </para>
    /// <para>
    /// <b><c>Rtf</c> wird nicht überschrieben — und das ist keine Nachlässigkeit, sondern die
    /// Regel aus §4.22.</b> Das Altfeld bleibt stehen, wie es war: als unangetastete Sicherung
    /// dessen, was vor der Übernahme dastand. Eine misslungene Übernahme ist damit kein
    /// Datenverlust, sondern ein Versuch, der beim nächsten Öffnen wiederholt wird.
    /// </para>
    /// <para>
    /// <b>Warum über <see cref="TextTabViewModel.Modell"/> und nicht direkt nach
    /// <c>Doc.Model</c>:</b> Damit beide Köpfe **denselben** Weg nehmen. <c>Save</c> ruft
    /// <c>Mitschreiben</c> (die Übernahme) und danach <c>ModellMitschreiben</c> (das
    /// Geschriebene) — <b>die Reihenfolge steht seit §4.23 fest und ist genau für diesen Tag
    /// so gewählt worden.</b> Zwei Wege in dasselbe Feld wären die Falle aus §4.13.
    /// </para>
    /// </summary>
    private void FlushToModel()
    {
        if (_vm == null) return;

        _vm.Modell = FlowZuTd.Umwandeln(_vm.Doc, Editor.Document, App.Db.Blobs);
        _vm.Doc.Images = DocumentImages.UsedBlobs(Editor.Document).ToList();
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || _vm == null) return;
        _vm.IsDirty = true;
        _statsTimer.Stop();
        _statsTimer.Start();
    }

    private void MarkDirty()
    {
        if (_vm != null) _vm.IsDirty = true;
    }

    // ==================== Ribbon ====================

    private void RibbonTab_Checked(object sender, RoutedEventArgs e)
    {
        if (PanelStart == null || PanelTable == null) return;  // während InitializeComponent
        string tag = (string)((RadioButton)sender).Tag;
        PanelStart.Visibility = tag == "Start" ? Visibility.Visible : Visibility.Collapsed;
        PanelInsert.Visibility = tag == "Einfügen" ? Visibility.Visible : Visibility.Collapsed;
        PanelLayout.Visibility = tag == "Layout" ? Visibility.Visible : Visibility.Collapsed;
        PanelRefs.Visibility = tag == "Verweise" ? Visibility.Visible : Visibility.Collapsed;
        PanelTable.Visibility = tag == "Tabelle" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Undo_Click(object s, RoutedEventArgs e) { Editor.Undo(); Editor.Focus(); }
    private void Redo_Click(object s, RoutedEventArgs e) { Editor.Redo(); Editor.Focus(); }

    // ==================== Toolbar-Zustand mit Auswahl synchronisieren ====================

    private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;

        if (TryApplyFormatPainter()) return;

        _syncing = true;
        try
        {
            var sel = Editor.Selection;

            FontCombo.SelectedItem = sel.GetPropertyValue(TextElement.FontFamilyProperty) is FontFamily ff
                ? ff.Source : null;
            // **Einen Grad, den die Leiter nicht hat, trotzdem zeigen** (§4.95). Vorher
            // lieferte `FirstOrDefault` hier `null`, und das Feld blieb leer: richtig
            // gehandelt, aber der Nutzer konnte die geltende Groesse weder ablesen noch
            // wiederherstellen. Die Ergaenzung steht in Core und gilt fuer beide Koepfe.
            if (sel.GetPropertyValue(TextElement.FontSizeProperty) is double fs)
            {
                var grade = Core.Theming.Schriftliste.GradeMit(fs);
                if (!ReferenceEquals(SizeCombo.ItemsSource, grade)) SizeCombo.ItemsSource = grade;
                SizeCombo.SelectedItem = grade.FirstOrDefault(s => Math.Abs(s - fs) < 0.1);
            }
            else
            {
                if (!ReferenceEquals(SizeCombo.ItemsSource, FontSizes)) SizeCombo.ItemsSource = FontSizes;
                SizeCombo.SelectedItem = null;
            }

            BtnBold.IsChecked = sel.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight w &&
                                w >= FontWeights.Bold;
            BtnItalic.IsChecked = sel.GetPropertyValue(TextElement.FontStyleProperty) is FontStyle st &&
                                  st == FontStyles.Italic;

            var deco = sel.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
            BtnUnderline.IsChecked = deco?.Any(d => d.Location == TextDecorationLocation.Underline) == true;
            BtnStrike.IsChecked = deco?.Any(d => d.Location == TextDecorationLocation.Strikethrough) == true;

            var baseline = sel.GetPropertyValue(Inline.BaselineAlignmentProperty);
            BtnSub.IsChecked = baseline is BaselineAlignment.Subscript;
            BtnSuper.IsChecked = baseline is BaselineAlignment.Superscript;

            SyncAlignButton(sel.Start.Paragraph?.TextAlignment);

            SyncParaSpacingFields();
            SyncStyleGallery();
            UpdateTableSection();
        }
        finally
        {
            _syncing = false;
        }

        EnsureCaretVisible();
    }

    /// <summary>
    /// Der äußere ScrollViewer scrollt (die RichTextBox selbst hat keinen Viewport) –
    /// deshalb den Cursor beim Tippen/Navigieren manuell sichtbar halten.
    /// </summary>
    private void EnsureCaretVisible()
    {
        try
        {
            var rect = Editor.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
            if (rect.IsEmpty) return;

            var top = Editor.TransformToAncestor(PageHost).Transform(rect.TopLeft);
            var bottom = Editor.TransformToAncestor(PageHost).Transform(rect.BottomLeft);
            double y1 = top.Y + PageHost.Margin.Top;
            double y2 = bottom.Y + PageHost.Margin.Top;

            if (y1 < MainScroll.VerticalOffset + 8)
                MainScroll.ScrollToVerticalOffset(Math.Max(0, y1 - 40));
            else if (y2 > MainScroll.VerticalOffset + MainScroll.ViewportHeight - 8)
                MainScroll.ScrollToVerticalOffset(y2 - MainScroll.ViewportHeight + 40);
        }
        catch
        {
            // Layout noch nicht fertig – unkritisch
        }
    }

    // ==================== Zoom ====================

    private void Zoom_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomText == null || PageScale == null) return;  // während InitializeComponent
        double z = e.NewValue / 100.0;
        PageScale.ScaleX = z;
        PageScale.ScaleY = z;
        ZoomText.Text = $"{Math.Round(e.NewValue)} %";
        if (_vm != null) _vm.Zoom = z;
        DrawRuler();
    }

    private void ZoomIn_Click(object s, RoutedEventArgs e) => ZoomSlider.Value = Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + 10);
    private void ZoomOut_Click(object s, RoutedEventArgs e) => ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, ZoomSlider.Value - 10);

    private void MainScroll_PreviewMouseWheel(object s, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        ZoomSlider.Value = Math.Clamp(ZoomSlider.Value + (e.Delta > 0 ? 10 : -10),
            ZoomSlider.Minimum, ZoomSlider.Maximum);
        e.Handled = true;
    }

    private void MainScroll_ScrollChanged(object s, ScrollChangedEventArgs e)
    {
        // Lineal horizontal mit dem Arbeitsbereich synchron halten
        if (RulerHost.Width != MainScroll.ExtentWidth && MainScroll.ExtentWidth > 0)
            RulerHost.Width = MainScroll.ExtentWidth;
        RulerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
    }

    // ==================== Statusleiste ====================

    private void UpdateWordCount()
    {
        string text = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;
        int words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        int chars = text.Count(c => c != '\r' && c != '\n');
        WordCountText.Text = Loc.T("Ed.Status.Counts.Format", words, chars);
    }

    private void Language_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Editor == null || LanguageCombo.SelectedItem is not ComboBoxItem item) return;
        SetSpellLanguage(XmlLanguage.GetLanguage((string)item.Tag));
    }

    /// <summary>Aktuelle Prüfsprache aus der Combo (Standard Deutsch).</summary>
    private XmlLanguage CurrentSpellLanguage()
    {
        var tag = (LanguageCombo?.SelectedItem as ComboBoxItem)?.Tag as string ?? "de-DE";
        return XmlLanguage.GetLanguage(tag);
    }

    /// <summary>
    /// Setzt die Sprache der Rechtschreibprüfung fürs ganze Dokument. WPF vergibt die
    /// Sprache sonst pro Textabschnitt anhand der Eingabesprache (Tastaturlayout) bzw.
    /// übernimmt sie beim DOCX-Import – dann werden Teile mit dem falschen Wörterbuch geprüft
    /// (Wörter fälschlich/nicht angestrichen) und ein reiner Sprachwechsel bliebe wirkungslos.
    /// Deshalb überschreiben wir die Sprache aller Runs/Blöcke und stoßen die Prüfung neu an.
    /// </summary>
    private void SetSpellLanguage(XmlLanguage lang)
    {
        if (Editor == null) return;
        Editor.Language = lang;
        Editor.Document.Language = lang;
        ApplyLanguageToBlocks(Editor.Document.Blocks, lang);
        ForceSpellRecheck();
        UpdateSpellLangWarning(lang);
    }

    /// <summary>
    /// Zeigt einen Hinweis, wenn Windows für die gewählte Sprache kein Wörterbuch hat
    /// (dann bleiben Markierungen aus – z. B. Englisch ohne installiertes Sprachpaket).
    /// </summary>
    private void UpdateSpellLangWarning(XmlLanguage lang)
    {
        if (SpellLangWarn == null) return;
        bool ok = App.Platform.SpellChecker.IsSupported(lang.IetfLanguageTag);
        SpellLangWarn.Visibility = ok ? Visibility.Collapsed : Visibility.Visible;
        if (!ok)
        {
            string name = (LanguageCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? lang.IetfLanguageTag;
            SpellLangWarn.ToolTip = Loc.T("Msg.NoDictionary", name);
        }
    }

    /// <summary>
    /// Texte, die der Code setzt (Formatvorlagen-Galerie, Wortzähler, Rechtschreib-Hinweis),
    /// hängen nicht an einer Bindung – nach einem Sprachwechsel werden sie neu aufgebaut.
    /// </summary>
    private void OnLanguageChanged()
    {
        StyleGallery.Children.Clear();
        StyleGridFull.Children.Clear();
        _styleCards.Clear();
        BuildStyleGallery();
        UpdateWordCount();
        UpdateSpellLangWarning(Editor.Language);
    }

    private static void ApplyLanguageToBlocks(BlockCollection blocks, XmlLanguage lang)
    {
        foreach (Block b in blocks) ApplyLanguageToBlock(b, lang);
    }

    private static void ApplyLanguageToBlock(Block b, XmlLanguage lang)
    {
        b.Language = lang;
        switch (b)
        {
            case Paragraph p:
                ApplyLanguageToInlines(p.Inlines, lang);
                break;
            case Section sec:
                ApplyLanguageToBlocks(sec.Blocks, lang);
                break;
            case List list:
                foreach (ListItem li in list.ListItems) { li.Language = lang; ApplyLanguageToBlocks(li.Blocks, lang); }
                break;
            case Table table:
                foreach (TableRowGroup rg in table.RowGroups)
                    foreach (TableRow row in rg.Rows)
                        foreach (TableCell cell in row.Cells) { cell.Language = lang; ApplyLanguageToBlocks(cell.Blocks, lang); }
                break;
        }
    }

    private static void ApplyLanguageToInlines(InlineCollection inlines, XmlLanguage lang)
    {
        foreach (Inline inline in inlines)
        {
            inline.Language = lang;
            if (inline is Span span) ApplyLanguageToInlines(span.Inlines, lang);
        }
    }

    /// <summary>
    /// Zwingt die WPF-Rechtschreibprüfung, den gesamten Text neu zu bewerten (kurz aus/ein):
    /// Ein reiner Sprachwechsel markiert bereits geprüften Text sonst nicht neu.
    /// </summary>
    private void ForceSpellRecheck()
    {
        if (Editor == null || BtnSpell?.IsChecked != true) return;
        Editor.SpellCheck.IsEnabled = false;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (BtnSpell?.IsChecked == true) Editor.SpellCheck.IsEnabled = true;
        }), DispatcherPriority.Background);
    }

    private void Spell_Click(object s, RoutedEventArgs e) =>
        Editor.SpellCheck.IsEnabled = BtnSpell.IsChecked == true;

    // ==================== Überschriften-Navigator ====================

    private void ToggleNavigator_Click(object s, RoutedEventArgs e)
    {
        NavPanel.Visibility = BtnSideNav.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        RefreshNavigator();
    }

    private void RefreshNavigator()
    {
        if (NavPanel.Visibility != Visibility.Visible) return;

        NavList.Items.Clear();
        foreach (var (level, text, para) in TextStyles.CollectHeadings(Editor.Document))
        {
            NavList.Items.Add(new ListBoxItem
            {
                Content = new TextBlock
                {
                    Text = text,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontSize = level == 1 ? 13 : 12,
                    FontWeight = level == 1 ? FontWeights.SemiBold : FontWeights.Normal,
                },
                Margin = new Thickness((level - 1) * 12, 0, 0, 0),
                Padding = new Thickness(6, 3, 4, 3),
                Tag = para,
                ToolTip = text,
            });
        }
        if (NavList.Items.Count == 0)
            NavList.Items.Add(new ListBoxItem
            {
                Content = "Keine Überschriften",
                IsEnabled = false,
                Padding = new Thickness(6, 3, 4, 3),
            });
    }

    private void NavList_Selected(object s, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ListBoxItem { Tag: Paragraph para }) return;
        para.BringIntoView();
        Editor.CaretPosition = para.ContentStart;
        Editor.Focus();
        NavList.SelectedItem = null;
    }

    // ==================== Tastatur ====================

    private void Editor_PreviewKeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ShowFind();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && FindPanel.Visibility == Visibility.Visible)
        {
            CloseFind_Click(s, e);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
        {
            // Strg+Alt+1…4 = Überschrift 1…4, Strg+Alt+0 = Standard (wie Word)
            int? level = e.Key switch
            {
                Key.D1 or Key.NumPad1 => 1,
                Key.D2 or Key.NumPad2 => 2,
                Key.D3 or Key.NumPad3 => 3,
                Key.D4 or Key.NumPad4 => 4,
                Key.D0 or Key.NumPad0 => 0,
                _ => null,
            };
            if (level is { } lvl)
            {
                ApplyParaStyle(TextStyles.ForHeading(lvl));
                e.Handled = true;
            }
        }
    }
}
