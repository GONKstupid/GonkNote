using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using GonkNote.Core.Rendering;
using GonkNote.Core.Text;

namespace GonkNote.Views;

/// <summary>
/// Tastatur und Maus — <b>Schritt 5 des Schreibens</b> (HANDOFF §6), und die Stelle, an der aus
/// den vier Bausteinen der Schritte 1 bis 4 ein Editor wird.
///
/// <para>
/// <b>Sie rechnet nichts.</b> Wohin ein Klick zeigt, weiß <see cref="TdHit"/>; was eine Eingabe
/// ändert, baut <see cref="TdEdit"/>; welche Schritte ein Handgriff sind, weiß
/// <see cref="TdUndo"/>. Was hier steht, ist die Übersetzung von Tasten und Zeigern in diese
/// Aufrufe — und die drei Dinge, die <b>nur hier</b> zu tun sind und die sonst niemand bemerkt:
/// <list type="number">
///   <item>
///     <see cref="TdUndo.Abschliessen"/> rufen, sobald der Nutzer die Schreibmarke versetzt.
///     Der Verlauf sieht Änderungen und keine Klicks (§4.33).
///   </item>
///   <item>
///     Die Marke <b>blinken</b> lassen, indem mal mit und mal ohne gezeichnet wird. Ein Takt
///     gehört nicht in Core — er wäre eine Uhr (§4.20), und im PDF stünde plötzlich ein
///     Strich (§4.34).
///   </item>
///   <item>
///     Nach jeder Änderung <b>neu umbrechen</b>. Bis heute geschah das einmal je Dokument
///     (§4.28); mit dem Schreiben wird diese Entscheidung fällig — siehe
///     <see cref="UmbruchAnstossen"/>.
///   </item>
/// </list>
/// </para>
///
/// <para>
/// <b>Getippt wird über <c>TextInput</c> und nicht über <c>KeyDown</c></b> (§6). Eine Taste ist
/// kein Zeichen: Auf einer deutschen Belegung entsteht „ä" aus einer Taste, „é" aus zweien
/// (tote Taste) und ein Emoji aus einer Compose-Folge. Wer aus <see cref="Key"/> Zeichen
/// ableitet, schreibt eine Belegungstabelle nach, die das System schon hat — und bekommt auf
/// jeder anderen Tastatur etwas anderes. <c>KeyDown</c> bleibt für das, was wirklich Tasten
/// sind: Pfeile, Pos1, Entf, Tastenkürzel.
/// </para>
/// </summary>
public partial class TextDocView
{
    // ==================== Zustand ====================

    /// <summary>
    /// Wo die Schreibmarke steht und was ausgewählt ist. <b>Eine leere Auswahl *ist* der
    /// Cursor</b> — es gibt keinen zweiten Zustand dafür (§4.30).
    /// </summary>
    private TdSelection _auswahl;

    /// <summary>
    /// Auswahl und Marke, fertig gerechnet für den Zeichner. <c>null</c>, solange nichts
    /// angezeigt wird — dann bekommt <see cref="TdRenderContext"/> keine, und am Bild ändert
    /// sich nichts (§4.34).
    /// </summary>
    private TdMarkierung? _markierung;

    /// <summary>
    /// Die Zeile, in der die Marke steht — gemerkt, damit das Blinken sie nur aus- und
    /// wieder einhängen muss, statt die ganze Markierung neu zu rechnen.
    /// </summary>
    private TdLine? _markeZeile;

    private bool _markeAn = true;
    private DispatcherTimer? _blinker;

    /// <summary>
    /// Die angepeilte Spalte einer Reihe von Auf-/Ab-Bewegungen (<see cref="TdZeilenzug"/>) —
    /// <c>null</c>, sobald der Nutzer etwas anderes tut.
    /// </summary>
    private double? _spalte;

    /// <summary>Liegt der Zeiger gerade auf und zieht eine Auswahl?</summary>
    private bool _zieht;

