using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Textausgabe und Textumbruch — **schriftunabhängig** geprüft.
/// <para>
/// Der Umstieg auf SkiaSharp 3 hat genau hier die meisten Zeilen angefasst: Größe und
/// Schriftart wanderten von <c>SKPaint</c> nach <see cref="SKFont"/>, <c>MeasureText</c>
/// wechselte den Besitzer, und die Ausrichtung ist jetzt ein Argument von <c>DrawText</c>.
/// Ein Fehler dabei sieht nicht wie ein Absturz aus, sondern wie Text in Schriftgröße 12,
/// wo 24 stehen sollte — oder wie ein Umbruch, der die Breite ignoriert.
/// </para>
/// Gehasht wird hier nichts (siehe <see cref="Farbfleck"/>): geprüft werden Eigenschaften,
/// die für jede Schrift gelten müssen. Damit laufen diese Tests unter Windows und Linux
/// gleich — was für die CI die Bedingung ist.
/// </summary>
public sealed class SchriftTests
{
    private const int Breite = 400;
    private const int Hoehe = 300;

    [Fact]
    public void Schriftgroesse_kommt_wirklich_in_der_Schrift_an()
    {
        using var klein = WbFonts.Font("Segoe UI", 12f);
        using var gross = WbFonts.Font("Segoe UI", 48f);

        Assert.Equal(12f, klein.Size);
        Assert.Equal(48f, gross.Size);

        // Der eigentliche Punkt: die Größe muss das Messen beeinflussen. Läge sie noch am
        // Paint (wie bis SkiaSharp 2), wären beide Messungen gleich.
        Assert.True(gross.MeasureText("Gonk") > klein.MeasureText("Gonk") * 3f,
            "Die Schriftgröße wirkt sich nicht aufs Messen aus — sitzt sie noch am SKPaint?");
    }

    [Fact]
    public void Unbekannte_Schrift_faellt_zurueck_und_wird_gecacht()
    {
        // Kein Absturz, sondern die Standardschrift: die App soll auf jedem System laufen,
        // auch ohne die gewünschte Schriftfamilie.
        var erste = WbFonts.Family("Diese Schrift Gibt Es Nicht 12345");
        var zweite = WbFonts.Family("Diese Schrift Gibt Es Nicht 12345");

        Assert.NotNull(erste);
        Assert.Same(erste, zweite);
        Assert.Same(WbFonts.Regular, WbFonts.Family(null));
        Assert.Same(WbFonts.Regular, WbFonts.Family(""));

        // **Bis §4.26 stand hier auch „Segoe UI".** Das war richtig, solange die
        // Oberflächenschrift Segoe UI *war* — jeder Name, der ihr entsprach, kam als
        // `Regular` zurück. Seit die App ihre Schriften mitliefert, ist die Oberflächenschrift
        // Inter, und „Segoe UI" ist ein **fremder** Name: Er darf nicht mehr auf die
        // Oberflächenschrift zeigen, sondern muss unter Windows die echte Segoe UI liefern —
        // sonst verlöre ein Bestandsdokument seine gespeicherte Schrift (§4.14).
        Assert.NotSame(WbFonts.Regular, WbFonts.Family("Segoe UI"));
    }

    [Fact]
    public void Text_landet_innerhalb_der_gemeldeten_Umschliessung()
    {
        var text = new TextElement
        {
            X = 40, Y = 60,
            Text = "Gonk Note",
            Color = "#FF000000",
            FontSize = 32f,
        };

        var box = WbRenderer.TextBounds(text);
        var fleck = Farbfleck.Von(Breite, Hoehe, c => WbRenderer.DrawText(c, text));

        Assert.False(fleck.Leer, "Es wurde überhaupt kein Text gezeichnet.");

        // Die Umschließung ist die Grundlage für Auswahl, Verschieben und Radieren. Läge der
        // Text daneben, könnte man ihn nicht anfassen.
        Assert.True(fleck.Umschliessung.Left >= (int)box.Left - 2, $"Text ragt links heraus: {fleck.Umschliessung} vs. {box}");
        Assert.True(fleck.Umschliessung.Top >= (int)box.Top - 2, $"Text ragt oben heraus: {fleck.Umschliessung} vs. {box}");
        Assert.True(fleck.Umschliessung.Right <= (int)box.Right + 2, $"Text ragt rechts heraus: {fleck.Umschliessung} vs. {box}");
        Assert.True(fleck.Umschliessung.Bottom <= (int)box.Bottom + 2, $"Text ragt unten heraus: {fleck.Umschliessung} vs. {box}");
    }

    [Fact]
    public void Mehr_Zeilen_ergeben_eine_hoehere_Umschliessung()
    {
        var eine = new TextElement { X = 10, Y = 10, Text = "Zeile", FontSize = 20f };
        var drei = new TextElement { X = 10, Y = 10, Text = "Zeile\nZeile\nZeile", FontSize = 20f };

        Assert.Equal(WbRenderer.TextBounds(eine).Height * 3f, WbRenderer.TextBounds(drei).Height, 3);

        // Und die Zeilen dürfen nicht übereinander liegen: der Fleck muss mitwachsen.
        var fleckEine = Farbfleck.Von(Breite, Hoehe, c => WbRenderer.DrawText(c, eine));
        var fleckDrei = Farbfleck.Von(Breite, Hoehe, c => WbRenderer.DrawText(c, drei));
        Assert.True(fleckDrei.Umschliessung.Height > fleckEine.Umschliessung.Height * 2,
            "Die drei Zeilen liegen offenbar übereinander.");
    }

