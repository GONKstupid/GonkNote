# Gonk Note

A modern, offline-capable note-taking app for **Windows 11 and Linux** — an alternative to
GoodNotes with notebooks, whiteboards and text documents. Stylus-friendly (Wacom, Microsoft
Pen, …), no cloud, no installer, no admin rights.

> **New here?** The step-by-step guide
> **[Getting started](GETTING-STARTED.md)** takes you from cloning the repo to your first
> written, exported and backed-up note in about 10 minutes. This page describes *what*
> Gonk Note can do — the guide shows *how* to begin.

*(English version. The German original is `README.md`. Inside the app this page follows the
language you picked under View → Language.)*

## Two editions, one app

Gonk Note comes as a **Windows edition** (WPF) and a **Linux edition** (Avalonia). Both read
the same database, use the same core library and draw with the same renderer — a notebook
looks identical on either.

**The Windows edition is complete.** Everything on this page applies to it.

**The Linux edition can do today:** folder tree including drag & drop, pinning and
favourites, gallery, two languages, dark/light, the sliding title bar, help and about
dialog — and the canvas for **notebooks and whiteboards**: pen, pencil and highlighter with
pressure and tilt, precise erasing, lasso and move, page navigation, adding and deleting
pages, page settings (pattern, shade, size), zoom, touch gestures, undo and saving.

**What the Linux edition still lacks** — each with a reason, none of it forgotten:

| Missing | Why |
|---|---|
| **Text documents** (they open but cannot be edited) | The editor rests on a Windows building block. It is currently being replaced by an in-house document engine that runs everywhere |
| **Import and export** (DOCX, PDF, Markdown, PNG) | Depends on that same change |
| Shape pen, text boxes, sticky notes, stickers, numpad, quick-options menu, ruler and set square | Follow after the document engine |
| Text recognition (OCR) and spell checking | Need counterparts to the Windows services |

Nothing is lost along the way: whatever the Linux edition cannot display yet, it does not
touch either — a file you created on Windows comes back out unchanged.

## Features

- **Folder tree** with arbitrary nesting, drag & drop (move, hold `Ctrl` to copy),
  rename (`F2`), delete (`Del`), context menu, freely chosen icon colours — items and
  subfolders **inherit their folder's colour automatically** as long as they have none of their own
- **Pinning & favourites**: pinned folders appear in the sidebar's quick-access area;
  favourites are listed first inside their folder
- **Gallery start view** (when no document is open): the current folder's contents as large
  tiles (GoodNotes-style) – coloured folder icons, notebook covers as previews, cards for
  whiteboards and text documents, each with name, date and context menu. Selecting a folder in
  the tree or opening a folder tile navigates into it (breadcrumb + back)
- **Two languages**: the interface can be switched between **German** and **English** under
  **View → Language** — at runtime, without restarting. The choice is remembered. Your own
  document names stay untouched; only the interface changes.
- **Three document types**, each in its own tab:
  - **Notebook** — A4/A3 pages with a customisable cover (gradient, lettering or your own
    image; bundled cover templates in the categories "Basic", "Muster" and "Pixel Art" plus
    your own uploadable templates under "Individuell")
  - **Whiteboard** — an infinite canvas with a dot grid
  - **Text document** — a rich-text editor with a permanently light writing surface
