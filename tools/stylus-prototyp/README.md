# Stylus-Prototyp

Messwerkzeug zu **Docs/HANDOFF.md §5a**: kommt Druckstärke unter Linux zuverlässig in Avalonia an?
Kein Produktivcode — deshalb liegt das hier unter `tools/` und nicht unter `src/`.

Das Projekt ist bewusst aus der zentralen Paketverwaltung ausgeklinkt
(`ManagePackageVersionsCentrally=false`), damit die Avalonia-Version hier nicht die
Versionswahl für Phase 3 vorwegnimmt.

## Warum drei Schichten statt nur der App

Zeigt Avalonia keinen Druck, kann das am Gerät, am Kernel, an libinput oder am Toolkit
liegen. Nur ein Test von unten nach oben sagt, **welche** Schicht schuld ist:

| Schicht | Werkzeug | Frage |
|---|---|---|
| Kernel / evdev | `evdev_beschreiben.py` | Hat das Gerät überhaupt eine `ABS_PRESSURE`-Achse? |
| Kernel / evdev | `evdev_druck.py` | Ändern sich die Werte beim Aufdrücken wirklich? |
| libinput | `libinput list-devices`, `libinput debug-events` | Reicht libinput die Achse weiter? |
| Toolkit | `GonkNote.StylusProbe` | Kommt der Druck in Avalonia an? |

## Voraussetzungen

```bash
sudo pacman -S --needed dotnet-sdk libinput-tools
```

`libinput` allein genügt **nicht** — `list-devices` und `debug-events` stecken im separaten
Paket `libinput-tools`.

Für den Zugriff auf den Device-Node:

```bash
# Nur lesen (reicht für die beiden Python-Skripte):
sudo setfacl -m u:gonk:r  /dev/input/eventN
# libinput öffnet read-write und braucht deshalb mehr:
sudo setfacl -m u:gonk:rw /dev/input/eventN
```

Beides gilt bis zum nächsten Neustart. Dauerhaft: `sudo usermod -aG input $USER` und neu
anmelden.

Den richtigen `eventN` findet man über:

```bash
grep -B3 Handlers /proc/bus/input/devices | grep -iE 'pen|stylus|wacom' -A3
```

## Benutzung

```bash
python3 evdev_beschreiben.py /dev/input/event13      # Achsenbereiche
python3 evdev_druck.py       /dev/input/event13 30   # 30 s mitschneiden, dann Statistik

cd GonkNote.StylusProbe
STYLUS_BERICHT=../messungen/lauf.txt dotnet run
```

Die App zeichnet den Druck als Kreisradius und blendet oben links laufend die Diagnose ein.
Rechte Maustaste leert die Fläche. Der Bericht wird **jede Sekunde** geschrieben, nicht erst
beim Schließen — sonst geht die Messung verloren, wenn der Prozess abgeräumt wird.

## Worauf es beim Ablesen ankommt

- **Anzahl verschiedener Druckwerte**, nicht der Momentanwert. Ein Gerät ohne Druck meldet in
  Avalonia konstant den Ersatzwert `0.5`; erst viele verschiedene Werte belegen echten Druck.
- **Getrennt je Zeigertyp.** Stift, Finger und Maus werden einzeln bewertet. Global gemessen
  gälte die Maus nach dem ersten Stiftstrich fälschlich als druckfähig und bekäme aus ihrem
  konstanten `0.5` eine starre Mittelbreite, statt in den Fallback zu laufen.
- **`Backend`-Zeile.** Avalonia 12 hat für Linux nur ein X11-Backend; unter einer
  Wayland-Sitzung läuft es also über XWayland. Die Zeile sagt, was tatsächlich aktiv war.

## Fallback

Liefert ein Zeigertyp keinen Druck, kommt die Strichbreite aus der Zeichengeschwindigkeit
(schnell = dünner), geglättet gegen Zittern. Das ist laut §5a Pflicht, nicht Kür: die App soll
mit **jedem** Stylus einen sauberen Strich zeigen.

Ergebnisse der Läufe liegen in `messungen/`.
