namespace GonkNote.Core.Text;

/// <summary>
/// <b>Wie ein Markdown-Betrachter mit Verweisen auf andere mitgelieferte Dokumente
/// umgeht</b> — gefragt wird zweimal und mit zwei verschiedenen Fragen.
///
/// <para>
/// <b>Der Anlass ist ein Fund aus dem Prüflauf von Phase 5, Schritt ④</b>, und er saß in
/// beiden Köpfen und in beiden Sprachen: Die Anleitung verweist dreimal auf das README,
/// das README zweimal auf <c>THIRD-PARTY-NOTICES.md</c> — und <b>keiner dieser fünf
/// Verweise tat etwas.</b> Sie sahen trotzdem aus wie Verweise: Ein <c>.md</c>-Ziel, das
/// niemand annimmt, wurde in beiden Betrachtern als <b>eingefärbter Text</b> gezeichnet,
/// in derselben Akzentfarbe wie ein echter. <i>Ein Verweis, der aussieht wie einer und
/// keiner ist</i> (§4.83, dasselbe Muster wie das Fenster, das nicht sagte, was es tut).
/// </para>
///
/// <para>
/// <b>Warum zwei Glieder und nicht eines.</b> Naheliegend wäre ein einziger Behandler, der
/// <c>true</c> zurückgibt, wenn er das Ziel genommen hat — dann müsste der Betrachter ihn
/// aber schon <b>beim Bauen</b> rufen, um zu wissen, ob er einen Verweis zeichnen darf, und
/// das Bauen öffnete Fenster. <b>Fragen und Handeln sind zwei Zeitpunkte</b>, also sind es
/// zwei Glieder: <see cref="Kann"/> darf nichts tun, <see cref="Oeffnen"/> darf nichts
/// beantworten.
/// </para>
///
/// <para>
/// <b>Und warum das in Core steht</b>, obwohl beide Betrachter in ihren Köpfen liegen: Es
/// ist der <b>Vertrag</b> zwischen ihnen, kein Pixel. Stünde er zweimal, wären es zwei —
/// und die Erfahrung dieses Projekts ist, dass dann einer stehen bleibt (§4.9, §4.26,
/// §4.31, §4.39, §4.77).
/// </para>
/// </summary>
/// <param name="Kann">
/// <b>Nimmst du dieses Ziel?</b> Wird <b>beim Bauen</b> gefragt und muss <b>frei von
/// Wirkung</b> sein. <c>false</c> heißt: Der Text bleibt Text — ohne Akzentfarbe, ohne
/// Zeigefinger, ohne Klick.
/// </param>
/// <param name="Oeffnen">
/// <b>Öffne dieses Ziel.</b> Wird nur für Ziele gerufen, für die <see cref="Kann"/> schon
/// <c>true</c> gesagt hat.
/// </param>
public sealed record Dokumentverweise(Func<string, bool> Kann, Action<string> Oeffnen);
