using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace GonkNote;

/// <summary>
/// Die einblendbare Titelleiste des maximierten Fensters.
///
/// <para>
/// <b>Wozu:</b> „in Groß" soll das Blatt zeigen und nicht die Fensterzier. Solange das
/// Fenster maximiert ist, verschwindet deshalb die Leiste des Systems; an ihre Stelle tritt
/// eine eigene, die bei Zeigerkontakt am oberen Rand hereingleitet. Im normalen Fenster
/// bleibt alles, wie es war — dort gehört die Zier dem System.
/// </para>
///
/// <para>
/// <b>Deutlich weniger Aufwand als im WPF-Kopf</b>, und zwar aus einem Grund, der
/// hierhergehört: dort muss ein <c>WM_GETMINMAXINFO</c>-Hook die Maximier-Größe von Hand
/// auf den Arbeitsbereich begrenzen (<c>WindowBounds</c>), weil ein randloses Fenster unter
/// Windows sonst über den Bildschirm hinausragt und die Taskleiste verdeckt. Unter X11
/// maximiert der Fenstermanager gegen <c>_NET_WORKAREA</c> — die Frage stellt sich nicht,
/// und <c>WindowBounds</c> bleibt zu Recht Windows-only (HANDOFF §6, Phase 2 Schritt 1).
/// </para>
/// </summary>
public partial class MainWindow
{
    private bool _titelleisteDraussen;

    /// <summary>Eingefahren: Höhe 34 plus etwas Rand, damit auch der Schatten verschwindet.</summary>
    private static readonly ITransform Eingefahren = TransformOperations.Parse("translateY(-40px)");
    private static readonly ITransform Ausgefahren = TransformOperations.Parse("translateY(0px)");

    private void TitelleisteEinhaengen()
    {
        // Auf den Fensterzustand hören. Avalonia hat kein `StateChanged` wie WPF; der
        // Zustand ist eine ganz gewöhnliche Eigenschaft, und dafür gibt es diesen Weg.
        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty) MaximierZier();
        };

        // Tunnel: der Zeiger meldet sich sonst nur, solange er über etwas liegt, das ihn
        // nicht selbst verbraucht — über der Zeichenfläche etwa käme nichts an, und genau
        // dort will man die Leiste hervorholen.
        AddHandler(PointerMovedEvent, Fenster_ZeigerBewegt, RoutingStrategies.Tunnel);

        MaximierZier();
    }

    /// <summary>
    /// Schaltet zwischen „Fensterzier des Systems" und „eigene Leiste" um. Läuft bei jedem
    /// Zustandswechsel und einmal beim Start — ein Fenster kann bereits maximiert
    /// hochkommen.
    /// </summary>
    private void MaximierZier()
    {
        if (WindowState == WindowState.Maximized)
        {
            // `WindowDecorations`, nicht das seit Avalonia 12 veraltete `SystemDecorations`.
            WindowDecorations = WindowDecorations.None;
            AutoTitelleiste.IsVisible = true;
            Einfahren(sofort: true);
        }
        else
        {
            WindowDecorations = WindowDecorations.Full;
            AutoTitelleiste.IsVisible = false;
            _titelleisteDraussen = false;
        }
    }

    /// <summary>
    /// Holt die Leiste hervor, wenn der Zeiger den oberen Rand erreicht, und schickt sie
    /// zurück, sobald er weit genug weg ist. Die beiden Grenzen (12 und 48) sind
    /// verschieden, damit die Leiste nicht flattert, während der Zeiger auf ihr liegt —
    /// dieselben Werte wie im WPF-Kopf.
    /// </summary>
    private void Fenster_ZeigerBewegt(object? sender, PointerEventArgs e)
    {
        if (WindowState != WindowState.Maximized) return;

        double y = e.GetPosition(this).Y;
        if (!_titelleisteDraussen && y <= 12) Ausfahren();
        else if (_titelleisteDraussen && y > 48) Einfahren(sofort: false);
    }

    private void Ausfahren()
    {
        _titelleisteDraussen = true;
        AutoTitelleiste.RenderTransform = Ausgefahren;
    }

    /// <param name="sofort">
    /// Beim Umschalten in den maximierten Zustand darf die Leiste nicht erst hereingleiten
    /// und wieder verschwinden — sie startet eingefahren. Dafür wird der Übergang für einen
    /// Augenblick abgehängt; ihn nur zu setzen, ließe ihn animiert von 0 nach −40 laufen.
    /// </param>
    private void Einfahren(bool sofort)
    {
        _titelleisteDraussen = false;

        if (!sofort)
        {
            AutoTitelleiste.RenderTransform = Eingefahren;
            return;
        }

        var uebergaenge = AutoTitelleiste.Transitions;
        AutoTitelleiste.Transitions = null;
        AutoTitelleiste.RenderTransform = Eingefahren;
        AutoTitelleiste.Transitions = uebergaenge;
    }

    // ---------- Die Knöpfe ----------

    private void Fenster_Minimieren(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Fenster_Wiederherstellen(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Normal;

    private void Fenster_Schliessen(object? sender, RoutedEventArgs e) => Close();

    /// <summary>Doppeltipp auf die eingeblendete Leiste stellt das Fenster wieder her.</summary>
    private void AutoTitelleiste_DoppelTipp(object? sender, TappedEventArgs e)
    {
        WindowState = WindowState.Normal;
        e.Handled = true;
    }
}
