namespace GonkNote.Core.Text;

/// <summary>Ein Stück Text, fertig gesetzt: mit Ort und Breite.</summary>
/// <param name="Text">Der Text dieses Stücks — schon ohne den Leerraum am Zeilenende.</param>
/// <param name="Format">Das **aufgelöste** Zeichenformat; der Zeichner muss nichts mehr kaskadieren.</param>
/// <param name="XCm">Abstand vom linken Rand des Textbereichs.</param>
/// <param name="WidthCm">Breite des Stücks.</param>
public sealed record TdLaidOutRun(string Text, TdCharFormat Format, double XCm, double WidthCm);

/// <summary>Eine gesetzte Zeile.</summary>
public sealed class TdLine
{
    public List<TdLaidOutRun> Runs { get; } = new();

    /// <summary>Oberkante, gemessen von der Oberkante des Textbereichs.</summary>
    public double YCm { get; set; }

    /// <summary>Gesamthöhe inklusive Zeilenabstand.</summary>
    public double HeightCm { get; set; }

    /// <summary>Grundlinie, gemessen von der Oberkante **dieser Zeile**.</summary>
    public double BaselineCm { get; set; }

    /// <summary>Der Absatz, aus dem diese Zeile stammt — für Auswahl und Cursor.</summary>
    public TdParagraph? Source { get; set; }

    /// <summary>
    /// Die Aufzählungsmarke, falls dies die **erste** Zeile eines Listenpunkts ist. Sie steht
    /// links vom Text (negatives <c>XCm</c>) und gehört nicht zu <see cref="Runs"/>: sie ist
    /// nicht Teil des Textes, lässt sich nicht auswählen und wird beim Kopieren nicht
    /// mitgenommen.
    /// </summary>
    public TdLaidOutRun? Marker { get; set; }

    public string PlainText() => string.Concat(Runs.Select(r => r.Text));
}

/// <summary>Eine gesetzte Tabellenzelle.</summary>
public sealed class TdLaidOutCell
{
    /// <summary>Abstand vom linken Rand des Textbereichs.</summary>
    public double XCm { get; set; }

    public double WidthCm { get; set; }

    /// <summary>
    /// Die Zeilen des Zellinhalts. Ihr <c>YCm</c> zählt ab der **Oberkante der Zelle**,
    /// nicht ab der Seite — der Zeichner setzt Zeile und Zelle zusammen.
    /// </summary>
    public List<TdLine> Lines { get; } = new();

    /// <summary>Die Zelle aus dem Modell — für Hintergrund, Rahmen und Auswahl.</summary>
    public TdTableCell? Source { get; set; }

    /// <summary>
    /// Über wie viele **Zeilen** diese Zelle reicht. 1 im Normalfall; bei einer senkrechten
    /// Verbindung so viele, wie Fortsetzungen folgen. Fortsetzungszellen selbst kommen im
    /// Ergebnis nicht vor — sie haben keinen Ort und keinen Inhalt.
    /// </summary>
    public int RowSpan { get; set; } = 1;
}

/// <summary>Eine gesetzte Tabellenzeile.</summary>
public sealed class TdLaidOutRow
{
    /// <summary>Oberkante, gemessen von der Oberkante des Textbereichs.</summary>
    public double YCm { get; set; }

    public double HeightCm { get; set; }

    public List<TdLaidOutCell> Cells { get; } = new();

    /// <summary>Die Tabelle, zu der diese Zeile gehört — für Rahmen und Auswahl.</summary>
    public TdTable? Table { get; set; }

    public TdTableRow? Source { get; set; }

    /// <summary>
    /// Ist das eine **wiederholte** Kopfzeile? Sie steht im Modell nur einmal, auf jeder
    /// Folgeseite aber erneut auf dem Papier — wer sie beim Zurückrechnen auf den Text
    /// mitzählt, findet den Cursor an der falschen Stelle.
    /// </summary>
    public bool IsRepeatedHeader { get; set; }
}

/// <summary>Eine gesetzte Seite.</summary>
public sealed class TdPage
{
    /// <summary>Beginnt bei 1 — die Zahl, die in <c>{SEITE}</c> steht.</summary>
    public int Number { get; set; }

