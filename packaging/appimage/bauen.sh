#!/usr/bin/env bash
# Baut das AppImage des Linux-Kopfs. HANDOFF §6, Phase 5, Schritt ③ — der zweite,
# abhängigkeitsfreie Kanal neben dem Flatpak.
#
# Was ein AppImage hier ist: der selbstenthaltene `dotnet publish`-Ordner, ein Startskript
# und ein Symbol, zusammengefaltet in eine einzige ausführbare Datei. Es installiert nichts
# und braucht kein Flatpak.
#
# ✅ DIE TEXTERKENNUNG KOMMT SEIT DEM 2026-09-04 MIT (HANDOFF §5 „Noch offen" 29,
#    Nutzer-Entscheidung: „nicht jede Linux-Verteilung hat eine").
#    Vorher stand hier: „Ein AppImage hat keinen Namensraum, /usr/lib ist der des WIRTS, ein
#    mitgeliefertes libtesseract würde also nicht gefunden." Der erste Satz stimmt weiter —
#    der zweite war die Folgerung daraus, und sie ist mit Schritt 5/6 unten aufgelöst:
#      * `TesseractBindung.SuchpfadeMit` in Core sucht `<AppFolder>/lib` ZUERST,
#      * `AppRun` setzt LD_LIBRARY_PATH auf denselben Ordner, damit auch Tesseracts EIGENE
#        Abhängigkeiten gefunden werden (der Systemlader sucht sie nicht neben dem Verweis).
#    Findet sich hier nichts zum Einsammeln, bleibt das AppImage brauchbar: die App fällt auf
#    das Wirtssystem zurück und meldet sonst ehrlich „nicht verfügbar" (§4.64).
#
# ⛔ NOCH NICHT AM GERÄT GEPRÜFT (Stand 2026-09-04, V2-121, geschrieben unter Windows).
#    Schritt 5 und 6 sind hergeleitet und gegen §4.63/§4.96 gelesen, aber NICHT gelaufen.
#    Was der Laptop zu prüfen hat, steht in §5d. Bis dahin gilt: dieses Skript ist ein
#    Entwurf, kein Beleg.
#
# Voraussetzung: appimagetool. Es wird beim ersten Lauf nach build/ geladen.
#
# Aufruf:  ./bauen.sh
set -euo pipefail

HIER="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WURZEL="$(cd "$HIER/../.." && pwd)"
ID=io.github.gonkstupid.GonkNote
APPDIR="$HIER/build/GonkNote.AppDir"

# Der Ordner, in dem die App ihre mitgelieferte Fassung sucht. Er liegt NEBEN DEM PROGRAMM,
# weil `AppContext.BaseDirectory` (= `IAppPaths.AppFolder`) dorthin zeigt — nicht in
# $APPDIR/usr/lib. Derselbe Name steht in `TesseractBindung.EigenerUnterordner` und im AppRun.
LIBDIR="$APPDIR/usr/bin/lib"

cd "$HIER"
mkdir -p build

echo "▶ 1/6  appimagetool besorgen"
if [[ ! -x build/appimagetool ]]; then
    curl -L --fail -o build/appimagetool \
        https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
    chmod +x build/appimagetool
fi

echo "▶ 2/6  dotnet publish (selbstenthalten, linux-x64)"
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin"
dotnet publish "$WURZEL/src/GonkNote.Avalonia" \
    -c Release -r linux-x64 --self-contained true \
    -o "$APPDIR/usr/bin"

# ⛔ Dieselben 12 MB Windows-DLLs wie im Flatpak (siehe dortiges Manifest).
rm -rf "$APPDIR/usr/bin/x64" "$APPDIR/usr/bin/x86"

echo "▶ 3/6  AppDir bestücken"
install -Dm644 "$HIER/../flatpak/$ID.desktop"       "$APPDIR/usr/share/applications/$ID.desktop"
install -Dm644 "$HIER/../flatpak/$ID.metainfo.xml"  "$APPDIR/usr/share/metainfo/$ID.metainfo.xml"
install -Dm644 "$WURZEL/Assets/gonk-note-Icon.png"  "$APPDIR/usr/share/icons/hicolor/256x256/apps/$ID.png"

