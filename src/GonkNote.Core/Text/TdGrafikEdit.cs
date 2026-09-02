namespace GonkNote.Core.Text;

/// <summary>
/// <b>Bilder und Diagramme im Text anfassen</b> — einfügen, größer und kleiner machen,
/// beschriften (§4.89).
///
/// <para>
/// <b>Sie rechnet fast nichts, und trotzdem gehört sie nach Core.</b> Was hier steht, sind
/// nicht Formeln, sondern <b>Regeln</b>: welcher Absatz eine Beschriftung bekommt, wie eine
/// Grafik ihr Seitenverhältnis behält, wo die Untergrenze liegt. Stünde das im Kopf, stünde es
/// beim nächsten Kopf ein zweites Mal — der Fall, der in §4.77, §4.78, §4.82 und §4.88
/// eingetreten ist.
/// </para>
/// <para>
/// <b>Alles läuft über <see cref="TdChange"/></b> und damit über den Verlauf. Eine Grafik an
/// Ort und Stelle zu vergrößern wäre eine Zeile weniger und dieselbe Falle wie in §4.32: die
/// Sicherung im Rückgängig-Stapel zeigt auf dasselbe Objekt, und ein Strg+Z brächte die alte
/// Größe nicht zurück.
/// </para>
/// </summary>
public static class TdGrafikEdit
{
    /// <summary>
    /// Die kleinste Kantenlänge in Zentimetern. <b>Ohne sie schrumpft ein Bild bei genug
    /// Klicks auf null</b> und ist danach unsichtbar und nicht mehr anklickbar — also weg,
    /// ohne dass jemand es gelöscht hätte.
    /// </summary>
    public const double MindestCm = 0.5;

    /// <summary>Die größte Kantenlänge — ein Bild, das breiter als jedes Papier ist, ist ein Versehen.</summary>
    public const double HoechstCm = 100;

    /// <summary>
    /// Die Grafik, die eine Auswahl meint — <c>null</c>, wenn keine.
    ///
    /// <para>
    /// <b>Gesucht wird an der Stelle *und* links davon.</b> Eine Grafik ist ein unteilbares
    /// Stück von einem Schritt Breite; wer sie anklickt, landet je nach Klickhälfte davor oder
    /// dahinter (§4.30). Nur an der Stelle zu suchen hieße: der Knopf tut mal etwas und mal
    /// nicht, je nachdem, wo genau der Nutzer getroffen hat.
    /// </para>
    /// </summary>
    public static (TdGraphic Grafik, TdPosition Stelle)? GrafikAn(
        TdDocument doc, TdSelection auswahl)
    {
        var stelle = TdCursor.Normalisieren(doc, auswahl).Start;

        if (TdCursor.StueckAn(doc, stelle) is TdGraphic hier) return (hier, stelle);

        if (TdCursor.AbsatzAn(doc, stelle.Paragraph) is not { } absatz) return null;

        int links = TdCursor.Linear(absatz, stelle) - 1;
        if (links < 0) return null;

        var davor = TdCursor.AusLinear(absatz, stelle.Paragraph, links);
        return TdCursor.StueckAn(doc, davor) is TdGraphic links_ ? (links_, davor) : null;
    }

    /// <summary>
    /// Fügt eine Grafik als <b>eigenen Absatz</b> ein.
    ///
    /// <para>
    /// <b>Ein eigener Absatz und nicht in den laufenden Text.</b> Ein Bild von acht Zentimetern
    /// mitten in einer Zeile macht diese Zeile acht Zentimeter hoch und den Absatz unlesbar.
    /// Word tut dasselbe, wenn man ein Bild einfügt, und wer es wirklich in den Text will,
    /// zieht es dorthin — das ist ein Handgriff, den es hier noch nicht gibt und der dann einer
    /// ist, keine Nebenwirkung.
    /// </para>
    /// </summary>
    public static TdChange? Einfuegen(TdDocument doc, TdSelection auswahl, TdGraphic grafik) =>
        TdBlockEdit.Einfuegen(doc, auswahl, new TdParagraph([grafik]));

    /// <summary>
    /// Ändert die Größe der Grafik an der Auswahl um einen Faktor je Richtung.
    ///
    /// <para>
    /// <b>Zwei Faktoren und nicht einer</b>, weil „größer" und „breiter" verschiedene Wünsche
    /// sind: <c>(1,15 · 1,15)</c> behält das Seitenverhältnis, <c>(1,15 · 1,0)</c> zieht in die
    /// Breite. Dieselben Werte wie drüben (<c>ResizeCurrent</c>) — beim Editor ist Windows die
    /// Vorlage (§6).
    /// </para>
    /// <para>
    /// <b>Eine Grafik ohne gesetzte Größe wird nicht angefasst.</b> <c>0 · 1,15</c> bliebe 0,
    /// und der Knopf sähe kaputt aus, obwohl er richtig rechnet.
    /// </para>
    /// </summary>
    public static TdChange? Groesse(
        TdDocument doc, TdSelection auswahl, double breiteMal, double hoeheMal)
    {
        if (GrafikAn(doc, auswahl) is not { } gefunden) return null;
        var grafik = gefunden.Grafik;
        if (grafik.WidthCm <= 0 || grafik.HeightCm <= 0) return null;

        return GroesseSetzen(
            doc, auswahl, grafik.WidthCm * breiteMal, grafik.HeightCm * hoeheMal);
    }