    /// <summary>
    /// Wie lange der letzte Umbruch gedauert hat, in Millisekunden — die Zahl, an der
    /// <see cref="UmbruchAnstossen"/> entscheidet.
    /// </summary>
    private double _umbruchMs;

    private bool _umbruchFaellig;

    /// <summary>Steht die Schreibmarke in einem Dokument, das sich bearbeiten lässt?</summary>
    private bool Schreibbar => _modell is not null && _umbruch is not null;

    // ==================== Anschluss ====================

    /// <summary>
    /// Hängt Zeiger und Tastatur an die Zeichenfläche. <b>An die Fläche und nicht an das
    /// <c>UserControl</c></b>: Tastenereignisse laufen in Avalonia zum **fokussierten** Element,
    /// und das ist die Fläche (§7, <see cref="SkiaCanvas"/>).
    /// </summary>
    private void EingabeAnhaengen()
    {
        Skia.PointerPressed += Zeiger_Gedrueckt;
        Skia.PointerMoved += Zeiger_Bewegt;
        Skia.PointerReleased += Zeiger_Losgelassen;
        Skia.KeyDown += Taste;
        Skia.TextInput += Texteingabe;

        // Schritt 6a: Die Fläche meldet sich als **Eingabeziel** an — sonst hat eine
        // Bildschirmtastatur nichts, woran sie andocken könnte (§5 „Noch offen" 10).
        EingabemethodeAnhaengen();
    }

    /// <summary>
    /// Setzt Auswahl und Verlauf für ein frisch geladenes Dokument auf.
    /// <para>
    /// <b>Der Verlauf wird nur geleert, wenn er zu einem anderen Dokument gehört</b>
    /// (<see cref="TdUndo.Leeren"/>): Ein gemerkter Schritt zeigt auf die Blockliste, in der er
    /// entstanden ist. Beim bloßen Wechsel der Registerkarte ist es dieselbe — deshalb liegen
    /// Modell und Verlauf am Register und nicht hier.
    /// </para>
    /// </summary>
    private void EingabeAufsetzen()
    {
        _auswahl = _modell is null
            ? new TdSelection(TdPosition.Null)
            : new TdSelection(TdCursor.Anfang(_modell));

        _spalte = null;
        _zieht = false;

        // **Ein angefangenes Zusammensetzen gehört dem alten Dokument** (Schritt 6a): Eine halb
        // getippte Silbe säße sonst gleich im nächsten.
        EingabemethodeZuruecksetzen();

        // Was daran hängt, zieht der Umbruch danach nach — er läuft ohnehin gleich.
        MarkierungNeu();
    }

    // ==================== Der Zeiger ====================

