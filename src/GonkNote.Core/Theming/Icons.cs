using GonkNote.Core.Models;

namespace GonkNote.Core.Theming;

/// <summary>
/// Eine Symbolform: ihre Pfadangabe und der Kasten, in dem sie gezeichnet ist.
///
/// <para>
/// <b>Warum der Kasten mitkommt, statt alles auf ein Maß zu bringen.</b> Die Formen stammen
/// aus zwei Quellen — Lucide zeichnet in 24 × 24, die selbst gezeichneten Formen dieser App
/// in 16 × 16. Sie umzurechnen hieße, siebzig Pfadangaben von Hand mit 1,5 zu multiplizieren;
/// das ist genau die Sorte Fleißarbeit, bei der ein Zahlendreher unbemerkt bleibt und als
/// leicht verrutschtes Symbol herauskommt. Der Kopf skaliert stattdessen beim Zeichnen
/// (<see cref="AppIcons.Scale"/>) — <b>eine Multiplikation an einer Stelle statt siebzig im
/// Quelltext.</b>
/// </para>
/// <para>
/// <b>Die Formen sind zum Nachziehen gedacht, nicht zum Füllen</b> (<c>Stroke</c>, runde Enden
/// und Ecken, Stärke aus <see cref="AppIcons.StrokeFor"/>). Wenige geschlossene Formen — Stern
/// und Pin — vertragen zusätzlich eine Füllung; die übrigen bestehen aus offenen Strichen und
/// wären gefüllt unsichtbar.
/// </para>
/// </summary>
/// <param name="Path">Die Pfadangabe, wie WPF und Avalonia sie beide lesen.</param>
/// <param name="Box">Die Kantenlänge des Kastens, in dem die Form gezeichnet ist.</param>
public readonly record struct IconShape(string Path, double Box);

/// <summary>
/// Die Symboltabelle der App — <b>eine Datentabelle, kein Programm</b>, dieselbe Bauart wie
/// <see cref="Themes"/> und <see cref="BundledFonts"/>.
///
/// <para>
/// <b>Herkunft und Lizenz.</b> Die meisten Formen kommen aus <b>Lucide</b> (ISC-Lizenz,
/// <c>https://lucide.dev</c>) und stehen hier unverändert — nur aus den SVG-Elementen in
/// reine Pfadangaben umgerechnet, weil beide Köpfe nur Geometrie-Zeichenketten lesen. Sieben
/// Formen sind eigene: Notizbuch, Textdokument, Whiteboard, Geodreieck, Seitenbreite, Ganze
/// Seite und Fenster-Wiederherstellen — für sie gibt es in keinem Satz etwas Passendes, oder
/// die eigene Form war die bessere (Nutzer-Entscheidung 2026-08-12). Der Lizenztext liegt in
/// <c>THIRD-PARTY-NOTICES.md</c>, beide README-Fassungen nennen ihn.
/// </para>
/// <para>
/// <b>Segoe Fluent Icons kommt hier nicht vor, und das ist der Punkt.</b> Die Schrift gehört
/// Microsoft, darf nicht mitgeliefert werden und fehlt unter Linux und iPadOS. Ein Symbol, das
/// nur auf einem Rechner erscheint, ist kein Symbol — es ist ein leeres Kästchen mit
/// Vorgeschichte.
/// </para>
/// <para>
/// <b>Die Pfadangaben sind erzeugt und nicht abgetippt.</b> Drei Fehler sind beim Erzeugen
/// aufgefallen, und alle drei wären still geblieben: ein führendes kleines <c>m</c> ist beim
/// Aneinanderhängen zweier Pfade nicht mehr dasselbe wie ein großes (der zweite Strich eines
/// „x" landete daneben), nach einem großen <c>M</c> sind Folgezahlen <i>absolute</i> LineTos
/// (aus <c>m15 14 5-5-5-5</c> wurde etwas ganz anderes), und ein deutsches Dezimalkomma ist in
/// einer Pfadangabe ein <i>Trennzeichen</i>. Gefunden hat sie erst ein Kontaktblatt mit allen
/// Symbolen — dieselbe Lehre wie in §4.28: eine Anzeige findet, was ein Textvergleich nicht
/// sehen kann.
/// </para>
/// </summary>
public static class AppIcons
{
    /// <summary>
    /// Die Form zu einem Symbol. <b>Wirft</b>, wenn eine fehlt — ein stiller Rückfall auf ein
    /// Ersatzsymbol wäre genau der Fehler, den diese Tabelle abschaffen soll: Er sähe im
    /// laufenden Programm wie eine Gestaltungsentscheidung aus.
    /// </summary>
    public static IconShape Shape(AppIcon icon) =>
        Tabelle.TryGetValue(icon, out var form)
            ? form
            : throw new ArgumentOutOfRangeException(
                nameof(icon), icon, "Für dieses Symbol steht keine Form in der Tabelle.");

