#!/usr/bin/env bash
# Baut das AppImage des Linux-Kopfs. HANDOFF §6, Phase 5, Schritt ③ — der zweite,
# abhängigkeitsfreie Kanal neben dem Flatpak.
#
# Was ein AppImage hier ist: der selbstenthaltene `dotnet publish`-Ordner, ein Startskript
# und ein Symbol, zusammengefaltet in eine einzige ausführbare Datei. Es installiert nichts
# und braucht kein Flatpak.
#
# ⚠ Was es NICHT löst — und das gehört hierher und nicht in den Befund:
#   Die Texterkennung sucht ihre Systembibliotheken über TesseractBindung.Suchpfade
#   (/app/lib, /usr/lib, …). In einem AppImage gibt es keinen Namensraum: /usr/lib ist der
#   des WIRTS. Ein mitgeliefertes libtesseract im AppDir würde also nicht gefunden.
#   Ergebnis: Texterkennung geht, wenn das Wirtssystem Tesseract hat, und meldet sonst
#   ehrlich „nicht verfügbar" (der Knopf wird ausgeblendet, §4.64). Alles andere — Schrift,
#   Skia, PDF — steckt im Paket.
#
# Voraussetzung: appimagetool. Es wird beim ersten Lauf nach build/ geladen.
#
# Aufruf:  ./bauen.sh
set -euo pipefail

HIER="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WURZEL="$(cd "$HIER/../.." && pwd)"
ID=io.github.gonkstupid.GonkNote
APPDIR="$HIER/build/GonkNote.AppDir"

cd "$HIER"
mkdir -p build

echo "▶ 1/4  appimagetool besorgen"
if [[ ! -x build/appimagetool ]]; then
    curl -L --fail -o build/appimagetool \
        https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
    chmod +x build/appimagetool
fi

echo "▶ 2/4  dotnet publish (selbstenthalten, linux-x64)"
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin"
dotnet publish "$WURZEL/src/GonkNote.Avalonia" \
    -c Release -r linux-x64 --self-contained true \
    -o "$APPDIR/usr/bin"

# ⛔ Dieselben 12 MB Windows-DLLs wie im Flatpak (siehe dortiges Manifest).
rm -rf "$APPDIR/usr/bin/x64" "$APPDIR/usr/bin/x86"

echo "▶ 3/4  AppDir bestücken"
install -Dm644 "$HIER/../flatpak/$ID.desktop"       "$APPDIR/usr/share/applications/$ID.desktop"
install -Dm644 "$HIER/../flatpak/$ID.metainfo.xml"  "$APPDIR/usr/share/metainfo/$ID.metainfo.xml"
install -Dm644 "$WURZEL/Assets/gonk-note-Icon.png"  "$APPDIR/usr/share/icons/hicolor/256x256/apps/$ID.png"

# appimagetool verlangt beides zusätzlich in der Wurzel des AppDir, und das Symbol muss
# dort genauso heißen wie der Icon=-Eintrag der .desktop-Datei.
cp "$APPDIR/usr/share/applications/$ID.desktop"                      "$APPDIR/$ID.desktop"
cp "$APPDIR/usr/share/icons/hicolor/256x256/apps/$ID.png"            "$APPDIR/$ID.png"
install -Dm755 "$HIER/AppRun" "$APPDIR/AppRun"

echo "▶ 4/4  appimagetool"
# ARCH ist Pflicht, sonst rät das Werkzeug und bricht ab.
ARCH=x86_64 ./build/appimagetool --no-appstream "$APPDIR" "build/GonkNote-x86_64.AppImage"

echo
echo "✅ Fertig: $HIER/build/GonkNote-x86_64.AppImage"
echo "   Starten mit:  ./build/GonkNote-x86_64.AppImage --db /tmp/gonk-test/gonknote.sqlite"
echo
echo "⚠ Ohne --db greift der Lauf auf ~/.config/GonkNote zu — das AppImage hat KEINE"
echo "   Sandbox, es ist derselbe echte Bestand wie beim Lauf aus dem Quellordner"
echo "   (HANDOFF Dauerregel 4)."
