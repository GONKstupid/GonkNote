#!/usr/bin/env bash
# Wegwerf-Werkzeug: klicken und tippen, dann fotografieren -- das Linux-Gegenstueck zu
# tools/klick.ps1 und tools/kette.ps1 in einem. Kein Produktivcode (HANDOFF §3).
#
# Anders als unter Windows braucht es hier keine zwei Skripte: die Falle, gegen die
# kette.ps1 gebaut wurde, gibt es unter X11 nicht. SetForegroundWindow schliesst dort jedes
# offene Menue, weshalb ein Skript, das je Klick erst das Fenster nach vorn holt, ueber den
# ersten Menueeintrag nie hinauskommt (HANDOFF §7). xdotool klickt, wo der Zeiger hinzeigt,
# ohne den Fokus anzufassen -- eine Kette von Schritten ist damit der Normalfall.
#
# WAS ES NICHT LEISTET: den Stift. xdotool bewegt einen Mauszeiger; Druck, Neigung und die
# Unterscheidung Stift/Finger entstehen im Digitizer und lassen sich nicht nachbilden.
# Alles, was am Stift haengt, bleibt Handarbeit -- dafuer gibt es F9 in der Zeichenflaeche.
set -euo pipefail

Bild="${TMPDIR:-/tmp}/gonk-schuss.png"
Pause=350

hilfe() {
    cat <<'ENDE'
Aufruf: tools/linux/klick.sh [--bild <pfad>] [--pause <ms>] <Schritt> [<Schritt> ...]

Ein Schritt ist:
  x,y        Klick (linke Taste)
  x,y,2      Doppelklick
  x,y,r      Rechtsklick
  #TASTEN    Tastenkombination: #Escape  #ctrl+z  #F9  #ctrl+shift+a
  :text      Text tippen
  warte:500  Pause in Millisekunden

Beispiel -- Menuepfad "Ansicht -> Design":
  tools/linux/klick.sh '#Escape' 292,45 122,211

JEDE KETTE MIT '#Escape' BEGINNEN. Ein offen gelassenes Menue laeuft im naechsten Aufruf
gegen die Wand: das Popup ist zu, Avalonias Menue haelt seinen Zustand aber noch -- der
naechste Klick auf die Menueleiste schliesst dann, statt zu oeffnen, und alle folgenden
Schritte landen auf dem, was darunter liegt (HANDOFF §7).

KOORDINATEN SIND ECHTE BILDSCHIRMPIXEL. Auf einer Vollbildaufnahme gemessen stimmen sie
ueberein; auf einer Fensteraufnahme nicht (Fensterkoordinaten sind nicht
Bildschirmkoordinaten).
ENDE
}

Schritte=()
while [[ $# -gt 0 ]]; do
    case "$1" in
        --bild)    Bild="$2"; shift 2 ;;
        --pause)   Pause="$2"; shift 2 ;;
        -h|--help) hilfe; exit 0 ;;
        *) Schritte+=("$1"); shift ;;
    esac
done

if [[ ${#Schritte[@]} -eq 0 ]]; then hilfe >&2; exit 2; fi

Wurzel="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
Zeiger="$Wurzel/tools/linux/zeiger/bin/Release/net10.0/zeiger"

# Geklickt wird ueber ein eigenes kleines Werkzeug (tools/linux/zeiger), nicht ueber
# xdotool. Der Grund steht dort ausfuehrlich; kurz: xdotool ist ein Paket, das erst
# installiert werden muss, und sudo braucht auf diesem Laptop ein Passwort -- die beiden
# Bibliotheken, die xdotool selbst benutzt (libX11, libXtst), liegen dagegen ohnehin auf
# jedem X-System. Das Werkzeug laeuft damit sofort und ueberall.
if [[ ! -x "$Zeiger" ]]; then
    echo "Nicht gebaut: $Zeiger" >&2
    echo "  dotnet build $Wurzel/tools/linux/zeiger -c Release" >&2
    exit 3
fi
if ! command -v import >/dev/null; then
    echo "FEHLT: import (ImageMagick). Nachinstallieren: sudo pacman -S imagemagick" >&2
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

for s in "${Schritte[@]}"; do
    "$Zeiger" "$s" || exit $?
    sleep "$(awk "BEGIN{print $Pause/1000}")"
done

Fensterbild "$Bild"
echo "Bild=$Bild"
