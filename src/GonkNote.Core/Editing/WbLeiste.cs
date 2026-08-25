using GonkNote.Core.Models;

namespace GonkNote.Core.Editing;

/// <summary>
/// Die Ordnung der Werkzeugleiste: welche Knöpfe in welcher Reihenfolge stehen, welche zu
/// einer <b>klappbaren Gruppe</b> gehören, und welcher davon eingeklappt sichtbar bleibt.
///
/// <para>
/// <b>Warum das hier steht und nicht im Kopf.</b> Bis Phase 4.5 lag die Klappregel viermal
/// nebeneinander in <c>WhiteboardView.xaml.cs</c> des WPF-Kopfs — einmal je Gruppe, jedes Mal
/// dieselben drei Zeilen mit anderen Feldern. Der Linux-Kopf hat sie gar nicht: dort stehen
/// <b>alle</b> Werkzeuge immer nebeneinander, und mit den Stücken 1 bis 4 sind es so viele
/// geworden, dass die Leiste rollt. <b>Zwei Köpfe, zwei Leisten, dieselbe App</b> — die
/// Ordnung gehört deshalb an eine Stelle, an der beide sie ablesen können.
/// </para>
///
/// <para>
/// <b>Die Reihenfolge ist eine Vorgabe und keine Beschreibung.</b> Sie sagt, wie die Leiste
/// aussehen <i>soll</i>; ob ein Kopf sie einhält, prüft ein Wächter gegen sein XAML und nicht
/// diese Datei. Das ist Absicht: eine Liste, die sich am Kopf ausrichtet, den sie ordnen
/// soll, ordnet nichts.
/// </para>
/// </summary>
public static class WbLeiste
{
    /// <summary>Die klappbaren Gruppen der Werkzeugleiste.</summary>
    public enum Gruppe
    {
        /// <summary>Kein Gruppenwerkzeug — steht immer für sich.</summary>
        Keine,
        /// <summary>Stift, Glättstift, Bleistift, Textmarker.</summary>
        Stifte,
        /// <summary>Lasso und Verschieben.</summary>
        Auswahl,
        /// <summary>Die fünf Formen.</summary>
        Formen,
        /// <summary>Lineal und Geodreieck.</summary>
        Zeichenhilfen,
    }

    /// <summary>
    /// Die Werkzeuge der Leiste in der Reihenfolge, in der sie stehen — <b>Trennstriche
    /// eingeschlossen</b>, denn wo eine Gruppe endet, ist Teil der Ordnung.
    /// </summary>
    public static readonly IReadOnlyList<ToolType?> Reihenfolge = new ToolType?[]
    {
        ToolType.Pan,
        null,                       // Trennstrich
        ToolType.Pen, ToolType.SmoothPen, ToolType.Pencil, ToolType.Highlighter,
        ToolType.Eraser,
        null,
        ToolType.Lasso, ToolType.Move,
        null,
        ToolType.Text,
        ToolType.Shape,
        ToolType.Sticky, ToolType.Sticker,
    };

    /// <summary>Die Stifte, in Leistenreihenfolge.</summary>
    public static readonly IReadOnlyList<ToolType> Stifte = new[]
    {
        ToolType.Pen, ToolType.SmoothPen, ToolType.Pencil, ToolType.Highlighter,
    };

    /// <summary>Die Auswahl-Werkzeuge, in Leistenreihenfolge.</summary>
    public static readonly IReadOnlyList<ToolType> Auswahlwerkzeuge = new[]
    {
        ToolType.Lasso, ToolType.Move,
    };

    /// <summary>Die fünf Formen, in Leistenreihenfolge — Rechteck zuerst, es ist die Vorgabe.</summary>
    public static readonly IReadOnlyList<ShapeKind> Formen = new[]
    {
        ShapeKind.Rectangle, ShapeKind.Line, ShapeKind.Arrow,
        ShapeKind.Ellipse, ShapeKind.Triangle,
    };

    /// <summary>Lineal und Geodreieck, in Leistenreihenfolge.</summary>
    public static readonly IReadOnlyList<Zeichenhilfe> Hilfen = new[]
    {
        Zeichenhilfe.Lineal, Zeichenhilfe.Geodreieck,
    };

    /// <summary>Zu welcher klappbaren Gruppe gehört ein Werkzeug?</summary>
    public static Gruppe GruppeVon(ToolType werkzeug) => werkzeug switch
    {
        ToolType.Pen or ToolType.SmoothPen or ToolType.Pencil or ToolType.Highlighter
            => Gruppe.Stifte,
        ToolType.Lasso or ToolType.Move => Gruppe.Auswahl,
        ToolType.Shape => Gruppe.Formen,
        _ => Gruppe.Keine,
    };

    /// <summary>Ist ein Stift aktiv?</summary>
    public static bool IstStift(ToolType werkzeug) => GruppeVon(werkzeug) == Gruppe.Stifte;

    /// <summary>Ist ein Auswahl-Werkzeug aktiv?</summary>
    public static bool IstAuswahl(ToolType werkzeug) => GruppeVon(werkzeug) == Gruppe.Auswahl;

    /// <summary>
    /// Ist eine Gruppe aufgeklappt? <b>Genau dann, wenn das aktive Werkzeug zu ihr gehört</b> —
    /// ein Klick genügt also in beide Richtungen: das Werkzeug wählen klappt seine Gruppe auf,
    /// ein anderes wählen klappt sie wieder ein.
    /// </summary>
    public static bool IstAufgeklappt(Gruppe gruppe, ToolType aktivesWerkzeug) =>
        gruppe != Gruppe.Keine && GruppeVon(aktivesWerkzeug) == gruppe;

    /// <summary>
    /// Ist ein Knopf der Gruppe sichtbar? Aufgeklappt sind alle zu sehen; eingeklappt bleibt
    /// nur der <paramref name="vertreter"/> stehen — <b>der zuletzt benutzte</b> und nicht
    /// der erste. Wer mit dem Textmarker arbeitet, will ihn beim nächsten Mal wiederfinden,
    /// ohne die Gruppe erst aufklappen zu müssen.
    /// </summary>
    public static bool IstSichtbar<T>(T knopf, T vertreter, bool aufgeklappt)
        where T : notnull
        => aufgeklappt || knopf.Equals(vertreter);

    /// <summary>
    /// Die Tastenkürzel der Werkzeugleiste — <b>dieselben Buchstaben in beiden Köpfen</b>.
    /// <para>
    /// <c>D</c> und <c>R</c> fehlen hier mit Absicht: sie gehören Geodreieck und Lineal, und
    /// die schalten kein Werkzeug um, sondern legen etwas auf die Fläche. <c>F</c> steht für
    /// die Formen-Gruppe und schaltet auf die zuletzt benutzte Form, nicht auf eine feste.
    /// </para>
    /// </summary>
    public static readonly IReadOnlyDictionary<char, ToolType> Kuerzel =
        new Dictionary<char, ToolType>
        {
            ['S'] = ToolType.Pen,
            ['G'] = ToolType.SmoothPen,
            ['B'] = ToolType.Pencil,
            ['M'] = ToolType.Highlighter,
            ['E'] = ToolType.Eraser,
            ['L'] = ToolType.Lasso,
            ['V'] = ToolType.Move,
            ['T'] = ToolType.Text,
            ['F'] = ToolType.Shape,
            ['N'] = ToolType.Sticky,
            ['H'] = ToolType.Pan,
        };
}
