[← Index: HANDOFF.md](../HANDOFF.md)

## 7. Fallen

**Alle Fallen aus `gonk-note\HANDOFF.md` §4 gelten unverändert weiter.** Die wichtigsten,
weil sie bei der Portierung direkt zuschlagen:

**Die Werkzeuge unter `tools\` — welches was tut**

- **`schau.ps1` STARTET die App, `klick.ps1 -AppPid` bedient eine LAUFENDE.** Das steht
  im Kopf von `schau.ps1`, ist aber leicht zu überlesen, und der Fehlgriff ist teuer:
  `schau.ps1` kennt **kein** `-AppPid` und **ignoriert ein mitgegebenes stillschweigend**
  (PowerShell bindet es an nichts und meldet nichts). Wer damit „den laufenden Kopf
  fotografieren" will, bekommt einen **zusätzlichen** Prozess — mit der Vorgabe
  `--db %TEMP%\gonk-echt\gonknote.db`, die auf eine **Alt**datenbank zeigt und beim
  Fehlen eine Fehlermeldung wirft. **In V2-88 sind so zwei fremde WPF-Instanzen entstanden**,
  deren Dialoge zunächst wie ein Fehler der eigenen Änderung aussahen (§4.66).
- **Und daraus die allgemeinere Regel:** *ein Werkzeug, das im Fehlerfall irgendetwas tut,
  ist schlimmer als eines, das nichts tut.* Dieselbe Lehre wie in V2-68, als `schau.ps1`
  ein fremdes Fenster fotografierte.

**Die Werkzeuge unter `tools/linux/` — was seit GNOME 50 nicht mehr gilt**

- **⛔ `zeiger` klickt und tippt nicht mehr, und der Fehlschlag sieht aus wie ein Fehler der
  App.** XTEST läuft unter GNOME 50 über das EI-Portal. Das erste Ereignis eines Prozesses
  öffnet **„Entfernter Bildschirm"**, und **bis zur Freigabe kommt kein einziges Ereignis
  irgendwo an** — auch nicht beim XWayland-Client. Ein Klick auf den `Neu`-Knopf blieb
  wirkungslos, während der Dialog offen stand. **Der Dialog selbst ist eine Wayland-Oberfläche
  und mit XTEST nicht bedienbar** — §5d behauptete vier Runden lang das Gegenteil.
  **Die Freigabe wird nicht gespeichert** (der Berechtigungsspeicher des Portals ist leer)
  **und eine Bildschirmsperre nimmt sie zurück.** Gefunden in V2-89 (§4.64).
- **⛔ Und die Sitzung sperrt sich mitten in der Messung selbst aus.** XTEST über XWayland
  setzt GNOMEs Leerlaufzähler **nicht** zurück; bei `idle-delay 300` und `lock-delay 0` ist
  nach fünf Minuten alles schwarz — **und die Portal-Freigabe obendrein weg**. *Eine
  Fernsteuerung, die den Rechner nicht wach hält, sperrt sich selbst aus.*
- **⛔ `ydotool` tippt rohe evdev-Keycodes gegen die physische Belegung.** Auf der deutschen
  Tastatur ist **Keycode 44 das `Y`** — `29:1 44:1` ist **Strg+Y** (*Wiederholen*), nicht
  Strg+Z. **Strg+Z ist Keycode 21.** In V2-89 hat das einen Scheinbefund erzeugt („der
  Notizzettel geht mit Strg+Z nicht weg"), der keiner war. `ydotool type` tippt aus demselben
  Grund `-` als `ß`.
- **⛔ `ydotool mousemove -a` ist nachgebildet und läuft durch die Mausbeschleunigung.** Sein
  virtuelles Gerät hat **keine absoluten Achsen** (`capabilities/abs` ist `0`). Am Anschlag
  `(0,0)` verankern und rechnen (Faktor **0,8133** auf Gerätepixel), `accel-profile` auf
  `flat`. **Und `XQueryPointer` taugt nicht zum Nachmessen:** steht der Zeiger über einer
  Wayland-Oberfläche, liefert XWayland den letzten Wert von vorher — *eine veraltete Zahl, die
  wie eine gültige Messung aussieht*, dieselbe Sorte Falle wie das Foto ohne Menü in V2-55.

**Neu aus §4.103 — was die Installationsprobe gelehrt hat**

- **⛔ Der Ordner `Fonts` gehört neben die Exe, und das steht in keiner Anweisung von
  selbst.** Die Oberflächenschriften sind **lose Dateien** (`AppFonts.Family` baut den Pfad
  aus `AppContext.BaseDirectory`). Fehlt der Ordner, **startet die App und zeichnet in Segoe
  UI** — §4.72 rückwärts, und **nichts weist darauf hin**. *Ein Sicherheitsnetz, das niemand
  sieht, verdeckt genau den Fehler, gegen den es gespannt ist.* **Wer eine Kopieranweisung
  schreibt, zählt den Ausgabeordner ab und nicht das Gedächtnis.**
- **⛔ Ein Blockzitat innerhalb eines Listenpunkts kennt `Markdown.Parse` nicht.** Die `>`
  stehen im Hilfe-Fenster **wörtlich** da, und die Zeilen laufen ineinander — **auf GitHub
  sieht dasselbe richtig aus.** In den vier mitgelieferten Dokumenten also: Blockzitate nur
  am Zeilenanfang, nie eingerückt.
- **⛔ Inline-Auszeichnung im Linktext überlebt wörtlich.** ``[`datei.md`](datei.md)`` zeigt
  im Betrachter die Backticks. Wird ein `.md`-Ziel nicht angenommen, wird es schlichter Text
  (§4.99) — und der Linktext dabei **nicht** weiter ausgewertet.
- **⚠ Dieselbe Regel wie bei der dreispaltigen Tabelle (§4.101):** Was in den vier Dokumenten
  steht, wird an **zwei** Orten gelesen — auf GitHub und im Hilfe-Fenster. **Beide prüfen.**

**Neu aus §4.102 — was Flathub verlangt**

- **⛔ Flathub baut aus dem Quellcode und ohne Netz.** Ein Manifest, das ein fertiges
  `dotnet publish`-Ergebnis einpackt (`type: dir`), wird abgelehnt. **Unser Manifest tut
  genau das** — es ist für die Erprobung in Schritt ③ gebaut worden und nicht für eine
  Einreichung. *Eine Begründung kann richtig sein und trotzdem nur für ihren Zweck gelten.*
- **⛔ `org.freedesktop.Sdk.Extension.dotnet10` EXISTIERT**, mit `branch/25.08` und dem
  SDK 10.0.300 (GA). Der Manifestkopf behauptete das Gegenteil und ist berichtigt.
  **Nachsehen, nicht annehmen** — die GitHub-API beantwortet die Frage in einer Zeile.
- **⚠ `--socket=x11` ohne `fallback-x11` ist unsere gemessene Entscheidung** (§4.96) und wird
  im Flathub-Review mit ziemlicher Sicherheit angesprochen. **Wer sie auf Zuruf ändert, macht
  das Paket unter GNOME/Wayland unstartbar.** Die Antwort steht in §4.102.
- **⚠ `flathub.json` mit `only-arches` gehört gleich richtig gesetzt.** Eine später
  gestrichene Architektur bleibt auf ihrer alten Fassung stehen und ist nur über ein Issue
  wieder loszuwerden.

**Neu aus §4.101 — was das Veröffentlichen gelehrt hat**

- **⛔ Eine Sortierung darf nicht von der Kultur des Rechners abhängen.** `AlsDatum` las mit
  `CultureInfo.CurrentCulture`: Auf `de-DE` war „15.02.2026" ein Datum, auf `en-US` nicht —
  und die Spalte wurde damit als **Zahlen**spalte sortiert, `15.022.026`. **Der Fehler ist
  still**, weil das Ergebnis sortiert aussieht. **Wo Daten *gelesen* werden, gehört eine
  feste Kulturenliste hin** (`de-DE`, dann invariant — wie `TdTabellenformel.AlsZahl` es seit
  jeher macht); `CurrentCulture` ist nur dort richtig, wo *Text für den Nutzer verglichen*
  wird (Suche, alphabetische Ordnung).
- **⛔ Und der Wächter dazu muss die Kultur selbst setzen.** Ein Test ohne
  `CultureInfo.CurrentCulture = new CultureInfo("en-US")` erbt die des Rechners — *er prüft
  dann den Rechner und nicht das Programm* und ist hier vier Tage lang grün geblieben,
  während die CI rot war. Muster: `ZahlenblockTests`, `TabellenUmbauTests`.
- **⛔ Die CI-Protokolle sind ohne Adminrechte am Repo nicht lesbar** — auch bei einem
  **öffentlichen** Repo, und auch die eines einzelnen Jobs (`HTTP 403: Must have admin
  rights to Repository`). **Lesbar sind Annotationen**
  (`/repos/:o/:r/check-runs/:id/annotations`). Deshalb schreiben beide Jobs ihr Protokoll per
  `tee` mit und benennen bei Fehlschlag die gefallenen Wächter per `::error::`. *Wer eine CI
  baut, an die er selbst nicht herankommt, baut ein Netz mit verbundenen Augen.*
- **⛔ GitHub startet jeden `bash`-Schritt mit `-eo pipefail`.** Ein `grep` ohne Treffer gibt
  1 zurück und bricht den Schritt ab. **Ein Schritt, der einen Fehler benennen soll, darf
  selbst keinen erzeugen** — `set +e`, `set +o pipefail`, `exit 0`. Beim ersten Anlauf standen
  deshalb **zwei** Fehlschläge je Job, und der zweite war der Erklärschritt.
- **⚠ Ein grüner lokaler Lauf sagt nichts über die CI.** Neun Runden hintereinander haben
  lokal grün gemessen und nicht bemerkt, dass das Netz durchhing. **Zum Ablauf einer Runde
  gehört ein Blick auf den letzten CI-Lauf** — der Aufruf steht in §8 und braucht keine
  Anmeldung.

- **⛔ Ein waagerechtes `StackPanel` schneidet Text ab, es bricht ihn nicht um.** Es misst
  seine Kinder mit **unendlicher Breite**; ein `TextBlock` darin erfährt nie, wie viel Platz
  er hat. **`TextWrapping` hilft dort nicht** — es gibt keine Breite, an der gebrochen werden
  könnte. **Gefunden am Über-Dialog**, als `About.Version` mit 1.0.0 länger wurde: Der
  Nachsatz war weg, der Bau grün. **Wer eine Textspalte neben ein Symbol setzt, nimmt ein
  `DockPanel` oder ein `Grid`** — in beiden Köpfen. *Ein Text, der abgeschnitten wird statt
  umzubrechen, ist kein Textproblem, sondern ein Layoutfehler, und er wartet auf den, der die
  Zeile das nächste Mal verlängert.*
- **⛔ Eine Markdown-Tabelle mit mehr als zwei Spalten wird im Hilfe-Fenster abgeschnitten.**
  Auf GitHub sieht sie gut aus, im Über-Dialog fehlt die letzte Spalte. **In den vier
  mitgelieferten Dokumenten also höchstens zwei Spalten** — oder nummerierte Absätze, wie im
  Abschnitt „Installieren". ⚠ *Abschnitt 14 beider Anleitungen trägt weiterhin eine
  dreispaltige; der Verdacht steht in §4.101 und ist ungeprüft.*
- **Eine Versionsnummer steht an fünf Stellen, und kein Compiler hält eine davon nach:**
  `Directory.Build.props`, `About.Version` in **beiden** Loc-Tabellen, die `<release>`-Zeile
  der `metainfo.xml`, die vier mitgelieferten Dokumente, der Tag. **Die Liste steht jetzt im
  Kommentar von `Directory.Build.props`** — dort sieht sie der nächste, der sie anhebt.
- **Bildschirmfotos dürfen nicht aus dem echten Bestand kommen.** Dauerregel 4 erlaubt eine
  Kopie zum **Prüfen**; ein Bild davon wäre die Veröffentlichung von Schulunterlagen. Dafür
  gibt es **`tools/demo-db`** — und wer es erweitert, hält sich an dessen eigene Regel:
  **alles auf einer Notizbuchseite muss über Seiten-y 690 liegen**, sonst steht es unterhalb
  dessen, was das Fenster bei 100 % zeigt, und fehlt auf dem Bild.
- **`site/` und nicht `docs/`, und der Grund ist Windows:** GitHub Pages kann `/docs`
  veröffentlichen, dieses Repo hat aber `Docs/` — und **unter Windows sind `docs` und `Docs`
  dasselbe Verzeichnis**. Veröffentlicht wird deshalb über `pages.yml`.
- **`tools/klick.ps1` kann einen Dialog nicht blättern.** Es holt das **Hauptfenster** in den
  Vordergrund; ein modaler Dialog verliert dabei die Tastatur, und `{PGDN}` läuft ins Leere.
  Im **WPF**-Kopf geht es, solange man vorher in den Text klickt — im **Avalonia**-Kopf gar
  nicht. *Wer weiter unten in einem Hilfe-Fenster etwas prüfen will, weiß das vorher besser
  als hinterher.*

**Neu aus §4.99 — was der Prüflauf von ④ gelehrt hat**

- **⛔ Eine abgelaufene Begründung liest sich wie eine gültige.** Der Kommentar an
  `AvaloniaDocumentIo.ImportFormats` („der Markdown-Import geht drüben über ein
  `FlowDocument`") **stimmte, als er geschrieben wurde**, und deckte danach jahrelang ein
  Loch in M2. *Der Unterschied zu §4.77 ist wichtig:* Dort war die Begründung **falsch** —
  hier war sie **abgelaufen**, und das ist die gefährlichere Sorte, weil sie beim Lesen
  richtig klingt. **Wer einen Kommentar findet, der eine Prüfung erspart, misst nach.**

- **⛔ Ein Test, der sich einer Lücke anpasst, hält sie nicht fest — er verdeckt sie.**
  `Jede_Diagrammart_uebersteht_DOCX` kürzte die Palette auf das, was durch DOCX passt, und
  war deshalb grün, **ohne über den Verlust etwas auszusagen**. *Wer eine Zeile schreibt, die
  einen Test an eine Einschränkung anpasst, schreibt daneben einen zweiten Test, der die
  Einschränkung **misst**.*

- **⛔ Eine benannte Lücke im Umbruch ist nicht automatisch unerreichbar.** §4.19 hielt fest,
  dass eine Tabelle *in* einer Zelle nicht gesetzt wird — und niemand hat gefragt, **ob der
  Nutzer eine anlegen kann**. Er konnte: `TdEdit.Ort` steigt in Zellen ab, weil man dort
  tippen können muss. *Zu jeder Lücke im **Zeichnen** gehört die Frage, ob das **Anlegen**
  gesperrt ist.*

- **⛔ Ein Verweis, der aussieht wie einer und keiner ist, ist schlimmer als gar keiner.**
  Ein `.md`-Ziel ohne Behandler wurde in **beiden** Malern in der Akzentfarbe gezeichnet, im
  Linux-Kopf zusätzlich unterstrichen und mit Handzeiger. **Fünf solche Stellen** standen in
  den mitgelieferten Dokumenten. Seither fragt der Maler über `Dokumentverweise.Kann`, **ob
  das Ziel überhaupt jemand annimmt**, und zeichnet sonst schlichten Text.
  - **Und zwei Glieder, nicht eines:** Fragen und Handeln sind zwei Zeitpunkte. Ein
    Behandler, der „habe ich es genommen?" zurückgibt, müsste beim **Bauen** gerufen werden
    — und das Bauen öffnete Fenster.

- **⛔ `EndsWith(".md")` ist keine Prüfung auf ein Markdown-Ziel.**
  `README.md#zwei-ausgaben-eine-app` endet nicht auf `.md`. Derselbe Fehler saß in den
  Prädikaten **und** in beiden Malern. *Zwei Stellen, die dasselbe entscheiden, entscheiden
  es verschieden* — die zweite ist deshalb ersatzlos gestrichen.

- **⛔ Die mitgelieferten Dokumente veralten schneller als der Code, und niemand merkt es.**
  Elf Stellen beschrieben eine App von **vor M2**; die README-Tabelle „Was der Linux-Ausgabe
  noch fehlt" hatte **sechs Zeilen, fünf davon falsch**. *Eine Einschränkungsliste, die
  niemand nachmisst, wächst nur — erledigte Punkte tragen sich nicht von selbst aus.*
  **Wer ein Loch schließt, streicht es im selben Commit aus README und Anleitung** — in
  **beiden** Sprachen, sonst entsteht die Unsymmetrie, die §4.99 vorgefunden hat.

- **⚠ Der Bildbetrachter der Hilfe zeigt einen Ersatztext, kein Bild.** `MdImage` wird in
  beiden Köpfen als `[alt]` in gedämpfter Farbe gezeichnet — die vier Dokumente liegen als
  eingebettete Resource, ihre Bildpfade lassen sich gegen keine Datei auflösen. **Wer in ⑤
  ein Bildschirmfoto ins README setzt, sehe es im Hilfe-Fenster nach.**

**Neu aus Phase 5 (§4.68–§4.96) — was dreiundzwanzig Runden Oberfläche gelehrt haben**

> **▶ Warum dieser Abschnitt am 2026-09-04 nachgetragen wurde, und das ist selbst der erste
> Befund:** §7 hatte zwischen §4.67 und §4.96 **keinen einzigen neuen Eintrag** — zwanzig
> Runden lang. Die Fallen dieser Zeit standen ausschließlich im **Prompt von §5e**, und der
> **wird jede Runde überschrieben**. Sie wären mit Schritt ⑤ ersatzlos verschwunden.
> *§7 ist der Ort, den man vor einer Änderung liest; ein Prompt ist der Ort, den man einmal
> ausführt. Was länger gilt als eine Runde, gehört nicht in den Prompt.*

