using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GonkNote.Core.Models;
using GonkNote.Core.Services;
using GonkNote.Core.Text;

// `TextElement` heißt in beiden Welten so — hier ist immer WPFs gemeint (in
// GonkNote.Core.Models ist es ein Whiteboard-Element).
using TextElement = System.Windows.Documents.TextElement;

namespace GonkNote.Services;

/// <summary>
/// Die Übernahme: aus einem <see cref="FlowDocument"/> wird ein <see cref="TdDocument"/>.
///
/// <para>
/// <b>Warum das im WPF-Kopf steht und nicht in Core</b> — dieselbe Begründung wie bei §4.1:
/// <c>FlowDocument</c> ist <c>System.Windows.Documents</c>. Und warum die Übernahme
/// **überhaupt** nur hier laufen darf: RTF und XamlPackage liest ausschließlich
/// <c>TextRange.Load</c>. Der Linux-Kopf kann eine Altdatei nicht übernehmen und darf es auch
/// nicht versuchen — er würde ein leeres Dokument erzeugen und hätte den Inhalt damit
/// scheinbar gelöscht.
/// </para>
///
/// <para>
/// <b>Sie ist die eine Stelle, an der Raten richtig ist.</b> Das <c>FlowDocument</c> hat
/// keinen Platz für eine Gliederungsebene (§4.20) — sie wird aus der Schriftgröße
/// zurückerkannt (<see cref="TextStyles.HeadingLevel"/>), genau wie der frühere
/// Markdown-Export es tat. **Danach steht sie als eigener Wert im Modell und muss nie wieder
/// geraten werden.** Das ist der Unterschied zwischen einer Übernahme und einem Format.
/// </para>
/// </summary>
public static class FlowZuTd
{
    // WPF rechnet in geräteunabhängigen Pixeln: 96 auf ein Zoll.
    private const double CmProPixel = 2.54 / 96.0;
    private const double PunktProPixel = 72.0 / 96.0;

    /// <summary>
    /// <b>Auf zehn Nanometer gerundet — und das ist keine Kosmetik</b> (HANDOFF §4.45).
    ///
    /// <para>
    /// Jedes Maß läuft auf dem Weg ins Altformat und zurück durch Zentimeter → Pixel →
    /// Zentimeter, und diese Rechnung trifft sich nicht immer wieder selbst: aus 4 wird
    /// 3,9999999999999996, daraus beim nächsten Mal etwas anderes. <b>Solange <c>Rtf</c>
    /// führte, war das Rauschen folgenlos</b> — mit Schritt 7 läuft die Rundreise bei
    /// <i>jedem</i> Speichern, und dann wandert eine Spaltenbreite über Wochen langsam davon
    /// und ein Wächter auf Gleichheit kann nie grün sein.
    /// </para>
    /// <para>
    /// <b>Sechs Nachkommastellen in Zentimetern sind zehn Nanometer.</b> Das ist rund ein
    /// Zehntausendstel dessen, was der feinste Drucker auflöst — es geht also nichts verloren,
    /// was jemals jemand sehen könnte, und die Rundreise wird zur Wiederholung ihrer selbst.
    /// </para>
    /// </summary>
    private static double Cm(double px) => Math.Round(px * CmProPixel, 6);

    /// <inheritdoc cref="Cm"/>
    private static double Pt(double px) => Math.Round(px * PunktProPixel, 6);

    /// <summary>
    /// Wandelt Inhalt **und** Seiteneinrichtung um.
    /// </summary>
    /// <param name="quelle">
    /// Das Bestandsdokument — es liefert die Seiteneinrichtung, die im <c>FlowDocument</c>
    /// gar nicht steht (§4.15).
    /// </param>
    public static TdDocument Umwandeln(TextDoc quelle, FlowDocument flow, BlobStore blobs)
    {
        var doc = new TdDocument();

        // **Die Grundschrift des Dokuments gehört ins Dokument** und nicht in jeden Absatz: Im
        // FlowDocument steht sie ganz oben (Schrift, Größe, Tinte), im Modell in
        // `DefaultCharFormat` — und von dort in die `docDefaults` der DOCX-Datei. Ohne diesen
        // Schritt bekäme jedes übernommene Dokument die Vorgabe des Modells (schwarz, 11 pt)
        // statt seiner eigenen Tinte, und zwar still.
        doc.DefaultCharFormat = GrundschriftVon(flow).Over(doc.DefaultCharFormat);

        var abschnitt = new TdSection { Page = SeiteUmwandeln(quelle, blobs) };

        var zustand = new Zustand(doc, blobs);
        BloeckeUmwandeln(flow.Blocks, abschnitt.Blocks, zustand);

        // Ein leeres Dokument ist nicht leer, sondern hat einen Absatz — sonst hätte der
        // Cursor beim ersten Tastendruck keinen Ort (§4.14).
        if (abschnitt.Blocks.Count == 0) abschnitt.Blocks.Add(new TdParagraph());

        doc.Sections.Add(abschnitt);
        return doc;
    }

