# Gonk Note V2 — Projektübergabe

**Stand: 2026-08-03 · Version 0.3.0 · net10.0 · SkiaSharp 3 · SQLite · Avalonia 12 · Phase 3, Zeichenfläche steht**

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
>    Fehlerbild, das nur mit Bestandsdokumenten auftritt —, darf der Inhalt von
>    `%APPDATA%\GonkNote` (`gonknote.sqlite`, solange vorhanden auch `gonknote.db`,
>    **plus** `gonknote.blobs`) nach `%TEMP%` kopiert und die **Kopie** geöffnet werden
>    (Befehle in §8).
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

Danach **Phase 1 komplett** (§4.6): zwei Testprojekte, Renderer-Snapshots,
Export-Golden-Files und CI auf Windows **und** Ubuntu. Dabei einen zweiten Absturz aus dem
SkiaSharp-3-Umstieg gefunden und behoben (`SKBitmap.Decode`, §7).

Dann **Phase 2, Schritte 1 und 2** (§4.7): `Core/Platform/` mit zwölf Schnittstellen,
der WPF-Kopf dahinter, und **`GonkNote.ViewModels` als eigene `net10.0`-Assembly**. Damit
ist der Ringschluss aus §4.2 aufgelöst und der Compiler hält ab jetzt nach, dass Core und
ViewModels WPF-frei bleiben.

Dann **Phase 2, Schritte 3 und 4** (§4.8): **LiteDB ist aus dem Produktivpfad
verschwunden**, die Persistenz läuft über `Microsoft.Data.Sqlite` mit
`System.Text.Json`-Source-Generator. Eine Altdatenbank wird beim ersten Start **einmalig
übertragen**, rein additiv; die alte Datei bleibt unversehrt liegen. An einer Kopie der
echten Datenbank feldweise gegengeprüft.

Dann **Phase 3, erster Brocken** (§4.9): **`src/GonkNote.Avalonia` steht und läuft** —
Avalonia 12.1.1 auf `net10.0`, alle zwölf Schnittstellen aus `Core/Platform/` umgesetzt,
Ordnerbaum und Galerie aus derselben `MainViewModel`-Instanz wie der WPF-Kopf. Die Farben
kommen aus einer **Farbtabelle in Core** (`Core/Theming/`), nicht aus einem zweiten Paar
fest verdrahteter Dateien. Version auf **0.3.0** angehoben.

Zuletzt **Phase 3, zweiter Brocken** (§4.10), **auf dem CachyOS-Laptop**: **die
Zeichenfläche steht.** Notizbuch und Whiteboard zeichnen, radieren und speichern unter
Linux; gezeichnet wird von `WbRenderer` aus Core auf Avalonias **eigenem `SKCanvas`**
(`ISkiaSharpApiLeaseFeature`, dieselbe SkiaSharp-Fassung wie Core). Der Eingabepfad nimmt
`GetIntermediatePoints()`, erkennt Druck statt ihn anzunehmen und weist den Handballen ab;
**die Neigung steht seitdem im Dateiformat** und verbreitert den Bleistift (§4.11).
**Textdokumente bleiben ausgegraut** — das ist so vorgesehen (M1). Dazu ein Linux-Pendant
der Fernsteuer-Werkzeuge (`tools/linux/`), das es bisher gar nicht gab.

**Als Nächstes:** die drei Restpunkte von Phase 3 — Drag & Drop im Baum, einblendbare
Titelleiste, `EmbeddedDocs`-Gegenstück (§6, Brocken 6 und 7). Danach ist **M1** erreichbar;
die Entscheidung, ob er ausgerufen wird, steht in §6.

**Tests laufen lassen:**

```powershell
dotnet test -c Release        # Windows: beide Projekte, 138 Tests
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

**Erledigt in Phase 2:** §4.7 (Platform-Naht, eigene ViewModels-Assembly) und §4.8
(LiteDB → SQLite, `BlobStore` über `IAppPaths`).

**Erledigt in Phase 3, erster Brocken:** §4.9 (Avalonia-Shell, Farbtabelle in Core).

**Erledigt in Phase 3, zweiter Brocken:** §4.10 (Zeichenfläche, Eingabepfad, `WbHit` in
Core, Linux-Werkzeuge) und §4.11 (Neigung im Dateiformat). Testzahl steht jetzt bei **138**
(125 Core + 13 WPF).

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
| Core/ViewModels referenzieren nichts aus Wpf | ✅ **seit Phase 2** — beide sind eigene `net10.0`-Projekte, ein `System.Windows.*` darin baut nicht mehr (§4.7) |
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
│  │  ├─ Platform/               die Naht zu den Köpfen (§4.7)        ← neu in Phase 2
│  │  │                          IAppPaths, IDialogService, IFileDialog, IClipboard,
│  │  │                          IThemeHost, IShell, IUiScheduler, IOcrEngine,
│  │  │                          ISpellChecker, IPdfRasterizer, IFontProvider,
│  │  │                          IDocumentIo — gebündelt in IPlatformServices
│  │  ├─ Rendering/              WbRenderer (Skia), WbAidRenderer (Geodreieck), WbImages (§7),
│  │  │                          WbImagePrep (Bildimport + OCR-Vorbereitung, §4.7)
│  │  ├─ Services/               DatabaseService (SQLite, §4.8), GonkJson (Source-Generator),
│  │  │                          ILegacyDatabaseReader, BlobStore, ImageCache, UndoStack,
│  │  │                          PdfImporter, DocumentHealth
│  │  ├─ Editing/                WbErase — punktgenaues Radieren
│  │  │                          WbHit — Trefferprüfung und Lasso (§4.10)  ← neu in Phase 3
│  │  ├─ Theming/                die Farbtabelle (§4.9)              ← neu in Phase 3
│  │  │                          ThemeColor (20 Farben), HexColor, ThemeDefinition,
│  │  │                          Themes.Light/.Dark — ein Theme ist eine Datentabelle
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
│  ├─ GonkNote.Avalonia/         net10.0 · LINUX-KOPF (§4.9)             ← neu in Phase 3
│  │  │                          Läuft auch unter Windows — genau deshalb wird Phase 3
│  │  │                          hier entwickelt und nicht auf dem Laptop (§5b)
│  │  ├─ Program.cs, App.axaml(.cs), MainWindow.axaml(.cs)
│  │  ├─ Platform/               die Umsetzungen zu Core/Platform:
│  │  │                          Avalonia* je Schnittstelle + AvaloniaPlatformServices,
│  │  │                          AvaloniaThemeHost (baut die Ressourcen aus der Farbtabelle),
│  │  │                          Modal.cs (synchron ↔ async, die größte Naht — §7)
│  │  ├─ Views/                  Converters, MessageWindow (Ersatz der MessageBox), AboutWindow,
│  │  │                          SkiaCanvas (der Weg an Avalonias SKCanvas, §4.10),
│  │  │                          WhiteboardView + .Render + .Input — die Zeichenfläche
│  │  ├─ Themes/Styles.axaml     Form und Vektor-Symbole — KEINE Farben (die kommen aus Core)
│  │  └─ Services/Localization/  TExtension + LocText (§7 „Übersetzung im Linux-Kopf")
│  │
│  └─ GonkNote.Wpf/              net10.0-windows10.0.19041.0 · Windows-Kopf (AssemblyName bleibt GonkNote)
│     ├─ App.xaml(.cs), MainWindow.xaml(.cs)
│     ├─ Platform/               die Umsetzungen zu Core/Platform     ← neu in Phase 2
│     │                          Wpf* je Schnittstelle + WpfPlatformServices (das Bündel),
│     │                          WpfThemeHost (war die statische Klasse ThemeService),
│     │                          WpfDocumentIo (die FlowDocument-Naht aus §4.1)
│     ├─ Views/                  Whiteboard (Partials), TextEditor (Partials), Dialoge
│     ├─ Themes/                 Light/Dark/Styles
│     └─ Services/               alles mit WPF-Bezug (§4.1):
│                                Docx/Markdown-Im-/Export, MarkdownFlow, PdfExporter,
│                                TextStyles, DocumentImages, EmbeddedDocs, OcrService,
│                                SpellCheckSupport, TitleBarTheme,
│                                WindowBounds, Localization/TExtension.cs
│
├─ .github/workflows/ci.yml      zwei Läufe: windows (alles), ubuntu (Core, ViewModels,
│                                Legacy, **Avalonia**) — §4.6, seit Phase 3 §4.9
│
├─ tests/
│  ├─ GonkNote.Core.Tests/       net10.0 · läuft auch unter Linux · 100 Tests
│  │  └─ Snapshots/*.sha256      Pixelhashes des Renderers (Golden-Files)
│  └─ GonkNote.Wpf.Tests/        net10.0-windows · nur Windows · 13 Tests
│     ├─ Fixtures/               referenz.md, referenz-docx.txt (Golden-Files)
│     └─ FarbtabelleTests.cs     hält Core/Theming und Themes/*.xaml zusammen (§4.9)
│
├─ tools/                        Werkzeuge, KEIN Produktivcode, nicht in der Solution
│  ├─ stylus-prototyp/           Messwerkzeug zu §5a — Ergebnis liegt dort vor
│  │  ├─ GonkNote.StylusProbe/   Avalonia-Prototyp (Druck als Kreisradius)
│  │  ├─ evdev_beschreiben.py    Achsenbereiche des Digitizers (EVIOCGABS)
│  │  ├─ evdev_druck.py          Druckverlauf mitschneiden und auswerten
│  │  └─ messungen/              Rohberichte der Läufe
│  ├─ schau.ps1                  App mit DB-Kopie starten und fotografieren (§8)
│  ├─ klick.ps1                  ein Klick / Tastendruck + neues Foto
│  ├─ kette.ps1                  mehrere Klicks in EINEM Durchgang — für Menüpfade (§7)
│  └─ linux/                     das Gegenstück dazu unter Linux (§4.10)  ← neu in Phase 3
│     ├─ schau.sh                Kopf starten und fotografieren
│     ├─ klick.sh                Schritte abarbeiten + Foto
│     └─ zeiger/                 klickt und tippt über X11/XTEST, ohne Fremdpaket
│
├─ Assets/  tessdata/  Docs/     1:1 aus V1, in der Wurzel
└─ LICENSE, README(.en).md, ERSTE-SCHRITTE.md, GETTING-STARTED.md, THIRD-PARTY-NOTICES.md
```

**`tools/` steht bewusst neben `src/`, nicht darin**, und ist **nicht** in `GonkNote.slnx`
eingetragen: was dort liegt, ist Wegwerf-Werkzeug und soll nicht mit den Produktivprojekten
verwechselt oder versehentlich mitgebaut werden. `GonkNote.StylusProbe` ist zusätzlich aus der
zentralen Paketverwaltung ausgeklinkt (`ManagePackageVersionsCentrally=false`), damit seine
Avalonia-Version nicht die Versionswahl für Phase 3 vorwegnimmt.

**Noch nicht angelegt:** `src/GonkNote.iOS` (Phase 5). Bewusst — leere Projekte, die nicht
bauen, sind nur Ballast; sie entstehen, wenn ihre Phase beginnt.

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

### 4.2 `GonkNote.ViewModels` ist eine eigene Assembly — erledigt in Phase 2

Bis Phase 2 kompilierte der WPF-Kopf die Dateien mit
(`<Compile Include="..\GonkNote.ViewModels\**\*.cs" />`), weil `MainViewModel` die
WPF-Klassen aus §4.1 direkt aufrief — ein eigenes Projekt wäre ein **Ringschluss** gewesen
(ViewModels → Wpf → ViewModels).

**Aufgelöst am 2026-07-31** über `Core/Platform/` (§4.7): das ViewModel bekommt seinen Kopf
als `IPlatformServices` herein, statt dessen Klassen zu kennen. Seitdem ist
`src/GonkNote.ViewModels/GonkNote.ViewModels.csproj` ein `net10.0`-Projekt in der `.slnx`,
und der WPF-Kopf referenziert es.

**Die vorhergesagte Falle ist eingetreten und behoben:** In `MainWindow.xaml` stand
`xmlns:vm="clr-namespace:GonkNote.ViewModels"`. Das trägt jetzt `;assembly=GonkNote.ViewModels` —
ohne den Zusatz zeigt `clr-namespace` immer auf die *lokale* Assembly, und die
`DataTemplate`s für die Registerkarten hätten ihre Typen nicht mehr gefunden.

### 4.3 Ziel-Framework `net10.0` statt `net9.0`

**Entschieden am 2026-07-28.** Die Roadmap sieht `net9.0`/`LangVersion 13` vor — das ist
eine **STS**-Version, deren Support im Mai 2026 ausgelaufen ist. `net10.0` ist **LTS**
(bis November 2028) und deckt damit den kompletten 9–12-Monats-Zeitrahmen der Portierung ab.
Auf diesem Rechner ist ohnehin keine 9er-Runtime installiert (nur 8.0 und 10.0).

Gesetzt: `LangVersion 14`, `GonkNote.Core` → `net10.0`, `GonkNote.Wpf` → `net10.0-windows10.0.19041.0`.
Build Debug und Release grün, Start und Textdokument mit DB-Kopie geprüft.

**Für die späteren Köpfe:** `net10.0-ios` bringt das hier installierte SDK mit.

> **Die Avalonia-Frage von damals ist beantwortet** (2026-08-03, §4.9): **Avalonia 12.1.1
> trägt `net10.0`** — erst gemessen im Stylus-Prototyp (§5a), seit Phase 3 im echten Kopf.
> Der Rückfallweg über Avalonia 11.x wird nicht gebraucht.

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
| Bildimport-Verkleinerung (`WhiteboardView.Import`) | ✅ **seit Phase 2** — als `WbImagePrep.ForImport` in Core, Wächter `BildaufbereitungTests` |
| OCR-Vorverarbeitung (`OcrService`) | ✅ **seit Phase 2** — als `WbImagePrep.ForOcr` in Core, gleicher Wächter |

**Damit ist jede Stelle, die der SkiaSharp-3-Umstieg angefasst hat, unter einem Test.**
Beide Methoden waren reines SkiaSharp und hatten nur deshalb keinen — sie lagen privat im
WPF-Kopf. Der Umzug nach `Core/Rendering/WbImagePrep.cs` hat nichts an ihrem Inhalt geändert;
die WPF-Seite ruft sie jetzt nur noch auf. Genau so war es in Phase 2 vorgesehen.

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
WPF-Kopf — die CI baut deshalb projektbezogen. *(Der LiteDB-Punkt ist seit §4.8 nur noch
Geschichte: SQLite regelt Mehrfachzugriff selbst, über `PRAGMA busy_timeout`.)*

**Neu als Paket:** `SkiaSharp.NativeAssets.Linux`, nur im Core-Testprojekt. Das Paket
`SkiaSharp` bringt die native Bibliothek für Windows und macOS mit, für Linux **nicht** —
zum Bauen genügt das (M0), zum Ausführen nicht. Es steht bewusst **nicht** in
`GonkNote.Core`: eine Linux-`.so` gehört nicht in ein Paket, das der Windows-Kopf
mitschleppt. Dazu braucht das System `fontconfig` und mindestens eine Schrift; die CI
installiert `libfontconfig1` und `fonts-dejavu-core`.

> **Die Voraussage ist eingetreten** (§4.9): Das Paket steht seit Phase 3 ein zweites Mal in
> `src/GonkNote.Avalonia`, aus demselben Grund und mit derselben Begründung. Es deckt dort
> den **Renderer** ab (`WbRenderer` benutzt SkiaSharp direkt); was Avalonia selbst zeichnet,
> bringt Avalonia mit.

