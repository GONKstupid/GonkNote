using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GonkNote.Core.Services;

namespace GonkNote.Services;

/// <summary>Verweis auf ein Bild-Original im Blob-Speicher.</summary>
/// <param name="Id">Schlüssel im <see cref="BlobStore"/>.</param>
/// <param name="Extension">Dateiendung des Originals ("jpg", "png", …) – bestimmt beim
/// DOCX-Export, als welcher Bildtyp die unveränderten Bytes wieder hineingeschrieben werden.</param>
public readonly record struct BlobRef(Guid Id, string Extension)
{
    private const string Prefix = "blob:";

    public override string ToString() => $"{Prefix}{Id:N}:{Extension}";

    public static BlobRef? Parse(object? value)
    {
        if (value is not string s || !s.StartsWith(Prefix, StringComparison.Ordinal)) return null;
        var parts = s[Prefix.Length..].Split(':');
        return parts.Length == 2 && Guid.TryParseExact(parts[0], "N", out var id)
            ? new BlobRef(id, parts[1])
            : null;
    }
}

/// <summary>
/// Hält die Bilder eines Textdokuments **außerhalb** des gespeicherten Dokuments.
/// <para>
/// Bisher wanderten Bilder mit ins XamlPackage – und WPF kodiert sie dabei als PNG neu.
/// Gemessen: ein Word-Dokument mit drei Fotos (2 MB JPEG) wurde zu 16,8 MB und riss damit
/// die 16-MB-Grenze von LiteDB; der Rückexport war 8,2× so groß wie das Original. Jetzt
/// liegt das **Original unangetastet** im Blob-Speicher, im Dokument steht nur ein Verweis,
/// und beim DOCX-Export gehen genau diese Bytes zurück.
/// </para>
/// <para>
/// Der Verweis wird an zwei Stellen geführt, weil XamlPackage nur wenige Eigenschaften
/// mitspeichert: zur Laufzeit im <c>Tag</c> (bequem, wird nicht serialisiert), beim Speichern
/// kurzzeitig im <c>ToolTip</c> (die einzige Zeichenketten-Eigenschaft, die das Paket erhält).
/// Der ToolTip existiert also nur in der gespeicherten Fassung, nie im offenen Dokument.
/// </para>
/// </summary>
public static class DocumentImages
{
    /// <summary>Längste Kante der Anzeige-Ableitung. Das Original bleibt davon unberührt.</summary>
    private const int ProxyLongSide = 2048;

    /// <summary>Alle Bilder eines Dokuments, in Dokumentreihenfolge.</summary>
    public static List<Image> All(FlowDocument doc)
    {
        var found = new List<Image>();
        Collect(doc, found);
        return found;
    }

    private static void Collect(object? node, List<Image> found)
    {
        switch (node)
        {
            case Image image: found.Add(image); break;
            case FlowDocument d: Collect(d.Blocks, found); break;
            case Section s: Collect(s.Blocks, found); break;
            case Paragraph p: Collect(p.Inlines, found); break;
            case Span sp: Collect(sp.Inlines, found); break;
            case List list: Collect(list.ListItems, found); break;
            case ListItem li: Collect(li.Blocks, found); break;
            case Table t: Collect(t.RowGroups, found); break;
            case TableRowGroup g: Collect(g.Rows, found); break;
            case TableRow r: Collect(r.Cells, found); break;
            case TableCell c: Collect(c.Blocks, found); break;
            case Figure f: Collect(f.Blocks, found); break;
            case Floater fl: Collect(fl.Blocks, found); break;
            case InlineUIContainer iuc: Collect(iuc.Child, found); break;
            case BlockUIContainer buc: Collect(buc.Child, found); break;
            case System.Collections.IEnumerable list: foreach (var x in list) Collect(x, found); break;
        }
    }

    /// <summary>
    /// Legt die Originalbytes ab und hängt den Verweis ans Bild. Aufzurufen beim Import,
    /// solange die Originaldatei noch vorliegt.
    /// </summary>
    public static void Remember(Image image, byte[] original, string extension, BlobStore blobs) =>
        image.Tag = new BlobRef(blobs.Put(original), extension);

