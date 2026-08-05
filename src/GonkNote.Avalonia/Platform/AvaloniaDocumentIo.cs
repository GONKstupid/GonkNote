using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Services;

namespace GonkNote.Platform;

/// <summary>
/// Import und Export — <b>im Linux-Kopf noch nicht vorhanden</b>, und zwar aus einem
/// Grund, der in der Roadmap steht und nicht in dieser Datei.
/// <para>
/// <c>DocxImporter</c>, <c>DocxExporter</c>, <c>MarkdownImporter</c>,
/// <c>MarkdownExporter</c> und <c>PdfExporter</c> stehen alle auf <c>FlowDocument</c>
/// (HANDOFF §4.1). Sie ziehen nach Core um, <b>wenn</b> die eigene Dokument-Engine steht —
/// das ist Phase 4, und der Umzug <i>ist</i> dieser Umbau, nicht die Vorbereitung darauf.
/// Etwas anderes hier hinzuschreiben hieße, dieselben 2.300 Zeilen ein zweites Mal zu
/// bauen, um sie dann beide wegzuwerfen.
/// </para>
/// <para>
/// <b>Warum die Klasse trotzdem existiert:</b> <see cref="IPlatformServices"/> verlangt sie,
/// und das ist der Sinn des Bündels — der Compiler zeigt, was ein Kopf noch nicht kann,
/// statt es stillschweigend fehlen zu lassen. Die drei Formatlisten sind leer, damit die
/// Oberfläche gar nicht erst einen Dateidialog öffnet; die drei Methoden sagen, was los ist.
/// </para>
/// </summary>
public sealed class AvaloniaDocumentIo : IDocumentIo
{
    // Leere Listen und keine erfundenen Einträge: ein Dateidialog ohne einen einzigen
    // Formatfilter wäre eine Sackgasse mit Aussicht.
    public IReadOnlyList<FileFilter> ImportFormats => [];
    public IReadOnlyList<FileFilter> TextExportFormats => [];
    public IReadOnlyList<FileFilter> BoardExportFormats => [];

    public byte[] Import(string path, TextDoc target) => throw Fehlt();

    public ExportResult ExportText(TextDoc doc, string title, string path) => throw Fehlt();

    public ExportResult ExportBoard(WhiteboardDoc doc, string title, string path) => throw Fehlt();

    /// <summary>
    /// <b>Nein — und das ist keine Lücke, sondern eine Schranke.</b> RTF und XamlPackage liest
    /// ausschließlich <c>System.Windows.Documents.TextRange</c>. Ein Versuch hier ergäbe ein
    /// **leeres** Dokument, und das wäre schlimmer als gar keine Übernahme: Der Inhalt sähe
    /// gelöscht aus, obwohl er unversehrt in <see cref="TextDoc.Rtf"/> steht (§4.22).
    /// </summary>
    public bool CanMigrate => false;

    /// <summary>
    /// Wirft **nicht**, sondern sagt „nichts getan". Die Übernahme läuft beim Öffnen eines
    /// Dokuments; eine Ausnahme an dieser Stelle wäre für den Nutzer dasselbe wie ein Absturz,
    /// und es ist ja nichts kaputt — dieses Dokument wartet nur auf den Windows-Rechner.
    /// </summary>
    public MigrationResult Migrate(TextDoc doc) =>
        new(false, Loc.T("Io.NotOnThisPlatform"));

    /// <summary>
    /// Der Aufrufer im <c>MainViewModel</c> fängt diese Ausnahme und zeigt ihre Meldung
    /// (<c>Msg.ExportFailed</c> bzw. <c>Msg.ImportFailed</c>) — es kommt also ein Satz an
    /// den Nutzer und nicht ein Absturz.
    /// </summary>
    private static NotSupportedException Fehlt() =>
        new(Loc.T("Io.NotOnThisPlatform"));
}
