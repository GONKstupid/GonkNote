using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Core.Theming;
using GonkNote.Platform;
using GonkNote.Services;
using GonkNote.ViewModels;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Die Zeichenfläche für Notizbücher und Whiteboards unter Linux.
///
/// <para>
/// <b>Was hier drinsteht und was nicht.</b> Der WPF-Kopf verteilt dasselbe auf vierzehn
/// Teildateien — die sind aber kein Vorbild für den Aufbau, sondern eine Liste dessen, was
/// es alles gibt (HANDOFF §6). Für Meilenstein M1 zählen vier Dinge: Seiten anzeigen,
/// zeichnen, radieren, speichern. Sticker, Texterkennung, Zahlenblock, Schnellaktionen und
/// das Geodreieck sind ausdrücklich nicht M1 und stehen deshalb hier auch nicht — ein
/// halbes Feature ist schlechter als ein fehlendes, weil niemand ihm ansieht, dass es halb
/// ist.
/// </para>
///
/// <para>
/// <b>Gezeichnet wird von <c>WbRenderer</c> aus Core</b> — derselbe Code, den der WPF-Kopf
/// und der PDF-Export benutzen, mit denselben Pixelhashes unter beiden Systemen (§4.6).
/// Hier steht nur, wie man an einen <see cref="SKCanvas"/> kommt
/// (<see cref="SkiaCanvas"/>) und wie Eingaben hereinkommen.
/// </para>
/// </summary>
public partial class WhiteboardView : UserControl
{
    private WhiteboardTabViewModel? _vm;
    private WbPage? _page;

    // ---- Werkzeugzustand ----
    private ToolType _tool = ToolType.Pen;
    private string _colorTag = "auto";
    private float _width = 2.5f;
    private bool _suppressToolEvents;

    /// <summary>
    /// Radius des Radierkreises in Punkten. Eigener Wert neben <see cref="_width"/>, damit
    /// der Radierer die Strichstärke der Stifte nicht überschreibt — der Schieber bedient je
    /// nach Werkzeug den einen oder anderen.
    /// </summary>
    private float _eraserRadius = 14f;

    // ---- Eingabezustand ----
    private bool _drawing;
    private List<WbPoint>? _activePoints;
    private bool _panning;
    private Point _panLast;
    private bool _spaceDown;

    /// <summary>Der Stift liegt auf dem Radiergummi-Ende (Avalonia meldet das als eigenen Zeigertyp).</summary>
    private bool _stylusInverted;

    // ---- Radierer ----
    private List<EraseStep>? _eraseSteps;
    private SKPoint _eraserPos;
    private bool _eraserVisible;

    // ---- Auswahl ----
    private List<SKPoint>? _lassoPts;
    private readonly HashSet<WbElement> _selection = new();
    private SKRect _selectionBounds;
    private bool _movingSelection;
    private SKPoint _moveLast;
    private float _movedX, _movedY;

    private ToggleButton[] ToolButtons =>
        [BtnPen, BtnPencil, BtnHighlighter, BtnEraser, BtnLasso, BtnMove, BtnPan];

