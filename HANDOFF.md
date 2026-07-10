# Gonk Note — Projektübergabe (Stand: 2026-07-10)

Diese Datei ist für den Einstieg in einen **neuen Chat-Thread** gedacht. Sie fasst
zusammen, was existiert, was als Nächstes ansteht, und wie gearbeitet werden soll.
Wenn du diesen Thread eröffnest, sag einfach: *"Lies HANDOFF.md und mach weiter."*

---

## 1. Was ist Gonk Note

Offline-Notiz-App für Windows 11 als Alternative zu GoodNotes/Apple Notes.
Kernanforderungen des Nutzers (unverändert gültig, siehe ursprünglicher Auftrag):

- **Plattform**: Windows 11, komplett offline, keine PWA
- **Single-File-Exe**, kein Installer, keine Adminrechte
- **RAM-Ziel**: < 200 MB im Normalbetrieb
- **Stylus-first**: Wacom/Microsoft Pen mit Druckstärke, Finger = Verschieben
- **Architektur-Entscheidung des Nutzers**: **WPF** (nicht WinUI 3), .NET 8
- **Drei Dokumenttypen**: Notizbuch, Whiteboard, Textdokument — je eigener Tab
- Datenverwaltung per Ordnerbaum, Drag & Drop, Umbenennen, Löschen
- Import (DOCX/PDF/Bilder) und Export (PDF/DOCX/Markdown) — **noch nicht gebaut**
- Dark/Light-Mode, Farbpalette Blau/Türkis mit Pink/Lila-Akzenten

Arbeitsweise laut Nutzer: **in Phasen**, möglichst polished/alltagstauglich,
Feedback zu Tools und UI wird **laufend** eingebaut, nicht erst am Phasenende
gesammelt.

---

## 2. Repo-Status

- Pfad: `C:\Dev\Zed\gonk-note`
- Git: `main`-Branch, 3 Commits, sauber committet, kein offenes Diff
- Build: `dotnet build` läuft fehlerfrei (0 Warnungen, 0 Fehler)
- Release-Test war erfolgreich: `dotnet publish -c Release` erzeugt eine
  einzelne `GonkNote.exe` (~67 MB, self-contained win-x64), gemessene
  Private Memory ~192 MB im Leerlauf — **knapp am 200-MB-Ziel, noch nicht
  optimiert** (Render-Caching/GC-Tuning ist bewusst auf Phase 3 verschoben)

Commits bisher:
1. `638bb73` — Phase 1: Grundgerüst (Whiteboard, Ordnerbaum, Tabs, Themes)
2. `6a921d9` — App-Icon eingebunden (Exe, Fenster, Seitenleiste, Willkommensbildschirm)
3. `64b8129` — Feedback-Runde 1 (siehe Abschnitt 4)

**Wichtig für den neuen Thread**: Vor dem nächsten Build immer prüfen, ob noch
eine `GonkNote.exe`-Testinstanz läuft (`taskkill //IM GonkNote.exe //F`), sonst
schlägt der Build mit einem Datei-Lock-Fehler fehl (kein Code-Problem).

---

## 3. Architektur-Überblick

```
GonkNote/
├─ App.xaml(.cs)              Einstieg, Theme-Init, DatabaseService-Lifecycle
├─ MainWindow.xaml(.cs)       Menü, einklappbare Seitenleiste, Ordnerbaum
│                             (Drag&Drop, Symbolfarben), Tab-Host
├─ Models/
│  ├─ NoteItem.cs             Baum-Eintrag (Ordner/Notizbuch/Whiteboard/Text),
│  │                          inkl. IconColor
│  └─ Whiteboard.cs           WbPage, WbElement (Stroke/Shape/Text), Enums
│                             (ToolType, ShapeKind, PageBackground, PageShade),
│                             WhiteboardDoc (inkl. Cover, PageTemplate), TextDoc
├─ ViewModels/
│  ├─ Mvvm.cs                 ObservableObject, RelayCommand (Basis)
│  ├─ MainViewModel.cs        Baum-Operationen, Tab-Verwaltung, Autosave (30s)
│  ├─ TreeItemViewModel.cs    Baumknoten inkl. IconBrush/IconGlyph
│  └─ DocumentTabViewModel.cs WhiteboardTabViewModel, TextTabViewModel
├─ Views/
│  ├─ WhiteboardView.xaml(.cs)   SkiaSharp-Canvas: Werkzeuge, Rendering,
│  │                             Undo/Redo, Zoom/Pan, Seiten, Cover
│  ├─ TextEditorView.xaml(.cs)   RichTextBox-Editor, OnlyOffice-Toolbar
│  ├─ ColorPickerDialog.xaml(.cs) HSV-Farbrad + Hex + Alpha (wiederverwendbar)
│  ├─ PageSetupDialog.xaml(.cs)   Muster/Farbton/Format-Dialog
│  ├─ TableSizeDialog.xaml(.cs)   Zeilen/Spalten-Auswahl für Tabelleneinfügung
│  └─ Converters.cs
├─ Services/
│  ├─ DatabaseService.cs      LiteDB-Wrapper (%APPDATA%\GonkNote\gonknote.db)
│  ├─ ThemeService.cs         Dark/Light-Umschaltung (ResourceDictionary-Swap)
│  └─ UndoStack.cs            IEditAction-Pattern (Add/Remove/Move/TextChange)
└─ Themes/
   ├─ Light.xaml / Dark.xaml  Farbressourcen (Brush.*, Color.*)
   └─ Styles.xaml             Alle Control-Templates + Vektor-Icons (Icon.*)
```