    /// <summary>Was über den ganzen Durchlauf gilt: Listenkennungen und der Bildspeicher.</summary>
    private sealed class Zustand(TdDocument doc, BlobStore blobs)
    {
        public TdDocument Doc { get; } = doc;
        public BlobStore Blobs { get; } = blobs;

        /// <summary>Die Liste, in der wir gerade stecken — <c>null</c> außerhalb.</summary>
        public TdListRef? Liste { get; set; }

        /// <summary>
        /// Stehen wir gerade in den generierten Einträgen eines Inhaltsverzeichnisses?
        /// <para>
        /// Sie werden übersprungen, weil an ihrer Stelle das <see cref="TdField"/> steht — siehe
        /// <see cref="VerzeichnisErkannt"/>.
        /// </para>
        /// </summary>
        public bool ImVerzeichnis { get; set; }
    }

    // ==================== Seiteneinrichtung ====================

    private static TdPageSetup SeiteUmwandeln(TextDoc quelle, BlobStore blobs)
    {
        var seite = quelle.PageFormat switch
        {
            "A5" => TdPageSetup.A5,
            "A3" => TdPageSetup.A3,
            "Letter" => TdPageSetup.Letter,
            _ => TdPageSetup.A4,
        };
        if (quelle.Landscape) seite.Quer();

        seite.MarginLeftCm = quelle.MarginLeftCm;
        seite.MarginTopCm = quelle.MarginTopCm;
        seite.MarginRightCm = quelle.MarginRightCm;
        seite.MarginBottomCm = quelle.MarginBottomCm;

        // **Die Platzhalter bleiben wörtlich stehen.** `{SEITE}` heißt im Modell dasselbe wie
        // im Altformat — die Zuordnung zu echten Feldern macht erst der Export (§4.20).
        seite.HeaderText = quelle.HeaderText;
        seite.FooterText = quelle.FooterText;
        seite.SuppressOnFirstPage = quelle.SuppressHeaderOnFirstPage;

        // Das Wasserzeichen lag als **Bytes** am Dokument (§4.15). Im Modell ist es ein Bild
        // wie jedes andere und gehört deshalb in den Blob-Speicher.
        if (quelle.WatermarkImage is { Length: > 0 } bytes)
        {
            seite.Watermark = new TdImage(blobs.Put(bytes), "png", seite.WidthCm, seite.HeightCm);
            seite.WatermarkOpacity = quelle.WatermarkOpacity;
        }

        return seite;
    }

    // ==================== Blöcke ====================

    private static void BloeckeUmwandeln(BlockCollection blocks, List<TdBlock> ziel, Zustand zustand)
    {
        foreach (var block in blocks) BlockUmwandeln(block, ziel, zustand);
    }

    /// <summary>
    /// <b>Die Umwandlung fasst die Vorlage nicht an.</b> Sie liest ein <c>FlowDocument</c>, das
    /// im Editor offen sein kann — ein Block, der dabei aus seinem Elternteil genommen würde,
    /// verschwände vor den Augen des Nutzers.
    /// </summary>
    private static void BlockUmwandeln(Block block, List<TdBlock> ziel, Zustand zustand)
    {
        switch (block)
        {
            case Paragraph p:
            {
                // **Ein generierter Verzeichniseintrag ist kein Absatz, sondern das Ergebnis
                // eines Feldes** (§4.20). Er wird übersprungen; an seiner Stelle steht das Feld,
                // das gleich hinter dem Titel angelegt wurde. Bliebe er stehen, hätte das
                // Dokument das Verzeichnis zweimal — einmal als Feld und einmal als Text, der
                // beim nächsten Öffnen veraltet ist.
                if (zustand.ImVerzeichnis && TextStyles.IsTocEntry(p)) break;
                zustand.ImVerzeichnis = false;

                ziel.Add(AbsatzUmwandeln(p, zustand));

                if (VerzeichnisErkannt(p))
                {
                    // **Vier Ebenen, nicht die üblichen drei** (`TdToc.EbeneBisStandard`): So
                    // viele Überschriftvorlagen hat der Editor, und genau so viele sammelt er
                    // in sein Verzeichnis ein (`TextStyles.CollectHeadings`). Bliebe es bei
                    // drei, verschwände jede „Überschrift 4" aus dem Verzeichnis — im Editor
                    // sichtbar, im Export nicht.
                    ziel.Add(new TdParagraph([
                        new TdField(TdFieldKind.TableOfContents)
                        {
                            Argument = TdToc.Ebenenangabe(TdToc.EbeneVonStandard, VorlagenEbenen),
                        },
                    ]));
                    zustand.ImVerzeichnis = true;
                }
                break;
            }

            case Section s:
                // Ein `Section` im FlowDocument ist eine bloße Klammer und keine
                // Seiteneinrichtung — es gibt im Altformat nur **eine** davon (§4.15).
                BloeckeUmwandeln(s.Blocks, ziel, zustand);
                break;

            case List liste:
                zustand.ImVerzeichnis = false;
                ListeUmwandeln(liste, ziel, zustand, ebene: 0);
                break;

            case Table tabelle:
                zustand.ImVerzeichnis = false;
                ziel.Add(TabelleUmwandeln(tabelle, zustand));
                break;

            case BlockUIContainer behaelter:
                zustand.ImVerzeichnis = false;
                // Ein Bild auf Blockebene wird ein Absatz, der nichts als dieses Bild
                // enthält — genau die Form, in der DOCX es kennt (§4.21).
                if (GrafikAus(behaelter.Child, zustand) is { } grafik)
                {
                    ziel.Add(new TdParagraph([grafik]));
                    break;
                }

                // **Alles andere in einem Blockbehälter ist die Trennlinie** („Einfügen →
                // Trennlinie" legt dort einen 2 px hohen Rahmen ab). Sie wird ein leerer
                // Absatz mit einer Unterlinie — genau die Form, in der DOCX sie seit jeher
                // kennt. Vorher fiel sie hier still heraus, und ein fehlender Strich sieht
                // nicht nach einem Fehler aus, sondern nach einem Dokument ohne Strich.
                ziel.Add(new TdParagraph { Format = { BottomBorder = LinieAus(behaelter.Child) } });
                break;
        }
    }

