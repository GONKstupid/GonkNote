using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Pixelgenauer Vergleich einer Zeichnung gegen einen eingecheckten Hash.
/// <para>
/// **Warum es das gibt:** Der Umstieg auf SkiaSharp 3 baute mit 0 Fehlern und 0 Warnungen —
/// und die App stürzte beim ersten Zeichnen jedes Notizbuchs ab
/// (<c>SKColorFilter.CreateTable</c> mit <c>null</c>, im statischen Konstruktor von
/// <c>WbRenderer</c>). Ein grüner Build sagt bei einem Bibliothekssprung fast nichts. Diese
/// Snapshots sind der Ersatz: sie zeichnen wirklich und vergleichen wirklich Pixel.
/// </para>
/// Verglichen wird der Hash der **Rohpixel**, nicht der eines PNG. Ein PNG-Encoder darf
/// seine Kompression zwischen Versionen ändern, ohne dass sich das Bild ändert — der Test
/// wäre dann rot, obwohl nichts kaputt ist.
/// <para>
/// **Neue Golden-Files anlegen bzw. absichtlich ändern:** Umgebungsvariable
/// <c>GONK_SNAPSHOT_UPDATE=1</c> setzen und die Tests einmal laufen lassen; die Hashes werden
/// dann in den Quellordner geschrieben. Danach **das Bild ansehen** (es liegt daneben im
/// Ausgabeordner unter <c>Snapshots\ist\</c>) und den Diff bewusst einchecken. Ohne die
/// Variable legt kein Test von sich aus einen Hash an — ein fehlender Golden-File ist ein
/// Fehlschlag, keine stille Zustimmung.
/// </para>
/// </summary>
internal static class Snapshot
{
    /// <summary>
    /// Zeichnet auf eine weiße Fläche der angegebenen Größe und vergleicht das Ergebnis mit
    /// <c>Snapshots/<paramref name="name"/>.sha256</c>.
    /// </summary>
    public static void Assert(string name, int width, int height, Action<SKCanvas> zeichnen)
    {
        using var bmp = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bmp))
        {
            canvas.Clear(SKColors.White);
            zeichnen(canvas);
        }

        string ist = Hash(bmp.Bytes);

        if (Aktualisieren)
        {
            Schreiben(QuellDatei(name), ist);
            // Das Bild gleich mit ablegen — ein Hash, den niemand angesehen hat, ist kein
            // Golden-File, sondern nur eine festgeschriebene Zufälligkeit.
            BildAblegen(name, bmp);
            return;
        }

        string? soll = Lesen(name);
        if (soll == null)
        {
            string beweis = BildAblegen(name, bmp);
            Xunit.Assert.Fail(
                $"Kein Golden-File für „{name}\".\n" +
                $"Das gezeichnete Bild liegt unter: {beweis}\n" +
                $"Ist es richtig, mit GONK_SNAPSHOT_UPDATE=1 einmal laufen lassen und den " +
                $"neuen Hash einchecken ({QuellDatei(name)}).");
        }

        if (soll != ist)
        {
            string beweis = BildAblegen(name, bmp);
            Xunit.Assert.Fail(
                $"Snapshot „{name}\" hat sich geändert.\n" +
                $"  erwartet: {soll}\n" +
                $"  ist:      {ist}\n" +
                $"Das jetzt gezeichnete Bild liegt unter: {beweis}\n" +
                "Erst ansehen, dann entscheiden: Fehler beheben — oder, wenn die Änderung " +
                "gewollt ist, mit GONK_SNAPSHOT_UPDATE=1 neu setzen.");
        }
    }

    // ---- Golden-Files --------------------------------------------------------------------

    private static bool Aktualisieren =>
        Environment.GetEnvironmentVariable("GONK_SNAPSHOT_UPDATE") == "1";

    /// <summary>
    /// Erst der plattformeigene Hash, dann der gemeinsame.
    /// <para>
    /// Die Zeichenroutinen sind plattformneutral und Skia rechnet auf der CPU — dieselbe
    /// SkiaSharp-Fassung sollte auf Windows und Linux dasselbe Bild ergeben, und genau
    /// deshalb steht normalerweise nur **ein** Hash da. Sollte sich das für eine Zeichnung
    /// doch unterscheiden, ist ein zusätzliches <c>&lt;name&gt;.linux.sha256</c> der Ausweg —
    /// bewusst angelegt, nicht automatisch. Ohne diesen Ausweg wäre die Versuchung groß, den
    /// Snapshot bei der ersten Abweichung ganz abzuschalten.
    /// </para>
    /// </summary>
    private static string? Lesen(string name)
    {
        foreach (string kandidat in Kandidaten(name))
            if (File.Exists(kandidat))
                return File.ReadAllText(kandidat).Trim();
        return null;
    }

    private static IEnumerable<string> Kandidaten(string name)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Snapshots");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            yield return Path.Combine(dir, name + ".linux.sha256");
        yield return Path.Combine(dir, name + ".sha256");
    }

    /// <summary>
    /// Pfad im **Quellordner**. Steht als Assembly-Metadatum in der .csproj, damit der Test
    /// den Ordner nicht über „drei Ebenen über bin" erraten muss — das bricht, sobald jemand
    /// die Ausgabepfade anfasst.
    /// </summary>
    private static string QuellDatei(string name) =>
        Path.Combine(Projektordner, "Snapshots", name + ".sha256");

    private static string Projektordner =>
        typeof(Snapshot).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjektOrdner")?.Value
        ?? throw new InvalidOperationException(
            "Assembly-Metadatum „ProjektOrdner\" fehlt — siehe GonkNote.Core.Tests.csproj.");

    private static void Schreiben(string path, string hash)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, hash + Environment.NewLine);
    }

    /// <summary>
    /// Legt das tatsächlich gezeichnete Bild als PNG in den Ausgabeordner. Ein Hash allein
    /// sagt niemandem, *was* sich geändert hat; ohne das Bild bleibt nur Raten. Bewusst in
    /// den Ausgabeordner und nicht in die Quellen — es wird nie eingecheckt.
    /// </summary>
    private static string BildAblegen(string name, SKBitmap bmp)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Snapshots", "ist");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name + ".png");

        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    private static string Hash(byte[] pixel) => Convert.ToHexStringLower(SHA256.HashData(pixel));
}
