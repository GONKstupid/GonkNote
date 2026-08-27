using System.Collections.Generic;
using System.Linq;
using GonkNote.Core.Editing;
using GonkNote.Core.Models;
using GonkNote.Core.Rendering;
using GonkNote.Core.Services;
using SkiaSharp;

namespace GonkNote.Views;

/// <summary>
/// Die Zwischenablage der Zeichenfläche: Kopieren, Ausschneiden, Einfügen, Duplizieren.
///
/// <para>
/// <b>Neu in Phase 4.5, Stück 5</b> — bis dahin konnte der Linux-Kopf nichts davon, während
/// der WPF-Kopf es seit Langem hat. Geklont und platziert wird in Core
/// (<see cref="WbKlon"/>, §4.61); was hier steht, ist der Weg dorthin und zurück.
/// </para>
///
/// <para>
/// <b>Die eigene Ablage ist eine Liste im Programm und nicht die des Systems.</b> Ein Strich
/// mit seinen Druck- und Neigungswerten hat in der Systemablage keine Form, in der ein
/// anderes Programm etwas damit anfangen könnte — und beim Zurücklesen wäre er ein Bild.
/// Die Systemablage kommt nur ins Spiel, wenn die eigene leer ist: dann wird ein
/// <b>Bild</b> daraus eingefügt. Genau dieselbe Reihenfolge wie im WPF-Kopf.
/// </para>
/// </summary>
public partial class WhiteboardView
{
    /// <summary>
    /// Die programmeigene Ablage. <b>Sie überlebt keinen Neustart</b> — dasselbe wie drüben,
    /// und für den Zweck richtig: wer über Programmgrenzen hinweg kopieren will, kopiert ein
    /// Bild, und dafür gibt es die Systemablage.
    /// </summary>
    private readonly List<WbElement> _ablage = [];

    private void Kopieren()
    {
        if (_selection.Count == 0) return;
        _ablage.Clear();
        _ablage.AddRange(WbKlon.Klonen(_selection));
    }

    private void Ausschneiden()
    {
        if (_selection.Count == 0) return;
        Kopieren();
        DeleteSelection();
    }

    private void Duplizieren()
    {
        if (_page == null || _vm == null || _selection.Count == 0) return;

        var klone = WbKlon.Klonen(_selection);
        WbKlon.Platzieren(klone, null);          // ohne Ziel: schräg weg, sonst unsichtbar

        _page.Elements.AddRange(klone);
        _vm.Undo.Push(_page, new AddElementsAction(klone));
        MarkDirty();
        AuswahlZeigen(klone);
    }

    /// <summary>
    /// Fügt ein. Mit <paramref name="ziel"/> kommt die Mitte der Gruppe dorthin — so trifft
    /// „hier einfügen" aus den Schnellaktionen die Stelle, an der der Zeiger stand.
    /// <para>
    /// <b>Ist die eigene Ablage leer, wird ein Bild aus der Systemablage versucht.</b> Erst
    /// dann, und nicht davor: was gerade im Programm kopiert wurde, ist näher an dem, was der
    /// Nutzer meint, als was irgendwann in der Systemablage landete.
    /// </para>
    /// </summary>
    private void Einfuegen(SKPoint? ziel)
    {
        if (_page == null || _vm == null) return;

        if (_ablage.Count == 0)
        {
            BildAusSystemablage();
            return;
        }

        var klone = WbKlon.Klonen(_ablage);
        WbKlon.Platzieren(klone, ziel);

        _page.Elements.AddRange(klone);
        _vm.Undo.Push(_page, new AddElementsAction(klone));
        MarkDirty();
        AuswahlZeigen(klone);
    }

    /// <summary>
    /// Ein Bild aus der Systemablage auf die Fläche. Es geht denselben Weg wie ein
    /// importiertes Bild (<see cref="WbImagePrep.ForImport"/> und <see cref="BilderAblegen"/>)
    /// — <b>also auch durch dieselbe Verkleinerung</b>, sonst läge ein Bildschirmfoto in
    /// voller Größe in der Datenbank.
    /// </summary>
    private void BildAusSystemablage()
    {
        var roh = App.Platform.Clipboard.GetImage();
        if (roh == null) return;

        if (WbImagePrep.ForImport(roh) is { } bild)
            BilderAblegen([(bild.Data, bild.Width, bild.Height)]);
    }
}