    /// <summary>
    /// Ein Klick setzt die Schreibmarke; mit Umschalt erweitert er die Auswahl, ein Doppelklick
    /// nimmt das Wort, ein Dreifachklick den Absatz.
    /// </summary>
    private void Zeiger_Gedrueckt(object? sender, PointerPressedEventArgs e)
    {
        if (!Schreibbar) return;

        // **Erst den Fokus holen.** Ohne ihn wird gezeichnet wie gewohnt (der Zeiger braucht
        // keinen), aber keine Taste kommt an — das unauffälligste Fehlerbild aus §7.
        Skia.Focus();

        if (StelleUnter(e.GetPosition(Skia)) is not { } stelle) return;

        var punkt = e.GetCurrentPoint(Skia);

        // **Die rechte Taste setzt die Marke, wenn außerhalb der Auswahl geklickt wird — und
        // sonst nicht.** Das ist die Erwartung aus jedem Textprogramm: Ein Rechtsklick *in* eine
        // Auswahl meint sie, ein Rechtsklick daneben meint die Stelle darunter. Ohne den ersten
        // Teil zeigte das Menü Tabellenbefehle für die Zelle, in der man zuletzt war; ohne den
        // zweiten verlöre ein Rechtsklick jede Auswahl, die man gerade treffen wollte.
        if (punkt.Properties.IsRightButtonPressed)
        {
            if (!InAuswahl(stelle)) { _auswahl = new TdSelection(stelle); MarkeVersetzt(); }
            _zieht = false;

            TabellenmenueZeigen();
            e.Handled = true;
            return;
        }

        if (!punkt.Properties.IsLeftButtonPressed && punkt.Pointer.Type == PointerType.Mouse) return;

        _auswahl = e.ClickCount switch
        {
            >= 3 => AbsatzAuswahl(stelle),
            2 => TdCursor.Wort(_modell!, stelle),
            _ when e.KeyModifiers.HasFlag(KeyModifiers.Shift) => _auswahl.Bis(stelle),
            _ => new TdSelection(stelle),
        };

        // Gezogen wird nur beim einfachen Klick. Wer ein Wort doppelt anklickt und die Taste
        // hält, würde sonst die Wortauswahl beim ersten Wackeln wieder verlieren.
        _zieht = e.ClickCount == 1;

        MarkeVersetzt();

        // **Finger und Stift holen die Bildschirmtastatur, die Maus nicht** (Schritt 6a). Wer
        // ein Gerät in der Hand hält und hineintippt, hat keine andere; wer eine Maus benutzt,
        // hat eine danebenliegen — und bekäme sonst bei jedem Klick ein halbes Fenster über das
        // Blatt geschoben. **Nach `MarkeVersetzt` und nicht davor:** Die Plattform fragt beim
        // Aufklappen sofort, wo die Marke steht, und das soll dann schon die neue Stelle sein.
        if (punkt.Pointer.Type is PointerType.Touch or PointerType.Pen) TastaturAnfordern();

        e.Handled = true;
    }

    private void Zeiger_Bewegt(object? sender, PointerEventArgs e)
    {
        if (!_zieht || !Schreibbar) return;
        if (StelleUnter(e.GetPosition(Skia)) is not { } stelle) return;
        if (stelle == _auswahl.Focus) return;

        _auswahl = _auswahl.Bis(stelle);
        MarkeVersetzt();
    }

    private void Zeiger_Losgelassen(object? sender, PointerReleasedEventArgs e) => _zieht = false;

    /// <summary>
    /// Liegt diese Stelle in der aktuellen Auswahl? <b>Verglichen wird zwischen normalisierten
    /// Stellen</b> (§4.30) — zwei Schreibweisen derselben Lücke kämen sonst als verschieden
    /// heraus, und ein Rechtsklick auf den Rand der Auswahl verlöre sie.
    /// </summary>
    private bool InAuswahl(TdPosition stelle)
    {
        if (_auswahl.IsEmpty || _modell is null) return false;

        var gezogen = TdCursor.Normalisieren(_modell, _auswahl);
        var hier = TdCursor.Normalisieren(_modell, stelle);

        return hier >= gezogen.Start && hier <= gezogen.End;
    }

    /// <summary>Der ganze Absatz, für den Dreifachklick.</summary>
    private TdSelection AbsatzAuswahl(TdPosition stelle) => new(
        TdCursor.AbsatzAnfang(_modell!, stelle.Paragraph),
        TdCursor.AbsatzEnde(_modell!, stelle.Paragraph));

    /// <summary>
    /// Welche Stelle im Modell unter diesem Punkt der Leinwand liegt — <c>null</c>, wenn dort
    /// kein Blatt ist.
    ///
    /// <para>
    /// <b>Die Umrechnung ist die Umkehrung von <see cref="OnPaint"/>, Zeile für Zeile.</b> Sie
    /// steht deshalb hier und nicht in Core: Wo ein Blatt auf der Leinwand liegt, ist eine
    /// Frage des Stapels — und der gehört dem Kopf. Was auf dem Blatt steht, rechnet
    /// <see cref="TdHit"/>.
    /// </para>
    /// </summary>
    private TdPosition? StelleUnter(Point p)
    {
        if (_umbruch is null || _modell is null || _seitenObenCm.Length == 0) return null;

        double massstab = TdRenderer.PixelProCm * _zoom;
        double yCm = p.Y / massstab;

        // Die nächstgelegene Seite und nicht nur die getroffene: Zwischen zwei Blättern liegt
        // ein Zwischenraum, und ein Klick hinein ist kein Grund, die Marke stehen zu lassen.
        int seite = 0;
        double bester = double.MaxValue;

        for (int i = 0; i < _umbruch.Pages.Count; i++)
        {
            double oben = _seitenObenCm[i];
            double unten = oben + _umbruch.Pages[i].Setup.HeightCm;
            double abstand = yCm < oben ? oben - yCm : yCm > unten ? yCm - unten : 0;

            if (abstand >= bester) continue;
            bester = abstand;
            seite = i;
        }

        var setup = _umbruch.Pages[seite].Setup;

        // Blätter liegen mittig — derselbe Ausdruck wie beim Zeichnen.
        double linksCm = (_stapelBreiteCm - setup.WidthCm) / 2;

        return TdHit.StelleAn(
            _umbruch, _modell, Messung, seite,
            p.X / massstab - linksCm,
            yCm - _seitenObenCm[seite]);
    }

