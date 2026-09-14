[← Index: HANDOFF.md](../HANDOFF.md)

## 5d. 🐧 Auftrag für den Linux-Laptop — hier anfangen

> **⚠ GERÄTEWECHSEL (2026-09-08, V2-128): Dieser Abschnitt beschreibt den alten Aufbau.**
> **⛔ HISTORIE (2026-09-09): Diesen Aufbau gibt es nicht mehr — und den Laptop auch nicht.**
> Entwickelt wird auf dem Lenovo Yoga unter Omarchy, und zwar **als einzigem Gerät** (§0).
> Der CachyOS-Laptop ist weg, der Plan „auf ihn kommt Windows als Gegenprobe" ist hinfällig,
> **Dauerregel 3a ist gestrichen**. Dieser Abschnitt wird **nicht nachgezogen**: er steht nur
> noch als Begründungsspeicher da. Wer hier einen Auftrag liest, liest einen Auftrag an ein
> Gerät, das es nicht mehr gibt.

> **Wenn du auf dem CachyOS-Laptop läufst, ist dieser Abschnitt deine Arbeitsanweisung.**
> Der Nutzer muss dir nichts weiter sagen als „lies das HANDOFF". Lies §5b (warum der Laptop
> Messgerät und kein Arbeitsplatz ist), dann arbeite den **aktuellen Auftrag** unten ab.
>
> **Nach jeder Runde auf dem Windows-Rechner wird dieser Auftrag hier neu geschrieben.** Steht
> unten ein Datum, das älter ist als der letzte Eintrag in der Chronik (§9), dann ist er
> veraltet — **dann nachfragen statt raten.**

### 📋 Der Prompt zum Kopieren

**Für den Nutzer:** Das hier in Claude Code auf dem Laptop einfügen — mehr ist nicht zu tippen.
Er steht als Block da, damit er sich in einem Zug markieren lässt, und er nennt bewusst **nur
den Weg zum Auftrag** und nicht den Auftrag selbst: Was zu prüfen ist, steht weiter unten und
wird nach jeder Windows-Runde neu geschrieben. Ein Prompt, der die Aufgabe wiederholt, wäre
dieselbe Angabe an zwei Stellen — und die zweite ist irgendwann die veraltete.

```text
Du laeufst auf dem CachyOS-Laptop. Das Repo liegt in
~/Zed/gonk-note-V2/GonkNote.

Lies dort HANDOFF.md, Abschnitt 5d ("Auftrag fuer den Linux-Laptop") --
der Prompt hier ist AKTUELL (Stand V2-124) und nennt das Wichtigste,
aber §5d ist genauer und gilt.

DU PRUEFST UND MELDEST, DU ENTWICKELST NICHT. Entwickelt wird unter
Windows (§5b) -- dort laufen beide Koepfe nebeneinander, hier nur einer.
Was du findest, wird BEHOBEN, wenn es linuxspezifisch ist, und sonst
gemeldet. NIEMALS die Solution bauen: sie enthaelt den WPF-Kopf, der
hier nicht baut. Das ist Absicht und kein Fehler.

Zieh zuerst den Stand und lauf die Kette ab:

  cd ~/Zed/gonk-note-V2/GonkNote
  git pull
  dotnet build src/GonkNote.Core          # 0 Fehler, 0 Warnungen
  dotnet build src/GonkNote.Avalonia      # der Linux-Kopf
  dotnet test tests/GonkNote.Core.Tests   # erwartet: 1239 Tests, alle gruen

IST HIER SCHON ETWAS ROT, IST DAS DER BEFUND. Nicht weiterarbeiten,
sondern festhalten, was rot ist und warum. Ein Test, der unter Windows
gruen und hier rot ist, ist genau die Sorte Fund, fuer die es diesen
Laptop gibt.

============================================================
ES LIEGT KEIN AUFTRAG AN (Stand V2-124, 2026-09-04)
============================================================

SCHRITT (3) UND SEIN ZWEITER BAU SIND HIER ABGEARBEITET.
V2-119 hat Flatpak und AppImage zum ersten Mal gebaut (§4.96),
V2-122 die mitgelieferte Texterkennung des AppImage gemessen
(§4.98), und V2-124 hat BEIDE Pakete noch einmal gebaut,
installiert und gestartet, nachdem Schritt (4) darin ist --
DAS IST DER BAU, DER HINAUSGEHT (§4.100). Kette dabei gruen:
0/0 und 1239/1239. Gebaut wird nach packaging/LIESMICH.md.

Was jetzt ansteht -- READMEs, GitHub Pages, Releases,
Repo-Beiwerk und die Version 1.0.0 -- ist Windows-Arbeit.

WENN DU DAS HIER LIEST UND IN DER CHRONIK (§9) KEINE RUNDE NACH
V2-124 STEHT: NICHTS MESSEN, NACHFRAGEN. Ein Auftrag entsteht in
§5e, nicht hier.

ZWEI DINGE WARTEN AUF EINE HAND AM GERAET, NICHT AUF EIN SKRIPT --
sie stehen in §5d unter "Was am Geraet von Hand nachzuholen ist":
eine Datei aus dem FLATPAK heraus exportieren oder importieren
(kommt der Portal-Dialog?) und mit dem STIFT auf eine Flaeche
schreiben. Beides ist von einem Skript aus nicht messbar.

ACHTUNG, NEU AUS V2-124 UND ES BETRIFFT JEDEN KUENFTIGEN AUFTRAG:
IN DIESER SITZUNG KONNTE NICHT GEKLICKT WERDEN. ydotoold scheitert
an /dev/uinput (der Nutzer ist NICHT in der Gruppe input), sudo
verlangt ein Passwort, DER SKILL sudopasswot STAND NICHT ZUR
VERFUEGUNG, und wtype/dotool/wlrctl/xdotool sind nicht installiert.
Wer einen Auftrag mit Klicks bekommt, klaert das ZUERST -- die zwei
Auswege stehen in §5d. Und was ohne Klick messbar ist, misst man
mit einer Wegwerf-Sonde gegen den echten Code: ein Klick zeigt,
DASS etwas passiert, eine Sonde zeigt, WAS (§4.98, §4.100).

Alles Weitere steht in §5d -- der Abschnitt ist genauer und gilt.

============================================================
HAUSREGELN, DIE HIER TEUER SIND
============================================================

 - KEINE KOPIE DER ECHTEN DATENBANK. Nutzer-Entscheidung 2026-08-03:
   die Schulunterlagen bleiben auf dem Windows-Rechner. Hier wird mit
   frisch angelegten Dokumenten gemessen.
 - NIE OHNE --db starten, solange geprueft wird. Der Datenordner ist
   ~/.config/GonkNote.
 - Wer prueft, ob etwas ENTSTEHT oder VERSCHWINDET, faengt mit einer
   LEEREN Flaeche an. Auf einer Flaeche, auf der schon etwas liegt,
   misst man zwei Dinge auf einmal (§4.56, zwei Scheinbefunde).
 - EIN GRUENER BAU BEWEIST AN EINER EINGABE-NAHT FAST NICHTS. 1012
   Waechter waren gruen, waehrend der Zahlenblock GAR NICHT aufging
   (§4.62). 913 waren gruen, waehrend getipptes "Hallo" Werkzeuge
   umschaltete (§4.55).
 - XDOTOOL IST HIER NICHT INSTALLIERT (gemessen V2-86).
 - ZEIGER KLICKT UND TIPPT NICHT MEHR (gemessen V2-89, §4.64). XTEST
   haengt unter GNOME 50 am Portal: das erste Ereignis oeffnet
   "Entfernter Bildschirm", und SOLANGE DER DIALOG STEHT KOMMT GAR
   KEIN EREIGNIS AN -- auch nicht beim XWayland-Client. Der Dialog ist
   eine WAYLAND-Oberflaeche, XTEST erreicht ihn NICHT; ein Opferklick
   hilft nicht (die alte Angabe hier war falsch). Die Freigabe wird
   nicht gespeichert, und eine Bildschirmsperre nimmt sie zurueck.
 - ZUM KLICKEN UND TIPPEN NIMM YDOTOOL (installiert, 1.0.4). Daemon:
   sudo ydotoold --socket-path=/tmp/.ydotool_socket --socket-own=1000:1000
   dann YDOTOOL_SOCKET=/tmp/.ydotool_socket. Es spritzt ueber
   /dev/uinput auf evdev-Ebene ein, also unterhalb des Portals.
 - ZWEI FALLEN IN YDOTOOL, beide haben in V2-89 einen Scheinbefund
   erzeugt: (a) sein Geraet hat KEINE absoluten Achsen, 'mousemove -a'
   ist nachgebildet -- accel-profile auf 'flat', mit -9000/-9000 am
   Anschlag (0,0) verankern, dann Strecke mal 1,6 (logisch->Geraet)
   geteilt durch 0,8133. Nachmessen per XQueryPointer geht NICHT.
   (b) Es tippt ROHE KEYCODES gegen die deutsche Belegung: Keycode 44
   ist Y, nicht Z -- STRG+Z IST KEYCODE 21.
 - ZEIGER TAUGT WEITER FUER 'hervor' UND 'fenster' -- das ist
   XRaiseWindow und kein XTEST, es loest den Dialog nicht aus.
 - DIE SITZUNG SPERRT SICH MITTEN IN DER MESSUNG SELBST AUS:
   idle-delay 300, lock-delay 0, und XTEST/XWayland setzen den
   Leerlaufzaehler NICHT zurueck. Fuer die Dauer der Messung
   'gsettings set org.gnome.desktop.session idle-delay 0', danach
   wieder auf 300.
 - ZWEI AUFNAHMEARTEN, und die falsche zeigt zu wenig:
   tools/linux/schau.sh fotografiert EIN X-Fenster -- ein Popup, ein
   Menue oder der Zahlenblock haengt in einem EIGENEN Fenster und ist
   darauf nicht drauf. Fuer alles, was aufklappt, den ganzen Schirm
   aufnehmen: tools/linux/wlschuss.sh.
 - sudo ueber den Skill "sudopasswot", nicht den Nutzer fragen. DER
   SKILL-INHALT GEHOERT NIE INS REPO.
 - ALLES AUF DEUTSCH (Dauerregel 3) -- Code, Kommentare, Commits, der
   Befund.

DER BEFUND KOMMT ALS TEXT INS HANDOFF UND WIRD GEPUSHT, NICHT IN DEN
CHAT. Was im Chat steht, findet der naechste Thread nie wieder. Schreib
ihn in den betroffenen §4-Abschnitt unter "Was der Laptop gefunden hat",
mit Datum und Rundennummer, dazu eine Zeile in die Chronik (§9).

Sag mir am Ende, ob der Windows-Rechner wieder dran ist.

Fang an.
```

