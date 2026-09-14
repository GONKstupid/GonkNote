[← Index: HANDOFF.md](../HANDOFF.md)

## 5e. 🪟 Auftrag für den Windows-Rechner — hier weitermachen

> **⛔ HISTORIE (2026-09-09): Diesen Aufbau gibt es nicht mehr — und den Laptop auch nicht.**
> Entwickelt wird auf dem Lenovo Yoga unter Omarchy, und zwar **als einzigem Gerät** (§0).
> Der CachyOS-Laptop ist weg, der Plan „auf ihn kommt Windows als Gegenprobe" ist hinfällig,
> **Dauerregel 3a ist gestrichen**. Dieser Abschnitt wird **nicht nachgezogen**: er steht nur
> noch als Begründungsspeicher da. Wer hier einen Auftrag liest, liest einen Auftrag an ein
> Gerät, das es nicht mehr gibt.

> **Das ist der Normalfall.** Entwickelt wird unter Windows (§5b); der Laptop ist Messgerät.
> Dieser Abschnitt sagt, **womit die nächste Runde anfängt** — nicht, was zu bauen ist. Was
> gebaut wird, steht in **§6, „Phase 5 — die Ordnung, und warum sie am 2026-08-28
> geändert wurde"**
> samt der Checkliste „Vor dem Öffentlich-Schalten", und zwar nur dort. *(Bis V2-68 stand hier
> „Als Nächstes: das Schreiben" — seit §4.48 fertig; bis V2-89 zeigte es auf „Vorgemerkt:
> Phase 4.5" — seit §4.64 fertig und mit M2 abgeschlossen.)* Ein Plan an zwei Stellen ist
> einer, der an einer davon veraltet.

### 📋 Der Prompt zum Kopieren

```text
Du laeufst auf dem Windows-Rechner. Das Repo liegt in C:\Dev\Zed\gonk-note-V2.

Lies dort Docs/HANDOFF.md (NICHT in der Wurzel, §5 Nr. 21). Lies §5e
("Auftrag fuer den Windows-Rechner") und §6 ("Was in (5) ansteht").

Zieh zuerst den Stand: git pull. Dann bauen und testen, BEVOR du etwas
anfasst. Erwartet sind 0 Fehler, 0 Warnungen und 1313 Tests
(1244 Core + 69 WPF) -- stimmt eine der Zahlen nicht, IST DAS DER ERSTE
BEFUND. Der letzte Eintrag in git log muss V2-125 sein.

⛔ UND DANN: WAS SAGT DIE CI? Ein gruener lokaler Lauf sagt darueber
nichts -- sie war neun Laeufe lang rot, ohne dass es jemand bemerkt hat
(§4.101). Der Aufruf steht in §8 und braucht keine Anmeldung.

⛔ PHASE 5, SCHRITTE (1) BIS (4) SIND ZU. Dem Linux-Kopf fehlt kein
Werkzeug mehr, die Oberflaeche ist geprueft, Flatpak und AppImage sind
gebaut und gestartet (§4.96), und der VOLLSTAENDIGE PRUEFLAUF ist
gelaufen (§4.99). FANG NICHTS DAVON NOCH EINMAL AN.

▶ DRAN IST (5): VEROEFFENTLICHEN. Die Reihenfolge ist nicht beliebig:

 1. ✅ ERLEDIGT AM 2026-09-04 (V2-124, §4.100): DER ZWEITE BAU IST
    GELAUFEN. Der Laptop hat BEIDE Pakete neu gebaut, installiert und
    gestartet, nachdem §4.97-§4.99 darin sind -- DAS IST DER BAU, DER
    HINAUSGEHT. Kette dabei gruen: 0/0 und 1239/1239.
    ⚠ ZWEI POSTEN KOMMEN VON DORT ZURUECK UND GEHOEREN HIERHER:
      (a) DIE VERSION STEHT UEBERALL AUF 0.3.0 -- Directory.Build.props,
          flatpak info und der einzige <release>-Eintrag der
          metainfo.xml, der sich selbst "packaging trial, not a
          release" nennt. §5 Nr. 23/24 verlangt 1.0.0.
      (b) DER LAPTOP KONNTE NICHT KLICKEN (ydotoold scheitert an
          /dev/uinput, sudo braucht ein Passwort, der Skill
          sudopasswot fehlte). Wer ihm den naechsten Auftrag
          schreibt, klaert das zuerst -- die zwei Auswege stehen
          in §5d.

 2. READMEs ueberarbeiten. ⚠ EIN GUTER TEIL IST SCHON GETAN: §4.99 hat
    beide READMEs und beide Anleitungen auf den Stand von M2 gezogen,
    samt der VIER Einschraenkungen, die wirklich gelten
    (Rechtschreibpruefung, zusammengesetzte Zeichen, Lineal,
    Bestandsuebernahme) und der Gegenrichtung (keine Seitenzahlen im
    WPF-Editor). OFFEN: Screenshots beider Koepfe, die drei
    Installationswege, MIT-Text, Verweis auf THIRD-PARTY-NOTICES.md.
    ⚠ Wer ein Bildschirmfoto einsetzt, sehe es im Hilfe-Fenster nach:
    ein ![…] wird dort als ERSATZTEXT gezeigt und nicht als Bild, und
    das ist Absicht (§4.99, Begruendung steht im Code).

 3. GitHub Pages, Releases mit Artefakten, Repo-Beiwerk (§6).

 4. Docs/HANDOFF.md einmal ganz auf Privates durchsehen (§5 Nr. 21),
    Repo auf public, Version 1.0.0 (§5 Nr. 23/24).

⛔ WAS OFFEN BLEIBT UND ENTSCHIEDEN IST -- nicht wieder aufmachen:
 - Menue-Aufklapppunkt im WPF-Kopf: DREI Anlaeufe gescheitert (§4.92).
   Im Prueflauf gut zu sehen: das Menue faellt links aus dem Fenster.
   Der einzige noch nicht versuchte Weg: ein eigenes Template nur fuer
   Role=TopLevelHeader.
 - Ed.Object.Behind/.Front (§4.89), Schnelltabellen (§4.91), Lineal im
   Linux-Kopf (§4.92 -- GESTRICHEN und im README benannt),
   Rechtschreibpruefung unter Linux (§5 Nr. 22, Phase 5.1).
 - Die Tabelle IN einer Zelle wird nicht gesetzt. Das EINFUEGEN ist
   gesperrt (§4.99, Nutzer-Entscheidung), die Luecke bleibt benannt.
 - Ungenutzte Palettenfarben und fremde Zeichnungen ueberstehen DOCX
   nicht (§4.21). Beide haben seit §4.99 einen Waechter.
 - "Seitenumbrueche anzeigen" und "Inhaltsverzeichnis aktualisieren"
   fehlen im Linux-Kopf ZU RECHT (§4.94). NICHT BAUEN.

⛔ DIE REGELN DIESER PHASE STEHEN IN §7 -- lies dort "Neu aus Phase 5".
Die vier wichtigsten, damit du sie nicht erst suchst:
 - EIN GRUENER BAU BEWEIST AN EINER OBERFLAECHE FAST NICHTS. Der
   Prueflauf von (4) hat FUENF tote Verweise gefunden, die alle
   1270 Waechter nicht sehen konnten.
 - EINE ABGELAUFENE BEGRUENDUNG LIEST SICH WIE EINE GUELTIGE (§4.99).
   Wer einen Kommentar findet, der eine Pruefung erspart, misst nach.
 - EIN TEST, DER SICH EINER LUECKE ANPASST, HAELT SIE NICHT FEST --
   ER VERDECKT SIE (§4.99, die Palettenluecke).
 - EINE ABGEHAKTE VERMUTUNG IST TEURER ALS EINE UNGEPRUEFTE (§4.95).

WIE GEMESSEN WIRD: beide Koepfe unter Windows an DERSELBEN frisch
gezogenen DB-Kopie (--db), IMMER NUR EINER SICHTBAR (§4.50),
fotografiert mit tools/foto.ps1, angefangen mit einer LEEREN Flaeche
(§4.56). Die Kopie danach LOESCHEN -- sie enthaelt Schulunterlagen.
Die Werkzeugfallen stehen in §7 unter "Neu aus Phase 5" und in §8.

Arbeite auf Deutsch, halte Docs/HANDOFF.md nach, und sag mir am Ende,
ob der Laptop dran ist.
```

### ✅ Abgearbeitet — die sieben Fragen, mit denen Schritt ①c anfing (V2-109 bis V2-115)

> **▶ Alle sieben sind beantwortet** (§4.86 bis §4.92), **gebaut, gemessen oder ausdrücklich
> gestrichen.** Die Tabelle bleibt stehen, damit nachvollziehbar ist, **was entschieden wurde
> und warum** — nicht als offene Aufgabe.
>
> **⛔ Zwei der Fragen standen auf einer falschen Prämisse**, und beides hat erst die Messung
> gezeigt: `TdFormatEdit.Gemeinsam` liefert nicht die Abweichung, sondern das **wirksame**
> Format (Frage 1), und das Violett an der Wurzel war keine eigene Runde, sondern **sieben
> Schlüssel und ein Setzer** (Frage 6). *Eine Frage, die auf einer Vermutung steht, bekommt
> eine Antwort auf die falsche Frage.*

