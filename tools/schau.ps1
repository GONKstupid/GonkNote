# Wegwerf-Werkzeug: App mit der DB-Kopie starten, Fenster nach vorn holen, fotografieren.
# Kein Produktivcode -- steht bewusst unter tools\ und nicht in der Solution (HANDOFF §3).
param(
    [string]$Db   = "$env:TEMP\gonk-echt\gonknote.db",
    [string]$Shot = "$env:TEMP\gonk-schuss.png",
    [int]$WaitMs  = 4000,
    # Welcher Kopf: 'wpf' (Windows) oder 'avalonia' (Linux-Kopf, laeuft auch unter Windows).
    # Seit Phase 3 gibt es zwei -- und der Sinn, den Avalonia-Kopf unter Windows zu bauen,
    # ist genau, sie hier nebeneinander vergleichen zu koennen (HANDOFF §5b).
    [ValidateSet('wpf', 'avalonia')]
    [string]$Kopf = 'wpf',
    # Debug baut schneller und ohne Self-Contained; Release ist der Auslieferungsstand.
    [ValidateSet('Release', 'Debug')]
    [string]$Konfig = 'Release'
)

# DPI-Falle (HANDOFF §7): der Testrechner laeuft auf 200 %. Ohne SetProcessDPIAware -- es
# steht in fenster.ps1 -- sind die Fensterkoordinaten skaliert und der Ausschnitt sitzt daneben.
. "$PSScriptRoot\fenster.ps1"

if ($Kopf -eq 'avalonia') {
    # net10.0 ohne Plattform-Anhaengsel und ohne RID -- der Avalonia-Kopf ist nicht
    # self-contained (er soll unter Linux ueber das SDK laufen).
    $exe = "$PSScriptRoot\..\src\GonkNote.Avalonia\bin\$Konfig\net10.0\GonkNote.Avalonia.exe"
} elseif ($Konfig -eq 'Release') {
    $exe = "$PSScriptRoot\..\src\GonkNote.Wpf\bin\Release\net10.0-windows10.0.19041.0\win-x64\GonkNote.exe"
} else {
    $exe = "$PSScriptRoot\..\src\GonkNote.Wpf\bin\Debug\net10.0-windows10.0.19041.0\GonkNote.exe"
}
if (-not (Test-Path $exe)) { Write-Error "Nicht gebaut: $exe"; exit 1 }
$p = Start-Process $exe -ArgumentList '--db', "`"$Db`"" -PassThru

# Nur auf die EIGENE PID warten -- nie pauschal nach Fenstern suchen oder taskkill /IM
for ($i = 0; $i -lt 60 -and $p.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 250; $p.Refresh() }
if ($p.MainWindowHandle -eq 0) { Write-Error 'Kein Fenster erschienen.'; exit 1 }

[F]::ShowWindow($p.MainWindowHandle, 3) | Out-Null      # 3 = maximiert
$vorn = Vorn $p.MainWindowHandle
Start-Sleep -Milliseconds $WaitMs

$p.Refresh()
$art = Foto $p.MainWindowHandle $Shot $vorn
Write-Output "PID=$($p.Id) Bild=$Shot Aufnahme=$art Vordergrund=$vorn"
