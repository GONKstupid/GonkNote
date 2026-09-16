[← Index: HANDOFF.md](../HANDOFF.md)

## 2. Stand

> **Diese Liste ist absichtlich kurz.** Bis zum 2026-08-11 stand hier jede Runde noch einmal
> in eigenen Worten — dieselbe Chronologie wie in §4 (das Warum), §6 (die Häkchen) und §9
> (die Chronik), also **viermal**. Was viermal dasteht, wird dreimal nicht gepflegt: die
> Testzahlen hier hinkten zuletzt um zwei Runden hinterher. **Der Stand steht jetzt einmal;
> die Geschichte steht in §9.**

| | |
|---|---|
| **Version** | **1.0.4** · `net10.0` · SkiaSharp 3.119.4 · Avalonia 12.1.1 · SQLite — 1.0.0 in V2-125, dann 1.0.1 (V2-129), 1.0.2 (V2-129b), 1.0.3 (V2-132), **1.0.4 in V2-135** (Rechtschreibprüfung unter Linux + Schriftvorschau). Die Stellen führt der Kommentar in `Directory.Build.props`; **`About.Version` trägt diesmal mit** — der Satz dahinter nannte die Rechtschreibprüfung als Nächstes und tut es nicht mehr. **Die Über-Dialoge sind bei 1.0.4 nicht gegengeprüft** — kein Windows-Rechner, und der Linux-Über-Dialog ist nicht am laufenden Programm gesehen worden. |
| **Tests** | **1355** — **1287** in `GonkNote.Core.Tests` (hier gemessen), **68 gerechnet** in `GonkNote.Wpf.Tests`. ⚠ **Die WPF-Zahl ist seit V2-128 nicht mehr messbar** — es gibt keinen Windows-Rechner (§0); sie wird fortgeschrieben, nicht gezählt. Stand V2-130 (§4.107: +26 in Core, in WPF vier Fälle weg und drei neu). Die Zeile darunter beschreibt den Stand von V2-125 und bleibt als Begründung stehen: 1244 in `GonkNote.Core.Tests`, 69 in `GonkNote.Wpf.Tests` (alles, was am `FlowDocument` hängt) · Stand V2-125. **+5**, und alle fünf halten dieselbe Sache fest: **eine Sortierung darf nicht von der Kultur des Rechners abhängen** (§4.101). Einer wurde außerdem *nachgezogen* (der vierte README-Verweis der Anleitung). **+38 in dieser Runde**: der Markdown-Import in Core (23), die neue Grammatik (6), die Tabellensperre (3), die zwei nachgereichten Wächter für die Lücken aus §4.21, und vier für die Dokumentverweise im WPF-Projekt |
| **Bau** | Debug und Release je 0 Fehler / 0 Warnungen; CI mit zwei Läufen (Windows, Ubuntu) — **beide grün seit dem 2026-09-05**. ⛔ **Sie waren es vier Tage lang nicht**, und keine der neun Runden dazwischen hat es bemerkt (§4.101): ein Wächter fiel nur auf `en-US`. **Die CI benennt jetzt bei jedem Fehlschlag die gefallenen Wächter als Annotation** — die ist ohne Anmeldung lesbar, die Protokolle sind es nicht |
| **Meilensteine** | ✅ **M0** (Core baut auf Linux) · ✅ **M1** (Notizbuch und Whiteboard laufen unter Linux) · ✅ **M2** (Funktionsgleichheit Linux ↔ Windows) — **ausgerufen am 2026-08-28**, Nutzer-Entscheidung, **mit einem benannten Loch**: die Rechtschreibprüfung fehlte im Linux-Kopf (§6, bewusst so entschieden am 2026-08-22) — **seit V2-135 / 1.0.4 ist es zu** (Phase 5.1). Die zweite Stift-Taste darf verschieden bleiben und ist **kein** Loch (§5 Nr. 17) · ▶ **M3** (veröffentlicht) — **das Repo ist seit dem 2026-09-05 öffentlich**, Phase 5 ist zu; **ausgerufen wird M3 mit dem Tag `v1.0.0`**, und den setzt der Nutzer (§4.101) |

**Wo das Projekt steht:**

