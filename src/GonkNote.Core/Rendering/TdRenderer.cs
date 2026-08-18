using GonkNote.Core.Text;
using SkiaSharp;

namespace GonkNote.Core.Rendering;

/// <summary>
/// Was der Zeichner über das Dokument hinaus wissen muss.
/// </summary>
/// <param name="Bilder">
/// Woher die Bytes eines Bildes kommen — dieselbe Naht wie beim Export (<see cref="ITdImages"/>,
/// §4.21). <c>null</c> ist erlaubt: dann bleibt jedes Bild ein Rahmen mit seinem
/// Alternativtext, und das Dokument wird trotzdem gezeichnet.
/// </param>
/// <param name="Felder">
/// Datum und Titel für Kopf- und Fußzeile. <b>Core fragt die Uhr nicht selbst</b> (§4.20) —
/// wer zeichnet, gibt sie mit.
/// </param>
/// <param name="Seitenzahl">
/// Die Gesamtzahl der Seiten für <c>{SEITEN}</c>. <c>null</c> = noch nicht bekannt; dann bleibt
/// der Platzhalter leer, statt eine falsche Zahl zu behaupten.
/// </param>
/// <param name="Markierung">
/// Auswahl und Schreibmarke, **fertig gerechnet** von <see cref="TdHit"/> — <c>null</c> beim
/// Drucken, beim Export und überall dort, wo niemand schreibt.
///
/// <para>
/// <b>Der Zeichner rechnet sie nicht selbst aus</b>, und das ist dieselbe Trennung wie beim
/// Umbruch und beim Diagramm (§4.16, §4.25): Wo etwas hingehört, ist eine Rechnung in
/// Zentimetern und gehört nach <c>Core/Text</c>; hier werden daraus zwei Rechtecke.
/// </para>
/// </param>
public readonly record struct TdRenderContext(
    ITdImages? Bilder = null,
    TdFieldContext? Felder = null,
    int? Seitenzahl = null,
    TdMarkierung? Markierung = null);

/// <summary>
/// Der Zeichner: aus einer gesetzten Seite (<see cref="TdPage"/>) werden Pixel.
///
/// <para>
/// <b>Er rechnet nichts.</b> Jede Zahl, die er braucht, steht schon im Umbruch — Ort und Maß in
/// Zentimetern, das Zeichenformat bereits aufgelöst (§4.16). Was hier passiert, ist die
/// Umrechnung in Pixel und der Aufruf von Skia, und sonst nichts. Diese Trennung ist der Grund,
/// warum der Umbruch in Zentimetern rechnet: **eine Zoomstufe darf den Umbruch nicht ändern**,
/// sonst bricht ein Dokument bei 150 % an anderer Stelle um als beim Drucken.
/// </para>
///
/// <para>
/// <b>Er steht in Core und ist damit für beide Köpfe da</b> — die Faustregel aus §3. SkiaSharp
/// läuft unter Windows wie unter Linux; genau deshalb misst schon der Umbruch mit
/// <see cref="TdSkiaMeasure"/>. Eine Zeile, die mit einer anderen Schriftmaschine gemessen als
/// gezeichnet wird, bricht an einer Stelle um und steht an einer anderen.
/// </para>
///
/// <para>
/// <b>Auch das Diagramm rechnet er nicht</b> (§4.25): Achsenteilung, Farbvergabe, Legende und
/// jeder Ort kommen aus <see cref="TdChartLayout"/> — in Zentimetern, wie alles andere. Hier
/// werden daraus Rechtecke, Bögen, Linienzüge und Beschriftungen.
/// </para>
/// <para>
/// <b>Was noch nicht geht, verschwindet nicht still</b> (§7): Gibt es aus den Zahlen kein Bild
/// (keine Reihen, ein Kuchen aus lauter Nullen, ein Netz mit zwei Kategorien), bleibt der
/// Platzhalterkasten stehen. Ein Kasten sagt „hier fehlt etwas"; eine Leerstelle sagt
/// „hier war nie etwas".
/// </para>
/// </summary>
public static class TdRenderer
{
    /// <summary>Ein Zoll sind 2,54 cm; ein Bildschirmpunkt ist 1/96 Zoll.</summary>
    public const double PixelProCm = 96.0 / 2.54;

    /// <summary>Punkt → Zentimeter, für Linienstärken (ein Punkt ist 1/72 Zoll).</summary>
    // **Öffentlich seit §4.43:** Der Kopf malt den unfertigen Text einer Eingabemethode
    // selbst und braucht dieselbe Umrechnung Punkt → Zentimeter. Eine zweite Zahl im Kopf
    // wäre eine zweite Wahrheit — und Schriftgrößen, die um den Faktor 2,8 danebenliegen.
    public const double CmProPunkt = 2.54 / 72.0;

    /// <summary>
    /// Die Farbe des Papiers. <b>Ein Dokument ist Papier</b> (§1) — es bleibt weiß, auch im
    /// dunklen Thema. Was sich verdunkelt, ist die Fläche **um** die Seite herum, und die
    /// gehört dem Kopf und nicht dem Zeichner.
    /// </summary>
    public static readonly SKColor Papier = SKColors.White;

    /// <summary>
    /// Die Farbe der Auswahl. <b>Dieselbe wie im heutigen WPF-Editor</b>
    /// (<c>PageSelectionBrush</c>, <c>#C7DBFF</c>) und ein fester heller Wert wie das Papier:
    /// Ein Dokument ist Papier (§1), und eine Auswahl, die im dunklen Thema die Farbe wechselt,
    /// wäre auf weißem Grund einmal unlesbar.
    /// </summary>
    public static readonly SKColor Auswahlfarbe = new(0xC7, 0xDB, 0xFF);

