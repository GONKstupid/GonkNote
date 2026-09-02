namespace GonkNote.Core.Text;

/// <summary>
/// <b>Etwas einfügen, das kein Text ist</b> — Seitenumbruch, Tabelle. Schritt 6, zweite Hälfte
/// (HANDOFF §6).
///
/// <para>
/// <b>Warum <see cref="TdFragment"/> das nicht kann, und warum es das auch nicht können
/// soll.</b> Ein Fragment beschreibt „ein oder mehrere Absätze voller Stücke" (§4.32) — genau
/// das, was beim Tippen, Löschen und Einfügen von Text entsteht. Ein Seitenumbruch ist keiner
/// dieser Absätze, sondern steht **zwischen** ihnen; eine Tabelle erst recht. Man könnte
/// <c>TdFragment</c> um Blöcke erweitern — dann trüge aber jeder Tastendruck eine Möglichkeit
/// mit sich, die er nie braucht, und <c>Ersetzen</c> bekäme einen Zweig, den nur diese Datei
/// auslöst. **Ein eigener Handgriff neben <c>Ersetzen</c> ist billiger als ein Sonderfall
/// darin.**
/// </para>
/// <para>
/// <b>Der Handgriff ist trotzdem derselbe Gedanke:</b> Die Auswahl verschwindet, der Absatz
/// wird an ihrer Stelle **geteilt**, und die neuen Blöcke kommen zwischen die beiden Hälften.
/// Damit ist „Seitenumbruch einfügen" dasselbe wie „Tabelle einfügen" und beides dasselbe wie
/// die Absatzmarke aus §4.32 — nur dass zwischen den Hälften diesmal etwas steht.
/// </para>
/// </summary>
public static class TdBlockEdit
{
    /// <summary>
    /// Fügt Blöcke an der Auswahl ein. Was ausgewählt war, wird dabei ersetzt.
    ///
    /// <para>
    /// <b>Die Schreibmarke landet im ersten Absatz *innerhalb* des Eingefügten</b>, wenn es
    /// einen gibt — bei einer Tabelle also in der ersten Zelle, wie in Word. Gibt es keinen
    /// (Seitenumbruch), steht sie am Anfang der unteren Hälfte. **Eine Regel und keine
    /// Fallunterscheidung nach Blockart:** Wer hier „bei Tabellen in die Zelle, sonst dahinter"
    /// schriebe, müsste jede künftige Blockart noch einmal einsortieren.
    /// </para>
    /// <para>
    /// <b>Die beiden Hälften bleiben stehen, auch wenn sie leer sind.</b> Ein Seitenumbruch am
    /// Absatzanfang darf den Absatz nicht verschlucken — und eine Tabelle, die als erster Block
    /// eines Abschnitts stünde, ließe sich davor nicht mehr anklicken; in Word ist das die Lage,
    /// aus der man ohne Tastentrick nicht mehr herauskommt.
    /// </para>
    /// <para>
    /// <c>null</c> heißt „nichts zu tun", mit denselben drei Gründen wie in
    /// <see cref="TdEdit.Ersetzen"/> — dazu einem vierten: <b>keine Blöcke übergeben.</b>
    /// </para>
    /// </summary>
    public static TdChange? Einfuegen(TdDocument doc, TdSelection auswahl, params TdBlock[] bloecke)
    {
        if (bloecke.Length == 0) return null;

        var gezogen = TdCursor.Normalisieren(doc, auswahl);
        var start = gezogen.Start;
        var ende = gezogen.End;

        if (TdEdit.Bereich(doc, start, ende) is not { } bereich) return null;
        var (container, iA, iB, absatzA, absatzB) = bereich;

        int von = TdCursor.Linear(absatzA, start);
        int bis = TdCursor.Linear(absatzB, ende);

        // Kopf und Schwanz wie in `Ersetzen` — nur dass dazwischen nichts *eingefügt* wird,
        // sondern die neuen Blöcke stehen.
        var oben = Absatz(absatzA, TdEdit.Teil(absatzA.Inlines, 0, von));
        var unten = Absatz(absatzB, TdEdit.Teil(absatzB.Inlines, bis, TdCursor.Laenge(absatzB)));

        var alt = container.GetRange(iA, iB - iA + 1);
        var neu = new List<TdBlock>(bloecke.Length + 2) { oben };
        neu.AddRange(bloecke);
        neu.Add(unten);

        return new TdChange(
            container, iA, alt, neu, gezogen, new TdSelection(Danach(start, bloecke, neu)),
            TdEditArt.Struktur, schliesstGruppe: true);
    }