    // ==================== Die Tastatur ====================

    /// <summary>
    /// Alles, was wirklich eine Taste ist. <b>Getippte Zeichen stehen nicht darunter</b> — die
    /// kommen über <see cref="Texteingabe"/>.
    /// </summary>
    private void Taste(object? sender, KeyEventArgs e)
    {
        if (!Schreibbar) return;

        bool umschalt = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool strg = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        e.Handled = true;

        switch (e.Key)
        {
            // ---------- Bewegen ----------
            case Key.Left:
                Bewegen(strg ? TdCursor.WortLinks(_modell!, _auswahl.Focus)
                             : TdCursor.Links(_modell!, _auswahl.Focus), umschalt);
                break;

            case Key.Right:
                Bewegen(strg ? TdCursor.WortRechts(_modell!, _auswahl.Focus)
                             : TdCursor.Rechts(_modell!, _auswahl.Focus), umschalt);
                break;

            case Key.Up:
            case Key.Down:
                Zeilenweise(e.Key == Key.Up, umschalt);
                break;

            case Key.Home:
                Bewegen(strg
                    ? TdCursor.Anfang(_modell!)
                    : TdHit.Zeilenrand(_umbruch!, _modell!, _auswahl.Focus, ende: false), umschalt);
                break;

            case Key.End:
                Bewegen(strg
                    ? TdCursor.Ende(_modell!)
                    : TdHit.Zeilenrand(_umbruch!, _modell!, _auswahl.Focus, ende: true), umschalt);
                break;

            case Key.PageUp:
            case Key.PageDown:
                Blaettern(e.Key == Key.PageUp, umschalt);
                break;

            // ---------- Ändern ----------
            case Key.Back:
                Aendern(TdEdit.Rueckwaerts(_modell!, _auswahl));
                break;

            case Key.Delete:
                Aendern(TdEdit.Vorwaerts(_modell!, _auswahl));
                break;

            case Key.Enter:
                Aendern(umschalt
                    ? TdEdit.Zeilenumbruch(_modell!, _auswahl)
                    : TdEdit.AbsatzTeilen(_modell!, _auswahl));
                break;

            // Ein Tabulator ist im Modell kein eigenes Stück (§4.20 kennt keinen Tabstopp) —
            // er wird zum Zwischenraum, statt still zu verschwinden oder den Fokus zu wechseln.
            case Key.Tab when !strg:
                Aendern(TdEdit.Tippen(_modell!, _auswahl, "    "));
                break;

            // ---------- Tastenkürzel ----------
            case Key.A when strg:
                _auswahl = TdSelection.Alles(_modell!);
                MarkeVersetzt();
                break;

            case Key.C when strg:
                Kopieren();
                break;

            case Key.X when strg:
                Kopieren();
                Aendern(TdEdit.Loeschen(_modell!, _auswahl));
                break;

            case Key.V when strg:
                Einfuegen();
                break;

            case Key.Z when strg && !umschalt:
                Zuruecknehmen(vor: false);
                break;

            case Key.Y when strg:
            case Key.Z when strg && umschalt:
                Zuruecknehmen(vor: true);
                break;

            // ---------- Format (Schritt 6) ----------
            // **Sie stehen hier und nicht nur am Knopf**: Die Kurzbefehle stehen in den
            // Kurzhinweisen des Ribbons („Fett (Strg+B)", `Ed.Bold`) — ein Hinweis, der etwas
            // verspricht, das es nicht gibt, ist schlimmer als keiner.
            case Key.B when strg:
                Fett();
                break;

            case Key.I when strg:
                Kursiv();
                break;

            case Key.U when strg:
                Unterstrichen();
                break;

            default:
                e.Handled = false;
                break;
        }
    }