    /// <summary>Wie viele Symbole die Tabelle kennt — für den Wächter.</summary>
    public static int Count => Tabelle.Count;

    /// <summary>
    /// Das Symbol zu einer Dokumentart — Ordnerbaum, Galerie, Reiter.
    ///
    /// <para>
    /// <b>Diese vier Zeilen standen bis zum 2026-08-12 zweimal da</b> und sagten Verschiedenes:
    /// im WPF-Kopf als <c>TreeItemViewModel.IconGlyph</c> (Segoe-Zeichencodes, in einer
    /// Assembly, die WPF-frei sein soll), im Avalonia-Kopf als <c>KindToIconConverter</c>
    /// (Namen von Vektorformen). Genau die Doppelung aus §4.13 — hier aufgelöst.
    /// </para>
    /// </summary>
    public static AppIcon ForKind(ItemKind kind) => kind switch
    {
        ItemKind.Folder => AppIcon.Folder,
        ItemKind.Notebook => AppIcon.Notebook,
        ItemKind.Whiteboard => AppIcon.Whiteboard,
        ItemKind.TextDocument => AppIcon.TextDocument,
        _ => AppIcon.Folder,
    };

    /// <summary>
    /// Der Faktor, mit dem die Form auf die gewünschte Kantenlänge kommt.
    /// <para>Der Kopf multipliziert damit; die Tabelle rechnet nicht.</para>
    /// </summary>
    public static double Scale(AppIcon icon, double size) => size / Shape(icon).Box;

    /// <summary>
    /// Die Strichstärke <b>in den Koordinaten der Form</b> — nicht in Bildpunkten. Sie wird von
    /// derselben Skalierung erfasst wie die Form selbst und bleibt dadurch bei jeder Größe im
    /// gleichen Verhältnis.
    /// <para>
    /// Das Zwölftel ist Lucides eigenes Maß (Stärke 2 im Kasten 24). Die sieben eigenen Formen
    /// sind darauf abgestimmt — sonst stünden im selben Fenster zwei Strichstärken
    /// nebeneinander, und das sieht nach Versehen aus, weil es eines wäre.
    /// </para>
    /// </summary>
    public static double StrokeFor(AppIcon icon) => Shape(icon).Box / 12.0;

