using GonkNote.Core.Models;
using GonkNote.Core.Services;

namespace GonkNote.Core.Tests;

/// <summary>
/// Speichern → schließen → neu öffnen → laden: kommt heraus, was hineinging?
/// <para>
/// Das ist der Test, der Phase 2 bewachen muss. Dort wird der Bauch des
/// <see cref="DatabaseService"/> von LiteDB auf SQLite umgebaut, und die Roadmap stuft
/// „Datenverlust" dabei als hohes Risiko ein. Die öffentliche API bleibt gleich — also
/// müssen diese Tests danach unverändert grün sein. Werden sie es nicht, sind Daten
/// betroffen, nicht Code.
/// </para>
/// Bewusst über eine **neue** <see cref="DatabaseService"/>-Instanz gelesen: würde derselbe
/// Dienst weiterbenutzt, könnte ein Cache das Ergebnis liefern, ohne dass jemals etwas auf
/// der Platte gestanden hätte.
/// </summary>
public sealed class DatenbankRoundtripTests
{
    [Fact]
    public void Notizbuch_kommt_unveraendert_zurueck()
    {
        using var ws = new TempWorkspace("roundtrip-board");

        var original = Beispieldokument.Notizbuch();
        // SaveBoard leert die Byte-Felder (Bilder wandern in den Blob-Speicher) — die
        // erwarteten Bytes deshalb vorher festhalten.
        var erwarteteBilder = Bilddaten(original);

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            db.UpsertItem(new NoteItem { Id = original.Id, Name = "Testbuch", Kind = ItemKind.Notebook });
            db.SaveBoard(original);
        }

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            var item = Assert.Single(db.GetAllItems());
            var geladen = db.GetBoard(item);

