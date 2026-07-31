using System.Windows;
using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>Meldungen über die WPF-<see cref="MessageBox"/>.</summary>
public sealed class WpfDialogService : IDialogService
{
    private const string Title = "Gonk Note";

    public void Inform(string message, DialogSeverity severity = DialogSeverity.Information) =>
        MessageBox.Show(Owner(), message, Title, MessageBoxButton.OK, Icon(severity));

    public bool Confirm(string message, DialogSeverity severity = DialogSeverity.Question) =>
        MessageBox.Show(Owner(), message, Title, MessageBoxButton.YesNo, Icon(severity))
            == MessageBoxResult.Yes;

    /// <summary>
    /// Ohne Besitzer erscheint das Fenster mittig auf dem Bildschirm statt über der App und
    /// kann hinter ihr verschwinden. <c>Application.Current</c> ist beim Start und beim
    /// Beenden kurzzeitig null — dann eben ohne.
    /// </summary>
    private static Window? Owner() => Application.Current?.MainWindow;

    private static MessageBoxImage Icon(DialogSeverity severity) => severity switch
    {
        DialogSeverity.Warning => MessageBoxImage.Warning,
        DialogSeverity.Question => MessageBoxImage.Question,
        _ => MessageBoxImage.Information,
    };
}
