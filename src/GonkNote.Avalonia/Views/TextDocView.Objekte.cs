using Avalonia.Controls;
using Avalonia.Interactivity;
using GonkNote.Core.Platform;
using GonkNote.Core.Services;
using GonkNote.Core.Theming;
using GonkNote.Platform;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// <b>Bild, Infobox, Beschriftung und die Objekt-Anordnung</b> — Phase 5, Schritt ①c
/// (HANDOFF §4.89).
///
/// <para>
/// <b>Sie rechnet nichts</b>, wie die Geschwisterdateien: <see cref="TdGrafikEdit"/> weiß, was
/// eine Grafik im Dokument ändert. Hier steht die Übersetzung von Klicks in diese Aufrufe —
/// und das eine, was nur hier geht: die Datei aufmachen und ihre Bytes in den Blob-Speicher
/// legen.
/// </para>
/// </summary>
public partial class TextDocView
{
    // ==================== Bild ====================

    /// <summary>
    /// <b>Die Originalbytes gehen unangetastet in den Blob-Speicher</b> (§4.14, <c>TdImage</c>):
    /// Ein JPEG, das beim Einfügen als PNG neu kodiert würde, ist um ein Vielfaches größer —
    /// das ist die gemessene Entscheidung aus V1, an der LiteDB seinerzeit die 16-MB-Grenze
    /// gerissen hat.
    ///
    /// <para>
    /// <b>Die Anzeigegröße wird aus den Bildpunkten gerechnet und nicht geraten:</b> 96 dpi,
    /// gedeckelt auf die nutzbare Textbreite. Ein Foto mit 4000 Bildpunkten Breite stünde sonst
    /// über einen Meter breit im Dokument und wäre nach dem Einfügen nicht mehr zu finden.
    /// </para>
    /// </summary>
    private void Bild_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        var dateien = App.Platform.Files.Open(
            Loc.T("Ed.Image.Insert"),
            [new FileFilter(Loc.T("Filter.ImagesImport"), Bildsammlung.ImportEndungen)]);

        if (dateien.Count == 0) return;

        byte[] bytes;
        try { bytes = File.ReadAllBytes(dateien[0]); }
        catch (Exception fehler)
        {
            Fehlerprotokoll.Schreiben(fehler);
            App.Platform.Dialogs.Inform(
                Loc.T("Msg.ImportFailed") + " " + fehler.Message, DialogSeverity.Warning);
            return;
        }

        var endung = Path.GetExtension(dateien[0]).TrimStart('.').ToLowerInvariant();
        var kennung = new TdBlobImages(App.Db.Blobs).Ablegen(bytes, endung);

        var (breiteCm, hoeheCm) = Anzeigegroesse(bytes);

        Aendern(TdGrafikEdit.Einfuegen(
            _modell!, _auswahl, new TdImage(kennung, endung, breiteCm, hoeheCm)));

