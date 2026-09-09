using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using GonkNote.ViewModels;

namespace GonkNote.Views;

/// <summary>
/// Die Einstellungen-Seitenleiste rechts: Seite (Muster, Farbton, Format, Ausrichtung),
/// Formen, Text, Notizzettel, Sticker und <b>Export</b>.
///
/// <para>
/// <b>Hier stand „nur die Seite", und dazu: „Diese Werkzeuge gibt es im Linux-Kopf nicht
/// (nicht M1)".</b> Das galt bis Phase 4.5 und ist seitdem in beiden Hälften falsch gewesen —
/// Formen (§4.53), Text und Notizzettel (§4.55) und Sticker (§4.56) werden gleich unten in
/// dieser Datei gespiegelt. <b>Der Satz ist als Zahl weitergereicht worden</b> („sieben
/// Klappgruppen gegen eine", §4.71/§4.75) und hat einen Auftrag mitgeprägt, der etwas bauen
/// wollte, das dastand.
/// </para>
///
/// <para>
/// ⛔ <b>Und der Satz darunter war seinerseits abgelaufen:</b> „Was jetzt noch fehlt, ist das
/// Cover." Das Cover ist in Phase 5, Schritt ①c gebaut worden (§4.81) — die Leiste hat seitdem
/// alle sieben Abschnitte. <i>Derselbe Fehler wie der im Absatz darüber, im selben Kommentar,
/// eine Runde später.</i>
/// </para>
///
/// <para>
/// <b>Seit dem 2026-09-09 sind die Abschnitte klappbar</b> (Nutzerwunsch). Die Leiste geht mit
/// <b>allem eingeklappt</b> auf; aufgeklappt wird von Hand oder durch das Werkzeug, das den
/// Abschnitt mitbringt. Vorher wurden die werkzeugeigenen Abschnitte ein- und
/// <b>ausgeblendet</b> — die Leiste war damit länger als das Fenster, und was gerade fehlte,
/// sah aus, als gäbe es das gar nicht.
/// </para>
///
/// <para>
/// <b>Es gibt keinen OK-Knopf.</b> Jede Änderung wirkt sofort auf die aktuelle Seite; das
/// ist dieselbe Bedienung wie drüben und der Grund, warum die Umschalter beim Spiegeln
/// stummgeschaltet werden müssen (<see cref="_stummeEinstellungen"/>) — sonst löste das
/// Setzen der Haken die Änderung aus, die es nur abbilden soll.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    private bool _stummeEinstellungen;

    // ==================== Die Klappgruppen ====================

    /// <summary>
    /// Kopf und Inhalt jedes Abschnitts, in der Reihenfolge der Leiste.
    ///
    /// <para>
    /// <b>Eine Tabelle statt sieben Zuweisungen</b> — das ist der Punkt der ganzen Umstellung.
    /// Vorher standen die vier werkzeugeigenen Abschnitte als vier Zeilen in
    /// <c>SetTool</c> (<c>FormenBereich.IsVisible = tool == ToolType.Shape;</c> und so fort),
    /// und ein fünfter Abschnitt hätte eine fünfte Zeile gebraucht, die man vergisst — genau
    /// die Falle, die §4.78 den Formen-Stift gekostet hat.
    /// </para>
    /// <para>
    /// <b>Die Zuordnung Werkzeug → Abschnitt steht nicht hier</b>, sondern in
    /// <see cref="WbLeiste.BereichVon"/>. Hier steht nur, welche zwei Steuerelemente zu einem
    /// Abschnitt gehören.
    /// </para>
    /// </summary>
    private (ToggleButton Kopf, Control Inhalt, WbLeiste.Einstellungsbereich Bereich)[] Abschnittstabelle =>
    [
        (KopfSeite,    SeiteBereich,   WbLeiste.Einstellungsbereich.Seite),
        (KopfFormen,   FormenBereich,  WbLeiste.Einstellungsbereich.Formen),
        (KopfText,     TextBereich,    WbLeiste.Einstellungsbereich.Text),
        (KopfZettel,   ZettelBereich,  WbLeiste.Einstellungsbereich.Zettel),
        (KopfSticker,  StickerBereich, WbLeiste.Einstellungsbereich.Sticker),
        (KopfCover,    CoverBereich,   WbLeiste.Einstellungsbereich.Cover),
        (KopfExport,   ExportBereich,  WbLeiste.Einstellungsbereich.Export),
    ];

    /// <summary>
    /// Hängt die Köpfe ein. <b>Einmal beim Aufbau</b>, aus dem Konstruktor der Ansicht — ein
    /// zweiter Aufruf hinge jeden Handler ein zweites Mal an, und dann klappte ein Klick den
    /// Abschnitt auf und sofort wieder zu.
    /// </summary>
    private void AbschnitteEinhaengen()
    {
        foreach (var (kopf, inhalt, _) in Abschnittstabelle)
        {
            var ziel = inhalt;
            kopf.IsCheckedChanged += (_, _) => ziel.IsVisible = kopf.IsChecked == true;
        }
    }

    /// <summary>
    /// Klappt genau einen Abschnitt auf und alle anderen zu.
    ///
    /// <para>
    /// <b><see cref="WbLeiste.Einstellungsbereich.Keiner"/> klappt nicht alles zu</b>, und das
    /// ist die eine Entscheidung, die hier zu treffen war. Wer vom Formen- zum Stift-Werkzeug
    /// wechselt, hat nichts über die Einstellungsleiste gesagt — ihm die Gruppe zuzuklappen,
    /// die er gerade von Hand geöffnet hat, wäre eine Antwort auf eine ungestellte Frage.
    /// Zugeklappt werden deshalb nur die <b>werkzeugeigenen</b> Abschnitte: einer davon ohne
    /// sein Werkzeug ist eine Einstellung ohne Gegenstand (dieselbe Begründung, mit der sie
    /// vorher ganz verschwanden). Seite, Cover und Export bleiben, wie der Nutzer sie
    /// hinterlassen hat.
    /// </para>
    /// </summary>
    private void BereichAufklappen(WbLeiste.Einstellungsbereich bereich)
    {
        foreach (var (kopf, _, eigener) in Abschnittstabelle)
        {
            if (eigener == bereich) kopf.IsChecked = true;
            else if (WerkzeugAbschnitt(eigener)) kopf.IsChecked = false;
        }
    }

    /// <summary>
    /// Hängt der Abschnitt an einem Werkzeug? <b>Aus <see cref="WbLeiste.BereichVon"/>
    /// abgeleitet und nicht danebengeschrieben</b> — sonst gäbe es die Liste zweimal, und die
    /// zweite veraltet.
    /// </summary>
    private static bool WerkzeugAbschnitt(WbLeiste.Einstellungsbereich bereich) =>
        Enum.GetValues<ToolType>().Any(t => WbLeiste.BereichVon(t) == bereich);

    private void Einstellungen_Click(object? sender, RoutedEventArgs e)
    {
        if (EinstellungenLeiste.IsVisible)
        {
            EinstellungenLeiste.IsVisible = false;
            return;
        }

        // **Erst sichtbar machen, dann spiegeln.** Umgekehrt steigt das Spiegeln sofort
        // wieder aus (es tut nichts an einer verborgenen Leiste), und die Leiste ginge mit
        // lauter leeren Umschaltern auf — am laufenden Programm genau so gesehen.
        EinstellungenLeiste.IsVisible = true;
        EinstellungenSpiegeln();

        // Von Hand aufgemacht heißt: der Nutzer sucht etwas und hat nicht gesagt, was. Den
        // Abschnitt des aktuellen Werkzeugs klappt er trotzdem auf — es ist die einzige
        // Vermutung, die es gibt, und sie kostet einen Klick, wenn sie falsch ist.
        BereichAufklappen(WbLeiste.BereichVon(_tool));
    }

    /// <summary>
    /// Bildet die aktuelle Seite in den Umschaltern ab. Wird von allem gerufen, was
    /// <see cref="_page"/> wechselt — sonst zeigte die offene Leiste nach einem
    /// Seitenwechsel die Einstellungen der vorigen Seite an, und die nächste Änderung
    /// schriebe sie der neuen auf.
    /// </summary>
    private void EinstellungenSpiegeln()
    {
        if (_vm == null || _page == null || !EinstellungenLeiste.IsVisible) return;

        // Die Vorschauflächen der Werkzeug-Sektionen (Phase 4.5). Sie hängen nicht an der
        // Seite, sondern am gewählten Werkzeug — spiegeln muss man sie trotzdem hier, sonst
        // gehen sie leer auf. Genau das ist in §4.53 an der zweiten Aufklappstelle passiert.
        FuellvorschauNachfuehren();
        TextGrundVorschauNachfuehren();
        ZettelVorschauNachfuehren();

        _stummeEinstellungen = true;

        (_page.Background switch
        {
            PageBackground.Lines => SetzeMusterLinien,
            PageBackground.Grid => SetzeMusterKaro,
            PageBackground.Dots => SetzeMusterPunkte,
            _ => SetzeMusterBlanko,
        }).IsChecked = true;

        (_page.Shade switch
        {
            PageShade.Light => SetzeTonHell,
            PageShade.Dark => SetzeTonDunkel,
            _ => SetzeTonAuto,
        }).IsChecked = true;

        bool geheftet = !_page.IsInfinite;
        FormatAbschnitt.IsVisible = geheftet;
        if (geheftet)
        {
            // Erkannt an der langen Seite, nicht an Breite und Höhe einzeln — sonst fiele
            // ein Querformat-A4 durch, dessen Breite größer als die A4-Höhe ist.
            float lang = Math.Max(_page.Width, _page.Height);
            (lang > WhiteboardDoc.A4Height + 1 ? SetzeFormatA3 : SetzeFormatA4).IsChecked = true;
            (_page.Width > _page.Height ? SetzeQuerformat : SetzeHochformat).IsChecked = true;
        }

        CoverHinweis.IsVisible = _page.IsCover;

        // Der Cover-Abschnitt (§4.81). **Hier und nicht in seiner eigenen Datei** — wer ihn
        // ohne Spiegeln aufklappt, bekommt lauter leere Schalter (§4.53).
        CoverSpiegeln();

        _stummeEinstellungen = false;
    }

    /// <summary>
    /// Eine Änderung im Panel wirkt sofort auf die aktuelle Seite. Aufgebaut wie
    /// <c>PageSetting_Changed</c> im WPF-Kopf — bis auf die eine Zeile, die es dort nicht
    /// braucht und hier zwingend ist: siehe <see cref="RefreshAutoSwatch"/> unten.
    /// </summary>
    private void Seiteneinstellung_Geaendert(object? sender, RoutedEventArgs e)
    {
        if (_stummeEinstellungen || _vm == null || _page == null) return;

        _page.Background =
            SetzeMusterLinien.IsChecked == true ? PageBackground.Lines
            : SetzeMusterKaro.IsChecked == true ? PageBackground.Grid
            : SetzeMusterPunkte.IsChecked == true ? PageBackground.Dots
            : PageBackground.Blank;

        _page.Shade =
            SetzeTonHell.IsChecked == true ? PageShade.Light
            : SetzeTonDunkel.IsChecked == true ? PageShade.Dark
            : PageShade.Auto;

        if (!_page.IsInfinite)
        {
            bool a3 = SetzeFormatA3.IsChecked == true;
            float breit = a3 ? WhiteboardDoc.A3Width : WhiteboardDoc.A4Width;
            float hoch = a3 ? WhiteboardDoc.A3Height : WhiteboardDoc.A4Height;

            bool quer = SetzeQuerformat.IsChecked == true;
            float nb = quer ? hoch : breit, nh = quer ? breit : hoch;

            bool andereGroesse = Math.Abs(_page.Width - nb) > 0.5f || Math.Abs(_page.Height - nh) > 0.5f;
            _page.Width = nb;
            _page.Height = nh;
            // Ein anderes Blatt sitzt sonst außermittig oder halb außerhalb der Fläche.
            if (andereGroesse) CenterView();

            if (SetzeAlsStandard.IsChecked == true)
                _vm.Doc.NewPageTemplate = new PageTemplate
                {
                    Width = nb,
                    Height = nh,
                    Background = _page.Background,
                    Shade = _page.Shade,
                };
        }

        // **Der Farbton bestimmt die Vorgabetinte.** Wer den Farbton auf Dunkel stellt und
        // die Kachel stehen lässt, schriebe sonst mit der alten Tinte weiter — im
        // schlimmsten Fall dunkel auf dunkel. Der WPF-Kopf braucht die Zeile nicht, weil er
        // die Kachel im Zeichenpfad nachführt; hier ist das ausgeschlossen (HANDOFF §7,
        // „Render läuft im Renderdurchlauf").
        RefreshAutoSwatch();

        MarkDirty();
        Neuzeichnen();
    }

    // ==================== Export ====================

    /// <summary>
    /// Exportiert die ganze Tafel — <b>neu in Phase 5, Schritt ①c</b>, und zwar in beiden
    /// Bedeutungen: der Knopf ist neu, und der Weg dahinter existierte in diesem Kopf
    /// überhaupt nicht (<c>AvaloniaDocumentIo.ExportBoard</c> warf, die Formatliste war leer).
    /// Er liegt seitdem in Core (<see cref="Core.Rendering.WbExport"/>) und ist derselbe wie
    /// drüben.
    ///
    /// <para>
    /// <b>Der Kopf exportiert nicht selbst</b>, er wählt nur das Format vor: <c>MainViewModel</c>
    /// speichert zuerst den offenen Stand ins Modell und ruft danach <c>IDocumentIo</c>.
    /// Derselbe Weg wie „Datei → Exportieren" und wie <c>TextDocView.Export_Click</c> —
    /// <b>ein zweiter Weg wäre die Falle aus §4.13.</b>
    /// </para>
    /// </summary>
    private void TafelExport_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string endung } &&
            (TopLevel.GetTopLevel(this) as Window)?.DataContext is MainViewModel vm)
            vm.ExportActiveTab(endung);
    }
}
