using GonkNote.Core.Theming;

namespace GonkNote.Core.Platform;

public enum AppTheme
{
    Light,
    Dark,
}

/// <summary>
/// Das aktive Erscheinungsbild. Der Kopf besitzt die Farbmittel (ResourceDictionary,
/// Styles) — Core und ViewModels wollen nur wissen, welches gerade gilt, und es umschalten
/// können.
/// </summary>
public interface IThemeHost
{
    AppTheme Current { get; }

    /// <summary>
    /// Die aktive Farbtabelle — seit dem 2026-09-10 (eigene Designs) die vollständige
    /// Auskunft, während <see cref="Current"/> nur noch „hell oder dunkel" beantwortet.
    /// <para>
    /// Beides steht nebeneinander und keines ist überflüssig: <c>WbRenderer</c> und die
    /// Titelleiste unter Windows brauchen die Variante, das Menü und die Einstellungen
    /// brauchen Name und Herkunft.
    /// </para>
    /// </summary>
    ThemeDefinition Definition { get; }

    void Apply(AppTheme theme);

    /// <summary>
    /// Eine beliebige Farbtabelle anlegen — der Weg für ein geladenes Design.
    /// <see cref="Apply(AppTheme)"/> ist der Sonderfall „nimm die mitgelieferte hell bzw.
    /// dunkel" (<see cref="Theming.Themes.ForVariant"/>).
    /// </summary>
    void Apply(ThemeDefinition theme);

    void Toggle();

    /// <summary>
    /// Nach jedem Wechsel. Alles, was Farben nicht über eine Bindung bezieht (gezeichnete
    /// Seiten, Symbolfarben, Titelleiste), hängt hier — siehe HANDOFF §7 „Texte, die der
    /// Code setzt".
    /// </summary>
    event Action? ThemeChanged;
}