# appimagetool verlangt beides zusätzlich in der Wurzel des AppDir, und das Symbol muss
# dort genauso heißen wie der Icon=-Eintrag der .desktop-Datei.
cp "$APPDIR/usr/share/applications/$ID.desktop"                      "$APPDIR/$ID.desktop"
cp "$APPDIR/usr/share/icons/hicolor/256x256/apps/$ID.png"            "$APPDIR/$ID.png"
install -Dm755 "$HIER/AppRun" "$APPDIR/AppRun"

# ============================================================================
# 4-6: die Texterkennung einsammeln
# ============================================================================

# Was NICHT mitkommt. Zwei Gruppen, und beide aus einem anderen Grund:
#
#   (a) glibc und ihr Umfeld. Sie müssen zum LADER des Wirts passen — eine mitgelieferte
#       libc neben dem `ld-linux` des Wirts ist der klassische Weg, ein AppImage auf JEDEM
#       fremden Rechner unstartbar zu machen, und der Fehler kommt als Absturz ohne Meldung.
#   (b) Grafik und Fenster (GL, X11, Wayland, drm). Sie hängen am TREIBER des Wirts.
#       Tesseract braucht sie ohnehin nicht; sie tauchen nur über Umwege in `ldd` auf.
#
# libstdc++ steht bewusst in (a): Sie muss zu libgcc und zum GL-Stack des Wirts passen. Der
# Preis ist benannt — auf einer SEHR alten Verteilung könnte die mitgebrachte Tesseract-
# Fassung eine neuere libstdc++ verlangen als der Wirt hat. Dann meldet die Erkennung
# „nicht verfügbar", und das ist die richtige Antwort: die App stürzt nicht ab (§4.64).
AUSSEN='^(ld-linux.*|libc|libm|libdl|libpthread|librt|libresolv|libnsl|libutil|libanl|libgcc_s|libstdc\+\+|libGL|libGLX|libGLdispatch|libEGL|libOpenGL|libX11.*|libxcb.*|libXau|libXdmcp|libXext|libXrender|libXi|libXfixes|libXrandr|libXcursor|libXinerama|libwayland.*|libdrm|libgbm|libglib-2\.0|libgobject-2\.0|libgio-2\.0|libgmodule-2\.0)\.so.*$'

# Findet die Systembibliothek nach DERSELBEN Regel wie `TesseractBindung.SonameWaehlen`:
# die gemessene Hauptversion zuerst, sonst die höchste vorhandene, sonst die unversionierte.
ORDNER=(/usr/lib /usr/lib/x86_64-linux-gnu /usr/lib64 /lib/x86_64-linux-gnu /usr/local/lib)

finde_lib() {
    local stamm="$1" haupt="$2" ordner treffer

    # Stufe 1 — die gemessene Hauptversion. Nichts schlägt das Gemessene (§4.63).
    for ordner in "${ORDNER[@]}"; do
        [[ -e "$ordner/$stamm.so.$haupt" ]] && { echo "$ordner/$stamm.so.$haupt"; return 0; }
    done
    # Stufe 2 — die höchste vorhandene Hauptversion.
    for ordner in "${ORDNER[@]}"; do
        treffer="$(ls -1 "$ordner/$stamm".so.[0-9]* 2>/dev/null | sort -V | tail -n1 || true)"
        [[ -n "$treffer" ]] && { echo "$treffer"; return 0; }
    done
    # Stufe 3 — der unversionierte Name (Entwicklungspaket).
    for ordner in "${ORDNER[@]}"; do
        [[ -e "$ordner/$stamm.so" ]] && { echo "$ordner/$stamm.so"; return 0; }
    done
    return 1
}

echo "▶ 4/6  Texterkennung suchen"
TESS="$(finde_lib libtesseract 5 || true)"
LEPT="$(finde_lib libleptonica 6 || true)"

if [[ -z "$TESS" || -z "$LEPT" ]]; then
    echo "⚠ Keine System-Texterkennung gefunden (tesseract/leptonica)."
    echo "  Das AppImage wird OHNE Texterkennung gebaut — die App meldet dann auf einem"
    echo "  Wirt ohne Tesseract ehrlich 'nicht verfügbar' und blendet den Knopf aus."
    echo "  Zum Mitliefern hier vorher installieren:  pacman -S tesseract leptonica"
