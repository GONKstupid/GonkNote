namespace GonkNote.Core.Text;

/// <summary>Ein Eintrag des Inhaltsverzeichnisses.</summary>
/// <param name="Text">Der Text der Überschrift.</param>
/// <param name="Level">Ihre Gliederungsebene (1 = Überschrift 1).</param>
/// <param name="Page">Die Seite, auf der sie steht — 0, solange kein Umbruch vorliegt.</param>
/// <param name="Source">Der Absatz selbst, für das Sprungziel.</param>
public sealed record TdTocEntry(string Text, int Level, int Page, TdParagraph Source);

/// <summary>
/// Das Inhaltsverzeichnis — Phase 4, Schritt 5.
///
/// <para>
/// <b>Es liest <see cref="TdParaFormat.OutlineLevel"/>, und das ist der ganze Punkt dieses
/// Schritts.</b> Der heutige Markdown-Exporter **rät** die Ebene aus der Schriftgröße zurück
/// (<c>HeadingPrefix</c> rechnet gegen <c>TextStyles</c>), weil das <c>FlowDocument</c> keinen
/// Platz für sie hat: Wer eine Überschrift kleiner stellt, verliert dort ihre Ebene, und ein
/// Verzeichnis daraus ist eine Vermutung. Im eigenen Modell steht die Ebene seit Schritt 1 als
/// eigener Wert — sie ist die verlässliche Quelle, die es vorher nicht gab.
/// </para>
///
/// <para>
/// <b>Und es wird gerechnet, nicht gespeichert</b> — dasselbe Muster wie bei der
/// Listennummer (§4.17). Ein abgelegtes Verzeichnis wäre nach der ersten Umbenennung einer
/// Überschrift falsch, und zwar still: eine veraltete Zeile sieht aus wie eine richtige.
/// </para>
/// </summary>
public static class TdToc
{
    /// <summary>Die Ebenen, die ein Verzeichnis ohne eigene Angabe aufnimmt.</summary>
    public const int EbeneVonStandard = 1;

    /// <inheritdoc cref="EbeneVonStandard"/>
    public const int EbeneBisStandard = 3;

    /// <summary>
    /// Liest die Ebenenangabe eines Feldes (<c>"1-3"</c>). Alles, was nicht danach aussieht,
    /// ergibt die Vorgabe — die Angabe kann aus einem fremden Dokument kommen, und ein
    /// Verzeichnis mit den üblichen drei Ebenen ist besser als keines.
    /// </summary>
    public static (int Von, int Bis) Ebenen(string? angabe)
    {
        if (angabe is not null)
        {
            int strich = angabe.IndexOf('-');
            if (strich > 0 &&
                int.TryParse(angabe[..strich], out int von) &&
                int.TryParse(angabe[(strich + 1)..], out int bis) &&
                von >= 1 && bis >= von)
            {
                return (von, Math.Min(bis, 9));
            }
        }
        return (EbeneVonStandard, EbeneBisStandard);
    }

    /// <summary>Die Ebenenangabe, wie sie ins Feld geschrieben wird.</summary>
    public static string Ebenenangabe(int von, int bis) => $"{von}-{bis}";

    /// <summary>
    /// Das Inhaltsverzeichnis-Feld eines Absatzes — oder <c>null</c>.
    /// <para>
    /// <b>Ein Absatz mit einem solchen Feld ist der Ort des Verzeichnisses und kein
    /// gewöhnlicher Absatz.</b> Er trägt keinen eigenen Text; was auf dem Papier steht, sind
    /// die Einträge.
    /// </para>
    /// </summary>
    public static TdField? Feld(TdParagraph absatz)
    {
        foreach (var inline in absatz.Inlines)
            if (inline is TdField { Kind: TdFieldKind.TableOfContents } feld) return feld;
        return null;
    }

    /// <summary>Enthält das Dokument überhaupt ein Inhaltsverzeichnis?</summary>
    public static bool Enthaelt(TdDocument doc)
    {
        foreach (var absatz in doc.Paragraphs())
            if (Feld(absatz) is not null) return true;
        return false;
    }