**Persistenz**: LiteDB, drei Collections (`items`, `boards`, `texts`) plus
`settings` (Key-Value, u.a. Theme, Sidebar-Zustand). Autosave alle 30s, Save-on-Close.

**Whiteboard-Rendering**: SkiaSharp `SKElement`, eigenes Koordinatensystem
(Canvas-Space ↔ Screen-Space via Zoom/Pan), Elemente sind POCOs
(`StrokeElement`, `ShapeElement`, `TextElement`), serialisiert direkt über LiteDB.

**Texteditor**: `RichTextBox` mit `XamlPackage`-Serialisierung (ZIP, erhält
Bilder/Tabellen). Ältere `TextDoc.Rtf`-Bytes werden weiter erkannt (RTF- vs.
PK-Header-Check) und geladen.

---

## 4. Was in Feedback-Runde 1 bereits umgesetzt wurde

Der Nutzer hatte nach Phase 1 eine Wunschliste gegeben — **alles ist erledigt
und committet** (Commit `64b8129`):

**UI:**
- Seitenleiste einklappbar (`Strg+B`, Hamburger-Button, Zustand persistiert)
- Symbolfarben im Baum wählbar (Kontextmenü-Palette + eigener Farbwähler)
- Neues Vektor-Icon für Whiteboard (Tafel mit Beinen) und Lasso (echte Schlinge)

**Werkzeuge:**
- Glättstift (Taste `G`): wie Stift, glättet Striche automatisch beim Absetzen
  (Resampling auf 3px-Abstände + 3-facher gleitender Mittelwertfilter)
- Freier Farbwähler (HSV-Rad, Hue-Slider, Alpha-Slider, Hex-Eingabe) zusätzlich
  zur festen Palette, wiederverwendet für Tinte/Füllung/Symbolfarbe/Textfarbe
- Formen mit optionaler Füllfarbe + Deckkraft-Slider (0–100 %)

**Features:**
- "Seite einrichten"-Dialog: Muster (Blanko/Liniert/Kariert/Punktiert),
  Farbton (Hell/Dunkel/Auto=Theme), bei Notizbüchern zusätzlich A4/A3 +
  Hoch-/Querformat + "als Standard für neue Seiten"
- Notizbücher starten mit Cover-Seite (Verlauf + Dokumenttitel), Seitenzähler
  ignoriert das Cover korrekt
- Texteditor massiv ausgebaut: Schriftart/-größe, Überschriften-Styles,
  Durchgestrichen, Hoch-/Tiefstellung, Text-/Markerfarbe, Blocksatz, Einzüge,
  Zeilenabstand, Bild/Tabelle/Trennlinie einfügen, Suchen & Ersetzen (`Strg+F`)

**Noch offenes Feedback vom Nutzer zu dieser Runde:** Es wurde noch keine
Rückmeldung zum *Test* dieser Änderungen gegeben (Glättstärke des Glättstifts,
Schreibgefühl, Texteditor-Bedienung). **Das im neuen Thread zuerst erfragen
bzw. entgegennehmen, falls der Nutzer es mitbringt.**

---

## 5. Bekannte Lücken / bewusst vertagt

- **Import**: DOCX/PDF/Bilder → noch nicht implementiert (Phase 2)
- **Export**: PDF/DOCX/Markdown → noch nicht implementiert (Phase 2)
- **Datei-Einfüge-Tool** (Mini-Vorschau für PDF/DOCX im Whiteboard) → Phase 2
- **Sticker, Notizzettel, Lineal/Geodreieck, OCR (optional)** → Phase 3
- **RAM-Optimierung** (Render-Caching, GC-Tuning, ggf. Trimming-Alternativen
  für WPF) → Phase 3, aktuell ~192 MB im Release-Build, Ziel < 200 MB steht
  aber noch ohne Sicherheitsabstand
