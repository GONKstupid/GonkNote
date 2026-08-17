namespace GonkNote.Core.Text;

/// <summary>
/// <b>Aufzählung, Nummerierung und Vorlagen</b> — Gruppe A aus §6 (HANDOFF §4.39).
///
/// <para>
/// <b>Beides sind Absatzänderungen und laufen deshalb über
/// <see cref="TdFormatEdit.Absatzweise"/></b>: Ein Listenpunkt ist ein Absatz mit einer Angabe
/// und kein eigener Blocktyp (§4.17), und eine Vorlage setzt Absatz- und Zeichenformat des
/// Absatzes. Hier steht nur, **welche** Angabe gesetzt wird — das Ersetzen der Blöcke und die
/// Gegenbewegung stehen an einer Stelle, und zwar dort.
/// </para>
/// </summary>
public static class TdListEdit
{
    // ---------------------------------------------------------------- Listen

    /// <summary>
    /// Schaltet Aufzählung oder Nummerierung für die berührten Absätze um.
    ///
    /// <para>
    /// <b>Umgeschaltet wird gegen das, was schon dasteht:</b> Sind **alle** berührten Absätze
    /// bereits eine Liste dieser Art, wird sie aufgehoben; sonst bekommen alle sie. Das ist die
    /// Erwartung aus jedem Textprogramm — und die einzige Regel, bei der zwei Klicks zu einem
    /// einheitlichen Zustand führen statt zurück ins Gemischte (dieselbe Überlegung wie beim
    /// Fettmachen, §4.36).
    /// </para>
    /// <para>
    /// <b>Alle berührten Absätze kommen in *eine* Liste.</b> Wer drei Absätze markiert und
    /// nummeriert, will 1, 2, 3 — nicht dreimal die 1. Deshalb wird **eine** Definition gesucht
    /// oder angelegt und allen zugewiesen.
    /// </para>
    /// <para>
    /// <b>Die Definition landet im Dokument und bleibt dort</b>, auch wenn die Liste später
    /// wieder aufgehoben wird. Das ist Absicht und kein Leck: <see cref="TdDocument.Lists"/>
    /// ist eine Vorlagensammlung, kein Baum (§4.17) — eine Definition, auf die niemand zeigt,
    /// kostet ein paar Bytes und ist beim nächsten Klick wieder da. **Sie im Verlauf zu führen
    /// hieße, neben dem Blocktausch eine zweite Mechanik zu bauen** (§4.32); eine
    /// zurückgenommene Liste findet ihre Definition so einfach wieder.
    /// </para>
    /// </summary>
    public static TdChange? Umschalten(TdDocument doc, TdSelection auswahl, bool nummeriert)
    {
        var absaetze = Beruehrte(doc, auswahl);
        if (absaetze.Count == 0) return null;

        bool schonAlle = absaetze.All(a => IstArt(doc, a, nummeriert));

        // Erst wenn wirklich eine gebraucht wird — sonst legte ein Ausschalten eine Definition
        // an, die niemand haben wollte.
        int id = schonAlle ? 0 : Definition(doc, nummeriert);

        return TdFormatEdit.Absatzweise(doc, auswahl, stil =>
            stil.List = schonAlle ? null : new TdListRef(id, stil.List?.Level ?? 0));
    }

    /// <summary>
    /// Eine Ebene tiefer oder höher. <b>Nur für Absätze, die schon in einer Liste sind</b> —
    /// für alle anderen ist „Ebene" ohne Bedeutung, und der Einzug ist der richtige Handgriff.
    ///
    /// <para>
    /// Bei 0 ist Schluss und bei 8 auch: Words Vorrat sind neun Ebenen
    /// (<see cref="TdListDefinition.Punkte"/>), und eine Ebene außerhalb der Definition zeichnet
    /// mit der letzten vorhandenen — sichtbar wäre der Klick dann wirkungslos.
    /// </para>
    /// </summary>
    public static TdChange? Ebene(TdDocument doc, TdSelection auswahl, int schritt)
    {
        var absaetze = Beruehrte(doc, auswahl);
        if (absaetze.Count == 0 || absaetze.All(a => a.List is null)) return null;

        return TdFormatEdit.Absatzweise(doc, auswahl, stil =>
        {
            if (stil.List is not { } liste) return;
            stil.List = new TdListRef(liste.ListId, Math.Clamp(liste.Level + schritt, 0, 8));
        });
    }

    /// <summary>
    /// Ist dieser Absatz ein Listenpunkt der gefragten Art? — <b>die Auskunft, aus der ein
    /// Ribbon seine zwei Listenknöpfe stellt.</b>
    /// </summary>
    public static bool IstArt(TdDocument doc, TdParagraph absatz, bool nummeriert)
    {
        if (absatz.List is not { } verweis) return false;

        var definition = doc.Lists.FirstOrDefault(l => l.Id == verweis.ListId);
        var ebene = definition?.Level(verweis.Level);

        return ebene is not null && Nummerierend(ebene.Marker) == nummeriert;
    }

    /// <summary>
    /// Gehören **alle** berührten Absätze zu einer Liste dieser Art? Für den gedrückten Zustand
    /// des Knopfs — <c>false</c>, sobald einer nicht dazugehört.
    /// </summary>
    public static bool Gemeinsam(TdDocument doc, TdSelection auswahl, bool nummeriert)
    {
        var absaetze = Beruehrte(doc, auswahl);
        return absaetze.Count > 0 && absaetze.All(a => IstArt(doc, a, nummeriert));
    }

    // ---------------------------------------------------------------- Vorlagen

