using GonkNote.Core.Rendering;
using GonkNote.Core.Text;
using GonkNote.Core.Theming;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Das mitgelieferte Schriftschema (HANDOFF §4.26).
///
/// <para>
/// <b>Was hier geprüft wird, ist die Auflösung — nicht das Aussehen.</b> Wie ein Buchstabe
/// aussieht, gehört nicht in einen Test (§4.6); <b>welche Datei</b> ihn zeichnet, sehr wohl.
/// Genau daran hing der Fehler, den diese Runde behoben hat: Chrome und Zeichenfläche des
/// Linux-Kopfs konnten verschiedene Schriften benutzen, ohne dass es jemandem auffiel.
/// </para>
/// </summary>
public sealed class SchriftkonzeptTests
{
    /// <summary>
    /// <b>Jede Rolle hat eine Familie, und jede Familie wird mitgeliefert.</b> Eine Rolle, die
    /// auf eine Schrift zeigt, die nicht dabei ist, fiele still auf die Rückfallkette — und
    /// „still" ist genau das, was hier nicht mehr passieren soll.
    /// </summary>
    [Fact]
    public void Jede_Rolle_zeigt_auf_eine_mitgelieferte_Familie()
    {
        var mitgeliefert = Fonts.Mitgeliefert.Select(f => f.Family).ToHashSet();

        foreach (FontRole rolle in Enum.GetValues<FontRole>())
        {
            string familie = Fonts.Standard.Family(rolle);
            Assert.False(string.IsNullOrWhiteSpace(familie), $"Rolle {rolle} hat keine Familie.");
            Assert.Contains(familie, mitgeliefert);
        }
    }

    /// <summary>
    /// Die Rückfallkette ist nicht leer und endet bei einem Sammelnamen. <b>Ohne sie fiele eine
    /// fehlende Schrift in Skias stille Vorgabe</b>, und niemand sähe, dass etwas fehlt.
    /// </summary>
    [Fact]
    public void Die_Rueckfallkette_ist_nicht_leer()
    {
        Assert.NotEmpty(Fonts.Rueckfallkette);
        Assert.All(Fonts.Rueckfallkette, n => Assert.False(string.IsNullOrWhiteSpace(n)));

        // „Segoe UI" bleibt bewusst darin: ein Bestandsdokument, das sie nennt, soll sie unter
        // Windows weiterhin bekommen (§4.14 — der gespeicherte Wert gewinnt).
        Assert.Contains("Segoe UI", Fonts.Rueckfallkette);
    }

    /// <summary>
    /// <b>Jede deklarierte Datei liegt auch da.</b> Ein Tippfehler im Dateinamen wäre sonst
    /// eine Familie, die stumm fehlt — die Registratur überspringt, was sie nicht findet, und
    /// das ist im Betrieb richtig (unvollständiger Ausgabeordner) und im Test falsch.
    /// </summary>
    [Fact]
    public void Jede_deklarierte_Schriftdatei_liegt_neben_dem_Programm()
    {
        foreach (var familie in Fonts.Mitgeliefert)
            foreach (var schnitt in familie.Cuts)
            {
                string pfad = Path.Combine(WbFonts.FontOrdner, familie.Ordner, schnitt.Datei);
                Assert.True(File.Exists(pfad), $"Fehlt: {pfad}");
            }
    }

