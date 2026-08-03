using System.Globalization;
using System.Runtime.InteropServices;

namespace GonkNote.Werkzeug;

/// <summary>
/// Klicken und Tippen unter X11 — das, was <c>SendKeys</c> und die Maus-Aufrufe in
/// <c>tools/klick.ps1</c> unter Windows tun.
///
/// <para>
/// <b>Warum das selbst gebaut ist und nicht xdotool aufruft:</b> xdotool ist ein Paket, das
/// erst installiert werden muss, und auf diesem Laptop braucht <c>sudo</c> ein Passwort.
/// Die zwei Bibliotheken, die xdotool selbst benutzt — <c>libX11</c> und <c>libXtst</c> —
/// liegen dagegen ohnehin auf jedem X-System; ohne sie liefe die Sitzung nicht. Ein
/// Werkzeug, das nur diese beiden aufruft, läuft damit sofort und überall, und es ist
/// kürzer als die Anleitung, wie man das fehlende Paket nachinstalliert.
/// </para>
/// <para>
/// <b>Was es nicht kann: den Stift.</b> XTEST erzeugt Maus- und Tastaturereignisse. Druck,
/// Neigung und die Unterscheidung Stift/Finger entstehen im Digitizer und im
/// libinput-Pfad darüber — sie lassen sich von hier aus nicht nachbilden. Alles, was am
/// Stift hängt, bleibt Handarbeit; dafür gibt es die Stift-Anzeige mit F9 in der
/// Zeichenfläche, die auf einem Foto nachlesbar ist.
/// </para>
/// </summary>
internal static class Programm
{
    private const string X11 = "libX11.so.6";
    private const string XTst = "libXtst.so.6";

    [DllImport(X11)] private static extern IntPtr XOpenDisplay(string? name);
    [DllImport(X11)] private static extern int XCloseDisplay(IntPtr d);
    [DllImport(X11)] private static extern int XFlush(IntPtr d);
    [DllImport(X11)] private static extern uint XStringToKeysym(string name);
    [DllImport(X11)] private static extern byte XKeysymToKeycode(IntPtr d, uint keysym);

    [DllImport(X11)] private static extern IntPtr XDefaultRootWindow(IntPtr d);
    [DllImport(X11)] private static extern uint XInternAtom(IntPtr d, string name, bool onlyIfExists);
    [DllImport(X11)] private static extern int XFree(IntPtr data);

    [DllImport(X11)]
    private static extern int XGetWindowProperty(
        IntPtr d, IntPtr w, uint property, long offset, long length, bool delete, uint reqType,
        out uint actualType, out int actualFormat, out ulong nItems, out ulong bytesAfter, out IntPtr prop);

    [DllImport(X11)]
    private static extern int XGetGeometry(
        IntPtr d, IntPtr w, out IntPtr root, out int x, out int y,
        out uint width, out uint height, out uint border, out uint depth);

    [DllImport(X11)]
    private static extern int XTranslateCoordinates(
        IntPtr d, IntPtr src, IntPtr dest, int srcX, int srcY,
        out int destX, out int destY, out IntPtr child);

    [DllImport(X11)]
    private static extern bool XQueryPointer(
        IntPtr d, IntPtr w, out IntPtr root, out IntPtr child,
        out int rootX, out int rootY, out int winX, out int winY, out uint mask);

    [DllImport(XTst)] private static extern int XTestFakeMotionEvent(IntPtr d, int screen, int x, int y, ulong delay);
    [DllImport(XTst)] private static extern int XTestFakeButtonEvent(IntPtr d, uint button, bool press, ulong delay);
    [DllImport(XTst)] private static extern int XTestFakeKeyEvent(IntPtr d, uint keycode, bool press, ulong delay);

    private static IntPtr _anzeige;

