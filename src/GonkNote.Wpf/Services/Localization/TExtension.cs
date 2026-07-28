using System.Windows.Data;
using System.Windows.Markup;

namespace GonkNote.Services;

/// <summary>
/// Markup-Erweiterung <c>{loc:T Schlüssel}</c> – bindet an den übersetzten Text.
/// <para>
/// Steckte bis zum Umzug nach V2 in <c>Loc.cs</c>. Sie ist der einzige Teil der
/// Lokalisierung mit WPF-Bezug (<c>Binding</c>/<c>MarkupExtension</c>) und bleibt deshalb
/// im Plattform-Kopf; die Tabellen und <c>Loc</c> selbst liegen jetzt in
/// <c>GonkNote.Core/Localization/</c>. Der Namensraum bleibt bewusst
/// <c>GonkNote.Services</c>, damit <c>xmlns:loc="clr-namespace:GonkNote.Services"</c> in
/// allen XAML-Dateien unverändert weiterläuft.
/// </para>
/// </summary>
public sealed class TExtension : MarkupExtension
{
    public TExtension() { }

    public TExtension(string key) => Key = key;

    [ConstructorArgument("key")]
    public string Key { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]")
        {
            Source = Loc.Source,
            Mode = BindingMode.OneWay,
        }.ProvideValue(serviceProvider);
}
