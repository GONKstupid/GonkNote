#!/bin/sh
# Der Startknopf im Paket. /app/gonknote/GonkNote.Avalonia ist der apphost aus
# `dotnet publish --self-contained` — eine ELF-Datei, die ihre Laufzeit daneben findet.
#
# `exec`, damit kein Zwischenprozess stehen bleibt: die Sandbox beendet sich, wenn ihr
# erster Prozess geht, und ein `sh`, das auf ein Kind wartet, verzögert genau das.
#
# "$@" wird durchgereicht — der Kopf nimmt `--db <pfad>` (HANDOFF Dauerregel 4).
exec /app/gonknote/GonkNote.Avalonia "$@"
