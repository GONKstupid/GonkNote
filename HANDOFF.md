# Gonk Note — Projektübergabe (Stand: 2026-07-24, Phase 3 laufend)

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
> **Runde 24 (2026-07-26) zuletzt: Render-Caching (vom Nutzer vorgezogen).**
> Während ein Strich gezogen wird, ändert sich am bereits Gezeichneten nichts — die Punkte
> laufen bis zum Absetzen in `_activePoints`, nicht in die Seite. Trotzdem wurde pro Bild die
> ganze Seite neu gerastert. Jetzt wird der fertige Stand **einmal** in ein Zwischenbild
> gerendert und danach nur kopiert (`WhiteboardView.Render.cs`, Abschnitt „Zwischenbild").
> Gemessen (Harness `%TEMP%\gonk-cache`): 25 Striche 14,7 → 2,1 ms · 100 Striche 56,8 → 3,1 ms ·
> 300 Striche 167,7 → 5,5 ms · 50 Bleistift-Striche 62,5 → 2,6 ms. **Die Kosten hängen nicht
> mehr am Seiteninhalt.** Im echten Fenster gegengeprüft: die Zeichenfläche während des Strichs
> ist **pixelgleich** mit dem frisch gezeichneten Stand danach (0 abweichende Pixel).
>
> **Die Invariante, auf der das steht:** gecacht wird nur, solange eine Interaktion **nur das
> Overlay** verändert — `ContentFrozen` = Strich zeichnen, Form aufziehen, Lasso, Zeichenhilfe
> schieben. Radieren, Verschieben, Skalieren und Drehen ändern den Inhalt und zeichnen weiterhin
> jedes Bild neu. Deshalb kann das Zwischenbild nicht stillschweigend veralten — es braucht
> keine Invalidierungs-Aufrufe, die man vergessen könnte.
> Speicher: ein Bild in Viewport-Größe (~20 MB bei Vollbild), nur während der Geste; danach
> sofort freigegeben.
>
> **Nächster Schritt: RAM-Optimierung. Zielwert vom Nutzer bestätigt (2026-07-26): 800 MB,
> harte Obergrenze 1 GB.**
>
> **Runde 23 (2026-07-26): Cleanup abgeschlossen + zweite Sprache (DE/EN).**
> - **Namensräume der Kernbibliothek** heißen jetzt `GonkNote.Core.*`. Vorher bezeichnete
>   `GonkNote.Services` zwei verschiedene Dinge (4 UI-freie Klassen in Core, 11 WPF-nahe in der
>   App) — am `using` war die Schichtgrenze nicht zu sehen. **Wichtig:** der `ModelTypeBinder`
>   übersetzt jetzt Assembly **und** alten Namensraum; ein Harness prüft alle drei historischen
>   `_type`-Formate (`%TEMP%\gonk-dbfix`).
> - **`TestAssets/` gelöscht** (Nutzer-Entscheidung). Die 60–85-Zeilen-Methoden bleiben stehen —
>   flache `switch`-Verteiler, die `coding-style` Kapitel 6 erlaubt (Nutzer-Entscheidung).
> - **Zahlenblock repariert:** er schloss sich beim ersten Tastendruck, weil der Popup-Inhalt in
>   einem eigenen Fenster mit eigenem Visual-Baum liegt und die „außerhalb geklickt?"-Prüfung ihn
>   nie erreichte. Öffnet jetzt einheitlich per Langdruck auf Icon/Schieber/Wertanzeige.
> - **Zweite Sprache (DE/EN)** unter „Ansicht → Sprache", zur Laufzeit, ohne Neustart:
>   `Services/Localization/` mit `Loc` + je einer Tabelle pro Sprache (473 Schlüssel).
>   In XAML `{loc:T Schlüssel}`, im Code `Loc.T("Schlüssel")`. Fehlt ein englischer Eintrag,
>   erscheint der deutsche Text. Details in §10.
> - **`ImageCache.Get` liefert jetzt null statt zu werfen**, wenn Bilddaten fehlen — ein einziges
>   kaputtes Bild ließ vorher die ganze Seite leer.
>
> **Runde 22 (2026-07-26): Nutzer-Test nach dem Port-Rückbau — drei Fixes + Cleanup-Start.**
> - **Absturz beim Öffnen von Bestands-Whiteboards/-Notizbüchern behoben** (die eigentliche
>   Ursache der drei Abstürze des Nutzers): LiteDB legt für jedes Whiteboard-Element ein Feld
>   `_type` = „Namensraum.Typ, **Assembly**" ab. Mit dem Umzug der Models nach `GonkNote.Core`
>   änderte sich der Assembly-Teil (`GonkNote` → `GonkNote.Core`) → `LiteException` beim Laden,
>   unbehandelt → App weg. Neuer `ModelTypeBinder` in `DatabaseService` löst Typnamen jetzt
>   **unabhängig von der Assembly** auf. Verifiziert mit einer DB im alten Format (Harness +
>   echte Exe). **Merksatz: Models nie umziehen, ohne an das `_type`-Feld zu denken.**
> - **Trägheit: Bleistift war der Bremsklotz.** Der Perlin-Shader wurde pro Strich und Bild
>   dreimal neu ausgewertet (~100 ms/Bild bei 50 Strichen ≈ 10 fps). Die Körnung wird jetzt
>   **einmal in eine kachelbare Textur** gerendert (`WbRenderer.GrainTexture`) → 1,7–1,9× schneller
>   bei identischer Optik (Sichtvergleich 1× und 3× Zoom).
> - **Radierergröße** über Größen-Schieber bzw. Zahlenblock einstellbar, eigener Wert je
>   Werkzeug (Strichstärke der Stifte bleibt erhalten), Radierkreis wächst sofort mit.
> - **Unerwartete Fehler beenden die App nicht mehr kommentarlos** — sie landen in
>   `%APPDATA%\GonkNote\fehler.log` und werden einmal pro Sitzung gemeldet (`App.OnDispatcherError`).
> - **Cleanup begonnen** (Vorgabe: Torvalds' `coding-style`, §5): `WhiteboardView.xaml.cs`
>   (4007 Zeilen) nach Themen in partials geteilt, `Views.Fonts`-Hülle entfernt, 60 unnötige
>   usings weg, keine Methode mehr über 100 Zeilen (vorher 4), **Build 0 Warnungen**.
>   Nebenbei gefunden und behoben: der DOCX-Export erzeugte für Tabellen ohne feste
>   Spaltenbreiten ungültiges OOXML (fehlendes `tblGrid`) — die Validierung meldete das
>   dem Nutzer nach jedem Export.
>
> **Runde 21 (2026-07-24): Linux-Port abgebrochen, Projekt aufgeräumt.**
> Der Nutzer hat sich die Entwicklung des Avalonia-Ports angesehen und ihn **abgebrochen**.
> `GonkNote.Avalonia/` ist **vollständig entfernt**; Gonk Note bleibt eine reine WPF-App.
> **Behalten wurde, was der App unabhängig davon nützt** (= der in §5 geforderte Cleanup):
> - **`GonkNote.Core/`** (`net8.0`, kein WPF): Models, `DatabaseService`, `UndoStack`,
>   `ImageCache`, `PdfImporter`, Zeichenroutinen (`WbRenderer`), Geodreieck-Renderer
>   (`WbAidRenderer`), Radier-Geometrie (`WbErase`). **Jede** Klasse wird von der WPF-App
>   genutzt — kein toter Code.
> - **De-Duplizierung:** `WhiteboardView` leitet an Core weiter statt eigener Kopien →
>   **~440 Zeilen** Doppelcode weg (4457 → ~4010).
> - **Verbesserter Bleistift** (echte Graphit-Körnung nach Nutzer-Referenzbild).
>
> Verifiziert: WPF baut 0 Fehler (nur die 6 bekannten `CS8622` aus `Numpad.cs`),
> **Single-File-Publish erzeugt weiterhin eine 79,6-MB-`GonkNote.exe`** (publish-Ordner sauber:
> Exe + Assets + tessdata). Details in **§9**.
>
> **Runden 19–20 (2026-07-23/24): Avalonia-Port** — mit Runde 21 rückgängig gemacht,
> siehe §9. (Die Zwischenstände sind nur noch in der Git-Historie.)
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
- **Zwei Projekte:** `GonkNote.csproj` (WPF-App, `net8.0-windows`) referenziert
  `GonkNote.Core/GonkNote.Core.csproj` (Kernlogik, `net8.0`). Siehe §3.
- Build: `dotnet build` fehlerfrei; **6 bekannte `CS8622`-Warnungen** in
  `WhiteboardView.Numpad.cs` (im Cleanup zu beheben, §5). Single-File-Publish geprüft.
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
├─ ViewModels/                Mvvm-Basis, MainViewModel (Baum, Tabs, Autosave 30s,
│                             Pin/Favorit, DOCX-Import), TreeItemViewModel, Tab-VMs
├─ Views/
│  ├─ WhiteboardView.xaml(.cs)   SkiaSharp-Canvas. Die Datei war 4007 Zeilen lang und ist
│  │                             seit dem Cleanup nach Themen in partials geteilt:
│  │      .xaml.cs      (724)     Felder, Werkzeuge/Toolbar, Zoom/Pan, Tastatur, Seiten
│  │      .Input.cs     (~690)    Stift/Maus/Finger, Radierer
│  │      .Import.cs    (~480)    Bilder, PDF, DOCX, Zwischenablage, Drag&Drop
│  │      .Render.cs    (~450)    Seitenhintergrund, Cover, Elemente, Overlays, Culling
│  │      .Selection.cs (~390)    Treffer-Erkennung, Lasso, Griffe, Kopieren/Einfügen
│  │      .Settings.cs  (~380)    Einstellungs-Seitenleiste (Seite/Formen/Text/Zettel)
│  │      .Shapes.cs    (~310)    Formen-Stift-Erkennung
│  │      .Aids.cs      (~300)    Lineal + Geodreieck
│  │      .QuickMenu.cs (~260)    Schnellaktionen (floatende Icon-Leiste)
│  │      .Covers.cs / .Stickers.cs / .Editing.cs / .Numpad.cs / .Ocr.cs
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
├─ Services/                  (WPF-nahe Dienste — alles mit FlowDocument-/UI-Bezug;
│                             Namensraum `GonkNote.Services`)
│  ├─ DocxImporter.cs         DOCX → FlowDocument → XamlPackage
│  ├─ DocxExporter.cs         FlowDocument → DOCX (OpenXML, Gegenrichtung)
│  ├─ MarkdownExporter.cs     FlowDocument → Markdown (best-effort)
│  ├─ PdfExporter.cs          Whiteboard→SKDocument, Text→Paginator-Raster→PDF
│  ├─ TextStyles.cs           Zentrale Formatvorlagen/Seitenformate/Heading-Erkennung/
│  │                          Ink-Normalisierung des Text-Editors (eine Wahrheit für
│  │                          Editor, TOC, PDF-/DOCX-Export, Import, Markdown)
│  ├─ OcrService.cs           Tesseract-Anbindung (Text aus Bildern/PDF-Seiten)
│  ├─ ThemeService.cs, TitleBarTheme.cs, WindowBounds.cs, SpellCheckSupport.cs
├─ Themes/                    Light/Dark.xaml, Styles.xaml (inkl. Vektor-Icons)

GonkNote.Core/                Kernbibliothek OHNE UI-Bezug (net8.0, kein WPF).
                              Namensraum durchgehend `GonkNote.Core.*` — am Namen im
                              `using` ist damit sofort zu sehen, auf welcher Seite der
                              Schichtgrenze man steht (Cleanup Runde 22).
├─ Models/                    NoteItem (IconColor/IsPinned/IsFavorite),
│                             Whiteboard.cs: WbPage (inkl. BackgroundImage/-Id für
│                             PDF-Seiten), Elemente (Stroke/Shape/Text/Image/
│                             StickyNote), IBoxElement, Enums (ToolType), CoverStyle,
│                             WhiteboardDoc, PageTemplate, TextDoc
├─ Services/
│  ├─ DatabaseService.cs      LiteDB (items/boards/texts/settings), --db-fähig
│  ├─ ImageCache.cs           Byte-Budget-Cache (96 MB) dekodierter Bilder
│  ├─ UndoStack.cs            Undo/Redo (PartialErase/Resize/Rotate/Scale-Actions)
│  └─ PdfImporter.cs          PDF → JPEG-Seiten via Docnet.Core/PDFium
├─ Rendering/
│  ├─ WbRenderer.cs           **Alle** Skia-Zeichenroutinen des Whiteboards
│  │                          (Stroke/Shape/Text/Image/Sticky, Bounds, Schatten-
│  │                          Cache, Textumbruch, Graphit-Bleistift). WhiteboardView
│  │                          und der PDF-Export leiten hierher → eine Wahrheit.
│  └─ WbAidRenderer.cs        Geodreieck aus den eingebetteten Nutzer-SVGs
└─ Editing/
   └─ WbErase.cs              Punktgenaues Radieren (Strich am Radierkreis auftrennen)
```

**Warum zwei Projekte:** Die Kernlogik ist bewusst von der Oberfläche entkoppelt (Leitlinie
§5). `GonkNote.Core` kennt kein WPF — das hält die Schichten sauber, entfernte ~440 Zeilen
doppelte Zeichenroutinen und erleichtert die geplante i18n- und RAM-Arbeit.

**⚠️ Beim Verschieben von Models an das `_type`-Feld denken:** LiteDB speichert für jedes
Whiteboard-Element „Namensraum.Typ, Assembly". Ändert sich eines von beiden, lassen sich
**alle** Bestandsdokumente nicht mehr öffnen. Die Übersetzung alter Namen steht an genau einer
Stelle: `ModelTypeBinder` in `DatabaseService`. Er deckt heute ab: `…, GonkNote` (vor dem
Core-Umzug), `GonkNote.Models.*` (vor der Namensraum-Umbenennung) und den aktuellen Stand.
Ein Harness prüft alle drei Formate (`%TEMP%\gonk-dbfix`).
*(Das Projekt entstand als Nebenprodukt des abgebrochenen Linux-Ports, siehe §9.)*

Pakete — **GonkNote (WPF):** SkiaSharp.Views.WPF, DocumentFormat.OpenXml (DOCX),
Tesseract (OCR) · **GonkNote.Core:** LiteDB, SkiaSharp, Docnet.Core (PDFium),
Svg.Skia (SVG-Rasterung).

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
  - **Code-Cleanup / Projekt aufräumen (vor der RAM-Optimierung, Nutzer-Wunsch) — teilweise
    erledigt:**
    - ✅ **Kernlogik von den Views entkoppelt** (2026-07-24): Models/DB/Undo/Bild-Cache/
      PDF-Import liegen in **`GonkNote.Core`** (kein WPF), siehe §3.
    - ✅ **Doppelte Zeichenroutinen zusammengeführt**: `WhiteboardView` leitet an
      `WbRenderer`/`WbAidRenderer`/`WbErase` weiter — **~440 Zeilen** weniger (4457 → ~4010).
    - ✅ **Runde 22 (2026-07-26), Leitlinie = Torvalds' `coding-style.rst`:**
      6 `CS8622`-Warnungen weg (**Build 0 Warnungen**); `WhiteboardView.xaml.cs` (4007 Zeilen)
      nach Themen in partials geteilt (größte Datei jetzt ~690 Zeilen); keine Methode mehr
      über 100 Zeilen (vorher `ToFlowDocument` 140, `DrawActiveOverlays` 127, `BeginInput` 125,
      `BuildTable` 109); tief verschachtelte Blöcke in benannte Helfer gezogen (Zeilen mit
      5+ Einrückungsebenen 561 → 434); wiederholte 8-teilige Bedingung als `InputInProgress`
      benannt; 60 unnötige usings entfernt; leerer Ordner `Models\` gelöscht;
      Hülle `Views.Fonts` entfernt (kollidierte mit `System.Windows.Media.Fonts`).
    - ⬜ **Offen:** Restliche Methoden im Bereich 60–85 Zeilen (`RenderTextPages`,
      `ConvertTable`, `MoveInput`, `DrawRuler`, `BuildImage`, `ExportActiveTab` …) prüfen —
      die meisten sind lange, aber flache `switch`-Verteiler, die Torvalds ausdrücklich
      erlaubt (Nutzer-Entscheidung 2026-07-26: **nicht anfassen**). Außerdem offen: ~155 Zeilen
      über 110 Zeichen, weitere Doppelungen suchen. **Verhalten unverändert lassen** (reines
      Aufräumen); nach jedem Schritt Build 0 Warnungen + kurzer Sichttest.
  - **Zweite Sprache (DE/EN, i18n): ✔ umgesetzt am 2026-07-26 — siehe §10.** Der ursprüngliche
    Plan lautete: Umschaltung
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
    - **`TestAssets/`** ist am 2026-07-26 gelöscht (war ohnehin in `.gitignore`).
    - Keine echten Notizdaten/Namen im Repo **oder in der History** (echte DB liegt in %APPDATA%).
    - Keine Segoe-Font-Dateien einchecken (App nutzt System-Font – bundlet keine `.ttf`).
  - **Cross-Platform (Linux): am 2026-07-24 vom Nutzer abgebrochen.** Der Avalonia-Port ist
    komplett aus dem Repo entfernt; Gonk Note bleibt eine **reine Windows-/WPF-App**. Was aus
    dem Versuch geblieben ist (`GonkNote.Core`, De-Duplizierung, Bleistift-Textur) und was die
    Erfahrung war, steht in **§9**. Nicht erneut anfangen, ohne dass der Nutzer es ausdrücklich
    wünscht.
  - **Nützlich unabhängig davon:** die WPF-Rechtschreibung hängt am Windows-Sprachpaket
    (Englisch fehlt auf dem Testrechner, s. Runde 18). **WeCantSpell.Hunspell** (pure C#) würde
    das lösen — eigenständig sinnvoll, nicht nur für einen Port.
  - **RAM-Leitlinie: „Features vor RAM".** Zielwert **800 MB** — am 2026-07-26 vom Nutzer
    bestätigt (die früher notierten „unter 80 MB" waren ein Tippfehler). Die **harte, nie zu
    überschreitende Obergrenze ist 1 GB**. RAM ist ausdrücklich zweitrangig — kein Feature
    dafür opfern.
  - **Render-Caching: ✔ umgesetzt am 2026-07-26** (vom Nutzer vor die RAM-Optimierung
    gezogen, nachdem der Bleistift-Fix die Trägheit nur zur Hälfte erklärt hatte).
    Einzelheiten oben in der Runde-24-Notiz.
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
   2026-07-23, Details in §5). **Kernlogik-Entkopplung (`GonkNote.Core`) und die
   De-Duplizierung der Zeichenroutinen sind am 2026-07-24 erledigt.** Offen bleiben:
   `CS8622`-Warnungen beheben, große partials entwirren, weitere Doppelungen/Altlasten —
   **Verhalten unverändert**, Build 0 Warnungen.
3. **Zweite Sprache (DE/EN, i18n)** — vom Nutzer **nach dem Cleanup** gewünscht
   (2026-07-23): „Ansicht → Sprache", zur Laufzeit umschaltbar (§5).
4. **RAM-Optimierung** (Details/Schwellenwerte in §5, „Nutzer-Strategie"). **Vorher
   den 1-GB-/800-MB-Wert einmal rückversichern** und die Leitlinie „Features vor RAM" beachten.
5. **GitHub-Veröffentlichung (MIT) — NACH fertiger RAM-Optimierung** (Nutzer-Wunsch
   2026-07-23): `LICENSE` (MIT), **alle Sticker löschen inkl. Git-History** (keine Lizenz),
   **Cover bleiben** (selbst erstellt), `TestAssets/` ist bereits gelöscht. Volle Checkliste in §5.
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
dotnet build GonkNote.csproj                        # zieht GonkNote.Core mit
./bin/Debug/net8.0-windows/GonkNote.exe            # Debug-Start (ECHTE DB!)
./bin/Debug/net8.0-windows/GonkNote.exe --db X.db  # Test-DB (fuer UI-Tests)
dotnet publish GonkNote.csproj -c Release           # Single-File-Exe (~80 MB)
# → bin/Release/net8.0-windows/win-x64/publish/GonkNote.exe
```

---
## 9. Cross-Platform (Linux) — Versuch abgebrochen (2026-07-24)

**Entscheidung des Nutzers am 2026-07-24: Der Avalonia-Port wird abgebrochen und ist
vollständig aus dem Repo entfernt.** Das Projekt bleibt eine **reine Windows-/WPF-App**.

**Was entfernt wurde:** das gesamte Projekt `GonkNote.Avalonia/` (Shell, Whiteboard-Canvas,
Markdown-Editor-Prototyp, Sticker-/Text-Dialoge) samt seiner Doku.

**Was bewusst geblieben ist**, weil es der App unabhängig vom Port nützt — es ist genau der
Cleanup-Schritt, den §5 fordert („Kernlogik sauber von den Views entkoppeln"):

- **`GonkNote.Core/`** (`net8.0`, kein WPF) mit Models, `DatabaseService`, `UndoStack`,
  `ImageCache`, `PdfImporter`, den Skia-Zeichenroutinen (`WbRenderer`), dem Geodreieck-
  Renderer (`WbAidRenderer`) und der Radier-Geometrie (`WbErase`). **Jede** dieser Klassen
  wird von der WPF-App genutzt — kein toter Code.
- **De-Duplizierung:** `WhiteboardView` hatte eine eigene Kopie aller Zeichenroutinen; sie
  leitet jetzt an `WbRenderer`/`WbAidRenderer`/`WbErase` weiter. Das hat rund **440 Zeilen
  Doppelcode** entfernt (4457 → ~4010 Zeilen) und sorgt dafür, dass Anzeige, PDF-Export und
  Harnesses garantiert identisch zeichnen.
- **Verbesserter Bleistift** (`WbRenderer.DrawPencil`): echte Graphit-Körnung nach dem
  Referenzbild des Nutzers (Perlin-Rauschen als Alpha-Maske + Kontrastkurve, drei Durchgänge
  von breit/zart nach schmal/dicht). Wirkt in der WPF-App.

**Falls das Thema je wieder aufkommt:** Die Erfahrung aus dem Versuch war, dass die UI-Schicht
komplett neu gebaut werden muss (WPF-`FlowDocument` hat kein Avalonia-Äquivalent → der
Text-Editor wäre ein Neubau, praktisch nur als Markdown-Editor mit spürbarem Feature-Verlust)
und dass unter 200 % DPI hartnäckige Layout-Eigenheiten auftraten. Der Kern-Reuse
(`GonkNote.Core`) ist dank der Entkopplung aber jederzeit wieder nutzbar.


---
## 10. Zweite Sprache (DE/EN) — Aufbau

**Umgeschaltet wird unter „Ansicht → Sprache"**, zur Laufzeit und ohne Neustart. Die Wahl liegt
in den Einstellungen (`language` = `de`/`en`) und gilt beim nächsten Start.

```
Services/Localization/
├─ Loc.cs           Loc.T(key[, args]) · Loc.Apply(sprache) · Loc.Culture (Datums-/Zahlenformate)
│                   LocSource  = Bindungsquelle für XAML (meldet beim Wechsel „alles geändert")
│                   TExtension = Markup-Erweiterung {loc:T Schlüssel}
├─ LocGerman.cs     die Vorlage — hier steht der maßgebliche Text
└─ LocEnglish.cs    dieselben Schlüssel auf Englisch
```

**So fügt man Text hinzu:** Schlüssel in **beide** Tabellen eintragen (Bereichspräfix, z. B.
`Ed.Table.Sort`), dann in XAML `Header="{loc:T Ed.Table.Sort}"` bzw. im Code `Loc.T(...)`.
Fehlt der englische Eintrag, erscheint automatisch der deutsche — eine halbfertige Übersetzung
hinterlässt also nie leere Beschriftungen.

**Die Stolperfalle:** Texte, die der **Code** setzt (Seitenzähler, Wortzähler, Galerietitel,
Formatvorlagen-Galerie, Tooltips aus `SyncSizeControls`), hängen an keiner Bindung. Sie müssen
nach einem Sprachwechsel neu geschrieben werden. Dafür gibt es `Loc.LanguageChanged`; angemeldet
sind `MainViewModel`, `WhiteboardView` und `TextEditorView`.

**Bewusst nicht übersetzt:**
- der Produktname „Gonk Note";
- `TextStyles.ParaStyle.Name` und die Namen der Tabellen-Formatvorlagen sind **Kennungen**
  (intern verglichen, bleiben deutsch) — angezeigt wird `Display` bzw. `Loc.T(Key)`;
- **bestehende Dokumentnamen**: die gehören dem Nutzer. Nur neu angelegte Dokumente bekommen
  ihren Namen in der gerade aktiven Sprache;
- der Über-Dialog lädt weiterhin das deutsche README.

**Prüfskript:** `%TEMP%\gonk-perf\loc_check.py` vergleicht beide Tabellen (gleiche Schlüssel,
gleiche Platzhalter `{0}`, `{1}`). Es hat schon einen echten Fehler gefunden: dem deutschen
`Page.Label` fehlten die Platzhalter.
