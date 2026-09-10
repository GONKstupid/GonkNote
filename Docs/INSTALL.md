# Installing Gonk Note

This page gets Gonk Note onto your machine — and nothing else. Once it runs and you
want to know *how* to work with it, carry on in
[Getting started](GETTING-STARTED.md).

*(English version. The German original is `INSTALLIEREN.md`.)*

---

## Contents

1. [Requirements](#requirements)
2. [Route 1 — Windows 11 (ready-made download)](#route-1--windows-11-ready-made-download)
3. [Route 2 — Linux, AppImage](#route-2--linux-appimage)
4. [Route 3 — Linux, Flatpak](#route-3--linux-flatpak)
5. [Building from source](#building-from-source)
6. [Where your data lives · Uninstalling](#where-your-data-lives--uninstalling)
7. [If the build fails](#if-the-build-fails)

---

## Requirements

- **Windows 11** (Windows 10 should work too but is untested) **or Linux**
- **No admin rights**, no registry entries, no cloud

The ready-made downloads (routes 1 and 2) run **without an installed .NET**. Only
building from source needs the
[.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) or newer.

All downloads are under
**[Releases](https://github.com/GONKstupid/GonkNote/releases)**.

---

## Route 1 — Windows 11 (ready-made download)

1. Download `GonkNote-<version>-windows-x64.zip` and unpack it.
2. Run `GonkNote.exe`.

> ⚠ **Keep the folder together.** Three folders sit next to the exe and each is needed:
>
> | Folder | For | Without it … |
> |---|---|---|
> | `Fonts` | interface and document fonts | Gonk Note **still starts** and draws everything in the Windows system font — **with no hint** |
> | `tessdata` | text-recognition language data | everything works except OCR |
> | `Assets` | cover templates, set square | the bundled covers and set square are missing |
>
> If you move the exe, take the three folders along.

---

## Route 2 — Linux, AppImage

One file, no dependencies, no sandbox:

```bash
chmod +x GonkNote-<version>-x86_64.AppImage
./GonkNote-<version>-x86_64.AppImage
```

Your system needs **fontconfig and at least one font** — without them every drawn piece
of text stays blank. On Arch-like systems (CachyOS, Manjaro, EndeavourOS …):

```bash
sudo pacman -S fontconfig ttf-dejavu
```

On Debian-like ones (Ubuntu, Mint …) the packages are called `libfontconfig1` and
`fonts-dejavu-core`.

That is the only prerequisite. Everything else is inside the image, **including text
recognition**: it works on a machine where no Tesseract is installed at all.

---

## Route 3 — Linux, Flatpak

Sandbox and software centre, intended as the main channel. **Gonk Note is not on Flathub
yet**, though — until then you build the package yourself. The prerequisites (runtime
and SDK from Flathub) and the two commands are in
[packaging/LIESMICH.md](../packaging/LIESMICH.md) (German):

```bash
cd packaging/flatpak && ./bauen.sh
flatpak run io.github.gonkstupid.GonkNote
```

---

## Building from source

The route for anyone who wants to contribute or run the latest state. Requirement:
[.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) or newer.

```bash
git clone https://github.com/GONKstupid/GonkNote.git
cd GonkNote
```

> **Always build per project, never the whole solution.** It contains both editions,
> and the Windows edition cannot be compiled on Linux — that is intended.

**Windows:**

```powershell
# Just to try it, straight from the source
dotnet run --project src/GonkNote.Wpf

# Single-file exe (runs without an installed .NET, moves anywhere)
dotnet publish src/GonkNote.Wpf -c Release
# Result: src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\GonkNote.exe
```

**Copy the whole folder contents**, not just the exe — `Fonts`, `tessdata` and `Assets`
belong with it (see the table under route 1).

**Linux:**

```bash
dotnet run --project src/GonkNote.Avalonia
```

Your system needs **fontconfig and at least one font** (see route 2). The first run
downloads the packages and takes a few minutes.

Under Wayland, Gonk Note runs through XWayland; stylus pressure and tilt arrive in full
through it.

---

## Where your data lives · Uninstalling

On first start Gonk Note quietly creates a folder — **`%APPDATA%\GonkNote` on Windows,
`~/.config/GonkNote` on Linux**:

```
<data folder>/
├─ gonknote.sqlite      your texts, strokes and the folder structure
├─ gonknote.blobs/      images plus imported PDF and Word pages
└─ gonknote.papierkorb/ images nobody currently needs (30-day grace period)
```

Nothing is sent to the internet, nothing is written to the registry, nothing is
installed. **To uninstall, the program and that folder are all there is.** The path is
shown to you any time under **Help → About Gonk Note**.

---

## If the build fails

| Message | Cause |
|---|---|
| Access denied / file in use | Gonk Note is still running — close it and build again |
| `NETSDK1045` or similar about the framework version | .NET SDK too old — install the [current SDK 10+](https://dotnet.microsoft.com/download/dotnet/10.0) |
| `net10.0-windows…` cannot be resolved (Linux) | You are building the Windows edition or the whole solution — use `src/GonkNote.Avalonia` |
| Merge conflict on `git pull` | you have local changes — `git stash`, then `git pull` again |
| On Linux every drawn piece of text stays blank | `fontconfig` or a font is missing — see route 2 |

---

**Next:** [Getting started with Gonk Note](GETTING-STARTED.md) · [Feature overview in the README](../README.en.md)
