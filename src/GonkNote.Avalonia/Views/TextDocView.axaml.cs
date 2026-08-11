using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Core.Text;
using GonkNote.Core.Theming;
using GonkNote.Platform;
using GonkNote.Services;
using GonkNote.ViewModels;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Die Anzeige eines Textdokuments im Linux-Kopf — <b>der letzte offene Haken aus Phase 4</b>
/// (HANDOFF §4.28).
///
/// <para>
/// <b>Sie rechnet nichts und sie zeichnet nichts.</b> Umbrochen wird von
/// <see cref="TdLayout"/>, gezeichnet von <see cref="TdRenderer"/> — beide in Core, beide
/// seit §4.24/§4.16 fertig und bis §4.27 nirgends angeschlossen. Was hier steht, ist der
/// Anschluss: wo die Seiten liegen, welche davon gerade zu sehen sind, und wie man an einen
/// <see cref="SKCanvas"/> kommt (<see cref="SkiaCanvas"/>).
/// </para>
///
/// <para>
/// <b>Es ist derselbe Weg, den das PDF nimmt</b> (§4.27) — bis auf eine Zahl, den Maßstab
/// (<see cref="TdRenderer.PixelProCm"/> hier, <c>TdPdf.PunktProCm</c> dort). Weil der Umbruch
/// in Zentimetern rechnet, ändert **keine Zoomstufe eine Umbruchstelle**: Was auf dem Schirm
/// steht, steht auch auf dem Papier. Genau dafür ist §4.16 so gebaut.
/// </para>
///
/// <para>
/// <b>Was hier ausdrücklich nicht ist: ein Editor.</b> Caret, Auswahl und Eingabe sind eine
/// eigene Runde. Das steht als Satz im Ribbon (<c>Ed.ViewOnly</c>) und nicht als
/// ausgegrauter Knopf — was noch nicht geht, verschwindet nicht still (§7), aber es tut auch
/// nicht so, als wäre es beinahe da.
/// </para>
/// </summary>
public partial class TextDocView : UserControl
{
    /// <summary>Der Abstand zwischen zwei Blättern und der Rand um den Stapel, in Zentimetern.</summary>
    private const double AbstandCm = 0.8;

    /// <summary>Die Zoomstufen, die die beiden Knöpfe durchlaufen — dieselben wie im WPF-Editor.</summary>
    private static readonly double[] Stufen = [0.5, 0.75, 0.9, 1.0, 1.25, 1.5, 2.0, 3.0];

    private TextTabViewModel? _vm;

    /// <summary>
    /// Der Umbruch — <b>einmal je Dokument gerechnet und nicht je Bild</b>. Er hängt an
    /// <see cref="TdSkiaMeasure"/> und läuft bei einem Inhaltsverzeichnis mehrfach (§4.20);
    /// ihn im Zeichenpfad zu rechnen hieße, ihn sechzigmal je Sekunde zu rechnen.
    /// </summary>
    private TdLayoutResult? _umbruch;

    private TdRenderContext _kontext;

    /// <summary>Die Oberkante jeder Seite im Blätterstapel, in Zentimetern.</summary>
    private double[] _seitenObenCm = [];

    private double _stapelBreiteCm;
    private double _stapelHoeheCm;

    private double _zoom = 1.0;

    /// <summary>Die Seite, auf die die Seitenleiste unten zeigt (0-basiert).</summary>
    private int _seite;

    public TextDocView()
    {
        // **Nicht AvaloniaXamlLoader.Load(this)** — nur das erzeugte InitializeComponent()
        // weist die x:Name-Felder zu (HANDOFF §7).
        InitializeComponent();

        Skia.Paint += OnPaint;

        // Der Bildlauf verschiebt den Inhalt, ohne ihn neu zeichnen zu lassen. Weil die
        // Leinwand nur die **sichtbaren** Seiten aufzeichnet, bliebe der neu hereingerollte
        // Bereich sonst leer — das Fehlerbild wäre ein Dokument, das nach dem Scrollen
        // aufhört.
        Blaetter.ScrollChanged += (_, _) =>
        {
            Skia.InvalidateVisual();
            SeiteAusBildlauf();
        };

        AttachedToVisualTree += (_, _) =>
        {
            App.Platform.Theme.ThemeChanged += OnThemeChanged;
            Loc.LanguageChanged += OnLanguageChanged;
        };
        DetachedFromVisualTree += (_, _) =>
        {
            App.Platform.Theme.ThemeChanged -= OnThemeChanged;
            Loc.LanguageChanged -= OnLanguageChanged;
        };

        DataContextChanged += (_, _) => Laden();

        // Strg + Rad zoomt, wie in jedem Dokumentbetrachter; ohne Strg rollt der ScrollViewer.
        Blaetter.AddHandler(PointerWheelChangedEvent, Rad, RoutingStrategies.Tunnel);
    }

