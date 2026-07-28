using System.Globalization;
using System.ComponentModel;

namespace GonkNote.Services;

public enum AppLanguage
{
    German,
    English,
}

/// <summary>
/// Übersetzungen der Oberfläche. Eine Stelle, an der jeder sichtbare Text steht – die
/// Sprache lässt sich zur Laufzeit umschalten, ohne die App neu zu starten.
/// <para>
/// Aufgerufen wird sie aus XAML über <c>{loc:T Menu.File}</c> und aus C# über
/// <c>Loc.T("Menu.File")</c>. Fehlt ein Schlüssel in der englischen Tabelle, erscheint der
/// deutsche Text – eine unvollständige Übersetzung führt also nie zu leeren Beschriftungen.
/// </para>
/// </summary>
public static class Loc
{
    /// <summary>Bindungsquelle für XAML; meldet beim Sprachwechsel alle Texte als geändert.</summary>
    public static LocSource Source { get; } = new();

    public static AppLanguage Current { get; private set; } = AppLanguage.German;

    /// <summary>Wird nach einem Sprachwechsel ausgelöst (für Code, der Texte selbst setzt).</summary>
    public static event Action? LanguageChanged;

    public static string T(string key) =>
        Current == AppLanguage.English && LocEnglish.Texts.TryGetValue(key, out var en)
            ? en
            : LocGerman.Texts.TryGetValue(key, out var de) ? de : key;

    /// <summary>Übersetzung mit Platzhaltern, z. B. <c>Loc.T("Export.Done", pfad)</c>.</summary>
    public static string T(string key, params object?[] args) => string.Format(T(key), args);

    public static void Apply(AppLanguage language)
    {
        if (language == Current) return;
        Current = language;
        Source.RaiseAllChanged();
        LanguageChanged?.Invoke();
    }

    /// <summary>
    /// Kultur für Datums- und Zahlenformate. Gehört zur Sprachwahl: ein englisches Menü mit
    /// deutschen Datumsangaben sähe halbfertig aus.
    /// </summary>
    public static CultureInfo Culture =>
        Current == AppLanguage.English ? CultureInfo.GetCultureInfo("en-GB") : CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Sprachcode für die Einstellungen ("de"/"en") und zurück.</summary>
    public static string Code => Current == AppLanguage.English ? "en" : "de";

    public static AppLanguage FromCode(string? code) =>
        code == "en" ? AppLanguage.English : AppLanguage.German;
}

/// <summary>
/// Indexer-Objekt für die XAML-Bindung. Ein <c>PropertyChanged</c> mit leerem Namen bedeutet
/// „alle Eigenschaften neu lesen" – so aktualisiert ein Sprachwechsel die ganze Oberfläche.
/// </summary>
public sealed class LocSource : INotifyPropertyChanged
{
    public string this[string key] => Loc.T(key);

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void RaiseAllChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
