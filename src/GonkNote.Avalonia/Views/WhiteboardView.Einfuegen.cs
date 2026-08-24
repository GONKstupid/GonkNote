using System.IO;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Core.Text;
using GonkNote.Platform;
using GonkNote.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Bilder, PDF- und DOCX-Seiten einfügen — das Gegenstück zu
/// <c>WhiteboardView.Import.cs</c> im WPF-Kopf, neu in Phase 4.5 (§4.58).
///
/// <para>
/// <b>Was hier NICHT steht, ist die Rechnung.</b> Sie liegt seit §4.57 in Core:
/// <see cref="WbImagePrep.ForImport"/> und <see cref="WbImagePrep.ForSvg"/> bereiten die
/// Bilder auf, <see cref="WbEinfuegen.FuerBilder"/>, <see cref="WbEinfuegen.SeitenAnzeigegroesse"/>
/// und <see cref="WbEinfuegen.SeitenRaster"/> sagen, wo etwas landet. Das Ergebnis wandert in
/// die Datei — zwei Fassungen hießen, dasselbe PDF liegt je nach Kopf anders auf der Fläche.
/// </para>
/// <para>
/// <b>Ein Knopf für alle drei Formate</b>, wie drüben: der Nutzer weiß, dass er etwas
/// einfügen will, und nicht, welchen Weg das Programm dafür nimmt.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>
    /// Renderauflösung der langen Kante (≈ 200 % einer A4-Seite bei 96 dpi) — dieselbe Zahl
    /// wie im WPF-Kopf, damit eine eingefügte Seite in beiden gleich scharf ist.
    /// </summary>
    private const int PdfLangeKante = 2246;

    /// <summary>
    /// Sperrt den Import gegen sich selbst. <b>Ohne das ließe sich während des Renderns ein
    /// zweites PDF anstoßen</b>, und beide schrieben in dieselbe Seite — die Wartesperre
    /// verdeckt zwar den Zeichenbereich, aber nicht die Werkzeugleiste.
    /// </summary>
    private bool _importLaeuft;

    // ==================== Der eine Knopf ====================

    private async void DateiEinfuegen_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_page == null || _vm == null || _importLaeuft) return;

        string bilder = string.Join(";", Bildsammlung.ImportEndungen.Select(x => "*" + x));
        var dateien = App.Platform.Files.Open(
            Loc.T("Wb.InsertFile"),
            [
                new FileFilter(Loc.T("Filter.InsertAll"), [.. Bildsammlung.ImportEndungen, ".pdf", ".docx"]),
                new FileFilter(Loc.T("Filter.Pdf"), ".pdf"),
                new FileFilter(Loc.T("Filter.Word"), ".docx"),
                new FileFilter(Loc.T("Filter.ImagesImport"), Bildsammlung.ImportEndungen),
            ],
            multiple: true);
        if (dateien.Count == 0) return;

        // Nach Art trennen — ein Nutzer darf Bilder und PDFs in einem Zug wählen.
        var alsBild = dateien.Where(Bildsammlung.IstEinfuegbar).ToList();
        var alsPdf = dateien.Where(f => Endung(f) == ".pdf").ToList();
        var alsDocx = dateien.Where(f => Endung(f) == ".docx").ToList();

        if (alsBild.Count > 0) BilderEinfuegen(alsBild);
        foreach (var pdf in alsPdf) await PdfEinfuegen(pdf);
        foreach (var docx in alsDocx) await DocxEinfuegen(docx);
    }

    private static string Endung(string pfad) => Path.GetExtension(pfad).ToLowerInvariant();

    // ==================== Bilder ====================

    private void BilderEinfuegen(IReadOnlyList<string> pfade)
    {
        if (_page == null || _vm == null) return;

        var fertig = new List<(byte[] Data, float B, float H)>();
        var gescheitert = new List<string>();

        foreach (var pfad in pfade)
        {
            try
            {
                // SVG wird gerastert, alles andere nur aufbereitet. Beide Wege können null
                // liefern — dann ist die Datei kein brauchbares Bild, und der Nutzer erfährt
                // es mit Namen, statt dass still nichts passiert.
                var bild = Endung(pfad) == ".svg"
                    ? WbImagePrep.ForSvg(File.ReadAllBytes(pfad)) is { } s
                        ? (s.Data, s.Width, s.Height)
                        : ((byte[], float, float)?)null
                    : WbImagePrep.ForImport(File.ReadAllBytes(pfad)) is { } r
                        ? (r.Data, r.Width, r.Height)
                        : null;

                if (bild is { } b) fertig.Add(b);
                else gescheitert.Add(Path.GetFileName(pfad));
            }
            catch
            {
                gescheitert.Add(Path.GetFileName(pfad));
            }
        }

        if (fertig.Count > 0) BilderAblegen(fertig);

        if (gescheitert.Count > 0)
            MessageWindow.Zeige(AvaloniaDialogService.Besitzer(),
                Loc.T("Msg.LoadFailed") + "\n" + string.Join("\n", gescheitert),
                DialogSeverity.Warning, frage: false);
    }

    /// <summary>Legt vorbereitete Bilder auf die Fläche und wählt sie aus.</summary>
    private void BilderAblegen(List<(byte[] Data, float B, float H)> bilder)
    {
        if (_page == null || _vm == null) return;

        var kaesten = WbEinfuegen.FuerBilder(
            bilder.Select(b => (b.B, b.H)).ToList(), Sichtmitte(), _page,
            (float)(Skia.Bounds.Width / Zoom), (float)(Skia.Bounds.Height / Zoom));

        var neu = new List<WbElement>(bilder.Count);
        for (int i = 0; i < bilder.Count; i++)
        {
            var k = kaesten[i];
            neu.Add(new ImageElement
            {
                X = k.Left, Y = k.Top, Width = k.Width, Height = k.Height, Data = bilder[i].Data,
            });
        }

        _page.Elements.AddRange(neu);
        _vm!.Undo.Push(_page, new AddElementsAction(neu));
        MarkDirty();
        AuswahlZeigen(neu);
    }

    // ==================== PDF ====================

    private async Task PdfEinfuegen(string pfad)
    {
        if (_vm == null || _page == null || _importLaeuft) return;

        // Ziel VOR dem Warten festhalten: der Nutzer kann während des Renderns die
        // Registerkarte wechseln, und dann gehörten die Seiten trotzdem hierher.
        var vm = _vm;
        var ziel = _page;

        _importLaeuft = true;
        WarteZeigen(Loc.T("Busy.Pdf"));
        var fortschritt = new Progress<(int Fertig, int Gesamt)>(t =>
            WarteText.Text = t.Gesamt > 0 ? Loc.T("Busy.Pdf.Progress", t.Fertig, t.Gesamt) : Loc.T("Busy.Pdf"));

        try
        {
            // Erst nur Vorschaubilder — rund siebzigmal billiger als volle Seiten, und das
            // macht die Auswahl auch bei hundert Seiten erträglich.
            var vorschau = await Task.Run(() =>
                App.Platform.Pdf.StreamPages(pfad, PdfImporter.ThumbnailLongSide, null, fortschritt).ToList());

            if (vorschau.Count == 0)
            {
                MessageWindow.Zeige(AvaloniaDialogService.Besitzer(),
                    Loc.T("Msg.PdfNoPages"), DialogSeverity.Information, frage: false);
                return;
            }

            WarteVerbergen();
            var gewuenscht = await SeitenWaehlen(Path.GetFileName(pfad), vorschau);
            if (gewuenscht == null) return;   // abgebrochen

            WarteZeigen(Loc.T("Busy.Pdf"));
            var seiten = await Task.Run(() =>
                App.Platform.Pdf.StreamPages(pfad, PdfLangeKante, gewuenscht, fortschritt).ToList());

            SeitenAblegen(seiten, ziel, vm);
        }
        catch (Exception ex)
        {
            MessageWindow.Zeige(AvaloniaDialogService.Besitzer(),
                Loc.T("Msg.PdfLoadFailed", ex.Message), DialogSeverity.Warning, frage: false);
        }
        finally
        {
            _importLaeuft = false;
            WarteVerbergen();
        }
    }

    // ==================== DOCX ====================

    private async Task DocxEinfuegen(string pfad)
    {
        if (_vm == null || _page == null || _importLaeuft) return;
        var vm = _vm;
        var ziel = _page;

        _importLaeuft = true;
        WarteZeigen(Loc.T("Busy.Docx"));
        try
        {
            string titel = Path.GetFileNameWithoutExtension(pfad);
            var bilder = new TdBlobImages(BlobStore.Current!);

            // Derselbe Weg wie der Text-Export: Modell lesen, Seitenbilder rechnen — beides
            // liegt seit §4.27 in Core, also braucht der Linux-Kopf dafür keinen eigenen Code.
            var seiten = await Task.Run(() =>
                TdPdf.Seitenbilder(TdDocx.Lesen(pfad, bilder), bilder, titel));

            if (seiten.Count == 0)
            {
                MessageWindow.Zeige(AvaloniaDialogService.Besitzer(),
                    Loc.T("Msg.DocxNoPages"), DialogSeverity.Information, frage: false);
                return;
            }

            WarteVerbergen();
            var gewuenscht = await SeitenWaehlen(Path.GetFileName(pfad), seiten);
            if (gewuenscht == null) return;

            SeitenAblegen([.. gewuenscht.Select(i => seiten[i])], ziel, vm);
        }
        catch (Exception ex)
        {
            MessageWindow.Zeige(AvaloniaDialogService.Besitzer(),
                Loc.T("Msg.DocxLoadFailed", ex.Message), DialogSeverity.Warning, frage: false);
        }
        finally
        {
            _importLaeuft = false;
            WarteVerbergen();
        }
    }

    // ==================== Gemeinsam ====================

    /// <summary>
    /// Zeigt die Seitenauswahl und liefert die <b>Nummern</b>; <c>null</c> = abgebrochen.
    /// Bei einer einzigen Seite wird nicht gefragt — eine Auswahl ohne Wahl ist ein Klick,
    /// der nichts entscheidet.
    /// </summary>
    private async Task<IReadOnlyCollection<int>?> SeitenWaehlen(
        string dateiname, IReadOnlyList<PdfImporter.PdfPageImage> vorschau)
    {
        if (vorschau.Count <= 1) return new[] { 0 };

        var fenster = new SeitenwahlWindow(dateiname, vorschau);
        var besitzer = AvaloniaDialogService.Besitzer();
        if (besitzer != null) await fenster.ShowDialog(besitzer);
        else fenster.Show();

        return fenster.Gewaehlt.Count > 0 ? fenster.Gewaehlt : null;
    }

    /// <summary>
    /// Legt gerenderte Seiten ab — <b>und wohin, entscheidet die Art des Dokuments</b>: auf der
    /// unendlichen Fläche werden sie zweispaltig danebengelegt, im Notizbuch wird <b>jede Seite
    /// ein eigenes Blatt</b> hinter der aktuellen. Dieselbe Weiche wie im WPF-Kopf.
    /// <para>
    /// <b>Ohne diese Weiche wäre das Werkzeug im Notizbuch falsch</b>, nicht nur unschön: die
    /// Seiten kommen in A4-Höhe, und <see cref="WbEinfuegen.SeitenRaster"/> klemmt nichts —
    /// sie lägen zur Hälfte neben dem Blatt.
    /// </para>
    /// </summary>
    private void SeitenAblegen(List<PdfImporter.PdfPageImage> seiten, WbPage ziel, ViewModels.WhiteboardTabViewModel vm)
    {
        if (seiten.Count == 0) return;

        if (!ziel.IsInfinite) { SeitenAlsBlaetter(seiten, ziel, vm); return; }

        var masse = seiten.Select(s => WbEinfuegen.SeitenAnzeigegroesse(s.Width, s.Height)).ToList();
        // Bezugspunkt nur, wenn dieses Dokument noch angezeigt wird — sonst kennt niemand
        // die Sichtmitte, und (0,0) ist die einzige Stelle, die immer stimmt.
        var bezug = _vm == vm && _page == ziel ? Sichtmitte() : new SKPoint(0, 0);
        var kaesten = WbEinfuegen.SeitenRaster(masse, bezug);

        var neu = new List<WbElement>(seiten.Count);
        for (int i = 0; i < seiten.Count; i++)
        {
            var k = kaesten[i];
            neu.Add(new ImageElement
            {
                X = k.Left, Y = k.Top, Width = k.Width, Height = k.Height, Data = seiten[i].Data,
            });
        }

        ziel.Elements.AddRange(neu);
        vm.Undo.Push(ziel, new AddElementsAction(neu));
        vm.IsDirty = true;

        if (_vm == vm && _page == ziel) AuswahlZeigen(neu);
    }

    /// <summary>
    /// Im Notizbuch wird jede gerenderte Seite ein <b>eigenes Blatt</b> hinter der aktuellen —
    /// mit den Maßen der Vorlage, damit ein Querformat-PDF nicht in ein Hochformat gepresst
    /// wird.
    /// <para>
    /// <b>Das Bild wird der Seitenhintergrund und kein Element:</b> so lässt es sich nicht aus
    /// Versehen verschieben, und man schreibt darauf wie auf ein Arbeitsblatt — genau dafür
    /// fügt jemand ein PDF in ein Notizbuch ein. Der WPF-Kopf macht es seit jeher so.
    /// </para>
    /// <para>
    /// <b>⚠ Und wie drüben liegt hier kein Undo-Schritt</b> — der Undo-Weg kennt Elemente, aber
    /// keine hinzugefügten Seiten. Rückgängig macht danach den Schritt <em>davor</em> rückgängig,
    /// nicht das Einfügen. Das ist in beiden Köpfen gleich und in §4.58 benannt.
    /// </para>
    /// </summary>
    private void SeitenAlsBlaetter(List<PdfImporter.PdfPageImage> seiten, WbPage ziel, ViewModels.WhiteboardTabViewModel vm)
    {
        int stelle = vm.Doc.Pages.IndexOf(ziel) + 1;
        if (stelle <= 0) stelle = vm.Doc.Pages.Count;

        foreach (var s in seiten)
        {
            var (breite, hoehe) = WbEinfuegen.SeitenAnzeigegroesse(s.Width, s.Height);
            vm.Doc.Pages.Insert(stelle++, new WbPage
            {
                Width = breite,
                Height = hoehe,
                Background = PageBackground.Blank,
                Shade = PageShade.Light,
                BackgroundImage = s.Data,
                BackgroundImageId = Guid.NewGuid(),
            });
        }

        vm.IsDirty = true;

        // Nur springen, wenn dieses Dokument noch angezeigt wird — sonst verschöbe sich die
        // Ansicht einer Registerkarte, die der Nutzer gerade gar nicht ansieht.
        if (_vm == vm && _page == ziel)
        {
            GoToPage(vm.Doc.Pages.IndexOf(ziel) + 1);
            Skia.Focus();
        }
    }

    /// <summary>Die Mitte der sichtbaren Fläche in Zeichenflächen-Einheiten.</summary>
    private SKPoint Sichtmitte() =>
        ToCanvas(new Avalonia.Point(Skia.Bounds.Width / 2, Skia.Bounds.Height / 2));

    /// <summary>
    /// Wählt das Eingefügte aus und schaltet aufs Verschieben um — sonst läge es da und das
    /// nächste, was der Nutzer täte, wäre ein Strich quer darüber.
    /// <para>
    /// <b>Und der Fokus geht zurück auf die Fläche.</b> Ohne das behielte der Knopf ihn, und
    /// danach käme keine Taste mehr an — nicht Strg+Z, kein Werkzeug-Kürzel (§4.56, am
    /// laufenden Programm gemessen).
    /// </para>
    /// </summary>
    private void AuswahlZeigen(List<WbElement> neu)
    {
        _suppressToolEvents = true;
        foreach (var b in ToolButtons) b.IsChecked = b == BtnMove;
        _suppressToolEvents = false;
        SetTool(ToolType.Move);

        _selection.Clear();
        foreach (var el in neu) _selection.Add(el);
        ComputeSelectionBounds();

        Skia.Focus();
        Neuzeichnen();
    }

    // ==================== Die Wartesperre ====================

    private void WarteZeigen(string text)
    {
        WarteText.Text = text;
        WarteBalken.IsIndeterminate = true;
        WarteSperre.IsVisible = true;
    }

    private void WarteVerbergen() => WarteSperre.IsVisible = false;
}
