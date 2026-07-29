#!/usr/bin/env python3
"""Schneidet Stift-Events mit und wertet den Druckverlauf aus.

Beantwortet Schritt 2 des Stylus-Prototyps: aendern sich die Druckwerte beim
Aufdruecken tatsaechlich, oder liefert das Geraet nur 0/max? Ausgabe ist eine
Statistik plus Histogramm, damit man echte Abstufung von einem simulierten
Zweizustands-Schalter unterscheiden kann.
"""
import select
import struct
import sys
import time
from collections import Counter

# struct input_event auf 64-bit: tv_sec, tv_usec, type, code, value
EVENT = struct.Struct("llHHi")

EV_KEY, EV_ABS = 0x01, 0x03
ABS_PRESSURE, ABS_TILT_X, ABS_TILT_Y = 0x18, 0x1A, 0x1B
BTN_TOUCH, BTN_TOOL_PEN, BTN_TOOL_RUBBER = 0x14A, 0x140, 0x141

DRUCK_MAX = 4095


def main(pfad, dauer):
    druckwerte = []
    tilt_x, tilt_y = [], []
    tasten = Counter()
    beruehrungen = 0
    events = 0
    ende = time.monotonic() + dauer

    print(f"Schneide {dauer:.0f}s mit auf {pfad} - jetzt zeichnen.", flush=True)

    # buffering=0: select() sieht sonst nicht, was schon im Python-Puffer liegt
    with open(pfad, "rb", buffering=0) as f:
        while True:
            rest = ende - time.monotonic()
            if rest <= 0:
                break
            # ohne select wuerde read() blockieren, solange niemand zeichnet,
            # und der Mitschnitt liefe ueber die Dauer hinaus weiter
            if not select.select([f], [], [], min(rest, 0.5))[0]:
                continue
            daten = f.read(EVENT.size)
            if not daten:
                break
            _s, _us, typ, code, wert = EVENT.unpack(daten)
            events += 1
            if typ == EV_ABS:
                if code == ABS_PRESSURE:
                    druckwerte.append(wert)
                elif code == ABS_TILT_X:
                    tilt_x.append(wert)
                elif code == ABS_TILT_Y:
                    tilt_y.append(wert)
            elif typ == EV_KEY:
                if wert == 1:
                    tasten[code] += 1
                if code == BTN_TOUCH and wert == 1:
                    beruehrungen += 1

    print(f"\n{events} Events gesamt, {beruehrungen} Aufsetzer.")

    aktiv = [w for w in druckwerte if w > 0]
    if not aktiv:
        print("KEIN Druck > 0 empfangen.")
        return 1

    einzigartig = sorted(set(aktiv))
    print(f"\nABS_PRESSURE: {len(druckwerte)} Samples, davon {len(aktiv)} > 0")
    print(f"  min/max     : {min(aktiv)} / {max(aktiv)}  (Achse geht bis {DRUCK_MAX})")
    print(f"  Mittelwert  : {sum(aktiv) / len(aktiv):.1f}")
    print(f"  verschiedene Werte: {len(einzigartig)}")

    # Histogramm ueber 16 Klassen - flacht ab, wenn nur zwei Zustaende kommen
    print("\n  Verteilung:")
    klassen = Counter(min(w * 16 // (DRUCK_MAX + 1), 15) for w in aktiv)
    hoechste = max(klassen.values())
    for k in range(16):
        anzahl = klassen.get(k, 0)
        balken = "#" * int(anzahl / hoechste * 40)
        unten = k * (DRUCK_MAX + 1) // 16
        oben = (k + 1) * (DRUCK_MAX + 1) // 16 - 1
        print(f"    {unten:>5}-{oben:<5} {anzahl:>6} {balken}")

    for bezeichnung, werte in (("ABS_TILT_X", tilt_x), ("ABS_TILT_Y", tilt_y)):
        bewegt = [w for w in werte if w != 0]
        if bewegt:
            print(f"\n{bezeichnung}: min {min(bewegt)} / max {max(bewegt)}, "
                  f"{len(set(bewegt))} verschiedene Werte")
        else:
            print(f"\n{bezeichnung}: konstant 0 (keine Neigung gemeldet)")

    namen = {BTN_TOUCH: "BTN_TOUCH", BTN_TOOL_PEN: "BTN_TOOL_PEN",
             BTN_TOOL_RUBBER: "BTN_TOOL_RUBBER", 0x14B: "BTN_STYLUS",
             0x14C: "BTN_STYLUS2"}
    print("\nTasten:", ", ".join(f"{namen.get(c, hex(c))}={n}"
                                 for c, n in sorted(tasten.items())) or "keine")
    return 0


if __name__ == "__main__":
    pfad = sys.argv[1] if len(sys.argv) > 1 else "/dev/input/event13"
    dauer = float(sys.argv[2]) if len(sys.argv) > 2 else 20.0
    sys.exit(main(pfad, dauer))