    /// <summary>
    /// <b>Die Lizenz geht mit.</b> Die SIL Open Font License verlangt, dass Lizenztext und
    /// Copyright-Zeile bei einer Weitergabe dabei sind — weitergegeben wird der Ausgabeordner
    /// und nicht das Repo. Ein Wächter dafür, weil eine fehlende Datei niemandem auffällt,
    /// bevor jemand fragt.
    /// </summary>
    [Fact]
    public void Zu_jeder_Familie_liegt_ihre_Lizenz()
    {
        foreach (var familie in Fonts.Mitgeliefert)
        {
            string pfad = Path.Combine(WbFonts.FontOrdner, familie.Ordner, "OFL.txt");
            Assert.True(File.Exists(pfad), $"Lizenz fehlt: {pfad}");

            string text = File.ReadAllText(pfad);
            Assert.Contains("SIL OPEN FONT LICENSE", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Copyright", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// <b>Alle fünf Familien werden wirklich geladen</b> — nicht nur deklariert. Läuft der Test
    /// unter Linux, ist das zugleich der Beleg, dass die App dort **nicht** von fontconfig
    /// abhängt: die Dateien liegen daneben und Skia liest sie.
    /// </summary>
    [Fact]
    public void Alle_Familien_werden_geladen()
    {
        var geladen = WbFonts.GeladeneFamilien;

        foreach (var familie in Fonts.Mitgeliefert)
            Assert.Contains(familie.Family, geladen);
    }

    /// <summary>
    /// <b>Mitgeliefert schlägt System.</b> Das ist die ganze Entscheidung dieser Runde: Sonst
    /// bekäme derselbe Dateiname auf drei Rechnern drei verschiedene Schriften.
    /// </summary>
    [Fact]
    public void Eine_mitgelieferte_Familie_kommt_aus_der_Datei_und_nicht_vom_System()
    {
        var eigen = WbFonts.Family("Source Sans 3");

        // Der Familienname im Namensverzeichnis der Datei — nicht der Name, mit dem gefragt
        // wurde. Käme hier eine Ersatzschrift, stünde dort etwas anderes.
        Assert.Equal("Source Sans 3", eigen.FamilyName);
    }

    /// <summary>
    /// Stärke und Neigung kommen als **eigene Schnitte** und nicht als verzerrte Regular.
    /// </summary>
    [Fact]
    public void Fett_und_kursiv_sind_eigene_Schnitte()
    {
        var normal = WbFonts.Family("Inter");
        var fett = WbFonts.Family("Inter", bold: true);
        var kursiv = WbFonts.Family("Inter", italic: true);

        Assert.NotSame(normal, fett);
        Assert.NotSame(normal, kursiv);
        Assert.True(fett.IsBold, "Der fette Schnitt meldet sich nicht als fett.");
        Assert.True(kursiv.IsItalic, "Der kursive Schnitt meldet sich nicht als kursiv.");
    }

    /// <summary>
    /// <b>Eine Familie ohne Kursiv liefert den aufrechten Schnitt und nicht nichts.</b> Space
    /// Grotesk hat keine kursiven Schnitte — das ist keine Auslassung, sondern der Umfang der
    /// Familie. Sie darf deshalb nicht in die Rückfallkette rutschen.
    /// </summary>
    [Fact]
    public void Eine_Familie_ohne_Kursiv_bleibt_bei_sich()
    {
        var kursiv = WbFonts.Family("Space Grotesk", italic: true);

        Assert.Equal("Space Grotesk", kursiv.FamilyName);
    }

    /// <summary>
    /// <b>Eine unbekannte Familie wirft nicht.</b> Ein Dokument soll in einer Ersatzschrift
    /// stehen und nicht gar nicht — dieselbe Regel wie vor §4.26 in <c>TdSkiaMeasure</c>.
    /// </summary>
    [Fact]
    public void Eine_unbekannte_Familie_faellt_zurueck_statt_zu_werfen()
    {
        var tf = WbFonts.Family("Diese Schrift gibt es nicht 12345");

        Assert.NotNull(tf);
    }

    /// <summary>
    /// <b>Der Wächter, der die drei früheren Auflösungswege zusammenhält.</b> Umbruch
    /// (<c>TdSkiaMeasure</c>) und Zeichner (<c>TdRenderer</c>) müssen für dasselbe Format
    /// dieselbe Schrift bekommen — sonst bricht eine Zeile an einer Stelle um und steht an
    /// einer anderen. Geprüft über die **Breite**: dieselbe Schrift in derselben Größe misst
    /// dasselbe, eine andere fast nie.
    /// </summary>
    [Fact]
    public void Umbruch_und_Zeichner_benutzen_dieselbe_Schrift()
    {
        var format = new TdCharFormat { FontFamily = "Source Sans 3", FontSize = 24, Bold = true };
        var a = format.Aufgeloest();

        using var messung = new TdSkiaMeasure();
        double breiteCm = messung.WidthCm("Handgloves", format);

        // Derselbe Weg, den TdRenderer geht.
        using var schrift = WbFonts.Font("Source Sans 3", (float)a.FontSize!.Value, bold: true, italic: false);
        double alsPunkt = schrift.MeasureText("Handgloves");

        Assert.Equal(breiteCm, alsPunkt * (2.54 / 72.0), 6);
    }

    /// <summary>
    /// <b>Die Grundschrift eines neuen Dokuments ist die Rolle „Body"</b> und steht nicht
    /// zweimal im Code. Sie ist Datenformat: was hier steht, landet in jeder neuen Datei und
    /// geht so nach DOCX.
    /// </summary>
    [Fact]
    public void Die_Grundschrift_des_Dokuments_kommt_aus_dem_Schema()
    {
        Assert.Equal(Fonts.Standard.Family(FontRole.Body), TdCharFormat.Standard.FontFamily);
        Assert.Equal("Source Sans 3", TdCharFormat.Standard.FontFamily);
    }

    /// <summary>
    /// Ein Schemawechsel wirft den Zwischenspeicher weg. <b>Bis §4.26 blieb ein später
    /// gesetzter Wert wirkungslos</b> — schlimmer als ein Fehler, weil nichts darauf hinwies.
    /// </summary>
    [Fact]
    public void Ein_Schemawechsel_wirkt_sofort()
    {
        var vorher = WbFonts.Schema;
        try
        {
            var eigenes = new FontScheme(
                new Dictionary<FontRole, string> { [FontRole.Ui] = "JetBrains Mono" },
                Fonts.Rueckfallkette);

            WbFonts.Schema = eigenes;
            Assert.Equal("JetBrains Mono", WbFonts.UiFamily);
            Assert.Equal("JetBrains Mono", WbFonts.Regular.FamilyName);
        }
        finally
        {
            WbFonts.Schema = vorher;
        }
    }
}
