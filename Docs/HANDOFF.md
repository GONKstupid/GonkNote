# Gonk Note V2 — Projektübergabe

**Stand: 2026-09-08 (V2-128)** · **▶ NEUER ENTWICKLUNGSRECHNER: LENOVO YOGA 7 2-IN-1 14 IML9 UNTER OMARCHY** (Arch-basiert) — er ersetzt den Windows-Entwicklungsrechner vollständig; auf den CachyOS-Laptop kommt wieder Windows als Gegenprobe-Gerät (noch nicht eingerichtet). **Die App läuft unter Omarchy über das installierte AppImage, mit ein paar bekannten Bugs. Reihenfolge: erst diese Bugs beheben, dann Flathub-Umbau (§4.102) und Phase 5.1.** §5b/§5c/§5d/§5e beschreiben weiterhin den alten Aufbau und sind als solcher markiert. Details in §0, „Hier geht es weiter". **Version 1.0.0, Ziel erreicht · net10.0 · SkiaSharp 3 · SQLite · Avalonia 12 · **▶ PHASE 5 IST ZU — SCHRITT ⑤ IST GELAUFEN, DAS REPO IST ÖFFENTLICH** (§4.101). **Die Version steht auf 1.0.0** — an allen fünf Stellen, und alle vier Über-Dialoge (zwei Köpfe × zwei Sprachen) sind am laufenden Programm gesehen. **READMEs** tragen jetzt die **drei Installationswege** und **sieben Bildschirmfotos**, die aus einer **erfundenen Demo-Datenbank** stammen (`tools/demo-db`, neu) und nicht aus dem echten Bestand. **Projektseite** unter `site/` samt `pages.yml`, **`release.yml`** an einem `v*`-Tag, **Beiwerk** (`CONTRIBUTING`, `SECURITY`, Issue- und PR-Vorlagen). **⛔ Der Fund der Runde saß im eigenen Über-Dialog:** Die längere Versionszeile wurde **abgeschnitten statt umgebrochen** — ein waagerechtes `StackPanel` misst mit **unendlicher** Breite, `TextWrapping` half daher nichts; **in beiden Köpfen** auf `DockPanel` umgestellt. **⛔ Dazu zwei weitere:** ein Wächter meldete den **vierten** README-Verweis der Anleitung, und eine **dreispaltige Tabelle** wird im Hilfe-Fenster abgeschnitten (ersetzt; **derselbe Verdacht steht benannt für Abschnitt 14 der Anleitung, ungeprüft**). **HANDOFF und ganze Git-Historie auf Privates durchgesehen — beides sauber.**  **⛔ UND EIN BEFUND, DER NICHT AUS DIESER RUNDE STAMMT: DIE CI WAR SEIT DEM 2026-09-03 ROT** — neun Läufe, beide Jobs, nur der Testschritt, **hier nicht reproduzierbar** (auch nicht im frischen Klon), und **die Protokolle verlangen Adminrechte**. Statt zu raten, sagt die CI es jetzt selbst: **`::error::`-Annotationen sind bei einem öffentlichen Repo ohne Anmeldung lesbar** — *und der erste Anlauf dieses Schrittes ist selbst gefallen* (`-eo pipefail`; ein `grep` ohne Treffer). **✅ Der Wächter war einer, und er hatte recht:** `TdTableEdit.AlsDatum` las mit `CultureInfo.CurrentCulture` — auf `en-US` scheiterte „15.02.2026", die Spalte galt als **Zahlen**spalte, und daraus wurde **15.022.026**. *Genau die Umkehrung, vor der der Kommentar daneben warnt, und das Ergebnis sieht sortiert aus.* Jetzt fest `de-DE`, dann invariant — wie in `TdTabellenformel.AlsZahl`, **das die gleiche Frage seit jeher so beantwortet**. **Bau 0/0, 1313 Tests (1244 Core + 69 WPF), +5 — und beide CI-Jobs grün.** ▶ **Drei Handgriffe bleiben beim Nutzer und gehören seinem Konto:** den **Tag `v1.0.0` schieben** (er löst das Release aus), **Pages auf „GitHub Actions" stellen**, **Beschreibung und Topics** setzen. **⛔ NACHGETRAGEN AM 2026-09-06 (V2-126, §4.102): Flathub ist nicht bloß „noch nicht eingereicht" — das Manifest erfüllt die Anforderungen nicht.** Flathub baut **aus dem Quellcode und ohne Netz**; unseres packt ein fertiges `dotnet publish`-Ergebnis ein. **Das ist ein Umbau** (Zuschnitt in §6, „Vorgemerkt: Flathub") — *und er blockiert nichts.* **⛔ Dabei ist eine Begründung im Manifestkopf als falsch nachgewiesen:** `org.freedesktop.Sdk.Extension.dotnet10` **gibt es**, mit `branch/25.08` und SDK 10.0.300 GA. **⛔ UND DIE INSTALLATIONSPROBE HAT EINEN FUND (V2-127, §4.103):** Das Windows-Paket ist **gemessen** — wie `release.yml` gebaut, entpackt, aus fremdem Ordner mit frischer Datenbank gestartet. **Aber der Ordner `Fonts` stand in keiner Kopieranweisung**, und ohne ihn zeichnet der Kopf in **Segoe UI** — §4.72 rückwärts, ohne jeden Hinweis. In allen vier Dokumenten behoben. **⛔ Dazu zwei Zerleger-Funde:** ein **Blockzitat in einem Listenpunkt** wird nicht erkannt (die `>` stehen wörtlich da — *auf GitHub sieht es richtig aus*), und **Backticks im Linktext** überleben wörtlich. **Linux ist von hier aus geprüft, soweit es geht:** Kreuzbau 341 Dateien / 155 MB, alle Bestandteile da, die drei Startpfade stimmen überein — **der Start selbst wartet auf das erste Release.** ▶ **Der Laptop ist nicht dran** — der Flathub-Umbau bestellt ihn aber, sobald er gemacht wird.**

> **📌 Dauerregeln des Nutzers — gelten immer, ohne Nachfragen:**
>
> 1. **Doku-Pflege — mitgelieferte Dokumente, immer paarweise:**
>
>    | Dokument | Gegenstück | wird angezeigt unter |
>    |---|---|---|
>    | `README.md` (DE) | `README.en.md` (EN) | **Hilfe → Über Gonk Note** |
>    | `Docs/ERSTE-SCHRITTE.md` (DE) | `Docs/GETTING-STARTED.md` (EN) | **Hilfe → Erste Schritte** |
>    | `Docs/INSTALLIEREN.md` (DE) | `Docs/INSTALL.md` (EN) | nur Repo + README, **nicht in der App** |
>
>    - **Seit V2-131 liegen die vier ersten unter `Docs/`** (per `Link=` im csproj bleibt
>      der Resource-Name flach — `EmbeddedDocs` und `IsGuideLink` sind unverändert). Die
>      Anleitung ist in **Installation** (`INSTALLIEREN.md`/`INSTALL.md`, nur Repo) und
>      **Bedienung** (`ERSTE-SCHRITTE.md`/`GETTING-STARTED.md`, weiterhin in der App)
>      geteilt.
>    - **Nie nur eine Sprache ändern.** Beide Fassungen müssen inhaltlich auf demselben
>      Stand sein. `EmbeddedDocs` wählt nach `Loc.Current` und fällt sonst auf Deutsch
>      zurück — eine halbfertige Übersetzung fällt also nicht auf, sie zeigt einfach still
>      den alten oder den deutschen Text.
>    - **Nach jeder Änderung den zugehörigen Dialog in der App gegenprüfen** (beide
>      Sprachen, Ansicht → Sprache). Alle vier liegen als eingebettete Resource in der Exe,
>      Textänderungen erscheinen also ohne Code-Änderung — genau deshalb merkt man einen
>      kaputten Umbruch, einen toten Verweis oder eine fehlende Übersetzung **nur im
>      laufenden Programm**.
>    - Von Hand zu pflegen ist nur die Versions-/Phasenzeile — sie steht seit §4.5 als
>      Schlüssel **`About.Version` in `LocGerman` und `LocEnglish`** und nicht mehr im Code
>      der Dialoge. **Beide Tabellen zusammen ändern**, danach beide Dialoge in beiden
>      Sprachen gegenprüfen — und zwar in **beiden Köpfen** (§7, „Der Kopf trägt seine eigene
>      Kopie von Core").
>    - Verweise zwischen den Dokumenten laufen über `EmbeddedDocs.GuideLinkDe/-En`
>      (`ERSTE-SCHRITTE.md` / `GETTING-STARTED.md`) — wer eine Datei umbenennt, muss dort
>      nachziehen.
> 2. **Antwort-Stil:** Erledigtes **sehr kurz und stichpunktartig** melden. **Ausführlich nur
>    bei offenen Fragen und Entscheidungen**, die der Nutzer treffen muss — die dafür klar
>    begründen.
> 3. **Sprache:** durchgehend Deutsch — UI, Kommentare, Commits, diese Datei.
>
>    *(Die frühere **Dauerregel 3a** — „am Ende jeder Antwort sagen, ob der Laptop dran ist" —
>    ist am **2026-09-09 vom Nutzer gestrichen**. **Den CachyOS-Laptop gibt es nicht mehr.**
>    Damit gibt es kein zweites Gerät, an das ein Schritt gehen könnte, und die Schlusszeile
>    beantwortet keine Frage mehr. Was sie einmal geregelt hat, steht in §9 unter V2-31 und
>    V2-129; **§5b/§5d/§5e sind damit vollständig Historie** — siehe §0.)*
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
> **⚠️ Diese Datei bleibt im Repo — auch wenn es öffentlich wird.** Nutzer-Entscheidung vom
> 2026-07-29 (eingecheckt, damit sie zwischen Windows und CachyOS-Laptop mitwandert),
> **bestätigt und zugeschnitten am 2026-08-28** (§5 Nr. 21): Sie liegt seit V2-93 unter
> **`Docs/HANDOFF.md`** und nicht mehr in der Wurzel — sie soll **keine der Hauptdateien auf
> der Repo-Startseite** sein, denn sie ist eine reine Arbeitsdatei. **Aus dem Baum genommen
> wird sie nicht**: sie muss mitwandern, heute zwischen zwei Rechnern und mit Phase 6
> zwischen dreien.
>
> **⛔ ENTSCHIEDEN IST AUCH: KEIN HISTORY-REWRITE.** Sie darf über `git log` und im Repo
> lesbar bleiben. *(In V1 ist umgekehrt entschieden worden — dort ist sie ausgeschlossen und
> die History umgeschrieben; V1-Handoff, Kopfzeile.)*
>
> **⚠ Daraus folgt eine Pflicht und keine Ersparnis.** Ohne Rewrite ist **jede** Fassung
> dieser Datei öffentlich lesbar, auch jede frühere. Die Regel gilt damit schärfer als je
> zuvor: **nichts hineinschreiben, was nicht irgendwann öffentlich stehen darf** — keine
> Pfade zu privaten Daten, keine Zugangsdaten, keine Inhalte aus den Schulunterlagen.
> **Bisher war sie durch die *Möglichkeit* eines Rewrites abgesichert; diese Absicherung
> fällt weg.** Vor dem Öffentlich-Schalten wird die Datei deshalb einmal ganz durchgesehen —
> eigener Punkt in der Checkliste in §6.

---

## Wo was steht

Die Datei ist lang, und das bleibt sie: sie trägt die Begründungen, nicht nur die Ergebnisse.
**Damit man nicht suchen muss:**

| § | Was drinsteht | Wann man es braucht |
|---|---|---|
| **0** | Schnelleinstieg, **▶ „Hier geht es weiter"**, Bau- und Testbefehle | **Immer zuerst.** Ein neuer Thread liest nur das und §5 |
| **1** | Auftrag und die Entscheidungen dahinter | einmal, zum Verstehen des Ganzen |
| **2** | Stand: Version, Testzahl, Meilensteine, welche Phase wo steht | „wo stehen wir?" |
| **3** | Struktur der Solution, Faustregel Core ↔ Kopf | vor jeder neuen Datei |
| **4** | **Warum es so ist, wie es ist** — eine Nummer je Runde (§4.1 – §4.41) | wenn eine Entscheidung fremd wirkt |
| **5** | **Entscheidungen** — getroffene als Tabelle, offene als Liste | **vor jeder Rückfrage an den Nutzer** |
| **5a** | Stylus unter Linux: was gemessen wurde und was offen ist | bei allem, was am Stift hängt |
| **5b** | ⛔ **Historie** — wann auf den CachyOS-Laptop gewechselt wurde | nur noch als Begründung; **den Laptop gibt es nicht mehr** |
| **5c** | GitHub-Zugang | wenn `git push` klemmt |
| **5d** | ⛔ **Historie** — 🐧 Arbeitsanweisung für den Laptop | nur noch als Begründung |
| **5e** | ⛔ **Historie** — 🪟 Arbeitsanweisung für den Windows-Rechner | nur noch als Begründung; **es gibt nur diesen einen Rechner** |
| **6** | Arbeitsplan: Häkchen je Phase, Vorgemerktes, Veröffentlichung | „was kommt als Nächstes?" |
| **7** | **Fallen** — was schon einmal weh getan hat | **vor jeder Code-Änderung überfliegen** |
| **8** | Schnellstart-Befehle (Bauen, Testen, Fernsteuern, DB-Kopie) | zum Kopieren |
| **9** | Chronik — eine Zeile je Runde, neueste zuerst | „was ist wann passiert?" |

---

## Wo die Abschnitte jetzt liegen

Diese Datei war 19.061 Zeilen. Der Inhalt ist **unverändert**, er liegt nur in
`Docs/handoff/` verteilt. Die Paragraphennummern gelten weiter — ein Verweis wie
„HANDOFF §4.42" steht in [`handoff/4-41-bis-4-50.md`](handoff/4-41-bis-4-50.md).

| Abschnitt | Datei | Zeilen |
|---|---|---|
| 1. Auftrag und die Entscheidungen dahinter | [`handoff/1-auftrag.md`](handoff/1-auftrag.md) | 22 |
| 2. Stand | [`handoff/2-stand.md`](handoff/2-stand.md) | 84 |
| 3. Struktur | [`handoff/3-struktur.md`](handoff/3-struktur.md) | 180 |
| 4. Abweichungen von der Roadmap — Übersicht | [`handoff/4-00-uebersicht.md`](handoff/4-00-uebersicht.md) | 7 |
| 4.1 – 4.10 | [`handoff/4-01-bis-4-10.md`](handoff/4-01-bis-4-10.md) | 671 |
| 4.11 – 4.20 | [`handoff/4-11-bis-4-20.md`](handoff/4-11-bis-4-20.md) | 1045 |
| 4.21 – 4.30 | [`handoff/4-21-bis-4-30.md`](handoff/4-21-bis-4-30.md) | 1516 |
| 4.31 – 4.40 | [`handoff/4-31-bis-4-40.md`](handoff/4-31-bis-4-40.md) | 1134 |
| 4.41 – 4.50 | [`handoff/4-41-bis-4-50.md`](handoff/4-41-bis-4-50.md) | 1723 |
| 4.51 – 4.60 | [`handoff/4-51-bis-4-60.md`](handoff/4-51-bis-4-60.md) | 725 |
| 4.61 – 4.70 | [`handoff/4-61-bis-4-70.md`](handoff/4-61-bis-4-70.md) | 1002 |
| 4.71 – 4.80 | [`handoff/4-71-bis-4-80.md`](handoff/4-71-bis-4-80.md) | 1123 |
| 4.81 – 4.90 | [`handoff/4-81-bis-4-90.md`](handoff/4-81-bis-4-90.md) | 868 |
| 4.91 – 4.100 | [`handoff/4-91-bis-4-100.md`](handoff/4-91-bis-4-100.md) | 1382 |
| 4.101 – 4.107 | [`handoff/4-101-bis-4-107.md`](handoff/4-101-bis-4-107.md) | 1032 |
| 5. Entscheidungen | [`handoff/5-entscheidungen.md`](handoff/5-entscheidungen.md) | 1000 |
| 5a. Stylus-Prototyp — der wichtigste Test vor Phase 3 | [`handoff/5a-stylus-prototyp.md`](handoff/5a-stylus-prototyp.md) | 184 |
| 5b. Wann und wie auf den CachyOS-Laptop wechseln | [`handoff/5b-cachyos-laptop.md`](handoff/5b-cachyos-laptop.md) | 143 |
| 5c. Zugang zu GitHub — Stand beider Rechner | [`handoff/5c-zugang-zu-github.md`](handoff/5c-zugang-zu-github.md) | 47 |
| 5d. 🐧 Auftrag für den Linux-Laptop — hier anfangen | [`handoff/5d-auftrag-fuer-den-linux-laptop.md`](handoff/5d-auftrag-fuer-den-linux-laptop.md) | 455 |
| 5e. 🪟 Auftrag für den Windows-Rechner — hier weitermachen | [`handoff/5e-auftrag-fuer-den-windows-rechner.md`](handoff/5e-auftrag-fuer-den-windows-rechner.md) | 893 |
| 6. Arbeitsplan | [`handoff/6-arbeitsplan.md`](handoff/6-arbeitsplan.md) | 967 |
| 7. Fallen | [`handoff/7-fallen.md`](handoff/7-fallen.md) | 1647 |
| 8. Schnellstart-Befehle | [`handoff/8-schnellstart-befehle.md`](handoff/8-schnellstart-befehle.md) | 159 |
| 9. Chronik | [`handoff/9-chronik.md`](handoff/9-chronik.md) | 142 |

---
## 0. Schnelleinstieg für einen neuen Thread

**Das Projekt:** Offline-Notiz-App (Notizbuch, Whiteboard, Textdokument), Stylus-first, keine
Cloud. Läuft heute als WPF-App unter Windows 11. **V2 ist die Portierung nach Linux (Avalonia)
und iPadOS** — Greenfield-Solution, in die der wiederverwendbare Code aus V1 wandert.

**Wo:**

| | |
|---|---|
| **V2 (hier gearbeitet)** | **seit V2-128: Lenovo Yoga unter Omarchy, `/home/gonk/Projects/gonk-note-V2`** (der Windows-Pfad `C:\Dev\Zed\gonk-note-V2` ist Historie), Branch `main` → <https://github.com/GONKstupid/GonkNote>, Remote über **SSH** (§5c) |
| **V2 auf dem Linux-Laptop** | `~/Zed/gonk-note-V2/GonkNote` (CachyOS) — **Messgerät, kein Arbeitsplatz.** Arbeitsanweisung: **§5d**, Begründung: §5b, Stift: §5a |
| **V1 (Referenz, nicht anfassen)** | `C:\Dev\Zed\gonk-note`, Branch `main`, <https://github.com/GONKstupid/gonk-note> |
| **Roadmap (die Vorgabe)** | `C:\Users\manue\Desktop\GonkNote-TM\gonk-note-port-RM.MD` — **umgezogen**, hier stand bis 2026-08-04 der Pfad direkt auf dem Desktop |
| **V1-Handoff (alle Alt-Erfahrungen)** | `C:\Dev\Zed\gonk-note\HANDOFF.md` — **weiterhin gültig**, §4 Fallen und §7 Testen dort lesen |

> ### Auf welchem Rechner läufst du?
>
> **⚠ Stand V2-128:** Entwickelt wird auf dem Lenovo Yoga unter Omarchy (§0-Block oben).
> Die Zeilen 🪟 Windows / 🐧 CachyOS-Laptop unten beschreiben den alten Aufbau; sie gelten
> wieder, sobald der Windows-Laptop steht.
>
> | | |
> |---|---|
> | 🪟 **Windows** — der Normalfall, hier wird entwickelt | **§5e** sagt, womit die nächste Runde anfängt; was gebaut wird, steht dahinter in **§6** |
> | 🐧 **CachyOS-Laptop** — Messgerät, kein Arbeitsplatz | **§5d**, dort und nirgends sonst. Vorher §5b lesen (warum das so ist) |
>
> Der Nutzer muss dir in beiden Fällen nichts weiter sagen als „lies das HANDOFF".

**In dieser Reihenfolge vorgehen:**

1. **Wünsche und Fehlermeldungen des Nutzers zuerst.** Vorrang vor allem anderen.
2. Sonst: **§5 Entscheidungen** — was dort offen steht, nachfragen statt raten.
3. Sonst: **§6 Arbeitsplan** — dort steht, was gebaut wird. *(Bis V2-128 stand hier §5e bzw.
   §5d; **beide sind Historie**, es gibt nur noch diesen einen Rechner — §0.)*
4. Vor jeder Code-Änderung **§7 Fallen** überfliegen.

**Bauen und prüfen:**

```powershell
cd C:\Dev\Zed\gonk-note-V2
dotnet build -c Release          # muss 0 Fehler, 0 Warnungen ergeben
```

**⚠ Seit V2-128 wird unter Omarchy entwickelt, nicht unter Windows** (§0, „Neuer
Entwicklungsrechner"). Dort **projektbezogen** bauen — `dotnet build src/GonkNote.Core`
bzw. `dotnet build src/GonkNote.Avalonia` —, die Solution wegen des WPF-Kopfs **nicht als
Ganzes**. Der PowerShell-Block oben ist der alte Windows-Weg.

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

Zuletzt **Schritt 3** (§4.17): **Listen**. Ein Listenpunkt ist ein **Absatz mit einer
Angabe** und kein eigener Blocktyp — ausschlaggebend war die Bearbeitung, denn so bleiben
Eingabe, Ebenenwechsel und Herausnehmen Absatzänderungen statt Baumumbauten. Die **Nummer
wird gerechnet, nicht gespeichert**: sie hängt davon ab, was vor einem Punkt steht.

Zuletzt **Schritt 4, ganz** (§4.18 und §4.19): **Tabellen**. Das Raster steht an der Tabelle,
eine senkrechte Verbindung ist `Restart` + `Continue` statt eines „RowSpan", Zellen enthalten
Blöcke. Das Layout verteilt die Höhe verbundener Zellen in **zwei Durchgängen** und wiederholt
die Kopfzeile auf jeder Folgeseite. **Eine benannte Lücke bleibt:** eine Tabelle *in* einer
Zelle wird noch nicht gesetzt — mit eigenem Wächter festgehalten, damit sie absichtlich
verschwindet.

Zuletzt **Schritt 5** (§4.20): **Felder, Verweise und das Inhaltsverzeichnis**. Ein Feld
speichert seinen Wert nicht, sondern seine **Art** — Seitenzahl, Seitenanzahl, Datum, Titel,
Inhaltsverzeichnis; gerechnet wird beim Umbruch, dasselbe Muster wie bei der Listennummer.
Das Verzeichnis liest `TdParaFormat.OutlineLevel` und damit **die verlässliche Quelle, die das
`FlowDocument` nie hatte**. Weil die Seitenanzahl erst feststeht, wenn alles gesetzt ist,
läuft der Umbruch bei Bedarf **mehrfach, bis sich nichts mehr ändert**. Kopf- und Fußzeile
haben ihre letzten zwei Platzhalter bekommen: `{DATUM}` und `{TITEL}` sind jetzt echte Felder.

Zuletzt **Schritt 6** (§4.21): **Bilder und Diagramme — damit ist das Dokumentmodell
vollständig.** Der Befund dahinter wiegt schwer: Der heutige Editor rendert ein Diagramm beim
Einfügen zu einer **Bitmap** und wirft die Zahlen im selben Augenblick weg; ändern lässt es
sich nie wieder. Im Modell stehen jetzt die Zahlen, und ein Diagramm geht als **echtes
Diagramm** nach DOCX statt als Pixelbild. Bilder tragen einen Verweis auf den Blob-Speicher
statt ihrer Bytes — die dritte Naht (`ITdImages`) nach Schriftmessung und Feldwerten. Das
**Wasserzeichen** ist damit auch eingelöst, wie §4.15 es versprochen hatte.

Dann **die Übernahme der Bestandsdokumente** (§4.22) — sie war seit §4.15 vorgemerkt und
durch das unfertige Modell blockiert. **Nutzer-Entscheidung: still, aber ein Fehler wird
gespiegelt.** `TextDoc` hat zwei neue Felder neben `Rtf` (`Model`, `MigrationIssue`), `Rtf`
selbst wird **nie** überschrieben, und `FlowZuTd` im WPF-Kopf wandelt um — nur dort, denn RTF
und XamlPackage liest ausschließlich Windows. **Am laufenden Programm mit einer Kopie der
echten Daten geprüft**, und dabei einen Fehler gefunden, den fünf Schritte lang kein Test sehen
konnte: gerechnete Werte standen mit in der Datei (§4.22).

Danach **das Umverdrahten** (§4.23): Das Modell wird bei jedem Speichern mitgeschrieben, **DOCX
und Markdown laufen gegen das Modell** statt gegen ein `FlowDocument`, und `DocxExporter` wie
`MarkdownExporter` sind gelöscht. Dabei **fünf Fehler gefunden**, die im
Modell-gegen-Modell-Roundtrip unsichtbar waren — allen voran: das Absatz-Zeichenformat kam in
Word gar nicht am Text an.

Zuletzt **der Zeichner** (§4.24): `TdRenderer` in Core malt eine gesetzte Seite mit SkiaSharp —
Text mit allen Zeichenformaten, Aufzählungsmarken, Absatzlinien, Tabellen, Bilder,
Kopf-/Fußzeile und Wasserzeichen. **Ein Diagramm bekommt vorerst einen benannten
Platzhalterkasten**, und **angeschlossen ist der Zeichner noch nirgends** — dieselbe Absicht
wie bei Schritt 1.

Danach **der PDF-Export gegen das Modell** (§4.27): `TdPdf` in Core bricht mit `TdLayout` um und
zeichnet mit `TdRenderer` **direkt auf die PDF-Leinwand** — der Text im PDF ist damit Text und
kein Rasterbild mehr, Verweise sind anklickbar, und der Weg läuft auf jedem Kopf. **§4.1 ist
damit aufgelöst:** kein Exportweg steht mehr auf einem `FlowDocument`. 18 neue Wächter, rund 530
Zeilen Produktivcode weniger.

Zuletzt **auf dem CachyOS-Laptop gegengeprüft** (§4.27, „Was der Laptop gefunden hat"): alle
479 Wächter grün, und die eigentliche Frage — bettet Skia die **mitgelieferte** Schrift unter
Linux wirklich ein, was kein Test sehen könnte — ist mit **Ja** beantwortet. Dabei gemessen,
was der Gewinn in Zahlen ist: **rund 1,5 KB je Seite** statt eines vollen Rasterbilds.

Zuletzt, wieder unter Windows: **die Anzeige im Linux-Kopf** (§4.28). `TdLayout` und
`TdRenderer` sind an die Avalonia-Leinwand angeschlossen, dazu ein Ribbon und
`AvaloniaDocumentIo` — **Textdokumente sind unter Linux nicht mehr ausgegraut**, sie werden
angezeigt, importiert (DOCX) und in alle vier Formate exportiert. **Damit ist Phase 4
abgeschlossen.** Der Anschluss hat sofort einen Fehler gezeigt, den vier Runden lang kein
Wächter sehen konnte: **jede Tabelle stand mit doppelter Kopfzeile da** — behoben.

### ▶ Hier geht es weiter (Stand 2026-09-05, nach Runde V2-125)

> ### ▶ Neuer Entwicklungsrechner: der Lenovo Yoga unter Omarchy (Stand 2026-09-08, V2-128)
>
> **▶ DER ENTWICKLUNGSRECHNER HAT GEWECHSELT.** Entwickelt wird ab jetzt auf einem **Lenovo
> Yoga 7 2-in-1 14 IML9** unter **Omarchy** (Arch-basiertes Linux). **Der bisherige
> Windows-Entwicklungsrechner ist raus** — der Yoga ersetzt ihn vollständig. Das Repo liegt
> hier unter **`/home/gonk/Projects/gonk-note-V2`**, Branch `main`, Remote über SSH
> (`git@github.com:GONKstupid/GonkNote.git`).
>
> **⛔ DEN CACHYOS-LAPTOP GIBT ES NICHT MEHR** (Nutzer, 2026-09-09). Bis dahin stand hier, auf
> ihn komme wieder Windows als Gegenprobe-Gerät — **das ist hinfällig.** Es gibt **einen**
> Rechner, diesen, und daraus folgt dreierlei:
>
> - **Dauerregel 3a ist gestrichen** (Kopfzeile). Es gibt kein zweites Gerät, an das ein
>   Schritt gehen könnte.
> - **§5b, §5d und §5e sind vollständig Historie.** Sie beschreiben einen Aufbau aus zwei
>   Rechnern (Windows entwickelt, CachyOS misst), den es nicht mehr gibt. Sie werden **nicht
>   nachgezogen** und bleiben nur als Begründungsspeicher stehen — wer dort einen Auftrag
>   liest, liest einen Auftrag an ein Gerät, das weg ist.
> - **▶ Ein neuer Windows-Laptop kommt** (Nutzer, 2026-09-09) — der WPF-Kopf wird also wieder
>   prüfbar. **Die Priorität liegt trotzdem bei Linux**; was das für die Arbeitsweise heißt,
>   steht als Entscheidung in §5 (erste Zeile).
> - **⚠ Und der Preis gehört bis dahin dazugesagt: es gibt keinen laufenden WPF-Kopf.** Alles, was
>   nur der Windows-Kopf beantworten kann — der Zwei-Köpfe-Vergleich aus Phase 5, Schritt ①,
>   die Gegenprobe „rechnet es dort auch so?", die 69 WPF-Wächter — ist von hier aus **nicht
>   mehr prüfbar.** Der Code bleibt im Baum und wird in der CI gebaut; **gesehen** hat ihn
>   danach niemand mehr. Das ist keine Lücke im Fleiß, sondern eine im Aufbau, und sie
>   trifft jede künftige Runde, die „in beiden Köpfen" sagt.
>
> **▶ Die App läuft unter Omarchy — über das AppImage installiert, und sie funktioniert**
> (bis auf ein paar Bugs). Erster echter Lauf auf einem fremden Linux außerhalb des
> Baurechners.
>
> **▶ ZWEI BUG-RUNDEN SIND GELAUFEN (2026-09-09, V2-129 §4.104 und V2-129b §4.105) — Version 1.0.2.**
> Vier Meldungen des Nutzers, dazu ein Bedienwunsch:
>
> | | Punkt | Stand |
> |---|---|---|
> | 1 | Seitenleiste schließt nicht (nur der Inhalt verschwand) | ✅ **behoben und am laufenden Programm belegt** |
> | 2 | Bildschirmtastatur geht nicht auf | ✅ **gebaut (§4.105) — ⚠ vom Nutzer zu prüfen** |
> | 3 | Textfeld-Werkzeug (T) mit Finger | ✅ behoben — ⚠ **vom Nutzer mit dem Finger gegenzuprüfen** |
> | 3 | Textfeld-Werkzeug (T) mit Stift | ✅ **Ursache gefunden und behoben (§4.106)** — ⚠ vom Nutzer zu prüfen |
> | 4 | Einstellungsleiste soll klappen | ✅ **gebaut und am laufenden Programm belegt** |
>
> **⛔ Zu Punkt 2 gehört eine Richtigstellung, und sie steht in §4.105.** Der Satz „eine
> externe Bildschirmtastatur ist stumm" war **falsch** — er stammte aus einer Messung mit
> `wtype`, und mit der Tastatur, die der Nutzer wirklich benutzt, kommen die Tasten sehr wohl
> an. **Stehen bleibt:** `Avalonia.X11` kennt kein `InputPane`, eine fremde Tastatur kann
> tippen, aber **nicht von selbst aufgehen**. Gebaut ist deshalb beides: eine **eingebaute**
> Tastatur (Ansicht → Bildschirmtastatur → Eingebaut) und ein **einstellbarer Umschaltbefehl**
> für die des Systems.
>
> **▶ Drei Proben liegen beim Nutzer, bevor die nächste Runde anfängt:** (a) mit dem
> **Finger** ein Textfeld setzen — geht es? (b) **Ansicht → Bildschirmtastatur → Eingebaut**,
> dann in ein Textfeld tippen — kommt alles an, in beiden Sprachen? (c) **F9** drücken, T
> wählen, mit dem **Stift** aufsetzen: bleibt das Feld jetzt stehen? Wenn nicht, steht im
> Protokoll unter der Anzeige, **was** es schließt.
>
> **▶ EIGENE DESIGNS SIND GEBAUT (2026-09-10, V2-130, §4.107) — der Wunsch vom 2026-08-02.**
> Ein Design ist eine JSON-Datei mit bis zu zwanzig benannten Farben unter
> `<Datenordner>/Themes/*.json`; was fehlt, kommt aus Hell bzw. Dunkel. **Ansicht → Design**
> gibt es jetzt in beiden Köpfen, mit „Eigenes laden…", „Vorlage speichern…" und
> „Design-Ordner öffnen". **Der WPF-Kopf ist mit umgestellt** (Nutzer-Entscheidung, §5):
> `Themes/Light.xaml` und `Dark.xaml` sind **gelöscht**, beide Köpfe bauen ihr Wörterbuch
> aus derselben Tabelle in Core. **Am Linux-Kopf am laufenden Programm belegt**, in sechs
> Schritten samt Neustart — **der WPF-Kopf ist gebaut und nicht gesehen** (§4.107, „Was diese
> Runde nicht belegen konnte"): das ist der erste Posten für den neuen Windows-Laptop.
>
> **▶ DOKU GETEILT (2026-09-10, V2-131) und 1.0.3 IST RAUS (2026-09-10, V2-132).** Die
> Anleitung liegt jetzt unter `Docs/` und ist in **Installation** (`INSTALLIEREN.md`/
> `INSTALL.md`, nur Repo) und **Bedienung** (`ERSTE-SCHRITTE.md`/`GETTING-STARTED.md`,
> weiter in der App) geteilt; die Bedienungs-Anleitung hat einen KI-Prompt für eigene
> Designs. Die READMEs sind neu geordnet, mit einem Schlusswort in beiden Sprachen
> (nebenher gevibecodetes Schülerprojekt, praktisch alles von KI). **Version 1.0.3** an
> allen Stellen (Directory.Build.props, metainfo.xml neuer `<release>`, site/, fehler.yml;
> `About.Version` unverändert, weil dort keine Zahl steht), **AppImage lokal gebaut und
> gestartet** (84 MB, Version 1.0.3.0, DB legt an), **Tag `v1.0.3` geschoben** →
> `release.yml` baut Windows-Zip, AppImage und Tarball und legt das Release an.
>
> **▶ Danach:** der **Flathub-Umbau** (§4.102, §6) und **Phase 5.1** (Rechtschreibprüfung,
> §5 Nr. 22).
>
> **▶ Die drei Handgriffe am Nutzer-Konto aus V2-125 gelten unverändert** (Tag `v1.0.0`,
> Pages-Quelle, Repo-Beschreibung/Topics) — siehe den Block darunter.


> **▶ PHASE 5 IST ZU. SCHRITT ⑤ IST GELAUFEN, UND DAS REPO IST ÖFFENTLICH** (§4.101,
> V2-125, unter Windows). **Bau 0/0, 1313 Tests (1244 Core + 69 WPF), +5.** Der Nutzer hat das Repo auf
> „public" geschaltet; alles, was §6 unter „Was in ⑤ ansteht" führt, ist damit fällig
> gewesen und getan: **Version 1.0.0** an allen fünf Stellen (samt allen vier Über-Dialogen
> am laufenden Programm), **READMEs** mit den drei Installationswegen und sieben
> Bildschirmfotos, **Projektseite** in `site/`, **`release.yml`** an einem `v*`-Tag,
> **Beiwerk**. Die Checkliste „Vor dem Öffentlich-Schalten" ist Punkt für Punkt abgehakt —
> **HANDOFF und die ganze Git-Historie sind auf Privates durchgesehen und beide sauber**
> (633 Pfade, 124 nicht mehr im Baum, alle 124 aus dem Umzug von Phase 0).
>
> **⛔ Der Fund der Runde saß im eigenen Über-Dialog, und er ist eine Klasse:** Die neue,
> längere Versionszeile wurde **abgeschnitten statt umgebrochen**. Ein waagerechtes
> `StackPanel` misst seine Kinder mit **unendlicher Breite** — `TextWrapping` hatte also
> keine Breite, an der es hätte brechen können. **Nicht der Text ist gekürzt, der Behälter
> ist getauscht** (`DockPanel`, in **beiden** Köpfen). *Ein Text, der abgeschnitten wird
> statt umzubrechen, ist kein Textproblem, sondern ein Layoutfehler — und er wartet auf den,
> der die Zeile das nächste Mal verlängert.*
>
> **⛔ Zwei weitere Funde:** Ein **Wächter** meldete den vierten README-Verweis der Anleitung
> (er zählt bewusst die genaue Liste, §4.99). Und eine **dreispaltige Tabelle** wird im
> Hilfe-Fenster abgeschnitten — die neue Installationstabelle ist deshalb zu drei Absätzen
> geworden; **⚠ derselbe Verdacht steht benannt für Abschnitt 14 der Anleitung und ist
> ungeprüft**, weil `tools/klick.ps1` das Hilfe-Fenster nicht blättern kann.
>
> **⚠ Die Bildschirmfotos durften nicht aus dem echten Bestand kommen** (Dauerregel 4: die
> Kopie ist zum *Prüfen* da, sie enthält Schulunterlagen). Dafür gibt es jetzt
> **`tools/demo-db`** — erfundener Inhalt, deutsch und englisch, über `TdMarkdown.Lesen`
> erzeugt. *Und das Werkzeug hat beim ersten Anlauf Elemente unterhalb des sichtbaren
> Seitendrittels gesetzt: auf dem Bild waren sie schlicht nicht da.*
>
> **⛔ UND EIN BEFUND, DER NICHT AUS DIESER RUNDE STAMMT: DIE CI WAR SEIT DEM 2026-09-03
> ROT** — neun Läufe, **beide Jobs**, und **nur der Testschritt**; alle Bauschritte grün.
> Angefangen hat es mit dem Push von neun Commits auf einmal (V2-109 bis V2-116). **Hier war
> es nicht reproduzierbar** — weder im Arbeitsbaum noch **in einem frischen Klon des
> gepushten Standes**. **Die Protokolle verlangen Adminrechte am Repo** (`HTTP 403`).
>
> **Statt zu raten, sagt die CI es jetzt selbst:** `::error::` erzeugt Annotationen, und die
> sind bei einem öffentlichen Repo **ohne Anmeldung** lesbar. *Der erste Anlauf dieses
> Schrittes ist dabei selbst gefallen — GitHub startet jeden `bash`-Schritt mit `-eo
> pipefail`, und ein `grep` ohne Treffer gibt 1 zurück. **Ein Schritt, der einen Fehler
> benennen soll, darf selbst keinen erzeugen.***
>
> **✅ ES WAR EIN EINZIGER WÄCHTER, UND ER HATTE RECHT:**
> `TabellenUmbauTests.Datumsangaben_werden_als_Datum_sortiert`. **Kein Testfehler:**
> `TdTableEdit.AlsDatum` las mit `CultureInfo.CurrentCulture` — auf `de-DE` ging
> „15.02.2026" durch, auf `en-US` nicht (Monat 15), die Spalte galt damit als
> **Zahlen**spalte, und daraus wurde **15.022.026**. *Genau die Umkehrung, vor der der
> Kommentar über der Sortierung warnt — und niemand sieht sie, weil das Ergebnis sortiert
> aussieht.* Jetzt fest **`de-DE`, dann invariant**, wie in `TdTabellenformel.AlsZahl`, **das
> die gleiche Frage seit jeher so beantwortet**; die Kultur gilt für die **ganze Spalte**.
> **+5 Wächter mit fest gesetzter Kultur** — der alte erbt die des Rechners und war deshalb
> hier immer grün. **✅ Beide CI-Jobs grün.**
>
> **⛔ Und der Satz bleibt trotzdem stehen:** Das Netz hing **vier Tage** durch, und keine der
> neun Runden dazwischen hat es bemerkt. *Ein Netz, in das niemand hineinsieht, meldet
> nichts; es hängt nur.* **Zum Ablauf einer Runde gehört ab jetzt ein Blick auf den letzten
> CI-Lauf** (§8).
>
> **✅ UND DIE INSTALLATIONSPROBE IST GELAUFEN** (§4.103, V2-127). **Windows: gemessen** —
> das Artefakt genau wie in `release.yml` gebaut (92,4 MB Zip), entpackt, **aus einem fremden
> Ordner mit frischer Datenbank gestartet**. **Linux: alles, was von hier aus prüfbar ist,
> stimmt** — der Kreuzbau läuft (341 Dateien, 155 MB, `libSkiaSharp.so`, `Fonts`, `tessdata`,
> 62 Cover), und `gonknote.sh`, `AppRun` und die `.desktop`-Datei nennen übereinstimmend
> dieselbe Startdatei. **⚠ Der Start selbst und der Weg „Release-Seite → Herunterladen →
> Starten" warten auf das erste Release.**
>
> **⛔ Und sie hat einen Fund gemacht: der Ordner `Fonts` stand in keiner Kopieranweisung.**
> Die Oberflächenschriften liegen als **lose Dateien** neben der Exe; fehlt der Ordner,
> **startet die App und zeichnet in Segoe UI** — §4.72 rückwärts, **ohne jeden Hinweis**.
> Nachgemessen und in allen vier Dokumenten behoben. *Ein Sicherheitsnetz, das niemand sieht,
> verdeckt genau den Fehler, gegen den es gespannt ist.*
>
> **⛔ Dazu zwei Funde am Zerleger, beide beim Gegenprüfen entstanden:** Ein **Blockzitat
> innerhalb eines Listenpunkts** kennt `Markdown.Parse` nicht — die `>` stehen im
> Hilfe-Fenster **wörtlich** da, *während dasselbe auf GitHub richtig aussieht*. Und
> **Backticks im Linktext** überleben wörtlich. Beides behoben. *Was in den vier Dokumenten
> steht, wird an zwei Orten gelesen — beide prüfen.*
>
> **▶ WAS JETZT NOCH ANSTEHT, UND ES GEHÖRT DEM KONTO DES NUTZERS — nicht diesem Baum:**
> 1. **Den Tag schieben:** `git tag -a v1.0.0 -m "Gonk Note 1.0.0"` und `git push origin
>    v1.0.0`. **Das ist die Veröffentlichung** — `release.yml` baut daraufhin Windows-Zip,
>    AppImage und Tarball und legt das Release an. ⚠ **Dieser Lauf ist nie gelaufen** (er
>    lässt sich nur durch einen Tag auslösen); der AppImage-Zweig ist auf dem Laptop
>    gemessen, in einer CI nie. **Die Tests, die er vorher laufen lässt, sind seit dem
>    2026-09-05 wieder grün** — das war bis dahin die Sperre.
> 2. **Pages einschalten:** Repo-Einstellungen → Pages → Quelle **GitHub Actions**. Ohne das
>    läuft `pages.yml` und veröffentlicht nichts.
> 3. **Beschreibung und Topics** des Repos setzen. **`gh` ist auf diesem Rechner nicht
>    angemeldet** (`HTTP 401: Bad credentials`) — beides ist eine Einstellung, keine Datei.
>
> **⛔ Und was zu Flathub inzwischen nachgelesen ist** (§4.102, V2-126): **Der Eintrag ist
> nicht bloß „noch nicht eingereicht" — das Manifest erfüllt die Anforderungen nicht.**
> Flathub baut **aus dem Quellcode und ohne Netz**; unseres packt ein fertiges
> `dotnet publish`-Ergebnis ein. **Das ist ein Umbau und keine Handreichung** — der Zuschnitt
> steht in §6, „Vorgemerkt: Flathub". *Es blockiert nichts:* AppImage und Windows-Zip decken
> beide Plattformen ab, sobald der Tag steht. **⛔ Nebenbei ist eine Begründung im
> Manifestkopf als falsch nachgewiesen** — `org.freedesktop.Sdk.Extension.dotnet10` **gibt
> es**, mit `branch/25.08` und SDK 10.0.300 GA; berichtigt.
>
> **Dazu unverändert offen:** der **Flathub-Eintrag** (nicht eingereicht), das
> **Avalonia-Issue** (geschrieben, nicht abgesendet — §4.97), der **Portal-Dateidialog** und
> der **Stift** in der Sandbox (§4.100, beide brauchen eine Hand am Gerät).
>
> **▶ Danach ist M3 erreicht, und der nächste Punkt ist Phase 5.1** — die
> Rechtschreibprüfung im Linux-Kopf (§5 Nr. 22), dann Phase 6 (iPadOS).


> **▶ DER ZWEITE BAU IST GELAUFEN, UND ER IST DER, DER HINAUSGEHT** (§4.100, V2-124, **auf
> dem CachyOS-Laptop**). Der Auftrag aus §5d ist abgearbeitet, alle fünf Punkte. Kette grün:
> Bau **0/0**, **1239/1239** — genau die Zahl, die §5d aus Windows nannte; diesmal hängt
> nichts an einer Zählfrage. **Flatpak (154,6 MB) und AppImage (86 MB) beide neu gebaut,
> installiert und gestartet**, nachdem §4.97–§4.99 darin sind. **Kein Produktivcode
> angefasst** — nur zwei Dateien in `packaging/`.
>
> **✅ Der Fund aus §4.96 ist zu:** in **beiden** Paketen liegen **null** native
> Windows-Binärdateien und kein `x64/`/`x86/`. *Die 12 MB sind weg.*
>
> **⛔ Der Werkzeugbefund, der den Zuschnitt der Runde geändert hat: der Laptop konnte nicht
> klicken.** `ydotoold` scheitert an `/dev/uinput` (der Nutzer ist nicht in der Gruppe
> `input`), `sudo` verlangt ein Passwort, **der Skill `sudopasswot` stand der Sitzung nicht
> zur Verfügung**, und `wtype`/`dotool`/`wlrctl`/`xdotool` sind alle nicht installiert.
> **Die zwei bedienten Punkte des Auftrags sind trotzdem nicht offen geblieben**, sondern mit
> einer **Wegwerf-Sonde gegen den echten Code** beantwortet — dieselbe Wahl wie §4.98: *ein
> Klick zeigt, **dass** etwas passiert, die Sonde zeigt, **was**.* Der Markdown-Import liest
> zeichengenau das, was §4.99 unter Windows sah (`#####` bleibt ein **Absatz**,
> `![alt](fehlt.png)` wird **`[alt]`**), und **beide Verweisfragen fallen richtig aus**:
> „Feature-Übersicht im README" wird **angenommen**, `THIRD-PARTY-NOTICES.md` bleibt in
> beiden Sprachen **schlichter Text**.
>
> **⚠ Beinahe wäre ein eigener Scheinbefund daraus geworden:** Der erste Zähler meldete
> **200 PE32-Dateien** — `file` sagt das über **jede** .NET-Assembly, auch die auf Linux
> laufende. Richtig gemessen sind es **0**. *Der teuerste Fehler einer Messrunde ist der am
> Messgerät (§4.56, §4.82).*
>
> **⛔ Drei veraltete Sätze in `packaging/` behoben**, alle seit V2-122 falsch — zweimal
> „noch nicht am Gerät geprüft" und einmal „Schritt ③ ist eine Erprobung … erst der zweite
> Bau geht hinaus".
>
> **▶ Damit ist der Laptop wieder nicht dran, und ⑤ läuft unter Windows weiter** (§5e, §6):
> **READMEs**, **GitHub Pages**, **Releases mit Artefakten**, **Repo-Beiwerk**. **⚠ Und zwei
> Posten kommen vom Laptop zurück:** Die **Version steht überall auf 0.3.0** — auch der
> einzige `<release>`-Eintrag der `metainfo.xml`, der sich selbst *„packaging trial, not a
> release"* nennt; **§5 Nr. 23/24 verlangt 1.0.0.** Und wer dem Laptop den nächsten Auftrag
> schreibt, **klärt zuerst, ob er klicken kann** (die zwei Auswege stehen in §5d).
>
> **⚠ Weiterhin ohne Beleg, und beides wartet auf eine Hand, nicht auf ein Skript:** der
> **Portal-Dateidialog** in der Flatpak-Sandbox und der **Stift**.


> **▶ SCHRITT ④ IST ZU — UND DER PRÜFLAUF HAT GEFUNDEN, WOFÜR ER DA IST** (§4.99, V2-123,
> unter Windows). **Bau 0/0, 1308 Tests (1239 Core + 69 WPF), +38.**
>
> **Posten 1 — die Nähte, gemessen:** Von zwölf Diensten im WPF-Kopf **müssen elf dort
> stehen** (WPF-Typen oder Windows-API). **Der zwölfte war `MarkdownImporter`** — 394 Zeilen
> eigene Markdown-Grammatik, obwohl Core seit §4.12 einen Zerleger hat. **Zum fünften Mal
> dieselbe Lage** nach Farben, Schriften, Symbolen und Vorlagen; §4.13 hatte den *Betrachter*
> längst umgestellt, nur den *Importer* nicht.
>
> **⛔ Zwei gemessene Folgen, nicht nur eine Doppelung:** Der Linux-Kopf konnte `.md`
> **überhaupt nicht** importieren — ein **unbenanntes Loch in M2** wie der Tafel-Export in
> §4.77 —, und ein importiertes `.md` bekam **kein `Model`** und war unter Linux erst lesbar,
> nachdem es unter Windows einmal offen war. *Die Begründung im Kommentar war diesmal nicht
> falsch, sondern **abgelaufen** — und die liest sich wie eine gültige.* Jetzt
> `TdMarkdown.Lesen` in Core, beide Köpfe importieren `.md`.
>
> **Posten 2 — die Tabelle in einer Zelle war von der Oberfläche aus erreichbar:** `TdEdit.Ort`
> steigt in Zellen ab, also legte „Tabelle einfügen" dort klaglos eine an, **die der Umbruch
> weglässt**. Auf Nutzer-Entscheidung **gesperrt**, die Lücke bleibt benannt. **⛔ Und zwei der
> drei benannten Lücken hatten entgegen §6 gar keinen Wächter** — die Palettenlücke war sogar
> *umgangen*: Der Rundreise-Test kürzt die Palette auf das, was durchpasst. *Ein Test, der
> sich einer Lücke anpasst, hält sie nicht fest; er verdeckt sie.*
>
> **⛔ Der Prüflauf fand fünf tote Verweise — in beiden Köpfen, in beiden Sprachen.**
> „Lies die Feature-Übersicht im README" tat **nichts**, und `THIRD-PARTY-NOTICES.md` ist in
> **keinem** Kopf eingebettet. Alle fünf sahen aus wie Verweise. Statt fünf Fälle zu flicken
> ist die **Klasse unmöglich gemacht**: `Dokumentverweise` (`Kann` + `Oeffnen`) in Core — was
> niemand annimmt, wird schlichter Text. *Und derselbe Fehler saß zweimal: `EndsWith(".md")`
> sagt zu `README.md#abschnitt` **nein**.*
>
> **⛔ Und der zweite Fund: die vier mitgelieferten Dokumente beschrieben eine App von vor
> M2** — elf Stellen, unsymmetrisch verteilt. Die README-Tabelle „Was der Linux-Ausgabe noch
> fehlt" hatte **sechs Zeilen, fünf davon falsch**. Alle vier Dokumente stehen jetzt auf dem
> Stand von M2, mit den **vier** Einschränkungen, die wirklich gelten.
>
> **▶ Damit ist ⑤ dran — das Veröffentlichen** (§5e, §6). *(Der zweite Bau, der hier als
> Nächstes stand, ist am 2026-09-04 gelaufen — §4.100, oben.)*


> **▶ DER BEIPACK TRÄGT — auf dem Laptop gemessen** (§4.98 „Was der Laptop gefunden hat",
> V2-122). Der Auftrag aus §5d ist abgearbeitet: **die Texterkennung im AppImage kommt aus
> dem Beipack und aus nichts sonst**, zeichengenau bei **verstecktem System-Tesseract**
> (`bwrap`), und `/proc/self/maps` nennt beide Bibliotheken namentlich. **Der Beipack kostet
> 24,5 MiB** (61 → 85 MiB), **die Hälfte davon ICU** — es kommt über
> `libtesseract → libarchive → libxml2` mit und **kann nicht weggelassen werden**.
>
> **⛔ Zwei Funde, beide nicht im Auftrag:** Die **benannte Grenze aus §4.98 ist jetzt
> gemessen** — ein unbrauchbarer Beipack schaltet ein vorhandenes System-Tesseract **aus**
> (nicht behoben, gehört nach Windows). Und **.NET nimmt sein ICU aus dem Beipack**
> (`LD_DEBUG=libs`); das ist harmlos, weil .NET abwärts auf die Fassung des Wirts zurückfällt,
> **aber der `AppRun`-Kommentar sagte etwas anderes** und ist berichtigt.
>
> **⛔ Und §4.98 hatte denselben falschen Satz an zwei Stellen und nur eine berichtigt** — die
> zweite steht jetzt richtig in §5 Nr. 29. *Wer einen falschen Satz findet, sucht seine
> Zwillinge.*
>
> **▶ Der Laptop ist damit wieder nicht dran.** Weiter geht es unter Windows mit dem **Rest
> von ④ und dem vollständigen Prüflauf** (§5e).

> **▶ ZWEI ENTSCHEIDUNGEN DES NUTZERS SIND GEFALLEN** (§4.98, 2026-09-04).
>
> **§5 Nr. 29 → (b): das AppImage bringt seine eigene Texterkennung mit.** Begründung des
> Nutzers: *„nicht jede Linux-Distro hat Texterkennung."* **Das widerlegt die Empfehlung
> dieses Dokuments, und sauber:** empfohlen war (a) mit dem Argument *„das Flatpak ist der
> Hauptweg"* — aber **ein AppImage gibt es gerade für den Rechner, auf dem kein Flatpak
> läuft.** *Eine Empfehlung, die den Hauptweg zum Maßstab nimmt, misst den zweiten Kanal an
> der falschen Frage.*
>
> Gebaut sind drei Stellen: `TesseractBindung.SuchpfadeMit` in Core (**+4 Wächter**),
> `TesseractLinux.EigenerLibOrdner`, und in `packaging/appimage` das Einsammeln per `ldd`
> samt `LD_LIBRARY_PATH` im `AppRun`. **✅ Am 2026-09-04 auf dem Laptop gelaufen und gemessen**
> (V2-122, Kasten oben) — *als dieser Absatz geschrieben wurde, war keine Zeile davon je
> gelaufen, und der Auftrag in §5d bestand deshalb auf der Messung mit verstecktem
> System-Tesseract.*
>
> **§5 Nr. 30 → (a): `UndoStack` und `TdUndo` bleiben getrennt.** Wie empfohlen. **Der
> Handgriff war nicht das Nein, sondern der Ort:** der Grund steht jetzt in **§7**, wo jemand
> nachsieht, der eine Doppelung sucht — in §4.33 stand er seit Phase 4, und die Frage ist
> trotzdem ein zweites Mal gestellt worden.
>
> **⛔ Der Fund der Runde stammt aus meinem eigenen Wächter-Kommentar.** Er behauptete, das
> Anhängen der Systempfade sei ein Rückfall für den Fall, dass die mitgelieferte Fassung
> *nicht lädt*. **Das stimmt nicht** — `QuelleSuchen` nimmt den ersten Ordner mit einem
> **passenden Dateinamen**, und ein unbrauchbarer Beipack schaltet das Wirtssystem damit aus.
> **Benannt und an den Bau geknüpft statt behoben** (eine Rangfolge kann nach Namen
> entscheiden, nicht nach Ladbarkeit). *Ein Wächter beweist, was er prüft; der Kommentar
> daneben behauptet, wofür er gut ist — nur das Erste hält der Compiler nach.*


> **▶ SCHRITT ④ IST ZUR HÄLFTE GELAUFEN** (§4.97, V2-120, unter Windows). **Drei der sieben
> Posten sind zu:** **toter Code, zweiter Lauf** — `tests/` und `tools/` sind durchsucht und
> **sauber**, fünf Sonden, kein totes Mitglied, keine tote Datei; tot waren **zwei
> Übersetzungstexte** (`Ed.FitWidth`/`Ed.FitPage`), die Tabellen stehen jetzt bei **579/579**.
> **§7 zu Ende gegengelesen** — fünf Stellen richtiggestellt. Und **das Avalonia-Issue ist
> geschrieben** (`Docs/avalonia-issue-tote-tasten.md`); **⛔ es ist nicht abgesendet, das ist
> das Konto des Nutzers.**
>
> **⛔ Der eigentliche Befund war §7 selbst: zwanzig Runden ohne einen einzigen neuen
> Eintrag.** Alles, was Phase 5 gelernt hat, stand ausschließlich im **Prompt von §5e** — und
> der wird jede Runde überschrieben, wäre mit ⑤ also ersatzlos verschwunden. *§7 ist der Ort,
> den man vor einer Änderung liest; ein Prompt ist der Ort, den man einmal ausführt. Was
> länger gilt als eine Runde, gehört nicht in den Prompt.* **Neu in §7: „Neu aus Phase 5
> (§4.68–§4.96)", elf Einträge.**
>
> **⛔ Und der gefährlichste Einzelfund ist eine Anweisung, keine Warnung:** §7 riet bis heute,
> ein neues Symbol als **Vektorform in `Themes/Styles.axaml`** anzulegen — seit §4.31
> (2026-08-12) falsch, denn dort steht **keine einzige** mehr; die 76 Symbole liegen in Core.
> **Wer dem folgt, baut das 77. in einen Kopf allein.** *§4.69 konnte es mit seinen drei
> Messungen nicht finden, weil jeder genannte Name noch existiert — nur die Aussage über sie
> nicht mehr. Eine veraltete Warnung kostet eine Nachfrage; eine veraltete **Anweisung** kostet
> den Umbau, den sie anordnet.*
>
> **✅ Nebenbei aufgelöst:** die Zählfrage aus §4.96 — **1201 Core + 65 WPF = 1266**, unter
> Windows nachgezählt. **Die Zahl des Laptops war die richtige.**
>
> **▶ Als Nächstes der Rest von ④, und der letzte Posten ist der größte:** §4.1, die benannten
> Lücken — **und dann der vollständige Prüflauf** (§5e, §6). **⛔ Danach wird das Flatpak ein
> zweites Mal gebaut, und erst dieser Bau geht hinaus.**
>
> **▶ Zwei Entscheidungen warten:** §5 „Noch offen" **29** (AppImage und Texterkennung) und
> **30** (`UndoStack` und `TdUndo` zusammenlegen?) — beide mit Empfehlung **(a)**.


> **▶ SCHRITT ③ IST GELAUFEN — auf dem CachyOS-Laptop** (§4.96, 2026-09-04). **Flatpak und
> AppImage sind gebaut, installiert und gestartet**, und beide sind auf dem Schirm
> **pixelgleich** zu einem gewöhnlichen `dotnet publish`-Lauf (0 abweichende Pixel).
> **Die Kernfrage, die §4.64 ausdrücklich außerhalb eines Flatpaks vermessen hatte, ist
> beantwortet: die Texterkennung trägt IN DER SANDBOX** — `leptonica` und `tesseract` sind im
> Manifest mitgebaut, und `TesseractBindung.Suchpfade` führte `/app/lib` seit §4.63 an erster
> Stelle. *Der Fall, für den es vorgesehen war, ist eingetreten, und es hat gepasst.*
>
> **⛔ Drei Funde, und der erste hat den ersten Start gekostet:** `--socket=fallback-x11`
> gibt X11 nur frei, **wenn kein Wayland da ist** — der Kopf starb mit „XOpenDisplay failed",
> denn **Avalonia 12 hat unter Linux nur den X11-Rücken**. Dazu: der CMake-Bau von Tesseract
> heißt `libtesseract.so.5.5` und **nicht** `.so.5`, und `dotnet publish -r linux-x64`
> schleppt **12 MB Windows-DLLs** mit. **Alle drei in `packaging/` behoben, keine Zeile
> Produktivcode angefasst.**
>
> **⚠ Zwei Dinge sind offen geblieben, und beide brauchen eine Hand am Gerät statt eines
> Auftrags** (§5d): der **Dateidialog** in der Sandbox (das Portal antwortet, der Dialog
> selbst ist ungeprüft) und der **Stift** (von einem Skript aus grundsätzlich nicht messbar).
>
> **▶ Als Nächstes ④, und das ist wieder Windows-Arbeit** — §5e, dann §6 „Was in ④ ansteht".
> **⛔ Danach wird noch einmal gebaut, und erst dieser Bau geht hinaus.**


> **▶ SCHRITT ② LÄUFT.** Die zwei Beobachtungen aus §4.86 sind nachgemessen (§4.93): der
> **„ungespeichert"-Punkt beim bloßen Öffnen ist ein echter Befund** (der WPF-Kopf hat ihn
> nicht), das **leere Schriftgradfeld ist keiner** — es hält sich an §4.36, die Größe des
> ersten Absatzes steht nur nicht in der Leiter. **Nebenbei gefunden: zwei Schriftgrad-Leitern**,
> jetzt vereinigt in Core. *Von zwei notierten „Beobachtungen" war eine keine — wer sie
> ungeprüft als Fehlerliste nimmt, repariert etwas, das richtig ist.*
>
> **▶ SCHRITT ① IST ZU.** Nach dreizehn Runden (§4.77 bis §4.92) fehlt dem
> Linux-Kopf kein Werkzeug mehr, das der WPF-Kopf hat. **Die sieben Entscheidungen aus §5e
> sind alle beantwortet:** fünf gebaut, eine gestrichen (Lineal), eine als **Messung**
> beantwortet statt als Frage gestellt (Strg+F betraf **alle** Kürzel).
>
> **⚠ Und zwei der sieben standen auf einer falschen Prämisse** — `TdFormatEdit.Gemeinsam`
> liefert das **wirksame** Format und nicht die Abweichung (§4.87), und das Violett an der
> Wurzel war keine eigene Runde, sondern **sieben Schlüssel und ein Setzer** (§4.86).
> *Wer eine Frage auf eine Vermutung stellt, bekommt eine Antwort auf die falsche Frage.*
>
> **▶ Der Auftrag steht in §5e**, „Der Prompt zum Kopieren“. **Was bewusst offen bleibt,
> steht in §4.92** — Menü-Aufklapppunkt (drei Anläufe, aber neu vermessen), `Ed.Object.Behind`/
> `.Front`, Schnelltabellen, Lineal, Rechtschreibprüfung.

> **✅ M2 ist erreicht und ausgerufen** (2026-08-28, §2) — Funktionsgleichheit Linux ↔
> Windows, **mit einem benannten Loch**: die Rechtschreibprüfung fehlt im Linux-Kopf.
> Phase 4.5 ist auf **beiden** Systemen zu (§4.64, sechs von sechs Stücken), und der älteste
> offene Fund des Laptops ist erledigt (§4.67).
>
> **✅ Phase 5 läuft** — zwei Punkte sind angefangen: **toter Code** (§4.68) und **Doku gegen
> den Code** (§4.69).
>
> **▶ Und am 2026-08-28 hat der Nutzer Phase 5 neu geordnet und sieben Entscheidungen
> getroffen** (§5 Nr. 11, 14, 15, 21–25).
>
> **▶ Schritt ① ist angefangen und geteilt: ①a hat gemessen** (§4.71, 2026-08-29) — **ohne
> anzugleichen**, so zugeschnitten. **①b baut.** Dazu die achte Entscheidung, **§5 Nr. 26:
> fehlt dem Linux-Kopf etwas, wird es dort nachgebaut und nicht drüben gelöscht.**
>
> **✅ ①b läuft.** **§4.72:** die drei Punkte aus §4.71, die keine Angleicharbeit waren,
> sondern **Fehler** — Oberflächenschrift des WPF-Kopfs (Segoe UI statt Inter),
> Tastaturfokus seines Umbenennen-Feldes, **Auswahlfarbe** des Linux-Kopfs (§5 Nr. 27).
> **§4.73:** **Fläche 1 angeglichen** (fünfzehn Unterschiede), **§5 Nr. 14 eingelöst**.
> **§4.74:** **Fläche 5 vermessen** (76/76 Symbole, keine Lücke), die **Tintenpalette** aus
> einer Quelle in Core, der **Farbwähler** im Linux-Kopf nachgebaut, **vier fest verdrahtete
> deutsche Texte** im WPF-Kopf übersetzt. **+12 Wächter insgesamt.**
>
> **§4.75:** **Fläche 4 vermessen** und die **28 Meldungen** des WPF-Kopfs von der nativen
> `MessageBox` auf ein eigenes Fenster umgestellt — sie sahen an keiner Stelle aus wie die
> App, während der Linux-Kopf seit jeher eines hat.
>
> *Die folgenden Absätze sind die Chronik von ①c, älteste Runden zuletzt — **der Schritt ist seit V2-115 zu**, siehe oben.*
>
> **✅ §4.78 (V2-101):** Der **Formen-Stift** liegt als `WbFormen` in Core und
> steht in **beiden** Leisten — **+23 Wächter**, wo es vorher **keinen einzigen** gab. **⛔ Und
> der Bau hat dabei nichts bewiesen:** der Knopf war da, das Werkzeug wählbar, und es tat
> **nichts** — drei `switch`-Weichen im Linux-Kopf zählten die Stifte einzeln auf und kannten
> ihn nicht. Gesehen hat es erst der Klick am laufenden Programm.
>
> **✅ §4.85 (V2-108): Der Navigator — und die Liste, die Core schon rechnete.** Er fehlte
> dem Linux-Kopf ganz; **gebaut werden musste nur die Anzeige**, denn `TdToc.Eintraege`
> rechnet die Überschriftenliste fürs Inhaltsverzeichnis ohnehin. *Der WPF-Kopf sammelt sie
> dagegen selbst — er kann nicht anders, weil sein Editor kein Modell kennt.*
>
> **✅ §4.84 (V2-107): Zwei kleine Posten — und ein Selektor, der still nicht galt.**
> **Formatierung löschen** (`TdCharFormat.Zuruecksetzen` in Core) und **Rückgängig/Wiederholen
> als Knöpfe**. Gelöscht wird die **Abweichung**, nicht das Aussehen (§4.14) — ein Text in
> einer Überschrift bleibt groß. **⛔ §4.80 zum zweiten Mal, nur andersherum:**
> `Classes="ribbonschalter"` ist auf `ToggleButton` selektiert und hätte einem `Button`
> **still** kein Aussehen gegeben; diesmal fiel es beim *Nachsehen* auf und nicht erst am
> laufenden Programm. **⚠ Der Formatpinsel liegt bewusst noch** — ob er die Abweichung oder
> das wirksame Format überträgt, ist eine **Entscheidung** und keine Umsetzung.
>
> **✅ §4.83 (V2-106): Die zwei Reste aus §4.82.** Ein bestehendes Diagramm lässt sich jetzt
> **per Doppelklick ändern** (`TdCursor.StueckAn` in Core, beide Köpfe) — §4.21 hielt fest,
> dass genau das nie ging. Und die **Bringschuld** ist eingelöst: die Tafel des WPF-Kopfs ist
> am laufenden Programm geprüft. **⛔ Der Fund, den nur das laufende Programm zeigen konnte:**
> Über einer *Änderung* stand „Diagramm einfügen", und auf dem Knopf „Einfügen" — Bau grün,
> Funktion richtig, *das Fenster sagte nur nicht, was es tut.*
>
> **✅ §4.82 (V2-105): Das Diagramm-Werkzeug — und die Zeichnung, die es zweimal gab.**
> **Damit ist die Tafel vollständig.** `ChartDialog` rechnete und zeichnete die sieben
> Diagrammarten **selbst** (435 Zeilen `DrawingVisual`), obwohl Core sie seit §4.25 kann — und
> lieferte eine **Bitmap** ab, womit **die Zahlen verloren waren** (§4.21, im Code benannt).
> Jetzt legen **beide** Köpfe ein `TdChart` ins Dokument. **⛔ Der Fund, den kein Wächter sehen
> konnte:** Die Rundreise war seit §4.50 grün, aber das **Werkzeug** baute seinen Behälter
> **ohne `Tag`** — ein frisch eingefügtes Diagramm war beim ersten Speichern ein Bild.
> *Zwei Wege, die dasselbe bauen, weichen voneinander ab — an der Stelle, die niemand
> nachsieht.* **Und drei Zeilen der Aufgabenliste fielen weg** (Stil- und Listengalerien gibt
> es längst), während **zwei fehlten** (Objekt-Anordnung, Beschriftung).
>
> **✅ §4.81 (V2-104): Das Cover-Werkzeug** — und damit hat die Einstellungsleiste des
> Linux-Kopfs **alle** Abschnitte des WPF-Kopfs. **✅ Und die Falle aus §4.60 ist nicht zugeschnappt:** die Cover-Vorlagen
> sind **im selben Commit** ins Avalonia-csproj gekommen. *Nicht die Warnung war das
> Verdienst, sondern ihr Ort — sie stand dort, wo gebaut wird, und nannte ihren eigenen
> Ablaufzeitpunkt.*
>
> **⛔ Und wieder eine Zeile der Aufgabenliste, die Arbeit versprach, die dastand:**
> „Wortzahl" fehlt dem Linux-Editor **nicht** (§4.81, zweites Mal nach §4.77). *Eine
> Aufgabenliste, die niemand nachmisst, wächst nur — erledigte Punkte tragen sich nicht von
> selbst aus.*
>
> **✅ §4.80 (V2-103): Suchen & Ersetzen** liegt als `TdSuche` in Core und im Linux-Kopf —
> **+20 Wächter**, und er findet dabei **mehr** als der WPF-Kopf (Treffer über Formatgrenzen).
> **⛔ Und diesmal lag der Code zu Recht drüben:** anders als in §4.77/§4.78 steht der WPF-Weg
> wirklich auf `TextPointer`. *Nicht jede Datei in einem Kopf liegt dort zu Unrecht — wer nach
> zwei Funden anfängt, es überall zu vermuten, spart sich das Nachsehen.*
>
> **⛔ Das Violett zum sechsten Mal, mit neuer Ursache:** Der Stil `Button.ribbonknopf`
> **existierte und galt nur nicht** — mein Knopf ist ein `ToggleButton`. *Wer den Typ eines
> Steuerelements wechselt, verliert sein Aussehen still.* **Nach sechs Malen ist das Muster
> selbst der Befund; ein Vorschlag zur Ursache statt zu den Erscheinungen steht in §5e.**
>
> **✅ Die zwei Funde aus §4.78 sind zu** (§4.79, Nutzer-Entscheidung, beide „ja"):
> **(a)** Die Formerkennung misst jetzt gegen die **Ausdehnung** statt gegen die Bogenlänge —
> und **derselbe Denkfehler saß an drei Stellen, nicht an einer**; wer nur die eine repariert
> hätte, hätte aus dem Gekritzel statt eines Dreiecks eine **gerade Linie** gemacht.
> **(b)** Die **Vorgabetinte kommt jetzt in beiden Köpfen aus der Tabelle in Core**
> (`#1B2B4B` / `#E6ECF7`), die Farbkachel geht mit. **Beides in beiden Köpfen am laufenden
> Programm gegengeprüft.**
>
> **✅ §4.77 (V2-100):** Der **Tafel-Export** liegt in Core
> (`WbExport`) und läuft **in beiden Köpfen** — der Linux-Kopf konnte eine Tafel vorher
> **überhaupt nicht** exportieren (`ExportBoard` warf). **Das war ein zweites, unbenanntes
> Loch in M2**, und es ist zu. Dazu die Export-Gruppe in der Tafel-Einstellungsleiste, das
> Violett an den Auswahlpunkten (§5 Nr. 27, **fünftes Mal**) und **+12 Wächter in Core**.
>
> **⛔ Und §4.77 hat die Aufgabenliste selbst richtiggestellt:** Die „**7 Klappgruppen gegen
> 1**" sind **7 gegen 5** — und drüben zur Laufzeit **drei**. Zahlenblock, Schnellaktionen,
> Import und Texterkennung **gibt es im Linux-Kopf längst**. *Ein Auftrag ist keine Messung,
> auch wenn er im HANDOFF steht* (§4.56, zum dritten Mal).
>
> **⚠ Was ①c noch vor sich hat:** **Tafel:** Cover-Werkzeug (+ Cover-Vorlagen ins
> Avalonia-csproj, §4.60) und **Formen-Stift** — das einzige fehlende Werkzeug der Leiste.
> **Editor:** Suchen & Ersetzen, der ganze Tabellenentwurf, Formatpinsel, die Galerien, Bild,
> Infobox, Symbol, Diagramm, Navigator, Wortzahl, Lineal, Rückgängig/Wiederholen als Knöpfe.
> **Das ist nach §5 Nr. 26 Phase-4.5-Arbeit unter neuem Namen — Werkzeuge bauen, nicht
> Oberflächen angleichen —, und der Zuschnitt gehört dem Nutzer.**

#### ▶ Die Ordnung von Phase 5 — sie steht in §6, hier nur der Wegweiser

```
①a Vermessen       Befundtabellen je Fläche ✅ erledigt (alle fünf)
①b Angleichen      was beide haben          ✅ erledigt bis auf zwei benannte Reste
①c Werkzeuge       was einem fehlt, bauen   ✅ erledigt (dreizehn Runden, §4.77–§4.92)
② Rückmeldung      was ① übersah, was ① brach ✅ erledigt (§4.93–§4.95)
③ Auslieferung     Flatpak + AppImage       ✅ ERPROBT auf dem Laptop (§4.96)
④ Rest + Prüflauf  toter Code ✅ · §7 ✅ · Avalonia-Issue ✅ (§4.97)
                   §4.1 ✅ · benannte Lücken ✅ · PRÜFLAUF ✅ (§4.99)
⑤ Veröffentlichen  zweiter Bau ✅ (§4.100) · READMEs ✅ · Pages ✅ · Beiwerk ✅
                   1.0.0 ✅ · public ✅ (Nutzer) · Release ▶ wartet auf den Tag (§4.101)
```

**▶ Damit ist Phase 5 zu.** Was noch aussteht, ist **kein Arbeitsschritt, sondern ein
Handgriff am Konto des Nutzers**: der Tag `v1.0.0`, der Pages-Schalter und die Repo-Angaben
— die drei stehen im Kasten ganz oben.

**Der volle Zuschnitt steht in §6, „Phase 5 — die Ordnung", und nur dort.** §5e sagt, womit
die nächste Runde anfängt, und trägt den Prompt. *Ein Plan an drei Stellen ist einer, der an
zweien veraltet.*

**Warum ③ vor ④ steht** (Nutzer): Die Auslieferung ist **das einzige Stück, das noch nie
gelaufen ist** — alles andere ist Nacharbeit an Bekanntem. **⛔ Und was das nicht heißt:**
③ ist eine **Erprobung**, keine Auslieferung. Nach ④ wird noch einmal gebaut, **und erst
dieser zweite Bau geht hinaus** — die Grundregel *„wer nach dem Aufräumen nicht mehr prüft,
veröffentlicht einen Stand, den nie jemand gesehen hat"* ist damit **nicht** gestrichen.

#### ✅ Schritt ① in drei Sätzen — *abgeschlossen mit V2-115, steht hier als Maßstab für ②*

**Beide Köpfe Fläche für Fläche vergleichen, jeden sichtbaren Unterschied beseitigen.**
**Vorlage ist der Linux-Kopf — außer beim Editor, dort ist Windows die Vorlage.**
Gemessen wird **nur unter Windows** (§5 Nr. 25), an **derselben** DB-Kopie, fotografiert mit
**`tools/fenster.ps1`** und nicht mit `schau.ps1` (§4.50). **§5 Nr. 14** (Schriftlisten
zusammenführen) läuft mit. Der Rest steht in §5e.

#### ▶ Die sieben Entscheidungen vom 2026-08-28 — nicht noch einmal fragen

| Nr. | | |
|---|---|---|
| **21** | `HANDOFF.md` | liegt seit V2-93 unter **`Docs/`**, **kein History-Rewrite**. Daraus folgt eine Pflicht statt einer Entscheidung: die Datei einmal ganz auf Privates durchsehen (§6-Checkliste) |
| **22** | Rechtschreibprüfung | als **bekannte Einschränkung** veröffentlichen **und** als **erster Punkt nach M3** fest einplanen — Phase **5.1** |
| **11** | tote Tasten | **melden und stehen lassen**, nicht umgehen |
| **14** | Schriftlisten | **zusammenführen** — mitgeliefert oben, System darunter, in beiden Köpfen |
| **15** | Seitenzahlen im WPF-Editor | **bleiben, wie sie sind**, und werden benannt |
| **23** | Version | **1.0.0**, gesetzt am Ende von ⑤ |
| **24** | Veröffentlichung | **alle vier**: READMEs, Pages, Releases, Beiwerk |
| **25** | Vergleich | **nur unter Windows** — was es nur unter echtem Linux gibt, fällt bei ③ auf |

#### ⚠ Was weiterhin gilt und leicht untergeht

- **Ein grüner Bau beweist an einer Oberfläche fast nichts.** Was am laufenden Programm zu
  sehen ist, wird am laufenden Programm gesehen — in **beiden** Köpfen, an **derselben**
  Datei. Fünf Runden in Folge haben Fehler gefunden, die **kein Wächter sehen konnte**
  (§4.53, §4.55, §4.60, §4.62, §4.68).
- **Ein Auftrag ist keine Messung, auch wenn er im HANDOFF steht** (§4.56).
- **Die Fernsteuerung des Laptops:** `zeiger` taugt nur noch für `hervor`/`fenster`, geklickt
  und getippt wird mit **`ydotool`** (§5 Nr. 20, §7).
- **Drei Beobachtungen am Zeichner, alle offen** (§6, Arbeitsplan): ein Verweis bekommt so
  viele `/Link`-Kästen wie Wörter, `TdRenderer` malt einen Verweis ohne Kennzeichnung (beide
  §4.27), und ein Diagramm kommt im **WPF**-Editor nicht an — mit Absicht (§4.28).
- **§5 Nr. 3** (darf ein PDF ~200 KB je Schriftfamilie wiegen) bleibt bei **(a) so lassen**.
- **Niemand hat je gesehen, wie der unfertige Text aussieht:** Unter Windows entsteht mangels
  Eingabemethode gar keiner, unter Linux schickt IBus an unseren Kontext **überhaupt keine
  Vorschau** — `VorschauMalen` ist bis heute nie gerufen worden.

*(Der volle Verlauf von Phase 3 bis 4.5 stand bis zum 2026-08-28 hier in voller Länge. Er
steht in §4 — dort wird er gesucht — und in der Chronik §9.)*

**Tests laufen lassen:**
```powershell
dotnet test -c Release        # Windows: beide Projekte, 1313 Tests (1244 Core + 69 WPF)
```

```bash
dotnet test tests/GonkNote.Core.Tests   # Linux: nur Core. Zuletzt auf dem Laptop gemessen: 1201 (Stand V2-120);
                                       # seither +43 unter Windows, davon laufen alle auch dort.
```

> **⛔ Und danach: was sagt die CI?** Ein grüner lokaler Lauf sagt darüber nichts — zwischen
> dem 2026-09-01 und dem 2026-09-05 war sie **neun Läufe lang rot**, und keine der Runden
> dazwischen hat es bemerkt (§4.101). Der Aufruf steht in **§8** und braucht keine Anmeldung.

---

