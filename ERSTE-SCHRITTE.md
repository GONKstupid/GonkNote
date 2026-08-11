# Erste Schritte mit Gonk Note

Diese Anleitung führt dich vom leeren Rechner bis zur ersten beschriebenen,
exportierten und gesicherten Notiz. Rechne mit **10 Minuten**.

Wenn du wissen willst, *was* Gonk Note alles kann, lies die
[Feature-Übersicht im README](README.md). Hier geht es darum, *wie* du anfängst.

*(Deutsche Fassung. Die englische ist `GETTING-STARTED.md`. Im Programm richtet sich diese
Anleitung nach der Sprache, die du unter Ansicht → Sprache gewählt hast.)*

---

## Inhalt

1. [Gonk Note auf den Rechner holen](#1-gonk-note-auf-den-rechner-holen)
2. [Der erste Start](#2-der-erste-start)
3. [Das Fenster in 30 Sekunden](#3-das-fenster-in-30-sekunden)
4. [Dein erstes Notizbuch](#4-dein-erstes-notizbuch)
5. [Schreiben und zeichnen](#5-schreiben-und-zeichnen)
6. [Etwas auswählen und ändern](#6-etwas-auswählen-und-ändern)
7. [Ein PDF oder Word-Dokument beschreiben](#7-ein-pdf-oder-word-dokument-beschreiben)
8. [Whiteboard und Textdokument](#8-whiteboard-und-textdokument)
9. [Exportieren](#9-exportieren)
10. [Sichern — bitte einmal einrichten](#10-sichern--bitte-einmal-einrichten)
11. [Eigene Sticker, Cover und Geodreieck](#11-eigene-sticker-cover-und-geodreieck)
12. [Sprache und Design](#12-sprache-und-design)
13. [Spickzettel](#13-spickzettel)
14. [Auf eine neue Version aktualisieren](#14-auf-eine-neue-version-aktualisieren)
15. [Wenn etwas klemmt](#15-wenn-etwas-klemmt)

---

## 1. Gonk Note auf den Rechner holen

**Voraussetzungen:** **Windows 11** (Windows 10 sollte ebenfalls laufen, ist aber
nicht getestet) **oder Linux**, dazu das
[.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) oder neuer.
Adminrechte brauchst du nicht.

> **Gonk Note gibt es in zwei Ausgaben** — eine für Windows und eine für Linux.
> Beide lesen dieselben Dateien und sehen fast gleich aus. Die **Linux-Ausgabe ist
> noch nicht vollständig**: Notizbuch und Whiteboard laufen, Textdokumente lassen
> sich **anzeigen und exportieren, aber noch nicht beschreiben**, und ein
> Whiteboard lässt sich dort nicht exportieren. Was genau fehlt, steht im
> [README](README.md#zwei-ausgaben-eine-app).

> Es gibt (noch) kein fertiges Release zum Herunterladen — du baust dir das
> Programm in zwei Befehlen selbst.

**Schritt für Schritt:**

1. Repository klonen:

   ```bash
   git clone https://github.com/GONKstupid/GonkNote.git
   cd GonkNote
   ```

2. Bauen — **je nach Ausgabe ein anderes Projekt**. Baue nie die ganze Solution:
   sie enthält beide, und die Windows-Ausgabe lässt sich unter Linux nicht
   übersetzen.

   **Windows:**

   ```powershell
   dotnet publish src/GonkNote.Wpf -c Release
   ```

   Das Ergebnis ist eine **einzelne Datei**, die ohne installiertes .NET läuft und
   sich beliebig verschieben lässt:

   ```
   src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\GonkNote.exe
   ```

   Kopiere sie dorthin, wo du sie haben willst — **zusammen mit dem Ordner
   `tessdata`** und, falls vorhanden, dem Ordner `Assets` aus demselben
   Verzeichnis. `tessdata` enthält die Sprachdaten für die Texterkennung; ohne
   ihn funktioniert alles außer OCR.

   **Linux:**

   ```bash
   dotnet run --project src/GonkNote.Avalonia
   ```

   Dein System braucht dafür **fontconfig und mindestens eine Schrift** — sonst
   bleibt jeder gezeichnete Text leer. Auf Arch-artigen Systemen (CachyOS, Manjaro,
   EndeavourOS …):

   ```bash
   sudo pacman -S fontconfig ttf-dejavu
   ```

   Auf Debian-artigen (Ubuntu, Mint …) heißen die Pakete `libfontconfig1` und
   `fonts-dejavu-core`.

Der erste Lauf lädt die Pakete herunter und dauert ein paar Minuten.

**Nur zum Ausprobieren** — ohne fertige Datei, direkt aus dem Quellcode:

```bash
dotnet run --project src/GonkNote.Wpf        # Windows
dotnet run --project src/GonkNote.Avalonia   # Linux
```

---

## 2. Der erste Start

Windows: Doppelklick auf `GonkNote.exe`. Linux: der `dotnet run`-Befehl von oben.

Beim ersten Start legt Gonk Note still einen Ordner an — **unter Windows
`%APPDATA%\GonkNote`, unter Linux `~/.config/GonkNote`**:

```
<Datenordner>/
├─ gonknote.sqlite      deine Texte, Striche und die Ordnerstruktur
├─ gonknote.blobs/      Bilder sowie importierte PDF- und Word-Seiten
└─ gonknote.papierkorb/ Bilder, die gerade niemand braucht (30 Tage Schonfrist)
```

Nichts wird ins Internet geschickt, nichts in die Registry geschrieben, nichts
installiert. Willst du Gonk Note wieder loswerden, reichen das Programm und dieser
Ordner.

> **Merke dir diesen Pfad.** Er ist gleichzeitig dein Backup — siehe
> [Abschnitt 10](#10-sichern--bitte-einmal-einrichten). Du musst ihn dir nicht
> merken: **Hilfe → Über Gonk Note** zeigt ihn an.

---

## 3. Das Fenster in 30 Sekunden

| Wo | Was |
|---|---|
| **Menüleiste oben** | `Datei`, `Ansicht`, `Hilfe` |
| **Seitenleiste links** | dein Ordnerbaum, oben vier Knöpfe für Neuanlagen, ganz oben der Schnellzugriff („ANGEPINNT") |
| **Mitte** | die **Galerie** — solange nichts geöffnet ist, siehst du hier den aktuellen Ordner als große Kacheln |
| **Registerkarten** | jedes geöffnete Dokument bekommt eine eigene Karte |

Ist die Seitenleiste im Weg: `Strg+B`.

Maximierst du das Fenster, verschwindet die Titelleiste. Sie gleitet wieder
herein, sobald du mit der Maus an den oberen Fensterrand fährst.

---

## 4. Dein erstes Notizbuch

1. **Anlegen** — `Datei → Neues Notizbuch`, oder der Knopf „Neu" über der
   Galerie, oder der Notizbuch-Knopf oben in der Seitenleiste.
2. **Benennen** — der Name lässt sich jederzeit mit `F2` ändern.
3. **Öffnen** — Doppelklick auf die Kachel in der Galerie oder auf den Eintrag
   im Baum. Das Notizbuch öffnet sich in einer eigenen Registerkarte.
4. **Blättern** — unten in der Mitte sitzt die Seitenleiste des Notizbuchs:
   `◀  Seite 1 / 1  ▶` und daneben **Neue Seite** (`+`) und **Seite löschen**.
5. **Aussehen ändern** — das **Zahnrad** rechts in der Werkzeugleiste öffnet die
   Einstellungen. Unter **Seite** wählst du Muster (Blanko, Liniert, Kariert,
   Punktiert), Farbton und Format (A4/A3, Hoch-/Querformat). Mit
   *„Als Standard für neue Seiten"* gilt die Wahl auch für alles, was du danach
   anlegst.
6. **Cover setzen** — in derselben Leiste die Sektion **Cover**: Farbverlauf,
   Schrift oder ein Bild. Mitgeliefert sind die Kategorien „Basic", „Muster" und
   „Pixel Art"; unter **Individuell** lädst du über die „+"-Kachel eigene Bilder
   hoch.

**Gespeichert wird von selbst** — alle 30 Sekunden, beim Schließen der
Registerkarte und beim Beenden. `Strg+S` geht trotzdem, wenn du dich wohler
fühlst.

---

## 5. Schreiben und zeichnen

Die Werkzeuge liegen oben in der Leiste; jedes hat ein Tastenkürzel:

| Taste | Werkzeug | Gut für |
|---|---|---|
| `S` | Stift | normales Schreiben, druckempfindlich |
| `B` | Bleistift | Skizzen mit Graphit-Körnung |
| `M` | Textmarker | Hervorheben |
| `G` | Formen-Stift | Kritzel dir Kreis, Rechteck oder Gerade — er erkennt sie |
| `E` | Radiergummi | radiert punktgenau, trennt Striche an der Berührstelle |
| `T` | Textfeld | getippter Text auf der Seite |
| `N` | Notizzettel | farbige Klebezettel |
| `F` | Formen | Linie, Pfeil, Rechteck, Ellipse, Dreieck |
| `R` / `D` | Lineal / Geodreieck | gerade Linien und Winkel |
| `H` | Hand | Ansicht verschieben |

**Drei Handgriffe, die den Unterschied machen:**

- **Strichstärke exakt setzen:** halte den Größen-Schieber (oder das Icon
  daneben) lang gedrückt — es öffnet sich ein Zahlenblock zur direkten Eingabe.
  Der Radiergummi merkt sich seine eigene Größe.
- **Zoom und Verschieben:** `Strg+Mausrad` zoomt; verschieben geht mit mittlerer
  Maustaste, gedrückter Leertaste oder dem Hand-Werkzeug. Am Touchscreen: ein
  Finger verschiebt, zwei Finger zoomen.
- **Verschrieben?** `Strg+Z`. Mit drei Fingern doppelt tippen tut dasselbe.

**Mit Stift:** Die Rückseite des Stifts radiert automatisch. Die zweite
Stift-Taste öffnet das Schnellmenü (siehe nächster Abschnitt).

---

## 6. Etwas auswählen und ändern

1. **Auswählen** — entweder `L` (Lasso) und das Objekt umkreisen, oder `V`
   (Verschieben) und das Objekt direkt anklicken. Das Lasso nimmt nur, was du
   ungefähr vollständig umschlossen hast.
2. **Ändern** — ziehen zum Verschieben, Eckgriff zum Skalieren, Dreh-Griff zum
   Drehen (rastet alle 15° ein). Das gilt für Striche, Formen, Text, Bilder und
   Notizzettel gleichermaßen.
3. **Schnellmenü** — nach einer Auswahl erscheint automatisch eine kleine
   Icon-Leiste: Ausschneiden, Kopieren, Duplizieren, Einfügen, **Text erkennen
   (OCR)**, Löschen, Alles auswählen.

   Du bekommst sie außerdem per **Rechtsklick**, über die **zweite Stift-Taste**
   oder indem du bei Lasso/Verschieben/Hand etwa eine halbe Sekunde gedrückt
   hältst — damit kommst du ganz ohne Tastatur aus.

**Text aus einem Bild holen:** Bild auswählen → Schnellmenü → *Text erkennen
(OCR)*. Die Erkennung läuft offline (Deutsch und Englisch). Das Ergebnis kannst
du kopieren oder direkt als Notizzettel einfügen.

---

## 7. Ein PDF oder Word-Dokument beschreiben

Der typische Fall: ein Skript oder Arbeitsblatt annotieren.

1. In der Werkzeugleiste auf **Datei einfügen** — oder die Datei einfach ins
   Fenster ziehen, oder `Strg+V`.
2. Bei PDF und Word erscheint ein **Seitenauswahl-Dialog** mit Vorschaubildern.
   Wähle aus, was du brauchst.
3. Bestätigen:
   - Im **Notizbuch** wird jede gewählte Seite eine eigene Seite zum
     Draufschreiben.
   - Im **Whiteboard** landen die Seiten als hochauflösende, skalierbare Bilder.
4. Schreib drauf los — die Seite verhält sich ab jetzt wie jede andere.

Große Dateien sind kein Problem: Gonk Note lädt ein PDF nie am Stück, sondern
rendert in voller Auflösung nur die Seiten, die du wirklich einfügst. Aus einem
600-Seiten-PDF fünf Seiten auszuwählen dauert Sekunden.

Ein ganzes **DOCX oder Markdown als neues Textdokument** öffnest du dagegen über
`Datei → Dokument importieren…`.

---

## 8. Whiteboard und Textdokument

**Whiteboard** (`Datei → Neues Whiteboard`) — dieselben Werkzeuge wie im
Notizbuch, aber statt Seiten eine unendliche Fläche mit Punktraster. Nimm es für
Mindmaps, Skizzen und alles, was nicht in A4 passt.

**Textdokument** (`Datei → Neues Textdokument`) — ein Rich-Text-Editor im
Ribbon-Layout (`Start`, `Einfügen`, `Layout`, `Verweise`).

> **Schreiben geht nur in der Windows-Ausgabe.** Die Linux-Ausgabe **zeigt** ein
> Textdokument vollständig an — als gesetztes Papier mit Tabellen, Bildern,
> Diagrammen und Kopfzeile, zum Blättern und Zoomen — und exportiert es. Tippen
> geht dort noch nicht; das Ribbon sagt „Nur Ansicht". Deine Texte bleiben dabei
> unangetastet.
>
> **Ein Dokument aus der Windows-Ausgabe** erscheint unter Linux erst, nachdem es
> dort einmal geöffnet und gespeichert wurde: sein altes Format liest nur Windows.
> Bis dahin steht in der Registerkarte, was zu tun ist — und der Inhalt ist
> unverändert gespeichert.

Zum Einstieg:

1. Text tippen, Formatvorlage oben links wählen (Überschrift 1–4, Zitat, …).
2. **Tabelle einfügen** über `Einfügen` — Raster aufziehen wie in Word. Steht der
   Cursor in einer Tabelle, erscheint der Kontext-Tab **Tabelle** mit allem
   Weiteren (Zellen verbinden, sortieren, Formeln wie `=SUMME(ABOVE)`).
3. **Seite einrichten** über `Layout` → *Erweiterte Einstellungen*: Format,
   Ausrichtung, Ränder in Zentimetern, Kopf-/Fußzeile, Wasserzeichen.
4. Die **Rechtschreibprüfung** schaltest du unten in der Statusleiste zwischen
   Deutsch und Englisch um.

---

## 9. Exportieren

1. `Datei → Exportieren…` — oder, bei Notizbuch und Whiteboard, die Sektion
   **Export** in der Einstellungs-Seitenleiste (Zahnrad).
2. Im Speichern-Dialog **bestimmt die gewählte Dateiendung das Format**:
   - Textdokument → `.pdf`, `.docx`, `.md`, `.png`
   - Notizbuch / Whiteboard → `.pdf`, `.png`
3. Speichern. Bei PNG bekommst du eine Datei je Seite.

Exportiert wird immer „auf Papier": auch im Dark Mode kommt ein helles Blatt mit
dunkler Schrift heraus. Fehlen zu einem Bild die Originaldaten, sagt Gonk Note
das nach dem Export — statt stillschweigend in schlechterer Qualität zu
exportieren.

> **In der Linux-Ausgabe: Textdokumente ja, Tafeln noch nicht.** Ein Textdokument
> geht dort in alle vier Formate — der Knopf **Exportieren** sitzt im Ribbon des
> Dokuments und wählt das Format gleich vor. Der Export eines Whiteboards oder
> Notizbuchs hängt noch am Windows-Baustein; die Linux-Ausgabe sagt das ehrlich,
> statt eine leere Datei zu schreiben.

---

## 10. Sichern — bitte einmal einrichten

Gonk Note hat keine Cloud. Deine Notizen liegen ausschließlich auf deinem
Rechner, und zwar an **zwei** Stellen im Datenordner (Windows:
`%APPDATA%\GonkNote`, Linux: `~/.config/GonkNote` — **Hilfe → Über Gonk Note**
zeigt ihn dir an):

```
<Datenordner>/gonknote.sqlite    ← Texte, Striche, Struktur
<Datenordner>/gonknote.blobs/    ← alle Bilder und importierten Seiten
```

**Für eine Sicherung brauchst du beides — die Datei *und* den Ordner.** Nur die
`.sqlite` zu kopieren sichert deine Notizen ohne die Bilder darin.

> **Kommst du von einer älteren Fassung?** Bis Version 0.2.0 hieß die Datei
> `gonknote.db`. Sie wird beim ersten Start danach einmalig übertragen und bleibt
> unverändert daneben liegen — als Rückweg, solange du sie behältst.
> **Sichere ab dann `gonknote.sqlite`:** die alte Datei wächst nicht mehr mit und ist
> nach kurzer Zeit ein veralteter Stand. Wer den ganzen Ordner sichert (nächster
> Absatz), hat ohnehin beides.

Am einfachsten: den kompletten Datenordner regelmäßig wegkopieren, am besten bei
geschlossener App. Zum Zurückholen kopierst du ihn an dieselbe Stelle zurück.

> **Zwischen Windows und Linux umziehen** geht damit auch: derselbe Ordnerinhalt,
> nur an der jeweils anderen Stelle. Die Dateien sind auf beiden Systemen
> identisch aufgebaut.

Bilder, auf die kein Dokument mehr zeigt, landen übrigens nicht sofort im Nichts,
sondern für 30 Tage in `gonknote.papierkorb\`. Wird so ein Bild vorher wieder
gebraucht, holt Gonk Note es von selbst zurück.

---

## 11. Eigene Sticker, Cover und Geodreieck

Cover und Geodreieck bringt Gonk Note mit; **Sticker liefert es aus Lizenzgründen
bewusst keine** — die legst du selbst ab. Eigene Dateien haben überall Vorrang vor
den mitgelieferten:

| Was | Wohin (im Datenordner) |
|---|---|
| Sticker (Bild-Aufkleber) | `Stickers/` — Unterordner werden zu eigenen Gruppen |
| Notizbuch-Cover | `Covers/` — erscheinen unter „Individuell" |
| Geodreieck-Grafik | `Geodreieck-Light.svg` bzw. `-Dark.svg` |

> **Dieser Abschnitt gilt heute nur für die Windows-Ausgabe.** Sticker, eigene
> Cover-Vorlagen und das Geodreieck gibt es in der Linux-Ausgabe noch nicht; die
> Dateien liegen dort ungenutzt herum, bis die Werkzeuge nachgezogen sind.

Sticker und Cover kannst du auch bequem über die **„+"-Kachel** im jeweiligen
Werkzeug hochladen; Gonk Note kopiert sie dann selbst an die richtige Stelle.

Beim Geodreieck legst du die Datei von Hand ab. Sie muss ein 16-cm-Geodreieck in
einer viewBox von 2520 × 1680 sein, mit der Hypotenusen-Mitte im Zentrum — sonst
passt der Aufdruck nicht zum Einrasten und Drehen. Fehlt sie, gilt die
mitgelieferte Grafik; fehlt auch die, zeichnet Gonk Note eine schlichte Kontur.

---

## 12. Sprache und Design

- **Sprache:** `Ansicht → Sprache → Deutsch / Englisch`. Wechselt sofort, ohne
  Neustart; deine Dokumentnamen bleiben unangetastet.
- **Dark/Light Mode:** `Strg+T` oder `Ansicht → Dark/Light Mode umschalten`. Die
  Schreibflächen bleiben dabei standardmäßig hell — den Farbton der Seite stellst
  du bei Bedarf in den Einstellungen um.

Beide Einstellungen werden gemerkt.

---

## 13. Spickzettel

**Überall**

| Kürzel | Wirkung |
|---|---|
| `Strg+S` / `Strg+Umschalt+S` | Speichern / alles speichern |
| `Strg+B` | Seitenleiste ein-/ausblenden |
| `Strg+T` | Dark/Light Mode |
| `F2` / `Entf` | Umbenennen / löschen (im Ordnerbaum) |
| `Strg+Z` / `Strg+Y` | Rückgängig / Wiederholen |

**Whiteboard und Notizbuch**

| Kürzel | Wirkung |
|---|---|
| `S` `B` `M` `G` `E` | Stift · Bleistift · Textmarker · Formen-Stift · Radierer |
| `T` `F` `N` | Textfeld · Formen · Notizzettel |
| `L` `V` `H` | Lasso · Verschieben · Hand |
| `R` `D` | Lineal · Geodreieck |
| `Strg+C/X/V` · `Strg+D` · `Strg+A` | Kopieren/Ausschneiden/Einfügen · Duplizieren · Alles auswählen |
| `Strg+Mausrad` | Zoom |
| Rechtsklick · zweite Stift-Taste · langes Drücken | Schnellmenü |

**Touch:** 1 Finger = verschieben · 2 Finger = zoomen · Drei-Finger-Doppeltipp =
rückgängig.

---

## 14. Auf eine neue Version aktualisieren

Gonk Note aktualisiert sich **nicht** von selbst — es gibt keinen Updater und keine
Internetverbindung. Du holst dir den neuen Stand und baust neu. Das dauert unter
einer Minute.

**Welche Version läuft gerade?** `Hilfe → Über Gonk Note` zeigt sie oben
(z. B. „Version 0.1.0 – Phase 3").

**Schritt für Schritt:**

1. **Gonk Note schließen.** Eine laufende Exe lässt sich nicht überschreiben; der
   Build bricht sonst mit „Zugriff verweigert" ab.
2. **Neuen Stand holen** — im Projektordner:

   ```bash
   git pull
   ```

3. **Neu bauen** — und hier kommt es darauf an, was du startest:

   | Du startest… | Befehl | Ergebnis liegt in |
   |---|---|---|
   | die Verknüpfung im Startmenü (Windows) | `dotnet build src/GonkNote.Wpf -c Release` | `src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\` |
   | eine kopierte Einzeldatei-Exe (Windows) | `dotnet publish src/GonkNote.Wpf -c Release` | `…\win-x64\publish\` |
   | die Linux-Ausgabe | `dotnet build src/GonkNote.Avalonia -c Release` | `src/GonkNote.Avalonia/bin/Release/net10.0/` |

   Beim zweiten Weg musst du die neue `GonkNote.exe` anschließend wieder dorthin
   kopieren, wo deine alte lag — **zusammen mit den Ordnern `Assets` und `tessdata`**,
   falls die sich geändert haben.

   **Nie `dotnet build` ohne Projektangabe.** Das baut die ganze Solution, und die
   enthält beide Ausgaben — unter Linux scheitert sie zwangsläufig an der
   Windows-Ausgabe.

4. **Starten und in `Hilfe → Über Gonk Note` nachsehen**, ob die neue Version anliegt.

**Deine Notizen bleiben dabei unangetastet.** Sie liegen im Datenordner und haben mit
dem Programmordner nichts zu tun — du kannst das Programm gefahrlos ersetzen oder sogar
löschen. Trotzdem gilt: **vor einem Update einmal sichern**
(siehe [Abschnitt 10](#10-sichern--bitte-einmal-einrichten)), das kostet zehn Sekunden.

**Wenn der Build fehlschlägt:**

| Meldung | Ursache |
|---|---|
| Zugriff verweigert / Datei in Verwendung | Gonk Note läuft noch — schließen und erneut bauen |
| `NETSDK1045` o. Ä. zur Framework-Version | .NET SDK zu alt — [aktuelles SDK 10+](https://dotnet.microsoft.com/download/dotnet/10.0) installieren |
| `net10.0-windows…` lässt sich nicht auflösen (Linux) | Du baust die Windows-Ausgabe oder die ganze Solution — nimm `src/GonkNote.Avalonia` |
| Merge-Konflikt bei `git pull` | du hast lokale Änderungen — `git stash`, dann erneut `git pull` |

**Nur für dich als Entwickler:** Die Versionsnummer selbst steht in
`Directory.Build.props` (`<Version>`) und gilt für beide Ausgaben; die Phasenangabe
daneben kommt aus dem Übersetzungsschlüssel `About.Version` (beide Sprachtabellen in
`src/GonkNote.Core/Localization/`). Beides wird von Hand gepflegt.

**Eine Version zurück?** Alle Stände liegen in der Git-Historie:
`git log --oneline` zeigt sie, `git checkout <commit>` holt einen davon, `git checkout main`
bringt dich zurück. Danach jeweils neu bauen.

---

## 15. Wenn etwas klemmt

**Die App zeigt einen Fehler.** Unerwartete Fehler landen als `fehler.log` im
Datenordner und werden einmal pro Sitzung gemeldet. Diese Datei ist das Erste, was
in einen Bug-Report gehört.

**Die Rechtschreibprüfung streicht nichts an.** Die Markierungen kommen von
Windows, nicht von Gonk Note. Fehlt für eine Sprache das Wörterbuch (typisch:
Englisch auf einem rein deutschen Windows), erscheint in der Statusleiste ein
Warndreieck. Abhilfe: die Sprache in den Windows-Einstellungen ergänzen. In der
Linux-Ausgabe gibt es die Prüfung noch nicht.

**OCR findet keinen Text / meldet fehlende Sprachdaten.** Der Ordner `tessdata`
muss neben `GonkNote.exe` liegen (siehe [Abschnitt 1](#1-gonk-note-auf-den-rechner-holen)).
Die Linux-Ausgabe hat noch keine Texterkennung und sagt das auch.

**Unter Linux bleibt jeder gezeichnete Text leer.** Dann fehlen `fontconfig` oder
eine Schrift — siehe [Abschnitt 1](#1-gonk-note-auf-den-rechner-holen).

**Ich will mit einer zweiten, leeren Datenbank testen.** Starte mit

```powershell
GonkNote.exe --db C:\Pfad\zu\test.sqlite                          # Windows
```
```bash
dotnet run --project src/GonkNote.Avalonia -- --db /tmp/test.sqlite   # Linux
```

Der echte Datenbestand bleibt dabei unberührt.

**Der Aufräumlauf für ungenutzte Bilder soll aus.** In der Datenbank die
Einstellung `blob-cleanup` auf `aus` setzen. Im Normalfall brauchst du das nicht
— aussortierte Bilder sind 30 Tage lang zurückholbar.

---

## Und dann?

- Die vollständige Feature-Liste steht im [README](README.md).
- Dieselben Texte findest du in der App unter `Hilfe → Über Gonk Note`.
- Fehler und Wünsche gehören in die
  [Issues](https://github.com/GONKstupid/GonkNote/issues).

Viel Spaß beim Schreiben.
