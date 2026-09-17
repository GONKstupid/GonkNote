using GonkNote.Core.Rendering;
using GonkNote.Core.Text;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die Rechtschreibprüfung (Phase 5.1, HANDOFF §5 Nr. 22).
///
/// <para>
/// <b>Geprüft wird die Wortzerlegung, nicht der Wortschatz.</b> Ob „Haus" im Wörterbuch
/// steht, ist die Aussage des Wörterbuchs und nicht die dieses Programms; wo ein Wort
/// anfängt und aufhört, ist unsere Rechnung — und genau die trägt die Wellenlinie an die
/// richtige Stelle. Ein Fehler darin sieht nicht wie ein Absturz aus, sondern wie ein
/// Strich unter dem halben Nachbarwort.
/// </para>
/// <para>
/// Die Wörterbücher liegen über den Projektverweis im Ausgabeordner. Fehlen sie, laufen die
/// Fälle unten trotzdem: <see cref="TdRechtschreibung.Fehler"/> gibt dann eine leere Liste
/// zurück, und <see cref="Wortgrenzen"/> prüft das ohne Wörterbuch.
/// </para>
/// </summary>
public sealed class RechtschreibungTests
{
    private const string De = "de-DE";

    [Fact]
    public void Ohne_Woerterbuch_wird_nichts_angestrichen()
    {
        // Eine Sprache, die nicht mitgeliefert wird. **Kein Wörterbuch heißt nicht
        // „alles falsch"** — sonst stünde der ganze Text unter Wellenlinien.
        Assert.False(TdRechtschreibung.Verfuegbar("xx-XX"));
        Assert.Empty(TdRechtschreibung.Fehler("Hallo Welt", "xx-XX"));
        Assert.Empty(TdRechtschreibung.Vorschlaege("Hallo", "xx-XX"));
    }

    [Fact]
    public void Deutsch_und_Englisch_sind_mitgeliefert()
    {
        Assert.True(TdRechtschreibung.Verfuegbar("de-DE"));
        Assert.True(TdRechtschreibung.Verfuegbar("en-US"));

        // Die Sprachfamilie genügt: de-AT bekommt de_DE, statt gar nicht geprüft zu werden.
        Assert.True(TdRechtschreibung.Verfuegbar("de-AT"));
        Assert.True(TdRechtschreibung.Verfuegbar("en-GB"));
    }

    [Fact]
    public void Ein_falsches_Wort_wird_genau_getroffen()
    {
        const string text = "Das ist ein Hausx.";
        var funde = TdRechtschreibung.Fehler(text, De);

        var fund = Assert.Single(funde);
        // **Die Stelle ist die ganze Aussage.** Steht hier eine Zahl daneben, sitzt die
        // Wellenlinie unter dem falschen Wort.
        Assert.Equal("Hausx", text.Substring(fund.Start, fund.Laenge));
    }

    [Fact]
    public void Richtig_Geschriebenes_bleibt_unangetastet()
    {
        Assert.Empty(TdRechtschreibung.Fehler("Das ist ein Haus.", De));
    }

    [Fact]
    public void Zahlwoerter_und_Kennungen_werden_uebersprungen()
    {
        // A4, x2, COVID19: keine Rechtschreibfrage. Sie alle anzustreichen wäre die
        // schnellste Art, die Prüfung unbrauchbar zu machen.
        Assert.Empty(TdRechtschreibung.Fehler("Auf A4 und in x2 und COVID19", De));
        Assert.Empty(TdRechtschreibung.Fehler("12 345 67", De));
    }

    [Fact]
    public void Vorschlaege_kommen_und_sind_gedeckelt()
    {
        var vorschlaege = TdRechtschreibung.Vorschlaege("Hausx", De, hoechstens: 3);
        Assert.NotEmpty(vorschlaege);
        Assert.True(vorschlaege.Count <= 3);
    }

    /// <summary>
    /// Die Wortzerlegung selbst — über eine Sprache ohne Wörterbuch nicht prüfbar, deshalb
    /// hier über Deutsch mit lauter Unwörtern: <b>jedes</b> muss einzeln getroffen werden.
    /// </summary>
    [Fact]
    public void Wortgrenzen()
    {
        const string text = "Xqwz-Vbnm, Qqqz!";
        var funde = TdRechtschreibung.Fehler(text, De);

        // Der Bindestrich trennt: drei Wörter, nicht zwei — und die Satzzeichen gehören zu
        // keinem davon.
        Assert.Equal(3, funde.Count);
        Assert.Equal(["Xqwz", "Vbnm", "Qqqz"], [.. funde.Select(f => text.Substring(f.Start, f.Laenge))]);
    }

