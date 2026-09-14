[← Index: HANDOFF.md](../HANDOFF.md)

## 8. Schnellstart-Befehle

```powershell
cd C:\Dev\Zed\gonk-note-V2

dotnet build -c Release      # 0 Fehler / 0 Warnungen
dotnet build -c Debug        # schneller, ohne Self-Contained/win-x64

dotnet test -c Release       # beide Testprojekte, 1355 Tests (1287 Core + 68 WPF, Stand V2-130)
                             # ⚠ Von hier (Omarchy) laeuft nur GonkNote.Core.Tests:
                             #   dotnet test tests/GonkNote.Core.Tests

# ⛔ NACH JEDEM PUSH: WAS SAGT DIE CI? Ein gruener lokaler Lauf sagt darueber nichts --
#    zwischen dem 2026-09-01 und dem 2026-09-05 war sie neun Laeufe lang rot, und keine der
#    Runden dazwischen hat es bemerkt (§4.101). Der Aufruf braucht KEINE Anmeldung.
curl -s "https://api.github.com/repos/GONKstupid/GonkNote/actions/runs?per_page=3&branch=main" |
  python -c "import json,sys; [print(r['name'], r['status'], r['conclusion'], r['head_sha'][:8]) for r in json.load(sys.stdin)['workflow_runs']]"

# ... und wenn er rot ist: WELCHE Waechter fielen. Die Protokolle brauchen Adminrechte,
#     die Annotationen nicht -- deshalb benennt die CI sie dort selbst (§7).
curl -s "https://api.github.com/repos/GONKstupid/GonkNote/commits/main/check-runs" |
  python -c "import json,sys; [print(c['name'], c['conclusion'], c['id']) for c in json.load(sys.stdin)['check_runs']]"
curl -s "https://api.github.com/repos/GONKstupid/GonkNote/check-runs/<ID>/annotations" |
  python -c "import json,sys; [print('-', a['message'][:300]) for a in json.load(sys.stdin)]"

# Golden-Files bewusst neu setzen (danach den Diff lesen, siehe §4.6)
$env:GONK_SNAPSHOT_UPDATE=1; dotnet test tests\GonkNote.Core.Tests; $env:GONK_SNAPSHOT_UPDATE=$null
$env:GONK_GOLDEN_UPDATE=1;   dotnet test tests\GonkNote.Wpf.Tests;  $env:GONK_GOLDEN_UPDATE=$null

# Testinstanz mit Wegwerf-DB -- Windows-Kopf
.\src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\GonkNote.exe --db "$env:TEMP\x.db"

# ... und der Linux-Kopf, der unter Windows genauso laeuft (seit Phase 3, §4.9/§5b).
# net10.0 ohne Plattform-Anhaengsel und ohne RID: nicht self-contained.
.\src\GonkNote.Avalonia\bin\Release\net10.0\GonkNote.Avalonia.exe --db "$env:TEMP\x.sqlite"

# Echte Daten gefahrlos gegentesten: erst kopieren (Dauerregel 4 -- ohne Nachfragen erlaubt).
# BEIDE Teile, immer: der Blob-Ordner leitet seinen Namen von der Datenbankdatei ab. Ohne ihn
# sind alle Bilder scheinbar weg und man sucht den Fehler an der falschen Stelle.
$d = "$env:TEMP\gonk-echt"; mkdir $d -Force
Copy-Item "$env:APPDATA\GonkNote\gonknote.sqlite" $d -ErrorAction SilentlyContinue
Copy-Item "$env:APPDATA\GonkNote\gonknote.db"     $d -ErrorAction SilentlyContinue   # Altstand, falls noch da
Copy-Item "$env:APPDATA\GonkNote\gonknote.blobs"  $d -Recurse
# Ab hier nur noch die Kopie -- die Dateien unter %APPDATA% werden nie geoeffnet.
.\src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\GonkNote.exe --db "$d\gonknote.sqlite"

# Die Migration selbst pruefen: NUR die Altdatei kopieren und diese uebergeben. Die App
# erkennt sie an der Kopfkennung, legt gonknote.sqlite daneben an und arbeitet damit weiter --
# der Stamm bleibt "gonknote", also passt gonknote.blobs zu beiden (HANDOFF §4.8).
.\src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\GonkNote.exe --db "$d\gonknote.db"

# Danach aufraeumen. Die Kopie enthaelt Schulunterlagen und darf nicht liegen bleiben.
Remove-Item $d -Recurse -Force

# Prüfen, ob nichts WPF-Verseuchtes nach Core gerutscht ist
Select-String -Path src\GonkNote.Core\**\*.cs -Pattern "System\.Windows|System\.Drawing" -List
```