- **Whiteboard tools** (SkiaSharp rendering; the default colour follows the page: black on
  light pages, white on dark ones):
  - Pen (pressure-sensitive), pencil, highlighter
  - **Shape pen** (`G`): recognises drawn shapes like GoodNotes does — straight lines
    (snapping to 45°), circles/ellipses, rectangles, polylines; otherwise the curve is smoothed
  - **Eraser** erases precisely: strokes are split at the point of contact, and the back of the
    stylus erases automatically. Its size is set with the size slider (or the number pad via a
    long press) and is remembered separately from the stroke width
  - **Selection** with two tools: **lasso** (`L`) encircles objects (only what is ~fully
    enclosed) and **move** (`V`) selects objects directly by clicking. Selected objects can be
    moved, **scaled** (corner handle) and **rotated** (rotation handle snapping to 15°) — for
    strokes, shapes, text, images and sticky notes
  - **Quick options menu** on the canvas (a floating icon bar in toolbar style: cut, copy,
    duplicate, paste, **recognise text (OCR)**, delete, select all) — opens via right-click,
    the **second stylus button**, a **long press** (finger or stylus, with lasso/move/hand) or
    automatically after a selection; entirely without the keyboard
  - **Stroke width via number pad**: a long press on the size slider (or a click on the value)
    opens a numpad for direct entry (modelled on Adobe Fresco)
  - **OCR** (text recognition, offline via Tesseract, German/English): recognises printed text
    in selected images or imported PDF pages; copy the result or insert it as a sticky note
  - Shapes (line, arrow, rectangle, ellipse, triangle) with fill colour and opacity —
    settings in the sidebar on the right
  - **Text boxes** with a choice of font, text and background colour (automatic contrast
    protection)
  - **Sticky notes** (coloured notes) and **stickers** (image stickers). Gonk Note deliberately
    ships no stickers for licensing reasons — put your own images into
    `%APPDATA%\GonkNote\Stickers` (or use the "+" tile in the sticker tool); subfolders appear
    as separate groups
  - **Drawing aids**: ruler (`R`) and set square (`D`), rotatable and snapping. The set square
    is a bundled vector graphic with millimetre and degree scales (one version each for the
    light and dark app theme). Your own drawing takes precedence: place it as
    `%APPDATA%\GonkNote\Geodreieck-Light.svg` or `-Dark.svg`
  - **Insert images**: toolbar button, `Ctrl+V` or drag & drop (PNG, JPEG, BMP, GIF, WebP,
    SVG); scalable proportionally with the corner handle
  - **Insert PDF & Word** (toolbar button or drag & drop): with a page selection dialog; in a
    notebook every page becomes a page of its own to write and highlight on (like GoodNotes),
    in a whiteboard the pages land as high-resolution, scalable images.
    **Very large PDFs too**: the file is never loaded in one piece, the selection shows fast
    thumbnails, and only the pages you actually insert are rendered at full resolution
    (picking five pages out of 600 takes seconds instead of minutes)
  - Undo/redo (`Ctrl+Z` / `Ctrl+Y`), zoom (`Ctrl+mouse wheel`), pan (middle mouse button,
    space bar, hand tool)
- **Touch gestures**: one finger pans the view, two fingers zoom (pinch) and pan, a
  three-finger double tap undoes
- **Settings sidebar on the right** (gear icon): page pattern and shade, format (A4/A3,
  portrait/landscape, template for new pages), shape options, text options, cover design and an
  **export section** (PDF/PNG straight from the sidebar) — changes take effect immediately
- **Text editor** in a ribbon layout (Home / Insert / Layout / References, plus the contextual
  tab **Table** when the caret sits inside a table):
  - Character and paragraph formatting, styles (Normal, Heading 1–4, Title, Quote,
    header/footer), format painter, lists with a style library, find & replace
  - **Advanced settings** (an expandable sidebar): page setup (A4/A5/A3/Letter,
    portrait/landscape, margins in cm including a worksheet template), paragraphs,
    headers/footers with placeholders, watermark, and table design/borders — each opened from
    its ribbon button
  - **Table of contents** from the headings, hyperlinks, special characters, captions
  - **Tables like in Word** (contextual tab "Table"): grid insert, text↔table, quick tables,
    rows/columns, merge cells (vertically too)/split, split table, autofit, sorting, formulas
    (`=SUMME(ABOVE)` …), table styles with header/total rows and banded rows/columns, borders
    and shading
  - **Charts** (column, bar, line, scatter, scatter+line, pie, radar — several series, colours
    extendable via "+" and removable via right-click)
  - Spell checking (German/English, switchable in the status bar; the chosen language applies
    to the whole document and is re-checked immediately) with correction suggestions.
    Note: the markings come from Windows – if no dictionary is installed for a language (English
    on a German-only Windows, say), a warning symbol appears and nothing is marked; the language
    can be added in the Windows settings. Ruler, status bar (words, zoom), heading navigator,
    page-break marks
- **Import**: images, PDF, DOCX and **Markdown (`.md`)** — DOCX/Markdown become new text documents
- **Export**: text document → PDF / DOCX / Markdown / PNG, whiteboard/notebook → PDF / PNG —
  via "File → Export" or the export section of the settings sidebar. If the original data for an
  image is missing, Gonk Note says so after the export instead of quietly exporting at lower
  quality
