namespace GonkNote.Core.Text;

/// <summary>
/// Die Seiteneinrichtung eines Abschnitts: Blattgröße und Ränder, dazu Kopf- und Fußzeile.
///
/// <para>
/// <b>Maße in Zentimetern, und die Größe ist der einzige Wert.</b> Der heutige Editor legt
/// das Format als Zeichenkette ab (<c>"A4"</c>) und rechnet die Größe daraus aus. Das geht
/// gut, solange niemand eine Größe einstellt, die keinen Namen hat — und es geht schief,
/// sobald ein DOCX aus fremder Hand hereinkommt: dessen <c>sectPr</c> nennt Zahlen, keinen
/// Namen. Hier stehen deshalb die Zahlen, und der Name wird über <see cref="Name"/>
/// **zurückerkannt** statt gespeichert. Zwei Felder, die sich widersprechen können, gibt es
/// damit gar nicht erst.
/// </para>
///
/// <para>
/// <b>Und aus demselben Grund kein Querformat-Schalter.</b> Querformat heißt: die Breite ist
/// größer als die Höhe. Ein zusätzliches <c>bool</c> daneben wäre eine zweite Wahrheit über
/// dieselbe Sache — genau die Doppelung, vor der HANDOFF §4.10 warnt. Wer umschalten will,
/// nimmt <see cref="Quer"/> oder <see cref="Hoch"/>.
/// </para>
/// </summary>
public sealed class TdPageSetup
{
    public double WidthCm { get; set; } = 21.0;
    public double HeightCm { get; set; } = 29.7;

    public double MarginLeftCm { get; set; } = 2;
    public double MarginTopCm { get; set; } = 2;
    public double MarginRightCm { get; set; } = 2;
    public double MarginBottomCm { get; set; } = 2;

    private string _headerText = "";
    private string _footerText = "";

    /// <summary>
    /// Kopf- und Fußzeile. Platzhalter: <c>{SEITE}</c>, <c>{SEITEN}</c>, <c>{DATUM}</c>,
    /// <c>{TITEL}</c> — dieselben, die der heutige Editor kennt. Leer = keine.
    /// <para>
    /// <b>Vorerst Text und nicht Absätze.</b> Eine Kopfzeile ist im Grunde ein kleines
    /// Dokument mit Feldern darin, und Felder sind Schritt 5. Bis dahin wäre ein
    /// Absatzbaum hier ein Versprechen, das die Oberfläche nicht einlösen kann.
    /// </para>
    /// </summary>
    public string HeaderText { get => _headerText; set => _headerText = value ?? ""; }

    /// <inheritdoc cref="HeaderText"/>
    public string FooterText { get => _footerText; set => _footerText = value ?? ""; }

    /// <summary>Kopf-/Fußzeile auf der ersten Seite unterdrücken (Deckblatt).</summary>
    public bool SuppressOnFirstPage { get; set; }

    /// <summary>
    /// Das Wasserzeichen — <c>null</c> = keines.
    ///
    /// <para>
    /// <b>Es steht hier erst seit Schritt 6</b>, und §4.15 hat genau das angekündigt: Ein
    /// Wasserzeichen ist ein **Bild**, und Bilder konnte das Modell bis dahin nicht. In DOCX
    /// ist es ohnehin eine in der **Kopfzeile** verankerte Zeichnung — es gehört also zur
    /// Seiteneinrichtung und nicht zum Inhalt, und es kommt genau dann, wenn das Modell Bilder
    /// kann.
    /// </para>
    /// <para>
    /// Ein <see cref="TdImage"/> und kein eigener Typ: Es ist ein Bild mit einer Größe, und
    /// ein zweiter Typ dafür hätte dieselben drei Felder.
    /// </para>
    /// </summary>
    public TdImage? Watermark { get; set; }

    /// <summary>
    /// Wie kräftig das Wasserzeichen steht: 1 = voll, 0 = unsichtbar.
    ///
    /// <para>
    /// <b>In DOCX gibt es dafür keine Deckkraft.</b> Word blasst ein Wasserzeichen über
    /// Helligkeit und Schwarzwert der Bilddaten auf (<c>gain</c>/<c>blacklevel</c>) — was
    /// dabei herauskommt, sieht aus wie Transparenz, ist aber keine. Der Wert wird deshalb auf
    /// <c>gain</c> abgebildet und von dort zurückgelesen; das ist eine **benannte Näherung**
    /// und keine verlustfreie Umrechnung (§4.21).
    /// </para>
    /// </summary>
    public double WatermarkOpacity { get; set; } = 0.35;