> **Warum dieser Prompt ausführlich ist — und bis V2-84 kurz war.** Er stand bewusst knapp da,
> mit dem Argument: *„ein Prompt, der die Aufgabe wiederholt, wäre dieselbe Angabe an zwei
> Stellen — und die zweite ist irgendwann die veraltete."* **Das Argument stimmt — solange der
> Prompt nicht mitgepflegt wird.** Der Windows-Prompt in §5e wird nach jeder Runde neu
> geschrieben und ist deshalb lang; **hier gilt jetzt dasselbe**, weil dieser Auftrag zum
> ersten Mal seit V2-62 wieder etwas trägt und weil er zwei sehr verschiedene Dinge verlangt
> (eine Messung ohne Bauen und einen Augenschein). **Steht unten ein Datum, das älter ist als
> der oberste Eintrag in der Chronik (§9), ist auch dieser Prompt veraltet — dann nachfragen
> statt raten.**
>
> Die vier Punkte, bei denen ein Fehlgriff **teuer** ist, stehen weiterhin darin: unter Windows
> entwickeln zu wollen, die Solution zu bauen (sie enthält den WPF-Kopf und **kann** hier nicht
> bauen), auf Englisch zu schreiben, und den Befund im Chat stehen zu lassen, wo ihn der
> nächste Thread nie wieder findet.

### Was hier grundsätzlich gilt — für jeden Auftrag

| Regel | Warum |
|---|---|
| **Nur prüfen und melden, nicht entwickeln** | Entwickelt wird unter Windows (§5b): dort laufen beide Köpfe nebeneinander an derselben Datenbank und die Fernsteuer-Werkzeuge funktionieren. Hier wird beantwortet, was **nur** hier zu beantworten ist |
| **Beheben nur, was linuxspezifisch ist** | Ein Fehler, der auf beiden Systemen auftritt, gehört nach Windows — dort ist er schneller zu finden. Ein Fehler, den es nur hier gibt, wird hier behoben |
| **Niemals die Solution bauen** | Sie enthält den WPF-Kopf, der unter Linux nicht baut. **Das ist Absicht** und kein Fehler. Projektbezogen bauen: `dotnet build src/GonkNote.Core` |
| **Keine Kopie der echten Datenbank** | Nutzer-Entscheidung 2026-08-03: Die Schulunterlagen bleiben auf dem Windows-Rechner. Selbst angelegte Notizbücher sind für den Eingabepfad ohnehin die bessere Prüfung. Dauerregel 4 erlaubt die Kopie — hier wird sie nicht gebraucht |
| **Alles auf Deutsch** | Dauerregel 3 — Code, Kommentare, Commits, dieser Text |
| **`sudo` über den Skill `sudopasswot`** | Nicht den Nutzer nach dem Passwort fragen. **Der Skill-Inhalt gehört nie ins Repo** |
| **⛔ Klicken und Tippen geht nur noch über `ydotool`** | Seit GNOME 50 hängt XTEST am „Entfernter Bildschirm"-Portal, und der Freigabe-Dialog ist eine **Wayland**-Oberfläche — XTEST kann ihn nicht bedienen, und solange er steht kommt **kein** Ereignis irgendwo an. **In V2-89 gemessen** (§4.64). `tools/linux/zeiger` bleibt für `hervor` und `fenster` (`XRaiseWindow`, kein XTEST). **`ydotool` tippt rohe Keycodes gegen die deutsche Belegung: Strg+Z ist 21, nicht 44** |
| **Zwei Aufnahmearten, und die falsche zeigt zu wenig** | `tools/linux/schau.sh` fotografiert **ein X-Fenster** — schnell, aber **ohne Menüs, Flyouts und jede Wayland-Oberfläche** (auch ohne die Bildschirmtastatur). `tools/linux/wlschuss.sh` holt über das Portal den **ganzen Bildschirm** und zeigt beides. **Wer einen Klick für wirkungslos hält, prüft ihn erst mit `wlschuss.sh` nach** — in V2-55 war ein offenes Menü dreimal schlicht nicht mit auf dem Bild (§7). Achtung: `wlschuss.sh` liefert **logische** Pixel, `zeiger` will **Geräte**pixel (hier Faktor 1,6) |