- **Dark/light mode** (`Ctrl+T`) for the app design — pages and writing surfaces stay light by
  default; the window title bar follows the theme (dark in dark mode). The sidebar collapses
  with `Ctrl+B`
- **Maximised window without a title bar**: when the window is maximised the title bar hides
  and the menu bar moves up. Move the mouse to the top edge and a title bar (minimise, restore,
  close) glides back in. A double-click on the menu bar (or the standard Windows commands)
  restores the window
- **Persistence**: a SQLite file `gonknote.sqlite` in the data folder for texts, strokes
  and structure; **images and imported PDF/Word pages live next to it** in
  `gonknote.blobs\` — one file per image. The data folder is `%APPDATA%\GonkNote` on
  Windows and `~/.config/GonkNote` on Linux; **Help → About Gonk Note** shows it. Autosave
  every 30 s, plus a save when closing tabs and the app.
  **For a backup, take both: the file *and* the folder.**
  Up to version 0.2.0 the file was called `gonknote.db` and was a LiteDB file. It is
  **migrated once** on the first start after that and then stays next to the new one,
  unchanged — a way back for as long as you keep it.
  **From now on back up `gonknote.sqlite`, no longer `gonknote.db`:** the old file no longer
  grows with your work and would soon be an outdated state.
  Images no longer referenced by any document move to `gonknote.papierkorb\` and are only
  removed for good after 30 days; if an image is needed again before that, Gonk Note fetches it
  back by itself
- **Large documents**: originals are stored untouched and written back untouched on export; what
  you see is a downscaled derivative. A Word document with photos therefore comes out exactly as
  large as it went in (previously eight times as large), and a notebook with 120 imported pages
  (118 MB) can be saved and opened. Memory stays flat during a PDF import: 530 MB of rendered
  pages pass through with a peak of about 114 MB. Several hundred pages are no problem in the
  text editor — a 500-page document opens in roughly 1.8 seconds
- **Memory use**: around 180 MB after startup, about 290 MB with a notebook open — regardless of
  how large the document is, because only the currently visible pages are held in memory (budget
  96 MB). Closing a tab releases memory; the undo history is capped at 200 steps so it does not
  grow during long sessions

### Keyboard shortcuts in the whiteboard

| Key | Tool |
|---|---|
| `S` | Pen |
| `G` | Shape pen |
| `B` | Pencil |
| `M` | Highlighter |
| `E` | Eraser |
| `V` | Move (click objects) |
| `L` | Lasso |
| `T` | Text box |
| `F` | Shapes |
| `N` | Sticky note |
| `R` | Ruler |
| `D` | Set square |
| `H` | Hand (pan the canvas) |

Selection: move, scale, rotate · `Ctrl+C/X/V` copy/cut/paste · `Ctrl+D` duplicate ·
`Ctrl+A` select all · `Del` delete · right-click, the second stylus button or a long press
opens the quick options menu.

## Build

Requirement: .NET SDK 10 or newer.

**Always build per project, never the whole solution.** It contains both editions, and the
Windows edition cannot be compiled on Linux — that is intended, not a fault.

### Windows

```powershell
# Development
dotnet run --project src/GonkNote.Wpf

