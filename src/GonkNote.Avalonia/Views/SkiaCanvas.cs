using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>Was ein Zeichner braucht: die Leinwand, ihre Größe und die Bildschirmauflösung.</summary>
/// <param name="Canvas">
/// <b>Achtung: eine aufzeichnende Leinwand</b>, nicht die des Bildschirms. Sie nimmt die
/// Zeichenbefehle nur entgegen; ausgeführt werden sie später auf dem Render-Faden. Alles,
/// was hier gezeichnet wird, muss deshalb ohne Rückgriff auf lebende Zustände auskommen —
/// die Begründung steht bei <see cref="SkiaCanvas"/>.
/// </param>
/// <param name="Bounds">Größe der Fläche in geräteunabhängigen Punkten, Ursprung oben links.</param>
/// <param name="Scaling">
/// Gerätepixel je Punkt (bei 200 % also 2). Avalonia rechnet das für alles, was auf dieser
/// Leinwand landet, selbst um — der Wert wird nur gebraucht, wo eine Zwischenfläche in
/// **Gerätepixeln** angelegt werden muss, damit sie nicht unscharf wird.
/// </param>
public readonly record struct SkiaPaintArgs(SKCanvas Canvas, SKRect Bounds, float Scaling);

/// <summary>
/// Die Fläche, auf der <c>WbRenderer</c> zeichnet — Avalonias Gegenstück zu
/// <c>SKElement</c> aus <c>SkiaSharp.Views.WPF</c>.
///
/// <para>
/// <b>Der Weg zum SKCanvas: ausleihen, nicht nachbauen.</b> Ein offizielles
/// <c>SkiaSharp.Views.Avalonia</c> gibt es nicht — SkiaSharp liefert Views für WPF,
/// WinForms, MAUI und andere, für Avalonia nicht. Es gibt aber etwas Besseres:
/// <c>Avalonia.Skia</c> hängt an <b>derselben SkiaSharp-Fassung wie Core</b> (3.119.4) und
/// gibt über <see cref="ISkiaSharpApiLeaseFeature"/> seinen eigenen
/// <see cref="SKCanvas"/> heraus. Damit zeichnet der Renderer direkt in dasselbe
/// Ziel, in das Avalonia seine Steuerelemente zeichnet: keine Zwischenfläche, keine Kopie
/// je Bild, und auf der Grafikkarte, wo Avalonia sie benutzt. Der Weg über eine eigene
/// <c>SKSurface</c> und ein <c>WriteableBitmap</c> wäre eine volle Bildkopie pro Bild
/// gewesen — bei einem Digitizer, der schneller abtastet als die Oberfläche zeichnet,
/// genau die falsche Stelle zum Sparen. (Entscheidung des Nutzers, 2026-08-03.)
/// </para>
///
/// <para>
/// <b>Und die Falle dabei — Avalonia zeichnet auf einem eigenen Faden.</b> Das ist der
/// Unterschied zu WPF, der hier am meisten kostet: <c>SKElement.PaintSurface</c> läuft
/// unter WPF auf dem Oberflächen-Faden, <see cref="ICustomDrawOperation.Render"/> läuft
/// unter Avalonia auf dem <b>Render-Faden</b>. Wer von dort aus in <c>_page.Elements</c>
/// greift, liest eine Liste, die der Oberflächen-Faden im selben Moment verändert —
/// während ein Strich gezogen wird, also genau dann, wenn am meisten passiert. Das Ergebnis
/// wäre kein sauberer Absturz, sondern ein sporadisches <c>InvalidOperationException:
/// Collection was modified</c> mitten im Zeichnen.
/// </para>
///
/// <para>
/// <b>Deshalb wird aufgezeichnet statt zurückgegriffen.</b> Auf dem Oberflächen-Faden
/// nimmt ein <see cref="SKPictureRecorder"/> die Zeichenbefehle entgegen — dort, wo die
/// Daten leben und niemand sie nebenher ändert. Was dabei entsteht, ist ein
/// <see cref="SKPicture"/>: unveränderlich, fertig, ohne Verweis auf lebende Listen. Der
/// Render-Faden spielt es nur noch ab. Aufzeichnen kostet fast nichts (es werden Befehle
/// notiert, nichts gerastert), und der Gewinn ist doppelt: der Zugriff ist von sich aus
/// sicher, und weil ein Bild Vektoren behält statt Pixel, bleibt es beim Zoomen scharf —
/// anders als das Zwischenbild, das der WPF-Kopf für denselben Zweck rastert.
/// </para>
/// </summary>
public sealed class SkiaCanvas : Control
{
    /// <summary>
    /// Zeichnen. Läuft auf dem <b>Oberflächen-Faden</b> — lebende Zustände dürfen hier
    /// gelesen werden, das ist der ganze Sinn der Aufzeichnung.
    /// </summary>
    public event Action<SkiaPaintArgs>? Paint;

