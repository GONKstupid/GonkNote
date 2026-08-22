using GonkNote.Core.Models;

namespace GonkNote.Core.Text;

/// <summary>
/// <b>Wer führt: das Altformat oder das eigene Modell?</b> — die Regel „wer voll ist, führt"
/// (HANDOFF §4.22, §4.23), einmal benannt statt dreimal als Längenvergleich hingeschrieben.
///
/// <para>
/// <b>Warum das eine eigene Stelle bekommt.</b> Ein <see cref="TextDoc"/> trägt seinen Inhalt
/// doppelt: als <see cref="TextDoc.Rtf"/> (RTF oder XamlPackage, liest nur WPF) und als
/// <see cref="TextDoc.Model"/> (das eigene Modell, liest jeder Kopf). Welches der beiden gilt,
/// stand bis §4.35 an drei Stellen als <c>doc.Rtf.Length == 0</c> — und drei Schreibweisen
/// derselben Regel sind drei Gelegenheiten, sie verschieden zu meinen. Sobald Schritt 7 die
/// Führung umdreht (§6), ist genau das die Stelle, die sich ändert, und keine andere.
/// </para>
/// <para>
/// <b>✅ Seit Schritt 7 führt das Modell, und zwar in beiden Köpfen</b> (§4.48). Damit ist die
/// Frage „wer führt?" beantwortet und <c>AltformatFuehrt</c> <b>gelöscht</b> — samt der Warnung
/// im Linux-Kopf, die es getragen hat. <b>Eine Funktion, die immer <c>false</c> zurückgibt,
/// wäre schlechter als keine:</b> Sie sähe nach einer offenen Frage aus und würde eines Tages
/// wieder geglaubt.
/// </para>
/// <para>
/// <b>Der Anlass war ein gemessener Datenverlust</b> (§5 „Noch offen" 9): Wer ein Dokument mit
/// gefülltem <see cref="TextDoc.Rtf"/> im Linux-Kopf beschrieb, schrieb in
/// <see cref="TextDoc.Model"/> — und der WPF-Editor schrieb es beim nächsten Speichern
/// bedingungslos aus <see cref="TextDoc.Rtf"/> neu. **Das ist mit §4.48 an der Wurzel
/// behoben**: Der WPF-Editor liest und schreibt jetzt dasselbe Feld.
/// </para>
/// <para>
/// <b>Was bleibt, ist <see cref="UebernahmeStehtAus"/></b> — und das ist eine andere Frage,
/// die es weiter gibt: Ein Dokument aus der Windows-Zeit trägt seinen Inhalt nur im Altfeld,
/// bis er **einmal** übernommen ist. Der Linux-Kopf kann das nicht und muss es wissen.
/// </para>
/// </summary>
public static class TdFuehrung
{
    /// <summary>
    /// <b>Steht die einmalige Übernahme noch aus?</b> Also: es gibt etwas zu übernehmen
    /// (<see cref="TextDoc.Rtf"/> ist gefüllt) und es ist noch nicht geschehen
    /// (<see cref="TextDoc.Model"/> ist leer).
    ///
    /// <para>
    /// <b>Nicht dasselbe wie <see cref="AltformatFuehrt"/>.</b> Nach der Übernahme führt das
    /// Altformat weiter, aber zu übernehmen gibt es nichts mehr — der Unterschied hat den
    /// Linux-Kopf schon einmal eine falsche Auskunft gekostet (§4.29, §5 „Noch offen" 8).
    /// </para>
    /// </summary>
    public static bool UebernahmeStehtAus(TextDoc doc) =>
        doc.Model.Length == 0 && doc.Rtf.Length > 0;
}
