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

    public string PlainText() => string.Concat(Runs.Select(r => r.Text));
}

/// <summary>Eine gesetzte Seite.</summary>
public sealed class TdPage
{
    /// <summary>Beginnt bei 1 — die Zahl, die in <c>{SEITE}</c> steht.</summary>
    public int Number { get; set; }

    /// <summary>Die Einrichtung des Abschnitts, zu dem diese Seite gehört.</summary>
    public TdPageSetup Setup { get; set; } = TdPageSetup.A4;

    public List<TdLine> Lines { get; } = new();
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

        foreach (var abschnitt in doc.Sections)
            AbschnittUmbrechen(doc, abschnitt, messung, ergebnis);

        // Ein Dokument ohne jeden Absatz hat trotzdem eine Seite — sonst gäbe es nichts
        // anzuzeigen und nichts zu drucken.
        if (ergebnis.Pages.Count == 0)
            ergebnis.Pages.Add(new TdPage { Number = 1, Setup = doc.Sections.FirstOrDefault()?.Page ?? TdPageSetup.A4 });

        return ergebnis;
    }

    private static void AbschnittUmbrechen(
        TdDocument doc, TdSection abschnitt, ITdTextMeasure messung, TdLayoutResult ergebnis)
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
            var zeilen = AbsatzZeilen(doc, absatz, abschnitt.Page, messung);
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

    // ==================== Zeilenumbruch ====================

    private const double CmProPunkt = 2.54 / 72.0;

    /// <summary>
    /// Bricht **einen** Absatz in Zeilen. Ohne Seitenbezug: wo die Zeilen später landen,
    /// entscheidet der Seitenumbruch.
    /// </summary>
    public static List<TdLine> AbsatzZeilen(
        TdDocument doc, TdParagraph absatz, TdPageSetup seite, ITdTextMeasure messung)
    {
        var format = doc.FormatVon(absatz);

        double breite = seite.TextBreiteCm - format.LeftIndentCm!.Value - format.RightIndentCm!.Value;
        double ersteZeileVersatz = format.FirstLineIndentCm!.Value;

        var zeilen = new List<TdLine>();
        var aktuell = new TdLine { Source = absatz };
        double x = Math.Max(0, ersteZeileVersatz);
        double verfuegbar = Math.Max(0.01, breite - ersteZeileVersatz);

        void ZeileAbschliessen(bool letzte)
        {
            Ausrichten(aktuell, format.Alignment!.Value, breite, x, letzte);
            HoeheSetzen(aktuell, doc, absatz, format.LineSpacing!.Value, messung);
            zeilen.Add(aktuell);
        }

        void NeueZeile()
        {
            aktuell = new TdLine { Source = absatz };
            x = 0;
            verfuegbar = Math.Max(0.01, breite);
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

                if (x + stueckBreite > verfuegbar && aktuell.Runs.Count > 0)
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
        return zeilen;
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
    private static void Ausrichten(TdLine zeile, TdAlign ausrichtung, double breite, double belegt, bool letzte)
    {
        if (zeile.Runs.Count == 0) return;

        double rest = breite - belegt;
        if (rest <= 0) return;

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