    /// <summary>
    /// Ist dieser Absatz die Überschrift eines vom Editor erzeugten Inhaltsverzeichnisses?
    /// <para>
    /// <b>Erkannt wird am Text und an der Größe</b> — der heutige Editor hat keinen anderen
    /// Marker, und genau so hat es der alte DOCX-Export auch gemacht. Das ist die letzte Stelle,
    /// an der geraten wird: Ab hier steht das Verzeichnis als <see cref="TdField"/> im Modell
    /// und muss nie wieder erkannt werden (§4.20).
    /// </para>
    /// </summary>
    /// <summary>So viele Überschriftvorlagen kennt der Editor (<c>TextStyles.All</c>).</summary>
    private static readonly int VorlagenEbenen = TextStyles.All.Max(s => s.HeadingLevel);

    private static bool VerzeichnisErkannt(Paragraph p) =>
        new TextRange(p.ContentStart, p.ContentEnd).Text.Trim() == TextStyles.TocTitle &&
        p.FontSize >= 20;

    private static TdParagraph AbsatzUmwandeln(Paragraph p, Zustand zustand)
    {
        var absatz = new TdParagraph
        {
            Format = AbsatzformatVon(p),
            CharFormat = ZeichenformatVon(p),
            List = zustand.Liste is { } l ? new TdListRef(l.ListId, l.Level) : null,
        };

        StueckeUmwandeln(p.Inlines, absatz.Inlines, zustand);
        return absatz;
    }

