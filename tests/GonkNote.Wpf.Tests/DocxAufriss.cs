using System.IO;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using GonkNote.Core.Rendering;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// Liest ein erzeugtes DOCX und schreibt seinen Inhalt als lesbaren Aufriss.
/// <para>
/// Das ist das Golden-File, an dem Phase 4 gemessen wird: die eigene Dokument-Engine ersetzt
/// dann das <c>FlowDocument</c> unter dem Exporter. Kommt danach derselbe Aufriss heraus, ist
/// nichts verloren gegangen — Überschriftenebenen, Tabellenraster, Zellinhalte, Bildmaße,
/// Listen, Nummerierung, TOC-Feld, Seiteneinrichtung, Kopf- und Fußzeile.
/// </para>
/// Aufgeschrieben wird nur, was **inhaltlich** zählt. Bewusst nicht dabei: Part-Namen,
/// Beziehungs-Ids und Bildbyte-Längen — die dürfen sich ändern, ohne dass das Dokument
/// anders aussieht. Ein Golden-File, das an solchen Dingen hängt, wird nach dem dritten
/// falschen Alarm abgeschaltet, und dann bewacht es gar nichts mehr.
/// </summary>
internal static class DocxAufriss
{
    public static string Von(string pfad)
    {
        using var docx = WordprocessingDocument.Open(pfad, false);
        var main = docx.MainDocumentPart ?? throw new InvalidOperationException("Kein Hauptteil im DOCX.");
        var body = main.Document.Body ?? throw new InvalidOperationException("Kein Body im DOCX.");

        var sb = new StringBuilder();
        sb.AppendLine("# Aufriss des DOCX-Exports");
        sb.AppendLine();

        // Gliederungsebene ist der Rohwert aus OOXML und damit 0-basiert: 0 = „Überschrift 1".
        sb.AppendLine("## Formatvorlagen");
        foreach (var style in main.StyleDefinitionsPart?.Styles?.Elements<W.Style>() ?? [])
        {
            int? ebene = style.StyleParagraphProperties?.OutlineLevel?.Val?.Value;
            sb.AppendLine($"- {style.StyleId?.Value} (\"{style.StyleName?.Val?.Value}\")"
                + (ebene.HasValue ? $" Gliederungsebene(0-basiert)={ebene}" : ""));
        }
        sb.AppendLine();

        sb.AppendLine("## Nummerierungen");
        foreach (var num in main.NumberingDefinitionsPart?.Numbering?.Elements<W.NumberingInstance>() ?? [])
            sb.AppendLine($"- numId={num.NumberID?.Value} -> abstractNumId={num.AbstractNumId?.Val?.Value}");
        sb.AppendLine();

        sb.AppendLine("## Einstellungen");
        sb.AppendLine($"- Felder beim Öffnen aktualisieren: " +
                      $"{main.DocumentSettingsPart?.Settings?.GetFirstChild<W.UpdateFieldsOnOpen>()?.Val?.Value}");
        sb.AppendLine();

        sb.AppendLine("## Inhalt");
        foreach (var element in body.ChildElements)
            SchreibeBlock(element, sb, main, einzug: 0);

        sb.AppendLine();
        sb.AppendLine("## Kopf- und Fußzeilen");
        foreach (var teil in main.HeaderParts)
            sb.AppendLine($"- Kopfzeile: {Feldtext(teil.Header)}");
        foreach (var teil in main.FooterParts)
            sb.AppendLine($"- Fußzeile: {Feldtext(teil.Footer)}");

        return sb.ToString();
    }

