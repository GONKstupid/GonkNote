using GonkNote.Core.Platform;

namespace GonkNote.Core.Theming;

/// <summary>
/// Die beiden mitgelieferten Farbtabellen — <b>die einzige Fassung dieser zwanzig Farben im
/// ganzen Baum</b>.
/// <para>
/// Bis zum 2026-09-10 standen sie ein zweites Mal in <c>src/GonkNote.Wpf/Themes/Light.xaml</c>
/// und <c>Dark.xaml</c>, und ein Wächter im WPF-Testprojekt verglich beide Fassungen Zeile
/// für Zeile — <em>weil zwei Wahrheiten auseinanderlaufen, sobald es niemand nachhält</em>
/// (§4.13). Mit den eigenen Designs baut auch <c>WpfThemeHost</c> sein
/// <c>ResourceDictionary</c> aus dieser Tabelle; die zwei XAML-Dateien sind gelöscht und der
/// Wächter ist damit beantwortet statt abgeschafft.
/// </para>
/// <para>
/// <b>Sie sind zugleich der Rückfall jedes geladenen Designs:</b> was eine Theme-Datei nicht
/// nennt, kommt von hier (<see cref="ThemeDefinition.Over"/>, <see cref="ThemeFile"/>).
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
    /// Die Tabelle zu einer Hell/Dunkel-Auskunft — der Rückfall, und seit den eigenen
    /// Designs zugleich die Vorlage, über die eine eigene Datei gelegt wird
    /// (<see cref="ThemeDefinition.Over"/>).
    /// </summary>
    public static ThemeDefinition ForVariant(AppTheme variant) =>
        variant == AppTheme.Dark ? Dark : Light;
}
