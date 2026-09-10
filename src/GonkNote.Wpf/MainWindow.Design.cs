using System.IO;
using System.Windows;
using System.Windows.Controls;
using GonkNote.Core.Platform;
using GonkNote.Core.Theming;
using GonkNote.Services;

namespace GonkNote;

/// <summary>
/// Das Design-Untermenü — Hell, Dunkel und die eigenen Designs aus dem Datenordner
/// (Nutzerwunsch vom 2026-08-02, HANDOFF §6 „eigene Farbschemata").
///
/// <para>
/// <b>Wortgleich zum Linux-Kopf</b> (<c>MainWindow.Design.cs</c> in
/// <c>src/GonkNote.Avalonia</c>) — bis auf das, was WPF anders schreibt: <c>Items</c> statt
/// <c>ItemsSource</c>, <c>IsCheckable</c> statt <c>ToggleType</c>. <b>Die Entscheidungen
/// stehen nicht hier</b>, sondern in Core: was eine gültige Datei ist, weiß
/// <see cref="ThemeFile"/>, wo sie liegt <see cref="ThemeLibrary"/>. Dieser Kopf kennt nur
/// Menüpunkte — genau deshalb konnte er dasselbe Werkzeug ohne zweite Fassung bekommen.
/// </para>
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// Baut das Untermenü neu auf. Läuft beim Start, nach jedem Designwechsel (auch dem über
    /// Strg+T) und nach jedem Sprachwechsel — die Beschriftungen stehen sonst weiter in der
    /// alten Sprache da (HANDOFF §7, „Texte, die der Code setzt").
    /// </summary>
    private void FillDesignMenu()
    {
        var aktiv = App.Platform.Theme.Definition;
        DesignMenu.Items.Clear();

        DesignMenu.Items.Add(DesignEntry(Loc.T("Menu.View.Design.Light"), Themes.Light,
            aktiv.File == null && aktiv.Variant == AppTheme.Light));
        DesignMenu.Items.Add(DesignEntry(Loc.T("Menu.View.Design.Dark"), Themes.Dark,
            aktiv.File == null && aktiv.Variant == AppTheme.Dark));

        var eigene = ThemeLibrary.All();
        if (eigene.Count > 0)
        {
            DesignMenu.Items.Add(new Separator());
            foreach (var eintrag in eigene)
                DesignMenu.Items.Add(DesignFile(eintrag, aktiv));
        }

        DesignMenu.Items.Add(new Separator());
        DesignMenu.Items.Add(MenuPoint(Loc.T("Menu.View.Design.Load"), DesignLoad_Click));
        DesignMenu.Items.Add(MenuPoint(Loc.T("Menu.View.Design.Template"), DesignTemplate_Click));
        DesignMenu.Items.Add(MenuPoint(Loc.T("Menu.View.Design.Folder"), DesignFolder_Click));
    }

    /// <summary>Ein Eintrag für eine der beiden mitgelieferten Tabellen.</summary>
    private MenuItem DesignEntry(string text, ThemeDefinition theme, bool gewaehlt)
    {
        var punkt = new MenuItem { Header = text, IsCheckable = true, IsChecked = gewaehlt };
        punkt.Click += (_, _) => SetDesign(theme);
        return punkt;
    }

    /// <summary>
    /// Ein Eintrag für eine Datei. <b>Eine kaputte Datei steht mit da</b> — ausgegraut und
    /// mit dem Grund im Tooltip. Wer sie im Menü nicht fände, suchte den Fehler bei sich
    /// (siehe <see cref="ThemeEntry"/>).
    /// </summary>
    private MenuItem DesignFile(ThemeEntry eintrag, ThemeDefinition aktiv)
    {
        var punkt = new MenuItem
        {
            Header = eintrag.Name,
            IsCheckable = true,
            IsChecked = eintrag.IsUsable && aktiv.File == eintrag.File,
            IsEnabled = eintrag.IsUsable,
        };

        if (eintrag.Error is { } fehler)
            punkt.ToolTip = Loc.T("Theme.Broken", fehler);

        if (eintrag.Theme is { } theme)
            punkt.Click += (_, _) => SetDesign(theme);

        return punkt;
    }

    private static MenuItem MenuPoint(string text, RoutedEventHandler klick)
    {
        var punkt = new MenuItem { Header = text };
        punkt.Click += klick;
        return punkt;
    }

    /// <summary>
    /// Anlegen — mehr nicht. Das Merken übernimmt <c>App</c> am Ereignis
    /// <see cref="IThemeHost.ThemeChanged"/>, und der Haken im Menü zieht über dasselbe
    /// Ereignis nach. Hier steht deshalb kein zweiter Schreibweg in die Einstellungen und
    /// kein zweiter Aufruf von <see cref="FillDesignMenu"/>: sonst gäbe es zwei Stellen, die
    /// den Startzustand bestimmen, und Strg+T (das hier nie vorbeikommt) ließe den Haken
    /// stehen, wo er nicht mehr hingehört.
    /// </summary>
    private void SetDesign(ThemeDefinition theme) => App.Platform.Theme.Apply(theme);

    /// <summary>
    /// „Eigenes laden…": wählen, <b>prüfen</b>, in den Design-Ordner kopieren, anlegen.
    /// <para>
    /// Die Reihenfolge ist die Aussage. Eine Datei, die sich nicht lesen lässt, landet gar
    /// nicht erst im Ordner — sonst stünde sie beim nächsten Start als ausgegrauter Eintrag
    /// da, den niemand bestellt hat.
    /// </para>
    /// </summary>
    private void DesignLoad_Click(object sender, RoutedEventArgs e)
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
            SetDesign(geprueft.Theme.WithFile(datei));
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
    private void DesignTemplate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string pfad = ThemeLibrary.WriteTemplate(App.Platform.Theme.Definition);
            FillDesignMenu();   // die Vorlage ist ab sofort ein Eintrag
            App.Platform.Dialogs.Inform(Loc.T("Theme.TemplateSaved", pfad));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            App.Platform.Dialogs.Inform(ex.Message, DialogSeverity.Warning);
        }
    }

    private void DesignFolder_Click(object sender, RoutedEventArgs e)
    {
        try { App.Platform.Shell.OpenExternal(ThemeLibrary.UserFolder); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            App.Platform.Dialogs.Inform(ex.Message, DialogSeverity.Warning);
        }
    }
}