    /// <summary>
    /// Zeichnet eine Seite. Der Nullpunkt der Leinwand ist die **obere linke Ecke des
    /// Papiers**, nicht die des Textbereichs — die Ränder rechnet der Zeichner selbst dazu.
    /// </summary>
    /// <param name="massstab">
    /// Pixel je Zentimeter. <see cref="PixelProCm"/> ist die Bildschirmvorgabe; für den Druck
    /// oder eine PNG-Ausgabe steht hier ein Vielfaches davon.
    /// </param>
    public static void Seite(
        SKCanvas leinwand, TdPage seite, double massstab = PixelProCm, TdRenderContext kontext = default)
    {
        var setup = seite.Setup;
        float breite = Px(setup.WidthCm, massstab);
        float hoehe = Px(setup.HeightCm, massstab);

        using (var papier = new SKPaint { Color = Papier, IsAntialias = false })
            leinwand.DrawRect(SKRect.Create(0, 0, breite, hoehe), papier);

        // **Das Wasserzeichen zuerst** — es liegt unter allem anderen. Andersherum stünde der
        // Text darunter und wäre je nach Deckkraft nicht mehr zu lesen.
        WasserzeichenZeichnen(leinwand, setup, massstab, kontext);

        KopfUndFusszeile(leinwand, seite, massstab, kontext);

        // Ab hier zählt alles vom Textbereich aus: genau so legt der Umbruch seine Maße ab.
        leinwand.Save();
        leinwand.Translate(Px(setup.MarginLeftCm, massstab), Px(setup.MarginTopCm, massstab));

        // Erst die Tabellen, dann die Zeilen: beide tragen ihr eigenes YCm, überschneiden sich
        // aber nie. Die Reihenfolge entscheidet nur, was bei einem Fehler oben liegt — und ein
        // Zellhintergrund über dem Text wäre der unangenehmere der beiden.
        foreach (var zeile in seite.TableRows) TabellenzeileZeichnen(leinwand, zeile, massstab, kontext);
        ZeilenZeichnen(leinwand, seite.Lines, setup.TextBreiteCm, massstab, kontext);

        leinwand.Restore();
    }

    /// <summary>Zeichnet alle Seiten untereinander, mit <paramref name="abstandCm"/> dazwischen.</summary>
    public static void Seiten(
        SKCanvas leinwand, TdLayoutResult umbruch, double massstab = PixelProCm,
        TdRenderContext kontext = default, double abstandCm = 0.8)
    {
        // Die Gesamtzahl ist hier bekannt, auch wenn der Aufrufer sie nicht mitgegeben hat —
        // sonst bliebe {SEITEN} leer, obwohl die Antwort danebensteht.
        var mitZahl = kontext with { Seitenzahl = kontext.Seitenzahl ?? umbruch.PageCount };

        float y = 0;
        foreach (var seite in umbruch.Pages)
        {
            leinwand.Save();
            leinwand.Translate(0, y);
            Seite(leinwand, seite, massstab, mitZahl);
            leinwand.Restore();

            y += Px(seite.Setup.HeightCm + abstandCm, massstab);
        }
    }

    // ==================== Zeilen und Textstücke ====================

    /// <summary>
    /// Die Zeilen eines Behälters — der Seite oder einer Tabellenzelle. <b>Der Nullpunkt der
    /// Leinwand ist bereits die Oberkante des Behälters</b>; die Zeilen tragen ihr <c>YCm</c>
    /// von dort aus (§4.19).
    /// </summary>
    /// <param name="breiteCm">
    /// Die Breite des Behälters — nur die Absatzlinie braucht sie: Sie zieht sich über die
    /// Spalte und nicht über den Text (<c>w:pBdr/w:bottom</c> tut dasselbe).
    /// </param>
    private static void ZeilenZeichnen(
        SKCanvas leinwand, IReadOnlyList<TdLine> zeilen, double breiteCm,
        double massstab, TdRenderContext kontext)
    {
        for (int i = 0; i < zeilen.Count; i++)
        {
            var zeile = zeilen[i];
            double grundlinieCm = zeile.YCm + zeile.BaselineCm;

            // **Die Auswahl liegt hinter dem Text und vor dem Zellhintergrund** — deshalb steht
            // sie hier und nicht in `Seite`: Dort wäre sie entweder über den Buchstaben oder
            // unter der Füllfarbe einer Tabellenzelle.
            AuswahlZeichnen(leinwand, zeile, massstab, kontext);

            // Die Aufzählungsmarke steht links vom Text (negatives XCm) und gehört nicht zu den
            // Läufen: sie ist nicht Teil des Textes (§4.17).
            if (zeile.Marker is { } marke) StueckZeichnen(leinwand, marke, grundlinieCm, massstab, kontext);

            foreach (var lauf in zeile.Runs) StueckZeichnen(leinwand, lauf, grundlinieCm, massstab, kontext);

            // **Die Linie steht unter dem letzten Zeilenumbruch des Absatzes, nicht unter jeder
            // Zeile.** Ein Absatz, der über drei Zeilen läuft, hat *eine* Trennlinie — wer sie
            // je Zeile zöge, bekäme liniertes Papier.
            if (LetzteDesAbsatzes(zeilen, i)) AbsatzlinieZeichnen(leinwand, zeile, breiteCm, massstab);

            // Die Schreibmarke zuletzt: Sie gehört über alles, auch über ein Bild.
            SchreibmarkeZeichnen(leinwand, zeile, massstab, kontext);
        }
    }