- **⛔ Eine im Code gebaute Liste bemerkt das Dateisystem nicht** (neu aus §4.107, 2026-09-10).
  Das Design-Untermenü entstand beim Start und wurde nur nach einem Design- oder
  Sprachwechsel neu gebaut — **zwei von Hand in den Ordner gelegte Dateien standen bis zum
  Neustart nicht darin**, obwohl derselbe Menüpunkt („Design-Ordner öffnen") ausdrücklich
  zum Hineinlegen einlädt. **Wer eine Liste aus einem Ordner baut, baut sie beim Aufklappen
  neu**, nicht beim Start; dasselbe gilt für Sticker, Cover und die Geodreieck-SVGs.
  *Kein Bau und kein Wächter kann das sehen — der Fehler entsteht erst, wenn jemand etwas in
  einen Ordner legt, während das Programm läuft.*
- **⛔ Ein Ressourcenschlüssel, der im Code entsteht, schlägt still fehl.** Seit §4.107 bauen
  **beide** Köpfe ihr Farb-Wörterbuch in einer Schleife über `ThemeColor`. Solange die Farben
  in einer XAML-Datei standen, war ein vertippter Schlüssel ein **Ladefehler**; jetzt bleibt
  die Fläche in der Vorgabe des Rahmenwerks stehen, und **niemand bekommt einen Fehler** —
  dieselbe Mechanik wie beim Violett (§4.86). **`ThemeschluesselTests` in beiden
  Testprojekten liest dafür den Quelltext** und prüft jeden `Brush.X`/`Color.X` gegen die
  Tabelle. **Und: nie `StaticResource` auf eine Theme-Farbe** — Stelle [0] der
  `MergedDictionaries` ist beim Auswerten der XAML **leer** und wird bei jedem Designwechsel
  ersetzt; ein `StaticResource` fragt genau einmal.
- **⛔ Ein grüner Bau beweist an einer *Oberfläche* fast nichts.** Das steht weiter unten
  schon für die **Eingabe-Naht** (§4.55) — an einer Oberfläche ist es schlimmer, weil dort
  gar kein Wächter existiert, der es sehen könnte. Vier Beispiele aus derselben Phase, alle
  bei **0 Fehlern und grünen Tests**: Gruppenüberschriften standen **unsichtbar** im Flyout
  (§4.88), die halbe Werkzeugleiste **ragte aus dem Fenster** (§4.90), in **jedem**
  beschrifteten Knopf stand das Symbol oben statt mittig — **dreizehn Runden lang** (§4.94),
  und ein Stil erwischte das Suchfeld, die Zahlenfelder daneben aber nicht (§4.95). **Was am
  laufenden Programm zu sehen ist, wird am laufenden Programm gesehen** — in beiden Köpfen,
  an derselben Datei.
- **⛔ Ein Selektor kann existieren und trotzdem nicht gelten — still.** In Avalonia ist
  `Style Selector="Button.ribbonknopf"` etwas **anderes** als
  `ToggleButton.ribbonknopf`; beide stehen in `Themes/Styles.axaml`, und ein Element bekommt
  nur den, der auf seinen **Typ** passt. **Wer den Typ eines Steuerelements wechselt
  (`Button` ↔ `ToggleButton`), verliert sein Aussehen ohne jede Meldung** — der Knopf ist da,
  er tut das Richtige, er sieht nur aus wie Fluents Vorgabe. **Zweimal passiert, in beide
  Richtungen** (§4.80 am laufenden Programm gefunden, §4.84 beim Nachsehen). *Der Compiler
  prüft, ob ein Stil existiert, nicht ob er jemanden trifft.*
- **⛔ Die Akzentfarbe wird an der *Wurzel* gesetzt, nicht am Blatt.** Fluents
  `SystemAccentColor` schlägt an jeder Fläche durch, die noch niemand überschrieben hat —
  Baum, Klappliste, Schieber, Reiter, Auswahlpunkt, Ribbon-Schalter, Eingabefeld: **sechs
  Runden, sechs Stellen, dasselbe Violett**. Die Antwort ist `FluentTheme.Palettes` /
  `ColorPaletteResources.Accent`, gespeist aus `ThemeColor.Accent` in Core — heute
  `AvaloniaThemeHost.AkzentSetzen`, **sieben Schlüssel und ein Setzer** (§4.86). *Wer die
  Wurzel färbt, muss den Namen des Blattes nicht kennen* — drei Anläufe in §4.74 waren daran
  gescheitert, ihn zu finden.
- **⛔ Zwei Wege, die dasselbe bauen, weichen an der Stelle voneinander ab, die niemand
  nachsieht.** Die Diagramm-Rundreise war seit §4.50 durch Wächter gedeckt — aber das
  **Werkzeug** baute seinen Behälter **ohne `Tag`**, und ein frisch eingefügtes Diagramm war
  beim ersten Speichern ein **Bild** (§4.82). Kein Wächter konnte das sehen: er prüfte den
  einen Weg. **Wer einen zweiten Erzeugungsweg baut, prüft ihn gegen den ersten und nicht
  gegen die Anschauung.**
- **⛔ Ein Fenster kann richtig rechnen und trotzdem lügen.** Über einer *Änderung* stand
  „Diagramm einfügen", auf dem Knopf „Einfügen" (§4.83). Bau grün, Funktion richtig — *das
  Fenster sagte nur nicht, was es tut.* Dieselbe Familie: vier Sprachtexte trugen `&amp;`
  **wörtlich**, und der WPF-Kopf zeigte sie seit jeher so an (§4.90).
- **⛔ Eine abgehakte Vermutung ist teurer als eine ungeprüfte** (§4.95). Die Ursache des
  „ungespeichert"-Punkts stand seit §4.93 **wörtlich als Vermutung in der Tabelle** — mit
  einem Häkchen daneben, weil sie **am falschen Steuerelement** geprüft worden war. Das
  Häkchen hat zwei Runden gekostet. **Wer eine Vermutung abhakt, schreibt dazu, *woran* er sie
  geprüft hat.**
- **⛔ Wer wissen will, wer etwas setzt, fragt die Stelle, an der es gesetzt wird** (§4.95).
  Drei Vermutungen am Code sind nacheinander gescheitert; eine **Wegwerf-Sonde im Setter** hat
  es beim **ersten** Lauf gezeigt. *Lesen beantwortet „wer könnte", messen beantwortet „wer
  hat".*
- **⛔ Koordinaten aus einem Flyout gelten nur für die Aufnahme, aus der sie stammen**
  (§4.91, drei Anläufe). Ein Flyout öffnet nicht zuverlässig an derselben Stelle. **Vor jedem
  Klick in ein Flyout neu fotografieren.**
- **⛔ `dotnet test --no-build` nach einem *Teilbau* läuft gegen eine veraltete Test-DLL**
  (§4.82) — vier Wächter sahen rot aus, obwohl der Code längst zurückgestellt war. **Vor jedem
  Testlauf, dessen Ergebnis zählt: `dotnet build GonkNote.slnx`.**
- **⛔ Ein laufender Kopf sperrt seine Exe.** Vor jedem Bau den vorherigen mit
  `CloseMainWindow` (bzw. `WM_CLOSE` an **alle** Fenster des Prozesses) schließen, **nie mit
  `Stop-Process`** (§4.55).
- **▶ `UndoStack` und `TdUndo` sehen wie eine Doppelung aus und sind keine — Nutzer-Entscheidung
  2026-09-04, §5 Nr. 30 (a): getrennt lassen.** Die beiden Verlaufsstapel teilen rund **zwanzig
  Zeilen** Form: zwei Listen, ein Deckel, ein Ereignis. **Der Unterschied zu §4.13 ist der, auf
  den es ankommt:** Dort standen `WbHit` und die Markdown-Grammatik **zweimal als dieselbe
  Rechnung** da, und die zwei Fassungen konnten auseinanderlaufen, *ohne dass es auffällt*.
  Hier gibt es **keine gemeinsame Wahrheit**, die zwei Stellen verschieden beantworten könnten
  — der eine speichert Elemente der Zeichenfläche, der andere Blöcke eines Dokuments, gefüllt
  von verschiedenen Rechnungen. Geteilt wird die **Form** eines Stapels, nicht sein Inhalt.
  **Die drei Gründe im Einzelnen stehen in §4.33.**

  > **▶ Und warum das hier steht und nicht nur in §4.33:** Der Grund stand dort schon seit
  > Phase 4 — **nur an der Stelle, die niemand liest, wenn er eine Doppelung sucht.** In
  > Schritt ④ ist die Frage deshalb zum zweiten Mal gestellt worden. *Eine Ähnlichkeit, deren
  > Auflösung nicht dort steht, wo man auf sie stößt, wird in jeder Aufräumrunde neu geprüft
  > — und irgendwann räumt sie jemand weg.*

- **▶ Und die Regel, die diese Phase am häufigsten bestätigt hat:** *ein Auftrag ist keine
  Messung, auch wenn er im HANDOFF steht.* Die Aufgabenliste von ①c versprach **viermal**
  Arbeit, die längst dastand (§4.77, §4.81, §4.82), und verschwieg **dreimal** welche —
  wovon zwei dann selbst Fehlalarm waren (§4.89). **Eine Aufgabenliste, die niemand nachmisst,
  wächst nur: erledigte Punkte tragen sich nicht von selbst aus.**

**Persistenz — der gefährlichste Bereich**

- **Models nie umziehen, ohne an `_type` zu denken.** Je Whiteboard-Element steht
  „Namensraum.Typ, Assembly" in der Datei. Ändert sich eines von beiden, lässt sich **kein**
  Bestandsdokument mehr öffnen. Seit Phase 2 stehen diese Zeichenketten als
  `[JsonDerivedType]` an `WbElement` — **sie sind Datenformat, kein Codedetail.** Wer den
  Namensraum von `GonkNote.Core.Models` oder den Assemblynamen ändert, zieht sie **nicht**
  mit. Die Übersetzung noch älterer Namen liegt in `ModelTypeBinder` in
  `src/GonkNote.Legacy/` und wird nur bei der einmaligen Übertragung gebraucht.
  Wächter: `AlteTypnamenTests`.
- **Ein neuer Elementtyp braucht drei Einträge, nicht einen.** `[JsonDerivedType]` an
  `WbElement`, ein Zweig in `DatenbankRoundtripTests.GleichesElement` (sonst bewacht kein
  Test seine Felder — der `default`-Zweig sagt das ausdrücklich) und ein Exemplar in
  `Beispieldokument`. Fehlt der erste, **wirft das Speichern**; das ist Absicht und besser
  als ein still verschwundenes Element.
- **`EmptyStringToNull` war bei LiteDB standardmäßig `true`** → leere Strings kamen als
  `null` zurück. `System.Text.Json` macht aus `""` nie `null`, der Fall ist damit erledigt.
  **Die null-sicheren Setter in `TextDoc` bleiben trotzdem stehen:** eine *migrierte*
  Altdatei kann `null` mitbringen. Wächter:
  `Leerer_String_kommt_leer_zurueck_und_nicht_als_null`.

**Neu aus Phase 2 — SQLite (§4.8)**

- **Der Dateiname der Datenbank bestimmt den Blob-Ordner.** `BlobStore` schneidet mit
  `Path.GetFileNameWithoutExtension` ab: `gonknote.sqlite` → `gonknote.blobs`. Nur die
  **Endung** zu ändern ist folgenlos, **den Stamm zu ändern wäre fatal** — alle Bilder wären
  scheinbar weg, und man sucht den Fehler im Renderer statt im Pfad. Genau deshalb heißt die
  neue Datei `gonknote.sqlite` und nicht `gonknote-v2.db`.
- **Kein WAL.** `PRAGMA journal_mode=wal` legt zwei Nebendateien an (`-wal`, `-shm`). Wer die
  Datenbank dann kopiert, ohne sie mitzunehmen, verliert die zuletzt geschriebenen
  Änderungen — und genau das tut §8 beim Gegenprüfen mit echten Daten. Eine Datei bleibt eine
  Datei. Wer WAL doch einschaltet, muss §8 und die Sicherungsanleitung in beiden
  Erste-Schritte-Fassungen mitziehen.
- **`Pooling = false` in der Verbindungszeichenkette ist Pflicht, nicht Geschmack.**
  `Microsoft.Data.Sqlite` hält die Datei sonst nach `Dispose` weiter offen: der Wegwerf-Ordner
  eines Tests ließe sich nicht löschen, und das `File.Move` am Ende der Migration schlüge fehl.
  Der Fehler sieht dabei nicht nach „Datei gesperrt" aus, sondern nach einem sporadisch roten
  Testlauf.
- **Eine Migration erkennt man an der Kopfkennung, nicht an der Endung.** `gonknote.db` sagt
  nichts darüber, was in der Datei steht — die ersten sechzehn Bytes (`SQLite format 3`)
  schon. Deshalb funktioniert `--db …\gonknote.db` aus §8 unverändert weiter: die Kopie wird
  erkannt, daneben entsteht `gonknote.sqlite`, und beide teilen sich `gonknote.blobs`.
- **Bei der Übertragung nicht `UpsertItem` benutzen.** Es setzt `ModifiedUtc` auf jetzt und
  datierte damit jeden Eintrag des Nutzers auf den Tag der Migration um. Dafür gibt es die
  private `SchreibeItem`.
- **Ein Kopf, der den `ILegacyDatabaseReader` vergisst, legt keine leere Datenbank an** — er
  bekommt eine `InvalidOperationException`. Das ist Absicht: eine leere Datenbank neben
  vollen Bestandsdaten ist für den Nutzer von Datenverlust nicht zu unterscheiden. Wächter:
  `Ohne_Leser_wird_nicht_stillschweigend_neu_angefangen`. **Seit Phase 3 übergibt ihn auch
  der Avalonia-Kopf** (`App.OnFrameworkInitializationCompleted`) — der Compiler hätte es
  nicht angemahnt, es ist ein *optionaler* Parameter. Für den iPadOS-Kopf gilt das
  ausdrücklich **nicht**: dort gab es nie eine LiteDB-Datei.
- **Ein Paket kann eine Sicherheitslücke mitziehen, die man nicht selbst anheben kann.**
  `Microsoft.Data.Sqlite` 10.0.10 bringt `SQLitePCLRaw…lib.e_sqlite3` **2.1.11** mit — mit
  einem bekannten Fund (NU1903). Behoben über
  `CentralPackageTransitivePinningEnabled` in `Directory.Packages.props` plus einer
  `PackageVersion`-Zeile auf 2.1.12. **Beim nächsten Anheben von `Microsoft.Data.Sqlite`
  prüfen, ob die Zeile noch nötig ist** — ein Pin, den niemand mehr braucht, hält irgendwann
  eine Fassung fest.
- **`System.Text.Json` schreibt auch nur lesbare Eigenschaften.** `WbPage.HasBackgroundImage`,
  `WbPage.IsInfinite` und `NoteItem.IsFolder` tragen deshalb `[JsonIgnore]` — sonst stünde ein
  abgeleiteter Wert mit in der Datei und sähe später wie gespeicherte Wahrheit aus.
- **Ein statisches `Source`/`Current`-Feld, das nur an einer Stelle gesetzt wird, ist eine
  stille Falle** (`BlobStore.Current` gesetzt, `ImageCache.Source` nicht → Bilderverlust).
- **Die Bytes im Datensatz sind kein „gibt es das Bild?"** — die Frage steht genau einmal im
  Modell: `WbPage.HasBackgroundImage`.
- **Neuer Bildträger?** Vier Schritte, sonst räumt der Aufräumlauf das Bild weg — V1-Handoff §9.2.

**Neu aus Phase 0**

- **Wurzel-Dateien brauchen `Link=`.** `Assets`, `tessdata`, `README.md` & Co. liegen zwei
  Ebenen über der `.csproj`. Ohne `Link` landen sie im Ausgabeordner an einem anderen Platz
  und der Resource-Name ändert sich — `EmbeddedDocs` (`pack://application:,,,/README.md`) und
  der dreistufige Ladeweg des Geodreiecks brechen dann **still**.
- **`xmlns:loc="clr-namespace:GonkNote.Services"` steht in 13 XAML-Dateien** ohne
  `assembly=`-Angabe, zeigt also immer auf die *lokale* Assembly. Deshalb ist `TExtension` im
  WPF-Projekt geblieben und behält den Namensraum `GonkNote.Services`, obwohl `Loc` jetzt in
  Core liegt. Wer den Namensraum von `Loc` ändert, muss alle 13 Dateien mitziehen.
  **Seit Phase 3 gibt es `TExtension` zweimal** — einmal je Kopf, mit demselben Namensraum,
  weil `MarkupExtension` und `Binding` in WPF und Avalonia gleich heißen und verschiedene
  Typen sind. Genau dafür wurde die Klasse in Phase 0 aus `Loc.cs` herausgetrennt.
- **`git mv` statt `Copy-Item`** — sonst ist `git blame` über die Umstrukturierung hinweg weg.
  Gegenprobe: `git log --follow <pfad>`.
- **`git mv Assets assets` schlägt auf NTFS fehl** (Groß-/Kleinschreibung). `Assets/`, `Docs/`
  und `tessdata/` heißen deshalb genau wie bisher.
- **Zentrale Paketverwaltung ist alles-oder-nichts:** sobald
  `ManagePackageVersionsCentrally` an ist, darf **keine** `PackageReference` mehr ein
  `Version=` tragen.

**Neu aus dem SkiaSharp-3-Umstieg — die teuerste Lektion bisher**

- **Ein grüner Build sagt bei einem Bibliothekssprung fast nichts.** Der Umstieg baute mit
  0 Fehlern und 0 Warnungen — und die App stürzte beim **ersten Zeichnen jedes Notizbuchs
  und jedes Whiteboards** ab. Ursache:
  `SKColorFilter.CreateTable(alpha, null, null, null)` — bis SkiaSharp 2 hieß `null`
  „dieser Kanal bleibt unverändert", seit 3.x wirft es `ArgumentNullException`. Das steckte
  in der Bleistift-Körnung im **statischen Konstruktor** von `WbRenderer`, riss also den
  ganzen Renderer mit (`TypeInitializationException`).
  **Merksatz: nach jedem Paketsprung die Zeichenwege am laufenden Programm abklappern**,
  nicht nur bauen. Genau dafür entstehen in Phase 1 die Renderer-Snapshots.
- **`null` an Skia-Aufrufen ist ab jetzt verdächtig.** Weiter geprüft und in Ordnung:
  `SKShader.CreateLinearGradient(..., colorPos: null, ...)` und
  `SKPathEffect`-Zuweisungen mit `null`.
- **Zweiter Fall derselben Falle, gefunden in Phase 1: `SKBitmap.Decode(byte[])`.** Bis 2.88
  lieferte der Aufruf bei unbrauchbaren Daten `null`; seit 3.x legt er intern einen `SKCodec`
  an und reicht ihn an `Decode(SKCodec)` weiter — ist das Format unbekannt, ist der Codec
  `null` und der Aufruf **wirft** `ArgumentNullException`. Die übliche Prüfung
  `if (bmp == null)` dahinter wird nie erreicht.
  **Betroffen waren drei Stellen**, alle mit genau diesem Muster: `ImageCache.Get` (ein
  einziges kaputtes Blob riss damit das Zeichnen der ganzen Seite ab),
  `WhiteboardView.PrepareRaster` und `OcrService.Preprocess`.
  Eine Wahrheit dafür: **`WbRenderer`-Nachbar `WbImages.Decode`** in
  `src/GonkNote.Core/Rendering/WbImages.cs` — stellt den alten Vertrag wieder her
  (`null` = „kein erkennbares Bildformat"). **Nie wieder `SKBitmap.Decode(bytes)` direkt
  aufrufen.** Wächter: der Snapshot-Test `Kaputtes_Bild_bekommt_einen_Platzhalter`.
  Merksatz derselbe wie oben: Ein grüner Build sagt bei einem Bibliothekssprung fast nichts.
- **Ein Paket kann eine Plattformversion erzwingen.** `SkiaSharp.Views.WPF` 3.x liefert
  `net10.0-windows10.0.19041`. Wer nur `net10.0-windows` (= `…7.0`) angibt, bekommt
  wortlos die alte `net462`-Fassung untergeschoben (NU1701) — samt OpenTK/GLWpfControl.
  **NU1701 ist kein Schönheitsfehler, sondern der Hinweis auf ein falsch aufgelöstes Paket.**

**Neu aus dem Symbolsatz (§4.31) — und es hat den Texteditor gekostet**

- **Ein Stil mit `TargetType="Path"` an einem `views:Symbol` wirft.** `Symbol` ist ein
  `FrameworkElement` und kein `Path`; WPF meldet beim **Anwenden** des Stils
  „TargetType Path entspricht nicht dem Typ des Elements Symbol". Beim Umbau von `<Path>` auf
  `<views:Symbol>` blieben in `TextEditorView.xaml` fünf `Style="{StaticResource PathIcon}"`
  stehen — **der Windows-Kopf konnte danach kein Textdokument mehr öffnen**, einen ganzen Tag
  lang. Behoben am 2026-08-12 (§4.32).
  **Merksatz: Ein `views:Symbol` trägt keinen Stil.** Strichstärke, Kappen und Größe bringt es
  selbst mit (`Size`, `Weight`, `AppIcons.StrokeFor`).
- **Kein Wächter kann das sehen, und zwar keiner der zwölf.** Die neun in Core prüfen die
  Tabelle, die drei im WPF-Projekt lesen den **Quelltext** der XAML — beide Sorten waren grün.
  Der Fehler entsteht erst, wenn WPF den Stil anwendet, und das tut nur ein laufendes Fenster.
  **Nach einer Runde, die XAML anfasst, gehört jede betroffene Ansicht einmal geöffnet** —
  dieselbe Lehre wie §4.28 („die doppelte Kopfzeile") und §4.31 („das Kontaktblatt").

**Oberfläche und Texte**

- **Die vier mitgelieferten Dokumente liegen paarweise** (`README.md`/`README.en.md`,
  `ERSTE-SCHRITTE.md`/`GETTING-STARTED.md`) und sind eingebettete Resources. Wer eine
  ändert, muss die Gegenfassung mitziehen — `EmbeddedDocs` wählt nach `Loc.Current` und
  fällt still auf Deutsch zurück. Siehe Dauerregel 1 in der Kopfzeile.
- **Die vier Dokumente nicht umbenennen.** Sie sind in `GonkNote.Wpf.csproj` als `Resource`
  eingebunden und werden über `pack://application:,,,/<Dateiname>` gelesen. Eine
  Umbenennung bricht sofort den Build (`BG1002`) — und wäre sie im `.csproj` nachgezogen,
  bräche sie still `EmbeddedDocs`. `README.md` ist zusätzlich die Startseite auf GitHub.
- **Zugriffstasten im Menü kollidieren still.** Im englischen Hilfe-Menü lagen
  „_Getting started" und „About _Gonk Note" beide auf `G` — WPF meldet das nicht, der
  Eintrag reagiert einfach nicht. **Beim Übersetzen die Unterstriche prüfen.**
- **Texte, die der Code setzt** (Seitenzähler, Wortzähler, Galerietitel, Tooltips), hängen
  an keiner Bindung und müssen nach einem Sprachwechsel neu geschrieben werden — dafür gibt
  es `Loc.LanguageChanged`.

**Neu aus Phase 2 — Übersetzung**

- **Eine gebundene Eigenschaft, deren Text aus `Loc` kommt, braucht `Loc.LanguageChanged`
  — auch wenn sie schon aus einem anderen Grund benachrichtigt.** `PinMenuHeader` und
  `FavoriteMenuHeader` meldeten ihre Änderung nur beim Umschalten des Zustands
  (`RefreshPinFavorite`). Nach dem Sprachwechsel blieben genau diese zwei Einträge deutsch
  stehen, während das ganze Menü drumherum englisch wurde. Vorher fiel das nicht auf, weil
  die Texte fest verdrahtet waren — sie waren *immer* falsch, also nie auffällig.
  **Behoben:** `MainViewModel.RefreshLanguage` läuft jetzt über den Baum und ruft
  `RefreshPinFavorite` auf jedem Knoten.
  **Merksatz: eine Beschriftung, die sich aus zwei Gründen ändert, braucht zwei Auslöser.**
- **Ein Menü, das der Code baut, hat kein `{loc:T …}` — und niemand vermisst es.**
  `MainWindow.ShowGalleryMenu` erzeugt das Kontextmenü der Galerie-Kachel in C#. Drei
  Einträge („Öffnen", „Umbenennen", „Löschen") standen dort seit jeher fest auf Deutsch und
  erschienen auch im englischen Programm so. Aufgefallen ist es erst, als die beiden
  Nachbarn daneben übersetzt wurden. **Behoben** über die längst vorhandenen Schlüssel
  `Tree.Open` / `Tree.Rename` / `Tree.Delete`. Weil das Menü bei jedem Öffnen neu entsteht,
  greift `Loc.T` zur richtigen Zeit und braucht kein `LanguageChanged`.
  **Beim Suchen nach Übersetzungslücken reicht das XAML nicht** — `grep` nach Zeichenketten
  in `.cs` gehört dazu.
- **Listen aus `Loc.T` nicht im Konstruktor festhalten.** `WpfDocumentIo.ImportFormats` &
  Co. sind bewusst `=>`-Eigenschaften und keine `{ get; } = […]`: das Bündel
  `WpfPlatformServices` entsteht einmal beim Start, eine im Konstruktor gebaute Liste trüge
  für immer die Startsprache.

**Bauen und Testen**

- **`obj/` nicht löschen, ohne danach Debug UND Release zu bauen.** Die impliziten `using`s
  stehen in einer generierten Datei je Konfiguration
  (`obj\<Konfiguration>\<TFM>\*.GlobalUsings.g.cs`). Fehlt sie, meldet die **IDE** in jeder
  Datei Fehler wie „Der Name `Math` ist im aktuellen Kontext nicht vorhanden" oder
  „`CancellationToken` wurde nicht gefunden" — obwohl `dotnet build` der *anderen*
  Konfiguration glatt durchläuft und am Code nichts fehlt. Am 2026-07-30 genau so passiert:
  `obj` gelöscht, nur Release gebaut, die IDE arbeitet gegen Debug.
  **Gegenmittel:** `dotnet build -c Debug` — dann ist es weg.
- **Vor dem Build laufende Instanz beenden** — sonst Datei-Lock. **Nie pauschal
  `taskkill /IM`**, der Nutzer hat die App oft selbst offen; nur die eigene PID.
- **Nie in der echten Datenbank testen.** `GonkNote.exe --db <wegwerf.db>`. Wenn echte Daten
  gebraucht werden: **kopieren** (DB **und** `.blobs`-Ordner, der Name leitet sich vom
  DB-Namen ab) und die Kopie öffnen.
- **Ein Wegwerf-Test, der echte Daten anfasst, kommt nicht ins Repo.** Der feldweise
  Vergleich Alt↔Neu aus §4.8 lag als `ZzTempEchtdatenVergleich.cs` im Core-Testprojekt, hat
  seine Zahlen ausgegeben und ist danach gelöscht worden. Er hätte sonst bei jedem CI-Lauf
  nach einer Datei in `%TEMP%` gesucht, die dort nicht liegt — und beim Nutzer nach Daten,
  die niemanden etwas angehen.
- **DPI-Falle:** `SetProcessDPIAware()` als erste Zeile jedes Skripts. Der Testrechner läuft
  auf **200 %**.
- **PDF-Export prüfen, ohne sie ansehen zu müssen:** Edge headless rendert PDFs nicht (man
  bekommt nur den grauen Betrachter-Hintergrund). Der zuverlässige Weg führt über
  `PdfImporter.StreamPages` aus dem **echten** Core. **Seit Phase 1 steht das als Test**
  (`ExportFixtureTests.Whiteboard_PDF_…`) und nicht mehr als Konsolenprogramm in `%TEMP%`.
- **Docnet kann mehr als rendern — es kann Text zurücklesen** (`IPageReader.GetText`). Damit
  lässt sich fragen, was auf einer Seite *steht*, statt Farbe zu zählen; genau daran hängen
  seit §4.27 die schärfsten PDF-Wächter. **Beim alten Rasterweg wäre das unmöglich gewesen.**
- **`IPageReader.GetPageWidth()` misst nicht das Papier, sondern das gerenderte Bild.** Mit
  `PageDimensions(1080, 1080)` steht dort 1080 und nicht die Seitenbreite — ein Test, der das
  Seitenmaß prüfen will, liest sonst seine eigene Vorgabe zurück und ist immer grün. Für das
  Papier: `PageDimensions(1.0)`. Bei Maßstab 1 rendert PDFium im Benutzerkoordinatensystem, und
  das ist Punkt.

**Neu aus §4.56 — messen heißt: auf sauberem Grund messen**

- **Wer prüft, ob etwas entsteht oder verschwindet, fängt mit einer LEEREN Fläche an.** Bei
  Elementen, die immer an derselben Stelle erscheinen (Sticker landen mittig in der Sicht),
  ist ein Altbestand nicht bloß störend — er ist **von der Sache nicht zu unterscheiden**.
  In §4.56 hat das zwei Scheinbefunde erzeugt: „ein Klick fügt zwei Sticker ein" und
  „Rückgängig wirkt nicht". Beide Male lag noch ein Sticker aus dem vorigen Lauf da, den der
  Kopf beim regulären Schließen gespeichert hatte. **Für Wegwerf-Messungen die DB-Kopie neu
  ziehen, nicht die vom letzten Durchgang weiterbenutzen.**
- **Wer mit einer Taste misst, sieht vorher nach, ob sie etwas tut.** Die Werkzeug-Kürzel des
  Whiteboards sind **S, B, M, E, L, V, H** — „P" ist keins, und ein Test damit beweist nichts.
- **Ein Auftrag ist keine Messung, auch wenn er im HANDOFF steht.** Der Prompt in §5e nannte ein
  `StickerElement`, das es nicht gibt; der Satz war eine Runde zuvor hingeschrieben worden, ohne
  nachzusehen. *Was hier steht, ist Chronik und Absicht — der Code ist die Auskunft.*
- **Wer einen Knopf baut, der etwas auf die Fläche legt, gibt danach den Fokus zurück.**
  `Skia.Focus()` — sonst behält der Knopf ihn, und **keine** Taste kommt mehr an: nicht Strg+Z,
  kein Werkzeug-Kürzel, bis der Nutzer wieder auf die Fläche klickt. Die Zeile steht aus
  demselben Grund in `WhiteboardView.Input.cs`. **Der WPF-Kopf zeigt das nicht** — dort trägt
  Strg+Z unmittelbar; es ist Avalonia-eigen, und die Knöpfe der Werkzeugleiste sind es auch
  nicht (sie nehmen den Fokus gar nicht erst).
- **Sticker- und Cover-Ordner hängen nicht an `--db`.** Sie liegen im Datenordner
  (`AppPaths.DataSubfolder`). **Wer mit einer Wegwerf-Datenbank misst, arbeitet trotzdem auf der
  echten Sammlung** — wer dort etwas zum Prüfen ablegt, legt eine eigens erzeugte Datei ab und
  löscht genau diese wieder.

**Neu aus §4.55 — Tasten, Themes und was ein grüner Bau nicht zeigt**

- **Kommt eine Taste am falschen Ort an, ist die erste Frage die ROUTING-STRATEGIE.** In
  Avalonia bestimmt sie, wer eine Taste **zuerst** sieht: `Tunnel` heißt „vor allen Kindern,
  egal wo der Fokus liegt", `Bubble` heißt „erst nachdem das Control sie verarbeitet hat".
  **Drei Fehler in §4.55 kamen daher, und keiner sah danach aus** — einer wie ein
  Fokusproblem (getipptes „Hallo" schaltete Werkzeuge um, weil `WhiteboardView` ihren Handler
  seit Phase 3 mit `Tunnel` einhängt), einer wie ein Zeichensatzfehler (ein Kästchen hinter
  dem Text, weil `KeyDown` am `TextBox` als Bubble hing und Strg+Eingabe erst *nach* dem
  Einfügen ankam). **Nicht der Fokus, nicht der Zeichensatz, nicht das Messwerkzeug.**
- **Eine Farbe, die nicht ankommt, steht meist an einem Kind, das man nie angefasst hat.**
  Fluent setzt den Hintergrund eines `TextBox` **nicht am Control**, sondern am Border im
  Template (`TextBox:focus /template/ Border#PART_BorderElement` auf
  `TextControlBackgroundFocused`, dieselbe Stelle noch einmal für `:pointerover`). Der
  `Foreground` dagegen sitzt am Control selbst und wird von einem gesetzten Wert geschlagen.
  **Erst ins Template sehen, dann einen Style schreiben** — und wo das Theme benannte
  Ressourcen anbietet, sind die der einfachere Hebel als ein eigener Selektor: sie erfordern
  keine Annahme über Vorränge. Template-Teile sind **logische** Kinder
  (`TemplatedControl.ApplyTemplate` ruft `SetParent(this)`), der Ressourcen-Lookup vom Kind
  findet also, was am Control liegt.
- **Ein grüner Bau beweist an einer Eingabe-Naht fast nichts.** 913 Wächter waren grün,
  während der Kopf jede getippte Taste an der falschen Stelle verarbeitete. Wo die Bedienung
  im Kopf liegt, prüft nur das laufende Programm.
- **Wer den Schreiber abschießt, misst nicht den Leser.** Ein mit `Stop-Process` hart
  beendeter Kopf hat vielleicht nie gespeichert; eine leere Fläche im anderen Kopf ist dann
  **kein Befund, sondern gar keine Messung.** Für die Runde zwischen den Köpfen gilt:
  **speichern** (der Änderungspunkt im Titel muss weggehen), **regulär schließen** oder
  vorher den Tab wechseln, **dann** drüben öffnen.

**Neu aus §4.54 — Pfade und geteilte Rechnung**

- **Kein Kopf baut einen Datenpfad selbst.** `AppPaths` (Core) weiß seit Phase 2, wo der
  Datenordner liegt — unter Windows `%APPDATA%\GonkNote`, unter Linux `~/.config/GonkNote` —
  und hat mit `DataSubfolder(name)`/`AppSubfolder(name)` genau die zwei Methoden dafür.
  **Sticker und Cover-Vorlagen taten es bis Phase 4.5 von Hand** über
  `Environment.SpecialFolder.ApplicationData`: eine Windows-Festlegung mitten in einer Regel,
  die für alle Köpfe gelten soll. Wer einen neuen Ordner braucht, fragt.
- **Eine Liste, die zwei Werkzeuge benutzen, gehört keinem von beiden.** Die Endungsliste hieß
  `StickerExts` und lag bei den Stickern — die Cover-Vorlagen benutzten sie mit. **Aufgefallen
  ist es dem Übersetzer beim Verschieben, nicht beim Lesen.** Beim Verschieben einer privaten
  Sache nach Core lohnt deshalb ein Blick auf die Fehlerliste: sie nennt die Mitbenutzer.
- **Beim Zusammenlegen alle Teildateien lesen, nicht nur die naheliegende.** In §4.54 wurde
  `ReadableStickyTextColor` aus `WhiteboardView.Editing.cs` geholt und
  `EnsureReadableTextColor` in `WhiteboardView.Settings.cs` **übersehen** — dieselbe
  Luminanz-Formel, eine Datei weiter. `WhiteboardView` ist im WPF-Kopf auf **vierzehn**
  Teildateien verteilt und **im Linux-Kopf inzwischen ebenfalls auf vierzehn**, *am
  2026-09-04 nachgezählt* — die vierzehnte ist `WhiteboardView.Cover.cs` aus §4.81.
  *(Bis zum 2026-08-28 stand hier „dreizehn"; die Zahl ist mit ①c gewachsen, und **eine Zahl
  in der Doku altert leise** — sie wird nicht falsch gelesen, sie wird geglaubt.)*
  ✅ **Die Doppelung selbst ist zu:** beide sind heute Einzeiler auf `HexColor` in Core.
  *(Die vollen Dateinamen stehen erst seit Phase 5 hier — vorher hieß es „`Editing.cs`" und
  „`Settings.cs`", und bei vierzehn Teildateien ist das eine Suche und keine Angabe.)*
- **Farbe für Schrift: zwei Fälle, nicht einer.** `HexColor.LesbareSchrift()` **bestimmt** eine
  Schriftfarbe (der Notizzettel hat keine gewählte); `HexColor.MitGenugKontrast(grund)`
  **überstimmt** eine gewählte nur, wenn sie unlesbar wäre (ein Textfeld erbt die Tintenfarbe
  des Nutzers). Beides in eine Methode zu zwingen gäbe an einer der zwei Stellen das falsche
  Verhalten.

**Neu aus §4.52/§4.53 — Avalonia-XAML und die zweite Tür**

- **Ein Suffix wie `px` in einem XAML-Maß wirft zur LAUFZEIT.** Avalonia liest jeden Wert
  einer `RelativeRect`/`RelativePoint` als `double`; ohne Suffix gilt
  `RelativeUnit.Absolute` (Pixel), mit `%` der relative Fall. **Der Bau merkt davon nichts** —
  XAML-Attribute werden erst beim Laden geparst, und der Fehler kommt als Dialog, wenn der
  Nutzer die Ansicht öffnet, die den Pinsel benutzt. In §4.53 genau so passiert.
- **`x:Name` an einem Pinsel oder Verlaufsstopp erzeugt kein Feld** — nur Controls landen im
  Namescope, anders als in WPF. Was sich zur Laufzeit ändern soll, wird entweder im Code
  gesetzt oder hängt an einem benannten **Control**.
- **`ImageBrush.Source` nimmt kein `DrawingImage`.** Für gekachelte Zeichnungen ist
  `DrawingBrush` der richtige Typ — er heißt wie sein WPF-Gegenstück und kann dasselbe.
- **Wer die Einstellungsleiste an einer neuen Stelle aufklappt, ruft `EinstellungenSpiegeln`.**
  Sonst geht sie mit **lauter leeren Umschaltern** auf. Das steht seit Phase 3 in
  `Einstellungen_Click` — und ist in §4.53 an der **zweiten** Aufklappstelle prompt wieder
  passiert. *Ein Warnhinweis nützt nur an der Stelle, an der jemand hinsieht:* wer einen
  Zustand an zwei Türen öffnet, gehört an beide Türen geschrieben.
- **Vor dem Portieren eines Werkzeugs prüfen, ob sein Symbol im Satz steht.** Der WPF-Kopf
  behilft sich an einzelnen Stellen noch mit **Unicode-Zeichen in „Segoe UI"** — bei den
  Formen war es so (§4.53, umgestellt). Unter Linux wäre jedes davon ein leeres Kästchen.

**Neu aus §4.51 — ein neuer Zug muss sich anmelden**

- **`WhiteboardView.InputInProgress` im Linux-Kopf entscheidet, ob eine Zeigerbewegung
  überhaupt weitergereicht wird.** Steht ein Zug nicht darin, gilt jede Bewegung als
  **bloßes Schweben über der Fläche** — der Zug beginnt, der Zustand wird gesetzt, und
  danach passiert **nichts**. **Es gibt keinen Fehler und keinen roten Wächter:** in §4.51
  baute es mit 0 Fehlern, alle 869 Tests blieben grün, und am Bildschirm bewegte sich das
  Element keinen Pixel. **Wer ein Werkzeug ergänzt, das über die Fläche zieht — und Phase 4.5
  ergänzt lauter solche —, trägt es dort ein.** Der WPF-Kopf hat dieselbe Abfrage
  (`WhiteboardView.Input.cs`, oben).
- **Die Griff-Geometrie liegt seit §4.51 in `WbHandles`, die Zeichnung in
  `WbSelectionRenderer`** — beides in Core, beide Köpfe rufen es. ✅ **`WbHit` behauptete in
  seinem Kopfkommentar das Gegenteil** („…, der Zustand und die Griffe" blieben im Kopf);
  **am 2026-08-28 richtiggestellt** (Phase 5, Punkt 2). *Der Satz stand hier seit §4.51 als
  „ist dort überholt" — und genau so lange stand er auch weiter im Code.* **Die Regel
  dahinter:** die *Wahl* einer Toleranz darf im Kopf liegen, die *Formel* nicht — `Zoom` ist
  kein Steuerelement, sondern eine Zahl, und als Parameter übergeben bleibt reine Geometrie
  übrig.
- **`ShowQuickMenuForSelection` im WPF-Kopf legt die Schnellaktionen auf `tl.Y - h - 10`** —
  mittig **genau auf den Drehgriff**. Erst wenn oben kein Platz ist, rutscht das Menü
  darunter (`WhiteboardView.QuickMenu.cs`, Zeile 152). **Das kostet beim Augenschein leicht
  eine halbe Stunde**, weil der Griff auf der Aufnahme schlicht fehlt und wie ein
  Zeichenfehler aussieht. ✅ **Der Satz „wer die Schnellaktionen in den Linux-Kopf portiert
  (§6, Stück 5), sollte das nicht mitnehmen" ist eingelöst** — Stück 5 ist in §4.62 gelaufen,
  und der Linux-Kopf hat das Aufklappen nach frischer Auswahl **bewusst weggelassen**; sein
  Kopfkommentar in `WhiteboardView.Schnellaktionen.cs` sagt das mit Begründung, und
  `SchnellaktionenTests` rechnet die Überschneidung nach. **Die erste Hälfte des Eintrags gilt
  unverändert** und ist der Grund, aus dem der Drehgriff im WPF-Kopf auf einer Aufnahme fehlen
  kann. *(Am 2026-09-04 nachgesehen.)*

**Neu aus Phase 3 — der Avalonia-Kopf**

- **`AvaloniaXamlLoader.Load(this)` füllt die `x:Name`-Felder nicht.** Beide bauen den
  Oberflächenbaum auf, aber nur das erzeugte `InitializeComponent()` weist danach die
  Felder zu. Mit dem Lader direkt bleibt **jedes** davon `null`, und der erste Zugriff wirft
  eine `NullReferenceException` an einer Stelle, die mit der Ursache nichts zu tun hat — hier
  war es eine Zeile, die nur einen Menühaken setzt. **Merksatz: im Code-Behind immer
  `InitializeComponent()`.** (Die Vorlage von `dotnet new avalonia.app` benutzt an einer
  Stelle den Lader — das gilt für die `Application`, die keine benannten Felder hat.)
- **Avalonia hält die Quelle einer Bindung nicht am Leben — das ist die teuerste Lektion
  dieses Brockens.** Der erste Anlauf für `{loc:T …}` legte je Bindung einen Träger an und
  hielt ihn *schwach*, damit nichts festhängt. Es funktionierte — bis zum ersten
  vollständigen Sammellauf. Den erzwingt `MainViewModel.ReleaseMemory` beim Schließen jeder
  Registerkarte; danach waren alle Träger weg, und der nächste Sprachwechsel erreichte nur
  noch die Texte, die der Code selbst schreibt. **Das Fehlerbild war eine halb übersetzte
  Oberfläche** — Pfadleiste und Datumsangaben deutsch, Menüleiste englisch — und es trat
  erst beim *zweiten* Wechsel auf. Behoben über **einen Träger je Schlüssel**, stark
  gehalten (`Services/Localization/LocText.cs`): die Zahl der Übersetzungsschlüssel ist
  begrenzt, die Zahl der Bindungen nicht.
- **Übersetzung im Linux-Kopf: Avalonia frischt eine Indexer-Bindung nicht auf.** Der
  WPF-Kopf bindet auf `Loc.Source["Schlüssel"]` und bekommt beim Sprachwechsel ein
  `PropertyChanged` mit **leerem Namen** — WPF versteht das als „alle Eigenschaften neu
  lesen" und wertet auch Indexer neu aus. Avalonia tut das nicht: `Loc.Current` war
  umgestellt, der Haken im Sprachmenü sprang, und **jeder** Text blieb stehen. Deshalb
  bindet der Avalonia-`TExtension` auf eine ganz gewöhnliche Eigenschaft (`LocText.Value`)
  und nicht auf einen Indexer. **`LocSource` in Core ist bewusst unverändert geblieben** —
  die Zusage „leerer Name = alles" ist eine WPF-Zusage.
- **Der Avalonia-Kopf zeigt unter Windows auf denselben Datenordner wie der WPF-Kopf**
  (`%APPDATA%\GonkNote`). Das ist Absicht — es ist dieselbe App mit denselben Daten, und nur
  so lassen sich beide Köpfe vergleichen. **Zum Prüfen deshalb immer `--db` mit einer
  Kopie**, sonst greift ein Testlauf auf den echten Bestand zu (Dauerregel 4).
- **`Core/Platform/` ist synchron, Avalonia ist es nicht.** Jeder Dialog läuft über
  `Platform/Modal.cs` und einen verschachtelten Nachrichtenlauf (`Dispatcher.PushFrame`).
  **Nur vom Oberflächen-Faden aufrufen und nur für etwas, worauf der Nutzer ohnehin
  wartet.** Wer ihn um eine lange Rechnung legt, bekommt Wiedereintritt an einer Stelle, an
  der niemand damit rechnet.
- ✅ **~~`IClipboard.HasImage` kodiert im Avalonia-Kopf wirklich.~~** *(Stand bis V2-84 —
  **stimmt seit dem 2026-08-25 nicht mehr**, hier aber bis zum 2026-08-28 stehen geblieben.)*
  Der alte Wortlaut: „Unter Windows sieht `Clipboard.ContainsImage()` nur nach; Avalonia hat
  kein ‚enthält' ohne ‚hol es'. Heute folgenlos — **wer sie künftig in einer Schleife
  aufruft, sollte es wissen.**"

  **Genau das ist eingetreten** (§4.62): Die Schnellaktionen fragen bei **jedem** Öffnen, ob
  Einfügen etwas zu tun hätte. `AvaloniaClipboard.HasImage` nimmt seitdem
  **`GetDataFormatsAsync`** und fasst die Daten gar nicht mehr an — *keine Näherung, sondern
  genau das, was die Ablage über sich selbst aussagt.* **Die Warnung hat funktioniert und ist
  eingelöst.**

  > **▶ Das Paar dazu steht ein paar Absätze weiter oben, und zusammen ergeben sie die
  > eigentliche Lehre von Phase 5, Punkt 2:** Bei `WbHit` wusste **die Doku**, dass der
  > **Code**-Kommentar falsch war, und ließ ihn drei Runden stehen. Hier war es umgekehrt —
  > der **Code** war richtiggestellt und **die Doku** warnte weiter vor dem behobenen
  > Zustand. *Eine Stelle richtigzustellen heißt nicht, die andere mitzuziehen; und welche
  > von beiden man liest, entscheidet der Zufall.*
- **`ApplicationIcon` und `ApplicationManifest` sind Windows-Artefakte.** Beide stehen in
  `GonkNote.Avalonia.csproj` unter einer OS-Bedingung, damit der Linux-Lauf der CI nicht an
  einer Windows-Eigenheit hängen bleibt.
- ⛔ **~~Die Icon-Schrift gibt es unter Linux nicht — wer im Avalonia-Kopf ein Symbol braucht,
  legt eine Vektorform in `Themes/Styles.axaml` an (16×16, gestrichen); `IconGlyph` aus dem
  ViewModel ist die Antwort des *Windows*-Kopfs und bleibt dort.~~** *(Stand bis §4.31 —
  **stimmt seit dem 2026-08-12 nicht mehr**, hier aber bis zum 2026-09-04 stehen geblieben,
  weil **jeder darin genannte Name noch existiert**: `Themes/Styles.axaml` gibt es, `IconGlyph`
  gibt es als Wort. §4.69 konnte das mit seinen drei Messungen nicht finden — genau der Fall,
  den es dort als „die Mehrheit" benannt hat.)*

  **Der erste Satz stimmt weiter, der Rat darunter ist die Falle.** Seit §4.31 steht der
  Symbolsatz **einmal in Core**: `Core/Theming/AppIcon.cs` (die Aufzählung) und
  `Core/Theming/Icons.cs` (die Pfaddaten), **am 2026-09-04 nachgezählt: 76 Symbole, in beiden
  Tabellen gleich viele**. `Themes/Styles.axaml` enthält **keine einzige** Symbolgeometrie
  mehr. Beide Köpfe malen sie über ein `Symbol`-Steuerelement (`<views:Symbol Icon="…"/>`, im
  Linux-Kopf an 113 Stellen), und die Zuordnung *Dokumentart → Symbol* ist
  `AppIcons.ForKind` — **von beiden `KindToIconConverter` gerufen, nicht nur vom WPF-Kopf**.

  > **⛔ Wer dem alten Rat folgt, baut das 77. Symbol in einen Kopf allein** — also genau die
  > Doppelung wieder auf, die §4.31 aufgelöst hat, und der Compiler sagt dazu nichts. **Der
  > richtige Weg: einen Wert an `AppIcon` anhängen und die Pfaddaten in `Icons.cs` dazu.**
  > `IkonentabelleTests` hält beide Tabellen zusammen, `IkonenimKopfTests` prüft den WPF-Kopf.
  >
  > *Und das ist die Kehrseite von `AvaloniaClipboard.HasImage` weiter unten: dort war der
  > **Code** richtiggestellt und die Doku warnte vor dem behobenen Zustand — hier hat die Doku
  > eine **Anweisung** überlebt, deren Grund weggefallen ist. Eine veraltete Warnung kostet
  > eine Nachfrage; eine veraltete **Anweisung** kostet den Umbau, den sie anordnet.*
- **`DrawingBrush` hat in Avalonia keine Inhaltseigenschaft.** `<DrawingBrush><GeometryDrawing/></DrawingBrush>`
  scheitert mit `AVLN2000: Internal compiler error: Index was out of range` — eine Meldung,
  die auf alles Mögliche hindeutet, nur nicht auf die Ursache. Es braucht ausdrücklich
  `<DrawingBrush.Drawing>`.
- **`Render` läuft *im* Renderdurchlauf — dort darf kein anderes Steuerelement angefasst
  werden.** Das ist die teuerste Falle des zweiten Brockens. Unter WPF läuft
  `SKElement.PaintSurface` außerhalb, und die Zeichenfläche darf von dort nebenbei die
  Farbkachel und die Zoom-Anzeige nachführen — der WPF-Kopf tut genau das. Avalonia wirft
  dafür **`InvalidOperationException: Visual was invalidated during the render pass`**, und
  zwar nicht nur für die eine Zuweisung: **der ganze Durchlauf bricht ab.**
  **Das Fehlerbild zeigt überall hin, nur nicht auf die Ursache** — hier war es eine leere
  Werkzeugleiste und ein leeres Blatt, während der Fehler in einer Zeile stand, die eine
  Farbkachel einfärbt. **Merksatz: im Zeichenpfad wird gezeichnet, sonst nichts.** Was
  aufgefrischt werden muss, hängt an dem Ereignis, das es auslöst (Seitenwechsel,
  Theme-Wechsel, Größenänderung) — und die Größe ist ohnehin das ehrlichere Ereignis für
  „jetzt kenne ich meine Breite" als das erste Bild.
- **`ICustomDrawOperation.Render` läuft auf dem Render-Faden, nicht auf dem
  Oberflächen-Faden.** Wer von dort in lebende Zustände greift (`_page.Elements`, die Punkte
  des laufenden Strichs), liest eine Liste, die der andere Faden gerade verändert. Das
  Ergebnis wäre kein sauberer Absturz, sondern ein sporadisches
  `InvalidOperationException: Collection was modified` mitten im Zeichnen — und zwar
  bevorzugt beim Zeichnen langer Striche, also genau dann, wenn niemand es reproduzieren
  will. **Gegenmittel: aufzeichnen statt zurückgreifen** (`SKPictureRecorder` auf dem
  Oberflächen-Faden, `DrawPicture` auf dem Render-Faden, §4.10).
- **Ziehen läuft auch innerhalb der App über XDND — also über eine Prozessgrenze.**
  `DragDrop.DoDragDropAsync` reicht die Fracht an den Fenstermanager weiter; ein
  .NET-Objektverweis überlebt das nicht, nur ein Datenstrom. **Gegenmittel:
  `DataFormat.CreateInProcessFormat<T>(...)`** — ein solches Format verlässt Avalonia gar
  nicht erst und reicht den echten `TreeItemViewModel` durch. Wer stattdessen eine Kennung
  schickt, muss den Eintrag auf der Gegenseite im Baum wiederfinden, und das ist genau die
  Sorte Arbeit, die man sich hier sparen kann.
- **`DoDragDropAsync` verlangt ausdrücklich `PointerPressedEventArgs`**, nicht irgendein
  Zeigerereignis — es braucht den Zeiger, der noch aufliegt. Ein Ziehen mit Schwelle muss
  den **Druck festhalten** und ihn erst benutzen, wenn der Zeiger weit genug gewandert ist.
  Die Schwelle selbst steht als Zahl im Code (6 px): WPF fragt dafür
  `SystemParameters.MinimumHorizontalDragDistance`, Avalonia hat keine Auskunft dazu.
- **Ein anklickbarer Verweis ist in Avalonia ein Steuerelement, kein ausgezeichneter Text.**
  `Run` kennt kein Klickereignis, und einen `Hyperlink` wie in WPF gibt es nicht. Der Weg
  führt über **`InlineUIContainer`** — ein `TextBlock` mitten im Fließtext. Der bricht in
  sich nicht um; bei Verweistexten von wenigen Wörtern fällt das nicht ins Gewicht.
- **Zwei Umbenennungen in Avalonia 12, die der Compiler meldet** — hier notiert, weil jede
  Anleitung im Netz noch die alten Namen zeigt: `SystemDecorations` ist veraltet
  (→ **`WindowDecorations`**), und `IDataObject`/`DataObject` sind ersetzt durch
  **`IDataTransfer`/`DataTransfer`** (`DataObject` existiert noch, tut aber nichts mehr).
- **Ein `UserControl` fokussierbar zu machen genügt nicht für Tastenkürzel.**
  Tastenereignisse laufen in Avalonia vom Wurzelfenster zum **fokussierten Element** und
  zurück. Liegt der Fokus noch im Ordnerbaum, kommt am Zeichenbereich nichts an, auch wenn
  der Rahmen darum `Focusable` ist und `Focus()` gerufen wurde. **Die Fläche selbst muss
  fokussierbar sein und den Fokus bekommen.** Das Fehlerbild ist unauffällig und deshalb
  teuer: gezeichnet wird einwandfrei — der Zeiger braucht keinen Fokus —, nur die
  Tastenkürzel tun nichts, und das hält man erst einmal für einen Fehler in der Tastenlogik.
- **Ein Bildlauf lässt den Inhalt nicht neu zeichnen** (§4.28). Der `ScrollViewer`
  verschiebt sein Kind, ohne dessen `Render` erneut zu rufen. Solange die Fläche alles
  aufzeichnet, fällt das nicht auf; **wer nur den sichtbaren Ausschnitt aufzeichnet** — und
  das muss man bei einem dreißigseitigen Dokument —, bekommt beim Rollen einen leeren Rand,
  und das sieht aus, als höre das Dokument auf. **Gegenmittel: `ScrollChanged` →
  `InvalidateVisual()`.** Kostet eine Aufzeichnung je Bildlaufschritt, und eine Aufzeichnung
  ist billig (es werden Befehle notiert, nichts gerastert).
- **Der sichtbare Ausschnitt steht im `ScrollViewer`, nicht in `Bounds`** (§4.28). Die
  Leinwand *ist* der ganze Seitenstapel — ihre Grenzen sagen über den Ausschnitt nichts. Wer
  gegen `Bounds` prüft, zeichnet immer alles und hat nichts gespart.

**Neu aus §4.41 — die Eingabe-Naht**

- **Zwei Zählweisen stehen im selben Namensraum nebeneinander, und sie sind beide richtig.**
  `TdCursor.Text` zählt **Zeichen** (ein Feld steuert keines bei — im Dokument steht ein Feld
  und keine Zahl), `TdEingabe.Text` zählt **Cursorschritte** (jedes unteilbare Stück bekommt
  eines, Feld/Bild/Diagramm als U+FFFC). **Wer sie je zusammenlegt, bricht eine von beiden.**
  Beide tragen den Grund im Quelltext, `EingabeUmfeldTests` hält den Unterschied als Wächter
  fest — und die Gleichung `TdEingabe.Text(absatz).Length == TdCursor.Laenge(absatz)` gleich
  mit. **Das Fehlerbild wäre kein Wurf, sondern ein Zeichen, das die Bildschirmtastatur hinter
  jedem Feld um eins verschoben einsetzt.**
- **Ein angemeldetes Eingabeziel ändert unter Windows den Weg, den getippter Text nimmt** —
  Avalonias Win32-Rücken geht auf **TSF**. Wer an `TextDocView.Eingabemethode.cs` etwas
  ändert, hat damit **den Eingabepfad** angefasst, auch wenn es nicht danach aussieht: Die
  Gegenprobe ist tippen und den **Zeichenzähler** unten ablesen, nicht „sieht gut aus"
  (§4.41: 48 → 63, exakt +15).
- **Eine Eigenschaft des Eingabeziels darf nicht werfen.** Avalonia fragt sie aus dem
  Eingabepfad heraus, also aus einem Faden, den niemand fängt. Deshalb klemmt `TdEingabe`
  jeden Abstand, zieht jede Stelle gerade und liefert bei einem Dokument ohne Absätze eine
  **leere Auskunft** statt einer Ausnahme — und `SurroundingText` gibt `""` und nicht `null`,
  auch bevor etwas geladen ist.
- **Die Bildschirmtastatur wird nur für Finger und Stift angefordert.** Wer das auf alle
  Zeiger ausweitet, bekommt bei **jedem Mausklick** ein halbes Fenster über das Blatt
  geschoben — und es fällt niemandem als Fehler auf, weil es ja „funktioniert".

**Neu aus V2-62 — Messwerkzeuge unter Wayland/XWayland**

- **`setxkbmap -query` lügt unter XWayland, und `_XKB_RULES_NAMES` lügt mit.** Beide meldeten
  auf dem Laptop `layout: us`, während der tatsächlich geladene Keymap **deutsch** ist —
  `xkbcomp -xkb :0 -` zeigt `xkb_symbols "pc_de_de_2_inet(evdev)"` mit
  `key <TLDE> = dead_circumflex`. **Wer den Keymap wissen will, fragt `xkbcomp`, nicht
  `setxkbmap`.** Das ist nicht akademisch: AvaloniaUI/Avalonia#18596 wollte als Erstes genau
  diese Auskunft sehen, und sie hätte die Fehlersuche in die falsche Richtung geschickt.
- **`xtrace` heißt auf Arch/CachyOS nicht `xtrace`.** `/usr/bin/xtrace` **existiert**, gehört
  aber zu **glibc** und ist ein Bibliotheks-Tracer — mit X11 hat er nichts zu tun. Er meldet
  sich mit `Unable to find 'all.proto' in search path!`, was wie ein fehlendes Paket aussieht
  und keines ist. Der **X11-Protokoll-Tracer** liegt nur im AUR und wird dort wegen genau
  dieses Konflikts als **`x11trace`** installiert. Ohne AUR-Helfer: Quelle 1.4.0 von
  `deb.debian.org` holen, Prüfsumme gegen den AUR-`PKGBUILD` halten, mit **eigenem Präfix**
  bauen (`--prefix=/tmp/…`) — **ohne Präfix findet er seine `.proto`-Dateien nicht**, weil er
  ausschließlich in seinem `PKGDATADIR` sucht. **Und `-n` ist unter Wayland Pflicht**: sonst
  bricht er beim Start an `xauth remove` ab, denn es gibt keine `~/.Xauthority` — mutter fährt
  eine eigene unter `/run/user/…/.mutter-Xwaylandauth.*`.
- **`pkill -f <Muster>` bringt die eigene Shell um, wenn das Muster im eigenen Befehl steht.**
  In V2-59 traf es `dbus-monitor`, in V2-62 `pkill -f "GonkNote.Avalonia"` — die Befehlszeile
  der Shell enthält das Muster, und der Aufruf killt sich selbst (Exit 144, kein Hinweis
  worauf). **Der Ausweg ist der Klammer-Trick:** `pkill -f "[G]onkNote[.]Avalonia"` — der
  reguläre Ausdruck trifft den Prozess, aber nicht die eigene Befehlszeile, in der ja die
  Klammern stehen. **Gilt für `pgrep` genauso.**
- **`xev` schreibt in seine Standardausgabe, nicht auf den Schirm.** Wer sein Fenster
  anklickt und tippt, sieht **nichts** und hält die Messung für gescheitert — die Ereignisse
  stehen in der Datei, in die umgeleitet wurde. **Ein leerer Schirm ist hier kein Befund.**

**Neu aus Phase 4 — die Anzeige als Prüfmittel**

- **Eine Anzeige findet, was ein Textvergleich nicht sehen kann** (§4.28). Der Fehler mit der
  doppelten Tabellen-Kopfzeile stand vier Runden lang da und war von **jedem** Wächter
  gedeckt: die Ausgabe-Wächter lesen Text zurück (zweimal „Spalte 1" fällt nicht auf), der
  Umbruch-Wächter sah auf `TableRows[0]` (die Doppelung stand an Index 1), und der
  Zeichner-Wächter prüft Farbe an gerechneten Orten statt Zeilen zu zählen. **Merksatz: wenn
  eine Rechnung zum ersten Mal ein Bild ergibt, ist der erste Augenschein ein Prüfschritt und
  keine Vorführung** — dieselbe Rolle, die §4.13 dem A/B-Bildvergleich gibt.
- **Ein Wächter, der nur das erste Element prüft, deckt die zweite Stelle nicht.**
  `Assert.False(TableRows[0].IsRepeatedHeader)` war richtig und trotzdem blind. Wo eine
  Anzahl zur Aussage gehört, gehört sie in den Wächter — hier: `Assert.Equal(4,
  seite.TableRows.Count)`.

**Neu aus §4.67 — der Umbruch, und ein Kommentar, der nie gestimmt hat**

- **⛔ „In der Anzeige falsch, im PDF richtig" gibt es an dieser Stelle nicht.** Beide Wege
  gehen durch **denselben** `TdLayout.Umbrechen` und **denselben** `TdRenderer.Seite`; der
  einzige Unterschied ist der Maßstab (`TdRenderer.PixelProCm` gegen `TdPdf.PunktProCm`).
  **Und der ist als wirkungslos gemessen:** Skia skaliert linear (0,00 px Unterschied), und
  Stück für Stück an den Umbruchstellen gezeichnet ist **pixelgleich** mit einem einzigen
  `DrawText`. Wer trotzdem einen Unterschied sieht, hat entweder zwei verschiedene Dokumente
  vor sich oder etwas anderes gefunden — **er hat jedenfalls keine Erklärung im Maßstab.**
- **Ein Stück (`TdLaidOutRun`) ist kein Wort.** Es entsteht an jedem Wort *und* an jedem
  Formatwechsel. **Jede Rechnung, die Stücke zählt und Wörter meint, ist falsch** — der
  Blocksatz hat das zwanzig Runden lang getan und an jeder Formatgrenze einen
  Wortzwischenraum eingeschoben, den es im Text nicht gibt. `TdLayout.Luecken` zählt seit
  §4.67 die **Leerraumfolgen im Text der Zeile**; wer dort etwas anbaut, zählt Leerraum und
  nicht Grenzen.
- **▶ Und die Falle dahinter, die keine Datei kennt:** Der Fehler stand als **Kommentar**
  daneben — „es gibt einen Zwischenraum weniger als Stücke" — und las sich wie eine
  Selbstverständlichkeit. **§7 kannte bisher nur den veralteten Kommentar** (die drei Fälle
  aus §4.60/§4.61/§4.62). **Dieser war von Anfang an falsch.** Ein Kommentar, der eine
  Annahme ausspricht, ist der beste Ort, sie zu prüfen — und der gefährlichste, sie zu
  glauben.

**Neu aus Phase 3 — die Farbtabelle**

- **Die Reihenfolge in `ThemeColor` ist Teil des Formats.** `ThemeDefinition` legt die Werte
  in einem Feld dieser Länge ab und greift über `(int)` darauf zu. **Neue Farben nur hinten
  anhängen** — wer eine dazwischenschiebt, verschiebt alle folgenden Werte gespeicherter
  Tabellen.
- **Dieselben zwanzig Farben stehen an zwei Stellen** (Core-Tabelle und die beiden
  WPF-`ResourceDictionary`-Dateien), weil der WPF-Kopf bewusst nicht umgestellt wurde.
  `FarbtabelleTests` hält sie zusammen — **in beide Richtungen**. Wer eine Farbe ändert,
  ändert beide Stellen oder bekommt einen roten Lauf.
  **Das ist die letzte verbliebene Doppelung dieser Art** — `WbHit` und die
  Markdown-Grammatik sind mit §4.13 zusammengelegt. Sie ist aber eine andere Sorte: dort
  standen zwei **Rechnungen**, hier stehen zwei **Datentabellen**, und ein Wächter hält sie
  wirklich zusammen. Sie fällt mit den eigenen Farbschemata (§6), nicht vorher.
- **Die Vorgabetinte gehört zum Papier, nicht zur App.** Eine Notizbuchseite ist
  standardmäßig `PageShade.Light` — **unabhängig vom App-Theme**, denn Papier soll wie
  Papier aussehen (V1-Vorgabe „Dark/Light bei hellem Papier", §1). Wer `DefaultInk` aus der
  *aktiven* Tabelle nimmt, holt sich im Dunkelmodus deren helles `DefaultInk` und schreibt
  **hell auf weiß**. Genau so passiert und beim Gegenprüfen aufgefallen: der Strich ist da,
  gespeichert und exportierbar, nur unsichtbar — auf einem Bildschirmfoto sieht es aus, als
  käme die Eingabe nicht an, und man sucht den Fehler im Eingabepfad.
  **Die Regel ist dieselbe wie beim Papier selbst:** bei `PageShade.Auto` folgt die Seite
  dem Theme, also auch die Tinte; bei festgelegtem Farbton zählt der, also die mitgelieferte
  Tabelle dazu.
- ⛔ **~~Der WPF-Kopf umgeht das, indem er an dieser Stelle Schwarz und Weiß fest verdrahtet —
  richtig im Ergebnis, aber an der Farbtabelle vorbei.~~** *(Stand bis §4.79 — **seit dem
  2026-08-31 behoben**, hier aber bis zum 2026-09-04 stehen geblieben.)* **Und der Nachsatz
  war schon beim Hinschreiben falsch:** „richtig im Ergebnis" war es **nicht**. Mit derselben
  Kachel „auto" schrieb der WPF-Kopf `#FF000000`, der Linux-Kopf `#1B2B4B` — **derselbe Strich
  hatte je nach Kopf eine andere Farbe im Dokument**, und das ist keine Frage des Aussehens,
  sondern der **gespeicherten Daten**. Gesehen hat es erst der Vergleich beider Köpfe
  nebeneinander (§4.78), nicht das Lesen. Heute nimmt `WhiteboardView.Shapes.AutoTinte()` die
  Tabelle aus Core, in **beiden** Köpfen.

  **Und dieselbe Doppelung lag eine Etage tiefer noch einmal:** `TextStyles.InkLight`/`InkDark`
  im WPF-**Texteditor** waren dieselben zwei Werte als Literale, mit dem Kommentar „entsprechen
  `Color.DefaultInk`" daneben. Sie entsprachen ihnen auch — *bis jemand die Tabelle ändert*.
  **In Phase 5, Schritt ④ auf die Core-Tabelle gelegt** (abgeleitet, nicht durch einen Wächter
  eingefroren: ein Wächter hätte die Doppelung festgeschrieben statt sie zu beseitigen).

  > **▶ Die Lehre daraus ist nicht „Farben gehören in eine Tabelle" — das wusste jeder.** Es
  > ist: *ein Kommentar, der eine Doppelung als gewollt erklärt („entsprechen …", „richtig im
  > Ergebnis"), schützt sie vor dem Aufräumen.* Beide Stellen sind bei jeder Suche nach
  > Doppelungen mitgelesen und beide Male für erledigt gehalten worden.

**Neu aus Phase 2 — Fernsteuern**

- **`SetForegroundWindow` schließt jedes offene Menü.** Ein Skript, das je Klick erst das
  Fenster nach vorn holt, kommt über den ersten Menüeintrag nie hinaus: das Popup ist beim
  zweiten Aufruf schon zu, und der Klick landet auf dem, was darunter liegt. Deshalb gibt es
  `tools\kette.ps1` — es fokussiert **genau einmal** und klickt dann die ganze Kette.
  `tools\klick.ps1` bleibt für Einzelschritte.
- **Ein modaler Dialog ist ein eigenes Fenster.** Für `SendKeys` an einen Datei- oder
  Meldungsdialog darf man das Hauptfenster **nicht** nach vorn holen — sonst tippt man in
  die Zeichenfläche. Erst in den Dialog klicken, dann senden. (Genau so ist in dieser Runde
  ein Geodreieck auf einer Notizbuchseite gelandet.)
- **Das Werkzeug klickt in die Zeichenfläche, wenn man sich vertut** — und der Stift ist
  meistens aktiv. Auf einer **Kopie** ist das folgenlos, auf der echten Datenbank wäre es
  ein Datenverlust. Das ist der praktische Grund für Dauerregel 4, nicht nur der
  theoretische.
- **Ein Doppelklick auf eine Galerie-Kachel malt einen Punkt in die Zeichenfläche**, wenn
  das Dokument schneller aufgeht als der zweite Klick kommt. Über den Baum öffnen ist
  sicherer.

**Neu aus §4.31 — Symbole und Namensräume**

- **Ein Namensraum, der wie das Framework heißt, verdeckt das Framework.** Eine neue Datei im
  Avalonia-Kopf stand versehentlich in `GonkNote.Avalonia.Views` (dem Assemblynamen). Damit gab
  es unterhalb von `GonkNote` ein `Avalonia`, und in **jeder anderen Datei** des Kopfs zeigte
  `Avalonia.Interactivity` plötzlich ins Leere — mit einer Fehlermeldung, die auf die *fremde*
  Datei zeigt und nicht auf die neue. **Der Kopf heißt im Quelltext `GonkNote.Views`**, und zwar
  aus genau diesem Grund.
- **Beim Aneinanderhängen von SVG-Pfaden ist ein kleines `m` nicht dasselbe wie ein großes** —
  und **groß machen reicht nicht**, denn nach einem großen `M` sind Folgezahlen absolute
  LineTos. Richtig ist ein vorangestelltes `M0,0`. Beides erzeugt **gültige** Pfadangaben, die
  etwas anderes zeichnen; kein Parser meldet das.
- **Ein deutsches Dezimalkomma ist in einer Pfadangabe ein Trennzeichen.** Wer Zahlen aus
  PowerShell in Geometrie schreibt, stellt vorher `CurrentCulture` auf invariant.
- **`SKPath.Bounds` misst die Stützpunkte einer Kurve, nicht die Kurve.** Bei einem
  elliptischen Bogen liegt das um Einheiten daneben. Für die tatsächliche Ausdehnung
  **`TightBounds`**.
- **Symbole prüft man am Bild, nicht am Quelltext.** Ein Kontaktblatt mit allen Formen
  nebeneinander hat in §4.31 drei Fehler gefunden, die Lesen und Parsen beide durchgelassen
  hatten.

**Neu aus §4.13 — Fernsteuern, Ziehen und Untermenüs**

- **Ohne Ziehen lässt sich die Auswahl gar nicht fernsteuern.** Lasso und Verschieben sind
  Ziehbewegungen; ein Klick erzeugt keine Auswahl. `kette.ps1` kann das seit §4.13 mit einem
  Schritt der Form `"x1,y1>x2,y2>…"`. **Zwischen den Stützpunkten muss interpoliert werden**
  — die App sammelt ihre Lassopunkte aus Mausbewegungen, ein einzelner `SetCursorPos`-Sprung
  ergäbe eine Gerade statt einer Umkreisung.
- **Die PowerShell-Pipeline entpackt verschachtelte Arrays.**
  `@($s -split '>' | ForEach-Object { [int[]]@($t[0], $t[1]) })` ergibt **keine** Liste von
  Paaren, sondern eine flache Liste von Zahlen — `SetCursorPos` bekommt dann ein Array, wo
  eine Zahl hingehört, und meldet „Die Argumenttypen stimmen nicht überein" an einer Zeile,
  die richtig aussieht. **Gegenmittel: zwei getrennte Achsen-Arrays** (`$xs`, `$ys`) statt
  einer Liste von Paaren.
- **Ein Klick auf einen Untermenü-Eintrag *schließt* das Untermenü, wenn der Zeiger es
  vorher schon per Hover geöffnet hat.** „Ansicht → Sprache → Englisch" scheiterte deshalb
  reproduzierbar: der dritte Klick machte das Untermenü zu, der vierte landete auf dem, was
  darunter lag — hier auf dem Notizbuch-Knopf der Seitenleiste, und es entstand ein
  Notizbuch. **Das Foto zeigt dabei die Ursache nicht**, weil das Untermenü zum
  Aufnahmezeitpunkt durch Hover wieder offen ist. **Gegenmittel: `-WaitMs` hochsetzen** (1400
  statt 700 hat gereicht) und danach am Ergebnis prüfen, nicht am Zwischenfoto.

  > **Nachtrag §4.29 (2026-08-11): dasselbe noch einmal, mit dem sichereren Gegenmittel.**
  > „Ansicht → Sprache → Deutsch" scheiterte wieder — der Klick landete diesmal auf einem
  > Ordner der Galerie und navigierte hinein. **Was zuverlässig geht: über den Untermenü-Kopf
  > nur *fahren* statt zu klicken** (`SetCursorPos`, ~900 ms warten), dann den Zieleintrag
  > klicken. Das öffnet das Untermenü per Hover und schließt es nicht wieder.
  > **`kette.ps1` kann das nicht** — jeder Schritt klickt. Bis es einen Schweb-Schritt gibt,
  > ist ein Untermenü ein Fall für ein paar Zeilen `SetCursorPos`/`mouse_event` von Hand.
- **Tastatur statt Maus hilft hier *nicht*.** Der naheliegende Ausweg
  (`{DOWN}{DOWN}{DOWN}{RIGHT}…`) ging ins Leere, weil ein Baumeintrag im Umbenennen-Modus
  stand und alle Tasten abfing. **Erst den Fokus klären, dann tippen.**
  **§4.29 hat denselben Ausweg ohne Umbenennen-Modus versucht — er ging auch dort ins Leere:**
  `SendKeys` schickt an das Fenster, das WPF-Menü lebt aber in einem eigenen Popup mit eigenem
  Fokus. **Der Weg über die Tastatur ist für WPF-Menüs damit zweimal gescheitert; er ist keine
  Reserve mehr.**
- **`{PGDN}` im `FlowDocumentScrollViewer` greift nur, solange der Fokus im Dokument liegt**
  — er geht schon beim nächsten `SetForegroundWindow` verloren, und dann rollt nichts mehr,
  ohne dass es wie ein Fehler aussieht (V1-Handoff §7 kennt das). **Zuverlässig ist der
  Rollbalken-Ziehgriff**, jetzt wo `kette.ps1` ziehen kann.

**Neu aus Phase 3 — Fernsteuern des Avalonia-Kopfs**

- **Avalonias Menüs und Flyouts sind eigene Fenster.** Sie liegen außerhalb von
  `GetWindowRect` des Hauptfensters und fehlen auf einer Fensteraufnahme **vollständig** —
  man sieht das geschlossene Fenster und hält das Menü fälschlich für nicht geöffnet. Dafür
  gibt es seit Phase 3 den Schalter **`-Voll`** an `kette.ps1`: er nimmt den ganzen
  Bildschirm auf. **Für jeden Menüpfad im Avalonia-Kopf Pflicht.** Der WPF-Kopf zeichnet
  seine Menüs in denselben Fensterbereich; dort fällt es nicht auf.
- **Ein offen gelassenes Menü rächt sich im nächsten Lauf.** `SetForegroundWindow` schließt
  das Popup, Avalonias Menü hält seinen Zustand aber noch — der nächste Klick auf die
  Menüleiste *schließt* dann, statt zu öffnen, und alle folgenden Schritte der Kette landen
  auf dem, was darunter liegt. In dieser Runde ist so ein Textdokument entstanden.
  **Gegenmittel: jede Kette mit `'#{ESC}'` beginnen.**
- **Fensterkoordinaten sind nicht Bildschirmkoordinaten.** Ein maximiertes Fenster ragt um
  die Rahmenbreite über den Bildschirm hinaus (hier ~13 px). Wer auf einer Fensteraufnahme
  misst und auf dem Bildschirm klickt, liegt um diesen Betrag daneben — bei einem großen
  Knopf egal, bei einem Menüeintrag nicht. **Mit `-Voll` gemessen stimmen beide überein.**
- **Beide Köpfe fotografieren:** `schau.ps1 -Kopf avalonia|wpf -Konfig Debug|Release`.
- **⚠ Ein Klick, der angenommen wird, ist noch kein Klick, der ankommt — und es kann von selbst
  wieder gehen.** Am 2026-08-17
  (§4.36) stellten die Skripte **keinerlei Mauseingaben** an den laufenden Kopf zu — und zwar
  ohne eine einzige Fehlermeldung: `schau.ps1` fotografierte richtig, `SetCursorPos` bewegte
  den Zeiger **nachweislich** (abgefragt: 151,1764 → 600,900), `SendInput` meldete **zwei
  angenommene Ereignisse**, und im Kopf geschah nichts — kein Hover, keine Auswahl, kein
  Themenwechsel. Auch ohne Sandbox nicht. Der Prozess war ansprechbar (`Responding = True`),
  das Fenster sichtbar und im Vordergrund.
  **Woran man es erkennt:** Das Foto danach sieht **exakt** aus wie das davor. Genau das ist
  die Falle — es sieht aus, als hätte die App den Klick verschluckt, und man sucht den Fehler
  in der App. **Erst das Werkzeug ausschließen** (§4.35, dort zweimal in Folge die Ursache):
  ein einziger Klick auf einen Knopf mit auffälliger Wirkung — „Erscheinungsbild wechseln"
  unten links im Avalonia-Kopf —, und wenn der nichts tut, ist die Kette tot und **jede
  Sichtprüfung Handarbeit**. Das gehört dem Nutzer gesagt, nicht umgangen.
  **Später in derselben Sitzung ging es wieder**, ohne dass an den Skripten etwas geändert
  wurde — die Sichtprüfung von §4.36 und §4.37 ist damit nachgeholt worden (§4.37). **Der
  Zustand ist also vorübergehend: bevor man ihn für dauerhaft hält, lohnt ein zweiter Versuch.**

**Neu aus Phase 3 — Fernsteuern unter Wayland (`tools/linux/`)**

> ⛔ **ZUERST LESEN — dieser ganze Block beschreibt die XTEST-Zeit, und die ist seit GNOME 50
> vorbei.** Ganz oben in §7 steht unter „Die Werkzeuge unter `tools/linux/` — was seit GNOME 50
> nicht mehr gilt", **warum `zeiger` nicht mehr klickt und nicht mehr tippt** und dass dafür
> **`ydotool`** genommen wird (§5 Nr. 20, §4.64, §4.96). **Was hier unten über das *Zustellen*
> von Ereignissen steht — Skalierungsfaktor der XTEST-Eingabeseite, `hervor` als erster Schritt,
> „ein Klick, der angenommen wird" — ist damit Vorgeschichte und keine Anleitung.**
>
> **Was aus dem Block weiter gilt, und es ist der größere Teil:** alles über das **Aufnehmen**
> und über die **Fenstergeometrie** — `wlschuss.sh` gegen `schau.sh`, die zwei
> Koordinatensysteme im Faktor 1,6, das halb außerhalb stehende Fenster, das veraltete
> `import`-Bild, die fehlenden Flyouts, und dass sich der **Stift** grundsätzlich nicht
> fernsteuern lässt. `zeiger hervor` und `zeiger fenster` tun es ebenfalls weiterhin.
>
> **▶ Er bleibt stehen und wird nicht gelöscht** (Regel von Schritt ④): Wer eines Tages auf
> eine Sitzung ohne das EI-Portal trifft, braucht ihn wieder — und die Zeit, die er gekostet
> hat, ist damit nicht zweimal zu bezahlen. *Aber ein Abschnitt, der eine tote Technik ohne
> Vorwarnung im Präsens erklärt, ist genau die Falle, die §4.69 an `AvaloniaClipboard.HasImage`
> beschrieben hat — nur größer: dort war es ein Absatz, hier sind es hundert Zeilen, und die
> Richtigstellung stand die ganze Zeit im selben Abschnitt, zweihundert Zeilen weiter oben.*
> *(Eingezogen am 2026-09-04, Phase 5 Schritt ④.)*

Drei Eigenheiten, über die auf diesem Laptop jeder stolpert. Alle drei haben Zeit gekostet,
und keine davon sieht wie ein Fehler aus.

- **XTEST-Koordinaten sind nicht die Koordinaten, die dabei herauskommen.** Unter
  GNOME-Wayland nimmt mutter die Ereignisse von XWayland entgegen und rechnet sie mit dem
  Skalierungsfaktor der Sitzung hoch: ein `XTestFakeMotionEvent` auf (500,500) setzt den
  Zeiger hier tatsächlich auf **(1000,1000)**. `XQueryPointer` meldet danach die *echte*
  Lage, `XGetGeometry` ebenfalls — **die Eingabeseite ist die einzige, die skaliert.**
  Das zeigt sich nicht als Fehler: es wird geklickt, es kommt auch an (die Leerlaufuhr der
  Sitzung springt zurück), nur eben an der doppelten Stelle. Man trifft scheinbar zufällig
  mal etwas und mal nichts und sucht den Fehler in der App.
  **Gegenmittel:** `zeiger` **misst** den Faktor beim ersten Zug, statt ihn anzunehmen.
- **Das X-Wurzelfenster ist unter Wayland kein verlässliches Abbild.** Eine Aufnahme davon
  zeigt alten Inhalt neben neuem — beim ersten Versuch sah es aus, als zeichne die App ihr
  Fenster **doppelt**. `import -window <id>` liest dagegen das Fenster selbst und liefert
  ein sauberes Bild. Dazu zwei Eigenheiten des hiesigen ImageMagick (7.1.2-29):
  `import -window root <datei>` scheitert mit „missing an image filename", obwohl der
  X-Delegat vorhanden ist — **mit einer Fensterkennung statt `root` läuft derselbe Aufruf
  durch**; und der GNOME-Weg über D-Bus (`org.gnome.Shell.Screenshot`) antwortet mit
  „Screenshot is not allowed". ~~der Portal-Weg braucht eine Rückfrage beim Nutzer.~~
  **Der Nachsatz über das Portal war falsch** (berichtigt 2026-08-18, V2-55): mit
  `interactive: false` fragt es **nicht** zurück, und es ist damit der Weg, der unter Wayland
  wirklich trägt — `tools/linux/wlschuss.sh`, siehe unten.
- **Nicht maximieren, während eine Kette läuft.** `super+Up` auf ein bereits maximiertes
  Fenster hebt die Maximierung auf; GNOME setzt es danach mit **anderer Geometrie** wieder
  zusammen (hier von 2560 auf 3072 px Breite). Alle vorher gemessenen Koordinaten sind dann
  falsch, und weil das Fenster trotzdem normal aussieht, sucht man lange. **Geometrie einmal
  mit `zeiger fenster` holen und danach nichts mehr an der Fenstergröße ändern.**
- **`import -window` liefert unter XWayland manchmal ein Bild von vorgestern.** Nicht nur
  das X-Wurzelfenster ist unzuverlässig (oben) — auch die Fensteraufnahme kann einen Stand
  zeigen, der mehrere Schritte alt ist. **Das Fehlerbild ist bösartig:** man klickt, das Foto
  zeigt keine Wirkung, man korrigiert die Koordinaten, klickt woanders hin — und in
  Wahrheit hat schon der erste Klick gesessen. In dieser Runde sind so vier Durchgänge
  draufgegangen; erst ein Klick auf „Design wechseln" hat gezeigt, dass die Eingaben längst
  ankamen.
  **Gegenmittel: nicht dem Bild glauben, sondern dem Vergleich.** Zwei Aufnahmen nacheinander
  hashen (`magick <datei> -format "%#" info:`); ändert sich der Hash nicht, ist entweder
  nichts passiert **oder das Bild ist alt** — dann eine Aktion mit unübersehbarer Wirkung
  auslösen (Theme wechseln) und erneut sehen.
- **Ohne `hervor` kommt kein Klick an — und niemand sagt es dir** (2026-08-11). Der Kopf läuft
  über XWayland und ist auf diesem Laptop oft das **einzige** X-Fenster; der Fokus liegt aber
  in der Regel auf einer **nativen Wayland-Oberfläche** (Terminal, Shell). Liegt die obenauf,
  nimmt sie den Klick entgegen. XTEST meldet trotzdem Erfolg, und die Aufnahme davor und danach
  ist **Pixel für Pixel identisch** — dasselbe Bild wie „nichts getroffen", nur dass Koordinaten
  korrigieren hier nichts hilft. **Erkennen:** `xprop -root _NET_ACTIVE_WINDOW` gegen
  `_NET_CLIENT_LIST` halten; steht dort eine Kennung, die in der Liste gar nicht vorkommt, ist
  es das. **Gegenmittel:** `zeiger hervor` als **erster Schritt jeder Kette** — es schickt
  `_NET_ACTIVE_WINDOW` an die Wurzel, statt sich den Fokus mit `XSetInputFocus` selbst zu
  nehmen (das ignoriert mutter und stapelt gleich wieder um).
- **Das Fenster kann halb außerhalb des Bildschirms stehen, ohne dass man es sieht**
  (2026-08-11). Nach dem Hervorholen stand es hier bei **x=-430** auf einem 3072 px breiten
  Bildschirm — die ganze Seitenleiste lag links draußen. **Auf der Aufnahme fällt das nicht
  auf:** `import` liefert nur den sichtbaren Teil, das Bild ist schmaler (2130 statt 2560) und
  sieht trotzdem vollständig aus. Wer darauf misst, misst auf einem beschnittenen Bild.
  **Gegenmittel:** `zeiger lage:256,-8` setzt es an eine feste Stelle; danach `zeiger fenster`
  und die Breite der Aufnahme mit `w=` vergleichen — **stimmen sie nicht überein, ist das Bild
  beschnitten.** Die y-Angabe von `lage:` ist um die Rahmenhöhe versetzt (hier 72 px), weil sie
  auf das Client-Fenster wirkt und nicht auf den Rahmen: `lage:…,-8` ergibt y=136.
- **Was sich so nicht prüfen lässt: der Stift.** XTEST erzeugt Maus- und Tastaturereignisse.
  Druck, Neigung und die Unterscheidung Stift/Finger entstehen im Digitizer und lassen sich
  nicht nachbilden — ein Zug mit `zeiger` prüft deshalb immer den **Rückfallpfad**, nie den
  Stiftpfad. Dafür gibt es **F9** (§4.10): die Anzeige schreibt hin, was wirklich ankommt,
  und ist auf einem Foto nachlesbar.
- **Avalonias Menüs und Flyouts fehlen auf der Fensteraufnahme**, aus demselben Grund wie
  unter Windows (sie sind eigene Fenster). ~~Unter Windows hilft `-Voll`; hier hilft das
  nicht, weil die Vollbildaufnahme unbrauchbar ist.~~ **Seit dem 2026-08-18 hilft es doch:
  `tools/linux/wlschuss.sh`** (siehe den Punkt darunter). **Die Falle ist trotzdem geblieben,
  und sie ist teurer, als sie klingt** (V2-55): Ein Klick auf „Neu" sah dreimal hintereinander
  wie ein **wirkungsloser Klick** aus — dieselbe Aufnahme vorher wie nachher. Das Menü war
  jedes Mal offen gewesen, nur eben nicht mit auf dem Bild. **Es ist dieselbe Familie wie der
  `zeiger`-Fehler aus V2-47:** was wie ein Fehler der App aussieht, ist die Aufnahme.
  **Wer einen Klick für wirkungslos hält, prüft ihn erst mit einem Vollbild nach.**
- **Vollbildaufnahmen unter Wayland gehen — die alte Angabe „unbrauchbar" war falsch**
  (2026-08-18, V2-55). Sie steht seit §4.10 im HANDOFF und hat mehrere Runden Arbeit gekostet;
  richtig daran ist nur, dass `import -window root` nichts taugt (GNOME nutzt XWayland
  rootless, im X-Wurzelfenster steht nichts). **Der Weg ist
  `org.freedesktop.portal.Screenshot` mit `interactive: false`** — er liefert den ganzen
  Bildschirm samt Wayland-Oberflächen, also auch die Bildschirmtastatur und jedes Avalonia-
  Flyout. Neu als **`tools/linux/wlschuss.sh`**.
  **Woran es wirklich lag:** `xdg-desktop-portal-gnome` **lief nicht**. Das ist die eigentliche
  Falle, denn es scheitert nicht — es **antwortet einfach nie**, ohne Fehlermeldung, und sieht
  aus wie ein Aufhänger im eigenen Skript. `systemctl --user start
  xdg-desktop-portal-gnome.service` genügt.
  **Was nicht geht:** `org.gnome.Shell.Screenshot` und `org.gnome.Shell.Introspect` antworten
  unter **GNOME 50** mit `AccessDenied` — beide sind auf abgeschottete Anwendungen beschränkt.
  Wer eine ältere Anleitung findet, die sie benutzt, sucht vergeblich am falschen Ende.
- **Zwei Koordinatensysteme, auf diesem Gerät im Faktor 1,6** (2026-08-18, V2-55). Wayland
  rechnet **logisch** (1920 × 1080), XWayland in **Gerätepixeln** (3072 × 1728). **`zeiger` und
  XTEST wollen Gerätepixel; `wlschuss.sh` liefert logische, `schau.sh` liefert Gerätepixel.**
  Ein auf dem Portal-Foto abgelesener Punkt muss also mit 1,6 multipliziert werden, sonst
  landet jeder Klick zu weit oben links — und zwar plausibel weit, also nicht offensichtlich
  daneben. Den Faktor bekommt man aus `zeiger fenster` (Gerätepixel) gegen dieselbe
  Fensterkante auf dem Portal-Foto (logisch).
- **Der erste `zeiger`-Klick geht an einen Systemdialog verloren — XTEST läuft unter GNOME 50
  über das EI-Portal** (2026-08-18, V2-59). XWayland startet mit `-enable-ei-portal`; das
  erste synthetische Ereignis lässt deshalb **„Entfernter Bildschirm — Fernzugriff zulassen"**
  aufgehen. Der Dialog ist eine **Wayland**-Oberfläche, liegt über allem, **schluckt genau den
  Klick, der ihn ausgelöst hat**, und hält danach eine Sitzung unter
  `/org/gnome/Mutter/RemoteDesktop/Session` offen — samt Aufnahme-Symbol oben rechts.
  **Er gehört weder zu GonkNote noch zu `wlschuss.sh`.** Die Falle ist dieselbe Familie wie
  das unsichtbare Flyout: **es sieht nach einem wirkungslosen Klick aus.** Wer eine Kette
  beginnt, prüft den ersten Klick mit `wlschuss.sh` nach; danach läuft es durch.
- **`pkill -f dbus-monitor` bringt die eigene Shell um** (2026-08-18, V2-59). Die Befehlszeile,
  in der der `pkill` steht, enthält das Muster selbst — der Aufruf trifft sich also mit.
  Sichtbar wird das als Abbruch mit **Rückgabewert 144** und als Datei, die nie entsteht.
  **Mit der PID beenden** (`MON=$!` … `kill $MON`).
- **Ein fremdes GTK4-Fenster ist schwarz auf der Aufnahme — `GSK_RENDERER=cairo` hilft**
  (2026-08-11, beim Ansehen des PDF in Evince). Die Werkzeuge sind für den Avalonia-Kopf
  gebaut; wer damit ein **fremdes** Programm fotografieren will, braucht zwei Zutaten.
  `GDK_BACKEND=x11` bringt es überhaupt erst nach XWayland, sonst findet `zeiger fenster` es
  gar nicht. Und selbst dann liefert `import -window` nur den **Fensterrahmen mit schwarzer
  Fläche**: GTK4 zeichnet über GL in eine eigene Oberfläche, aus dem X-Fenster ist nichts zu
  lesen. **`GSK_RENDERER=cairo` zwingt es in die Software-Ausgabe**, und dieselbe Aufnahme
  wird sauber. Der Rahmen sieht in beiden Fällen normal aus — man hält das schwarze Bild
  leicht für einen Absturz des fremden Programms.
  **Was auch damit nicht ging: Eingaben.** Weder Klick noch Tastatur kamen bei Evince an,
  auch mit `hervor` als erstem Schritt nicht — der Weg über `_NET_ACTIVE_WINDOW` wirkt hier
  offenbar nicht wie beim eigenen Kopf. **Nicht weiter verfolgt**, weil die Frage der Runde
  aus der Datei zu beantworten war (§4.27, „Was der Laptop gefunden hat"). Wer ein fremdes
  Fenster wirklich **bedienen** muss, sollte damit rechnen, dass `zeiger` das nicht kann.

**Die Naht zwischen Modell und `FlowDocument` — `TdZuFlow` ↔ `FlowZuTd` (§4.45)**

- **`?? 0` ist dort keine Vorsichtsmaßnahme, sondern eine Behauptung.** Ein Feld von
  `TdParaFormat` ist `null`, solange es *nicht gesetzt* ist — es gilt dann
  `TdParaFormat.Standard`, und dort steht `SpaceAfterPt = 8`. Wer beim Weg nach WPF
  `f.SpaceAfterPt ?? 0` schreibt, behauptet „nicht gesetzt heißt null" und liegt um den
  ganzen Absatzabstand daneben. **Der Fehler bleibt nicht bei der Anzeige:** `FlowZuTd` liest
  einen so gesetzten Wert als *örtlich gesetzt* zurück und schreibt ihn fest.
  **Ein falsch angezeigter Wert repariert sich, ein festgeschriebener nicht.**
- **Die beiden Richtungen sind Umkehrungen voneinander, und das ist eine Auflage.**
  `TdZuFlow.AbsatzformatSetzen` löst die Kaskade auf, `FlowZuTd.AufAbweichungenKuerzen` kürzt
  wieder auf Abweichungen. Wer eine der beiden ändert, ohne die andere mitzuziehen, bekommt
  den Fehler von oben zurück — nur schwerer zu finden, weil dann zwei Stellen daran beteiligt
  sind. Wächter: `Die_zweite_Rundreise_aendert_nichts_mehr` vergleicht Byte für Byte.
- **Ein `FlowDocument` steht von Haus aus auf `Justify`.** Es ist die einzige Eigenschaft, bei
  der WPFs Vorgabe nicht die des Modells ist — deshalb liest `FlowZuTd` die Ausrichtung
  *wirksam* und nicht örtlich, und deshalb muss `TdZuFlow` sie **immer** setzen. Wer das
  vergisst, übernimmt jedes Dokument als durchgehenden Blocksatz (§4.37, Fund 2).
- **Ein Träger überlebt das `XamlPackage` nicht.** Gemessen: `Tag` und `ToolTip` an `Run` und
  `Paragraph` kommen als `null` zurück. **Nur ein `ToolTip` an einem `Image`** (also an einem
  UIElement) übersteht es — genau deshalb führt `DocumentImages` seinen Blob-Verweis dort und
  nirgends sonst. Wer etwas durch das Paket tragen will, hat genau diesen einen Weg.
- **Wer einen festen Wert in den `FlowDocument`-Weg schreibt, schreibt ihn ins Dokument.**
  `Brushes.Gray`, `Thickness(0.5)`, `Padding(6, 3, 6, 3)` am Tabellenrahmen und
  `Thickness(0, 1, 0, 1)` am Listenpunkt sahen wie Anzeigefragen aus und waren jede eine
  stille Änderung am Modell, sobald der Rückweg lief. **Was der Editor zeigt, kommt aus dem
  Modell — auch dann, wenn eine Vorgabe hübscher aussähe.**
- **Zentimeter → Pixel → Zentimeter trifft sich nicht selbst.** Aus 4 wird
  3,9999999999999996. `Cm`/`Pt` runden deshalb auf sechs Nachkommastellen; ohne das wandert
  jedes Maß bei jedem Speichern weiter und kein Wächter auf Gleichheit kann je grün sein.

**Markdown-Export — der Verweis muss zuerst drankommen**

> **⚠ Am 2026-08-28 gegen den Code gelesen (Phase 5, Punkt 2): der erste Punkt gilt weiter,
> aber aus einem anderen Grund — und `AppendInline` gibt es nicht mehr.** Der Weg liegt seit
> §4.12 in Core (`TdMarkdown.StueckSchreiben`), und dort erbt `TdHyperlink` **nicht** von
> einem allgemeineren Typ, sondern ist eine **Klammer um Stücke** (`TdInline`).
> **Die Anweisung bleibt dieselbe, die Falle dahinter ist eine andere.** Der alte Wortlaut
> steht darunter, weil er erklärt, wie es das erste Mal schiefging.

- **Der Verweis steht im `switch` vor allem anderen.** Heute, weil er ein Behälter ist: Wer
  über `Inlines` läuft und ihn nicht abfragt, schreibt eine **leere Zeile** — der Verweis
  erscheint selbst, sein Text steckt darin (§4.20, dieselbe Wurzel wie `FlacheStuecke`).
  Wächter unverändert: `Markdown_behaelt_das_Ziel_eines_Verweises`.
- ✅ **Beim Ziel `OriginalString` und nicht `AbsoluteUri`** — sonst wird aus einem relativen
  Ziel (`kapitel-2.md`) ein absoluter `file:///`-Pfad. **Das gilt unverändert und ist heute
  an drei Stellen eingelöst:** `FlowZuTd` beim Einlesen, `TdDocx` beim Schreiben und Lesen —
  und vor allem **im Modell selbst**: `TdHyperlink.Target` ist mit Absicht eine
  **Zeichenkette und kein `Uri`**, weil ein `Uri` vereinheitlicht. *Aus der Falle ist eine
  Formatentscheidung geworden; das ist der beste Ort, an dem eine Falle enden kann.*

  ~~**Der `Hyperlink`-Fall muss in `AppendInline` VOR `case Span` stehen.**~~ *(Stand bis
  §4.12 — der Export lief damals gegen das `FlowDocument`, wo `Bold`, `Italic`, `Underline`
  und `Hyperlink` alle `Span` sind.)* Genau daran ist es gescheitert: der allgemeinere Fall
  griff zuerst, der Linktext kam durch und das **Ziel fiel weg**. Ein Markdown-Import mit
  Links war nach dem Rückexport nur noch Fließtext. Behoben am 2026-07-30.
  **Wer im WPF-Kopf am `FlowDocument` arbeitet, dem gilt die Erbfolge weiter** — dort ist
  sie unverändert, und `FlowZuTd` läuft genau dort.
- Ziel mit Leerzeichen oder runden Klammern gehört in spitze Klammern, eckige Klammern im
  Linktext werden maskiert — sonst endet der Link vorzeitig.

**Neu aus Phase 1 — Tests**

- **Statische Zustände zwingen zu seriellen Tests.** `BlobStore.Current`,
  `ImageCache.Source` und der Cache in `ImageCache` sind prozessweit. Beide Testprojekte
  setzen deshalb `CollectionBehavior(DisableTestParallelization = true)`. Wer das entfernt,
  bekommt Fehlschläge, die von der Testreihenfolge abhängen.
- **Zwischengespeicherte Ladewege gehören in *einen* Test.** `WbAidRenderer` hält die
  geladene SVG-Grafik in einem statischen Feld. Eigenbau-Fall und SVG-Fall stehen darum
  zusammen in `GeodreieckTests` — als zwei Tests entschiede die Reihenfolge über das
  Ergebnis.
- **Nie Zeit oder Zufall in einer Fixture.** Kein `DateTime.Now`, kein `Guid.NewGuid()`, kein
  `{DATUM}` in der Fußzeile — sonst ändert sich ein Golden-File morgen von selbst. Die
  Beispieldokumente arbeiten mit festen Ids und einem festen Zeitstempel.
- **Der Aufräumlauf entscheidet über das Alter der *Datei*, nicht über eine Uhr im Code.**
  Ein Test dazu muss `File.SetLastWriteTimeUtc` benutzen (siehe `BlobSpeicherTests`) — die
  Alternative wäre, eine Stunde zu warten.
- **Bilder in Tests werden erzeugt, nicht eingecheckt.** Eine Binärdatei im Repo bräuchte
  eine geklärte Lizenz (§6); ein mit Skia gemaltes Rechteck braucht keine. Dabei nie
  achsensymmetrisch malen, sonst fällt eine vertauschte Achse nicht auf.

**Neu aus §4.26 — Schriften**

- **Eine eingebettete Schrift des UI-Rahmens ist für Skia nicht da.** Avalonias
  `WithInterFont()` registriert Inter bei **Avalonia**; `SKTypeface.FromFamilyName("Inter")`
  geht über **fontconfig**. Beides „Inter" zu nennen heißt nicht, dass es dieselbe Datei ist —
  im Avalonia-Kopf standen dadurch Chrome und Zeichenfläche in zwei Schriften, ohne dass etwas
  danach aussah.
- **`SKTypeface.FromFamilyName` findet keine mitgelieferte Datei.** Wer Schriften ausliefert,
  muss sie über `FromFile`/`FromStream` selbst laden und **einen** Ort haben, an dem das
  passiert — sonst hat jede Aufrufstelle ihre eigene Wahrheit.
- **`SKTypeface.FromFamilyName` liefert *immer* etwas.** Es gibt keinen Fehlschlag, nur eine
  Ersatzschrift. Wer prüfen will, ob der Name wirklich gefunden wurde, muss den
  zurückgegebenen `FamilyName` mit dem gefragten vergleichen.
- **Menüs erben ihre Schrift nicht vom Fenster.** WPF setzt `Menu`/`MenuItem` aus
  `SystemFonts.MenuFontFamily`. Ein `FontFamily` am `Window` erreicht alles — außer der
  Menüleiste. Gefunden nur, weil der A/B-Vergleich sie mit **0 abweichenden Pixeln** auswies,
  während sich jede andere Zone änderte.
- **Zwei ähnliche Schriften unterscheidet man nicht am Bildschirmfoto.** Um zu belegen, dass
  eine mitgelieferte Schrift wirklich greift, kurz auf eine **auffällige** umstellen (hier
  Space Grotesk), fotografieren, zonenweise vergleichen — und danach zurück. Ein „sieht richtig
  aus" ist bei Inter gegen Segoe UI keine Aussage.
- **Der Familienname einer Schriftdatei ist nicht der Dateiname.** `Inter-SemiBold.ttf` trägt in
  vielen Familien „Inter SemiBold" als eigene Familie. Deshalb steht die Zuordnung
  Datei → (Familie, fett, kursiv) **deklariert** in `Fonts.cs` und wird nicht aus der Datei
  gelesen. Womit WPF eine Schrift anspricht, verrät
  `System.Windows.Media.Fonts.GetFontFamilies(<Ordner-Uri>)`.
- **Die OFL verlangt den Lizenztext neben der Schrift, nicht im Repo.** Weitergegeben wird die
  Exe samt Ordner — die `OFL.txt` muss also in die **Ausgabe** kopiert werden. Und: **nicht
  verändern, nicht subsetten**, dann greift die Regel zu den Reserved Font Names nicht. Source
  Sans führt „Source" als RFN.

**Neu aus §4.25 — Gegenprüfen am laufenden Programm**

- **Der Kopf trägt seine eigene Kopie von Core.** Beide Köpfe kopieren `GonkNote.Core.dll` in
  ihren eigenen Ausgabeordner. Wer eine Zeichenkette in `LocGerman`/`LocEnglish` ändert und
  **nur einen** Kopf neu baut, prüft im anderen den alten Stand — und sieht dort den *alten*
  Text, ohne dass etwas kaputt aussieht. **Genau so passiert am 2026-08-10:** Der WPF-Kopf
  zeigte „Phase 4", der Avalonia-Kopf daneben weiter „phase 3", weil er vor der Änderung
  gebaut worden war. **Bei jeder Core-Änderung beide Köpfe neu bauen, bevor man
  gegenprüft** — sonst ist die Gegenprobe eine Aussage über einen Ordner und nicht über den
  Code.
- **Ein Skript, das das Fenster nach vorn holt, schließt jedes offene Menü** (§7,
  „Fernsteuern"). Der Umkehrschluss stand noch nicht da: **Ein zweiter Aufruf von
  `kette.ps1` fängt deshalb immer beim geschlossenen Menü an.** Der erste Schritt einer Kette
  sollte ein Klick auf eine leere Stelle sein, nicht `#{ESC}` — sonst landet der zweite Klick
  auf dem, was unter dem geschlossenen Menü liegt. In dieser Runde waren das zwei
  versehentlich angelegte Ordner (in einer Wegwerf-Datenbank, also folgenlos).
- **Ein modaler Dialog schluckt die Kette.** Solange er offen ist, laufen Klicks auf das
  Hauptfenster ins Leere. Erst schließen, dann weiterklicken.

**Neu aus Phase 4 — die Diagramme (§4.25)**

- **Eine Werteachse, die nicht bei null anfängt, lügt mit richtigen Zahlen.** 98 bis 100 lässt
  eine Säule doppelt so hoch aussehen wie die daneben. Die Achse fängt deshalb immer bei null
  an — oder darunter, wenn es negative Werte gibt.
- **Ober- und Untergrenze der Achse sind Vielfache der Teilung.** Nur dann liegt die Null auf
  einer Stufe, und nur daraus können alle Säulen wachsen. Ohne das stünde die Grundlinie aller
  Säulen dort, wo keine Linie ist.
- **Eine Rechnung, die misst, ist keine plattformneutrale Rechnung.** `TdChartLayout` schätzt
  die Breite einer Achsenbeschriftung (Zeichenzahl × Grad × 0,55), statt `ITdTextMeasure` zu
  fragen. Gemessen hinge die Lage der Zeichenfläche an der Schriftausstattung des Rechners —
  dieselbe Falle wie in §4.16, nur eine Ebene später.
- **Ein Bogen über 360° ist in Skia entweder nichts oder ein Kreis mit einem Schnitt darin.**
  Ein Kuchen mit **einer** Kategorie ist der Normalfall und nicht die Ausnahme; er wird als
  Kreis gezeichnet und nicht als Bogen.
- **„Kein Wert" ist nicht „der Wert null".** Eine kürzere Reihe bekommt an dieser Kategorie
  gar keine Säule; eine Reihe mit dem Wert 0 bekommt eine Pixelzeile auf der Nulllinie. Wer
  beides gleich behandelt, behauptet entweder eine Zahl, die niemand eingegeben hat, oder
  verschweigt eine, die dasteht.
- **Auch ein Diagramm rechnet in Zentimetern.** Der heutige Editor hat feste Pixelmaße
  (`left = 42`, `legendW = 120`) für genau eine Bildgröße. Schriftgrad, Linienstärke,
  Markenradius und Ränder müssen mit dem Diagramm **und** mit dem Maßstab wachsen — sonst ist
  die Beschriftung beim Druck mit 300 dpi ein Haar mit einem Punkt darin (§4.24).
- **Eine Zahl an der Achse gehört nicht in die Kultur des Rechners.** „1.5" fest im Code, wie
  das Datumsmuster in §4.20. Sonst ist dasselbe Dokument auf zwei Rechnern zwei Bilder.
- **Ein erfundener Reihenname wäre eine Übersetzung im Dokument.** „Reihe 2" hinge an
  `Loc.Current`; gezeichnet wird die laufende Nummer, wie bei `TdChart.Kategorie` (§4.21).

**Neu aus Phase 4 — der Zeichner (§4.24)**

- **Punkt ist nicht Pixel.** Die Schriftgröße steht im Modell in Punkt, die Leinwand rechnet in
  Pixeln, und der Maßstab kommt obendrauf: `Pixel = Punkt × 2,54/72 × Pixel-je-Zentimeter`. Wer
  die Umrechnung vergisst, bekommt bei jeder Zoomstufe dieselbe winzige Schrift auf einer immer
  größeren Seite — der Umbruch hat dann für Zeilen gerechnet, die niemand so sieht.
- **Der Zellinhalt zählt ab der Innenkante der Zelle**, nicht ab dem Textbereich: Der Umbruch
  setzt ihn als eigene kleine Seite ohne Ränder (§4.19). Ohne den Versatz steht der Text links
  neben der Tabelle, bei jeder Spalte weiter daneben.
- **Eine 1 px starke Linie kann zwei Pixelzeilen einfärben.** Liegt sie auf einer halben
  Pixelgrenze, verteilt die Kantenglättung sie. Ein Wächter, der Pixelzeilen zählt, findet dann
  zwei Linien, wo eine ist — **gezählt werden zusammenhängende Bänder.**
- **Für alles mit Schrift ist die Umschließung belastbarer als die Pixelzahl.** Wie viele Pixel
  ein Buchstabe einfärbt, hängt an Kantenglättung und Hinting und wächst bei kleinen Graden
  nicht linear mit; wie groß er ist, schon.
- **Ein Platzhalter skaliert mit.** Rahmenstärke, Strichlänge und Beschriftung in festen Pixeln
  sehen bei 100 % richtig aus und beim Druck mit 300 dpi wie ein Haar mit einem Punkt darin.

**Neu aus Phase 4 — das Umverdrahten (§4.23)**

- **In Word erben Läufe ihr Format aus der Formatvorlage, nicht aus dem `w:pPr/w:rPr`.** Das
  `pPr/rPr` gilt **nur für die Absatzmarke**. Wer das Absatz-Zeichenformat allein dorthin
  schreibt, bekommt eine Datei, die im eigenen Roundtrip einwandfrei aussieht und in Word jede
  Überschrift als Fließtext zeigt. **Ein Modell-gegen-Modell-Roundtrip kann das nicht finden**
  — er liest denselben falschen Ort wieder aus, den er beschrieben hat.
- **Ein `FlowDocument` steht von Haus aus auf `TextAlignment.Justify`.** Als einzige
  Eigenschaft ist ihr WPF-Vorgabewert nicht der, den das Modell annimmt (`Left`). Sie ist
  deshalb die einzige, die **wirksam** und nicht mit `ReadLocalValue` gelesen wird — sonst
  wandert jedes Dokument still von Blocksatz nach linksbündig.
- **Ein Wächter, der das Falsche liest, ist schlimmer als keiner.** `DocxAufriss` prüfte
  `rPr.Bold != null` statt den Wert: Seit der Export `w:b w:val="0"` ausschreibt (§4.14:
  `false` heißt *ausdrücklich nicht*), stand im Aufriss jede nicht-fette Zelle als fett. Bei
  An/Aus-Angaben in OOXML gilt: **fehlendes `w:val` heißt „an", `w:val="0"` heißt „aus".**
- **Es gibt zwei Fragen, nicht eine: „ist das eine Überschrift?" und „gehört das ins
  Verzeichnis?"** Titel und die Zeile „Inhaltsverzeichnis" beantworten sie verschieden. Word
  trennt sie über Vorlage (`Title`, `TOC Heading`) gegen `w:outlineLvl`; das Modell über
  `OutlineLevel` gegen `ExcludeFromToc`.
- **Ein generierter Verzeichnistext ist kein Inhalt.** Der Editor legt sein Verzeichnis als
  gewöhnliche Absätze ab. Wer sie beim Übernehmen mitnimmt, bekommt das Verzeichnis zweimal —
  einmal als Feld, einmal als Text, der beim nächsten Öffnen veraltet ist (§4.20).
- ⛔ **~~Der Export erst, das Umschalten später.~~** *(Stand bis §4.48 — **gilt seit dem
  2026-08-22 nicht mehr, und zwar umgekehrt.**)* Der Wortlaut war: „Solange `Rtf` führt, ist
  jeder Fehler im Umwandler folgenlos. Genau deshalb läuft er ab jetzt bei **jedem**
  Speichern mit."

  **Beides ist heute falsch.** `Rtf` **führt nicht mehr** — der WPF-Editor liest und schreibt
  seit §4.48 das Modell. **Ein Fehler im Umwandler ist damit nicht mehr folgenlos, sondern
  genau einmal folgenreich:** Er trifft die Übernahme eines Bestandsdokuments, und danach
  gibt es keinen zweiten Anlauf, der ihn korrigiert. *(Beim Aufräumen am 2026-08-28
  gefunden, §4.68 — die Zeile stand seit §4.23 hier, in dem Abschnitt, den man **vor** jeder
  Codeänderung liest.)*

  > **⚠ Und die Genauigkeit gehört dazu, sonst entsteht die nächste falsche Zeile:**
  > `Migrate` **wird weiter bei jedem Speichern gerufen** (`DocumentTabViewModel.Mitschreiben`
  > → `_io.Migrate(Doc)`). Es **tut** nur nichts mehr: `TdFuehrung.UebernahmeStehtAus` lässt
  > es nur durch, solange `Model` leer **und** `Rtf` gefüllt ist. **Der Aufruf ist geblieben,
  > die Arbeit ist einmalig geworden** — wer „läuft nicht mehr bei jedem Speichern" liest und
  > den Aufruf im Speicherweg findet, sucht sonst einen Fehler, den es nicht gibt.
  > *(Am Code nachgemessen, nicht im HANDOFF nachgelesen.)*

  **Was von der Lehre bleibt:** Die Altdatei wird nie überschrieben, eine misslungene
  Übernahme ist also kein Datenverlust, sondern ein Versuch, der wiederholt werden kann
  (`DatabaseService.AltdateiZu`). Das war der eigentliche Schutz — nicht der Umwandler, der
  bei jedem Speichern mitlief.

**Neu aus Phase 4 — die Übernahme (§4.22)**

- **Ein Roundtrip prüft, was zurückkommt — nicht, was dasteht.** Fünf Schritte lang schrieb
  das eigene Format in jedes Format ein `"IstLeer":false` und in jede Seiteneinrichtung vier
  gerechnete Werte. Kein Test konnte es sehen: Die Felder haben keinen Setter, werden also nie
  gelesen. **Gefunden am laufenden Programm**, behoben mit `[JsonIgnore]`; der neue Wächter
  sieht auf die **Datei**.
- **`ReadLocalValue` unterscheidet „nicht gesetzt" von „Vorgabewert".** Genau die
  Unterscheidung, auf der das Modell steht (§4.14) — eine Eigenschaft direkt abzufragen gäbe
  immer einen Wert, auch einen geerbten, und dann trüge jeder Lauf eine vollständige
  Formatkopie.
- **Aber `Bold`, `Italic` und `Underline` tragen ihre Bedeutung im Typ**, nicht in einer
  Eigenschaft: `ReadLocalValue(FontWeightProperty)` gibt für ein `Bold` nichts zurück. Wer nur
  örtliche Werte liest, verliert die drei häufigsten Auszeichnungen.
- **Eine Umwandlung darf ihre Vorlage nicht anfassen.** Das `FlowDocument` kann im Editor offen
  sein — ein Block, der kurz aus seinem Elternteil genommen wird, verschwindet vor den Augen
  des Nutzers.
- **Geraten wird einmal, bei der Übernahme.** Die Gliederungsebene kommt aus der Schriftgröße,
  weil das `FlowDocument` keinen Platz dafür hat — danach steht sie als eigener Wert im Modell.
  **Das ist der Unterschied zwischen einer Übernahme und einem Format.**
- **Die Übernahme läuft nur dort, wo das Altformat lesbar ist.** Unter Linux gäbe ein Versuch
  ein leeres Dokument — und das sähe aus wie gelöschter Inhalt, obwohl er unversehrt danebenliegt.
- **Eine Übernahme wirft nicht.** Sie läuft beim Öffnen; eine Ausnahme dort ist für den Nutzer
  dasselbe wie ein Absturz. Was schiefgeht, steht im Ergebnis **und im Dokument**.

**Neu aus Phase 4 — Bilder und Diagramme (§4.21)**

- **Ein Diagramm, das als Bitmap gespeichert wird, ist danach nicht mehr änderbar.** Genau das
  tut der heutige Editor (`ChartDialog`), und es fällt nicht auf, weil das Bild ja richtig
  aussieht. Im Modell stehen die Zahlen; Legende, Farbvergabe und fehlende Beschriftungen
  werden gerechnet.
- **Ein eingebettetes Arbeitsblatt zu einem Diagramm sind dieselben Zahlen ein zweites Mal.**
  Word schreibt es, wir nicht — literale Daten (`c:strLit`/`c:numLit`) tragen dasselbe. Der
  Preis ist benannt: Words „Daten bearbeiten" findet keine Mappe.
- **`c:scatterChart` verlangt Zahlen auf beiden Achsen.** Ein Punktdiagramm über *Kategorien*
  ist deshalb ein Liniendiagramm mit unsichtbarer Linie — und die Linie muss ausdrücklich
  unsichtbar gemacht werden (`a:ln`/`a:noFill`), denn ohne `a:ln` zeichnet Word eine.
- **Bilddaten gehören nicht ins Dokument.** In V1 gemessen: drei Fotos (2 MB) wurden im
  XamlPackage zu 16,8 MB und rissen die 16-MB-Grenze von LiteDB, weil WPF jedes Bild als PNG
  neu kodierte. Im Dokument steht ein Verweis, im Export gehen die **Originalbytes** hinaus —
  dafür braucht der Export die Endung, sonst rät er.
- **Fehlende Bilddaten sind zweierlei.** Keine Naht mitgegeben = Programmierfehler, also
  Ausnahme. Ein einzelner Blob fehlt = unvollständige Sicherung (der Blob-Ordner wird beim
  Kopieren vergessen, Dauerregel 4), also dieses eine Bild überspringen und weitermachen.
- **Die Kennung eines Bildes ist keine Aussage über das Dokument.** Beim Lesen bekommt es eine
  neue, weil die Bytes neu abgelegt werden — ein Roundtrip-Wächter muss die **Bytes**
  vergleichen, nicht die Kennung.
- **Eine Grafik steht *auf* der Grundlinie, nicht darin.** Ihre Höhe zählt ganz nach oben.
  Ohne das rechnet der Seitenumbruch mit einer Höhe, die es nicht gibt, und ein Bild läuft über
  den Seitenrand.
- **Die Absatzmarke ist immer dabei.** Sonst wäre ein Absatz mit einem winzigen Bild schmaler
  als ein leerer. Die Zeilenhöhe geht deshalb von der Schrift des Absatzes aus und nimmt die
  Stücke als Aufschlag.
- **Ein Wasserzeichen ist VML und hängt in der Kopfzeile** — als eingebundene Zeichnung gibt es
  ein hinter dem Text liegendes, zentriertes Bild gar nicht. Sein Bildteil hängt am
  **Kopfzeilenteil**: Beziehungen gehören zu dem Teil, der sie benutzt. Und **Deckkraft kennt
  DOCX nicht** — Word blasst über `gain` auf; das ist eine Näherung, keine Umrechnung.

**Neu aus Phase 4 — Felder, Verweise und Verzeichnis (§4.20)**

- **Ein Feld speichert seine Art und nicht seinen Wert.** Seitenzahl, Seitenanzahl und
  Inhaltsverzeichnis hängen von der Umgebung ab; gespeichert wären sie nach der nächsten
  Änderung falsch, und das Fehlerbild ist **kein** Fehlerbild — eine veraltete Seitenzahl sieht
  aus wie eine Seitenzahl.
- **Core fragt die Uhr nicht.** Datum und Titel kommen über `TdFieldContext` herein, wie die
  Schriftmaße über `ITdTextMeasure`. Ein `DateTime.Now` in der Rechnung macht jeden Wächter
  davon abhängig, wann er läuft. Das Datumsmuster steht aus demselben Grund fest im Code und
  nicht in der Kultur des Rechners.
- **Die Seitenzahl wird gesetzt, wenn die Zeile ihre Seite bekommt — nicht beim Umbrechen.**
  Eine zusammengehaltene Gruppe kann noch auf die nächste Seite rutschen; eine Zahl, die vorher
  eingetragen wurde, liegt dann um eins daneben.
- **Der Umbruch läuft mehrfach und hat eine Obergrenze.** Die Seitenanzahl steht erst am Ende
  fest, und ein Verzeichnis verschiebt durch seine Länge die Überschriften, deren Seiten es
  nennt. Gelaufen wird bis zum Stillstand, höchstens fünfmal — eine Schleife auf einen
  Fixpunkt, den es nicht gibt, meldet keinen Fehler, sondern gar nichts (§4.16).
- **Ein Feldergebnis darf nicht mitgeschrieben werden.** Sonst käme es beim Lesen als Text
  zurück, und das Dokument wüchse mit jedem Speichern um ein ganzes Inhaltsverzeichnis —
  dieselbe Falle wie beim Trennabsatz zwischen zwei Tabellen (§4.18). Beim **Lesen** fremder
  Dateien gilt umgekehrt: das Ergebnis eines bekannten Feldes wird verworfen, das eines
  unbekannten behalten. Eine Rechenvorschrift zu verlieren ist verschmerzbar, Text nicht.
- **Ein Verweis, ein `fldSimple` und eine Textmarke sind Geschwister des Laufs, keine Teile von
  ihm.** Wer beim Lesen nur `w:r` einsammelt, verliert jeden Verweistext — und merkt es nicht,
  weil er ja Text bekommen hat, nur weniger. Dasselbe gilt für jede Prüfung „ist dieser Absatz
  leer": ein Absatz mit nur einem Verweis ist nicht leer.
- **Das Ziel eines Verweises ist eine Zeichenkette und kein `Uri`** — sonst wird aus
  `kapitel-2.md` ein absoluter `file:///`-Pfad (§7, „Markdown-Export"). Ein Ziel mit `#` ist in
  DOCX ein **Anker** und keine Beziehung; als Beziehung geschrieben öffnet Word ein zweites
  Fenster auf dieselbe Datei.
- **Wer über die Stücke eines Absatzes läuft, muss in die Verweise hineinsteigen**
  (`TdParagraph.FlacheStuecke()`). Über `Inlines` sieht man die Klammer und nicht ihren Inhalt,
  und die Zeile bleibt leer.
- **Textmarken werden erzeugt, nicht gespeichert** — und nur, wenn es ein Verzeichnis gibt.
  Gespeichert wären sie ein zweiter Name für dieselbe Überschrift. Ihre Kennungen müssen
  dokumentweit eindeutig sein, sonst springen alle Einträge an dieselbe Stelle.
- **Ein Verzeichnis darf sich nicht selbst enthalten** und muss in Tabellenzellen absteigen
  (dieselbe Lehre wie bei der Nummerierung, §4.19). Ein leeres behält seine Zeile, sonst hat der
  Cursor an seiner Stelle keinen Ort.

**Neu aus Phase 4 — Tabellen-Layout (§4.19)**

- **Eine verbundene Zelle darf ihre erste Zeile nicht allein hochziehen.** Ihre Höhe verteilt
  sich über die Zeilen, über die sie reicht — und wie viel überhaupt fehlt, steht erst fest,
  wenn diese Zeilen ihre eigene Höhe kennen. **Die Rechnung braucht deshalb zwei Durchgänge.**
  Fehlt Platz, wächst die **letzte** Zeile der Verbindung: gleichmäßig verteilt sähe
  gefälliger aus, verschöbe aber die Zeilen dazwischen gegenüber ihren unverbundenen
  Nachbarn, und dann fluchtet die Tabelle nicht mehr.
- **Der Zellinhalt bricht in der Innenbreite um, nicht in der Zellbreite.** Ein vergessener
  Innenabstand ist kein sichtbarer Fehler, sondern eine Tabelle, deren Text am Rand klebt und
  eine Zeile zu spät umbricht.
- **Eine Fortsetzungszelle bekommt keinen Ort — sie schiebt aber die Spalte weiter.** Wer sie
  einfach überspringt, setzt alles dahinter eine Spalte zu weit links.
- **Eine wiederholte Kopfzeile muss als Wiederholung erkennbar sein** (`IsRepeatedHeader`).
  Sie steht im Modell einmal, auf dem Papier mehrfach; wer sie beim Zurückrechnen auf den
  Text mitzählt, findet den Cursor an der falschen Stelle. Wiederholt wird nur, wenn danach
  noch Platz für mindestens eine Inhaltszeile bleibt — sonst steht sie allein unten.
- **`TdDocument.Paragraphs()` muss in Tabellenzellen absteigen.** Die Nummerierung läuft über
  diesen Durchlauf; ohne den Abstieg bekommt ein Listenpunkt in einer Tabelle keine Marke,
  und das Fehlerbild ist ein Punkt ohne Nummer, kein Absturz.

**Neu aus Phase 4 — Tabellen (§4.18)**

- **DOCX misst Rahmen in Achtel-Punkt.** Eine 0,5-pt-Linie ist die `4`. Wer Punkte einträgt,
  bekommt eine achtmal zu dicke Linie — und das sieht nicht nach einem Umrechnungsfehler aus,
  sondern nach einer Tabelle mit unabsichtlich fetten Rändern.
- **Eine senkrecht verbundene Zelle ist `vMerge=restart` plus `vMerge` *ohne Wert*.** Ein
  „continue" als Wert kennt das Schema nicht. Beim Lesen gilt dieselbe Regel wie beim
  `<w:b/>`: kein Wert heißt nicht „aus".
- **Zwei Tabellen direkt hintereinander verschmelzen in Word zu einer** — dazwischen gehört
  ein leerer Absatz. Ebenso hinter der letzten Tabelle, sonst hat der Cursor darunter keinen
  Platz. **Diese Absätze sind kein Inhalt und müssen beim Lesen wieder heraus**, sonst wächst
  das Dokument mit jedem Speichern um eine Leerzeile je Tabelle. Wächter: der Roundtrip läuft
  zweimal und muss beide Male dasselbe ergeben.
- **Eine `sectPr` zählt nicht als Absatzformat.** Endet ein *nicht letzter* Abschnitt mit
  einer Tabelle, trägt genau der Trennabsatz die Abschnittsangabe — gälte er deswegen als
  „nicht leer", käme er als Leerzeile zurück.
- **Eine Tabellenzelle ohne Absatz ist schemawidrig**, und eine Fortsetzungszelle hat
  zwangsläufig keinen Inhalt. Der Pflichtabsatz darf beim Lesen nicht als Inhalt gelten.
- **Spaltenbreiten dürfen nie null oder negativ werden.** Eine Tabelle aus fremder Hand, deren
  angegebene Spalten breiter sind als die Seite, ergäbe sonst Spalten von −3 cm — und der
  Zeilenumbruch darin liefe gegen die Wand.

**Neu aus Phase 4 — Listen (§4.17)**

- **Eine Listennummer gehört nicht in den Absatz.** Sie hängt davon ab, was vor ihm steht.
  Gespeichert müsste sie bei jeder Einfügung im ganzen Dokument nachgezogen werden, und jede
  vergessene Stelle wäre eine Liste, die nach dem Löschen der 2 bei 3 weiterzählt.
- **Eine tiefere Ebene muss bei jedem Einrücken neu anfangen.** Ohne das Zurücksetzen zählt
  die zweite Unterliste dort weiter, wo die erste aufgehört hat — und das bemerkt man erst
  bei der dritten Ebene.
- **Bei einer Liste gibt `hanging` an, wie weit die Marke links steht — nicht der Text.** Der
  Text jeder Zeile beginnt am Einzug. Bei einem gewöhnlichen hängenden Einzug fängt dagegen
  die *erste* Zeile weiter links an. Wer das verwechselt, rückt die zweite Zeile unter die
  Marke.
- **Die Marke ist kein Lauf.** Sonst wird sie mitkopiert, lässt sich auswählen, und der
  Cursor kann davor stehen.
- **`abstractNum` ist die Vorlage, `num` die Instanz** — nur Letztere hat die Kennung, auf die
  ein Absatz zeigt. Im XML stehen **erst alle** `abstractNum`, **dann alle** `num`;
  verschachtelt geschrieben öffnet Word die Datei nicht. Beim Lesen umgekehrt: ein `num` kann
  auf ein `abstractNum` zeigen, das erst danach kommt.

**Neu aus Phase 4 — Umbruch (§4.16)**

- **Eine Schriftmessung gehört hinter eine Naht.** „Segoe UI" gibt es unter Linux nicht, und
  ein Schriftartenupdate verschiebt jede Breite. Ein Umbruch-Test gegen echte Maße hat auf
  Windows und im Linux-Lauf der CI verschiedene Ergebnisse und wird nach dem ersten falschen
  Alarm abgeschaltet. Mit `ITdTextMeasure` davor prüfen die Wächter mit festen Maßen und
  bekommen exakte Zahlen; von Skia bleibt eine Plausibilitätsprüfung übrig.
- **Im Umbruch wird in Zentimetern gerechnet, nicht in Pixeln.** Sonst müsste er bei jeder
  Zoomstufe neu laufen und brächte bei jeder ein leicht anderes Ergebnis. Der Zoomfaktor
  gehört an die Stelle, die zeichnet.
- **Drei Stellen, an denen ein Umbruch stehenbleiben kann:** ein Wort breiter als die Zeile,
  eine zusammengehaltene Gruppe höher als eine Seite, und ein Zerlegen, das den Zähler nicht
  weiterrückt. Alle drei brauchen einen Ausweg, der etwas **sichtbar Falsches** erzeugt statt
  gar nichts — ein Lauf, der nicht zurückkommt, meldet keinen Fehler (§4.12).
- **Blocksatz gilt nicht für die letzte Zeile eines Absatzes** — und ein erzwungener
  Zeilenumbruch beendet ebenfalls eine „letzte" Zeile. Ohne das zieht ein Schlusswort über die
  ganze Breite auseinander.
- **Der Leerraum gehört beim Zerlegen ans Wort davor, nicht dazwischen.** Sonst rückt jede
  Zeile nach einem Umbruch um ein Leerzeichen ein, und bei Blocksatz fällt das sofort auf.
- **Absatzabstände gehören zur ersten bzw. letzten Zeile**, nicht zwischen die Zeilen — nur
  so wandern sie mit, wenn ein Absatz auf die nächste Seite rutscht. Beim Abstand *davor*
  muss die Grundlinie mitwandern, sonst sitzt der Text im Abstand.
- **Wer mit runden Zahlen rechnet, muss die Vorgabewerte kennen.** „Zehn Zeilen à 1 cm passen
  auf 10 cm" ergab sieben — `TdParaFormat.Standard` hat 8 pt Abstand nach jedem Absatz. Der
  Test prüfte die Vorgabe mit, ohne es zu wissen.

**Neu aus Phase 4 — Dokumentformat und DOCX (§4.14, §4.15)**

- **DOCX legt die Seiteneinrichtung unsymmetrisch ab.** Die des **letzten** Abschnitts steht
  am Ende des Körpers, die aller anderen im `pPr` ihres jeweils letzten Absatzes. Wer alle ans
  Körperende hängt, bekommt ein Dokument mit genau **einer** Seiteneinrichtung — und merkt es
  erst am Ausdruck. Beim Lesen gilt dasselbe rückwärts: der Absatz, der die `sectPr` trägt,
  ist **Inhalt** und kein bloßer Träger; wer ihn überspringt, verliert je Abschnitt eine Zeile.
- **Word leitet die Ausrichtung nicht aus den Maßen ab.** Ohne `w:orient` dreht es ein quer
  eingetragenes Blatt beim Drucken wieder hoch. Die Datei sieht dabei richtig aus, nur der
  Ausdruck nicht.
- **`{SEITE}` als Text steht auf jeder Seite gleich da.** Kopf- und Fußzeilen brauchen echte
  Felder (PAGE, NUMPAGES) plus `UpdateFieldsOnOpen`. Und den Weg **zurück**: ohne ihn kommt
  aus einem Rückimport die beim Schreiben eingesetzte „1" als gewöhnlicher Text.
- **Zwei Wahrheiten über dieselbe Sache driften auseinander — auch im Kleinen.** Deshalb hat
  `TdPageSetup` weder einen Formatnamen (`"A4"` wird aus der Größe zurückerkannt) noch einen
  Querformat-Schalter (breiter als hoch = quer). Der heutige Editor speichert beides und
  rechnet die Größe aus dem Namen — das bricht beim ersten fremden DOCX, dessen `sectPr`
  Zahlen nennt und keinen Namen.

- **Dasselbe Paket kann je Ziel-Framework eine andere Abhängigkeitskette haben.**
  `DocumentFormat.OpenXml` zieht auf reinem `net10.0` ein `System.IO.Packaging` aus NuGet
  nach — mit zwei bekannten Lücken (NU1903). Unter `net10.0-windows` mit WPF kommt dieselbe
  Klasse aus dem Framework, und es wird gar kein Paket geholt. Der WPF-Kopf benutzt OpenXml
  seit jeher; aufgefallen ist es erst, als **Core** die Referenz bekam. **Eine Lücke taucht
  dort auf, wo man sie nicht sucht** — behoben über das transitive Pinning wie bei
  SQLitePCLRaw. Für den iOS-Kopf gilt dasselbe noch einmal.
- **`<w:b/>` ohne `w:val` heißt „an".** Ein `Val?.Value ?? false` beim Lesen macht damit jede
  fette Stelle eines **fremden** Dokuments still normal — und nur die: die eigenen Dateien
  schreiben `val` immer mit, das Fehlerbild tritt also erst beim Import aus Word auf und nie
  im eigenen Roundtrip. Gilt genauso für `i`, `strike`, `keepNext` und `pageBreakBefore`.
  Wächter: `Eine_Auszeichnung_ohne_Wert_gilt_als_gesetzt`.
- **Ein hängender Einzug ist in DOCX kein negativer Einzug**, sondern ein eigenes Feld
  (`w:hanging` statt `w:firstLine`), und beide sind positiv. Wer nur eines schreibt, macht
  aus −0,5 cm ein +0,5 cm.
- **Ohne `xml:space="preserve"` fallen führende und mehrfache Leerzeichen weg** — nach dem
  Speichern, nicht beim Tippen.
- **Die Reihenfolge der Kindelemente in `w:rPr` und `w:pPr` ist Schema.** Vertauscht ergibt
  sie kein schiefes Bild, sondern eine Datei, die Word nicht öffnet. Deshalb prüft
  `TdDocx.Pruefen` mit dem `OpenXmlValidator` gegen Office 2019 — dieselbe Messlatte, die
  der frühere `DocxExporter` seit jeher anlegte.
- **Eine Toleranz gehört an die Auflösung des Formats, nicht an die der Fließkommazahl.**
  Ein Twip ist 1/1440 Zoll = **0,0018 cm**; aus 1,5 cm werden 850 Twips und daraus wieder
  1,4993 cm. Ein Roundtrip-Test, der drei Nachkommastellen verlangt, prüft nicht den eigenen
  Code, sondern die Auflösung eines fremden Formats — und wird nach dem ersten falschen
  Alarm abgeschaltet. Zentimeter deshalb auf zwei Stellen, Punkt auf drei (`pt·20` geht auf).
- **`null` heißt im Dokumentmodell „nicht gesetzt" und nicht „Standardwert".** Wer das
  einebnet, kann Fett in einer fetten Überschrift nie wieder abschalten. Deshalb schreibt
  `TdJson` mit `WhenWritingNull` — **das Gegenteil von `GonkJson`**, und aus dem
  entgegengesetzten Grund: dort wäre „Feld fehlt" gegen „Feld ist 0" der Verlust, hier ist
  genau diese Unterscheidung der Inhalt.

**Neu aus Phase 3 — Testen**

- **Ein Testlauf, der nicht zurückkommt, ist ein Fehlschlag und kein langsamer Rechner.**
  `dotnet test` gibt seine Zusammenfassung erst am Ende aus; eine Endlosschleife in einem
  einzigen Test sieht deshalb genauso aus wie ein Rechner, der sich Zeit lässt. In dieser
  Runde lief er über zwanzig Minuten, bevor jemand nachsah — der ganze Durchlauf dauert
  **7 Sekunden** (§4.12, `Eine_Tabelle_braucht_ihre_Trennzeile`).
  **Merksatz: dauert ein Lauf um Größenordnungen länger als sonst, ist er hängen geblieben.**
- **Zwei `dotnet test` gleichzeitig auf demselben Testprojekt blockieren einander.** Sie
  teilen sich Ausgabeordner und MSBuild-Knoten. Sieht aus wie dieselbe Endlosschleife und
  ist eine andere Ursache — beim Suchen zuerst nachsehen, ob überhaupt nur ein Lauf läuft
  (`pgrep -a dotnet`).

---
