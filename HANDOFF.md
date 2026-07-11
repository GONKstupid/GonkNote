# Gonk Note — Projektübergabe (Stand: 2026-07-10, abends)

Diese Datei ist für den Einstieg in einen **neuen Chat-Thread** gedacht. Sie fasst
zusammen, was existiert, was als Nächstes ansteht, und wie gearbeitet werden soll.
Wenn du diesen Thread eröffnest, sag einfach: *"Lies HANDOFF.md und mach weiter."*

---

## 1. Was ist Gonk Note

Offline-Notiz-App für Windows 11 als Alternative zu GoodNotes/Apple Notes.
Kernanforderungen des Nutzers (unverändert gültig):

- **Plattform**: Windows 11, komplett offline, keine PWA
- **Single-File-Exe**, kein Installer, keine Adminrechte
- **RAM-Ziel**: < 200 MB im Normalbetrieb
- **Stylus-first**: Wacom/Microsoft Pen mit Druckstärke, Finger = Gesten
- **Architektur-Entscheidung des Nutzers**: **WPF** (nicht WinUI 3), .NET 8
- **Drei Dokumenttypen**: Notizbuch, Whiteboard, Textdokument — je eigener Tab
- Import (DOCX ✔ / PDF ✗ / Bilder ✔) und Export (PDF/DOCX/Markdown — noch offen)
- Dark/Light-Mode fürs App-Design; **Seiten/Schreibflächen standardmäßig hell**

Arbeitsweise: **in Phasen**, polished/alltagstauglich, Nutzer-Feedback wird
**laufend** eingearbeitet (kommt in großen Batches, siehe Runden 1–3).

---

## 2. Repo-Status

- Pfad: `C:\Dev\Zed\gonk-note`, Branch `main`, sauber committet
- Build: `dotnet build` fehlerfrei (0 Warnungen)
- Wichtige Commits:
  - `638bb73` Phase 1 Grundgerüst · `6a921d9` App-Icon · `64b8129` Feedback-Runde 1
  - `6e0da2e` **Phase 2.1**: Bilder-Import + Feedback-Runde 2 (Formen-Stift,
    punktgenauer Radierer, Einstellungs-Seitenleiste, anpassbares Cover)
  - `34eed2e` **Feedback-Runde 3**: Anpinnen/Favoriten, Touch-Gesten, Text-Optionen,
    Schwarz/Hell-Defaults, Über-Dialog mit README
  - `5b745f2` **Phase 2.2**: DOCX-Import

**Vor jedem Build**: laufende Testinstanz beenden (`taskkill //IM GonkNote.exe //F`),
sonst Datei-Lock-Fehler.

