namespace GonkNote.Core.Text;

/// <summary>
/// <b>Der Tabellenentwurf</b> — Rahmen, Füllung, Kopfzeile, Zellabstand, Spaltenbreite,
/// verbinden und teilen (§4.90).
///
/// <para>
/// <b>Warum das eine zweite Datei derselben Klasse ist und keine zweite Klasse:</b> Alles hier
/// nimmt denselben Weg wie Zeile und Spalte ein- und ausfügen — <c>Aufsetzen</c> findet die
/// Tabelle, <c>Kopie</c> baut sie neu, <c>Tausch</c> hängt den Schritt in den Verlauf. Eine
/// zweite Klasse hätte dieselben drei Helfer ein zweites Mal gebraucht oder sie öffentlich
/// machen müssen; beides ist schlechter als eine Datei mehr.
/// </para>
/// <para>
/// <b>Die Zellen werden nie an Ort und Stelle umgestellt.</b> Das ist die Regel aus §4.32, und
/// sie gilt hier besonders: Die Sicherung im Rückgängig-Stapel hält <i>dieselbe</i> Tabelle,
/// und wer an ihrem Format dreht, ändert die Vergangenheit mit.
/// </para>
/// </summary>
public static partial class TdTableEdit
{
    /// <summary>Welche Kanten ein Rahmenbefehl meint.</summary>
    public enum Rahmenwahl
    {
        /// <summary>Alle sechs — außen und innen.</summary>
        Alle,

        /// <summary>Nur der äußere Rahmen; innen bleibt, wie es war.</summary>
        Aussen,

        /// <summary>Nur die inneren Linien.</summary>
        Innen,

        /// <summary>Gar keine.</summary>
        Keine,
    }

