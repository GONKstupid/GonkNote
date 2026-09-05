# Auslieferung der Linux-Ausgabe

Hier stehen die zwei Wege, auf denen der Linux-Kopf das Gerät verlässt. Sie sind in
**Phase 5, Schritt ③** entstanden (`Docs/HANDOFF.md`, §6) und **auf dem CachyOS-Laptop
gebaut und gestartet**, nicht hergeleitet.

> **✅ Der zweite Bau ist gelaufen** (2026-09-04, V2-124, auf dem CachyOS-Laptop): **beide**
> Pakete gebaut, installiert und gestartet, nachdem die Runden von Schritt ④ (§4.97–§4.99)
> darin sind. Schritt ③ war eine **Erprobung**; **dieser Bau ist der, der hinausgeht.**
>
> ✅ **Und der Posten, der dazu noch offen war, ist erledigt** (2026-09-05, V2-125, Schritt ⑤):
> Die Version steht in `Directory.Build.props`, in `About.Version` (beide Sprachtabellen) und
> in der `metainfo.xml` auf **1.0.0**; der `<release>`-Eintrag beschreibt eine Ausgabe und
> nennt sich nicht mehr selbst „packaging trial“. Der alte 0.3.0-Eintrag bleibt als
> `type="development"` stehen — eine Fassung, die es gab, wird nicht nachträglich weggelassen.
>
> ⛔ **Neu und leicht zu übersehen:** Die `metainfo.xml` trägt jetzt `<screenshots>`, und die
> zeigen auf **GitHub Pages** (`site/bilder/*.png` dieses Repos). **Wer dort eine Datei
> umbenennt, bricht den Flathub-Eintrag** — und es fällt erst bei der Einreichung auf, nicht
> beim Bauen.

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

> ✅ **Am Gerät geprüft** (2026-09-04, V2-122, CachyOS — der Befund steht in §4.98 „Was der
> Laptop gefunden hat“). Und er ist **nicht** auf dem Baurechner im Normalzustand entstanden:
> dort ist Tesseract installiert, die App fände es also auch ohne das Mitgelieferte. Gemessen
> wurde deshalb mit **verstecktem System-Tesseract** (`bwrap`) und einer Wegwerf-Sonde im
> Namensraum des Abbilds — sie sagt nicht nur, *dass* erkannt wird, sondern **welche Datei
> geladen ist**: `/proc/self/maps` nennt `libtesseract.so.5` und `libleptonica.so.6`
> namentlich aus `usr/bin/lib`. **Der Beipack kostet 24,5 MiB (61 → 85 MiB).**
>
> ⚠ **Die eine Schranke dabei ist gemessen und nicht hergeleitet:** Ist das mitgelieferte
> `libtesseract.so.5` unbrauchbar, scheitert die Erkennung — **auch wenn ein tadelloses
> System-Tesseract danebenliegt.** Das Wirtssystem wird nie gefragt.

## Wo die Befunde stehen

**Nicht hier.** Was der Bau und der Lauf ergeben haben, steht in `Docs/HANDOFF.md`, §4.96 —
dort wird es gesucht. Diese Datei sagt nur, wie gebaut wird.
