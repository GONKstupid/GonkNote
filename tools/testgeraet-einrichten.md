# Testgerät einrichten (Windows 11)

Frisches Windows 11 zum Gonk-Note-Testgerät machen. Wegwerf-Werkzeug, kein Produktivcode.

**So geht's:** PowerShell öffnen (normal oder als Admin, beides geht), den ganzen Block unten
kopieren und einfügen, Enter. Er läuft als ein Stück durch. Mehrfach ausführen ist harmlos:
winget überspringt, was schon da ist.

> Falls `winget` fehlt oder meckert: im Microsoft Store „App-Installer“ aktualisieren.

```powershell
& {
$ProgressPreference = 'SilentlyContinue'   # macht Invoke-WebRequest in PS 5.1 viel schneller

$pakete = @(
    # Bauen & Testen
    'Git.Git'
    'GitHub.cli'
    'Microsoft.DotNet.SDK.10'          # net10.0 + net10.0-windows (WPF-Kopf)
    'EclipseAdoptium.Temurin.21.JRE'   # fuer den LanguageTool-Server (Grammatik)
    'Python.Python.3.13'
    'OpenJS.NodeJS.LTS'
    # Shell & Editor wie auf dem Linux-Rechner
    'Microsoft.PowerShell'
    'Microsoft.WindowsTerminal'
    'ZedIndustries.Zed'                # Standard-Editor
    'JesseDuffield.lazygit'
    'BurntSushi.ripgrep.MSVC'
    'junegunn.fzf'
    'sharkdp.bat'
    'eza-community.eza'
    'ajeetdsouza.zoxide'
    'Starship.Starship'
    'Fastfetch-cli.Fastfetch'
    'aristocratos.btop4win'
    'astral-sh.uv'
    '7zip.7zip'
    'DEVCOM.JetBrainsMonoNerdFont'
    # Browser
    'Zen-Team.Zen-Browser'
)

winget source update | Out-Null
$fehler = @()
foreach ($id in $pakete) {
    Write-Host "==> $id" -ForegroundColor Cyan
    winget install --id $id -e --silent --accept-package-agreements --accept-source-agreements
    # -1978335189 = schon installiert, kein Update noetig
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne -1978335189) { $fehler += $id }
}

# PATH neu laden, damit git/dotnet/java in dieser Sitzung gefunden werden
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
            [Environment]::GetEnvironmentVariable('Path', 'User')

git config --global core.editor 'zed --wait'

# Claude Code (nativer Installer, braucht Git for Windows von oben)
if (-not (Get-Command claude -ErrorAction SilentlyContinue)) {
    Invoke-RestMethod https://claude.ai/install.ps1 | Invoke-Expression
}

# LanguageTool-Server (nicht in winget). TdLanguageTool erwartet http://localhost:8081.
$lt = "$HOME\LanguageTool"
if (-not (Test-Path $lt)) {
    $zip = "$env:TEMP\languagetool.zip"
    Invoke-WebRequest https://languagetool.org/download/LanguageTool-stable.zip -OutFile $zip
    Expand-Archive $zip "$env:TEMP\lt" -Force
    Move-Item (Get-ChildItem "$env:TEMP\lt" -Directory | Select-Object -First 1).FullName $lt
    Remove-Item $zip, "$env:TEMP\lt" -Recurse -Force
    Set-Content "$lt\start.cmd" "@java -cp `"%~dp0languagetool-server.jar`" org.languagetool.server.HTTPServer --port 8081"
}

# Repo holen, bauen, testen -- wie CI (ci.yml, Job windows)
$repo = "$HOME\Projects\gonk-note-V2"
if (-not (Test-Path $repo)) {
    git clone https://github.com/GONKstupid/GonkNote.git $repo
}
# Als Admin geklont gehoert der Ordner "Administratoren" -- ohne das meckert git spaeter
# in der normalen Shell ueber "dubious ownership".
git config --global --add safe.directory ($repo -replace '\\', '/')
Push-Location $repo
dotnet build GonkNote.slnx -c Release -warnaserror
if ($LASTEXITCODE -eq 0) { dotnet test GonkNote.slnx -c Release }
Pop-Location

Write-Host ''
if ($fehler) { Write-Host "Fehlgeschlagen: $($fehler -join ', ')" -ForegroundColor Red }
else { Write-Host 'Alle Pakete da.' -ForegroundColor Green }
}
```

## Danach von Hand

- `gh auth login`
- `claude` starten und einloggen
- `git config --global user.name "…"` und `user.email`
- Windows Terminal: Schrift auf *JetBrainsMono Nerd Font* stellen
- Zed-Einstellungen vom Linux-Rechner (`~/.config/zed/settings.json`, `themes/`, `AGENTS.md`)
  nach `%APPDATA%\Zed` kopieren
- Grammatik testen: `%USERPROFILE%\LanguageTool\start.cmd` starten, dann Gonk Note
