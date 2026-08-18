using Avalonia;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using GonkNote.Core.Rendering;
using GonkNote.Core.Text;

namespace GonkNote.Views;

/// <summary>
/// Die Eingabe-Naht — <b>Schritt 6a des Schreibens</b> (HANDOFF §6, §5 „Noch offen" 10,
/// entschieden vom Nutzer am 2026-08-16).
///
/// <para>
/// <b>Der Anlass ist gemessen und nicht vermutet</b> (§4.35, „Die Bildschirmtastatur: nicht nur
/// unsichtbar, sondern taub"): Auf dem Laptop erscheint GNOMEs Bildschirmtastatur nicht, wenn
/// die Textfläche den Fokus hat — und von Hand hervorgeholt kommt aus ihr trotzdem nichts an.
/// Die Ursache stand im Quelltext: <see cref="TextDocView"/> ist ein selbstgebautes
/// Steuerelement, das auf <c>TextInputEventArgs</c> hört und sich <b>nirgends als Eingabeziel
/// anmeldet</b>. Die Tastatur hatte nichts, woran sie andocken konnte.
/// </para>
///
/// <para>
/// <b>Was ein Eingabeziel beantworten muss</b>, sind vier Fragen, und Avalonia stellt sie über
/// <see cref="TextInputMethodClient"/>: <i>welche Fläche zeigt den Text</i>
/// (<see cref="Eingabeziel.TextViewVisual"/>), <i>wo steht die Marke auf ihr</i>
/// (<see cref="Eingabeziel.CursorRectangle"/>), <i>was steht um sie herum</i>
/// (<see cref="Eingabeziel.SurroundingText"/>) und <i>was ist davon ausgewählt</i>
/// (<see cref="Eingabeziel.Selection"/>). <b>Gerechnet wird keine davon hier</b> — die dritte
/// und die vierte beantwortet <see cref="TdEingabe"/> in Core, die zweite
/// <see cref="TdHit.Schreibmarke"/>. Was hier steht, ist die Übersetzung in Avalonias Sprache
/// und die Umrechnung von Seitenkoordinaten auf die Leinwand.
/// </para>
///
/// <para>
/// <b>Es ist nicht linuxspezifisch</b> — dem WPF-Kopf fehlt dasselbe; dort fällt es nur nicht
/// auf, weil eine Tastatur danebensteht. Gebaut ist es trotzdem nur hier: Der WPF-Editor ist
/// bis Schritt 7 der alte <c>RichTextBox</c>, und der bringt WPFs eigene Eingabe-Naht mit.
/// </para>
/// <para>
/// <b>Ob die Bildschirmtastatur danach wirklich aufgeht, kann nur der Laptop sagen</b> (§5b:
/// „sieht oder fühlt es sich auf Linux richtig an?" → Laptop). Was von hier aus zu prüfen war,
/// ist die Rechnung dahinter, und die halten die Wächter in <c>EingabeUmfeldTests</c>.
/// </para>
/// </summary>
public partial class TextDocView
{
    /// <summary>
    /// Das Eingabeziel dieser Ansicht. <b>Eines je Ansicht und nicht eines je Anfrage:</b>
    /// Avalonia fragt bei jedem Fokuswechsel neu, und ein frisches Ziel je Frage hätte bei
    /// jedem Wechsel die Ereignisstränge der Eingabemethode abgerissen.
    /// </summary>
    private Eingabeziel? _eingabeziel;

    /// <inheritdoc cref="_eingabeziel"/>
    private Eingabeziel Ziel => _eingabeziel ??= new Eingabeziel(this);

    // ==================== Anschluss ====================

    /// <summary>
    /// Meldet die Zeichenfläche als Eingabeziel an — gerufen aus
    /// <see cref="EingabeAnhaengen"/>, also aus demselben einen Trichter wie Tastatur und
    /// Zeiger.
    ///
    /// <para>
    /// <b>An <see cref="SkiaCanvas"/> und nicht an das <c>UserControl</c></b>, aus demselben
    /// Grund wie dort: Avalonia fragt das <b>fokussierte</b> Element nach seinem Eingabeziel,
    /// und das ist die Fläche.
    /// </para>
    /// <para>
    /// <b><c>Multiline</c> steht ausdrücklich da.</b> Eine Bildschirmtastatur zeigt sonst statt
    /// der Eingabetaste ein „Fertig", das den Fokus wegnimmt — in einem Textdokument ist die
    /// Eingabetaste ein Absatz und kein Abschluss.
    /// </para>
    /// </summary>
    private void EingabemethodeAnhaengen()
    {
        InputMethod.SetIsInputMethodEnabled(Skia, true);
        TextInputOptions.SetMultiline(Skia, true);
        TextInputOptions.SetContentType(Skia, TextInputContentType.Normal);

        Skia.TextInputMethodClientRequested += (_, e) => e.Client = Ziel;
    }

