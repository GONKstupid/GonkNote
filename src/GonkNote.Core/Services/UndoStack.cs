using GonkNote.Core.Models;

namespace GonkNote.Core.Services;

public interface IEditAction
{
    void Undo(WbPage page);
    void Redo(WbPage page);
}

public sealed class AddElementsAction : IEditAction
{
    private readonly List<WbElement> _elements;
    public AddElementsAction(IEnumerable<WbElement> elements) => _elements = elements.ToList();

    public void Redo(WbPage page) => page.Elements.AddRange(_elements);
    public void Undo(WbPage page)
    {
        foreach (var el in _elements) page.Elements.Remove(el);
    }
}

public sealed class RemoveElementsAction : IEditAction
{
    private readonly List<(WbElement El, int Index)> _removed;

    public RemoveElementsAction(WbPage page, IEnumerable<WbElement> elements)
    {
        _removed = elements
            .Select(el => (el, page.Elements.IndexOf(el)))
            .Where(t => t.Item2 >= 0)
            .OrderBy(t => t.Item2)
            .ToList();
    }

    /// <summary>Für bereits entfernte Elemente mit bekannten Ursprungs-Indizes (Live-Radierer).</summary>
    public RemoveElementsAction(IEnumerable<(WbElement El, int Index)> removed)
    {
        _removed = removed.OrderBy(t => t.Index).ToList();
    }

    public void Redo(WbPage page)
    {
        foreach (var (el, _) in _removed) page.Elements.Remove(el);
    }

    public void Undo(WbPage page)
    {
        foreach (var (el, idx) in _removed)
            page.Elements.Insert(Math.Min(idx, page.Elements.Count), el);
    }
}

public sealed class MoveElementsAction : IEditAction
{
    private readonly List<WbElement> _elements;
    private readonly float _dx, _dy;

    public MoveElementsAction(IEnumerable<WbElement> elements, float dx, float dy)
    {
        _elements = elements.ToList();
        _dx = dx; _dy = dy;
    }

    public void Redo(WbPage page)
    {
        foreach (var el in _elements) el.Translate(_dx, _dy);
    }

    public void Undo(WbPage page)
    {
        foreach (var el in _elements) el.Translate(-_dx, -_dy);
    }
}

public sealed class TextChangeAction : IEditAction
{
    private readonly TextElement _element;
    private readonly string _oldText, _newText;

    public TextChangeAction(TextElement element, string oldText, string newText)
    {
        _element = element;
        _oldText = oldText;
        _newText = newText;
    }

    public void Redo(WbPage page) => _element.Text = _newText;
    public void Undo(WbPage page) => _element.Text = _oldText;
}

/// <summary>Ein Radier-Schritt: ein Element wurde entfernt und ggf. durch Teilstücke ersetzt.</summary>
public sealed record EraseStep(WbElement Removed, int Index, List<WbElement> Added);

/// <summary>
/// Punktgenaues Radieren: Striche werden an der Berührstelle aufgetrennt.
/// Die Schritte sind geordnet, weil spätere Schritte Teilstücke früherer treffen können.
/// </summary>
public sealed class PartialEraseAction : IEditAction
{
    private readonly List<EraseStep> _steps;

    public PartialEraseAction(List<EraseStep> steps) => _steps = steps;

    public void Redo(WbPage page)
    {
        foreach (var s in _steps)
        {
            int idx = page.Elements.IndexOf(s.Removed);
            if (idx >= 0) page.Elements.RemoveAt(idx);
            else idx = Math.Min(s.Index, page.Elements.Count);
            page.Elements.InsertRange(Math.Min(idx, page.Elements.Count), s.Added);
        }
    }

    public void Undo(WbPage page)
    {
        for (int i = _steps.Count - 1; i >= 0; i--)
        {
            var s = _steps[i];
            int idx = -1;
            foreach (var a in s.Added)
            {
                int j = page.Elements.IndexOf(a);
                if (j < 0) continue;
                if (idx < 0) idx = j;
                page.Elements.RemoveAt(j);
            }
            if (idx < 0) idx = Math.Min(s.Index, page.Elements.Count);
            page.Elements.Insert(Math.Min(idx, page.Elements.Count), s.Removed);
        }
    }
}

