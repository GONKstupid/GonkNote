namespace GonkNote.Core.Text;

/// <summary>
/// <b>Tabellen bearbeiten</b> — Zeilen und Spalten einfügen und löschen. Schritt 6, zweite
/// Hälfte (HANDOFF §6).
///
/// <para>
/// <b>Eine Tabellenänderung ist ein Blocktausch und sonst nichts.</b> Sie ersetzt die
/// <see cref="TdTable"/> an ihrem Ort durch eine neue — damit fällt sie unter dieselbe
/// <see cref="TdChange"/> wie jede andere Änderung, und Rückgängig kostet keine Zeile
/// zusätzlich (§4.32). <b>Die alte Tabelle wird dabei nie angefasst</b>: Sie ist die Sicherung.
/// Der Preis ist eine Kopie je Handgriff; sie ist billig, weil die **Absätze in den Zellen
/// weitergereicht und nicht verdoppelt** werden — an ihnen ändert sich nichts.
/// </para>
/// <para>
/// <b>Verbundene Zellen sind der Grund, warum hier mehr steht als „Liste einfügen".</b> Eine
/// Zelle über zwei Spalten (<see cref="TdTableCell.ColumnSpan"/>) belegt zwei Rasterplätze,
/// steht aber einmal in der Zeile; senkrecht verbundene Zellen stehen in jeder Zeile, aber nur
/// die oberste trägt Inhalt (<see cref="TdVerticalMerge"/>, DOCX' Sicht). <b>Gerechnet wird
/// deshalb durchweg in Rasterspalten und nicht in Zellindizes</b> — wer die verwechselt,
/// bekommt bei der ersten unregelmäßigen Tabelle eine Spalte an der falschen Stelle, und das
/// sieht nach einem Zeichenfehler aus.
/// </para>
/// <para>
/// <b>Was hier ausdrücklich nicht steht: Zellen verbinden und trennen.</b> Das ist kein
/// Vergessen, sondern die nächste Runde — es verlangt eine Antwort darauf, was mit dem Inhalt
/// der aufgehenden Zellen geschieht, und ein geratener wäre stiller Datenverlust. Dieselbe
/// Sorte benannte Lücke wie die Tabellengrenze in §4.32.
/// </para>
/// </summary>
public static partial class TdTableEdit
{
    // ---------------------------------------------------------------- Wo steht der Cursor?

    /// <summary>
    /// Die Tabelle, in der die Schreibmarke steht, samt Zeile und Rasterspalte —
    /// <c>null</c>, wenn sie in keiner steht.
    ///
    /// <para>
    /// <b>Die Auskunft, an der der Reiter „Tabelle" hängt.</b> Er ist nur zu sehen, wenn der
    /// Cursor in einer Tabelle steht — genau wie im WPF-Ribbon, und aus demselben Grund: Ein
    /// dauerhaft sichtbarer Reiter, dessen Knöpfe meistens nichts tun können, ist die
    /// ausgegraute Fläche aus §4.28 in groß.
    /// </para>
    /// </summary>
    public static (TdTable Tabelle, int Zeile, int Spalte)? Ort(TdDocument doc, TdPosition stelle)
    {
        var absatz = TdCursor.AbsatzAn(doc, stelle.Paragraph);
        if (absatz is null) return null;

        foreach (var abschnitt in doc.Sections)
            if (Suchen(abschnitt.Blocks, absatz) is { } treffer)
                return treffer;

        return null;
    }

    /// <summary>
    /// Der Block, in dem die Tabelle steht, und ihre Stelle darin — für den Tausch.
    /// <c>null</c>, wenn sie nicht in diesem Dokument steht.
    /// </summary>
    private static (List<TdBlock> Liste, int Index)? Stelle(TdDocument doc, TdTable tabelle)
    {
        foreach (var abschnitt in doc.Sections)
            if (Stelle(abschnitt.Blocks, tabelle) is { } treffer)
                return treffer;

        return null;
    }

    private static (List<TdBlock> Liste, int Index)? Stelle(List<TdBlock> bloecke, TdTable gesucht)
    {
        for (int i = 0; i < bloecke.Count; i++)
        {
            if (ReferenceEquals(bloecke[i], gesucht)) return (bloecke, i);

            if (bloecke[i] is not TdTable tabelle) continue;

            foreach (var zeile in tabelle.Rows)
                foreach (var zelle in zeile.Cells)
                    if (Stelle(zelle.Blocks, gesucht) is { } tiefer)
                        return tiefer;
        }

        return null;
    }

