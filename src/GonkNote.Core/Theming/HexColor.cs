using System.Globalization;

namespace GonkNote.Core.Theming;

/// <summary>
/// Eine Farbe als vier Bytes — das kleinste gemeinsame Maß aller Köpfe. WPF baut daraus
/// ein <c>System.Windows.Media.Color</c>, Avalonia ein <c>Avalonia.Media.Color</c>, der
/// Renderer ein <c>SKColor</c>; keiner von ihnen braucht dafür einen eigenen Parser.
/// </summary>
/// <remarks>
/// Reihenfolge <b>A, R, G, B</b> wie in <c>#AARRGGBB</c> — nicht RGBA. Die Zeichenketten in
/// der Datenbank (<c>NoteItem.IconColor</c>) und in den Themes stehen seit V1 in dieser
/// Form; eine andere Reihenfolge hier wäre eine stille Farbvertauschung.
/// </remarks>
public readonly record struct HexColor(byte A, byte R, byte G, byte B)
{
    /// <summary>Undurchsichtiges Schwarz — der Wert, den <see cref="Parse"/> nie liefert, ohne gefragt zu werden.</summary>
    public static HexColor Black => new(0xFF, 0, 0, 0);

    /// <summary>
    /// Liest <c>#RGB</c>, <c>#RRGGBB</c> oder <c>#AARRGGBB</c>; das Doppelkreuz darf fehlen.
    /// Ohne Alpha-Anteil gilt <c>0xFF</c>. Groß-/Kleinschreibung ist egal.
    /// <para>
    /// <c>false</c> statt einer Ausnahme, weil der häufigste Aufrufer ein Konverter an einer
    /// Bindung ist: eine unbrauchbare Farbe darf dort einen Rückfall auslösen und nicht das
    /// Zeichnen abbrechen.
    /// </para>
    /// </summary>
    public static bool TryParse(string? text, out HexColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var s = text.AsSpan().Trim();
        if (s.Length > 0 && s[0] == '#') s = s[1..];

        switch (s.Length)
        {
            case 3:   // #RGB → jede Ziffer verdoppelt, wie in CSS
                if (!Nibble(s[0], out byte r3) || !Nibble(s[1], out byte g3) || !Nibble(s[2], out byte b3))
                    return false;
                color = new HexColor(0xFF, (byte)(r3 * 17), (byte)(g3 * 17), (byte)(b3 * 17));
                return true;

            case 6:
                if (!Byte(s[..2], out byte r6) || !Byte(s[2..4], out byte g6) || !Byte(s[4..6], out byte b6))
                    return false;
                color = new HexColor(0xFF, r6, g6, b6);
                return true;

            case 8:
                if (!Byte(s[..2], out byte a8) || !Byte(s[2..4], out byte r8) ||
                    !Byte(s[4..6], out byte g8) || !Byte(s[6..8], out byte b8))
                    return false;
                color = new HexColor(a8, r8, g8, b8);
                return true;

            default:
                return false;
        }

        static bool Nibble(char c, out byte value)
        {
            value = 0;
            if (!byte.TryParse([c], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte v)) return false;
            value = v;
            return true;
        }

        static bool Byte(ReadOnlySpan<char> s, out byte value) =>
            byte.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Wie <see cref="TryParse"/>, aber mit <paramref name="fallback"/> statt eines Fehlschlags.</summary>
    public static HexColor Parse(string? text, HexColor fallback) =>
        TryParse(text, out var c) ? c : fallback;

    /// <summary>
    /// Zurück in Text — <c>#RRGGBB</c>, bei einem Alpha-Anteil unter <c>0xFF</c>
    /// <c>#AARRGGBB</c>. Damit ist <c>Parse(ToString()) == this</c> für jede Farbe.
    /// </summary>
    public override string ToString() => A == 0xFF
        ? $"#{R:X2}{G:X2}{B:X2}"
        : $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    // ==================== Farbton, Sättigung, Helligkeit ====================
    //
    // Für den Farbwähler (Phase 4.5). Ein Wähler zeigt nicht vier Bytes, sondern eine
    // Farbfläche, einen Farbtonstreifen und einen Deckkraftregler — das sind H, S, V und A.
    // Die Umrechnung ist reine Arithmetik und gehört deshalb hierher und nicht in einen
    // Kopf: bis Phase 4.5 stand sie privat im WPF-Dialog, und der Linux-Kopf hätte sie
    // abschreiben müssen.

    /// <summary>
    /// Zerlegt die Farbe in Farbton (0–360°), Sättigung und Helligkeit (je 0–1). Der
    /// Alpha-Anteil bleibt außen vor — er ist keine Eigenschaft des Farbtons.
    /// <para>
    /// <b>Bei Grau ist der Farbton nicht bestimmt</b> und wird als 0 gemeldet. Ein Wähler
    /// darf deshalb <b>H, S und V als eigenen Zustand halten</b> und nicht bei jeder
    /// Bewegung aus der Farbe zurückrechnen — sonst springt der Farbtonzeiger auf Rot,
    /// sobald der Nutzer die Sättigung auf null zieht.
    /// </para>
    /// </summary>
    public (double H, double S, double V) ToHsv()
    {
        double r = R / 255.0, g = G / 255.0, b = B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        double d = max - min;

        double h = 0;
        if (d > 0)
        {
            if (max == r) h = 60 * (((g - b) / d) % 6);
            else if (max == g) h = 60 * ((b - r) / d + 2);
            else h = 60 * ((r - g) / d + 4);
        }
        if (h < 0) h += 360;

        return (h, max == 0 ? 0 : d / max, max);
    }

    /// <summary>
    /// Baut eine Farbe aus Farbton (0–360°), Sättigung und Helligkeit (je 0–1) sowie einem
    /// Alpha-Anteil. Werte außerhalb ihrer Bereiche werden zurechtgestutzt statt abgelehnt —
    /// der Aufrufer ist ein Mauszeiger auf einer Fläche, und der darf über den Rand hinaus.
    /// </summary>
    public static HexColor FromHsv(double h, double s, double v, byte a = 0xFF)
    {
        h = ((h % 360) + 360) % 360;   // auch negative Winkel landen im Bereich
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);

        double c = v * s;
        double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        double m = v - c;

        var (r, g, b) = ((int)(h / 60) % 6) switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return new HexColor(a,
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    /// <summary>Dieselbe Farbe mit einem anderen Alpha-Anteil.</summary>
    public HexColor WithAlpha(byte a) => this with { A = a };
}
