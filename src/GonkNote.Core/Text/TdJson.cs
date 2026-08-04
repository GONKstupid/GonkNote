using System.Text.Json;
using System.Text.Json.Serialization;

namespace GonkNote.Core.Text;

/// <summary>
/// Der zur Übersetzungszeit erzeugte Bauplan für <see cref="TdDocument"/> — dieselbe
/// Begründung wie bei <c>GonkJson</c>: unter NativeAOT gibt es keine Reflexion zur Laufzeit,
/// und ein <see cref="JsonSerializerContext"/> erzeugt den Lese- und Schreibcode als
/// gewöhnliche C#-Datei (HANDOFF §1).
/// <para>
/// <b>Ein eigener Kontext und nicht ein Eintrag in <c>GonkJson</c>:</b> dort stehen die
/// Typen, die die **Datenbank** speichert. Das Textformat ist ein Dokumentformat — es wird
/// auch in Dateien geschrieben und wieder gelesen, und es hat mit
/// <c>DefaultIgnoreCondition = Never</c> andere Anforderungen als eine Datenbankzeile
/// (siehe unten).
/// </para>
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    // **Hier anders als in GonkJson.** Dort werden Standardwerte mitgeschrieben, weil „Feld
    // fehlt" und „Feld ist 0" sonst ununterscheidbar wären. Genau diese Unterscheidung ist
    // hier aber der ganze Sinn: ein Format-Feld ist `null`, solange es *nicht gesetzt* ist,
    // und `null` schreibt man nicht in die Datei — sonst stünde in jedem Lauf eines
    // Dokuments neunmal `null`, und das ist bei einem Feld je Zeichenauszeichnung kein
    // kleiner Unterschied. Nullable-Felder tragen ihre Bedeutung im Weglassen.
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TdDocument))]
internal sealed partial class TdJson : JsonSerializerContext;

/// <summary>
/// Lesen und Schreiben des Dokumentformats.
/// <para>
/// <b>UTF-8-Bytes und kein String</b>: der Inhalt landet in <c>TextDoc.Rtf</c> — einem
/// <c>byte[]</c> —, und ein Umweg über eine Zeichenkette wäre eine zusätzliche Kopie des
/// ganzen Dokuments bei jedem Speichern.
/// </para>
/// </summary>
public static class TdFormatIo
{
    /// <summary>
    /// Kennung am Anfang jedes gespeicherten Dokuments: <c>GNTD</c> („Gonk Note
    /// TextDokument").
    /// <para>
    /// <b>Sie ist der Grund, warum die Übernahme später gefahrlos ist.</b> Das Feld, in dem
    /// der Inhalt liegt, heißt historisch <c>Rtf</c> und trägt heute je nach Alter RTF oder
    /// ein WPF-<c>XamlPackage</c> (ZIP, erkennbar am „PK"). Ein drittes Format dazwischen
    /// lässt sich nur unterscheiden, wenn es sich selbst zu erkennen gibt — dieselbe
    /// Überlegung, aus der eine SQLite-Datei an ihren ersten sechzehn Bytes erkannt wird
    /// und nicht an ihrer Endung (HANDOFF §4.8).
    /// </para>
    /// </summary>
    public static ReadOnlySpan<byte> Kennung => "GNTD"u8;

    /// <summary>Trägt <paramref name="bytes"/> die Kennung dieses Formats?</summary>
    public static bool IstEigenesFormat(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 4 && bytes[..4].SequenceEqual(Kennung);

    /// <summary>Schreibt das Dokument als Kennung + UTF-8-Json.</summary>
    public static byte[] Schreiben(TdDocument doc)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(doc, TdJson.Default.TdDocument);

        var ziel = new byte[Kennung.Length + json.Length];
        Kennung.CopyTo(ziel);
        json.CopyTo(ziel, Kennung.Length);
        return ziel;
    }

    /// <summary>
    /// Liest ein Dokument. Gibt <c>null</c> zurück, wenn die Bytes **nicht** dieses Format
    /// sind — also bei RTF, bei einem XamlPackage und bei leeren Bytes.
    /// <para>
    /// <b>Kein Wurf und keine Ausnahme:</b> „das ist ein Altformat" ist der Normalfall,
    /// solange die Übernahme läuft, und kein Fehler. Wer eine Ausnahme daraus macht,
    /// bekommt sie beim Öffnen jedes Bestandsdokuments.
    /// </para>
    /// </summary>
    public static TdDocument? Lesen(ReadOnlySpan<byte> bytes)
    {
        if (!IstEigenesFormat(bytes)) return null;

        // Eine kaputte Datei ist etwas anderes als ein fremdes Format: sie trägt die Kennung
        // und ist trotzdem unlesbar. Sie darf nicht als „Altformat" durchgehen, sonst
        // versucht der Kopf, sie als RTF zu laden, und meldet einen Fehler an der falschen
        // Stelle.
        return JsonSerializer.Deserialize(bytes[Kennung.Length..], TdJson.Default.TdDocument)
               ?? TdDocument.Leer();
    }
}
