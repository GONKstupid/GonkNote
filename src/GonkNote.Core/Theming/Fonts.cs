namespace GonkNote.Core.Theming;

/// <summary>
/// Wofür eine Schrift da ist. <b>Nicht welche</b> — das steht im Schema.
/// </summary>
public enum FontRole
{
    /// <summary>App-Oberfläche: Menüs, Ordnerbaum, Galerie, Dialoge, Ribbon.</summary>
    Ui,

    /// <summary>Der Fließtext eines Textdokuments — die Grundschrift auf dem Papier.</summary>
    Body,

    /// <summary>Code und alles, was in Spalten stehen muss.</summary>
    Mono,

    /// <summary>Cover-Titel und große Überschriften.</summary>
    Display,

    /// <summary>Textfelder, Notizzettel und Sticker auf dem Whiteboard.</summary>
    Handwriting,
}

/// <summary>
/// Ein Schnitt einer mitgelieferten Familie: welche Datei zu welcher Stärke und Neigung gehört.
///
/// <para>
/// <b>Die Zuordnung steht hier und wird nicht aus der Datei gelesen.</b> Der Familienname im
/// Namensverzeichnis einer TTF ist nicht verlässlich: Für „SemiBold" tragen viele Schriften dort
/// <c>„Inter SemiBold"</c> als eigene Familie ein statt <c>„Inter"</c> mit einer Stärke — wer
/// sich darauf verlässt, findet den Schnitt später nicht wieder. Deklariert ist deklariert.
/// </para>
/// </summary>
/// <param name="Datei">Dateiname unterhalb von <c>Assets/Fonts/&lt;Familie&gt;/</c>.</param>
/// <param name="Bold">Ist das der fette Schnitt?</param>
/// <param name="Italic">Ist das der kursive Schnitt?</param>
public readonly record struct FontCut(string Datei, bool Bold = false, bool Italic = false);

/// <summary>
/// Eine mitgelieferte Schriftfamilie: ihr Name, ihre Schnitte und ihre Lizenz.
/// </summary>
/// <param name="Family">
/// Der Name, unter dem sie angesprochen wird — <b>auch aus einem Dokument heraus</b>. Er steht
/// im Datenformat (<c>TdCharFormat.FontFamily</c>) und ist damit nicht frei umbenennbar.
/// </param>
/// <param name="Ordner">Der Ordner unter <c>Assets/Fonts/</c>.</param>
/// <param name="Cuts">Die Schnitte, die mitgeliefert werden — nicht alle, die es gibt.</param>
public sealed record BundledFont(string Family, string Ordner, IReadOnlyList<FontCut> Cuts);

/// <summary>
/// Das Schriftschema: welche Familie welche Rolle spielt.
///
/// <para>
/// <b>Ein Schriftschema ist eine Datentabelle</b> — dasselbe Muster wie bei den Farben (§4.9,
/// „ein Theme ist eine Datentabelle"), eine Ebene weiter. Es steht in Core und gilt damit für
/// alle drei Köpfe; die Plattform wählt nicht mehr mit.
/// </para>
/// </summary>
/// <param name="Rollen">Rolle → Familienname.</param>
/// <param name="Rueckfall">
/// Die Kette, die greift, wenn eine Familie nicht da ist — in dieser Reihenfolge, zuletzt die
/// Systemvorgabe. <b>Sie ist kein Beiwerk:</b> Ohne sie fiele eine fehlende Schrift in Skias
/// stille Standardschrift, und niemand sähe, dass etwas fehlt.
/// </param>
public sealed record FontScheme(
    IReadOnlyDictionary<FontRole, string> Rollen,
    IReadOnlyList<string> Rueckfall)
{
    /// <summary>Die Familie zu einer Rolle — oder die erste Rückfallschrift.</summary>
    public string Family(FontRole rolle) =>
        Rollen.TryGetValue(rolle, out var name) ? name : Rueckfall[0];
}

