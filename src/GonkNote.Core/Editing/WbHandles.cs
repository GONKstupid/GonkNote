using System;
using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Editing;

/// <summary>
/// Die Griffe an der Auswahl: wo sie sitzen, und was der Zeiger gerade anfasst.
///
/// <para>
/// <b>Warum das hier steht und nicht im Kopf.</b> Bis Phase 4.5 lag diese Geometrie privat in
/// <c>WhiteboardView.Selection.cs</c> des WPF-Kopfs, und <see cref="WbHit"/> hielt in seinem
/// Kopfkommentar ausdrücklich fest, die Griffe blieben im Kopf, weil sie „am Steuerelement
/// hängen". <b>Diese Begründung trägt nicht mehr</b>, und sie trug schon damals nur zur
/// Hälfte: was hier gerechnet wird, benutzt <see cref="WbRenderer.ElementBounds"/>,
/// <see cref="WbHit.Rotate"/> und <see cref="WbElement.Rotation"/> — alles aus Core — plus
/// <c>Zoom</c>. Und <c>Zoom</c> ist kein Steuerelement, sondern <b>eine Zahl</b>; als
/// Parameter übergeben, bleibt reine Geometrie übrig. Nach der Faustregel aus HANDOFF §3
/// gehört sie damit hierher.
/// </para>
/// <para>
/// <b>Der Anlass ist Phase 4.5.</b> Der Linux-Kopf kann bisher nur verschieben und löschen und
/// bekommt jetzt Drehen und Skalieren. Wer die Formeln dort abschreibt, hat zwei Fassungen
/// derselben Geometrie — genau die Lage, die §4.13 für die Trefferprüfung aufgelöst hat, und
/// sie fällt nicht auf: die Griffe säßen je Kopf ein paar Pixel anders.
/// </para>
/// <para>
/// <b>Was im Kopf bleibt:</b> der Zustand (was ist ausgewählt, was wird gerade gezogen), das
/// Entgegennehmen der Zeigerereignisse und das Anstoßen der Undo-Aktionen. Gerechnet wird hier,
/// gezeichnet in <see cref="WbSelectionRenderer"/>.
/// </para>
/// <para>
/// <b>Alle Maße sind Bildschirmpixel und werden durch <c>zoom</c> geteilt</b> — ein Griff soll
/// bei jeder Vergrößerung gleich groß aussehen und gleich leicht zu treffen sein.
/// </para>
/// </summary>
public static class WbHandles
{
    /// <summary>Abstand des Rahmens vom Element (Einzelauswahl).</summary>
    public const float PadPx = 10f;

    /// <summary>Länge des Arms, an dem der Dreh-Griff über der Oberkante hängt.</summary>
    public const float RotateArmPx = 28f;

    /// <summary>Fangradius eines Griffs der Einzelauswahl.</summary>
    public const float GrabRadiusPx = 13f;

    /// <summary>Abstand des achsenparallelen Kastens bei Mehrfachauswahl.</summary>
    public const float BoxPadPx = 12f;

    /// <summary>Fangradius des Skalier-Griffs am Kasten der Mehrfachauswahl.</summary>
    public const float BoxGrabRadiusPx = 12f;

    /// <summary>Halbe Kantenlänge bzw. Radius eines gezeichneten Griffs.</summary>
    public const float HandleSizePx = 6f;

    /// <summary>Innerhalb dieser Gradzahl rastet das Drehen auf ein Vielfaches von 15° ein.</summary>
    public const float SnapToleranceDeg = 3f;

    /// <summary>
    /// Die sechs Punkte der Einzelauswahl, <b>mitgedreht</b>: der Rahmen liegt um das
    /// achsenparallele Kästchen des Elements und wird anschließend samt Griffen um dessen
    /// Mittelpunkt gedreht.
    /// </summary>
    /// <param name="Rotate">Dreh-Griff, über der Oberkante.</param>
    /// <param name="Scale">Skalier-Griff — derselbe Punkt wie <paramref name="BR"/>.</param>
    public readonly record struct Set(
        SKPoint Rotate, SKPoint Scale, SKPoint TL, SKPoint TR, SKPoint BR, SKPoint BL);

    /// <summary>Was der Zeiger an der Auswahl anfasst.</summary>
    public enum Grab
    {
        /// <summary>Nichts davon — freie Fläche, also neue Auswahl oder Lasso.</summary>
        None,
        /// <summary>Der Dreh-Griff (nur Einzelauswahl).</summary>
        Rotate,
        /// <summary>Der Skalier-Griff.</summary>
        Scale,
        /// <summary>Die Auswahl selbst — verschieben.</summary>
        Move,
    }

    /// <summary>Der Mittelpunkt, um den ein Element gedreht und skaliert wird.</summary>
    public static SKPoint Center(WbElement el)
    {
        var b = WbRenderer.ElementBounds(el);
        return new SKPoint(b.MidX, b.MidY);
    }

