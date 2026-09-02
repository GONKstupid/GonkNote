using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Der Reiter <b>Layout</b> und die <b>Einstellungs-Seitenleiste</b> rechts — §4.38.
///
/// <para>
/// <b>Die Aufteilung ist die des WPF-Kopfs, und sie hat einen Grund:</b> Eine Werkzeugleiste
/// trägt Werkzeuge. Was viele Zahlen hat und selten gebraucht wird — Seitenränder,
/// Absatzabstände —, steht neunundneunzigmal im Weg, wenn es einmal gebraucht wird. Es wandert
/// deshalb in die Seitenleiste, und in der Leiste bleibt ein Knopf, der sie öffnet.
/// </para>
/// <para>
/// <b>Zwei verschiedene Sorten Änderung stehen hier nebeneinander, und der Unterschied ist
/// benannt:</b> Absatzabstände laufen über <see cref="TdFormatEdit.Absatz"/> und stehen damit
/// im Verlauf wie jede andere Änderung. **Seitenränder und Papierformat nicht** — sie sitzen am
/// <see cref="TdSection"/> und nicht in einer Blockliste, und <see cref="TdChange"/> tauscht
/// Blöcke (§4.32). Ein eigener Verlaufsweg dafür wäre eine zweite Mechanik neben der
/// vorhandenen; **die Grenze steht stattdessen als Satz in der Seitenleiste**
/// (<c>Ed.Page.NoUndo</c>), damit niemand sie durch Ausprobieren findet.
/// </para>
/// </summary>
public partial class TextDocView
{
    /// <summary>Welcher Abschnitt der Seitenleiste gerade offen ist — <c>null</c> = keiner.</summary>
    private string? _abschnitt;

    /// <summary>
    /// Sperrt das Zurückschreiben, während <see cref="SeiteNachziehen"/> die Felder füllt.
    /// <b>Ohne sie löste jedes Nachziehen die Änderung aus, die es gerade anzeigt</b> — und ein
    /// Dokument, dessen Ränder man nur ansieht, wäre danach geändert.
    /// </summary>
    private bool _fuellt;

    // ==================== Die Leiste öffnen ====================

    private void Raender_Click(object? s, RoutedEventArgs e) => Abschnitt("Ränder");
    private void Abstaende_Click(object? s, RoutedEventArgs e) => Abschnitt("Abstände");
    private void KopfFuss_Click(object? s, RoutedEventArgs e) => Abschnitt("KopfFuss");

    private void EinstellungenZu_Click(object? s, RoutedEventArgs e)
    {
        _abschnitt = null;
        Einstellungen.IsVisible = false;
    }

    /// <summary>
    /// Öffnet einen Abschnitt — <b>oder schließt die Leiste, wenn er schon offen war</b>. Ein
    /// Knopf, der nur öffnet, zwingt zum Griff ans Kreuz; derselbe Knopf zweimal ist die
    /// Bewegung, die jeder ohnehin macht.
    /// </summary>
    private void Abschnitt(string name)
    {
        _abschnitt = _abschnitt == name ? null : name;

        Einstellungen.IsVisible = _abschnitt is not null;
        AbschnittRaender.IsVisible = _abschnitt == "Ränder";
        AbschnittAbstaende.IsVisible = _abschnitt == "Abstände";
        AbschnittKopfFuss.IsVisible = _abschnitt == "KopfFuss";

        EinstellungenTitel.Text = _abschnitt switch
        {
            "Ränder" => Loc.T("Ed.Layout.Margins"),
            "Abstände" => Loc.T("Ed.Paragraphs"),
            "KopfFuss" => Loc.T("Ed.HeaderFooter"),
            _ => "",
        };

        SeiteNachziehen();
    }

    // ==================== Papierformat und Ausrichtung ====================

