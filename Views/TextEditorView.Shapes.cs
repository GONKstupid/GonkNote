using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GonkNote.Views;

/// <summary>
/// Formen und Diagramme im Text-Editor. Formen werden als kleine Vektorgrafik
/// (WPF-Shape in einem InlineUIContainer) eingefügt; Diagramme als aus Werten
/// gerenderte Zeichnung. Bewusst einfach gehalten – keine voll interaktiven
/// Office-Objekte, aber druck-/exportfähig (PNG-Roundtrip über den Export).
/// </summary>
public partial class TextEditorView
{
    // ==================== Formen ====================

    // Geometrie je Form (in einem 24×24-Feld), Akzentfarbe, gefüllt/umrandet
    private static readonly (string Key, string Tip)[] ShapeList =
    {
        ("rect", "Rechteck"), ("round", "Abgerundetes Rechteck"), ("ellipse", "Ellipse"),
        ("triangle", "Dreieck"), ("rtriangle", "Rechtwinkliges Dreieck"), ("diamond", "Raute"),
        ("pentagon", "Fünfeck"), ("hexagon", "Sechseck"), ("star", "Stern"),
        ("arrowR", "Pfeil rechts"), ("arrowL", "Pfeil links"), ("arrowU", "Pfeil oben"),
        ("arrowD", "Pfeil unten"), ("chevron", "Pfeilspitze"), ("heart", "Herz"),
        ("cloud", "Wolke"), ("cross", "Kreuz/Plus"), ("line", "Linie"),
        ("callout", "Sprechblase"), ("cylinder", "Zylinder"),
    };

    private bool _shapesBuilt;

    private void OpenShapes_Click(object s, RoutedEventArgs e)
    {
        if (!_shapesBuilt)
        {
            _shapesBuilt = true;
            var stroke = (Brush)FindResource("Brush.Accent");
            foreach (var (key, tip) in ShapeList)
            {
                var preview = new Path
                {
                    Data = Geometry.Parse(ShapeGeometry(key)),
                    Stroke = stroke,
                    StrokeThickness = 1.4,
                    Fill = FilledShape(key) ? new SolidColorBrush(((SolidColorBrush)stroke).Color) { Opacity = 0.18 } : null,
                    Stretch = Stretch.Uniform,
                    Width = 22, Height = 22,
                };
                var btn = new Button
                {
                    Style = (Style)FindResource("FlatButton"),
                    Content = preview,
                    Width = 40, Height = 40,
                    ToolTip = tip,
                };
                string k = key;
                btn.Click += (_, _) => { InsertShape(k); ShapesPopup.IsOpen = false; };
                ShapesGrid.Children.Add(btn);
            }
        }
        ShapesPopup.PlacementTarget = (UIElement)s;
        ShapesPopup.IsOpen = true;
    }

    private void InsertShape(string key)
    {
        var accent = ((SolidColorBrush)(Brush)FindResource("Brush.Accent")).Color;
        var shape = new Path
        {
            Data = Geometry.Parse(ShapeGeometry(key)),
            Stroke = new SolidColorBrush(accent),
            StrokeThickness = 2,
            Fill = FilledShape(key) ? new SolidColorBrush(accent) { Opacity = 0.18 } : null,
            Stretch = Stretch.Uniform,
            Width = key == "line" ? 120 : 90,
            Height = key == "line" ? 24 : 90,
            Margin = new Thickness(2),
        };
        Editor.CaretPosition = Editor.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
        _ = new InlineUIContainer(shape, Editor.CaretPosition);
        MarkDirty();
        Editor.Focus();
    }

    private static bool FilledShape(string key) =>
        key is not ("line" or "chevron" or "arrowR" or "arrowL" or "arrowU" or "arrowD");

    /// <summary>Pfad-Geometrie je Form im 24×24-Raster.</summary>
    private static string ShapeGeometry(string key) => key switch
    {
        "rect" => "M2,4 H22 V20 H2 Z",
        "round" => "M6,4 H18 A4,4 0 0 1 22,8 V16 A4,4 0 0 1 18,20 H6 A4,4 0 0 1 2,16 V8 A4,4 0 0 1 6,4 Z",
        "ellipse" => "M12,4 A10,8 0 1 1 12,20 A10,8 0 1 1 12,4 Z",
        "triangle" => "M12,3 L22,21 H2 Z",
        "rtriangle" => "M3,3 V21 H21 Z",
        "diamond" => "M12,2 L22,12 L12,22 L2,12 Z",
        "pentagon" => "M12,2 L22,10 L18,22 H6 L2,10 Z",
        "hexagon" => "M7,3 H17 L22,12 L17,21 H7 L2,12 Z",
        "star" => "M12,2 L14.9,8.6 L22,9.3 L16.6,14 L18.2,21 L12,17.3 L5.8,21 L7.4,14 L2,9.3 L9.1,8.6 Z",
        "arrowR" => "M2,9 H14 V5 L22,12 L14,19 V15 H2 Z",
        "arrowL" => "M22,9 H10 V5 L2,12 L10,19 V15 H22 Z",
        "arrowU" => "M9,22 V10 H5 L12,2 L19,10 H15 V22 Z",
        "arrowD" => "M9,2 V14 H5 L12,22 L19,14 H15 V2 Z",
        "chevron" => "M6,3 L15,12 L6,21",
        "heart" => "M12,21 C3,14 4,5 9,5 C11,5 12,7 12,7 C12,7 13,5 15,5 C20,5 21,14 12,21 Z",
        "cloud" => "M7,18 A4,4 0 0 1 7,10 A5,5 0 0 1 17,9 A4,4 0 0 1 17,18 Z",
        "cross" => "M9,3 H15 V9 H21 V15 H15 V21 H9 V15 H3 V9 H9 Z",
        "line" => "M2,12 H22",
        "callout" => "M3,4 H21 V16 H12 L7,21 V16 H3 Z",
        "cylinder" => "M4,6 A8,3 0 0 1 20,6 V18 A8,3 0 0 1 4,18 Z M4,6 A8,3 0 0 0 20,6",
        _ => "M2,4 H22 V20 H2 Z",
    };

    // ==================== Diagramm ====================

    private void OpenChart_Click(object s, RoutedEventArgs e)
    {
        var dlg = new ChartDialog { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true || dlg.ResultImage == null) return;

        var img = new Image
        {
            Source = dlg.ResultImage,
            Width = dlg.ResultImage.Width,
            Height = dlg.ResultImage.Height,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(2, 6, 2, 6),
        };
        // Diagramm als eigener Absatz (zentriert), damit es nicht im Fließtext klemmt
        var para = new Paragraph { TextAlignment = TextAlignment.Center };
        para.Inlines.Add(new InlineUIContainer(img));
        InsertBlockAtCaret(para);
        MarkDirty();
        Editor.Focus();
    }
}
