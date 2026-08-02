using GonkNote.Core.Platform;

namespace GonkNote.Core.Theming;

/// <summary>
/// Die beiden mitgelieferten Farbtabellen.
/// <para>
/// Die Werte sind <b>wörtlich</b> dieselben wie in <c>src/GonkNote.Wpf/Themes/Light.xaml</c>
/// und <c>Dark.xaml</c> — die Farben der App ändern sich mit Phase 3 nicht, sie stehen nur
/// an einer Stelle mehr zur Verfügung. Der WPF-Kopf liest weiterhin sein
/// <c>ResourceDictionary</c>; ihn auf die Tabelle umzustellen wäre ein Umbau im laufenden
/// Kopf ohne Gegenwert und stünde der Regel „der WPF-Kopf verhält sich unverändert"
/// entgegen.
/// </para>
/// <para>
/// <b>Damit die beiden Fassungen nicht auseinanderlaufen</b>, vergleicht der Test
/// <c>FarbtabelleTests</c> im WPF-Testprojekt diese Tabelle Zeile für Zeile mit den beiden
/// XAML-Dateien. Wer hier oder dort eine Farbe ändert, bekommt einen roten Lauf statt zweier
/// Köpfe, die unterschiedlich aussehen.
/// </para>
/// </summary>
public static class Themes
{
    /// <summary>Das helle Erscheinungsbild — die Voreinstellung.</summary>
    public static ThemeDefinition Light { get; } = ThemeDefinition.FromHex(
        "Hell", AppTheme.Light,
        // Grundflächen
        "#F4F7FB",   // WindowBg
        "#EAF0F8",   // SidebarBg
        "#FFFFFF",   // CardBg
        "#FFFFFF",   // ToolbarBg
        "#D4DEEA",   // Border
        // Text
        "#1B2B4B",   // Text
        "#6B7A99",   // TextMuted
        // Akzente
        "#2563EB",   // Accent
        "#DBEAFE",   // AccentSoft
        "#14B8A6",   // Turquoise
        "#EC4899",   // Pink
        "#8B5CF6",   // Purple
        // Interaktion
        "#DEE8F4",   // Hover
        "#CFDDF0",   // Pressed
        "#C7DBFF",   // Selection
        // Das gezeichnete Blatt
        "#E8EDF5",   // CanvasBg
        "#FFFFFF",   // PageBg
        "#BBD2F0",   // PageLine
        "#B8C6DC",   // PageGridDot
        "#1B2B4B");  // DefaultInk

    /// <summary>Das dunkle Erscheinungsbild.</summary>
    public static ThemeDefinition Dark { get; } = ThemeDefinition.FromHex(
        "Dunkel", AppTheme.Dark,
        // Grundflächen
        "#0F1420",   // WindowBg
        "#131A2A",   // SidebarBg
        "#1A2233",   // CardBg
        "#161E30",   // ToolbarBg
        "#2A3550",   // Border
        // Text
        "#E6ECF7",   // Text
        "#8CA0C4",   // TextMuted
        // Akzente
        "#3B82F6",   // Accent
        "#1E3A8A",   // AccentSoft
        "#2DD4BF",   // Turquoise
        "#F472B6",   // Pink
        "#A78BFA",   // Purple
        // Interaktion
        "#223052",   // Hover
        "#2A3B63",   // Pressed
        "#2C3E66",   // Selection
        // Das gezeichnete Blatt
        "#10151F",   // CanvasBg
        "#1E2638",   // PageBg
        "#35486E",   // PageLine
        "#3A4A6B",   // PageGridDot
        "#E6ECF7");  // DefaultInk

    /// <summary>
    /// Die Tabelle zu einer Hell/Dunkel-Auskunft. Das ist der Rückfall, solange es keine
    /// geladenen Tabellen gibt — danach bleibt sie die Vorlage, über die eine eigene Datei
    /// gelegt wird (<see cref="ThemeDefinition.Over"/>).
    /// </summary>
    public static ThemeDefinition ForVariant(AppTheme variant) =>
        variant == AppTheme.Dark ? Dark : Light;
}
