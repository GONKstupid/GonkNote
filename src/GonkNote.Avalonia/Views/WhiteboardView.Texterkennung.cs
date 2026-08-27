using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Core.Services;
using GonkNote.Platform;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Texterkennung auf der Zeichenfläche (Phase 4.5, Stück 6 — §4.64). Erkennt Text in den
/// ausgewählten Bildern oder, wenn nichts ausgewählt ist, im eingefügten Seitenhintergrund;
/// das Ergebnis kommt in ein Fenster zum Nachbessern, Kopieren oder Ablegen als Notizzettel.
///
/// <para>
/// <b>Die Maschine dahinter ist dieselbe wie im WPF-Kopf</b> — <c>TesseractOcrEngine</c> in
/// GonkNote.Ocr, über <see cref="IPlatformServices.Ocr"/>. Was hier steht, ist nur die
/// Bedienung: welche Bilder gemeint sind, was währenddessen auf dem Schirm passiert, und
/// wohin der Text danach geht.
/// </para>
///
/// <para>
/// <b>Welche Quelle gilt, rechnet Core</b> (<see cref="WbSchnellaktionen.Rechnen"/>) — der
/// Knopf ist gesperrt, wenn es nichts zu erkennen gibt. Die Auswahl hier muss deshalb
/// dieselbe Regel treffen, sonst wäre ein bedienbarer Knopf ohne Wirkung möglich.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>
    /// Der ganze Ablauf. <b>Erkannt wird auf einem Hintergrund-Faden</b> — eine A4-Seite
    /// kostet je nach Gerät mehrere Sekunden, und auf dem Oberflächen-Faden stünde
    /// währenddessen das Bild.
    /// </summary>
    private async Task TexterkennungLaufen()
    {
        if (_page == null || _vm == null || _importLaeuft) return;

        var bilder = Quellbilder();
        if (bilder.Count == 0) return;

        _importLaeuft = true;
        WarteZeigen(Loc.T(bilder.Count > 1 ? "Msg.OcrRunningMany" : "Msg.OcrRunning"));
        try
        {
            string text = await Task.Run(() =>
            {
                var sb = new StringBuilder();
                foreach (var daten in bilder)
                {
                    string t = App.Platform.Ocr.Recognize(daten);
                    if (t.Length == 0) continue;
                    if (sb.Length > 0) sb.Append("\n\n");
                    sb.Append(t);
                }
                return sb.ToString();
            });

            WarteVerbergen();

            if (string.IsNullOrWhiteSpace(text))
            {
                MessageWindow.Zeige(AvaloniaDialogService.Besitzer(),
                    Loc.T("Msg.OcrNoText"), DialogSeverity.Information, frage: false);
                Skia.Focus();
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window besitzer)
            {
                Skia.Focus();
                return;
            }

            var fenster = TexterkennungWindow.Zeige(besitzer, text);
            if (fenster.AlsZettel) AlsZettelAblegen(fenster.Ergebnis);
            else Skia.Focus();
        }
        catch (Exception ex)
        {
            WarteVerbergen();
            MessageWindow.Zeige(AvaloniaDialogService.Besitzer(),
                Loc.T("Msg.OcrFailed", ex.Message), DialogSeverity.Warning, frage: false);
            Skia.Focus();
        }
        finally
        {
            _importLaeuft = false;
            WarteVerbergen();
        }
    }

    /// <summary>
    /// Woran erkannt wird: die ausgewählten Bilder — und nur wenn <b>gar nichts</b>
    /// ausgewählt ist, der Seitenhintergrund.
    ///
    /// <para>
    /// <b>Dieselbe Regel wie in <see cref="WbSchnellaktionen.Rechnen"/></b>, und das ist kein
    /// Zufall: dort entscheidet sie, ob der Knopf überhaupt anspricht. Liefe sie hier anders,
    /// gäbe es einen Knopf, der bedienbar ist und nichts tut — oder umgekehrt.
    /// </para>
    /// </summary>
    private List<byte[]> Quellbilder()
    {
        var bilder = _selection.OfType<ImageElement>()
            .Select(im => ImageCache.Bytes(im.Id, im.Data))
            .OfType<byte[]>()
            .Where(d => d.Length > 0)
            .ToList();

        if (bilder.Count == 0 && _selection.Count == 0 && _page != null &&
            ImageCache.Bytes(_page.BackgroundImageId, _page.BackgroundImage) is { Length: > 0 } grund)
            bilder.Add(grund);

        return bilder;
    }

    /// <summary>
    /// Legt den erkannten Text als Notizzettel in die Mitte der Sicht und wählt ihn aus.
    ///
    /// <para>
    /// <b>Und der Fokus geht zurück auf die Fläche.</b> Ohne das behielte das eben
    /// geschlossene Fenster ihn, und danach käme keine Taste mehr an — nicht Strg+Z, kein
    /// Werkzeug-Kürzel (§4.56, am laufenden Programm gemessen; der WPF-Kopf zeigt das nicht).
    /// </para>
    /// </summary>
    private void AlsZettelAblegen(string text)
    {
        if (_page == null || _vm == null || string.IsNullOrWhiteSpace(text))
        {
            Skia.Focus();
            return;
        }

        const float b = 280f, h = 220f;
        var mitte = Sichtmitte();

        var zettel = new StickyNoteElement
        {
            X = mitte.X - b / 2f,
            Y = mitte.Y - h / 2f,
            Width = b,
            Height = h,
            Text = text.Trim(),
            Color = _zettelfarbe.ToString(),
            TextColor = _zettelfarbe.LesbareSchrift().ToString(),
        };

        // Auf einer begrenzten Seite darf der Zettel nicht über den Rand hängen. Auf einer
        // unendlichen gibt es keinen, an dem er sich stoßen könnte.
        if (!_page.IsInfinite)
        {
            zettel.X = Math.Clamp(zettel.X, 0, Math.Max(0, _page.Width - b));
            zettel.Y = Math.Clamp(zettel.Y, 0, Math.Max(0, _page.Height - h));
        }

        _page.Elements.Add(zettel);
        _vm.Undo.Push(_page, new AddElementsAction([zettel]));
        MarkDirty();

        // AuswahlZeigen macht beides: aufs Verschieben umschalten und den Fokus zurückgeben.
        AuswahlZeigen([zettel]);
    }
}