    /// <summary>
    /// Ein getipptes Zeichen. <b>Steuerzeichen werden abgewiesen</b>: Manche Tastaturen liefern
    /// für Eingabe und Rücktaste zusätzlich ein <c>TextInput</c> mit „\r" oder „\b" — beides
    /// hat oben schon eine Antwort bekommen, und hier stünde es danach als Zeichen im Text.
    /// </summary>
    private void Texteingabe(object? sender, TextInputEventArgs e)
    {
        if (!Schreibbar || string.IsNullOrEmpty(e.Text)) return;

        string text = new([.. e.Text.Where(c => !char.IsControl(c))]);
        if (text.Length == 0) return;

        // **Der fertige Text verbraucht den unfertigen** (§4.43). IBus schickt zwar meist noch
        // eine leere Vorschau hinterher, aber nicht verlässlich und nicht vor dem `commit` —
        // ohne diese Zeile stünde die Silbe für einen Augenblick doppelt da: einmal
        // festgeschrieben im Absatz und einmal als Auflage darüber.
        VorschauVerwerfen();

        Aendern(TdEdit.Tippen(_modell!, _auswahl, text));
        e.Handled = true;
    }

    // ==================== Bewegen ====================

    /// <summary>
    /// Die Marke an eine neue Stelle — mit Umschalt zieht sie die Auswahl hinter sich her, ohne
    /// lässt sie sie fallen.
    /// </summary>
    private void Bewegen(TdPosition ziel, bool auswaehlen)
    {
        _auswahl = auswaehlen ? _auswahl.Bis(ziel) : new TdSelection(ziel);
        _spalte = null;      // eine waagerechte Bewegung gibt die angepeilte Spalte auf
        MarkeVersetzt();
    }

    /// <summary>
    /// Eine Zeile höher oder tiefer. <b>Die einzige Bewegung, die den Umbruch braucht</b> — im
    /// Modell gibt es keine Zeilen (§4.30). Gibt es keine Zeile mehr, bleibt die Marke stehen.
    /// </summary>
    private void Zeilenweise(bool hoch, bool auswaehlen)
    {
        var zug = hoch
            ? TdHit.Hoch(_umbruch!, _modell!, Messung, _auswahl.Focus, _spalte)
            : TdHit.Runter(_umbruch!, _modell!, Messung, _auswahl.Focus, _spalte);

        if (zug is not { } z) return;

        _auswahl = auswaehlen ? _auswahl.Bis(z.Stelle) : new TdSelection(z.Stelle);

        // **Erst nach dem Bewegen setzen**: Bewegen() räumt die Spalte weg, dieser Weg behält
        // sie — das ist der ganze Unterschied zwischen „hoch" und „links".
        _spalte = z.SpalteCm;
        MarkeVersetzt();
    }