    /// <summary>
    /// Anzeige-Ableitung aus Originalbytes: dekodiert höchstens auf <see cref="ProxyLongSide"/>.
    /// Ein 6000-px-Foto belegt so rund ein Zehntel des Speichers, ohne dass am Original etwas
    /// verloren geht.
    /// </summary>
    public static BitmapImage? Proxy(byte[] original)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = new MemoryStream(original);
            bmp.DecodePixelWidth = ProxyLongSide;   // WPF hält das Seitenverhältnis
            bmp.EndInit();
            bmp.Freeze();
            return bmp.PixelWidth > 0 ? bmp : null;
        }
        catch
        {
            return null;   // unlesbares Bild überspringen statt das Dokument zu verlieren
        }
    }

    /// <summary>
    /// Bereitet das Dokument fürs Speichern vor: Bildquellen raus, Verweise rein. Beim
    /// Verwerfen des Rückgabewerts wird der ursprüngliche Zustand wiederhergestellt – auch
    /// wenn das Speichern dazwischen fehlschlägt.
    /// </summary>
    public static IDisposable Detach(FlowDocument doc, BlobStore blobs) => new Detached(doc, blobs);

    /// <summary>
    /// Stellt nach dem Laden die Bilder wieder her: Verweis aus dem ToolTip lesen, Original
    /// aus dem Blob-Speicher holen, Ableitung anzeigen.
    /// </summary>
    public static void Attach(FlowDocument doc, BlobStore blobs)
    {
        foreach (var image in All(doc))
        {
            if (BlobRef.Parse(image.ToolTip) is not { } reference) continue;

            image.Tag = reference;
            image.ToolTip = null;
            if (blobs.Read(reference.Id) is { } bytes && Proxy(bytes) is { } proxy)
                image.Source = proxy;
        }
    }

    /// <summary>Ids aller Bilder eines Dokuments – für den Aufräumlauf verwaister Blobs.</summary>
    public static IEnumerable<Guid> UsedBlobs(FlowDocument doc) =>
        All(doc).Select(i => i.Tag).OfType<BlobRef>().Select(r => r.Id);

    /// <summary>
    /// Verweis eines Bildes – vorhandener oder neu angelegter.
    /// <para>
    /// Bilder ohne Verweis in den Blob-Speicher übernehmen: Diagramme aus der App, eingefügte
    /// Zwischenablage-Bilder und alle Dokumente aus der Zeit, als die Bilder noch im Datensatz
    /// lagen. Nur hier gibt es keine Originaldatei mehr, deshalb wird verlustfrei als PNG
    /// gesichert.
    /// </para>
    /// <para>
    /// <b>Öffentlich seit der Übernahme</b> (§4.22): <see cref="FlowZuTd"/> braucht denselben
    /// Weg, und eine zweite Fassung davon wäre die Doppelung aus §4.10 — mit dem Fehlerbild
    /// „dasselbe Bild liegt zweimal im Blob-Speicher".
    /// </para>
    /// </summary>
    public static BlobRef? Adopt(Image image, BlobStore blobs)
    {
        if (image.Tag is BlobRef vorhanden) return vorhanden;
        if (image.Source is not BitmapSource source) return null;

        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var ms = new MemoryStream();
            encoder.Save(ms);

            var reference = new BlobRef(blobs.Put(ms.ToArray()), "png");
            image.Tag = reference;
            return reference;
        }
        catch
        {
            return null;   // dann bleibt dieses eine Bild eben im Dokument
        }
    }

    /// <summary>
    /// Setzt für die Dauer des Rückgabewerts die **Originale** als Bildquelle ein. Für den
    /// Export: gerastert wird aus dem Original, nicht aus der Anzeige-Ableitung – sonst wäre
    /// ein seitenbreites Foto im PDF weicher als in der Vorlage.
    /// </summary>
    public static IDisposable WithFullResolution(FlowDocument doc, BlobStore blobs) =>
        new FullResolution(doc, blobs);

    /// <summary>
    /// Wie viele Bilder beim letzten Export **nicht** in Originalauflösung eingesetzt werden
    /// konnten, weil ihre Daten fehlten. Ohne diesen Zähler exportierte die App stillschweigend
    /// die kleinere Anzeige-Ableitung – auf Seitenbreite hochgezogen sieht das verpixelt aus,
    /// und niemand erfuhr, warum.
    /// </summary>
    public static int LastExportMissingOriginals { get; private set; }

    private sealed class FullResolution : IDisposable
    {
        private readonly List<(Image Image, ImageSource? Source)> _saved = new();

        public FullResolution(FlowDocument doc, BlobStore blobs)
        {
            int missing = 0;
            foreach (var image in All(doc))
            {
                if (image.Tag is not BlobRef reference) continue;
                if (blobs.Read(reference.Id) is not { } bytes) { missing++; continue; }
                if (Decode(bytes) is not { } full) { missing++; continue; }

                _saved.Add((image, image.Source));
                image.Source = full;
            }
            LastExportMissingOriginals = missing;
        }

        private static BitmapImage? Decode(byte[] bytes)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = new MemoryStream(bytes);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            foreach (var (image, source) in _saved) image.Source = source;
        }
    }

    private sealed class Detached : IDisposable
    {
        // 1×1-Bild statt leerer Quelle: WPF verwirft ein Bild ohne Quelle beim Speichern ganz.
        private static readonly BitmapSource Placeholder = CreatePlaceholder();

        private readonly List<(Image Image, ImageSource? Source, object? ToolTip)> _saved = new();

        public Detached(FlowDocument doc, BlobStore blobs)
        {
            foreach (var image in All(doc))
            {
                var reference = DocumentImages.Adopt(image, blobs);
                if (reference == null) continue;

                _saved.Add((image, image.Source, image.ToolTip));
                image.ToolTip = reference.Value.ToString();
                image.Source = Placeholder;
            }
        }

        public void Dispose()
        {
            foreach (var (image, source, toolTip) in _saved)
            {
                image.Source = source;
                image.ToolTip = toolTip;
            }
        }

        private static BitmapSource CreatePlaceholder()
        {
            var bmp = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
            bmp.Freeze();
            return bmp;
        }
    }
}
