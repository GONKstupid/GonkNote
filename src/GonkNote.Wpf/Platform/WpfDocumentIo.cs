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

    public byte[] Import(string path, TextDoc target)
    {
        bool isMd = Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase);
        return isMd
            ? MarkdownImporter.ToXamlPackage(path, target)
            : DocxImporter.ToXamlPackage(path, target);   // liest auch die Seiteneinrichtung
    }

    public ExportResult ExportText(TextDoc doc, string title, string path)
    {
        var flow = LoadFlowDocument(doc);
        string ext = Path.GetExtension(path).ToLowerInvariant();

        List<string> written = [path];
        int issues = 0;

        switch (ext)
        {
            case ".docx": issues = DocxExporter.Export(flow, doc, title, path); break;
            case ".md": MarkdownExporter.Export(flow, path); break;
            case ".png": written = PdfExporter.ExportFlowDocumentPng(flow, doc, title, path); break;
            default: PdfExporter.ExportFlowDocument(flow, doc, title, path); break;
        }

        // Nur PDF/PNG rastern aus den Originalen; DOCX und Markdown reichen sie durch und
        // können deshalb gar nichts vermissen.
        int missing = ext is ".pdf" or ".png" or ""
            ? DocumentImages.LastExportMissingOriginals
            : 0;

        return new ExportResult(written, issues, missing);
    }

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
            var flow = LoadFlowDocument(doc);

            // Die Bilder sind nach dem Laden noch Verweise im ToolTip — erst danach hängen sie
            // wieder am Tag, und nur so findet die Übernahme ihre Blobs (DocumentImages).
            DocumentImages.Attach(flow, BlobStore.Current!);

            var modell = FlowZuTd.Umwandeln(doc, flow, BlobStore.Current!);
            doc.Model = TdFormatIo.Schreiben(modell);
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
