using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GonkNote.Core.Platform;
using GonkNote.Views;

namespace GonkNote.Platform;

/// <summary>
/// Meldungen über <see cref="MessageWindow"/>.
/// <para>
/// Avalonia bringt <b>keine</b> MessageBox mit — anders als WPF, wo
/// <c>WpfDialogService</c> aus drei Zeilen besteht. Das ist kein Mangel des Toolkits,
/// sondern eine Folge davon, dass es keine gemeinsame Fassung für Windows, Linux und macOS
/// gibt.
/// </para>
/// </summary>
public sealed class AvaloniaDialogService : IDialogService
{
    public void Inform(string message, DialogSeverity severity = DialogSeverity.Information) =>
        MessageWindow.Zeige(Besitzer(), message, severity, frage: false);

    public bool Confirm(string message, DialogSeverity severity = DialogSeverity.Question) =>
        MessageWindow.Zeige(Besitzer(), message, severity, frage: true);

    /// <summary>
    /// Ohne Besitzer erschiene das Fenster mittig auf dem Bildschirm statt über der App und
    /// könnte hinter ihr verschwinden. Beim Start und beim Beenden gibt es kurzzeitig
    /// keines — dann eben ohne (dieselbe Überlegung wie in <c>WpfDialogService</c>).
    /// </summary>
    internal static Window? Besitzer() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;
}
