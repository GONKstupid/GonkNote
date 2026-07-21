# Gonk Note

Moderne, offline-fähige Notiz-App für Windows 11 — eine Alternative zu GoodNotes mit
Notizbüchern, Whiteboards und Textdokumenten. Stylus-freundlich (Wacom, Microsoft Pen, …),
ohne Cloud, ohne Installation, ohne Adminrechte.

## Features

- **Ordnerbaum** mit beliebiger Verschachtelung, Drag & Drop (Verschieben, mit `Strg` Kopieren),
  Umbenennen (`F2`), Löschen (`Entf`), Kontextmenü, frei wählbare Symbolfarben — Elemente und
  Unterordner **erben automatisch die Farbe ihres Ordners**, solange sie keine eigene Farbe haben
- **Anpinnen & Favoriten**: angepinnte Ordner erscheinen im Schnellzugriff-Bereich der
  Seitenleiste; Favoriten werden in ihrem Ordner zuerst angezeigt
- **Drei Dokumenttypen**, jeder in eigener Registerkarte:
  - **Notizbuch** — A4/A3-Seiten mit anpassbarem Cover (Farbverlauf, Schrift oder eigenes Bild;
    mitgelieferte Cover-Vorlagen in den Kategorien „Basic", „Muster" und „Pixel Art" sowie
    eigene, hochladbare Vorlagen unter „Individuell")
  - **Whiteboard** — unendliche Zeichenfläche mit Punktraster
  - **Textdokument** — Rich-Text-Editor mit stets heller Schreibfläche
- **Whiteboard-Werkzeuge** (SkiaSharp-Rendering; Standardfarbe folgt der Seite: Schwarz auf
  hellen, Weiß auf dunklen Seiten):
  - Stift (druckempfindlich), Bleistift, Textmarker
  - **Formen-Stift** (`G`): erkennt gezeichnete Formen wie in GoodNotes — Geraden
    (mit 45°-Einrasten), Kreise/Ellipsen, Rechtecke, Streckenzüge; sonst wird die Kurve geglättet
  - **Radiergummi** radiert punktgenau: Striche werden an der Berührstelle aufgetrennt,
    Stift-Rückseite radiert automatisch
  - **Auswahl** mit zwei Werkzeugen: **Lasso** (`L`) umkreist Objekte (nur was ~vollständig
    umschlossen ist) und **Verschieben** (`V`) wählt Objekte direkt per Klick an. Ausgewählte
    Objekte lassen sich verschieben, **skalieren** (Eckgriff) und **drehen** (Dreh-Griff mit
    15°-Rastung) — für Striche, Formen, Text, Bilder und Notizzettel
  - **Quick-Options-Menü** auf der Leinwand (floatende Icon-Leiste im Toolbar-Look:
    Ausschneiden, Kopieren, Duplizieren, Einfügen, **Text erkennen (OCR)**, Löschen, Alles
    auswählen) — öffnet per Rechtsklick, **zweiter Stift-Taste**, **langem Drücken** (Finger/
    Stift, bei Lasso/Verschieben/Hand) oder automatisch nach einer Auswahl; ganz ohne Tastatur
  - **Textstärke per Zahlenblock**: langes Drücken auf den Größen-Regler (oder Klick auf den
    Wert) öffnet ein Numpad zur direkten Eingabe (Vorbild Adobe Fresco)
  - **OCR** (Texterkennung, offline via Tesseract, Deutsch/Englisch): erkennt gedruckten Text in
    ausgewählten Bildern oder importierten PDF-Seiten; Ergebnis kopieren oder als Notizzettel einfügen
  - Formen (Linie, Pfeil, Rechteck, Ellipse, Dreieck) mit Füllfarbe und Deckkraft —
    Einstellungen in der Seitenleiste rechts
  - **Textfelder** mit wählbarer Schriftart, Text- und Hintergrundfarbe
    (automatischer Kontrastschutz)
  - **Notizzettel** (farbige Klebezettel) und **Sticker** (Bild-Aufkleber mit eigener,
    erweiterbarer Sammlung)
  - **Zeichenhilfen**: Lineal (`R`) und Holo-Geodreieck (`D`) mit Winkelskala, drehbar/einrastend
  - **Bilder einfügen**: Toolbar-Button, `Strg+V` oder Drag & Drop
    (PNG, JPEG, BMP, GIF, WebP, SVG); mit Eckgriff proportional skalierbar
  - **PDF & Word einfügen** (Toolbar-Button oder Drag & Drop): mit Seitenauswahl-Dialog;
    im Notizbuch wird jede Seite eine eigene Seite zum Draufschreiben und Markieren (wie
    GoodNotes), im Whiteboard landen die Seiten als hochauflösende, skalierbare Bilder
  - Undo/Redo (`Strg+Z` / `Strg+Y`), Zoom (`Strg+Mausrad`), Pan (mittlere Maustaste,
    Leertaste, Hand-Werkzeug)
- **Touch-Gesten**: 1 Finger verschiebt die Ansicht, 2 Finger zoomen (Pinch) und
  verschieben, Drei-Finger-Doppeltipp = Rückgängig
- **Einstellungs-Seitenleiste rechts** (Zahnrad): Seitenmuster und -farbton, Format
  (A4/A3, Hoch-/Querformat, Vorlage für neue Seiten), Formen-Optionen, Text-Optionen
  und Cover-Gestaltung — Änderungen wirken sofort
- **Texteditor** im Ribbon-Layout (Start / Einfügen / Layout / Verweise, plus Kontext-Tab
  **Tabelle**, wenn der Cursor in einer Tabelle steht):
  - Zeichen-/Absatzformate, Formatvorlagen (Standard, Überschrift 1–4, Titel, Zitat,
    Kopf-/Fußzeile), Format übertragen, Listen mit Stil-Bibliothek, Suchen & Ersetzen
  - **Erweiterte Einstellungen** (ausklappbare Seitenleiste): Seiteneinrichtung (A4/A5/A3/Letter,
    Hoch-/Querformat, Ränder in cm inkl. Lernblatt-Vorlage), Absätze, Kopf-/Fußzeile mit
    Platzhaltern, Wasserzeichen, sowie Tabellen-Design/Rahmen — jeweils über ihren Ribbon-Button geöffnet
  - **Inhaltsverzeichnis** aus den Überschriften, Hyperlinks, Sonderzeichen, Beschriftungen
  - **Tabellen wie in Word** (Kontext-Tab „Tabelle"): Raster-Einfügen, Text↔Tabelle, Schnell­tabellen,
    Zeilen/Spalten, Zellen verbinden (auch senkrecht)/teilen, Tabelle teilen, AutoAnpassen,
    Sortieren, Formeln (`=SUMME(ABOVE)` …), Formatvorlagen mit Kopf-/Ergebniszeile und
    Zeilen-/Spaltenbändern, Rahmen und Füllung
  - **Diagramme** (Säulen, Balken, Linie, Punkt, Punkt+Linie, Kuchen, Radar — mehrere Reihen,
    Farben per „+" erweiterbar und per Rechtsklick löschbar)
  - Rechtschreibprüfung (Deutsch/Englisch) mit Korrekturvorschlägen, Lineal, Statusleiste
    (Wörter, Zoom), Überschriften-Navigator, Seitenumbruch-Marken
- **Import**: Bilder, PDF, DOCX und **Markdown (`.md`)** — DOCX/Markdown als neue Textdokumente
- **Export**: Textdokument → PDF / DOCX / Markdown / PNG, Whiteboard/Notizbuch → PDF / PNG
- **Dark-/Light-Mode** (`Strg+T`) fürs App-Design — Seiten und Schreibflächen bleiben
  standardmäßig hell; die Fenster-Titelleiste folgt dem Theme (im Dark Mode dunkel).
  Seitenleiste einklappbar (`Strg+B`)
- **Maximiertes Fenster ohne Titelleiste**: Wird das Fenster maximiert („in Groß"),
  blendet sich die Titelleiste aus und die Menüleiste rückt nach oben; Doppelklick auf
  die Menüleiste (oder die Windows-Standardbefehle) stellt das Fenster wieder her
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
| `V` | Verschieben (Objekte anklicken) |
| `L` | Lasso |
| `T` | Textfeld |
| `F` | Formen |
| `N` | Notizzettel |
| `R` | Lineal |
| `D` | Geodreieck |
| `H` | Hand (Leinwand verschieben) |

Auswahl: verschieben, skalieren, drehen · `Strg+C/X/V` kopieren/ausschneiden/einfügen ·
`Strg+D` duplizieren · `Strg+A` alles auswählen · `Entf` löschen · Rechtsklick, zweite
Stift-Taste oder langes Drücken = Quick-Options-Menü.

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

- **Phase 2 — Import/Export** ✔: Bilder-/DOCX-/PDF-/Markdown-Import, Export nach
  PDF/DOCX/Markdown/PNG, Datei-Einfüge-Tool mit Seitenvorschau
- **Phase 3 — Feinschliff**:
  - ✔ Sticker, Notizzettel, Lineal/Geodreieck, Diagramme, Tabellen (Word-artiger Kontext-Tab),
    Quick-Options-Menü, **OCR** (Tesseract, Deutsch/Englisch), Markdown-Import, Cover-Vorlagen,
    Ordner-Farbvererbung, Zahlenblock für die Strichstärke
  - **Nächster Schritt (nach der Testphase):** RAM-Optimierung. Leitlinie: *Features vor RAM* —
    Ziel wären ~200 MB, akzeptabel bis in den mittleren dreistelligen MB-Bereich, **harte
    Obergrenze 1 GB**, die nie überschritten werden darf.
  - **Render-Caching**: nur, falls die RAM-Auslastung nach der Optimierung noch zu hoch ist
    (oder vorgezogen, falls es technisch sinnvoller vor der Optimierung liegt)
  - **Handschrift-Erkennung** (optional, aufbauend auf der OCR-Grundlage)
  - **Keine Obfuskierung** — das Projekt soll so offen wie möglich bleiben
