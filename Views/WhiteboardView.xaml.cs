using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using GonkNote.Core.Models;
using GonkNote.Services;
using GonkNote.Core.Services;
using GonkNote.ViewModels;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// SkiaSharp-Zeichenfläche für Whiteboards und Notizbücher.
/// Stylus-Eingabe mit Druckstärke, Maus als Fallback, Finger = Verschieben.
/// </summary>
public partial class WhiteboardView : UserControl
{
    private WhiteboardTabViewModel? _vm;
    private WbPage? _page;

    // Werkzeugzustand
    private ToolType _tool = ToolType.Pen;
    private ShapeKind _shape = ShapeKind.Rectangle;
    private string _colorTag = "auto";
    private float _width = 2.5f;
    private bool _suppressToolEvents;

    // Formfüllung
    private bool _shapeFillOn;
    private string _shapeFillRgb = "#14B8A6";
    private float _shapeFillOpacity = 0.4f;

    // Eingabezustand
    private bool _drawing;
    private List<WbPoint>? _activePoints;
    private SKPoint _shapeStart, _shapeCur;
    private bool _shapeActive;
    private bool _panning;
    private Point _panLast;
    private bool _spaceDown;
    private bool _stylusInverted;

    // Radierer (punktgenau: Striche werden aufgetrennt statt komplett gelöscht)
    private List<EraseStep>? _eraseSteps;
    private SKPoint _eraserPos;
    private bool _eraserVisible;
    /// <summary>
    /// Radius des Radierkreises in Bildschirmpunkten (bleibt beim Zoomen gleich groß, wie eine
    /// echte Radiergummi-Spitze). Eigener Wert, damit der Radierer die Strichstärke der Stifte
    /// nicht überschreibt; der Größen-Schieber bedient je nach Werkzeug den einen oder anderen.
    /// </summary>
    private float _eraserRadius = 14f;

    // Lasso / Auswahl
    private List<SKPoint>? _lassoPts;
    private readonly HashSet<WbElement> _selection = new();
    private SKRect _selectionBounds;
    private bool _movingSelection;
    private SKPoint _moveLast;
    private float _movedX, _movedY;

    // Gleichmäßige Skalierung einer beliebigen Auswahl (Striche/Formen/mehrere Objekte)
    private bool _scalingSelection;
    private SKPoint _scalePivot;      // Ankerpunkt (oben links der Auswahl)
    private float _scaleStartDist;    // Abstand Pivot→Zeiger beim Anfassen
    private float _scaleAccum = 1f;   // bisher angewandter Gesamtfaktor (für Undo)

    // Drehen eines einzelnen Elements (Griff über der Auswahl)
    private WbElement? _rotatingEl;
    private float _rotStartDeg;       // Rotation des Elements beim Anfassen
    private float _rotStartPointer;   // Zeigerwinkel beim Anfassen

    // Texteingabe (Textfeld)
    private TextElement? _editingText;
    private bool _editingIsNew;
    private string _editingOldText = "";
    private bool _cancelEdit;

    // Notizzettel-Bearbeitung
    private StickyNoteElement? _editingSticky;
    private bool _editingStickyIsNew;
    private string _editingStickyOld = "";
    private string _stickyColorHex = "#FFFEF08A";

    // Zeichenhilfen: Lineal & Geodreieck (transient, werden nicht gespeichert)
    private enum DrawAid { None, Ruler, SetSquare }
    private enum RulerDrag { None, Move, Rotate }
    private DrawAid _aid = DrawAid.None;
    private bool _aidPlaced;
    private SKPoint _aidCenter;
    private float _aidAngleDeg;
    private RulerDrag _rulerDrag;
    private SKPoint _rulerDragLast;
    // Einrasten eines Strichs auf eine Kante der aktiven Zeichenhilfe
    private bool _rulerSnapActive;
    private SKPoint _rulerSnapE0, _rulerSnapDir;

