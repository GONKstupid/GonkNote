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

Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class K {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    public const uint DOWN = 0x0002, UP = 0x0004;
}
'@
[K]::SetProcessDPIAware() | Out-Null

$p = if ($AppPid -gt 0) { Get-Process -Id $AppPid } else { Get-Process GonkNote | Select-Object -First 1 }
[K]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 400

if ($X -ge 0) {
    [K]::SetCursorPos($X, $Y) | Out-Null
    Start-Sleep -Milliseconds 150
    for ($i = 0; $i -le $Doppel; $i++) {
        [K]::mouse_event([K]::DOWN, 0, 0, 0, [IntPtr]::Zero)
        [K]::mouse_event([K]::UP,   0, 0, 0, [IntPtr]::Zero)
        Start-Sleep -Milliseconds 60
    }
}
if ($Keys -ne '') { [Windows.Forms.SendKeys]::SendWait($Keys) }

Start-Sleep -Milliseconds $WaitMs

$r = New-Object K+RECT
[K]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$bmp = New-Object Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
$g = [Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$bmp.Save($Shot, [Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "ok $Shot"
