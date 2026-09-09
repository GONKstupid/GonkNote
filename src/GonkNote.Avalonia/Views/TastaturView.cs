using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using GonkNote.Core.Editing;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Die eingebaute Bildschirmtastatur — <b>die Fläche</b>; die Belegung steht in
/// <see cref="Bildschirmtastatur"/> (Core).
///
/// <para>
/// <b>Sie ist im Code gebaut und nicht in XAML</b>, aus demselben Grund wie die
/// Schriftgrößen- und Schriftartenliste (§4.39): Die Reihen kommen aus einer Tabelle, die
/// sich mit der Sprache ändert. In XAML stünden 60 Tasten doppelt da — einmal deutsch,
/// einmal englisch —, und die dritte Belegung schriebe jemand nur noch in eine davon.
/// </para>
///
/// <para>
/// ⛔ <b>Die Tasten nehmen keinen Fokus, und daran hängt alles.</b> Eine Tastatur, deren
/// Tasten fokussierbar sind, nimmt dem Textfeld beim ersten Tastendruck den Fokus — das
/// Feld schließt (im Whiteboard verwirft es sich sogar), und die Tastatur schreibt danach
/// ins Leere. <c>Focusable = false</c> an jedem Knopf ist deshalb keine Feinheit, sondern
/// die Voraussetzung dafür, dass das Ding überhaupt etwas tut.
/// </para>
///
/// <para>
/// <b>Wie sie schreibt:</b> Sie erhebt <c>TextInput</c> bzw. <c>KeyDown</c> am Element, das
/// gerade den Fokus hat. Das ist bewusst derselbe Weg, den auch eine echte Tastatur nimmt —
/// dadurch funktioniert sie ohne eine Zeile Zusatzcode im Textdokument-Editor <b>und</b> im
/// Eingabefeld der Tafel, und sie kann später auf iPadOS unverändert mitkommen. Ein eigener
/// Weg „schreibe in den TextBox, den ich kenne" wäre die Falle aus §4.13: zwei Wege zum
/// selben Ziel, von denen einer irgendwann etwas anderes tut.
/// </para>
/// </summary>
public sealed class TastaturView : UserControl
{
    private Bildschirmtastatur.Stand _stand = Bildschirmtastatur.Stand.Grund;
    private readonly StackPanel _reihen = new() { Spacing = 4 };

    /// <summary>Wird ausgelöst, wenn der Nutzer die Tastatur über ihre eigene Taste zuklappt.</summary>
    public event Action? Zugeklappt;

