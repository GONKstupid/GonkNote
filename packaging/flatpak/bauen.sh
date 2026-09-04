#!/usr/bin/env bash
# Baut das Flatpak des Linux-Kopfs. HANDOFF §6, Phase 5, Schritt ③.
#
# Zwei Schritte, und die Reihenfolge ist zwingend:
#   1. `dotnet publish` legt den fertigen Kopf nach build/publish
#   2. `flatpak-builder` packt genau diesen Ordner ein (das Manifest baut NICHT selbst —
#      die Begründung steht oben im Manifest)
#
# Voraussetzungen (einmalig, ohne sudo):
#   flatpak remote-add --user --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo
#   flatpak install --user flathub org.freedesktop.Platform//25.08 org.freedesktop.Sdk//25.08
#
# Aufruf:
#   ./bauen.sh              baut und installiert ins Nutzer-Flatpak
#   ./bauen.sh --nur-bauen  baut, ohne zu installieren
set -euo pipefail

HIER="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WURZEL="$(cd "$HIER/../.." && pwd)"
ID=io.github.gonkstupid.GonkNote

cd "$HIER"

echo "▶ 1/2  dotnet publish (selbstenthalten, linux-x64)"
rm -rf build/publish
dotnet publish "$WURZEL/src/GonkNote.Avalonia" \
    -c Release -r linux-x64 --self-contained true \
    -o build/publish

echo "▶ 2/2  flatpak-builder"
# --force-clean: ein stehengebliebener build/ aus einem abgebrochenen Lauf ist die
# häufigste Ursache für „es baut, aber die Änderung ist nicht drin".
ARGS=(--force-clean --user --repo=build/repo)
if [[ "${1:-}" != "--nur-bauen" ]]; then
    ARGS+=(--install)
fi
flatpak-builder "${ARGS[@]}" build/flatpak "$ID.yml"

echo
echo "✅ Fertig. Starten mit:"
echo "   flatpak run $ID -- --db /tmp/gonk-test/gonknote.sqlite"
echo
echo "⚠ Ohne --db greift der Lauf auf den echten Bestand zu (HANDOFF Dauerregel 4)."
echo "   In der Sandbox ist das ~/.var/app/$ID/config/GonkNote und nicht ~/.config/GonkNote."
