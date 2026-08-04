using System.Text;

namespace GonkNote.Core.Text;

/// <summary>Die Art der Aufzählungsmarke.</summary>
public enum TdListMarker
{
    Bullet,
    Decimal,
    LowerLetter,
    UpperLetter,
    LowerRoman,
    UpperRoman,
}

/// <summary>
/// Eine Ebene einer Liste: wie ihre Marke aussieht und wie weit sie einrückt.
/// </summary>
public sealed class TdListLevel
{
    public TdListMarker Marker { get; set; } = TdListMarker.Bullet;

    private string _text = "";

    /// <summary>
    /// Die Vorlage der Marke. <c>%1</c> bis <c>%9</c> stehen für den Zähler der jeweiligen
    /// Ebene — <c>"%1."</c> ergibt „1.", <c>"%1.%2."</c> ergibt „1.2." wie in einer
    /// Gliederung. Bei <see cref="TdListMarker.Bullet"/> steht hier das Zeichen selbst.
    /// <para>
    /// **Die Platzhalter sind Words Schreibweise** (<c>w:lvlText</c>) und nicht eine eigene
    /// Erfindung — so geht die Vorlage unverändert durch DOCX, statt bei jedem Export
    /// nachgebaut zu werden.
    /// </para>
    /// </summary>
    public string Text { get => _text; set => _text = value ?? ""; }

    /// <summary>Linker Einzug des Absatzes in Zentimetern.</summary>
    public double IndentCm { get; set; } = 0.75;

    /// <summary>
    /// Hängender Einzug: so weit steht die Marke **links** vom Text. Der Text aller Zeilen
    /// fluchtet damit, auch der der zweiten — genau das unterscheidet eine Liste von einem
    /// Absatz, dem man ein „• " vorangestellt hat.
    /// </summary>
    public double HangingCm { get; set; } = 0.5;

    /// <summary>Der erste Wert dieser Ebene. Fast immer 1.</summary>
    public int Start { get; set; } = 1;

    public static TdListLevel Punkt(int ebene) => new()
    {
        Marker = TdListMarker.Bullet,
        Text = "•",
        IndentCm = 0.75 * (ebene + 1),
        HangingCm = 0.5,
    };

    public static TdListLevel Nummer(int ebene) => new()
    {
        Marker = TdListMarker.Decimal,
        Text = "%" + (ebene + 1) + ".",
        IndentCm = 0.75 * (ebene + 1),
        HangingCm = 0.5,
        Start = 1,
    };
}

/// <summary>
/// Eine Listendefinition: ihre Ebenen. Mehrere Absätze verweisen über
/// <see cref="TdListRef"/> darauf.
/// </summary>
public sealed class TdListDefinition
{
    /// <summary>Die Kennung, auf die <see cref="TdListRef.ListId"/> zeigt.</summary>
    public int Id { get; set; }

    public List<TdListLevel> Levels { get; set; } = new();

    /// <summary>Neun Ebenen Aufzählungspunkte — Words Vorrat.</summary>
    public static TdListDefinition Punkte(int id) => new()
    {
        Id = id,
        Levels = [.. Enumerable.Range(0, 9).Select(TdListLevel.Punkt)],
    };

    /// <summary>Neun Ebenen Nummerierung.</summary>
    public static TdListDefinition Nummern(int id) => new()
    {
        Id = id,
        Levels = [.. Enumerable.Range(0, 9).Select(TdListLevel.Nummer)],
    };

    /// <summary>
    /// Die Ebene, oder die letzte vorhandene. **Kein Wurf bei einer zu tiefen Ebene:** ein
    /// fremdes DOCX kann auf eine Ebene zeigen, die seine Definition nicht hat, und ein
    /// Aufzählungspunkt an der falschen Stelle ist besser als ein Dokument, das nicht aufgeht.
    /// </summary>
    public TdListLevel? Level(int ebene)
    {
        if (Levels.Count == 0) return null;
        return Levels[Math.Clamp(ebene, 0, Levels.Count - 1)];
    }
}

/// <summary>Der Verweis eines Absatzes auf seine Liste und Ebene.</summary>
public sealed class TdListRef
{
    public int ListId { get; set; }

    /// <summary>Ab 0 gezählt — wie in DOCX (<c>w:ilvl</c>).</summary>
    public int Level { get; set; }

    public TdListRef() { }

    public TdListRef(int listId, int level) { ListId = listId; Level = level; }
}

