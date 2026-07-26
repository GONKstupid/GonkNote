# Gonk Note — Projektübergabe (Stand: 2026-07-23, Phase 3 laufend)

> **📌 Doku-Pflege (Nutzer-Wunsch, dauerhaft):** Wird das **README** aktualisiert,
> muss auch **Hilfe → Über Gonk Note** (`Views/AboutDialog`) mitgezogen werden. Das
> README wird dort zur Laufzeit als eingebettete Resource geladen → Textänderungen
> erscheinen automatisch; **manuell** zu pflegen ist nur die Versions-/Phasenzeile in
> `AboutDialog.xaml.cs` (`VersionText.Text`, aktuell „Phase 3") sowie ggf. Layout/Über-
> Texte im Dialog selbst. Kurz: README ändern ⇒ Über-Dialog gegenprüfen.
>
> **📌 Antwort-Stil (Nutzer-Wunsch, dauerhaft):** Zusammenfassungen/Rückmeldungen zu
> erledigter Arbeit **sehr kurz, stichpunktartig** halten (kein langer Fließtext, keine
> ausführlichen Tabellen als Standard). **Ausführlich nur** bei **offenen Fragen und
> Entscheidungen**, die der Nutzer treffen muss — die weiterhin klar begründen.
>
> **Runde 20 (2026-07-23) zuletzt:** **Avalonia-Port — Schritt 2 (Core) + Schritt 5 (Editor-Prototyp).**
> (1) Plattformneutrale Kernlogik (Models + DatabaseService + UndoStack + ImageCache) in ein echtes
> `net8.0`-Projekt `GonkNote.Core/` verschoben (`git mv`); WPF **und** Avalonia referenzieren es
> per `ProjectReference` (Avalonia-`<Compile Include>`-Links weg) — Details **§9.1b**. (2) Auf
> Nutzer-Wunsch den **Text-Editor-Ansatz prototypt** (Risiko-Teil): `Markdown.Avalonia` **rendert
> formatierten Rich-Text** (Screenshot verifiziert) → **Markdown-Editor (Ansatz b) empfohlen**;
> Entscheidungsmatrix + ehrlicher Feature-Preis in **§9.3b**. **Nutzer-Entscheidung: Editor-Bau
> zurückstellen** → stattdessen (3) **Shell-Baustein 3a gebaut** (§9.3c): Ordnerbaum aus der echten
> LiteDB mit **Farbvererbung** + Auswahl-Navigation + Theme-Umschalter (Avalonia-native VMs);
> per Screenshot verifiziert. Alle drei Projekte bauen grün. **Noch nicht committet.**
> Nächster Schritt: **Shell 3b** (Galerie/Tabs/Pins/Umbenennen/DnD) oder **Whiteboard (§9.4 Punkt 4)**.
>
> **Runde 19 (2026-07-23):** **Cross-Platform-Port (Linux) begonnen** — auf
> Nutzer-Wunsch vorgezogen. Neues Avalonia-Projekt `GonkNote.Avalonia/` (net8.0, kein Flutter)
> als PoC, das die echte Kernlogik (Models + `DatabaseService` + LiteDB) wiederverwendet und
> den Ordnerbaum lädt; WPF-App unangetastet. **Vollständiger Leitfaden für einen neuen Thread
> in §9** (Stand, Architektur, Reuse-Karte, gestaffelter Plan, Fallstricke). Außerdem: ein
> versehentlich (vom Nutzer) nach `Views/` verschobenes `Themes/Dark.xaml` zurückgestellt.
>
> **Fixes-Runde 18 (2026-07-23):**
> - **Rechtschreibprüfung: Erkennung + Sprach-Swap repariert.** Ursache: WPF vergibt die
>   Sprache (`xml:lang`) **pro Textabschnitt** anhand der Eingabesprache (Tastaturlayout) bzw.
>   übernimmt sie beim DOCX-Import → Teile wurden mit dem falschen Wörterbuch geprüft (Wörter
>   fälschlich/nicht angestrichen), und die Combo änderte nur `Editor.Language`, nicht die
>   vorhandenen Runs (Swap wirkungslos, WPF prüft laufenden Text bei Sprachwechsel nicht neu).
>   - Fix in `TextEditorView.xaml.cs`: neue `SetSpellLanguage(lang)` setzt die Sprache auf
>     **alle** Runs/Blöcke (`ApplyLanguageToBlocks`, rekursiv über Paragraph/Section/List/Table/
>     Span) und stößt die Prüfung neu an (`ForceSpellRecheck` = kurz SpellCheck aus/ein). Wird
>     beim **Sprachwechsel** und **beim Laden** (`LoadFromModel`) aufgerufen (Standard Deutsch).
>   - **Englisch braucht ein Windows-Wörterbuch.** Auf dem Testrechner ist nur **de-DE**
>     installiert (`Get-WinUserLanguageList` → 1 Sprache), daher zeigt Englisch keine Markierungen
>     – das ist eine OS-Grenze, kein App-Bug (WPF nutzt ab Win8.1 die Plattform-Rechtschreibung).
>     Neu `Services/SpellCheckSupport.cs` (COM `ISpellCheckerFactory.IsSupported`) prüft die
>     Verfügbarkeit; fehlt das Wörterbuch, erscheint in der Statusleiste ein **Warndreieck**
>     (`SpellLangWarn`) mit Tooltip (Sprache in den Windows-Einstellungen ergänzen).
>   - Verifiziert (geseedetes Test-Dokument dt./engl. mit Tippfehlern, `%TEMP%\gonk-titlebar`):
>     Deutsch markiert „Tset/Fehlren/Woertern" korrekt; Swap auf Englisch prüft neu (die zuvor
>     im Deutsch-Modus markierten englischen Wörter verschwinden) und blendet das Warndreieck ein.
>
> **Fixes-Runde 17 (2026-07-22):**
> - **Einblendbare Titelleiste im maximierten Fenster.** Im maximierten (titelleistenlosen)
>   Fenster gleitet bei Mauskontakt am oberen Rand eine eigene Titelleiste herein (Overlay-
>   Streifen `AutoTitleBar` in `MainWindow.xaml`, animiert per `TranslateTransform` +
>   `DoubleAnimation`, weil die **native** Leiste nicht animierbar ist). Enthält Gonk-Note-
>   Titel + Minimieren/Wiederherstellen/Schließen (Styles `CaptionButton`/`CaptionCloseButton`).
>   Logik in `MainWindow.xaml.cs`: `Root_MouseMove` (PreviewMouseMove; einblenden bei y≤12,
>   ausblenden bei y>48 – Hysterese gegen Flackern, großzügig wegen des ~7-px-Überstands),
>   nur aktiv wenn `WindowState==Maximized` (Umschaltung in `ApplyMaximizedChrome`).
>   Doppelklick auf den Streifen stellt wieder her. Verifiziert (Screenshot: Streifen gleitet
>   bei Maus oben herein).
> - **Big-Picture-Galeriemodus (an GoodNotes angelehnt).** Ist **kein Dokument offen**, zeigt
>   der Arbeitsbereich den Inhalt des aktuellen Ordners als große Kacheln (ersetzt den alten
>   „Willkommen"-Leerzustand): **farbige Ordner** (großes Symbol in Ordnerfarbe + Favoritenstern),
>   **Notizbuch-Cover als Vorschau** (Bild oder Farbverlauf+Titel), **Karten** für Whiteboard
>   (Punktraster) und Textdokument (Blatt mit Zeilen); je Kachel Name + Chevron-Menü + Datum.
>   Kopf mit Breadcrumb („Dokumente > …"), Zurück-Pfeil, großem Ordnertitel und „Neu"-Menü.
>   - Ordner im Baum wählen ⇒ Galerie zeigt seinen Inhalt (`SelectedTreeItem`-Setter →
>     `GalleryFolder`); Ordnerkachel/Breadcrumb/Zurück navigieren (`NavigateGallery`).
>   - Neu: `ViewModels/GalleryItemViewModel.cs` (+ `BreadcrumbEntry`), Galerie-Zustand/Befehle
>     in `MainViewModel` (`GalleryItems`, `Breadcrumb`, `GalleryOpen/Back/Navigate`,
>     `RebuildGallery` nach Anlegen/Löschen/Verschieben/Umbenennen/Favorit/Farbe).
>   - Cover werden **schlank** geladen: `DatabaseService.GetCover(id)` projiziert nur das
>     Cover-Feld (nicht alle Seiten) – wichtig fürs RAM. Kachelmenü/„Neu" laufen über Code-behind
>     (`ShowGalleryMenu`, `GalleryNew_Click`) bzw. gebundene Befehle.
>   - Verifiziert per geseedeter Test-DB (`%TEMP%\gonk-gallery-seed`, referenziert die echte
>     GonkNote.dll): Galerie rendert Ordner/Cover/Textkarte, Cover-Projektion lädt den gesetzten
>     Farbverlauf. **Ordner-Navigation (Breadcrumb/Zurück) und Dark Mode nur per Logik/Theme-
>     Brushes abgesichert, nicht klick-getestet.**
>   - **Bewusst v1:** Whiteboard/Text zeigen stilisierte Karten (kein echtes Inhalts-Thumbnail);
>     Favoritenstern nur für Ordner (Modell kennt Favoriten nur bei Ordnern). Echte
>     Seiten-Thumbnails wären ein späterer Ausbau über die `WhiteboardView.Draw*`-Routinen.
>
> **Fixes-Runde 16 (2026-07-22):**
> - **Titelleiste: folgt dem Theme + verschwindet beim Maximieren.** Das Fenster nutzt
>   weiterhin die native Windows-Titelleiste (kein Custom-Chrome).
>   - *Dark Mode:* Leiste wird dunkel via DWM-Attribut `DWMWA_USE_IMMERSIVE_DARK_MODE`
>     (neuer `Services/TitleBarTheme.cs`, `DwmSetWindowAttribute` + `SetWindowPos …|
>     FRAMECHANGED` als Repaint-Anstoß, sonst färbt sie sich erst beim Fokuswechsel).
>     Angewandt in `MainWindow.OnSourceInitialized` (Handle existiert erst dort) und bei
>     jedem `ThemeService.ThemeChanged`.
>   - *Maximiert = keine Titelleiste* (Nutzer-Wunsch, ersetzt die kurz gebaute F11-Variante):
>     `StateChanged` → `ApplyMaximizedChrome()` setzt `WindowStyle=None` bei Maximiert und
>     zurück auf `SingleBorderWindow` bei Normal (danach Titelfarbe neu setzen).
>     **Zurück ins Fenster:** Doppelklick auf die Menüleiste (`TopBar_MouseLeftButtonDown`,
>     Klicks auf Menü/Buttons ausgenommen) oder Windows-Standard (Win+ab/Taskleiste).
>   - *Randlos-Maximier-Größe:* `Services/WindowBounds.cs` klemmt via WM_GETMINMAXINFO-Hook
>     (`HwndSource.AddHook` in `OnSourceInitialized`, `handled=true`) auf den Monitor-
>     Arbeitsbereich → Taskleiste bleibt sichtbar. **Grenze:** ist der Arbeitsbereich = volle
>     Monitorgröße (ausgeblendete Taskleiste), legt WPF noch ~7 DIP Rahmenüberstand an;
>     bei den 10-px-Rändern der App schneidet das keinen sichtbaren Inhalt ab (geprüft).
>   - Verifiziert per echtem Screen-Capture + GetWindowRect-Messung (`%TEMP%\gonk-titlebar`):
>     Light hell, Dark dunkel, maximiert ohne Leiste, wiederhergestellt mit Leiste. Achtung:
>     Testrechner läuft 200 % DPI + Taskleiste auto-ausgeblendet (DPI-Stolperfalle §7).
>     README aktualisiert (Über-Dialog zieht nach).
>
> **Fixes-Runde 15 (2026-07-21):**
> - **Cover-Vorlagen: 3. Kategorie „Pixel Art".** Neun fertige Pixel-Art-Cover des
>   Nutzers (`Assets\Covers\Pixel Art\PA (1..9).png`) sind als eigene Galerie-Gruppe
>   in der Cover-Sektion verfügbar. **Kein neuer View-Code nötig** – die Gruppen werden
>   in `WhiteboardView.Covers.cs` generisch aus den Unterordnern von `Assets\Covers`
>   erzeugt (alphabetisch → Basic, Muster, Pixel Art) und `.png` ist in `StickerExts`
>   ohnehin erlaubt. Einziger Baustein: die csproj kopierte bisher nur `Covers\**\*.jpg`
>   neben die Exe; jetzt zusätzlich `Covers\**\*.png` (`Content … PreserveNewest`),
>   damit die PNGs im Build-Output/Single-File-Publish landen. Verifiziert: Build grün,
>   alle 9 PNGs liegen neben der Debug-Exe. In-App-Klick nicht durchgespielt (Render-
>   Pfad identisch zu Basic/Muster).
>
> **Fixes-Runde 14 (2026-07-19):**
> - **Zahlenblock für die Strichstärke (Vorbild Adobe Fresco).** Langes Drücken auf
>   den Größen-Slider (`WidthSlider`, ~500 ms, Finger/Stift/Maus) – oder ein Klick auf
>   die Wertanzeige daneben (jetzt ein Button) – öffnet ein **Numpad-Popup**
>   (`SizeNumpad`) im App-Stil: Display + 7-8-9/4-5-6/1-2-3/0-,-⌫. Eingabe wird live
>   auf den Slider (1–20) geclampt und angewandt. Code: `Views/WhiteboardView.Numpad.cs`
>   (Langdruck-Timer + Numpad-Logik), Style `NumpadKey` in `WhiteboardView.xaml`.
>   `WidthLabel` ist jetzt ein Button (Content statt Text – `WidthSlider_ValueChanged`
>   angepasst). README/Über-Dialog aktualisiert (s. o.).
>   **Nachtrag:** Auslöser jetzt auch das **Strichstärke-Icon** (`WidthIcon`, Langdruck),
>   nicht nur Slider/Wert. Popup ist `StaysOpen=True` und schließt **nur bei Klick klar
>   außerhalb** (eigener `PreviewMouseDown`/`PreviewTouchDown`-Handler; `IsSizeTrigger`
>   nimmt Slider/Icon/Wert aus) – Klicks im Popup oder auf die Größen-Steuerelemente
>   lassen es offen. Verifiziert (Slider-Klick → bleibt offen, Canvas-Klick → schließt).
>
> **Fixes-Runde 13 (2026-07-19):**
> - **Ordner-Tree: Farbvererbung.** Elemente/Unterordner ohne eigene (händisch
>   gesetzte) Symbolfarbe erben automatisch die Farbe des übergeordneten Ordners
>   (rekursiv). `IconColor` = manuell (bleibt), sonst geerbt. Berechnung zentral in
>   `MainViewModel.ApplyInheritedColors()` (nicht persistiert → Ordner-Farbwechsel
>   schlägt sofort auf alle Nachkommen durch); aufgerufen nach Baumaufbau, Farbwahl,
>   Move/Copy, Neuanlage, Import. `TreeItemViewModel.InheritedColorHex` +
>   `IconBrush = IconColor ?? InheritedColorHex ?? Türkis`. Kontextmenü „Standard" →
>   **„Automatisch (Ordnerfarbe)"**. Verifiziert (Screenshot: rotes/grünes Kind erbt,
>   blaues Kind manuell bleibt, verschachtelter Unterordner erbt).
> - **Tabellen-Tab aufgeräumt + Auslagerung.** Der Kontext-Tab „Tabelle" ist jetzt
>   eine schlanke Zeile: Zeile ▾ / Spalte ▾ (Sammel-Dropdowns), Verbinden / Teilen ▾,
>   Sortieren / Formel / In Text, **„Design & Rahmen…"** (öffnet Seitenleiste),
>   Tabelle löschen. Design/Rahmen/Füllung/Größe (Formatvorlage-Combo, Kopf-/
>   Ergebniszeile + Bänder als Checkboxen, Rahmen, Füllfarbe, Spaltenbreite,
>   AutoAnpassen, Zellenränder) liegen in der Seitenleisten-Sektion „Tabelle"
>   (`SecTable`). Handler unverändert. Verifiziert (Screenshots Tab + Seitenleiste).
>
> **Fixes-Runde 12 (2026-07-19):**
> - **Tabellenfunktion komplett neu (nach Word-Vorbild, Nutzer-Vorgabe):** Die alte
>   Sidebar-Sektion „Tabelle" ist raus; stattdessen gibt es einen **Kontext-Ribbon-Tab
>   „Tabelle"** (wie Words Tabellentools), der nur erscheint, wenn der Cursor in einer
>   Tabelle steht (`UpdateTableRibbon`). Einfügen jetzt über **Hover-Raster** (8×10, wie
>   Word) + Dialog + **Text↔Tabelle** + **Schnelltabellen** (Kalender, Liste). Neu im Tab:
>   Zeilen/Spalten (einf./lösch.), **Verbinden/Verbund aufheben/Zelle teilen/Tabelle
>   teilen**, Spaltenbreite, **AutoAnpassen** (Inhalt/Seitenbreite/fest), **Zellenränder**,
>   **Sortieren** (Text/Zahl/Datum, `TableSortDialog`), **Formeln** (=SUMME/MITTELWERT/
>   MIN/MAX/ANZAHL/PRODUKT über ABOVE/BELOW/LEFT/RIGHT), **In Text umwandeln**,
>   **Formatvorlagen** (7 Farbschemata + Kopfzeile/Ergebniszeile/Zeilen-/Spaltenbänder,
>   im `Table.Tag` gemerkt), Rahmen/Füllung auf die Auswahl. Logik in
>   `Views/TextEditorView.Table.cs` (+ `.Insert.cs` Grundwerkzeuge). Verifiziert im echten
>   Fenster (Raster → 4×3 eingefügt, Kontext-Tab erscheint, Schnelltabelle mit Kopfzeile).
>   **Bewusste WPF-Grenzen** (Info-i im Tab): nur durchgezogene Zellränder, keine
>   senkrechte Zellen-Ausrichtung/feste Zeilenhöhe, keine Excel-Einbettung, keine
>   Kopfzeilen-Wiederholung beim Seitenumbruch, kein Textumfluss um die Tabelle.
>   Vorgaben-Datei `TestAssets/tabellenfunktion.md` nach Umsetzung gelöscht.
>
> **Fixes-Runde 11 (2026-07-18):**
> - **Notizbuch-Performance:** Der Seiten- und Notizzettel-Schatten lief als
>   `SKImageFilter.CreateBlur` **pro Frame** — Kosten skalieren mit der sichtbaren
>   Pixelfläche (Notizbuch-Seite = ganzes Fenster; bei hohem Zoom zusätzlich riesige
>   Blur-Sigma in Gerätepixeln). Genau das machte das Notizbuch träge und alle
>   Werkzeuge bei 218 %/575 % Zoom verzögert (Whiteboard = unendliche Fläche ohne
>   Seitenschatten → war nie betroffen). Jetzt: Schatten einmal klein rendern und als
>   **gecachtes Nine-Patch** dehnen (`DrawCachedShadow`/`ShadowNinePatch` in
>   `WhiteboardView.xaml.cs`) — konstante Kosten, identische Optik (Screenshots
>   Notizbuch-Seite + Zettel geprüft). **Latenz-Verbesserung muss der Nutzer mit
>   Stift/Zoom bestätigen.**
> - **Text-Editor:** Seitenleiste heißt **„Erweiterte Einstellungen"**; es ist immer
>   nur die per Button geöffnete Sektion sichtbar (Ränder/Absätze/Hintergrundbild über
>   den Layout-Tab, Tabelle übers Rechtsklick-Menü „Erweiterte Einstellungen" —
>   umbenannt von „Rahmen/Zellen formatieren…"). `_activeSection` in `.Layout.cs`;
>   Tabellen-Sektion schließt, wenn der Cursor die Tabelle verlässt.
> - **Tabelle:** Zellen verbinden jetzt **auch senkrecht/rechteckig** (RowSpan;
>   `GridPositions`-Belegungsraster + Rechteck-Erweiterung in `.Insert.cs`; Aufheben
>   füllt Zeilen darunter wieder auf). Rahmenfarbe + Spaltenbreite wirken nur noch auf
>   die **ausgewählten Zellen/Spalten**. „Füllung weg" → **„Füllfarbe löschen"**.
>   DOCX-Export/-Import können vMerge (Zeilenverbund) jetzt in beide Richtungen.
> - **Diagramm (alle Arbeitsbereiche):** hinter den Farbkacheln gibt es eine
>   **„+"-Kachel** → Farbwähler → neue Farbe wird angehängt, beliebig oft; Rechtsklick
>   auf eine Kachel → **„Farbe löschen"** (mind. eine bleibt). Palette ist statisch
>   (Sitzung) und gilt für Text-Editor/Whiteboard/Notizbuch (`ChartDialog`).
> - **Cover-Vorlagen (Notizbuch):** 62 Nutzer-Motive (19 „Basic", 34 „Muster",
>   9 „Pixel Art") als Galerie in der Cover-Sektion der Einstellungs-Leiste; jede
>   Gruppe ist ein eigener zuklappbarer Expander (Kacheln laden lazy beim Aufklappen,
>   `WhiteboardView.Covers.cs` + `CoverPresetHost` im XAML). Klick = Bild-Cover über
>   den bestehenden Weg (`CoverStyle.Image` → DB). Auslieferung wie Sticker:
>   `Assets\Covers\<Gruppe>\*.jpg` neben der Exe (aus den A4-Originalen auf 1600 px
>   JPEG q85 konvertiert, 296 MB → 8,9 MB; A3-Duplikate verworfen — gleiches Motiv,
>   gleiches Seitenverhältnis, Cover wird eh mittig beschnitten). Eigene Vorlagen:
>   `%APPDATA%\GonkNote\Covers` — erscheint als stets vorhandene Gruppe
>   **„Individuell"** mit **„+"-Kachel** zum Hochladen (Dateidialog, Multiselect,
>   Kopie nach %APPDATA%, kollisionssicher wie Sticker; Unterordner = eigene Gruppen);
>   eigene Kacheln per Rechtsklick → **„Vorlage löschen"** (nur %APPDATA%-Dateien,
>   mitgelieferte Motive nicht löschbar). Der Quell-Ordner
>   `Assets\Notizbuch-Cover-Presets` wurde nach der Konvertierung gelöscht (Nutzer-OK).
>   End-to-end verifiziert (Galerie rendert, Klick setzt Cover, Screenshot).
> - **Quick-Menü per Langdruck:** ~600 ms an derselben Stelle gedrückt halten
>   (Stift **oder** Finger, Toleranz 10 px) öffnet die Schnellaktionen — **nur** bei
>   Lasso (L)/Verschieben (V)/Hand (H), damit Zeichenwerkzeuge ungestört bleiben.
>   `StartHoldDetect`/`HoldTimer_Tick` in `WhiteboardView.xaml.cs`; bricht die
>   angefangene Interaktion ab (`_suppressNextEndInput` fürs Stift-Up, `_touches`-
>   Clear für Finger-Pan). Touch-Tipps auf die Leiste laufen am Canvas vorbei
>   (`IsOnQuickMenu`-Guards auch in den Touch-Handlern). **Nutzer-Test mit
>   Stift-/Touch-Hardware steht aus.**
>
> **Runde 10 (2026-07-17):** **OCR mit Tesseract** (Details §5/§6):
> Kontextaktion „Text erkennen (OCR)" auf Bildern/PDF-Seiten, Ergebnis-Dialog
> (kopieren / als Notizzettel). **Rechtsklick-Menü im Whiteboard/Notizbuch
> ersetzt** durch ein **Quick-Options-Menü im Toolbar-Look** (floatende Icon-Leiste,
> `WhiteboardView.xaml` `QuickMenu`): öffnet per Rechtsklick, **zweiter Stift-Taste**
> (Barrel-Button, direkt + `RightTap`-Fallback) **und automatisch nach einer Auswahl
> mit Lasso (L)/Verschieben (V)**. Icons: Ausschneiden/Kopieren/Duplizieren/Einfügen ·
> OCR · Löschen/Alles auswählen. **Stift-Fixes vom Nutzer mit Hardware bestätigt**
> (Klicks auf die Leiste + zweite Taste funktionieren).
>
> **Fixes-Runde 9 (2026-07-16):** Formen aus dem Text-Editor
> **entfernt** (voll interaktive Office-Formen im FlowDocument nicht sinnvoll
> machbar → Zeichnen ist Whiteboard-Sache). **Diagramm** stark erweitert: Typen
> Säulen/Balken/Linie/Punkt/Punkt+Linie/Kuchen/Radar + **mehrere Reihen** (eine
> Zeile je Kurve) + Legende. **Tabelle**: Struktur zurück ins Rechtsklick-Menü,
> Rahmen wirken nur auf die **ausgewählten Zellen**, Sidebar-UI aufgeräumt.
> **Whiteboard/Notizbuch**: Rechtsklick-**Kontextmenü** (Ausschneiden/Kopieren/
> Duplizieren/Einfügen/Löschen/Alles auswählen) für keyboard-freie Nutzung, mit
> interner Element-Zwischenablage. Alle Commits `…`→HEAD, Build 0 Warnungen.
>
> **⇒ Aktueller Arbeitsmodus:** Der Nutzer **testet die App ausführlich** und schickt
> laufend kleine Änderungs-/Fix-Wünsche — die zuerst erledigen. **Danach in dieser Reihenfolge:
> Code-Cleanup → zweite Sprache (DE/EN) → RAM-Optimierung → GitHub-Veröffentlichung (MIT)**
> (Leitlinie & Schwellenwerte in §5, Ablauf in §6).


Diese Datei ist für den Einstieg in einen **neuen Chat-Thread** gedacht. Sie fasst
zusammen, was existiert, was als Nächstes ansteht, und wie gearbeitet werden soll.
Wenn du diesen Thread eröffnest, sag einfach: *"Lies HANDOFF.md und mach weiter."*

---

## 1. Was ist Gonk Note

Offline-Notiz-App für Windows 11 als Alternative zu GoodNotes/Apple Notes.
Kernanforderungen des Nutzers (unverändert gültig):

- **Plattform**: Windows 11, komplett offline, keine PWA
- **Single-File-Exe**, kein Installer, keine Adminrechte
- **RAM-Ziel**: < 200 MB im Normalbetrieb (noch nicht optimiert, siehe §5)
- **Stylus-first**: Wacom/Microsoft Pen mit Druckstärke, Finger = Gesten
- **Architektur-Entscheidung des Nutzers**: **WPF** (nicht WinUI 3), .NET 8
- **Drei Dokumenttypen**: Notizbuch, Whiteboard, Textdokument — je eigener Tab
- Import: **Bilder ✔ / DOCX ✔ / PDF ✔** · Export: **PDF ✔ / DOCX ✔ / Markdown ✔ / PNG ✔**
- Dark/Light-Mode fürs App-Design; **Seiten/Schreibflächen standardmäßig hell**

Arbeitsweise: **in Phasen**, polished/alltagstauglich, Nutzer-Feedback wird
**laufend** eingearbeitet (kommt in großen Batches, siehe Runden 1–4). Sprache
durchgehend Deutsch (UI, Kommentare, Commits). Ehrliches Scoping statt Übercommit.

---

## 2. Repo-Status

- Pfad: `C:\Dev\Zed\gonk-note`, Branch `main`, sauber committet
- Build: `dotnet build` fehlerfrei (0 Warnungen)
- Commit-Historie (neueste zuerst):
  - **Fixes-Runde 7 (2026-07-15)** – große Wunschliste des Nutzers abgearbeitet
    (Ordner `Änderungen` war die Quelle, danach gelöscht):
    - `d68308e` HANDOFF + Ordner entfernt
    - `388eed5` **Batch C 2/2**: Auswahl skalieren (alle Objekttypen, Undo) +
      **Sticker-Werkzeug** mit Sammlung (`Assets/Stickers` + `%APPDATA%\GonkNote\Stickers`)
    - `fdcb443` **Batch C 1/2**: „Hand (H)" (umbenannt) + neues **Verschieben (V)**-Tool
      (Direktauswahl), Notizzettel in der Auswahl, Lasso wählt nur ~95 %-umschlossene Objekte
    - `4e79516` **Batch B**: Einstellungs-Seitenleiste (Text-Editor, rechts), Preset-Kacheln
      auf 3 + Flyout, Layout-Tab entschlackt (Ränder/Abstände/Hintergrundbild → Seitenleiste)
    - `5a4b1e3` **Batch A**: Listen-Dropdowns repariert (waren ausgegraut), Format-Painter-Fix,
      Zeilenabstand→Layout, Link/Beschriftung nur unter Verweise, Rechtschreib-Vorschläge im
      Kontextmenü, **Formen-Palette + Diagramm-Tool** im Einfügen-Tab
    - `c602d84` **Datei-Einfüge-Tool**: Seitenauswahl-Dialog für PDF **und DOCX**
    - `6a0a30b` Geodreieck als Nutzer-**SVG-Asset** (Light/Dark) statt Code-Zeichnung
    - `416d705` **Fix**: Crash beim Öffnen von Textdokumenten (LiteDB EmptyStringToNull)
    - `ad0ba9f` Feedback-Runde 6 (Text-Editor-Feinschliff) · `91d197f` Text-Editor-Großausbau
      (Ribbon-UI nach `Docs/Design-Konzept-Text-Editor.md` + Word-Funktionen aus
      `Docs/word-funktionen-liste.md`)
  - **Phase 3 – Zeichenhilfen (Lineal/Geodreieck)**:
    - `8d4284e` Geodreieck-Overlay: korrekte Relationen nach Vorlage
    - `cd0f8f7` Geodreieck-Overlay 1:1 nach echtem Vorbild (Akzentfarbe)
    - `9c24184` eine Toolbar-Gruppe (wie Stifte) + neue Icons + volles Overlay
    - `a061985` Lineal um Winkelanzeige/-rastung (15°) erweitert + Geodreieck
    - `3e63c10` Lineal (Zeichen-Hilfsmittel) mit Kanten-Einrasten
    - ⚠️ **Geodreieck rendert, funktioniert aber im Betrieb noch nicht** (s. o.)
  - **Phase 3 – Notizzettel**: `4b10f9e` Notizzettel/Sticker aufs Whiteboard
    (funktioniert: gerendert + DB-Persistenz + im echten Binary verifiziert)
  - `41e14c6` Ordnername als Tooltip beim Hovern über Pin-Kacheln
  - `8f105cf` **Fix**: PDF-Export von Whiteboards/Notizbüchern wieder scharf
    (Skia re-komprimierte Bilder → jetzt `EncodingQuality=100` + Original-Bytes
    via `SKImage.FromEncodedData`; wirkt auf Bestandsdokumente ohne Neu-Import)
  - `79f0fd2` Übergabe-Doku auf Stand Feedback-Runde 5
  - `3cf5372` **Feedback-Runde 5**: Export-Qualität gefixt (Text-PDF scharf),
    PNG-Export, Zen-Style-Pin-Kacheln, Tree-Einfachklick öffnet/Doppelklick
    benennt um, Einstellungs-Accordion (Formen nur bei Formwerkzeug)
  - `f743db5` **Phase 2.4**: Export – PDF (alle Typen), DOCX + Markdown (Text)
  - `618f6fc` **Phase 2.3+**: PDF-Import performanter (Viewport-Culling, async +
    Fortschritt), Whiteboard 2-spaltig, **ein** Import-Button für Bild+PDF
  - `6a1c8b6` Phase 2.3: PDF-Import (Grundfunktion) in Notizbücher & Whiteboards
  - `5b745f2` Phase 2.2: DOCX-Import
  - `34eed2e` Feedback-Runde 3: Anpinnen/Favoriten, Touch-Gesten, Text-Optionen,
    Schwarz/Hell-Defaults, Über-Dialog mit README
  - `6e0da2e` Phase 2.1: Bilder-Import + Feedback-Runde 2 (Formen-Stift,
    punktgenauer Radierer, Einstellungs-Seitenleiste, anpassbares Cover)
  - `638bb73` Phase 1 · `6a921d9` App-Icon · `64b8129` Feedback-Runde 1

**Vor jedem Build**: laufende Testinstanz beenden (`taskkill //IM GonkNote.exe //F`),
sonst Datei-Lock-Fehler (kein Code-Problem).

---

## 3. Architektur-Überblick

```
GonkNote/
├─ App.xaml(.cs)              Einstieg, Theme-Init, "--db <pfad>"-Argument
├─ MainWindow.xaml(.cs)       Menü (inkl. DOCX-Import), Seitenleiste mit
│                             Schnellzugriff (angepinnte Ordner), Ordnerbaum
│                             (Drag&Drop, Favoriten-Stern), Tab-Host, Über-Dialog
├─ Models/
│  ├─ NoteItem.cs             Baum-Eintrag inkl. IconColor, IsPinned, IsFavorite
│  └─ Whiteboard.cs           WbPage (inkl. BackgroundImage/-Id für PDF-Seiten),
│                             Elemente (Stroke/Shape/Text/Image/**StickyNote**),
│                             `IBoxElement` (Bild+Zettel: Pos+Größe für Resize),
│                             Enums (ToolType inkl. **Sticky**), CoverStyle,
│                             WhiteboardDoc, PageTemplate, TextDoc
├─ ViewModels/                Mvvm-Basis, MainViewModel (Baum, Tabs, Autosave 30s,
│                             Pin/Favorit, DOCX-Import), TreeItemViewModel, Tab-VMs
├─ Views/
│  ├─ WhiteboardView.xaml(.cs)   SkiaSharp-Canvas: Werkzeuge (Stifte-Gruppe klappbar),
│  │      + .Stickers.cs          Formen-Stift-Erkennung, punktgenauer Radierer,
│  │        (partial)             Bild-/PDF-/DOCX-Import (ein Button, Paste, DnD),
│  │                             Verschieben (V)=Direktauswahl + Lasso (L, nur ~95 %),
│  │                             Auswahl skalieren (alle Objekttypen), Hand (H)=Pan,
│  │                             Sticker-Werkzeug (.Stickers.cs), Touch-Gesten,
│  │                             Einstellungs-Seitenleiste rechts (Seite/Formen/Text/
│  │                             Sticker/Cover), Undo/Redo, Zoom/Pan, Seiten, Cover,
│  │                             Viewport-Culling, Busy-Overlay
│  ├─ TextEditorView.xaml(.cs)   Text-Editor im Ribbon-Layout (Tabs Start/Einfügen/
│  │      + .Format/.Insert/      Layout/Verweise), rechte Einstellungs-Seitenleiste
│  │        .Layout/.Refs/        (Ränder/Absätze/Hintergrundbild), Listen-Split-Buttons
│  │        .Find/.Lists/         + Bibliotheken (.Lists.cs), Diagramm-Werkzeug
│  │        .Shapes.cs (partial)  (.Shapes.cs → ChartDialog), Formatvorlagen-Galerie (nur
│  │                              3 Kacheln inline + Flyout; Überschrift 1–4 farbig,
│  │                              Erkennung über Größe+Gewicht → TOC/Navigator/DOCX-Styles),
│  │                              Seiteneinrichtung (A4/A5/A3/
│  │                              Letter, Hoch/Quer, Ränder in cm inkl. „Lernblatt“
│  │                              4 cm links), Kopf-/Fußzeile ({SEITE}/{SEITEN}/{DATUM}/
│  │                              {TITEL}), Wasserzeichen, Inhaltsverzeichnis, Format
│  │                              übertragen, Tabellen-Werkzeuge (Kontextmenü), Links,
│  │                              Sonderzeichen, Beschriftungen, Lineal, Statusleiste
│  │                              (Wörter/Sprache/Rechtschreibung/Zoom), Navigator,
│  │                              Seitenumbruch-Marken (Layout-Tab, Näherung).
│  │                              Seite bleibt in beiden Themes weiß (Nutzer-Wunsch
│  │                              Runde 6); Ink-Normalisierung repariert Altbestände
│  ├─ HeaderFooterDialog / PromptDialog   Kopf-/Fußzeile bzw. generische Eingabe
│  ├─ ColorPickerDialog.xaml(.cs) HSV-Farbrad + Hex + Alpha
│  ├─ AboutDialog.xaml(.cs)      Version + eingebettetes README (scrollbar)
│  ├─ PageSetupDialog / TableSizeDialog / Converters
├─ Services/
│  ├─ DatabaseService.cs      LiteDB (items/boards/texts/settings), --db-fähig
│  ├─ DocxImporter.cs         DOCX → FlowDocument → XamlPackage
│  ├─ DocxExporter.cs         FlowDocument → DOCX (OpenXML, Gegenrichtung)
│  ├─ MarkdownExporter.cs     FlowDocument → Markdown (best-effort)
│  ├─ PdfImporter.cs          PDF → JPEG-Seiten via Docnet.Core/PDFium
│  ├─ PdfExporter.cs          Whiteboard→SKDocument, Text→Paginator-Raster→PDF
│  ├─ ImageCache.cs           Byte-Budget-Cache (96 MB) dekodierter Bilder
│  ├─ TextStyles.cs           Zentrale Formatvorlagen/Seitenformate/Heading-Erkennung/
│  │                          Ink-Normalisierung des Text-Editors (eine Wahrheit für
│  │                          Editor, TOC, PDF-/DOCX-Export, Import, Markdown)
│  ├─ ThemeService.cs, UndoStack.cs (PartialErase/ResizeImage-Actions)
├─ Themes/                    Light/Dark.xaml, Styles.xaml (inkl. Vektor-Icons)
└─ TestAssets/               testdokument.pdf (20 Seiten, gitignored) für UI-Tests
```

Pakete: LiteDB, SkiaSharp.Views.WPF, Svg.Skia (SVG-Rasterung),
DocumentFormat.OpenXml (DOCX), **Docnet.Core** (PDFium-Rendering).

**Wichtige Eigenheiten:**
- **LiteDB-Stolperfalle**: `BsonMapper.Global.EmptyStringToNull` ist bei LiteDB
  standardmäßig **true** → leere Strings werden als BSON-Null gespeichert und
  kommen als `null` zurück (hat den Crash beim Öffnen von Textdokumenten mit
  leerer Kopf-/Fußzeile verursacht). Seit dem Fix: in `DatabaseService` auf
  false gesetzt **und** String-Properties in `TextDoc` mit null-sicheren
  Settern. Bei neuen String-Feldern in Modellen daran denken!
- **PDF-Import** (Nutzer-Entscheidung: Bild-Seiten, keine Text-Extraktion):
  `PdfImporter.RenderPages` rendert jede Seite als JPEG (lange Kante 2246 px ≈
  200 % A4@96dpi). Läuft **asynchron** (`InsertPdfFileAsync` + `Task.Run`),
  UI bleibt bedienbar, **Busy-Overlay** mit Fortschritt (Seite X/Y). Ziel-Seite/
  -Doc werden vor dem `await` festgehalten (Tabwechsel-sicher).
  - *Notizbuch*: jede PDF-Seite → neue `WbPage` mit `BackgroundImage` (füllt die
    Seite, ersetzt das Muster, ist weder verschieb- noch radierbar, Seiten-
    verhältnis erhalten, lange Kante = A4-Höhe). Zum Draufschreiben/Markieren.
  - *Whiteboard*: Seiten als `ImageElement` **zweispaltig** (s1 s2 / s3 s4 …),
    direkt ausgewählt, per Lasso verschieb-/skalierbar.
- **Viewport-Culling** in `Skia_PaintSurface`: nur Elemente im sichtbaren Bereich
  (`VisibleCanvasRect` + `ElementBounds`) werden gezeichnet. **Kritisch** gegen
  Lag bei vielen Bildern — sonst wird jedes Frame jedes Bild neu dekodiert.
- `ImageCache` mit **Byte-Budget** (96 MB, LRU) statt Stückzahl. Rendering von
  Whiteboard-Bildern und PDF-Seiten-Hintergründen läuft darüber.
- **Ein Import-Button** in der Whiteboard-Toolbar (`InsertFile_Click`): Dateidialog
  mit kombiniertem Filter (Bilder+PDF), Dispatch nach Endung. Bilder auch per
  Strg+V; Bild **und** PDF per Drag&Drop (`CanvasHost_Drop`, async).
- Formen-Stift (`G`): Douglas-Peucker-Eckenerkennung, Sehnenabweichung für Geraden
  (45°-Einrasten), Ellipsen-Fit; Fallback = geglättete Kurve.
- Radierer trennt Strokes an der Berührstelle auf (`SplitStroke` + `PartialEraseAction`).
- Touch: rohe Touch-Events (1 Finger Pan, 2 Finger Pinch+Pan, 3-Finger-Doppeltipp
  Undo); Stylus-Events ignorieren Touch-Geräte. **Nur per Code verifiziert — kein
  Touchscreen im Test.**
- Farb-Tags: `_colorTag` "auto" = Schwarz (auf dunklen Seiten hell); Checked-Handler
  gegen leere Tags abgesichert (es gab einen Null-Farben-Bug).
- **Export** (`MainViewModel.ExportActiveTab`): Text → PDF/DOCX/Markdown/PNG,
  Whiteboard/Notizbuch → PDF/PNG. Text-PDF wird über den WPF-Paginator direkt in
  ein `RenderTargetBitmap` gerendert (3×/288 DPI, PagePadding) und als Bild ins
  PDF gelegt → scharf. Whiteboard-PDF/PNG rendert vektorbasiert über dieselben
  Zeichenroutinen (`WhiteboardView.Draw*` sind dafür `internal static`).
- **Ordnerbaum-Interaktion**: Einfachklick öffnet Dokumente (Ordner werden nur
  ausgewählt, Aufklappen per Pfeil), Doppelklick startet Umbenennen. Logik in
  `MainWindow.Tree_PreviewMouseLeftButtonUp` (Einfachklick, nur `!IsFolder`) und
  `Tree_MouseDoubleClick` (Umbenennen).
- **Angepinnte Ordner**: kompaktes Icon-Kachel-Raster (WrapPanel aus
  Button-Kacheln mit eigenem Template) in `MainWindow.xaml`, Zen-Browser-Stil.
- **Einstellungs-Seitenleiste**: ausklappbare `Expander`-Sektionen (Style in
  `Styles.xaml`); `ShapeSection` nur sichtbar bei aktivem Formen-Werkzeug
  (`RefreshSettingsPanel` setzt die Sichtbarkeit).

---

## 4. Feedback-Stand

**Runden 1–6 + Phase-3-Anfang umgesetzt und committet.** (Runde 6 = Text-Editor-
Feinschliff: weiße Seite in beiden Themes, Navigator-Kontrast, Sammel-Buttons/
WrapPanel in der Toolbar, Schriftarten-Vorschau, Seitenumbruch-Marken.)

**Geodreieck — jetzt SVG-Asset des Nutzers (2026-07-14, Nutzer-Test steht aus):**
Der Nutzer hat eigene SVGs geliefert; die Code-Zeichnung wurde durch reines
SVG-Rendering ersetzt (`WhiteboardView.DrawSetSquare` via Svg.Skia):
- Assets: `Assets/Geodreieck-Light.svg` / `-Dark.svg` (EmbeddedResource; einziger
  Unterschied ist die Bandfarbe Lila/Pink, gecacht je Theme, Wechsel greift sofort).
- Vermessene SVG-Geometrie: viewBox 2520×1680, Hypotenuse 2515,2 units =
  **16-cm-Geodreieck** → 157,2 units/cm; Hypotenusen-Mittelpunkt (1259,85|1468,85)
  wird auf das Interaktionszentrum gelegt, Skalierung 1 Geodreieck-cm = 1 Seiten-cm.
  `SsHalfHyp = 8 cm` — Einrast-Polygon und Optik decken sich exakt (per Harness
  mit übergelegtem rotem Polygon in 0° und 25° verifiziert).
- Fallback: fehlt/bricht die Ressource, wird eine schlichte Glas-Kontur gezeichnet.
- Harness: `%TEMP%\gonk-texttest` Modus `geo` (ruft echten Code aus GonkNote.dll),
  Modus `svg` rastert die SVG-Dateien direkt. In-App-Test: `ui-geotest.ps1`
  (öffnet Whiteboard, Kurzbefehl D, PrintWindow-Screenshot).
Interaktion (Bewegen/Drehen/Einrasten) ist unverändert; ob das frühere
„funktioniert nicht" an der Optik lag oder an der Interaktion, klärt der
Nutzer-Test mit Stift.

**Funktioniert (verifiziert):** PDF-Export-Schärfe (Docnet-Render geprüft), Notizzettel
(im echten Binary aus geseedeter DB geladen + gerendert), Pin-Tooltip, **Lineal**
(Harness: Kanten-Einrasten, Winkelanzeige, 15°-Rastung — Stylus-Feel vom Nutzer als
„funktioniert super" bestätigt).

**Noch keine Praxis-Rückmeldung** zu Formen-Stift/Touch auf echtem Gerät.

---

## 5. Bekannte Lücken / bewusst vertagt

**✔ Fixes-Runde 8 (2026-07-16) umgesetzt** (Commits `13413cb`…dieser):
- *Whiteboard:* Toolbar neu geordnet (Hand→Stift→Radierer→Lasso→Rest); Verschieben
  (V)-Icon jetzt hohler Mauszeiger; Lasso+Verschieben als klappbare Gruppe (wie
  Stifte). **Rotieren** neu umgesetzt (`WbElement.Rotation`, rotationsfähiges
  Rendering/Hit-Test/Export, Dreh-Griff mit 15°-Rastung, `RotateElementAction`);
  **Skalieren** vereinheitlicht (Einzel-/Mehrfachauswahl, mitgedrehte Auswahlbox).
- *Text-Editor:* Auflistungs-Bibliotheken mit festem dunklem Text auf weißer Karte
  (Kontrast-Bug behoben) + Word-artige Vorschau. Neue Presets Titel/Zitat/Kopf-/
  Fußzeile. Diagramme/Bilder per Kontextmenü in der Größe änderbar; Diagramm-
  farben im Dialog wählbar; Objekt „hinter den Text" (Figure + reduzierte Deckkraft).
  Tabellen-Formatierung in einer eigenen Seitenleisten-Sektion (Struktur, Rahmen-
  dicke/-variante, Farben) statt im Rechtsklick-Menü.
- **Formen aus dem Text-Editor entfernt (2026-07-16):** Voll interaktive Office-
  Objekte (freies Ziehen/Drehen mit Umfluss) sind im WPF-FlowDocument nur mit einem
  großen, CPU-hungrigen Overlay-/Adorner-System machbar (Text-Relayout pro Frame) —
  bewusst nicht gebaut. Das interaktive Zeichnen bietet das Whiteboard. Die statische
  Formen-Palette wurde daher auf Nutzerwunsch komplett entfernt (Diagramme/Bilder
  bleiben). `.Shapes.cs` enthält nur noch das Diagramm-Werkzeug.

**⚠️ Bewusst vertagt (nicht vergessen):**
- **Gedrehte Elemente – Feinschliff:** Auswahlbox/Hit-Test gedrehter Box-Elemente
  nutzen die achsenparallele Bounding-Box (Näherung); für sehr präzises Anfassen
  gedrehter Bilder/Zettel ggf. später verfeinern. Grundfunktion (drehen, rendern,
  exportieren) läuft.
- **Gestrichelte/gepunktete Tabellenränder:** WPF-FlowDocument-Tabellen rendern
  Zellränder immer durchgezogen; „Rand-Arten" daher über Linienverteilung
  (alle/außen/innen/keine) + Dicke gelöst, nicht über Strichart.
- **Grammatik-/Satzbauprüfung (Text-Editor, „blaue" Markierung):** Die WPF-
  `RichTextBox` bringt nur Rechtschreibung mit (rote Wellenlinie, Vorschläge jetzt
  im Kontextmenü). Eine echte Grammatikprüfung gibt es in .NET nicht eingebaut;
  sie bräuchte eine große externe Engine oder einen Online-Dienst (widerspricht dem
  Offline-Ziel). Bewusst nicht umgesetzt.


- **Import-Dauer**: PDF-Rendering ist CPU-gebunden (~0,5–0,7 s pro Seite bei
  2246 px). Jetzt wenigstens non-blocking mit Fortschritt. **Mögliche künftige
  Optimierung**: Seiten *lazy* on-demand rendern (nur PDF-Bytes + Seitenindex
  speichern, Bild erst beim Anzeigen erzeugen/cachen) → Import quasi sofort.
  Größerer Umbau (Persistenz, Undo, Save/Load), bewusst vertagt.
- **Datei-Einfüge-Tool** ✔ (umgesetzt 2026-07-15): Der eine Import-Button nimmt jetzt
  auch **DOCX** (zusätzlich zu Bild/PDF), per Klick, Strg+V (Bilder) und Drag&Drop.
  Ab 2 Seiten erscheint der **Seitenauswahl-Dialog** (`FileInsertDialog`): Thumbnails
  mit Häkchen, „Alle/Keine", Button zeigt „N Seiten einfügen". Gewählte Seiten landen
  wie bisher im Whiteboard (2-spaltige Bild-Seiten) bzw. Notizbuch (neue Hintergrund-
  Seiten). DOCX wird über den Text-Paginator (`PdfExporter.RenderFlowDocumentPages`,
  ruft `DocxImporter.ToFlowDocument`) zu JPEG-Seiten gerendert – gleiche Optik wie der
  Text-Export. Verifiziert: Dialog + Auswahl-Logik im View-Host-Harness, DOCX→Seiten
  im `gonk-texttest`-Harness (`dialog`/`docxpages`).
- **Zeichenhilfen** (`WhiteboardView`, Bereich „Zeichenhilfen: Lineal & Geodreieck"):
  transiente Overlays (nicht in der DB gespeichert). Gemeinsame Basis `DrawAid`
  (None/Ruler/SetSquare), Toolbar-Gruppe `BtnRuler`/`BtnSetSquare` (klappbar wie die
  Stifte). Einrasten = Punkt-auf-Kanten-Projektion (`TryActivateAidSnap`/`ApplyAidSnap`),
  Bewegen/Drehen (`TryBeginAid`/`UpdateAidDrag`), Winkel-Rastung 15° (`SnapAngle`).
  Geometrie über `AidP`/`AidPolar`; alles als Bruchteil von `SsHalfHyp` (=8 cm) bzw.
  `PxPerCm`. **Lineal ok, Geodreieck-Verhalten offen (§4).** Zahlen drehen mit der
  Hilfe mit (aufrecht war eine frühere Variante). Prototyp/Sichtprüfung:
  `%TEMP%\gonk-geotest` und `%TEMP%\gonk-rulertest` (portieren die Zeichenlogik 1:1).
- **Nutzer-Strategie & Phase-3-Rest (Stand 2026-07-16, verbindlich):**
  - **Reihenfolge (Nutzer-verbindlich, Stand 2026-07-23):** laufende Fixes (Vorrang) →
    **Code-Cleanup** → **zweite Sprache (DE/EN, i18n)** → **RAM-Optimierung** →
    **GitHub-Veröffentlichung (MIT)** → Render-Caching nur bei Bedarf. RAM nicht vor dem
    Aufräumen anfangen; i18n bewusst nach dem Cleanup.
  - **Code-Cleanup / Projekt aufräumen (vor der RAM-Optimierung, Nutzer-Wunsch):** den
    bestehenden Code durchgehen und aufräumen, bevor RAM angegangen wird. Ansatzpunkte:
    tote/ungenutzte Pfade und Altlasten entfernen, die 6 `CS8622`-Warnungen in
    `WhiteboardView.Numpad.cs` beheben (0-Warnungen-Ziel halten), große partial-Dateien/
    Methoden entwirren, Namensgebung/Kommentare vereinheitlichen, überflüssige `TestAssets`/
    Wegwerf-Harnesses aufräumen, Doppelungen zusammenführen. **Verhalten unverändert lassen**
    (reines Aufräumen, keine Feature-Änderung); nach jedem Schritt Build 0 Warnungen + kurzer
    Sichttest. Beim Aufräumen **Kernlogik (Models/Services/DB) sauber von den Views entkoppeln**
    – das erleichtert i18n *und* einen möglichen späteren Cross-Platform-Port.
  - **Zweite Sprache (DE/EN, i18n) — nach dem Cleanup (Nutzer-Wunsch 2026-07-23):** Umschaltung
    unter „Ansicht → Sprache", zur Laufzeit (kein Neustart). Empfohlenes Muster: zentraler
    `LocalizationManager` (`INotifyPropertyChanged`) + Markup-Extension `{loc:T Key}`, alle
    Texte darauf umstellen; Sprachwechsel feuert PropertyChanged → UI aktualisiert live. Der
    Umbau ist klein, das **Extrahieren der vielen hartcodierten deutschen Strings** ist der
    Aufwand (MainWindow, großes Text-Editor-Ribbon, Whiteboard, Dialoge, `MessageBox`-Texte,
    dynamisch gebaute Menüs). Beim Cleanup Strings gleich zentralisieren. Kleinkram: dynamische
    Menüs müssen die aktuelle Sprache lesen; Über-Dialog lädt das dt. README (EN-Variante oder
    DE lassen); Datums-/Zahlenformate über `CultureInfo`. Grob 1–3 Tage für Vollabdeckung.
  - **GitHub-Veröffentlichung — NACH fertiger RAM-Optimierung (Nutzer-Wunsch 2026-07-23):**
    Ziel Open Source, **MIT-Lizenz** (Copyright Manuel Toegel). Vor dem Public-Schalten:
    - **`LICENSE` (MIT)** anlegen + README-Abschnitt „Lizenz" (+ optional „Third-party": alle
      Deps permissiv — LiteDB/SkiaSharp/Svg.Skia/OpenXML MIT, Docnet/PDFium BSD-3, Tesseract +
      tessdata Apache-2.0).
    - **Alle Sticker löschen** (`Assets/Stickers/*.png`, 14 Stück, u. a. Meme-/Fremdmaterial) –
      Nutzer hat **keine Lizenz** dafür. **Auch aus der Git-History entfernen** (sonst per altem
      Commit auscheckbar): am einfachsten **neues Repo mit einem sauberen Initial-Commit** (ohne
      Alt-History) oder `git filter-repo`/BFG auf `Assets/Stickers/**`. csproj-Include Zeile 46
      kann bleiben (matcht dann nichts) oder mit raus. Sticker-Feature bleibt (Nutzer-Sticker in
      %APPDATA%), nur die mitgelieferten Basis-Sticker entfallen.
    - **Cover bleiben** (`Assets/Covers/**` – vom Nutzer selbst erstellt, unbedenklich).
    - **`TestAssets/`** ist schon in `.gitignore` (GoodNotes-Screenshots/Test-PDF gehen nicht mit
      hoch) und wird ohnehin nach der RAM-Optimierung gelöscht.
    - Keine echten Notizdaten/Namen im Repo **oder in der History** (echte DB liegt in %APPDATA%).
    - Keine Segoe-Font-Dateien einchecken (App nutzt System-Font – bundlet keine `.ttf`).
  - **Cross-Platform (offen, Priorität Linux) — Nutzer-Frage 2026-07-23:** WPF ist Windows-only,
    die UI-Schicht müsste also neu. Empfehlung, wenn Linux Priorität hat und C# bleiben soll:
    **Avalonia UI** (WPF-nahes XAML; Windows/Linux/macOS/iOS/Android). Reuse: Modelle, DB
    (LiteDB), **Whiteboard-Rendering (SkiaSharp)**, Kernlogik. **Harter Brocken:** der
    Text-Editor (WPF `RichTextBox`/`FlowDocument` hat kein Avalonia-Äquivalent → Neubau) – zuerst
    als Prototyp abklopfen. Windows-Spezifika ersetzen: DWM-Titelleiste/P-Invoke (Avalonia-
    Chrome), WPF-Rechtschreibung → **WeCantSpell.Hunspell** (pure C#, plattformunabhängig – löst
    nebenbei das Windows-Wörterbuch-Problem, s. Runde 18), Tesseract/PDFium haben Linux-Builds.
    Aufwand: groß (Port, kein Rewrite wie bei Flutter), Wochen. Deshalb beim Cleanup UI/Kernlogik
    entkoppeln. **PoC am 2026-07-23 begonnen (Avalonia, `GonkNote.Avalonia/`); vollständiger
    Umsetzungs-Leitfaden in §9.**
  - **RAM-Leitlinie: „Features vor RAM".** Zielwunsch ~200 MB. Als akzeptabel nannte
    der Nutzer „unter 80 MB" — das ist angesichts der ~190-MB-Basis mit ~96 MB
    Bild-Cache **mit hoher Wahrscheinlichkeit ein Tippfehler für ~800 MB**; die
    **harte, nie zu überschreitende Obergrenze ist 1 GB**. Vor dem RAM-Thema den
    Schwellenwert **einmal beim Nutzer rückversichern**. RAM ist ausdrücklich
    zweitrangig — kein Feature dafür opfern.
  - **Render-Caching:** erst umsetzen, **wenn nach der RAM-Optimierung** die
    Auslastung noch zu hoch ist. Ausnahme: wäre es technisch unsinnig, es *nach* der
    RAM-Optimierung zu machen, dann vorziehen — aber **immer erst nach** den
    laufenden Änderungen/Fixes.
  - **OCR: ✔ umgesetzt (2026-07-17) mit Tesseract** (Nutzer-Entscheidung 2026-07-16).
    - `Tesseract` 5.2.0 (NuGet) + native `tesseract50.dll`/`leptonica`-DLLs (kommen
      über das Paket in den `x64`-/`x86`-Unterordner neben der Exe).
    - Sprachdaten `tessdata/deu.traineddata` + `eng.traineddata` (**tessdata_fast**,
      zusammen ~5,6 MB) liegen als **lose Begleitdatei** neben der Exe – genau wie die
      Basis-Sticker (`Content CopyToOutputDirectory=PreserveNewest` in der csproj).
      Damit auch im Single-File-Publish dabei (wie die Sticker).
    - `Services/OcrService.cs`: `Recognize(byte[])` → erkannter Text. Setzt
      `TesseractEnviornment.CustomSearchPath = AppContext.BaseDirectory` (im
      Single-File-Publish ist die Assembly-Location leer, sonst findet der native
      Loader `x64\` nicht). Kleine Bilder werden vor dem OCR hochskaliert (bessere
      Erkennung). Sprachwahl automatisch aus vorhandenen tessdata (`deu+eng`).
    - UI: Aktion „Text erkennen (OCR)" im **Quick-Options-Menü** (`WhiteboardView.Ocr.cs`).
      Quelle = ausgewählte Bild-Elemente, sonst (ohne Auswahl) der importierte
      Seitenhintergrund (PDF-Seite). Ergebnis im `OcrResultDialog` (bearbeitbar,
      Kopieren / als Notizzettel einfügen). Läuft async mit Busy-Overlay.
    - **Verifiziert:** kompletter nativer Stack + tessdata end-to-end im Harness
      `%TEMP%\gonk-ocrtest` (rendert bekannten Text via SkiaSharp, erkennt ihn korrekt
      inkl. Zahlen, deu+eng). OCR-Button erscheint im Quick-Menü (Screenshot, tessdata
      neben Debug-Exe erkannt). **In-App-Endfluss (Klick→Dialog→Zettel) noch nicht mit
      echtem Bild im Fenster durchgespielt.**
    - Für **Handschrift** ggf. separat der `InkAnalyzer` (offen).

  - **Quick-Options-Menü (Runde 10, ersetzt das Rechtsklick-Kontextmenü):**
    Floatende Icon-Leiste im Toolbar-Look (`WhiteboardView.xaml` Border `QuickMenu`,
    Icon-Buttons `Qm_*` mit Segoe-Fluent-Glyphen). Logik in `WhiteboardView.xaml.cs`
    (`ShowQuickMenuAt`/`ShowQuickMenuForSelection`/`PrepareQuickMenu`/`PlaceQuickMenu`/
    `HideQuickMenu`/`IsOnQuickMenu`). **Auslöser:** Maus-Rechtsklick
    (`OnCanvasRightButtonUp`), **zweite Stift-Taste** (`OnCanvasStylusSystemGesture`,
    `SystemGesture.RightTap`; Entprellung gegen doppelte Auslösung per `_quickShownTick`),
    **und automatisch nach frischer Lasso/Move-Auswahl** (`freshSelect` in `EndInput` →
    `ShowQuickMenuForSelection`, mittig über der Auswahl). Schließt bei neuer Eingabe/
    Pan/Zoom/Tool-Wechsel/Auswahl-leeren (`HideQuickMenu` an den passenden Stellen).
    Klicks auf die Leiste werden in `OnMouseDown`/`OnStylusDown` via `IsOnQuickMenu`
    von der Zeichenlogik ausgenommen. Die alten `Cm_*_Click`-Handler bleiben (jetzt vom
    Quick-Menü genutzt) und rufen zuerst `HideQuickMenu`.
  - **Obfuskierung: gestrichen** — der Nutzer will das Projekt so **Open Source wie
    möglich** halten.
  - **Bereits erledigt** (kein offener Phase-3-Punkt mehr): Sticker, Notizzettel,
    Lineal/Geodreieck, Diagramme, Tabellen, Whiteboard-Quick-Menü (Runde 10), OCR (Runde 10).
  - **RAM-Ausgangslage** (für die spätere Optimierung): Basis ~190 MB + bis 96 MB
    Bild-Cache. Ansatzpunkte: Cache-Budget senken, GC-Tuning, PDF-Seiten lazy
    on-demand rendern (statt alle Bitmaps im Speicher).
- Text-Stiländerungen am bestehenden Whiteboard-Textfeld sind nicht undo-fähig
  (bewusst einfach). DOCX-Import: keine Fußnoten, verschachtelten
  Tabellen. PDF: keine Text-Extraktion (per Nutzer-Wunsch reine Bild-Seiten).
- **Export-Grenzen**: Text→PDF ist gerastert (kein selektierbarer Text im PDF –
  bewusst, erhält aber die Formatierung 1:1). DOCX-Bilder landen unter `media/`
  (nicht `word/media/`) – gültig. Markdown ist best-effort (Farben/Marker gehen
  verloren). Whiteboard→DOCX/Markdown gibt es nicht (nur PDF).
- **Text-Editor – bewusst nicht umgesetzt** (aus der Word-Funktionsliste in
  `Docs/word-funktionen-liste.md`; ehrliches Scoping):
  - **Kein Live-Seitenumbruch**: Der Editor zeigt eine fortlaufende Seite in
    Seitenbreite; der Umbruch in echte Seiten passiert beim PDF-Export (WPF-
    RichTextBox kann nicht paginiert editieren). Statusleiste zeigt daher keine
    Seitenzahl.
  - Spalten, Abschnittsumbrüche, Zeilennummerierung, Texteffekte (Schatten/Kontur),
    Formen/SmartArt/WordArt/Diagramme, Fußnoten/Endnoten, Querverweise, Lesezeichen,
    Zitate/Literaturverzeichnis, Kommentare, Änderungen nachverfolgen, Dokumente
    vergleichen, Versionsverlauf, Thesaurus, Serienbrief, Makros, AutoKorrektur/
    QuickParts, Vorlagen-Katalog, Barrierefreiheitsprüfung, Übersetzen.
  - Rechtschreibprüfung = WPF-eigene (de-DE/en-US umschaltbar in der Statusleiste);
    rote Unterstreichung braucht das jeweilige Windows-Sprachpaket.
  - Wasserzeichen wird in PDF exportiert, aber (noch) nicht in DOCX (Header-Bild
    hinter Text in OpenXML ist aufwendig; vertagt).
  - Kopf-/Fußzeile: ein Text für alle Seiten (+ Option „erste Seite ohne“); keine
    getrennten gerade/ungerade Seiten.
- **Design-Entscheidungen Text-Editor** (Design-Konzept kritisch angewendet):
  - **Seite bleibt in beiden Themes weiß** (Feedback-Runde 6 — überschreibt die
    Konzept-Entscheidung „Seite folgt Color.PageBg“). Nur die Canvas-Umgebung
    folgt dem Theme. Feste helle Werte im Editor: PageBgBrush #FFFFFF, InkBrush
    #1B2B4B, Selektion #C7DBFF. `TextStyles.NormalizeInk` bleibt aktiv, um
    Dokumente zu reparieren, die in der kurzen Dunkle-Seite-Phase helle Tinte
    gespeichert haben; Exporte normalisieren weiterhin auf dunkle Tinte.
  - Linke Icon-Leiste nur mit real existierenden Funktionen (Suche, Navigator) —
    keine toten Icons für Kommentare/Plugins.
  - Titelleiste/Fenster-Chrome bleibt App-Sache (Editor ist ein Tab).
  - Toolbar mit Sammel-Buttons (Ausrichtung ▾, Listen/Einzug ▾) und WrapPanels —
    nichts ragt aus dem Sichtfeld, bei schmalen Fenstern bricht das Ribbon um
    (Feedback-Runde 6). Schriftarten-Combo mit Live-Vorschau je Eintrag.

---

## 6. Empfohlener Ablaufplan für den neuen Thread

**Aktuelle Phase: Nutzer testet ausführlich (Stand 2026-07-16).**

1. **Laufende Änderungs-/Fix-Wünsche des Nutzers zuerst** einarbeiten (kommen in
   Batches während seiner Testphase). Sie haben Vorrang vor allem anderen.
2. **Ist der Nutzer zufrieden → Code-Cleanup / Projekt aufräumen** (Nutzer-Wunsch
   2026-07-23, Details in §5): Altlasten/tote Pfade raus, `CS8622`-Warnungen beheben,
   große partials entwirren, Doppelungen zusammenführen, **Kernlogik von den Views
   entkoppeln** — **Verhalten unverändert**, Build 0 Warnungen.
3. **Zweite Sprache (DE/EN, i18n)** — vom Nutzer **nach dem Cleanup** gewünscht
   (2026-07-23): „Ansicht → Sprache", zur Laufzeit umschaltbar (§5).
4. **RAM-Optimierung** (Details/Schwellenwerte in §5, „Nutzer-Strategie"). **Vorher
   den 1-GB-/800-MB-Wert einmal rückversichern** und die Leitlinie „Features vor RAM" beachten.
5. **GitHub-Veröffentlichung (MIT) — NACH fertiger RAM-Optimierung** (Nutzer-Wunsch
   2026-07-23): `LICENSE` (MIT), **alle Sticker löschen inkl. Git-History** (keine Lizenz),
   **Cover bleiben** (selbst erstellt), `TestAssets/` ohnehin gelöscht. Volle Checkliste in §5.
6. **Render-Caching** nur bei Bedarf **nach** der RAM-Optimierung (§5).
7. **OCR ✔ umgesetzt (2026-07-17) mit Tesseract** (§5) — Kontextmenü „Text erkennen
   (OCR)" auf Bildern/PDF-Seiten. In-App-UI-Fluss noch im echten Fenster zu testen;
   `InkAnalyzer` für Handschrift bleibt offen.
8. **Obfuskierung ist gestrichen** (Open-Source-Ziel).

Export sitzt in `Datei → Exportieren`; `ExportActiveTab` wählt anhand des aktiven
Tabs die Formate.

---

## 7. UI-Tests (bewährtes Muster — wichtig!)

- **Nie in der echten Nutzer-DB testen!** `%APPDATA%\GonkNote\gonknote.db` enthält
  echte Notizen/Schuldaten. Immer `GonkNote.exe --db <wegwerf.db>` verwenden.
- Skripte liegen in `%TEMP%\gonk-verify\` (PowerShell). Bausteine: `SetProcessDPIAware`,
  UIA (`System.Windows.Automation`) für Menüs/TreeItems/benannte Buttons,
  `mouse_event`/`SetCursorPos` für Canvas-Drags mit **physischen** Koordinaten,
  Screenshot je Schritt. Skripte per `Get-Content -Raw -Encoding UTF8` + neu
  speichern re-enkodieren, sonst zerlegen Umlaute die UIA-Namenssuche.
- **Toolbar-Buttons** haben `AutomationProperties.Name` (z. B. "Datei einfügen",
  "Nächste Seite") → per UIA-`NameProperty` ansteuerbar. Der Import-Button sitzt
  bei ~physisch (2217, 242), ZoomOut ~(1958, 242) — je nach Auflösung neu ermitteln
  (siehe `locate.ps1`, das per Screenshot die Positionen zeigt).
- **Achtung**: Tests übernehmen Maus/Tastatur. Der Nutzer arbeitet oft parallel am
  Rechner — kurz halten, Fokusverlust einplanen. Ein DB-Tool (net8.0-Console mit
  LiteDB) liegt in `%TEMP%\gonk-dbclean\` (seedet u. a. `seed.db` mit fertigem
  Whiteboard/Ordner); ein Docnet-Renderer in `%TEMP%\gonk-render\` rendert
  Export-PDFs zu PNG zur Sichtprüfung.
- **Render-Harnesses (zuverlässigste Prüfung für Zeichen-/Overlay-Logik!)**: statt
  die flakige echte UI zu klicken, wird die Zeichenlogik 1:1 in ein Konsolen-Skia-
  Programm portiert und zu PNG gerendert. Vorhanden: `%TEMP%\gonk-geotest`
  (Geodreieck), `%TEMP%\gonk-rulertest` (Lineal + Snap), `%TEMP%\gonk-stickytest`
  (Notizzettel), `%TEMP%\gonk-pdftest` (PDF-Export-Qualität). So wurden Proportionen/
  Snap/Umbruch geprüft. **Aber: der Harness prüft nur das *Rendering/die Mathe*,
  nicht das *Verhalten im echten Fenster* — genau da hakt das Geodreieck (§4).**
- **Notizzettel/Geodreieck im echten Binary zeigen**: Seeder mit **echten Modellen**
  in `%TEMP%\gonk-seedsticky` (referenziert `bin\Debug\...\GonkNote.dll` direkt, da
  das Projekt eine self-contained Exe ist → kein ProjectReference möglich) schreibt
  ein Whiteboard mit Elementen in eine Wegwerf-DB; dann App mit `--db` starten und
  per **`PrintWindow(hwnd, hdc, 2)`** (PW_RENDERFULLCONTENT) kapturen — fängt das
  Fenster auch, wenn es nicht im Vordergrund ist. Vordergrund erzwingen via
  `AttachThreadInput`-Trick.
- **DPI-Stolperfalle**: die PowerShell-Instanz ist mal DPI-aware, mal nicht →
  `GetWindowRect`/`SetCursorPos` liefern inkonsistent physische vs. virtualisierte
  Koordinaten, deshalb landen fixe Klick-Koordinaten oft daneben. **UIA-`Select` auf
  TreeItems** funktioniert (öffnet Doku per Enter/Klick auf dessen BoundingRectangle);
  **UIA-`Toggle` auf die Icon-ToggleButtons der Toolbar fand den Button trotz
  `AutomationProperties.Name` nicht** (ungeklärt) — Buttons daher nur *visuell*
  bestätigt.
- **⚠️ Bekannte Grenze der Testumgebung**: Die IDE (Zed/Claude) reißt nach dem
  Start einer Test-Instanz oft den Fokus zurück → automatische `mouse_event`-Klicks
  landen dann in der IDE statt in Gonk Note (im schlimmsten Fall in einer echten,
  parallel laufenden Nutzer-Instanz!). **Zuverlässig ist nur**: Instanz mit
  `--db` starten, `ShowWindow(hwnd,3)` maximieren, **sofort einen Screenshot**
  machen (fängt den Ladezustand). *Interaktive* Klick-Sequenzen sind unzuverlässig.
  Vor jedem Test prüfen, ob eine echte Instanz läuft (`Get-Process GonkNote` +
  CommandLine ansehen) und nur die eigene per **PID** beenden, nie pauschal
  `taskkill /IM`, solange der Nutzer die App offen haben könnte.

---

## 8. Schnellstart-Befehle

```bash
cd "C:\Dev\Zed\gonk-note"
taskkill //IM GonkNote.exe //F                     # vor jedem Build
dotnet build
./bin/Debug/net8.0-windows/GonkNote.exe            # Debug-Start (ECHTE DB!)
./bin/Debug/net8.0-windows/GonkNote.exe --db X.db  # Test-DB (fuer UI-Tests)
dotnet publish -c Release                           # Single-File-Exe
# → bin/Release/net8.0-windows/win-x64/publish/GonkNote.exe
```

---

## 9. Cross-Platform-Port (Linux, Avalonia) — Leitfaden für einen neuen Thread

**Ziel (Nutzer 2026-07-23): die App auch unter Linux nutzbar machen — Priorität Linux, C#
behalten, KEIN Flutter.** iPad wäre „eventuell später". Entscheidung: **Avalonia UI**
(WPF-nahes XAML, ein .NET-Code für Windows/Linux/macOS/iOS/Android). MAUI raus (kein
offizielles Linux), Flutter raus (kompletter Rewrite, kein C#-Reuse).

> **Einstieg im neuen Thread:** „Lies HANDOFF.md §9 und mach mit dem Avalonia-Port weiter."
> Der PoC liegt schon im Repo unter `GonkNote.Avalonia/`.

### 9.1 Stand: PoC bereits gebaut & verifiziert (2026-07-23)
- Neues, **eigenständiges** Projekt **`GonkNote.Avalonia/`** (Avalonia **11.0.10**,
  **`net8.0` — plattformneutral, NICHT `-windows`**). Reines NuGet, **kein Workload nötig**.
- Verwendet die **echten Kernklassen der WPF-App per `<Compile Include>`-Link** (keine Kopie):
  `..\Models\NoteItem.cs`, `..\Models\Whiteboard.cs`, `..\Services\DatabaseService.cs` (+ `LiteDB`).
- Dateien: `GonkNote.Avalonia.csproj`, `Program.cs`, `App.axaml(.cs)`, `MainWindow.axaml(.cs)`,
  `app.manifest`. `MainWindow` lädt den Ordnerbaum über den **echten `DatabaseService`** aus
  einer LiteDB und zeigt ihn (Demo-DB `%TEMP%\gonk-avalonia-demo.db`, wird bei Erststart geseedet).
- **Verifiziert (Windows):** baut 0 Fehler; Fenster rendert den Baum im Fluent-Dark-Theme
  (Screenshot `%TEMP%\gonk-titlebar\AV2-avalonia.png`).
- **WPF-App unangetastet** und weiter grün: `GonkNote.csproj` schließt den Ordner aus
  (`<Compile Remove="GonkNote.Avalonia\**\*.cs"/>` + None/Page/Resource-Remove) — sonst greift
  dessen `**/*.cs`-Glob hinein.
- **Bewiesen:** Avalonia + Models + DatabaseService + LiteDB laufen auf `net8.0` → Cross-Platform-fähig.

Bauen/Starten:
```bash
dotnet build GonkNote.Avalonia/GonkNote.Avalonia.csproj
GonkNote.Avalonia/bin/Debug/net8.0/GonkNote.Avalonia.exe      # Linux: dotnet …/GonkNote.Avalonia.dll
```

### 9.1b Schritt 2 erledigt: `GonkNote.Core` extrahiert (2026-07-23, Runde 20)
- Neues **`GonkNote.Core/`** (`net8.0`, kein WPF): enthält die **echten, verschobenen**
  Dateien (per `git mv`, Historie erhalten) — `Models/NoteItem.cs`, `Models/Whiteboard.cs`,
  `Services/DatabaseService.cs`, `Services/UndoStack.cs`, `Services/ImageCache.cs`.
  Namespaces unverändert (`GonkNote.Models`/`GonkNote.Services`) → WPF-Views brauchen **keine**
  `using`-Änderung. NuGet: **LiteDB 5.0.21 + SkiaSharp 2.88.9** (für `ImageCache`).
- **WPF (`GonkNote.csproj`)** referenziert Core per `ProjectReference`; die verschobenen Typen
  kommen jetzt aus `Core.dll`. **Root-Glob-Falle:** der WPF-Root-Glob greift in `GonkNote.Core\`
  hinein → zusätzlich `Compile/None/Page/Resource Remove="GonkNote.Core\**"` (wie bei Avalonia).
- **Avalonia** referenziert Core per `ProjectReference`; die alten `<Compile Include>`-Links
  **entfernt**, LiteDB-`PackageReference` entfernt (kommt transitiv aus Core).
- **Verifiziert (Windows):** Core baut isoliert (0/0), WPF baut 0 Fehler (nur die 6 bekannten
  `CS8622`-Alt-Warnungen aus `WhiteboardView.Numpad.cs`, unberührt), Avalonia baut 0/0;
  `Core.dll`+`LiteDB.dll`+`SkiaSharp.dll` liegen im Avalonia-Output; Avalonia-Exe startet und
  läuft stabil (kein TypeLoad-Fehler). **Reste-Verweise auf `..\Models`/`..\Services` weg.**

### 9.2 Architektur-Zielbild
- ✅ **Umgesetzt (§9.1b):** die **plattformneutrale Kernlogik** liegt im echten Projekt
  **`GonkNote.Core`** (`net8.0`): Models + DB + reine Services (Undo, ImageCache). WPF **und**
  Avalonia referenzieren `Core` (ProjectReference); die `<Compile Include>`-Links im Avalonia-
  Projekt sind weg. *Später ggf. weitere WPF-freie Kernlogik nachziehen (z. B. Export-Kernteile).*
- UI bleibt pro Plattform getrennt: `GonkNote` (WPF, Windows) + `GonkNote.Avalonia` (überall).
  Optional später WPF ganz durch Avalonia ersetzen (eine UI für alle Plattformen).

### 9.3 Reuse-Karte — portierbar vs. Neubau
**Gut wiederverwendbar (kein/kaum WPF):**
- Modelle (`Models/*`), `DatabaseService` (LiteDB), Enums/Logik.
- **Whiteboard-Rendering** läuft schon über **SkiaSharp** (`WhiteboardView.Draw*` sind
  `internal static`). Avalonia nutzt selbst Skia → die Zeichenroutinen lassen sich auf eine
  Avalonia-Custom-Control heben. **Größter Reuse-Gewinn.**
- OCR (Tesseract) und PDF (Docnet/PDFium) haben **Linux-Builds** (native Libs pro RID mitliefern).

**Muss neu gebaut werden (Windows/WPF-spezifisch):**
- **Text-Editor = der harte Brocken.** WPF `RichTextBox`/`FlowDocument` gibt es in Avalonia
  nicht. Optionen: (a) eigenes Rich-Text-Modell auf Avalonias Text-Stack, (b) Markdown-Editor,
  (c) HTML-Editor via WebView. **Zuerst prototypen** — davon hängt ab, wie viel vom aktuellen
  Editor (Formatvorlagen, Tabellen, Export) sich retten lässt. Aufwendigstes Teil des Ports.
- **Fenster-Chrome**: DWM-Titelleiste + `WindowBounds`/`TitleBarTheme` (P/Invoke, Runde 16/17)
  → Avalonias `ExtendClientAreaToDecorationsHint`/Fenster-APIs. Einblendbare Titelleiste (Runde 17)
  neu, aber einfacher.
- **Rechtschreibung**: WPF-`SpellCheck` ist Windows-Plattform → **WeCantSpell.Hunspell** (pure C#)
  + Hunspell-Wörterbücher (de_DE/en_US) mitliefern. **Bonus:** löst zugleich das Englisch-Problem
  auf Windows (Runde 18) und macht die Prüfung unabhängig vom Windows-Sprachpaket.
- WPF-Only-Pfade rund um `FlowDocument` (`PdfExporter` Text-Teil, `DocxImporter/Exporter`,
  `MarkdownExporter`, `TextStyles`) hängen am Editor-Ansatz und werden mitgezogen.

### 9.3b Text-Editor-Prototyp (Schritt 5) — Ergebnis & Entscheidung (2026-07-23, Runde 20)
**Auf Nutzer-Wunsch vorgezogen prototypt** (der Risiko-Teil). Umgesetzt im Avalonia-Projekt:
`EditorPrototypeWindow.axaml(.cs)` (Editor mit Formatier-Toolbar Fett/Kursiv/Code/H1–H3/Listen/
Zitat/Tabelle + Markdown-Datei-Roundtrip) und `MarkdownProbe.axaml(.cs)` (isolierter Render-Test);
beide über Buttons in `MainWindow` erreichbar. Neues Paket: **`Markdown.Avalonia` 11.0.2**.

**Kernbefund (verifiziert per Screenshot `%TEMP%\gonk-titlebar\AV3-mdprobe.png`):**
`Markdown.Avalonia` **rendert formatierten Rich-Text** (Überschriften, **fett**, *kursiv*, `Code`,
Aufzählungen, nummerierte Listen, Zitatblöcke, **Tabellen**) sauber auf `net8.0` → cross-platform-
tauglich, schlank, offline, **ohne** WPF/FlowDocument/WebView. **Das beantwortet die Machbarkeits-
frage: JA.** Zwei Styling-To-dos: der Style `"Standard"` ist nicht dark-mode-aware (Überschriften
unsichtbar auf dunklem Grund, Tabelle mit hellem Hintergrund) → eigener/dunkler Markdown-Style nötig.

**Entscheidungsmatrix der drei Ansätze:**
| Ansatz | Cross-Platform/schlank | Feature-Deckung ggü. WPF-Editor | Aufwand | Urteil |
|---|---|---|---|---|
| **(b) Markdown + Live-Vorschau** | ★★★ (pure C#, klein, offline) | mittel (kein WYSIWYG, keine Seiten-Layout/Kopf-Fußzeile; Tabellen/Listen/Überschriften ja) | niedrig | **empfohlen als Start** |
| (a) Eigenes Rich-Text auf Avalonia-Text-Stack | ★★★ | potenziell hoch (WYSIWYG möglich) | **sehr hoch** (Caret/Selektion/Layout/Tabellen selbst bauen — Wochen) | später, nur wenn WYSIWYG zwingend |
| (c) HTML-Editor via WebView (CEF) | ★ (CEF ~100 MB, Linux-Packaging, Single-File-Bruch) | hoch | mittel | **verworfen** (widerspricht offline/schlank/Single-File) |

**Empfehlung:** **Ansatz (b) Markdown** als Editor der Avalonia-Version. Passt zu den Kern-
vorgaben (offline, Single-File, schlank, Linux-Priorität) und nutzt den **schon vorhandenen
Markdown-Weg** (`MarkdownExporter`/`MarkdownImporter`). **Preis (ehrlich):** Der Avalonia-Editor
wird **nicht** die Word-artige WYSIWYG-Fülle des WPF-Editors haben (kein freies Seiten-Layout,
Kopf-/Fußzeilen, Wasserzeichen, Format­vorlagen-Galerie). Rettbar sind: **Tabellen, Listen,
Überschriften, Fett/Kursiv/Code, Export nach Markdown/PDF** (PDF über Markdown→Render).
Die plattformneutralen **Seiteneinrichtungs-Felder** in `TextDoc` (Format/Ränder/Kopf-Fuß) bleiben
im Modell nutzbar. **Speicherung:** UTF-8-Markdown in `TextDoc.Rtf` (statt WPF-XamlPackage).
**Offene Nutzer-Entscheidung:** ob dieser bewusste Feature-Verlust für Linux ok ist, oder ob der
WPF-Editor Windows-exklusiv bleibt und Avalonia nur den schlanken Markdown-Editor bekommt.

**Beobachteter Avalonia-Layout-Quirk (Notiz für Schritt 3/5):** In dieser Dev-Umgebung
(Avalonia 11.0.10 @ **200 % DPI**, Windows) bricht eine **mehrzeilige `TextBox` mit
`TextWrapping="Wrap"`** nicht auf Containerbreite um (läuft rechts über); `TextBlock`-Wrapping und
das übrige Layout (Shell, Baum, Toolbar) funktionieren dagegen einwandfrei, und der reine
`MarkdownScrollViewer` beschränkt sich korrekt. Vermutlich ein DPI/Version-spezifischer Effekt →
beim echten Port auf Linux / mit gepinnter Avalonia-Version gegenprüfen (Zwei-Spalten-Split erst
danach fein machen; der Prototyp nutzt vorerst **Reiter Bearbeiten/Vorschau** statt Side-by-side).

### 9.3c Shell-Baustein 3a (2026-07-23, Runde 20) — Ordnerbaum + Navigation + Theme
**Avalonia-native VMs** (kein Wholesale-Port des 711-Zeilen-WPF-`MainViewModel`):
- `Mvvm.cs` (ObservableObject + RelayCommand **ohne** WPF-`CommandManager`),
  `TreeItemVM.cs` (Glyph als Emoji — Segoe-Fluent gibt's unter Linux nicht; Icon-Farbe als
  Avalonia-`SolidColorBrush`, eigen→geerbt→Türkis; Sortierung Ordner→Favorit→Name),
  `ShellViewModel.cs` (lädt Baum über echten `DatabaseService`, baut **Farbvererbung** nach,
  hält `Selected`). Seed farbig in `%TEMP%\gonk-avalonia-shell.db`.
- `MainWindow`: Zwei-Panel-Shell (`DockPanel`: Baum links `Width=300`, Inhalt rechts) +
  Kopfleiste mit **Theme-Umschalter** (`RequestedThemeVariant` Light/Dark) und Buttons zu
  Editor-Prototyp/Render-Probe. Auswahl im Baum ⇒ rechts Kontext (Typ + Titel; echte Doku-
  Ansicht folgt 4/5).
- **Verifiziert per Screenshot** (`%TEMP%\gonk-titlebar\AV3-shell3a-sel2.png`): Baum rendert,
  **Farbvererbung sichtbar** (Ordner „Projekte" blau → Kinder blau, „Schule" rot → Kinder rot),
  Sortierung stimmt, **Auswahl „Ideen" → rechts „Notizbuch / Ideen"** (Navigation end-to-end).
- **Quirk-Workaround angewandt:** Der in §9.5 beschriebene Fill-Panel-Measure-Effekt traf auch den
  rechten Inhaltsbereich (Inhalt wurde rausgeschoben). **Gelöst:** `MainWindow.axaml.cs` koppelt
  `ContentHost.Width` an `ClientSize.Width − Seitenleiste` (`GetObservable(ClientSizeProperty)`) →
  Inhalt bricht/zentriert wieder korrekt (per Screenshot `AV3-shell3a-fix.png` verifiziert:
  Leerzustand zentriert). **Muster für 3b/Doku-Ansichten:** Fill/Star-Bereichen eine explizite,
  an die Fenstergröße gekoppelte Breite geben — oder beim Linux-Build prüfen, ob der Quirk dort
  gar nicht auftritt (dann entfällt der Workaround).

### 9.3d Shell-Baustein 3b — „Big-Picture"-Galerie (2026-07-24, Runde 20)
- Rechter Inhaltsbereich zeigt jetzt den **Ordnerinhalt als Kacheln** (GoodNotes-Stil): farbige
  Karte je Element (Ordnerfarbe/geerbte Farbe als Kartenhintergrund, Emoji-Glyph, Name, Typ).
  Keine Auswahl / Ordner gewählt ⇒ Galerie (`ShowGallery`); Nicht-Ordner gewählt ⇒ Doku-Kontext.
- `ShellViewModel`: `GalleryItems` (+ `RebuildGallery`), `ShowGallery`/`ShowDocument`,
  `GalleryTitle`, `GalleryEmpty`, **`OpenItem`-`ICommand`** (Kachel-/Baumklick: Ordner rein +
  aufklappen, sonst Kontext). `TreeItemVM.KindLabel` für Kacheln. Kachel = `Button.tile`
  (flache Karte + Hover, Style im `MainWindow.axaml`), gebunden per
  `{Binding DataContext.OpenItem, RelativeSource={RelativeSource AncestorType=Window}}`.
- **WrapPanel braucht endliche Breite** → funktioniert nur dank des `ContentHost.Width`-Workarounds
  (§9.3c/§9.5). `MainWindow.axaml.cs` braucht `using Avalonia;` + `SizeChanged`/`Loaded` (statt
  `GetObservable(...).Subscribe(Action)` — die Overload fehlt in 11.0.10).
- **Verifiziert per Screenshot:** Startansicht „Alle Dokumente" mit farbiger Ordner-Kachel
  (`AV3-gallery.png`); Ordnerauswahl „Projekte" ⇒ Galerie zeigt Kinder Ideen/Skizzen als Kacheln
  (`AV3-gallery-sub.png`). **Kachel-Klick-Navigation nur per Logik/Vorauswahl geprüft** — echte
  Maus-Klicks ließen sich in der Testumgebung nicht automatisieren (Foreground-Sperre); der
  `OpenItem`-Command ist Standard-MVVM und baut ohne Binding-Fehler.

**Nachtrag 3b: Breadcrumb + Umbenennen (2026-07-24).**
- **Breadcrumb** über der Galerie: „Alle Dokumente › Ordner › …", Segmente sind Buttons
  (`Button.crumb`, Link-Optik) mit `NavigateCrumb`-Command; `BreadcrumbEntry(Label, Target)`,
  `Target=null` = Wurzelansicht. Aufbau in `RebuildBreadcrumb()` über die neue
  **`TreeItemVM.Parent`**-Kette (in `LoadTree` gesetzt); bei gewähltem Dokument zählt dessen
  Elternordner als Position.
- **Inline-Umbenennen** im Baum: **Doppelklick** startet die Bearbeitung (`TreeItemVM.BeginRename`,
  `IsRenaming`/`IsNotRenaming`/`EditName`); im Template wechselt TextBlock ⇄ TextBox.
  **Enter** übernimmt, **Escape** verwirft, **Fokusverlust** übernimmt
  (`Tree_DoubleTapped`/`Tree_KeyDown`/`Tree_LostFocus` in `MainWindow.axaml.cs`).
  `ShellViewModel.CommitRename` schreibt via **`_db.UpsertItem`** in dieselbe LiteDB, sortiert die
  Ebene neu (`SortCollection`/`SortRoots`) und frischt Galerie/Breadcrumb auf; leerer oder
  unveränderter Name = verwerfen.
- **Verifiziert:** Breadcrumb „Alle Dokumente › Projekte ›" + Umbenennung „Skizzen" → „Skizzen NEU"
  in Baum *und* Kachel (`AV3-breadcrumb-rename.png`); **Persistenz end-to-end bestätigt** — nach
  Neustart ohne Temp-Code steht „Skizzen NEU" weiterhin im Baum (`AV3-persist.png`).
  *Nicht klick-getestet* (Foreground-Sperre): Doppelklick-Auslöser und Breadcrumb-Klick.

**Nachtrag 3b: Umbenennen-Fokus-Fix + Anlegen/Löschen/Favoriten (2026-07-24).**
- **Bugfix Umbenennen** (Nutzer-Meldung: „nach Doppelklick nur ~1 s bearbeitbar"): Ursache war ein
  `LostFocus`-Handler am **ganzen Baum** (feuerte bei fremden Fokuswechseln) plus ein Fokus-Rückgriff
  der TreeView. Fix: Handler (`KeyDown`/`GotFocus`/`LostFocus`) hängen jetzt **an der TextBox**;
  der Fokus wird mit `DispatcherPriority.Background` **nach** der TreeView-Fokuslogik gesetzt
  (+ `SelectAll`), und der `LostFocus`-Commit greift erst, wenn die Box **wirklich Fokus hatte**
  (`_renameHadFocus`). **Vom Nutzer gegenzuprüfen** — Klicks sind hier nicht automatisierbar.
- **Neu anlegen**: „＋ Neu"-Button (MenuFlyout: Ordner/Notizbuch/Whiteboard/Textdokument) legt im
  aktuellen Ordner an (`ShellViewModel.CreateItem`), erbt die Ordnerfarbe, speichert via
  `UpsertItem` und springt direkt in die Umbenennung.
- **Löschen** (`DeleteItem` → `DatabaseService.DeleteItemRecursive`, inkl. Unterbaum) und
  **Favorit umschalten** (`ToggleFavorite`, Stern ★ im Baum, wirkt auf die Sortierung) über das
  **Kontextmenü** im Baum (Umbenennen/Favorit/Löschen).
- **Verifiziert per Screenshot** (`AV3-crud.png`): „Neues Notizbuch" in „Schule" mit geerbter Farbe,
  „Biologie" mit ★ nach oben sortiert, „Notizen" gelöscht.

### 9.3e Schritt 4 (Whiteboard) — gemeinsamer Skia-Renderer + Avalonia-Canvas (2026-07-24)
- **`GonkNote.Core/Rendering/WbRenderer.cs`** (neu, `namespace GonkNote.Rendering`): die reinen
  SkiaSharp-Zeichenroutinen aus `Views/WhiteboardView.xaml.cs` — `DrawElement`(+Rotation)/
  `DrawElementCore`, `DrawStroke` (Druckverlauf, Pencil, Highlighter), `DrawShape` (Linie/Pfeil/
  Rechteck/Ellipse/Dreieck), `DrawText`, `DrawSticky`(+`DrawStickyCard` mit gecachtem
  Nine-Patch-Schatten), `DrawImage` (über `ImageCache`), plus Helfer `ParseColor`,
  `BuildSmoothPath`, `TrianglePoints`, `TextBounds`, `ElementBounds`, `WrapText` und
  **`WbFonts`**. Enthält **keine** Eingabe-/Werkzeuglogik.
- **`GonkNote.Avalonia/WhiteboardCanvas.cs`** (neu): Avalonia-`Control` mit
  `ICustomDrawOperation`; leiht sich über `ISkiaSharpApiLeaseFeature` **direkt Avalonias SKCanvas**
  (kein Zwischenbild) und zeichnet Seite + Elemente mit `WbRenderer`. Properties `Page`/`Zoom`
  (`AffectsRender`). Seitenmuster (Linien/Raster/Punkte) vereinfacht nachgebaut.
- Shell-Anbindung: Auswahl eines Whiteboards/Notizbuchs lädt via `DatabaseService.GetBoard` die
  erste Seite (`ShellViewModel.CurrentPage`/`ShowWhiteboard`); Seed legt ein Beispiel-Board an.
- **Verifiziert per Screenshot** (`AV3-whiteboard2.png`): Strich mit Druckverlauf, Rechteck,
  Pfeil, Text und Punktraster rendern im rechten Bereich, Baum/Kopfleiste bleiben sichtbar.
- **Zwei Fallstricke (gelöst, für später merken):**
  1. `ImmediateDrawingContext.TryGetFeature` ist in Avalonia 11.0.x **nicht generisch**
     (`TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is ISkiaSharpApiLeaseFeature`).
  2. **`SKCanvas.Clear()` löscht die gesamte Fensteroberfläche**, nicht nur das Control — es
     überdeckte anfangs Baum und Kopfleiste. Stattdessen auf `Bounds` **clippen** und den
     Hintergrund als Rechteck füllen.
- ✅ **De-Duplizierung erledigt (2026-07-24):** die WPF-`WhiteboardView` leitet ihre Draw-/Helfer-
  Methoden jetzt an `WbRenderer` weiter (`DrawElementCore`, `DrawImage/Stroke/Shape/Text/Sticky/
  StickyCard`, `ElementBounds`, `TextBounds`, `TrianglePoints`, `ParseColor`, `WrapText`,
  `DrawCachedShadow`; `Fonts` → `WbFonts`). Die eigenen Kopien (inkl. `BuildSmoothPath`,
  `ShadowNinePatch`, `BreakLongWord`) sind entfallen: **`WhiteboardView.xaml.cs` 4457 → 4077
  Zeilen (−380)**. WPF baut 0 Fehler (nur die 6 bekannten CS8622 aus `Numpad.cs`). Damit nutzen
  WPF, PDF-Export und Avalonia **eine** Implementierung.
**Schritt 4b (Teil 1): Stift-Eingabe (2026-07-24).**
- `WhiteboardCanvas` verarbeitet jetzt `OnPointerPressed/Moved/Released`: baut einen
  `StrokeElement` mit **Druckstärke** (`PointerPoint.Properties.Pressure`; ohne Drucksensor 0,5),
  zeichnet ihn live als Overlay (`WbRenderer.DrawStroke`) und hängt ihn beim Loslassen an die
  Seite. Zeigererfassung via `Pointer.Capture`; Koordinaten werden durch den Zoom geteilt.
- Neue Properties `InkColor`/`InkWidth`; Event **`StrokeCompleted`** → `MainWindow` ruft
  `ShellViewModel.SaveCurrentBoard()` → **jeder fertige Strich landet sofort in der LiteDB**
  (der geladene `WhiteboardDoc` wird dafür im VM gehalten).
- Kompakte **Stift-Werkzeugleiste** über dem Canvas: 4 Farben + dünn/mittel/dick.
- **Hit-Testing:** `Control` hat kein `Background`; damit Pointer-Events zuverlässig ankommen,
  füllt `Render` zusätzlich ein **transparentes Rechteck** über `Bounds`.
- **Verifiziert:** Werkzeugleiste + Canvas rendern korrekt (`AV3-pen.png`). ⚠️ **Das eigentliche
  Zeichnen ist NICHT getestet** — programmatische Mausklicks kommen in dieser Umgebung nicht am
  Fenster an (Foreground-Sperre). **Vom Nutzer zu prüfen.**
**Schritt 4b (Teil 2): Werkzeuge, Radierer, Undo/Redo, Zoom/Pan (2026-07-24).**
- **Werkzeugauswahl** über `ToolProperty` (`Models.ToolType`): Stift / Bleistift / Textmarker
  (setzen `StrokeKind` und beim Marker die 5-fache Breite) / **Radierer** / **Hand**.
- **Radierer:** **punktgenau** (Nutzer-Meldung „radiert immer ganze Linien" behoben). Neue
  plattformneutrale Klasse **`GonkNote.Core/Editing/WbErase.cs`** (`SplitStroke`,
  `SegmentDistance`, `HitsStroke`, `HitsOther`) — aus der WPF-Logik gehoben: Striche werden am
  Radierkreis **aufgetrennt**, die Reststücke bleiben stehen; Formen/Text/Zettel gehen als Ganzes,
  Bilder bleiben (wie in WPF). Ein Radier-Zug = **ein** Undo-Schritt via `PartialEraseAction`.
- **Undo/Redo** über den **Core-`UndoStack`**: `AddElementsAction` je Strich,
  `RemoveElementsAction` je Radier-Zug; Stack liegt im `ShellViewModel` **je Dokument** (wird beim
  Laden neu erzeugt). Nach Undo/Redo wird gespeichert und der Canvas neu gezeichnet.
- **Zoom/Pan:** Mausrad zoomt (0,2×–8×), Hand-Werkzeug **oder mittlere Maustaste** verschiebt;
  `PanX`/`PanY` gehen als `canvas.Translate` in die Draw-Operation. Bildschirm→Seite über
  `ToPage()`. Das Seitenmuster rastet aufs **Seiten**raster (sonst wandert es beim Pan).
- Werkzeugleiste erweitert (Werkzeuge · Farben · Breiten · Rückgängig/Wiederholen · 100 %).
- **Verifiziert:** Undo exakt — zwei Teststriche angelegt, **einmal** rückgängig: der erste
  (magenta) bleibt sichtbar, der zweite (cyan) verschwindet (`AV3-undo2.png`).
  ⚠️ Zeichnen/Radieren/Pan **mit echter Maus/Stift weiterhin ungetestet** (Foreground-Sperre).
**Schritt 4b (Teil 3): Fixes aus dem Nutzer-Test (2026-07-24).**
- **Radierer radierte ganze Linien** → jetzt punktgenau (s. o., `WbErase.SplitStroke`).
- **Bleistift sah aus wie ein Stift mit zackigen Kanten** → neue `WbRenderer.DrawPencil`:
  Graphit-Anmutung in **drei günstigen Skia-Durchgängen** statt einer `CreateDiscrete`-Zackenlinie —
  halbtransparente Kernlinie (Alpha 80), fein aufgerauter Rand (kleine Discrete-Amplitude) und eine
  **gestempelte Körnung** (`Create1DPath`-Punkte auf einem leicht verrauschten Pfad via
  `CreateCompose`). Wirkt auf WPF **und** Avalonia, da geteilter Renderer.
- Aktives Werkzeug wird in der Leiste hervorgehoben (`Button.active`).
- **Verifiziert per Screenshot** (`AV3-pencil.png`): Bleistift-Linie deutlich körnig gegenüber der
  Stift-Linie; aufgetrennter Strich zeigt die Lücke mit stehen gebliebenen Enden.
**Schritt 4b (Teil 4): restliche Werkzeuge (2026-07-24).**
- **Auswahl/Verschieben (`Move`)**: Klick greift das oberste Element (`TopElementAt`, nutzt
  `WbErase.HitsStroke/HitsOther`), Ziehen verschiebt es (`WbElement.Translate`); ein Zug =
  **ein** Undo-Schritt (`MoveElementsAction`).
- **Lasso**: freies Polygon, Auswahl über Punkt-in-Polygon (Ray-Casting) auf den Element-
  Mittelpunkt; Klick **in** eine bestehende Auswahl verschiebt sie stattdessen. Gestrichelte
  Lasso-Spur + Auswahlrahmen werden als **transientes Overlay** gezeichnet (nicht Teil der Seite,
  Strichbreite zoom-kompensiert).
- **Formen** (`Shape` + `ComboBox`: Rechteck/Ellipse/Linie/Pfeil/Dreieck): Aufziehen mit
  Live-Vorschau, beim Loslassen als `ShapeElement` übernommen (Fehlklicks < 2 px verworfen).
- **Notizzettel** (`Sticky`): Klick platziert eine 200×160-Karte. **Text** (`Text`): Klick öffnet
  den neuen Mini-Dialog **`TextPrompt`** (Code-only, feste Breiten → umgeht den Quirk aus §9.5).
- **Auswahl löschen** per Button oder **Entf**-Taste (`RemoveElementsAction`).
- **Pinch-Zoom** über `Gestures.AddPinchHandler` (Skalierung relativ zum Zoom bei Gestenbeginn).
- **Mehrseitigkeit**: `‹ / ›`-Navigation, `＋ Seite` (übernimmt Format/Muster der aktuellen Seite),
  Anzeige „Seite X / Y". **Undo-Verlauf gilt je Seite** (beim Wechsel neuer `UndoStack`).
- **Robustheit:** Das Zeichnen jedes Elements läuft in `try/catch` — Custom-DrawOperations
  verschlucken Ausnahmen sonst still, ein defektes Element hätte das ganze Bild gekostet.
- **Verifiziert per Screenshot** (`AV3-tools-final.png`): Rechteck, Ellipse, Pfeil, Notizzettel
  (mit Schatten + Textumbruch) und Text-Element rendern korrekt.
- ⚠️ **DPI-Falle beim Testen** (kostete hier Zeit): Avalonia rechnet in **DIPs**. Auf dem
  Testrechner (200 %) ist der sichtbare Canvas nur ~410×290 DIP — Testkoordinaten > ~400 liegen
  außerhalb und wirken wie ein Render-Bug. Erst prüfen, bevor man Fehler in `WbRenderer` sucht.
**Schritt 4b (Teil 5): Skalieren, Zwischenablage, Bild-Import (2026-07-24).**
- **Skalieren der Auswahl**: vier **Eckgriffe** am Auswahlrahmen (weiß mit Akzentrand,
  bildschirmgroß via `1/Zoom`). Ziehen skaliert um die **gegenüberliegende Ecke** als Pivot
  (`HandleAt` liefert sie); Faktor = Abstandsverhältnis, geklemmt auf 0,05–20. Der Zug wird
  inkrementell angewandt, aber als **ein** `ScaleElementsAction` gebucht. Griffe haben Vorrang
  vor Verschieben/Lasso.
- **Zwischenablage** (intern, wie in der WPF-App): **Strg+C / Strg+V / Strg+D** und Buttons.
  `CloneElement` macht echte Tiefkopien je Typ; Einfügen versetzt um 18 px und wählt das
  Eingefügte aus, mehrfaches Einfügen versetzt weiter. Ein `AddElementsAction` je Vorgang.
- **Bild-Import**: Button (`StorageProvider.OpenFilePickerAsync`, Mehrfachauswahl) **und
  Drag&Drop** (`DragDrop.AllowDrop` am Fenster, Filter auf Bildendungen). Das Bild wird auf
  max. 340 px lange Kante skaliert, in der Ansichtsmitte platziert, ausgewählt, und das
  Werkzeug wechselt auf **Auswahl** → sofort verschieb-/skalierbar.
- **Verifiziert per Screenshot** (`AV3-scale-img.png`): Bild-Element rendert, Auswahlrahmen
  gestrichelt, vier Eckgriffe korrekt platziert.
**Schritt 4b (Teil 6): Ausschneiden, Drehen, Finger-Gesten, Seitenmuster, Sticker (2026-07-24).**
- **Ausschneiden** (Strg+X) und **Duplizieren** als Buttons nachgereicht (fehlten in der Leiste).
- **Drehen**: runder Dreh-Griff mittig über der Auswahl (mit Verbindungslinie), **nur bei
  Einzelauswahl**. Ziehen dreht um den Elementmittelpunkt, **Shift rastet auf 15°**;
  ein Zug = ein `RotateElementAction`. Griff-Reihenfolge: Drehen → Skalieren → Verschieben.
- **Finger-Gesten** (rohe Touch-Kontakte selbst verfolgt, wie in der WPF-App, `PointerType.Touch`):
  **1 Finger** schiebt die Leinwand · **2 Finger** zoomen um die Fingermitte **und** schieben
  zugleich · **3 Finger tippen** = rückgängig (`_maxTouches` merkt sich die höchste Fingerzahl).
  Ab dem zweiten Finger wird eine laufende Zeichnung/Auswahl **abgebrochen**, damit die Geste
  sauber übernimmt. Stift und Maus laufen unverändert durch die Werkzeuglogik.
- **Seitenmuster + Farbton** über zwei ComboBoxen (Blanko/Linien/Raster/Punkte · Hell/Dunkel);
  wirkt auf die aktuelle Seite, wird sofort gespeichert. `SyncPagePickers` gleicht die Auswahl
  beim Seiten-/Dokumentwechsel an (`_syncingPickers`-Flag verhindert Rückschreiben).
- **Sticker-Werkzeug**: neuer `StickerPicker` (Code-only-Dialog) liest dieselben Quellen wie die
  WPF-App — `Assets/Stickers` neben der Exe **und** `%APPDATA%\GonkNote\Stickers` (Linux:
  `~/.config/GonkNote/Stickers`), rekursiv, Endungen png/jpg/jpeg/webp. Klick fügt den Sticker
  als Bild-Element ein (gleicher Pfad wie der Bild-Import). Ohne Sticker: Hinweis mit den Pfaden.
- **Verifiziert per Screenshot:** Dreh-Griff an gedrehtem Zettel (`AV3-rotate.png`),
  Linien-Muster hell (`AV3-pattern-lines.png`) und **Punkte-Muster auf dunkler Seite**
  (`AV3-pattern-dark.png`, zugleich Beleg für die Mehrseitigkeit „Seite 3 / 3").
- ⚠️ **Nicht getestet (braucht Hardware/Interaktion):** die **Finger-Gesten** (Touch-Gerät nötig)
  und der Sticker-Dialog (in der Testumgebung liegen keine Sticker-Dateien neben der Exe).
**Schritt 4b (Teil 7): Rotations-Fix + PDF-Import (2026-07-24).**
- **Fix (Nutzer-Test): Auswahlrahmen drehte nicht mit.** Das Overlay (Rahmen, Eckgriffe,
  Dreh-Griff) wird jetzt um denselben Mittelpunkt gedreht wie das Element. Damit die Griffe
  weiter treffen, rechnen `HandleAt`/`IsOnRotateHandle` **und die Skalierung** den Zeigerpunkt
  über **`ToLocal()`** in den ungedrehten Raum zurück — dort liegt auch der Skalier-Pivot, den
  `WbElement.Scale` erwartet. Bei Mehrfachauswahl bleibt der Rahmen bewusst achsenparallel.
  *(Damit ist die in §5 als „vertagt" notierte WPF-Näherung im Avalonia-Port gelöst.)*
- **Fix (Nutzer-Test): Sticker-Ordner existierte nicht.** `StickerPicker.UserDir` **legt ihn an**;
  der Dialog hat jetzt einen Knopf **„＋ Sticker hinzufügen…"** (Dateiauswahl, kopiert
  kollisionssicher — „ (2)", „ (3)" …). Der Leerzustand erklärt beide Quellen mit vollem Pfad.
- **PDF-Import**: `PdfImporter` nach **`GonkNote.Core/Services/`** verschoben (ist reines
  Docnet+Skia, kein WPF) — `Docnet.Core` ist jetzt Core-Abhängigkeit. Button **📕 PDF** und
  **Drag&Drop**: Seiten werden mit 1400 px langer Kante gerendert (**im Hintergrund-Thread**,
  Fortschritt „PDF-Seite X / Y" in der Leiste) und **zweispaltig** als Bild-Elemente eingefügt;
  ein `AddElementsAction` für alles, danach Werkzeug = Auswahl.
- **Verifiziert per Screenshot:** Rahmen/Griffe liegen exakt auf dem 30°-Zettel
  (`AV3-rotframe.png`); PDF-Seiten 1–2 des Testdokuments scharf im Raster (`AV3-pdf.png`).
- 🐧 **Linux-relevant:** Die nativen PDFium-Bibliotheken liegen im Build-Output für **alle** RIDs
  (`runtimes/linux-x64/native/pdfium.so` usw.) — PDF-Import sollte unter Linux ohne Zusatzarbeit
  laufen.
**Schritt 4b (Teil 8): Zeichenhilfen Lineal + Geodreieck (2026-07-24).**
- **Geometrie nach Core gehoben:** neue **`GonkNote.Core/Editing/WbDrawAid.cs`** — Polygon,
  Achsen, Kanten-Einrasten (`TryActivateSnap`/`ApplySnap`), Dreh-Griff, 15°-Winkelrastung.
  Konstanten wie in WPF (`PxPerCm=37,795`, Lineal 680×52, Geodreieck 16 cm).
- **Rendering nach Core:** neue **`GonkNote.Core/Rendering/WbAidRenderer.cs`** zeichnet Lineal
  (Glasfläche + cm-Skala) und Geodreieck. Die **SVG-Assets des Nutzers** sind jetzt in
  **Core** eingebettet (`LogicalName=GonkNote.Core.Assets.Geodreieck-*.svg`, Dateien bleiben
  in `Assets/`); `Svg.Skia` ist Core-Abhängigkeit. **WPF leitet `DrawSetSquare` dorthin weiter**
  → eine Wahrheit, `WhiteboardView.xaml.cs` 4077 → **4013 Zeilen**.
- **Avalonia:** Buttons **📏 Lineal** / **📐 Geodreieck** (aktiver Zustand hervorgehoben).
  Die Hilfe lässt sich **verschieben** (Körper ziehen) und **drehen** (Griff, 15°-Rastung);
  beides hat Vorrang vor den Werkzeugen. Beim Zeichnen rastet der Strich an der nächsten Kante
  ein (`AddPoint` projiziert über `ApplySnap`), das Einrasten endet mit dem Strich.
- **Zwei Fehler dabei gefunden und behoben:**
  1. **Doku-Platzhalter lag über dem Whiteboard** — `ShowDocument` wurde gemeldet, *bevor*
     `LoadBoardPage()` die Seite setzte (und `ShowWhiteboard` davon abhängt). Reihenfolge
     korrigiert.
  2. **`ViewCenter()` bei ungemessenem Control** lieferte (0,0) → Hilfen landeten am Rand.
     Fällt jetzt auf eine Ersatzgröße zurück.
- **Verifiziert per Screenshot:** Geodreieck rendert **vollständig aus den SVG-Assets**
  (Gradskala, Winkellinien, pinkes Band, cm-Skala — `AV3-setsquare4.png`); Lineal mit
  cm-Skala und Akzentkante (`AV3-ruler.png`).
- **Offen (4b-Rest):** Notizbuch-Cover.

### 9.4 Gestaffelter Plan
1. ✅ **PoC/Scaffold + Kernlogik-Reuse** (fertig, §9.1).
2. ✅ **`GonkNote.Core` extrahiert** (Models + DB + Undo + ImageCache); WPF + Avalonia
   referenzieren es per ProjectReference; Links im Avalonia-Projekt entfernt (fertig, §9.1b).
3. 🟡 **Shell portieren** (Nutzer-Priorität 2026-07-23) — **3a + 3b-Galerie erledigt (§9.3c/§9.3d)**:
   Ordnerbaum + Farbvererbung + Navigation + Theme (3a); **„Big-Picture"-Galerie** (3b) — rechter
   Bereich zeigt Ordnerinhalt als farbige Kacheln, Kachelklick navigiert; **+ Breadcrumb,
   Inline-Umbenennen, Neu-Anlegen/Löschen, Favoriten** (alles DB-persistent). **Bewusst offen:**
   echte Doku-Tabs (sinnvoll erst mit echten Ansichten ⇒ Schritt 4/5), Drag&Drop, Anpinnen-Kacheln.
4. 🟡 **Whiteboard** — **4a erledigt (§9.3e):** Zeichenroutinen als `WbRenderer` nach Core gehoben,
   Avalonia-`WhiteboardCanvas` rendert Seiten über Avalonias eigenen Skia-Canvas. **Offen (4b):**
   Stylus/Touch über Avalonia-Pointer-Events, Werkzeuge, Zoom/Pan, Speichern; WPF auf `WbRenderer`
   weiterleiten (De-Duplizierung).
5. 🟡 **Text-Editor: Ansatz entschieden, Bau zurückgestellt** (Prototyp fertig, §9.3b):
   **Markdown (Ansatz b) empfohlen**. **Nutzer-Entscheidung 2026-07-23: Editor-Bau vorerst
   zurückstellen** — erst Shell (3) + Whiteboard (4) portieren, Editor-Scope später festlegen.
6. **Plattform-Teile**: Fenster-Chrome, Hunspell-Rechtschreibung, OCR/PDF mit Linux-Native-Libs.
7. **Auf echtem Linux** bauen/testen (X11 + Wayland), `dotnet publish -r linux-x64`.

### 9.5 Fallstricke / Notizen
- **`net8.0` (nicht `-windows`)** für Core + Avalonia — sonst kein Linux.
- **`ImplicitUsings` müssen übereinstimmen:** die verlinkten Kernklassen nutzen implizite Usings
  (System.Linq etc.) → Avalonia-Projekt hat `ImplicitUsings=enable`; im Core-Projekt genauso.
- **WPF-csproj-Ausschluss** für `GonkNote.Avalonia\**` beibehalten, solange beide im selben
  Ordnerbaum liegen.
- **Bisher nur auf Windows entwickelt/verifiziert** — der echte Linux-Build/-Test steht aus
  (kein Linux in dieser Umgebung).
- **DockPanel-Fill / Grid-Star bekommen unendliche Messbreite** (Avalonia @ 200 % DPI, Windows) —
  der Kern des „Layout-Quirks". Eingegrenzt (Runde 20, LayoutProbe-Wegwerftests): ein
  **vertikales StackPanel** gibt Kindern endliche Breite (Umbruch ok), aber das **LastChildFill-
  Kind eines DockPanels** und **Grid-`*`-Spalten/Zeilen** werden mit unendlicher Breite gemessen
  (Text bricht nicht um, `MaxWidth` greift nicht, zentrierter Inhalt wird rausgeschoben).
  **Fixe Breiten (Dock=Left Width=…, Grid-Pixelspalten) sind unbetroffen** → Workaround für die
  Shell: feste Panelbreiten statt Fill/Star. **`Avalonia 11.2.3`-Bump getestet → behebt es NICHT**
  (also kein reiner Versionsbug; vmtl. DPI-spezifisch). Beim echten **Linux-Build** gegenprüfen
  (dort evtl. gar nicht vorhanden). Auch die multiline-`TextBox` (ignoriert `TextWrapping=Wrap`)
  hängt vermutlich an derselben Ursache.
- **`Markdown.Avalonia`-Style `"Standard"` ist nicht dark-mode-aware** (Überschriften unsichtbar,
  Tabelle hell) → eigener dunkler Markdown-Style beim Editor-Bau nötig (§9.3b).
- Reihenfolge laut Roadmap (§5/§6): eigentlich Cleanup → i18n → RAM → GitHub; der Port wurde auf
  **ausdrücklichen Nutzer-Wunsch vorgezogen begonnen** (nur Scaffold). Die Hauptumsetzung verzahnt
  sich sinnvoll mit dem Cleanup (die `GonkNote.Core`-Extraktion ist beides zugleich).