---

### 4.7 Phase 2, Schritte 1 und 2 — die Naht zwischen Core und Kopf

Umgesetzt am 2026-07-31. Schritte 3 und 4 folgten am 2026-08-02, siehe §4.8.

#### Die Schnittstellen in `Core/Platform/`

Zwölf Stück, alle mit mindestens einem echten Aufrufer — eine Schnittstelle ohne Benutzer
ist nur eine Behauptung. Die Roadmap nennt acht; `IDialogService`, `IShell`, `IUiScheduler`
und `IDocumentIo` sind dazugekommen, weil das `MainViewModel` sie braucht.

| Schnittstelle | Was der Kopf liefert | Warum sie existiert |
|---|---|---|
| `IAppPaths` | Datenordner, Programmordner | `%APPDATA%` stand an vier Stellen hartkodiert |
| `IDialogService` | `MessageBox` | 5 Aufrufe im `MainViewModel` |
| `IFileDialog` | Öffnen/Speichern | Win32-Filter mit `FileFilter` beschrieben statt als `\|`-Zeichenkette |
| `IClipboard` | Text, Bild (**als PNG-Bytes**), Dateiliste | ein `BitmapSource` überquert die Grenze nicht |
| `IThemeHost` | Light/Dark umschalten | war die statische Klasse `ThemeService` |
| `IShell` | Datei öffnen, App beenden | `Process.Start` bzw. `MainWindow.Close()` |
| `IUiScheduler` | Wiederholung auf dem UI-Faden | war `DispatcherTimer` (Autospeicherung) |
| `IOcrEngine` | Tesseract | Rückfall `NoOcrEngine` liegt daneben |
| `ISpellChecker` | Windows-Rechtschreib-API | Rückfall `AlwaysSupportedSpellChecker` |
| `IPdfRasterizer` | PDFium | Umsetzung `PdfiumRasterizer` liegt in Core (Windows **und** Linux teilen sie); iOS bekommt PDFKit |
| `IFontProvider` | „Segoe UI" | `WbFonts` hatte den Namen fest verdrahtet |
| `IDocumentIo` | Im-/Export über `FlowDocument` | die Naht aus §4.1 — bleibt bis Phase 4 im Kopf |

**Sie werden als Bündel übergeben, nicht global abgefragt.** `IPlatformServices` fasst alle
zwölf zusammen; `MainViewModel` nimmt es im Konstruktor. Das ist Absicht: wenn Phase 3
`AvaloniaPlatformServices` anlegt, sagt der Compiler dort Stück für Stück, was noch fehlt.
Bei zwölf Konstruktor-Argumenten stünde dieselbe Liste verteilt über mehrere Aufrufe, und
der nächste Dienst käme still nur an einer davon an.

Der **Windows-Kopf** setzt in `App.OnStartup` als Erstes `Platform`, `AppPaths.Current` und
`WbFonts.UiFamily` — noch vor der Datenbank, denn die fragt bereits nach den Pfaden. Die
Views greifen über `App.Platform` zu, genau wie sie es mit `App.Db` schon taten.

#### Was dabei aus den ViewModels verschwinden musste

`GonkNote.ViewModels` ist jetzt `net10.0` ohne `-windows`. Alles, was WPF war, ist raus:

| War | Ist |
|---|---|
| `Brush IconBrush` (aus `Application.Current.Resources`) | `string? IconColorHex` + `HexToBrushConverter` im Kopf |
| `Visibility FavoriteVisibility` / `StarVisibility` | `bool IsFavorite` + der vorhandene `BoolToVis` |
| `ImageSource CoverImage` (`BitmapImage`) | `byte[]? CoverImageData` + `BytesToImageConverter` |
| `Brush CoverBrush` (`LinearGradientBrush`) | zwei Hex-Farben + `GradientBrushConverter` (MultiBinding) |
| `CommandManager.RequerySuggested` | eigenes `CanExecuteChanged` + `RaiseCanExecuteChanged` |
| `FlowDocument`-Export im ViewModel | `IDocumentIo.ExportText/ExportBoard` → `ExportResult` |

**`System.Windows.Input.ICommand` durfte bleiben.** Der Typ liegt trotz seines Namensraums
in `System.ObjectModel` und steht auch unter Linux und iOS zur Verfügung; Avalonia bindet
gegen genau diese Schnittstelle.

**Die Kachel dekodiert ihr Cover nicht mehr selbst.** Vorher hielt jedes
`GalleryItemViewModel` eine fertige `BitmapImage`, jetzt nur die Bytes; das Dekodieren macht
der Konverter je Bindung. Bei 240 px Vorschaubreite ist das billig — falls eine Galerie mit
sehr vielen Notizbüchern doch spürbar wird, ist das die Stelle.

#### Am laufenden Programm geprüft (Dauerregel 1 und 4)

Mit einer **Kopie** der echten Datenbank samt Blob-Ordner, in **beiden** Sprachen: Galerie
mit Ordnerfarben (eigene Farbe *und* Theme-Rückfall), Cover-Bild aus dem Blob-Speicher,
Notizbuch öffnen, Baum mit Favoritensternen und Schnellzugriff, Kontextmenü in Baum **und**
Galerie, Theme-Wechsel hell/dunkel, Export-Dialog und ein vollständiger PDF-Export. Die
erzeugte PDF wurde **nicht angesehen, sondern gemessen** — über `PdfImporter` aus dem echten
Core (§7): 2 Seiten, Hochformat. Die echte Datenbank blieb unangetastet, die Kopie ist
gelöscht.

**Dabei zwei Fehler gefunden**, beide in §7 beschrieben: die neuen Kontextmenü-Beschriftungen
blieben beim Sprachwechsel stehen, und drei Einträge des Galerie-Menüs standen seit jeher
fest auf Deutsch.

---

### 4.8 Phase 2, Schritte 3 und 4 — LiteDB → SQLite, und der Blob-Ordner über `IAppPaths`

Umgesetzt am 2026-08-02. **Damit ist Phase 2 abgeschlossen.**

#### Warum überhaupt

LiteDB baut die Zuordnung zwischen Objekt und Datensatz zur Laufzeit über
`System.Reflection.Emit`. Unter NativeAOT gibt es keinen Just-in-time-Übersetzer — das
stürzt beim ersten Zugriff ab, und AOT ist für den App-Store-Weg auf dem iPad Pflicht (§1).
SQLite ist eine C-Bibliothek ohne diese Eigenschaft; die Objekte gehen über einen
**`System.Text.Json`-Source-Generator** (`GonkJson`), der seinen Lese- und Schreibcode zur
Übersetzungszeit erzeugt.

**Die öffentliche API des `DatabaseService` ist unverändert geblieben.** Das war die
Bedingung: `DatenbankRoundtripTests`, `AlteTypnamenTests` und `BlobSpeicherTests` prüfen
genau sie. Sie sind nach dem Umbau unverändert grün — kein Test musste angepasst werden,
außer den dreien in `AlteTypnamenTests`, die jetzt bewusst die *Migration* bewachen.

#### Die vier Entscheidungen (Nutzer, 2026-08-02)

| Frage | Entscheidung | Warum |
|---|---|---|
| Dateiname | **`gonknote.sqlite`**, neben `gonknote.db` | Der **Stamm** bleibt `gonknote` — davon leitet `BlobStore` seinen Ordner ab. Ein anderer Stamm hieße `gonknote.blobs` findet niemand mehr, und alle Bilder wären scheinbar weg |
| Zeitpunkt | **automatisch und still beim ersten Start** | Verlustfrei und einmalig; ein Dialog wäre neue Oberfläche in zwei Sprachen für eine Frage ohne echte Wahl |
| Altdatei | **unangetastet liegen lassen** | Nie beschrieben, nie umbenannt, nie gelöscht — dieselbe Regel wie Dauerregel 4. Sie ist der Rückweg |
| Schema | **ein JSON-Dokument je Zeile** | Die App fragt nie über einzelne Felder ab; ein relationales Schema machte jede Modelländerung zur Schemamigration |

#### Was in der Datei steht

```sql
meta     (id TEXT PRIMARY KEY, value TEXT)   -- schema=1, migriert-aus=<dateiname>
items    (id TEXT PRIMARY KEY, parent_id TEXT NULL, json TEXT)
boards   (id TEXT PRIMARY KEY, json TEXT)
texts    (id TEXT PRIMARY KEY, json TEXT)
settings (id TEXT PRIMARY KEY, value TEXT)
CREATE INDEX ix_items_parent ON items(parent_id)
```

`parent_id` ist eine eigene Spalte, weil `DeleteItemRecursive` danach sucht. Die
Galerie-Abkürzung `GetCover` schneidet das Cover mit **`json_extract`** heraus, damit nicht
jedes bildlastige Board komplett durch den Deserialisierer muss — mit Rückfall auf den
vollständigen Weg, falls `json_extract` einmal fehlt.

#### Die `_type`-Namen leben wörtlich weiter

`WbElement` trägt jetzt `[JsonPolymorphic(TypeDiscriminatorPropertyName = "_type")]` und je
Elementtyp ein `[JsonDerivedType(…, "GonkNote.Core.Models.X, GonkNote.Core")]` — **dieselben
Zeichenketten**, die LiteDB geschrieben hat. Das ist Datenformat, kein Codedetail (§7).

#### Wo LiteDB geblieben ist

In einem **eigenen Projekt** `src/GonkNote.Legacy/` mit genau einer Aufgabe: eine Altdatei
lesen. Core kennt nur die Schnittstelle `ILegacyDatabaseReader`; der Kopf reicht die
Umsetzung im Konstruktor herein (`new DatabaseService(dbPath, new Legacy.LiteDbReader())`).
Windows und Linux referenzieren das Projekt, der iPadOS-Kopf wird es **nicht** tun — dort gab
es nie eine LiteDB-Datei, und das Paket wäre unter AOT nicht baubar.

Die Verbindung dorthin steht auf **`ReadOnly`**: LiteDB legt dann weder Log-Abschnitt noch
Prüfpunkt an. Das ist der Grund, warum die Altdatei nach zwei vollständigen App-Sitzungen
byteweise dieselbe war (unten).

#### Wie die Übertragung abläuft

1. Erkennen: existiert die Zieldatei nicht und liegt daneben eine Datei **ohne
   SQLite-Kopfkennung**, ist das der Bestand. Erkannt wird an den ersten sechzehn Bytes,
   nicht an der Endung — eine Endung ist eine Behauptung.
2. Lesen über `ILegacyDatabaseReader` (wirft bei einem Typ, den es nicht mehr gibt).
3. Schreiben nach `…​.sqlite.neu`, alles in **einer** Transaktion.
4. Erst nach dem Commit `File.Move` auf den endgültigen Namen.

Scheitert irgendetwas, verschwindet die halbe Datei und der Fehler kommt nach oben. Der
WPF-Kopf fängt ihn ab, meldet ihn über den neuen `Loc`-Schlüssel **`Db.OpenFailed`** (beide
Sprachen) und beendet sich — statt mit halben Daten weiterzulaufen. Der Text erscheint
zwangsläufig in der Standardsprache: die Sprachwahl steht in eben der Datenbank, die sich
nicht öffnen lässt.

**`UpsertItem` wird bei der Übertragung bewusst nicht benutzt** — es setzt `ModifiedUtc` auf
jetzt und datierte damit jeden Eintrag des Nutzers auf den Tag der Migration um.

#### Schritt 4: der Blob-Ordner

`AppPaths` hat drei neue Eigenschaften: `DatabaseFile` (jetzt `gonknote.sqlite`),
`LegacyDatabaseFile` (`gonknote.db`) und **`BlobFolder`** (`<DataFolder>\gonknote.blobs`).
`BlobStore.InFolder(...)` nimmt einen vorgegebenen Ordner; der Papierkorb entsteht daneben
mit demselben Stamm.

**Der alte Konstruktor `new BlobStore(datenbankpfad)` bleibt** und wird weiter benutzt, wenn
ein Pfad über `--db` mitgegeben wurde. Sonst zöge eine Testinstanz die Bilder der echten
Ablage an sich — und §8 hängt daran.

#### Am laufenden Programm geprüft (Dauerregel 4)

Mit einer Kopie der echten Datenbank (8,1 MB LiteDB + 23 Blobs). Zwei Wege:

**Feldweise gegen die Altdatei** (Wegwerf-Test, danach gelöscht): 27 Baumeinträge mit Name,
Art, Elternteil, Farbe, Anpinnung und Favorit; 6 Whiteboards, 8 Seiten, **160 Striche mit
6308 Druckpunkten**, 17 Formen, 9 Textelemente, 32 Bilder, 3 Einstellungen. Jedes Feld
gleich, und die Bildbytes über `ImageCache.Bytes` **byteweise identisch**.

**Am Programm:** Migration lief still beim Start (8,1 MB → 385 kB — LiteDB-Dateien haben viel
Leerraum). Galerie mit Ordnerfarben und Favoritensternen, Cover-Bilder aus dem Blob-Speicher,
Notizbuch mit Cover und Titeltext, Whiteboard mit vier importierten PDF-Seiten als
Bildelemente, Baum mit Schnellzugriff, Sprachwechsel EN→DE, Theme-Wechsel dunkel→hell.

**Die Altdatei war danach byteweise dieselbe** (SHA-256 vor und nach zwei Sitzungen mit
Schreibvorgängen). Die Kopie ist gelöscht, die echte Datenbank wurde nie geöffnet.

---

### 4.9 Phase 3, erster Brocken — die Avalonia-Shell steht

Umgesetzt am 2026-08-03, **unter Windows** (§5b). Der Rest von Phase 3 — vor allem die
Zeichenfläche — steht noch aus; die Häkchen stehen in §6.

#### Was der Kopf kann, und was ausdrücklich nicht

| Kann | Kann nicht |
|---|---|
| Start, Datenbank öffnen, Altdatenbank übertragen | **Zeichenfläche** — ein geöffnetes Dokument zeigt einen Hinweis (`Tab.NoCanvasYet`) |
| Ordnerbaum mit Farben, Favoritensternen, Schnellzugriff | Drag & Drop im Baum |
| Galerie mit Cover-Bildern, Kacheln je Dokumentart, Pfadleiste | Import und Export (`AvaloniaDocumentIo` wirft — §4.1) |
| Anlegen, Umbenennen, Löschen, Kontextmenüs | Texterkennung (`NoOcrEngine`), Rechtschreibung (`AlwaysSupportedSpellChecker`) |
| Theme hell/dunkel, Sprache de/en zur Laufzeit | „Hilfe → Erste Schritte" und das gerenderte README im Über-Dialog (`EmbeddedDocs` hängt an `FlowDocument`) |
| Über-Dialog mit Versionszeile und Datenordner | die einblendbare Titelleiste des maximierten Fensters |

**Es sind bewusst Rückfälle aus Core und keine halben Eigenbauten.** `NoOcrEngine` meldet
ehrlich „nicht verfügbar", statt einen leeren Text zu liefern, der von „nichts erkannt"
nicht zu unterscheiden wäre; `AvaloniaDocumentIo` liefert **leere** Formatlisten, damit gar
nicht erst ein Dateidialog ohne Formate aufgeht.

#### Die Farbtabelle — die Entscheidung, die hier fiel

**Nutzer-Entscheidung 2026-08-03: alle zwanzig Farben, das gezeichnete Blatt inbegriffen.**
Der WPF-Kopf hat sein Theme als Paar fest verdrahteter `ResourceDictionary`-Dateien. Noch
ein zweites solches Paar hätte den vorgemerkten Wunsch „eigene Farbschemata" (§6) zu einem
Umbau gemacht statt zu einer Zutat.

