using GonkNote.Core.Platform;
using GonkNote.Core.Services;
using GonkNote.Services;

namespace GonkNote.Core.Text;

/// <summary>
/// Der eine Exportweg eines Textdokuments — PDF, DOCX, Markdown und PNG hinter einer
/// Endung.
///
/// <para>
/// <b>Warum das hier steht und nicht in den Köpfen.</b> Seit §4.27 laufen alle vier Wege
/// gegen <see cref="TdDocument"/> und stehen vollständig in Core. Der Rest war ein
/// <c>switch</c> über die Endung — vierzehn Zeilen, die im WPF-Kopf standen. Sie hier
/// hinzuschreiben und im Linux-Kopf ein zweites Mal, wäre genau die Falle aus §4.13: zwei
/// Fassungen derselben Entscheidung, die auseinanderdriften, sobald ein Format dazukommt.
/// <b>Wer ein Format ergänzt, ergänzt es an einer Stelle</b>, und beide Köpfe haben es.
/// </para>
///
/// <para>
/// <b>Was hier bewusst nicht steht:</b> woher das Modell kommt. Der WPF-Kopf kann ein
/// Bestandsdokument aus <c>Rtf</c> übernehmen, der Linux-Kopf nicht (§4.22) — das ist die
/// eine Sache, die die Köpfe wirklich verschieden beantworten, und sie bleibt bei ihnen.
/// </para>
/// </summary>
public static class TdExport
{
    /// <summary>
    /// Die Formate, in die ein Textdokument geschrieben werden kann — <b>in beiden
    /// Köpfen dieselben</b>, weil alle vier in Core stehen.
    /// <para>
    /// Bei jedem Zugriff neu gebaut und nicht einmal beim Start: die Beschriftungen hängen an
    /// <c>Loc.Current</c>, und ein Sprachwechsel ginge sonst still an ihnen vorbei
    /// (HANDOFF §7, „Texte, die der Code setzt").
    /// </para>
    /// </summary>
    public static IReadOnlyList<FileFilter> Formate =>
    [
        new(Loc.T("Filter.Pdf"), ".pdf"),
        new(Loc.T("Filter.Word"), ".docx"),
        new(Loc.T("Filter.Markdown"), ".md"),
        new(Loc.T("Filter.Png"), ".png"),
    ];

    /// <summary>
    /// Die Formate, aus denen ein Textdokument <b>gelesen</b> werden kann — <b>in beiden
    /// Köpfen dieselben</b>, seit Phase 5, Schritt ④.
    ///
    /// <para>
    /// <b>Sie steht aus demselben Grund hier wie <see cref="Formate"/>:</b> Die Exportliste
    /// wanderte in §4.28 nach Core, weil zwei Aufzählungen zwei sind, von denen später eine
    /// jemand ändert — <b>die Importliste stand danach trotzdem weiter in beiden Köpfen
    /// einzeln</b>, und sie <i>war</i> auseinandergelaufen: Der Linux-Kopf bot nur DOCX an
    /// (siehe <c>AvaloniaDocumentIo.ImportFormats</c>). <b>Genau der Fall, den §4.77 mit der
    /// leeren Tafel-Exportliste schon einmal hatte</b>, und er ist beide Male niemandem
    /// aufgefallen, weil eine kürzere Liste nach einer Entscheidung aussieht.
    /// </para>
    /// <para>
    /// <b>„Alle Dateien" steht bewusst nicht darin.</b> Der Leser entscheidet an der Endung;
    /// was weder <c>.docx</c> noch <c>.md</c> ist, läuft in eine Ausnahme. Ein Filter, der
    /// alles anbietet, verspricht mehr als der Leser hält — dieselbe Überlegung wie beim
    /// leeren Tafel-Filter (§4.77), nur andersherum.
    /// </para>
    /// </summary>
    public static IReadOnlyList<FileFilter> Importformate =>
    [
        new(Loc.T("Filter.Documents"), ".docx", ".md"),
        new(Loc.T("Filter.Word"), ".docx"),
        new(Loc.T("Filter.Markdown"), ".md"),
    ];

    /// <summary>
    /// Schreibt <paramref name="modell"/> nach <paramref name="pfad"/>; das Format ergibt
    /// sich aus der Endung, alles Unbekannte wird ein PDF.
    /// </summary>
    /// <param name="bilder">
    /// Woher die Bytes eines Bildes kommen. <c>null</c> ist erlaubt — dann bleibt jedes Bild
    /// ein Rahmen mit seinem Alternativtext (Dauerregel 4).
    /// </param>
    /// <param name="titel">Der Wert für <c>{TITEL}</c> in Kopf- und Fußzeile.</param>
    /// <param name="felder">
    /// Datum und Titel für die Felder. <b>Core fragt die Uhr nicht selbst</b> (§4.20) — wer
    /// exportiert, gibt sie mit.
    /// </param>
    public static ExportResult Schreiben(
        TdDocument modell, string pfad, ITdImages? bilder, string titel, TdFieldContext felder)
    {
        string endung = Path.GetExtension(pfad).ToLowerInvariant();

        List<string> geschrieben = [pfad];
        int beanstandungen = 0;

        switch (endung)
        {
            case ".docx":
                TdDocx.Schreiben(modell, pfad, bilder, titel);
                beanstandungen = TdDocx.Pruefen(pfad);
                break;

            case ".md":
                TdMarkdown.Export(modell, pfad, felder);
                break;

            case ".png":
                geschrieben = TdPdf.Png(modell, pfad, bilder, titel, felder);
                break;

            default:
                TdPdf.Schreiben(modell, pfad, bilder, titel, felder);
                break;
        }

        // Nur PDF und PNG malen die Bilder wirklich; DOCX und Markdown reichen sie durch und
        // können deshalb gar nichts vermissen.
        int fehlend = endung is ".pdf" or ".png" or ""
            ? DocumentHealth.MissingImages(modell, bilder)
            : 0;

        return new ExportResult(geschrieben, beanstandungen, fehlend);
    }
}