    /// <summary>
    /// Bild auf und ab: so viele Zeilensprünge, wie ins Sichtfenster passen. <b>Gezählt in
    /// Zeilen und nicht in Zentimetern</b>, denn eine halbe Zeile ist keine Stelle — und wer
    /// zweimal blättert, will zweimal dieselbe Strecke.
    /// </summary>
    private void Blaettern(bool hoch, bool auswaehlen)
    {
        double sichtCm = Blaetter.Viewport.Height / (TdRenderer.PixelProCm * _zoom);

        var stelle = _auswahl.Focus;
        double? spalte = _spalte;
        double gegangen = 0;

        while (gegangen < sichtCm)
        {
            var zug = hoch
                ? TdHit.Hoch(_umbruch!, _modell!, Messung, stelle, spalte)
                : TdHit.Runter(_umbruch!, _modell!, Messung, stelle, spalte);

            if (zug is not { } z || z.Stelle == stelle) break;

            var vorher = TdHit.Schreibmarke(_umbruch!, _modell!, Messung, stelle);
            stelle = z.Stelle;
            spalte = z.SpalteCm;

            var nachher = TdHit.Schreibmarke(_umbruch!, _modell!, Messung, stelle);
            gegangen += vorher is { } a && nachher is { } b && a.Seite == b.Seite
                ? Math.Abs(b.YCm - a.YCm)
                : sichtCm;      // ein Seitenwechsel beendet den Sprung
        }

        _auswahl = auswaehlen ? _auswahl.Bis(stelle) : new TdSelection(stelle);
        _spalte = spalte;
        MarkeVersetzt();
    }

    // ==================== Ändern ====================

    /// <summary>
    /// Führt eine Änderung aus, merkt sie im Verlauf und bricht neu um. <b><c>null</c> heißt
    /// „nichts zu tun"</b> und wird still hingenommen — Rücktaste am Dokumentanfang, eine
    /// Auswahl über eine Tabellengrenze (§4.32).
    /// </summary>
    private void Aendern(TdChange? aenderung)
    {
        if (aenderung is null || _vm is null) return;

        _auswahl = aenderung.Anwenden();
        _vm.Undo.Push(aenderung);
        _vm.IsDirty = true;

        // Getippt wird in der Spalte, in der man steht — eine angepeilte Spalte von vorher
        // gilt danach nicht mehr.
        _spalte = null;

        UmbruchAnstossen();
    }

    private void Zuruecknehmen(bool vor)
    {
        if (_vm is null) return;

        var auswahl = vor ? _vm.Undo.Redo() : _vm.Undo.Undo();
        if (auswahl is not { } neu) return;

        _auswahl = neu;
        _vm.IsDirty = true;
        _spalte = null;

        UmbruchAnstossen();
    }

    // ==================== Zwischenablage ====================

    /// <summary>
    /// Kopiert den ausgewählten **Klartext**. Formate, Felder und Bilder gehen dabei nicht mit:
    /// Die Zwischenablage trägt heute nur Text (<c>IClipboard</c>), und ein eigenes Format
    /// dafür wäre eine Runde für sich. <b>Was fehlt, verschwindet nicht still</b> — es steht
    /// hier und in §6.
    /// </summary>
    private void Kopieren()
    {
        if (_auswahl.IsEmpty) return;
        App.Platform.Clipboard.SetText(TdCursor.Text(_modell!, _auswahl));
    }

    /// <summary>
    /// Fügt Klartext ein — <b>über denselben Handgriff wie das Tippen</b>.
    ///
    /// <para>
    /// Dass Zeilenumbrüche dabei zu Absätzen werden, steht in <see cref="TdFragment.Text"/> und
    /// nicht hier. Es hier ein zweites Mal zu entscheiden, wäre die Falle aus §4.13: zwei
    /// Fassungen derselben Regel, von denen die eine später jemand ändert.
    /// </para>
    /// </summary>
    private void Einfuegen()
    {
        if (App.Platform.Clipboard.GetText() is { Length: > 0 } text)
            Aendern(TdEdit.Tippen(_modell!, _auswahl, text));
    }

    // ==================== Umbrechen ====================

