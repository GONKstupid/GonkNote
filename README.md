# Gonk Note

Moderne, offline-fähige Notiz-App für Windows 11 — eine Alternative zu GoodNotes mit
Notizbüchern, Whiteboards und Textdokumenten. Stylus-freundlich (Wacom, Microsoft Pen, …),
ohne Cloud, ohne Installation, ohne Adminrechte.

## Features (Phase 1)

- **Ordnerbaum** mit beliebiger Verschachtelung, Drag & Drop (Verschieben, mit `Strg` Kopieren),
  Umbenennen (`F2`), Löschen (`Entf`), Kontextmenü
- **Drei Dokumenttypen**, jeder in eigener Registerkarte:
  - **Notizbuch** — A4-Seiten mit Linien-Hintergrund, Seitennavigation, Seiten hinzufügen/löschen
  - **Whiteboard** — unendliche Zeichenfläche mit Punktraster
  - **Textdokument** — Rich-Text-Editor (fett/kursiv/unterstrichen, Listen, Schriftgröße, Textfarben)
- **Whiteboard-Werkzeuge** (SkiaSharp-Rendering):
  - Stift (druckempfindlich), Glättstift (glättet Linien automatisch), Bleistift, Textmarker
  - Radiergummi (Element-Radierer, Stift-Rückseite radiert automatisch)
  - Lasso-Auswahl: verschieben, löschen (`Entf`), duplizieren (`Strg+D`)
  - Formen: Linie, Pfeil, Rechteck, Ellipse, Dreieck (`Umschalt` = proportional),
    optional mit Füllfarbe und Deckkraft 0–100 %
  - Freie Farbwahl über Farbrad + Hex-Eingabe, zusätzlich feste Farbpalette
  - Textfelder direkt auf der Zeichenfläche
  - Undo/Redo (`Strg+Z` / `Strg+Y`), Zoom (`Strg+Mausrad`), Pan (mittlere Maustaste,
    Leertaste halten, Hand-Werkzeug oder **Finger auf Touchscreen**)
- **Seiten & Hintergründe**: Muster (Blanko/Liniert/Kariert/Punktiert) und Farbton
  (Hell/Dunkel/wie App) pro Seite; Notizbücher mit A4/A3, Hoch-/Querformat,
  Vorlage für neue Seiten und **Cover-Seite** mit Dokumenttitel
- **Texteditor** (OnlyOffice-orientierte Toolbar): Schriftart/-größe, Absatzformate
  (Überschriften), Fett/Kursiv/Unterstrichen/Durchgestrichen, Hoch-/Tiefstellung,
  Text- und Markerfarbe, Ausrichtung inkl. Blocksatz, Listen, Einzüge, Zeilenabstand,
  Bilder, Tabellen, Trennlinien, Suchen & Ersetzen (`Strg+F`)
- **Dark-/Light-Mode** (`Strg+T`), Farbpalette Blau/Türkis mit Pink/Lila-Akzenten;
  Seitenleiste einklappbar (`Strg+B`), Symbolfarben im Baum frei wählbar
- **Persistenz**: LiteDB-Datei unter `%APPDATA%\GonkNote\gonknote.db`, Autosave alle 30 s,
  Speichern beim Schließen von Tabs und der App

### Tastenkürzel im Whiteboard

| Taste | Werkzeug |
|---|---|
| `S` | Stift |
| `G` | Glättstift |
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
komprimiert (`EnableCompressionInSingleFile`).

## Architektur

| Baustein | Technologie |
|---|---|
| UI | WPF (.NET 8), MVVM, dynamische Theme-ResourceDictionaries |
| Whiteboard-Rendering | SkiaSharp (`SKElement`), WPF-Stylus-Events mit Druckstärke |
| Persistenz | LiteDB (eine lokale Datei, polymorphe Element-Serialisierung) |

```
GonkNote/
├─ App.xaml(.cs)           Einstieg, Theme-Initialisierung
├─ MainWindow.xaml(.cs)    Menü, Ordnerbaum (Drag & Drop), Tab-Verwaltung
├─ Models/                 NoteItem (Baum), Whiteboard-Elemente, Enums
├─ ViewModels/             MainViewModel, Tab-VMs, Baum-VM, MVVM-Basis
├─ Views/                  WhiteboardView (Skia-Canvas), TextEditorView
├─ Services/               DatabaseService (LiteDB), ThemeService, UndoStack
└─ Themes/                 Light.xaml, Dark.xaml, Styles.xaml
```

## Roadmap

- **Phase 2 — Import/Export**: PDF/DOCX/Bilder importieren, Export nach PDF (Standard),
  DOCX und Markdown; Datei-Einfüge-Tool mit Mini-Vorschau
- **Phase 3 — Feinschliff**: Sticker, Notizzettel, Lineal/Geodreieck, OCR (optional),
  RAM-Profiling (< 200 MB), Render-Caching, App-Icon, Obfuskierung