- **Obfuskierung** (aus Originalanforderung) → nicht begonnen
- Textfarben im Editor werden beim Theme-Wechsel nicht automatisch invertiert
  (bekannte Einschränkung, kein Bug-Report dazu bisher)

---

## 6. Empfohlener Ablaufplan für den neuen Thread

### Schritt 0 — Kontext laden
- Diese Datei lesen, dann `git log --oneline` und `git status` prüfen, um zu
  bestätigen, dass der Stand mit dieser Doku übereinstimmt
- Falls der Nutzer Feedback zur Feedback-Runde 1 mitbringt (Glättstift-Gefühl,
  Texteditor-Test, UI-Kleinigkeiten): **zuerst einarbeiten**, bevor Phase 2
  beginnt — das ist die etablierte Arbeitsweise (laufend einbauen, nicht sammeln)

### Schritt 1 — Phase 2: Import
Reihenfolge nach Aufwand/Nutzen, jede Etappe einzeln bauen+testen+committen:
1. **Bilder importieren** (PNG/JPEG/SVG) ins Whiteboard und ins Textdokument
   — einfachster Fall, kein externes Paket nötig für Raster-Bilder (SVG braucht
   ggf. `Svg.Skia`, das mit SkiaSharp harmoniert)
2. **DOCX-Import** → Textdokument (OpenXML SDK, `DocumentFormat.OpenXml`
   NuGet-Paket), Text + Bilder rüberziehen, Formatierung bestmöglich erhalten
3. **PDF-Import** → als Bild-Seiten ins Whiteboard (Rendering z. B. via
   `PdfiumViewer` oder `Docnet.Core`; iText7 selbst rendert nicht zu Bitmaps,
   dafür bräuchte es zusätzlich etwas wie PDFium) — mit dem Nutzer klären, ob
   Text-Extraktion (editierbar) oder Bild-Import (Faksimile) Priorität hat,
   da das sehr unterschiedliche Bibliotheken/Aufwand bedeutet

### Schritt 2 — Phase 2: Export
1. **Markdown-Export** aus Textdokument (einfachste Fließtext-Konvertierung
   aus `RichTextBox`-Xaml)
2. **DOCX-Export** aus Textdokument (OpenXML SDK)
3. **PDF-Export** — von Whiteboard-Seiten (SkiaSharp kann direkt auf ein
   PDF-Canvas rendern, `SKDocument.CreatePdf`) und von Textdokumenten
   (PDFsharp oder Weiterverwendung von SkiaSharp-Textlayout)

### Schritt 3 — Datei-Einfüge-Tool
Mini-Vorschau-Dialog, der PDF/DOCX anzeigt und wahlweise als Bild oder
strukturierte Elemente ins Whiteboard einfügt — baut auf Schritt 1 auf.

### Schritt 4 — Phase 3 (nach Rücksprache mit Nutzer)
RAM-Profiling und -Optimierung, Sticker/Notizzettel/Lineal, optionales OCR,
Obfuskierung, ggf. weiteres UI-Feintuning aus laufendem Feedback.

---

## 7. Arbeitsweise-Hinweise (aus bisherigem Verlauf gelernt)

- Nutzer will **WPF**, nicht WinUI 3 — trotz ursprünglicher Architekturempfehlung
  in der Anforderung, die WinUI 3 vorschlug. Diese Entscheidung ist bereits
  gefallen und umgesetzt, nicht erneut aufrollen.
- Nutzer gibt Feedback **gerne detailliert und in großen Batches** (siehe
  Feedback-Runde 1: 3 Kategorien, ~10 Einzelpunkte auf einmal) — solche Listen
  systematisch mit TaskCreate/TaskUpdate abarbeiten, dann gebündelt committen.
- Nutzer erwartet **laufende Verifikation**: nach jeder größeren Änderung
  bauen, App starten, Screenshot machen und optisch prüfen, bevor als fertig
  gemeldet wird.
- Deutsch ist die Konversations- und UI-Sprache; Code-Kommentare (wo vorhanden)
  ebenfalls auf Deutsch gehalten.
- Erwartungsmanagement wichtig: bei "70% wie Word" wurde offen kommuniziert,
  dass das ein Richtwert ist und iterativ angegangen wird, statt eine falsche
  Zusage zu machen. Diesen Stil beibehalten — lieber ehrlich scoping als
  übercommitten.

---

## 8. Schnellstart-Befehle

```bash
cd "C:\Dev\Zed\gonk-note"

# Laufende Testinstanz beenden (vor jedem Build empfohlen)
taskkill //IM GonkNote.exe //F

# Bauen
dotnet build

# Starten (Debug)
./bin/Debug/net8.0-windows/GonkNote.exe

# Release / Single-File-Exe
dotnet publish -c Release
# → bin/Release/net8.0-windows/win-x64/publish/GonkNote.exe
```
