# Gonk Note — Specification

> Diese Datei ist die Quelle der Wahrheit für Gonk Note. Alles Weitere (Köpfe,
> Dokumenttypen, Doku, Pakete) leitet sich hieraus ab.
>
> **Sie wird nur bei neuen Features angefasst** — nicht bei Fehlerbehebungen,
> nicht bei Umbauten, nicht bei Versionssprüngen. Wer eine Runde Arbeit
> beschreiben will, schreibt sie in `Docs/HANDOFF.md`; wer wissen will, *was
> Gonk Note ist*, liest diese Datei. **Der Stand steht in §2 des HANDOFF, die
> Geschichte in §9 — hier steht keins von beidem.**

---

## Soul

Gonk Note ist eine **offline-fähige Notiz-App für Windows 11 und Linux** — eine
Alternative zu GoodNotes mit Notizbüchern, Whiteboards und Textdokumenten.
Stylus-freundlich (Wacom, Microsoft Pen, …), ohne Cloud, ohne Installation, ohne
Adminrechte.

**Deine Daten bleiben, wo sie sind.** Ein Ordner beim ersten Start
(`%APPDATA%\GonkNote` bzw. `~/.config/GonkNote`), sonst nirgendwo hin. Zum
Deinstallieren reicht löschen.

**Kerngedanke:** Ein Notizbuch, das einem gehört. Kein Konto, keine Synchronisation,
kein Abo — und trotzdem kein Verzicht: dieselben Werkzeuge, dieselben Dokumente,
dasselbe Aussehen auf jedem unterstützten System.

Die Reihenfolge der Plattformen ist eine Priorität und keine Technikfrage:
**Linux ist das Ziel des Projekts, Windows bleibt bestehen, iPadOS ist ein Zusatz**
(HANDOFF §1, §6).

---

## The One Load-Bearing Idea

**Ein Kopf zeichnet Pixel und nimmt Eingaben entgegen. Alles andere liegt in Core.**

- `GonkNote.Core` ist `net10.0` und hat **keine UI-Abhängigkeit**. Modelle,
  Dokument-Engine, Renderer, Persistenz, Theming, Lokalisierung — alles dort.
- Ein Plattform-Kopf (WPF, Avalonia, später iOS) erreicht das System **nur über die
  Naht** `Core/Platform/` (`IAppPaths`, `IDialogService`, `IFileDialog`,
  `IClipboard`, `IThemeHost`, `IShell`, `IUiScheduler`, `IOcrEngine`,
  `ISpellChecker`, `IPdfRasterizer`, `IFontProvider`, `IDocumentIo` — gebündelt in
  `IPlatformServices`).
- Deshalb sieht ein Notizbuch auf beiden Ausgaben **gleich aus**: beide lesen
  dieselbe Datenbank, benutzen dieselbe Kernbibliothek und zeichnen mit demselben
  Skia-Renderer.
- Einen dritten Kopf hinzuzufügen heißt: die Naht umsetzen. **Keine Änderung an
  Core nötig.**

Das ersetzt zwei getrennte Anwendungen, zwei Dateiformate und zwei Wahrheiten.

> **Faustregel (HANDOFF §3):** In den Plattform-Köpfen darf *nur noch* stehen, was
> Pixel zeichnet oder Eingaben entgegennimmt.

---

## Scope

### In

- **Drei Dokumenttypen**, jeder in eigener Registerkarte: **Notizbuch** (A4/A3-Seiten
  mit Cover), **Whiteboard** (unendliche Fläche, Punktraster), **Textdokument**.
