# Gonk Note V2 — Projektübergabe

**Stand: 2026-08-04 · Version 0.3.0 · net10.0 · SkiaSharp 3 · SQLite · Avalonia 12 · ✅ M1 erreicht · beide Schulden aus Phase 3 eingelöst · Phase 4 läuft (Schritte 1 und 2 von 6)**

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
| **Roadmap (die Vorgabe)** | `C:\Users\manue\Desktop\GonkNote-TM\gonk-note-port-RM.MD` — **umgezogen**, hier stand bis 2026-08-04 der Pfad direkt auf dem Desktop |
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

Dann **Phase 3, Brocken 6 und 7** (§4.12), ebenfalls auf dem Laptop: **Drag & Drop im
Baum, die einblendbare Titelleiste, die Einstellungen-Seitenleiste der Zeichenfläche** und
das **`EmbeddedDocs`-Gegenstück** — „Hilfe → Erste Schritte" und das gerenderte README
erscheinen jetzt auch unter Linux. Der Markdown-**Zerleger** ist dafür nach `Core/Text/`
gewandert; jeder Kopf malt nur noch. **Damit ist Phase 3 abgeschlossen und M1 ausgerufen**
(Nutzer-Entscheidung), und die vier mitgelieferten Dokumente beschreiben im selben Zug beide
Ausgaben (Dauerregel 1).

Zuletzt, **wieder unter Windows: die zwei benannten Schulden aus Phase 3 sind eingelöst**
(§4.13). Der WPF-Kopf rechnet Trefferprüfung und Lasso jetzt mit **`WbHit` aus Core**, und
`MarkdownFlow` ruft **`Markdown.Parse`** statt selbst zu zerlegen — die Endlosschleife aus
§4.12 ist damit mitgekommen. **Dieselbe Geometrie und dieselbe Grammatik stehen nicht mehr
doppelt.** Dass die Auswahl dabei nicht um Pixel gewandert ist, steht nicht als Behauptung
da: derselbe Prüflauf ist einmal mit dem alten und einmal mit dem neuen Stand gefahren
worden, und die Aufnahmen sind **Pixel für Pixel identisch**. Dazu zeigt der WPF-Über-Dialog
endlich den **Datenordner**, wie der Linux-Kopf es tut.

Danach **Phase 4, Schritt 1** (§4.14): **das eigene Dokumentmodell steht** in `Core/Text/` —
`TdDocument` → `TdBlock` → `TdInline` mit nullbaren Formaten und einer vierstufigen Kaskade,
dazu ein eigenes Speicherformat (Kennung `GNTD` + Json über den Source-Generator) und **DOCX
in beide Richtungen** als das Tor, das die Roadmap nach jedem Schritt verlangt. Der Befund,
der die ganze Phase begründet, steht in §4.14: `TextDoc.Rtf` enthält RTF oder ein
WPF-`XamlPackage` — **das Speicherformat der Textdokumente ist Windows, in Bytes gegossen**,
und solange das so ist, bleiben Textdokumente unter Linux ausgegraut, egal wie gut die
Oberfläche wird.

Zuletzt **Schritt 2, ganz** (§4.15 und §4.16): **Abschnitte, Seiteneinrichtung und die
Layout-Rechnung**. Auf Nutzer-Entscheidung wandert die Seiteneinrichtung aus `TextDoc` in
`TdSection`; die Übernahme der Bestandsdokumente kommt **zuletzt**, nach Schritt 6, weil sie
vorher stiller Datenverlust wäre. `TdPageSetup` speichert nur Maße — Formatname und
Querformat werden daraus **abgelesen** statt gespeichert, damit es keine zweite Wahrheit
gibt. `TdLayout` bricht Zeilen und Seiten um; gemessen wird hinter der Naht
**`ITdTextMeasure`**, damit der Umbruch auf jedem System dieselben Zahlen liefert und nicht
von der installierten Schrift abhängt.

**Als Nächstes:** Phase 4, **Schritt 3 — Listen** (§6). Strikt in der Reihenfolge aus Roadmap
§5, nach jedem Schritt DOCX-Roundtrip. **M1 bleibt ein gültiger Ausstiegspunkt**; Phase 4 ist
die, an der Projekte sterben.

**Tests laufen lassen:**

```powershell
dotnet test -c Release        # Windows: beide Projekte, 234 Tests
```

