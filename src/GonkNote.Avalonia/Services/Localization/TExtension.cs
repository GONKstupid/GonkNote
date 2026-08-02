using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace GonkNote.Services;

/// <summary>
/// Markup-Erweiterung <c>{loc:T Schlüssel}</c> – bindet an den übersetzten Text.
/// <para>
/// Das Gegenstück zu <c>src/GonkNote.Wpf/Services/Localization/TExtension.cs</c>. Beide
/// tun dasselbe und können es trotzdem nicht teilen: <c>MarkupExtension</c> und
/// <c>Binding</c> heißen in beiden Toolkits gleich und sind verschiedene Typen. Genau
/// deshalb ist <c>TExtension</c> in Phase 0 aus <c>Loc.cs</c> herausgetrennt worden — die
/// Tabellen und <c>Loc</c> selbst liegen in Core und werden geteilt (HANDOFF §7).
/// </para>
/// <para>
/// <b>Der Namensraum <c>GonkNote.Services</c> ist Pflicht, nicht Geschmack.</b> In den
/// XAML-Dateien steht <c>xmlns:loc="clr-namespace:GonkNote.Services"</c> ohne
/// <c>assembly=</c>-Angabe, zeigt also auf die <i>eigene</i> Assembly. Nur weil diese Klasse
/// hier denselben Namensraum trägt wie ihr WPF-Zwilling, sieht die Zeile in beiden Köpfen
/// identisch aus — und <c>Loc</c> aus Core steht darunter ebenfalls in
/// <c>GonkNote.Services</c>, sodass beides zusammenpasst.
/// </para>
/// </summary>
public sealed class TExtension : MarkupExtension
{
    public TExtension() { }

    public TExtension(string key) => Key = key;

    public string Key { get; set; } = "";

    /// <summary>
    /// Eine Bindung auf einen eigenen Träger — kein fester Text. Der Unterschied zeigt sich
    /// beim Sprachwechsel: der Träger meldet seinen neuen Wert, und die Oberfläche schreibt
    /// sich neu, ohne dass die App neu starten muss.
    /// <para>
    /// <b>Nicht auf <c>Loc.Source["Schlüssel"]</c> wie im WPF-Kopf.</b> Avalonia frischt
    /// eine Indexer-Bindung bei der Sammelmeldung von <c>LocSource</c> nicht auf — die
    /// Begründung samt Fehlerbild steht in <see cref="LocText"/>.
    /// </para>
    /// </summary>
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding
        {
            Source = LocTexte.Fuer(Key),
            Path = nameof(LocText.Value),
            Mode = BindingMode.OneWay,
        };
}
