using GonkNote.Core.Models;
using SkiaSharp;

namespace GonkNote.Core.Editing;

/// <summary>
/// Die Schnellaktionen: die kleine schwebende Leiste mit Ausschneiden, Kopieren, Duplizieren,
/// Einfügen, Texterkennung, Löschen und Alles-Wählen. Sie ersetzt das Rechtsklick-Menü und
/// kommt ohne Tastatur aus — für den Stift der eigentliche Zweck.
///
/// <para>
/// <b>Warum das hier steht und nicht im Kopf.</b> Bis Phase 4.5 lag beides privat in
/// <c>WhiteboardView.QuickMenu.cs</c>: wo die Leiste hinkommt, und welcher Knopf gerade
/// etwas tun kann. Das erste ist Rechnen mit Rechtecken, das zweite eine Regel über den
/// Zustand der Seite — der Linux-Kopf braucht beide unverändert.
/// </para>
///
/// <para>
/// <b>⚠ Was ausdrücklich nicht mitwandert: das Aufklappen nach einer frischen Auswahl.</b>
/// Der WPF-Kopf öffnet die Leiste von selbst, sobald mit Lasso oder Verschieben etwas
/// ausgewählt wurde, und legt sie <i>über</i> die Auswahl. Genau dort hängt aber der
/// Dreh-Griff: <see cref="WbHandles.RotateArmPx"/> setzt ihn 28 Pixel über die Oberkante,
/// und die Leiste belegt den Streifen von 10 bis Leistenhöhe + 10 darüber. <b>Bei jeder
/// üblichen Leistenhöhe überschneidet sich das</b> — aufgefallen ist es in §4.51, als der
/// Linux-Kopf den Dreh-Griff bekam und der WPF-Kopf ihn nicht mehr hergab.
/// <see cref="UeberDerAuswahl"/> steht deshalb hier, damit die Rechnung nachlesbar bleibt,
/// wird vom Linux-Kopf aber nicht benutzt: dort öffnet die Leiste nur auf Anforderung.
/// </para>
/// </summary>
public static class WbSchnellaktionen
{
    /// <summary>Abstand unter den Zeiger, wenn die Leiste am Zeiger aufgeht.</summary>
    public const double AbstandZumZeiger = 12;

    /// <summary>Abstand zur Auswahl, wenn die Leiste über oder unter ihr aufgeht.</summary>
    public const double AbstandZurAuswahl = 10;

    /// <summary>
    /// Zwei Ereignisse innerhalb dieser Zeit gelten als eines. Ein Stift meldet die zweite
    /// Taste doppelt — einmal als Geste, einmal als synthetische rechte Maustaste —, und
    /// ohne diese Sperre klappte die Leiste auf und sofort wieder zu.
    /// </summary>
    public static readonly TimeSpan Entprellzeit = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Welche Aktionen gerade etwas tun können. <see cref="TexterkennungSichtbar"/> ist von
    /// <see cref="Texterkennung"/> getrennt: fehlt die Texterkennung auf diesem System ganz,
    /// wird der Knopf <b>ausgeblendet</b> und nicht bloß ausgegraut — ein dauerhaft grauer
    /// Knopf sieht aus wie ein Fehler.
    /// </summary>
    public readonly record struct Zustand(
        bool Ausschneiden, bool Kopieren, bool Duplizieren, bool Loeschen,
        bool Einfuegen, bool AllesWaehlen,
        bool TexterkennungSichtbar, bool Texterkennung);

    /// <summary>
    /// Rechnet den Zustand aus dem aus, was auf der Seite und in den Ablagen liegt.
    /// </summary>
    /// <param name="auswahl">Was gerade ausgewählt ist.</param>
    /// <param name="seite">Die Seite, auf der gearbeitet wird; <c>null</c> sperrt alles.</param>
    /// <param name="eigeneAblage">Wie viele Elemente in der programmeigenen Ablage liegen.</param>
    /// <param name="systemablageHatBild">Liegt in der Zwischenablage des Systems ein Bild?</param>
    /// <param name="texterkennungVerfuegbar">Gibt es auf diesem System eine Texterkennung?</param>
    public static Zustand Rechnen(
        IReadOnlyCollection<WbElement> auswahl,
        WbPage? seite,
        int eigeneAblage,
        bool systemablageHatBild,
        bool texterkennungVerfuegbar)
    {
        bool etwasGewaehlt = auswahl.Count > 0;

        // Erkannt wird an einem ausgewählten Bild — oder, wenn nichts ausgewählt ist, am
        // eingefügten Seitenhintergrund. Das ist der Fall „PDF-Seite importiert und gleich
        // vorlesen lassen", ohne dass man die Seite erst anklicken muss.
        bool quelleDa = auswahl.OfType<ImageElement>().Any()
                        || (!etwasGewaehlt && seite is { HasBackgroundImage: true });

        return new Zustand(
            Ausschneiden: etwasGewaehlt,
            Kopieren: etwasGewaehlt,
            Duplizieren: etwasGewaehlt,
            Loeschen: etwasGewaehlt,
            Einfuegen: eigeneAblage > 0 || systemablageHatBild,
            AllesWaehlen: seite is { Elements.Count: > 0 },
            TexterkennungSichtbar: texterkennungVerfuegbar,
            Texterkennung: texterkennungVerfuegbar && quelleDa);
    }

    /// <summary>
    /// Die Ecke oben links, an der die Leiste am Zeiger aufgeht: waagerecht mittig unter ihm,
    /// senkrecht ein Stück darunter — damit die Hand, die gerade dort war, nichts verdeckt.
    /// </summary>
    public static SKPoint AmZeiger(SKPoint zeiger, SKSize leiste) =>
        new((float)(zeiger.X - leiste.Width / 2), (float)(zeiger.Y + AbstandZumZeiger));

    /// <summary>
    /// Die Ecke oben links, wenn die Leiste an einer Auswahl aufgeht: mittig über ihr, und
    /// wenn oben kein Platz mehr ist, darunter.
    /// <para>
    /// <b>Nur für den WPF-Kopf</b> — siehe die Anmerkung zum Dreh-Griff im Kopfkommentar.
    /// </para>
    /// </summary>
    public static SKPoint UeberDerAuswahl(SKRect auswahlAufDemSchirm, SKSize leiste)
    {
        float oben = (float)(auswahlAufDemSchirm.Top - leiste.Height - AbstandZurAuswahl);
        if (oben < 4) oben = (float)(auswahlAufDemSchirm.Bottom + AbstandZurAuswahl);
        return new SKPoint(auswahlAufDemSchirm.MidX - leiste.Width / 2, oben);
    }

    /// <summary>
    /// Hält die Leiste im sichtbaren Bereich. <b>Ist die Fläche schmaler als die Leiste</b>,
    /// bleibt sie am linken bzw. oberen Rand kleben statt negativ hinauszurutschen — dann ist
    /// wenigstens ihr Anfang zu sehen.
    /// </summary>
    public static SKPoint ImBlick(SKPoint ecke, SKSize leiste, SKSize flaeche)
    {
        float maxX = Math.Max(0, flaeche.Width - leiste.Width);
        float maxY = Math.Max(0, flaeche.Height - leiste.Height);
        return new SKPoint(Math.Clamp(ecke.X, 0, maxX), Math.Clamp(ecke.Y, 0, maxY));
    }
}
