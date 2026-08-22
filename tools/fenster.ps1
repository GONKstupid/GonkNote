# Wegwerf-Werkzeug, gemeinsamer Unterbau von schau.ps1 und klick.ps1.
# Kein Produktivcode -- steht bewusst unter tools\ und nicht in der Solution (HANDOFF §3).
#
# WARUM ES DAS GIBT (§4.49, der gescheiterte Augenschein): CopyFromScreen fotografiert den
# BILDSCHIRM. Liegt ein fremdes Fenster oben, steht es auf dem Bild -- und SetForegroundWindow
# wird von Windows abgelehnt, wenn ein anderer Prozess den Fokus haelt. Zweimal ist so ein
# fremdes Fenster aufgenommen worden. PrintWindow fotografiert das FENSTER SELBST und braucht
# keinen Fokus; PW_RENDERFULLCONTENT (2) holt auch DWM-gezeichnete Inhalte (WPF).

Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class F {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool an);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    public const uint DOWN = 0x0002, UP = 0x0004;
}
'@
[F]::SetProcessDPIAware() | Out-Null

# Nach vorn holen, so hartnaeckig wie Windows es zulaesst: an den Eingabestrang des aktuellen
# Vordergrundfensters anhaengen -- nur dann darf ein Prozess den Fokus verschieben.
function Vorn([IntPtr]$h) {
    if ($h -eq [IntPtr]::Zero) { return $false }
    $fremd = [F]::GetForegroundWindow()
    $strangFremd = [F]::GetWindowThreadProcessId($fremd, [IntPtr]::Zero)
    $strangSelbst = [F]::GetCurrentThreadId()
    if ($strangFremd -ne $strangSelbst) { [F]::AttachThreadInput($strangFremd, $strangSelbst, $true) | Out-Null }
    [F]::BringWindowToTop($h) | Out-Null
    $ok = [F]::SetForegroundWindow($h)
    if ($strangFremd -ne $strangSelbst) { [F]::AttachThreadInput($strangFremd, $strangSelbst, $false) | Out-Null }
    Start-Sleep -Milliseconds 300
    return ([F]::GetForegroundWindow() -eq $h)
}

# Fotografieren. ZWEI Verfahren, und die Wahl haengt am Vordergrund:
#  - Fenster steht vorn  -> Bildschirmabzug. Das ist, was der Nutzer saehe, und nur so kommen
#    Klappmenues und Dialoge mit aufs Bild: in Avalonia sind das EIGENE Fenster, PrintWindow
#    des Hauptfensters zeigt sie nicht.
#  - Fenster steht nicht vorn -> PrintWindow. Zeigt nur das Fenster selbst, aber garantiert
#    DAS RICHTIGE (§4.49: zweimal wurde ein fremdes Fenster aufgenommen).
function Foto([IntPtr]$h, [string]$Pfad, [bool]$Vorne) {
    $r = New-Object F+RECT
    [F]::GetWindowRect($h, [ref]$r) | Out-Null
    $b = $r.R - $r.L; $ho = $r.B - $r.T
    if ($b -le 0 -or $ho -le 0) { Write-Error "Fenster ohne Flaeche ($b x $ho)"; return $null }
    $bmp = New-Object Drawing.Bitmap $b, $ho
    $g = [Drawing.Graphics]::FromImage($bmp)
    $art = ''
    if ($Vorne) {
        $g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
        $art = 'Bildschirmabzug (Fenster stand vorn -- Klappmenues sind mit drauf)'
    } else {
        $dc = $g.GetHdc()
        $ok = [F]::PrintWindow($h, $dc, 2)          # 2 = PW_RENDERFULLCONTENT
        $g.ReleaseHdc($dc)
        $art = 'PrintWindow (nicht vorn -- Klappmenues FEHLEN auf dem Bild)'
        if (-not $ok -or (Schwarz $bmp)) {
            $g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
            $art = 'Bildschirmabzug OHNE Vordergrund (ACHTUNG: fremdes Fenster moeglich)'
        }
    }
    $bmp.Save($Pfad, [Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    return $art
}

# Stichprobe auf ein leeres Bild -- ein paar Punkte reichen, ein ganzer Durchlauf kostet Sekunden.
function Schwarz($bmp) {
    for ($x = 4; $x -lt $bmp.Width; $x += [Math]::Max(1, [int]($bmp.Width / 20))) {
        for ($y = 4; $y -lt $bmp.Height; $y += [Math]::Max(1, [int]($bmp.Height / 20))) {
            $p = $bmp.GetPixel($x, $y)
            if ($p.R -gt 8 -or $p.G -gt 8 -or $p.B -gt 8) { return $false }
        }
    }
    return $true
}
