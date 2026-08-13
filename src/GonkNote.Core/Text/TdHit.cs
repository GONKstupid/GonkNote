namespace GonkNote.Core.Text;

// ==================== Was auf dem Papier zu markieren ist ====================

/// <summary>Ein Stück einer Zeile, von wo bis wo — in Zentimetern ab dem Behälterrand.</summary>
public readonly record struct TdSpanne(double VonCm, double BisCm)
{
    public double BreiteCm => Math.Max(0, BisCm - VonCm);
}

/// <summary>
/// Wo die Schreibmarke steht, **in Seitenkoordinaten** — auf welchem Blatt und wo darauf,
/// gezählt von der oberen linken Ecke des Papiers.
///
/// <para>
/// <b>Das ist die andere Sicht auf dieselbe Marke als in <see cref="TdMarkierung"/></b>, und
/// beide werden gebraucht: Der Zeichner will sie in Zeilenkoordinaten, weil er ohnehin Zeile
/// für Zeile arbeitet. Wer sie **ins Bild rollen** will, wenn der Cursor unten aus der Seite
/// läuft, braucht das Blatt und den Ort darauf — und das ist keine Zeichenfrage.
/// </para>
/// </summary>
/// <param name="Seite">Der Index in <see cref="TdLayoutResult.Pages"/>.</param>
public readonly record struct TdCaret(int Seite, double XCm, double YCm, double HoeheCm);

/// <summary>
/// Wo die Auswahl liegt und wo die Schreibmarke steht — **je gesetzter Zeile**.
///
/// <para>
/// <b>Warum je Zeile und nicht in Seitenkoordinaten.</b> Der Zeichner arbeitet ohnehin Zeile
/// für Zeile, und zwar in einer Leinwand, deren Nullpunkt schon der Behälter ist — die Seite
/// oder eine Tabellenzelle (§4.19). Bekäme er absolute Kästen, müsste er sie zurückrechnen;
/// bekommt er sie je Zeile, malt er den Kasten **vor** den Buchstaben dieser Zeile und die
/// Marke danach. Damit liegt die Auswahl **hinter** dem Text und **über** dem Zellhintergrund,
/// ohne dass jemand die Reihenfolge der Seite umbauen muss.
/// </para>
/// <para>
/// <b>Die Zeile ist der Schlüssel, und zwar als Objekt.</b> Zwei Zeilen mit demselben Text
/// sind zwei Zeilen; verglichen wird deshalb, was <see cref="TdLine.Source"/> schon immer so
/// vergleicht — die Identität.
/// </para>
/// </summary>
public sealed class TdMarkierung
{
    /// <summary>Je Zeile das Stück, das ausgewählt ist. Zeilen ohne Eintrag sind nicht markiert.</summary>
    public Dictionary<TdLine, TdSpanne> Auswahl { get; } = [];

    /// <summary>Die Zeile, in der die Schreibmarke steht — <c>null</c>, wenn sie nirgends steht.</summary>
    public TdLine? MarkeZeile { get; set; }

    /// <summary>Wo die Schreibmarke in dieser Zeile steht, in Zentimetern ab dem Behälterrand.</summary>
    public double MarkeXCm { get; set; }
}

// ==================== Die Rechnung ====================

