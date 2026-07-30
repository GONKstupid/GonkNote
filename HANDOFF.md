# Gonk Note V2 — Projektübergabe

**Stand: 2026-07-30 · Version 0.2.0 · net10.0 · SkiaSharp 3 · Roadmap Phase 1 abgeschlossen**

> **📌 Dauerregeln des Nutzers — gelten immer, ohne Nachfragen:**
>
> 1. **Doku-Pflege — vier mitgelieferte Dokumente, immer paarweise:**
>
>    | Dokument | Gegenstück | wird angezeigt unter |
>    |---|---|---|
>    | `README.md` (DE) | `README.en.md` (EN) | **Hilfe → Über Gonk Note** |
>    | `ERSTE-SCHRITTE.md` (DE) | `GETTING-STARTED.md` (EN) | **Hilfe → Erste Schritte** |
>
>    - **Nie nur eine Sprache ändern.** Beide Fassungen müssen inhaltlich auf demselben
>      Stand sein. `EmbeddedDocs` wählt nach `Loc.Current` und fällt sonst auf Deutsch
>      zurück — eine halbfertige Übersetzung fällt also nicht auf, sie zeigt einfach still
>      den alten oder den deutschen Text.
>    - **Nach jeder Änderung den zugehörigen Dialog in der App gegenprüfen** (beide
>      Sprachen, Ansicht → Sprache). Alle vier liegen als eingebettete Resource in der Exe,
>      Textänderungen erscheinen also ohne Code-Änderung — genau deshalb merkt man einen
>      kaputten Umbruch, einen toten Verweis oder eine fehlende Übersetzung **nur im
>      laufenden Programm**.
>    - Von Hand zu pflegen ist nur die Versions-/Phasenzeile in `AboutDialog.xaml.cs`
>      (`VersionText`).
>    - Verweise zwischen den Dokumenten laufen über `EmbeddedDocs.GuideLinkDe/-En`
>      (`ERSTE-SCHRITTE.md` / `GETTING-STARTED.md`) — wer eine Datei umbenennt, muss dort
>      nachziehen.
> 2. **Antwort-Stil:** Erledigtes **sehr kurz und stichpunktartig** melden. **Ausführlich nur
>    bei offenen Fragen und Entscheidungen**, die der Nutzer treffen muss — die dafür klar
>    begründen.
> 3. **Sprache:** durchgehend Deutsch — UI, Kommentare, Commits, diese Datei.
> 4. **Kopie der echten Daten anlegen ist erlaubt, ohne zu fragen** (Nutzer-Entscheidung
>    2026-07-30). Wenn echte Daten zum Prüfen gebraucht werden — Migration, Export, ein
>    Fehlerbild, das nur mit Bestandsdokumenten auftritt —, darf
>    `%APPDATA%\GonkNote\gonknote.db` **plus** `%APPDATA%\GonkNote\gonknote.blobs` nach
>    `%TEMP%` kopiert und die **Kopie** geöffnet werden (Befehle in §8).
>
>    **Die Grenze bleibt:** die Datenbank unter `%APPDATA%` wird nie geöffnet, nie beschrieben,
>    nie umbenannt und nie gelöscht — nur gelesen, um sie zu kopieren. Gearbeitet wird
>    ausschließlich auf der Kopie, gestartet ausschließlich mit `--db <Kopie>`.
>
>    **Beide Teile kopieren, immer.** Der Blob-Ordner leitet seinen Namen von der
>    Datenbankdatei ab; ohne ihn sind alle Bilder scheinbar weg, und man sucht den Fehler an
>    der falschen Stelle. **Aus der Kopie nichts ins Repo übernehmen** — dort stehen
>    Schulunterlagen (Kopfzeile).
>
> **⚠️ Diese Datei liegt im Repo — solange es privat ist.** Nutzer-Entscheidung vom
> 2026-07-29: eingecheckt, damit sie zwischen Rechnern (Windows ↔ CachyOS-Laptop)
> mitwandert. **Bevor das Repo öffentlich geschaltet wird, muss sie wieder raus** — der
> Eintrag steht als erster Punkt in §6 „Vor dem Öffentlich-Schalten".
>
> **Dabei wissen:** Ein `git rm` entfernt sie nur aus dem *aktuellen* Stand, nicht aus der
> **History** — jeder frühere Stand bliebe lesbar. Wer das vermeiden will, braucht einen
> History-Rewrite vor dem Umschalten. In V1 ist genau das passiert und wurde damals
> bewusst hingenommen (V1-Handoff, Kopfzeile). Deshalb hier die Regel: **nichts in diese
> Datei schreiben, was nicht irgendwann öffentlich stehen darf** — keine Pfade zu privaten
> Daten, keine Zugangsdaten, keine Inhalte aus den Schulunterlagen.

---

## 0. Schnelleinstieg für einen neuen Thread

**Das Projekt:** Offline-Notiz-App (Notizbuch, Whiteboard, Textdokument), Stylus-first, keine
Cloud. Läuft heute als WPF-App unter Windows 11. **V2 ist die Portierung nach Linux (Avalonia)
und iPadOS** — Greenfield-Solution, in die der wiederverwendbare Code aus V1 wandert.

**Wo:**

| | |
|---|---|
| **V2 (hier gearbeitet)** | `C:\Dev\Zed\gonk-note-V2`, Branch `main` → <https://github.com/GONKstupid/GonkNote> (**privat**), Remote über **SSH** (§5c) |
| **V2 auf dem Linux-Laptop** | `~/Zed/gonk-note-V2/GonkNote` (CachyOS) — nur für den Stylus-Prototyp und den Core-Build, siehe §5a/§5b |
| **V1 (Referenz, nicht anfassen)** | `C:\Dev\Zed\gonk-note`, Branch `main`, <https://github.com/GONKstupid/gonk-note> |
| **Roadmap (die Vorgabe)** | `C:\Users\manue\Desktop\gonk-note-port-RM.MD` |
| **V1-Handoff (alle Alt-Erfahrungen)** | `C:\Dev\Zed\gonk-note\HANDOFF.md` — **weiterhin gültig**, §4 Fallen und §7 Testen dort lesen |

**In dieser Reihenfolge vorgehen:**

1. **Wünsche und Fehlermeldungen des Nutzers zuerst.** Vorrang vor allem anderen.
2. Sonst: **§5 Entscheidungen** — was dort offen steht, nachfragen statt raten.
3. Sonst: den nächsten Punkt aus **§6 Arbeitsplan** nehmen.
4. Vor jeder Code-Änderung **§7 Fallen** überfliegen.

**Bauen und prüfen:**

```powershell
cd C:\Dev\Zed\gonk-note-V2
dotnet build -c Release          # muss 0 Fehler, 0 Warnungen ergeben
```

Auf dem **Linux-Laptop** stattdessen nur das Core-Projekt (die Solution enthält den
WPF-Kopf und ist dort nicht baubar — das ist so gewollt):

```bash
cd ~/Zed/gonk-note-V2/GonkNote
dotnet build src/GonkNote.Core   # Meilenstein M0
```

Für alles Linux-Spezifische — SSH-Entsperren, sudo-Skill, Stand der Werkzeuge — **§5b lesen**.

**Was zuletzt lief:** Phase 0 komplett (Umzug in die `src/`-Struktur, zentrale
Paketverwaltung, Lokalisierung nach Core), Anhebung auf **net10.0**, Umstieg auf
**SkiaSharp 3.119.4 / Svg.Skia 5.1.1** (dabei einen Absturz gefunden und behoben, §7).
Alles am laufenden Programm geprüft — mit einer **Kopie** der echten Datenbank.

Danach der **Stylus-Prototyp (§5a) auf dem Linux-Laptop: Druck kommt in Avalonia an**, damit ist
das größte Risiko vor Phase 3 ausgeräumt. Meilenstein **M0 ist erreicht** —
`dotnet build src/GonkNote.Core` läuft unter Linux durch.

Zuletzt **Phase 1 komplett** (§4.6): zwei Testprojekte mit 78 Tests, Renderer-Snapshots,
Export-Golden-Files und CI auf Windows **und** Ubuntu. Dabei einen zweiten Absturz aus dem
SkiaSharp-3-Umstieg gefunden und behoben (`SKBitmap.Decode`, §7). **Als Nächstes: Phase 2**
(§6) — und vorher die zwei offenen Fragen aus §5 klären.

**Tests laufen lassen:**

```powershell
dotnet test -c Release        # Windows: beide Projekte, 78 Tests
```

---

## 1. Auftrag und die Entscheidungen dahinter

Aus dem Vorgespräch zur Roadmap, nicht ohne Rückfrage ändern:

- **Ziel-Plattformen:** Linux zuerst, iPadOS danach. Windows/WPF bleibt bestehen.
- **Vertrieb iPad:** App Store / TestFlight → **NativeAOT ist Pflicht**. Das prägt die
  Architektur (kein `Reflection.Emit`, kein LiteDB, Json über Source-Generator).
- **Texteditor:** **eigene Dokument-Engine**, kein Feature-Verlust. Kein Rückzug auf einen
  einfachen Editor.
- **Vorgehen:** gemeinsame Basis zuerst, dann Linux, dann iPad.
- **Die App soll mit jedem Stylus funktionieren** (Nutzer, 2026-07-29) — nicht nur mit dem
  Gerät, auf dem gerade getestet wird. Der Eingabepfad wird gegen die *Fähigkeiten* des
  Geräts geschrieben; fehlt Druck oder Neigung, muss der Strich trotzdem sauber aussehen.