    public SkiaCanvas()
    {
        // Die Fläche nimmt die Tastatur selbst. Es genügt **nicht**, das umgebende
        // UserControl fokussierbar zu machen: Tastenereignisse laufen in Avalonia vom
        // Wurzelfenster zum fokussierten Element und wieder zurück — liegt der Fokus noch
        // im Ordnerbaum, kommt am Zeichenbereich nichts an. Das Fehlerbild ist unauffällig
        // und deshalb teuer: gezeichnet wird einwandfrei (der Zeiger braucht keinen Fokus),
        // nur die Tastenkürzel tun nichts.
        Focusable = true;
    }

    public override void Render(DrawingContext context)
    {
        var groesse = Bounds.Size;
        if (groesse.Width <= 0 || groesse.Height <= 0) return;

        var rahmen = SKRect.Create((float)groesse.Width, (float)groesse.Height);
        float skalierung = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);

        // Aufzeichnen — hier, auf dem Oberflächen-Faden.
        using var aufnahme = new SKPictureRecorder();
        var leinwand = aufnahme.BeginRecording(rahmen);
        Paint?.Invoke(new SkiaPaintArgs(leinwand, rahmen, skalierung));
        var bild = aufnahme.EndRecording();

        // Abspielen — dort, auf dem Render-Faden.
        context.Custom(new Wiedergabe(new Rect(groesse), bild));
    }

    /// <summary>
    /// Spielt ein fertiges <see cref="SKPicture"/> auf Avalonias eigener Leinwand ab.
    /// Enthält bewusst keine Zeichenlogik — sie stünde sonst auf dem falschen Faden.
    /// </summary>
    private sealed class Wiedergabe(Rect grenzen, SKPicture bild) : ICustomDrawOperation
    {
        public Rect Bounds => grenzen;

        /// <summary>
        /// Die Fläche fängt ihre Zeiger über das Steuerelement ab, nicht über den
        /// Zeichenschritt. Hier <c>false</c> zu melden hieße, dass Avalonia den Bereich für
        /// durchlässig hält.
        /// </summary>
        public bool HitTest(Point p) => grenzen.Contains(p);

        /// <summary>
        /// Immer <c>false</c>: jedes Bild trägt ein frisch aufgezeichnetes
        /// <see cref="SKPicture"/>. Zwei Aufzeichnungen als gleich zu melden, nur weil sie
        /// gleich aussehen, hieße den Zeichenschritt zu überspringen — und dafür müsste hier
        /// der Inhalt verglichen werden, nicht der Verweis.
        /// </summary>
        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            // Nicht-generisch abgefragt: die generische Fassung ist eine Erweiterungsmethode
            // und damit eine Fassungsfrage mehr, die niemand braucht.
            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature merkmal)
                return;   // kein Skia-Rücken (theoretisch: ein anderes Backend) — dann bleibt die Fläche leer

            using var leihe = merkmal.Lease();
            var leinwand = leihe.SkCanvas;

            // Die Leinwand trägt bereits Avalonias Abbildung: Ursprung an der linken oberen
            // Ecke dieses Steuerelements, Maßstab auf die Bildschirmauflösung. Es wird
            // deshalb in Punkten gezeichnet und nichts von Hand umgerechnet.
            leinwand.Save();
            leinwand.ClipRect(SKRect.Create((float)grenzen.Width, (float)grenzen.Height));
            leinwand.DrawPicture(bild);
            leinwand.Restore();
        }

        /// <summary>Avalonia ruft das nach dem Abspielen — hier stirbt die Aufzeichnung.</summary>
        public void Dispose() => bild.Dispose();
    }
}