```bash
dotnet test tests/GonkNote.Core.Tests   # Linux: 221 Tests, laufen in ~7 s
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
Core, Linux-Werkzeuge) und §4.11 (Neigung im Dateiformat).

**Erledigt in Phase 3, Brocken 6 und 7:** §4.12 (Drag & Drop, Titelleiste,
Einstellungen-Seitenleiste, `EmbeddedDocs` samt Markdown-Zerleger in Core). Testzahl steht
jetzt bei **159** (146 Core + 13 WPF).

**Erledigt nach Phase 3:** §4.13 — die **zwei benannten Schulden** sind eingelöst
(`WbHit` und `Markdown.Parse` im WPF-Kopf), dazu der Datenordner im WPF-Über-Dialog. Die
Testzahl bleibt bei **159**: es ist nichts dazugekommen, es sind zwei Fassungen weniger
geworden. **Genau das ist das Ergebnis** — die Wächter, die vorher nur die Core-Fassung
gehalten haben (`TrefferTests`, `MarkdownTests`), halten jetzt beide Köpfe.

**Erledigt in Phase 4, Schritt 1:** §4.14 — das **Dokumentmodell** in `Core/Text/` samt
Speicherformat und DOCX-Roundtrip.

**Erledigt in Phase 4, Schritt 2:** §4.15 (**Abschnitte und Seiteneinrichtung** —
`TdSection`, `TdPageSetup`, DOCX-`sectPr` in beide Richtungen) und §4.16 (**die
Layout-Rechnung** — `TdLayout` bricht Zeilen und Seiten um, hinter der Naht
`ITdTextMeasure`). Testzahl jetzt **234** (221 Core + 13 WPF). Alles davon ist noch nirgends
angeschlossen; das ist Absicht und in §4.14 begründet.

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
│  │  │                          seit §4.13 von BEIDEN Köpfen benutzt
│  │  ├─ Theming/                die Farbtabelle (§4.9)              ← neu in Phase 3
│  │  │                          ThemeColor (20 Farben), HexColor, ThemeDefinition,
│  │  │                          Themes.Light/.Dark — ein Theme ist eine Datentabelle
│  │  ├─ Text/                   Markdown — der Zerleger hinter den vier mitgelieferten
│  │  │                          Dokumenten (§4.12)                  ← neu in Phase 3
│  │  │                          seit §4.13 von BEIDEN Köpfen benutzt
│  │  │                          TdDocument/TdSection/TdBlock/TdInline + TdFormat — das
│  │  │                          eigene Dokumentmodell, TdJson (Speicherformat), TdDocx
│  │  │                          (DOCX in beide Richtungen), TdLayout (Zeilen- und
│  │  │                          Seitenumbruch) hinter der Naht ITdTextMeasure/
│  │  │                          TdSkiaMeasure                        ← neu in Phase 4
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
│  │  ├─ Themes/Styles.axaml     Form und Vektor-Symbole — KEINE Farben (die kommen aus Core)
│  │  └─ Services/               EmbeddedDocs (avares:// statt pack://, §4.12)
│  │     └─ Localization/        TExtension + LocText (§7 „Übersetzung im Linux-Kopf")
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
│  ├─ GonkNote.Core.Tests/       net10.0 · läuft auch unter Linux · 221 Tests
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
│  ├─ kette.ps1                  mehrere Klicks in EINEM Durchgang — für Menüpfade (§7);
│  │                             seit §4.13 auch ZIEHEN ("x1,y1>x2,y2>…"), ohne das sich
│  │                             Lasso und Verschieben nicht fernsteuern lassen
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

> **Die rechte Spalte ist der Stand von damals.** Die Zeichenfläche kam mit §4.10, Drag &
> Drop, die Titelleiste und `EmbeddedDocs` mit §4.12. Übrig sind heute nur noch Import und
> Export sowie Texterkennung und Rechtschreibung — alle vier mit demselben Grund und
> ausdrücklich nicht M1.

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
| Seiten blättern, anlegen, löschen; Zoom, Verschieben, Finger-Gesten | ~~Seiteneinstellungen (Format, Muster, Farbton)~~ — **seit §4.12 vorhanden** |
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

> **✅ Eingelöst am 2026-08-04** (§4.13): der WPF-Kopf rechnet mit `WbHit`, die private
> Fassung in `WhiteboardView.Selection.cs` ist weg. Pixelgleichheit belegt.

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

### 4.12 Phase 3, Brocken 6 und 7 — der Rest bis M1

Umgesetzt am 2026-08-03 **auf dem CachyOS-Laptop**, direkt im Anschluss an §4.11.
**Damit ist Phase 3 abgeschlossen und M1 erreicht** (Nutzer-Entscheidung, §5 Punkt 4).

#### Was dazugekommen ist

| | |
|---|---|
| **Drag & Drop im Baum** | `MainWindow.Ziehen.cs`. Verschieben, mit `Strg` kopieren; Ziel ist der Ordner, der Ordner des getroffenen Dokuments oder — auf der leeren Fläche — die Wurzel |
| **Einblendbare Titelleiste** | `MainWindow.Titelleiste.cs`. Maximiert verschwindet die Zier des Systems; die eigene Leiste gleitet bei Zeigerkontakt am oberen Rand herein |
| **Einstellungen-Seitenleiste** | `WhiteboardView.Einstellungen.cs`. **Nur der Seiten-Abschnitt**: Muster, Farbton, Format, Ausrichtung, „Als Standard für neue Seiten" |
| **`EmbeddedDocs`-Gegenstück** | `Services/EmbeddedDocs.cs`, `Views/MarkdownView.cs`, `Views/GuideWindow.axaml`. „Hilfe → Erste Schritte" und das gerenderte README im Über-Dialog |
| **Markdown-Zerleger in Core** | `Core/Text/Markdown.cs` — neu, siehe unten. Wächter `MarkdownTests` (21 Tests, jetzt **146** Core) |

#### Die Entscheidung, die hier fiel: der Zerleger geht nach Core

Der WPF-Kopf hat `Services/MarkdownFlow.cs` — dort sind **Zerlegen und Darstellen dasselbe**,
weil das Ergebnis unmittelbar ein `FlowDocument` ist. Avalonia hat keines (§4.1). Die
Grammatik ein zweites Mal abzuschreiben wäre die Falle aus §4.10 gewesen: **zwei Fassungen
derselben Formel driften auseinander, ohne dass es auffällt.**

Deshalb steht die Grammatik jetzt einmal in **`Core/Text/Markdown.cs`** und liefert einen
Blockbaum (`MdHeading`, `MdParagraph`, `MdList`, `MdTable`, …); jeder Kopf malt nur noch.
Nach der Faustregel aus §3 gehört sie genau dorthin — sie zeichnet kein Pixel.

**Der WPF-Kopf ist wieder bewusst nicht umgestellt**, aus demselben Grund wie bei der
Farbtabelle und `WbHit`: er lässt sich hier nicht bauen. **Damit steht dieselbe Grammatik an
zwei Stellen** — eine Schuld, kein Zustand; sie gehört auf dem Windows-Rechner
zusammengelegt. Der Umbau ist dort klein: `MarkdownFlow` behält seine `FlowDocument`-Hälfte
und ruft `Markdown.Parse` statt selbst zu zerlegen.

> **✅ Eingelöst am 2026-08-04** (§4.13). Der Umbau war tatsächlich klein — und er hat die
> Datei um ein Drittel kürzer gemacht.

**`[GeneratedRegex]` statt `RegexOptions.Compiled`.** Das eine erzeugt seinen Code zur
Übersetzungszeit, das andere zur Laufzeit über `Reflection.Emit` — und den gibt es unter
NativeAOT nicht (§1, derselbe Grund, aus dem LiteDB weichen musste). `MarkdownFlow` benutzt
noch die Laufzeitfassung; dort ist es folgenlos.

#### Was der Zerleger sofort gefunden hat

**Eine Endlosschleife**, und zwar eine, die auch in `MarkdownFlow` steckt: Eine
**Tabellenzeile ohne Trennzeile** darunter ist laut `Parse` keine Tabelle und landet im
Absatz-Zweig — dessen Schleife weist Tabellenzeilen aber ab (`!IstTabellenZeile`). Der Absatz
bliebe leer, `i` stünde still, `Parse` liefe endlos.

**Behoben, indem die erste Zeile bedingungslos genommen wird**: `Parse` hat für sie bereits
entschieden, dass sie ein Absatz ist. Wächter: `Eine_Tabelle_braucht_ihre_Trennzeile`.

**Aufgefallen ist es daran, dass der Testlauf nicht mehr zurückkam** — nicht an einer roten
Meldung. Merksatz: ein Testlauf, der hängt, ist ein Fehlschlag und kein langsamer Rechner.
In den mitgelieferten Dokumenten steht heute keine solche Zeile, deshalb ist die Falle im
WPF-Kopf nie zugeschlagen; **beim Zusammenlegen gehört sie mitgezogen.**

#### Die achte Stelle, an der Avalonia nicht wie WPF ist

Drei kamen in §4.9 dazu, eine in §4.10. Hier sind es drei weitere, alle beim Ziehen und beim
Darstellen:

1. **Ziehen läuft über XDND, also über Prozessgrenzen.** `DragDrop.DoDragDropAsync` gibt die
   Fracht an den Fenstermanager weiter; ein .NET-Objektverweis überlebt das nicht. Der Ausweg
   ist ein **prozessinternes Format** (`DataFormat.CreateInProcessFormat<T>`) — es verlässt
   Avalonia gar nicht erst. Sonst müsste eine Kennung reisen und die Gegenseite den Eintrag
   im Baum wiederfinden.
2. **Der Auslöser muss `PointerPressedEventArgs` sein**, nicht irgendein Zeigerereignis. Der
   Druck wird deshalb festgehalten und erst benutzt, wenn der Zeiger die Ziehschwelle
   überschreitet — WPF fragt an derselben Stelle `SystemParameters.MinimumHorizontalDragDistance`,
   Avalonia hat dafür keine Auskunft, also steht hier ein Wert (6 px).
3. **Ein anklickbarer Verweis ist ein Steuerelement, kein ausgezeichneter Text.** `Run` kennt
   kein Klickereignis, einen `Hyperlink` wie in WPF gibt es nicht. Der Weg führt über
   `InlineUIContainer` — ein `TextBlock` mitten im Fließtext.

Dazu zwei Kleinigkeiten, die der Compiler meldet und die trotzdem hierhergehören, weil sie
in Avalonia **12** neu sind: `SystemDecorations` ist veraltet (→ `WindowDecorations`), und
`IDataObject`/`DataObject` sind durch `IDataTransfer`/`DataTransfer` ersetzt.

#### Was die einblendbare Titelleiste hier *nicht* braucht

Der WPF-Kopf hängt dafür einen `WM_GETMINMAXINFO`-Hook ein (`WindowBounds`), weil ein
randloses Fenster unter Windows sonst über den Bildschirm hinausragt und die Taskleiste
verdeckt. **Unter X11 maximiert der Fenstermanager gegen `_NET_WORKAREA`** — gemessen: das
Fenster sitzt exakt auf 0,64 / 3072×1664, dem Arbeitsbereich. `WindowBounds` bleibt damit zu
Recht Windows-only.

Die Bewegung macht ein `TransformOperationsTransition`; es genügt, `RenderTransform` zu
setzen. Nur beim Umschalten *in* den maximierten Zustand wird der Übergang für einen
Augenblick abgehängt — sonst glite die Leiste erst herein und gleich wieder hinaus.

#### Zwei Texte, die auf Linux falsch waren

Beide beim Gegenprüfen am laufenden Programm gefunden, beide in **beiden** Tabellen behoben
(Dauerregel 1):

- **`About.Subtitle`** nannte fest `%APPDATA%\GonkNote` — im Linux-Kopf also eine Angabe, die
  dort nicht stimmt, und zwar **direkt über der Zeile, die den echten Ordner zeigt**. Der
  Satz kommt jetzt ohne Pfad aus. Das war Bedingung: denselben Schlüssel benutzt der
  WPF-Dialog, und **der** zeigt den Datenordner nicht daneben an.
- **`Wb.Settings.Tip`** verspricht „Seite, Formen, Text, Cover". Die Leiste im Linux-Kopf hat
  nur die Seite. Neuer Schlüssel **`Wb.Settings.PageTip`** statt eines Textes, der drei Dinge
  zusagt, die es nicht gibt.

#### Nachtrag: zwei Symbole zurück auf die V1-Form (Nutzerwunsch)

Beim Ausprobieren hat der Nutzer zwei der neuen Vektorformen abgelehnt — **Ordner** und
**Textmarker**; die übrigen ausdrücklich für besser als V1 befunden. Beide sind neu
gezeichnet, und zwar **nach dem Zeichen, das der WPF-Kopf dafür benutzt** (Segoe Fluent
`E8B7` bzw. `E7E6`) statt nach dem Vorschaubild — das ist die verlässlichere Vorlage, und
sie steht im Repo:

| Symbol | War | Ist |
|---|---|---|
| `Icon.Folder` | der „moderne" Ordner ohne Reiter: linke Hälfte höher, eine Schräge zur Grundfläche | der **klassische mit Reiter** oben links, samt der waagerechten Linie darunter, die den Reiter erst als Reiter lesbar macht |
| `Icon.Textmarker` | erst ein **schräg liegender** Marker, dann ein aufrechter mit **geschlossener Kappe** | ein aufrechter, breiter Marker, **oben offen**: der Schaft ist am Rand abgeschnitten, zwei Stege stehen über der Bandlinie, unten die Keilspitze |

**Drei Lehren, alle am Bild und nicht am Code sichtbar geworden:**

1. **Der Marker ist oben offen.** Daran sind die ersten beiden Anläufe gescheitert. Eine
   geschlossene Kappe macht daraus einen Stift mit Deckel; erst der abgeschnittene Schaft
   liest sich als Marker.
2. **Die Breite ist das Erkennungsmerkmal**, nicht die Neigung — ein schmaler Umriss steht
   neben Stift und Bleistift wie ein dritter Stift.
3. **`StrokeThickness` wächst nicht mit `Stretch` mit.** Der Wert steht in Gerätepunkten,
   nicht in denen der Geometrie: dieselben `0,7`, die im Baum bei 16 px kräftig aussehen,
   sind auf der Galerie-Kachel bei 118 px ein Haarstrich. Die Kachel trägt deshalb ihren
   **eigenen** Wert (`4,5`, in `MainWindow.axaml`). Das ist keine Eigenheit des Ordners —
   **wer künftig eine Vektorform groß zeigt, muss die Strichstärke dort eigens setzen.**

Beide am laufenden Programm angesehen, in **drei** Größen: Baum (16 px), Schnellzugriff
(21 px) und Galerie-Kachel (118 px), und in **beiden** Themes. **Der WPF-Kopf ist nicht
betroffen** — er zeichnet diese Symbole weiter mit der Icon-Schrift.

> **Zum Beurteilen einer Form ist ein SVG schneller als die App.** Dieselbe Pfadangabe in
> eine `.svg` schreiben, mit `magick … -filter point -resize 400x` groß rendern und ansehen:
> das ist genau das, was Avalonia zeichnet, und kostet keinen Neustart. Die Gegenprobe am
> laufenden Programm bleibt trotzdem Pflicht — nur eben einmal statt bei jedem Zwischenstand.

> **~~Offen geblieben, klein und benannt:~~** ~~Der **WPF**-Über-Dialog könnte den
> Datenordner genauso anzeigen wie der Linux-Kopf es tut.~~ **✅ Erledigt am 2026-08-04**
> (§4.13): eine Zeile XAML, eine Zeile Code, am laufenden Programm angesehen — der Dialog
> zeigt `C:\Users\…\AppData\Roaming\GonkNote` unter dem Untertitel.

#### Am laufenden Programm geprüft (Dauerregel 1 und 4)

**Ohne echte Daten**, gegen `/tmp/gonk-probe.sqlite`, in **beiden** Sprachen; die Werkzeuge
aus `tools/linux/` haben jeden Schritt fotografiert:

- **Drag & Drop:** ein Notizbuch in einen Ordner gezogen (Baum klappt auf, Galerie zählt
  eins weniger) und über die leere Fläche wieder in die Wurzel zurück.
- **Titelleiste:** `super+Up` → die Zier des Systems verschwindet, die Menüleiste rückt nach
  oben; Zeiger an den oberen Rand → die eigene Leiste mit Symbol, Titel und den drei
  Fensterknöpfen gleitet herein.
- **Einstellungen:** Muster „Kariert" → das Blatt bekommt sofort ein Raster und die
  Registerkarte ihren Änderungspunkt; Farbton „Dunkel" → **die erste Farbkachel springt auf
  Weiß mit** (die Falle aus §7, „Die Vorgabetinte gehört zum Papier").
- **Hilfe → Erste Schritte** und **Über Gonk Note** mit dem gerenderten README: Überschriften,
  Zitatblock mit Akzentbalken, fett/kursiv, `Code` mit Hinterlegung, nummerierte Liste,
  Trennlinie. Der Verweis „Erste Schritte" im README **öffnet die Anleitung** — angeklickt
  und nachgewiesen.
- **Beides in Englisch** über Ansicht → Sprache: „Version 0.3.0 · Port, phase 3", englischer
  Untertitel, englisches README, englische Anleitung.

**Dabei ein Fehler in der eigenen Arbeit gefunden:** die Einstellungen-Leiste ging mit
**lauter leeren Umschaltern** auf. `EinstellungenSpiegeln` steigt aus, solange die Leiste
unsichtbar ist — und sie wurde vor dem Sichtbarmachen gerufen. Auf einem Foto sieht das aus,
als würde die Seite nicht ausgelesen; die Ursache war die Reihenfolge zweier Zeilen.

---

### 4.13 Die zwei Schulden aus Phase 3 sind eingelöst

Umgesetzt am 2026-08-04, **auf dem Windows-Rechner** — dem einzigen, auf dem der WPF-Kopf
baut und sich am laufenden Programm gegenprüfen lässt. Genau darum standen beide Punkte
seit §4.10 und §4.12 als *benannte* Schuld da und nicht als Versäumnis.

#### Was zusammengelegt wurde

| Stand doppelt | Jetzt |
|---|---|
| Trefferprüfung und Lasso | `WhiteboardView.Selection.cs` ruft **`WbHit`** aus Core. `RotatePt`, `HitElement`, `HitTestElement`, `SegOrPointDist`, `ShapeOutlineDist`, `AllCornersInside`, der Lasso-Kern und `ComputeSelectionBounds` sind als eigene Rechnung verschwunden — die Datei ist von 384 auf 290 Zeilen geschrumpft |
| Markdown-Grammatik | `MarkdownFlow` ruft **`Markdown.Parse`** und malt nur noch den Blockbaum. Von 339 auf 232 Zeilen; `System.Text.RegularExpressions` ist als `using` weg |

**Die Namen sind absichtlich stehen geblieben.** `HitElement`, `HitTestElement`,
`SelectByLasso` und `ComputeSelectionBounds` gibt es weiter — als Einzeiler, die an `WbHit`
weiterreichen. Sie haben zusammen **elf** Aufrufstellen in fünf anderen Partials
(`Input`, `Import`, `QuickMenu`, `Stickers`, `Ocr`); die alle anzufassen hätte den Diff
verdreifacht und jede dieser Stellen zu einer neuen Fehlerquelle gemacht, ohne dass sich am
Ergebnis etwas ändert. **Der Zweck war, die zweite Rechnung loszuwerden, nicht die zweite
Bezeichnung.** Was hier bleibt, hängt am Steuerelement: `Zoom` für die Toleranzen (`5f/Zoom`,
`12f/Zoom`), `_page` und `_selection` für den Zustand, die Griffe für die Darstellung.

#### Die Endlosschleife ist mitgekommen — ohne dass jemand sie anfassen musste

§4.12 verlangt ausdrücklich, sie beim Zusammenlegen mitzuziehen: eine **Tabellenzeile ohne
Trennzeile** darunter landet im Absatz-Zweig, den die Schleife dort selbst abweist, sodass
der Zeilenzähler stillsteht. In `Markdown.Parse` ist sie behoben (die erste Zeile wird
bedingungslos genommen).

**Der Punkt daran:** es war *kein* zusätzlicher Handgriff. Wer die Grammatik wegwirft, wirft
den Fehler mit weg — das ist der Unterschied zwischen „an zwei Stellen reparieren" und
„eine Stelle haben". Wächter bleibt `Eine_Tabelle_braucht_ihre_Trennzeile`, und er bewacht
seit heute beide Köpfe.

#### Der Über-Dialog zeigt den Datenordner

Die Zeile aus §4.12 („Offen geblieben, klein und benannt"). Ein `TextBlock` in
`AboutDialog.xaml`, `AppPaths.Current.DataFolder` in `AboutDialog.xaml.cs` — genau wie im
Linux-Kopf. **Sie war nötig, nicht kosmetisch:** `About.Subtitle` nennt seit §4.12 keinen
festen Pfad mehr, weil er unter Linux falsch war; ohne diese Zeile fehlte die Angabe auf
Windows damit **ganz**. Der Untertitel-Rand ist von 12 auf 8 gegangen, damit die zwei Zeilen
zusammengehören.

#### „Pixelgleich" ist hier gemessen und nicht behauptet

Der Auftrag war, am laufenden Programm zu prüfen, dass Lasso und Verschieben **pixelgleich**
liegen wie vorher. Nachlesen und Testlauf reichen dafür nicht: `TrefferTests` bewacht
`WbHit`, aber nicht, dass der WPF-Kopf dieselben Toleranzen einsetzt wie seine alte Fassung.

**Der Aufbau, und warum er so aussieht:**

1. Eine **Master-Datenbank** einmal anlegen — zwei Striche und ein Rechteck, gezeichnet und
   gespeichert. Sie wird für beide Läufe kopiert. **Das ist der entscheidende Teil:** würde
   in jedem Lauf neu gezeichnet, unterschieden sich schon die Strichpunkte, und jeder
   Vergleich wäre wertlos.
2. Ein **Prüflauf** als Skript, mit festen Bildschirmkoordinaten: Dokument über den Baum
   öffnen → Lasso-Werkzeug → eine Umkreisung um alle drei Elemente → Foto → aus der Auswahl
   heraus um (145,145) ziehen → Foto.
3. Diesen Lauf **zweimal**: einmal mit dem neuen Stand, dann `git stash push` **nur** auf
   `WhiteboardView.Selection.cs`, neu bauen, denselben Lauf mit dem alten.
4. Die Aufnahmen **Pixel für Pixel** vergleichen.

**Ergebnis: alle drei Bildpaare identisch, 2906×1826, null abweichende Pixel.** Auswahlrahmen,
Schnellmenü, Skaliergriff und die verschobenen Elemente liegen auf denselben Koordinaten.

> **Warum das mehr wert ist als ein grüner Testlauf:** Ein Test prüft, was er kennt. Dieser
> Vergleich prüft **alles, was auf dem Schirm steht** — auch das, woran beim Umbau niemand
> gedacht hat. Er kostet zwei Läufe und einen `git stash`, und er ist der einzige Beleg, der
> das Wort „pixelgleich" trägt. **Für jede weitere Zusammenlegung dieser Art ist er das
> Muster** (Phase 4 wird davon mehrere haben).

#### Der Zieh-Schritt, der dafür erst gebaut werden musste

Lasso und Verschieben sind **Ziehbewegungen**. `kette.ps1` und `klick.ps1` konnten nur
klicken und tippen — ein Klick allein erzeugt keine Auswahl, der Prüflauf war damit gar
nicht fernsteuerbar. Das Linux-Gegenstück kann es seit §4.10 (`klick.sh`, `z:`); unter
Windows fehlte es schlicht.

Neu in `kette.ps1`: ein Schritt der Form **`"x1,y1>x2,y2>…"`** — aufsetzen, über alle
Stützpunkte fahren, loslassen. **Zwischen den Stützpunkten wird interpoliert** (12
Teilschritte), denn die App sammelt ihre Lassopunkte aus Mausbewegungen; ein einzelner
Sprung ergäbe eine Gerade statt einer Umkreisung. Eine PowerShell-Eigenheit steckt darin
(§7, „Fernsteuern").

#### Am laufenden Programm geprüft (Dauerregel 1 und 4)

**Ohne echte Daten** — für Geometrie und Markdown taugt eine selbst angelegte Datenbank
besser, und die Schulunterlagen bleiben, wo sie sind. Gegen eine Wegwerf-DB in `%TEMP%`,
die danach samt Blob-Ordner gelöscht wurde:

- **Lasso und Verschieben:** der Pixelvergleich oben.
- **„Hilfe → Erste Schritte"**, deutsch **und** englisch: Überschriften, Absätze, `Code`
  mit Hinterlegung, fett/kursiv, Trennlinien, nummerierte Liste mit Verweisen,
  **verschachtelte** Listen (Punkte unter Nummern), Zitatblock mit Akzentbalken, Code-Block.
- **„Über Gonk Note"**, deutsch **und** englisch: die Versionszeile über `Loc`
  („Version 0.3.0 · Portierung, Phase 3" / „· Port, phase 3"), der Untertitel **ohne** Pfad,
  darunter der **Datenordner**, und das gerenderte README samt **Tabelle** (Kopfzeile
  halbfett, Unterkanten an den Zellen, fetter Text in der Zelle).
- **Der Verweis „Erste Schritte" im README** öffnet die Anleitung — angeklickt und
  nachgewiesen. Das ist die Stelle, an der der Umbau am ehesten etwas hätte fallen lassen:
  `MarkdownFlow` reichte den Handler bis dahin über ein `[ThreadStatic]`-Feld durch, weil
  Zitatblöcke sich selbst erneut aufriefen. **Mit dem Blockbaum entfällt der Grund** — ein
  `MdQuote` bringt seinen Inhalt bereits zerlegt mit —, der Handler ist jetzt ein
  gewöhnlicher Parameter, und das Feld ist weg.

**Zwei Fehler in der eigenen Arbeit gefunden**, beide beim Fernsteuern und beide in §7
nachgetragen: die verschachtelten Arrays im neuen Zieh-Schritt, und der Untermenü-Klick, der
schließt statt zu öffnen.

---

### 4.14 Phase 4, Schritt 1 — das Dokumentmodell steht

Umgesetzt am 2026-08-04 unter Windows, direkt im Anschluss an §4.13. **Der erste Schritt der
Reihenfolge, die Roadmap §5 vorschreibt:** Absätze + Zeichenformate → Seitenumbruch → Listen
→ Tabellen → Felder/TOC → Diagramme.

#### Der Befund, der die ganze Phase begründet

`TextDoc.Rtf` heißt nicht nur historisch so — das Feld enthält **RTF oder ein
WPF-`XamlPackage`** (ZIP, erkennbar am „PK"), beides erzeugt von
`System.Windows.Documents.TextRange.Save/Load`. **Das Speicherformat der Textdokumente ist
Windows, in Bytes gegossen.**

Daraus folgt mehr, als „der Editor fehlt noch": Solange das das Format ist, bleiben
Textdokumente unter Linux ausgegraut, **egal wie gut die Oberfläche wird** — es gibt nichts,
was der Linux-Kopf lesen könnte. Phase 4 ist deshalb nicht nur ein Editor-Umbau, sondern ein
Formatwechsel, und der gehört in denselben Vorsichtsbereich wie §4.8.

#### Was jetzt in `Core/Text/` steht

| Datei | Inhalt |
|---|---|
| `TdFormat.cs` | `TdCharFormat`, `TdParaFormat` — nullbare Felder plus `Over()`-Kaskade |
| `TdDocument.cs` | `TdDocument` → `TdBlock`(`TdParagraph`, `TdPageBreak`) → `TdInline`(`TdRun`, `TdLineBreak`) |
| `TdJson.cs` | Speicherformat: Kennung `GNTD` + UTF-8-Json über den Source-Generator |
| `TdDocx.cs` | DOCX in **beide** Richtungen gegen das Modell — das Tor aus Roadmap §5 |

**Das Namenspräfix `Td` folgt dem Haus:** `Wb*` für das Whiteboard, `Md*` für Markdown,
`Td*` für das Textdokument. Es ist hier zusätzlich praktisch notwendig: `Paragraph`, `Run`,
`Table`, `Section` und `Hyperlink` heißen in `System.Windows.Documents` genauso, und die
Exporter, die als Nächstes umgeschrieben werden, müssten sonst jede Zeile mit einem Alias
versehen.

#### Die eine Entscheidung, die das Modell trägt: `null` heißt „nicht gesetzt"

Jedes Formatfeld ist nullbar, und **`null` ist nicht „Standardwert", sondern „hier steht
nichts"**. Das ist der Unterschied, an dem ein Texteditor sonst scheitert: Wer in einer
Überschrift ein Wort fett macht, setzt genau ein Feld — Schrift und Größe müssen weiter von
der Überschrift kommen. Trüge jeder Lauf eine vollständige Kopie des Formats, ginge jede
spätere Änderung an der Überschrift an allen bereits geschriebenen Läufen vorbei.

Gerechnet wird über `Over()`, **dasselbe Muster wie `ThemeDefinition.Over` in
`Core/Theming`** — der eigene Wert gewinnt, wo er gesetzt ist, sonst zählt die Unterlage. Die
Kette ist vierstufig: Stück → Absatz → Dokument → `Standard`. Nur `Standard` ist überall
belegt, `Aufgeloest()` kann deshalb nie `null` liefern.

**Der Preis dafür steht im Speicherformat:** `TdJson` benutzt
`DefaultIgnoreCondition = WhenWritingNull` und damit **das Gegenteil von `GonkJson`**. Dort
werden Standardwerte mitgeschrieben, weil „Feld fehlt" und „Feld ist 0" sonst
ununterscheidbar wären; hier ist genau diese Unterscheidung der ganze Sinn. Ein Format hat
neun bzw. zehn Felder, ein Dokument hat sehr viele Läufe — stünde in jedem neunmal `null`,
wäre das kein kleiner Unterschied.

#### Die Diskriminatoren sind kurz, und das ist eine Entscheidung

`"p"`, `"run"`, `"break"`, `"pagebreak"` — nicht `"Namensraum.Typ, Assembly"` wie bei
`WbElement`. Dort ist die lange Form ein **Erbe**: LiteDB hat sie so geschrieben, und
Bestandsdaten hängen daran (§7). Dieses Format ist neu und hat kein solches Erbe. Es gibt
keinen Grund, einen Assemblynamen in jede Datei zu schreiben — und schon gar keinen, ihn
dadurch für immer unveränderlich zu machen.

**Reserviert, damit spätere Schritte die Namen nicht zweimal vergeben:** `"list"` (Schritt 3),
`"table"` (Schritt 4), `"hyperlink"` und `"field"` (Schritt 5), `"image"` und `"chart"`
(Schritt 6). Wächter: `Die_Diskriminatoren_stehen_fest` hält sie **wörtlich** fest, genau wie
`AlteTypnamenTests` es für die alten tut.

#### Warum `Section` in Schritt 1 noch fehlte

Die Roadmap nennt `Document → Section → Block → Inline`. `Section` fehlte in Schritt 1, und
zwar nicht aus Versehen: Ein Abschnitt trägt seine eigene Seiteneinrichtung, und die stand
**vollständig an `TextDoc`** (Format, Ränder, Kopf-/Fußzeile, Wasserzeichen). Sie dort **und**
hier zu führen wäre die Doppelung aus §4.10 gewesen. **Mit §4.15 ist sie da** — samt der
Nutzer-Entscheidung, wohin die Seiteneinrichtung gehört.

**`TdPageBreak` steht dagegen schon jetzt da**, obwohl das Seitenlayout erst Schritt 2 ist:
Der heutige Editor kann einen Seitenumbruch einfügen, ein Bestandsdokument kann ihn also
enthalten, und ein Modell, das ihn nicht kennt, würde ihn bei der Übernahme still
verschlucken. **Ein verlorener Seitenumbruch fällt erst beim Drucken auf.**

#### Das DOCX-Tor — und warum es nicht der eigene Roundtrip ist

Die Roadmap verlangt es wörtlich: „Nach jedem Schritt muss der DOCX-Roundtrip-Test grün
sein." Der Grund steht in derselben Zeile — Phase 4 ist die, an der Projekte sterben, und ein
Modell ohne Gegenprobe wächst so lange weiter, bis niemand mehr weiß, welcher Teil davon je
funktioniert hat.

**DOCX ist dafür die richtige Gegenprobe, weil es ein fremdes Format ist.** Der eigene
Json-Roundtrip (`DokumentmodellTests`) beweist nur, dass Schreiben und Lesen zueinander
passen — auch wenn beide denselben Fehler machen. DOCX kennt die eigenen Bequemlichkeiten
nicht und hat sofort vier Stellen aufgedeckt, an denen das Modell und das Format sich nicht
von selbst decken; alle vier stehen jetzt als eigener Wächter da:

| Stelle | Worum es geht |
|---|---|
| **Hängender Einzug** | DOCX kennt dafür **zwei** Felder (`firstLine`, `hanging`), beide positiv. Wer nur eines schreibt, macht aus −0,5 cm ein +0,5 cm — aus einer Aufzählung wird ein Einzug in die falsche Richtung |
| **`<w:b/>` ohne `val`** | heißt **„an"**. Ein naives `Val?.Value ?? false` macht jede fette Stelle eines fremden Dokuments still normal — und zwar nur bei Dateien aus Word, nie bei den eigenen |
| **Leerzeichen am Rand** | überleben nur mit `xml:space="preserve"`. Sonst sitzt der Text nach dem Speichern zusammengeschoben da, nicht beim Tippen |
| **Hervorhebung „aus"** | `null` = nicht gesetzt, `""` = ausdrücklich keine. In DOCX ist Letzteres die Füllung `auto` und **nicht** das Fehlen des Elements |

Dazu prüft `TdDocx.Pruefen` gegen das **Office-2019-Schema**, dieselbe Messlatte wie beim
heutigen `DocxExporter`: ein Dokument, das Word nicht öffnet, ist kein Export. Deshalb ist
die Reihenfolge der Kindelemente in `TdDocx` Schema und keine Geschmacksfrage (`CT_RPr`:
rFonts, b, i, strike, color, sz, u, shd, vertAlign — `CT_PPr`: keepNext, pageBreakBefore,
spacing, ind, jc, outlineLvl).

> **Das ist nicht der Ersatz für `DocxExporter`/`DocxImporter`.** Die laufen weiter und
> bedienen die App; sie werden abgelöst, wenn das Modell alles kann, was sie können
> („danach umverdrahten", Roadmap §5). Sie **parallel** zu pflegen wäre die Falle aus §4.10 —
> deshalb steht in `TdDocx` nur, was das Modell heute wirklich trägt, und alles andere wirft
> statt still zu verschwinden (`Was_noch_nicht_geht_verschwindet_nicht_still`).

#### Eine Sicherheitslücke, die erst hier auftauchen konnte

Core hat für DOCX `DocumentFormat.OpenXml` bekommen — der von Roadmap §0.3 vorgesehene
Endzustand. Der Build meldete daraufhin **NU1903**: das mitgezogene `System.IO.Packaging`
8.0.0 hat zwei bekannte Lücken. Behoben wie bei SQLitePCLRaw (§7) über das transitive Pinning
plus eine `PackageVersion`-Zeile auf 10.0.10.

**Die Lehre steckt darin, warum es vorher nie aufgefallen ist:** Der WPF-Kopf benutzt
OpenXml seit jeher — aber unter `net10.0-windows` mit WPF kommt `System.IO.Packaging` aus dem
Framework (WindowsDesktop), es wird gar kein NuGet-Paket geholt. Erst als **Core** die
Referenz bekam, reines `net10.0`, wurde daraus eine echte Abhängigkeit. **Dasselbe Paket kann
je Ziel-Framework eine andere Abhängigkeitskette haben** — eine Lücke taucht dann dort auf,
wo man sie nicht sucht. Für Phase 5 (iOS) ist das derselbe Punkt.

#### Ein Wächter, der zu genau war

Drei Roundtrip-Tests waren zuerst rot, und zwar zu Recht rot aussehend und trotzdem kein
Codefehler: Einzüge kamen als 1,4993 cm statt 1,5 cm zurück. **Ein Twip ist 1/1440 Zoll =
0,0018 cm** — feiner kann DOCX einen Einzug gar nicht ablegen. Der Test verlangte drei
Nachkommastellen und prüfte damit nicht den eigenen Code, sondern die Auflösung eines fremden
Formats.

Behoben am **Test**, nicht am Code: Zentimeter werden auf zwei Stellen verglichen (0,005 cm,
immer noch dreimal feiner als ein Twip), Punktwerte weiter auf drei — `pt·20` ergibt ganze
Twips und geht auf. **Merksatz: eine Toleranz gehört an die Auflösung des Formats, nicht an
die Genauigkeit der Fließkommazahl.**

#### Stand und was ausdrücklich noch nicht gilt

**37 neue Tests** (18 Modell, 19 DOCX), Gesamtzahl **196** (183 Core + 13 WPF). Beide Köpfe
bauen mit 0 Warnungen.

**Am laufenden Programm ist hier nichts zu sehen, und das ist richtig so:** Das Modell ist
noch **nirgends angeschlossen** — kein Editor schreibt hinein, keine Datenbank liest daraus,
`TextDoc.Rtf` ist unverändert. Es gibt in diesem Schritt nichts, was ein Bildschirmfoto
zeigen könnte; die Gegenprobe ist der DOCX-Roundtrip, und der ist ein fremdes Format und
damit strenger als ein Foto. **Der Anschluss ans laufende Programm kommt mit der Übernahme**
(siehe §6, „Wie die Bestandsdokumente herüberkommen").

---

### 4.15 Phase 4, Schritt 2 (erster Teil) — Abschnitte und Seiteneinrichtung

Umgesetzt am 2026-08-04. **Schritt 2 heißt „Seitenumbruch" und hat zwei Hälften:** das
Modell, das eine Seite überhaupt beschreiben kann (hier), und die Layout-Rechnung, die Zeilen
und Seiten daraus umbricht (steht noch aus, siehe §6).

#### Die Nutzer-Entscheidung, die diesen Schritt bestimmt hat

**Gefragt war:** Die Seiteneinrichtung steht heute an `TextDoc`, also neben dem Inhalt.
Bleibt sie dort, oder wandert sie ins Modell?

**Entschieden (2026-08-04): sie wandert in `TdSection`**, und die Felder an `TextDoc` werden
zur Quelle der einmaligen Übernahme. Was das kauft: mehrere Abschnitte je Dokument — Deckblatt
quer, Rest hoch — und ein DOCX-Import, der `sectPr` vollständig lesen kann statt die zweite
Einrichtung wegzuwerfen. Was es kostet: eine additive Migration nach dem Muster §4.8.

> **Bis die Übernahme läuft (nach Schritt 6), stehen beide nebeneinander — und das ist
> ausdrücklich *nicht* die Doppelung aus §4.10.** Sie beschreiben verschiedene Dokumente: die
> alten im Altformat, die neuen im eigenen. Wer eine der beiden „aufräumt", bevor die
> Übernahme steht, macht Bestandsdokumente unlesbar. Der Unterschied zur echten Doppelung ist
> greifbar: dort gab es **zwei Rechnungen für dieselbe Sache**, hier gibt es **zwei Formate
> für verschiedene Dateien**.

#### Zwei Felder, die es bewusst nicht gibt

Beide Male aus demselben Grund — **zwei Wahrheiten über dieselbe Sache driften auseinander**:

- **Kein Formatname.** `TdPageSetup` speichert Breite und Höhe in Zentimetern, sonst nichts.
  „A4" wird über `Name` aus der Größe **zurückerkannt**. Der heutige Editor legt stattdessen
  `PageFormat = "A4"` ab und rechnet die Größe daraus aus — das geht gut, bis ein fremdes
  DOCX hereinkommt, dessen `sectPr` Zahlen nennt und keinen Namen. **Eine eigene Größe ist
  kein Fehler, sie hat nur keinen Namen** (`Name` gibt dann `null`).
- **Kein Querformat-Schalter.** Querformat heißt: breiter als hoch. `IstQuerformat` liest das
  ab, `Quer()`/`Hoch()` setzen es. Ein `bool` daneben wäre die zweite Wahrheit.

**Die Toleranz beim Zurückerkennen (0,1 cm) ist Absicht:** ein Blatt geht durch DOCX in Twips
und wieder zurück, und wer auf Gleichheit prüft, bekommt für ein A4-Blatt irgendwann
„unbekannt".

#### Die Stelle, an der DOCX unsymmetrisch ist

**Die Seiteneinrichtung des *letzten* Abschnitts steht am Ende des Körpers, die aller anderen
im Absatzformat ihres jeweils letzten Absatzes.** Wer alle ans Körperende hängt, bekommt ein
Dokument mit genau einer Seiteneinrichtung — und merkt es erst am Ausdruck.

Zwei Folgefallen, beide mit eigenem Wächter:

- **Ein Abschnittswechsel darf keinen Absatz kosten.** Der Absatz, der die `sectPr` trägt,
  ist Inhalt. Wer ihn beim Lesen als bloßen Träger abtut, verliert je Abschnitt eine Zeile.
- **Ein Abschnitt darf mit einem Seitenumbruch enden.** Dann hängt die `sectPr` am
  Umbruch-Absatz und muss beim Lesen trotzdem ein Seitenumbruch bleiben.

#### Kopf- und Fußzeile gehen durch echte Felder

`{SEITE}` und `{SEITEN}` werden zu **PAGE** und **NUMPAGES**, nicht zu Text. Als Text stünde
in einem exportierten Dokument auf jeder Seite dieselbe Zahl. Beim Lesen geht es den Weg
zurück — ohne den käme aus einem Rückimport die beim Schreiben eingesetzte „1" als
gewöhnlicher Text zurück, und die Kopfzeile zeigte auf jeder Seite Seite 1. Dazu
`UpdateFieldsOnOpen`, damit Word die Felder beim Öffnen rechnet.

`{DATUM}` und `{TITEL}` bleiben vorerst wörtlich stehen; sie werden mit den Feldern in
Schritt 5 nachgezogen. **Kopf- und Fußzeile sind aus demselben Grund noch Text und keine
Absätze** — eine Kopfzeile ist im Grunde ein kleines Dokument mit Feldern darin, und ein
Absatzbaum hier wäre heute ein Versprechen, das die Oberfläche nicht einlösen kann.

#### Was in `TdPageSetup` ausdrücklich fehlt

**Das Wasserzeichen.** Es steht weiter an `TextDoc`, weil es ein **Bild** ist und Bilder
Schritt 6 sind. In DOCX ist es ohnehin eine kopfzeilenverankerte Zeichnung — es kommt also
genau dann, wenn das Modell Bilder kann, und keinen Schritt früher.

---

### 4.16 Phase 4, Schritt 2 (zweiter Teil) — die Layout-Rechnung

Umgesetzt am 2026-08-04. **Damit ist Schritt 2 abgeschlossen**: aus einem `TdDocument` und
einer Schriftmessung werden Seiten mit Zeilen mit Stücken, jedes mit Ort und Maß.

#### Eine Naht vor der Schriftmessung — und warum sie nötig war

`TdLayout` misst nicht selbst, sondern fragt **`ITdTextMeasure`**. Zwei Gründe, und beide
zählen:

1. **Die Schriften unterscheiden sich je System.** „Segoe UI" gibt es unter Linux nicht —
   dieselbe Falle wie bei der Icon-Schrift (§7). Ein Umbruch-Test gegen echte Schriftbreiten
   hätte auf Windows und im Linux-Lauf der CI **verschiedene** Ergebnisse und wäre nach dem
   ersten falschen Alarm abgeschaltet. Genau darum nimmt §4.6 die Schrift schon aus den
   Renderer-Snapshots heraus.
2. **Der Umbruch ist Rechnung, kein Zeichnen.** Mit der Naht davor prüfen die Wächter mit
   festen Maßen — **jedes Zeichen 1 cm breit, jede Zeile 1 cm hoch** — und „nach zehn Zeichen
   umbrechen" wird zu einer Zahl, die stimmt oder nicht. Was übrig bleibt, ist
   `TdSkiaMeasure`: dreißig Zeilen, die Skia fragen, und die selbst nur auf Plausibilität
   geprüft werden (breiterer Text misst mehr, größere Schrift misst mehr, fehlende Schrift
   wirft nicht).

**Gerechnet wird durchgehend in Zentimetern, nicht in Pixeln.** Das Modell rechnet so, DOCX
rechnet so, und der Zoomfaktor gehört an die Stelle, die zeichnet. Ein Umbruch in Pixeln
müsste bei jeder Zoomstufe neu laufen und brächte bei jeder ein leicht anderes Ergebnis.

> **Abweichung von der Roadmap, benannt:** Sie nennt für den Zeilenumbruch
> `SKShaper`/HarfBuzz. Hier misst `SKFont.MeasureText` — das trägt Kerning und reicht für
> Latein vollständig. **HarfBuzz wird gebraucht, sobald arabische oder indische Schrift
> dazukommt**; die Naht ist genau die Stelle, an der das dann ausgetauscht wird, ohne den
> Umbruch anzufassen. Ein Paket, das heute nichts kann, was gebraucht wird, ist eine
> Abhängigkeit ohne Gegenwert.

#### Die Gruppe — wie „nicht vom nächsten Absatz trennen" wirklich funktioniert

Der erste Anlauf war **toter Code**: eine Liste zurückgehaltener Zeilen, die nie gefüllt
wurde. Aufgefallen beim Nachlesen, nicht im Test — ein Wächter dafür hätte auch gefehlt.

Richtig ist: Solange ein Absatz `KeepWithNext` trägt, wandern seine Zeilen **nicht** sofort
auf die Seite, sondern sammeln sich in einer Gruppe — zusammen mit denen der folgenden
Absätze. Erst ein Absatz ohne `KeepWithNext` schließt die Gruppe, und dann wird sie **am
Stück** gesetzt: passt sie nicht mehr, fängt sie ganz auf der nächsten Seite an. Eine
Überschrift bleibt so bei ihrem ersten Absatz, statt allein unten zu stehen.

**Die Abstände davor und danach gehören zur ersten bzw. letzten Zeile** und nicht zwischen
die Zeilen — nur so wandern sie mit der Gruppe. Ein Abstand, der allein oben auf der neuen
Seite landet, wäre ein sichtbarer Fehler. Beim Abstand **davor** wandert die Grundlinie mit:
sie zählt ab der Zeilenoberkante, und die liegt jetzt höher; ohne das säße der Text im
Abstand statt darunter.

#### Drei Stellen, an denen der Umbruch stehenbleiben könnte

Alle drei haben denselben Merksatz aus §4.12: **ein Lauf, der nicht zurückkommt, meldet
keinen Fehler — er meldet gar nichts.**

| Fall | Was passiert |
|---|---|
| **Ein Wort breiter als die Zeile** | Es steht allein in seiner Zeile und ragt heraus. Sichtbar falsch ist besser als ein Umbruch, der hängt |
| **Eine Gruppe höher als eine Seite** | Sie bricht innerhalb um, statt ewig auf eine Seite zu warten, die groß genug ist |
| **Ein Stück, das weder Leerraum noch Nicht-Leerraum ist** | Kann es nicht geben — die Zeile, die den Zähler trotzdem weiterrückt, steht trotzdem da |

#### Was der Umbruch sonst noch richtig macht

- **Blocksatz gilt nicht für die letzte Zeile eines Absatzes.** Sonst zöge ein Schlusswort
  über die ganze Breite auseinander — der auffälligste Fehler, den ein Textsatz machen kann.
  Ein erzwungener Zeilenumbruch beendet ebenfalls eine „letzte" Zeile.
- **Der Leerraum am Zeilenanfang fällt weg.** Er gehört beim Zerlegen ans Wort davor, nicht
  dazwischen — sonst rückte jede Folgezeile um ein Leerzeichen ein.
- **`PageBreakBefore` auf dem ersten Absatz erzeugt keine leere Seite davor.** Der häufigste
  Weg, wie ein Dokument ein weißes Deckblatt bekommt.
- **Ein leerer Absatz hat eine Zeile mit Höhe** — sonst hätte der Cursor keinen Ort.
- **Jeder Abschnitt beginnt auf einer neuen Seite**, und jede Seite kennt die Einrichtung
  ihres Abschnitts. Ein fortlaufender Abschnittswechsel mitten auf der Seite wäre eine eigene
  Angabe in `sectPr`, und die gibt es im Modell noch nicht.

#### Ein Wächter, der zu wenig wusste

Drei Umbruch-Tests waren zuerst rot: „zehn Zeilen à 1 cm passen auf 10 cm" ergab sieben.
**Der Standard hat 8 pt Abstand nach jedem Absatz** — richtig so, aber jede Rechnung im Kopf
geht daran vorbei. Behoben am Test: die Rechenbeispiele setzen den Abstand auf null, und wo
er geprüft werden soll, steht er ausdrücklich am Absatz. **Merksatz: wer mit runden Zahlen
rechnet, muss die Vorgabewerte kennen — sonst prüft er sie mit.**

#### Stand

**43 neue Wächter in Schritt 2** (16 Modell, 27 Umbruch), Gesamtzahl **234** (221 Core +
13 WPF). Alle drei Projekte 0 Warnungen. Wie in Schritt 1 gilt: **am laufenden Programm ist
nichts zu sehen**, das Modell ist noch nicht angeschlossen.

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
| **Wird M1 ausgerufen?** | **Ja** — mit Brocken 6 und 7 ist der M1-Satz buchstäblich erfüllt: Notizbuch und Whiteboard laufen unter Linux, Textdokumente sind ausgegraut. Import/Export steht nicht im M1-Satz und hängt an §4.1 (Phase 4). **Die vier mitgelieferten Dokumente sind im selben Zug auf den Linux-Kopf erweitert worden** (§4.12). Entschieden 2026-08-03 |
| Wo der Markdown-Zerleger steht | **In Core** (`Core/Text/Markdown.cs`), nicht ein zweites Mal im Kopf — er zeichnet kein Pixel (§3, Faustregel), und zwei Fassungen derselben Grammatik driften auseinander (§4.12). ~~Der WPF-Kopf behält vorerst `MarkdownFlow`~~ — **seit 2026-08-04 ruft er `Markdown.Parse`** (§4.13). Entschieden 2026-08-03 |
| Die zwei Schulden aus Phase 3 | **Eingelöst am 2026-08-04** (§4.13), beide unter Windows. `WbHit` und `Markdown.Parse` im WPF-Kopf; die Endlosschleife kam ohne eigenen Handgriff mit |
| Wie „pixelgleich" belegt wird | **Zwei Läufe, ein Bildvergleich** — derselbe Prüflauf gegen dieselbe Master-Datenbank, einmal mit altem und einmal mit neuem Stand, danach Pixel für Pixel verglichen (§4.13). Ein Testlauf allein prüft nur, was er kennt. **Muster für jede weitere Zusammenlegung.** Entschieden 2026-08-04 |
| Wo die Seiteneinrichtung steht | **In `TdSection`**, nicht mehr an `TextDoc` (§4.15). Das kauft mehrere Abschnitte je Dokument und einen DOCX-Import, der `sectPr` vollständig liest; es kostet eine additive Migration nach dem Muster §4.8. Bis die läuft, stehen beide nebeneinander — **verschiedene Formate für verschiedene Dateien**, nicht zwei Rechnungen für dieselbe Sache. Entschieden 2026-08-04 (Nutzer) |
| Wann die Bestandsdokumente übernommen werden | **Zuletzt, nach Schritt 6** (§6). RTF und XamlPackage tragen Tabellen, Bilder und Diagramme; das Modell kann die erst ab Schritt 4 bzw. 6. Eine Übernahme davor wäre **stiller Datenverlust** — genau das, wovor §4.8 warnt. Bis dahin bleibt `Rtf` das führende Feld und das Modell wird über DOCX geprüft, nicht über Nutzerdaten. Entschieden 2026-08-04 (Nutzer) |
| Namen der WPF-Hilfsmethoden | **Bleiben stehen** — `HitElement`, `HitTestElement`, `SelectByLasso`, `ComputeSelectionBounds` sind Einzeiler, die an `WbHit` weiterreichen. Elf Aufrufstellen in fünf Partials umzubenennen hätte den Diff verdreifacht, ohne am Ergebnis etwas zu ändern; wegkommen sollte die zweite **Rechnung**, nicht die zweite Bezeichnung (§4.13). Entschieden 2026-08-04 |

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
3. ~~**Beschreiben die vier mitgelieferten Dokumente V1 oder V2?**~~ **Entschieden am
   2026-08-03** (Nutzer): **V2.** `git clone …/GonkNote.git` in beiden Erste-Schritte-
   Fassungen, ebenso der Issues-Verweis am Ende. Zweimal zurückgestellt, mit §4.12 fällig
   geworden — die Anleitung nannte daneben `src/GonkNote.Avalonia`, ein Projekt, das es im
   V1-Repo nicht gibt.

   > **Dabei wissen:** Das V2-Repo ist **privat**. Der Klon-Befehl läuft damit heute nur für
   > Konten mit Zugriff. Das ist kein Fehler, sondern eine Folge der Reihenfolge — er wird
   > richtig, sobald §6 „Vor dem Öffentlich-Schalten" abgearbeitet ist.
4. ~~**Wann beschreiben die Dokumente auch den Linux-Kopf?**~~ **Entschieden und umgesetzt
   am 2026-08-03** (§4.12): **M1 wird ausgerufen**, und beide Paare sind im selben Zug
   nachgezogen — neuer Abschnitt „Zwei Ausgaben, eine App" mit einer Tabelle, was der
   Linux-Ausgabe fehlt und warum; Bau-, Pfad- und Sicherungsanweisungen für beide Systeme.
   `EmbeddedDocs` hat sein Gegenstück bekommen, die Texte stehen also auch in der App.

   **Was daran offen bleibt:** Punkt 3 oben — in `ERSTE-SCHRITTE.md` und
   `GETTING-STARTED.md` steht weiterhin der **V1**-Klon-Befehl. Er ist auch in dieser Runde
   nicht angefasst worden, weil es eine inhaltliche Frage ist und keine technische. **Er
   fällt jetzt stärker auf**, denn die Anleitung nennt daneben `src/GonkNote.Avalonia` — ein
   Projekt, das es im V1-Repo nicht gibt.

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

### Erledigt: Phase 3 — Avalonia-Shell für Linux · **M1 erreicht**

Erster Brocken am 2026-08-03 unter Windows (§4.9, §5b), Brocken 2 bis 7 am selben Tag auf
dem CachyOS-Laptop (§4.10, §4.11, §4.12).

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
- [x] 6. Drag & Drop im Baum, einblendbare Titelleiste, Einstellungen-Seitenleiste (§4.12).
      Die Seitenleiste hat **nur** den Seiten-Abschnitt — Formen, Text, Notizzettel und
      Cover fehlen als Werkzeuge, also fehlen auch ihre Abschnitte
- [x] 7. `EmbeddedDocs`-Gegenstück (§4.12). Der Markdown-**Zerleger** ist dabei nach
      `Core/Text/` gewandert, weil er kein Pixel zeichnet; jeder Kopf malt nur noch.
      Wächter `MarkdownTests` (21 Tests) — er hat sofort eine Endlosschleife gefunden, die
      auch in `MarkdownFlow` steckt

**Was Phase 3 bewusst ausgelassen hat** — nicht vergessen, sondern nicht M1: Text-,
Formen- und Notizzettel-**Werkzeug** (angezeigt werden diese Elemente, nur anlegen kann man
sie nicht), Drehen und Skalieren der Auswahl, Bild- und PDF-Import, Import und Export.
Sticker, Texterkennung, Zahlenblock, Schnellaktionen und Geodreieck gehören ohnehin nicht
dazu. **Alles davon steht jetzt auch in den vier mitgelieferten Dokumenten**, mit Begründung
(§4.12).

> **✅ M1 ist ausgerufen** (Nutzer-Entscheidung 2026-08-03, §5): Notizbuch und Whiteboard
> laufen unter Linux, Textdokumente sind ausgegraut — der Wortlaut von M1. Damit ist auch
> der gültige Ausstiegspunkt erreicht, den die Roadmap dafür vorsieht.

> **Für den nächsten Brocken vorher lesen:** §4.10 und §4.12 (wie die Zeichenfläche und der
> Kopf gebaut sind), §7 „Der Avalonia-Kopf" — dort stehen jetzt **zehn** Eigenheiten statt
> vier — und §7 „Fernsteuern unter Wayland", ohne das sich auf dem Laptop nichts belegen
> lässt. **Wer etwas zusammenlegt, liest zusätzlich §4.13** („Pixelgleich ist hier
> gemessen") — der Vergleich alt↔neu ist das Muster dafür.

### Erledigt: die zwei Schulden aus Phase 3 — **eingelöst**

Am 2026-08-04 unter Windows abgearbeitet (§4.13). Beide waren Zusammenlegungen, keine
Neubauten:

| Stand doppelt | Erledigt durch |
|---|---|
| ✅ Trefferprüfung und Lasso (`WbHit` in Core ↔ `WhiteboardView.Selection.cs`) | Der WPF-Kopf rechnet mit `WbHit`; die private Fassung ist weg. **Pixelgleichheit gemessen**, nicht behauptet (§4.13) |
| ✅ Markdown-Grammatik (`Core/Text/Markdown.cs` ↔ `Services/MarkdownFlow.cs`) | `MarkdownFlow` behält seine `FlowDocument`-Hälfte und ruft `Markdown.Parse`. **Die Endlosschleife kam ohne eigenen Handgriff mit** — wer die Grammatik wegwirft, wirft den Fehler mit weg |
| ✅ (klein, aus §4.12) Datenordner im WPF-Über-Dialog | Eine Zeile XAML, eine Zeile Code — `About.Subtitle` nennt seit §4.12 keinen Pfad mehr, die Angabe fehlte auf Windows sonst ganz |

**Die Testzahl bleibt bei 159.** Es ist nichts dazugekommen; es sind zwei Fassungen weniger
geworden. `TrefferTests` und `MarkdownTests` bewachen ab jetzt **beide** Köpfe statt nur den
Linux-Kopf.

### Läuft: Phase 4 — eigene Dokument-Engine

**Strikt in der Reihenfolge aus Roadmap §5, nach jedem Schritt DOCX-Roundtrip.** Die
Reihenfolge ist kein Vorschlag: sie ist die Antwort auf „Dokument-Engine wird zum Fass ohne
Boden", das einzige Risiko, das die Roadmap mit **hoch** einstuft.

- [x] **1. Absätze + Zeichenformate** (§4.14, 2026-08-04). Modell, Speicherformat und das
      DOCX-Tor stehen; 37 neue Wächter. **Noch nirgends angeschlossen** — das ist Absicht
- [x] **2. Seitenumbruch** — zwei Hälften, beide fertig:
      - [x] **Modell** (§4.15): `TdSection` + `TdPageSetup`, DOCX-`sectPr` in beide
            Richtungen, Kopf-/Fußzeile mit echten PAGE-Feldern
      - [x] **Layout-Rechnung** (§4.16): `TdLayout` bricht Zeilen und Seiten um, hinter der
            Naht `ITdTextMeasure`. 43 neue Wächter in Schritt 2
- [ ] 3. Listen — `"list"` ist als Diskriminator reserviert
- [ ] 4. Tabellen, inkl. verbundener Zellen
- [ ] 5. Felder und Inhaltsverzeichnis — `TdParaFormat.OutlineLevel` steht seit Schritt 1
      bereit und ist die verlässliche Quelle, die das `FlowDocument` nie hatte
- [ ] 6. Diagramme
- [ ] Danach umverdrahten: `Docx`-/`Markdown`-Im-/Export und `PdfExporter` gegen das eigene
      Modell (§4.1 löst sich damit auf), Ribbon in Avalonia neu

#### Wie die Bestandsdokumente herüberkommen — **vor Schritt 2 zu entscheiden**

`TextDoc.Rtf` enthält RTF oder ein WPF-`XamlPackage` (§4.14). Das Modell kann beides nicht
lesen, und der Linux-Kopf konnte es nie. **Der Vorschlag folgt der Entscheidung, die für die
Datenbank schon gefallen ist** (§4.8, „additiv, die alte Datei bleibt unversehrt liegen"):

- Ein **neues Feld** an `TextDoc` neben `Rtf`, nicht statt dessen. Die alten Bytes werden nie
  überschrieben — dieselbe Regel, aus der `gonknote.db` neben `gonknote.sqlite` liegen bleibt.
- Die Übernahme läuft **auf dem Windows-Rechner**, einmalig beim Öffnen: nur dort gibt es das
  `FlowDocument`, das RTF und XamlPackage überhaupt lesen kann. Der Linux-Kopf kann eine
  Altdatei nicht übernehmen und darf das auch nicht versuchen.
- Ein Dokument, das noch nicht übernommen ist, muss unter Linux **sagen, was los ist**, statt
  leer aufzugehen. „Leer" ist von „kaputt" für den Nutzer nicht zu unterscheiden — dieselbe
  Begründung wie bei `Ohne_Leser_wird_nicht_stillschweigend_neu_angefangen` (§7).

**Was daran offen ist und der Nutzer entscheiden muss:** ob die Übernahme **still** läuft
(wie die SQLite-Migration) oder ob sie sich zeigt. Bei der Datenbank war „still" richtig,
weil nichts verlorengehen konnte. Hier kann etwas verlorengehen: RTF und XamlPackage tragen
Dinge, die das Modell **vor Schritt 6** noch nicht kennt — Tabellen, Bilder, Diagramme. Eine
stille Übernahme vor Schritt 6 wäre stiller Datenverlust.

> **Deshalb der Vorschlag: die Übernahme kommt zuletzt, nicht zuerst.** Erst wenn das Modell
> alles kann, was ein Bestandsdokument enthalten darf, ist sie verlustfrei. Bis dahin bleibt
> `Rtf` das führende Feld und das Modell läuft daneben — geprüft über DOCX, nicht über
> Nutzerdaten.

### Der Rest (Roadmap §5)

| Phase | Inhalt | Aufwand | Ziel |
|---|---|---|---|
| 3 | Avalonia-Shell für Linux — **fertig** | 6–8 W. | ✅ **M1 erreicht** — Notizbuch + Whiteboard laufen unter Linux, Textdokumente ausgegraut |
| 4 | Eigene Dokument-Engine in `Core/Text/` — **läuft, Schritt 1 von 6** | 8–12 W. | **M2** — Funktionsgleichheit Linux ↔ Windows |
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
- **Ziehen läuft auch innerhalb der App über XDND — also über eine Prozessgrenze.**
  `DragDrop.DoDragDropAsync` reicht die Fracht an den Fenstermanager weiter; ein
  .NET-Objektverweis überlebt das nicht, nur ein Datenstrom. **Gegenmittel:
  `DataFormat.CreateInProcessFormat<T>(...)`** — ein solches Format verlässt Avalonia gar
  nicht erst und reicht den echten `TreeItemViewModel` durch. Wer stattdessen eine Kennung
  schickt, muss den Eintrag auf der Gegenseite im Baum wiederfinden, und das ist genau die
  Sorte Arbeit, die man sich hier sparen kann.
- **`DoDragDropAsync` verlangt ausdrücklich `PointerPressedEventArgs`**, nicht irgendein
  Zeigerereignis — es braucht den Zeiger, der noch aufliegt. Ein Ziehen mit Schwelle muss
  den **Druck festhalten** und ihn erst benutzen, wenn der Zeiger weit genug gewandert ist.
  Die Schwelle selbst steht als Zahl im Code (6 px): WPF fragt dafür
  `SystemParameters.MinimumHorizontalDragDistance`, Avalonia hat keine Auskunft dazu.
- **Ein anklickbarer Verweis ist in Avalonia ein Steuerelement, kein ausgezeichneter Text.**
  `Run` kennt kein Klickereignis, und einen `Hyperlink` wie in WPF gibt es nicht. Der Weg
  führt über **`InlineUIContainer`** — ein `TextBlock` mitten im Fließtext. Der bricht in
  sich nicht um; bei Verweistexten von wenigen Wörtern fällt das nicht ins Gewicht.
- **Zwei Umbenennungen in Avalonia 12, die der Compiler meldet** — hier notiert, weil jede
  Anleitung im Netz noch die alten Namen zeigt: `SystemDecorations` ist veraltet
  (→ **`WindowDecorations`**), und `IDataObject`/`DataObject` sind ersetzt durch
  **`IDataTransfer`/`DataTransfer`** (`DataObject` existiert noch, tut aber nichts mehr).
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
  **Das ist die letzte verbliebene Doppelung dieser Art** — `WbHit` und die
  Markdown-Grammatik sind mit §4.13 zusammengelegt. Sie ist aber eine andere Sorte: dort
  standen zwei **Rechnungen**, hier stehen zwei **Datentabellen**, und ein Wächter hält sie
  wirklich zusammen. Sie fällt mit den eigenen Farbschemata (§6), nicht vorher.
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

**Neu aus §4.13 — Fernsteuern, Ziehen und Untermenüs**

- **Ohne Ziehen lässt sich die Auswahl gar nicht fernsteuern.** Lasso und Verschieben sind
  Ziehbewegungen; ein Klick erzeugt keine Auswahl. `kette.ps1` kann das seit §4.13 mit einem
  Schritt der Form `"x1,y1>x2,y2>…"`. **Zwischen den Stützpunkten muss interpoliert werden**
  — die App sammelt ihre Lassopunkte aus Mausbewegungen, ein einzelner `SetCursorPos`-Sprung
  ergäbe eine Gerade statt einer Umkreisung.
- **Die PowerShell-Pipeline entpackt verschachtelte Arrays.**
  `@($s -split '>' | ForEach-Object { [int[]]@($t[0], $t[1]) })` ergibt **keine** Liste von
  Paaren, sondern eine flache Liste von Zahlen — `SetCursorPos` bekommt dann ein Array, wo
  eine Zahl hingehört, und meldet „Die Argumenttypen stimmen nicht überein" an einer Zeile,
  die richtig aussieht. **Gegenmittel: zwei getrennte Achsen-Arrays** (`$xs`, `$ys`) statt
  einer Liste von Paaren.
- **Ein Klick auf einen Untermenü-Eintrag *schließt* das Untermenü, wenn der Zeiger es
  vorher schon per Hover geöffnet hat.** „Ansicht → Sprache → Englisch" scheiterte deshalb
  reproduzierbar: der dritte Klick machte das Untermenü zu, der vierte landete auf dem, was
  darunter lag — hier auf dem Notizbuch-Knopf der Seitenleiste, und es entstand ein
  Notizbuch. **Das Foto zeigt dabei die Ursache nicht**, weil das Untermenü zum
  Aufnahmezeitpunkt durch Hover wieder offen ist. **Gegenmittel: `-WaitMs` hochsetzen** (1400
  statt 700 hat gereicht) und danach am Ergebnis prüfen, nicht am Zwischenfoto.
- **Tastatur statt Maus hilft hier *nicht*.** Der naheliegende Ausweg
  (`{DOWN}{DOWN}{DOWN}{RIGHT}…`) ging ins Leere, weil ein Baumeintrag im Umbenennen-Modus
  stand und alle Tasten abfing. **Erst den Fokus klären, dann tippen.**
- **`{PGDN}` im `FlowDocumentScrollViewer` greift nur, solange der Fokus im Dokument liegt**
  — er geht schon beim nächsten `SetForegroundWindow` verloren, und dann rollt nichts mehr,
  ohne dass es wie ein Fehler aussieht (V1-Handoff §7 kennt das). **Zuverlässig ist der
  Rollbalken-Ziehgriff**, jetzt wo `kette.ps1` ziehen kann.

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
- **`import -window` liefert unter XWayland manchmal ein Bild von vorgestern.** Nicht nur
  das X-Wurzelfenster ist unzuverlässig (oben) — auch die Fensteraufnahme kann einen Stand
  zeigen, der mehrere Schritte alt ist. **Das Fehlerbild ist bösartig:** man klickt, das Foto
  zeigt keine Wirkung, man korrigiert die Koordinaten, klickt woanders hin — und in
  Wahrheit hat schon der erste Klick gesessen. In dieser Runde sind so vier Durchgänge
  draufgegangen; erst ein Klick auf „Design wechseln" hat gezeigt, dass die Eingaben längst
  ankamen.
  **Gegenmittel: nicht dem Bild glauben, sondern dem Vergleich.** Zwei Aufnahmen nacheinander
  hashen (`magick <datei> -format "%#" info:`); ändert sich der Hash nicht, ist entweder
  nichts passiert **oder das Bild ist alt** — dann eine Aktion mit unübersehbarer Wirkung
  auslösen (Theme wechseln) und erneut sehen.
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

**Neu aus Phase 4 — Umbruch (§4.16)**

- **Eine Schriftmessung gehört hinter eine Naht.** „Segoe UI" gibt es unter Linux nicht, und
  ein Schriftartenupdate verschiebt jede Breite. Ein Umbruch-Test gegen echte Maße hat auf
  Windows und im Linux-Lauf der CI verschiedene Ergebnisse und wird nach dem ersten falschen
  Alarm abgeschaltet. Mit `ITdTextMeasure` davor prüfen die Wächter mit festen Maßen und
  bekommen exakte Zahlen; von Skia bleibt eine Plausibilitätsprüfung übrig.
- **Im Umbruch wird in Zentimetern gerechnet, nicht in Pixeln.** Sonst müsste er bei jeder
  Zoomstufe neu laufen und brächte bei jeder ein leicht anderes Ergebnis. Der Zoomfaktor
  gehört an die Stelle, die zeichnet.
- **Drei Stellen, an denen ein Umbruch stehenbleiben kann:** ein Wort breiter als die Zeile,
  eine zusammengehaltene Gruppe höher als eine Seite, und ein Zerlegen, das den Zähler nicht
  weiterrückt. Alle drei brauchen einen Ausweg, der etwas **sichtbar Falsches** erzeugt statt
  gar nichts — ein Lauf, der nicht zurückkommt, meldet keinen Fehler (§4.12).
- **Blocksatz gilt nicht für die letzte Zeile eines Absatzes** — und ein erzwungener
  Zeilenumbruch beendet ebenfalls eine „letzte" Zeile. Ohne das zieht ein Schlusswort über die
  ganze Breite auseinander.
- **Der Leerraum gehört beim Zerlegen ans Wort davor, nicht dazwischen.** Sonst rückt jede
  Zeile nach einem Umbruch um ein Leerzeichen ein, und bei Blocksatz fällt das sofort auf.
- **Absatzabstände gehören zur ersten bzw. letzten Zeile**, nicht zwischen die Zeilen — nur
  so wandern sie mit, wenn ein Absatz auf die nächste Seite rutscht. Beim Abstand *davor*
  muss die Grundlinie mitwandern, sonst sitzt der Text im Abstand.
- **Wer mit runden Zahlen rechnet, muss die Vorgabewerte kennen.** „Zehn Zeilen à 1 cm passen
  auf 10 cm" ergab sieben — `TdParaFormat.Standard` hat 8 pt Abstand nach jedem Absatz. Der
  Test prüfte die Vorgabe mit, ohne es zu wissen.

**Neu aus Phase 4 — Dokumentformat und DOCX (§4.14, §4.15)**

- **DOCX legt die Seiteneinrichtung unsymmetrisch ab.** Die des **letzten** Abschnitts steht
  am Ende des Körpers, die aller anderen im `pPr` ihres jeweils letzten Absatzes. Wer alle ans
  Körperende hängt, bekommt ein Dokument mit genau **einer** Seiteneinrichtung — und merkt es
  erst am Ausdruck. Beim Lesen gilt dasselbe rückwärts: der Absatz, der die `sectPr` trägt,
  ist **Inhalt** und kein bloßer Träger; wer ihn überspringt, verliert je Abschnitt eine Zeile.
- **Word leitet die Ausrichtung nicht aus den Maßen ab.** Ohne `w:orient` dreht es ein quer
  eingetragenes Blatt beim Drucken wieder hoch. Die Datei sieht dabei richtig aus, nur der
  Ausdruck nicht.
- **`{SEITE}` als Text steht auf jeder Seite gleich da.** Kopf- und Fußzeilen brauchen echte
  Felder (PAGE, NUMPAGES) plus `UpdateFieldsOnOpen`. Und den Weg **zurück**: ohne ihn kommt
  aus einem Rückimport die beim Schreiben eingesetzte „1" als gewöhnlicher Text.
- **Zwei Wahrheiten über dieselbe Sache driften auseinander — auch im Kleinen.** Deshalb hat
  `TdPageSetup` weder einen Formatnamen (`"A4"` wird aus der Größe zurückerkannt) noch einen
  Querformat-Schalter (breiter als hoch = quer). Der heutige Editor speichert beides und
  rechnet die Größe aus dem Namen — das bricht beim ersten fremden DOCX, dessen `sectPr`
  Zahlen nennt und keinen Namen.

- **Dasselbe Paket kann je Ziel-Framework eine andere Abhängigkeitskette haben.**
  `DocumentFormat.OpenXml` zieht auf reinem `net10.0` ein `System.IO.Packaging` aus NuGet
  nach — mit zwei bekannten Lücken (NU1903). Unter `net10.0-windows` mit WPF kommt dieselbe
  Klasse aus dem Framework, und es wird gar kein Paket geholt. Der WPF-Kopf benutzt OpenXml
  seit jeher; aufgefallen ist es erst, als **Core** die Referenz bekam. **Eine Lücke taucht
  dort auf, wo man sie nicht sucht** — behoben über das transitive Pinning wie bei
  SQLitePCLRaw. Für den iOS-Kopf gilt dasselbe noch einmal.
- **`<w:b/>` ohne `w:val` heißt „an".** Ein `Val?.Value ?? false` beim Lesen macht damit jede
  fette Stelle eines **fremden** Dokuments still normal — und nur die: die eigenen Dateien
  schreiben `val` immer mit, das Fehlerbild tritt also erst beim Import aus Word auf und nie
  im eigenen Roundtrip. Gilt genauso für `i`, `strike`, `keepNext` und `pageBreakBefore`.
  Wächter: `Eine_Auszeichnung_ohne_Wert_gilt_als_gesetzt`.
- **Ein hängender Einzug ist in DOCX kein negativer Einzug**, sondern ein eigenes Feld
  (`w:hanging` statt `w:firstLine`), und beide sind positiv. Wer nur eines schreibt, macht
  aus −0,5 cm ein +0,5 cm.
- **Ohne `xml:space="preserve"` fallen führende und mehrfache Leerzeichen weg** — nach dem
  Speichern, nicht beim Tippen.
- **Die Reihenfolge der Kindelemente in `w:rPr` und `w:pPr` ist Schema.** Vertauscht ergibt
  sie kein schiefes Bild, sondern eine Datei, die Word nicht öffnet. Deshalb prüft
  `TdDocx.Pruefen` mit dem `OpenXmlValidator` gegen Office 2019 — dieselbe Messlatte, die
  `DocxExporter` seit jeher anlegt.
- **Eine Toleranz gehört an die Auflösung des Formats, nicht an die der Fließkommazahl.**
  Ein Twip ist 1/1440 Zoll = **0,0018 cm**; aus 1,5 cm werden 850 Twips und daraus wieder
  1,4993 cm. Ein Roundtrip-Test, der drei Nachkommastellen verlangt, prüft nicht den eigenen
  Code, sondern die Auflösung eines fremden Formats — und wird nach dem ersten falschen
  Alarm abgeschaltet. Zentimeter deshalb auf zwei Stellen, Punkt auf drei (`pt·20` geht auf).
- **`null` heißt im Dokumentmodell „nicht gesetzt" und nicht „Standardwert".** Wer das
  einebnet, kann Fett in einer fetten Überschrift nie wieder abschalten. Deshalb schreibt
  `TdJson` mit `WhenWritingNull` — **das Gegenteil von `GonkJson`**, und aus dem
  entgegengesetzten Grund: dort wäre „Feld fehlt" gegen „Feld ist 0" der Verlust, hier ist
  genau diese Unterscheidung der Inhalt.

**Neu aus Phase 3 — Testen**

- **Ein Testlauf, der nicht zurückkommt, ist ein Fehlschlag und kein langsamer Rechner.**
  `dotnet test` gibt seine Zusammenfassung erst am Ende aus; eine Endlosschleife in einem
  einzigen Test sieht deshalb genauso aus wie ein Rechner, der sich Zeit lässt. In dieser
  Runde lief er über zwanzig Minuten, bevor jemand nachsah — der ganze Durchlauf dauert
  **7 Sekunden** (§4.12, `Eine_Tabelle_braucht_ihre_Trennzeile`).
  **Merksatz: dauert ein Lauf um Größenordnungen länger als sonst, ist er hängen geblieben.**
- **Zwei `dotnet test` gleichzeitig auf demselben Testprojekt blockieren einander.** Sie
  teilen sich Ausgabeordner und MSBuild-Knoten. Sieht aus wie dieselbe Endlosschleife und
  ist eine andere Ursache — beim Suchen zuerst nachsehen, ob überhaupt nur ein Lauf läuft
  (`pgrep -a dotnet`).

---

## 8. Schnellstart-Befehle

```powershell
cd C:\Dev\Zed\gonk-note-V2

