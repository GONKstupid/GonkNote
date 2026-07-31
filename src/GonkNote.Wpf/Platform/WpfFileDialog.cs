using GonkNote.Core.Platform;
using Microsoft.Win32;

namespace GonkNote.Platform;

/// <summary>Die Standard-Dateidialoge von Windows.</summary>
public sealed class WpfFileDialog : IFileDialog
{
    public IReadOnlyList<string> Open(string title, IReadOnlyList<FileFilter> filters, bool multiple = false)
    {
        var dlg = new OpenFileDialog
        {
            Title = title,
            Filter = ToWin32(filters),
            Multiselect = multiple,
        };
        return dlg.ShowDialog() == true ? dlg.FileNames : [];
    }

    public string? Save(string title, string suggestedName, IReadOnlyList<FileFilter> filters, string? preferred = null)
    {
        var dlg = new SaveFileDialog
        {
            Title = title,
            FileName = suggestedName,
            Filter = ToWin32(filters),
        };

        // Vorgewähltes Format. Der FilterIndex ist 1-basiert – ein 0 hier wählt still den
        // ersten Filter, was genau dem Standardfall entspricht und deshalb nicht auffiele.
        if (preferred != null)
        {
            int idx = IndexOf(filters, preferred);
            if (idx >= 0)
            {
                dlg.FilterIndex = idx + 1;
                dlg.DefaultExt = preferred.TrimStart('.');
            }
        }

        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private static int IndexOf(IReadOnlyList<FileFilter> filters, string extension)
    {
        for (int i = 0; i < filters.Count; i++)
            if (filters[i].Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    /// <summary>
    /// Windows erwartet <c>Bezeichnung|*.a;*.b|Bezeichnung|*.c</c>. Die Endungen kommen mit
    /// Punkt herein (<c>".pdf"</c>) und brauchen hier den Stern davor.
    /// </summary>
    private static string ToWin32(IReadOnlyList<FileFilter> filters) =>
        string.Join('|', filters.Select(f =>
            $"{f.Label}|{string.Join(';', f.Extensions.Select(e => "*" + e))}"));
}