- `Core/Theming/ThemeColor` — 20 benannte Farben: 15 für die Oberfläche, **5 für das
  gezeichnete Blatt** (`CanvasBg`, `PageBg`, `PageLine`, `PageGridDot`, `DefaultInk`).
  Die Reihenfolge ist Teil des Formats; **neue Farben nur hinten anhängen.**
- `HexColor` — `#RGB`, `#RRGGBB`, `#AARRGGBB`; `TryParse` statt Ausnahme, weil der häufigste
  Aufrufer ein Konverter an einer Bindung ist.
- `Themes.Light` / `Themes.Dark` — die mitgelieferten Tabellen, **wörtlich dieselben Werte**
  wie in `src/GonkNote.Wpf/Themes/*.xaml`. Die Farben der App haben sich mit Phase 3 nicht
  geändert.
- `ThemeDefinition.Over(...)` — legt eine unvollständige Tabelle über eine vollständige.
  Das ist der Weg, auf dem später eine Theme-Datei mit drei Farben genügt.

**Der WPF-Kopf wurde nicht umgestellt.** Er liest weiter sein `ResourceDictionary` — ihn
anzufassen wäre ein Umbau ohne Gegenwert und stünde der Regel „der WPF-Kopf verhält sich
unverändert" entgegen. **Damit stehen dieselben zwanzig Farben an zwei Stellen**, und genau
dafür gibt es `FarbtabelleTests` im WPF-Testprojekt: er liest die beiden XAML-Dateien als
reines XML und vergleicht sie Zeile für Zeile mit der Tabelle — in beide Richtungen, also
auch „steht in der XAML eine Farbe, die die Tabelle nicht kennt?".

`AvaloniaThemeHost` baut daraus zur Laufzeit ein `ResourceDictionary` (`Brush.X` **und**
`Color.X` je Eintrag) und setzt zusätzlich `RequestedThemeVariant`, damit auch Avalonias
mitgelieferte Steuerelemente mitwechseln.

#### Die drei Stellen, an denen Avalonia nicht wie WPF ist

Sie sind der eigentliche Inhalt dieses Brockens — alles andere war Abschreiben.

1. **Synchron gegen asynchron** (`Platform/Modal.cs`). `Core/Platform/` ist durchgehend
   synchron, weil es gegen WPF entstanden ist: `MessageBox.Show` blockiert,
   `OpenFileDialog.ShowDialog` blockiert. Avalonia hat für beides nur `Task`-Fassungen. Die
   Schnittstelle auf `async` umzustellen wäre ein Eingriff in Core, in die ViewModels **und**
   in den WPF-Kopf gewesen. Stattdessen ein **verschachtelter Nachrichtenlauf**
   (`Dispatcher.PushFrame`) — dasselbe, was WPF für modale Dialoge von sich aus tut.
2. **Keine MessageBox.** Avalonia bringt keine mit; `Views/MessageWindow` ist der Ersatz.
   Nebenwirkung: die Knopfbeschriftungen sind zum ersten Mal **unsere** (neue Loc-Schlüssel
   `Dlg.Yes`/`Dlg.No`, beide Tabellen) — unter Windows liefert die MessageBox sie in der
   Sprache des *Systems*, nicht in der der App.
3. **Keine Icon-Schrift.** Der WPF-Kopf zeichnet die meisten Symbole mit Zeichen aus „Segoe
   Fluent Icons". Die Schrift gehört zu Windows, lässt sich nicht mitliefern und existiert
   unter Linux nicht — jedes Symbol wäre ein leeres Kästchen. Der Avalonia-Kopf benutzt
   deshalb **Vektorformen** (`Themes/Styles.axaml`), so wie es derselbe WPF-Kopf schon dort
   tut, wo die Schrift nichts Passendes hat (`Icon.Whiteboard` und Nachbarn).
   `TreeItemViewModel.IconGlyph` bleibt unberührt — das ist die Antwort des Windows-Kopfs
   auf dieselbe Frage.

