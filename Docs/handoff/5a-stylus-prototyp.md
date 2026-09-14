[← Index: HANDOFF.md](../HANDOFF.md)

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
