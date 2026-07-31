using System.Windows;
using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>
/// Schaltet zur Laufzeit zwischen dem Light- und dem Dark-<see cref="ResourceDictionary"/> um.
/// <para>
/// War bis Phase 2 die statische Klasse <c>GonkNote.Services.ThemeService</c>. Sie ist eine
/// Instanz geworden, weil das Umschalten Sache des Kopfes ist: der Avalonia-Kopf hat kein
/// <c>Application.Current.Resources</c>, wohl aber dieselbe Frage „hell oder dunkel".
/// </para>
/// </summary>
public sealed class WpfThemeHost : IThemeHost
{
    public AppTheme Current { get; private set; } = AppTheme.Light;

    public event Action? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        Current = theme;
        var dictionaries = Application.Current.Resources.MergedDictionaries;

        var uri = new Uri(theme == AppTheme.Dark
            ? "Themes/Dark.xaml"
            : "Themes/Light.xaml", UriKind.Relative);

        // Erstes Dictionary ist immer das Theme (siehe App.xaml)
        dictionaries[0] = new ResourceDictionary { Source = uri };
        ThemeChanged?.Invoke();
    }

    public void Toggle() =>
        Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
