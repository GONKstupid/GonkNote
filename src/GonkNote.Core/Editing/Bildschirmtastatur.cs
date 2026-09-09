using GonkNote.Services;

namespace GonkNote.Core.Editing;

/// <summary>
/// Die <b>eingebaute</b> Bildschirmtastatur — Belegung und Regeln, nicht ihr Aussehen.
///
/// <para>
/// <b>Warum die App eine eigene mitbringt</b> (Nutzer-Entscheidung 2026-09-09). Der
/// Linux-Kopf ist XWayland-Client, und <c>Avalonia.X11</c> hat keine <c>IInputPane</c>
/// (§4.104): <b>kein Kopfcode kann eine fremde Tastatur aufklappen lassen.</b> Tippen kann
/// eine fremde durchaus — das ist gemessen —, aber <b>von selbst aufgehen</b> tut sie nur,
/// wenn die App den Befehl <i>ihrer</i> Tastatur kennt, und den gibt es nicht einheitlich:
/// <c>wvkbd</c> hört auf ein Signal, <c>squeekboard</c> auf D-Bus, jede weitere auf etwas
/// Drittes. Eine eingebaute Tastatur beantwortet die Frage ein für alle Mal, ist auf allen
/// drei Zielplattformen dieselbe und funktioniert in jeder Sandbox.
/// </para>
///
/// <para>
/// <b>Warum die Belegung hier steht und nicht im Kopf.</b> Dieselbe Begründung wie bei
/// <see cref="WbZahlenblock"/> und <see cref="WbLeiste"/>: Welche Taste welches Zeichen
/// erzeugt, ist eine Aussage über die App und nicht über ein XAML. Der iPadOS-Kopf wird
/// dieselbe Tabelle lesen, und zwei Tabellen liefen auseinander, ohne dass es jemand merkt —
/// eine Tastatur, die auf zwei Plattformen verschiedene Umlaute schreibt, fällt erst dem
/// Nutzer auf.
/// </para>
///
/// <para>
/// <b>Zwei Belegungen, und die Sprache entscheidet</b> (Nutzer): Deutsch als QWERTZ,
/// Englisch als QWERTY. Sie folgen <see cref="Loc.Current"/> und nicht der Tastatur des
/// Rechners — wer die Oberfläche auf Englisch stellt, will auch hier ein englisches Bild;
/// und ein Gerät ohne Hardware-Tastatur hat gar keine Rechner-Belegung, an der man sich
/// ausrichten könnte.
/// </para>
/// </summary>
public static class Bildschirmtastatur
{
    /// <summary>Was eine Taste tut.</summary>
    public enum Art
    {
        /// <summary>Schreibt ein Zeichen.</summary>
        Zeichen,
        /// <summary>Löscht das Zeichen links der Marke.</summary>
        Rueck,
        /// <summary>Neue Zeile.</summary>
        Eingabe,
        /// <summary>Tabulator.</summary>
        Tab,
        /// <summary>Umschalten — gilt für <b>eine</b> Taste, siehe <see cref="NaechsterStand"/>.</summary>
        Umschalt,
        /// <summary>Die dritte Ebene (€, @, |, …).</summary>
        AltGr,
        /// <summary>Leerzeichen.</summary>
        Leer,
        /// <summary>Marke nach links.</summary>
        Links,
        /// <summary>Marke nach rechts.</summary>
        Rechts,
        /// <summary>Blendet die Tastatur aus.</summary>
        Zu,
    }

    /// <summary>
    /// Eine Taste. <paramref name="Normal"/>, <paramref name="Umschaltet"/> und
    /// <paramref name="AltGr"/> sind die drei Ebenen; bei allem außer
    /// <see cref="Art.Zeichen"/> trägt <paramref name="Normal"/> nur die Beschriftung.
    /// <paramref name="Breite"/> zählt in <b>Grundbreiten</b> und nicht in Punkten — wie
    /// breit eine Grundbreite ist, entscheidet der Kopf am verfügbaren Platz.
    /// </summary>
    public readonly record struct Taste(
        Art Art,
        string Normal,
        string Umschaltet = "",
        string? AltGr = null,
        double Breite = 1.0);

