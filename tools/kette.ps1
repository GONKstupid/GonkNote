# Wegwerf-Werkzeug: mehrere Klicks/Tasten in EINEM Durchgang.
#
# Warum nicht klick.ps1 mehrfach aufrufen: jeder Aufruf holt das Fenster mit
# SetForegroundWindow nach vorn — und genau das schliesst ein offenes Menue-Popup.
# Menuepfade brauchen deshalb eine Kette ohne Fokuswechsel dazwischen.
#
# Schritte als "x,y" (Klick), "x,y,2" (Doppelklick), "x,y,r" (Rechtsklick),
# "#TASTEN" (SendKeys) oder "x1,y1>x2,y2>..." (ZIEHEN ueber einen Pfad).
#
# Der Zieh-Schritt kam nach, weil Lasso und Verschieben ohne ihn nicht fernsteuerbar sind:
# ein Klick allein erzeugt keine Auswahl. Das Linux-Gegenstueck (tools/linux/klick.sh) kann
# das seit Phase 3 mit 'z:', unter Windows fehlte es. Zwischen den Stuetzpunkten wird
# interpoliert -- die App sammelt ihre Lassopunkte aus Mausbewegungen, ein einzelner Sprung
# ergaebe eine Gerade statt einer Umkreisung.
param(
    [string[]]$Schritte,
    [string]$Shot   = "$env:TEMP\gonk-schuss.png",
    [int]$WaitMs    = 700,
    [int]$EndeMs    = 1500,
    [int]$AppPid    = 0,
    # Ganzen Bildschirm statt nur des Fensters aufnehmen.
    # **Fuer Menuepfade Pflicht:** Avalonia zeichnet Menues und Flyouts in eigene
    # Popup-Fenster. Die liegen ausserhalb von GetWindowRect des Hauptfensters und fehlen
    # deshalb auf einer Fensteraufnahme vollstaendig -- man sieht das geschlossene Fenster
    # und haelt das Menue faelschlich fuer nicht geoeffnet (HANDOFF §7).
    # Der WPF-Kopf zeichnet seine Menues in denselben Fensterbereich; dort faellt es nicht auf.
    [switch]$Voll
)

Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class C {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    public const uint DOWN = 0x0002, UP = 0x0004, RDOWN = 0x0008, RUP = 0x0010;
}
'@
[C]::SetProcessDPIAware() | Out-Null

$p = if ($AppPid -gt 0) { Get-Process -Id $AppPid } else { Get-Process GonkNote | Select-Object -First 1 }
[C]::SetForegroundWindow($p.MainWindowHandle) | Out-Null   # genau einmal, ganz am Anfang
Start-Sleep -Milliseconds 500

foreach ($s in $Schritte) {
    if ($s.StartsWith('#')) {
        [Windows.Forms.SendKeys]::SendWait($s.Substring(1))
    }
    elseif ($s.Contains('>')) {
        # Ziehen: aufsetzen, ueber alle Stuetzpunkte fahren, loslassen.
        # Getrennte Achsen statt einer Liste von Paaren: die Pipeline entpackt
        # verschachtelte Arrays, und SetCursorPos bekommt dann ein Array statt einer Zahl.
        $xs = @(); $ys = @()
        foreach ($punkt in ($s -split '>')) {
            $t = $punkt -split ','
            $xs += [int]$t[0]; $ys += [int]$t[1]
        }
        [C]::SetCursorPos($xs[0], $ys[0]) | Out-Null
        Start-Sleep -Milliseconds 200
        [C]::mouse_event([C]::DOWN, 0, 0, 0, [IntPtr]::Zero)
        Start-Sleep -Milliseconds 120
        for ($k = 1; $k -lt $xs.Count; $k++) {
            for ($j = 1; $j -le 12; $j++) {
                [C]::SetCursorPos(
                    [int]($xs[$k - 1] + ($xs[$k] - $xs[$k - 1]) * $j / 12),
                    [int]($ys[$k - 1] + ($ys[$k] - $ys[$k - 1]) * $j / 12)) | Out-Null
                Start-Sleep -Milliseconds 12
            }
        }
        Start-Sleep -Milliseconds 120
        [C]::mouse_event([C]::UP, 0, 0, 0, [IntPtr]::Zero)
    }
    else {
        $t = $s -split ','
        [C]::SetCursorPos([int]$t[0], [int]$t[1]) | Out-Null
        Start-Sleep -Milliseconds 200
        if ($t.Count -gt 2 -and $t[2] -eq 'r') {
            [C]::mouse_event([C]::RDOWN, 0, 0, 0, [IntPtr]::Zero)
            [C]::mouse_event([C]::RUP,   0, 0, 0, [IntPtr]::Zero)
        }
        else {
            $n = if ($t.Count -gt 2) { [int]$t[2] } else { 1 }
            for ($i = 0; $i -lt $n; $i++) {
                [C]::mouse_event([C]::DOWN, 0, 0, 0, [IntPtr]::Zero)
                [C]::mouse_event([C]::UP,   0, 0, 0, [IntPtr]::Zero)
                Start-Sleep -Milliseconds 60
            }
        }
    }
    Start-Sleep -Milliseconds $WaitMs
}

Start-Sleep -Milliseconds $EndeMs
$r = New-Object C+RECT
if ($Voll) {
    $s = [Windows.Forms.SystemInformation]::VirtualScreen
    $r.L = $s.Left; $r.T = $s.Top; $r.R = $s.Right; $r.B = $s.Bottom
} else {
    [C]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
}
$bmp = New-Object Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
$g = [Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$bmp.Save($Shot, [Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "ok $Shot"