    /// <summary>Ein Seitenumbruch an der Auswahl.</summary>
    public static TdChange? Seitenumbruch(TdDocument doc, TdSelection auswahl) =>
        Einfuegen(doc, auswahl, new TdPageBreak());

    /// <summary>
    /// Eine waagerechte Trennlinie — <b>ein leerer Absatz mit Unterstrich</b>
    /// (<see cref="TdParaFormat.BottomBorder"/>).
    ///
    /// <para>
    /// <b>Kein eigener Blocktyp, und das ist dieselbe Entscheidung wie beim Listenpunkt</b>
    /// (§4.17): Word kennt eine Trennlinie ebenfalls als Absatzrahmen, DOCX schreibt sie als
    /// <c>w:pBdr</c>, und der Umbruch zeichnet sie seit §4.15. Ein Blocktyp dafür müsste durch
    /// jeden Export, jeden Umbruch und jede Trefferrechnung eigens hindurch — für eine Linie,
    /// die es als Absatzformat schon gibt.
    /// </para>
    /// <para>
    /// <b>Die Linie steht *unter* dem leeren Absatz</b>, nicht unter dem Text davor: Sonst
    /// gehörte sie dem Absatz darüber und verschwände, wenn jemand dort die Formatierung
    /// zurücksetzt.
    /// </para>
    /// </summary>
    public static TdChange? Trennlinie(TdDocument doc, TdSelection auswahl)
    {
        var linie = new TdParagraph
        {
            Format = { BottomBorder = new TdBorder(1, "#D4DEEA") },
        };

        return Einfuegen(doc, auswahl, linie);
    }

    /// <summary>
    /// Eine leere Tabelle mit <paramref name="zeilen"/> × <paramref name="spalten"/> Zellen.
    ///
    /// <para>
    /// <b>Ohne Spaltenbreiten.</b> <see cref="TdTable.Spaltenbreiten"/> teilt dann gleichmäßig,
    /// und zwar an der Breite, die beim Umbruch wirklich zur Verfügung steht — hier eine Zahl
    /// hineinzuschreiben hieße, den Seitenrand zu raten, den der Abschnitt erst später sagt
    /// (§4.19).
    /// </para>
    /// <para>
    /// <b>Jede Zelle bekommt einen leeren Absatz</b> und nicht gar nichts: In einer Zelle ohne
    /// Absatz hätte der Cursor keinen Ort — dieselbe Überlegung, aus der ein leeres Dokument
    /// einen leeren Absatz hat (<see cref="TdDocument.Leer"/>).
    /// </para>
    /// </summary>
    public static TdChange? Tabelle(TdDocument doc, TdSelection auswahl, int zeilen, int spalten)
    {
        if (zeilen < 1 || spalten < 1) return null;

        var tabelle = new TdTable();
        for (int z = 0; z < zeilen; z++)
        {
            var zeile = new TdTableRow();
            for (int s = 0; s < spalten; s++)
                zeile.Cells.Add(new TdTableCell(new TdParagraph()));
            tabelle.Rows.Add(zeile);
        }

        return Einfuegen(doc, auswahl, tabelle);
    }

