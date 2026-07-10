# Gonk Note

Moderne, offline-fähige Notiz-App für Windows 11 — eine Alternative zu GoodNotes mit
Notizbüchern, Whiteboards und Textdokumenten. Stylus-freundlich (Wacom, Microsoft Pen, …),
ohne Cloud, ohne Installation, ohne Adminrechte.

## Features

- **Ordnerbaum** mit beliebiger Verschachtelung, Drag & Drop (Verschieben, mit `Strg` Kopieren),
  Umbenennen (`F2`), Löschen (`Entf`), Kontextmenü, frei wählbare Symbolfarben
- **Anpinnen & Favoriten**: angepinnte Ordner erscheinen im Schnellzugriff-Bereich der
  Seitenleiste; Favoriten werden in ihrem Ordner zuerst angezeigt
- **Drei Dokumenttypen**, jeder in eigener Registerkarte:
  - **Notizbuch** — A4/A3-Seiten mit anpassbarem Cover (Farbverlauf, Schrift oder eigenes Bild)
  - **Whiteboard** — unendliche Zeichenfläche mit Punktraster
  - **Textdokument** — Rich-Text-Editor mit stets heller Schreibfläche
- **Whiteboard-Werkzeuge** (SkiaSharp-Rendering, Standardfarbe Schwarz):
  - Stift (druckempfindlich), Bleistift, Textmarker
  - **Formen-Stift** (`G`): erkennt gezeichnete Formen wie in GoodNotes — Geraden
    (mit 45°-Einrasten), Kreise/Ellipsen, Rechtecke, Streckenzüge; sonst wird die Kurve geglättet
  - **Radiergummi** radiert punktgenau: Striche werden an der Berührstelle aufgetrennt,
    Stift-Rückseite radiert automatisch
  - Lasso-Auswahl: verschieben, löschen (`Entf`), duplizieren (`Strg+D`), Bilder skalieren
  - Formen (Linie, Pfeil, Rechteck, Ellipse, Dreieck) mit Füllfarbe und Deckkraft —
    Einstellungen in der Seitenleiste rechts
  - **Textfelder** mit wählbarer Schriftart, Text- und Hintergrundfarbe
    (automatischer Kontrastschutz)
  - **Bilder einfügen**: Toolbar-Button, `Strg+V` oder Drag & Drop
    (PNG, JPEG, BMP, GIF, WebP, SVG); mit Eckgriff proportional skalierbar
  - Undo/Redo (`Strg+Z` / `Strg+Y`), Zoom (`Strg+Mausrad`), Pan (mittlere Maustaste,
    Leertaste, Hand-Werkzeug)
- **Touch-Gesten**: 1 Finger verschiebt die Ansicht, 2 Finger zoomen (Pinch) und
  verschieben, Drei-Finger-Doppeltipp = Rückgängig
- **Einstellungs-Seitenleiste rechts** (Zahnrad): Seitenmuster und -farbton, Format
  (A4/A3, Hoch-/Querformat, Vorlage für neue Seiten), Formen-Optionen, Text-Optionen
  und Cover-Gestaltung — Änderungen wirken sofort
- **Texteditor**: Schriftart/-größe, Absatzformate, Fett/Kursiv/Unterstrichen/Durchgestrichen,
  Hoch-/Tiefstellung, Text- und Markerfarbe, Blocksatz, Listen, Einzüge, Zeilenabstand,
  Bilder, Tabellen, Trennlinien, Suchen & Ersetzen (`Strg+F`)
- **Dark-/Light-Mode** (`Strg+T`) fürs App-Design — Seiten und Schreibflächen bleiben
  standardmäßig hell; Seitenleiste einklappbar (`Strg+B`)
- **Persistenz**: LiteDB-Datei unter `%APPDATA%\GonkNote\gonknote.db`, Autosave alle 30 s,
  Speichern beim Schließen von Tabs und der App

### Tastenkürzel im Whiteboard

| Taste | Werkzeug |
|---|---|
| `S` | Stift |
| `G` | Formen-Stift |
| `B` | Bleistift |
| `M` | Textmarker |
| `E` | Radiergummi |
| `L` | Lasso |
| `T` | Textfeld |
| `F` | Formen |
| `H` | Verschieben |

## Build

Voraussetzung: .NET SDK 8 oder neuer.

```bash
# Entwicklung
dotnet run

# Single-File-Exe (selbständig, keine .NET-Installation nötig)
dotnet publish -c Release
# Ergebnis: bin/Release/net8.0-windows/win-x64/publish/GonkNote.exe
```

Hinweis: WPF unterstützt kein Assembly-Trimming (`PublishTrimmed`); die Exe wird stattdessen
komprimiert (`EnableCompressionInSingleFile`). Für Tests kann mit `GonkNote.exe --db <pfad>`
eine alternative Datenbank verwendet werden.

## Architektur

| Baustein | Technologie |
|---|---|
| UI | WPF (.NET 8), MVVM, dynamische Theme-ResourceDictionaries |
| Whiteboard-Rendering | SkiaSharp (`SKElement`), WPF-Stylus-Events mit Druckstärke |
| Persistenz | LiteDB (eine lokale Datei, polymorphe Element-Serialisierung) |

```
GonkNote/
├─ App.xaml(.cs)           Einstieg, Theme-Initialisierung, --db-Argument
├─ MainWindow.xaml(.cs)    Menü, Ordnerbaum (Drag & Drop, Anpinnen), Tab-Verwaltung
├─ Models/                 NoteItem (Baum), Whiteboard-Elemente, Enums
├─ ViewModels/             MainViewModel, Tab-VMs, Baum-VM, MVVM-Basis
├─ Views/                  WhiteboardView (Skia-Canvas), TextEditorView, Dialoge
├─ Services/               DatabaseService (LiteDB), ThemeService, UndoStack, ImageCache
└─ Themes/                 Light.xaml, Dark.xaml, Styles.xaml
```

## Roadmap

- **Phase 2 — Import/Export**: Bilder-Import ✔; als Nächstes DOCX-Import, PDF-Import,
  Export nach PDF/DOCX/Markdown, Datei-Einfüge-Tool mit Mini-Vorschau
- **Phase 3 — Feinschliff**: Sticker, Notizzettel, Lineal/Geodreieck, OCR (optional),
  RAM-Profiling (< 200 MB), Render-Caching, Obfuskierung
