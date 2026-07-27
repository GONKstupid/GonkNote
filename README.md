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
- **Galerie-Startansicht** (wenn kein Dokument geöffnet ist): der Inhalt des aktuellen Ordners
  in großen Kacheln (GoodNotes-artig) – farbige Ordnersymbole, Notizbuch-Cover als Vorschau,
  Karten für Whiteboard/Textdokument, jeweils mit Name, Datum und Kontextmenü. Ein Ordner im
  Baum wählen oder eine Ordnerkachel öffnen navigiert hinein (Breadcrumb + Zurück)
- **Zwei Sprachen**: Die Oberfläche lässt sich unter **Ansicht → Sprache** zwischen
  **Deutsch** und **Englisch** umschalten — zur Laufzeit, ohne Neustart. Die Wahl wird
  gemerkt. Eigene Dokumentnamen bleiben unverändert; nur die Oberfläche wechselt.
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
    Stift-Rückseite radiert automatisch. Die Größe stellt der Größen-Schieber ein
    (oder der Zahlenblock per langem Drücken) und wird getrennt von der Strichstärke gemerkt
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
  - **Notizzettel** (farbige Klebezettel) und **Sticker** (Bild-Aufkleber). Sticker bringt
    Gonk Note aus Lizenzgründen keine mit — lege eigene Bilder in
    `%APPDATA%\GonkNote\Stickers` ab (oder nutze die „+"-Kachel im Sticker-Werkzeug);
    Unterordner erscheinen als eigene Gruppen
  - **Zeichenhilfen**: Lineal (`R`) und Geodreieck (`D`) mit Winkelskala, drehbar/einrastend.
    Statt der selbst gezeichneten Kontur lässt sich eine eigene SVG hinterlegen
    (`%APPDATA%\GonkNote\Geodreieck-Light.svg` bzw. `-Dark.svg`)
  - **Bilder einfügen**: Toolbar-Button, `Strg+V` oder Drag & Drop
    (PNG, JPEG, BMP, GIF, WebP, SVG); mit Eckgriff proportional skalierbar
  - **PDF & Word einfügen** (Toolbar-Button oder Drag & Drop): mit Seitenauswahl-Dialog;
    im Notizbuch wird jede Seite eine eigene Seite zum Draufschreiben und Markieren (wie
    GoodNotes), im Whiteboard landen die Seiten als hochauflösende, skalierbare Bilder.
    **Auch sehr große PDFs**: die Datei wird nie am Stück geladen, die Auswahl zeigt
    schnelle Vorschaubilder, und in voller Auflösung gerendert werden nur die Seiten, die
    du wirklich einfügst (aus 600 Seiten fünf auszuwählen dauert Sekunden statt Minuten)
  - Undo/Redo (`Strg+Z` / `Strg+Y`), Zoom (`Strg+Mausrad`), Pan (mittlere Maustaste,
    Leertaste, Hand-Werkzeug)
- **Touch-Gesten**: 1 Finger verschiebt die Ansicht, 2 Finger zoomen (Pinch) und
  verschieben, Drei-Finger-Doppeltipp = Rückgängig
- **Einstellungs-Seitenleiste rechts** (Zahnrad): Seitenmuster und -farbton, Format
  (A4/A3, Hoch-/Querformat, Vorlage für neue Seiten), Formen-Optionen, Text-Optionen,
  Cover-Gestaltung und ein **Export-Abschnitt** (PDF/PNG direkt aus der Leiste) —
  Änderungen wirken sofort
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
  - Rechtschreibprüfung (Deutsch/Englisch umschaltbar in der Statusleiste; die gewählte
    Sprache gilt fürs ganze Dokument und wird sofort neu geprüft) mit Korrekturvorschlägen.
    Hinweis: Die Markierungen kommen von Windows – ist für eine Sprache kein Wörterbuch
    installiert (z. B. Englisch auf einem rein deutschen Windows), erscheint ein Warnsymbol
    und es wird nichts angestrichen; die Sprache lässt sich in den Windows-Einstellungen
    ergänzen. Lineal, Statusleiste (Wörter, Zoom), Überschriften-Navigator, Seitenumbruch-Marken
- **Import**: Bilder, PDF, DOCX und **Markdown (`.md`)** — DOCX/Markdown als neue Textdokumente
- **Export**: Textdokument → PDF / DOCX / Markdown / PNG, Whiteboard/Notizbuch → PDF / PNG —
  über „Datei → Exportieren" oder den Export-Abschnitt der Einstellungs-Seitenleiste. Fehlen
  zu einem Bild die Originaldaten, sagt Gonk Note das nach dem Export, statt stillschweigend
  in schlechterer Qualität zu exportieren
- **Dark-/Light-Mode** (`Strg+T`) fürs App-Design — Seiten und Schreibflächen bleiben
  standardmäßig hell; die Fenster-Titelleiste folgt dem Theme (im Dark Mode dunkel).
  Seitenleiste einklappbar (`Strg+B`)
- **Maximiertes Fenster ohne Titelleiste**: Wird das Fenster maximiert („in Groß"),
  blendet sich die Titelleiste aus und die Menüleiste rückt nach oben. Fährst du mit der
  Maus an den oberen Fensterrand, gleitet eine Titelleiste (Minimieren/Wiederherstellen/
  Schließen) sanft wieder ein. Doppelklick auf die Menüleiste (oder die Windows-Standard-
  befehle) stellt das Fenster wieder her
- **Persistenz**: LiteDB-Datei unter `%APPDATA%\GonkNote\gonknote.db` für Texte, Striche und
  Struktur; **Bilder, importierte PDF- und Word-Seiten liegen daneben** in
  `%APPDATA%\GonkNote\gonknote.blobs\` — je Bild eine Datei. Autosave alle 30 s, Speichern
  beim Schließen von Tabs und der App.
  **Für eine Sicherung beides mitnehmen: die Datei *und* den Ordner.**
  Bilder, auf die kein Dokument mehr zeigt, wandern in `gonknote.papierkorb\` und werden erst
  nach 30 Tagen endgültig entfernt; wird ein Bild vorher wieder gebraucht, holt Gonk Note es
  von selbst zurück
- **Große Dokumente**: Originale werden unverändert abgelegt und beim Export unverändert
  zurückgeschrieben; angezeigt wird eine verkleinerte Ableitung. Ein Word-Dokument mit Fotos
  kommt dadurch genauso groß wieder heraus, wie es hereinkam (vorher das Achtfache), und ein
  Notizbuch mit 120 importierten Seiten (118 MB) lässt sich speichern und öffnen.
  Beim PDF-Import bleibt der Speicherbedarf flach: 530 MB gerenderte Seiten laufen mit rund
  114 MB Spitze durch. Im Text-Editor sind mehrere hundert Seiten kein Problem — ein
  500-Seiten-Dokument öffnet in etwa 1,8 Sekunden
- **Speicherbedarf**: rund 180 MB nach dem Start, mit einem geöffneten Notizbuch etwa 290 MB —
  unabhängig davon, wie groß das Dokument ist, weil nur die gerade sichtbaren Seiten im
  Speicher liegen (Budget 96 MB). Beim Schließen einer Registerkarte wird freigegeben; der
  Undo-Verlauf ist auf 200 Schritte begrenzt, damit er in langen Sitzungen nicht wächst

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
| Kernlogik | eigene Bibliothek `GonkNote.Core` (net8.0) — ohne UI-Abhängigkeiten |

Die Anwendung besteht aus zwei Projekten: der WPF-Oberfläche und einer Kernbibliothek
ohne UI-Bezug. Das hält die Schichten sauber — Datenmodell, Persistenz und die
Zeichenroutinen des Whiteboards sind unabhängig von der Oberfläche.

```
GonkNote/                    WPF-Oberfläche (net8.0-windows)
├─ App.xaml(.cs)           Einstieg, Theme-Initialisierung, --db-Argument
├─ MainWindow.xaml(.cs)    Menü, Ordnerbaum (Drag & Drop, Anpinnen), Tab-Verwaltung
├─ ViewModels/             MainViewModel, Tab-VMs, Baum-VM, MVVM-Basis
├─ Views/                  WhiteboardView (Skia-Canvas) und TextEditorView — beide nach
│                          Themen in partial-Dateien geteilt (Eingabe, Auswahl, Rendern,
│                          Import, Einstellungen …), dazu die Dialoge
├─ Services/               Import/Export (DOCX, PDF, Markdown), OCR, Theme, Textstile
│  └─ Localization/        Sprachumschaltung: Loc (Nachschlagen) + je eine Tabelle DE/EN
└─ Themes/                 Light.xaml, Dark.xaml, Styles.xaml

GonkNote.Core/               Kernlogik ohne UI-Bezug (net8.0), Namensraum GonkNote.Core.*
├─ Models/                 NoteItem (Baum), Whiteboard-Elemente, Enums
├─ Services/               DatabaseService (LiteDB), BlobStore (Bilder/PDFs neben der
│                          Datenbank), UndoStack, ImageCache, PDF-Import
├─ Rendering/              Skia-Zeichenroutinen des Whiteboards, Geodreieck-Overlay
└─ Editing/                Punktgenaues Radieren
```

## Lizenz

Gonk Note steht unter der **MIT-Lizenz** — siehe [LICENSE](LICENSE).
Copyright © 2026 Manuel Toegel.

Kurz gesagt: benutzen, verändern und weitergeben ist erlaubt, auch kommerziell; der
Lizenztext und der Copyright-Hinweis müssen mitgeliefert werden, und es gibt keine
Garantie.

Die mitgelieferten **Notizbuch-Cover** (`Assets/Covers/**`) und das **App-Icon** sind eigene
Werke und fallen unter dieselbe Lizenz. Darüber hinaus liefert Gonk Note bewusst keine
Grafiken mit — weder Sticker noch eine Geodreieck-Vorlage; beides kannst du in
`%APPDATA%\GonkNote` selbst hinterlegen (siehe
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)).

**Sticker liefert Gonk Note bewusst keine mit** — das Werkzeug arbeitet nur mit Bildern, die
du selbst unter `%APPDATA%\GonkNote\Stickers` ablegst.

### Verwendete Bibliotheken

Alle Abhängigkeiten sind permissiv lizenziert und mit der MIT-Lizenz vereinbar. Die
Vermerke, die Apache-2.0 und BSD-3 bei einer Weitergabe verlangen (insbesondere für die
Single-File-Exe), stehen in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md):

| Baustein | Zweck | Lizenz |
|---|---|---|
| [LiteDB](https://www.litedb.org/) | Persistenz | MIT |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | Whiteboard-Rendering | MIT |
| [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) | SVG-Rasterung | MIT |
| [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK) | DOCX-Import/-Export | MIT |
| [Docnet.Core](https://github.com/GowenGit/docnet) / PDFium | PDF-Import | MIT / BSD-3-Clause |
| [Tesseract](https://github.com/charlesw/tesseract) + `tessdata` (deu, eng) | OCR | Apache-2.0 |