    // ==================== Laden ====================

    /// <summary>
    /// Holt das Modell aus <c>TextDoc.Model</c> und bricht es um.
    ///
    /// <para>
    /// <b>Aus <c>Model</c> und nicht aus <c>Rtf</c>, und das ist keine Wahl:</b> RTF und
    /// XamlPackage liest ausschließlich WPF (§4.22). Steht dort nichts, ist das Dokument
    /// noch nicht übernommen — dann kommt der Hinweis und **kein leeres Blatt**, denn ein
    /// leeres Blatt sähe aus wie gelöschter Inhalt.
    /// </para>
    /// </summary>
    private void Laden()
    {
        _vm = DataContext as TextTabViewModel;
        _umbruch = null;
        _seite = 0;

        var modell = _vm != null ? TdFormatIo.Lesen(_vm.Doc.Model) : null;

        HinweisUebernahme.IsVisible = modell == null;
        Blaetter.IsVisible = modell != null;
        Seitenleiste.IsVisible = modell != null;
        KnopfExport.IsEnabled = modell != null;

        if (modell == null || _vm == null)
        {
            Beschriftungen();
            return;
        }

        _zoom = _vm.Zoom > 0 ? _vm.Zoom : 1.0;

        // Die Uhr wird hier gefragt und nicht in Core (§4.20) — sonst hinge jeder Wächter
        // daran, wann er läuft.
        var felder = new TdFieldContext { Date = DateTime.Now, Title = _vm.Title };

        using (var messung = new TdSkiaMeasure())
            _umbruch = TdLayout.Umbrechen(modell, messung, felder);

        _kontext = new TdRenderContext(
            new TdBlobImages(App.Db.Blobs), felder, _umbruch.PageCount);

        StapelVermessen();
        LeinwandGroesse();
        Beschriftungen();
        LayoutFelder(modell);
        Zaehlen(modell);
    }

    /// <summary>
    /// Wo jede Seite im Stapel liegt. <b>In Zentimetern</b>, wie alles im Umbruch — der
    /// Maßstab kommt erst beim Zeichnen dazu, und deshalb kostet eine Zoomstufe hier nichts.
    /// </summary>
    private void StapelVermessen()
    {
        if (_umbruch == null) { _seitenObenCm = []; return; }

        _seitenObenCm = new double[_umbruch.Pages.Count];
        double y = AbstandCm;
        double breite = 0;

        for (int i = 0; i < _umbruch.Pages.Count; i++)
        {
            var setup = _umbruch.Pages[i].Setup;
            _seitenObenCm[i] = y;
            y += setup.HeightCm + AbstandCm;
            breite = Math.Max(breite, setup.WidthCm);
        }

        // Verschieden große Seiten sind erlaubt (ein Abschnitt darf quer liegen, §4.15) —
        // der Stapel ist deshalb so breit wie sein breitestes Blatt und nicht wie das erste.
        _stapelBreiteCm = breite + 2 * AbstandCm;
        _stapelHoeheCm = y;
    }

    /// <summary>
    /// Die Leinwand bekommt die volle Größe des Stapels; den Ausschnitt besorgt der
    /// <c>ScrollViewer</c>. Gezeichnet wird trotzdem nur, was zu sehen ist (siehe
    /// <see cref="OnPaint"/>).
    /// </summary>
    private void LeinwandGroesse()
    {
        Skia.Width = Math.Max(1, _stapelBreiteCm * TdRenderer.PixelProCm * _zoom);
        Skia.Height = Math.Max(1, _stapelHoeheCm * TdRenderer.PixelProCm * _zoom);
        Skia.InvalidateVisual();
    }

