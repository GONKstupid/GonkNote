using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GonkNote.Views;

/// <summary>
/// Größe und Anordnung eingefügter Objekte (Formen, Diagramme, Bilder). Änderung
/// per Kontextmenü, da die WPF-RichTextBox keine Ziehgriffe für Inline-Objekte hat.
/// „Hinter den Text legen“ wird über ein <see cref="Figure"/> mit
/// <c>WrapDirection=None</c> gelöst: der Text fließt dann über die Form.
/// </summary>
public partial class TextEditorView
{
    /// <summary>Das größen-änderbare Element (Bild/Form/Diagramm) an der Einfügemarke.</summary>
    private FrameworkElement? CurrentSizableElement()
    {
        var caret = Editor.CaretPosition;
        foreach (var dir in new[] { LogicalDirection.Backward, LogicalDirection.Forward })
        {
            if (caret.GetAdjacentElement(dir) is InlineUIContainer { Child: FrameworkElement fe })
                return fe;
        }
        // Cursor steht ggf. innerhalb eines Figure/BlockUIContainer
        for (object? el = caret.Parent; el is DependencyObject d; el = LogicalTreeHelper.GetParent(d))
            if (el is InlineUIContainer { Child: FrameworkElement fe2 }) return fe2;
        return null;
    }

    /// <summary>Findet die InlineUIContainer bzw. Figure, die das Objekt am Cursor trägt.</summary>
    private (Inline Host, FrameworkElement Child)? CurrentObjectHost()
    {
        var caret = Editor.CaretPosition;
        foreach (var dir in new[] { LogicalDirection.Backward, LogicalDirection.Forward })
        {
            var adj = caret.GetAdjacentElement(dir);
            if (adj is InlineUIContainer { Child: FrameworkElement fe } iuc) return (iuc, fe);
        }
        // Innerhalb eines Figure (hinter den Text gelegt)
        for (object? el = caret.Parent; el is DependencyObject; el = LogicalTreeHelper.GetParent((DependencyObject)el!))
        {
            if (el is Figure fig && fig.Blocks.FirstBlock is BlockUIContainer { Child: FrameworkElement bfe })
                return (fig, bfe);
        }
        return null;
    }

    private void EnsureSize(FrameworkElement fe)
    {
        if (double.IsNaN(fe.Width) || fe.Width <= 0)
            fe.Width = fe.ActualWidth > 0 ? fe.ActualWidth : 120;
        if (double.IsNaN(fe.Height) || fe.Height <= 0)
            fe.Height = fe.ActualHeight > 0 ? fe.ActualHeight : 120;
    }

    private void ResizeCurrent(double fw, double fh)
    {
        if (CurrentSizableElement() is not { } fe) return;
        EnsureSize(fe);
        fe.Width = Math.Clamp(fe.Width * fw, 16, 2000);
        fe.Height = Math.Clamp(fe.Height * fh, 16, 2000);
        fe.MaxWidth = double.PositiveInfinity;   // eventuelle Deckelung vom Einfügen lösen
        MarkDirty();
    }

    private void ObjBigger_Click(object s, RoutedEventArgs e) => ResizeCurrent(1.15, 1.15);
    private void ObjSmaller_Click(object s, RoutedEventArgs e) => ResizeCurrent(0.87, 0.87);
    private void ObjWider_Click(object s, RoutedEventArgs e) => ResizeCurrent(1.15, 1.0);
    private void ObjNarrower_Click(object s, RoutedEventArgs e) => ResizeCurrent(0.87, 1.0);
    private void ObjTaller_Click(object s, RoutedEventArgs e) => ResizeCurrent(1.0, 1.15);
    private void ObjShorter_Click(object s, RoutedEventArgs e) => ResizeCurrent(1.0, 0.87);

    private void ObjExactSize_Click(object s, RoutedEventArgs e)
    {
        if (CurrentSizableElement() is not { } fe) return;
        EnsureSize(fe);
        string init = $"{Math.Round(fe.Width)} x {Math.Round(fe.Height)}";
        if (PromptDialog.Show(Window.GetWindow(this), "Genaue Größe",
                "Breite x Höhe in Pixel (z. B. 200 x 150):", init) is not { } input) return;

        var parts = input.Split(new[] { 'x', 'X', '*', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 &&
            double.TryParse(parts[0].Trim(), out double w) &&
            double.TryParse(parts[1].Trim(), out double h))
        {
            fe.Width = Math.Clamp(w, 16, 2000);
            fe.Height = Math.Clamp(h, 16, 2000);
            fe.MaxWidth = double.PositiveInfinity;
            MarkDirty();
        }
    }

    // ==================== Hinter/vor den Text ====================

    private void ShapeToBack_Click(object s, RoutedEventArgs e)
    {
        if (CurrentObjectHost() is not { Host: InlineUIContainer iuc, Child: { } fe }) return;
        var parent = iuc.Parent switch
        {
            Paragraph p => p.Inlines,
            Span span => span.Inlines,
            _ => null,
        };
        if (parent == null) return;
        EnsureSize(fe);

        // Kind aus dem alten Container lösen und in ein Figure hängen (Text fließt darüber).
        // Deutlich transparent → wirkt wie ein Hintergrund und bleibt lesbar, egal wie
        // WPF die Z-Reihenfolge des Figures legt.
        iuc.Child = null;
        fe.Opacity = 0.35;
        var figure = new Figure(new BlockUIContainer(fe))
        {
            Width = new FigureLength(fe.Width),
            Height = new FigureLength(fe.Height),
            HorizontalAnchor = FigureHorizontalAnchor.ContentCenter,
            VerticalAnchor = FigureVerticalAnchor.ParagraphTop,
            WrapDirection = WrapDirection.None,    // Text umfließt NICHT → er liegt darüber
            Padding = new Thickness(0), Margin = new Thickness(0),
        };
        parent.InsertAfter(iuc, figure);
        parent.Remove(iuc);
        MarkDirty();
        Editor.Focus();
    }

    private void ShapeToFront_Click(object s, RoutedEventArgs e)
    {
        if (CurrentObjectHost() is not { Host: Figure fig, Child: { } fe }) return;
        var parent = fig.Parent switch
        {
            Paragraph p => p.Inlines,
            Span span => span.Inlines,
            _ => null,
        };
        if (parent == null) return;

        if (fig.Blocks.FirstBlock is BlockUIContainer bc) bc.Child = null;
        fe.Opacity = 1.0;
        var iuc = new InlineUIContainer(fe);
        parent.InsertAfter(fig, iuc);
        parent.Remove(fig);
        MarkDirty();
        Editor.Focus();
    }
}
