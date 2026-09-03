using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Interactivity;
using GonkNote.Core.Text;
using GonkNote.Core.Theming;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// <b>Aufzählung, Nummerierung, Absatzvorlagen und die Schriftgrößenliste</b> — Gruppe A aus §6
/// (HANDOFF §4.39).
///
/// <para>
/// <b>Sie rechnet nichts</b>, wie die Geschwisterdateien: <see cref="TdListEdit"/> weiß, was
/// eine Liste oder eine Vorlage am Absatz ändert, und <see cref="TdStil.Alle"/> ist die
/// Tabelle. Hier steht die Übersetzung von Klicks in diese Aufrufe — und das eine, was nur hier
/// geht: die Listen **aus der Tabelle** aufbauen, statt sie in der XAML zu wiederholen.
/// </para>
/// <para>
/// <b>Beide Listen entstehen im Code und tragen ihre Beschriftung aus <see cref="Loc.T"/>.</b>
/// Bei den Vorlagen ist das nötig, weil die Tabelle in Core steht; bei den Größen wäre es nicht
/// nötig, aber neunzehn Einträge von Hand in die XAML zu schreiben hieße, die Leiter aus
/// <c>TextDocView.Format.cs</c> ein zweites Mal hinzuschreiben (§4.13). **Ein Sprachwechsel
/// zieht die Vorlagennamen deshalb über <c>OnLanguageChanged</c> nach** — anders als bei
/// <c>{loc:T …}</c> tut Avalonia das hier nicht von selbst (§7).
/// </para>
/// </summary>
public partial class TextDocView
{
    /// <summary>
    /// Sperrt das Zurückschreiben, während die Listen gefüllt werden — <b>ohne sie setzte jedes
    /// Nachziehen die Vorlage, die es gerade anzeigt</b> (dieselbe Vorsorge wie in
    /// <c>TextDocView.Seite.cs</c>).
    /// </summary>
    private bool _fuelltListen;

    /// <summary>
    /// Baut die beiden Auswahllisten auf. <b>Gerufen aus dem Konstruktor und bei jedem
    /// Sprachwechsel</b> — die Vorlagennamen kommen aus <see cref="Loc.T"/>, und Avalonia zieht
    /// nur nach, was als <c>{loc:T …}</c> in der XAML steht.
    /// </summary>
    private void ListenAufbauen()
    {
        _fuelltListen = true;
        try
        {
            VorlagenAufbauen();

            GroessenlisteSetzen(Groessen);

            MarkenAufbauen();
            SonderzeichenAufbauen();
            ZellfarbenAufbauen();
            TabellenrasterAufbauen();
        }
        finally
        {
            _fuelltListen = false;
        }
    }

    // ==================== Listen ====================

    private void Aufzaehlung_Click(object? s, RoutedEventArgs e) => Liste(nummeriert: false);
    private void Nummerierung_Click(object? s, RoutedEventArgs e) => Liste(nummeriert: true);

    // ==================== Die Markenauswahl (§4.88) ====================

    /// <summary>
    /// Baut die beiden Kachelfelder. <b>Aus <see cref="TdMarkenvorrat"/> in Core</b> — bis
    /// §4.88 stand die Liste fest verdrahtet im WPF-Kopf, und dieser hier hätte sie ein zweites
    /// Mal gebraucht. Das ist zum vierten Mal derselbe Fall (§4.77, §4.78, §4.82).
    ///
    /// <para>
    /// <b>Die Kachel zeigt das Zeichen selbst und keine Vorschauzeile.</b> Drüben stehen drei
    /// Zeilen aus Marke und Strich in jeder Kachel — bei sechs Punkten, die sich nur im Zeichen
    /// unterscheiden, sagt die dritte Zeile nichts, was die erste nicht schon sagt.
    /// </para>
    /// </summary>
    private void MarkenAufbauen()
    {
        PunktwahlFeld.Children.Clear();
        NummernwahlFeld.Children.Clear();

        foreach (var zeichen in TdMarkenvorrat.Punkte)
            PunktwahlFeld.Children.Add(
                Markenkachel(zeichen, () => Marke(TdListMarker.Bullet, zeichen)));

        foreach (var art in TdMarkenvorrat.Nummern)
            NummernwahlFeld.Children.Add(
                Markenkachel(TdMarkenvorrat.Beispiel(art),
                             () => Marke(art, TdMarkenvorrat.Muster(0))));
    }