    /// <summary>
    /// Die Einträge eines Verzeichnisses.
    /// </summary>
    /// <param name="umbruch">
    /// Der Umbruch, aus dem die Seitenzahlen kommen. <c>null</c> im **ersten** Durchgang: da
    /// gibt es noch keinen, und alle Einträge stehen auf Seite 0 (§4.20).
    /// </param>
    public static List<TdTocEntry> Eintraege(TdDocument doc, TdLayoutResult? umbruch, int von, int bis)
    {
        var seiten = umbruch is null ? null : Seitenzuordnung(umbruch);
        var eintraege = new List<TdTocEntry>();

        foreach (var absatz in doc.Paragraphs())
        {
            // Ein Verzeichnisabsatz ist keine Überschrift, auch wenn ihm jemand eine Ebene
            // gegeben hat — sonst stünde das Verzeichnis in sich selbst.
            if (Feld(absatz) is not null) continue;

            var format = doc.FormatVon(absatz);

            // Der Titel des Dokuments und die Zeile „Inhaltsverzeichnis" sind Überschriften,
            // gehören aber nicht hierher (§4.23) — sonst führte das Verzeichnis den
            // Dokumenttitel als ersten Eintrag und sich selbst als zweiten.
            if (format.ExcludeFromToc == true) continue;

            int ebene = format.OutlineLevel!.Value;
            if (ebene < von || ebene > bis) continue;

            string text = absatz.PlainText().Trim();
            if (text.Length == 0) continue;

            eintraege.Add(new TdTocEntry(text, ebene, seiten?.GetValueOrDefault(absatz) ?? 0, absatz));
        }

        return eintraege;
    }

    /// <inheritdoc cref="Eintraege(TdDocument, TdLayoutResult?, int, int)"/>
    public static List<TdTocEntry> Eintraege(TdDocument doc, TdLayoutResult? umbruch, string? ebenenangabe = null)
    {
        var (von, bis) = Ebenen(ebenenangabe);
        return Eintraege(doc, umbruch, von, bis);
    }

    /// <summary>
    /// Auf welcher Seite steht welcher Absatz? Die **erste** Seite gewinnt: ein Absatz, der
    /// über einen Seitenwechsel läuft, gehört dorthin, wo er anfängt.
    /// <para>
    /// Der Durchlauf steigt in Tabellenzellen ab, weil eine Überschrift auch dort stehen kann
    /// — dieselbe Lehre wie bei <c>TdDocument.Paragraphs()</c> (§4.19).
    /// </para>
    /// </summary>
    private static Dictionary<TdParagraph, int> Seitenzuordnung(TdLayoutResult umbruch)
    {
        var seiten = new Dictionary<TdParagraph, int>();

        foreach (var seite in umbruch.Pages)
        {
            foreach (var zeile in seite.Lines) Merken(zeile);

            foreach (var zeilenband in seite.TableRows)
                foreach (var zelle in zeilenband.Cells)
                    foreach (var zeile in zelle.Lines) Merken(zeile);

            void Merken(TdLine zeile)
            {
                if (zeile.Source is { } absatz) seiten.TryAdd(absatz, seite.Number);
            }
        }

        return seiten;
    }

    /// <summary>
    /// Der Absatz, als der ein Eintrag gesetzt wird: eingerückt nach seiner Ebene, ohne
    /// Absatzabstände.
    /// <para>
    /// <b>Die Seitenzahl steht nicht in diesem Absatz.</b> Sie gehört rechtsbündig ans
    /// Zeilenende, und das ist in Word ein Tabulator mit Füllzeichen — den kennt das Modell
    /// nicht, und einen dafür zu erfinden, wäre ein halbes Feature an der falschen Stelle. Der
    /// Umbruch setzt sie deshalb selbst an den rechten Rand (§4.20).
    /// </para>
    /// </summary>
    public static TdParagraph EintragsAbsatz(TdTocEntry eintrag, TdCharFormat zeichenformat) =>
        new(eintrag.Text)
        {
            CharFormat = zeichenformat,
            Format =
            {
                Alignment = TdAlign.Left,
                LeftIndentCm = 0.5 * (eintrag.Level - 1),
                FirstLineIndentCm = 0,
                SpaceBeforePt = 0,
                SpaceAfterPt = 0,
                OutlineLevel = 0,
            },
        };
}
