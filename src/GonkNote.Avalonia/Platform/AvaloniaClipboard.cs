using System.IO;
using Avalonia.Input;
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
    /// Fragt <b>nur die Formatliste</b> ab und holt das Bild nicht.
    /// <para>
    /// <b>Bis V2-84 stand hier <c>GetImage() != null</c></b> — und der Kommentar daneben
    /// warnte wörtlich: *„Wer sie künftig … beim Öffnen jedes Menüs aufruft, sollte das
    /// wissen."* Genau das kam mit den Schnellaktionen (§4.62): sie fragen bei
    /// <b>jedem</b> Öffnen, ob Einfügen etwas zu tun hätte. Unter Windows ist das ein
    /// billiges <c>ContainsImage</c>; hier wäre bei jedem Öffnen das ganze Bild durch den
    /// Dekodierer <b>und</b> den PNG-Kodierer gegangen — und <see cref="Modal.Warte"/>
    /// betritt dabei die Nachrichtenschleife neu, mitten in der Eingabeverarbeitung.
    /// </para>
    /// <para>
    /// <c>GetDataFormatsAsync</c> beantwortet dieselbe Frage, ohne die Daten anzufassen.
    /// <b>Das ist keine Näherung:</b> die Formatliste ist genau das, was die Ablage über
    /// sich selbst aussagt. Was danach beim Holen schiefgehen kann, fängt
    /// <see cref="GetImage"/> ohnehin ab.
    /// </para>
    /// </summary>
    public bool HasImage
    {
        get
        {
            try
            {
                if (Ablage() is not { } a) return false;
                return Modal.Warte(a.GetDataFormatsAsync()).Contains(DataFormat.Bitmap);
            }
            catch { return false; }
        }
    }

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
