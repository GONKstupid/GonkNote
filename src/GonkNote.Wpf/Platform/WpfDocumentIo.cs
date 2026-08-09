using System.IO;
using System.Windows;
using System.Windows.Documents;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Core.Services;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Platform;

/// <summary>
/// Import und Export über den WPF-Weg: alles läuft über ein <see cref="FlowDocument"/>.
/// <para>
/// Diese Klasse ist die Naht aus HANDOFF §4.1. Dahinter liegen ~2.300 Zeilen, die auf
/// <c>System.Windows.Documents</c> stehen und deshalb erst nach Phase 4 nach Core können.
/// Davor steht ab jetzt <see cref="IDocumentIo"/> — die ViewModels sehen nur noch
/// „Datei rein, Datei raus".
/// </para>
/// </summary>
public sealed class WpfDocumentIo : IDocumentIo
{
    // Bei jedem Zugriff neu gebaut, nicht einmal beim Start: die Beschriftungen hängen an
    // Loc.Current, und ein Sprachwechsel würde sonst still an ihnen vorbeigehen
    // (HANDOFF §7, „Texte, die der Code setzt").

    public IReadOnlyList<FileFilter> ImportFormats =>
    [
        new(Loc.T("Filter.Documents"), ".docx", ".md"),
        new(Loc.T("Filter.Word"), ".docx"),
        new(Loc.T("Filter.Markdown"), ".md"),
        new(Loc.T("Filter.AllFiles"), ".*"),
    ];

    public IReadOnlyList<FileFilter> TextExportFormats =>
    [
        new(Loc.T("Filter.Pdf"), ".pdf"),
        new(Loc.T("Filter.Word"), ".docx"),
        new(Loc.T("Filter.Markdown"), ".md"),
        new(Loc.T("Filter.Png"), ".png"),
    ];

    public IReadOnlyList<FileFilter> BoardExportFormats =>
    [
        new(Loc.T("Filter.Pdf"), ".pdf"),
        new(Loc.T("Filter.Png"), ".png"),
    ];

    /// <summary>
    /// <inheritdoc cref="IDocumentIo.Import"/>
    /// <para>
    /// <b>DOCX läuft seit dem Umverdrahten über <see cref="TdDocx"/></b> (§4.23): gelesen wird
    /// ins eigene Modell, und <see cref="TdZuFlow"/> macht daraus das <c>XamlPackage</c>, das
    /// der Editor anzeigt. <b>Beide Felder werden dabei gefüllt</b> — <c>Model</c> aus der
    /// Datei, <c>Rtf</c> aus dem Modell —, denn <c>Rtf</c> führt weiter (§4.22).
    /// </para>
    /// </summary>
    public byte[] Import(string path, TextDoc target)
    {
        if (Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase))
            return MarkdownImporter.ToXamlPackage(path, target);

        var modell = TdDocx.Lesen(path, new TdBlobImages(BlobStore.Current!));
        var flow = TdZuFlow.Umwandeln(modell, BlobStore.Current!, target);   // samt Seiteneinrichtung

