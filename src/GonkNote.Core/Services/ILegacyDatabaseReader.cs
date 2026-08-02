using GonkNote.Core.Models;

namespace GonkNote.Core.Services;

/// <summary>
/// Der vollständige Inhalt einer Altdatenbank, so wie die Migration ihn braucht.
/// <para>
/// Bewusst alles auf einmal statt eines Stroms: die Migration schreibt die neue Datei in
/// **einer** Transaktion und benennt sie erst am Ende um. Was hier nicht ankommt, wird auch
/// nicht geschrieben — und die Altdatei bleibt in jedem Fall unversehrt liegen.
/// </para>
/// </summary>
public sealed class LegacyContent
{
    public List<NoteItem> Items { get; init; } = [];
    public List<WhiteboardDoc> Boards { get; init; } = [];
    public List<TextDoc> Texts { get; init; } = [];

    /// <summary>Schlüssel/Wert der Einstellungen — dieselben Namen wie in der neuen Datenbank.</summary>
    public List<KeyValuePair<string, string>> Settings { get; init; } = [];
}

/// <summary>
/// Liest eine Datenbank aus einem früheren Programmstand (LiteDB). Die Umsetzung liegt
/// bewusst **nicht** in Core, sondern in <c>GonkNote.Legacy</c>: LiteDB benutzt
/// <c>System.Reflection.Emit</c> und lässt sich unter NativeAOT nicht bauen. So schleppt
/// der iPadOS-Kopf es nicht mit — dort gab es nie eine Altdatei.
/// <para>
/// <b>Diese Schnittstelle liest nur.</b> Die Altdatei wird nie beschrieben, nie umbenannt
/// und nie gelöscht; sie bleibt nach der Migration unverändert liegen (HANDOFF §4.8).
/// </para>
/// </summary>
public interface ILegacyDatabaseReader
{
    /// <summary>
    /// Liest die Altdatei vollständig ein. Wirft, wenn sie sich nicht lesen lässt oder einen
    /// Elementtyp enthält, den es nicht mehr gibt — ein stilles Weglassen wäre Datenverlust,
    /// den erst der Nutzer bemerkt, und dann sieht er wie eine leere Seite aus.
    /// </summary>
    LegacyContent Lies(string path);
}