| # | Frage | Was daran hängt |
|---|---|---|
| **1** | ✅ **Entschieden am 2026-09-01: das wirksame Format aufnehmen, aber minimal schreiben** | ⛔ **Die Frage stand auf einer falschen Prämisse** (§4.86): `TdFormatEdit.Gemeinsam` liefert **nicht** die Abweichung, sondern das **wirksame** Format — eine Methode für die gemeinsame *Abweichung* gibt es in Core gar nicht. Damit gab es eine dritte Antwort: Quelle wirksam auflösen, am Ziel aber nur die Eigenschaften in die Abweichung schreiben, die vom Ziel-Absatz **nicht ohnehin** kommen. Sieht aus wie „wirksam“, verhält sich wie „Abweichung“ — und brennt nichts ein |
| **2** | ✅ **Erledigt (§4.90 Runde A, §4.91 Runde B)** | **Geschnitten nach Nutzen statt nach Schicht.** Runde A: Rahmen, Füllung, Kopfzeile, Zellabstand, Spaltenbreite, verbinden und teilen. Runde B: Tabelle teilen, sortieren (Datum/Zahl/Text), Formel, Tabelle ↔ Text. **+61 Wächter.** ⚠ Drei benannte Einschränkungen — verbunden wird mit der rechten Nachbarin, gefüllt die Zelle unter der Marke (die Auswahl kennt kein Rechteck aus Zellen), und die Formel schreibt ihr **Ergebnis** und kein Feld (wie drüben) |
| **3** | ✅ **Erledigt (§4.89, V2-112)** | **Zusammen** — Beschriftung und Objekt-Anordnung haben ohne ein Bild im Dokument nichts zu tun. `TdGrafikEdit` + `TdBlockEdit.Infobox` in Core, **+13 Wächter**. Die Symbolauswahl ist mit (5) in §4.88 gekommen. ⚠ **Zwei der neun `Ed.Object.*` sind nicht gebaut:** „vor/hinter den Text“ verlangt eine freigestellte Grafik, und die kennt das Modell nicht — benannt statt als Knopf, der nichts tut |
| **4** | ✅ **Gestrichen und benannt (§4.92, V2-115)** | **Gemessen: es ist eine Zierleiste, kein Werkzeug** — `DrawRuler` im WPF-Kopf hat keinen einzigen Maus-Handler, die Randmarken lassen sich nicht ziehen. Der einzige Posten ohne Rückhalt in Core, und was er leistet, leisten die vier Randfelder im Layout-Reiter **in Zahlen** und änderbar. **Wird in ⑤ als bekannter Unterschied im README genannt** |
| **5** | ✅ **Erledigt (§4.88, V2-111)** | Zusammen mit der **Symbolauswahl** aus Frage (3) gebaut — beides ist dasselbe: ein Raster, aus dem man ein Zeichen klickt. **Beide Vorräte lagen fest verdrahtet im WPF-Kopf** und stehen jetzt als `TdMarkenvorrat` und `TdSonderzeichen` in Core (zum vierten Mal derselbe Fall). **+17 Wächter** — einer davon hat sofort einen Fehler gefunden, den der Kommentar daneben bestritt |
| **6** | ✅ **Erledigt (§4.86, V2-109)** | Nicht „eine eigene Runde“, sondern **sieben Schlüssel und ein Setzer**: `AvaloniaThemeHost.AkzentSetzen` gibt Fluents `Palettes[variante].Accent` die Farbe aus `ThemeColor.Accent`; die sechs Abstufungen rechnet `ColorPaletteResources` selbst. **An der gebundenen Assembly gemessen** (Avalonia 12.1.1). **Und der violette Stummel am Schieber aus §4.74 ist ohne eigenen Handgriff mitgekommen** |
| **7** | ✅ **Erledigt (§4.86, V2-109)** | **Die Messung hat die Frage beantwortet, statt sie zu stellen:** es waren **alle** Kürzel — vierzehn Strg-Kürzel und die ganze Navigation hingen in **einem** Handler an `Skia.KeyDown`. Gebaut ist das Muster der Tafel, ganz: `Tunnel`-Handler an der Ansicht + `FokusHolen()` beim Anhängen, mit einer Wache auf den **Fokus** (nicht auf die Sichtbarkeit — der Editor hat sechs Eingabefelder, nicht eines) |

> **⚠ Und zwei Reste aus ①b hängen mit dran**, beide mit **gescheiterten Versuchen im Code**:
> der **Menü-Aufklapppunkt** im WPF-Kopf (§4.73 — `PlacementTarget` weder per `ElementName`
> noch per `TemplatedParent`; danach öffnet das Menü **gar nicht mehr**) und der **violette
> Stummel am Schieber** im Linux-Kopf (§4.74 — nur eine Vorlage **nur für den Thumb** trägt).
> *Fang bei beiden nicht bei Anlauf eins an.*

### ▶ Aktueller Auftrag — **▶ es liegt keiner an; Phase 5 ist zu** (Stand 2026-09-05, nach Runde V2-125)

> **✅ SCHRITT ⑤ IST GELAUFEN, UND DAMIT PHASE 5** (§4.101, V2-125). **Bau 0/0, 1313 Tests
> (1244 Core + 69 WPF), +5 — und beide CI-Jobs grün.** Das Repo ist öffentlich, die Version steht auf **1.0.0**, die READMEs tragen
> die drei Installationswege und sieben Bildschirmfotos, die Projektseite liegt in `site/`,
> `release.yml` wartet auf einen Tag, und das Beiwerk steht. Die Checkliste in §6 ist Punkt
> für Punkt abgehakt — **einschließlich der zwei Prüfungen, die sich nicht nachholen
> lassen**: `Docs/HANDOFF.md` und die **ganze Git-Historie** sind auf Privates durchgesehen
> und **beide sauber**.
>
> **✅ UND DER BEFUND, DER NICHT AUS DIESER RUNDE STAMMTE, IST ZU: DIE CI WAR SEIT DEM
> 2026-09-03 ROT UND IST WIEDER GRÜN** (§4.101). Neun Läufe lang, beide Jobs, nur der
> Testschritt — **hier nicht reproduzierbar**, auch nicht im frischen Klon, und die
> Protokolle verlangen Adminrechte. **Gelöst, indem die CI es selbst sagt:**
> `::error::`-Annotationen sind ohne Anmeldung lesbar. Es war **ein einziger** Wächter, und
> **kein Testfehler**: `TdTableEdit.AlsDatum` las mit `CultureInfo.CurrentCulture`, auf
> `en-US` wurde aus einer Datumsspalte eine Zahlenspalte. **+5 Wächter mit fest gesetzter
> Kultur.**
>
> **⛔ Was davon bleibt, ist eine Gewohnheit und keine Zeile Code:** Das Netz hing **vier
> Tage** durch, und keine der neun Runden dazwischen hat es bemerkt — jede hat lokal grün
> gemessen. **Zum Ablauf einer Runde gehört ab jetzt ein Blick auf den letzten CI-Lauf**;
> der Aufruf steht in §8.
>
> **▶ ES BLEIBEN DREI HANDGRIFFE, UND SIE GEHÖREN DEM NUTZER — nicht dem nächsten Thread:**
> 1. **`git tag -a v1.0.0 -m "Gonk Note 1.0.0"` und `git push origin v1.0.0`.** Das löst
>    `release.yml` aus und **ist** die Veröffentlichung. Danach ist **M3 erreicht**.
> 2. **Pages-Quelle auf „GitHub Actions" stellen** (Repo-Einstellungen → Pages).
> 3. **Beschreibung und Topics** setzen. `gh` ist hier **nicht angemeldet**.
>
> **▶ Und eine Runde liegt vorgemerkt bereit, falls sie vorgezogen werden soll:** der
> **Flathub-Umbau** (§4.102, §6). Er ist vollständig zugeschnitten — SDK-Erweiterung,
> `nuget-sources.json`, Git-Quelle auf den Tag, `finish-args`, `flathub.json` —, **braucht
> danach aber zwingend den Laptop** zum Bauen und Linten. *Er blockiert nichts.*
>
> **⛔ WER ALS NÄCHSTES ETWAS ANFÄNGT, FÄNGT PHASE 5.1 AN** — die Rechtschreibprüfung im
> Linux-Kopf (§5 Nr. 22, das eine benannte Loch in M2), danach Phase 6 (iPadOS). **Aber erst,
> wenn der Tag steht:** Ein Stand, der nach dem Release weitergebaut wird, ist nicht mehr der,
> den das Release enthält.
>
> **⚠ Und drei Dinge sind benannt offen, keines davon eine Runde wert, alle drei
> aufschreibenswert:**
> - **Der dreispaltige-Tabellen-Verdacht** (§4.101): Abschnitt 14 beider Anleitungen trägt
>   eine Tabelle mit drei Spalten und langen Pfaden. Im Hilfe-Fenster wird so etwas
>   **abgeschnitten** — **gesehen ist es dort nicht**, weil `tools/klick.ps1` den Dialog
>   nicht blättern kann. *Wer das Werkzeug einmal um „scrollen" erweitert, misst es in fünf
>   Minuten nach.*
> - **`release.yml` ist nie gelaufen** — es gibt keinen Probelauf, der nichts hinausschickt.
> - **Flathub, das Avalonia-Issue, Portal-Dateidialog und Stift** — alle vier warten auf eine
>   Hand und nicht auf einen Auftrag.

### ▶ Abgearbeitet — **Schritt ④, zweite Hälfte** (Stand 2026-09-04, nach Runde V2-120)

> **✅ ④ IST ZUR HÄLFTE GELAUFEN (§4.97, V2-120).** **Drei der sieben Posten sind zu:**
> **toter Code** (zweiter Lauf, `tests/` und `tools/` — **leer**, und das ist das Ergebnis;
> gelöscht wurden zwei tote Übersetzungstexte), **§7 zu Ende gegengelesen** (fünf Stellen
> richtiggestellt, ein Abschnitt neu) und **das Avalonia-Issue geschrieben**
> (`Docs/avalonia-issue-tote-tasten.md`, **nicht abgesendet — das ist das Konto des Nutzers**).
> **Nebenbei aufgelöst:** die Zählfrage aus §4.96 — **1201 Core + 65 WPF = 1266**, die Zahl
> des Laptops war richtig.
>
> **⛔ Der eigentliche Befund war §7 selbst:** zwischen §4.67 und §4.96 hatte er **keinen
> einzigen neuen Eintrag** — zwanzig Runden. Alles, was Phase 5 gelernt hat, stand nur im
> **Prompt dieses Abschnitts**, und der wird jede Runde überschrieben. *Was länger gilt als
> eine Runde, gehört nicht in den Prompt.* Neu in §7: „Neu aus Phase 5 (§4.68–§4.96)".
>
> **▶ WAS JETZT ANSTEHT, UND DER LETZTE POSTEN IST DER GRÖSSTE:**
> 1. **Aufgelöste Nähte wegräumen** (§4.1) — was steht noch im WPF-Kopf, weil es dort stehen
>    **musste**, und was nur, weil es dort stand?
> 2. **Benannte Lücken schließen oder benennen** — Tabelle *in* einer Zelle (§4.19),
>    ungenutzte Palettenfarben und fremde Zeichnungen (§4.21).
> 3. **⛔ DER VOLLSTÄNDIGE PRÜFLAUF.** Beide Testprojekte, beide Köpfe mit 0 Warnungen, **die
>    vier mitgelieferten Dokumente in beiden Sprachen am laufenden Programm** (Dauerregel 1)
>    und ein Durchgang mit einer **Kopie** der echten Daten (Dauerregel 4). **V2-120 hat das
>    laufende Programm nicht gesehen** — sie hat Doku und Sprachtabellen angefasst und eine
>    Zeile Produktivcode. *Das entbindet den Prüflauf nicht, es macht ihn fällig.*
>
> **✅ Die zwei Entscheidungen sind gefallen** (2026-09-04, §4.98): **Nr. 29 → (b)** — das
> AppImage bringt seine eigene Texterkennung mit; **Nr. 30 → (a)** — die zwei Verlaufsstapel
> bleiben getrennt, und der Grund steht jetzt in §7.
>
> **▶ Damit trägt der Laptop wieder einen Auftrag** (§5d): das AppImage bauen und **mit
> verstecktem System-Tesseract** messen. **Er blockiert den Rest von ④ nicht** — beides läuft
> nebeneinander.
>
> **⛔ Danach wird das Flatpak ein zweites Mal gebaut — und erst dieser Bau geht hinaus** (§6).