    /// <summary>Die Einrichtung des Abschnitts, zu dem diese Seite gehört.</summary>
    public TdPageSetup Setup { get; set; } = TdPageSetup.A4;

    public List<TdLine> Lines { get; } = new();

    /// <summary>
    /// Die Tabellenzeilen dieser Seite.
    /// <para>
    /// <b>Eine zweite Liste neben <see cref="Lines"/> und keine gemeinsame:</b> eine
    /// Tabellenzeile ist keine Zeile — sie hat Zellen, und deren Inhalt sind wieder Zeilen.
    /// Beide tragen ihr <c>YCm</c>; der Zeichner geht sie in dieser Reihenfolge durch und
    /// braucht nichts weiter zu wissen.
    /// </para>
    /// </summary>
    public List<TdLaidOutRow> TableRows { get; } = new();
}

/// <summary>Das Ergebnis eines Umbruchs.</summary>
public sealed class TdLayoutResult
{
    public List<TdPage> Pages { get; } = new();

    /// <summary>Die Zahl, die in <c>{SEITEN}</c> steht.</summary>
    public int PageCount => Pages.Count;
}

/// <summary>
/// Zeilen- und Seitenumbruch — die zweite Hälfte von Phase 4, Schritt 2.
///
/// <para>
/// <b>Reine Rechnung.</b> Nichts hier zeichnet, und nichts hier kennt eine Oberfläche: aus
/// einem <see cref="TdDocument"/> und einer Schriftmessung werden Seiten mit Zeilen mit
/// Stücken, jedes mit Ort und Maß in Zentimetern. Was daraus Pixel macht, ist Sache des
/// Kopfes — nach der Faustregel aus HANDOFF §3 steht deshalb alles davon hier.
/// </para>
///
/// <para>
/// <b>Zentimeter und keine Pixel</b>, durchgehend. Das Dokumentmodell rechnet so, DOCX
/// rechnet so, und der Zoomfaktor gehört an die Stelle, die zeichnet — nicht an die, die
/// umbricht. Ein Umbruch in Pixeln müsste bei jeder Zoomstufe neu laufen und brächte bei
/// jeder ein leicht anderes Ergebnis.
/// </para>
/// </summary>
public static class TdLayout
{
    /// <summary>
    /// Bricht das ganze Dokument um.
    /// <para>
    /// <b>Jeder Abschnitt beginnt auf einer neuen Seite.</b> Das ist DOCX' Vorgabe für
    /// <c>sectPr</c> ohne eigene Angabe („nextPage") und der einzige Fall, den das Modell
    /// heute kennt — ein fortlaufender Abschnittswechsel mitten auf der Seite wäre eine
    /// eigene Angabe, und die gibt es noch nicht.
    /// </para>
    /// </summary>
    public static TdLayoutResult Umbrechen(TdDocument doc, ITdTextMeasure messung)
    {
        var ergebnis = new TdLayoutResult();

        // Die Nummern hängen davon ab, was **vor** einem Punkt steht — sie werden deshalb
        // einmal für das ganze Dokument gerechnet und nicht je Absatz.
        var marken = TdListNumbering.Marken(doc);

        foreach (var abschnitt in doc.Sections)
            AbschnittUmbrechen(doc, abschnitt, messung, ergebnis, marken);

        // Ein Dokument ohne jeden Absatz hat trotzdem eine Seite — sonst gäbe es nichts
        // anzuzeigen und nichts zu drucken.
        if (ergebnis.Pages.Count == 0)
            ergebnis.Pages.Add(new TdPage { Number = 1, Setup = doc.Sections.FirstOrDefault()?.Page ?? TdPageSetup.A4 });

        return ergebnis;
    }

