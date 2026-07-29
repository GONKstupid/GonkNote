#!/usr/bin/env python3
"""Liest die Achsen-Eckdaten eines evdev-Geraets per EVIOCGABS aus.

Braucht nur Lesezugriff auf den Device-Node (im Gegensatz zu libinput, das
read-write oeffnet). Liefert die Rohbereiche, die libinput spaeter auf 0..1
normalisiert - genau die Zahlen, die man fuer die Druck-Aufloesung braucht.
"""
import fcntl
import struct
import sys

# struct input_absinfo: value, minimum, maximum, fuzz, flat, resolution
ABSINFO = struct.Struct("6i")

ACHSEN = {
    0x00: "ABS_X",
    0x01: "ABS_Y",
    0x18: "ABS_PRESSURE",
    0x1A: "ABS_TILT_X",
    0x1B: "ABS_TILT_Y",
    0x1C: "ABS_TOOL_WIDTH",
    0x2C: "ABS_MISC",
}


def eviocgabs(achse):
    """_IOR('E', 0x40 + achse, struct input_absinfo)"""
    return (2 << 30) | (ABSINFO.size << 16) | (0x45 << 8) | (0x40 + achse)


def eviocgname(laenge=256):
    """_IOC(_IOC_READ, 'E', 0x06, laenge)"""
    return (2 << 30) | (laenge << 16) | (0x45 << 8) | 0x06


def main(pfad):
    with open(pfad, "rb") as f:
        puffer = bytearray(256)
        fcntl.ioctl(f, eviocgname(256), puffer)
        name = puffer.split(b"\x00")[0].decode()
        print(f"Geraet : {pfad}")
        print(f"Name   : {name}")
        print()
        print(f"{'Achse':<16}{'min':>8}{'max':>8}{'fuzz':>8}{'flat':>8}{'res':>8}{'Stufen':>10}")
        print("-" * 66)

        for code, bezeichnung in ACHSEN.items():
            try:
                roh = fcntl.ioctl(f, eviocgabs(code), bytes(ABSINFO.size))
            except OSError:
                continue  # Achse existiert auf diesem Geraet nicht
            _wert, minimum, maximum, fuzz, flat, aufloesung = ABSINFO.unpack(roh)
            if (minimum, maximum) == (0, 0):
                continue
            stufen = maximum - minimum + 1
            print(
                f"{bezeichnung:<16}{minimum:>8}{maximum:>8}{fuzz:>8}"
                f"{flat:>8}{aufloesung:>8}{stufen:>10}"
            )


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("Aufruf: evdev_describe.py /dev/input/eventN")
    main(sys.argv[1])