    public TastaturView()
    {
        // Die Tastatur selbst darf ebenso wenig Fokus nehmen wie ihre Tasten.
        Focusable = false;
        Padding = new Thickness(8, 6);

        // **Sie schrumpft, statt auszufransen.** Die Reihen werden in Grundbreiten gebaut
        // (Core); wie breit eine Grundbreite wirklich ist, entscheidet der Platz. Ohne das
        // ragte die längste Reihe aus einem schmalen Fenster heraus und die äußeren Tasten
        // wären unerreichbar — auf einem Gerät ohne Hardware-Tastatur wäre das das Ende der
        // Eingabe. `DownOnly`, damit sie auf einem breiten Schirm nicht ins Alberne wächst.
        Content = new Viewbox
        {
            Child = _reihen,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        Aufbauen();
        Loc.LanguageChanged += Aufbauen;
        DetachedFromVisualTree += (_, _) => Loc.LanguageChanged -= Aufbauen;
    }

    /// <summary>
    /// Baut die Reihen neu auf. <b>Bei jedem Sprachwechsel</b> — die Belegung folgt der
    /// Oberflächensprache (Begründung in <see cref="Bildschirmtastatur"/>).
    /// </summary>
    private void Aufbauen()
    {
        _reihen.Children.Clear();
        var reihen = Bildschirmtastatur.Reihen(Loc.Current);

        foreach (var reihe in reihen)
        {
            var zeile = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            foreach (var taste in reihe) zeile.Children.Add(Knopf(taste));
            _reihen.Children.Add(zeile);
        }
    }

    /// <summary>Die Breite einer Grundbreite in Punkten. Fingerfreundlich wie der Zahlenblock.</summary>
    private const double Grundbreite = 52;

    private Button Knopf(Bildschirmtastatur.Taste taste)
    {
        var knopf = new Button
        {
            Classes = { "taste" },
            // ⛔ Ohne das schließt sich das Textfeld beim ersten Tastendruck. Siehe Kopf.
            Focusable = false,
            Width = Grundbreite * taste.Breite + 4 * (taste.Breite - 1),
            Content = new TextBlock
            {
                Text = Bildschirmtastatur.Beschriftung(taste, _stand),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
            Tag = taste,
        };
        knopf.Click += (_, _) => Gedrueckt(taste);
        return knopf;
    }

    /// <summary>
    /// Schreibt die Beschriftungen neu, ohne die Knöpfe neu zu bauen. <b>Neu bauen wäre hier
    /// falsch:</b> ein Umschalter wechselt bei jedem zweiten Tastendruck, und ein
    /// Oberflächenbaum, der dabei jedes Mal verworfen wird, flackert und verliert den
    /// gedrückten Zustand mitten in der Bewegung.
    /// </summary>
    private void BeschriftungenNachfuehren()
    {
        foreach (var zeile in _reihen.Children.OfType<StackPanel>())
            foreach (var knopf in zeile.Children.OfType<Button>())
                if (knopf is { Tag: Bildschirmtastatur.Taste t, Content: TextBlock tb })
                {
                    tb.Text = Bildschirmtastatur.Beschriftung(t, _stand);
                    knopf.Classes.Set("gedrueckt",
                        (t.Art == Bildschirmtastatur.Art.Umschalt && _stand.Umschalt) ||
                        (t.Art == Bildschirmtastatur.Art.AltGr && _stand.AltGr));
                }
    }

    private void Gedrueckt(Bildschirmtastatur.Taste taste)
    {
        if (taste.Art == Bildschirmtastatur.Art.Zu) { Zugeklappt?.Invoke(); return; }

        // **Erst senden, dann den Stand fortschreiben** — sonst schriebe eine Umschalt-Taste
        // ihr eigenes Zeichen schon in der neuen Ebene.
        Senden(taste);
        _stand = Bildschirmtastatur.NaechsterStand(taste, _stand);
        BeschriftungenNachfuehren();
    }

    /// <summary>
    /// Schickt die Taste an das Element mit dem Fokus.
    ///
    /// <para>
    /// <b>Ohne Fokusziel passiert nichts</b>, und das ist richtig: Eine Tastatur, die ins
    /// Leere schreibt, wäre schlimmer als eine, die nichts tut — der Text landete
    /// irgendwo oder nirgends, und der Nutzer suchte ihn.
    /// </para>
    /// </summary>
    private void Senden(Bildschirmtastatur.Taste taste)
    {
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not InputElement ziel)
            return;

        if (Bildschirmtastatur.Zeichen(taste, _stand) is { } text)
        {
            ziel.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = InputElement.TextInputEvent,
                Text = text,
            });
            return;
        }

        // Alles, was kein Zeichen schreibt, geht als Taste hinaus — dieselben Tasten, die
        // eine echte Tastatur schickt, damit Auswahl, Marke und Rückgängig sich genauso
        // verhalten.
        var key = taste.Art switch
        {
            Bildschirmtastatur.Art.Rueck => Key.Back,
            Bildschirmtastatur.Art.Eingabe => Key.Enter,
            Bildschirmtastatur.Art.Tab => Key.Tab,
            Bildschirmtastatur.Art.Links => Key.Left,
            Bildschirmtastatur.Art.Rechts => Key.Right,
            _ => Key.None,
        };
        if (key == Key.None) return;

        ziel.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = key,
        });
    }
}
