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
}