    /// <summary>
    /// Sagt der Eingabemethode, dass sich Marke, Auswahl oder Text geändert haben — gerufen aus
    /// <see cref="MarkeNachziehen"/>, also nach jedem Klick, jedem Pfeil und jedem Umbruch.
    ///
    /// <para>
    /// <b>Ein Aufruf für alle drei Meldungen</b>, obwohl Avalonia drei Ereignisse dafür hat:
    /// Getrennt gemeldet wären es drei Listen von Stellen, an denen daran zu denken ist, und
    /// eine davon hätte irgendwann einen Punkt weniger. Der Preis ist eine Meldung zu viel,
    /// wenn nur die Marke gewandert ist — die Eingabemethode liest daraufhin eine Zeichenkette,
    /// die sie schon hatte, und das kostet nichts.
    /// </para>
    /// </summary>
    private void EingabemethodeNachziehen() => _eingabeziel?.Nachziehen();

    /// <summary>
    /// Verwirft ein angefangenes Zusammensetzen — beim Wechsel des Dokuments.
    /// <b>Ohne das trüge eine halb getippte Silbe in das nächste Dokument hinüber</b>, und die
    /// Eingabemethode setzte sie dort ab, wo sie nie angefangen wurde.
    /// </summary>
    private void EingabemethodeZuruecksetzen() => _eingabeziel?.Zuruecksetzen();

    /// <summary>
    /// Bittet die Plattform, die Bildschirmtastatur aufzuklappen. <b>Nur für Finger und
    /// Stift</b> — siehe <see cref="Zeiger_Gedrueckt"/>.
    /// </summary>
    private void TastaturAnfordern()
    {
        if (Schreibbar) Ziel.TastaturAnfordern();
    }

    // ==================== Die Marke auf der Leinwand ====================

    /// <summary>
    /// Wo die Schreibmarke auf der Zeichenfläche steht, in Avalonia-Punkten —
    /// <c>default</c>, wenn sie nirgends steht.
    ///
    /// <para>
    /// <b>Dieselbe Umrechnung wie in <see cref="OnPaint"/>, nur für einen einzigen Punkt:</b>
    /// <see cref="TdHit.Schreibmarke"/> antwortet in <i>Seiten</i>koordinaten (auf welchem
    /// Blatt und wo darauf), die Eingabemethode fragt nach <i>Flächen</i>koordinaten. Dazwischen
    /// liegen der Stapel (<see cref="_seitenObenCm"/>), die Mitte (Blätter liegen mittig) und
    /// der Maßstab. <b>Es ist genau die Rechnung, die <see cref="StelleUnter"/> rückwärts
    /// macht</b> — und sie steht hier zum zweiten Mal, weil die eine aus einem Punkt eine
    /// Stelle macht und die andere aus einer Stelle einen Punkt; zusammengelegt wären es zwei
    /// Richtungen in einer Funktion.
    /// </para>
    /// </summary>
    private Rect MarkeAufLeinwand()
    {
        if (!Schreibbar) return default;
        if (TdHit.Schreibmarke(_umbruch!, _modell!, Messung, _auswahl.Focus) is not { } marke)
            return default;
        if (marke.Seite < 0 || marke.Seite >= _seitenObenCm.Length) return default;

        double massstab = TdRenderer.PixelProCm * _zoom;
        var setup = _umbruch!.Pages[marke.Seite].Setup;
        double linksCm = (_stapelBreiteCm - setup.WidthCm) / 2;

        return new Rect(
            (linksCm + marke.XCm) * massstab,
            (_seitenObenCm[marke.Seite] + marke.YCm) * massstab,
            // Eine Marke ist ein Strich und hat keine Breite. Null einzutragen ließe manche
            // Plattform die Tastatur an der linken Fensterkante ausrichten — ein Punkt ist die
            // ehrlichste Zahl, die noch eine Fläche ist.
            1,
            marke.HoeheCm * massstab);
    }

    // ==================== Das Eingabeziel ====================

    /// <summary>
    /// Die Antwort auf Avalonias vier Fragen. <b>Sie hält keinen eigenen Zustand</b> — jede
    /// Frage wird im Augenblick des Fragens aus Modell und Auswahl beantwortet. Ein gemerkter
    /// Text wäre eine zweite Wahrheit neben dem Dokument, und sie ginge genau dann auseinander,
    /// wenn getippt wird.
    /// </summary>
    private sealed class Eingabeziel(TextDocView ansicht) : TextInputMethodClient
    {
        /// <summary>
        /// Sperrt das Zurückmelden, während die Eingabemethode selbst die Auswahl setzt.
        /// <b>Ohne sie bekäme sie ihre eigene Änderung als Nachricht zurück</b> — dieselbe
        /// Vorsorge wie <c>_fuellt</c> in der Einstellungsleiste (§4.38).
        /// </summary>
        private bool _setztSelbst;

        /// <summary>Die Fläche, auf der der Text steht.</summary>
        public override Visual TextViewVisual => ansicht.Skia;

