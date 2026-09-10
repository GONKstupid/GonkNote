using System.Windows;
using System.Windows.Media;
using GonkNote.Core.Platform;
using GonkNote.Core.Theming;

namespace GonkNote.Platform;

/// <summary>
/// Das Erscheinungsbild des Windows-Kopfs — <b>seit dem 2026-09-10 aus der Farbtabelle in
/// Core gebaut</b>, nicht mehr aus zwei fest verdrahteten XAML-Dateien.
///
/// <para>
/// <b>Was sich geändert hat und was nicht.</b> Bis dahin tauschte diese Klasse
/// <c>Themes/Light.xaml</c> gegen <c>Themes/Dark.xaml</c>; die zwanzig Farben standen damit
/// zweimal im Baum — einmal hier als XAML, einmal in <see cref="Themes"/> für den
/// Linux-Kopf. Ein Wächter im WPF-Testprojekt verglich beide Fassungen Zeile für Zeile,
/// <em>weil zwei Wahrheiten auseinanderlaufen, sobald es niemand nachhält</em> (§4.13).
/// <b>Jetzt gibt es nur noch eine</b>, und der Wächter ist überflüssig geworden — nicht
/// abgeschafft, sondern beantwortet. Die beiden XAML-Dateien sind gelöscht.
/// </para>
/// <para>
/// <b>Die Schlüssel bleiben wörtlich dieselben</b> (<c>Brush.WindowBg</c>,
/// <c>Color.PageBg</c> …), und dieselbe Stelle im <c>MergedDictionaries</c> wird getauscht
/// wie zuvor. Für jede Bindung in der Oberfläche ändert sich damit nichts: sie alle stehen
/// als <c>DynamicResource</c> da, und ein <c>DynamicResource</c> fragt bei jedem Austausch
/// neu. <b>Nachgesehen und nicht angenommen:</b> es gibt im ganzen WPF-Kopf keinen einzigen
/// <c>StaticResource</c> auf einen Theme-Schlüssel.
/// </para>
/// <para>
/// <b>Und der eigentliche Ertrag:</b> ein eigenes Design (HANDOFF §6, Nutzerwunsch
/// 2026-08-02) ist damit auch hier eine <see cref="ThemeDefinition"/> und kein zweites
/// Dateiformat. Eine <c>.xaml</c> zur Laufzeit einzulesen wäre der falsche Vertrag gewesen —
/// XAML kann Typen erzeugen, eine Theme-Datei aus dem Netz wäre ausführbarer Code
/// (<see cref="ThemeFile"/>).
/// </para>
/// </summary>
public sealed class WpfThemeHost : IThemeHost
{
    /// <summary>Das zur Laufzeit gefüllte Wörterbuch; liegt immer an Stelle 0 (siehe App.xaml).</summary>
    private const int ThemenPlatz = 0;

    public ThemeDefinition Definition { get; private set; } = Themes.Light;

    public AppTheme Current => Definition.Variant;

    public event Action? ThemeChanged;

    public void Apply(AppTheme theme) => Apply(Themes.ForVariant(theme));

    /// <summary>
    /// Eine beliebige Farbtabelle anlegen. <see cref="Apply(AppTheme)"/> ist der Sonderfall
    /// „nimm die mitgelieferte hell bzw. dunkel“.
    /// </summary>
    public void Apply(ThemeDefinition theme)
    {
        Definition = theme;

        var farben = new ResourceDictionary();
        foreach (var (name, wert) in theme.Entries)
        {
            var farbe = Color.FromArgb(wert.A, wert.R, wert.G, wert.B);

            // Zu jeder Farbe beides — Pinsel und rohe Farbe. Die XAML-Dateien trennten das
            // (15 Pinsel, 5 Farben), weil dort jeder Eintrag von Hand stand; hier kostet die
            // zweite Form nichts und erspart der Oberfläche die Frage, welche der beiden es
            // gerade gibt. Dieselbe Entscheidung wie im Linux-Kopf.
            var pinsel = new SolidColorBrush(farbe);
            pinsel.Freeze();   // eingefroren: geteilt über alle Fenster und Fäden, nie geändert

            farben[$"Brush.{name}"] = pinsel;
            farben[$"Color.{name}"] = farbe;
        }

        var zusammen = Application.Current.Resources.MergedDictionaries;
        while (zusammen.Count <= ThemenPlatz) zusammen.Add(new ResourceDictionary());
        zusammen[ThemenPlatz] = farben;

        ThemeChanged?.Invoke();
    }

    /// <summary>
    /// Strg+T. <b>Schaltet zwischen den zwei mitgelieferten Tabellen um</b>, auch wenn gerade
    /// ein eigenes Design läuft — die Taste heißt „Dark/Light“ und nicht „vorheriges Design“.
    /// </summary>
    public void Toggle() =>
        Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
