using SkiaSharp;

namespace GonkNote.Core.Text;

/// <summary>Die Maße einer Schrift, in Zentimetern.</summary>
/// <param name="AscentCm">Höhe über der Grundlinie (positiv).</param>
/// <param name="DescentCm">Tiefe unter der Grundlinie (positiv).</param>
/// <param name="LineHeightCm">Der volle Zeilenvorschub inklusive Durchschuss.</param>
public readonly record struct TdFontMetrics(double AscentCm, double DescentCm, double LineHeightCm);

/// <summary>
/// Woher der Umbruch weiß, wie breit ein Text ist.
///
/// <para>
/// <b>Warum das eine Naht ist und kein direkter Skia-Aufruf.</b> Zwei Gründe, und beide
/// zählen:
/// </para>
/// <para>
/// <b>1. Die Schriften unterscheiden sich je System.</b> „Segoe UI" gibt es unter Linux
/// nicht — dieselbe Falle wie bei der Icon-Schrift (HANDOFF §7). Ein Umbruch-Test, der echte
/// Schriftbreiten misst, hätte auf Windows und im Linux-Lauf der CI **verschiedene**
/// Ergebnisse und wäre nach dem ersten falschen Alarm abgeschaltet. Genau deshalb nimmt
/// HANDOFF §4.6 die Schrift schon aus den Renderer-Snapshots heraus.
/// </para>
/// <para>
/// <b>2. Der Umbruch ist Rechnung, kein Zeichnen.</b> Mit einer Naht davor lässt sich die
/// Rechnung mit festen Maßen prüfen — „jedes Zeichen ist 1 mm breit" — und das Ergebnis ist
/// dann eine exakte Zahl statt eines Ungefähr. Was übrig bleibt, ist
/// <see cref="TdSkiaMeasure"/>: dreißig Zeilen, die Skia fragen.
/// </para>
/// </summary>
public interface ITdTextMeasure
{
    /// <summary>Breite von <paramref name="text"/> in dieser Schrift, in Zentimetern.</summary>
    double WidthCm(string text, TdCharFormat format);

    /// <summary>Die Maße der Schrift selbst, unabhängig vom Text.</summary>
    TdFontMetrics Metrics(TdCharFormat format);
}

/// <summary>
/// Die Messung über SkiaSharp — dieselbe Bibliothek, mit der auch gezeichnet wird. Nur so
/// stimmen Umbruch und Ausgabe überein: eine Zeile, die mit einer anderen Schriftmaschine
/// gemessen wurde, bricht an einer anderen Stelle um, als sie später gezeichnet wird.
/// </summary>
public sealed class TdSkiaMeasure : ITdTextMeasure, IDisposable
{
    // Punkt → Zentimeter. Ein Punkt ist 1/72 Zoll.
    private const double CmProPunkt = 2.54 / 72.0;

    /// <summary>
    /// Zwischenspeicher je Schrift-Kennung. Ein <c>SKTypeface</c> zu öffnen kostet einen
    /// Dateizugriff, und der Umbruch fragt je Wort danach.
    /// </summary>
    private readonly Dictionary<(string Familie, bool Fett, bool Kursiv), SKTypeface> _schriften = new();

    private SKTypeface Schrift(TdCharFormat f)
    {
        (string Familie, bool Fett, bool Kursiv) schluessel =
            (f.FontFamily ?? "", f.Bold == true, f.Italic == true);
        if (_schriften.TryGetValue(schluessel, out var vorhanden)) return vorhanden;

        var stil = new SKFontStyle(
            schluessel.Fett ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            schluessel.Kursiv ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        // **Kein Wurf, wenn die Schrift fehlt.** Unter Linux gibt es „Segoe UI" nicht; das
        // Dokument soll dann in einer Ersatzschrift umbrechen und nicht gar nicht.
        var schrift = SKTypeface.FromFamilyName(schluessel.Familie, stil)
                      ?? SKTypeface.FromFamilyName(null, stil)
                      ?? SKTypeface.Default;

        _schriften[schluessel] = schrift;
        return schrift;
    }

    private SKFont Font(TdCharFormat f)
    {
        var aufgeloest = f.Aufgeloest();
        return new SKFont(Schrift(aufgeloest), (float)(aufgeloest.FontSize!.Value));
    }

    public double WidthCm(string text, TdCharFormat format)
    {
        if (text.Length == 0) return 0;
        using var font = Font(format);
        // MeasureText rechnet in der Einheit der Schriftgröße — die steht hier in Punkt.
        return font.MeasureText(text) * CmProPunkt;
    }

    public TdFontMetrics Metrics(TdCharFormat format)
    {
        using var font = Font(format);
        var m = font.Metrics;

        // Skia gibt Ascent negativ (nach oben) und Descent positiv (nach unten).
        double auf = -m.Ascent * CmProPunkt;
        double ab = m.Descent * CmProPunkt;
        double durchschuss = m.Leading * CmProPunkt;

        return new TdFontMetrics(auf, ab, auf + ab + durchschuss);
    }

    public void Dispose()
    {
        foreach (var s in _schriften.Values) s.Dispose();
        _schriften.Clear();
    }
}
