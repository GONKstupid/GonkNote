namespace GonkNote.Core.Theming;

/// <summary>
/// Jedes Symbol, das die Oberfläche zeigt — <b>wofür</b> es steht, nicht wie es aussieht.
///
/// <para>
/// <b>Warum das überhaupt in Core steht.</b> Bis zum 2026-08-12 beantworteten die beiden Köpfe
/// diese Frage getrennt: Der WPF-Kopf setzte an 91 Stellen Zeichen aus <c>Segoe Fluent
/// Icons</c> — einer <b>Windows-Systemschrift</b>, die es unter Linux nicht gibt und die sich
/// nicht mitliefern lässt —, der Avalonia-Kopf zeichnete 31 eigene Vektorformen. Das ist
/// dieselbe Lage wie bei den Farben (§4.9) und den Schriften (§4.26), und sie hatte dieselben
/// zwei Folgen: <c>Icon.Lasso</c> und <c>Icon.Hand</c> standen in beiden Köpfen mit
/// <b>verschiedenen</b> Formen (§4.13), und ein Segoe-Zeichencode saß in
/// <c>GonkNote.ViewModels</c> — einer Assembly, die WPF-frei sein soll (§4.2).
/// </para>
///
/// <para>
/// <b>Die Reihenfolge ist beliebig, die Namen sind es nicht.</b> Anders als bei
/// <see cref="ThemeColor"/> wird hier nichts über <c>(int)</c> abgelegt; ein Symbol steht in
/// keiner gespeicherten Datei. Umbenennen ist deshalb unbedenklich — Weglassen nicht: Der
/// Wächter <c>IkonentabelleTests</c> verlangt zu jedem Wert eine Form.
/// </para>
///
/// <para>
/// <b>Ein Symbol je Bedeutung, nicht je Knopf.</b> „Schließen" ist einmal da und nicht dreimal
/// (Fenster, Reiter, Dialog); dasselbe gilt für die Chevrons und für Vergrößern/Verkleinern,
/// die im WPF-Kopf früher zwei verschiedene Glyphen hatten, je nachdem ob man in der
/// Zeichenfläche oder im Texteditor stand. Wer zwei Formen für dieselbe Sache will, braucht
/// dafür einen Grund, den man aufschreiben kann.
/// </para>
/// </summary>
public enum AppIcon
{
    // ---- Fenster und Schale ------------------------------------------------------------

    /// <summary>Die Seitenleiste ein- und ausklappen.</summary>
    Menu,

    /// <summary>Seitenleiste.</summary>
    Sidebar,

    /// <summary>Fenster minimieren.</summary>
    WindowMinimize,

    /// <summary>Fenster maximieren.</summary>
    WindowMaximize,

    /// <summary>Fenster wiederherstellen.</summary>
    WindowRestore,

    /// <summary>Schließen — Fenster, Reiter, Dialog.</summary>
    Close,

    /// <summary>Aufklappen.</summary>
    ChevronDown,

    /// <summary>Zuklappen.</summary>
    ChevronUp,

    /// <summary>Zurück, vorige Seite.</summary>
    ChevronLeft,

    /// <summary>Weiter, nächste Seite, Untermenü.</summary>
    ChevronRight,

    // ---- Dokumentarten -----------------------------------------------------------------

    /// <summary>Ordner.</summary>
    Folder,

    /// <summary>Neuer Ordner.</summary>
    FolderNew,

    /// <summary>Notizbuch. <b>Eigene Form</b> — Nutzer-Entscheidung 2026-08-12.</summary>
    Notebook,

    /// <summary>Whiteboard. <b>Eigene Form.</b></summary>
    Whiteboard,

    /// <summary>Textdokument. <b>Eigene Form</b> — Nutzer-Entscheidung 2026-08-12.</summary>
    TextDocument,

    // ---- Baum und Galerie --------------------------------------------------------------

    /// <summary>Anheften.</summary>
    Pin,

    /// <summary>Favorit. Eine der wenigen Formen, die auch gefüllt etwas zeigt.</summary>
    Star,

    /// <summary>Farbe wählen.</summary>
    Palette,

    /// <summary>Löschen.</summary>
    Trash,

    /// <summary>Hell und dunkel wechseln.</summary>
    Theme,

    /// <summary>Hinweis.</summary>
    Info,

    /// <summary>Warnung.</summary>
    Warning,

    /// <summary>Suchen.</summary>
    Search,

    /// <summary>Einstellungen.</summary>
    Settings,

    // ---- Werkzeuge der Zeichenfläche ---------------------------------------------------

    /// <summary>Stift.</summary>
    Pen,

