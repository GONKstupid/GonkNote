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
        // **Ohne Formatliste im Menüpunkt** (seit §4.28). Sie stand dort als „(DOCX /
        // Markdown)" bzw. „(PDF / DOCX / Markdown)" und war zweimal ungenau: PNG fehlte, und
        // der Linux-Kopf importiert nur DOCX — Markdown liest dort niemand. Welche Formate
        // wirklich gehen, sagt der Dateidialog, und der sagt es je Kopf richtig.
        ["Menu.File.Import"] = "Dokument _importieren…",
        ["Menu.File.Export"] = "_Exportieren…",
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
        ["Menu.Help.Guide"] = "_Erste Schritte",
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
        // Kontextmenü im Ordnerbaum – die Beschriftung wechselt mit dem Zustand.
        // Standen bis Phase 2 fest verdrahtet auf Deutsch im TreeItemViewModel.
        ["Tree.Pin"] = "Anpinnen",
        ["Tree.Unpin"] = "Nicht mehr anpinnen",
        ["Tree.MarkFavorite"] = "Als Favorit",
        ["Tree.UnmarkFavorite"] = "Favorit entfernen",
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
        // Die drei Kuerzel des Editor-Kontextmenues. Sie standen bis zum 2026-08-30
        // fest verdrahtet in TextEditorView.xaml — im englischen Programm also auf
        // Deutsch (HANDOFF §4.74, Dauerregel 1).
        ["Shortcut.Cut"] = "Strg+X",
        ["Shortcut.Copy"] = "Strg+C",
        ["Shortcut.Paste"] = "Strg+V",

        // ---- Texte, die der Code setzt ----
        ["Gallery.Root"] = "Dokumente",
        ["Item.CopySuffix"] = " (Kopie)",
        ["Gallery.DateFormat"] = "d. MMM yyyy, HH:mm",

        // Titel und Formatliste der Dateidialoge. Die Endungen stehen im Code
        // (GonkNote.Core.Platform.FileFilter); hier steht nur, was der Nutzer liest.
        ["Dialog.ImportTitle"] = "Dokument importieren",
        ["Dialog.ExportTitle"] = "Exportieren",
        ["Filter.Documents"] = "Dokumente (*.docx;*.md)",
        ["Filter.Word"] = "Word-Dokument (*.docx)",
        ["Filter.Markdown"] = "Markdown (*.md)",
        ["Filter.AllFiles"] = "Alle Dateien (*.*)",
        ["Filter.Pdf"] = "PDF-Dokument (*.pdf)",
        ["Filter.Png"] = "PNG-Bild(er) (*.png)",
        ["Filter.Images"] = "Bilder (*.png;*.jpg;*.jpeg;*.webp)",
        ["Filter.ImagesImport"] = "Bilder (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.svg)",
        ["Filter.InsertAll"] = "Bilder, PDF & Word",

        // ---- Whiteboard / Notizbuch ----
        ["Tool.Hand"] = "Hand",
        ["Tool.Hand.Tip"] = "Hand (H) – Leinwand verschieben (oder mittlere Maustaste)",
        ["Tool.Pen"] = "Stift (S)",
        ["Tool.SmoothPen"] = "Formen-Stift (G) – erkennt Linien, Kreise, Rechtecke & Co., glättet Kurven",
        ["Tool.Pencil"] = "Bleistift (B)",
        ["Tool.Highlighter"] = "Textmarker (M)",
        ["Tool.Eraser"] = "Radiergummi (E)",
        ["Tool.Lasso"] = "Lasso-Auswahl (L) – Objekte umkreisen (nur was ~vollständig umschlossen ist)",
        ["Tool.Move"] = "Verschieben",
        ["Tool.Move.Tip"] = "Verschieben (V) – Objekt direkt anklicken, dann verschieben oder skalieren",
        ["Tool.Text"] = "Textfeld (T)",
        ["Tool.Shape.Rect"] = "Rechteck (F)",
        ["Tool.Shape.Line"] = "Linie",
        ["Tool.Shape.Arrow"] = "Pfeil",
        ["Tool.Shape.Ellipse"] = "Ellipse",
        ["Tool.Shape.Triangle"] = "Dreieck",
        ["Tool.Chart"] = "Diagramm",
        ["Tool.Chart.Tip"] = "Diagramm einfügen (Säulen, Balken, Linie, Punkt, Kuchen, Radar)",
        ["Tool.Sticky"] = "Notizzettel (N) – farbigen Klebezettel setzen",
        ["Tool.Sticker"] = "Sticker",
        ["Tool.Sticker.Tip"] = "Sticker – Bild-Aufkleber aus der Sammlung einfügen",
        ["Tool.Ruler"] = "Lineal",
        ["Tool.Ruler.Tip"] = "Lineal (R) – gerade Linien entlang der Kante; Körper ziehen = bewegen, Griff ziehen = drehen",
        ["Tool.SetSquare"] = "Geodreieck",
        ["Tool.SetSquare.Tip"] = "Geodreieck (D) – gerade Linien an drei Kanten, mit Winkelskala; Körper ziehen = bewegen, Griff ziehen = drehen",
        ["Color.Auto"] = "Standard (Schwarz auf hellen, Weiß auf dunklen Seiten)",
        ["Color.Custom"] = "Eigene Farbe",
        ["Color.Pick"] = "Farbe wählen (Farbrad / Hex)",
        ["Size.Tip"] = "Strichstärke – lange drücken für Zahleneingabe",
        ["Action.Delete"] = "Löschen",
        ["Action.Undo"] = "Rückgängig (Strg+Z)",
        ["Action.Redo"] = "Wiederholen (Strg+Y)",
        ["Zoom.Out"] = "Verkleinern",
        ["Zoom.Reset"] = "Zoom zurücksetzen",
        ["Zoom.In"] = "Vergrößern",
        ["Wb.InsertFile"] = "Datei einfügen",
        ["Wb.InsertFile.Tip"] = "Datei einfügen – Bild, PDF oder Word (bei PDF/Word Seiten auswählbar; im Notizbuch als neue Seiten, im Whiteboard als Bild-Seiten); auch per Strg+V oder Drag & Drop",
        ["Wb.Settings"] = "Einstellungen",
        ["Wb.Settings.Tip"] = "Einstellungen (Seite, Formen, Text, Cover)",
        // Eigener Text für den Linux-Kopf: dessen Leiste hat nur den Seiten-Abschnitt, weil
        // es Formen, Text und Cover dort (noch) nicht gibt. Der Text daneben verspräche
        // sonst vier Dinge und zeigte eines (HANDOFF §4.12).
        ["Wb.Settings.PageTip"] = "Seiteneinstellungen (Muster, Farbton, Format)",
        ["Wb.Settings.Close"] = "Einstellungen schließen",
        ["Page.Previous"] = "Vorherige Seite",
        ["Page.Next"] = "Nächste Seite",
        ["Page.Add"] = "Neue Seite",
        ["Page.Delete"] = "Seite löschen",
        ["Page.Label"] = "Seite {0} / {1}",
        ["Settings.Page"] = "Seite",
        ["Settings.Page.Pattern"] = "Muster",
        ["Settings.Page.Blank"] = "Blanko",
        ["Settings.Page.Lines"] = "Liniert",
        ["Settings.Page.Grid"] = "Kariert",
        ["Settings.Page.Dots"] = "Punktiert",
        ["Settings.Page.Shade"] = "Farbton",
        ["Settings.Page.Shade.Auto"] = "Wie App-Design",
        ["Settings.Page.Shade.Light"] = "Hell",
        ["Settings.Page.Shade.Dark"] = "Dunkel",
        ["Settings.Page.Format"] = "Format",
        ["Settings.Page.Orientation"] = "Ausrichtung",
        ["Settings.Page.Portrait"] = "Hochformat",
        ["Settings.Page.Landscape"] = "Querformat",
        ["Settings.Page.AsDefault"] = "Als Standard für neue Seiten",
        ["Settings.Page.CoverHint"] = "Das Cover hat kein Muster und keinen Farbton — sein Aussehen steht in den Cover-Einstellungen.",
        ["Settings.Shapes"] = "Formen",
        ["Settings.Shapes.Hint"] = "Form wählst du in der Werkzeugleiste; hier die Füllung.",
        ["Settings.Shapes.FillToggle"] = "Füllung ein/aus",
        ["Settings.Shapes.FillColor"] = "Füllfarbe wählen",
        ["Settings.Shapes.Fill"] = "Füllung",
        ["Settings.Shapes.Opacity"] = "Deckkraft",
        ["Settings.Text"] = "Text",
        ["Settings.Text.ColorTip"] = "Textfarbe wählen (gilt auch als Tintenfarbe)",
        ["Settings.Text.Color"] = "Textfarbe",
        ["Settings.Text.BgTip"] = "Hintergrundfarbe des Textfelds",
        ["Settings.Text.Bg"] = "Hintergrund",
        ["Settings.Text.Transparent"] = "Transparent",
        ["Settings.Text.Font"] = "Schriftart",
        ["Settings.Text.Hint"] = "Wirkt auf neue Textfelder sowie das gerade bearbeitete bzw. ausgewählte.",
        ["Settings.Sticky"] = "Notizzettel",
        ["Settings.Sticky.Color"] = "Zettelfarbe",
        ["Settings.Sticky.CustomColor"] = "Eigene Zettelfarbe wählen",
        ["Settings.Sticky.Hint"] = "Zum Setzen aufs Blatt tippen, dann tippen zum Beschriften. Verschieben/Skalieren mit dem Verschieben- oder Lasso-Werkzeug.",
        ["Settings.Sticker.Hint"] = "Klick fügt den Sticker mittig aufs Blatt ein. Danach mit dem Verschieben-Werkzeug (V) platzieren und skalieren.",
        ["Settings.Sticker.Add"] = "Sticker hinzufügen",
        ["Settings.Sticker.Empty"] = "Noch keine Sticker. Über „Sticker hinzufügen“ eigene Bilder ergänzen.",
        ["Settings.Cover"] = "Cover",
        ["Settings.Cover.Gradient"] = "Farbverlauf",
        ["Settings.Cover.StartColor"] = "Startfarbe wählen",
        ["Settings.Cover.Start"] = "Start",
        ["Settings.Cover.EndColor"] = "Endfarbe wählen",
        ["Settings.Cover.End"] = "Ende",
        ["Settings.Cover.Font"] = "Schrift",
        ["Settings.Cover.Image"] = "Bild als Cover",
        ["Settings.Cover.ChooseImage"] = "Bild wählen…",
        ["Settings.Cover.RemoveImage"] = "Entfernen",
        ["Settings.Cover.ImageHint"] = "Das Bild ersetzt Verlauf und Titel auf dem Cover.",
        ["Settings.Cover.Presets"] = "Vorlagen",
        // Der Knopf unter der Sammlung. **Nicht „Vorlagen"** — er stand dort in §4.81 kurz
        // als „+ Vorlagen" und las sich wie eine zweite Überschrift statt wie ein Handgriff.
        // Am laufenden Programm gesehen; der Sticker-Knopf daneben macht es richtig vor.
        ["Settings.Cover.Add"] = "Vorlage hinzufügen",
        ["Quick.Cut.Tip"] = "Ausschneiden (Strg+X)",
        ["Quick.Cut"] = "Ausschneiden",
        ["Quick.Copy.Tip"] = "Kopieren (Strg+C)",
        ["Quick.Copy"] = "Kopieren",
        ["Quick.Duplicate.Tip"] = "Duplizieren (Strg+D)",
        ["Quick.Duplicate"] = "Duplizieren",
        ["Quick.Paste.Tip"] = "Einfügen (Strg+V)",
        ["Quick.Paste"] = "Einfügen",
        ["Quick.Ocr"] = "Text erkennen (OCR)",
        ["Quick.Delete.Tip"] = "Löschen (Entf)",
        ["Quick.SelectAll.Tip"] = "Alles auswählen (Strg+A)",
        ["Quick.SelectAll"] = "Alles auswählen",
        ["Busy.Importing"] = "Wird importiert…",
        ["Busy.Pdf"] = "PDF wird importiert…",
        ["Busy.Pdf.Progress"] = "PDF wird importiert…  {0} / {1}",
        ["Busy.Docx"] = "Word-Dokument wird eingefügt…",
        ["Page.Cover"] = "Cover",
        ["Size.Eraser.Tip"] = "Radierergröße – lange drücken für Zahleneingabe",

        // ---- Text-Editor ----
        ["Action.Undo.Short"] = "Rückgängig",
        ["Action.Redo.Short"] = "Wiederholen",
        ["Ed.Tab.Home"] = "Start",
        ["Ed.Tab.Insert"] = "Einfügen",
        ["Ed.Tab.Layout"] = "Layout",
        ["Ed.Tab.References"] = "Verweise",
        ["Ed.Tab.Table"] = "Tabelle",
        ["Ed.FormatPainter.Tip"] = "Format übertragen: Klicken, dann Ziel markieren",
        ["Ed.FormatPainter"] = "Format übertragen",
        ["Ed.ClearFormat"] = "Formatierung löschen",
        ["Ed.Font"] = "Schriftart",
        ["Ed.FontSize"] = "Schriftgröße",
        ["Ed.FontGrow"] = "Schrift vergrößern",
        ["Ed.FontShrink"] = "Schrift verkleinern",
        ["Ed.Bold"] = "Fett (Strg+B)",
        ["Ed.Italic"] = "Kursiv (Strg+I)",
        ["Ed.Underline"] = "Unterstrichen (Strg+U)",
        ["Ed.Strike"] = "Durchgestrichen",
        ["Ed.Subscript"] = "Tiefgestellt",
        ["Ed.Superscript"] = "Hochgestellt",
        ["Ed.TextColor.Apply"] = "Textfarbe anwenden",
        ["Ed.TextColor.Pick"] = "Textfarbe wählen",
        ["Ed.Highlight.Apply"] = "Textmarker anwenden",
        ["Ed.Highlight.Pick"] = "Markerfarbe wählen (inkl. „keine“)",
        ["Ed.Align"] = "Textausrichtung",
        ["Ed.Align.Left"] = "Linksbündig",
        ["Ed.Align.Center"] = "Zentriert",
        ["Ed.Align.Right"] = "Rechtsbündig",
        ["Ed.Align.Justify"] = "Blocksatz",
        ["Ed.List.Bullet"] = "Aufzählung",
        ["Ed.List.BulletPick"] = "Aufzählungszeichen wählen",
        ["Ed.List.Number"] = "Nummerierte Liste",
        ["Ed.List.NumberPick"] = "Nummerierungsart wählen",
        ["Ed.Indent.More"] = "Einzug vergrößern",
        ["Ed.Indent.Less"] = "Einzug verkleinern",
        ["Ed.Styles.All"] = "Alle Formatvorlagen",
        ["Ed.Styles"] = "Formatvorlagen",
        ["Ed.List.Bullets"] = "Aufzählungszeichen",
        ["Ed.List.Numbering"] = "Nummerierung",
        ["Ed.Table.Insert"] = "Tabelle einfügen",
        ["Ed.Table.InsertDialog"] = "Tabelle einfügen…",
        ["Ed.Table.FromText"] = "Text in Tabelle umwandeln",
        ["Ed.Table.FromText.Tip"] = "Markierten Text (Trennung per Tab, Semikolon oder Komma) in eine Tabelle umwandeln",
        ["Ed.Table.Quick"] = "Schnelltabellen",
        ["Ed.Table.Quick.Calendar"] = "Kalender (Monat)",
        ["Ed.Table.Quick.List"] = "Einfache Liste (mit Kopfzeile)",
        ["Ed.Image.Insert"] = "Bild einfügen",
        ["Ed.Image"] = "Bild",
        ["Ed.InfoBox.Tip"] = "Farbige Infobox (1×1-Tabelle mit Füllung und Rahmen)",
        ["Ed.InfoBox.Insert"] = "Infobox einfügen",
        ["Ed.InfoBox"] = "Infobox",
        ["Ed.Rule"] = "— Trennlinie",
        ["Ed.Symbol.Insert"] = "Sonderzeichen einfügen",
        ["Ed.Symbol"] = "Sonderzeichen",
        ["Ed.Symbol.Button"] = " Ω Symbol ",
        ["Ed.Symbol.Group.Text"] = "Satzzeichen",
        ["Ed.Symbol.Group.Arrows"] = "Pfeile",
        ["Ed.Symbol.Group.Math"] = "Mathematik",
        ["Ed.Symbol.Group.Greek"] = "Griechisch",
        ["Ed.Symbol.Group.Misc"] = "Weiteres",
        ["Ed.Chart.Tip"] = "Diagramm aus eigenen Werten einfügen (Balken, Linie, Kuchen)",
        ["Ed.Chart"] = "Diagramm",
        ["Ed.HeaderFooter.Dialog"] = "Kopf-/Fußzeile…",
        ["Ed.HeaderFooter"] = "Kopf- und Fußzeile",
        ["Ed.PageNumbers"] = "Seitenzahlen",
        ["Ed.Layout.Format"] = "Format",
        ["Ed.Layout.PaperSize"] = "Papierformat",
        ["Ed.Layout.Letter"] = "Letter",
        ["Ed.Layout.Portrait"] = "Hochformat",
        ["Ed.Layout.Portrait.Short"] = " Hoch ",
        ["Ed.Layout.Landscape"] = "Querformat",
        ["Ed.Layout.Landscape.Short"] = " Quer ",
        ["Ed.Layout.Margins.Tip"] = "Seitenränder einstellen",
        ["Ed.Layout.Margins"] = "Ränder",
        ["Ed.Layout.Spacing.Tip"] = "Absatz- und Zeilenabstände",
        ["Ed.Layout.Spacing"] = "Abstände",
        ["Ed.Layout.Background.Tip"] = "Hintergrundbild / Wasserzeichen",
        ["Ed.Layout.Background"] = "Hintergrundbild",
        ["Ed.Layout.PageBreaks"] = "Seitenumbrüche anzeigen",
        ["Ed.Layout.PageBreaks.Tip"] = "Zeigt als Näherung, wo beim PDF-Export die Seiten umbrechen",
        ["Ed.Toc.Insert.Tip"] = "Erstellt ein Inhaltsverzeichnis aus den Überschriften 1–4",
        ["Ed.Toc.Insert"] = "Inhaltsverzeichnis einfügen",
        ["Ed.Toc.Update.Short"] = "Aktualisieren",
        ["Ed.Toc.Update.Tip"] = "Inhaltsverzeichnis neu aus den Überschriften aufbauen",
        ["Ed.Toc.Update"] = "Inhaltsverzeichnis aktualisieren",
        ["Ed.Link.Tip"] = "Hyperlink einfügen/bearbeiten",
        ["Ed.Link.Insert"] = "Link einfügen",
        ["Ed.Link"] = "Link",
        ["Ed.Caption.Tip"] = "Bildunterschrift „Abbildung N: …“ unter dem Bild/Absatz einfügen",
        ["Ed.Caption.Insert"] = "Beschriftung einfügen",
        ["Ed.Caption"] = "Beschriftung",
        ["Ed.Caption.Prefix"] = "Abbildung",
        ["Action.Apply"] = "Übernehmen",
        ["Ed.References.NoteName"] = "Hinweis Verweise",
        ["Ed.References.Note"] = "Beim DOCX-Export wird das Inhaltsverzeichnis zu einem echten Word-Feld (mit Seitenzahlen, aktualisiert sich beim Öffnen). Überschriften 1–4 erscheinen automatisch im Verzeichnis.",
        ["Ed.Table.Row"] = "Zeile",
        ["Ed.Table.Row.Above"] = "Zeile darüber einfügen",
        ["Ed.Table.Row.Below"] = "Zeile darunter einfügen",
        ["Ed.Table.Row.Delete"] = "Zeile löschen",
        ["Ed.Table.Column"] = "Spalte",
        ["Ed.Table.Column.Left"] = "Spalte links einfügen",
        ["Ed.Table.Column.Right"] = "Spalte rechts einfügen",
        ["Ed.Table.Column.Delete"] = "Spalte löschen",
        ["Ed.Table.Merge"] = "Verbinden",
        ["Ed.Table.Merge.Tip"] = "Markierte Zellen verbinden (waagerecht, senkrecht oder rechteckig)",
        ["Ed.Table.Split"] = "Teilen",
        ["Ed.Table.Unmerge"] = "Zellverbund aufheben",
        ["Ed.Table.SplitCell"] = "Zelle teilen…",
        ["Ed.Table.SplitTable"] = "Tabelle teilen (oberhalb der Zeile)",
        ["Ed.Table.Sort"] = "Sortieren…",
        ["Ed.Table.Sort.Tip"] = "Zeilen nach einer Spalte sortieren (Text/Zahl/Datum)",
        ["Ed.Table.Formula"] = "Formel…",
        ["Ed.Table.Formula.Tip"] = "Berechnung einfügen, z. B. =SUMME(ABOVE) oder =MITTELWERT(LEFT)",
        ["Ed.Table.ToText"] = "In Text…",
        ["Ed.Table.ToText.Tip"] = "Tabelle in Text umwandeln",
        ["Ed.Table.Design.Tip"] = "Formatvorlage, Rahmen, Füllung und Größe in der Seitenleiste",
        ["Ed.Table.Design"] = "Design & Rahmen…",
        ["Ed.Table.Delete"] = "Tabelle löschen",
        ["Ed.Table.NoteName"] = "Hinweis Tabelle",
        ["Ed.Table.Note"] = "Technische Grenzen der Textengine: Zellränder sind immer durchgezogen (keine Stricharten), Text lässt sich in Zellen nur waagerecht ausrichten (senkrechte Ausrichtung/Zeilenhöhe über Zellenränder annähern), keine Excel-Einbettung und keine Kopfzeilen-Wiederholung beim Seitenumbruch.",
        ["Ed.Find.Term"] = "Suchbegriff",
        ["Ed.Find.Next"] = "Weiter",
        ["Ed.Find.ReplaceWith"] = "Ersetzen durch",
        ["Ed.Find.Replace"] = "Ersetzen",
        ["Ed.Find.ReplaceAll"] = "Alle ersetzen",
        ["Ed.Find.Close"] = "Schließen (Esc)",
        ["Ed.Find.Tip"] = "Suchen & Ersetzen (Strg+F)",
        ["Ed.Find"] = "Suchen",
        // Die zwei Meldungen der Suchleiste. **Sie standen bis §4.80 fest verdrahtet auf
        // Deutsch im WPF-Kopf** („Nicht gefunden", „{0} ersetzt") — ein englisches Programm
        // zeigte dort Deutsch (Dauerregel 1, dieselbe Sorte Fund wie §4.74 und §4.75).
        ["Ed.Find.NotFound"] = "Nicht gefunden",
        ["Ed.Find.Replaced"] = "{0} ersetzt",
        ["Ed.Status.Counts"] = "Wörter: 0 · Zeichen: 0",
        ["Ed.Spell.Language"] = "Sprache für die Rechtschreibprüfung",
        ["Lang.German"] = "Deutsch",
        ["Lang.English"] = "Englisch",
        ["Ed.Spell.Toggle"] = "Rechtschreibprüfung ein/aus",
        ["Ed.Spell"] = "Rechtschreibprüfung",
        ["Ed.Spell.Button"] = " ABC✓ ",
        ["Zoom.Out.Long"] = "Zoom verkleinern",
        ["Zoom.In.Long"] = "Zoom vergrößern",
        ["Ed.Navigator.Tip"] = "Überschriften-Navigator",
        ["Ed.Navigator"] = "Überschriften",
        ["Ed.Navigator.Empty"] = "Noch keine Überschriften — wer einen Absatz als Überschrift auszeichnet, sieht ihn hier.",
        ["Ed.Advanced"] = "Erweiterte Einstellungen",
        ["Ed.Margins.Preset"] = "Vorlage",
        ["Ed.Margins.Normal"] = "Normal (2 cm)",
        ["Ed.Margins.Narrow"] = "Schmal (1,27 cm)",
        ["Ed.Margins.Wide"] = "Breit (3 cm)",
        ["Ed.Margins.Study"] = "Lernblatt (4 cm links)",
        ["Ed.Margins.Custom"] = "Benutzerdefiniert",
        ["Ed.Margins.Left"] = "Links",
        ["Ed.Margins.Top"] = "Oben",
        ["Ed.Margins.Right"] = "Rechts",
        ["Ed.Margins.Bottom"] = "Unten",
        ["Ed.Margins.Unit"] = "Werte in cm",
        ["Ed.Paragraphs"] = "Absätze",
        ["Ed.Spacing.Before"] = "Abstand vor",
        ["Ed.Spacing.After"] = "Abstand nach",
        ["Ed.Spacing.Unit"] = "Abstände in pt",
        ["Ed.Spacing.Line"] = "Zeilenabstand",
        ["Ed.Background.Choose"] = "Bild wählen…",
        ["Ed.Background.Tip"] = "Seitenfüllendes Bild hinter dem Text (Wasserzeichen)",
        ["Ed.Background.Pick"] = "Hintergrundbild wählen",
        ["Ed.Opacity"] = "Deckkraft",
        ["Ed.Remove"] = "Entfernen",
        ["Ed.Table.Style"] = "Formatvorlage",
        ["Ed.Table.HeaderRow"] = "Kopfzeile",
        ["Ed.Table.TotalRow"] = "Ergebniszeile",
        ["Ed.Table.BandedRows"] = "Zeilenbänder",
        ["Ed.Table.BandedColumns"] = "Spaltenbänder",
        ["Ed.Table.Borders"] = "Rahmen der Auswahl",
        ["Ed.Table.Border.Width"] = "Dicke",
        ["Ed.Table.Border.Thin"] = "Dünn (0,5)",
        ["Ed.Table.Border.Normal"] = "Normal (1)",
        ["Ed.Table.Border.Strong"] = "Kräftig (2)",
        ["Ed.Table.Border.Thick"] = "Dick (3,5)",
        ["Ed.Table.Border.All"] = "Alle",
        ["Ed.Table.Border.Outside"] = "Nur außen",
        ["Ed.Table.Border.Inside"] = "Nur innen",
        ["Ed.Table.Border.None"] = "Keine",
        ["Ed.Table.Border.Color"] = "Rahmenfarbe…",
        ["Ed.Table.Fill"] = "Füllung",
        ["Ed.Table.Fill.Color"] = "Füllfarbe…",
        ["Ed.Table.Size"] = "Größe",
        ["Ed.Table.ColumnWidth"] = "Spaltenbreite…",
        ["Ed.Table.AutoFit"] = "AutoAnpassen",
        ["Ed.Table.AutoFit.Content"] = "Inhalt",
        ["Ed.Table.AutoFit.Content.Tip"] = "Spaltenbreiten an den Inhalt anpassen",
        ["Ed.Table.AutoFit.Page"] = "Seite",
        ["Ed.Table.AutoFit.Page.Tip"] = "Tabelle auf Seitenbreite dehnen",
        ["Ed.Table.AutoFit.Fixed"] = "Fest…",
        ["Ed.Table.AutoFit.Fixed.Tip"] = "Feste Spaltenbreite in cm",
        ["Ed.Table.CellPadding"] = "Zellenränder…",
        ["Ed.Table.CellPadding.Tip"] = "Innenabstand der ausgewählten Zellen",
        ["Ed.Link.Edit"] = "Link bearbeiten…",
        ["Ed.Link.Remove"] = "Link entfernen",
        ["Ed.Object.Bigger"] = "Größer",
        ["Ed.Object.Smaller"] = "Kleiner",
        ["Ed.Object.Wider"] = "Breiter",
        ["Ed.Object.Narrower"] = "Schmaler",
        ["Ed.Object.Taller"] = "Höher",
        ["Ed.Object.Shorter"] = "Flacher",
        ["Ed.Object.ExactSize"] = "Genaue Größe…",
        ["Ed.Object.Behind"] = "Hinter den Text legen",
        ["Ed.Object.Front"] = "Vor den Text holen",
        ["Ed.Table.MergeSelection"] = "Zellen verbinden (Auswahl)",
        ["Ed.Table.AllTools"] = "Alle Tabellen-Werkzeuge (Ribbon-Tab)",

        // ---- Dialoge ----
        ["Dlg.Cancel"] = "Abbrechen",
        ["Dlg.Apply"] = "Übernehmen",
        ["Dlg.Ok"] = "OK",
        ["Dlg.All"] = "Alle",
        ["Dlg.None"] = "Keine",
        // Neu in Phase 3. Der WPF-Kopf braucht sie nicht: dort liefert die MessageBox von
        // Windows ihre eigenen Knöpfe — in der Sprache des **Systems**, nicht in der der
        // App. Avalonia hat keine MessageBox, der Kopf baut sie selbst, und damit sind
        // die Beschriftungen zum ersten Mal unsere. (Nebenbei ist das die einzige Stelle,
        // an der der Linux-Kopf die Sprachwahl konsequenter befolgt als der Windows-Kopf.)
        // Der Text des unerwarteten Fehlers. Er stand bis zum 2026-08-31 in BEIDEN
        // Koepfen fest verdrahtet auf Deutsch — kein Unterschied zwischen ihnen, aber
        // eine Luecke in beiden (§4.75, Dauerregel 1).
        ["Msg.Unexpected"] =
            "Es ist ein unerwarteter Fehler aufgetreten:\n\n{0}\n\n" +
            "Die App läuft weiter. Einzelheiten stehen in:\n{1}",
        ["Dlg.Yes"] = "Ja",
        ["Dlg.No"] = "Nein",
        // ---- Textdokument im Linux-Kopf (HANDOFF §4.28) ----
        // Der Schlüssel Tab.NoCanvasYet ist mit §4.28 verschwunden: die Registerkarte zeigt
        // seitdem das gesetzte Dokument. Was blieb, sind zwei Sätze — der eine für ein
        // Dokument, das die Übernahme noch vor sich hat, der andere für die Tafel, deren
        // Export weiterhin am WPF-Kopf hängt (Phase 4.5).
        //
        // Bewusst ganze Sätze und kein „Nicht implementiert": wer das liest, soll wissen,
        // dass die Daten in Ordnung sind.
        ["Io.NotOnThisPlatform"] =
            "Diesen Export gibt es auf dieser Plattform noch nicht — er zeichnet über die " +
            "Windows-Zeichenfläche und zieht erst mit den Linux-Werkzeugen um. " +
            "Textdokumente lassen sich hier bereits exportieren.",
        ["Io.NotMigrated"] =
            "Dieses Dokument stammt aus der Windows-Fassung und ist noch nicht ins eigene " +
            "Format übernommen. Die Übernahme läuft nur dort — einmal in der Windows-Fassung " +
            "öffnen und speichern genügt.",
        ["Td.NotMigrated"] =
            "Dieses Dokument stammt aus der Windows-Fassung und ist noch nicht ins eigene " +
            "Format übernommen — anzeigen lässt es sich hier deshalb noch nicht. " +
            "Der Inhalt ist unverändert gespeichert. Einmal in der Windows-Fassung öffnen " +
            "und speichern, danach steht es auch hier.",
        // Steht im WPF-Editor in einem Inhaltsverzeichnis ohne Überschriften — sonst
        // hätte der Behälter keine Höhe und das Feld wäre da, aber unauffindbar (§4.49).
        ["Td.Toc.Empty"] = "Inhaltsverzeichnis — noch keine Überschriften im Dokument",
        // **Hier standen bis Phase 5 `Ed.ViewOnly` und `Ed.ViewOnly.Tip`** — „Nur Ansicht:
        // der Linux-Kopf zeigt das Dokument, geschrieben wird bis auf Weiteres in der
        // Windows-Fassung." **Seit §4.35 wird hier geschrieben**, seit §4.36 formatiert; die
        // Zeile war seitdem eine Zusicherung, die nicht mehr stimmte. Abgerufen hat sie
        // zuletzt niemand. `Ed.TextOnly` darunter ist ihr *lebender* Nachbar und bleibt —
        // der beschreibt eine Ansicht, die es wirklich gibt.
        ["Ed.TextOnly"] = "Nur Text",
        ["Ed.TextOnly.Tip"] =
            "Hier lässt sich der Text schreiben: tippen, löschen, auswählen, rückgängig " +
            "machen. Formate, Tabellen und Bilder setzt weiterhin die Windows-Fassung. " +
            "Angezeigt wird genau das, was auch exportiert würde — derselbe Umbruch, " +
            "derselbe Zeichner.",
        // ---- Die drei neuen Reiter des Linux-Ribbons (HANDOFF §4.37, Schritt 6) ----
        // Was es drüben schon gibt, benutzt die vorhandenen Schlüssel (Ed.Table.*, Ed.Link.*,
        // Ed.Toc.*); hier stehen nur die, die der WPF-Kopf nicht braucht, weil er es anders
        // löst — er hat einen Dialog, wo hier ein Feld in der Leiste steht.
        ["Ed.Break.Page"] = "Seitenumbruch",
        ["Ed.Break.Page.Tip"] = "Ab hier auf einer neuen Seite weiterschreiben",
        ["Ed.Break.Line"] = "Zeilenumbruch",
        ["Ed.Break.Line.Tip"] =
            "Neue Zeile im selben Absatz (Umschalt+Eingabe) — Absatzabstände und Listenmarke " +
            "bleiben unangetastet",
        ["Ed.Field"] = "Feld",
        ["Ed.Field.Tip"] =
            "Eine Stelle, deren Inhalt gerechnet wird: Seitenzahl, Seitenanzahl, Datum, Titel. " +
            "Sie stimmt nach jeder Änderung von selbst.",
        ["Ed.Field.PageNumber"] = "Seitenzahl",
        ["Ed.Field.PageCount"] = "Seitenanzahl",
        ["Ed.Field.Date"] = "Datum",
        ["Ed.Field.Title"] = "Titel des Dokuments",
        ["Ed.Table.Rows"] = "Zeilen",
        ["Ed.Table.Columns"] = "Spalten",
        ["Ed.Link.Target"] = "Ziel",
        ["Ed.Link.Target.Tip"] =
            "Adresse des Verweises. Steht die Schreibmarke in einem vorhandenen Verweis, " +
            "erscheint sein Ziel hier und lässt sich ändern.",
        ["Ed.Link.Set"] = "Setzen",
        // Der Satz, der den Reiter „Tabelle" ersetzt, solange die Marke in keiner steht.
        // **Kein ausgegrauter Reiter:** Ein Knopf, der meistens nichts tun kann, ist die
        // ausgegraute Fläche aus §4.28 in groß (HANDOFF §4.37).
        ["Ed.Table.NotInTable"] =
            "Setz die Schreibmarke in eine Tabelle — dann stehen hier ihre Werkzeuge.",
        // ---- Farbnamen für Schrift und Hervorhebung (HANDOFF §4.40, `TdTextfarben`) ----
        // Sie stehen in einer Tabelle in Core; hier sind nur die Namen. „Automatisch" und
        // „Keine" tragen kein Hex — sie nehmen die Abweichung heraus, statt eine Farbe zu setzen.
        ["Td.Color.Auto"] = "Automatisch",
        ["Td.Color.None"] = "Keine",
        ["Td.Color.Red"] = "Rot",
        ["Td.Color.Blue"] = "Blau",
        ["Td.Color.Green"] = "Grün",
        ["Td.Color.Amber"] = "Bernstein",
        ["Td.Color.Purple"] = "Violett",
        ["Td.Color.Grey"] = "Grau",
        ["Td.Color.Yellow"] = "Gelb",
        ["Td.Color.Lime"] = "Limette",
        ["Td.Color.Cyan"] = "Türkis",
        ["Td.Color.Pink"] = "Rosa",
        ["Td.Color.Sky"] = "Himmelblau",
        ["Td.Color.Silver"] = "Silber",
        // Kopf- und Fußzeile in der Einstellungs-Seitenleiste (§4.40).
        ["Ed.HeaderFooter.Header"] = "Kopfzeile",
        ["Ed.HeaderFooter.Footer"] = "Fußzeile",
        ["Ed.HeaderFooter.SuppressFirst"] = "Auf der ersten Seite weglassen",
        // Die drei Zeilen der Platzhalter-Erklaerung. Sie standen bis zum 2026-08-30 fest
        // verdrahtet im WPF-Dialog und fehlten dem Linux-Kopf ganz (§4.74).
        //
        // ⚠ Die Platzhalter selbst bleiben DEUTSCH, auch im englischen Text: sie stehen so
        // in Core (TdField.PlatzhalterTabelle) und werden dort woertlich gesucht. Wer sie
        // uebersetzt, macht aus einer Erklaerung eine Anleitung, die nicht funktioniert.
        ["Ed.HeaderFooter.Fields"] = "{SEITE} = Seitenzahl · {SEITEN} = Seitenanzahl",
        ["Ed.HeaderFooter.Fields2"] = "{DATUM} = heutiges Datum · {TITEL} = Dokumentname",
        ["Ed.HeaderFooter.Example"] =
            "Beispiel: Mathe – Brüche · Klasse 6b · Seite {SEITE}/{SEITEN}",
        ["Ed.PageNumbers.Tip"] = "Fügt „Seite {SEITE} von {SEITEN}“ in die Fußzeile ein",
        // Zwei Sätze in der Einstellungs-Seitenleiste, die eine Grenze benennen, statt sie
        // den Nutzer suchen zu lassen (HANDOFF §4.38).
        ["Ed.Page.NoUndo"] =
            "Seitenränder und Format gelten für den ganzen Abschnitt und stehen nicht im " +
            "Verlauf — Rückgängig holt sie nicht zurück.",
        ["Ed.Spacing.Scope"] =
            "Gilt für die Absätze, die die Auswahl berührt.",
        // **Hier standen bis Phase 5 die drei `Td.RtfLeads*`** — die Warnung über dem Blatt,
        // wenn das Altformat noch führt (§4.36, Nutzer-Entscheidung 2026-08-16: warnen statt
        // sperren). **Mit §4.48 ist der Grund weggefallen und nicht nur die Warnung:** Der
        // WPF-Editor liest und schreibt seitdem das Modell, `Rtf` wird nie mehr überschrieben,
        // und `TdFuehrung.AltformatFuehrt` ist gelöscht — es gibt nichts mehr, wovor zu warnen
        // wäre. Die Streifen sind damals verschwunden, ihre Texte sind liegen geblieben.
        //
        // **Der Satz, für den sie geschrieben wurden, bleibt lesenswert** und steht in §5
        // „Noch offen" 9: *eine Warnung muss die Folge nennen und nicht den Zustand.* „Dieses
        // Dokument führt noch das Altformat" ist für den, der es liest, keine Auskunft,
        // sondern ein Rätsel.
        ["Ed.Export"] = "Exportieren",
        ["Ed.Export.Tip"] = "Dieses Dokument als PDF, Word, Markdown oder PNG schreiben",
        ["Ed.FitWidth"] = "Seitenbreite",
        ["Ed.FitWidth.Tip"] = "Zoom so setzen, dass die Seite in die Breite passt",
        ["Ed.FitPage"] = "Ganze Seite",
        ["Ed.FitPage.Tip"] = "Zoom so setzen, dass eine ganze Seite zu sehen ist",
        // Ablesewerte unter der Beschriftung „Kopf- und Fußzeile" — sie dürfen die
        // Beschriftung nicht wiederholen, sonst steht dort zweimal dasselbe.
        ["Td.HeaderFooter.Both"] = "beide",
        ["Td.HeaderFooter.HeaderOnly"] = "nur Kopfzeile",
        ["Td.HeaderFooter.FooterOnly"] = "nur Fußzeile",
        ["Td.HeaderFooter.None"] = "keine",
        ["Guide.Title"] = "Erste Schritte",
        ["Guide.Heading"] = "Erste Schritte mit Gonk Note",
        ["Guide.Subtitle"] = "Von der ersten Notiz bis zur Sicherung — Schritt für Schritt.",
        ["About.Title"] = "Über Gonk Note",
        // **Kein fester Pfad mehr.** Bis Phase 3 stand hier „%APPDATA%\GonkNote" — im
        // Linux-Kopf also eine Angabe, die dort nicht stimmt, und zwar direkt über der
        // Zeile, die den echten Ordner zeigt (HANDOFF §4.12). Der Satz muss **ohne** Pfad
        // tragen: denselben Schlüssel benutzt der WPF-Dialog, und der zeigt den Ordner
        // nicht daneben an.
        ["About.Subtitle"] = "Offline-Notizen, Whiteboards und Textdokumente. Deine Daten liegen ausschließlich auf diesem Rechner, in deinem Benutzerordner.",
        // {0} = Versionsnummer. Die Phase meint die **Portierung** (Linux/iPadOS), nicht mehr
        // die Entwicklungsphase von V1 — bis 0.2.0 stand hier fest „Phase 3" und war damit
        // zweideutig. Von Hand nachzuziehen, wenn eine Portierungsphase beginnt (HANDOFF §6).
        //
        // Die Zeile sagt, **woran gearbeitet wird**, nicht was fertig ist (HANDOFF §5). Der
        // Zusatz steht dabei, weil die Phasennummer allein zu grob wurde: die Dokument-Engine
        // ist seit dem 2026-08-11 fertig (§4.28), das Schreiben ist der Rest derselben Phase.
        //
        // **Nachgezogen am 2026-08-28 mit M2** (HANDOFF §4.67): Phase 4.5 ist abgeschlossen,
        // Linux und Windows können dasselbe. Gearbeitet wird ab jetzt an Phase 5 — aufräumen,
        // dann veröffentlichen. **Die Versionsnummer bleibt bei 0.3.0**: sie gehört zur
        // Auslieferung und damit ans Ende dieser Phase, nicht an ihren Anfang.
        ["About.Version"] = "Version {0} · Portierung, Phase 5 — aufräumen und veröffentlichen",
        // {0} = Fehlermeldung, {1} = Pfad des Protokolls. Erscheint, wenn sich die Datenbank
        // nicht öffnen lässt — meist beim einmaligen Übertragen einer Altdatenbank nach
        // SQLite. Zwangsläufig in der Standardsprache: die Sprachwahl steht in eben der
        // Datenbank, die gerade nicht lesbar ist (HANDOFF §4.8).
        ["Db.OpenFailed"] =
            "Die Datenbank konnte nicht geöffnet werden:\n\n{0}\n\n" +
            "Die bisherigen Daten sind nicht verloren — die alte Datenbankdatei liegt " +
            "unverändert an ihrem Platz. Einzelheiten stehen in:\n{1}",
        ["Chart.Title"] = "Diagramm einfügen",
        ["Chart.Title.Edit"] = "Diagramm ändern",
        ["Chart.Type"] = "Typ",
        ["Chart.Type.Column"] = "Säulen",
        ["Chart.Type.Bar"] = "Balken",
        ["Chart.Type.Line"] = "Linie",
        ["Chart.Type.Scatter"] = "Punkt (Streuung)",
        ["Chart.Type.ScatterLine"] = "Punkt + Linie",
        ["Chart.Type.Pie"] = "Kuchen",
        ["Chart.Type.Radar"] = "Radar (Netz)",
        ["Chart.TitleField"] = "Titel",
        ["Chart.Categories"] = "Kategorien",
        ["Chart.Categories.Example"] = "Jan, Feb, Mär, Apr",
        ["Chart.Categories.Hint"] = "Achsen-Beschriftungen, durch Komma getrennt",
        ["Chart.SeriesNames"] = "Reihen-Namen",
        ["Chart.Series.Example"] = "Reihe 1",
        ["Chart.SeriesNames.Hint"] = "ein Name je Reihe, durch Komma getrennt (optional)",
        ["Chart.Values"] = "Werte",
        ["Chart.Values.Hint"] = "eine Zeile je Reihe/Kurve, Werte durch Komma getrennt",
        ["Chart.Values.Note"] = "Mehrere Reihen/Kurven: pro Zeile eine Reihe (Enter für neue Zeile).",
        ["Chart.Colors"] = "Farben",
        ["Chart.Color.Change"] = "Farbe {0} ändern",
        ["Chart.Color.Remove"] = "Farbe löschen",
        ["Chart.Color.Add"] = "Neue Farbe hinzufügen",
        ["Chart.Error.NoValues"] = "Bitte Werte eingeben (z. B. 4, 7, 3).",
        ["Chart.Error.RadarNeedsThree"] = "Ein Netzdiagramm braucht mindestens drei Kategorien.",
        ["ColorPicker.Title"] = "Farbe wählen",
        ["ColorPicker.Hex"] = "Hex",
        ["HeaderFooter.Footer"] = "Fußzeile",
        ["HeaderFooter.SkipFirst"] = "Auf der ersten Seite ausblenden (Deckblatt)",
        ["HeaderFooter.Placeholders"] = "Platzhalter:",
        ["Ocr.Title"] = "Texterkennung",
        ["Ocr.Hint"] = "Erkannter Text – bei Bedarf bearbeiten, kopieren oder als Notizzettel einfügen:",
        ["Ocr.AsSticky"] = "Als Notizzettel",
        ["TableSize.Rows"] = "Zeilen",
        ["TableSize.Columns"] = "Spalten",
        ["Sort.Title"] = "Tabelle sortieren",
        ["Sort.By"] = "Sortieren nach",
        ["Sort.Type.Number"] = "Zahl",
        ["Sort.Type.Date"] = "Datum",
        ["Sort.Order"] = "Reihenfolge",
        ["Sort.Ascending"] = "Aufsteigend",
        ["Sort.Descending"] = "Absteigend",
        ["Sort.HeaderRow"] = "Erste Zeile ist Kopfzeile (nicht mitsortieren)",
        ["Sort.Do"] = "Sortieren",
        ["Sort.Type.Text"] = "Text",

        // ---- Meldungen ----
        ["Msg.ImportFailed"] = "Import fehlgeschlagen:",
        // Die Übernahme ins eigene Dokumentmodell läuft still — nur ein Fehler dabei meldet
        // sich (Nutzer-Entscheidung 2026-08-05, HANDOFF §4.22). {0} = Name, {1} = Grund.
        ["Msg.MigrationFailed"] =
            "„{0}“ konnte nicht ins neue Dokumentformat übernommen werden:\n{1}\n\n" +
            "Das Dokument ist unverändert und lässt sich weiter bearbeiten — " +
            "der Versuch wird beim nächsten Öffnen wiederholt.",
        ["Msg.OpenDocumentFirst"] = "Bitte zuerst ein Dokument öffnen.",
        ["Msg.ExportFailed"] = "Export fehlgeschlagen:\n{0}",
        ["Msg.ExportedPages"] = "{0} Seiten exportiert nach:\n{1}\n\nErste Datei öffnen?",
        ["Msg.ExportedFile"] = "Exportiert nach:\n{0}\n\nDatei jetzt öffnen?",
        ["Settings.Export"] = "Export",
        ["Settings.Export.Hint"] = "Exportiert dieses Dokument mit allen Seiten.",
        ["Settings.Export.Pdf"] = "Als PDF exportieren …",
        ["Settings.Export.Png"] = "Als PNG exportieren …",
        ["Msg.ValidationHint"] = "\n\nHinweis: OpenXML-Validierung meldet {0} Punkt(e).",
        ["Msg.ExportImagesMissing"] = "\n\nAchtung: Zu {0} Bild(ern) fehlen die Daten. Sie erscheinen " +
            "im Export als Platzhalter oder in schlechterer Qualität. Prüfe, ob die Sicherung des " +
            "Ordners „gonknote.blobs“ neben der Datenbank vollständig ist.",
        ["Msg.DeleteFolder"] = "Ordner „{0}“ und den gesamten Inhalt löschen?",
        ["Msg.DeleteItem"] = "„{0}“ löschen?",
        ["Msg.DeletePage"] = "Diese Seite und ihren Inhalt löschen?",
        ["Msg.ImageLoadFailed"] = "Bild konnte nicht geladen werden:\n{0}",
        ["Msg.ImageLoadSimple"] = "Das Bild konnte nicht geladen werden.",
        ["Msg.InvalidUrl"] = "Die Adresse ist keine gültige URL.",
        ["Msg.UrlPrompt"] = "Adresse (URL):",
        ["Msg.MergeSameTable"] = "Zum Verbinden bitte Zellen innerhalb derselben Tabelle markieren.",
        ["Msg.NoToc"] = "Kein Inhaltsverzeichnis gefunden. Bitte zuerst über „Inhaltsverzeichnis einfügen“ anlegen.",
        ["Msg.SelectTextFirst"] = "Bitte zuerst den Text markieren, der umgewandelt werden soll.",
        ["Msg.SplitNeedsSecondRow"] = "Der Cursor muss in einer Zeile ab der zweiten stehen (die Tabelle wird oberhalb dieser Zeile getrennt).",
        ["Msg.SplitAcrossMerge"] = "An dieser Stelle verläuft ein Zeilenverbund über die Trennlinie – bitte zuerst den Verbund aufheben.",
        ["Msg.SortWithMerge"] = "Tabellen mit Zeilenverbünden lassen sich nicht sortieren – bitte zuerst die Verbünde aufheben.",
        ["Msg.FormulaUnknown"] = "Formel nicht erkannt. Unterstützt: SUMME, MITTELWERT, MIN, MAX, ANZAHL, PRODUKT über ABOVE, BELOW, LEFT oder RIGHT.",
        ["Msg.NoNumbers"] = "Im gewählten Bereich stehen keine Zahlen.",
        ["Msg.TableSize"] = "{0} × {1} Tabelle",
        ["Msg.StyleName"] = "Formatvorlage {0}",
        ["Msg.PresetDeleteFailed"] = "Vorlage konnte nicht gelöscht werden:\n{0}",
        ["Msg.PresetLoadFailed"] = "Vorlage konnte nicht geladen werden:\n{0}",
        ["Msg.LoadFailed"] = "Konnte nicht geladen werden:",
        ["Msg.PdfNoPages"] = "Das PDF enthält keine darstellbaren Seiten.",
        ["Msg.PdfLoadFailed"] = "PDF konnte nicht geladen werden:\n{0}",
        ["Msg.DocxNoPages"] = "Das Word-Dokument enthält keine darstellbaren Seiten.",
        ["Msg.DocxLoadFailed"] = "Word-Dokument konnte nicht geladen werden:\n{0}",
        ["Msg.OcrNoText"] = "Es wurde kein Text erkannt.",
        ["Msg.OcrFailed"] = "Texterkennung fehlgeschlagen:\n{0}",
        // Die Wartemeldung stand bis Phase 4.5, Stück 6 hart im WPF-Kopf — auf Deutsch, auch
        // wenn die App auf Englisch lief. Jetzt hier, und beide Köpfe lesen sie von hier.
        ["Msg.OcrRunning"] = "Text wird erkannt…",
        ["Msg.OcrRunningMany"] = "Text wird erkannt…  (mehrere Bilder)",
        ["Msg.StickerFailed"] = "Sticker konnte nicht eingefügt werden:\n{0}",
        ["Msg.PageN"] = "Seite {0}",
        ["Dialog.ChoosePages"] = "Seiten auswählen – {0}",
        ["Msg.PagesHint"] = "{0} Seiten · Klick auf eine Seite wählt sie an/ab",
        ["Msg.InsertOnePage"] = "1 Seite einfügen",
        ["Msg.InsertPages"] = "{0} Seiten einfügen",

        // ---- Formatvorlagen ----
        ["Style.Normal"] = "Standard",
        ["Style.NoSpacing"] = "Kein Abstand",
        ["Style.Heading1"] = "Überschrift 1",
        ["Style.Heading2"] = "Überschrift 2",
        ["Style.Heading3"] = "Überschrift 3",
        ["Style.Heading4"] = "Überschrift 4",
        ["Style.Title"] = "Titel",
        ["Style.Quote"] = "Zitat",
        ["Style.Header"] = "Kopfzeile",
        ["Style.Footer"] = "Fußzeile",
        ["Msg.HeadingTip"] = "{0} (Strg+Alt+{1}) – erscheint im Inhaltsverzeichnis",
        ["Ed.Status.Counts.Format"] = "Wörter: {0} · Zeichen: {1}",
        ["Msg.NoDictionary"] = "Für {0} ist in Windows kein Rechtschreib-Wörterbuch installiert – daher werden keine Fehler angestrichen. Sprache in den Windows-Einstellungen ergänzen: Zeit und Sprache → Sprache und Region → Sprache hinzufügen (inkl. Rechtschreibung).",

        // ---- Tabellen-Formatvorlagen ----
        ["TStyle.Plain"] = "Einfach (Raster)",
        ["TStyle.Blue"] = "Blau",
        ["TStyle.Teal"] = "Türkis",
        ["TStyle.Purple"] = "Lila",
        ["TStyle.Gray"] = "Grau",
        ["TStyle.Warm"] = "Warm (Gelb)",
        ["TStyle.Borderless"] = "Ohne Rahmen",
    };
}
