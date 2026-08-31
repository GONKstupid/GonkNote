using System.Windows;
using GonkNote.Core.Platform;
using GonkNote.Views;

namespace GonkNote.Platform;

/// <summary>
/// Meldungen über <see cref="MessageWindow"/> — <b>nicht mehr über die native
/// <c>MessageBox</c></b> (HANDOFF §4.75).
///
/// <para>
/// <b>Der Wechsel ist der ganze Inhalt dieser Datei.</b> Die <c>MessageBox</c> trägt
/// Systemfarben, Systemschrift und Systemsymbol; an 28 Aufrufstellen sah damit jede
/// Meldung dieses Kopfs anders aus als dieselbe Meldung im Linux-Kopf, der seit jeher ein
/// eigenes Fenster hat. <i>Ein Unterschied, den man an jeder Fehlermeldung sieht, ist keine
/// Kleinigkeit — er ist nur überall ein bisschen.</i>
/// </para>
/// </summary>
public sealed class WpfDialogService : IDialogService
{
    public void Inform(string message, DialogSeverity severity = DialogSeverity.Information) =>
        MessageWindow.Zeige(Owner(), message, severity, frage: false);

    public bool Confirm(string message, DialogSeverity severity = DialogSeverity.Question) =>
        MessageWindow.Zeige(Owner(), message, severity, frage: true);

    /// <summary>
    /// Ohne Besitzer erscheint das Fenster mittig auf dem Bildschirm statt über der App und
    /// kann hinter ihr verschwinden. <c>Application.Current</c> ist beim Start und beim
    /// Beenden kurzzeitig null — dann eben ohne.
    /// </summary>
    private static Window? Owner() => Application.Current?.MainWindow;
}