    /// <summary>Der Zustand der beiden Umschalter.</summary>
    public readonly record struct Stand(bool Umschalt, bool AltGr)
    {
        public static readonly Stand Grund = new(false, false);
    }

    private static Taste Z(string normal, string gross, string? altgr = null) =>
        new(Art.Zeichen, normal, gross, altgr);

    // ==================== Deutsch (QWERTZ) ====================

    private static readonly Taste[][] DeutschReihen =
    [
        [
            Z("1", "!"), Z("2", "\""), Z("3", "§"), Z("4", "$"), Z("5", "%"),
            Z("6", "&"), Z("7", "/", "{"), Z("8", "(", "["), Z("9", ")", "]"),
            Z("0", "=", "}"), Z("ß", "?", "\\"),
            new(Art.Rueck, "⌫", Breite: 1.6),
        ],
        [
            Z("q", "Q", "@"), Z("w", "W"), Z("e", "E", "€"), Z("r", "R"), Z("t", "T"),
            Z("z", "Z"), Z("u", "U"), Z("i", "I"), Z("o", "O"), Z("p", "P"),
            Z("ü", "Ü"), Z("+", "*", "~"),
        ],
        [
            Z("a", "A"), Z("s", "S"), Z("d", "D"), Z("f", "F"), Z("g", "G"),
            Z("h", "H"), Z("j", "J"), Z("k", "K"), Z("l", "L"),
            Z("ö", "Ö"), Z("ä", "Ä"), Z("#", "'"),
        ],
        [
            new(Art.Umschalt, "⇧", Breite: 1.6),
            Z("y", "Y"), Z("x", "X"), Z("c", "C"), Z("v", "V"), Z("b", "B"),
            Z("n", "N"), Z("m", "M"), Z(",", ";"), Z(".", ":"), Z("-", "_"),
            Z("<", ">", "|"),
        ],
        [
            new(Art.AltGr, "AltGr", Breite: 1.6),
            new(Art.Tab, "⇥", Breite: 1.2),
            new(Art.Leer, "", Breite: 5.0),
            new(Art.Links, "◀"), new(Art.Rechts, "▶"),
            new(Art.Eingabe, "⏎", Breite: 1.8),
            new(Art.Zu, "⌄", Breite: 1.2),
        ],
    ];

    // ==================== Englisch (QWERTY) ====================

    private static readonly Taste[][] EnglischReihen =
    [
        [
            Z("1", "!"), Z("2", "@"), Z("3", "#"), Z("4", "$"), Z("5", "%"),
            Z("6", "^"), Z("7", "&", "{"), Z("8", "*", "["), Z("9", "(", "]"),
            Z("0", ")", "}"), Z("-", "_", "\\"),
            new(Art.Rueck, "⌫", Breite: 1.6),
        ],
        [
            Z("q", "Q"), Z("w", "W"), Z("e", "E", "€"), Z("r", "R"), Z("t", "T"),
            Z("y", "Y"), Z("u", "U"), Z("i", "I"), Z("o", "O"), Z("p", "P"),
            Z("[", "{"), Z("]", "}", "~"),
        ],
        [
            Z("a", "A"), Z("s", "S"), Z("d", "D"), Z("f", "F"), Z("g", "G"),
            Z("h", "H"), Z("j", "J"), Z("k", "K"), Z("l", "L"),
            Z(";", ":"), Z("'", "\""), Z("=", "+"),
        ],
        [
            new(Art.Umschalt, "⇧", Breite: 1.6),
            Z("z", "Z"), Z("x", "X"), Z("c", "C"), Z("v", "V"), Z("b", "B"),
            Z("n", "N"), Z("m", "M"), Z(",", "<"), Z(".", ">"), Z("/", "?"),
            Z("`", "~", "|"),
        ],
        [
            new(Art.AltGr, "AltGr", Breite: 1.6),
            new(Art.Tab, "⇥", Breite: 1.2),
            new(Art.Leer, "", Breite: 5.0),
            new(Art.Links, "◀"), new(Art.Rechts, "▶"),
            new(Art.Eingabe, "⏎", Breite: 1.8),
            new(Art.Zu, "⌄", Breite: 1.2),
        ],
    ];