    private static Button Markenkachel(string beschriftung, Action gewaehlt)
    {
        var kachel = new Button
        {
            // `kachel` statt der Fluent-Vorgabe (Schritt ②): ohne die Klasse standen hier
            // graue Kästen, und ein Raster aus grauen Kästen zeigt seine Zeichen schlechter
            // als eines ohne. Drüben ist es `FlatButton` (BuildSymbolGrid).
            Classes = { "kachel" },
            Width = 30,
            Height = 30,
            Margin = new Avalonia.Thickness(1),
            Content = new TextBlock
            {
                Text = beschriftung,
                FontSize = 14,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            },
        };

        kachel.Click += (_, _) => gewaehlt();
        return kachel;
    }

    private void Marke(TdListMarker art, string zeichen)
    {
        if (!Schreibbar) return;

        Aendern(TdListEdit.Marke(_modell!, _auswahl, art, zeichen));
        RibbonNachziehen();

        // Das Flyout schließt sich von selbst; der Fokus muss zurück auf die Fläche, sonst
        // gilt danach kein Kürzel mehr (§4.86).
        Skia.Focus();
    }

    // ==================== Die Sonderzeichen (§4.88) ====================

    /// <summary>
    /// Baut das Sonderzeichen-Feld — <b>nach Gruppen</b>, mit einer Überschrift je Gruppe.
    ///
    /// <para>
    /// Der Vorrat steht als <see cref="TdSonderzeichen"/> in Core. Drüben lagen 57 Zeichen in
    /// **einem** Raster: Wer darin „≠" sucht, liest alle 57. Die Gruppen kosten nichts, und
    /// ihre Namen kommen aus <see cref="Loc.T"/> — deshalb wird das Feld beim Sprachwechsel
    /// neu gebaut wie die Vorlagenliste darüber.
    /// </para>
    /// </summary>
    private void SonderzeichenAufbauen()
    {
        SonderzeichenFeld.Children.Clear();

        foreach (var gruppe in TdSonderzeichen.Gruppen)
        {
            var ueberschrift = new TextBlock
            {
                Text = Loc.T(gruppe.Schluessel),
                FontSize = 11,
                Margin = new Avalonia.Thickness(2, 4, 0, 0),
            };

            // **Über `DynamicResource` und nicht über `FindResource`** (§4.88): Dieses Feld wird
            // im Konstruktor gebaut, und da hängt das Control noch an keinem Baum — `FindResource`
            // lieferte `null`, und die Überschriften standen **unsichtbar** im Flyout. Der Bau war
            // grün, die Wächter auch; gesehen hat es erst der Blick aufs laufende Programm.
            ueberschrift[!TextBlock.ForegroundProperty] =
                new DynamicResourceExtension("Brush.TextMuted");

            SonderzeichenFeld.Children.Add(ueberschrift);

            var feld = new WrapPanel();
            foreach (var zeichen in gruppe.Zeichen)
                feld.Children.Add(Sonderzeichenkachel(zeichen));

            SonderzeichenFeld.Children.Add(feld);
        }
    }

    private Button Sonderzeichenkachel(string zeichen)
    {
        var kachel = new Button
        {
            // `kachel` statt der Fluent-Vorgabe (Schritt ②): ohne die Klasse standen hier
            // graue Kästen, und ein Raster aus grauen Kästen zeigt seine Zeichen schlechter
            // als eines ohne. Drüben ist es `FlatButton` (BuildSymbolGrid).
            Classes = { "kachel" },
            Width = 30,
            Height = 30,
            Margin = new Avalonia.Thickness(1),
            Content = new TextBlock
            {
                Text = zeichen,
                FontSize = 15,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            },
        };

        kachel.Click += (_, _) => Sonderzeichen(zeichen);
        return kachel;
    }

