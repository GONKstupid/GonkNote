using System.Text;

namespace GonkNote.Core.Text;

/// <summary>
/// Markdown-Export eines <see cref="TdDocument"/> — der erste Exporter, der gegen das eigene
/// Modell läuft statt gegen ein <c>FlowDocument</c> (HANDOFF §4.1, „danach umverdrahten").
///
/// <para>
/// <b>Der Gewinn steht in der ersten Zeile, die er schreibt.</b> Die Überschriftsebene kommt
/// aus <see cref="TdParaFormat.OutlineLevel"/> und wird nicht mehr aus der Schriftgröße
/// zurückgerechnet. Der alte Exporter hatte dafür eine Schwellentabelle
/// (<c>HeadingPrefix</c>: ≥25 → „# ", ≥20 → „## " …), weil das <c>FlowDocument</c> keinen Ort
/// für die Ebene hat — wer eine Überschrift kleiner stellte, verlor dort ihre Ebene, und wer
/// einen Absatz nur groß setzte, bekam ungefragt eine Überschrift. **Geraten wird jetzt genau
/// einmal, bei der Übernahme** (§4.22); danach steht die Ebene im Dokument.
/// </para>
///
/// <para>
/// <b>Diese Klasse steht in Core und zeichnet kein Pixel</b> — die Faustregel aus §3. Sie
/// braucht weder WPF noch Skia; damit kann auch der Linux-Kopf nach Markdown exportieren,
/// sobald er ein Dokument öffnen kann.
/// </para>
///
/// <para>
/// <b>Markdown ist ein ärmeres Format, und was dabei wegfällt, steht hier und nicht in einer
/// stillen Lücke</b> (§7): Es hat keine Seiten — ein <see cref="TdPageBreak"/> und die Felder
/// <see cref="TdFieldKind.PageNumber"/>/<see cref="TdFieldKind.PageCount"/> haben deshalb kein
/// Gegenstück und fallen weg. Kopf- und Fußzeile gehören zur Seiteneinrichtung und werden
/// ebenso wenig geschrieben wie Ränder, Wasserzeichen oder Farben. Ein Bild geht als
/// Platzhalter hinaus: seine Bytes liegen im Blob-Speicher, und eine Markdown-Datei ist eine
/// Datei und kein Ordner.
/// </para>
/// </summary>
public static class TdMarkdown
{
    /// <summary>
    /// Der Platzhaltertext eines Bildes ohne Alternativtext. **Deutsch und fest**: er steht so
    /// in bereits exportierten Dateien und ist damit Datenformat, keine Übersetzung —
    /// dieselbe Überlegung wie bei <see cref="TdField.Platzhalter"/> (§4.20).
    /// </summary>
    private const string BildOhneText = "Bild";

    /// <inheritdoc cref="Schreiben"/>
    public static void Export(TdDocument doc, string pfad, TdFieldContext? kontext = null) =>
        File.WriteAllText(pfad, Schreiben(doc, kontext), new UTF8Encoding(false));

    /// <summary>
    /// Das Dokument als Markdown.
    /// </summary>
    /// <param name="kontext">
    /// Datum und Titel für die Felder, die sie brauchen — <c>null</c> = keine.
    /// <b>Core fragt die Uhr nicht selbst</b> (§4.20), auch hier nicht.
    /// </param>
    public static string Schreiben(TdDocument doc, TdFieldContext? kontext = null)
    {
        var sb = new StringBuilder();
        var marken = TdListNumbering.Marken(doc);
        var stand = new Stand(doc, kontext ?? TdFieldContext.Ohne, marken);

        foreach (var block in doc.Blocks()) BlockSchreiben(block, sb, stand);

        return sb.ToString().TrimEnd() + "\n";
    }

