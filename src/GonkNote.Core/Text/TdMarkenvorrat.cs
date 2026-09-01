namespace GonkNote.Core.Text;

/// <summary>
/// <b>Was zur Auswahl steht</b> — die Aufzählungszeichen und Nummerierungsarten, die ein Kopf
/// in seiner Markenauswahl anbietet (§4.88).
///
/// <para>
/// <b>Warum das in Core liegt und nicht in der Oberfläche.</b> Bis §4.88 stand die Liste als
/// <c>BulletStyles</c>/<c>NumberStyles</c> fest verdrahtet im WPF-Kopf. Der Linux-Kopf hätte
/// sie ein zweites Mal gebraucht — und damit wäre eingetreten, was schon dreimal eingetreten
/// ist (§4.77, §4.78, §4.82): zwei Listen, von denen später eine einen Eintrag mehr hat.
/// <b>Es ist kein Rechenwerk, aber es ist eine Entscheidung</b>, und Entscheidungen gehören
/// dorthin, wo beide Köpfe sie lesen.
/// </para>
/// <para>
/// <b>Die Zeichen sind Text und keine Marken-Art.</b> Alle sechs Punkte sind
/// <see cref="TdListMarker.Bullet"/> — was sie unterscheidet, ist das Zeichen in
/// <see cref="TdListLevel.Text"/>. Bei den Nummerierungen ist es umgekehrt: dort trägt die
/// **Art** die Bedeutung, und der Text ist nur das Muster drumherum (§4.17).
/// </para>
/// </summary>
public static class TdMarkenvorrat
{
    /// <summary>
    /// Die angebotenen Aufzählungszeichen. <b>Der erste ist der, den
    /// <see cref="TdListLevel.Punkt"/> ohnehin setzt</b> — wer die Auswahl öffnet und den
    /// ersten nimmt, bekommt genau das, was der Knopf daneben tut.
    /// </summary>
    public static readonly IReadOnlyList<string> Punkte = ["•", "◦", "▪", "▫", "‣", "–"];

    /// <summary>
    /// Die angebotenen Nummerierungsarten, in der Reihenfolge, in der Word sie zeigt.
    /// <b><see cref="TdListMarker.Bullet"/> steht nicht darin</b> — es ist keine Zählung
    /// (<c>TdListEdit.Nummerierend</c>), und in dieser Liste stünde es als „1., a., A., i., I.,
    /// Punkt" da.
    /// </summary>
    public static readonly IReadOnlyList<TdListMarker> Nummern =
    [
        TdListMarker.Decimal,
        TdListMarker.LowerLetter,
        TdListMarker.UpperLetter,
        TdListMarker.LowerRoman,
        TdListMarker.UpperRoman,
    ];

    /// <summary>
    /// Wie der erste Eintrag dieser Art aussieht — <b>die Beschriftung der Kachel</b>, „1.",
    /// „a.", „I." und so fort.
    ///
    /// <para>
    /// <b>Gerechnet und nicht aufgeschrieben:</b> Die Schreibweise steht in
    /// <see cref="TdListNumbering.Formatiert"/>, und sie ist dieselbe, mit der die Liste später
    /// wirklich gezeichnet wird. Eine zweite Tabelle mit „a." darin wäre die erste, die von der
    /// Zählung abweicht — genau die Falle aus §4.13.
    /// </para>
    /// </summary>
    public static string Beispiel(TdListMarker art) =>
        art == TdListMarker.Bullet
            ? Punkte[0]
            : TdListNumbering.Formatiert(1, art) + ".";

    /// <summary>Das Textmuster einer Ebene für eine Nummerierungsart — <c>„%1."</c> und so fort.</summary>
    public static string Muster(int ebene) => "%" + (ebene + 1) + ".";
}
