using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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

    // Skalieren (Phase 4.5). Gerechnet wird der GESAMTfaktor seit dem Anfassen; der Schritt
    // ergibt sich daraus. Wer stattdessen je Mausbewegung einen kleinen Faktor anwendet,
    // sammelt Rundungsfehler ein, und das Element schrumpft beim Hin- und Herziehen.
    private bool _scalingSelection;
    private SKPoint _scalePivot;
    private float _scaleStartDist;    // Abstand Pivot→Zeiger beim Anfassen
    private float _scaleAccum;        // bereits angewandter Gesamtfaktor

    // Drehen (Phase 4.5). Nur bei Einzelauswahl — der Kasten mehrerer Elemente ist
    // achsenparallel und hat deshalb keinen Drehgriff.
    private WbElement? _rotatingEl;
    private float _rotStartDeg;       // Drehung des Elements beim Anfassen
    private float _rotStartPointer;   // Zeigerwinkel beim Anfassen

    // ---- Formen (Phase 4.5) ----
    private ShapeKind _form = ShapeKind.Rectangle;
    private SKPoint _formStart, _formJetzt;
    private bool _formAktiv;
    private bool _fuellungAn;
    private bool _umschaltGedrueckt;
    private HexColor _fuellfarbe = new(0xFF, 0x14, 0xB8, 0xA6);   // dasselbe Türkis wie drüben
    private double _fuellDeckkraft = 0.4;

    /// <summary>
    /// Die Knöpfe der fünf Formen. <b>Sie stehen bewusst nicht in <see cref="ToolButtons"/></b>:
    /// dort gilt „genau einer ist gedrückt", und die fünf teilen sich <em>ein</em> Werkzeug —
    /// wer sie mit hineinnähme, könnte die Form nicht wechseln, ohne das Werkzeug zu verlieren.
    /// </summary>
    private ToggleButton[] FormButtons =>
        [BtnFormLinie, BtnFormPfeil, BtnFormRechteck, BtnFormEllipse, BtnFormDreieck];

    // ---- Textfeld und Notizzettel (Phase 4.5) ----
    private TextElement? _bearbeiteterText;
    private StickyNoteElement? _bearbeiteterZettel;
    private bool _bearbeitungIstNeu;
    private string _bearbeitungVorher = "";
    private bool _bearbeitungVerwerfen;

    /// <summary>Vorgabe-Hintergrund neuer Textfelder; <c>null</c> = durchsichtig.</summary>
    private string? _textGrundHex;
    private HexColor _zettelfarbe = new(0xFF, 0xFD, 0xE6, 0x8A);   // dasselbe Gelb wie drüben

    private ToggleButton[] ToolButtons =>
        [BtnPen, BtnPencil, BtnHighlighter, BtnEraser, BtnLasso, BtnMove, BtnPan,
         BtnText, BtnZettel, BtnSticker];

    /// <summary>
    /// Der umgekehrte Weg zu <see cref="ToCanvas"/>: von der Zeichenfläche auf den Schirm.
    /// Gebraucht, um das Eingabefeld über das Element zu legen, das es bearbeitet.
    /// </summary>
    private Point ToScreen(SKPoint p) => new(p.X * Zoom + PanX, p.Y * Zoom + PanY);

    public WhiteboardView()
    {
        // **Nicht AvaloniaXamlLoader.Load(this).** Nur das erzeugte InitializeComponent()
        // weist danach die x:Name-Felder zu; mit dem Lader direkt bliebe jedes davon null
        // und der erste Zugriff wirft an einer Stelle, die mit der Ursache nichts zu tun hat
        // (HANDOFF §7, „AvaloniaXamlLoader.Load füllt die x:Name-Felder nicht").
        InitializeComponent();

        FarbkachelnAufbauen();

        _suppressToolEvents = true;
        BtnPen.IsChecked = true;
        _suppressToolEvents = false;

        // Der Anfangsstand der Leiste. **Ohne diesen Ruf stünden alle Knöpfe da** — die
        // Klappregel läuft sonst erst beim ersten Werkzeugwechsel, und bis dahin sähe die
        // Leiste aus wie vor der Umstellung.
        LeisteKlappen();
        ZahlenblockAnhaengen();

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
            FokusHolen();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            App.Platform.Theme.ThemeChanged -= OnThemeChanged;
            Loc.LanguageChanged -= OnLanguageChanged;
            InhaltVerwerfen();
        };

        DataContextChanged += OnDataContextChanged;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        // Auch am Eingabefeld im Tunnel: als gewöhnliches Ereignis käme der Handler erst,
        // nachdem der TextBox die Taste verarbeitet hat — Strg+Eingabe stand dann als
        // Zeichen im Text, bevor die Bearbeitung abschloss (am laufenden Programm gesehen).
        EditFeld.AddHandler(KeyDownEvent, EditFeld_Taste, RoutingStrategies.Tunnel);
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

    /// <summary>
    /// Gibt der Zeichenfläche nach dem Öffnen den Tastaturfokus.
    ///
    /// <para>
    /// <b>Ohne das ist der erste Tastendruck nach dem Öffnen verloren</b> — der Fokus liegt
    /// dann auf der Seitenleiste, über die das Dokument aufgemacht wurde. Der Laptop hat es
    /// an Strg+V gefunden (§5 „Noch offen" 19, V2-86); es trifft aber jedes Kürzel, und
    /// <b>der WPF-Kopf tut dasselbe</b> — am laufenden Programm in beiden Köpfen
    /// gegengeprüft, deshalb ist es hier behoben und nicht als Linux-Sache zurückgegangen.
    /// </para>
    ///
    /// <para>
    /// <b>Die Ausnahme ist der Grund, warum das keine Einzeiler-Zeile ist:</b> ein frisch
    /// angelegtes Board wird in der Seitenleiste sofort zum Umbenennen aufgeklappt, und
    /// gleichzeitig geht sein Reiter auf. Wer hier bedingungslos den Fokus nimmt, reißt dem
    /// Nutzer das Umbenennen unter den Fingern weg. Steht der Fokus in einem Textfeld, bleibt
    /// er dort.
    /// </para>
    ///
    /// <para>
    /// <b>Über den Dispatcher</b>, weil der Fokus zum Zeitpunkt des Anhängens noch verteilt
    /// wird — ein <c>Focus()</c> mitten hinein wäre einen Wimpernschlag später wieder weg.
    /// </para>
    /// </summary>
    private void FokusHolen() => Dispatcher.UIThread.Post(() =>
    {
        // Kein TopLevel heißt: der Reiter ist wieder zu, bevor der Dispatcher drankam.
        if (TopLevel.GetTopLevel(this) is not { } oben) return;
        if (oben.FocusManager?.GetFocusedElement() is TextBox) return;
        Skia.Focus();
    }, DispatcherPriority.Background);

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

    /// <summary>
    /// Eine der fünf Formen wurde gewählt. Sie schalten alle dasselbe Werkzeug ein und
    /// unterscheiden sich nur in der Art — deshalb ein eigenes Ereignis und eine eigene
    /// Gruppe (siehe <see cref="FormButtons"/>).
    /// </summary>
    private void Form_Gewaehlt(object? sender, RoutedEventArgs e)
    {
        if (_suppressToolEvents || sender is not ToggleButton btn) return;

        if (btn.IsChecked != true)
        {
            // Wie bei den Werkzeugen: der aktive Knopf lässt sich nicht abwählen, solange
            // das Formen-Werkzeug läuft — sonst stünde ein Werkzeug ohne Form da.
            if (_tool == ToolType.Shape && FormButtons.All(b => b.IsChecked != true))
            {
                _suppressToolEvents = true;
                btn.IsChecked = true;
                _suppressToolEvents = false;
            }
            return;
        }

        _suppressToolEvents = true;
        foreach (var b in FormButtons)
            if (b != btn) b.IsChecked = false;
        foreach (var b in ToolButtons) b.IsChecked = false;
        _suppressToolEvents = false;

        _form = Enum.Parse<ShapeKind>((string)btn.Tag!);
        SetTool(ToolType.Shape);   // ruft LeisteKlappen und nimmt _form als Vertreter
    }

    private void SetTool(ToolType tool)
    {
        // Eine offene Beschriftung gehört abgeschlossen, bevor das Werkzeug wechselt —
        // sonst bliebe das Eingabefeld über einer Fläche stehen, auf der man schon wieder
        // zeichnet, und sein Inhalt käme nie im Element an.
        BearbeitungAbschliessen();

        // Die Auswahl bleibt nur bei den Auswahl-Werkzeugen stehen.
        if (tool != ToolType.Lasso && tool != ToolType.Move) ClearSelection();

        // Ein anderes Werkzeug hebt die Formenwahl auf — die fünf Knöpfe hängen an
        // ToolType.Shape und dürfen nicht gedrückt bleiben, wenn er nicht mehr gilt.
        if (tool != ToolType.Shape)
        {
            _suppressToolEvents = true;
            foreach (var b in FormButtons) b.IsChecked = false;
            _suppressToolEvents = false;
        }

        _tool = tool;
        _eraserVisible = false;
        SyncSizeControls();

        // Erst merken, dann klappen: eingeklappt soll der Knopf stehen bleiben, den der
        // Nutzer gerade gewählt hat, und nicht der davor.
        VertreterMerken(tool);
        LeisteKlappen();

        // Ein Werkzeugwechsel beendet die Schnellaktionen und den Zahlenblock — beide
        // beziehen sich auf einen Zustand, den es gerade nicht mehr gibt.
        SchnellaktionenVerbergen();
        ZahlenblockSchliessen();

        // Die Formen-Einstellungen erscheinen nur mit dem Werkzeug. Ist die Leiste zu,
        // klappt sie auf — sonst wäre die Füllfarbe hinter einem Knopf versteckt, den
        // niemand in dem Moment drückt (dieselbe Regel wie im WPF-Kopf).
        //
        // **Und dann gespiegelt.** Wer die Leiste aufklappt, ohne EinstellungenSpiegeln zu
        // rufen, bekommt sie mit lauter leeren Umschaltern — kein Muster, kein Farbton, kein
        // Format markiert. Das steht seit Phase 3 in Einstellungen_Click und ist hier beim
        // zweiten Aufklappweg prompt wieder passiert (am laufenden Programm gesehen).
        FormenBereich.IsVisible = tool == ToolType.Shape;
        TextBereich.IsVisible = tool == ToolType.Text;
        ZettelBereich.IsVisible = tool == ToolType.Sticky;
        StickerBereich.IsVisible = tool == ToolType.Sticker;

        // Die Sammlung wird erst gelesen, wenn jemand sie sehen will — sie liegt auf der
        // Platte, und beim Start braucht sie niemand.
        if (tool == ToolType.Sticker) StickerSicherstellen();

        bool eigeneSektion = tool is ToolType.Shape or ToolType.Text or ToolType.Sticky
                                  or ToolType.Sticker;
        if (eigeneSektion && !EinstellungenLeiste.IsVisible)
        {
            EinstellungenLeiste.IsVisible = true;
            EinstellungenSpiegeln();
        }

        Skia.Cursor = new Cursor(tool switch
        {
            ToolType.Eraser => StandardCursorType.Cross,
            ToolType.Pan => StandardCursorType.Hand,
            ToolType.Lasso => StandardCursorType.Cross,
            ToolType.Shape => StandardCursorType.Cross,
            _ => StandardCursorType.Arrow,
        });
        Neuzeichnen();
    }

    // ==================== Füllung ====================

    /// <summary>Die Füllfarbe für neue Formen — <c>null</c>, wenn nicht gefüllt wird.</summary>
    private string? AktuelleFuellung() => _fuellungAn
        ? _fuellfarbe.WithAlpha((byte)Math.Round(_fuellDeckkraft * 255)).ToString()
        : null;

    private void Fuellung_Umgeschaltet(object? sender, RoutedEventArgs e)
    {
        _fuellungAn = FuellungAn.IsChecked == true;
        FuellvorschauNachfuehren();
    }

    private void Deckkraft_Geaendert(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != RangeBase.ValueProperty) return;
        _fuellDeckkraft = FuellungDeckkraft.Value / 100.0;
        DeckkraftAnzeige.Text = $"{Math.Round(FuellungDeckkraft.Value)} %";
        FuellvorschauNachfuehren();
    }

    /// <summary>
    /// Öffnet den Farbwähler für die Füllung. <b>Ohne Deckkraft</b> — die steht als eigener
    /// Regler daneben, und zwei Stellen für dieselbe Zahl wären zwei Wahrheiten.
    /// </summary>
    private void Fuellfarbe_Click(object? sender, RoutedEventArgs e)
    {
        if (ColorPickerWindow.Waehlen(TopLevel.GetTopLevel(this) as Window,
                _fuellfarbe.WithAlpha(0xFF), mitDeckkraft: false) is not { } gewaehlt)
            return;

        _fuellfarbe = gewaehlt;

        // Eine Farbe zu wählen heißt, sie benutzen zu wollen — genau wie drüben.
        _fuellungAn = true;
        _suppressToolEvents = true;
        FuellungAn.IsChecked = true;
        _suppressToolEvents = false;

        FuellvorschauNachfuehren();
    }

    private void FuellvorschauNachfuehren() =>
        FuellungVorschau.Background =
            _fuellfarbe.WithAlpha((byte)Math.Round(_fuellDeckkraft * 255)).ToBrush();

    /// <summary>
    /// Baut die Farbkacheln der Werkzeugleiste aus <see cref="WbTinte.Palette"/> — hinter
    /// „automatisch" und vor der eigenen Farbe.
    ///
    /// <para>
    /// <b>Aus Core und nicht aus der XAML</b> (HANDOFF §4.74): Beide Köpfe pflegten die Liste
    /// von Hand, in verschiedener Länge und Reihenfolge.
    /// </para>
    /// </summary>
    private void FarbkachelnAufbauen()
    {
        if (AutoSwatch.Parent is not Panel leiste) return;

        int stelle = leiste.Children.IndexOf(AutoSwatch) + 1;

        foreach (var farbe in WbTinte.Palette)
        {
            // „automatisch" ist AutoSwatch und folgt der Seite.
            if (farbe.Hex is not { } hex) continue;

            var kachel = new RadioButton
            {
                GroupName = "Tinte",
                Tag = WbTinte.Marke(farbe),
                Background = HexColor.Parse(hex, HexColor.Black).ToBrush(),
            };
            kachel.Classes.Add("farbe");
            ToolTip.SetTip(kachel, Loc.T(farbe.Key));
            kachel.IsCheckedChanged += Color_Changed;
            leiste.Children.Insert(stelle++, kachel);
        }
    }

    /// <summary>
    /// Eine freie Tintenfarbe über den Farbwähler — <b>den Knopf gab es hier nicht</b>
    /// (§4.74), obwohl der Wähler seit §4.52 da ist und der WPF-Kopf ihn seit jeher anbietet.
    ///
    /// <para>
    /// <b>Die gewählte Farbe bleibt als eigene Kachel stehen</b> und wird ausgewählt — sonst
    /// müsste man sie beim nächsten Strich erneut heraussuchen. Dasselbe tut der WPF-Kopf.
    /// </para>
    /// </summary>
    private void EigeneTinte_Click(object? sender, RoutedEventArgs e)
    {
        var start = HexColor.Parse(CustomSwatch.Tag as string, HexColor.Black);

        if (ColorPickerWindow.Waehlen(TopLevel.GetTopLevel(this) as Window,
                start, mitDeckkraft: false) is not { } gewaehlt)
            return;

        CustomSwatch.Background = gewaehlt.ToBrush();
        CustomSwatch.Tag = "#FF" + gewaehlt.ToString().TrimStart('#');
        CustomSwatch.IsVisible = true;
        CustomSwatch.IsChecked = true;
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

    /// <summary>Stellt Schieber, Symbol und Anzeige auf das aktive Werkzeug um.</summary>
    private void SyncSizeControls()
    {
        if (WidthSlider == null || WidthLabel == null) return;

        // Der Tooltip gehört an **alle drei** Auslöser des Zahlenblocks, nicht nur an den
        // Schieber: sie tun dasselbe, also sollen sie dasselbe erklären.
        string tip = Loc.T(SizeControlsEraser ? "Size.Eraser.Tip" : "Size.Tip");
        ToolTip.SetTip(WidthSlider, tip);
        if (WidthIcon != null)
        {
            ToolTip.SetTip(WidthIcon, tip);
            // Das Symbol sagt, **was** hier eingestellt wird — Strichstärke oder Radierergröße.
            WidthIcon.Icon = SizeControlsEraser ? AppIcon.Eraser : AppIcon.Pencil;
        }
        ToolTip.SetTip(WidthLabel, tip);

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
        _scalingSelection = false;
        _rotatingEl = null;
        Neuzeichnen();
    }

    private SKRect InflatedSelectionBounds() => WbHandles.InflatedBounds(_selectionBounds, Zoom);

    private void ComputeSelectionBounds() => _selectionBounds = WbHit.Bounds(_selection);

    /// <summary>
    /// Das einzelne ausgewählte Element — oder <c>null</c>, wenn es keines oder mehrere sind.
    /// So fragen Weiche und Zeichner in Core danach (<see cref="WbHandles.Probe"/>).
    /// </summary>
    private WbElement? SingleSelected => _selection.Count == 1 ? _selection.First() : null;

    /// <summary>Was der Zeiger an der Auswahl anfasst. Gerechnet wird in <see cref="WbHandles"/>.</summary>
    private WbHandles.Grab ProbeHandles(SKPoint c) =>
        WbHandles.Probe(SingleSelected, _selectionBounds, _selection.Count, c, Zoom);

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

    /// <summary>Der Leistenknopf zu einem Werkzeug; <c>null</c>, wenn es hier keinen gibt.</summary>
    private ToggleButton? KnopfFuer(ToolType werkzeug) =>
        ToolButtons.FirstOrDefault(b => (string?)b.Tag == werkzeug.ToString());

    /// <summary>Der Knopf zu einer Form — die fünf teilen sich ein Werkzeug.</summary>
    private ToggleButton FormButtonFuer(ShapeKind form) =>
        FormButtons.First(b => (string?)b.Tag == form.ToString());

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_vm == null) return;

        // **Während einer Beschriftung hält dieser Handler still** — und das ist am
        // laufenden Programm gemessen, nicht vermutet: er hängt am `Tunnel` (siehe
        // AddHandler oben) und läuft damit **vor** dem Eingabefeld, egal wo der Fokus
        // liegt. Ohne diese Zeile wurde aus einem getippten „Hallo" ein Werkzeugwechsel
        // („H" schaltet die Hand ein), und im Textfeld stand nichts.
        //
        // Der Tunnel ist für die Zeichenfläche richtig — sie soll die Tasten sicher
        // bekommen. Nur solange getippt wird, gehört das Feld davor.
        if (EditFeld.IsVisible) return;

        bool strg = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (strg)
        {
            switch (e.Key)
            {
                case Key.Z: DoUndo(); e.Handled = true; return;
                case Key.Y: DoRedo(); e.Handled = true; return;
                case Key.A: SelectAll(); e.Handled = true; return;
                case Key.C: Kopieren(); e.Handled = true; return;
                case Key.X: Ausschneiden(); e.Handled = true; return;
                case Key.D: Duplizieren(); e.Handled = true; return;
                // Ohne Zielpunkt: die Tastatur nennt keine Stelle, also rückt das
                // Eingefügte schräg weg. Aus den Schnellaktionen kommt einer mit.
                case Key.V: Einfuegen(null); e.Handled = true; return;
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

        // Die Zeichenhilfen haben eigene Kürzel und stehen deshalb vor der Werkzeugtabelle:
        // sie schalten kein Werkzeug um, sondern legen etwas auf die Fläche. Dieselben
        // Buchstaben wie im WPF-Kopf.
        switch (e.Key)
        {
            case Key.R: HilfeSetzen(Zeichenhilfe.Lineal); e.Handled = true; return;
            case Key.D: HilfeSetzen(Zeichenhilfe.Geodreieck); e.Handled = true; return;
        }

        // Werkzeug-Kürzel — **dieselbe Tabelle wie drüben**, sie steht in Core
        // (WbLeiste.Kuerzel, §4.61). Bis V2-83 fehlten hier G, T, F und N, obwohl es die
        // zugehörigen Werkzeuge seit §4.53/§4.55 gibt: sie waren beim Bauen der Werkzeuge
        // schlicht nicht nachgezogen worden.
        // Key.A bis Key.Z liegen lückenlos hintereinander — daraus wird der Buchstabe,
        // mit dem die Tabelle in Core geschlüsselt ist.
        if (e.Key is >= Key.A and <= Key.Z &&
            WbLeiste.Kuerzel.TryGetValue((char)('A' + (e.Key - Key.A)), out var werkzeug))
        {
            // Die Formen sind keine eigenen Werkzeuge, sondern fünf Knöpfe an einem —
            // „F" schaltet deshalb auf die zuletzt benutzte Form und nicht auf eine feste.
            if (werkzeug == ToolType.Shape) FormButtonFuer(_form).IsChecked = true;
            else if (KnopfFuer(werkzeug) is { } btn) btn.IsChecked = true;
            else return;

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