    /// <summary>
    /// Setzt die Rahmen der Tabelle, in der die Marke steht.
    ///
    /// <para>
    /// <b>Am Tabellenformat und nicht an den Zellen</b> — dort führt das Modell sie (§4.18),
    /// dort schreibt DOCX sie, und dort gelten sie für die ganze Tabelle. Eine Kante je Zelle
    /// wäre dieselbe Angabe n-mal, und die erste, die abweicht, ist ein Fehler, den niemand
    /// findet.
    /// </para>
    /// <para>
    /// <b>Wer „nur außen" wählt, lässt innen stehen, was dort steht</b> — sonst wäre jeder
    /// Rahmenbefehl heimlich ein „alle".
    /// </para>
    /// </summary>
    public static TdChange? Rahmen(
        TdDocument doc, TdSelection auswahl, Rahmenwahl wahl, double breitePt, string farbe)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, _, _, liste, index, gezogen) = lage;

        var neu = Kopie(tabelle);
        var format = neu.Format;
        var wert = wahl == Rahmenwahl.Keine
            ? TdBorder.Keine
            : new TdBorder(breitePt, farbe);

        if (wahl is Rahmenwahl.Alle or Rahmenwahl.Aussen or Rahmenwahl.Keine)
        {
            format.Top = wert;
            format.Left = wert;
            format.Bottom = wert;
            format.Right = wert;
        }

        if (wahl is Rahmenwahl.Alle or Rahmenwahl.Innen or Rahmenwahl.Keine)
        {
            format.InsideH = wert;
            format.InsideV = wert;
        }

        neu.Format = format;

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: false);
    }

    /// <summary>
    /// Färbt die Zelle, in der die Marke steht — <c>null</c> als Farbe nimmt die Füllung weg.
    ///
    /// <para>
    /// <b>Eine Zelle und nicht die Auswahl, und das ist eine gemessene Einschränkung.</b> Die
    /// Auswahl des Editors ist eine Spanne über Absätze (§4.30) und kennt kein Rechteck aus
    /// Zellen; ein Befehl, der „alle berührten Zellen" behauptete, träfe je nach Ziehrichtung
    /// verschiedene. <b>Das ist ein benannter Unterschied zum WPF-Kopf</b>, dessen
    /// <c>TableCellColor</c> die markierten Zellen färbt.
    /// </para>
    /// </summary>
    public static TdChange? Fuellung(TdDocument doc, TdSelection auswahl, string? farbe)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, zeile, spalte, liste, index, gezogen) = lage;

        var neu = Kopie(tabelle);
        if (!InGrenzen(neu, zeile, spalte)) return null;
        if (neu.Rows[zeile].Cells[spalte].Shading == farbe) return null;

        neu.Rows[zeile].Cells[spalte].Shading = farbe;

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: false);
    }

    /// <summary>
    /// Macht die erste Zeile zur Kopfzeile oder nimmt ihr das wieder.
    ///
    /// <para>
    /// <b>Es ist keine Formatierung, sondern eine Auskunft:</b> Eine Kopfzeile
    /// <b>wiederholt sich</b> auf jeder Folgeseite (§4.19), und DOCX schreibt sie als
    /// <c>w:tblHeader</c>. Wer sie nur fett haben will, nimmt den Fett-Knopf.
    /// </para>
    /// </summary>
    public static TdChange? Kopfzeile(TdDocument doc, TdSelection auswahl, bool an)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, _, _, liste, index, gezogen) = lage;

        if (tabelle.Rows.Count == 0 || tabelle.Rows[0].IsHeader == an) return null;

        var neu = Kopie(tabelle);
        neu.Rows[0].IsHeader = an;

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: false);
    }

    /// <summary>
    /// Setzt den waagerechten Abstand zwischen Zellrand und Text.
    ///
    /// <para>
    /// <b>Links und rechts gemeinsam, oben und unten gar nicht:</b> Word führt alle vier
    /// getrennt, aber vier Zahlenfelder für eine Angabe, die in neunundneunzig von hundert
    /// Fällen gleich ist, sind eine Einstellung, die man oft sieht und selten braucht (§4.38).
    /// Oben und unten bleiben bei ihrer Vorgabe — dieselbe Aufteilung, die
    /// <see cref="TdTableFormat"/> ohnehin trifft.
    /// </para>
    /// </summary>
    public static TdChange? Zellabstand(TdDocument doc, TdSelection auswahl, double cm)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, _, _, liste, index, gezogen) = lage;

        double wert = Math.Clamp(cm, 0, 2);
        if (Math.Abs(tabelle.Format.CellPaddingLeftCm - wert) < 0.001 &&
            Math.Abs(tabelle.Format.CellPaddingRightCm - wert) < 0.001) return null;

        var neu = Kopie(tabelle);
        var format = neu.Format;
        format.CellPaddingLeftCm = wert;
        format.CellPaddingRightCm = wert;
        neu.Format = format;

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: false);
    }

    /// <summary>
    /// Setzt die Breite der Spalte, in der die Marke steht — <c>null</c> gibt <b>alle</b>
    /// Spalten wieder frei.
    ///
    /// <para>
    /// <b>„Frei" ist das AutoAnpassen, und es braucht keinen eigenen Handgriff:</b> Ohne
    /// Angabe teilt <see cref="TdTable.Spaltenbreiten"/> den Platz gleichmäßig auf, und zwar an
    /// der Breite, die beim Umbruch wirklich zur Verfügung steht (§4.19). Eine Zahl hier
    /// hineinzuschreiben hieße, den Seitenrand zu raten, den der Abschnitt erst später sagt.
    /// </para>
    /// <para>
    /// <b>Beim Setzen werden alle Breiten aufgefüllt</b>, nicht nur die eine. Eine Liste mit
    /// einer Lücke wäre eine Tabelle, in der die dritte Spalte die Breite der zweiten trägt.
    /// </para>
    /// </summary>
    public static TdChange? Spaltenbreite(TdDocument doc, TdSelection auswahl, double? cm)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, _, spalte, liste, index, gezogen) = lage;

        var neu = Kopie(tabelle);

        if (cm is null)
        {
            if (neu.ColumnWidthsCm.Count == 0) return null;
            neu.ColumnWidthsCm.Clear();
        }
        else
        {
            if (spalte < 0 || spalte >= tabelle.Spaltenzahl()) return null;

            var breiten = tabelle.Spaltenbreiten(AutoBreiteCm);
            neu.ColumnWidthsCm.Clear();
            neu.ColumnWidthsCm.AddRange(breiten);
            neu.ColumnWidthsCm[spalte] = Math.Clamp(cm.Value, 0.5, 50);
        }

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: false);
    }

    /// <summary>
    /// Die Breite, an der die Spalten aufgeteilt werden, wenn eine einzelne festgenagelt wird.
    /// <b>Eine Annahme und keine Messung</b> — die wirkliche Breite kennt erst der Umbruch
    /// (§4.19). Sie ist der Textbereich von A4 mit Standardrändern und damit der Fall, den
    /// jemand meint, der eine Spaltenbreite von Hand einstellt.
    /// </summary>
    private const double AutoBreiteCm = 16;

    /// <summary>
    /// <b>Verbindet die Zelle unter der Marke mit ihrer rechten Nachbarin.</b>
    ///
    /// <para>
    /// <b>Nach rechts und nicht „die Auswahl", und auch das ist gemessen:</b> Die Auswahl des
    /// Editors ist eine Spanne über Absätze (§4.30) und kennt kein Rechteck aus Zellen. Ein
    /// „markierte Zellen verbinden" müsste erst eine zweite Art von Auswahl geben — das ist ein
    /// eigener Handgriff und keine Nebenwirkung dieses hier. <b>Mehrmals gedrückt verbindet es
    /// weiter</b>, und damit ist jede Breite erreichbar.
    /// </para>
    /// <para>
    /// <b>Der Inhalt der Nachbarzelle geht nicht verloren</b>: ihre Blöcke wandern an die
    /// verbundene Zelle. Word wirft sie ebenso zusammen, und alles andere wäre stiller
    /// Datenverlust. <b>Ein leerer Absatz wandert nicht mit</b> — sonst stünde nach jedem
    /// Verbinden eine Leerzeile darin, die niemand eingegeben hat.
    /// </para>
    /// </summary>
    public static TdChange? ZellenVerbinden(TdDocument doc, TdSelection auswahl)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, zeile, spalte, liste, index, gezogen) = lage;

        var neu = Kopie(tabelle);
        if (!InGrenzen(neu, zeile, spalte)) return null;
        if (spalte + 1 >= neu.Rows[zeile].Cells.Count) return null;

        var links = neu.Rows[zeile].Cells[spalte];
        var rechts = neu.Rows[zeile].Cells[spalte + 1];

        links.ColumnSpan = Math.Max(1, links.ColumnSpan) + Math.Max(1, rechts.ColumnSpan);

        if (HatInhalt(rechts)) links.Blocks.AddRange(rechts.Blocks);

        neu.Rows[zeile].Cells.RemoveAt(spalte + 1);

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: false);
    }

    /// <summary>
    /// <b>Teilt die verbundene Zelle unter der Marke wieder auf.</b>
    ///
    /// <para>
    /// Eine Zelle, die nichts überspannt, lässt sich nicht teilen — dann kommt <c>null</c> und
    /// nicht eine zweite, halb so breite Spalte. <b>Teilen ist die Gegenbewegung zum
    /// Verbinden und nicht das Einfügen einer Spalte</b>; dafür gibt es
    /// <see cref="SpalteEinfuegen"/>, und es hieße sonst, dass ein Klick die ganze Tabelle
    /// umbaut statt einer Zelle.
    /// </para>
    /// </summary>
    public static TdChange? ZelleTeilen(TdDocument doc, TdSelection auswahl)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, zeile, spalte, liste, index, gezogen) = lage;

        var neu = Kopie(tabelle);
        if (!InGrenzen(neu, zeile, spalte)) return null;

        var zelle = neu.Rows[zeile].Cells[spalte];
        if (zelle.ColumnSpan <= 1) return null;

        int span = zelle.ColumnSpan;
        zelle.ColumnSpan = 1;

        for (int i = 1; i < span; i++)
            neu.Rows[zeile].Cells.Insert(spalte + i, new TdTableCell(new TdParagraph()));

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: false);
    }

    /// <summary>Steht in dieser Zelle etwas, das beim Verbinden mitkommen muss?</summary>
    private static bool HatInhalt(TdTableCell zelle) =>
        zelle.Blocks.Any(b => b is not TdParagraph absatz || absatz.PlainText().Length > 0);

    /// <summary>Liegt diese Zelle wirklich in dieser Tabelle?</summary>
    private static bool InGrenzen(TdTable tabelle, int zeile, int spalte) =>
        zeile >= 0 && zeile < tabelle.Rows.Count &&
        spalte >= 0 && spalte < tabelle.Rows[zeile].Cells.Count;
}
