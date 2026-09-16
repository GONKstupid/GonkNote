# Hinweise zu Fremdsoftware · Third-Party Notices

Gonk Note selbst steht unter der MIT-Lizenz (siehe [LICENSE](LICENSE)). Diese Datei listet
die mitgelieferte bzw. eingebundene Fremdsoftware und die Vermerke, die deren Lizenzen bei
einer Weitergabe verlangen.

Das ist besonders relevant, weil Gonk Note als **Single-File-Exe** veröffentlicht wird: die
folgenden Bibliotheken stecken dann mit im Programm. Wer die Exe weitergibt, gibt sie mit
weiter — und sollte diese Datei beilegen.

Die Lizenzangaben stammen aus den Paket-Metadaten der verwendeten NuGet-Versionen.

---

## MIT

Die folgenden Pakete stehen unter der MIT-Lizenz. Deren Bedingungen entsprechen der
Lizenz von Gonk Note selbst: Copyright-Vermerk und Lizenztext müssen bei einer Weitergabe
erhalten bleiben.

| Paket | Version | Projekt |
|---|---|---|
| Microsoft.Data.Sqlite | 10.0.10 | https://github.com/dotnet/efcore |
| LiteDB | 5.0.21 | https://github.com/litedb-org/LiteDB |
| SkiaSharp | 3.119.4 | https://github.com/mono/SkiaSharp |
| SkiaSharp.Views.WPF | 3.119.4 | https://github.com/mono/SkiaSharp |
| Svg.Skia | 5.1.1 | https://github.com/wieslawsoltes/Svg.Skia |
| DocumentFormat.OpenXml | 3.1.0 | https://github.com/dotnet/Open-XML-SDK |
| Docnet.Core | 2.6.0 | https://github.com/GowenGit/docnet |
| WeCantSpell.Hunspell | 7.0.1 | https://github.com/aarondandy/WeCantSpell.Hunspell |

**LiteDB steht nicht mehr im Produktivpfad.** Es liegt nur noch in `GonkNote.Legacy` und
liest dort Datenbanken bis einschließlich Version 0.2.0 ein, damit sie einmalig nach SQLite
übertragen werden können. Weitergegeben wird es trotzdem mit — der Vermerk bleibt.

Den jeweiligen Lizenztext samt Copyright-Zeile findest du im NuGet-Paket bzw. im verlinkten
Projekt-Repository.

---

## Apache License 2.0

### SQLitePCLRaw, Version 2.1.x

https://github.com/ericsink/SQLitePCL.raw

`Microsoft.Data.Sqlite` bindet SQLitePCLRaw ein — das Paket, das die native
SQLite-Bibliothek je Plattform mitbringt (`SQLitePCLRaw.bundle_e_sqlite3` samt
`core`, `provider.e_sqlite3` und `lib.e_sqlite3`). Es steht unter der Apache License 2.0.

### Tesseract (.NET-Anbindung), Version 5.2.0

https://github.com/charlesw/tesseract

### Tesseract-Sprachdaten (`tessdata/deu.traineddata`, `tessdata/eng.traineddata`)

https://github.com/tesseract-ocr/tessdata_fast — Copyright (C) Google Inc.

Diese beiden Dateien liegen **unverändert** als Begleitdateien neben der Exe.

Beide Bestandteile stehen unter der Apache License, Version 2.0:

> Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file
> except in compliance with the License. You may obtain a copy of the License at
>
> http://www.apache.org/licenses/LICENSE-2.0
>
> Unless required by applicable law or agreed to in writing, software distributed under the
> License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
> either express or implied. See the License for the specific language governing permissions
> and limitations under the License.

---

## BSD 3-Clause

### PDFium

Docnet.Core bindet die PDF-Bibliothek **PDFium** ein (native Binärdateien im Paket).
PDFium stammt aus dem Chromium-Projekt und steht unter der BSD-3-Clause-Lizenz.
Die Lizenz verlangt, dass der folgende Vermerk bei einer Weitergabe erhalten bleibt:

