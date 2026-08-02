using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using GonkNote.Core.Models;
using GonkNote.Core.Theming;
using GonkNote.Platform;

namespace GonkNote.Views;

/// <summary>
/// Die Gegenstücke zu <c>src/GonkNote.Wpf/Views/Converters.cs</c>.
/// <para>
/// Sie sind der Grund, warum die ViewModels seit Phase 2 Farben als Hex-Text und Bilder als
/// <c>byte[]</c> liefern (HANDOFF §4.7): ein <c>Brush</c> und eine <c>BitmapImage</c> sind
/// WPF, eine Farbe und ein paar Bytes sind es nicht. Was hier steht, ist genau die
/// Übersetzung zurück in Avalonia-Typen — mehr nicht.
/// </para>
/// </summary>
internal static class Umrechnung
{
    /// <summary>Aus der plattformneutralen Farbe die Avalonia-Farbe.</summary>
    public static Color ToAvalonia(this HexColor c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    /// <summary>
    /// Ein unveränderlicher Pinsel. Unveränderlich, weil derselbe Pinsel an vielen Kacheln
    /// hängt — ein gemeinsam benutzter veränderlicher Pinsel ist eine Fehlerquelle, die erst
    /// auffällt, wenn irgendwo etwas die Farbe umsetzt.
    /// </summary>
    public static IBrush ToBrush(this HexColor c) => new ImmutableSolidColorBrush(c.ToAvalonia());
}

/// <summary>
/// Farbe als Zeichenkette (<c>„#RRGGBB"</c>) zu einem Pinsel; <c>null</c> oder ein
/// unbrauchbarer Wert ergibt das Türkis des aktiven Themes.
/// <para>
/// Der Rückfall kommt aus <see cref="AvaloniaThemeHost.Current"/> und nicht aus einer
/// Ressourcensuche: so steht er auch schon zur Verfügung, bevor das erste Fenster existiert,
/// und die Kette ist dieselbe wie im WPF-Kopf — nach einem Theme-Wechsel meldet
/// <c>MainViewModel.RefreshAllIcons</c> die Symbolfarben als geändert, die Bindung läuft neu
/// und holt sich hier das neue Türkis.
/// </para>
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string hex && HexColor.TryParse(hex, out var farbe)
            ? farbe.ToBrush()
            : AvaloniaThemeHost.Current[ThemeColor.Turquoise].ToBrush();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Zwei Hex-Farben zu einem diagonalen Farbverlauf (Cover ohne Bild). Fehlt eine, gilt
/// das Standardpaar Blau→Lila wie beim echten Cover — dieselben zwei Werte wie im WPF-Kopf.
/// </summary>
public sealed class GradientBrushConverter : IMultiValueConverter
{
    private static readonly HexColor RueckfallStart = new(0xFF, 0x1E, 0x3A, 0x8A);
    private static readonly HexColor RueckfallEnde = new(0xFF, 0x7C, 0x3A, 0xED);

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var von = Lies(values.ElementAtOrDefault(0), RueckfallStart);
        var bis = Lies(values.ElementAtOrDefault(1), RueckfallEnde);

        return new ImmutableLinearGradientBrush(
            [new ImmutableGradientStop(0, von.ToAvalonia()), new ImmutableGradientStop(1, bis.ToAvalonia())],
            startPoint: new RelativePoint(0, 0, RelativeUnit.Relative),
            endPoint: new RelativePoint(1, 1, RelativeUnit.Relative));
    }

    private static HexColor Lies(object? value, HexColor rueckfall) =>
        value is string hex ? HexColor.Parse(hex, rueckfall) : rueckfall;
}

/// <summary>
/// Bild-Bytes zu einer Vorschau. <see cref="DecodeWidth"/> begrenzt die Auflösung — eine
/// Kachel braucht keine 4000 Pixel, und das RAM-Ziel der App hängt genau an solchen Stellen.
/// </summary>
public sealed class BytesToImageConverter : IValueConverter
{
    /// <summary>Wie beim WPF-Konverter: 240 px, das Doppelte der Kachelbreite.</summary>
    public int DecodeWidth { get; set; } = 240;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } bytes) return null;
        try
        {
            using var ms = new MemoryStream(bytes);
            return Bitmap.DecodeToWidth(ms, DecodeWidth);
        }
        catch
        {
            // Ein einziges unbrauchbares Blob darf nicht die ganze Galerie abreißen — die
            // Kachel bleibt dann eben bei ihrem Farbverlauf. Dieselbe Haltung wie
            // WbImages.Decode in Core (HANDOFF §7).
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// „Ist diese Zahl 0?" — für „keine Registerkarte offen" und „leerer Ordner".
/// <para>
/// Das Gegenstück im WPF-Kopf heißt <c>ZeroToVisibilityConverter</c> und liefert eine
/// <c>Visibility</c>. Avalonia bindet stattdessen an <c>IsVisible</c> (ein <c>bool</c>) und
/// braucht deshalb weder diesen Typ noch die beiden Bool-Konverter daneben.
/// </para>
/// </summary>
public sealed class IsZeroConverter : IValueConverter
{
    /// <summary>Dreht die Antwort um: „ist etwas da?"</summary>
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is int i && i == 0) != Invert;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Die Art eines Eintrags zu seinem Symbol.
/// <para>
/// <b>Warum der Kopf hier etwas anderes tut als der WPF-Kopf:</b> dort liefert
/// <c>TreeItemViewModel.IconGlyph</c> ein Zeichen aus „Segoe Fluent Icons" — eine Schrift,
/// die es unter Linux nicht gibt und die sich auch nicht mitliefern lässt (keine Lizenz
/// dafür). Der Eintrag wäre dort ein leeres Kästchen.
/// </para>
/// <para>
/// Also Vektorformen, so wie der WPF-Kopf es an den Stellen schon macht, an denen die
/// Icon-Schrift nichts Passendes hat (<c>Icon.Whiteboard</c> und Nachbarn in
/// <c>Themes/Styles.xaml</c>). <c>IconGlyph</c> bleibt unberührt im ViewModel stehen — es
/// ist die Antwort des Windows-Kopfs auf dieselbe Frage.
/// </para>
/// </summary>
public sealed class KindToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string name = value is ItemKind kind ? kind switch
        {
            ItemKind.Folder => "Icon.Folder",
            ItemKind.Notebook => "Icon.Notebook",
            ItemKind.Whiteboard => "Icon.Whiteboard",
            ItemKind.TextDocument => "Icon.Text",
            _ => "Icon.Folder",
        } : "Icon.Folder";

        return Application.Current?.TryFindResource(name, out var form) == true ? form : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
