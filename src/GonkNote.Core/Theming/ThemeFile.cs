using System.Text.Json;
using System.Text.Json.Serialization;
using GonkNote.Core.Platform;
using GonkNote.Services;

namespace GonkNote.Core.Theming;

/// <summary>
/// Die Gestalt einer Theme-Datei auf der Platte. Absichtlich <b>drei</b> Felder und nicht
/// zwanzig: ein Name, die Auskunft hell/dunkel und ein Wörterbuch aus Farbnamen.
/// </summary>
/// <remarks>
/// Ein Wörterbuch statt zwanzig Eigenschaften, weil die Datei <b>unvollständig sein darf</b>
/// (HANDOFF §6, Nutzer-Entscheidung 2026-09-10): Was fehlt, kommt aus Hell bzw. Dunkel. Mit
/// zwanzig Eigenschaften wäre „nicht gesetzt" von „auf Schwarz gesetzt" nicht zu
/// unterscheiden — dieselbe Frage wie im Dokumentmodell (§4.14), und dort ist sie mit
/// nullbaren Feldern beantwortet worden.
/// </remarks>
public sealed class ThemeFileData
{
    /// <summary>Anzeigename im Menü. Fehlt er, tritt der Dateiname an seine Stelle.</summary>
    public string? Name { get; set; }

    /// <summary>„light" oder „dark" — <b>Pflicht</b>, siehe <see cref="ThemeFile.TryParse"/>.</summary>
    public string? Variant { get; set; }

    /// <summary>Farbname aus <see cref="ThemeColor"/> → Hex-Wert. Alles freiwillig.</summary>
    public Dictionary<string, string>? Colors { get; set; }
}

/// <summary>
/// Der Bauplan für <see cref="ThemeFileData"/>, zur Übersetzungszeit erzeugt.
/// <para>
/// <b>Warum nicht einfach XAML lesen?</b> Weil ein XAML-Theme drei Dinge wäre, die es nicht
/// sein soll (HANDOFF §6): unter NativeAOT nicht lauffähig (<c>XamlReader</c> lebt von
/// Reflexion), ein reines Windows-Format (Avalonia hat kein <c>ResourceDictionary</c> im
/// WPF-Sinn) und <b>ausführbarer Code</b> — XAML kann Typen erzeugen. Bei einer Datei, die
/// Nutzer untereinander weitergeben, ist das der falsche Vertrag. Eine JSON mit zwanzig
/// Zeichenketten kann nichts als Farben sein.
/// </para>
/// </summary>
[JsonSourceGenerationOptions(
    // Diese Datei ist ausdrücklich Lesestoff — sie wird von Hand geschrieben, anders als
    // der Datenbank-Json (GonkJson, dort WriteIndented = false).
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(ThemeFileData))]
internal sealed partial class ThemeJson : JsonSerializerContext;

/// <summary>
/// Liest und schreibt eine Theme-Datei. <b>Ohne Dateisystem</b> — das steht in
/// <see cref="ThemeLibrary"/>; hier geht es nur um Text hinein und eine Farbtabelle hinaus.
///
/// <para>
/// <b>Die Regel für unvollständige Dateien</b> (Nutzer, 2026-09-10): fehlende Farben werden
/// <b>still</b> aus der mitgelieferten Tabelle derselben Variante ergänzt. Eine Datei mit
/// drei Farben ist damit ein gültiges Theme. Eine Datei abzulehnen, weil ihr siebzehn
/// Angaben fehlen, wäre bei etwas, das Nutzer von Hand schreiben, die falsche Strenge — und
/// der Mechanismus dafür steht seit Phase 3 (<see cref="ThemeDefinition.Over"/>).
/// </para>
/// <para>
/// <b>Was dagegen gemeldet wird:</b> kaputtes JSON, eine fehlende oder unbekannte Variante
/// und ein <i>unlesbarer</i> Farbwert. Der Unterschied ist Absicht: „nicht dagewesen" ist
/// eine Aussage des Nutzers, „<c>#GG00ZZ</c>" ist ein Tippfehler. Ihn still auf die
/// Vorgabefarbe zurückfallen zu lassen hieße, jemanden nach einer Farbe suchen zu lassen,
/// die nie ankommt.
/// </para>
/// </summary>
public static class ThemeFile
{
    /// <summary>Die Endung einer Theme-Datei — an genau einer Stelle.</summary>
    public const string Extension = ".json";

