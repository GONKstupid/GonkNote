namespace GonkNote.Services;

/// <summary>
/// Die englischen Texte. Gleiche Schlüssel wie <see cref="LocGerman"/>; was hier fehlt,
/// erscheint auf Deutsch.
/// </summary>
internal static class LocEnglish
{
    public static readonly Dictionary<string, string> Texts = new()
    {
        // ---- Menu bar (underscore = access key) ----
        ["Menu.File"] = "_File",
        ["Menu.File.NewFolder"] = "New _folder",
        ["Menu.File.NewNotebook"] = "New _notebook",
        ["Menu.File.NewWhiteboard"] = "New _whiteboard",
        ["Menu.File.NewText"] = "New _text document",
        ["Menu.File.Import"] = "_Import document… (DOCX / Markdown)",
        ["Menu.File.Export"] = "_Export… (PDF / DOCX / Markdown)",
        ["Menu.File.Save"] = "_Save",
        ["Menu.File.SaveAll"] = "Save _all",
        ["Menu.File.Quit"] = "_Quit",
        ["Menu.View"] = "_View",
        ["Menu.View.Sidebar"] = "_Sidebar",
        ["Menu.View.Theme"] = "Toggle _dark/light mode",
        ["Menu.View.Language"] = "_Language",
        ["Menu.View.Language.German"] = "German",
        ["Menu.View.Language.English"] = "English",
        ["Menu.Help"] = "_Help",
        ["Menu.Help.About"] = "About _Gonk Note",

        // ---- Sidebar ----
        ["Sidebar.Toggle"] = "Show/hide sidebar (Ctrl+B)",
        ["Sidebar.Pinned"] = "PINNED",
        ["Sidebar.SwitchTheme"] = "Switch theme",
        ["Sidebar.ThemeTooltip"] = "Dark/light mode (Ctrl+T)",

        // ---- New documents ----
        ["New.Folder"] = "New folder",
        ["New.Notebook"] = "New notebook",
        ["New.Whiteboard"] = "New whiteboard",
        ["New.Text"] = "New text document",

        // ---- Folder tree ----
        ["Tree.Open"] = "Open",
        ["Tree.Rename"] = "Rename",
        ["Tree.Delete"] = "Delete",
        ["Tree.Favorite"] = "Favourite",
        ["Tree.IconColor"] = "Icon colour",
        ["Tree.IconColor.Auto"] = "Automatic (folder colour)",
        ["Tree.IconColor.AutoTooltip"] = "Takes the colour of the parent folder",
        ["Tree.IconColor.Custom"] = "Custom colour…",

        // ---- Colours ----
        ["Color.Blue"] = "Blue",
        ["Color.Teal"] = "Teal",
        ["Color.Pink"] = "Pink",
        ["Color.Purple"] = "Purple",
        ["Color.Red"] = "Red",
        ["Color.Orange"] = "Orange",
        ["Color.Yellow"] = "Yellow",
        ["Color.Green"] = "Green",
        ["Color.Gray"] = "Grey",

        // ---- Gallery (start view without an open document) ----
        ["Gallery.Back"] = "Back",
        ["Gallery.New"] = "New",
        ["Gallery.Empty.Title"] = "Nothing here yet",
        ["Gallery.Empty.Hint"] = "Create something via “New” or the sidebar.",
        ["Gallery.Options"] = "Options",

        // ---- Window / tabs ----
        ["Window.CloseTab"] = "Close (Ctrl+W)",
        ["Window.Minimize"] = "Minimise",
        ["Window.Restore"] = "Restore",
        ["Window.Close"] = "Close",

        // ---- Keyboard shortcuts (menu display only) ----
        ["Shortcut.Save"] = "Ctrl+S",
        ["Shortcut.SaveAll"] = "Ctrl+Shift+S",
        ["Shortcut.Quit"] = "Alt+F4",
        ["Shortcut.Sidebar"] = "Ctrl+B",
        ["Shortcut.Theme"] = "Ctrl+T",
        ["Shortcut.Rename"] = "F2",
        ["Shortcut.Delete"] = "Del",

        // ---- Texts set from code ----
        ["Gallery.Root"] = "Documents",
        ["Item.CopySuffix"] = " (copy)",
        ["Import.Filter"] = "Documents (*.docx;*.md)|*.docx;*.md|Word documents (*.docx)|*.docx",
        ["Gallery.DateFormat"] = "d MMM yyyy, HH:mm",
    };
}