    [Fact]
    public void Apostroph_haelt_ein_Wort_zusammen()
    {
        const string text = "don't";
        var funde = TdRechtschreibung.Fehler(text, "en-US");

        // Richtig geschrieben, also kein Fund — **und vor allem kein Fund auf „t"**, was
        // dabei herauskäme, wenn der Apostroph trennte.
        Assert.Empty(funde);
    }

    // ==================== Die Wellenlinie auf dem Papier ====================

    /// <summary>
    /// Gezeichnet wird mit der echten Schriftmessung (<see cref="TdSkiaMeasure"/>), damit
    /// Umbruch und Zeichner dieselbe Breite meinen — sonst stünde die Welle rechnerisch
    /// woanders als der Text.
    /// </summary>
    private static int RoteBildpunkte(string text, string? sprache)
    {
        var doc = new TdDocument
        {
            DefaultParaFormat = { SpaceBeforePt = 0, SpaceAfterPt = 0 },
            Sections =
            {
                new TdSection([new TdParagraph(text)])
                {
                    Page = new TdPageSetup
                    {
                        WidthCm = 12, HeightCm = 4,
                        MarginLeftCm = 1, MarginRightCm = 1, MarginTopCm = 1, MarginBottomCm = 1,
                    },
                },
            },
        };

        using var messung = new TdSkiaMeasure();
        var seite = TdLayout.Umbrechen(doc, messung).Pages[0];

        const double massstab = TdRenderer.PixelProCm;
        int breite = (int)Math.Ceiling(seite.Setup.WidthCm * massstab);
        int hoehe = (int)Math.Ceiling(seite.Setup.HeightCm * massstab);

        using var bmp = new SKBitmap(new SKImageInfo(breite, hoehe, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var leinwand = new SKCanvas(bmp))
        {
            leinwand.Clear(SKColors.White);
            // **Grammatik aus.** Geprüft wird hier die rote Welle; mit Grammatik ginge nebenbei
            // eine Anfrage an einen LanguageTool-Server hinaus, die unter Windows zwei Sekunden
            // nachhängt und in den nächsten Test platzt (CI, 2026-09-16).
            TdRenderer.Seite(leinwand, seite, massstab,
                new TdRenderContext(Rechtschreibsprache: sprache, Grammatik: false));
        }

        // **Gezählt wird nur Rot**, nicht „nicht mehr weiß" wie in <see cref="Farbfleck"/>:
        // Der Text selbst ist schwarz und stünde sonst im Ergebnis. Die Schwelle hält die
        // blassen Ränder der Kantenglättung draußen.
        int rot = 0;
        for (int y = 0; y < hoehe; y++)
            for (int x = 0; x < breite; x++)
            {
                var p = bmp.GetPixel(x, y);
                if (p.Red > 120 && p.Green < 110 && p.Blue < 110) rot++;
            }
        return rot;
    }

    [Fact]
    public void Ohne_Sprache_zeichnet_der_Zeichner_keine_Welle()
    {
        // Der Normalfall: Drucken, Export, jede Vorschau. **Eine Wellenlinie gehört nicht
        // aufs Papier** — stünde hier eine Zahl über null, käme sie mit ins PDF.
        Assert.Equal(0, RoteBildpunkte("Das ist ein Hausx.", sprache: null));
    }

    [Fact]
    public void Mit_Sprache_bekommt_das_falsche_Wort_eine_Welle()
    {
        Assert.True(RoteBildpunkte("Das ist ein Hausx.", De) > 0);
    }

    [Fact]
    public void Ein_fehlerfreier_Satz_bleibt_ohne_Welle()
    {
        // Der teuerste Fehler dieser Funktion ist nicht die fehlende Welle, sondern die
        // falsche: Wer unter richtig geschriebenen Wörtern Striche sieht, schaltet sie ab.
        Assert.Equal(0, RoteBildpunkte("Das ist ein Haus.", De));
    }

    [Fact]
    public void Leerer_Text_wirft_nicht()
    {
        Assert.Empty(TdRechtschreibung.Fehler("", De));
        Assert.Empty(TdRechtschreibung.Fehler((string?)null, De));
        Assert.Empty(TdRechtschreibung.Fehler("   \t ", De));
    }
}
