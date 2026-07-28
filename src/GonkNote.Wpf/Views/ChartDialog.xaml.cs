using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GonkNote.Views;

/// <summary>
/// Diagramm-Werkzeug: Kategorien + eine oder mehrere Werte-Reihen (je Zeile eine
/// Kurve), Typ wählbar (Säulen/Balken/Linie/Punkt/Punkt+Linie/Kuchen/Radar),
/// anpassbare Farben, Live-Vorschau. Das Ergebnis wird als Bitmap gerendert und in
/// den Text eingefügt (druck-/exportfähig, keine Live-Datenbindung).
/// </summary>
public partial class ChartDialog : Window
{
    public RenderTargetBitmap? ResultImage { get; private set; }

    /// <summary>
    /// Farbpalette der Reihen/Kategorien; über die „+"-Kachel beliebig erweiterbar.
    /// Statisch, damit angepasste und hinzugefügte Farben innerhalb der Sitzung für
    /// alle Diagramme (Text-Editor, Whiteboard, Notizbuch) erhalten bleiben.
    /// </summary>
    private static readonly List<Color> Palette = new()
    {
        Color.FromRgb(0x25, 0x63, 0xEB), Color.FromRgb(0x14, 0xB8, 0xA6),
        Color.FromRgb(0xEC, 0x48, 0x99), Color.FromRgb(0x8B, 0x5C, 0xF6),
        Color.FromRgb(0x22, 0xC5, 0x5E), Color.FromRgb(0xEA, 0xB3, 0x08),
    };

    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x1B, 0x2B, 0x4B));
    private static readonly Brush Grid = new SolidColorBrush(Color.FromRgb(0xD4, 0xDE, 0xEA));
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(0x6B, 0x7A, 0x99));

    public ChartDialog()
    {
        InitializeComponent();
        BuildColorRow();
        Loaded += (_, _) => Redraw();
    }

    private void BuildColorRow()
    {
        ColorRow.Children.Clear();
        for (int i = 0; i < Palette.Count; i++)
        {
            int idx = i;
            var swatch = new Button
            {
                Width = 24, Height = 24, Margin = new Thickness(0, 0, 4, 4),
                Background = new SolidColorBrush(Palette[i]),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xDE, 0xEA)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = $"Farbe {i + 1} ändern",
            };
            swatch.Click += (_, _) =>
            {
                if (ColorPickerDialog.Pick(this, Palette[idx], allowAlpha: false) is { } col)
                {
                    Palette[idx] = col;
                    ((SolidColorBrush)swatch.Background).Color = col;
                    Redraw();
                }
            };

            // Rechtsklick: Farbe wieder löschen (mindestens eine bleibt übrig)
            var del = new MenuItem { Header = "Farbe löschen", IsEnabled = Palette.Count > 1 };
            del.Click += (_, _) =>
            {
                if (Palette.Count <= 1) return;
                Palette.RemoveAt(idx);
                BuildColorRow();   // Indizes der Kacheln neu vergeben
                Redraw();
            };
            var menu = new ContextMenu();
            menu.Items.Add(del);
            swatch.ContextMenu = menu;

            ColorRow.Children.Add(swatch);
        }

        // „+": Farbwähler öffnen und die gewählte Farbe als neue Kachel anhängen –
        // beliebig oft wiederholbar (Nutzer-Wunsch: mehr als 6 Farben)
        var add = new Button
        {
            Width = 24, Height = 24, Margin = new Thickness(0, 0, 4, 4),
            Content = "+", FontSize = 14, Padding = new Thickness(0, -2, 0, 0),
            Foreground = (Brush)FindResource("Brush.Text"),
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xDE, 0xEA)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Neue Farbe hinzufügen",
        };
        add.Click += (_, _) =>
        {
            if (ColorPickerDialog.Pick(this, Palette[^1], allowAlpha: false) is { } col)
            {
                Palette.Add(col);
                BuildColorRow();   // Reihe inkl. „+" neu aufbauen
                Redraw();
            }
        };
        ColorRow.Children.Add(add);
    }

    private void Input_Changed(object sender, RoutedEventArgs e) => Redraw();

    private string ChartType => (string)((ComboBoxItem)TypeCombo.SelectedItem).Tag;

    private static double[] ParseRow(string s) =>
        s.Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
         .Select(x => double.TryParse(x.Trim().Replace(',', '.'), NumberStyles.Float,
             CultureInfo.InvariantCulture, out double v) ? v : (double?)null)
         .Where(v => v.HasValue).Select(v => v!.Value).ToArray();

    private void Redraw()
    {
        if (Preview == null) return;
        ErrorText.Text = "";

        var cats = LabelsBox.Text.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();

        // Jede nicht-leere Zeile ist eine Reihe/Kurve
        var series = ValuesBox.Text
            .Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0)
            .Select(ParseRow).Where(r => r.Length > 0).ToList();
        if (series.Count == 0)
        {
            ErrorText.Text = "Bitte Werte eingeben (z. B. 4, 7, 3).";
            Preview.Source = null;
            ResultImage = null;
            return;
        }

        int maxLen = series.Max(r => r.Length);
        if (cats.Length < maxLen)
            cats = cats.Concat(Enumerable.Range(cats.Length + 1, maxLen - cats.Length)
                .Select(n => n.ToString())).ToArray();

        var names = SeriesBox.Text.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        if (names.Length < series.Count)
            names = names.Concat(Enumerable.Range(names.Length + 1, series.Count - names.Length)
                .Select(n => $"Reihe {n}")).ToArray();

        var bmp = RenderChart(ChartType, TitleBox.Text.Trim(), cats, series, names, Palette, 560, 320);
        Preview.Source = bmp;
        ResultImage = bmp;
    }

    private static RenderTargetBitmap RenderChart(string type, string title, string[] cats,
        List<double[]> series, string[] names, IReadOnlyList<Color> palette, int w, int h)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
            double top = title.Length > 0 ? 34 : 14;
            if (title.Length > 0)
                dc.DrawText(Text(title, 15, Ink, true, w), new Point(0, 8));

            // Legende (Reihennamen) rechts, wenn mehr als eine Reihe oder kein Kuchen
            bool showLegend = type != "pie" && series.Count > 1;
            double legendW = showLegend ? 120 : 0;
            var plot = new Rect(0, top, w - legendW, h - top);

            switch (type)
            {
                case "pie": DrawPie(dc, cats, series[0], palette, w, h, top); break;
                case "radar": DrawRadar(dc, cats, series, palette, plot); break;
                case "bar": DrawBars(dc, cats, series, palette, plot, horizontal: true); break;
                case "column": DrawBars(dc, cats, series, palette, plot, horizontal: false); break;
                default: DrawXY(dc, type, cats, series, palette, plot); break; // line/scatter/scatterline
            }

            if (showLegend) DrawLegend(dc, names, palette, w - legendW + 12, top + 8);
        }

        var rtb = new RenderTargetBitmap(w * 2, h * 2, 192, 192, PixelFormats.Pbgra32);
        rtb.Render(dv);
        rtb.Freeze();
        return rtb;
    }

    // ==================== Achsen-Diagramme (Linie/Punkt) ====================

    private static void DrawXY(DrawingContext dc, string type, string[] cats, List<double[]> series,
        IReadOnlyList<Color> palette, Rect area)
    {
        double left = 42, right = 14, bottom = 34;
        double plotW = area.Width - left - right, plotH = area.Height - bottom - 6;
        double x0 = area.X + left, y0 = area.Y + 6;
        double max = Math.Max(1, series.SelectMany(s => s).DefaultIfEmpty(0).Max());
        double step = NiceStep(max / 4), axisMax = Math.Ceiling(max / step) * step;

        var axisPen = new Pen(Grid, 1);
        for (int i = 0; i <= 4; i++)
        {
            double y = y0 + plotH - plotH * i / 4;
            dc.DrawLine(axisPen, new Point(x0, y), new Point(x0 + plotW, y));
            dc.DrawText(Text((axisMax * i / 4).ToString("0.#"), 11, Muted, false, 38, TextAlignment.Right),
                new Point(x0 - 40, y - 8));
        }

        int n = cats.Length;
        double slot = plotW / Math.Max(1, n);
        bool line = type is "line" or "scatterline";
        bool dots = type is "scatter" or "scatterline";

        for (int si = 0; si < series.Count; si++)
        {
            var col = new SolidColorBrush(palette[si % palette.Count]);
            var pen = new Pen(col, 2.4) { LineJoin = PenLineJoin.Round };
            Point? prev = null;
            var s = series[si];
            for (int i = 0; i < s.Length; i++)
            {
                double x = x0 + slot * (i + 0.5);
                double y = y0 + plotH - plotH * (s[i] / axisMax);
                var pt = new Point(x, y);
                if (line && prev is { } p) dc.DrawLine(pen, p, pt);
                prev = pt;
            }
            if (dots)
                for (int i = 0; i < s.Length; i++)
                {
                    double x = x0 + slot * (i + 0.5);
                    double y = y0 + plotH - plotH * (s[i] / axisMax);
                    dc.DrawEllipse(col, null, new Point(x, y), 3.6, 3.6);
                }
        }

        for (int i = 0; i < n; i++)
            dc.DrawText(Text(cats[i], 11, Muted, false, slot), new Point(x0 + slot * i, y0 + plotH + 6));
    }

    // ==================== Balken / Säulen (gruppiert, mehrere Reihen) ====================

    private static void DrawBars(DrawingContext dc, string[] cats, List<double[]> series,
        IReadOnlyList<Color> palette, Rect area, bool horizontal)
    {
        double max = Math.Max(1, series.SelectMany(s => s).DefaultIfEmpty(0).Max());
        double step = NiceStep(max / 4), axisMax = Math.Ceiling(max / step) * step;
        int n = cats.Length, m = series.Count;

        if (!horizontal)
        {
            double left = 42, right = 14, bottom = 34;
            double plotW = area.Width - left - right, plotH = area.Height - bottom - 6;
            double x0 = area.X + left, y0 = area.Y + 6;
            var axisPen = new Pen(Grid, 1);
            for (int i = 0; i <= 4; i++)
            {
                double y = y0 + plotH - plotH * i / 4;
                dc.DrawLine(axisPen, new Point(x0, y), new Point(x0 + plotW, y));
                dc.DrawText(Text((axisMax * i / 4).ToString("0.#"), 11, Muted, false, 38, TextAlignment.Right),
                    new Point(x0 - 40, y - 8));
            }
            double slot = plotW / Math.Max(1, n), bw = slot * 0.7 / m;
            for (int i = 0; i < n; i++)
            {
                double gx = x0 + slot * i + slot * 0.15;
                for (int si = 0; si < m; si++)
                {
                    if (i >= series[si].Length) continue;
                    double bh = plotH * (series[si][i] / axisMax);
                    // Eine Reihe → Farbe je Kategorie (bunt); mehrere Reihen → Farbe je Reihe
                    var col = m == 1 ? palette[i % palette.Count] : palette[si % palette.Count];
                    dc.DrawRectangle(new SolidColorBrush(col), null,
                        new Rect(gx + si * bw, y0 + plotH - bh, bw * 0.92, bh));
                }
                dc.DrawText(Text(cats[i], 11, Muted, false, slot), new Point(x0 + slot * i, y0 + plotH + 6));
            }
        }
        else
        {
            double left = 70, right = 30, top2 = 6, bottom = 20;
            double plotW = area.Width - left - right, plotH = area.Height - top2 - bottom;
            double x0 = area.X + left, y0 = area.Y + top2;
            var axisPen = new Pen(Grid, 1);
            for (int i = 0; i <= 4; i++)
            {
                double x = x0 + plotW * i / 4;
                dc.DrawLine(axisPen, new Point(x, y0), new Point(x, y0 + plotH));
                dc.DrawText(Text((axisMax * i / 4).ToString("0.#"), 11, Muted, false, 40),
                    new Point(x - 20, y0 + plotH + 3));
            }
            double slot = plotH / Math.Max(1, n), bh = slot * 0.7 / m;
            for (int i = 0; i < n; i++)
            {
                double gy = y0 + slot * i + slot * 0.15;
                for (int si = 0; si < m; si++)
                {
                    if (i >= series[si].Length) continue;
                    double bw = plotW * (series[si][i] / axisMax);
                    var col = m == 1 ? palette[i % palette.Count] : palette[si % palette.Count];
                    dc.DrawRectangle(new SolidColorBrush(col), null,
                        new Rect(x0, gy + si * bh, bw, bh * 0.92));
                }
                dc.DrawText(Text(cats[i], 11, Muted, false, left - 4, TextAlignment.Right),
                    new Point(x0 - left, gy + slot * 0.2));
            }
        }
    }

    // ==================== Radar ====================

    private static void DrawRadar(DrawingContext dc, string[] cats, List<double[]> series,
        IReadOnlyList<Color> palette, Rect area)
    {
        int n = cats.Length;
        if (n < 3) { dc.DrawText(Text("Radar braucht ≥ 3 Kategorien", 12, Muted, false, area.Width),
            new Point(area.X, area.Y + area.Height / 2)); return; }

        double cx = area.X + area.Width / 2, cy = area.Y + area.Height / 2 + 4;
        double r = Math.Min(area.Width, area.Height) / 2 - 30;
        double max = Math.Max(1, series.SelectMany(s => s).DefaultIfEmpty(0).Max());
        double step = NiceStep(max / 4), axisMax = Math.Ceiling(max / step) * step;

        Point Vertex(int i, double val) =>
            new(cx + r * (val / axisMax) * Math.Sin(2 * Math.PI * i / n),
                cy - r * (val / axisMax) * Math.Cos(2 * Math.PI * i / n));

        // Netz
        var gridPen = new Pen(Grid, 1);
        for (int ring = 1; ring <= 4; ring++)
        {
            var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(Vertex(0, axisMax * ring / 4), false, true);
                for (int i = 1; i < n; i++) ctx.LineTo(Vertex(i, axisMax * ring / 4), true, false);
            }
            dc.DrawGeometry(null, gridPen, g);
        }
        for (int i = 0; i < n; i++)
        {
            dc.DrawLine(gridPen, new Point(cx, cy), Vertex(i, axisMax));
            var lp = Vertex(i, axisMax * 1.12);
            dc.DrawText(Text(cats[i], 10.5, Muted, false, 70), new Point(lp.X - 35, lp.Y - 7));
        }

        // Reihen als Polygone
        for (int si = 0; si < series.Count; si++)
        {
            var s = series[si];
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(Vertex(0, s.Length > 0 ? s[0] : 0), true, true);
                for (int i = 1; i < n; i++) ctx.LineTo(Vertex(i, i < s.Length ? s[i] : 0), true, false);
            }
            var col = palette[si % palette.Count];
            dc.DrawGeometry(new SolidColorBrush(col) { Opacity = 0.18 },
                new Pen(new SolidColorBrush(col), 2), geo);
        }
    }

    // ==================== Kuchen ====================

    private static void DrawPie(DrawingContext dc, string[] cats, double[] values, IReadOnlyList<Color> palette,
        int w, int h, double top)
    {
        double total = values.Sum();
        if (total <= 0) return;
        double cx = w * 0.34, cy = top + (h - top) / 2, r = Math.Min(cx - 20, (h - top) / 2 - 16);
        double angle = -90;
        for (int i = 0; i < values.Length; i++)
        {
            double sweep = values[i] / total * 360;
            dc.DrawGeometry(new SolidColorBrush(palette[i % palette.Count]), null,
                PieSlice(cx, cy, r, angle, angle + sweep));
            angle += sweep;
        }
        double lx = w * 0.66, ly = top + 10;
        for (int i = 0; i < values.Length; i++)
        {
            dc.DrawRectangle(new SolidColorBrush(palette[i % palette.Count]), null, new Rect(lx, ly + i * 22, 14, 14));
            string cat = i < cats.Length ? cats[i] : (i + 1).ToString();
            dc.DrawText(Text($"{cat} · {values[i] / total * 100:0}%", 12, Muted, false, 150),
                new Point(lx + 20, ly + i * 22 - 1));
        }
    }

    private static void DrawLegend(DrawingContext dc, string[] names, IReadOnlyList<Color> palette, double x, double y)
    {
        for (int i = 0; i < names.Length; i++)
        {
            dc.DrawRectangle(new SolidColorBrush(palette[i % palette.Count]), null, new Rect(x, y + i * 22, 14, 14));
            dc.DrawText(Text(names[i], 12, Muted, false, 100), new Point(x + 20, y + i * 22 - 1));
        }
    }

    // ==================== Helfer ====================

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
            MaxTextWidth = Math.Max(1, maxW), MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis,
            TextAlignment = align,
        };

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Redraw();
        if (ResultImage == null) { ErrorText.Text = "Bitte gültige Werte eingeben."; return; }
        DialogResult = true;
    }
}