    private static void AbschnittUmbrechen(
        TdDocument doc, TdSection abschnitt, ITdTextMeasure messung, TdLayoutResult ergebnis,
        Dictionary<TdParagraph, string> marken)
    {
        var seite = NeueSeite(abschnitt.Page, ergebnis);
        double hoehe = abschnitt.Page.TextHoeheCm;
        double y = 0;

        // **Die Gruppe ist der Kern von „nicht vom nächsten Absatz trennen".** Solange ein
        // Absatz `KeepWithNext` trägt, wandern seine Zeilen nicht sofort auf die Seite,
        // sondern sammeln sich hier — zusammen mit denen der folgenden Absätze. Erst wenn
        // ein Absatz ohne `KeepWithNext` kommt, wird die ganze Gruppe **am Stück** gesetzt.
        // Eine Überschrift bleibt so bei ihrem ersten Absatz, statt allein unten zu stehen.
        var gruppe = new List<TdLine>();

        void GruppeSetzen()
        {
            if (gruppe.Count == 0) return;

            double gebraucht = gruppe.Sum(z => z.HeightCm);

            // Passt sie als Ganzes nicht mehr, fängt sie auf einer neuen Seite an — aber nur,
            // wenn die aktuelle überhaupt schon etwas trägt. Sonst entstünde eine leere Seite
            // vor einer Gruppe, die auf **keine** Seite passt.
            if (y + gebraucht > hoehe && seite.Lines.Count > 0)
            {
                seite = NeueSeite(abschnitt.Page, ergebnis);
                y = 0;
            }

            foreach (var zeile in gruppe)
            {
                // **Eine Gruppe, die höher ist als eine ganze Seite, muss trotzdem
                // weiterlaufen.** Sie bricht dann innerhalb um — sichtbar unschön ist besser
                // als ein Umbruch, der nicht zurückkommt (dieselbe Lehre wie bei
                // `Eine_Tabelle_braucht_ihre_Trennzeile`, §4.12).
                if (y + zeile.HeightCm > hoehe && seite.Lines.Count > 0)
                {
                    seite = NeueSeite(abschnitt.Page, ergebnis);
                    y = 0;
                }

                zeile.YCm = y;
                seite.Lines.Add(zeile);
                y += zeile.HeightCm;
            }
            gruppe.Clear();
        }

        void SeiteWechseln()
        {
            GruppeSetzen();
            seite = NeueSeite(abschnitt.Page, ergebnis);
            y = 0;
        }

        foreach (var block in abschnitt.Blocks)
        {
            if (block is TdPageBreak) { SeiteWechseln(); continue; }

            if (block is TdTable tabelle)
            {
                // Eine Tabelle bricht **zwischen** ihren Zeilen um und gehört deshalb nicht
                // in die Gruppe: sie wäre sonst als Ganzes unteilbar.
                GruppeSetzen();
                TabelleSetzen(doc, tabelle, abschnitt, messung, marken, ergebnis,
                    ref seite, ref y, hoehe);
                continue;
            }

            if (block is not TdParagraph absatz) continue;

            var format = doc.FormatVon(absatz);

            // Ein erzwungener Umbruch **vor** dem Absatz — aber nicht, wenn die Seite ohnehin
            // noch leer ist: sonst entstünde eine leere Seite davor.
            if (format.PageBreakBefore == true && (seite.Lines.Count > 0 || gruppe.Count > 0))
                SeiteWechseln();

            // Die Abstände davor und danach werden der ersten bzw. letzten Zeile zugeschlagen.
            // So bleiben sie Teil der Gruppe und wandern mit ihr — ein Abstand, der allein
            // oben auf der neuen Seite landet, wäre ein sichtbarer Fehler.
            //
            // **Beim Abstand davor wandert die Grundlinie mit**: sie zählt ab der Oberkante
            // der Zeile, und die liegt jetzt um den Abstand höher. Ohne das säße der Text im
            // Abstand statt darunter.
            var zeilen = AbsatzZeilen(doc, absatz, abschnitt.Page, messung,
                marken.GetValueOrDefault(absatz));
            double vor = format.SpaceBeforePt!.Value * CmProPunkt;
            zeilen[0].HeightCm += vor;
            zeilen[0].BaselineCm += vor;
            zeilen[^1].HeightCm += format.SpaceAfterPt!.Value * CmProPunkt;

            gruppe.AddRange(zeilen);

            if (format.KeepWithNext != true) GruppeSetzen();
        }

        GruppeSetzen();
    }

