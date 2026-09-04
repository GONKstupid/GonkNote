# Auslieferung der Linux-Ausgabe

Hier stehen die zwei Wege, auf denen der Linux-Kopf das Gerät verlässt. Sie sind in
**Phase 5, Schritt ③** entstanden (`Docs/HANDOFF.md`, §6) und **auf dem CachyOS-Laptop
gebaut und gestartet**, nicht hergeleitet.

> **⛔ Schritt ③ ist eine Erprobung und keine Auslieferung.** Nach dem Aufräumen (④) wird
> beides noch einmal gebaut, und erst dieser zweite Bau geht hinaus. Wer hier ein fertiges
> Release erwartet, liest den falschen Ordner.

| | Wofür | Ordner |
|---|---|---|
| **Flatpak** | Der Hauptweg, Ziel ist Flathub. Sandbox, Portale, mitgebautes Tesseract | `flatpak/` |
| **AppImage** | Der zweite Kanal: eine Datei, kein Installieren, keine Sandbox | `appimage/` |

## Was beide gemeinsam haben

**Gebaut wird mit `dotnet publish --self-contained`**, nicht aus dem Quelltext heraus. Damit
steckt die .NET-Laufzeit im Paket; das Wirtssystem braucht keine. Für das Flatpak ist es
zusätzlich der Grund, warum das Manifest nur einpackt: `flatpak-builder` baut ohne Netz, und
ein NuGet-Wiederherstellen im Sandkasten bräuchte einen mitgelieferten Paketspiegel.

**Beide werfen `x64/` und `x86/` weg.** Das Tesseract-NuGet legt seine Windows-DLLs als
Inhaltsdateien ab, nicht unter `runtimes/` — `dotnet publish -r linux-x64` nimmt sie deshalb
mit. Das sind **12 MB PE32+-Dateien in einem Linux-Paket**.

## Flatpak

```bash
# einmalig, ohne sudo
flatpak remote-add --user --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo
flatpak install --user flathub org.freedesktop.Platform//25.08 org.freedesktop.Sdk//25.08

cd packaging/flatpak
./bauen.sh                      # baut und installiert ins Nutzer-Flatpak
flatpak run io.github.gonkstupid.GonkNote -- --db /tmp/gonk-test/gonknote.sqlite
```

**Tesseract und Leptonica werden mitgebaut** (Quelltext-Module im Manifest). Keine
Freedesktop-Plattform bringt sie mit, und `TesseractBindung.Suchpfade` in Core hat `/app/lib`
seit jeher an erster Stelle — genau für diesen Fall. **`fontconfig` und `freetype` kommen aus
der Plattform**; ohne sie zeichnet SkiaSharp keine Schrift.

**Der Datenordner ist in der Sandbox ein anderer:**
`~/.var/app/io.github.gonkstupid.GonkNote/config/GonkNote` statt `~/.config/GonkNote`. Das ist
kein Fehler, sondern das, was `$XDG_CONFIG_HOME` in der Sandbox bedeutet — der Kopf rechnet
nichts um und muss es auch nicht.

## AppImage

```bash
cd packaging/appimage
./bauen.sh                      # lädt appimagetool beim ersten Lauf selbst
./build/GonkNote-x86_64.AppImage --db /tmp/gonk-test/gonknote.sqlite
```

**✅ Die Texterkennung kommt seit dem 2026-09-04 mit** — Nutzer-Entscheidung, §5 „Noch offen"
**29**: *„nicht jede Linux-Verteilung hat eine."*

`bauen.sh` sucht `libtesseract`/`libleptonica` auf dem **Baurechner**, holt mit `ldd` ihre
Abhängigkeiten dazu und legt alles nach `usr/bin/lib` im Abbild. Zwei Stellen machen daraus
eine ladbare Kette, und **beide werden gebraucht**:

| | |
|---|---|
| `TesseractBindung.SuchpfadeMit` (Core) | sucht `<AppFolder>/lib` **vor** allen Systempfaden — dort findet die App die Datei, auf die sie ihren Verweis legt |
| `AppRun` (`LD_LIBRARY_PATH`) | damit **Tesseracts eigene** Abhängigkeiten geladen werden. Der Systemlader sucht sie **nicht** neben dem Verweisziel |

> ⛔ **Das widerspricht §4.63 nicht.** Dort wurde gemessen, dass `LD_LIBRARY_PATH` nicht hilft
> — das galt für den Lader **des NuGet-Pakets**, der die Datei *am Pfad prüft, bevor er
> `dlopen` ruft*. Hier geht es um die Stufe danach. *Zwei Stufen, zwei Regeln.*

**Was das nicht ändert:** Findet sich nichts zum Mitnehmen — oder lädt es auf dem fremden
Rechner nicht —, fällt die App auf das Wirtssystem zurück und meldet sonst weiterhin ehrlich
„nicht verfügbar" und **blendet den Knopf aus** (§4.64). *Mitliefern ist eine zusätzliche
Stufe, keine Ablösung.* **`glibc`, `libstdc++` und der Grafikstapel kommen bewusst NICHT mit**
— die Begründung steht als Ausschlussliste in `bauen.sh`.

> ⛔ **Noch nicht am Gerät geprüft** (Stand 2026-09-04, V2-121 — geschrieben unter Windows).
> Und der aussagekräftige Test ist **nicht der Baurechner**: dort ist Tesseract installiert,
> die App fände es also auch ohne das Mitgelieferte. Wie geprüft wird, steht in §5d.

## Wo die Befunde stehen

**Nicht hier.** Was der Bau und der Lauf ergeben haben, steht in `Docs/HANDOFF.md`, §4.96 —
dort wird es gesucht. Diese Datei sagt nur, wie gebaut wird.
