using GonkNote.Core.Models;
using GonkNote.Core.Theming;
using SkiaSharp;

namespace GonkNote.Core.Tests;

/// <summary>
/// Die Symboltabelle aus §4.31 — <see cref="AppIcons"/>.
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Eine Pfadangabe ist eine Zeichenkette: Sie kann
/// verrutscht, halbiert oder ganz falsch sein, ohne dass irgendetwas abstürzt — es erscheint
/// nur ein anderes Symbol, oder gar keines. Beim Erzeugen der Tabelle sind genau so drei
/// Fehler entstanden (§4.31): ein relatives <c>m</c> am Pfadanfang, absolute LineTos nach
/// einem großen <c>M</c>, und ein deutsches Dezimalkomma. Alle drei wären still geblieben.
/// </para>
/// <para>
/// <b>Gemessen wird mit Skia und nicht mit einem Kopf.</b> Beide Köpfe lesen dieselbe
/// Pfadsprache, aber nur Skia liegt in Core — und damit läuft dieser Wächter auch unter
/// Linux, wo es kein WPF gibt. Was er nicht kann, ist <i>hinsehen</i>; dafür gibt es das
/// Kontaktblatt aus §4.31.
/// </para>
/// </summary>
public sealed class IkonentabelleTests
{
    private static IEnumerable<AppIcon> Alle => Enum.GetValues<AppIcon>();

    /// <summary>
    /// Zu jedem Wert des Aufzählungstyps gehört eine Form. <b>Der wichtigste Wächter:</b> Ein
    /// neuer Wert ohne Eintrag wirft erst im laufenden Programm, und zwar an genau der Stelle,
    /// an der niemand hinsieht.
    /// </summary>
    [Fact]
    public void Jedes_Symbol_hat_eine_Form()
    {
        foreach (var symbol in Alle)
        {
            var form = AppIcons.Shape(symbol);
            Assert.False(string.IsNullOrWhiteSpace(form.Path), $"{symbol} hat keine Pfadangabe.");
        }

        Assert.Equal(Alle.Count(), AppIcons.Count);
    }

    /// <summary>
    /// Jede Pfadangabe lässt sich lesen. <c>ParseSvgPathData</c> liefert bei Unsinn einen
    /// leeren Pfad statt einer Ausnahme — deshalb wird auf die Punktzahl geprüft und nicht
    /// darauf, dass nichts fliegt.
    /// </summary>
    [Fact]
    public void Jede_Form_laesst_sich_lesen()
    {
        foreach (var symbol in Alle)
        {
            using var pfad = SKPath.ParseSvgPathData(AppIcons.Shape(symbol).Path);
            Assert.True(pfad is { PointCount: > 1 }, $"{symbol} ergibt keinen brauchbaren Pfad.");
        }
    }

    /// <summary>
    /// Jede Form bleibt in ihrem Kasten. <b>Das ist der Wächter gegen den Zahlendreher:</b>
    /// Eine 24er-Form, die versehentlich als 16er einträgt, ragt heraus und wird beim Zeichnen
    /// abgeschnitten — im Bild sieht das nach einem schlecht gezeichneten Symbol aus, nicht
    /// nach einer falschen Zahl.
    ///
    /// <para>
    /// <b>Gemessen wird mit <c>TightBounds</c> und nicht mit <c>Bounds</c>.</b> Das zweite
    /// liefert den Kasten um die <i>Stützpunkte</i> einer Kurve, nicht um die Kurve: Beim Lasso
    /// (ein elliptischer Bogen) ergab das 4,5 Einheiten außerhalb — der Wächter war rot, die
    /// Form richtig. Ein Wächter, der die falsche Größe misst, ist schlimmer als keiner.
    /// </para>
    /// <para>
    /// Der halbe Strich Zugabe ist kein Schlupf, sondern Geometrie: Gezeichnet wird auf der
    /// Linie, die Strichstärke liegt also je zur Hälfte innen und außen. Lucide nutzt den
    /// Kasten bis an den Rand aus.
    /// </para>
    /// </summary>
    [Fact]
    public void Jede_Form_bleibt_in_ihrem_Kasten()
    {
        foreach (var symbol in Alle)
        {
            var form = AppIcons.Shape(symbol);
            using var pfad = SKPath.ParseSvgPathData(form.Path);
            var kasten = pfad.TightBounds;

            double zugabe = AppIcons.StrokeFor(symbol) / 2.0;

            Assert.True(kasten.Left >= -zugabe, $"{symbol} ragt links heraus ({kasten.Left}).");
            Assert.True(kasten.Top >= -zugabe, $"{symbol} ragt oben heraus ({kasten.Top}).");
            Assert.True(kasten.Right <= form.Box + zugabe,
                $"{symbol} ragt rechts heraus ({kasten.Right} > {form.Box}).");
            Assert.True(kasten.Bottom <= form.Box + zugabe,
                $"{symbol} ragt unten heraus ({kasten.Bottom} > {form.Box}).");
        }
    }

