using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GonkNote.Core.Theming;

// Der Namensraum heißt GonkNote.Views und nicht GonkNote.Avalonia.Views — obwohl die
// Assembly GonkNote.Avalonia heißt. Das ist kein Versehen, sondern Notwehr: Ein Namensraum
// „GonkNote.Avalonia" verdeckt in jeder Datei unterhalb von „GonkNote" die echte Wurzel
// „Avalonia", und dann findet `Avalonia.Interactivity.RoutedEventArgs` plötzlich nichts mehr.
namespace GonkNote.Views;

/// <summary>
/// Ein Symbol aus der Tabelle in Core (<see cref="AppIcons"/>) — das Gegenstück zu
/// <c>GonkNote.Wpf.Views.Symbol</c>.
///
/// <para>
/// <b>Warum ein eigenes Steuerelement und nicht weiter <c>Path</c> mit <c>Data</c>.</b> Eine
/// Form aus der Tabelle bringt ihren eigenen Kasten mit (16 oder 24, siehe
/// <see cref="IconShape"/>) und muss auf die gewünschte Kantenlänge skaliert werden. Mit
/// <c>Path</c> hieße das an <b>jeder</b> der Fundstellen eine <c>RenderTransform</c> und eine
/// dazu passende <c>StrokeThickness</c> — zwei Zahlen, die zueinander stimmen müssen, an
/// über dreißig Stellen. Hier stehen sie einmal.
/// </para>
/// <para>
/// <b><c>Stretch</c> wäre der naheliegende Ausweg und ist der falsche:</b> Es skaliert auf die
/// <i>Ausdehnung der Form</i>, nicht auf ihren Kasten. Ein Minuszeichen (breit und flach)
/// käme dann genauso groß heraus wie ein Quadrat, und in einer Werkzeugleiste stünden lauter
/// verschieden große Symbole nebeneinander.
/// </para>
/// </summary>
public sealed class Symbol : Control
{
    // Geometrien werden zwischengespeichert: Parse läuft sonst bei jedem Bildaufbau, und
    // ein Bildlauf über den Ordnerbaum baut Dutzende Symbole neu auf. Dieselbe Überlegung
    // wie beim Schriftzwischenspeicher in WbFonts (§4.26).
    private static readonly Dictionary<AppIcon, Geometry> Formen = new();

    public static readonly StyledProperty<AppIcon> IconProperty =
        AvaloniaProperty.Register<Symbol, AppIcon>(nameof(Icon));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<Symbol, double>(nameof(Size), 16.0);

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<Symbol, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<Symbol, IBrush?>(nameof(Fill));

    /// <summary>
    /// Ein leicht dickerer oder dünnerer Strich als das Zwölftel aus der Tabelle.
    /// <b>Ein Faktor und keine Stärke</b> — eine feste Stärke hinge am Kasten der Form und
    /// fiele bei den 16er- und den 24er-Formen verschieden aus.
    /// </summary>
    public static readonly StyledProperty<double> WeightProperty =
        AvaloniaProperty.Register<Symbol, double>(nameof(Weight), 1.0);

    static Symbol()
    {
        AffectsRender<Symbol>(IconProperty, StrokeProperty, FillProperty, WeightProperty);
        AffectsMeasure<Symbol>(SizeProperty);
    }

    /// <summary>Wofür das Symbol steht.</summary>
    public AppIcon Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Kantenlänge in Bildpunkten.</summary>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Strichfarbe. Ohne sie wird nichts gezeichnet.</summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>
    /// Füllfarbe — nur für geschlossene Formen sinnvoll (Stern, Pin). Die übrigen bestehen
    /// aus offenen Strichen und blieben gefüllt unsichtbar.
    /// </summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <inheritdoc cref="WeightProperty"/>
    public double Weight
    {
        get => GetValue(WeightProperty);
        set => SetValue(WeightProperty, value);
    }

    protected override global::Avalonia.Size MeasureOverride(global::Avalonia.Size verfuegbar) =>
        new(Size, Size);

    public override void Render(DrawingContext kontext)
    {
        if (Stroke is null && Fill is null) return;

        var form = AppIcons.Shape(Icon);
        if (!Formen.TryGetValue(Icon, out var geometrie))
        {
            geometrie = Geometry.Parse(form.Path);
            Formen[Icon] = geometrie;
        }

        var stift = Stroke is null
            ? null
            : new Pen(Stroke, AppIcons.StrokeFor(Icon) * Weight,
                      lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

        double m = AppIcons.Scale(Icon, Size);
        using (kontext.PushTransform(Matrix.CreateScale(m, m)))
            kontext.DrawGeometry(Fill, stift, geometrie);
    }
}
