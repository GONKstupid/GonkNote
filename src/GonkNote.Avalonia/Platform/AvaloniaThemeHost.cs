using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
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
        var variante = theme.Variant == AppTheme.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        app.RequestedThemeVariant = variante;

        AkzentSetzen(app, variante, theme[ThemeColor.Accent]);

        ThemeChanged?.Invoke();
    }

    /// <summary>
    /// Gibt Fluent die Akzentfarbe <b>an der Wurzel</b> statt an jedem Blatt (§4.86).
    ///
    /// <para>
    /// <b>Das ist die Antwort auf eine Ursache, nicht auf ihre Erscheinungen.</b> Fluent malt
    /// alles Ausgewählte, Angehakte und Aufgeschobene in <c>SystemAccentColor</c> — und das ist
    /// ein Violett, das mit der Farbtabelle in Core nichts zu tun hat. Sechs Runden lang ist es
    /// an sechs Stellen einzeln überschrieben worden (Baum §4.72, Klappliste §4.73, Schieber
    /// §4.74, Reiter und Auswahlpunkt §4.77, Ribbon-Schalter und Eingabefeld §4.80). <b>Jede
    /// Fläche, die noch niemand angefasst hatte, brachte ihr eigenes Violett mit</b> — die
    /// Reparatur wuchs also mit der Oberfläche mit, statt sie einzuholen.
    /// </para>
    ///
    /// <para>
    /// <b>Sieben Schlüssel, ein Setzer.</b> Fluent 12.1.1 liest <c>SystemAccentColor</c> und
    /// sechs Abstufungen (<c>…Light1/2/3</c>, <c>…Dark1/2/3</c>); die Abstufungen rechnet
    /// <c>ColorPaletteResources</c> aus <see cref="ColorPaletteResources.Accent"/> selbst aus.
    /// Zu setzen ist deshalb <b>eine</b> Farbe, und sie kommt aus derselben Tabelle wie alles
    /// andere (§5 Nr. 27: nie ein fester Farbwert, immer einer aus Core).
    /// </para>
    ///
    /// <para>
    /// <b>Nur die gerade gültige Variante.</b> <see cref="Apply(ThemeDefinition)"/> läuft bei
    /// jedem Wechsel und setzt <c>RequestedThemeVariant</c> mit — die andere Variante ist in
    /// diesem Augenblick unsichtbar und bekommt ihre Farbe, sobald sie an die Reihe kommt.
    /// </para>
    ///
    /// <para>
    /// <b>Was das nicht wegräumt:</b> die vorhandenen <c>Brush.Accent</c>-Setter in
    /// <c>Themes/Styles.axaml</c> bleiben stehen und gewinnen weiter — sie werden überflüssig,
    /// nicht falsch. Wer sie herausnimmt, prüft jede Fläche einzeln nach; das ist Aufräumarbeit
    /// für Schritt ④ und nicht Teil dieser Runde.
    /// </para>
    /// </summary>
    private static void AkzentSetzen(Application app, ThemeVariant variante, HexColor akzent)
    {
        // Kein Fluent im Stilbaum hieße: eine andere Oberfläche als die, für die das hier
        // gedacht ist. Dann lieber nichts tun als raten.
        if (app.Styles.OfType<FluentTheme>().FirstOrDefault() is not { } fluent) return;

        if (!fluent.Palettes.TryGetValue(variante, out var palette))
        {
            palette = new ColorPaletteResources();
            fluent.Palettes[variante] = palette;
        }

        palette.Accent = akzent.ToAvalonia();
    }

    public void Toggle() =>
        Apply(Current.Variant == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
