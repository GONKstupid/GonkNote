# Design-Konzept: Text-Editor (Light & Dark Mode)
### UI/UX-Analyse von ONLYOFFICE Docs (Theme „Modern Hell" / „Modern Dunkel") mit Übertragung auf das eigene Farbsystem

> Grundlage: 4 Screenshots des ONLYOFFICE Text-Editors (aktuelle Version, Startseite- und Layout-Tab, je Hell/Dunkel) sowie `Light.xaml` und `Dark.xaml` als eigene Farbvorgabe.

> **▶ Geltungsbereich (nachgetragen am 2026-09-03, Phase 5, Schritt ②): Dieses Konzept gilt für *beide* Köpfe.**
> Bis dahin war es nur im WPF-Kopf umgesetzt (`Views/TextEditorView.xaml`), und der Linux-Kopf hatte an
> mehreren Zonen etwas anderes — eine Klappliste statt der Formatvorlagen-Galerie (7.4), keine linke
> Icon-Leiste (7.5), die Statusleiste auf der falschen Grundfläche (7.8) und Aufklappflächen in Fluents
> Vorgabegrau statt `Brush.CardBg` (5, 8). **Das ist in Schritt ② angeglichen worden.** Wer eine Zone
> hier ändert, ändert sie ab jetzt in beiden Köpfen — genau wie bei Farben, Schriften und Symbolen, die
> längst gemeinsam in Core stehen.

> **Priorisierungsregel (verbindlich für dieses Dokument):** `Light.xaml` und `Dark.xaml` sind das führende Konzept und wurden **nicht verändert**. Wo ein ONLYOFFICE-Muster mit den eigenen Tokens kollidiert, wird das ONLYOFFICE-Muster angepasst — nicht das eigene Farbsystem. Nach Prüfung aller Zonen (siehe Abschnitt 6 und 10) gibt es **keinen Fall, in dem das nicht möglich wäre**: Das eigene Tokenset deckt alle beobachteten ONLYOFFICE-Zonen vollständig ab. Sollte in einer künftigen Weiterentwicklung dennoch ein Fall auftreten, der sich nicht ohne Änderung an den XAML-Dateien lösen lässt, wird das an dieser Stelle ausdrücklich ergänzt.

---

## 1. Vorgehen

1. Die Screenshots wurden auf **Struktur** (welche Zonen gibt es, wie sind sie angeordnet) und **Farbverhalten** (was ändert sich zwischen Hell/Dunkel, was bleibt gleich) untersucht.
2. Die eigenen XAML-Tokens wurden als verbindliches Farbsystem übernommen — es werden **keine neuen Farben erfunden**, sondern die vorhandenen Brushes den ONLYOFFICE-Zonen zugeordnet.
3. Daraus entsteht ein Zonen-für-Zonen-Konzept, das strukturell an ONLYOFFICE angelehnt ist, aber vollständig im eigenen Look erscheint.

---

## 2. Struktur-Analyse: Die Zonen des ONLYOFFICE Text-Editors

Der Editor gliedert sich in acht wiederkehrende Zonen:

| # | Zone | Beschreibung |
|---|------|--------------|
| 1 | **Titelleiste** | App-Logo, Dokumenttab, Fenstersteuerung (−/□/×) |
| 2 | **Tab-Leiste (Ribbon-Header)** | Datei, Startseite, Einfügen, Zeichnen, Layout, Verweise, Zusammenarbeit, Schutz, Ansicht, Plugins, AI |
| 3 | **Werkzeugleiste (Ribbon-Body)** | Kontextabhängige Icon-Gruppen (Zwischenablage, Schrift, Absatz, Formatvorlagen, Ausrichtung) mit Zahlenfeldern (z. B. Einzug in cm) |
| 4 | **Formatvorlagen-Galerie** | Vorschau-Karten „Normal", „Kein Abstand", „Überschrift 1–4" |
| 5 | **Linke Icon-Leiste** | Suche, Kommentare, Überschriften-Navigator, Plugins, Feedback — nur Icons, keine Labels |
| 6 | **Lineal** | Horizontal, mit Einzugsmarken |
| 7 | **Arbeitsbereich (Canvas)** | Gedeckte Umgebungsfläche + darauf liegende weiße „Seite" |
| 8 | **Statusleiste** | Seitenzahl, Wörter zählen, Sprache, Rechtschreibprüfung, Ansichtsmodi, Zoom |