    /// <summary>
    /// Der blaue Kasten hinter dem ausgewählten Text.
    /// <para>
    /// <b>Er reicht über die ganze Zeilenhöhe</b> und nicht nur über die Buchstaben — sonst
    /// stünden zwischen den Zeilen einer ausgewählten Stelle weiße Streifen, und die Auswahl
    /// sähe zerrissen aus.
    /// </para>
    /// </summary>
    private static void AuswahlZeichnen(
        SKCanvas leinwand, TdLine zeile, double massstab, TdRenderContext kontext)
    {
        if (kontext.Markierung?.Auswahl.TryGetValue(zeile, out var spanne) is not true) return;
        if (spanne.BreiteCm <= 0) return;

        using var pinsel = new SKPaint { Color = Auswahlfarbe, IsAntialias = false };
        leinwand.DrawRect(
            SKRect.Create(
                Px(spanne.VonCm, massstab), Px(zeile.YCm, massstab),
                Px(spanne.BreiteCm, massstab), Px(zeile.HeightCm, massstab)),
            pinsel);
    }

    /// <summary>
    /// Die Schreibmarke: ein senkrechter Strich <b>so hoch wie die Schrift</b>, um die
    /// Grundlinie herum.
    ///
    /// <para>
    /// <b>Und ausdrücklich nicht über die Zeilenhöhe</b> — das war sie bis §4.35, und es fiel
    /// in der Sekunde auf, in der zum ersten Mal wirklich eine Marke auf dem Schirm stand: Die
    /// Zeilenhöhe trägt den Absatzabstand mit (<see cref="TdLine.HeightCm"/>), und zwischen
    /// zwei Absätzen war der Strich damit doppelt so hoch wie die Buchstaben daneben. Er sah
    /// nicht wie ein Cursor aus, sondern wie ein Trennstrich. <b>Der Auswahlkasten bleibt
    /// dagegen zeilenhoch</b> (<see cref="AuswahlZeichnen"/>) — dort ist genau das richtig,
    /// sonst stünden weiße Streifen zwischen den Zeilen.
    /// </para>
    /// <para>
    /// <b>Ob sie blinkt, entscheidet der Kopf</b> — er lässt sie einfach mal mit und mal ohne
    /// Marke zeichnen. Ein Takt gehört nicht in Core: Er wäre eine Uhr (§4.20), und beim Export
    /// stünde plötzlich ein Strich im PDF.
    /// </para>
    /// </summary>
    private static void SchreibmarkeZeichnen(
        SKCanvas leinwand, TdLine zeile, double massstab, TdRenderContext kontext)
    {
        if (kontext.Markierung is not { } markierung) return;
        if (!ReferenceEquals(markierung.MarkeZeile, zeile)) return;

        // In einer leeren Zeile gibt es keinen Lauf, aus dem sich die Schrift ablesen ließe —
        // dann gilt die des Absatzes. Genau dort steht der Cursor beim Schreiben besonders oft.
        var format = zeile.Runs.Count > 0
            ? zeile.Runs[0].Format
            : zeile.Source?.CharFormat.Aufgeloest() ?? TdCharFormat.Standard;

        using var stift = new SKPaint
        {
            Color = Farbe(format.Color) ?? SKColors.Black,
            StrokeWidth = Math.Max(1f, (float)(massstab / PixelProCm)),
            IsAntialias = false,
        };

        using var schrift = SchriftFuer(format, massstab);
        var masse = schrift.Metrics;

        // Skia zählt Ascent nach oben negativ und Descent nach unten positiv — beide werden
        // deshalb addiert und nicht abgezogen.
        float grundlinie = Px(zeile.YCm + zeile.BaselineCm, massstab);
        float x = Px(markierung.MarkeXCm, massstab);

        leinwand.DrawLine(x, grundlinie + masse.Ascent, x, grundlinie + masse.Descent, stift);
    }

    /// <summary>
    /// Ist das die letzte Zeile ihres Absatzes? Verglichen wird der Absatz selbst und nicht
    /// sein Text: zwei aufeinanderfolgende Absätze dürfen gleich lauten.
    /// </summary>
    private static bool LetzteDesAbsatzes(IReadOnlyList<TdLine> zeilen, int i) =>
        zeilen[i].Source is not null &&
        (i + 1 >= zeilen.Count || !ReferenceEquals(zeilen[i + 1].Source, zeilen[i].Source));

    /// <summary>
    /// Die Trennlinie unter einem Absatz. Sie ist im Modell eine Angabe am Absatz und kein
    /// Block für sich (§4.22) — gezeichnet wird sie an der Unterkante seiner letzten Zeile.
    /// </summary>
    private static void AbsatzlinieZeichnen(
        SKCanvas leinwand, TdLine zeile, double breiteCm, double massstab)
    {
        if (zeile.Source?.Format.BottomBorder is not { Sichtbar: true } linie) return;

        using var stift = new SKPaint
        {
            Color = Farbe(linie.Color) ?? SKColors.Gray,
            StrokeWidth = Math.Max(1f, Px(linie.WidthPt * CmProPunkt, massstab)),
            IsAntialias = true,
        };

        float y = Px(zeile.YCm + zeile.HeightCm, massstab);
        leinwand.DrawLine(0, y, Px(breiteCm, massstab), y, stift);
    }

