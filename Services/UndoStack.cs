using GonkNote.Models;

namespace GonkNote.Services;

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

/// <summary>Skalieren eines Box-Elements (Bild oder Notizzettel).</summary>
public sealed class ResizeBoxAction : IEditAction
{
    private readonly IBoxElement _element;
    private readonly float _oldW, _oldH, _newW, _newH;

    public ResizeBoxAction(IBoxElement element, float oldW, float oldH, float newW, float newH)
    {
        _element = element;
        _oldW = oldW; _oldH = oldH;
        _newW = newW; _newH = newH;
    }

    public void Redo(WbPage page) { _element.Width = _newW; _element.Height = _newH; }
    public void Undo(WbPage page) { _element.Width = _oldW; _element.Height = _oldH; }
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
    private readonly record struct Entry(WbPage Page, IEditAction Action);

    private readonly Stack<Entry> _undo = new();
    private readonly Stack<Entry> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public event Action? Changed;

    /// <summary>Registriert eine bereits ausgeführte Aktion.</summary>
    public void Push(WbPage page, IEditAction action)
    {
        _undo.Push(new Entry(page, action));
        _redo.Clear();
        Changed?.Invoke();
    }

    /// <summary>Macht die letzte Aktion rückgängig und liefert deren Seite (für die Anzeige).</summary>
    public WbPage? Undo()
    {
        if (_undo.Count == 0) return null;
        var e = _undo.Pop();
        e.Action.Undo(e.Page);
        _redo.Push(e);
        Changed?.Invoke();
        return e.Page;
    }

    public WbPage? Redo()
    {
        if (_redo.Count == 0) return null;
        var e = _redo.Pop();
        e.Action.Redo(e.Page);
        _undo.Push(e);
        Changed?.Invoke();
        return e.Page;
    }
}