    private static TdParaFormat AbsatzformatVon(Paragraph p)
    {
        var f = new TdParaFormat();

        // **Die Ausrichtung wird als einzige nicht örtlich gelesen, sondern wirksam.** Ein
        // `FlowDocument` steht von Haus aus auf `Justify` — anders als jede andere Eigenschaft
        // ist der Vorgabewert hier also nicht der, den das Modell annimmt (`Left`). Wer nur
        // `ReadLocalValue` fragt, übernimmt ein durchgehend im Blocksatz gesetztes Dokument als
        // linksbündiges: kein Absturz, kein Testfehler — nur ein Export, der anders aussieht
        // als der Bildschirm.
        f.Alignment = p.TextAlignment switch
        {
            TextAlignment.Center => TdAlign.Center,
            TextAlignment.Right => TdAlign.Right,
            TextAlignment.Justify => TdAlign.Justify,
            _ => TdAlign.Left,
        };

        if (Lokal(p, Block.MarginProperty) is Thickness rand)
        {
            f.LeftIndentCm = Cm(rand.Left);
            f.RightIndentCm = Cm(rand.Right);
            f.SpaceBeforePt = Pt(rand.Top);
            f.SpaceAfterPt = Pt(rand.Bottom);
        }

        if (Lokal(p, Paragraph.TextIndentProperty) is double einzug) f.FirstLineIndentCm = Cm(einzug);

        // Weiter unten steht der Rest; abgeräumt wird erst ganz am Ende, wenn alles beisammen
        // ist — siehe `AufAbweichungenKuerzen` am Ende dieser Methode.

        // Der Editor setzt den Zeilenabstand als `FontSize * Faktor` (TextEditorView.Format) —
        // die Rechnung geht deshalb glatt zurück und muss nicht geschätzt werden.
        if (Lokal(p, Block.LineHeightProperty) is double hoehe && !double.IsNaN(hoehe) && p.FontSize > 0)
            f.LineSpacing = hoehe / p.FontSize;

        if (Lokal(p, Paragraph.KeepWithNextProperty) is bool halten) f.KeepWithNext = halten;
        if (Lokal(p, Paragraph.BreakPageBeforeProperty) is bool umbruch) f.PageBreakBefore = umbruch;

        // Ein Absatz **mit** Unterlinie: so kommt eine Trennlinie zurück, die einmal durch das
        // eigene Modell gelaufen ist (TdZuFlow legt sie als Rahmen am Absatz ab, nicht als
        // Blockbehälter). Der Behälter des heutigen Editors wird in BlockUmwandeln behandelt.
        if (Lokal(p, Block.BorderThicknessProperty) is Thickness dicke && dicke.Bottom > 0)
            f.BottomBorder = new TdBorder(
                Pt(dicke.Bottom),
                Lokal(p, Block.BorderBrushProperty) is SolidColorBrush pinsel
                    ? Hex(pinsel.Color)
                    : LinienfarbeStandard);

        // **Hier wird geraten, und hier ist es richtig:** Das FlowDocument hat keinen Platz für
        // eine Gliederungsebene, also wird sie aus der Schriftgröße zurückerkannt — danach
        // steht sie im Modell und muss nie wieder geraten werden (§4.20).
        int ebene = TextStyles.HeadingLevel(p);
        if (ebene > 0)
        {
            f.OutlineLevel = ebene;
        }
        else if (TitelartigerAbsatz(p))
        {
            // Titel und die Zeile „Inhaltsverzeichnis": Überschrift ja, Verzeichniseintrag
            // nein (§4.23). `TextStyles.HeadingLevel` gibt für beide bewusst 0 zurück — es
            // beantwortet die Frage „gehört das ins Verzeichnis?" und nicht „ist das eine
            // Überschrift?". Für den Export sind das zwei verschiedene Fragen.
            f.OutlineLevel = 1;
            f.ExcludeFromToc = true;
        }

        return AufAbweichungenKuerzen(f);
    }

    /// <summary>
    /// <b>Was dem aufgelösten Standard entspricht, wird wieder <c>null</c></b> — die genaue
    /// Umkehrung von <c>TdZuFlow.AbsatzformatSetzen</c> (HANDOFF §4.45).
    ///
    /// <para>
    /// <b>Warum das nötig ist:</b> Seit der Weg nach WPF die Kaskade <i>auflöst</i>, trägt dort
    /// jeder Absatz einen örtlichen Wert — auch der, an dem im Modell nie etwas stand. Ohne
    /// diesen Schritt käme jeder davon als ausdrücklich gesetzt zurück, und aus „nicht gesetzt"
    /// (§4.14) würde bei der ersten Rundreise eine festgeschriebene Zahl. Der Absatz sähe danach
    /// gleich aus und verlöre trotzdem etwas: Er folgte einer späteren Änderung des
    /// Dokumentstandards nicht mehr.
    /// </para>
    /// <para>
    /// <b>Es ist kein Sparen an Bytes, sondern das Wiederherstellen einer Bedeutung.</b>
    /// Deshalb steht es hier und nicht im Serialisierer: Der weiß nicht, ob eine 8 dasteht,
    /// weil jemand sie gesetzt hat.
    /// </para>
    /// </summary>
    private static TdParaFormat AufAbweichungenKuerzen(TdParaFormat f)
    {
        var norm = TdParaFormat.Standard;

        if (f.Alignment == norm.Alignment) f.Alignment = null;
        if (Gleich(f.LeftIndentCm, norm.LeftIndentCm)) f.LeftIndentCm = null;
        if (Gleich(f.RightIndentCm, norm.RightIndentCm)) f.RightIndentCm = null;
        if (Gleich(f.FirstLineIndentCm, norm.FirstLineIndentCm)) f.FirstLineIndentCm = null;
        if (Gleich(f.SpaceBeforePt, norm.SpaceBeforePt)) f.SpaceBeforePt = null;
        if (Gleich(f.SpaceAfterPt, norm.SpaceAfterPt)) f.SpaceAfterPt = null;
        if (Gleich(f.LineSpacing, norm.LineSpacing)) f.LineSpacing = null;
        if (f.OutlineLevel == norm.OutlineLevel) f.OutlineLevel = null;
        if (f.ExcludeFromToc == norm.ExcludeFromToc) f.ExcludeFromToc = null;
        if (f.KeepWithNext == norm.KeepWithNext) f.KeepWithNext = null;
        if (f.PageBreakBefore == norm.PageBreakBefore) f.PageBreakBefore = null;
        if (f.BottomBorder is { Sichtbar: false }) f.BottomBorder = null;

        return f;
    }

