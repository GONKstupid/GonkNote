using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using GonkNote.Models;
using GonkNote.Services;

namespace GonkNote.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadTree();
    }

    /// <summary>
    /// Lädt den Ordnerbaum über den echten <see cref="DatabaseService"/> (dieselbe LiteDB-
    /// Persistenz wie die WPF-App) und zeigt ihn – beweist die Wiederverwendung der Kernlogik.
    /// </summary>
    private void LoadTree()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), "gonk-avalonia-demo.db");
        var db = new DatabaseService(dbPath);

        if (db.GetAllItems().Count == 0)
        {
            var schule = new NoteItem { Kind = ItemKind.Folder, Name = "Schule" };
            db.UpsertItem(schule);
            db.UpsertItem(new NoteItem { Kind = ItemKind.Notebook, Name = "Bio", ParentId = schule.Id });
            db.UpsertItem(new NoteItem { Kind = ItemKind.TextDocument, Name = "Notizen", ParentId = schule.Id });
            db.UpsertItem(new NoteItem { Kind = ItemKind.Whiteboard, Name = "Skizzen" });
        }

        var all = db.GetAllItems();
        var byId = all.ToDictionary(i => i.Id, i => new Node(i));
        var roots = new ObservableCollection<Node>();
        foreach (var n in byId.Values)
        {
            if (n.Item.ParentId is Guid pid && byId.TryGetValue(pid, out var parent))
                parent.Children.Add(n);
            else
                roots.Add(n);
        }
        db.Dispose();

        Tree.ItemsSource = roots;
    }
}

/// <summary>Baumknoten für die Anzeige (nur PoC – später ein richtiges ViewModel).</summary>
public sealed class Node
{
    public NoteItem Item { get; }
    public ObservableCollection<Node> Children { get; } = new();

    public Node(NoteItem item) => Item = item;

    public string Label => $"{Glyph(Item.Kind)}  {Item.Name}";

    private static string Glyph(ItemKind k) => k switch
    {
        ItemKind.Folder => "📁",
        ItemKind.Notebook => "📓",
        ItemKind.Whiteboard => "🖊",
        ItemKind.TextDocument => "📄",
        _ => "•",
    };
}
