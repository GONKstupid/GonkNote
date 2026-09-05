# Getting started with Gonk Note

This guide takes you from an empty machine to your first written, exported and backed-up
note. Allow about **10 minutes**.

If you want to know *what* Gonk Note can do, read the
[feature overview in the README](README.en.md). This page is about *how* to begin.

*(English version. The German original is `ERSTE-SCHRITTE.md`. Inside the app this guide
follows the language you picked under View → Language.)*

---

## Contents

1. [Getting Gonk Note onto your machine](#1-getting-gonk-note-onto-your-machine)
2. [The first start](#2-the-first-start)
3. [The window in 30 seconds](#3-the-window-in-30-seconds)
4. [Your first notebook](#4-your-first-notebook)
5. [Writing and drawing](#5-writing-and-drawing)
6. [Selecting and changing things](#6-selecting-and-changing-things)
7. [Annotating a PDF or Word document](#7-annotating-a-pdf-or-word-document)
8. [Whiteboard and text document](#8-whiteboard-and-text-document)
9. [Exporting](#9-exporting)
10. [Backups — please set this up once](#10-backups--please-set-this-up-once)
11. [Your own stickers, covers and set square](#11-your-own-stickers-covers-and-set-square)
12. [Language and appearance](#12-language-and-appearance)
13. [Cheat sheet](#13-cheat-sheet)
14. [Updating to a new version](#14-updating-to-a-new-version)
15. [When something goes wrong](#15-when-something-goes-wrong)

---

## 1. Getting Gonk Note onto your machine

**Requirements:** **Windows 11** (Windows 10 should work too but is untested) **or Linux**,
plus the [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) or newer. You do
not need admin rights.

> **Gonk Note comes in two editions** — one for Windows and one for Linux. Both read the
> same files, can do the same things and look almost identical. **The few differences that
> remain** are listed by name in the
> [README](README.en.md#two-editions-one-app) — the biggest is **spell checking**, which
> does not exist on Linux yet.

> **The quickest route is a ready-made download.**
> **[Releases](https://github.com/GONKstupid/GonkNote/releases)** carries the Windows exe and
> the Linux AppImage — both run **without an installed .NET**, so you need none of what
> follows below. The three ways are described in the [README](README.en.md#install).
> **This guide builds the program from source** — the route for anyone who wants to
> contribute or run the latest state.

**Step by step:**

1. Clone the repository:

   ```bash
   git clone https://github.com/GONKstupid/GonkNote.git
   cd GonkNote
   ```

2. Build — **a different project per edition**. Never build the whole solution: it contains
   both, and the Windows edition cannot be compiled on Linux.

   **Windows:**

   ```powershell
   dotnet publish src/GonkNote.Wpf -c Release
   ```

   The result is a **single file** that runs without an installed .NET and can be moved
   anywhere:

   ```
   src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\GonkNote.exe
   ```

   Copy it wherever you want it — **together with the `tessdata` folder** and, if present,
   the `Assets` folder from the same directory. `tessdata` holds the language data for text
   recognition; without it everything works except OCR.

   **Linux:**

   ```bash
   dotnet run --project src/GonkNote.Avalonia
   ```

   Your system needs **fontconfig and at least one font** for this — otherwise every drawn
   piece of text stays blank. On Arch-like systems (CachyOS, Manjaro, EndeavourOS …):

   ```bash
   sudo pacman -S fontconfig ttf-dejavu
   ```

   On Debian-like ones (Ubuntu, Mint …) the packages are called `libfontconfig1` and
   `fonts-dejavu-core`.

The first run downloads the packages and takes a few minutes.

**Just to try it out** — no finished file, straight from the source:

```bash
dotnet run --project src/GonkNote.Wpf        # Windows
dotnet run --project src/GonkNote.Avalonia   # Linux
```

---

## 2. The first start

Windows: double-click `GonkNote.exe`. Linux: the `dotnet run` command above.

On the first start Gonk Note quietly creates a folder — **`%APPDATA%\GonkNote` on Windows,
`~/.config/GonkNote` on Linux**:

```
<data folder>/
├─ gonknote.sqlite      your texts, strokes and the folder structure
├─ gonknote.blobs/      images plus imported PDF and Word pages
└─ gonknote.papierkorb/ images nobody currently needs (30-day grace period)
```

Nothing is sent to the internet, nothing is written to the registry, nothing is installed. If
you want to get rid of Gonk Note again, the program and this folder are all there is.

> **Remember this path.** It is also your backup — see
> [section 10](#10-backups--please-set-this-up-once). You do not have to memorise it:
> **Help → About Gonk Note** shows it.

---

## 3. The window in 30 seconds

| Where | What |
|---|---|
| **Menu bar at the top** | `File`, `View`, `Help` |
| **Sidebar on the left** | your folder tree, four buttons for new items at the top, quick access ("PINNED") above that |
| **Middle** | the **gallery** — as long as nothing is open you see the current folder as large tiles |
| **Tabs** | every open document gets its own tab |

If the sidebar is in the way: `Ctrl+B`.

When you maximise the window the title bar disappears. It glides back in as soon as you move
the mouse to the top edge of the window.

---

## 4. Your first notebook

1. **Create** — `File → New notebook`, or the "New" button above the gallery, or the notebook
   button at the top of the sidebar.
2. **Name it** — the name can be changed at any time with `F2`.
3. **Open** — double-click the tile in the gallery or the entry in the tree. The notebook
   opens in a tab of its own.
4. **Turn pages** — the notebook's page bar sits at the bottom centre:
   `◀  Page 1 / 1  ▶` and next to it **New page** (`+`) and **Delete page**.
5. **Change the look** — the **gear icon** on the right of the toolbar opens the settings.
   Under **Page** you choose the pattern (blank, ruled, squared, dotted), the shade and the
   format (A4/A3, portrait/landscape). With *"Set as default for new pages"* the choice also
   applies to everything you create afterwards.
6. **Set a cover** — the **Cover** section in the same sidebar: gradient, lettering or an
   image. The categories "Basic", "Muster" and "Pixel Art" are included; under **Individuell**
   you upload your own images via the "+" tile.

**Saving happens by itself** — every 30 seconds, when closing the tab and when quitting.
`Ctrl+S` still works if it makes you feel better.

---

## 5. Writing and drawing

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

- **Setting the stroke width exactly:** press and hold the size slider (or the icon next to
  it) — a number pad opens for direct entry. The eraser remembers its own size.
- **Zoom and pan:** `Ctrl+mouse wheel` zooms; panning works with the middle mouse button, a
  held space bar or the hand tool. On a touchscreen: one finger pans, two fingers zoom.
- **Made a mistake?** `Ctrl+Z`. A double tap with three fingers does the same.

**With a stylus:** the back of the pen erases automatically. The second stylus button opens the
quick menu (see the next section).

---

## 6. Selecting and changing things

1. **Select** — either `L` (lasso) and encircle the object, or `V` (move) and click the object
   directly. The lasso only takes what you have enclosed more or less completely.
2. **Change** — drag to move, corner handle to scale, rotation handle to rotate (snapping every
   15°). This applies equally to strokes, shapes, text, images and sticky notes.
3. **Quick menu** — after a selection a small icon bar appears automatically: cut, copy,
   duplicate, paste, **recognise text (OCR)**, delete, select all.

   You also get it via **right-click**, the **second stylus button**, or by pressing and
   holding for about half a second with lasso/move/hand — so you can work entirely without a
   keyboard.

**Getting text out of an image:** select the image → quick menu → *Recognise text (OCR)*.
Recognition runs offline (German and English). You can copy the result or insert it directly as
a sticky note.

---

## 7. Annotating a PDF or Word document

The typical case: marking up lecture notes or a worksheet.

1. Click **Insert file** in the toolbar — or simply drag the file into the window, or press
   `Ctrl+V`.
2. For PDF and Word a **page selection dialog** with thumbnails appears. Pick what you need.
3. Confirm:
   - In a **notebook** every selected page becomes a page of its own to write on.
   - In a **whiteboard** the pages land as high-resolution, scalable images.
4. Start writing — from now on the page behaves like any other.

Large files are no problem: Gonk Note never loads a PDF in one piece, and only renders the pages
you actually insert at full resolution. Picking five pages out of a 600-page PDF takes seconds.

To open a whole **DOCX or Markdown file as a new text document**, use
`File → Import document…` instead.

---

## 8. Whiteboard and text document

**Whiteboard** (`File → New whiteboard`) — the same tools as in a notebook, but instead of
pages an infinite surface with a dot grid. Use it for mind maps, sketches and anything that
does not fit on A4.

**Text document** (`File → New text document`) — a rich-text editor in a ribbon layout
(`Home`, `Insert`, `Layout`, `References`).

> **Both editions write.** The Linux edition shows a text document as typeset paper with
> tables, images, charts and a running head, page by page and with zoom — and lets you type,
> format, search and export in it. **Two differences remain:** there is no **spell checking**
> there yet, and **composed characters** (`´` then `e` for `é`) do not arrive — that is
> Avalonia's doing, not Gonk Note's. Umlauts are unaffected.
>
> In return the **Linux** edition shows **page numbers** that the Windows edition does not
> have: it typesets real pages, while Windows merely flows the text.
>
> **A document from the Windows edition** shows up under Linux only after being opened and
> saved there once: its old format is readable on Windows only. Until then the tab tells you
> what to do — and the contents are stored unchanged.

To get going:

1. Type some text and pick a style at the top left (Heading 1–4, Quote, …).
2. **Insert a table** via `Insert` — drag out a grid as in Word. When the caret is inside a
   table, the contextual tab **Table** appears with everything else (merging cells, sorting,
   formulas such as `=SUMME(ABOVE)`).
3. **Set up the page** via `Layout` → *Advanced settings*: format, orientation, margins in
   centimetres, headers/footers, watermark.
4. Switch the **spell checker** between German and English in the status bar at the bottom.

---

## 9. Exporting

1. `File → Export…` — or, for notebooks and whiteboards, the **Export** section in the settings
   sidebar (gear icon).
2. In the save dialog **the chosen file extension determines the format**:
   - Text document → `.pdf`, `.docx`, `.md`, `.png`
   - Notebook / whiteboard → `.pdf`, `.png`
3. Save. With PNG you get one file per page.

Exports are always "on paper": even in dark mode you get a light sheet with dark text. If the
original data for an image is missing, Gonk Note tells you after the export — instead of quietly
exporting at lower quality.

> **The same in both editions.** A text document goes out in all four formats — the
> **Export** button sits in the document's ribbon and preselects the format. A whiteboard or
> notebook goes out in both editions too; they compute the same path for it.

---

## 10. Backups — please set this up once

Gonk Note has no cloud. Your notes live exclusively on your machine, in **two** places
inside the data folder (Windows: `%APPDATA%\GonkNote`, Linux: `~/.config/GonkNote` —
**Help → About Gonk Note** shows it to you):

```
<data folder>/gonknote.sqlite    ← texts, strokes, structure
<data folder>/gonknote.blobs/    ← all images and imported pages
```

**A backup needs both — the file *and* the folder.** Copying only the `.sqlite` backs up your
notes without the images in them.

> **Coming from an older version?** Up to version 0.2.0 the file was called `gonknote.db`. It is
> migrated once on the first start after that and then stays next to the new one, unchanged — a
> way back for as long as you keep it.
> **From then on back up `gonknote.sqlite`:** the old file no longer grows with your work and is
> soon an outdated state. Backing up the whole folder (next paragraph) covers both anyway.

The simplest approach: copy the entire data folder somewhere regularly, ideally with the app
closed. To restore, copy it back to the same place.

> **Moving between Windows and Linux** works the same way: the same folder contents, just in
> the other location. The files have an identical layout on both systems.

Incidentally, images no longer referenced by any document do not vanish immediately but sit in
`gonknote.papierkorb\` for 30 days. If such an image is needed again before that, Gonk Note
fetches it back by itself.

---

## 11. Your own stickers, covers and set square

Covers and the set square are included; **stickers deliberately are not**, for licensing
reasons — you supply those yourself. Your own files always take precedence over the bundled
ones:

| What | Where (inside the data folder) |
|---|---|
| Stickers (image stickers) | `Stickers/` — subfolders become separate groups |
| Notebook covers | `Covers/` — appear under "Individuell" |
| Set-square graphic | `Geodreieck-Light.svg` or `-Dark.svg` |

> **This section applies to both editions.** Stickers, your own cover templates and the set
> square are read from the same folders on Linux — they just live under
> `~/.config/GonkNote` instead of `%APPDATA%`. The about dialog tells you exactly where.

Stickers and covers can also be uploaded conveniently via the **"+" tile** in the respective
tool; Gonk Note then copies them to the right place itself.

For the set square you place the file by hand. It has to be a 16 cm set square in a viewBox of
2520 × 1680 with the midpoint of the hypotenuse at the centre — otherwise the printed scale will
not match snapping and rotating. If it is missing, the bundled graphic applies; if that is
missing too, Gonk Note draws a plain outline.

---

## 12. Language and appearance

- **Language:** `View → Language → German / English`. Switches immediately, without a restart;
  your document names are left untouched.
- **Dark/light mode:** `Ctrl+T` or `View → Toggle dark/light mode`. Writing surfaces stay light
  by default — change the page shade in the settings if you want.

Both settings are remembered.

---

## 13. Cheat sheet

**Everywhere**

| Shortcut | Effect |
|---|---|
| `Ctrl+S` / `Ctrl+Shift+S` | Save / save all |
| `Ctrl+B` | Show/hide the sidebar |
| `Ctrl+T` | Dark/light mode |
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

## 14. Updating to a new version

Gonk Note does **not** update itself — there is no updater and no internet connection. You fetch
the new state and rebuild. It takes less than a minute.

**Which version is running?** `Help → About Gonk Note` shows it at the top
(e.g. "Version 1.0.0 · Windows and Linux").

> **If you use a ready-made download**, this section does not apply: just fetch the new file
> from [Releases](https://github.com/GONKstupid/GonkNote/releases) and replace the old one.
> **Your data folder is not touched** — it lives elsewhere (section 2).

**Step by step:**

1. **Close Gonk Note.** A running exe cannot be overwritten; otherwise the build fails with
   "access denied".
2. **Fetch the new state** — in the project folder:

   ```bash
   git pull
   ```

3. **Rebuild** — and here it matters what you start:

   | You start… | Command | Result is in |
   |---|---|---|
   | the Start menu shortcut (Windows) | `dotnet build src/GonkNote.Wpf -c Release` | `src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\` |
   | a copied single-file exe (Windows) | `dotnet publish src/GonkNote.Wpf -c Release` | `…\win-x64\publish\` |
   | the Linux edition | `dotnet build src/GonkNote.Avalonia -c Release` | `src/GonkNote.Avalonia/bin/Release/net10.0/` |

   With the second route you then have to copy the new `GonkNote.exe` back to wherever your old
   one was — **together with the `Assets` and `tessdata` folders** if those have changed.

   **Never `dotnet build` without naming a project.** That builds the whole solution, which
   contains both editions — on Linux it is bound to fail on the Windows one.

4. **Start it and check `Help → About Gonk Note`** to see whether the new version is in place.

**Your notes are untouched by all this.** They live in the data folder and have nothing to do
with the program folder — you can replace or even delete the program safely. Even so: **make a
backup before updating** (see [section 10](#10-backups--please-set-this-up-once)), it costs ten
seconds.

**If the build fails:**

| Message | Cause |
|---|---|
| Access denied / file in use | Gonk Note is still running — close it and build again |
| `NETSDK1045` or similar about the framework version | .NET SDK too old — install the [current SDK 10+](https://dotnet.microsoft.com/download/dotnet/10.0) |
| `net10.0-windows…` cannot be resolved (Linux) | You are building the Windows edition or the whole solution — use `src/GonkNote.Avalonia` |
| Merge conflict on `git pull` | you have local changes — `git stash`, then `git pull` again |

**For you as a developer only:** the version number itself lives in `Directory.Build.props`
(`<Version>`) and applies to both editions; the phase label next to it comes from the
translation key `About.Version` (both language tables in `src/GonkNote.Core/Localization/`).
Both are maintained by hand.

**Want to go back a version?** Every state is in the Git history: `git log --oneline` lists them,
`git checkout <commit>` fetches one, `git checkout main` brings you back. Rebuild afterwards
either way.

---

## 15. When something goes wrong

**The app shows an error.** Unexpected errors end up as `fehler.log` in the data folder and are
reported once per session. That file is the first thing that belongs in a bug report.

**The spell checker marks nothing.** The markings come from Windows, not from Gonk Note. If the
dictionary for a language is missing (typically English on a German-only Windows), a warning
triangle appears in the status bar. The fix: add the language in the Windows settings.
**The Linux edition has no spell checking yet** — it is the first item to be added after the
port.

**OCR finds no text / reports missing language data.** The `tessdata` folder has to sit next to
`GonkNote.exe` (see [section 1](#1-getting-gonk-note-onto-your-machine)). **This applies to
both editions:** `tessdata` belongs next to the program, not in the data folder. The build
puts it there itself; move the program and you have to take it along. Flatpak and AppImage
bring it with them anyway.

**On Linux every drawn piece of text stays blank.** Then `fontconfig` or a font is missing —
see [section 1](#1-getting-gonk-note-onto-your-machine).

**I want to test with a second, empty database.** Start it with

```powershell
GonkNote.exe --db C:\path\to\test.sqlite                          # Windows
```
```bash
dotnet run --project src/GonkNote.Avalonia -- --db /tmp/test.sqlite   # Linux
```

Your real data stays untouched.

**I want to switch off the cleanup of unused images.** Set the `blob-cleanup` setting in the
database to `aus`. You normally do not need this — sorted-out images can be recovered for 30
days.

---

## And then?

- The full feature list is in the [README](README.en.md).
- You will find the same texts inside the app under `Help → About Gonk Note`.
- Bugs and wishes belong in the
  [issues](https://github.com/GONKstupid/GonkNote/issues).

Enjoy your writing.
