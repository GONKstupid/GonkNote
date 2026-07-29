using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace GonkNote.StylusProbe;

/// <summary>Ein einzelner abgetasteter Punkt eines Strichs.</summary>
internal readonly record struct Abtastung(Point Ort, double Radius);

/// <summary>
/// Zeichenflaeche des Stylus-Prototyps. Stellt den von Avalonia gemeldeten
/// Druck als Kreisradius dar und faellt auf eine geschwindigkeitsbasierte
/// Breite zurueck, sobald das Geraet keinen Druck liefert. Der Fallback ist
/// Pflicht: die App soll mit jedem Stylus einen sauberen Strich zeigen, nicht
/// nur mit druckfaehigen.
/// </summary>
public class StiftFlaeche : Control
{
    /// <summary>Ersatzwert, den Avalonia meldet, wenn ein Geraet keinen Druck kann.</summary>
    private const double DruckErsatzwert = 0.5;

    private const double RadiusMin = 0.7;
    private const double RadiusMax = 9.0;

    /// <summary>Ab so vielen verschiedenen Druckwerten gilt der Druck als echt.</summary>
    private const int DruckNachweisSchwelle = 3;

    private readonly List<List<Abtastung>> _striche = new();
    private List<Abtastung>? _aktuellerStrich;

    private readonly Stopwatch _uhr = Stopwatch.StartNew();
    private Point? _vorigerOrt;
    private double _vorigeZeitMs;
    private double _vorigeBreite = 3.0;

    // --- Diagnose ---
    // Druckfaehigkeit muss je Zeigertyp entschieden werden, nicht global: sonst
    // gilt nach dem ersten Stiftstrich auch die Maus als druckfaehig und bekommt
    // aus ihrem konstanten Ersatzwert 0,5 eine starre Mittelbreite, statt in den
    // Geschwindigkeits-Fallback zu laufen.
    private readonly Dictionary<PointerType, HashSet<float>> _druckJeTyp = new();
    private readonly HashSet<PointerType> _gesehenerTyp = new();
    // Nur den letzten Neigungswert zu merken sagt nichts aus - der stammt oft
    // vom zuletzt benutzten Zeiger (Maus meldet konstant 0).
    private readonly HashSet<float> _gesehenNeigungX = new();
    private readonly HashSet<float> _gesehenNeigungY = new();
    private PointerType _zeigertyp = PointerType.Mouse;
    private float _druck, _neigungX, _neigungY;
    private int _abtastungen;
    private int _striche_gesamt;

    /// <summary>
    /// Druck gilt fuer einen Zeigertyp erst als echt, wenn von ihm mehrere
    /// verschiedene Werte ankamen. Ein Geraet ohne Druck liefert konstant
    /// <see cref="DruckErsatzwert"/>.
    /// </summary>
    private bool DruckIstEcht(PointerType typ) =>
        _druckJeTyp.TryGetValue(typ, out var werte) && werte.Count >= DruckNachweisSchwelle;

