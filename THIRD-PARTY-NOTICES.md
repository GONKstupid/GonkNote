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
| LiteDB | 5.0.21 | https://github.com/litedb-org/LiteDB |
| SkiaSharp | 2.88.9 | https://github.com/mono/SkiaSharp |
| SkiaSharp.Views.WPF | 2.88.9 | https://github.com/mono/SkiaSharp |
| Svg.Skia | 1.0.0.18 | https://github.com/wieslawsoltes/Svg.Skia |
| DocumentFormat.OpenXml | 3.1.0 | https://github.com/dotnet/Open-XML-SDK |
| Docnet.Core | 2.6.0 | https://github.com/GowenGit/docnet |

Den jeweiligen Lizenztext samt Copyright-Zeile findest du im NuGet-Paket bzw. im verlinkten
Projekt-Repository.

---

## Apache License 2.0

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

## .NET und WPF

Gonk Note setzt auf .NET 8 und WPF (MIT-Lizenz, Microsoft). Beim Single-File-Publish wird die
.NET-Laufzeit mit eingebettet; für deren Weitergabe gelten die Bedingungen von Microsoft.

---

## Mitgelieferte Grafiken

| Datei(en) | Herkunft |
|---|---|
| `Assets/Covers/**` (Basic, Muster, Pixel Art) | eigene Werke des Autors, MIT wie das Projekt |
| `Assets/Geodreieck-Light.svg`, `-Dark.svg` | eigene Werke des Autors, MIT wie das Projekt |
| `Assets/GonkNote.ico`, `Assets/gonk-note-Icon.png` | eigene Werke des Autors, MIT wie das Projekt |

**Sticker liefert Gonk Note bewusst keine mit.** Das Sticker-Werkzeug funktioniert
ausschließlich mit Bildern, die du selbst unter `%APPDATA%\GonkNote\Stickers` ablegst —
für deren Rechte bist du selbst verantwortlich.

---

## Markennamen

Im Programm und in der Dokumentation werden fremde Produkte benannt (GoodNotes, Microsoft
Word, Adobe Fresco, Apple Notes …), um Vorbilder und Dateiformate zu beschreiben. Die
jeweiligen Marken gehören ihren Inhabern. Gonk Note steht in keiner Verbindung zu ihnen und
wird von ihnen weder unterstützt noch geprüft.