/// <summary>
/// Das mitgelieferte Schriftschema — der einzige Ort, an dem die fünf Familiennamen stehen.
///
/// <para>
/// <b>Warum mitgeliefert und nicht vom System genommen.</b> „Segoe UI" gibt es unter Linux
/// nicht und unter iPadOS auch nicht; umgekehrt ist auf keinem Linux-Rechner eine bestimmte
/// Schrift garantiert. Bis hierher hat die App die Frage je Plattform verschieden beantwortet —
/// mit dem Ergebnis, dass **dasselbe Dokument auf drei Systemen drei Schriftbilder** hatte und
/// der Avalonia-Kopf sein Chrome sogar in einer anderen Schrift zeichnete als seine
/// Zeichenfläche (§4.26). Eine mitgelieferte Schrift beantwortet die Frage einmal.
/// </para>
/// <para>
/// <b>Alle fünf stehen unter der SIL Open Font License 1.1</b> — Lizenztext und Copyright-Zeile
/// liegen je Familie als <c>OFL.txt</c> daneben und gehen mit der Exe hinaus. Sie werden
/// <b>weder verändert noch beschnitten</b>: Damit kommen Reserved Font Names gar nicht erst ins
/// Spiel, und das ist kein theoretischer Punkt — <b>Source Sans führt „Source" als RFN</b>.
/// Auszuwählen, welche Schnitte mitgehen, ist keine Veränderung.
/// </para>
/// </summary>
public static class Fonts
{
    /// <summary>Der Ordner unterhalb des Programmverzeichnisses.</summary>
    public const string Ordner = "Fonts";

    /// <summary>
    /// Die Rückfallkette. <b>„Segoe UI" steht bewusst noch darin</b> — ein Bestandsdokument, in
    /// dem sie steht, soll sie unter Windows weiterhin bekommen (§4.14: der gespeicherte Wert
    /// gewinnt). Nur die *Vorgabe* für neue Dokumente hat gewechselt.
    /// </summary>
    public static IReadOnlyList<string> Rueckfallkette { get; } =
        ["Inter", "Segoe UI", "DejaVu Sans", "sans-serif"];

    /// <summary>Das Schema, mit dem die App ausgeliefert wird.</summary>
    public static FontScheme Standard { get; } = new(
        new Dictionary<FontRole, string>
        {
            [FontRole.Ui] = "Inter",
            [FontRole.Body] = "Source Sans 3",
            [FontRole.Mono] = "JetBrains Mono",
            [FontRole.Display] = "Space Grotesk",
            [FontRole.Handwriting] = "Geist",
        },
        Rueckfallkette);

    /// <summary>
    /// Was mitgeliefert wird. <b>Nicht jede Familie in jeder Stärke</b> — was nicht benutzt
    /// wird, wiegt nur in der Exe. Space Grotesk hat gar keine kursiven Schnitte; das ist keine
    /// Auslassung, sondern der Umfang der Familie.
    /// </summary>
    public static IReadOnlyList<BundledFont> Mitgeliefert { get; } =
    [
        new("Inter", "Inter",
        [
            new("Inter-Regular.ttf"),
            new("Inter-Italic.ttf", Italic: true),
            new("Inter-SemiBold.ttf"),
            new("Inter-Bold.ttf", Bold: true),
            new("Inter-BoldItalic.ttf", Bold: true, Italic: true),
        ]),
        new("Source Sans 3", "SourceSans3",
        [
            new("SourceSans3-Regular.ttf"),
            new("SourceSans3-It.ttf", Italic: true),
            new("SourceSans3-Semibold.ttf"),
            new("SourceSans3-Bold.ttf", Bold: true),
            new("SourceSans3-BoldIt.ttf", Bold: true, Italic: true),
        ]),
        new("JetBrains Mono", "JetBrainsMono",
        [
            new("JetBrainsMono-Regular.ttf"),
            new("JetBrainsMono-Italic.ttf", Italic: true),
            new("JetBrainsMono-Bold.ttf", Bold: true),
            new("JetBrainsMono-BoldItalic.ttf", Bold: true, Italic: true),
        ]),
        new("Space Grotesk", "SpaceGrotesk",
        [
            new("SpaceGrotesk-Regular.ttf"),
            new("SpaceGrotesk-Medium.ttf"),
            new("SpaceGrotesk-Bold.ttf", Bold: true),
        ]),
        new("Geist", "Geist",
        [
            new("Geist-Regular.ttf"),
            new("Geist-Italic.ttf", Italic: true),
            new("Geist-SemiBold.ttf"),
            new("Geist-Bold.ttf", Bold: true),
            new("Geist-BoldItalic.ttf", Bold: true, Italic: true),
        ]),
    ];
}
