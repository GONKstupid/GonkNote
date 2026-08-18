#!/usr/bin/env bash
# Wegwerf-Werkzeug: ein Foto des ganzen Bildschirms unter Wayland. Kein Produktivcode; steht
# unter tools/ und nicht in der Solution (HANDOFF §3).
#
# WARUM ES DAS BRAUCHT: schau.sh fotografiert ueber X11 ein einzelnes Fenster. Das reicht fuer
# den Kopf selbst, aber nicht fuer alles, was *neben* ihm steht:
#   * ein Avalonia-Flyout ist ein EIGENES X-Fenster und steht auf einer Fensteraufnahme nicht
#     mit drauf -- ein Klick auf ein Menue sieht dann wie ein wirkungsloser Klick aus,
#     obwohl das Menue offen ist (HANDOFF §7, gefunden in V2-55);
#   * GNOMEs Bildschirmtastatur ist eine WAYLAND-Oberflaeche und in einer X11-Aufnahme
#     grundsaetzlich unsichtbar -- genau die Frage, um derentwillen es §5d gab.
#
# WARUM UEBER DAS PORTAL: org.gnome.Shell.Screenshot und org.gnome.Shell.Introspect antworten
# unter GNOME 50 mit AccessDenied. Der Portal-Weg ist der einzige, der bleibt.
#
# WENN ES HAENGT, LAEUFT DAS PORTAL NICHT:
#   systemctl --user start xdg-desktop-portal-gnome.service xdg-desktop-portal.service
# Ohne laufenden Dienst kommt schlicht nie eine Antwort -- keine Fehlermeldung, nur Warten.
# Das war der Grund, aus dem §5d/§4.10 Vollbildaufnahmen jahrelang fuer "unbrauchbar" hielten.
#
# ACHTUNG, ZWEI KOORDINATENSYSTEME: das Foto ist in LOGISCHEN Pixeln (auf diesem Geraet
# 1920x1080), zeiger und XTEST rechnen in GERAETEpixeln (3072x1728). Auf einem Foto von hier
# gemessene Punkte muessen mit dem Skalierungsfaktor multipliziert werden, sonst landet jeder
# Klick zu weit oben links.
set -euo pipefail

Ziel="${1:-${TMPDIR:-/tmp}/gonk-wlschuss.png}"

if [[ "$Ziel" == "-h" || "$Ziel" == "--help" ]]; then
    cat <<'HILFE'
Aufruf: tools/linux/wlschuss.sh [Zieldatei]

  Fotografiert den ganzen Bildschirm ueber xdg-desktop-portal und legt das Bild unter
  Zieldatei ab (Standard: $TMPDIR/gonk-wlschuss.png). Gibt den Pfad aus.

  Fuer eine reine Fensteraufnahme ueber X11 ist schau.sh --nur-foto schneller; dieses
  Werkzeug ist fuer alles, was neben dem Fenster steht (Menues, Bildschirmtastatur).
HILFE
    exit 0
fi

python3 - "$Ziel" <<'PY'
import random, shutil, sys, urllib.parse
import dbus, dbus.mainloop.glib
from gi.repository import GLib

dbus.mainloop.glib.DBusGMainLoop(set_as_default=True)
sitzung = dbus.SessionBus()
portal = sitzung.get_object('org.freedesktop.portal.Desktop',
                            '/org/freedesktop/portal/desktop')
schnitt = dbus.Interface(portal, 'org.freedesktop.portal.Screenshot')

# interactive=False: ohne Rueckfrage und ohne Auswahlrahmen -- sonst wartet es auf einen
# Nutzer, den es bei einer Fernsteuerung nicht gibt.
anfrage = schnitt.Screenshot('', {'handle_token': 'gonk%d' % random.randint(1, 10**9),
                                  'interactive': dbus.Boolean(False)})

schleife, antwort = GLib.MainLoop(), {}
def fertig(code, ergebnis):
    antwort['code'] = code
    antwort['uri'] = dict(ergebnis).get('uri')
    schleife.quit()

sitzung.add_signal_receiver(fertig, 'Response', 'org.freedesktop.portal.Request',
                            path=anfrage)
GLib.timeout_add_seconds(20, schleife.quit)
schleife.run()

if antwort.get('code') != 0:
    sys.exit('Kein Foto. Laeuft das Portal? '
             'systemctl --user start xdg-desktop-portal-gnome.service')

# Das Portal legt das Bild selbst ab (meist unter ~/Bilder) und gibt eine URI zurueck --
# verschieben statt kopieren, sonst sammelt sich dort bei jedem Aufruf eine Datei an.
shutil.move(urllib.parse.unquote(urllib.parse.urlparse(antwort['uri']).path), sys.argv[1])
print(sys.argv[1])
PY
