namespace GonkNote.Core.Text;

/// <summary>
/// <b>Suchen und Ersetzen im Dokumentmodell</b> — Phase 5, Schritt ①c.
///
/// <para>
/// <b>Warum das hier neu entsteht und nicht umzieht:</b> Der WPF-Kopf hat Suchen &amp;
/// Ersetzen seit jeher (<c>TextEditorView.Find.cs</c>), aber vollständig auf
/// <c>TextPointer</c> und <c>TextRange</c> — das gibt es nur unter Windows. **Anders als
/// beim Tafel-Export (§4.77) und beim Formen-Stift (§4.78) war das keine Verwechslung,
/// sondern echt:** dort lag Code im falschen Projekt, hier steht er auf einer Schranke.
/// *Nicht jede Datei in einem Kopf liegt dort zu Unrecht — man muss nachsehen, statt es
/// anzunehmen.*
/// </para>
/// <para>
/// <b>Gesucht wird über die Absätze und ihre flache Stücksicht</b>
/// (<see cref="TdCursor.Stuecke"/>), also über dasselbe Koordinatensystem, in dem der Cursor
/// steht. Herauskommt eine <see cref="TdSelection"/> — der Kopf muss sie nur noch anzeigen,
/// und Ersetzen ist danach ein gewöhnliches <see cref="TdEdit.Ersetzen"/> mit Undo.
/// </para>
/// <para>
/// <b>⛔ Ein Unterschied zum WPF-Kopf ist Absicht und keine Nachlässigkeit:</b> Der sucht
/// *„innerhalb einzelner Text-Runs"* (so steht es dort). **Ein Treffer, der über eine
/// Formatgrenze läuft, wird drüben also nicht gefunden** — wer „Hallo" schreibt und das
/// „llo" fett macht, findet „Hallo" nicht mehr. Hier wird über den **ganzen Absatztext**
/// gesucht; Formatgrenzen sind für die Suche unsichtbar. *Das ist die richtige Antwort, und
/// der WPF-Kopf steht damit als benannter Unterschied da* (§5e).
/// </para>
/// </summary>
public static class TdSuche
{
    /// <summary>Obergrenze für „alle ersetzen" — dieselbe wie im WPF-Kopf.</summary>
    public const int MaxErsetzungen = 10_000;

    /// <summary>
    /// Der nächste Treffer ab <paramref name="ab"/>, <b>mit Umlauf</b>: Wer am Ende
    /// ankommt, sucht vom Anfang weiter. <c>null</c> heißt „kommt im Dokument nicht vor" —
    /// und zwar erst, nachdem einmal ganz herumgesucht wurde.
    ///
    /// <para>
    /// <b>Verglichen wird ohne Rücksicht auf Groß- und Kleinschreibung</b>, kulturabhängig
    /// (<see cref="StringComparison.CurrentCultureIgnoreCase"/>) — dieselbe Regel wie drüben.
    /// Ein deutsches „STRASSE" findet damit „Straße".
    /// </para>
    /// </summary>
    public static TdSelection? Naechster(TdDocument doc, string suche, TdPosition ab)
    {
        if (string.IsNullOrEmpty(suche)) return null;

        var absaetze = TdCursor.Absaetze(doc);
        if (absaetze.Count == 0) return null;

        var start = TdCursor.Normalisieren(doc, ab);
        int startAbsatz = Math.Clamp(start.Paragraph, 0, absaetze.Count - 1);
        int startOffset = TdCursor.Linear(absaetze[startAbsatz], start);

        // Zwei Durchgänge: vom Cursor bis zum Ende, dann vom Anfang bis zum Cursor.
        // **Der zweite darf den Startabsatz noch einmal anfassen** — ein Treffer, der davor
        // liegt, ist beim Umlauf der richtige.
        for (int runde = 0; runde < 2; runde++)
        {
            int von = runde == 0 ? startAbsatz : 0;
            int bis = runde == 0 ? absaetze.Count - 1 : startAbsatz;

            for (int p = von; p <= bis; p++)
            {
                string text = AbsatzText(absaetze[p]);
                int abOffset = runde == 0 && p == startAbsatz ? startOffset : 0;
                if (abOffset > text.Length) continue;

                int idx = text.IndexOf(suche, abOffset, StringComparison.CurrentCultureIgnoreCase);

                // Beim Umlauf im Startabsatz zählt nur, was **vor** dem Cursor liegt —
                // sonst käme derselbe Treffer ein zweites Mal.
                if (runde == 1 && p == startAbsatz && idx >= startOffset) idx = -1;

                if (idx >= 0) return Treffer(absaetze[p], p, idx, suche.Length);
            }
        }
        return null;
    }

