using System.Windows.Media;
using GonkNote.Core.Models;
using GonkNote.Core.Theming;
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

    /// <summary>
    /// Die Farbe, mit der gezeichnet wird. „auto" heißt: dunkel auf hellem, hell auf dunklem
    /// Papier — <b>und die Vorgabefarbe kommt aus der Farbtabelle in Core</b>.
    ///
    /// <para>
    /// ⛔ <b>Hier standen bis §4.79 zwei feste Werte:</b> <c>"#FF000000"</c> und
    /// <c>"#FFFFFFFF"</c>. Der Linux-Kopf nahm an derselben Stelle
    /// <c>Themes.Light[ThemeColor.DefaultInk]</c> = <c>#1B2B4B</c> — <b>mit derselben Kachel
    /// „auto" schrieben die beiden Köpfe also in verschiedenen Farben.</b> Beim Fotografieren
    /// nebeneinander gesehen (§4.78): dieselbe Seite, vier dunkelblaue Formen aus dem einen
    /// Kopf, zwei tiefschwarze aus dem anderen.
    /// </para>
    /// <para>
    /// <b>Das ist §5 Nr. 27 — „nie ein fester Farbwert, immer einer aus der Tabelle in
    /// Core"</b> —, und es war der schlimmere Fall davon: Es betrifft nicht das Aussehen der
    /// Oberfläche, sondern <b>die gespeicherten Daten</b>. Ein Dokument, das auf beiden
    /// Rechnern bearbeitet wird, bekommt zwei verschiedene Schwarztöne, und man sieht es erst
    /// nebeneinander.
    /// </para>
    /// <para>
    /// <b>Der Kommentar im Linux-Kopf benannte die Regel wörtlich, gegen die dieser hier
    /// verstieß</b> — nachzulesen in <c>WhiteboardView.axaml.cs</c>, <c>AutoTinte</c>.
    /// *Zwei Fassungen derselben Entscheidung, und die eine wusste von der anderen nichts.*
    /// </para>
    /// </summary>
    private string CurrentInkHex()
    {
        if (!string.IsNullOrEmpty(_colorTag) && _colorTag != "auto") return _colorTag;
        return AutoTinte().ToString();
    }

    /// <summary>
    /// Die Vorgabetinte zur Seite. <b>Sie gehört zum Papier, nicht zur App</b> — bei einem
    /// festgelegten Farbton zählt <b>der</b>, und nur bei <see cref="PageShade.Auto"/> folgt
    /// sie dem Theme. <see cref="EffectiveShade"/> beantwortet genau das.
    /// </summary>
    private HexColor AutoTinte() =>
        (EffectiveShade(_page) == PageShade.Dark ? Themes.Dark : Themes.Light)[ThemeColor.DefaultInk];

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

        // **Dieselbe Quelle wie die Tinte selbst** (§4.79). Hier standen Colors.White und
        // Colors.Black — die Kachel zeigte damit eine andere Farbe an, als der Stift
        // schrieb, sobald man den Wert nur an einer der beiden Stellen ändert.
        // *Eine Vorschau, die aus einer zweiten Quelle kommt, ist keine Vorschau.*
        var tinte = AutoTinte();
        AutoSwatch.Background = new SolidColorBrush(Color.FromArgb(tinte.A, tinte.R, tinte.G, tinte.B));
    }
}