**Toten Code suchen** (Phase 5, Punkt 1 — §4.68). Zwei Wegwerf-Sonden, beide Python, beide in
Minuten wieder geschrieben; **sie liegen bewusst nicht im Repo**, weil ihr Ergebnis eine
Kandidatenliste ist und keine Zusicherung:

1. **Typen und Mitglieder.** Jede `.cs`/`.axaml` unter `src/`, `tests/`, `tools/` **einmal**
   in Bezeichner zerlegen (`re.findall(r'[A-Za-z_]\w*')` in einen `Counter`), dann nur noch
   nachschlagen. Die naive Fassung — je Name einmal über alle Dateien — läuft in die
   Zeitgrenze. Drei Fragen getrennt stellen: Typ aus `src/` in **keiner** anderen Datei ·
   Typ **nur von Tests** gerufen · öffentliches Mitglied nur an seiner Deklaration.
2. **Übersetzungstabellen und mitgelieferte Dateien.** Schlüssel per `\["([\w.]+)"\]\s*=` aus
   beiden `Loc*.cs` ziehen, gegen alles andere halten. Cover und Sticker sind dabei **immer**
   Fehlalarm — sie werden per Verzeichnislauf geladen.

> **⚠ Und dann jeden Treffer von Hand nachsehen.** In §4.68 war **die Mehrheit falsch**:
> Markup-Erweiterungen (`{views:Icon …}` → die Klasse heißt `IconExtension`),
> Erweiterungsmethoden, Schnittstellen-Umsetzungen, alles was über `var` benutzt wird.
> **Und nach dem Löschen ein zweites Mal laufen lassen** — eine Löschung macht die nächste
> Stelle tot (dort: `ResizeBoxAction` → `IBoxElement`).

**Fernsteuern und fotografieren** — seit Phase 2 als Skripte unter `tools\`, statt jedes
Mal neu getippt. **`fenster.ps1` ist keine Anwendung, sondern der gemeinsame Unterbau**
(`Vorn`, `Foto`, `Schwarz`); die anderen binden es per Punkt-Aufruf ein.

> **⛔ Zwei Werkzeugfallen, in §4.82 je ein Durchgang:**
> **(1) `dotnet test --no-build` nach einem *Teilbau*** läuft gegen eine **veraltete Test-DLL**
> — vier Wächter sahen rot aus, obwohl der Code längst zurückgestellt war. *Vor jedem
> Testlauf, dessen Ergebnis zählt, `dotnet build GonkNote.slnx`.*
> **(2) `klick.ps1` erwartet PHYSISCHE Bildschirmpixel.** `fenster.ps1` ruft
> `SetProcessDPIAware()`; ein eigener PowerShell-Aufruf tut das nicht und sieht 1440×900 statt
> 2880×1800. Koordinaten aus einem `foto.ps1`-Bild werden **um den Fensterursprung versetzt,
> nicht halbiert**. Drei Klicks landeten daneben, und es sah aus wie ein wirkungsloser Knopf.
> **Aufgedeckt hat es die Gegenprobe aus §4.78:** als auch ein *funktionierendes* Werkzeug
> nichts tat, war klar, dass die Messung kaputt ist und nicht der Code.

**Zum Vergleichen zweier Köpfe (Phase 5, §4.71) ist `foto.ps1` das richtige Werkzeug** — es
fotografiert ein **schon laufendes** Fenster, so oft man will, und **bricht ab, statt im
Fehlerfall irgendein Bild zu liefern**:

```powershell
.\tools\foto.ps1 -AppPid <pid> -Bild $env:TEMP\a.png           # PrintWindow: garantiert DIESES Fenster
.\tools\foto.ps1 -AppPid <pid> -Bild $env:TEMP\b.png -Menue    # fuer offene Menues und Flyouts
```

> **⛔ Drei Regeln, die §4.71 teuer gelernt hat, und alle drei sind Messfehler und keine
> Befunde gewesen:**
> **(1) Immer nur EIN Kopf sichtbar.** Stehen beide deckungsgleich, fotografieren
> `kette.ps1` und `klick.ps1` per `CopyFromScreen` des Fensterrechtecks — und liefern **den
> anderen Kopf**. *Das ist §4.50 zum zweiten Mal; die Warnung dort nennt `schau.ps1`, der
> Fehler saß in `kette.ps1`.*
> **(2) `-Menue` nur für offene Menues.** Es nimmt die Hülle über **alle Fenster des
> Prozesses** auf und holt das Fenster **nicht** nach vorn, wenn es schon vorn ist — sonst
> schließt das Nachvornholen das Flyout, und das Bild sieht aus wie „kein Kontextmenü".
> **(3) `Process.MainWindowHandle` ist unzuverlässig** — bei offenem Tooltip zeigt es auf
> dessen Fenster (gemessen: ein 314×50-Bild). `foto.ps1` nimmt deshalb das **größte**
> sichtbare Fenster des Prozesses.

Die drei älteren Skripte:

```powershell
# Seit Phase 3 wählt -Kopf, welcher der beiden Köpfe startet.
.\tools\schau.ps1 -Kopf wpf                          # startet mit der Kopie, maximiert, fotografiert
.\tools\schau.ps1 -Kopf avalonia -Konfig Debug -Db "$env:TEMP\gonk-echt\gonknote.sqlite"

