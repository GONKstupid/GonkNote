using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>
/// Der Windows-Kopf, gebündelt. Wenn Phase 3 <c>AvaloniaPlatformServices</c> anlegt, sagt
/// der Compiler dort Stück für Stück, was noch fehlt — das ist der eigentliche Zweck des
/// Bündels.
/// </summary>
public sealed class WpfPlatformServices : IPlatformServices
{
    public IAppPaths Paths { get; } = new WpfAppPaths();
    public IDialogService Dialogs { get; } = new WpfDialogService();
    public IFileDialog Files { get; } = new WpfFileDialog();
    public IClipboard Clipboard { get; } = new WpfClipboard();
    public IThemeHost Theme { get; } = new WpfThemeHost();
    public IShell Shell { get; } = new WpfShell();
    public IUiScheduler Scheduler { get; } = new WpfUiScheduler();
    public IOcrEngine Ocr { get; } = new WpfOcrEngine();
    public ISpellChecker SpellChecker { get; } = new WpfSpellChecker();
    public IPdfRasterizer Pdf { get; } = new PdfiumRasterizer();
    public IFontProvider Fonts { get; } = new WpfFontProvider();
    public IDocumentIo Documents { get; } = new WpfDocumentIo();
}