    private static TdPage NeueSeite(TdPageSetup einrichtung, TdLayoutResult ergebnis)
    {
        var seite = new TdPage { Number = ergebnis.Pages.Count + 1, Setup = einrichtung };
        ergebnis.Pages.Add(seite);
        return seite;
    }

    // ==================== Tabellen ====================

    /// <summary>
    /// Setzt eine Tabelle. **Sie bricht zwischen ihren Zeilen um**, und die Kopfzeilen
    /// werden auf jeder Folgeseite wiederholt.
    /// </summary>
    private static void TabelleSetzen(
        TdDocument doc, TdTable tabelle, TdSection abschnitt, ITdTextMeasure messung,
        Dictionary<TdParagraph, string> marken, TdLayoutResult ergebnis,
        ref TdPage seite, ref double y, double hoehe)
    {
        if (tabelle.Rows.Count == 0) return;

        var breiten = tabelle.Spaltenbreiten(abschnitt.Page.TextBreiteCm);
        var gesetzt = new List<TdLaidOutRow>();

        foreach (var zeile in tabelle.Rows)
            gesetzt.Add(ZeileSetzen(doc, tabelle, zeile, breiten, abschnitt, messung, marken));

        SenkrechteVerbindungenAufloesen(tabelle, gesetzt);

        // Die Kopfzeilen sind die **führenden** Zeilen mit `IsHeader`. Eine Kopfzeile mitten
        // in der Tabelle lässt sich nicht wiederholen — Word ignoriert sie ebenso, und das
        // ist keine Eigenheit, sondern die einzige Lesart, die sich umbrechen lässt.
        int kopfzeilen = 0;
        while (kopfzeilen < gesetzt.Count && tabelle.Rows[kopfzeilen].IsHeader) kopfzeilen++;
        double kopfhoehe = gesetzt.Take(kopfzeilen).Sum(z => z.HeightCm);

        bool kopfSteht = false;

        for (int i = 0; i < gesetzt.Count; i++)
        {
            var zeile = gesetzt[i];
            bool istKopf = i < kopfzeilen;

            // **Eine Zeile, die höher ist als die Seite, darf nicht in eine Endlosschleife
            // führen.** Sie kommt auf die (leere) Seite und ragt heraus — derselbe Ausweg
            // wie beim überlangen Wort und bei der zu großen Gruppe (§4.16).
            if (y + zeile.HeightCm > hoehe && seite.Lines.Count + seite.TableRows.Count > 0)
            {
                seite = NeueSeite(abschnitt.Page, ergebnis);
                y = 0;
                kopfSteht = false;
            }

            // Auf einer Folgeseite die Kopfzeilen wiederholen — aber nur, wenn danach noch
            // Platz für mindestens eine Inhaltszeile bleibt. Sonst stünde eine Kopfzeile
            // allein auf der Seite und die Tabelle begänne erst auf der nächsten.
            if (!istKopf && !kopfSteht && kopfzeilen > 0 &&
                y + kopfhoehe + zeile.HeightCm <= hoehe)
            {
                for (int k = 0; k < kopfzeilen; k++)
                {
                    var wiederholt = ZeileSetzen(doc, tabelle, tabelle.Rows[k], breiten,
                        abschnitt, messung, marken);
                    wiederholt.IsRepeatedHeader = true;
                    wiederholt.YCm = y;
                    seite.TableRows.Add(wiederholt);
                    y += wiederholt.HeightCm;
                }
            }
            if (!istKopf) kopfSteht = true;

            zeile.YCm = y;
            seite.TableRows.Add(zeile);
            y += zeile.HeightCm;
        }
    }

