[← Index: HANDOFF.md](../HANDOFF.md)

## 5b. Wann und wie auf den CachyOS-Laptop wechseln

> **⛔ HISTORIE (2026-09-09): Diesen Aufbau gibt es nicht mehr — und den Laptop auch nicht.**
> Entwickelt wird auf dem Lenovo Yoga unter Omarchy, und zwar **als einzigem Gerät** (§0).
> Der CachyOS-Laptop ist weg, der Plan „auf ihn kommt Windows als Gegenprobe" ist hinfällig,
> **Dauerregel 3a ist gestrichen**. Dieser Abschnitt wird **nicht nachgezogen**: er steht nur
> noch als Begründungsspeicher da. Wer hier einen Auftrag liest, liest einen Auftrag an ein
> Gerät, das es nicht mehr gibt.

> *(Dieser Abschnitt war die Begründung zu **Dauerregel 3a** — der Schlusszeile „ist der
> Laptop dran?". Die Regel ist am 2026-09-09 gestrichen, weil es den Laptop nicht mehr gibt.)*
>
> **Was auf dem Laptop zu tun ist, steht nicht hier, sondern in §5d.** Dieser Abschnitt sagt
> *ob und warum* gewechselt wird, §5d sagt *was dann zu tun ist* — samt dem **Prompt zum
> Kopieren**, den der Nutzer dort drüben einfügt.

**Kurz: noch nicht umziehen — auch nicht in Phase 3.** Entwickelt wird unter Windows; der
Laptop ist Pflicht für alles, was am **Stift** und an **Linux-Pfaden** hängt.

> **Der zweite Brocken von Phase 3 lief trotzdem hier** (2026-08-03, §4.10) — und das war
> richtig so: er hing an allen fünf Punkten, die der Kasten unten dem Laptop zuschreibt.
> Die Regel bleibt aber, wie sie steht. Was der Laptop **nicht** kann, hat sich dabei
> gezeigt: die Fernsteuer-Werkzeuge gab es hier gar nicht (jetzt in `tools/linux/`, §4.10),
> eine X11-Fensteraufnahme zeigt weder Menüs noch Wayland-Oberflächen (§7), und beide Köpfe nebeneinander an
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
