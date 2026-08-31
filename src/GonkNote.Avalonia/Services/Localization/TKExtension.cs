using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace GonkNote.Services;

/// <summary>
/// Markup-Erweiterung <c>{loc:TK Beschriftung|Kuerzel}</c> — eine Menü-Kopfzeile aus
/// <b>Text links und Tastenkürzel rechts</b>, beides übersetzt.
///
/// <para>
/// <b>Warum es das gibt, und warum nicht <c>InputGesture</c></b> (HANDOFF §4.72). Avalonias
/// <c>MenuItem</c> nimmt für das Kürzel eine <c>KeyGesture</c> und zeichnet sie über den
/// <c>PlatformKeyGestureConverter</c> — also in <b>englischer</b> Schreibweise. Im deutschen
/// Menü stand damit „Ctrl+S", während der WPF-Kopf „Strg+S" zeigte; beide Texte stehen
/// längst in der Sprachtabelle (<c>Shortcut.Save</c>), nur kam die dort nie an.
/// </para>
/// <para>
/// <b>Umstylen ging nicht:</b> Das Fluent-Template hat für das Kürzel <b>keinen benannten
/// Teil</b> — an der gebundenen Fassung 12.1.1 nachgesehen, es gibt nur
/// <c>PART_IconPresenter</c> und <c>PART_ExpandCollapseChevron</c>. Ein Selektor
/// <c>/template/ TextBlock</c> träfe die Beschriftung mit.
/// </para>
/// <para>
/// <b>Deshalb die Kopfzeile selbst:</b> ein <see cref="DockPanel"/> mit dem Kürzel rechts.
/// Das ist dieselbe Anordnung, die WPF mit <c>InputGestureText</c> von Haus aus liefert —
/// und der Text kommt aus <b>derselben</b> Tabelle, also niemals aus zwei Quellen
/// (Dauerregel 1).
/// </para>
/// <para>
/// <b>Beide Texte bleiben Bindungen</b> und werden nicht einmalig eingesetzt: Bei einem
/// Sprachwechsel schreibt sich das Menü sonst nicht neu — genau der Grund, aus dem
/// <see cref="TExtension"/> ebenfalls bindet (siehe dort und <see cref="LocText"/>).
/// </para>
/// </summary>
public sealed class TKExtension : MarkupExtension
{
    public TKExtension() { }

    /// <param name="keys">
    /// <c>Beschriftungsschlüssel|Kürzelschlüssel</c>. Ein Senkrechtstrich statt zweier
    /// Eigenschaften, damit die XAML-Zeile so kurz bleibt wie das <c>{loc:T …}</c> daneben.
    /// </param>
    public TKExtension(string keys)
    {
        int strich = keys.IndexOf('|');
        if (strich < 0) { Key = keys; return; }

        Key = keys[..strich];
        ShortcutKey = keys[(strich + 1)..];
    }

    public string Key { get; set; } = "";

    public string ShortcutKey { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // **`AccessText` und nicht `TextBlock`** — und das ist ein am laufenden Programm
        // gefundener Unterschied, kein Vorgriff: Die Beschriftungen tragen den
        // Zugriffstasten-Strich („_Speichern"). Ein `MenuItem` mit einer Zeichenkette als
        // Kopfzeile setzt von sich aus ein `AccessText` ein, das ihn auswertet und
        // wegnimmt. Ein roher `TextBlock` zeigte ihn — im Bild stand „_Speichern".
        var text = new AccessText { VerticalAlignment = VerticalAlignment.Center };
        text.Bind(AccessText.TextProperty, new Binding
        {
            Source = LocTexte.Fuer(Key),
            Path = nameof(LocText.Value),
            Mode = BindingMode.OneWay,
        });

        var kuerzel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            // Derselbe Abstand und dieselbe Dämpfung wie im WPF-Menü. 32 px, weil darunter
            // Beschriftung und Kürzel bei kurzen Einträgen aneinanderkleben.
            Margin = new Avalonia.Thickness(32, 0, 0, 0),
            Opacity = 0.62,
        };
        kuerzel.Bind(TextBlock.TextProperty, new Binding
        {
            Source = LocTexte.Fuer(ShortcutKey),
            Path = nameof(LocText.Value),
            Mode = BindingMode.OneWay,
        });

        DockPanel.SetDock(kuerzel, Dock.Right);

        // `HorizontalAlignment.Stretch` ist der Grund, warum das Kürzel wirklich rechts
        // landet: ohne es schrumpft das Panel auf den Inhalt, und „rechts" wäre dann
        // unmittelbar hinter der Beschriftung.
        return new DockPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children = { kuerzel, text },
        };
    }
}