    private static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Hilfe();
            return args.Length == 0 ? 2 : 0;
        }

        _anzeige = XOpenDisplay(null);
        if (_anzeige == IntPtr.Zero)
        {
            Console.Error.WriteLine("Keine Verbindung zum X-Server. Steht DISPLAY?");
            return 3;
        }

        try
        {
            if (args[0] == "pos")
            {
                XQueryPointer(_anzeige, XDefaultRootWindow(_anzeige), out _, out IntPtr kind,
                    out int rx, out int ry, out _, out _, out uint maske);
                Console.WriteLine($"x={rx} y={ry} unter=0x{kind.ToInt64():x} maske=0x{maske:x}");
                return 0;
            }

            if (args[0] == "fenster")
            {
                var f = ErstesFenster();
                if (f == null) { Console.Error.WriteLine("Kein verwaltetes Fenster gefunden."); return 4; }
                Console.WriteLine($"id=0x{f.Value.Id.ToInt64():x} x={f.Value.X} y={f.Value.Y} " +
                                  $"w={f.Value.Breite} h={f.Value.Hoehe}");
                return 0;
            }

            foreach (var schritt in args)
                if (!Ausfuehren(schritt)) return 2;
        }
        finally
        {
            XFlush(_anzeige);
            XCloseDisplay(_anzeige);
        }
        return 0;
    }

    // ==================== Die Eichung ====================
    //
    // **XTEST-Koordinaten sind nicht die Koordinaten, die dabei herauskommen.** Unter
    // GNOME-Wayland nimmt mutter die Ereignisse von XWayland entgegen und rechnet sie mit
    // dem Skalierungsfaktor der Sitzung hoch: ein XTestFakeMotionEvent auf (500,500) setzt
    // den Zeiger auf diesem Laptop tatsaechlich auf (1000,1000). XQueryPointer meldet
    // danach die *echte* Lage, XGetGeometry ebenfalls -- die Eingabeseite ist die einzige,
    // die skaliert.
    //
    // Das ist die teuerste Falle dieses Werkzeugs, weil sie sich nicht als Fehler zeigt: es
    // wird geklickt, es kommt auch an (die Leerlaufuhr der Sitzung springt zurueck), nur
    // eben an der doppelten Stelle. Wer auf einem Foto misst und hier einsetzt, trifft
    // scheinbar zufaellig mal etwas und mal nichts.
    //
    // Der Faktor wird deshalb nicht angenommen, sondern **gemessen**: einmal irgendwohin
    // fahren, nachsehen, wo der Zeiger gelandet ist. Damit stimmt es auch auf einem
    // Rechner ohne Skalierung (Faktor 1) und bei gebrochener Skalierung.
    private static double? _faktor;

    private static double Faktor()
    {
        if (_faktor is { } f) return f;

        const int probe = 400;
        XTestFakeMotionEvent(_anzeige, -1, probe, probe, 0);
        XFlush(_anzeige);
        Thread.Sleep(60);

        XQueryPointer(_anzeige, XDefaultRootWindow(_anzeige), out _, out _,
            out int rx, out int ry, out _, out _, out _);

        // Nur uebernehmen, wenn beide Achsen dasselbe sagen und das Ergebnis plausibel ist;
        // sonst lieber ungeeicht weiterlaufen als eine falsche Zahl festhalten.
        double fx = rx / (double)probe, fy = ry / (double)probe;
        _faktor = Math.Abs(fx - fy) < 0.05 && fx > 0.2 && fx < 8 ? fx : 1.0;
        return _faktor.Value;
    }

    /// <summary>Den Zeiger auf eine <b>echte</b> Bildschirmposition setzen.</summary>
    private static void Bewege(int x, int y)
    {
        double f = Faktor();
        XTestFakeMotionEvent(_anzeige, -1, (int)Math.Round(x / f), (int)Math.Round(y / f), 0);
        XFlush(_anzeige);
    }

    private readonly record struct Fenster(IntPtr Id, int X, int Y, uint Breite, uint Hoehe);

    /// <summary>
    /// Das erste vom Fenstermanager verwaltete Fenster (<c>_NET_CLIENT_LIST</c>), mit seiner
    /// Lage auf dem <b>Bildschirm</b>.
    /// <para>
    /// Das ist die Antwort auf eine Falle, die unter Windows echtes Geld gekostet hat:
    /// „Fensterkoordinaten sind nicht Bildschirmkoordinaten" (HANDOFF §7). Wer auf einer
    /// Fensteraufnahme misst und auf dem Bildschirm klickt, liegt um den Fensterursprung
    /// daneben — bei einem großen Knopf egal, bei einem Menüeintrag nicht. Mit dieser
    /// Auskunft rechnet <c>w:x,y</c> das von selbst um.
    /// </para>
    /// </summary>
    private static Fenster? ErstesFenster()
    {
        uint liste = XInternAtom(_anzeige, "_NET_CLIENT_LIST", true);
        if (liste == 0) return null;

        const uint AnyPropertyType = 0;
        if (XGetWindowProperty(_anzeige, XDefaultRootWindow(_anzeige), liste, 0, 64, false,
                AnyPropertyType, out _, out _, out ulong anzahl, out _, out IntPtr daten) != 0 ||
            daten == IntPtr.Zero || anzahl == 0)
            return null;

        // Fensterkennungen stehen als 32-Bit-Werte in der Eigenschaft, werden von Xlib aber
        // in `long` zurückgegeben — auf 64 Bit sind das acht Byte je Eintrag.
        var id = new IntPtr(Marshal.ReadInt64(daten));
        XFree(daten);

        if (XGetGeometry(_anzeige, id, out IntPtr wurzel, out _, out _,
                out uint w, out uint h, out _, out _) == 0)
            return null;

        // Über den Wurzelursprung, nicht über x/y aus XGetGeometry: Letztere sind relativ
        // zum Elternfenster, und beim umrahmten Fenster ist das der Rahmen, nicht die Wurzel.
        XTranslateCoordinates(_anzeige, id, wurzel, 0, 0, out int bx, out int by, out _);
        return new Fenster(id, bx, by, w, h);
    }

    private static bool Ausfuehren(string schritt)
    {
        // "#Tasten" — eine Tastenkombination, xdotool-Schreibweise: #Escape #ctrl+z #F9
        if (schritt.StartsWith('#')) return Tasten(schritt[1..]);

        // "w:x,y[,art]" — fensterrelativ. Rechnet den Fensterursprung selbst dazu.
        // "w:z:x1,y1>x2,y2" — dasselbe für eine Bahn.
        if (schritt.StartsWith("w:", StringComparison.Ordinal))
        {
            var f = ErstesFenster();
            if (f == null) { Console.Error.WriteLine("Kein verwaltetes Fenster gefunden."); return false; }

            if (schritt.StartsWith("w:z:", StringComparison.Ordinal))
            {
                var bahn = string.Join(">", schritt[4..].Split('>', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s =>
                    {
                        var q = s.Split(',');
                        return $"{int.Parse(q[0]) + f.Value.X},{int.Parse(q[1]) + f.Value.Y}";
                    }));
                return Ziehen(bahn);
            }

            var t = schritt[2..].Split(',');
            if (t.Length < 2 || !int.TryParse(t[0], out int wx) || !int.TryParse(t[1], out int wy))
            {
                Console.Error.WriteLine($"Unbekannter Schritt: {schritt}");
                return false;
            }
            string rest = t.Length == 3 ? "," + t[2] : "";
            return Ausfuehren($"{wx + f.Value.X},{wy + f.Value.Y}{rest}");
        }

        // ":Text" — Text tippen
        if (schritt.StartsWith(':')) return Tippen(schritt[1..]);

        // "warte:500" — Pause in Millisekunden
        if (schritt.StartsWith("warte:", StringComparison.Ordinal))
        {
            Thread.Sleep(int.Parse(schritt[6..], CultureInfo.InvariantCulture));
            return true;
        }

        // "z:x1,y1>x2,y2>…" — ziehen. Aufsetzen, über die Stützpunkte fahren, absetzen.
        if (schritt.StartsWith("z:", StringComparison.Ordinal)) return Ziehen(schritt[2..]);

        // "x,y" / "x,y,2" / "x,y,r"
        var teile = schritt.Split(',');
        if (teile.Length is < 2 or > 3 ||
            !int.TryParse(teile[0], out int x) || !int.TryParse(teile[1], out int y))
        {
            Console.Error.WriteLine($"Unbekannter Schritt: {schritt}");
            return false;
        }

        string art = teile.Length == 3 ? teile[2] : "1";
        uint taste = art == "r" ? 3u : 1u;
        int male = art == "2" ? 2 : 1;

        Bewege(x, y);
        Thread.Sleep(40);   // dem Fenstersystem Zeit lassen, den Zeiger anzunehmen

        for (int i = 0; i < male; i++)
        {
            XTestFakeButtonEvent(_anzeige, taste, true, 0);
            XTestFakeButtonEvent(_anzeige, taste, false, 0);
            XFlush(_anzeige);
            if (i + 1 < male) Thread.Sleep(60);   // innerhalb der Doppelklick-Frist bleiben
        }
        return true;
    }

    /// <summary>
    /// Ziehen: aufsetzen, über die Stützpunkte fahren, absetzen. Das ist der Schritt, ohne
    /// den sich ein Strich gar nicht prüfen lässt — ein Klick allein erzeugt nur einen Punkt.
    /// <para>
    /// Zwischen den Stützpunkten wird <b>interpoliert</b>, und zwar in kleinen Schritten.
    /// Das ist kein Luxus: die Zeichenfläche verwirft Punkte, die dichter als ein
    /// Bildschirmpunkt beieinanderliegen, und sie glättet über den Vorgänger. Wer nur die
    /// Ecken sendet, prüft damit nicht den Strichaufbau, sondern nur, ob überhaupt etwas
    /// ankommt.
    /// </para>
    /// <para>
    /// <b>Es bleibt eine Maus.</b> Druck und Neigung entstehen im Digitizer; hier kommt
    /// jeder Punkt mit demselben Rückfallwert an. Genau so soll es sein — es prüft den
    /// Rückfallpfad, nicht den Stiftpfad.
    /// </para>
    /// </summary>
    private static bool Ziehen(string bahn)
    {
        var punkte = new List<(int X, int Y)>();
        foreach (var s in bahn.Split('>', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = s.Split(',');
            if (t.Length != 2 || !int.TryParse(t[0], out int x) || !int.TryParse(t[1], out int y))
            {
                Console.Error.WriteLine($"Unbekannter Punkt in der Bahn: {s}");
                return false;
            }
            punkte.Add((x, y));
        }
        if (punkte.Count < 2)
        {
            Console.Error.WriteLine("Eine Bahn braucht mindestens zwei Punkte.");
            return false;
        }

        Bewege(punkte[0].X, punkte[0].Y);
        Thread.Sleep(60);

        XTestFakeButtonEvent(_anzeige, 1, true, 0);
        XFlush(_anzeige);
        Thread.Sleep(40);

        for (int i = 1; i < punkte.Count; i++)
        {
            var (ax, ay) = punkte[i - 1];
            var (bx, by) = punkte[i];
            int schritte = Math.Max(2, (int)(Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay)) / 6));

            for (int s = 1; s <= schritte; s++)
            {
                Bewege(ax + (bx - ax) * s / schritte, ay + (by - ay) * s / schritte);
                Thread.Sleep(6);
            }
        }

        Thread.Sleep(40);
        XTestFakeButtonEvent(_anzeige, 1, false, 0);
        XFlush(_anzeige);
        return true;
    }

    /// <summary>Eine Kombination wie <c>ctrl+shift+z</c> oder eine einzelne Taste wie <c>Escape</c>.</summary>
    private static bool Tasten(string kombination)
    {
        var teile = kombination.Split('+', StringSplitOptions.RemoveEmptyEntries);
        var codes = new List<byte>();

        foreach (var t in teile)
        {
            string name = t.ToLowerInvariant() switch
            {
                "ctrl" or "control" => "Control_L",
                "shift" => "Shift_L",
                "alt" => "Alt_L",
                "super" or "meta" => "Super_L",
                _ => t,
            };

            uint sym = XStringToKeysym(name);
            if (sym == 0)
            {
                Console.Error.WriteLine($"Unbekannte Taste: {t}");
                return false;
            }
            byte code = XKeysymToKeycode(_anzeige, sym);
            if (code == 0)
            {
                Console.Error.WriteLine($"Taste ohne Code auf dieser Belegung: {t}");
                return false;
            }
            codes.Add(code);
        }

        // Erst alle drücken, dann in umgekehrter Reihenfolge loslassen — sonst hängt ein
        // Umschalter fest und jede folgende Eingabe kommt verändert an.
        foreach (var c in codes) XTestFakeKeyEvent(_anzeige, c, true, 0);
        for (int i = codes.Count - 1; i >= 0; i--) XTestFakeKeyEvent(_anzeige, codes[i], false, 0);
        XFlush(_anzeige);
        return true;
    }

    private static bool Tippen(string text)
    {
        foreach (char z in text)
        {
            // Über den Unicode-Punkt: X kennt für jedes Zeichen einen Keysym der Form
            // 0x01000000 + Codepunkt. Für die Zeichen, die ohnehin auf der Belegung
            // liegen, findet XKeysymToKeycode denselben Code.
            uint sym = z switch
            {
                ' ' => XStringToKeysym("space"),
                '\n' => XStringToKeysym("Return"),
                '\t' => XStringToKeysym("Tab"),
                _ => 0x01000000u + z,
            };

            byte code = XKeysymToKeycode(_anzeige, sym);
            if (code == 0 && z is >= ' ' and <= '~')
                code = XKeysymToKeycode(_anzeige, z);   // ASCII-Keysyms sind der Codepunkt selbst

            if (code == 0)
            {
                Console.Error.WriteLine($"Zeichen liegt nicht auf der Tastaturbelegung: '{z}'");
                return false;
            }

            bool gross = char.IsUpper(z);
            byte umschalt = XKeysymToKeycode(_anzeige, XStringToKeysym("Shift_L"));

            if (gross) XTestFakeKeyEvent(_anzeige, umschalt, true, 0);
            XTestFakeKeyEvent(_anzeige, code, true, 0);
            XTestFakeKeyEvent(_anzeige, code, false, 0);
            if (gross) XTestFakeKeyEvent(_anzeige, umschalt, false, 0);
            XFlush(_anzeige);
            Thread.Sleep(12);
        }
        return true;
    }

    private static void Hilfe() => Console.WriteLine("""
        zeiger — klicken und tippen unter X11 (Wegwerf-Werkzeug, HANDOFF §3)

        Aufruf: zeiger <Schritt> [<Schritt> ...]

          x,y         Klick (linke Taste), echte Bildschirmpixel
          x,y,2       Doppelklick
          x,y,r       Rechtsklick
          w:x,y[,art] dasselbe, aber relativ zur linken oberen Ecke des Fensters
          z:x1,y1>x2,y2>...  ziehen (aufsetzen, fahren, absetzen)
          #Tasten     Tastenkombination: #Escape  #ctrl+z  #F9  #ctrl+shift+a
          :Text       Text tippen
          warte:500   Pause in Millisekunden

        Sonderaufruf:
          zeiger fenster    Kennung, Lage und Groesse des Fensters ausgeben

        AUF EINER FENSTERAUFNAHME GEMESSENE PUNKTE MIT w: ANGEBEN. Sie sind relativ zum
        Fenster, Klicks gehen aber auf den Bildschirm -- w: rechnet den Ursprung dazu.
        """);
}
