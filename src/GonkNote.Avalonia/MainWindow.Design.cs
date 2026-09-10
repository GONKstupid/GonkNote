using Avalonia.Controls;
using Avalonia.Interactivity;
using GonkNote.Core.Platform;
using GonkNote.Core.Theming;
using GonkNote.Services;

namespace GonkNote;

/// <summary>
/// Das Design-Untermenü — Hell, Dunkel und die eigenen Designs aus dem Datenordner
/// (Nutzerwunsch vom 2026-08-02, HANDOFF §6 „eigene Farbschemata").
///
/// <para>
/// <b>Warum das Menü im Code entsteht und nicht in der XAML:</b> Die Zahl der Einträge steht
/// erst zur Laufzeit fest — sie ist die Zahl der Dateien in
/// <see cref="ThemeLibrary.UserFolder"/> und ändert sich, während die App läuft. Dasselbe
/// Muster wie bei den Sticker- und Cover-Listen.
/// </para>
/// <para>
/// <b>Was hier ausdrücklich nicht steht, ist eine Farbe.</b> Ein Design wird geladen, geprüft
/// und an <see cref="IThemeHost.Apply(ThemeDefinition)"/> gegeben; wie daraus Pinsel werden,
/// weiß <c>AvaloniaThemeHost</c>, und was eine gültige Datei ist, weiß Core. Dieser Kopf
/// kennt nur Menüpunkte.
/// </para>
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// Baut das Untermenü neu auf. Läuft beim Start, nach jedem Designwechsel (auch dem über
    /// Strg+T) und nach jedem Sprachwechsel — die Beschriftungen stehen sonst weiter in der
    /// alten Sprache da (HANDOFF §7, „Texte, die der Code setzt").
    /// </summary>
    private void DesignMenueFuellen()
    {
        var aktiv = App.Platform.Theme.Definition;
        var eintraege = new List<object>
        {
            DesignEintrag(Loc.T("Menu.View.Design.Light"), Themes.Light,
                          aktiv.File == null && aktiv.Variant == AppTheme.Light),
            DesignEintrag(Loc.T("Menu.View.Design.Dark"), Themes.Dark,
                          aktiv.File == null && aktiv.Variant == AppTheme.Dark),
        };

        var eigene = ThemeLibrary.All();
        if (eigene.Count > 0)
        {
            eintraege.Add(new Separator());
            foreach (var eintrag in eigene)
                eintraege.Add(DesignDatei(eintrag, aktiv));
        }

        eintraege.Add(new Separator());
        eintraege.Add(Menuepunkt(Loc.T("Menu.View.Design.Load"), DesignLaden_Click));
        eintraege.Add(Menuepunkt(Loc.T("Menu.View.Design.Template"), DesignVorlage_Click));
        eintraege.Add(Menuepunkt(Loc.T("Menu.View.Design.Folder"), DesignOrdner_Click));

        DesignMenue.ItemsSource = eintraege;
    }

    /// <summary>Ein Eintrag für eine der beiden mitgelieferten Tabellen.</summary>
    private MenuItem DesignEintrag(string text, ThemeDefinition theme, bool gewaehlt)
    {
        var punkt = new MenuItem
        {
            Header = text,
            ToggleType = MenuItemToggleType.Radio,
            GroupName = "Design",
            IsChecked = gewaehlt,
        };
        punkt.Click += (_, _) => DesignSetzen(theme);
        return punkt;
    }

    /// <summary>
    /// Ein Eintrag für eine Datei. <b>Eine kaputte Datei steht mit da</b> — ausgegraut und
    /// mit dem Grund im Tooltip. Wer sie im Menü nicht fände, suchte den Fehler bei sich
    /// (siehe <see cref="ThemeEntry"/>).
    /// </summary>
    private MenuItem DesignDatei(ThemeEntry eintrag, ThemeDefinition aktiv)
    {
        var punkt = new MenuItem
        {
            Header = eintrag.Name,
            ToggleType = MenuItemToggleType.Radio,
            GroupName = "Design",
            IsChecked = eintrag.IsUsable && aktiv.File == eintrag.File,
            IsEnabled = eintrag.IsUsable,
        };

        if (eintrag.Error is { } fehler)
            ToolTip.SetTip(punkt, Loc.T("Theme.Broken", fehler));

        if (eintrag.Theme is { } theme)
            punkt.Click += (_, _) => DesignSetzen(theme);

        return punkt;
    }

    private static MenuItem Menuepunkt(string text, EventHandler<RoutedEventArgs> klick)
    {
        var punkt = new MenuItem { Header = text };
        punkt.Click += klick;
        return punkt;
    }

    /// <summary>
    /// Anlegen — mehr nicht. Das Merken übernimmt <c>App</c> am Ereignis
    /// <see cref="IThemeHost.ThemeChanged"/>, und der Haken im Menü zieht über dasselbe
    /// Ereignis nach. Hier steht deshalb kein zweiter Schreibweg in die Einstellungen und
    /// kein zweiter Aufruf von <see cref="DesignMenueFuellen"/>: sonst gäbe es zwei Stellen,
    /// die den Startzustand bestimmen, und Strg+T (das hier nie vorbeikommt) ließe den Haken
    /// stehen, wo er nicht mehr hingehört.
    /// </summary>
    private void DesignSetzen(ThemeDefinition theme) => App.Platform.Theme.Apply(theme);

    /// <summary>
    /// „Eigenes laden…": wählen, <b>prüfen</b>, in den Design-Ordner kopieren, anlegen.
    /// <para>
    /// Die Reihenfolge ist die Aussage. Eine Datei, die sich nicht lesen lässt, landet gar
    /// nicht erst im Ordner — sonst stünde sie beim nächsten Start als ausgegrauter Eintrag
    /// da, den niemand bestellt hat.
    /// </para>
    /// </summary>
    private void DesignLaden_Click(object? sender, RoutedEventArgs e)
    {
        var gewaehlt = App.Platform.Files.Open(
            Loc.T("Theme.LoadTitle"),
            [new FileFilter(Loc.T("Theme.Filter"), ThemeFile.Extension)]);

        if (gewaehlt.Count == 0) return;
        string quelle = gewaehlt[0];

        try
        {
            var geprueft = ThemeLibrary.Read(quelle);
            if (geprueft.Theme == null)
            {
                App.Platform.Dialogs.Inform(
                    Loc.T("Theme.LoadFailed", Path.GetFileName(quelle), geprueft.Error ?? ""),
                    DialogSeverity.Warning);
                return;
            }

            // Erst nach der Prüfung kopieren — und dann die Kopie anlegen, nicht das eben
            // Gelesene: der Dateiname im Ordner entscheidet, was beim nächsten Start
            // wiederkommt, und er kann sich beim Kopieren geändert haben (Durchnummerieren).
            string datei = ThemeLibrary.Insert(quelle);
            DesignSetzen(geprueft.Theme.WithFile(datei));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            App.Platform.Dialogs.Inform(
                Loc.T("Theme.LoadFailed", Path.GetFileName(quelle), ex.Message),
                DialogSeverity.Warning);
        }
    }

    /// <summary>
    /// „Vorlage speichern…": schreibt das <b>aktive</b> Design als vollständige Datei in den
    /// Design-Ordner und sagt, wo sie liegt. Das ist der Anfang jedes eigenen Designs — die
    /// zwanzig Farbnamen aus einer Anleitung abzutippen macht niemand (Nutzer-Entscheidung
    /// 2026-09-10).
    /// </summary>
    private void DesignVorlage_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string pfad = ThemeLibrary.WriteTemplate(App.Platform.Theme.Definition);
            DesignMenueFuellen();   // die Vorlage ist ab sofort ein Eintrag
            App.Platform.Dialogs.Inform(Loc.T("Theme.TemplateSaved", pfad));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            App.Platform.Dialogs.Inform(ex.Message, DialogSeverity.Warning);
        }
    }

    private void DesignOrdner_Click(object? sender, RoutedEventArgs e)
    {
        try { App.Platform.Shell.OpenExternal(ThemeLibrary.UserFolder); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            App.Platform.Dialogs.Inform(ex.Message, DialogSeverity.Warning);
        }
    }
}