    private static TdLaidOutRow ZeileSetzen(
        TdDocument doc, TdTable tabelle, TdTableRow zeile, double[] breiten,
        TdSection abschnitt, ITdTextMeasure messung, Dictionary<TdParagraph, string> marken)
    {
        var gesetzt = new TdLaidOutRow { Table = tabelle, Source = zeile };
        var f = tabelle.Format;

        double x = 0;
        int spalte = 0;
        double inhaltshoehe = 0;

        foreach (var zelle in zeile.Cells)
        {
            int spannweite = Math.Max(1, zelle.ColumnSpan);

            double breite = 0;
            for (int i = 0; i < spannweite && spalte + i < breiten.Length; i++)
                breite += breiten[spalte + i];

            // **Fortsetzungszellen bekommen keinen Ort und keinen Inhalt** — ihr Inhalt steht
            // in der Restart-Zelle darüber. Sie schieben aber die Spalte weiter, sonst säße
            // alles dahinter eine Spalte zu weit links.
            if (!zelle.IstFortsetzung)
            {
                var gesetzteZelle = ZelleSetzen(doc, zelle, x, breite, f, abschnitt, messung, marken);
                gesetzt.Cells.Add(gesetzteZelle);

                double hoehe = gesetzteZelle.Lines.Sum(z => z.HeightCm)
                               + f.CellPaddingTopCm + f.CellPaddingBottomCm;

                // Eine Zelle, die über mehrere Zeilen reicht, darf **diese** Zeile nicht
                // hochziehen — ihre Höhe verteilt sich, und das entscheidet erst
                // SenkrechteVerbindungenAufloesen.
                if (zelle.VerticalMerge != TdVerticalMerge.Restart)
                    inhaltshoehe = Math.Max(inhaltshoehe, hoehe);
            }

            x += breite;
            spalte += spannweite;
        }

        gesetzt.HeightCm = Math.Max(zeile.MinHeightCm ?? 0, inhaltshoehe);
        return gesetzt;
    }

    private static TdLaidOutCell ZelleSetzen(
        TdDocument doc, TdTableCell zelle, double x, double breite, TdTableFormat f,
        TdSection abschnitt, ITdTextMeasure messung, Dictionary<TdParagraph, string> marken)
    {
        var gesetzt = new TdLaidOutCell
        {
            XCm = x,
            WidthCm = breite,
            Source = zelle,
        };

        // Der Inhalt bricht in der **Innenbreite** um, nicht in der Zellbreite. Ein
        // vergessener Innenabstand ist kein sichtbarer Fehler, sondern eine Tabelle, deren
        // Text am Rand klebt und eine Zeile zu spät umbricht.
        double innen = Math.Max(0.1, breite - f.CellPaddingLeftCm - f.CellPaddingRightCm);

        // Die Zelle ist eine eigene kleine Seite: der Zellinhalt kennt weder Seitenränder noch
        // die Einzüge der Tabelle, nur seine eigene Breite.
        var zellseite = new TdPageSetup
        {
            WidthCm = innen,
            HeightCm = abschnitt.Page.HeightCm,
            MarginLeftCm = 0,
            MarginRightCm = 0,
            MarginTopCm = 0,
            MarginBottomCm = 0,
        };

        double y = 0;
        foreach (var block in zelle.Blocks)
        {
            // **Benannte Lücke: eine Tabelle *in* einer Zelle wird hier nicht gesetzt.**
            // Das Modell trägt sie, DOCX schreibt und liest sie (§4.18) — nur der Umbruch
            // kennt sie noch nicht, denn dafür bräuchte eine gesetzte Zelle ihrerseits
            // Tabellenzeilen. Sie ist damit sichtbar leer statt still falsch, und der
            // Wächter `Eine_Tabelle_in_einer_Zelle_wird_noch_nicht_gesetzt` hält den Zustand
            // fest, damit er absichtlich verschwindet und nicht versehentlich.
            if (block is not TdParagraph absatz) continue;

            var format = doc.FormatVon(absatz);
            var zeilen = AbsatzZeilen(doc, absatz, zellseite, messung, marken.GetValueOrDefault(absatz));

            double vor = format.SpaceBeforePt!.Value * CmProPunkt;
            zeilen[0].HeightCm += vor;
            zeilen[0].BaselineCm += vor;
            zeilen[^1].HeightCm += format.SpaceAfterPt!.Value * CmProPunkt;

            foreach (var zeile in zeilen)
            {
                zeile.YCm = y;
                gesetzt.Lines.Add(zeile);
                y += zeile.HeightCm;
            }
        }

        return gesetzt;
    }