    /// <summary>
    /// Ein leerer Textkasten braucht trotzdem eine Fläche — sonst lässt er sich nach dem
    /// Anlegen nicht mehr anklicken und nicht mehr löschen.
    /// </summary>
    [Fact]
    public void Leerer_Text_behaelt_eine_anfassbare_Flaeche()
    {
        var box = WbRenderer.TextBounds(new TextElement { X = 5, Y = 7, Text = "", FontSize = 18f });

        Assert.True(box.Width >= 10, $"Breite {box.Width} ist zu klein zum Anfassen.");
        Assert.True(box.Height > 0, "Höhe 0 — der Kasten wäre unsichtbar und unanklickbar.");
    }

    [Fact]
    public void Hintergrundfarbe_wird_hinter_den_Text_gelegt()
    {
        var ohne = new TextElement { X = 40, Y = 60, Text = "Gonk", FontSize = 28f };
        var mit = new TextElement { X = 40, Y = 60, Text = "Gonk", FontSize = 28f, Background = "#FFFEF3C7" };

        var fleckOhne = Farbfleck.Von(Breite, Hoehe, c => WbRenderer.DrawText(c, ohne));
        var fleckMit = Farbfleck.Von(Breite, Hoehe, c => WbRenderer.DrawText(c, mit));

        // Die Fläche ist deutlich größer als die Buchstaben allein …
        Assert.True(fleckMit.Pixel > fleckOhne.Pixel * 2, "Der Hintergrund wurde nicht gezeichnet.");
        // … und sie umschließt sie.
        Assert.True(fleckMit.Umschliessung.Contains(fleckOhne.Umschliessung),
            $"Hintergrund {fleckMit.Umschliessung} umschließt den Text {fleckOhne.Umschliessung} nicht.");
    }

    // ---- Textumbruch ---------------------------------------------------------------------

    [Fact]
    public void Umbruch_haelt_die_Breite_ein()
    {
        using var font = WbFonts.Font("Segoe UI", 16f);
        const float maxBreite = 120f;
        const string text = "Ein längerer Satz, der auf mehrere Zeilen umbrochen werden muss.";

        var zeilen = WbRenderer.WrapText(text, font, maxBreite).ToList();

        Assert.True(zeilen.Count > 1, "Es wurde gar nicht umbrochen.");
        foreach (var zeile in zeilen)
            Assert.True(font.MeasureText(zeile) <= maxBreite,
                $"Zeile „{zeile}\" ist {font.MeasureText(zeile)} breit, erlaubt sind {maxBreite}.");

        // Nichts darf verloren gehen: die Wörter müssen in der Reihenfolge wieder da sein.
        Assert.Equal(
            text.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            string.Join(' ', zeilen).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void Umbruch_behaelt_eigene_Zeilenwechsel()
    {
        using var font = WbFonts.Font("Segoe UI", 16f);

        var zeilen = WbRenderer.WrapText("eins\n\nzwei", font, 500f).ToList();

        Assert.Equal(["eins", "", "zwei"], zeilen);
    }

    /// <summary>
    /// Ein einzelnes Wort, das breiter ist als die Karte, muss hart umbrochen werden — sonst
    /// läuft es über den Zettelrand hinaus und wird abgeschnitten.
    /// </summary>
    [Fact]
    public void Zu_langes_Wort_wird_hart_umbrochen()
    {
        using var font = WbFonts.Font("Segoe UI", 16f);
        const float maxBreite = 60f;

        var zeilen = WbRenderer.WrapText(new string('M', 60), font, maxBreite).ToList();

        Assert.True(zeilen.Count > 1, "Das überlange Wort wurde nicht umbrochen.");
        Assert.Equal(new string('M', 60), string.Concat(zeilen));
        foreach (var zeile in zeilen)
            // Ein einzelnes Zeichen darf breiter sein als erlaubt — feiner geht es nicht.
            if (zeile.Length > 1)
                Assert.True(font.MeasureText(zeile) <= maxBreite, $"Bruchstück „{zeile}\" ist zu breit.");
    }

    /// <summary>
    /// Text, der nicht mehr auf den Zettel passt, wird abgeschnitten — und zwar innerhalb der
    /// Karte. Ohne den Clip liefe die Schrift über den Rand ins Bild.
    /// </summary>
    [Fact]
    public void Zettel_schneidet_ueberzaehligen_Text_an_der_Karte_ab()
    {
        var zettel = new StickyNoteElement
        {
            X = 60, Y = 40, Width = 140, Height = 90,
            Text = string.Join(' ', Enumerable.Repeat("Wort", 60)),
            Color = "#FFFEF08A",
            TextColor = "#FF1F2937",
            FontSize = 14f,
        };

        var fleck = Farbfleck.Von(Breite, Hoehe, c => WbRenderer.DrawSticky(c, zettel));

        Assert.False(fleck.Leer);
        // Der Schatten liegt bewusst außerhalb der Karte (18 px Rand, 3 px nach unten
        // versetzt) — die Toleranz ist genau dieser Rand, nicht mehr.
        var erlaubt = new SKRectI(
            (int)zettel.X - 20, (int)zettel.Y - 20,
            (int)(zettel.X + zettel.Width) + 20, (int)(zettel.Y + zettel.Height) + 24);

        Assert.True(erlaubt.Contains(fleck.Umschliessung),
            $"Der Zettel malt außerhalb: {fleck.Umschliessung}, erlaubt {erlaubt}.");
    }
}
