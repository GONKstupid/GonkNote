[← Index: HANDOFF.md](../HANDOFF.md)

## 3. Struktur

```
gonk-note-V2/
├─ GonkNote.slnx                 (neues Solution-Format des .NET-10-SDK)
├─ Directory.Build.props         Nullable, ImplicitUsings, LangVersion, Version, DebugType
├─ Directory.Packages.props      zentrale NuGet-Versionen
│
├─ src/
│  ├─ GonkNote.Core/             net10.0 · KEINE UI-Abhängigkeit · das Zentrum
│  │  ├─ Models/                 NoteItem, Whiteboard-Elemente
│  │  ├─ Platform/               die Naht zu den Köpfen (§4.7)        ← neu in Phase 2
│  │  │                          IAppPaths, IDialogService, IFileDialog, IClipboard,
│  │  │                          IThemeHost, IShell, IUiScheduler, IOcrEngine,
│  │  │                          ISpellChecker, IPdfRasterizer, IFontProvider,
│  │  │                          IDocumentIo — gebündelt in IPlatformServices
│  │  ├─ Rendering/              WbRenderer (Skia), WbAidRenderer (Geodreieck), WbImages (§7),
│  │  │                          WbImagePrep (Bildimport + OCR-Vorbereitung, §4.7),
│  │  │                          TdRenderer (die gesetzte Seite → Pixel, seit §4.25 auch
│  │  │                          die sieben Diagrammarten)             ← neu in §4.24
│  │  ├─ Services/               DatabaseService (SQLite, §4.8), GonkJson (Source-Generator),
│  │  │                          ILegacyDatabaseReader, BlobStore, ImageCache, UndoStack,
│  │  │                          PdfImporter, DocumentHealth,
│  │                          Fehlerprotokoll — das Fehlerprotokoll MIT Obergrenze;
│  │                          beide Koepfe hatten je eine eigene Fassung OHNE
│  │                          (272 MB an einem Nachmittag, §4.66) ← neu in §4.66
│  │  ├─ Editing/                WbErase — punktgenaues Radieren
│  │  │                          WbHit — Trefferprüfung und Lasso (§4.10)  ← neu in Phase 3
│  │  │                          seit §4.13 von BEIDEN Köpfen benutzt
│  │  │                          WbHandles — die Griffe an der Auswahl (§4.51)
│  │  │                          WbEinfuegen — wo Importiertes landet (§4.57)
│  │  │                          WbZeichenhilfe — Lineal und Geodreieck (§4.59)
│  │  │                          WbKlon — Klonen und Einfügen (§4.61)
│  │  │                          WbZahlenblock — die Zifferneingabe (§4.61)
│  │  │                          WbSchnellaktionen — Zustand und Lage (§4.61)
│  │  │                          WbLeiste — Ordnung und Klappgruppen (§4.61)
│  │  ├─ Theming/                die Farbtabelle (§4.9)              ← neu in Phase 3
│  │  │                          ThemeColor (20 Farben), HexColor, ThemeDefinition,
│  │  │                          Themes.Light/.Dark — ein Theme ist eine Datentabelle
│  │  │                          ThemeFile (die JSON-Datei), ThemeLibrary (der Ordner
│  │  │                          Themes/ im Datenordner)              ← neu in §4.107
│  │  │                          Fonts.cs — dasselbe für Schriften: FontRole (5 Rollen),
│  │  │                          FontScheme, Fonts.Standard, die mitgelieferten
│  │  │                          Schnitte                             ← neu in §4.26
│  │  ├─ Text/                   Markdown — der Zerleger hinter den vier mitgelieferten
│  │  │                          Dokumenten (§4.12)                  ← neu in Phase 3
│  │  │                          seit §4.13 von BEIDEN Köpfen benutzt
│  │  │                          TdDocument/TdSection/TdBlock/TdInline + TdFormat — das
│  │  │                          eigene Dokumentmodell, TdList (Listen samt der
│  │  │                          gerechneten Nummerierung), TdField (Felder, Verweise und
│  │  │                          die Feldwerte), TdToc (das gerechnete Inhaltsverzeichnis),
│  │  │                          TdGraphic (Bilder und Diagramme), TdImages (die Naht zu
│  │  │                          den Bilddaten), TdJson (Speicherformat),
│  │  │                          — die Übernahme aus dem Altformat steht als FlowZuTd
│  │  │                          im WPF-Kopf, weil nur der RTF lesen kann (§4.22)
│  │  │                          TdDocx (DOCX in beide Richtungen), TdLayout (Zeilen- und
│  │  │                          Seitenumbruch) hinter der Naht ITdTextMeasure/
│  │  │                          TdSkiaMeasure                        ← neu in Phase 4
│  │  │                          TdMarkdown (Markdown-Export gegen das Modell — der
│  │  │                          erste Exporter ohne WPF)             ← neu in §4.23
│  │  │                          TdChartLayout (aus den Zahlen eines Diagramms wird ein
│  │  │                          Bild — in Zentimetern, ohne eine Zeile Skia) ← neu in §4.25
│  │  └─ Localization/           Loc + LocGerman + LocEnglish        ← neu in Phase 0
│  │
│  ├─ GonkNote.ViewModels/       net10.0 · EIGENE Assembly seit Phase 2 (§4.7)
│  │                             MainViewModel, DocumentTabViewModel, TreeItem…, Gallery…, Mvvm
│  │
│  ├─ GonkNote.Legacy/           net10.0 · der EINZIGE Ort mit LiteDB      ← neu in Phase 2
│  │                             LiteDbReader + ModelTypeBinder: liest eine Altdatenbank,
│  │                             damit DatabaseService sie einmalig nach SQLite überträgt.
│  │                             Windows und Linux referenzieren es, iOS nicht (§4.8)
│  │
│  ├─ GonkNote.Ocr/              net10.0 · der EINZIGE Ort mit Tesseract   ← neu in §4.64
│  │                             TesseractOcrEngine — die Texterkennung, EINE Datei für
│  │                             BEIDE Köpfe (vorher OcrService im WPF-Kopf, 93 Zeilen,
│  │                             und NoOcrEngine im Linux-Kopf).
│  │                             TesseractLinux — der Handgriff, ohne den unter Linux
│  │                             nichts lädt: DllImportResolver für libdl und die zwei
│  │                             Verweise unter x64/ (§4.63 misst es, §4.64 baut es).
│  │                             Eigenes Projekt aus demselben Grund wie Legacy: das
│  │                             Paket bindet nativ, und iOS bekommt Vision statt
│  │                             Tesseract. ⚠ Die Köpfe referenzieren `Tesseract`
│  │                             ZUSÄTZLICH selbst — build\Tesseract.targets wandert
│  │                             nicht durch eine ProjectReference (§4.64)
│  │
│  ├─ GonkNote.Avalonia/         net10.0 · LINUX-KOPF (§4.9)             ← neu in Phase 3
│  │  │                          Läuft auch unter Windows — genau deshalb wird Phase 3
│  │  │                          hier entwickelt und nicht auf dem Laptop (§5b)
│  │  ├─ Program.cs, App.axaml(.cs)
│  │  ├─ MainWindow.axaml(.cs)   dazu .Ziehen.cs (Drag & Drop im Baum) und
│  │  │                          .Titelleiste.cs (maximiertes Fenster) — §4.12
│  │  ├─ Platform/               die Umsetzungen zu Core/Platform:
│  │  │                          Avalonia* je Schnittstelle + AvaloniaPlatformServices,
│  │  │                          AvaloniaThemeHost (baut die Ressourcen aus der Farbtabelle),
│  │  │                          Modal.cs (synchron ↔ async, die größte Naht — §7)
│  │  ├─ Views/                  Converters, MessageWindow (Ersatz der MessageBox),
│  │  │                          AboutWindow + GuideWindow, MarkdownView (malt, was
│  │  │                          Core/Text zerlegt hat, §4.12),
│  │  │                          SkiaCanvas (der Weg an Avalonias SKCanvas, §4.10),
│  │  │                          WhiteboardView + .Render + .Input + .Einstellungen
│  │  │                            + .Texterkennung, dazu TexterkennungWindow (§4.64)
│  │  ├─ Themes/Styles.axaml     Form und Vektor-Symbole — KEINE Farben (die kommen aus Core)
│  │  └─ Services/               EmbeddedDocs (avares:// statt pack://, §4.12)
│  │     └─ Localization/        TExtension + LocText (§7 „Übersetzung im Linux-Kopf")
│  │
│  └─ GonkNote.Wpf/              net10.0-windows10.0.19041.0 · Windows-Kopf (AssemblyName bleibt GonkNote)
│     ├─ App.xaml(.cs), MainWindow.xaml(.cs)
│     ├─ Platform/               die Umsetzungen zu Core/Platform     ← neu in Phase 2
│     │                          Wpf* je Schnittstelle + WpfPlatformServices (das Bündel),
│     │                          WpfThemeHost (war die statische Klasse ThemeService),
│     │                          WpfDocumentIo (die FlowDocument-Naht aus §4.1; seit
│     │                          §4.22 auch die Übernahme der Bestandsdokumente)
│     ├─ Views/                  Whiteboard (Partials), TextEditor (Partials), Dialoge
│     ├─ Themes/                 nur noch Styles.xaml — Light/Dark sind mit §4.107
│     │                          gelöscht, die Farben kommen aus Core
│     └─ Services/               alles mit WPF-Bezug (§4.1):
│                                MarkdownImporter, MarkdownFlow,
│                                PdfExporter (seit §4.27 nur noch Whiteboard/Notizbuch),
│                                FlowZuTd (die Übernahme ins eigene Modell, §4.22),
│                                TdZuFlow (der Rückweg, damit der Editor ein
│                                importiertes Modell anzeigen kann)  ← neu in §4.23
│                                TextStyles, DocumentImages, EmbeddedDocs,
│                                SpellCheckSupport, TitleBarTheme,
│                                — OcrService ist mit §4.64 nach GonkNote.Ocr gewandert
│                                  und heißt dort TesseractOcrEngine,
│                                WindowBounds, Localization/TExtension.cs
│
├─ .github/workflows/ci.yml      zwei Läufe: windows (alles), ubuntu (Core, ViewModels,
│                                Legacy, **Avalonia**) — §4.6, seit Phase 3 §4.9
│
├─ tests/
│  ├─ GonkNote.Core.Tests/       net10.0 · läuft auch unter Linux · 953 Tests
│  │  └─ Snapshots/*.sha256      Pixelhashes des Renderers (Golden-Files)
│  └─ GonkNote.Wpf.Tests/        net10.0-windows · nur Windows · 59 Tests
│     ├─ Fixtures/               referenz.md, referenz-docx.txt (Golden-Files)
│     └─ ThemeschluesselTests.cs prüft jeden „Brush.X"/„Color.X" im Quelltext gegen die
│                                Farbtabelle (§4.107; hieß bis dahin FarbtabelleTests und
│                                verglich Core/Theming mit Themes/*.xaml, §4.9)
│
├─ tools/                        Werkzeuge, KEIN Produktivcode, nicht in der Solution
│  ├─ stylus-prototyp/           Messwerkzeug zu §5a — Ergebnis liegt dort vor
│  │  ├─ GonkNote.StylusProbe/   Avalonia-Prototyp (Druck als Kreisradius)
│  │  ├─ evdev_beschreiben.py    Achsenbereiche des Digitizers (EVIOCGABS)
│  │  ├─ evdev_druck.py          Druckverlauf mitschneiden und auswerten
│  │  └─ messungen/              Rohberichte der Läufe
│  ├─ schau.ps1                  App mit DB-Kopie starten und fotografieren (§8)
│  ├─ klick.ps1                  ein Klick / Tastendruck + neues Foto
│  ├─ kette.ps1                  mehrere Klicks in EINEM Durchgang — für Menüpfade (§7);
│  │                             seit §4.13 auch ZIEHEN ("x1,y1>x2,y2>…"), ohne das sich
│  │                             Lasso und Verschieben nicht fernsteuern lassen
│  └─ linux/                     das Gegenstück dazu unter Linux (§4.10)  ← neu in Phase 3
│     ├─ schau.sh                Kopf starten und fotografieren
│     ├─ klick.sh                Schritte abarbeiten + Foto
│     └─ zeiger/                 klickt und tippt über X11/XTEST, ohne Fremdpaket
│
├─ Assets/  tessdata/            1:1 aus V1, in der Wurzel
├─ Docs/                         HANDOFF.md + Design-Konzepte + seit V2-131 die
│                                Anleitung: ERSTE-SCHRITTE / GETTING-STARTED (in der App)
│                                und INSTALLIEREN / INSTALL (nur Repo)
│  └─ Fonts/<Familie>/           die fünf mitgelieferten Schriften samt OFL.txt (§4.26).
│                                GonkNote.Core.csproj kopiert sie als `Content` — damit
│                                landen sie in der Ausgabe JEDES Kopfes und beider
│                                Testprojekte, ohne dass vier .csproj dieselbe Liste führen
└─ LICENSE, README(.en).md, THIRD-PARTY-NOTICES.md, CONTRIBUTING.md
```

**`tools/` steht bewusst neben `src/`, nicht darin**, und ist **nicht** in `GonkNote.slnx`
eingetragen: was dort liegt, ist Wegwerf-Werkzeug und soll nicht mit den Produktivprojekten
verwechselt oder versehentlich mitgebaut werden. `GonkNote.StylusProbe` ist zusätzlich aus der
zentralen Paketverwaltung ausgeklinkt (`ManagePackageVersionsCentrally=false`), damit seine
Avalonia-Version nicht die Versionswahl für Phase 3 vorwegnimmt.

**Noch nicht angelegt:** `src/GonkNote.iOS` (**Phase 6** — seit dem 2026-08-18 nachgestellt, §6). Bewusst — leere Projekte, die nicht
bauen, sind nur Ballast; sie entstehen, wenn ihre Phase beginnt.

**Faustregel:** Nach Phase 2 darf in den Plattform-Köpfen *nur noch* stehen, was Pixel
zeichnet oder Eingaben entgegennimmt. Alles andere gehört in Core.

---
