using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>
/// Die Windows-Zwischenablage. Alle Zugriffe sind abgesichert: die Ablage gehört dem
/// ganzen System, ein anderes Programm kann sie im selben Moment sperren
/// (<c>COMException</c>) — das darf hier nie mehr sein als „nichts da".
/// </summary>
public sealed class WpfClipboard : IClipboard
{
    public void SetText(string text)
    {
        try { Clipboard.SetText(text); } catch { /* fremd gesperrt */ }
    }

    public string? GetText()
    {
        try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
        catch { return null; }
    }

    public bool HasImage
    {
        get { try { return Clipboard.ContainsImage(); } catch { return false; } }
    }

    /// <summary>
    /// Das Bild als PNG-Bytes — verlustfrei, und genau die Form, in der es danach in den
    /// Blob-Speicher wandert.
    /// </summary>
    public byte[]? GetImage()
    {
        try
        {
            if (Clipboard.GetImage() is not { } src) return null;
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(src));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
        catch { return null; }
    }

    public IReadOnlyList<string> GetFiles()
    {
        try
        {
            if (!Clipboard.ContainsFileDropList()) return [];
            return Clipboard.GetFileDropList().Cast<string>().Where(s => s.Length > 0).ToList();
        }
        catch { return []; }
    }
}