    private static void StueckZeichnen(
        SKCanvas leinwand, TdLaidOutRun lauf, double grundlinieCm, double massstab, TdRenderContext kontext)
    {
        if (lauf.Graphic is { } grafik)
        {
            GrafikZeichnen(leinwand, lauf, grafik, grundlinieCm, massstab, kontext);
            return;
        }

        if (lauf.Text.Length == 0) return;

        var f = lauf.Format;
        using var schrift = SchriftFuer(f, massstab);

        float x = Px(lauf.XCm, massstab);
        float grundlinie = Px(grundlinieCm, massstab);

        // Hoch- und Tiefstellung verschiebt die Grundlinie dieses **einen** Stücks. Die Zeile
        // behält ihre Höhe: der Umbruch hat sie so gemessen, und ein hochgestelltes Zeichen
        // darf die Zeilen darüber nicht auseinanderschieben.
        if (f.VerticalAlign is TdVerticalAlign.Superscript) grundlinie -= schrift.Size * 0.33f;
        else if (f.VerticalAlign is TdVerticalAlign.Subscript) grundlinie += schrift.Size * 0.16f;

        float weite = Px(lauf.WidthCm, massstab);

        // Die Hervorhebung liegt **hinter** dem Text und reicht über die ganze Zeilenhöhe der
        // Schrift — nicht nur über die Buchstaben. `""` heißt „ausdrücklich keine" (§4.14).
        if (f.Highlight is { Length: > 0 } hervor && Farbe(hervor) is { } hinten)
        {
            var m = schrift.Metrics;
            using var pinsel = new SKPaint { Color = hinten, IsAntialias = false };
            leinwand.DrawRect(SKRect.Create(x, grundlinie + m.Ascent, weite, m.Descent - m.Ascent), pinsel);
        }

        using var tinte = new SKPaint
        {
            Color = Farbe(f.Color) ?? SKColors.Black,
            IsAntialias = true,
        };
        leinwand.DrawText(lauf.Text, x, grundlinie, SKTextAlign.Left, schrift, tinte);

        StricheZeichnen(leinwand, f, schrift, x, weite, grundlinie, tinte.Color);
    }

    /// <summary>
    /// Unterstrichen und durchgestrichen. <b>Die Stärke kommt aus der Schrift und ist keine
    /// feste Zahl</b> — ein Strich, der bei 8 pt passt, ist bei 28 pt ein Balken.
    /// </summary>
    private static void StricheZeichnen(
        SKCanvas leinwand, TdCharFormat f, SKFont schrift,
        float x, float weite, float grundlinie, SKColor farbe)
    {
        if (f.Underline != true && f.Strikethrough != true) return;

        var m = schrift.Metrics;
        float staerke = Math.Max(1f, schrift.Size / 14f);
        using var stift = new SKPaint { Color = farbe, StrokeWidth = staerke, IsAntialias = true };

        if (f.Underline == true)
        {
            float y = grundlinie + (m.UnderlinePosition ?? schrift.Size * 0.1f);
            leinwand.DrawLine(x, y, x + weite, y, stift);
        }

        if (f.Strikethrough == true)
        {
            float y = grundlinie + (m.StrikeoutPosition ?? -schrift.Size * 0.28f);
            leinwand.DrawLine(x, y, x + weite, y, stift);
        }
    }

    // ==================== Bilder und Diagramme ====================

    private static void GrafikZeichnen(
        SKCanvas leinwand, TdLaidOutRun lauf, TdGraphic grafik,
        double grundlinieCm, double massstab, TdRenderContext kontext)
    {
        // Eine Grafik sitzt **auf** der Grundlinie: ihre Unterkante ist die Grundlinie, so hat
        // der Umbruch die Zeilenhöhe gerechnet (§4.21).
        var kasten = SKRect.Create(
            Px(lauf.XCm, massstab),
            Px(grundlinieCm - grafik.HeightCm, massstab),
            Px(grafik.WidthCm, massstab),
            Px(grafik.HeightCm, massstab));

        if (grafik is TdImage bild && BildZeichnen(leinwand, bild, kasten, kontext)) return;
        if (grafik is TdChart diagramm && DiagrammZeichnen(leinwand, diagramm, kasten, massstab)) return;

        // **Der benannte Platzhalter.** Für ein Bild bedeutet er „der Blob fehlt" — eine
        // unvollständige Sicherung und kein Programmierfehler (Dauerregel 4). Für ein Diagramm
        // heißt er: aus diesen Zahlen gibt es kein Bild (keine Reihen, ein Kuchen aus lauter
        // Nullen, ein Netz mit zwei Kategorien — <see cref="TdChartPlan.IstLeer"/>). Beides sieht
        // man dem Kasten an, und beides wäre als Leerstelle nicht zu bemerken.
        PlatzhalterZeichnen(leinwand, kasten, Platzhaltertext(grafik), massstab);
    }

    private static bool BildZeichnen(SKCanvas leinwand, TdImage bild, SKRect kasten, TdRenderContext kontext)
    {
        if (kontext.Bilder?.Lesen(bild.BlobId) is not { } bytes) return false;
        using var abbild = Abbild(bytes);
        if (abbild is null) return false;

        using var glaettung = new SKPaint { IsAntialias = true };
        leinwand.DrawImage(abbild, kasten, WbRenderer.HighSampling, glaettung);
        return true;
    }

    /// <summary>Was in einem Platzhalterkasten steht — Titel, Alternativtext oder nichts.</summary>
    private static string Platzhaltertext(TdGraphic grafik) => grafik switch
    {
        TdChart { Title.Length: > 0 } diagramm => diagramm.Title,
        _ => grafik.AltText ?? "",
    };