    public WhiteboardView()
    {
        // **Nicht AvaloniaXamlLoader.Load(this).** Nur das erzeugte InitializeComponent()
        // weist danach die x:Name-Felder zu; mit dem Lader direkt bliebe jedes davon null
        // und der erste Zugriff wirft an einer Stelle, die mit der Ursache nichts zu tun hat
        // (HANDOFF §7, „AvaloniaXamlLoader.Load füllt die x:Name-Felder nicht").
        InitializeComponent();

        _suppressToolEvents = true;
        BtnPen.IsChecked = true;
        _suppressToolEvents = false;

        Skia.Paint += OnPaint;

        // Die Seite mittig setzen, sobald die Fläche zum ersten Mal eine Breite hat. Der
        // WPF-Kopf macht das im Zeichenpfad; hier geht das nicht (Begründung in
        // WhiteboardView.Render.cs, OnPaint) — und die Größe ist ohnehin das Ereignis, um
        // das es dabei wirklich geht.
        Skia.SizeChanged += (_, _) =>
        {
            if (_vm == null || _vm.ViewInitialized || Skia.Bounds.Width <= 0) return;
            CenterView();
            UpdateZoomLabel();
            _vm.ViewInitialized = true;
            Neuzeichnen();
        };

        // Eingabe. Tunnel bei Pressed/Moved, damit die Fläche den Zeiger sicher bekommt,
        // bevor ein darüberliegendes Steuerelement ihn beansprucht.
        Skia.PointerPressed += OnPointerPressed;
        Skia.PointerMoved += OnPointerMoved;
        Skia.PointerReleased += OnPointerReleased;
        Skia.PointerExited += OnPointerExited;
        Skia.PointerWheelChanged += OnPointerWheel;

        AttachedToVisualTree += (_, _) =>
        {
            App.Platform.Theme.ThemeChanged += OnThemeChanged;
            Loc.LanguageChanged += OnLanguageChanged;
            Neuzeichnen();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            App.Platform.Theme.ThemeChanged -= OnThemeChanged;
            Loc.LanguageChanged -= OnLanguageChanged;
            InhaltVerwerfen();
        };

        DataContextChanged += OnDataContextChanged;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);