    /// <summary>
    /// Zwei Maße gelten als gleich, wenn sie es <b>auf dem Papier</b> sind.
    ///
    /// <para>
    /// <b>Ein genauer Vergleich wäre hier falsch, nicht nur streng.</b> Jeder Wert läuft über
    /// die Umrechnung Zentimeter → Pixel → Zentimeter, und die trifft sich nicht immer wieder
    /// selbst: aus 4 wird 3,9999999999999996. Wer darauf mit <c>==</c> prüft, bekommt bei jeder
    /// Rundreise eine neue Abweichung, die keine ist — und das Modell wächst um Werte, die
    /// niemand gesetzt hat. Die Schranke ist bewusst grob: 1/1000 Punkt ist rund ein
    /// Hundertstel Haaresbreite.
    /// </para>
    /// </summary>
    private static bool Gleich(double? a, double? b) =>
        a is { } x && b is { } y && Math.Abs(x - y) < 0.001;

    /// <summary>
    /// Ein Absatz, der als oberste Überschrift gesetzt wird, ohne im Verzeichnis zu stehen:
    /// der <b>Titel</b> (Vorlage „Titel") und die Zeile <b>„Inhaltsverzeichnis"</b>. Beide
    /// erkennt man im Altformat nur an Text und Größe — es ist dieselbe Stelle, an der schon
    /// die Gliederungsebene geraten wird, und ab hier steht beides im Modell.
    /// </summary>
    private static bool TitelartigerAbsatz(Paragraph p)
    {
        string text = new TextRange(p.ContentStart, p.ContentEnd).Text.Trim();
        if (text.Length == 0) return false;

        if (text == TextStyles.TocTitle) return p.FontSize >= 20;

        var titel = TextStyles.All.First(s => s.Name == "Titel");
        return Math.Abs(p.FontSize - titel.Size) < 0.6;
    }

    // ==================== Listen ====================

    private static void ListeUmwandeln(List liste, List<TdBlock> ziel, Zustand zustand, int ebene)
    {
        // Eine verschachtelte Liste ist **dieselbe** Liste eine Ebene tiefer — im Modell wie in
        // DOCX (§4.17). Nur die äußerste legt eine Definition an.
        int id;
        if (zustand.Liste is { } laufend)
        {
            id = laufend.ListId;
        }
        else
        {
            id = zustand.Doc.NextListId();
            zustand.Doc.Lists.Add(DefinitionFuer(liste, id));
        }

        var vorher = zustand.Liste;

        foreach (var punkt in liste.ListItems)
        {
            zustand.Liste = new TdListRef(id, ebene);

            foreach (var block in punkt.Blocks)
            {
                if (block is List innen)
                {
                    ListeUmwandeln(innen, ziel, zustand, ebene + 1);
                    continue;
                }

                BlockUmwandeln(block, ziel, zustand);
            }
        }

        zustand.Liste = vorher;
    }

    /// <summary>
    /// Neun Ebenen, alle mit derselben Markenart. Das Altformat kennt je Liste **eine**
    /// (<c>MarkerStyle</c>) — mehr lässt sich nicht herausholen, und mehr zu erfinden hieße,
    /// dem Dokument etwas anzudichten.
    /// </summary>
    private static TdListDefinition DefinitionFuer(List liste, int id)
    {
        bool nummeriert = liste.MarkerStyle is
            TextMarkerStyle.Decimal or TextMarkerStyle.LowerLatin or TextMarkerStyle.UpperLatin
            or TextMarkerStyle.LowerRoman or TextMarkerStyle.UpperRoman;

        var definition = nummeriert ? TdListDefinition.Nummern(id) : TdListDefinition.Punkte(id);

        if (nummeriert)
        {
            var art = liste.MarkerStyle switch
            {
                TextMarkerStyle.LowerLatin => TdListMarker.LowerLetter,
                TextMarkerStyle.UpperLatin => TdListMarker.UpperLetter,
                TextMarkerStyle.LowerRoman => TdListMarker.LowerRoman,
                TextMarkerStyle.UpperRoman => TdListMarker.UpperRoman,
                _ => TdListMarker.Decimal,
            };
            foreach (var stufe in definition.Levels) stufe.Marker = art;
        }

        return definition;
    }

    // ==================== Tabellen ====================