.\tools\kette.ps1 -AppPid <pid> -Schritte '#{ESC}','292,45','122,211' -Voll   # Menüpfad
.\tools\klick.ps1 -AppPid <pid> -X 251 -Y 406 -Doppel 1                       # Einzelschritt

# Ziehen (seit §4.13) -- ein Lasso um drei Elemente, danach die Auswahl verschieben:
.\tools\kette.ps1 -AppPid <pid> -Schritte '886,465>1698,465>1698,1481>886,1481>886,470',`
                                          '1292,712>1437,857'
```

Schritte sind `"x,y"`, `"x,y,2"` (Doppelklick), `"x,y,r"` (Rechtsklick), `"#TASTEN"`
(SendKeys) oder **`"x1,y1>x2,y2>…"` (Ziehen über einen Pfad)**. **Koordinaten sind echte
Bildschirmpixel** — `SetProcessDPIAware()` steht in jedem Skript, der Rechner läuft auf 200 %.

**Ohne den Zieh-Schritt ist die Auswahl nicht fernsteuerbar** — ein Klick allein erzeugt
keine. Zwischen den Stützpunkten wird interpoliert, weil die App ihre Lassopunkte aus
Mausbewegungen sammelt (§7, „Ziehen und Untermenüs").

**Beim Avalonia-Kopf zusätzlich `-Voll`** und **jede Kette mit `'#{ESC}'` beginnen**: seine
Menüs sind eigene Fenster und fehlen sonst auf dem Foto, und ein offen gelassenes Menü läuft
im nächsten Aufruf gegen die Wand. Zu allen Fallstricken siehe §7 „Fernsteuern".

**Auf dem CachyOS-Laptop** stattdessen (§4.10; `zeiger` einmalig bauen, siehe §5b):

```bash
tools/linux/schau.sh --db /tmp/gonk-probe.sqlite --bild /tmp/schuss.png
tools/linux/klick.sh --bild /tmp/schuss.png w:120,191 '#Return' 'w:z:800,450>1600,400'
```

**Koordinaten mit `w:` angeben.** Sie sind dann relativ zur linken oberen Ecke des Fensters
— genau so, wie man sie auf einer Fensteraufnahme misst — und `zeiger` rechnet Ursprung
**und** Skalierungsfaktor selbst dazu. Wer echte Bildschirmpixel einsetzt, liegt unter
Wayland um den Faktor 2 daneben (§7 „Fernsteuern unter Wayland").

Der ältere, ausführliche Weg steht im V1-Handoff §7 — inklusive der Stolpersteine
(Umbenennen-Modus nach dem Anlegen, Bild-hoch/-runter greift im `FlowDocumentScrollViewer`
nicht, die IDE reißt den Fokus zurück).

---
