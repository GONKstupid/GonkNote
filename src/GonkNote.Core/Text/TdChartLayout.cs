using System.Globalization;

namespace GonkNote.Core.Text;

/// <summary>Wo eine Beschriftung an ihrem Ort hängt.</summary>
public enum TdChartAnchor
{
    Links,
    Mitte,
    Rechts,
}

/// <summary>Ein Kasten in Zentimetern, gezählt ab der linken oberen Ecke des Diagramms.</summary>
public readonly record struct TdChartBox(double XCm, double YCm, double WidthCm, double HeightCm)
{
    public double RechtsCm => XCm + WidthCm;
    public double UntenCm => YCm + HeightCm;
    public double MitteXCm => XCm + WidthCm / 2;
    public double MitteYCm => YCm + HeightCm / 2;
}

/// <summary>Ein Ort im Diagramm, in Zentimetern.</summary>
public readonly record struct TdChartPunkt(double XCm, double YCm);

/// <summary>Eine gefüllte Fläche: eine Säule, ein Balken oder ein Legendenkästchen.</summary>
public readonly record struct TdChartFlaeche(TdChartBox Kasten, string Farbe);

/// <summary>Eine gerade Linie: eine Gitterlinie, die Nulllinie oder eine Netzspeiche.</summary>
public readonly record struct TdChartStrich(
    TdChartPunkt Von, TdChartPunkt Bis, string Farbe, double StaerkeCm);

/// <summary>
/// Ein Linienzug: eine Kurve, ein Netzring oder das Polygon einer Reihe im Netz.
/// </summary>
/// <param name="Fuellung">
/// Deckkraft der Füllfläche, 0 = keine. Nur das Netz füllt; eine gefüllte Kurve wäre ein
/// Flächendiagramm, und das kennt <see cref="TdChartKind"/> nicht.
/// </param>
/// <param name="MarkenRadiusCm">Radius der Punktmarken — nur wenn <paramref name="Marken"/>.</param>
public sealed record TdChartZug(
    IReadOnlyList<TdChartPunkt> Punkte,
    string Farbe,
    double StaerkeCm,
    bool Geschlossen = false,
    bool Linie = true,
    bool Marken = false,
    double Fuellung = 0,
    double MarkenRadiusCm = 0);

/// <summary>Ein Kuchenstück. Der Winkel zählt in Grad, im Uhrzeigersinn, 0° zeigt nach rechts.</summary>
public readonly record struct TdChartStueck(
    TdChartPunkt Mitte, double RadiusCm, double StartGrad, double SpanGrad, string Farbe);

/// <summary>
/// Eine Beschriftung.
/// </summary>
/// <param name="YCm">
/// Die <b>Grundlinie</b>, nicht die Oberkante. Der Zeichner gibt sie unverändert an Skia weiter;
/// müsste er sie selbst aus der Schrifthöhe rechnen, hinge der Ort einer Achsenbeschriftung an
/// der Schriftausstattung des Rechners — und damit säße sie unter Linux woanders als unter
/// Windows (§4.16).
/// </param>
/// <param name="HoechstensCm">
/// So breit darf sie werden; 0 = keine Grenze. <b>Die Grenze steht in der Rechnung, das
/// Kürzen im Zeichner</b> — wie viel Text hineinpasst, weiß nur, wer messen kann.
/// </param>
public readonly record struct TdChartSchrift(
    string Text,
    double XCm,
    double YCm,
    double GroesseCm,
    string Farbe,
    TdChartAnchor Anker = TdChartAnchor.Mitte,
    bool Fett = false,
    double HoechstensCm = 0);

/// <summary>
/// Die Werteachse: von <see cref="Min"/> bis <see cref="Max"/> in <see cref="Stufen"/> Schritten.
///
/// <para>
/// <b>Die Grenzen sind Vielfache von <see cref="Schritt"/></b>, und <see cref="Min"/> ist nie
/// größer als null. Beides zusammen heißt: <b>die Null liegt immer auf einer Stufe</b>. Eine
/// Säule wächst aus der Nulllinie; läge die Null zwischen zwei Stufen, stünde die Grundlinie
/// aller Säulen an einer Stelle, an der keine Linie ist.
/// </para>
/// </summary>
public readonly record struct TdChartAchse(double Min, double Max, double Schritt, int Stufen)
{
    /// <summary>Der Wert an der Stufe <paramref name="i"/> — 0 ist unten.</summary>
    public double Wert(int i) => Min + Schritt * i;

    /// <summary>Die Spanne. Nie null, sonst teilte die Umrechnung durch null.</summary>
    public double Spanne => Math.Max(Max - Min, 1e-9);

    /// <summary>
    /// Wo ein Wert zwischen 0 (unten) und 1 (oben) liegt. <b>Beschnitten</b>: ein Wert
    /// außerhalb der Achse kommt aus einer Reihe, die nach dem Rechnen der Achse gewachsen ist —
    /// ein Balken, der aus dem Diagramm herausragt, sähe aus wie ein Zeichenfehler.
    /// </summary>
    public double Anteil(double wert) => Math.Clamp((wert - Min) / Spanne, 0, 1);
}

