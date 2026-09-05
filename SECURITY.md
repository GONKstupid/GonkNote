# Sicherheit · Security

*Deutsch zuerst, English below.*

## Deutsch

### Was hier überhaupt eine Sicherheitslücke wäre

Gonk Note hat **keinen Server, kein Konto und keine Netzwerkverbindung**. Es gibt nichts zu
übernehmen und keine Sitzung zu entführen. Angreifbar ist die App trotzdem, und zwar über
das, was sie einliest — **fremde Dateien**:

- ein **DOCX**, **PDF** oder **Markdown**, das jemand importiert
- ein **Bild** oder eine **SVG**, die auf die Zeichenfläche gezogen wird
- eine **eigene Cover-Vorlage**, ein **Sticker** oder ein **Geodreieck-SVG** aus dem
  Datenordner
- eine **Datenbankdatei**, die per `--db` mitgegeben wird

Wenn eine dieser Dateien dazu führt, dass Code ausgeführt wird, dass etwas außerhalb des
Datenordners geschrieben oder gelesen wird, oder dass die App etwas ins Netz schickt — dann
ist das eine Lücke, und dann bitte melden.

**Ein Absturz allein ist keine.** Er ist ein Fehler; dafür gibt es die Fehlervorlage.

### Wie melden

**Nicht als öffentliches Issue.** Nimm den Weg über GitHub:
**[Security → Report a vulnerability](https://github.com/GONKstupid/GonkNote/security/advisories/new)**.
Der ist privat, bis wir beide etwas dazu sagen können.

Hilfreich ist: welche Ausgabe (Windows oder Linux), welche Version, und die kleinste Datei
oder Schrittfolge, mit der es sich wiederholen lässt.

### Was du erwarten kannst — und was nicht

Das hier ist ein Ein-Personen-Projekt eines Schülers. Es gibt **keine zugesagte
Reaktionszeit** und kein Sicherheitsteam. Was es gibt: Ich lese das, ich antworte, und wenn
es stimmt, wird es behoben und in den Anmerkungen der nächsten Ausgabe benannt.

### Welche Fassungen gepflegt werden

**Nur die jeweils neueste.** Es gibt keine Wartungszweige — die Behebung kommt in der
nächsten Ausgabe.

---

## English

### What would count as a vulnerability here

Gonk Note has **no server, no account and no network connection**. There is nothing to take
over and no session to hijack. It is still attackable, though — through what it reads,
namely **files from elsewhere**:

- a **DOCX**, **PDF** or **Markdown** file someone imports
- an **image** or **SVG** dropped onto the canvas
- a **custom cover template**, **sticker** or **set-square SVG** from the data folder
- a **database file** passed in with `--db`

If one of those causes code to run, causes something outside the data folder to be read or
written, or makes the app send anything over the network — that is a vulnerability, and
please report it.

**A crash on its own is not one.** That is a bug; the bug template is for that.

### How to report

**Not as a public issue.** Use GitHub's private route:
**[Security → Report a vulnerability](https://github.com/GONKstupid/GonkNote/security/advisories/new)**.
It stays private until both of us have something to say about it.

Helpful: which edition (Windows or Linux), which version, and the smallest file or sequence
of steps that reproduces it.

### What you can expect — and what you cannot

This is a one-person project by a school student. There is **no promised response time** and
no security team. What there is: I read it, I answer, and if it holds up it gets fixed and
named in the next release's notes.

### Supported versions

**The latest one only.** There are no maintenance branches — the fix arrives in the next
release.
