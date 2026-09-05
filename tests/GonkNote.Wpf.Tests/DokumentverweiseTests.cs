using System.IO;
using System.Windows.Documents;
using GonkNote.Core.Text;
using GonkNote.Services;

namespace GonkNote.Wpf.Tests;

/// <summary>
/// <b>Ein Verweis wird nur dann als Verweis gezeichnet, wenn ihn auch jemand annimmt</b> —
/// die Regel aus <see cref="Dokumentverweise"/>, gemessen an den <b>echten</b>
/// mitgelieferten Dokumenten.
///
/// <para>
/// <b>Der Anlass war der Prüflauf von Phase 5, Schritt ④, und der Fund kam vom laufenden
/// Programm und von keinem Wächter.</b> „Erste Schritte" enthält den Satz „lies die
/// Feature-Übersicht im README" — angeklickt geschah <b>nichts</b>. Das Nachmessen ergab
/// <b>fünf</b> solche Stellen in beiden Sprachen: dreimal Anleitung → README (der Dialog
/// bekam gar keinen Behandler) und zweimal README → <c>THIRD-PARTY-NOTICES.md</c> (die Datei
/// ist in keinem Kopf eingebettet und <i>kann</i> nicht geöffnet werden). Alle fünf waren in
/// der Akzentfarbe gezeichnet und damit von einem echten Verweis nicht zu unterscheiden.
/// </para>
///
/// <para>
/// <b>Warum gegen die echten Dateien und nicht gegen getippten Text:</b> Der Fehler bestand
/// nicht darin, dass die Regel falsch war, sondern darin, dass niemand nachgesehen hat,
/// <i>welche</i> Verweise in den ausgelieferten Dokumenten wirklich stehen. Ein Wächter mit
/// eigenem Beispieltext hätte genau das wieder nicht getan. <b>Wer ein neues
/// <c>.md</c>-Ziel ins README schreibt, das niemand öffnet, macht diesen Test rot.</b>
/// </para>
///
/// <para>
/// Geprüft wird nur der <b>WPF</b>-Betrachter — der Linux-Kopf hat kein Testprojekt, und
/// <c>MarkdownView</c> trifft dieselbe Entscheidung an derselben Stelle. Der Unterschied
/// stand vor ④ zugunsten dieses Kopfes: Drüben bekam ein totes Ziel zusätzlich
/// Unterstreichung, Handzeiger und Tooltip.
/// </para>
/// </summary>
public sealed class DokumentverweiseTests
{
    /// <summary>
    /// Ein mitgeliefertes Dokument, <b>aus dem Repo gelesen und nicht über
    /// <c>EmbeddedDocs</c></b>.
    ///
    /// <para>
    /// <b>Der Umweg ist nötig und nicht bequem:</b> <c>EmbeddedDocs</c> holt die Dateien über
    /// <c>Application.GetResourceStream</c>, und im Testwirt gibt es keine
    /// <c>Application</c> — die Klasse fiele auf ihre Ausweichmeldung zurück, und der
    /// Wächter prüfte einen Satz, der nirgends steht. <b>Er wäre grün und leer.</b>
    /// </para>
    /// <para>
    /// <b>Und die Datei im Repo ist ohnehin die richtige Quelle:</b> Sie ist es, die per
    /// <c>&lt;Resource Include="..\..\README.md"&gt;</c> in beide Köpfe eingebettet wird.
    /// </para>
    /// </summary>
    private static string Mitgeliefert(string name)
    {
        var ordner = new DirectoryInfo(AppContext.BaseDirectory);

        while (ordner != null && !File.Exists(Path.Combine(ordner.FullName, "GonkNote.slnx")))
            ordner = ordner.Parent;

        Assert.NotNull(ordner);

        string pfad = Path.Combine(ordner!.FullName, name);
        Assert.True(File.Exists(pfad), $"{name} nicht gefunden — erwartet in {ordner.FullName}");

        return File.ReadAllText(pfad);
    }

    private static string Anleitung() => Mitgeliefert("ERSTE-SCHRITTE.md");

    private static string Liesmich() => Mitgeliefert("README.md");

    /// <summary>
    /// Alle <b>klickbaren</b> Verweise im Dokument, mit ihrer Beschriftung und der
    /// Unterscheidung <b>Netz oder Dokument</b>.
    ///
    /// <para>
    /// <b>Die Unterscheidung ist nötig und war der erste rote Lauf dieses Wächters:</b> Die
    /// Anleitung enthält auch Verweise ins Netz, und die bleiben klickbar, ganz ohne
    /// Behandler — sie gehen an den Browser. Ein Wächter, der nur „klickbar" zählt, misst
    /// sie mit und behauptet etwas Falsches über die Dokumentverweise.
    /// </para>
    /// </summary>
    private static List<(string Text, bool Netz)> Verweise(
        string markdown, Dokumentverweise? verweise)
    {
        var doc = MarkdownFlow.ToFlowDocument(markdown, verweise);
        var gefunden = new List<(string, bool)>();

        foreach (var block in Bloecke(doc.Blocks))
            if (block is Paragraph absatz)
                Sammeln(absatz.Inlines, gefunden);

        return gefunden;
    }

