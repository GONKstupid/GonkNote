<!-- Kurz halten. Wer eine lange Vorlage vorfindet, löscht sie -- und dann steht gar nichts da. -->

## Was und warum · What and why

<!-- Zwei bis drei Sätze. Das „warum" ist der Teil, der später fehlt. -->

Behebt · Fixes: #

## Geprüft · Checked

- [ ] `dotnet build GonkNote.slnx -c Release -warnaserror` — 0 Fehler, 0 Warnungen
- [ ] `dotnet test GonkNote.slnx -c Release` — grün
- [ ] **Am laufenden Programm gesehen**, wenn etwas Sichtbares betroffen ist —
      in **beiden** Ausgaben · *seen in the running program, in **both** editions*
- [ ] Neue Oberflächentexte stehen in **beiden** Sprachtabellen (`LocGerman`, `LocEnglish`)
- [ ] Nichts Berechnetes ist in einer Oberfläche gelandet, was nach `GonkNote.Core` gehört

<!--
  Der dritte Haken ist der, auf den es ankommt. Ein grüner Bau beweist an einer Oberfläche
  fast nichts -- in diesem Projekt haben mehrere Runden hintereinander Fehler gefunden, die
  kein Test sehen konnte. Wenn du ihn nicht setzen kannst, schreib dazu warum; das ist eine
  brauchbare Antwort, ein stillschweigend gesetzter Haken nicht.
-->
