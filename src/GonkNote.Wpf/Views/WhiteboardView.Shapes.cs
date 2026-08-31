using System.Windows.Media;
using GonkNote.Core.Models;
using GonkNote.Services;
using SkiaSharp;
using GonkNote.Core.Platform;

namespace GonkNote.Views;

/// <summary>
/// Formen-Stift: erkennt aus einem freihaendigen Zug die gemeinte Form
/// (Gerade, Rechteck, Ellipse, Dreieck, Pfeil).
/// </summary>
public partial class WhiteboardView
{
    // ==================== Formen-Stift ====================
    //
    // Die Erkennung stand hier — 255 Zeilen Geometrie — und ist in Phase 5, Schritt ①c nach
    // Core gezogen (WbFormen). **Sie war nie an WPF gebunden**: Punktlisten hinein,
    // Modellobjekte hinaus. Was wirklich am Kopf hing, waren zwei Werte — die aktuelle Tinte
    // und die Strichbreite —, und die stehen jetzt als Parameter davor.
    //
    // Derselbe Fall wie der Tafel-Export in §4.77, nur ohne falschen Kommentar: die Datei lag
    // hier, weil sie hier entstanden ist. **Und sie war dadurch durch keinen einzigen Wächter
    // gedeckt** — rufbar nur mit einem WPF-Fenster. Jetzt deckt Core.Tests sie ab.
    //
    // Was unten stehen bleibt, gehört wirklich dem Kopf: welche Tinte gerade gilt (Seite,
    // Theme, Farbkachel) und die Farbkachel selbst.

    /// <summary>Effektiver Farbton der Seite (Auto folgt dem App-Theme).</summary>
    private static PageShade EffectiveShade(WbPage? page)
    {
        if (page != null && page.Shade != PageShade.Auto) return page.Shade;
        return App.Platform.Theme.Current == AppTheme.Dark ? PageShade.Dark : PageShade.Light;
    }

    private string CurrentInkHex()
    {
        if (!string.IsNullOrEmpty(_colorTag) && _colorTag != "auto") return _colorTag;
        // Standardtinte: Schwarz auf hellen, Weiß auf dunklen Seiten
        return EffectiveShade(_page) == PageShade.Dark ? "#FFFFFFFF" : "#FF000000";
    }

    /// <summary>
    /// Hält die erste Farbkachel synchron zur Seite: Schwarz auf hellen, Weiß auf
    /// dunklen Seiten. Wird aus dem Paint-Pfad aufgerufen (deckt Seitenwechsel,
    /// Farbton- und Theme-Wechsel ab) und ist per Cache-Feld praktisch kostenlos.
    /// </summary>
    private bool? _autoSwatchDark;

    private void RefreshAutoSwatch()
    {
        bool dark = EffectiveShade(_page) == PageShade.Dark;
        if (_autoSwatchDark == dark) return;
        _autoSwatchDark = dark;
        AutoSwatch.Background = new SolidColorBrush(dark ? Colors.White : Colors.Black);
    }
}
