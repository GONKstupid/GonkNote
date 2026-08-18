using System.Text;

namespace GonkNote.Core.Text;

// ==================== Was die Eingabemethode sieht ====================

/// <summary>
/// Der Ausschnitt des Dokuments, den eine <b>Eingabemethode</b> zu sehen bekommt: ein Absatz
/// als Zeichenkette und die Auswahl darin — <b>Schritt 6a des Schreibens</b> (HANDOFF §6,
/// §5 „Noch offen" 10).
///
/// <para>
/// <b>Wozu das gebraucht wird.</b> Eine Bildschirmtastatur, eine ostasiatische
/// Eingabemethode und jede Diktierhilfe fragen dasselbe: <i>wo steht die Marke, und was steht
/// um sie herum?</i> Ohne diese Auskunft klappt unter GNOME keine Tastatur auf, und von Hand
/// hervorgeholt kommt aus ihr auch nichts an — genau das ist auf dem Laptop gemessen worden
/// (§4.35, „Die Bildschirmtastatur: nicht nur unsichtbar, sondern taub"). Für eine
/// Stylus-first-App mit noch ausstehendem iPadOS-Kopf ist das keine Randfrage.
/// </para>
/// <para>
/// <b>Ein Absatz und nicht das ganze Dokument.</b> „Der umgebende Text" heißt in jeder dieser
/// Schnittstellen <i>der laufende Absatz</i>. Das ganze Dokument hineinzureichen wäre bei
/// 85.000 Zeichen (der Laptop-Messung aus §4.35) eine volle Kopie bei jedem Tastendruck — und
/// die Eingabemethode braucht davon nichts.
/// </para>
/// </summary>
/// <param name="Absatz">
/// Der wievielte Absatz das ist — Zählung wie <see cref="TdDocument.Paragraphs"/>. Er wird
/// mitgegeben, weil der Rückweg (<see cref="TdEingabe.Auswahl"/>) ihn braucht: Eine
/// Eingabemethode antwortet in Abständen innerhalb <see cref="Text"/> und weiß von Absätzen
/// nichts.
/// </param>
/// <param name="Text">Der Absatz als Zeichenkette — siehe <see cref="TdEingabe.Text"/>.</param>
/// <param name="Start">Der kleinere der beiden Abstände, in <see cref="Text"/> gezählt.</param>
/// <param name="Ende">Der größere. Bei leerer Auswahl gleich <see cref="Start"/>.</param>
public readonly record struct TdUmfeld(int Absatz, string Text, int Start, int Ende);

// ==================== Die Naht ====================

