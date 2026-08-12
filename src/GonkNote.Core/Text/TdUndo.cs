namespace GonkNote.Core.Text;

/// <summary>
/// Rückgängig und Wiederherstellen für ein Textdokument — Schritt 3 des Schreibens
/// (HANDOFF §6).
///
/// <para>
/// <b>Er macht nichts rückgängig.</b> Das kann eine <see cref="TdChange"/> seit Schritt 2
/// selbst, und zwar beliebig oft im Wechsel. Was hier steht, ist nur das, was eine Änderung
/// allein nicht wissen kann: **in welcher Reihenfolge** die Schritte gemacht wurden und
/// **welche davon der Nutzer als einen erlebt hat**.
/// </para>
///
/// <para>
/// <b>Warum er neben <c>UndoStack</c> steht und nicht darin</b> — die Frage aus §6, hier
/// beantwortet und nicht angenommen:
/// <list type="bullet">
///   <item>
///     <c>UndoStack.Push</c> verlangt eine <c>WbPage</c>, und <c>IEditAction</c> bekommt sie
///     bei jedem Zug gereicht: Eine Zeichenflächen-Aktion hält Elemente, nicht ihren Ort.
///     **Ein Textdokument hat keine Seite** — die Stelle einer Änderung ist ihr Container, und
///     den hält sie selbst.
///   </item>
///   <item>
///     <c>UndoStack.Undo</c> gibt die <b>Seite</b> zurück, damit die Anzeige sie zeigt. Hier
///     ist die Antwort eine <b>Auswahl</b>: Ein Rückgängig, das den Text wiederherstellt und
///     den Cursor woanders stehen lässt, zwingt den Nutzer, seine Stelle wiederzufinden.
///   </item>
///   <item>
///     <b>Und das Zusammenfassen gibt es dort gar nicht.</b> Wer „Hallo" tippt, hat einen
///     Handgriff gemacht und nicht fünf; auf der Zeichenfläche stellt sich diese Frage nie,
///     denn ein Strich ist ein Zug. Ohne das Zusammenfassen nimmt Strg+Z ein Zeichen je Druck
///     zurück, und das ist der Fehler, über den jeder als Erstes stolpert.
///   </item>
/// </list>
/// Geblieben ist damit eine Liste, ein Deckel und ein Ereignis — rund zwanzig Zeilen, die
/// beiden gemeinsam sind. <b>Das ist keine Doppelung im Sinne von §4.13</b>: Dort standen
/// Trefferprüfung und Markdown-Grammatik zweimal, also **Verhalten**, in dem ein Fehler
/// zweimal wohnte. Ob beide Stapel eines werden sollen, gehört in die Aufräumrunde von
/// Phase 6; zusammengelegt würden sie heute zwei Typparameter und einen Haken tragen, um
/// zwanzig Zeilen zu teilen.
/// </para>
///
/// <para>
/// <b>Die Namen sind absichtlich die von <c>UndoStack</c></b> (<see cref="Push"/>,
/// <see cref="Undo"/>, <see cref="Redo"/>, <see cref="CanUndo"/>, <see cref="Changed"/>) —
/// wer den einen kennt, erkennt den anderen wieder, und die Frage „sind das zwei?" bleibt
/// sichtbar, statt sich hinter neuen Wörtern zu verstecken.
/// </para>
/// </summary>
public sealed class TdUndo
{
    /// <summary>
    /// Tiefe des Verlaufs — dieselbe Zahl wie in <c>UndoStack</c>, aus demselben Grund: Ein
    /// Schritt **hält Absätze am Leben**, die sonst niemand mehr hält.
    /// <para>
    /// Beim Text wiegt das weniger als auf der Zeichenfläche (ein Absatz ist kein Bild), aber
    /// es wiegt: Jeder Tastendruck legt einen neuen Absatz an, und ohne Grenze bliebe jede
    /// Fassung jedes Absatzes einer langen Sitzung liegen. **Das Zusammenfassen drückt die
    /// Zahl zusätzlich**, denn ein getipptes Wort ist ein Schritt und nicht fünf.
    /// </para>
    /// </summary>
    private const int MaxSchritte = 200;

    // Listen statt Stapel: Beim Überlauf muss der **älteste** Schritt vorne herausfallen.
    private readonly List<TdChange> _zurueck = [];
    private readonly List<TdChange> _vor = [];

    /// <summary>
    /// Ist die oberste Änderung abgeschlossen? Dann wird nicht mehr an sie angebaut.
    /// <para>
    /// Gesetzt wird das von der Änderung selbst (<see cref="TdChange.SchliesstGruppe"/>, ein
    /// getippter Zwischenraum) und von <see cref="Abschliessen"/>, das die Oberfläche ruft,
    /// wenn der Nutzer die Schreibmarke versetzt: Wer klickt, fängt einen neuen Handgriff an.
    /// </para>
    /// </summary>
    private bool _zu;

    public bool CanUndo => _zurueck.Count > 0;

    public bool CanRedo => _vor.Count > 0;

    /// <summary>Für die Knöpfe im Ribbon — wie bei <c>UndoStack</c>.</summary>
    public event Action? Changed;

