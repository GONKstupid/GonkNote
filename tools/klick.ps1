# Wegwerf-Werkzeug: in das laufende Fenster klicken / tippen und neu fotografieren.
# Koordinaten sind ECHTE Bildschirmpixel (der Rechner laeuft auf 200 %, HANDOFF §7).
param(
    [int]$X = -1,
    [int]$Y = -1,
    [int]$Doppel = 0,
    [string]$Keys = '',
    [string]$Shot = "$env:TEMP\gonk-schuss.png",
    [int]$WaitMs = 1500,
    [int]$AppPid = 0
)

. "$PSScriptRoot\fenster.ps1"

$p = if ($AppPid -gt 0) { Get-Process -Id $AppPid } else { Get-Process GonkNote | Select-Object -First 1 }
$h = $p.MainWindowHandle
$vorn = Vorn $h

# Ohne Vordergrund geht KLICKEN nicht -- der Zeiger traefe ein fremdes Fenster. Lieber
# abbrechen als daneben klicken (§4.49: zweimal wurde ein fremdes Fenster fotografiert).
if (-not $vorn -and ($X -ge 0 -or $Keys -ne '')) {
    Write-Error 'Fenster kam nicht in den Vordergrund -- nicht geklickt, nicht getippt.'
    exit 1
}

if ($X -ge 0) {
    [F]::SetCursorPos($X, $Y) | Out-Null
    Start-Sleep -Milliseconds 150
    for ($i = 0; $i -le $Doppel; $i++) {
        [F]::mouse_event([F]::DOWN, 0, 0, 0, [IntPtr]::Zero)
        [F]::mouse_event([F]::UP,   0, 0, 0, [IntPtr]::Zero)
        Start-Sleep -Milliseconds 60
    }
}
if ($Keys -ne '') { [Windows.Forms.SendKeys]::SendWait($Keys) }

Start-Sleep -Milliseconds $WaitMs

$p.Refresh()
$art = Foto $p.MainWindowHandle $Shot $vorn
Write-Output "ok $Shot Aufnahme=$art Vordergrund=$vorn"
