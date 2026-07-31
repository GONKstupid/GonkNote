using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die beiden Wege, die ein Bild nimmt, bevor es gespeichert oder erkannt wird.
/// <para>
/// Bis Phase 2 waren das die <b>letzten zwei Stellen ohne Test</b>, die der
/// SkiaSharp-3-Umstieg angefasst hat (HANDOFF §4.4): der Bildimport lag privat in
/// <c>WhiteboardView.PrepareRaster</c>, die OCR-Vorverarbeitung privat in
/// <c>OcrService.Preprocess</c> — beide im WPF-Kopf, beide reines SkiaSharp. Sie liegen
/// jetzt in <see cref="WbImagePrep"/> und sind damit prüfbar, ohne den Kopf hochzufahren.
/// </para>
/// Bilder werden hier gemalt, nicht eingecheckt — und nie achsensymmetrisch, sonst fiele
/// eine vertauschte Achse nicht auf (HANDOFF §7).
/// </summary>
public sealed class BildaufbereitungTests
{
    /// <summary>
    /// Ein Testbild mit klar unterscheidbaren Kanten: links ein breiter roter Streifen,
    /// oben ein schmaler blauer. Breite ≠ Höhe, damit eine vertauschte Achse auffällt.
    /// </summary>
    private static byte[] Bild(int breite, int hoehe, SKEncodedImageFormat format, bool durchsichtig = false)
    {
        var info = new SKImageInfo(breite, hoehe, SKColorType.Bgra8888,
            durchsichtig ? SKAlphaType.Premul : SKAlphaType.Opaque);
        using var bmp = new SKBitmap(info);
        using (var canvas = new SKCanvas(bmp))
        {
            canvas.Clear(durchsichtig ? SKColors.Transparent : SKColors.White);
            using var rot = new SKPaint { Color = SKColors.Red };
            using var blau = new SKPaint { Color = SKColors.Blue };
            canvas.DrawRect(0, 0, breite * 0.3f, hoehe, rot);
            canvas.DrawRect(0, 0, breite, hoehe * 0.1f, blau);
        }
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(format, 95);
        return data.ToArray();
    }

    // ---------- Import ----------

    [Fact]
    public void Kleines_Bild_kommt_Byte_fuer_Byte_unveraendert_zurueck()
    {
        var roh = Bild(300, 200, SKEncodedImageFormat.Png);

        var ergebnis = WbImagePrep.ForImport(roh);

        Assert.NotNull(ergebnis);
        // Nicht nur „gleich groß": es müssen dieselben Bytes sein. Würde hier neu kodiert,
        // wäre aus einem PNG-Original still ein JPEG geworden – mit Verlusten, ohne Gewinn.
        Assert.Same(roh, ergebnis!.Value.Data);
        Assert.Equal(300, ergebnis.Value.Width);
        Assert.Equal(200, ergebnis.Value.Height);
    }

    [Fact]
    public void Grosses_Bild_wird_auf_die_Grenze_verkleinert_und_behaelt_das_Seitenverhaeltnis()
    {
        // 3000 × 1200: über der Grenze, und bewusst nicht quadratisch.
        var roh = Bild(3000, 1200, SKEncodedImageFormat.Jpeg);

        var ergebnis = WbImagePrep.ForImport(roh);

        Assert.NotNull(ergebnis);
        Assert.Equal(WbImagePrep.MaxImportDim, ergebnis!.Value.Width);
        // 1200 / 3000 * 2048 = 819,2 → 819 (abgeschnitten, nicht gerundet)
        Assert.Equal(819, ergebnis.Value.Height);
        Assert.True(ergebnis.Value.Data.Length < roh.Length,
            "Das verkleinerte Bild ist nicht kleiner als das Original.");
    }

    [Fact]
    public void Durchsichtigkeit_ueberlebt_die_Verkleinerung()
    {
        // JPEG kann keine Durchsichtigkeit. Wird beim Verkleinern das Format falsch
        // gewählt, bekommt ein freigestelltes Bild einen schwarzen Kasten – und niemand
        // sieht das dem Byte-Strom an.
        var roh = Bild(3000, 1200, SKEncodedImageFormat.Png, durchsichtig: true);

        var ergebnis = WbImagePrep.ForImport(roh);

        Assert.NotNull(ergebnis);
        using var wieder = WbImages.Decode(ergebnis!.Value.Data);
        Assert.NotNull(wieder);
        Assert.True(WbImagePrep.HasTransparency(wieder!),
            "Nach dem Verkleinern ist die Durchsichtigkeit weg – wurde als JPEG kodiert?");
    }

    [Fact]
    public void Undurchsichtiges_Bild_gilt_nicht_als_durchsichtig()
    {
        using var bmp = WbImages.Decode(Bild(300, 200, SKEncodedImageFormat.Jpeg));
        Assert.NotNull(bmp);
        Assert.False(WbImagePrep.HasTransparency(bmp!));
    }

    [Fact]
    public void Kaputte_Datei_meldet_sich_als_kein_Bild()
    {
        // Der Fall aus dem SkiaSharp-3-Umstieg: SKBitmap.Decode wirft hier, statt null zu
        // liefern. Wer WbImages.Decode umgeht, reißt damit den ganzen Import ab.
        Assert.Null(WbImagePrep.ForImport("Das ist kein Bild, sondern Text."u8.ToArray()));
    }

    // ---------- Texterkennung ----------

    [Fact]
    public void Kleines_Bild_wird_fuer_die_Erkennung_hochgezogen()
    {
        var roh = Bild(400, 250, SKEncodedImageFormat.Jpeg);

        using var vorbereitet = WbImages.Decode(WbImagePrep.ForOcr(roh));

        Assert.NotNull(vorbereitet);
        Assert.Equal(WbImagePrep.OcrTargetLongSide, vorbereitet!.Width);
        // 250 / 400 * 1600 = 1000 (gerundet, nicht abgeschnitten – anders als beim Import)
        Assert.Equal(1000, vorbereitet.Height);
    }

    [Fact]
    public void Grosse_Seite_bleibt_fuer_die_Erkennung_in_ihrer_Groesse()
    {
        // Eine importierte PDF-Seite ist längst groß genug; Hochziehen kostete nur Zeit.
        var roh = Bild(2246, 1600, SKEncodedImageFormat.Jpeg);

        using var vorbereitet = WbImages.Decode(WbImagePrep.ForOcr(roh));

        Assert.NotNull(vorbereitet);
        Assert.Equal(2246, vorbereitet!.Width);
        Assert.Equal(1600, vorbereitet.Height);
    }

    [Fact]
    public void Kaputte_Daten_gehen_unveraendert_an_die_Erkennung_weiter()
    {
        // Absicht: die Erkennungs-Bibliothek soll ihre eigene Dekodierung versuchen dürfen.
        // Werfen darf es hier auf keinen Fall – seit SkiaSharp 3 wäre genau das der Fehler.
        var roh = "Das ist kein Bild, sondern Text."u8.ToArray();
        Assert.Same(roh, WbImagePrep.ForOcr(roh));
    }
}