    /// <summary>
    /// Verteilt die Höhe senkrecht verbundener Zellen.
    /// <para>
    /// <b>Die Rechnung läuft in zwei Durchgängen, und das ist nötig:</b> Wie hoch eine
    /// verbundene Zelle die Zeilen macht, über die sie reicht, steht erst fest, wenn diese
    /// Zeilen ihre eigene Höhe kennen. Erst danach lässt sich sagen, ob noch etwas fehlt.
    /// </para>
    /// </summary>
    private static void SenkrechteVerbindungenAufloesen(TdTable tabelle, List<TdLaidOutRow> gesetzt)
    {
        for (int z = 0; z < tabelle.Rows.Count; z++)
        {
            int spalte = 0;
            int zellenIndex = 0;

            foreach (var zelle in tabelle.Rows[z].Cells)
            {
                int spannweite = Math.Max(1, zelle.ColumnSpan);

                if (zelle.VerticalMerge == TdVerticalMerge.Restart)
                {
                    // Wie viele Zeilen reicht sie? So viele Fortsetzungen folgen ihr in
                    // derselben Spalte.
                    int reicht = 1;
                    for (int w = z + 1; w < tabelle.Rows.Count; w++)
                    {
                        if (ZelleAnSpalte(tabelle.Rows[w], spalte) is { IstFortsetzung: true }) reicht++;
                        else break;
                    }

                    var gesetzteZelle = gesetzt[z].Cells[zellenIndex];
                    gesetzteZelle.RowSpan = reicht;

                    double vorhanden = 0;
                    for (int w = z; w < z + reicht && w < gesetzt.Count; w++) vorhanden += gesetzt[w].HeightCm;

                    double gebraucht = gesetzteZelle.Lines.Sum(l => l.HeightCm)
                                       + tabelle.Format.CellPaddingTopCm + tabelle.Format.CellPaddingBottomCm;

                    // Fehlt Platz, wächst die **letzte** Zeile der Verbindung. Sie
                    // gleichmäßig zu verteilen sähe gefälliger aus, verschöbe aber die
                    // Zeilen dazwischen gegenüber ihren unverbundenen Nachbarn.
                    if (gebraucht > vorhanden && z + reicht - 1 < gesetzt.Count)
                        gesetzt[z + reicht - 1].HeightCm += gebraucht - vorhanden;
                }

                if (!zelle.IstFortsetzung) zellenIndex++;
                spalte += spannweite;
            }
        }
    }

    /// <summary>Die Zelle, die an Rasterspalte <paramref name="spalte"/> beginnt — oder <c>null</c>.</summary>
    private static TdTableCell? ZelleAnSpalte(TdTableRow zeile, int spalte)
    {
        int s = 0;
        foreach (var zelle in zeile.Cells)
        {
            if (s == spalte) return zelle;
            s += Math.Max(1, zelle.ColumnSpan);
            if (s > spalte) return null;
        }
        return null;
    }

    // ==================== Zeilenumbruch ====================

    private const double CmProPunkt = 2.54 / 72.0;