    /// <summary>
    /// Ersetzt <b>alle</b> Vorkommen und liefert, wie viele es waren.
    ///
    /// <para>
    /// <b>Rückwärts durchs Dokument</b>, und das ist kein Geschmack: Jede Ersetzung ändert
    /// die Länge des Absatzes und damit die Stellen dahinter. Wer vorwärts läuft, muss nach
    /// jedem Schritt neu suchen — wer rückwärts läuft, lässt alles, was er noch braucht,
    /// unberührt hinter sich.
    /// </para>
    /// <para>
    /// <b>Angewandt wird hier, nicht beim Aufrufer.</b> Das ist die Ausnahme vom Muster in
    /// <see cref="TdEdit"/> (dort baut die Methode nur, der Kopf ruft <c>Anwenden</c>) — und
    /// sie ist erzwungen: <b>jede Ersetzung tauscht die Blöcke aus</b>, die nächste rechnet
    /// also gegen ein Dokument, das es ohne die vorige noch gar nicht gibt. *Am gefallenen
    /// Wächter gemessen: ohne <c>Anwenden</c> kamen drei Änderungen heraus und im Dokument
    /// stand unverändert der alte Text.*
    /// </para>
    /// <para>
    /// <b>⚠ Und was das für den Verlauf heißt, steht hier, weil es sonst überrascht:</b> Der
    /// Kopf bekommt die Änderungen einzeln und schiebt sie einzeln in <see cref="TdUndo"/>.
    /// Aufeinanderfolgende verschmelzen dort von selbst, <b>über Absatzgrenzen hinweg aber
    /// nicht</b> — „alle ersetzen" ist also so viele Undo-Schritte, wie Absätze betroffen
    /// waren. Benannt statt versteckt.
    /// </para>
    /// </summary>
    public static IReadOnlyList<TdChange> AlleErsetzen(TdDocument doc, string suche, string ersatz)
    {
        var aenderungen = new List<TdChange>();
        if (string.IsNullOrEmpty(suche)) return aenderungen;

        int anzahlAbsaetze = TdCursor.Absaetze(doc).Count;

        for (int p = anzahlAbsaetze - 1; p >= 0; p--)
        {
            if (TdCursor.AbsatzAn(doc, p) is not { } absatz) continue;
            string text = AbsatzText(absatz);

            var stellen = new List<int>();
            int idx = text.IndexOf(suche, StringComparison.CurrentCultureIgnoreCase);
            while (idx >= 0 && stellen.Count < MaxErsetzungen)
            {
                stellen.Add(idx);
                idx = text.IndexOf(suche, idx + suche.Length, StringComparison.CurrentCultureIgnoreCase);
            }

            for (int i = stellen.Count - 1; i >= 0; i--)
            {
                if (aenderungen.Count >= MaxErsetzungen) return aenderungen;

                // **Der Absatz wird jedes Mal frisch geholt.** Eine angewandte Änderung
                // tauscht die Blöcke aus; die Referenz von oben zeigt danach auf einen
                // Absatz, der nicht mehr im Dokument steht — und das Ersetzen liefe ins
                // Leere, ohne zu klagen.
                if (TdCursor.AbsatzAn(doc, p) is not { } aktuell) break;

                var treffer = Treffer(aktuell, p, stellen[i], suche.Length);

                // **Das Format kommt vom Treffer selbst**, nicht aus dem Nichts: Wer ein
                // fettes Wort ersetzt, will ein fettes Wort zurück.
                //
                // ⚠ **Und zwar vom ENDE des Treffers, nicht vom Anfang** — das ist an einem
                // gefallenen Wächter gemessen: `FormatBei` erbt nach **links** (§4.30, „ein
                // eingefügtes Zeichen erbt das Format links davon"). Am Trefferanfang ist
                // der linke Nachbar aber das Stück **davor**, also gerade nicht der Treffer.
                // In „mager **Hund**" kam so das Format von „mager " heraus, und der Ersatz
                // stand mager da.
                var format = TdEdit.FormatBei(aktuell, treffer.End);

                if (TdEdit.Ersetzen(doc, treffer, TdFragment.Text(ersatz, format)) is { } aenderung)
                {
                    aenderung.Anwenden();
                    aenderungen.Add(aenderung);
                }
            }
        }
        return aenderungen;
    }

    // ==================== Innen ====================

    /// <summary>
    /// Der Klartext eines Absatzes in <b>Cursorschritten</b>.
    ///
    /// <para>
    /// <b>Das ist nicht <c>PlainText()</c>, und der Unterschied ist der Punkt:</b> Ein Feld,
    /// ein Bild und ein Zeilenumbruch sind für den Cursor **ein** Schritt breit, liefern im
    /// Klartext aber nichts (§4.20, §4.21). Wer sie ausließe, bekäme einen Text, dessen
    /// Indizes nicht mehr zu <see cref="TdCursor.Linear"/> passen — und der Treffer säße
    /// hinterher um so viele Zeichen daneben, wie Felder davor stehen.
    /// </para>
    /// <para>
    /// Sie werden deshalb durch <b>ein Ersatzzeichen</b> vertreten: <c>U+FFFC</c>, das
    /// „Object Replacement Character". <b>Es ist bewusst kein Leerzeichen</b> — sonst fände
    /// eine Suche nach „a b" ein „a" vor einem Bild und ein „b" dahinter.
    /// </para>
    /// </summary>
    private static string AbsatzText(TdParagraph absatz)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var stueck in TdCursor.Stuecke(absatz))
        {
            if (stueck is TdRun run) sb.Append(run.Text);
            else sb.Append('￼', TdCursor.Laenge(stueck));
        }
        return sb.ToString();
    }

    /// <summary>Aus Absatznummer und Zeichenabstand eine Auswahl über <paramref name="laenge"/> Zeichen.</summary>
    private static TdSelection Treffer(TdParagraph absatz, int absatzIndex, int von, int laenge) =>
        new(TdCursor.AusLinear(absatz, absatzIndex, von),
            TdCursor.AusLinear(absatz, absatzIndex, von + laenge));
}