    /// <summary>
    /// Ein Kasten mit gestricheltem Rand und, wenn Platz ist, einer Beschriftung. Bewusst
    /// schlicht: Er soll auffallen und nicht so aussehen, als gehöre er ins Dokument.
    /// </summary>
    private static void PlatzhalterZeichnen(
        SKCanvas leinwand, SKRect kasten, string text, double massstab)
    {
        if (kasten.Width <= 0 || kasten.Height <= 0) return;

        // Strichlänge und Stärke wachsen mit: ein bei 100 % passender Rahmen wäre beim Druck
        // mit 300 dpi eine haarfeine, fast durchgezogene Linie.
        float einheit = Px(0.1, massstab);
        using var strich = SKPathEffect.CreateDash([einheit * 2, einheit * 1.3f], 0);
        using var rahmen = new SKPaint
        {
            Color = new SKColor(0x99, 0x99, 0x99),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1f, einheit / 3f),
            PathEffect = strich,
            IsAntialias = true,
        };
        leinwand.DrawRect(kasten, rahmen);

        if (text.Length == 0) return;

        // **Auch die Beschriftung skaliert**, sonst bleibt sie bei jeder Zoomstufe gleich klein
        // und verschwindet im Kasten. Die Höhe des Kastens begrenzt sie weiterhin.
        using var schrift = new SKFont(
            WbFonts.Regular, Math.Min(Px(0.35, massstab), kasten.Height / 3f));
        using var tinte = new SKPaint { Color = new SKColor(0x66, 0x66, 0x66), IsAntialias = true };

        // Passt die Beschriftung nicht, bleibt der Kasten leer — abgeschnittener Text sieht aus
        // wie ein Fehler im Dokument und nicht wie eine Lücke im Programm.
        if (schrift.MeasureText(text) > kasten.Width - 8) return;