/// <summary>
/// Vom Papier ins Modell und zurück — Schritt 4 des Schreibens (HANDOFF §6).
///
/// <para>
/// <b>Zwei Fragen, eine Rechnung.</b> „Welche Stelle gehört zu diesem Mausklick?"
/// (<see cref="StelleAn"/>) und „wo auf dem Papier steht diese Stelle?"
/// (<see cref="Markieren"/>) sind dieselbe Zuordnung in beide Richtungen; sie stehen deshalb
/// nebeneinander und teilen sich, wie eine Zeile gefunden wird. Getrennt gebaut wären es zwei
/// Rechnungen, die auseinanderlaufen können — und der Fehler sähe aus wie ein Cursor, der beim
/// Klicken woanders landet, als er danach blinkt.
/// </para>
///
/// <para>
/// <b>Sie rechnet nichts nach, was der Umbruch schon weiß.</b> Jedes gesetzte Stück trägt seit
/// Schritt 4 seine Stelle im Absatz (<see cref="TdLaidOutRun.Linear"/>), jede Zeile ihren
/// Anfang (<see cref="TdLine.Linear"/>). Was hier bleibt, ist: die Zeile unter dem Zeiger
/// finden, das Stück darin, und das Zeichen darin messen.
/// </para>
///
/// <para>
/// <b>Zwei Sorten Zeilen stehen nicht im Dokument und sind deshalb ausgenommen:</b> ein
/// gerechneter Inhaltsverzeichnis-Eintrag (<see cref="TdLine.IsTocEntry"/>, §4.20) und eine
/// wiederholte Tabellenkopfzeile (<see cref="TdLaidOutRow.IsRepeatedHeader"/>, §4.19). Wer in
/// sie hineinklickt, landet nicht in ihnen — im ersten Fall gibt es nichts zu bearbeiten, im
/// zweiten steht die Zeile ein paar Seiten weiter vorn.
/// </para>
/// </summary>
public static class TdHit
{
    /// <summary>
    /// Die Stelle im Modell, die zu einem Punkt auf dem Papier gehört.
    ///
    /// <para>
    /// <paramref name="xCm"/> und <paramref name="yCm"/> zählen von der **oberen linken Ecke
    /// des Papiers**, nicht des Textbereichs — genau wie der Nullpunkt der Leinwand in
    /// <c>TdRenderer.Seite</c>. Wer sie anders zählte, träfe um die Seitenränder daneben.
    /// </para>
    /// <para>
    /// <b>Es gibt immer eine Antwort.</b> Ein Klick neben den Text, unter die letzte Zeile oder
    /// auf den Rand liefert die nächstgelegene Stelle und nicht <c>null</c>: Der Nutzer hat ins
    /// Dokument geklickt, und ein Cursor, der dabei stehen bleibt, sieht aus wie ein hängendes
    /// Programm. Nur ein Dokument ohne jede setzbare Zeile liefert den Nullpunkt.
    /// </para>
    /// </summary>
    public static TdPosition StelleAn(
        TdLayoutResult umbruch, TdDocument doc, ITdTextMeasure messung,
        int seite, double xCm, double yCm)
    {
        if (seite < 0 || seite >= umbruch.Pages.Count) return TdPosition.Null;

        var blatt = umbruch.Pages[seite];
        double x = xCm - blatt.Setup.MarginLeftCm;
        double y = yCm - blatt.Setup.MarginTopCm;

        var treffer = ZeileAn(blatt, x, y);
        if (treffer is not { } t) return TdPosition.Null;

        return StelleInZeile(doc, messung, t.Zeile, x - t.XVersatzCm);
    }

    /// <summary>
    /// Wo eine Auswahl auf dem Papier liegt und wo ihre Schreibmarke steht.
    ///
    /// <para>
    /// <b>Die Marke steht auf der Spitze</b> (<see cref="TdSelection.Focus"/>, §4.30) — dort,
    /// wo der Nutzer zuletzt war, und nicht am Anfang der Auswahl.
    /// </para>
    /// </summary>
    public static TdMarkierung Markieren(
        TdLayoutResult umbruch, TdDocument doc, ITdTextMeasure messung, TdSelection auswahl)
    {
        var markierung = new TdMarkierung();
        var gezogen = TdCursor.Normalisieren(doc, auswahl);

        if (ZeileZu(umbruch, doc, gezogen.Focus) is { } marke)
        {
            markierung.MarkeZeile = marke.Zeile;
            markierung.MarkeXCm = XVon(messung, marke.Zeile, marke.Linear);
        }

        if (gezogen.IsEmpty) return markierung;

        var von = gezogen.Start;
        var bis = gezogen.End;

        foreach (var zeile in AlleZeilen(umbruch))
        {
            if (zeile.Source is not { } absatz) continue;
            if (zeile.IsTocEntry) continue;

            int absatzIndex = TdCursor.IndexVon(doc, absatz);
            if (absatzIndex < 0) continue;
            if (absatzIndex < von.Paragraph || absatzIndex > bis.Paragraph) continue;

            int anfang = zeile.Linear;
            int ende = ZeilenEnde(zeile);

            // Wie weit die Auswahl in **diese** Zeile hineinreicht.
            int linksLinear = absatzIndex == von.Paragraph
                ? Math.Max(anfang, TdCursor.Linear(absatz, von))
                : anfang;

            int rechtsLinear = absatzIndex == bis.Paragraph
                ? Math.Min(ende, TdCursor.Linear(absatz, bis))
                : ende;

            if (rechtsLinear < linksLinear) continue;

            // Eine leere Zeile mitten in der Auswahl bekommt trotzdem einen Kasten — sonst
            // sähe ein ausgewählter Leerabsatz aus, als wäre er nicht mit dabei.
            if (rechtsLinear == linksLinear && !(anfang == ende && ImInneren(absatzIndex, von, bis)))
                continue;

            double links = XVon(messung, zeile, linksLinear);
            double rechts = rechtsLinear == linksLinear
                ? links + LeereBreiteCm(messung, zeile)
                : XVon(messung, zeile, rechtsLinear);

            markierung.Auswahl[zeile] = new TdSpanne(links, rechts);
        }

        return markierung;
    }

