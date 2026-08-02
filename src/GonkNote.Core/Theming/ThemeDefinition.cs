using GonkNote.Core.Platform;

namespace GonkNote.Core.Theming;

/// <summary>
/// Ein vollständiges Erscheinungsbild: ein Name, die Auskunft „hell oder dunkel" und
/// zwanzig Farben.
/// <para>
/// <b>Warum das in Core liegt und nicht im Kopf:</b> bis Phase 2 war ein Theme ein Paar
/// fest verdrahteter <c>ResourceDictionary</c>-Dateien im WPF-Kopf. Jeder weitere Kopf
/// hätte dasselbe Paar noch einmal gebraucht, und der vorgemerkte Wunsch „eigene
/// Farbschemata" (HANDOFF §6) wäre je Kopf einzeln umzusetzen gewesen. Jetzt hält Core die
/// Tabelle und <b>jeder Kopf übersetzt sie in seine eigenen Pinsel</b>.
/// </para>
/// <para>
/// Die Klasse ist bewusst unveränderlich und ohne Datei-Zugriff: Laden aus
/// <c>%APPDATA%\GonkNote\Themes\*.json</c> ist für nach Meilenstein M1 vorgesehen und
/// braucht dann nichts weiter als einen Deserialisierer, der <see cref="Over"/> aufruft.
/// </para>
/// </summary>
public sealed class ThemeDefinition
{
    /// <summary>Anzahl der Farben — die Länge von <see cref="ThemeColor"/>.</summary>
    public const int ColorCount = (int)ThemeColor.DefaultInk + 1;

    private readonly HexColor[] _colors;

    private ThemeDefinition(string name, AppTheme variant, HexColor[] colors)
    {
        Name = name;
        Variant = variant;
        _colors = colors;
    }

    /// <summary>Anzeigename, z. B. „Hell". Bei einer geladenen Datei deren Name.</summary>
    public string Name { get; }

    /// <summary>
    /// Hell oder dunkel. Das ist <b>keine</b> Farbe, sondern eine Auskunft: der Renderer
    /// wählt danach seine Voreinstellungen, und die Titelleiste unter Windows braucht sie
    /// für ihren dunklen Modus. Auch ein selbst gebautes Theme muss sich hier festlegen.
    /// </summary>
    public AppTheme Variant { get; }

    public HexColor this[ThemeColor color] => _colors[(int)color];

    /// <summary>Alle Farben in der Reihenfolge von <see cref="ThemeColor"/> — für Köpfe, die ihre Ressourcen in einer Schleife aufbauen.</summary>
    public IEnumerable<(ThemeColor Color, HexColor Value)> Entries
    {
        get
        {
            for (int i = 0; i < ColorCount; i++)
                yield return ((ThemeColor)i, _colors[i]);
        }
    }

    /// <summary>
    /// Baut eine Tabelle aus Text-Werten. <paramref name="hex"/> muss <see cref="ColorCount"/>
    /// Einträge in der Reihenfolge von <see cref="ThemeColor"/> haben; jeder muss sich lesen
    /// lassen. Das ist der Weg für die <b>mitgelieferten</b> Tabellen: dort ist ein Tippfehler
    /// ein Programmierfehler und soll beim ersten Start auffallen, nicht still eine schwarze
    /// Fläche ergeben.
    /// </summary>
    public static ThemeDefinition FromHex(string name, AppTheme variant, params string[] hex)
    {
        if (hex.Length != ColorCount)
            throw new ArgumentException(
                $"Eine Farbtabelle hat {ColorCount} Einträge, hier sind es {hex.Length}.", nameof(hex));

        var colors = new HexColor[ColorCount];
        for (int i = 0; i < ColorCount; i++)
        {
            if (!HexColor.TryParse(hex[i], out colors[i]))
                throw new ArgumentException(
                    $"„{hex[i]}\" ist keine Farbe (Eintrag {(ThemeColor)i}).", nameof(hex));
        }
        return new ThemeDefinition(name, variant, colors);
    }

    /// <summary>
    /// Legt <paramref name="overrides"/> über diese Tabelle: was dort steht, gewinnt, alles
    /// andere bleibt.
    /// <para>
    /// Das ist die Antwort auf „was passiert bei einer unvollständigen Datei?" — eine
    /// Theme-Datei mit drei Farben ist genug, der Rest kommt aus Hell bzw. Dunkel. Eine
    /// Datei abzulehnen, weil ihr siebzehn Angaben fehlen, wäre bei einer Datei, die Nutzer
    /// von Hand schreiben, die falsche Strenge.
    /// </para>
    /// </summary>
    public ThemeDefinition Over(string name, AppTheme variant, IEnumerable<(ThemeColor Color, HexColor Value)> overrides)
    {
        var colors = (HexColor[])_colors.Clone();
        foreach (var (color, value) in overrides) colors[(int)color] = value;
        return new ThemeDefinition(name, variant, colors);
    }
}
