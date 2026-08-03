#!/usr/bin/env bash
# Wegwerf-Werkzeug: den Avalonia-Kopf starten und fotografieren -- das Linux-Gegenstueck zu
# tools/schau.ps1. Kein Produktivcode; steht unter tools/ und nicht in der Solution
# (HANDOFF §3).
#
# WARUM ES DAS BRAUCHT: die drei Skripte unter tools/ sind Windows-PowerShell
# (SetForegroundWindow, SendKeys, System.Drawing) und haben unter Linux kein Gegenstueck.
# Ohne sie laesst sich "am laufenden Programm gegengeprueft" (Dauerregel 1 und 4) auf dem
# Laptop nicht belegen, sondern nur behaupten.
#
# WAS ES NICHT LEISTET: den Stift. Druck laesst sich nicht synthetisieren -- ein Strich mit
# echter Druckstaerke bleibt Handarbeit. Genau dafuer gibt es die Stift-Anzeige mit F9 in
# der Zeichenflaeche: sie schreibt hin, was wirklich ankommt, und ist damit auf einem Foto
# nachlesbar.
set -euo pipefail

Wurzel="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

Db="${TMPDIR:-/tmp}/gonk-probe.sqlite"
Bild="${TMPDIR:-/tmp}/gonk-schuss.png"
Konfig="Debug"
WarteMs=4000
Starten=1

hilfe() {
    cat <<'ENDE'
Aufruf: tools/linux/schau.sh [Optionen]

  --db <pfad>       Datenbank (Standard: $TMPDIR/gonk-probe.sqlite)
  --konfig <Kfg>    Debug (Standard) oder Release
  --bild <pfad>     Zieldatei des Fotos (Standard: $TMPDIR/gonk-schuss.png)
  --warte <ms>      Wartezeit nach dem Start (Standard: 4000)
  --nur-foto        Nichts starten, nur den Bildschirm fotografieren
  -h, --help        Diese Hilfe

NIE OHNE --db PRUEFEN. Der Datenordner ist unter Linux ~/.config/GonkNote; die Regel ist
dieselbe wie unter Windows (HANDOFF Dauerregel 4). Der Standard oben zeigt bewusst nach
/tmp und nicht dorthin.
ENDE
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --db)       Db="$2"; shift 2 ;;
        --konfig)   Konfig="$2"; shift 2 ;;
        --bild)     Bild="$2"; shift 2 ;;
        --warte)    WarteMs="$2"; shift 2 ;;
        --nur-foto) Starten=0; shift ;;
        -h|--help)  hilfe; exit 0 ;;
        *) echo "Unbekannte Option: $1" >&2; hilfe >&2; exit 2 ;;
    esac
done

Zeiger="$Wurzel/tools/linux/zeiger/bin/Release/net10.0/zeiger"

if ! command -v import >/dev/null; then
    echo "FEHLT: import (ImageMagick). Nachinstallieren: sudo pacman -S imagemagick" >&2
    exit 3
fi
if [[ ! -x "$Zeiger" ]]; then
    echo "Nicht gebaut: $Zeiger" >&2
    echo "  dotnet build $Wurzel/tools/linux/zeiger -c Release" >&2
    exit 3
fi

# Aufgenommen wird DAS FENSTER, nicht die Bildschirmwurzel. Das ist unter Wayland kein
# Geschmack, sondern die einzige Fassung, die stimmt: Avalonia laeuft ueber XWayland
# (HANDOFF §5a), und das X-Wurzelfenster ist dort kein verlaessliches Abbild -- mutter
# haelt es nicht nach, sodass eine Aufnahme davon alten Inhalt neben neuem zeigt. Beim
# ersten Versuch sah das aus, als zeichne die App ihr Fenster doppelt.
#
# `import -window <id>` liest dagegen das Fenster selbst und liefert ein sauberes Bild.
# Zwei Eigenheiten dieses ImageMagick-Stands (7.1.2-29) gehoeren dazu:
#   * `import -window root <datei>` scheitert mit "missing an image filename", obwohl der
#     X-Delegat vorhanden ist -- mit einer Fensterkennung statt `root` laeuft derselbe
#     Aufruf durch.
#   * Der GNOME-Weg ueber D-Bus (org.gnome.Shell.Screenshot) antwortet mit
#     "Screenshot is not allowed"; der Portal-Weg braucht eine Rueckfrage beim Nutzer.
#
# WAS DAMIT FEHLT: Avalonias Menues und Flyouts sind eigene Fenster (HANDOFF §7) und
# stehen deshalb NICHT auf der Fensteraufnahme. Wer einen Menuepfad belegen will, nimmt
# `zeiger fenster` fuer die Kennung des Popups -- es taucht dort als eigener Eintrag auf.
Fensterbild() {
    local ziel="$1"
    local id
    id="$("$Zeiger" fenster | sed -n 's/^id=\([^ ]*\).*/\1/p')"
    if [[ -z "$id" ]]; then
        echo "Kein Fenster gefunden -- laeuft der Kopf?" >&2
        return 1
    fi
    import -window "$id" "$ziel"
}

Pid=""
if [[ $Starten -eq 1 ]]; then
    Projekt="$Wurzel/src/GonkNote.Avalonia"
    Exe="$Projekt/bin/$Konfig/net10.0/GonkNote.Avalonia"
    if [[ ! -x "$Exe" ]]; then
        echo "Nicht gebaut: $Exe" >&2
        echo "  dotnet build $Projekt${Konfig:+ -c $Konfig}" >&2
        exit 1
    fi

    "$Exe" --db "$Db" &
    Pid=$!

    # Nur auf die EIGENE PID warten. Nie pauschal nach Prozessen oder Fenstern suchen und
    # nie pkill nach Namen -- der Nutzer hat die App oft selbst offen (HANDOFF §7).
    sleep "$(awk "BEGIN{print $WarteMs/1000}")"

    if ! kill -0 "$Pid" 2>/dev/null; then
        echo "Der Kopf ist beim Start gestorben (PID $Pid). Protokoll:" >&2
        echo "  ~/.config/GonkNote/gonknote.log" >&2
        exit 1
    fi
fi

Fensterbild "$Bild"

echo "PID=${Pid:-–} Bild=$Bild"