**UI-Tests**: `GonkNote.exe --db <pfad>` nutzt eine alternative DB → niemals in der
echten Nutzer-DB (`%APPDATA%\GonkNote\gonknote.db` — enthält echte Schuldaten!) testen.
Bewährtes Muster: PowerShell-Skripte in `%TEMP%\gonk-verify\` (UIA für Menüs/TreeItems,
SetProcessDPIAware + physische Koordinaten für Canvas-Drags, Screenshots je Schritt).
**Achtung**: Solche Tests übernehmen Maus/Tastatur — der Nutzer arbeitet oft parallel
am Rechner; kurz halten, vor jeder Aktion SetForegroundWindow, Fokusverlust einplanen.

---

## 3. Architektur-Überblick

```
GonkNote/
├─ App.xaml(.cs)              Einstieg, Theme-Init, --db-Argument
├─ MainWindow.xaml(.cs)       Menü (inkl. DOCX-Import), Seitenleiste mit
│                             Schnellzugriff (angepinnte Ordner), Ordnerbaum
│                             (Drag&Drop, Favoriten-Stern), Tab-Host
├─ Models/
│  ├─ NoteItem.cs             Baum-Eintrag inkl. IconColor, IsPinned, IsFavorite
│  └─ Whiteboard.cs           WbPage, Elemente (Stroke/Shape/Text/Image), Enums,
│                             CoverStyle, WhiteboardDoc, PageTemplate, TextDoc
├─ ViewModels/                Mvvm-Basis, MainViewModel (Baum, Tabs, Autosave 30s,
│                             Pin/Favorit, DOCX-Import), TreeItemViewModel, Tab-VMs
├─ Views/
│  ├─ WhiteboardView.xaml(.cs)   SkiaSharp-Canvas: Werkzeuge (Stifte-Gruppe klappbar),
│  │                             Formen-Stift-Erkennung, punktgenauer Radierer,
│  │                             Bilder (Import/Paste/DnD/Resize), Touch-Gesten,
│  │                             Einstellungs-Seitenleiste rechts (Seite/Formen/
│  │                             Text/Cover), Undo/Redo, Zoom/Pan, Seiten, Cover
│  ├─ TextEditorView.xaml(.cs)   RichTextBox (XamlPackage), Schreibfläche immer hell
│  ├─ ColorPickerDialog.xaml(.cs) HSV-Farbrad + Hex + Alpha
│  ├─ AboutDialog.xaml(.cs)      Version + eingebettetes README
│  ├─ TableSizeDialog.xaml(.cs)
│  └─ Converters.cs
├─ Services/
│  ├─ DatabaseService.cs      LiteDB (items/boards/texts/settings)
│  ├─ DocxImporter.cs         DOCX → FlowDocument → XamlPackage
│  ├─ ImageCache.cs           LRU-Cache dekodierter Bilder (max. 24)
│  ├─ ThemeService.cs, UndoStack.cs (inkl. PartialEraseAction, ResizeImageAction)
└─ Themes/                    Light/Dark.xaml, Styles.xaml (inkl. Vektor-Icons)
```

Pakete: LiteDB, SkiaSharp.Views.WPF, Svg.Skia (SVG-Rasterung beim Import),
DocumentFormat.OpenXml (DOCX-Import).

**Wichtige Eigenheiten:**
- Whiteboard-Bilder liegen als PNG/JPEG-Bytes im Element (Downscale auf 2048 px),
  Rendering über `ImageCache` (RAM-Ziel!)
- Formen-Stift (`G`): Douglas-Peucker-Eckenerkennung, Sehnenabweichung für Geraden
  (45°-Einrasten), Ellipsen-Fit; Fallback = geglättete Kurve
- Radierer trennt Strokes an der Berührstelle auf (`SplitStroke` + `PartialEraseAction`)
- Touch: rohe Touch-Events (1 Finger Pan, 2 Finger Pinch+Pan, 3-Finger-Doppeltipp
  Undo); Stylus-Events ignorieren Touch-Geräte
- Farb-Tags: `_colorTag` "auto" = Schwarz (auf dunklen Seiten hell); Checked-Handler
  ist gegen fehlende Tags abgesichert (es gab einen Null-Farben-Bug)

---

## 4. Feedback-Stand

**Runde 1–3 sind vollständig umgesetzt und committet.** Noch keine Nutzer-Rückmeldung
zu: Formen-Stift-Erkennung in der Praxis, Touch-Gesten auf echtem Gerät (konnte nur
per Maus/Code verifiziert werden, kein Touchscreen im Test), Text-Tool-Optionen,
Cover-Gestaltung, DOCX-Import mit echten Dokumenten. **Feedback dazu zuerst
einarbeiten, wenn es kommt.**

---

## 5. Bekannte Lücken / bewusst vertagt

- **Export**: Markdown → DOCX → PDF (Whiteboard via `SKDocument.CreatePdf`)
- **Datei-Einfüge-Tool** (PDF/DOCX-Vorschau ins Whiteboard) → nach Import/Export
- Sticker, Notizzettel, Lineal, OCR, RAM-Optimierung, Obfuskierung → Phase 3
- Text-Stiländerungen (Schriftart/Farbe am bestehenden Whiteboard-Textfeld) sind
  nicht undo-fähig (bewusst einfach gehalten)
- DOCX-Import: keine Kopf-/Fußzeilen, Fußnoten, verschachtelte Tabellen

---

## 6. Empfohlener Ablaufplan

1. Nutzer-Feedback zu Runde 2/3 + DOCX-/PDF-Import einarbeiten (falls vorhanden)
2. **Export**: Markdown → DOCX (OpenXML, Gegenrichtung zum Importer) → PDF
3. Datei-Einfüge-Tool, dann Phase 3 (nach Rücksprache)

**PDF-Import ist umgesetzt** (Nutzer-Entscheidung: Bild-Seiten, keine Text-Extraktion):
Docnet.Core/PDFium rendert Seiten als JPEG (lange Kante 2246 px). Im Notizbuch wird
jede PDF-Seite eine neue `WbPage` mit `BackgroundImage` (nicht verschieb-/radierbar,
ersetzt das Muster, Seitenverhältnis erhalten, lange Kante = A4-Höhe). Im Whiteboard
werden die Seiten als `ImageElement` untereinander eingefügt und direkt ausgewählt.
`ImageCache` arbeitet mit Byte-Budget (96 MB) statt Stückzahl-Limit.

---

## 7. Schnellstart-Befehle

```bash
cd "C:\Dev\Zed\gonk-note"
taskkill //IM GonkNote.exe //F   # vor jedem Build
dotnet build
./bin/Debug/net8.0-windows/GonkNote.exe            # Debug-Start (echte DB!)
./bin/Debug/net8.0-windows/GonkNote.exe --db X.db  # Test-DB
dotnet publish -c Release                           # Single-File-Exe
```