    /// <summary>
    /// Wo die Schreibmarke auf dem Papier steht — <c>null</c>, wenn die Stelle auf keiner Seite
    /// gesetzt ist.
    ///
    /// <para>
    /// Die Zahlen zählen von der oberen linken Ecke des Papiers, **wie bei
    /// <see cref="StelleAn"/>** — die beiden sind die zwei Richtungen derselben Zuordnung, und
    /// zwei verschiedene Nullpunkte wären die erste Stelle, an der sie auseinanderlaufen.
    /// </para>
    /// </summary>
    public static TdCaret? Schreibmarke(
        TdLayoutResult umbruch, TdDocument doc, ITdTextMeasure messung, TdPosition stelle)
    {
        var gezogen = TdCursor.Normalisieren(doc, stelle);
        if (ZeileZu(umbruch, doc, gezogen) is not { } marke) return null;

        for (int s = 0; s < umbruch.Pages.Count; s++)
        {
            var blatt = umbruch.Pages[s];

            foreach (var t in ZeilenDerSeite(blatt))
            {
                if (!ReferenceEquals(t.Zeile, marke.Zeile)) continue;

                return new TdCaret(
                    s,
                    blatt.Setup.MarginLeftCm + t.XVersatzCm + XVon(messung, marke.Zeile, marke.Linear),
                    blatt.Setup.MarginTopCm + t.YVersatzCm + marke.Zeile.YCm,
                    marke.Zeile.HeightCm);
            }
        }

        return null;
    }

    /// <summary>Liegt dieser Absatz **zwischen** den Enden der Auswahl, also ganz darin?</summary>
    private static bool ImInneren(int absatzIndex, TdPosition von, TdPosition bis) =>
        absatzIndex > von.Paragraph && absatzIndex < bis.Paragraph;

    /// <summary>
    /// Die Breite, die eine ausgewählte **leere** Zeile bekommt. Ein halbes Zeichen ist genug,
    /// um zu zeigen „dieser Absatz gehört dazu" — so hält es jeder Editor.
    /// </summary>
    private static double LeereBreiteCm(ITdTextMeasure messung, TdLine zeile)
    {
        var format = zeile.Runs.Count > 0
            ? zeile.Runs[0].Format
            : zeile.Source?.CharFormat.Aufgeloest() ?? TdCharFormat.Standard;

        return messung.WidthCm(" ", format);
    }

    // ---------------------------------------------------------------- Zeilen finden

    /// <summary>
    /// Eine Zeile samt dem Versatz ihres Behälters (bei einer Tabellenzelle) und dessen
    /// waagerechter Ausdehnung.
    /// </summary>
    /// <param name="VonCm">Linke Kante des Behälters — die Spalte, nicht der Text darin.</param>
    /// <param name="BisCm">Rechte Kante des Behälters.</param>
    private readonly record struct Treffer(
        TdLine Zeile, double XVersatzCm, double YVersatzCm, double VonCm, double BisCm);

