using System.Windows;

namespace GonkNote.Services;

public enum AppTheme
{
    Light,
    Dark,
}

/// <summary>Schaltet zur Laufzeit zwischen Light- und Dark-ResourceDictionary um.</summary>
public static class ThemeService
{
    public static AppTheme Current { get; private set; } = AppTheme.Light;

    public static event Action? ThemeChanged;

    public static void Apply(AppTheme theme)
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

    public static void Toggle() =>
        Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
