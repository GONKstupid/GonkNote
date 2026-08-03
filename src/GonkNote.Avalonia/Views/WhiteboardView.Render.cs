using Avalonia;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Core.Theming;
using GonkNote.Platform;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Zeichnen: Seitenhintergrund, Cover, Elemente und die Überlagerungen, die nur während
/// einer Aktion zu sehen sind.
///
/// <para>
/// <b>Die Zeichenroutinen selbst stehen hier nicht.</b> Sie liegen in
/// <see cref="WbRenderer"/> in Core — derselbe Code, den der WPF-Kopf und der PDF-Export
/// benutzen. Was hier steht, ist die Reihenfolge: was liegt über was, und was gehört zur
/// Seite und was nur zum Augenblick.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>Eine Farbe aus der Farbtabelle in Core, im Format des Renderers.</summary>
    private static SKColor ThemeSk(ThemeColor farbe)
    {
        var c = AvaloniaThemeHost.Current[farbe];
        return new SKColor(c.R, c.G, c.B, c.A);
    }

    private void OnPaint(SkiaPaintArgs e)
    {
        var leinwand = e.Canvas;

        // Kein `Clear`: das hier ist eine **aufzeichnende** Leinwand, und ein Clear darauf
        // löscht beim Abspielen alles im Beschnitt — auch das, was Avalonia darunter schon
        // gezeichnet hat. Eine gefüllte Fläche tut dasselbe, ohne diese Nebenwirkung.
        using (var hintergrund = new SKPaint { Color = CanvasBackColor() })
            leinwand.DrawRect(e.Bounds, hintergrund);

        if (_page == null || _vm == null) return;

        // **Hier wird nur gezeichnet — kein anderes Steuerelement angefasst.** Das ist der
        // Unterschied zu WPF, der diesen Brocken am meisten Zeit gekostet hat: dort läuft
        // `SKElement.PaintSurface` außerhalb des Renderdurchlaufs, und die Fläche darf von
        // dort aus nebenbei die Farbkachel und die Zoom-Anzeige nachführen. Avalonia ruft
        // `Render` **im** Durchlauf; wer darin die Eigenschaft eines anderen Elements
        // setzt, bekommt „Visual was invalidated during the render pass" — und der ganze
        // Durchlauf bricht ab, nicht nur die eine Zuweisung.
        //
        // Das Fehlerbild war entsprechend irreführend: eine **leere Werkzeugleiste** und
        // ein leeres Blatt, also überall außer an der Ursache. Was früher hier stand
        // (`RefreshAutoSwatch`, `UpdateZoomLabel`, `CenterView`), hängt jetzt an den
        // Ereignissen, die es wirklich auslösen — Seitenwechsel, Theme-Wechsel, Größe.

        DrawContent(leinwand, e);

        leinwand.Save();
        ApplyViewTransform(leinwand);
        DrawActiveOverlays(leinwand);
        leinwand.Restore();

        // Ohne Ansichtsabbildung: die Anzeige klebt am Fenster, nicht am Blatt.
        DrawStiftAnzeige(leinwand, e);
    }

    /// <summary>
    /// Was der Stift gerade wirklich liefert — mit <b>F9</b> einblendbar, standardmäßig aus.
    ///
    /// <para>
    /// Das ist kein Feature, sondern ein Messgerät, und es steht hier aus einem konkreten
    /// Grund: §5a hat Druck und Neigung im <i>Prototyp</i> gemessen, nicht in der App. Ob
    /// sie auch durch den fertigen Eingabepfad kommen — durch <c>GetIntermediatePoints</c>,
    /// durch die Zeigererkennung, durch die Fangregel — beantwortet sonst niemand, und ein
    /// gleichmäßiger Strich sieht genauso aus, ob er aus dem Rückfall stammt oder aus einem
    /// Gerät, das keinen Druck liefert. Hier steht es dran.
    /// </para>
    /// <para>
    /// Es ist ausdrücklich auch die Stelle, an der ein <b>zweites Stiftgerät</b> (MPP, EMR)
    /// beurteilt wird — der einzige Punkt aus §5 „Noch offen", der echtes Risiko trägt.
    /// </para>
    /// </summary>
    private void DrawStiftAnzeige(SKCanvas leinwand, SkiaPaintArgs e)
    {
        if (!_stiftAnzeige) return;

        string[] zeilen =
        [
            $"Zeiger   {_letzteZeigerart}",
            $"Druck    {_letzterDruck:0.0000}  {(_geraetLiefertDruck ? "(Gerät liefert Druck)" : "(Rückfall: feste Breite)")}",
            $"Neigung  X {_tiltX,6:0.0}°   Y {_tiltY,6:0.0}°",
            $"Punkte   {_activePoints?.Count ?? 0}",
        ];

        using var schrift = new SKFont(WbFonts.Regular, 12);
        float breite = 0;
        foreach (var z in zeilen) breite = Math.Max(breite, schrift.MeasureText(z));

        float h = zeilen.Length * 16f + 12f;
        var kasten = SKRect.Create(e.Bounds.Right - breite - 24f, 10f, breite + 14f, h);

        using (var flaeche = new SKPaint { Color = ThemeSk(ThemeColor.CardBg).WithAlpha(230), IsAntialias = true })
            leinwand.DrawRoundRect(kasten, 6, 6, flaeche);
        using (var rand = new SKPaint
               {
                   Color = ThemeSk(ThemeColor.Accent), Style = SKPaintStyle.Stroke,
                   StrokeWidth = 1, IsAntialias = true,
               })
            leinwand.DrawRoundRect(kasten, 6, 6, rand);

        using var text = new SKPaint { Color = ThemeSk(ThemeColor.Text), IsAntialias = true };
        for (int i = 0; i < zeilen.Length; i++)
            leinwand.DrawText(zeilen[i], kasten.Left + 7f, kasten.Top + 20f + i * 16f,
                SKTextAlign.Left, schrift, text);
    }

    /// <summary>Die Farbe hinter der Seite — im unendlichen Whiteboard folgt sie dem Seitenton.</summary>
    private SKColor CanvasBackColor()
    {
        if (_page is not { IsInfinite: true } p || p.Shade == PageShade.Auto)
            return ThemeSk(ThemeColor.CanvasBg);
        return EffectiveShade(p) == PageShade.Dark
            ? SKColor.Parse("#12161F")
            : SKColor.Parse("#EEF2F8");
    }

    /// <summary>
    /// Punkte der Fläche → Leinwandkoordinaten. <b>Ohne Umrechnung auf Gerätepixel</b> —
    /// die hat Avalonia schon auf der geliehenen Leinwand stehen. Der WPF-Kopf muss an
    /// dieser Stelle zusätzlich mit <c>pixelScale</c> skalieren, weil ihm <c>SKElement</c>
    /// eine Fläche in Gerätepixeln übergibt.
    /// </summary>
    private void ApplyViewTransform(SKCanvas leinwand)
    {
        leinwand.Translate(PanX, PanY);
        leinwand.Scale(Zoom);
    }

    // ==================== Der eingefrorene Seiteninhalt ====================
    //
    // Während ein Strich gezogen wird, ändert sich am bereits Gezeichneten nichts: die
    // Punkte laufen bis zum Absetzen in _activePoints und nicht in die Seite. Trotzdem
    // müsste ohne diesen Zwischenschritt pro Bild die ganze Seite neu aufgezeichnet
    // werden — bei einer vollgeschriebenen Seite ist das der Unterschied zwischen einem
    // Strich, der am Stift klebt, und einem, der hinterherhinkt.
    //
    // **Und es ist ein Bild, kein Pixelspeicher.** Der WPF-Kopf rastert an dieser Stelle
    // ein SKImage in Fenstergröße (~20 MB) und muss es bei jedem Zoom- und Verschiebe-
    // schritt wegwerfen. Ein SKPicture behält die Zeichenbefehle: es wird in
    // **Seitenkoordinaten** aufgezeichnet, die Ansicht kommt erst beim Abspielen darüber.
    // Zoomen und Verschieben machen es deshalb gar nicht erst ungültig, und scharf bleibt
    // es bei jedem Maßstab.

    private SKPicture? _inhalt;
    private InhaltSchluessel _inhaltSchluessel;

    /// <summary>Alles, was die Aufzeichnung ungültig macht. Ansicht und Größe stehen bewusst nicht darin.</summary>
    private readonly record struct InhaltSchluessel(WbPage? Seite, AppTheme Theme, int Elemente);

    /// <summary>
    /// Läuft gerade etwas, das <b>nur</b> die Überlagerung ändert? Dann stehen die fertigen
    /// Elemente nachweislich still und dürfen aus der Aufzeichnung kommen. Radieren und
    /// Verschieben ändern den Inhalt — die zählen bewusst nicht dazu.
    /// </summary>
    private bool InhaltStehtStill => _drawing || _lassoPts != null;

    private void DrawContent(SKCanvas leinwand, SkiaPaintArgs e)
    {
        if (!InhaltStehtStill)
        {
            InhaltVerwerfen();
            leinwand.Save();
            ApplyViewTransform(leinwand);
            DrawPageAndElements(leinwand, e);
            leinwand.Restore();
            return;
        }

        var schluessel = new InhaltSchluessel(_page, App.Platform.Theme.Current, _page!.Elements.Count);
        if (_inhalt == null || !_inhaltSchluessel.Equals(schluessel))
        {
            InhaltVerwerfen();
            _inhalt = InhaltAufzeichnen(e);
            _inhaltSchluessel = schluessel;
        }

        leinwand.Save();
        ApplyViewTransform(leinwand);
        leinwand.DrawPicture(_inhalt);
        leinwand.Restore();
    }

    /// <summary>
    /// Zeichnet den fertigen Seiteninhalt einmal in Seitenkoordinaten auf.
    /// <para>
    /// Der Beschnitt ist großzügig gewählt: er begrenzt nur, was Skia beim Abspielen
    /// überspringen darf, und ein zu enger Rahmen schnitte einen Strich ab, der über den
    /// Seitenrand hinausragt.
    /// </para>
    /// </summary>
    private SKPicture InhaltAufzeichnen(SkiaPaintArgs e)
    {
        var rahmen = _page is { IsInfinite: false }
            ? SKRect.Create(-2000, -2000, _page.Width + 4000, _page.Height + 4000)
            : SichtbarerBereich(e).Standardized;

        using var aufnahme = new SKPictureRecorder();
        var leinwand = aufnahme.BeginRecording(rahmen);
        DrawPageAndElements(leinwand, e);
        return aufnahme.EndRecording();
    }

    private void InhaltVerwerfen()
    {
        _inhalt?.Dispose();
        _inhalt = null;
    }

    /// <summary>Der gerade sichtbare Bereich in Leinwandkoordinaten — fürs Weglassen (Culling).</summary>
    private SKRect SichtbarerBereich(SkiaPaintArgs e)
    {
        var tl = ToCanvas(new Point(0, 0));
        var br = ToCanvas(new Point(e.Bounds.Width, e.Bounds.Height));
        return new SKRect(
            Math.Min(tl.X, br.X), Math.Min(tl.Y, br.Y),
            Math.Max(tl.X, br.X), Math.Max(tl.Y, br.Y));
    }

    private void DrawPageAndElements(SKCanvas leinwand, SkiaPaintArgs e)
    {
        DrawPageBackground(leinwand, e);
        if (_page == null) return;

        // Nur Sichtbares zeichnen. Das verhindert, dass bei vielen hochauflösenden Bildern
        // jedes Bild alle durch den Dekodierer geht.
        var sichtbar = SichtbarerBereich(e);
        foreach (var el in _page.Elements)
        {
            var b = WbRenderer.ElementBounds(el);
            if (!b.IsEmpty && !sichtbar.IntersectsWith(b)) continue;
            WbRenderer.DrawElement(leinwand, el);
        }
    }

    // ==================== Seitenhintergrund ====================

    private static SKImage? _pageShadowImg;

    private SKColor PageLineColor()
    {
        if (_page == null || _page.Shade == PageShade.Auto) return ThemeSk(ThemeColor.PageLine);
        return EffectiveShade(_page) == PageShade.Dark ? SKColor.Parse("#35486E") : SKColor.Parse("#BBD2F0");
    }

    private SKColor PageDotColor()
    {
        if (_page == null || _page.Shade == PageShade.Auto) return ThemeSk(ThemeColor.PageGridDot);
        return EffectiveShade(_page) == PageShade.Dark ? SKColor.Parse("#3A4A6B") : SKColor.Parse("#B8C6DC");
    }

    private void DrawPageBackground(SKCanvas leinwand, SkiaPaintArgs e)
    {
        if (_page == null) return;

        if (_page.IsInfinite)
        {
            DrawInfinitePattern(leinwand, e);
            return;
        }

        var seite = SKRect.Create(0, 0, _page.Width, _page.Height);
        WbRenderer.DrawCachedShadow(leinwand, seite, 0, 60, ref _pageShadowImg);

        if (_page.IsCover)
        {
            DrawCover(leinwand, seite);
            return;
        }

        var papier = _page.Shade == PageShade.Auto
            ? ThemeSk(ThemeColor.PageBg)
            : EffectiveShade(_page) == PageShade.Dark ? SKColor.Parse("#1E2638") : SKColors.White;
        using (var bg = new SKPaint { Color = papier })
            leinwand.DrawRect(seite, bg);

        // Hintergrundbild (etwa eine importierte PDF-Seite): seitenfüllend, ersetzt das
        // Muster. **Nicht auf die Bytes im Dokument prüfen** — die sind nach dem ersten
        // Speichern leer, weil das Bild dann im Blob-Speicher liegt. Wer hier vorab
        // abbricht, zeigt die Seite für immer leer an (HANDOFF §7).
        if (_page.HasBackgroundImage &&
            ImageCache.Get(_page.BackgroundImageId, _page.BackgroundImage) is { } bild)
        {
            using var ip = new SKPaint { IsAntialias = true };
            leinwand.DrawImage(bild, seite, WbRenderer.MediumSampling, ip);
            return;
        }

        using var linie = new SKPaint
        {
            Color = PageLineColor(),
            StrokeWidth = 1f,
            IsAntialias = false,
        };

        const float abstand = 30f;
        switch (_page.Background)
        {
            case PageBackground.Lines:
                for (float y = 84; y < _page.Height - 30; y += abstand)
                    leinwand.DrawLine(30, y, _page.Width - 30, y, linie);
                break;

            case PageBackground.Grid:
                for (float y = 0; y <= _page.Height; y += abstand)
                    leinwand.DrawLine(0, y, _page.Width, y, linie);
                for (float x = 0; x <= _page.Width; x += abstand)
                    leinwand.DrawLine(x, 0, x, _page.Height, linie);
                break;

            case PageBackground.Dots:
                using (var punkt = new SKPaint { Color = PageDotColor(), IsAntialias = true })
                {
                    for (float x = 24; x < _page.Width; x += 24)
                        for (float y = 24; y < _page.Height; y += 24)
                            leinwand.DrawCircle(x, y, 1.1f, punkt);
                }
                break;
        }
    }

    /// <summary>Muster der unendlichen Fläche, nur über den sichtbaren Bereich.</summary>
    private void DrawInfinitePattern(SKCanvas leinwand, SkiaPaintArgs e)
    {
        if (_page == null || _page.Background == PageBackground.Blank) return;

        var sicht = SichtbarerBereich(e);
        float abstand = _page.Background == PageBackground.Dots ? 28f : 30f;
        // Beim Herauszoomen das Raster ausdünnen, statt eine graue Fläche zu zeichnen.
        while (abstand * Zoom < 14f) abstand *= 2f;

        float x0 = MathF.Floor(sicht.Left / abstand) * abstand;
        float y0 = MathF.Floor(sicht.Top / abstand) * abstand;

        switch (_page.Background)
        {
            case PageBackground.Dots:
                using (var punkt = new SKPaint { Color = PageDotColor(), IsAntialias = true })
                {
                    float r = 1.1f / Zoom;
                    for (float x = x0; x <= sicht.Right; x += abstand)
                        for (float y = y0; y <= sicht.Bottom; y += abstand)
                            leinwand.DrawCircle(x, y, r, punkt);
                }
                break;

            case PageBackground.Grid:
            case PageBackground.Lines:
                using (var linie = new SKPaint { Color = PageLineColor(), StrokeWidth = 1f / Zoom })
                {
                    for (float y = y0; y <= sicht.Bottom; y += abstand)
                        leinwand.DrawLine(sicht.Left, y, sicht.Right, y, linie);
                    if (_page.Background == PageBackground.Grid)
                        for (float x = x0; x <= sicht.Right; x += abstand)
                            leinwand.DrawLine(x, sicht.Top, x, sicht.Bottom, linie);
                }
                break;
        }
    }

    /// <summary>Cover-Seite: Bild oder Farbverlauf, Akzentlinie und Dokumenttitel.</summary>
    private void DrawCover(SKCanvas leinwand, SKRect seite)
    {
        if (_page == null) return;
        var stil = _vm?.Doc.Cover;

        // Bild-Cover: füllt die Seite formatfüllend, mittig beschnitten.
        if (stil != null && ImageCache.Get(stil.ImageId, stil.Image) is { } bild)
        {
            float f = Math.Max(seite.Width / bild.Width, seite.Height / bild.Height);
            float w = bild.Width * f, h = bild.Height * f;
            var ziel = SKRect.Create(seite.MidX - w / 2f, seite.MidY - h / 2f, w, h);
            leinwand.Save();
            leinwand.ClipRect(seite);
            using var ip = new SKPaint { IsAntialias = true };
            leinwand.DrawImage(bild, ziel, WbRenderer.MediumSampling, ip);
            leinwand.Restore();
            return;
        }

        using (var verlauf = new SKPaint { IsAntialias = true })
        {
            verlauf.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(_page.Width, _page.Height),
                [WbRenderer.ParseColor(stil?.GradientStart ?? "#1E3A8A"),
                 WbRenderer.ParseColor(stil?.GradientEnd ?? "#7C3AED")],
                null, SKShaderTileMode.Clamp);
            leinwand.DrawRect(seite, verlauf);
        }

        var fett = stil == null
            ? WbFonts.Bold
            : SKTypeface.FromFamilyName(stil.FontFamily, SKFontStyle.Bold) ?? WbFonts.Bold;

        string titel = _vm?.Item.Name ?? "";
        using var titelFarbe = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var titelSchrift = new SKFont(fett, 46);
        while (titelSchrift.Size > 18 && titelSchrift.MeasureText(titel) > _page.Width * 0.8f)
            titelSchrift.Size -= 2;
        leinwand.DrawText(titel, _page.Width / 2f, _page.Height * 0.4f,
            SKTextAlign.Center, titelSchrift, titelFarbe);

        using (var akzent = new SKPaint
        {
            Color = SKColor.Parse("#2DD4BF"),
            StrokeWidth = 4,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
        })
            leinwand.DrawLine(_page.Width * 0.3f, _page.Height * 0.445f,
                              _page.Width * 0.7f, _page.Height * 0.445f, akzent);

        using var unterFarbe = new SKPaint { Color = SKColors.White.WithAlpha(170), IsAntialias = true };
        using var unterSchrift = new SKFont(
            stil == null ? WbFonts.Regular : SKTypeface.FromFamilyName(stil.FontFamily) ?? WbFonts.Regular, 15);
        leinwand.DrawText("N O T I Z B U C H", _page.Width / 2f, _page.Height * 0.49f,
            SKTextAlign.Center, unterSchrift, unterFarbe);
    }

    // ==================== Überlagerungen ====================

    /// <summary>Alles, was nur während einer Aktion zu sehen ist — liegt über den Elementen.</summary>
    private void DrawActiveOverlays(SKCanvas leinwand)
    {
        DrawActiveStroke(leinwand);

        var akzent = ThemeSk(ThemeColor.Accent);
        DrawLassoPath(leinwand, akzent);
        DrawSelectionFrame(leinwand, akzent);
        DrawEraserCursor(leinwand);
    }

    /// <summary>Der Strich, der gerade gezogen wird — noch nicht im Dokument.</summary>
    private void DrawActiveStroke(SKCanvas leinwand)
    {
        if (!_drawing || _activePoints is not { Count: > 0 }) return;

        WbRenderer.DrawStroke(leinwand, new StrokeElement
        {
            // Eine Kopie: der Eingabepfad hängt weitere Punkte an, während dieses Bild
            // aufgezeichnet wird — beides läuft zwar auf dem Oberflächen-Faden, aber der
            // Strich soll nicht halb aus zwei Zuständen bestehen.
            Points = [.. _activePoints],
            Color = CurrentInkHex(),
            Width = AktiveStrichbreite(),
            Kind = AktiveStrichart(),
        });
    }

    private StrokeKind AktiveStrichart() => _tool switch
    {
        ToolType.Pencil => StrokeKind.Pencil,
        ToolType.Highlighter => StrokeKind.Highlighter,
        _ => StrokeKind.Pen,
    };

    /// <summary>Der Textmarker ist breit — sonst markiert er nichts.</summary>
    private float AktiveStrichbreite() =>
        AktiveStrichart() == StrokeKind.Highlighter ? Math.Max(_width * 5f, 10f) : _width;

    private void DrawLassoPath(SKCanvas leinwand, SKColor akzent)
    {
        if (_lassoPts is not { Count: > 1 }) return;

        using var pfad = new SKPath();
        pfad.MoveTo(_lassoPts[0]);
        for (int i = 1; i < _lassoPts.Count; i++) pfad.LineTo(_lassoPts[i]);

        using var flaeche = new SKPaint { Color = akzent.WithAlpha(25), IsAntialias = true };
        leinwand.DrawPath(pfad, flaeche);

        using var rand = new SKPaint
        {
            Color = akzent,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.4f / Zoom,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash([6f / Zoom, 4f / Zoom], 0),
        };
        leinwand.DrawPath(pfad, rand);
    }

    /// <summary>
    /// Auswahlrahmen. Für M1 ein achsenparalleler Kasten — Dreh- und Skaliergriffe hängen
    /// an Werkzeugen, die es hier noch nicht gibt.
    /// </summary>
    private void DrawSelectionFrame(SKCanvas leinwand, SKColor akzent)
    {
        if (_selection.Count == 0) return;

        var b = InflatedSelectionBounds();
        using var flaeche = new SKPaint { Color = akzent.WithAlpha(18) };
        using var rand = new SKPaint
        {
            Color = akzent,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.4f / Zoom,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash([6f / Zoom, 4f / Zoom], 0),
        };
        leinwand.DrawRect(b, flaeche);
        leinwand.DrawRect(b, rand);
    }

    /// <summary>Radierer statt Zeiger: ein Ring in der eingestellten Größe.</summary>
    private void DrawEraserCursor(SKCanvas leinwand)
    {
        if (!_eraserVisible || EffectiveTool != ToolType.Eraser) return;

        using var ring = new SKPaint
        {
            Color = ThemeSk(ThemeColor.Text).WithAlpha(160),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.2f / Zoom,
            IsAntialias = true,
        };
        leinwand.DrawCircle(_eraserPos, _eraserRadius / Zoom, ring);
    }
}
