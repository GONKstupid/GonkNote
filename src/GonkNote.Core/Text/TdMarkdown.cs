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
}