else
    echo "   tesseract : $TESS"
    echo "   leptonica : $LEPT"

    echo "▶ 5/6  Abhängigkeiten einsammeln (transitiv)"
    mkdir -p "$LIBDIR"

    # `ldd` löst den ganzen Baum auf einmal auf — deshalb reicht ein Lauf je Wurzel, und
    # eine eigene Rekursion wäre nur eine zweite Fassung derselben Arbeit.
    # Kopiert wird die AUFGELÖSTE Datei unter ihrem SONAME-Namen, damit der Verweis, den die
    # App später anlegt, denselben Namen findet wie im System.
    while read -r pfad; do
        [[ -f "$pfad" ]] || continue
        name="$(basename "$pfad")"
        if [[ "$name" =~ $AUSSEN ]]; then continue; fi
        # `cp -L` löst Verweise auf: im Abbild soll die echte Datei liegen und nicht ein
        # Verweis, der auf das Wirtssystem zeigt — dort gibt es sie später vielleicht nicht.
        cp -Lu "$pfad" "$LIBDIR/$name"
    done < <(
        {
            echo "$TESS"; echo "$LEPT"
            ldd "$TESS" "$LEPT" 2>/dev/null | awk '{for(i=1;i<=NF;i++) if($i ~ /^\//) print $i}'
        } | sort -u
    )

    # Der Name, unter dem die App sucht, ist `libtesseract.so.5` bzw. `libleptonica.so.6`
    # (TesseractBindung.SonameWaehlen, Stufe 1). Liegt im Ordner nur die volle Version —
    # etwa `libtesseract.so.5.5.1`, wie der CMake-Bau sie benennt (§4.96, Fund 2) —, dann
    # findet die App NICHTS und der Bau bliebe grün. Deshalb hier den kurzen Namen anlegen.
    for paar in "libtesseract:5" "libleptonica:6"; do
        stamm="${paar%%:*}"; haupt="${paar##*:}"
        if [[ ! -e "$LIBDIR/$stamm.so.$haupt" ]]; then
            voll="$(ls -1 "$LIBDIR/$stamm".so.* 2>/dev/null | sort -V | tail -n1 || true)"
            [[ -n "$voll" ]] && ln -sf "$(basename "$voll")" "$LIBDIR/$stamm.so.$haupt"
        fi
    done

    echo "   $(find "$LIBDIR" -maxdepth 1 -type f | wc -l) Dateien, $(du -sh "$LIBDIR" | cut -f1)"
    # `echo "$(ls)"` haengte die erste Zeile an die eigene Einrueckung an und schob sie um
    # drei Zeichen ein -- die Liste sah aus, als faenge sie mit einem Sonderfall an.
    ls -1 "$LIBDIR" | sed 's/^/     /'
fi

echo "▶ 6/6  appimagetool"
# ARCH ist Pflicht, sonst rät das Werkzeug und bricht ab.
ARCH=x86_64 ./build/appimagetool --no-appstream "$APPDIR" "build/GonkNote-x86_64.AppImage"

echo
echo "✅ Fertig: $HIER/build/GonkNote-x86_64.AppImage"
echo "   Starten mit:  ./build/GonkNote-x86_64.AppImage --db /tmp/gonk-test/gonknote.sqlite"
echo
echo "⚠ Ohne --db greift der Lauf auf ~/.config/GonkNote zu — das AppImage hat KEINE"
echo "   Sandbox, es ist derselbe echte Bestand wie beim Lauf aus dem Quellordner"
echo "   (HANDOFF Dauerregel 4)."
echo
echo "⛔ Die mitgelieferte Texterkennung ist NOCH NICHT AM GERÄT GEPRÜFT (§5d)."
echo "   Der aussagekräftige Test ist NICHT dieser Rechner — hier ist Tesseract installiert,"
echo "   also kann die App auch ohne das Mitgelieferte erkennen und der Lauf beweist nichts."
echo "   Geprüft wird mit verstecktem System-Tesseract, siehe §5d."
