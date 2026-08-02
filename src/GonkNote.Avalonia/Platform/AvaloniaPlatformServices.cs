using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>
/// Der Linux-Kopf, gebündelt — das Gegenstück zu <c>WpfPlatformServices</c>.
/// <para>
/// <b>Diese Klasse ist der Grund, warum <see cref="IPlatformServices"/> ein Bündel ist</b>
/// (HANDOFF §4.7): beim Anlegen hat der Compiler Stück für Stück gesagt, was fehlt. Bei
/// zwölf einzelnen Konstruktor-Argumenten stünde dieselbe Liste verteilt über mehrere
/// Aufrufe, und der nächste Dienst käme still nur an einer davon an.
/// </para>
/// <para>
/// <b>Drei Einträge sind Rückfälle, keine Umsetzungen</b> — und zwar die, die Core bereits
/// mitbringt. Sie sind hier ausdrücklich benannt, damit niemand nachsehen muss, was der Kopf
/// kann und was nicht:
/// </para>
/// <list type="table">
///   <item><term><see cref="NoOcrEngine"/></term>
///     <description>Tesseract ist im WPF-Kopf verdrahtet (<c>OcrService</c>) und zieht
///     Sprachdaten und native Bibliotheken mit. Der Rückfall meldet ehrlich „nicht
///     verfügbar", statt still einen leeren Text zu liefern — der wäre von „nichts erkannt"
///     nicht zu unterscheiden.</description></item>
///   <item><term><see cref="AlwaysSupportedSpellChecker"/></term>
///     <description>Es gibt keine Windows-Rechtschreib-API unter Linux. Hunspell ist der
///     vorgesehene Weg (HANDOFF §6, „aus V1 mitgeschleppt"); bis dahin nicht blockieren und
///     nicht warnen.</description></item>
///   <item><term><see cref="AvaloniaDocumentIo"/></term>
///     <description>Import und Export hängen an <c>FlowDocument</c> und kommen mit Phase 4
///     (HANDOFF §4.1). Siehe die Klasse selbst.</description></item>
/// </list>
/// <para>
/// <see cref="PdfiumRasterizer"/> dagegen ist <b>keine</b> Notlösung: die Umsetzung liegt in
/// Core, weil Windows und Linux sie sich teilen — nur iOS bekommt in Phase 5 etwas eigenes.
/// </para>
/// </summary>
public sealed class AvaloniaPlatformServices : IPlatformServices
{
    public IAppPaths Paths { get; } = new AvaloniaAppPaths();
    public IDialogService Dialogs { get; } = new AvaloniaDialogService();
    public IFileDialog Files { get; } = new AvaloniaFileDialog();
    public IClipboard Clipboard { get; } = new AvaloniaClipboard();
    public IThemeHost Theme { get; } = new AvaloniaThemeHost();
    public IShell Shell { get; } = new AvaloniaShell();
    public IUiScheduler Scheduler { get; } = new AvaloniaUiScheduler();
    public IOcrEngine Ocr { get; } = new NoOcrEngine();
    public ISpellChecker SpellChecker { get; } = new AlwaysSupportedSpellChecker();
    public IPdfRasterizer Pdf { get; } = new PdfiumRasterizer();
    public IFontProvider Fonts { get; } = new AvaloniaFontProvider();
    public IDocumentIo Documents { get; } = new AvaloniaDocumentIo();
}
