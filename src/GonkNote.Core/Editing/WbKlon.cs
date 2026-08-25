using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using SkiaSharp;

namespace GonkNote.Core.Editing;

/// <summary>
/// Kopieren, Duplizieren und Einfügen von Whiteboard-Elementen: der <b>Klon</b> und die
/// Stelle, an der er landet.
///
/// <para>
/// <b>Warum das hier steht und nicht im Kopf.</b> Bis Phase 4.5 lag das Klonen privat in
/// <c>WhiteboardView.QuickMenu.cs</c> des WPF-Kopfs, die Platzierung in
/// <c>WhiteboardView.Selection.cs</c>. Der Linux-Kopf kennt beides gar nicht — er hat weder
/// Zwischenablage noch Duplizieren. Wer das abschreibt, schreibt auch die Lücken ab.
/// </para>
///
/// <para>
/// <b>Und es waren zwei, beide am laufenden Programm gemessen (V2-83).</b> Ein
/// <see cref="WbElement.Rotation">gedrehtes</see> Element kam beim Duplizieren gerade wieder
/// heraus, und ein Bleistiftstrich verlor die <see cref="WbPoint.TX">Neigung</see>: der alte
/// Kloner setzte in jedem Zweig die typeigenen Felder und übersah beides. Kein Wunder — die
/// Drehung kam mit §4.51, die Neigung mit §4.11, der Kloner ist älter als beide.
/// <b>Genau das ist der Grund für eine gemeinsame Stelle:</b> ein Feld, das an einem Ort
/// ergänzt wird, muss an einem Ort mitgezogen werden, nicht an fünf.
/// </para>
///
/// <para>
/// <b>Die Id wird bewusst nicht kopiert.</b> Ein Klon ist ein neues Element und bekommt eine
/// neue <see cref="WbElement.Id"/> — sonst stünden zwei Elemente mit derselben Id auf
/// derselben Seite, und der Verlaufsstapel könnte sie nicht mehr auseinanderhalten.
/// </para>
/// </summary>
public static class WbKlon
{
    /// <summary>Der Versatz eines Klons, wenn keine Zielstelle genannt ist (Punkte).</summary>
    public const float Versatz = 18f;

    /// <summary>
    /// Eine unabhängige Kopie eines Elements — mit Drehung, und beim Strich mit Neigung.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Bei einem Elementtyp, den diese Stelle noch nicht kennt. <b>Das ist Absicht:</b> ein
    /// neuer Typ soll hier auffallen und nicht stillschweigend halb kopiert werden.
    /// Wächter: <c>KlonTests</c>.
    /// </exception>
    public static WbElement Klonen(WbElement el)
    {
        WbElement klon = el switch
        {
            StrokeElement s => new StrokeElement
            {
                // Die Punkte werden **neu gebaut** und nicht geteilt: Verschieben und
                // Skalieren schreiben in die Punkte hinein, ein geteilter Punkt bewegte
                // beide Striche zugleich.
                Points = s.Points.Select(p => new WbPoint(p.X, p.Y, p.P, p.TX, p.TY)).ToList(),
                Color = s.Color, Width = s.Width, Kind = s.Kind,
            },
            ShapeElement sh => new ShapeElement
            {
                Shape = sh.Shape, X1 = sh.X1, Y1 = sh.Y1, X2 = sh.X2, Y2 = sh.Y2,
                Color = sh.Color, StrokeWidth = sh.StrokeWidth, Fill = sh.Fill,
            },
            TextElement t => new TextElement
            {
                X = t.X, Y = t.Y, Text = t.Text, Color = t.Color, FontSize = t.FontSize,
                Background = t.Background, FontFamily = t.FontFamily,
            },
            // Data wird bewusst geteilt: die Bytes ändern sich nach dem Import nie mehr,
            // und ein zweites Bild im Speicher wäre bei einer PDF-Seite ein spürbarer Posten.
            ImageElement im => new ImageElement
            {
                X = im.X, Y = im.Y, Width = im.Width, Height = im.Height, Data = im.Data,
            },
            StickyNoteElement sn => new StickyNoteElement
            {
                X = sn.X, Y = sn.Y, Width = sn.Width, Height = sn.Height, Text = sn.Text,
                Color = sn.Color, TextColor = sn.TextColor, FontSize = sn.FontSize,
                FontFamily = sn.FontFamily,
            },
            _ => throw new NotSupportedException(
                $"Kein Klonweg für {el.GetType().Name} — WbKlon.Klonen ergänzen."),
        };

        // Die Drehung steht an der Basisklasse und gehört deshalb hierher, nicht in die
        // Zweige. Wer sie dort einträgt, vergisst sie beim nächsten Typ wieder.
        klon.Rotation = el.Rotation;
        return klon;
    }

    /// <summary>Klont eine ganze Auswahl, Reihenfolge erhalten.</summary>
    public static List<WbElement> Klonen(IEnumerable<WbElement> elemente) =>
        elemente.Select(Klonen).ToList();

    /// <summary>
    /// Verschiebt frisch geklonte Elemente an ihren Platz.
    /// <para>
    /// Mit <paramref name="ziel"/> kommt der <b>Mittelpunkt der ganzen Gruppe</b> auf den
    /// Zielpunkt — die Elemente behalten dabei ihre Lage zueinander. Ohne Zielpunkt rücken
    /// sie um <see cref="Versatz"/> nach rechts unten, damit ein Duplikat nicht deckungsgleich
    /// auf dem Original liegt und unsichtbar wirkt.
    /// </para>
    /// </summary>
    public static void Platzieren(IReadOnlyList<WbElement> klone, SKPoint? ziel)
    {
        if (klone.Count == 0) return;

        if (ziel is not { } z)
        {
            foreach (var k in klone) k.Translate(Versatz, Versatz);
            return;
        }

        var kasten = Umschliessung(klone);
        foreach (var k in klone) k.Translate(z.X - kasten.MidX, z.Y - kasten.MidY);
    }

    /// <summary>
    /// Der Kasten um eine Gruppe von Elementen. <b>Nicht <see cref="SKRect.Empty"/> als
    /// Startwert</b>: das ist der Punkt (0,0), und eine Gruppe weit rechts unten bekäme
    /// dadurch einen Kasten, der bis zum Ursprung reicht — der Mittelpunkt läge dann falsch.
    /// </summary>
    public static SKRect Umschliessung(IReadOnlyList<WbElement> elemente)
    {
        if (elemente.Count == 0) return SKRect.Empty;

        var kasten = WbRenderer.ElementBounds(elemente[0]);
        for (int i = 1; i < elemente.Count; i++)
            kasten = SKRect.Union(kasten, WbRenderer.ElementBounds(elemente[i]));
        return kasten;
    }
}
