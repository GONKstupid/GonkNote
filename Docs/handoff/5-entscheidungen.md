[← Index: HANDOFF.md](../HANDOFF.md)

## 5. Entscheidungen

**Getroffen, alle umgesetzt:**

| Frage | Entscheidung |
|---|---|
| **Wann geht das Repo öffentlich — vor oder nach dem iPad-Kopf?** | **Vorher** (Nutzer, 2026-08-18). Die Veröffentlichung ist **vorgezogen**, iPadOS **nachgestellt**: Phase 4.5 → **Phase 5 Aufräumen + öffentlich gehen + Flatpak/AppImage** → **Phase 6 iPadOS**. Der Linux-Port ist das Ziel des Projekts, der iPad-Kopf ein Zusatz — eine fertige Linux-Fassung soll nicht auf einen zweiten Port warten. **iPadOS bleibt Teil desselben Projekts und Repos**, nichts ist gestrichen. **M1/M2 unverändert**, dahinter umnummeriert: **M3 = veröffentlicht, M4 = TestFlight**. Begründung und Folgen im Kasten in §6; **`gonk-note-port-RM.MD` ist nachgezogen** |
| Ziel-Framework | **`net10.0`** statt `net9.0` — LTS bis Nov 2028 (§4.3) |
| SkiaSharp | **3.119.4 + Svg.Skia 5.1.1**, vorgezogen vor Phase 1 (§4.4) |
| Remote | **<https://github.com/GONKstupid/GonkNote>**, privat, vom Nutzer angelegt. Branch `main`, alles gepusht |
| Stift | Die App soll mit **jedem** Stylus laufen — nicht nur mit einem Modell (§1) |
| Linux-Rechner | Zweiter Laptop mit **CachyOS** steht bereit; Stift ist ein Lenovo Precision Pen 2 (§5a) |
| Über-Dialog-Text | **`About.Version` über `Loc`**, deutsch „Portierung, Phase 2" / englisch „Port, phase 2" (§4.5). Erledigt 2026-07-30 |
| Welche Phase der Über-Dialog nennt | **Die, an der gearbeitet wird — nicht die, die fertig ist** (§4.5). Am 2026-08-10 auf „Portierung, Phase 4" / „Port, phase 4" gesetzt; „Phase 3" war seit dem 2026-08-03 falsch, und der Dialog zeigte einen Stand von vor sieben Runden. So stand die Zeile in Phase 2 und 3 auch schon da, während die Phase noch lief. Gegengeprüft in allen vier Kombinationen — dabei fiel auf, dass jeder Kopf seine eigene Kopie von Core trägt (§7). Entschieden 2026-08-10 (Nutzer) |
| Ob ein achtes Diagramm (Fläche) dazukommt | **Vorerst nein** (§4.25). Die sieben aus `TdChartKind` decken den heutigen Editor und den DOCX-Weg vollständig ab; ein Flächendiagramm hat noch nie jemand anlegen können. Die im HANDOFF genannte „Fläche" war ein Schreibfehler an der Stelle von „Punkt+Linie" und ist korrigiert. Entschieden 2026-08-10 (Nutzer) |
| Markdown-Export und Hyperlinks | **Ziel bleibt erhalten** (`[Text](URL)`), §7 „Markdown-Export". Erledigt 2026-07-30 |
| Kopie der echten Daten | **Ohne Nachfragen erlaubt**, die echte DB bleibt unangetastet — Dauerregel 4 in der Kopfzeile, Befehle in §8. Entschieden 2026-07-30 |
| Wie die ViewModels an den Kopf kommen | **Ein Bündel `IPlatformServices` im Konstruktor**, kein Service-Locator und keine zwölf Argumente (§4.7). Entschieden 2026-07-31 |
| Farben und Bilder in den ViewModels | **Als Hex-Text und Bytes**, Pinsel und Bitmaps baut der Kopf über Konverter (§4.7). Entschieden 2026-07-31 |
| Name der SQLite-Datei | **`gonknote.sqlite`** — Stamm bleibt `gonknote`, sonst wandert der Blob-Ordner (§4.8). Entschieden 2026-08-02 |
| Wann migriert wird | **Automatisch und still beim ersten Start** (§4.8). Entschieden 2026-08-02 |
| Was mit `gonknote.db` passiert | **Unangetastet liegen lassen** — nie beschrieben, nie umbenannt, nie gelöscht (§4.8). Entschieden 2026-08-02 |
| SQLite-Schema | **Ein JSON-Dokument je Zeile**, kein relationales Schema für Seiten und Elemente (§4.8). Entschieden 2026-08-02 |
| Wo Phase 3 entwickelt wird | **Unter Windows**, nicht auf dem Laptop — `Avalonia.Desktop` läuft dort auch, und nur so lassen sich beide Köpfe nebeneinander vergleichen und die Werkzeuge in `tools\` benutzen (§5b). Entschieden 2026-08-03 |
| Avalonia-Fassung | **12.1.1** — genau die, mit der der Stylus-Prototyp gemessen hat (§5a). Damit ist die offene Frage aus §4.3 beantwortet: Avalonia 12 trägt `net10.0`. Entschieden 2026-08-03 |
| Woher die Avalonia-Farben kommen | **Aus einer Farbtabelle in Core, alle zwanzig — das gezeichnete Blatt inbegriffen** (§4.9). Damit ist auch die erste der drei Fragen aus §6 beantwortet. Entschieden 2026-08-03 |
| Versionsnummer | **0.3.0**, weil der Persistenz-Umbau das Dateiformat betrifft; `About.Version` in beiden Tabellen auf Phase 3 nachgezogen. Entschieden 2026-08-03 |
| Wie der Renderer an seinen `SKCanvas` kommt | **Avalonias eigenen ausleihen** (`ISkiaSharpApiLeaseFeature`) statt eine Zwischenfläche zu rastern — `Avalonia.Skia` hängt an derselben SkiaSharp-Fassung wie Core, ein offizielles `SkiaSharp.Views.Avalonia` gibt es nicht (§4.10). Entschieden 2026-08-03 |
| Testdaten auf dem Laptop | **Keine Kopie der echten Datenbank.** Selbst angelegte Notizbücher sind für den Eingabepfad die bessere Prüfung, und die Schulunterlagen bleiben auf dem Windows-Rechner. Dauerregel 4 erlaubt die Kopie weiterhin — sie wurde hier nur nicht gebraucht. Entschieden 2026-08-03 |
| Linux-Fernsteuer-Werkzeuge | **Ja, minimal** — `schau.sh`, `klick.sh` und ein eigenes `zeiger` über X11/XTEST, ohne Fremdpaket (§4.10). Der Stift bleibt dabei Handarbeit. Entschieden 2026-08-03 |
| Neigung im Dateiformat | **Ja, und zwar auf dem Laptop** (§4.11). Zwei Felder an `WbPoint`, bedingt geschrieben; nur der Bleistift wertet sie aus. Bestandsdateien und alle zwanzig Pixelhashes bleiben unverändert — deshalb war keine Windows-Gegenprobe nötig. Entschieden 2026-08-03 |
| **Wird M1 ausgerufen?** | **Ja** — mit Brocken 6 und 7 ist der M1-Satz buchstäblich erfüllt: Notizbuch und Whiteboard laufen unter Linux, Textdokumente sind ausgegraut. Import/Export steht nicht im M1-Satz und hängt an §4.1 (Phase 4). **Die vier mitgelieferten Dokumente sind im selben Zug auf den Linux-Kopf erweitert worden** (§4.12). Entschieden 2026-08-03 |
| Wo der Markdown-Zerleger steht | **In Core** (`Core/Text/Markdown.cs`), nicht ein zweites Mal im Kopf — er zeichnet kein Pixel (§3, Faustregel), und zwei Fassungen derselben Grammatik driften auseinander (§4.12). ~~Der WPF-Kopf behält vorerst `MarkdownFlow`~~ — **seit 2026-08-04 ruft er `Markdown.Parse`** (§4.13). Entschieden 2026-08-03 |
| Die zwei Schulden aus Phase 3 | **Eingelöst am 2026-08-04** (§4.13), beide unter Windows. `WbHit` und `Markdown.Parse` im WPF-Kopf; die Endlosschleife kam ohne eigenen Handgriff mit |
| Wie „pixelgleich" belegt wird | **Zwei Läufe, ein Bildvergleich** — derselbe Prüflauf gegen dieselbe Master-Datenbank, einmal mit altem und einmal mit neuem Stand, danach Pixel für Pixel verglichen (§4.13). Ein Testlauf allein prüft nur, was er kennt. **Muster für jede weitere Zusammenlegung.** Entschieden 2026-08-04 |
| Wie eine verbundene Zelle abgelegt wird | **`Restart` + `Continue` je Zeile**, kein „RowSpan = 3" (§4.18). Jede Zeile behält damit ihre volle Zellenzahl, und daran hängt, dass Spaltenzählung und Rahmen stimmen. DOCX macht es so, und beim ersten unregelmäßigen Raster zeigt sich, warum. Entschieden 2026-08-04 |
| Wo die Spaltenbreiten stehen | **An der Tabelle** (`ColumnWidthsCm`), nicht an den Zellen (§4.18). Eine Spaltenbreite gilt für die ganze Spalte; je Zelle geführt stünde derselbe Wert je Zeile noch einmal, und ein Dokument, in dem Zeile 3 eine andere Vorstellung von Spalte 2 hat als Zeile 1, ist gar nicht darstellbar. Entschieden 2026-08-04 |
| Wie ein Listenpunkt aussieht | **Ein Absatz mit einer Angabe** (`TdParagraph.List`), kein eigener Blocktyp (§4.17). Ausschlaggebend war die Bearbeitung: Eingabe, Ebenenwechsel und Herausnehmen bleiben Absatzänderungen statt Baumumbauten, und `TdLayout` braucht keinen zweiten Umbruchpfad. `MdList` macht es für Markdown anders, und das ist dort richtig — Markdown wird gelesen, nie bearbeitet. Entschieden 2026-08-04 |
| Wo die Listennummer steht | **Nirgends — sie wird gerechnet** (`TdListNumbering`). Sie hängt davon ab, was vor einem Punkt steht; gespeichert wäre sie bei jeder Einfügung im ganzen Dokument nachzuziehen, und jede vergessene Stelle wäre eine Liste, die nach dem Löschen der 2 bei 3 weiterzählt. DOCX macht es aus demselben Grund so. Entschieden 2026-08-04 |
| Wo die Seiteneinrichtung steht | **In `TdSection`**, nicht mehr an `TextDoc` (§4.15). Das kauft mehrere Abschnitte je Dokument und einen DOCX-Import, der `sectPr` vollständig liest; es kostet eine additive Migration nach dem Muster §4.8. Bis die läuft, stehen beide nebeneinander — **verschiedene Formate für verschiedene Dateien**, nicht zwei Rechnungen für dieselbe Sache. Entschieden 2026-08-04 (Nutzer) |
| Wann die Bestandsdokumente übernommen werden | **Zuletzt, nach Schritt 6** (§6). RTF und XamlPackage tragen Tabellen, Bilder und Diagramme; das Modell kann die erst ab Schritt 4 bzw. 6. Eine Übernahme davor wäre **stiller Datenverlust** — genau das, wovor §4.8 warnt. Bis dahin bleibt `Rtf` das führende Feld und das Modell wird über DOCX geprüft, nicht über Nutzerdaten. Entschieden 2026-08-04 (Nutzer) |
| Was ein Feld speichert | **Seine Art, nicht seinen Wert** (§4.20). Seitenzahl, Seitenanzahl und Inhaltsverzeichnis hängen von der Umgebung ab und nicht vom Feld; gespeichert wären sie nach der nächsten Änderung falsch, und zwar still — eine veraltete Seitenzahl sieht aus wie eine Seitenzahl. Dasselbe Muster wie bei der Listennummer (§4.17). Entschieden 2026-08-05 |
| Woher Datum und Titel kommen | **Von außen, über `TdFieldContext`** (§4.20). **Core fragt die Uhr nicht selbst** — dieselbe Begründung wie bei `ITdTextMeasure` (§4.16): ein `DateTime.Now` in der Rechnung machte jeden Wächter davon abhängig, wann er läuft. Aus demselben Grund steht das Datumsmuster fest im Code statt in der Kultur des Rechners. Entschieden 2026-08-05 |
| Wie oft der Umbruch läuft | **So oft, bis sich nichts mehr ändert — höchstens fünfmal** (§4.20). Die Seitenanzahl steht erst am Ende fest, und ein Inhaltsverzeichnis verschiebt durch seine Länge die Überschriften, deren Seiten es nennt. Word rechnet ebenso mehrfach; die Obergrenze steht da, weil eine Schleife auf einen Fixpunkt, den es nicht gibt, keinen Fehler meldet, sondern gar nichts (§4.16). Entschieden 2026-08-05 |
| Wie ein Verweis abgelegt wird | **Als Klammer um Stücke, mit dem Ziel als Zeichenkette** (§4.20). Ein `Uri` vereinheitlicht und macht aus `kapitel-2.md` einen `file:///`-Pfad — genau der Fehler aus §7 („Markdown-Export"). Ein Ziel mit `#` wird in DOCX ein Anker und keine Beziehung. Entschieden 2026-08-05 |
| Ob das Feldergebnis mitgeschrieben wird | **Nein** (§4.20). Ein mitgeschriebenes Inhaltsverzeichnis käme beim Lesen als Absätze zurück, und das Dokument wüchse mit **jedem** Speichern um ein ganzes Verzeichnis — dieselbe Falle wie beim Trennabsatz (§4.18), nur mit dreißig Zeilen statt einer. Word füllt das Feld beim Öffnen (`UpdateFieldsOnOpen`). Beim **Lesen** wird das Ergebnis eines *bekannten* Feldes verworfen, das eines unbekannten behalten — eine Rechenvorschrift zu verlieren ist verschmerzbar, Text zu verlieren nicht. Entschieden 2026-08-05 |
| Ob Kopf-/Fußzeile Absätze werden | **Nein, sie bleiben Text mit Platzhaltern** (§4.20). Die Begründung aus §4.15 gilt unverändert; eingelöst ist nur das Versprechen, dass **alle vier** Platzhalter echte Felder werden — `{DATUM}` und `{TITEL}` waren bis Schritt 5 wörtlicher Text. Entschieden 2026-08-05 |
| Was ein Diagramm speichert | **Seine Zahlen, nicht ein Bild davon** (§4.21). Der heutige Editor rendert es beim Einfügen zu einer Bitmap — danach lässt es sich nie wieder ändern, und beim Export geht ein Pixelbild hinaus, wo Word ein Diagramm erwartet. Legende, Farbvergabe und fehlende Beschriftungen werden **gerechnet**, zum dritten Mal nach §4.17 und §4.20. Entschieden 2026-08-05 |
| Wie ein Diagramm nach DOCX kommt | **Als echtes `c:chart` mit literalen Daten**, ohne eingebettete Arbeitsmappe (§4.21). Die Mappe wären dieselben Zahlen ein zweites Mal (§4.10). Der Preis, benannt: Words Knopf „Daten bearbeiten" findet keine Mappe und bietet an, eine anzulegen; angezeigt, gedruckt und zurückgelesen wird einwandfrei. Entschieden 2026-08-05 |
| Wo die Bytes eines Bildes liegen | **Im Blob-Speicher, hinter der Naht `ITdImages`** — im Dokument steht ein Verweis (§4.21). **Gemessen und nicht gemeint:** als sie in V1 noch im Dokument lagen, wurde ein Dokument mit drei Fotos (2 MB) zu 16,8 MB und riss die 16-MB-Grenze von LiteDB. Fehlt die Naht, wirft der Export; fehlt ein einzelner Blob, fällt nur dieses Bild weg. Entschieden 2026-08-05 |
| Ob Bild und Diagramm Blöcke sind | **Nein, Stücke** (`TdInline`, §4.21). In DOCX steht eine Zeichnung immer in einem Lauf; ein bildbreites Foto ist ein Absatz, der nichts als dieses Bild enthält. Die reservierten Blocknamen „image" und „chart" bleiben frei — wie „list" in §4.17. Entschieden 2026-08-05 |
| Wie die Übernahme abläuft | **Still — aber ein Fehler wird gespiegelt** (§4.22). Was gelingt, gelingt wortlos: ein Hinweis, den man bei jedem Dokument wegklickt, wird nach dem dritten Mal nicht mehr gelesen. Was misslingt, wird benannt und **im Dokument vermerkt** (`MigrationIssue`) — anders als bei der Datenbank (§4.8) kann hier etwas verlorengehen. `Rtf` bleibt unangetastet, der Versuch wird beim nächsten Öffnen wiederholt. Entschieden 2026-08-05 (Nutzer) |
| Aufräumen vor der Veröffentlichung | **Ja, als eigener Schritt in Phase 6** (§6). Erst aufräumen, **dann** noch einmal vollständig prüfen, dann veröffentlichen — wer nach dem Aufräumen nicht mehr prüft, veröffentlicht einen Stand, den nie jemand gesehen hat. Entschieden 2026-08-05 (Nutzer) |
| Wann `TextDoc.Model` geschrieben wird | **Bei jedem Speichern, neben `Rtf`** (§4.23). Sonst exportierte ein Export aus dem Modell den Stand der einmaligen Übernahme statt dessen, was auf dem Schirm steht. Die Nebenwirkung ist erwünscht: Der Umwandler läuft ab jetzt ständig, und ein Fehler darin fällt auf, **solange `Rtf` noch führt** — also solange er niemandem schaden kann. Entschieden 2026-08-09 |
| Ob der Dokumenttitel eine Überschrift ist | **Ja im Markdown (`#`), nein im Inhaltsverzeichnis** — dafür gibt es `TdParaFormat.ExcludeFromToc` (§4.23). Eine exportierte `.md` ohne oberste Überschrift wäre ärmer, ein Verzeichnis mit dem Dokumenttitel als erstem Eintrag falsch. **Word trennt genauso** (`Title`, `TOC Heading`: Rang aus der Vorlage, Verzeichnis aus `w:outlineLvl`). Entschieden 2026-08-09 (Nutzer) |
| Wo das Absatz-Zeichenformat in DOCX steht | **Im `pPr/rPr` *und* unter jedem Lauf** (§4.23). Das `pPr/rPr` allein gilt in Word nur für die Absatzmarke — Läufe erben aus der Formatvorlage. Beim Lesen nimmt `TdCharFormat.Ohne` die Dopplung wieder heraus, sonst trüge jeder Lauf eine vollständige Formatkopie (§4.14). Entschieden 2026-08-09 |
| Wie groß die Zeichner-Runde wird | **Text, Tabellen und Bilder zuerst — Diagramme danach** (§4.24). Die sieben Diagrammarten samt Legende, Achsen und Beschriftung hätten die Runde und den Diff verdoppelt. Ein `TdChart` bekommt bis dahin einen **benannten Platzhalterkasten** und keine Leerstelle: „hier fehlt etwas" statt „hier war nie etwas" (§7). Entschieden 2026-08-09 (Nutzer) |
| Wo das Diagramm gerechnet wird | **In `Core/Text/TdChartLayout.cs`, nicht im Zeichner** (§4.25). Achsenteilung, Farbvergabe, Legende und jeder Ort stehen als Zahl in Zentimetern; `TdRenderer` ruft nur noch Skia auf. Der Grund ist Prüfbarkeit und nicht Ordnungsliebe: an jeder Achse steht Schrift, und Schrift darf nicht gehasht werden (§4.6) — als Rechnung sind es 43 Wächter ohne ein einziges Pixel. Zum vierten Mal dasselbe Muster nach §4.17, §4.20 und §4.21. Entschieden 2026-08-10 |
| Ob die Diagramm-Rechnung misst | **Nein, sie schätzt** (§4.25). `TdChartLayout` kennt `ITdTextMeasure` nicht; die Breite einer Achsenbeschriftung wird über Zeichenzahl × Grad × 0,55 geschätzt. Gemessen hinge die Lage der Zeichenfläche an der Schriftausstattung des Rechners, und dasselbe Dokument bekäme unter Linux ein anders geteiltes Diagramm — dieselbe Falle wie in §4.16. Ob der Text hineinpasst, entscheidet der Zeichner, der messen kann. Entschieden 2026-08-10 |
| Wo die Werteachse anfängt | **Immer bei null oder darunter, nie beim kleinsten Wert** (§4.25). Eine Säule, die bei 98 anfängt und bei 100 endet, sieht doppelt so hoch aus wie eine bis 99 — die bekannteste Art, mit einem richtigen Diagramm etwas Falsches zu behaupten. Negative Werte hängen unter der Nulllinie, statt am Boden abgeschnitten zu werden; Ober- und Untergrenze sind Vielfache der Teilung, damit die Null auf einer Stufe liegt. Entschieden 2026-08-10 |
| Wie eine Reihe ohne Namen in der Legende heißt | **Mit ihrer laufenden Nummer, nicht mit „Reihe 2"** (§4.25). Dieselbe Antwort wie bei `TdChart.Kategorie` (§4.21): Ein deutsches Wort hinge an `Loc.Current` und stünde beim nächsten Öffnen auf Englisch — dasselbe Dokument, zwei Bilder. Aus demselben Grund steht die Achsenzahl invariant („1.5"), wie das Datumsmuster in §4.20. Entschieden 2026-08-10 |
| Ob die App ihre Schriften mitliefert | **Ja, fünf Familien für fünf Rollen** (§4.26). „Segoe UI" gibt es unter Linux nicht und unter iPadOS auch nicht; ohne mitgelieferte Schriften sähe dasselbe Dokument auf drei Plattformen verschieden aus. **Die Plattform-Weiche in `AvaloniaFontProvider` ist damit weg** — es gibt keine Windows- und keine Linux-Antwort mehr, sondern eine. Preis: rund 6 MB in der Exe, benannt. Entschieden 2026-08-10 (Nutzer) |
| Wo Schriften aufgelöst werden | **Nur in `WbFonts`** (§4.26). Vorher fragten drei Stellen unabhängig `SKTypeface.FromFamilyName` — das sieht nur Systemschriften, eine mitgelieferte Schrift hätte den Dokumentzeichner nie erreicht. Reihenfolge: mitgeliefert → System → Rückfallkette. „Segoe UI" bleibt in der Kette, damit ein Bestandsdokument seine gespeicherte Schrift behält (§4.14). Dasselbe Muster wie §4.13. Entschieden 2026-08-10 |
| Ob die Dokument-Grundschrift mitwechselt | **Ja** (§4.26) — `TdCharFormat.Standard` steht auf „Source Sans 3", die Whiteboard-Vorgaben auf Geist und Space Grotesk. **Das ist Datenformat**, betrifft aber nur *neue* Dokumente: der gespeicherte Wert gewinnt, es gibt keinen Migrationsschritt. Benannter Preis: Word ohne die Schrift blendet um — DOCX speichert den Namen, nicht die Schrift. Entschieden 2026-08-10 (Nutzer) |
| Wie groß das Ribbon im Linux-Kopf wird | **Nur die zwei Reiter, die Inhalt haben** (§4.28). „Einfügen", „Verweise" und „Tabelle" gibt es drüben, weil man dort schreiben kann; hier wären es drei leere Flächen. Ein halbes Feature ist schlechter als ein fehlendes (§4.12) — und **was fehlt, steht als Satz da** („Nur Ansicht") und nicht als ausgegrauter Knopf. Entschieden 2026-08-11 |
| Ob der Reiter „Layout" die Seiteneinrichtung auch **stellt** | **Nein, er liest sie ab** (§4.28). Sie zu ändern hieße das Dokument zu ändern, und dafür fehlt der Schreibweg. Angezeigt wird sie trotzdem: sie erklärt den Umbruch, den man daneben sieht. Entschieden 2026-08-11 |
| Wo die Weiche über die Dateiendung steht | **In Core (`TdExport`), nicht in den Köpfen** (§4.28). Seit §4.27 stehen alle vier Exportwege in Core; was blieb, war ein `switch`. Ihn zweimal zu schreiben wäre die Falle aus §4.13 — zwei Fassungen derselben Entscheidung, die auseinanderdriften, sobald ein Format dazukommt. Entschieden 2026-08-11 |
| Was ein unter Linux importiertes Dokument in `Rtf` bekommt | **Nichts — und der WPF-Editor fällt dafür auf `Model` zurück** (§4.28). Ein `XamlPackage` gibt es nur unter Windows; ohne den Rückfall zeigte der WPF-Editor ein **leeres Blatt**, und das sieht nach gelöschtem Inhalt aus. **Die Reihenfolge kehrt sich dabei nicht um:** `Rtf` führt weiter, solange dort etwas steht — gelesen wird aus `Model` nur, wenn es sonst nichts zu lesen gäbe. Entschieden 2026-08-11 |
| Ob der Linux-Kopf Markdown importiert | **Nein, nur DOCX** (§4.28). `TdDocx.Lesen` steht in Core, der Markdown-*Import* geht drüben weiter über ein `FlowDocument`. Ein `.md`-Eintrag in der Formatliste führte in einen Dateidialog, hinter dem eine Ausnahme wartet — **ein Format anzubieten, das man nicht lesen kann, ist schlimmer, als es nicht anzubieten.** Entschieden 2026-08-11 |
| **Was bei einer unvollständigen Theme-Datei geschieht** | **Still ergänzen** (§4.107). Fehlende Farben kommen aus Hell bzw. Dunkel — eine Datei mit drei Farben ist ein gültiges Design; der Mechanismus (`ThemeDefinition.Over`) stand seit Phase 3. **Gemeldet wird nur, was falsch dasteht**: kaputtes JSON, fehlende `variant`, unlesbarer Farbwert. *„Nicht dagewesen" ist eine Aussage des Nutzers, ein Tippfehler ist keine.* Entschieden 2026-09-10 (Nutzer) |
| **Ob der WPF-Kopf die eigenen Designs sofort mitbekommt** | **Ja, sofort** (§4.107) — **gegen die Empfehlung dieses Dokuments**, die auf „nur Linux, WPF vormerken" lautete, weil es keinen laufenden Windows-Rechner gibt (§0). **Sie gilt.** `WpfThemeHost` baut sein Wörterbuch jetzt aus derselben Tabelle, `Themes/Light.xaml` und `Dark.xaml` sind gelöscht. **Der Preis ist benannt:** der Kopf ist gebaut, aber nicht gesehen worden. Entschieden 2026-09-10 (Nutzer) |
| **Wie jemand zu seiner ersten Theme-Datei kommt** | **Über eine exportierte Vorlage** (§4.107): „Ansicht → Design → Vorlage speichern…" schreibt das aktive Design vollständig heraus. Zwanzig Farbnamen aus einer Anleitung abzutippen macht niemand — und eine Vorlage, die nur die Hälfte zeigt, verschweigt genau das, wonach jemand sucht. Entschieden 2026-09-10 (Nutzer) |
| Namen der WPF-Hilfsmethoden | **Bleiben stehen** — `HitElement`, `HitTestElement`, `SelectByLasso`, `ComputeSelectionBounds` sind Einzeiler, die an `WbHit` weiterreichen. Elf Aufrufstellen in fünf Partials umzubenennen hätte den Diff verdreifacht, ohne am Ergebnis etwas zu ändern; wegkommen sollte die zweite **Rechnung**, nicht die zweite Bezeichnung (§4.13). Entschieden 2026-08-04 |