    /// <summary>
    /// Die Reihen der gewählten Sprache. <b>Jede Sprache, die keine eigene Belegung hat,
    /// bekommt die englische</b> — ein QWERTY ist für jeden lesbar, eine fehlende Belegung
    /// wäre eine leere Fläche.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<Taste>> Reihen(AppLanguage sprache) =>
        sprache == AppLanguage.German ? DeutschReihen : EnglischReihen;

    /// <summary>
    /// Was auf der Taste steht — abhängig vom Stand der Umschalter.
    /// <para>
    /// <b>AltGr gewinnt über Umschalt</b>, und nur dort, wo es eine dritte Ebene gibt. Sonst
    /// bliebe die Taste in der AltGr-Ebene leer, und eine leere Taste sieht kaputt aus statt
    /// unbelegt.
    /// </para>
    /// </summary>
    public static string Beschriftung(Taste taste, Stand stand)
    {
        if (taste.Art != Art.Zeichen) return taste.Normal;
        if (stand.AltGr && taste.AltGr is { } dritte) return dritte;
        return stand.Umschalt && taste.Umschaltet.Length > 0 ? taste.Umschaltet : taste.Normal;
    }

    /// <summary>
    /// Das Zeichen, das die Taste schreibt — oder <c>null</c>, wenn sie keines schreibt
    /// (Rücktaste, Pfeile, …). <b>Dieselbe Rechnung wie <see cref="Beschriftung"/></b>, und
    /// das ist der Punkt: Was draufsteht, kommt heraus. Zwei getrennte Rechnungen wären zwei
    /// Wahrheiten, und die Abweichung fiele erst beim Tippen auf.
    /// </summary>
    public static string? Zeichen(Taste taste, Stand stand) => taste.Art switch
    {
        Art.Zeichen => Beschriftung(taste, stand),
        Art.Leer => " ",
        _ => null,
    };

    /// <summary>
    /// Wie der Stand nach einem Tastendruck aussieht.
    ///
    /// <para>
    /// <b>Beide Umschalter gelten für genau eine Taste</b>, so wie auf jeder
    /// Bildschirmtastatur: Wer <c>⇧</c> und dann <c>a</c> drückt, bekommt <c>A</c> und
    /// danach wieder Kleinbuchstaben. Ein Dauerumschalter wäre auf einer Tastatur ohne
    /// fühlbare Tasten eine Falle — man sieht dem Gerät nicht an, dass es noch gehalten ist,
    /// und schreibt zehn Zeichen groß, bevor es auffällt.
    /// </para>
    /// <para>
    /// <b>Ein zweiter Druck auf denselben Umschalter hebt ihn auf</b> — sonst käme man aus
    /// einem versehentlich gedrückten Umschalter nur heraus, indem man ein Zeichen falsch
    /// schreibt.
    /// </para>
    /// </summary>
    public static Stand NaechsterStand(Taste taste, Stand stand) => taste.Art switch
    {
        Art.Umschalt => new Stand(!stand.Umschalt, false),
        Art.AltGr => new Stand(false, !stand.AltGr),
        // Rücktaste, Pfeile und Tabulator sollen den Umschalter NICHT verbrauchen: wer ⇧
        // drückt und sich vertippt, will nach der Korrektur immer noch groß schreiben.
        Art.Rueck or Art.Links or Art.Rechts or Art.Tab or Art.Zu => stand,
        _ => Stand.Grund,
    };

    /// <summary>
    /// Die breiteste Reihe in Grundbreiten — der Kopf teilt seine Fläche danach ein, damit
    /// alle Reihen dieselbe Gesamtbreite bekommen und die Tastatur nicht ausfranst.
    /// </summary>
    public static double Grundbreiten(IReadOnlyList<IReadOnlyList<Taste>> reihen) =>
        reihen.Max(r => r.Sum(t => t.Breite));
}