    private static IEnumerable<Block> Bloecke(IEnumerable<Block> bloecke)
    {
        foreach (var b in bloecke)
        {
            yield return b;

            var innen = b switch
            {
                Section s => s.Blocks.AsEnumerable(),
                List l => l.ListItems.SelectMany(i => i.Blocks),
                Table t => t.RowGroups.SelectMany(g => g.Rows)
                            .SelectMany(r => r.Cells).SelectMany(c => c.Blocks),
                _ => [],
            };

            foreach (var i in Bloecke(innen)) yield return i;
        }
    }

    private static void Sammeln(InlineCollection stuecke, List<(string, bool)> ziel)
    {
        foreach (var s in stuecke)
        {
            switch (s)
            {
                case Hyperlink h:
                    ziel.Add((Text(h.Inlines), h.NavigateUri != null));
                    break;

                case Span sp:
                    Sammeln(sp.Inlines, ziel);
                    break;
            }
        }
    }

    private static string Text(InlineCollection stuecke) =>
        string.Concat(stuecke.OfType<Run>().Select(r => r.Text));

    /// <summary>
    /// <b>Der Verweis der Anleitung auf das README ist klickbar</b> — er war es bis Schritt ④
    /// nicht, weil <c>GuideDialog</c> gar keinen Behandler übergab.
    /// </summary>
    [Fact]
    public void Die_Anleitung_verweist_klickbar_aufs_README() => Sta.Run(() =>
    {
        var verweise = Verweise(
            Anleitung(),
            new Dokumentverweise(EmbeddedDocs.IsReadmeLink, _ => { }));

        // Vier Stellen zeigen aufs README — gemessen an der Datei, nicht angenommen.
        //
        // Die vierte ist in Phase 5, Schritt ⑤ dazugekommen: Abschnitt 1 der Anleitung
        // verweist seither auf „README.md#installieren", weil es die drei Installationswege
        // vorher gar nicht gab. **Dieser Wächter hat den Zusatz gemeldet** — er zählt
        // absichtlich die genaue Liste und nicht „mindestens eine": Eine Zahl, die mit
        // wächst, was sie prüft, prüft nichts (§4.99, der Rundreise-Test, der sich der
        // Palettenlücke angepasst hat). Wer hier eine Zeile ergänzt, ergänzt sie also hier
        // mit — und sieht dabei nach, ob der neue Verweis im Programm wirklich klickbar ist.
        Assert.Equal(
            "Feature-Übersicht im README | README | README | README",
            string.Join(" | ", verweise.Where(v => !v.Netz).Select(v => v.Text)));
    });

    /// <summary>
    /// <b>Und die Gegenprobe, die den eigentlichen Fund festhält:</b> Nimmt der Behandler nur
    /// README-Ziele an, bleibt <c>THIRD-PARTY-NOTICES.md</c> im README <b>gewöhnlicher
    /// Text</b> — kein <see cref="Hyperlink"/>, keine Akzentfarbe.
    /// </summary>
    [Fact]
    public void Ein_Ziel_das_niemand_oeffnet_wird_kein_Verweis() => Sta.Run(() =>
    {
        string readme = Liesmich();

        // Erst nachmessen, dass die Stelle überhaupt im ausgelieferten Text steht — sonst
        // prüfte dieser Wächter ab morgen nichts mehr und bliebe trotzdem grün.
        Assert.Contains("THIRD-PARTY-NOTICES.md", readme);

        var verweise = Verweise(
            readme, new Dokumentverweise(EmbeddedDocs.IsGuideLink, _ => { }));

        Assert.NotEmpty(verweise);
        Assert.DoesNotContain(verweise, v => v.Text.Contains("THIRD-PARTY"));
        Assert.DoesNotContain(verweise, v => v.Text.Contains("Drittanbieter"));
    });

    /// <summary>
    /// <b>Ohne jeden Behandler ist kein <c>.md</c>-Ziel klickbar</b> — und ebenso wenig
    /// eingefärbt. Das ist der Zustand, in dem <c>GuideDialog</c> bis Schritt ④ war.
    ///
    /// <para>
    /// <b>Die Verweise ins Netz bleiben es sehr wohl</b>, und dass sie hier mitgezählt werden
    /// müssen, hat der erste Lauf dieses Wächters gezeigt: Er stand auf <c>Assert.Empty</c>
    /// und war rot — <b>zu Recht</b>.
    /// </para>
    /// </summary>
    [Fact]
    public void Ohne_Behandler_gibt_es_keine_Dokumentverweise() => Sta.Run(() =>
    {
        var verweise = Verweise(Anleitung(), null);

        Assert.DoesNotContain(verweise, v => !v.Netz);
        Assert.Contains(verweise, v => v.Netz);
    });

    /// <summary>
    /// <b>Ein Web-Verweis bleibt klickbar, auch ohne Behandler</b> — er geht an den Browser
    /// und nicht an ein Fenster dieser App. <b>Eine Sperre, die zu viel sperrt, fällt
    /// niemandem auf</b>, deshalb zieht dieser Wächter die Grenze von der anderen Seite.
    /// </summary>
    [Fact]
    public void Ein_Webverweis_bleibt_klickbar() => Sta.Run(() =>
    {
        Assert.True(Assert.Single(Verweise("siehe [Netz](https://example.com)", null)).Netz);
    });
}
