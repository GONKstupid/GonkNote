using System.IO;
using Avalonia.Platform.Storage;
using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>
/// Datei öffnen und speichern über Avalonias <c>StorageProvider</c>.
/// <para>
/// Unter Linux ist das je nach Sitzung der GTK-Dialog des Portals oder Avalonias eigener;
/// unter Windows derselbe Dialog wie beim WPF-Kopf. Der Kopf muss das nicht wissen.
/// </para>
/// </summary>
public sealed class AvaloniaFileDialog : IFileDialog
{
    public IReadOnlyList<string> Open(string title, IReadOnlyList<FileFilter> filters, bool multiple = false)
    {
        var speicher = Speicher();
        if (speicher == null) return [];

        var auswahl = Modal.Warte(speicher.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = multiple,
            FileTypeFilter = ZuTypen(filters),
        }));

        // Leere Liste statt null: ein Abbruch ist kein Sonderfall, sondern schlicht
        // „keine Datei" (so steht es an IFileDialog).
        return auswahl.Select(Pfad).Where(p => p.Length > 0).ToList();
    }

    public string? Save(string title, string suggestedName, IReadOnlyList<FileFilter> filters, string? preferred = null)
    {
        var speicher = Speicher();
        if (speicher == null) return null;

        var typen = ZuTypen(filters);

        // Vorgewähltes Format: Avalonia hat kein „FilterIndex", die Reihenfolge der Liste
        // entscheidet. Den gewünschten Eintrag also nach vorn holen — und die Endung
        // zusätzlich als DefaultExtension setzen, damit ein Name ohne Punkt die richtige
        // bekommt.
        string? endung = preferred?.TrimStart('.');
        if (preferred != null)
        {
            int i = IndexVon(filters, preferred);
            if (i > 0) typen = [typen[i], .. typen.Where((_, k) => k != i)];
        }
        endung ??= filters.Count > 0 ? filters[0].PrimaryExtension.TrimStart('.') : null;

        var datei = Modal.Warte(speicher.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = endung,
            FileTypeChoices = typen,
            ShowOverwritePrompt = true,
        }));

        if (datei == null) return null;
        string pfad = Pfad(datei);
        return pfad.Length > 0 ? pfad : null;
    }

    private static int IndexVon(IReadOnlyList<FileFilter> filters, string extension)
    {
        for (int i = 0; i < filters.Count; i++)
            if (filters[i].Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    /// <summary>
    /// Die Endungen kommen mit Punkt herein (<c>".pdf"</c>) und brauchen hier den Stern
    /// davor — dieselbe Umformung wie bei Windows, nur mit anderem Ziel.
    /// </summary>
    private static List<FilePickerFileType> ZuTypen(IReadOnlyList<FileFilter> filters) =>
        [.. filters.Select(f => new FilePickerFileType(f.Label)
        {
            Patterns = [.. f.Extensions.Select(e => "*" + e)],
        })];

    /// <summary>
    /// Der lokale Pfad hinter einem <see cref="IStorageItem"/>.
    /// <para>
    /// <b>Kann leer sein, und das ist kein Fehler:</b> unter Linux liefert ein Portal-Dialog
    /// unter Umständen eine URI ohne Dateisystempfad (etwa aus einer Netzwerkfreigabe).
    /// <c>IFileDialog</c> gibt Pfade weiter, weil der Import große Dateien seitenweise über
    /// den Pfad liest — so eine Auswahl lässt sich hier also nicht bedienen und wird
    /// verworfen, statt später mit einem leeren Namen aufzuschlagen.
    /// </para>
    /// </summary>
    private static string Pfad(IStorageItem item)
    {
        try { return item.TryGetLocalPath() ?? ""; }
        catch { return ""; }
    }

    private static IStorageProvider? Speicher() =>
        AvaloniaDialogService.Besitzer()?.StorageProvider;
}