### ▶ Abgearbeitet — **Schritt ④, erste Hälfte** (Stand 2026-09-04, nach Runde V2-119)

> **✅ ③ IST GELAUFEN — auf dem Laptop, 2026-09-04 (§4.96).** Flatpak **und** AppImage sind
> gebaut, installiert und gestartet; beide sind auf dem Schirm **pixelgleich** zu einem
> gewöhnlichen `dotnet publish`-Lauf (0 abweichende Pixel). **Die Kernfrage ist beantwortet:
> die Texterkennung trägt IN DER SANDBOX**, zeichengenau, gegen das mitgebaute `/app/lib`.
> Neu im Repo: **`packaging/`**.
>
> **⛔ Drei Funde, und der erste hat den ersten Start gekostet:** `--socket=fallback-x11`
> lässt den Kopf in einer Wayland-Sitzung mit „XOpenDisplay failed" sterben (Avalonia 12 hat
> nur den X11-Rücken); der CMake-Bau von Tesseract heißt `libtesseract.so.5.5` und **nicht**
> `.so.5`; und `dotnet publish -r linux-x64` schleppt **12 MB Windows-DLLs** mit. Alle drei
> in `packaging/` behoben, **keine Zeile Produktivcode angefasst**.
>
> **⚠ Zwei Fragen blieben offen, und keine davon aus Nachlässigkeit** (§4.96): der
> **Dateidialog** in der Sandbox (das Portal antwortet, der Dialog selbst braucht einen
> Klick — `ydotool` verlangt `sudo`, XTEST ist unter GNOME 50 unbrauchbar) und der **Stift**
> (von einem Skript aus grundsätzlich nicht messbar, §4.62). **Beides wartet auf eine Hand
> am Gerät, nicht auf einen Auftrag.**
>
> **▶ Damit ist ④ an der Reihe, und das ist wieder Windows-Arbeit** — die Vorratsliste steht
> in §6, „Was in ④ ansteht": toter Code im Testprojekt und in `tools/`, §7 zu Ende
> gegenlesen, das Avalonia-Issue schreiben, die benannten Lücken schließen oder benennen,
> und **dann der ganze Prüflauf**. **⛔ Und danach wird das Flatpak ein zweites Mal gebaut —
> erst dieser Bau geht hinaus** (§6).
>
> **▶ Eine Entscheidung wartet: §5 „Noch offen" 29** — soll das AppImage seine eigene
> Texterkennung mitbringen? Die Empfehlung ist **(a) so lassen und benennen**.
>
> **⛔ Nebenbei gemessen und noch nicht aufgelöst:** der Laptop zählt **1201** Core-Wächter,
> §2 und §5e sagen **1194**. **Nachzählen gehört nach Windows.**
> — ✅ **Nachgezählt in V2-120 (§4.97): 1201 + 65 = 1266, die Zahl des Laptops war richtig.**

