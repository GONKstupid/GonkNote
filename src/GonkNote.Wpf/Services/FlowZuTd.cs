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
/// zurückerkannt (<see cref="TextStyles.HeadingLevel"/>), genau wie der heutige
/// Markdown-Export es tut. **Danach steht sie als eigener Wert im Modell und muss nie wieder
/// geraten werden.** Das ist der Unterschied zwischen einer Übernahme und einem Format.
/// </para>
/// </summary>
public static class FlowZuTd
{
    // WPF rechnet in geräteunabhängigen Pixeln: 96 auf ein Zoll.
    private const double CmProPixel = 2.54 / 96.0;
    private const double PunktProPixel = 72.0 / 96.0;

    private static double Cm(double px) => px * CmProPixel;
    private static double Pt(double px) => px * PunktProPixel;

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
                ziel.Add(AbsatzUmwandeln(p, zustand));
                break;

            case Section s:
                // Ein `Section` im FlowDocument ist eine bloße Klammer und keine
                // Seiteneinrichtung — es gibt im Altformat nur **eine** davon (§4.15).
                BloeckeUmwandeln(s.Blocks, ziel, zustand);
                break;

            case List liste:
                ListeUmwandeln(liste, ziel, zustand, ebene: 0);
                break;

            case Table tabelle:
                ziel.Add(TabelleUmwandeln(tabelle, zustand));
                break;

            case BlockUIContainer behaelter:
                // Ein Bild auf Blockebene wird ein Absatz, der nichts als dieses Bild
                // enthält — genau die Form, in der DOCX es kennt (§4.21).
                if (GrafikAus(behaelter.Child, zustand) is { } grafik)
                    ziel.Add(new TdParagraph([grafik]));
                break;
        }
    }

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

        if (Lokal(p, Block.TextAlignmentProperty) is TextAlignment aus)
            f.Alignment = aus switch
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

        // Der Editor setzt den Zeilenabstand als `FontSize * Faktor` (TextEditorView.Format) —
        // die Rechnung geht deshalb glatt zurück und muss nicht geschätzt werden.
        if (Lokal(p, Block.LineHeightProperty) is double hoehe && !double.IsNaN(hoehe) && p.FontSize > 0)
            f.LineSpacing = hoehe / p.FontSize;

        if (Lokal(p, Paragraph.KeepWithNextProperty) is bool halten) f.KeepWithNext = halten;
        if (Lokal(p, Paragraph.BreakPageBeforeProperty) is bool umbruch) f.PageBreakBefore = umbruch;

        // **Hier wird geraten, und hier ist es richtig:** Das FlowDocument hat keinen Platz für
        // eine Gliederungsebene, also wird sie aus der Schriftgröße zurückerkannt — danach
        // steht sie im Modell und muss nie wieder geraten werden (§4.20).
        int ebene = TextStyles.HeadingLevel(p);
        if (ebene > 0) f.OutlineLevel = ebene;

        return f;
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

    // ==================== Formate ====================

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