    private static void SchreibeBlock(OpenXmlElement element, StringBuilder sb,
                                      MainDocumentPart main, int einzug)
    {
        string pad = new(' ', einzug * 2);

        switch (element)
        {
            case W.Paragraph p:
                sb.Append(pad).AppendLine(AbsatzZeile(p, main));
                break;

            case W.Table t:
            {
                var spalten = t.GetFirstChild<W.TableGrid>()?.Elements<W.GridColumn>()
                    .Select(c => c.Width?.Value ?? "?") ?? [];
                var zeilen = t.Elements<W.TableRow>().ToList();
                sb.Append(pad).AppendLine($"TABELLE {zeilen.Count} Zeilen, Raster [{string.Join(", ", spalten)}]");

                // **Die Linien stehen an der Tabelle und nicht an jeder Zelle** (§4.18). Der
                // alte Exporter schrieb sie je Zelle; ohne diese Zeile stünde im Aufriss gar
                // keine Linie mehr, und ein rahmenloser Export sähe aus wie ein gewollter.
                if (t.GetFirstChild<W.TableProperties>()?.TableBorders?.TopBorder is { } tblRahmen)
                    sb.Append(pad).AppendLine(
                        $"  RAHMEN {Enumwert(tblRahmen.Val, "-")}:{tblRahmen.Color?.Value} Stärke={tblRahmen.Size?.Value}");

                for (int z = 0; z < zeilen.Count; z++)
                {
                    var zellen = zeilen[z].Elements<W.TableCell>().ToList();
                    sb.Append(pad).AppendLine($"  Zeile {z + 1}:");
                    foreach (var zelle in zellen)
                    {
                        // Die Spaltenbreiten stehen im Raster (tblGrid), nicht je Zelle — das
                        // ist gültiges OOXML und Word richtet sich danach. Hier steht deshalb
                        // nur, was tatsächlich je Zelle geschrieben wird: Verbund,
                        // Schattierung und abweichende Rahmen.
                        var props = zelle.TableCellProperties;
                        var merkmale = new List<string>();
                        if (props?.GridSpan?.Val is { } span) merkmale.Add($"Spalten={span}");
                        if (props?.VerticalMerge != null)
                            merkmale.Add($"Zeilenverbund={Enumwert(props.VerticalMerge.Val, "Continue")}");
                        merkmale.Add($"Füllung={props?.Shading?.Fill?.Value ?? "-"}");
                        if (props?.TableCellBorders?.TopBorder is { } rahmen)
                            merkmale.Add($"Rahmen={Enumwert(rahmen.Val, "-")}:{rahmen.Color?.Value}");
                        sb.Append(pad).AppendLine($"    ZELLE {string.Join(' ', merkmale)}");
                        foreach (var inhalt in zelle.ChildElements)
                            if (inhalt is W.Paragraph or W.Table)
                                SchreibeBlock(inhalt, sb, main, einzug + 3);
                    }
                }
                break;
            }

            case W.SectionProperties sect:
            {
                var groesse = sect.GetFirstChild<W.PageSize>();
                var rand = sect.GetFirstChild<W.PageMargin>();
                sb.Append(pad).AppendLine(
                    $"SEITE {groesse?.Width?.Value}x{groesse?.Height?.Value} " +
                    $"Ausrichtung={Enumwert(groesse?.Orient, "portrait")}");
                sb.Append(pad).AppendLine(
                    $"RÄNDER links={rand?.Left?.Value} oben={rand?.Top?.Value} " +
                    $"rechts={rand?.Right?.Value} unten={rand?.Bottom?.Value}");
                foreach (var verweis in sect.Elements<W.HeaderReference>())
                    sb.Append(pad).AppendLine($"KOPFZEILE-VERWEIS Typ={Enumwert(verweis.Type, "default")}");
                foreach (var verweis in sect.Elements<W.FooterReference>())
                    sb.Append(pad).AppendLine($"FUSSZEILEN-VERWEIS Typ={Enumwert(verweis.Type, "default")}");
                if (sect.GetFirstChild<W.TitlePage>() != null)
                    sb.Append(pad).AppendLine("ERSTE SEITE ABWEICHEND");
                break;
            }
        }
    }

    private static string AbsatzZeile(W.Paragraph p, MainDocumentPart main)
    {
        var props = p.ParagraphProperties;
        var teile = new List<string> { "ABSATZ" };

        if (props?.ParagraphStyleId?.Val?.Value is { } vorlage) teile.Add($"Vorlage={vorlage}");
        if (props?.Justification?.Val is { } aus) teile.Add($"Ausrichtung={aus}");
        if (props?.NumberingProperties is { } nummer)
            teile.Add($"Liste=numId{nummer.NumberingId?.Val?.Value}/Ebene{nummer.NumberingLevelReference?.Val?.Value}");
        if (props?.ParagraphBorders?.BottomBorder != null) teile.Add("Trennlinie");
        if (props?.SpacingBetweenLines is { } abstand && (abstand.Before != null || abstand.After != null))
            teile.Add($"Abstand={abstand.Before?.Value ?? "0"}/{abstand.After?.Value ?? "0"}");

        string inhalt = InhaltsZeile(p, main);
        return string.Join(' ', teile) + (inhalt.Length > 0 ? " " + inhalt : " (leer)");
    }

