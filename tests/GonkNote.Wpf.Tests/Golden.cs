using System.IO;
using System.Reflection;
using System.Text;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Vergleich gegen eine eingecheckte Textfassung (Golden-File).
/// <para>
/// **Warum Text und nicht die Datei selbst:** Ein DOCX ist ein ZIP mit Zeitstempeln und
/// wechselnden Part-Namen, ein PDF trägt Erzeugungsdatum und Objekt-Nummern. Byteweise
/// vergleichen ließe sich beides nur, wenn man vorher alles Veränderliche herausschneidet —
/// und dann vergleicht man ohnehin einen Aufriss. Also gleich einen lesbaren: bei einem
/// Fehlschlag steht im Diff, **was** sich geändert hat, nicht nur dass sich etwas geändert hat.
/// </para>
/// **Neu anlegen bzw. bewusst ändern:** <c>GONK_GOLDEN_UPDATE=1</c> setzen, einmal laufen
/// lassen, den Diff lesen und einchecken. Ohne die Variable legt kein Test von sich aus eine
/// Datei an — ein fehlendes Golden-File ist ein Fehlschlag, keine stille Zustimmung.
/// </summary>
internal static class Golden
{
    public static void Assert(string dateiname, string ist)
    {
        ist = Normalisieren(ist);

        if (Aktualisieren)
        {
            string quelle = Path.Combine(Projektordner, "Fixtures", dateiname);
            Directory.CreateDirectory(Path.GetDirectoryName(quelle)!);
            File.WriteAllText(quelle, ist, new UTF8Encoding(false));
            return;
        }

        string erwartetPfad = Path.Combine(AppContext.BaseDirectory, "Fixtures", dateiname);
        if (!File.Exists(erwartetPfad))
        {
            Xunit.Assert.Fail(
                $"Kein Golden-File „{dateiname}\".\n" +
                $"Erzeugt wurde:\n{Ausschnitt(ist)}\n" +
                $"Stimmt das, mit GONK_GOLDEN_UPDATE=1 einmal laufen lassen und " +
                $"{Path.Combine(Projektordner, "Fixtures", dateiname)} einchecken.");
        }

        string soll = Normalisieren(File.ReadAllText(erwartetPfad));
        if (soll == ist) return;

        string beweis = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ist", dateiname);
        Directory.CreateDirectory(Path.GetDirectoryName(beweis)!);
        File.WriteAllText(beweis, ist, new UTF8Encoding(false));

        Xunit.Assert.Fail(
            $"„{dateiname}\" hat sich geändert.\n{ErsteAbweichung(soll, ist)}\n" +
            $"Das jetzt Erzeugte liegt vollständig unter: {beweis}\n" +
            "Erst lesen, dann entscheiden: Fehler beheben — oder, wenn die Änderung gewollt " +
            "ist, mit GONK_GOLDEN_UPDATE=1 neu setzen.");
    }

    private static bool Aktualisieren =>
        Environment.GetEnvironmentVariable("GONK_GOLDEN_UPDATE") == "1";

    /// <summary>
    /// Zeilenenden vereinheitlichen und am Ende abschneiden. Sonst hängt der Vergleich an der
    /// Git-Einstellung <c>core.autocrlf</c> des Rechners — der Test wäre auf dem Linux-Laptop
    /// rot und hier grün.
    /// </summary>
    private static string Normalisieren(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd() + "\n";

    /// <summary>Die erste abweichende Zeile mit ihrem Umfeld — das, was man wirklich lesen will.</summary>
    private static string ErsteAbweichung(string soll, string ist)
    {
        var a = soll.Split('\n');
        var b = ist.Split('\n');

        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            string za = i < a.Length ? a[i] : "<Datei zu Ende>";
            string zb = i < b.Length ? b[i] : "<Datei zu Ende>";
            if (za == zb) continue;

            var sb = new StringBuilder();
            sb.AppendLine($"Erste Abweichung in Zeile {i + 1}:");
            for (int k = Math.Max(0, i - 2); k < i; k++)
                sb.AppendLine($"  {k + 1,4} | {a[k]}");
            sb.AppendLine($"- {i + 1,4} | {za}");
            sb.AppendLine($"+ {i + 1,4} | {zb}");
            sb.Append($"({a.Length} Zeilen erwartet, {b.Length} erzeugt)");
            return sb.ToString();
        }
        return "Kein Zeilenunterschied gefunden — dann liegt es am Zeilenende.";
    }

    private static string Ausschnitt(string text)
    {
        var zeilen = text.Split('\n');
        return zeilen.Length <= 40
            ? text
            : string.Join('\n', zeilen.Take(40)) + $"\n… ({zeilen.Length - 40} weitere Zeilen)";
    }

    private static string Projektordner =>
        typeof(Golden).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjektOrdner")?.Value
        ?? throw new InvalidOperationException(
            "Assembly-Metadatum „ProjektOrdner\" fehlt — siehe GonkNote.Wpf.Tests.csproj.");
}