        SyncSizeControls();
    }

    private void OnThemeChanged()
    {
        RefreshAutoSwatch();
        Neuzeichnen();
    }

    /// <summary>Ein neues Bild anfordern. Alles, was den Inhalt ändert, ruft das.</summary>
    private void Neuzeichnen() => Skia.InvalidateVisual();

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm != null) _vm.Undo.Changed -= OnUndoChanged;

        _vm = DataContext as WhiteboardTabViewModel;
        if (_vm == null) return;

        _vm.Undo.Changed += OnUndoChanged;
        _vm.PageIndex = Math.Clamp(_vm.PageIndex, 0, _vm.Doc.Pages.Count - 1);
        _page = _vm.Doc.Pages[_vm.PageIndex];

        // Die Seitenleiste gehört zum gehefteten Notizbuch; die unendliche Fläche hat keine
        // Seiten, über die man blättern könnte.
        PageBar.IsVisible = !_page.IsInfinite;

        UpdatePageLabel();
        OnUndoChanged();
        UpdateZoomLabel();
        RefreshAutoSwatch();
        EinstellungenSpiegeln();
        Neuzeichnen();
    }

    private void OnUndoChanged()
    {
        if (_vm == null) return;
        BtnUndo.IsEnabled = _vm.Undo.CanUndo;
        BtnRedo.IsEnabled = _vm.Undo.CanRedo;
    }

    /// <summary>
    /// Der Grund, warum ein Dokument überhaupt gespeichert wird. <c>MainViewModel</c>
    /// schreibt die Registerkarte weg, sobald das gesetzt ist — die Fläche selbst kennt die
    /// Datenbank nicht.
    /// </summary>
    private void MarkDirty()
    {
        if (_vm != null) _vm.IsDirty = true;
    }

    // ==================== Werkzeuge ====================

    private void Tool_Changed(object? sender, RoutedEventArgs e)
    {
        if (_suppressToolEvents || sender is not ToggleButton btn) return;

        if (btn.IsChecked != true)
        {
            // Das aktive Werkzeug lässt sich nicht abwählen — irgendeines muss es sein.
            if (ToolButtons.All(b => b.IsChecked != true))
            {
                _suppressToolEvents = true;
                btn.IsChecked = true;
                _suppressToolEvents = false;
            }
            return;
        }

        _suppressToolEvents = true;
        foreach (var b in ToolButtons)
            if (b != btn) b.IsChecked = false;
        _suppressToolEvents = false;

        SetTool(Enum.Parse<ToolType>((string)btn.Tag!));
    }

    private void SetTool(ToolType tool)
    {
        // Die Auswahl bleibt nur bei den Auswahl-Werkzeugen stehen.
        if (tool != ToolType.Lasso && tool != ToolType.Move) ClearSelection();

        _tool = tool;
        _eraserVisible = false;
        SyncSizeControls();

        Skia.Cursor = new Cursor(tool switch
        {
            ToolType.Eraser => StandardCursorType.Cross,
            ToolType.Pan => StandardCursorType.Hand,
            ToolType.Lasso => StandardCursorType.Cross,
            _ => StandardCursorType.Arrow,
        });
        Neuzeichnen();
    }

    private void Color_Changed(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true, Tag: string tag } && tag.Length > 0)
            _colorTag = tag;
    }

    /// <summary>
    /// Die Farbe, mit der gezeichnet wird. „auto" heißt: dunkel auf hellem, hell auf
    /// dunklem Papier — und die Vorgabefarbe kommt aus der Farbtabelle in Core, nicht aus
    /// einer Konstante hier.
    /// </summary>
    private string CurrentInkHex() =>
        !string.IsNullOrEmpty(_colorTag) && _colorTag != "auto"
            ? _colorTag
            : AutoTinte().ToString();

    /// <summary>
    /// Die Vorgabetinte. <b>Sie gehört zum Papier, nicht zur App.</b>
    ///
    /// <para>
    /// Das ist ein Fehler, der beim Gegenprüfen am laufenden Programm aufgefallen ist und
    /// vorher niemandem: Eine Notizbuchseite ist standardmäßig <see cref="PageShade.Light"/>
    /// — <b>unabhängig vom App-Theme</b>, denn Papier soll wie Papier aussehen (V1-Vorgabe
    /// „Dark/Light bei hellem Papier", HANDOFF §1). Wer die Tinte aus der *aktiven* Tabelle
    /// nimmt, holt sich im Dunkelmodus deren helles <see cref="ThemeColor.DefaultInk"/> —
    /// und schreibt hell auf weiß. Der Strich ist dann da, gespeichert und exportierbar,
    /// nur eben unsichtbar; auf einem Bildschirmfoto sieht es aus, als käme die Eingabe
    /// nicht an.
    /// </para>
    /// <para>
    /// Die Regel ist deshalb dieselbe wie eine Zeile weiter oben beim Papier selbst: bei
    /// <see cref="PageShade.Auto"/> folgt die Seite dem Theme, also auch die Tinte; bei
    /// einem festgelegten Farbton zählt <b>der</b>, also die mitgelieferte Tabelle dazu.
    /// </para>
    /// </summary>
    private HexColor AutoTinte()
    {
        if (_page == null || _page.Shade == PageShade.Auto)
            return AvaloniaThemeHost.Current[ThemeColor.DefaultInk];

        return (_page.Shade == PageShade.Dark ? Themes.Dark : Themes.Light)[ThemeColor.DefaultInk];
    }

    /// <summary>Effektiver Farbton der Seite — „Auto" folgt dem App-Theme.</summary>
    private static PageShade EffectiveShade(WbPage? page)
    {
        if (page != null && page.Shade != PageShade.Auto) return page.Shade;
        return App.Platform.Theme.Current == AppTheme.Dark ? PageShade.Dark : PageShade.Light;
    }

    /// <summary>
    /// Hält die erste Farbkachel synchron zur Seite: schwarz auf hellen, weiß auf dunklen.
    /// <para>
    /// <b>Wird von den Ereignissen gerufen, die es auslösen</b> — Dokumentwechsel,
    /// Seitenwechsel, Theme-Wechsel — und ausdrücklich <b>nicht</b> aus dem Zeichenpfad,
    /// wie es der WPF-Kopf tut. Dort ist das bequem und billig; hier wäre es ein Zugriff auf
    /// ein fremdes Steuerelement mitten im Renderdurchlauf und damit ein Abbruch desselben
    /// (Begründung in WhiteboardView.Render.cs, OnPaint).
    /// </para>
    /// </summary>
    private HexColor? _autoSwatch;

    private void RefreshAutoSwatch()
    {
        // Genau die Farbe, mit der auch gezeichnet wird — die Kachel ist eine Vorschau und
        // darf sich ihre Farbe nicht selbst ausdenken.
        var farbe = AutoTinte();
        if (_autoSwatch == farbe) return;
        _autoSwatch = farbe;
        AutoSwatch.Background = farbe.ToBrush();
    }

    // ---- Größe (Strichstärke bzw. Radierradius) ----

    private bool SizeControlsEraser => EffectiveTool == ToolType.Eraser;

    private float ActiveSize => SizeControlsEraser ? _eraserRadius : _width;

    private void WidthSlider_Changed(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != RangeBase.ValueProperty || WidthSlider == null) return;

        if (SizeControlsEraser) _eraserRadius = (float)WidthSlider.Value;
        else _width = (float)WidthSlider.Value;

        if (WidthLabel != null) WidthLabel.Text = ActiveSize.ToString("0.#");
        if (_eraserVisible) Neuzeichnen();   // den Radierkreis sofort mitwachsen lassen
    }

    /// <summary>Stellt Schieber und Anzeige auf das aktive Werkzeug um.</summary>
    private void SyncSizeControls()
    {
        if (WidthSlider == null || WidthLabel == null) return;

        ToolTip.SetTip(WidthSlider, Loc.T(SizeControlsEraser ? "Size.Eraser.Tip" : "Size.Tip"));
        WidthSlider.Value = ActiveSize;
        WidthLabel.Text = ActiveSize.ToString("0.#");
    }

    /// <summary>
    /// Texte, die der Code setzt, hängen an keiner Bindung und müssen nach einem
    /// Sprachwechsel neu geschrieben werden (HANDOFF §7, „Texte, die der Code setzt").
    /// </summary>
    private void OnLanguageChanged()
    {
        UpdatePageLabel();
        SyncSizeControls();
    }

    // ==================== Rückgängig ====================

    private void Undo_Click(object? sender, RoutedEventArgs e) => DoUndo();
    private void Redo_Click(object? sender, RoutedEventArgs e) => DoRedo();

    private void DoUndo()
    {
        if (_vm == null) return;
        ClearSelection();
        var page = _vm.Undo.Undo();
        if (page != null) { NavigateToPage(page); MarkDirty(); Neuzeichnen(); }
    }

    private void DoRedo()
    {
        if (_vm == null) return;
        ClearSelection();
        var page = _vm.Undo.Redo();
        if (page != null) { NavigateToPage(page); MarkDirty(); Neuzeichnen(); }
    }

    /// <summary>
    /// Springt zu der Seite, auf der ein rückgängig gemachter Schritt lag. Ohne das
    /// verschwände auf einer anderen Seite still etwas, das niemand sieht.
    /// </summary>
    private void NavigateToPage(WbPage page)
    {
        if (_vm == null || page == _page) return;
        int idx = _vm.Doc.Pages.IndexOf(page);
        if (idx < 0) return;
        _vm.PageIndex = idx;
        _page = page;
        UpdatePageLabel();
        RefreshAutoSwatch();
        EinstellungenSpiegeln();
    }

    // ==================== Auswahl ====================

    private void ClearSelection()
    {
        _selection.Clear();
        _movingSelection = false;
        Neuzeichnen();
    }

    private SKRect InflatedSelectionBounds()
    {
        var b = _selectionBounds;
        b.Inflate(12f / Zoom, 12f / Zoom);
        return b;
    }

    private void ComputeSelectionBounds() => _selectionBounds = WbHit.Bounds(_selection);

    private void SelectAll()
    {
        if (_page == null) return;
        _selection.Clear();
        foreach (var el in _page.Elements) _selection.Add(el);
        if (_selection.Count > 0) ComputeSelectionBounds();
        Neuzeichnen();
    }

    private void DeleteSelection()
    {
        if (_page == null || _vm == null || _selection.Count == 0) return;
        var aktion = new RemoveElementsAction(_page, _selection);
        aktion.Redo(_page);
        _vm.Undo.Push(_page, aktion);
        _selection.Clear();
        MarkDirty();
        Neuzeichnen();
    }

    // ==================== Zoom und Verschieben ====================

    private float Zoom { get => _vm?.Zoom ?? 1f; set { if (_vm != null) _vm.Zoom = value; } }
    private float PanX { get => _vm?.PanX ?? 0f; set { if (_vm != null) _vm.PanX = value; } }
    private float PanY { get => _vm?.PanY ?? 0f; set { if (_vm != null) _vm.PanY = value; } }

    private void UpdateZoomLabel() => ZoomLabel.Content = $"{Zoom * 100:0} %";

    private void ZoomAt(Point mitte, float faktor)
    {
        float neu = Math.Clamp(Zoom * faktor, 0.15f, 8f);
        faktor = neu / Zoom;
        PanX = (float)(mitte.X - (mitte.X - PanX) * faktor);
        PanY = (float)(mitte.Y - (mitte.Y - PanY) * faktor);
        Zoom = neu;
        UpdateZoomLabel();
        Neuzeichnen();
    }

    private Point FlaechenMitte() => new(Skia.Bounds.Width / 2, Skia.Bounds.Height / 2);

    private void ZoomIn_Click(object? sender, RoutedEventArgs e) => ZoomAt(FlaechenMitte(), 1.25f);
    private void ZoomOut_Click(object? sender, RoutedEventArgs e) => ZoomAt(FlaechenMitte(), 0.8f);

    private void ZoomReset_Click(object? sender, RoutedEventArgs e)
    {
        Zoom = 1f;
        CenterView();
        UpdateZoomLabel();
        Neuzeichnen();
    }

    private void CenterView()
    {
        if (_page == null) return;
        if (_page.IsInfinite)
        {
            PanX = 32; PanY = 32;
        }
        else
        {
            PanX = Math.Max(24f, (float)(Skia.Bounds.Width - _page.Width * Zoom) / 2f);
            PanY = 24;
        }
    }

    /// <summary>Punkt der Fläche → Leinwandkoordinaten.</summary>
    private SKPoint ToCanvas(Point p) => new((float)((p.X - PanX) / Zoom), (float)((p.Y - PanY) / Zoom));

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        var m = e.KeyModifiers;
        if (m.HasFlag(KeyModifiers.Control))
            ZoomAt(e.GetPosition(Skia), (float)Math.Pow(1.1, e.Delta.Y));
        else if (m.HasFlag(KeyModifiers.Shift))
        {
            PanX += (float)e.Delta.Y * 60f;
            Neuzeichnen();
        }
        else
        {
            PanX += (float)e.Delta.X * 60f;
            PanY += (float)e.Delta.Y * 60f;
            Neuzeichnen();
        }
        e.Handled = true;
    }

    // ==================== Tastatur ====================

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_vm == null) return;
        bool strg = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (strg)
        {
            switch (e.Key)
            {
                case Key.Z: DoUndo(); e.Handled = true; return;
                case Key.Y: DoRedo(); e.Handled = true; return;
                case Key.A: SelectAll(); e.Handled = true; return;
            }
            return;
        }

        switch (e.Key)
        {
            case Key.Delete:
            case Key.Back:
                DeleteSelection();
                e.Handled = true;
                return;
            case Key.Escape:
                ClearSelection();
                e.Handled = true;
                return;
            case Key.Space:
                _spaceDown = true;
                return;
            case Key.F9:
                // Die Stift-Anzeige. Kein Menüeintrag: sie ist ein Messgerät für diese
                // Portierungsphase, kein Feature (Begründung bei DrawStiftAnzeige).
                _stiftAnzeige = !_stiftAnzeige;
                Neuzeichnen();
                e.Handled = true;
                return;
        }

        // Werkzeug-Kürzel — dieselben Buchstaben wie im WPF-Kopf.
        ToggleButton? btn = e.Key switch
        {
            Key.S => BtnPen,
            Key.B => BtnPencil,
            Key.M => BtnHighlighter,
            Key.E => BtnEraser,
            Key.L => BtnLasso,
            Key.V => BtnMove,
            Key.H => BtnPan,
            _ => null,
        };
        if (btn != null)
        {
            btn.IsChecked = true;
            e.Handled = true;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space) _spaceDown = false;
    }

    // ==================== Seiten ====================

    private void UpdatePageLabel()
    {
        if (_vm == null) return;
        var seiten = _vm.Doc.Pages;
        int cover = seiten.Count(pg => pg.IsCover);
        int gesamt = seiten.Count - cover;

        if (seiten[_vm.PageIndex].IsCover)
        {
            PageLabel.Text = Loc.T("Page.Cover");
            return;
        }

        int nr = 0;
        for (int i = 0; i <= _vm.PageIndex; i++)
            if (!seiten[i].IsCover) nr++;
        PageLabel.Text = Loc.T("Page.Label", nr, gesamt);
    }

    private void GoToPage(int idx)
    {
        if (_vm == null) return;
        idx = Math.Clamp(idx, 0, _vm.Doc.Pages.Count - 1);
        if (idx == _vm.PageIndex) return;

        ClearSelection();
        _vm.PageIndex = idx;
        _page = _vm.Doc.Pages[idx];
        InhaltVerwerfen();
        UpdatePageLabel();
        RefreshAutoSwatch();
        EinstellungenSpiegeln();
        Neuzeichnen();
    }

    private void PrevPage_Click(object? sender, RoutedEventArgs e) => GoToPage((_vm?.PageIndex ?? 0) - 1);
    private void NextPage_Click(object? sender, RoutedEventArgs e) => GoToPage((_vm?.PageIndex ?? 0) + 1);

    private void AddPage_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;

        WbPage seite;
        if (_vm.Doc.NewPageTemplate != null)
            seite = _vm.Doc.PageFromTemplate();
        else if (_page is { IsCover: false, IsInfinite: false })
            // Ohne Vorlage: die aktuelle Seite fortführen.
            seite = new WbPage
            {
                Width = _page.Width,
                Height = _page.Height,
                Background = _page.Background,
                Shade = _page.Shade,
            };
        else
            seite = WhiteboardDoc.NewNotebookPage();

        _vm.Doc.Pages.Insert(_vm.PageIndex + 1, seite);
        MarkDirty();
        GoToPage(_vm.PageIndex + 1);
        UpdatePageLabel();
    }

    private void DeletePage_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || _vm.Doc.Pages.Count <= 1 || _page == null) return;

        // Nur nachfragen, wenn etwas verloren ginge — eine leere Seite ist keine Rückfrage
        // wert. `MessageWindow.Zeige` blockiert (Modal.PushFrame, HANDOFF §7), verhält sich
        // hier also wie `MessageBox.Show` im WPF-Kopf. Das ist genau der zulässige Fall:
        // vom Oberflächen-Faden aufgerufen, für etwas, worauf der Nutzer ohnehin wartet.
        if ((_page.Elements.Count > 0 || _page.HasBackgroundImage) &&
            !MessageWindow.Zeige(TopLevel.GetTopLevel(this) as Window,
                Loc.T("Msg.DeletePage"), DialogSeverity.Warning, frage: true))
            return;

        int idx = _vm.PageIndex;
        _vm.Doc.Pages.RemoveAt(idx);
        _vm.PageIndex = Math.Min(idx, _vm.Doc.Pages.Count - 1);
        _page = _vm.Doc.Pages[_vm.PageIndex];
        InhaltVerwerfen();
        MarkDirty();
        UpdatePageLabel();
        RefreshAutoSwatch();
        EinstellungenSpiegeln();
        Neuzeichnen();
    }
}