            GleichesDokument(Beispieldokument.Notizbuch(), geladen);
            Assert.Equal(erwarteteBilder, Bilddaten(geladen));
        }
    }

    [Fact]
    public void Textdokument_kommt_unveraendert_zurueck()
    {
        using var ws = new TempWorkspace("roundtrip-text");

        var original = Beispieldokument.Textdokument();

        using (var db = new DatabaseService(ws.DatabasePath))
            db.SaveText(original);

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            var geladen = db.GetText(original.Id);

            Assert.Equal(original.Rtf, geladen.Rtf);

            // **Beide Felder nebeneinander**: das Altformat und das eigene Modell. Das eine
            // wird nie überschrieben, das andere ist leer, solange die Übernahme aussteht
            // (§4.22).
            Assert.Equal(original.Model, geladen.Model);
            Assert.Equal(original.MigrationIssue, geladen.MigrationIssue);

            Assert.Equal(original.Images, geladen.Images);
            Assert.Equal(original.PageFormat, geladen.PageFormat);
            Assert.Equal(original.Landscape, geladen.Landscape);
            Assert.Equal(original.MarginLeftCm, geladen.MarginLeftCm);
            Assert.Equal(original.MarginTopCm, geladen.MarginTopCm);
            Assert.Equal(original.MarginRightCm, geladen.MarginRightCm);
            Assert.Equal(original.MarginBottomCm, geladen.MarginBottomCm);
            Assert.Equal(original.FooterText, geladen.FooterText);
            Assert.Equal(original.SuppressHeaderOnFirstPage, geladen.SuppressHeaderOnFirstPage);
            Assert.Equal(original.WatermarkImage, geladen.WatermarkImage);
            Assert.Equal(original.WatermarkOpacity, geladen.WatermarkOpacity);
            Assert.Equal(original.ShowPageBreaks, geladen.ShowPageBreaks);
        }
    }

    /// <summary>
    /// Der Fall, der in V1 das Öffnen von Textdokumenten hat abstürzen lassen: LiteDB macht
    /// aus einem leeren String standardmäßig BSON-Null (<c>EmptyStringToNull</c>). Der
    /// <see cref="DatabaseService"/> schaltet das ab, zusätzlich sind die Setter null-sicher.
    /// **Beim Nachbau in SQLite/Json muss beides erhalten bleiben** (HANDOFF §7).
    /// </summary>
    [Fact]
    public void Leerer_String_kommt_leer_zurueck_und_nicht_als_null()
    {
        using var ws = new TempWorkspace("empty-string");

        using (var db = new DatabaseService(ws.DatabasePath))
            db.SaveText(new TextDoc { Id = Beispieldokument.DokumentId, HeaderText = "", FooterText = "" });

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            var geladen = db.GetText(Beispieldokument.DokumentId);

            Assert.NotNull(geladen.HeaderText);
            Assert.NotNull(geladen.FooterText);
            Assert.Equal("", geladen.HeaderText);
            Assert.Equal("", geladen.FooterText);
            // PageFormat hat einen null-sicheren Setter mit Standardwert — er darf nicht
            // erst greifen, wenn schon null angekommen ist.
            Assert.Equal("A4", geladen.PageFormat);
        }
    }

    [Fact]
    public void Ordnerbaum_kommt_unveraendert_zurueck()
    {
        using var ws = new TempWorkspace("roundtrip-tree");

        var ordner = new NoteItem
        {
            Name = "Schule",
            Kind = ItemKind.Folder,
            IconColor = "#FF3B82F6",
            IsPinned = true,
            IsFavorite = true,
            CreatedUtc = Beispieldokument.Zeitpunkt,
        };
        var buch = new NoteItem { ParentId = ordner.Id, Name = "Mathe", Kind = ItemKind.Notebook };

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            db.UpsertItem(ordner);
            db.UpsertItem(buch);
        }

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            var alle = db.GetAllItems();
            var geladen = Assert.Single(alle, x => x.Id == ordner.Id);

            Assert.Equal(ordner.Name, geladen.Name);
            Assert.Equal(ordner.Kind, geladen.Kind);
            Assert.Equal(ordner.IconColor, geladen.IconColor);
            Assert.True(geladen.IsPinned);
            Assert.True(geladen.IsFavorite);
            Assert.Equal(ordner.ParentId, geladen.ParentId);
            Assert.Equal(buch.ParentId, Assert.Single(alle, x => x.Id == buch.Id).ParentId);

            // BSON kennt Datumsangaben nur auf die Millisekunde. Feiner darf hier niemand
            // vergleichen — und feiner braucht es auch nichts, das Feld zeigt „geändert am".
            Assert.Equal(ordner.CreatedUtc, geladen.CreatedUtc.ToUniversalTime(), TimeSpan.FromMilliseconds(1));
        }
    }

    /// <summary>
    /// Löschen muss Kinder **und** Inhalte mitnehmen, sonst wächst die Datenbank mit
    /// Dokumenten, auf die kein Eintrag mehr zeigt (und der Blob-Aufräumlauf sieht sie
    /// weiterhin als „benutzt").
    /// </summary>
    [Fact]
    public void Loeschen_nimmt_Kinder_und_Inhalte_mit()
    {
        using var ws = new TempWorkspace("delete-recursive");

        var ordner = new NoteItem { Name = "Weg damit", Kind = ItemKind.Folder };
        var buch = new NoteItem { ParentId = ordner.Id, Name = "Buch", Kind = ItemKind.Notebook };
        var text = new NoteItem { ParentId = buch.Id, Name = "Text", Kind = ItemKind.TextDocument };
        var bleibt = new NoteItem { Name = "Bleibt", Kind = ItemKind.Folder };

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            db.UpsertItem(ordner);
            db.UpsertItem(buch);
            db.UpsertItem(text);
            db.UpsertItem(bleibt);
            db.SaveBoard(new WhiteboardDoc { Id = buch.Id, Pages = { WhiteboardDoc.NewNotebookPage() } });
            db.SaveText(new TextDoc { Id = text.Id, Rtf = [1, 2, 3] });

            db.DeleteItemRecursive(ordner.Id);
        }

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            Assert.Equal([bleibt.Id], db.GetAllItems().Select(x => x.Id));

            // Der Inhalt ist weg: GetBoard liefert ein frisches Notizbuch (2 Seiten:
            // Cover + erste Seite), GetText ein leeres Dokument.
            var frisch = db.GetBoard(new NoteItem { Id = buch.Id, Kind = ItemKind.Notebook });
            Assert.Equal(2, frisch.Pages.Count);
            Assert.True(frisch.Pages[0].IsCover);
            Assert.Empty(db.GetText(text.Id).Rtf);
        }
    }

    [Fact]
    public void Einstellungen_kommen_unveraendert_zurueck()
    {
        using var ws = new TempWorkspace("settings");

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            db.SetSetting("theme", "Dark");
            db.SetSetting("language", "en");
            db.SetSetting("theme", "Light");   // Upsert, kein zweiter Datensatz
        }

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            Assert.Equal("Light", db.GetSetting("theme"));
            Assert.Equal("en", db.GetSetting("language"));
            Assert.Null(db.GetSetting("gibt-es-nicht"));
        }
    }

    /// <summary>
    /// <see cref="DatabaseService.GetCover"/> projiziert nur das Cover-Feld, damit die Galerie
    /// nicht jedes bildlastige Notizbuch komplett laden muss. Die Abkürzung muss dasselbe
    /// liefern wie der vollständige Weg.
    /// </summary>
    [Fact]
    public void Cover_Abkuerzung_liefert_dasselbe_wie_der_ganze_Weg()
    {
        using var ws = new TempWorkspace("cover");

        var doc = Beispieldokument.Notizbuch();
        using (var db = new DatabaseService(ws.DatabasePath))
            db.SaveBoard(doc);

        using (var db = new DatabaseService(ws.DatabasePath))
        {
            var cover = db.GetCover(doc.Id);

            Assert.NotNull(cover);
            Assert.Equal("#0F766E", cover.GradientStart);
            Assert.Equal("#BE185D", cover.GradientEnd);
            Assert.Equal("Georgia", cover.FontFamily);
            Assert.Equal(doc.Cover!.ImageId, cover.ImageId);
        }
    }

    // ---- Vergleich ----------------------------------------------------------------------

    /// <summary>
    /// Feldweiser Vergleich statt eines allgemeinen Objektvergleichs: so steht im Test
    /// schwarz auf weiß, welche Felder die Persistenz tragen muss, und ein Fehlschlag nennt
    /// das Feld beim Namen.
    /// </summary>
    private static void GleichesDokument(WhiteboardDoc erwartet, WhiteboardDoc ist)
    {
        Assert.Equal(erwartet.Id, ist.Id);
        Assert.Equal(erwartet.Pages.Count, ist.Pages.Count);

        Assert.NotNull(ist.NewPageTemplate);
        Assert.Equal(erwartet.NewPageTemplate!.Width, ist.NewPageTemplate.Width);
        Assert.Equal(erwartet.NewPageTemplate.Height, ist.NewPageTemplate.Height);
        Assert.Equal(erwartet.NewPageTemplate.Background, ist.NewPageTemplate.Background);
        Assert.Equal(erwartet.NewPageTemplate.Shade, ist.NewPageTemplate.Shade);

        Assert.NotNull(ist.Cover);
        Assert.Equal(erwartet.Cover!.GradientStart, ist.Cover.GradientStart);
        Assert.Equal(erwartet.Cover.GradientEnd, ist.Cover.GradientEnd);
        Assert.Equal(erwartet.Cover.FontFamily, ist.Cover.FontFamily);
        Assert.Equal(erwartet.Cover.ImageId, ist.Cover.ImageId);

        for (int i = 0; i < erwartet.Pages.Count; i++)
        {
            var a = erwartet.Pages[i];
            var b = ist.Pages[i];

            Assert.Equal(a.Width, b.Width);
            Assert.Equal(a.Height, b.Height);
            Assert.Equal(a.Background, b.Background);
            Assert.Equal(a.Shade, b.Shade);
            Assert.Equal(a.IsCover, b.IsCover);
            Assert.Equal(a.IsInfinite, b.IsInfinite);
            Assert.Equal(a.BackgroundImageId, b.BackgroundImageId);

            // Nicht an den Bytes ablesen: die stehen nach dem Speichern nur noch im
            // Blob-Speicher. Genau dafür gibt es HasBackgroundImage (HANDOFF §7).
            Assert.Equal(a.HasBackgroundImage, b.HasBackgroundImage);

            Assert.Equal(a.Elements.Count, b.Elements.Count);
            for (int k = 0; k < a.Elements.Count; k++)
                GleichesElement(a.Elements[k], b.Elements[k]);
        }
    }

    private static void GleichesElement(WbElement erwartet, WbElement ist)
    {
        // Der Typ ist der wichtigste Vergleich: er hängt am _type-Feld, und ein falsch
        // aufgelöster Typname ist der Fehler, der Bestandsdokumente unlesbar macht.
        Assert.Equal(erwartet.GetType(), ist.GetType());
        Assert.Equal(erwartet.Id, ist.Id);
        Assert.Equal(erwartet.Rotation, ist.Rotation);

        switch (erwartet, ist)
        {
            case (StrokeElement a, StrokeElement b):
                Assert.Equal(a.Color, b.Color);
                Assert.Equal(a.Width, b.Width);
                Assert.Equal(a.Kind, b.Kind);
                Assert.Equal(a.Points.Count, b.Points.Count);
                for (int i = 0; i < a.Points.Count; i++)
                {
                    Assert.Equal(a.Points[i].X, b.Points[i].X);
                    Assert.Equal(a.Points[i].Y, b.Points[i].Y);
                    Assert.Equal(a.Points[i].P, b.Points[i].P);
                    // Neigung seit Phase 3 (HANDOFF §4.11). Sie steht hier, damit das Feld
                    // überhaupt einen Wächter hat: sie wird bedingt geschrieben
                    // (WhenWritingDefault), und ein Fehler darin sähe wie „war halt 0" aus.
                    Assert.Equal(a.Points[i].TX, b.Points[i].TX);
                    Assert.Equal(a.Points[i].TY, b.Points[i].TY);
                }
                break;

            case (ShapeElement a, ShapeElement b):
                Assert.Equal(a.Shape, b.Shape);
                Assert.Equal(a.X1, b.X1);
                Assert.Equal(a.Y1, b.Y1);
                Assert.Equal(a.X2, b.X2);
                Assert.Equal(a.Y2, b.Y2);
                Assert.Equal(a.Color, b.Color);
                Assert.Equal(a.StrokeWidth, b.StrokeWidth);
                Assert.Equal(a.Fill, b.Fill);
                break;

            case (TextElement a, TextElement b):
                Assert.Equal(a.X, b.X);
                Assert.Equal(a.Y, b.Y);
                Assert.Equal(a.Text, b.Text);
                Assert.Equal(a.Color, b.Color);
                Assert.Equal(a.FontSize, b.FontSize);
                Assert.Equal(a.Background, b.Background);
                Assert.Equal(a.FontFamily, b.FontFamily);
                break;

            case (ImageElement a, ImageElement b):
                Assert.Equal(a.X, b.X);
                Assert.Equal(a.Y, b.Y);
                Assert.Equal(a.Width, b.Width);
                Assert.Equal(a.Height, b.Height);
                break;

            case (StickyNoteElement a, StickyNoteElement b):
                Assert.Equal(a.X, b.X);
                Assert.Equal(a.Y, b.Y);
                Assert.Equal(a.Width, b.Width);
                Assert.Equal(a.Height, b.Height);
                Assert.Equal(a.Text, b.Text);
                Assert.Equal(a.Color, b.Color);
                Assert.Equal(a.TextColor, b.TextColor);
                Assert.Equal(a.FontSize, b.FontSize);
                Assert.Equal(a.FontFamily, b.FontFamily);
                break;

            default:
                Assert.Fail($"Unbekannter Elementtyp {erwartet.GetType().Name} — " +
                            "GleichesElement muss ergänzt werden, sonst bewacht kein Test die neuen Felder.");
                break;
        }
    }

    /// <summary>
    /// Alle Bilddaten eines Dokuments über den Weg, den auch das Zeichnen nimmt: erst der
    /// Datensatz, dann der Blob-Speicher. Nach dem Speichern liefert nur noch der zweite
    /// Weg etwas — der Vergleich muss trotzdem stimmen.
    /// </summary>
    private static List<byte[]> Bilddaten(WhiteboardDoc doc)
    {
        var alle = new List<byte[]>();
        if (doc.Cover != null)
            alle.Add(ImageCache.Bytes(doc.Cover.ImageId, doc.Cover.Image) ?? []);

        foreach (var page in doc.Pages)
        {
            alle.Add(ImageCache.Bytes(page.BackgroundImageId, page.BackgroundImage) ?? []);
            foreach (var bild in page.Elements.OfType<ImageElement>())
                alle.Add(ImageCache.Bytes(bild.Id, bild.Data) ?? []);
        }
        return alle;
    }
}