    /// <summary>
    /// Aus Text eine Farbtabelle. <paramref name="fallbackName"/> tritt ein, wenn die Datei
    /// selbst keinen Namen nennt (üblicherweise der Dateiname ohne Endung).
    /// </summary>
    /// <returns><c>false</c> samt Begründung in <paramref name="fehler"/>, statt zu werfen —
    /// der Aufrufer ist ein Menüpunkt und kein Programmierfehler. <b>Dasselbe Muster wie
    /// <see cref="HexColor.TryParse"/> daneben</b>, und aus demselben Grund.</returns>
    public static bool TryParse(string json, string fallbackName,
        out ThemeDefinition? theme, out string? fehler)
    {
        theme = null;
        fehler = null;

        ThemeFileData? daten;
        try
        {
            daten = JsonSerializer.Deserialize(json, ThemeJson.Default.ThemeFileData);
        }
        catch (JsonException ex)
        {
            fehler = Loc.T("Theme.Err.Json", ex.Message);
            return false;
        }

        if (daten == null)
        {
            fehler = Loc.T("Theme.Err.Empty");
            return false;
        }

        // Die Variante ist Pflicht und wird ausdrücklich **nicht** aus der Helligkeit von
        // PageBg geraten (HANDOFF §6, Punkt 4). Sie ist keine Farbe, sondern eine Auskunft:
        // WbRenderer wählt danach seine Vorgaben und die Titelleiste unter Windows ihren
        // dunklen Modus. Ein Ratespiel wäre an genau den Stellen falsch, an denen ein Theme
        // interessant wird — etwa einem dunklen Bild mit hellem Papier.
        if (!TryVariant(daten.Variant, out var variante))
        {
            fehler = Loc.T("Theme.Err.Variant", daten.Variant ?? "");
            return false;
        }

        var auflagen = new List<(ThemeColor, HexColor)>();
        foreach (var (schluessel, wert) in daten.Colors ?? [])
        {
            // Ein unbekannter Farbname ist **kein** Fehler: eine Datei aus einer späteren
            // Fassung, die eine einundzwanzigste Farbe kennt, soll hier trotzdem laufen.
            // Die Reihenfolge in ThemeColor ist Teil des Formats, die Namen sind es auch —
            // was wir nicht kennen, geht uns nichts an.
            if (!Enum.TryParse<ThemeColor>(schluessel, ignoreCase: true, out var farbe)) continue;

            if (!HexColor.TryParse(wert, out var hex))
            {
                fehler = Loc.T("Theme.Err.Color", schluessel, wert ?? "");
                return false;
            }
            auflagen.Add((farbe, hex));
        }

        string name = string.IsNullOrWhiteSpace(daten.Name) ? fallbackName : daten.Name.Trim();
        theme = Themes.ForVariant(variante).Over(name, variante, auflagen);
        return true;
    }

    /// <summary>
    /// Eine Farbtabelle als Datei-Text — <b>vollständig</b>, mit allen zwanzig Farben.
    /// <para>
    /// Das ist die Vorlage, mit der ein eigenes Theme anfängt: wer aus dem aktiven Bild eine
    /// Datei schreibt, ändert darin Hex-Werte und lädt sie zurück. Der Weg über eine Vorlage
    /// statt über eine Beschreibung in der Doku ist eine Nutzer-Entscheidung (2026-09-10) —
    /// zwanzig Farbnamen aus einer Anleitung abzutippen ist Arbeit, die niemand macht.
    /// </para>
    /// </summary>
    public static string ToJson(ThemeDefinition theme)
    {
        var daten = new ThemeFileData
        {
            Name = theme.Name,
            Variant = theme.Variant == AppTheme.Dark ? "dark" : "light",
            Colors = theme.Entries.ToDictionary(e => e.Color.ToString(), e => e.Value.ToString()),
        };
        return JsonSerializer.Serialize(daten, ThemeJson.Default.ThemeFileData);
    }

    private static bool TryVariant(string? text, out AppTheme variante)
    {
        variante = AppTheme.Light;
        if (string.IsNullOrWhiteSpace(text)) return false;

        switch (text.Trim().ToLowerInvariant())
        {
            case "dark":
            case "dunkel":
                variante = AppTheme.Dark;
                return true;
            case "light":
            case "hell":
                variante = AppTheme.Light;
                return true;
            default:
                return false;
        }
    }
}
