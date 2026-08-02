using System.ComponentModel;

namespace GonkNote.Services;

/// <summary>
/// Ein übersetzter Text als Bindungsquelle — <b>eine gewöhnliche Eigenschaft, kein
/// Indexer</b>, und <b>einer je Schlüssel</b>, nicht je Bindung.
/// <para>
/// <b>Warum kein Indexer.</b> Der WPF-Kopf bindet auf <c>Loc.Source["Schlüssel"]</c> und
/// bekommt beim Sprachwechsel eine <c>PropertyChanged</c>-Meldung mit leerem Namen — WPF
/// versteht das als „alle Eigenschaften neu lesen" und wertet auch Indexer-Bindungen neu
/// aus. <b>Avalonia tut das nicht.</b> Am laufenden Programm sah das so aus: der Haken im
/// Sprachmenü sprang auf Deutsch, <c>Loc.Current</c> war umgestellt — und <i>jeder</i> Text
/// der Oberfläche blieb englisch stehen.
/// </para>
/// <para>
/// <b>Warum einer je Schlüssel.</b> Der erste Anlauf legte je <c>{loc:T …}</c> einen eigenen
/// Träger an und hielt ihn <i>schwach</i>, damit nichts festhängt. Das ging schief, und
/// zwar auf lehrreiche Weise: <b>Avalonia hält die Quelle einer Bindung nicht am Leben.</b>
/// Beim Schließen einer Registerkarte erzwingt <c>MainViewModel.ReleaseMemory</c> einen
/// vollständigen Sammellauf — danach waren alle Träger eingesammelt, und der nächste
/// Sprachwechsel erreichte nur noch die Texte, die der Code selbst schreibt
/// (Galerietitel, Pfadleiste, Datumsangaben). Das Ergebnis war eine <b>halb übersetzte</b>
/// Oberfläche: Pfadleiste deutsch, Menüleiste englisch.
/// </para>
/// <para>
/// Ein Träger je Schlüssel löst beides auf einmal: die Sammlung ist durch die Zahl der
/// Übersetzungsschlüssel begrenzt (einige hundert, keine unbegrenzte Liste), sie darf
/// deshalb <b>stark</b> halten, und ein Sammellauf kann nichts mehr wegnehmen. Dass sich
/// viele Bindungen einen Träger teilen, ist unproblematisch — er ist unveränderlich bis auf
/// die Meldung.
/// </para>
/// </summary>
internal sealed class LocText(string key) : INotifyPropertyChanged
{
    public string Value => Loc.T(key);

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Auffrischen() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
}

/// <summary>Die Träger, einer je Übersetzungsschlüssel.</summary>
internal static class LocTexte
{
    private static readonly Lock Schloss = new();
    private static readonly Dictionary<string, LocText> Traeger = new(StringComparer.Ordinal);

    static LocTexte() => Loc.LanguageChanged += AlleAuffrischen;

    /// <summary>Der Träger zu <paramref name="key"/> — beim ersten Mal angelegt, danach derselbe.</summary>
    public static LocText Fuer(string key)
    {
        lock (Schloss)
        {
            if (!Traeger.TryGetValue(key, out var text))
                Traeger[key] = text = new LocText(key);
            return text;
        }
    }

    private static void AlleAuffrischen()
    {
        LocText[] alle;
        lock (Schloss) alle = [.. Traeger.Values];

        // Außerhalb des Schlosses melden: das Auffrischen führt in die Oberfläche, und die
        // kann dabei ihrerseits neue Bindungen aufbauen und damit Fuer(...) aufrufen.
        foreach (var t in alle) t.Auffrischen();
    }
}
