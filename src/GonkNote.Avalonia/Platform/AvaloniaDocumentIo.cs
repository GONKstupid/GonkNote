using System.IO;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Core.Rendering;
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
/// <b>Was hier noch fehlt, fehlt benannt</b> (§7): die <b>Übernahme</b> eines
/// Bestandsdokuments. Sie ist keine vergessene Zeile, sondern hängt an <c>TextRange</c>, das
/// es unter Linux nicht gibt — die Begründung steht an Ort und Stelle.
/// </para>
///
/// <para>
/// <b>Der Tafel-Export stand bis Phase 5, Schritt ①c ebenfalls auf dieser Liste</b> — und er
/// stand zu Unrecht darauf: seine Begründung („zeichnet über den Kopf“) war von Anfang an
/// falsch. Er liegt jetzt in Core (<see cref="WbExport"/>) und läuft in beiden Köpfen.
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
    /// <b>Dieselbe Liste wie im WPF-Kopf, seit Phase 5, Schritt ①c — und zwar buchstäblich
    /// dieselbe: sie steht in Core (<see cref="WbExport.Formate"/>).</b>
    ///
    /// <para>
    /// <b>Hier stand „leer, und zwar weiterhin" — mit einer Begründung, die nie stimmte:</b>
    /// „Der Tafel-Export zeichnet über <c>WhiteboardView</c>, also über den <i>Kopf</i>."
    /// <b>Tat er nicht.</b> Er rief fünf Weiterleitungen, die alle in <c>WbRenderer</c> in
    /// <b>Core</b> endeten (siehe <see cref="WbExport"/>). Der Umzug war eine Umbenennung.
    /// </para>
    /// <para>
    /// <b>Und was die leere Liste tatsächlich bewirkt hat, war nicht das, was hier stand:</b>
    /// „hält den Dateidialog zu" — <c>SaveFilePickerAsync</c> öffnet mit leerem
    /// <c>FileTypeChoices</c> sehr wohl, nur ohne Typwahl. Wer eine Tafel exportieren wollte,
    /// bekam also einen Dateidialog, wählte einen Namen — und danach die Fehlermeldung aus
    /// <c>ExportBoard</c>. <b>Zwei falsche Sätze in einem Kommentar, und der zweite hat den
    /// ersten gedeckt.</b>
    /// </para>
    /// </summary>
    public IReadOnlyList<FileFilter> BoardExportFormats => WbExport.Formate;

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
    /// <b>Ja, seit Phase 5, Schritt ①c — und es ist dieselbe Zeile wie drüben.</b> Hier stand
    /// ein <c>throw</c>, und damit war der Tafel-Export das <b>zweite</b>, nie benannte Loch
    /// in M2 (das erste ist die Rechtschreibprüfung). Der Weg liegt jetzt in Core
    /// (<see cref="WbExport"/>), rechnet mit denselben Zeichenroutinen wie der Bildschirm und
    /// braucht von diesem Kopf nichts.
    /// </summary>
    public ExportResult ExportBoard(WhiteboardDoc doc, string title, string path) =>
        WbExport.Exportieren(doc, title, path);

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