// **Hier stand bis Phase 5 ein `ResizeBoxAction`** — „Skalieren eines Box-Elements (Bild oder
// Notizzettel)", mit alter und neuer Breite und Höhe. **Gebaut hat ihn nie jemand.**
// Abgelöst hat ihn `ScaleElementsAction` darunter: seit §4.51 gehen beide Köpfe über die
// Griffe der *Auswahl*, und `ImageElement.Scale`/`StickyNoteElement.Scale` ändern Breite und
// Höhe mit. **Es fehlt hier also kein Undo-Schritt** — nachgesehen, statt es anzunehmen: die
// einzigen zwei Stellen, die skalieren, legen beide ein `ScaleElementsAction` an.

/// <summary>Gleichmäßiges Skalieren einer Auswahl um einen Pivot (Lasso-/Verschieben-Griff).</summary>
public sealed class ScaleElementsAction : IEditAction
{
    private readonly List<WbElement> _elements;
    private readonly float _factor, _px, _py;

    public ScaleElementsAction(IEnumerable<WbElement> elements, float factor, float px, float py)
    {
        _elements = elements.ToList();
        _factor = factor; _px = px; _py = py;
    }

    public void Redo(WbPage page)
    {
        foreach (var el in _elements) el.Scale(_factor, _px, _py);
    }

    public void Undo(WbPage page)
    {
        foreach (var el in _elements) el.Scale(1f / _factor, _px, _py);
    }
}

/// <summary>Drehen eines einzelnen Elements (um seinen Mittelpunkt).</summary>
public sealed class RotateElementAction : IEditAction
{
    private readonly WbElement _element;
    private readonly float _oldDeg, _newDeg;

    public RotateElementAction(WbElement element, float oldDeg, float newDeg)
    {
        _element = element;
        _oldDeg = oldDeg; _newDeg = newDeg;
    }

    public void Redo(WbPage page) => _element.Rotation = _newDeg;
    public void Undo(WbPage page) => _element.Rotation = _oldDeg;
}

public sealed class StickyTextChangeAction : IEditAction
{
    private readonly StickyNoteElement _element;
    private readonly string _oldText, _newText;

    public StickyTextChangeAction(StickyNoteElement element, string oldText, string newText)
    {
        _element = element;
        _oldText = oldText;
        _newText = newText;
    }

    public void Redo(WbPage page) => _element.Text = _newText;
    public void Undo(WbPage page) => _element.Text = _oldText;
}

/// <summary>Undo/Redo pro Dokument; jede Aktion kennt ihre Seite.</summary>
public sealed class UndoStack
{
    /// <summary>
    /// Tiefe des Verlaufs. Nötig, weil ein Schritt Elemente **am Leben hält**: gelöschte
    /// Striche, Bilder und Notizzettel hängen nur noch am Verlauf. Ohne Grenze wuchs der
    /// Speicherbedarf einer langen Sitzung immer weiter. 200 Schritte sind mehr, als je
    /// jemand zurücknimmt, und decken den Bedarf reichlich ab.
    /// </summary>
    private const int MaxSteps = 200;

    private readonly record struct Entry(WbPage Page, IEditAction Action);

    // Liste statt Stack: der älteste Schritt muss beim Überlauf vorne herausfallen.
    private readonly List<Entry> _undo = new();
    private readonly List<Entry> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public event Action? Changed;

    /// <summary>Registriert eine bereits ausgeführte Aktion.</summary>
    public void Push(WbPage page, IEditAction action)
    {
        _undo.Add(new Entry(page, action));
        if (_undo.Count > MaxSteps) _undo.RemoveAt(0);
        _redo.Clear();
        Changed?.Invoke();
    }

    /// <summary>Macht die letzte Aktion rückgängig und liefert deren Seite (für die Anzeige).</summary>
    public WbPage? Undo() => Move(_undo, _redo, redo: false);

    public WbPage? Redo() => Move(_redo, _undo, redo: true);

    private WbPage? Move(List<Entry> from, List<Entry> to, bool redo)
    {
        if (from.Count == 0) return null;

        var e = from[^1];
        from.RemoveAt(from.Count - 1);
        if (redo) e.Action.Redo(e.Page);
        else e.Action.Undo(e.Page);

        to.Add(e);
        if (to.Count > MaxSteps) to.RemoveAt(0);

        Changed?.Invoke();
        return e.Page;
    }
}
