# Erste Schritte mit Gonk Note

Diese Anleitung zeigt, *wie du mit Gonk Note arbeitest* — vom ersten Notizbuch bis zum
Export. Rechne mit **10 Minuten**.

- **Wie du Gonk Note auf den Rechner bekommst**, steht in
  [Installieren](INSTALLIEREN.md).
- **Was Gonk Note alles kann**, steht in der
  [Feature-Übersicht im README](../README.md).

*(Deutsche Fassung. Die englische ist `GETTING-STARTED.md`. Im Programm richtet sich
diese Anleitung nach der Sprache, die du unter Ansicht → Sprache gewählt hast — sie
liegt auch unter Hilfe → Erste Schritte.)*

---

## Inhalt

1. [Der erste Start](#1-der-erste-start)
2. [Das Fenster in 30 Sekunden](#2-das-fenster-in-30-sekunden)
3. [Dein erstes Notizbuch](#3-dein-erstes-notizbuch)
4. [Schreiben und zeichnen](#4-schreiben-und-zeichnen)
5. [Etwas auswählen und ändern](#5-etwas-auswählen-und-ändern)
6. [Ein PDF oder Word-Dokument beschreiben](#6-ein-pdf-oder-word-dokument-beschreiben)
7. [Whiteboard und Textdokument](#7-whiteboard-und-textdokument)
8. [Exportieren](#8-exportieren)
9. [Sichern — bitte einmal einrichten](#9-sichern--bitte-einmal-einrichten)
10. [Eigene Sticker, Cover und Geodreieck](#10-eigene-sticker-cover-und-geodreieck)
11. [Sprache und Design](#11-sprache-und-design)
12. [Eigene Designs — auch von einer KI erstellen lassen](#12-eigene-designs--auch-von-einer-ki-erstellen-lassen)
13. [Spickzettel](#13-spickzettel)
14. [Wenn etwas klemmt](#14-wenn-etwas-klemmt)
15. [Gonk Note aktualisieren](#15-gonk-note-aktualisieren)

---

## 1. Der erste Start

Windows: Doppelklick auf `GonkNote.exe`. Linux: das AppImage bzw. `dotnet run`.

Beim ersten Start legt Gonk Note still einen Ordner an — **unter Windows
`%APPDATA%\GonkNote`, unter Linux `~/.config/GonkNote`**:

```
<Datenordner>/
├─ gonknote.sqlite      deine Texte, Striche und die Ordnerstruktur
├─ gonknote.blobs/      Bilder sowie importierte PDF- und Word-Seiten
└─ gonknote.papierkorb/ Bilder, die gerade niemand braucht (30 Tage Schonfrist)
```

> **Merke dir diesen Pfad.** Er ist gleichzeitig dein Backup — siehe
> [Abschnitt 9](#9-sichern--bitte-einmal-einrichten). Du musst ihn dir nicht merken:
> **Hilfe → Über Gonk Note** zeigt ihn an.

---

## 2. Das Fenster in 30 Sekunden

| Wo | Was |
|---|---|
| **Menüleiste oben** | `Datei`, `Ansicht`, `Hilfe` |
| **Seitenleiste links** | dein Ordnerbaum, oben vier Knöpfe für Neuanlagen, ganz oben der Schnellzugriff („ANGEPINNT") |
| **Mitte** | die **Galerie** — solange nichts geöffnet ist, siehst du hier den aktuellen Ordner als große Kacheln |
| **Registerkarten** | jedes geöffnete Dokument bekommt eine eigene Karte |

Ist die Seitenleiste im Weg: `Strg+B`.

Maximierst du das Fenster, verschwindet die Titelleiste. Sie gleitet wieder herein,
sobald du mit der Maus an den oberen Fensterrand fährst.

---

## 3. Dein erstes Notizbuch

1. **Anlegen** — `Datei → Neues Notizbuch`, oder der Knopf „Neu" über der Galerie, oder
   der Notizbuch-Knopf oben in der Seitenleiste.
2. **Benennen** — der Name lässt sich jederzeit mit `F2` ändern.
3. **Öffnen** — Doppelklick auf die Kachel in der Galerie oder auf den Eintrag im Baum.
   Das Notizbuch öffnet sich in einer eigenen Registerkarte.
4. **Blättern** — unten in der Mitte sitzt die Seitenleiste des Notizbuchs:
   `◀  Seite 1 / 1  ▶` und daneben **Neue Seite** (`+`) und **Seite löschen**.
5. **Aussehen ändern** — das **Zahnrad** rechts in der Werkzeugleiste öffnet die
   Einstellungen. Unter **Seite** wählst du Muster (Blanko, Liniert, Kariert,
   Punktiert), Farbton und Format (A4/A3, Hoch-/Querformat). Mit
   *„Als Standard für neue Seiten"* gilt die Wahl auch für alles, was du danach anlegst.
6. **Cover setzen** — in derselben Leiste die Sektion **Cover**: Farbverlauf, Schrift
   oder ein Bild. Mitgeliefert sind die Kategorien „Basic", „Muster" und „Pixel Art";
   unter **Individuell** lädst du über die „+"-Kachel eigene Bilder hoch.

**Gespeichert wird von selbst** — alle 30 Sekunden, beim Schließen der Registerkarte
und beim Beenden. `Strg+S` geht trotzdem, wenn du dich wohler fühlst.

---

## 4. Schreiben und zeichnen

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

- **Strichstärke exakt setzen:** halte den Größen-Schieber (oder das Icon daneben) lang
  gedrückt — es öffnet sich ein Zahlenblock zur direkten Eingabe. Der Radiergummi merkt
  sich seine eigene Größe.
- **Zoom und Verschieben:** `Strg+Mausrad` zoomt; verschieben geht mit mittlerer
  Maustaste, gedrückter Leertaste oder dem Hand-Werkzeug. Am Touchscreen: ein Finger
  verschiebt, zwei Finger zoomen.
- **Verschrieben?** `Strg+Z`. Mit drei Fingern doppelt tippen tut dasselbe.

**Mit Stift:** Die Rückseite des Stifts radiert automatisch. Die zweite Stift-Taste
öffnet das Schnellmenü (siehe nächster Abschnitt).

---

## 5. Etwas auswählen und ändern

1. **Auswählen** — entweder `L` (Lasso) und das Objekt umkreisen, oder `V` (Verschieben)
   und das Objekt direkt anklicken. Das Lasso nimmt nur, was du ungefähr vollständig
   umschlossen hast.
2. **Ändern** — ziehen zum Verschieben, Eckgriff zum Skalieren, Dreh-Griff zum Drehen
   (rastet alle 15° ein). Das gilt für Striche, Formen, Text, Bilder und Notizzettel
   gleichermaßen.
3. **Schnellmenü** — nach einer Auswahl erscheint automatisch eine kleine Icon-Leiste:
   Ausschneiden, Kopieren, Duplizieren, Einfügen, **Text erkennen (OCR)**, Löschen,
   Alles auswählen.

   Du bekommst sie außerdem per **Rechtsklick**, über die **zweite Stift-Taste** oder
   indem du bei Lasso/Verschieben/Hand etwa eine halbe Sekunde gedrückt hältst — damit
   kommst du ganz ohne Tastatur aus.

**Text aus einem Bild holen:** Bild auswählen → Schnellmenü → *Text erkennen (OCR)*.
Die Erkennung läuft offline (Deutsch und Englisch). Das Ergebnis kannst du kopieren oder
direkt als Notizzettel einfügen.

---

## 6. Ein PDF oder Word-Dokument beschreiben

Der typische Fall: ein Skript oder Arbeitsblatt annotieren.

1. In der Werkzeugleiste auf **Datei einfügen** — oder die Datei einfach ins Fenster
   ziehen, oder `Strg+V`.
2. Bei PDF und Word erscheint ein **Seitenauswahl-Dialog** mit Vorschaubildern. Wähle
   aus, was du brauchst.
3. Bestätigen:
   - Im **Notizbuch** wird jede gewählte Seite eine eigene Seite zum Draufschreiben.
   - Im **Whiteboard** landen die Seiten als hochauflösende, skalierbare Bilder.
4. Schreib drauf los — die Seite verhält sich ab jetzt wie jede andere.

Große Dateien sind kein Problem: Gonk Note lädt ein PDF nie am Stück, sondern rendert in
voller Auflösung nur die Seiten, die du wirklich einfügst. Aus einem 600-Seiten-PDF fünf
Seiten auszuwählen dauert Sekunden.

Ein ganzes **DOCX oder Markdown als neues Textdokument** öffnest du dagegen über
`Datei → Dokument importieren…`.

---

## 7. Whiteboard und Textdokument

**Whiteboard** (`Datei → Neues Whiteboard`) — dieselben Werkzeuge wie im Notizbuch, aber
statt Seiten eine unendliche Fläche mit Punktraster. Nimm es für Mindmaps, Skizzen und
alles, was nicht in A4 passt.

**Textdokument** (`Datei → Neues Textdokument`) — ein Rich-Text-Editor im Ribbon-Layout
(`Start`, `Einfügen`, `Layout`, `Verweise`).

Zum Einstieg:

1. Text tippen, Formatvorlage oben links wählen (Überschrift 1–4, Zitat, …).
2. **Tabelle einfügen** über `Einfügen` — Raster aufziehen wie in Word. Steht der Cursor
   in einer Tabelle, erscheint der Kontext-Tab **Tabelle** mit allem Weiteren (Zellen
   verbinden, sortieren, Formeln wie `=SUMME(ABOVE)`).
3. **Seite einrichten** über `Layout` → *Erweiterte Einstellungen*: Format, Ausrichtung,
   Ränder in Zentimetern, Kopf-/Fußzeile, Wasserzeichen.
4. **Rechtschreib- und Grammatikprüfung** schaltest du unten in der Statusleiste
   zwischen Deutsch und Englisch um; daneben stehen zwei Schalter, einer je Prüfung.
   Falsch geschriebene Wörter bekommen eine **rote** Wellenlinie, Grammatikbefunde eine
   **blaue**. Die rechte Maustaste auf einem angestrichenen Wort bietet Verbesserungen an.

> **Beide Ausgaben schreiben.** Die Linux-Ausgabe zeigt ein Textdokument als gesetztes
> Papier mit Tabellen, Bildern, Diagrammen und Kopfzeile, zum Blättern und Zoomen — und
> lässt dich darin tippen, formatieren, suchen und exportieren. Die Unterschiede
> zwischen den Ausgaben stehen im
> [README](../README.md#zwei-ausgaben-eine-app). **Die Rechtschreibprüfung, die lange
> darunter fehlte, gibt es seit 1.0.4 auch dort** — mit mitgelieferten Wörterbüchern,
> also ohne dass du etwas installieren musst.
>
> **Ein Dokument aus der Windows-Ausgabe** erscheint unter Linux erst, nachdem es dort
> einmal geöffnet und gespeichert wurde. Bis dahin steht in der Registerkarte, was zu
> tun ist — und der Inhalt ist unverändert gespeichert.

---

## 8. Exportieren

1. `Datei → Exportieren…` — oder, bei Notizbuch und Whiteboard, die Sektion **Export**
   in der Einstellungs-Seitenleiste (Zahnrad).
2. Im Speichern-Dialog **bestimmt die gewählte Dateiendung das Format**:
   - Textdokument → `.pdf`, `.docx`, `.md`, `.png`
   - Notizbuch / Whiteboard → `.pdf`, `.png`
3. Speichern. Bei PNG bekommst du eine Datei je Seite.

Exportiert wird immer „auf Papier": auch im Dark Mode kommt ein helles Blatt mit dunkler
Schrift heraus. Fehlen zu einem Bild die Originaldaten, sagt Gonk Note das nach dem
Export — statt stillschweigend in schlechterer Qualität zu exportieren.

---

## 9. Sichern — bitte einmal einrichten

Gonk Note hat keine Cloud. Deine Notizen liegen ausschließlich auf deinem Rechner, und
zwar an **zwei** Stellen im Datenordner (**Hilfe → Über Gonk Note** zeigt ihn dir an):

```
<Datenordner>/gonknote.sqlite    ← Texte, Striche, Struktur
<Datenordner>/gonknote.blobs/    ← alle Bilder und importierten Seiten
```

**Für eine Sicherung brauchst du beides — die Datei *und* den Ordner.** Nur die
`.sqlite` zu kopieren sichert deine Notizen ohne die Bilder darin.

Am einfachsten: den kompletten Datenordner regelmäßig wegkopieren, am besten bei
geschlossener App. Zum Zurückholen kopierst du ihn an dieselbe Stelle zurück.

> **Zwischen Windows und Linux umziehen** geht damit auch: derselbe Ordnerinhalt, nur an
> der jeweils anderen Stelle. Die Dateien sind auf beiden Systemen identisch aufgebaut.

> **Kommst du von einer älteren Fassung?** Bis Version 0.2.0 hieß die Datei
> `gonknote.db`. Sie wird beim ersten Start danach einmalig übertragen und bleibt
> unverändert daneben liegen. Sichere ab dann `gonknote.sqlite`; wer den ganzen Ordner
> sichert, hat ohnehin beides.

Bilder, auf die kein Dokument mehr zeigt, landen übrigens nicht sofort im Nichts,
sondern für 30 Tage in `gonknote.papierkorb/`. Wird so ein Bild vorher wieder gebraucht,
holt Gonk Note es von selbst zurück.

---

## 10. Eigene Sticker, Cover und Geodreieck

Cover und Geodreieck bringt Gonk Note mit; **Sticker liefert es aus Lizenzgründen
bewusst keine** — die legst du selbst ab. Eigene Dateien haben überall Vorrang vor den
mitgelieferten:

| Was | Wohin (im Datenordner) |
|---|---|
| Sticker (Bild-Aufkleber) | `Stickers/` — Unterordner werden zu eigenen Gruppen |
| Notizbuch-Cover | `Covers/` — erscheinen unter „Individuell" |
| Geodreieck-Grafik | `Geodreieck-Light.svg` bzw. `-Dark.svg` |
| Eigene Designs (Farbschemata) | `Themes/*.json` — erscheinen unter „Ansicht → Design" (Abschnitt 12) |

Sticker und Cover kannst du auch bequem über die **„+"-Kachel** im jeweiligen Werkzeug
hochladen; Gonk Note kopiert sie dann selbst an die richtige Stelle.

Beim Geodreieck legst du die Datei von Hand ab. Sie muss ein 16-cm-Geodreieck in einer
viewBox von 2520 × 1680 sein, mit der Hypotenusen-Mitte im Zentrum — sonst passt der
Aufdruck nicht zum Einrasten und Drehen. Fehlt sie, gilt die mitgelieferte Grafik; fehlt
auch die, zeichnet Gonk Note eine schlichte Kontur.

> **Alles in diesem Abschnitt gilt für beide Ausgaben.** Unter Linux liegen die Ordner
> unter `~/.config/GonkNote` statt unter `%APPDATA%`.

---

## 11. Sprache und Design

- **Sprache:** `Ansicht → Sprache → Deutsch / Englisch`. Wechselt sofort, ohne Neustart;
  deine Dokumentnamen bleiben unangetastet.
- **Hell und Dunkel:** `Strg+T` schaltet um; `Ansicht → Design` zeigt beides zur Auswahl.
  Die Schreibflächen bleiben dabei standardmäßig hell — den Farbton der Seite stellst du
  bei Bedarf in den Einstellungen um.

Beide Einstellungen werden gemerkt.

---

## 12. Eigene Designs — auch von einer KI erstellen lassen

Ein Design ist bei Gonk Note nichts als eine **Liste von bis zu zwanzig Farben** — eine
JSON-Datei, kein Programm. Es liegt unter `Themes/*.json` im Datenordner und erscheint
unter `Ansicht → Design` neben Hell und Dunkel. **Gilt für beide Ausgaben.**

### Von Hand

1. `Ansicht → Design → Vorlage speichern…` — Gonk Note schreibt das gerade aktive Design
   als vollständige Datei (alle zwanzig Farben) in den Ordner `Themes/` und sagt dir, wo
   sie liegt.
2. Öffne sie in einem Texteditor und ändere die Farbwerte (`#RRGGBB`, `#AARRGGBB` oder
   kurz `#RGB`). `name` ist, was später im Menü steht.
3. Die Datei erscheint unter `Ansicht → Design`, sobald du das Menü das nächste Mal
   aufklappst. Eine Datei von woanders holst du über `Eigenes laden…` — Gonk Note prüft
   sie und **kopiert** sie in den Ordner, das Original bleibt liegen.

**Du musst nicht alle zwanzig Farben angeben.** Was fehlt, kommt aus Hell bzw. Dunkel —
eine Datei mit drei Zeilen ist ein gültiges Design:

```json
{
  "name": "Abendrot",
  "variant": "dark",
  "colors": {
    "Accent": "#FF6B35",
    "WindowBg": "#241019"
  }
}
```

`variant` ist die einzige Pflichtangabe neben den Farben: Gonk Note muss wissen, ob dein
Design **hell oder dunkel** gemeint ist. Daran hängt mehr als die Farbe — die
Voreinstellungen der Zeichenfläche und unter Windows die Fenster-Titelleiste richten
sich danach, und geraten wird das nicht.

> **Zwei Dinge, die dazugehören.** Ein Design kann auch das **Papier** einfärben
> (`PageBg`, `PageLine`, `PageGridDot`, `CanvasBg`, `DefaultInk`) — und das wirkt sich
> dann auch auf den **Export** aus. Und eine Datei, die Gonk Note nicht lesen kann,
> verschwindet nicht: sie steht ausgegraut im Menü, und der Tooltip sagt, was ihr fehlt.

### Von einer KI erstellen lassen

Du musst keine Hex-Werte selbst wählen. Gib den folgenden Prompt einem KI-Assistenten
(ChatGPT, Claude, …), beschreibe darin am Ende, was du willst — und du bekommst eine
fertige `theme.json`, die du in den `Themes/`-Ordner legst.

````text
Du erstellst eine Theme-Datei für die Notiz-App „Gonk Note".

Gib AUSSCHLIESSLICH eine gültige JSON-Datei aus, ohne Kommentar davor oder danach.
Aufbau:

{
  "name":    "<Anzeigename im Menü>",
  "variant": "light" ODER "dark"   (PFLICHT),
  "colors":  { "<Farbname>": "<Hex>", ... }
}

Hex-Werte: "#RGB", "#RRGGBB" oder "#AARRGGBB" (mit Alpha). Gib alle 20 Farben an.

Die 20 Farbnamen und ihre Bedeutung:

Oberfläche (15):
  WindowBg     Fensterhintergrund und Arbeitsbereich
  SidebarBg    Seitenleiste und Menüleiste
  CardBg       Karten und Kacheln vor dem Fensterhintergrund
  ToolbarBg    Werkzeugleisten über einem Dokument
  Border       Trennlinien und Umrandungen
  Text         Vordergrundfarbe für Fließtext und Beschriftungen
  TextMuted    Zurückgenommener Text: Datum, Hinweise, Symbole ohne Betonung
  Accent       Akzentfarbe — Knöpfe, Auswahlrahmen, Hervorhebungen
  AccentSoft   weiche Fassung der Akzentfarbe für Flächen
  Turquoise    Türkis; auch Rückfall für Symbolfarben ohne eigene Farbe
  Pink         Pink
  Purple       Lila
  Hover        Fläche unter dem Zeiger
  Pressed      Fläche während des Klicks
  Selection    ausgewählter Eintrag in Baum und Listen

Das gezeichnete Blatt (5):
  CanvasBg     Fläche neben dem Blatt (Whiteboard-Leinwand)
  PageBg       das Papier selbst
  PageLine     Linien einer linierten Seite
  PageGridDot  Punkte einer karierten Seite
  DefaultInk   voreingestellte Schreibfarbe

Regeln:
- "variant": "light"  -> helle Flächen, dunkler Text.
  "variant": "dark"   -> dunkle Flächen, heller Text.
- Text muss auf WindowBg, SidebarBg und CardBg klar lesbar sein (hoher Kontrast).
- Accent muss auf diesen Flächen deutlich sichtbar sein; AccentSoft ist dieselbe
  Farbe, nur blasser/transparenter für Flächen.
- Hover ist eine Spur heller/dunkler als die Fläche darunter, Pressed etwas stärker,
  Selection klar erkennbar, aber nicht grell.
- PageBg ist Papier: normalerweise (fast) weiß lassen, außer der Wunsch verlangt
  ausdrücklich farbiges Papier. PageLine und PageGridDot sind dezente Varianten von
  PageBg. DefaultInk kontrastiert stark zu PageBg. CanvasBg ist die Fläche daneben,
  etwas dunkler/heller als PageBg.
- Turquoise, Pink, Purple sind Farbtupfer für Ordnersymbole — kräftig und
  voneinander unterscheidbar.

Der Wunsch: <hier beschreiben — z. B. „ein warmes dunkles Design in Braun- und
Bernsteintönen, Akzent orange" oder „ein helles Design im Stil von frischem Grün">
````

Prüfen musst du nichts von Hand: Legst du die Datei in den `Themes/`-Ordner und wählst
sie unter `Ansicht → Design`, sagt Gonk Note dir sofort, wenn etwas mit ihr nicht stimmt.

---

## 13. Spickzettel

**Überall**

| Kürzel | Wirkung |
|---|---|
| `Strg+S` / `Strg+Umschalt+S` | Speichern / alles speichern |
| `Strg+B` | Seitenleiste ein-/ausblenden |
| `Strg+T` | Hell/Dunkel umschalten |
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

## 14. Wenn etwas klemmt

**Die App zeigt einen Fehler.** Unerwartete Fehler landen als `fehler.log` im
Datenordner und werden einmal pro Sitzung gemeldet. Diese Datei ist das Erste, was in
einen Bug-Report gehört.

**Die Rechtschreibprüfung streicht nichts an.** *Windows-Ausgabe:* Die Markierungen
kommen von Windows, nicht von Gonk Note. Fehlt für eine Sprache das Wörterbuch (typisch:
Englisch auf einem rein deutschen Windows), erscheint in der Statusleiste ein
Warndreieck. Abhilfe: die Sprache in den Windows-Einstellungen ergänzen.
*Linux-Ausgabe:* Die Wörterbücher kommen mit dem Programm; ist der Schalter grau, fehlt
der Ordner `Dictionaries` neben dem Programm.

**Die Grammatikprüfung findet nur Kleinigkeiten.** Das stimmt, und es ist kein Fehler:
Ohne Zusatz prüft Gonk Note feste Regeln — dasselbe Wort zweimal, Leerzeichen vor dem
Komma, ein klein beginnender Satz. Satzbau und Fälle kann sie nicht. **Wer echte
Grammatikprüfung will, startet einen LanguageTool-Server auf dem eigenen Rechner**
(`languagetool --http`, Port 8081); Gonk Note findet ihn von selbst und übernimmt seine
Befunde. **Nur der eigene Rechner** — an einen fremden Server schickt Gonk Note
grundsätzlich nichts, auch nicht an den öffentlichen Dienst von languagetool.org.

**OCR findet keinen Text / meldet fehlende Sprachdaten.** Der Ordner `tessdata` muss
neben `GonkNote.exe` liegen — nicht im Datenordner. Der Bau legt ihn selbst dorthin; wer
das Programm verschiebt, muss ihn mitnehmen. Flatpak und AppImage bringen ihn ohnehin
mit.

**Ich will mit einer zweiten, leeren Datenbank testen.** Starte mit `--db`:

```powershell
GonkNote.exe --db C:\Pfad\zu\test.sqlite                             # Windows
```
```bash
dotnet run --project src/GonkNote.Avalonia -- --db /tmp/test.sqlite  # Linux
```

Der echte Datenbestand bleibt dabei unberührt.

**Der Aufräumlauf für ungenutzte Bilder soll aus.** In der Datenbank die Einstellung
`blob-cleanup` auf `aus` setzen. Im Normalfall brauchst du das nicht — aussortierte
Bilder sind 30 Tage lang zurückholbar.

Weitere Fälle rund um Installation und Bau stehen in
[Installieren](INSTALLIEREN.md#wenn-der-bau-fehlschlägt).

---

## 15. Gonk Note aktualisieren

Gonk Note aktualisiert sich **nicht** von selbst — kein Updater, keine
Internetverbindung. **Welche Version läuft**, zeigt `Hilfe → Über Gonk Note`.

- **Fertiger Download:** die neue Datei von
  [Releases](https://github.com/GONKstupid/GonkNote/releases) holen und die alte
  ersetzen. Beim Windows-Paket die drei Ordner (`Fonts`, `tessdata`, `Assets`)
  mitnehmen.
- **Aus dem Quellcode gebaut:** im Projektordner `git pull`, dann neu bauen — genau so
  wie beim ersten Mal, siehe [README](../README.md#installieren) bzw.
  [Installieren](INSTALLIEREN.md#selbst-aus-dem-quellcode-bauen).

**Dein Datenordner wird dabei nie angefasst** — er liegt woanders (Abschnitt 1).
Trotzdem: vor einem Update einmal sichern (Abschnitt 9), das kostet zehn Sekunden.

---

## Und dann?

- Die vollständige Feature-Liste steht im [README](../README.md).
- Dieselben Texte findest du in der App unter `Hilfe`.
- Fehler und Wünsche gehören in die
  [Issues](https://github.com/GONKstupid/GonkNote/issues).

Viel Spaß beim Schreiben.