**Noch offen:**

1. ✅ **Beantwortet am 2026-08-16: ein zweiter Stift läuft, und zwar mit anderer Technik.**
   Auf dem Laptop mit **F9** gegengeprüft (§4.35, „Was der Laptop gefunden hat"): Der zweite
   Stift ist ein **MPP**-Gerät — nicht dieselbe Technik wie der eingebaute EMR-Wacom —, und
   **Druck kommt an**. Kein Rückfall, echte Werte.

   **Damit ist die Anforderung „läuft mit jedem Stylus" (§1) zum ersten Mal an zwei
   verschiedenen Stifttechniken belegt** und der einzige Punkt mit echtem Restrisiko
   eingelöst. Was er vorher war, steht in §5a „Offen"; er stand seit §4.10 offen und war mit
   der Zeichenfläche akut geworden.
2. ✅ **Beantwortet am 2026-08-11: das Schreiben im Linux-Kopf zuerst — Weg (a).**
   **Nutzer-Entscheidung**, gegen die Empfehlung dieses Dokuments (die auf Phase 4.5 lautete,
   Begründung steht weiter unten in §6). **Sie gilt.**

   Was das heißt: Cursor, Auswahl, Eingabe und Undo gegen `TdDocument` — der Rest dessen, was
   die Roadmap unter Phase 4 „Editing" nennt, und der Weg, an dessen Ende `Rtf` als führendes
   Feld abgelöst wird. **Phase 4.5 rückt dahinter**, bleibt aber vollständig vorgemerkt und
   trägt weiterhin M2. Der Arbeitsplan steht in **§6, „Als Nächstes: das Schreiben"**.

3. **Darf ein PDF rund 200 KB je benutzter Schriftfamilie wiegen?** Auf dem Laptop gemessen
   (§4.27, „Was der Laptop gefunden hat"): **Skia bettet die ganze TTF-Datei ein, nicht einen
   Auszug** — die `/Length1`-Werte im PDF sind byteweise die Dateigrößen unter `Assets/Fonts/`.
   Eine A5-Seite mit sechs Absätzen in fünf Schnitten wiegt **762 KB**.

   **Warum das trotzdem kein Rückschritt ist:** die Kosten fallen **einmal** an und nicht je
   Seite. Dasselbe Dokument in einer Familie: 1 Seite 207 KB, 35 Seiten 260 KB — **rund 1,5 KB
   je Seite**. Der alte Weg legte je Seite ein volles Rasterbild ab; ab wenigen Seiten gewinnt
   der neue Weg deutlich, und der Abstand wächst.

   **Die Frage ist die kurze Datei.** Ein einseitiger Aushang wiegt heute mehr als ein
   dreißigseitiges Skript wiegen müsste. Drei Antworten sind denkbar:
   (a) **so lassen** — Papier ist billig, und ein PDF, dessen Schrift überall gleich aussieht,
   ist der Grund für §4.26;
   (b) einen **Auszug** selbst bilden, bevor die Schrift an Skia geht — das ist echte Arbeit
   (Glyphen sammeln, Tabellen neu schreiben) und eine eigene Runde;
   (c) an der Naht **umschalten**: die mitgelieferte Schrift nur einbetten, wenn sie auch
   benutzt wird — das tut Skia bereits, es sind hier wirklich fünf benutzte Schnitte.

   **Empfehlung: (a), vorerst.** Es ist kein Fehler, nichts hängt daran, und (b) lohnt erst,
   wenn jemand über die Dateigröße stolpert. **Nicht linuxspezifisch** — Skia macht das unter
   Windows genauso, dort ist es nur nie gemessen worden. Vermerkt 2026-08-11.

4. **Eigene Farbschemata** (Nutzerwunsch 2026-08-02) — vorgemerkt in §6. Die wichtigste der
   drei Fragen ist mit §4.9 beantwortet (die Tabelle umfasst auch das Papier); offen bleiben
   die beiden kleineren: Verhalten bei einer unvollständigen Datei und der Menüaufbau.

5. ✅ **Erledigt am 2026-08-11 (V2-40): `About.Version` ist in beiden Köpfen gegengeprüft.**
   Mit Punkt 2 wurde sie fällig und steht seit dem 2026-08-11 auf **„Portierung, Phase 4 —
   das Schreiben"** / **„Port, phase 4 — editing"**, beide Tabellen zusammen geändert.

   Im Linux-Kopf am 2026-08-11 geprüft, **im WPF-Kopf am selben Tag nachgezogen** — beide
   Sprachen, am laufenden Programm (Hilfe → Über Gonk Note), mit einer Kopie der echten
   Datenbank. Der Dialog liest die Zeile über `Loc.T("About.Version", …)`, in beiden Köpfen
   dieselbe Stelle; **die Sorge aus §4.25 hat sich hier nicht bestätigt**, aber sie bleibt
   berechtigt (§7, „Der Kopf trägt seine eigene Kopie von Core") — geprüft wurde, nicht
   angenommen.

6. ✅ **Erledigt am 2026-08-28 (§4.67) — und die Ursachenvermutung, die hier stand, ist dabei
   widerlegt worden.** Der Blocksatz verteilte den Restplatz auf **jede Stückgrenze**, und ein
   Stück entsteht nicht nur am Wort, sondern auch am **Formatwechsel**. Behoben in
   `TdLayout` (neu: `Luecken`), **5 Wächter** gegen die feste Messung, **am laufenden Programm
   mit dem alten Stand daneben** gegengeprüft: dieselbe Zeile, sechs Lücken weniger.

   **⛔ Zwei Angaben aus dem alten Wortlaut gelten nicht mehr** und stehen unten nur noch als
   Chronik: (1) Der Maßstab hat **nichts** damit zu tun — Skia skaliert linear, gemessen auf
   0,00 px, und Stück für Stück gezeichnet ist pixelgleich mit einem Zug. (2) **„Im PDF ist
   sie richtig" stimmt nicht** — Anzeige und PDF gehen durch denselben Umbruch und denselben
   Zeichner; der Fehler war in beiden. Verglichen worden waren Zeilen- und Seitenumbruch.

   **⚠ Eine Hälfte des Fundes ist offen geblieben:** die notierte Schreibweise
   „und**Farbiges**" — eine **ganz fehlende** Lücke — ließ sich nicht reproduzieren; die alte
   Rechnung macht Lücken zu breit, nie zu schmal. Erst wieder ein Befund, wenn es jemand
   erneut sieht.

   ~~Der Wortzwischenraum an einer Stückgrenze sitzt in der Anzeige falsch.~~ Auf dem Laptop
   gefunden (§4.28, „Was der Laptop gefunden hat"): „…, Unterstrichenes und Farbiges." steht
   auf dem Schirm als **„Unterstrichenes  undFarbiges ."** — die Lücke fehlt vor dem farbigen
   Stück und steht dahinter. **Im PDF derselben Datei ist sie richtig**, Zeilen- und
   Seitenumbruch stimmen zwischen beiden überein.

   **Vermutlich nicht linuxspezifisch** (derselbe Maßstab greift im Avalonia-Kopf unter
   Windows), deshalb nach §5d dort nicht angefasst. **Es ist keine Entscheidung, sondern
   Arbeit** — es steht hier, damit es nicht untergeht, bis eine Windows-Runde es aufgreift.
   Es fällt nur bei Text mit mehreren Zeichenformaten auf. Vermerkt 2026-08-11.

7. ✅ **Beantwortet am 2026-08-16 (V2-47): auch der Portal-Dialog trägt.** Die Wayland-Hälfte,
   die unten als „was offen bleibt" steht, ist von Hand am Gerät geprüft (§4.35, „Was der
   Laptop gefunden hat"): `Datei → Dokument importieren…` mit einer DOCX **bringt das Dokument
   wirklich in den Kopf**, und im Speichern-Dialog steht **PDF oben vorgewählt** — dieselben
   zwei Ja wie unter Windows. **Damit ist der Punkt ganz zu**, in beiden Dialog-Fassungen.

   **Fernsteuern lässt er sich weiterhin nicht** (§4.28); jede weitere Prüfung ist wieder
   Handarbeit. Der Verlauf, der dahin geführt hat, steht darunter.

   ⏳ *(Stand bis 2026-08-16 — unter Windows beantwortet, unter Wayland offen.)*
   Er öffnet sich und blockiert richtig (§4.28), aber die zwei Fragen aus §5d —
   **kommt ein Pfad zurück**, und **steht das vorgewählte Format oben** — konnten auf dem
   Laptop nicht beantwortet werden: unter GNOME-Wayland ist es der **Portal**-Dialog, also
   ein natives Wayland-Fenster, und das ist mit `tools/linux` weder zu fotografieren noch zu
   bedienen (nur `Escape` kommt an).

   **Am 2026-08-11 unter Windows durchgespielt** (V2-40, §4.29) — dort läuft
   `Avalonia.Desktop` gegen den Win32-Dialog, nicht gegen das Portal. Beide Antworten sind
   **ja**: Der Export-Dialog kommt mit vorgewähltem Dateityp **und** vorbelegtem Dateinamen
   (dem Dokumenttitel), der Import-Dialog mit vorgewähltem `Word-Dokument (*.docx)`. Ein
   PDF und ein DOCX wurden geschrieben, das DOCX wieder eingelesen — Text und Umlaute kamen
   unverändert an. Damit ist der Weg **vom gewählten Pfad ins Modell** gesehen.

   **Was offen bleibt, ist genau die Wayland-Hälfte:** ob der Portal-Dialog dieselben Werte
   liefert. Das ist eine andere Implementierung derselben Avalonia-Schnittstelle, also keine
   Formsache. **Zwei Minuten Nutzer am Gerät**, wenn er ohnehin am Laptop sitzt.

8. ✅ **Erledigt am 2026-08-12 (V2-43, §4.32): ein neues Textdokument bekommt ein leeres
   `TdDocument`.** Der Fund (V2-40, §4.29): „Datei → Neues Textdokument" erzeugte ein Dokument
   ohne `Model` **und** ohne `Rtf`, und `TextDocView.Laden()` — das nur `Model == null` kennt —
   zeigte dafür „Dieses Dokument stammt aus der Windows-Fassung"; Export blieb ausgegraut.

   **Gelöst an der Wurzel und nicht in den Köpfen:** `DatabaseService.GetText` legt ein noch
   nicht gespeichertes Dokument mit einem leeren Modell an. Wo etwas steht, gibt es nichts zu
   übernehmen — ein dritter Zustand durch alle Köpfe war damit nicht nötig. `Rtf` bleibt leer,
   „wer voll ist, führt" ist unangetastet. **In beiden Köpfen am laufenden Programm geprüft**,
   an einer Kopie der echten Datenbank.

9. ✅✅ **ERLEDIGT am 2026-08-22 (§4.48, Schritt 7) — an der Wurzel und nicht mehr durch
   eine Warnung.** Der WPF-Editor liest **und schreibt** seit dem das Modell; `Rtf` wird nie
   mehr überschrieben, `Migrate` läuft nur noch für die **einmalige** Übernahme, und der
   Warnstreifen im Linux-Kopf ist **entfallen** — es gibt nichts mehr, wovor zu warnen wäre.
   **`TdFuehrung.AltformatFuehrt` ist gelöscht**, nicht auf `false` gesetzt.

   **Am laufenden Programm belegt und danach in der Datei nachgemessen:** Linux schreibt →
   Windows bearbeitet und speichert → Linux liest **beides**. In der Datenbank steht danach
   `Rtf` mit **Länge 0** (vom WPF-Editor nicht angefasst, obwohl er gespeichert hat) und
   `Model` mit dem Windows-Text. **Der Weg, der vorher Datenverlust war, trägt jetzt.**

   **Der Wortlaut der Vertagung von 2026-08-16 steht darunter** — sie war (b) „warnen, nicht
   sperren", und die Lösung ist immer (a) gewesen.

   ~~Entschieden am 2026-08-16: (b) warnen, nicht sperren — und am selben Tag gebaut~~
   (§4.36, V2-48). Der Linux-Kopf zeigt bei gefülltem `Rtf` einen Streifen über dem Blatt:
   „Windows-Fassung führt — was hier geschrieben wird, geht verloren, sobald dieses Dokument in
   der Windows-Fassung gespeichert wird." Drei Übersetzungsschlüssel je Sprache, keine
   Formatänderung. Die Regel dahinter heißt jetzt `TdFuehrung.AltformatFuehrt` und steht an
   **einer** Stelle statt an dreien.

   **Der Punkt ist damit entschärft, aber nicht erledigt.** Die Gefahr besteht weiter — sie ist
   nur nicht mehr still. **Ganz zu ist er erst mit Schritt 7** (§6), und deshalb bleibt er hier
   stehen. **Und die Warnung selbst ist noch nicht am laufenden Programm gesehen worden**
   (§4.36, „Was diese Runde nicht belegen konnte").

   Der Befund, der dazu geführt hat, steht darunter — vom 2026-08-16 (§4.35), denn seit
   Schritt 5 lässt sich dort wirklich schreiben.

   **Der gemessene Ablauf:** Ein Dokument, das schon einmal im WPF-Editor beschrieben wurde,
   hat ein gefülltes `Rtf` — und das führt (§5, „wer voll ist, führt"). Wer es danach im
   Linux-Kopf bearbeitet, schreibt in `Model`. Der WPF-Editor zeigt beim nächsten Öffnen
   **den alten Stand**, und beim nächsten Speichern dort schreibt `WpfDocumentIo.Migrate` das
   `Model` **bedingungslos** aus `Rtf` neu. **Die Linux-Arbeit ist dann still weg.** Das ist
   nicht die „zweite Wahrheit" aus §6, sondern Datenverlust ohne Warnung — **und es betrifft
   jedes Bestandsdokument.**

   **Vier Antworten sind denkbar:**
   (a) **Schritt 7 vorziehen** — der WPF-Editor liest aus dem Modell, `Rtf` verliert die
   Führung. Das löst es an der Wurzel, ist aber der Schritt, bei dem am meisten schiefgehen
   kann (§6), und er ist nicht klein.
   (b) **Warnen statt verhindern** — der Linux-Kopf sagt im Ribbon, dass dieses Dokument noch
   die Windows-Fassung führt. Zwei Übersetzungsschlüssel, keine Formatänderung, kein
   Datenverlust ohne Vorwarnung. **Empfehlung, wenn (a) nicht sofort kommt.**
   (c) **Das Schreiben sperren**, solange `Rtf` steht. Sicher — und macht Schritt 5 für echte
   Dokumente unbenutzbar.
   (d) **`Migrate` bremsen:** nicht übernehmen, wenn `Model` schon gefüllt ist. Klingt billig,
   ist aber falsch herum: dann liefe der Umwandler nie mehr, und §4.23 hat ihn genau deshalb
   bei jedem Speichern laufen lassen.

   **Nichts davon war in der Runde gebaut worden, die es gefunden hat** (V2-46), weil jede
   Antwort eine Entscheidung des Nutzers ist. Bis Schritt 7 gilt weiterhin: **im Linux-Kopf
   gefahrlos schreiben lässt sich nur, was der WPF-Editor nie beschrieben hat** — seit V2-48
   steht das aber auf dem Schirm.

10. ✅ **Erledigt am 2026-08-18 (§4.41, V2-54): die Eingabe-Naht steht.** Entschieden am
    2026-08-16 war **(a) `TextInputMethodClient` umsetzen, aber nach Schritt 6** — und genau so
    ist es gelaufen. `TextDocView` meldet sich über `Views/TextDocView.Eingabemethode.cs` als
    Eingabeziel an und beantwortet, wo die Marke steht, was der umgebende Text ist und was
    davon ausgewählt ist; gerechnet wird das in `Core/Text/TdEingabe.cs` (16 Wächter).

    **Der Befund, der dazu geführt hat** (Laptop, 2026-08-16, §4.35): Ohne Hardware-Tastatur
    ist im Linux-Kopf nicht zu schreiben — GNOMEs Bildschirmtastatur erscheint nicht, wenn die
    Textfläche den Fokus hat, und von Hand hervorgeholt kommt trotzdem nichts an. Beides
    gemessen. Die Ursache lag im Kopf und nicht im System: Es gab **kein
    `TextInputMethodClient`**.

    **✅ Der Laptop hat am 2026-08-18 gemessen, und der Nutzer hat den Rest von Hand geprüft**
    (§4.41, „Was der Laptop gefunden hat", V2-55). **Der Anlass des Punktes ist eingelöst:**
    In V2-47 war die Bildschirmtastatur *unsichtbar **und** taub* — **taub ist sie nicht
    mehr.** Von Hand hervorgeholt **schreibt sie ins Dokument**; genau dafür war die Naht
    gebaut, und dieser Teil trägt.

    **⏳ Was offen bleibt, ist das Aufklappen — und das behebt kein Kopfcode.**
    `TopLevel.InputPane` ist unter **`Avalonia.X11` `null`**: Der Rücken hat gar keine
    Eingabefläche, `RaiseInputPaneActivationRequested` läuft ins Leere, **egal welcher
    Zeigertyp es auslöst**. Am laufenden Programm gegengeprüft (dieselbe Tastatur klappt bei
    `gnome-text-editor` auf und verschwindet, sobald GonkNote den Fokus hat) und vom Nutzer am
    Gerät bestätigt (mit Stift und Finger tippt sich nichts auf).

    **Damit ist der Punkt gebaut, gesehen und in seiner Wirkung halb eingelöst.** Die
    verbleibende Frage geht an den **Rücken**, nicht an den Kopf, und ist keine Runde Arbeit:
    warten, bis Avalonia unter Linux eine `IInputPane` hat, oder **Linux mit „von Hand
    hervorholen" führen** und das in den Erste-Schritte-Text schreiben.

    **Für den iPadOS-Kopf ist die Naht die Voraussetzung** — dort bringt `Avalonia.iOS` eine
    `IInputPane` mit, und dann greift dieselbe Rechnung ohne Zutun. **Was V2-54 gebaut hat,
    ist also nicht umsonst, sondern unter Linux nur halb sichtbar.**

    **Sie hat aber etwas gekostet, das vorher lief: siehe Punkt 11.**

10a. ✅ **Erledigt am 2026-08-18 (§4.43): das Zusammensetzen ist gebaut.**
    `SupportsPreedit` meldet **`true`**, der unfertige Text steht als **Ansichtszustand** im
    Kopf und wird an der Marke gemalt — nicht ins Modell geschrieben. Damit ist dieser Punkt
    keine Auslassung mehr. *(Der Wortlaut von vorher bleibt darunter stehen, weil er die
    Begründung trägt, warum es überhaupt teuer wurde.)*
    ⚠ **Nicht mehr nur benannt, sondern teuer: das Zusammensetzen (Preedit).**
    `SupportsPreedit` meldet `false` (§4.41). Die Annahme dabei war, die Plattform zeige
    unfertigen Text in ihrem eigenen Fenster und liefere ihn fertig als `TextInput` nach —
    **unter Windows/TSF stimmt das, unter X11/IBus nicht.** Der Laptop hat es am 2026-08-18
    gemessen; **siehe Punkt 11**, dort steht der volle Befund. Für lateinische Schrift war es
    als „kostet nichts" verbucht — es kostet die toten Tasten.

11. ✅ **Entschieden am 2026-08-28 (Nutzer): beides — an Avalonia melden UND als benannten
    Mangel stehen lassen.** Der Weg „im Kopf umgehen“ ist ausdrücklich **nicht** gewählt.

    **Die Begründung ist der Ort des Fehlers.** Er sitzt in `Avalonia.X11`
    (`HandleKeyEvent` bestimmt den Keysym über `LookupKey(keycode)`, und bei `keycode = 0`
    fällt genau das Zeichen heraus, das im Ereignis steckt — §4.44). Ihn zu umgehen hieße,
    **unterhalb des Frameworks eigenen X11-Zugriff zu machen**: kein Wächter kann das
    absichern, und es wäre bei **jedem** Avalonia-Update neu zu prüfen. *Ein Fehler, den
    man am Framework vorbei behebt, wandert mit jeder Fassung wieder heran.*

    **Was daraus an Arbeit folgt — beides gehört zu Schritt ④ von Phase 5** (§6):
    - **Ein neues Issue bei AvaloniaUI/Avalonia** mit dem gemessenen Befund. `#18596` ist
      **exakt dieses Symptom und ohne Fix geschlossen** — die Meldung dort nannte nur das
      Symptom, **dieser Befund ist neu**: die Kette bis `XmbLookupString`, der Gegenbeweis
      mit `XMODIFIERS=@im=none`, der `dbus-monitor`-Vergleich gegen `gnome-text-editor`.
      **Mit hinein gehören die zwei Nebenbefunde derselben Naht**: der Keycode-Versatz um
      **8** und das fehlende **`LockMask`**-Bit.
    - **In beiden READMEs** (Dauerregel 1) unter den bekannten Einschränkungen: unter Linux
      kommen **zusammengesetzte Zeichen** (`^`+`e`, `´`+`a`) nicht an; **Umlaute und alles
      Übrige laufen**, ein Absatz von 427 Zeichen kommt exakt an.

    **Der volle Befund steht darunter und bleibt stehen** — er ist die Vorlage für das Issue.

    ⚠ ~~**Tote Tasten kommen seit V2-54 nicht mehr an — und das ist eine Regression.**~~
    Auf dem Laptop gemessen (§4.41, „Was der Laptop gefunden hat", 2026-08-18, V2-55):
    `^`+`e` ergibt **nichts** statt `ê`, `´`+`a` **nichts** statt `á`. Am 2026-08-16 stand
    genau das noch als gemessen und grün im HANDOFF (§4.35, V2-47).

    **Es ist keine Anzeigefrage — das Zeichen kommt gar nicht im Dokument an.** Der
    Zeichenzähler zählt es nicht mit: `Hallo` + `^e` + `´a` ergibt **5** statt 7. Kein
    Ersatzzeichen, kein `^e`, keine Fehlermeldung. **Umlaute sind nicht betroffen**
    (`Hallo äöüß ÄÖÜ` → exakt 14) — sie sind einzelne Keysyms und werden nicht zusammengesetzt.

    **Die Ursache ist eingekreist, mit drei Gegenproben:** `gnome-text-editor` bekommt
    dieselbe Tastenfolge vollständig (`ZZZêá`) — also weder Werkzeug noch Plattform; der
    Stand `c4532fe` **vor** V2-54 liefert `Halloêá`, `a25ef09` liefert `Hallo` — also V2-54;
    und dieselbe Binärdatei mit leerem `XMODIFIERS` liefert wieder `Halloêá` — also **IBus**.

    **Der Mechanismus:** Vor V2-54 war die Textfläche kein Eingabeziel, X11 lieferte das
    fertige Zeichen direkt. Seit der Anmeldung setzt **IBus** zusammen und reicht das
    Ergebnis als **Preedit** heraus; `SupportsPreedit => false` bedient diesen Weg nicht, und
    das Zeichen fällt still weg.

    > **⚠ Wichtig für die Abwägung, und es hat sich am 2026-08-18 geändert:** Die Anmeldung
    > ist **nicht** folgenlos geblieben — **sie hat die Bildschirmtastatur schreibfähig
    > gemacht** (Punkt 10: von Hand hervorgeholt kommt ihr Text jetzt an, in V2-47 nicht).
    > **Sie zurückzunehmen ist damit nicht mehr kostenlos.**

    **Drei Antworten waren denkbar — ✅ am 2026-08-18 sind zwei davon ausgeschieden** (§4.42,
    V2-56, am ausgelieferten Rücken nachgelesen):
    (a) **Preedit bauen** — `SupportsPreedit => true`. ✅ **Am 2026-08-18 gebaut** (§4.43,
    V2-56, **20 Wächter**, 823 Tests, unter Windows gegengeprüft). Der Einwand „berührt
    `TdDocument`" hat sich aufgelöst: **unfertiger Text ist Ansichtszustand** und wird als
    Auflage an der Marke gemalt — `TdDocument` wird nie angefasst, §4.32 greift nicht.
    ⏳ **Ob es wirkt, ist noch nicht gemessen** — das kann nur der Laptop (§5d).
    (b) ~~**Das Fertige annehmen, ohne das Unfertige zu zeigen**~~ — ⛔ **fällt aus, weil es
    nichts zu schließen gibt.** `OnCommitText` reicht den `commit` **ohne jede Abfrage von
    `SupportsPreedit`** durch (§4.42). Die vermutete Lücke gibt es nicht. **Der Hebel liegt
    woanders:** `SupportsPreedit` bestimmt das **Fähigkeitswort an IBus** (`CapPreeditText`)
    und damit, wohin IBus das Zusammensetzen überhaupt schickt.
    (c) ~~**Die Anmeldung unter Linux zurücknehmen**~~ — **fällt aus.** Sie stellte zwar die
    toten Tasten wieder her, machte aber die Bildschirmtastatur **wieder taub** und nähme
    damit den einzigen Gewinn zurück, den V2-54 unter Linux tatsächlich gebracht hat. **Es
    wäre ein Tausch, kein Fix.**
    **(d) Warten fällt auch aus:** **12.1.1 ist die neueste veröffentlichte Fassung**
    (2026-08-18 gegen nuget.org geprüft) — es gibt nichts, worauf sich warten ließe.

    **Damit galt die Regel, die dieser Punkt selbst aufgestellt hat: „wenn (b) nicht trägt,
    ist (a) fällig."** ✅ **(a) ist gebaut** (§4.43).

    ⛔ **Und am 2026-08-18 hat der Laptop gemessen: es wirkt nicht** (§4.43, „Was der Laptop
    gefunden hat", V2-59; 789/789 grün). `^`+`e` ergibt weiterhin **nichts**, der Zähler
    steht auf **5** statt 7 — **zahlengleich mit V2-55**. Umlaute unverändert **14**, ein
    Absatz von **427 Zeichen** kommt **exakt** an. **Die Erwartung aus §4.43 ist damit
    widerlegt, und zwar an der Stelle, an der sie es sein sollte.**

    **Der `dbus-monitor` verschiebt die Ursache, statt sie nur zu verneinen** — mitgelesen auf
    dem **eigenen Bus des IBus-Daemons**, nicht auf dem Sitzungsbus:
    - **`CommitText`: 0 mal** bei uns, **2 mal** bei `gnome-text-editor` — **derselbe Daemon,
      dieselbe Engine, dieselbe Sekunde.** Es liegt also **nicht** an IBus.
    - ✅ **Das Fähigkeitswort kommt an:** `SetCapabilities uint32 9` = `CapPreeditText` +
      `CapFocus`. **Punkt 2 aus §4.42 ist damit gemessen und bestätigt** — (a) hat getan, was
      es tun sollte.
    - ⚠ **Der Fehler sitzt eine Station davor:** Für die tote Taste und den Buchstaben danach
      schickt der Kopf **gar keinen Tastendruck** an IBus, nur das Loslassen — und dazwischen
      **einen Aufruf mit `keysym = 0` und `keycode = 0`**, auf den IBus mit **`true`**
      („behandelt") antwortet. **Genau danach verwirft Avalonia laut §4.42 das rohe Ereignis
      samt Text.** Der Nachbar schickt für dieselben zwei Tasten vier saubere Aufrufe und
      bekommt `UpdatePreeditText "^"` und `CommitText "ê"`.
    - **Nebenbei:** die Keycodes stehen um **8** daneben (X11 statt evdev). Als Ursache
      scheidet das aus — die Engine entscheidet am Keysym —, **aber es steht in derselben
      Naht.**

    **Was daraus folgt, und was ausdrücklich nicht:** **(a) bleibt richtig und wird nicht
    zurückgebaut** — es hat das Fähigkeitswort gerade gerückt, den Windows-Weg nicht
    beschädigt, und der iPadOS-Kopf braucht die Vorschau ohnehin. **Es war nur nicht
    hinreichend.** ▶ **Der nächste Griff gehört nach Windows und ist benannt:** nachsehen,
    **wer den Aufruf mit `keysym = 0` absetzt** — eine der vier Anschlussstellen aus §4.41
    oder `Avalonia.X11` selbst; **am zerlegten Rücken zu lesen, wie in §4.42.**

    **Bis dahin gilt der Stand als benannter Mangel:** tote Tasten kommen im Linux-Kopf nicht
    an, Umlaute und alles Übrige laufen.

    **Nicht auf dem Laptop behoben** — `SupportsPreedit` steht in gemeinsamem Kopfcode und war
    in §4.41 ausdrücklich eine Entscheidung, nicht ein Versehen (§5d: hier wird nur behoben,
    was es nur hier gibt). Vermerkt 2026-08-18.

    **▶ Stand nach der Windows-Runde 2026-08-19 (V2-61, §4.44):** Der Tastenweg ist am
    zerlegten Rücken abgelaufen, und die Auftragsfrage ist beantwortet: **Unser Kopf ist es
    nicht** — die vier Anschlussstellen setzen keine Tastenereignisse ab, und auch die
    Warteschlange verändert weder Keysym noch Keycode. Der `0/0/0`-Aufruf wird von
    **Avalonias eigenem dbus-Client** abgesetzt (der einzige Rufer von `ProcessKeyEvent`),
    aber er wird von einem **Phantom-`KeyPress` mit `keycode = 0` aus dem X-Strom**
    gespeist, und der echte Druck der toten Taste verschwindet zwischen `XNextEvent` und dem
    ersten Avalonia-Code — die einzige Weiche dort ist `XFilterEvent`, die nach der
    Startlogik des Rückens allerdings gar nicht filtern dürfte (das XIC wird unter IBus nie
    fokussiert, §4.44). Der
    **Keycode-Versatz von 8** ist bestätigt (Avalonia reicht den X11-Keycode unverändert —
    ein Avalonia-Fehler wie der `CapSurroundingText`-Fund aus §4.42, **nicht** die Ursache;
    nicht geradegebogen). **Upstream belegt:** AvaloniaUI/Avalonia#18596 ist unser Symptom
    und wurde **ohne Fix geschlossen**; ibus/ibus#546 betrifft dieselbe Sorte Defekt in der
    XIM-Brücke. **Was mit dem Fund geschieht (Umgehung im Kopf, Meldung an Avalonia, stehen
    lassen), entscheidet der Nutzer** — bis dahin ist nichts zu patchen. Die letzte offene
    Frage (warum der Druck verschwindet, wer das Phantom einspeist) klären **zwei Messungen
    in §5d**; danach ist der Weg frei für Schritt 7.

    **▶ ✅ Stand nach der Laptop-Runde 2026-08-19 (V2-62, §4.44 „Was der Laptop gefunden
    hat"): die Ursache ist gemessen, und sie liegt eine Station anders als vermutet.**
    789/789 grün, kein Produktivcode angefasst, von Hand getippt.
    - ⛔ **Die Leitspur aus §4.44 ist widerlegt:** Der Druck der toten Taste **erreicht den
      X11-Client** — `xtrace` zeigt `KeyPress keycode 49` **3 mal** und `keycode 26`
      **5 mal**, jeweils mit Loslassen.
    - ⛔ **Und es gibt kein Phantom aus dem X-Strom:** ein `KeyPress` mit `keycode 0` steht
      **nirgends auf der Leitung**, weder bei GonkNote noch bei `xev` — damit fallen sowohl
      XWayland als auch ein fremder `XSendEvent`-Client aus.
    - ✅ **Gemessen ist die zweite Zeile der Auftragstabelle: das Verschwinden sitzt im
      Prozess.** `XFilterEvent` **filtert sehr wohl** (in `xev` mit `True` belegt) — auch das
      widerlegt eine Herleitung aus §4.44.
    - ✅ **Das `keycode = 0` ist kein Defekt, sondern der Bote:** libX11 verschluckt die
      beteiligten Drucke und legt das **fertige Zeichen** als Ereignis mit `keycode = 0`
      nach; `XmbLookupString` liefert dort `(c3 aa) "ê"`. **Mit `XMODIFIERS=@im=none`
      gegengeprüft** — also in genau der Einstellung, mit der Avalonia fährt: identisches
      Bild, **4 mal `ecircumflex`**. **Der Zusammensetzer ist Xlibs lokale Eingabemethode**,
      nicht `ibus-x11` und nicht IBus.
    - ⚠ **Der Fehler des Kopfes ist damit benannt und kleiner als gedacht:** Avalonia lässt
      libX11 filtern, holt das Gefilterte aber nie ab — `HandleKeyEvent` bestimmt den Keysym
      über `LookupKey(keycode)`, und bei `keycode = 0` fällt genau das Zeichen heraus, das im
      Ereignis steckt. **Den Weg, auf dem `xev` sein `ê` abholt (`XmbLookupString`), geht der
      Kopf nie.**
    - **Nebenprodukt, das zwei alte Fragen mit schließt:** Umlaute sind heil, weil `ä` ein
      einzelnes Keysym ist und `XFilterEvent` dort `False` gibt; und vor V2-54 lief es, weil
      der Kopf ohne Eingabeziel das rohe Ereignis selbst auswertete und IBus gar nicht
      fragte.

    **▶ Damit ist der Punkt vollständig aufgeklärt und wartet nur noch auf eine
    Entscheidung** (Nutzer): **im Kopf umgehen** (das `keycode = 0`-Ereignis erkennen und den
    Text über die Eingabemethode abholen — klein, liegt aber in `Avalonia.X11` und ginge von
    unserer Seite nur als eigener X11-Zugriff am Kopf vorbei) **oder melden**
    (AvaloniaUI/Avalonia#18596 ist geschlossen, **dieser Befund ist aber neu** — die
    Meldung dort nannte nur das Symptom) **oder als benannten Mangel stehen lassen**.
    **Nichts davon blockiert Schritt 7.**

    **Es ist keine Kleinigkeit und deshalb ausgelassen:** Ihn im Blatt anzuzeigen hieße
    entweder, ihn ins Modell zu schreiben und wieder herauszunehmen (der Griff, vor dem §4.32
    warnt), oder ihn über den Text daneben zu malen. **Keine Entscheidung, die jetzt ansteht**
    — sie wird fällig, wenn jemand die App in einer Schrift benutzen will, die zusammengesetzt
    wird.

12. ✅ **Erledigt am 2026-08-22 (§4.47).** Entschieden am 2026-08-21 (Nutzer): **(b) das
    Modell geht direkt in den `RichTextBox`**, als eigene Runde vor Schritt 7 — und genau so
    ist es gelaufen. `TextEditorView.AusModell` hat den Umweg über das `XamlPackage` verloren,
    die Übernahme steht als `TdZuFlow.InhaltUebernehmen` neben `Umwandeln`. **5 Wächter,
    840 Tests**, Führung unverändert.

    **Die Auflage „erst messen, dann bauen" hat den Entwurf umgeworfen, bevor eine Zeile davon
    stand:** WPF **kopiert** ein `Tag` beim Teilen eines Absatzes **und** eines Laufs auf beide
    Hälften — ein Träger dort wäre nach einem Tastendruck doppelt vorhanden, **schlimmer als
    die Lücke, die er schließen sollte**. Übrig bleibt der **`InlineUIContainer`**: unteilbar,
    und derselbe Ort, an dem `DocumentImages` seinen Blob-Verweis seit jeher führt.

    **✅ Und ein Fund, der nicht gesucht war:** Das Paket schiebt die Schrift des Dokuments als
    **örtlichen Wert** auf jeden Absatz — derselbe Fehler wie §4.45, an anderer Stelle. Mit
    Schritt 7 wäre er in jedes Dokument gewandert; der direkte Weg räumt ihn mit weg.

    ⏳ **Die Träger selbst sind noch nicht gebaut, und das ist Absicht:** Sie überleben jetzt
    das *Laden*, aber `FlushToModel` schreibt beim Speichern weiter ein Paket nach `Rtf`. Sie
    gehören zu **Schritt 7**, wo das Paket ganz aus dem Speicherweg fällt.

    **Die Herleitung, die zu dieser Entscheidung geführt hat** (§4.45, §4.47):

    **Was daraus folgt, in der Reihenfolge:** erst der Ladeweg (eigene Runde), dann Schritt 7.
    Der Grund für die Trennung ist derselbe wie bei §4.45: Wer die Führung umdreht und
    gleichzeitig den Ladeweg umbaut, hat bei einem Fehler zwei Verdächtige.
    **Zu messen und nicht anzunehmen ist dabei**, was beim Tippen mit einem Träger geschieht —
    ein Absatz, den WPF beim Drücken der Eingabetaste teilt, und Läufe, die es beim Tippen
    zusammenzieht: Erbt der neue Teil den Träger des alten? **Ein geerbter Träger wäre eine
    verdoppelte Gliederungsebene**, und das fiele erst im Inhaltsverzeichnis auf.

    **Die Herleitung, die zu dieser Antwort geführt hat** (§4.45, V2-63).

    **Der Befund, aus dem sie folgt:** Ein Träger, an dem etwas hängen könnte, das ein
    `FlowDocument` nicht kennt, **überlebt das `XamlPackage` nicht** — `Tag` und `ToolTip` an
    `Run` und `Paragraph` kommen als `null` zurück; nur ein `ToolTip` an einem `Image`
    übersteht es, und genau deshalb trägt `DocumentImages` ihn dort. **Daraus folgen die
    Lücken aus §4.45.**

    ⚠ **Seit §4.46 sind es nur noch zwei: Diagramm und Feld.** Die dritte — die
    **Gliederungsebene** — war keine Eigenschaft des `FlowDocument`, sondern der Zahlendreher
    aus Punkt 13, und sie hat sich mit ihm geschlossen. **Das senkt den Preis dieser
    Entscheidung, hebt sie aber nicht auf:** Ein Diagramm, das beim Speichern verschwindet,
    bleibt ein Diagramm, das verschwindet.

    **Die beiden Wege:**

    (a) **Das Paket bleibt, die Lücken bleiben benannt.** Schritt 7 kommt schnell, der
    Editor liest aus dem Modell, und **Diagramm und Feld** gehen beim Speichern im WPF-Kopf
    weiter verloren — **so wie sie es heute schon tun** (§4.45: der Verlust läuft seit §4.23).
    Es wäre keine Verschlechterung, aber ein Mangel, der mit dem Umschalten dauerhaft wird.
    (Die **Gliederungsebene** stand hier bis §4.46 mit in der Liste und ist seitdem heil.)

    (b) **Das Modell geht direkt in den `RichTextBox`.** Sobald `Rtf` nicht mehr geschrieben
    wird, gibt es im Speicherweg **gar kein `XamlPackage` mehr** — ein Träger lebt dann nur im
    Arbeitsspeicher und überlebt, weil ihn niemand serialisiert. Damit sind **beide**
    verbliebenen Lücken schließbar. **Der Preis steht schon als Warnung im Code:** `TextEditorView.AusModell` sagt,
    dass ein ausgetauschtes `Document` „dem `RichTextBox` seine Stile und alle
    Ereignisverdrahtungen mit" nähme — es müssten also die Blöcke umgehängt werden statt des
    Dokuments, und was beim Tippen mit einem Träger geschieht (Absatz teilen, Läufe
    zusammenziehen), ist **zu messen und nicht anzunehmen**.

    **Gewählt ist (b), als eigene Runde vor Schritt 7** — siehe oben.

13. ✅ **Erledigt am 2026-08-21 (§4.46) — und die Entscheidung ist dabei einmal
    zurückgenommen worden.**

    **Zuerst entschieden: den WPF-Kopf anheben.** Grundlage war der Befund aus §4.45 mit einer
    **ungeprüften Deutung** — dass der WPF-Kopf zu klein zeige. **Beim Bauen ist die Deutung
    zusammengebrochen:** `TextStyles.BodySize` ist als **DIP** dokumentiert, `TdStil.KoerperPt`
    als **Punkt** mit dem Kommentar „dieselbe Zahl wie `TextStyles.BodySize`", und
    `TdCharFormat.Standard.FontSize` ist **11 pt** — was 15 DIP entspricht. **Der WPF-Kopf und
    die Vorgabe des Dateiformats stimmten längst überein; die Kopie war weggelaufen.**
    **Der Beweis brauchte den anderen Kopf gar nicht:** Im Linux-Kopf machte die Vorlage
    „Standard" einen unberührten Absatz um ein Drittel **größer**.

    ✅ **Daraufhin umgedreht (Nutzer, 2026-08-21): `TdStil` wird verkleinert.** Alle Zahlen
    mal 0,75 — Körper 11,25, Überschrift 1 **21**, Titel 25,5; die Abstände ebenso, und der
    Einzug des Zitats auf die krumme Zahl, die richtig ist. `TdCharFormat.Standard` bleibt bei
    **11**: Das ist die Vorgabe des *Formats* und keine Vorlage (§4.14). **Gebaut, mit
    Wächtern belegt und in beiden Köpfen am laufenden Programm gesehen** (§4.46).

    **Die Lehre, und sie ist die eigentliche:** Eine Entscheidung, die auf einer ungeprüften
    Deutung des Befunds steht, ist selbst ungeprüft — **auch dann, wenn der Befund stimmt.**

    **Der Befund dahinter** — Nebenbefund aus §4.45, **nicht dort entstanden**:

    `TdStil.SizePt` ist in **Punkt**, `TextStyles.All[].Size` geht als WPF-`FontSize` in
    **geräteunabhängige Pixel** — bei 96 dpi ein Verhältnis von **4:3**. Also:

    - „Überschrift 1" ist im Linux-Kopf **28 pt**, im WPF-Kopf **28 px ≈ 21 pt**.
    - `TdStil.KoerperPt` ist **15**, `TdCharFormat.Standard.FontSize` ist **11**.

    **`VorlagentabelleTests` ist grün, weil er die Zahlen vergleicht und nicht die Größen** —
    und sein eigener Kommentar nennt genau den Zustand, den er verhindern sollte: „dasselbe
    Dokument in zwei Schriftbildern, je nachdem, welcher Kopf die Überschrift gesetzt hat".
    Die Zeile „bei 96 dpi ist beides dieselbe Zahl" über den Abständen ist die Stelle, an der
    die Verwechslung festgeschrieben wurde; 1 pt sind bei 96 dpi **1,333** px.

    **Es ist auch die Ursache dafür, dass Lücke 3 aus §4.45 so hart zuschlägt:**
    `TextStyles.HeadingLevel` misst 37,33 px gegen seine eigene 28 und erkennt eine
    Überschrift aus dem Linux-Kopf deshalb **nie** wieder.

    **Warum es nicht in §4.45 nebenbei behoben wurde:** Die Richtung war eine Entscheidung
    des Nutzers. **Sie ist am 2026-08-21 gefallen, und zwar zweimal** (siehe oben).

    ✅ **Und eine der drei Lücken aus §4.45 hat sich dabei von selbst geschlossen:** Die
    **Gliederungsebene** überlebt die Rundreise wieder, weil eine Überschrift 1 jetzt als
    21 pt = **28 px** ankommt und `TextStyles.HeadingLevel` sie gegen genau diese 28 hält.
    **Sie war keine Eigenschaft des `FlowDocument`, sondern ein Zahlendreher** — es sind noch
    **zwei** (Diagramm und Feld).


14. ✅ **Entschieden am 2026-08-28 (Nutzer): die beiden Listen werden zusammengeführt** —
    **mitgelieferte Schriften oben, Systemschriften darunter**, in **beiden** Köpfen
    dieselbe Quelle. Damit ist die Empfehlung dieses Dokuments angenommen.

    **Warum nicht „Linux ist Vorlage“, obwohl das sonst die Regel von Schritt ① ist**
    (§6): Würde der WPF-Kopf auf `Fonts.Mitgeliefert` allein umgestellt, fände **niemand
    mehr die Systemschrift wieder, die er heute in einem Dokument gewählt hat** — die
    Angleichung nähme etwas weg, statt einen Unterschied zu beseitigen. Umgekehrt (beide
    bekommen die Systemschriften) wäre der Schriftbestand unter Linux von Rechner zu
    Rechner verschieden, und **genau das sollte §4.26 verhindern**.

    **Der Fehler, den es behebt, ist gesehen und nicht hergeleitet** (§4.68): In einem neuen
    Textdokument steht das Schriftfeld des WPF-Kopfs **leer**, weil die Grundschrift
    „Source Sans 3“ heißt und die Liste sie nicht kennt. **Es ist kein falscher Name,
    sondern gar keiner.**

    **Diese Arbeit gehört in Schritt ①** (die UI-Angleichung) — sie ist ein sichtbarer
    Unterschied derselben Sorte und wird dort mit gemessen. Der Befund darunter bleibt.

    ⚠ ~~**Die Schriftliste des WPF-Kopfs zeigt nur Systemschriften.**~~ Benannter Nebenbefund
    aus §4.47, **nicht dort entstanden**.

    `FontCombo.ItemsSource` ist `Fonts.SystemFontFamilies`. Die fünf **mitgelieferten**
    Familien (§4.26) stehen nicht darin — und „Source Sans 3" ist die Grundschrift jedes
    Dokuments aus dem Modell. **Die Liste kann die Schrift des Dokuments also gar nicht
    anzeigen.**

    **Der Linux-Kopf macht es richtig** (§4.40: „`Fonts.Mitgeliefert` **ist** der Bestand, und
    eine Liste der *System*schriften wäre sogar falsch"). **Der WPF-Kopf ist nie nachgezogen
    worden.** Kein Datenverlust und keine Blockade — aber eine Liste, die etwas anderes
    behauptet als das Blatt darunter.

    ✅ **Am 2026-08-28 zum ersten Mal gesehen statt hergeleitet** (§4.68, beim Augenschein zur
    Aufräumrunde): In einem neuen Textdokument steht das Schriftfeld des WPF-Kopfs **leer** —
    die Grundschrift ist „Source Sans 3", und die Liste kennt sie nicht, also ist nichts
    gewählt. **Der Linux-Kopf zeigt an derselben Stelle „Source Sans 3".** Das Fehlerbild ist
    damit benannt: es ist kein falscher Name, sondern **gar keiner**.

    **Zu entscheiden ist, ob der WPF-Kopf dieselbe Quelle bekommt** (dann verschwinden die
    Systemschriften aus der Liste — wer heute eine davon gewählt hat, findet sie nicht mehr),
    **ob beide Listen zusammengeführt werden** (mitgeliefert oben, System darunter) oder ob es
    **als benannter Unterschied stehen bleibt**. **Empfehlung: zusammenführen** — der Bestand
    zuerst, das System danach; ein Dokument soll die Schrift zeigen können, in der es gesetzt
    ist, ohne dass ein bestehendes unlesbar wird.


15. ✅ **Entschieden am 2026-08-28 (Nutzer): es bleibt, wie es ist — und wird benannt.**
    Damit ist die abgelaufene Empfehlung „warten bis nach Phase 4.5“ ersetzt.

    **Die Begründung ist der Preis und nicht die Machbarkeit.** Möglich wäre es — `TdLayout`
    rechnet den echten Umbruch, der PDF-Weg tut es längst. Aber der Umbruch müsste **bei
    jeder Änderung nachlaufen**, damit die Zahlen nicht stillschweigend veralten, und genau
    deshalb wurden sie in §4.20 **gerechnet statt gespeichert**. *Eine Zahl, die manchmal
    stimmt, ist schlechter als keine.*

    **Es ist kein Datenverlust und kein Rückschritt:** Im **Modell** steht das Feld, im
    **PDF** stehen die Zahlen, und der **Linux-Kopf** rechnet beides sofort wieder aus (am
    laufenden Programm belegt, §4.50). Betroffen ist allein die Anzeige **im WPF-Editor**.

    **Ausdrücklich eine Ausnahme von der Vorlage-Regel aus Schritt ①** (§6: beim Editor ist
    Windows die Vorlage): Hier ist der Linux-Kopf der bessere von beiden, und er wird
    **nicht** an den schlechteren angeglichen. Die Regel betrifft das **Aussehen** des
    Ribbons, nicht das Rechnen dahinter. **In beiden READMEs unter den bekannten
    Unterschieden zu nennen** (Dauerregel 1).

    ⚠ ~~**Der WPF-Editor rechnet keine Seitenzahlen — das Feld zeigt `{SEITE}`, das
    Inhaltsverzeichnis hat keine.**~~ Beim Augenschein zu §4.49 gesehen (§4.50), **nicht dort
    entstanden**.

    **Der Grund ist derselbe für beides: Der Editor bricht keine Seiten um.** Ein
    `RichTextBox` fließt durch; `TdZuFlow.VerzeichnisUmwandeln` übergibt deshalb `null` als
    Umbruch, und jeder Eintrag steht auf **Seite 0**. Eine „0" hinzuschreiben wäre schlechter
    als keine Zahl.

    **Es ist kein Verlust und kein Rückschritt** — am Code vor §4.49 nachgesehen: Auch die
    frühere Fassung schrieb keine Seitenzahlen. **Im Modell steht das Feld**, und der
    Linux-Kopf rechnet beides sofort wieder aus (am laufenden Programm belegt, §4.50). Der
    Unterschied ist rein die Anzeige — **aber er ist ein sichtbarer Unterschied zwischen den
    zwei Köpfen an derselben Datei**, und deshalb steht er hier.

    **Zu entscheiden ist, ob es so bleibt.** Möglich wäre es: `TdLayout` rechnet den echten
    Umbruch, der PDF-Weg tut es längst. **Die Kosten sind aber nicht klein** — ein Umbruch
    müsste bei jeder Änderung nachlaufen, damit die Zahlen nicht stillschweigend veralten, und
    genau das war der Grund, sie in §4.20 zu *rechnen* statt zu speichern. **Empfehlung:
    stehen lassen bis nach Phase 4.5** und dann gemeinsam mit der Frage entscheiden, ob der
    WPF-Kopf überhaupt noch einen eigenen Editor behält (§6) — eine Seitenrechnung in einen
    Kopf zu bauen, den es danach vielleicht nicht mehr gibt, wäre die teuerste der drei
    Antworten.

16. ✅ **Entschieden am 2026-08-25 (Nutzer): beide Köpfe auf 0,5 bis 40 — gebaut.**
    Beim Bauen des Zahlenblocks gemessen (§4.61/§4.62): der WPF-Schieber lief von **1 bis
    20**, der des Linux-Kopfs von **0,5 bis 40**. Beide waren seit Langem so; aufgefallen ist
    es erst, als dasselbe Bedienelement in beiden Köpfen stand.

    **Der weitere Bereich nimmt nichts weg** — 0,5 ist ein feiner Strich, den 1 nicht kann,
    und ein Strich der Stärke 30 aus dem Linux-Kopf ließ sich drüben vorher **nicht auf
    seinen eigenen Wert einstellen**. Geändert wurde genau eine Zeile (`WhiteboardView.xaml`,
    der `Slider`); der Zahlenblock klemmt gegen die Werte des Schiebers und zieht damit von
    selbst mit. Die Vorgabewerte (Strich 2,5, Radierer 14) liegen in beiden Köpfen gleich und
    beide im neuen Bereich.

17. ✅ **Entschieden am 2026-08-25 (Nutzer): die zweite Stift-Taste darf verschieden
    bleiben — M2 verlangt hier keine Gleichheit.** Der WPF-Kopf öffnet damit die
    Schnellaktionen; der Linux-Kopf **radiert**, solange sie gehalten wird (§4.10, damals mit
    der Begründung „die Schnellaktionen sind nicht M1"). Seit §4.62 gibt es sie hier auch —
    die Begründung ist also abgelaufen, **die Belegung bleibt trotzdem**.

    **Warum das keine Lücke ist:** Radieren auf Tastendruck ist am Stift bequemer als jedes
    Menü, und die Schnellaktionen sind im Linux-Kopf über **Rechtsklick und langes Drücken**
    erreichbar — es fehlt also keine Funktion, nur ein zweiter Weg zu ihr. **Beim Ausrufen
    von M2 ist das nicht zu erwähnen**, anders als die Rechtschreibprüfung (§6): dort fehlt
    eine Funktion, hier ist es eine Bedienentscheidung je Kopf.

18. ✅ **Beantwortet am 2026-08-27: verwiesen wird, nicht mitgeliefert — Weg (a).**
    **Nutzer-Entscheidung**, und sie folgt der Empfehlung: das Flatpak-Manifest muss
    `tesseract` und `leptonica` ohnehin als Abhängigkeit nennen (§4.63), dort liegt die
    Version fest. **Gebaut in §4.64** (`TesseractLinux`): beim ersten Zugriff entstehen
    Symlinks auf die System-Bibliotheken unter `…/x64/`.

    **Zwei Dinge sind beim Bauen anders geworden als die Frage sie stellte.** Erstens liegen
    die Verweise **nicht neben dem Programm**, sondern in den Nutzerdaten — `AppFolder` ist
    unter Linux und im Flatpak schreibgeschützt. Zweitens ist der Einwand gegen (a)
    entschärft: `TesseractBindung.SonameWaehlen` nimmt die gemessene Hauptversion, sonst die
    **höchste vorhandene**, **steigt Arch also auf `libleptonica.so.7`, bricht der Verweis
    nicht** (Wächter in `TesseractBindungTests`).

    **(b) bleibt vorgemerkt** für den Fall, dass je außerhalb des Flatpaks ausgeliefert wird.

19. ✅ **Erledigt am 2026-08-27: ein gemeinsamer Fehler, in beiden Köpfen behoben** (§4.65).
    Die Frage war, ob es linuxspezifisch ist. **Ist es nicht** — der WPF-Kopf tut dasselbe,
    an beiden laufenden Programmen gegengeprüft, mit der Gegenprobe „nach einem Klick fügt
    derselbe Strg+V ein". `FokusHolen` gibt der Fläche nach dem Öffnen den Tastaturfokus,
    **außer der Fokus steht in einem Textfeld** — sonst risse es das Umbenennen eines frisch
    angelegten Boards mitten entzwei.

20. ✅ **Entschieden am 2026-08-28 (Nutzer): (a) — vorerst nichts umbauen, nur die Anleitung
    gilt.** `zeiger` behält `hervor`/`fenster` (reines X11, weiterhin richtig), geklickt und
    getippt wird mit `ydotool`; §5d und §7 sind in V2-89 bereits richtiggestellt. **Die
    Begründung ist die Lage und nicht die Technik:** Der Laptop hat keinen Auftrag, und ein
    Umbau ohne anstehende Messung ist Arbeit auf Vorrat. **Sobald wieder etwas zu messen ist,
    lohnt (b)** — die Abwägung darunter bleibt deshalb stehen, statt gelöscht zu werden.

    ~~Soll `tools/linux/zeiger` auf `ydotool` umgebaut werden — oder bleibt es bei zwei
    Werkzeugen nebeneinander?~~ Aufgeworfen in V2-89 (§4.64). **Der Befund ist nicht
    strittig:** XTEST ist unter GNOME 50 zum Klicken und Tippen unbrauchbar, weil das erste
    Ereignis den Portal-Dialog öffnet, dieser eine Wayland-Oberfläche ist und XTEST ihn nicht
    bedienen kann — **bis zur Freigabe kommt gar nichts an**. `ydotool` geht über
    `/dev/uinput` daran vorbei.

    **Was zu entscheiden ist, ist der Zuschnitt.** `zeiger` ist ein eigenes C#-Projekt und
    kann heute `hervor`/`fenster` (reines X11, weiterhin richtig), Klicks, Ziehbahnen, Tasten
    und Text. **Drei Wege:**

    | | Weg | Preis |
    |---|---|---|
    | **(a)** | `zeiger` behält `hervor`/`fenster`, alles andere geht künftig über `ydotool` | **Zwei Werkzeuge**, und die Anleitung muss sagen, wann welches. So ist V2-89 gefahren |
    | **(b)** | `zeiger` ruft intern `ydotool` auf und behält seine Befehlssprache | Eine Oberfläche, aber eine **Abhängigkeit von einem Paket** und von einem `root`-Daemon |
    | **(c)** | `zeiger` spricht `/dev/uinput` selbst an | Kein fremdes Paket, aber **`root`** und deutlich mehr Code — und die Belegungsfrage bleibt |

    **⚠ Und der eigentliche Stachel steckt in allen dreien:** `uinput` kennt nur
    **Keycodes**, keine Zeichen. Wer `Strg+Z` schreibt, meint auf QWERTZ den Keycode **21**
    und auf QWERTY die **44**. **Ein Werkzeug, das Tastennamen annimmt, muss die Belegung
    kennen** — sonst erzeugt es lautlos einen Scheinbefund, wie in V2-89 geschehen. *Das ist
    die eigentliche Arbeit, nicht das Einspritzen.*

    **Empfehlung dieses Dokuments: (a) vorerst**, und zwar unverändert lassen und nur die
    Anleitung richtigstellen (in V2-89 geschehen, §5d und §7). Der Laptop hat gerade keinen
    Auftrag; ein Umbau ohne anstehende Messung ist Arbeit auf Vorrat. **Sobald wieder etwas
    zu messen ist, lohnt (b).**

21. ✅ **Entschieden am 2026-08-28 (Nutzer): `HANDOFF.md` zieht nach `Docs/` — und die
    History-Frage ist damit beantwortet: KEIN Rewrite.** Umgesetzt in V2-93 (§4.70).

    **Der Auftrag im Wortlaut:** Die Datei *darf* weiterhin über die Vorgeschichte und auf
    anderen Wegen im Repo einsehbar sein; sie soll nur **keine der Hauptdateien auf der
    Startseite** des Repos sein, denn sie ist eine reine Arbeitsdatei. Als mögliche
    Umsetzung war das V1-Repo genannt (dort ist sie schlicht ausgeschlossen).

    **Warum trotzdem `Docs/` und nicht der V1-Weg** (`git rm --cached` + `.gitignore`):
    Der V1-Weg nimmt die Datei aus dem Baum, **und damit wandert sie nicht mehr per git
    mit**. Sie muss aber mitwandern — Windows ↔ CachyOS-Laptop heute, und **mit Phase 6
    kommt der iPad-Kopf als dritter Rechner dazu**. Ein Unterordner löst die gestellte
    Aufgabe vollständig (die Wurzelliste zeigt `Docs/`, nicht die Datei) und kostet nichts.

    **⚠ Die Regel aus der Kopfzeile bleibt unverändert und wird durch diese Entscheidung
    sogar schärfer:** *Nichts in diese Datei schreiben, was nicht irgendwann öffentlich
    stehen darf* — keine Pfade zu privaten Daten, keine Zugangsdaten, keine Inhalte aus den
    Schulunterlagen. **Ohne Rewrite ist das die einzige Sicherung, die es gibt.**

22. ✅ **Entschieden am 2026-08-28 (Nutzer): die Rechtschreibprüfung wird als bekannte
    Einschränkung veröffentlicht — und zugleich als erster Punkt nach M3 fest eingeplant.**

    **Beides, nicht eines von beidem.** Genannt wird sie in **beiden READMEs** *und* im
    **Über-Dialog** (Dauerregel 1, also vier Dokumente und zwei Sprachtabellen), und sie
    steht als **erster Punkt nach der Veröffentlichung** in der Roadmap **und** im README —
    **vor** dem iPad-Kopf.

    **Der Unterschied zu „benannt lassen“ ist der Punkt:** Ein benanntes Loch ohne Termin
    liest sich wie eine Absicht. *Es ist keine Absicht, es ist eine Reihenfolge.*

    **Der Grund, warum sie nicht vor die Veröffentlichung rückt, steht seit dem 2026-08-22
    fest** (§2): Die zwei „Ausnahmen“ von Phase 4.5 waren ungleich teuer — OCRs Naht stand
    **vollständig**, die der Rechtschreibung **gar nicht**: `ISpellChecker` liefert nur
    ja/nein und müsste **Fundstellen** liefern. Das ist eine eigene Runde und keine
    Aufräumarbeit.

23. ✅ **Entschieden am 2026-08-28 (Nutzer): die Veröffentlichung geht auf `1.0.0`.**
    Heute steht **0.3.0**. **Die Nummer wird am Ende von Schritt ⑤ gesetzt und nicht
    vorher** — sie gehört zur Auslieferung, und ein Stand, der die Nummer schon trägt,
    bevor er hinausgeht, ist zweimal derselbe Name für zwei verschiedene Stände.

    **Zu ändern sind dabei zusammen:** `Directory.Build.props` (`Version`), der Schlüssel
    **`About.Version` in `LocGerman` UND `LocEnglish`** (§4.5 — die Zeile steht seitdem
    nicht mehr im Code der Dialoge), die vier mitgelieferten Dokumente, der Git-Tag und der
    GitHub-Release. **Danach beide Über-Dialoge in beiden Sprachen und in beiden Köpfen
    gegenprüfen** (Dauerregel 1).

24. ✅ **Entschieden am 2026-08-28 (Nutzer): die Veröffentlichung umfasst alle vier Stücke.**
    READMEs überarbeiten, **GitHub Pages**, **Releases mit Artefakten**, **Repo-Beiwerk**.
    Der Zuschnitt steht in §6, Schritt ⑤.

    **Die Reihenfolge darin ist nicht beliebig:** Ein Release braucht ein gebautes Artefakt
    (also Schritt ③), und eine Projektseite, die auf einen Download zeigt, den es noch nicht
    gibt, ist schlechter als keine Seite.

25. ✅ **Entschieden am 2026-08-28 (Nutzer): der 1:1-Vergleich der Oberflächen läuft NUR
    unter Windows.** Beide Köpfe an derselben Datenbank-Kopie, `tools/fenster.ps1`, Bild
    neben Bild (§5b: `Avalonia.Desktop` läuft unter Windows, und nur dort lassen sich beide
    Köpfe nebeneinander fernsteuern).

    **⚠ Was dieser Vergleich damit nicht findet, und es steht hier, damit es später niemand
    für einen Rückschritt hält:** Unterschiede, die es **nur unter echtem Linux** gibt —
    fontconfig-Rückfallschrift, GNOME-Fensterdekoration, HiDPI-Maße. **Sie fallen beim
    Flatpak-Durchgang auf** (Schritt ③), der ohnehin auf dem Laptop läuft. *Keine
    Auslassung, eine Verschiebung — aber eine, die benannt sein muss.*
26. ✅ **Entschieden am 2026-08-29 (Nutzer): fehlt dem Linux-Kopf etwas, wird es dort
    NACHGEBAUT — nicht im WPF-Kopf gelöscht.**

    **Die Frage kam beim Messen von Schritt ①a** (§4.71) und musste gestellt werden, weil
    die Vorlage-Regel sie nicht beantwortet: „Bei jedem Unterschied ist Linux die Vorlage"
    setzt voraus, dass **beide** Köpfe etwas haben und es verschieden aussieht. **Der
    häufigste Fall ist ein anderer** — dem Linux-Kopf fehlt etwas ganz. Wörtlich genommen
    verlöre der WPF-Kopf dann sein **Cover-Werkzeug**, **sechs von sieben
    Einstellungs-Klappgruppen** der Tafel, **Suchen & Ersetzen**, den **ganzen
    Tabellenentwurf** und die **Kürzelanzeige** in Menüs und Kontextmenüs.

    **Was jetzt gilt:**

    | Lage | Was geschieht |
    |---|---|
    | Beide haben es, es sieht verschieden aus | **Linux gewinnt** (Editor: Windows, §4.70) |
    | **Linux fehlt es** | **im Linux-Kopf nachbauen** |
    | **Windows fehlt es** | **im WPF-Kopf nachbauen** (z. B. der Punkt für ungespeicherte Änderungen am Reiter) |
    | Eine Seite ist eine **durchschlagende Rahmenwerk-Vorgabe** und keine Entscheidung | **weder noch — nachfragen** (§4.71, violetter Auswahlbalken) |

    > **⚠ Das ist ausdrücklich keine Aufweichung der Regel, sondern ihre Lesart.** §4.70
    > hatte dieselbe Kollision schon einmal — an §5 Nr. 15, den Seitenzahlen — und dort
    > einzeln entschieden. **Der Fall ist aber nicht die Ausnahme, sondern die Mehrheit**,
    > und eine Regel, die man wörtlich nimmt, ohne zu prüfen, worauf sie zeigt, macht aus
    > einem Vorteil einen Verlust.

    **⛔ Was daraus folgt und beim Planen von ①b nicht untergehen darf:** Schritt ① ist
    damit **keine reine Angleicharbeit mehr**. Ein Cover-Werkzeug, eine Suchleiste und der
    Tabellenentwurf sind **Phase-4.5-Arbeit unter neuem Namen**. **Der Zuschnitt der
    Baurunden gehört dem Nutzer** und ist nach dem Befund von §4.71 zu treffen — nicht
    vorweg.

27. ✅ **Entschieden am 2026-08-29 (Nutzer): die Auswahl sieht in beiden Köpfen gleich aus
    und folgt dem Theme.**

    **Die Frage kam aus §4.71:** Fluent malte die gewählte Baumzeile und den Reiterstrich
    des Linux-Kopfs in `SystemAccentColor` — **violett**, während der Rest der App blau ist.
    Das war keine Gestaltungsentscheidung, sondern eine durchschlagende Vorgabe, und deshalb
    ließ sich die Vorlage-Regel nicht anwenden: **sie setzt voraus, dass beide Seiten gemeint
    sind.**

    **Die Antwort ist größer als die Farbe:** Heute ist es Blau, weil das mitgelieferte
    Schema blau ist. **Kommen später eigene Farbschemata** (§6, „Vorgemerkt: eigene
    Farbschemata"), **folgt die Auswahl in beiden Köpfen dem geladenen Schema.** Damit ist
    die Vorgabe nicht „blau streichen", sondern **eine Farbe aus der Tabelle statt einer aus
    dem Rahmenwerk**.

    **Umgesetzt in §4.72** — `Brush.Selection` für die Baumzeile, `Brush.Accent` für den
    Reiterstrich, beide aus `GonkNote.Core.Theming`. **Gegengeprüft mit einem Themenwechsel
    und nicht mit einem Bild:** hell `#C7DBFF`, dunkel `#2C3E66`, beides die Werte aus der
    Tabelle. *Ein Bild in Blau hätte nur gezeigt, dass es blau ist — nicht, dass es folgt.*

    > **⚠ Nicht mit entschieden und deshalb offen:** die **Gestalt** des gewählten Reiters.
    > Der Linux-Kopf zeichnet einen Strich darunter, der WPF-Kopf eine gefüllte Fläche mit
    > runden oberen Ecken. Das ist eine Formfrage, sie gehört in den Durchgang über Fläche 1,
    > und der Nutzer hat nach der **Farbe** gefragt.


28. ✅ **Entschieden am 2026-08-31 (Nutzer): die Rückmeldung (②) wird verschoben, bis die
    fehlenden Werkzeuge gebaut sind.**

    **Der Anlass steht in §4.75.** Nach dem Vermessen aller fünf Flächen ist klar geworden,
    dass ① aus **zwei verschiedenen Arbeiten** besteht, die nur denselben Namen tragen:

    | | |
    |---|---|
    | **Angleichen** | Symbole, Farben, Kürzel, Tooltips, Anordnung — Stellen, an denen **beide** Köpfe etwas haben und es verschieden aussieht |
    | **Bauen** | Suchen & Ersetzen, Tabellenentwurf, Cover-Werkzeug, sechs Einstellungs-Klappgruppen, Diagramm, Formen-Stift — Stellen, an denen **einer nichts hat** (§5 Nr. 26) |

    **Die Begründung des Nutzers, und sie ist die stärkere:** Eine Rückmeldung über eine
    Oberfläche, an der die Hälfte der Werkzeuge noch fehlt, **listet in der Mehrzahl Dinge,
    die ohnehin schon geplant sind**. Sie doppelt die Aufgabenliste, statt sie zu ergänzen —
    und wer zwei Listen führt, arbeitet die kürzere ab.

    > **⛔ Was das ausdrücklich NICHT heißt, damit es nicht als Streichung gelesen wird:**
    > ② fällt **nicht weg**. Der Satz aus §6 gilt unverändert — *wer eine Oberfläche in einem
    > Zug umbaut und nicht noch einmal hinsieht, hat sie nicht geprüft, sondern nur geändert.*
    > **② rückt nur hinter ①c**, und dann ist es die Rückmeldung über eine **vollständige**
    > Oberfläche. *Eine Prüfung, die zu früh kommt, ersetzt keine, die zur richtigen Zeit
    > kommt — sie verbraucht nur die Aufmerksamkeit dafür.*

    **Umgesetzt in §6:** ① zerfällt in **①a Vermessen** (erledigt), **①b Angleichen** und
    **①c Die fehlenden Werkzeuge**. ② folgt auf ①c.

29. ✅ **BEANTWORTET am 2026-09-04 (Nutzer): (b) — das AppImage bringt seine eigene
    Texterkennung mit.** Begründung des Nutzers: *„nicht jede Linux-Verteilung hat eine."*
    **Umgesetzt in §4.98** — `TesseractBindung.SuchpfadeMit` in Core (der mitgelieferte
    Ordner **vor** allen Systempfaden, +4 Wächter), `AppRun` setzt `LD_LIBRARY_PATH`, und
    `packaging/appimage/bauen.sh` sammelt die Kette mit `ldd` ein.
    ✅ **AM GERÄT GEPRÜFT am 2026-09-04 (V2-122, CachyOS)** — gebaut, und **mit `bwrap`
    gegen ein verstecktes System-Tesseract gemessen**: die Erkennung läuft zeichengenau aus
    dem Beipack, `/proc/self/maps` nennt beide Bibliotheken namentlich. **Der Beipack kostet
    24,5 MiB** (61 → 85 MiB). Der volle Befund steht in **§4.98 „Was der Laptop gefunden
    hat"**.

    > **▶ Damit ist §5 Nr. 18 nicht aufgehoben, sondern um einen Fall ergänzt:** *verwiesen
    > wird, mitgeliefert nur dort, wo es sonst nichts gäbe.* Das Flatpak verweist weiter auf
    > `/app/lib` aus seinem Manifest, ein Lauf aus dem Quellordner weiter auf das System —
    > **und findet sich das Mitgelieferte nicht, fällt die App auf genau diesen alten Weg
    > zurück.** *Mitliefern ist eine zusätzliche Stufe, keine Ablösung; ein Wächter hält fest,
    > dass die Systempfade angehängt und nicht ersetzt werden.*
    >
    > **⛔ Hier stand bis zum 2026-09-04 „nicht **oder lädt es nicht**", und das ist falsch —
    > jetzt auf dem Laptop gemessen** (§4.98, V2-122). §4.98 hat genau diesen Satz im
    > Wächter-Kommentar schon einmal richtiggestellt und **ihn hier stehen lassen**; die
    > Messung war eindeutig: ein unbrauchbares `libtesseract.so.5` im Beipack lässt die
    > Erkennung scheitern, **obwohl ein tadelloses System-Tesseract danebenliegt**.
    > *Eine Richtigstellung, die nur eine von zwei Fundstellen erreicht, ist eine halbe — und
    > die verbliebene liest sich danach wie geprüft.*

    *Der ursprüngliche Wortlaut der Frage, damit die Abwägung nachlesbar bleibt:*

    Aufgeworfen von Schritt ③
    (§4.96), und die Frage ist eine an das **Modell** und nicht an die Verpackung.

    **Der Befund:** Im Flatpak trägt die Erkennung, weil `TesseractBindung.Suchpfade` in Core
    `/app/lib` an erster Stelle führt. **Ein AppImage hat keinen eigenen Namensraum** —
    `/usr/lib` ist der des Wirts, und für den Einhängepunkt des Abbilds gibt es **keinen
    Eintrag**. Heute heißt das: hat der Wirt Tesseract, geht die Erkennung; hat er keines,
    meldet die App ehrlich „nicht verfügbar" und blendet den Knopf aus (§4.64).

    **Die zwei Wege:**

    **(a) So lassen und im README benennen.** Das AppImage ist dann „abhängigkeitsfrei bis
    auf die Texterkennung". Kostet nichts, und die App lügt an keiner Stelle.

    **(b) `Suchpfade` um den eigenen Ordner erweitern** — etwa `AppFolder/lib`, gefüllt vom
    AppImage-Skript. **Das ist eine Core-Änderung und betrifft damit beide Köpfe**, also
    Windows-Arbeit; dazu wandert die Bibliothek unter unsere Lizenzangabe
    (`THIRD-PARTY-NOTICES.md`) und muss bei jedem Bau nachgezogen werden.

    **Die Empfehlung dieses Dokuments war (a)** — das Flatpak ist der Hauptweg (§6), und
    für den ist die Frage schon beantwortet. **⛔ Der Nutzer hat (b) entschieden, und die
    Begründung schlägt die Empfehlung:** Sie stellte auf den *Hauptweg* ab; der Nutzer stellt
    auf den *Zweck des zweiten Kanals* ab. **Ein AppImage gibt es gerade für den Rechner, auf
    dem kein Flatpak läuft** — und dort ist „hat der Wirt zufällig Tesseract?" die schlechteste
    aller Antworten. *Eine Empfehlung, die den Hauptweg zum Maßstab nimmt, misst den zweiten
    Kanal an der falschen Frage.*

30. **Zwei Verlaufsstapel — einer oder zwei?** Aufgeworfen von Schritt ④ (§4.97), und es ist
    der einzige Posten der Vorratsliste, der **keine Arbeit** ist, sondern eine Frage.

    **Der Befund:** `UndoStack` (Zeichenfläche) und `TdUndo` (Text) teilen rund **zwanzig
    Zeilen** — zwei Listen, ein Deckel, ein Ereignis. **§4.33 hat sie mit drei Gründen
    bewusst getrennt gelassen**, und die Gründe stehen dort; sie sind nicht widerlegt.

    **Die zwei Wege:**

    **(a) So lassen und den Grund in §7 festhalten.** Zwanzig Zeilen Ähnlichkeit sind keine
    Doppelung: die beiden Stapel speichern **verschiedene Dinge** (Elemente gegen Blöcke) und
    werden von verschiedenen Rechnungen gefüllt. Was sie teilen, ist die **Form** eines
    Stapels, nicht sein Inhalt — und eine gemeinsame Basisklasse für „Liste mit Deckel" ist
    die Sorte Abstraktion, die man später wieder auseinandernimmt.

    **(b) Zusammenlegen.** Eine gemeinsame Grundlage in Core, zwei Ableitungen. Es wäre eine
    eigene Runde, sie **berührt beide Köpfe** und den Verlauf jedes offenen Dokuments.

    **Die Empfehlung dieses Dokuments ist (a)** — und zwar nicht aus Bequemlichkeit: §4.13
    hat `WbHit` und die Markdown-Grammatik zusammengelegt, weil dort **dieselbe Rechnung**
    zweimal stand und die zwei Fassungen auseinanderlaufen konnten, *ohne dass es auffällt*.
    Hier läuft nichts auseinander — es gibt keine gemeinsame Wahrheit, die zwei Stellen
    verschieden beantworten könnten. **Aber es ist die Sorte Frage, die zum Aufräumen gehört,
    und deshalb steht sie hier und nicht in einem Kommentar.**

    ✅ **BEANTWORTET am 2026-09-04 (Nutzer): (a) — getrennt lassen.** **Der Grund steht seit
    §4.98 in §7** („Neu aus Phase 5"), und das ist der eigentliche Handgriff dieser Antwort:
    *Ein „nein" ohne Begründung ist keine Entscheidung, sondern eine Verzögerung* — die nächste
    Runde, die die zwanzig ähnlichen Zeilen sieht, fragte sonst zum dritten Mal.
    **In §4.33 stand der Grund schon; er stand nur an der Stelle, die niemand liest, wenn er
    eine Doppelung sucht.**

**Beantwortet und hier nur noch als Verweis** — der volle Wortlaut stand bis zum 2026-08-11
darunter und wurde von niemandem mehr gelesen; was gilt, steht in der Tabelle oben:

| Frage | Antwort | Wo |
|---|---|---|
| Sagt der Über-Dialog die richtige Phase? | ja, auf Phase 4 gesetzt (2026-08-10) | §4.25, jetzt wieder offen als Punkt 5 |
| Beschreiben die vier Dokumente V1 oder V2? | **V2** (2026-08-03) — der Klon-Befehl in beiden Erste-Schritte-Fassungen zeigt auf `GonkNote.git`. **Er läuft heute nur mit Zugriff**, weil das Repo privat ist; das wird mit §6 „Vor dem Öffentlich-Schalten" richtig | §4.12 |
| Wann beschreiben sie auch den Linux-Kopf? | mit M1 (2026-08-03), Abschnitt „Zwei Ausgaben, eine App" in beiden READMEs | §4.12 |
| Soll die Neigung ins Dateiformat? | **ja** (2026-08-03). Die Gegenprobe am echten Stift gehört zu Punkt 1 | §4.11 |
| Soll es ein achtes Diagramm geben (Fläche)? | **vorerst nein** (2026-08-10). Es wäre ein *Modell*schritt und keine Zeichenarbeit: neuer Enum-Wert, `c:areaChart` in beide Richtungen, beide Beispieldokumente, beide Vergleichsmethoden, `ChartDialog` und beide Sprachtabellen. Der Zeichner selbst wäre der kleinste Teil — eine gefüllte Kurve ist ein `TdChartZug` mit `Fuellung`, und das Netz füllt schon genauso | §4.25 |

---