    /// <summary>
    /// Eine Tabelle. <b>Die Umrechnung, die man beim ersten Anlauf falsch macht:</b> WPF kennt
    /// <c>RowSpan</c> an der Zelle, das Modell (und DOCX) kennen <c>Restart</c> +
    /// <c>Continue</c> je Zeile (§4.18). Eine von oben hineinragende Zelle muss deshalb in
    /// **jeder** überdeckten Zeile als Fortsetzung nachgetragen werden — sonst rutscht alles
    /// dahinter eine Spalte nach links.
    /// </summary>
    private static TdTable TabelleUmwandeln(Table tabelle, Zustand zustand)
    {
        var t = new TdTable();

        foreach (var spalte in tabelle.Columns)
            t.ColumnWidthsCm.Add(spalte.Width.IsAbsolute ? Cm(spalte.Width.Value) : 0);

        RahmenUebernehmen(tabelle, t.Format);
        ZellabstandUebernehmen(tabelle, t.Format);

        // Je Spalte: wie viele Zeilen ragt eine Verbindung von oben noch herein?
        var offen = new Dictionary<int, int>();

        foreach (var gruppe in tabelle.RowGroups)
        {
            foreach (var zeile in gruppe.Rows)
            {
                var z = new TdTableRow();
                int spalte = 0;

                foreach (var zelle in zeile.Cells)
                {
                    // Erst alles nachtragen, was von oben hereinragt.
                    while (offen.TryGetValue(spalte, out int rest) && rest > 0)
                    {
                        z.Cells.Add(new TdTableCell { VerticalMerge = TdVerticalMerge.Continue });
                        offen[spalte] = rest - 1;
                        spalte++;
                    }

                    var neu = new TdTableCell { ColumnSpan = Math.Max(1, zelle.ColumnSpan) };
                    if (zelle.RowSpan > 1)
                    {
                        neu.VerticalMerge = TdVerticalMerge.Restart;
                        offen[spalte] = zelle.RowSpan - 1;
                    }

                    if (Lokal(zelle, TableCell.BackgroundProperty) is SolidColorBrush hintergrund)
                        neu.Shading = Hex(hintergrund.Color);

                    BloeckeUmwandeln(zelle.Blocks, neu.Blocks, zustand);
                    z.Cells.Add(neu);
                    spalte += neu.ColumnSpan;
                }

                // Und was hinter der letzten Zelle noch offen ist.
                while (offen.TryGetValue(spalte, out int rest) && rest > 0)
                {
                    z.Cells.Add(new TdTableCell { VerticalMerge = TdVerticalMerge.Continue });
                    offen[spalte] = rest - 1;
                    spalte++;
                }

                t.Rows.Add(z);
            }
        }

        return t;
    }

    /// <summary>
    /// Die Linien einer Tabelle. <b>WPF führt sie an jeder Zelle, das Modell und DOCX an der
    /// Tabelle</b> (§4.18) — gelesen wird deshalb die **erste** Zelle, denn der Editor setzt
    /// überall dieselbe Linie (<c>TextEditorView.Table</c>). Hat die Tabelle keine Zellen oder
    /// keine Linie, bleibt der Standard des Modells stehen.
    /// </summary>
    private static void RahmenUebernehmen(Table tabelle, TdTableFormat format)
    {
        var erste = tabelle.RowGroups.SelectMany(g => g.Rows).SelectMany(z => z.Cells).FirstOrDefault();
        if (erste is null) return;

        double staerke = erste.BorderThickness.Bottom;
        if (staerke <= 0) staerke = tabelle.BorderThickness.Bottom;
        if (staerke <= 0) return;

        var pinsel = erste.BorderBrush as SolidColorBrush ?? tabelle.BorderBrush as SolidColorBrush;
        var linie = new TdBorder(Pt(staerke), pinsel is null ? LinienfarbeStandard : Hex(pinsel.Color));

        format.Top = format.Left = format.Bottom = format.Right = linie;
        format.InsideH = format.InsideV = linie;
    }

    /// <summary>
    /// Der Innenabstand der Zellen — im Altformat das <c>Padding</c> jeder einzelnen, im Modell
    /// eine Angabe für die ganze Tabelle. Dieselbe Umrechnung wie bei den Linien und aus
    /// demselben Grund.
    /// </summary>
    private static void ZellabstandUebernehmen(Table tabelle, TdTableFormat format)
    {
        var erste = tabelle.RowGroups.SelectMany(g => g.Rows).SelectMany(z => z.Cells).FirstOrDefault();
        if (erste is null) return;

        format.CellPaddingLeftCm = Cm(erste.Padding.Left);
        format.CellPaddingRightCm = Cm(erste.Padding.Right);
        format.CellPaddingTopCm = Cm(erste.Padding.Top);
        format.CellPaddingBottomCm = Cm(erste.Padding.Bottom);
    }

    // ==================== Textstücke ====================

    private static void StueckeUmwandeln(InlineCollection inlines, List<TdInline> ziel, Zustand zustand) =>
        StueckeUmwandeln(inlines, ziel, zustand, new TdCharFormat());

