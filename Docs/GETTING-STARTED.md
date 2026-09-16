# Getting started with Gonk Note

This guide shows *how to work with Gonk Note* — from your first notebook to exporting.
Allow about **10 minutes**.

- **How to get Gonk Note onto your machine** is in [Install](INSTALL.md).
- **What Gonk Note can do** is in the
  [feature overview in the README](../README.en.md).

*(English version. The German original is `ERSTE-SCHRITTE.md`. Inside the app this guide
follows the language you picked under View → Language — it is also under
Help → Getting started.)*

---

## Contents

1. [The first start](#1-the-first-start)
2. [The window in 30 seconds](#2-the-window-in-30-seconds)
3. [Your first notebook](#3-your-first-notebook)
4. [Writing and drawing](#4-writing-and-drawing)
5. [Selecting and changing things](#5-selecting-and-changing-things)
6. [Annotating a PDF or Word document](#6-annotating-a-pdf-or-word-document)
7. [Whiteboard and text document](#7-whiteboard-and-text-document)
8. [Exporting](#8-exporting)
9. [Backups — please set this up once](#9-backups--please-set-this-up-once)
10. [Your own stickers, covers and set square](#10-your-own-stickers-covers-and-set-square)
11. [Language and appearance](#11-language-and-appearance)
12. [Your own themes — including with an AI](#12-your-own-themes--including-with-an-ai)
13. [Cheat sheet](#13-cheat-sheet)
14. [When something goes wrong](#14-when-something-goes-wrong)
15. [Updating Gonk Note](#15-updating-gonk-note)

---

## 1. The first start

Windows: double-click `GonkNote.exe`. Linux: the AppImage or `dotnet run`.

On the first start Gonk Note quietly creates a folder — **`%APPDATA%\GonkNote` on
Windows, `~/.config/GonkNote` on Linux**:

```
<data folder>/
├─ gonknote.sqlite      your texts, strokes and the folder structure
├─ gonknote.blobs/      images plus imported PDF and Word pages
└─ gonknote.papierkorb/ images nobody currently needs (30-day grace period)
```

> **Remember this path.** It is also your backup — see
> [section 9](#9-backups--please-set-this-up-once). You do not have to memorise it:
> **Help → About Gonk Note** shows it.

---

## 2. The window in 30 seconds

| Where | What |
|---|---|
| **Menu bar at the top** | `File`, `View`, `Help` |
| **Sidebar on the left** | your folder tree, four buttons for new items at the top, quick access ("PINNED") above that |
| **Middle** | the **gallery** — as long as nothing is open you see the current folder as large tiles |
| **Tabs** | every open document gets its own tab |

If the sidebar is in the way: `Ctrl+B`.

When you maximise the window the title bar disappears. It glides back in as soon as you
move the mouse to the top edge of the window.

---

## 3. Your first notebook

1. **Create** — `File → New notebook`, or the "New" button above the gallery, or the
   notebook button at the top of the sidebar.
2. **Name it** — the name can be changed at any time with `F2`.
3. **Open** — double-click the tile in the gallery or the entry in the tree. The
   notebook opens in a tab of its own.
4. **Turn pages** — the notebook's page bar sits at the bottom centre:
   `◀  Page 1 / 1  ▶` and next to it **New page** (`+`) and **Delete page**.
5. **Change the look** — the **gear icon** on the right of the toolbar opens the
   settings. Under **Page** you choose the pattern (blank, ruled, squared, dotted), the
   shade and the format (A4/A3, portrait/landscape). With *"Set as default for new
   pages"* the choice also applies to everything you create afterwards.
6. **Set a cover** — the **Cover** section in the same sidebar: gradient, lettering or an
   image. The categories "Basic", "Muster" and "Pixel Art" are included; under
   **Individuell** you upload your own images via the "+" tile.

**Saving happens by itself** — every 30 seconds, when closing the tab and when quitting.
`Ctrl+S` still works if it makes you feel better.

---

## 4. Writing and drawing

The tools sit in the bar at the top; each has a keyboard shortcut:

| Key | Tool | Good for |
|---|---|---|
| `S` | Pen | normal writing, pressure-sensitive |
| `B` | Pencil | sketches with graphite grain |
| `M` | Highlighter | emphasising |
| `G` | Shape pen | scribble a circle, rectangle or line — it recognises them |
| `E` | Eraser | erases precisely, splits strokes at the point of contact |
| `T` | Text box | typed text on the page |
| `N` | Sticky note | coloured notes |
| `F` | Shapes | line, arrow, rectangle, ellipse, triangle |
| `R` / `D` | Ruler / set square | straight lines and angles |
| `H` | Hand | pan the view |

**Three moves that make the difference:**

- **Setting the stroke width exactly:** press and hold the size slider (or the icon next
  to it) — a number pad opens for direct entry. The eraser remembers its own size.
- **Zoom and pan:** `Ctrl+mouse wheel` zooms; panning works with the middle mouse
  button, a held space bar or the hand tool. On a touchscreen: one finger pans, two
  fingers zoom.
- **Made a mistake?** `Ctrl+Z`. A double tap with three fingers does the same.

**With a stylus:** the back of the pen erases automatically. The second stylus button
opens the quick menu (see the next section).

---

## 5. Selecting and changing things

1. **Select** — either `L` (lasso) and encircle the object, or `V` (move) and click the
   object directly. The lasso only takes what you have enclosed more or less completely.
2. **Change** — drag to move, corner handle to scale, rotation handle to rotate
   (snapping every 15°). This applies equally to strokes, shapes, text, images and
   sticky notes.
3. **Quick menu** — after a selection a small icon bar appears automatically: cut, copy,
   duplicate, paste, **recognise text (OCR)**, delete, select all.

   You also get it via **right-click**, the **second stylus button**, or by pressing and
   holding for about half a second with lasso/move/hand — so you can work entirely
   without a keyboard.

**Getting text out of an image:** select the image → quick menu → *Recognise text
(OCR)*. Recognition runs offline (German and English). You can copy the result or insert
it directly as a sticky note.

---

## 6. Annotating a PDF or Word document

The typical case: marking up lecture notes or a worksheet.

1. Click **Insert file** in the toolbar — or simply drag the file into the window, or
   press `Ctrl+V`.
2. For PDF and Word a **page selection dialog** with thumbnails appears. Pick what you
   need.
3. Confirm:
   - In a **notebook** every selected page becomes a page of its own to write on.
   - In a **whiteboard** the pages land as high-resolution, scalable images.
4. Start writing — from now on the page behaves like any other.

Large files are no problem: Gonk Note never loads a PDF in one piece, and only renders
the pages you actually insert at full resolution. Picking five pages out of a 600-page
PDF takes seconds.

To open a whole **DOCX or Markdown file as a new text document**, use
`File → Import document…` instead.

---

## 7. Whiteboard and text document

**Whiteboard** (`File → New whiteboard`) — the same tools as in a notebook, but instead
of pages an infinite surface with a dot grid. Use it for mind maps, sketches and
anything that does not fit on A4.

**Text document** (`File → New text document`) — a rich-text editor in a ribbon layout
(`Home`, `Insert`, `Layout`, `References`).

To get going:

1. Type some text and pick a style at the top left (Heading 1–4, Quote, …).
2. **Insert a table** via `Insert` — drag out a grid as in Word. When the caret is
   inside a table, the contextual tab **Table** appears with everything else (merging
   cells, sorting, formulas such as `=SUMME(ABOVE)`).
3. **Set up the page** via `Layout` → *Advanced settings*: format, orientation, margins
   in centimetres, headers/footers, watermark.
4. Switch **spell and grammar checking** between German and English in the status bar at
   the bottom; next to it are two switches, one per check. Misspelled words get a **red**
   wavy underline, grammar findings a **blue** one. Right-click an underlined word for
   corrections.

> **Both editions write.** The Linux edition shows a text document as typeset paper with
> tables, images, charts and a running head, page by page and with zoom — and lets you
> type, format, search and export in it. The differences between the editions are listed
> in the [README](../README.en.md#two-editions-one-app). **Spell checking, which was the
> biggest of them for a long time, has been there since 1.0.4** — with bundled
> dictionaries, so there is nothing to install first.
>
> **A document from the Windows edition** shows up under Linux only after being opened
> and saved there once. Until then the tab tells you what to do — and the contents are
> stored unchanged.

---

## 8. Exporting

1. `File → Export…` — or, for notebooks and whiteboards, the **Export** section in the
   settings sidebar (gear icon).
2. In the save dialog **the chosen file extension determines the format**:
   - Text document → `.pdf`, `.docx`, `.md`, `.png`
   - Notebook / whiteboard → `.pdf`, `.png`
3. Save. With PNG you get one file per page.

Exports are always "on paper": even in dark mode you get a light sheet with dark text.
If the original data for an image is missing, Gonk Note tells you after the export —
instead of quietly exporting at lower quality.

---

## 9. Backups — please set this up once

Gonk Note has no cloud. Your notes live exclusively on your machine, in **two** places
inside the data folder (**Help → About Gonk Note** shows it to you):

```
<data folder>/gonknote.sqlite    ← texts, strokes, structure
<data folder>/gonknote.blobs/    ← all images and imported pages
```

**A backup needs both — the file *and* the folder.** Copying only the `.sqlite` backs up
your notes without the images in them.

The simplest approach: copy the entire data folder somewhere regularly, ideally with the
app closed. To restore, copy it back to the same place.

> **Moving between Windows and Linux** works the same way: the same folder contents, just
> in the other location. The files have an identical layout on both systems.

> **Coming from an older version?** Up to version 0.2.0 the file was called
> `gonknote.db`. It is migrated once on the first start after that and then stays next to
> the new one, unchanged. From then on back up `gonknote.sqlite`; backing up the whole
> folder covers both anyway.

Incidentally, images no longer referenced by any document do not vanish immediately but
sit in `gonknote.papierkorb/` for 30 days. If such an image is needed again before that,
Gonk Note fetches it back by itself.

---

## 10. Your own stickers, covers and set square

Covers and the set square are included; **stickers deliberately are not**, for licensing
reasons — you supply those yourself. Your own files always take precedence over the
bundled ones:

| What | Where (inside the data folder) |
|---|---|
| Stickers (image stickers) | `Stickers/` — subfolders become separate groups |
| Notebook covers | `Covers/` — appear under "Individuell" |
| Set-square graphic | `Geodreieck-Light.svg` or `-Dark.svg` |
| Your own themes (colour schemes) | `Themes/*.json` — appear under "View → Theme" (section 12) |

Stickers and covers can also be uploaded conveniently via the **"+" tile** in the
respective tool; Gonk Note then copies them to the right place itself.

For the set square you place the file by hand. It has to be a 16 cm set square in a
viewBox of 2520 × 1680 with the midpoint of the hypotenuse at the centre — otherwise the
printed scale will not match snapping and rotating. If it is missing, the bundled graphic
applies; if that is missing too, Gonk Note draws a plain outline.

> **Everything in this section applies to both editions.** On Linux the folders live
> under `~/.config/GonkNote` instead of `%APPDATA%`.

---

## 11. Language and appearance

- **Language:** `View → Language → German / English`. Switches immediately, without a
  restart; your document names are left untouched.
- **Light and dark:** `Ctrl+T` toggles; `View → Theme` offers both for picking. Writing
  surfaces stay light by default — change the page shade in the settings if you want.

Both settings are remembered.

---

## 12. Your own themes — including with an AI

A theme in Gonk Note is nothing but a **list of up to twenty colours** — a JSON file,
not a program. It lives in `Themes/*.json` inside the data folder and appears under
`View → Theme` next to Light and Dark. **Applies to both editions.**

### By hand

1. `View → Theme → Save template…` — Gonk Note writes the currently active theme as a
   complete file (all twenty colours) into the `Themes/` folder and tells you where it
   is.
2. Open it in a text editor and change the colour values (`#RRGGBB`, `#AARRGGBB` or the
   short `#RGB`). `name` is what will appear in the menu.
3. The file shows up under `View → Theme` the next time you open the menu. To pick up a
   file from somewhere else use `Load your own…` — Gonk Note checks it and **copies** it
   into the folder; your original stays where it is.

**You do not have to list all twenty colours.** Whatever is missing comes from Light or
Dark — a file with three lines is a valid theme:

```json
{
  "name": "Sunset",
  "variant": "dark",
  "colors": {
    "Accent": "#FF6B35",
    "WindowBg": "#241019"
  }
}
```

Apart from the colours, `variant` is the one thing that is required: Gonk Note has to
know whether your theme is meant to be **light or dark**. More than a colour hangs on it
— the defaults of the drawing surface and, on Windows, the window title bar follow it,
and that is not something to guess.

> **Two things that come with it.** A theme can colour the **paper** as well (`PageBg`,
> `PageLine`, `PageGridDot`, `CanvasBg`, `DefaultInk`) — and that then shows up in
> **exports** too. And a file Gonk Note cannot read does not vanish: it sits greyed out
> in the menu, with a tooltip saying what is wrong with it.

### With an AI

You do not have to pick hex values yourself. Give the prompt below to an AI assistant
(ChatGPT, Claude, …), describe at the end what you want — and you get a finished
`theme.json` to drop into the `Themes/` folder.

````text
You create a theme file for the note-taking app "Gonk Note".

Output ONLY a valid JSON file, with no comment before or after it. Shape:

{
  "name":    "<display name in the menu>",
  "variant": "light" OR "dark"   (REQUIRED),
  "colors":  { "<colour name>": "<hex>", ... }
}

Hex values: "#RGB", "#RRGGBB" or "#AARRGGBB" (with alpha). Give all 20 colours.

The 20 colour names and their meaning:

Interface (15):
  WindowBg     window background and work area
  SidebarBg    sidebar and menu bar
  CardBg       cards and tiles on the window background
  ToolbarBg    toolbars above a document
  Border       dividers and outlines
  Text         foreground colour for body text and labels
  TextMuted    de-emphasised text: dates, hints, unemphasised icons
  Accent       accent colour — buttons, selection outlines, highlights
  AccentSoft   soft version of the accent colour, for fills
  Turquoise    turquoise; also the fallback for icon colours without their own
  Pink         pink
  Purple       purple
  Hover        surface under the pointer
  Pressed      surface while clicking
  Selection    selected entry in the tree and lists

The drawn sheet (5):
  CanvasBg     area beside the sheet (whiteboard canvas)
  PageBg       the paper itself
  PageLine     lines of a ruled page
  PageGridDot  dots of a squared page
  DefaultInk   default writing colour

Rules:
- "variant": "light"  -> light surfaces, dark text.
  "variant": "dark"   -> dark surfaces, light text.
- Text must be clearly readable on WindowBg, SidebarBg and CardBg (high contrast).
- Accent must stand out clearly on those surfaces; AccentSoft is the same colour,
  only paler/more transparent, for fills.
- Hover is a touch lighter/darker than the surface beneath it, Pressed a bit
  stronger, Selection clearly visible but not garish.
- PageBg is paper: normally keep it (near) white, unless the request explicitly asks
  for coloured paper. PageLine and PageGridDot are subtle variants of PageBg.
  DefaultInk contrasts strongly with PageBg. CanvasBg is the area next to it, a little
  darker/lighter than PageBg.
- Turquoise, Pink, Purple are accent dots for folder icons — vivid and clearly
  distinct from one another.

The request: <describe here — e.g. "a warm dark theme in browns and ambers, orange
accent" or "a light theme in the style of fresh green">
````

You do not have to check anything by hand: drop the file into the `Themes/` folder and
select it under `View → Theme`, and Gonk Note tells you at once if something is wrong
with it.

---

## 13. Cheat sheet

**Everywhere**

| Shortcut | Effect |
|---|---|
| `Ctrl+S` / `Ctrl+Shift+S` | Save / save all |
| `Ctrl+B` | Show/hide the sidebar |
| `Ctrl+T` | Toggle light/dark |
| `F2` / `Del` | Rename / delete (in the folder tree) |
| `Ctrl+Z` / `Ctrl+Y` | Undo / redo |

**Whiteboard and notebook**

| Shortcut | Effect |
|---|---|
| `S` `B` `M` `G` `E` | Pen · pencil · highlighter · shape pen · eraser |
| `T` `F` `N` | Text box · shapes · sticky note |
| `L` `V` `H` | Lasso · move · hand |
| `R` `D` | Ruler · set square |
| `Ctrl+C/X/V` · `Ctrl+D` · `Ctrl+A` | Copy/cut/paste · duplicate · select all |
| `Ctrl+mouse wheel` | Zoom |
| Right-click · second stylus button · long press | Quick menu |

**Touch:** 1 finger = pan · 2 fingers = zoom · three-finger double tap = undo.

---

## 14. When something goes wrong

**The app shows an error.** Unexpected errors end up as `fehler.log` in the data folder
and are reported once per session. That file is the first thing that belongs in a bug
report.

**The spell checker marks nothing.** *Windows edition:* the markings come from Windows,
not from Gonk Note. If the dictionary for a language is missing (typically English on a
German-only Windows), a warning triangle appears in the status bar. The fix: add the
language in the Windows settings. *Linux edition:* the dictionaries ship with the
program; if the switch is greyed out, the `Dictionaries` folder next to the program is
missing.

**Grammar checking only finds small things.** True, and not a bug: on its own Gonk Note
checks fixed rules — the same word twice, a space before a comma, a sentence starting in
lower case. Syntax and cases are beyond it. **For real grammar checking, run a
LanguageTool server on your own machine** (`languagetool --http`, port 8081); Gonk Note
finds it by itself and takes its findings. **Your machine only** — Gonk Note never sends
anything to a remote server, including the public languagetool.org service.

**OCR finds no text / reports missing language data.** The `tessdata` folder has to sit
next to `GonkNote.exe` — not in the data folder. The build puts it there itself; move
the program and you have to take it along. Flatpak and AppImage bring it with them
anyway.

**I want to test with a second, empty database.** Start it with `--db`:

```powershell
GonkNote.exe --db C:\path\to\test.sqlite                             # Windows
```
```bash
dotnet run --project src/GonkNote.Avalonia -- --db /tmp/test.sqlite  # Linux
```

Your real data stays untouched.

**I want to switch off the cleanup of unused images.** Set the `blob-cleanup` setting in
the database to `aus`. You normally do not need this — sorted-out images can be recovered
for 30 days.

More cases around installation and building are in
[Install](INSTALL.md#if-the-build-fails).

---

## 15. Updating Gonk Note

Gonk Note does **not** update itself — no updater, no internet connection. **Which
version is running** is shown by `Help → About Gonk Note`.

- **Ready-made download:** fetch the new file from
  [Releases](https://github.com/GONKstupid/GonkNote/releases) and replace the old one.
  With the Windows package, take the three folders (`Fonts`, `tessdata`, `Assets`)
  along.
- **Built from source:** in the project folder run `git pull`, then rebuild — exactly as
  the first time, see [README](../README.en.md#install) or
  [Install](INSTALL.md#building-from-source).

**Your data folder is never touched** — it lives elsewhere (section 1). Even so: make a
backup before updating (section 9), it costs ten seconds.

---

## And then?

- The full feature list is in the [README](../README.en.md).
- You will find the same texts inside the app under `Help`.
- Bugs and wishes belong in the
  [issues](https://github.com/GONKstupid/GonkNote/issues).

Enjoy your writing.