    /// <summary>
    /// Bricht nach einer Änderung neu um — <b>sofort, solange das billig ist, und sonst
    /// gesammelt</b>.
    ///
    /// <para>
    /// <b>Das ist die Antwort auf die erste der drei Fallen aus §6, und sie ist gemessen und
    /// nicht geraten.</b> Auf diesem Rechner (Release) kostet ein voller Umbruch: 2 Seiten
    /// 4,5 ms · 9 Seiten 25 ms · 35 Seiten 208 ms. <b>Der Fund dabei: das mehrfache Rechnen
    /// eines Inhaltsverzeichnisses ist *nicht* der teure Teil</b> — dieselben 9 Seiten kosten
    /// mit Verzeichnis 27,6 statt 25,1 ms. Was zählt, ist die **Länge** des Dokuments; der Preis
    /// steckt im Messen der Schrift.
    /// </para>
    /// <para>
    /// Daraus folgt diese Weiche: Bleibt der letzte Umbruch unter <see cref="SofortGrenzeMs"/>,
    /// wird sofort gerechnet — die Marke steht dann in derselben Sekunde richtig, in der das
    /// Zeichen erscheint. Darüber wird über den Nachrichtenlauf **gesammelt**: Eingaben haben
    /// Vorrang vor <see cref="DispatcherPriority.Background"/>, also laufen zehn schnell
    /// getippte Zeichen durch das Modell und danach **ein** Umbruch. <b>Das Modell hinkt dabei
    /// nie hinterher, nur das Bild</b> — und es holt in der ersten Tippause auf.
    /// </para>
    /// <para>
    /// <b>Was damit ausdrücklich noch nicht getan ist:</b> nur den betroffenen Absatz neu zu
    /// setzen. Das ist der andere Weg aus §6, und er ist keine Kleinigkeit — eine Änderung in
    /// Absatz 3 verschiebt jeden Seitenumbruch danach und jede Seitenzahl im Verzeichnis. Er
    /// lohnt erst, wenn ein Dokument dieser Länge wirklich vorkommt.
    /// </para>
    /// </summary>
    private const double SofortGrenzeMs = 40;

    /// <inheritdoc cref="SofortGrenzeMs"/>
    private void UmbruchAnstossen()
    {
        if (_umbruchMs <= SofortGrenzeMs)
        {
            NeuUmbrechen();
            return;
        }

        if (_umbruchFaellig) return;
        _umbruchFaellig = true;

        Dispatcher.UIThread.Post(
            () =>
            {
                _umbruchFaellig = false;
                NeuUmbrechen();
            },
            DispatcherPriority.Background);
    }

    // ==================== Die Marke ====================

    /// <summary>
    /// Die Marke ist versetzt worden — ohne dass sich der Text geändert hätte.
    ///
    /// <para>
    /// <b>Hier steht der Schnitt im Verlauf</b> (§4.33): Wer klickt oder mit dem Pfeil
    /// wegspringt, fängt einen neuen Handgriff an. Ohne ihn hinge ein Buchstabe, den jemand
    /// zehn Zeilen weiter oben nachträgt, am selben Schritt wie der Satz davor — und ein
    /// Strg+Z nähme beides zurück. <b>Der Verlauf kann das nicht selbst merken: er sieht
    /// Änderungen und keine Klicks.</b>
    /// </para>
    /// </summary>
    private void MarkeVersetzt()
    {
        _vm?.Undo.Abschliessen();
        MarkeNachziehen();
    }

    /// <summary>
    /// Alles, was der Marke folgt: neu rechnen, ins Bild rollen, den Takt von vorn anfangen.
    /// <b>Dieselben drei Handgriffe nach einem Klick wie nach einem Umbruch</b> — getrennt
    /// geschrieben wären es zwei Listen, von denen eine irgendwann einen Punkt weniger hätte.
    /// </summary>
    private void MarkeNachziehen()
    {
        MarkierungNeu();
        InsBildRollen();
        BlinkenAnstossen();

        // Seit Schritt 6 hängt daran ein vierter: Die Formatknöpfe zeigen, was **an der
        // Auswahl** gilt — sie bewegen sich also mit ihr und nicht mit dem Text.
        RibbonNachziehen();

        // Und seit Schritt 6a ein fünfter: Die Eingabemethode führt die Bildschirmtastatur an
        // der Marke nach und liest den Text um sie herum. **Hier und nicht in `Aendern`** —
        // dieselbe Begründung wie oben: nach einem Klick ist die Auskunft genauso falsch
        // geworden wie nach einem Tastendruck.
        EingabemethodeNachziehen();
    }