        leinwand.DrawText(
            text, kasten.MidX, kasten.MidY + schrift.Size / 3f, SKTextAlign.Center, schrift, tinte);
    }

    // ==================== Diagramme ====================

    /// <summary>
    /// Zeichnet ein Diagramm in seinen Kasten — oder gibt <c>false</c> zurück, wenn es aus
    /// diesen Zahlen kein Bild gibt (dann bleibt der Platzhalter).
    ///
    /// <para>
    /// <b>Hier wird nicht gerechnet.</b> Achsenteilung, Farbvergabe, Legende und jeder Ort
    /// stehen fertig in <see cref="TdChartLayout.Rechnen"/> — in Zentimetern, ab der linken
    /// oberen Ecke des Kastens. Was hier passiert, ist die Umrechnung in Pixel und der Aufruf
    /// von Skia, genau wie beim Text (§4.24).
    /// </para>
    /// </summary>
    private static bool DiagrammZeichnen(
        SKCanvas leinwand, TdChart diagramm, SKRect kasten, double massstab)
    {
        var plan = TdChartLayout.Rechnen(diagramm);
        if (plan.IstLeer) return false;

        leinwand.Save();
        leinwand.Translate(kasten.Left, kasten.Top);

        // Die Reihenfolge ist die Reihenfolge der Listen im Plan: Gitter unter den Flächen, die
        // Beschriftung über allem. Andersherum liefen die Gitterlinien quer über die Säulen.
        foreach (var strich in plan.Striche) StrichZeichnen(leinwand, strich, massstab);
        foreach (var flaeche in plan.Flaechen) FlaecheZeichnen(leinwand, flaeche, massstab);
        foreach (var stueck in plan.Stuecke) KuchenstueckZeichnen(leinwand, stueck, massstab);
        foreach (var zug in plan.Zuege) ZugZeichnen(leinwand, zug, massstab);
        foreach (var schrift in plan.Schriften) BeschriftungZeichnen(leinwand, schrift, massstab);

        leinwand.Restore();
        return true;
    }

    private static void FlaecheZeichnen(SKCanvas leinwand, TdChartFlaeche flaeche, double massstab)
    {
        if (Farbe(flaeche.Farbe) is not { } farbe) return;

        var k = Kasten(flaeche.Kasten, massstab);

        // Eine Säule mit dem Wert null ist ein Strich ohne Höhe. **Sie wird trotzdem
        // gezeichnet** — als eine Pixelzeile auf der Nulllinie: dass dort ein Wert steht, ist
        // eine Aussage, und eine Leerstelle wäre die Aussage „hier fehlt eine Zahl".
        if (k.Height < 1) k = SKRect.Create(k.Left, k.Top, k.Width, 1);
        if (k.Width < 1) k = SKRect.Create(k.Left, k.Top, 1, k.Height);

        using var pinsel = new SKPaint { Color = farbe, IsAntialias = true };
        leinwand.DrawRect(k, pinsel);
    }

    private static void StrichZeichnen(SKCanvas leinwand, TdChartStrich strich, double massstab)
    {
        if (Farbe(strich.Farbe) is not { } farbe) return;

        using var stift = new SKPaint
        {
            Color = farbe,
            StrokeWidth = Math.Max(1f, Px(strich.StaerkeCm, massstab)),
            IsAntialias = true,
        };
        leinwand.DrawLine(
            Px(strich.Von.XCm, massstab), Px(strich.Von.YCm, massstab),
            Px(strich.Bis.XCm, massstab), Px(strich.Bis.YCm, massstab), stift);
    }

    /// <summary>
    /// Ein Kuchenstück. <b>Ein volles Rundum wird zum Kreis</b> — ein Bogen über 360° ist in
    /// Skia je nach Weg entweder nichts oder ein Kreis mit einem Schnitt darin, und eine
    /// einzige Kategorie ist der Normalfall, nicht die Ausnahme.
    /// </summary>
    private static void KuchenstueckZeichnen(SKCanvas leinwand, TdChartStueck stueck, double massstab)
    {
        if (Farbe(stueck.Farbe) is not { } farbe) return;

        float mx = Px(stueck.Mitte.XCm, massstab);
        float my = Px(stueck.Mitte.YCm, massstab);
        float r = Px(stueck.RadiusCm, massstab);
        if (r <= 0) return;

        using var pinsel = new SKPaint { Color = farbe, IsAntialias = true };

        if (stueck.SpanGrad >= 359.9)
        {
            leinwand.DrawCircle(mx, my, r, pinsel);
            return;
        }

        using var pfad = new SKPath();
        pfad.MoveTo(mx, my);
        pfad.ArcTo(SKRect.Create(mx - r, my - r, r * 2, r * 2),
            (float)stueck.StartGrad, (float)stueck.SpanGrad, forceMoveTo: false);
        pfad.Close();
        leinwand.DrawPath(pfad, pinsel);
    }

    private static void ZugZeichnen(SKCanvas leinwand, TdChartZug zug, double massstab)
    {
        if (zug.Punkte.Count == 0 || Farbe(zug.Farbe) is not { } farbe) return;

        using var pfad = new SKPath();
        for (int i = 0; i < zug.Punkte.Count; i++)
        {
            float x = Px(zug.Punkte[i].XCm, massstab);
            float y = Px(zug.Punkte[i].YCm, massstab);
            if (i == 0) pfad.MoveTo(x, y); else pfad.LineTo(x, y);
        }
        if (zug.Geschlossen) pfad.Close();

        if (zug.Fuellung > 0)
        {
            using var fuellung = new SKPaint
            {
                Color = farbe.WithAlpha((byte)Math.Clamp(zug.Fuellung * 255, 0, 255)),
                IsAntialias = true,
            };
            leinwand.DrawPath(pfad, fuellung);
        }

        if (zug.Linie)
        {
            using var stift = new SKPaint
            {
                Color = farbe,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(1f, Px(zug.StaerkeCm, massstab)),
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                IsAntialias = true,
            };
            leinwand.DrawPath(pfad, stift);
        }

        if (!zug.Marken) return;

        float radius = Math.Max(1f, Px(zug.MarkenRadiusCm, massstab));
        using var marke = new SKPaint { Color = farbe, IsAntialias = true };
        foreach (var punkt in zug.Punkte)
            leinwand.DrawCircle(Px(punkt.XCm, massstab), Px(punkt.YCm, massstab), radius, marke);
    }

    /// <summary>
    /// Eine Beschriftung des Diagramms.
    ///
    /// <para>
    /// <b>Der Platz steht in der Rechnung, das Hineinpassen entscheidet sich hier</b> — die
    /// Rechnung kann nicht messen (sie soll es auch nicht, §4.16), und ein Kategoriename, der
    /// in die Nachbarspalte hineinragt, sähe aus wie ein verrutschtes Diagramm. Erst wird die
    /// Schrift verkleinert, und erst wenn das nicht reicht, wird gekürzt: eine kleinere Zahl ist
    /// lesbar, eine abgeschnittene ist eine andere Zahl.
    /// </para>
    /// </summary>
    private static void BeschriftungZeichnen(
        SKCanvas leinwand, TdChartSchrift beschriftung, double massstab)
    {
        if (beschriftung.Text.Length == 0) return;

        float groesse = Px(beschriftung.GroesseCm, massstab);
        // Unter einem Pixel Höhe gibt es nichts mehr zu lesen — ein grauer Schleier über der
        // Zeichenfläche sähe aus wie ein Fehler im Bild.
        if (groesse < 3f) return;

        using var schrift = new SKFont(
            beschriftung.Fett ? WbFonts.Bold : WbFonts.Regular, groesse);

        string text = beschriftung.Text;

        if (beschriftung.HoechstensCm > 0)
        {
            float grenze = Px(beschriftung.HoechstensCm, massstab);
            float breite = schrift.MeasureText(text);

            if (breite > grenze)
            {
                // Bis zu einem Drittel kleiner; darunter wird die Beschriftung unleserlich, und
                // dann ist Kürzen das ehrlichere Mittel.
                schrift.Size = groesse * Math.Max(0.66f, grenze / breite);
                text = Gekuerzt(schrift, text, grenze);
            }
        }

        if (text.Length == 0) return;

        using var tinte = new SKPaint
        {
            Color = Farbe(beschriftung.Farbe) ?? SKColors.Gray,
            IsAntialias = true,
        };
        leinwand.DrawText(
            text, Px(beschriftung.XCm, massstab), Px(beschriftung.YCm, massstab),
            beschriftung.Anker switch
            {
                TdChartAnchor.Links => SKTextAlign.Left,
                TdChartAnchor.Rechts => SKTextAlign.Right,
                _ => SKTextAlign.Center,
            },
            schrift, tinte);
    }

    /// <summary>
    /// Der Text, so weit er passt, mit „…" dahinter. <b>Leer, wenn nicht einmal das passt</b> —
    /// ein einzelnes Auslassungszeichen sagt weniger als nichts.
    /// </summary>
    private static string Gekuerzt(SKFont schrift, string text, float grenze)
    {
        if (schrift.MeasureText(text) <= grenze) return text;

        for (int laenge = text.Length - 1; laenge > 0; laenge--)
        {
            string versuch = text[..laenge] + "…";
            if (schrift.MeasureText(versuch) <= grenze) return versuch;
        }
        return "";
    }

    private static SKRect Kasten(TdChartBox box, double massstab) => SKRect.Create(
        Px(box.XCm, massstab), Px(box.YCm, massstab),
        Px(box.WidthCm, massstab), Px(box.HeightCm, massstab));

    // ==================== Tabellen ====================

    private static void TabellenzeileZeichnen(
        SKCanvas leinwand, TdLaidOutRow zeile, double massstab, TdRenderContext kontext)
    {
        var format = zeile.Table?.Format ?? new TdTableFormat();

        foreach (var zelle in zeile.Cells)
        {
            // Eine verbundene Zelle reicht über mehrere Zeilen; ihre Höhe ist die Summe der
            // Zeilen, die sie überdeckt — die kennt hier nur die eigene Zeile, deshalb steht
            // die volle Höhe im Umbruch bereits an der Zeile darüber.
            var kasten = SKRect.Create(
                Px(zelle.XCm, massstab),
                Px(zeile.YCm, massstab),
                Px(zelle.WidthCm, massstab),
                Px(zeile.HeightCm, massstab));

            if (Farbe(zelle.Source?.Shading) is { } fuellung)
            {
                using var pinsel = new SKPaint { Color = fuellung, IsAntialias = false };
                leinwand.DrawRect(kasten, pinsel);
            }

            RahmenZeichnen(leinwand, kasten, format, massstab);

            // **Der Zellinhalt zählt ab der Innenkante der Zelle, nicht ab dem Textbereich.**
            // Der Umbruch setzt ihn als eigene kleine Seite ohne Ränder (§4.19); wer den
            // Innenabstand hier vergisst, bekommt eine Tabelle, deren Text links neben ihr
            // steht — und zwar bei jeder Spalte weiter daneben.
            leinwand.Save();
            leinwand.Translate(
                Px(zelle.XCm + format.CellPaddingLeftCm, massstab),
                Px(zeile.YCm + format.CellPaddingTopCm, massstab));

            double innenCm = Math.Max(
                0, zelle.WidthCm - format.CellPaddingLeftCm - format.CellPaddingRightCm);
            ZeilenZeichnen(leinwand, zelle.Lines, innenCm, massstab, kontext);

            leinwand.Restore();
        }
    }

    /// <summary>
    /// Die vier Linien einer Zelle. <b>Gezeichnet wird je Zelle, obwohl das Format an der
    /// Tabelle hängt</b> (§4.18) — die Innenlinien liegen damit doppelt übereinander, und das
    /// ist richtig so: Eine Linie, die nur einmal gezeichnet würde, fehlte an der letzten
    /// Zelle einer Zeile.
    /// </summary>
    private static void RahmenZeichnen(
        SKCanvas leinwand, SKRect kasten, TdTableFormat format, double massstab)
    {
        Linie(format.Top, kasten.Left, kasten.Top, kasten.Right, kasten.Top);
        Linie(format.Bottom, kasten.Left, kasten.Bottom, kasten.Right, kasten.Bottom);
        Linie(format.Left, kasten.Left, kasten.Top, kasten.Left, kasten.Bottom);
        Linie(format.Right, kasten.Right, kasten.Top, kasten.Right, kasten.Bottom);

        void Linie(TdBorder rand, float x1, float y1, float x2, float y2)
        {
            if (!rand.Sichtbar) return;

            using var stift = new SKPaint
            {
                Color = Farbe(rand.Color) ?? SKColors.Black,
                StrokeWidth = Math.Max(1f, Px(rand.WidthPt * CmProPunkt, massstab)),
                IsAntialias = true,
            };
            leinwand.DrawLine(x1, y1, x2, y2, stift);
        }
    }

    // ==================== Seitenschmuck ====================

    /// <summary>
    /// Kopf- und Fußzeile. <b>Sie stehen nicht im Umbruch</b>, denn sie gehören zur Seite und
    /// nicht zum Textfluss (§4.15) — der Zeichner setzt sie selbst, einzeilig, und löst dabei
    /// die Platzhalter auf. Die Seitenzahl kennt an dieser Stelle ohnehin nur er.
    /// </summary>
    private static void KopfUndFusszeile(
        SKCanvas leinwand, TdPage seite, double massstab, TdRenderContext kontext)
    {
        var setup = seite.Setup;

        // Auf der ersten Seite unterdrückt — die Angabe gilt für **beide** Zeilen, so hält es
        // das Altformat und so hält es DOCX (`titlePg`).
        bool erste = seite.Number <= 1 && setup.SuppressOnFirstPage;
        if (erste) return;

        if (setup.HeaderText.Length > 0)
            SchmuckzeileZeichnen(leinwand, setup.HeaderText, seite, massstab, kontext,
                yCm: setup.MarginTopCm / 2);

        if (setup.FooterText.Length > 0)
            SchmuckzeileZeichnen(leinwand, setup.FooterText, seite, massstab, kontext,
                yCm: setup.HeightCm - setup.MarginBottomCm / 2);
    }

    private static void SchmuckzeileZeichnen(
        SKCanvas leinwand, string vorlage, TdPage seite, double massstab,
        TdRenderContext kontext, double yCm)
    {
        string text = PlatzhalterAufloesen(vorlage, seite.Number, kontext);
        if (text.Length == 0) return;

        using var schrift = new SKFont(WbFonts.Regular, (float)(9 * massstab * CmProPunkt));
        using var tinte = new SKPaint { Color = new SKColor(0x55, 0x55, 0x55), IsAntialias = true };

        leinwand.DrawText(
            text,
            Px(seite.Setup.MarginLeftCm, massstab),
            Px(yCm, massstab),
            SKTextAlign.Left, schrift, tinte);
    }

    /// <summary>
    /// Ersetzt <c>{SEITE}</c>, <c>{SEITEN}</c>, <c>{DATUM}</c> und <c>{TITEL}</c> durch ihre
    /// Werte. <b>Die Zuordnung kommt aus <see cref="TdField.Platzhalter"/></b> und steht nicht
    /// noch einmal hier — sonst gäbe es zwei Listen, und eine davon veraltet (§4.20).
    /// </summary>
    private static string PlatzhalterAufloesen(string vorlage, int seite, TdRenderContext kontext)
    {
        string text = vorlage;
        var felder = kontext.Felder ?? TdFieldContext.Ohne;

        foreach (var (platzhalter, art) in TdField.Platzhalter)
        {
            if (!text.Contains(platzhalter, StringComparison.Ordinal)) continue;

            string wert = TdFieldValues.Text(new TdField(art), felder, seite, kontext.Seitenzahl);
            text = text.Replace(platzhalter, wert, StringComparison.Ordinal);
        }

        return text;
    }

    /// <summary>
    /// Das Wasserzeichen — mittig, über die ganze Seite, mit seiner Deckkraft.
    /// <b>Fehlt der Blob, fehlt das Wasserzeichen und nicht die Seite</b> (Dauerregel 4).
    /// </summary>
    private static void WasserzeichenZeichnen(
        SKCanvas leinwand, TdPageSetup setup, double massstab, TdRenderContext kontext)
    {
        if (setup.Watermark is not { } zeichen) return;
        if (kontext.Bilder?.Lesen(zeichen.BlobId) is not { } bytes) return;

        using var abbild = Abbild(bytes);
        if (abbild is null) return;

        double breiteCm = zeichen.WidthCm > 0 ? zeichen.WidthCm : setup.WidthCm;
        double hoeheCm = zeichen.HeightCm > 0 ? zeichen.HeightCm : setup.HeightCm;

        var kasten = SKRect.Create(
            Px((setup.WidthCm - breiteCm) / 2, massstab),
            Px((setup.HeightCm - hoeheCm) / 2, massstab),
            Px(breiteCm, massstab),
            Px(hoeheCm, massstab));

        byte deckkraft = (byte)Math.Clamp(setup.WatermarkOpacity * 255, 0, 255);
        using var pinsel = new SKPaint
        {
            Color = SKColors.White.WithAlpha(deckkraft),
            IsAntialias = true,
        };
        leinwand.DrawImage(abbild, kasten, WbRenderer.MediumSampling, pinsel);
    }

    // ==================== Hilfsmittel ====================

    private static float Px(double cm, double massstab) => (float)(cm * massstab);

    /// <summary>
    /// Bytes als <see cref="SKImage"/>. <b>Über <see cref="WbImages.Decode"/> und nicht über
    /// <c>SKImage.FromEncodedData</c>:</b> Dort steckt der Absturz aus dem SkiaSharp-3-Umstieg
    /// samt seiner Behebung (§7) — zwei Wege, ein Bild zu dekodieren, wären zwei Stellen, an
    /// denen er wieder auftauchen kann.
    /// </summary>
    private static SKImage? Abbild(byte[] bytes)
    {
        using var bitmap = WbImages.Decode(bytes);
        return bitmap is null ? null : SKImage.FromBitmap(bitmap);
    }

    /// <summary>
    /// Die Schrift zu einem **aufgelösten** Zeichenformat, in Pixeln.
    ///
    /// <para>
    /// <b>Die Umrechnung Punkt → Pixel ist die Stelle, an der Umbruch und Zeichnung
    /// auseinanderlaufen, wenn man sie vergisst.</b> Im Modell steht die Größe in Punkt, und
    /// <see cref="TdSkiaMeasure"/> misst damit in Zentimetern. Die Leinwand rechnet in Pixeln;
    /// eine Schrift, die hier ihre Punktzahl als Pixelzahl bekäme, wäre etwa dreimal zu klein —
    /// der Umbruch hätte für Zeilen gerechnet, die niemand so sieht.
    /// </para>
    /// </summary>
    private static SKFont SchriftFuer(TdCharFormat format, double massstab)
    {
        var f = format.Aufgeloest();

        // **Dieselbe Quelle wie die Messung** (§4.26): `WbFonts` ist der einzige Ort, der aus
        // einem Namen ein Typeface macht — mitgeliefert vor System vor Rückfallkette. Zwei
        // eigene Aufrufe von `SKTypeface.FromFamilyName` sahen nur zufällig gleich aus und
        // hätten eine mitgelieferte Schrift beide nicht gefunden.
        return WbFonts.Font(
            f.FontFamily, Px(f.FontSize!.Value * CmProPunkt, massstab),
            f.Bold == true, f.Italic == true);
    }

    /// <summary>„#RRGGBB" als Farbe — oder <c>null</c>, wenn dort nichts Brauchbares steht.</summary>
    private static SKColor? Farbe(string? hex)
    {
        if (hex is not { Length: > 0 }) return null;
        return SKColor.TryParse(hex, out var farbe) ? farbe : null;
    }
}