- **Zwei Ausgaben, gleicher Funktionsumfang:** Windows (WPF) und Linux (Avalonia).
  Was die eine kann, kann die andere — Ausnahmen sind **vollständig aufgezählt** und
  gemessen, nicht geschätzt (README, „Zwei Ausgaben, eine App").
- **Verwalten:** Ordnerbaum mit Drag & Drop, Anpinnen/Favoriten, Galerie-Startansicht,
  zwei Sprachen (DE/EN) zur Laufzeit, Dark/Light, eigene Designs als JSON.
- **Zeichnen:** Stift (druckempfindlich), Bleistift, Textmarker, Formen-Stift,
  punktgenaues Radieren, Lasso und Verschieben/Skalieren/Drehen, Zeichenhilfen
  (Lineal, Geodreieck), Notizzettel, Sticker, Textfelder, Touch-Gesten.
- **Eigene Dokument-Engine** in `Core/Text/` (`TdDocument`/`TdSection`/`TdBlock`/
  `TdInline`): Formatvorlagen, Listen, Tabellen mit Formeln, Diagramme,
  Inhaltsverzeichnis, Felder und Verweise, Kopf-/Fußzeile, Wasserzeichen,
  echter Seitenumbruch.
- **Prüfung:** Rechtschreibung in beiden Ausgaben (Hunspell in reinem C#,
  Wörterbücher DE/EN mitgeliefert), dazu unter Linux feste Grammatikregeln —
  und echte Grammatik, **wenn lokal ein LanguageTool-Server läuft**.
- **OCR offline** über Tesseract (Deutsch/Englisch), ein einziger Ort:
  `GonkNote.Ocr`.
- **Import:** Bilder, PDF, DOCX, Markdown. **Export:** PDF, DOCX, Markdown, PNG.
- **Vertrieb ohne Adminrechte:** Windows-Zip (Single-File), Linux-AppImage,
  Linux-Flatpak.
- **So open source wie möglich** — MIT, Herkunft jeder fremden Zutat in
  `THIRD-PARTY-NOTICES.md`.

### Out (explizit)

- **Cloud, Konto, Synchronisation, Telemetrie.** Gonk Note ruft niemanden an. Die
  Grammatikprüfung spricht ausschließlich mit `localhost`; die öffentliche
  LanguageTool-API ist bewusst nicht unterstützt.
- **Installer und Adminrechte.** Entpacken oder Abbild starten, fertig.
- **Ein Rückzug auf einen einfachen Texteditor.** Die eigene Engine bleibt, kein
  Feature-Verlust (HANDOFF §1).
- **Reflection.Emit, LiteDB im Produktivpfad, Json ohne Source-Generator** — der
  iPad-Vertrieb verlangt NativeAOT, und das prägt die Architektur schon heute.
  LiteDB lebt nur noch in `GonkNote.Legacy`, um Altdatenbanken **einmalig** nach
  SQLite zu übertragen.
- **Neue Features in V1.** V1 ist Referenz und Notausgang; Fehlerbehebungen dort
  wandern sofort nach V2.
- **Mitgelieferte Sticker** — aus Lizenzgründen bringt Gonk Note keine mit.

---

## Architecture

### Projekte

| Projekt | Ziel | Rolle |
|---|---|---|
| `GonkNote.Core` | `net10.0` | **Das Zentrum.** Keine UI-Abhängigkeit |
| `GonkNote.ViewModels` | `net10.0` | Eigene Assembly, von beiden Köpfen benutzt |
| `GonkNote.Legacy` | `net10.0` | Der **einzige** Ort mit LiteDB — liest Altdatenbanken |
| `GonkNote.Ocr` | `net10.0` | Der **einzige** Ort mit Tesseract, inkl. Linux-Resolver |
| `GonkNote.Avalonia` | `net10.0` | **Linux-Kopf** (läuft auch unter Windows) |
| `GonkNote.Wpf` | `net10.0-windows…` | **Windows-Kopf**, AssemblyName bleibt `GonkNote` |

Der iOS-Kopf (`src/GonkNote.iOS`) wird **erst angelegt, wenn seine Phase beginnt** —
leere Projekte, die nicht bauen, sind Ballast.

### Core im Überblick

- `Models/` — `NoteItem`, Whiteboard-Elemente
- `Platform/` — die Naht zu den Köpfen
- `Rendering/` — `WbRenderer`, `WbAidRenderer`, `WbImages`, `WbImagePrep`, `TdRenderer`
- `Services/` — `DatabaseService` (SQLite), `GonkJson` (Source-Generator), `BlobStore`,
  `ImageCache`, `UndoStack`, `PdfImporter`, `DocumentHealth`, Fehlerprotokoll **mit**
  Obergrenze
- `Editing/` — `WbErase`, `WbHit`, `WbHandles`, `WbEinfuegen`, `WbZeichenhilfe`,
  `WbKlon`, `WbZahlenblock`, `WbSchnellaktionen`, `WbLeiste`
- `Theming/` — 20 benannte Farben (`ThemeColor`), `ThemeFile`/`ThemeLibrary`,
  `Fonts` mit fünf Rollen
- `Text/` — das eigene Dokumentmodell samt `TdLayout`, `TdDocx`, `TdMarkdown`,
  `TdChartLayout`, `TdRechtschreibung`, `TdGrammatik`, `TdLanguageTool`, `TdPruefung`
- `Localization/` — `Loc` + `LocGerman` + `LocEnglish`

### Persistenz

- **SQLite-Datei `gonknote.sqlite`** für Texte, Striche und Struktur.
- **Bilder und importierte Seiten liegen daneben** in `gonknote.blobs/` — je Bild
  eine Datei. Eine Sicherung braucht **beides**.
- Autosave alle 30 s, Speichern beim Schließen, Papierkorb 30 Tage.
- **Originale werden unverändert abgelegt und beim Export unverändert
  zurückgeschrieben**; angezeigt wird eine verkleinerte Ableitung.

### Leitplanken

- **Was eine Ausgabe nicht anzeigen kann, fasst sie auch nicht an** — eine Datei aus
  Windows kommt unter Linux unverändert wieder heraus.
- **Der Eingabepfad wird gegen die *Fähigkeiten* des Geräts geschrieben.** Fehlt
  Druck oder Neigung, muss der Strich trotzdem sauber aussehen — die App soll mit
  **jedem** Stylus funktionieren, nicht nur mit dem gerade angeschlossenen.
- **Farben kommen aus Core, nie aus XAML.** Ein Theme ist eine Datentabelle; ein
  Wächtertest prüft jeden `Brush.X`/`Color.X` im Quelltext dagegen.
- **Nur sichtbare Seiten liegen im Speicher** — RAM-Ziel 800 MB, Grenze 1 GB,
  unabhängig von der Dokumentgröße. Undo-Verlauf auf 200 Schritte begrenzt.
- **`tools/` steht neben `src/`, nicht darin**, und ist nicht in der Solution:
  Wegwerf-Werkzeug wird nicht mitgebaut.
- **Nie die ganze Solution bauen** — sie enthält beide Köpfe, und der Windows-Kopf
  übersetzt unter Linux nicht.

---

## Design & UI-Konzept

- **Seiten bleiben hell, auch im Dark-Mode** — Papier ist Papier. Die Titelleiste
  folgt dem Theme.
- **Ein Design ist eine JSON-Datei** mit bis zu zwanzig benannten Farben unter
  `Themes/` im Datenordner; was fehlt, kommt aus Hell bzw. Dunkel — **drei Farben
  genügen**.
- **Fünf Schriftrollen**, fünf mitgelieferte Schnitte unter `Docs/Fonts/<Familie>/`
  samt OFL — damit ein PDF überall gleich aussieht. `GonkNote.Core.csproj` kopiert
  sie in die Ausgabe **jedes** Kopfes.
- **Texteditor:** Ribbon-Layout (Start / Einfügen / Layout / Verweise, Kontext-Tab
  Tabelle) — der ausführliche Entwurf steht in
  [`Docs/Design-Konzept-Text-Editor.md`](Design-Konzept-Text-Editor.md).
- **Bedienung ohne Tastatur muss gehen:** Quick-Options-Menü auf der Leinwand per
  Rechtsklick, zweiter Stift-Taste oder langem Drücken; Zahlenblock am
  Größen-Regler.
- **Zwei Sprachen sind Pflicht, nicht Zugabe** (Dauerregel 1): Jede neue
  Oberflächenzeile entsteht in **beiden** Loc-Tabellen.

---

## Repository Layout (Ziel)

```
gonk-note-V2/
├─ GonkNote.slnx                 Solution-Format des .NET-10-SDK
├─ Directory.Build.props         gemeinsame Eigenschaften + Version
├─ Directory.Packages.props      zentrale NuGet-Versionen
├─ src/                          Core · ViewModels · Legacy · Ocr · Avalonia · Wpf
├─ tests/                        Core.Tests (läuft auch unter Linux) · Wpf.Tests (nur Windows)
├─ tools/                        Werkzeuge, KEIN Produktivcode, nicht in der Solution
├─ packaging/                    flatpak/ · appimage/
├─ site/                         GitHub Pages (DE + EN)
├─ Assets/  tessdata/            Schriften, Wörterbücher, Cover, Symbole
├─ Docs/                         diese Spec · HANDOFF.md · Anleitungen (DE/EN) · Design-Konzepte
└─ LICENSE · README(.en).md · THIRD-PARTY-NOTICES.md · CONTRIBUTING.md
```

---

## Roadmap

> **Hier steht die Reihenfolge und das Ziel jeder Phase — nicht ihr Fortschritt.**
> Die Häkchen je Runde führt `Docs/HANDOFF.md` §6, der Stand steht dort in §2.
> *Ein Plan an drei Stellen ist einer, der an zweien veraltet.*

**Die Grundregel:** aufräumen, prüfen, veröffentlichen. **Wer nach dem Aufräumen nicht
mehr prüft, veröffentlicht einen Stand, den nie jemand gesehen hat.**

| Phase | Inhalt | Trägt |
|---|---|---|
| **0** | Umzug in die `src/`-Struktur, geklont statt kopiert (`git log --follow` überlebt) | — |
| **1** | Netz einziehen: Tests, Golden-Files, CI mit zwei Läufen | — |
| **2** | Die große Entkopplung: Platform-Naht, eigene ViewModels, LiteDB → SQLite | **M0** — Core baut auf Linux |
| **3** | Avalonia-Shell für Linux | **M1** — Notizbuch und Whiteboard laufen unter Linux |
| **4** | Eigene Dokument-Engine in `Core/Text/`, samt Umverdrahten der Exportwege | — |
| **4.5** | Die fehlenden Werkzeuge des Linux-Kopfs (Formen, Zeichenhilfen, Textfelder, Sticker, Import, Zahlenblock, Drehen/Skalieren) | **M2** — Funktionsgleichheit Linux ↔ Windows |
| **5** | ① UI/UX angleichen → ② Rückmeldung → ③ Flatpak/AppImage → ④ aufräumen und vollständig prüfen → ⑤ veröffentlichen | **M3** — veröffentlicht, Version 1.0.0 |
| **5.1** | Rechtschreibprüfung im Linux-Kopf — das benannte Loch von M2 | schließt M2 |
| **5.2** | Grammatikprüfung im Linux-Kopf: feste Regeln in Core, LanguageTool darüber | — |
| **6** | iPadOS-Kopf: Apple Pencil, PDFKit/Vision, AOT-Härtung, App Store | **M4** — TestFlight-Build |

**Vorgemerkt, ohne eigene Phase:** der Flathub-Quellbau (siehe *Entscheidungen — Offen*).
Er hält nichts auf und darf warten, bis er sauber gemacht werden kann.

### Was die Reihenfolge bestimmt

- **Linux vor iPad.** Der Linux-Port ist das Ziel des Projekts, der iPad-Kopf ist ein
  Zusatz. Eine fertige Linux-Fassung bleibt nicht liegen, bis ein zweiter Port nachkommt
  (2026-08-18, Nutzer — vorher war Phase 5 iPadOS).
- **Werkzeuge vor dem dritten Kopf.** Wer die fehlenden Werkzeuge erst nach dem iPad
  baut, baut sie **zweimal**. Deshalb steht 4.5 vor 6 — und das Argument wird stärker,
  je weiter 6 nach hinten rückt.
- **Ein Meilenstein wird ausgerufen, nicht gerechnet.** M2 kam mit einem *benannten*
  Loch heraus: was fehlt, wird benannt und nicht verschwiegen.
- **Veröffentlichen setzt M2 voraus.** Was hinausgeht, soll auf Linux können, was es auf
  Windows kann.

---

## Entscheidungen — Fixiert

| Bereich | Entscheidung |
|---|---|
| Ziel-Plattformen | Linux zuerst, Windows bleibt, iPadOS danach (HANDOFF §1) |
| Reihenfolge | Veröffentlichung **vor** iPadOS (2026-08-18, Nutzer) |
| Laufzeit | `net10.0` (LTS), `LangVersion 14`, Nullable + ImplicitUsings an |
| Architektur | Core ohne UI; Köpfe erreichen das System nur über `Core/Platform/` |
| AOT | NativeAOT ist Pflicht für iPad → kein `Reflection.Emit`, Json per Source-Generator |
| Persistenz | SQLite + Blobs daneben; LiteDB nur in `GonkNote.Legacy` zum einmaligen Übertragen |
| Texteditor | Eigene Dokument-Engine, kein Feature-Verlust |
| Führendes Feld | `Rtf` führt weiterhin im WPF-Kopf, bis dessen Editor auf das Modell umgestellt ist |
| Rendering | SkiaSharp in beiden Köpfen, ein Renderer |
| Theming | 20 benannte Farben in Core; eigene Designs als JSON, unvollständige Dateien erben |
| Rechtschreibung | Hunspell in reinem C#, de_DE + en_US mitgeliefert |
| Grammatik | Feste Regeln in Core (immer), LanguageTool darüber — **nur Loopback** |
| OCR | Tesseract, ein Projekt für beide Köpfe |
| Verlaufsstapel | `UndoStack` und `TdUndo` bleiben **getrennt** (2026-09-04, Nutzer) |
| Zweite Stift-Taste | Darf sich zwischen den Köpfen unterscheiden — **kein** Loch in M2 |
| Fehlende Werkzeuge | Fehlt dem Linux-Kopf etwas, wird es **dort gebaut**, nicht dem Windows-Kopf weggenommen |
| Lizenz | MIT; Herkunft jeder fremden Zutat in `THIRD-PARTY-NOTICES.md` |
| Repo-Historie | **Kein** History-Rewrite; `Docs/HANDOFF.md` bleibt lesbar im Baum |
| Version | Eine Anhebung zieht **sechs weitere Stellen** nach — die Liste führt `Directory.Build.props` |
| Vertrieb | Windows-Zip + Linux-AppImage + Flatpak, alle ohne Adminrechte |

---

## Entscheidungen — Offen

- **PDF-Größe bei kurzen Dateien.** Skia bettet die ganze TTF ein, nicht einen
  Auszug: ~200 KB je benutzter Familie, einmalig — bei 35 Seiten sind das ~1,5 KB je
  Seite, bei einer Seite der ganze Betrag. **Empfehlung: so lassen**, ein eigener
  Glyphen-Auszug lohnt erst, wenn jemand darüber stolpert.
- **Flathub.** Das Manifest packt heute ein fertiges `dotnet publish` ein; Flathub
  baut **aus dem Quellcode und ohne Netz**. Der Umbau (SDK-Erweiterung,
  `nuget-sources.json`, Quelle auf den Tag gepinnt) ist beschrieben, aber nicht
  gemacht — er hält nichts auf, AppImage und Zip decken beide Plattformen ab.
- **Zusammengesetzte Zeichen unter Linux** (`´` + `e` → `é`) — ein Fehler im
  Fenster-Baustein von Avalonia, dort gemeldet. Umgehung offen.
- **Achtes Diagramm (Fläche)** — vorerst nein; es wäre ein Modell- und kein
  Zeichenschritt.
- **iPadOS-Kopf** (Phase 6): Apple Pencil, PDFKit/Vision statt Tesseract,
  AOT-Härtung, App Store. Vollständig Teil des Projekts und desselben Repos.
- **Aus V1 mitgeschleppt:** OCR-Endfluss, Formen-Stift und Touch haben nie
  Praxis-Rückmeldung auf echter Hardware bekommen; Cover-Kennzeichen, Verpixelung ab
  Seite 12, Geodreieck auf dunkler Seite, gedrehte Elemente, Wasserzeichen im
  DOCX-Export.

---

## Wie mit dieser Datei gearbeitet wird

1. **Ein neues Feature beginnt hier.** Erst der Eintrag unter *Scope → In* (oder eine
   fixierte Entscheidung), dann der Code. Passt es in keinen Abschnitt, ist es
   entweder kein Feature oder die Spec ist überholt — beides ist eine Rückfrage wert.
2. **Was hier nicht steht, wird nicht gebaut.** Ein Wunsch ohne Zeile in dieser Datei
   ist eine offene Entscheidung, kein Auftrag.
3. **Was hier unter *Out* steht, bleibt draußen** — bis jemand es bewusst
   herüberschiebt und begründet.
4. **Diese Datei beschreibt keinen Stand.** Keine Versionsnummern im Fließtext, keine
   Testzahlen, keine Häkchen. *Was viermal dasteht, wird dreimal nicht gepflegt.*
5. **Arbeitsprotokoll, Fallen und Chronik bleiben im HANDOFF.** Diese Spec verweist,
   sie wiederholt nicht.