| Phase | Stand | Wo es beschrieben ist |
|---|---|---|
| **0** — Umzug in die `src/`-Struktur | ✅ | unten in diesem Abschnitt, dazu §3 |
| **1** — Netz einziehen (Tests, Golden-Files, CI) | ✅ | §4.6, Häkchen in §6 |
| **2** — die große Entkopplung (Platform-Naht, eigene ViewModels, LiteDB → SQLite) | ✅ | §4.7, §4.8 |
| **3** — Avalonia-Shell für Linux · **M1** | ✅ | §4.9 – §4.12, die zwei Schulden in §4.13 |
| **4** — eigene Dokument-Engine | ✅ **abgeschlossen 2026-08-11** | §4.14 – §4.28 |
| **4.5** — die fehlenden Werkzeuge des Linux-Kopfs · **trägt M2** | ✅ **abgeschlossen 2026-08-28** — sechs von sechs Stücken, auf **beiden** Systemen gegengeprüft (§4.64) | §4.51 – §4.64, §6 |
| **5** — ① UI/UX angleichen · ② Rückmeldung · ③ Flatpak/AppImage · ④ aufräumen und prüfen · ⑤ veröffentlichen · **M3** | ▶ **an der Reihe, Schritt ⑤** — ① ist **zu** (①a §4.71/§4.74/§4.75, ①b §4.72–§4.75, ①c §4.77–§4.92), **② ist zu** (§4.93, §4.94, Reparaturen §4.95), **③ ist gebaut und gestartet** (§4.96 — Flatpak **und** AppImage; der Beipack in §4.98/V2-122 auf dem Laptop gemessen), **④ ist zu** (§4.97 und §4.99, **samt vollständigem Prüflauf**), und **der zweite Bau ist gelaufen** (§4.100/V2-124, beide Pakete) — **das ist der, der hinausgeht**. **⑤ ist gelaufen** (§4.101/V2-125): Version **1.0.0**, READMEs mit Installationswegen und Bildschirmfotos, **GitHub Pages** in `site/`, **`release.yml`**, Repo-Beiwerk — **und das Repo ist öffentlich**. **▶ Offen sind nur noch drei Handgriffe am Konto des Nutzers:** Tag `v1.0.0` schieben, Pages auf „GitHub Actions“ stellen, Beschreibung und Topics setzen | §6 |
| **5.1** — Rechtschreibprüfung im Linux-Kopf | ✅ **abgeschlossen 2026-09-16 (V2-135), ausgeliefert als 1.0.4** — Hunspell in reinem C# (`TdRechtschreibung` in Core), Wellenlinien im Zeichner, Sprachwahl und Schalter in der Statusleiste, Verbesserungen im Rechtsklick-Menü; **de_DE und en_US werden mitgeliefert** (`Assets/Dictionaries/`, Lizenzen in `THIRD-PARTY-NOTICES.md`). **Das benannte Loch aus M2 ist zu.** ⚠ **Am laufenden Programm noch nicht gesehen** — belegt ist es über 1303 grüne Tests, darunter drei, die die rote Welle als Bildpunkte nachzählen. *(Diese Zeile hieß bis V2-125 „5.6“; §5 Nr. 22 und §6 sagen beide **5.1**.)* | §6 |
| **6** — iPadOS, Apple Pencil, TestFlight · **M4** | ⏳ offen | §6 |

> **⚠ Die Reihenfolge 5/6 ist am 2026-08-18 getauscht worden** (Nutzer): Veröffentlichung
> **vor** iPadOS. Wer eine ältere Zeile liest, in der Phase 5 „iPadOS" heißt, liest den Stand
> von vorher — der Kasten in §6 erklärt es.

> **▶ Und was seither dazugekommen ist** (Phase 5, Schritt ①c — §4.77 bis §4.92): Dem
> **Linux-Kopf fehlt kein Werkzeug mehr, das der WPF-Kopf hat.** Tafel-Export, Formen-Stift,
> Cover, Suchen & Ersetzen, Diagramm samt Ändern, Navigator, Formatpinsel, Markenauswahl und
> Sonderzeichen, Bild/Infobox/Beschriftung/Objekt-Anordnung/Wasserzeichen und der volle
> Tabellenentwurf. **Was bewusst offen bleibt, steht gesammelt in §4.92.**

**Was Phase 4 am Ende kann** — in einem Satz je Kopf:

- **Windows:** unverändert alles, was vorher ging. Alle vier Exportwege laufen jetzt gegen
  das eigene Modell statt gegen ein `FlowDocument`; der Editor selbst liest weiter aus `Rtf`.
- **Linux:** Notizbuch und Whiteboard zeichnen, **Textdokumente werden angezeigt**,
  importiert (DOCX) und in alle vier Formate exportiert. **Seit §4.35 wird darin geschrieben**
  (Tastatur, Maus, Auswahl, Rückgängig), **seit §4.36 formatiert** (fett bis Einzug) und **seit
  §4.37 auch eingefügt** (Seitenumbruch, Tabelle, Feld, Verweis, Inhaltsverzeichnis) samt
  Tabellenbearbeitung. **Was fehlt, ist die Bildschirmtastatur-Naht — und Schritt 7**, mit dem
  `Rtf` die Führung verliert.

> **`Rtf` ist weiterhin das führende Feld** (§5). Es abzulösen heißt, den WPF-Editor auf das
> Modell umzustellen — dafür fehlt derselbe Schreibweg wie im Linux-Kopf.

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
