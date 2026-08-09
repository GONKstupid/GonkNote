using GonkNote.Core.Models;
using GonkNote.Core.Platform;
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
    private readonly IDocumentIo _io;

    public TextDoc Doc { get; }

    /// <summary>Wird vor dem Speichern ausgelöst; der Editor schreibt sein RTF in Doc.Rtf.</summary>
    public event Action? FlushRequested;

    public TextTabViewModel(NoteItem item, DatabaseService db, IDocumentIo io) : base(item, db)
    {
        _io = io;
        Doc = db.GetText(item.Id);
    }

    /// <summary>Editor-Zoom (1 = 100 %), überlebt Tab-Wechsel.</summary>
    public double Zoom { get; set; } = 1.0;

    public override void Save()
    {
        if (!IsDirty) return;
        FlushRequested?.Invoke();
        Mitschreiben();
        Db.SaveText(Doc);
        Db.UpsertItem(Item);
        IsDirty = false;
    }

    /// <summary>
    /// Schreibt <see cref="TextDoc.Model"/> bei jedem Speichern mit — <b>neben</b>
    /// <see cref="TextDoc.Rtf"/>, das weiter führt (HANDOFF §4.23).
    ///
    /// <para>
    /// <b>Warum das sein muss, sobald ein Export aus dem Modell läuft:</b> Solange der Editor
    /// nur <c>Rtf</c> schreibt, stünde in <c>Model</c> der Stand der einmaligen Übernahme —
    /// ein Export daraus lieferte das Dokument von damals und nicht das auf dem Schirm. Der
    /// Fehler wäre kein Absturz, sondern eine Datei, in der die letzte Stunde Arbeit fehlt.
    /// </para>
    /// <para>
    /// <b>Die Nebenwirkung ist erwünscht:</b> Der Umwandler läuft ab jetzt bei jedem
    /// Speichern. Ein Fehler darin fällt damit auf, <b>solange <c>Rtf</c> noch führt</b> —
    /// also solange er niemandem schaden kann. Später, wenn die Anzeige aus dem Modell läuft,
    /// wäre derselbe Fehler sichtbarer Datenverlust.
    /// </para>
    /// <para>
    /// <b>Ein Fehler bleibt am Dokument stehen</b> (<see cref="TextDoc.MigrationIssue"/>,
    /// §4.22) — und wird hier bewusst **nicht** in ein Hinweisfenster gehoben: Gespeichert
    /// wird auch alle 30 Sekunden von selbst, und ein Fenster an dieser Stelle wäre genau der
    /// Hinweis, den man nach dem dritten Mal wegklickt, ohne ihn zu lesen.
    /// </para>
    /// </summary>
    private void Mitschreiben()
    {
        // Der Linux-Kopf kann das Altformat nicht lesen und darf es nicht versuchen — er
        // erzeugte ein leeres Modell und hätte den Inhalt damit scheinbar gelöscht (§4.22).
        if (_io.CanMigrate) _io.Migrate(Doc);
    }
}
