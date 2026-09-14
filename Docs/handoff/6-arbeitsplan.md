[← Index: HANDOFF.md](../HANDOFF.md)

## 6. Arbeitsplan

### Erledigt: Phase 1 — Netz einziehen

Umgesetzt am 2026-07-30 unter Windows, die Linux-Seite im Container gegengeprüft (§4.6).

- [x] `tests/GonkNote.Core.Tests` angelegt, in der `.slnx`; dazu `tests/GonkNote.Wpf.Tests`
      für alles, was am `FlowDocument` hängt (§4.1) und darum nur unter Windows läuft
- [x] **DB-Roundtrip:** Notizbuch, Textdokument, Ordnerbaum und Einstellungen. Der
      `ModelTypeBinder` hat eigene Tests mit **von Hand geschriebenen alten `_type`-Namen**
      (`AlteTypnamenTests`) — der Roundtrip allein deckt ihn nicht ab, der schreibt und liest
      immer mit dem heutigen Namen. `EmptyStringToNull` und die Blob-Auslagerung ebenfalls
- [x] **Renderer-Snapshots:** 20 Pixelhashes, u. a. die Bleistift-Körnung (der Absturz aus
      §7), Textmarker-Alpha, Bildabtastung, Nine-Patch-Schatten, Drehung, beide
      Geodreieck-Ladestufen. Schrift bewusst ausgenommen — Begründung in §4.6
- [x] **Export-Fixtures:** ein Referenzdokument mit Tabelle, Bild, Diagramm,
      Inhaltsverzeichnis, Listen, Zeichenformaten, Verweis, Kopf-/Fußzeile und Wasserzeichen.
      Golden-Files: `referenz.md` (Text 1:1) und `referenz-docx.txt` (Aufriss des DOCX). Der
      PDF-Weg über den Rückimport mit PDFium
- [x] **CI:** `.github/workflows/ci.yml`, zwei Läufe. Beide Befehlsfolgen vorab lokal bzw. im
      Container ausgeführt, nicht nur aufgeschrieben

**Was Phase 1 gefunden hat:** einen zweiten Absturz aus dem SkiaSharp-3-Umstieg
(`SKBitmap.Decode` wirft, wo es früher `null` lieferte — §7, behoben in `WbImages`). Genau
dafür war die Phase da.

**Ebenfalls gefunden und (auf Nutzer-Entscheidung) behoben:** Der Markdown-Export verlor das
**Ziel** von Hyperlinks. Siehe §7 „Markdown-Export".

### Erledigt: Phase 2 — die große Entkopplung

Lief unter Windows/WPF. **Nach jedem Schritt musste die App noch starten** — hat sie.

- [x] 1. `Core/Platform/`-Interfaces einziehen, WPF-Implementierungen dahinterhängen
      (§4.7). Es sind **zwölf** geworden statt der acht aus der Roadmap — `IDialogService`,
      `IShell`, `IUiScheduler` und `IDocumentIo` kamen dazu, weil das `MainViewModel` sie
      braucht. (`TitleBarTheme`, `WindowBounds` bleiben ersatzlos Windows-only.)
- [x] 2. `GonkNote.ViewModels` freischneiden und zur eigenen Assembly machen (§4.2)
- [x] 3. **LiteDB → `Microsoft.Data.Sqlite`** (§4.8, 2026-08-02). Migration additiv, die
      alte Datei bleibt unversehrt liegen; an einer Kopie der echten Datenbank feldweise
      gegengeprüft. LiteDB lebt nur noch in `src/GonkNote.Legacy/`
- [x] 4. `BlobStore` von der Ableitung aus dem DB-Dateinamen auf `IAppPaths` umstellen
      (§4.8) — `AppPaths.BlobFolder` + `BlobStore.InFolder(...)`. Der alte Konstruktor
      bleibt für `--db`, sonst bräche §8

**Das Netz aus Phase 1 hat getragen.** `DatenbankRoundtripTests`, `AlteTypnamenTests` und
`BlobSpeicherTests` prüfen die **öffentliche** API des `DatabaseService` — die ist beim Umbau
gleich geblieben, und die Tests sind unverändert grün. `AlteTypnamenTests` ist vom
Roundtrip- zum **Migrations**-Wächter geworden (drei neue Tests, §4.8).

**Meilenstein M0 ist vollständig:** `Core`, `ViewModels` **und** `Legacy` bauen auf Linux
(alle `net10.0`), Windows verhält sich unverändert.

### Erledigt: Phase 3 — Avalonia-Shell für Linux · **M1 erreicht**

Erster Brocken am 2026-08-03 unter Windows (§4.9, §5b), Brocken 2 bis 7 am selben Tag auf
dem CachyOS-Laptop (§4.10, §4.11, §4.12).

- [x] 1. `src/GonkNote.Avalonia` angelegt (`net10.0`, Avalonia 12.1.1), in der `.slnx`,
      im Linux-Lauf der CI
- [x] 2. `AvaloniaPlatformServices` — alle **zwölf** Schnittstellen aus `Core/Platform/`.
      Drei davon sind die vorhandenen Rückfälle (`NoOcrEngine`,
      `AlwaysSupportedSpellChecker`, leeres `IDocumentIo`) und ausdrücklich so benannt
- [x] 3. **Farbtabelle in Core** (`Core/Theming/`) statt eines zweiten Paars fest
      verdrahteter Theme-Dateien — die Entscheidung, die laut §6 jetzt fallen musste.
      Wächter `FarbtabelleTests` hält beide Fassungen zusammen
- [x] 4. Start mit `--db` **und `ILegacyDatabaseReader`**; Ordnerbaum und Galerie aus
      demselben `MainViewModel` wie der WPF-Kopf, kein zweites
- [x] 5. **Zeichenfläche** — Notizbuch und Whiteboard, umgesetzt am 2026-08-03 **auf dem
      Laptop** (§4.10). Der Renderer bekommt Avalonias eigenen `SKCanvas`
      (`ISkiaSharpApiLeaseFeature`); damit ist die offene Frage aus §5a beantwortet.
      Eingabepfad mit `GetIntermediatePoints()`, erkanntem Druck samt Rückfall und
      Handballenabweisung. Radieren über `WbErase`, Trefferprüfung als neues `WbHit` in Core
- [x] 6. Drag & Drop im Baum, einblendbare Titelleiste, Einstellungen-Seitenleiste (§4.12).
      Die Seitenleiste hat **nur** den Seiten-Abschnitt — Formen, Text, Notizzettel und
      Cover fehlen als Werkzeuge, also fehlen auch ihre Abschnitte
- [x] 7. `EmbeddedDocs`-Gegenstück (§4.12). Der Markdown-**Zerleger** ist dabei nach
      `Core/Text/` gewandert, weil er kein Pixel zeichnet; jeder Kopf malt nur noch.
      Wächter `MarkdownTests` (21 Tests) — er hat sofort eine Endlosschleife gefunden, die
      auch in `MarkdownFlow` steckt

**Was Phase 3 bewusst ausgelassen hat** — nicht vergessen, sondern nicht M1: Text-,
Formen- und Notizzettel-**Werkzeug** (angezeigt werden diese Elemente, nur anlegen kann man
sie nicht), Drehen und Skalieren der Auswahl, Bild- und PDF-Import, Import und Export.
Sticker, Texterkennung, Zahlenblock, Schnellaktionen und Geodreieck gehören ohnehin nicht
dazu. **Alles davon steht jetzt auch in den vier mitgelieferten Dokumenten**, mit Begründung
(§4.12).