    /// <summary>Rechnet Auswahl und Marke für den Zeichner neu.</summary>
    private void MarkierungNeu()
    {
        if (!Schreibbar)
        {
            _markierung = null;
            _markeZeile = null;
            _kontext = _kontext with { Markierung = null };
            return;
        }

        _markierung = TdHit.Markieren(_umbruch!, _modell!, Messung, _auswahl);
        _markeZeile = _markierung.MarkeZeile;
        _kontext = _kontext with { Markierung = _markierung };

        Skia.InvalidateVisual();
    }

    /// <summary>
    /// Der Takt der Marke. <b>Er gehört dem Kopf</b> — in Core wäre er eine Uhr (§4.20), und
    /// ein Export bekäme je nach Augenblick einen Strich mit (§4.34).
    ///
    /// <para>
    /// Nach jedem Versetzen fängt er von vorn und **sichtbar** an: Eine Marke, die im falschen
    /// Halbtakt landet, ist nach einem Klick für einen halben Schlag unsichtbar — und genau
    /// dann sucht man sie.
    /// </para>
    /// </summary>
    private void BlinkenAnstossen()
    {
        _markeAn = true;

        if (_blinker is null)
        {
            // 530 ms ist der Takt, den Windows seit jeher vorgibt; Avalonia hat keine Auskunft
            // dazu, und ein eigener Wert wäre nur eine zweite Meinung.
            _blinker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
            _blinker.Tick += (_, _) =>
            {
                _markeAn = !_markeAn;
                Skia.InvalidateVisual();
            };
        }

        _blinker.Stop();
        if (Schreibbar) _blinker.Start();

        Skia.InvalidateVisual();
    }

    /// <summary>
    /// Hängt die Marke für den nächsten Zeichenschritt ein oder aus. <b>Läuft unmittelbar vor
    /// dem Aufzeichnen</b> und ändert nur ein Feld — die Auswahl bleibt in jedem Halbtakt
    /// stehen, denn sie blinkt nicht.
    /// </summary>
    private void MarkeTakten()
    {
        if (_markierung is null) return;

        // **Solange etwas zusammengesetzt wird, gehört die Marke dem unfertigen Text** (§4.43).
        // Die des Dokuments stünde genau an dessen Anfang — zwei Striche nebeneinander, von
        // denen einer blinkt und der andere nicht, und keiner sagt, wo das nächste Zeichen
        // hinkommt.
        _markierung.MarkeZeile = Setzt ? null : _markeAn ? _markeZeile : null;
    }

    /// <summary>
    /// Rollt die Schreibmarke ins Bild, wenn sie herausgelaufen ist — <b>und nur dann</b>. Wer
    /// bei jedem Tastendruck zentrierte, ließe das Blatt unter dem Text wandern.
    /// </summary>
    private void InsBildRollen()
    {
        if (!Schreibbar) return;
        if (TdHit.Schreibmarke(_umbruch!, _modell!, Messung, _auswahl.Focus) is not { } marke) return;
        if (marke.Seite < 0 || marke.Seite >= _seitenObenCm.Length) return;

        double massstab = TdRenderer.PixelProCm * _zoom;
        double oben = (_seitenObenCm[marke.Seite] + marke.YCm) * massstab;
        double unten = oben + marke.HoeheCm * massstab;

        double sicht = Blaetter.Viewport.Height;
        double jetzt = Blaetter.Offset.Y;
        if (sicht <= 0) return;

        // Ein Rand von einer Zeilenhöhe: Eine Marke, die genau auf der Kante klebt, sieht aus,
        // als wäre sie halb abgeschnitten.
        double rand = marke.HoeheCm * massstab;

        double neu = oben - rand < jetzt ? oben - rand
            : unten + rand > jetzt + sicht ? unten + rand - sicht
            : jetzt;

        if (Math.Abs(neu - jetzt) > 0.5)
            Blaetter.Offset = Blaetter.Offset.WithY(Math.Max(0, neu));
    }
}
