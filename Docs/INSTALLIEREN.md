# Gonk Note installieren

Diese Seite bringt Gonk Note auf deinen Rechner — und sonst nichts. Wenn du danach
wissen willst, *wie* man damit arbeitet, geht es in
[Erste Schritte](ERSTE-SCHRITTE.md) weiter.

*(Deutsche Fassung. Die englische ist `INSTALL.md`.)*

---

## Inhalt

1. [Voraussetzungen](#voraussetzungen)
2. [Weg 1 — Windows 11 (fertige Datei)](#weg-1--windows-11-fertige-datei)
3. [Weg 2 — Linux, AppImage](#weg-2--linux-appimage)
4. [Weg 3 — Linux, Flatpak](#weg-3--linux-flatpak)
5. [Selbst aus dem Quellcode bauen](#selbst-aus-dem-quellcode-bauen)
6. [Wo deine Daten liegen · Deinstallieren](#wo-deine-daten-liegen--deinstallieren)
7. [Wenn der Bau fehlschlägt](#wenn-der-bau-fehlschlägt)

---

## Voraussetzungen

- **Windows 11** (Windows 10 sollte auch laufen, ist aber nicht getestet) **oder Linux**
- **Keine Adminrechte**, keine Registry-Einträge, keine Cloud

Die fertigen Downloads (Weg 1 und 2) laufen **ohne installiertes .NET**. Nur wer selbst
aus dem Quellcode baut, braucht das
[.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) oder neuer.

Alle Downloads liegen unter
**[Releases](https://github.com/GONKstupid/GonkNote/releases)**.

---

## Weg 1 — Windows 11 (fertige Datei)

1. `GonkNote-<version>-windows-x64.zip` herunterladen und entpacken.
2. `GonkNote.exe` starten.

> ⚠ **Den Ordner zusammenlassen.** Neben der Exe liegen drei Ordner, und jeder wird
> gebraucht:
>
> | Ordner | Wofür | Fehlt er, dann … |
> |---|---|---|
> | `Fonts` | Oberflächen- und Dokumentschriften | Gonk Note **startet trotzdem** und zeichnet alles in der Windows-Systemschrift — **ohne Hinweis** |
> | `tessdata` | Sprachdaten der Texterkennung | alles läuft außer OCR |
> | `Assets` | Cover-Vorlagen, Geodreieck | mitgelieferte Cover und Geodreieck fehlen |
>
> Wer die Exe verschiebt, nimmt die drei Ordner mit.

---

## Weg 2 — Linux, AppImage

Eine Datei, keine Abhängigkeiten, keine Sandbox:

```bash
chmod +x GonkNote-<version>-x86_64.AppImage
./GonkNote-<version>-x86_64.AppImage
```

Dein System braucht **fontconfig und mindestens eine Schrift** — ohne sie bleibt jeder
gezeichnete Text leer. Auf Arch-artigen Systemen (CachyOS, Manjaro, EndeavourOS …):

```bash
sudo pacman -S fontconfig ttf-dejavu
```

Auf Debian-artigen (Ubuntu, Mint …) heißen die Pakete `libfontconfig1` und
`fonts-dejavu-core`.

Das ist die einzige Voraussetzung. Alles andere steckt im Abbild, **auch die
Texterkennung**: Sie funktioniert dort, wo gar kein Tesseract installiert ist.

---

## Weg 3 — Linux, Flatpak

Sandbox und Software-Zentrum, als Hauptweg vorgesehen. **Auf Flathub gibt es Gonk Note
aber noch nicht** — bis dahin baust du das Paket selbst. Die Voraussetzungen (Runtime
und SDK von Flathub) und die zwei Befehle stehen in
[packaging/LIESMICH.md](../packaging/LIESMICH.md):

```bash
cd packaging/flatpak && ./bauen.sh
flatpak run io.github.gonkstupid.GonkNote
```

---

## Selbst aus dem Quellcode bauen

Der Weg für alle, die mitentwickeln oder den neuesten Stand wollen. Voraussetzung:
[.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) oder neuer.

```bash
git clone https://github.com/GONKstupid/GonkNote.git
cd GonkNote
```

> **Immer projektbezogen bauen, nie die ganze Solution.** Sie enthält beide Ausgaben,
> und die Windows-Ausgabe lässt sich unter Linux nicht übersetzen — das ist so gewollt.

**Windows:**

```powershell
# Nur zum Ausprobieren, direkt aus dem Quellcode
dotnet run --project src/GonkNote.Wpf

# Fertige Einzeldatei (läuft ohne installiertes .NET, beliebig verschiebbar)
dotnet publish src/GonkNote.Wpf -c Release
# Ergebnis: src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\GonkNote.exe
```

**Kopiere den ganzen Ordnerinhalt**, nicht nur die Exe — `Fonts`, `tessdata` und
`Assets` gehören dazu (siehe Tabelle bei Weg 1).

**Linux:**

```bash
dotnet run --project src/GonkNote.Avalonia
```

Dein System braucht **fontconfig und mindestens eine Schrift** (siehe Weg 2). Der erste
Lauf lädt die Pakete herunter und dauert ein paar Minuten.

Unter Wayland läuft Gonk Note über XWayland; Druck und Neigung des Stifts kommen darüber
vollständig an.

---

## Wo deine Daten liegen · Deinstallieren

Beim ersten Start legt Gonk Note still einen Ordner an — **unter Windows
`%APPDATA%\GonkNote`, unter Linux `~/.config/GonkNote`**:

```
<Datenordner>/
├─ gonknote.sqlite      deine Texte, Striche und die Ordnerstruktur
├─ gonknote.blobs/      Bilder sowie importierte PDF- und Word-Seiten
└─ gonknote.papierkorb/ Bilder, die gerade niemand braucht (30 Tage Schonfrist)
```

Nichts wird ins Internet geschickt, nichts in die Registry geschrieben, nichts
installiert. **Zum Deinstallieren reichen das Programm und dieser Ordner.** Den Pfad
zeigt dir jederzeit **Hilfe → Über Gonk Note** an.

---

## Wenn der Bau fehlschlägt

| Meldung | Ursache |
|---|---|
| Zugriff verweigert / Datei in Verwendung | Gonk Note läuft noch — schließen und erneut bauen |
| `NETSDK1045` o. Ä. zur Framework-Version | .NET SDK zu alt — [aktuelles SDK 10+](https://dotnet.microsoft.com/download/dotnet/10.0) installieren |
| `net10.0-windows…` lässt sich nicht auflösen (Linux) | Du baust die Windows-Ausgabe oder die ganze Solution — nimm `src/GonkNote.Avalonia` |
| Merge-Konflikt bei `git pull` | du hast lokale Änderungen — `git stash`, dann erneut `git pull` |
| Unter Linux bleibt jeder gezeichnete Text leer | `fontconfig` oder eine Schrift fehlt — siehe Weg 2 |

---

**Weiter:** [Erste Schritte mit Gonk Note](ERSTE-SCHRITTE.md) · [Feature-Übersicht im README](../README.md)
