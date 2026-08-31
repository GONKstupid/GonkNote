using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GonkNote.Core.Models;
using GonkNote.Core.Platform;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using GonkNote.Core.Theming;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Das <b>Cover-Werkzeug</b> — neu in Phase 5, Schritt ①c (§4.81), und <b>der letzte
/// Abschnitt, der der Einstellungsleiste dieses Kopfes fehlte</b>.
///
/// <para>
/// <b>Gezeichnet hat der Linux-Kopf das Cover seit jeher</b> (<c>DrawCover</c> in
/// <c>WhiteboardView.Render.cs</c>) — ein Notizbuch, dessen Cover unter Windows gestaltet
/// wurde, sah hier immer richtig aus. <b>Bedienen ließ es sich nicht.</b> Das ist der
/// Unterschied, den §4.77 beim Tafel-Export schon einmal gemacht hat: *ein Werkzeug, dessen
/// Ergebnis man sieht und nicht ändern kann, wirkt wie ein Fehler in den Daten.*
/// </para>
/// <para>
/// <b>Wo die Vorlagen liegen und wie sie gruppiert sind, steht in Core</b>
/// (<see cref="CoverLibrary"/>) — nach dem Vorbild von <see cref="StickerLibrary"/> (§4.54)
/// und aus demselben Grund: zwei Fassungen derselben Sammlung sehen auf zwei Systemen
/// verschieden aus, und beide bestehen jeden Test (§4.13).
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>Kantenlänge einer Vorlagenkachel. Cover sind hochkant — daher höher als breit.</summary>
    private const int CoverKachelBreite = 58;

    private bool _coverVorlagenGeladen;

    // ==================== Der Datensatz ====================

    /// <summary>
    /// Der Cover-Stil des Dokuments, bei Bedarf angelegt. <b>Erst beim Ändern und nicht beim
    /// Anzeigen</b> — ein Dokument, das nie ein eigenes Cover bekommen hat, soll auch keinen
    /// leeren Datensatz mitschleppen (er landete sonst in der Datei).
    /// </summary>
    private CoverStyle CoverStil()
    {
        _vm!.Doc.Cover ??= new CoverStyle();
        return _vm.Doc.Cover;
    }

    /// <summary>
    /// Nach jeder Änderung: merken, zum Cover springen, neu zeichnen.
    ///
    /// <para>
    /// <b>Der Sprung ist der Punkt</b> — dieselbe Regel wie drüben. Wer die Startfarbe
    /// ändert, während er auf Seite 7 steht, sähe sonst nichts und hielte den Farbwähler für
    /// kaputt.
    /// </para>
    /// </summary>
    private void CoverGeaendert()
    {
        if (_vm == null) return;

        MarkDirty();

        int idx = _vm.Doc.Pages.FindIndex(p => p.IsCover);
        if (idx >= 0 && idx != _vm.PageIndex) NavigateToPage(_vm.Doc.Pages[idx]);

        Neuzeichnen();
    }

    // ==================== Sichtbarkeit und Spiegeln ====================

    /// <summary>
    /// Trägt den Zustand des Dokuments in den Abschnitt. <b>Wird aus
    /// <c>EinstellungenSpiegeln</c> gerufen und nicht selbst aktiv</b> — genau das ist die
    /// Falle aus §4.53: ein Abschnitt, der ohne Spiegeln aufklappt, zeigt lauter leere
    /// Schalter.
    /// </summary>
    private void CoverSpiegeln()
    {
        CoverBereich.IsVisible = _page?.IsCover == true;
        if (!CoverBereich.IsVisible) return;

        var stil = _vm?.Doc.Cover;

        CoverStartKachel.Background = Farbe(stil?.GradientStart, "#1E3A8A");
        CoverEndeKachel.Background = Farbe(stil?.GradientEnd, "#7C3AED");
        CoverBildWeg.IsEnabled = ImageCache.Bytes(stil?.ImageId ?? Guid.Empty, stil?.Image) is { Length: > 0 };

        CoverSchriftenFuellen(stil?.FontFamily);
        CoverVorlagenSicherstellen();

        static IBrush Farbe(string? hex, string rueckfall) =>
            HexColor.Parse(hex, HexColor.Parse(rueckfall, HexColor.Black)).ToBrush();
    }

    /// <summary>
    /// Die Schriftenliste des Cover-Wählers.
    ///
    /// <para>
    /// <b>Sie kommt aus <see cref="Schriftliste"/> in Core</b> — mitgelieferte oben,
    /// Systemschriften darunter, ohne Doppelte. **Das ist §5 Nr. 14**, dieselbe
    /// Entscheidung wie beim Schriftfeld des Editors (§4.73).
    /// </para>
    /// <para>
    /// ⚠ <b>Der WPF-Kopf hält hier bis heute eine fest verdrahtete Liste</b> — „Segoe UI",
    /// „Segoe Print", „Calibri" … , also <i>Windows</i>-Schriften an einer Stelle, die für
    /// beide Köpfe gilt. §5 Nr. 14 ist dort beim Editor angekommen und beim Cover nicht.
    /// Benannt in §4.81.
    /// </para>
    /// </summary>
    private void CoverSchriftenFuellen(string? gewaehlt)
    {
        // Dieselbe Quelle wie das Schriftfeld des Editors (TextDocView.Farben.cs).
        _coverSchriften ??= Schriftliste.Aufbauen(
            FontManager.Current.SystemFonts.Select(f => f.Name));

        _stummeEinstellungen = true;
        try
        {
            if (CoverSchrift.ItemsSource is null) CoverSchrift.ItemsSource = _coverSchriften;

            // **Was das Dokument nennt, wird angeboten, auch wenn dieses System die Schrift
            // nicht hat** — sonst spränge die Auswahl beim Öffnen still auf etwas anderes und
            // schriebe das beim nächsten Ändern in die Datei. Dieselbe Regel wie drüben.
            if (gewaehlt is { Length: > 0 } && !_coverSchriften.Contains(gewaehlt))
            {
                _coverSchriften = [.. _coverSchriften, gewaehlt];
                CoverSchrift.ItemsSource = _coverSchriften;
            }

            // ⚠ **Gegen die Liste prüfen und nicht gegen `CoverSchrift.Items`**: bei gesetztem
            // `ItemsSource` ist `Items` nicht die Liste, gegen die man sinnvoll vergleicht —
            // die Auswahl blieb dadurch leer, obwohl die Schrift dastand. Am laufenden
            // Programm gegen den WPF-Kopf gehalten, der „Space Grotesk" zeigte (§4.81).
            CoverSchrift.SelectedItem = gewaehlt is { Length: > 0 } ? gewaehlt : null;
        }
        finally { _stummeEinstellungen = false; }
    }

    /// <summary>Die Schriftenliste, einmal gebaut — sie kostet einen Durchgang durch alle Systemschriften.</summary>
    private IReadOnlyList<string>? _coverSchriften;

    // ==================== Farbe, Schrift, Bild ====================

    private void CoverStart_Click(object? sender, RoutedEventArgs e) => CoverFarbe(start: true);
    private void CoverEnde_Click(object? sender, RoutedEventArgs e) => CoverFarbe(start: false);

    private void CoverFarbe(bool start)
    {
        if (_vm == null) return;

        var stil = CoverStil();
        var jetzt = HexColor.Parse(start ? stil.GradientStart : stil.GradientEnd,
                                   HexColor.Parse(start ? "#1E3A8A" : "#7C3AED", HexColor.Black));

        if (ColorPickerWindow.Waehlen(TopLevel.GetTopLevel(this) as Window, jetzt,
                                      mitDeckkraft: false) is not { } gewaehlt)
            return;

        // **Ohne Alpha in die Datei**: der Verlauf des Covers ist undurchsichtig, und ein
        // mitgeschriebenes „FF" davor sähe im Datensatz aus wie eine Deckkraft, die es nicht
        // gibt. Dieselbe Schreibweise wie drüben.
        string hex = $"#{gewaehlt.R:X2}{gewaehlt.G:X2}{gewaehlt.B:X2}";
        if (start) stil.GradientStart = hex; else stil.GradientEnd = hex;

        (start ? CoverStartKachel : CoverEndeKachel).Background = gewaehlt.ToBrush();
        CoverGeaendert();
    }

    private void CoverSchrift_Geaendert(object? sender, SelectionChangedEventArgs e)
    {
        if (_stummeEinstellungen || _vm == null) return;
        if (CoverSchrift.SelectedItem is not string schrift) return;

        CoverStil().FontFamily = schrift;
        CoverGeaendert();
    }

    private void CoverBild_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;

        var dateien = App.Platform.Files.Open(
            Loc.T("Settings.Cover.ChooseImage"),
            [new FileFilter(Loc.T("Filter.Images"), Bildsammlung.Endungen)],
            multiple: false);
        if (dateien.Count == 0) return;

        if (!CoverBildSetzen(dateien[0]))
            App.Platform.Dialogs.Inform(Loc.T("Msg.ImageLoadSimple"), DialogSeverity.Warning);
    }

    /// <summary>
    /// Legt <paramref name="pfad"/> als Cover-Bild ab. <b>Der eine Weg für „Bild wählen…"
    /// und für einen Klick auf eine Vorlage</b> — zwei Wege gäben demselben Bild je nach
    /// Herkunft eine andere Aufbereitung.
    /// </summary>
    private bool CoverBildSetzen(string pfad)
    {
        try
        {
            if (WbImagePrep.ForImport(File.ReadAllBytes(pfad)) is not { } bild) return false;

            var stil = CoverStil();
            stil.Image = bild.Data;

            // **Eine neue Kennung bei jedem Wechsel**, auch beim Entfernen: der Bild-Cache
            // schlüsselt darüber, und ohne Wechsel zeigte er weiter das alte Bild.
            stil.ImageId = Guid.NewGuid();

            CoverBildWeg.IsEnabled = true;
            CoverGeaendert();
            return true;
        }
        catch { return false; }
    }

    private void CoverBildWeg_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm?.Doc.Cover is not { } stil) return;

        stil.Image = null;
        stil.ImageId = Guid.NewGuid();
        CoverBildWeg.IsEnabled = false;
        CoverGeaendert();
    }

    // ==================== Die Vorlagen ====================

    /// <summary>
    /// Lädt die Sammlung beim ersten Blick darauf und nicht beim Start — dieselbe Regel wie
    /// bei den Stickern: 9,5 MB Bilder liegen auf der Platte, und wer nie ein Cover ändert,
    /// soll dafür nicht warten.
    /// </summary>
    private void CoverVorlagenSicherstellen()
    {
        if (_coverVorlagenGeladen) return;
        _coverVorlagenGeladen = true;
        CoverVorlagenNeuLaden();
    }

    private void CoverVorlagenNeuLaden()
    {
        CoverVorlagen.Children.Clear();

        foreach (var gruppe in CoverLibrary.Gruppen())
        {
            var raster = new WrapPanel();
            foreach (string datei in gruppe.Dateien)
                raster.Children.Add(CoverKachel(datei));

            CoverVorlagen.Children.Add(new Expander
            {
                Header = gruppe.Name,
                // Die eigene Gruppe steht offen: wer gerade eine Vorlage hinzugefügt hat,
                // will sie sehen und nicht erst suchen.
                IsExpanded = gruppe.Eigene,
                Margin = new Avalonia.Thickness(0, 0, 0, 4),
                Content = new ScrollViewer { MaxHeight = 260, Content = raster },
            });
        }
    }

    /// <summary>
    /// Eine Vorlagenkachel. <b>Eine unlesbare Datei nimmt die Kachel nicht mit herunter</b> —
    /// dieselbe Regel wie bei den Stickern; sie bekommt ein Fragezeichen und bleibt
    /// anklickbar, und der Fehler kommt beim Setzen zur Sprache statt als leere Sammlung.
    /// </summary>
    private Control CoverKachel(string datei)
    {
        Control inhalt;
        try
        {
            using var strom = File.OpenRead(datei);
            inhalt = new Image
            {
                Source = Bitmap.DecodeToWidth(strom, CoverKachelBreite * 2),
                Stretch = Stretch.UniformToFill,
            };
        }
        catch
        {
            inhalt = new TextBlock
            {
                Text = "?",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = this.FindResource("Brush.TextMuted") as IBrush,
            };
        }

        var knopf = new Button
        {
            Width = CoverKachelBreite,
            Height = CoverKachelBreite * 4 / 3,
            Margin = new Avalonia.Thickness(3),
            Padding = new Avalonia.Thickness(2),
            Content = new Border { ClipToBounds = true, CornerRadius = new Avalonia.CornerRadius(3), Child = inhalt },
        };
        knopf.Classes.Add("flach");
        ToolTip.SetTip(knopf, Path.GetFileNameWithoutExtension(datei));
        knopf.Click += (_, _) =>
        {
            if (!CoverBildSetzen(datei))
                App.Platform.Dialogs.Inform(Loc.T("Msg.ImageLoadSimple"), DialogSeverity.Warning);
        };
        return knopf;
    }

    /// <summary>
    /// Eigene Vorlagen aufnehmen — <b>kopieren, nicht verweisen</b>: dieselbe Entscheidung
    /// wie bei den Stickern. Eine Vorlage, die auf eine Datei im Download-Ordner zeigt, ist
    /// nach dem nächsten Aufräumen weg.
    /// </summary>
    private void CoverVorlageHinzufuegen_Click(object? sender, RoutedEventArgs e)
    {
        var dateien = App.Platform.Files.Open(
            Loc.T("Settings.Cover.Add"),
            [new FileFilter(Loc.T("Filter.Images"), Bildsammlung.Endungen)],
            multiple: true);
        if (dateien.Count == 0) return;

        foreach (string quelle in dateien)
        {
            try
            {
                string ziel = Path.Combine(CoverLibrary.UserFolder, Path.GetFileName(quelle));

                // Namensgleiche Datei nicht überschreiben — die alte Vorlage ist vielleicht
                // schon als Cover in Gebrauch.
                int n = 1;
                while (File.Exists(ziel))
                {
                    string stamm = Path.GetFileNameWithoutExtension(quelle);
                    ziel = Path.Combine(CoverLibrary.UserFolder, $"{stamm}_{n++}{Path.GetExtension(quelle)}");
                }

                File.Copy(quelle, ziel);
            }
            catch
            {
                App.Platform.Dialogs.Inform(Loc.T("Msg.ImageLoadSimple"), DialogSeverity.Warning);
            }
        }

        CoverVorlagenNeuLaden();
    }
}
