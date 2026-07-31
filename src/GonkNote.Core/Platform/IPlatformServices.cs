namespace GonkNote.Core.Platform;

/// <summary>
/// Alles, was die ViewModels vom Plattform-Kopf brauchen, an einer Stelle.
/// <para>
/// <b>Warum ein Bündel und keine acht Konstruktor-Argumente:</b> jeder neue Kopf muss
/// genau diese Liste erfüllen, und der Compiler sagt ihm beim Anlegen der Klasse, was
/// fehlt. Bei acht Argumenten stünde dieselbe Liste verteilt über mehrere Aufrufe, und
/// der nächste Dienst käme still nur an einer davon an.
/// </para>
/// <para>
/// Das ist <b>kein</b> Service-Locator: das Bündel wird übergeben, nicht global
/// abgefragt. Wer hier etwas hinzufügt, sollte es auch wirklich in mehr als einem
/// ViewModel brauchen.
/// </para>
/// </summary>
public interface IPlatformServices
{
    IAppPaths Paths { get; }
    IDialogService Dialogs { get; }
    IFileDialog Files { get; }
    IClipboard Clipboard { get; }
    IThemeHost Theme { get; }
    IShell Shell { get; }
    IUiScheduler Scheduler { get; }
    IOcrEngine Ocr { get; }
    ISpellChecker SpellChecker { get; }
    IPdfRasterizer Pdf { get; }
    IFontProvider Fonts { get; }
    IDocumentIo Documents { get; }
}
