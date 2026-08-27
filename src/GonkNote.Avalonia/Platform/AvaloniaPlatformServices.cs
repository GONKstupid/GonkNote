using GonkNote.Core.Platform;
using GonkNote.Ocr;

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
/// <b>Zwei Einträge sind Rückfälle, keine Umsetzungen</b> — und zwar die, die Core bereits
/// mitbringt. Sie sind hier ausdrücklich benannt, damit niemand nachsehen muss, was der Kopf
/// kann und was nicht:
/// </para>
/// <list type="table">
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
/// <para>
/// <b>Hier stand bis Phase 4.5, Stück 6 auch <c>NoOcrEngine</c>.</b> Seither steht dort
/// <see cref="TesseractOcrEngine"/> — <b>dieselbe Klasse wie im WPF-Kopf</b>. Dass das
/// gehen würde, war bis dahin eine Annahme; der Laptop hat sie gemessen (HANDOFF §4.63):
/// der Wrapper trägt unter Linux gegen die System-Bibliothek, sobald drei Namen gerichtet
/// sind. Findet er sie nicht, meldet <see cref="IOcrEngine.IsAvailable"/> ehrlich „nicht
/// verfügbar" und der Knopf in den Schnellaktionen wird ausgeblendet — genau wie vorher.
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
    public IOcrEngine Ocr { get; } = new TesseractOcrEngine();
    public ISpellChecker SpellChecker { get; } = new AlwaysSupportedSpellChecker();
    public IPdfRasterizer Pdf { get; } = new PdfiumRasterizer();
    public IFontProvider Fonts { get; } = new AvaloniaFontProvider();
    public IDocumentIo Documents { get; } = new AvaloniaDocumentIo();
}