        /// <summary>
        /// <b>Nein — und das ist eine benannte Auslassung, keine Lücke.</b> „Preedit" ist der
        /// unfertige Text, den eine ostasiatische Eingabemethode zeigt, bevor er im Dokument
        /// steht. Ihn hier anzuzeigen hieße, ihn ins Modell zu schreiben und wieder
        /// herauszunehmen (der Griff, vor dem §4.32 warnt) oder ihn über den Text daneben zu
        /// malen. <c>false</c> zu melden ist kein Ausfall: Die Plattform zeigt ihn dann in
        /// ihrem eigenen Fenster, und der Text kommt fertig zusammengesetzt als
        /// <c>TextInput</c> an — genau der Weg, den Umlaute und tote Tasten heute schon nehmen
        /// (§4.35, V2-47). Die Begründung im Langen steht bei <see cref="TdEingabe"/>.
        /// </summary>
        public override bool SupportsPreedit => false;

        /// <summary>
        /// <b>Ja.</b> Ohne diese Auskunft weiß eine Bildschirmtastatur nicht, welches Wort
        /// gerade geschrieben wird — und kann weder vervollständigen noch groß schreiben, was
        /// am Satzanfang steht.
        /// </summary>
        public override bool SupportsSurroundingText => true;

        /// <inheritdoc cref="TdEingabe.Umfeld"/>
        public override string SurroundingText => Umfeld().Text;

        /// <inheritdoc cref="MarkeAufLeinwand"/>
        public override Rect CursorRectangle => ansicht.MarkeAufLeinwand();

        /// <summary>
        /// Die Auswahl innerhalb von <see cref="SurroundingText"/>.
        ///
        /// <para>
        /// <b>Der Setzer geht denselben Weg wie ein Klick</b> (<see cref="MarkeVersetzt"/>) und
        /// nicht an ihm vorbei: Er schließt den Verlaufsschritt ab (§4.33), zieht Markierung
        /// und Ribbon nach und rollt die Marke ins Bild. Eine Auswahl, die still gesetzt wird,
        /// wäre eine Stelle, an der der Verlauf einen Sprung nicht mitbekommt — und ein Strg+Z
        /// nähme danach zwei Handgriffe auf einmal zurück.
        /// </para>
        /// </summary>
        public override TextSelection Selection
        {
            get
            {
                var umfeld = Umfeld();
                return new TextSelection(umfeld.Start, umfeld.Ende);
            }
            set
            {
                if (ansicht._modell is null) return;

                _setztSelbst = true;
                try
                {
                    ansicht._auswahl = TdEingabe.Auswahl(
                        ansicht._modell, Umfeld().Absatz, value.Start, value.End);

                    ansicht.MarkeVersetzt();
                }
                finally
                {
                    _setztSelbst = false;
                }
            }
        }

        /// <summary>
        /// Was die Eingabemethode an Befehlen schicken darf. <b>Sie laufen über dieselben
        /// Handgriffe wie die Tastenkürzel</b> — ein zweiter Weg zum Ausschneiden wäre eine
        /// zweite Gelegenheit, den Verlaufsschritt zu vergessen.
        /// </summary>
        public override void ExecuteContextMenuAction(ContextMenuAction action)
        {
            if (!ansicht.Schreibbar) return;

            switch (action)
            {
                case ContextMenuAction.Copy:
                    ansicht.Kopieren();
                    break;

                case ContextMenuAction.Cut:
                    ansicht.Kopieren();
                    ansicht.Aendern(TdEdit.Loeschen(ansicht._modell!, ansicht._auswahl));
                    break;

                case ContextMenuAction.Paste:
                    ansicht.Einfuegen();
                    break;

                case ContextMenuAction.SelectAll:
                    ansicht._auswahl = TdSelection.Alles(ansicht._modell!);
                    ansicht.MarkeVersetzt();
                    break;
            }
        }

        // ---------------------------------------------------------------- vom Kopf gerufen

        /// <inheritdoc cref="EingabemethodeNachziehen"/>
        public void Nachziehen()
        {
            if (_setztSelbst) return;

            RaiseCursorRectangleChanged();
            RaiseSurroundingTextChanged();
            RaiseSelectionChanged();
        }

        /// <inheritdoc cref="EingabemethodeZuruecksetzen"/>
        public void Zuruecksetzen() => RequestReset();

        /// <inheritdoc cref="TextDocView.TastaturAnfordern"/>
        public void TastaturAnfordern() => RaiseInputPaneActivationRequested();

        /// <summary>
        /// Das Umfeld der Marke — <b>leer, solange nichts angezeigt wird</b>. Eine Auskunft
        /// über ein Dokument, das nicht da ist, wäre eine erfundene.
        /// </summary>
        private TdUmfeld Umfeld() => ansicht._modell is { } modell
            ? TdEingabe.Umfeld(modell, ansicht._auswahl)
            // **Nicht `default`:** Das gäbe `Text = null`, und `SurroundingText` ist als
            // Zeichenkette angekündigt — die Eingabemethode fragt auch, bevor ein Dokument
            // geladen ist.
            : new TdUmfeld(0, "", 0, 0);
    }
}