    /// <summary>
    /// Und sie füllt ihn auch aus. Eine Form, die nur ein Viertel des Kastens benutzt, stünde
    /// neben den anderen als Miniatur da — der häufigste Fall wäre ein Pfad, von dem beim
    /// Umrechnen ein Teil verlorengegangen ist.
    ///
    /// <para>
    /// <b>Die Ausnahmen stehen namentlich da</b>, statt die Schwelle für alle zu senken: Ein
    /// Minuszeichen und ein Chevron sind von Natur aus flach, ein Trennstrich ist ein Strich.
    /// Wer eine weitere Ausnahme braucht, trägt sie hier ein und begründet sie — das ist der
    /// Sinn einer benannten Liste gegenüber einer weichen Grenze.
    /// </para>
    /// </summary>
    [Fact]
    public void Jede_Form_fuellt_ihren_Kasten_aus()
    {
        var flach = new HashSet<AppIcon>
        {
            AppIcon.WindowMinimize,   // ein Strich
            AppIcon.ChevronDown, AppIcon.ChevronUp,
            AppIcon.ChevronLeft, AppIcon.ChevronRight,
        };

        foreach (var symbol in Alle)
        {
            var form = AppIcons.Shape(symbol);
            using var pfad = SKPath.ParseSvgPathData(form.Path);
            var kasten = pfad.TightBounds;

            double laengsteSeite = Math.Max(kasten.Width, kasten.Height);
            double erwartet = flach.Contains(symbol) ? form.Box * 0.3 : form.Box * 0.55;

            Assert.True(laengsteSeite >= erwartet,
                $"{symbol} nutzt nur {laengsteSeite:0.0} von {form.Box} — ist der Pfad vollständig?");
        }
    }

    /// <summary>
    /// Es gibt nur zwei Kastengrößen: 24 (Lucide) und 16 (die eigenen Formen). Eine dritte
    /// wäre kein Fehler, aber sie gehört benannt — sonst wächst still ein drittes Maß heran,
    /// und die Strichstärken passen nicht mehr zueinander.
    /// </summary>
    [Fact]
    public void Es_gibt_nur_die_zwei_bekannten_Kastengroessen()
    {
        foreach (var symbol in Alle)
        {
            double kasten = AppIcons.Shape(symbol).Box;
            Assert.True(kasten is 16 or 24, $"{symbol} hat den unbekannten Kasten {kasten}.");
        }
    }

    /// <summary>
    /// <b>Segoe Fluent Icons darf nicht zurückkommen.</b> Die Zeichencodes der Schrift liegen
    /// im privaten Bereich von Unicode (U+E000 – U+F8FF); ein solches Zeichen in einer
    /// Pfadangabe wäre der Rückfall in genau das, was §4.31 abgeschafft hat.
    /// </summary>
    [Fact]
    public void Keine_Form_enthaelt_ein_Zeichen_aus_dem_privaten_Bereich()
    {
        foreach (var symbol in Alle)
        {
            foreach (char c in AppIcons.Shape(symbol).Path)
                Assert.False(c is >= '' and <= '',
                    $"{symbol} enthält U+{(int)c:X4} — das ist eine Glyphe, keine Form.");
        }
    }

    /// <summary>
    /// Jede Dokumentart hat ihr Symbol, und keine bekommt aus Versehen das des Ordners. Diese
    /// Zuordnung stand bis zum 2026-08-12 zweimal da und sagte Verschiedenes (§4.31).
    /// </summary>
    [Fact]
    public void Jede_Dokumentart_hat_ihr_eigenes_Symbol()
    {
        var arten = new[]
        {
            ItemKind.Folder, ItemKind.Notebook, ItemKind.Whiteboard, ItemKind.TextDocument,
        };

        var symbole = arten.Select(AppIcons.ForKind).ToList();

        Assert.Equal(symbole.Count, symbole.Distinct().Count());
        Assert.Equal(AppIcon.Folder, AppIcons.ForKind(ItemKind.Folder));
        Assert.Equal(AppIcon.TextDocument, AppIcons.ForKind(ItemKind.TextDocument));
    }

    /// <summary>
    /// Die Strichstärke ist ein Zwölftel des Kastens — bei beiden Größen. Daran hängt, dass im
    /// selben Fenster keine zwei Strichstärken stehen.
    /// </summary>
    [Fact]
    public void Die_Strichstaerke_haelt_dasselbe_Verhaeltnis()
    {
        foreach (var symbol in Alle)
        {
            var form = AppIcons.Shape(symbol);
            Assert.Equal(form.Box / 12.0, AppIcons.StrokeFor(symbol), 6);

            // Auf 16 Bildpunkte gezeichnet ergibt das bei jedem Symbol denselben Strich.
            double aufSchirm = AppIcons.StrokeFor(symbol) * AppIcons.Scale(symbol, 16);
            Assert.Equal(16 / 12.0, aufSchirm, 6);
        }
    }

    /// <summary>Ein unbekannter Wert wirft, statt still ein Ersatzsymbol zu liefern.</summary>
    [Fact]
    public void Ein_unbekanntes_Symbol_wirft()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AppIcons.Shape((AppIcon)9999));
    }
}