    /// <summary>
    /// <b>Eine Infobox — ein gefüllter, gerahmter Kasten für einen Hinweis</b> (§4.89).
    ///
    /// <para>
    /// <b>Es ist eine Tabelle mit einer Zelle, und das ist dieselbe Entscheidung wie bei der
    /// Trennlinie</b> (§4.40): Das Modell kann eine gefüllte, gerahmte Fläche bereits
    /// (<see cref="TdTableFormat"/>, <see cref="TdTableCell.Shading"/>), DOCX schreibt sie so,
    /// und der Umbruch zeichnet sie seit §4.19. Ein eigener Blocktyp dafür müsste durch jeden
    /// Export, jeden Umbruch und jede Trefferrechnung eigens hindurch — für einen Kasten, den
    /// es als Tabelle schon gibt. Der WPF-Kopf tut seit jeher dasselbe.
    /// </para>
    /// <para>
    /// <b>Die zwei Farben kommen von außen</b> und stehen nicht hier: Sie gehören der
    /// Farbtabelle des Kopfs (§5 Nr. 27), und ein fester Wert an dieser Stelle stünde im
    /// Dokument und wäre in einem anderen Erscheinungsbild falsch.
    /// </para>
    /// <para>
    /// <b>Die Innenränder sind großzügiger als bei einer gewöhnlichen Tabelle.</b> Eine
    /// Infobox ist zum Lesen da und nicht zum Vergleichen — Text, der am Rahmen klebt, sieht
    /// aus wie ein Versehen.
    /// </para>
    /// </summary>
    public static TdChange? Infobox(
        TdDocument doc, TdSelection auswahl, string fuellung, string rahmen)
    {
        var linie = new TdBorder(1, rahmen);

        var zelle = new TdTableCell(new TdParagraph()) { Shading = fuellung };

        var tabelle = new TdTable
        {
            Format =
            {
                Top = linie, Left = linie, Bottom = linie, Right = linie,
                InsideH = TdBorder.Keine, InsideV = TdBorder.Keine,
                CellPaddingLeftCm = 0.35, CellPaddingRightCm = 0.35,
                CellPaddingTopCm = 0.25, CellPaddingBottomCm = 0.25,
            },
        };
        tabelle.Rows.Add(new TdTableRow { Cells = { zelle } });

        return Einfuegen(doc, auswahl, tabelle);
    }

    // ---------------------------------------------------------------- Kleinteile

    /// <summary>
    /// Wo die Schreibmarke danach steht — <inheritdoc cref="Einfuegen" path="/summary/para[1]"/>
    /// </summary>
    private static TdPosition Danach(TdPosition start, TdBlock[] bloecke, List<TdBlock> neu)
    {
        // Der Kopfabsatz trägt die Nummer, die die Auswahl hatte; alles Weitere zählt dahinter.
        int nummer = start.Paragraph + 1;

        foreach (var block in bloecke)
        {
            foreach (var _ in Absaetze(block)) return new TdPosition(nummer, 0, 0);
            nummer += Anzahl(block);
        }

        // Kein Absatz im Eingefügten: dann der Anfang der unteren Hälfte.
        return new TdPosition(start.Paragraph + Anzahl(neu) - 1, 0, 0);
    }

    private static int Anzahl(TdBlock block) => Absaetze(block).Count();

    private static int Anzahl(IEnumerable<TdBlock> bloecke) => bloecke.Sum(Anzahl);

    /// <summary>
    /// Die Absätze in einem Block, in derselben Reihenfolge wie <see cref="TdDocument.Paragraphs"/>
    /// sie zählt — <b>und das ist die einzige Anforderung an diese Methode.</b> Wer hier anders
    /// zählte als der Durchlauf, dem die Stellen folgen, setzte die Marke in eine fremde Zelle.
    /// </summary>
    internal static IEnumerable<TdParagraph> Absaetze(TdBlock block)
    {
        switch (block)
        {
            case TdParagraph absatz:
                yield return absatz;
                break;

            case TdTable tabelle:
                foreach (var zeile in tabelle.Rows)
                    foreach (var zelle in zeile.Cells)
                        foreach (var innen in zelle.Blocks)
                            foreach (var absatz in Absaetze(innen))
                                yield return absatz;
                break;
        }
    }

    /// <summary>
    /// Eine der beiden Hälften: ein neuer Absatz mit den Formaten der Vorlage.
    /// <inheritdoc cref="TdFormatEdit" path="/summary/para[3]"/>
    /// </summary>
    private static TdParagraph Absatz(TdParagraph vorlage, List<TdInline> stuecke) =>
        new(TdEdit.Aufraeumen(stuecke))
        {
            Format = vorlage.Format,
            CharFormat = vorlage.CharFormat,
            List = vorlage.List,
        };
}
