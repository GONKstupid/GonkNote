# Gonk Note

Moderne, offline-fähige Notiz-App für **Windows 11 und Linux** — eine Alternative zu
GoodNotes mit Notizbüchern, Whiteboards und Textdokumenten. Stylus-freundlich (Wacom,
Microsoft Pen, …), ohne Cloud, ohne Installation, ohne Adminrechte.

> **Neu hier?** Die Schritt-für-Schritt-Anleitung
> **[Erste Schritte](ERSTE-SCHRITTE.md)** führt dich in rund 10 Minuten vom Klonen des
> Repos bis zur ersten beschriebenen, exportierten und gesicherten Notiz. Diese Seite
> hier beschreibt, *was* Gonk Note kann — die Anleitung zeigt, *wie* du anfängst.

*(Deutsche Fassung. Die englische ist `README.en.md`. Im Programm richtet sich diese Seite
nach der Sprache, die du unter Ansicht → Sprache gewählt hast.)*

## Zwei Ausgaben, eine App

Gonk Note gibt es als **Windows-Ausgabe** (WPF) und als **Linux-Ausgabe** (Avalonia). Beide
lesen dieselbe Datenbank, benutzen dieselbe Kernbibliothek und zeichnen mit demselben
Renderer — ein Notizbuch sieht auf beiden gleich aus.

**Beide Ausgaben können dasselbe** — alles, was auf dieser Seite steht, gilt für beide.
Ordnerbaum samt Drag & Drop, Anpinnen und Favoriten, Galerie, zwei Sprachen, Dark/Light,
die einblendbare Titelleiste, Hilfe und Über-Dialog; die Zeichenfläche für **Notizbuch und
Whiteboard** mit Stift, Bleistift und Textmarker samt Druck und Neigung, punktgenauem
Radieren, Lasso und Verschieben, Formen-Stift, Textfeldern, Notizzetteln, Stickern, Import,
Lineal und Geodreieck, Zahlenblock, Schnellaktionen und **Texterkennung**; Seiten blättern,
anlegen und löschen, Seiteneinstellungen, Zoom, Finger-Gesten, Rückgängig und Speichern —
und der **Textdokument-Editor**.

Die kurze Liste darunter sagt, wo sie sich trotzdem unterscheiden. **Sie ist vollständig:**
Jeder Punkt darin ist gemessen, keiner geschätzt.

**Textdokumente werden angezeigt und beschrieben.** Ein Textdokument öffnet sich als
gesetztes Papier — mit Überschriften, Zeichenformaten, Listen, Tabellen, Bildern, Diagrammen
sowie Kopf- und Fußzeile, seitenweise zum Blättern, mit Zoom, „Seitenbreite" und „Ganze
Seite". **Angezeigt wird dabei genau das, was auch exportiert würde** — derselbe Umbruch,
derselbe Zeichner. Dazu **Import** (DOCX, Markdown) und **Export** (PDF, DOCX, Markdown, PNG).

**Was der Linux-Ausgabe noch fehlt** — jeweils mit Grund, keines davon ist vergessen:

| Fehlt | Warum |
|---|---|
| **Rechtschreibprüfung** | Sie hängt in der Windows-Ausgabe an einem Windows-Dienst. Ein Gegenstück ist der **erste Punkt nach der Portierung** und fest eingeplant |
| **Zusammengesetzte Zeichen** (`´` + `e` → `é`) kommen nicht an | Ein Fehler im Fenster-Baustein von Avalonia unter Linux, nicht in Gonk Note. Er ist dort gemeldet; einfache Zeichen und Umlaute sind nicht betroffen |
| **Lineal** über dem Textdokument | Bewusst weggelassen: In der Windows-Ausgabe ist es eine Zierleiste ohne Funktion — die Ränder lassen sich dort nicht ziehen. Was es zeigt, steht im Reiter „Layout" in Zahlen und ist dort auch änderbar |
| **Bestandsdokumente aus der Windows-Ausgabe** erscheinen erst, nachdem sie dort einmal geöffnet und gespeichert wurden | Ihr altes Format liest nur Windows. Der Inhalt bleibt dabei unangetastet |

Und umgekehrt fehlt der **Windows**-Ausgabe eines: Ihr Texteditor zeigt **keine Seitenzahlen**.
Er rechnet keine Seiten, sondern lässt Windows den Text fließen; die Linux-Ausgabe setzt
echte Seiten und weiß deshalb, auf welcher man steht.

