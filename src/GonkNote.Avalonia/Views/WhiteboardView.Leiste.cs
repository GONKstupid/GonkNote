using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;

namespace GonkNote.Views;

/// <summary>
/// Die klappbaren Gruppen der Werkzeugleiste (Phase 4.5, Stück 5).
///
/// <para>
/// <b>Warum überhaupt geklappt wird.</b> Mit den Stücken 1 bis 4 sind so viele Werkzeuge
/// dazugekommen, dass die Leiste rollte — und eine Leiste, die rollt, ist keine mehr: was
/// rechts hinausfällt, findet niemand. Eingeklappt zeigt eine Gruppe nur den <b>zuletzt
/// benutzten</b> Knopf; wer ihn anwählt, klappt die Gruppe auf und sieht die anderen.
/// </para>
///
/// <para>
/// <b>Die Regel steht in Core</b> (<see cref="WbLeiste"/>, §4.61) und nicht hier — der
/// WPF-Kopf hatte sie viermal nebeneinander stehen, einmal je Gruppe, jedes Mal dieselben
/// drei Zeilen mit anderen Feldern. Was hier steht, ist die Zuordnung von Knöpfen zu
/// Gruppen und das Merken des Vertreters.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    // Der zuletzt benutzte Knopf je Gruppe — er bleibt eingeklappt stehen.
    private ToolType _letzterStift = ToolType.Pen;
    private ToolType _letztesAuswahlwerkzeug = ToolType.Lasso;
    private Zeichenhilfe _letzteHilfe = Zeichenhilfe.Lineal;

    // **Vier, nicht drei** (§4.78). Hier standen drei, während `WbLeiste.Stifte` in Core seit
    // jeher vier führt — der Formen-Stift fehlte, und kein Wächter hat die beiden verglichen.
    // Der Wächter dafür steht jetzt in Core.Tests.
    private ToggleButton[] StiftKnoepfe => [BtnPen, BtnFormenStift, BtnPencil, BtnHighlighter];
    private ToggleButton[] AuswahlKnoepfe => [BtnLasso, BtnMove];
    private ToggleButton[] HilfeKnoepfe => [BtnLineal, BtnGeodreieck];

    /// <summary>
    /// Klappt alle vier Gruppen auf den Stand, der zum aktiven Werkzeug gehört.
    /// <para>
    /// <b>Die Formen-Gruppe hängt nicht am Werkzeug allein:</b> sie bleibt auf, solange das
    /// Formen-Werkzeug läuft, und geht mit jedem anderen zu — dieselbe Regel wie drüben.
    /// Die Zeichenhilfen hängen an gar keinem Werkzeug (sie sind keines), sondern daran, ob
    /// gerade eine auf der Fläche liegt.
    /// </para>
    /// </summary>
    private void LeisteKlappen()
    {
        Klappen(StiftKnoepfe, KnopfFuerStift(_letzterStift),
                WbLeiste.IstAufgeklappt(WbLeiste.Gruppe.Stifte, _tool));

        Klappen(AuswahlKnoepfe, _letztesAuswahlwerkzeug == ToolType.Move ? BtnMove : BtnLasso,
                WbLeiste.IstAufgeklappt(WbLeiste.Gruppe.Auswahl, _tool));

        Klappen(FormButtons, FormButtonFuer(_form),
                WbLeiste.IstAufgeklappt(WbLeiste.Gruppe.Formen, _tool));

        Klappen(HilfeKnoepfe, _letzteHilfe == Zeichenhilfe.Geodreieck ? BtnGeodreieck : BtnLineal,
                _hilfe != Zeichenhilfe.Keine);
    }

    /// <summary>
    /// Merkt sich den Vertreter einer Gruppe, bevor geklappt wird. <b>Vor</b> und nicht
    /// danach: eingeklappt soll der Knopf stehen bleiben, den der Nutzer gerade benutzt hat.
    /// </summary>
    private void VertreterMerken(ToolType werkzeug)
    {
        if (WbLeiste.IstStift(werkzeug)) _letzterStift = werkzeug;
        else if (WbLeiste.IstAuswahl(werkzeug)) _letztesAuswahlwerkzeug = werkzeug;
    }

    /// <inheritdoc cref="VertreterMerken(ToolType)"/>
    private void VertreterMerken(Zeichenhilfe hilfe)
    {
        if (hilfe != Zeichenhilfe.Keine) _letzteHilfe = hilfe;
    }

    private static void Klappen(IEnumerable<ToggleButton> knoepfe, ToggleButton vertreter, bool auf)
    {
        foreach (var b in knoepfe)
            b.IsVisible = WbLeiste.IstSichtbar(b, vertreter, auf);
    }

    private ToggleButton KnopfFuerStift(ToolType stift) => stift switch
    {
        ToolType.SmoothPen => BtnFormenStift,
        ToolType.Pencil => BtnPencil,
        ToolType.Highlighter => BtnHighlighter,
        _ => BtnPen,
    };
}
