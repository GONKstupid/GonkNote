using System.IO;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Das Sticker-Werkzeug — das Gegenstück zu <c>WhiteboardView.Stickers.cs</c> im WPF-Kopf,
/// neu in Phase 4.5 (§4.56).
///
/// <para>
/// <b>Ein Sticker ist kein eigener Elementtyp.</b> Er wird als gewöhnliches
/// <see cref="ImageElement"/> abgelegt; die Sammlung ist eine <em>Bildquelle</em> und sonst
/// nichts. Das ist beim Messen herausgekommen und hat den Zuschnitt der Runde geändert — es
/// gibt kein <c>StickerElement</c>, und wer eines suchte, fände es nicht.
/// </para>
/// <para>
/// <b>Damit ist das hier zugleich der erste Bild-Einfügeweg des Linux-Kopfs.</b> Er konnte
/// bisher überhaupt kein Bild auf die Fläche bringen — anzeigen konnte er es immer
/// (<see cref="WbRenderer.DrawImage"/> liegt in Core), nur entstanden ist dort nie eines.
/// </para>
/// <para>
/// <b>Was hier bewusst NICHT steht:</b> ein Zug über die Fläche. Ein Sticker wird über seine
/// Kachel eingefügt und landet in der Mitte der Sicht, genau wie drüben — deshalb braucht
/// <c>ToolType.Sticker</c> auch keinen Eintrag in <c>InputInProgress</c> (§4.51). Platziert
/// und skaliert wird er danach mit dem Verschieben-Werkzeug, auf das der Einfügeweg selbst
/// umschaltet.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>Kantenlänge einer Kachel in der Sammlung.</summary>
    private const int KachelKante = 64;

    private bool _stickerGeladen;

    /// <summary>
    /// Lädt die Sammlung beim ersten Blick darauf und nicht beim Start: die Ordner liegen auf
    /// der Platte, und wer nie einen Sticker benutzt, soll dafür nicht warten.
    /// </summary>
    private void StickerSicherstellen()
    {
        if (_stickerGeladen) return;
        _stickerGeladen = true;
        StickerNeuLaden();
    }

    private void StickerNeuLaden()
    {
        StickerRaster.Children.Clear();

        // Welche Dateien zählen und in welcher Reihenfolge, steht seit §4.54 in Core
        // (StickerLibrary/Bildsammlung) — mitgelieferte zuerst, eigene danach.
        var dateien = StickerLibrary.Alle();

        if (dateien.Count == 0)
        {
            StickerLeerHinweis.IsVisible = true;
            return;
        }

        StickerLeerHinweis.IsVisible = false;
        foreach (var datei in dateien)
            StickerRaster.Children.Add(Kachel(datei));
    }

    /// <summary>
    /// Eine Kachel der Sammlung. <b>Eine unlesbare Datei nimmt die Kachel nicht mit
    /// herunter</b> — sie bekommt ein Fragezeichen und bleibt anklickbar; der Fehler kommt
    /// dann beim Einfügen zur Sprache, mit Namen, statt dass die ganze Sammlung leer wirkt.
    /// </summary>
    private Control Kachel(string datei)
    {
        Control inhalt;
        try
        {
            // DecodeToWidth statt new Bitmap(datei): eine Sammlung mit dreißig Stickern läge
            // sonst in voller Auflösung im Speicher, nur um sie 64 Pixel breit zu zeigen.
            using var strom = File.OpenRead(datei);
            inhalt = new Image
            {
                Source = Bitmap.DecodeToWidth(strom, KachelKante * 2),
                Stretch = Stretch.Uniform,
            };
        }
        catch
        {
            inhalt = new TextBlock
            {
                Text = "?",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = this.FindResource("Brush.TextMuted") as IBrush,
            };
        }

        var knopf = new Button
        {
            Width = KachelKante,
            Height = KachelKante,
            Margin = new Avalonia.Thickness(3),
            Padding = new Avalonia.Thickness(3),
            Content = inhalt,
        };
        knopf.Classes.Add("flach");
        ToolTip.SetTip(knopf, Path.GetFileNameWithoutExtension(datei));
        knopf.Click += (_, _) => StickerEinfuegen(datei);
        return knopf;
    }

    /// <summary>
    /// Legt den Sticker in die Mitte der Sicht und wählt ihn aus.
    ///
    /// <para>
    /// <b>Die Rechnung steht in Core</b> (<see cref="WbEinfuegen.FuerSticker"/>) und die
    /// Bildaufbereitung auch (<see cref="WbImagePrep.ForImport"/>, seit Phase 2) — beides
    /// wandert in die Datei, und zwei Fassungen gäben demselben Sticker je nach Kopf eine
    /// andere Größe.
    /// </para>
    /// </summary>
    private void StickerEinfuegen(string datei)
    {
        if (_page == null || _vm == null) return;
        try
        {
            if (WbImagePrep.ForImport(File.ReadAllBytes(datei)) is not { } bild) return;

            var mitte = ToCanvas(new Avalonia.Point(Skia.Bounds.Width / 2, Skia.Bounds.Height / 2));
            var kasten = WbEinfuegen.FuerSticker(bild.Width, bild.Height, mitte, _page);

            var el = new ImageElement
            {
                X = kasten.Left, Y = kasten.Top,
                Width = kasten.Width, Height = kasten.Height,
                Data = bild.Data,
            };
            _page.Elements.Add(el);
            _vm.Undo.Push(_page, new AddElementsAction([el]));
            MarkDirty();

            // Sofort verschiebbar: ohne den Werkzeugwechsel stünde der Sticker in der Mitte
            // und das Sticker-Werkzeug wäre noch aktiv — der nächste Klick auf die Fläche
            // täte dann nichts, und es sähe aus, als klemme die Bedienung. Drüben ebenso.
            _suppressToolEvents = true;
            foreach (var b in ToolButtons) b.IsChecked = b == BtnMove;
            _suppressToolEvents = false;
            SetTool(ToolType.Move);

            _selection.Clear();
            _selection.Add(el);
            ComputeSelectionBounds();

            // **Und der Fokus zurück auf die Fläche.** Ohne diese Zeile behält die
            // angeklickte Kachel ihn, und danach kommt am laufenden Programm *keine* Taste
            // mehr an: nicht Strg+Z, nicht die Werkzeug-Kürzel — bis man wieder auf die
            // Fläche klickt. Gemessen und nicht vermutet (§4.56); die Werkzeugleiste zeigt
            // den Fehler nicht, ihre Knöpfe nehmen den Fokus nicht. Dieselbe Zeile steht aus
            // demselben Grund in WhiteboardView.Input.cs beim Zeigerdruck.
            Skia.Focus();

            Neuzeichnen();
        }
        catch (Exception ex)
        {
            MessageWindow.Zeige(GonkNote.Platform.AvaloniaDialogService.Besitzer(),
                Loc.T("Msg.StickerFailed", ex.Message), DialogSeverity.Warning, frage: false);
        }
    }

    /// <summary>
    /// Eigene Bilder in die Sammlung aufnehmen. <b>Die Datei wird kopiert und nicht
    /// verknüpft</b> — ein Sticker, der verschwindet, weil der Nutzer sein Downloads-Verzeichnis
    /// aufgeräumt hat, wäre schlimmer als einer, der Platz braucht.
    /// </summary>
    private void StickerHinzufuegen_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Über IFileDialog und nicht über Avalonias StorageProvider direkt: unter Linux ist
        // das je nach Sitzung der Portal-Dialog, unter Windows derselbe wie drüben — der Kopf
        // muss das nicht wissen (Phase 2, §4.7).
        var dateien = App.Platform.Files.Open(
            Loc.T("Settings.Sticker.Add"),
            [new FileFilter(Loc.T("Filter.Images"), Bildsammlung.Endungen)],
            multiple: true);
        if (dateien.Count == 0) return;

        int genommen = 0;
        foreach (var quelle in dateien)
        {
            try
            {
                string ziel = Path.Combine(StickerLibrary.UserFolder, Path.GetFileName(quelle));

                // Namensgleiche Datei nicht überschreiben — der alte Sticker ist vielleicht
                // schon auf einer Seite in Gebrauch.
                int n = 1;
                while (File.Exists(ziel))
                {
                    string stamm = Path.GetFileNameWithoutExtension(quelle);
                    ziel = Path.Combine(StickerLibrary.UserFolder, $"{stamm}_{n++}{Path.GetExtension(quelle)}");
                }

                File.Copy(quelle, ziel);
                genommen++;
            }
            catch
            {
                // Ein einzelner Fehlversuch hält die anderen nicht auf.
            }
        }

        if (genommen > 0) StickerNeuLaden();
    }
}