    /// <summary>Was über den ganzen Durchlauf gilt.</summary>
    private sealed record Stand(
        TdDocument Doc, TdFieldContext Kontext, Dictionary<TdParagraph, string> Marken)
    {
        /// <summary>
        /// Die Liste, in der wir gerade stecken — <c>0</c> außerhalb. Sie trennt **zwei
        /// aufeinanderfolgende Listen**: ohne Leerzeile dazwischen hängen die Punkte der
        /// zweiten in den meisten Betrachtern an der ersten, und aus „drei Striche, dann 1./2."
        /// wird eine einzige Aufzählung.
        /// </summary>
        public int Liste { get; set; }
    }

    // ==================== Blöcke ====================

    private static void BlockSchreiben(TdBlock block, StringBuilder sb, Stand stand)
    {
        switch (block)
        {
            case TdParagraph p:
                AbsatzSchreiben(p, sb, stand);
                break;

            case TdTable t:
                TabelleSchreiben(t, sb, stand);
                break;

            // Ein Seitenumbruch hat in einem Format ohne Seiten kein Gegenstück. Er
            // verschwindet hier und nirgends sonst — im Modell und in DOCX steht er weiter.
            case TdPageBreak:
                break;
        }
    }

    private static void AbsatzSchreiben(TdParagraph p, StringBuilder sb, Stand stand)
    {
        // **Ein Absatz mit einem Inhaltsverzeichnis-Feld ist kein Absatz, sondern der Ort des
        // Verzeichnisses** (§4.20). Er trägt keinen eigenen Text.
        if (TdToc.Feld(p) is { } toc)
        {
            VerzeichnisSchreiben(toc, sb, stand);
            return;
        }

        int ebene = stand.Doc.FormatVon(p).OutlineLevel!.Value;

        // Eine Überschrift bekommt keine zusätzliche Fett-/Kursiv-Auszeichnung: die Rauten
        // sagen bereits alles, und `**# Titel**` gibt es in Markdown nicht.
        string text = ebene > 0 ? p.PlainText() : StueckeText(p, stand);
        text = text.Trim();

        string einzug = p.List is { Level: > 0 and var tiefe } ? new string(' ', tiefe * 2) : "";
        string marke = stand.Marken.TryGetValue(p, out var m) ? Listenmarke(m) : "";

        // Eine neue Liste beginnt: Leerzeile davor, sonst verschmilzt sie mit der vorigen.
        int liste = p.List?.ListId ?? 0;
        if (liste != stand.Liste && liste != 0 && stand.Liste != 0) sb.AppendLine();
        stand.Liste = liste;

        if (ebene > 0)
        {
            // Markdown kennt sechs Ebenen, das Modell neun (Word ebenso). Tiefer geht es
            // nicht — eine siebte Raute wäre in jedem Betrachter gewöhnlicher Text.
            sb.Append(new string('#', Math.Min(ebene, 6))).Append(' ').AppendLine(text);
            sb.AppendLine();
        }
        else if (marke.Length > 0)
        {
            sb.Append(einzug).Append(marke).AppendLine(text);
            // **Kein Leerzeile zwischen zwei Listenpunkten** — sonst wird aus einer Liste in
            // vielen Betrachtern eine Folge einzelner Absätze mit Strich davor.
        }
        else if (text.Length > 0)
        {
            sb.AppendLine(text);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine();
        }

        // Die Trennlinie steht **unter** dem Absatz, auch unter einem mit Text — genau wie
        // `w:pBdr/w:bottom` in DOCX. Die Leerzeile davor ist Pflicht: `Text` gefolgt von
        // `---` ist in Markdown eine Überschrift und keine Linie.
        if (stand.Doc.FormatVon(p).BottomBorder is { Sichtbar: true })
        {
            if (marke.Length > 0) sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Die Marke eines Listenpunkts in Markdown-Schreibweise.
    /// <para>
    /// Ein Aufzählungspunkt wird zum Strich — das Zeichen aus dem Modell („•") ist in Markdown
    /// gewöhnlicher Text. Eine Nummer kommt dagegen **wörtlich** aus der Rechnung
    /// (<see cref="TdListNumbering"/>): so bleibt „a)" ein „a)" und wird nicht zur „1.".
    /// </para>
    /// </summary>
    private static string Listenmarke(string marke) =>
        marke.Any(char.IsLetterOrDigit) ? marke + " " : "- ";

    /// <summary>
    /// Das Inhaltsverzeichnis als Liste. **Ohne Seitenzahlen** — die kommen aus dem Umbruch,
    /// und ein Format ohne Seiten hat keine.
    /// </summary>
    private static void VerzeichnisSchreiben(TdField feld, StringBuilder sb, Stand stand)
    {
        var eintraege = TdToc.Eintraege(stand.Doc, null, feld.Argument);
        if (eintraege.Count == 0) { sb.AppendLine(); return; }

        int flach = eintraege.Min(e => e.Level);
        foreach (var eintrag in eintraege)
            sb.Append(new string(' ', (eintrag.Level - flach) * 2))
              .Append("- ")
              .AppendLine(Maskiert(eintrag.Text));

        sb.AppendLine();
    }

    // ==================== Tabellen ====================

    private static void TabelleSchreiben(TdTable t, StringBuilder sb, Stand stand)
    {
        if (t.Rows.Count == 0) return;

        int spalten = t.Spaltenzahl();
        bool kopfGeschrieben = false;

        foreach (var zeile in t.Rows)
        {
            var felder = new List<string>(spalten);
            foreach (var zelle in zeile.Cells)
            {
                // **Eine Fortsetzungszelle trägt keinen Inhalt** (§4.18) — er steht in der
                // Zelle darüber. Und eine über zwei Spalten verbundene Zelle belegt in einer
                // Markdown-Tabelle zwei Felder: das Raster muss in jeder Zeile gleich breit
                // sein, sonst ist es keine Tabelle mehr.
                felder.Add(zelle.IstFortsetzung ? "" : Zellentext(zelle, stand));
                for (int i = 1; i < Math.Max(1, zelle.ColumnSpan); i++) felder.Add("");
            }
            while (felder.Count < spalten) felder.Add("");

            Zeile(felder.Take(spalten));

            if (!kopfGeschrieben)
            {
                Zeile(Enumerable.Repeat("---", spalten));
                kopfGeschrieben = true;
            }
        }

        sb.AppendLine();

        void Zeile(IEnumerable<string> felder)
        {
            sb.Append("| ");
            foreach (string feld in felder) sb.Append(feld).Append(" | ");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Der Text einer Zelle, einzeilig. <b>Ein Rohrzeichen muss maskiert werden</b>, sonst
    /// sprengt eine Note „Physik | Chemie" die Tabelle — und ein Zeilenumbruch ebenso: in
    /// Markdown endet eine Tabellenzeile am Zeilenende.
    /// </summary>
    private static string Zellentext(TdTableCell zelle, Stand stand)
    {
        var teile = zelle.Blocks
            .OfType<TdParagraph>()
            .Select(p => StueckeText(p, stand).Trim())
            .Where(s => s.Length > 0);

        return string.Join(" ", teile).Replace("|", @"\|").Replace("\r", "").Replace("\n", " ");
    }

    // ==================== Textstücke ====================

    private static string StueckeText(TdParagraph p, Stand stand)
    {
        var sb = new StringBuilder();
        foreach (var inline in p.Inlines) StueckSchreiben(inline, p, sb, stand);
        return sb.ToString();
    }

    private static void StueckSchreiben(
        TdInline inline, TdParagraph p, StringBuilder sb, Stand stand)
    {
        switch (inline)
        {
            // **Der Verweis steht vor allem anderen** — er ist eine Klammer um Stücke, und wer
            // über `Inlines` läuft, ohne ihn abzufragen, schreibt eine leere Zeile (§4.20).
            case TdHyperlink verweis:
            {
                var innen = new StringBuilder();
                foreach (var stueck in verweis.Inlines) StueckSchreiben(stueck, p, innen, stand);
                string text = innen.ToString();

                // Ein Verweis ohne Ziel ist keiner — den gibt es in den mitgelieferten
                // Hilfe-Dokumenten. Der bleibt Text statt ein leerer Link zu werden.
                if (verweis.Target.Length == 0) { sb.Append(text); break; }

                sb.Append('[').Append(Maskiert(text)).Append("](").Append(Ziel(verweis.Target)).Append(')');
                break;
            }

            case TdRun lauf when lauf.Text.Length > 0:
            {
                var f = stand.Doc.FormatVon(p, lauf);
                sb.Append(Ausgezeichnet(lauf.Text, f.Bold == true, f.Italic == true, f.Strikethrough == true));
                break;
            }

            case TdGraphic grafik:
                sb.Append("![").Append(Maskiert(grafik.AltText ?? BildOhneText)).Append("]()");
                break;

            case TdField feld:
                sb.Append(Feldtext(feld, stand));
                break;

            case TdLineBreak:
                // Zwei Leerzeichen vor dem Umbruch: das ist in Markdown der erzwungene
                // Zeilenumbruch innerhalb eines Absatzes. Ohne sie fließt die Zeile weiter.
                sb.Append("  \n");
                break;
        }
    }

    /// <summary>
    /// Was von einem Feld im Markdown stehen bleibt.
    /// <para>
    /// <b>Seitenzahl und Seitenanzahl fallen weg</b>, und zwar begründet: Sie hängen am
    /// Umbruch (§4.20), und eine Markdown-Datei hat keine Seiten. Eine Zahl hinzuschreiben
    /// hieße, eine Aussage zu treffen, die im Ziel gar nicht gilt.
    /// </para>
    /// </summary>
    private static string Feldtext(TdField feld, Stand stand) => feld.Kind switch
    {
        TdFieldKind.Date or TdFieldKind.Title =>
            TdFieldValues.Text(feld, stand.Kontext, seite: 0, seiten: null),
        _ => "",
    };

    /// <summary>
    /// Text in eckigen Klammern. Eine unmaskierte Klammer darin beendet den Verweis vorzeitig
    /// — aus „[a]b](url)" wird sonst der Verweis „a", gefolgt von Text.
    /// </summary>
    private static string Maskiert(string text) =>
        text.Replace(@"\", @"\\").Replace("[", @"\[").Replace("]", @"\]");

    /// <summary>
    /// Das Ziel für die runden Klammern. Es geht **wörtlich** hinaus — das ist die
    /// Entscheidung aus §4.20, und genau der Fehler, an dem der alte Exporter einmal
    /// gescheitert ist: ein <c>Uri</c> machte aus <c>kapitel-2.md</c> einen absoluten
    /// <c>file:///</c>-Pfad, der auf jedem anderen Rechner ins Leere zeigt.
    /// <para>
    /// Enthält es Leerzeichen oder Klammern, muss es in spitze Klammern: sonst endet der
    /// Verweis an der ersten schließenden runden Klammer.
    /// </para>
    /// </summary>
    private static string Ziel(string ziel) =>
        ziel.AsSpan().IndexOfAny(" ()<>") >= 0 ? $"<{ziel}>" : ziel;

    private static string Ausgezeichnet(string text, bool fett, bool kursiv, bool durchgestrichen)
    {
        // Leerraum wird nicht dekoriert: `** **` ist in Markdown kein fettes Leerzeichen,
        // sondern zwei Sternchenpaare im Text.
        if (text.Trim().Length == 0) return text;

        string vorn = text[..(text.Length - text.TrimStart().Length)];
        string hinten = text[text.TrimEnd().Length..];
        string kern = text.Trim();

        if (durchgestrichen) kern = $"~~{kern}~~";
        if (fett && kursiv) kern = $"***{kern}***";
        else if (fett) kern = $"**{kern}**";
        else if (kursiv) kern = $"*{kern}*";

        return vorn + kern + hinten;
    }

    // ==================== Lesen ====================

    /// <summary>
    /// <b>Liest eine Markdown-Datei ins eigene Modell</b> — die Gegenrichtung zu
    /// <see cref="Schreiben"/>.
    ///
    /// <para>
    /// <b>Warum das hier steht und nicht im WPF-Kopf, wo es bis Phase 5, Schritt ④ stand.</b>
    /// Der Import war dort die letzte Stelle mit einer <b>zweiten Markdown-Grammatik</b>:
    /// <c>Services/MarkdownImporter.cs</c>, 394 Zeilen mit eigenen regulären Ausdrücken,
    /// obwohl <see cref="Markdown.Parse"/> seit §4.12 in Core steht. <b>Das ist zum fünften
    /// Mal dieselbe Lage</b> nach Farben (§4.9), Schriften (§4.26), Symbolen (§4.31) und
    /// Vorlagen (§4.39) — und §4.13 hatte den <i>Betrachter</i> (<c>MarkdownFlow</c>) schon
    /// umgestellt, nur den Importer nicht.
    /// </para>
    /// <para>
    /// <b>Und es waren zwei gemessene Folgen und nicht nur eine Doppelung:</b> Der Linux-Kopf
    /// konnte <c>.md</c> <b>überhaupt nicht</b> importieren (<c>AvaloniaDocumentIo</c> bot nur
    /// DOCX an) — ein unbenanntes Loch in M2, dasselbe wie beim Tafel-Export in §4.77. Und ein
    /// importiertes Markdown-Dokument bekam <b>kein <c>TextDoc.Model</c></b>, nur
    /// Altformat-Bytes: <see cref="TdFuehrung.UebernahmeStehtAus"/> sagte danach „ja", also
    /// nahm die Datei den Umweg über die Übernahme (§4.22) — und war unter Linux erst lesbar,
    /// nachdem sie einmal unter Windows offen war.
    /// </para>
    /// <para>
    /// <b>Was der Umzug an der Grammatik gekostet hat:</b> zwei Formen, die der Zerleger nicht
    /// kannte — <see cref="MdStrike"/> und <see cref="MdImage"/>, dazu <c>***</c> und
    /// <c>__</c>. <b>Sie fehlten ihm auch gegenüber dem eigenen Export:</b>
    /// <see cref="Schreiben"/> schreibt seit jeher <c>~~</c> und <c>***</c>, und
    /// <see cref="Markdown.Parse"/> las sie als gewöhnlichen Text zurück.
    /// </para>
    /// </summary>
    /// <param name="pfad">Die Markdown-Datei. Bildpfade darin gelten relativ zu ihrem Ordner.</param>
    /// <param name="bilder">
    /// Wohin die Bytes eines lokalen Bildes gehen — die Naht aus <see cref="ITdImages"/>.
    /// </param>
    public static TdDocument Lesen(string pfad, ITdImages bilder) =>
        Lesen(
            File.ReadAllText(pfad),
            Path.GetDirectoryName(Path.GetFullPath(pfad)) ?? "",
            bilder);

    /// <inheritdoc cref="Lesen(string, ITdImages)"/>
    /// <param name="markdown">Der Text selbst.</param>
    /// <param name="basisOrdner">
    /// Wogegen relative Bildpfade aufgelöst werden. <b>Leer heißt „gar nicht":</b> Dann bleibt
    /// jedes Bild sein Ersatztext — der Fall, in dem der Text nicht aus einer Datei kommt.
    /// </param>
    /// <param name="bilder">
    /// <inheritdoc cref="Lesen(string, ITdImages)" path="/param[@name='bilder']"/>
    /// </param>
    public static TdDocument Lesen(string markdown, string basisOrdner, ITdImages bilder)
    {
        var doc = new TdDocument();
        var abschnitt = new TdSection();
        var stand = new Lesestand(doc, basisOrdner, bilder);

        foreach (var block in Markdown.Parse(markdown))
            BlockLesen(block, abschnitt.Blocks, stand);

        // **Ein Dokument ohne Absatz hat keinen Ort für die Schreibmarke** — dieselbe
        // Überlegung wie bei `TdDocument.Leer` und bei der leeren Zelle (§4.19). Eine leere
        // oder unlesbare Datei gibt deshalb ein leeres Dokument und keinen Wurf.
        if (abschnitt.Blocks.Count == 0) abschnitt.Blocks.Add(new TdParagraph());

        doc.Sections.Add(abschnitt);
        return doc;
    }

    /// <summary>Was über den ganzen Lesedurchlauf gilt.</summary>
    private sealed class Lesestand(TdDocument doc, string basisOrdner, ITdImages bilder)
    {
        public TdDocument Doc { get; } = doc;
        public string BasisOrdner { get; } = basisOrdner;
        public ITdImages Bilder { get; } = bilder;

        /// <summary>
        /// Die Kennungen der Listen, die schon angelegt sind — <b>je Art eine</b>.
        ///
        /// <para>
        /// <b>Je Art eine und nicht je Liste eine</b>, und das ist eine Entscheidung: Markdown
        /// unterscheidet zwei Aufzählungsarten und sonst nichts. Zwei Definitionen mit
        /// denselben neun Ebenen wären zwei Wege, dasselbe zu sagen — und die Nummerierung
        /// rechnet <c>TdListNumbering</c> ohnehin aus dem Verlauf und nicht aus der
        /// Definition (§4.17).
        /// </para>
        /// </summary>
        private Dictionary<bool, int> Listen { get; } = new();

        /// <summary>Die Kennung der Liste dieser Art — angelegt, falls es sie noch nicht gibt.</summary>
        public int ListeFuer(bool nummeriert)
        {
            if (Listen.TryGetValue(nummeriert, out int da)) return da;

            int id = Doc.Lists.Count + 1;
            Doc.Lists.Add(nummeriert
                ? TdListDefinition.Nummern(id)
                : TdListDefinition.Punkte(id));

            Listen[nummeriert] = id;
            return id;
        }
    }

    // ---------------------------------------------------------------- Blöcke

    private static void BlockLesen(MdBlock block, List<TdBlock> ziel, Lesestand stand)
    {
        switch (block)
        {
            case MdHeading h:
                var ueberschrift = new TdParagraph(StueckeLesen(h.Inlines, stand));

                // Die Ebene wird **gesetzt und nicht aus der Größe zurückgerechnet** — genau
                // der Gewinn, den der Export oben beschreibt, nur andersherum. `ZurEbene`
                // kennt 1–4; eine `#####` bleibt ein Absatz und keine falsche Überschrift.
                TdStil.ZurEbene(h.Level)?.AufAbsatz(ueberschrift);
                ziel.Add(ueberschrift);
                break;

            case MdParagraph p:
                ziel.Add(new TdParagraph(StueckeLesen(p.Inlines, stand)));
                break;

            case MdCodeBlock c:
                ziel.Add(Codeblock(c.Text));
                break;

            case MdRule:
                // **Dieselbe Trennlinie wie `TdBlockEdit.Trennlinie`** — ein leerer Absatz mit
                // Unterrahmen und kein eigener Blocktyp (§4.40).
                ziel.Add(new TdParagraph
                {
                    Format = { BottomBorder = new TdBorder(1, "#D4DEEA") },
                });
                break;

            case MdQuote q:
                // **Das Zitat wird flachgeklopft**, und das ist eine benannte Grenze: Markdown
                // schachtelt Zitate beliebig tief, das Modell kennt dafür nur die Vorlage
                // „Zitat" (Einzug links und rechts). Ein Zitat im Zitat sieht damit aus wie
                // eines — besser als ein Blocktyp, den kein Export und kein Umbruch kennt.
                foreach (var innen in q.Blocks)
                {
                    int vorher = ziel.Count;
                    BlockLesen(innen, ziel, stand);

                    for (int i = vorher; i < ziel.Count; i++)
                        if (ziel[i] is TdParagraph absatz && absatz.Format.OutlineLevel is null)
                            TdStil.MitNamen("Zitat")?.AufAbsatz(absatz);
                }
                break;

            case MdList l:
                ListeLesen(l, 0, ziel, stand);
                break;

            case MdTable t:
                ziel.Add(TabelleLesen(t, stand));
                break;
        }
    }

    /// <summary>
    /// Ein Code-Block wird <b>ein</b> Absatz mit Zeilenumbrüchen darin und nicht einer je
    /// Zeile: Sonst bekäme jede Zeile den Absatzabstand, und aus zehn Zeilen Code würde eine
    /// Leiter mit Lücken.
    /// </summary>
    private static TdParagraph Codeblock(string text)
    {
        var absatz = new TdParagraph
        {
            CharFormat = { FontFamily = Theming.Fonts.Standard.Family(Theming.FontRole.Mono) },
        };

        string[] zeilen = text.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < zeilen.Length; i++)
        {
            if (i > 0) absatz.Inlines.Add(new TdLineBreak());
            absatz.Inlines.Add(new TdRun(zeilen[i]));
        }

        return absatz;
    }

    private static void ListeLesen(MdList liste, int ebene, List<TdBlock> ziel, Lesestand stand)
    {
        int id = stand.ListeFuer(liste.Ordered);

        foreach (var punkt in liste.Items)
        {
            ziel.Add(new TdParagraph(StueckeLesen(punkt.Inlines, stand))
            {
                List = new TdListRef(id, ebene),
            });

            // Die Unterliste ist im Zerleger ein **Kind** des Punktes, im Modell aber ein
            // **Geschwister** mit höherer Ebene (§4.17) — hier wird aus dem Baum die Folge.
            if (punkt.Sub is { } unter) ListeLesen(unter, ebene + 1, ziel, stand);
        }
    }

    /// <summary>
    /// <b>Die Spaltenbreiten werden gleichmäßig verteilt</b>, und die Gesamtbreite kommt aus
    /// der Seiteneinrichtung: Markdown sagt über Breiten nichts, und eine Tabelle ohne Raster
    /// setzt der Umbruch nicht (§4.19).
    /// </summary>
    private static TdTable TabelleLesen(MdTable t, Lesestand stand)
    {
        var tabelle = new TdTable();

        double breite = stand.Doc.Sections.Count > 0
            ? stand.Doc.Sections[0].Page.TextBreiteCm
            : new TdPageSetup().TextBreiteCm;

        int spalten = Math.Max(1, t.Columns);
        for (int i = 0; i < spalten; i++)
            tabelle.ColumnWidthsCm.Add(breite / spalten);

        tabelle.Rows.Add(ZeileLesen(t.Header, spalten, stand, kopf: true));
        foreach (var zeile in t.Rows)
            tabelle.Rows.Add(ZeileLesen(zeile, spalten, stand, kopf: false));

        return tabelle;
    }

    private static TdTableRow ZeileLesen(
        IReadOnlyList<IReadOnlyList<MdInline>> zellen, int spalten, Lesestand stand, bool kopf)
    {
        var zeile = new TdTableRow { IsHeader = kopf };

        // **Auf die Spaltenzahl aufgefüllt und nicht abgeschnitten:** Eine Zeile mit zu wenig
        // Pipes ist in Markdown erlaubt, und eine fehlende Zelle ließe das Raster verrutschen.
        for (int i = 0; i < spalten; i++)
        {
            var inhalt = i < zellen.Count ? zellen[i] : [];
            var absatz = new TdParagraph(StueckeLesen(inhalt, stand));
            if (kopf) absatz.CharFormat.Bold = true;

            zeile.Cells.Add(new TdTableCell(absatz));
        }

        return zeile;
    }

    // ---------------------------------------------------------------- Stücke

    private static List<TdInline> StueckeLesen(IReadOnlyList<MdInline> stuecke, Lesestand stand)
    {
        var ziel = new List<TdInline>();
        foreach (var s in stuecke) StueckLesen(s, new TdCharFormat(), ziel, stand);
        return ziel;
    }

    /// <summary>
    /// <b>Die Auszeichnung wird beim Absteigen mitgeführt und nicht nachträglich verteilt.</b>
    /// Der Zerleger schachtelt (<c>**fett mit *kursiv* darin**</c>); das Modell kennt keine
    /// Schachtelung, sondern flache Stücke mit Formaten. Wer erst flach macht und dann
    /// auszeichnet, verliert die innere Auszeichnung — <b>das ist die Erbfolge aus §7</b>.
    /// </summary>
    private static void StueckLesen(
        MdInline stueck, TdCharFormat geerbt, List<TdInline> ziel, Lesestand stand)
    {
        switch (stueck)
        {
            case MdText t:
                if (t.Text.Length > 0) ziel.Add(new TdRun(t.Text) { Format = geerbt.Kopie() });
                break;

            case MdCodeSpan c:
                var fest = geerbt.Kopie();
                fest.FontFamily = Theming.Fonts.Standard.Family(Theming.FontRole.Mono);
                ziel.Add(new TdRun(c.Text) { Format = fest });
                break;

            case MdBold b:
                var fett = geerbt.Kopie();
                fett.Bold = true;
                foreach (var innen in b.Inner) StueckLesen(innen, fett, ziel, stand);
                break;

            case MdItalic k:
                var kursiv = geerbt.Kopie();
                kursiv.Italic = true;
                foreach (var innen in k.Inner) StueckLesen(innen, kursiv, ziel, stand);
                break;

            case MdStrike d:
                var durch = geerbt.Kopie();
                durch.Strikethrough = true;
                foreach (var innen in d.Inner) StueckLesen(innen, durch, ziel, stand);
                break;

            case MdLink l:
                // **Der Verweis ist eine Klammer und kein Lauf mit Zusatzfeld** (§4.20) — der
                // Linktext steht darin, mit dem geerbten Format.
                ziel.Add(new TdHyperlink(
                    l.Target, new TdRun(l.Text) { Format = geerbt.Kopie() }));
                break;

            case MdImage bild:
                ziel.Add(BildLesen(bild, geerbt, stand));
                break;
        }
    }

    /// <summary>
    /// <b>Ein Bild, das sich nicht laden lässt, wird sein Ersatztext</b> — und nicht
    /// weggelassen.
    ///
    /// <para>
    /// <b>Die drei Fälle, in denen das eintritt, sind alle gewöhnlich</b> und keiner ein
    /// Fehler: ein Verweis ins Netz (die App ist offline, §1), eine Datei, die es nicht gibt,
    /// und ein Text, der gar nicht aus einer Datei kommt (leerer Basisordner). <b>Ein
    /// weggelassenes Bild sähe aus, als stünde im Dokument nichts</b> — der Ersatztext sagt,
    /// dass dort etwas war.
    /// </para>
    /// <para>
    /// <b>Die Originalbytes gehen unverändert in den Blob-Speicher</b> (§4.21): Neu kodiert
    /// wurde in V1 aus 2 MB Vorlage ein 16,8-MB-Export.
    /// </para>
    /// </summary>
    private static TdInline BildLesen(MdImage bild, TdCharFormat geerbt, Lesestand stand)
    {
        if (Bildbytes(bild.Source, stand.BasisOrdner) is var (daten, endung))
        {
            var gelesen = new TdImage(stand.Bilder.Ablegen(daten, endung), endung, 0, 0);
            if (bild.Alt.Length > 0 && bild.Alt != BildOhneText) gelesen.AltText = bild.Alt;
            return gelesen;
        }

        string ersatz = bild.Alt.Length > 0 ? bild.Alt : BildOhneText;
        return new TdRun($"[{ersatz}]") { Format = geerbt.Kopie() };
    }

    /// <summary>
    /// Die Bytes eines lokalen Bildes samt Endung — <c>null</c> in allen drei Fällen oben.
    /// </summary>
    private static (byte[] Daten, string Endung)? Bildbytes(string quelle, string basisOrdner)
    {
        try
        {
            if (quelle.Length == 0) return null;
            if (quelle.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return null;

            string voll = Path.IsPathRooted(quelle)
                ? quelle
                : basisOrdner.Length == 0 ? "" : Path.Combine(basisOrdner, quelle);

            if (voll.Length == 0 || !File.Exists(voll)) return null;

            return (File.ReadAllBytes(voll),
                    Path.GetExtension(voll).TrimStart('.').ToLowerInvariant());
        }
        catch
        {
            // Ein unlesbarer Pfad ist kein Grund, den Import abzubrechen — das Bild wird sein
            // Ersatztext, wie in den drei Fällen oben.
            return null;
        }
    }
}