    /// <summary>
    /// Die Zeile, die zu einem Punkt im Textbereich gehört — **die nächstgelegene**, wenn
    /// keine getroffen ist.
    ///
    /// <para>
    /// <b>Erst die Höhe, dann die Breite.</b> Gewöhnliche Zeilen stehen untereinander; für sie
    /// entscheidet die Höhe allein, und wer neben eine Zeile klickt, meint ihren Anfang oder
    /// ihr Ende. **Tabellenzellen stehen aber nebeneinander** — dort liegen mehrere Zeilen in
    /// derselben Höhe, und ohne die zweite Frage landete jeder Klick in der ersten Spalte.
    /// </para>
    /// <para>
    /// <b>Beim Erproben gefunden, und der Rückweg-Wächter sah es nicht:</b> Beide Richtungen
    /// waren gleich falsch, also fand jede Stelle weiterhin zu sich selbst — nur eben in der
    /// falschen Spalte.
    /// </para>
    /// </summary>
    private static Treffer? ZeileAn(TdPage seite, double x, double y)
    {
        Treffer? beste = null;
        double bestSenkrecht = double.MaxValue;
        double bestWaagerecht = double.MaxValue;

        foreach (var t in ZeilenDerSeite(seite))
        {
            double oben = t.Zeile.YCm + t.YVersatzCm;
            double unten = oben + t.Zeile.HeightCm;

            double senkrecht = y < oben ? oben - y : y >= unten ? y - unten : 0;
            double waagerecht = x < t.VonCm ? t.VonCm - x : x >= t.BisCm ? x - t.BisCm : 0;

            if (senkrecht > bestSenkrecht) continue;
            if (senkrecht == bestSenkrecht && waagerecht >= bestWaagerecht) continue;

            bestSenkrecht = senkrecht;
            bestWaagerecht = waagerecht;
            beste = t;
        }

        return beste;
    }

    /// <summary>Alle Zeilen einer Seite, samt Versatz — Tabellenzellen eingeschlossen.</summary>
    private static IEnumerable<Treffer> ZeilenDerSeite(TdPage seite)
    {
        foreach (var zeile in seite.Lines)
            if (!zeile.IsTocEntry)
                yield return new Treffer(zeile, 0, 0, 0, seite.Setup.TextBreiteCm);

        foreach (var reihe in seite.TableRows)
        {
            // Eine wiederholte Kopfzeile steht im Modell nur einmal; ein Klick in sie führte
            // ein paar Seiten zurück — verwirrender als gar keine Antwort (§4.19).
            if (reihe.IsRepeatedHeader) continue;

            var format = reihe.Table?.Format ?? new TdTableFormat();

            foreach (var zelle in reihe.Cells)
                foreach (var zeile in zelle.Lines)
                {
                    if (zeile.IsTocEntry) continue;

                    yield return new Treffer(
                        zeile,
                        zelle.XCm + format.CellPaddingLeftCm,
                        reihe.YCm + format.CellPaddingTopCm,
                        zelle.XCm,
                        zelle.XCm + zelle.WidthCm);
                }
        }
    }

    /// <summary>Alle Zeilen des ganzen Umbruchs, in Dokumentreihenfolge.</summary>
    private static IEnumerable<TdLine> AlleZeilen(TdLayoutResult umbruch)
    {
        foreach (var seite in umbruch.Pages)
            foreach (var t in ZeilenDerSeite(seite))
                yield return t.Zeile;
    }

    /// <summary>
    /// Die Zeile, in der eine Stelle steht, samt der Stelle selbst.
    /// <para>
    /// Gesucht wird die **erste** Zeile des Absatzes, deren Bereich die Stelle enthält. Eine
    /// Stelle, die genau auf der Grenze sitzt, gehört damit ans **Ende** der früheren Zeile —
    /// dieselbe Regel wie bei der Stückgrenze (§4.30), und aus demselben Grund: Es muss eine
    /// von beiden gelten, sonst blinkt die Marke mal hier und mal dort.
    /// </para>
    /// </summary>
    private static (TdLine Zeile, int Linear)? ZeileZu(
        TdLayoutResult umbruch, TdDocument doc, TdPosition stelle)
    {
        var absatz = TdCursor.AbsatzAn(doc, stelle.Paragraph);
        if (absatz is null) return null;

        int gesucht = TdCursor.Linear(absatz, stelle);

        TdLine? letzte = null;
        foreach (var zeile in AlleZeilen(umbruch))
        {
            if (!ReferenceEquals(zeile.Source, absatz) || zeile.IsTocEntry) continue;

            if (gesucht >= zeile.Linear && gesucht <= ZeilenEnde(zeile))
                return (zeile, gesucht);

            letzte = zeile;
        }

        // Die Stelle liegt in einer Lücke — im Leerraum, den der Umbruch am Zeilenende
        // weggeworfen hat. Dann gehört sie ans Ende der letzten Zeile davor.
        return letzte is null ? null : (letzte, ZeilenEnde(letzte));
    }

