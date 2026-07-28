using System.Runtime.InteropServices;

namespace GonkNote.Services;

/// <summary>
/// Begrenzt ein randloses (WindowStyle=None) maximiertes Fenster auf den Arbeitsbereich
/// des Monitors, sodass die Taskleiste sichtbar bleibt (statt sie – wie WPF-Standard –
/// zu überdecken). Wird als WM_GETMINMAXINFO-Hook eingehängt (siehe MainWindow).
/// Hinweis: Ist der Arbeitsbereich exakt so groß wie der Monitor (z. B. automatisch
/// ausgeblendete Taskleiste), legt WPF beim randlosen Maximieren noch ~7 px Rahmen­
/// überstand an; das schneidet bei den 10-px-Rändern der App keinen sichtbaren Inhalt ab.
/// </summary>
public static class WindowBounds
{
    public const int WmGetMinMaxInfo = 0x0024;

    /// <summary>
    /// Setzt Maximier-Position/-Größe im MINMAXINFO auf den Monitor-Arbeitsbereich
    /// (physische Pixel – WM_GETMINMAXINFO rechnet in Gerätepixeln, unabhängig von der DPI).
    /// </summary>
    public static void AdjustMaximizedBounds(IntPtr hwnd, IntPtr lParam)
    {
        var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
        try
        {
            IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
            {
                RECT work = info.rcWork, mon = info.rcMonitor;
                mmi.ptMaxPosition.X = work.Left - mon.Left; // relativ zum Monitor
                mmi.ptMaxPosition.Y = work.Top - mon.Top;
                mmi.ptMaxSize.X = work.Right - work.Left;
                mmi.ptMaxSize.Y = work.Bottom - work.Top;
            }
        }
        catch { /* im Zweifel Standardgröße lassen */ }
        Marshal.StructureToPtr(mmi, lParam, true);
    }

    private const int MonitorDefaultToNearest = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO info);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor, rcWork;
        public int dwFlags;
    }
}
