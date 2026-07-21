using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GonkNote.Services;

/// <summary>
/// Färbt die native Windows-Titelleiste (Minimieren/Maximieren/Schließen) passend
/// zum App-Theme ein – dunkel im Dark Mode, hell im Light Mode. Nutzt das DWM-Attribut
/// DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 10 1809+/Windows 11). Auf älteren Systemen
/// ohne das Attribut bleibt die Leiste einfach hell (kein Fehler).
/// </summary>
public static class TitleBarTheme
{
    // Ab Windows 10 20H1 (Build 19041) ist das Attribut 20; auf 1809/1903/1909 war es 19.
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModePre20H1 = 19;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter,
        int x, int y, int cx, int cy, uint flags);

    private const uint SwpNoSize = 0x0001, SwpNoMove = 0x0002, SwpNoZOrder = 0x0004,
        SwpNoActivate = 0x0010, SwpFrameChanged = 0x0020;

    /// <summary>Setzt (oder entfernt) den dunklen Titelleisten-Modus für das Fenster.</summary>
    public static void Apply(Window window, bool dark)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return; // Fenster-Handle existiert erst ab OnSourceInitialized

        int on = dark ? 1 : 0;
        // Neueres Attribut zuerst; schlägt es fehl (ältere Builds), das alte versuchen.
        if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref on, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModePre20H1, ref on, sizeof(int));

        // Den Nicht-Client-Bereich neu berechnen/zeichnen lassen, damit die Farbe sofort
        // greift – ohne diesen Anstoß färbt sich die Leiste erst beim nächsten Fokuswechsel.
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }
}
