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
