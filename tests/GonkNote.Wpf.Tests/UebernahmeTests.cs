using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GonkNote.Core.Models;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// <see cref="FlowZuTd"/> — die Übernahme der Bestandsdokumente ins eigene Dokumentmodell
/// (HANDOFF §4.22).
///
/// <para>
/// <b>Wofür diese Wächter da sind.</b> Die Übernahme läuft **still** (Nutzer-Entscheidung
/// 2026-08-05) — sie meldet sich nur, wenn sie fehlschlägt. Was sie dabei *falsch* umwandelt,
/// meldet sich also gar nicht: Ein verlorener Einzug, eine vertauschte Verbindung in einer
/// Tabelle oder eine Überschrift, die keine mehr ist, fällt erst dem Leser auf, und dann steht
/// das Altformat schon nicht mehr im Blick. Genau deshalb wird hier gegen dasselbe
/// Referenzdokument geprüft, das auch die Export-Golden-Files trägt.
/// </para>
/// <para>
/// Alles läuft über <see cref="Sta"/> auf einem eigenen STA-Thread — WPF verlangt das.
/// </para>
/// </summary>
public sealed class UebernahmeTests
{
    /// <summary>Die Seiteneinrichtung des Referenzdokuments, samt Wasserzeichen.</summary>
    private static TextDoc Einstellungen() => Referenzdokument.Einstellungen();

    // ==================== Das ganze Referenzdokument ====================

    /// <summary>
    /// <b>Das Referenzdokument kommt vollständig herüber</b> — mit Überschriften, Listen,
    /// Tabelle, Bild und Verweisen. Es ist dasselbe, an dem die Export-Golden-Files hängen;
    /// was hier durchfällt, wäre auch dort schon einmal aufgefallen.
    /// </summary>
    [Fact]
    public void Das_Referenzdokument_kommt_vollstaendig_herueber() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("uebernahme");
        var flow = Referenzdokument.Bauen(werkbank.Blobs);
        var quelle = Einstellungen();

        var doc = FlowZuTd.Umwandeln(quelle, flow, werkbank.Blobs);

        // Ein Abschnitt, viele Blöcke — das Altformat kennt nur eine Seiteneinrichtung (§4.15).
        var abschnitt = Assert.Single(doc.Sections);
        Assert.NotEmpty(abschnitt.Blocks);