    // ==================== Zeichnen ====================

    private void OnPaint(SkiaPaintArgs e)
    {
        var leinwand = e.Canvas;

        // Kein `Clear`: die Leinwand zeichnet auf, und ein Clear löschte beim Abspielen auch,
        // was Avalonia darunter schon gezeichnet hat (siehe SkiaCanvas).
        using (var hintergrund = new SKPaint { Color = ThemeSk(ThemeColor.CanvasBg) })
            leinwand.DrawRect(e.Bounds, hintergrund);

        if (_umbruch == null) return;

        double massstab = TdRenderer.PixelProCm * _zoom;
        var (obenPx, untenPx) = Sichtfenster();

        for (int i = 0; i < _umbruch.Pages.Count; i++)
        {
            var seite = _umbruch.Pages[i];
            float y = (float)(_seitenObenCm[i] * massstab);
            float h = (float)(seite.Setup.HeightCm * massstab);
            float b = (float)(seite.Setup.WidthCm * massstab);

            // Was außerhalb des Sichtfensters liegt, wird nicht aufgezeichnet. Bei einem
            // dreißigseitigen Skript spart das neunundzwanzig Seiten je Bild — und ein
            // aufgezeichneter Befehl ist nicht umsonst, nur weil er später weggeschnitten
            // wird.
            if (y + h < obenPx || y > untenPx) continue;

            // Blätter liegen mittig, auch wenn sie verschieden breit sind.
            float x = (float)((_stapelBreiteCm * massstab - b) / 2);

            var blatt = SKRect.Create(x, y, b, h);
            Blattschatten(leinwand, blatt);

            leinwand.Save();
            leinwand.Translate(x, y);
            TdRenderer.Seite(leinwand, seite, massstab, _kontext);
            leinwand.Restore();

            Blattrand(leinwand, blatt);
        }
    }

    /// <summary>
    /// Der sichtbare Bereich der Leinwand in Pixeln. <b>Er kommt vom
    /// <c>ScrollViewer</c> und nicht aus <c>Bounds</c>:</b> die Leinwand <i>ist</i> der ganze
    /// Stapel, ihre Grenzen sagen über den Ausschnitt nichts.
    /// </summary>
    private (double Oben, double Unten) Sichtfenster()
    {
        double oben = Blaetter.Offset.Y;
        double hoehe = Blaetter.Viewport.Height;

        // Ein Blatt Vorlauf nach oben und unten: beim Rollen ist der Rand sonst für ein Bild
        // lang leer, weil Aufzeichnung und Anzeige nicht im selben Augenblick passieren.
        return (oben - hoehe, oben + 2 * hoehe);
    }

    /// <summary>
    /// Schatten und Rand eines Blattes. <b>Beides gehört dem Kopf und nicht dem Zeichner:</b>
    /// <see cref="TdRenderer"/> malt Papier, und Papier hat keinen Schatten — der entsteht
    /// erst dadurch, dass es auf einer Fläche liegt (§4.24, „Ein Dokument ist Papier"). Im PDF
    /// wäre er sogar falsch: dort <i>ist</i> die Seite das Papier.
    ///
    /// <para>
    /// Ein versetztes halbdurchsichtiges Rechteck und kein <c>SKImageFilter</c>: Der Filter
    /// zwingt Skia zu einer Zwischenfläche je Blatt, und das für einen Schatten, den bei
    /// dieser Größe niemand von einer harten Kante unterscheidet.
    /// </para>
    /// </summary>
    private static void Blattschatten(SKCanvas leinwand, SKRect blatt)
    {
        using var schatten = new SKPaint { Color = new SKColor(0, 0, 0, 28), IsAntialias = true };
        leinwand.DrawRect(
            SKRect.Create(blatt.Left + 2, blatt.Top + 3, blatt.Width, blatt.Height), schatten);
    }

