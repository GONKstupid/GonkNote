namespace GonkNote.Core.Platform;

/// <summary>Wie schwer wiegt die Meldung — bestimmt Symbol und Klang.</summary>
public enum DialogSeverity
{
    Information,
    Warning,
    Question,
}

/// <summary>
/// Meldungen an den Nutzer, ohne dass der Aufrufer wissen muss, womit sie angezeigt
/// werden. Der Titel („Gonk Note") gehört in die Umsetzung, nicht in jeden Aufruf.
/// <para>
/// Beide Methoden blockieren bis zur Antwort — genau wie <c>MessageBox.Show</c> heute.
/// Aufrufe gehören deshalb auf den Oberflächen-Faden.
/// </para>
/// </summary>
public interface IDialogService
{
    /// <summary>Eine Mitteilung mit einem einzigen „OK".</summary>
    void Inform(string message, DialogSeverity severity = DialogSeverity.Information);

    /// <summary>Eine Ja/Nein-Frage; <c>true</c> = Ja.</summary>
    bool Confirm(string message, DialogSeverity severity = DialogSeverity.Question);
}