    /// <summary>
    /// <b>Ein Sonderzeichen ist getippter Text und nichts weiter</b> — deshalb
    /// <see cref="TdEdit.Tippen"/> und kein eigener Handgriff. Es ersetzt eine gezogene Auswahl
    /// wie jede andere Eingabe, und der Verlauf fasst mehrere hintereinander zu einem Schritt
    /// zusammen (§4.33), genau wie bei der Tastatur.
    /// </summary>
    private void Sonderzeichen(string zeichen)
    {
        if (!Schreibbar) return;

        Aendern(TdEdit.Tippen(_modell!, _auswahl, zeichen));
        Skia.Focus();
    }

    private void Liste(bool nummeriert)
    {
        if (!Schreibbar) return;

        Aendern(TdListEdit.Umschalten(_modell!, _auswahl, nummeriert));
        RibbonNachziehen();
    }

    // ==================== Vorlagen (Design-Konzept 7.4) ====================

    /// <summary>
    /// Alle Kacheln, die eine Vorlage zeigen — <b>die drei in der Leiste und die zehn im
    /// Aufklappfeld zusammen</b>. Die Liste dient nur dem Nachziehen des Akzentrahmens:
    /// „Überschrift 1" steht zweimal da und muss beide Male gleich aussehen.
    /// </summary>
    private readonly List<Button> _stilkacheln = [];

    /// <summary>
    /// Die drei Vorlagen, die <b>ohne Aufklappen</b> erreichbar sind. <b>Dieselben drei wie
    /// drüben</b> (<c>InlineStyleNames</c> im WPF-Kopf): Standard und die beiden obersten
    /// Überschriften decken ab, was beim Schreiben ständig gebraucht wird; alles Weitere ist
    /// einen Klick entfernt. Verglichen wird am <b>internen</b> Namen und nicht am
    /// übersetzten — <see cref="TdStil.Name"/> steht dafür in der Tabelle.
    /// </summary>
    private static readonly string[] VorlagenInLeiste =
        ["Standard", "Überschrift 1", "Überschrift 2"];

    /// <summary>
    /// Baut beide Kachelfelder aus <see cref="TdStil.Alle"/>. <b>Wie die Listen wird auch das
    /// beim Sprachwechsel wiederholt</b> — die Beschriftung einer Kachel ist der übersetzte
    /// Name der Vorlage.
    /// </summary>
    private void VorlagenAufbauen()
    {
        Vorlagenkacheln.Children.Clear();
        VorlagenFeld.Children.Clear();
        _stilkacheln.Clear();

        foreach (var stil in TdStil.Alle)
        {
            if (VorlagenInLeiste.Contains(stil.Name))
                Vorlagenkacheln.Children.Add(Stilkachel(stil, imFeld: false));

            VorlagenFeld.Children.Add(Stilkachel(stil, imFeld: true));
        }
    }

    /// <summary>
    /// Eine Kachel: die Vorlage, gezeigt statt genannt.
    ///
    /// <para>
    /// <b>Die Vorschau übernimmt Farbe, Fett und Kursiv der Vorlage, aber nicht ihre
    /// Größe</b> — „Titel" sind 25,5 pt, und eine Kachel von 44 px Höhe kann die nicht
    /// zeigen. Gestaucht wird nach derselben Formel wie drüben
    /// (<c>Math.Min(14, Size * 0,5 + 4)</c>), damit die Abstufung erhalten bleibt: Die
    /// Kachel soll die Rangfolge zeigen, nicht die Punktzahl.
    /// </para>
    /// <para>
    /// <b>Die Tinte ist fest und kommt nicht aus der Farbtabelle</b> — sie liegt auf der
    /// Papierfläche der Kachel, und die ist in beiden Erscheinungsbildern weiß
    /// (<c>TdRenderer.Papier</c>). Ein <c>Brush.Text</c> wäre im dunklen Bild hell und
    /// stünde unsichtbar auf Weiß. Derselbe feste Wert steht drüben als <c>InkBrush</c>.
    /// </para>
    /// </summary>
    private Button Stilkachel(TdStil stil, bool imFeld)
    {
        var vorschau = new TextBlock
        {
            Text = Loc.T(stil.Key),
            FontSize = Math.Min(14, stil.SizePt * 0.5 + 4),
            FontWeight = stil.Bold ? Avalonia.Media.FontWeight.SemiBold
                                   : Avalonia.Media.FontWeight.Normal,
            FontStyle = stil.Italic ? Avalonia.Media.FontStyle.Italic
                                    : Avalonia.Media.FontStyle.Normal,
            TextAlignment = Avalonia.Media.TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse(stil.ColorHex ?? Papierschrift)),
        };

