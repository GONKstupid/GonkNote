# Mitmachen · Contributing

*Deutsch zuerst, English below.*

> **Warum diese Datei einsprachig-doppelt ist und nicht zweimal existiert:** Die vier
> mitgelieferten Dokumente (`README.md`, `README.en.md`, `ERSTE-SCHRITTE.md`,
> `GETTING-STARTED.md`) gibt es paarweise, weil die App sie anzeigt und nach der eingestellten
> Sprache auswählt. Diese hier zeigt niemand an — und zwei Dateien, die niemand nebeneinander
> sieht, laufen auseinander. Also stehen beide Sprachen in einer Datei.

---

## Deutsch

Danke fürs Hinsehen. Gonk Note ist ein Ein-Personen-Projekt eines Schülers; es gibt kein
Team, keine Roadmap-Abstimmung und keine Reaktionszeit, auf die du dich verlassen könntest.
Das ist keine Absage — nur eine ehrliche Erwartung.

### Was am meisten hilft

1. **Fehler melden.** Am wertvollsten sind Berichte von Geräten, die es hier nicht gibt:
   andere Stifte (Wacom, XP-Pen, Huion, Surface), andere Linux-Verteilungen, andere
   Bildschirmauflösungen. Entwickelt wird auf Windows 11 und einem CachyOS-Laptop — alles
   andere ist ungeprüft.
2. **Sagen, was unverständlich ist.** Wenn die Anleitung an einer Stelle nicht weiterhilft,
   ist das ein Fehler in der Anleitung.
3. **Code.** Gern, aber bitte vorher ein Issue aufmachen. Siehe „Bevor du Code schickst".

### Einen Fehler melden

Nimm die [Fehlervorlage](https://github.com/GONKstupid/GonkNote/issues/new/choose). Was
wirklich gebraucht wird, ist wenig, aber davon nichts weglassen:

- **Welche Ausgabe** — Windows oder Linux — und welche Version (`Hilfe → Über Gonk Note`)
- **Was du getan hast**, in der Reihenfolge, in der du es getan hast
- **Was passiert ist** und was du erwartet hättest
- Auf Linux: Verteilung, Desktop und ob **Wayland oder X11**
- Wenn etwas abgestürzt ist: die letzten Zeilen aus `<Datenordner>/fehler.log`

> **Bitte keine eigenen Dokumente anhängen.** Ein Bildschirmfoto vom Fehler genügt fast
> immer, und dein Notizbuch geht niemanden etwas an.

### Bevor du Code schickst

**Mach zuerst ein Issue auf.** Das Projekt hat Entscheidungen, die von außen wie Willkür
aussehen und keine sind — sie stehen mit Begründung in `Docs/HANDOFF.md`. Ein Pull Request,
der eine davon umdreht, kostet uns beide Zeit.

Wenn wir uns einig sind:

```bash
dotnet build GonkNote.slnx -c Release -warnaserror   # 0 Fehler, 0 Warnungen
dotnet test  GonkNote.slnx -c Release                # beide Testprojekte
```

Die zwei Regeln, an denen hier nichts verhandelbar ist:

- **`GonkNote.Core` kennt keine Oberfläche.** Was gerechnet, gemessen, gelesen oder
  geschrieben wird, gehört in den Kern; in einer Oberfläche steht nur, was Pixel zeichnet
  oder Eingaben entgegennimmt. Dass es die Linux-Ausgabe überhaupt gibt, liegt genau daran.
- **Ein sichtbarer Unterschied wird am laufenden Programm geprüft, in beiden Ausgaben.**
  Ein grüner Bau beweist an einer Oberfläche fast nichts. Das ist in diesem Projekt mehrfach
  teuer gelernt worden.

Dazu: Oberflächentexte kommen aus `Loc` und werden **in beiden Sprachtabellen** ergänzt
(`LocGerman`, `LocEnglish`) — nie nur in einer.

**Sprache im Code:** Kommentare, Commit-Nachrichten und Bezeichner sind hier deutsch. Wenn
dir das nicht liegt, schreib englisch — das ist besser als gar kein Kommentar.

### Was eher nicht kommt

Cloud-Abgleich, Konten, Telemetrie, ein Plugin-System. Nicht aus Prinzipienreiterei, sondern
weil das Versprechen der App ein einzelner Ordner auf deinem Rechner ist.

### Sicherheitslücken

Nicht als Issue. Siehe [SECURITY.md](SECURITY.md).

---

## English

Thanks for looking. Gonk Note is a one-person project by a school student; there is no team,
no roadmap committee and no response time you could rely on. That is not a brush-off — just
an honest expectation.

### What helps most

1. **Bug reports.** The most valuable ones come from hardware that does not exist here:
   other styluses (Wacom, XP-Pen, Huion, Surface), other Linux distributions, other screen
   resolutions. Development happens on Windows 11 and one CachyOS laptop — everything else
   is untested.
2. **Telling us what is unclear.** If the guide leaves you stuck somewhere, that is a bug in
   the guide.
3. **Code.** Welcome, but please open an issue first. See "Before you send code".

### Reporting a bug

Use the [bug template](https://github.com/GONKstupid/GonkNote/issues/new/choose). What is
actually needed is little, but please leave none of it out:

- **Which edition** — Windows or Linux — and which version (`Help → About Gonk Note`)
- **What you did**, in the order you did it
- **What happened** and what you expected instead
- On Linux: distribution, desktop and whether **Wayland or X11**
- If something crashed: the last lines of `<data folder>/fehler.log`

> **Please do not attach your own documents.** A screenshot of the failure is almost always
> enough, and your notebook is nobody else's business.

### Before you send code

**Open an issue first.** This project has decisions that look arbitrary from the outside and
are not — they are written down with their reasons in `Docs/HANDOFF.md` (German). A pull
request that reverses one of them costs us both time.

Once we agree:

```bash
dotnet build GonkNote.slnx -c Release -warnaserror   # 0 errors, 0 warnings
dotnet test  GonkNote.slnx -c Release                # both test projects
```

The two rules that are not up for negotiation here:

- **`GonkNote.Core` knows nothing about a UI.** Anything computed, measured, read or written
  belongs in the core; a front end contains only what draws pixels or takes input. The Linux
  edition exists precisely because of this.
- **A visible change is checked in the running program, in both editions.** A green build
  proves almost nothing about a user interface. This project has learned that the expensive
  way, more than once.

Also: interface strings come from `Loc` and are added to **both** tables (`LocGerman`,
`LocEnglish`) — never just one.

**Language in the code:** comments, commit messages and identifiers here are German. If that
does not work for you, write English — that beats no comment at all.

### What is unlikely to happen

Cloud sync, accounts, telemetry, a plugin system. Not out of principle, but because the
app's promise is a single folder on your own machine.

### Security issues

Not as an issue. See [SECURITY.md](SECURITY.md).