        target.Model = TdFormatIo.Schreiben(modell);
        target.MigrationIssue = "";
        return AlsXamlPackage(flow, target);
    }

    public ExportResult ExportText(TextDoc doc, string title, string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();

        List<string> written = [path];
        int issues = 0;

        // **Die zwei Wege, die schon umverdrahtet sind, laufen gegen das Modell** (§4.23) —
        // die zwei, die noch am WPF-Paginator hängen (PDF/PNG), gegen das FlowDocument.
        switch (ext)
        {
            case ".docx":
                TdDocx.Schreiben(Modell(doc), path, new TdBlobImages(BlobStore.Current!), title);
                issues = TdDocx.Pruefen(path);
                break;

            case ".md":
                TdMarkdown.Export(Modell(doc), path, Feldwerte(title));
                break;

            case ".png":
                written = PdfExporter.ExportFlowDocumentPng(LoadFlowDocument(doc), doc, title, path);
                break;

            default:
                PdfExporter.ExportFlowDocument(LoadFlowDocument(doc), doc, title, path);
                break;
        }

        // Nur PDF/PNG rastern aus den Originalen; DOCX und Markdown reichen sie durch und
        // können deshalb gar nichts vermissen.
        int missing = ext is ".pdf" or ".png" or ""
            ? DocumentImages.LastExportMissingOriginals
            : 0;

        return new ExportResult(written, issues, missing);
    }

    /// <summary>
    /// Das Modell, aus dem exportiert wird.
    ///
    /// <para>
    /// <b>Es kommt aus <see cref="TextDoc.Model"/> und wird nur dann neu gebaut, wenn dort
    /// nichts steht.</b> Beides zählt: Das Feld wird bei **jedem** Speichern mitgeschrieben
    /// (§4.23), enthält also das, was auf dem Schirm steht — und ein Dokument, das nie
    /// übernommen wurde (importiert unter Linux, angelegt vor der Übernahme), hätte sonst
    /// nichts zu exportieren.
    /// </para>
    /// </summary>
    private static TdDocument Modell(TextDoc doc)
    {
        if (TdFormatIo.Lesen(doc.Model) is { } gespeichert) return gespeichert;

        var modell = AusAltformat(doc);
        doc.Model = TdFormatIo.Schreiben(modell);
        return modell;
    }

    /// <summary>
    /// Das Modell zum Altformat eines Dokuments — der eine Weg dorthin.
    /// <para>
    /// <b><see cref="DocumentImages.Attach"/> gehört dazu und ist nicht nachträglich
    /// angeflanscht:</b> Nach dem Laden stehen die Bilder als Verweis im <c>ToolTip</c>, erst
    /// danach hängen sie am <c>Tag</c>. Ohne diesen Schritt findet die Übernahme ihre Blobs
    /// nicht und legt jedes Bild ein zweites Mal ab — dieselben Bytes, eine neue Kennung.
    /// </para>
    /// </summary>
    private static TdDocument AusAltformat(TextDoc doc)
    {
        var flow = LoadFlowDocument(doc);
        DocumentImages.Attach(flow, BlobStore.Current!);
        return FlowZuTd.Umwandeln(doc, flow, BlobStore.Current!);
    }

    /// <summary>Ein FlowDocument als <c>XamlPackage</c> — die Form, in der der Editor liest.</summary>
    private static byte[] AlsXamlPackage(FlowDocument flow, TextDoc target)
    {
        var range = new TextRange(flow.ContentStart, flow.ContentEnd);
        using var ms = new MemoryStream();

        // Bilder bleiben draußen: im Paket steht nur ein Verweis auf das Original im
        // Blob-Speicher (DocumentImages). Das using stellt sie danach wieder her.
        using (DocumentImages.Detach(flow, BlobStore.Current!))
            range.Save(ms, DataFormats.XamlPackage, true);

        target.Images = DocumentImages.UsedBlobs(flow).ToList();
        return ms.ToArray();
    }

    /// <summary>
    /// Datum und Titel für die Felder. <b>Die Uhr wird hier gefragt und nicht in Core</b>
    /// (§4.20) — genau dafür gibt es <see cref="TdFieldContext"/>.
    /// </summary>
    private static TdFieldContext Feldwerte(string title) =>
        new() { Date = DateTime.Now, Title = title };

    public ExportResult ExportBoard(WhiteboardDoc doc, string title, string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();

        List<string> written = ext == ".png"
            ? PdfExporter.ExportWhiteboardPng(doc, title, path)
            : Run(() => PdfExporter.ExportWhiteboard(doc, title, path), path);

        return new ExportResult(written, 0, DocumentHealth.MissingImages(doc));

        static List<string> Run(Action export, string path) { export(); return [path]; }
    }

    /// <summary>
    /// <b>Ja</b> — und nur hier. RTF und XamlPackage liest ausschließlich
    /// <c>TextRange.Load</c>, und das gibt es nur unter Windows (§4.22).
    /// </summary>
    public bool CanMigrate => true;

    public MigrationResult Migrate(TextDoc doc)
    {
        if (doc.Rtf.Length == 0) return new MigrationResult(false);

        try
        {
            doc.Model = TdFormatIo.Schreiben(AusAltformat(doc));
            doc.MigrationIssue = "";
            return new MigrationResult(true);
        }
        catch (Exception ex)
        {
            // **Das Altfeld bleibt unangetastet.** Eine misslungene Übernahme ist damit kein
            // Datenverlust, sondern ein Versuch, der wiederholt werden kann — die Regel aus
            // §4.8, hier ein zweites Mal.
            doc.MigrationIssue = ex.Message;
            return new MigrationResult(false, ex.Message);
        }
    }

    /// <summary>Baut aus den gespeicherten Bytes eines Textdokuments ein FlowDocument.</summary>
    private static FlowDocument LoadFlowDocument(TextDoc doc)
    {
        var flow = new FlowDocument();
        var bytes = doc.Rtf;
        if (bytes.Length > 2)
        {
            var range = new TextRange(flow.ContentStart, flow.ContentEnd);
            using var ms = new MemoryStream(bytes);
            bool isPackage = bytes[0] == 0x50 && bytes[1] == 0x4B; // "PK" = XamlPackage-ZIP
            range.Load(ms, isPackage ? DataFormats.XamlPackage : DataFormats.Rtf);
        }
        // Export ist immer „Papier": Dark-Mode-Schreibfarbe auf dunkle Tinte normalisieren
        TextStyles.NormalizeInk(flow, TextStyles.InkLight);
        return flow;
    }
}
