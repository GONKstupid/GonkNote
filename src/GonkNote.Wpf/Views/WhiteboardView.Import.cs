using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using GonkNote.Core.Models;
using GonkNote.Services;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.ViewModels;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Einfuegen von Bildern, PDF- und DOCX-Seiten (Dialog, Zwischenablage, Drag&amp;Drop).
/// </summary>
public partial class WhiteboardView
{
    // ==================== Bilder einfügen ====================

    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".svg" };
    private const int MaxImportDim = 2048;

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
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Datei einfügen",
            Filter = "Bilder, PDF & Word (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg;*.pdf;*.docx)"
                   + "|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg;*.pdf;*.docx"
                   + "|PDF-Dokumente (*.pdf)|*.pdf"
                   + "|Word-Dokumente (*.docx)|*.docx"
                   + "|Bilder (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg"
                   + "|Alle Dateien (*.*)|*.*",
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
            MessageBox.Show(Loc.T("Msg.LoadFailed") + "\n" + string.Join("\n", failed),
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>Dekodiert, verkleinert große Bilder (RAM-/DB-Ziel) und liefert speicherbare Bytes + Pixelmaße.</summary>
    private static (byte[] Data, float W, float H)? PrepareRaster(byte[] raw)
    {
        using var bmp = SKBitmap.Decode(raw);
        if (bmp == null) return null;

        // Klein genug: Originalbytes unverändert übernehmen
        if (bmp.Width <= MaxImportDim && bmp.Height <= MaxImportDim && raw.Length <= 2 * 1024 * 1024)
            return (raw, bmp.Width, bmp.Height);

        float scale = Math.Min(1f, MaxImportDim / (float)Math.Max(bmp.Width, bmp.Height));
        SKBitmap use = bmp;
        if (scale < 1f)
        {
            int nw = Math.Max(1, (int)(bmp.Width * scale));
            int nh = Math.Max(1, (int)(bmp.Height * scale));
            use = bmp.Resize(new SKImageInfo(nw, nh), WbRenderer.HighSampling) ?? bmp;
        }
        try
        {
            var format = HasTransparency(use) ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
            using var img = SKImage.FromBitmap(use);
            using var data = img.Encode(format, 88);
            return (data.ToArray(), use.Width, use.Height);
        }
        finally
        {
            if (!ReferenceEquals(use, bmp)) use.Dispose();
        }
    }

    private static bool HasTransparency(SKBitmap bmp)
    {
        if (bmp.AlphaType == SKAlphaType.Opaque) return false;
        int sx = Math.Max(1, bmp.Width / 256), sy = Math.Max(1, bmp.Height / 256);
        for (int y = 0; y < bmp.Height; y += sy)
            for (int x = 0; x < bmp.Width; x += sx)
                if (bmp.GetPixel(x, y).Alpha < 250) return true;
        return false;
    }

    /// <summary>SVG wird beim Import gerastert (2x für scharfes Zoomen), Ergebnis ist PNG; Anzeigegröße bleibt die SVG-Größe.</summary>
    private static (byte[] Data, float W, float H)? RasterizeSvg(byte[] raw)
    {
        using var svg = new Svg.Skia.SKSvg();
        using var ms = new MemoryStream(raw);
        if (svg.Load(ms) == null || svg.Picture == null) return null;

        var bounds = svg.Picture.CullRect;
        if (bounds.Width < 1 || bounds.Height < 1) return null;
        float scale = Math.Min(2f, MaxImportDim / Math.Max(bounds.Width, bounds.Height));
        int w = Math.Max(1, (int)(bounds.Width * scale));
        int h = Math.Max(1, (int)(bounds.Height * scale));

        using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (surface == null) return null;
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Scale(scale);
        surface.Canvas.Translate(-bounds.Left, -bounds.Top);
        surface.Canvas.DrawPicture(svg.Picture);
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return (data.ToArray(), bounds.Width, bounds.Height);
    }

    private void PlaceImages(List<(byte[] Data, float W, float H)> images, SKPoint at)
    {
        if (_page == null || _vm == null) return;

        // Maximale Anzeigegröße: in Seite bzw. Sichtbereich einpassen
        float maxW, maxH;
        if (_page.IsInfinite)
        {
            maxW = Math.Max(64f, (float)CanvasHost.ActualWidth * 0.6f / Zoom);
            maxH = Math.Max(64f, (float)CanvasHost.ActualHeight * 0.6f / Zoom);
        }
        else
        {
            maxW = _page.Width * 0.7f;
            maxH = _page.Height * 0.7f;
        }

        var added = new List<WbElement>();
        int i = 0;
        foreach (var (data, w, h) in images)
        {
            float scale = Math.Min(1f, Math.Min(maxW / Math.Max(1f, w), maxH / Math.Max(1f, h)));
            float dw = w * scale, dh = h * scale;
            float x = at.X - dw / 2f + i * 24f;
            float y = at.Y - dh / 2f + i * 24f;
            if (!_page.IsInfinite)
            {
                x = Math.Clamp(x, 0, Math.Max(0, _page.Width - dw));
                y = Math.Clamp(y, 0, Math.Max(0, _page.Height - dh));
            }
            added.Add(new ImageElement { X = x, Y = y, Width = dw, Height = dh, Data = data });
            i++;
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
                MessageBox.Show(Loc.T("Msg.PdfNoPages"),
                    "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            HideBusy();
            var wanted = ChoosePageNumbers(Path.GetFileName(path), thumbs);
            if (wanted == null) return;   // abgebrochen

            // Und jetzt nur die gewählten Seiten in voller Auflösung – Seite für Seite.
            ShowBusy(Loc.T("Busy.Pdf"));
            var pages = await Task.Run(() =>
                PdfImporter.StreamPages(path, PdfRenderLongSide, wanted, progress).ToList());

            if (anchor.IsInfinite) InsertPdfIntoWhiteboard(pages, anchor, vm);
            else InsertPdfIntoNotebook(pages, anchor, vm);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Loc.T("Msg.PdfLoadFailed", ex.Message),
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    /// Rendert ein DOCX über denselben Paginator wie der Text-Export zu Bildseiten
    /// und fügt die gewählten Seiten wie ein PDF ein. Das Rendern (Paginator) muss
    /// auf dem UI-Thread laufen; der Import bleibt daher kurz sichtbar „busy“.
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
            // Kurz Zeit geben, damit das Busy-Overlay sichtbar wird
            await Task.Yield();

            var settings = new TextDoc();
            var flow = DocxImporter.ToFlowDocument(path, settings);
            var pages = PdfExporter.RenderFlowDocumentPages(flow, settings, Path.GetFileNameWithoutExtension(path));
            if (pages.Count == 0)
            {
                MessageBox.Show(Loc.T("Msg.DocxNoPages"),
                    "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show(Loc.T("Msg.DocxLoadFailed", ex.Message),
                "Gonk Note", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    private static (float W, float H) PdfDisplaySize(PdfImporter.PdfPageImage pg)
    {
        const float longSide = WhiteboardDoc.A4Height;
        return pg.Height >= pg.Width
            ? (longSide * pg.Width / pg.Height, longSide)
            : (longSide, longSide * pg.Height / pg.Width);
    }

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
        const float gap = 28f;
        var sizes = pages.Select(PdfDisplaySize).ToList();
        float colW = sizes.Max(s => s.W);

        // Startpunkt: sichtbarer Mittelpunkt, wenn das Dokument noch angezeigt wird
        SKPoint at = _vm == vm && _page == anchor ? ViewCenter() : new SKPoint(0, 0);
        float leftX = at.X - colW - gap / 2f;

        var added = new List<WbElement>();
        float y = at.Y;
        for (int i = 0; i < pages.Count; i += 2)
        {
            float rowH = sizes[i].H;
            if (i + 1 < pages.Count) rowH = Math.Max(rowH, sizes[i + 1].H);

            added.Add(new ImageElement
            {
                X = leftX, Y = y, Width = sizes[i].W, Height = sizes[i].H, Data = pages[i].Data,
            });
            if (i + 1 < pages.Count)
                added.Add(new ImageElement
                {
                    X = leftX + colW + gap, Y = y,
                    Width = sizes[i + 1].W, Height = sizes[i + 1].H, Data = pages[i + 1].Data,
                });

            y += rowH + gap;
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
