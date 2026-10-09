using Avalonia.Controls;
using Avalonia.Input;

namespace GonkNote.Views;

/// <summary>
/// Der Knopf einer Galeriekachel. Er überhört Tasten, die aus dem Umbenennen-Feld darin
/// hochlaufen: sonst öffnete die Leertaste den Eintrag. Das Feld selbst darf sie nicht
/// als erledigt markieren — Avalonia schluckt dann auch das Leerzeichen.
/// </summary>
public class KachelKnopf : Button
{
    protected override Type StyleKeyOverride => typeof(Button);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Source is not TextBox) base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Source is not TextBox) base.OnKeyUp(e);
    }
}
