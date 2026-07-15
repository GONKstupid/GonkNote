# Gonk Note — Projektübergabe (Stand: 2026-07-15, Phase 3 laufend)

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
│  │        .Find/.Lists/         + Bibliotheken (.Lists.cs), Formen-Palette + Diagramm
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

**⚠️ OFFENE WUNSCHLISTE Fixes-Runde 8 (Nutzer, 2026-07-15) — in Arbeit:**
*Text-Editor:* Breite/Höhe von Formen & Diagrammen ändern · Diagrammfarben im
Erstellungsprozess wählbar · Formen in den Hintergrund legen (hinter Text) ·
Tabellen-Formatierung in die Einstellungs-Seitenleiste statt Rechtsklick-Chaos ·
Tabellenränder mit unterschiedlicher Dicke/Art · Text-Presets auch für Kopf-/
Fußzeile, Titel, Zitate · ausgeklappte Auflistungs-Menüs wirken buggy + Kontraste
stimmen nicht (fixen).
*Whiteboard:* Rotieren UND Größe-Ändern der Auswahl „funktioniert noch nicht"
(Nutzer) → prüfen/fixen · Verschieben (V)-Icon zu **hohlem** Mauszeiger (nur Rand) ·
Lasso mit Verschieben (V) zu einer klappbaren Gruppe (wie die Stifte: nur Lasso
zeigen, Rest bei Nutzung) · Toolbar neu ordnen: **Hand (H) → Stift (S) → Radierer
(E) → Lasso (L)**, Rest beibehalten.

**⚠️ Bewusst vertagt (nicht vergessen):**
- **Rotieren von Auswahl-Objekten (Whiteboard):** sauber ginge es nur für strich-
  basierte Objekte (Punkte drehen); Rechtecke/Ellipsen/Text/Bilder/Notizzettel
  bräuchten ein `Rotation`-Feld je Element **plus** rotationsfähiges Rendering,
  Hit-Test, Bounds und Export über alle Zeichenpfade (interaktiv UND `PdfExporter`).
  Großer, invasiver Umbau. **Skalieren** ist umgesetzt (`WbElement.Scale`,
  `ScaleElementsAction`, Eckgriff an der Auswahl-Umrandung) — falls der Nutzer sagt
  „geht nicht", zuerst prüfen, ob der Griff gefunden/gezeichnet wird.
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
- Sticker (über Notizzettel hinaus), OCR, **RAM-Optimierung**, Obfuskierung → Phase 3.
  RAM: Basis ~190 MB + bis 96 MB Bild-Cache; Ziel < 200 MB braucht noch Arbeit
  (Cache-Budget senken, Render-Caching, GC-Tuning).
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

1. **Geodreieck zum Laufen bringen (§4, OFFEN #1).** Zuerst beim Nutzer erfragen,
   *welches* Verhalten fehlt (Anzeige? Einrasten? Bewegen/Drehen? Umschalten?), dann
   gezielt fixen. Nicht blind neu bauen — das Overlay stimmt bereits proportional.
2. Falls Nutzer weiteres Feedback mitbringt → **zuerst** einarbeiten. Insbesondere
   Praxis-Feedback zum **neuen Text-Editor** (Ribbon, Formatvorlagen, TOC, Export)
   einholen — der Umbau ist per Harness + Screenshots verifiziert (Light/Dark,
   PDF/DOCX-Roundtrip, OpenXML-Validierung 0 Fehler), aber noch ohne Nutzer-Test.
   Test-Harness: `%TEMP%\gonk-texttest` (End-zu-End ohne UI), UI-Skript
   `%TEMP%\gonk-verify\ui-texttest.ps1`.
3. **Datei-Einfüge-Tool** ✔ erledigt (Seitenauswahl-Dialog für PDF+DOCX, s. §5).
4. **Rest von Phase 3**: RAM-Profiling/-Optimierung (Ziel < 200 MB), weitere Sticker,
   optionales OCR, Obfuskierung, laufendes UI-Feintuning.

**Reihenfolge-Wunsch des Nutzers war:** Lineal/Geodreieck → Datei-Einfüge-Tool →
Rest Phase 3. Lineal + Notizzettel sind durch; das Geodreieck hängt an OFFEN #1.

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
