using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using GonkNote.Core.Platform;
using GonkNote.Core.Theming;
using GonkNote.Views;

namespace GonkNote.Platform;

/// <summary>
/// Das Erscheinungsbild des Linux-Kopfs — <b>aus der Farbtabelle in Core gebaut</b>, nicht
/// aus zwei fest verdrahteten Dateien.
/// <para>
/// Das ist die Entscheidung vom 2026-08-02 (HANDOFF §6): der WPF-Kopf hat sein Theme als
/// Paar von <c>ResourceDictionary</c>-Dateien, und noch ein zweites solches Paar hätte den
/// vorgemerkten Wunsch „eigene Farbschemata" zu einem Umbau gemacht statt zu einer Zutat.
/// Hier steht stattdessen eine Schleife über <see cref="ThemeDefinition.Entries"/>. Ein
/// geladenes Theme später einzuhängen heißt: eine andere <see cref="ThemeDefinition"/> an
/// <see cref="Apply(ThemeDefinition)"/> geben — mehr nicht.
/// </para>
/// </summary>
public sealed class AvaloniaThemeHost : IThemeHost
{
    /// <summary>
    /// Die aktive Farbtabelle. Statisch, weil die Konverter sie brauchen, bevor es ein
    /// Fenster gibt — und weil es genau eine gibt: das Erscheinungsbild ist keine
    /// Eigenschaft eines einzelnen Fensters.
    /// </summary>
    public static ThemeDefinition Current { get; private set; } = Themes.Light;

    /// <summary>Das zur Laufzeit gefüllte Wörterbuch; liegt immer an Stelle 0 (siehe App.axaml).</summary>
    private const int ThemenPlatz = 0;

    AppTheme IThemeHost.Current => Current.Variant;

    public event Action? ThemeChanged;

    public void Apply(AppTheme theme) => Apply(Themes.ForVariant(theme));

    /// <summary>
    /// Eine beliebige Farbtabelle anlegen. <see cref="IThemeHost.Apply(AppTheme)"/> ist der
    /// Sonderfall „nimm die mitgelieferte hell bzw. dunkel".
    /// </summary>
    public void Apply(ThemeDefinition theme)
    {
        Current = theme;

        var app = Application.Current;
        if (app == null) return;   // vor dem Start des Rahmens — dann genügt Current

        var farben = new ResourceDictionary();
        foreach (var (name, wert) in theme.Entries)
        {
            // Zu jeder Farbe beides: den Pinsel für Flächen und Text, und die rohe Farbe
            // für alles, was selbst zeichnet. Der WPF-Kopf trennt das (15 Pinsel, 5 Farben),
            // weil dort jeder Eintrag von Hand steht; hier kostet die zweite Form nichts
            // und erspart der XAML die Frage, welche der beiden Formen es gerade gibt.
            farben[$"Brush.{name}"] = wert.ToBrush();
            farben[$"Color.{name}"] = wert.ToAvalonia();
        }

        var zusammen = app.Resources.MergedDictionaries;
        while (zusammen.Count <= ThemenPlatz) zusammen.Add(new ResourceDictionary());
        zusammen[ThemenPlatz] = farben;

        // Damit auch das mitgelieferte Fluent-Aussehen mitwechselt: Rollbalken, Menüs,
        // Textfelder und Bildlaufleisten holen ihre Farben von dort, nicht aus der Tabelle.
        // Ohne diese Zeile stünde ein helles Kontextmenü vor einer dunklen Oberfläche.
        app.RequestedThemeVariant = theme.Variant == AppTheme.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;

        ThemeChanged?.Invoke();
    }

    public void Toggle() =>
        Apply(Current.Variant == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