        Skia.Focus();
    }

    /// <summary>
    /// Breite und Höhe in Zentimetern, aus den Bildpunkten bei 96 dpi — gedeckelt auf die
    /// nutzbare Textbreite der Seite. <b>Das Seitenverhältnis bleibt beim Deckeln erhalten.</b>
    /// </summary>
    private (double Breite, double Hoehe) Anzeigegroesse(byte[] bytes)
    {
        const double ZollProCm = 2.54;
        const double Dpi = 96;

        double breite = 8, hoehe = 6;

        if (SkiaSharp.SKBitmap.Decode(bytes) is { } bild)
        {
            using (bild)
            {
                breite = bild.Width / Dpi * ZollProCm;
                hoehe = bild.Height / Dpi * ZollProCm;
            }
        }

        double platz = Textbreite();
        if (breite > platz && breite > 0)
        {
            hoehe *= platz / breite;
            breite = platz;
        }

        return (Math.Max(breite, TdGrafikEdit.MindestCm),
                Math.Max(hoehe, TdGrafikEdit.MindestCm));
    }

    /// <summary>Die nutzbare Textbreite der Seite, an der die Auswahl steht.</summary>
    private double Textbreite()
    {
        var seite = Abschnitt()?.Page;
        return seite is null ? 16 : Math.Max(4, seite.WidthCm - seite.MarginLeftCm - seite.MarginRightCm);
    }

    // ==================== Infobox ====================

    /// <summary>
    /// <b>Was eine Infobox ist, steht in Core</b> (<see cref="TdBlockEdit.Infobox"/>) — hier
    /// stehen nur ihre zwei Farben.
    ///
    /// <para>
    /// <b>⛔ Und sie kommen aus der HELLEN Tabelle, auch im dunklen Erscheinungsbild.</b> Das
    /// ist keine Nachlässigkeit, sondern §1: <i>ein Dokument ist Papier.</i> Der erste Anlauf
    /// nahm die Farben des laufenden Themes — und im dunklen Modus entstand ein
    /// **dunkelblauer** Kasten mit schwarzem Text darin, der so ins PDF und ins DOCX gegangen
    /// wäre. <b>Es betrifft nicht das Aussehen, sondern die gespeicherten Daten</b>: derselbe
    /// Klick hätte je nach Theme ein anderes Dokument ergeben — genau der Fehler aus §4.79 (b).
    /// Am laufenden Programm gesehen, bei grünem Bau (§4.89).
    /// </para>
    /// <para>
    /// <b>Aus der Tabelle in Core bleibt es trotzdem</b> (§5 Nr. 27) — nur eben aus der
    /// Papier-Variante, so wie <see cref="TdCharFormat.Standard"/> sein Schwarz von dort nimmt
    /// und nicht vom Theme.
    /// </para>
    /// </summary>
    private void Infobox_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        Aendern(TdBlockEdit.Infobox(
            _modell!, _auswahl,
            fuellung: Themes.Light[ThemeColor.AccentSoft].ToString(),
            rahmen: Themes.Light[ThemeColor.Accent].ToString()));

        Skia.Focus();
    }

    // ==================== Beschriftung ====================

    private void Beschriftung_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        Aendern(TdGrafikEdit.Beschriftung(_modell!, _auswahl, Loc.T("Ed.Caption.Prefix")));
        Skia.Focus();
    }

    // ==================== Objekt-Anordnung ====================

    /// <summary>
    /// Die sechs Schritt-Knöpfe. <b>Die Faktoren stehen in Core</b> und nicht hier — dieselben
    /// wie drüben (§6: beim Editor ist Windows die Vorlage).
    /// </summary>
    private void Objekt_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar || s is not Control knopf) return;

        var (breite, hoehe) = (string?)knopf.Tag switch
        {
            "Bigger" => (1.15, 1.15),
            "Smaller" => (0.87, 0.87),
            "Wider" => (1.15, 1.00),
            "Narrower" => (0.87, 1.00),
            "Taller" => (1.00, 1.15),
            "Shorter" => (1.00, 0.87),
            _ => (1.00, 1.00),
        };

        Aendern(TdGrafikEdit.Groesse(_modell!, _auswahl, breite, hoehe));
        ObjekteNachziehen();
        Skia.Focus();
    }

    private void GenaueGroesse_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar) return;

        Aendern(TdGrafikEdit.GroesseSetzen(
            _modell!, _auswahl,
            (double)(ObjektBreite.Value ?? 8), (double)(ObjektHoehe.Value ?? 6)));

        ObjekteNachziehen();
        Skia.Focus();
    }

    /// <summary>
    /// Stellt die Objekt-Knöpfe auf das, was an der Auswahl steht. <b>Sie sind aus, wo keine
    /// Grafik ist</b> — ein Knopf, der aussieht, als täte er etwas, und nichts tut, ist der
    /// Fehler aus §4.78.
    /// </summary>
    private void ObjekteNachziehen()
    {
        if (ObjektBreite is null) return;

        var gefunden = Schreibbar ? TdGrafikEdit.GrafikAn(_modell!, _auswahl) : null;
        bool an = gefunden is not null;

        foreach (var knopf in ObjektKnoepfe.Children.OfType<Control>())
            knopf.IsEnabled = an;

        ObjektBreite.IsEnabled = an;
        ObjektHoehe.IsEnabled = an;

        if (gefunden is { } treffer)
        {
            ObjektBreite.Value = (decimal)Math.Round(treffer.Grafik.WidthCm, 2);
            ObjektHoehe.Value = (decimal)Math.Round(treffer.Grafik.HeightCm, 2);
        }
    }
    // ==================== Seitenhintergrund / Wasserzeichen (§4.89) ====================

    /// <summary>
    /// <b>Der Posten, der auf keiner Aufgabenliste stand.</b> Das Modell kann ein Wasserzeichen
    /// seit §4.15 (<see cref="TdPageSetup.Watermark"/>), <c>TdRenderer</c> zeichnet es, und
    /// <c>AvaloniaDocumentIo</c> bewahrt es beim Aufräumen — <b>nur setzen konnte man es
    /// nicht</b>. Ein Dokument mit Wasserzeichen sah man also, ein neues bekam keines. Gefunden
    /// hat es erst der Abgleich aller <c>Ed.*</c>-Schlüssel beider Köpfe (§4.86).
    ///
    /// <para>
    /// <b>Es hängt am Abschnitt und nicht am Dokument</b> — dort steht es im Modell, und dort
    /// steht es auch in DOCX. Wer zwei Abschnitte hat, darf sie verschieden bebildern.
    /// </para>
    /// <para>
    /// <b>Kein Verlaufsschritt.</b> Das Wasserzeichen gehört zur Seiteneinrichtung, und die
    /// führt der Verlauf nicht — genau wie Papierformat und Ränder daneben (§4.15). Wer das
    /// ändert, ändert es für alle Blocklisten des Abschnitts auf einmal, und ein Blocktausch
    /// träfe es nicht.
    /// </para>
    /// </summary>
    private void Wasserzeichen_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar || Abschnitt() is not { } abschnitt) return;

        var dateien = App.Platform.Files.Open(
            Loc.T("Ed.Background.Choose"),
            [new FileFilter(Loc.T("Filter.ImagesImport"), Bildsammlung.ImportEndungen)]);

        if (dateien.Count == 0) return;

        byte[] bytes;
        try { bytes = File.ReadAllBytes(dateien[0]); }
        catch (Exception fehler)
        {
            Fehlerprotokoll.Schreiben(fehler);
            App.Platform.Dialogs.Inform(
                Loc.T("Msg.ImportFailed") + " " + fehler.Message, DialogSeverity.Warning);
            return;
        }

        var endung = Path.GetExtension(dateien[0]).TrimStart('.').ToLowerInvariant();
        var kennung = new TdBlobImages(App.Db.Blobs).Ablegen(bytes, endung);
        var (breiteCm, hoeheCm) = Anzeigegroesse(bytes);

        abschnitt.Page.Watermark = new TdImage(kennung, endung, breiteCm, hoeheCm);

        SeiteGeaendert();
    }

    private void WasserzeichenWeg_Click(object? s, RoutedEventArgs e)
    {
        if (!Schreibbar || Abschnitt() is not { } abschnitt) return;
        if (abschnitt.Page.Watermark is null) return;

        abschnitt.Page.Watermark = null;
        SeiteGeaendert();
    }

    private void WasserzeichenDeckkraft_Geaendert(
        object? s, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!Schreibbar || _fuellt || Abschnitt() is not { } abschnitt) return;
        if (abschnitt.Page.Watermark is null) return;

        abschnitt.Page.WatermarkOpacity = e.NewValue / 100.0;
        SeiteGeaendert();
    }

    /// <summary>Stellt die Wasserzeichen-Bedienung auf das, was im Abschnitt steht.</summary>
    private void WasserzeichenNachziehen()
    {
        if (WasserzeichenDeckkraft is null) return;

        var seite = Abschnitt()?.Page;
        bool hat = seite?.Watermark is not null;

        KnopfWasserzeichen.IsEnabled = Schreibbar;
        KnopfWasserzeichenWeg.IsEnabled = hat;
        WasserzeichenDeckkraft.IsEnabled = hat;
        WasserzeichenDeckkraft.Value = (seite?.WatermarkOpacity ?? 0.35) * 100;
    }
}