    /// <summary>Die Griffe eines einzelnen Elements, mitgedreht.</summary>
    public static Set Single(WbElement el, float zoom)
    {
        var b = WbRenderer.ElementBounds(el);
        var ctr = new SKPoint(b.MidX, b.MidY);
        float pad = PadPx / zoom;
        var tl = new SKPoint(b.Left - pad, b.Top - pad);
        var tr = new SKPoint(b.Right + pad, b.Top - pad);
        var br = new SKPoint(b.Right + pad, b.Bottom + pad);
        var bl = new SKPoint(b.Left - pad, b.Bottom + pad);
        var rot = new SKPoint(b.MidX, b.Top - pad - RotateArmPx / zoom);
        float d = el.Rotation;
        return new Set(
            WbHit.Rotate(rot, ctr, d), WbHit.Rotate(br, ctr, d),
            WbHit.Rotate(tl, ctr, d), WbHit.Rotate(tr, ctr, d),
            WbHit.Rotate(br, ctr, d), WbHit.Rotate(bl, ctr, d));
    }

    /// <summary>Liegt der Zeiger im Fangradius eines Griffs der Einzelauswahl?</summary>
    public static bool Near(SKPoint c, SKPoint handle, float zoom)
    {
        float r = GrabRadiusPx / zoom;
        float dx = c.X - handle.X, dy = c.Y - handle.Y;
        return dx * dx + dy * dy <= r * r;
    }

    /// <summary>Das aufgeblähte Kästchen der Mehrfachauswahl.</summary>
    public static SKRect InflatedBounds(SKRect bounds, float zoom)
    {
        var b = bounds;
        b.Inflate(BoxPadPx / zoom, BoxPadPx / zoom);
        return b;
    }

    /// <summary>Liegt der Zeiger auf dem Skalier-Griff unten rechts am Kasten?</summary>
    public static bool NearBoxScale(SKRect bounds, SKPoint c, float zoom)
    {
        var b = InflatedBounds(bounds, zoom);
        float r = BoxGrabRadiusPx / zoom;
        float dx = c.X - b.Right, dy = c.Y - b.Bottom;
        return dx * dx + dy * dy <= r * r;
    }

    /// <summary>
    /// Liegt der Zeiger „innerhalb" eines einzelnen — womöglich gedrehten — Elements? Der
    /// Zeiger wird dafür in den <b>ungedrehten</b> Raum zurückgeholt; die Geometrie eines
    /// Elements liegt immer achsenparallel (siehe <see cref="WbHit.Rotate"/>).
    /// </summary>
    public static bool Contains(WbElement el, SKPoint c, float zoom)
    {
        var b = WbRenderer.ElementBounds(el);
        var ctr = new SKPoint(b.MidX, b.MidY);
        var local = WbHit.Rotate(c, ctr, -el.Rotation);
        b.Inflate(PadPx / zoom, PadPx / zoom);
        return b.Contains(local);
    }

    /// <summary>Winkel von <paramref name="from"/> nach <paramref name="to"/> in Grad.</summary>
    public static float AngleDeg(SKPoint from, SKPoint to) =>
        MathF.Atan2(to.Y - from.Y, to.X - from.X) * 180f / MathF.PI;

    /// <summary>
    /// Der neue Drehwinkel beim Ziehen am Dreh-Griff, samt Einrasten auf 15°.
    /// <paramref name="startDeg"/> ist die Drehung beim Anfassen, <paramref name="startPointer"/>
    /// der Zeigerwinkel beim Anfassen.
    /// </summary>
    public static float RotationFromDrag(SKPoint center, SKPoint c, float startDeg, float startPointer)
    {
        float deg = startDeg + (AngleDeg(center, c) - startPointer);
        float snapped = MathF.Round(deg / 15f) * 15f;
        return MathF.Abs(deg - snapped) <= SnapToleranceDeg ? snapped : deg;
    }

    /// <summary>
    /// <b>Die Weiche.</b> Was liegt unter dem Zeiger — Dreh-Griff, Skalier-Griff, die Auswahl
    /// selbst oder freie Fläche? Die Reihenfolge ist der Punkt: die Griffe ragen über den
    /// Rahmen hinaus und müssen deshalb <b>vor</b> dem Verschieben geprüft werden.
    /// <para>
    /// Gibt nur zurück, <b>was</b> angefasst wird — <b>getan</b> wird es im Kopf, denn dort
    /// liegt der Zustand. So können die Köpfe in der Reihenfolge nicht auseinanderlaufen.
    /// </para>
    /// </summary>
    /// <param name="single">Das Element bei Einzelauswahl, sonst <c>null</c>.</param>
    /// <param name="bounds">Das Kästchen der Auswahl (nur bei Mehrfachauswahl gebraucht).</param>
    /// <param name="count">
    /// Wie viele Elemente ausgewählt sind. <b>Der Parameter ist kein Beiwerk:</b> bei leerer
    /// Auswahl ist <paramref name="bounds"/> ein leeres Rechteck am Ursprung, und ohne diese
    /// Abfrage läge um den Punkt (0,0) ein unsichtbarer Kasten, der Klicks als „verschieben"
    /// deutet.
    /// </param>
    public static Grab Probe(WbElement? single, SKRect bounds, int count, SKPoint c, float zoom)
    {
        if (count <= 0) return Grab.None;

        if (single != null)
        {
            var h = Single(single, zoom);
            if (Near(c, h.Rotate, zoom)) return Grab.Rotate;
            if (Near(c, h.Scale, zoom)) return Grab.Scale;
            return Contains(single, c, zoom) ? Grab.Move : Grab.None;
        }

        if (NearBoxScale(bounds, c, zoom)) return Grab.Scale;
        return InflatedBounds(bounds, zoom).Contains(c) ? Grab.Move : Grab.None;
    }
}