    /// <summary>
    /// Setzt das Papierformat des Abschnitts, in dem die Marke steht.
    ///
    /// <para>
    /// <b>Die Ausrichtung bleibt dabei erhalten:</b> Wer ein quer liegendes A5 auf A4 stellt,
    /// meint ein quer liegendes A4 — <see cref="TdPageSetup.A4"/> und Co. liefern immer das
    /// Hochformat, und die Seiten werden hier bei Bedarf getauscht.
    /// </para>
    /// </summary>
    private void Papierformat_Gewechselt(object? sender, SelectionChangedEventArgs e)
    {
        if (_fuellt || !Schreibbar) return;
        if ((sender as ComboBox)?.SelectedItem is not ComboBoxItem { Tag: string name }) return;
        if (Abschnitt() is not { } abschnitt) return;

        var neu = name switch
        {
            "A5" => TdPageSetup.A5,
            "A3" => TdPageSetup.A3,
            "Letter" => TdPageSetup.Letter,
            _ => TdPageSetup.A4,
        };

        bool quer = abschnitt.Page.IstQuerformat;

        // Ränder, Kopf- und Fußzeile gehören nicht zum Papier — sie bleiben stehen.
        var seite = abschnitt.Page;
        seite.WidthCm = quer ? neu.HeightCm : neu.WidthCm;
        seite.HeightCm = quer ? neu.WidthCm : neu.HeightCm;

        SeiteGeaendert();
    }

    private void Ausrichtung_Click(object? sender, RoutedEventArgs e)
    {
        if (_fuellt || !Schreibbar) return;
        if ((sender as ToggleButton)?.Tag is not string tag) return;
        if (Abschnitt() is not { } abschnitt) return;

        bool quer = tag == "L";
        var seite = abschnitt.Page;

        if (seite.IstQuerformat == quer) { SeiteNachziehen(); return; }

        (seite.WidthCm, seite.HeightCm) = (seite.HeightCm, seite.WidthCm);
        SeiteGeaendert();
    }

    // ==================== Seitenränder ====================

    /// <summary>
    /// <b>Nur reagieren, wenn sich wirklich etwas ändert</b> (§4.93).
    ///
    /// <para>
    /// <b>Der Schutz über <c>_fuellt</c> allein reicht hier nicht.</b> Ein
    /// <c>NumericUpDown</c> meldet seinen Wert nicht immer im selben Zug, in dem er gesetzt
    /// wird — kommt die Meldung einen Takt später, steht <c>_fuellt</c> längst wieder auf
    /// <c>false</c>, und das bloße Öffnen eines Dokuments markierte es als
    /// <b>geändert</b>. Am laufenden Programm gemessen: der Reiter trug sofort den Punkt für
    /// „ungespeichert“, ohne dass jemand etwas angefasst hatte — <b>und der WPF-Kopf tat es
    /// bei demselben Dokument nicht.</b>
    /// </para>
    /// <para>
    /// <b>Ein Vergleich vor dem Schreiben ist der Schutz, der kein Zeitfenster braucht</b> —
    /// dieselbe Regel wie in §4.90 („dieselbe Füllung zweimal ist keine Änderung“).
    /// </para>
    /// </summary>
    private void Rand_Geaendert(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_fuellt || !Schreibbar) return;
        if (Abschnitt() is not { } abschnitt) return;

        var seite = abschnitt.Page;

        double links = (double)(RandLinks.Value ?? 0);
        double oben = (double)(RandOben.Value ?? 0);
        double rechts = (double)(RandRechts.Value ?? 0);
        double unten = (double)(RandUnten.Value ?? 0);

        if (Gleich(seite.MarginLeftCm, links) && Gleich(seite.MarginTopCm, oben) &&
            Gleich(seite.MarginRightCm, rechts) && Gleich(seite.MarginBottomCm, unten)) return;

        seite.MarginLeftCm = links;
        seite.MarginTopCm = oben;
        seite.MarginRightCm = rechts;
        seite.MarginBottomCm = unten;

