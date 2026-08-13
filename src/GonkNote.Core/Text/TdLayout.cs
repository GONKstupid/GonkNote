namespace GonkNote.Core.Text;

/// <summary>Ein Stück Text, fertig gesetzt: mit Ort und Breite.</summary>
/// <param name="Text">Der Text dieses Stücks — schon ohne den Leerraum am Zeilenende.</param>
/// <param name="Format">Das **aufgelöste** Zeichenformat; der Zeichner muss nichts mehr kaskadieren.</param>
/// <param name="XCm">Abstand vom linken Rand des Textbereichs.</param>
/// <param name="WidthCm">Breite des Stücks.</param>
/// <param name="Link">
/// Der Verweis, zu dem dieses Stück gehört — <c>null</c>, wenn es keiner ist. **Er steht hier
/// und nicht nur im Modell**, weil der Zeichner beim Klick nur das gesetzte Ergebnis vor sich
/// hat: ohne diese Angabe müsste er aus einem Punkt auf dem Papier den Absatz zurückrechnen,
/// um das Ziel zu finden.
/// </param>
/// <param name="Field">
/// Das Feld, dessen gerechneter Wert dieses Stück ist — <c>null</c> bei gewöhnlichem Text.
/// <b>Ein Feld ist unteilbar:</b> der Cursor gehört davor oder dahinter und nicht hinein, und
/// wer das nicht weiß, lässt den Nutzer eine Seitenzahl bearbeiten, die beim nächsten Umbruch
/// wieder überschrieben wird — dieselbe Überlegung wie bei der Aufzählungsmarke (§4.17).
/// </param>
/// <param name="Graphic">
/// Das Bild oder Diagramm, dessen Kasten dieses Stück ist — <c>null</c> bei Text. <b>Sein
/// <see cref="TdGraphic.HeightCm"/> ist die einzige Höhe, die nicht aus der Schrift kommt</b>;
/// der Zeichner malt hier statt Buchstaben, und <see cref="Text"/> ist leer.
/// </param>
/// <param name="Linear">
/// Der Abstand des **ersten Zeichens** dieses Stücks vom Absatzanfang, in Cursorschritten
/// (<see cref="TdCursor.Linear"/>) — <c>-1</c>, wenn das Stück im Modell gar nicht steht (die
/// Aufzählungsmarke, die Seitenzahl im Inhaltsverzeichnis).
///
/// <para>
/// <b>Das ist der Rückweg vom Papier ins Modell, und er muss hier stehen.</b> §4.30 hat ihn
/// für den Absatz versprochen (<see cref="TdLine.Source"/>), aber ein Absatz allein sagt nicht,
/// das wievielte Zeichen angeklickt wurde. Nachzählen ginge nicht: Der Umbruch wirft den
/// Leerraum am Zeilenende weg, ein Feld setzt seinen **gerechneten** Wert statt seiner selbst,
/// und ein Zeilenumbruch hinterlässt gar kein Stück — wer die Textlängen der Läufe addiert,
/// verzählt sich beim ersten umgebrochenen Wort und findet den Cursor danach immer weiter
/// daneben. <b>Die Zahl kennt nur, wer schneidet</b>, also der Umbruch.
/// </para>
/// <para>
/// <b>Bei einem Feld ist es die Stelle des Feldes und nicht seines Wertes</b> — im Dokument
/// steht ein Feld und keine Zahl (§4.20), und es ist genau **einen** Schritt breit.
/// </para>
/// </param>
public sealed record TdLaidOutRun(
    string Text,
    TdCharFormat Format,
    double XCm,
    double WidthCm,
    TdHyperlink? Link = null,
    TdField? Field = null,
    TdGraphic? Graphic = null,
    int Linear = -1)
{
    /// <summary>
    /// Wie viele **Cursorschritte** dieses Stück breit ist — dieselbe Zählung wie
    /// <see cref="TdCursor.Laenge(TdInline)"/>: ein Feld und ein Bild sind einer, obwohl auf
    /// dem Papier mehr steht.
    /// </summary>
    public int Schritte => Field is not null || Graphic is not null ? 1 : Text.Length;
}

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
    /// Wo diese Zeile im Absatz **anfängt**, in Cursorschritten (<see cref="TdCursor.Linear"/>).
    ///
    /// <para>
    /// <b>Ohne diese Zahl hätte eine leere Zeile keine Stelle.</b> Die Läufe tragen ihre eigene
    /// (<see cref="TdLaidOutRun.Linear"/>), aber eine Zeile ohne Läufe gibt es laufend: ein
    /// leerer Absatz, und jede Zeile hinter einem Umschalt+Eingabe-Umbruch. Genau dort steht
    /// beim Schreiben besonders oft der Cursor.
    /// </para>
    /// </summary>
    public int Linear { get; set; }

    /// <summary>
    /// Der linke Rand dieser Zeile, in Zentimetern ab dem Behälterrand — dieselbe Zählung wie
    /// <see cref="TdLaidOutRun.XCm"/>.
    /// <para>
    /// Gebraucht wird er nur für die Schreibmarke in einer **leeren** Zeile: Wo Läufe stehen,
    /// ist der erste ihr linker Rand. Wo keine stehen, wäre die Marke sonst auf 0 gerutscht —
    /// also an den Textrand statt an den Einzug.
    /// </para>
    /// </summary>
    public double StartXCm { get; set; }

    /// <summary>
    /// Die Aufzählungsmarke, falls dies die **erste** Zeile eines Listenpunkts ist. Sie steht
    /// links vom Text (negatives <c>XCm</c>) und gehört nicht zu <see cref="Runs"/>: sie ist
    /// nicht Teil des Textes, lässt sich nicht auswählen und wird beim Kopieren nicht
    /// mitgenommen.
    /// </summary>
    public TdLaidOutRun? Marker { get; set; }

    /// <summary>
    /// Gehört diese Zeile zu einem gesetzten Inhaltsverzeichnis? Sie steht dann **nicht** so
    /// im Dokument: ihr Absatz ist gerechnet, nicht geschrieben (§4.20). Wer sie beim
    /// Zurückrechnen auf den Text mitzählt, findet den Cursor an der falschen Stelle —
    /// dieselbe Vorsicht wie bei <see cref="TdLaidOutRow.IsRepeatedHeader"/>.
    /// </summary>
    public bool IsTocEntry { get; set; }

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
    /// Wie oft der Umbruch höchstens läuft, wenn Felder ihn von sich selbst abhängig machen
    /// (Schritt 5).
    ///
    /// <para>
    /// <b>Die Rechnung fällt sich hier ins Wort, und das ist keine Schwäche des Entwurfs,
    /// sondern die Sache selbst:</b> Die Seitenanzahl steht erst fest, wenn alles gesetzt ist
    /// — und ein Inhaltsverzeichnis braucht die Seitenzahlen der Überschriften, verschiebt
    /// diese Überschriften aber durch seine eigene Länge. Word rechnet deshalb ebenfalls
    /// mehrfach. Gelaufen wird, bis sich nichts mehr ändert, **höchstens aber so oft**: eine
    /// Schleife, die auf einen Fixpunkt wartet, den es nicht gibt, meldet keinen Fehler — sie
    /// meldet gar nichts (§4.16).
    /// </para>
    /// </summary>
    private const int HoechstensDurchgaenge = 5;

    /// <summary>
    /// Bricht das ganze Dokument um.
    /// <para>
    /// <b>Jeder Abschnitt beginnt auf einer neuen Seite.</b> Das ist DOCX' Vorgabe für
    /// <c>sectPr</c> ohne eigene Angabe („nextPage") und der einzige Fall, den das Modell
    /// heute kennt — ein fortlaufender Abschnittswechsel mitten auf der Seite wäre eine
    /// eigene Angabe, und die gibt es noch nicht.
    /// </para>
    /// </summary>
    /// <param name="felder">
    /// Datum und Titel für die Felder aus Schritt 5. <c>null</c> = <see cref="TdFieldContext.Ohne"/>
    /// — Core fragt die Uhr nicht selbst (§4.20).
    /// </param>
    public static TdLayoutResult Umbrechen(TdDocument doc, ITdTextMeasure messung, TdFieldContext? felder = null)
    {
        var kontext = felder ?? TdFieldContext.Ohne;

        var ergebnis = new Lauf(doc, messung, kontext, vorher: null).Umbrechen();

        // Ohne Felder, die vom Umbruch abhängen, ist der erste Durchgang auch der letzte.
        if (!HaengtVomUmbruchAb(doc)) return ergebnis;

        string signatur = Signatur(ergebnis);
        for (int i = 1; i < HoechstensDurchgaenge; i++)
        {
            var naechster = new Lauf(doc, messung, kontext, vorher: ergebnis).Umbrechen();
            string naechsteSignatur = Signatur(naechster);

            ergebnis = naechster;
            if (naechsteSignatur == signatur) break;
            signatur = naechsteSignatur;
        }

        return ergebnis;
    }

    /// <summary>
    /// Hängt im Dokument etwas vom Ergebnis des Umbruchs ab? Nur dann lohnt ein zweiter
    /// Durchgang — die Seitenzahl allein tut es nicht, die steht beim Setzen der Zeile fest.
    /// </summary>
    private static bool HaengtVomUmbruchAb(TdDocument doc)
    {
        foreach (var absatz in doc.Paragraphs())
            foreach (var (stueck, _) in absatz.FlacheStuecke())
                if (stueck is TdField { Kind: TdFieldKind.PageCount or TdFieldKind.TableOfContents })
                    return true;
        return false;
    }

    /// <summary>
    /// Ein Fingerabdruck des Ergebnisses. Er muss **nicht** jedes Pixel erfassen, sondern
    /// das, was sich zwischen zwei Durchgängen ändern kann: wie viele Seiten es gibt und wie
    /// viel auf jeder steht.
    /// </summary>
    private static string Signatur(TdLayoutResult ergebnis) =>
        string.Join(";", ergebnis.Pages.Select(s => $"{s.Lines.Count}/{s.TableRows.Count}"));

    // ==================== Ein Durchgang ====================

    /// <summary>
    /// Ein einzelner Durchgang des Umbruchs.
    ///
    /// <para>
    /// <b>Eine Klasse und keine Kette von Argumenten:</b> Dokument, Messung, Feldwerte,
    /// Listenmarken und das Ergebnis des vorigen Durchgangs gelten für den ganzen Lauf und
    /// werden sonst durch sieben Methoden gereicht. Sie stehen deshalb einmal hier —
    /// erzeugt wird pro Durchgang eine neue Instanz, damit nichts aus dem vorigen
    /// stehenbleibt.
    /// </para>
    /// </summary>
    private sealed class Lauf(
        TdDocument doc, ITdTextMeasure messung, TdFieldContext kontext, TdLayoutResult? vorher)
    {
        private readonly TdLayoutResult _ergebnis = new();

        /// <summary>
        /// Die Nummern hängen davon ab, was **vor** einem Punkt steht — sie werden deshalb
        /// einmal für das ganze Dokument gerechnet und nicht je Absatz.
        /// </summary>
        private readonly Dictionary<TdParagraph, string> _marken = TdListNumbering.Marken(doc);

        /// <summary>
        /// Die Gesamtzahl der Seiten aus dem **vorigen** Durchgang — im ersten gibt es keine,
        /// und ein <c>{SEITEN}</c> steht dann leer da statt mit einer erfundenen Zahl.
        /// </summary>
        private int? Seitenzahl => vorher?.PageCount;

        public TdLayoutResult Umbrechen()
        {
            foreach (var abschnitt in doc.Sections) AbschnittUmbrechen(abschnitt);

            // Ein Dokument ohne jeden Absatz hat trotzdem eine Seite — sonst gäbe es nichts
            // anzuzeigen und nichts zu drucken.
            if (_ergebnis.Pages.Count == 0)
                _ergebnis.Pages.Add(new TdPage
                {
                    Number = 1,
                    Setup = doc.Sections.FirstOrDefault()?.Page ?? TdPageSetup.A4,
                });

            return _ergebnis;
        }

        private void AbschnittUmbrechen(TdSection abschnitt)
        {
            var seite = NeueSeite(abschnitt.Page);
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
                    seite = NeueSeite(abschnitt.Page);
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
                        seite = NeueSeite(abschnitt.Page);
                        y = 0;
                    }

                    zeile.YCm = y;
                    SeitenzahlNachziehen(zeile, seite.Number);
                    seite.Lines.Add(zeile);
                    y += zeile.HeightCm;
                }
                gruppe.Clear();
            }

            void SeiteWechseln()
            {
                GruppeSetzen();
                seite = NeueSeite(abschnitt.Page);
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
                    TabelleSetzen(tabelle, abschnitt, ref seite, ref y, hoehe);
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
                var zeilen = AbsatzOderVerzeichnis(absatz, abschnitt.Page, seite.Number);
                double vor = format.SpaceBeforePt!.Value * CmProPunkt;
                zeilen[0].HeightCm += vor;
                zeilen[0].BaselineCm += vor;
                zeilen[^1].HeightCm += format.SpaceAfterPt!.Value * CmProPunkt;

                gruppe.AddRange(zeilen);

                if (format.KeepWithNext != true) GruppeSetzen();
            }

            GruppeSetzen();
        }

        private TdPage NeueSeite(TdPageSetup einrichtung)
        {
            var seite = new TdPage { Number = _ergebnis.Pages.Count + 1, Setup = einrichtung };
            _ergebnis.Pages.Add(seite);
            return seite;
        }

        // ==================== Felder ====================

        /// <summary>
        /// Setzt die Seitenzahl in einer Zeile richtig, sobald feststeht, auf welcher Seite sie
        /// landet.
        ///
        /// <para>
        /// <b>Warum das nachträglich geschieht:</b> Eine Zeile wird umbrochen, bevor entschieden
        /// ist, wohin sie kommt — eine zusammengehaltene Gruppe kann noch auf die nächste Seite
        /// rutschen. Ein <c>PAGE</c>-Feld, das beim Umbrechen die damals aktuelle Nummer
        /// bekommt, stünde dann um eins daneben.
        /// </para>
        /// <para>
        /// <b>Die Zeile wird dafür nicht neu umbrochen.</b> Wächst die Zahl von 9 auf 10, ist das
        /// Stück eine Ziffer breiter, als beim Messen angenommen — Word rechnet an dieser Stelle
        /// genauso ungenau. Ein vollständiges Neuumbrechen bei jedem Seitenwechsel wäre eine
        /// Rechnung, die sich selbst ins Wort fällt, und der Gewinn wäre eine Ziffer.
        /// </para>
        /// </summary>
        private void SeitenzahlNachziehen(TdLine zeile, int seitennummer)
        {
            for (int i = 0; i < zeile.Runs.Count; i++)
            {
                if (zeile.Runs[i].Field is not { Kind: TdFieldKind.PageNumber } feld) continue;

                zeile.Runs[i] = zeile.Runs[i] with
                {
                    Text = TdFieldValues.Text(feld, kontext, seitennummer, Seitenzahl),
                };
            }
        }

        /// <summary>
        /// Die Zeilen eines Absatzes — oder, wenn er ein Inhaltsverzeichnis ist, dessen
        /// Einträge.
        ///
        /// <para>
        /// <b>Ein Verzeichnisabsatz wird ersetzt, nicht ergänzt.</b> Was in ihm steht, ist das
        /// Feld und nicht der Text; auf dem Papier stehen die Überschriften des Dokuments.
        /// Solange es keine gibt, bleibt eine leere Zeile stehen — sonst hätte der Cursor an
        /// der Stelle des Verzeichnisses keinen Ort.
        /// </para>
        /// </summary>
        private List<TdLine> AbsatzOderVerzeichnis(TdParagraph absatz, TdPageSetup seite, int seitennummer)
        {
            if (TdToc.Feld(absatz) is not { } feld)
                return AbsatzZeilen(doc, absatz, seite, messung, _marken.GetValueOrDefault(absatz),
                    kontext, seitennummer, Seitenzahl);

            var eintraege = TdToc.Eintraege(doc, vorher, feld.Argument);
            if (eintraege.Count == 0)
                return AbsatzZeilen(doc, new TdParagraph { CharFormat = absatz.CharFormat, Format = absatz.Format },
                    seite, messung, marke: null, kontext, seitennummer, Seitenzahl);

            var zeichenformat = absatz.CharFormat.Over(doc.DefaultCharFormat).Aufgeloest();
            var zeilen = new List<TdLine>();

            foreach (var eintrag in eintraege)
            {
                var eintragsabsatz = TdToc.EintragsAbsatz(eintrag, absatz.CharFormat.Kopie());
                var eintragszeilen = AbsatzZeilen(doc, eintragsabsatz, seite, messung, marke: null,
                    kontext, seitennummer, Seitenzahl);

                // Die Zeilen gehören zum **Verzeichnisabsatz** und nicht zum gerechneten
                // Eintragsabsatz: Letzteren gibt es im Dokument nicht, und ein Cursor, der auf
                // ihn zeigt, zeigt ins Leere.
                foreach (var zeile in eintragszeilen)
                {
                    zeile.Source = absatz;
                    zeile.IsTocEntry = true;
                }

                if (eintrag.Page > 0) SeitenzahlAnsEnde(eintragszeilen[^1], eintrag.Page, zeichenformat, seite);

                zeilen.AddRange(eintragszeilen);
            }

            return zeilen;
        }

        /// <summary>
        /// Hängt die Seitenzahl eines Verzeichniseintrags rechtsbündig an seine letzte Zeile.
        /// <para>
        /// In Word steht dort ein Tabulator mit Füllzeichen. Den kennt das Modell nicht, und
        /// einen dafür zu erfinden hieße, ein halbes Feature an der falschen Stelle zu bauen —
        /// für das Verzeichnis genügt der rechte Rand, und den kennt der Umbruch ohnehin.
        /// </para>
        /// </summary>
        private void SeitenzahlAnsEnde(TdLine zeile, int nummer, TdCharFormat format, TdPageSetup seite)
        {
            string text = nummer.ToString();
            double breite = messung.WidthCm(text, format);
            zeile.Runs.Add(new TdLaidOutRun(text, format, Math.Max(0, seite.TextBreiteCm - breite), breite));
        }

        // ==================== Tabellen ====================

        /// <summary>
        /// Setzt eine Tabelle. **Sie bricht zwischen ihren Zeilen um**, und die Kopfzeilen
        /// werden auf jeder Folgeseite wiederholt.
        /// </summary>
        private void TabelleSetzen(
            TdTable tabelle, TdSection abschnitt, ref TdPage seite, ref double y, double hoehe)
        {
            if (tabelle.Rows.Count == 0) return;

            var breiten = tabelle.Spaltenbreiten(abschnitt.Page.TextBreiteCm);
            var gesetzt = new List<TdLaidOutRow>();

            foreach (var zeile in tabelle.Rows)
                gesetzt.Add(ZeileSetzen(tabelle, zeile, breiten, abschnitt, seite.Number));

            SenkrechteVerbindungenAufloesen(tabelle, gesetzt);

            // Die Kopfzeilen sind die **führenden** Zeilen mit `IsHeader`. Eine Kopfzeile mitten
            // in der Tabelle lässt sich nicht wiederholen — Word ignoriert sie ebenso, und das
            // ist keine Eigenheit, sondern die einzige Lesart, die sich umbrechen lässt.
            int kopfzeilen = 0;
            while (kopfzeilen < gesetzt.Count && tabelle.Rows[kopfzeilen].IsHeader) kopfzeilen++;
            double kopfhoehe = gesetzt.Take(kopfzeilen).Sum(z => z.HeightCm);

            // **Steht der Kopf schon auf dieser Seite?** Der Merker zählt die *echten*
            // Kopfzeilen mit und nicht nur die wiederholten — genau daran hing ein Fehler, der
            // bis §4.28 unbemerkt blieb: Er stand auf `false`, während die echten Kopfzeilen
            // gerade gesetzt wurden, und die erste Inhaltszeile löste dann eine Wiederholung
            // aus. **Jede** Tabelle bekam ihre Kopfzeile damit doppelt, schon auf Seite 1.
            //
            // Aufgefallen ist er erst, als der Zeichner angeschlossen wurde (§4.28) — die
            // Wächter prüften den *Umbruch nach* einem Seitenwechsel und die *Ausgabe* nach
            // Text; zweimal derselbe Zellinhalt fällt in einem Textvergleich nicht auf, im
            // Bild sofort. Der Wächter dazu heißt jetzt
            // `Eine_Tabelle_auf_einer_Seite_hat_keine_wiederholte_Kopfzeile`.
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
                    seite = NeueSeite(abschnitt.Page);
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
                        var wiederholt = ZeileSetzen(tabelle, tabelle.Rows[k], breiten, abschnitt, seite.Number);
                        wiederholt.IsRepeatedHeader = true;
                        wiederholt.YCm = y;
                        ZeileAufSeite(wiederholt, seite);
                        y += wiederholt.HeightCm;
                    }
                }
                if (!istKopf) kopfSteht = true;

                zeile.YCm = y;
                ZeileAufSeite(zeile, seite);
                y += zeile.HeightCm;

                // Die letzte **echte** Kopfzeile steht — ab hier braucht es auf dieser Seite
                // keine Wiederholung mehr. Ohne diese Zeile wiederholt sich der Kopf direkt
                // unter sich selbst.
                if (istKopf && i == kopfzeilen - 1) kopfSteht = true;
            }
        }

        /// <summary>
        /// Setzt eine Tabellenzeile auf die Seite — und zieht dabei die Seitenzahlen in ihren
        /// Zellen nach, aus demselben Grund wie bei einer gewöhnlichen Zeile.
        /// </summary>
        private void ZeileAufSeite(TdLaidOutRow zeile, TdPage seite)
        {
            foreach (var zelle in zeile.Cells)
                foreach (var innen in zelle.Lines)
                    SeitenzahlNachziehen(innen, seite.Number);

            seite.TableRows.Add(zeile);
        }

        private TdLaidOutRow ZeileSetzen(
            TdTable tabelle, TdTableRow zeile, double[] breiten, TdSection abschnitt, int seitennummer)
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
                    var gesetzteZelle = ZelleSetzen(zelle, x, breite, f, abschnitt, seitennummer);
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

        private TdLaidOutCell ZelleSetzen(
            TdTableCell zelle, double x, double breite, TdTableFormat f, TdSection abschnitt, int seitennummer)
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
                var zeilen = AbsatzOderVerzeichnis(absatz, zellseite, seitennummer);

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
    /// <param name="felder">Datum und Titel für die Felder (Schritt 5).</param>
    /// <param name="seitennummer">
    /// Die Seite, auf der die Zeilen **voraussichtlich** landen. Endgültig gesetzt wird eine
    /// Seitenzahl erst beim Setzen der Zeile (§4.20).
    /// </param>
    /// <param name="seiten">Die Gesamtzahl der Seiten, soweit sie schon feststeht.</param>
    public static List<TdLine> AbsatzZeilen(
        TdDocument doc, TdParagraph absatz, TdPageSetup seite, ITdTextMeasure messung,
        string? marke = null, TdFieldContext? felder = null, int seitennummer = 1, int? seiten = null)
    {
        var kontext = felder ?? TdFieldContext.Ohne;
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
            aktuell.StartXCm = zeilenStart;
            Ausrichten(aktuell, format.Alignment!.Value, rechtsKante - x, letzte);
            HoeheSetzen(aktuell, doc, absatz, format.LineSpacing!.Value, messung);
            zeilen.Add(aktuell);
        }

        // <paramref name="linear"/> ist die Stelle im Absatz, an der die neue Zeile anfängt —
        // ohne sie hätte eine leere Zeile keinen Ort (siehe TdLine.Linear).
        void NeueZeile(int linear)
        {
            aktuell = new TdLine { Source = absatz, Linear = linear };
            zeilenStart = links;
            x = zeilenStart;
        }

        // Setzt ein Stück und bricht davor um, wenn es nicht mehr passt.
        void StueckSetzen(
            string stueck, TdCharFormat zeichenformat, TdHyperlink? verweis, TdField? feld, int linear)
        {
            double stueckBreite = messung.WidthCm(stueck, zeichenformat);

            if (x + stueckBreite > rechtsKante && aktuell.Runs.Count > 0)
            {
                ZeileAbschliessen(letzte: false);

                // Nach einem Umbruch fällt der führende Leerraum weg — sonst rückte jede
                // Folgezeile um ein Leerzeichen ein. **Die Stelle rückt dabei mit**: Was hier
                // wegfällt, steht weiter im Absatz, und ein Cursor, der das nicht wüsste, wäre
                // ab der zweiten Zeile um jeden weggefallenen Zwischenraum verschoben.
                string ohneLeerraum = stueck.TrimStart();
                int weggefallen = stueck.Length - ohneLeerraum.Length;

                NeueZeile(linear + weggefallen);

                if (ohneLeerraum.Length == 0) return;
                if (weggefallen > 0) stueckBreite = messung.WidthCm(ohneLeerraum, zeichenformat);

                aktuell.Runs.Add(new TdLaidOutRun(
                    ohneLeerraum, zeichenformat, x, stueckBreite, verweis, feld, null,
                    linear + weggefallen));
                x += stueckBreite;
                return;
            }

            aktuell.Runs.Add(new TdLaidOutRun(
                stueck, zeichenformat, x, stueckBreite, verweis, feld, null, linear));
            x += stueckBreite;
        }

        // **Der Durchlauf ist flach**: ein Verweis erscheint nicht selbst, sondern seine
        // Stücke. Wer hier über `Inlines` liefe, sähe den Verweis und nicht seinen Text
        // (§7, „Markdown-Export").
        // **Der Abstand vom Absatzanfang läuft mit** — dieselbe Zählung wie TdCursor.Linear,
        // also über die flachen Stücke und mit einem Schritt je Feld, Bild und Umbruch. Nur so
        // findet Schritt 4 aus einem Mausklick die Stelle im Modell zurück.
        int linear = 0;

        foreach (var (stueck, verweis) in absatz.FlacheStuecke())
        {
            if (stueck is TdLineBreak)
            {
                ZeileAbschliessen(letzte: true);   // ein erzwungener Umbruch wird nicht gestreckt
                linear++;
                NeueZeile(linear);
                continue;
            }

            var zeichenformat = doc.FormatVon(absatz, stueck);

            if (stueck is TdGraphic grafik)
            {
                // **Eine Grafik wird nicht gemessen, sie hat ihr Maß dabei.** Die
                // Schriftmessung hat hier nichts zu suchen — ein Bild ist so breit, wie es im
                // Dokument steht, und auf jedem System gleich.
                double kasten = Math.Max(0, grafik.WidthCm);

                // Ist sie breiter als die Zeile, steht sie allein darin und ragt heraus —
                // derselbe Ausweg wie beim überlangen Wort (§4.16). Sichtbar falsch ist besser
                // als ein Umbruch, der nicht zurückkommt.
                if (x + kasten > rechtsKante && aktuell.Runs.Count > 0)
                {
                    ZeileAbschliessen(letzte: false);
                    NeueZeile(linear);
                }

                aktuell.Runs.Add(new TdLaidOutRun(
                    "", zeichenformat, x, kasten, verweis, null, grafik, linear));
                x += kasten;
                linear++;
                continue;
            }

            if (stueck is TdField feld)
            {
                // **Ein Feld bricht nicht in sich um.** Sein Wert ist ein Stück und kein Text:
                // eine Seitenzahl, deren zweite Ziffer in der nächsten Zeile steht, wäre keine
                // Seitenzahl mehr. Deshalb geht es ungeteilt durch, auch wenn es Leerraum
                // enthält (ein Titel darf einen haben).
                string wert = TdFieldValues.Text(feld, kontext, seitennummer, seiten);
                if (wert.Length > 0) StueckSetzen(wert, zeichenformat, verweis, feld, linear);

                // **Ein Schritt, egal wie lang der Wert ist** — im Dokument steht ein Feld und
                // keine Zahl (§4.30). Auch ein Feld ohne Wert zählt: es steht da.
                linear++;
                continue;
            }

            if (stueck is not TdRun lauf || lauf.Text.Length == 0) continue;

            foreach (var (wort, anfang) in InWoerter(lauf.Text))
                StueckSetzen(wort, zeichenformat, verweis, null, linear + anfang);

            linear += lauf.Text.Length;
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
    /// Zerlegt einen Text in umbruchfähige Stücke: jedes Wort **samt** dem Leerraum davor,
    /// und dazu, das wievielte Zeichen des Textes es ist.
    /// <para>
    /// Der Leerraum gehört ans Wort und nicht dazwischen, weil er beim Umbruch verschwinden
    /// muss — ein Leerzeichen am Zeilenanfang rückt die Zeile ein, und das fällt bei
    /// Blocksatz sofort auf.
    /// </para>
    /// <para>
    /// <b>Die Stelle kommt mit, weil nur hier bekannt ist, wo geschnitten wurde.</b> Wer sie
    /// draußen mitzählte, käme auf dieselbe Zahl — bis jemand diese Zerlegung ändert, und dann
    /// stünde die zweite Zählung still falsch da (<see cref="TdLaidOutRun.Linear"/>).
    /// </para>
    /// </summary>
    private static IEnumerable<(string Wort, int Anfang)> InWoerter(string text)
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

            yield return (text[anfang..i], anfang);
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
        // **Die Schrift des Absatzes ist immer dabei**, auch wenn kein Stück sie benutzt: Die
        // Absatzmarke steht am Zeilenende und hat eine Höhe. Ohne sie hätte ein leerer Absatz
        // keine Höhe und der Cursor keinen Ort — und eine Zeile mit nur einem winzigen Bild
        // wäre schmaler als eine leere.
        var absatzschrift = messung.Metrics(absatz.CharFormat.Over(doc.DefaultCharFormat).Aufgeloest());

        double auf = absatzschrift.AscentCm;
        double ab = absatzschrift.DescentCm;
        double zeilenhoehe = absatzschrift.LineHeightCm;

        foreach (var r in zeile.Runs)
        {
                // **Eine Grafik steht auf der Grundlinie und nicht darin.** Ihre Höhe zählt
                // deshalb ganz nach oben: sie schiebt die Grundlinie hinunter, statt die Zeile
                // nach unten zu überschreiten. Word setzt ein eingebundenes Bild genauso —
                // seine Unterkante sitzt auf der Grundlinie.
            if (r.Graphic is { } g)
            {
                auf = Math.Max(auf, g.HeightCm);
                continue;
            }

            var m = messung.Metrics(r.Format);
            auf = Math.Max(auf, m.AscentCm);
            ab = Math.Max(ab, m.DescentCm);
            zeilenhoehe = Math.Max(zeilenhoehe, m.LineHeightCm);
        }

        // Die Zeile muss mindestens so hoch sein, wie über und unter der Grundlinie gebraucht
        // wird. Bei reinem Text ändert das nichts — der Zeilenvorschub ist ohnehin größer.
        // Bei einer Grafik ist es der ganze Unterschied.
        zeilenhoehe = Math.Max(zeilenhoehe, auf + ab);

        zeile.BaselineCm = auf;
        zeile.HeightCm = zeilenhoehe * zeilenabstand;
    }
}
