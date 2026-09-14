[← Index: HANDOFF.md](../HANDOFF.md)

## 1. Auftrag und die Entscheidungen dahinter

Aus dem Vorgespräch zur Roadmap, nicht ohne Rückfrage ändern:

- **Ziel-Plattformen:** Linux zuerst, iPadOS danach. Windows/WPF bleibt bestehen.
- **Vertrieb iPad:** App Store / TestFlight → **NativeAOT ist Pflicht**. Das prägt die
  Architektur (kein `Reflection.Emit`, kein LiteDB, Json über Source-Generator).
- **Texteditor:** **eigene Dokument-Engine**, kein Feature-Verlust. Kein Rückzug auf einen
  einfachen Editor.
- **Vorgehen:** gemeinsame Basis zuerst, dann Linux, dann iPad.
- **Die App soll mit jedem Stylus funktionieren** (Nutzer, 2026-07-29) — nicht nur mit dem
  Gerät, auf dem gerade getestet wird. Der Eingabepfad wird gegen die *Fähigkeiten* des
  Geräts geschrieben; fehlt Druck oder Neigung, muss der Strich trotzdem sauber aussehen.
- **V1 bleibt unangetastet stehen** — Referenz und Notausgang bis Meilenstein M1.
  Ab jetzt **keine neuen Features in V1**; Bugfixes dort sofort nach V2 cherry-picken.

Die Anforderungen aus V1 (offline, Single-File, Stylus-first, drei Dokumenttypen,
Import/Export, Dark/Light bei hellem Papier, RAM-Ziel 800 MB / Grenze 1 GB, so open source wie
möglich) gelten unverändert weiter — siehe `gonk-note\HANDOFF.md` §1.

---