    // ERZEUGT — nicht von Hand ändern. Die Formen stammen aus Lucide 1.31.0 bzw. aus den
    // eigenen Vektoren der beiden Köpfe; das Werkzeug dazu ist ein Wegwerf-Skript, sein
    // Vorgehen steht in §4.31. Wer eine Form ändern will, ändert sie hier — aber prüft sie
    // danach am Kontaktblatt und nicht am Quelltext.
    private static readonly Dictionary<AppIcon, IconShape> Tabelle = new()
    {
// ---- Schale ----------------------------------------------------------------
        // Die Seitenleiste ein- und ausklappen  (lucide/menu)
        [AppIcon.Menu] = new("M4 5h16 M4 12h16 M4 19h16", 24),
        // Seitenleiste  (lucide/panel-left)
        [AppIcon.Sidebar] = new("M9 3v18 M5,3 H19 A2,2 0 0 1 21,5 V19 A2,2 0 0 1 19,21 H5 A2,2 0 0 1 3,19 V5 A2,2 0 0 1 5,3 Z", 24),
        // Fenster minimieren  (lucide/minus)
        [AppIcon.WindowMinimize] = new("M5 12h14", 24),
        // Fenster maximieren  (lucide/square)
        [AppIcon.WindowMaximize] = new("M5,3 H19 A2,2 0 0 1 21,5 V19 A2,2 0 0 1 19,21 H5 A2,2 0 0 1 3,19 V5 A2,2 0 0 1 5,3 Z", 24),
        // Fenster wiederherstellen  (eigen)
        [AppIcon.WindowRestore] = new("M5.5,5.5 H12.5 V12.5 H5.5 Z M3.5,10.5 V3.5 H10.5", 16),
        // Schliessen -- Fenster, Reiter, Dialog  (lucide/x)
        [AppIcon.Close] = new("M18 6 6 18 M0,0 m6 6 12 12", 24),
        // Aufklappen  (lucide/chevron-down)
        [AppIcon.ChevronDown] = new("M0,0 m6 9 6 6 6-6", 24),
        // Zuklappen  (lucide/chevron-up)
        [AppIcon.ChevronUp] = new("M0,0 m18 15-6-6-6 6", 24),
        // Zurueck, vorige Seite  (lucide/chevron-left)
        [AppIcon.ChevronLeft] = new("M0,0 m15 18-6-6 6-6", 24),
        // Weiter, naechste Seite, Untermenue  (lucide/chevron-right)
        [AppIcon.ChevronRight] = new("M0,0 m9 18 6-6-6-6", 24),

        // ---- Arten -----------------------------------------------------------------
        // Ordner  (lucide/folder)
        [AppIcon.Folder] = new("M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z", 24),
        // Neuer Ordner  (lucide/folder-plus)
        [AppIcon.FolderNew] = new("M12 10v6 M9 13h6 M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z", 24),
        // Notizbuch -- eigene Form, Nutzerwunsch  (eigen)
        [AppIcon.Notebook] = new("M3.5,2.5 H13 A1,1 0 0 1 14,3.5 V12.5 A1,1 0 0 1 13,13.5 H3.5 Z M6,2.5 V13.5 M8,5.5 H12 M8,8 H12 M8,10.5 H11", 16),
        // Whiteboard -- eigene Form  (eigen)
        [AppIcon.Whiteboard] = new("M1.5,2.5 H14.5 V11 H1.5 Z M4,7.5 Q6,5 8,7 Q10,9 12,6 M5,11 L3.5,14.5 M11,11 L12.5,14.5", 16),
        // Textdokument -- eigene Form, Nutzerwunsch  (eigen)
        [AppIcon.TextDocument] = new("M3.5,1.5 H10 L12.5,4 V14.5 H3.5 Z M10,1.5 V4 H12.5 M5.5,7 H10.5 M5.5,9.5 H10.5 M5.5,12 H8.5", 16),

        // ---- Baum ------------------------------------------------------------------
        // Anheften  (lucide/pin)
        [AppIcon.Pin] = new("M12 17v5 M9 10.76a2 2 0 0 1-1.11 1.79l-1.78.9A2 2 0 0 0 5 15.24V16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-.76a2 2 0 0 0-1.11-1.79l-1.78-.9A2 2 0 0 1 15 10.76V7a1 1 0 0 1 1-1 2 2 0 0 0 0-4H8a2 2 0 0 0 0 4 1 1 0 0 1 1 1z", 24),
        // Favorit  (lucide/star)
        [AppIcon.Star] = new("M11.525 2.295a.53.53 0 0 1 .95 0l2.31 4.679a2.123 2.123 0 0 0 1.595 1.16l5.166.756a.53.53 0 0 1 .294.904l-3.736 3.638a2.123 2.123 0 0 0-.611 1.878l.882 5.14a.53.53 0 0 1-.771.56l-4.618-2.428a2.122 2.122 0 0 0-1.973 0L6.396 21.01a.53.53 0 0 1-.77-.56l.881-5.139a2.122 2.122 0 0 0-.611-1.879L2.16 9.795a.53.53 0 0 1 .294-.906l5.165-.755a2.122 2.122 0 0 0 1.597-1.16z", 24),
        // Farbe waehlen  (lucide/palette)
        [AppIcon.Palette] = new("M12 22a1 1 0 0 1 0-20 10 9 0 0 1 10 9 5 5 0 0 1-5 5h-2.25a1.75 1.75 0 0 0-1.4 2.8l.3.4a1.75 1.75 0 0 1-1.4 2.8z M13,6.5 a0.5,0.5 0 1,0 1,0 a0.5,0.5 0 1,0 -1,0 M17,10.5 a0.5,0.5 0 1,0 1,0 a0.5,0.5 0 1,0 -1,0 M6,12.5 a0.5,0.5 0 1,0 1,0 a0.5,0.5 0 1,0 -1,0 M8,7.5 a0.5,0.5 0 1,0 1,0 a0.5,0.5 0 1,0 -1,0", 24),
        // Loeschen  (lucide/trash-2)
        [AppIcon.Trash] = new("M10 11v6 M14 11v6 M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6 M3 6h18 M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2", 24),
        // Hell und dunkel wechseln  (lucide/moon)
        [AppIcon.Theme] = new("M20.985 12.486a9 9 0 1 1-9.473-9.472c.405-.022.617.46.402.803a6 6 0 0 0 8.268 8.268c.344-.215.825-.004.803.401", 24),
        // Hinweis  (lucide/info)
        [AppIcon.Info] = new("M12 16v-4 M12 8h.01 M2,12 a10,10 0 1,0 20,0 a10,10 0 1,0 -20,0", 24),
        // Warnung  (lucide/triangle-alert)
        [AppIcon.Warning] = new("M0,0 m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3 M12 9v4 M12 17h.01", 24),
        // Suchen  (lucide/search)
        [AppIcon.Search] = new("M0,0 m21 21-4.34-4.34 M3,11 a8,8 0 1,0 16,0 a8,8 0 1,0 -16,0", 24),
        // Einstellungen  (lucide/settings)
        [AppIcon.Settings] = new("M9.671 4.136a2.34 2.34 0 0 1 4.659 0 2.34 2.34 0 0 0 3.319 1.915 2.34 2.34 0 0 1 2.33 4.033 2.34 2.34 0 0 0 0 3.831 2.34 2.34 0 0 1-2.33 4.033 2.34 2.34 0 0 0-3.319 1.915 2.34 2.34 0 0 1-4.659 0 2.34 2.34 0 0 0-3.32-1.915 2.34 2.34 0 0 1-2.33-4.033 2.34 2.34 0 0 0 0-3.831A2.34 2.34 0 0 1 6.35 6.051a2.34 2.34 0 0 0 3.319-1.915 M9,12 a3,3 0 1,0 6,0 a3,3 0 1,0 -6,0", 24),

        // ---- Werkzeug --------------------------------------------------------------
        // Stift  (lucide/pen)
        [AppIcon.Pen] = new("M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z", 24),
        // Bleistift  (lucide/pencil)
        [AppIcon.Pencil] = new("M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z M0,0 m15 5 4 4", 24),
        // Textmarker  (lucide/highlighter)
        [AppIcon.Highlighter] = new("M0,0 m9 11-6 6v3h9l3-3 M0,0 m22 12-4.6 4.6a2 2 0 0 1-2.8 0l-5.2-5.2a2 2 0 0 1 0-2.8L14 4", 24),
        // Radierer  (lucide/eraser)
        [AppIcon.Eraser] = new("M21 21H8a2 2 0 0 1-1.42-.587l-3.994-3.999a2 2 0 0 1 0-2.828l10-10a2 2 0 0 1 2.829 0l5.999 6a2 2 0 0 1 0 2.828L12.834 21 M0,0 m5.082 11.09 8.828 8.828", 24),
        // Lasso-Auswahl  (lucide/lasso)
        [AppIcon.Lasso] = new("M3.704 14.467a10 8 0 1 1 3.115 2.375 M7 22a5 5 0 0 1-2-3.994 M3,16 a2,2 0 1,0 4,0 a2,2 0 1,0 -4,0", 24),
        // Auswaehlen und verschieben  (lucide/mouse-pointer-2)
        [AppIcon.Move] = new("M4.037 4.688a.495.495 0 0 1 .651-.651l16 6.5a.5.5 0 0 1-.063.947l-6.124 1.58a2 2 0 0 0-1.438 1.435l-1.579 6.126a.5.5 0 0 1-.947.063z", 24),
        // Blatt schieben  (lucide/hand)
        [AppIcon.Hand] = new("M18 11V6a2 2 0 0 0-2-2a2 2 0 0 0-2 2 M14 10V4a2 2 0 0 0-2-2a2 2 0 0 0-2 2v2 M10 10.5V6a2 2 0 0 0-2-2a2 2 0 0 0-2 2v8 M18 8a2 2 0 1 1 4 0v6a8 8 0 0 1-8 8h-2c-2.8 0-4.5-.86-5.99-2.34l-3.6-3.6a2 2 0 0 1 2.83-2.82L7 15", 24),
        // Textfeld  (lucide/type)
        [AppIcon.TextTool] = new("M12 4v16 M4 7V5a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v2 M9 20h6", 24),
        // Formen-Stift  (lucide/pen-tool)
        [AppIcon.ShapePen] = new("M15.707 21.293a1 1 0 0 1-1.414 0l-1.586-1.586a1 1 0 0 1 0-1.414l5.586-5.586a1 1 0 0 1 1.414 0l1.586 1.586a1 1 0 0 1 0 1.414z M0,0 m18 13-1.375-6.874a1 1 0 0 0-.746-.776L3.235 2.028a1 1 0 0 0-1.207 1.207L5.35 15.879a1 1 0 0 0 .776.746L13 18 M0,0 m2.3 2.3 7.286 7.286 M9,11 a2,2 0 1,0 4,0 a2,2 0 1,0 -4,0", 24),
        // Die fünf Formen -- eigene Formen (Phase 4.5). Sie zeigen genau das, was sie
        // anlegen; ein Symbolsatz hat dafür nichts Besseres. Alle im 24er-Raster, mit
        // demselben Rand wie die Lucide-Formen daneben, damit sie in der Leiste nicht
        // größer oder kleiner wirken als ihre Nachbarn.
        [AppIcon.ShapeLine] = new("M4,20 L20,4", 24),
        [AppIcon.ShapeArrow] = new("M4,20 L20,4 M13,4 H20 V11", 24),
        [AppIcon.ShapeRect] = new("M4,6 H20 V18 H4 Z", 24),
        [AppIcon.ShapeEllipse] = new("M2,12 a10,7 0 1,0 20,0 a10,7 0 1,0 -20,0", 24),
        [AppIcon.ShapeTriangle] = new("M12,4 L21,19 H3 Z", 24),
        // Notizzettel  (lucide/sticky-note)
        [AppIcon.StickyNote] = new("M21 9a2.4 2.4 0 0 0-.706-1.706l-3.588-3.588A2.4 2.4 0 0 0 15 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2z M15 3v5a1 1 0 0 0 1 1h5", 24),
        // Sticker  (lucide/sticker)
        [AppIcon.Sticker] = new("M21 9a2.4 2.4 0 0 0-.706-1.706l-3.588-3.588A2.4 2.4 0 0 0 15 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2z M15 3v5a1 1 0 0 0 1 1h5 M8 13h.01 M16 13h.01 M10 16s.8 1 2 1c1.3 0 2-1 2-1", 24),
        // Diagramm  (lucide/chart-column)
        [AppIcon.Chart] = new("M3 3v16a2 2 0 0 0 2 2h16 M18 17V9 M13 17V5 M8 17v-3", 24),
        // Lineal  (lucide/ruler)
        [AppIcon.Ruler] = new("M21.3 15.3a2.4 2.4 0 0 1 0 3.4l-2.6 2.6a2.4 2.4 0 0 1-3.4 0L2.7 8.7a2.41 2.41 0 0 1 0-3.4l2.6-2.6a2.41 2.41 0 0 1 3.4 0Z M0,0 m14.5 12.5 2-2 M0,0 m11.5 9.5 2-2 M0,0 m8.5 6.5 2-2 M0,0 m17.5 15.5 2-2", 24),
        // Geodreieck -- eigene Form, in keinem Satz enthalten  (eigen)
        [AppIcon.SetSquare] = new("M2,12 L14,12 L8,4 Z M5,12 A3,3 0 0 0 11,12 M8,12 V10.5", 16),

        // ---- Bearbeiten ------------------------------------------------------------
        // Rueckgaengig  (lucide/undo-2)
        [AppIcon.Undo] = new("M9 14 4 9l5-5 M4 9h10.5a5.5 5.5 0 0 1 5.5 5.5a5.5 5.5 0 0 1-5.5 5.5H11", 24),
        // Wiederholen  (lucide/redo-2)
        [AppIcon.Redo] = new("M0,0 m15 14 5-5-5-5 M20 9H9.5A5.5 5.5 0 0 0 4 14.5A5.5 5.5 0 0 0 9.5 20H13", 24),
        // Ausschneiden  (lucide/scissors)
        [AppIcon.Cut] = new("M8.12 8.12 12 12 M20 4 8.12 15.88 M14.8 14.8 20 20 M3,6 a3,3 0 1,0 6,0 a3,3 0 1,0 -6,0 M3,18 a3,3 0 1,0 6,0 a3,3 0 1,0 -6,0", 24),
        // Kopieren  (lucide/copy)
        [AppIcon.Copy] = new("M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2 M10,8 H20 A2,2 0 0 1 22,10 V20 A2,2 0 0 1 20,22 H10 A2,2 0 0 1 8,20 V10 A2,2 0 0 1 10,8 Z", 24),
        // Einfuegen  (lucide/clipboard-paste)
        [AppIcon.Paste] = new("M11 14h10 M16 4h2a2 2 0 0 1 2 2v1.344 M0,0 m17 18 4-4-4-4 M8 4H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 1.793-1.113 M9,2 H15 A1,1 0 0 1 16,3 V5 A1,1 0 0 1 15,6 H9 A1,1 0 0 1 8,5 V3 A1,1 0 0 1 9,2 Z", 24),
        // Duplizieren  (lucide/copy-plus)
        [AppIcon.Duplicate] = new("M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2 M10,8 H20 A2,2 0 0 1 22,10 V20 A2,2 0 0 1 20,22 H10 A2,2 0 0 1 8,20 V10 A2,2 0 0 1 10,8 Z M15,12 L15,18 M12,15 L18,15", 24),
        // Alles auswaehlen  (lucide/box-select)
        [AppIcon.SelectAll] = new("M5 3a2 2 0 0 0-2 2 M19 3a2 2 0 0 1 2 2 M21 19a2 2 0 0 1-2 2 M5 21a2 2 0 0 1-2-2 M9 3h1 M9 21h1 M14 3h1 M14 21h1 M3 9v1 M21 9v1 M3 14v1 M21 14v1", 24),
        // Rueckschritt -- Zahlenblock  (lucide/delete)
        [AppIcon.Backspace] = new("M10 5a2 2 0 0 0-1.344.519l-6.328 5.74a1 1 0 0 0 0 1.481l6.328 5.741A2 2 0 0 0 10 19h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2z M0,0 m12 9 6 6 M0,0 m18 9-6 6", 24),
        // Texterkennung  (lucide/scan-text)
        [AppIcon.Ocr] = new("M3 7V5a2 2 0 0 1 2-2h2 M17 3h2a2 2 0 0 1 2 2v2 M21 17v2a2 2 0 0 1-2 2h-2 M7 21H5a2 2 0 0 1-2-2v-2 M7 8h8 M7 12h10 M7 16h6", 24),

        // ---- Ansicht ---------------------------------------------------------------
        // Vergroessern  (lucide/zoom-in)
        [AppIcon.ZoomIn] = new("M3,11 a8,8 0 1,0 16,0 a8,8 0 1,0 -16,0 M21,21 L16.65,16.65 M11,8 L11,14 M8,11 L14,11", 24),
        // Verkleinern  (lucide/zoom-out)
        [AppIcon.ZoomOut] = new("M3,11 a8,8 0 1,0 16,0 a8,8 0 1,0 -16,0 M21,21 L16.65,16.65 M8,11 L14,11", 24),
        // Seitenbreite -- eigene Form  (eigen)
        [AppIcon.FitWidth] = new("M4.5,2.5 H11.5 V13.5 H4.5 Z M1.5,8 H4 M12,8 H14.5 M1.5,8 L3,6.5 M1.5,8 L3,9.5 M14.5,8 L13,6.5 M14.5,8 L13,9.5", 16),
        // Ganze Seite -- eigene Form  (eigen)
        [AppIcon.FitPage] = new("M4.5,3.5 H11.5 V12.5 H4.5 Z M8,0.8 V3 M8,13 V15.2 M8,0.8 L6.6,2.2 M8,0.8 L9.4,2.2 M8,15.2 L6.6,13.8 M8,15.2 L9.4,13.8", 16),
        // Hinzufuegen -- Seite, Eintrag  (lucide/plus)
        [AppIcon.Plus] = new("M5 12h14 M12 5v14", 24),
        // Exportieren  (lucide/upload)
        [AppIcon.Export] = new("M12 3v12 M0,0 m17 8-5-5-5 5 M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4", 24),
        // Bild oder Datei einfuegen  (lucide/image)
        [AppIcon.Image] = new("M0,0 m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21 M7,9 a2,2 0 1,0 4,0 a2,2 0 1,0 -4,0 M5,3 H19 A2,2 0 0 1 21,5 V19 A2,2 0 0 1 19,21 H5 A2,2 0 0 1 3,19 V5 A2,2 0 0 1 5,3 Z", 24),
        // Verweis  (lucide/link)
        [AppIcon.Link] = new("M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71 M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71", 24),
        // Aufzaehlung, Inhaltsverzeichnis  (lucide/list)
        [AppIcon.List] = new("M3 5h.01 M3 12h.01 M3 19h.01 M8 5h13 M8 12h13 M8 19h13", 24),
        // Gliederung  (lucide/list-tree)
        [AppIcon.Outline] = new("M8 5h13 M13 12h8 M13 19h8 M3 10a2 2 0 0 0 2 2h3 M3 5v12a2 2 0 0 0 2 2h3", 24),
        // Tabelle  (lucide/table)
        [AppIcon.Table] = new("M12 3v18 M3 9h18 M3 15h18 M5,3 H19 A2,2 0 0 1 21,5 V19 A2,2 0 0 1 19,21 H5 A2,2 0 0 1 3,19 V5 A2,2 0 0 1 5,3 Z", 24),

        // ---- Format ----------------------------------------------------------------
        // Seitenraender  (lucide/scan)
        [AppIcon.Margins] = new("M3 7V5a2 2 0 0 1 2-2h2 M17 3h2a2 2 0 0 1 2 2v2 M21 17v2a2 2 0 0 1-2 2h-2 M7 21H5a2 2 0 0 1-2-2v-2", 24),
        // Zeilenabstand  (lucide/align-vertical-space-around)
        [AppIcon.LineSpacing] = new("M22 20H2 M22 4H2 M9,9 H15 A2,2 0 0 1 17,11 V13 A2,2 0 0 1 15,15 H9 A2,2 0 0 1 7,13 V11 A2,2 0 0 1 9,9 Z", 24),
        // Seitenhintergrund, Wasserzeichen  (lucide/paint-bucket)
        [AppIcon.Background] = new("M11 7 6 2 M18.992 12H2.041 M21.145 18.38A3.34 3.34 0 0 1 20 16.5a3.3 3.3 0 0 1-1.145 1.88c-.575.46-.855 1.02-.855 1.595A2 2 0 0 0 20 22a2 2 0 0 0 2-2.025c0-.58-.285-1.13-.855-1.595 M0,0 m8.5 4.5 2.148-2.148a1.205 1.205 0 0 1 1.704 0l7.296 7.296a1.205 1.205 0 0 1 0 1.704l-7.592 7.592a3.615 3.615 0 0 1-5.112 0l-3.888-3.888a3.615 3.615 0 0 1 0-5.112L5.67 7.33", 24),
        // Beschriftung  (lucide/captions)
        [AppIcon.Caption] = new("M7 15h4M15 15h2M7 11h2M13 11h4 M5,5 H19 A2,2 0 0 1 21,7 V17 A2,2 0 0 1 19,19 H5 A2,2 0 0 1 3,17 V7 A2,2 0 0 1 5,5 Z", 24),
        // Schrift groesser  (lucide/a-arrow-up)
        [AppIcon.FontGrow] = new("M0,0 m14 11 4-4 4 4 M18 16V7 M0,0 m2 16 4.039-9.69a.5.5 0 0 1 .923 0L11 16 M3.304 13h6.392", 24),
        // Schrift kleiner  (lucide/a-arrow-down)
        [AppIcon.FontShrink] = new("M0,0 m14 12 4 4 4-4 M18 16V7 M0,0 m2 16 4.039-9.69a.5.5 0 0 1 .923 0L11 16 M3.304 13h6.392", 24),
        // Format uebertragen  (lucide/paintbrush)
        [AppIcon.FormatPainter] = new("M0,0 m14.622 17.897-10.68-2.913 M18.376 2.622a1 1 0 1 1 3.002 3.002L17.36 9.643a.5.5 0 0 0 0 .707l.944.944a2.41 2.41 0 0 1 0 3.408l-.944.944a.5.5 0 0 1-.707 0L8.354 7.348a.5.5 0 0 1 0-.707l.944-.944a2.41 2.41 0 0 1 3.408 0l.944.944a.5.5 0 0 0 .707 0z M9 8c-1.804 2.71-3.97 3.46-6.583 3.948a.507.507 0 0 0-.302.819l7.32 8.883a1 1 0 0 0 1.185.204C12.735 20.405 16 16.792 16 15", 24),
        // Linksbuendig  (lucide/align-left)
        [AppIcon.AlignLeft] = new("M21 5H3 M15 12H3 M17 19H3", 24),
        // Zentriert  (lucide/align-center)
        [AppIcon.AlignCenter] = new("M21 5H3 M17 12H7 M19 19H5", 24),
        // Rechtsbuendig  (lucide/align-right)
        [AppIcon.AlignRight] = new("M21 5H3 M21 12H9 M21 19H7", 24),
        // Blocksatz  (lucide/align-justify)
        [AppIcon.AlignJustify] = new("M3 5h18 M3 12h18 M3 19h18", 24),
        // Einzug vergroessern  (lucide/indent-increase)
        [AppIcon.IndentIncrease] = new("M21 5H11 M21 12H11 M21 19H11 M0,0 m3 8 4 4-4 4", 24),
        // Einzug verkleinern  (lucide/indent-decrease)
        [AppIcon.IndentDecrease] = new("M21 5H11 M21 12H11 M21 19H11 M0,0 m7 8-4 4 4 4", 24),
    };
}
