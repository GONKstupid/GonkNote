using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using GonkNote.ViewModels;

namespace GonkNote;

/// <summary>
/// Drag &amp; Drop im Ordnerbaum. Verschieben ist der Normalfall, mit gehaltener
/// <b>Strg</b>-Taste wird kopiert — dieselbe Bedienung wie im WPF-Kopf.
///
/// <para>
/// <b>Entschieden wird auch hier nichts.</b> Ziel und Quelle gehen an
/// <see cref="MainViewModel.MoveItem"/>; dort steht, dass ein Eintrag nicht in sich selbst
/// oder einen eigenen Unterordner wandern darf, dass Kopien die Ordnerfarbe des Ziels erben
/// und wann die Galerie neu gebaut wird. Diese Datei liefert nur, was der Zeiger meint.
/// </para>
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// Das Format, in dem der gezogene Eintrag reist — <b>prozessintern</b>
    /// (<see cref="DataFormat.CreateInProcessFormat{T}"/>).
    ///
    /// <para>
    /// Das ist keine Bequemlichkeit, sondern die einzige Fassung, die trägt: unter X11 läuft
    /// ein gewöhnliches Ziehen über <b>XDND</b>, ein Protokoll zwischen Prozessen — dort
    /// überlebt kein .NET-Objektverweis, sondern nur ein Datenstrom. Ein prozessinternes
    /// Format verlässt Avalonia gar nicht erst und reicht deshalb den echten
    /// <see cref="TreeItemViewModel"/> durch, statt eine Kennung, die die Gegenseite wieder
    /// im Baum suchen müsste.
    /// </para>
    /// </summary>
    private static readonly DataFormat<TreeItemViewModel> BaumEintrag =
        DataFormat.CreateInProcessFormat<TreeItemViewModel>("GonkNote.Baumeintrag");

    /// <summary>
    /// Der Zeigerdruck, aus dem das Ziehen hervorgeht. <see cref="DragDrop.DoDragDropAsync"/>
    /// verlangt ausdrücklich die <see cref="PointerPressedEventArgs"/> und nicht irgendein
    /// Zeigerereignis — es braucht den Zeiger, der noch aufliegt.
    /// </summary>
    private PointerPressedEventArgs? _ziehDruck;

    private TreeItemViewModel? _ziehKandidat;
    private Point _ziehVon;

    /// <summary>Der Knoten, der gerade als Ziel hervorgehoben ist (siehe <see cref="ZielMarkieren"/>).</summary>
    private TreeViewItem? _zielKnoten;

    /// <summary>
    /// Ab wieviel Pixeln Zeigerweg ein Druck als Ziehen gilt. WPF fragt das System
    /// (<c>SystemParameters.MinimumHorizontalDragDistance</c>); Avalonia hat dafür keine
    /// Auskunft, deshalb steht hier ein Wert. Zu klein hieße: jeder Klick auf einen Eintrag
    /// wird zum Ziehen und das Auswählen fühlt sich zittrig an.
    /// </summary>
    private const double Ziehschwelle = 6;

    /// <summary>Die Klasse, über die <c>Themes/Styles.axaml</c> den Zielordner einfärbt.</summary>
    private const string ZielKlasse = "zielordner";

    private void ZiehenEinhaengen()
    {
        DragDrop.SetAllowDrop(Baum, true);

        // Tunnel bei Gedrückt und Bewegt: der Baum muss den Zeiger sehen, bevor der
        // TreeViewItem darunter ihn für Auswahl und Aufklappen verbraucht.
        Baum.AddHandler(PointerPressedEvent, Baum_ZeigerGedrueckt, RoutingStrategies.Tunnel);
        Baum.AddHandler(PointerMovedEvent, Baum_ZeigerBewegt, RoutingStrategies.Tunnel);
        Baum.AddHandler(PointerReleasedEvent, Baum_ZeigerLos, RoutingStrategies.Tunnel);

        Baum.AddHandler(DragDrop.DragOverEvent, Baum_DarueberGezogen);
        Baum.AddHandler(DragDrop.DragLeaveEvent, Baum_Verlassen);
        Baum.AddHandler(DragDrop.DropEvent, Baum_Fallengelassen);
    }

    // ==================== Quelle ====================

    private void Baum_ZeigerGedrueckt(object? sender, PointerPressedEventArgs e)
    {
        _ziehDruck = null;
        _ziehKandidat = null;

        // Nur die linke Taste zieht; ein Rechtsklick öffnet das Kontextmenü.
        if (!e.GetCurrentPoint(Baum).Properties.IsLeftButtonPressed) return;
        // Auf einem Umbenennen-Feld ist Ziehen Textauswahl.
        if (e.Source is TextBox) return;

        if (Knoten(e.Source)?.DataContext is not TreeItemViewModel t || t.IsRenaming) return;

        _ziehDruck = e;
        _ziehKandidat = t;
        _ziehVon = e.GetPosition(Baum);
    }

    private void Baum_ZeigerBewegt(object? sender, PointerEventArgs e)
    {
        if (_ziehDruck is not { } druck || _ziehKandidat is not { } quelle) return;
        if (!e.GetCurrentPoint(Baum).Properties.IsLeftButtonPressed) { ZiehenVergessen(); return; }

        var jetzt = e.GetPosition(Baum);
        if (Math.Abs(jetzt.X - _ziehVon.X) < Ziehschwelle &&
            Math.Abs(jetzt.Y - _ziehVon.Y) < Ziehschwelle) return;

        ZiehenVergessen();

        var fracht = new DataTransfer();
        fracht.Add(DataTransferItem.Create(BaumEintrag, quelle));

        // Bewusst nicht erwartet: `DoDragDropAsync` läuft, bis der Nutzer loslässt. Das ist
        // genau der Fall, den ein `async void`-Ereignis abdeckt — hier wartet niemand auf
        // ein Ergebnis, und der Oberflächen-Faden bleibt in seiner Nachrichtenschleife.
        _ = DragDrop.DoDragDropAsync(druck, fracht, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void Baum_ZeigerLos(object? sender, PointerReleasedEventArgs e) => ZiehenVergessen();

    private void ZiehenVergessen()
    {
        _ziehDruck = null;
        _ziehKandidat = null;
    }

    // ==================== Ziel ====================

    private void Baum_DarueberGezogen(object? sender, DragEventArgs e)
    {
        if (Gezogener(e) is not { } quelle)
        {
            e.DragEffects = DragDropEffects.None;
            ZielMarkieren(null);
            return;
        }

        var knoten = Knoten(e.Source);
        var ziel = Zielordner(knoten?.DataContext as TreeItemViewModel);

        // Was `MoveItem` ohnehin abweisen würde, gar nicht erst als Ziel anbieten — sonst
        // sagt der Zeiger „geht" und beim Loslassen passiert nichts.
        if (!Erlaubt(quelle, ziel))
        {
            e.DragEffects = DragDropEffects.None;
            ZielMarkieren(null);
            return;
        }

        e.DragEffects = Kopieren(e) ? DragDropEffects.Copy : DragDropEffects.Move;
        // Die Wurzel (leere Fläche unter dem Baum) hat keinen Knoten zum Einfärben.
        ZielMarkieren(ziel == null ? null : knoten);
        e.Handled = true;
    }

    private void Baum_Verlassen(object? sender, DragEventArgs e) => ZielMarkieren(null);

    private void Baum_Fallengelassen(object? sender, DragEventArgs e)
    {
        ZielMarkieren(null);
        if (Gezogener(e) is not { } quelle) return;

        var ziel = Zielordner(Knoten(e.Source)?.DataContext as TreeItemViewModel);
        if (!Erlaubt(quelle, ziel)) return;

        _vm.MoveItem(quelle, ziel, Kopieren(e));
        e.Handled = true;
    }

    // ==================== Kleinkram ====================

    private static TreeItemViewModel? Gezogener(DragEventArgs e) =>
        e.DataTransfer.TryGetValue(BaumEintrag);

    private static bool Kopieren(DragEventArgs e) => e.KeyModifiers.HasFlag(KeyModifiers.Control);

    /// <summary>
    /// Wohin ein Eintrag fällt: auf einen Ordner in ihn hinein, auf ein Dokument in dessen
    /// Ordner, auf die leere Fläche in die Wurzel (<c>null</c>) — genau wie im WPF-Kopf.
    /// </summary>
    private TreeItemViewModel? Zielordner(TreeItemViewModel? getroffen) =>
        getroffen == null ? null
        : getroffen.IsFolder ? getroffen
        : _vm.FindParent(getroffen);

    /// <summary>
    /// Die Vorschau auf <see cref="MainViewModel.MoveItem"/>: kein Ziehen in sich selbst
    /// oder einen eigenen Unterordner, und kein Verschieben dorthin, wo der Eintrag schon
    /// liegt. Beim Kopieren ist Letzteres erlaubt — daraus wird eine Kopie daneben.
    /// </summary>
    private bool Erlaubt(TreeItemViewModel quelle, TreeItemViewModel? ziel)
    {
        for (var p = ziel; p != null; p = _vm.FindParent(p))
            if (p == quelle) return false;
        return true;
    }

    /// <summary>
    /// Der Baumknoten unter einem Ereignisziel. Gesucht wird über den <b>visuellen</b>
    /// Elternteil: bei einem Ziehereignis ist die Quelle das Steuerelement unter dem Zeiger,
    /// und das ist je nach Treffer der Text, das Symbol oder der Knoten selbst.
    /// </summary>
    private static TreeViewItem? Knoten(object? quelle) =>
        (quelle as Visual)?.FindAncestorOfType<TreeViewItem>(includeSelf: true);

    /// <summary>
    /// Hebt den Zielknoten hervor. <b>Ohne das sieht man beim Ziehen nicht, wo es hingeht</b>
    /// — unter Windows sagt es der Zeiger (Verschieben-/Kopieren-Symbol), unter
    /// X11/XWayland ist darauf kein Verlass.
    /// </summary>
    private void ZielMarkieren(TreeViewItem? knoten)
    {
        if (ReferenceEquals(_zielKnoten, knoten)) return;
        _zielKnoten?.Classes.Remove(ZielKlasse);
        _zielKnoten = knoten;
        _zielKnoten?.Classes.Add(ZielKlasse);
    }
}