    /// <summary>
    /// Merkt eine **bereits ausgeführte** Änderung — dieselbe Verabredung wie bei
    /// <c>UndoStack.Push</c>. Wer sie hier hineingibt, hat <see cref="TdChange.Anwenden"/>
    /// schon gerufen.
    ///
    /// <para>
    /// <b>Passt sie an die vorige an, werden beide zu einer</b> (siehe
    /// <see cref="TdChange.Verschmelzen"/>): Ein getipptes Wort ist ein Schritt. Zusammengefasst
    /// wird nur, was **derselben Art** ist und **lückenlos** anschließt — Tippen an Tippen,
    /// Löschen an Löschen. Zwischen Tippen und Löschen liegt immer ein Schnitt, denn wer erst
    /// schreibt und dann wegnimmt, hat sich entschieden.
    /// </para>
    /// </summary>
    public void Push(TdChange aenderung)
    {
        if (Anbauen(aenderung) is not { } zusammen)
        {
            _zurueck.Add(aenderung);
            if (_zurueck.Count > MaxSchritte) _zurueck.RemoveAt(0);
        }
        else
        {
            _zurueck[^1] = zusammen;
        }

        _zu = aenderung.SchliesstGruppe;

        // **Ein neuer Schritt wirft die Wiederherstellung weg** — wie in UndoStack. Wer nach
        // einem Rückgängig weiterschreibt, hat sich für diesen Weg entschieden; ein Vorwärts,
        // das danach in eine andere Fassung führte, wäre eine dritte.
        _vor.Clear();
        Changed?.Invoke();
    }

    /// <summary>
    /// Die zusammengefasste Änderung — oder <c>null</c>, wenn hier ein Schnitt gehört.
    /// </summary>
    private TdChange? Anbauen(TdChange neue)
    {
        if (_zu || _zurueck.Count == 0) return null;
        if (neue.Art == TdEditArt.Struktur) return null;

        var vorige = _zurueck[^1];
        return vorige.Art != neue.Art ? null : vorige.Verschmelzen(neue);
    }

    /// <summary>
    /// Setzt einen Schnitt: Die nächste Änderung fängt einen neuen Schritt an, auch wenn sie
    /// lückenlos anschlösse.
    ///
    /// <para>
    /// <b>Das ruft die Oberfläche, wenn der Nutzer die Schreibmarke versetzt</b> — mit der
    /// Maus, mit den Pfeiltasten, beim Wechsel des Registers. Ohne diesen Schnitt hinge ein
    /// Buchstabe, den jemand zehn Zeilen weiter oben nachträgt, am selben Schritt wie der Satz
    /// davor; ein Strg+Z nähme beides zurück. **Der Verlauf kann das nicht selbst merken:** Er
    /// sieht Änderungen und keine Klicks.
    /// </para>
    /// </summary>
    public void Abschliessen() => _zu = true;

    /// <summary>
    /// Einen Schritt zurück. Zurück kommt die Auswahl, die **vor** der Änderung galt —
    /// <c>null</c>, wenn es nichts zurückzunehmen gibt.
    /// </summary>
    public TdSelection? Undo() => Bewegen(_zurueck, _vor, zurueck: true);

    /// <summary>Einen Schritt wieder vor.</summary>
    public TdSelection? Redo() => Bewegen(_vor, _zurueck, zurueck: false);

    private TdSelection? Bewegen(List<TdChange> von, List<TdChange> nach, bool zurueck)
    {
        if (von.Count == 0) return null;

        var aenderung = von[^1];
        von.RemoveAt(von.Count - 1);

        var auswahl = zurueck ? aenderung.Zuruecknehmen() : aenderung.Anwenden();

        nach.Add(aenderung);
        if (nach.Count > MaxSchritte) nach.RemoveAt(0);

        // Nach jedem Zug ein Schnitt: Wer zurückgeht und dann tippt, baut nicht an dem Schritt
        // weiter, den er gerade zurückgenommen hat.
        _zu = true;

        Changed?.Invoke();
        return auswahl;
    }

    /// <summary>
    /// Wirft den ganzen Verlauf weg — beim Schließen eines Dokuments und immer dann, wenn der
    /// Inhalt **nicht** über diesen Weg ausgetauscht wurde (ein Import, eine Übernahme aus dem
    /// Altformat).
    ///
    /// <para>
    /// <b>Das ist kein Aufräumen, sondern Notwehr:</b> Eine gemerkte Änderung zeigt auf die
    /// Blockliste, in der sie entstanden ist. Steht dort ein anderes Dokument, passt sie nicht
    /// mehr — <see cref="TdChange.Anwenden"/> wirft dann, und das ist besser als still fremde
    /// Absätze zu ersetzen. Wer den Inhalt tauscht, leert hier.
    /// </para>
    /// </summary>
    public void Leeren()
    {
        if (_zurueck.Count == 0 && _vor.Count == 0) return;

        _zurueck.Clear();
        _vor.Clear();
        _zu = false;
        Changed?.Invoke();
    }
}