/// <summary>
/// Rechnet aus, welche Marke vor welchem Absatz steht.
///
/// <para>
/// <b>Warum das eine eigene Rechnung ist und nicht am Absatz steht.</b> Die Nummer eines
/// Listenpunkts hängt nicht von ihm selbst ab, sondern davon, was **vor** ihm kommt. Sie im
/// Absatz zu speichern hieße, sie bei jeder Einfügung im ganzen Dokument nachzuziehen — und
/// jede vergessene Stelle wäre eine Liste, die bei 3 weiterzählt, nachdem die 2 gelöscht
/// wurde. Genau deshalb speichert auch DOCX nur die Zugehörigkeit und rechnet die Nummer beim
/// Öffnen.
/// </para>
/// </summary>
public static class TdListNumbering
{
    /// <summary>
    /// Die Marke je Absatz, in Dokumentreihenfolge. Absätze ohne Liste kommen nicht vor.
    /// </summary>
    public static Dictionary<TdParagraph, string> Marken(TdDocument doc)
    {
        var marken = new Dictionary<TdParagraph, string>();

        // Je Liste ein Satz Zähler, einer je Ebene.
        var zaehler = new Dictionary<int, int[]>();
        // Die zuletzt benutzte Ebene je Liste — daran hängt, wann tiefere Ebenen neu anfangen.
        var letzteEbene = new Dictionary<int, int>();

        foreach (var absatz in doc.Paragraphs())
        {
            if (absatz.List is not { } verweis) continue;

            var definition = doc.Lists.FirstOrDefault(l => l.Id == verweis.ListId);
            if (definition?.Level(verweis.Level) is not { } ebene) continue;

            int tiefe = Math.Clamp(verweis.Level, 0, Math.Max(0, definition.Levels.Count - 1));

            if (!zaehler.TryGetValue(verweis.ListId, out var stand))
            {
                stand = new int[definition.Levels.Count];
                for (int i = 0; i < stand.Length; i++) stand[i] = definition.Levels[i].Start - 1;
                zaehler[verweis.ListId] = stand;
                letzteEbene[verweis.ListId] = tiefe;
            }

            // **Tiefer gerückt heißt: hier fängt eine neue Unterliste an.** Ohne das
            // Zurücksetzen zählte die zweite Unterliste dort weiter, wo die erste aufgehört
            // hat — der Fehler, den man erst bei der dritten Ebene bemerkt.
            if (tiefe > letzteEbene[verweis.ListId])
            {
                for (int i = letzteEbene[verweis.ListId] + 1; i <= tiefe && i < stand.Length; i++)
                    stand[i] = definition.Levels[i].Start - 1;
            }

            stand[tiefe]++;
            letzteEbene[verweis.ListId] = tiefe;

            marken[absatz] = MarkeBauen(ebene, definition, stand, tiefe);
        }

        return marken;
    }

    /// <summary>
    /// Setzt die Vorlage der Ebene zusammen: <c>%1</c> bis <c>%9</c> werden durch die Zähler
    /// ersetzt, jeder in der Schreibweise **seiner eigenen** Ebene. Ein Aufzählungspunkt hat
    /// keine Platzhalter und kommt unverändert durch.
    /// </summary>
    private static string MarkeBauen(TdListLevel ebene, TdListDefinition definition, int[] stand, int tiefe)
    {
        if (ebene.Marker == TdListMarker.Bullet || !ebene.Text.Contains('%')) return ebene.Text;

        var sb = new StringBuilder(ebene.Text.Length + 4);

        for (int i = 0; i < ebene.Text.Length; i++)
        {
            if (ebene.Text[i] == '%' && i + 1 < ebene.Text.Length && char.IsDigit(ebene.Text[i + 1]))
            {
                int nummer = ebene.Text[i + 1] - '0';   // 1..9
                int index = nummer - 1;

                // Ein Verweis auf eine Ebene **unter** der eigenen ist sinnlos (ihr Zähler
                // gehört zu einer Unterliste, die hier noch gar nicht begonnen hat) — und
                // einer über den Vorrat hinaus kommt aus einer fremden Datei.
                if (index >= 0 && index < stand.Length && index <= tiefe)
                    sb.Append(Formatiert(stand[index], definition.Levels[index].Marker));

                i++;   // die Ziffer ist verbraucht
                continue;
            }
            sb.Append(ebene.Text[i]);
        }
        return sb.ToString();
    }

    /// <summary>Ein Zähler in der Schreibweise seiner Ebene.</summary>
    public static string Formatiert(int wert, TdListMarker art) => art switch
    {
        TdListMarker.Decimal => wert.ToString(),
        TdListMarker.LowerLetter => Buchstaben(wert, 'a'),
        TdListMarker.UpperLetter => Buchstaben(wert, 'A'),
        TdListMarker.LowerRoman => Roemisch(wert).ToLowerInvariant(),
        TdListMarker.UpperRoman => Roemisch(wert),
        _ => wert.ToString(),
    };

    /// <summary>a, b, … z, aa, ab, … — wie Word und wie eine Tabellenkalkulation.</summary>
    private static string Buchstaben(int wert, char erster)
    {
        if (wert < 1) return "";

        var sb = new StringBuilder();
        while (wert > 0)
        {
            wert--;                                   // 1 → 'a', nicht 'b'
            sb.Insert(0, (char)(erster + wert % 26));
            wert /= 26;
        }
        return sb.ToString();
    }

    private static readonly (int Wert, string Zeichen)[] RoemischeStufen =
    [
        (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
        (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
        (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
    ];

    private static string Roemisch(int wert)
    {
        // Null und negative Zahlen haben keine römische Schreibweise. Sie kommen aus einer
        // fremden Datei mit `start="0"` — dann steht dort die Ziffer, und nicht nichts.
        if (wert < 1) return wert.ToString();

        var sb = new StringBuilder();
        foreach (var (stufe, zeichen) in RoemischeStufen)
            while (wert >= stufe) { sb.Append(zeichen); wert -= stufe; }

        return sb.ToString();
    }
}