    /// <summary>
    /// Bricht **einen** Absatz in Zeilen. Ohne Seitenbezug: wo die Zeilen später landen,
    /// entscheidet der Seitenumbruch.
    /// </summary>
    /// <param name="marke">
    /// Die schon gerechnete Aufzählungsmarke, falls dies ein Listenpunkt ist. Sie kommt von
    /// außen, weil sie vom ganzen Dokument abhängt und nicht von diesem Absatz.
    /// </param>
    public static List<TdLine> AbsatzZeilen(
        TdDocument doc, TdParagraph absatz, TdPageSetup seite, ITdTextMeasure messung,
        string? marke = null)
    {
        var format = doc.FormatVon(absatz);

        double links = format.LeftIndentCm!.Value;
        double ersteZeileVersatz = format.FirstLineIndentCm!.Value;

        // **Bei einem Listenpunkt kommen Einzug und hängender Einzug aus der Ebene** — es sei
        // denn, der Absatz sagt selbst etwas anderes. Genau so hält es DOCX: das `pPr` der
        // Ebene ist die Vorlage, das des Absatzes schlägt sie.
        var ebene = ListenEbene(doc, absatz);
        if (ebene is not null)
        {
            if (absatz.Format.LeftIndentCm is null) links = ebene.IndentCm;
            if (absatz.Format.FirstLineIndentCm is null) ersteZeileVersatz = -ebene.HangingCm;
        }

        // **Bei einem Listenpunkt beginnt der Text jeder Zeile am Einzug**, und die Marke
        // steht davor. Das ist etwas anderes als ein gewöhnlicher hängender Einzug, bei dem
        // die *erste* Zeile weiter links anfängt: dort steht der Text im Überhang, hier die
        // Nummer. Word hält es genauso — der Wert `hanging` gibt bei einer Liste an, wie weit
        // die Marke links steht, und nicht den Text.
        if (ebene is not null && marke is not null) ersteZeileVersatz = 0;

        // Alle X-Werte sind **relativ zum Textbereich der Seite**, nicht zum Einzug: das ist
        // die Zahl, die der Zeichner braucht, und sie muss den Einzug schon enthalten.
        double rechtsKante = seite.TextBreiteCm - format.RightIndentCm!.Value;
        double breite = Math.Max(0.01, rechtsKante - links);

        var zeilen = new List<TdLine>();
        var aktuell = new TdLine { Source = absatz };
        double zeilenStart = links + ersteZeileVersatz;
        double x = zeilenStart;

        void ZeileAbschliessen(bool letzte)
        {
            Ausrichten(aktuell, format.Alignment!.Value, rechtsKante - x, letzte);
            HoeheSetzen(aktuell, doc, absatz, format.LineSpacing!.Value, messung);
            zeilen.Add(aktuell);
        }

        void NeueZeile()
        {
            aktuell = new TdLine { Source = absatz };
            zeilenStart = links;
            x = zeilenStart;
        }

        foreach (var inline in absatz.Inlines)
        {
            if (inline is TdLineBreak)
            {
                ZeileAbschliessen(letzte: true);   // ein erzwungener Umbruch wird nicht gestreckt
                NeueZeile();
                continue;
            }

            if (inline is not TdRun lauf || lauf.Text.Length == 0) continue;

            var zeichenformat = doc.FormatVon(absatz, lauf);

            foreach (string stueck in InWoerter(lauf.Text))
            {
                double stueckBreite = messung.WidthCm(stueck, zeichenformat);

                if (x + stueckBreite > rechtsKante && aktuell.Runs.Count > 0)
                {
                    ZeileAbschliessen(letzte: false);
                    NeueZeile();

                    // Nach einem Umbruch fällt der führende Leerraum weg — sonst rückte jede
                    // Folgezeile um ein Leerzeichen ein.
                    string ohneLeerraum = stueck.TrimStart();
                    if (ohneLeerraum.Length == 0) continue;
                    if (ohneLeerraum.Length != stueck.Length)
                        stueckBreite = messung.WidthCm(ohneLeerraum, zeichenformat);

                    aktuell.Runs.Add(new TdLaidOutRun(ohneLeerraum, zeichenformat, x, stueckBreite));
                    x += stueckBreite;
                    continue;
                }

                aktuell.Runs.Add(new TdLaidOutRun(stueck, zeichenformat, x, stueckBreite));
                x += stueckBreite;
            }
        }

        ZeileAbschliessen(letzte: true);

        // Die Marke gehört an die **erste** Zeile und links vom Text. Sie steht bewusst nicht
        // in `Runs`: sie ist kein Text, lässt sich nicht auswählen und wird nicht mitkopiert.
        if (ebene is not null && marke is { Length: > 0 })
        {
            var markenformat = absatz.CharFormat.Over(doc.DefaultCharFormat).Aufgeloest();
            zeilen[0].Marker = new TdLaidOutRun(
                marke, markenformat,
                links - ebene.HangingCm,
                messung.WidthCm(marke, markenformat));
        }

        return zeilen;
    }

    /// <summary>Die Ebene, auf die der Listenverweis eines Absatzes zeigt — oder <c>null</c>.</summary>
    private static TdListLevel? ListenEbene(TdDocument doc, TdParagraph absatz)
    {
        if (absatz.List is not { } verweis) return null;
        return doc.Lists.FirstOrDefault(l => l.Id == verweis.ListId)?.Level(verweis.Level);
    }

