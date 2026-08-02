using System.IO;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GonkNote.Core.Platform;

// Beide Seiten nennen ihre Schnittstelle IClipboard. Der Kurzname bleibt der aus Core —
// das ist die, die dieser Kopf **erfüllt**; die von Avalonia ist das Werkzeug dahinter.
using Systemablage = Avalonia.Input.Platform.IClipboard;

namespace GonkNote.Platform;

/// <summary>
/// Die Zwischenablage des Systems.
/// <para>
/// Wie beim WPF-Kopf ist jeder Zugriff abgesichert: die Ablage gehört dem ganzen System,
/// ein anderes Programm kann sie im selben Moment sperren — das darf hier nie mehr sein als
/// „nichts da". Unter Linux kommt dazu, dass die Ablage am <b>Besitzerfenster</b> hängt:
/// ist die Quelle geschlossen, gibt es den Inhalt schlicht nicht mehr.
/// </para>
/// </summary>
public sealed class AvaloniaClipboard : Core.Platform.IClipboard
{
    public void SetText(string text)
    {
        try { if (Ablage() is { } a) Modal.Warte(a.SetTextAsync(text)); }
        catch { /* fremd gesperrt oder kein Fenster */ }
    }

    public string? GetText()
    {
        try { return Ablage() is { } a ? Modal.Warte(a.TryGetTextAsync()) : null; }
        catch { return null; }
    }

    /// <summary>
    /// <b>Diese Abfrage kodiert wirklich</b> — anders als unter Windows, wo
    /// <c>Clipboard.ContainsImage()</c> nur nachsieht. Avalonia hat kein „enthält" ohne
    /// „hol es", das Bild muss also durch den Dekodierer, um die Frage zu beantworten.
    /// <para>
    /// Für den heutigen Aufrufer ist das folgenlos (die Abfrage entscheidet über einen
    /// ausgegrauten Menüeintrag, und die Ablage enthält selten ein großes Bild). <b>Wer sie
    /// künftig in einer Schleife oder beim Öffnen jedes Menüs aufruft, sollte das
    /// wissen.</b>
    /// </para>
    /// </summary>
    public bool HasImage => GetImage() != null;

    /// <summary>Das Bild als PNG-Bytes — verlustfrei, und die Form, in der es in den Blob-Speicher wandert.</summary>
    public byte[]? GetImage()
    {
        try
        {
            if (Ablage() is not { } a) return null;
            using var bild = Modal.Warte(a.TryGetBitmapAsync());
            if (bild == null) return null;

            using var ms = new MemoryStream();
            bild.Save(ms, PngBitmapEncoderOptions.Default);   // PNG: verlustfrei
            return ms.ToArray();
        }
        catch { return null; }
    }

    public IReadOnlyList<string> GetFiles()
    {
        try
        {
            if (Ablage() is not { } a) return [];
            var dateien = Modal.Warte(a.TryGetFilesAsync());
            if (dateien == null) return [];

            // Wie im Dateidialog: was keinen lokalen Pfad hat, lässt sich nicht öffnen.
            return [.. dateien.Select(f => f.TryGetLocalPath() ?? "").Where(p => p.Length > 0)];
        }
        catch { return []; }
    }

    private static Systemablage? Ablage() => AvaloniaDialogService.Besitzer()?.Clipboard;
}