    /// <summary>Wo eine Zeile im Absatz endet — der Anfang, wenn sie leer ist.</summary>
    private static int ZeilenEnde(TdLine zeile)
    {
        int ende = zeile.Linear;

        foreach (var lauf in zeile.Runs)
            if (lauf.Linear >= 0)
                ende = Math.Max(ende, lauf.Linear + lauf.Schritte);

        return ende;
    }

    // ---------------------------------------------------------------- Innerhalb einer Zeile

    /// <summary>Wo eine Stelle innerhalb ihrer Zeile steht, in Zentimetern ab dem Behälterrand.</summary>
    private static double XVon(ITdTextMeasure messung, TdLine zeile, int linear)
    {
        if (zeile.Runs.Count == 0) return zeile.StartXCm;

        foreach (var lauf in zeile.Runs)
        {
            if (lauf.Linear < 0) continue;

            int innen = linear - lauf.Linear;
            if (innen < 0) return lauf.XCm;                 // vor diesem Stück, also davor
            if (innen > lauf.Schritte) continue;            // noch weiter rechts

            // Ein Feld und ein Bild sind unteilbar: davor oder dahinter, nichts dazwischen.
            if (lauf.Field is not null || lauf.Graphic is not null)
                return innen == 0 ? lauf.XCm : lauf.XCm + lauf.WidthCm;

            if (innen >= lauf.Text.Length) return lauf.XCm + lauf.WidthCm;

            return lauf.XCm + messung.WidthCm(lauf.Text[..innen], lauf.Format);
        }

        var letzter = zeile.Runs[^1];
        return letzter.XCm + letzter.WidthCm;
    }

    /// <summary>
    /// Welche Stelle in dieser Zeile bei <paramref name="x"/> steht.
    /// <para>
    /// <b>Gemessen wird zeichenweise und auf die *Mitte* geprüft</b>, nicht auf die Kante: Wer
    /// auf die rechte Hälfte eines Buchstabens klickt, will dahinter stehen. Ohne das läge der
    /// Cursor bei jedem Klick um ein Zeichen zu weit links, und niemand könnte hinter das
    /// letzte Zeichen einer Zeile zeigen.
    /// </para>
    /// </summary>
    private static TdPosition StelleInZeile(
        TdDocument doc, ITdTextMeasure messung, TdLine zeile, double x)
    {
        if (zeile.Source is not { } absatz) return TdPosition.Null;

        int absatzIndex = TdCursor.IndexVon(doc, absatz);
        if (absatzIndex < 0) return TdPosition.Null;

        return TdCursor.AusLinear(absatz, absatzIndex, LinearAn(messung, zeile, x));
    }

    /// <inheritdoc cref="StelleInZeile"/>
    private static int LinearAn(ITdTextMeasure messung, TdLine zeile, double x)
    {
        if (zeile.Runs.Count == 0) return zeile.Linear;

        foreach (var lauf in zeile.Runs)
        {
            if (lauf.Linear < 0) continue;
            if (x >= lauf.XCm + lauf.WidthCm) continue;     // ganz rechts von diesem Stück

            if (x <= lauf.XCm) return lauf.Linear;          // links davon: davor

            if (lauf.Field is not null || lauf.Graphic is not null)
                return x < lauf.XCm + lauf.WidthCm / 2 ? lauf.Linear : lauf.Linear + 1;

            double innen = x - lauf.XCm;
            double vorher = 0;

            for (int i = 1; i <= lauf.Text.Length; i++)
            {
                double bis = messung.WidthCm(lauf.Text[..i], lauf.Format);
                if (innen < (vorher + bis) / 2) return lauf.Linear + i - 1;
                vorher = bis;
            }

            return lauf.Linear + lauf.Text.Length;
        }

        // Rechts von allem: ans Zeilenende. Das ist die Antwort auf jeden Klick in den
        // leeren Bereich rechts neben dem Text.
        return ZeilenEnde(zeile);
    }
}