    // Maße in Canvas-Einheiten (96 DPI): 1 cm ≈ 37,8 px
    private const float RulerLength = 680f;
    private const float RulerHalfWidth = 26f;
    private const float RulerSnapDist = 26f;
    private const float PxPerCm = 37.795f;
    // Winkel-Einrasten (Fresco-Stil): magnetische Rastung an 15°-Vielfachen
    private const float RulerAngleStep = 15f;
    private const float RulerAngleSnapTol = 4f;

    private ToggleButton[] ToolButtons => new[]
    {
        BtnPen, BtnSmoothPen, BtnPencil, BtnHighlighter, BtnEraser,
        BtnMove, BtnLasso, BtnText, BtnSticky, BtnSticker, BtnPan,
    };

    // Formen-Gruppe (Kind-Auswahl in der Toolbar, klappbar wie die Stifte)
    private ToggleButton[] ShapeButtons => new[] { BtnShapeRect, BtnShapeLine, BtnShapeArrow, BtnShapeEllipse, BtnShapeTriangle };
    private ShapeKind _lastShape = ShapeKind.Rectangle;
    private bool _shapeGroupExpanded;

    private ToggleButton ShapeButtonFor(ShapeKind k) => k switch
    {
        ShapeKind.Line => BtnShapeLine,
        ShapeKind.Arrow => BtnShapeArrow,
        ShapeKind.Ellipse => BtnShapeEllipse,
        ShapeKind.Triangle => BtnShapeTriangle,
        _ => BtnShapeRect,
    };

    private void SetShapeGroupExpanded(bool expanded)
    {
        _shapeGroupExpanded = expanded;
        var rep = ShapeButtonFor(_lastShape);
        foreach (var b in ShapeButtons)
            b.Visibility = expanded || b == rep ? Visibility.Visible : Visibility.Collapsed;
    }
    private ToggleButton[] PenButtons => new[] { BtnPen, BtnSmoothPen, BtnPencil, BtnHighlighter };

    // Stifte-Gruppe: eingeklappt ist nur der zuletzt benutzte Stift sichtbar
    private ToolType _lastPen = ToolType.Pen;
    private bool _penGroupExpanded;

    // Auswahl-Gruppe (Lasso + Verschieben): eingeklappt ist nur das zuletzt benutzte sichtbar
    private ToggleButton[] SelectButtons => new[] { BtnLasso, BtnMove };
    private ToolType _lastSelect = ToolType.Lasso;
    private bool _selectGroupExpanded;

    // Zeichenhilfen-Gruppe (Lineal/Geodreieck): eingeklappt ist nur die zuletzt benutzte sichtbar
    private ToggleButton[] AidButtons => new[] { BtnRuler, BtnSetSquare };
    private DrawAid _lastAid = DrawAid.Ruler;
    private bool _aidGroupExpanded;

    public WhiteboardView()
    {
        InitializeComponent();

        _suppressToolEvents = true;
        BtnPen.IsChecked = true;
        _suppressToolEvents = false;
        SetPenGroupExpanded(false);
        SetSelectGroupExpanded(false);
        SetShapeGroupExpanded(false);
        SetAidGroupExpanded(false);

        foreach (var b in ToolButtons) b.Unchecked += Tool_Unchecked;

        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            ThemeService.ThemeChanged += OnThemeChanged;
            Loc.LanguageChanged += OnLanguageChanged;
            Skia.InvalidateVisual();
        };
        Unloaded += (_, _) =>
        {
            ThemeService.ThemeChanged -= OnThemeChanged;
            Loc.LanguageChanged -= OnLanguageChanged;
        };

        // Eingabe
        CanvasHost.MouseDown += OnMouseDown;
        CanvasHost.MouseMove += OnMouseMove;
        CanvasHost.MouseUp += OnMouseUp;
        CanvasHost.MouseWheel += OnMouseWheel;
        CanvasHost.MouseLeave += (_, _) => { _eraserVisible = false; Skia.InvalidateVisual(); };
        CanvasHost.StylusDown += OnStylusDown;
        CanvasHost.StylusMove += OnStylusMove;
        CanvasHost.StylusUp += OnStylusUp;
        CanvasHost.TouchDown += OnTouchDown;
        CanvasHost.TouchMove += OnTouchMove;
        CanvasHost.TouchUp += OnTouchUp;