    /// <summary>
    /// <paramref name="geerbt"/> sammelt, was die **Spannen** auf dem Weg gesetzt haben — nicht,
    /// was am Absatz steht.
    /// <para>
    /// Der Unterschied ist die Entscheidung aus §4.14: Trüge jeder Lauf eine vollständige Kopie
    /// des Formats, ginge jede spätere Änderung an der Überschrift an allen Läufen vorbei. Was
    /// am Absatz steht, gehört an den Absatz.
    /// </para>
    /// </summary>
    private static void StueckeUmwandeln(
        InlineCollection inlines, List<TdInline> ziel, Zustand zustand, TdCharFormat geerbt)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                // **Der Verweis muss vor der Spanne stehen** — er *ist* eine (§7). Wer den
                // allgemeineren Fall zuerst behandelt, verliert das Ziel.
                case Hyperlink link:
                {
                    var verweis = new TdHyperlink
                    {
                        // OriginalString und nicht AbsoluteUri: ein relatives Ziel bleibt
                        // relativ (§7, „Markdown-Export").
                        Target = link.NavigateUri?.OriginalString ?? "",
                        Format = ZeichenformatVon(link).Over(geerbt),
                    };
                    StueckeUmwandeln(link.Inlines, verweis.Inlines, zustand, new TdCharFormat());
                    if (verweis.Inlines.Count > 0) ziel.Add(verweis);
                    break;
                }

                case Span span:
                    StueckeUmwandeln(span.Inlines, ziel, zustand, ZeichenformatVon(span).Over(geerbt));
                    break;

                case Run lauf when lauf.Text.Length > 0:
                    ziel.Add(new TdRun(lauf.Text, ZeichenformatVon(lauf).Over(geerbt)));
                    break;

                case LineBreak:
                    ziel.Add(new TdLineBreak { Format = geerbt.Kopie() });
                    break;