/// <summary>
/// Ein fertig gerechnetes Diagramm: Flächen, Striche, Züge, Stücke und Beschriftungen, alle in
/// Zentimetern und alle gezählt ab der linken oberen Ecke des Diagrammkastens.
///
/// <para>
/// <b>Die Reihenfolge, in der gezeichnet werden muss</b>, ist die Reihenfolge der Listen hier:
/// Striche (Gitter) liegen unter den Flächen, die Beschriftung liegt über allem. Wer die
/// Reihenfolge umdreht, bekommt Gitterlinien quer über den Säulen.
/// </para>
/// </summary>
public sealed class TdChartPlan
{
    /// <summary>Die Zeichenfläche ohne Titel, Legende und Beschriftungsränder.</summary>
    public TdChartBox Flaeche { get; init; }

    /// <summary>Die Werteachse — <c>null</c> beim Kuchen, der keine hat.</summary>
    public TdChartAchse? Achse { get; init; }

    public IReadOnlyList<TdChartStrich> Striche { get; init; } = [];
    public IReadOnlyList<TdChartFlaeche> Flaechen { get; init; } = [];
    public IReadOnlyList<TdChartStueck> Stuecke { get; init; } = [];
    public IReadOnlyList<TdChartZug> Zuege { get; init; } = [];
    public IReadOnlyList<TdChartSchrift> Schriften { get; init; } = [];

    /// <summary>
    /// Hat dieses Diagramm überhaupt etwas zu zeigen?
    ///
    /// <para>
    /// <b>Nein heißt: der Zeichner setzt den Platzhalterkasten</b> statt ein leeres Achsenkreuz
    /// (§4.24). Ein Diagramm ohne Reihen, ein Kuchen aus lauter Nullen und ein Netz mit zwei
    /// Kategorien haben alle dasselbe Problem — es gibt kein Bild dieser Zahlen. Ein Kasten sagt
    /// „hier fehlt etwas"; leere Achsen sagten „hier ist alles in Ordnung".
    /// </para>
    /// </summary>
    public bool IstLeer => Flaechen.Count == 0 && Stuecke.Count == 0 && Zuege.Count == 0;
}

/// <summary>
/// Rechnet aus den Zahlen eines <see cref="TdChart"/> ein Bild — in Zentimetern, ohne eine
/// einzige Zeile Skia.
///
/// <para>
/// <b>Warum das hier steht und nicht im Zeichner.</b> Zum vierten Mal dasselbe Muster nach der
/// Listennummer (§4.17), dem Feld (§4.20) und dem Diagramm selbst (§4.21): <b>Was sich ableiten
/// lässt, wird gerechnet — und gerechnet wird dort, wo es geprüft werden kann.</b> Achsenteilung,
/// Farbvergabe und die Frage, ob eine Legende nötig ist, sind Zahlen; im Zeichner stünden
/// sie zwischen Skia-Aufrufen und ließen sich nur noch über Pixel prüfen. Der heutige Editor
/// macht genau das (<c>ChartDialog</c>), und deshalb sieht dort niemand, wie er rundet.
/// </para>
///
/// <para>
/// <b>Alles in Zentimetern, nichts in Pixeln</b> — dieselbe Regel wie beim Umbruch (§4.16). Der
/// Maßstab kommt erst beim Zeichnen dazu; ein in Pixeln gerechnetes Diagramm sähe bei jeder
/// Zoomstufe anders aus, und beim Druck mit 300 dpi wäre seine Beschriftung ein Haar.
/// </para>
///
/// <para>
/// <b>Und ohne <see cref="ITdTextMeasure"/>.</b> Die Breite einer Achsenbeschriftung wird
/// **geschätzt** (<see cref="TextBreiteCm"/>) und nicht gemessen. Das ist Absicht: gemessen hinge
/// die Lage der Zeichenfläche an der Schriftausstattung des Rechners, und dasselbe Dokument
/// bekäme unter Linux ein anders geteiltes Diagramm als unter Windows — genau die Falle, wegen
/// der schon der Umbruch seine Naht hat. Geschätzt wird nur der <b>Platz</b>; ob der Text
/// hineinpasst, entscheidet der Zeichner, der messen kann.
/// </para>
/// </summary>
public static class TdChartLayout
{
    /// <summary>Die Farbe von Gitter und Netz. Wie im heutigen Editor.</summary>
    public const string Gitter = "#D4DEEA";

