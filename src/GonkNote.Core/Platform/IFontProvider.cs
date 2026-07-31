namespace GonkNote.Core.Platform;

/// <summary>
/// Welche Schrift die Oberfläche der Plattform benutzt. <c>WbRenderer</c> zeichnet Text
/// ohne Familienangabe damit — unter Windows „Segoe UI", unter Linux und iOS etwas anderes.
/// <para>
/// Das ist kein Schönheitsthema: bis Phase 2 stand „Segoe UI" fest im Renderer, und was
/// dann herauskam, war die stille Rückfallschrift von Skia. Genau deshalb prüft kein
/// Pixelhash gezeichneten Text (HANDOFF §4.6).
/// </para>
/// </summary>
public interface IFontProvider
{
    /// <summary>Familienname der Standard-Oberflächenschrift, z. B. „Segoe UI".</summary>
    string UiFamily { get; }
}

/// <summary>Die Vorgabe: „Segoe UI", also das Verhalten vor Phase 2.</summary>
public sealed class DefaultFontProvider : IFontProvider
{
    public string UiFamily => "Segoe UI";
}
