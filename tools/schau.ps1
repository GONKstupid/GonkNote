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

Add-Type -AssemblyName System.Windows.Forms, System.Drawing

# DPI-Falle (HANDOFF §7): der Testrechner laeuft auf 200 %. Ohne das sind die
# Fensterkoordinaten skaliert und der Ausschnitt sitzt daneben.
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class W {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
'@
[W]::SetProcessDPIAware() | Out-Null

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

[W]::ShowWindow($p.MainWindowHandle, 3) | Out-Null      # 3 = maximiert
[W]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds $WaitMs

$r = New-Object W+RECT
[W]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$bmp = New-Object Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
$g = [Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$bmp.Save($Shot, [Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

Write-Output "PID=$($p.Id) Bild=$Shot"