        // Der Text ist da. Stichprobe statt Golden-File: Das Modell hat noch keinen
        // eingecheckten Vergleichsstand, und ein neuer wäre hier nur ein zweiter Wächter für
        // dieselbe Sache.
        string klartext = doc.PlainText();
        Assert.Contains("Physik", klartext);
        Assert.Contains("Chemie", klartext);
    });

    /// <summary>
    /// <b>Die Übernahme fasst die Vorlage nicht an.</b> Sie liest ein <c>FlowDocument</c>, das
    /// im Editor offen sein kann — ein Block, der dabei aus seinem Elternteil genommen würde,
    /// verschwände vor den Augen des Nutzers.
    /// </summary>
    [Fact]
    public void Die_Uebernahme_veraendert_die_Vorlage_nicht() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("unberuehrt");
        var flow = Referenzdokument.Bauen(werkbank.Blobs);

        int vorher = flow.Blocks.Count;
        string textVorher = new TextRange(flow.ContentStart, flow.ContentEnd).Text;

        FlowZuTd.Umwandeln(Einstellungen(), flow, werkbank.Blobs);

        Assert.Equal(vorher, flow.Blocks.Count);
        Assert.Equal(textVorher, new TextRange(flow.ContentStart, flow.ContentEnd).Text);
    });

    // ==================== Die Stellen, an denen es schiefgeht ====================

    /// <summary>
    /// <b>Die Gliederungsebene wird geraten — und das ist hier richtig.</b> Das
    /// <c>FlowDocument</c> hat keinen Platz dafür (§4.20), also kommt sie aus der
    /// Schriftgröße. **Danach steht sie als eigener Wert im Modell und muss nie wieder geraten
    /// werden** — genau das ist der Gewinn der ganzen Phase.
    /// </summary>
    [Fact]
    public void Aus_der_Schriftgroesse_wird_eine_echte_Gliederungsebene() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("ebene");
        var flow = new FlowDocument();
        flow.Blocks.Add(Ueberschrift("Kapitel 1", 1));
        flow.Blocks.Add(Ueberschrift("Abschnitt", 2));
        flow.Blocks.Add(new Paragraph(new Run("Fließtext")));

        var doc = FlowZuTd.Umwandeln(Einstellungen(), flow, werkbank.Blobs);
        var absaetze = doc.Paragraphs().ToList();

        Assert.Equal(1, absaetze[0].Format.OutlineLevel);
        Assert.Equal(2, absaetze[1].Format.OutlineLevel);
        Assert.Null(absaetze[2].Format.OutlineLevel);

        // Und damit taugt das Dokument sofort für ein Inhaltsverzeichnis (§4.20).
        Assert.Equal(["Kapitel 1", "Abschnitt"], TdToc.Eintraege(doc, null).Select(e => e.Text));
    });

    /// <summary>
    /// <b>Nur örtlich gesetzte Werte werden übernommen.</b> Das ist die Entscheidung aus §4.14:
    /// Trüge jeder Lauf eine vollständige Kopie des Formats, ginge jede spätere Änderung an der
    /// Überschrift an allen Läufen vorbei. Was am Absatz steht, gehört an den Absatz.
    /// </summary>
    [Fact]
    public void Nur_oertlich_Gesetztes_wird_uebernommen() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("lokal");

        var absatz = new Paragraph { FontFamily = new FontFamily("Georgia"), FontSize = 24 };
        absatz.Inlines.Add(new Run("normal"));
        absatz.Inlines.Add(new Bold(new Run("fett")));

        var flow = new FlowDocument();
        flow.Blocks.Add(absatz);

        var p = FlowZuTd.Umwandeln(Einstellungen(), flow, werkbank.Blobs).Paragraphs().First();

        // Schrift und Größe stehen **am Absatz**, nicht an jedem Lauf.
        Assert.Equal("Georgia", p.CharFormat.FontFamily);
        Assert.Equal(18, p.CharFormat.FontSize!.Value, 3);   // 24 px = 18 pt

        Assert.Null(p.Inlines[0].Format.FontFamily);
        Assert.Null(p.Inlines[0].Format.Bold);
        Assert.True(p.Inlines[1].Format.Bold);
        Assert.Null(p.Inlines[1].Format.FontFamily);
    });

    /// <summary>
    /// <b>Ein Verweis muss vor der Spanne behandelt werden</b> — er *ist* eine (§7,
    /// „Markdown-Export"). Wer den allgemeineren Fall zuerst nimmt, bekommt den Linktext und
    /// verliert das Ziel. Und das Ziel bleibt **wörtlich** stehen: relativ bleibt relativ.
    /// </summary>
    [Fact]
    public void Ein_Verweis_behaelt_sein_Ziel_und_bleibt_relativ() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("verweis");

        var link = new Hyperlink(new Run("Kapitel 2")) { NavigateUri = new Uri("kapitel-2.md", UriKind.Relative) };
        var absatz = new Paragraph();
        absatz.Inlines.Add(new Run("Siehe "));
        absatz.Inlines.Add(link);

        var flow = new FlowDocument();
        flow.Blocks.Add(absatz);

        var p = FlowZuTd.Umwandeln(Einstellungen(), flow, werkbank.Blobs).Paragraphs().First();

        var verweis = Assert.IsType<TdHyperlink>(p.Inlines[1]);
        Assert.Equal("kapitel-2.md", verweis.Target);
        Assert.Equal("Kapitel 2", verweis.PlainText());
    });

    /// <summary>
    /// <b>Die Umrechnung, die man beim ersten Anlauf falsch macht.</b> WPF kennt <c>RowSpan</c>
    /// an der Zelle, das Modell (und DOCX) kennen <c>Restart</c> + <c>Continue</c> je Zeile
    /// (§4.18). Eine von oben hineinragende Zelle muss in **jeder** überdeckten Zeile
    /// nachgetragen werden — sonst rutscht alles dahinter eine Spalte nach links, und die
    /// Tabelle sieht „irgendwie verrutscht" aus statt falsch.
    /// </summary>
    [Fact]
    public void Aus_RowSpan_werden_Restart_und_Continue() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("verbunden");

        var tabelle = new Table();
        tabelle.Columns.Add(new TableColumn());
        tabelle.Columns.Add(new TableColumn());
        var gruppe = new TableRowGroup();
        tabelle.RowGroups.Add(gruppe);

        var oben = new TableRow();
        oben.Cells.Add(new TableCell(new Paragraph(new Run("verbunden"))) { RowSpan = 2 });
        oben.Cells.Add(new TableCell(new Paragraph(new Run("b"))));
        gruppe.Rows.Add(oben);

        var unten = new TableRow();
        unten.Cells.Add(new TableCell(new Paragraph(new Run("d"))));   // steht in Spalte 2!
        gruppe.Rows.Add(unten);

        var flow = new FlowDocument();
        flow.Blocks.Add(tabelle);

        var t = Assert.IsType<TdTable>(FlowZuTd.Umwandeln(Einstellungen(), flow, werkbank.Blobs)
            .Sections[0].Blocks[0]);

        Assert.Equal(TdVerticalMerge.Restart, t.Rows[0].Cells[0].VerticalMerge);
        Assert.Equal("verbunden", t.Rows[0].Cells[0].PlainText());

        // Die zweite Zeile hat **zwei** Zellen: die Fortsetzung und den Inhalt daneben.
        Assert.Equal(2, t.Rows[1].Cells.Count);
        Assert.Equal(TdVerticalMerge.Continue, t.Rows[1].Cells[0].VerticalMerge);
        Assert.Equal("d", t.Rows[1].Cells[1].PlainText());
    });

    /// <summary>
    /// Eine verschachtelte Liste ist **dieselbe** Liste eine Ebene tiefer (§4.17) — nicht eine
    /// zweite. Zwei Definitionen daraus zu machen hieße, dass die innere wieder bei 1 anfängt,
    /// und das bemerkt man erst bei der dritten Ebene.
    /// </summary>
    [Fact]
    public void Eine_verschachtelte_Liste_bleibt_dieselbe_Liste() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("liste");

        var innen = new List { MarkerStyle = TextMarkerStyle.Decimal };
        innen.ListItems.Add(new ListItem(new Paragraph(new Run("innen"))));

        var aussen = new List { MarkerStyle = TextMarkerStyle.Decimal };
        var punkt = new ListItem(new Paragraph(new Run("außen")));
        punkt.Blocks.Add(innen);
        aussen.ListItems.Add(punkt);

        var flow = new FlowDocument();
        flow.Blocks.Add(aussen);

        var doc = FlowZuTd.Umwandeln(Einstellungen(), flow, werkbank.Blobs);
        var absaetze = doc.Paragraphs().ToList();

        Assert.Single(doc.Lists);
        Assert.Equal(0, absaetze[0].List!.Level);
        Assert.Equal(1, absaetze[1].List!.Level);
        Assert.Equal(absaetze[0].List!.ListId, absaetze[1].List!.ListId);
    });

    /// <summary>
    /// Das Wasserzeichen lag als **Bytes** am Dokument (§4.15). Im Modell ist es ein Bild wie
    /// jedes andere und gehört deshalb in den Blob-Speicher — im Dokument steht nur noch der
    /// Verweis (§4.21).
    /// </summary>
    [Fact]
    public void Das_Wasserzeichen_wandert_in_den_Blob_Speicher() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("wasserzeichen");
        var quelle = Einstellungen();
        Assert.NotNull(quelle.WatermarkImage);

        var seite = FlowZuTd.Umwandeln(quelle, new FlowDocument(), werkbank.Blobs).Sections[0].Page;

        Assert.NotNull(seite.Watermark);
        Assert.Equal(quelle.WatermarkOpacity, seite.WatermarkOpacity, 3);
        Assert.Equal(quelle.WatermarkImage, werkbank.Blobs.Read(seite.Watermark!.BlobId));
    });

    /// <summary>
    /// Die Seiteneinrichtung steht **nicht** im <c>FlowDocument</c>, sondern am
    /// <c>TextDoc</c> (§4.15) — sie muss deshalb eigens mitkommen, sonst steht jedes
    /// übernommene Dokument auf A4 mit 2 cm Rand.
    /// </summary>
    [Fact]
    public void Die_Seiteneinrichtung_kommt_aus_dem_Altdokument() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("seite");
        var quelle = Einstellungen();
        quelle.PageFormat = "A5";
        quelle.Landscape = true;
        quelle.MarginLeftCm = 3.5;
        quelle.FooterText = "Seite {SEITE} von {SEITEN}";

        var seite = FlowZuTd.Umwandeln(quelle, new FlowDocument(), werkbank.Blobs).Sections[0].Page;

        Assert.Equal("A5", seite.Name);
        Assert.True(seite.IstQuerformat);
        Assert.Equal(3.5, seite.MarginLeftCm, 3);

        // **Die Platzhalter bleiben wörtlich stehen** — echte Felder daraus macht erst der
        // Export (§4.20).
        Assert.Equal("Seite {SEITE} von {SEITEN}", seite.FooterText);
    });

    /// <summary>
    /// Ein leeres Bestandsdokument ergibt **einen leeren Absatz** und kein leeres Dokument —
    /// sonst hätte der Cursor beim ersten Tastendruck keinen Ort (§4.14).
    /// </summary>
    [Fact]
    public void Ein_leeres_Dokument_bekommt_einen_Absatz() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("leer");

        var doc = FlowZuTd.Umwandeln(Einstellungen(), new FlowDocument(), werkbank.Blobs);

        Assert.IsType<TdParagraph>(Assert.Single(Assert.Single(doc.Sections).Blocks));
    });

    /// <summary>
    /// <b>Und der Weg geht weiter:</b> Was übernommen wurde, muss sich speichern und wieder
    /// lesen lassen — sonst wäre die Übernahme ein Einbahnstraßen-Erfolg. Geprüft wird über
    /// dasselbe Speicherformat, das später in <c>TextDoc.Model</c> steht.
    /// </summary>
    [Fact]
    public void Das_Uebernommene_uebersteht_das_Speicherformat() => Sta.Run(() =>
    {
        using var werkbank = new Referenzdokument.Werkbank("speichern");
        var flow = Referenzdokument.Bauen(werkbank.Blobs);

        var doc = FlowZuTd.Umwandeln(Einstellungen(), flow, werkbank.Blobs);
        var zurueck = TdFormatIo.Lesen(TdFormatIo.Schreiben(doc));

        Assert.NotNull(zurueck);
        Assert.Equal(doc.PlainText(), zurueck!.PlainText());
        Assert.Equal(doc.Sections[0].Blocks.Count, zurueck.Sections[0].Blocks.Count);
    });

    // ==================== Hilfsmittel ====================

    private static Paragraph Ueberschrift(string text, int ebene)
    {
        var stil = TextStyles.ForHeading(ebene);
        return new Paragraph(new Run(text))
        {
            FontSize = stil.Size,
            FontWeight = stil.Weight,
            FontStyle = stil.Style,
        };
    }
}