/// <summary>
/// Die Rechnung hinter der Eingabe-Naht: <b>ein Absatz als Zeichenkette, in der jeder
/// Cursorschritt genau eine Stelle breit ist</b> — und der Weg hin und zurück zwischen einer
/// <see cref="TdSelection"/> und zwei Abständen darin.
///
/// <para>
/// <b>Warum das in Core steht und nicht im Kopf.</b> Es ist eine Umrechnung auf dem Modell und
/// hängt an keiner Oberfläche: Dieselbe Auskunft braucht der Avalonia-Kopf für
/// <c>TextInputMethodClient</c>, der iPadOS-Kopf später für <c>UITextInput</c> und der
/// WPF-Kopf, sobald er aus dem Modell liest (Schritt 7). Zweimal geschrieben wären es zwei
/// Zählweisen, von denen eine irgendwann um eins danebenliegt — und das Fehlerbild wäre ein
/// Zeichen, das die Bildschirmtastatur an der falschen Stelle einsetzt.
/// </para>
///
/// <para>
/// <b>Die eine Entscheidung, um die sich alles dreht: ein Schritt = ein Zeichen.</b>
/// <see cref="TdCursor.Text"/> liefert den <i>Klartext</i> einer Auswahl, und der ist
/// ausdrücklich <b>nicht</b> so lang, wie die Auswahl Schritte breit ist: Ein Feld und ein
/// Bild sind je einen Cursorschritt breit und steuern kein Zeichen bei (dort benannt, bei
/// <see cref="TdCursor.Laenge(TdInline)"/>). Für den Klartext ist das richtig — im Dokument
/// steht ein Feld und keine Zahl.
/// <br/>
/// <b>Für eine Eingabemethode wäre es verhängnisvoll.</b> Sie bekommt eine Zeichenkette und
/// zwei Abstände darin und rechnet damit weiter — „lösche zwei Zeichen vor der Marke", „ersetze
/// den Bereich 3 bis 7". Klaffen Zeichenzählung und Schrittzählung auseinander, zeigt jeder
/// dieser Abstände hinter jedem Feld um eins daneben, und je weiter im Absatz, desto weiter.
/// <b>Deshalb bekommt hier jedes unteilbare Stück ein Zeichen:</b> ein Zeilenumbruch sein
/// <c>\n</c>, alles andere — Feld, Bild, Diagramm — den <see cref="Platzhalter"/>. Damit gilt
/// <c>Text(absatz).Length == TdCursor.Laenge(absatz)</c>, und <see cref="TdCursor.Linear"/> ist
/// ohne Umrechnung schon der Abstand in dieser Zeichenkette. <b>Das ist der ganze Trick, und
/// ein Wächter hält ihn fest</b> (<c>EingabeUmfeldTests</c>).
/// </para>
///
/// <para>
/// <b>Was hier nicht steht: das Zusammensetzen (Preedit).</b> Eine Eingabemethode meldet
/// unfertigen Text, bevor er im Dokument steht — ein halb getipptes chinesisches Zeichen. Ihn
/// anzuzeigen hieße entweder, ihn ins Modell zu schreiben und wieder herauszunehmen (genau der
/// Griff, vor dem §4.32 warnt), oder ihn über den Text daneben zu malen. <b>Benannt
/// ausgelassen statt halb gebaut</b> (§4.28): Der Kopf meldet <c>SupportsPreedit = false</c>,
/// und die Plattform zeigt den unfertigen Text dann in ihrem eigenen Fenster. Für den Fall,
/// der diesen Schritt ausgelöst hat — eine Bildschirmtastatur für lateinische Schrift —, wird
/// nichts davon gebraucht: Umlaute und tote Tasten kommen schon heute als fertiges Zeichen an,
/// und zwar gemessen (§4.35, V2-47).
/// </para>
/// </summary>
public static class TdEingabe
{
    /// <summary>
    /// Das Zeichen, das für ein unteilbares Stück ohne Klartext einspringt — Feld, Bild,
    /// Diagramm.
    ///
    /// <para>
    /// <b>U+FFFC OBJECT REPLACEMENT CHARACTER</b>, und nicht ein Leerzeichen oder ein Fragezeichen:
    /// Unicode hat für genau diesen Zweck ein Zeichen, jede Eingabemethode kennt es als „hier
    /// steht etwas, das kein Text ist", und es kommt in echtem Text nicht vor. Ein Leerzeichen
    /// dagegen ließe eine Wortvervollständigung über ein Bild hinweg trennen.
    /// </para>
    /// </summary>
    public const char Platzhalter = '￼';