    private static string InhaltsZeile(W.Paragraph p, MainDocumentPart main)
    {
        var sb = new StringBuilder();

        foreach (var element in p.ChildElements)
        {
            switch (element)
            {
                case W.Run run:
                    sb.Append(RunText(run, main));
                    break;

                case W.Hyperlink link:
                {
                    string ziel = link.Id?.Value is { } id
                        ? main.HyperlinkRelationships.FirstOrDefault(r => r.Id == id)?.Uri?.ToString() ?? "?"
                        : "?";
                    sb.Append("[VERWEIS ").Append(ziel).Append(": ");
                    foreach (var run in link.Elements<W.Run>()) sb.Append(RunText(run, main));
                    sb.Append(']');
                    break;
                }

                case W.SimpleField feld:
                    sb.Append("{FELD ").Append(feld.Instruction?.Value?.Trim()).Append('}');
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Ist eine An/Aus-Angabe wirklich an? <b>Ein fehlendes <c>w:val</c> heißt „an"</b>, ein
    /// <c>w:val="0"</c> dagegen ausdrücklich „aus" — und das ist kein Haarspalten: Seit der
    /// Export gegen das eigene Modell läuft, schreibt er beide Fälle aus (§4.14, <c>null</c>
    /// heißt „nicht gesetzt", <c>false</c> heißt „ausdrücklich nicht"). Wer hier nur auf
    /// Vorhandensein prüft, liest jede nicht-fette Zelle als fett.
    /// </summary>
    private static bool An(W.OnOffType? angabe) => angabe is not null && angabe.Val?.Value != false;

    private static string RunText(W.Run run, MainDocumentPart main)
    {
        var sb = new StringBuilder();
        var rp = run.RunProperties;

        var marken = new List<string>();
        if (An(rp?.Bold)) marken.Add("fett");
        if (An(rp?.Italic)) marken.Add("kursiv");
        if (rp?.Underline is { } u && u.Val?.Value != W.UnderlineValues.None) marken.Add("unterstrichen");
        if (An(rp?.Strike)) marken.Add("durchgestrichen");
        if (rp?.Highlight?.Val is { } hl) marken.Add($"hervor:{hl}");
        if (rp?.VerticalTextAlignment?.Val is { } vert) marken.Add($"stellung:{vert}");
        if (rp?.Color?.Val?.Value is { } farbe) marken.Add($"farbe:{farbe}");
        if (rp?.FontSize?.Val?.Value is { } groesse) marken.Add($"größe:{groesse}");

        string text = string.Concat(run.Elements<W.Text>().Select(t => t.Text));
        if (text.Length > 0)
        {
            sb.Append('"').Append(text).Append('"');
            if (marken.Count > 0) sb.Append('<').Append(string.Join(',', marken)).Append('>');
        }

        // Die **dreiteilige** Feldform: `fldChar begin`, `instrText`, `fldChar end`. Sie steht
        // in einem Lauf und nicht daneben — anders als das kurze `fldSimple`, das der alte
        // Exporter benutzt hat. Ohne diesen Zweig wäre der Absatz mit dem Inhaltsverzeichnis
        // hier „(leer)": das Feld ist da, nur ungelesen.
        foreach (var anweisung in run.Elements<W.FieldCode>())
            sb.Append("{FELD ").Append(anweisung.Text.Trim()).Append('}');

        foreach (var _ in run.Elements<W.Break>()) sb.Append("<umbruch>");
        foreach (var zeichnung in run.Elements<W.Drawing>()) sb.Append(BildText(zeichnung, main));

        return sb.ToString();
    }

    /// <summary>
    /// Bildmaße und die tatsächlichen Pixel des eingebetteten Teils. Die **Byte-Länge** steht
    /// bewusst nicht da: sie hängt am PNG-Kodierer des Systems und würde nach einem
    /// Windows-Update falschen Alarm auslösen. Die Pixelmaße prüfen dagegen genau das, worum
    /// es geht — dass das Original hinausgeht und nicht die kleinere Anzeige-Ableitung.
    /// </summary>
    private static string BildText(W.Drawing zeichnung, MainDocumentPart main)
    {
        var inline = zeichnung.GetFirstChild<DW.Inline>();
        var ausdehnung = inline?.Extent;
        string id = inline?.Descendants<A.Blip>().FirstOrDefault()?.Embed?.Value ?? "";

        string pixel = "?";
        string typ = "?";
        if (id.Length > 0 && main.GetPartById(id) is ImagePart teil)
        {
            typ = teil.ContentType;
            using var strom = teil.GetStream();
            using var speicher = new MemoryStream();
            strom.CopyTo(speicher);
            using var bmp = WbImages.Decode(speicher.ToArray());
            pixel = bmp == null ? "nicht dekodierbar" : $"{bmp.Width}x{bmp.Height}";
        }

        return $"[BILD {ausdehnung?.Cx}x{ausdehnung?.Cy} EMU, Teil={typ} {pixel}]";
    }

    /// <summary>
    /// Der Wert eines OOXML-Aufzählungsattributs als Text.
    /// <para>
    /// Nicht <c>.Value.ToString()</c>: seit der OpenXML-SDK-Fassung 3 sind diese
    /// Aufzählungen Strukturen, deren <c>ToString()</c> „BorderValues { }" liefert — also
    /// nichts. Der Rohtext steht in <c>InnerText</c>.
    /// </para>
    /// </summary>
    private static string Enumwert(OpenXmlSimpleType? wert, string wennLeer) =>
        wert?.InnerText is { Length: > 0 } text ? text : wennLeer;

    private static string Feldtext(OpenXmlElement? wurzel)
    {
        if (wurzel == null) return "(leer)";
        var sb = new StringBuilder();
        foreach (var element in wurzel.Descendants())
            switch (element)
            {
                case W.Text t: sb.Append(t.Text); break;
                case W.SimpleField f: sb.Append('{').Append(f.Instruction?.Value?.Trim()).Append('}'); break;
                case W.FieldCode c: sb.Append('{').Append(c.Text.Trim()).Append('}'); break;
            }
        return sb.Length == 0 ? "(leer)" : sb.ToString();
    }
}
