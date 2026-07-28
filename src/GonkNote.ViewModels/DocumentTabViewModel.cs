using GonkNote.Core.Models;
using GonkNote.Core.Services;

namespace GonkNote.ViewModels;

/// <summary>Basis für ein geöffnetes Dokument in einer Registerkarte.</summary>
public abstract class DocumentTabViewModel : ObservableObject
{
    protected readonly DatabaseService Db;
    private bool _isDirty;

    protected DocumentTabViewModel(NoteItem item, DatabaseService db)
    {
        Item = item;
        Db = db;
    }

    public NoteItem Item { get; }
    public Guid Id => Item.Id;
    public string Title => Item.Name;
    public ItemKind Kind => Item.Kind;

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (Set(ref _isDirty, value)) OnPropertyChanged(nameof(TabHeader));
        }
    }

    public string TabHeader => IsDirty ? Title + " •" : Title;

    public void NotifyRenamed()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(TabHeader));
    }

    /// <summary>Persistiert den aktuellen Stand in die Datenbank.</summary>
    public abstract void Save();
}

/// <summary>Tab für Whiteboards und Notizbücher (gleicher Editor, andere Seitengeometrie).</summary>
public sealed class WhiteboardTabViewModel : DocumentTabViewModel
{
    public WhiteboardDoc Doc { get; }

    public WhiteboardTabViewModel(NoteItem item, DatabaseService db) : base(item, db)
    {
        Doc = db.GetBoard(item);
    }

    // Ansichts-Zustand überlebt Tab-Wechsel (die View wird je Wechsel neu erzeugt)
    public UndoStack Undo { get; } = new();
    public int PageIndex { get; set; }
    public float Zoom { get; set; } = 1f;
    public float PanX { get; set; }
    public float PanY { get; set; }
    public bool ViewInitialized { get; set; }

    public override void Save()
    {
        if (!IsDirty) return;
        Db.SaveBoard(Doc);
        Db.UpsertItem(Item);
        IsDirty = false;
    }
}

/// <summary>Tab für Textdokumente. Der View liefert das RTF beim Speichern über FlushRequested.</summary>
public sealed class TextTabViewModel : DocumentTabViewModel
{
    public TextDoc Doc { get; }

    /// <summary>Wird vor dem Speichern ausgelöst; der Editor schreibt sein RTF in Doc.Rtf.</summary>
    public event Action? FlushRequested;

    public TextTabViewModel(NoteItem item, DatabaseService db) : base(item, db)
    {
        Doc = db.GetText(item.Id);
    }

    /// <summary>Editor-Zoom (1 = 100 %), überlebt Tab-Wechsel.</summary>
    public double Zoom { get; set; } = 1.0;

    public override void Save()
    {
        if (!IsDirty) return;
        FlushRequested?.Invoke();
        Db.SaveText(Doc);
        Db.UpsertItem(Item);
        IsDirty = false;
    }
}