    /// <summary>
    /// Ein Absatz als Zeichenkette, <b>Stelle für Stelle so breit wie der Cursor läuft</b>.
    /// Die Begründung steht oben bei <see cref="TdEingabe"/>.
    /// </summary>
    public static string Text(TdParagraph absatz)
    {
        var stuecke = TdCursor.Stuecke(absatz);

        // Der Normalfall ist ein einziger Lauf — dafür lohnt kein StringBuilder (wie in
        // TdParagraph.PlainText).
        if (stuecke.Count == 1 && stuecke[0] is TdRun einziger) return einziger.Text;

        var sb = new StringBuilder();

        foreach (var stueck in stuecke)
        {
            switch (stueck)
            {
                case TdRun run:
                    sb.Append(run.Text);
                    break;

                case TdLineBreak:
                    sb.Append('\n');
                    break;

                // Feld, Bild, Diagramm — und ein Verweis, falls doch einmal jemand eine
                // unflache Liste hereinreicht. **Über TdCursor.Laenge und nicht über eine 1:**
                // So bleibt die Gleichung auch dann stehen, und sie ist der Sinn der Sache.
                default:
                    sb.Append(Platzhalter, TdCursor.Laenge(stueck));
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Was die Eingabemethode zu sehen bekommt: der Absatz, in dem die <b>Schreibmarke</b>
    /// steht, und die Auswahl darin.
    ///
    /// <para>
    /// <b>Der Absatz der Marke und nicht der des Ankers</b> (<see cref="TdSelection.Focus"/>):
    /// Die Marke ist die Stelle, an der der Nutzer zuletzt war, und an ihr klappt die Tastatur
    /// auf.
    /// </para>
    /// <para>
    /// <b>Eine Auswahl über mehrere Absätze wird auf diesen einen beschnitten</b> — liegt der
    /// Anker davor, beginnt sie bei 0, liegt er dahinter, endet sie am Absatzende. Das ist eine
    /// Auskunft und keine Änderung: Ausgewählt bleibt, was ausgewählt ist; die Eingabemethode
    /// erfährt nur den Teil, über den sie überhaupt reden kann.
    /// </para>
    /// <para>
    /// <b><see cref="TdUmfeld.Start"/> ist immer der kleinere Abstand</b>, auch wenn die
    /// Auswahl nach links gezogen wurde. <see cref="TdSelection"/> hält Anker und Spitze
    /// auseinander, weil das Ziehen es verlangt — eine Eingabemethode kennt diesen Unterschied
    /// nicht und schneidet mit den beiden Zahlen in die Zeichenkette. Unsortiert
    /// herausgereicht, wäre die erste Rückwärtsauswahl ein Fehler in fremdem Code.
    /// </para>
    /// </summary>
    public static TdUmfeld Umfeld(TdDocument doc, TdSelection auswahl)
    {
        var absaetze = TdCursor.Absaetze(doc);

        // Ein Dokument ohne Absätze gibt es im Modell nicht — aber der Kopf fragt auch, während
        // noch nichts geladen ist, und dann ist die leere Auskunft die richtige.
        if (absaetze.Count == 0) return new TdUmfeld(0, "", 0, 0);

        var marke = TdCursor.Normalisieren(doc, auswahl.Focus);
        int p = marke.Paragraph;
        var absatz = absaetze[p];

        int hier = TdCursor.Linear(absatz, marke);

        var anker = TdCursor.Normalisieren(doc, auswahl.Anchor);
        int dort = anker.Paragraph < p ? 0
            : anker.Paragraph > p ? TdCursor.Laenge(absatz)
            : TdCursor.Linear(absatz, anker);

        return new TdUmfeld(p, Text(absatz), Math.Min(hier, dort), Math.Max(hier, dort));
    }

    /// <summary>
    /// Der Rückweg: aus zwei Abständen in <see cref="TdUmfeld.Text"/> wieder eine Auswahl.
    /// <b>Er wird gebraucht, wenn die Eingabemethode die Auswahl setzt</b> — eine
    /// Bildschirmtastatur tut das beim Vervollständigen eines Wortes.
    ///
    /// <para>
    /// <b>Der Anker landet auf <paramref name="start"/>, die Marke auf
    /// <paramref name="ende"/>.</b> Eine Eingabemethode reicht sortierte Zahlen herein und hat
    /// keine Meinung dazu, welches Ende der Nutzer in der Hand hält; die naheliegende Zuordnung
    /// ist die, bei der ein anschließendes Umschalt+Rechts die Auswahl vergrößert.
    /// </para>
    /// <para>
    /// Beide Abstände werden von <see cref="TdCursor.AusLinear"/> in den gültigen Bereich
    /// geklemmt und in die kanonische Form gebracht — <b>nichts, was von außen kommt, wird
    /// geglaubt</b>.
    /// </para>
    /// </summary>
    public static TdSelection Auswahl(TdDocument doc, int absatzIndex, int start, int ende)
    {
        var absaetze = TdCursor.Absaetze(doc);
        if (absaetze.Count == 0) return new TdSelection(TdPosition.Null);

        int p = Math.Clamp(absatzIndex, 0, absaetze.Count - 1);
        var absatz = absaetze[p];

        return new TdSelection(
            TdCursor.AusLinear(absatz, p, start),
            TdCursor.AusLinear(absatz, p, ende));
    }
}
