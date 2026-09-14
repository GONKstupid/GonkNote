[← Index: HANDOFF.md](../HANDOFF.md)

## 5c. Zugang zu GitHub — Stand beider Rechner

> **⚠ GERÄTEWECHSEL (2026-09-08, V2-128): Dieser Abschnitt beschreibt den alten Aufbau.**
> Entwickelt wird jetzt auf dem Lenovo Yoga unter Omarchy (§0, „Neuer Entwicklungsrechner");
> `origin` läuft dort über SSH `git@github.com:GONKstupid/GonkNote.git` (funktioniert, eigene
> Zeile in der Tabelle unten). Der Windows-Entwicklungsrechner ist raus; auf den
> CachyOS-Laptop kommt Windows als Gegenprobe-Gerät (noch nicht eingerichtet). Bis der
> Windows-Laptop steht, sind die beiden alten Rechnerzeilen Historie. Nachgezogen wird
> §5b–§5e, sobald der Windows-Laptop eingerichtet ist.

**Hier steht bewusst kein Schlüsselmaterial und kein Token.** Diese Datei liegt im Repo
(Kopfzeile) — was hier landet, ist irgendwann öffentlich. Nur *wo* etwas liegt, nicht *was*.

| Rechner | Zugang |
|---|---|
| **Windows** (dieser) | Eigener SSH-Key in `%USERPROFILE%\.ssh\id_ed25519`, bei GitHub als „Windows-Entwicklungsrechner (gonk)" hinterlegt. `origin` läuft über `git@github.com:…` |
| **CachyOS-Laptop** | Eigener SSH-Key in `~/.ssh/id_ed25519`, bei GitHub als „CachyOS-Laptop (gonk)" (§5b) |
| **Lenovo Yoga (Omarchy)** — neuer Entwicklungsrechner (V2-128) | Repo unter `/home/gonk/Projects/gonk-note-V2`, `origin` über SSH `git@github.com:GONKstupid/GonkNote.git` (funktioniert). Eigener SSH-Key des Geräts — **Bezeichnung bei GitHub hier nachtragen, sobald bestätigt.** |

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