Nichts davon geht dabei verloren: was eine Ausgabe nicht anzeigen kann, wird auch nicht
angefasst — eine Datei, die du unter Windows angelegt hast, kommt unter Linux unverändert
wieder heraus.

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
  - **Zeichenhilfen**: Lineal (`R`) und Geodreieck (`D`), drehbar/einrastend. Das Geodreieck
    ist eine mitgelieferte Vektorgrafik mit Millimeter- und Gradskala (je eine Fassung für
    helles und dunkles App-Design). Eine eigene Zeichnung geht vor: als
    `%APPDATA%\GonkNote\Geodreieck-Light.svg` bzw. `-Dark.svg` hinterlegen
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
- **Persistenz**: SQLite-Datei `gonknote.sqlite` im Datenordner für Texte, Striche und
  Struktur; **Bilder, importierte PDF- und Word-Seiten liegen daneben** in
  `gonknote.blobs\` — je Bild eine Datei. Der Datenordner ist unter Windows
  `%APPDATA%\GonkNote`, unter Linux `~/.config/GonkNote`; **Hilfe → Über Gonk Note** zeigt
  ihn an. Autosave alle 30 s, Speichern beim Schließen von Tabs und der App.
  **Für eine Sicherung beides mitnehmen: die Datei *und* den Ordner.**
  Bis Version 0.2.0 hieß die Datei `gonknote.db` und war eine LiteDB-Datei. Sie wird beim
  ersten Start danach **einmalig übertragen** und bleibt anschließend unverändert daneben
  liegen — als Rückweg, solange du sie behältst.
  **Sichere ab jetzt `gonknote.sqlite`, nicht mehr `gonknote.db`:** die alte Datei wächst
  nicht mehr mit und wäre nach kurzer Zeit ein veralteter Stand.
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

Voraussetzung: .NET SDK 10 oder neuer.

**Immer projektbezogen bauen, nie die ganze Solution.** Sie enthält beide Ausgaben, und die
Windows-Ausgabe lässt sich unter Linux nicht übersetzen — das ist so gewollt und kein
Fehler.

### Windows

```powershell
# Entwicklung
dotnet run --project src/GonkNote.Wpf

# Single-File-Exe (selbständig, keine .NET-Installation nötig)
dotnet publish src/GonkNote.Wpf -c Release
# Ergebnis: src/GonkNote.Wpf/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/GonkNote.exe
```

Hinweis: WPF unterstützt kein Assembly-Trimming (`PublishTrimmed`); die Exe wird stattdessen
komprimiert (`EnableCompressionInSingleFile`).

### Linux

```bash
dotnet run --project src/GonkNote.Avalonia
```

Das System braucht dafür **fontconfig und mindestens eine Schrift** — ohne sie bleibt jeder
gezeichnete Text leer. Auf Arch-artigen Systemen:

```bash
sudo pacman -S fontconfig ttf-dejavu
```

Unter Wayland läuft Gonk Note über XWayland; ein eigener Wayland-Pfad existiert im Toolkit
nicht. Druck und Neigung des Stifts kommen darüber vollständig an.

### Beides

Für Tests kann mit `--db <pfad>` eine alternative Datenbank verwendet werden —
**auf einer Kopie arbeiten, nie auf dem Bestand.**

Was neben dem Programm liegen muss (`tessdata` für die Texterkennung unter Windows) und wie
es danach weitergeht, steht in [Erste Schritte](ERSTE-SCHRITTE.md).

## Architektur

| Baustein | Technologie |
|---|---|
| Oberfläche Windows | WPF (.NET 10), MVVM, dynamische Theme-ResourceDictionaries |
| Oberfläche Linux | Avalonia 12 (.NET 10), dieselben ViewModels, Farben aus einer Tabelle in `GonkNote.Core` |
| Whiteboard-Rendering | SkiaSharp — unter Windows über `SKElement`, unter Linux über Avalonias eigene Skia-Leinwand; **derselbe Renderer, dieselben Pixel** |
| Stifteingabe | WPF-Stylus-Events bzw. Avalonia-Pointer, beide mit Druck und Neigung |
| Persistenz | SQLite (`Microsoft.Data.Sqlite`); Dokumente als JSON, gelesen und geschrieben über einen Source-Generator |
| Kernlogik | eigene Bibliothek `GonkNote.Core` (net10.0) — ohne UI-Abhängigkeiten |

Die Anwendung besteht aus **einem Kern und zwei Oberflächen**. Datenmodell, Persistenz,
Zeichenroutinen, Farben und Übersetzungen liegen im Kern; in einer Oberfläche steht nur,
was Pixel zeichnet oder Eingaben entgegennimmt. Deshalb gibt es die Linux-Ausgabe
überhaupt — sie hat nichts davon nachgebaut.

```
src/
├─ GonkNote.Core/            Kernlogik ohne UI-Bezug (net10.0), Namensraum GonkNote.Core.*
│  ├─ Models/               NoteItem (Baum), Whiteboard-Elemente, Enums
│  ├─ Platform/             die Naht zu den Oberflächen: Dateidialoge, Zwischenablage,
│  │                        Theme, OCR, Rechtschreibung … als Schnittstellen
│  ├─ Services/             DatabaseService (SQLite), BlobStore (Bilder/PDFs neben der
│  │                        Datenbank), UndoStack, ImageCache, PDF-Import
│  ├─ Rendering/            Skia-Zeichenroutinen des Whiteboards, Geodreieck-Overlay
│  ├─ Editing/              Punktgenaues Radieren, Trefferprüfung und Lasso
│  ├─ Text/                 Markdown-Zerleger für die mitgelieferten Dokumente
│  ├─ Theming/              die Farbtabelle: ein Design ist 20 benannte Farben
│  └─ Localization/         Loc (Nachschlagen) + je eine Tabelle DE/EN
│
├─ GonkNote.ViewModels/      MainViewModel, Tab-VMs, Baum-VM, MVVM-Basis (net10.0) —
│                            von beiden Oberflächen benutzt
│
├─ GonkNote.Legacy/          liest Datenbanken bis Version 0.2.0 ein (LiteDB); der einzige
│                            Ort im Projekt, der dieses Paket noch kennt
│
├─ GonkNote.Wpf/             Windows-Oberfläche (net10.0-windows)
│  ├─ Platform/             die Umsetzungen zu Core/Platform
│  ├─ Views/                WhiteboardView und TextEditorView — beide nach Themen in
│  │                        partial-Dateien geteilt, dazu die Dialoge
│  ├─ Services/             Import/Export (DOCX, PDF, Markdown), OCR, Textstile
│  └─ Themes/               Light.xaml, Dark.xaml, Styles.xaml
│
└─ GonkNote.Avalonia/        Linux-Oberfläche (net10.0, läuft auch unter Windows)
   ├─ Platform/             dieselben Schnittstellen, für Avalonia umgesetzt
   ├─ Views/                WhiteboardView (Eingabe, Rendern, Einstellungen), Dialoge,
   │                        Markdown-Darstellung
   └─ Themes/Styles.axaml   Form und Vektor-Symbole — die Farben kommen aus dem Kern
