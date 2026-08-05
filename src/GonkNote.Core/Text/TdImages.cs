namespace GonkNote.Core.Text;

/// <summary>
/// Woher die Bytes eines Bildes kommen — und wohin sie beim Lesen gehen.
///
/// <para>
/// <b>Die dritte Naht des Dokumentmodells</b>, nach <see cref="ITdTextMeasure"/> (§4.16) und
/// <see cref="TdFieldContext"/> (§4.20), und aus verwandtem Grund: Das Modell beschreibt ein
/// Dokument, es verwahrt keine Dateien. Wo die Bytes wirklich liegen, weiß der Kopf — heute
/// der <c>BlobStore</c> neben der Datenbank, morgen vielleicht etwas anderes.
/// </para>
/// <para>
/// <b>Warum die Bytes überhaupt draußen bleiben, ist gemessen und nicht gemeint:</b> Als sie
/// in V1 noch im Dokument lagen, wurde ein Word-Dokument mit drei Fotos (2 MB JPEG) zu
/// **16,8 MB** — WPF kodierte jedes Bild als PNG neu — und riss damit die 16-MB-Grenze von
/// LiteDB. Siehe <c>DocumentImages</c> im WPF-Kopf.
/// </para>
/// </summary>
public interface ITdImages
{
    /// <summary>
    /// Die Originalbytes zu einer Kennung — oder <c>null</c>, wenn es sie nicht (mehr) gibt.
    /// <para>
    /// <b><c>null</c> ist kein Programmierfehler.</b> Ein Blob kann fehlen, weil eine Sicherung
    /// unvollständig war oder weil der Blob-Ordner beim Kopieren vergessen wurde — genau die
    /// Falle, vor der Dauerregel 4 warnt. Der Export überspringt das Bild dann, statt
    /// abzubrechen.
    /// </para>
    /// </summary>
    byte[]? Lesen(Guid id);

    /// <summary>
    /// Legt Bytes ab und gibt ihre Kennung zurück — der Weg für einen Import, der Bilder
    /// mitbringt.
    /// </summary>
    Guid Ablegen(byte[] daten, string endung);
}

/// <summary>
/// Ein Bildspeicher im Arbeitsspeicher. **Für Wächter und für einen Roundtrip ohne
/// Datenbank** — nicht für die App: er überlebt keinen Programmstart.
/// </summary>
public sealed class TdMemoryImages : ITdImages
{
    private readonly Dictionary<Guid, byte[]> _bilder = new();

    /// <summary>Wie viele Bilder abgelegt sind.</summary>
    public int Anzahl => _bilder.Count;

    public byte[]? Lesen(Guid id) => _bilder.GetValueOrDefault(id);

    public Guid Ablegen(byte[] daten, string endung)
    {
        // Die Kennung wird aus dem Inhalt gebildet und nicht gewürfelt: derselbe Inhalt ergibt
        // dieselbe Kennung, und ein Wächter, der zweimal dasselbe Bild ablegt, bekommt kein
        // Ergebnis, das sich von Lauf zu Lauf ändert (§7, „Nie Zeit oder Zufall in einer
        // Fixture").
        var streu = System.Security.Cryptography.SHA256.HashData(daten);
        var id = new Guid(streu[..16]);
        _bilder[id] = daten;
        return id;
    }

    /// <summary>Legt Bytes unter einer **vorgegebenen** Kennung ab — für Beispieldokumente.</summary>
    public TdMemoryImages Mit(Guid id, byte[] daten)
    {
        _bilder[id] = daten;
        return this;
    }
}