**Das Repo liegt in `~/Zed/gonk-note-V2/GonkNote`**, Remote über SSH, Key ohne Passphrase —
Git-Befehle laufen ohne Vorbereitung (§5c). Der SDK-Stand und die Werkzeuge stehen in §5b
(„Stand des Laptops"); **das ist alles schon eingerichtet, nicht wiederholen.**

### Der Auftrag zum Ablaufen — immer diese Reihenfolge

```bash
cd ~/Zed/gonk-note-V2/GonkNote
git pull                                   # der Windows-Rechner hat vorgelegt
dotnet build src/GonkNote.Core             # Meilenstein M0 -- 0 Fehler, 0 Warnungen
dotnet build src/GonkNote.Avalonia         # der Linux-Kopf
dotnet test tests/GonkNote.Core.Tests      # muss vollstaendig gruen sein
```

**Wenn hier schon etwas rot ist, ist das der Befund** — nicht weiterarbeiten, sondern
festhalten, was rot ist und warum. Ein Test, der unter Windows grün und hier rot ist, ist genau
die Sorte Fund, für die es diesen Laptop gibt.

Danach den Kopf ansehen:

```bash
dotnet run --project src/GonkNote.Avalonia -- --db /tmp/gonk-test/gonknote.sqlite
```

### Wie der Befund zurückkommt

**Nicht im Chat stehen lassen** — der nächste Thread liest ihn dort nicht. Sondern:

1. In den betroffenen §4-Abschnitt einen Block **„Was der Laptop gefunden hat"** schreiben,
   mit Datum. Auch wenn alles in Ordnung war: **„geprüft, nichts gefunden" ist ein Ergebnis**
   und verhindert, dass es jemand noch einmal prüft.
2. Eine Zeile in die **Chronik** (§9).
3. Was offen bleibt, nach **§5 „Noch offen"** oder §5a „Offen".
4. Committen und pushen — der Windows-Rechner holt es mit `git pull`.

---

### ▶ Aktueller Auftrag — **▶ es liegt keiner an** (Stand 2026-09-05, nach Runde V2-125)

> **▶ V2-125 war eine reine Windows-Runde** (Phase 5, Schritt ⑤ — §4.101): Version 1.0.0,
> READMEs, Projektseite, Auslieferungs-Workflow, Beiwerk. **Für den Laptop ist daraus
> nichts entstanden.** Er wird das nächste Mal gebraucht, wenn ein **Release** existiert
> und jemand das **fertige AppImage von der Release-Seite** auf echtem Linux startet —
> bis dahin gilt der Stand aus V2-124.
>
> **⛔ Und der Werkzeugbefund von damals gilt weiter:** Er konnte in V2-124 **nicht
> klicken**. Wer ihm den nächsten Auftrag schreibt, klärt das **zuerst** — die zwei
> Auswege stehen gleich darunter.

> **✅ DER ZWEITE BAU IST GELAUFEN, UND ER IST DER, DER HINAUSGEHT** (§4.100, V2-124).
> Alle fünf Punkte des Auftrags aus V2-123 sind abgearbeitet. Kette grün: Bau **0/0**,
> **1239/1239** — genau die Zahl, die hier stand.
>
> **▶ HIER LIEGT NICHTS AN.** Wer den Laptop-Prompt einwirft und diesen Abschnitt liest,
> ohne dass in der Chronik (§9) eine Runde **nach V2-124** steht: **nichts messen,
> nachfragen.** Ein Auftrag entsteht in §5e, nicht hier. Was jetzt ansteht — READMEs,
> GitHub Pages, Releases, Repo-Beiwerk, Version **1.0.0** — ist Windows-Arbeit.

#### ▶ Was als Nächstes auf ihn zukommt — **zwei Dinge, beide noch nicht bestellt**

1. **Das fertige AppImage von der Release-Seite starten**, sobald der Tag steht. Nicht das
   selbst gebaute — **das von der Seite**, denn die Kette „`release.yml` → Artefakt →
   Download" ist nie gelaufen (§4.101).
2. **Der Flathub-Quellbau** (§4.102, §6): `flathub-build` und **zweimal** `flatpak-builder-lint`
   (Manifest **und** Repo). ⛔ **Erst, wenn das Manifest umgebaut ist** — heute packt es ein
   fertiges `dotnet publish`-Ergebnis ein, und genau das lehnt Flathub ab. **Und es ist ein
   anderer Bau als die zwei, die er kennt.**

**Beides braucht nur die Kommandozeile** — das Klick-Problem unten steht dem nicht im Weg.

#### ⛔ Bevor der Laptop das nächste Mal etwas messen soll: **er konnte in V2-124 nicht klicken**

Das ist der teuerste Befund dieser Runde, und er gehört an den Anfang, weil er den Zuschnitt
jedes künftigen Auftrags bestimmt:

| Glied | Befund (2026-09-04, V2-124) |
|---|---|
| `ydotoold` | `failed to open uinput device: Permission denied` |
| `/dev/uinput` | `crw-rw---- root input` — der Nutzer `gonk` ist **nicht** in der Gruppe `input` |
| `systemctl --user start ydotool.service` | `failed`, dieselbe Ursache |
| `sudo -n true` | `sudo: Ein Passwort ist notwendig` |
| **Der Skill `sudopasswot`** | **stand der Sitzung nicht zur Verfügung** — weder unter `.claude/skills/` noch global |
| `wtype`, `dotool`, `wlrctl`, `evemu-play`, `xdotool` | **alle nicht installiert** |

**Die Hausregel „`sudo` über den Skill" setzt voraus, dass es den Skill gibt.** Steht er
nicht bereit, ist der Laptop für alles Bediente stumm — und ein Auftrag, der einen Klick
verlangt, kommt dann leer zurück. **Zwei Auswege, beide vorher zu klären:**

1. **Den Skill bereitstellen**, dann trägt `ydotool` wie in V2-89 beschrieben.
2. **Den Nutzer einmalig in die Gruppe `input` aufnehmen** (`usermod -aG input gonk`, danach
   neu anmelden). Dann braucht `ydotoold` kein `sudo` mehr — **der dauerhaftere Weg.**

*Und die Lehre für den Zuschnitt: V2-124 hat die zwei bedienten Punkte nicht offen gelassen,
sondern mit einer **Wegwerf-Sonde gegen den echten Code** beantwortet — dieselbe Wahl wie
§4.98. Ein Klick zeigt, **dass** etwas passiert; die Sonde zeigt, **was**.*

#### ✅ ABGEARBEITET AM 2026-09-04 (V2-124): der zweite Bau, und beide Pakete gehen hinaus

> | Punkt | Ergebnis |
> |---|---|
> | 1 · Kette | ✅ Bau **0/0** in Core und im Linux-Kopf, **1239/1239** — die Zahl aus §5d, ohne Abweichung |
> | 2 · Flatpak | ✅ neu gebaut, installiert, gestartet (**154,6 MB**, Plattform 25.08). In der Sandbox: `/app/lib` mit Tesseract **5.5.1** und Leptonica, `ldd` ohne fehlende Abhängigkeit, `deu`/`eng` als `/app/gonknote/tessdata` mit im Paket |
> | 3 · AppImage | ✅ neu gebaut und gestartet (**86 MB**). Beipack unverändert **43 Dateien / 65 MB**, `TesseractBindung.cs` seit **V2-121** unberührt — die Messung aus §4.98 ist damit **bestätigt** und keine neue Frage |
> | 4 · Markdown-Import | ✅ **gemessen, aber nicht durch den Dialog** (siehe Werkzeugbefund). `#####` bleibt ein **Absatz**, `![alt](fehlt.png)` wird **`[alt]`**, `~~…~~` und `***…***` tragen, Tabelle 3×3, **zwei** Listendefinitionen. **Zeile für Zeile das, was §4.99 unter Windows sah** |
> | 5 · Die zwei Verweise | ✅ **beide richtig.** „Feature-Übersicht im README“ wird **angenommen**; `THIRD-PARTY-NOTICES.md` bleibt in **beiden** Sprachen schlichter Text — ohne Akzentfarbe, ohne Unterstrich, ohne Handzeiger |
>
> **✅ Nebenbei ist der Fund aus §4.96 zu:** in **beiden** Paketen liegen **null** native
> Windows-Binärdateien und kein `x64/`/`x86/`. *Die 12 MB sind weg.*
>
> **⚠ Und beinahe wäre ein Scheinbefund daraus geworden:** Der erste Zähler sagte **200
> PE32-Dateien** — `file` meldet **jede** .NET-Assembly so, auch die auf Linux laufende.
> Erst mit `grep "for MS Windows"` ohne `Mono/.Net assembly` misst man die Frage, und die
> Antwort ist **0**. *Der teuerste Fehler einer Messrunde ist der am Messgerät (§4.56, §4.82).*
>
> **⛔ Drei veraltete Sätze in `packaging/` behoben** (§5d erlaubt genau das), alle seit
> V2-122 falsch: zweimal „noch nicht am Gerät geprüft“ (`LIESMICH.md` und der Kopf- **und**
> Schlusstext von `appimage/bauen.sh`) und einmal „Schritt ③ ist eine Erprobung … erst der
> zweite Bau geht hinaus“. **Kein Produktivcode angefasst.** Der Warnhinweis im `bauen.sh`
> ist dabei nicht verschwunden, sondern **richtig herum gestellt**: ein Lauf *auf dem
> Baurechner* beweist die Erkennung nicht, weil hier ein System-Tesseract liegt.
>
> **⚠ Der Posten, der aus dieser Runde für Windows übrig bleibt:** Die Version steht
> **überall auf 0.3.0** — `flatpak info`, `Directory.Build.props` und der einzige
> `<release>`-Eintrag der `metainfo.xml`, der sich selbst *„packaging trial, not a release“*
> nennt. **§5 Nr. 23/24 verlangt 1.0.0.**
>
> Der volle Befund steht in **§4.100**.

#### ✅ ABGEARBEITET AM 2026-09-04 (V2-122): das AppImage bringt seine Texterkennung mit, und es ist gemessen

> Kette vorher grün: Bau **0/0** in Core und im Linux-Kopf, **1205/1205**. *(§5d nannte hier
> 1201 — das war der Stand von V2-119; §4.98 hat unter Windows vier Wächter dazugebaut.)*
>
> **✅ Die Kernfrage ist beantwortet, und mit dem schärfsten verfügbaren Instrument:** Die
> Erkennung im AppImage kommt **aus dem Beipack und aus nichts sonst** — zeichengenau, während
> das System-Tesseract im selben Namensraum tot war, und `/proc/self/maps` nennt beide
> Bibliotheken **namentlich mit vollem Pfad**. Dazu die harte Fassung mit **20 versteckten
> Wirtsbibliotheken der ganzen Kette**: unverändert zeichengenau.
>
> | Schritt | Ergebnis |
> |---|---|
> | 1 · Bauen | ✅ Schritt 4/6 nennt beide, 5/6 listet **43 Dateien, 65 MB** |
> | 2 · Was drin liegt | ✅ `libtesseract.so.5` und `libleptonica.so.6` unter **genau** diesen Namen, als echte Dateien; `ldd` ohne `not found`. **⚠ Der `ln -sf`-Zweig für den CMake-Namen `…so.5.5` ist dabei nie gelaufen** — Arch liefert den kurzen Namen selbst |
> | 3 · Mit verstecktem System | ✅ **zeichengenau**, geladen aus `usr/bin/lib` |
> | 4 · Gegenprobe **mit** System | ✅ zeichengenau — **und geladen wird trotzdem der Beipack** |
> | Größe | **61 → 85 MiB**, der Beipack kostet **24,5 MiB (+40 %)** |
>
> **⛔ Zwei Funde, und beide standen nicht im Auftrag:**
>
> 1. **Die benannte Grenze aus §4.98 ist jetzt gemessen und nicht mehr hergeleitet.** Ein
>    unbrauchbares `libtesseract.so.5` im Beipack lässt die Erkennung scheitern, **obwohl ein
>    tadelloses System-Tesseract danebenliegt** — das Wirtssystem wird nie gefragt. **Nicht
>    behoben** (gehört nach Windows).
> 2. **.NET nimmt sein ICU aus dem Beipack.** Der `AppRun` stellt seinen Ordner voran, und
>    `ldd` zieht über `libtesseract → libarchive → libxml2` ICU herein — **die Hälfte der
>    24,5 MiB.** Gemessen mit `LD_DEBUG=libs`. **Es ist trotzdem richtig so**, und beide
>    Gründe sind gemessen: .NET probiert die ICU-Versionen abwärts und fällt auf die des
>    Wirts zurück, **und ohne die Dateien lädt die Kette auf einer fremden Verteilung gar
>    nicht.** *Der `AppRun`-Kommentar war zu eng gefasst und ist berichtigt.*
>
> **In `packaging/` behoben** (§5d erlaubt genau das): der `AppRun`-Kommentar und eine
> verrutschte erste Zeile in der Dateiliste von `bauen.sh`. **Kein Produktivcode angefasst.**
> Der volle Befund steht in **§4.98 „Was der Laptop gefunden hat"**.
>
> **⚠ Offen bleibt nur, was eine Hand braucht:** die Erkennung **durch die Oberfläche** des
> AppImage. Klicken geht auf diesem Gerät nur mit `ydotool` samt `sudo`-Dienst. *Das ist kein
> Loch — die Sonde hat die Frage schärfer beantwortet, als ein Klick es könnte: sie sagt,
> **welche Datei** geladen ist.*

> ✅ **Die Zählfrage aus §4.96 ist unter Windows beantwortet (§4.97): 1201 Core + 65 WPF = 1266** — **die Zahl,
> die der Laptop gemessen hat, war die richtige.** §0 und §2 sind nachgezogen. *Der Laptop hatte nichts Eigenes
> gemessen, sondern den aktuellen Stand — und die Doku den vorletzten.*

> **✅ ABGEARBEITET AM 2026-09-04 (V2-119): die Auslieferung ist gebaut und gestartet.**
> Kette grün (Bau 0/0, **1201/1201**), dann **Flatpak und AppImage** — beide laufen, beide
> sind auf dem Schirm **pixelgleich** zu einem gewöhnlichen `dotnet publish`-Lauf. Der volle
> Befund steht in **§4.96**, gebaut wird nach **`packaging/LIESMICH.md`**.
>
> **Die fünf Fragen, die hier seit dem 2026-08-28 standen, sind beantwortet:**
>
> | Frage | Antwort |
> |---|---|
> | Baut das Manifest? | ✅ Ja. `org.freedesktop.Platform 25.08`, **`leptonica 1.85.0` und `tesseract 5.5.1` aus dem Quelltext mitgebaut**, `fontconfig`/`freetype` aus der Plattform |
> | Trägt die Tesseract-Bindung **in der Sandbox**? | ✅ **Ja, zeichengenau gemessen** — mit einer Wegwerf-Sonde im Namensraum des Pakets. Die Verweise zeigen auf `/app/lib`, genau wie `Suchpfade` es seit §4.63 vorsieht |
> | Finden Datenordner, Blobs und Sticker sich wieder? | ✅ Ja, unter `~/.var/app/io.github.gonkstupid.GonkNote/config/GonkNote`. **Und der echte Bestand ist von dort aus unsichtbar** — im Flatpak erfüllt die Sandbox Dauerregel 4 von selbst |
> | Kommt der Stift durch das Portal? | ⚠ **Nicht gemessen, und von hier aus grundsätzlich nicht messbar** — `uinput` erzeugt keine Digitizer-Achsen (§4.62). **Wartet auf den Nutzer am Gerät** |
> | Was es nur unter echtem Linux gibt (§5 Nr. 25) | ⚠ **Teilweise:** Schrift, Fensterdekoration und Maßstab sind auf **diesem** Gerät pixelgleich. Was fontconfig auf einer **fremden** Verteilung als Rückfall wählt, fällt hier nicht auf |
>
> **⚠ Und eine sechste, die erst hier entstanden ist:** der **Dateidialog** in der Sandbox.
> Das Manifest verzichtet bewusst auf `--filesystem=home`; **das Portal antwortet aus der
> Sandbox** (`FileChooser`, Version 4), **der Dialog selbst ist ungeprüft** — dafür muss
> geklickt werden, und `ydotool` braucht einen Dienst mit `sudo`, den diese Sitzung nicht
> starten konnte.
>
> *(Der Wegweiser, der hier stand — „was jetzt anliegt: nichts, keine Runde nach V2-119" —
> ist am 2026-09-04 entfernt worden: er war seit dem Auftrag von V2-121 falsch. **Was jetzt
> anliegt, steht in genau einer Überschrift, der obersten.** Zwei Wegweiser in einem
> Abschnitt sind einer, der veraltet, und man liest den erstbesten.)*

#### ⚠ Was am Gerät von Hand nachzuholen ist — es ist kein Auftrag, sondern ein Handgriff

Beides im **Flatpak** (`flatpak run io.github.gonkstupid.GonkNote`), beides braucht nur ein
paar Sekunden und eine echte Hand:

1. **Eine Datei exportieren oder importieren.** Kommt der Portal-Dialog, und liegt die Datei
   danach wirklich dort, wo sie hin soll? **Wenn nein, ist die Antwort `--filesystem=home`
   im Manifest** — dann aber bitte mit einer Zeile Begründung daneben, denn es nimmt der
   Sandbox ihren Sinn.
2. **Mit dem Stift auf eine Fläche schreiben.** Druck, Neigung, Handballen — dieselbe Probe
   wie §5a, nur diesmal in der Sandbox.

#### Der abgearbeitete Stand darunter — Buchhaltung


> **✅ Der Auftrag aus V2-87 ist abgearbeitet** (§4.64 „Was der Laptop gefunden hat"): sechs
> Fragen, sechs Antworten, **die Texterkennung trägt unter Linux am laufenden Programm** und
> der Fokus-Fix aus §4.65 ist ohne Regression. Bau 0/0, **976/976 grün**.
>
> **▶ Hier liegt nichts an.** Wer den Laptop-Prompt einwirft und diesen Abschnitt liest,
> ohne dass in der Chronik (§9) eine neue Windows-Runde steht: **nichts messen, nachfragen.**
> Ein Auftrag entsteht in §5e, nicht hier.
>
> **▶ Was sich seit V2-89 hier geändert hat, und es ist nur Buchhaltung:** Windows hat mit
> **V2-90** M2 ausgerufen und den ältesten offenen Laptop-Fund erledigt (§4.67, der
> Wortzwischenraum an der Stückgrenze, offen seit dem 2026-08-11). **Daraus entsteht kein
> Auftrag** — die Änderung sitzt in Core, betrifft beide Köpfe gleich und ist unter Windows am
> laufenden Programm gesehen worden. Nach der Regel oben („nur, was **hier** zu beantworten
> ist") gibt es hier nichts zu messen.
>
> **⚠ Ein Nachtrag zum alten Befund, damit ihn niemand zweimal sucht:** Die Angabe aus §4.28,
> der Fehler sei ein **Anzeige**problem und im PDF nicht vorhanden, **stimmt nicht** — Anzeige
> und PDF gehen durch denselben Umbruch und denselben Zeichner (§4.67). Verglichen worden
> waren damals Zeilen- und Seitenumbruch, nicht die Breite einer Lücke. *Kein Vorwurf an die
> Messung: sie hat gesehen, was da war, und ausdrücklich „Vermutung" darübergeschrieben.*
>
> **⛔ Wenn in Phase 5 doch ein Auftrag entsteht, wird dieser Abschnitt neu geschrieben** —
> und **zuerst** der Werkzeug-Abschnitt oben gelesen.
>
> **⛔ Und wer doch etwas misst, liest zuerst den Werkzeug-Abschnitt oben.** Seit V2-89 ist
> `zeiger` **nicht** mehr das Werkzeug zum Klicken und Tippen — XTEST ist unter GNOME 50 ohne
> Handbedienung nicht mehr benutzbar (§4.64). Das kostet sonst eine halbe Runde.

#### Was weiterhin auf den Nutzer am Gerät wartet, nicht auf einen Auftrag

Eine **Xorg-Sitzung** als Vergleich (§5a „Offen" 2), die **Druckschwelle unten**
(§5a „Offen" 3) und **Compose** (hier nach wie vor nicht eingerichtet). Dazu der
**Zahlenblock und die Klappgruppen mit dem Stift** statt der Maus — XTEST erzeugt eine Maus
und keinen Digitizer, das ist von hier aus grundsätzlich nicht messbar (§4.62), und mit
`ydotool` ändert sich daran nichts: `uinput` erzeugt ebenfalls keine Digitizer-Achsen.

### Abgearbeitete Aufträge — nur die Kurzfassung

> **Hier steht bewusst nur, *dass* etwas geprüft wurde und *wo* der Befund steht.** Der
> vollständige Wortlaut eines abgearbeiteten Auftrags hat keinen Leser mehr: was
> herausgekommen ist, gehört in den §4-Abschnitt (dort wird es gesucht), und der Auftrag
> selbst steht in der Git-Historie. Bis zum 2026-08-11 stand er hier noch einmal in voller
> Länge — achtzig Zeilen, die niemand mehr brauchte.

| Auftrag | Datum | Ergebnis |
|---|---|---|
| **Der zweite Bau, und dieser geht hinaus** (§6, Schritt ⑤ Punkt 1) — Kette ablaufen, **beide** Pakete neu bauen und starten, den seit V2-123 ungeprüften Markdown-Import ansehen und die zwei Verweise nachprüfen | 2026-09-04 (V2-124) | ✅⛔ **Alle fünf Punkte abgearbeitet; Bau 0/0, 1239/1239 — genau die aus Windows genannte Zahl.** **Flatpak (154,6 MB) und AppImage (86 MB) neu gebaut, installiert und gestartet**; Beipack unverändert **43 Dateien / 65 MB**, `TesseractBindung.cs` seit V2-121 unberührt, in der Sandbox `/app/lib` mit Tesseract **5.5.1** und `deu`/`eng` im Paket. **✅ Der Fund aus §4.96 ist zu:** in beiden Paketen **null** native Windows-Binärdateien. **⛔ Der Werkzeugbefund, der den Zuschnitt änderte: es konnte nicht geklickt werden** — `ydotoold` scheitert an `/dev/uinput` (nicht in Gruppe `input`), `sudo` verlangt ein Passwort, **der Skill `sudopasswot` stand nicht zur Verfügung**, alle Ausweichwerkzeuge fehlen. **Punkt 4 und 5 deshalb mit einer Wegwerf-Sonde gegen den echten Code beantwortet statt gar nicht** — *ein Klick zeigt, dass etwas passiert, die Sonde zeigt was*: der Markdown-Import liest **zeichengenau das, was §4.99 unter Windows sah** (`#####` bleibt Absatz, `![alt]` wird `[alt]`, `~~…~~`/`***…***`, Tabelle 3×3, zwei Listendefinitionen), und **beide Verweisfragen fallen richtig aus** („Feature-Übersicht im README“ wird angenommen, `THIRD-PARTY-NOTICES.md` bleibt in beiden Sprachen schlichter Text). **⚠ Beinahe ein eigener Scheinbefund:** `file … PE32` sagte 200 — das meldet `file` für jede .NET-Assembly; richtig gemessen sind es **0**. **⛔ Drei veraltete Sätze in `packaging/` behoben**, alle seit V2-122 falsch. **⚠ Ungeprüft geblieben:** der Portal-Dateidialog und der Stift (beides braucht eine Hand). **⚠ Für Windows übrig: die Version steht überall auf 0.3.0**, §5 Nr. 23/24 verlangt 1.0.0. Befund: **§4.100** |
| **Das AppImage mit mitgelieferter Texterkennung bauen und messen** (§5 Nr. 29) — baut das Skript, liegen die zwei Namen richtig drin, kommt die Erkennung wirklich aus dem Beipack, und wie viel kostet er? | 2026-09-04 (V2-122) | ✅⛔ **Alle vier Schritte abgearbeitet; Bau 0/0, 1205/1205 grün.** **Die Erkennung kommt aus dem Beipack und aus nichts sonst** — zeichengenau bei **verstecktem System-Tesseract** (`bwrap`, Gegenprobe am Werkzeug zuerst: `tesseract --version` scheitert dort selbst), und **`/proc/self/maps` nennt beide Bibliotheken namentlich**; die harte Fassung mit **20 versteckten Wirtsbibliotheken** der ganzen Kette bleibt zeichengenau. **Schritt 4 fällt besser aus als befürchtet:** mit vorhandenem System lädt **trotzdem der Beipack**. **Gemessen mit einer Wegwerf-Sonde im Namensraum des Pakets** — die schärfere Wahl, denn *ein Klick hätte gezeigt, dass etwas erkannt wird, nicht woher*. **Größe: 61 → 85 MiB, der Beipack kostet 24,5 MiB (+40 %).** **⛔ Zwei Funde:** (1) **die benannte Grenze aus §4.98 ist jetzt gemessen** — ein unbrauchbares `libtesseract.so.5` im Beipack schaltet ein tadelloses System-Tesseract **aus** (nicht behoben, gehört nach Windows); (2) **.NET nimmt sein ICU aus dem Beipack** (`LD_DEBUG=libs`), es ist **die Hälfte der 24,5 MiB** und kommt über `libtesseract → libarchive → libxml2` — **harmlos** (.NET fällt von 90 bis 60 abwärts auf die Fassung des Wirts zurück) **und nicht wegzulassen** (unser `libxml2` verlangt es namentlich), aber der `AppRun`-Kommentar sagte etwas anderes. **⛔ Und §4.98 hatte denselben falschen Satz an zwei Stellen und nur eine berichtigt** — die zweite steht jetzt richtig in §5 Nr. 29. **⚠ Ungeprüft geblieben:** der `ln -sf`-Zweig für den CMake-Namen `…so.5.5` (Arch liefert den kurzen Namen selbst) und die Erkennung **durch die Oberfläche** (braucht einen Klick, `ydotool` braucht `sudo`). **Zwei Handgriffe in `packaging/` behoben.** Befund: **§4.98 „Was der Laptop gefunden hat"** |
| **Die Auslieferung bauen und starten** — baut das Manifest, trägt Tesseract in der Sandbox, findet der Datenordner sich wieder, kommt der Stift durch das Portal? | 2026-09-04 (V2-119) | ✅⚠ **Vier von fünf beantwortet; Bau 0/0, 1201/1201 grün.** **Flatpak und AppImage laufen** und sind auf dem Schirm **pixelgleich** zu einem `dotnet publish`-Lauf (`compare -metric AE` = **0**). **Die Texterkennung trägt in der Sandbox**, zeichengenau, gegen das im Manifest **mitgebaute** `/app/lib` — `Suchpfade` führte es seit §4.63 an erster Stelle. Der Datenordner liegt unter `~/.var/app/…/config/GonkNote`, **und der echte Bestand ist von dort unsichtbar**. **⛔ Drei Funde:** `--socket=fallback-x11` tötet den Start in einer Wayland-Sitzung (Avalonia 12 hat nur X11); Tesseract aus CMake heißt `so.5.5` und nicht `so.5`; `dotnet publish -r linux-x64` schleppt **12 MB Windows-DLLs** mit. **⚠ Der Stift bleibt ungemessen** (`uinput` erzeugt keine Digitizer-Achsen) und **der Dateidialog auch** (`ydotool` braucht `sudo`). Befund: **§4.96** |
| **Trägt die Texterkennung unter Linux?** — richtet der gebaute Handgriff die drei Namen, und ist der Fokus-Fund weg? | 2026-08-28 (V2-89) | ✅⚠ **Beides ja; Bau 0/0, 976/976 grün.** **① Der Handgriff trägt:** die App legt beide Verweise **selbst** an (`libtesseract50.so → /usr/lib/libtesseract.so.5`, `libleptonica-1.82.0.so → /usr/lib/libleptonica.so.6`), und zwar **erst beim ersten `IsAvailable`** — der Ordner war vorher gar nicht da. Sprachdaten liegen mit (`deu+eng`). **Der Knopf steht in den Schnellaktionen**, zwischen Einfügen und Löschen mit eigenem Trenner (er wird ausgeblendet statt ausgegraut) — **aber der Beweis ist er nicht: `IsAvailable` prüft nur die Sprachdaten, das `dlopen` passiert erst in `Recognize`.** *§4.64 sagt das selbst; diese Zeile hier hat es bis zum 2026-09-04 weggelassen, und in V2-122 ist es nachgemessen worden.* **Erkannt wird zeichengenau** (`Hallo Welt 123` / `Aeltere Baeume`), gemessen **zweimal**: Wegwerf-Sonde gegen `TesseractOcrEngine` und im Fenster des laufenden Kopfs. **Der ganze Weg trägt**, samt **Als Notizzettel** und **Strg+Z direkt danach ohne Klick**. **② §4.65 gegengeprüft:** erster Strg+V nach dem Öffnen kommt an, **und** ein neu angelegtes Board lässt sich weiterhin sofort umbenennen — keine Regression. **Kein `fehler.log` auf diesem Gerät.** **⛔ Drei Werkzeugfunde, und sie wiegen schwer:** XTEST ist unter GNOME 50 ohne Handbedienung **unbrauchbar** — der Freigabe-Dialog ist eine Wayland-Oberfläche, XTEST erreicht ihn nicht, und solange er steht kommt **gar kein** Ereignis an (**die Angabe in §5d war falsch**); die Freigabe wird **nicht gespeichert** und **eine Bildschirmsperre nimmt sie zurück**; **XTEST hält die Sitzung nicht wach**, sie sperrt nach 300 s mitten in der Messung. **Ausweg `ydotool`** (evdev/uinput, unterhalb des Portals) — mit zwei eigenen Fallen: **keine absoluten Achsen** (am Anschlag verankern, Faktor 0,8133) und **rohe Keycodes gegen die deutsche Belegung** (Strg+Z ist **21**, nicht 44). Befund: **§4.64** und **§4.65** |
| **OCR vermessen, und der Augenschein von Phase 4.5** — trägt `Tesseract 5.2.0` unter Linux, und hält die Bedienung aus sechs Runden dem ersten Blick unter X11 stand? | 2026-08-27 (V2-86) | ✅⚠ **Beides beantwortet; Bau 0/0, 953/953 grün.** **① Die Bindung trägt — aber nicht von selbst:** das Paket bringt für Linux **nichts** mit (kein `runtimes/linux-*`) und sucht die **Windows-Namen** `libtesseract50.so` und `libleptonica-1.82.0.so` im Unterordner **`x64/`**; die unversionierte `libtesseract.so` des Systems hilft **nicht**. **Ein zweiter Riegel:** `libdl.so` gibt es seit glibc 2.44 nicht mehr, nur `libdl.so.2`. **Mit drei gerichteten Namen erkennt es zeichengenau** (Version 5.5.3, Zuversicht **0,930**, gegen die Sprachdaten aus dem Repo). **`LD_LIBRARY_PATH` hilft nicht, das Wurzelverzeichnis auch nicht — nur `x64/`.** Ein `DllImportResolver` reicht **für `libdl`**, für die zwei anderen **nicht** (`dlopen` im Paket). **② Der Augenschein ist überwiegend grün:** Umlaute kommen an, **tote Tasten nicht** (dieselbe Wurzel wie §4.44), das Feld **sitzt richtig** (kein 1,6-Versatz), die **Zwischenablage trägt in allen drei Richtungen** und überlebt den Tod des Besitzerprozesses (GNOMEs Verwalter hält sie), **`HasImage` öffnet die Leiste in < 400 ms** mit einem 5000×4000-Bild, **Zahlenblock und Klappgruppen gehen per Langdruck auf**, und der **Farbfix aus V2-77 trägt** im dunklen Design. **⚠ Ein neuer Fund:** die Fläche hat nach dem Öffnen **keinen Tastaturfokus** (§5 „Noch offen" 19). **Drei Werkzeugfunde:** `xdotool` ist hier **nicht installiert** (`zeiger` ist das Werkzeug), die Freigabe „Entfernter Bildschirm" hängt an der **XTEST-Sitzung** und lässt sich **per Opferklick selbst bedienen**, und ein Langdruck ist eine `z:`-Bahn auf denselben Punkt. Befund: **§4.63** und **§4.62 „Was der Laptop gefunden hat"** |
| **Zwei Messungen am Tastenweg** — erreicht der Druck den X11-Client, und wer speist das Phantom mit `keycode = 0` ein? | 2026-08-19 (V2-62) | ✅ **Beide beantwortet, und die Leitspur aus §4.44 ist widerlegt; 789/789 grün, kein Produktivcode angefasst.** **Der Druck kommt an:** `xtrace` zeigt `KeyPress keycode 49` **3 mal** und `keycode 26` **5 mal**, je mit Loslassen. **Ein `keycode 0` steht nirgends auf der Leitung** — weder bei GonkNote noch bei `xev`, also **weder XWayland noch ein fremder `XSendEvent`-Client**. **Gemessen ist Zeile 2 der Tabelle: das Verschwinden sitzt im Prozess** — `XFilterEvent` **filtert sehr wohl** (in `xev` mit `True` belegt, auch das gegen §4.44). ✅ **Und das `keycode = 0` ist kein Phantom, sondern der Bote:** libX11 verschluckt die beteiligten Drucke und legt das fertige Zeichen nach — `XmbLookupString` liefert dort `(c3 aa) "ê"`. **Mit `XMODIFIERS=@im=none` gegengeprüft** (Avalonias eigene Einstellung): identisch, **4 mal `ecircumflex`** — der Zusammensetzer ist **Xlibs lokale Eingabemethode**. **Der Fehler des Kopfes:** er lässt filtern, holt aber nie ab (`LookupKey(keycode)` statt `XmbLookupString`). **Am dbus bestätigt:** für die tote Taste nur `LOSLASSEN` plus `DRUCK 0/0/0`, während `a` und ein einzelnes `e` sauber durchgehen. **Zwei Werkzeugfunde:** `xtrace` gibt es auf Arch **nicht** als Paket (`/usr/bin/xtrace` ist glibcs Tracer; der X11-Tracer heißt im AUR `x11trace`), und **`setxkbmap -query` lügt unter XWayland** (`us` statt `de` — `xkbcomp` fragen). Befund: **§4.44 „Was der Laptop gefunden hat"** |
| **Die toten Tasten gegenmessen** — kommt `ê` mit `SupportsPreedit => true` wieder an, sind die Umlaute heil, sieht der unfertige Text richtig aus, bleibt beim Festschreiben nichts stehen, tippt es sich flüssig? | 2026-08-18 (V2-59) | ⛔ **Nein — die Erwartung ist widerlegt, und der Grund ist gemessen; 789/789 grün.** `^`+`e` ergibt weiterhin **nichts** (Zähler **5** statt 7, zahlengleich mit V2-55). **Umlaute heil** (**14**), **flüssig** (ein Absatz von **427** Zeichen kommt **exakt** an). **Fragen 3 und 4 sind gegenstandslos** — IBus schickt an unseren Kontext **gar keine Vorschau**, also wird `VorschauMalen` nie gerufen; **das Bild hat immer noch niemand gesehen.** **⚠ Der Fund kommt vom `dbus-monitor`** (auf dem **eigenen Bus des IBus-Daemons**, nicht dem Sitzungsbus): **`CommitText` 0 mal bei uns, 2 mal bei `gnome-text-editor`** — derselbe Daemon, dieselbe Engine, dieselbe Sekunde, **es liegt also nicht an IBus**. ✅ **Das Fähigkeitswort kommt an** (`SetCapabilities uint32 9` = `CapPreeditText`+`CapFocus`) — **§4.42 Punkt 2 ist damit gemessen**. **⚠ Der Fehler sitzt davor:** für die tote Taste und den Buchstaben danach schickt der Kopf **keinen Tastendruck**, nur das Loslassen — und dazwischen **einen Aufruf mit `keysym = 0`/`keycode = 0`, den IBus mit `true` beantwortet**; genau danach verwirft Avalonia das rohe Ereignis (§4.42). **Nebenbei:** Keycodes um **8** daneben (X11 statt evdev). **Werkzeugfund:** XTEST läuft unter GNOME 50 über das **EI-Portal** — der erste Klick öffnet „Entfernter Bildschirm" und **wird geschluckt**. Befund: **§4.43 „Was der Laptop gefunden hat"** |
| **Die Bildschirmtastatur** — klappt sie bei Stift/Finger auf, kommt ihr Text zeichengenau an, bleibt sie beim *Mausklick* zu, sitzt sie an der Marke, und tippt die Hardware-Tastatur noch wie vorher? | 2026-08-18 (V2-55) | ⚠ **Drei von fünf beantwortet, eine Regression gefunden; 769/769 grün.** **Die Tastatur klappt nicht von selbst auf** — `TopLevel.InputPane` ist unter `Avalonia.X11` **`null`** (mit einer Wegwerf-Sonde gemessen), und im Augenschein klappt dieselbe Tastatur bei `gnome-text-editor` auf und verschwindet, sobald GonkNote den Fokus hat. **✅ Frage 2 hat der Nutzer von Hand beantwortet, und sie fällt positiv aus:** von Hand hervorgeholt **schreibt die Tastatur ins Dokument** — in V2-47 kam auch von Hand nichts an. **Die Naht wirkt also zur Hälfte: taub ist sie nicht mehr, unsichtbar bleibt sie.** Frage 4 bleibt offen, **Frage 3 ist wertlos** — die `PointerType`-Weiche ist unter Linux nicht prüfbar. **⚠ Der Fund ist Frage 5:** tote Tasten kommen seit V2-54 **nicht mehr an** (`^`+`e` → nichts statt `ê`), Umlaute schon; eingekreist mit drei Gegenproben auf **IBus + `SupportsPreedit => false`** (§5 „Noch offen" **11**). **Drei Werkzeug-Funde:** Vollbildaufnahmen unter Wayland gehen **doch** (Portal, neu als `tools/linux/wlschuss.sh` — die Angabe „unbrauchbar" war veraltet), ein Avalonia-Flyout fehlt auf jeder X11-Fensteraufnahme, und Wayland/XWayland rechnen im Faktor **1,6** auseinander. Befund: **§4.41 „Was der Laptop gefunden hat"** |
| **Das Schreiben unter Linux** — Umlaute und tote Tasten, Compose, der Cursor am Stift, die Bildschirmtastatur, und läuft es flüssig? | 2026-08-16 (V2-47) | ✅ **Vier von fünf sauber; 662/662 grün.** Umlaute **und** tote Tasten kommen an (`^`+`e` → `ê`), der **Stift setzt die Marke und zieht eine Auswahl**, getippt wird flüssig — **kein Zeichen verloren, auch nicht in einem Dokument mit 32 Seiten** (85.691 → 85.792 exakt). Der Umbruch ist hier **schneller als unter Windows**; die 40-ms-Grenze fällt erst bei ~32 Seiten. **Compose:** ungeprüft, weil auf diesem Gerät keine eingerichtet ist. **Ein Fund:** ohne Hardware-Tastatur ist nicht zu schreiben — dem Kopf fehlt ein `TextInputMethodClient` (§5 „Noch offen" **10**, gehört nach Windows). **Ein Werkzeugfehler**, der wie ein Fehler der App aussah: `zeiger` tippte Latin-1-Zeichen gar nicht — behoben. **Zwei alte Punkte mit zu:** Dateidialog (§5 Nr. 7) und **zweites Stiftgerät** (§5 Nr. 1, MPP, **Druck kommt an**). Befund: **§4.35 „Was der Laptop gefunden hat"** |
| **Der erste Augenschein des Textdokuments unter Linux** — steht Schrift auf dem Blatt, stimmt sie mit dem PDF überein, trägt das Rollen, trägt der Dateidialog? | 2026-08-11 (V2-37) | ✅ **Fragen 1, 3 und 5 sauber; 489/489 grün.** Anzeige vollständig (Formate, Aufzählung, Tabelle mit **einer** Kopfzeile, Diagramm, Kopf-/Fußzeile), Rollen und alle Zoomstufen tragen, der Portal-Dialog erscheint nicht hinter dem Fenster und **blockiert richtig**. **Ein Fund:** der Wortzwischenraum an einer Stückgrenze sitzt in der Anzeige falsch, im PDF nicht (§5 „Noch offen" 6). **Frage 4 blieb offen** — der Portal-Dialog ist nicht fernsteuerbar (§5 „Noch offen" 7). Befund: **§4.28 „Was der Laptop gefunden hat"** |
| **Den PDF-Export gegenprüfen** — läuft er unter Linux, und bettet Skia die mitgelieferten Schriften wirklich ein? | 2026-08-11 (V2-34) | ✅ **Ja.** 18/18 und 479/479 grün, `emb yes` und CID TrueType bei allen fünf Schnitten, kein Systemname, kein `Type3`. **Ein Fund:** Skia bettet die ganze TTF ein statt eines Auszugs — rund 200 KB je Familie, aber **einmalig und nicht je Seite**. Befund: **§4.27 „Was der Laptop gefunden hat"**, offene Frage: §5 „Noch offen" 3 |
| **Das Schriftkonzept gegenprüfen** — greift die Rückfallkette, und sieht Linux dieselben Schriften? | 2026-08-10/11 (V2-32) | ✅ Befund: **§4.26 „Was der Laptop gefunden hat"** |

---