```

## Lizenz

Gonk Note steht unter der **MIT-Lizenz** — siehe [LICENSE](LICENSE).
Copyright © 2026 Manuel Toegel.

Kurz gesagt: benutzen, verändern und weitergeben ist erlaubt, auch kommerziell; der
Lizenztext und der Copyright-Hinweis müssen mitgeliefert werden, und es gibt keine
Garantie.

Die mitgelieferten **Notizbuch-Cover** (`Assets/Covers/**`), die **Geodreieck-Grafiken**
(`Assets/Geodreieck-Light.svg`, `-Dark.svg`) und das **App-Icon** sind eigene Werke und
fallen unter dieselbe Lizenz (siehe [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)).

**Sticker liefert Gonk Note bewusst keine mit** — das Werkzeug arbeitet nur mit Bildern, die
du selbst unter `%APPDATA%\GonkNote\Stickers` ablegst.

### Verwendete Bibliotheken

Alle Abhängigkeiten sind permissiv lizenziert und mit der MIT-Lizenz vereinbar. Die
Vermerke, die Apache-2.0 und BSD-3 bei einer Weitergabe verlangen (insbesondere für die
Single-File-Exe), stehen in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md):

| Baustein | Zweck | Lizenz |
|---|---|---|
| [SQLite](https://www.sqlite.org/) ueber [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/) | Persistenz | Public Domain / MIT |
| [LiteDB](https://www.litedb.org/) | liest Datenbanken bis Version 0.2.0 ein | MIT |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | Whiteboard-Rendering | MIT |
| [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) | SVG-Rasterung | MIT |
| [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK) | DOCX-Import/-Export | MIT |
| [Docnet.Core](https://github.com/GowenGit/docnet) / PDFium | PDF-Import | MIT / BSD-3-Clause |
| [Tesseract](https://github.com/charlesw/tesseract) + `tessdata` (deu, eng) | OCR | Apache-2.0 |
| [Lucide](https://lucide.dev) | Symbole der Oberfläche | ISC (teils MIT) |
| [Inter](https://github.com/rsms/inter) | Schrift der Oberfläche | SIL OFL 1.1 |
| [Source Sans 3](https://github.com/adobe-fonts/source-sans) | Grundschrift der Textdokumente | SIL OFL 1.1 |
| [JetBrains Mono](https://github.com/JetBrains/JetBrainsMono) | Code und Festbreitentext | SIL OFL 1.1 |
| [Space Grotesk](https://github.com/floriankarsten/space-grotesk) | Cover-Titel und große Überschriften | SIL OFL 1.1 |
| [Geist](https://github.com/vercel/geist-font) | Textfelder und Notizzettel im Whiteboard | SIL OFL 1.1 |

**Die Symbole kommen aus einer Tabelle im Programm**, nicht aus einer Icon-Schrift. Auch das ist
Absicht und derselbe Grund wie bei den Schriften: „Segoe Fluent Icons" gehört Microsoft, darf
nicht mitgeliefert werden und fehlt unter Linux — dort stünde an jeder Stelle ein leeres
Kästchen. Sieben Formen (Notizbuch, Textdokument, Whiteboard, Geodreieck, Seitenbreite, Ganze
Seite, Wiederherstellen) sind eigene, der Rest stammt aus Lucide.

**Die fünf Schriften werden mitgeliefert** und liegen als `Fonts\`-Ordner neben dem Programm.
Das ist Absicht: „Segoe UI" gibt es unter Linux nicht, und auf keinem Linux-System ist eine
bestimmte Schrift garantiert — ohne mitgelieferte Schriften sähe dasselbe Dokument auf beiden
Ausgaben verschieden aus. Die Lizenztexte liegen je Familie als `OFL.txt` daneben und gehören
bei einer Weitergabe dazu.