    // ---------------------------------------------------------------- Formate

    public static TdPageSetup A4 => new() { WidthCm = 21.0, HeightCm = 29.7 };
    public static TdPageSetup A5 => new() { WidthCm = 14.8, HeightCm = 21.0 };
    public static TdPageSetup A3 => new() { WidthCm = 29.7, HeightCm = 42.0 };
    public static TdPageSetup Letter => new() { WidthCm = 21.59, HeightCm = 27.94 };

    /// <summary>Ist die Breite größer als die Höhe?</summary>
    public bool IstQuerformat => WidthCm > HeightCm;

    /// <summary>Dreht auf Querformat, falls es nicht schon so liegt.</summary>
    public TdPageSetup Quer()
    {
        if (!IstQuerformat) (WidthCm, HeightCm) = (HeightCm, WidthCm);
        return this;
    }

    /// <summary>Dreht auf Hochformat, falls es nicht schon so liegt.</summary>
    public TdPageSetup Hoch()
    {
        if (IstQuerformat) (WidthCm, HeightCm) = (HeightCm, WidthCm);
        return this;
    }

    /// <summary>
    /// Erkennt ein benanntes Format an seiner Größe zurück — für die Oberfläche, die „A4"
    /// anzeigen will. Passt keines, kommt <c>null</c>: **eine eigene Größe ist kein Fehler**,
    /// sie hat nur keinen Namen.
    /// <para>
    /// Die Toleranz von 0,1 cm ist Absicht: Ein Blatt geht durch DOCX in Twips und wieder
    /// zurück, und wer hier auf Gleichheit prüft, bekommt für ein A4-Blatt irgendwann
    /// „unbekannt".
    /// </para>
    /// </summary>
    public string? Name
    {
        get
        {
            // Immer gegen das Hochformat vergleichen — ein quer liegendes A4 ist A4.
            double kurz = Math.Min(WidthCm, HeightCm), lang = Math.Max(WidthCm, HeightCm);

            foreach (var (name, blatt) in new[]
                     { ("A4", A4), ("A5", A5), ("A3", A3), ("Letter", Letter) })
            {
                if (Math.Abs(kurz - blatt.WidthCm) <= 0.1 && Math.Abs(lang - blatt.HeightCm) <= 0.1)
                    return name;
            }
            return null;
        }
    }

    /// <summary>Die bedruckbare Breite — Blatt minus linkem und rechtem Rand.</summary>
    public double TextBreiteCm => Math.Max(0, WidthCm - MarginLeftCm - MarginRightCm);

    /// <summary>Die bedruckbare Höhe — Blatt minus oberem und unterem Rand.</summary>
    public double TextHoeheCm => Math.Max(0, HeightCm - MarginTopCm - MarginBottomCm);

    public TdPageSetup Kopie() => (TdPageSetup)MemberwiseClone();
}

/// <summary>
/// Ein Abschnitt: eine Folge von Blöcken mit **einer** Seiteneinrichtung.
///
/// <para>
/// <b>Warum es ihn gibt.</b> Ein Dokument kann mehr als eine Seiteneinrichtung haben — das
/// Deckblatt quer, der Rest hoch; ein Anhang mit anderen Rändern. Word kann das über
/// <c>sectPr</c>, und ein importiertes DOCX bringt es mit. Ohne Abschnitt müsste ein Import
/// entweder die zweite Einrichtung wegwerfen oder das Dokument aufteilen.
/// </para>
///
/// <para>
/// <b>Nutzer-Entscheidung 2026-08-04:</b> Die Seiteneinrichtung wandert damit **aus**
/// <c>TextDoc</c> hierher; die Felder dort werden zur Quelle der einmaligen Übernahme. Bis
/// die läuft (nach Schritt 6, siehe HANDOFF §6), stehen beide nebeneinander — das ist
/// <b>nicht</b> die Doppelung aus §4.10, denn sie beschreiben verschiedene Dokumente: die
/// alten im Altformat, die neuen im eigenen. Wer eine davon „aufräumt", bevor die Übernahme
/// steht, macht Bestandsdokumente unlesbar.
/// </para>
/// </summary>
public sealed class TdSection
{
    public List<TdBlock> Blocks { get; set; } = new();

    public TdPageSetup Page { get; set; } = TdPageSetup.A4;

    public TdSection() { }

    public TdSection(params TdBlock[] blocks) => Blocks.AddRange(blocks);
}
