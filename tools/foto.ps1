# Wegwerf-Werkzeug: ein SCHON LAUFENDES Fenster fotografieren.
# Kein Produktivcode -- steht bewusst unter tools\ und nicht in der Solution (HANDOFF §3).
#
# WARUM ES DAS GIBT (Phase 5, Schritt ①a): schau.ps1 STARTET die App und fotografiert einmal.
# Zum Vergleichen zweier Koepfe braucht man das Gegenteil -- einen Kopf, der stehen bleibt,
# und viele Aufnahmen daran. fenster.ps1 ist dafuer der Unterbau und keine Anwendung: es
# definiert [F], Vorn und Schwarz und wird per Punkt-Aufruf eingebunden.
#
# UND WARUM ES ABBRICHT STATT ZURUECKZUFALLEN (§4.50): Foto in fenster.ps1 faellt, wenn
# PrintWindow versagt, auf einen Bildschirmabzug OHNE Vordergrund zurueck und vermerkt das
# nur im Ausgabetext. Zweimal ist so ein FREMDES Fenster als Befund in die Doku gewandert.
# Hier gibt es diesen Rueckfall nicht: kommt kein sicheres Bild zustande, entsteht KEINE
# Datei und der Aufruf endet mit Fehler. Ein Werkzeug, das im Fehlerfall irgendein Bild
# liefert, ist schlimmer als eines, das nichts liefert.

param(
    # Die PID des laufenden Kopfs. NIE pauschal nach Fenstern suchen (§7, "Fernsteuern").
    [Parameter(Mandatory = $true)][int]$AppPid,
    [Parameter(Mandatory = $true)][string]$Bild,
    # Fuer Aufnahmen MIT offenem Klappmenue oder Flyout. Im Avalonia-Kopf sind das EIGENE
    # Fenster -- PrintWindow des Hauptfensters zeigt sie NICHT (fenster.ps1, Zeile 50).
    # Dann bleibt nur der Bildschirmabzug, und der verlangt echten Vordergrund.
    [switch]$Menue,
    # Setzzeit nach dem Nachvornholen, bevor ausgeloest wird.
    [int]$WartenMs = 350
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\fenster.ps1"

# Zusatz zu [F]: alle sichtbaren Fenster durchgehen. Gebraucht wird das fuer die
# Klappmenues -- in BEIDEN Koepfen sind sie eigene Fenster und liegen ausserhalb des
# Hauptfensters (im Avalonia-Kopf immer, im WPF-Kopf sobald das Menue nach links aufklappt).
# Aufgenommen wird die Huelle ueber die Fenster DIESES Prozesses und keinen Pixel mehr:
# ein fremdes Fenster kann so gar nicht erst aufs Bild geraten (§4.50).
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class E {
    [DllImport("user32.dll")] static extern bool EnumWindows(Del cb, IntPtr p);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out R r);
    [StructLayout(LayoutKind.Sequential)] public struct R { public int L, T, Ri, B; }
    delegate bool Del(IntPtr h, IntPtr p);
    public static bool PidVon(IntPtr h, ref int pid) {
        uint p; GetWindowThreadProcessId(h, out p); pid = (int)p; return true;
    }
    /// Das groesste sichtbare Fenster des Prozesses.
    /// Process.MainWindowHandle ist dafuer NICHT verlaesslich: steht gerade ein Tooltip oder
    /// ein Popup offen, liefert es dessen Fenster -- gemessen wurde so ein 314x50-Bild, das
    /// wie ein kaputtes Hauptfenster aussah. Die Groesse entscheidet, nicht die Reihenfolge.
    public static IntPtr Groesstes(uint ziel) {
        IntPtr best = IntPtr.Zero; long flaeche = 0;
        EnumWindows((h, _) => {
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (pid != ziel || !IsWindowVisible(h)) return true;
            R w; if (!GetWindowRect(h, out w)) return true;
            long f = (long)(w.Ri - w.L) * (w.B - w.T);
            if (f > flaeche) { flaeche = f; best = h; }
            return true;
        }, IntPtr.Zero);
        return best;
    }

    public static int[] Huelle(uint ziel) {
        int l = int.MaxValue, t = int.MaxValue, r = int.MinValue, b = int.MinValue;
        EnumWindows((h, _) => {
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (pid != ziel || !IsWindowVisible(h)) return true;
            R w; if (!GetWindowRect(h, out w)) return true;
            if (w.Ri - w.L <= 1 || w.B - w.T <= 1) return true;
            if (w.L < l) l = w.L;
            if (w.T < t) t = w.T;
            if (w.Ri > r) r = w.Ri;
            if (w.B > b) b = w.B;
            return true;
        }, IntPtr.Zero);
        return new[] { l, t, r, b };
    }
}
'@


