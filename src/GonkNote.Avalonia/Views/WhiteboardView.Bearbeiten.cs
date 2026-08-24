using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Core.Theming;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Textfelder und Notizzettel auf der Fläche beschriften — das Gegenstück zu
/// <c>WhiteboardView.Editing.cs</c> im WPF-Kopf, neu in Phase 4.5.
///
/// <para>
/// <b>Warum ein echtes Eingabefeld über der Fläche und nicht auf ihr.</b> Ein
/// <c>TextBox</c> bringt Marke, Auswahl, Zwischenablage und — das ist der Punkt — die
/// <b>Eingabemethode</b> mit. Umlaute, tote Tasten und das Zusammensetzen wären auf einer
/// selbstgezeichneten Fläche alles Eigenbau; §4.41 bis §4.44 zeigen, wie viel daran hängt.
/// </para>
/// <para>
/// <b>Der Farbfehler beim Bearbeiten ist gemessen und behoben</b> (§4.55): der Hintergrund
/// kam nicht an, weil Fluent ihn <b>am Border im Template</b> setzt und nicht am
/// <c>TextBox</c>. Wo das steht und warum die Behebung über die Ressourcen läuft, sagt
/// <see cref="FeldfarbenSetzen"/>. <b>Die Schrift kam immer an</b> — „schwarz mit heller
/// Schrift" war eine Fehlbeobachtung; sie war unsere eigene dunkle Farbe auf schwarzem
/// Grund.
/// </para>
/// <para>
/// <b>Was hier bewusst geteilt ist:</b> Beide Werkzeuge benutzen <b>dasselbe</b> Eingabefeld.
/// Zwei Felder hießen zwei Stellen, an denen der Fokus verlorengehen kann — und
/// <c>BearbeitungAbschliessen</c> müsste beide kennen. Dafür setzt der Zettel ein paar
/// Eigenschaften, die das Textfeld danach <b>zurücksetzen muss</b>; der WPF-Kopf hat dieselbe
/// Stelle und denselben Kommentar.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>Abstand zwischen Zettelrand und Text (Zeichenflächen-Einheiten, wie drüben).</summary>
    private const float ZettelRand = 14f;

    // ==================== Anlegen und Treffen ====================

    /// <summary>Textfeld unter dem Zeiger bearbeiten — sonst ein neues anlegen.</summary>
    private void BeginTextInput(SKPoint c)
    {
        if (_page == null) return;

        var treffer = _page.Elements.OfType<TextElement>()
            .LastOrDefault(t => WbRenderer.TextBounds(t).Contains(c));
        if (treffer != null)
        {
            TextBearbeiten(treffer, neu: false);
            return;
        }

        TextBearbeiten(new TextElement
        {
            X = c.X, Y = c.Y,
            Color = HexColor.Parse(CurrentInkHex(), HexColor.Black)
                            .MitGenugKontrast(_textGrundHex is null
                                ? null
                                : HexColor.Parse(_textGrundHex, HexColor.Black))
                            .ToString(),
            FontSize = 18f,
            Background = _textGrundHex,
        }, neu: true);
    }

    /// <summary>Notizzettel unter dem Zeiger bearbeiten — sonst einen neuen anlegen.</summary>
    private void BeginStickyInput(SKPoint c)
    {
        if (_page == null) return;

        var treffer = _page.Elements.OfType<StickyNoteElement>()
            .LastOrDefault(s => SKRect.Create(s.X, s.Y, s.Width, s.Height).Contains(c));
        if (treffer != null)
        {
            ZettelBearbeiten(treffer, neu: false);
            return;
        }

        // Der neue Zettel liegt mittig unter dem Zeiger — man setzt ihn dorthin, wo man
        // hinzeigt, nicht mit der Ecke daneben.
        ZettelBearbeiten(new StickyNoteElement
        {
            X = c.X - 100f, Y = c.Y - 100f,
            Color = _zettelfarbe.ToString(),
            TextColor = _zettelfarbe.LesbareSchrift().ToString(),
        }, neu: true);
    }

    // ==================== Das Eingabefeld ====================

    private void TextBearbeiten(TextElement el, bool neu)
    {
        BearbeitungAbschliessen();
        _bearbeiteterText = el;
        _bearbeitungIstNeu = neu;
        _bearbeitungVorher = el.Text;
        _bearbeitungVerwerfen = false;

        var schirm = ToScreen(new SKPoint(el.X, el.Y));
        Canvas.SetLeft(EditFeld, schirm.X - 4);
        Canvas.SetTop(EditFeld, schirm.Y - 3);

        // Die Zettel-Maße zurücksetzen — beide Werkzeuge teilen sich dieses Feld, und ein
        // Textfeld darf die Kastengröße des zuletzt bearbeiteten Zettels nicht erben.
        EditFeld.Width = double.NaN;
        EditFeld.Height = double.NaN;
        EditFeld.TextWrapping = TextWrapping.NoWrap;
        EditFeld.VerticalContentAlignment = VerticalAlignment.Center;

        EditFeld.FontSize = Math.Max(8, el.FontSize * Zoom);
        FeldfarbenSetzen(
            el.Background is { } grund
                ? HexColor.Parse(grund, HexColor.Black).ToBrush()
                : new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
            HexColor.Parse(el.Color, HexColor.Black).ToBrush());
        EditFeld.Text = el.Text;

        FeldZeigen();
    }

    private void ZettelBearbeiten(StickyNoteElement el, bool neu)
    {
        BearbeitungAbschliessen();
        _bearbeiteterZettel = el;
        _bearbeitungIstNeu = neu;
        _bearbeitungVorher = el.Text;
        _bearbeitungVerwerfen = false;

        var schirm = ToScreen(new SKPoint(el.X + ZettelRand, el.Y + ZettelRand));
        Canvas.SetLeft(EditFeld, schirm.X);
        Canvas.SetTop(EditFeld, schirm.Y);
        EditFeld.Width = Math.Max(24, (el.Width - ZettelRand * 2) * Zoom);
        EditFeld.Height = Math.Max(24, (el.Height - ZettelRand * 2) * Zoom);
        EditFeld.TextWrapping = TextWrapping.Wrap;
        EditFeld.VerticalContentAlignment = VerticalAlignment.Top;

        EditFeld.FontSize = Math.Max(8, el.FontSize * Zoom);
        FeldfarbenSetzen(
            HexColor.Parse(el.Color, HexColor.Black).ToBrush(),
            HexColor.Parse(el.TextColor, HexColor.Black).ToBrush());
        EditFeld.Text = el.Text;

        FeldZeigen();
    }

    /// <summary>
    /// Gibt dem Eingabefeld seine Farben — und zwar so, dass sie auch ankommen.
    ///
    /// <para>
    /// <b>Warum das mehr ist als zwei Zuweisungen.</b> <c>Foreground</c> trägt als gesetzter
    /// Wert; <c>Background</c> tut es <b>nicht</b>, und das ist am ausgelieferten Theme
    /// nachgelesen (<c>Avalonia.Themes.Fluent</c> 12.1.1, zerlegt wie in §4.42): Fluent setzt
    /// den Hintergrund nicht am <c>TextBox</c>, sondern <b>am Border im Template</b> —
    /// <c>TextBox:focus /template/ Border#PART_BorderElement</c> auf
    /// <c>TextControlBackgroundFocused</c>, dieselbe Stelle noch einmal für
    /// <c>:pointerover</c>. Dort ist unser Wert nicht gesetzt, also gewinnt der Setter des
    /// Themes, und im dunklen Erscheinungsbild ist das ein fast schwarzer Pinsel. Am
    /// laufenden Programm gemessen: über einem gelben Zettel stand ein schwarzes Feld mit
    /// unserer dunklen Schrift darin — <b>der Text war richtig, nur unlesbar</b>.
    /// </para>
    /// <para>
    /// <b>Warum über die Ressourcen und nicht über einen eigenen Style.</b> Die drei
    /// <c>TextControlBackground…</c>-Schlüssel sind die Stellschraube, die das Theme selbst
    /// vorsieht; wer sie am Feld überschreibt, muss nichts über Selektoren und ihre Vorränge
    /// annehmen. Und dass der Lookup vom Border aus hier oben ankommt, ist ebenfalls
    /// nachgelesen und nicht vermutet: <c>TemplatedControl.ApplyTemplate</c> hängt das
    /// Template als <b>logisches</b> Kind ein (<c>SetParent(this)</c>).
    /// </para>
    /// </summary>
    private void FeldfarbenSetzen(IBrush grund, IBrush schrift)
    {
        EditFeld.Background = grund;
        EditFeld.Foreground = schrift;

        // Alle drei Zustände, in denen ein Feld beim Beschriften steht: ruhend, unter dem
        // Zeiger, mit Fokus. Fehlte einer, bliebe genau der eine Zustand schwarz.
        EditFeld.Resources["TextControlBackground"] = grund;
        EditFeld.Resources["TextControlBackgroundPointerOver"] = grund;
        EditFeld.Resources["TextControlBackgroundFocused"] = grund;
    }

    /// <summary>
    /// Zeigt das Eingabefeld und gibt ihm den Fokus.
    ///
    /// <para>
    /// <b>Der Fokus wird hinten angestellt</b> (<see cref="Dispatcher"/>), damit er nach dem
    /// Anordnen kommt. <b>Ob ein direktes <c>Focus()</c> hier scheitert, ist NICHT gemessen</b> —
    /// beim Augenschein sah es zunächst so aus, aber die Ursache lag am Messwerkzeug:
    /// <c>tools/kette.ps1</c> holt das Fenster vor dem Tippen nach vorn, und das nimmt dem
    /// Feld den Fokus wieder. Am Bild <b>nach dem Klick allein</b> steht das Feld da und hat
    /// ihn. Der verzögerte Aufruf bleibt trotzdem stehen: er ist der robustere Weg und kostet
    /// nichts — <b>aber er löst kein belegtes Problem</b>, und das gehört dazugesagt.
    /// </para>
    /// </summary>
    private void FeldZeigen()
    {
        EditFeld.IsVisible = true;
        EditFeld.CaretIndex = EditFeld.Text?.Length ?? 0;
        Neuzeichnen();

        Dispatcher.UIThread.Post(() =>
        {
            if (!EditFeld.IsVisible) return;   // inzwischen abgeschlossen
            EditFeld.Focus();
            EditFeld.CaretIndex = EditFeld.Text?.Length ?? 0;
        }, DispatcherPriority.Input);
    }

    // ==================== Abschließen ====================

    /// <summary>Schließt eine offene Text- oder Zettel-Bearbeitung. Beides, denn nur eines läuft.</summary>
    private void BearbeitungAbschliessen()
    {
        TextAbschliessen();
        ZettelAbschliessen();
    }

    /// <summary>
    /// <b>Leerer Text löscht das Textfeld.</b> Ein Textfeld ohne Text ist unsichtbar und
    /// ließe sich nie wieder anfassen — es bliebe als Geist in der Datei stehen. Beim
    /// Notizzettel ist es umgekehrt (siehe <see cref="ZettelAbschliessen"/>).
    /// </summary>
    private void TextAbschliessen()
    {
        if (_bearbeiteterText == null || _page == null || _vm == null) return;
        var el = _bearbeiteterText;
        _bearbeiteterText = null;

        EditFeld.IsVisible = false;
        string neu = _bearbeitungVerwerfen ? _bearbeitungVorher : EditFeld.Text ?? "";
        _bearbeitungVerwerfen = false;

        if (_bearbeitungIstNeu)
        {
            if (!string.IsNullOrWhiteSpace(neu))
            {
                el.Text = neu;
                _page.Elements.Add(el);
                _vm.Undo.Push(_page, new AddElementsAction([el]));
                MarkDirty();
            }
        }
        else if (string.IsNullOrWhiteSpace(neu))
        {
            var aktion = new RemoveElementsAction(_page, [el]);
            aktion.Redo(_page);
            _vm.Undo.Push(_page, aktion);
            MarkDirty();
        }
        else if (neu != _bearbeitungVorher)
        {
            el.Text = neu;
            _vm.Undo.Push(_page, new TextChangeAction(el, _bearbeitungVorher, neu));
            MarkDirty();
        }

        Neuzeichnen();
    }

    /// <summary>
    /// <b>Ein Zettel bleibt auch leer stehen.</b> Anders als ein Textfeld ist er sichtbar —
    /// wer ihn gesetzt hat, wollte ihn, und ein Zettel, den man erst beschriften muss, damit
    /// er bleibt, wäre eine Falle. Weg kommt er über den Radierer oder die Auswahl.
    /// </summary>
    private void ZettelAbschliessen()
    {
        if (_bearbeiteterZettel == null || _page == null || _vm == null) return;
        var el = _bearbeiteterZettel;
        _bearbeiteterZettel = null;

        EditFeld.IsVisible = false;
        string neu = _bearbeitungVerwerfen ? _bearbeitungVorher : EditFeld.Text ?? "";
        _bearbeitungVerwerfen = false;

        if (_bearbeitungIstNeu)
        {
            el.Text = neu;
            _page.Elements.Add(el);
            _vm.Undo.Push(_page, new AddElementsAction([el]));
            MarkDirty();
        }
        else if (neu != _bearbeitungVorher)
        {
            el.Text = neu;
            _vm.Undo.Push(_page, new StickyTextChangeAction(el, _bearbeitungVorher, neu));
            MarkDirty();
        }

        Neuzeichnen();
    }

    // ==================== Tastatur ====================

    private void EditFeld_Taste(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _bearbeitungVerwerfen = true;
            Skia.Focus();          // löst LostFocus aus und damit das Abschließen
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            // Strg+Eingabe schließt ab; Eingabe allein macht einen Absatz (AcceptsReturn).
            Skia.Focus();
            e.Handled = true;
        }
    }

    private void EditFeld_Verlassen(object? sender, RoutedEventArgs e) => BearbeitungAbschliessen();

    /// <summary>
    /// Läuft gerade eine Beschriftung? Dann darf der Zeichner das Element <b>nicht</b> malen —
    /// sonst stünde der Text doppelt da, einmal im Eingabefeld und einmal darunter.
    /// </summary>
    private bool WirdBearbeitet(WbElement el) =>
        ReferenceEquals(el, _bearbeiteterText) || ReferenceEquals(el, _bearbeiteterZettel);

    // ==================== Einstellungen ====================

    private void TextGrund_Umgeschaltet(object? sender, RoutedEventArgs e)
    {
        if (_suppressToolEvents) return;
        _textGrundHex = TextGrundAn.IsChecked == true ? _textGrundFarbe.ToString() : null;
        TextGrundVorschauNachfuehren();
    }

    /// <summary>
    /// Die zuletzt gewählte Hintergrundfarbe. <b>Sie überlebt das Ausschalten</b> — wer den
    /// Grund abschaltet und wieder an, bekommt seine Farbe zurück und nicht die Vorgabe.
    /// </summary>
    private HexColor _textGrundFarbe = new(0xE6, 0xFF, 0xFF, 0xFF);

    private void TextGrundfarbe_Click(object? sender, RoutedEventArgs e)
    {
        if (ColorPickerWindow.Waehlen(TopLevel.GetTopLevel(this) as Window,
                _textGrundFarbe, mitDeckkraft: true) is not { } gewaehlt)
            return;

        _textGrundFarbe = gewaehlt;
        _textGrundHex = gewaehlt.ToString();

        // Eine Farbe zu wählen heißt, sie benutzen zu wollen — wie bei der Formfüllung.
        _suppressToolEvents = true;
        TextGrundAn.IsChecked = true;
        _suppressToolEvents = false;

        TextGrundVorschauNachfuehren();
    }

    private void TextGrundVorschauNachfuehren() =>
        TextGrundVorschau.Background = _textGrundHex is null
            ? Brushes.Transparent
            : _textGrundFarbe.ToBrush();

    private void Zettelfarbe_Click(object? sender, RoutedEventArgs e)
    {
        if (ColorPickerWindow.Waehlen(TopLevel.GetTopLevel(this) as Window,
                _zettelfarbe, mitDeckkraft: false) is not { } gewaehlt)
            return;

        _zettelfarbe = gewaehlt;
        ZettelVorschauNachfuehren();
    }

    private void ZettelVorschauNachfuehren() => ZettelVorschau.Background = _zettelfarbe.ToBrush();
}