        var kachel = new Button
        {
            Classes = { "stilkachel" },
            Content = vorschau,
            Tag = stil.Name,
        };

        if (imFeld) kachel.Margin = new Avalonia.Thickness(2, 2);

        ToolTip.SetTip(kachel, stil.Heading > 0
            ? Loc.T("Msg.HeadingTip", Loc.T(stil.Key), stil.Heading)
            : Loc.T(stil.Key));

        Avalonia.Automation.AutomationProperties.SetName(
            kachel, Loc.T("Msg.StyleName", Loc.T(stil.Key)));

        kachel.Click += (_, _) =>
        {
            Vorlage(stil);

            // Das Aufklappfeld schließt sich nicht von selbst, wenn der Klick auf einem
            // Knopf darin landet — und ein Feld, das nach der Wahl offen stehen bleibt,
            // verdeckt genau den Absatz, den es gerade geändert hat.
            if (imFeld) KnopfVorlagen.Flyout?.Hide();
        };

        _stilkacheln.Add(kachel);
        return kachel;
    }

    /// <summary>Die Schreibfarbe auf der Papierfläche einer Kachel — <c>Brush.Text</c> hell.</summary>
    private const string Papierschrift = "#1B2B4B";

    private void Vorlage(TdStil stil)
    {
        if (!Schreibbar) return;

        Aendern(TdListEdit.Vorlage(_modell!, _auswahl, stil));
        RibbonNachziehen();

        // Wie bei den Marken (§4.86): ohne den Rücksprung gilt danach kein Kürzel mehr.
        Skia.Focus();
    }

    // ==================== Schriftgröße ====================

    private void Groesse_Gewechselt(object? sender, SelectionChangedEventArgs e)
    {
        if (_fuelltListen || !Schreibbar) return;
        if ((sender as ComboBox)?.SelectedItem is not ComboBoxItem { Tag: double punkte }) return;

        // Derselbe Vergleich wie bei Schriftart und Papierformat (§4.95) — eine nachgetragene
        // Auswahl darf kein Verlaufsschritt werden.
        if (TdFormatEdit.Gemeinsam(_modell!, _auswahl).FontSize is { } gilt
            && Math.Abs(gilt - punkte) < 0.01) return;

        Aendern(TdFormatEdit.Zeichen(_modell!, _auswahl,
            (abweichung, _) => abweichung.FontSize = punkte));

        RibbonNachziehen();
    }

    // ==================== Nachziehen ====================

    /// <summary>
    /// Stellt Vorlage, Größe und die zwei Listenschalter auf das, was die Auswahl zeigt —
    /// gerufen aus <see cref="RibbonNachziehen"/>, also aus demselben einen Trichter.
    ///
    /// <para>
    /// <b>Uneinig heißt „nichts gewählt" und nicht „das erste"</b> (§4.36): Eine Liste, die über
    /// einer gemischten Auswahl eine Vorlage nennt, behauptet etwas Falsches.
    /// </para>
    /// </summary>
    private void ListenNachziehen()
    {
        if (GroesseWahl is null) return;

        _fuelltListen = true;
        try
        {
            bool an = Schreibbar;
            foreach (var kachel in _stilkacheln) kachel.IsEnabled = an;
            KnopfVorlagen.IsEnabled = an;
            GroesseWahl.IsEnabled = an;
            SchalterPunkte.IsEnabled = an;
            SchalterNummern.IsEnabled = an;
            KnopfPunktwahl.IsEnabled = an;
            KnopfNummernwahl.IsEnabled = an;

            if (!an)
            {
                VorlageMarkieren(null);
                GroesseWahl.SelectedItem = null;
                SchalterPunkte.IsChecked = false;
                SchalterNummern.IsChecked = false;
                return;
            }

            VorlageMarkieren(TdListEdit.GemeinsameVorlage(_modell!, _auswahl)?.Name);

            double? groesse = TdFormatEdit.Gemeinsam(_modell!, _auswahl).FontSize;

            // **Einen Grad, den die Leiter nicht hat, trotzdem zeigen** (§4.95). Bis dahin
            // blieb das Feld leer — richtig gehandelt (§4.36: was die Liste nicht hat, wird
            // nicht behauptet), aber der Nutzer konnte die geltende Größe weder ablesen noch
            // wiederherstellen, nachdem er einmal eine andere gewählt hatte.
            GroessenlisteSetzen(groesse is { } grad
                ? Schriftliste.GradeMit(grad)
                : Groessen);

            GroesseWahl.SelectedItem = groesse is { } pt
                ? Eintrag(GroesseWahl, i => i.Tag is double g && Math.Abs(g - pt) < 0.01)
                : null;

            SchalterPunkte.IsChecked = TdListEdit.Gemeinsam(_modell!, _auswahl, nummeriert: false);
            SchalterNummern.IsChecked = TdListEdit.Gemeinsam(_modell!, _auswahl, nummeriert: true);
        }
        finally
        {
            _fuelltListen = false;
        }
    }

    /// <summary>
    /// Setzt den Akzentrahmen auf die Kachel dieser Vorlage — und nimmt ihn von allen
    /// anderen. <b><c>null</c> heißt „uneinig oder keine"</b>, und dann trägt keine Kachel
    /// ihn: Dieselbe Regel wie bei der Klappliste davor (§4.36). Ein Rahmen, der über einer
    /// gemischten Auswahl auf „Standard" liegt, behauptet etwas Falsches.
    /// </summary>
    private void VorlageMarkieren(string? name)
    {
        foreach (var kachel in _stilkacheln)
        {
            bool gilt = name != null && (string?)kachel.Tag == name;

            if (gilt) kachel.Classes.Add("gilt");
            else kachel.Classes.Remove("gilt");
        }
    }

    /// <summary>
    /// Füllt die Größenliste — <b>und lässt sie in Ruhe, wenn schon dasselbe darin steht.</b>
    ///
    /// <para>
    /// Der Vergleich ist nicht Sparsamkeit: <see cref="ListenNachziehen"/> läuft nach jedem
    /// Klick und jeder Pfeiltaste. Würde die Liste jedes Mal neu gebaut, verlöre die
    /// <c>ComboBox</c> bei jedem Aufbau ihre Auswahl und meldete das — und eine gemeldete
    /// Auswahl, die niemand getroffen hat, ist genau der Fehler aus §4.95.
    /// </para>
    /// </summary>
    private void GroessenlisteSetzen(IReadOnlyList<double> grade)
    {
        if (GroesseWahl.ItemsSource is IReadOnlyList<ComboBoxItem> bisher
            && bisher.Count == grade.Count
            && bisher.Select((i, k) => i.Tag is double g && Math.Abs(g - grade[k]) < 0.01).All(x => x))
            return;

        GroesseWahl.ItemsSource = grade
            .Select(g => new ComboBoxItem { Content = $"{g:0.##}", Tag = g })
            .ToList();
    }

    private static ComboBoxItem? Eintrag(ComboBox liste, Func<ComboBoxItem, bool> passt) =>
        (liste.ItemsSource as IEnumerable<ComboBoxItem>)?.FirstOrDefault(passt);
}