    /// <summary>
    /// Der Rand kommt <b>nach</b> dem Zeichner: Papier ist weiß, und auf einer hellen Fläche
    /// wäre ein Blatt ohne Kante nicht zu sehen.
    /// </summary>
    private static void Blattrand(SKCanvas leinwand, SKRect blatt)
    {
        using var rand = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 38),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true,
        };
        leinwand.DrawRect(blatt, rand);
    }

    private static SKColor ThemeSk(ThemeColor farbe)
    {
        var c = AvaloniaThemeHost.Current[farbe];
        return new SKColor(c.R, c.G, c.B, c.A);
    }

    // ==================== Ribbon ====================

    /// <summary>
    /// Der Umschalter zwischen den Ribbon-Leisten. Die Null-Prüfung ist nicht Vorsicht,
    /// sondern nötig: <c>IsChecked="True"</c> am ersten Reiter feuert <b>während</b>
    /// <c>InitializeComponent</c>, und da gibt es die Leisten noch nicht.
    /// </summary>
    private void Reiter_Gewechselt(object? sender, RoutedEventArgs e)
    {
        if (LeisteStart == null || LeisteLayout == null || ReiterStart == null) return;

        LeisteStart.IsVisible = ReiterStart.IsChecked == true;
        LeisteLayout.IsVisible = ReiterLayout.IsChecked == true;
    }

    /// <summary>
    /// Export über den Weg, den auch „Datei → Exportieren" nimmt — <c>MainViewModel</c>
    /// speichert dabei zuerst und ruft danach <c>AvaloniaDocumentIo</c>. <b>Der Kopf
    /// exportiert nicht selbst</b>, er wählt nur das Format vor.
    /// </summary>
    private void Export_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string endung } && Fenster() is { } vm)
            vm.ExportActiveTab(endung);
    }

    private MainViewModel? Fenster() =>
        (TopLevel.GetTopLevel(this) as Window)?.DataContext as MainViewModel;

    // ==================== Zoom ====================

    private void ZoomRein_Click(object? s, RoutedEventArgs e) => ZoomStufe(+1);
    private void ZoomRaus_Click(object? s, RoutedEventArgs e) => ZoomStufe(-1);
    private void ZoomZurueck_Click(object? s, RoutedEventArgs e) => ZoomSetzen(1.0);

    private void ZoomStufe(int richtung)
    {
        int i = Array.FindIndex(Stufen, z => z > _zoom + 0.0001);
        if (richtung < 0) i = Array.FindLastIndex(Stufen, z => z < _zoom - 0.0001);
        if (i < 0) return;
        ZoomSetzen(Stufen[i]);
    }

    private void Seitenbreite_Click(object? s, RoutedEventArgs e) => Einpassen(nurBreite: true);
    private void GanzeSeite_Click(object? s, RoutedEventArgs e) => Einpassen(nurBreite: false);

    /// <summary>
    /// Passt den Zoom so an, dass ein Blatt in die Breite (oder ganz) ins Sichtfenster geht.
    /// Gerechnet wird gegen die <b>aktuelle</b> Seite, nicht gegen die erste: bei einem quer
    /// liegenden Abschnitt wäre sonst genau das Blatt zu groß, das man ansieht.
    /// </summary>
    private void Einpassen(bool nurBreite)
    {
        if (_umbruch == null || _umbruch.Pages.Count == 0) return;

        var setup = _umbruch.Pages[Math.Clamp(_seite, 0, _umbruch.Pages.Count - 1)].Setup;
        double breite = Blaetter.Viewport.Width, hoehe = Blaetter.Viewport.Height;
        if (breite <= 0 || hoehe <= 0) return;

        // Der Abstand um den Stapel zählt mit — sonst klebt das Blatt an der Bildlaufleiste.
        double passtBreite = breite / ((setup.WidthCm + 2 * AbstandCm) * TdRenderer.PixelProCm);
        double passtHoehe = hoehe / ((setup.HeightCm + 2 * AbstandCm) * TdRenderer.PixelProCm);

        ZoomSetzen(nurBreite ? passtBreite : Math.Min(passtBreite, passtHoehe));
    }

    private void ZoomSetzen(double zoom)
    {
        _zoom = Math.Clamp(zoom, 0.2, 5.0);
        if (_vm != null) _vm.Zoom = _zoom;

        LeinwandGroesse();
        ZuSeite(_seite);
        Beschriftungen();
    }

    private void Rad(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
        ZoomStufe(e.Delta.Y > 0 ? +1 : -1);
        e.Handled = true;
    }

    // ==================== Seiten ====================

    private void SeiteZurueck_Click(object? s, RoutedEventArgs e) => ZuSeite(_seite - 1);
    private void SeiteVor_Click(object? s, RoutedEventArgs e) => ZuSeite(_seite + 1);

    private void ZuSeite(int nummer)
    {
        if (_umbruch == null || _umbruch.Pages.Count == 0) return;

        _seite = Math.Clamp(nummer, 0, _umbruch.Pages.Count - 1);
        double y = (_seitenObenCm[_seite] - AbstandCm) * TdRenderer.PixelProCm * _zoom;

        Blaetter.Offset = Blaetter.Offset.WithY(Math.Max(0, y));
        Beschriftungen();
    }

    /// <summary>
    /// Welche Seite die Leiste unten nennt: die, die den oberen Rand des Sichtfensters
    /// überdeckt. <b>Nicht die, die am meisten zu sehen ist</b> — dann sprünge die Anzeige
    /// beim Rollen hin und her, sobald zwei Seiten etwa gleich viel Platz haben.
    /// </summary>
    private void SeiteAusBildlauf()
    {
        if (_umbruch == null || _seitenObenCm.Length == 0) return;

        double obenCm = Blaetter.Offset.Y / (TdRenderer.PixelProCm * _zoom);

        int gefunden = 0;
        for (int i = 0; i < _seitenObenCm.Length; i++)
            if (_seitenObenCm[i] <= obenCm + AbstandCm) gefunden = i;

        if (gefunden == _seite) return;
        _seite = gefunden;
        Beschriftungen();
    }

    // ==================== Texte ====================

    private void Beschriftungen()
    {
        ZoomAnzeige.Content = $"{Math.Round(_zoom * 100)} %";
        SeitenAnzeige.Text = _umbruch == null
            ? ""
            : Loc.T("Page.Label", _seite + 1, _umbruch.PageCount);
    }

    /// <summary>Die Seiteneinrichtung des ersten Abschnitts, abgelesen für den Reiter „Layout".</summary>
    private void LayoutFelder(TdDocument modell)
    {
        var seite = modell.Sections.FirstOrDefault()?.Page ?? TdPageSetup.A4;

        // Ein Blatt ohne benanntes Format ist kein Fehler — es hat nur keinen Namen (§4.15).
        FeldFormat.Text = seite.Name ?? $"{seite.WidthCm:0.#} × {seite.HeightCm:0.#} cm";
        FeldLage.Text = Loc.T(seite.IstQuerformat
            ? "Settings.Page.Landscape"
            : "Settings.Page.Portrait");
        FeldRaender.Text =
            $"{seite.MarginLeftCm:0.#} / {seite.MarginTopCm:0.#} / " +
            $"{seite.MarginRightCm:0.#} / {seite.MarginBottomCm:0.#} cm";

        bool kopf = seite.HeaderText.Length > 0, fuss = seite.FooterText.Length > 0;
        FeldKopfFuss.Text = (kopf, fuss) switch
        {
            (true, true) => Loc.T("Td.HeaderFooter.Both"),
            (true, false) => Loc.T("Td.HeaderFooter.HeaderOnly"),
            (false, true) => Loc.T("Td.HeaderFooter.FooterOnly"),
            _ => Loc.T("Td.HeaderFooter.None"),
        };
    }

    private void Zaehlen(TdDocument modell)
    {
        string text = modell.PlainText();
        int woerter = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        Zaehlung.Text = Loc.T("Ed.Status.Counts.Format", woerter, text.Length);
    }

    // ==================== Ereignisse von außen ====================

    private void OnThemeChanged() => Skia.InvalidateVisual();

    /// <summary>
    /// Ein Sprachwechsel betrifft die Texte, die <b>der Code</b> setzt — die aus
    /// <c>{loc:T …}</c> zieht Avalonia selbst nach (HANDOFF §7).
    /// </summary>
    private void OnLanguageChanged()
    {
        Beschriftungen();
        if (_vm != null && TdFormatIo.Lesen(_vm.Doc.Model) is { } modell)
        {
            LayoutFelder(modell);
            Zaehlen(modell);
        }
    }
}