    /// <summary>Bleistift.</summary>
    Pencil,

    /// <summary>Textmarker.</summary>
    Highlighter,

    /// <summary>Radierer.</summary>
    Eraser,

    /// <summary>Lasso-Auswahl.</summary>
    Lasso,

    /// <summary>Auswählen und verschieben.</summary>
    Move,

    /// <summary>Blatt schieben.</summary>
    Hand,

    /// <summary>Textfeld.</summary>
    TextTool,

    /// <summary>Formen-Stift.</summary>
    ShapePen,

    // ---- Die fünf Formen ----------------------------------------------------------------
    //
    // Neu in Phase 4.5. Der WPF-Kopf zeigte hier bis dahin **Unicode-Zeichen** (▭ ╱ → ◯ △)
    // mit `FontFamily="Segoe UI"` — genau das, was §4.31 abgeschafft hat: unter Linux gibt es
    // diese Schrift nicht, jedes davon wäre ein leeres Kästchen gewesen. Eigene Formen, denn
    // ein Werkzeugsymbol für „Rechteck" ist ein Rechteck; da hat kein Satz etwas Besseres.

    /// <summary>Form: Gerade. <b>Eigene Form.</b></summary>
    ShapeLine,

    /// <summary>Form: Pfeil. <b>Eigene Form.</b></summary>
    ShapeArrow,

    /// <summary>Form: Rechteck. <b>Eigene Form.</b></summary>
    ShapeRect,

    /// <summary>Form: Ellipse. <b>Eigene Form.</b></summary>
    ShapeEllipse,

    /// <summary>Form: Dreieck. <b>Eigene Form.</b></summary>
    ShapeTriangle,

    /// <summary>Notizzettel.</summary>
    StickyNote,

    /// <summary>Sticker.</summary>
    Sticker,

    /// <summary>Diagramm.</summary>
    Chart,

    /// <summary>Lineal.</summary>
    Ruler,

    /// <summary>Geodreieck. <b>Eigene Form</b> — dafür gibt es in keinem Satz etwas.</summary>
    SetSquare,

    // ---- Bearbeiten --------------------------------------------------------------------

    /// <summary>Rückgängig.</summary>
    Undo,

    /// <summary>Wiederholen.</summary>
    Redo,

    /// <summary>Ausschneiden.</summary>
    Cut,

    /// <summary>Kopieren.</summary>
    Copy,

    /// <summary>Einfügen.</summary>
    Paste,

    /// <summary>Duplizieren.</summary>
    Duplicate,

    /// <summary>Alles auswählen.</summary>
    SelectAll,

    /// <summary>Rückschritt — die Taste des Zahlenblocks.</summary>
    Backspace,

    /// <summary>Texterkennung.</summary>
    Ocr,

    // ---- Ansicht -----------------------------------------------------------------------

    /// <summary>Vergrößern.</summary>
    ZoomIn,

    /// <summary>Verkleinern.</summary>
    ZoomOut,

    /// <summary>Seitenbreite. <b>Eigene Form.</b></summary>
    FitWidth,

    /// <summary>Ganze Seite. <b>Eigene Form.</b></summary>
    FitPage,

    /// <summary>Hinzufügen — Seite, Eintrag.</summary>
    Plus,

    /// <summary>Exportieren.</summary>
    Export,

    /// <summary>Bild oder Datei einfügen.</summary>
    Image,

    /// <summary>Verweis.</summary>
    Link,

    /// <summary>Aufzählung, Inhaltsverzeichnis.</summary>
    List,

    /// <summary>Gliederung.</summary>
    Outline,

    /// <summary>Tabelle.</summary>
    Table,

    // ---- Textformat --------------------------------------------------------------------

    /// <summary>Seitenränder.</summary>
    Margins,

    /// <summary>Zeilenabstand.</summary>
    LineSpacing,

    /// <summary>Seitenhintergrund, Wasserzeichen.</summary>
    Background,

    /// <summary>Beschriftung.</summary>
    Caption,

    /// <summary>Schrift größer.</summary>
    FontGrow,

    /// <summary>Schrift kleiner.</summary>
    FontShrink,

    /// <summary>Format übertragen.</summary>
    FormatPainter,

    /// <summary>Linksbündig.</summary>
    AlignLeft,

    /// <summary>Zentriert.</summary>
    AlignCenter,

    /// <summary>Rechtsbündig.</summary>
    AlignRight,

    /// <summary>Blocksatz.</summary>
    AlignJustify,

    /// <summary>Einzug vergrößern.</summary>
    IndentIncrease,

    /// <summary>Einzug verkleinern.</summary>
    IndentDecrease,
}
