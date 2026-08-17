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
/// <b>Der Anlass ist ein gemessener Datenverlust</b> (§5 „Noch offen" 9): Wer ein Dokument mit
/// gefülltem <see cref="TextDoc.Rtf"/> im Linux-Kopf beschreibt, schreibt in
/// <see cref="TextDoc.Model"/> — und der WPF-Editor schreibt es beim nächsten Speichern
/// bedingungslos aus <see cref="TextDoc.Rtf"/> neu. Die Arbeit ist dann still weg.
/// <see cref="AltformatFuehrt"/> ist die Frage, die der Linux-Kopf stellen muss, bevor er so
/// tut, als sei Schreiben hier gefahrlos.
/// </para>
/// </summary>
public static class TdFuehrung
{
    /// <summary>
    /// <b>Führt das Altformat?</b> Dann zeigt und schreibt der WPF-Editor aus
    /// <see cref="TextDoc.Rtf"/>, und was ein anderer Kopf in <see cref="TextDoc.Model"/>
    /// geschrieben hat, überlebt dessen nächstes Speichern nicht.
    ///
    /// <para>
    /// <b>Es zählt allein das Altfeld und nicht der Vergleich beider.</b> Ein übernommenes
    /// Dokument hat <i>beide</i> Felder gefüllt — und genau das ist der gefährliche Fall, nicht
    /// der harmlose. Wer hier „nur wenn <c>Model</c> leer ist" schriebe, bekäme eine Warnung,
    /// die ausgerechnet dann schweigt, wenn sie gebraucht wird.
    /// </para>
    /// </summary>
    public static bool AltformatFuehrt(TextDoc doc) => doc.Rtf.Length > 0;

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
