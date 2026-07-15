using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GonkNote.Views;

/// <summary>
/// Einfaches Diagramm-Werkzeug: Werte + Beschriftungen eingeben, Typ wählen
/// (Balken/Linie/Kuchen), Live-Vorschau. Das Ergebnis wird als Bitmap gerendert
/// und in den Text eingefügt (druck-/exportfähig, keine Live-Datenbindung).
/// </summary>
public partial class ChartDialog : Window
{
    public RenderTargetBitmap? ResultImage { get; private set; }

    // Akzentpalette (Blau/Türkis/Pink/Lila/Grün/Gelb) für die Reihen/Segmente
    private static readonly Color[] Palette =
    {
        Color.FromRgb(0x25, 0x63, 0xEB), Color.FromRgb(0x14, 0xB8, 0xA6),
        Color.FromRgb(0xEC, 0x48, 0x99), Color.FromRgb(0x8B, 0x5C, 0xF6),
        Color.FromRgb(0x22, 0xC5, 0x5E), Color.FromRgb(0xEA, 0xB3, 0x08),
    };

    public ChartDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
    }

    private void Input_Changed(object sender, RoutedEventArgs e) => Redraw();

    private string ChartType =>
        (string)((ComboBoxItem)TypeCombo.SelectedItem).Tag;

    private void Redraw()
    {
        if (Preview == null) return;
        ErrorText.Text = "";

        var labels = LabelsBox.Text.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        var valueStrs = ValuesBox.Text.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        var values = new List<double>();
        foreach (var vs in valueStrs)
        {
            if (double.TryParse(vs.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                values.Add(v);
        }

        if (values.Count == 0) { ErrorText.Text = "Bitte Werte eingeben (z. B. 4, 7, 3)."; Preview.Source = null; return; }
        if (labels.Length < values.Count)
        {
            // fehlende Beschriftungen mit Nummern auffüllen
            labels = labels.Concat(Enumerable.Range(labels.Length + 1, values.Count - labels.Length)
                .Select(n => n.ToString())).ToArray();
        }

        var bmp = RenderChart(ChartType, TitleBox.Text.Trim(), labels, values, 520, 300);
        Preview.Source = bmp;
        ResultImage = bmp;
    }

    private static RenderTargetBitmap RenderChart(string type, string title, string[] labels,
        List<double> values, int w, int h)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
            var ink = new SolidColorBrush(Color.FromRgb(0x1B, 0x2B, 0x4B));
            var grid = new SolidColorBrush(Color.FromRgb(0xD4, 0xDE, 0xEA));
            var muted = new SolidColorBrush(Color.FromRgb(0x6B, 0x7A, 0x99));

            double top = title.Length > 0 ? 34 : 14;
            if (title.Length > 0)
                // Text() zentriert bereits über MaxTextWidth = w → Ursprung x = 0
                dc.DrawText(Text(title, 15, ink, true, w), new Point(0, 8));

            if (type == "pie")
                DrawPie(dc, labels, values, w, h, top, muted);
            else
                DrawAxes(dc, type, labels, values, w, h, top, ink, grid, muted);
        }

        var rtb = new RenderTargetBitmap(w * 2, h * 2, 192, 192, PixelFormats.Pbgra32);
        rtb.Render(dv);
        rtb.Freeze();
        return rtb;
    }

    private static void DrawAxes(DrawingContext dc, string type, string[] labels, List<double> values,
        int w, int h, double top, Brush ink, Brush grid, Brush muted)
    {
        double left = 40, right = 14, bottom = 34;
        double plotW = w - left - right, plotH = h - top - bottom;
        double max = Math.Max(1, values.Max());
        // "schöne" Obergrenze
        double step = NiceStep(max / 4);
        double axisMax = Math.Ceiling(max / step) * step;

        var axisPen = new Pen(grid, 1);
        for (int i = 0; i <= 4; i++)
        {
            double y = top + plotH - plotH * i / 4;
            dc.DrawLine(axisPen, new Point(left, y), new Point(left + plotW, y));
            double val = axisMax * i / 4;
            dc.DrawText(Text(val.ToString("0.#"), 11, muted, false, 36, TextAlignment.Right),
                new Point(left - 38, y - 8));
        }

        int n = values.Count;
        double slot = plotW / n;
        if (type == "line")
        {
            var pen = new Pen(new SolidColorBrush(Palette[0]), 2.5) { LineJoin = PenLineJoin.Round };
            Point? prev = null;
            for (int i = 0; i < n; i++)
            {
                double x = left + slot * (i + 0.5);
                double y = top + plotH - plotH * (values[i] / axisMax);
                if (prev is { } p) dc.DrawLine(pen, p, new Point(x, y));
                prev = new Point(x, y);
            }
            for (int i = 0; i < n; i++)
            {
                double x = left + slot * (i + 0.5);
                double y = top + plotH - plotH * (values[i] / axisMax);
                dc.DrawEllipse(new SolidColorBrush(Palette[0]), null, new Point(x, y), 3.5, 3.5);
                dc.DrawText(Text(labels[i], 11, muted, false, slot), new Point(x - slot / 2, top + plotH + 6));
            }
        }
        else // bar
        {
            double bw = slot * 0.6;
            for (int i = 0; i < n; i++)
            {
                double x = left + slot * i + (slot - bw) / 2;
                double bh = plotH * (values[i] / axisMax);
                double y = top + plotH - bh;
                dc.DrawRectangle(new SolidColorBrush(Palette[i % Palette.Length]), null, new Rect(x, y, bw, bh));
                dc.DrawText(Text(labels[i], 11, muted, false, slot),
                    new Point(left + slot * i + slot / 2 - slot / 2, top + plotH + 6));
            }
        }
    }

    private static void DrawPie(DrawingContext dc, string[] labels, List<double> values,
        int w, int h, double top, Brush muted)
    {
        double total = values.Sum();
        if (total <= 0) return;
        double cx = w * 0.36, cy = top + (h - top) / 2, r = Math.Min(cx - 20, (h - top) / 2 - 16);
        double angle = -90;
        for (int i = 0; i < values.Count; i++)
        {
            double sweep = values[i] / total * 360;
            var fill = new SolidColorBrush(Palette[i % Palette.Length]);
            dc.DrawGeometry(fill, null, PieSlice(cx, cy, r, angle, angle + sweep));
            angle += sweep;
        }
        // Legende rechts
        double lx = w * 0.7, ly = top + 10;
        for (int i = 0; i < values.Count; i++)
        {
            dc.DrawRectangle(new SolidColorBrush(Palette[i % Palette.Length]), null, new Rect(lx, ly + i * 22, 14, 14));
            double pct = values[i] / total * 100;
            dc.DrawText(Text($"{labels[i]} · {pct:0}%", 12, muted, false, 160),
                new Point(lx + 20, ly + i * 22 - 1));
        }
    }

    private static Geometry PieSlice(double cx, double cy, double r, double a0, double a1)
    {
        var p0 = new Point(cx + r * Math.Cos(a0 * Math.PI / 180), cy + r * Math.Sin(a0 * Math.PI / 180));
        var p1 = new Point(cx + r * Math.Cos(a1 * Math.PI / 180), cy + r * Math.Sin(a1 * Math.PI / 180));
        var fig = new PathFigure { StartPoint = new Point(cx, cy) };
        fig.Segments.Add(new LineSegment(p0, true));
        fig.Segments.Add(new ArcSegment(p1, new Size(r, r), 0, a1 - a0 > 180, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(new Point(cx, cy), true));
        var g = new PathGeometry();
        g.Figures.Add(fig);
        return g;
    }

    private static double NiceStep(double raw)
    {
        double mag = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(raw, 0.0001))));
        double norm = raw / mag;
        double nice = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10;
        return nice * mag;
    }

    private static FormattedText Text(string s, double size, Brush brush, bool bold, double maxW,
        TextAlignment align = TextAlignment.Center) =>
        new(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal,
                bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal),
            size, brush, 1.0)
        {
            MaxTextWidth = maxW, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis,
            TextAlignment = align,
        };

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Redraw();
        if (ResultImage == null) { ErrorText.Text = "Bitte gültige Werte eingeben."; return; }
        DialogResult = true;
    }
}