                case InlineUIContainer behaelter:
                    if (GrafikAus(behaelter.Child, zustand) is { } grafik)
                    {
                        grafik.Format = geerbt.Kopie();
                        ziel.Add(grafik);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Ein Bild aus einem UI-Behälter.
    /// <para>
    /// <b>Ein Diagramm kommt hier als Bild an, und das ist kein Fehler dieser Umwandlung,
    /// sondern der Befund aus §4.21:</b> Der heutige Editor rendert es beim Einfügen zu einer
    /// Bitmap und wirft die Zahlen weg. Aus einem Pixelbild lassen sie sich nicht
    /// zurückholen — **was die Übernahme retten kann, ist das Bild.**
    /// </para>
    /// </summary>
    private static TdGraphic? GrafikAus(UIElement? element, Zustand zustand)
    {
        if (element is not Image bild || bild.Source is not System.Windows.Media.Imaging.BitmapSource quelle)
            return null;

        // Ohne Verweis liegt das Bild noch im Dokument — dann wandert es jetzt in den
        // Blob-Speicher, genau wie beim Speichern (DocumentImages.Adopt).
        if (DocumentImages.Adopt(bild, BlobStore.Current!) is not { } verweis) return null;

        double breite = double.IsNaN(bild.Width) ? quelle.PixelWidth : bild.Width;
        double hoehe = double.IsNaN(bild.Height)
            ? (double.IsNaN(bild.Width) ? quelle.PixelHeight : bild.Width * quelle.PixelHeight / quelle.PixelWidth)
            : bild.Height;

        return new TdImage(verweis.Id, verweis.Extension, Cm(breite), Cm(hoehe));
    }

    /// <summary>
    /// Die Farbe einer Trennlinie, wenn das Element keine nennt. **Derselbe Wert, den der
    /// heutige DOCX-Export einträgt** (<c>AAAAAA</c>) — eine Trennlinie ohne Farbe gibt es
    /// nicht, und Schwarz wäre ein sichtbar anderer Strich.
    /// </summary>
    private const string LinienfarbeStandard = "#AAAAAA";

    /// <summary>
    /// Die Linie hinter einem Blockbehälter. Der Editor legt dort einen <c>Border</c> ab; seine
    /// Höhe ist die Stärke, seine Füllung die Farbe. Steht dort etwas anderes — ein Element,
    /// das dieses Modell nicht kennt —, bleibt die übliche Trennlinie übrig: **eine Linie ist
    /// besser als eine stille Lücke.**
    /// </summary>
    private static TdBorder LinieAus(UIElement? element)
    {
        double staerke = 0.75;
        string farbe = LinienfarbeStandard;

        if (element is System.Windows.Controls.Border rahmen)
        {
            if (!double.IsNaN(rahmen.Height) && rahmen.Height > 0) staerke = Pt(rahmen.Height);
            if (rahmen.Background is SolidColorBrush pinsel) farbe = Hex(pinsel.Color);
            else if (rahmen.BorderBrush is SolidColorBrush rand) farbe = Hex(rand.Color);
        }

        return new TdBorder(staerke, farbe);
    }

    // ==================== Formate ====================

    /// <summary>
    /// Die Grundschrift des ganzen Dokuments. <c>FlowDocument</c> ist kein
    /// <c>TextElement</c> — es trägt dieselben Eigenschaften, steht aber neben der Erbfolge und
    /// nicht darin, deshalb hier eigens gelesen. Mehr als diese drei gibt es dort nicht zu
    /// holen: fett oder kursiv setzt der Editor nie am Dokument selbst.
    /// </summary>
    private static TdCharFormat GrundschriftVon(FlowDocument flow)
    {
        var f = new TdCharFormat();

        if (Lokal(flow, TextElement.FontFamilyProperty) is FontFamily schrift) f.FontFamily = schrift.Source;
        if (Lokal(flow, TextElement.FontSizeProperty) is double groesse) f.FontSize = Pt(groesse);
        if (Lokal(flow, TextElement.ForegroundProperty) is SolidColorBrush tinte) f.Color = Hex(tinte.Color);

        return f;
    }

    /// <summary>
    /// Das Zeichenformat eines Elements — **nur seine örtlich gesetzten Werte**.
    /// <para>
    /// <c>ReadLocalValue</c> ist hier genau das richtige Werkzeug: Es unterscheidet „hier steht
    /// nichts" von „hier steht der Vorgabewert" — und das ist dieselbe Unterscheidung, auf der
    /// das ganze Modell steht (§4.14, <c>null</c> heißt „nicht gesetzt"). Ein <c>FontSize</c>
    /// abzufragen gäbe immer eine Zahl, auch wenn sie nur geerbt ist.
    /// </para>
    /// </summary>
    private static TdCharFormat ZeichenformatVon(TextElement e)
    {
        var f = new TdCharFormat();

        if (Lokal(e, TextElement.FontFamilyProperty) is FontFamily schrift) f.FontFamily = schrift.Source;
        if (Lokal(e, TextElement.FontSizeProperty) is double groesse) f.FontSize = Pt(groesse);
        if (Lokal(e, TextElement.FontWeightProperty) is FontWeight gewicht) f.Bold = gewicht.ToOpenTypeWeight() >= 600;
        if (Lokal(e, TextElement.FontStyleProperty) is FontStyle stil) f.Italic = stil != FontStyles.Normal;
        if (Lokal(e, TextElement.ForegroundProperty) is SolidColorBrush vorne) f.Color = Hex(vorne.Color);
        if (Lokal(e, TextElement.BackgroundProperty) is SolidColorBrush hinten) f.Highlight = Hex(hinten.Color);

        if (Lokal(e, Inline.TextDecorationsProperty) is TextDecorationCollection striche)
        {
            f.Underline = striche.Any(d => d.Location == TextDecorationLocation.Underline);
            f.Strikethrough = striche.Any(d => d.Location == TextDecorationLocation.Strikethrough);
        }

        // **Bold, Italic und Underline tragen ihre Bedeutung im Typ und nicht in einer
        // Eigenschaft.** `ReadLocalValue(FontWeightProperty)` gibt für ein `Bold` nichts
        // zurück — der Wert kommt aus dem Stil des Elements. Wer nur örtliche Werte liest,
        // verliert damit genau die drei Auszeichnungen, die der Editor am häufigsten setzt.
        if (e is Bold) f.Bold = true;
        if (e is Italic) f.Italic = true;
        if (e is Underline) f.Underline = true;

        if (e is Inline stueck)
        {
            // Hoch- und Tiefstellung setzt der Editor über die Grundlinienverschiebung.
            if (Lokal(stueck, Inline.BaselineAlignmentProperty) is BaselineAlignment lage)
                f.VerticalAlign = lage switch
                {
                    BaselineAlignment.Superscript => TdVerticalAlign.Superscript,
                    BaselineAlignment.Subscript => TdVerticalAlign.Subscript,
                    _ => TdVerticalAlign.Normal,
                };
        }

        return f;
    }

    /// <summary>
    /// Der **örtlich gesetzte** Wert einer Eigenschaft — oder <c>null</c>, wenn dort nichts
    /// steht. Ein Ausdruck (Binding, Style) zählt nicht als Wert: was daraus wird, weiß nur
    /// WPF.
    /// </summary>
    private static object? Lokal(DependencyObject e, DependencyProperty eigenschaft)
    {
        object wert = e.ReadLocalValue(eigenschaft);
        return wert == DependencyProperty.UnsetValue || wert is Expression ? null : wert;
    }

    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}