    /// <summary>
    /// Setzt eine Absatzvorlage (<see cref="TdStil"/>) auf die berührten Absätze.
    ///
    /// <para>
    /// <b>Die Größe und das Fett gehen an den Absatz und nicht an seine Stücke</b>
    /// (<see cref="TdParagraph.CharFormat"/>): Nur so überlebt ein einzelnes fett gemachtes Wort
    /// darin eine spätere Änderung der Überschrift. **Am Stück gesetzte Abweichungen bleiben
    /// dabei stehen** — wer in einer Überschrift ein Wort rot gemacht hat, will es rot behalten.
    /// </para>
    /// <para>
    /// <b>Eine Vorlage hebt die Listenzugehörigkeit auf</b> — eine Überschrift ist kein
    /// Listenpunkt. Ausgenommen ist „Standard": Sie ist das, worauf man landet, wenn man eine
    /// Überschrift zurücknimmt, und dabei einen Aufzählungspunkt mit zu verlieren wäre eine
    /// Überraschung.
    /// </para>
    /// <para>
    /// <b>Die Gliederungsebene wird mitgesetzt</b> (<see cref="TdParaFormat.OutlineLevel"/>) —
    /// daran hängt das Inhaltsverzeichnis (§4.20). Ohne sie sähe eine Überschrift wie eine aus
    /// und stünde trotzdem nicht darin.
    /// </para>
    /// </summary>
    public static TdChange? Vorlage(TdDocument doc, TdSelection auswahl, TdStil stil) =>
        TdFormatEdit.Absatzweise(doc, auswahl, ziel =>
        {
            ziel.CharFormat.FontSize = stil.SizePt;
            ziel.CharFormat.Bold = stil.Bold;
            ziel.CharFormat.Italic = stil.Italic;
            ziel.CharFormat.Color = stil.ColorHex;

            ziel.Format.SpaceBeforePt = stil.BeforePt == 0 ? null : stil.BeforePt;
            ziel.Format.SpaceAfterPt = stil.AfterPt == 0 ? null : stil.AfterPt;
            ziel.Format.LeftIndentCm = stil.LeftCm == 0 ? null : stil.LeftCm;
            ziel.Format.RightIndentCm = stil.RightCm == 0 ? null : stil.RightCm;
            ziel.Format.OutlineLevel = stil.Heading == 0 ? null : stil.Heading;

            if (stil.Heading > 0) ziel.List = null;
        });

    /// <summary>
    /// Welche Vorlage die Auswahl zeigt — <c>null</c>, wenn keine passt oder die Auswahl sich
    /// nicht einig ist. <b>Die dritte Antwort, wie überall</b> (§4.36).
    /// </summary>
    public static TdStil? GemeinsameVorlage(TdDocument doc, TdSelection auswahl)
    {
        var absaetze = Beruehrte(doc, auswahl);
        if (absaetze.Count == 0) return null;

        TdStil? gefunden = null;

        foreach (var absatz in absaetze)
        {
            var aufgeloest = absatz.CharFormat.Over(doc.DefaultCharFormat).Aufgeloest();

            TdStil? hier = null;
            foreach (var stil in TdStil.Alle)
                if (stil.Passt(aufgeloest)) { hier = stil; break; }

            if (hier is null) return null;
            if (gefunden is not null && gefunden.Value.Name != hier.Value.Name) return null;

            gefunden = hier;
        }

        return gefunden;
    }

    // ---------------------------------------------------------------- Kleinteile

    /// <summary>
    /// Die Absätze, die die Auswahl berührt — <b>dieselbe Auffassung von „berührt" wie
    /// <see cref="TdFormatEdit.Absatz"/></b>, damit Auskunft und Handgriff nie über
    /// verschiedene Absätze reden.
    /// </summary>
    private static List<TdParagraph> Beruehrte(TdDocument doc, TdSelection auswahl)
    {
        var gezogen = TdCursor.Normalisieren(doc, auswahl);

        if (TdEdit.Bereich(doc, gezogen.Start, gezogen.End) is not { } bereich) return [];
        var (container, iA, iB, _, _) = bereich;

        var ziel = new List<TdParagraph>();
        for (int i = iA; i <= iB; i++)
            if (container[i] is TdParagraph absatz) ziel.Add(absatz);

        return ziel;
    }

    /// <summary>
    /// Eine Definition der gefragten Art im Dokument — die **erste passende**, sonst eine neue.
    ///
    /// <para>
    /// <b>Wiederverwenden und nicht immer neu anlegen:</b> Zwei Definitionen derselben Art
    /// bedeuten zwei Zählungen, und wer zwei getrennt nummerierte Absätze zu einer Liste machen
    /// will, bekäme zweimal die 1 (§4.17, „Zwei Listen dürfen sich keine Kennung teilen" — die
    /// Umkehrung gilt auch).
    /// </para>
    /// </summary>
    private static int Definition(TdDocument doc, bool nummeriert)
    {
        foreach (var vorhanden in doc.Lists)
            if (vorhanden.Level(0) is { } ebene && Nummerierend(ebene.Marker) == nummeriert)
                return vorhanden.Id;

        int id = doc.NextListId();
        doc.Lists.Add(nummeriert
            ? TdListDefinition.Nummern(id)
            : TdListDefinition.Punkte(id));

        return id;
    }

    /// <summary>
    /// Zählt diese Marke, oder steht sie nur da? <b>Alles außer <see cref="TdListMarker.Bullet"/>
    /// zählt</b> — römisch, alphabetisch und dezimal sind Nummerierungen, und ein Ribbon mit
    /// einem Knopf je Zählart hätte niemand verlangt.
    /// </summary>
    private static bool Nummerierend(TdListMarker marker) => marker != TdListMarker.Bullet;
}