> **✅ ①a, ①b, ①c, ② und die Reparaturrunde sind gelaufen.** Alle fünf Flächen vermessen
> (§4.71, §4.74, §4.75), angeglichen (§4.72–§4.75), die fehlenden Werkzeuge gebaut
> (§4.77–§4.92), die Rückmeldung gefahren (§4.93, §4.94) und ihre vier offenen Punkte
> repariert (§4.95).
>
> **✅ Das Design-Konzept gilt seit §4.94 für beide Köpfe** — Nutzer-Auftrag vom 2026-09-03.
> `Docs/Design-Konzept-Text-Editor.md` sagt das jetzt in einem Kasten ganz oben: **wer eine
> Zone dort ändert, ändert sie in beiden Köpfen.**
>
> **▶ Damit ist ③ an der Reihe, und ③ braucht den Laptop** (§5d): Flatpak/Flathub als
> Hauptweg, AppImage als zweiter. **⛔ ③ ist eine Erprobung und KEINE Auslieferung** — das
> Flatpak wird nach ④ noch einmal gebaut, und erst dieser zweite Bau geht hinaus (§6).
>
> **Was der Windows-Rechner bis dahin tun kann**, wenn ③ warten muss: die Vorratsliste aus
> §6 („Was in ④ ansteht") — toter Code im Testprojekt und in `tools/`, §7 zu Ende gegenlesen,
> das Avalonia-Issue schreiben.
>
> **▶ Die Ordnung der Phase steht in §6, „Die Schritte“, und nur dort.**

#### ✅ Was in ①c gebaut wurde — sieben Runden

*Die Liste kam aus §4.71 und §4.75 und war an fünf Stellen falsch — viermal versprach sie
Arbeit, die längst dastand (§4.77, §4.81, §4.82), und dreimal verschwieg sie welche (§4.82,
§4.86). **Zwei der drei „verschwiegenen“ aus §4.86 waren dann selbst Fehlalarm** (§4.89).
Was hier steht, ist der Stand nach allen Messungen.*

| Runde | Was dazukam | In Core |
|---|---|---|
| **§4.77** | Tafel-Export | `Core/Rendering/WbExport.cs`, +12 W. |
| **§4.78** | Formen-Stift | `Core/Editing/WbFormen.cs`, +23 W. |
| **§4.80** | Suchen & Ersetzen | `Core/Text/TdSuche.cs`, +20 W. |
| **§4.81** | Cover-Werkzeug | `Core/Services/CoverLibrary.cs`, +8 W. |
| **§4.82/§4.83** | Diagramm samt Ändern | `Core/Text/TdChartEingabe.cs`, +29 W. |
| **§4.84** | Format löschen, Undo/Redo-Knöpfe | `TdCharFormat.Zuruecksetzen`, +3 W. |
| **§4.85** | Navigator | `TdToc.Eintraege` war schon da |
| **§4.86** | Wurzelfärbung + der ganze Tastenweg | `AvaloniaThemeHost.AkzentSetzen` |
| **§4.87** | Formatpinsel | `TdFormatEdit.Uebertragen`, +7 W. |
| **§4.88** | Markenauswahl + Sonderzeichen | `TdMarkenvorrat`, `TdSonderzeichen`, +17 W. |
| **§4.89** | Bild, Infobox, Beschriftung, Objekt-Anordnung, Wasserzeichen | `TdGrafikEdit`, `TdBlockEdit.Infobox`, +13 W. |
| **§4.90** | Tabellenentwurf, Runde A | `TdTableEntwurf.cs`, +25 W. |
| **§4.91** | Tabellenentwurf, Runde B | `TdTabellenformel`, `TdTableUmbau.cs`, +36 W. |
| **§4.92** | Lineal gestrichen, Menü-Aufklapppunkt neu vermessen | — |

#### ✅ Die alte Aufgabenliste — vollständig abgearbeitet

*Alles im **Linux-Kopf**, sofern nicht anders vermerkt. Die Liste kommt aus §4.71 und §4.75.*

> **⛔ Diese Liste stammt aus §4.71/§4.75 und war an drei Stellen falsch. §4.77 hat sie
> nachgemessen und richtiggestellt** — *ein Auftrag ist keine Messung, auch wenn er im
> HANDOFF steht* (§4.56, drittes Mal). Was hier steht, ist der Stand **nach** dieser Messung.

| Gegenstand | Umfang |
|---|---|
| ~~**Tafel-Export**~~ | ✅ **Erledigt (§4.77).** Er fehlte dem Linux-Kopf **ganz** — `ExportBoard` warf. Liegt jetzt als `Core/Rendering/WbExport.cs` in Core und läuft in beiden Köpfen, samt Export-Gruppe in der Einstellungsleiste |
| ~~**Einstellungsleiste der Tafel**~~ | ✅ **Vollständig (§4.77 Export, §4.81 Cover).** Sie hat jetzt **alle** Abschnitte des WPF-Kopfs. ⚠ Offen bleibt allein die **Form**: `Expander` drüben gegen `StackPanel` hier, und die Überschrift („Einstellungen" gegen „Seite") — das ist ①b und keine fehlende Funktion |
| ~~**Cover-Werkzeug**~~ | ✅ **Erledigt (§4.81).** `Core/Services/CoverLibrary.cs` + Abschnitt in der Einstellungsleiste, **+8 Wächter** — und **die Cover-Vorlagen sind im selben Commit ins Avalonia-csproj gekommen**, die Warnung von §4.60 hat getragen. **Die Einstellungsleiste hat damit alle Abschnitte des WPF-Kopfs** |
| ~~**Formen-Stift** (`Tool.SmoothPen`)~~ | ✅ **Erledigt (§4.78).** Die Geometrie liegt als `Core/Editing/WbFormen.cs` in Core, der Knopf steht in der Leiste, **+23 Wächter** — vorher war sie durch **keinen einzigen** gedeckt. **⛔ Und dabei sind zwei Fehler aufgefallen, die nicht repariert sind — sie stehen unten** |
| ~~**Suchen & Ersetzen**~~ | ✅ **Erledigt (§4.80).** `Core/Text/TdSuche.cs` gegen das Modell — **nicht umgezogen**: der WPF-Weg steht auf `TextPointer` und ist eine echte Windows-Schranke. **+20 Wächter**, und der Linux-Kopf findet dabei mehr als der WPF-Kopf (Treffer über Formatgrenzen) |
| ~~**Tabellenentwurf**~~ | ✅ **Erledigt** (§4.90 Runde A, §4.91 Runde B) |
| ~~**Zellen verbinden/teilen, Tabelle teilen/sortieren/Formel/in Text**~~ | ✅ **Alles erledigt** (§4.90, §4.91). ⚠ Nicht gebaut und benannt: die **Schnelltabellen** (`Ed.Table.Quick.*`) — zwei fest verdrahtete Vorlagen, die zu einer Vorlagensammlung gehören, die es hier noch nicht gibt |
| ~~**Formatpinsel**~~, ~~Formatierung löschen~~ | ✅ **Beide erledigt** (§4.84 Löschen, §4.87 Pinsel). Der Pinsel überträgt das **wirksame** Format, schreibt aber nur, was von der Unterlage des Ziels abweicht — `TdFormatEdit.Uebertragen` in Core, **+7 Wächter**. ⛔ Die Frage in §5e stand auf einer falschen Prämisse: `Gemeinsam` liefert nicht die Abweichung, sondern das Wirksame |
| ~~**Stil-, Aufzählungs- und Nummerierungsgalerie**~~ | ✅ **Vollständig zu** (§4.82 nachgemessen, §4.88 gebaut). Die Absatzvorlagen-Klappliste und beide Listenschalter gab es längst; die **Markenauswahl** (`Ed.List.BulletPick`/`NumberPick`) ist in §4.88 dazugekommen, gespeist aus `TdMarkenvorrat` in Core |
| ~~**Bild, Infobox, Symbolauswahl**~~ | ✅ **Erledigt** (§4.88 Symbolauswahl, §4.89 Bild und Infobox) |
| ~~**Diagramm**~~ | ✅ **Erledigt (§4.82, §4.83).** Ein bestehendes Diagramm lässt sich per **Doppelklick ändern** — in beiden Köpfen. `Core/Text/TdChartEingabe.cs` + `TdRenderer.DiagrammPng`, **+29 Wächter** — in **beiden** Köpfen, Tafel **und** Editor. **`ChartDialog` 435 → 235 Zeilen**: er rechnete die sieben Arten selbst, die Core seit §4.25 kann, und lieferte eine **Bitmap** ab. Beide Köpfe legen jetzt ein `TdChart` ins Dokument — **die Zahlen bleiben** |
| ~~**Navigator**~~ | ✅ **Erledigt (§4.85).** `TdToc.Eintraege` lieferte die Liste bereits fertig — gebaut werden musste **nur die Anzeige**. Der WPF-Kopf sammelt seine Überschriften dagegen selbst (`CollectHeadings`), weil sein Editor kein Modell kennt |
| ~~**Objekt-Anordnung und Beschriftung**~~ | ✅ **Erledigt (§4.89).** Sieben der neun `Ed.Object.*`-Schlüssel und `Ed.Caption`. ⚠ **Zwei sind benannt und nicht gebaut** — „vor/hinter den Text“ braucht eine freigestellte Grafik, die das Modell nicht kennt |
| ~~**Wortzahl**~~ | ⛔ **Fehlt nicht** — der Linux-Editor zeigt „Wörter · Zeichen" seit jeher (`Ed.Status.Counts.Format`, in der Statusleiste). **In §4.81 nachgemessen**; die Zeile stand hier zu Unrecht (§4.56, zweites Mal nach §4.77) |
| ~~**Rückgängig/Wiederholen im Editor**~~ | ✅ **Erledigt (§4.84).** Die Funktion gab es längst über Strg+Z/Y; es fehlte allein der Griff für den, der nicht mit Tasten arbeitet |
| ~~**Lineal im Editor**~~ | ✅ **Bewusst gestrichen** (§4.92) — eine Zierleiste ohne Maus-Handler; die vier Randfelder im Layout-Reiter sagen dasselbe in Zahlen. Wird in ⑤ im README benannt |

> **✅ Der „größte sichtbare Posten" war keiner.** Die Bedienflächen für Formen (§4.53), Text
> und Notizzettel (§4.55) und Sticker (§4.56) **standen bereits im XAML** — gezählt worden war
> die *Form* (Expander gegen StackPanel), nicht der Inhalt. Was dahinter wirklich fehlte, war
> **der Export**, und zwar nicht als Bedienfläche, sondern als **Funktion** (§4.77). *Eine
> falsche Zählung kostet nicht die Runde, die sie schreibt, sondern die, die ihr glaubt.*

> **✅ Und die Falle aus §4.60 ist nicht zugeschnappt.** Die Warnung im Avalonia-csproj lautete:
> *„sobald das Cover-Werkzeug kommt, müssen die Vorlagen im selben Zug hier mit hinein — ein
> dritter Fall wäre kein Fall mehr, sondern ein Muster."* **Sie hat getragen:** die 62
> Vorlagen sind in §4.81 **im selben Commit** mitgekommen. *Nicht die Warnung war das
> Verdienst, sondern ihr Ort — sie stand dort, wo gebaut wird, und nannte ihren eigenen
> Ablaufzeitpunkt.*

#### ⚠ Was aus ①b offen blieb — **einer von zweien ist zu**

| | |
|---|---|
| ⛔ **Menü-Aufklapppunkt (WPF)** — **offen, drei Anläufe** | **▶ Neu vermessen in §4.92, und die Messung kippt die alte Beschreibung:** Es klappt nicht "am linken Rand der Leiste" auf, sondern **außerhalb des Fensters**, rund 170 px links vom Rand — und **„Datei“ und „Ansicht“ klappen an *derselben* Stelle auf.** Damit ist das Aufklappziel für beide **dasselbe Element**, also weder das `MenuItem` noch sein Grid. **Gescheitert:** `PlacementTarget` per `ElementName` und per `TemplatedParent` (§4.73, danach öffnete das Menü gar nicht mehr) und der Popup aus dem Spaltengitter heraus (§4.92, ohne jede Wirkung). **▶ Der einzige Weg, der zur Messung passt und den noch niemand versucht hat: ein eigenes Template nur für `Role=TopLevelHeader`** — WPF benutzt dafür normalerweise drei Vorlagen, hier dient **eine** für alle drei Rollen |
| ~~**Violetter Stummel am Schieber (Linux)**~~ | ✅ **Zu — und ohne eigenen Handgriff** (§4.86). Drei Anläufe in §4.74 hatten daran gehangen, den **Namen** des malenden Elements zu finden; die Wurzelfärbung braucht ihn nicht. *Wer die Wurzel färbt, muss den Namen des Blattes nicht kennen.* |
| **Auswahlpunkt: Gestalt (beide)** | ✅ **Die Farbe ist zu** (§4.77, `CheckOuterEllipse`/`CheckGlyph`, an 12.1.1 nachgemessen). **Offen bleibt die Gestalt:** der WPF-Kopf lässt seine `RadioButton` unangetastet und bekommt die Systemdarstellung. Gleicher **Bezug**, nicht gleiche **Form** |
| **Einstellungsleiste: Expander gegen StackPanel** | Drüben klappbare Gruppen mit blauen Überschriften und dem Titel „Einstellungen", hier feste Abschnitte mit weißen Überschriften und dem Titel „Seite" (§4.77, beide Köpfe nebeneinander gesehen) |

#### ✅ Zwei Punkte aus §4.80 — **beide erledigt (§4.86)**

*Sie standen bis V2-108 als Fragen an den Nutzer. Beide sind in einer Runde gefallen, und **beide waren kleiner, als sie hier beschrieben sind** — die Tabelle bleibt stehen, damit nachvollziehbar ist, wie die Schätzung danebenlag.*

| | |
|---|---|
| ~~**Das Violett an der Wurzel statt an den Blättern**~~ ✅ **§4.86** — *nicht "eine eigene Runde", sondern **sieben Schlüssel und ein Setzer**; der Stummel aus §4.74 kam mit* | **Sechs Runden, sechs Stellen** (Baum §4.72, Klappliste §4.73, Schieber §4.74, Reiter, Auswahlpunkt §4.77, Ribbon-Schalter und Eingabefeld §4.80). Fluent lässt sich die Akzentfarbe **an einer Stelle** vorgeben (`FluentTheme.Palettes` / `ColorPaletteResources.Accent`), gespeist aus `ThemeColor.Accent` in Core — das wäre die Antwort auf die **Ursache** statt auf ihre Erscheinungen. **⚠ Es färbt aber jede Fläche auf einmal um und verlangt einen vollen Durchgang durch beide Köpfe**, also eine eigene Runde. *Nicht nebenbei.* |
| ~~**Strg+F wirkt nur mit Fokus auf der Leinwand**~~ ✅ **§4.86** — *die Messung hat die Frage beantwortet, statt sie zu stellen: es waren **alle** vierzehn Kürzel* | Der Tastenweg des Editors hängt am Canvas (`TextDocView.Eingabe.cs`). Wer ein Dokument gerade im Ordnerbaum doppelgeklickt hat, drückt ins Leere; der Ribbon-Knopf geht immer. **Das hat in §4.80 einen Messdurchgang gekostet** — die Leiste blieb zu, und es sah aus, als sei das Kürzel nicht verdrahtet. Betrifft vermutlich **alle** Editor-Kürzel und ist damit größer als es aussieht: erst messen, dann bauen |

#### ✅ Zwei Funde aus §4.78 — beide entschieden und erledigt (§4.79)

*Der Nutzer hat am 2026-08-31 beides mit „ja" entschieden. **Beide sind umgesetzt und in
beiden Köpfen am laufenden Programm gegengeprüft.** Die Tabelle bleibt stehen, damit
nachvollziehbar ist, was geändert wurde — nicht als offene Aufgabe.*

| | |
|---|---|
| ✅ **(a) Lange Gekritzel wurden zusammengefaltet** | Ein Zug aus 120 Punkten, **2.886 lang und 297 weit**, gilt als *geschlossen* und wird zu einem **Streckenzug aus drei Punkten** — der Zug ist weg. **Ursache:** als geschlossen gilt `Sehne < Länge × 0,16`, die Schwelle **wächst also mit der Zuglänge**. Gemeint ist „Anfang und Ende liegen nah beieinander", gemessen wird gegen die Gesamtlänge statt gegen die **Ausdehnung**. **Seit Phase 3 so**, in beiden Köpfen (der Linux-Kopf hat den Fehler in §4.78 mitbekommen). Ein Wächter friert ihn ein; **wer die Schwelle richtigstellt, lässt ihn fallen** |
| ✅ **(b) Zwei Köpfe, zwei Vorgabetinten** | Mit derselben Kachel „auto" schreibt der Linux-Kopf **`#1B2B4B`** (aus `Themes.Light[DefaultInk]` in Core), der WPF-Kopf **`#FF000000`** (fest verdrahtet). **Das ist §5 Nr. 27**, und es betrifft nicht das Aussehen, sondern die **gespeicherten Daten**: derselbe Strich hat je nach Kopf eine andere Farbe im Dokument. Der Kommentar im Linux-Kopf benennt die Regel wörtlich, gegen die der WPF-Kopf verstößt. **Vorlage ist der Linux-Kopf** (§6) — die Reparatur wäre also, den WPF-Kopf auf die Core-Tabelle zu stellen, und sie ändert die Tinte, mit der der Nutzer schreibt |

*Beide stehen als Warnung im Code. **Fang nicht bei Anlauf eins an.***

#### ▶ Die Regel, die sich in jeder Runde bestätigt hat

**Jede Fläche, die noch nicht angefasst ist, hat ihr eigenes Violett.** Fluents
`SystemAccentColor` schlägt überall durch, wo niemand es überschrieben hat — Baum (§4.72),
Klappliste (§4.73), Schieber (§4.74). **§5 Nr. 27:** die Auswahl sieht in **beiden** Köpfen
gleich aus und **folgt dem Theme** — also nie ein fester Farbwert, immer einer aus der
Tabelle in Core.

#### ▶ Wie fotografiert wird — drei Regeln, die ①a teuer gelernt hat

**`tools/fenster.ps1` ist keine Anwendung, sondern der Unterbau.** Genommen wird
**`tools/foto.ps1 -AppPid <pid> -Bild <pfad> [-Menue]`** (§8).

| Regel | Warum |
|---|---|
| **Immer nur ein Kopf sichtbar** | Stehen beide deckungsgleich, fotografiert `kette.ps1`/`klick.ps1` per `CopyFromScreen` **den anderen Kopf**. Das ist §4.50 zum zweiten Mal, und es ist in ①a passiert |
| **Zustandsbilder mit `foto.ps1` ohne `-Menue`** | `PrintWindow`, also garantiert dieses Fenster |
| **Menü- und Flyout-Bilder mit `-Menue`** | Nimmt die Hülle über **alle Fenster des Prozesses** auf und holt das Fenster **nicht** nach vorn, wenn es schon vorn ist — sonst schließt das Nachvornholen das Flyout, und das Bild sieht aus wie „kein Kontextmenü" |

> **Und wenn ein modales Fenster den Bau blockiert:** `WM_CLOSE` an **alle** Fenster des
> Prozesses. **Nie `Stop-Process`** (§4.55).

#### ✅ Schritt ① — der Zuschnitt, nach dem gearbeitet wurde (abgeschlossen)

**Beide Köpfe Fläche für Fläche vergleichen und jeden sichtbaren Unterschied beseitigen.**
Der Zuschnitt, die Vorlage-Regel samt ihrer einen Ausnahme und die Liste der Flächen stehen
in **§6**. Hier steht nur, was diese Runde vor dem ersten Handgriff wissen muss:

| | |
|---|---|
| **Die Vorlage** | **Linux gewinnt bei jedem Unterschied** — **außer beim Editor**, dort ist Windows die Vorlage. **Und §5 Nr. 15 ist eine ausdrückliche Ausnahme davon:** die fehlenden Seitenzahlen des WPF-Editors werden **nicht** nach Linux übernommen. Die Regel betrifft das **Aussehen** des Ribbons, nicht das Rechnen dahinter |
| **Wo gemessen wird** | **Nur unter Windows** (§5 Nr. 25), beide Köpfe an **derselben** frisch gezogenen DB-Kopie (`--db`, Dauerregel 4) |
| **Womit fotografiert wird** | **`tools/fenster.ps1`, nicht `schau.ps1`** — das fotografiert den *Bildschirm* und liefert im Fehlerfall ein fremdes Fenster (§4.50) |
| **Womit angefangen wird** | **Mit einer leeren Fläche.** Zwei Runden sind an Scheinbefunden verloren gegangen, weil noch etwas aus dem vorigen Lauf dalag (§4.56) |
| **Was mitläuft** | **§5 Nr. 14** — die Schriftlisten werden zusammengeführt. Es ist ein sichtbarer Unterschied derselben Sorte |
| **Was herauskommt** | Eine **Befundtabelle je Fläche**: *Element · WPF · Avalonia · Vorlage · Befund*. Sie ist das Arbeitsblatt für ① und die **Vorlage für ②** |

> **⚠ Schon beim Lesen des Codes aufgefallen — und ausdrücklich noch keine Messung:** Im
> Linux-Kopf haben **`ChartDialog`** (389 Zeilen drüben), **`HeaderFooterDialog`**,
> **`TableSizeDialog`**, **`TableSortDialog`**, **`PromptDialog`**, **`FileInsertDialog`**,
> **`TextEditorView.Find.cs`** (Suchen/Ersetzen) und **`WhiteboardView.Covers.cs`**
> (227 Zeilen) **keine eigene Datei**. Ob sie dort **fehlen** oder **inline** liegen, ist
> **nachzusehen und nicht anzunehmen** — *ein Auftrag ist keine Messung, auch wenn er im
> HANDOFF steht* (§4.56, wo genau dieser Fehler eine Runde gekostet hat).

> **⚠ Und die Falle, die in genau dieser Arbeit wartet, weil sie schon fünfmal zugeschlagen
> hat:** In Avalonia entscheidet die **Routing-Strategie**, wer eine Taste zuerst sieht
> (`Tunnel` vor allen Kindern, `Bubble` erst nach dem Control). **Drei der vier Fehler in
> §4.55 und der Fehler in §4.62 hatten diese eine Ursache — und der Bau war jedes Mal grün.**

#### ▶ Was danach kommt — die Kurzform, die volle steht in §6

| | | |
|---|---|---|
| **①c** | **Die fehlenden Werkzeuge** ✅ *erledigt* | **Wo einer nichts hat, wird es dort gebaut** (§5 Nr. 26) — dreizehn Runden, §4.77 bis §4.92. **Die sieben Entscheidungen aus §5e sind alle beantwortet.** ⚠ Was bewusst offen bleibt, steht in §4.92 |
| **②** | **Rückmeldung** ◀ *jetzt dran* | Was der Vergleich übersehen hat und was ① kaputt gemacht hat. **Eine eigene Runde und kein Anhängsel** — wer eine Oberfläche in einem Zug umbaut und nicht noch einmal hinsieht, hat sie nicht geprüft, sondern nur geändert. **⚠ Sieben Runden neue Fläche seit V2-108** (§4.86–§4.92); zwei Beobachtungen liegen schon vor (§4.86) |
| **③** | **Flatpak/AppImage** ✅ *erledigt (§4.96)* | **Auf dem Laptop gebaut und gestartet** (V2-119): beides läuft, die Texterkennung trägt **in der Sandbox**, drei Funde (`fallback-x11`, SOVERSION `5.5`, 12 MB Windows-DLLs). **⛔ Erprobung, keine Auslieferung** — nach ④ wird noch einmal gebaut |
| **④** | **Rest des Aufräumens + vollständiger Prüflauf** | Dazu gehört auch das **Avalonia-Issue** (§5 Nr. 11) |
| **⑤** | **Veröffentlichen** | READMEs, Pages, Releases, Beiwerk, Version **1.0.0** |

#### ⛔ Was der Laptop mitgeschickt hat — es gilt weiter, auch ohne Auftrag

**Die Fernsteuerung des Laptops ist kaputtgegangen, ohne dass jemand etwas geändert hat.**
Unter GNOME 50 hängt XTEST am „Entfernter Bildschirm"-Portal: das erste Ereignis öffnet einen
Dialog, **der eine Wayland-Oberfläche ist und den XTEST selbst nicht bedienen kann** — und
solange er steht, kommt **gar kein** Ereignis an. **Gearbeitet wird dort mit `ydotool`**
(§7, §5d); `zeiger` behält `hervor`/`fenster` (§5 Nr. 20).

> **▶ Und die allgemeine Lehre, die hier hingehört:** *eine Anleitung, die vier Runden lang
> richtig war, ist damit nicht richtig geblieben.* **Werkzeugwissen über fremde Oberflächen
> verfällt; Befunde über den eigenen Code nicht.** *(§4.67 zeigt die Kehrseite: ein Kommentar
> über den **eigenen** Code kann von Anfang an falsch sein und trotzdem zwanzig Runden
> überleben.)*

#### Abgearbeitet — die Fragen, die dem Nutzer gehörten (V2-93)

> **▶ Hier standen bis zum 2026-08-28 zwei Fragen, die den Rest der Phase blockierten.**
> **Beide sind beantwortet** (§5 Nr. 21 und §6): `HANDOFF.md` liegt unter `Docs/`, **kein
> History-Rewrite**, und der Zeitpunkt des Öffentlich-Schaltens ist Schritt ⑤. Dazu fünf
> weitere Entscheidungen in derselben Sitzung (§5 Nr. 11, 14, 15, 22–25).

#### Abgearbeitet — der Wortzwischenraum an der Stückgrenze (V2-90)

> **▶ Der älteste offene Fund des Laptops**, vom 2026-08-11. **Erledigt** (§4.67) — und die
> Ursachenvermutung, die im Punkt stand, ist dabei widerlegt worden.

#### Abgearbeitet — Stück 6, die Texterkennung (V2-87)

> **▶ Die drei Teile aus §4.63**, und es ist ein Zusammenlegen geworden statt einer zweiten
> Umsetzung: `GonkNote.Ocr` mit `TesseractOcrEngine` für **beide** Köpfe. **Erledigt**
> (§4.64). Der Fokus-Fund lief in derselben Runde mit (§4.65).

#### Abgearbeitet — Stück 5 (V2-83, V2-84)

> **▶ Erst die Zusammenlegung** (§4.61), **dann die Bedienung** (§4.62). **Beides erledigt.**

#### Abgearbeitet — Lineal und Geodreieck (V2-81, V2-82)

> **▶ Erst die Zusammenlegung** (§4.59), **dann die Bedienung** (§4.60). **Beides erledigt.**

#### Abgearbeitet — der Import (V2-79, V2-80)

> **▶ Erst die Zusammenlegung** (§4.57), **dann die Bedienung** (§4.58). Getrennt gefahren nach
> dem Muster von §4.54. **Beides erledigt.**

#### Abgearbeitet — die Zusammenlegung für den Import (V2-79)

> **▶ Was Bild-, PDF- und DOCX-Import teilen, zuerst nach Core** — dasselbe Muster wie §4.54.
> **Erledigt** (§4.57).

#### Abgearbeitet — die Sticker (V2-78)

> **▶ Stück 2, dritte Runde.** **Erledigt** (§4.56). Damit ist Stück 2 vollständig.

#### Abgearbeitet — Text und Notizzettel (V2-73 bis V2-77)

> **▶ Beide zusammen, denn sie brauchen dieselbe Naht** — ein Eingabefeld über der
> Zeichenfläche. **Erledigt** (§4.55). Die Zusammenlegung davor steht in §4.54.

#### Abgearbeitet — die Formen (V2-71)

> **▶ Stück 1: Linie, Pfeil, Rechteck, Ellipse, Dreieck** samt Füllung und Deckkraft.
> **Erledigt** (§4.53). Davor die Zwischenrunde **Farbwähler** (§4.52), vorgezogen, weil er
> vier der sechs verbleibenden Stücke blockierte.

#### Abgearbeitet — die Auswahl (V2-69)

> **▶ Drehen und Skalieren im Linux-Kopf**, Stück 0 der Reihenfolge. **Erledigt** (§4.51).
> Die Griff-Geometrie und die Griff-Zeichnung sind dabei nach Core gewandert (`WbHandles`,
> `WbSelectionRenderer`), und `InputInProgress` hat gezeigt, dass ein vergessener Zug keinen
> Fehler wirft, sondern **gar nichts** tut.

#### Abgearbeitet — Phase 4.5 wartete auf ein Ja (V2-68)

> **⚠ Hier war zuerst der Nutzer am Zug und nicht der Rechner.** Die Schätzung 5–8 Wochen war
> **hergeleitet und nicht gemessen**, und §6 nannte die Stücke ohne Reihenfolge. Zu klären
> war: (1) volle Breite oder kleiner zugeschnitten? (2) womit anfangen? **Beides ist am
> 2026-08-22 entschieden** — siehe oben und §6.

#### Abgearbeitet — der Augenschein und das Diagramm (V2-68)

> **⑤ Der Augenschein zu §4.49.** Ein Dokument mit Seitenzahl-Feld und Inhaltsverzeichnis aus
> dem Linux-Kopf, im WPF-Kopf geöffnet — sitzt die Seitenzahl auf der Grundlinie, sieht das
> Verzeichnis aus wie eines? Danach die Runde zurück wie in §4.48. **Erledigt, siehe oben.**
>
> **⑥ Das Diagramm** — die letzte Lücke aus §4.45. `TdRenderer.DiagrammZeichnen` war privat
> und brauchte einen öffentlichen Einstieg; eine Änderung in Core an dem Zeichner, den PDF und
> beide Köpfe benutzen, deshalb eine eigene Runde und mit Golden-File-Blick (§4.26).
> **Erledigt, siehe oben — kein Golden-File hat sich bewegt.**


#### Abgearbeitet — die drei Runden aus V2-63

> **② Der Ladeweg** (§5 „Noch offen" 12). Das Modell geht **direkt** in den `RichTextBox`
> statt über das `XamlPackage`. **Eigene Runde und nicht Teil von Schritt 7** — wer die
> Führung umdreht und gleichzeitig den Ladeweg umbaut, hat bei einem Fehler zwei Verdächtige.
> **Die Warnung, die dabei ernst zu nehmen ist, steht schon im Code:**
> `TextEditorView.AusModell` sagt, dass ein ausgetauschtes `Document` dem `RichTextBox` seine
> Stile und alle Ereignisverdrahtungen mitnähme — es sind also die **Blöcke** umzuhängen und
> nicht das Dokument. **Und zu messen, nicht anzunehmen:** Erbt ein von WPF geteilter Absatz
> den Träger des alten? Das wäre eine **verdoppelte Gliederungsebene**, und sie fiele erst im
> Inhaltsverzeichnis auf.
>
> **③ Schritt 7 — `Rtf` verliert die Führung** (§6). Erst danach, und dann mit der Gegenprobe
> in **beiden** Köpfen an einer Kopie der echten Datenbank (Dauerregel 4).
>
> **Die Lehre aus ①, und sie gilt für ② genauso:** Eine Entscheidung, die auf einer
> **ungeprüften Deutung** des Befunds steht, ist selbst ungeprüft — auch dann, wenn der Befund
> stimmt. Für ② heißt das: **erst messen, was beim Tippen mit einem Träger geschieht**, dann
> bauen.
>
> Der Wortlaut des davor abgearbeiteten Auftrags steht darunter.

### ▶ Abgearbeitet — **Schritt 7. Der Tastenweg ist zu Ende gemessen, es fehlt nur noch eine Entscheidung** (Stand 2026-08-19, nach Runde V2-62)

> **✅ Schritt 1 ist abgeschlossen.** Der Windows-Teil steht in §4.44 (V2-61), **die zwei
> Messungen des Laptops sind am 2026-08-19 nachgekommen** (§4.44 „Was der Laptop gefunden
> hat", V2-62) — **und sie haben die Leitspur umgedreht.**
>
> **Was jetzt gilt:** Der Druck der toten Taste **erreicht den X11-Client** (`keycode 49`,
> 3 mal, mit Loslassen), **ein `keycode 0` steht nie auf der Leitung** — es gibt also **kein
> Phantom aus dem X-Strom**, weder von XWayland noch von einem fremden Client. Das
> Verschwinden sitzt **im Prozess**: `XFilterEvent` verschluckt die beteiligten Drucke
> (gemessen, gegen die Herleitung in §4.44), und **libX11 legt das fertige Zeichen als
> Ereignis mit `keycode = 0` nach** — `XmbLookupString` liefert dort `"ê"`. **Avalonias
> Fehler ist, dass es dieses Ereignis über `LookupKey(keycode)` ausliest** und damit genau
> das Zeichen wegwirft, das darin steckt. **Mit `XMODIFIERS=@im=none` gegengeprüft**, also in
> Avalonias eigener Einstellung.
>
> **Damit ist §5 „Noch offen" 11 aufgeklärt und §5d trägt keinen Auftrag mehr.** Es steht nur
> noch **die Entscheidung des Nutzers** aus, was mit dem benannten Avalonia-Fund geschieht:
> **im Kopf umgehen** (das `keycode = 0`-Ereignis erkennen und den Text über die
> Eingabemethode abholen — klein, liegt aber in `Avalonia.X11`), **melden**
> (AvaloniaUI/Avalonia#18596 ist geschlossen, **dieser Befund ist neu**) oder **als benannten
> Mangel stehen lassen**. **Keine der drei Antworten blockiert Schritt 7** — deshalb ist
> Schritt 7 jetzt dran (§6). Darunter steht der Wortlaut des abgearbeiteten Auftrags.

> **Der Laptop hat am 2026-08-18 abgeliefert** (§4.43, „Was der Laptop gefunden hat", V2-59),
> und der Befund ändert, womit diese Runde anfängt. **§4.43 und der Befundblock darunter sind
> vorher zu lesen** — dort stehen die rohen Zeilen, um die es hier geht.
>
> **Die Lage in drei Sätzen.** Preedit ist gebaut und kommt bei IBus an — das
> Fähigkeitswort ist gemessen (`SetCapabilities uint32 9`). **Die toten Tasten kommen trotzdem
> nicht.** Der Grund liegt **eine Station vor** der Fähigkeit: Für die tote Taste und den
> Buchstaben danach schickt der Kopf **gar keinen Tastendruck** an IBus, nur das Loslassen —
> und dazwischen **einen Aufruf mit `keysym = 0` und `keycode = 0`**, den IBus mit **`true`**
> beantwortet; genau danach verwirft Avalonia das rohe Ereignis **samt Text** (§4.42).

#### ⛔ Drei Dinge, die ausdrücklich NICHT zu tun sind

**Sie stehen zuerst, weil jede von ihnen nach der naheliegende Reaktion auf ein „wirkt nicht"
aussieht — und alle drei wären falsch.**

| | Warum nicht |
|---|---|
| **Preedit nicht noch einmal bauen** | Es **ist** gebaut (§4.43), 20 Wächter, und es **funktioniert**: das Fähigkeitswort kommt an, gemessen am Bus. Es ist nicht falsch gewesen, nur **nicht hinreichend** |
| **`SupportsPreedit` nicht zurückbauen** | Es heilte nichts (die Ursache liegt davor) und **nähme die Vorschau wieder weg**, die der iPadOS-Kopf ohnehin braucht — `Avalonia.iOS` bringt die `IInputPane` mit, für die §4.41 die Naht gebaut hat |
| **Keine `IInputPane` für X11 nachrüsten** | Arbeit am Avalonia-Rücken, nicht an dieser App (§5 „Noch offen" 10). Unter Linux bleibt es beim Hervorholen von Hand |

#### Schritt 1 — **wer setzt den Aufruf mit `keysym = 0` ab?**

**Das ist die ganze Frage dieser Runde.** Sie ist am **zerlegten Rücken** zu beantworten,
genau wie §4.42 es vorgemacht hat — **dafür braucht es keinen Laptop und kein laufendes
Linux**, und das ist der Grund, warum sie hier steht und nicht in §5d:

```powershell
# dieselbe Methode wie §4.42, gegen genau die gebundenen Fassungen (Avalonia 12.1.1)
ilspycmd -o .\zerlegt\ $env:USERPROFILE\.nuget\packages\avalonia.x11\12.1.1\lib\net8.0\Avalonia.X11.dll
ilspycmd -o .\zerlegt\ $env:USERPROFILE\.nuget\packages\avalonia.freedesktop\12.1.1\lib\net8.0\Avalonia.FreeDesktop.dll
```

**Die Kette, die §4.42 schon abgelaufen ist** und an der es jetzt genauer hinzusehen gilt:
`X11Window.ScheduleKeyInput` → `FilterIme` → `ProcessNextImeEvent` → `HandleEventAsync` →
`IBusX11TextInputMethod`. **Die Stelle, die gesucht wird, ist die, an der ein Tastenereignis
ohne Keysym und ohne Keycode an `ProcessKeyEvent` weitergereicht wird.**

**Die schärfste Frage zuerst, weil sie den Fehler einkreist, ohne ihn zu kennen:**

> **Warum kommen die Drucke von `a`, `Entf` und `Umschalt` sauber an — und die der toten
> Taste nicht?** Am Bus ist beides in derselben Minute mitgelesen: gewöhnliche Tasten liefern
> Druck **und** Loslassen mit richtigem Keysym; ab der toten Taste fehlt **jeder** Druck.
> **Der Unterschied zwischen diesen beiden Fällen ist der Fehler.**

**Zwei Verdächtige, beide sind Vermutungen und als solche zu prüfen — nicht zu glauben:**

1. **Die Warteschlange des IME-Wegs** (`ProcessNextImeEvent`, `HandleEventAsync`). Sie
   entkoppelt Tastendruck und Antwort. **Wenn ein Ereignis darin verbraucht oder ersetzt
   wird**, entstünde genau das beobachtete Bild: ein Aufruf ohne Inhalt, gefolgt von einem
   verworfenen Zeichen.
2. **Unsere vier Anschlussstellen aus §4.41** in `TextDocView.Eingabemethode.cs`. **Weniger
   wahrscheinlich** — wir rufen IBus nicht selbst —, **aber nicht auszuschließen**: ein
   `Reset`, ein Fokuswechsel oder ein leer gemeldetes Umfeld zur falschen Zeit kann den Rücken
   dazu bringen, ein leeres Ereignis nachzuschieben. **Zuerst zu prüfen ist der Rücken**, weil
   dort der Aufruf tatsächlich abgesetzt wird.

#### Schritt 2 — **den Keycode-Versatz von 8 mitprüfen**

Am Bus gemessen: Der Kopf meldet **49** und **26**, `gnome-text-editor` meldet **41** und
**18**. Das ist der Abstand **X11-Keycode = evdev-Keycode + 8**; **IBus erwartet evdev**.

**Als alleinige Ursache scheidet es aus** — die IBus-Engine entscheidet bei toten Tasten am
Keysym, und der ist richtig. **Es steht aber in derselben Naht**, es ist billig zu prüfen, und
wenn es fehlt, fehlt es auch für alles andere, was am Keycode hängt. **Nachsehen, ob
`IBusX11TextInputMethod` die 8 abzieht** — und wenn nicht, **es hier nicht heimlich
geradebiegen**, sondern als Befund festhalten: es wäre ein Fehler in Avalonia, wie der
`CapSurroundingText`-Fund aus §4.42.

#### Was mit dem Fund geschieht — drei Fälle, und nur einer ist Arbeit für diese Runde

| Der Aufruf kommt aus … | Dann |
|---|---|
| **unserem Kopfcode** | **beheben, hier, sofort** — und der Laptop misst gegen. Das wäre der beste Ausgang: es wäre unser Fehler und damit unser Hebel |
| **`Avalonia.X11`** | **nicht patchen, nicht umgehen, bevor es benannt ist.** Erst festhalten wie §4.42 es getan hat (Methodenname, Fassung, Zeile), dann entscheiden: Umgehung im Kopf, Meldung an Avalonia, oder als benannter Mangel stehen lassen. **Das ist eine Entscheidung des Nutzers und keine dieser Runde** |
| **weder noch** (die Herleitung trägt nicht) | **Dann ist auch das ein Ergebnis** — festhalten, was ausgeschlossen ist, und den nächsten Griff benennen. **Nichts raten** |

> **Die Grenze dieser Runde ist scharf und ist dieselbe wie in §4.42:** **Unter Windows ist das
> Symptom nicht zu sehen** — es gibt hier weder tote Tasten noch IBus, hier läuft TSF.
> **Was hier entsteht, ist eine Herleitung; ob sie trägt, sagt allein der Laptop.** §4.42 hat
> genau so gearbeitet und lag **zur Hälfte richtig** — das Fähigkeitswort stimmte, die
> Wirkung nicht. **Deshalb: die Erwartung wieder ausdrücklich widerlegbar formulieren**, mit
> Zahlen, gegen die der Laptop messen kann.

#### Danach — Schritt 7, und er ist der eigentliche Zweck

**Erst wenn Schritt 1 abgeschlossen ist** (behoben, oder benannt und begründet zurückgestellt),
geht es weiter mit **Schritt 7: `Rtf` verliert die Führung** (§6). Der Grund für die
Reihenfolge steht unverändert: **die Regression sitzt in derselben Naht, die Schritt 7
benutzt** — wer erst Schritt 7 baut, baut auf einem Eingabepfad weiter, an dem gerade Zeichen
verschwinden. **Ein Fund liegt dafür bereit:** der Weg durch den WPF-Editor setzt den Absatz
auf **Blocksatz** (§4.37, Fund 2).

#### Bevor du anfängst, und bevor du fertig bist

```powershell
cd C:\Dev\Zed\gonk-note-V2
git pull                      # der Laptop hat vorgelegt (V2-59)
dotnet build -c Release       # 0 Fehler, 0 Warnungen
dotnet test -c Release        # 823 Tests (789 Core + 34 WPF)
```

**Und zum Schluss, damit die Runde nicht auf halbem Weg endet:** Was der Laptop als Nächstes
messen soll, **gehört nach §5d, bevor der Nutzer wechselt** (Dauerregel 3a) — mit den Zahlen
aus V2-59 zum Vergleichen: `Halloêá` → **7**, `Hallo äöüß ÄÖÜ` → **14**, ein Absatz von
**427** Zeichen → **427**. **Steht dort nichts, hat der Laptop nichts zu tun**, und der Fund
dieser Runde bleibt ungeprüft liegen.

### ✅ Aufgeräumt ist — die drei Handgriffe sind erledigt (2026-08-11, V2-40)

Sie standen offen, **weil sie auf dem Laptop nicht zu erledigen waren**. Der Befund steht in
**§4.29**; hier nur, was davon bleibt:

| # | Was | Ergebnis |
|---|---|---|
| **1** | `About.Version` im WPF-Dialog | ✅ **Stimmt, beide Sprachen**, am laufenden Programm geprüft. §5 „Noch offen" 5 erledigt |
| **2** | Der Dateidialog des Linux-Kopfs | ✅ **Beide Fragen: ja.** Vorgewähltes Format *und* vorbelegter Dateiname, und der Pfad kommt zurück. PDF und DOCX geschrieben, das DOCX wieder eingelesen. **Der Portal-Fall ist seit dem 2026-08-16 ebenfalls geprüft** (V2-47, von Hand am Laptop): dieselben zwei Ja. §5 „Noch offen" 7 ist damit **ganz zu** |
| **3** | `gonk-note-port-RM.MD` nachziehen | ✅ **Phase 4.5 eingetragen, M2 dorthin verschoben**, Zeitrahmen ergänzt. Die Aufwandszahl (5–8 Wochen) ist ein **Vorschlag** und wartet auf das Ja des Nutzers |

> **Dabei gefunden, und es hat sich mit Schritt 2 aufgelöst wie angekündigt:** Ein **neu
> angelegtes** Textdokument zeigte im Linux-Kopf „stammt aus der Windows-Fassung" und ließ sich
> nicht exportieren (§4.29, §5 „Noch offen" 8). ✅ **Erledigt am 2026-08-12** (§4.32) — an der
> Wurzel, in `DatabaseService.GetText`, und nicht im Kopf.

### Damit gilt jetzt: der Arbeitsplan — §6, „Als Nächstes: das Schreiben"

**Nutzer-Entscheidung 2026-08-11: Weg (a)**, das Schreiben im Linux-Kopf, vor Phase 4.5
(§5 „Noch offen" 2). **Nicht mehr nachfragen** — die Entscheidung ist gefallen.

**✅ Schritt 1 steht** (§4.30): `TdPosition`, `TdSelection` und `TdCursor` in `Core/Text/`. Die
Stelle steht **im Modell**, kanonisch ist die **linke** Schreibweise einer Stückgrenze, ein Feld
ist **einen Schritt breit und kein Zeichen**, und bewegt wird in **ganzen Zeichen**.

**✅ Schritt 2 steht** (§4.32): `TdEdit`, `TdFragment` und `TdChange` daneben. Alles läuft über
**einen** Handgriff (`Ersetzen`), eine Änderung merkt sich **die Blöcke davor und danach**, und
daraus folgt die Regel, die alles Weitere erbt: **Absätze und Stücke werden nie verändert,
sondern ersetzt.** Ein eingefügtes Zeichen erbt das Format **links** davon. §5 „Noch offen" 8
ist mit erledigt.

**✅ Schritt 3 steht** (§4.33): `TdUndo` daneben — **neben** `UndoStack` und nicht darin, mit
drei Gründen belegt. Getippte Zeichen werden zu **einem** Schritt; die Schnitte hängen an der
Gestalt der Änderung und nicht an der Uhr.

**✅ Schritt 4 steht** (§4.34): `TdHit` rechnet in beide Richtungen — Klick → Stelle und
Stelle → Papier —, der Zeichner malt Schreibmarke und Auswahl. Jedes gesetzte Stück trägt jetzt
seine Stelle im Absatz; der in §4.30 versprochene Rückweg war nur die Hälfte.

**✅ Schritt 5 steht** (§4.35): `TextDocView.Eingabe.cs` — **der Linux-Kopf schreibt.** Tippen
über `TextInput`, Auswahl mit Maus und Umschalt, Wort- und Zeilensprünge, Rückgängig. Drei
Rechnungen kamen dafür in Core dazu (`TdHit.Hoch`/`Runter`, `TdHit.Zeilenrand`,
`TdCursor.Wort`), Modell und Verlauf liegen am Register. Der Umbruch nach jeder Änderung ist
**gemessen** und nicht geraten.

**✅ Schritt 5 ist auf dem Laptop gegengeprüft** (§4.35, „Was der Laptop gefunden hat", V2-47):
Umlaute **und** tote Tasten kommen an *(⚠ die toten Tasten seit V2-54 nicht mehr — §5 „Noch
offen" 11, gemessen am 2026-08-18)*, der **Stift setzt die Marke und zieht eine Auswahl**,
getippt wird flüssig — **kein Zeichen verloren, auch nicht in einem Dokument mit 32 Seiten**.
Der Umbruch ist dort **schneller als hier**; die 40-ms-Grenze fällt erst bei ~32 Seiten. **Damit
ist `TextInput` statt `KeyDown` gemessen und nicht mehr vermutet.**

**✅ Schritt 6 steht ganz** (§4.36 und §4.37): **Formate lassen sich setzen** (`TdFormatEdit`,
Formatgruppe im Reiter „Start", Strg+B/I/U), **und die drei Reiter sind da** — „Einfügen"
(Seitenumbruch, Zeilenumbruch, Tabelle, Felder), „Verweise" (Verweis setzen/entfernen,
Inhaltsverzeichnis) und „Tabelle" (Zeile/Spalte einfügen und löschen, Tabelle löschen).
**Und die Warnung aus §5 „Noch offen" 9 steht** (`TdFuehrung`): Bei gefülltem `Rtf` sagt der
Kopf, dass die Windows-Fassung führt und hier Geschriebenes verlorengeht.

**✅ Beide Entscheidungen sind gefallen** (Nutzer, 2026-08-16): §5 Nr. 9 → **(b) warnen, nicht
sperren**; §5 Nr. 10 → **(a) `TextInputMethodClient` umsetzen, aber nach Schritt 6**. **Nicht
mehr nachfragen.**

**✅ Die Sichtprüfung ist erledigt** (§4.37, „Die Sichtprüfung ist nachgeholt"): In beiden Köpfen
an einer Kopie der echten Datenbank durchgespielt — Formatknöpfe, Tabelle samt Marke in der
ersten Zelle, Verweis, und **der Warnstreifen erscheint genau dann, wenn `Rtf` gefüllt ist**.
Der WPF-Editor zeigt die vom Linux-Kopf angelegte Tabelle, das Fett und den Verweis.

**✅ Das Ribbon ist aufgeräumt** (§4.38, Nutzerwunsch): Zoom, Einpassung und Zählung stehen in
der **unteren Leiste**, die Tabellengröße im Flyout, die Word-Hinweise hinter einem **„i"**, der
Reiter „Layout" **stellt** Format und Ausrichtung, rechts gibt es eine **Einstellungsleiste**
(Ränder, Absätze), und die rechte Maustaste öffnet in einer Tabelle deren Menü.

**▶ Dran ist — die Reihenfolge gehört dem Nutzer, vorgeschlagen ist:**

1. ✅ **Gruppe A ist erledigt** (§4.39, 2026-08-17): Aufzählung, Nummerierung,
   Überschriften-Vorlagen und die Schriftgrößenliste stehen — die Vorlagentabelle liegt in Core
   und wird von einem Wächter gegen die des WPF-Kopfs gehalten.
2. ✅ **Gruppe B ist erledigt** (§4.40, 2026-08-17): Schriftart, Schriftfarbe, Hervorhebung,
   Trennlinie und Kopf-/Fußzeile stehen. **Das Wasserzeichen ist dabei nach C gewandert** — es
   ist ein `TdImage` und braucht den Blob-Weg wie jedes Bild; die erste Einschätzung war zu
   optimistisch.
   **⚠ Drei Dinge sind gebaut, aber nicht angeklickt worden:** Trennlinie, Kopf-/Fußzeile und
   die Hervorhebung. Sie stehen als erste Zeile der nächsten Sichtprüfung.
3. ✅ **Schritt 6a ist gebaut und gesehen** (§4.41, 2026-08-18): `TdEingabe` in Core,
   `Eingabeziel` im Kopf — die Fläche meldet sich als Eingabeziel an und beantwortet Marke,
   Umfeld und Auswahl. **Der Laptop hat am selben Tag gemessen** (V2-55): **Die Tastatur
   schreibt jetzt** (von Hand hervorgeholt — in V2-47 war sie taub), **klappt aber nicht von
   selbst auf**, weil `Avalonia.X11` keine `IInputPane` hat. **⚠ Und sie hat die toten Tasten
   gekostet** — siehe Punkt 4.
4. **▶ ZUERST: die Regression aus §5 „Noch offen" 11 — tote Tasten.** **Sie geht vor
   Schritt 7, weil sie in derselben Naht sitzt**, die Schritt 7 benutzt — wer erst Schritt 7
   baut, baut auf einem Eingabepfad weiter, an dem gerade Zeichen verschwinden.
   **Der Stand ist seit dem 2026-08-18 (V2-59, §4.43) ein anderer, als die Punkte 1 bis 3
   erwarten lassen:** (a) **ist gebaut** (Preedit, §4.43), **wird nicht zurückgebaut** — und
   **hat die Regression nicht behoben.** Der Laptop hat es gemessen: `^`+`e` ergibt weiterhin
   nichts, **Zähler 5 statt 7**.
   ✅ **Was die Messung ausgeschlossen hat:** Es liegt **nicht an IBus** (`CommitText` kommt
   beim Nachbarprogramm im selben Augenblick an), und es liegt **nicht am Fähigkeitswort**
   (`SetCapabilities uint32 9` — es kommt an, **§4.42 Punkt 2 ist bestätigt**).
   ⚠ **Wo es klemmt, ist damit eingegrenzt: im Tastenweg, eine Station vor IBus.** Für die
   tote Taste schickt der Kopf **gar keinen Tastendruck**, nur das Loslassen — und dazwischen
   **einen Aufruf mit `keysym = 0` und `keycode = 0`**, den IBus mit **`true`** beantwortet;
   **genau danach verwirft Avalonia das rohe Ereignis samt Text** (§4.42).
   **▶ Der erste Griff ist damit benannt und braucht keinen Laptop:** nachsehen, **wer diesen
   Aufruf absetzt** — eine der vier Anschlussstellen aus §4.41 oder `Avalonia.X11` selbst,
   **am zerlegten Rücken zu lesen wie in §4.42**. **Nebenbei mitprüfen:** die Keycodes stehen
   um **8** daneben (X11 statt evdev).
   **(c) — die Anmeldung zurücknehmen — fällt weiterhin aus**, es machte die
   Bildschirmtastatur wieder taub.
5. **▶ DANACH: Schritt 7: `Rtf` verliert die Führung** — der einzige vollständige Ausweg aus
   §5 Nr. 9 und der eigentliche Zweck des ganzen Wegs. **Dabei liegt ein Fund bereit:** Der Weg
   durch den WPF-Editor setzt den Absatz auf **Blocksatz** (§4.37, Fund 2).
6. **Gruppe C** zusammen mit Phase 4.5 — Bilder, Diagramme, Infoboxen, Symbole, Beschriftungen,
   Zellen verbinden.

> **Zwei Dinge, die dabei *nicht* zu bauen sind, damit niemand sie anfängt:**
> **(1) Keine `IInputPane` für X11 nachrüsten.** Das ist Arbeit am Avalonia-Rücken und nicht
> an dieser App; unter Linux bleibt es beim Hervorholen von Hand, und das gehört in den
> Erste-Schritte-Text statt in den Kopf (§5 „Noch offen" 10).
> **(2) Die `PointerType`-Weiche nicht „reparieren".** Sie ist richtig und unter Windows
> belegt — dass sie unter Linux wirkungslos ist, liegt an der fehlenden `IInputPane` und nicht
> an ihr. **Sie ist unter Linux schlicht nicht prüfbar**, und ein Umbau dort wäre eine Änderung
> ohne Messgerät.

> **⚠ Beim Fernsteuern zu wissen:** Am 2026-08-17 haben die Skripte in `tools\` **zeitweise
> keine Klicks zugestellt** — `schau.ps1` fotografierte richtig, der Zeiger bewegte sich
> nachweislich, `SendInput` meldete Erfolg, und im Kopf kam nichts an (auch ohne Sandbox nicht).
> Später in derselben Sitzung ging es wieder, ohne dass etwas geändert wurde. **Das ist ein
> Werkzeugbefund und keiner der App** (§4.36, §7). Wer eine Sichtprüfung anfängt, macht **einen
> Klick auf „Erscheinungsbild wechseln"** als Erstes — und sagt dem Nutzer Bescheid, wenn der
> nichts tut: Dann ist jede Sichtprüfung Handarbeit, und das gehört gesagt und nicht umgangen.

> **Was die Schritte 1 bis 5 mitgenommen haben und nicht noch einmal zu suchen ist:** die Frage
> nach der Formaterbung (beantwortet: links, wie in Word), §5 „Noch offen" 8 (erledigt), die
> Prüfung, ob `TextChangeAction` passt (nein — §4.32), wem der Verlaufsstapel gehört (§4.33),
> der Rückweg vom Papier ins Modell (§4.34) und **was ein Umbruch wirklich kostet** (§4.35 —
> und das Verzeichnis ist es nicht).

> **✅ Schritt 5 ist auf dem Laptop durch** (§5d, V2-47 vom 2026-08-16) — Umlaute, tote Tasten
> und der Cursor **am Stift** sind beantwortet, Compose nur deshalb nicht, weil auf dem Gerät
> keine eingerichtet ist (und das ist die Antwort, nicht eine Lücke). **Die Bildschirmtastatur
> war damals die Ausnahme** — sie ging nicht auf, und daraus wurde §5 „Noch offen“ 10.
> **▶ Am 2026-08-18 ist auch das gemessen** (V2-55, §4.41): Die Tastatur **schreibt** jetzt,
> wenn man sie von Hand hervorholt — **aufklappen tut sie nicht**, dem X11-Rücken fehlt die
> `IInputPane`. **⚠ Und die toten Tasten aus V2-47 gelten nicht mehr:** sie sind seit V2-54
> weg (§5 „Noch offen" 11). **§5d trägt jetzt keinen Auftrag mehr.**

> **Warum das Schreiben trotz seiner Core-Anteile hierher gehört und nicht auf den Laptop:**
> nicht wegen der Werkzeuge, sondern wegen der **Gegenprobe**. Jede Änderung am Modell muss der
> **WPF-Editor überleben** — und beide Köpfe nebeneinander an derselben Datenbank-Kopie gibt es
> nur hier (§5b). Ein Modell, das der Linux-Kopf sauber schreibt und der WPF-Kopf leer anzeigt,
> ist der teuerste Fehler dieser Art; §4.28 hat ihn einmal knapp verhindert.

### Wie geprüft wird — dieselbe Regel wie in Phase 4

```powershell
cd C:\Dev\Zed\gonk-note-V2
dotnet build -c Release       # 0 Fehler, 0 Warnungen
dotnet test -c Release        # beide Projekte, derzeit 823 Tests
```

**Und danach am laufenden Programm**, mit einer **Kopie** der echten Datenbank (Dauerregel 4,
Befehle in §8) — **in beiden Köpfen**. Beim Schreiben heißt das immer dieselbe Frage: *Was
sieht der andere Kopf, nachdem dieser gespeichert hat?*

### Wann der Laptop wieder dran ist

**Nicht jetzt.** Er hat zuletzt am 2026-08-18 abgeliefert (**V2-59**, §4.43), **§5d trägt
keinen Auftrag**, und auf dem Gerät steht nichts mehr offen, was nur er beantworten könnte —
außer zwei alten Stift-Fragen (§5a „Offen" 2 und 3: eine Xorg-Sitzung als Vergleich und die
Druckschwelle unten) und einer kleinen neuen: ob die von Hand hervorgeholte Tastatur die Marke
**verdeckt**.

**Fällig wird er wieder, sobald am Tastenweg etwas geändert ist** (§5e, „Aktueller Auftrag") —
ob die toten Tasten danach ankommen, ist die Frage, die **nur dort** zu beantworten ist: Unter
Windows läuft TSF und nicht IBus, und die Regression ist hier **nicht zu sehen**.
**Das ist der Musterfall für §5b**, und er ist zweimal teuer gewesen: Der Fehler ist am
2026-08-18 gebaut worden und wäre ohne den Laptop unbemerkt in Schritt 7 hineingelaufen — und
**die Reparatur desselben Tages hat ohne ihn wie eine Lösung ausgesehen**, obwohl sie keine
war (§4.43: das Fähigkeitswort kam an, das Zeichen nicht).

> **Die Lehre, die dabei angefallen ist, gilt über diesen Punkt hinaus:** §4.41 hat aus einer
> Windows-Messung (V2-47) geschlossen, `SupportsPreedit => false` koste für lateinische Schrift
> nichts. **Die Messung stammte von *vor* der Änderung** und konnte den neuen Weg gar nicht
> abdecken. **Wer einen Beleg zitiert, prüft, ob er den geänderten Pfad überhaupt berührt hat.**

**Wenn es so weit ist, gehört der Auftrag nach §5d, bevor der Nutzer wechselt**
(Dauerregel 3a).

> **Zwei Punkte, die der Laptop mitgeschlossen hat und die hier nicht noch einmal zu suchen
> sind:** der **Dateidialog** (§5 „Noch offen" 7 — jetzt in beiden Fassungen geprüft, Win32 wie
> Portal) und das **zweite Stiftgerät** (§5 „Noch offen" 1 — ein **MPP**-Stift, andere Technik
> als der eingebaute EMR-Wacom, **Druck kommt an**). Damit ist „läuft mit jedem Stylus" (§1)
> zum ersten Mal an zwei Stifttechniken belegt und **der einzige Punkt mit echtem Restrisiko
> eingelöst.**

**Vor jedem Laptop-Auftrag die betroffene `.axaml` ansehen** — ob der Linux-Kopf überhaupt
kann, was der Auftrag verlangt. In §4.26 ist genau das schiefgegangen.

> **Was der Laptop zuletzt gefunden hat, ist beim Fernsteuern zu wissen** (§4.28, „Was den
> Laptop dabei aufgehalten hat"): Avalonias Menü-Popups stehen auf **keiner** Fensteraufnahme,
> `Down` zählt die Einträge **ohne** Trenner, einzelne Tastendrücke gehen unter 0,8 s Abstand
> verloren, und der **Portal-Dateidialog** ist unter GNOME-Wayland weder zu fotografieren noch
> zu bedienen. **Das gilt drüben, nicht hier** — unter Windows tun es die Skripte in `tools\`
> wie gewohnt.
>
> **Dazu ein dritter Werkzeugfehler, gefunden am 2026-08-16** (§4.35): `zeiger` tippte
> **Latin-1-Zeichen gar nicht** — `äöüß` erzeugte keinen einzigen Tastendruck, und auf dem
> Schirm sah das aus, als schluckte der Kopf die Umlaute. Behoben. **Die Lehre daraus gilt auch
> hier:** Wenn eine Prüfung am Laptop etwas findet, das nach einem Fehler der App aussieht,
> **erst das Werkzeug ausschließen** — das ist jetzt zweimal hintereinander die Ursache
> gewesen, und beide Male hätte es eine falsche Zeile im HANDOFF gegeben.

---