**Kernbeobachtung:** ONLYOFFICE trennt strikt zwischen **Chrome** (Zonen 1–3, 5, 6, 8 — das eigentliche "App-Gerüst") und **Content** (Zone 4 und 7 — das, was am Ende gedruckt/exportiert wird). Nur das Chrome wechselt mit dem Theme; Formatvorlagen-Vorschauen und die Dokumentseite bleiben **immer hell**, weil sie 1:1 zeigen, wie das Dokument aussehen wird (WYSIWYG-Prinzip schlägt Theme-Konsistenz).

---

## 3. Farbverhalten Hell vs. Dunkel (visuelle Einschätzung anhand der Screenshots)

*Werte sind optisch geschätzt, nicht exakt aus dem Original ausgelesen — sie dienen nur zur Einordnung des Kontrastverhaltens.*

| Zone | Modern Hell | Modern Dunkel | Ändert sich mit Theme? |
|---|---|---|---|
| Titelleiste / Ribbon-Hintergrund | sehr helles Grau/Weiß | fast schwarzes Anthrazit | ✅ ja |
| Tab-/Icon-Text | dunkles Grau/Schwarz | helles Grau/Weiß | ✅ ja |
| Aktiver Tab „Startseite" | Blau, fett, Unterstrich | Blau, fett, Unterstrich | ❌ Akzentblau bleibt identisch |
| Formatvorlagen-Karten | Weiß mit dünnem Rahmen | **bleiben weiß** | ❌ bewusst konstant |
| Überschrift-Vorschautext | Blau | Blau | ❌ konstant |
| Linke Icon-Leiste | Weiß, dunkle Icons | Dunkel, helle Icons | ✅ ja |
| Canvas-Umgebung (hinter der Seite) | helles Grau | dunkles Grau | ✅ ja |
| Dokumentseite selbst | Weiß | **bleibt weiß** | ❌ bewusst konstant |
| Lineal | Weiß/hell, dunkle Markierungen | dunkel, helle Markierungen | ✅ ja |
| Eingabefelder (z. B. „0 cm") | helles Feld, dunkler Text | **bleiben hell** | ❌ Formularfelder wie Content behandelt |
| Statusleiste | Hellgrau | Dunkelgrau | ✅ ja |
| Deaktivierte Icons (z. B. „Umbruch" ohne Auswahl) | abgeblendet/blasser | abgeblendet/blasser | ✅ (gleiche Logik, andere Basisfarbe) |

**Wichtigstes Muster:** Es gibt genau **eine** Akzentfarbe (Blau), die themeübergreifend konstant bleibt und ausschließlich für *aktive/interaktive Zustände* verwendet wird (aktiver Tab, ausgewählte Formatvorlage, Fokus-Rahmen). Alles andere ist neutrales Grau in zwei Helligkeitsstufen (Chrome-Hintergrund vs. Content-Hintergrund).

---

## 4. Übernommene UX-Prinzipien

Diese Muster werden 1:1 für das eigene Konzept übernommen, unabhängig von der Farbe:

1. **Chrome vs. Content trennen** — App-Gerüst wechselt mit dem Theme, das Dokument selbst bleibt lesbar/druckgetreu.
2. **Ein Akzent für Zustand, nicht für Dekoration** — Farbe zeigt „das ist aktiv/ausgewählt", nicht „das ist hübsch".
3. **Aktivierung über Linie/Rahmen statt Fläche** — 2 px Unterstrich beim Tab, dünner Akzentrahmen bei der ausgewählten Formatvorlage. Chrome bleibt dadurch ruhig, auch wenn viel benutzt wird.
4. **Icon-only Seitenleisten** ohne Text-Labels, um Platz für den Arbeitsbereich zu sparen.
5. **Kontextuelle Ribbon-Inhalte** — Zahlenfelder (Einzug, Abstand) erscheinen direkt im Ribbon der Layout-Registerkarte, kein Modal/Popup nötig.
6. **Deaktivierte Elemente** werden durch reduzierte Deckkraft/Sättigung markiert, nicht ausgeblendet — Struktur bleibt erkennbar.
7. **Dünne 1 px-Trennlinien** statt Schatten zur Gliederung von Werkzeuggruppen.
8. **Statusleiste als dichte, niedrig-kontrastierte Infoebene** — viele kleine Informationen, aber optisch zurückhaltend.

---

## 5. Eigenes Farbsystem (aus den XAML-Dateien)

| Token | Light | Dark | Rolle |
|---|---|---|---|
| `Brush.WindowBg` | `#F4F7FB` | `#0F1420` | äußerster Fensterhintergrund |
| `Brush.SidebarBg` | `#EAF0F8` | `#131A2A` | Seitenleisten / Titelleiste |
| `Brush.CardBg` | `#FFFFFF` | `#1A2233` | Karten, Panels, Popovers |
| `Brush.ToolbarBg` | `#FFFFFF` | `#161E30` | Ribbon / Werkzeugleiste |
| `Brush.Border` | `#D4DEEA` | `#2A3550` | Trennlinien, Rahmen |
| `Brush.Text` | `#1B2B4B` | `#E6ECF7` | Haupttext |
| `Brush.TextMuted` | `#6B7A99` | `#8CA0C4` | Sekundärtext, Icons inaktiv |
| `Brush.Accent` | `#2563EB` | `#3B82F6` | primärer Akzent (Blau) |
| `Brush.AccentSoft` | `#DBEAFE` | `#1E3A8A` | Akzent-Hintergrund (z. B. aktive Chip-Fläche) |
| `Brush.Turquoise` | `#14B8A6` | `#2DD4BF` | Zweit-Akzent |
| `Brush.Pink` | `#EC4899` | `#F472B6` | Zweit-Akzent |
| `Brush.Purple` | `#8B5CF6` | `#A78BFA` | Zweit-Akzent |
| `Brush.Hover` | `#DEE8F4` | `#223052` | Hover-Zustand |
| `Brush.Pressed` | `#CFDDF0` | `#2A3B63` | Pressed-Zustand |
| `Brush.Selection` | `#C7DBFF` | `#2C3E66` | Textmarkierung im Dokument |
| `Color.CanvasBg` | `#E8EDF5` | `#10151F` | Fläche hinter der Dokumentseite |
| `Color.PageBg` | `#FFFFFF` | `#1E2638` | Dokumentseite selbst |
| `Color.PageLine` | `#BBD2F0` | `#35486E` | Hilfslinien auf der Seite (z. B. Tabellenraster) |
| `Color.PageGridDot` | `#B8C6DC` | `#3A4A6B` | Rasterpunkte |
| `Color.DefaultInk` | `#1B2B4B` | `#E6ECF7` | Standard-Schreibfarbe im Dokument |

---

## 6. Mapping: ONLYOFFICE-Zone → eigenes Token

| ONLYOFFICE-Zone | Light-Token | Dark-Token |
|---|---|---|
| Titelleiste | `Brush.SidebarBg` | `Brush.SidebarBg` |
| Tab-Leiste + Ribbon-Body | `Brush.ToolbarBg` | `Brush.ToolbarBg` |
| Trennlinien in der Ribbon | `Brush.Border` | `Brush.Border` |
| Aktiver Tab (Unterstrich + Text) | `Brush.Accent` | `Brush.Accent` |
| Formatvorlagen-Karten | `Color.PageBg` | `Color.PageBg` |
| Ausgewählte Formatvorlage (Rahmen) | `Brush.Accent` | `Brush.Accent` |
| Linke/rechte Icon-Leiste | `Brush.SidebarBg` | `Brush.SidebarBg` |
| Icons (Standard) | `Brush.TextMuted` | `Brush.TextMuted` |
| Icons (aktiv/gehovert) | `Brush.Text` auf `Brush.Hover` | `Brush.Text` auf `Brush.Hover` |
| Lineal-Hintergrund | `Brush.ToolbarBg` | `Brush.ToolbarBg` |
| Lineal-Markierungen | `Brush.TextMuted` | `Brush.TextMuted` |
| Canvas-Umgebung | `Color.CanvasBg` | `Color.CanvasBg` |
| Dokumentseite | `Color.PageBg` | `Color.PageBg` |
| Fließtext im Dokument | `Color.DefaultInk` | `Color.DefaultInk` |
| Textmarkierung (Selektion) | `Brush.Selection` | `Brush.Selection` |
| Eingabefelder (z. B. „cm"-Felder) | `Brush.CardBg` + `Brush.Border` | `Brush.CardBg` + `Brush.Border` |
| Statusleiste | `Brush.SidebarBg` | `Brush.SidebarBg` |
| Deaktivierte Icons | `Brush.TextMuted` bei 40 % Deckkraft | `Brush.TextMuted` bei 40 % Deckkraft |

**Hinweis zur Dokumentseite im Dark Mode (entschieden, keine offene Frage):** ONLYOFFICE hält die Seite immer reinweiß. Das eigene `Dark-Mode.xaml` legt für genau diesen Fall aber bereits `Color.PageBg = #1E2638` fest — die Antwort ist also durch das eigene Konzept selbst vorgegeben, nicht optional. Entsprechend der Priorisierungsregel wird dieser Punkt bei ONLYOFFICE angepasst: Die Seite folgt `Color.PageBg`, nicht `#FFFFFF`. Aus demselben Grund folgen auch die Formatvorlagen-Karten (siehe oben) und jede andere Fläche, die im Original „die echte Seite" abbildet, konsequent `Color.PageBg` statt eines fixen Weiß.

---

## 7. Design-Konzept je Zone

### 7.1 Titelleiste
- Höhe ca. 40 px, Hintergrund `Brush.SidebarBg`, Bodenlinie `Brush.Border` (1 px).
- Logo + Dokumenttitel linksbündig, Text in `Brush.Text`.
- Fenstersteuerung rechts, Icons in `Brush.TextMuted`, Hover-Fläche `Brush.Hover`.

### 7.2 Tab-Leiste
- Hintergrund `Brush.ToolbarBg`.
- Inaktive Tabs: `Brush.TextMuted`, keine Fläche.
- Aktiver Tab: `Brush.Text`, fett, plus 2 px Unterstrich in `Brush.Accent`.
- Hover auf inaktivem Tab: Text wird `Brush.Text`, keine Fläche (leichter Übergang, wie im Original).

### 7.3 Werkzeugleiste (Ribbon-Body)
- Hintergrund `Brush.ToolbarBg`, Gruppentrenner `Brush.Border` (1 px vertikale Linie).
- Icon-Buttons: 32×32 px, Icon in `Brush.TextMuted`; bei Hover Fläche `Brush.Hover` + Icon `Brush.Text`; bei Pressed/aktiv Fläche `Brush.Pressed`.
- Dropdown-Felder (Schriftart, Größe): Rahmen `Brush.Border`, Hintergrund `Brush.CardBg`, Text `Brush.Text`; Fokus-Rahmen `Brush.Accent`.
- Zahlenfelder (Einzug, Abstand): identisch behandelt wie Dropdowns — bewusst „hell/Content-artig" via `Brush.CardBg`, damit sie sich wie Formulareingaben und nicht wie Chrome anfühlen.
- Toggle-Buttons (Fett/Kursiv aktiv): Fläche `Brush.AccentSoft`, Icon `Brush.Accent`.

### 7.4 Formatvorlagen-Galerie
- Karten: **die Fläche der echten Seite**, Rahmen `Brush.Border`, 4 px Radius. Grund: Diese Karten zeigen eine Vorschau der echten Seite — nach ONLYOFFICEs eigenem WYSIWYG-Prinzip müssen sie exakt deren Hintergrund abbilden.
  - ⚠ **Korrigiert am 2026-09-03:** Hier stand „`Color.PageBg`, im Dark Mode also `#1E2638`". Das ist im gebauten Programm **nicht so** — die Seite eines *Textdokuments* ist in beiden Erscheinungsbildern weiß (`TdRenderer.Papier`, Nutzer-Entscheidung „Papier ist Papier"), und `Color.PageBg` beschreibt das Blatt der *Zeichenfläche*. Die Karten folgen deshalb dem Papier: **weiß mit `#1B2B4B` als Tinte, in beiden Themes.** Siehe Abschnitt 10.
- **Drei Karten liegen in der Leiste** („Standard", „Überschrift 1", „Überschrift 2"), **alle zehn im Aufklappfeld** hinter dem Pfeil daneben. Der Grund ist Platz: Bei offener Ordner-Seitenleiste bleiben nur die drei sichtbar, die beim Schreiben ständig gebraucht werden.
- Ausgewählte Karte: 2 px Rahmen `Brush.Accent`. **Keine gefüllte Fläche** — sie verdeckte die Vorschau, die der Zweck der Karte ist (Punkt 4.3).
- Vorschautext je Ebene farblich differenziert, um Hierarchie zu zeigen:
  - Überschrift 1 → `Brush.Accent`
  - Überschrift 2 → `Brush.Turquoise`
  - Überschrift 3 → `Brush.Purple`
  - Überschrift 4 → `Brush.Pink` (kursiv, kleinste Stufe)
  - *(Damit bekommen die vier Akzentfarben aus der XAML-Datei einen konkreten, wiederkehrenden Verwendungszweck statt „irgendwo dekorativ" zu sein.)*

### 7.5 Linke Icon-Leiste
- Hintergrund `Brush.SidebarBg`, Icons `Brush.TextMuted`, Breite ca. 48 px.
- Aktiver Bereich (z. B. Kommentare geöffnet): linker 3 px Balken in `Brush.Accent` + Icon-Fläche `Brush.Hover`.

### 7.6 Lineal
- Hintergrund `Brush.ToolbarBg`, Skala/Zahlen `Brush.TextMuted`, Rand `Brush.Border`.
- Einzugsmarken (Dreiecke): `Brush.Accent`, damit sie beim Ziehen gut erkennbar sind.
- ⚠ **Nur im WPF-Kopf, und dort bewusst als Zierleiste** (nachgemessen 2026-09-02): Das gebaute Lineal hat **keinen einzigen Maus-Handler**, die Randmarken lassen sich nicht ziehen. Der Linux-Kopf hat deshalb **gar keins** — was es leistet, leisten die vier Randfelder im Layout-Reiter in Zahlen und änderbar. **Diese Zone ist die eine, die absichtlich nicht in beiden Köpfen steht.**

### 7.7 Arbeitsbereich
- Umgebungsfläche `Color.CanvasBg`.
- Seite `Color.PageBg`, mit dezentem Schlagschatten (kein Brush nötig, ~15 % Schwarz/Weiß je nach Theme).
- Hilfslinien/Tabellenraster: `Color.PageLine`, Rasterpunkte `Color.PageGridDot`.
- Textcursor & Standardtext: `Color.DefaultInk`.
- Markierter Text: Hintergrund `Brush.Selection`.

### 7.8 Statusleiste
- Hintergrund `Brush.SidebarBg`, Text `Brush.TextMuted`, Trennlinie oben `Brush.Border`.
- Aktiver Ansichtsmodus-Button: Icon `Brush.Accent`.
- ⚠ **`SidebarBg`, nicht `ToolbarBg`** — im hellen Erscheinungsbild sind beide weiß, und der Unterschied fällt erst im dunklen auf. Der Linux-Kopf stand bis 2026-09-03 auf `ToolbarBg` und hatte dort einen hellen Streifen unter einer dunkleren Seitenleiste.

### 7.9 Aufklappflächen (Flyouts, Popups, Menüs)

*Diese Zone gab es in den ONLYOFFICE-Screenshots nicht zu sehen — sie ist beim Übertragen auf den Linux-Kopf dazugekommen, weil dort **jede** Auswahl hinter einem Aufklapp-Pfeil sitzt (Marken, Sonderzeichen, Zellfarben, Tabellengröße, Formatvorlagen).*

- Fläche `Brush.CardBg`, Rahmen `Brush.Border` (1 px), 8 px Radius, 10 px Innenabstand — wie eine Karte, weil es eine ist (Abschnitt 5: `CardBg` = „Karten, Panels, **Popovers**").
- Kacheln darin sind **flach**: keine eigene Fläche, `Brush.Hover` unter dem Zeiger, `Brush.Pressed` beim Klick (Abschnitt 8). Ein Raster aus 57 grauen Kästen zeigt seine Zeichen schlechter als eines ohne.
- **⛔ Und das ist die Stelle, an der ein Oberflächen-Werkzeugkasten still gewinnt:** Was hier nicht ausdrücklich gesetzt wird, bekommt die Vorgabefläche des Frameworks — bei Avalonias Fluent ein Grau, das in **keiner** der beiden Farbtabellen steht. Dieselbe Regel wie beim Akzentviolett, nur eine Ebene höher: *jede Fläche, die noch nicht angefasst ist, trägt die Vorgabe.*

---

## 8. Interaktionszustände (zonenübergreifend gültig)

| Zustand | Regel |
|---|---|
| Default | Basisfarbe der Zone (`ToolbarBg`/`SidebarBg`/`CardBg`) |
| Hover | Fläche wechselt zu `Brush.Hover` |
| Pressed / aktiv gehalten | Fläche wechselt zu `Brush.Pressed` |
| Ausgewählt/Toggled an | Fläche `Brush.AccentSoft`, Icon/Text `Brush.Accent` |
| Fokus (Tastatur) | 2 px Außenrahmen `Brush.Accent`, keine Flächenänderung |
| Disabled | Icon/Text `Brush.TextMuted` bei ca. 40 % Deckkraft, kein Hover |
| Textmarkierung im Dokument | `Brush.Selection` |

---

## 9. Einsatz der vier Akzentfarben (Blau/Türkis/Pink/Lila)

ONLYOFFICE nutzt nur **einen** Akzent. Das eigene System hat **vier**, was ein Alleinstellungsmerkmal werden kann, wenn ihr Einsatz klar geregelt ist:

- **`Brush.Accent` (Blau):** einzige Farbe für *System-Interaktion* — aktiver Tab, Fokus, ausgewählte Formatvorlage, primäre Buttons.
- **`Brush.Turquoise` / `Brush.Pink` / `Brush.Purple`:** reserviert für *Inhalts-/Kollaborationskontext*, z. B.:
  - Formatvorlagen-Hierarchie (siehe 7.4)
  - Farbcodierung von Mitbearbeiter-Cursorn/-Avataren bei Zusammenarbeit (jede Person erhält eine der drei Farben, Blau bleibt für „ich selbst" reserviert)
  - Kommentar-Marker/Tags im Dokumentrand
  - Diagramm-/Tabellen-Akzente

Diese Trennung verhindert, dass die App durch vier gleichwertige Akzente „bunt/unruhig" wirkt — Blau bleibt eindeutig die Systemfarbe, die anderen drei sind Inhalts-Farben.

---

## 10. Notwendige Anpassungen von ONLYOFFICE an das eigene, vorrangige Konzept

Kein Punkt hier ist eine freie Geschmacksentscheidung — jeder ergibt sich zwingend daraus, dass die eigenen XAML-Dateien Vorrang haben. In allen vier Fällen ließ sich ONLYOFFICE anpassen, ohne die eigenen Konzepte zu verändern:

| Thema | ONLYOFFICE-Original | Verbindlich laut eigenem Konzept | Warum ONLYOFFICE hier weichen muss |
|---|---|---|---|
| ~~Seite im Dark Mode~~ | immer weiß | ~~`Color.PageBg` dunkel (`#1E2638`)~~ | **⚠ Diese Zeile war falsch — siehe den Kasten darunter** |
| ~~Formatvorlagen-Karten~~ | immer weiß | ~~`Color.PageBg` (folgt der Seite)~~ | **⚠ Folgefehler derselben Zeile** |
| Eingabefelder im Dark Mode | bleiben hell (wie Content) | folgen `Brush.CardBg` (dunkel) | eigenes Tokensystem sieht für UI-Formularelemente `CardBg` vor, keinen fixen Hellwert |
| Akzentfarben | 1 (Blau) | 4 Tokens vorhanden (Blau + 3 Zweitfarben) | eigenes System definiert vier Akzente; ONLYOFFICEs „nur Blau"-Prinzip wird auf die Rollenverteilung in Punkt 9 übertragen, statt drei Tokens ungenutzt zu lassen |

> **⛔ Nachtrag vom 2026-09-03 — der Fall, den dieses Dokument sich selbst zum Nachtragen aufgegeben hat.**
>
> Die Priorisierungsregel ganz oben verspricht: *„sollte dennoch ein Fall auftreten, der sich nicht ohne
> Änderung an den XAML-Dateien lösen lässt, wird das an dieser Stelle ausdrücklich ergänzt."* **Hier ist er
> — und er lag nicht am Farbsystem, sondern an einer Verwechslung in diesem Dokument.**
>
> `ThemeColor.PageBg` (`#FFFFFF` hell / `#1E2638` dunkel) ist **das Blatt der Zeichenfläche** — liniertes,
> kariertes, punktiertes Papier eines Notizbuchs, das mit dem Erscheinungsbild mitwechseln soll. Die Seite
> eines **Textdokuments** ist etwas anderes: Sie wird gedruckt und exportiert, und sie ist deshalb in beiden
> Erscheinungsbildern **weiß**, mit `#1B2B4B` als Tinte. Beide Köpfe halten es so, unabhängig voneinander
> und aus demselben Grund: `TdRenderer.Papier = SKColors.White` in Core (*„Ein Dokument ist Papier"*) und
> `PageBgBrush = #FFFFFF` im WPF-Kopf (*„Papier ist Papier", Nutzer-Wunsch*).
>
> **Was daraus folgt:** Die Zeilen „Seite im Dark Mode" und „Formatvorlagen-Karten" oben beschrieben nie
> das gebaute Programm, sondern eine Ableitung aus dem falschen Token. `Light.xaml` und `Dark.xaml` sind
> **trotzdem unverändert geblieben** — sie sagen über die Textdokumentseite gar nichts aus, das war der
> Irrtum. Der ONLYOFFICE-Punkt „die Seite bleibt immer weiß" musste hier also **nicht** weichen; er stimmt.
>
> *Nicht das Farbsystem hat sich als zu eng erwiesen, sondern die Annahme, ein Tokenname sage schon, wofür
> das Token gilt.*

---

## 11. Zusammenfassung

Das Konzept übernimmt von ONLYOFFICE die **Zonierung** (Chrome/Content-Trennung, Ribbon-Struktur, Icon-Leisten, Statusleiste) und die **Interaktionslogik** (Akzent nur für Zustand, Linie statt Fläche bei Aktivierung, Trennlinien statt Schatten). Farblich gilt ausschließlich das bestehende eigene Tokensystem aus `Light.xaml` / `Dark.xaml`, wobei die vier Akzentfarben eine klare Rollenverteilung erhalten (Blau = System, Türkis/Pink/Lila = Inhalt/Kollaboration).

**Ergebnis der Kollisionsprüfung:** An den Stellen, an denen ONLYOFFICEs Original-Verhalten dem eigenen Farbkonzept widersprach (Eingabefelder, Anzahl der Akzentfarben — siehe Abschnitt 10), ließ sich ONLYOFFICE anpassen, ohne `Light.xaml` oder `Dark.xaml` zu verändern. **Beide Dateien sind bis heute unverändert.**

**⚠ Zwei der ursprünglich vier Kollisionen waren aber gar keine** (Nachtrag 2026-09-03, siehe Abschnitt 10): Seite und Formatvorlagen-Karten im Dark Mode standen auf einem Token, das für die *Zeichenfläche* gilt und nicht für die Textdokumentseite. Der Konflikt lag nicht zwischen ONLYOFFICE und dem Farbsystem, sondern zwischen diesem Dokument und dem gebauten Programm — und die Instanz, die ihn gefunden hat, war **der Vergleich am laufenden Programm**, nicht das Nachlesen.

**Und seit dem 2026-09-03 gilt das Konzept für beide Köpfe** (siehe den Kasten ganz oben). Die Zonen 7.2, 7.4, 7.5, 7.7 und 7.8 sowie die Interaktionszustände aus Abschnitt 8 stehen jetzt im WPF- **und** im Avalonia-Kopf; die einzige Zone, die bewusst nur einer hat, ist das Lineal (7.6).