    /// <summary>
    /// Setzt die Größe der Grafik an der Auswahl auf feste Werte — der Weg hinter „Genaue
    /// Größe…". Beide Kanten werden auf <see cref="MindestCm"/>…<see cref="HoechstCm"/>
    /// begrenzt.
    /// </summary>
    public static TdChange? GroesseSetzen(
        TdDocument doc, TdSelection auswahl, double breiteCm, double hoeheCm)
    {
        if (GrafikAn(doc, auswahl) is not { } gefunden) return null;
        var (grafik, stelle) = gefunden;

        double breite = Math.Clamp(breiteCm, MindestCm, HoechstCm);
        double hoehe = Math.Clamp(hoeheCm, MindestCm, HoechstCm);

        if (Math.Abs(breite - grafik.WidthCm) < 0.001 &&
            Math.Abs(hoehe - grafik.HeightCm) < 0.001) return null;

        // **Der ganze Schritt wird ersetzt**, nicht die Grafik umgestellt — derselbe Weg, den
        // das Ändern eines Diagramms nimmt (§4.83), und aus demselben Grund (§4.32).
        var ganzes = new TdSelection(stelle with { Offset = 0 }, stelle with { Offset = 1 });
        return TdEdit.Ersetzen(doc, ganzes, TdFragment.Stuecke(Mit(grafik, breite, hoehe)));
    }

    /// <summary>
    /// Eine Kopie der Grafik in neuer Größe. <b>Kopie und nicht Umstellung</b> — §4.32.
    /// </summary>
    private static TdGraphic Mit(TdGraphic grafik, double breiteCm, double hoeheCm)
    {
        TdGraphic neu = grafik switch
        {
            TdImage bild => new TdImage(bild.BlobId, bild.Extension, breiteCm, hoeheCm),
            TdChart diagramm => new TdChart
            {
                Kind = diagramm.Kind,
                Title = diagramm.Title,
                Categories = [.. diagramm.Categories],
                Series = [.. diagramm.Series.Select(r => new TdChartSeries
                {
                    Name = r.Name,
                    Values = [.. r.Values],
                })],
                Palette = [.. diagramm.Palette],
                WidthCm = breiteCm,
                HeightCm = hoeheCm,
            },
            _ => throw new NotSupportedException(
                $"Unbekannte Grafik: {grafik.GetType().Name}. Wer eine dritte Art einführt, "
                + "trägt sie hier nach — sonst verliert sie beim ersten Größenklick ihren Inhalt."),
        };

        neu.AltText = grafik.AltText;
        neu.Format = grafik.Format.Kopie();
        return neu;
    }

    /// <summary>
    /// Setzt eine Beschriftung <b>unter</b> den Absatz, in dem die Auswahl steht — „Abbildung
    /// N: ".
    ///
    /// <para>
    /// <b>Die Nummer wird gezählt und nicht gespeichert</b>, und das ist dieselbe Entscheidung
    /// wie bei den Feldern (§4.20): Wer eine Abbildung dazwischenschiebt, will nicht alle
    /// folgenden von Hand nachziehen. Gezählt werden die Absätze, die bereits so anfangen —
    /// **die Zählung steckt also im Text und nicht neben ihm.** Das ist eine bewusste
    /// Vereinfachung gegenüber einem echten <c>SEQ</c>-Feld, und sie ist unten benannt.
    /// </para>
    /// <para>
    /// <b>Der Text ist übersetzt und steht nicht fest.</b> <paramref name="vorsatz"/> kommt aus
    /// der Sprachtabelle — ein festes „Abbildung" stünde in der englischen Fassung genauso da.
    /// </para>
    /// </summary>
    public static TdChange? Beschriftung(TdDocument doc, TdSelection auswahl, string vorsatz)
    {
        var stelle = TdCursor.Normalisieren(doc, auswahl).End;
        if (TdCursor.AbsatzAn(doc, stelle.Paragraph) is null) return null;

        int nummer = 1 + doc.Paragraphs().Count(
            a => a.PlainText().TrimStart().StartsWith(vorsatz + " ", StringComparison.Ordinal));

        var text = new TdRun($"{vorsatz} {nummer}: ", new TdCharFormat
        {
            Italic = true,
            FontSize = 9.5,
        });

        var beschriftung = new TdParagraph([text]);
        beschriftung.Format.Alignment = TdAlign.Center;
        beschriftung.Format.SpaceAfterPt = 10;

        // **Ans Absatzende und dann einfügen** — `Einfuegen` teilt an der Auswahl, und geteilt
        // am Ende heißt: der Absatz bleibt ganz, die Beschriftung steht darunter.
        int laenge = TdCursor.Laenge(TdCursor.AbsatzAn(doc, stelle.Paragraph)!);
        var ende = new TdSelection(
            TdCursor.AusLinear(TdCursor.AbsatzAn(doc, stelle.Paragraph)!, stelle.Paragraph, laenge));

        return TdBlockEdit.Einfuegen(doc, ende, beschriftung);
    }
}