# Single-file exe (self-contained, no .NET installation needed)
dotnet publish src/GonkNote.Wpf -c Release
# Result: src/GonkNote.Wpf/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/GonkNote.exe
```

Note: WPF does not support assembly trimming (`PublishTrimmed`); the exe is compressed
instead (`EnableCompressionInSingleFile`).

### Linux

```bash
dotnet run --project src/GonkNote.Avalonia
```

The system needs **fontconfig and at least one font** for this — without them every drawn
piece of text stays blank. On Arch-like systems:

```bash
sudo pacman -S fontconfig ttf-dejavu
```

Under Wayland, Gonk Note runs through XWayland; the toolkit has no native Wayland path.
Stylus pressure and tilt arrive in full through it.

### Both

For testing, `--db <path>` uses an alternative database — **work on a copy, never on your
real data.**

What has to sit next to the program (`tessdata` for text recognition on Windows) and how to
carry on from there is described in [Getting started](GETTING-STARTED.md).

## Architecture

| Building block | Technology |
|---|---|
| Windows interface | WPF (.NET 10), MVVM, dynamic theme resource dictionaries |
| Linux interface | Avalonia 12 (.NET 10), the same view models, colours from a table in `GonkNote.Core` |
| Whiteboard rendering | SkiaSharp — via `SKElement` on Windows, via Avalonia's own Skia canvas on Linux; **same renderer, same pixels** |
| Stylus input | WPF stylus events resp. Avalonia pointers, both with pressure and tilt |
| Persistence | SQLite (`Microsoft.Data.Sqlite`); documents as JSON, read and written via a source generator |
| Core logic | a separate library `GonkNote.Core` (net10.0) — free of UI dependencies |

The application consists of **one core and two interfaces**. Data model, persistence,
drawing routines, colours and translations live in the core; an interface only holds what
draws pixels or accepts input. That is why the Linux edition exists at all — it rebuilt
none of it.

```
src/
├─ GonkNote.Core/            Core logic without UI ties (net10.0), namespace GonkNote.Core.*
│  ├─ Models/               NoteItem (tree), whiteboard elements, enums
│  ├─ Platform/             the seam to the interfaces: file dialogs, clipboard, theme,
│  │                        OCR, spell checking … as interfaces
│  ├─ Services/             DatabaseService (SQLite), BlobStore (images/PDFs next to the
│  │                        database), UndoStack, ImageCache, PDF import
│  ├─ Rendering/            Skia drawing routines of the whiteboard, set-square overlay
│  ├─ Editing/              Precise erasing, hit testing and lasso
│  ├─ Text/                 Markdown parser for the bundled documents
│  ├─ Theming/              the colour table: a theme is 20 named colours
│  └─ Localization/         Loc (lookup) + one table each for DE/EN
│
├─ GonkNote.ViewModels/      MainViewModel, tab VMs, tree VM, MVVM base (net10.0) —
│                            used by both interfaces
│
├─ GonkNote.Legacy/          Reads databases up to version 0.2.0 (LiteDB); the only place
│                            in the project that still knows that package
│
├─ GonkNote.Wpf/             Windows interface (net10.0-windows)
│  ├─ Platform/             the implementations behind Core/Platform
│  ├─ Views/                WhiteboardView and TextEditorView — both split into partial
│  │                        files by topic, plus the dialogs
│  ├─ Services/             Import/export (DOCX, PDF, Markdown), OCR, text styles
│  └─ Themes/               Light.xaml, Dark.xaml, Styles.xaml
│
└─ GonkNote.Avalonia/        Linux interface (net10.0, also runs on Windows)
   ├─ Platform/             the same interfaces, implemented for Avalonia
   ├─ Views/                WhiteboardView (input, rendering, settings), dialogs,
   │                        Markdown display
   └─ Themes/Styles.axaml   Shape and vector icons — the colours come from the core
```

## Licence

Gonk Note is licensed under the **MIT licence** — see [LICENSE](LICENSE).
Copyright © 2026 Manuel Toegel.

In short: use, modify and redistribute freely, including commercially; the licence text and the
copyright notice must be included, and there is no warranty.

The bundled **notebook covers** (`Assets/Covers/**`), the **set-square graphics**
(`Assets/Geodreieck-Light.svg`, `-Dark.svg`) and the **app icon** are original works and fall
under the same licence (see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)).

**Gonk Note deliberately ships no stickers** — the tool only works with images you place in
`%APPDATA%\GonkNote\Stickers` yourself.

### Libraries used

All dependencies are permissively licensed and compatible with the MIT licence. The notices that
Apache-2.0 and BSD-3 require on redistribution (particularly for the single-file exe) are in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md):

| Building block | Purpose | Licence |
|---|---|---|
| [SQLite](https://www.sqlite.org/) via [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/) | Persistence | Public domain / MIT |
| [LiteDB](https://www.litedb.org/) | reads databases up to version 0.2.0 | MIT |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | Whiteboard rendering | MIT |
| [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) | SVG rasterisation | MIT |
| [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK) | DOCX import/export | MIT |
| [Docnet.Core](https://github.com/GowenGit/docnet) / PDFium | PDF import | MIT / BSD-3-Clause |
| [Tesseract](https://github.com/charlesw/tesseract) + `tessdata` (deu, eng) | OCR | Apache-2.0 |
