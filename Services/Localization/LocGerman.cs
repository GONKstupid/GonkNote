namespace GonkNote.Services;

/// <summary>
/// Die deutschen Texte – die Vorlage. Was hier steht, ist die Wahrheit; die englische
/// Tabelle übersetzt dieselben Schlüssel. Fehlt dort einer, erscheint der deutsche Text.
/// <para>Schlüssel sind nach Bereichen gruppiert: <c>Menu.*</c>, <c>Tree.*</c>, …</para>
/// </summary>
internal static class LocGerman
{
    public static readonly Dictionary<string, string> Texts = new()
    {
        // ---- Menüleiste (Unterstrich = Zugriffstaste) ----
        ["Menu.File"] = "_Datei",
        ["Menu.File.NewFolder"] = "Neuer _Ordner",
        ["Menu.File.NewNotebook"] = "Neues _Notizbuch",
        ["Menu.File.NewWhiteboard"] = "Neues _Whiteboard",
        ["Menu.File.NewText"] = "Neues _Textdokument",
        ["Menu.File.Import"] = "Dokument _importieren… (DOCX / Markdown)",
        ["Menu.File.Export"] = "_Exportieren… (PDF / DOCX / Markdown)",
        ["Menu.File.Save"] = "_Speichern",
        ["Menu.File.SaveAll"] = "_Alle speichern",
        ["Menu.File.Quit"] = "_Beenden",
        ["Menu.View"] = "_Ansicht",
        ["Menu.View.Sidebar"] = "_Seitenleiste",
        ["Menu.View.Theme"] = "_Dark/Light Mode umschalten",
        ["Menu.View.Language"] = "S_prache",
        ["Menu.View.Language.German"] = "Deutsch",
        ["Menu.View.Language.English"] = "Englisch",
        ["Menu.Help"] = "_Hilfe",
        ["Menu.Help.About"] = "Über _Gonk Note",

        // ---- Seitenleiste ----
        ["Sidebar.Toggle"] = "Seitenleiste ein-/ausblenden (Strg+B)",
        ["Sidebar.Pinned"] = "ANGEPINNT",
        ["Sidebar.SwitchTheme"] = "Design wechseln",
        ["Sidebar.ThemeTooltip"] = "Dark/Light Mode (Strg+T)",

        // ---- Neue Dokumente (Schaltflächen, Kontextmenü, Galerie) ----
        ["New.Folder"] = "Neuer Ordner",
        ["New.Notebook"] = "Neues Notizbuch",
        ["New.Whiteboard"] = "Neues Whiteboard",
        ["New.Text"] = "Neues Textdokument",

        // ---- Ordnerbaum ----
        ["Tree.Open"] = "Öffnen",
        ["Tree.Rename"] = "Umbenennen",
        ["Tree.Delete"] = "Löschen",
        ["Tree.Favorite"] = "Favorit",
        ["Tree.IconColor"] = "Symbolfarbe",
        ["Tree.IconColor.Auto"] = "Automatisch (Ordnerfarbe)",
        ["Tree.IconColor.AutoTooltip"] = "Übernimmt die Farbe des übergeordneten Ordners",
        ["Tree.IconColor.Custom"] = "Eigene Farbe…",

        // ---- Farben ----
        ["Color.Blue"] = "Blau",
        ["Color.Teal"] = "Türkis",
        ["Color.Pink"] = "Pink",
        ["Color.Purple"] = "Lila",
        ["Color.Red"] = "Rot",
        ["Color.Orange"] = "Orange",
        ["Color.Yellow"] = "Gelb",
        ["Color.Green"] = "Grün",
        ["Color.Gray"] = "Grau",

        // ---- Galerie (Startansicht ohne geöffnetes Dokument) ----
        ["Gallery.Back"] = "Zurück",
        ["Gallery.New"] = "Neu",
        ["Gallery.Empty.Title"] = "Noch nichts hier",
        ["Gallery.Empty.Hint"] = "Erstelle etwas über „Neu“ oder die Seitenleiste.",
        ["Gallery.Options"] = "Optionen",

        // ---- Fenster / Tabs ----
        ["Window.CloseTab"] = "Schließen (Strg+W)",
        ["Window.Minimize"] = "Minimieren",
        ["Window.Restore"] = "Wiederherstellen",
        ["Window.Close"] = "Schließen",

        // ---- Tastenkürzel (nur die Anzeige im Menü) ----
        ["Shortcut.Save"] = "Strg+S",
        ["Shortcut.SaveAll"] = "Strg+Umschalt+S",
        ["Shortcut.Quit"] = "Alt+F4",
        ["Shortcut.Sidebar"] = "Strg+B",
        ["Shortcut.Theme"] = "Strg+T",
        ["Shortcut.Rename"] = "F2",
        ["Shortcut.Delete"] = "Entf",

        // ---- Texte, die der Code setzt ----
        ["Gallery.Root"] = "Dokumente",
        ["Item.CopySuffix"] = " (Kopie)",
        ["Import.Filter"] = "Dokumente (*.docx;*.md)|*.docx;*.md|Word-Dokumente (*.docx)|*.docx",
        ["Gallery.DateFormat"] = "d. MMM yyyy, HH:mm",
    };
}
