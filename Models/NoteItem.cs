namespace GonkNote.Models;

/// <summary>Typ eines Eintrags im Ordnerbaum.</summary>
public enum ItemKind
{
    Folder,
    Notebook,
    Whiteboard,
    TextDocument,
}

/// <summary>Ein Eintrag im Ordnerbaum (Ordner oder Dokument). Inhalt liegt separat in der DB.</summary>
public class NoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = "";
    public ItemKind Kind { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Symbolfarbe im Baum als "#RRGGBB"; null = Standardfarbe.</summary>
    public string? IconColor { get; set; }

    public bool IsFolder => Kind == ItemKind.Folder;
}
