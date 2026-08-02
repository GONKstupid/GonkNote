namespace GonkNote.Core.Theming;

/// <summary>
/// Die zwanzig Farben, aus denen ein Erscheinungsbild besteht. Mehr sind es nicht — ein
/// Theme ist in dieser App eine <b>Datentabelle</b>, kein Programm: keine Verläufe, keine
/// Struktur, keine Logik (HANDOFF §6 „eigene Farbschemata").
/// <para>
/// Die Reihenfolge ist Teil des Formats: <see cref="ThemeDefinition"/> legt die Werte in
/// einem Feld dieser Länge ab und greift über <c>(int)</c> darauf zu. <b>Neue Farben nur
/// hinten anhängen</b> — wer eine dazwischenschiebt, verschiebt alle folgenden Werte
/// gespeicherter Tabellen.
/// </para>
/// </summary>
public enum ThemeColor
{
    // ---- Oberfläche (15) ----------------------------------------------------------
    // Was im WPF-Kopf als SolidColorBrush in Themes/Light.xaml steht.

    /// <summary>Fensterhintergrund und Arbeitsbereich.</summary>
    WindowBg,

    /// <summary>Seitenleiste und Menüleiste.</summary>
    SidebarBg,

    /// <summary>Karten und Kacheln vor dem Fensterhintergrund.</summary>
    CardBg,

    /// <summary>Werkzeugleisten über einem Dokument.</summary>
    ToolbarBg,

    /// <summary>Trennlinien und Umrandungen.</summary>
    Border,

    /// <summary>Vordergrundfarbe für Fließtext und Beschriftungen.</summary>
    Text,

    /// <summary>Zurückgenommener Text: Datumsangaben, Hinweise, Symbole ohne Betonung.</summary>
    TextMuted,

    /// <summary>Akzentfarbe — Knöpfe, Auswahlrahmen, Hervorhebungen.</summary>
    Accent,

    /// <summary>Weiche Fassung der Akzentfarbe für Flächen.</summary>
    AccentSoft,

    /// <summary>Türkis. Auch der <b>Rückfall für Symbolfarben</b> ohne eigene Farbe.</summary>
    Turquoise,

    /// <summary>Pink.</summary>
    Pink,

    /// <summary>Lila.</summary>
    Purple,

    /// <summary>Fläche unter dem Zeiger.</summary>
    Hover,

    /// <summary>Fläche während des Klicks.</summary>
    Pressed,

    /// <summary>Ausgewählter Eintrag in Baum und Listen.</summary>
    Selection,

    // ---- Das gezeichnete Blatt (5) ------------------------------------------------
    // Was im WPF-Kopf als rohe <Color> in Themes/Light.xaml steht, weil der Renderer
    // SKColor braucht und keinen Pinsel.
    //
    // Diese fünf sind ausdrücklich Teil der Tabelle: ein Theme, das sie mit ändert,
    // ändert das Aussehen von Notizbüchern — auch im Export. Ob ein einzelnes Theme
    // davon Gebrauch macht, ist danach eine reine Datenfrage; dass es *kann*, ist die
    // Entscheidung vom 2026-08-02 (HANDOFF §6). Sie nachträglich einzuziehen hieße,
    // WbRenderer und den Exportweg noch einmal anzufassen.

    /// <summary>Fläche neben dem Blatt (Whiteboard-Leinwand).</summary>
    CanvasBg,

    /// <summary>Das Papier selbst.</summary>
    PageBg,

    /// <summary>Linien einer linierten Seite.</summary>
    PageLine,

    /// <summary>Punkte einer karierten Seite.</summary>
    PageGridDot,

    /// <summary>Voreingestellte Schreibfarbe.</summary>
    DefaultInk,
}