    /// <summary>
    /// Sucht den Absatz in einer Blockliste und meldet, in welcher Tabelle er steht.
    /// <b>Die innerste gewinnt</b> — bei einer Tabelle in einer Zelle (§4.19) meint der Nutzer
    /// die, in der er gerade schreibt.
    /// </summary>
    private static (TdTable, int, int)? Suchen(List<TdBlock> bloecke, TdParagraph gesucht)
    {
        foreach (var block in bloecke)
        {
            if (block is not TdTable tabelle) continue;

            for (int z = 0; z < tabelle.Rows.Count; z++)
            {
                int spalte = 0;

                foreach (var zelle in tabelle.Rows[z].Cells)
                {
                    if (Suchen(zelle.Blocks, gesucht) is { } tiefer) return tiefer;
                    if (Enthaelt(zelle.Blocks, gesucht)) return (tabelle, z, spalte);

                    spalte += Math.Max(1, zelle.ColumnSpan);
                }
            }
        }

        return null;
    }

    private static bool Enthaelt(List<TdBlock> bloecke, TdParagraph gesucht) =>
        bloecke.Any(b => ReferenceEquals(b, gesucht));

    // ---------------------------------------------------------------- Zeilen

    /// <summary>
    /// Eine leere Zeile über oder unter der, in der die Marke steht.
    ///
    /// <para>
    /// <b>Sie bekommt so viele Zellen, wie die Vorlagezeile Rasterspalten belegt</b>, und nicht
    /// so viele, wie diese Zellen hat: Steht dort eine Zelle über zwei Spalten, hätte die neue
    /// Zeile sonst eine Spalte zu wenig, und alles rechts davon rutschte.
    /// </para>
    /// <para>
    /// <b>Kopfzeile wird sie nie.</b> `IsHeader` gilt nur für die **ersten** Zeilen einer
    /// Tabelle (<see cref="TdTableRow.IsHeader"/>); eine geerbte Markierung mitten in der
    /// Tabelle ignoriert Word, und der Umbruch hier wiederholte eine Zeile, die niemand als
    /// Kopf gemeint hat.
    /// </para>
    /// </summary>
    public static TdChange? ZeileEinfuegen(TdDocument doc, TdSelection auswahl, bool darunter)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, zeile, _, liste, index, gezogen) = lage;

        var neu = Kopie(tabelle);
        int wohin = darunter ? zeile + 1 : zeile;

        var frisch = new TdTableRow();
        for (int i = 0; i < tabelle.Rows[zeile].GridSpaltenzahl(); i++)
            frisch.Cells.Add(new TdTableCell(new TdParagraph()));

        neu.Rows.Insert(wohin, frisch);

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: false);
    }

    /// <summary>
    /// Die Zeile löschen, in der die Marke steht. <b>Die letzte Zeile wird nicht gelöscht</b> —
    /// eine Tabelle ohne Zeilen ist nichts, was man noch anklicken könnte; wer die Tabelle
    /// loswerden will, nimmt <see cref="TabelleLoeschen"/>.
    /// </summary>
    public static TdChange? ZeileLoeschen(TdDocument doc, TdSelection auswahl)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, zeile, _, liste, index, gezogen) = lage;

        if (tabelle.Rows.Count <= 1) return null;

        var neu = Kopie(tabelle);
        neu.Rows.RemoveAt(zeile);

        // **Die Marke muss weg von hier**: Der Absatz, in dem sie steht, ist gerade gelöscht
        // worden — eine stehengebliebene Stelle zeigte auf eine Zelle, die es nicht mehr gibt.
        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: true);
    }

    // ---------------------------------------------------------------- Spalten

    /// <summary>
    /// Eine leere Spalte links oder rechts von der, in der die Marke steht.
    ///
    /// <para>
    /// <b>Eingefügt wird an einer Rasterspalte, und deshalb muss jede Zeile einzeln gefragt
    /// werden, welche ihrer Zellen dort sitzt.</b> Trifft die Stelle **mitten** in eine über
    /// mehrere Spalten verbundene Zelle, wird diese um eine Spalte **breiter**, statt
    /// zerschnitten zu werden — zerschneiden hieße entscheiden, welche Hälfte den Inhalt behält.
    /// </para>
    /// </summary>
    public static TdChange? SpalteEinfuegen(TdDocument doc, TdSelection auswahl, bool rechts)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, zeile, spalte, liste, index, gezogen) = lage;

        int wohin = rechts ? spalte + Breite(tabelle.Rows[zeile], spalte) : spalte;

        var neu = Kopie(tabelle);
        foreach (var reihe in neu.Rows) SpalteEinsetzen(reihe, wohin);

        if (wohin <= neu.ColumnWidthsCm.Count) neu.ColumnWidthsCm.Insert(wohin, 0);

        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: false);
    }

    /// <summary>
    /// Die Spalte löschen, in der die Marke steht. <b>Die letzte Spalte wird nicht gelöscht</b> —
    /// siehe <see cref="ZeileLoeschen"/>.
    /// </summary>
    public static TdChange? SpalteLoeschen(TdDocument doc, TdSelection auswahl)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, _, spalte, liste, index, gezogen) = lage;

        if (tabelle.Spaltenzahl() <= 1) return null;

        var neu = Kopie(tabelle);
        foreach (var reihe in neu.Rows) SpalteEntfernen(reihe, spalte);

        if (spalte < neu.ColumnWidthsCm.Count) neu.ColumnWidthsCm.RemoveAt(spalte);

        // <inheritdoc cref="ZeileLoeschen"/> — dieselbe Lage, andere Richtung.
        return Tausch(doc, liste, index, tabelle, neu, gezogen, markeNeu: true);
    }

    // ---------------------------------------------------------------- Die ganze Tabelle

    /// <summary>
    /// Die Tabelle löschen, in der die Marke steht — <b>und einen leeren Absatz an ihre
    /// Stelle setzen</b>.
    ///
    /// <para>
    /// **Nicht ersatzlos:** War die Tabelle der einzige Block eines Abschnitts, bliebe er sonst
    /// ohne einen Ort, an dem der Cursor stehen kann — dieselbe Überlegung wie bei
    /// <see cref="TdDocument.Leer"/>.
    /// </para>
    /// </summary>
    public static TdChange? TabelleLoeschen(TdDocument doc, TdSelection auswahl)
    {
        if (Aufsetzen(doc, auswahl) is not { } lage) return null;
        var (tabelle, _, _, liste, index, gezogen) = lage;

        var ersatz = new TdParagraph();

        // Der Ersatzabsatz steht genau dort, wo der erste Absatz der Tabelle stand — die
        // Absätze davor sind unberührt, also stimmt die Zahl vor wie nach dem Tausch.
        return new TdChange(
            liste, index, [tabelle], [ersatz],
            gezogen, new TdSelection(new TdPosition(ErsterAbsatzIndex(doc, tabelle), 0, 0)),
            TdEditArt.Struktur, schliesstGruppe: true);
    }

    // ---------------------------------------------------------------- Kleinteile

    /// <summary>
    /// Alles, was jeder Handgriff zuerst braucht: geradegezogene Auswahl, die Tabelle, ihre
    /// Stelle im Dokument. <c>null</c>, wenn die Marke in keiner Tabelle steht.
    /// </summary>
    private static (TdTable Tabelle, int Zeile, int Spalte,
                    List<TdBlock> Liste, int Index, TdSelection Gezogen)?
        Aufsetzen(TdDocument doc, TdSelection auswahl)
    {
        var gezogen = TdCursor.Normalisieren(doc, auswahl);

        if (Ort(doc, gezogen.Focus) is not { } ort) return null;
        if (Stelle(doc, ort.Tabelle) is not { } stelle) return null;

        return (ort.Tabelle, ort.Zeile, ort.Spalte, stelle.Liste, stelle.Index, gezogen);
    }

    /// <summary>
    /// Der Tausch selbst. <b>Die Marke bleibt stehen, wo sie war</b>, solange die Zelle unter
    /// ihr noch da ist — beim Löschen einer Zeile oder Spalte ist sie das nicht, dann kommt sie
    /// an den Anfang der Tabelle.
    /// </summary>
    private static TdChange Tausch(
        TdDocument doc, List<TdBlock> liste, int index, TdTable alt, TdTable neu,
        TdSelection gezogen, bool markeNeu)
    {
        var nachher = markeNeu
            ? new TdSelection(new TdPosition(ErsterAbsatzIndex(doc, alt), 0, 0))
            : gezogen;

        return new TdChange(
            liste, index, [alt], [neu], gezogen, nachher,
            TdEditArt.Struktur, schliesstGruppe: true);
    }

    /// <summary>
    /// Eine Tabelle mit neuen Zeilen und Zellen, aber **denselben Absätzen darin**. Am Inhalt
    /// ändert sich nichts; ihn zu verdoppeln kostete nur Speicher und machte jede
    /// Verlaufsprüfung auf Objektgleichheit blind.
    /// </summary>
    private static TdTable Kopie(TdTable vorlage)
    {
        var neu = new TdTable
        {
            // **Das Format wird kopiert und nicht weitergereicht** (§4.90). Es ist eine Klasse:
            // Bis zum Tabellenentwurf fasste kein Handgriff es an, und die geteilte Referenz
            // fiel nicht auf. Sobald einer es anfasst, aendert er die Sicherung im Verlauf mit,
            // und das erste Strg+Z bringt nichts zurueck -- genau der Fehler aus §4.32. Ein
            // Waechter hat ihn gefunden, bevor die Oberflaeche dazu gebaut war.
            Format = vorlage.Format.Kopie(),
            ColumnWidthsCm = [.. vorlage.ColumnWidthsCm],
        };

        foreach (var zeile in vorlage.Rows)
        {
            var reihe = new TdTableRow { IsHeader = zeile.IsHeader, MinHeightCm = zeile.MinHeightCm };

            foreach (var zelle in zeile.Cells)
                reihe.Cells.Add(new TdTableCell
                {
                    Blocks = [.. zelle.Blocks],
                    ColumnSpan = zelle.ColumnSpan,
                    VerticalMerge = zelle.VerticalMerge,
                    Shading = zelle.Shading,
                    VerticalAlign = zelle.VerticalAlign,
                });

            neu.Rows.Add(reihe);
        }

        return neu;
    }

    /// <summary>Wie breit die Zelle ist, die an dieser Rasterspalte beginnt oder sie überdeckt.</summary>
    private static int Breite(TdTableRow zeile, int spalte)
    {
        int gelaufen = 0;

        foreach (var zelle in zeile.Cells)
        {
            int breite = Math.Max(1, zelle.ColumnSpan);
            if (spalte < gelaufen + breite) return breite - (spalte - gelaufen);
            gelaufen += breite;
        }

        return 1;
    }

    /// <summary>
    /// Setzt in dieser Zeile eine leere Zelle an der Rasterspalte ein.
    /// <inheritdoc cref="SpalteEinfuegen" path="/summary/para[1]"/>
    /// </summary>
    private static void SpalteEinsetzen(TdTableRow zeile, int spalte)
    {
        int gelaufen = 0;

        for (int i = 0; i < zeile.Cells.Count; i++)
        {
            int breite = Math.Max(1, zeile.Cells[i].ColumnSpan);

            if (spalte == gelaufen)
            {
                zeile.Cells.Insert(i, new TdTableCell(new TdParagraph()));
                return;
            }

            // Mitten in einer verbundenen Zelle: sie wird breiter statt zerschnitten.
            if (spalte < gelaufen + breite)
            {
                zeile.Cells[i].ColumnSpan = breite + 1;
                return;
            }

            gelaufen += breite;
        }

        zeile.Cells.Add(new TdTableCell(new TdParagraph()));
    }

    /// <summary>
    /// Nimmt in dieser Zeile die Rasterspalte heraus. Eine verbundene Zelle wird dabei
    /// **schmaler**; erst wenn sie auf null schrumpfte, verschwindet sie ganz.
    /// </summary>
    private static void SpalteEntfernen(TdTableRow zeile, int spalte)
    {
        int gelaufen = 0;

        for (int i = 0; i < zeile.Cells.Count; i++)
        {
            int breite = Math.Max(1, zeile.Cells[i].ColumnSpan);

            if (spalte < gelaufen + breite)
            {
                if (breite <= 1) zeile.Cells.RemoveAt(i);
                else zeile.Cells[i].ColumnSpan = breite - 1;
                return;
            }

            gelaufen += breite;
        }
    }

    /// <summary>
    /// Der wievielte Absatz des Dokuments der **erste in dieser Tabelle** ist.
    ///
    /// <para>
    /// <b>Er ist zugleich die Stelle, an die die Marke nach jedem Löschen kommt</b>, und
    /// zugleich die des Ersatzabsatzes, wenn die ganze Tabelle verschwindet: Was **vor** der
    /// Tabelle steht, ändert keiner dieser Handgriffe an, also gilt die Zahl vor und nach dem
    /// Tausch. Gezählt wird über <see cref="TdDocument.Paragraphs"/> — <b>denselben Durchlauf</b>,
    /// dem alle Stellen folgen (§4.30); eine eigene Zählung hier wäre die zweite, die von der
    /// ersten abweicht.
    /// </para>
    /// </summary>
    private static int ErsterAbsatzIndex(TdDocument doc, TdTable tabelle)
    {
        var drin = new HashSet<TdParagraph>(TdBlockEdit.Absaetze(tabelle));

        int i = 0;
        foreach (var absatz in doc.Paragraphs())
        {
            if (drin.Contains(absatz)) return i;
            i++;
        }

        return 0;
    }
}
