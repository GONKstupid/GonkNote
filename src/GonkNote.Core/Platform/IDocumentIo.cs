using GonkNote.Core.Models;

namespace GonkNote.Core.Platform;

/// <summary>
/// Was ein Export hinterlassen hat. Alle drei Angaben landen in derselben Meldung an den
/// Nutzer — lieber einmal zu viel sagen als stillschweigend schlechter exportieren.
/// </summary>
/// <param name="Written">Die geschriebenen Dateien; der PNG-Export erzeugt eine je Seite.</param>
/// <param name="ValidationIssues">Beanstandungen des DOCX-Exports; 0 = keine.</param>
/// <param name="MissingImages">Bilder ohne auffindbare Vorlage — sie werden im Ziel grob.</param>
public sealed record ExportResult(
    IReadOnlyList<string> Written,
    int ValidationIssues = 0,
    int MissingImages = 0);

/// <summary>
/// Was die Übernahme eines Bestandsdokuments ins eigene Modell ergeben hat.
///
/// <para>
/// <b>Sie läuft still, ein Fehler dabei nicht</b> (Nutzer-Entscheidung 2026-08-05). Bei der
/// Datenbankübernahme (§4.8) war „still" durchgehend richtig, weil nichts verlorengehen
/// konnte. Hier kann etwas verlorengehen: RTF und XamlPackage können Dinge tragen, die kein
/// Modell kennt. Was gelingt, gelingt deshalb wortlos; was misslingt, wird **benannt** —
/// dieselbe Regel wie bei <c>Was_noch_nicht_geht_verschwindet_nicht_still</c>.
/// </para>
/// </summary>
/// <param name="Migrated">Ist <see cref="TextDoc.Model"/> jetzt gefüllt?</param>
/// <param name="Issue">Was schiefging; <c>null</c> = nichts.</param>
public sealed record MigrationResult(bool Migrated, string? Issue = null);

/// <summary>
/// Import und Export von Textdokumenten und Tafeln.
/// <para>
/// <b>Warum das eine Schnittstelle ist und kein Core-Code:</b> DOCX-, Markdown- und
/// PDF-Weg stehen auf <c>FlowDocument</c> — genau dem Typ, den Phase 4 durch die eigene
/// Dokument-Engine ersetzt (HANDOFF §4.1). Sie können erst umziehen, wenn es die Engine
/// gibt; bis dahin liegen sie im WPF-Kopf und die ViewModels greifen hierüber zu.
/// </para>
/// </summary>
public interface IDocumentIo
{
    /// <summary>Formate, aus denen importiert werden kann.</summary>
    IReadOnlyList<FileFilter> ImportFormats { get; }

    /// <summary>Formate, in die ein Textdokument geschrieben werden kann.</summary>
    IReadOnlyList<FileFilter> TextExportFormats { get; }

    /// <summary>Formate, in die eine Tafel (Whiteboard/Notizbuch) geschrieben werden kann.</summary>
    IReadOnlyList<FileFilter> BoardExportFormats { get; }

    /// <summary>
    /// Liest <paramref name="path"/> in <paramref name="target"/> ein (samt Seiteneinrichtung)
    /// und liefert den Inhalt in der Form, wie er in <see cref="TextDoc.Rtf"/> gehört.
    /// Wirft bei unlesbaren Dateien — der Aufrufer sammelt und meldet gebündelt.
    /// </summary>
    byte[] Import(string path, TextDoc target);

    /// <summary>Schreibt ein Textdokument; das Format ergibt sich aus der Endung von <paramref name="path"/>.</summary>
    ExportResult ExportText(TextDoc doc, string title, string path);

    /// <summary>Schreibt eine Tafel; das Format ergibt sich aus der Endung von <paramref name="path"/>.</summary>
    ExportResult ExportBoard(WhiteboardDoc doc, string title, string path);

    /// <summary>
    /// Kann dieser Kopf ein Bestandsdokument übernehmen?
    /// <para>
    /// <b>Nur der Windows-Kopf kann es</b>, und das ist keine Bequemlichkeit: RTF und
    /// XamlPackage liest ausschließlich <c>System.Windows.Documents.TextRange</c>. Der
    /// Linux-Kopf darf es deshalb nicht versuchen — er würde ein leeres Dokument erzeugen und
    /// hätte damit den Inhalt scheinbar gelöscht.
    /// </para>
    /// </summary>
    bool CanMigrate { get; }

    /// <summary>
    /// Übernimmt <paramref name="doc"/> aus dem Altformat ins eigene Modell und füllt
    /// <see cref="TextDoc.Model"/>. <b>Wirft nicht</b> — was schiefgeht, steht im Ergebnis:
    /// eine Ausnahme mitten im Öffnen eines Dokuments wäre für den Nutzer dasselbe wie ein
    /// Absturz.
    /// </summary>
    MigrationResult Migrate(TextDoc doc);
}
