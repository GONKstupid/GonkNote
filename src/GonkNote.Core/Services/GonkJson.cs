using System.Text.Json.Serialization;
using GonkNote.Core.Models;

namespace GonkNote.Core.Services;

/// <summary>
/// Der Bauplan, den <c>System.Text.Json</c> zur **Übersetzungszeit** erzeugt, statt ihn zur
/// Laufzeit aus Reflexion zusammenzusuchen.
/// <para>
/// Das ist der Grund, warum LiteDB weichen musste (HANDOFF §1): unter NativeAOT — Pflicht
/// für den App-Store-Weg auf dem iPad — gibt es keinen Just-in-time-Übersetzer, und alles,
/// was auf <c>System.Reflection.Emit</c> baut, stürzt beim ersten Zugriff ab. Ein
/// <see cref="JsonSerializerContext"/> erzeugt den Lese- und Schreibcode dagegen als
/// gewöhnliche C#-Datei; der Trimmer sieht jedes benutzte Feld und wirft nichts weg, was
/// noch gebraucht wird.
/// </para>
/// <b>Wer einen Typ ergänzt, der eigenständig gespeichert wird, trägt ihn hier ein.</b>
/// Typen, die nur *innerhalb* eines der genannten hängen (Seiten, Elemente, Punkte), findet
/// der Generator von selbst über den Objektgraphen.
/// </summary>
[JsonSourceGenerationOptions(
    // Kompakt schreiben: die Datei ist Speicher, kein Lesestoff. Wer trotzdem hineinsehen
    // will, öffnet sie mit einem beliebigen SQLite-Werkzeug und formatiert dort.
    WriteIndented = false,
    // Standardwerte mitschreiben. Das Gegenteil (…WhenWritingDefault) spart Platz, macht
    // aber "Feld fehlt" und "Feld ist 0" ununterscheidbar — bei einem Format, das
    // Bestandsdaten tragen muss, ist das die falsche Sparsamkeit.
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(NoteItem))]
[JsonSerializable(typeof(WhiteboardDoc))]
[JsonSerializable(typeof(TextDoc))]
[JsonSerializable(typeof(CoverStyle))]
internal sealed partial class GonkJson : JsonSerializerContext;