        SeiteGeaendert();
    }

    /// <summary>Zwei Zentimeterangaben, die auf einen hundertstel Millimeter gleich sind.</summary>
    private static bool Gleich(double a, double b) => Math.Abs(a - b) < 0.0001;

    // ==================== Absatzabstände ====================

    private void Abstand_Geaendert(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_fuellt || !Schreibbar) return;

        double davor = (double)(AbstandDavor.Value ?? 0);
        double danach = (double)(AbstandDanach.Value ?? 0);

        // Dieselbe Vorsorge wie bei den Rändern (§4.93): Ein spät gemeldeter Wert, der sich
        // gar nicht unterscheidet, darf kein Verlaufsschritt werden.
        var jetzt = TdFormatEdit.GemeinsamerAbsatz(_modell!, _auswahl);
        if (Gleich(jetzt.SpaceBeforePt ?? 0, davor) && Gleich(jetzt.SpaceAfterPt ?? 0, danach))
            return;

        // **`null` statt `0`, wo nichts gesetzt ist** (§4.14): Eine ausdrückliche Null am Absatz
        // überschriebe einen Abstand, der aus dem Dokumentstandard kommt.
        Aendern(TdFormatEdit.Absatz(_modell!, _auswahl, f =>
        {
            f.SpaceBeforePt = davor == 0 ? null : davor;
            f.SpaceAfterPt = danach == 0 ? null : danach;
        }));
    }

    private void Zeilenabstand_Gewechselt(object? sender, SelectionChangedEventArgs e)
    {
        if (_fuellt || !Schreibbar) return;
        if ((sender as ComboBox)?.SelectedItem is not ComboBoxItem { Tag: string tag }) return;
        if (!double.TryParse(tag, System.Globalization.CultureInfo.InvariantCulture, out double wert))
            return;

        Aendern(TdFormatEdit.Absatz(_modell!, _auswahl, f => f.LineSpacing = wert));
    }

    // ==================== Kopf- und Fußzeile ====================

    /// <summary>
    /// Übernimmt Kopf- und Fußzeile. <b>Beim Verlassen des Feldes und bei Eingabe</b> — nicht
    /// bei jedem Tastendruck: Jeder Buchstabe löste sonst einen vollen Umbruch aus (§4.35), und
    /// zwar den teuersten, weil Kopf- und Fußzeile auf **jeder** Seite stehen.
    /// </summary>
    private void KopfFuss_Geaendert(object? sender, RoutedEventArgs e) => KopfFussUebernehmen();

    private void KopfFuss_Taste(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key is not (Avalonia.Input.Key.Enter or Avalonia.Input.Key.Return)) return;

        KopfFussUebernehmen();
        e.Handled = true;
    }

    private void KopfFussUebernehmen()
    {
        if (_fuellt || !Schreibbar) return;
        if (Abschnitt() is not { } abschnitt) return;

        string kopf = KopfzeileFeld.Text ?? "";
        string fuss = FusszeileFeld.Text ?? "";

        if (abschnitt.Page.HeaderText == kopf && abschnitt.Page.FooterText == fuss) return;

        abschnitt.Page.HeaderText = kopf;
        abschnitt.Page.FooterText = fuss;

        SeiteGeaendert();
    }

    private void ErsteSeiteOhne_Geaendert(object? sender, RoutedEventArgs e)
    {
        if (_fuellt || !Schreibbar) return;
        if (Abschnitt() is not { } abschnitt) return;

        bool ohne = ErsteSeiteOhne.IsChecked == true;
        if (abschnitt.Page.SuppressOnFirstPage == ohne) return;

        abschnitt.Page.SuppressOnFirstPage = ohne;
        SeiteGeaendert();
    }

    // ==================== Nachziehen ====================

    /// <summary>
    /// Der Abschnitt, in dem die Marke steht.
    ///
    /// <para>
    /// <b>Nicht einfach der erste.</b> Ein Dokument darf mehrere haben, und jeder trägt seine
    /// eigene Seiteneinrichtung (§4.15) — wer hier `Sections[0]` nähme, stellte beim Blättern in
    /// einem quer liegenden Abschnitt die Ränder des ersten.
    /// </para>
    /// </summary>
    private TdSection? Abschnitt() =>
        _modell?.AbschnittVon(_auswahl.Focus.Paragraph) ?? _modell?.Sections.FirstOrDefault();

    /// <summary>
    /// Nach einer Änderung an der Seiteneinrichtung: neu umbrechen und als geändert merken.
    /// <b>Ohne Verlaufsschritt</b> — siehe die Begründung oben.
    /// </summary>
    private void SeiteGeaendert()
    {
        if (_vm is not null) _vm.IsDirty = true;

        NeuUmbrechen();
        SeiteNachziehen();
    }

    /// <summary>
    /// Stellt Format, Ausrichtung, Ränder und Abstände auf das, was gerade gilt — gerufen aus
    /// <see cref="RibbonNachziehen"/>, also aus demselben einen Trichter wie alles andere.
    /// </summary>
    private void SeiteNachziehen()
    {
        if (FormatWahl is null) return;

        _fuellt = true;
        try
        {
            bool an = Schreibbar;
            FormatWahl.IsEnabled = an;
            SchalterHoch2.IsEnabled = an;
            SchalterQuer.IsEnabled = an;

            var seite = an ? Abschnitt()?.Page : null;
            if (seite is null)
            {
                FormatWahl.SelectedItem = null;
                SchalterHoch2.IsChecked = false;
                SchalterQuer.IsChecked = false;
                return;
            }

            // Ein Blatt ohne benanntes Format ist kein Fehler — es hat nur keinen Namen (§4.15);
            // dann steht in der Liste nichts, statt eine falsche Zeile zu behaupten.
            FormatWahl.SelectedItem = FormatWahl.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => (string?)i.Tag == seite.Name);

            SchalterHoch2.IsChecked = !seite.IstQuerformat;
            SchalterQuer.IsChecked = seite.IstQuerformat;

            // **Nicht überschreiben, während der Nutzer tippt** — dieselbe Vorsorge wie beim
            // Verweisziel (§4.37): Ein Umbruch mitten in der Eingabe nähme ihm sonst den Satz
            // aus der Hand.
            if (!KopfzeileFeld.IsFocused) KopfzeileFeld.Text = seite.HeaderText;
            if (!FusszeileFeld.IsFocused) FusszeileFeld.Text = seite.FooterText;
            ErsteSeiteOhne.IsChecked = seite.SuppressOnFirstPage;

            WasserzeichenNachziehen();

            RandLinks.Value = (decimal)seite.MarginLeftCm;
            RandOben.Value = (decimal)seite.MarginTopCm;
            RandRechts.Value = (decimal)seite.MarginRightCm;
            RandUnten.Value = (decimal)seite.MarginBottomCm;

            var absatz = TdFormatEdit.GemeinsamerAbsatz(_modell!, _auswahl);
            AbstandDavor.Value = (decimal)(absatz.SpaceBeforePt ?? 0);
            AbstandDanach.Value = (decimal)(absatz.SpaceAfterPt ?? 0);

            // Uneinige Auswahl: keine Zeile gewählt, statt eine der beiden zu behaupten
            // (dieselbe dritte Antwort wie bei den Formatknöpfen, §4.36).
            Zeilenabstand.SelectedItem = absatz.LineSpacing is { } zeilen
                ? Zeilenabstand.Items.OfType<ComboBoxItem>().FirstOrDefault(
                    i => double.TryParse((string?)i.Tag,
                             System.Globalization.CultureInfo.InvariantCulture, out double w)
                         && Math.Abs(w - zeilen) < 0.001)
                : null;
        }
        finally
        {
            _fuellt = false;
        }
    }
}
