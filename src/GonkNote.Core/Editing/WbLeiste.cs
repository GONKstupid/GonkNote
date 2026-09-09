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
    /// Setzt das Werkzeug auf einen <b>Tipp</b> etwas ab, statt einen Zug zu zeichnen?
    ///
    /// <para>
    /// <b>Warum diese Frage überhaupt gestellt werden muss.</b> Der Finger zeichnet nie — das
    /// ist die Grundlage der Handballenabweisung (§4.10): was nicht zeichnen kann, kann auch
    /// nicht versehentlich malen. Im Linux-Kopf war daraus „der Finger schiebt und zoomt, und
    /// sonst nichts" geworden, und damit waren <b>Textfeld und Notizzettel mit dem Finger
    /// unerreichbar</b> — vom Nutzer am 2026-09-09 gemeldet, und im Code war es eine einzige
    /// Zeile, die vor dem Werkzeug abbog.
    /// </para>
    /// <para>
    /// <b>Der Unterschied ist nicht „Finger ja/nein", sondern „Zug oder Tipp".</b> Ein Stift
    /// zieht einen Strich, und ein Handballen zieht ihn versehentlich mit — deshalb bleibt der
    /// Finger von allem fern, was aus einer <i>Bewegung</i> entsteht. Ein Textfeld entsteht
    /// aber aus einer <b>Stelle</b>: es gibt keinen Zug, den ein Handballen verderben könnte,
    /// und ohne diesen Weg ist das Werkzeug auf einem Gerät ohne Maus schlicht nicht bedienbar
    /// — auf genau dem Gerät also, für das die App gebaut ist.
    /// </para>
    /// <para>
    /// <b>Die Auswahl-Werkzeuge stehen bewusst nicht hier.</b> Lasso und Verschieben brauchen
    /// den Zug, und ein Tipp mit dem Finger konkurriert dort mit dem Schieben der Fläche.
    /// Sticker ebenso wenig: der wird über seine Kachel in der Leiste eingefügt und nicht auf
    /// die Fläche getippt.
    /// </para>
    /// </summary>
    public static bool IstTippwerkzeug(ToolType werkzeug) =>
        werkzeug is ToolType.Text or ToolType.Sticky;

    /// <summary>Die Klappgruppen der Einstellungsleiste rechts.</summary>
    public enum Einstellungsbereich
    {
        /// <summary>Das Werkzeug bringt keinen eigenen Abschnitt mit.</summary>
        Keiner,
        /// <summary>Muster, Farbton, Format, Ausrichtung.</summary>
        Seite,
        /// <summary>Füllung und Deckkraft der Formen.</summary>
        Formen,
        /// <summary>Hintergrund des Textfelds.</summary>
        Text,
        /// <summary>Zettelfarbe.</summary>
        Zettel,
        /// <summary>Die Sticker-Sammlung.</summary>
        Sticker,
        /// <summary>Farbverlauf, Schrift und Bild des Covers.</summary>
        Cover,
        /// <summary>PDF und PNG.</summary>
        Export,
    }

    /// <summary>
    /// Welchen Abschnitt der Einstellungsleiste klappt ein Werkzeug auf?
    ///
    /// <para>
    /// <b>Nutzerwunsch vom 2026-09-09:</b> Die Leiste geht mit <b>allem eingeklappt</b> auf,
    /// und wer ein Werkzeug wählt, bekommt genau dessen Abschnitt offen — „das Formen-Werkzeug
    /// nutzen heißt: die Leiste geht auf, alle Bereiche sind eingeklappt bis auf die
    /// Formen-Einstellungen".
    /// </para>
    /// <para>
    /// <b>Warum das hier steht und nicht im Kopf.</b> Dieselbe Begründung wie bei
    /// <see cref="GruppeVon"/>: die Zuordnung Werkzeug → Abschnitt ist eine Aussage über die
    /// App und nicht über ein XAML. Der WPF-Kopf beantwortet sie heute in
    /// <c>SetTool</c> mit drei <c>else if</c>-Zweigen, der Linux-Kopf tat es mit vier
    /// Zuweisungen — <b>zwei Listen derselben Sache, und beide unvollständig</b> (drüben fehlt
    /// der Text-Abschnitt, hier klappte gar nichts auf). Ab jetzt lesen beide von hier.
    /// </para>
    /// <para>
    /// <b><see cref="Einstellungsbereich.Seite"/>, <see cref="Einstellungsbereich.Cover"/> und
    /// <see cref="Einstellungsbereich.Export"/> stehen bewusst in keinem Zweig:</b> sie hängen
    /// an der Seite und am Dokument, nicht an dem, was gerade in der Hand liegt. Kein Werkzeug
    /// klappt sie auf; sie warten auf einen Klick.
    /// </para>
    /// </summary>
    public static Einstellungsbereich BereichVon(ToolType werkzeug) => werkzeug switch
    {
        ToolType.Shape => Einstellungsbereich.Formen,
        ToolType.Text => Einstellungsbereich.Text,
        ToolType.Sticky => Einstellungsbereich.Zettel,
        ToolType.Sticker => Einstellungsbereich.Sticker,
        _ => Einstellungsbereich.Keiner,
    };

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