    /// <summary>Die Farbe der Beschriftung.</summary>
    public const string Beschriftung = "#6B7A99";

    /// <summary>Die Farbe des Titels — dieselbe Tinte wie im Fließtext.</summary>
    public const string Tinte = "#1B2B4B";

    /// <summary>So viele Stufen hat eine Achse anzustreben — vier Abschnitte, fünf Linien.</summary>
    private const int Wunschstufen = 4;

    /// <summary>Ein Netz unter drei Ecken ist kein Netz, sondern eine Strecke.</summary>
    public const int NetzMindestens = 3;

    /// <summary>
    /// Die geschätzte Breite eines Textes. <b>Ein Mittelwert und keine Messung</b> — für
    /// Achsenzahlen und kurze Kategorien reicht er, und er ist auf jedem System derselbe.
    /// </summary>
    public static double TextBreiteCm(string text, double groesseCm) =>
        text.Length * groesseCm * 0.55;

    /// <summary>
    /// Die nächstgrößere „schöne" Schrittweite: 1, 2, 5 oder 10 mal einer Zehnerpotenz.
    /// Ohne sie stünde an der Achse „2,3333" — eine Zahl, die niemand als Teilung liest.
    /// </summary>
    public static double SchoenerSchritt(double roh)
    {
        double groesse = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(roh, 1e-6))));
        double norm = roh / groesse;
        double schoen = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10;
        return schoen * groesse;
    }

    /// <summary>
    /// Die Achse zu einer Menge von Werten.
    /// <para>
    /// <b>Die Null ist immer dabei</b>, auch wenn alle Werte darüber liegen: Eine Säule, die bei
    /// 98 anfängt und bei 100 aufhört, sieht doppelt so hoch aus wie eine, die bei 99 endet.
    /// Das ist die bekannteste Art, mit einem richtigen Diagramm etwas Falsches zu behaupten.
    /// </para>
    /// </summary>
    public static TdChartAchse AchseFuer(IEnumerable<double> werte)
    {
        double min = 0, max = 0;
        bool etwas = false;

        foreach (double w in werte)
        {
            if (double.IsNaN(w) || double.IsInfinity(w)) continue;
            if (!etwas) { min = Math.Min(0, w); max = Math.Max(0, w); etwas = true; continue; }
            min = Math.Min(min, w);
            max = Math.Max(max, w);
        }

        // Lauter Nullen (oder gar nichts) — eine Achse von 0 bis 1, damit die Nulllinie
        // irgendwo liegt. Ohne die Untergrenze teilte die Umrechnung durch null.
        double spanne = Math.Max(max - min, 1e-9);
        if (spanne < 1e-6) { max = min + 1; spanne = 1; }

        double schritt = SchoenerSchritt(spanne / Wunschstufen);
        double achsenMin = Math.Floor(min / schritt) * schritt;
        double achsenMax = Math.Ceiling(max / schritt) * schritt;

        // Deckt sich Ober- mit Untergrenze (alle Werte genau null), bleibt eine Stufe übrig.
        if (achsenMax - achsenMin < schritt / 2) achsenMax = achsenMin + schritt;

        int stufen = Math.Clamp((int)Math.Round((achsenMax - achsenMin) / schritt), 1, 12);
        return new TdChartAchse(achsenMin, achsenMin + schritt * stufen, schritt, stufen);
    }

    /// <summary>
    /// Eine Zahl an der Achse. <b>Fest und nicht in der Kultur des Rechners</b> — dieselbe
    /// Entscheidung wie beim Datumsmuster (§4.20): Ein Dokument, dessen Diagramm auf dem einen
    /// Rechner „1.5" und auf dem anderen „1,5" zeigt, ist nicht mehr dasselbe Dokument.
    /// </summary>
    public static string Zahl(double wert) => wert.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>
    /// Der Name einer Reihe für die Legende — oder ihre laufende Nummer.
    /// <para>
    /// <b>Keine Erfindung wie „Reihe 2".</b> Gespeichert wird nur, was jemand eingegeben hat
    /// (§4.21); ein deutsches Wort in einer Legende hinge zusätzlich an der Sprache des
    /// Rechners und stünde beim nächsten Öffnen auf Englisch. Dieselbe Antwort wie bei
    /// <see cref="TdChart.Kategorie"/>: die Nummer.
    /// </para>
    /// </summary>
    public static string Reihenname(TdChart d, int index) =>
        index < d.Series.Count && d.Series[index].Name.Length > 0
            ? d.Series[index].Name
            : (index + 1).ToString(CultureInfo.InvariantCulture);

    // ==================== Der Plan ====================

    /// <summary>
    /// Rechnet das Diagramm. Der Nullpunkt ist die linke obere Ecke seines Kastens, die Maße
    /// stehen an <paramref name="d"/> selbst.
    /// </summary>
    public static TdChartPlan Rechnen(TdChart d)
    {
        double breite = d.WidthCm, hoehe = d.HeightCm;
        if (breite <= 0 || hoehe <= 0 || d.Series.Count == 0 || d.Punktzahl() == 0)
            return new TdChartPlan();

        var b = new Bau(d, breite, hoehe);

        b.TitelSetzen();

        switch (d.Kind)
        {
            case TdChartKind.Pie:
                b.KuchenSetzen();
                break;
            case TdChartKind.Radar:
                b.NetzSetzen();
                break;
            case TdChartKind.Bar:
                b.AchsenSetzen(waagerecht: true);
                b.BalkenSetzen();
                break;
            case TdChartKind.Column:
                b.AchsenSetzen(waagerecht: false);
                b.SaeulenSetzen();
                break;
            default:
                b.AchsenSetzen(waagerecht: false);
                b.KurvenSetzen();
                break;
        }

        b.LegendeSetzen();
        return b.Fertig();
    }

    /// <summary>
    /// Der Bauplatz. <b>Eine Klasse und keine zwanzig Parameter</b>: Schriftgrad, Ränder und
    /// Achse hängen voneinander ab und werden von jedem Diagrammtyp gebraucht.
    /// </summary>
    private sealed class Bau
    {
        private readonly TdChart _d;
        private readonly double _breite;
        private readonly double _hoehe;

        private readonly List<TdChartStrich> _striche = [];
        private readonly List<TdChartFlaeche> _flaechen = [];
        private readonly List<TdChartStueck> _stuecke = [];
        private readonly List<TdChartZug> _zuege = [];
        private readonly List<TdChartSchrift> _schriften = [];

        /// <summary>Grundschriftgrad der Beschriftung.</summary>
        private readonly double _schrift;

        /// <summary>Zeilenhöhe für Legende und Beschriftungszeilen.</summary>
        private readonly double _zeile;

        private readonly double _gitterStaerke;
        private readonly double _kurvenStaerke;

        /// <summary>Unterkante des Titelbandes — hier fängt alles andere an.</summary>
        private readonly double _oben;

        /// <summary>Breite des Legendenbandes rechts; 0 = keine Legende.</summary>
        private readonly double _legende;

        private TdChartBox _flaeche;
        private TdChartAchse? _achse;

        public Bau(TdChart d, double breite, double hoehe)
        {
            _d = d;
            _breite = breite;
            _hoehe = hoehe;

            // **Der Schriftgrad wächst mit dem Diagramm und nicht mit dem Maßstab.** Ein festes
            // Maß wäre in einem 3 cm breiten Diagramm eine Überschrift und in einem 15 cm
            // breiten unlesbar. Die Schranken halten beides in einem druckbaren Bereich:
            // 0,2 cm sind knapp 6 pt, 0,5 cm gut 14 pt.
            _schrift = Math.Clamp(Math.Min(hoehe, breite * 0.6) * 0.045, 0.20, 0.50);
            _zeile = _schrift * 1.7;
            _gitterStaerke = Math.Max(0.01, hoehe * 0.003);
            _kurvenStaerke = Math.Max(0.025, hoehe * 0.0075);

            double titelSchrift = _schrift * 1.35;
            _oben = d.Title.Length > 0 ? titelSchrift * 2.2 : _schrift * 0.8;

            _legende = d.Kind == TdChartKind.Pie
                // Der Kuchen führt seine Legende immer mit: die Namen stehen an den Stücken,
                // und dort ist kein Platz. Ein Drittel der Breite, wie im heutigen Editor.
                ? breite * 0.34
                : d.ShowLegend
                    ? Math.Clamp(
                        LaengsterReihenname() + _schrift * 1.8,
                        breite * 0.12,
                        breite * 0.32)
                    : 0;
        }

        private double LaengsterReihenname()
        {
            double breiteste = 0;
            for (int i = 0; i < _d.Series.Count; i++)
                breiteste = Math.Max(breiteste, TextBreiteCm(Reihenname(_d, i), _schrift));
            return breiteste;
        }

        /// <summary>Was nach Titel und Legende übrig bleibt.</summary>
        private TdChartBox Innen => new(
            0, _oben, Math.Max(0.1, _breite - _legende), Math.Max(0.1, _hoehe - _oben));

        public void TitelSetzen()
        {
            if (_d.Title.Length == 0) return;

            double groesse = _schrift * 1.35;
            _schriften.Add(new TdChartSchrift(
                _d.Title, _breite / 2, groesse * 1.4, groesse, Tinte,
                TdChartAnchor.Mitte, Fett: true, HoechstensCm: _breite));
        }

        // ==================== Achsen ====================

        /// <summary>
        /// Zeichenfläche, Gitter und die Beschriftung beider Achsen.
        /// <para>
        /// <b>Waagerecht ist nicht „dasselbe gedreht".</b> Beim Balkendiagramm stehen die
        /// Kategorien links und die Werte unten; der Platz dafür wird an der jeweils anderen
        /// Seite gebraucht. Wer nur die Werte tauscht, bekommt Kategorienamen, die links aus dem
        /// Diagramm herauslaufen.
        /// </para>
        /// </summary>
        public void AchsenSetzen(bool waagerecht)
        {
            var achse = AchseFuer(_d.Series.SelectMany(r => r.Values));
            _achse = achse;

            int n = _d.Punktzahl();
            var innen = Innen;

            double breitesteZahl = 0;
            for (int i = 0; i <= achse.Stufen; i++)
                breitesteZahl = Math.Max(breitesteZahl, TextBreiteCm(Zahl(achse.Wert(i)), _schrift));

            double breitesteKategorie = 0;
            for (int i = 0; i < n; i++)
                breitesteKategorie = Math.Max(breitesteKategorie, TextBreiteCm(_d.Kategorie(i), _schrift));

            double links = waagerecht
                ? Math.Min(breitesteKategorie + _schrift * 0.5, innen.WidthCm * 0.4)
                : Math.Min(breitesteZahl + _schrift * 0.5, innen.WidthCm * 0.4);
            double rechts = waagerecht ? _schrift * 1.2 : _schrift * 0.8;
            double unten = _zeile;
            double obenLuft = _schrift * 0.5;

            _flaeche = new TdChartBox(
                innen.XCm + links,
                innen.YCm + obenLuft,
                Math.Max(0.1, innen.WidthCm - links - rechts),
                Math.Max(0.1, innen.HeightCm - obenLuft - unten));

            if (waagerecht) GitterWaagerecht(achse); else GitterSenkrecht(achse);
            if (waagerecht) KategorienLinks(n); else KategorienUnten(n);
        }

        /// <summary>Säulen, Linien, Punkte: waagerechte Gitterlinien, Zahlen links davon.</summary>
        private void GitterSenkrecht(TdChartAchse achse)
        {
            for (int i = 0; i <= achse.Stufen; i++)
            {
                double wert = achse.Wert(i);
                double y = Y(wert);

                _striche.Add(new TdChartStrich(
                    new TdChartPunkt(_flaeche.XCm, y),
                    new TdChartPunkt(_flaeche.RechtsCm, y),
                    // Die Nulllinie ist keine Gitterlinie: aus ihr wachsen die Säulen, und bei
                    // negativen Werten ist sie die einzige Linie, die etwas bedeutet.
                    Math.Abs(wert) < 1e-9 ? Beschriftung : Gitter,
                    Math.Abs(wert) < 1e-9 ? _gitterStaerke * 1.5 : _gitterStaerke));

                _schriften.Add(new TdChartSchrift(
                    Zahl(wert), _flaeche.XCm - _schrift * 0.3, y + _schrift * 0.35, _schrift,
                    Beschriftung, TdChartAnchor.Rechts, HoechstensCm: _flaeche.XCm));
            }
        }

        /// <summary>Balken: senkrechte Gitterlinien, Zahlen darunter.</summary>
        private void GitterWaagerecht(TdChartAchse achse)
        {
            for (int i = 0; i <= achse.Stufen; i++)
            {
                double wert = achse.Wert(i);
                double x = X(wert);

                _striche.Add(new TdChartStrich(
                    new TdChartPunkt(x, _flaeche.YCm),
                    new TdChartPunkt(x, _flaeche.UntenCm),
                    Math.Abs(wert) < 1e-9 ? Beschriftung : Gitter,
                    Math.Abs(wert) < 1e-9 ? _gitterStaerke * 1.5 : _gitterStaerke));

                _schriften.Add(new TdChartSchrift(
                    Zahl(wert), x, _flaeche.UntenCm + _zeile * 0.75, _schrift,
                    Beschriftung, TdChartAnchor.Mitte, HoechstensCm: _flaeche.WidthCm / Math.Max(1, achse.Stufen)));
            }
        }

        private void KategorienUnten(int n)
        {
            double fach = _flaeche.WidthCm / n;
            for (int i = 0; i < n; i++)
                _schriften.Add(new TdChartSchrift(
                    _d.Kategorie(i), _flaeche.XCm + fach * (i + 0.5), _flaeche.UntenCm + _zeile * 0.75,
                    _schrift, Beschriftung, TdChartAnchor.Mitte, HoechstensCm: fach));
        }

        private void KategorienLinks(int n)
        {
            double fach = _flaeche.HeightCm / n;
            for (int i = 0; i < n; i++)
                _schriften.Add(new TdChartSchrift(
                    _d.Kategorie(i), _flaeche.XCm - _schrift * 0.3,
                    _flaeche.YCm + fach * (i + 0.5) + _schrift * 0.35,
                    _schrift, Beschriftung, TdChartAnchor.Rechts, HoechstensCm: _flaeche.XCm));
        }

        /// <summary>Der Ort eines Wertes auf der senkrechten Achse.</summary>
        private double Y(double wert) =>
            _flaeche.UntenCm - _flaeche.HeightCm * (_achse ?? default).Anteil(wert);

        /// <summary>Der Ort eines Wertes auf der waagerechten Achse.</summary>
        private double X(double wert) =>
            _flaeche.XCm + _flaeche.WidthCm * (_achse ?? default).Anteil(wert);

        /// <summary>
        /// Der Wert einer Reihe an einer Stelle — oder <c>null</c>, wenn sie dort keinen hat.
        /// <b>Kein stillschweigendes Null:</b> eine kürzere Reihe hat an dieser Kategorie
        /// nichts zu sagen, und eine Säule der Höhe null behauptete, sie habe dort den Wert 0.
        /// </summary>
        private double? Wert(int reihe, int punkt)
        {
            var werte = _d.Series[reihe].Values;
            if (punkt >= werte.Count) return null;
            double w = werte[punkt];
            return double.IsNaN(w) || double.IsInfinity(w) ? null : w;
        }

        /// <summary>
        /// Die Farbe eines Elements. <b>Die Unterscheidung steht am Diagramm</b>
        /// (<see cref="TdChart.FarbeJeElement"/>, §4.21) und nicht hier — sie gilt auch für
        /// DOCX, und zwei Antworten auf dieselbe Frage wären zwei Diagramme.
        /// </summary>
        private string Farbe(int reihe, int punkt) => _d.Farbe(_d.FarbeJeElement ? punkt : reihe);

        // ==================== Säulen und Balken ====================

        public void SaeulenSetzen()
        {
            int n = _d.Punktzahl(), m = _d.Series.Count;
            double fach = _flaeche.WidthCm / n;
            double dicke = fach * 0.7 / m;
            double null_ = Y(0);

            for (int i = 0; i < n; i++)
            {
                double x = _flaeche.XCm + fach * i + fach * 0.15;
                for (int r = 0; r < m; r++)
                {
                    if (Wert(r, i) is not { } wert) continue;

                    double y = Y(wert);
                    // **Aus der Nulllinie nach oben oder nach unten.** Ein negativer Wert hängt
                    // unter der Achse, statt am Boden abgeschnitten zu werden.
                    _flaechen.Add(new TdChartFlaeche(
                        new TdChartBox(x + r * dicke, Math.Min(y, null_), dicke * 0.92, Math.Abs(null_ - y)),
                        Farbe(r, i)));
                }
            }
        }

        public void BalkenSetzen()
        {
            int n = _d.Punktzahl(), m = _d.Series.Count;
            double fach = _flaeche.HeightCm / n;
            double dicke = fach * 0.7 / m;
            double null_ = X(0);

            for (int i = 0; i < n; i++)
            {
                double y = _flaeche.YCm + fach * i + fach * 0.15;
                for (int r = 0; r < m; r++)
                {
                    if (Wert(r, i) is not { } wert) continue;

                    double x = X(wert);
                    _flaechen.Add(new TdChartFlaeche(
                        new TdChartBox(Math.Min(x, null_), y + r * dicke, Math.Abs(x - null_), dicke * 0.92),
                        Farbe(r, i)));
                }
            }
        }

        // ==================== Linie, Punkt, Punkt+Linie ====================

        /// <summary>
        /// <b>Die drei unterscheiden sich nur darin, ob eine Linie und ob Marken dastehen</b> —
        /// genau wie in DOCX, wo alle drei ein <c>c:lineChart</c> sind (§4.21).
        /// </summary>
        public void KurvenSetzen()
        {
            bool linie = _d.Kind != TdChartKind.Scatter;
            bool marken = _d.Kind != TdChartKind.Line;

            int n = _d.Punktzahl();
            double fach = _flaeche.WidthCm / n;

            for (int r = 0; r < _d.Series.Count; r++)
            {
                var punkte = new List<TdChartPunkt>();
                for (int i = 0; i < n; i++)
                    if (Wert(r, i) is { } wert)
                        punkte.Add(new TdChartPunkt(_flaeche.XCm + fach * (i + 0.5), Y(wert)));

                if (punkte.Count == 0) continue;

                _zuege.Add(new TdChartZug(
                    punkte, Farbe(r, 0), _kurvenStaerke,
                    Linie: linie, Marken: marken, MarkenRadiusCm: _kurvenStaerke * 1.5));
            }
        }

        // ==================== Kuchen ====================

        public void KuchenSetzen()
        {
            // **Nur die erste Reihe.** Ein Kuchen zeigt Anteile an einem Ganzen; zwei Ganze
            // nebeneinander wären zwei Kuchen. Word liest ihn genauso (§4.21).
            var werte = _d.Series[0].Values;

            double summe = 0;
            foreach (double w in werte)
                if (w > 0 && !double.IsNaN(w) && !double.IsInfinity(w)) summe += w;

            // Ohne positive Werte gibt es keine Anteile — dann bleibt der Platzhalter stehen.
            if (summe <= 0) return;

            var innen = Innen;
            double radius = Math.Min(innen.WidthCm, innen.HeightCm) / 2 - _schrift * 0.6;
            if (radius <= 0) return;

            var mitte = new TdChartPunkt(innen.MitteXCm, innen.MitteYCm);
            _flaeche = new TdChartBox(
                mitte.XCm - radius, mitte.YCm - radius, radius * 2, radius * 2);

            // Bei −90° fängt das erste Stück oben an. Alles andere ließe den Kuchen schief
            // aussehen, ohne dass man auf Anhieb sagen könnte, warum.
            double winkel = -90;
            double legendeY = _oben + _zeile;

            for (int i = 0; i < werte.Count; i++)
            {
                double w = werte[i];
                if (!(w > 0) || double.IsNaN(w) || double.IsInfinity(w)) continue;

                double anteil = w / summe;
                _stuecke.Add(new TdChartStueck(mitte, radius, winkel, anteil * 360, _d.Farbe(i)));
                winkel += anteil * 360;

                if (legendeY + _zeile > _hoehe) continue;   // was nicht mehr hinpasst, fällt weg

                _flaechen.Add(new TdChartFlaeche(
                    new TdChartBox(_breite - _legende + _schrift * 0.4, legendeY, _schrift, _schrift),
                    _d.Farbe(i)));

                _schriften.Add(new TdChartSchrift(
                    $"{_d.Kategorie(i)} · {(anteil * 100).ToString("0", CultureInfo.InvariantCulture)}%",
                    _breite - _legende + _schrift * 1.8, legendeY + _schrift * 0.85, _schrift,
                    Beschriftung, TdChartAnchor.Links,
                    HoechstensCm: _legende - _schrift * 2.2));

                legendeY += _zeile;
            }
        }

        // ==================== Netz ====================

        public void NetzSetzen()
        {
            int n = _d.Punktzahl();

            // **Unter drei Ecken gibt es kein Netz.** Zwei Kategorien ergäben eine Strecke, die
            // wie ein Zeichenfehler aussieht — dann lieber der Platzhalterkasten (§4.24).
            if (n < NetzMindestens) return;

            var achse = AchseFuer(_d.Series.SelectMany(r => r.Values));
            _achse = achse;

            var innen = Innen;
            // Außen um das Netz stehen die Kategorienamen; ohne den Rand dafür laufen sie aus
            // dem Diagramm heraus.
            double radius = Math.Min(innen.WidthCm, innen.HeightCm) / 2 - _zeile;
            if (radius <= 0) return;

            double cx = innen.MitteXCm, cy = innen.MitteYCm;
            _flaeche = new TdChartBox(cx - radius, cy - radius, radius * 2, radius * 2);

            TdChartPunkt Ecke(int i, double anteil) => new(
                cx + radius * anteil * Math.Sin(2 * Math.PI * i / n),
                cy - radius * anteil * Math.Cos(2 * Math.PI * i / n));

            // Die Ringe — von innen nach außen, einer je Stufe.
            for (int ring = 1; ring <= achse.Stufen; ring++)
            {
                double anteil = (double)ring / achse.Stufen;
                var ecken = new List<TdChartPunkt>(n);
                for (int i = 0; i < n; i++) ecken.Add(Ecke(i, anteil));
                _zuege.Add(new TdChartZug(ecken, Gitter, _gitterStaerke, Geschlossen: true));
            }

            // Die Speichen und die Namen außen daneben.
            for (int i = 0; i < n; i++)
            {
                _striche.Add(new TdChartStrich(
                    new TdChartPunkt(cx, cy), Ecke(i, 1), Gitter, _gitterStaerke));

                var beschriftung = Ecke(i, 1.14);
                _schriften.Add(new TdChartSchrift(
                    _d.Kategorie(i), beschriftung.XCm, beschriftung.YCm + _schrift * 0.35, _schrift,
                    Beschriftung, TdChartAnchor.Mitte, HoechstensCm: _zeile * 3));
            }

            // Die Reihen als geschlossene Polygone, halbdurchsichtig gefüllt: zwei Reihen
            // übereinander bleiben so beide sichtbar.
            for (int r = 0; r < _d.Series.Count; r++)
            {
                var ecken = new List<TdChartPunkt>(n);
                for (int i = 0; i < n; i++)
                    // **Eine fehlende Ecke wird zur Achsenuntergrenze und nicht ausgelassen.**
                    // Ein Polygon mit weniger Ecken als das Netz wäre ein anderes Vieleck und
                    // stünde an ganz anderer Stelle.
                    ecken.Add(Ecke(i, achse.Anteil(Wert(r, i) ?? achse.Min)));

                _zuege.Add(new TdChartZug(
                    ecken, Farbe(r, 0), _kurvenStaerke, Geschlossen: true, Fuellung: 0.18));
            }
        }

        // ==================== Legende ====================

        public void LegendeSetzen()
        {
            // Der Kuchen hat seine Legende schon (die Anteile stehen darin); ob überhaupt eine
            // nötig ist, weiß das Diagramm selbst (§4.21).
            if (_legende <= 0 || _d.Kind == TdChartKind.Pie || !_d.ShowLegend) return;

            double x = _breite - _legende + _schrift * 0.4;
            double y = _oben + _zeile;

            for (int r = 0; r < _d.Series.Count; r++)
            {
                if (y + _zeile > _hoehe) break;

                _flaechen.Add(new TdChartFlaeche(
                    new TdChartBox(x, y, _schrift, _schrift), _d.Farbe(r)));

                _schriften.Add(new TdChartSchrift(
                    Reihenname(_d, r), x + _schrift * 1.4, y + _schrift * 0.85, _schrift,
                    Beschriftung, TdChartAnchor.Links, HoechstensCm: _legende - _schrift * 1.8));

                y += _zeile;
            }
        }

        public TdChartPlan Fertig() => new()
        {
            Flaeche = _flaeche,
            Achse = _achse,
            Striche = _striche,
            Flaechen = _flaechen,
            Stuecke = _stuecke,
            Zuege = _zuege,
            Schriften = _schriften,
        };
    }
}