Dazu kommt die Übersetzung, die zwei Anläufe gebraucht hat — sie steht in §7
(„Übersetzung im Linux-Kopf"), weil sie eine Falle ist und keine Entscheidung.

#### Am laufenden Programm geprüft (Dauerregel 1 und 4)

Mit einer **Kopie** der echten Datenbank samt Blob-Ordner, in **beiden** Sprachen und
**beiden** Themes:

- **Avalonia-Kopf:** Baum mit 9 verschiedenen Ordnerfarben und Favoritensternen,
  Schnellzugriff, Galerie mit Cover-Bild aus dem Blob-Speicher, Whiteboard-Kachel mit
  Punktraster, Textdokument-Kachel; Pfadleiste über drei Ebenen; Anlegen, Umbenennen
  (Enter/Escape), Löschen mit Rückfrage über `MessageWindow`; Kontextmenü in Baum **und**
  Galerie-Kachel; Sprachwechsel DE↔EN zur Laufzeit **mehrfach und über einen erzwungenen
  Sammellauf hinweg** (dazu §7); Theme-Wechsel hell↔dunkel; Über-Dialog mit
  „Version 0.3.0 · Portierung, Phase 3" bzw. „Port, phase 3".
- **WPF-Kopf an derselben Kopie:** unverändert — Baum, Galerie, Über-Dialog mit dem
  gerenderten README, Notizbuch mit Cover und Titeltext über den SkiaSharp-Renderer.
- **Die echte Datenbank blieb unangetastet** (SHA-256 vorher und nachher identisch), die
  Kopie ist gelöscht.

---

### 4.10 Phase 3, zweiter Brocken — die Zeichenfläche

Umgesetzt am 2026-08-03, **auf dem CachyOS-Laptop** — anders als der erste Brocken, und aus
dem Grund, den §5b dafür genannt hatte: alles, was am Stift und an Linux-Pfaden hängt,
lässt sich unter Windows nicht beurteilen.

#### Was jetzt geht

| Kann | Kann nicht |
|---|---|
| Notizbuch und Whiteboard **anzeigen** — Cover, Linien, Raster, Punkte, Hintergrundbilder | Text, Formen, Notizzettel **anlegen** (angezeigt werden sie) |
| **Zeichnen** mit Stift, Bleistift, Textmarker — Druck, Rückfall ohne Druck | Sticker, Texterkennung, Zahlenblock, Schnellaktionen, Geodreieck (nicht M1, §6) |
| **Radieren**, punktgenau (Striche werden aufgetrennt) | Bilder und PDF-Seiten importieren |
| Auswählen per Lasso und Verschieben, Löschen | Drehen und Skalieren der Auswahl |
| Seiten blättern, anlegen, löschen; Zoom, Verschieben, Finger-Gesten | Seiteneinstellungen (Format, Muster, Farbton) |
| Rückgängig und Wiederholen, Speichern | |

**Textdokumente bleiben ausgegraut** — das ist die M1-Vorgabe und keine Lücke. Der Text
dazu (`Tab.NoCanvasYet`) sagt das jetzt auch: er sprach vorher allgemein von „diesem
Dokument" und nannte die fehlende Zeichenfläche als Grund. Beide Tabellen nachgezogen
(Dauerregel 1).

#### Die Entscheidung, die hier fiel: ausleihen statt nachbauen

**Nutzer-Entscheidung 2026-08-03: der Renderer bekommt Avalonias eigenen `SKCanvas`.**
§5a hatte die Frage `SKCanvasView` gegen `DrawingContext` ausdrücklich offen gelassen. Die
Erhebung vorab hat sie beantwortet:

- **`Avalonia.Skia` 12.1.1 hängt an SkiaSharp 3.119.4** — Zeichen für Zeichen dieselbe
  Fassung, die `GonkNote.Core` benutzt. Der `SKCanvas`, den Avalonia herausgibt, ist für
  `WbRenderer` damit **derselbe Typ**, nicht ein gleichnamiger.
- Es gibt **`ISkiaSharpApiLeaseFeature`**: einen dokumentierten Weg an genau diese Leinwand.
- Ein offizielles `SkiaSharp.Views.Avalonia` gibt es **nicht** — SkiaSharp liefert Views
  für WPF, WinForms, MAUI und andere, für Avalonia nicht. „`SKCanvasView`" wäre hier
  Fremdpaket oder Eigenbau gewesen.

Der Weg über eine eigene `SKSurface` und ein `WriteableBitmap` wäre **eine volle Bildkopie
je Bild** gewesen — bei einem Digitizer, der schneller abtastet als die Oberfläche zeichnet,
die falsche Stelle zum Sparen. Das Paket steht jetzt **ausdrücklich** in der `.csproj` und
nicht nur als Abhängigkeit einer Abhängigkeit.

#### Die vierte Stelle, an der Avalonia nicht wie WPF ist

§4.9 hatte drei genannt. Das hier ist die vierte, und sie war die teuerste dieses Brockens:

**Avalonia zeichnet auf einem eigenen Faden.** `SKElement.PaintSurface` läuft unter WPF auf
dem Oberflächen-Faden, `ICustomDrawOperation.Render` läuft unter Avalonia auf dem
**Render-Faden**. Wer von dort in `_page.Elements` greift, liest eine Liste, die der
Oberflächen-Faden im selben Moment verändert — während ein Strich gezogen wird, also genau
dann, wenn am meisten passiert.

**Gelöst durch Aufzeichnen statt Zurückgreifen:** Auf dem Oberflächen-Faden nimmt ein
`SKPictureRecorder` die Zeichenbefehle entgegen, dort, wo die Daten leben. Was dabei
entsteht, ist ein `SKPicture` — unveränderlich, ohne Verweis auf lebende Listen. Der
Render-Faden spielt es nur noch ab. Aufzeichnen kostet fast nichts (es werden Befehle
notiert, nichts gerastert), und **der Gewinn ist doppelt**: der Zugriff ist von sich aus
sicher, und weil ein Bild Vektoren behält statt Pixel, bleibt der zwischengespeicherte
Seiteninhalt beim Zoomen scharf. Der WPF-Kopf rastert an derselben Stelle ein `SKImage` in
Fenstergröße (~20 MB) und muss es bei jedem Zoom- und Verschiebeschritt wegwerfen; hier
wird in **Seitenkoordinaten** aufgezeichnet, und Zoomen macht die Aufzeichnung gar nicht
erst ungültig.

#### Der Eingabepfad — gegen Fähigkeiten, nicht gegen ein Gerät

- **`GetIntermediatePoints()`**, wie §5a es verlangt. Der Digitizer tastet mit einigen
  hundert Hertz ab, die Oberfläche zeichnet mit sechzig.
- **Druck wird erkannt, nicht angenommen.** Avalonia gibt für einen Zeiger ohne Drucksensor
  nicht „unbekannt" zurück, sondern glatt **0,5** — ein druckloser Stift ist an der Zahl
  allein nicht von einem zu unterscheiden, der mittelfest aufliegt. Erst wenn ein Wert
  auftaucht, der davon abweicht, gilt das Gerät als drucktauglich; bis dahin läuft der
  **Rückfall** mit fester Breite. Damit ist die Zusage aus §1 („läuft mit jedem Stylus")
  nicht mehr eine Absicht, sondern eine Verzweigung im Code.
- **Handballenabweisung in zwei Stufen.** Der Finger zeichnet grundsätzlich nie — er
  verschiebt und zoomt; ein aufliegender Handballen kann also gar keinen Strich erzeugen.
  Und solange ein Stift aufliegt, wird **jede** Berührung verworfen, damit das Blatt nicht
  unter dem Stift wegrutscht. Möglich ist beides nur, weil `Pointer.Type` Stift und Finger
  sauber trennt — §5a hatte genau das als Voraussetzung geprüft.
- **Das Radiergummi-Ende** meldet sich als eigener Zeiger (`IsEraser`) und schlägt das
  gewählte Werkzeug; die zweite Stift-Taste radiert, solange sie gehalten wird.

> **Neigung kam zunächst an, ohne gespeichert zu werden.** Das ist mit §4.11 erledigt —
> sie steht jetzt im Format und verbreitert den Bleistift.

#### Die Stift-Anzeige (F9) — ein Messgerät, kein Feature

Eingeblendet mit **F9**, standardmäßig aus: Zeigerart, Druck mit Angabe ob echt oder
Rückfall, Neigung in Grad, Zahl der Punkte im laufenden Strich.

Sie steht da aus einem konkreten Grund: **§5a hat im Prototyp gemessen, nicht in der App.**
Ob Druck und Neigung auch durch den fertigen Eingabepfad kommen, beantwortet sonst niemand —
und ein gleichmäßiger Strich sieht genauso aus, ob er aus dem Rückfall stammt oder aus einem
Gerät ohne Drucksensor. Sie ist ausdrücklich auch die Stelle, an der ein **zweites
Stiftgerät** (MPP, EMR) beurteilt wird — der einzige Punkt aus §5 „Noch offen" mit echtem
Restrisiko.

#### `WbHit` ist nach Core gewandert

Trefferprüfung und Lasso sind reine Geometrie — kein Pixel wird gezeichnet, keine Eingabe
entgegengenommen. Nach der Faustregel aus §3 gehört das nach Core. Bis Phase 3 lag es privat
in `WhiteboardView.Selection.cs` des WPF-Kopfs; der Linux-Kopf hätte es Zeile für Zeile
abschreiben müssen, und **zwei Fassungen derselben Formel driften auseinander, ohne dass es
auffällt** — die Auswahl säße dann je Kopf ein paar Pixel anders, und niemand hätte einen
Anhaltspunkt, welche richtig liegt.

**Der WPF-Kopf ist bewusst nicht umgestellt**, aus demselben Grund wie bei der Farbtabelle
(§4.9): er lässt sich hier nicht bauen, und ein Umbau ohne Gegenprobe am laufenden Programm
wäre genau die Art Änderung, vor der §7 warnt. **Damit steht dieselbe Geometrie an zwei
Stellen** — das ist eine Schuld, kein Zustand; sie gehört auf dem Windows-Rechner
zusammengelegt. Neuer Wächter: `TrefferTests` (15 Tests).

#### Die Linux-Werkzeuge — die Lücke, die §5b nicht kannte

Die drei Skripte unter `tools/` sind Windows-PowerShell und haben unter Linux **kein**
Gegenstück. Ohne sie lässt sich „am laufenden Programm gegengeprüft" auf dem Laptop nicht
belegen, sondern nur behaupten — und Dauerregel 1 und 4 hängen daran. Neu in `tools/linux/`:

| | |
|---|---|
| `schau.sh` | Kopf mit `--db` starten und fotografieren |
| `klick.sh` | Schritte abarbeiten, danach fotografieren |
| `zeiger/` | ein kleines `net10.0`-Werkzeug, das über **X11/XTEST** klickt und tippt |

**Warum `zeiger` selbst gebaut ist und nicht `xdotool` aufruft:** `xdotool` ist ein Paket,
das erst installiert werden müsste, und `sudo` braucht auf diesem Laptop ein Passwort. Die
zwei Bibliotheken, die `xdotool` selbst benutzt — `libX11` und `libXtst` —, liegen dagegen
ohnehin auf jedem X-System. Das Werkzeug läuft damit sofort und überall, und es ist kürzer
als die Anleitung, wie man das fehlende Paket nachinstalliert. **Was es nicht kann: den
Stift.** XTEST erzeugt Maus- und Tastaturereignisse; Druck, Neigung und die Unterscheidung
Stift/Finger entstehen im Digitizer. Alles, was am Stift hängt, bleibt Handarbeit — dafür
gibt es F9.

Die drei Eigenheiten, über die dabei jeder stolpert, stehen in §7 („Fernsteuern unter
Wayland").

#### Am laufenden Programm geprüft (Dauerregel 1 und 4)

**Ohne echte Daten** — Nutzer-Entscheidung 2026-08-03: für den Eingabepfad ist ein selbst
vollgeschriebenes Notizbuch die bessere Prüfung, und die Schulunterlagen bleiben, wo sie
sind. Gegen `/tmp/gonk-probe.sqlite`, in hellem **und** dunklem Theme:

- **Cover-Seite:** Farbverlauf, Titeltext, Akzentlinie, Untertitel — alles über `WbRenderer`
  auf der geliehenen Leinwand.
- **Notizbuchseite:** A4 mit Linienraster, Seitenzähler „Seite 1 / 1", Blättern zum Cover.
- **Zeichnen:** mehrere Striche, die Stift-Anzeige liest dabei
  `Druck 0,5000 (Rückfall: feste Breite)` bei Zeigerart `Mouse` — **der Rückfall greift
  nachweislich**, und der Strich hat entsprechend gleichmäßige Breite.
- **Radieren:** ein Strich quer über einen anderen trennt ihn auf, die Reststücke bleiben
  stehen (`WbErase.SplitStroke` aus Core), der Radierring folgt dem Zeiger.
- **Rückgängig:** `Strg+Z` setzt den aufgetrennten Strich wieder zusammen.
- **Theme-Wechsel hell↔dunkel:** Rahmen und Leinwandumfeld wechseln mit, **das Papier bleibt
  hell** — die V1-Vorgabe „Dark/Light bei hellem Papier" (§1).
- **Speichern:** nach der Autospeicherung die App beendet und neu gestartet — Striche,
  Seiten und der Theme-Zustand sind unverändert wieder da.

**Dabei drei Fehler gefunden und behoben**, alle drei in §7 beschrieben: der Zugriff auf ein
fremdes Steuerelement mitten im Renderdurchlauf, der Tastaturfokus auf dem Rahmen statt auf
der Fläche, und die Vorgabetinte, die dem App-Theme statt dem Papier folgte.

> **Der Stift selbst ist danach vom Nutzer am Gerät geprüft worden — alles bestanden.**
> Druck, Neigung und Handballenabweisung, gemessen mit F9 in der laufenden App. Das ist der
> Teil, den kein Skript liefern kann (XTEST erzeugt keine Stiftereignisse); Einzelheiten
> stehen in §5a unter „Gegenprobe in der echten App".

---

### 4.11 Die Neigung wandert ins Dateiformat

Umgesetzt am 2026-08-03 direkt im Anschluss an §4.10, **auf dem Laptop**
(Nutzer-Entscheidung: „einbauen, außer es gibt einen klaren Grund, es nicht hier zu machen").
Den Grund gab es nicht — die Begründung dafür steht unten unter „Warum das hier gehen darf".

#### Was sich geändert hat

| | |
|---|---|
| `WbPoint` | zwei neue Felder **`TX`/`TY`** — Neigung in Grad, −90…+90 |
| `WbRenderer` | **nur der Bleistift** wird durch Neigung breiter (`TiltWidthFactor`) |
| Eingabepfad | schreibt die gemessene Neigung in jeden Punkt, geglättet wie der Druck |

**`0` heißt „senkrecht" — und zugleich „nicht bekannt".** Eine Maus, ein Finger und ein
Digitizer ohne Neigungsachse liefern alle 0, und der Renderer behandelt sie wie einen
senkrecht gehaltenen Stift. Das ist kein Verlust, sondern der Normalfall. **Beim Druck geht
das ausdrücklich nicht** (§4.10): dessen Rückfallwert ist 0,5 und damit von einem echten
Messwert nicht zu unterscheiden — deshalb braucht der Druck eine Erkennung und die Neigung
keine.

#### Warum nur der Bleistift

Eine schräg gehaltene Mine legt sich um und zieht eine breitere, weichere Spur — das ist der
Grund, warum man beim Schraffieren den Stift kippt. Ein Fineliner hat eine feste Spitze und
wird davon nicht breiter. Ein Textmarker hat eine Keilspitze, deren Verhalten von der
**Drehung um die eigene Achse** abhinge, und die liefert kein Digitizer. Beides nachzuahmen
wäre erfunden, nicht beobachtet. Wächter: `Nur_der_Bleistift_reagiert_auf_Neigung`.

Gerechnet wird mit der **mittleren** Neigung des Strichs. Die Körnung des Bleistifts entsteht
aus drei Durchgängen über einen gemeinsamen Pfad; je Segment eine eigene Breite hieße, den
Pfad je Segment neu zu bauen. Ein Strich wird ohnehin selten in der Mitte umgegriffen.

#### Warum das hier gehen darf — und keine Windows-Gegenprobe braucht

Das ist eine Änderung am **gespeicherten Format** und am **gemeinsamen Renderer**, also
genau die Sorte, vor der §7 warnt. Sie ist trotzdem unbedenklich, und zwar aus drei Gründen,
die alle unter Test stehen:

1. **Bestandsdateien bleiben byteweise gleich.** Die Felder tragen
   `[JsonIgnore(WhenWritingDefault)]` und werden nicht geschrieben, solange sie 0 sind. Ein
   Dokument ohne Neigung sieht in der Datenbank aus wie vorher; ein Dokument von vor der
   Änderung liest sich mit 0 ein. Das zählt hier mehr als anderswo: `WbPoint` ist der mit
   Abstand häufigste Datensatz der App — 6308 Druckpunkte auf 160 Striche in der echten
   Datenbank (§4.8). Zwei bedingungslos geschriebene Felder je Punkt wären ein knappes
   Drittel mehr Datei für einen Wert, den die meisten Geräte nicht liefern.
2. **Kein Pixel ändert sich ohne Neigungsangabe.** Bei `TX`/`TY` gleich 0 ist der
   Breitenfaktor **exakt 1** — nicht „ungefähr 1" — und jede Rechnung darunter eine
   Multiplikation mit Eins. Die zwanzig Pixelhashes aus Phase 1 sind unverändert grün, ohne
   dass ein einziges Golden-File neu gesetzt werden musste. **Damit zeichnet auch der
   WPF-Kopf Bestandsdokumente unverändert** — er benutzt denselben Renderer und wurde für
   diese Änderung nicht angefasst.
3. **`_type` ist nicht betroffen.** `WbPoint` ist kein polymorpher Typ; die Zeichenketten
   aus §7 („Persistenz") bleiben, wie sie sind.

**Neue Wächter:** `NeigungTests` (10 Tests), dazu Neigung im `Beispieldokument` und im
feldweisen Vergleich von `DatenbankRoundtripTests`. Die Neigung im Beispieldokument steht
bewusst am **Stift** und nicht am Bleistift: nur der Bleistift wertet sie aus, und ein
geneigter Bleistift dort verschöbe den Pixelhash `bleistift-koernung`.

> **Am Gerät bestätigt** (2026-08-03, vom Nutzer): ein mit gekipptem Stift gezogener
> Bleistift-Strich ist sichtbar breiter als ein senkrecht gezogener, und F9 nennt dazu
> Grad-Zahlen ≠ 0. Das war der einzige Teil dieser Änderung, der sich nicht automatisiert
> belegen ließ — XTEST erzeugt keine Stiftereignisse (§7 „Fernsteuern unter Wayland").

---

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
| Wie die ViewModels an den Kopf kommen | **Ein Bündel `IPlatformServices` im Konstruktor**, kein Service-Locator und keine zwölf Argumente (§4.7). Entschieden 2026-07-31 |
| Farben und Bilder in den ViewModels | **Als Hex-Text und Bytes**, Pinsel und Bitmaps baut der Kopf über Konverter (§4.7). Entschieden 2026-07-31 |
| Name der SQLite-Datei | **`gonknote.sqlite`** — Stamm bleibt `gonknote`, sonst wandert der Blob-Ordner (§4.8). Entschieden 2026-08-02 |
| Wann migriert wird | **Automatisch und still beim ersten Start** (§4.8). Entschieden 2026-08-02 |
| Was mit `gonknote.db` passiert | **Unangetastet liegen lassen** — nie beschrieben, nie umbenannt, nie gelöscht (§4.8). Entschieden 2026-08-02 |
| SQLite-Schema | **Ein JSON-Dokument je Zeile**, kein relationales Schema für Seiten und Elemente (§4.8). Entschieden 2026-08-02 |
| Wo Phase 3 entwickelt wird | **Unter Windows**, nicht auf dem Laptop — `Avalonia.Desktop` läuft dort auch, und nur so lassen sich beide Köpfe nebeneinander vergleichen und die Werkzeuge in `tools\` benutzen (§5b). Entschieden 2026-08-03 |
| Avalonia-Fassung | **12.1.1** — genau die, mit der der Stylus-Prototyp gemessen hat (§5a). Damit ist die offene Frage aus §4.3 beantwortet: Avalonia 12 trägt `net10.0`. Entschieden 2026-08-03 |
| Woher die Avalonia-Farben kommen | **Aus einer Farbtabelle in Core, alle zwanzig — das gezeichnete Blatt inbegriffen** (§4.9). Damit ist auch die erste der drei Fragen aus §6 beantwortet. Entschieden 2026-08-03 |
| Versionsnummer | **0.3.0**, weil der Persistenz-Umbau das Dateiformat betrifft; `About.Version` in beiden Tabellen auf Phase 3 nachgezogen. Entschieden 2026-08-03 |
| Wie der Renderer an seinen `SKCanvas` kommt | **Avalonias eigenen ausleihen** (`ISkiaSharpApiLeaseFeature`) statt eine Zwischenfläche zu rastern — `Avalonia.Skia` hängt an derselben SkiaSharp-Fassung wie Core, ein offizielles `SkiaSharp.Views.Avalonia` gibt es nicht (§4.10). Entschieden 2026-08-03 |
| Testdaten auf dem Laptop | **Keine Kopie der echten Datenbank.** Selbst angelegte Notizbücher sind für den Eingabepfad die bessere Prüfung, und die Schulunterlagen bleiben auf dem Windows-Rechner. Dauerregel 4 erlaubt die Kopie weiterhin — sie wurde hier nur nicht gebraucht. Entschieden 2026-08-03 |
| Linux-Fernsteuer-Werkzeuge | **Ja, minimal** — `schau.sh`, `klick.sh` und ein eigenes `zeiger` über X11/XTEST, ohne Fremdpaket (§4.10). Der Stift bleibt dabei Handarbeit. Entschieden 2026-08-03 |
| Neigung im Dateiformat | **Ja, und zwar auf dem Laptop** (§4.11). Zwei Felder an `WbPoint`, bedingt geschrieben; nur der Bleistift wertet sie aus. Bestandsdateien und alle zwanzig Pixelhashes bleiben unverändert — deshalb war keine Windows-Gegenprobe nötig. Entschieden 2026-08-03 |

**Noch offen:**

1. **Zweites Stylus-Gerät** (MPP und/oder EMR) — der einzige Punkt mit echtem Restrisiko,
   siehe §5a „Offen". Die Anforderung „läuft mit jedem Stylus" ist bis dahin unbeantwortet.
   **Er ist mit der Zeichenfläche akut geworden.** Was sich seit §4.10 geändert hat: der
   Rückfall ohne Druck ist jetzt gebaut und nachweislich wirksam (die Stift-Anzeige liest
   ihn ab), und mit **F9** gibt es ein Messgerät, das die Frage an einem fremden Gerät in
   einer Minute beantwortet. **Der Punkt ist damit kleiner geworden, aber nicht erledigt.**
2. **Eigene Farbschemata** (Nutzerwunsch 2026-08-02) — vorgemerkt in §6. Die wichtigste der
   drei Fragen ist mit §4.9 beantwortet (die Tabelle umfasst auch das Papier); offen bleiben
   die beiden kleineren: Verhalten bei einer unvollständigen Datei und der Menüaufbau.
3. **Beschreiben die vier mitgelieferten Dokumente V1 oder V2?** In `ERSTE-SCHRITTE.md` und
   `GETTING-STARTED.md` steht weiterhin `git clone …/gonk-note.git` — das ist das
   **V1**-Repo. Solange V2 privat und nicht veröffentlicht ist, schadet das niemandem; vor
   dem Öffentlich-Schalten (§6) muss es entschieden werden. Framework- und
   Ausgabepfad-Angaben sind am 2026-08-02 auf `net10.0` nachgezogen worden, der Klon-Befehl
   bewusst nicht — das ist eine inhaltliche Frage, keine technische. **Am 2026-08-03 erneut
   vorgelegt und erneut zurückgestellt.**
4. **Wann beschreiben die Dokumente auch den Linux-Kopf?** Die vier mitgelieferten
   Dokumente sprechen durchgehend von Windows („Notiz-App für Windows 11", `%APPDATA%`).
   **Diese Frage ist mit §4.10 fällig geworden**, denn die Begründung von damals ist
   verbraucht: „bleibt richtig, solange der Linux-Kopf keine Zeichenfläche hat" — er hat
   jetzt eine. Zu klären ist beides zusammen:
   - **Wird M1 ausgerufen?** Notizbuch und Whiteboard zeichnen, radieren und speichern unter
     Linux; Textdokumente sind ausgegraut. Das ist der Wortlaut von M1 (§6). Offen sind
     Brocken 6 und 7 (Drag & Drop, Titelleiste, `EmbeddedDocs`) sowie Import/Export —
     nichts davon steht im M1-Satz, aber ein Meilenstein, den man erklären muss, ist keiner.
   - **Erst danach die Dokumente.** Dann sind **beide Paare** nachzuziehen (Dauerregel 1),
     und `EmbeddedDocs` braucht vorher sein Gegenstück im Avalonia-Kopf — sonst stünde die
     Linux-Beschreibung in einem Dialog, den es unter Linux nicht gibt.

5. ~~**Soll die Neigung ins Dateiformat?**~~ **Entschieden und umgesetzt am 2026-08-03**
   (§4.11): ja, mit zwei bedingt geschriebenen Feldern an `WbPoint`; nur der Bleistift
   wertet sie aus. **Offen bleibt daran nur die Gegenprobe am echten Stift** — sie gehört
   zum Punkt 1 oben und läuft im selben Handgriff mit.

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

#### Was daraus in der echten App geworden ist (§4.10)

Die drei Punkte, die dieser Abschnitt für Phase 3 vorgemerkt hatte, sind umgesetzt und am
laufenden Programm nachgewiesen:

| Vorgemerkt hier | Umgesetzt |
|---|---|
| `GetIntermediatePoints()` statt nur `GetCurrentPoint()` | ✅ im Eingabepfad |
| Rückfall ohne Druck ist Pflicht | ✅ **und erkannt statt angenommen** — Avalonia meldet für ein druckloses Gerät glatt 0,5, was von echtem Mitteldruck nicht zu unterscheiden ist |
| Pen/Touch/Mouse trennen (Voraussetzung für Handballenabweisung) | ✅ zweistufig: der Finger zeichnet nie, und solange ein Stift aufliegt, wird jede Berührung verworfen |
| `SKCanvasView` oder `DrawingContext`? — hier offen gelassen | ✅ **beantwortet: keins von beiden.** Avalonias eigener `SKCanvas` wird ausgeliehen (§4.10) |

**Neu als Messgerät: die Stift-Anzeige mit F9** in der Zeichenfläche. Dieser Abschnitt hat
im *Prototyp* gemessen; F9 misst in der *App*, also durch den fertigen Eingabepfad hindurch.
Für ein zweites Gerät ist das der Weg, der eine Minute dauert statt eines Nachmittags.

#### Gegenprobe in der echten App (2026-08-03, vom Nutzer am Gerät)

**Alles bestanden.** Damit ist die Kette vom Digitizer bis zum gezeichneten Pixel zum ersten
Mal durchgehend belegt — bisher endete der Nachweis am Prototyp:

| Geprüft | Ergebnis |
|---|---|
| **Druck** — leicht und fest aufdrücken, F9 mitlesen | Zeigerart `Pen`, Anzeige meldet `(Gerät liefert Druck)`, der Strich wird beim Aufdrücken dicker |
| **Neigung** — Bleistift senkrecht gegen stark gekippt | Grad-Zahlen ≠ 0, der gekippte Strich ist sichtbar breiter (§4.11) |
| **Handballenabweisung** — Hand beim Schreiben aufs Display | kein Strich vom Handballen, das Blatt verrutscht nicht |

**Was das ausräumt:** die drei Punkte, die §4.10 und §4.11 nur konstruktiv absichern konnten,
sind jetzt am Gerät bestätigt. Insbesondere ist die Druckerkennung **nicht** nur im
Rückfallzweig geprüft — die automatisierten Belege konnten das nicht leisten, weil XTEST
keine Stiftereignisse erzeugt.

**Was das nicht ausräumt:** es ist weiterhin **eine** Geräteklasse — der Wacom-AES-Digitizer
dieses Laptops. MPP und EMR bleiben ungetestet, siehe „Offen" unten.

#### Offen

1. **Zweites Gerät** (MPP und/oder EMR) — die Kernanforderung „mit jedem Stylus" hängt daran.
   Das ist der einzige Punkt, der noch echtes Risiko trägt. **Der Rückfall dafür steht
   inzwischen und ist wirksam**, und das Gerät dieses Laptops ist in der App durchgeprüft
   (oben); ungeprüft ist, ob ein **anderes** Gerät überhaupt als `PointerType.Pen` ankommt
   und was es an Druck liefert. Mit F9 in einer Minute zu klären, sobald eines greifbar ist.
2. **Xorg-Sitzung** als Vergleich zu XWayland. Nach derzeitigem Stand Absicherung, keine offene
   Risikofrage — Avalonia hat ohnehin nur den X11-Pfad.
3. **Druckschwelle unten:** evdev meldete nie unter 1500 von 4095, libinput nie unter 0,01.
   Ob der Digitizer eine hohe Einsatzschwelle hat oder nur nie leicht genug aufgesetzt wurde,
   ist offen — relevant dafür, wie sich ganz feine Striche später anfühlen.

---

## 5b. Wann und wie auf den CachyOS-Laptop wechseln

**Kurz: noch nicht umziehen — auch nicht in Phase 3.** Entwickelt wird unter Windows; der
Laptop ist Pflicht für alles, was am **Stift** und an **Linux-Pfaden** hängt.

> **Der zweite Brocken von Phase 3 lief trotzdem hier** (2026-08-03, §4.10) — und das war
> richtig so: er hing an allen fünf Punkten, die der Kasten unten dem Laptop zuschreibt.
> Die Regel bleibt aber, wie sie steht. Was der Laptop **nicht** kann, hat sich dabei
> gezeigt: die Fernsteuer-Werkzeuge gab es hier gar nicht (jetzt in `tools/linux/`, §4.10),
> Vollbildaufnahmen sind unter Wayland unbrauchbar, und beide Köpfe nebeneinander an
> derselben Datenbank zu vergleichen geht hier grundsätzlich nicht.

> **Diese Antwort hat sich am 2026-08-03 geändert.** Bis dahin stand hier „ab Phase 3 wird
> der Laptop der Hauptarbeitsplatz". Das war geschrieben, bevor klar war, dass
> **`Avalonia.Desktop` auch unter Windows läuft** — derselbe Kopf, dieselbe `net10.0`-Datei,
> nur ein anderes Backend. Unter Windows entwickeln hat zwei Vorteile, die schwer wiegen:
> die Fernsteuer-Werkzeuge in `tools\` funktionieren (und ohne sie lässt sich Dauerregel 4
> kaum einhalten), und **WPF- und Avalonia-Kopf lassen sich an derselben Datenbank-Kopie
> direkt nebeneinander vergleichen**. Genau das hat in §4.9 die Unterschiede gefunden.
>
> **Was der Laptop trotzdem beantworten muss** — und nur er:
> Druck und Neigung des Stifts, Handballenabweisung, `~/.config/GonkNote` als Datenordner,
> `SkiaSharp.NativeAssets.Linux` samt fontconfig zur Laufzeit, und wie die
> Rückfallschrift des Renderers dort wirklich aussieht. **Der nächste Brocken
> (Zeichenfläche) hängt an allen fünf Punkten.**

Der Laptop hat daneben die Aufgaben aus dem Stylus-Prototyp.

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

**Seit Phase 3 gibt es dort auch etwas zu starten:**

```bash
sudo pacman -S fontconfig ttf-dejavu                  # falls noch nicht geschehen
dotnet build src/GonkNote.Avalonia                    # muss durchlaufen
dotnet run --project src/GonkNote.Avalonia -- --db ~/gonk-probe.sqlite
```

**Seit dem zweiten Brocken gibt es hier auch Werkzeuge** (§4.10). Sie brauchen einmalig
`imagemagick` und einen Build des kleinen X11-Helfers — sonst nichts, insbesondere **kein**
`xdotool`:

```bash
sudo pacman -S imagemagick                            # falls noch nicht da
dotnet build tools/linux/zeiger -c Release            # einmalig

tools/linux/schau.sh --db /tmp/gonk-probe.sqlite      # starten und fotografieren
tools/linux/klick.sh w:120,191 '#Return' 'w:z:800,450>1600,400'
tools/linux/zeiger/bin/Release/net10.0/zeiger fenster # Kennung, Lage, Groesse
```

Schritte sind `x,y` (Bildschirm), `w:x,y` (fensterrelativ), `x,y,2`/`x,y,r`,
`w:z:x1,y1>x2,y2>…` (ziehen), `#Tasten`, `:Text`, `warte:500`. **Die Fallstricke stehen in
§7 „Fernsteuern unter Wayland" — ohne sie kommt man hier nicht weit.**

**Nie ohne `--db` starten, solange geprüft wird.** Der Datenordner ist unter Linux
`~/.config/GonkNote` — dort liegen (noch) keine Bestandsdaten, aber die Regel ist dieselbe
wie unter Windows (Dauerregel 4).

`GonkNote.Wpf` lässt sich dort **nicht** bauen (`net10.0-windows10.0.19041.0`) — das ist so gewollt und
kein Fehler. Die Solution als Ganzes deshalb unter Linux nicht anfassen, sondern
projektbezogen bauen, genau wie die CI es tut.

Der Wechsel kostet nichts weiter als `git pull` — genau dafür ist der Remote da.

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

### Erledigt: Phase 2 — die große Entkopplung

Lief unter Windows/WPF. **Nach jedem Schritt musste die App noch starten** — hat sie.

- [x] 1. `Core/Platform/`-Interfaces einziehen, WPF-Implementierungen dahinterhängen
      (§4.7). Es sind **zwölf** geworden statt der acht aus der Roadmap — `IDialogService`,
      `IShell`, `IUiScheduler` und `IDocumentIo` kamen dazu, weil das `MainViewModel` sie
      braucht. (`TitleBarTheme`, `WindowBounds` bleiben ersatzlos Windows-only.)
- [x] 2. `GonkNote.ViewModels` freischneiden und zur eigenen Assembly machen (§4.2)
- [x] 3. **LiteDB → `Microsoft.Data.Sqlite`** (§4.8, 2026-08-02). Migration additiv, die
      alte Datei bleibt unversehrt liegen; an einer Kopie der echten Datenbank feldweise
      gegengeprüft. LiteDB lebt nur noch in `src/GonkNote.Legacy/`
- [x] 4. `BlobStore` von der Ableitung aus dem DB-Dateinamen auf `IAppPaths` umstellen
      (§4.8) — `AppPaths.BlobFolder` + `BlobStore.InFolder(...)`. Der alte Konstruktor
      bleibt für `--db`, sonst bräche §8

**Das Netz aus Phase 1 hat getragen.** `DatenbankRoundtripTests`, `AlteTypnamenTests` und
`BlobSpeicherTests` prüfen die **öffentliche** API des `DatabaseService` — die ist beim Umbau
gleich geblieben, und die Tests sind unverändert grün. `AlteTypnamenTests` ist vom
Roundtrip- zum **Migrations**-Wächter geworden (drei neue Tests, §4.8).

**Meilenstein M0 ist vollständig:** `Core`, `ViewModels` **und** `Legacy` bauen auf Linux
(alle `net10.0`), Windows verhält sich unverändert.

### Läuft: Phase 3 — Avalonia-Shell für Linux

Erster Brocken umgesetzt am 2026-08-03 unter Windows (§4.9, §5b).

- [x] 1. `src/GonkNote.Avalonia` angelegt (`net10.0`, Avalonia 12.1.1), in der `.slnx`,
      im Linux-Lauf der CI
- [x] 2. `AvaloniaPlatformServices` — alle **zwölf** Schnittstellen aus `Core/Platform/`.
      Drei davon sind die vorhandenen Rückfälle (`NoOcrEngine`,
      `AlwaysSupportedSpellChecker`, leeres `IDocumentIo`) und ausdrücklich so benannt
- [x] 3. **Farbtabelle in Core** (`Core/Theming/`) statt eines zweiten Paars fest
      verdrahteter Theme-Dateien — die Entscheidung, die laut §6 jetzt fallen musste.
      Wächter `FarbtabelleTests` hält beide Fassungen zusammen
- [x] 4. Start mit `--db` **und `ILegacyDatabaseReader`**; Ordnerbaum und Galerie aus
      demselben `MainViewModel` wie der WPF-Kopf, kein zweites
- [x] 5. **Zeichenfläche** — Notizbuch und Whiteboard, umgesetzt am 2026-08-03 **auf dem
      Laptop** (§4.10). Der Renderer bekommt Avalonias eigenen `SKCanvas`
      (`ISkiaSharpApiLeaseFeature`); damit ist die offene Frage aus §5a beantwortet.
      Eingabepfad mit `GetIntermediatePoints()`, erkanntem Druck samt Rückfall und
      Handballenabweisung. Radieren über `WbErase`, Trefferprüfung als neues `WbHit` in Core
- [ ] 6. Drag & Drop im Baum, einblendbare Titelleiste, Einstellungen-Seitenleiste
- [ ] 7. `EmbeddedDocs`-Gegenstück, damit „Hilfe → Erste Schritte" und das gerenderte
      README auch im Linux-Kopf erscheinen (hängt an §4.1)

**Was Brocken 5 bewusst ausgelassen hat** — nicht vergessen, sondern nicht M1: Text-,
Formen- und Notizzettel-**Werkzeug** (angezeigt werden diese Elemente, nur anlegen kann man
sie nicht), Drehen und Skalieren der Auswahl, Seiteneinstellungen, Bild- und PDF-Import.
Sticker, Texterkennung, Zahlenblock, Schnellaktionen und Geodreieck gehören ohnehin nicht
dazu.

> **Für Brocken 6 und 7 vorher lesen:** §4.10 (was die Zeichenfläche kann und wie sie
> gebaut ist), §7 „Der Avalonia-Kopf" — dort stehen jetzt sieben Eigenheiten statt vier —
> und §7 „Fernsteuern unter Wayland", ohne das sich auf dem Laptop nichts belegen lässt.

### Der Rest (Roadmap §5)

| Phase | Inhalt | Aufwand | Ziel |
|---|---|---|---|
| 3 | Avalonia-Shell für Linux — *Shell steht, Zeichenfläche offen* | 6–8 W. | **M1** — Notizbuch + Whiteboard laufen unter Linux, Textdokumente ausgegraut |
| 4 | Eigene Dokument-Engine in `Core/Text/` | 8–12 W. | **M2** — Funktionsgleichheit Linux ↔ Windows |
| 5 | iPadOS-Head, Apple Pencil, PDFKit/Vision, AOT-Härtung | 6–10 W. | **M3** — TestFlight-Build |
| 6 | Flatpak/AppImage, App Store | 2–4 W. | Veröffentlichung |

> **M1 ist ein gültiger Ausstiegspunkt.** Phase 4 ist die, an der Projekte sterben — dort
> strikt in der Reihenfolge Absätze/Zeichenformate → Seitenumbruch → Listen → Tabellen →
> Felder/TOC → Diagramme bauen, nach **jedem** Schritt Roundtrip-Test.

### Vorgemerkt: eigene Farbschemata (Nutzerwunsch 2026-08-02)

**Gewünscht:** eigene Themes anlegen und über **Ansicht → Design** laden.

**Machbar, und kleiner als es klingt.** Ein Theme ist heute nichts als **20 flache
Hex-Farben** — 15 `SolidColorBrush` plus 5 rohe `Color` (`src/GonkNote.Wpf/Themes/Light.xaml`).
Keine Verläufe, keine Struktur, keine Logik. Das ist eine Datendatei, kein Programm.

**Aber ausdrücklich nicht als XAML-Upload.** Eine `.xaml` zur Laufzeit einzulesen ginge unter
WPF über `XamlReader.Load` — und wäre aus drei Gründen die falsche Entscheidung:

- **NativeAOT.** `XamlReader` lebt von Reflection. Für den iPad-Kopf ist AOT Pflicht (§1);
  eine Theme-Funktion, die dort nicht läuft, wäre eine Funktion, die man später wieder
  ausbaut.
- **Portierbarkeit.** Avalonia hat kein `ResourceDictionary` im WPF-Sinn. Ein XAML-Theme
  wäre ein Windows-Theme und müsste für jeden Kopf neu erfunden werden.
- **Sicherheit.** XAML kann Typen erzeugen. Eine „Theme-Datei" aus dem Internet wäre damit
  ausführbarer Code — bei einer Datei, die Nutzer untereinander weitergeben, ist das der
  falsche Vertrag.

**Der tragfähige Zuschnitt** — er passt auf ein Muster, das die App schon hat:

- Ein Theme ist eine **JSON-Datei mit 20 benannten Farben**, gelesen über
  `System.Text.Json` mit Source-Generator (AOT-tauglich, wie in Phase 2 Schritt 3).
- Sie liegt in **`%APPDATA%\GonkNote\Themes\*.json`** — dieselbe Stelle und dieselbe Regel
  wie Sticker, Cover-Vorlagen und die eigenen Geodreieck-SVGs: die Datei des Nutzers
  gewinnt, die mitgelieferte ist der Rückfall. Über `IAppPaths.DataFolder` (steht seit
  Phase 2).
- Core hält die Farbtabelle und prüft sie; **jeder Kopf übersetzt sie in seine eigenen
  Pinsel.** Hell und Dunkel werden dabei zu zwei mitgelieferten Tabellen statt zu zwei
  fest verdrahteten Dictionaries.
- **`IThemeHost` (§4.7) ist die Naht, die dafür schon da ist.** Aus `Apply(AppTheme)` wird
  `Apply(ThemeDefinition)`; `AppTheme` bleibt als „hell oder dunkel"-Auskunft bestehen, denn
  `WbRenderer` und die Titelleiste brauchen sie weiter.
- „Hochladen" heißt: Datei wählen (`IFileDialog`), prüfen, in den Theme-Ordner **kopieren** —
  genau wie `WhiteboardView.Covers` es mit eigenen Cover-Vorlagen macht.

**Wann:** **frühestens nach Phase 3 (M1), nicht davor.** Nicht weil es schwer wäre, sondern
wegen der Reihenfolge — heute gäbe es eine WPF-Fassung, die Phase 3 sofort noch einmal bauen
müsste.

> **✅ Die eine Entscheidung, die jetzt fallen musste, ist gefallen** (2026-08-03, §4.9):
> die Avalonia-Farben kommen **aus einer Farbtabelle** (`Core/Theming/`) und nicht als
> zweites Paar fest verdrahteter Dateien. Der teure Nachbau ist damit vermieden — was noch
> fehlt, ist das **Laden** einer Datei, und das ist jetzt wirklich nur noch eine Zutat.

**Vor der Umsetzung zu klären:**

1. ~~**Reicht die Chrome, oder auch das Papier?**~~ **Entschieden am 2026-08-03: auch das
   Papier.** Die Tabelle in Core umfasst alle zwanzig Farben, `CanvasBg`, `PageBg`,
   `PageLine`, `PageGridDot` und `DefaultInk` inbegriffen. Ein Theme *kann* damit das
   Aussehen von Notizbüchern ändern — ob ein einzelnes es *tut*, ist danach eine reine
   Datenfrage. **Das heißt auch: es kann den Export verändern.** Wer eigene Themes
   ausliefert, sollte das im Blick behalten.
2. **Was passiert bei einer unvollständigen Datei?** Vorschlag: fehlende Schlüssel still aus
   Hell/Dunkel ergänzen, statt die Datei abzulehnen — dann genügt eine Datei mit drei Farben.
   **Der Mechanismus steht bereits**: `ThemeDefinition.Over(...)`, Wächter
   `Eine_unvollstaendige_Tabelle_wird_still_ergaenzt`. Offen ist nur, ob es so gewollt ist.
3. **Menü:** „Ansicht → Design wechseln" wird zu einem Untermenü (Hell / Dunkel / eigene /
   „Eigenes laden…"). Neue `Loc`-Schlüssel in **beiden** Tabellen (Dauerregel 1).
4. **Neu:** Ein geladenes Theme muss sagen, ob es **hell oder dunkel** ist
   (`ThemeDefinition.Variant`). Das ist keine Farbe, sondern eine Auskunft — `WbRenderer`
   und die Titelleiste unter Windows brauchen sie. In der JSON-Datei also ein eigenes Feld,
   und kein Ratespiel über die Helligkeit von `PageBg`.

**Nicht vergessen:** Das ist ein **neuer Wunsch**, keine Vorgabe aus
`gonk-note-port-RM.MD`. Wer die Roadmap-Datei auf dem Desktop pflegt, sollte ihn dort
nachtragen — sonst steht er nur hier.

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

- **Models nie umziehen, ohne an `_type` zu denken.** Je Whiteboard-Element steht
  „Namensraum.Typ, Assembly" in der Datei. Ändert sich eines von beiden, lässt sich **kein**
  Bestandsdokument mehr öffnen. Seit Phase 2 stehen diese Zeichenketten als
  `[JsonDerivedType]` an `WbElement` — **sie sind Datenformat, kein Codedetail.** Wer den
  Namensraum von `GonkNote.Core.Models` oder den Assemblynamen ändert, zieht sie **nicht**
  mit. Die Übersetzung noch älterer Namen liegt in `ModelTypeBinder` in
  `src/GonkNote.Legacy/` und wird nur bei der einmaligen Übertragung gebraucht.
  Wächter: `AlteTypnamenTests`.
- **Ein neuer Elementtyp braucht drei Einträge, nicht einen.** `[JsonDerivedType]` an
  `WbElement`, ein Zweig in `DatenbankRoundtripTests.GleichesElement` (sonst bewacht kein
  Test seine Felder — der `default`-Zweig sagt das ausdrücklich) und ein Exemplar in
  `Beispieldokument`. Fehlt der erste, **wirft das Speichern**; das ist Absicht und besser
  als ein still verschwundenes Element.
- **`EmptyStringToNull` war bei LiteDB standardmäßig `true`** → leere Strings kamen als
  `null` zurück. `System.Text.Json` macht aus `""` nie `null`, der Fall ist damit erledigt.
  **Die null-sicheren Setter in `TextDoc` bleiben trotzdem stehen:** eine *migrierte*
  Altdatei kann `null` mitbringen. Wächter:
  `Leerer_String_kommt_leer_zurueck_und_nicht_als_null`.

**Neu aus Phase 2 — SQLite (§4.8)**

- **Der Dateiname der Datenbank bestimmt den Blob-Ordner.** `BlobStore` schneidet mit
  `Path.GetFileNameWithoutExtension` ab: `gonknote.sqlite` → `gonknote.blobs`. Nur die
  **Endung** zu ändern ist folgenlos, **den Stamm zu ändern wäre fatal** — alle Bilder wären
  scheinbar weg, und man sucht den Fehler im Renderer statt im Pfad. Genau deshalb heißt die
  neue Datei `gonknote.sqlite` und nicht `gonknote-v2.db`.
- **Kein WAL.** `PRAGMA journal_mode=wal` legt zwei Nebendateien an (`-wal`, `-shm`). Wer die
  Datenbank dann kopiert, ohne sie mitzunehmen, verliert die zuletzt geschriebenen
  Änderungen — und genau das tut §8 beim Gegenprüfen mit echten Daten. Eine Datei bleibt eine
  Datei. Wer WAL doch einschaltet, muss §8 und die Sicherungsanleitung in beiden
  Erste-Schritte-Fassungen mitziehen.
- **`Pooling = false` in der Verbindungszeichenkette ist Pflicht, nicht Geschmack.**
  `Microsoft.Data.Sqlite` hält die Datei sonst nach `Dispose` weiter offen: der Wegwerf-Ordner
  eines Tests ließe sich nicht löschen, und das `File.Move` am Ende der Migration schlüge fehl.
  Der Fehler sieht dabei nicht nach „Datei gesperrt" aus, sondern nach einem sporadisch roten
  Testlauf.
- **Eine Migration erkennt man an der Kopfkennung, nicht an der Endung.** `gonknote.db` sagt
  nichts darüber, was in der Datei steht — die ersten sechzehn Bytes (`SQLite format 3`)
  schon. Deshalb funktioniert `--db …\gonknote.db` aus §8 unverändert weiter: die Kopie wird
  erkannt, daneben entsteht `gonknote.sqlite`, und beide teilen sich `gonknote.blobs`.
- **Bei der Übertragung nicht `UpsertItem` benutzen.** Es setzt `ModifiedUtc` auf jetzt und
  datierte damit jeden Eintrag des Nutzers auf den Tag der Migration um. Dafür gibt es die
  private `SchreibeItem`.
- **Ein Kopf, der den `ILegacyDatabaseReader` vergisst, legt keine leere Datenbank an** — er
  bekommt eine `InvalidOperationException`. Das ist Absicht: eine leere Datenbank neben
  vollen Bestandsdaten ist für den Nutzer von Datenverlust nicht zu unterscheiden. Wächter:
  `Ohne_Leser_wird_nicht_stillschweigend_neu_angefangen`. **Seit Phase 3 übergibt ihn auch
  der Avalonia-Kopf** (`App.OnFrameworkInitializationCompleted`) — der Compiler hätte es
  nicht angemahnt, es ist ein *optionaler* Parameter. Für den iPadOS-Kopf gilt das
  ausdrücklich **nicht**: dort gab es nie eine LiteDB-Datei.
- **Ein Paket kann eine Sicherheitslücke mitziehen, die man nicht selbst anheben kann.**
  `Microsoft.Data.Sqlite` 10.0.10 bringt `SQLitePCLRaw…lib.e_sqlite3` **2.1.11** mit — mit
  einem bekannten Fund (NU1903). Behoben über
  `CentralPackageTransitivePinningEnabled` in `Directory.Packages.props` plus einer
  `PackageVersion`-Zeile auf 2.1.12. **Beim nächsten Anheben von `Microsoft.Data.Sqlite`
  prüfen, ob die Zeile noch nötig ist** — ein Pin, den niemand mehr braucht, hält irgendwann
  eine Fassung fest.
- **`System.Text.Json` schreibt auch nur lesbare Eigenschaften.** `WbPage.HasBackgroundImage`,
  `WbPage.IsInfinite` und `NoteItem.IsFolder` tragen deshalb `[JsonIgnore]` — sonst stünde ein
  abgeleiteter Wert mit in der Datei und sähe später wie gespeicherte Wahrheit aus.
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
  **Seit Phase 3 gibt es `TExtension` zweimal** — einmal je Kopf, mit demselben Namensraum,
  weil `MarkupExtension` und `Binding` in WPF und Avalonia gleich heißen und verschiedene
  Typen sind. Genau dafür wurde die Klasse in Phase 0 aus `Loc.cs` herausgetrennt.
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

**Neu aus Phase 2 — Übersetzung**

- **Eine gebundene Eigenschaft, deren Text aus `Loc` kommt, braucht `Loc.LanguageChanged`
  — auch wenn sie schon aus einem anderen Grund benachrichtigt.** `PinMenuHeader` und
  `FavoriteMenuHeader` meldeten ihre Änderung nur beim Umschalten des Zustands
  (`RefreshPinFavorite`). Nach dem Sprachwechsel blieben genau diese zwei Einträge deutsch
  stehen, während das ganze Menü drumherum englisch wurde. Vorher fiel das nicht auf, weil
  die Texte fest verdrahtet waren — sie waren *immer* falsch, also nie auffällig.
  **Behoben:** `MainViewModel.RefreshLanguage` läuft jetzt über den Baum und ruft
  `RefreshPinFavorite` auf jedem Knoten.
  **Merksatz: eine Beschriftung, die sich aus zwei Gründen ändert, braucht zwei Auslöser.**
- **Ein Menü, das der Code baut, hat kein `{loc:T …}` — und niemand vermisst es.**
  `MainWindow.ShowGalleryMenu` erzeugt das Kontextmenü der Galerie-Kachel in C#. Drei
  Einträge („Öffnen", „Umbenennen", „Löschen") standen dort seit jeher fest auf Deutsch und
  erschienen auch im englischen Programm so. Aufgefallen ist es erst, als die beiden
  Nachbarn daneben übersetzt wurden. **Behoben** über die längst vorhandenen Schlüssel
  `Tree.Open` / `Tree.Rename` / `Tree.Delete`. Weil das Menü bei jedem Öffnen neu entsteht,
  greift `Loc.T` zur richtigen Zeit und braucht kein `LanguageChanged`.
  **Beim Suchen nach Übersetzungslücken reicht das XAML nicht** — `grep` nach Zeichenketten
  in `.cs` gehört dazu.
- **Listen aus `Loc.T` nicht im Konstruktor festhalten.** `WpfDocumentIo.ImportFormats` &
  Co. sind bewusst `=>`-Eigenschaften und keine `{ get; } = […]`: das Bündel
  `WpfPlatformServices` entsteht einmal beim Start, eine im Konstruktor gebaute Liste trüge
  für immer die Startsprache.

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
- **Ein Wegwerf-Test, der echte Daten anfasst, kommt nicht ins Repo.** Der feldweise
  Vergleich Alt↔Neu aus §4.8 lag als `ZzTempEchtdatenVergleich.cs` im Core-Testprojekt, hat
  seine Zahlen ausgegeben und ist danach gelöscht worden. Er hätte sonst bei jedem CI-Lauf
  nach einer Datei in `%TEMP%` gesucht, die dort nicht liegt — und beim Nutzer nach Daten,
  die niemanden etwas angehen.
- **DPI-Falle:** `SetProcessDPIAware()` als erste Zeile jedes Skripts. Der Testrechner läuft
  auf **200 %**.
- **PDF-Export prüfen, ohne sie ansehen zu müssen:** Edge headless rendert PDFs nicht (man
  bekommt nur den grauen Betrachter-Hintergrund). Der zuverlässige Weg führt über
  `PdfImporter.StreamPages` aus dem **echten** Core. **Seit Phase 1 steht das als Test**
  (`ExportFixtureTests.Whiteboard_PDF_…`) und nicht mehr als Konsolenprogramm in `%TEMP%`.

**Neu aus Phase 3 — der Avalonia-Kopf**

- **`AvaloniaXamlLoader.Load(this)` füllt die `x:Name`-Felder nicht.** Beide bauen den
  Oberflächenbaum auf, aber nur das erzeugte `InitializeComponent()` weist danach die
  Felder zu. Mit dem Lader direkt bleibt **jedes** davon `null`, und der erste Zugriff wirft
  eine `NullReferenceException` an einer Stelle, die mit der Ursache nichts zu tun hat — hier
  war es eine Zeile, die nur einen Menühaken setzt. **Merksatz: im Code-Behind immer
  `InitializeComponent()`.** (Die Vorlage von `dotnet new avalonia.app` benutzt an einer
  Stelle den Lader — das gilt für die `Application`, die keine benannten Felder hat.)
- **Avalonia hält die Quelle einer Bindung nicht am Leben — das ist die teuerste Lektion
  dieses Brockens.** Der erste Anlauf für `{loc:T …}` legte je Bindung einen Träger an und
  hielt ihn *schwach*, damit nichts festhängt. Es funktionierte — bis zum ersten
  vollständigen Sammellauf. Den erzwingt `MainViewModel.ReleaseMemory` beim Schließen jeder
  Registerkarte; danach waren alle Träger weg, und der nächste Sprachwechsel erreichte nur
  noch die Texte, die der Code selbst schreibt. **Das Fehlerbild war eine halb übersetzte
  Oberfläche** — Pfadleiste und Datumsangaben deutsch, Menüleiste englisch — und es trat
  erst beim *zweiten* Wechsel auf. Behoben über **einen Träger je Schlüssel**, stark
  gehalten (`Services/Localization/LocText.cs`): die Zahl der Übersetzungsschlüssel ist
  begrenzt, die Zahl der Bindungen nicht.
- **Übersetzung im Linux-Kopf: Avalonia frischt eine Indexer-Bindung nicht auf.** Der
  WPF-Kopf bindet auf `Loc.Source["Schlüssel"]` und bekommt beim Sprachwechsel ein
  `PropertyChanged` mit **leerem Namen** — WPF versteht das als „alle Eigenschaften neu
  lesen" und wertet auch Indexer neu aus. Avalonia tut das nicht: `Loc.Current` war
  umgestellt, der Haken im Sprachmenü sprang, und **jeder** Text blieb stehen. Deshalb
  bindet der Avalonia-`TExtension` auf eine ganz gewöhnliche Eigenschaft (`LocText.Value`)
  und nicht auf einen Indexer. **`LocSource` in Core ist bewusst unverändert geblieben** —
  die Zusage „leerer Name = alles" ist eine WPF-Zusage.
- **Der Avalonia-Kopf zeigt unter Windows auf denselben Datenordner wie der WPF-Kopf**
  (`%APPDATA%\GonkNote`). Das ist Absicht — es ist dieselbe App mit denselben Daten, und nur
  so lassen sich beide Köpfe vergleichen. **Zum Prüfen deshalb immer `--db` mit einer
  Kopie**, sonst greift ein Testlauf auf den echten Bestand zu (Dauerregel 4).
- **`Core/Platform/` ist synchron, Avalonia ist es nicht.** Jeder Dialog läuft über
  `Platform/Modal.cs` und einen verschachtelten Nachrichtenlauf (`Dispatcher.PushFrame`).
  **Nur vom Oberflächen-Faden aufrufen und nur für etwas, worauf der Nutzer ohnehin
  wartet.** Wer ihn um eine lange Rechnung legt, bekommt Wiedereintritt an einer Stelle, an
  der niemand damit rechnet.
- **`IClipboard.HasImage` kodiert im Avalonia-Kopf wirklich.** Unter Windows sieht
  `Clipboard.ContainsImage()` nur nach; Avalonia hat kein „enthält" ohne „hol es". Heute
  folgenlos (die Abfrage entscheidet über einen ausgegrauten Menüeintrag) — wer sie künftig
  in einer Schleife aufruft, sollte es wissen.
- **`ApplicationIcon` und `ApplicationManifest` sind Windows-Artefakte.** Beide stehen in
  `GonkNote.Avalonia.csproj` unter einer OS-Bedingung, damit der Linux-Lauf der CI nicht an
  einer Windows-Eigenheit hängen bleibt.
- **Die Icon-Schrift gibt es unter Linux nicht.** „Segoe Fluent Icons" gehört zu Windows und
  lässt sich nicht mitliefern. Wer im Avalonia-Kopf ein Symbol braucht, legt eine
  **Vektorform** in `Themes/Styles.axaml` an (16×16, gestrichen) — `IconGlyph` aus dem
  ViewModel ist die Antwort des *Windows*-Kopfs und bleibt dort.
- **`DrawingBrush` hat in Avalonia keine Inhaltseigenschaft.** `<DrawingBrush><GeometryDrawing/></DrawingBrush>`
  scheitert mit `AVLN2000: Internal compiler error: Index was out of range` — eine Meldung,
  die auf alles Mögliche hindeutet, nur nicht auf die Ursache. Es braucht ausdrücklich
  `<DrawingBrush.Drawing>`.
- **`Render` läuft *im* Renderdurchlauf — dort darf kein anderes Steuerelement angefasst
  werden.** Das ist die teuerste Falle des zweiten Brockens. Unter WPF läuft
  `SKElement.PaintSurface` außerhalb, und die Zeichenfläche darf von dort nebenbei die
  Farbkachel und die Zoom-Anzeige nachführen — der WPF-Kopf tut genau das. Avalonia wirft
  dafür **`InvalidOperationException: Visual was invalidated during the render pass`**, und
  zwar nicht nur für die eine Zuweisung: **der ganze Durchlauf bricht ab.**
  **Das Fehlerbild zeigt überall hin, nur nicht auf die Ursache** — hier war es eine leere
  Werkzeugleiste und ein leeres Blatt, während der Fehler in einer Zeile stand, die eine
  Farbkachel einfärbt. **Merksatz: im Zeichenpfad wird gezeichnet, sonst nichts.** Was
  aufgefrischt werden muss, hängt an dem Ereignis, das es auslöst (Seitenwechsel,
  Theme-Wechsel, Größenänderung) — und die Größe ist ohnehin das ehrlichere Ereignis für
  „jetzt kenne ich meine Breite" als das erste Bild.
- **`ICustomDrawOperation.Render` läuft auf dem Render-Faden, nicht auf dem
  Oberflächen-Faden.** Wer von dort in lebende Zustände greift (`_page.Elements`, die Punkte
  des laufenden Strichs), liest eine Liste, die der andere Faden gerade verändert. Das
  Ergebnis wäre kein sauberer Absturz, sondern ein sporadisches
  `InvalidOperationException: Collection was modified` mitten im Zeichnen — und zwar
  bevorzugt beim Zeichnen langer Striche, also genau dann, wenn niemand es reproduzieren
  will. **Gegenmittel: aufzeichnen statt zurückgreifen** (`SKPictureRecorder` auf dem
  Oberflächen-Faden, `DrawPicture` auf dem Render-Faden, §4.10).
- **Ein `UserControl` fokussierbar zu machen genügt nicht für Tastenkürzel.**
  Tastenereignisse laufen in Avalonia vom Wurzelfenster zum **fokussierten Element** und
  zurück. Liegt der Fokus noch im Ordnerbaum, kommt am Zeichenbereich nichts an, auch wenn
  der Rahmen darum `Focusable` ist und `Focus()` gerufen wurde. **Die Fläche selbst muss
  fokussierbar sein und den Fokus bekommen.** Das Fehlerbild ist unauffällig und deshalb
  teuer: gezeichnet wird einwandfrei — der Zeiger braucht keinen Fokus —, nur die
  Tastenkürzel tun nichts, und das hält man erst einmal für einen Fehler in der Tastenlogik.

**Neu aus Phase 3 — die Farbtabelle**

- **Die Reihenfolge in `ThemeColor` ist Teil des Formats.** `ThemeDefinition` legt die Werte
  in einem Feld dieser Länge ab und greift über `(int)` darauf zu. **Neue Farben nur hinten
  anhängen** — wer eine dazwischenschiebt, verschiebt alle folgenden Werte gespeicherter
  Tabellen.
- **Dieselben zwanzig Farben stehen an zwei Stellen** (Core-Tabelle und die beiden
  WPF-`ResourceDictionary`-Dateien), weil der WPF-Kopf bewusst nicht umgestellt wurde.
  `FarbtabelleTests` hält sie zusammen — **in beide Richtungen**. Wer eine Farbe ändert,
  ändert beide Stellen oder bekommt einen roten Lauf.
- **Die Vorgabetinte gehört zum Papier, nicht zur App.** Eine Notizbuchseite ist
  standardmäßig `PageShade.Light` — **unabhängig vom App-Theme**, denn Papier soll wie
  Papier aussehen (V1-Vorgabe „Dark/Light bei hellem Papier", §1). Wer `DefaultInk` aus der
  *aktiven* Tabelle nimmt, holt sich im Dunkelmodus deren helles `DefaultInk` und schreibt
  **hell auf weiß**. Genau so passiert und beim Gegenprüfen aufgefallen: der Strich ist da,
  gespeichert und exportierbar, nur unsichtbar — auf einem Bildschirmfoto sieht es aus, als
  käme die Eingabe nicht an, und man sucht den Fehler im Eingabepfad.
  **Die Regel ist dieselbe wie beim Papier selbst:** bei `PageShade.Auto` folgt die Seite
  dem Theme, also auch die Tinte; bei festgelegtem Farbton zählt der, also die mitgelieferte
  Tabelle dazu. Der WPF-Kopf umgeht das, indem er an dieser Stelle Schwarz und Weiß fest
  verdrahtet — richtig im Ergebnis, aber an der Farbtabelle vorbei.

**Neu aus Phase 2 — Fernsteuern**

- **`SetForegroundWindow` schließt jedes offene Menü.** Ein Skript, das je Klick erst das
  Fenster nach vorn holt, kommt über den ersten Menüeintrag nie hinaus: das Popup ist beim
  zweiten Aufruf schon zu, und der Klick landet auf dem, was darunter liegt. Deshalb gibt es
  `tools\kette.ps1` — es fokussiert **genau einmal** und klickt dann die ganze Kette.
  `tools\klick.ps1` bleibt für Einzelschritte.
- **Ein modaler Dialog ist ein eigenes Fenster.** Für `SendKeys` an einen Datei- oder
  Meldungsdialog darf man das Hauptfenster **nicht** nach vorn holen — sonst tippt man in
  die Zeichenfläche. Erst in den Dialog klicken, dann senden. (Genau so ist in dieser Runde
  ein Geodreieck auf einer Notizbuchseite gelandet.)
- **Das Werkzeug klickt in die Zeichenfläche, wenn man sich vertut** — und der Stift ist
  meistens aktiv. Auf einer **Kopie** ist das folgenlos, auf der echten Datenbank wäre es
  ein Datenverlust. Das ist der praktische Grund für Dauerregel 4, nicht nur der
  theoretische.
- **Ein Doppelklick auf eine Galerie-Kachel malt einen Punkt in die Zeichenfläche**, wenn
  das Dokument schneller aufgeht als der zweite Klick kommt. Über den Baum öffnen ist
  sicherer.

**Neu aus Phase 3 — Fernsteuern des Avalonia-Kopfs**

- **Avalonias Menüs und Flyouts sind eigene Fenster.** Sie liegen außerhalb von
  `GetWindowRect` des Hauptfensters und fehlen auf einer Fensteraufnahme **vollständig** —
  man sieht das geschlossene Fenster und hält das Menü fälschlich für nicht geöffnet. Dafür
  gibt es seit Phase 3 den Schalter **`-Voll`** an `kette.ps1`: er nimmt den ganzen
  Bildschirm auf. **Für jeden Menüpfad im Avalonia-Kopf Pflicht.** Der WPF-Kopf zeichnet
  seine Menüs in denselben Fensterbereich; dort fällt es nicht auf.
- **Ein offen gelassenes Menü rächt sich im nächsten Lauf.** `SetForegroundWindow` schließt
  das Popup, Avalonias Menü hält seinen Zustand aber noch — der nächste Klick auf die
  Menüleiste *schließt* dann, statt zu öffnen, und alle folgenden Schritte der Kette landen
  auf dem, was darunter liegt. In dieser Runde ist so ein Textdokument entstanden.
  **Gegenmittel: jede Kette mit `'#{ESC}'` beginnen.**
- **Fensterkoordinaten sind nicht Bildschirmkoordinaten.** Ein maximiertes Fenster ragt um
  die Rahmenbreite über den Bildschirm hinaus (hier ~13 px). Wer auf einer Fensteraufnahme
  misst und auf dem Bildschirm klickt, liegt um diesen Betrag daneben — bei einem großen
  Knopf egal, bei einem Menüeintrag nicht. **Mit `-Voll` gemessen stimmen beide überein.**
- **Beide Köpfe fotografieren:** `schau.ps1 -Kopf avalonia|wpf -Konfig Debug|Release`.

**Neu aus Phase 3 — Fernsteuern unter Wayland (`tools/linux/`)**

Drei Eigenheiten, über die auf diesem Laptop jeder stolpert. Alle drei haben Zeit gekostet,
und keine davon sieht wie ein Fehler aus.

- **XTEST-Koordinaten sind nicht die Koordinaten, die dabei herauskommen.** Unter
  GNOME-Wayland nimmt mutter die Ereignisse von XWayland entgegen und rechnet sie mit dem
  Skalierungsfaktor der Sitzung hoch: ein `XTestFakeMotionEvent` auf (500,500) setzt den
  Zeiger hier tatsächlich auf **(1000,1000)**. `XQueryPointer` meldet danach die *echte*
  Lage, `XGetGeometry` ebenfalls — **die Eingabeseite ist die einzige, die skaliert.**
  Das zeigt sich nicht als Fehler: es wird geklickt, es kommt auch an (die Leerlaufuhr der
  Sitzung springt zurück), nur eben an der doppelten Stelle. Man trifft scheinbar zufällig
  mal etwas und mal nichts und sucht den Fehler in der App.
  **Gegenmittel:** `zeiger` **misst** den Faktor beim ersten Zug, statt ihn anzunehmen.
- **Das X-Wurzelfenster ist unter Wayland kein verlässliches Abbild.** Eine Aufnahme davon
  zeigt alten Inhalt neben neuem — beim ersten Versuch sah es aus, als zeichne die App ihr
  Fenster **doppelt**. `import -window <id>` liest dagegen das Fenster selbst und liefert
  ein sauberes Bild. Dazu zwei Eigenheiten des hiesigen ImageMagick (7.1.2-29):
  `import -window root <datei>` scheitert mit „missing an image filename", obwohl der
  X-Delegat vorhanden ist — **mit einer Fensterkennung statt `root` läuft derselbe Aufruf
  durch**; und der GNOME-Weg über D-Bus (`org.gnome.Shell.Screenshot`) antwortet mit
  „Screenshot is not allowed", der Portal-Weg braucht eine Rückfrage beim Nutzer.
- **Nicht maximieren, während eine Kette läuft.** `super+Up` auf ein bereits maximiertes
  Fenster hebt die Maximierung auf; GNOME setzt es danach mit **anderer Geometrie** wieder
  zusammen (hier von 2560 auf 3072 px Breite). Alle vorher gemessenen Koordinaten sind dann
  falsch, und weil das Fenster trotzdem normal aussieht, sucht man lange. **Geometrie einmal
  mit `zeiger fenster` holen und danach nichts mehr an der Fenstergröße ändern.**
- **Was sich so nicht prüfen lässt: der Stift.** XTEST erzeugt Maus- und Tastaturereignisse.
  Druck, Neigung und die Unterscheidung Stift/Finger entstehen im Digitizer und lassen sich
  nicht nachbilden — ein Zug mit `zeiger` prüft deshalb immer den **Rückfallpfad**, nie den
  Stiftpfad. Dafür gibt es **F9** (§4.10): die Anzeige schreibt hin, was wirklich ankommt,
  und ist auf einem Foto nachlesbar.
- **Avalonias Menüs und Flyouts fehlen auf der Fensteraufnahme**, aus demselben Grund wie
  unter Windows (sie sind eigene Fenster). Unter Windows hilft `-Voll`; hier hilft das
  nicht, weil die Vollbildaufnahme unbrauchbar ist. Wer einen Menüpfad belegen will, holt
  sich die Kennung des Popups über die Fensterliste und fotografiert **die**.

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

dotnet test -c Release       # beide Testprojekte, 138 Tests

# Golden-Files bewusst neu setzen (danach den Diff lesen, siehe §4.6)
$env:GONK_SNAPSHOT_UPDATE=1; dotnet test tests\GonkNote.Core.Tests; $env:GONK_SNAPSHOT_UPDATE=$null
$env:GONK_GOLDEN_UPDATE=1;   dotnet test tests\GonkNote.Wpf.Tests;  $env:GONK_GOLDEN_UPDATE=$null

# Testinstanz mit Wegwerf-DB -- Windows-Kopf
.\src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\GonkNote.exe --db "$env:TEMP\x.db"

# ... und der Linux-Kopf, der unter Windows genauso laeuft (seit Phase 3, §4.9/§5b).
# net10.0 ohne Plattform-Anhaengsel und ohne RID: nicht self-contained.
.\src\GonkNote.Avalonia\bin\Release\net10.0\GonkNote.Avalonia.exe --db "$env:TEMP\x.sqlite"

# Echte Daten gefahrlos gegentesten: erst kopieren (Dauerregel 4 -- ohne Nachfragen erlaubt).
# BEIDE Teile, immer: der Blob-Ordner leitet seinen Namen von der Datenbankdatei ab. Ohne ihn
# sind alle Bilder scheinbar weg und man sucht den Fehler an der falschen Stelle.
$d = "$env:TEMP\gonk-echt"; mkdir $d -Force
Copy-Item "$env:APPDATA\GonkNote\gonknote.sqlite" $d -ErrorAction SilentlyContinue
Copy-Item "$env:APPDATA\GonkNote\gonknote.db"     $d -ErrorAction SilentlyContinue   # Altstand, falls noch da
Copy-Item "$env:APPDATA\GonkNote\gonknote.blobs"  $d -Recurse
# Ab hier nur noch die Kopie -- die Dateien unter %APPDATA% werden nie geoeffnet.
.\src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\GonkNote.exe --db "$d\gonknote.sqlite"

# Die Migration selbst pruefen: NUR die Altdatei kopieren und diese uebergeben. Die App
# erkennt sie an der Kopfkennung, legt gonknote.sqlite daneben an und arbeitet damit weiter --
# der Stamm bleibt "gonknote", also passt gonknote.blobs zu beiden (HANDOFF §4.8).
.\src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\GonkNote.exe --db "$d\gonknote.db"

# Danach aufraeumen. Die Kopie enthaelt Schulunterlagen und darf nicht liegen bleiben.
Remove-Item $d -Recurse -Force

# Prüfen, ob nichts WPF-Verseuchtes nach Core gerutscht ist
Select-String -Path src\GonkNote.Core\**\*.cs -Pattern "System\.Windows|System\.Drawing" -List
```

**Fernsteuern und fotografieren** — seit Phase 2 als drei Skripte unter `tools\`, statt
jedes Mal neu getippt:

```powershell
# Seit Phase 3 wählt -Kopf, welcher der beiden Köpfe startet.
.\tools\schau.ps1 -Kopf wpf                          # startet mit der Kopie, maximiert, fotografiert
.\tools\schau.ps1 -Kopf avalonia -Konfig Debug -Db "$env:TEMP\gonk-echt\gonknote.sqlite"

.\tools\kette.ps1 -AppPid <pid> -Schritte '#{ESC}','292,45','122,211' -Voll   # Menüpfad
.\tools\klick.ps1 -AppPid <pid> -X 251 -Y 406 -Doppel 1                       # Einzelschritt
```

Schritte sind `"x,y"`, `"x,y,2"` (Doppelklick), `"x,y,r"` (Rechtsklick) oder `"#TASTEN"`
(SendKeys). **Koordinaten sind echte Bildschirmpixel** — `SetProcessDPIAware()` steht in
jedem Skript, der Rechner läuft auf 200 %.

**Beim Avalonia-Kopf zusätzlich `-Voll`** und **jede Kette mit `'#{ESC}'` beginnen**: seine
Menüs sind eigene Fenster und fehlen sonst auf dem Foto, und ein offen gelassenes Menü läuft
im nächsten Aufruf gegen die Wand. Zu allen Fallstricken siehe §7 „Fernsteuern".

**Auf dem CachyOS-Laptop** stattdessen (§4.10; `zeiger` einmalig bauen, siehe §5b):

```bash
tools/linux/schau.sh --db /tmp/gonk-probe.sqlite --bild /tmp/schuss.png
tools/linux/klick.sh --bild /tmp/schuss.png w:120,191 '#Return' 'w:z:800,450>1600,400'
```

**Koordinaten mit `w:` angeben.** Sie sind dann relativ zur linken oberen Ecke des Fensters
— genau so, wie man sie auf einer Fensteraufnahme misst — und `zeiger` rechnet Ursprung
**und** Skalierungsfaktor selbst dazu. Wer echte Bildschirmpixel einsetzt, liegt unter
Wayland um den Faktor 2 daneben (§7 „Fernsteuern unter Wayland").

Der ältere, ausführliche Weg steht im V1-Handoff §7 — inklusive der Stolpersteine
(Umbenennen-Modus nach dem Anlegen, Bild-hoch/-runter greift im `FlowDocumentScrollViewer`
nicht, die IDE reißt den Fokus zurück).

---

## 9. Chronik

Eine Zeile je Runde, neueste zuerst. V1-Runden 1–36 stehen in `gonk-note\HANDOFF.md` §10.

| Runde | Datum | Was |
|---|---|---|
| V2-13 | 2026-08-03 | **Stift am Gerät geprüft — alles bestanden** (§5a, „Gegenprobe in der echten App"). Der Nutzer hat Druck, Neigung und Handballenabweisung mit der F9-Anzeige in der laufenden App durchgeprüft: Zeigerart `Pen`, `(Gerät liefert Druck)` mit veränderlichem Wert, dickerer Strich beim Aufdrücken; Neigung in Grad ≠ 0 und ein sichtbar breiterer Bleistift bei gekipptem Stift; kein Strich vom Handballen und kein verrutschendes Blatt. **Damit ist die Kette vom Digitizer bis zum gezeichneten Pixel zum ersten Mal durchgehend belegt** — bisher endete der Nachweis am Prototyp (§5a), und die automatisierten Belege konnten nur den Rückfallzweig zeigen, weil XTEST keine Stiftereignisse erzeugt. **Nicht ausgeräumt:** es bleibt **eine** Geräteklasse (Wacom AES); MPP und EMR sind weiter ungetestet, §5 „Noch offen" Punkt 1 steht |
| V2-12 | 2026-08-03 | **Die Neigung wandert ins Dateiformat** (§4.11) — Nutzer-Entscheidung, und bewusst **auf dem Laptop** statt unter Windows. `WbPoint` bekommt `TX`/`TY` (Grad), der Eingabepfad schreibt sie in jeden Punkt, und **nur der Bleistift** wertet sie aus: eine schräg gehaltene Mine zieht eine breitere Spur, ein Fineliner nicht, und beim Textmarker hinge es an der Drehung um die eigene Achse, die kein Digitizer liefert. **Warum das ohne Windows-Gegenprobe zulässig war:** die Felder tragen `WhenWritingDefault` und werden bei 0 nicht geschrieben — Bestandsdateien bleiben byteweise gleich, was bei 6308 Druckpunkten auf 160 Striche (§4.8) auch eine Größenfrage ist; der Breitenfaktor ist ohne Neigung **exakt 1**, sodass alle zwanzig Pixelhashes aus Phase 1 unverändert grün blieben und **auch der WPF-Kopf Bestandsdokumente gleich zeichnet**; und `_type` ist nicht betroffen, weil `WbPoint` kein polymorpher Typ ist. `NeigungTests` (10 Tests, jetzt 125 Core / 138 gesamt), Neigung zusätzlich im `Beispieldokument` und im feldweisen Roundtrip-Vergleich — dort bewusst am **Stift**, weil ein geneigter Bleistift den Hash `bleistift-koernung` verschöbe. **Offen bleibt die Gegenprobe am echten Stift**: XTEST erzeugt keine Stiftereignisse, das geht nur von Hand (F9) |
| V2-11 | 2026-08-03 | **Phase 3, zweiter Brocken — die Zeichenfläche steht** (§4.10), erstmals **auf dem CachyOS-Laptop** gebaut. **Notizbuch und Whiteboard zeichnen, radieren und speichern unter Linux**; Textdokumente bleiben ausgegraut (M1-Vorgabe, `Tab.NoCanvasYet` in beiden Tabellen darauf umformuliert). Der Renderer bekommt **Avalonias eigenen `SKCanvas`** über `ISkiaSharpApiLeaseFeature` — möglich, weil `Avalonia.Skia` an derselben SkiaSharp-Fassung hängt wie Core (3.119.4); ein offizielles `SkiaSharp.Views.Avalonia` gibt es nicht, und eine gerasterte Zwischenfläche wäre eine volle Bildkopie je Bild gewesen. Damit ist die offene Frage aus §5a beantwortet. **Vierte Avalonia-Eigenheit aufgelöst:** `Render` läuft auf dem Render-Faden — gelöst durch **Aufzeichnen** (`SKPictureRecorder` auf dem Oberflächen-Faden, `DrawPicture` auf dem Render-Faden), was nebenbei den Zwischenspeicher vektoriell und damit zoomfest macht. Eingabepfad mit `GetIntermediatePoints()`, **erkanntem statt angenommenem Druck** (Avalonia meldet für ein druckloses Gerät glatt 0,5) samt wirksamem Rückfall, zweistufiger Handballenabweisung, Radiergummi-Ende und Stiftknopf. **Neigung kommt an, wird aber nicht gespeichert** — `WbPoint` hat keinen Platz dafür, das ist eine offene Formatfrage (§5). Neu als Messgerät: die **Stift-Anzeige mit F9**. Trefferprüfung und Lasso als **`WbHit` nach Core** gezogen (15 neue Tests, jetzt 115 Core / 128 gesamt); der WPF-Kopf behält bewusst seine Fassung. **Neu: `tools/linux/`** — `schau.sh`, `klick.sh` und ein eigener `zeiger` über X11/XTEST, ohne Fremdpaket; die Lücke, die §5b nicht kannte. **Drei Fehler am laufenden Programm gefunden** (§7): Zugriff auf ein fremdes Steuerelement im Renderdurchlauf (Fehlerbild: leere Werkzeugleiste), Tastaturfokus auf dem Rahmen statt auf der Fläche, und die Vorgabetinte, die dem App-Theme statt dem Papier folgte (Fehlerbild: hell auf weiß). Dazu drei Wayland-Fallen fürs Fernsteuern (Eingabekoordinaten um Faktor 2 skaliert, X-Wurzelaufnahme unbrauchbar, Maximieren ändert die Geometrie). Geprüft **ohne** echte Daten — Nutzer-Entscheidung: selbst angelegte Notizbücher sind für den Eingabepfad die bessere Probe |
| V2-10 | 2026-08-03 | **Phase 3, erster Brocken** (§4.9): **`src/GonkNote.Avalonia` steht und läuft** — Avalonia **12.1.1** auf `net10.0` (dieselbe Fassung, mit der §5a gemessen hat), alle **zwölf** Schnittstellen aus `Core/Platform/` umgesetzt, davon drei bewusst als vorhandener Rückfall. Ordnerbaum und Galerie aus **demselben** `MainViewModel` wie der WPF-Kopf. **Farbtabelle in Core** (`Core/Theming/`, 20 Farben **inklusive Papier**) statt eines zweiten Paars fest verdrahteter Theme-Dateien — die Entscheidung, die §6 für diesen Zeitpunkt vorgesehen hatte; Wächter `FarbtabelleTests` hält beide Fassungen zusammen. **Noch keine Zeichenfläche**, das ist der nächste Brocken. Drei Avalonia-Eigenheiten aufgelöst: synchrone Schnittstelle gegen asynchrones Toolkit (`Modal.PushFrame`), keine MessageBox (eigenes `MessageWindow`, neue Schlüssel `Dlg.Yes`/`Dlg.No`), keine Icon-Schrift unter Linux (Vektorformen). **Zwei Übersetzungsfallen am laufenden Programm gefunden** (§7): Avalonia frischt Indexer-Bindungen nicht auf, und es hält die Quelle einer Bindung nicht am Leben — das zweite fiel erst nach einem erzwungenen Sammellauf auf, als halb übersetzte Oberfläche. Version auf **0.3.0**, `About.Version` in beiden Tabellen auf Phase 3. CI baut den Kopf im Linux-Lauf mit; `schau.ps1`/`kette.ps1` um `-Kopf` und `-Voll` erweitert. An einer Kopie der echten Datenbank in **beiden** Sprachen und **beiden** Themes geprüft, WPF-Kopf an derselben Kopie unverändert, echte DB byteweise identisch. Entschieden: **Phase 3 wird unter Windows entwickelt**, nicht auf dem Laptop (§5b) |
| V2-9 | 2026-08-02 | **Phase 2, Schritte 3+4 — Phase 2 damit fertig** (§4.8): **LiteDB raus aus dem Produktivpfad**, Persistenz über `Microsoft.Data.Sqlite` mit `System.Text.Json`-Source-Generator (`GonkJson`); die `_type`-Namen leben als `[JsonDerivedType]` **wörtlich** weiter. Neues Projekt `src/GonkNote.Legacy/` als einziger Ort mit LiteDB — liest Altdatenbanken **ReadOnly** ein; iOS wird es nie sehen. Migration automatisch beim ersten Start, in eine `.neu`-Datei und erst nach dem Commit umbenannt; die Altdatei bleibt unangetastet. `BlobStore` über `AppPaths.BlobFolder` (Schritt 4), Stamm `gonknote` bewusst unverändert. `AlteTypnamenTests` vom Roundtrip- zum **Migrations**-Wächter (3 neue Tests, jetzt 90). Sicherheitslücke in einem mitgezogenen SQLitePCLRaw über transitives Pinning behoben. An einer Kopie der echten Datenbank **feldweise** verglichen (27 Einträge, 160 Striche / 6308 Punkte, 32 Bilder byteweise gleich) und am laufenden Programm in beiden Sprachen geprüft; Altdatei danach byteweise identisch. Alle vier mitgelieferten Dokumente auf `gonknote.sqlite` nachgezogen (Dauerregel 1) — dort steht die Sicherungsanleitung —, dazu `THIRD-PARTY-NOTICES.md` und die seit V2-3 veralteten `net8.0`-Angaben |
| V2-8 | 2026-07-31 | **Phase 2, Schritte 1+2** (§4.7): `Core/Platform/` mit zwölf Schnittstellen und `IPlatformServices` als Bündel, WPF-Umsetzungen in `src/GonkNote.Wpf/Platform/`, `ThemeService` → `WpfThemeHost`. **`GonkNote.ViewModels` ist eigene `net10.0`-Assembly** — der Ringschluss aus §4.2 ist weg, `Core` und `ViewModels` sind nachweislich WPF-frei. Bildimport und OCR-Vorbereitung nach `WbImagePrep` in Core gezogen: damit sind die **letzten zwei Lücken aus §4.4 zu** (8 neue Tests, jetzt 87). Zwei Übersetzungsfehler am laufenden Programm gefunden und behoben (§7 „Übersetzung"). In beiden Sprachen mit einer DB-Kopie gegengeprüft, PDF-Export über den PDFium-Rückweg gemessen |
| V2-7 | 2026-07-30 | Versionszeile im Über-Dialog über `Loc` statt fest verdrahtet — war zweideutig **und** unübersetzt (§4.5), in beiden Sprachen am laufenden Programm geprüft. Dauerregel 4 aufgenommen: Kopie der echten Daten ohne Nachfragen erlaubt, die echte DB bleibt unangetastet. Alles nach GitHub gepusht |
| V2-6 | 2026-07-30 | Markdown-Export behält Hyperlink-Ziele (`[Text](URL)`, §7) — Nutzer-Entscheidung; Golden-File `referenz.md` bewusst nachgezogen. IDE-Fehler in `OcrService.cs` waren fehlende `obj\Debug`-Zwischendateien, kein Codefehler (§7) |
| V2-5 | 2026-07-30 | **Phase 1:** `GonkNote.Core.Tests` (70 Tests) und `GonkNote.Wpf.Tests` (8 Export-Fixtures), 20 Renderer-Snapshots, Golden-Files für DOCX/Markdown, PDF über den PDFium-Rückweg, CI mit windows- und ubuntu-Lauf. Linux-Seite im Docker-Container gegengeprüft: alle 70 Core-Tests grün, Pixelhashes **identisch** zu Windows. Dabei den zweiten SkiaSharp-3-Absturz gefunden und behoben (`SKBitmap.Decode` → `WbImages`, §7). Markdown-Hyperlink-Lücke gefunden, nicht behoben (§5.3) |
| V2-4 | 2026-07-29 | `HANDOFF.md` ins Repo aufgenommen (solange privat), Rückweg als Checkliste in §6; Doku-Pflege-Regel auf alle vier Dokumente und beide Sprachen erweitert |
| V2-3 | 2026-07-29 | **SkiaSharp 3.119.4 + Svg.Skia 5.1.1**: Text auf `SKFont`, Bildqualität auf `SKSamplingOptions`, Absturz in der Bleistift-Körnung behoben (§7); WPF-Kopf auf `net10.0-windows10.0.19041.0`. Remote angelegt und alles gepusht. Anforderung „jeder Stylus" aufgenommen |
| V2-2 | 2026-07-28 | Ziel-Framework auf **net10.0** (LTS) gehoben, `LangVersion 14`; Entscheidungen zu SkiaSharp, Remote und Stylus-Test festgehalten |
| V2-1 | 2026-07-28 | **Phase 0:** Klon aus V1 mit Historie, Umzug nach `src/` per `git mv`, `Directory.Build/Packages.props`, Lokalisierung nach Core (`TExtension` abgespalten), Wurzel-Assets über `Link`. Build Debug+Release grün, Start und echte DB (Kopie) am laufenden Programm geprüft |