        // Schnellaktionen (Quick-Options) statt Rechtsklick-Menü: Maus-Rechtsklick
        // und – für den Stift – die zweite Taste (Barrel-Button). Letzterer wird
        // sowohl direkt (StylusButtonDown) als auch als RightTap-Geste erkannt; der
        // eigentliche Barrel-Zustand wird zusätzlich in OnStylusDown geprüft, damit
        // die zweite Taste zuverlässig öffnet und keinen Strich auslöst.
        CanvasHost.MouseRightButtonUp += OnCanvasRightButtonUp;
        CanvasHost.StylusSystemGesture += OnCanvasStylusSystemGesture;
        CanvasHost.StylusButtonDown += OnCanvasStylusButtonDown;

        PreviewKeyDown += OnPreviewKeyDown;
        PreviewKeyUp += OnPreviewKeyUp;
    }

    private void OnThemeChanged() => Skia.InvalidateVisual();

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.Undo.Changed -= OnUndoChanged;

        _vm = DataContext as WhiteboardTabViewModel;
        if (_vm == null) return;

        _vm.Undo.Changed += OnUndoChanged;
        _vm.PageIndex = Math.Clamp(_vm.PageIndex, 0, _vm.Doc.Pages.Count - 1);
        _page = _vm.Doc.Pages[_vm.PageIndex];

        bool paged = !_page.IsInfinite;
        PageBar.Visibility = paged ? Visibility.Visible : Visibility.Collapsed;
        UpdatePageLabel();
        OnUndoChanged();
        UpdateZoomLabel();
        if (SettingsPanel.Visibility == Visibility.Visible) RefreshSettingsPanel();
        Skia.InvalidateVisual();
    }

    private void OnUndoChanged()
    {
        if (_vm == null) return;
        BtnUndo.IsEnabled = _vm.Undo.CanUndo;
        BtnRedo.IsEnabled = _vm.Undo.CanRedo;
    }

    private void MarkDirty()
    {
        if (_vm != null) _vm.IsDirty = true;
    }

    // ==================== Werkzeuge / Toolbar ====================

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressToolEvents) return;
        var btn = (ToggleButton)sender;

        _suppressToolEvents = true;
        foreach (var b in ToolButtons)
            if (b != btn) b.IsChecked = false;
        _suppressToolEvents = false;

        SetTool(Enum.Parse<ToolType>((string)btn.Tag));

        // Ein Klick genügt: Stift auswählen klappt die Stifte-Gruppe aus,
        // ein anderes Werkzeug klappt sie wieder ein
        if (IsPenTool(_tool)) _lastPen = _tool;
        SetPenGroupExpanded(IsPenTool(_tool));

        if (IsSelectTool(_tool)) _lastSelect = _tool;
        SetSelectGroupExpanded(IsSelectTool(_tool));

        // Formen-Gruppe abwählen/einklappen, wenn ein anderes Werkzeug gewählt wird
        _suppressToolEvents = true;
        foreach (var b in ShapeButtons) b.IsChecked = false;
        _suppressToolEvents = false;
        SetShapeGroupExpanded(false);
    }

    private void Tool_Unchecked(object? sender, RoutedEventArgs e)
    {
        if (_suppressToolEvents) return;
        // Aktives Werkzeug lässt sich nicht abwählen
        var btn = (ToggleButton)sender!;
        if (ToolButtons.All(b => b.IsChecked != true))
        {
            _suppressToolEvents = true;
            btn.IsChecked = true;
            _suppressToolEvents = false;
        }
    }

    private static bool IsPenTool(ToolType t) =>
        t is ToolType.Pen or ToolType.SmoothPen or ToolType.Pencil or ToolType.Highlighter;

    private ToggleButton PenButtonFor(ToolType t) => t switch
    {
        ToolType.SmoothPen => BtnSmoothPen,
        ToolType.Pencil => BtnPencil,
        ToolType.Highlighter => BtnHighlighter,
        _ => BtnPen,
    };

    private void SetPenGroupExpanded(bool expanded)
    {
        _penGroupExpanded = expanded;
        var rep = PenButtonFor(_lastPen);
        foreach (var b in PenButtons)
            b.Visibility = expanded || b == rep ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool IsSelectTool(ToolType t) => t is ToolType.Lasso or ToolType.Move;

    private ToggleButton SelectButtonFor(ToolType t) => t == ToolType.Move ? BtnMove : BtnLasso;

    private void SetSelectGroupExpanded(bool expanded)
    {
        _selectGroupExpanded = expanded;
        var rep = SelectButtonFor(_lastSelect);
        foreach (var b in SelectButtons)
            b.Visibility = expanded || b == rep ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetTool(ToolType tool)
    {
        CommitActiveEdit();
        HideQuickMenu();
        // Auswahl bleibt nur bei den Auswahl-Werkzeugen (Lasso, Verschieben) erhalten
        if (tool != ToolType.Lasso && tool != ToolType.Move) ClearSelection();

        _tool = tool;
        _eraserVisible = false;
        SyncSizeControls();

        // Formen-/Notizzettel-Werkzeug: Einstellungs-Panel mit passender Sektion öffnen
        if (tool == ToolType.Shape && SettingsPanel.Visibility != Visibility.Visible)
        {
            RefreshSettingsPanel();
            ShapeSection.IsExpanded = true;
            SettingsPanel.Visibility = Visibility.Visible;
        }
        else if (tool == ToolType.Sticky && SettingsPanel.Visibility != Visibility.Visible)
        {
            RefreshSettingsPanel();
            StickySection.IsExpanded = true;
            SettingsPanel.Visibility = Visibility.Visible;
        }
        else if (tool == ToolType.Sticker)
        {
            EnsureStickersLoaded();
            RefreshSettingsPanel();
            StickerSection.IsExpanded = true;
            SettingsPanel.Visibility = Visibility.Visible;
        }
        else if (SettingsPanel.Visibility == Visibility.Visible)
        {
            // Sichtbarkeit der werkzeugspezifischen Sektionen ans neue Werkzeug anpassen
            RefreshSettingsPanel();
        }

        CanvasHost.Cursor = tool switch
        {
            ToolType.Pen or ToolType.SmoothPen or ToolType.Pencil or ToolType.Highlighter => Cursors.Pen,
            ToolType.Eraser => Cursors.None,
            ToolType.Text => Cursors.IBeam,
            ToolType.Shape => Cursors.Cross,
            ToolType.Sticky => Cursors.Cross,
            ToolType.Sticker => Cursors.Arrow,
            ToolType.Pan => Cursors.Hand,
            ToolType.Move => Cursors.Arrow,
            _ => Cursors.Arrow,
        };
        Skia.InvalidateVisual();
    }

    /// <summary>Ein Formen-Werkzeug in der Toolbar: Formen-Werkzeug aktivieren + Art setzen.</summary>
    private void ShapeTool_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressToolEvents) return;
        var btn = (ToggleButton)sender;

        _suppressToolEvents = true;
        foreach (var b in ToolButtons) b.IsChecked = false;            // andere Werkzeuge aus
        foreach (var b in ShapeButtons) if (b != btn) b.IsChecked = false;
        btn.IsChecked = true;
        _suppressToolEvents = false;

        _shape = Enum.Parse<ShapeKind>((string)btn.Tag);
        _lastShape = _shape;
        SetTool(ToolType.Shape);
        SetPenGroupExpanded(false);
        SetSelectGroupExpanded(false);
        SetShapeGroupExpanded(true);
    }

    private void Color_Checked(object sender, RoutedEventArgs e)
    {
        // Tag kann während des XAML-Ladens noch fehlen → Standard behalten
        if (((RadioButton)sender).Tag is string tag && tag.Length > 0)
            _colorTag = tag;
    }

    /// <summary>Aktuelle Füllfarbe inkl. Deckkraft oder null, wenn Füllung aus ist.</summary>
    private string? CurrentFill()
    {
        if (!_shapeFillOn) return null;
        var c = ParseColor(_shapeFillRgb);
        byte a = (byte)Math.Round(_shapeFillOpacity * 255);
        return $"#{a:X2}{c.Red:X2}{c.Green:X2}{c.Blue:X2}";
    }

    private void UpdateFillPreview()
    {
        var c = ParseColor(_shapeFillRgb);
        byte a = (byte)Math.Round(_shapeFillOpacity * 255);
        FillPreviewRect.Fill = new SolidColorBrush(Color.FromArgb(a, c.Red, c.Green, c.Blue));
    }

    private void ShapeFill_Changed(object sender, RoutedEventArgs e)
    {
        _shapeFillOn = BtnShapeFill.IsChecked == true;
    }

    private void PickFillColor_Click(object sender, RoutedEventArgs e)
    {
        var cur = ParseColor(_shapeFillRgb);
        var initial = Color.FromRgb(cur.Red, cur.Green, cur.Blue);
        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial, allowAlpha: false) is not { } c) return;

        _shapeFillRgb = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        _shapeFillOn = true;
        BtnShapeFill.IsChecked = true;
        UpdateFillPreview();
    }

    private void FillOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _shapeFillOpacity = (float)(e.NewValue / 100.0);
        if (FillOpacityLabel != null) FillOpacityLabel.Text = $"{e.NewValue:0} %";
        if (FillPreviewRect != null) UpdateFillPreview();
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        // Startwert: aktuelle Farbe
        var cur = ParseColor(CurrentInkHex());
        var initial = Color.FromArgb(cur.Alpha, cur.Red, cur.Green, cur.Blue);

        if (ColorPickerDialog.Pick(Window.GetWindow(this), initial) is not { } c) return;

        string hex = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        CustomSwatch.Background = new SolidColorBrush(c);
        CustomSwatch.Tag = hex;
        CustomSwatch.Visibility = Visibility.Visible;
        _colorTag = hex;
        CustomSwatch.IsChecked = true;
    }

    // ---- Größen-Schieber / Zahlenblock ----
    // Beim Radierer stellt der Schieber dessen Radius ein, sonst die Strichstärke. Beide Werte
    // werden getrennt gemerkt, damit ein Werkzeugwechsel den jeweils anderen nicht überschreibt.

    private bool SizeControlsEraser => _tool == ToolType.Eraser;

    /// <summary>Die Größe, die Schieber und Zahlenblock gerade bedienen.</summary>
    private float ActiveSize => SizeControlsEraser ? _eraserRadius : _width;

    private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SizeControlsEraser) _eraserRadius = (float)e.NewValue;
        else _width = (float)e.NewValue;

        if (WidthLabel != null) WidthLabel.Content = ActiveSize.ToString("0.#");
        if (_eraserVisible) Skia.InvalidateVisual();   // Radierkreis sofort mitwachsen lassen
    }

    /// <summary>
    /// Texte, die der Code setzt (Seitenzähler, Größen-Tooltips), tragen die Sprache nicht
    /// über eine Bindung – nach einem Sprachwechsel werden sie neu geschrieben.
    /// </summary>
    private void OnLanguageChanged()
    {
        UpdatePageLabel();
        SyncSizeControls();
    }

    /// <summary>Stellt Schieber, Anzeige und Beschriftung auf das aktive Werkzeug um.</summary>
    private void SyncSizeControls()
    {
        if (WidthSlider == null || WidthLabel == null || WidthIcon == null) return;

        WidthSlider.ToolTip = WidthIcon.ToolTip = WidthLabel.ToolTip =
            Loc.T(SizeControlsEraser ? "Size.Eraser.Tip" : "Size.Tip");

        WidthSlider.Value = ActiveSize;
        WidthLabel.Content = ActiveSize.ToString("0.#");
    }

    private void Undo_Click(object sender, RoutedEventArgs e) => DoUndo();
    private void Redo_Click(object sender, RoutedEventArgs e) => DoRedo();

    private void DoUndo()
    {
        if (_vm == null) return;
        CommitActiveEdit();
        ClearSelection();
        var page = _vm.Undo.Undo();
        if (page != null) { NavigateToPage(page); MarkDirty(); Skia.InvalidateVisual(); }
    }

    private void DoRedo()
    {
        if (_vm == null) return;
        CommitActiveEdit();
        ClearSelection();
        var page = _vm.Undo.Redo();
        if (page != null) { NavigateToPage(page); MarkDirty(); Skia.InvalidateVisual(); }
    }

    private void NavigateToPage(WbPage page)
    {
        if (_vm == null || page == _page) return;
        int idx = _vm.Doc.Pages.IndexOf(page);
        if (idx < 0) return;
        _vm.PageIndex = idx;
        _page = page;
        UpdatePageLabel();
    }

    // ==================== Zoom / Pan ====================

    private float Zoom { get => _vm?.Zoom ?? 1f; set { if (_vm != null) _vm.Zoom = value; } }
    private float PanX { get => _vm?.PanX ?? 0f; set { if (_vm != null) _vm.PanX = value; } }
    private float PanY { get => _vm?.PanY ?? 0f; set { if (_vm != null) _vm.PanY = value; } }

    private void UpdateZoomLabel() => ZoomLabel.Content = $"{Zoom * 100:0} %";

    private void ZoomAt(Point screenCenter, float factor)
    {
        float newZoom = Math.Clamp(Zoom * factor, 0.15f, 8f);
        factor = newZoom / Zoom;
        PanX = (float)(screenCenter.X - (screenCenter.X - PanX) * factor);
        PanY = (float)(screenCenter.Y - (screenCenter.Y - PanY) * factor);
        Zoom = newZoom;
        UpdateZoomLabel();
        Skia.InvalidateVisual();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) =>
        ZoomAt(new Point(CanvasHost.ActualWidth / 2, CanvasHost.ActualHeight / 2), 1.25f);

    private void ZoomOut_Click(object sender, RoutedEventArgs e) =>
        ZoomAt(new Point(CanvasHost.ActualWidth / 2, CanvasHost.ActualHeight / 2), 0.8f);

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        Zoom = 1f;
        CenterView();
        UpdateZoomLabel();
        Skia.InvalidateVisual();
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
            PanX = Math.Max(24f, (float)(CanvasHost.ActualWidth - _page.Width * Zoom) / 2f);
            PanY = 24;
        }
    }

    private SKPoint ToCanvas(Point p) => new((float)((p.X - PanX) / Zoom), (float)((p.Y - PanY) / Zoom));

    private Point ToScreen(SKPoint c) => new(c.X * Zoom + PanX, c.Y * Zoom + PanY);

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        HideQuickMenu();
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ZoomAt(e.GetPosition(CanvasHost), (float)Math.Pow(1.1, e.Delta / 120.0));
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            PanX += e.Delta * 0.5f;
            Skia.InvalidateVisual();
        }
        else
        {
            PanY += e.Delta * 0.5f;
            Skia.InvalidateVisual();
        }
        e.Handled = true;
    }

    // ==================== Tastatur ====================

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (EditBox.IsKeyboardFocused) return;
        if (_vm == null) return;

        bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

        if (ctrl && e.Key == Key.Z) { DoUndo(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.Y) { DoRedo(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.D) { DuplicateSelection(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.C) { CopySelection(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.X) { CutSelection(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.A) { SelectAll(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.V)
        {
            // Erst interne Element-Zwischenablage, sonst Bild aus der System-Zwischenablage
            if (_clipboard.Count > 0) PasteClipboard(null);
            else PasteImageFromClipboard();
            e.Handled = true;
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
        }

        if (ctrl) return;

        if (e.Key == Key.R) { SetAid(DrawAid.Ruler); e.Handled = true; return; }
        if (e.Key == Key.D) { SetAid(DrawAid.SetSquare); e.Handled = true; return; }

        ToggleButton? btn = e.Key switch
        {
            Key.S => BtnPen,
            Key.G => BtnSmoothPen,
            Key.B => BtnPencil,
            Key.M => BtnHighlighter,
            Key.E => BtnEraser,
            Key.V => BtnMove,
            Key.L => BtnLasso,
            Key.T => BtnText,
            Key.F => ShapeButtonFor(_lastShape),   // Formen-Werkzeug mit zuletzt genutzter Art
            Key.N => BtnSticky,
            Key.H => BtnPan,
            _ => null,
        };
        if (btn != null)
        {
            btn.IsChecked = true;
            e.Handled = true;
        }
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space) _spaceDown = false;
    }

    // ==================== Seiten ====================

    private void UpdatePageLabel()
    {
        if (_vm == null) return;
        var pages = _vm.Doc.Pages;
        int covers = pages.Count(pg => pg.IsCover);
        int contentTotal = pages.Count - covers;

        if (pages[_vm.PageIndex].IsCover)
        {
            PageLabel.Text = Loc.T("Page.Cover");
        }
        else
        {
            int num = 0;
            for (int i = 0; i <= _vm.PageIndex; i++)
                if (!pages[i].IsCover) num++;
            PageLabel.Text = Loc.T("Page.Label", num, contentTotal);
        }
    }

    private void GoToPage(int idx)
    {
        if (_vm == null) return;
        idx = Math.Clamp(idx, 0, _vm.Doc.Pages.Count - 1);
        if (idx == _vm.PageIndex) return;
        CommitActiveEdit();
        ClearSelection();
        _vm.PageIndex = idx;
        _page = _vm.Doc.Pages[idx];
        UpdatePageLabel();
        if (SettingsPanel.Visibility == Visibility.Visible) RefreshSettingsPanel();
        Skia.InvalidateVisual();
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e) => GoToPage((_vm?.PageIndex ?? 0) - 1);
    private void NextPage_Click(object sender, RoutedEventArgs e) => GoToPage((_vm?.PageIndex ?? 0) + 1);

    private void AddPage_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;

        WbPage page;
        if (_vm.Doc.NewPageTemplate != null)
        {
            page = _vm.Doc.PageFromTemplate();
        }
        else if (_page is { IsCover: false, IsInfinite: false })
        {
            // Ohne Vorlage: aktuelle Seite fortführen
            page = new WbPage
            {
                Width = _page.Width,
                Height = _page.Height,
                Background = _page.Background,
                Shade = _page.Shade,
            };
        }
        else
        {
            page = WhiteboardDoc.NewNotebookPage();
        }

        _vm.Doc.Pages.Insert(_vm.PageIndex + 1, page);
        MarkDirty();
        GoToPage(_vm.PageIndex + 1);
        UpdatePageLabel();
    }

    private void DeletePage_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null || _vm.Doc.Pages.Count <= 1 || _page == null) return;
        if ((_page.Elements.Count > 0 || _page.BackgroundImage != null) &&
            MessageBox.Show("Diese Seite und ihren Inhalt löschen?", "Gonk Note",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        int idx = _vm.PageIndex;
        _vm.Doc.Pages.RemoveAt(idx);
        _vm.PageIndex = Math.Min(idx, _vm.Doc.Pages.Count - 1);
        _page = _vm.Doc.Pages[_vm.PageIndex];
        MarkDirty();
        UpdatePageLabel();
        Skia.InvalidateVisual();
    }
}
