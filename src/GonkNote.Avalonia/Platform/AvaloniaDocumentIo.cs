using System.IO;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Core.Services;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Platform;

/// <summary>
/// Import und Export im Linux-Kopf.
///
/// <para>
/// <b>Bis §4.27 war diese Klasse leer, und das war richtig:</b> DOCX-, Markdown- und
/// PDF-Weg standen auf <c>FlowDocument</c> (§4.1) und konnten erst nach der eigenen
/// Dokument-Engine umziehen. <b>Seit §4.27 stehen alle vier vollständig in Core</b> und
/// laufen nachweislich unter Linux — die Schranke war ab da keine technische mehr, sondern
/// eine unverdrahtete. Diese Datei ist die Verdrahtung (§4.28).
/// </para>
///
/// <para>
/// <b>Was hier trotzdem fehlt, fehlt benannt</b> (§7): der <b>Tafel</b>-Export
/// (Whiteboard/Notizbuch) und die <b>Übernahme</b> eines Bestandsdokuments. Beides ist keine
/// vergessene Zeile, sondern hängt an Code, den es unter Linux nicht gibt — die Begründung
/// steht jeweils an Ort und Stelle.
/// </para>
/// </summary>
public sealed class AvaloniaDocumentIo : IDocumentIo
{
    /// <summary>
    /// <b>Nur DOCX</b>, und das ist der Unterschied zum WPF-Kopf: <c>TdDocx.Lesen</c> steht in
    /// Core, der Markdown-<i>Import</i> geht drüben weiter über ein <c>FlowDocument</c>
    /// (<c>MarkdownImporter</c>, §4.27). Ein <c>.md</c>-Eintrag hier führte in einen
    /// Dateidialog, hinter dem eine Ausnahme wartet — ein Format anzubieten, das man nicht
    /// lesen kann, ist schlimmer, als es nicht anzubieten.
    /// </summary>
    public IReadOnlyList<FileFilter> ImportFormats =>
    [
        new(Loc.T("Filter.Word"), ".docx"),
    ];

    /// <summary>Dieselbe Liste wie im WPF-Kopf — sie steht in Core (§4.28).</summary>
    public IReadOnlyList<FileFilter> TextExportFormats => TdExport.Formate;

    /// <summary>
    /// <b>Leer, und zwar weiterhin.</b> Der Tafel-Export zeichnet über <c>WhiteboardView</c>,
    /// also über den <i>Kopf</i>, und liegt darum bis heute im WPF-Projekt (<c>PdfExporter</c>,
    /// §4.27). Ihn nach Core zu ziehen ist Arbeit am Linux-Whiteboard und gehört zu Phase 4.5.
    /// Eine leere Liste hält den Dateidialog zu, statt ihn in <see cref="ExportBoard"/> laufen
    /// zu lassen.
    /// </summary>
    public IReadOnlyList<FileFilter> BoardExportFormats => [];

    /// <summary>
    /// Liest eine DOCX-Datei ins <b>eigene Modell</b> und gibt <b>keine</b> Altformat-Bytes
    /// zurück.
    ///
    /// <para>
    /// <b>Das ist die eine Stelle, an der sich die Köpfe wirklich unterscheiden.</b> Drüben
    /// entsteht aus dem Modell zusätzlich ein <c>XamlPackage</c> für <see cref="TextDoc.Rtf"/>
    /// (<c>TdZuFlow</c>); das geht nur unter Windows. Hier bleibt <c>Rtf</c> deshalb leer und
    /// <see cref="TextDoc.Model"/> ist das einzige gefüllte Feld — genau der Fall, den
    /// <c>TextDoc.Model</c> seit jeher beschreibt: „wer voll ist, führt".
    /// </para>
    /// <para>
    /// <b>Damit das kein Datenverlust auf dem Windows-Rechner wird</b>, liest der WPF-Editor
    /// seit §4.28 aus <c>Model</c>, wenn <c>Rtf</c> leer ist. Ohne diesen Rückfall sähe ein
    /// unter Linux importiertes Dokument drüben aus wie ein leeres Blatt — und das ist der
    /// teuerste Fehler dieser Art, weil er nach einem gelöschten Inhalt aussieht.
    /// </para>
    /// </summary>
    public byte[] Import(string path, TextDoc target)
    {
        var bilder = new TdBlobImages(BlobStore.Current!);
        var modell = TdDocx.Lesen(path, bilder);

        target.Model = TdFormatIo.Schreiben(modell);
        target.MigrationIssue = "";
        target.Images = [.. Bildkennungen(modell)];

        return [];
    }

    /// <inheritdoc cref="IDocumentIo.ExportText"/>
    /// <summary>
    /// <b>Derselbe Weg wie drüben</b> (<see cref="TdExport.Schreiben"/>), nur ohne den
    /// Rückfall aufs Altformat: Was hier kein Modell hat, hatte nie eines — RTF und
    /// XamlPackage liest ausschließlich WPF (§4.22). Ein leeres Dokument zu exportieren wäre
    /// schlimmer als eine Meldung: die Datei sähe aus, als wäre der Inhalt weg.
    /// </summary>
    public ExportResult ExportText(TextDoc doc, string title, string path)
    {
        if (TdFormatIo.Lesen(doc.Model) is not { } modell)
            throw new NotSupportedException(Loc.T("Io.NotMigrated"));

        return TdExport.Schreiben(
            modell, path, new TdBlobImages(BlobStore.Current!), title,
            // Die Uhr wird hier gefragt und nicht in Core (§4.20).
            new TdFieldContext { Date = DateTime.Now, Title = title });
    }

    /// <summary>
    /// <b>Nein — siehe <see cref="BoardExportFormats"/>.</b> Erreichbar ist die Methode
    /// ohnehin nicht, solange die Liste leer ist; sie sagt trotzdem, was los ist, statt einer
    /// leeren Datei.
    /// </summary>
    public ExportResult ExportBoard(WhiteboardDoc doc, string title, string path) =>
        throw new NotSupportedException(Loc.T("Io.NotOnThisPlatform"));

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
    /// Die Blobs, auf die das Dokument zeigt — sie landen in <see cref="TextDoc.Images"/> und
    /// bewahren sie vor dem Aufräumlauf. <b>Das Wasserzeichen gehört dazu</b>: es ist ein Bild
    /// wie jedes andere, und ohne diesen Eintrag wäre es nach dem nächsten Aufräumen weg.
    /// </summary>
    private static IEnumerable<Guid> Bildkennungen(TdDocument doc)
    {
        foreach (var abschnitt in doc.Sections)
            if (abschnitt.Page.Watermark is { } zeichen)
                yield return zeichen.BlobId;

        foreach (var absatz in doc.Paragraphs())
            foreach (var bild in absatz.Inlines.OfType<TdImage>())
                yield return bild.BlobId;
    }
}