- **V1 bleibt unangetastet stehen** — Referenz und Notausgang bis Meilenstein M1.
  Ab jetzt **keine neuen Features in V1**; Bugfixes dort sofort nach V2 cherry-picken.

Die Anforderungen aus V1 (offline, Single-File, Stylus-first, drei Dokumenttypen,
Import/Export, Dark/Light bei hellem Papier, RAM-Ziel 800 MB / Grenze 1 GB, so open source wie
möglich) gelten unverändert weiter — siehe `gonk-note\HANDOFF.md` §1.

---

## 2. Stand

Vier Commits auf `main`, alle nach <https://github.com/GONKstupid/GonkNote> gepusht:

| | |
|---|---|
| `ee64646` | Phase 0: Umzug in die `src/`-Struktur |
| `610537e` | Platzhalter für das Testprojekt |
| `50f33f2` | Ziel-Framework auf `net10.0` |
| `e9bf400` | SkiaSharp 3.119.4, Svg.Skia 5.1.1 |

Dazu die Commits aus Phase 1 (2026-07-30) — Testprojekte, CI und der `SKBitmap.Decode`-Fix.

**Erledigt in Phase 1:** siehe §4.6 (was das Netz prüft) und §6 (Häkchen). Kurz:
`tests/GonkNote.Core.Tests` (70 Tests, Windows **und** Linux) und `tests/GonkNote.Wpf.Tests`
(8 Tests, Export-Fixtures), CI mit zwei Läufen, ein gefundener und behobener Absturz (§7).

**Erledigt in Phase 0:**

- **Geklont statt kopiert.** `git clone` aus V1 → die 122 Commits sind mitgekommen, `origin`
  auf das eigene V2-Repo umgestellt.
- **Alles per `git mv` verschoben** → `git log --follow` funktioniert über die
  Umstrukturierung hinweg, `git blame` überlebt.
- **Neue Struktur** (§3) steht, `GonkNote.slnx` angelegt.
- **`Directory.Build.props`** (gemeinsame Eigenschaften) und **`Directory.Packages.props`**
  (zentrale Paketversionen, `ManagePackageVersionsCentrally`) angelegt; beide `.csproj`
  nennen nur noch Paketnamen.
- **Lokalisierung nach Core gezogen** — `Loc`, `LocGerman`, `LocEnglish`. Die
  Markup-Erweiterung `TExtension` (`{loc:T …}`) war der einzige WPF-Teil und liegt jetzt
  einzeln in `src/GonkNote.Wpf/Services/Localization/TExtension.cs`. Der Namensraum bleibt
  bewusst `GonkNote.Services`, damit `xmlns:loc="clr-namespace:GonkNote.Services"` in **allen**
  XAML-Dateien unverändert weiterläuft.
- **Assets, tessdata und die Markdown-Dokumente bleiben in der Wurzel**; die WPF-`.csproj`
  verweist mit `Link=` darauf, damit Ausgabepfade (`Assets\…`, `tessdata\…`) und
  Resource-Namen (`pack://application:,,,/README.md`) sich **nicht** ändern.

**Checkliste Roadmap §0.5:**

| Prüfpunkt | Ergebnis |
|---|---|
| `dotnet build` grün | ✅ Debug **und** Release: 0 Fehler, 0 Warnungen |
| WPF-App startet aus V2, öffnet die echte DB | ✅ mit einer **Kopie** der echten `gonknote.db` + Blobs in `%TEMP%` geprüft: Ordnerbaum, angepinnte Ordner, Galerie, Dark-Theme, englische Sprache, Textdokument mit Ribbon/Lineal/Wortzähler/Rechtschreibung. Die echte DB wurde **nicht** angefasst (Regel aus V1 §7) |
| `git log --follow` zeigt die alte Historie | ✅ `WbRenderer.cs` und `LocGerman.cs` je 6 Commits über den Umzug hinweg |
| Core/ViewModels referenzieren nichts aus Wpf | ⚠️ Core: ✅ (keine ProjectReference). **ViewModels: noch nicht** — siehe §4.2 |
| `C:\Dev\Zed\gonk-note` unverändert | ✅ nur gelesen |

