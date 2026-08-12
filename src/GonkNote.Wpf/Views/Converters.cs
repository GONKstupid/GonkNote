using System.Globalization;
using GonkNote.Core.Models;
using GonkNote.Core.Theming;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GonkNote.Views;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is true;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Farbe als Zeichenkette (<c>„#RRGGBB"</c>) zu einem Pinsel; <c>null</c> oder ein
/// unbrauchbarer Wert ergibt das Theme-Türkis.
/// <para>
/// Seit Phase 2 liefern die ViewModels die Symbolfarbe als Hex-Text statt als
/// <see cref="Brush"/> — ein Pinsel ist WPF, eine Farbe nicht. Der Rückfall auf die
/// Theme-Farbe gehört hierher, weil nur der Kopf ein <c>ResourceDictionary</c> hat.
/// </para>
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && hex.Length > 0)
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                brush.Freeze();
                return brush;
            }
            catch { /* ungültiger Wert → Standard */ }
        }
        return Application.Current.Resources["Brush.Turquoise"];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Zwei Hex-Farben zu einem diagonalen Farbverlauf (Cover ohne Bild). Fehlt eine, gilt
/// das Standardpaar Blau→Lila wie beim echten Cover.
/// </summary>
public sealed class GradientBrushConverter : IMultiValueConverter
{
    private static readonly Color FallbackStart = Color.FromRgb(0x1E, 0x3A, 0x8A);
    private static readonly Color FallbackEnd = Color.FromRgb(0x7C, 0x3A, 0xED);

    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var brush = new LinearGradientBrush(
            Parse(values.ElementAtOrDefault(0), FallbackStart),
            Parse(values.ElementAtOrDefault(1), FallbackEnd),
            new Point(0, 0), new Point(1, 1));
        brush.Freeze();
        return brush;
    }

    private static Color Parse(object? value, Color fallback)
    {
        if (value is string hex && hex.Length > 0)
            try { return (Color)ColorConverter.ConvertFromString(hex); } catch { /* Standard */ }
        return fallback;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Bild-Bytes zu einer Vorschau. <see cref="DecodeWidth"/> begrenzt die Auflösung — eine
/// Kachel braucht keine 4000 Pixel, und das RAM-Ziel der App hängt genau an solchen Stellen.
/// <para>
/// Die Bytes kommen aus dem ViewModel, das Dekodieren bleibt im Kopf: <c>BitmapImage</c>
/// gibt es unter Avalonia nicht.
/// </para>
/// </summary>
public sealed class BytesToImageConverter : IValueConverter
{
    public int DecodeWidth { get; set; } = 240;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } bytes) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.DecodePixelWidth = DecodeWidth;
            bmp.StreamSource = new MemoryStream(bytes);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Die Art eines Eintrags zu ihrem Symbol — das Gegenstück zum gleichnamigen Konverter im
/// Avalonia-Kopf, und seit dem 2026-08-12 mit derselben Antwort (§4.31).
///
/// <para>
/// <b>Vorher stand hier nichts, und das war das Problem.</b> Der Baum band an
/// <c>TreeItemViewModel.IconGlyph</c> — vier Segoe-Zeichencodes, mitten in einer Assembly,
/// die WPF-frei sein soll (§4.2). Weil die Icon-Schrift kein Whiteboard kennt, hing daneben
/// eine zweite Form als <c>Path</c>, unsichtbar geschaltet, plus ein <c>DataTrigger</c>, der
/// zwischen beiden umschaltete. <b>Drei Bauteile für „welches Symbol gehört zu dieser
/// Art".</b> Mit einer Tabelle in Core ist es eine Bindung.
/// </para>
/// </summary>
public sealed class KindToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ItemKind kind ? AppIcons.ForKind(kind) : AppIcon.Folder;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