```
Copyright 2014 The PDFium Authors. All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are
permitted provided that the following conditions are met:

   * Redistributions of source code must retain the above copyright notice, this list of
     conditions and the following disclaimer.
   * Redistributions in binary form must reproduce the above copyright notice, this list of
     conditions and the following disclaimer in the documentation and/or other materials
     provided with the distribution.
   * Neither the name of Google Inc. nor the names of its contributors may be used to
     endorse or promote products derived from this software without specific prior written
     permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS
OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

---

## SIL Open Font License 1.1

Gonk Note **liefert seine Schriften mit** (`Assets/Fonts/`, HANDOFF §4.26). Der Grund ist kein
gestalterischer: „Segoe UI" gibt es unter Linux nicht und unter iPadOS auch nicht, und auf
keinem Linux-System ist eine bestimmte Schrift garantiert. Ohne mitgelieferte Schriften sähe
dasselbe Dokument auf drei Plattformen verschieden aus.

**Was die OFL bei einer Weitergabe verlangt** — und wie es hier eingehalten wird:

| Bedingung | Umsetzung |
|---|---|
| Lizenztext und Copyright-Vermerk müssen mitgehen | Je Familie liegt die unveränderte `OFL.txt` im Ordner der Schrift und wird in die Ausgabe **neben die Exe** kopiert |
| Die Schrift darf nicht einzeln verkauft werden | Trifft nicht zu — sie wird als Teil des Programms weitergegeben |
| Eine **veränderte** Fassung darf einen Reserved Font Name nicht weiterführen | **Nichts wird verändert und nichts beschnitten.** Damit greift die Regel nicht. Sie wäre sonst nicht theoretisch: **Source Sans führt „Source" als Reserved Font Name** |

**Nur auszuwählen, welche Schnitte mitgehen, ist keine Veränderung** — mitgeliefert wird je
Familie das, was die App benutzt, und nicht der ganze Satz.

| Familie | Version | Rolle in der App | Copyright | Projekt |
|---|---|---|---|---|
| Inter | 4.1 | Oberfläche: Menüs, Ordnerbaum, Galerie, Dialoge | Copyright (c) 2016 The Inter Project Authors | https://github.com/rsms/inter |
| Source Sans 3 | 3.052R | Grundschrift der Textdokumente | Copyright 2010–2022 Adobe (http://www.adobe.com/), with Reserved Font Name 'Source' | https://github.com/adobe-fonts/source-sans |
| JetBrains Mono | 2.304 | Code und Festbreitentext | Copyright 2020 The JetBrains Mono Project Authors | https://github.com/JetBrains/JetBrainsMono |
| Space Grotesk | 2.0.0 | Cover-Titel und große Überschriften | Copyright 2020 The Space Grotesk Project Authors | https://github.com/floriankarsten/space-grotesk |
| Geist | 1.7.2 | Textfelder, Notizzettel und Sticker auf dem Whiteboard | Copyright 2024 The Geist Project Authors | https://github.com/vercel/geist-font |

Den vollständigen Lizenztext samt Copyright-Zeile findest du je Familie in
`Assets/Fonts/<Familie>/OFL.txt` — im Repo und neben der ausgelieferten Exe.

> **Inter war schon vorher dabei**, über das NuGet-Paket `Avalonia.Fonts.Inter` im Linux-Kopf —
> **ohne Vermerk in dieser Datei.** Das war eine Lücke und ist mit §4.26 geschlossen.

---

## Die mitgelieferten Wörterbücher (Rechtschreibprüfung)

Gonk Note **liefert seine Wörterbücher mit** (`Assets/Dictionaries/`, HANDOFF §5 Nr. 22,
Phase 5.1/5.2). Der Grund ist derselbe wie bei den Schriften einen Abschnitt weiter oben: Auf
keinem Linux-System ist ein Hunspell-Wörterbuch garantiert — auf dem Rechner, auf dem diese
Funktion entstanden ist, war keines installiert. Ohne Beipack wäre die Rechtschreibprüfung
bei den meisten Nutzern vorhanden und nirgends wirksam.

**⚠ Diese beiden Dateisätze stehen NICHT unter der MIT-Lizenz des Programms**, und das
deutsche steht sogar unter einer Copyleft-Lizenz. Beides ist zulässig und ändert nichts an
der Lizenz von Gonk Note:

* Ein Wörterbuch ist eine **Datentabelle**, keine Bibliothek. Es wird nicht dazugebunden,
  sondern zur Laufzeit als Datei gelesen — dieselbe Art Beipack wie die Schriften und die
  Tesseract-Sprachdaten. Die GPL wirkt auf das, womit sie ein Werk bildet, und eine
  Wortliste neben dem Programm ist eine bloße Zusammenstellung auf einem Datenträger.
* Die Dateien gehen **unverändert** hinaus, samt ihren README- und Lizenzdateien.
* Weitergegeben wird das, was die GPL als Quelle eines Wörterbuchs kennt: die `.dic` und
  die `.aff` selbst. Es gibt keine übersetzte Form davon, die man zusätzlich schulden könnte.

Wer nur das Programm ohne die Wörterbücher weitergeben will, lässt den Ordner
`Dictionaries/` weg: Dann meldet die Prüfung ehrlich „nicht verfügbar", und der Schalter in
der Statusleiste bleibt grau (HANDOFF §4.64, dieselbe Regel wie bei der Texterkennung).

### de_DE — GNU General Public License, Version 2 oder 3

`Assets/Dictionaries/de_DE.aff`, `de_DE.dic` · Fassung `20161207+frami20170109`

Aus der LibreOffice-Wörterbuchsammlung (`de/de_DE_frami`), abgeleitet vom
**igerman98**-Wörterbuch von Björn Jacke — https://www.j3e.de/ispell/igerman98/ . Die
Erweiterung „frami" wird von Franz Michael Baumann gepflegt.

> Das Wörterbuch und alle enthaltenen Wortlisten sind lizenziert unter der GNU GPL,
> Version 2 oder 3.

Der vollständige Lizenztext liegt als `COPYING_GPLv3` daneben, die Herkunftsangabe als
`README_de_DE_frami.txt`; beide werden mit in die Ausgabe kopiert und gehen mit dem Programm
hinaus.

> **Hinweis zur Fassung:** Das Basis-igerman98 steht wahlweise auch unter der
> OASIS-Verteilungslizenz. **Für die hier mitgelieferte `frami`-Erweiterung gilt das
> nicht** — ihre eigene README nennt ausdrücklich nur die GPL v2 oder v3. Wer die
> permissivere Wahlmöglichkeit braucht, nimmt das Basiswörterbuch statt dieser Erweiterung.

### en_US — SCOWL (BSD-artig, permissiv)

`Assets/Dictionaries/en_US.aff`, `en_US.dic` · Fassung `2020.12.07`

Aus SCOWL (Spell Checker Oriented Word Lists) von Kevin Atkinson —
http://wordlist.sourceforge.net . Die Affix-Datei ist eine stark überarbeitete Fassung der
`english.aff` aus Geoff Kuennings Ispell und steht unter dessen BSD-Lizenz.

> The collective work is Copyright 2000-2018 by Kevin Atkinson as well as any of the
> copyrights mentioned below:
>
> Copyright 2000-2018 by Kevin Atkinson
>
> Permission to use, copy, modify, distribute and sell these word lists, the associated
> scripts, the output created from the scripts, and its documentation for any purpose is
> hereby granted without fee, provided that the above copyright notice appears in all copies
> and that both that copyright notice and this permission notice appear in supporting
> documentation. Kevin Atkinson makes no representations about the suitability of this array
> for any purpose. It is provided "as is" without express or implied warranty.

Die vollständigen Angaben zu Quellen und Beiträgen — darunter Alan Beales 12Dicts, das
gemeinfreie Moby-Lexikon und Brian Kelks Wortliste — stehen in `README_en_US.txt`, das
ebenfalls mit in die Ausgabe kopiert wird.

### LanguageTool — benutzt, wenn da; **nicht mitgeliefert**

https://languagetool.org · https://github.com/languagetool-org/languagetool

Seit 1.0.4 nimmt Gonk Note die Befunde eines **LanguageTool-Servers** an, wenn auf demselben
Rechner einer läuft (`languagetool --http`). **Weitergegeben wird davon nichts**: kein Byte
LanguageTool liegt im Programmordner, im AppImage oder im Flatpak, und es gibt keine
Paketabhängigkeit darauf. Wer den Server will, installiert ihn selbst — aus der
Paketverwaltung seiner Verteilung oder von der Projektseite.

Deshalb entsteht hier **keine Lizenzpflicht**: LanguageTool steht unter der LGPL 2.1, und die
greift bei Weitergabe. Gonk Note gibt es nicht weiter, sondern spricht über HTTP mit einem
Dienst, den der Nutzer betreibt — dieselbe Art Beziehung wie zu einem Drucker oder einer
Datenbank.

> **⛔ Nur der eigene Rechner.** Die Prüfung spricht ausschließlich mit dem Loopback-Gerät;
> jede andere Adresse wird abgelehnt, auch über die Umgebungsvariable. Die **öffentliche API
> von languagetool.org wird bewusst nicht unterstützt** — ein Grammatikdienst bekommt den
> Text des Dokuments zu sehen, und „Deine Daten liegen nur auf diesem Rechner" ist eine
> Zusage und keine Voreinstellung.

### Die Prüfmaschine selbst

Gelesen werden die Dateien von **WeCantSpell.Hunspell** (MIT, siehe Tabelle oben) — einer
Hunspell-Umsetzung in reinem C#. **Es wird keine native `libhunspell` mitgeliefert und keine
eingebunden**; deshalb taucht hier nur ein MIT-Paket auf und kein zweiter Beipack wie bei
Tesseract.

---

## ISC License

### Lucide

https://lucide.dev · https://github.com/lucide-icons/lucide

**Die Symbole der Oberfläche stammen aus Lucide** (Fassung 1.31.0) — Ordner, Stift, Radierer,
Rückgängig, Papierkorb und die sechzig anderen (HANDOFF §4.31). Sie liegen **nicht als
Dateien** in der Ausgabe, sondern als Pfadangaben in einer Tabelle im Programm
(`Core/Theming/Icons.cs`); aus den SVG-Dateien ist nur die Geometrie übernommen worden.

**Warum überhaupt ein fremder Satz:** Vorher benutzte die Windows-Ausgabe die Schrift
*Segoe Fluent Icons*. Die gehört Microsoft, darf nicht mitgeliefert werden und fehlt unter
Linux und iPadOS — dieselbe Lage wie bei „Segoe UI" und den Schriften (siehe SIL OFL oben).
Ein Symbol, das nur auf einer Plattform erscheint, ist keines.

**Sieben Formen sind eigene** und stehen unter der Lizenz von Gonk Note selbst: Notizbuch,
Textdokument, Whiteboard, Geodreieck, Seitenbreite, Ganze Seite und
Fenster-Wiederherstellen.

> ISC License
>
> Copyright (c) 2026 Lucide Icons and Contributors
>
> Permission to use, copy, modify, and/or distribute this software for any
> purpose with or without fee is hereby granted, provided that the above
> copyright notice and this permission notice appear in all copies.
>
> THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
> WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
> MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
> ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
> WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
> ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
> OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

**Ein Teil der benutzten Symbole stammt ursprünglich aus [Feather](https://feathericons.com/)
und steht zusätzlich unter der MIT-Lizenz** — betroffen sind hier unter anderem `chevron-down`,
`chevron-left`, `chevron-right`, `chevron-up`, `info`, `minus`, `moon`, `move`, `plus`,
`search`, `square`, `trash-2`, `type`, `upload`, `x`, `zoom-in` und `zoom-out`. Lucide führt die
vollständige Liste in seiner `LICENSE`.

> The MIT License (MIT)
>
> Copyright (c) 2013-present Cole Bemis
>
> Permission is hereby granted, free of charge, to any person obtaining a copy
> of this software and associated documentation files (the "Software"), to deal
> in the Software without restriction, including without limitation the rights
> to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
> copies of the Software, and to permit persons to whom the Software is
> furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all
> copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
> IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
> FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
> AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
> LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
> OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
> SOFTWARE.

---

## Gemeinfrei

### SQLite

https://www.sqlite.org/copyright.html

Die eigentliche Datenbank-Bibliothek (`e_sqlite3`, native Binärdateien im
SQLitePCLRaw-Paket) ist **Public Domain** — ihre Urheber haben ausdrücklich auf das
Urheberrecht verzichtet. Es gibt deshalb nichts, was bei einer Weitergabe erhalten bleiben
müsste. Die .NET-Anbindung darum herum steht unter MIT bzw. Apache 2.0, siehe oben.

---

## .NET und WPF

Gonk Note setzt auf .NET 10 und WPF (MIT-Lizenz, Microsoft). Beim Single-File-Publish wird die
.NET-Laufzeit mit eingebettet; für deren Weitergabe gelten die Bedingungen von Microsoft.

---

## Mitgelieferte Grafiken

| Datei(en) | Herkunft |
|---|---|
| `Assets/Covers/**` (Basic, Muster, Pixel Art) | eigene Werke des Autors, MIT wie das Projekt |
| `Assets/Geodreieck-Light.svg`, `Assets/Geodreieck-Dark.svg` | eigene Werke des Autors, MIT wie das Projekt |
| `Assets/GonkNote.ico`, `Assets/gonk-note-Icon.png` | eigene Werke des Autors, MIT wie das Projekt |
| `site/bilder/*.png` | Bildschirmfotos der eigenen App, MIT wie das Projekt |

**Zu den Bildschirmfotos:** Sie gehen mit **nicht** hinaus — sie liegen im Repo und auf der
Projektseite, nicht im Programm. Ihr Inhalt stammt aus einer **erfundenen Demo-Datenbank**
(`tools/demo-db`) und nicht aus einem echten Bestand; das ist keine Feinheit, sondern die
Bedingung dafür, dass sie veröffentlicht werden dürfen.

Das ist alles, was Gonk Note an Grafik mitbringt. **Sticker werden bewusst nicht
mitgeliefert:** das Sticker-Werkzeug arbeitet ausschließlich mit Bildern, die du selbst unter
`%APPDATA%\GonkNote\Stickers` ablegst — für deren Rechte bist du verantwortlich.

Auch das Geodreieck lässt sich austauschen: eine eigene Zeichnung unter
`%APPDATA%\GonkNote\Geodreieck-Light.svg` bzw. `-Dark.svg` hat Vorrang vor der
mitgelieferten (16-cm-Geodreieck, Hypotenusen-Mitte im Zentrum, viewBox 2520×1680).

So ist das gelöst, weil das Projekt nur Material ausliefert, dessen Herkunft eindeutig ist.

---

## Markennamen

Im Programm und in der Dokumentation werden fremde Produkte benannt (GoodNotes, Microsoft
Word, Adobe Fresco, Apple Notes …), um Vorbilder und Dateiformate zu beschreiben. Die
jeweiligen Marken gehören ihren Inhabern. Gonk Note steht in keiner Verbindung zu ihnen und
wird von ihnen weder unterstützt noch geprüft.