    /// <summary>
    /// Zerlegt einen Text in umbruchfähige Stücke: jedes Wort **samt** dem Leerraum davor.
    /// <para>
    /// Der Leerraum gehört ans Wort und nicht dazwischen, weil er beim Umbruch verschwinden
    /// muss — ein Leerzeichen am Zeilenanfang rückt die Zeile ein, und das fällt bei
    /// Blocksatz sofort auf.
    /// </para>
    /// </summary>
    private static IEnumerable<string> InWoerter(string text)
    {
        int i = 0;
        while (i < text.Length)
        {
            int anfang = i;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;

            // **Ohne diesen Fortschritt stünde die Schleife still.** Er greift nur bei einem
            // Text, der weder Leerraum noch Nicht-Leerraum ist — also nie; die Zeile steht
            // hier trotzdem, weil ein hängender Umbruch keinen Fehler meldet, sondern gar
            // nichts (§7, „Ein Testlauf, der nicht zurückkommt").
            if (i == anfang) i++;

            yield return text[anfang..i];
        }
    }

    /// <summary>
    /// Schiebt die Stücke der Zeile an ihren Platz. **Blocksatz gilt nicht für die letzte
    /// Zeile eines Absatzes** — sonst zöge ein Schlusswort über die ganze Breite auseinander.
    /// </summary>
    /// <param name="rest">Der Platz, der auf der Zeile noch frei ist.</param>
    private static void Ausrichten(TdLine zeile, TdAlign ausrichtung, double rest, bool letzte)
    {
        if (zeile.Runs.Count == 0 || rest <= 0) return;

        switch (ausrichtung)
        {
            case TdAlign.Center:
                Verschieben(zeile, rest / 2);
                break;

            case TdAlign.Right:
                Verschieben(zeile, rest);
                break;

            case TdAlign.Justify when !letzte && zeile.Runs.Count > 1:
            {
                // Der Rest wird auf die Zwischenräume verteilt — es gibt einen weniger als
                // Stücke.
                double proLuecke = rest / (zeile.Runs.Count - 1);
                for (int i = 1; i < zeile.Runs.Count; i++)
                    zeile.Runs[i] = zeile.Runs[i] with { XCm = zeile.Runs[i].XCm + proLuecke * i };
                break;
            }
        }
    }

    private static void Verschieben(TdLine zeile, double um)
    {
        for (int i = 0; i < zeile.Runs.Count; i++)
            zeile.Runs[i] = zeile.Runs[i] with { XCm = zeile.Runs[i].XCm + um };
    }

    /// <summary>
    /// Höhe und Grundlinie einer Zeile: das größte Maß aller Stücke darin.
    /// <para>
    /// **Eine leere Zeile bekommt die Maße des Absatzes** und nicht null — sonst hätte ein
    /// leerer Absatz keine Höhe, der Cursor keinen Ort und der Seitenumbruch nichts zu
    /// rechnen.
    /// </para>
    /// </summary>
    private static void HoeheSetzen(
        TdLine zeile, TdDocument doc, TdParagraph absatz, double zeilenabstand, ITdTextMeasure messung)
    {
        TdFontMetrics groesstes;

        if (zeile.Runs.Count == 0)
        {
            groesstes = messung.Metrics(absatz.CharFormat.Over(doc.DefaultCharFormat).Aufgeloest());
        }
        else
        {
            double auf = 0, ab = 0, zeilenhoehe = 0;
            foreach (var r in zeile.Runs)
            {
                var m = messung.Metrics(r.Format);
                auf = Math.Max(auf, m.AscentCm);
                ab = Math.Max(ab, m.DescentCm);
                zeilenhoehe = Math.Max(zeilenhoehe, m.LineHeightCm);
            }
            groesstes = new TdFontMetrics(auf, ab, zeilenhoehe);
        }

        zeile.BaselineCm = groesstes.AscentCm;
        zeile.HeightCm = groesstes.LineHeightCm * zeilenabstand;
    }
}
