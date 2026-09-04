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

**⚠ Die Texterkennung hängt hier am Wirtssystem.** Ein AppImage hat keinen eigenen
Namensraum: `/usr/lib` ist der des Wirts, ein mitgeliefertes `libtesseract` würde von
`TesseractBindung.Suchpfade` nicht gefunden. Hat der Wirt Tesseract, geht die Erkennung; hat
er keines, meldet die App ehrlich „nicht verfügbar" und **blendet den Knopf aus** (§4.64).
Alles andere — Schrift, Skia, PDF — steckt im Paket.

## Wo die Befunde stehen

**Nicht hier.** Was der Bau und der Lauf ergeben haben, steht in `Docs/HANDOFF.md`, §4.96 —
dort wird es gesucht. Diese Datei sagt nur, wie gebaut wird.
