using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using GonkNote.Services;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Core.Text;
using GonkNote.ViewModels;
using SkiaSharp;

using GonkNote.Core.Platform;

namespace GonkNote.Views;

/// <summary>
/// Einfuegen von Bildern, PDF- und DOCX-Seiten (Dialog, Zwischenablage, Drag&amp;Drop).
/// </summary>
public partial class WhiteboardView
{
    // ==================== Bilder einfügen ====================

    /// <summary>
    /// Was sich einfügen lässt, steht seit Phase 4.5 in <see cref="Bildsammlung.ImportEndungen"/>
    /// — der Linux-Kopf braucht dieselbe Liste, sonst nimmt er andere Dateien an als Windows.
    /// </summary>
    private static readonly string[] ImageExtensions = Bildsammlung.ImportEndungen;
    /// <summary>Eine Wahrheit dafür: die Grenze steht in Core, hier steht nur der kurze Name.</summary>
    private const int MaxImportDim = WbImagePrep.MaxImportDim;

    private SKPoint ViewCenter() => ToCanvas(new Point(CanvasHost.ActualWidth / 2, CanvasHost.ActualHeight / 2));

    /// <summary>Aktuell sichtbarer Bereich in Canvas-Koordinaten (fürs Culling).</summary>
    private SKRect VisibleCanvasRect()
    {
        var tl = ToCanvas(new Point(0, 0));
        var br = ToCanvas(new Point(CanvasHost.ActualWidth, CanvasHost.ActualHeight));
        return new SKRect(
            Math.Min(tl.X, br.X), Math.Min(tl.Y, br.Y),
            Math.Max(tl.X, br.X), Math.Max(tl.Y, br.Y));
    }

    /// <summary>Ein Import-Button für alle Formate: Bilder direkt, PDF/DOCX seitenweise mit Vorschau.</summary>
    private async void InsertFile_Click(object sender, RoutedEventArgs e)
    {
        // Titel und Filter standen bis Phase 4.5 **fest auf Deutsch** im Code — in einer App
        // mit zwei Sprachtabellen (Dauerregel 1). Aufgefallen beim Portieren des Imports in
        // den Linux-Kopf, genau wie bei den Stickern (§4.56).
        string bilder = string.Join(";", ImageExtensions.Select(e => "*" + e));
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = Loc.T("Wb.InsertFile"),
            Filter = $"{Loc.T("Filter.InsertAll")}|{bilder};*.pdf;*.docx"
                   + $"|{Loc.T("Filter.Pdf")}|*.pdf"
                   + $"|{Loc.T("Filter.Word")}|*.docx"
                   + $"|{Loc.T("Filter.ImagesImport")}|{bilder}"
                   + $"|{Loc.T("Filter.AllFiles")}|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        var images = dlg.FileNames
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
        var pdfs = dlg.FileNames
            .Where(f => Path.GetExtension(f).Equals(".pdf", StringComparison.OrdinalIgnoreCase)).ToList();
        var docxs = dlg.FileNames
            .Where(f => Path.GetExtension(f).Equals(".docx", StringComparison.OrdinalIgnoreCase)).ToList();

        if (images.Count > 0) InsertImageFiles(images, ViewCenter());
        foreach (var pdf in pdfs) await InsertPdfFileAsync(pdf);
        foreach (var docx in docxs) await InsertDocxFileAsync(docx);
    }

