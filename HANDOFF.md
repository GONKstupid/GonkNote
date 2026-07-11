# Gonk Note — Projektübergabe (Stand: 2026-07-11, Phase 2 abgeschlossen)

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
- Import: **Bilder ✔ / DOCX ✔ / PDF ✔** · Export: **PDF (alle) ✔ / DOCX ✔ / Markdown ✔**
- Dark/Light-Mode fürs App-Design; **Seiten/Schreibflächen standardmäßig hell**

Arbeitsweise: **in Phasen**, polished/alltagstauglich, Nutzer-Feedback wird
**laufend** eingearbeitet (kommt in großen Batches, siehe Runden 1–4). Sprache
durchgehend Deutsch (UI, Kommentare, Commits). Ehrliches Scoping statt Übercommit.

---

## 2. Repo-Status

- Pfad: `C:\Dev\Zed\gonk-note`, Branch `main`, sauber committet
- Build: `dotnet build` fehlerfrei (0 Warnungen)
- Commit-Historie (neueste zuerst):
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
│                             Elemente (Stroke/Shape/Text/Image), Enums,
│                             CoverStyle, WhiteboardDoc, PageTemplate, TextDoc
├─ ViewModels/                Mvvm-Basis, MainViewModel (Baum, Tabs, Autosave 30s,
│                             Pin/Favorit, DOCX-Import), TreeItemViewModel, Tab-VMs
├─ Views/
│  ├─ WhiteboardView.xaml(.cs)   SkiaSharp-Canvas: Werkzeuge (Stifte-Gruppe klappbar),
│  │                             Formen-Stift-Erkennung, punktgenauer Radierer,
│  │                             Bild-/PDF-Import (ein Button, Paste, DnD, Resize),
│  │                             Touch-Gesten, Einstellungs-Seitenleiste rechts
│  │                             (Seite/Formen/Text/Cover), Undo/Redo, Zoom/Pan,
│  │                             Seiten, Cover, Viewport-Culling, Busy-Overlay
│  ├─ TextEditorView.xaml(.cs)   RichTextBox (XamlPackage), Schreibfläche immer hell
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
│  ├─ ThemeService.cs, UndoStack.cs (PartialErase/ResizeImage-Actions)
├─ Themes/                    Light/Dark.xaml, Styles.xaml (inkl. Vektor-Icons)
└─ TestAssets/               testdokument.pdf (20 Seiten, gitignored) für UI-Tests
```

Pakete: LiteDB, SkiaSharp.Views.WPF, Svg.Skia (SVG-Rasterung),
DocumentFormat.OpenXml (DOCX), **Docnet.Core** (PDFium-Rendering).

**Wichtige Eigenheiten:**
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

---

## 4. Feedback-Stand

**Runden 1–4 vollständig umgesetzt und committet.** Noch keine Nutzer-Rückmeldung zu:
Formen-Stift-Erkennung in der Praxis, Touch-Gesten auf echtem Gerät, Text-Tool-
Optionen, Cover-Gestaltung, DOCX-/PDF-Import mit *echten* großen Dokumenten.
**Feedback dazu zuerst einarbeiten, wenn es kommt** (etablierte Arbeitsweise).

Zuletzt (Runde 4) erledigt: PDF-Import-Lag behoben (Culling), Import asynchron mit
Fortschritt, Whiteboard 2-spaltig, ein statt zwei Import-Buttons. Danach **Export**
(Phase 2.4) fertig – noch kein Praxis-Feedback dazu.

---

## 5. Bekannte Lücken / bewusst vertagt

- **Import-Dauer**: PDF-Rendering ist CPU-gebunden (~0,5–0,7 s pro Seite bei
  2246 px). Jetzt wenigstens non-blocking mit Fortschritt. **Mögliche künftige
  Optimierung**: Seiten *lazy* on-demand rendern (nur PDF-Bytes + Seitenindex
  speichern, Bild erst beim Anzeigen erzeugen/cachen) → Import quasi sofort.
  Größerer Umbau (Persistenz, Undo, Save/Load), bewusst vertagt.
- **Datei-Einfüge-Tool** (PDF/DOCX-Mini-Vorschau ins Whiteboard) → noch offen.
- Sticker, Notizzettel, Lineal/Geodreieck, OCR, **RAM-Optimierung**, Obfuskierung
  → Phase 3. RAM: Basis ~190 MB + bis 96 MB Bild-Cache; Ziel < 200 MB braucht noch
  Arbeit (Cache-Budget senken, Render-Caching, GC-Tuning).
- Text-Stiländerungen am bestehenden Whiteboard-Textfeld sind nicht undo-fähig
  (bewusst einfach). DOCX-Import: keine Kopf-/Fußzeilen, Fußnoten, verschachtelten
  Tabellen. PDF: keine Text-Extraktion (per Nutzer-Wunsch reine Bild-Seiten).
- **Export-Grenzen**: Text→PDF ist gerastert (kein selektierbarer Text im PDF –
  bewusst, erhält aber die Formatierung 1:1). DOCX-Bilder landen unter `media/`
  (nicht `word/media/`) – gültig. Markdown ist best-effort (Farben/Marker gehen
  verloren). Whiteboard→DOCX/Markdown gibt es nicht (nur PDF).

---

## 6. Empfohlener Ablaufplan für den neuen Thread

1. Falls Nutzer Feedback zu Import/Export in der Praxis mitbringt → **zuerst** einarbeiten.
2. **Datei-Einfüge-Tool**: PDF/DOCX-Vorschau, wahlweise als Bild oder strukturierte
   Elemente ins Whiteboard einfügen (baut auf Import auf).
3. **Phase 3** nach Rücksprache: RAM-Profiling/-Optimierung (Ziel < 200 MB),
   Sticker/Notizzettel/Lineal, optionales OCR, Obfuskierung, laufendes UI-Feintuning.

**Damit ist Phase 2 (Import + Export) abgeschlossen.** Export sitzt in
`Datei → Exportieren`; `ExportActiveTab` im MainViewModel wählt anhand des aktiven
Tabs die Formate. Zum Testen: `verify-export.ps1` / `verify-wbexport2.ps1` in
`%TEMP%\gonk-verify\`, PDFs rendern mit dem Docnet-Tool in `%TEMP%\gonk-render\`.

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
  Rechner — kurz halten, vor jeder Aktion `SetForegroundWindow`, Fokusverlust
  einplanen. Ein DB-Dump-Tool (net8.0-Console mit LiteDB/OpenXml/SkiaSharp) lag in
  `%TEMP%\gonk-dbclean\` zum Erzeugen von Test-DOCX/PDF und Inspizieren der Test-DB.

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
