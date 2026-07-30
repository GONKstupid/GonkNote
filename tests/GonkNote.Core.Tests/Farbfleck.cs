using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Was von einer Zeichnung tatsächlich auf dem Papier landet: Anzahl und Lage der Pixel,
/// die nicht mehr weiß sind.
/// <para>
/// Gebraucht für alles, was **Schrift** enthält. Ein Pixel-Hash wäre dort wertlos: die
/// Zeichenroutinen fragen „Segoe UI" ab und fallen sonst auf die Standardschrift des Systems
/// zurück — unter Linux also auf eine andere Schrift. Statt der Pixel prüfen diese Tests
/// deshalb Eigenschaften, die für jede Schrift gelten müssen: dass überhaupt Farbe ankommt,
/// dass sie innerhalb der gemeldeten Umschließung bleibt, dass sie am Kartenrand aufhört.
/// </para>
/// </summary>
internal sealed record Farbfleck(int Pixel, SKRectI Umschliessung)
{
    public bool Leer => Pixel == 0;

    /// <summary>Zeichnet auf weißen Grund und vermisst, was nicht mehr weiß ist.</summary>
    public static Farbfleck Von(int width, int height, Action<SKCanvas> zeichnen)
    {
        using var bmp = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bmp))
        {
            canvas.Clear(SKColors.White);
            zeichnen(canvas);
        }

        int anzahl = 0;
        int links = int.MaxValue, oben = int.MaxValue, rechts = int.MinValue, unten = int.MinValue;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var p = bmp.GetPixel(x, y);
                // Kantenglättung erzeugt sehr blasse Ränder. Die Schwelle hält sie draußen,
                // sonst wüchse jede Umschließung um ein, zwei Pixel und die Tests müssten mit
                // Toleranzen arbeiten, die nichts mehr aussagen.
                if (p.Red > 245 && p.Green > 245 && p.Blue > 245) continue;

                anzahl++;
                if (x < links) links = x;
                if (x > rechts) rechts = x;
                if (y < oben) oben = y;
                if (y > unten) unten = y;
            }

        return anzahl == 0
            ? new Farbfleck(0, SKRectI.Empty)
            : new Farbfleck(anzahl, new SKRectI(links, oben, rechts + 1, unten + 1));
    }
}
