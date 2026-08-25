using System.Windows;
using GonkNote.Core.Editing;
using GonkNote.Core.Rendering;
using GonkNote.Services;
using SkiaSharp;
using GonkNote.Core.Platform;

namespace GonkNote.Views;

/// <summary>
/// Zeichenhilfen Lineal und Geodreieck: Platzieren, Drehen, Einrasten.
///
/// <para>
/// <b>Seit Phase 4.5 steht hier nur noch die Bedienung.</b> Geometrie (Umriss, Kanten,
/// Treffer, Einrasten, Winkelraster) liegt in <see cref="WbZeichenhilfe"/>, die Zeichnung in
/// <see cref="WbAidRenderer"/> — beides Core, beides ohne Steuerelement. Vorher waren es hier
/// rund zweihundert Zeilen, und §6 hielt Lineal und Geodreieck deshalb für „reine
/// Bedienarbeit"; das Messen zum Portieren hat es widerlegt (§4.59).
/// </para>
/// </summary>
public partial class WhiteboardView
{
    private void Ruler_Click(object sender, RoutedEventArgs e) => SetAid(Zeichenhilfe.Lineal);
    private void SetSquare_Click(object sender, RoutedEventArgs e) => SetAid(Zeichenhilfe.Geodreieck);

    /// <summary>Schaltet eine Zeichenhilfe ein bzw. (bei erneutem Klick) aus. Beide schließen sich aus.</summary>
    private void SetAid(Zeichenhilfe kind)
    {
        _aid = _aid == kind ? Zeichenhilfe.Keine : kind;
        if (_aid != Zeichenhilfe.Keine) _lastAid = _aid;
        BtnRuler.IsChecked = _aid == Zeichenhilfe.Lineal;
        BtnSetSquare.IsChecked = _aid == Zeichenhilfe.Geodreieck;
        // Gruppe aufklappen, solange eine Hilfe aktiv ist (zum Umschalten), sonst nur die zuletzt benutzte zeigen
        SetAidGroupExpanded(_aid != Zeichenhilfe.Keine);
        if (_aid != Zeichenhilfe.Keine && !_aidPlaced)
        {
            var v = VisibleCanvasRect();
            _aidCenter = new SKPoint(v.MidX, v.MidY);
            _aidAngleDeg = 0f;
            _aidPlaced = true;
        }
        Skia.InvalidateVisual();
    }

    private void SetAidGroupExpanded(bool expanded) =>
        GruppeKlappen(AidButtons, _lastAid == Zeichenhilfe.Geodreieck ? BtnSetSquare : BtnRuler,
                      expanded, out _aidGroupExpanded);

    // ==================== Was der Zeiger trifft ====================

    private bool AidBodyContains(SKPoint c) =>
        WbZeichenhilfe.TrifftKoerper(_aid, _aidCenter, _aidAngleDeg, c);

    /// <summary>Prüft, ob ein Strichstart nahe einer Kante liegt, und aktiviert das Einrasten auf diese Kante.</summary>
    private bool TryActivateAidSnap(SKPoint c)
    {
        _aidSnap = WbZeichenhilfe.Einrasten(_aid, _aidCenter, _aidAngleDeg, c);
        return _aidSnap != null;
    }

    /// <summary>Projiziert einen Punkt auf die eingerastete Kantenlinie (sonst unverändert).</summary>
    private SKPoint ApplyAidSnap(SKPoint p) =>
        _aidSnap is { } kante ? WbZeichenhilfe.AufKante(kante, p) : p;

    /// <summary>Startet Bewegen/Drehen, wenn Körper bzw. Dreh-Griff getroffen wird.</summary>
    private bool TryBeginAid(SKPoint c)
    {
        if (_aid == Zeichenhilfe.Keine) return false;

        if (WbZeichenhilfe.TrifftGriff(_aid, _aidCenter, _aidAngleDeg, Zoom, c))
        {
            _rulerDrag = RulerDrag.Rotate;
            Skia.InvalidateVisual();
            return true;
        }
        if (AidBodyContains(c))
        {
            _rulerDrag = RulerDrag.Move;
            _rulerDragLast = c;
            Skia.InvalidateVisual();
            return true;
        }
        return false;
    }

    private void UpdateAidDrag(SKPoint c)
    {
        if (_rulerDrag == RulerDrag.Move)
        {
            _aidCenter = new SKPoint(_aidCenter.X + (c.X - _rulerDragLast.X), _aidCenter.Y + (c.Y - _rulerDragLast.Y));
            _rulerDragLast = c;
        }
        else if (_rulerDrag == RulerDrag.Rotate)
        {
            float raw = MathF.Atan2(c.Y - _aidCenter.Y, c.X - _aidCenter.X) * 180f / MathF.PI;
            _aidAngleDeg = WbZeichenhilfe.WinkelFangen(raw);
        }
        Skia.InvalidateVisual();
    }

    // ==================== Zeichnen ====================

    private void DrawActiveAid(SKCanvas canvas) =>
        WbAidRenderer.DrawAid(canvas, _aid, _aidCenter, _aidAngleDeg, Zoom,
                              App.Platform.Theme.Current == AppTheme.Dark,
                              ResColorFromBrush("Brush.Accent"),
                              _rulerDrag == RulerDrag.Rotate);

    /// <summary>
    /// Zeichnet das Geodreieck-SVG um <paramref name="center"/> mit Drehung
    /// <paramref name="angleDeg"/>. Leitet auf <see cref="WbAidRenderer"/> in GonkNote.Core
    /// weiter, damit WPF und der Avalonia-Port dieselben Assets und dieselbe Geometrie
    /// nutzen. <b>Statisch, damit der Render-Harness denselben Code aufruft.</b>
    /// </summary>
    public static void DrawSetSquare(SKCanvas canvas, SKPoint center, float angleDeg, float zoom) =>
        WbAidRenderer.DrawSetSquare(canvas, center, angleDeg, zoom,
                                    App.Platform.Theme.Current == AppTheme.Dark);
}