> **Seit 2026-08-04 hat das eine Phase:** „Vorgemerkt: Phase 4.5" weiter unten. Bis dahin
> war es benannt, aber **keiner Phase zugeordnet** — und M2 („Funktionsgleichheit") damit
> unerreichbar. Aufgefallen ist es durch eine Rückfrage des Nutzers, nicht beim Planen.

> **✅ M1 ist ausgerufen** (Nutzer-Entscheidung 2026-08-03, §5): Notizbuch und Whiteboard
> laufen unter Linux, Textdokumente sind ausgegraut — der Wortlaut von M1. Damit ist auch
> der gültige Ausstiegspunkt erreicht, den die Roadmap dafür vorsieht.

> **Für den nächsten Brocken vorher lesen:** §4.10 und §4.12 (wie die Zeichenfläche und der
> Kopf gebaut sind), §7 „Der Avalonia-Kopf" — dort stehen jetzt **zehn** Eigenheiten statt
> vier — und §7 „Fernsteuern unter Wayland", ohne das sich auf dem Laptop nichts belegen
> lässt. **Wer etwas zusammenlegt, liest zusätzlich §4.13** („Pixelgleich ist hier
> gemessen") — der Vergleich alt↔neu ist das Muster dafür.

### Erledigt: die zwei Schulden aus Phase 3 — **eingelöst**

Am 2026-08-04 unter Windows abgearbeitet (§4.13). Beide waren Zusammenlegungen, keine
Neubauten:

| Stand doppelt | Erledigt durch |
|---|---|
| ✅ Trefferprüfung und Lasso (`WbHit` in Core ↔ `WhiteboardView.Selection.cs`) | Der WPF-Kopf rechnet mit `WbHit`; die private Fassung ist weg. **Pixelgleichheit gemessen**, nicht behauptet (§4.13) |
| ✅ Markdown-Grammatik (`Core/Text/Markdown.cs` ↔ `Services/MarkdownFlow.cs`) | `MarkdownFlow` behält seine `FlowDocument`-Hälfte und ruft `Markdown.Parse`. **Die Endlosschleife kam ohne eigenen Handgriff mit** — wer die Grammatik wegwirft, wirft den Fehler mit weg |
| ✅ (klein, aus §4.12) Datenordner im WPF-Über-Dialog | Eine Zeile XAML, eine Zeile Code — `About.Subtitle` nennt seit §4.12 keinen Pfad mehr, die Angabe fehlte auf Windows sonst ganz |

**Die Testzahl bleibt bei 159.** Es ist nichts dazugekommen; es sind zwei Fassungen weniger
geworden. `TrefferTests` und `MarkdownTests` bewachen ab jetzt **beide** Köpfe statt nur den
Linux-Kopf.

### Erledigt: Phase 4 — eigene Dokument-Engine · **abgeschlossen 2026-08-11**

**Strikt in der Reihenfolge aus Roadmap §5, nach jedem Schritt DOCX-Roundtrip.** Die
Reihenfolge ist kein Vorschlag: sie ist die Antwort auf „Dokument-Engine wird zum Fass ohne
Boden", das einzige Risiko, das die Roadmap mit **hoch** einstuft.

- [x] **1. Absätze + Zeichenformate** (§4.14, 2026-08-04). Modell, Speicherformat und das
      DOCX-Tor stehen; 37 neue Wächter. **Noch nirgends angeschlossen** — das ist Absicht
- [x] **2. Seitenumbruch** — zwei Hälften, beide fertig:
      - [x] **Modell** (§4.15): `TdSection` + `TdPageSetup`, DOCX-`sectPr` in beide
            Richtungen, Kopf-/Fußzeile mit echten PAGE-Feldern
      - [x] **Layout-Rechnung** (§4.16): `TdLayout` bricht Zeilen und Seiten um, hinter der
            Naht `ITdTextMeasure`. 43 neue Wächter in Schritt 2
- [x] **3. Listen** (§4.17). Der reservierte Diskriminator `"list"` wird **nicht** gebraucht:
      ein Listenpunkt ist ein Absatz mit einer Angabe, kein eigener Blocktyp. Er bleibt frei
- [x] **4. Tabellen, inkl. verbundener Zellen** — zwei Hälften, beide fertig:
      - [x] **Modell und DOCX** (§4.18): `TdTable`/`TdTableRow`/`TdTableCell`, waagerechte und
            senkrechte Verbindungen, Rahmen, Zellhintergrund, Tabellen in Zellen
      - [x] **Layout-Rechnung** (§4.19): Spaltenbreiten, Zellumbruch, Zeilenhöhe aus der
            höchsten Zelle, Umbruch zwischen Zeilen mit wiederholter Kopfzeile. 36 Wächter
            in Schritt 4. **Eine benannte Lücke bleibt:** eine Tabelle *in* einer Zelle wird
            noch nicht gesetzt (§4.19), mit eigenem Wächter festgehalten
- [x] **5. Felder und Inhaltsverzeichnis** (§4.20). `TdField` speichert die **Art** und nicht
      den Wert, `TdHyperlink` das Ziel **wörtlich**, `TdToc` rechnet über
      `TdParaFormat.OutlineLevel` — die verlässliche Quelle, die das `FlowDocument` nie hatte.
      Der Umbruch läuft dafür mehrfach, bis sich nichts mehr ändert. 57 neue Wächter
- [x] **6. Diagramme** (§4.21) — **und Bilder, denn ein Diagramm *ist* heute ein Bild.**
      `TdChart` speichert die **Zahlen** und geht als echtes `c:chart` nach DOCX; `TdImage`
      trägt einen Verweis auf den Blob-Speicher hinter der Naht `ITdImages`. Das
      **Wasserzeichen** aus §4.15 ist mitgekommen. 41 neue Wächter. **Die reservierten
      Blocknamen `"image"`/`"chart"` blieben frei** — beide sind Stücke, wie in DOCX
- [x] **Umverdrahten, erster Teil** (§4.23, 2026-08-09): Das Modell wird bei jedem Speichern
      mitgeschrieben, **DOCX und Markdown laufen gegen das Modell** (`TdDocx`, `TdMarkdown`),
      der DOCX-Import kommt über `TdZuFlow` zurück in den Editor. `DocxExporter` und
      `MarkdownExporter` sind gelöscht. **Fünf Fehler dabei gefunden**, die der
      Modell-gegen-Modell-Roundtrip nicht sehen konnte
- [x] **Der Zeichner** (§4.24, 2026-08-09): `TdRenderer` in Core malt eine gesetzte Seite mit
      SkiaSharp — Text mit allen Zeichenformaten, Aufzählungsmarken, Absatzlinien, Tabellen,
      Bilder, Kopf-/Fußzeile, Wasserzeichen. 11 neue Wächter. **Noch nirgends angeschlossen** —
      dieselbe Absicht wie bei Schritt 1
- [x] **Die sieben Diagrammarten zeichnen** (§4.25, 2026-08-10): Säule, Balken, Linie, Punkte,
      Punkt+Linie, Kuchen und Netz, samt Achsen, Gitter, Legende und Beschriftung. **Die
      Rechnung steht in `Core/Text/TdChartLayout.cs`**, der Zeichner malt nur — zum vierten Mal
      dasselbe Muster nach §4.17, §4.20 und §4.21. 53 neue Wächter, **kein einziges neues Feld
      am Modell**. Dabei ist aufgefallen, dass die hier bisher genannte „Fläche" gar keine
      Diagrammart ist (§5 „Noch offen", Punkt 6)
- [x] **Ein Schriftkonzept für alle drei Plattformen** (§4.26, 2026-08-10): Fünf mitgelieferte
      Familien für fünf Rollen, als Datentabelle in `Core/Theming/Fonts.cs`; `WbFonts` ist der
      einzige Auflösungspunkt. **Vorgezogen vor den `PdfExporter`**, damit dessen Golden-Files
      nicht zweimal gesetzt werden. 12 neue Wächter. Dabei kam ein Fehler heraus, den nur ein
      A/B-Bildvergleich zeigen konnte
- [x] **Der PDF-Export gegen das Modell** (§4.27, 2026-08-11): `TdPdf` in Core statt des
      WPF-Paginators — umbrechen mit `TdLayout`, zeichnen mit `TdRenderer` **direkt auf die
      PDF-Leinwand**. Der Text im PDF ist Text, Verweise sind anklickbar. **§4.1 ist damit
      aufgelöst.** 18 neue Wächter, **kein einziges neues Feld am Modell** — zum fünften Mal
      dasselbe Muster nach §4.17, §4.20, §4.21 und §4.25. Nebenbei gelöscht: `DocxImporter`
      (556 Zeilen) und zwei tote Stücke aus `DocumentImages`
- [x] **Die Anzeige im Linux-Kopf** (§4.28, 2026-08-11): `TdLayout` + `TdRenderer` auf die
      Avalonia-Leinwand, dazu das **Ribbon** (zwei Reiter: Ansicht/Ausgabe und die abgelesene
      Seiteneinrichtung) und `AvaloniaDocumentIo` verdrahtet — DOCX-Import, Export in alle
      vier Formate. Die Weiche über die Endung ist als `TdExport` nach Core gewandert, damit
      sie nicht zweimal dasteht. **Dabei gefunden und behoben: jede Tabelle stand mit
      doppelter Kopfzeile da** — vier Runden lang, und kein Wächter konnte es sehen, weil alle
      Text vergleichen und keiner ein Bild ansieht

> **✅ Damit ist Phase 4 abgeschlossen.** **Was ausdrücklich nicht dazugehörte: das
> Schreiben** im Linux-Kopf (Cursor, Auswahl, Eingabe, Undo). Die Roadmap nennt es unter
> „Editing" in Phase 4; es ist eine eigene Runde und steht in §6 „Als Nächstes". **`Rtf`
> bleibt bis dahin das führende Feld** (§5) — es abzulösen heißt, den WPF-Editor auf das
> Modell umzustellen, und dafür fehlt derselbe Schreibweg.

#### Wie die Bestandsdokumente herüberkommen — **vor Schritt 2 zu entscheiden**

`TextDoc.Rtf` enthält RTF oder ein WPF-`XamlPackage` (§4.14). Das Modell kann beides nicht
lesen, und der Linux-Kopf konnte es nie. **Der Vorschlag folgt der Entscheidung, die für die
Datenbank schon gefallen ist** (§4.8, „additiv, die alte Datei bleibt unversehrt liegen"):

- Ein **neues Feld** an `TextDoc` neben `Rtf`, nicht statt dessen. Die alten Bytes werden nie
  überschrieben — dieselbe Regel, aus der `gonknote.db` neben `gonknote.sqlite` liegen bleibt.
- Die Übernahme läuft **auf dem Windows-Rechner**, einmalig beim Öffnen: nur dort gibt es das
  `FlowDocument`, das RTF und XamlPackage überhaupt lesen kann. Der Linux-Kopf kann eine
  Altdatei nicht übernehmen und darf das auch nicht versuchen.
- Ein Dokument, das noch nicht übernommen ist, muss unter Linux **sagen, was los ist**, statt
  leer aufzugehen. „Leer" ist von „kaputt" für den Nutzer nicht zu unterscheiden — dieselbe
  Begründung wie bei `Ohne_Leser_wird_nicht_stillschweigend_neu_angefangen` (§7).

> **✅ Entschieden und umgesetzt am 2026-08-05 (§4.22): still — aber ein Fehler wird dem
> Nutzer gespiegelt.** Was hier steht, ist damit gebaut; der Absatz bleibt als Begründung
> stehen.

**Die Frage, die dahinterstand:** ob die Übernahme **still** läuft (wie die SQLite-Migration)
oder ob sie sich zeigt. Bei der Datenbank war „still" richtig, weil nichts verlorengehen
konnte.

> **Der Vorschlag von damals — die Übernahme kommt zuletzt, nicht zuerst — ist eingelöst:**
> **Seit Schritt 6 (§4.21) kann das Modell alles, was ein Bestandsdokument enthalten darf** —
> Tabellen, Bilder, Diagramme, Felder, Verweise, Wasserzeichen. Die Begründung „eine Übernahme
> davor wäre stiller Datenverlust" ist damit ausgeräumt, und **die Frage still-oder-sichtbar
> ist jetzt fällig**. Bis sie beantwortet ist, bleibt `Rtf` das führende Feld und das Modell
> läuft daneben — geprüft über DOCX, nicht über Nutzerdaten.
>
> **Was der Antwort trotzdem im Weg steht, auch bei einem vollständigen Modell:** RTF und
> XamlPackage können Dinge tragen, die *kein* Modell kennt (eingebettete Objekte, Formen,
> Kommentare). Eine Übernahme sollte deshalb prüfen, was sie nicht versteht, statt es
> stillschweigend fallen zu lassen — dieselbe Regel wie bei `Was_noch_nicht_geht_verschwindet_nicht_still`.

### ▶ Als Nächstes: das Schreiben — der Rest von Phase 4

**Nutzer-Entscheidung 2026-08-11 (§5 „Noch offen" 2): Weg (a) vor Phase 4.5.** Damit ist
Phase 4.5 nicht abgesagt, nur nachgereiht — sie trägt weiterhin M2, und ihre Begründung unten
gilt unverändert.

> **Der Auftrag in einem Satz:** Ein Textdokument, das der Linux-Kopf heute *anzeigt*, soll
> sich darin **bearbeiten** lassen — Cursor setzen, auswählen, tippen, löschen, rückgängig
> machen —, und zwar gegen `TdDocument` und nicht gegen ein `FlowDocument`.

**Wo gearbeitet wird: auf dem Windows-Rechner** (§5b). Der Grund ist diesmal nicht die
Werkzeugkette, sondern die Gegenprobe: **jede Änderung am Modell muss der WPF-Editor
überleben**, und beide Köpfe nebeneinander an derselben Datenbank gibt es nur dort.

#### Die Reihenfolge — und warum sie so herum ist

Dieselbe Regel wie in Phase 4: **erst das Modell, dann die Oberfläche.** Die Schritte 1–3
liegen vollständig in Core und sind damit **auf jedem Rechner prüfbar**, auch auf dem Laptop;
erst ab Schritt 4 wird es Kopfarbeit.

- [x] **1. Wo steht der Cursor?** ✅ **Erledigt 2026-08-12 (§4.30).** `TdPosition` (Absatz,
      Stück, Zeichen), `TdSelection` (Anker und Spitze) und `TdCursor` (geradeziehen, bewegen,
      lesen) stehen in `Core/Text/TdPosition.cs`; **28 Wächter**. Die Stelle steht **im
      Modell**, wie vorgeschlagen. Vier Entscheidungen, die dahinter fielen und die Schritt 2
      erbt: die **linke** Schreibweise einer Stückgrenze ist die kanonische (daran hängt die
      Formaterbung), **Absatzende und nächster Absatzanfang bleiben zwei Stellen**, ein Feld
      und ein Bild sind **ein Schritt breit und kein Zeichen**, und bewegt wird in **ganzen
      Zeichen** und nicht in UTF-16-Einheiten.
- [x] **2. Was ändert sich, wenn man tippt?** ✅ **Erledigt 2026-08-12 (§4.32).** `TdEdit`,
      `TdFragment` und `TdChange` in `Core/Text/TdEdit.cs`; **58 Wächter**. Alles läuft über
      **einen** Handgriff (`Ersetzen`) — damit fällt „Absätze verbinden" von selbst heraus. Eine
      Änderung merkt sich **die Blöcke davor und danach** und nicht den Handgriff; daraus folgt
      die Regel, die der ganze weitere Weg erbt: **Absätze und Stücke werden nie verändert,
      sondern ersetzt.** Ein eingefügtes Zeichen erbt das Format **links** davon (die Einlösung
      der kanonischen linken Form aus §4.30). **§5 „Noch offen" 8 ist mit erledigt.**
      **Eine benannte Lücke:** über eine Tabellengrenze hinweg wird nicht bearbeitet (§4.32).
- [x] **3. Rückgängig.** ✅ **Erledigt 2026-08-13 (§4.33).** `TdUndo` in `Core/Text/TdUndo.cs`;
      **21 Wächter**. **Die Frage, wem der Stapel gehört, ist mit drei Gründen beantwortet: er
      steht *neben* `UndoStack`** — `Push` dort verlangt eine `WbPage`, die Antwort ist dort
      eine Seite und hier eine **Auswahl**, und das **Zusammenfassen** gibt es dort gar nicht.
      Die ~20 Zeilen Doppelung sind benannt und gehören in die Aufräumrunde von Phase 6.
      **Getippte Zeichen werden zu einem Schritt**, Schnitte sitzen an Wortgrenzen, zwischen
      Arten und bei jeder Strukturänderung — **alle an der Gestalt der Änderung und nicht an
      der Uhr** (Core fragt sie nicht, §4.20).
- [x] **4. Der Cursor auf dem Schirm.** ✅ **Erledigt 2026-08-13 (§4.34).** `TdHit` in
      `Core/Text/TdHit.cs` (Klick → Stelle, Stelle → Papier), Schreibmarke und Auswahl im
      `TdRenderer`; **34 Wächter**. **Der Rückweg aus §4.30 war die Hälfte** — ein Absatz sagt
      nicht, das wievielte Zeichen getroffen wurde, und nachzählen geht nicht (weggeworfener
      Leerraum, gerechnete Feldwerte, Umbrüche ohne Stück). Jedes gesetzte Stück trägt jetzt
      seine Stelle im Absatz. **Die Markierung geht je Zeile an den Zeichner**, damit die
      Auswahl hinter dem Text und über dem Zellhintergrund liegt.
- [x] **5. Tastatur und Maus im Linux-Kopf.** ✅ **Erledigt 2026-08-16 (§4.35).**
      `TextDocView.Eingabe.cs`; **22 Wächter**. Tippen über **`TextInput`** (Umlaute und tote
      Tasten), Absatz teilen, löschen, Pfeile in alle vier Richtungen, Strg+Pfeil wortweise,
      Pos1/Ende, Bild auf/ab, Umschalt-Auswahl, Ziehen, Doppel- und Dreifachklick, Strg+A/C/X/V
      und Strg+Z/Y. **Drei Rechnungen kamen dafür in Core dazu:** `TdHit.Hoch`/`Runter` samt
      angepeilter Spalte (`TdZeilenzug`), `TdHit.Zeilenrand` und `TdCursor.Wort`/
      `WortLinks`/`WortRechts`. **Modell und Verlauf liegen am Register** und nicht an der
      Ansicht — sonst zeigte ein Verlaufsschritt nach jedem Registerwechsel auf ein Dokument,
      das es nicht mehr gibt (§4.33). **Der Fund: die Schreibmarke war zu hoch** — sie ging
      über die Zeilenhöhe samt Absatzabstand; jetzt steht sie um die Grundlinie herum. Kein
      Wächter konnte das sehen, nur der Schirm (§4.28).
      **⚠ Und die Gegenprobe hat den ernsten Befund gebracht:** Bei gefülltem `Rtf` zeigt der
      WPF-Editor die Linux-Arbeit nicht und löscht sie beim nächsten Speichern —
      **§5 „Noch offen" 9, zur Entscheidung.**
- [~] **6. Formate setzen und das Ribbon.** **Erste Hälfte erledigt 2026-08-17 (§4.36).**
      `Core/Text/TdFormatEdit.cs` (Zeichen- und Absatzformat auf eine Auswahl, samt der Auskunft
      „was zeigt die Auswahl gerade?") und `Views/TextDocView.Format.cs`; **31 Wächter**. Im
      Reiter „Start" stehen jetzt F, K, U, S, hoch/tief, Schriftgröße ±, die vier Ausrichtungen
      und der Einzug — dazu Strg+B/I/U. **Es läuft ausdrücklich nicht über `TdEdit.Ersetzen`**,
      mit zwei Gründen belegt (Absatzformat der Zwischenabsätze; der Verlauf würde
      zusammenfassen). **Vor dem Bauen geprüft, dass der Zeichner jeden dieser Knöpfe auch
      wirklich malt** — die Lehre aus §4.26.
      **✅ Zweite Hälfte erledigt 2026-08-17 (§4.37):** `Core/Text/TdBlockEdit.cs`,
      `Core/Text/TdTableEdit.cs`, `TdFormatEdit.Verweis` und `Views/TextDocView.Reiter.cs`;
      **30 Wächter**. Die drei Reiter **Einfügen** (Seitenumbruch, Zeilenumbruch, Tabelle,
      Felder), **Verweise** (Verweis setzen/entfernen, Inhaltsverzeichnis) und **Tabelle**
      (Zeile/Spalte einfügen und löschen, Tabelle löschen) stehen. **`TdFragment` hat dafür
      *keine* Blöcke bekommen** — sonst trüge jeder Tastendruck eine Möglichkeit mit sich, die
      er nie braucht.
      **Benannt ausgelassen statt halb gebaut** (§4.28): **Zellen verbinden und teilen** (was
      geschieht mit dem Inhalt der aufgehenden Zellen? — geraten wäre es Datenverlust), Bilder
      (Phase 4.5), Beschriftungen und Fußnoten, sowie Schriftartenliste, Textfarbe und
      Hervorhebung aus §4.36.
      **⚠ Am laufenden Programm ist von §4.36 und §4.37 nichts gesehen worden** (§4.36, „Was
      diese Runde nicht belegen konnte").
      **„Nur Ansicht" ist schon weg** und heißt seit §4.35 „Nur Text": Der Satz war mit Schritt 5
      falsch geworden, und ein falscher Satz auf dem Schirm wartet nicht auf den nächsten
      Schritt.
- [x] **6a. `TextInputMethodClient`** — **gebaut am 2026-08-18** (§4.41). `Core/Text/TdEingabe.cs`
      und `Views/TextDocView.Eingabemethode.cs`: Die Fläche meldet sich als Eingabeziel an und
      beantwortet, wo die Marke steht, was der umgebende Text ist und was davon ausgewählt ist.
      **Die Rechnung liegt in Core** — der iPadOS-Kopf erbt sie für `UITextInput`, der WPF-Kopf
      mit Schritt 7.
      **Der eine Satz, der alles trägt:** In `TdEingabe.Text` ist jeder Cursorschritt genau
      **ein** Zeichen breit (Feld, Bild und Diagramm bekommen U+FFFC) — sonst zeigte jeder
      Abstand, den eine Eingabemethode zurückreicht, hinter jedem Feld um eins daneben.
      **Benannt ausgelassen war** das Zusammensetzen (`SupportsPreedit = false`) — ✅ **seit
      §4.43 nachgeholt, siehe Schritt 6b.**
      **✅ Der Laptop hat geantwortet** (V2-55, §4.41): Die Bildschirmtastatur **klappt nicht
      von selbst auf** (`Avalonia.X11` hat keine `IInputPane` — kein Kopfcode ändert das),
      **schreibt aber, von Hand hervorgeholt**. Der halbe Zweck ist damit erreicht; für den
      iPadOS-Kopf ist die Naht die Voraussetzung.

- **✅ Schritt 6b — das Zusammensetzen (Preedit)** (§4.43, V2-57): `TdVorschau` in
      `Core/Text/TdEingabe.cs`, `SupportsPreedit => true`, `SetPreeditText` und
      `VorschauMalen` im Avalonia-Kopf. **20 Wächter.**
      **Es ist die Behebung einer Regression und keine Kür:** `SupportsPreedit` ist unter
      X11/IBus keine Anzeigefrage, sondern das **Fähigkeitswort an IBus** — mit `false` fielen
      **tote Tasten still weg** (§4.42).
      **Der eine Satz, der alles trägt:** Der unfertige Text ist **Ansichtszustand** und wird
      an der Marke gemalt — **`TdDocument` wird nie angefasst**, damit greift §4.32 nicht.
      In Core steht nur das Klemmen, und dort sitzt die eigentliche Wache: **eine Marke
      zerschneidet kein Ersatzpaar**.
      **⏳ Am laufenden Programm ist nur die Gegenprobe gefallen** (Windows: TSF tippt
      zeichengenau weiter). **Ob die toten Tasten wiederkommen und wie der unfertige Text
      aussieht, sagt der Laptop** — §5d.

#### Was dem Linux-Editor noch fehlt — die vollständige Liste

**Neu am 2026-08-17 (§4.38), auf eine Frage des Nutzers hin.** Ein Teil davon stand in §4.36 und
§4.37 als „benannt ausgelassen", der größere Teil stand **nirgends** — und ohne Liste ist der
Unterschied zwischen „bewusst offen" und „übersehen" nur im Kopf dessen, der sie gelassen hat.

**Alles hiervon gibt es im WPF-Kopf**, gehört also zur Funktionsgleichheit (M2). Sortiert nach
dem, was es kostet:

| | Was fehlt | Was es braucht |
|---|---|---|
| ✅ **A** | ~~Aufzählung und Nummerierung~~ | **Erledigt 2026-08-17 (§4.39)** — `TdListEdit.Umschalten`/`Ebene` |
| ✅ **A** | ~~Überschriften-Vorlagen~~ | **Erledigt 2026-08-17 (§4.39)** — `TdStil` in Core, `TdListEdit.Vorlage`, Wächter gegen die WPF-Tabelle |
| ✅ **A** | ~~Schriftgröße als Punktliste~~ | **Erledigt 2026-08-17 (§4.39)** |
| ✅ **B** | ~~Schriftart wählen~~ | **Erledigt 2026-08-17 (§4.40)** — die Naht war schon da: `Fonts.Mitgeliefert` **ist** der Bestand, und eine Liste der *System*schriften wäre sogar falsch (§4.26) |
| ✅ **B** | ~~Schriftfarbe und Hervorhebung~~ | **Erledigt 2026-08-17 (§4.40)** — Kacheln aus `TdTextfarben` (Core) statt eines Farbrads, wie auf der Zeichenfläche |
| ✅ **B** | ~~Trennlinie~~ | **Erledigt 2026-08-17 (§4.40)** — `TdBlockEdit.Trennlinie` |
| ✅ **B** | ~~Kopf- und Fußzeile bearbeiten~~ | **Erledigt 2026-08-17 (§4.40)** — dritter Abschnitt der Einstellungsleiste |
| **B→C** | **Hintergrundbild / Wasserzeichen** | `TdPageSetup.Watermark` ist ein `TdImage` und braucht damit **den Blob-Weg wie jedes Bild** — es gehört deshalb zu C und nicht zu B; die erste Einschätzung war zu optimistisch |
| **C** | **Bilder einfügen** | Blob-Speicher **und** Dateidialog; `TdImage` steht seit §4.21 |
| **C** | **Diagramme** | `TdChart` und der Zeichner stehen seit §4.25 — es fehlt der **Dialog** (WPF hat `ChartDialog`) |
| **C** | **Infoboxen** | Im Modell nicht vorgesehen — im WPF-Kopf ein Absatz mit Rahmen und Füllung; zu entscheiden, ob es ein eigener Blocktyp wird |
| **C** | **Symbole einfügen** | Eine Zeichentafel; im Modell ist es gewöhnlicher Text und damit trivial — der Aufwand liegt ganz in der Oberfläche |
| **C** | **Beschriftungen, Fußnoten, Textmarken** | Im Modell nicht vorgesehen; eigene Runden |
| **C** | **Zellen verbinden und teilen** | Verlangt eine Antwort darauf, was mit dem Inhalt der aufgehenden Zellen geschieht (§4.37) |

> **A = geht mit dem, was Core heute kann** (reine Kopfarbeit). **B = eine kleine Ergänzung
> vorweg.** **C = eigene Runde, teils mit Modellfrage.**
>
> **Wo das hingehört:** A und B sind der Rest von Phase 4 — sie machen den Linux-Editor
> benutzbar. C überschneidet sich mit **Phase 4.5** (Bilder brauchen denselben Blob-Weg wie die
> Zeichenfläche). **Die Reihenfolge entscheidet der Nutzer**; vorgeschlagen ist A, dann B, dann
> Schritt 6a und Schritt 7, dann C zusammen mit Phase 4.5.
- [x] **6c. Die Rundreise dicht machen.** ✅ **Erledigt 2026-08-21 (§4.45).** `TdZuFlow` löst
      die Kaskade auf statt sie durch Null zu ersetzen, `FlowZuTd` kürzt wieder auf
      Abweichungen — die beiden sind Umkehrungen voneinander. Dazu Tabellenformat aus dem
      Modell, der eigene Abstand des Listenpunkts, Rundung auf zehn Nanometer und
      `DefaultParaFormat`. **9 Wächter**, **832 Tests**.
      **Der Anlass war eine Messung und keine Vorsicht:** Ein Modell, wie der Linux-Kopf es
      anlegt, verlor auf **einer** Rundreise sieben Dinge — allen voran den Absatzabstand
      (`Standard.SpaceAfterPt` ist **8** und nicht 0) und die Ausrichtung (ein `FlowDocument`
      steht von Haus aus auf Blocksatz, §4.37 Fund 2). **Und der Verlust wartet nicht auf
      Schritt 7:** `Migrate` läuft seit §4.23 bei jedem Speichern, also verliert ein
      importiertes Dokument heute schon beim ersten Speichern im WPF-Editor seine
      Tabellenrahmen und Absatzabstände.
      **Drei Lücken bleiben und haben eigene Wächter** (§4.19): Diagramm, Feld und
      Gliederungsebene überleben die Rundreise nicht — **kein Fleiß schließt sie**, solange der
      Weg über das `XamlPackage` läuft (gemessen: `Tag` und `ToolTip` überleben es nicht).
      **Das ist §5 „Noch offen" 12**, und es steht vor Schritt 7.

- [x] **6d. Punkt und Pixel.** ✅ **Erledigt 2026-08-21 (§4.46).** `TdStil` rechnet in
      echten Punkten — alle Zahlen mal 0,75, Körper 11,25, Überschrift 1 **21**; die Abstände
      ebenso, der Einzug des Zitats auf die krumme Zahl, die richtig ist.
      `TdCharFormat.Standard` bleibt bei **11** (Vorgabe des Formats, keine Vorlage).
      **3 neue Wächter, einer umgedreht, 835 Tests**, Golden-Files unbewegt.
      **Die Entscheidung ist dabei einmal zurückgenommen worden** — die erste stand auf einer
      ungeprüften Deutung des Befunds aus §4.45.
      ✅ **Und eine der drei Lücken aus §4.45 hat sich damit von selbst geschlossen:** Die
      **Gliederungsebene** überlebt die Rundreise wieder. **Es sind noch zwei.**
      ✅ **In beiden Köpfen am laufenden Programm gesehen** — im Linux-Kopf ändert „Standard"
      die Größe eines unberührten Absatzes nicht mehr, im WPF-Kopf steht für dieselbe
      Überschrift **28** im Ribbon und **„Überschrift 1" ist in der Galerie markiert**.

- [x] **6e. Der Ladeweg.** ✅ **Erledigt 2026-08-22 (§4.47).** `AusModell` lädt direkt statt
      über ein `XamlPackage`; die Übernahme steht als `TdZuFlow.InhaltUebernehmen` neben
      `Umwandeln`. **5 Wächter, 840 Tests**, Führung unverändert.
      **Die Auflage „erst messen" hat den Entwurf umgeworfen:** WPF **kopiert** ein `Tag` beim
      Teilen eines Absatzes **und** eines Laufs auf beide Hälften — ein Träger dort wäre nach
      einem Tastendruck doppelt und damit **schlimmer als die Lücke**. Übrig bleibt der
      **`InlineUIContainer`**, unteilbar und derselbe Ort, an dem `DocumentImages` seit jeher
      trägt.
      **Ein Fund nebenbei:** Das Paket schiebt die Dokumentschrift als **örtlichen Wert** auf
      jeden Absatz — derselbe Fehler wie §4.45, an anderer Stelle; mit Schritt 7 wäre er in
      jedes Dokument gewandert.
      ⏳ **Die Träger selbst gehören zu Schritt 7**, wo das Paket aus dem Speicherweg fällt —
      vorher überlebten sie das Laden und nicht das Speichern.

- [x] **7. `Rtf` verliert die Führung.** ✅ **Erledigt 2026-08-22 (§4.48) — der eigentliche
      Zweck des ganzen Wegs** (§5). Der WPF-Editor liest **und schreibt** das Modell; `Rtf`
      wird **nie überschrieben** (§4.22) und bleibt als Sicherung stehen. `Migrate` läuft nur
      noch für die **einmalige** Übernahme — sonst überschriebe es beim nächsten Speichern,
      was gerade getippt wurde. **Es ist Antwort (d) aus §5 Nr. 9, die damals ausdrücklich
      falsch war** und es seit dem Umdrehen der Führung nicht mehr ist.
      **`TdFuehrung.AltformatFuehrt` ist gelöscht** (nicht auf `false` gesetzt), mit ihm vier
      Wächter und der **Warnstreifen im Linux-Kopf**. **837 Tests.**
      **✅ §5 „Noch offen" 9 ist damit an der Wurzel erledigt** — nicht mehr gewarnt, behoben.
      **✅ In beiden Köpfen belegt und danach in der Datei nachgemessen:** Linux schreibt →
      Windows bearbeitet → Linux liest beides; in der Datenbank `Rtf` **Länge 0**, `Model` mit
      dem Windows-Text.
      **Ein Wächter musste umgedreht werden** (`Das_Modell_fuehrt_auch_bei_vollem_Altfeld`),
      und ein neuer hält fest, dass „führt nicht mehr" nicht „ist weg" heißt.
      ⏳ **Die zwei Lücken aus §4.45** (Diagramm, Feld) sind **nicht** mitgekommen — sie sind
      aber **jetzt zum ersten Mal schließbar**, weil das `XamlPackage` ganz aus dem Weg ist.
      Eigene Runde, siehe §5e.

- [x] **8. Die zwei Träger.** **Erledigt 2026-08-22, in zwei Runden** (§4.49 und §4.50).
      Ein **Feld** reist als `InlineUIContainer` mit dem `TdField` als Auflage, ein
      **Inhaltsverzeichnis** als `BlockUIContainer`, ein **Diagramm** auf demselben Träger wie
      das Feld, nur mit einem `Image` darin. **Alle fünf Feldarten und das Diagramm überleben
      die Rundreise**, vorher keine einzige. **13 Wächter**, zwei umgedreht, **849 Tests**.
      **Der Träger ist gemessen und nicht gewählt** (§4.47): `Tag` an Absatz und Lauf wird beim
      Teilen **kopiert** und wäre nach einem Tastendruck doppelt — **schlimmer als die Lücke**.
      **✅ Der Augenschein ist gefahren** (§4.50): Grundlinie **895** für den gewöhnlichen Lauf
      *und* für den Behälter, das Verzeichnis sieht aus wie eines, die Runde zurück in den
      Linux-Kopf rechnet „Seite 1" wieder aus.
      **✅ Für das Diagramm ist `TdRenderer.Diagramm` der öffentliche Einstieg** — ohne
      Rückgabewert, denn er zeichnet auch den Platzhalter; `GrafikZeichnen` ruft ihn, damit
      Editoranzeige und gedruckte Seite nicht auseinanderlaufen. **Kein Golden-File bewegt.**
      **Schließt zugleich die Lücke aus §4.28** („ein Diagramm kommt im WPF-Editor nicht an").
      **⚠ Benannt bleibt:** Der WPF-Editor rechnet keine Seitenzahlen (§5 „Noch offen" 15).

#### Drei Fallen, die schon feststanden — zwei sind erledigt

| | |
|---|---|
| ✅ **Der Umbruch läuft mehrfach** | **Gemessen und beantwortet (§4.35).** Und die Vermutung war falsch: Das mehrfache Rechnen des Verzeichnisses kostet ein Zehntel, nicht ein Vielfaches — teuer ist die **Länge** des Dokuments (2 Seiten 4,5 ms · 9 Seiten 25 ms · 35 Seiten 208 ms). Unter 40 ms wird sofort umbrochen, darüber über den Nachrichtenlauf gesammelt. **Nur den betroffenen Absatz neu zu setzen bleibt ungebaut** — es ist keine Kleinigkeit, weil jede Änderung die Seitenumbrüche danach verschiebt |
| ✅ **Die Anzeige rechnet einmal je Dokument** | **Erledigt mit §4.35** — sie rechnet jetzt nach jeder Änderung, gesammelt wie oben |
| ⚠ **Zwei Editoren auf einem Modell** | **Nachgemessen und schlimmer als hier beschrieben (§4.35):** Es sind nicht nur zwei Wahrheiten — der WPF-Editor **überschreibt** das Modell beim nächsten Speichern aus `Rtf`, und die Linux-Arbeit ist still weg. Betrifft **jedes Bestandsdokument**. Schritt 7 löst es auf; **bis dahin steht es als Entscheidung in §5 „Noch offen" 9** |

> **Was dabei mitgenommen gehört, weil es ohnehin am Text hängt:** ein Verweis wird von
> `TdRenderer` **ohne jede Kennzeichnung** gemalt (kein Blau, keine Unterstreichung) und
> bekommt im PDF so viele Kästen, wie er Wörter hat (beide §4.27). Und der Wortzwischenraum
> an einer Stückgrenze sitzt in der **Anzeige** falsch (§5 „Noch offen" 6, auf dem Laptop
> gefunden). Alle drei sind Arbeit am selben Zeichner.

### Vorgemerkt: Phase 4.5 — die fehlenden Werkzeuge des Linux-Kopfs

**Nutzer-Entscheidung 2026-08-04, nach einer Rückfrage — und die Rückfrage war berechtigt.**

**Der Befund:** Diese Werkzeuge waren **benannt, aber nicht eingeplant**. Sie stehen in §6
(„Was Phase 3 bewusst ausgelassen hat"), und **beide README-Fassungen sagen es den Nutzern
wörtlich**: „Formen-Stift, Textfelder, Notizzettel, Sticker, Zahlenblock, Quick-Options-Menü,
Lineal und Geodreieck — *kommen nach der Dokument-Engine*". Nur besaß sie **keine Phase**:
Roadmap-Phase 4 enthält die Dokument-Engine, das Umverdrahten der Exporter und das Ribbon,
Phase 5 ist iPadOS, dazwischen steht nichts.

> **Der eigentliche Fehler im Plan ist nicht das Vergessen, sondern M2.** Die Roadmap
> definiert M2 als **„Funktionsgleichheit Linux ↔ Windows"** und hängt ihn an das Ende von
> Phase 4. Diese Werkzeuge gehören zur Funktionsgleichheit — **M2 ist mit Phase 4 allein
> also gar nicht erreichbar.** Der Aufwandsschätzer (8–12 Wochen) deckt nur die Engine.

**Was dazugehört** — alles davon ist **im Windows-Kopf vorhanden** und fehlt nur im
Avalonia-Kopf:

| | |
|---|---|
| **Werkzeuge zum Anlegen** | Formen (Linie, Pfeil, Rechteck, Ellipse, Dreieck) samt Füllung und Deckkraft, **Formen-Stift** (erkennt gezeichnete Formen), Textfelder, Notizzettel, Sticker |
| **Zeichenhilfen** | Lineal und Geodreieck (drehbar, rastend; eigene SVG aus dem Datenordner) |
| **Auswahl** | Drehen und Skalieren — heute kann der Linux-Kopf nur verschieben und löschen |
| **Einfügen** | Bilder (PNG/JPEG/BMP/GIF/WebP/SVG) und **PDF-/Word-Seiten** mit Seitenauswahl |
| **Bedienung** | **Langdruck auf den Größenregler → Zahlenblock**, Schnellaktionen-Menü auf der Fläche, **die Reihenfolge der Werkzeugleiste** wie im Windows-Kopf |
| **Dienste** | Texterkennung (OCR) und Rechtschreibprüfung — **die zwei Ausnahmen**: sie brauchen Gegenstücke zu den Windows-Diensten (`IOcrEngine`, `ISpellChecker` stehen seit Phase 2), das ist eigene Arbeit und keine reine Portierung |

> ### ▶ Zuschnitt und Reihenfolge — **entschieden am 2026-08-22** (Nutzer, nach der Rückfrage aus §5e)
>
> **① Volle Breite, aber die Rechtschreibprüfung wird herausgenommen** und eigens geführt.
>
> **Der Grund ist gemessen und nicht geschätzt** — die zwei „Ausnahmen" oben sind **ungleich
> teuer**, und das stand so nicht in der Liste:
>
> | | |
> |---|---|
> | **OCR** | Die Naht steht **vollständig** (`IsAvailable` + `Recognize`), `OcrService` ist 93 Zeilen, `tessdata` liegt im Repo. Linux braucht eine Tesseract-Umsetzung plus `libtesseract` vom System. **Klein — aber von Windows aus nicht messbar**, das ist Laptop-Arbeit |
> | **Rechtschreibung** | Die Naht steht **nicht**. `ISpellChecker` liefert heute nur `IsSupported(bcp47)` — ja/nein. Der eigene Kommentar sagt es wörtlich: *„Heute prüft das nur — die Markierungen selbst zeichnet die WPF-`RichTextBox`. Ab Phase 4 muss die Schnittstelle auch die Fundstellen liefern."* Das ist **keine portierte Bedienung, sondern eine neue Funktion der Dokument-Engine**, und der Linux-Editor müsste die Wellenlinien selbst zeichnen |
>
> **⚠ Was das für M2 heißt, und es ist bewusst so entschieden:** M2 heißt „Funktionsgleichheit",
> und der WPF-Kopf **hat** die Rechtschreibung. **M2 wird also mit einem benannten Loch
> ausgerufen.** Das ist der Preis dafür, das teuerste und am schlechtesten vermessene Stück
> nicht in die Phase zu ziehen, die vor der Veröffentlichung steht.
>
> **✅ Und genau so ist es am 2026-08-28 geschehen** (Nutzer-Entscheidung, V2-90): **M2 ist
> ausgerufen**, sechs von sechs Stücken stehen und sind auf **beiden** Systemen am laufenden
> Programm gesehen (§4.64). **Das Loch bleibt benannt und gehört in jede
> Veröffentlichungsnotiz:** *die Rechtschreibprüfung fehlt im Linux-Kopf.* **Die zweite
> Stift-Taste gehört ausdrücklich nicht dazu** (§5 Nr. 17) — dort fehlt keine Funktion,
> sondern es ist eine Bedienentscheidung je Kopf.
>
> **▶ Was aus diesem Loch eine Aufgabe macht, steht schon in der Zeile darüber:** Die Naht
> `ISpellChecker` liefert heute nur ja/nein und **keine Fundstellen**. Wer die Lücke schließen
> will, baut zuerst die Schnittstelle um und dann die Wellenlinien im Linux-Editor — **das ist
> eine eigene Runde und keine Nacharbeit an Phase 4.5.**
>
> **② Angefangen wird mit der Auswahl — Drehen und Skalieren.** ✅ **Erledigt** (§4.51, V2-69).
> Der Grund: es ist die **Grundlage, die jedes neue Werkzeug erbt** — wer Formen baut, ohne
> drehen zu können, baut ein halbes Werkzeug —, und es war **sofort messbar**, ohne vorher
> etwas anlegen zu müssen: Striche und importierte Elemente gibt es im Linux-Kopf längst.
>
> **▶ Was als Nächstes ansteht**, in dieser Reihenfolge — der Vorschlag steht, entschieden ist
> er nicht:
>
> | | | |
> |---|---|---|
> | **1** | ✅ **Formen** (Linie, Pfeil, Rechteck, Ellipse, Dreieck) samt Füllung und Deckkraft — **erledigt** (§4.53, V2-71); davor die Zwischenrunde **Farbwähler** (§4.52, V2-70), vorgezogen, weil er auch 2 und 4 blockierte | Das größte fehlende Werkzeug. Zeigt den ganzen Weg — anlegen, auswählen, Undo, Werkzeugleiste — und liefert damit das Muster für Text, Notizzettel und Sticker. Der Zeichner steht schon (`WbRenderer` zeichnet `ShapeElement`), das Modell auch; es fehlt nur der Eingabeweg |
> | **2** | ✅ **Text, Notizzettel, Sticker — erledigt.** Die Zusammenlegung (§4.54, V2-72), **Text/Notizzettel** (§4.55, V2-73 bis V2-77) und **die Sticker** (§4.56, V2-78) — alles in beiden Köpfen belegt | Erben das Muster aus (1). **Ein Sticker ist kein eigener Elementtyp**, sondern ein `ImageElement`; die Sammlung ist eine Bildquelle (`StickerLibrary`, `Bildsammlung` in Core) |
> | **3** | ✅ **Bild- und PDF-Import — erledigt.** Die Zusammenlegung (§4.57, V2-79) und die Bedienung im Linux-Kopf (§4.58, V2-80): Bilder, SVG, PDF **und DOCX**, mit Seitenauswahl und Wartesperre | `PdfiumRasterizer` und `WbImagePrep` lagen in Core, die Platzierungsrechnungen kamen mit §4.57 dazu. **Im Notizbuch wird jede Seite ein eigenes Blatt** — dafür gibt es in beiden Köpfen keinen Undo-Schritt |
> | **4** | ✅ **Lineal und Geodreieck — erledigt.** Die Zusammenlegung (§4.59, V2-81) und die Bedienung im Linux-Kopf (§4.60, V2-82): auflegen, verschieben, drehen, Strich einrasten | **⚠ Die Einschätzung „reine Bedienarbeit" war falsch** — rund **200 Zeilen Geometrie** und die ganze Linealzeichnung lagen privat im WPF-Kopf. *Die Reihenfolge in §6 ist gemessen, die Aufwandsschätzungen daneben sind es nicht* |
> | **5** | ✅ **Zahlenblock, Schnellaktionen, Leistenordnung — erledigt.** Die Zusammenlegung (§4.61, V2-83: `WbKlon`, `WbZahlenblock`, `WbSchnellaktionen`, `WbLeiste`) und die Bedienung im Linux-Kopf (§4.62, V2-84): Zwischenablage samt Strg+C/X/V/D, Schnellaktionen, Zahlenblock, vier klappbare Gruppen und die Leiste in der Ordnung des WPF-Kopfs | **⚠ Die Einschätzung „die Bedienung“ war wieder zu klein** — 440 Zeilen lagen privat im WPF-Kopf, und **zwei Fehler in beiden Klonwegen** kamen dabei heraus (Drehung und Neigung gingen beim Duplizieren verloren). Der Fund der Bedienrunde war die **Routing-Strategie**: der Zahlenblock ging nicht auf, weil der `Slider` den Druck vorher verschluckte |
> | **6** | **OCR** | Braucht den Laptop (`libtesseract`), siehe oben |
>
> **Der Formen-Stift** (erkennt gezeichnete Formen) — **seit §4.53 vermessen: er steckt mit dem Glättstift in `WhiteboardView.Shapes.cs`, rund 240 Zeilen reine Geometrie im Kopf, also eine Zusammenlegung nach dem Muster von §4.51 und kein Neubau** — hängt an (1) und gehört dahinter, nicht
> davor.

**Wann: direkt nach Phase 4 — und damit vor allem anderen.** Begründung, in dieser
Reihenfolge:

1. **Erst die Engine, dann die Werkzeuge.** Textdokumente waren unter Linux ausgegraut — das
   war das größere Loch, und es blockierte zusätzlich Import/Export (§4.1).
   **✅ Eingelöst mit §4.28** (2026-08-11): die Engine steht, Textdokumente werden angezeigt,
   importiert und exportiert. Damit ist dieser Grund abgearbeitet und Phase 4.5 **an der
   Reihe** — es sei denn, das Schreiben im Linux-Kopf wird vorgezogen (§5 „Noch offen" 2).
2. **Vor dem iPad, nicht danach.** Der iPad-Kopf (seit dem 2026-08-18 **Phase 6**) baut den
   dritten Kopf auf derselben Avalonia-Grundlage. Wer die Werkzeuge erst danach baut, baut sie
   **zweimal** — einmal für Linux, einmal für iPadOS. Davor gebaut, bekommt der iPad-Kopf sie
   fast geschenkt (nur der UI-Umbau bleibt). **Die Umstellung der Reihenfolge verstärkt das
   Argument**, sie widerspricht ihm nicht: Der iPad-Kopf rückt weiter nach hinten.
3. **Neu seit dem 2026-08-18: M2 ist die Eintrittskarte zur Veröffentlichung.** Seit der
   Umstellung hängt **Phase 5 (öffentlich gehen)** unmittelbar an dieser Phase hier. **Was hier
   fehlt, fehlt am Tag der Veröffentlichung** — das ist der Unterschied zu vorher, wo noch ein
   ganzer iPad-Port dazwischenlag.
4. **M2 wird damit wieder wahr.** Er wandert ans Ende von Phase 4.5 statt ans Ende von
   Phase 4. **Daran hat die Umstellung vom 2026-08-18 nichts geändert** — M1 und M2 behalten
   ihre Bedeutung; umnummeriert wurde nur dahinter (M3 = veröffentlicht, M4 = TestFlight).

> **✅ Nachgetragen:** Das war eine Ergänzung zu `gonk-note-port-RM.MD` und nicht daraus
> abgeleitet — dieselbe Lage wie bei den eigenen Farbschemata unten. **Die Roadmap-Datei kennt
> Phase 4.5 und M2 seit dem 2026-08-11** (Nachtrag dort oben) und die neue Phasenfolge seit dem
> **2026-08-18**. **Beide Dateien sagen jetzt dasselbe.**

### Der Rest (Roadmap §5)

| Phase | Inhalt | Aufwand | Ziel |
|---|---|---|---|
| 3 | Avalonia-Shell für Linux — **fertig** | 6–8 W. | ✅ **M1 erreicht** — Notizbuch + Whiteboard laufen unter Linux, Textdokumente ausgegraut |
| 4 | Eigene Dokument-Engine in `Core/Text/` — **fertig**, samt Umverdrahten (§4.48) | 8–12 W. | ~~M2~~ — Textdokumente laufen unter Linux |
| **4.5** | **Die fehlenden Werkzeuge des Linux-Kopfs** (Formen, Lineal/Geodreieck, Textfelder, Sticker, Notizzettel, Bild-/PDF-Import, Zahlenblock, Schnellaktionen, Werkzeugleisten-Anordnung, Drehen/Skalieren) — **fertig, sechs von sechs Stücken** | 2026-08-23 bis 2026-08-28 | ✅ **M2 erreicht** — Funktionsgleichheit Linux ↔ Windows, **mit einem benannten Loch** (Rechtschreibprüfung) |
| **5** | **① UI/UX angleichen → ② Rückmeldung → ③ Flatpak/AppImage → ④ aufräumen und vollständig prüfen → ⑤ veröffentlichen** — die Ordnung ist am 2026-08-28 geändert worden, Begründung unten · ▶ **an der Reihe** | 3–5 W. | **M3 — veröffentlicht** (Linux + Windows), Version **1.0.0** |
| **5.6** | **Rechtschreibprüfung im Linux-Kopf** — das benannte Loch von M2, **fest eingeplant als erster Punkt NACH M3** (§5 Nr. 22) | 1–2 W. | Das Loch aus M2 ist zu |
| **6** | **iPadOS-Head**, Apple Pencil, PDFKit/Vision, AOT-Härtung, App Store | 7–12 W. | **M4** — TestFlight-Build |

> ### ▶ Die Reihenfolge ist am 2026-08-18 geändert worden — Nutzer-Entscheidung
>
> **Vorher:** Phase 5 iPadOS → Phase 6 Veröffentlichung (beide Plattformen in einem Schritt).
> **Jetzt:** Phase 5 **Veröffentlichung Linux/Windows** → Phase 6 **iPadOS**.
>
> **Die Begründung ist eine Priorität und kein neuer Befund:** Der **Linux-Port ist das Ziel
> des Projekts**, der iPad-Kopf ist ein Zusatz. Es gibt keinen technischen Grund, eine fertige
> Linux-Fassung mehrere Monate im privaten Repo liegen zu lassen, bis ein zweiter Port
> nachkommt.
>
> **Der Sache nach war das schon die Vorgabe.** `gonk-note-port-RM.MD` hält im Vorgespräch
> fest: „Reihenfolge: gemeinsame Basis zuerst, **Linux priorisiert, iPad danach**". Nur die
> Phasenfolge hat das für die *Veröffentlichung* nie abgebildet — **diese Umstellung korrigiert
> also einen Widerspruch im Plan** und führt keinen neuen ein.
>
> **✅ iPadOS bleibt vollständig Teil des Projekts und desselben Repos.** Nichts ist
> gestrichen, nichts ausgelagert. Es ist nur nicht mehr das, was die Linux-Fassung aufhält.
>
> **Was mitgewandert ist** — „alles, was mit dem Öffentlich-Gehen zusammenhängt":
>
> | | |
> |---|---|
> | **Nach vorn, in Phase 5** | Die Aufräumrunde samt vollständigem Prüflauf, die Checkliste „Vor dem Öffentlich-Schalten des Repos", **Flatpak/Flathub und AppImage**, `.deb`/AUR, MIT-Lizenztext + `THIRD-PARTY-NOTICES.md` |
> | **Nach hinten, mit Phase 6** | Der ganze Apple-Teil: Developer Program (99 $/Jahr), Privacy Nutrition Labels, Datenschutzerklärung-URL, App-Store-Review. **Ohne den iPad-Kopf haben sie keinen Gegenstand** |
> | **Unverändert** | **M1** und **M2** behalten ihre Bedeutung. Phase **4.5** bleibt, wo sie ist |
>
> **Umnummeriert:** Der TestFlight-Build hieß **M3** und heißt jetzt **M4**; **M3** ist die
> Veröffentlichung. *(Es gab genau eine Stelle im HANDOFF, die M3 nannte — die Tabelle hier.)*
>
> **⚠ Zwei Folgen, die leicht untergehen und deshalb hier stehen:**
>
> 1. **Die History-Frage rückt näher.** Ob `HANDOFF.md` per `git filter-repo` aus der
>    Vorgeschichte entfernt wird, war bisher zwei Phasen weit weg und ist jetzt die **nächste
>    Phase nach 4.5**. Sie steht unten in „Vor dem Öffentlich-Schalten" und ist **noch nicht
>    entschieden**.
> 2. **Bei der Veröffentlichung beschreiben die vier Dokumente zwei Köpfe, nicht drei.**
>    README und Erste-Schritte-Text sagen dann „Windows + Linux"; mit Phase 6 kommt eine dritte
>    Ausgabe dazu, in **beiden** Sprachfassungen (Dauerregel 1). **Keine zusätzliche Arbeit,
>    nur spätere** — und der iPad-Kopf entsteht dann in einem **öffentlichen** Repo, wird also
>    von Anfang an mitgelesen.
>
> **Phase 4.5 wird davon nicht berührt — im Gegenteil.** Ihr Grund Nr. 2 lautet „vor dem iPad,
> nicht danach: wer die Werkzeuge erst danach baut, baut sie **zweimal**". Der iPad-Kopf rückt
> weiter nach hinten, **das Argument wird also stärker und nicht schwächer**.

#### Phase 5 — die Ordnung, und warum sie am 2026-08-28 geändert wurde

> **▶ HIER STEHT DIE REIHENFOLGE, UND NUR HIER.** §0 und §5e verweisen darauf, statt sie zu
> wiederholen. *Ein Plan an drei Stellen ist einer, der an zweien veraltet.*

**Die Grundregel von 2026-08-05 gilt unverändert** *(die Phase hieß damals 6 und lag hinter
iPadOS — seit dem 2026-08-18 ist sie Phase 5 und liegt davor; siehe den Kasten oben)*:
**aufräumen, prüfen, veröffentlichen. Wer nach dem Aufräumen nicht mehr prüft, veröffentlicht
einen Stand, den nie jemand gesehen hat.**

**Voraussetzung ist M2** — Funktionsgleichheit Linux ↔ Windows (Ende Phase 4.5). Vorher hat
Veröffentlichen keinen Sinn: Was hinausgeht, soll auf Linux können, was es auf Windows kann.
**M2 ist am 2026-08-28 ausgerufen** (§2).

##### Die Schritte

| | Schritt | Was darin steckt |
|---|---|---|
| **①a** | **Vermessen** ✅ *erledigt (§4.71, §4.74, §4.75)* | Beide Köpfe Fläche für Fläche vergleichen und je Fläche eine **Befundtabelle** schreiben — **ohne anzugleichen**. *Wer misst und ändert zugleich, misst zum Teil seine eigene Änderung.* **Alle fünf Flächen sind vermessen.** |
| **①b** | **Angleichen** — *weitgehend erledigt* | Jeden sichtbaren Unterschied beseitigen, **wo beide Köpfe etwas haben und es verschieden aussieht**: Symbole, Farben, Kürzel, Tooltips, Anordnung. **Vorlage ist der Linux-Kopf — Ausnahme: beim Editor ist Windows die Vorlage.** ✅ Flächen **1, 4, 5** und die **Werkzeugleiste** der Tafel (§4.72–§4.75), dazu §5 Nr. **14**. **Offen:** die zwei benannten Reste aus §4.73/§4.74 |
| **①c** | **Die fehlenden Werkzeuge** ✅ *erledigt* | **Wo einer nichts hat, wird es dort gebaut** (§5 Nr. **26**) — Phase-4.5-Arbeit unter neuem Namen. ✅ **Dreizehn Runden, §4.77 bis §4.92**: Tafel-Export, Formen-Stift, Cover, Suchen & Ersetzen, Diagramm samt Ändern, Format löschen, Navigator, Wurzelfärbung und Tastenweg, Formatpinsel, Markenauswahl und Sonderzeichen, Bild/Infobox/Beschriftung/Objekt-Anordnung/Wasserzeichen, Tabellenentwurf in zwei Hälften. **Die sieben Entscheidungen aus §5e sind alle beantwortet** — fünf gebaut, eine gestrichen (Lineal), eine als Messung beantwortet statt als Frage gestellt (Strg+F). **⚠ Was bewusst offen bleibt, steht in §4.92** |
| **②** | **Rückmeldung** ✅ *erledigt* | Was der Vergleich übersehen hat und was ① kaputt gemacht hat. **Eine eigene Runde und kein Anhängsel** — wer eine Oberfläche in einem Zug umbaut und nicht noch einmal hinsieht, hat sie nicht geprüft, sondern nur geändert. **✅ Gefahren in zwei Runden:** §4.93 die beiden Beobachtungen aus §4.86 (**eine war keine**), §4.94 der Durchgang durch alle Flächen beider Köpfe — **sieben Funde, sechs repariert**, dazu **drei Unterschiede, die keine Löcher sind** und deshalb ausdrücklich benannt stehen. **Und das Design-Konzept gilt seither für beide Köpfe** (Nutzer-Auftrag 2026-09-03). ⚠ Der Rest ist klein und steht in §5e — keine eigene Runde wert |
| **③** | **Auslieferung — vorgezogen** ✅ *gebaut und gestartet (§4.96)* | **Flatpak/Flathub** als Hauptweg, **AppImage** als zweiter, abhängigkeitsfreier Kanal, `.deb`/AUR optional. ✅ **Beides läuft auf dem Laptop** (2026-09-04, V2-119): `packaging/flatpak` und `packaging/appimage`, `fontconfig`/`freetype` aus der Plattform, **`tesseract`/`leptonica` im Manifest mitgebaut** — die Erkennung trägt **in der Sandbox** gemessen (§4.63, §5 Nr. 18). **⛔ Erprobung, keine Auslieferung** — der zweite Bau nach ④ geht hinaus. ⚠ Offen bleibt, was nur eine Hand am Gerät zeigen kann: **Dateidialog** und **Stift** in der Sandbox (§4.96) |
| **④** | **Der Rest des Aufräumens, dann der vollständige Prüflauf** | Die Vorratsliste unten, dann der ganze Prüflauf |
| **⑤** | **Veröffentlichen** ✅ *erledigt (§4.101)* | READMEs (Installationswege, sieben Bildschirmfotos aus `tools/demo-db`), **GitHub Pages** in `site/` samt `pages.yml`, **`release.yml`** an einem `v*`-Tag, Repo-Beiwerk (`CONTRIBUTING`, `SECURITY`, Issue- und PR-Vorlagen), `Docs/HANDOFF.md` **und die ganze Git-Historie** auf Privates durchgesehen (beides sauber), Version **1.0.0** an fünf Stellen, **Repo auf „public" — vom Nutzer geschaltet**. ⚠ **Drei Handgriffe bleiben seinem Konto:** Tag `v1.0.0` schieben (er löst das Release aus), Pages-Quelle auf „GitHub Actions" stellen, Beschreibung und Topics setzen |

> **▶ Warum ① in ①a und ①b zerfällt, und warum das keine zusätzliche Runde ist:** Beim
> Messen hat sich gezeigt, dass ① **kein Zuschnitt für eine Runde** ist — dem Linux-Kopf
> fehlen ganze Werkzeuge (Cover, Suchen & Ersetzen, Tabellenentwurf, sechs
> Einstellungs-Klappgruppen), und mit §5 Nr. 26 werden sie **gebaut** und nicht drüben
> gestrichen. **Die Teilung benennt diesen Umfang, statt ihn in einer Runde zu verstecken.**
> Der Nutzer entscheidet den Zuschnitt von ①b am Befund von §4.71 — nicht vorweg.

##### ⚠ Warum ② hinter ①c rückt — und warum es keine Streichung ist

**Nutzer-Entscheidung 2026-08-31** (§5 Nr. 28). Beim Vermessen aller fünf Flächen hat sich
gezeigt, dass ① **zwei verschiedene Arbeiten** unter einem Namen führt: das **Angleichen**
dessen, was beide Köpfe haben, und das **Bauen** dessen, was einem fehlt. Das Zweite ist
Phase-4.5-Arbeit unter neuem Namen.

**Eine Rückmeldung dazwischen liefe leer:** Sie nennte in der Mehrzahl Werkzeuge, die
ohnehin auf der Liste stehen — und **wer zwei Listen führt, arbeitet die kürzere ab.**

> **⛔ Und was das nicht heißt:** ② fällt **nicht weg**. *Wer eine Oberfläche in einem Zug
> umbaut und nicht noch einmal hinsieht, hat sie nicht geprüft, sondern nur geändert* — der
> Satz gilt unverändert. ② rückt nur dorthin, wo es eine **vollständige** Oberfläche zu
> prüfen gibt. *Eine Prüfung, die zu früh kommt, ersetzt keine, die zur richtigen Zeit
> kommt — sie verbraucht nur die Aufmerksamkeit dafür.*

##### ⚠ Warum ③ vor ④ steht — und warum die Grundregel damit NICHT gestrichen ist

**Nutzer-Entscheidung 2026-08-28, und die Begründung ist gemessen und nicht hergeleitet:**
Die Auslieferung ist **das einzige Stück, das noch nie gelaufen ist**; alles andere ist
Nacharbeit an Bekanntem. Ein Flatpak, das erst nach dem Aufräumen zum ersten Mal gebaut wird,
**findet seine Fehler zum spätestmöglichen Zeitpunkt** — und **§4.64 hat die
Tesseract-Bindung ausdrücklich außerhalb eines Flatpaks vermessen**, die Sandbox ist also
ungeprüft. Dazu kommt, was §5 Nr. 25 benennt: Der UI-Vergleich läuft nur unter Windows, also
ist ③ ohnehin der erste Durchgang auf echtem Linux.

> **⛔ Und deshalb steht hier ausdrücklich, was ③ NICHT ist:** ③ ist eine **Erprobung** und
> **keine Auslieferung**. Das Flatpak wird **nach ④ noch einmal gebaut und laufen gelassen**,
> und **erst dieser zweite Bau geht hinaus**. Ohne diesen Satz liest die nächste Runde das
> Vorziehen als Streichung der Grundregel — und veröffentlicht genau den Stand, den nie
> jemand gesehen hat.

##### Was in ④ ansteht

*(Die Liste ergibt sich aus dem, was die Phasen bewusst stehen gelassen haben — sie ist beim
Aufräumen zu **ergänzen**, nicht abzuarbeiten wie ein Vertrag.)*

| | |
|---|---|
| ~~**Toter Code, zweiter Lauf**~~ | ✅ **Beide Runden erledigt** (§4.68 für `src/`, §4.97 für `tests/` und `tools/`). **Der zweite Lauf war leer, und das ist das Ergebnis** — fünf Sonden, kein totes Mitglied, keine tote Datei. **Was wirklich tot war, stand in den Sprachtabellen:** `Ed.FitWidth`/`Ed.FitPage`, gelöscht, jetzt **579/579** symmetrisch. ⚠ Und die Sonde, die zuerst lief, war die falsche — `private` ist **dateilokal** und muss auch so gezählt werden |
| ~~**Doku gegen den Code, zu Ende**~~ | ✅ **Zu Ende gelesen in §4.97** — **fünf weitere Stellen**, darunter wieder eine gefährliche: §7 riet bis zum 2026-09-04, ein neues Symbol als **Vektorform in `Themes/Styles.axaml`** anzulegen, obwohl §4.31 den Satz seit dem 2026-08-12 in Core zusammengezogen hat. **⚠ Der eigentliche Befund war der Abschnitt selbst:** §7 hatte zwischen §4.67 und §4.96 **keinen einzigen neuen Eintrag** — alles aus Phase 5 stand nur im Prompt von §5e, **und der wird jede Runde überschrieben**. Neu in §7: „Neu aus Phase 5 (§4.68–§4.96)" |
| ~~**Aufgelöste Nähte wegräumen**~~ | ✅ **Gemessen in §4.99:** Von zwölf Diensten im WPF-Kopf **mussten elf dort stehen** (WPF-Typen oder Windows-API). **Der zwölfte war `MarkdownImporter`** — 394 Zeilen eigene Markdown-Grammatik neben dem Zerleger in Core, **zum fünften Mal dieselbe Lage**. Er liegt jetzt als `TdMarkdown.Lesen` in Core, und damit ist ein **unbenanntes Loch in M2** zu: Der Linux-Kopf konnte `.md` **überhaupt nicht** importieren |
| ~~**Benannte Lücken schließen oder benennen**~~ | ✅ **Alle drei entschieden (§4.99).** Die **Tabelle in einer Zelle** war von der Oberfläche aus **erreichbar** — auf Nutzer-Entscheidung **gesperrt**, die Lücke bleibt benannt. ⛔ **Und die Behauptung in dieser Zeile war falsch:** „Jede ist heute mit einem Wächter festgehalten" stimmte nur für die Tabelle; die zwei aus §4.21 hatten **keinen**, und die Palettenlücke war im Rundreise-Test sogar **umgangen**. Beide haben jetzt einen |
| ~~**Zwei Verlaufsstapel: einer oder zwei?**~~ | `UndoStack` (Zeichenfläche) und `TdUndo` (Text) teilen rund zwanzig Zeilen — zwei Listen, ein Deckel, ein Ereignis. **Bewusst getrennt gelassen** (§4.33, mit drei Gründen), aber genau die Sorte Frage, die zum Aufräumen gehört und nicht zum Bauen. ✅ **Entschieden am 2026-09-04 (Nutzer): (a) — getrennt lassen** (§5 Nr. 30, §4.98). **Und der Handgriff war nicht das Nein, sondern der Ort:** der Grund steht jetzt in **§7**, wo jemand nachsieht, der eine Doppelung sucht — in §4.33 stand er seit Phase 4, und die Frage ist trotzdem ein zweites Mal gestellt worden |
| ~~**Das Avalonia-Issue schreiben**~~ | ✅ **Geschrieben in §4.97**: `Docs/avalonia-issue-tote-tasten.md`, mit der Kette in vier Schritten, den drei Messungen samt Werkzeug, der `@im=none`-Gegenprobe und der Warnung, dass **`setxkbmap -query` unter XWayland lügt** — ausgerechnet die einzige Rückfrage in `#18596`. **⛔ Nicht abgesendet: das ist das Konto des Nutzers und ein öffentlicher Beitrag.** Die Datei sagt in ihrem Kopf, was vorher zu prüfen und nachher nachzuziehen ist |
| ~~**Das Fehlerprotokoll wächst unbegrenzt**~~ | ✅ **Erledigt am 2026-08-28** (§4.66): `Fehlerprotokoll` in Core mit **5 MB Obergrenze**, für beide Köpfe. Die Datei war auf **272 MB** — 48.962 mal derselbe Fehler, alle von **einem** Tag |
| ~~**`HANDOFF.md` raus**~~ | ✅ **Entschieden am 2026-08-28** (§5 Nr. 21) und in V2-93 **umgesetzt**: die Datei liegt unter `Docs/`, **kein History-Rewrite**. In ⑤ ist nur noch zu prüfen, dass sie dort geblieben ist |

> **Und danach der ganze Prüflauf, nicht nur die Tests:** beide Testprojekte, beide Köpfe mit
> **0 Warnungen**, die **vier mitgelieferten Dokumente in beiden Sprachen am laufenden
> Programm** (Dauerregel 1) und ein Durchgang mit einer **Kopie** der echten Daten
> (Dauerregel 4). Aufräumen ist die Sorte Änderung, die nichts kaputt machen soll — und genau
> deshalb prüft sie niemand nach.

##### Was in ⑤ anstand (§5 Nr. 24: alle vier) — ✅ **abgearbeitet in §4.101**

> **▶ Die vier Posten sind erledigt.** Was danach noch offen ist, steht am Ende dieses
> Abschnitts und ist **kein Arbeitsschritt, sondern ein Handgriff am GitHub-Konto**.

| | |
|---|---|
| ~~**READMEs überarbeiten**~~ ✅ | Beide Fassungen zusammen (Dauerregel 1): Screenshots **beider** Köpfe, die drei Installationswege, MIT-Text, Verweis auf `THIRD-PARTY-NOTICES.md`. **Und die bekannten Einschränkungen namentlich:** Rechtschreibprüfung fehlt unter Linux (§5 Nr. 22, **mit dem Hinweis, dass sie als erster Punkt nach M3 eingeplant ist**), zusammengesetzte Zeichen kommen unter Linux nicht an (§5 Nr. 11), keine Seitenzahlen im WPF-Editor (§5 Nr. 15) |
| ~~**GitHub Pages**~~ ✅ | Projektseite aus dem Repo, **zweisprachig**: Screenshots, Download, Kurzbeschreibung — sie ist die Seite, auf die ein Flathub-Eintrag verweisen kann, und die `metainfo.xml` zeigt mit ihren `<screenshots>` bereits dorthin. **⛔ Sie liegt in `site/` und nicht in `docs/`:** Dieses Repo hat schon `Docs/`, und unter Windows sind `docs` und `Docs` **dasselbe Verzeichnis**. Veröffentlicht wird über `.github/workflows/pages.yml` (Quelle: GitHub Actions). ⚠ **Der Schalter dafür ist eine Repo-Einstellung und noch nicht gesetzt** |
| ~~**Releases mit Artefakten**~~ ✅ *gebaut, ▶ nicht ausgelöst* | `.github/workflows/release.yml` baut an einem `v*`-Tag **Windows-Zip, AppImage und Tarball**, prüft vorher beide Testprojekte und legt das Release samt Text an. **⛔ Der Tag ist die Veröffentlichung und gehört dem Nutzer** — es gibt keinen Probelauf, der nichts hinausschickt, und **deshalb ist dieser Lauf nie gelaufen**. ⚠ Der AppImage-Zweig ist auf dem Laptop gemessen (§4.96–§4.100), **in einer CI nie**; der **Tarball** ist die Rückfallebene, falls er dort bricht |
| ~~**Repo-Beiwerk**~~ ✅ *bis auf zwei Einstellungen* | `CONTRIBUTING.md`, `SECURITY.md`, zwei Issue-Vorlagen und eine PR-Vorlage — **alle zweisprachig in je einer Datei**, weil sie niemand nebeneinander sieht und zwei Dateien deshalb auseinanderlaufen. `LICENSE` lag schon sichtbar. ⚠ **Beschreibung und Topics fehlen noch**: das sind Einstellungen und keine Dateien, und `gh` ist auf diesem Rechner nicht angemeldet |

> **⚠ Die Reihenfolge in ⑤ ist nicht beliebig:** Ein Release braucht ein gebautes Artefakt
> (also ③ **und** dessen Wiederholung nach ④), und eine Projektseite, die auf einen Download
> zeigt, den es noch nicht gibt, ist schlechter als keine Seite.

> **✅ Nachgetragen:** Die Auslieferung war eine Ergänzung zu `gonk-note-port-RM.MD` und nicht
> daraus abgeleitet — **am 2026-08-18 ist die Roadmap-Datei nachgezogen worden**, und **am
> 2026-08-28 ein zweites Mal** (die neue Ordnung ①–⑤, Phase 5.1, Version 1.0.0). **Beide
> Dateien sagen dasselbe.**

> **M1 ist ein gültiger Ausstiegspunkt.** Phase 4 ist die, an der Projekte sterben — dort
> strikt in der Reihenfolge Absätze/Zeichenformate → Seitenumbruch → Listen → Tabellen →
> Felder/TOC → Diagramme bauen, nach **jedem** Schritt Roundtrip-Test.

### Vorgemerkt: Flathub — der Umbau auf einen Quellbau

> **▶ Der volle Befund steht in §4.102, hier nur der Zuschnitt.** Er ist nachgelesen und
> nicht vermutet (docs.flathub.org, 2026-09-06).

**Es blockiert nichts.** AppImage und Windows-Zip decken beide Plattformen ab, sobald der Tag
steht; Flathub ist der bequemere Weg, nicht der einzige, und beide READMEs sagen das ehrlich.
*Ein Umbau, der die Auslieferung nicht aufhält, darf warten, bis er sauber gemacht werden
kann.*

**⛔ Der Blocker ist einer und er ist grundsätzlich:** Flathub baut **aus dem Quellcode und
ohne Netz**. Unser Manifest packt ein fertiges `dotnet publish`-Ergebnis ein (`type: dir`) —
für die Erprobung in Schritt ③ die richtige Wahl, für eine Einreichung nicht.

**Was der Umbau umfasst** (Windows-Arbeit, eine Runde):

| | |
|---|---|
| **SDK-Erweiterung statt Fertigpaket** | `org.freedesktop.Sdk.Extension.dotnet10`, `branch/25.08`, SDK **10.0.300 GA**. ⛔ **Der Manifestkopf behauptete, es gebe sie nicht** — berichtigt in §4.102 |
| **NuGet vorrätig legen** | `nuget-sources.json` über `flatpak-dotnet-generator.py`; 20 direkte Pakete, transitiv einige hundert |
| **Quelle auf den Tag pinnen** | Das Repo als `git`-Quelle auf `v1.0.0`; Icon, `.desktop` und `.metainfo.xml` kommen von dort statt als Dateien in den PR |
| **`finish-args` aufräumen** | `--talk-name=org.freedesktop.portal.Desktop` raus (Portale sind ohnehin erlaubt), `--socket=wayland` raus (Avalonia hat keinen Wayland-Rücken) |
| **`flathub.json`** | `{"only-arches": ["x86_64"]}` — ⚠ **gleich richtig**, eine später gestrichene Architektur bleibt sonst auf ihrer alten Fassung stehen |

**Was der Laptop danach tun muss, und nur er:** bauen und **zweimal** linten
(`flathub-build`, dann `flatpak-builder-lint` für Manifest **und** Repo). `flatpak-builder`
läuft nicht unter Windows, und **der Quellbau ist ein anderer als der, den er zweimal gemacht
hat.**

**Was der Nutzer tun muss, und nur er:** `flathub/flathub` forken, PR gegen den Zweig
**`new-pr`** (nicht `master`), im Review antworten, nach dem Merge die Einladung annehmen
(2FA, binnen einer Woche).

**⚠ Zwei Vorbedingungen, die schon auf der ⑤-Liste stehen:** die **Pages-Quelle** muss auf
„GitHub Actions" stehen (sonst zeigen die `<screenshots>` der `metainfo.xml` ins Leere —
heute **404**), und der **Tag** muss existieren.

**⚠ Die erste Frage, die der Umbau beantwortet:** Hier wird mit `10.0.400-preview` gebaut, die
Erweiterung liefert `10.0.300` GA. Es gibt **kein `global.json`**, das den Preview-Stand
festnagelt — *tragen sollte es also, gemessen ist es nicht.*

### ✅ Erledigt: eigene Farbschemata (Nutzerwunsch 2026-08-02, **gebaut am 2026-09-10**)

> **▶ DIESER PUNKT IST ZU — die Umsetzung steht in §4.107.** Was darunter folgt, ist der
> **Zuschnitt von 2026-08-02** und bleibt als Begründungsspeicher stehen: er ist Punkt für
> Punkt so gebaut worden, und keine seiner Annahmen musste korrigiert werden. **Die vier
> „vor der Umsetzung zu klären"-Fragen am Ende sind alle beantwortet** (1 am 2026-08-03,
> 2 und 3 und 4 am 2026-09-10) — die Antworten stehen in §5 und in §4.107.
>
> **Was heute gilt, in vier Zeilen:** Ein Design ist eine JSON-Datei mit bis zu zwanzig
> benannten Farben unter `<Datenordner>/Themes/*.json`. Was fehlt, kommt aus Hell bzw.
> Dunkel; `variant` ist Pflicht. Das Menü steht in **beiden** Köpfen unter
> **Ansicht → Design**, samt „Eigenes laden…", „Vorlage speichern…" und „Design-Ordner
> öffnen". **Auch der WPF-Kopf baut sein Wörterbuch jetzt aus der Farbtabelle in Core** —
> `Themes/Light.xaml` und `Dark.xaml` sind gelöscht.

**Gewünscht war:** eigene Themes anlegen und über **Ansicht → Design** laden.

**Machbar, und kleiner als es klingt.** Ein Theme ist heute nichts als **20 flache
Hex-Farben** — 15 `SolidColorBrush` plus 5 rohe `Color` (`src/GonkNote.Wpf/Themes/Light.xaml`).
Keine Verläufe, keine Struktur, keine Logik. Das ist eine Datendatei, kein Programm.

**Aber ausdrücklich nicht als XAML-Upload.** Eine `.xaml` zur Laufzeit einzulesen ginge unter
WPF über `XamlReader.Load` — und wäre aus drei Gründen die falsche Entscheidung:

- **NativeAOT.** `XamlReader` lebt von Reflection. Für den iPad-Kopf ist AOT Pflicht (§1);
  eine Theme-Funktion, die dort nicht läuft, wäre eine Funktion, die man später wieder
  ausbaut.
- **Portierbarkeit.** Avalonia hat kein `ResourceDictionary` im WPF-Sinn. Ein XAML-Theme
  wäre ein Windows-Theme und müsste für jeden Kopf neu erfunden werden.
- **Sicherheit.** XAML kann Typen erzeugen. Eine „Theme-Datei" aus dem Internet wäre damit
  ausführbarer Code — bei einer Datei, die Nutzer untereinander weitergeben, ist das der
  falsche Vertrag.

**Der tragfähige Zuschnitt** — er passt auf ein Muster, das die App schon hat:

- Ein Theme ist eine **JSON-Datei mit 20 benannten Farben**, gelesen über
  `System.Text.Json` mit Source-Generator (AOT-tauglich, wie in Phase 2 Schritt 3).
- Sie liegt in **`%APPDATA%\GonkNote\Themes\*.json`** — dieselbe Stelle und dieselbe Regel
  wie Sticker, Cover-Vorlagen und die eigenen Geodreieck-SVGs: die Datei des Nutzers
  gewinnt, die mitgelieferte ist der Rückfall. Über `IAppPaths.DataFolder` (steht seit
  Phase 2).
- Core hält die Farbtabelle und prüft sie; **jeder Kopf übersetzt sie in seine eigenen
  Pinsel.** Hell und Dunkel werden dabei zu zwei mitgelieferten Tabellen statt zu zwei
  fest verdrahteten Dictionaries.
- **`IThemeHost` (§4.7) ist die Naht, die dafür schon da ist.** Aus `Apply(AppTheme)` wird
  `Apply(ThemeDefinition)`; `AppTheme` bleibt als „hell oder dunkel"-Auskunft bestehen, denn
  `WbRenderer` und die Titelleiste brauchen sie weiter.
- „Hochladen" heißt: Datei wählen (`IFileDialog`), prüfen, in den Theme-Ordner **kopieren** —
  genau wie `WhiteboardView.Covers` es mit eigenen Cover-Vorlagen macht.

**Wann:** **frühestens nach Phase 3 (M1), nicht davor.** Nicht weil es schwer wäre, sondern
wegen der Reihenfolge — heute gäbe es eine WPF-Fassung, die Phase 3 sofort noch einmal bauen
müsste.

> **✅ Die eine Entscheidung, die jetzt fallen musste, ist gefallen** (2026-08-03, §4.9):
> die Avalonia-Farben kommen **aus einer Farbtabelle** (`Core/Theming/`) und nicht als
> zweites Paar fest verdrahteter Dateien. Der teure Nachbau ist damit vermieden — was noch
> fehlt, ist das **Laden** einer Datei, und das ist jetzt wirklich nur noch eine Zutat.

**Vor der Umsetzung zu klären:**

1. ~~**Reicht die Chrome, oder auch das Papier?**~~ **Entschieden am 2026-08-03: auch das
   Papier.** Die Tabelle in Core umfasst alle zwanzig Farben, `CanvasBg`, `PageBg`,
   `PageLine`, `PageGridDot` und `DefaultInk` inbegriffen. Ein Theme *kann* damit das
   Aussehen von Notizbüchern ändern — ob ein einzelnes es *tut*, ist danach eine reine
   Datenfrage. **Das heißt auch: es kann den Export verändern.** Wer eigene Themes
   ausliefert, sollte das im Blick behalten.
2. **Was passiert bei einer unvollständigen Datei?** Vorschlag: fehlende Schlüssel still aus
   Hell/Dunkel ergänzen, statt die Datei abzulehnen — dann genügt eine Datei mit drei Farben.
   **Der Mechanismus steht bereits**: `ThemeDefinition.Over(...)`, Wächter
   `Eine_unvollstaendige_Tabelle_wird_still_ergaenzt`. Offen ist nur, ob es so gewollt ist.
3. **Menü:** „Ansicht → Design wechseln" wird zu einem Untermenü (Hell / Dunkel / eigene /
   „Eigenes laden…"). Neue `Loc`-Schlüssel in **beiden** Tabellen (Dauerregel 1).
4. **Neu:** Ein geladenes Theme muss sagen, ob es **hell oder dunkel** ist
   (`ThemeDefinition.Variant`). Das ist keine Farbe, sondern eine Auskunft — `WbRenderer`
   und die Titelleiste unter Windows brauchen sie. In der JSON-Datei also ein eigenes Feld,
   und kein Ratespiel über die Helligkeit von `PageBg`.

**Nicht vergessen:** Das ist ein **neuer Wunsch**, keine Vorgabe aus
`gonk-note-port-RM.MD`. Wer die Roadmap-Datei auf dem Desktop pflegt, sollte ihn dort
nachtragen — sonst steht er nur hier.

### Aus V1 mitgeschleppt, weiterhin offen

Vollständig in `gonk-note\HANDOFF.md` §5. Die Punkte, die für die Portierung zählen:

- **Trägheit im Text-Editor bei großen Dokumenten** — erledigt sich mit Phase 4 vermutlich von
  selbst (eigene Engine kann virtualisieren, `FlowDocument` in `RichTextBox` nicht). **Keine
  Zeit mehr in die WPF-Fassung stecken.**
- **Rechtschreibung ohne Windows-Sprachpaket** — löst sich mit `ISpellChecker` +
  Hunspell (Phase 2/3) gleich mit.
- OCR-Endfluss, Formen-Stift und Touch haben nie Praxis-Rückmeldung auf echter Hardware
  bekommen.
- Cover-Kennzeichen, Verpixelung ab Seite 12, Geodreieck auf dunkler Seite, gedrehte Elemente,
  Wasserzeichen im DOCX-Export.

### Vor dem Öffentlich-Schalten des Repos — ✅ **abgearbeitet, das Repo ist öffentlich**

> **▶ Das war Schritt ⑤ von Phase 5.** **Der Nutzer hat das Repo am 2026-09-05 auf „public"
> geschaltet**, und die Liste darunter ist in derselben Runde Punkt für Punkt abgearbeitet
> worden (§4.101). *Die Reihenfolge — erst abarbeiten, dann umschalten — ist damit knapp
> eingehalten: umgeschaltet wurde zuerst, abgearbeitet unmittelbar danach und **bevor ein
> Release existiert**, das jemand herunterladen könnte.*
>
> **Was am Ende offen bleibt, sind drei Handgriffe am GitHub-Konto** und keine Arbeit an
> diesem Baum. Sie stehen unten als einzige nicht abgehakte Punkte.

- [x] ✅ **`HANDOFF.md` von der Startseite nehmen — erledigt in V2-93.** Nutzer-Entscheidung
      2026-08-28 (§5 Nr. 21): Die Datei liegt jetzt unter **`Docs/HANDOFF.md`** und wird
      **nicht** aus dem Baum genommen — sie muss zwischen den Rechnern mitwandern. Der
      Kommentar in `.gitignore` sagt, warum sie dort nicht ausgeschlossen ist.
- [x] ✅ **History-Rewrite: NEIN — entschieden am 2026-08-28** (Nutzer, §5 Nr. 21). Die Datei
      darf über `git log` und im Repo lesbar bleiben; sie soll nur keine der Hauptdateien auf
      der Startseite sein. **Das war die einzige Entscheidung in Phase 5, die nachträglich
      nicht mehr zu treffen gewesen wäre** — sie ist getroffen. *(Der Weg wäre gewesen:
      `git filter-repo --path HANDOFF.md --invert-paths`, danach `git reflog expire
      --expire=now --all` + `git gc --prune=now`, **und `git fsck` gegenprüfen** — in V1
      hingen nach einem Rewrite noch unerreichbare Commits herum. Er wird nicht gegangen.)*
- [x] ✅ **`Docs/HANDOFF.md` einmal ganz durchgesehen — sauber** (V2-125). Gesucht war, was
      die Kopfzeile seit jeher verbietet: **Pfade zu privaten Daten, Zugangsdaten, Inhalte
      aus den Schulunterlagen.** Gefunden ist **nichts davon**: kein `C:\Users\<Name>`, kein
      Token, kein Schlüsselmaterial; „Schulunterlagen" kommt ausschließlich als **Regel** vor
      und nie als Inhalt. Der Laptop-Pfad `/home/gonk/…` bleibt stehen — er nennt den
      Datenordner der App und deckt sich mit dem öffentlichen Kontonamen.
      *Diese Prüfung war eine Pflicht und keine Entscheidung: Ohne Rewrite ist die Datei in
      **jeder** Fassung öffentlich lesbar, auch in jeder früheren.*
- [x] ✅ **`git log --all --name-only` durchgesehen — sauber** (V2-125), und zwar die ganze
      Historie und nicht nur den aktuellen Baum. **633 Pfade, davon 124 nicht mehr im
      Baum — und alle 124 sind Quelldateien aus dem Umzug von Phase 0**
      (`Views/…` → `src/GonkNote.Wpf/Views/…`). **Keine Datenbank, kein Blob, kein PDF, kein
      DOCX, kein Schlüssel.** *Der Fall aus V1 — derselbe Inhalt lag ein zweites Mal in einem
      längst gelöschten Ordner — tritt hier nicht auf.*
- [x] ✅ **Lizenzlage geprüft** (V2-125). Jede eingecheckte Grafik hat eine geklärte Herkunft,
      und die Tabelle in `THIRD-PARTY-NOTICES.md` führt sie jetzt **vollständig**: **62
      Cover** *(die Checkliste sagte bis hierher „53" — nachgezählt sind es 62)*, die zwei
      Geodreieck-SVGs, `GonkNote.ico` und `gonk-note-Icon.png` — **und neu die sieben
      Bildschirmfotos unter `site/bilder/`**, deren Inhalt aus der erfundenen Demo-Datenbank
      stammt. **Sticker liefert das Projekt bewusst keine.** Die fünf Schriftfamilien stehen
      unter der SIL OFL 1.1 und liegen unverändert mit ihrer `OFL.txt` daneben.
      *„Selbst abgeändert" ist keine Lizenz, **NC ist mit MIT unvereinbar** — beides trifft
      hier auf nichts zu.*
- [x] ✅ **README-Paar und Erste-Schritte-Paar auf Stand** (Dauerregel 1). §4.99 hatte sie auf
      den Stand von M2 gezogen, samt den **vier** Einschränkungen, die wirklich gelten
      (§5 Nr. 22, 11, 15 und das Lineal) und dem Hinweis, dass die Rechtschreibprüfung als
      **erster Punkt nach M3** eingeplant ist. V2-125 hat die **drei Installationswege**, die
      **Bildschirmfotos** und den Verweis auf die Releases ergänzt — **am laufenden Programm
      gegengeprüft**.
- [x] ✅ **Version auf 1.0.0** (§5 Nr. 23), an fünf Stellen: `Directory.Build.props`,
      `About.Version` in **beiden** Sprachtabellen, die `<release>`-Zeile der
      `metainfo.xml`, die vier Dokumente — und der Tag, sobald er geschoben ist. **Alle vier
      Über-Dialoge** (zwei Köpfe × zwei Sprachen) sind am laufenden Programm gesehen.
      **⛔ Genau dabei fiel der Fund der Runde auf:** Die längere Zeile wurde
      **abgeschnitten statt umgebrochen**; behoben ist der **Behälter**, nicht der Text
      (§4.101).
- [x] ✅ **Der Klon-Befehl in beiden Erste-Schritte-Fassungen läuft** — die GitHub-API meldet
      für das Repo `"private": false`. **Nachgesehen, nicht angenommen** (§4.12 hatte genau
      das verlangt).
- [x] ✅ **GitHub Pages, Releases mit Artefakten und das Repo-Beiwerk angelegt** (§5 Nr. 24,
      §4.101): `site/` samt `pages.yml`, `release.yml` an einem `v*`-Tag, `CONTRIBUTING.md`,
      `SECURITY.md`, Issue- und PR-Vorlagen.

**▶ Und das ist der ganze Rest von Phase 5 — drei Handgriffe, alle am GitHub-Konto:**

- [x] ✅ **Die CI ist wieder grün** (2026-09-05, §4.101). Sie war es vier Tage lang nicht,
      und **`release.yml` lässt dieselben Tests laufen** — bis hierher hätte der Tag kein
      Release erzeugt, sondern einen roten Lauf. Ursache war **ein** Wächter: eine
      Datumsspalte wurde mit der Kultur des Rechners gelesen.
- [ ] **Den Tag schieben:** `git tag -a v1.0.0 -m "Gonk Note 1.0.0"` und
      `git push origin v1.0.0`. **Das ist die Veröffentlichung** — `release.yml` baut
      daraufhin Windows-Zip, AppImage und Tarball und legt das Release samt Text an.
      **⚠ Dieser Lauf ist nie gelaufen**, denn er lässt sich nur durch einen Tag auslösen und
      ein Tag schickt hinaus. Geht der AppImage-Zweig schief, steht der **Tarball**
      trotzdem bereit. **Mit dem Release ist M3 erreicht.**
- [ ] **Pages einschalten:** Repo-Einstellungen → Pages → Quelle **GitHub Actions** (nicht
      „Deploy from a branch"). Ohne das läuft `pages.yml` und veröffentlicht trotzdem nichts.
      ⚠ Danach nachsehen, ob `https://gonkstupid.github.io/GonkNote/` steht — **die
      `<screenshots>` der `metainfo.xml` zeigen dorthin**, und ein Flathub-Eintrag mit toten
      Bild-URLs wird abgelehnt.
- [ ] **Beschreibung und Topics** des Repos setzen. Beides ist eine Einstellung und keine
      Datei; `gh` ist auf dem Windows-Rechner **nicht angemeldet** (`HTTP 401: Bad
      credentials`), und ein Token einzurichten ist eine Entscheidung des Nutzers.

**Nicht auf dieser Liste, aber in derselben Lage** — es wartet auf eine Hand und nicht auf
eine Runde: der **Flathub-Eintrag** (nicht eingereicht), das **Avalonia-Issue** (geschrieben,
nicht abgesendet — §4.97), sowie **Portal-Dateidialog** und **Stift** in der Flatpak-Sandbox
(§4.100).

---
