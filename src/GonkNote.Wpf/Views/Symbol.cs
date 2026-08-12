using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using GonkNote.Core.Theming;

namespace GonkNote.Views;

/// <summary>
/// Ein Symbol aus der Tabelle in Core (<see cref="AppIcons"/>) — das Gegenstück zum
/// gleichnamigen Steuerelement im Avalonia-Kopf.
///
/// <para>
/// <b>Was hier verschwindet:</b> <c>Segoe Fluent Icons</c>. Bis zum 2026-08-12 standen in
/// diesem Kopf 91 Zeichencodes dieser Schrift (§4.31). Sie ist eine Windows-Systemschrift —
/// unter Linux und iPadOS fehlt sie, und mitliefern darf man sie nicht. Jedes dieser Symbole
/// war damit eine Antwort, die nur auf einem der drei Ziele richtig war.
/// </para>
/// <para>
/// <b>Der Kopf sieht dadurch anders aus als vorher</b>, und das war die Nutzer-Entscheidung
/// vom 2026-08-12: Die Segoe-Formen lassen sich nicht mitnehmen, nur ersetzen. Ersetzt sind
/// sie durch Lucide — derselbe dünne Strich, dieselbe Bedeutung, aber eine Form, die auf
/// allen drei Plattformen dieselbe ist.
/// </para>
/// </summary>
public sealed class Symbol : FrameworkElement
{
    // Geometrien werden zwischengespeichert und eingefroren: Parse laufe sonst bei jedem
    // Bildaufbau, und ein Ribbon baut dreißig Symbole auf einmal auf. Freeze macht sie
    // zusätzlich threadfrei und spart WPF das Änderungs-Nachhalten.
    private static readonly Dictionary<AppIcon, Geometry> Formen = new();

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(AppIcon), typeof(Symbol),
        new FrameworkPropertyMetadata(default(AppIcon), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(Symbol),
        new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>
    /// Die Strichfarbe. <b>Erbt</b> — dadurch färbt ein <c>Foreground</c> am Knopf das Symbol
    /// mit, genau wie es die Glyphenschrift vorher tat. Ohne die Vererbung müsste jede der
    /// 91 Fundstellen ihre Farbe selbst noch einmal sagen.
    /// </summary>
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(Symbol),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender |
            FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(Symbol),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <inheritdoc cref="GonkNote.Views.Symbol.Weight"/>
    public static readonly DependencyProperty WeightProperty = DependencyProperty.Register(
        nameof(Weight), typeof(double), typeof(Symbol),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Wofür das Symbol steht.</summary>
    public AppIcon Icon
    {
        get => (AppIcon)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Kantenlänge in Bildpunkten.</summary>
    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <inheritdoc cref="StrokeProperty"/>
    public Brush? Stroke
    {
        get => (Brush?)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>
    /// Füllfarbe — nur für geschlossene Formen sinnvoll (Stern, Pin). Die übrigen bestehen aus
    /// offenen Strichen und blieben gefüllt unsichtbar.
    /// </summary>
    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>
    /// Ein Faktor auf die Strichstärke aus der Tabelle — <b>kein</b> Ersatz für sie. Eine feste
    /// Stärke hinge am Kasten der Form und fiele bei den 16er- und den 24er-Formen verschieden
    /// aus.
    /// </summary>
    public double Weight
    {
        get => (double)GetValue(WeightProperty);
        set => SetValue(WeightProperty, value);
    }

    protected override Size MeasureOverride(Size verfuegbar) => new(Size, Size);

    protected override void OnRender(DrawingContext kontext)
    {
        var strich = Stroke ?? TextElement.GetForeground(this);
        if (strich is null && Fill is null) return;

        if (!Formen.TryGetValue(Icon, out var geometrie))
        {
            geometrie = Geometry.Parse(AppIcons.Shape(Icon).Path);
            geometrie.Freeze();
            Formen[Icon] = geometrie;
        }

        Pen? stift = null;
        if (strich is not null)
        {
            stift = new Pen(strich, AppIcons.StrokeFor(Icon) * Weight)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
            };
            stift.Freeze();
        }

        double m = AppIcons.Scale(Icon, Size);
        kontext.PushTransform(new ScaleTransform(m, m));
        kontext.DrawGeometry(Fill, stift, geometrie);
        kontext.Pop();
    }
}

/// <summary>
/// Ein Symbol als <c>Content</c> eines Knopfes: <c>Content="{views:Icon Undo}"</c>.
///
/// <para>
/// <b>Warum das nötig ist und im Avalonia-Kopf nicht.</b> Dort standen die Symbole schon als
/// eigene Elemente im XAML (<c>&lt;Path .../&gt;</c>) und ließen sich eins zu eins ersetzen.
/// Hier standen sie als <b>Attribut</b> — <c>Content="&amp;#xE7A7;"</c> —, und ein Attribut
/// gegen ein Kindelement zu tauschen heißt, 60 Knöpfe umzubauen statt 60 Werte zu ersetzen.
/// Mit dieser Erweiterung bleibt es ein Wert.
/// </para>
/// <para>
/// <b>Nicht in einem <c>Setter</c> benutzen.</b> Ein Setter teilt seinen Wert unter allen
/// Elementen, die den Stil tragen; ein <c>Symbol</c> ist aber ein Element und kann nur einen
/// Vater haben. WPF meldet das als „bereits ein logischer Vater vorhanden" — im Ribbon
/// gleichzeitig an dreißig Stellen. In einer <c>ControlTemplate</c> ist es dagegen
/// unbedenklich: die wird je Steuerelement neu aufgebaut.
/// </para>
/// </summary>
[MarkupExtensionReturnType(typeof(Symbol))]
public sealed class IconExtension : MarkupExtension
{
    public IconExtension() { }

    public IconExtension(AppIcon icon) => Icon = icon;

    /// <summary>Wofür das Symbol steht.</summary>
    public AppIcon Icon { get; set; }

    /// <summary>Kantenlänge in Bildpunkten.</summary>
    public double Size { get; set; } = 16.0;

    /// <inheritdoc cref="Symbol.Weight"/>
    public double Weight { get; set; } = 1.0;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Symbol
        {
            Icon = Icon,
            Size = Size,
            Weight = Weight,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
}