---

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
│  │  ├─ Rendering/              WbRenderer (Skia), WbAidRenderer (Geodreieck), WbImages (§7)
│  │  ├─ Services/               DatabaseService, BlobStore, ImageCache, UndoStack, PdfImporter
│  │  ├─ Editing/                WbErase — punktgenaues Radieren
│  │  └─ Localization/           Loc + LocGerman + LocEnglish        ← neu in Phase 0
│  │
│  ├─ GonkNote.ViewModels/       eigener Ordner, noch KEIN eigenes Projekt (§4.2)
│  │                             MainViewModel, DocumentTabViewModel, TreeItem…, Gallery…, Mvvm
│  │
│  └─ GonkNote.Wpf/              net10.0-windows10.0.19041.0 · Windows-Kopf (AssemblyName bleibt GonkNote)
│     ├─ App.xaml(.cs), MainWindow.xaml(.cs)
│     ├─ Views/                  Whiteboard (Partials), TextEditor (Partials), Dialoge
│     ├─ Themes/                 Light/Dark/Styles
│     └─ Services/               alles mit WPF-Bezug (§4.1):
│                                Docx/Markdown-Im-/Export, MarkdownFlow, PdfExporter,
│                                TextStyles, DocumentImages, EmbeddedDocs, OcrService,
│                                SpellCheckSupport, ThemeService, TitleBarTheme,
│                                WindowBounds, Localization/TExtension.cs
│
├─ .github/workflows/ci.yml      zwei Läufe: windows (alles), ubuntu (nur Core) — §4.6
│
├─ tests/
│  ├─ GonkNote.Core.Tests/       net10.0 · läuft auch unter Linux · 70 Tests
│  │  └─ Snapshots/*.sha256      Pixelhashes des Renderers (Golden-Files)
│  └─ GonkNote.Wpf.Tests/        net10.0-windows · nur Windows · 8 Export-Fixtures
│     └─ Fixtures/               referenz.md, referenz-docx.txt (Golden-Files)
│
├─ tools/                        Werkzeuge, KEIN Produktivcode, nicht in der Solution
│  └─ stylus-prototyp/           Messwerkzeug zu §5a — Ergebnis liegt dort vor
│     ├─ GonkNote.StylusProbe/   Avalonia-Prototyp (Druck als Kreisradius)
│     ├─ evdev_beschreiben.py    Achsenbereiche des Digitizers (EVIOCGABS)
│     ├─ evdev_druck.py          Druckverlauf mitschneiden und auswerten
│     └─ messungen/              Rohberichte der Läufe
│
├─ Assets/  tessdata/  Docs/     1:1 aus V1, in der Wurzel
└─ LICENSE, README(.en).md, ERSTE-SCHRITTE.md, GETTING-STARTED.md, THIRD-PARTY-NOTICES.md
```

**`tools/` steht bewusst neben `src/`, nicht darin**, und ist **nicht** in `GonkNote.slnx`
eingetragen: was dort liegt, ist Wegwerf-Werkzeug und soll nicht mit den Produktivprojekten
verwechselt oder versehentlich mitgebaut werden. `GonkNote.StylusProbe` ist zusätzlich aus der
zentralen Paketverwaltung ausgeklinkt (`ManagePackageVersionsCentrally=false`), damit seine
Avalonia-Version nicht die Versionswahl für Phase 3 vorwegnimmt.

**Noch nicht angelegt:** `src/GonkNote.Avalonia` (Phase 3) und `src/GonkNote.iOS` (Phase 5).
Bewusst — leere Projekte, die nicht bauen, sind nur Ballast; sie entstehen, wenn ihre Phase
beginnt.

**Faustregel:** Nach Phase 2 darf in den Plattform-Köpfen *nur noch* stehen, was Pixel
zeichnet oder Eingaben entgegennimmt. Alles andere gehört in Core.

---

## 4. Abweichungen von der Roadmap — und warum

Die Roadmap §0.2 nimmt an, mehrere Dateien seien plattformneutral. Der von ihr selbst
vorgegebene Test (`grep` nach `System.Windows`) sagt etwas anderes. Ich bin dem **Grep**
gefolgt, weil sonst Phase 0 nicht baubar wäre — die Roadmap nennt ihn ausdrücklich als
Faustregel für Zweifelsfälle.

### 4.1 Import/Export bleiben vorerst im WPF-Kopf

`DocxImporter`, `DocxExporter`, `MarkdownImporter`, `MarkdownExporter`, `TextStyles`,
`DocumentImages`, `PdfExporter` (~2.300 Zeilen) sollten laut Roadmap „1:1" nach
`Core/Import` bzw. `Core/Export`. Sie stehen aber **alle auf `FlowDocument`**
(`System.Windows.Documents`) — genau das, was die Roadmap in Phase 4 ersetzen will.

Sie können deshalb erst umziehen, **nachdem** die eigene Dokument-Engine steht: Der Umzug
*ist* der Umbau, nicht die Vorbereitung darauf. Bis dahin liegen sie in
`src/GonkNote.Wpf/Services/`.

Der OpenXml-Code selbst (~1.237 Zeilen) bleibt dabei größtenteils erhalten, wie geplant —
er tauscht nur seine Gegenseite.

### 4.2 `GonkNote.ViewModels` ist noch keine eigene Assembly

Der Ordner liegt an seinem endgültigen Platz, aber die Dateien werden noch vom WPF-Projekt
mitkompiliert (`<Compile Include="..\GonkNote.ViewModels\**\*.cs" LinkBase="ViewModels" />`).

**Grund:** `MainViewModel` ruft `DocxImporter`, `DocxExporter`, `MarkdownExporter`,
`PdfExporter`, `DocumentImages`, `TextStyles` und `ThemeService` auf — alles Klassen, die nach
§4.1 im WPF-Kopf liegen. Ein eigenes Projekt wäre heute ein **Ringschluss**
(ViewModels → Wpf → ViewModels). Die Auftrennung ist genau der erste Schritt von Phase 2
(„von WPF freischneiden"): erst Interfaces in `Core/Platform/` einziehen, dann die
`.csproj` anlegen — der Compiler zeigt dann jede verbliebene Abhängigkeit von selbst.

**Wenn das Projekt entsteht, nicht vergessen:** In `MainWindow.xaml` steht
`xmlns:vm="clr-namespace:GonkNote.ViewModels"` — ohne `;assembly=GonkNote.ViewModels` bricht
das XAML in dem Moment, in dem die Typen in eine andere Assembly wandern.

### 4.3 Ziel-Framework `net10.0` statt `net9.0`

**Entschieden am 2026-07-28.** Die Roadmap sieht `net9.0`/`LangVersion 13` vor — das ist
eine **STS**-Version, deren Support im Mai 2026 ausgelaufen ist. `net10.0` ist **LTS**
(bis November 2028) und deckt damit den kompletten 9–12-Monats-Zeitrahmen der Portierung ab.
Auf diesem Rechner ist ohnehin keine 9er-Runtime installiert (nur 8.0 und 10.0).

Gesetzt: `LangVersion 14`, `GonkNote.Core` → `net10.0`, `GonkNote.Wpf` → `net10.0-windows10.0.19041.0`.
Build Debug und Release grün, Start und Textdokument mit DB-Kopie geprüft.

**Für die späteren Köpfe:** `net10.0-ios` bringt das hier installierte SDK mit. Für Avalonia
ist vor Phase 3 zu prüfen, welche Avalonia-Fassung `net10.0` unterstützt — die Roadmap nennt
Avalonia 12; falls das noch nicht trägt, ist Avalonia 11.x auf `net10.0` der Rückfallweg.

**Achtung:** Der Ausgabepfad hat sich mit geändert
(`bin\Release\net10.0-windows10.0.19041.0\win-x64\`). Die alten `net8.0`-Ordner sind gelöscht, damit
niemand versehentlich die veraltete Exe startet.

### 4.4 SkiaSharp 3.119.4 · Svg.Skia 5.1.1 — erledigt

Roadmap §4. Umgesetzt am 2026-07-29 (Nutzer-Entscheidung, vorgezogen vor Phase 1).
Svg.Skia 5.1.1 ist die erste Fassung, die auf SkiaSharp ≥ 3.119.2 aufsetzt.

**Was sich in der API geändert hat — beim Weiterbauen wissen:**

| bis 2.88 | ab 3.x |
|---|---|
| `SKPaint.TextSize` / `.Typeface` / `.TextAlign` | `SKFont` — neu: `WbFonts.Font(name, size)`; Ausrichtung ist Argument von `DrawText` |
| `paint.MeasureText(s)` | `font.MeasureText(s)` — `WbRenderer.WrapText` nimmt jetzt ein `SKFont` |
| `SKPaint.FilterQuality` | `SKSamplingOptions` beim Zeichnen — eine Wahrheit dafür: `WbRenderer.MediumSampling` / `.HighSampling` |
| `DrawImageNinePatch(img, c, dst)` | Filtermodus muss ausdrücklich dabeistehen, sonst ist der Aufruf zweideutig |
| `SKColorFilter.CreateTable(a, null, null, null)` | **wirft** `ArgumentNullException` — siehe §7 |

**Geprüft** am laufenden Programm mit einer Kopie der echten Datenbank: Notizbuch mit
Bild-Cover, Gitterseite, Striche in zwei Farben, Lineal, **Geodreieck** (der SVG-Weg über
Svg.Skia 5), Farbverlauf-Cover mit Titeltext, **PDF-Export** und dessen Rückimport über
`PdfImporter`.

**Die vier offenen Stellen von damals — Stand nach Phase 1:**

| Stelle | Stand |
|---|---|
| DOCX-/Markdown-Export, Text-Editor-PDF-Weg | ✅ Golden-Files in `GonkNote.Wpf.Tests` (§4.6) |
| Bleistift mit Graphit-Körnung | ✅ Pixelhash `bleistift-koernung` |
| Bildimport-Verkleinerung (`WhiteboardView.Import`) | ⚠️ **weiterhin ohne Test** — private Methode im WPF-Kopf. Der `SKBitmap.Decode`-Fix (§7) hat sie angefasst |
| OCR-Vorverarbeitung (`OcrService`) | ⚠️ **weiterhin ohne Test** — braucht die Tesseract-Nativbibliotheken; ebenfalls vom Decode-Fix betroffen |

Die beiden offenen sind in Phase 2 fällig: dort entstehen `IOcrEngine` und der Bildimport-Weg
wird für Avalonia herausgelöst — an öffentlichen Schnittstellen lassen sie sich dann testen,
ohne den WPF-Kopf hochzufahren.

### 4.5 Versionszeile im Über-Dialog — erledigt

Bis 0.2.0 stand in `AboutDialog.xaml.cs` fest verdrahtet `$"Version {…} – Phase 3"`. Zwei
Probleme in einer Zeile:

- **Zweideutig:** „Phase 3" war die *Entwicklungsphase von V1*, nicht die Portierungsphase.
  Neben der laufenden Portierung las es sich wie deren Phase 3.
- **Nicht übersetzt:** die Zeile stand im Code und erschien deshalb auch im **englischen**
  Dialog deutsch. Aufgefallen ist das erst, weil `Loc` sonst überall benutzt wird — genau die
  Art Lücke, die Dauerregel 1 meint.

Behoben am 2026-07-30 (Nutzer-Entscheidung): neuer Schlüssel **`About.Version`** in
`LocGerman`/`LocEnglish`, `{0}` ist die Versionsnummer.

| | |
|---|---|
| Deutsch | `Version 0.2.0 · Portierung, Phase 2` |
| Englisch | `Version 0.2.0 · Port, phase 2` |

**Von Hand nachzuziehen, wenn eine Portierungsphase beginnt** — und zwar in **beiden**
Tabellen (Dauerregel 1). Der Dialog wird bei jedem Öffnen neu erzeugt, `Loc.LanguageChanged`
braucht er deshalb nicht.

Am laufenden Programm in beiden Sprachen gegengeprüft (Dauerregel 1), mit einer
Wegwerf-Datenbank: Zeile passt in eine Zeile, kein Umbruch, und der ganze Dialog wechselt mit
(englisches README, englische Menüleiste).

---

### 4.6 Phase 1 — was das Netz prüft, und was ausdrücklich nicht

Umgesetzt am 2026-07-30. **78 Tests**, aufgeteilt auf zwei Projekte:

| Projekt | Ziel | Läuft |
|---|---|---|
| `tests/GonkNote.Core.Tests` | 70 Tests, nur `GonkNote.Core`, `net10.0` | Windows **und** Linux |
| `tests/GonkNote.Wpf.Tests` | 8 Tests, Export-Fixtures am `FlowDocument` | nur Windows |

Getestet wird deutsch benannt (Dauerregel 3): `Notizbuch_kommt_unveraendert_zurueck`.

**Beide Testprojekte laufen seriell** (`DisableTestParallelization`). Der Kern hat drei
prozessweit statische Zustände, die parallele Testklassen einander umstellen würden:
`BlobStore.Current`, `ImageCache.Source` (beide setzt der `DatabaseService`-Konstruktor) und
den Bild-Cache in `ImageCache` selbst. Ein grüner Lauf wäre sonst Glück.

**Golden-Files ändern** — für beide Mechanismen gilt: ein fehlender Golden-File ist ein
Fehlschlag, keine stille Zustimmung. Neu setzen nur ausdrücklich:

```powershell
$env:GONK_SNAPSHOT_UPDATE=1   # Renderer-Pixelhashes (Core.Tests\Snapshots\*.sha256)
$env:GONK_GOLDEN_UPDATE=1     # Export-Aufrisse (Wpf.Tests\Fixtures\*)
dotnet test -c Debug
```

Bei Abweichung legt der Test das **tatsächlich Erzeugte** in den Ausgabeordner
(`Snapshots\ist\*.png` bzw. `Fixtures\ist\*`) und nennt den Pfad in der Fehlermeldung — ein
Hash allein sagt niemandem, *was* sich geändert hat. Die CI lädt diese Dateien bei einem
Fehlschlag als Artefakt hoch.

**Bewusst nicht gehasht: alles mit Schrift.** `WbFonts` fragt „Segoe UI" ab und fällt auf
`SKTypeface.Default` zurück; unter Linux ist das eine andere Schrift. Ein Pixelhash über
gezeichneten Text würde die Schriftausstattung des Rechners prüfen, nicht den Renderer — und
wäre auf dem Ubuntu-Läufer dauerhaft rot. Text hat stattdessen schriftunabhängige Tests
(`SchriftTests`, Hilfsklasse `Farbfleck`): kommt überhaupt Farbe an, bleibt sie in der
gemeldeten Umschließung, hält der Umbruch die Breite ein, schneidet der Zettel am Kartenrand ab.

**Dasselbe beim PDF eines Textdokuments.** Die Seiten entstehen über den WPF-Paginator mit
den Systemschriften; ein Schriftartenupdate verschiebt jeden Pixel. Geprüft wird deshalb, was
ein Layout-Umbau wirklich kaputt macht und was für jede Schrift gilt: Seitenzahl,
Hoch-/Querformat, **keine leere Seite**, und dass die Kopfzeile auf Seite 1 fehlt und auf
Seite 2 steht.

**Der Whiteboard-PDF-Export wird über den Rückweg geprüft:** erzeugen, dann mit
`PdfImporter` (PDFium) wieder einlesen und die Seiten vermessen. Das ist der Harness, den §7
für Phase 1 vorgesehen hatte — jetzt als Test statt als Konsolenprogramm in `%TEMP%`.

**Überraschung: die Pixelhashes stimmen auf Linux und Windows überein.** Verifiziert im
Container (`mcr.microsoft.com/dotnet/sdk:10.0`), alle 70 Core-Tests grün. Deshalb steht je
Snapshot nur **ein** Hash. `Snapshot.cs` kennt vorsorglich einen Ausweg für den Fall, dass
sich das einmal unterscheidet (`<name>.linux.sha256`, bewusst anzulegen) — gebraucht wird er
heute nicht.

**Ebenfalls im Container verifiziert:** LiteDB im `Shared`-Modus läuft unter Linux (der
benannte Mutex trägt), und `dotnet build GonkNote.slnx` scheitert dort wie erwartet am
WPF-Kopf — die CI baut deshalb projektbezogen.

**Neu als Paket:** `SkiaSharp.NativeAssets.Linux`, nur im Core-Testprojekt. Das Paket
`SkiaSharp` bringt die native Bibliothek für Windows und macOS mit, für Linux **nicht** —
zum Bauen genügt das (M0), zum Ausführen nicht. Es steht bewusst **nicht** in
`GonkNote.Core`: eine Linux-`.so` gehört nicht in ein Paket, das der Windows-Kopf
mitschleppt. **Für Phase 3 ist das derselbe Punkt beim Avalonia-Kopf.** Dazu braucht das
System `fontconfig` und mindestens eine Schrift; die CI installiert `libfontconfig1` und
`fonts-dejavu-core`.

## 5. Entscheidungen

**Getroffen, alle umgesetzt:**

| Frage | Entscheidung |
|---|---|
| Ziel-Framework | **`net10.0`** statt `net9.0` — LTS bis Nov 2028 (§4.3) |
| SkiaSharp | **3.119.4 + Svg.Skia 5.1.1**, vorgezogen vor Phase 1 (§4.4) |
| Remote | **<https://github.com/GONKstupid/GonkNote>**, privat, vom Nutzer angelegt. Branch `main`, alles gepusht |
| Stift | Die App soll mit **jedem** Stylus laufen — nicht nur mit einem Modell (§1) |
| Linux-Rechner | Zweiter Laptop mit **CachyOS** steht bereit; Stift ist ein Lenovo Precision Pen 2 (§5a) |
| Über-Dialog-Text | **`About.Version` über `Loc`**, deutsch „Portierung, Phase 2" / englisch „Port, phase 2" (§4.5). Erledigt 2026-07-30 |
| Markdown-Export und Hyperlinks | **Ziel bleibt erhalten** (`[Text](URL)`), §7 „Markdown-Export". Erledigt 2026-07-30 |
| Kopie der echten Daten | **Ohne Nachfragen erlaubt**, die echte DB bleibt unangetastet — Dauerregel 4 in der Kopfzeile, Befehle in §8. Entschieden 2026-07-30 |

**Noch offen:**

1. **Wann auf den CachyOS-Laptop wechseln?** Siehe §5b — kurze Antwort: für den
   Stylus-Prototyp jetzt, für die eigentliche Arbeit erst zu Phase 3.
2. **Zweites Stylus-Gerät** (MPP und/oder EMR) — der einzige Punkt mit echtem Restrisiko,
   siehe §5a „Offen". Die Anforderung „läuft mit jedem Stylus" ist bis dahin unbeantwortet.

---

## 5a. Stylus-Prototyp — der wichtigste Test vor Phase 3

Die Roadmap stuft die Druckstärke unter Linux als **wackeligsten Punkt der ganzen
Portierung** ein: 6–8 Wochen Phase 3 stehen und fallen damit. Der Test hängt an nichts
anderem und sollte laufen, **bevor** die Avalonia-Shell gebaut wird.

**Aufbau:** leeres Avalonia-Projekt, `SKCanvasView`, `PointerPointProperties.Pressure` als
Kreisradius zeichnen. ~50 Zeilen. Ich kann das Projekt fertig vorbereiten — laufen lassen
muss es der Nutzer auf dem CachyOS-Laptop.

**Der Stift ist ein Lenovo Precision Pen 2 — und die App soll mit jedem Stylus laufen.**
Das ist eine Anforderung, keine Nebensache: der Eingabepfad wird deshalb gegen die
*Fähigkeiten* des Geräts geschrieben, nicht gegen ein Modell.

- Es ist ein **MPP-Stift** (Microsoft Pen Protocol), kein EMR-Digitizer wie beim Wacom, den
  die Roadmap annimmt. Unter Linux hängt er am HID-Digitizer-Pfad (`hid-multitouch`), nicht
  am `wacom`-Treiber. Ob Druck **und** Neigung als Achsen ankommen, ist geräteabhängig.
- **Genau deshalb muss der Prototyp beide Klassen abdecken**: MPP (Precision Pen 2) und, wenn
  greifbar, ein EMR-Gerät. Ein Test auf nur einem Stift beantwortet die Frage „läuft mit
  jedem Stylus" gerade nicht.
- **Fallback ist Pflicht, nicht Kür:** liefert ein Gerät keinen Druck, muss der Strich
  trotzdem sauber aussehen (feste Breite bzw. Breite aus der Geschwindigkeit). Das gilt für
  Neigung genauso. Windows/WPF macht das heute schon so — beim Avalonia-Pfad daran denken.
- **Vor dem Avalonia-Test zuerst die Kette darunter prüfen**, sonst misst man die falsche
  Schicht:
  1. `libinput list-devices` → taucht der Stift als *tablet tool* auf, mit `pressure`?
  2. `libinput debug-events` → ändern sich die Druckwerte beim Aufdrücken?
  3. Erst wenn 1 und 2 stimmen: Avalonia-Prototyp. Zeigt Avalonia dann keinen Druck, liegt es
     am Toolkit, nicht am Gerät — das wäre die eigentlich schlechte Nachricht.
- Unter **Wayland und X11/XWayland getrennt** messen. Der Unterschied ist die Antwort auf die
  Frage, ob Phase 3 ein Wayland-Problem bekommt.

**Ergebnis hier eintragen, wenn es vorliegt** — davon hängt ab, ob Phase 3 wie geplant
gebaut werden kann oder ob ein Umweg über einen eigenen Eingabepfad nötig wird.

---

### Ergebnis (29.07.2026, CachyOS-Laptop)

**Kurz: Druck kommt an. Phase 3 kann wie geplant gebaut werden.** Der Prototyp liegt in
`tools/stylus-prototyp/`, die Rohberichte unter `tools/stylus-prototyp/messungen/`.

#### Drei Annahmen aus dem Abschnitt oben waren falsch

Der Laptop ist **kein Lenovo, sondern ein HP** (`HPQ6001`, `HP WMI hotkeys`). Der Digitizer ist
ein **Wacom-AES-Gerät** (`Wacom HID 493A Pen`, VID `056a` PID `493a`, I²C `WCOM4900:00`) und
hängt am **`wacom`-Treiber**, nicht an `hid-multitouch`. Es wurde also **AES gemessen, nicht
MPP** — ein Lenovo Precision Pen 2 dürfte auf diesem Digitizer gar nicht erst schreiben.

Für die Anforderung „läuft mit jedem Stylus" heißt das: **sie ist weiterhin unbeantwortet.**
Getestet ist genau eine Geräteklasse, und es ist nicht einmal die, die hier angenommen wurde.
Der Fallback bleibt Pflicht, und ein zweites Gerät (MPP oder EMR) muss noch durch.

#### Schicht 1 — Kernel / evdev

| Achse | Bereich | Auflösung |
|---|---|---|
| `ABS_PRESSURE` | 0 – 4095 | **4096 Stufen** |
| `ABS_TILT_X` / `ABS_TILT_Y` | −90° – +90° | 57 Einheiten/° |
| `ABS_X` / `ABS_Y` | 0 – 30937 / 0 – 17402 | 100/mm → 309,4 × 174,0 mm |

Tasten: `BTN_TOOL_PEN`, `BTN_TOOL_RUBBER`, `BTN_TOUCH`, `BTN_STYLUS`, `BTN_STYLUS2` —
Radiergummi-Erkennung und zwei Stiftknöpfe sind also vorhanden. `PROP=INPUT_PROP_DIRECT`.

Live-Mitschnitt (30 s): 330 Druck-Samples, davon **275 verschiedene Werte**, genutzter Bereich
1500 – 3130. Kontinuierlicher Druck, kein Zweizustands-Schalter.

#### Schicht 2 — libinput

`libinput list-devices` meldet den Stift als **`Capabilities: tablet`**, Größe 309×174 mm,
Id `i2c:056a:493a`. Er taucht also als Tablet-Gerät auf, nicht als Maus oder Touchscreen.

`libinput debug-events`, 60 s Mitschnitt:

| Ereignis | Anzahl |
|---|---|
| `TABLET_TOOL_AXIS` | 6095 |
| `TABLET_TOOL_TIP` | 79 |
| `TABLET_TOOL_PROXIMITY` | 19 |
| `TABLET_TOOL_BUTTON` | 2 (`BTN_STYLUS` pressed/released) |

- **pressure:** 6193 Samples, davon 5532 > 0, Bereich **0,01 – 0,81**
- **tilt X:** 0,0° – 36,2° (139 verschiedene Werte) · **tilt Y:** −24,1° – 30,2° (210 Werte)

Druck und Neigung werden also durchgereicht, Stiftknopf und Proximity ebenfalls.

> Die Zahl *verschiedener* Druckwerte ist bei `debug-events` **nicht** aussagekräftig: das
> Werkzeug druckt nur zwei Nachkommastellen. Die echte Auflösung steht in Schicht 1 (4096
> Stufen) und Schicht 3 (bis 1489 unterscheidbare Werte pro Lauf).

Nebenbefund für §5b: der Befehl dort installiert die Tools **nicht**. `list-devices` und
`debug-events` stecken im Paket **`libinput-tools`**, nicht in `libinput`. Zusätzlich öffnet
libinput Device-Nodes **read-write** — eine reine Lese-ACL genügt nicht.

#### Schicht 3 — Avalonia (12.1.1)

Zwei Läufe, GNOME-Wayland-Sitzung, Backend `X11 / XWayland`:

| | Lauf 1 | Lauf 2 |
|---|---|---|
| Abtastungen / Striche | 2846 / 13 | 2772 / 11 |
| verschiedene Druckwerte | **1067** | **1489** |
| Druckbereich | **0,0019 – 1,0000** | 0,0023 – 0,9651 |
| Zeigertypen | Pen, Touch, Mouse | Pen |
| `XTilt` | — | **−22,1° … 41,2°**, 228 Werte |
| `YTilt` | — | **−32,2° … 29,9°**, 231 Werte |

`PointerPointProperties.Pressure` liefert den vollen Bereich 0…1 ungefiltert, `XTilt`/`YTilt`
kommen in Grad an. `Pointer.Type` unterscheidet Pen/Touch/Mouse sauber — das ist die
Voraussetzung dafür, dass Handballenabweisung in Phase 3 überhaupt baubar ist.

Wichtig für die Umsetzung: `GetIntermediatePoints()` benutzen, nicht nur `GetCurrentPoint()`.
Der Digitizer tastet schneller ab als die UI Frames zeichnet; ohne die Zwischenpunkte geht der
Großteil der Auflösung verloren.

#### Wayland vs. X11 — die Frage stellt sich nicht

**Avalonia 12.1.1 hat für Linux gar kein Wayland-Backend.** Im Build liegen nur
`Avalonia.X11.dll`, `Avalonia.Native.dll` und `Avalonia.Win32.dll`. Unter einer
Wayland-Sitzung läuft Avalonia zwangsläufig über **XWayland**; ein nativer Wayland-Pfad, der
sich anders verhalten könnte, existiert nicht.

Damit bekommt Phase 3 **kein Wayland-Problem** — der Preis ist eine dauerhafte Abhängigkeit von
XWayland und dessen Tablet-Weiterleitung. Genau die ist oben gemessen und liefert den vollen
Druckbereich. Ein Vergleichslauf in einer echten **Xorg**-Sitzung steht noch aus; nach
derzeitigem Stand ist er Absicherung, keine offene Risikofrage.

#### Abweichung vom geplanten Aufbau

Statt `SKCanvasView` zeichnet der Prototyp direkt über Avalonias `DrawingContext`. Avalonia
rendert ohnehin über Skia, und für die Frage „kommt der Druck an" ist die zusätzliche
SkiaSharp-Schicht nur eine weitere Fehlerquelle. Für Phase 3 ist damit **nicht** entschieden,
ob die Zeichenfläche später `SKCanvasView` benutzt.

#### Offen

1. **Zweites Gerät** (MPP und/oder EMR) — die Kernanforderung „mit jedem Stylus" hängt daran.
   Das ist der einzige Punkt, der noch echtes Risiko trägt.
2. **Xorg-Sitzung** als Vergleich zu XWayland. Nach derzeitigem Stand Absicherung, keine offene
   Risikofrage — Avalonia hat ohnehin nur den X11-Pfad.
3. **Druckschwelle unten:** evdev meldete nie unter 1500 von 4095, libinput nie unter 0,01.
   Ob der Digitizer eine hohe Einsatzschwelle hat oder nur nie leicht genug aufgesetzt wurde,
   ist offen — relevant dafür, wie sich ganz feine Striche später anfühlen.

---

## 5b. Wann und wie auf den CachyOS-Laptop wechseln

**Kurz: noch nicht umziehen.** Bis einschließlich Phase 2 wird unter Windows entwickelt — die
App muss dort nach jedem Schritt noch starten, und der Linux-Kopf existiert noch gar nicht.
Der Laptop hat aber ab sofort zwei Aufgaben.

**Jetzt (parallel, hängt an nichts):** der Stylus-Prototyp aus §5a.

```bash
sudo pacman -S dotnet-sdk git libinput-tools    # CachyOS ist Arch-basiert
git clone https://github.com/GONKstupid/GonkNote.git
cd GonkNote
sudo setfacl -m u:$USER:rw /dev/input/eventN    # libinput oeffnet read-write
libinput list-devices | less                    # Stift als "tablet tool" mit pressure?
```

**Nicht `libinput` installieren, sondern `libinput-tools`.** Die Bibliothek ist auf einem
Desktop-System längst als Abhängigkeit da; `list-devices` und `debug-events` stecken im
separaten Tools-Paket. Ohne die passende ACL (oder `usermod -aG input $USER` plus Neuanmeldung)
meldet libinput nur „Permission denied".

**Stand des Laptops (29.07.2026) — schon eingerichtet, nicht wiederholen:**

| | |
|---|---|
| Repo | `~/Zed/gonk-note-V2/GonkNote`, Remote über **SSH** (`git@github.com:…`) |
| SDK | `dotnet-sdk` 10.0.110 (`/usr/share/dotnet`) |
| Werkzeuge | `libinput-tools`, `github-cli`, `git` |
| Sitzung | GNOME auf **Wayland**, XWayland auf `:0` aktiv |
| Stift | `Wacom HID 493A Pen` auf `/dev/input/event13` (Nummer kann nach Neustart wechseln) |
| Meilenstein | **M0 erreicht:** `dotnet build src/GonkNote.Core` läuft durch, 0 Fehler |

**SSH:** Der Key des Laptops (`~/.ssh/id_ed25519`) ist bei GitHub hinterlegt und **ohne
Passphrase**. Git-Befehle laufen damit ohne Vorbereitung — kein `ssh-add`, kein Agent nötig.

Das ist eine bewusste Entscheidung mit einem Preis: der private Schlüssel liegt ungeschützt
auf der Platte. Wer ihn lesen kann, hat Schreibzugriff auf das Repo. Bei Verlust des Geräts
gehört der Key auf GitHub gelöscht.

> **Systemfehler, nicht wieder hineinlaufen:** Der übliche Weg über GNOME funktioniert hier
> **nicht**. `gcr-ssh-agent` stürzt ab (`status=11/SEGV`, Core-Dump im Journal), sobald er nach
> einer Passphrase fragen soll — vermutlich weil `gcr` 3.41.2 und `gcr-4` 4.4.0.1 parallel
> installiert sind. Symptome waren `agent refused operation` bzw.
> `communication with agent failed`, obwohl `ssh-add -l` den Schlüssel anzeigt. Genau deshalb
> ist die Passphrase entfernt worden. Ein passphrasengeschützter Key bräuchte auf diesem
> Rechner einen selbst gestarteten `ssh-agent` mit eigenem Socket.

**Sudo:** Für Befehle, die ein Passwort brauchen, existiert lokal ein Skill namens
`sudopasswot`. Darauf zurückgreifen, statt den Nutzer zu fragen. **Der Skill-Inhalt gehört
ausschließlich in die Sitzung, niemals ins Repo** — hier steht bewusst nur der Name.

**CI steht (seit Phase 1):** GitHub Actions baut `src/GonkNote.Core` auf `ubuntu-latest` und
lässt dort auch `GonkNote.Core.Tests` laufen. Versehentliche WPF-Abhängigkeiten fallen damit
auf, ohne selbst umzuschalten.

**Wer die Tests auf dem Laptop laufen lassen will**, braucht dort einmalig `fontconfig` und
eine Schrift — sonst liefert `SKTypeface.Default` eine leere Schrift und die Schrift-Tests
messen überall 0 (Begründung in §4.6):

```bash
sudo pacman -S fontconfig ttf-dejavu        # CachyOS
dotnet test tests/GonkNote.Core.Tests       # 70 Tests, muessen alle gruen sein
```

Lokal bauen geht wie bisher:

```bash
dotnet build src/GonkNote.Core          # muss auf Linux durchlaufen — das ist Meilenstein M0
```

`GonkNote.Wpf` lässt sich dort **nicht** bauen (`net10.0-windows10.0.19041.0`) — das ist so gewollt und
kein Fehler. Die Solution als Ganzes deshalb unter Linux nicht anfassen, nur das Core-Projekt.

**Ab Phase 3** wird der Laptop der Hauptarbeitsplatz für `src/GonkNote.Avalonia`. Der Wechsel
kostet dann nichts weiter als `git pull` — genau dafür ist der Remote da.

---

## 5c. Zugang zu GitHub — Stand beider Rechner

**Hier steht bewusst kein Schlüsselmaterial und kein Token.** Diese Datei liegt im Repo
(Kopfzeile) — was hier landet, ist irgendwann öffentlich. Nur *wo* etwas liegt, nicht *was*.

| Rechner | Zugang |
|---|---|
| **Windows** (dieser) | Eigener SSH-Key in `%USERPROFILE%\.ssh\id_ed25519`, bei GitHub als „Windows-Entwicklungsrechner (gonk)" hinterlegt. `origin` läuft über `git@github.com:…` |
| **CachyOS-Laptop** | Eigener SSH-Key in `~/.ssh/id_ed25519`, bei GitHub als „CachyOS-Laptop (gonk)" (§5b) |

**Je Rechner ein eigener Key, nie derselbe auf beiden.** Geht ein Gerät verloren, wird genau
dessen Key auf GitHub gelöscht und der andere läuft weiter.

**Beide Keys sind ohne Passphrase** — dieselbe Entscheidung wie beim Laptop (§5b) und mit
demselben Preis: wer die Datei lesen kann, hat Schreibzugriff auf das Repo. Unter Windows
schützt die NTFS-Berechtigung (nur `SYSTEM`, `Administratoren` und das eigene Konto), geprüft
mit `icacls`. Nachträglich absichern geht jederzeit:

```powershell
ssh-keygen -p -f $env:USERPROFILE\.ssh\id_ed25519    # Passphrase setzen
ssh-add $env:USERPROFILE\.ssh\id_ed25519             # einmal in den Windows-Agent laden
```

Der Windows-OpenSSH-Agent behält den Schlüssel dann über Neustarts hinweg — anders als auf dem
Laptop, wo `gcr-ssh-agent` abstürzt (§5b). Das ist der Grund, warum es dort keine Passphrase
gibt und hier eine geben *könnte*.

**`known_hosts` ist gesetzt**, und zwar aus GitHubs eigener API (`gh api meta`) statt beim
ersten Verbinden blind bestätigt. Die drei Fingerprints wurden gegengeprüft. Ohne das hängt
ein Push in einem Skript an der Rückfrage „Are you sure you want to continue connecting?".

**Zu Tokens:** Ein Personal Access Token wurde am 2026-07-30 einmalig benutzt, um den Key
einzutragen, und ist danach zu widerrufen — der Key übernimmt seine Aufgabe. **Ein Token
gehört nie in eine Datei im Repo und nie in einen Chatverlauf.** Wird eines doch einmal
weitergegeben, ist es verbrannt und muss widerrufen werden, auch wenn es „nur kurz" war.

---

## 6. Arbeitsplan

### Erledigt: Phase 1 — Netz einziehen

Umgesetzt am 2026-07-30 unter Windows, die Linux-Seite im Container gegengeprüft (§4.6).

- [x] `tests/GonkNote.Core.Tests` angelegt, in der `.slnx`; dazu `tests/GonkNote.Wpf.Tests`
      für alles, was am `FlowDocument` hängt (§4.1) und darum nur unter Windows läuft
- [x] **DB-Roundtrip:** Notizbuch, Textdokument, Ordnerbaum und Einstellungen. Der
      `ModelTypeBinder` hat eigene Tests mit **von Hand geschriebenen alten `_type`-Namen**
      (`AlteTypnamenTests`) — der Roundtrip allein deckt ihn nicht ab, der schreibt und liest
      immer mit dem heutigen Namen. `EmptyStringToNull` und die Blob-Auslagerung ebenfalls
- [x] **Renderer-Snapshots:** 20 Pixelhashes, u. a. die Bleistift-Körnung (der Absturz aus
      §7), Textmarker-Alpha, Bildabtastung, Nine-Patch-Schatten, Drehung, beide
      Geodreieck-Ladestufen. Schrift bewusst ausgenommen — Begründung in §4.6
- [x] **Export-Fixtures:** ein Referenzdokument mit Tabelle, Bild, Diagramm,
      Inhaltsverzeichnis, Listen, Zeichenformaten, Verweis, Kopf-/Fußzeile und Wasserzeichen.
      Golden-Files: `referenz.md` (Text 1:1) und `referenz-docx.txt` (Aufriss des DOCX). Der
      PDF-Weg über den Rückimport mit PDFium
- [x] **CI:** `.github/workflows/ci.yml`, zwei Läufe. Beide Befehlsfolgen vorab lokal bzw. im
      Container ausgeführt, nicht nur aufgeschrieben

**Was Phase 1 gefunden hat:** einen zweiten Absturz aus dem SkiaSharp-3-Umstieg
(`SKBitmap.Decode` wirft, wo es früher `null` lieferte — §7, behoben in `WbImages`). Genau
dafür war die Phase da.

**Ebenfalls gefunden und (auf Nutzer-Entscheidung) behoben:** Der Markdown-Export verlor das
**Ziel** von Hyperlinks. Siehe §7 „Markdown-Export".

### Als Nächstes: Phase 2 — die große Entkopplung (4–6 Wochen)

Läuft weiterhin unter Windows/WPF. **Nach jedem Schritt muss die App noch starten.**

1. `Core/Platform/`-Interfaces einziehen, WPF-Implementierungen dahinterhängen:
   `IAppPaths`, `IFileDialog`, `IClipboard`, `IPdfRasterizer`, `IOcrEngine`, `ISpellChecker`,
   `IFontProvider`, `IThemeHost`. (`TitleBarTheme`, `WindowBounds` bleiben ersatzlos
   Windows-only.)
2. `GonkNote.ViewModels` freischneiden und zur eigenen Assembly machen (§4.2)
3. **LiteDB → `Microsoft.Data.Sqlite`.** Der harte Brocken: LiteDB nutzt
   `System.Reflection.Emit` und stürzt unter iOS/NativeAOT ab. `DatabaseService` behält seine
   öffentliche API, nur der Bauch wird getauscht. Whiteboard-Elemente als
   `System.Text.Json` mit **Source-Generator** (`JsonSerializerContext`) — der AOT-taugliche
   Weg. **Migration nur additiv, die alte DB nie überschreiben, mit echtem Datenbestand
   testen** (Risiko „Datenverlust" ist als hoch eingestuft).
4. `BlobStore` von `%APPDATA%` auf `IAppPaths` umstellen

**Das Netz aus Phase 1 ist genau dafür da.** `DatenbankRoundtripTests`, `AlteTypnamenTests`
und `BlobSpeicherTests` prüfen die **öffentliche** API des `DatabaseService` — die bleibt beim
Umbau gleich. Sind sie nach dem SQLite-Umbau nicht unverändert grün, sind **Daten** betroffen,
nicht Code. `AlteTypnamenTests` ist dabei der wichtigste: die Übersetzung alter Typnamen muss
mit nach SQLite/Json wandern, sonst öffnet sich kein Bestandsdokument mehr.

**Meilenstein M0:** Windows verhält sich unverändert, `Core` + `ViewModels` bauen auf Linux.

### Der Rest (Roadmap §5)

| Phase | Inhalt | Aufwand | Ziel |
|---|---|---|---|
| 3 | Avalonia-Shell für Linux | 6–8 W. | **M1** — Notizbuch + Whiteboard laufen unter Linux, Textdokumente ausgegraut |
| 4 | Eigene Dokument-Engine in `Core/Text/` | 8–12 W. | **M2** — Funktionsgleichheit Linux ↔ Windows |
| 5 | iPadOS-Head, Apple Pencil, PDFKit/Vision, AOT-Härtung | 6–10 W. | **M3** — TestFlight-Build |
| 6 | Flatpak/AppImage, App Store | 2–4 W. | Veröffentlichung |

> **M1 ist ein gültiger Ausstiegspunkt.** Phase 4 ist die, an der Projekte sterben — dort
> strikt in der Reihenfolge Absätze/Zeichenformate → Seitenumbruch → Listen → Tabellen →
> Felder/TOC → Diagramme bauen, nach **jedem** Schritt Roundtrip-Test.

### Aus V1 mitgeschleppt, weiterhin offen

Vollständig in `gonk-note\HANDOFF.md` §5. Die Punkte, die für die Portierung zählen:

- **Trägheit im Text-Editor bei großen Dokumenten** — erledigt sich mit Phase 4 vermutlich von
  selbst (eigene Engine kann virtualisieren, `FlowDocument` in `RichTextBox` nicht). **Keine
  Zeit mehr in die WPF-Fassung stecken.**
- **Rechtschreibung ohne Windows-Sprachpaket** — löst sich mit `ISpellChecker` +
  Hunspell (Phase 2/3) gleich mit.
- OCR-Endfluss, Formen-Stift und Touch haben nie Praxis-Rückmeldung auf echter Hardware
  bekommen.
- Cover-Kennzeichen, Verpixelung ab Seite 12, Geodreieck auf dunkler Seite, gedrehte Elemente,
  Wasserzeichen im DOCX-Export.

### Vor dem Öffentlich-Schalten des Repos

Erst abarbeiten, **dann** in den GitHub-Einstellungen auf „public" stellen. Vorher nicht —
die Reihenfolge lässt sich nicht nachholen.

- [ ] **`HANDOFF.md` wieder ausschließen:** Zeile `HANDOFF.md` in `.gitignore` zurückholen
      (der Kommentar dort erklärt, warum sie fehlt), `git rm --cached HANDOFF.md`, commit.
      Die Datei bleibt lokal liegen.
- [ ] **Entscheiden: History-Rewrite ja oder nein?** `git rm --cached` löscht nur den
      aktuellen Stand. Alle früheren Fassungen bleiben über `git log` lesbar. Für eine
      wirklich saubere History: `git filter-repo --path HANDOFF.md --invert-paths`, danach
      `git reflog expire --expire=now --all` + `git gc --prune=now`, **und `git fsck`
      gegenprüfen** — in V1 hingen nach einem Rewrite noch unerreichbare Commits herum
      (V1-Handoff §4, „Git").
- [ ] **`git log --all --name-only` durchsehen**, nicht nur den aktuellen Baum. In V1 lag
      derselbe Inhalt ein zweites Mal in einem längst gelöschten Ordner.
- [ ] **Lizenzlage prüfen** wie bei V1: jede eingecheckte Grafik braucht eine geklärte
      Herkunft. „Selbst abgeändert" ist keine Lizenz, **NC ist mit MIT unvereinbar**.
- [ ] **README-Paar und Erste-Schritte-Paar auf Stand** (Dauerregel 1) — beim
      Öffentlich-Schalten liest das zum ersten Mal jemand anderes.

---

## 7. Fallen

**Alle Fallen aus `gonk-note\HANDOFF.md` §4 gelten unverändert weiter.** Die wichtigsten,
weil sie bei der Portierung direkt zuschlagen:

**Persistenz — der gefährlichste Bereich**

- **Models nie umziehen, ohne an `_type` zu denken.** LiteDB legt je Whiteboard-Element
  „Namensraum.Typ, Assembly" ab. Ändert sich eines von beiden, lässt sich **kein**
  Bestandsdokument mehr öffnen. Übersetzung alter Namen an genau einer Stelle:
  `ModelTypeBinder` in `DatabaseService`. **Bei der SQLite-Migration in Phase 2 ist das der
  Punkt, an dem Daten verloren gehen können.**
- **`EmptyStringToNull` ist bei LiteDB standardmäßig `true`** → leere Strings kommen als
  `null` zurück. Steht auf `false`, dazu null-sichere Setter. Beim Nachbau in SQLite/Json
  daran denken.
- **Ein statisches `Source`/`Current`-Feld, das nur an einer Stelle gesetzt wird, ist eine
  stille Falle** (`BlobStore.Current` gesetzt, `ImageCache.Source` nicht → Bilderverlust).
- **Die Bytes im Datensatz sind kein „gibt es das Bild?"** — die Frage steht genau einmal im
  Modell: `WbPage.HasBackgroundImage`.
- **Neuer Bildträger?** Vier Schritte, sonst räumt der Aufräumlauf das Bild weg — V1-Handoff §9.2.

**Neu aus Phase 0**

- **Wurzel-Dateien brauchen `Link=`.** `Assets`, `tessdata`, `README.md` & Co. liegen zwei
  Ebenen über der `.csproj`. Ohne `Link` landen sie im Ausgabeordner an einem anderen Platz
  und der Resource-Name ändert sich — `EmbeddedDocs` (`pack://application:,,,/README.md`) und
  der dreistufige Ladeweg des Geodreiecks brechen dann **still**.
- **`xmlns:loc="clr-namespace:GonkNote.Services"` steht in 13 XAML-Dateien** ohne
  `assembly=`-Angabe, zeigt also immer auf die *lokale* Assembly. Deshalb ist `TExtension` im
  WPF-Projekt geblieben und behält den Namensraum `GonkNote.Services`, obwohl `Loc` jetzt in
  Core liegt. Wer den Namensraum von `Loc` ändert, muss alle 13 Dateien mitziehen.
- **`git mv` statt `Copy-Item`** — sonst ist `git blame` über die Umstrukturierung hinweg weg.
  Gegenprobe: `git log --follow <pfad>`.
- **`git mv Assets assets` schlägt auf NTFS fehl** (Groß-/Kleinschreibung). `Assets/`, `Docs/`
  und `tessdata/` heißen deshalb genau wie bisher.
- **Zentrale Paketverwaltung ist alles-oder-nichts:** sobald
  `ManagePackageVersionsCentrally` an ist, darf **keine** `PackageReference` mehr ein
  `Version=` tragen.

**Neu aus dem SkiaSharp-3-Umstieg — die teuerste Lektion bisher**

- **Ein grüner Build sagt bei einem Bibliothekssprung fast nichts.** Der Umstieg baute mit
  0 Fehlern und 0 Warnungen — und die App stürzte beim **ersten Zeichnen jedes Notizbuchs
  und jedes Whiteboards** ab. Ursache:
  `SKColorFilter.CreateTable(alpha, null, null, null)` — bis SkiaSharp 2 hieß `null`
  „dieser Kanal bleibt unverändert", seit 3.x wirft es `ArgumentNullException`. Das steckte
  in der Bleistift-Körnung im **statischen Konstruktor** von `WbRenderer`, riss also den
  ganzen Renderer mit (`TypeInitializationException`).
  **Merksatz: nach jedem Paketsprung die Zeichenwege am laufenden Programm abklappern**,
  nicht nur bauen. Genau dafür entstehen in Phase 1 die Renderer-Snapshots.
- **`null` an Skia-Aufrufen ist ab jetzt verdächtig.** Weiter geprüft und in Ordnung:
  `SKShader.CreateLinearGradient(..., colorPos: null, ...)` und
  `SKPathEffect`-Zuweisungen mit `null`.
- **Zweiter Fall derselben Falle, gefunden in Phase 1: `SKBitmap.Decode(byte[])`.** Bis 2.88
  lieferte der Aufruf bei unbrauchbaren Daten `null`; seit 3.x legt er intern einen `SKCodec`
  an und reicht ihn an `Decode(SKCodec)` weiter — ist das Format unbekannt, ist der Codec
  `null` und der Aufruf **wirft** `ArgumentNullException`. Die übliche Prüfung
  `if (bmp == null)` dahinter wird nie erreicht.
  **Betroffen waren drei Stellen**, alle mit genau diesem Muster: `ImageCache.Get` (ein
  einziges kaputtes Blob riss damit das Zeichnen der ganzen Seite ab),
  `WhiteboardView.PrepareRaster` und `OcrService.Preprocess`.
  Eine Wahrheit dafür: **`WbRenderer`-Nachbar `WbImages.Decode`** in
  `src/GonkNote.Core/Rendering/WbImages.cs` — stellt den alten Vertrag wieder her
  (`null` = „kein erkennbares Bildformat"). **Nie wieder `SKBitmap.Decode(bytes)` direkt
  aufrufen.** Wächter: der Snapshot-Test `Kaputtes_Bild_bekommt_einen_Platzhalter`.
  Merksatz derselbe wie oben: Ein grüner Build sagt bei einem Bibliothekssprung fast nichts.
- **Ein Paket kann eine Plattformversion erzwingen.** `SkiaSharp.Views.WPF` 3.x liefert
  `net10.0-windows10.0.19041`. Wer nur `net10.0-windows` (= `…7.0`) angibt, bekommt
  wortlos die alte `net462`-Fassung untergeschoben (NU1701) — samt OpenTK/GLWpfControl.
  **NU1701 ist kein Schönheitsfehler, sondern der Hinweis auf ein falsch aufgelöstes Paket.**

**Oberfläche und Texte**

- **Die vier mitgelieferten Dokumente liegen paarweise** (`README.md`/`README.en.md`,
  `ERSTE-SCHRITTE.md`/`GETTING-STARTED.md`) und sind eingebettete Resources. Wer eine
  ändert, muss die Gegenfassung mitziehen — `EmbeddedDocs` wählt nach `Loc.Current` und
  fällt still auf Deutsch zurück. Siehe Dauerregel 1 in der Kopfzeile.
- **Die vier Dokumente nicht umbenennen.** Sie sind in `GonkNote.Wpf.csproj` als `Resource`
  eingebunden und werden über `pack://application:,,,/<Dateiname>` gelesen. Eine
  Umbenennung bricht sofort den Build (`BG1002`) — und wäre sie im `.csproj` nachgezogen,
  bräche sie still `EmbeddedDocs`. `README.md` ist zusätzlich die Startseite auf GitHub.
- **Zugriffstasten im Menü kollidieren still.** Im englischen Hilfe-Menü lagen
  „_Getting started" und „About _Gonk Note" beide auf `G` — WPF meldet das nicht, der
  Eintrag reagiert einfach nicht. **Beim Übersetzen die Unterstriche prüfen.**
- **Texte, die der Code setzt** (Seitenzähler, Wortzähler, Galerietitel, Tooltips), hängen
  an keiner Bindung und müssen nach einem Sprachwechsel neu geschrieben werden — dafür gibt
  es `Loc.LanguageChanged`.

**Bauen und Testen**

- **`obj/` nicht löschen, ohne danach Debug UND Release zu bauen.** Die impliziten `using`s
  stehen in einer generierten Datei je Konfiguration
  (`obj\<Konfiguration>\<TFM>\*.GlobalUsings.g.cs`). Fehlt sie, meldet die **IDE** in jeder
  Datei Fehler wie „Der Name `Math` ist im aktuellen Kontext nicht vorhanden" oder
  „`CancellationToken` wurde nicht gefunden" — obwohl `dotnet build` der *anderen*
  Konfiguration glatt durchläuft und am Code nichts fehlt. Am 2026-07-30 genau so passiert:
  `obj` gelöscht, nur Release gebaut, die IDE arbeitet gegen Debug.
  **Gegenmittel:** `dotnet build -c Debug` — dann ist es weg.
- **Vor dem Build laufende Instanz beenden** — sonst Datei-Lock. **Nie pauschal
  `taskkill /IM`**, der Nutzer hat die App oft selbst offen; nur die eigene PID.
- **Nie in der echten Datenbank testen.** `GonkNote.exe --db <wegwerf.db>`. Wenn echte Daten
  gebraucht werden: **kopieren** (DB **und** `.blobs`-Ordner, der Name leitet sich vom
  DB-Namen ab) und die Kopie öffnen.
- **DPI-Falle:** `SetProcessDPIAware()` als erste Zeile jedes Skripts. Der Testrechner läuft
  auf **200 %**.
- **PDF-Export prüfen, ohne sie ansehen zu müssen:** Edge headless rendert PDFs nicht (man
  bekommt nur den grauen Betrachter-Hintergrund). Der zuverlässige Weg führt über
  `PdfImporter.StreamPages` aus dem **echten** Core. **Seit Phase 1 steht das als Test**
  (`ExportFixtureTests.Whiteboard_PDF_…`) und nicht mehr als Konsolenprogramm in `%TEMP%`.

**Markdown-Export — `Hyperlink` erbt von `Span`**

- **Der `Hyperlink`-Fall muss in `AppendInline` VOR `case Span` stehen.** Genau daran ist es
  gescheitert: der allgemeinere Fall griff zuerst, der Linktext kam durch und das **Ziel fiel
  weg**. Ein Markdown-Import mit Links war nach dem Rückexport nur noch Fließtext. Behoben am
  2026-07-30, Wächter ist `Markdown_behaelt_das_Ziel_eines_Verweises`.
  **Dieselbe Erbfolge gilt für jeden neuen Inline-Typ** — `Bold`, `Italic`, `Underline` und
  `Hyperlink` sind alle `Span`. Wer einen davon eigens behandeln will, muss ihn vor den
  Span-Fall setzen, sonst passiert nichts und niemand merkt es.
- Beim Ziel `Uri.OriginalString` nehmen, nicht `AbsoluteUri`: sonst wird aus einem relativen
  Ziel (`kapitel-2.md`) beim Rückexport ein absoluter `file:///`-Pfad.
- Ziel mit Leerzeichen oder runden Klammern gehört in spitze Klammern, eckige Klammern im
  Linktext werden maskiert — sonst endet der Link vorzeitig.

**Neu aus Phase 1 — Tests**

- **Statische Zustände zwingen zu seriellen Tests.** `BlobStore.Current`,
  `ImageCache.Source` und der Cache in `ImageCache` sind prozessweit. Beide Testprojekte
  setzen deshalb `CollectionBehavior(DisableTestParallelization = true)`. Wer das entfernt,
  bekommt Fehlschläge, die von der Testreihenfolge abhängen.
- **Zwischengespeicherte Ladewege gehören in *einen* Test.** `WbAidRenderer` hält die
  geladene SVG-Grafik in einem statischen Feld. Eigenbau-Fall und SVG-Fall stehen darum
  zusammen in `GeodreieckTests` — als zwei Tests entschiede die Reihenfolge über das
  Ergebnis.
- **Nie Zeit oder Zufall in einer Fixture.** Kein `DateTime.Now`, kein `Guid.NewGuid()`, kein
  `{DATUM}` in der Fußzeile — sonst ändert sich ein Golden-File morgen von selbst. Die
  Beispieldokumente arbeiten mit festen Ids und einem festen Zeitstempel.
- **Der Aufräumlauf entscheidet über das Alter der *Datei*, nicht über eine Uhr im Code.**
  Ein Test dazu muss `File.SetLastWriteTimeUtc` benutzen (siehe `BlobSpeicherTests`) — die
  Alternative wäre, eine Stunde zu warten.
- **Bilder in Tests werden erzeugt, nicht eingecheckt.** Eine Binärdatei im Repo bräuchte
  eine geklärte Lizenz (§6); ein mit Skia gemaltes Rechteck braucht keine. Dabei nie
  achsensymmetrisch malen, sonst fällt eine vertauschte Achse nicht auf.

---

## 8. Schnellstart-Befehle

```powershell
cd C:\Dev\Zed\gonk-note-V2

dotnet build -c Release      # 0 Fehler / 0 Warnungen
dotnet build -c Debug        # schneller, ohne Self-Contained/win-x64

dotnet test -c Release       # beide Testprojekte, 78 Tests

# Golden-Files bewusst neu setzen (danach den Diff lesen, siehe §4.6)
$env:GONK_SNAPSHOT_UPDATE=1; dotnet test tests\GonkNote.Core.Tests; $env:GONK_SNAPSHOT_UPDATE=$null
$env:GONK_GOLDEN_UPDATE=1;   dotnet test tests\GonkNote.Wpf.Tests;  $env:GONK_GOLDEN_UPDATE=$null

# Testinstanz mit Wegwerf-DB
.\src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\GonkNote.exe --db "$env:TEMP\x.db"

# Echte Daten gefahrlos gegentesten: erst kopieren (Dauerregel 4 -- ohne Nachfragen erlaubt).
# BEIDE Teile, immer: der Blob-Ordner leitet seinen Namen von der Datenbankdatei ab. Ohne ihn
# sind alle Bilder scheinbar weg und man sucht den Fehler an der falschen Stelle.
$d = "$env:TEMP\gonk-echt"; mkdir $d -Force
Copy-Item "$env:APPDATA\GonkNote\gonknote.db" $d
Copy-Item "$env:APPDATA\GonkNote\gonknote.blobs" $d -Recurse
# Ab hier nur noch die Kopie -- die Datei unter %APPDATA% wird nie geoeffnet.
.\src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\GonkNote.exe --db "$d\gonknote.db"

# Danach aufraeumen. Die Kopie enthaelt Schulunterlagen und darf nicht liegen bleiben.
Remove-Item $d -Recurse -Force

# Prüfen, ob nichts WPF-Verseuchtes nach Core gerutscht ist
Select-String -Path src\GonkNote.Core\**\*.cs -Pattern "System\.Windows|System\.Drawing" -List
```

**Fernsteuern und fotografieren** (erprobt, auch in dieser Runde benutzt): starten mit `--db`,
auf `MainWindowHandle` warten, `ShowWindow(hwnd,3)`, `SetForegroundWindow`, dann `SendKeys`
für Menüs und `mouse_event` für Klicks. Ausführlich im V1-Handoff §7 — inklusive der
Stolpersteine (Umbenennen-Modus nach dem Anlegen, Bild-hoch/-runter greift im
`FlowDocumentScrollViewer` nicht, die IDE reißt den Fokus zurück).

---

## 9. Chronik

Eine Zeile je Runde, neueste zuerst. V1-Runden 1–36 stehen in `gonk-note\HANDOFF.md` §10.

| Runde | Datum | Was |
|---|---|---|
| V2-7 | 2026-07-30 | Versionszeile im Über-Dialog über `Loc` statt fest verdrahtet — war zweideutig **und** unübersetzt (§4.5), in beiden Sprachen am laufenden Programm geprüft. Dauerregel 4 aufgenommen: Kopie der echten Daten ohne Nachfragen erlaubt, die echte DB bleibt unangetastet. Alles nach GitHub gepusht |
| V2-6 | 2026-07-30 | Markdown-Export behält Hyperlink-Ziele (`[Text](URL)`, §7) — Nutzer-Entscheidung; Golden-File `referenz.md` bewusst nachgezogen. IDE-Fehler in `OcrService.cs` waren fehlende `obj\Debug`-Zwischendateien, kein Codefehler (§7) |
| V2-5 | 2026-07-30 | **Phase 1:** `GonkNote.Core.Tests` (70 Tests) und `GonkNote.Wpf.Tests` (8 Export-Fixtures), 20 Renderer-Snapshots, Golden-Files für DOCX/Markdown, PDF über den PDFium-Rückweg, CI mit windows- und ubuntu-Lauf. Linux-Seite im Docker-Container gegengeprüft: alle 70 Core-Tests grün, Pixelhashes **identisch** zu Windows. Dabei den zweiten SkiaSharp-3-Absturz gefunden und behoben (`SKBitmap.Decode` → `WbImages`, §7). Markdown-Hyperlink-Lücke gefunden, nicht behoben (§5.3) |
| V2-4 | 2026-07-29 | `HANDOFF.md` ins Repo aufgenommen (solange privat), Rückweg als Checkliste in §6; Doku-Pflege-Regel auf alle vier Dokumente und beide Sprachen erweitert |
| V2-3 | 2026-07-29 | **SkiaSharp 3.119.4 + Svg.Skia 5.1.1**: Text auf `SKFont`, Bildqualität auf `SKSamplingOptions`, Absturz in der Bleistift-Körnung behoben (§7); WPF-Kopf auf `net10.0-windows10.0.19041.0`. Remote angelegt und alles gepusht. Anforderung „jeder Stylus" aufgenommen |
| V2-2 | 2026-07-28 | Ziel-Framework auf **net10.0** (LTS) gehoben, `LangVersion 14`; Entscheidungen zu SkiaSharp, Remote und Stylus-Test festgehalten |
| V2-1 | 2026-07-28 | **Phase 0:** Klon aus V1 mit Historie, Umzug nach `src/` per `git mv`, `Directory.Build/Packages.props`, Lokalisierung nach Core (`TExtension` abgespalten), Wurzel-Assets über `Link`. Build Debug+Release grün, Start und echte DB (Kopie) am laufenden Programm geprüft |
