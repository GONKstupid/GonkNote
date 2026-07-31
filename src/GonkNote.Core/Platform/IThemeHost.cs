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

    void Apply(AppTheme theme);

    void Toggle();

    /// <summary>
    /// Nach jedem Wechsel. Alles, was Farben nicht über eine Bindung bezieht (gezeichnete
    /// Seiten, Symbolfarben, Titelleiste), hängt hier — siehe HANDOFF §7 „Texte, die der
    /// Code setzt".
    /// </summary>
    event Action? ThemeChanged;
}