    public StiftFlaeche()
    {
        // Control liefert keinen Hintergrund mit; ohne gefuellte Flaeche waere
        // auch nicht sichtbar, wo gezeichnet werden kann.
        ClipToBounds = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var eigenschaften = e.GetCurrentPoint(this).Properties;

        // Rechte Taste bzw. Stiftknopf leert die Flaeche
        if (eigenschaften.IsRightButtonPressed || eigenschaften.IsMiddleButtonPressed)
        {
            _striche.Clear();
            _aktuellerStrich = null;
            InvalidateVisual();
            return;
        }

        _aktuellerStrich = new List<Abtastung>();
        _striche.Add(_aktuellerStrich);
        _striche_gesamt++;
        _vorigerOrt = null;
        e.Pointer.Capture(this);
        Uebernehmen(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_aktuellerStrich is not null)
            Uebernehmen(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _aktuellerStrich = null;
        _vorigerOrt = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    /// <summary>
    /// Uebernimmt alle Abtastungen des Events. GetIntermediatePoints liefert die
    /// Zwischenpunkte, die zwischen zwei UI-Frames angefallen sind - ohne die
    /// verliert man bei einem 100+ Hz Digitizer den Grossteil der Aufloesung.
    /// </summary>
    private void Uebernehmen(PointerEventArgs e)
    {
        var punkte = e.GetIntermediatePoints(this);
        if (punkte.Count == 0)
            punkte = new[] { e.GetCurrentPoint(this) };

        foreach (var punkt in punkte)
        {
            var eigenschaften = punkt.Properties;
            _zeigertyp = e.Pointer.Type;
            _gesehenerTyp.Add(_zeigertyp);
            _druck = eigenschaften.Pressure;
            _neigungX = eigenschaften.XTilt;
            _neigungY = eigenschaften.YTilt;
            if (!_druckJeTyp.TryGetValue(_zeigertyp, out var druckwerte))
                _druckJeTyp[_zeigertyp] = druckwerte = new HashSet<float>();
            druckwerte.Add(_druck);

            // nur vom Stift, sonst verwaessern die konstanten Nullen der Maus
            if (_zeigertyp == PointerType.Pen)
            {
                _gesehenNeigungX.Add(_neigungX);
                _gesehenNeigungY.Add(_neigungY);
            }
            _abtastungen++;

            var radius = RadiusBestimmen(punkt.Position, _druck, _zeigertyp);
            _aktuellerStrich?.Add(new Abtastung(punkt.Position, radius));
        }

        InvalidateVisual();
    }

    /// <summary>
    /// Radius aus dem Druck - oder, wenn kein Druck vorliegt, aus der
    /// Zeichengeschwindigkeit: schnell gezogen wird duenner, langsam dicker.
    /// </summary>
    private double RadiusBestimmen(Point ort, double druck, PointerType typ)
    {
        var jetzt = _uhr.Elapsed.TotalMilliseconds;

        if (DruckIstEcht(typ))
        {
            _vorigerOrt = ort;
            _vorigeZeitMs = jetzt;
            return RadiusMin + druck * (RadiusMax - RadiusMin);
        }

        // --- Fallback ohne Druck ---
        double breite;
        if (_vorigerOrt is { } vorher)
        {
            var strecke = Math.Sqrt(Math.Pow(ort.X - vorher.X, 2) + Math.Pow(ort.Y - vorher.Y, 2));
            var dauer = Math.Max(jetzt - _vorigeZeitMs, 1.0);
            var tempo = strecke / dauer; // px/ms

            var roh = RadiusMax - tempo * 3.0;
            roh = Math.Clamp(roh, RadiusMin, RadiusMax * 0.7);

            // Glaetten, sonst zappelt die Strichbreite bei jedem Ruckler
            breite = _vorigeBreite * 0.7 + roh * 0.3;
        }
        else
        {
            breite = 3.0;
        }

        _vorigeBreite = breite;
        _vorigerOrt = ort;
        _vorigeZeitMs = jetzt;
        return breite;
    }

    public override void Render(DrawingContext kontext)
    {
        base.Render(kontext);
        kontext.FillRectangle(Brushes.White, new Rect(Bounds.Size));

        foreach (var strich in _striche)
            foreach (var abtastung in strich)
                kontext.DrawEllipse(Brushes.Black, null, abtastung.Ort,
                                    abtastung.Radius, abtastung.Radius);

        var kopfzeile = new FormattedText(
            Diagnose(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Parse("monospace")), 13, Brushes.DarkSlateBlue);
        kontext.DrawText(kopfzeile, new Point(12, 10));
    }

    private string Diagnose() =>
        $"""
         Backend    : {Umgebung.BackendName}   Session: {Umgebung.SitzungsTyp}
         Zeigertyp  : {_zeigertyp}   gesehen: {string.Join(", ", _gesehenerTyp)}
         Druck      : {_druck:F3}   verschiedene Werte: {DruckwerteVon(_zeigertyp).Count}
         Modus      : {(DruckIstEcht(_zeigertyp) ? "DRUCK (echt)" : "FALLBACK (Geschwindigkeit)")}
         Neigung    : X {_neigungX,6:F1}   Y {_neigungY,6:F1}   (Stift-Werte: {_gesehenNeigungX.Count}/{_gesehenNeigungY.Count})
         Abtastungen: {_abtastungen}   Striche: {_striche_gesamt}

         Rechte Taste leert die Flaeche.
         """;

    /// <summary>Fasst die Messung fuer den Bericht zusammen (wird beim Beenden geschrieben).</summary>
    public string Bericht()
    {
        var jeTyp = string.Join(Environment.NewLine, _druckJeTyp
            .OrderBy(p => p.Key.ToString())
            .Select(p => $"  {p.Key,-6}: {DruckbefundVon(p.Value)}"));

        return $"""
                === GonkNote Stylus-Prototyp: Messung ===
                Backend            : {Umgebung.BackendName}
                XDG_SESSION_TYPE   : {Umgebung.SitzungsTyp}
                WAYLAND_DISPLAY    : {Umgebung.WaylandAnzeige}
                DISPLAY            : {Umgebung.X11Anzeige}

                Zeigertypen gesehen: {string.Join(", ", _gesehenerTyp)}
                Abtastungen        : {_abtastungen}
                Striche            : {_striche_gesamt}

                Druck je Zeigertyp:
                {jeTyp}

                ERGEBNIS           : {(DruckIstEcht(PointerType.Pen) ? "DRUCK KOMMT IN AVALONIA AN" : "KEIN DRUCK VOM STIFT IN AVALONIA")}

                Neigung (nur Stift): X {Achsenbefund(_gesehenNeigungX)}
                                     Y {Achsenbefund(_gesehenNeigungY)}
                """;
    }

    private HashSet<float> DruckwerteVon(PointerType typ) =>
        _druckJeTyp.TryGetValue(typ, out var werte) ? werte : new HashSet<float>();

    /// <summary>Beschreibt, ob ein Zeigertyp echten Druck liefert oder den Ersatzwert.</summary>
    private static string DruckbefundVon(HashSet<float> werte)
    {
        var echte = werte.Where(w => Math.Abs(w - DruckErsatzwert) > 0.0001f).ToList();
        if (werte.Count < DruckNachweisSchwelle)
            return $"kein Druck (konstant {string.Join("/", werte.Select(w => w.ToString("F2")))}) "
                 + "-> Fallback Geschwindigkeit";
        return $"{werte.Count} verschiedene Werte, {echte.Min():F4} .. {echte.Max():F4} -> echter Druck";
    }

    /// <summary>Fasst zusammen, ob eine Achse ueberhaupt variiert - und in welchem Bereich.</summary>
    private static string Achsenbefund(HashSet<float> werte)
    {
        if (werte.Count == 0) return "keine Stift-Abtastung";
        if (werte.Count == 1) return $"konstant {werte.First():F1} - KOMMT NICHT AN";
        return $"{werte.Min():F1} .. {werte.Max():F1}, {werte.Count} verschiedene Werte";
    }
}