dotnet build -c Release      # 0 Fehler / 0 Warnungen
dotnet build -c Debug        # schneller, ohne Self-Contained/win-x64

dotnet test -c Release       # beide Testprojekte, 196 Tests

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

# Ziehen (seit §4.13) -- ein Lasso um drei Elemente, danach die Auswahl verschieben:
.\tools\kette.ps1 -AppPid <pid> -Schritte '886,465>1698,465>1698,1481>886,1481>886,470',`
                                          '1292,712>1437,857'
```

Schritte sind `"x,y"`, `"x,y,2"` (Doppelklick), `"x,y,r"` (Rechtsklick), `"#TASTEN"`
(SendKeys) oder **`"x1,y1>x2,y2>…"` (Ziehen über einen Pfad)**. **Koordinaten sind echte
Bildschirmpixel** — `SetProcessDPIAware()` steht in jedem Skript, der Rechner läuft auf 200 %.

**Ohne den Zieh-Schritt ist die Auswahl nicht fernsteuerbar** — ein Klick allein erzeugt
keine. Zwischen den Stützpunkten wird interpoliert, weil die App ihre Lassopunkte aus
Mausbewegungen sammelt (§7, „Ziehen und Untermenüs").

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
| V2-20 | 2026-08-04 | **Phase 4, Schritt 2 abgeschlossen: die Layout-Rechnung** (§4.16). `TdLayout` macht aus einem `TdDocument` Seiten mit Zeilen mit Stücken — jedes mit Ort und Maß, **durchgehend in Zentimetern und nicht in Pixeln**: ein Umbruch in Pixeln müsste bei jeder Zoomstufe neu laufen und brächte bei jeder ein anderes Ergebnis. **Die tragende Entscheidung ist eine Naht: `ITdTextMeasure`.** „Segoe UI" gibt es unter Linux nicht, und ein Schriftartenupdate verschiebt jede Breite — ein Umbruch-Test gegen echte Maße hätte auf Windows und im Linux-Lauf der CI verschiedene Ergebnisse und wäre nach dem ersten falschen Alarm abgeschaltet (dieselbe Überlegung, aus der §4.6 die Schrift aus den Renderer-Snapshots heraushält). Mit fester Messung — jedes Zeichen 1 cm, jede Zeile 1 cm — wird „nach zehn Zeichen umbrechen" zu einer Zahl, die stimmt oder nicht; von Skia bleibt `TdSkiaMeasure` mit einer Plausibilitätsprüfung übrig. **Benannte Abweichung von der Roadmap:** sie nennt SKShaper/HarfBuzz, hier misst `SKFont.MeasureText` — das trägt Kerning und reicht für Latein; HarfBuzz wird gebraucht, sobald arabische oder indische Schrift dazukommt, und die Naht ist genau die Stelle zum Austauschen. **„Nicht vom nächsten Absatz trennen" war im ersten Anlauf toter Code** — eine Liste zurückgehaltener Zeilen, die nie gefüllt wurde; aufgefallen beim Nachlesen, nicht im Test. Richtig ist eine **Gruppe**: solange ein Absatz `KeepWithNext` trägt, sammeln sich seine Zeilen samt denen der folgenden, und erst ein Absatz ohne bindet sie ab und setzt sie **am Stück**. Die Absatzabstände gehören dabei zur ersten bzw. letzten Zeile, nicht dazwischen — nur so wandern sie mit; beim Abstand davor muss die Grundlinie mitwandern, sonst sitzt der Text im Abstand. **Drei Stellen, an denen der Umbruch stehenbleiben könnte, haben einen Ausweg bekommen** (ein Wort breiter als die Zeile, eine Gruppe höher als eine Seite, ein Zerlegen ohne Fortschritt): alle erzeugen etwas **sichtbar Falsches** statt gar nichts — ein Lauf, der nicht zurückkommt, meldet keinen Fehler (§4.12). Dazu: Blocksatz lässt die letzte Zeile in Ruhe, der Leerraum am Zeilenanfang fällt weg, `PageBreakBefore` auf dem ersten Absatz erzeugt keine leere Seite, ein leerer Absatz hat trotzdem Höhe, jeder Abschnitt beginnt auf einer neuen Seite. **Drei Wächter waren zuerst rot und hatten unrecht:** „zehn Zeilen à 1 cm passen auf 10 cm" ergab sieben, weil `TdParaFormat.Standard` 8 pt Abstand nach jedem Absatz hat — der Test prüfte die Vorgabe mit, ohne es zu wissen. **27 neue Wächter, jetzt 234** (221 Core + 13 WPF), alle drei Projekte 0 Warnungen. **Als Nächstes: Schritt 3, Listen** |
| V2-19 | 2026-08-04 | **Phase 4, Schritt 2, erster Teil: Abschnitte und Seiteneinrichtung** (§4.15). **Zwei Nutzer-Entscheidungen vorab eingeholt**, beide betrafen das Dateiformat und ließen sich nicht aus dem Bestand ableiten: (1) Die Seiteneinrichtung **wandert in `TdSection`**, die Felder an `TextDoc` werden zur Quelle der Übernahme — das kauft mehrere Abschnitte je Dokument (Deckblatt quer, Rest hoch) und einen DOCX-Import, der `sectPr` vollständig liest statt die zweite Einrichtung wegzuwerfen. (2) Die **Übernahme der Bestandsdokumente kommt zuletzt**, nach Schritt 6: RTF und XamlPackage tragen Tabellen, Bilder und Diagramme, und eine Übernahme davor wäre stiller Datenverlust. **Wichtig für den nächsten Leser:** bis dahin stehen `TextDoc` und `TdPageSetup` nebeneinander, und das ist **nicht** die Doppelung aus §4.10 — dort gab es zwei Rechnungen für dieselbe Sache, hier zwei Formate für verschiedene Dateien. Wer eine davon aufräumt, macht Bestandsdokumente unlesbar. **Zwei Felder gibt es bewusst nicht**, beide Male weil zwei Wahrheiten über dieselbe Sache auseinanderdriften: keinen Formatnamen (`"A4"` wird über `Name` aus der Größe zurückerkannt, Toleranz 0,1 cm, weil ein Blatt durch Twips geht) und keinen Querformat-Schalter (breiter als hoch = quer). Der heutige Editor speichert beides und rechnet die Größe aus dem Namen — das bricht beim ersten fremden DOCX, dessen `sectPr` Zahlen nennt. **Die teuerste Eigenheit dieses Teils:** DOCX legt die Seiteneinrichtung **unsymmetrisch** ab — die des letzten Abschnitts am Körperende, die aller anderen im `pPr` ihres letzten Absatzes. Wer alle ans Körperende hängt, bekommt ein Dokument mit genau einer Einrichtung und merkt es erst am Ausdruck; wer beim Lesen den Träger-Absatz überspringt, verliert je Abschnitt eine Zeile. Beides hat jetzt einen eigenen Wächter, ebenso der Abschnitt, der mit einem Seitenumbruch endet. **Kopf- und Fußzeile gehen durch echte Felder** (PAGE/NUMPAGES plus `UpdateFieldsOnOpen`) und beim Lesen wieder zurück zu `{SEITE}`/`{SEITEN}` — als Text stünde auf jeder Seite dieselbe Zahl. Das **Wasserzeichen bleibt an `TextDoc`**: es ist ein Bild, und Bilder sind Schritt 6. **16 neue Wächter, jetzt 212** (199 Core + 13 WPF), alle drei Projekte 0 Warnungen. **Dabei einmal PowerShell 5.1 in die Umlaut-Falle getappt**: `Get-Content -Raw` ohne `-Encoding` liest UTF-8 als ANSI, und das Zurückschreiben macht Mojibake aus jedem Umlaut — Quelldateien gehören durch das Edit-Werkzeug, nicht durch die Shell. **Ausstehend in Schritt 2:** die Layout-Rechnung (Zeilen- und Seitenumbruch); erst damit bekommt `TextHoeheCm` einen Abnehmer |
| V2-18 | 2026-08-04 | **Phase 4 beginnt: Schritt 1 — Absätze und Zeichenformate** (§4.14), strikt in der Reihenfolge aus Roadmap §5. **Der Befund, der die Phase begründet, steht jetzt schwarz auf weiß:** `TextDoc.Rtf` enthält RTF **oder ein WPF-`XamlPackage`** — das Speicherformat der Textdokumente ist Windows, in Bytes gegossen. Solange das so ist, bleiben Textdokumente unter Linux ausgegraut, egal wie gut die Oberfläche wird; Phase 4 ist deshalb kein Editor-Umbau, sondern ein Formatwechsel und gehört in denselben Vorsichtsbereich wie §4.8. Neu in `Core/Text/`: **`TdDocument` → `TdBlock`(`TdParagraph`, `TdPageBreak`) → `TdInline`(`TdRun`, `TdLineBreak`)** mit `TdCharFormat`/`TdParaFormat`. **Die tragende Entscheidung: `null` heißt „nicht gesetzt" und nicht „Standardwert"** — sonst könnte man Fett in einer fetten Überschrift nie wieder abschalten, und jede spätere Änderung an der Überschrift ginge an allen geschriebenen Läufen vorbei. Gerechnet wird über `Over()`, **dasselbe Muster wie `ThemeDefinition.Over`**, vierstufig: Stück → Absatz → Dokument → Standard. Das Speicherformat (`TdJson`, Kennung `GNTD` + Json über den Source-Generator) schreibt deshalb mit `WhenWritingNull` — **dem Gegenteil von `GonkJson`**, und aus dem entgegengesetzten Grund. Die **Diskriminatoren sind kurz** (`"p"`, `"run"`, …) statt „Namensraum.Typ, Assembly": die lange Form bei `WbElement` ist ein LiteDB-Erbe, dieses Format hat keines — Namen für die späteren Schritte sind reserviert und wörtlich festgenagelt. **`Section` fehlt bewusst** (die Seiteneinrichtung steht an `TextDoc`, zwei Fassungen wären §4.10), **`TdPageBreak` steht bewusst schon da** (der heutige Editor kann ihn einfügen, und ein verschluckter Seitenumbruch fällt erst beim Drucken auf). **Das Tor, das die Roadmap nach jedem Schritt verlangt, ist gebaut:** `TdDocx` schreibt und liest DOCX gegen das Modell und prüft gegen das Office-2019-Schema. **Warum DOCX und nicht der eigene Roundtrip:** ein fremdes Format kennt die eigenen Bequemlichkeiten nicht — es hat sofort vier Stellen aufgedeckt, die jetzt eigene Wächter haben (hängender Einzug ist in DOCX ein eigenes Feld und kein Vorzeichen; `<w:b/>` **ohne** `val` heißt „an", was nur bei fremden Dateien zuschlägt; Leerzeichen am Rand brauchen `xml:space="preserve"`; „Hervorhebung aus" ist die Füllung `auto` und nicht das Fehlen des Elements). **Eine Sicherheitslücke gefunden, die vorher gar nicht auftauchen konnte:** Core bekam `DocumentFormat.OpenXml` und damit `System.IO.Packaging` 8.0.0 mit zwei Lücken (NU1903) — unter `net10.0-windows` mit WPF kommt dieselbe Klasse aus dem Framework, es wird gar kein Paket geholt. **Dasselbe Paket kann je Ziel-Framework eine andere Abhängigkeitskette haben**; behoben über das transitive Pinning wie bei SQLitePCLRaw. **Drei Wächter waren zu genau und wurden korrigiert, nicht der Code:** ein Twip ist 0,0018 cm, und ein Roundtrip-Test, der drei Nachkommastellen in Zentimetern verlangt, prüft die Auflösung eines fremden Formats statt des eigenen Codes. **37 neue Tests, jetzt 196** (183 Core + 13 WPF), beide Köpfe 0 Warnungen. **Am laufenden Programm ist hier nichts zu sehen, und das ist richtig so** — das Modell ist noch nirgends angeschlossen, `TextDoc.Rtf` ist unverändert, und die Gegenprobe ist der DOCX-Roundtrip. **Offen und vor Schritt 2 zu entscheiden:** wie die Bestandsdokumente herüberkommen (§6) — Vorschlag: zuletzt und nicht zuerst, weil eine Übernahme vor Schritt 6 stiller Datenverlust wäre |
| V2-17 | 2026-08-04 | **Die zwei benannten Schulden aus Phase 3 sind eingelöst** (§4.13), auf dem Windows-Rechner — dem einzigen, auf dem der WPF-Kopf baut. (1) **`WhiteboardView.Selection.cs` rechnet mit `WbHit` aus Core**: `RotatePt`, `HitElement`, `HitTestElement`, `SegOrPointDist`, `ShapeOutlineDist`, `AllCornersInside`, der Lasso-Kern und `ComputeSelectionBounds` sind als eigene Rechnung verschwunden, 384 → 290 Zeilen. Die **Namen** bleiben als Einzeiler stehen: sie haben elf Aufrufstellen in fünf anderen Partials, und wegkommen sollte die zweite Rechnung, nicht die zweite Bezeichnung. (2) **`MarkdownFlow` ruft `Markdown.Parse`** und malt nur noch den Blockbaum, 339 → 232 Zeilen. **Die Endlosschleife aus §4.12 kam ohne eigenen Handgriff mit** — wer die Grammatik wegwirft, wirft den Fehler mit weg; das ist der Unterschied zwischen „an zwei Stellen reparieren" und „eine Stelle haben". Nebenbei entfällt das `[ThreadStatic]`-Feld für den Dokument-Verweis: es gab es nur, weil Zitatblöcke sich selbst erneut aufriefen, und ein `MdQuote` bringt seinen Inhalt bereits zerlegt mit. (3) **Der WPF-Über-Dialog zeigt den Datenordner** — die Zeile aus §4.12; sie war nötig und nicht kosmetisch, weil `About.Subtitle` seit §4.12 keinen Pfad mehr nennt und die Angabe auf Windows sonst ganz fehlte. **Das Wort „pixelgleich" ist gemessen, nicht behauptet:** derselbe Prüflauf gegen dieselbe Master-Datenbank, einmal mit dem alten Stand (`git stash` auf die eine Datei) und einmal mit dem neuen — **alle drei Bildpaare Pixel für Pixel identisch, 2906×1826, null Abweichung**. Ein Testlauf hätte das nicht hergegeben: er prüft, was er kennt, der Bildvergleich prüft alles, was auf dem Schirm steht. **Für Phase 4 ist das jetzt das Muster.** Dafür musste `kette.ps1` erst **ziehen** lernen (`"x1,y1>x2,y2>…"`, mit Interpolation) — das Linux-Gegenstück kann es seit §4.10, unter Windows fehlte es, und ohne Ziehen ist die Auswahl gar nicht fernsteuerbar. **Zwei Fernsteuer-Fallen dabei gefunden** (§7): die PowerShell-Pipeline entpackt verschachtelte Arrays, sodass `SetCursorPos` ein Array statt einer Zahl bekommt; und ein Klick auf einen Untermenü-Eintrag **schließt** das Untermenü, wenn der Hover es schon geöffnet hat — der nächste Klick landet dann darunter, und auf dem Foto sieht man die Ursache nicht, weil der Hover das Menü bis zur Aufnahme wieder öffnet. Geprüft ohne echte Daten, beide Markdown-Dialoge in **beiden** Sprachen (Überschriften, verschachtelte Listen, Zitat, Code-Block, **Tabelle**, und der Verweis „Erste Schritte" öffnet die Anleitung). **159 Tests unverändert grün** — nichts dazugekommen, zwei Fassungen weniger; `TrefferTests` und `MarkdownTests` bewachen ab jetzt beide Köpfe |
| V2-16 | 2026-08-04 | **Nachlese zu V2-15, wieder auf Nutzerwunsch.** (1) **Die große Ordner-Kachel bekommt ihre eigene Strichstärke** (`4,5` statt `0,7`, `MainWindow.axaml`). Grund und allgemeine Lehre: **`StrokeThickness` wächst nicht mit `Stretch` mit** — der Wert steht in Gerätepunkten und nicht in denen der Geometrie, weshalb dieselben 0,7, die im Baum bei 16 px kräftig aussehen, bei 118 px ein Haarstrich sind. Wer künftig eine Vektorform groß zeigt, muss die Stärke dort eigens setzen. In der Seitenleiste bleibt alles wie es war. (2) **`Icon.Textmarker` im dritten Anlauf**, jetzt **oben offen**: der Schaft ist am oberen Rand abgeschnitten, zwei Stege stehen über der Bandlinie, unten die Keilspitze. Genau daran lagen die ersten beiden Fassungen daneben — eine geschlossene Kappe macht daraus einen Stift mit Deckel. **Neu als Arbeitsweise:** eine Form beurteilt man schneller, indem man dieselbe Pfadangabe als `.svg` groß rendert (`magick … -filter point -resize 400x`), als indem man die App neu startet; die Gegenprobe am laufenden Programm bleibt Pflicht, aber einmal statt bei jedem Zwischenstand. **Dabei eine teure Wayland-Falle gefunden** (§7): **`import -window` liefert manchmal ein Bild, das mehrere Schritte alt ist** — man klickt, das Foto zeigt keine Wirkung, man korrigiert Koordinaten, und in Wahrheit hat schon der erste Klick gesessen. Vier Durchgänge sind so verloren gegangen. Gegenmittel: zwei Aufnahmen hashen und bei Gleichstand eine Aktion mit unübersehbarer Wirkung auslösen |
| V2-15 | 2026-08-03 | **Nachlese zu V2-14, beides auf Nutzerwunsch.** (1) Der **Klon-Befehl zeigt jetzt auf V2** (`GonkNote.git`) — in beiden Erste-Schritte-Fassungen, dazu der Issues-Verweis am Ende. Damit ist §5 „Noch offen" Punkt 3 nach zweimaligem Zurückstellen entschieden; fällig geworden war er dadurch, dass die Anleitung daneben `src/GonkNote.Avalonia` nennt, ein Projekt, das es im V1-Repo nicht gibt. **Das Repo ist weiterhin privat** — der Befehl läuft heute nur für Konten mit Zugriff, und das wird mit §6 „Vor dem Öffentlich-Schalten" richtig. (2) **Zwei Vektorformen zurück auf die V1-Gestalt**: `Icon.Folder` ist wieder der klassische Ordner **mit Reiter** oben links (statt der „modernen" Fassung mit Schräge), `Icon.Textmarker` ein **aufrechter, breiter** Marker mit Bandlinie und Keilspitze (statt eines schräg liegenden, der neben dem Bleistift wie ein zweiter Stift aussah). Gezeichnet **nach dem Zeichen, das der WPF-Kopf dafür benutzt** (Segoe Fluent `E8B7`/`E7E6`) und nicht nach dem Vorschaubild — die Vorlage steht im Repo und ist damit nachprüfbar. Zwei Lehren stehen jetzt am Symbolblock in `Themes/Styles.axaml`: beim Marker ist **die Breite** das Erkennungsmerkmal und nicht die Neigung, und **die Bandlinie braucht Abstand zum oberen Rand** — im ersten Anlauf verschmolz sie bei 16 px mit der gerundeten Kappe zu einem einzigen Strich. In drei Größen am laufenden Programm angesehen (Baum 16 px, Schnellzugriff 21 px, Galerie-Kachel 118 px). Die übrigen neuen Symbole bleiben, wie sie sind — der Nutzer hält sie für besser als die V1-Fassungen. Der WPF-Kopf ist nicht betroffen: er zeichnet weiter mit der Icon-Schrift |
| V2-14 | 2026-08-03 | **Phase 3, Brocken 6 und 7 — der Rest bis M1** (§4.12), auf dem CachyOS-Laptop. **Drag & Drop im Ordnerbaum** (verschieben, mit `Strg` kopieren; leere Fläche = Wurzel), die **einblendbare Titelleiste** des maximierten Fensters und die **Einstellungen-Seitenleiste** der Zeichenfläche (Muster, Farbton, Format, Ausrichtung — **nur** der Seiten-Abschnitt, weil es die anderen Werkzeuge nicht gibt). Dazu das **`EmbeddedDocs`-Gegenstück**: „Hilfe → Erste Schritte" und das gerenderte README erscheinen jetzt auch unter Linux. **Der Markdown-Zerleger ist dabei nach `Core/Text/` gewandert** statt ein zweites Mal abgeschrieben zu werden — er zeichnet kein Pixel (§3), und zwei Fassungen derselben Grammatik driften auseinander, ohne dass es auffällt; jeder Kopf malt nur noch. Wächter `MarkdownTests` (21 Tests, jetzt **146** Core / 159 gesamt) — **er hat sofort eine Endlosschleife gefunden, die auch in `MarkdownFlow` steckt**: eine Tabellenzeile ohne Trennzeile darunter landet im Absatz-Zweig, den sie selbst abweist, sodass `Parse` nie weiterrückt. Aufgefallen ist das nicht an einer roten Meldung, sondern daran, dass der Testlauf nicht mehr zurückkam (§7, neu). **Drei weitere Avalonia-Eigenheiten** (§7): Ziehen läuft auch in der App über XDND, weshalb es ein prozessinternes `DataFormat` braucht; `DoDragDropAsync` verlangt die `PointerPressedEventArgs`; ein anklickbarer Verweis ist ein Steuerelement (`InlineUIContainer`) und kein `Run`. Die Titelleiste braucht hier **keinen** MinMax-Hook — X11 maximiert gegen `_NET_WORKAREA`, `WindowBounds` bleibt zu Recht Windows-only. **Zwei Texte am laufenden Programm als falsch entlarvt** und in beiden Tabellen behoben: `About.Subtitle` nannte fest `%APPDATA%\GonkNote` (direkt über der Zeile mit dem echten Ordner), und der Werkzeugtipp der Seitenleiste versprach vier Abschnitte, von denen es einen gibt (neuer Schlüssel `Wb.Settings.PageTip`). **Ein eigener Fehler dabei gefunden:** die Leiste ging mit lauter leeren Umschaltern auf — `EinstellungenSpiegeln` wurde vor dem Sichtbarmachen gerufen und stieg deshalb sofort wieder aus. **Nutzer-Entscheidung: M1 wird ausgerufen** — und im selben Zug sind **alle vier mitgelieferten Dokumente** auf beide Ausgaben erweitert worden (Dauerregel 1): neuer Abschnitt „Zwei Ausgaben, eine App" mit einer Tabelle, was der Linux-Ausgabe fehlt und warum, dazu Bau-, Pfad- und Sicherungsanweisungen je System. Geprüft ohne echte Daten, in **beiden** Sprachen, jeder Schritt mit `tools/linux/` fotografiert. **Offen bleibt** der V1-Klon-Befehl in beiden Erste-Schritte-Fassungen (§5, Punkt 3) — er fällt jetzt stärker auf, weil daneben `src/GonkNote.Avalonia` steht |
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
