using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GonkNote.Views;

/// <summary>
/// Aufzählungen und Nummerierungen. Früher hingen die Befehle in einem ContextMenu
/// mit <c>CommandTarget="{Binding ElementName=Editor}"</c> – über die ContextMenu-
/// Namescope-Grenze löst diese Bindung nicht auf, weshalb die Befehle ausgegraut
/// waren. Jetzt direkt aus dem Code auf den Editor angewandt, plus eine kleine
/// Stil-Bibliothek (Aufzählungszeichen / Nummerierung) wie in Word.
/// </summary>
public partial class TextEditorView
{
    // ---------- Direkte Umschalter (Split-Button links) ----------

    private void ToggleBullets_Click(object s, RoutedEventArgs e)
    {
        Editor.Focus();
        EditingCommands.ToggleBullets.Execute(null, Editor);
        MarkDirty();
    }

    private void ToggleNumbering_Click(object s, RoutedEventArgs e)
    {
        Editor.Focus();
        EditingCommands.ToggleNumbering.Execute(null, Editor);
        MarkDirty();
    }

    private void IndentInc_Click(object s, RoutedEventArgs e)
    {
        Editor.Focus();
        EditingCommands.IncreaseIndentation.Execute(null, Editor);
        MarkDirty();
    }

    private void IndentDec_Click(object s, RoutedEventArgs e)
    {
        Editor.Focus();
        EditingCommands.DecreaseIndentation.Execute(null, Editor);
        MarkDirty();
    }

    // ---------- Stil-Bibliotheken ----------

    private static readonly (string Label, TextMarkerStyle? Marker)[] BulletStyles =
    {
        ("—", null),                        // Kein(e)
        ("●", TextMarkerStyle.Disc),
        ("○", TextMarkerStyle.Circle),
        ("■", TextMarkerStyle.Square),
        ("▪", TextMarkerStyle.Box),
    };

    private static readonly (string Label, TextMarkerStyle? Marker)[] NumberStyles =
    {
        ("—", null),                        // Kein(e)
        ("1.", TextMarkerStyle.Decimal),
        ("a.", TextMarkerStyle.LowerLatin),
        ("A.", TextMarkerStyle.UpperLatin),
        ("i.", TextMarkerStyle.LowerRoman),
        ("I.", TextMarkerStyle.UpperRoman),
    };

    private bool _listLibsBuilt;

    private void BuildListLibraries()
    {
        if (_listLibsBuilt) return;
        _listLibsBuilt = true;

        foreach (var (label, marker) in BulletStyles)
            BulletGrid.Children.Add(MakeListCard(label, marker, ordered: false));
        foreach (var (label, marker) in NumberStyles)
            NumberGrid.Children.Add(MakeListCard(label, marker, ordered: true));
    }

    private Button MakeListCard(string label, TextMarkerStyle? marker, bool ordered)
    {
        var card = new Button
        {
            Style = (Style)FindResource("StyleCard"),
            Content = new TextBlock
            {
                Text = label,
                FontSize = 16,
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = marker == null
                    ? (Brush)FindResource("Brush.TextMuted")
                    : (Brush)FindResource("Brush.Text"),
            },
            Width = 66,
            Height = 40,
            ToolTip = marker == null ? "Keine Liste" : label,
        };
        card.Click += (_, _) =>
        {
            ApplyListMarker(marker, ordered);
            BulletPopup.IsOpen = false;
            NumberPopup.IsOpen = false;
        };
        return card;
    }

    private void OpenBulletLibrary_Click(object s, RoutedEventArgs e)
    {
        BuildListLibraries();
        NumberPopup.IsOpen = false;
        BulletPopup.PlacementTarget = (UIElement)s;
        BulletPopup.IsOpen = true;
    }

    private void OpenNumberLibrary_Click(object s, RoutedEventArgs e)
    {
        BuildListLibraries();
        BulletPopup.IsOpen = false;
        NumberPopup.PlacementTarget = (UIElement)s;
        NumberPopup.IsOpen = true;
    }

    // ---------- Anwenden ----------

    /// <summary>Die List, in der der Cursor steht (oder null).</summary>
    private List? CurrentList()
    {
        for (object? el = Editor.CaretPosition?.Parent; el is TextElement te; el = te.Parent)
            if (te is ListItem { Parent: List list }) return list;
        return null;
    }

    private static bool IsOrdered(TextMarkerStyle m) => m is
        TextMarkerStyle.Decimal or TextMarkerStyle.LowerLatin or TextMarkerStyle.UpperLatin
        or TextMarkerStyle.LowerRoman or TextMarkerStyle.UpperRoman;

    /// <summary>
    /// Setzt (oder entfernt bei marker == null) eine Liste des gewählten Stils auf die
    /// aktuelle Auswahl. Nutzt die EditingCommands zum Erzeugen/Entfernen und setzt
    /// danach die konkrete Markierungsart.
    /// </summary>
    private void ApplyListMarker(TextMarkerStyle? marker, bool ordered)
    {
        Editor.Focus();
        var list = CurrentList();

        if (marker == null)
        {
            if (list != null)
            {
                if (IsOrdered(list.MarkerStyle)) EditingCommands.ToggleNumbering.Execute(null, Editor);
                else EditingCommands.ToggleBullets.Execute(null, Editor);
            }
            MarkDirty();
            return;
        }

        if (list == null)
        {
            if (ordered) EditingCommands.ToggleNumbering.Execute(null, Editor);
            else EditingCommands.ToggleBullets.Execute(null, Editor);
            list = CurrentList();
        }
        if (list != null) list.MarkerStyle = marker.Value;
        MarkDirty();
    }
}
