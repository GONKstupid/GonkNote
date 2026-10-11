# Windows-Testliste (Testgerät)

Was der Windows-Laptop jetzt beantworten muss. Seit V2-128 (2026-09-08) hat **niemand den
WPF-Kopf laufen sehen** — alles darunter ist gebaut, aber nie angeschaut. Erst wenn diese Liste
durch ist, geht es mit der Roadmap weiter (Flathub-Quellbau, dann Phase 6).

Einrichtung: `tools/testgeraet-einrichten.md`. **Immer mit Kopie-Datenbank:**
`GonkNote.exe --db C:\Temp\test.db` — nie mit dem echten Bestand.

Befund je Punkt: ✅ / ❌ + ein Satz, was passiert ist (bei ❌ ein Bildschirmfoto).

## 0. Bau und Wächter

- [ ] `dotnet build GonkNote.slnx -c Release -warnaserror` → 0 Fehler / 0 Warnungen
- [ ] `dotnet test GonkNote.slnx -c Release` → alles grün. **WPF-Tests wirklich zählen** —
      HANDOFF §2 führt 68 nur *gerechnet* (seit V2-128 nicht gemessen)
- [ ] Core-Tests auf Windows: Zahl notieren (Linux, 2026-10-11: **1327**)

## 1. Eigene Designs im WPF-Kopf (V2-130, §4.107) — **zuerst, das ist der größte Posten**

Die Quelle *jeder* Farbe im Kopf ist getauscht (`Themes/Light.xaml`/`Dark.xaml` gelöscht,
Wörterbuch kommt aus der Tabelle in Core).

- [ ] Start in Hell: sieht alles aus wie in 1.0.0? (Fenster, Seitenleiste, Ribbon, Menüs, Dialoge)
- [ ] Start in Dunkel: dito
- [ ] **Ansicht → Design**: Untermenü da, Haken an der richtigen Stelle
- [ ] Hell ↔ Dunkel umschalten: **Titelleiste zieht mit** (`TitleBarTheme` an `ThemeChanged`)
- [ ] „Vorlage speichern…" → JSON entsteht; „Design-Ordner öffnen" öffnet den Explorer dort
- [ ] Eigenes Design laden (z. B. Farben aus der Vorlage geändert): alles wechselt in einem Zug,
      auch bei **offenem** Menü
- [ ] Unvollständige JSON (nur 2–3 Farben): Rest kommt aus Hell bzw. Dunkel, kein Absturz
- [ ] Kaputte JSON: Fehlermeldung statt Absturz
- [ ] Neustart: eigenes Design ist wieder da
- [ ] Notizbuch öffnen: Leinwand, Werkzeugleiste und Vorgabetinte tragen die Designfarben

## 2. Tafel-Änderungen im WPF-Kopf (V2-139, V2-140)

- [ ] **Notizzettel mit Muster**: Blanko / Liniert / Kariert / Gepunktet über der Zettelfarbe
- [ ] **Seiten untereinander**: Nachbarseiten sichtbar, Scrollen wechselt die Seite (wie GoodNotes)
- [ ] **Fußleiste** sitzt jetzt in der Einstellungsleiste — alles noch erreichbar?
- [ ] Einstellungsleiste (aufgeräumt): jede Einstellung wirkt noch
- [ ] **Galerie**: Umbenennen zeigt sich live in der Kachel, eigenes Eingabefeld funktioniert
- [ ] **Schriftwahl** der Textfelder kommt auf der Tafel an (gesetzte Schrift wird auch gezeichnet)
- [ ] **Zahlenblock** (Langdruck auf Größenregler) — V2-134 hat dort geändert
- [ ] Texterkennung (OCR) auf einer Seite: läuft noch (V2-140 hat `WhiteboardView.Ocr.cs` berührt)
- [ ] Lasso: auswählen, **in die Auswahl drücken und verschieben** (auf Linux war das kaputt,
      im WPF-Kopf sollte es immer gegangen sein — Gegenprobe)

## 3. Stift und Touch (nur echte Hardware kann das)

- [ ] Stift: Druck und Neigung kommen an, Strich ohne Versatz
- [ ] Handballen beim Schreiben wird abgewiesen
- [ ] Zweite Stift-Taste: was tut sie? (darf vom Linux-Kopf abweichen, §5 Nr. 17 — nur notieren)
- [ ] Textfeld (T) mit **Finger** setzen, mit **Stift** setzen — bleibt stehen?
- [ ] Bildschirmtastatur (Windows/TSF) geht beim Tippen in ein Textfeld auf
- [ ] Formen-Stift, Lineal/Geodreieck mit Stift (Geodreieck auch auf **dunkler** Seite)

## 4. Linux-Kopf unter Windows (Avalonia läuft auch hier)

- [ ] `dotnet run --project src/GonkNote.Avalonia -- --db C:\Temp\test-ava.db` startet
- [ ] **Rechtschreibprüfung** (Phase 5.1): rote Wellen, Sprache in der Statusleiste umschalten,
      Verbesserungen im Rechtsklick-Menü — *auch unter Linux nie am laufenden Programm gesehen*
- [ ] **Grammatik** (Phase 5.2) ohne Server: feste Regeln greifen, Menü zeigt den
      LanguageTool-Hinweis
- [ ] Grammatik **mit** Server: `%USERPROFILE%\LanguageTool\start.cmd`, dann prüfen
- [ ] Zwei-Köpfe-Vergleich: dieselbe Datenbank in WPF und Avalonia öffnen — sieht ein
      Dokument/eine Tafel gleich aus?

## 5. Paket wie ein Nutzer

- [ ] Windows-Zip aus dem Release **v1.0.4** laden, entpacken, aus fremdem Ordner starten
- [ ] Oberfläche zeichnet in der mitgelieferten Schrift, **nicht in Segoe UI** (der `Fonts`-Ordner, §4.103)
- [ ] Über-Dialog (DE und EN): zeigt **1.0.4**, Zeile bricht um statt abgeschnitten
      (*„Die Über-Dialoge sind bei 1.0.4 nicht gegengeprüft"*, HANDOFF §2)
- [ ] Hilfe-Fenster: Abschnitt 14 der Anleitung — dreispaltige Tabelle abgeschnitten?
      (benannter Verdacht aus V2-125, nie geprüft)

## 6. Altlasten aus V1 (Spec, „Entscheidungen — Offen")

Nie auf echter Hardware rückgemeldet — kurz anschauen, nur notieren:

- [ ] OCR-Endfluss, Formen-Stift, Touch (s. o.)
- [ ] Cover-Kennzeichen, Verpixelung ab Seite 12, gedrehte Elemente
- [ ] Wasserzeichen im DOCX-Export (in Word öffnen)

---

**Danach:** Befunde in HANDOFF §2 nachtragen (WPF-Testzahl gemessen statt gerechnet), ❌-Punkte
als Bug-Runde vor alles andere ziehen — dann weiter mit dem Flathub-Quellbau.