$p = Get-Process -Id $AppPid -ErrorAction SilentlyContinue
if (-not $p) { Write-Error "Kein Prozess mit PID $AppPid."; exit 1 }
$p.Refresh()
$h = [E]::Groesstes([uint32]$AppPid)
if ($h -eq [IntPtr]::Zero) { $h = $p.MainWindowHandle }
if ($h -eq [IntPtr]::Zero) { Write-Error "PID $AppPid hat kein sichtbares Fenster."; exit 1 }

# Nach vorn holen -- ABER NUR, WENN NOETIG.
#
# ⚠ Der Fehler, der hier zweimal einen Scheinbefund erzeugt hat: Steht bereits ein Menue
# oder Flyout offen, ist DAS das Vordergrundfenster. Ein SetForegroundWindow auf das
# HAUPTfenster schliesst es -- und die Aufnahme zeigt dann ein Fenster ohne Menue, was
# aussieht wie "der Kopf hat kein Kontextmenue". Genau das ist gemessen und war falsch.
#
# Deshalb: Vordergrund NICHT erzwingen, sondern pruefen -- gehoert das Vordergrundfenster
# schon diesem Prozess (Hauptfenster ODER Popup), ist alles gut und es wird nichts angefasst.
$fg = [F]::GetForegroundWindow()
$fgPid = 0
[E]::PidVon($fg, [ref]$fgPid) | Out-Null
$vorn = ($fgPid -eq $AppPid)
if (-not $vorn) { $vorn = Vorn $h }
if ($Menue -and -not $vorn) {
    Write-Error "Fenster $AppPid kam nicht in den Vordergrund -- ein Bildschirmabzug zeigte jetzt ein FREMDES Fenster. Kein Bild geschrieben."
    exit 1
}
if ($WartenMs -gt 0) { Start-Sleep -Milliseconds $WartenMs }

$p.Refresh()
$r = New-Object F+RECT
[F]::GetWindowRect($h, [ref]$r) | Out-Null
$links = $r.L; $oben = $r.T; $rechts = $r.R; $unten = $r.B

if ($Menue) {
    # Huelle ueber ALLE sichtbaren Fenster dieses Prozesses -- Hauptfenster plus die
    # aufgeklappten Menues. Danach auf die Arbeitsflaeche beschneiden, sonst laeuft der
    # Bildschirmabzug bei einem Menue am Rand ins Leere.
    $hu = [E]::Huelle([uint32]$AppPid)
    if ($hu[0] -lt $hu[2]) {
        $links  = [Math]::Min($links,  $hu[0]); $oben  = [Math]::Min($oben,  $hu[1])
        $rechts = [Math]::Max($rechts, $hu[2]); $unten = [Math]::Max($unten, $hu[3])
    }
    $s = [Windows.Forms.SystemInformation]::VirtualScreen
    $links  = [Math]::Max($links,  $s.Left);  $oben  = [Math]::Max($oben,  $s.Top)
    $rechts = [Math]::Min($rechts, $s.Right); $unten = [Math]::Min($unten, $s.Bottom)
}

$b = $rechts - $links; $ho = $unten - $oben
if ($b -le 0 -or $ho -le 0) { Write-Error "Fenster ohne Flaeche ($b x $ho)."; exit 1 }

$bmp = New-Object Drawing.Bitmap $b, $ho
$g = [Drawing.Graphics]::FromImage($bmp)
try {
    if ($Menue) {
        # Vordergrund ist oben geprueft -- was auf dem Schirm steht, ist dieses Fenster.
        $g.CopyFromScreen($links, $oben, 0, 0, $bmp.Size)
        $art = 'Bildschirmabzug (Vordergrund geprueft -- Klappmenues sind mit drauf)'
    } else {
        $dc = $g.GetHdc()
        $ok = [F]::PrintWindow($h, $dc, 2)          # 2 = PW_RENDERFULLCONTENT
        $g.ReleaseHdc($dc)
        if (-not $ok) { throw "PrintWindow hat versagt. Mit -Menue erneut versuchen." }
        if (Schwarz $bmp) { throw "PrintWindow lieferte ein leeres Bild. Mit -Menue erneut versuchen." }
        $art = 'PrintWindow (garantiert dieses Fenster -- Klappmenues FEHLEN)'
    }
    $bmp.Save($Bild, [Drawing.Imaging.ImageFormat]::Png)
} catch {
    # Kein halbes Bild stehen lassen -- eine Datei aus einem misslungenen Lauf wird beim
    # naechsten Blick fuer einen Befund gehalten (§4.56).
    if (Test-Path $Bild) { Remove-Item $Bild -Force }
    Write-Error $_.Exception.Message
    exit 1
} finally {
    $g.Dispose(); $bmp.Dispose()
}

Write-Output "PID=$AppPid Bild=$Bild Groesse=${b}x${ho} Aufnahme=$art Vordergrund=$vorn"