    private void InsertImageFiles(IEnumerable<string> paths, SKPoint at)
    {
        var imported = new List<(byte[] Data, float W, float H)>();
        var failed = new List<string>();
        foreach (var path in paths)
        {
            try
            {
                var img = Path.GetExtension(path).Equals(".svg", StringComparison.OrdinalIgnoreCase)
                    ? RasterizeSvg(File.ReadAllBytes(path))
                    : PrepareRaster(File.ReadAllBytes(path));
                if (img != null) imported.Add(img.Value);
                else failed.Add(Path.GetFileName(path));
            }
            catch
            {
                failed.Add(Path.GetFileName(path));
            }
        }

        if (imported.Count > 0) PlaceImages(imported, at);
        if (failed.Count > 0)
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.LoadFailed") + "\n" + string.Join("\n", failed),
                DialogSeverity.Warning, frage: false);
    }

    /// <summary>
    /// Dekodiert, verkleinert große Bilder (RAM-/DB-Ziel) und liefert speicherbare Bytes
    /// samt Pixelmaßen.
    /// <para>
    /// Der Inhalt liegt seit Phase 2 in <see cref="WbImagePrep.ForImport"/> — er ist reines
    /// SkiaSharp und hatte nur deshalb keinen Test, weil er hier privat im Kopf stand
    /// (HANDOFF §4.4). Hier bleibt die Umformung auf die Gleitkomma-Maße, mit denen die
    /// Platzierung rechnet.
    /// </para>
    /// </summary>
    private static (byte[] Data, float W, float H)? PrepareRaster(byte[] raw) =>
        WbImagePrep.ForImport(raw) is { } p ? (p.Data, p.Width, p.Height) : null;

    /// <summary>
    /// SVG wird beim Import gerastert; die Rechnung steht seit Phase 4.5 in
    /// <see cref="WbImagePrep.ForSvg"/> — sie ist reines SkiaSharp, und <c>Svg.Skia</c> war
    /// schon immer ein Core-Paket. Hier bleibt nur die Umformung auf die Maße, mit denen die
    /// Platzierung rechnet.
    /// </summary>
    private static (byte[] Data, float W, float H)? RasterizeSvg(byte[] raw) =>
        WbImagePrep.ForSvg(raw) is { } p ? (p.Data, p.Width, p.Height) : null;

    private void PlaceImages(List<(byte[] Data, float W, float H)> images, SKPoint at)
    {
        if (_page == null || _vm == null) return;

        // Wohin und wie groß, rechnet seit Phase 4.5 Core (WbEinfuegen.FuerBilder) — das
        // Ergebnis wandert in die Datei, und zwei Fassungen gäben demselben Bild je nach Kopf
        // eine andere Größe. Der Sichtbereich geht in Zeichenflächen-Einheiten hinein, also
        // durch den Zoom geteilt; die Fläche selbst kennt Core nicht.
        var kaesten = WbEinfuegen.FuerBilder(
            images.Select(im => (im.W, im.H)).ToList(), at, _page,
            (float)CanvasHost.ActualWidth / Zoom, (float)CanvasHost.ActualHeight / Zoom);

        var added = new List<WbElement>();
        for (int i = 0; i < images.Count; i++)
        {
            var k = kaesten[i];
            added.Add(new ImageElement
            {
                X = k.Left, Y = k.Top, Width = k.Width, Height = k.Height, Data = images[i].Data,
            });
        }

        _page.Elements.AddRange(added);
        _vm.Undo.Push(_page, new AddElementsAction(added));
        MarkDirty();

        // Direkt auswählen, damit Verschieben/Skalieren sofort möglich ist
        BtnLasso.IsChecked = true;
        _selection.Clear();
        foreach (var el in added) _selection.Add(el);
        ComputeSelectionBounds();
        Skia.InvalidateVisual();
    }

    private bool PasteImageFromClipboard()
    {
        if (Clipboard.ContainsImage() && Clipboard.GetImage() is { } src)
        {
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(src));
            using var ms = new MemoryStream();
            enc.Save(ms);
            if (PrepareRaster(ms.ToArray()) is { } img)
            {
                PlaceImages(new List<(byte[], float, float)> { img }, ViewCenter());
                return true;
            }
            return false;
        }

        if (Clipboard.ContainsFileDropList())
        {
            var files = Clipboard.GetFileDropList().Cast<string>()
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();
            if (files.Count > 0)
            {
                InsertImageFiles(files, ViewCenter());
                return true;
            }
        }
        return false;
    }

    private void CanvasHost_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetDroppedImageFiles(e).Count > 0
            || GetDroppedByExtension(e, ".pdf").Count > 0
            || GetDroppedByExtension(e, ".docx").Count > 0
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void CanvasHost_Drop(object sender, DragEventArgs e)
    {
        var files = GetDroppedImageFiles(e);
        if (files.Count > 0)
            InsertImageFiles(files, ToCanvas(e.GetPosition(CanvasHost)));

        var pdfs = GetDroppedByExtension(e, ".pdf");
        var docxs = GetDroppedByExtension(e, ".docx");
        e.Handled = true;
        foreach (var pdf in pdfs)
            await InsertPdfFileAsync(pdf);
        foreach (var docx in docxs)
            await InsertDocxFileAsync(docx);
    }

    private static List<string> GetDroppedImageFiles(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return new List<string>();
        return files.Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
    }

    private static List<string> GetDroppedByExtension(DragEventArgs e, string ext)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return new List<string>();
        return files.Where(f => Path.GetExtension(f).Equals(ext, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // ==================== PDF einfügen ====================

    /// <summary>Renderauflösung der langen Kante (≈ 200 % einer A4-Seite bei 96 DPI).</summary>
    private const int PdfRenderLongSide = 2246;

    private bool _importing;

    /// <summary>
    /// Rendert ein PDF im Hintergrund (UI bleibt bedienbar, Fortschritt sichtbar)
    /// und fügt es je nach Dokumenttyp ein. Ziel-Seite/-Dokument werden vor dem
    /// Await festgehalten, damit ein Tabwechsel während des Imports nichts verfälscht.
    /// </summary>
    private async Task InsertPdfFileAsync(string path)
    {
        if (_vm == null || _page == null || _importing) return;
        var vm = _vm;
        var anchor = _page;

        _importing = true;
        ShowBusy(Loc.T("Busy.Pdf"));
        var progress = new Progress<(int Done, int Total)>(t =>
            BusyText.Text = t.Total > 0 ? Loc.T("Busy.Pdf.Progress", t.Done, t.Total) : Loc.T("Busy.Pdf"));

        try
        {
            // Erst nur Vorschaubilder: die sind rund 70× billiger als eine volle Seite und
            // machen den Auswahldialog auch bei hunderten Seiten erträglich.
            var thumbs = await Task.Run(() =>
                PdfImporter.RenderPages(path, PdfImporter.ThumbnailLongSide, progress));
            if (thumbs.Count == 0)
            {
                MessageWindow.Zeige(
                    Window.GetWindow(this),
                    Loc.T("Msg.PdfNoPages"),
                    DialogSeverity.Information, frage: false);
                return;
            }

            HideBusy();
            var wanted = ChoosePageNumbers(Path.GetFileName(path), thumbs);
            if (wanted == null) return;   // abgebrochen

            // Und jetzt nur die gewählten Seiten in voller Auflösung – Seite für Seite.
            ShowBusy(Loc.T("Busy.Pdf"));
            var pages = await Task.Run(() =>
                App.Platform.Pdf.StreamPages(path, PdfRenderLongSide, wanted, progress).ToList());

            if (anchor.IsInfinite) InsertPdfIntoWhiteboard(pages, anchor, vm);
            else InsertPdfIntoNotebook(pages, anchor, vm);
        }
        catch (Exception ex)
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.PdfLoadFailed", ex.Message),
                DialogSeverity.Warning, frage: false);
        }
        finally
        {
            _importing = false;
            HideBusy();
        }
    }

    /// <summary>
    /// Zeigt die Seitenauswahl (ab 2 Seiten) und liefert die **Nummern** der gewählten
    /// Seiten; null = abgebrochen. Nur die Nummern, weil die Seiten danach erst in voller
    /// Auflösung gerendert werden – der Dialog arbeitet mit Vorschaubildern.
    /// </summary>
    private IReadOnlyCollection<int>? ChoosePageNumbers(string fileName, List<PdfImporter.PdfPageImage> thumbs)
    {
        if (thumbs.Count <= 1) return new[] { 0 };

        var dlg = new FileInsertDialog(fileName, thumbs) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true || dlg.SelectedPages.Count == 0) return null;
        return dlg.SelectedPages.OrderBy(i => i).ToList();
    }

    /// <summary>Wie <see cref="ChoosePageNumbers"/>, aber für bereits gerenderte Seiten (DOCX).</summary>
    private List<PdfImporter.PdfPageImage>? ChoosePages(string fileName, List<PdfImporter.PdfPageImage> pages)
    {
        if (pages.Count <= 1) return pages;

        var dlg = new FileInsertDialog(fileName, pages) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true || dlg.SelectedPages.Count == 0) return null;
        return dlg.SelectedPages.Select(i => pages[i]).ToList();
    }

    // ==================== DOCX einfügen ====================

    /// <summary>
    /// Rendert ein DOCX auf demselben Weg wie der Text-Export zu Bildseiten und fügt die
    /// gewählten Seiten wie ein PDF ein.
    /// <para>
    /// <b>Seit §4.27 läuft das über das Modell</b> (<see cref="TdDocx"/> → <see cref="TdPdf"/>)
    /// und nicht mehr über <c>FlowDocument</c> und den WPF-Paginator. Damit fällt der Zwang
    /// weg, auf dem UI-Thread zu rendern: das Setzen und Malen geht in einen Hintergrund-Thread,
    /// und das Busy-Overlay läuft währenddessen wirklich weiter, statt eingefroren dazustehen.
    /// </para>
    /// </summary>
    private async Task InsertDocxFileAsync(string path)
    {
        if (_vm == null || _page == null || _importing) return;
        var vm = _vm;
        var anchor = _page;

        _importing = true;
        ShowBusy(Loc.T("Busy.Docx"));
        try
        {
            string titel = Path.GetFileNameWithoutExtension(path);
            var bilder = new TdBlobImages(BlobStore.Current!);

            var pages = await Task.Run(() =>
                TdPdf.Seitenbilder(TdDocx.Lesen(path, bilder), bilder, titel));

            if (pages.Count == 0)
            {
                MessageWindow.Zeige(
                    Window.GetWindow(this),
                    Loc.T("Msg.DocxNoPages"),
                    DialogSeverity.Information, frage: false);
                return;
            }

            HideBusy();
            var chosen = ChoosePages(Path.GetFileName(path), pages);
            if (chosen == null) return;

            if (anchor.IsInfinite) InsertPdfIntoWhiteboard(chosen, anchor, vm);
            else InsertPdfIntoNotebook(chosen, anchor, vm);
        }
        catch (Exception ex)
        {
            MessageWindow.Zeige(
                Window.GetWindow(this),
                Loc.T("Msg.DocxLoadFailed", ex.Message),
                DialogSeverity.Warning, frage: false);
        }
        finally
        {
            _importing = false;
            HideBusy();
        }
    }

    private void ShowBusy(string text)
    {
        BusyText.Text = text;
        BusyBar.IsIndeterminate = true;
        BusyOverlay.Visibility = Visibility.Visible;
    }

    private void HideBusy() => BusyOverlay.Visibility = Visibility.Collapsed;

    /// <summary>Anzeigemaße einer PDF-Seite: lange Kante = A4-Höhe, Seitenverhältnis bleibt.</summary>
    private static (float W, float H) PdfDisplaySize(PdfImporter.PdfPageImage pg) =>
        WbEinfuegen.SeitenAnzeigegroesse(pg.Width, pg.Height);

    /// <summary>Notizbuch: jede PDF-Seite wird eine neue Seite hinter der Ankerseite.</summary>
    private void InsertPdfIntoNotebook(List<PdfImporter.PdfPageImage> pages, WbPage anchor, WhiteboardTabViewModel vm)
    {
        int insertAt = vm.Doc.Pages.IndexOf(anchor) + 1;
        if (insertAt <= 0) insertAt = vm.Doc.Pages.Count;

        foreach (var pg in pages)
        {
            var (pw, ph) = PdfDisplaySize(pg);
            vm.Doc.Pages.Insert(insertAt++, new WbPage
            {
                Width = pw,
                Height = ph,
                Background = PageBackground.Blank,
                Shade = PageShade.Light,
                BackgroundImage = pg.Data,
                BackgroundImageId = Guid.NewGuid(),
            });
        }

        vm.IsDirty = true;
        // Nur wenn dieses Dokument noch angezeigt wird, zur ersten neuen Seite springen
        if (_vm == vm && _page == anchor)
        {
            GoToPage(vm.Doc.Pages.IndexOf(anchor) + 1);
            UpdatePageLabel();
        }
    }

    /// <summary>Whiteboard: PDF-Seiten zweispaltig (s1 s2 / s3 s4 …) als Bild-Elemente.</summary>
    private void InsertPdfIntoWhiteboard(List<PdfImporter.PdfPageImage> pages, WbPage anchor, WhiteboardTabViewModel vm)
    {
        // Startpunkt: sichtbarer Mittelpunkt, wenn das Dokument noch angezeigt wird
        SKPoint at = _vm == vm && _page == anchor ? ViewCenter() : new SKPoint(0, 0);

        // Das zweispaltige Raster rechnet seit Phase 4.5 Core (WbEinfuegen.SeitenRaster).
        var masse = pages.Select(PdfDisplaySize).ToList();
        var kaesten = WbEinfuegen.SeitenRaster(masse, at);

        var added = new List<WbElement>();
        for (int i = 0; i < pages.Count; i++)
        {
            var k = kaesten[i];
            added.Add(new ImageElement
            {
                X = k.Left, Y = k.Top, Width = k.Width, Height = k.Height, Data = pages[i].Data,
            });
        }

        anchor.Elements.AddRange(added);
        vm.Undo.Push(anchor, new AddElementsAction(added));
        vm.IsDirty = true;

        if (_vm == vm && _page == anchor)
        {
            BtnLasso.IsChecked = true;
            _selection.Clear();
            foreach (var el in added) _selection.Add(el);
            ComputeSelectionBounds();
            Skia.InvalidateVisual();
        }
    }
}
