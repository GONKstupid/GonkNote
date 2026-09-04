# Avalonia-Issue: tote Tasten unter X11 — Entwurf zum Absenden

> **Was das hier ist.** Der fertige Text für die Meldung an `AvaloniaUI/Avalonia`, die §5
> „Noch offen" **Nr. 11** beschlossen hat: **melden und stehen lassen**, nicht umgehen.
> Geschrieben in Phase 5, Schritt ④ (2026-09-04). Der Befund selbst stammt aus **§4.42**,
> **§4.44** und der Laptop-Messung in §4.44 („Was der Laptop gefunden hat", V2-62).
>
> **⛔ Er ist nicht abgesendet.** Das gehört dem Nutzer: es ist sein GitHub-Konto und ein
> öffentlicher Beitrag. **Vor dem Absenden zwei Dinge prüfen**, weil beide seit dem
> 2026-08-19 veraltet sein können:
> 1. Ist **#18596** noch geschlossen, und gibt es inzwischen ein neueres Issue mit demselben
>    Symptom? Dann dort kommentieren statt neu aufmachen.
> 2. Ist **12.1.1** noch die Fassung, gegen die wir bauen? Steht in
>    `Directory.Packages.props`. Wenn nicht, den Befund gegen die neue Fassung noch einmal
>    kurz gegenprüfen — die Zeilennummern unten stammen aus einer **Zerlegung** und sind
>    ohnehin nur als Wegweiser gemeint, nicht als Zitat.
>
> **Was bewusst nicht drinsteht:** kein Vorwurf, keine Vermutung, kein Vorschlag für eine
> Umgehung in fremdem Code. Was gemessen ist, steht mit dem Werkzeug daneben, mit dem es
> gemessen wurde; was hergeleitet ist, ist als Herleitung gekennzeichnet. *Eine Meldung, die
> Messung und Vermutung nicht trennt, wird als Vermutung gelesen.*

---

## Titel

```
X11: dead keys / Compose sequences are silently dropped — Avalonia lets libX11 filter the
event but never retrieves the composed result
```

## Labels (Vorschlag)

`bug`, `platform-linux`, `input`

---

## Text der Meldung

**Avalonia version:** 12.1.1 (the same symptom is reported for 11.2.6 in #18596)
**OS:** CachyOS (Arch), GNOME 50.3, Wayland session — the app runs as an XWayland client
**Input method:** IBus 1.5.34, `XMODIFIERS=@im=ibus`
**Locale / layout:** `de_DE.UTF-8`, German keyboard (`pc_de_de_2_inet(evdev)`)

### Summary

On X11, a dead key followed by a base letter produces **nothing at all** — no character, no
replacement, no error. `^` + `e` should give `ê`; the application receives no input event and
the character count does not increase. Single-keysym characters such as `ä`, `ö`, `ü`, `ß` are
**not** affected, because they are not composed.

The cause is not that the composition fails. It succeeds — **Avalonia just never picks up the
result.** libX11's own local input method filters the dead key, composes the character and
queues it as a `KeyPress` with `keycode = 0`, to be retrieved via `XmbLookupString`. Avalonia
derives the keysym from the **keycode** instead, gets keysym `0`, and forwards
`ProcessKeyEvent(0, 0, 0)` to IBus over D-Bus. IBus answers `true` ("handled"), so Avalonia
discards the event — and the composed character is lost.

In other words: Avalonia accepts libX11's filtering, but attributes the composition to IBus
over D-Bus. **The path on which the composed text is actually available (`XmbLookupString`) is
never taken.**

### Steps to reproduce

1. A GNOME/Wayland session with IBus (`XMODIFIERS=@im=ibus`), locale `de_DE.UTF-8`, German
   keyboard layout.
2. Any Avalonia app with a text input target — i.e. one that registers a
   `TextInputMethodClient` via `TextInputMethodClientRequestedEvent`. **A plain `TextBox` is
   enough.**
3. Type `^` then `e`.

**Expected:** `ê`
**Actual:** nothing — no character, no replacement character, no exception.

**Two controls:**
* `gnome-text-editor` in the same session receives the full sequence (`ê`, `á`) — the platform
  and the input method are fine.
* The **same Avalonia binary** started with an empty `XMODIFIERS` produces `ê` correctly.

> **⚠ This only shows up once the app registers an input-method client.** As long as it does
> not, Avalonia evaluates the raw event itself and never asks IBus — so it never encounters the
> `true` answer it now discards. In our project the regression appeared the moment we
> implemented `TextInputMethodClient`; before that, dead keys worked.

### What we measured

All three measurements were taken in the same session, within the same minute.

**1. `x11trace` around the process** — the physical press *is* on the wire, and it is filtered:

* the `KeyPress` for the dead key arrives normally (`keycode 49`, keysym `dead_circumflex`),
* `XFilterEvent` returns **`True`** for it,
* **no event with `keycode 0` ever appears on the X connection.**

**2. `xev`** — what becomes of the swallowed press. In the process, 13 events with
`keycode 0` appear, 4 of them `ecircumflex`, each with:

```
XmbLookupString gives 2 bytes: (c3 aa) "ê"
```

The composed character is therefore fully available — but **only via `XmbLookupString`, not
via the keycode.** Running `xev` with `@im=none` makes both the filtering and the `keycode 0`
events disappear, and the raw presses come through unchanged. That pins the mechanism on the
**local input method of Xlib** (which reads the locale's Compose table) — not on `ibus-x11`
and not on IBus itself.

**3. `dbus-monitor`** — what Avalonia sends to IBus at the same moment:

```
ProcessKeyEvent(keyval = 0, keycode = 0, state = 0)   ->   reply: true
```

IBus reports the event as handled, so Avalonia drops the raw event.

### Where this happens in the code

Line numbers are from a decompile of `Avalonia.X11.dll` / `Avalonia.FreeDesktop.dll` 12.1.1
(`ilspycmd`) and are meant as signposts, not as citations.

| # | Location | What happens |
|---|---|---|
| 1 | `X11EventDispatcher.DispatchX11Events` (Avalonia.X11, ~L740) | `XFilterEvent` runs before everything else. On `true` the event is `continue`d — no window ever sees it |
| 2 | `X11Window.OnEvent` (~L12036) | `KeyPress`/`KeyRelease` → `HandleKeyEvent` |
| 3 | `X11Window.HandleKeyEvent` (~L13175) | **`LookupKey(ev.KeyEvent.keycode)` determines the keysym from the keycode.** With `keycode = 0` this yields keysym `0` |
| 4 | `ScheduleKeyInput` → `FilterIme` (~L13363/13373), `ProcessNextImeEvent` (~L13387) | queues the tuple unchanged |
| 5 | `DBusTextInputMethodBase.HandleEventAsync` (Avalonia.FreeDesktop, ~L6733) → `IBusX11TextInputMethod.HandleKeyCore` (~L7071) | builds the modifier mask and calls `ProcessKeyEventAsync(keyval, keycode, mask)` (~L7104), unchanged |

Steps 2–5 do not modify either value, so **a real press (`keycode 49`) can never become
`0/0/0` on this path.** Avalonia does not invent the phantom event — it forwards it. The
composed text that libX11 attached to it is simply never read.

### Suggested direction (not a patch)

When an event arrives with `keycode == 0` after `XFilterEvent` has filtered a preceding press,
the payload is text, not a key. Retrieving it with `XmbLookupString` — the way `xev` does —
would make the composed character available; deriving a keysym from the keycode cannot.

We have deliberately **not** worked around this in our application. From our side it would
only be possible by reaching past Avalonia to X11 directly, and the defect is not ours.

### Relation to #18596

**#18596** ("Dead keys/accented characters not working on Linux", 11.2.6) describes exactly
this symptom. It was closed by the reporter without a fix ("switched to Fcitx5"); the only
response was a request for `setxkbmap -query`.

> **⚠ One caution about that request, because it cost us a measurement:** under XWayland,
> `setxkbmap -query` reports `layout: us`, and `_XKB_RULES_NAMES` on the root window says the
> same. **The actual keymap here is German** — `xkbcomp -xkb :0` yields
> `xkb_symbols "pc_de_de_2_inet(evdev)"` with `key <TLDE> = dead_circumflex`. Anyone
> diagnosing this from `setxkbmap -query` will be led away from the layout that reproduces it.

**#13351 in SubtitleEdit** is a different cause with the same symptom: Avalonia 11.x defaulted
`EnableIme = false` for European locales. That does not apply here — 12.1.1 defaults it to
`true`, we set no X11 options at all (`UsePlatformDetect()` only), and the D-Bus path is
demonstrably live in our traces.

### Two secondary findings, noted rather than filed separately

Both were found while tracing the above. Neither causes the dead-key loss — the engine decides
on the keysym, and that one is correct — but both are in the same code path:

1. **`HandleKeyCore` forwards the X11 keycode unchanged to IBus.** IBus expects evdev
   keycodes, which are X11 keycodes **minus 8**.
2. **`HandleKeyCore` does not map `LockMask` (NumLock)** — only Ctrl / Mod1 / Shift / Mod4 and
   the release bit. An event that X11 reports with `state = 16` reaches IBus with `state = 0`.

---

## Was nach dem Absenden im HANDOFF nachzuziehen ist

* Die Nummer des Issues in **§5 „Noch offen" Nr. 11** und in **§4.44** eintragen — beide
  verweisen heute nur auf `#18596`.
* In **§6, „Was in ④ ansteht"** die Zeile „Das Avalonia-Issue schreiben" abhaken.
* In den **READMEs** (Schritt ⑤) steht die Einschränkung ohnehin schon als benannt
  (§5 Nr. 11): *zusammengesetzte Zeichen kommen unter Linux nicht an.* **Dort gehört dann die
  Issue-Nummer dazu** — eine benannte Einschränkung mit Verweis ist etwas anderes als eine
  ohne.
